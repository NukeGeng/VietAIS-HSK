namespace VietAisHsk.Api.Modules.Shared;

public sealed record ReviewEvaluationSignal(
    string UserId,
    string KnowledgeType,
    string KnowledgeId,
    string Result,
    DateTimeOffset EvaluatedAt,
    string? EventId = null);
