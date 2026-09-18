namespace VietAisHsk.Api.Modules.Curriculum;

public enum ContentStatus
{
    Draft,
    Published,
    Archived
}

public sealed record SyllabusVersion(
    string Id,
    string Name,
    string SourceType,
    ContentStatus Status,
    DateTimeOffset UpdatedAt);

public sealed record HskLevel(
    string Id,
    string SyllabusVersionId,
    int LevelNumber,
    string DisplayName,
    ContentStatus Status,
    DateTimeOffset UpdatedAt);

public sealed record HskLevelTree(
    HskLevel Level,
    IReadOnlyList<TopicNode> Topics);

public sealed record TopicNode(
    string Id,
    string Name,
    IReadOnlyList<UnitNode> Units);

public sealed record UnitNode(
    string Id,
    string Name,
    IReadOnlyList<LessonNode> Lessons);

public sealed record LessonNode(
    string Id,
    string Name,
    ContentStatus Status);

public sealed record BeginnerTrack(
    string Id,
    string Name,
    IReadOnlyList<BeginnerStage> Stages);

public sealed record BeginnerStage(
    string Id,
    int Order,
    string Name,
    ContentStatus Status);

public sealed record FoundationCatalog(
    string Name,
    IReadOnlyList<FoundationItem> Items);

public sealed record FoundationItem(
    string Id,
    string Label,
    string? Pinyin,
    int? Tone);

public sealed record CurriculumImportRequest(
    string? SyllabusVersionId,
    string? SyllabusName,
    string? SourceType,
    IReadOnlyList<HskLevelImportItem>? Levels);

public sealed record HskLevelImportItem(
    string? Id,
    int LevelNumber,
    string? DisplayName);

public sealed record CurriculumImportResult(
    string SyllabusVersionId,
    int LevelsUpserted,
    bool Idempotent);
