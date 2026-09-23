namespace VietAisHsk.Api.Modules.Shared;

public sealed record ExamKnowledgeResult(
    string QuestionId,
    string KnowledgeType,
    string KnowledgeId,
    string Result);

public sealed record ExamResultSignal(
    string UserId,
    string ExamId,
    string AttemptId,
    int Correct,
    int Total,
    int ObjectiveScore,
    IReadOnlyList<string> IncorrectQuestionIds,
    DateTimeOffset EvaluatedAt,
    string? EventId = null,
    IReadOnlyList<ExamKnowledgeResult>? KnowledgeResults = null);
