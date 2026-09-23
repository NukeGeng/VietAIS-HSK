using System.Collections.Concurrent;

namespace VietAisHsk.Api.Modules.Curriculum;

public interface ICurriculumStore
{
    IReadOnlyList<HskLevel> GetAdminLevels();
    IReadOnlyList<HskLevel> GetPublishedLevels();
    HskLevelTree? GetPublishedTree(string level);
    LessonNode? GetPublishedLesson(string lessonId);
    HskLessonDetail? GetPublishedLessonDetail(string lessonId);
    BeginnerTrack GetBeginnerTrack();
    FoundationCatalog GetPinyinCatalog();
    FoundationCatalog GetToneCatalog();
    bool IsPublishedLesson(string lessonId);
    CurriculumImportResult Import(CurriculumImportRequest request);
    HskLevel? PublishLevel(string levelId);
    LessonNode? PublishLesson(string levelId, string lessonId);
}

public interface IHanziCatalog
{
    IReadOnlyList<HanziCharacter> GetAll();
    HanziCharacter? Get(string id);
    HanziStrokeSet? GetStrokes(string id);
}

public interface IVocabularyCatalog
{
    IReadOnlyList<VocabularyEntry> SearchVocabulary(string? search, string? hskLevel, string? topic);
    VocabularyEntry? GetVocabulary(string id);
}

public interface IGrammarCatalog
{
    IReadOnlyList<GrammarPoint> SearchGrammar(string? search, string? hskLevel, string? topic);
    GrammarPoint? GetGrammar(string id);
}

public sealed class InMemoryCurriculumStore : ICurriculumStore
{
    private readonly ConcurrentDictionary<string, SyllabusVersion> syllabusVersions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, HskLevel> levels = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IReadOnlyList<TopicNode>> topicsByLevel = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<HskLevel> GetAdminLevels() => levels.Values
        .OrderBy(level => level.LevelNumber)
        .ThenByDescending(level => level.UpdatedAt)
        .ThenBy(level => level.Id, StringComparer.Ordinal)
        .ToArray();

