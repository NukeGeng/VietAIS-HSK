using Marten;
using JasperFx.Events;
using Wolverine.Marten;
using VietAisHsk.Api.Modules.Curriculum;
using VietAisHsk.Api.Modules.Content;
using VietAisHsk.Api.Modules.Exam;
using VietAisHsk.Api.Modules.Identity;
using VietAisHsk.Api.Modules.Learning;
using VietAisHsk.Api.Modules.Practice;
using VietAisHsk.Api.Modules.Progress;
using VietAisHsk.Api.Modules.Review;
using VietAisHsk.Api.Modules.Speaking;
using VietAisHsk.Api.Modules.Translation;

namespace VietAisHsk.Api.Infrastructure;

public sealed record IdentityDocument(
    string Id,
    UserAccount Account,
    LearnerProfile Profile,
    PersistedAuthorizationContext Authorization);

public sealed record PersistedAuthorizationContext(
    string UserId,
    HashSet<string> Permissions);

public sealed class MartenIdentityStore(IDocumentStore documentStore) : IIdentityStore
{
    public IdentitySnapshot EnsureUserProvisioned(string userId)
    {
        using var session = documentStore.LightweightSession();
        var existing = session.LoadAsync<IdentityDocument>(userId).GetAwaiter().GetResult();
        if (existing is not null)
        {
            return ToSnapshot(existing);
        }

        var now = DateTimeOffset.UtcNow;
        var document = new IdentityDocument(
            userId,
            new UserAccount(userId, UserStatus.Active, now, now),
            new LearnerProfile(userId, null, null, null, null, "UTC", new StudyPreferences(), now),
            new PersistedAuthorizationContext(userId, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
        session.Store(document);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return ToSnapshot(document);
    }

    public IdentitySnapshot UpdateProfile(string userId, UpdateLearnerProfileRequest request)
    {
        using var session = documentStore.LightweightSession();
        var current = session.LoadAsync<IdentityDocument>(userId).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("User must be provisioned before profile update.");
        var now = DateTimeOffset.UtcNow;
        var updated = current with
        {
            Account = current.Account with { UpdatedAt = now },
            Profile = current.Profile with
            {
                DisplayName = request.DisplayName?.Trim(),
                AvatarUrl = request.AvatarUrl?.Trim(),
                Timezone = request.Timezone?.Trim() ?? current.Profile.Timezone,
                StudyPreferences = request.StudyPreferences ?? current.Profile.StudyPreferences,
                UpdatedAt = now
            }
        };
        session.Store(updated);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return ToSnapshot(updated);
    }

    public IdentitySnapshot SetLearningTarget(string userId, SetLearningTargetRequest request)
    {
        using var session = documentStore.LightweightSession();
        var current = session.LoadAsync<IdentityDocument>(userId).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("User must be provisioned before target update.");
        var now = DateTimeOffset.UtcNow;
        var updated = current with
        {
            Account = current.Account with { UpdatedAt = now },
            Profile = current.Profile with
            {
                PreferredHskLevelId = request.PreferredHskLevelId?.Trim(),
                TargetHskLevelId = request.TargetHskLevelId?.Trim(),
                UpdatedAt = now
            }
        };
        session.Store(updated);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return ToSnapshot(updated);
    }

    public IReadOnlySet<string> GetPermissions(string userId) =>
        EnsureUserProvisioned(userId).Authorization.Permissions;

    public IReadOnlyList<UserListItem> SearchUsers(string? search)
    {
        using var session = documentStore.QuerySession();
        var normalized = search?.Trim();
        var query = session.Query<IdentityDocument>();
        var documents = string.IsNullOrWhiteSpace(normalized)
            ? query.ToListAsync().GetAwaiter().GetResult()
            : query.Where(document => document.Id.Contains(normalized) || (document.Profile.DisplayName != null && document.Profile.DisplayName.Contains(normalized)))
                .ToListAsync().GetAwaiter().GetResult();
        return documents.OrderBy(document => document.Id, StringComparer.Ordinal).Select(ToListItem).ToArray();
    }

    public UserListItem? GetUser(string userId)
    {
        using var session = documentStore.QuerySession();
        var document = session.LoadAsync<IdentityDocument>(userId).GetAwaiter().GetResult();
        return document is null ? null : ToListItem(document);
    }

    public UserListItem? ChangeStatus(string userId, UserStatus status)
    {
        using var session = documentStore.LightweightSession();
        var current = session.LoadAsync<IdentityDocument>(userId).GetAwaiter().GetResult();
        if (current is null)
        {
            return null;
        }

        var updated = current with { Account = current.Account with { Status = status, UpdatedAt = DateTimeOffset.UtcNow } };
        session.Store(updated);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return ToListItem(updated);
    }

    private static IdentitySnapshot ToSnapshot(IdentityDocument document) => new(
        document.Account,
        document.Profile,
        new AuthorizationContext(document.Authorization.UserId, document.Authorization.Permissions));

    private static UserListItem ToListItem(IdentityDocument document) => new(
        document.Account.UserId,
        document.Account.Status,
        document.Profile.DisplayName,
        document.Profile.TargetHskLevelId,
        document.Account.UpdatedAt);
}

public static class MartenPersistenceExtensions
{
    public static IServiceCollection AddVietAisMarten(
        this IServiceCollection services,
        string? connectionString,
        bool integrateWithWolverine = false)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        var marten = services.AddMarten(options =>
        {
            options.Connection(connectionString);
            options.DatabaseSchemaName = "vietais_hsk";
            options.AutoCreateSchemaObjects = JasperFx.AutoCreate.CreateOrUpdate;
            options.Events.StreamIdentity = StreamIdentity.AsString;
            options.Schema.For<IdentityDocument>().Identity(x => x.Id);
            options.Schema.For<CurriculumDocument>().Identity(x => x.Id);
            options.Schema.For<PracticeSession>().Identity(x => x.Id);
            options.Schema.For<HanziWritingAttempt>().Identity(x => x.Id);
            options.Schema.For<ProgressProjectionDocument>().Identity(x => x.Id);
            options.Schema.For<ReviewItem>().Identity(x => x.Id);
            options.Schema.For<ReviewSession>().Identity(x => x.Id);
            options.Schema.For<ExamAttemptMetadata>().Identity(x => x.Id);
            options.Schema.For<TranslationAttempt>().Identity(x => x.Id);
            options.Schema.For<SpeakingSession>().Identity(x => x.Id);
            options.Schema.For<ContentQuestion>().Identity(x => x.Id);
            options.Schema.For<StoryContent>().Identity(x => x.Id);
            options.Schema.For<VideoContent>().Identity(x => x.Id);
            options.Schema.For<LearningResource>().Identity(x => x.Id);
            options.Schema.For<ToolDefinition>().Identity(x => x.Id);
            options.Schema.For<AudioAsset>().Identity(x => x.Id);
        });

        if (integrateWithWolverine)
        {
            marten.IntegrateWithWolverine();
        }

        services.AddScoped<IIdentityStore, MartenIdentityStore>();
        services.AddScoped<ICurriculumStore, MartenCurriculumStore>();
        services.AddScoped<ILearningStore, MartenLearningStore>();
        services.AddScoped<IPracticeStore, MartenPracticeStore>();
        services.AddScoped<IHanziWritingStore, MartenHanziWritingStore>();
        services.AddScoped<IProgressStore, MartenProgressStore>();
        services.AddScoped<IProgressSignalSink>(provider => provider.GetRequiredService<IProgressStore>());
        services.AddScoped<IReviewStore, MartenReviewStore>();
        services.AddScoped<IReviewSignalSink>(provider => provider.GetRequiredService<IReviewStore>());
        services.AddScoped<IExamStore, MartenExamStore>();
        services.AddScoped<ITranslationStore, MartenTranslationStore>();
        services.AddScoped<ISpeakingStore, MartenSpeakingStore>();
        services.AddScoped<IQuestionBank, MartenQuestionBank>();
        services.AddScoped<IQuestionBankAdmin, MartenQuestionBank>();
        services.AddScoped<IExtendedContentStore, MartenExtendedContentStore>();
        services.AddScoped<IAudioAssetStore, MartenAudioAssetStore>();
        return services;
    }
}
