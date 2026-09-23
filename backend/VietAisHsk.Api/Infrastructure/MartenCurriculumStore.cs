using Marten;
using VietAisHsk.Api.Modules.Curriculum;

namespace VietAisHsk.Api.Infrastructure;

public sealed record CurriculumDocument(
    string Id,
    List<SyllabusVersion> SyllabusVersions,
    List<HskLevel> Levels)
{
    public List<CurriculumLevelTree> Trees { get; init; } = [];
}

public sealed record CurriculumLevelTree(string LevelId, List<TopicNode> Topics);

public sealed class MartenCurriculumStore(IDocumentStore documentStore) : ICurriculumStore
{
    private const string RootId = "curriculum-root";

    public IReadOnlyList<HskLevel> GetAdminLevels() => Read().Levels
        .OrderBy(level => level.LevelNumber)
        .ThenByDescending(level => level.UpdatedAt)
        .ThenBy(level => level.Id, StringComparer.Ordinal)
        .ToArray();

    public IReadOnlyList<HskLevel> GetPublishedLevels()
    {
        var document = Read();
        var publishedVersions = document.SyllabusVersions
            .Where(version => version.Status == ContentStatus.Published)
            .Select(version => version.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return document.Levels
            .Where(level => level.Status == ContentStatus.Published && publishedVersions.Contains(level.SyllabusVersionId))
            .GroupBy(level => level.LevelNumber)
            .Select(group => group
                .OrderByDescending(level => level.UpdatedAt)
                .ThenByDescending(level => level.Id, StringComparer.Ordinal)
                .First())
            .OrderBy(level => level.LevelNumber)
            .ToArray();
    }

    public HskLevelTree? GetPublishedTree(string level)
    {
        var document = Read();
        var publishedVersionIds = document.SyllabusVersions
            .Where(version => version.Status == ContentStatus.Published)
            .Select(version => version.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var published = document.Levels.FirstOrDefault(item =>
            item.Status == ContentStatus.Published
            && publishedVersionIds.Contains(item.SyllabusVersionId)
            && (string.Equals(item.Id, level, StringComparison.OrdinalIgnoreCase)
                || item.LevelNumber.ToString() == level));
        if (published is null)
        {
            return null;
        }

        var storedTopics = document.Trees.FirstOrDefault(tree =>
            string.Equals(tree.LevelId, published.Id, StringComparison.OrdinalIgnoreCase))?.Topics ?? [];
        return new HskLevelTree(published, CurriculumTreeOperations.PublishedOnly(storedTopics));
    }

    public BeginnerTrack GetBeginnerTrack() => new InMemoryCurriculumStore().GetBeginnerTrack();

    public FoundationCatalog GetPinyinCatalog() => FoundationCatalogData.Pinyin;

    public FoundationCatalog GetToneCatalog() => FoundationCatalogData.Tones;

    public bool IsPublishedLesson(string lessonId)
    {
        var document = Read();
        var publishedVersionIds = document.SyllabusVersions
            .Where(version => version.Status == ContentStatus.Published)
            .Select(version => version.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var level in document.Levels.Where(level =>
                     level.Status == ContentStatus.Published && publishedVersionIds.Contains(level.SyllabusVersionId)))
        {
            var topics = document.Trees.FirstOrDefault(tree =>
                string.Equals(tree.LevelId, level.Id, StringComparison.OrdinalIgnoreCase))?.Topics;
            if (topics is not null && CurriculumTreeOperations.ContainsPublishedLesson(topics, lessonId))
            {
                return true;
            }
        }

        return false;
    }

    public LessonNode? GetPublishedLesson(string lessonId) => GetPublishedLessonDetail(lessonId) is { } detail
        ? new LessonNode(detail.Id, detail.Name, detail.Status)
        : null;

    public HskLessonDetail? GetPublishedLessonDetail(string lessonId)
    {
        var document = Read();
        var publishedVersionIds = document.SyllabusVersions
            .Where(version => version.Status == ContentStatus.Published)
            .Select(version => version.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var level in document.Levels.Where(level =>
                     level.Status == ContentStatus.Published && publishedVersionIds.Contains(level.SyllabusVersionId)))
        {
            var topics = document.Trees.FirstOrDefault(tree =>
                string.Equals(tree.LevelId, level.Id, StringComparison.OrdinalIgnoreCase))?.Topics;
            var lesson = topics?
                .SelectMany(topic => topic.Units)
                .SelectMany(unit => unit.Lessons)
                .FirstOrDefault(candidate => candidate.Status == ContentStatus.Published
                    && candidate.Id.Equals(lessonId, StringComparison.OrdinalIgnoreCase));
            if (lesson is not null)
            {
                var topic = topics!
                    .First(candidate => candidate.Units.Any(unit => unit.Lessons.Any(item =>
                        item.Status == ContentStatus.Published
                        && item.Id.Equals(lessonId, StringComparison.OrdinalIgnoreCase))));
                var unit = topic.Units.First(candidate => candidate.Lessons.Any(item =>
                    item.Status == ContentStatus.Published
                    && item.Id.Equals(lessonId, StringComparison.OrdinalIgnoreCase)));
                return new HskLessonDetail(lesson.Id, lesson.Name, lesson.Status, level, topic.Name, unit.Name);
            }
        }

        return null;
    }

    public LessonNode? PublishLesson(string levelId, string lessonId)
    {
        var document = Read();
        var level = document.Levels.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, levelId, StringComparison.OrdinalIgnoreCase));
        if (level is null || level.Status != ContentStatus.Published
            || !document.SyllabusVersions.Any(version =>
                string.Equals(version.Id, level.SyllabusVersionId, StringComparison.OrdinalIgnoreCase)
                && version.Status == ContentStatus.Published))
        {
            return null;
        }

        var treeIndex = document.Trees.FindIndex(tree =>
            string.Equals(tree.LevelId, level.Id, StringComparison.OrdinalIgnoreCase));
        if (treeIndex < 0
            || !CurriculumTreeOperations.TryPublishLesson(
                document.Trees[treeIndex].Topics,
                lessonId,
                out var updatedTopics,
                out var publishedLesson))
        {
            return null;
        }

        document.Trees[treeIndex] = document.Trees[treeIndex] with { Topics = updatedTopics };
        Write(document);
        return publishedLesson;
    }

