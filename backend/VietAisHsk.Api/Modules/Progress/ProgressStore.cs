using System.Collections.Concurrent;
using VietAisHsk.Api.Modules.Shared;

namespace VietAisHsk.Api.Modules.Progress;

public interface IProgressSignalSink
{
    void ApplyPracticeSignal(PracticeEvaluationSignal signal);
    void ApplyReviewSignal(ReviewEvaluationSignal signal);
    void ApplyExamSignal(ExamResultSignal signal);
    void ApplyTranslationSignal(TranslationAttemptSignal signal);
    void ApplySpeakingSignal(SpeakingSessionCompletedSignal signal);
    void RecordActivity(string userId, string activityType, string referenceId, DateTimeOffset occurredAt);
}

public interface IProgressStore : IProgressSignalSink
{
    UserProgressSnapshot GetSnapshot(string userId);
    IReadOnlyList<UserKnowledgeMastery> GetMastery(string userId);
    IReadOnlyList<UserWeakPoint> GetWeakPoints(string userId);
    IReadOnlyList<LearningHistoryEntry> GetHistory(string userId);
    StudyStreak GetStreak(string userId, string timezone);
}

public sealed class InMemoryProgressStore : IProgressStore
{
    private sealed class MutableProjection
    {
        public Dictionary<string, PracticeEvaluationSignal> PracticeSignals { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, ReviewEvaluationSignal> ReviewSignals { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, ExamResultSignal> ExamSignals { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, TranslationAttemptSignal> TranslationSignals { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, SpeakingSessionCompletedSignal> SpeakingSignals { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, ProgressActivityRecord> Activities { get; } = new(StringComparer.Ordinal);
    }

    private readonly ConcurrentDictionary<string, MutableProjection> projections = new(StringComparer.Ordinal);

    public void ApplyPracticeSignal(PracticeEvaluationSignal signal)
    {
        var projection = projections.GetOrAdd(signal.UserId, _ => new MutableProjection());
        lock (projection)
        {
            projection.PracticeSignals[ProgressProjectionBuilder.GetPracticeEventKey(signal)] = signal;
        }
    }

    public void ApplyReviewSignal(ReviewEvaluationSignal signal)
    {
        var projection = projections.GetOrAdd(signal.UserId, _ => new MutableProjection());
        lock (projection)
        {
            projection.ReviewSignals[ProgressProjectionBuilder.GetReviewEventKey(signal)] = signal;
        }
    }

    public void ApplyExamSignal(ExamResultSignal signal)
    {
        var projection = projections.GetOrAdd(signal.UserId, _ => new MutableProjection());
        lock (projection)
        {
            projection.ExamSignals[ProgressProjectionBuilder.GetExamEventKey(signal)] = signal;
        }
    }

    public void ApplyTranslationSignal(TranslationAttemptSignal signal)
    {
        var projection = projections.GetOrAdd(signal.UserId, _ => new MutableProjection());
        lock (projection)
        {
            projection.TranslationSignals[ProgressProjectionBuilder.GetTranslationEventKey(signal)] = signal;
        }
    }

    public void ApplySpeakingSignal(SpeakingSessionCompletedSignal signal)
    {
        var projection = projections.GetOrAdd(signal.UserId, _ => new MutableProjection());
        lock (projection)
        {
            projection.SpeakingSignals[ProgressProjectionBuilder.GetSpeakingEventKey(signal)] = signal;
        }
    }

    public void RecordActivity(string userId, string activityType, string referenceId, DateTimeOffset occurredAt)
    {
        var projection = projections.GetOrAdd(userId, _ => new MutableProjection());
        var eventKey = ProgressProjectionBuilder.GetActivityEventKey(activityType, referenceId);
        lock (projection)
        {
            projection.Activities.TryAdd(
                eventKey,
                new ProgressActivityRecord(eventKey, new LearningHistoryEntry(activityType, referenceId, occurredAt)));
        }
    }

    public UserProgressSnapshot GetSnapshot(string userId) => GetDocument(userId).Snapshot;

    public IReadOnlyList<UserKnowledgeMastery> GetMastery(string userId) => GetDocument(userId).Mastery ?? [];

    public IReadOnlyList<UserWeakPoint> GetWeakPoints(string userId) => GetDocument(userId).WeakPoints;

    public IReadOnlyList<LearningHistoryEntry> GetHistory(string userId) =>
        GetDocument(userId).Activities
            .Select(activity => activity.Entry)
            .OrderByDescending(entry => entry.OccurredAt)
            .ToArray();

    public StudyStreak GetStreak(string userId, string timezone) =>
        ProgressProjectionBuilder.CalculateStreak(GetDocument(userId).Activities.Select(activity => activity.Entry), timezone, DateTimeOffset.UtcNow);

    private ProgressProjectionDocument GetDocument(string userId)
    {
        var projection = projections.GetOrAdd(userId, _ => new MutableProjection());
        lock (projection)
        {
            return ProgressProjectionBuilder.Rebuild(
                userId,
                projection.PracticeSignals.Values,
                projection.Activities.Values,
                reviewSignals: projection.ReviewSignals.Values,
                examSignals: projection.ExamSignals.Values,
                translationSignals: projection.TranslationSignals.Values,
                speakingSignals: projection.SpeakingSignals.Values);
        }
    }
}
