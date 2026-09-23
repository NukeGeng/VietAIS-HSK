namespace VietAisHsk.Api.Modules.Curriculum;

public sealed record HanziCharacter(
    string Id,
    string Character,
    string Pinyin,
    string Meaning,
    string Radical,
    int StrokeCount,
    string HskContext,
    IReadOnlyList<string> RelatedWords,
    string Source,
    string SourceVersion,
    string LicenseRef);

public sealed record HanziStroke(
    string Id,
    int Order,
    string Description,
    string Path);

public sealed record HanziStrokeSet(
    string HanziId,
    int StrokeCount,
    IReadOnlyList<HanziStroke> Strokes,
    string Source,
    string SourceVersion,
    string LicenseRef);
