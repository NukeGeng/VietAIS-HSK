namespace VietAisHsk.Api.Modules.Practice;

public enum PracticeSessionStatus
{
    Active,
    Completed
}

public enum PracticeResult
{
    Correct,
    Incorrect,
    NeedsRetry
}

public sealed record PracticeQuestion(
    string Id,
    string Type,
    string Prompt,
    IReadOnlyList<string> AcceptedAnswers,
    string Status = "Published",
    IReadOnlyList<string>? Options = null,
    int ContentVersion = 1);

public sealed record StartPracticeSessionRequest(IReadOnlyList<string>? QuestionIds);

public sealed record SubmitPracticeAnswerRequest(string? QuestionId, string? Answer);

public sealed record QuestionAttempt(
    string QuestionId,
    string Answer,
    PracticeResult Result,
    DateTimeOffset SubmittedAt);

public sealed record PracticeSession(
    string Id,
    string UserId,
    IReadOnlyList<PracticeQuestion> Questions,
    IReadOnlyList<QuestionAttempt> Attempts,
    PracticeSessionStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PracticeSessionResult(
    string SessionId,
    PracticeSessionStatus Status,
    int Correct,
    int Incorrect,
    int NeedsRetry,
    int Answered,
    int Total);

public sealed record PracticeQuestionView(
    string Id,
    string Type,
    string Prompt,
    string Status,
    IReadOnlyList<string>? Options = null,
    int ContentVersion = 1);

public sealed record PracticeSessionView(
    string Id,
    PracticeSessionStatus Status,
    IReadOnlyList<PracticeQuestionView> Questions,
    IReadOnlyList<QuestionAttempt> Attempts,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
