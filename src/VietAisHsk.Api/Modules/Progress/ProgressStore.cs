using System.Collections.Concurrent;
using VietAisHsk.Api.Modules.Shared;

namespace VietAisHsk.Api.Modules.Progress;

public interface IProgressSignalSink
{
    void ApplyPracticeSignal(PracticeEvaluationSignal signal);
    void RecordActivity(string userId, string activityType, string referenceId, DateTimeOffset occurredAt);
}

public interface IProgressStore : IProgressSignalSink
{
    UserProgressSnapshot GetSnapshot(string userId);
    IReadOnlyList<UserWeakPoint> GetWeakPoints(string userId);
    IReadOnlyList<LearningHistoryEntry> GetHistory(string userId);
    StudyStreak GetStreak(string userId, string timezone);
}

public sealed class InMemoryProgressStore : IProgressStore
{
    private sealed class MutableProjection
    {
        public int PracticeAnswered;
        public int PracticeCorrect;
        public int PracticeIncorrect;
        public Dictionary<string, UserWeakPoint> WeakPoints { get; } = new(StringComparer.Ordinal);
        public List<LearningHistoryEntry> History { get; } = [];
        public HashSet<string> AppliedActivities { get; } = new(StringComparer.Ordinal);
        public DateTimeOffset UpdatedAt = DateTimeOffset.UtcNow;
    }

    private readonly ConcurrentDictionary<string, MutableProjection> projections = new(StringComparer.Ordinal);

    public void ApplyPracticeSignal(PracticeEvaluationSignal signal)
    {
        var projection = projections.GetOrAdd(signal.UserId, _ => new MutableProjection());
        lock (projection)
        {
            projection.PracticeAnswered++;
            if (string.Equals(signal.Result, "Correct", StringComparison.OrdinalIgnoreCase))
            {
                projection.PracticeCorrect++;
            }
            else if (string.Equals(signal.Result, "Incorrect", StringComparison.OrdinalIgnoreCase))
            {
                projection.PracticeIncorrect++;
                var key = $"{signal.KnowledgeType}:{signal.KnowledgeId}";
                projection.WeakPoints[key] = projection.WeakPoints.TryGetValue(key, out var existing)
                    ? existing with { EvidenceCount = existing.EvidenceCount + 1 }
                    : new UserWeakPoint(signal.KnowledgeType, signal.KnowledgeId, "WrongAnswer", 1, "Ôn ngay");
            }
            projection.UpdatedAt = signal.EvaluatedAt;
        }
    }

    public void RecordActivity(string userId, string activityType, string referenceId, DateTimeOffset occurredAt)
    {
        var projection = projections.GetOrAdd(userId, _ => new MutableProjection());
        var activityKey = $"{activityType}:{referenceId}";
        lock (projection)
        {
            if (!projection.AppliedActivities.Add(activityKey))
            {
                return;
            }

            projection.History.Add(new LearningHistoryEntry(activityType, referenceId, occurredAt));
            projection.UpdatedAt = occurredAt;
        }
    }

    public UserProgressSnapshot GetSnapshot(string userId)
    {
        var projection = projections.GetOrAdd(userId, _ => new MutableProjection());
        lock (projection)
        {
            return new UserProgressSnapshot(userId, projection.PracticeAnswered, projection.PracticeCorrect, projection.PracticeIncorrect, projection.History.Count, projection.UpdatedAt);
        }
    }

    public IReadOnlyList<UserWeakPoint> GetWeakPoints(string userId)
    {
        var projection = projections.GetOrAdd(userId, _ => new MutableProjection());
        lock (projection)
        {
            return projection.WeakPoints.Values.OrderByDescending(item => item.EvidenceCount).ToArray();
        }
    }

    public IReadOnlyList<LearningHistoryEntry> GetHistory(string userId)
    {
        var projection = projections.GetOrAdd(userId, _ => new MutableProjection());
        lock (projection)
        {
            return projection.History.OrderByDescending(item => item.OccurredAt).ToArray();
        }
    }

    public StudyStreak GetStreak(string userId, string timezone)
    {
        var projection = projections.GetOrAdd(userId, _ => new MutableProjection());
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            zone = TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            zone = TimeZoneInfo.Utc;
        }

        lock (projection)
        {
            var days = projection.History
                .Select(entry => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(entry.OccurredAt, zone).DateTime))
                .Distinct()
                .OrderByDescending(day => day)
                .ToArray();
            var current = 0;
            var cursor = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).DateTime);
            foreach (var day in days)
            {
                if (day != cursor)
                {
                    break;
                }
                current++;
                cursor = cursor.AddDays(-1);
            }
            return new StudyStreak(current, days);
        }
    }
}
