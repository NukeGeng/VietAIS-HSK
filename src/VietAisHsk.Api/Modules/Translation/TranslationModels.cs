namespace VietAisHsk.Api.Modules.Translation;

public sealed record TranslationExercise(
    string Id,
    string PromptVietnamese,
    string ReferenceChinese,
    string HskContext,
    string Status = "Published");

public sealed record TranslationAttempt(
    string Id,
    string UserId,
    string ExerciseId,
    string AnswerChinese,
    string ReferenceChinese,
    DateTimeOffset SubmittedAt,
    TranslationFeedback? Feedback);

public sealed record SubmitTranslationAttemptRequest(string? AnswerChinese);

public sealed record TranslationFeedback(
    string Meaning,
    string Grammar,
    string WordChoice,
    string Naturalness,
    string SuggestedRevision,
    string Status);
