using System.Collections.Concurrent;

namespace VietAisHsk.Api.Modules.Curriculum;

public interface ICurriculumStore
{
    IReadOnlyList<HskLevel> GetPublishedLevels();
    HskLevelTree? GetPublishedTree(string level);
    BeginnerTrack GetBeginnerTrack();
    FoundationCatalog GetPinyinCatalog();
    FoundationCatalog GetToneCatalog();
    bool IsPublishedLesson(string lessonId);
    CurriculumImportResult Import(CurriculumImportRequest request);
    HskLevel? PublishLevel(string levelId);
}

public sealed class InMemoryCurriculumStore : ICurriculumStore
{
    private readonly ConcurrentDictionary<string, SyllabusVersion> syllabusVersions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, HskLevel> levels = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<HskLevel> GetPublishedLevels()
    {
        var publishedVersions = syllabusVersions.Values
            .Where(version => version.Status == ContentStatus.Published)
            .Select(version => version.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return levels.Values
            .Where(level => level.Status == ContentStatus.Published && publishedVersions.Contains(level.SyllabusVersionId))
            .OrderBy(level => level.LevelNumber)
            .ToArray();
    }

    public HskLevelTree? GetPublishedTree(string level)
    {
        var published = GetPublishedLevels().FirstOrDefault(item =>
            string.Equals(item.Id, level, StringComparison.OrdinalIgnoreCase)
            || item.LevelNumber.ToString() == level);

        return published is null ? null : new HskLevelTree(published, Array.Empty<TopicNode>());
    }

    public BeginnerTrack GetBeginnerTrack()
    {
        var stages = new[]
        {
            new BeginnerStage("pinyin", 1, "Pinyin", ContentStatus.Published),
            new BeginnerStage("tones", 2, "Thanh điệu", ContentStatus.Published),
            new BeginnerStage("initials-finals", 3, "Âm đầu và vần", ContentStatus.Published),
            new BeginnerStage("syllable-blending", 4, "Ghép âm", ContentStatus.Published),
            new BeginnerStage("basic-hanzi", 5, "Chữ Hán cơ bản", ContentStatus.Published),
            new BeginnerStage("basic-strokes", 6, "Nét cơ bản", ContentStatus.Published),
            new BeginnerStage("familiarization", 7, "Bài làm quen", ContentStatus.Published)
        };
        return new BeginnerTrack("beginner-foundation", "Người mới bắt đầu", stages);
    }

    public FoundationCatalog GetPinyinCatalog() => new("Pinyin", Array.Empty<FoundationItem>());

    public FoundationCatalog GetToneCatalog() => new("Thanh điệu", Array.Empty<FoundationItem>());

    public bool IsPublishedLesson(string lessonId) => false;

    public CurriculumImportResult Import(CurriculumImportRequest request)
    {
        var versionId = request.SyllabusVersionId!.Trim();
        var now = DateTimeOffset.UtcNow;
        var wasAlreadyPresent = syllabusVersions.ContainsKey(versionId)
            && request.Levels!.All(level => levels.ContainsKey(level.Id!.Trim()));

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
