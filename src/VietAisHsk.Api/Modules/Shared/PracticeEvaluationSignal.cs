namespace VietAisHsk.Api.Modules.Shared;

public sealed record PracticeEvaluationSignal(
    string UserId,
    string KnowledgeType,
    string KnowledgeId,
    string Result,
    DateTimeOffset EvaluatedAt);
