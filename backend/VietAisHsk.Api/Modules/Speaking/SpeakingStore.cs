using System.Collections.Concurrent;

namespace VietAisHsk.Api.Modules.Speaking;

public interface ISpeakingProvider
{
    Task<string?> GenerateResponseAsync(SpeakingSession session, SpeakingTurn turn, CancellationToken cancellationToken);
}

public sealed class UnconfiguredSpeakingProvider : ISpeakingProvider
{
    public Task<string?> GenerateResponseAsync(SpeakingSession session, SpeakingTurn turn, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(null);
}

public interface ISpeakingStore
{
    SpeakingSession Create(string userId, StartSpeakingSessionRequest request);
    SpeakingSession? Get(string userId, string sessionId);
    SpeakingSession? AddTurn(string userId, string sessionId, SubmitSpeakingTurnRequest request, string providerStatus, string? assistantText);
    SpeakingSession? End(string userId, string sessionId);
    IReadOnlyList<SpeakingSession> History(string userId);
}

public sealed class InMemorySpeakingStore : ISpeakingStore
{
    private readonly ConcurrentDictionary<string, SpeakingSession> sessions = new(StringComparer.Ordinal);

    public SpeakingSession Create(string userId, StartSpeakingSessionRequest request)
    {
        var session = new SpeakingSession(
            Guid.NewGuid().ToString("N"),
            userId,
            request.HskContext!.Trim(),
            request.Mode!.Trim(),
            SpeakingSessionStatus.Active,
            Array.Empty<SpeakingTurn>(),
            DateTimeOffset.UtcNow,
            null);
        sessions[session.Id] = session;
        return session;
    }

    public SpeakingSession? Get(string userId, string sessionId) =>
        sessions.TryGetValue(sessionId, out var session) && session.UserId == userId ? session : null;

    public SpeakingSession? AddTurn(string userId, string sessionId, SubmitSpeakingTurnRequest request, string providerStatus, string? assistantText)
    {
        var session = Get(userId, sessionId);
        if (session is null || session.Status != SpeakingSessionStatus.Active)
        {
            return null;
        }

        var turn = new SpeakingTurn(Guid.NewGuid().ToString("N"), request.Transcript!.Trim(), request.AudioReference?.Trim(), providerStatus, assistantText, DateTimeOffset.UtcNow);
        var updated = session with { Turns = session.Turns.Append(turn).ToArray() };
        sessions[sessionId] = updated;
        return updated;
    }

    public SpeakingSession? End(string userId, string sessionId)
    {
        var session = Get(userId, sessionId);
        if (session is null)
        {
            return null;
        }

        if (session.Status == SpeakingSessionStatus.Ended)
        {
            return session;
        }

        var ended = session with { Status = SpeakingSessionStatus.Ended, EndedAt = DateTimeOffset.UtcNow };
        sessions[sessionId] = ended;
        return ended;
    }

    public IReadOnlyList<SpeakingSession> History(string userId) =>
        sessions.Values.Where(session => session.UserId == userId).OrderByDescending(session => session.StartedAt).ToArray();
}