    public IReadOnlyList<HskLevel> GetPublishedLevels()
    {
        var publishedVersions = syllabusVersions.Values
            .Where(version => version.Status == ContentStatus.Published)
            .Select(version => version.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return levels.Values
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
        var published = GetPublishedLevels().FirstOrDefault(item =>
            string.Equals(item.Id, level, StringComparison.OrdinalIgnoreCase)
            || item.LevelNumber.ToString() == level);

        if (published is null)
        {
            return null;
        }

        var topics = topicsByLevel.TryGetValue(published.Id, out var storedTopics)
            ? CurriculumTreeOperations.PublishedOnly(storedTopics)
            : Array.Empty<TopicNode>();
        return new HskLevelTree(published, topics);
    }

    public BeginnerTrack GetBeginnerTrack()
    {
        var stages = new[]
        {
            new BeginnerStage("pinyin", 1, "Pinyin", ContentStatus.Published, ["foundation:pinyin"]),
            new BeginnerStage("tones", 2, "Thanh điệu", ContentStatus.Published, ["foundation:tones"]),
            new BeginnerStage("initials-finals", 3, "Âm đầu và vần", ContentStatus.Draft, ["foundation:pinyin.initials", "foundation:pinyin.finals"]),
            new BeginnerStage("syllable-blending", 4, "Ghép âm", ContentStatus.Draft, ["foundation:pinyin.syllables"]),
            new BeginnerStage("basic-hanzi", 5, "Chữ Hán cơ bản", ContentStatus.Draft, ["hanzi:catalog"]),
            new BeginnerStage("basic-strokes", 6, "Nét cơ bản", ContentStatus.Draft, ["hanzi:strokes"]),
            new BeginnerStage("familiarization", 7, "Bài làm quen", ContentStatus.Draft, ["foundation:pinyin", "hanzi:catalog"])
        };
        return new BeginnerTrack("beginner-foundation", "Người mới bắt đầu", stages);
    }

    public FoundationCatalog GetPinyinCatalog() => FoundationCatalogData.Pinyin;

    public FoundationCatalog GetToneCatalog() => FoundationCatalogData.Tones;

    public bool IsPublishedLesson(string lessonId) => GetPublishedLevels().Any(level =>
        topicsByLevel.TryGetValue(level.Id, out var topics)
        && CurriculumTreeOperations.ContainsPublishedLesson(topics, lessonId));

    public LessonNode? GetPublishedLesson(string lessonId) => GetPublishedLessonDetail(lessonId) is { } detail
        ? new LessonNode(detail.Id, detail.Name, detail.Status)
        : null;

    public HskLessonDetail? GetPublishedLessonDetail(string lessonId)
    {
        foreach (var level in GetPublishedLevels())
        {
            if (!topicsByLevel.TryGetValue(level.Id, out var topics))
            {
                continue;
            }

            foreach (var topic in topics)
            {
                foreach (var unit in topic.Units)
                {
                    var lesson = unit.Lessons.FirstOrDefault(candidate =>
                        candidate.Status == ContentStatus.Published
                        && candidate.Id.Equals(lessonId, StringComparison.OrdinalIgnoreCase));
                    if (lesson is not null)
                    {
                        return new HskLessonDetail(lesson.Id, lesson.Name, lesson.Status, level, topic.Name, unit.Name);
                    }
                }
            }
        }

        return null;
    }

    public LessonNode? PublishLesson(string levelId, string lessonId)
    {
        if (!levels.TryGetValue(levelId, out var level)
            || level.Status != ContentStatus.Published
            || !syllabusVersions.TryGetValue(level.SyllabusVersionId, out var version)
            || version.Status != ContentStatus.Published
            || !topicsByLevel.TryGetValue(level.Id, out var topics)
            || !CurriculumTreeOperations.TryPublishLesson(topics, lessonId, out var updatedTopics, out var publishedLesson))
        {
            return null;
        }

        topicsByLevel[level.Id] = updatedTopics;
        return publishedLesson;
    }

    public CurriculumImportResult Import(CurriculumImportRequest request)
    {
        var versionId = request.SyllabusVersionId!.Trim();
        var now = DateTimeOffset.UtcNow;
        var versionExists = syllabusVersions.TryGetValue(versionId, out var existingVersion);

        var conflictingLevel = request.Levels!
            .Select(item => (Item: item, Existing: levels.TryGetValue(item.Id!.Trim(), out var existing) ? existing : null))
            .FirstOrDefault(pair => pair.Existing is not null
                && !string.Equals(pair.Existing.SyllabusVersionId, versionId, StringComparison.OrdinalIgnoreCase));
        if (conflictingLevel.Existing is not null)
        {
            throw new CurriculumLevelIdConflictException(
                conflictingLevel.Item.Id!.Trim(),
                conflictingLevel.Existing.SyllabusVersionId,
                versionId);
        }

        var wasAlreadyPresent = versionExists
            && request.Levels!.All(level => levels.ContainsKey(level.Id!.Trim()));

        if (existingVersion?.Status == ContentStatus.Published)
        {
            var identical = string.Equals(existingVersion.Name, request.SyllabusName!.Trim(), StringComparison.Ordinal)
                && string.Equals(existingVersion.SourceType, request.SourceType!.Trim(), StringComparison.Ordinal)
                && request.Levels!.All(item => levels.TryGetValue(item.Id!.Trim(), out var existingLevel)
                    && CurriculumTreeOperations.MatchesImport(
                        item,
                        existingLevel,
                        topicsByLevel.TryGetValue(existingLevel.Id, out var storedTopics) ? storedTopics : Array.Empty<TopicNode>(),
                        versionId));
            if (identical)
            {
                return new CurriculumImportResult(versionId, request.Levels!.Count, true);
            }

            throw new PublishedCurriculumVersionException(versionId);
        }

        syllabusVersions.AddOrUpdate(
            versionId,
            _ => new SyllabusVersion(versionId, request.SyllabusName!.Trim(), request.SourceType!.Trim(), ContentStatus.Draft, now),
            (_, current) => current with
            {
                Name = request.SyllabusName!.Trim(),
                SourceType = request.SourceType!.Trim(),
                UpdatedAt = now
            });

        foreach (var item in request.Levels!)
        {
            var levelId = item.Id!.Trim();
            levels.AddOrUpdate(
                levelId,
                _ => new HskLevel(levelId, versionId, item.LevelNumber, item.DisplayName!.Trim(), ContentStatus.Draft, now),
                (_, current) => current with
                {
                    SyllabusVersionId = versionId,
                    LevelNumber = item.LevelNumber,
                    DisplayName = item.DisplayName!.Trim(),
                    UpdatedAt = now
                });
            topicsByLevel[levelId] = CurriculumTreeOperations.ToDraftTopics(item);
        }

        return new CurriculumImportResult(versionId, request.Levels.Count, wasAlreadyPresent);
    }

    public HskLevel? PublishLevel(string levelId)
    {
        if (!levels.TryGetValue(levelId, out var current))
        {
            return null;
        }

        if (!syllabusVersions.TryGetValue(current.SyllabusVersionId, out var version))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        levels[levelId] = current with { Status = ContentStatus.Published, UpdatedAt = now };
        syllabusVersions[version.Id] = version with { Status = ContentStatus.Published, UpdatedAt = now };
        return levels[levelId];
    }
}
