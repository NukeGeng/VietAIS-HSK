using Marten;
using VietAisHsk.Api.Modules.Review;
using VietAisHsk.Api.Modules.Shared;

namespace VietAisHsk.Api.Infrastructure;

public sealed class MartenReviewStore(IDocumentStore documentStore, IReviewClock clock) : IReviewStore
{
    public void ApplyExamSignal(ExamResultSignal signal)
    {
        foreach (var result in signal.KnowledgeResults ?? [])
        {
            if (!string.Equals(result.Result, "Incorrect", StringComparison.OrdinalIgnoreCase)) continue;

            var reason = ReviewReason.WrongAnswer;
            var itemId = BuildItemId(signal.UserId, result.KnowledgeType, result.KnowledgeId, reason);
            var sourceEventId = $"{signal.EventId ?? $"exam:{signal.AttemptId}"}:{result.QuestionId}";
            using var session = documentStore.LightweightSession();
            var current = session.LoadAsync<ReviewItem>(itemId).GetAwaiter().GetResult();
            if (current?.LastSourceEventId == sourceEventId) continue;

            var updated = current is null
                ? new ReviewItem(itemId, signal.UserId, result.KnowledgeType, result.KnowledgeId, reason, 60, 1, signal.EvaluatedAt, signal.EvaluatedAt, false, sourceEventId)
                : current with
                {
                    Priority = Math.Min(100, current.Priority + 10),
                    MistakeCount = current.MistakeCount + 1,
                    NextReviewAt = signal.EvaluatedAt,
                    LastAttemptedAt = signal.EvaluatedAt,
                    Resolved = false,
                    LastSourceEventId = sourceEventId,
                };
            session.Store(updated);
            session.SaveChangesAsync().GetAwaiter().GetResult();
        }
    }

    public void ApplyPracticeSignal(PracticeEvaluationSignal signal)
    {
        var reason = string.Equals(signal.KnowledgeType, "hanzi-writing", StringComparison.OrdinalIgnoreCase)
            ? ReviewReason.WritingWeak
            : ReviewReason.WrongAnswer;
        var itemId = BuildItemId(signal.UserId, signal.KnowledgeType, signal.KnowledgeId, reason);
        using var session = documentStore.LightweightSession();
        var current = session.LoadAsync<ReviewItem>(itemId).GetAwaiter().GetResult();
        var now = signal.EvaluatedAt;

        if (string.Equals(signal.Result, "Incorrect", StringComparison.OrdinalIgnoreCase))
        {
            var updated = current is null
                ? new ReviewItem(itemId, signal.UserId, signal.KnowledgeType, signal.KnowledgeId, reason, 60, 1, now, now, false)
                : current with
                {
                    Priority = Math.Min(100, current.Priority + 10),
                    MistakeCount = current.MistakeCount + 1,
                    NextReviewAt = now,
                    LastAttemptedAt = now,
                    Resolved = false
                };
            session.Store(updated);
            session.SaveChangesAsync().GetAwaiter().GetResult();
            return;
        }

        if (string.Equals(signal.Result, "Correct", StringComparison.OrdinalIgnoreCase) && current is not null)
        {
            session.Store(current with
            {
                Priority = Math.Max(0, current.Priority - 20),
                NextReviewAt = ReviewScheduler.NextAfterCorrect(now),
                LastAttemptedAt = now,
                Resolved = current.MistakeCount <= 1
            });
            session.SaveChangesAsync().GetAwaiter().GetResult();
        }
    }

    public IReadOnlyList<ReviewItem> GetItems(string userId, bool dueOnly = false, bool mistakesOnly = false)
    {
        using var session = documentStore.QuerySession();
        var now = clock.UtcNow;
        return session.Query<ReviewItem>()
            .Where(item => item.UserId == userId)
            .ToListAsync().GetAwaiter().GetResult()
            .Where(item => !item.Resolved)
            .Where(item => !dueOnly || item.NextReviewAt <= now)
            .Where(item => !mistakesOnly || item.Reason == ReviewReason.WrongAnswer)
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.NextReviewAt)
            .ToArray();
    }

    public ReviewSummary GetSummary(string userId)
    {
        var items = GetItems(userId);
        var now = clock.UtcNow;
        return new ReviewSummary(
            items.Count(item => item.NextReviewAt <= now),
            items.Count(item => item.Reason == ReviewReason.WrongAnswer),
            items.Count);
    }

    public ReviewSession? StartSession(string userId, IReadOnlyList<string> itemIds)
    {
        var eligibleIds = GetItems(userId)
            .Where(item => itemIds.Contains(item.Id, StringComparer.Ordinal))
            .Select(item => item.Id)
            .ToArray();
        if (eligibleIds.Length == 0)
        {
            return null;
        }

        var reviewSession = new ReviewSession(Guid.NewGuid().ToString("N"), userId, eligibleIds, clock.UtcNow);
        using var session = documentStore.LightweightSession();
        session.Store(reviewSession);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return reviewSession;
    }

    public ReviewSession? GetSession(string userId, string sessionId)
    {
        using var session = documentStore.QuerySession();
        var reviewSession = session.LoadAsync<ReviewSession>(sessionId).GetAwaiter().GetResult();
        return reviewSession?.UserId == userId ? reviewSession : null;
    }

    public ReviewItem? RecordResult(string userId, string sessionId, string itemId, bool correct)
    {
        using var session = documentStore.LightweightSession();
        var reviewSession = session.LoadAsync<ReviewSession>(sessionId).GetAwaiter().GetResult();
        var item = session.LoadAsync<ReviewItem>(itemId).GetAwaiter().GetResult();
        if (reviewSession is null || reviewSession.UserId != userId
            || !reviewSession.ItemIds.Contains(itemId, StringComparer.Ordinal)
            || item is null || item.UserId != userId)
        {
            return null;
        }

        var recordedResults = reviewSession.RecordedResults ?? new Dictionary<string, bool>(StringComparer.Ordinal);
        if (recordedResults.TryGetValue(itemId, out var previousResult))
        {
            return previousResult == correct ? item : null;
        }

        var now = clock.UtcNow;
        var updated = correct
            ? item with
            {
                Priority = Math.Max(0, item.Priority - 20),
                NextReviewAt = ReviewScheduler.NextAfterCorrect(now),
                LastAttemptedAt = now,
                Resolved = item.MistakeCount <= 1
            }
            : item with
            {
                Priority = Math.Min(100, item.Priority + 10),
                MistakeCount = item.MistakeCount + 1,
                NextReviewAt = ReviewScheduler.NextAfterIncorrect(now),
                LastAttemptedAt = now,
                Resolved = false
            };
        var results = recordedResults.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        results[itemId] = correct;
        session.Store(updated);
        session.Store(reviewSession with { RecordedResults = results });
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return updated;
    }

    private static string BuildItemId(string userId, string knowledgeType, string knowledgeId, ReviewReason reason) =>
        $"{userId}:{knowledgeType}:{knowledgeId}:{reason}";
}
