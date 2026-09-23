using Marten;
using VietAisHsk.Api.Modules.Content;

namespace VietAisHsk.Api.Infrastructure;

/// <summary>
/// Persistent Content question-bank adapter. The bootstrap catalog is seeded only when the
/// document type is empty; after that, drafts/publish changes live in Marten across restarts.
/// </summary>
public sealed class MartenQuestionBank(IDocumentStore documentStore) : IQuestionBank, IQuestionBankAdmin
{
    private readonly BootstrapQuestionBank seed = new();

    public IReadOnlyList<ContentQuestion> GetPublishedQuestions(string? type = null) =>
        GetAllQuestions(type).Where(question => question.Status == "Published").ToArray();

    public IReadOnlyList<ContentQuestion> GetPublishedQuestions(IReadOnlyList<string> questionIds) =>
        GetAllQuestions()
            .Where(question => question.Status == "Published")
            .Where(questionIds.ContainsById)
            .OrderBy(question => questionIds.FindIndex(id => id.Equals(question.Id, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

    public IReadOnlyList<ContentQuestion> GetAllQuestions(string? type = null)
    {
        EnsureSeeded();
        using var session = documentStore.QuerySession();
        var questions = session.Query<ContentQuestion>().ToListAsync().GetAwaiter().GetResult();
        return questions
            .Where(question => string.IsNullOrWhiteSpace(type)
                || question.Type.Equals(type.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(question => question.Id, StringComparer.Ordinal)
            .ToArray();
    }

    public ContentQuestion? GetQuestion(string id)
    {
        EnsureSeeded();
        using var session = documentStore.QuerySession();
        return session.LoadAsync<ContentQuestion>(id.Trim()).GetAwaiter().GetResult();
    }

    public ContentQuestion? SaveDraft(string id, SaveQuestionDraftRequest request)
    {
        EnsureSeeded();
        using var session = documentStore.LightweightSession();
        var existing = session.LoadAsync<ContentQuestion>(id.Trim()).GetAwaiter().GetResult();
        if (existing?.Status == "Published") return null;

        var nextVersion = existing?.ContentVersion + 1 ?? 1;
        var question = new ContentQuestion(
            id.Trim(),
            request.Type!.Trim(),
            request.Prompt!.Trim(),
            request.AcceptedAnswers!
                .Select(answer => answer.Trim())
                .Where(answer => answer.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            "Draft",
            string.IsNullOrWhiteSpace(request.SourceType) ? "PlatformAuthoredReferenceFixture" : request.SourceType.Trim(),
            string.IsNullOrWhiteSpace(request.SourceVersion) ? "question-reference-v1" : request.SourceVersion.Trim(),
            string.IsNullOrWhiteSpace(request.LicenseRef) ? "platform-authored" : request.LicenseRef.Trim(),
            request.Options?
                .Select(option => option.Trim())
                .Where(option => option.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            request.HskLevel?.Trim(),
            request.Skill?.Trim(),
            request.KnowledgeId?.Trim(),
            request.Explanation?.Trim(),
            request.Difficulty,
            nextVersion);
        session.Store(question);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return question;
    }

    public ContentQuestion? Publish(string id)
    {
        EnsureSeeded();
        using var session = documentStore.LightweightSession();
        var current = session.LoadAsync<ContentQuestion>(id.Trim()).GetAwaiter().GetResult();
        if (current is null) return null;

        var published = current with { Status = "Published" };
        session.Store(published);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return published;
    }

    private void EnsureSeeded()
    {
        using var query = documentStore.QuerySession();
        if (query.Query<ContentQuestion>().ToListAsync().GetAwaiter().GetResult().Count > 0) return;

        using var session = documentStore.LightweightSession();
        foreach (var question in seed.GetAllQuestions()) session.Store(question);
        session.SaveChangesAsync().GetAwaiter().GetResult();
    }
}

/// <summary>
/// Persistent adapter for the approved extended-content fixture. Publish state is stored as
/// documents rather than being reset when the API process restarts.
/// </summary>
public sealed class MartenExtendedContentStore(IDocumentStore documentStore) : IExtendedContentStore
{
    public IReadOnlyList<StoryContent> GetStories(string? hsk, string? topic) =>
        Query<StoryContent>()
            .Where(item => IsPublished(item.Status) && Matches(item.HskLevel, hsk) && Matches(item.Topic, topic))
            .ToArray();

    public StoryContent? GetStory(string id) =>
        Query<StoryContent>().FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && IsPublished(item.Status));

    public IReadOnlyList<VideoContent> GetVideos(string? hsk, string? topic) =>
        Query<VideoContent>()
            .Where(item => IsPublished(item.Status) && Matches(item.HskLevel, hsk) && Matches(item.Topic, topic))
            .ToArray();

    public VideoContent? GetVideo(string id) =>
        Query<VideoContent>().FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && IsPublished(item.Status));

    public IReadOnlyList<LearningResource> GetResources(string? hsk, string? topic) =>
        Query<LearningResource>()
            .Where(item => IsPublished(item.Status) && Matches(item.HskLevel, hsk) && Matches(item.Topic, topic))
            .ToArray();

    public LearningResource? GetResource(string id) =>
        Query<LearningResource>().FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && IsPublished(item.Status));

    public IReadOnlyList<ToolDefinition> GetTools() =>
        Query<ToolDefinition>().Where(item => IsPublished(item.Status)).ToArray();

    public ToolDefinition? GetTool(string id) =>
        Query<ToolDefinition>().FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase) && IsPublished(item.Status));

    public object? GetAdminItems(string kind) => kind.ToLowerInvariant() switch
    {
        "stories" => Query<StoryContent>().ToArray(),
        "videos" => Query<VideoContent>().ToArray(),
        "resources" => Query<LearningResource>().ToArray(),
        "tools" => Query<ToolDefinition>().ToArray(),
        _ => null,
    };

    public object? Publish(string kind, string id) => kind.ToLowerInvariant() switch
    {
        "stories" => PublishDocument<StoryContent>(id),
        "videos" => PublishDocument<VideoContent>(id),
        "resources" => PublishDocument<LearningResource>(id),
        "tools" => PublishDocument<ToolDefinition>(id),
        _ => null,
    };

    private T[] Query<T>() where T : class
    {
        EnsureSeeded<T>();
        using var session = documentStore.QuerySession();
        return session.Query<T>().ToListAsync().GetAwaiter().GetResult().ToArray();
    }

    private T? PublishDocument<T>(string id) where T : class
    {
        EnsureSeeded<T>();
        using var session = documentStore.LightweightSession();
        var current = session.Query<T>().ToListAsync().GetAwaiter().GetResult()
            .FirstOrDefault(item => GetId(item).Equals(id, StringComparison.OrdinalIgnoreCase));
        if (current is null) return null;

        object published = current switch
        {
            StoryContent story => story with { Status = "Published" },
            VideoContent video => video with { Status = "Published" },
            LearningResource resource => resource with { Status = "Published" },
            ToolDefinition tool => tool with { Status = "Published" },
            _ => current,
        };
        session.Store(published);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return (T)published;
    }

    private void EnsureSeeded<T>() where T : class
    {
        using var query = documentStore.QuerySession();
        if (query.Query<T>().ToListAsync().GetAwaiter().GetResult().Count > 0) return;

        var bootstrap = new InMemoryExtendedContentStore().GetAdminItems(GetKind<T>());
        using var session = documentStore.LightweightSession();
        if (bootstrap is IEnumerable<T> items)
        {
            foreach (var item in items) session.Store(item);
            session.SaveChangesAsync().GetAwaiter().GetResult();
        }
    }

    private static string GetKind<T>() where T : class => typeof(T) switch
    {
        var type when type == typeof(StoryContent) => "stories",
        var type when type == typeof(VideoContent) => "videos",
        var type when type == typeof(LearningResource) => "resources",
        _ => "tools",
    };

    private static string GetId<T>(T item) where T : class => item switch
    {
        StoryContent story => story.Id,
        VideoContent video => video.Id,
        LearningResource resource => resource.Id,
        ToolDefinition tool => tool.Id,
        _ => string.Empty,
    };

    private static bool Matches(string value, string? filter) =>
        string.IsNullOrWhiteSpace(filter) || string.Equals(value, filter.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool IsPublished(string status) => string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Persistent audio asset state. The queue remains injected so local tests can use the in-memory
/// seam while a configured Wolverine/RabbitMQ queue receives the same generation contract.
/// </summary>
public sealed class MartenAudioAssetStore(IDocumentStore documentStore, IAudioGenerationQueue queue) : IAudioAssetStore
{
    private const string DefaultVoice = "cosyvoice-v1";

    public IReadOnlyList<AudioAsset> GetAll()
    {
        EnsureSeeded();
        using var session = documentStore.QuerySession();
        return session.Query<AudioAsset>()
            .ToListAsync().GetAwaiter().GetResult()
            .OrderByDescending(asset => asset.UpdatedAt)
            .ThenBy(asset => asset.Id, StringComparer.Ordinal)
            .ToArray();
    }

    public AudioAsset? Get(string id)
    {
        EnsureSeeded();
        using var session = documentStore.QuerySession();
        return session.LoadAsync<AudioAsset>(id.Trim()).GetAwaiter().GetResult();
    }

    public AudioAsset? GetReady(string id) => Get(id) is { Status: AudioAssetStatus.Ready } asset ? asset : null;

    public AudioRequestOutcome Request(RequestAudioGenerationRequest request)
    {
        EnsureSeeded();
        var contentId = request.ContentId!.Trim();
        var text = request.Text!.Trim();
        var voice = string.IsNullOrWhiteSpace(request.Voice) ? DefaultVoice : request.Voice.Trim();
        var idempotencyKey = request.IdempotencyKey!.Trim();

        using (var query = documentStore.QuerySession())
        {
            var existing = query.Query<AudioAsset>()
                .ToListAsync().GetAwaiter().GetResult()
                .FirstOrDefault(asset => asset.IdempotencyKey.Equals(idempotencyKey, StringComparison.Ordinal));
            if (existing is not null)
            {
                var sameRequest = existing.ContentId.Equals(contentId, StringComparison.Ordinal)
                    && existing.Text.Equals(text, StringComparison.Ordinal)
                    && existing.Voice.Equals(voice, StringComparison.Ordinal);
                return new AudioRequestOutcome(existing, false, !sameRequest);
            }
        }

        var now = DateTimeOffset.UtcNow;
        var asset = new AudioAsset(
            $"audio-{Guid.NewGuid():N}",
            contentId,
            text,
            voice,
            AudioAssetStatus.Pending,
            0,
            idempotencyKey,
            null,
            null,
            now,
            now);
        using (var session = documentStore.LightweightSession())
        {
            session.Store(asset);
            session.SaveChangesAsync().GetAwaiter().GetResult();
        }

        queue.Enqueue(new AudioGenerationRequested(
            asset.Id,
            asset.ContentId,
            asset.Text,
            asset.Voice,
            asset.AttemptCount,
            asset.IdempotencyKey,
            now));
        return new AudioRequestOutcome(asset, true, false);
    }

    public AudioRetryOutcome? Retry(string id)
    {
        var current = Get(id);
        if (current is null) return null;
        if (current.Status != AudioAssetStatus.Failed) return new AudioRetryOutcome(current, false, true);

        var now = DateTimeOffset.UtcNow;
        var retried = current with
        {
            Status = AudioAssetStatus.Pending,
            AttemptCount = current.AttemptCount + 1,
            IdempotencyKey = $"{current.Id}:attempt:{current.AttemptCount + 1}",
            AudioUrl = null,
            LastError = null,
            UpdatedAt = now,
        };
        Save(retried);
        queue.Enqueue(new AudioGenerationRequested(
            retried.Id,
            retried.ContentId,
            retried.Text,
            retried.Voice,
            retried.AttemptCount,
            retried.IdempotencyKey,
            now));
        return new AudioRetryOutcome(retried, true, false);
    }

    public AudioAsset? MarkProcessing(string id)
    {
        var current = Get(id);
        if (current is null) return null;
        if (current.Status == AudioAssetStatus.Processing) return current;
        if (current.Status != AudioAssetStatus.Pending) return null;
        var updated = current with { Status = AudioAssetStatus.Processing, UpdatedAt = DateTimeOffset.UtcNow };
        Save(updated);
        return updated;
    }

    public AudioAsset? MarkReady(string id, string audioUrl)
    {
        var current = Get(id);
        if (current is null || string.IsNullOrWhiteSpace(audioUrl)) return null;
        var updated = current with { Status = AudioAssetStatus.Ready, AudioUrl = audioUrl.Trim(), LastError = null, UpdatedAt = DateTimeOffset.UtcNow };
        Save(updated);
        return updated;
    }

    public AudioAsset? MarkFailed(string id, string error)
    {
        var current = Get(id);
        if (current is null || string.IsNullOrWhiteSpace(error)) return null;
        var updated = current with { Status = AudioAssetStatus.Failed, LastError = error.Trim(), UpdatedAt = DateTimeOffset.UtcNow };
        Save(updated);
        return updated;
    }

    private void Save(AudioAsset asset)
    {
        using var session = documentStore.LightweightSession();
        session.Store(asset);
        session.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private void EnsureSeeded()
    {
        using var query = documentStore.QuerySession();
        if (query.Query<AudioAsset>().ToListAsync().GetAwaiter().GetResult().Count > 0) return;

        var bootstrap = new InMemoryAudioAssetStore(new InMemoryAudioGenerationQueue()).GetAll();
        using var session = documentStore.LightweightSession();
        foreach (var asset in bootstrap) session.Store(asset);
        session.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
