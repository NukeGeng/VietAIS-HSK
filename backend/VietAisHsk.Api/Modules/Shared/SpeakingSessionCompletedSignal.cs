namespace VietAisHsk.Api.Modules.Shared;

public sealed record SpeakingSessionCompletedSignal(
    string UserId,
    string SessionId,
    string HskContext,
    int TurnCount,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    string? EventId = null);
