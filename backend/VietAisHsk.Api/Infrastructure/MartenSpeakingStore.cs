using Marten;
using VietAisHsk.Api.Modules.Speaking;

namespace VietAisHsk.Api.Infrastructure;

public sealed class MartenSpeakingStore(IDocumentStore documentStore) : ISpeakingStore
{
    public SpeakingSession Create(string userId, StartSpeakingSessionRequest request)
    {
        var sessionDocument = new SpeakingSession(
            Guid.NewGuid().ToString("N"),
            userId,
            request.HskContext!.Trim(),
            request.Mode!.Trim(),
            SpeakingSessionStatus.Active,
            Array.Empty<SpeakingTurn>(),
            DateTimeOffset.UtcNow,
            null);

        using var session = documentStore.LightweightSession();
        session.Store(sessionDocument);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return sessionDocument;
    }

    public SpeakingSession? Get(string userId, string sessionId)
    {
        using var session = documentStore.QuerySession();
        var speakingSession = session.LoadAsync<SpeakingSession>(sessionId).GetAwaiter().GetResult();
        return speakingSession is not null && string.Equals(speakingSession.UserId, userId, StringComparison.Ordinal)
            ? speakingSession
            : null;
    }

    public SpeakingSession? AddTurn(
        string userId,
        string sessionId,
        SubmitSpeakingTurnRequest request,
        string providerStatus,
        string? assistantText)
    {
        using var session = documentStore.LightweightSession();
        var current = session.LoadAsync<SpeakingSession>(sessionId).GetAwaiter().GetResult();
        if (current is null
            || !string.Equals(current.UserId, userId, StringComparison.Ordinal)
            || current.Status != SpeakingSessionStatus.Active)
        {
            return null;
        }

        var turn = new SpeakingTurn(
            Guid.NewGuid().ToString("N"),
            request.Transcript!.Trim(),
            request.AudioReference?.Trim(),
            providerStatus,
            assistantText,
            DateTimeOffset.UtcNow);
        var updated = current with { Turns = current.Turns.Append(turn).ToArray() };
        session.Store(updated);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return updated;
    }

    public SpeakingSession? End(string userId, string sessionId)
    {
        using var session = documentStore.LightweightSession();
        var current = session.LoadAsync<SpeakingSession>(sessionId).GetAwaiter().GetResult();
        if (current is null || !string.Equals(current.UserId, userId, StringComparison.Ordinal))
        {
            return null;
        }

        if (current.Status == SpeakingSessionStatus.Ended)
        {
            return current;
        }

        var ended = current with
        {
            Status = SpeakingSessionStatus.Ended,
            EndedAt = DateTimeOffset.UtcNow,
        };
        session.Store(ended);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return ended;
    }

    public IReadOnlyList<SpeakingSession> History(string userId)
    {
        using var session = documentStore.QuerySession();
        return session.Query<SpeakingSession>()
            .Where(speakingSession => speakingSession.UserId == userId)
            .OrderByDescending(speakingSession => speakingSession.StartedAt)
            .ToListAsync()
            .GetAwaiter()
            .GetResult();
    }
}
