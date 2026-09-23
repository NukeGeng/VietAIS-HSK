namespace VietAisHsk.Api.Modules.Shared;

public sealed record TranslationAttemptSignal(
    string UserId,
    string AttemptId,
    string ExerciseId,
    DateTimeOffset SubmittedAt,
    string? EventId = null);
