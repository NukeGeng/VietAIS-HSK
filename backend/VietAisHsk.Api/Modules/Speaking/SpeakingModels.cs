namespace VietAisHsk.Api.Modules.Speaking;

public enum SpeakingSessionStatus
{
    Active,
    Ended
}

public sealed record SpeakingSession(
    string Id,
    string UserId,
    string HskContext,
    string Mode,
    SpeakingSessionStatus Status,
    IReadOnlyList<SpeakingTurn> Turns,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt);

public sealed record SpeakingTurn(
    string Id,
    string Transcript,
    string? AudioReference,
    string ProviderStatus,
    string? AssistantText,
    DateTimeOffset CreatedAt);

public sealed record StartSpeakingSessionRequest(string? HskContext, string? Mode);

public sealed record SubmitSpeakingTurnRequest(string? Transcript, string? AudioReference);

public sealed record SpeakingTurnResult(
    SpeakingTurn Turn,
    string NextAction);
