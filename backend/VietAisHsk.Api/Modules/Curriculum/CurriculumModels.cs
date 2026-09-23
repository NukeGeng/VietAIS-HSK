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

public sealed record HskLessonDetail(
    string Id,
    string Name,
    ContentStatus Status,
    HskLevel Level,
    string TopicName,
    string UnitName);

public sealed record BeginnerTrack(
    string Id,
    string Name,
    IReadOnlyList<BeginnerStage> Stages);

public sealed record BeginnerStage(
    string Id,
    int Order,
    string Name,
    ContentStatus Status,
    IReadOnlyList<string>? MasterDataRefs = null);

public sealed record FoundationCatalog(
    string Name,
    string SourceType,
    string Version,
    string EditorialNote,
    IReadOnlyList<FoundationSource> Sources,
    IReadOnlyList<FoundationItem> Items);

public sealed record FoundationSource(
    string Name,
    string Url,
    string Scope);

public sealed record FoundationItem(
    string Id,
    string Category,
    string Label,
    string Description,
    string? Pinyin,
    int? Tone,
    string? Example,
    string? ExampleMeaning);

public sealed record CurriculumImportRequest(
    string? SyllabusVersionId,
    string? SyllabusName,
    string? SourceType,
    IReadOnlyList<HskLevelImportItem>? Levels);

public sealed record HskLevelImportItem(
    string? Id,
    int LevelNumber,
    string? DisplayName,
    IReadOnlyList<TopicImportItem>? Topics = null);

public sealed record TopicImportItem(
    string? Id,
    string? Name,
    IReadOnlyList<UnitImportItem>? Units = null);

public sealed record UnitImportItem(
    string? Id,
    string? Name,
    IReadOnlyList<LessonImportItem>? Lessons = null);

public sealed record LessonImportItem(
    string? Id,
    string? Name);

public sealed class PublishedCurriculumVersionException(string versionId)
    : InvalidOperationException($"Syllabus version '{versionId}' đã publish; cần tạo version mới để thay đổi nội dung.");

public sealed class CurriculumLevelIdConflictException(string levelId, string existingVersionId, string incomingVersionId)
    : InvalidOperationException(
        $"HSK level '{levelId}' đã thuộc syllabus version '{existingVersionId}'; không thể dùng lại Id này cho version '{incomingVersionId}'. Hãy dùng Id level mới theo version.");

public sealed record CurriculumImportResult(
    string SyllabusVersionId,
    int LevelsUpserted,
    bool Idempotent);