    public CurriculumImportResult Import(CurriculumImportRequest request)
    {
        var document = Read();
        var versionId = request.SyllabusVersionId!.Trim();
        var now = DateTimeOffset.UtcNow;
        var versionIndex = document.SyllabusVersions.FindIndex(version =>
            string.Equals(version.Id, versionId, StringComparison.OrdinalIgnoreCase));
        var existingVersion = versionIndex >= 0 ? document.SyllabusVersions[versionIndex] : null;

        var conflictingLevel = request.Levels!
            .Select(item => (Item: item, Existing: document.Levels.FirstOrDefault(level =>
                string.Equals(level.Id, item.Id!.Trim(), StringComparison.OrdinalIgnoreCase))))
            .FirstOrDefault(pair => pair.Existing is not null
                && !string.Equals(pair.Existing.SyllabusVersionId, versionId, StringComparison.OrdinalIgnoreCase));
        if (conflictingLevel.Existing is not null)
        {
            throw new CurriculumLevelIdConflictException(
                conflictingLevel.Item.Id!.Trim(),
                conflictingLevel.Existing.SyllabusVersionId,
                versionId);
        }

        var wasAlreadyPresent = existingVersion is not null
            && request.Levels!.All(item => document.Levels.Any(level =>
                string.Equals(level.Id, item.Id!.Trim(), StringComparison.OrdinalIgnoreCase)));

        if (existingVersion?.Status == ContentStatus.Published)
        {
            var identical = string.Equals(existingVersion.Name, request.SyllabusName!.Trim(), StringComparison.Ordinal)
                && string.Equals(existingVersion.SourceType, request.SourceType!.Trim(), StringComparison.Ordinal)
                && request.Levels!.All(item =>
                {
                    var existingLevel = document.Levels.FirstOrDefault(level =>
                        string.Equals(level.Id, item.Id!.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (existingLevel is null)
                    {
                        return false;
                    }

                    var existingTopics = document.Trees.FirstOrDefault(tree =>
                        string.Equals(tree.LevelId, existingLevel.Id, StringComparison.OrdinalIgnoreCase))?.Topics ?? [];
                    return CurriculumTreeOperations.MatchesImport(item, existingLevel, existingTopics, versionId);
                });
            if (identical)
            {
                return new CurriculumImportResult(versionId, request.Levels!.Count, true);
            }

            throw new PublishedCurriculumVersionException(versionId);
        }

        var version = new SyllabusVersion(versionId, request.SyllabusName!.Trim(), request.SourceType!.Trim(), ContentStatus.Draft, now);
        if (versionIndex >= 0)
        {
            var current = document.SyllabusVersions[versionIndex];
            document.SyllabusVersions[versionIndex] = current with { Name = version.Name, SourceType = version.SourceType, UpdatedAt = now };
        }
        else
        {
            document.SyllabusVersions.Add(version);
        }

        foreach (var item in request.Levels!)
        {
            var levelId = item.Id!.Trim();
            var levelIndex = document.Levels.FindIndex(level => string.Equals(level.Id, levelId, StringComparison.OrdinalIgnoreCase));
            var level = new HskLevel(levelId, versionId, item.LevelNumber, item.DisplayName!.Trim(), ContentStatus.Draft, now);
            if (levelIndex >= 0)
            {
                var current = document.Levels[levelIndex];
                document.Levels[levelIndex] = level with { Status = current.Status };
            }
            else
            {
                document.Levels.Add(level);
            }

            var treeIndex = document.Trees.FindIndex(tree => string.Equals(tree.LevelId, levelId, StringComparison.OrdinalIgnoreCase));
            var tree = new CurriculumLevelTree(levelId, CurriculumTreeOperations.ToDraftTopics(item));
            if (treeIndex >= 0)
            {
                document.Trees[treeIndex] = tree;
            }
            else
            {
                document.Trees.Add(tree);
            }
        }

        Write(document);
        return new CurriculumImportResult(versionId, request.Levels.Count, wasAlreadyPresent);
    }

    public HskLevel? PublishLevel(string levelId)
    {
        var document = Read();
        var index = document.Levels.FindIndex(level => string.Equals(level.Id, levelId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var published = document.Levels[index] with { Status = ContentStatus.Published, UpdatedAt = now };
        document.Levels[index] = published;
        var versionIndex = document.SyllabusVersions.FindIndex(version =>
            string.Equals(version.Id, published.SyllabusVersionId, StringComparison.OrdinalIgnoreCase));
        if (versionIndex < 0)
        {
            return null;
        }
        document.SyllabusVersions[versionIndex] = document.SyllabusVersions[versionIndex] with { Status = ContentStatus.Published, UpdatedAt = now };
        Write(document);
        return published;
    }

    private CurriculumDocument Read()
    {
        using var session = documentStore.QuerySession();
        return session.LoadAsync<CurriculumDocument>(RootId).GetAwaiter().GetResult()
            ?? new CurriculumDocument(RootId, [], []);
    }

    private void Write(CurriculumDocument document)
    {
        using var session = documentStore.LightweightSession();
        session.Store(document);
        session.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
