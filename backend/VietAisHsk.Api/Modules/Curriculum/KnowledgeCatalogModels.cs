namespace VietAisHsk.Api.Modules.Curriculum;

public sealed record VocabularyExample(
    string Chinese,
    string Pinyin,
    string Vietnamese);

public sealed record VocabularyEntry(
    string Id,
    string Simplified,
    string Pinyin,
    string Meaning,
    string PartOfSpeech,
    string HskLevel,
    string Topic,
    IReadOnlyList<string> RelatedHanzi,
    IReadOnlyList<VocabularyExample> Examples,
    string Status,
    string SourceType,
    string SourceVersion,
    string LicenseRef);

public sealed record GrammarExample(
    string Chinese,
    string Pinyin,
    string Vietnamese);

public sealed record GrammarPoint(
    string Id,
    string Pattern,
    string Title,
    string Explanation,
    string HskLevel,
    string Topic,
    IReadOnlyList<string> CommonMistakes,
    IReadOnlyList<GrammarExample> Examples,
    string? RelatedLessonId,
    string Status,
    string SourceType,
    string SourceVersion,
    string LicenseRef);
