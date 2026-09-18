namespace VietAisHsk.Api.Modules.Exam;

public enum ExamAttemptStatus
{
    Active,
    Submitted,
    Scored
}

public sealed record ExamQuestion(
    string Id,
    string Prompt,
    IReadOnlyList<string> AcceptedAnswers);

public sealed record ExamDefinition(
    string Id,
    string Name,
    string HskLevel,
    string ContentVersion,
    IReadOnlyList<ExamQuestion> Questions);

public sealed record ExamEvent(
    string Type,
    DateTimeOffset OccurredAt,
    IReadOnlyList<string>? QuestionIds = null,
    string? QuestionId = null,
    string? Answer = null,
    int? Score = null);

public sealed record ExamAttempt(
    string Id,
    string UserId,
    string ExamId,
    string ContentVersion,
    ExamAttemptStatus Status,
    IReadOnlyList<ExamQuestion> Questions,
    IReadOnlyDictionary<string, string> Answers,
    int? ObjectiveScore,
    IReadOnlyList<ExamEvent> Events);

public sealed record ExamAnswerRequest(string? QuestionId, string? Answer);

public sealed record ExamResult(
    string AttemptId,
    ExamAttemptStatus Status,
    int? ObjectiveScore,
    int Correct,
    int Total,
    IReadOnlyList<string> IncorrectQuestionIds);

public sealed record ExamQuestionView(string Id, string Prompt);

public sealed record ExamDefinitionView(
    string Id,
    string Name,
    string HskLevel,
    string ContentVersion,
    IReadOnlyList<ExamQuestionView> Questions);

public sealed record ExamAttemptView(
    string Id,
    string ExamId,
    string ContentVersion,
    ExamAttemptStatus Status,
    IReadOnlyList<ExamQuestionView> Questions,
    IReadOnlyDictionary<string, string> Answers,
    int? ObjectiveScore);
