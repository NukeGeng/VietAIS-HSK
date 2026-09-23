using System.Collections.Concurrent;
using VietAisHsk.Api.Modules.Shared;

namespace VietAisHsk.Api.Modules.Review;

public interface IReviewSignalSink
{
    void ApplyPracticeSignal(PracticeEvaluationSignal signal);
    void ApplyExamSignal(ExamResultSignal signal);
}

public interface IReviewStore : IReviewSignalSink
{
    IReadOnlyList<ReviewItem> GetItems(string userId, bool dueOnly = false, bool mistakesOnly = false);
    ReviewSummary GetSummary(string userId);
    ReviewSession? StartSession(string userId, IReadOnlyList<string> itemIds);
    ReviewSession? GetSession(string userId, string sessionId);
    ReviewItem? RecordResult(string userId, string sessionId, string itemId, bool correct);
}

public sealed class InMemoryReviewStore(IReviewClock? reviewClock = null) : IReviewStore
{
    private readonly IReviewClock clock = reviewClock ?? new SystemReviewClock();
    private readonly ConcurrentDictionary<string, ReviewItem> items = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ReviewSession> sessions = new(StringComparer.Ordinal);

    public void ApplyExamSignal(ExamResultSignal signal)
    {
        foreach (var result in signal.KnowledgeResults ?? [])
        {
            if (!string.Equals(result.Result, "Incorrect", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var reason = ReviewReason.WrongAnswer;
            var itemId = BuildItemId(signal.UserId, result.KnowledgeType, result.KnowledgeId, reason);
            var sourceEventId = $"{signal.EventId ?? $"exam:{signal.AttemptId}"}:{result.QuestionId}";
            items.AddOrUpdate(
                itemId,
                _ => new ReviewItem(itemId, signal.UserId, result.KnowledgeType, result.KnowledgeId, reason, 60, 1, signal.EvaluatedAt, signal.EvaluatedAt, false, sourceEventId),
                (_, current) => current.LastSourceEventId == sourceEventId
                    ? current
                    : current with
                    {
                        Priority = Math.Min(100, current.Priority + 10),
                        MistakeCount = current.MistakeCount + 1,
                        NextReviewAt = signal.EvaluatedAt,
                        LastAttemptedAt = signal.EvaluatedAt,
                        Resolved = false,
                        LastSourceEventId = sourceEventId,
                    });
        }
    }

    public void ApplyPracticeSignal(PracticeEvaluationSignal signal)
    {
        var reason = string.Equals(signal.KnowledgeType, "hanzi-writing", StringComparison.OrdinalIgnoreCase)
            ? ReviewReason.WritingWeak
            : ReviewReason.WrongAnswer;
        var itemId = BuildItemId(signal.UserId, signal.KnowledgeType, signal.KnowledgeId, reason);
        var now = signal.EvaluatedAt;

        if (string.Equals(signal.Result, "Incorrect", StringComparison.OrdinalIgnoreCase))
        {
            items.AddOrUpdate(
                itemId,
                _ => new ReviewItem(itemId, signal.UserId, signal.KnowledgeType, signal.KnowledgeId, reason, 60, 1, now, now, false),
                (_, current) => current with
                {
                    Priority = Math.Min(100, current.Priority + 10),
                    MistakeCount = current.MistakeCount + 1,
                    NextReviewAt = now,
                    LastAttemptedAt = now,
                    Resolved = false
                });
            return;
        }

        if (string.Equals(signal.Result, "Correct", StringComparison.OrdinalIgnoreCase)
            && items.TryGetValue(itemId, out var existing))
        {
            items[itemId] = existing with
            {
                Priority = Math.Max(0, existing.Priority - 20),
                NextReviewAt = ReviewScheduler.NextAfterCorrect(now),
                LastAttemptedAt = now,
                Resolved = existing.MistakeCount <= 1
            };
        }
    }

    public IReadOnlyList<ReviewItem> GetItems(string userId, bool dueOnly = false, bool mistakesOnly = false)
    {
        var now = clock.UtcNow;
        return items.Values
            .Where(item => item.UserId == userId && !item.Resolved)
            .Where(item => !dueOnly || item.NextReviewAt <= now)
            .Where(item => !mistakesOnly || item.Reason == ReviewReason.WrongAnswer)
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.NextReviewAt)
            .ToArray();
    }

    public ReviewSummary GetSummary(string userId)
    {
        var all = GetItems(userId);
        return new ReviewSummary(
            all.Count(item => item.NextReviewAt <= clock.UtcNow),
            all.Count(item => item.Reason == ReviewReason.WrongAnswer),
            all.Count);
    }

    public ReviewSession? StartSession(string userId, IReadOnlyList<string> itemIds)
    {
        var eligible = GetItems(userId).Where(item => itemIds.Contains(item.Id, StringComparer.Ordinal)).Select(item => item.Id).ToArray();
        if (eligible.Length == 0)
        {
            return null;
        }

        var session = new ReviewSession(Guid.NewGuid().ToString("N"), userId, eligible, clock.UtcNow);
        sessions[session.Id] = session;
        return session;
    }

    public ReviewSession? GetSession(string userId, string sessionId) =>
        sessions.TryGetValue(sessionId, out var session) && session.UserId == userId ? session : null;

    public ReviewItem? RecordResult(string userId, string sessionId, string itemId, bool correct)
    {
        var session = GetSession(userId, sessionId);
        if (session is null || !session.ItemIds.Contains(itemId, StringComparer.Ordinal) || !items.TryGetValue(itemId, out var item))
        {
            return null;
        }

        var recordedResults = session.RecordedResults ?? new Dictionary<string, bool>(StringComparer.Ordinal);
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
        items[itemId] = updated;
        var results = recordedResults.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        results[itemId] = correct;
        sessions[session.Id] = session with { RecordedResults = results };
        return updated;
    }

    private static string BuildItemId(string userId, string knowledgeType, string knowledgeId, ReviewReason reason) =>
        $"{userId}:{knowledgeType}:{knowledgeId}:{reason}";
}
