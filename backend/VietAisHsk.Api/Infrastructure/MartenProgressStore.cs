using Marten;
using VietAisHsk.Api.Modules.Progress;
using VietAisHsk.Api.Modules.Shared;

namespace VietAisHsk.Api.Infrastructure;

public sealed class MartenProgressStore(IDocumentStore documentStore) : IProgressStore
{
    public void ApplyPracticeSignal(PracticeEvaluationSignal signal)
    {
        using var session = documentStore.LightweightSession();
        var current = Load(session, signal.UserId) ?? Empty(signal.UserId);
        var signals = current.PracticeSignals.ToDictionary(ProgressProjectionBuilder.GetPracticeEventKey, StringComparer.Ordinal);
        var eventKey = ProgressProjectionBuilder.GetPracticeEventKey(signal);
        if (signals.TryGetValue(eventKey, out var existing) && existing == signal)
        {
            return;
        }

        signals[eventKey] = signal;
        session.Store(ProgressProjectionBuilder.Rebuild(
            signal.UserId,
            signals.Values,
            current.Activities,
            reviewSignals: current.ReviewSignals ?? [],
            examSignals: current.ExamSignals ?? [],
            translationSignals: current.TranslationSignals ?? [],
            speakingSignals: current.SpeakingSignals ?? []));
        session.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public void ApplyReviewSignal(ReviewEvaluationSignal signal)
    {
        using var session = documentStore.LightweightSession();
        var current = Load(session, signal.UserId) ?? Empty(signal.UserId);
        var signals = (current.ReviewSignals ?? []).ToDictionary(ProgressProjectionBuilder.GetReviewEventKey, StringComparer.Ordinal);
        var eventKey = ProgressProjectionBuilder.GetReviewEventKey(signal);
        if (signals.TryGetValue(eventKey, out var existing) && existing == signal)
        {
            return;
        }

        signals[eventKey] = signal;
        session.Store(ProgressProjectionBuilder.Rebuild(
            signal.UserId,
            current.PracticeSignals,
            current.Activities,
            reviewSignals: signals.Values,
            examSignals: current.ExamSignals ?? [],
            translationSignals: current.TranslationSignals ?? [],
            speakingSignals: current.SpeakingSignals ?? []));
        session.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public void ApplyExamSignal(ExamResultSignal signal)
    {
        using var session = documentStore.LightweightSession();
        var current = Load(session, signal.UserId) ?? Empty(signal.UserId);
        var signals = (current.ExamSignals ?? []).ToDictionary(ProgressProjectionBuilder.GetExamEventKey, StringComparer.Ordinal);
        var eventKey = ProgressProjectionBuilder.GetExamEventKey(signal);
        if (signals.TryGetValue(eventKey, out var existing) && existing == signal)
        {
            return;
        }

        signals[eventKey] = signal;
        session.Store(ProgressProjectionBuilder.Rebuild(
            signal.UserId,
            current.PracticeSignals,
            current.Activities,
            reviewSignals: current.ReviewSignals ?? [],
            examSignals: signals.Values,
            translationSignals: current.TranslationSignals ?? [],
            speakingSignals: current.SpeakingSignals ?? []));
        session.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public void ApplyTranslationSignal(TranslationAttemptSignal signal)
    {
        using var session = documentStore.LightweightSession();
        var current = Load(session, signal.UserId) ?? Empty(signal.UserId);
        var signals = (current.TranslationSignals ?? []).ToDictionary(ProgressProjectionBuilder.GetTranslationEventKey, StringComparer.Ordinal);
        var eventKey = ProgressProjectionBuilder.GetTranslationEventKey(signal);
        if (signals.TryGetValue(eventKey, out var existing) && existing == signal)
        {
            return;
        }

        signals[eventKey] = signal;
        session.Store(ProgressProjectionBuilder.Rebuild(
            signal.UserId,
            current.PracticeSignals,
            current.Activities,
            reviewSignals: current.ReviewSignals ?? [],
            examSignals: current.ExamSignals ?? [],
            translationSignals: signals.Values,
            speakingSignals: current.SpeakingSignals ?? []));
        session.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public void ApplySpeakingSignal(SpeakingSessionCompletedSignal signal)
    {
        using var session = documentStore.LightweightSession();
        var current = Load(session, signal.UserId) ?? Empty(signal.UserId);
        var signals = (current.SpeakingSignals ?? []).ToDictionary(ProgressProjectionBuilder.GetSpeakingEventKey, StringComparer.Ordinal);
        var eventKey = ProgressProjectionBuilder.GetSpeakingEventKey(signal);
        if (signals.TryGetValue(eventKey, out var existing) && existing == signal)
        {
            return;
        }

        signals[eventKey] = signal;
        session.Store(ProgressProjectionBuilder.Rebuild(
            signal.UserId,
            current.PracticeSignals,
            current.Activities,
            reviewSignals: current.ReviewSignals ?? [],
            examSignals: current.ExamSignals ?? [],
            translationSignals: current.TranslationSignals ?? [],
            speakingSignals: signals.Values));
        session.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public void RecordActivity(string userId, string activityType, string referenceId, DateTimeOffset occurredAt)
    {
        using var session = documentStore.LightweightSession();
        var current = Load(session, userId) ?? Empty(userId);
        var eventKey = ProgressProjectionBuilder.GetActivityEventKey(activityType, referenceId);
        if (current.Activities.Any(activity => string.Equals(activity.EventKey, eventKey, StringComparison.Ordinal)))
        {
            return;
        }

        var activity = new ProgressActivityRecord(
            eventKey,
            new LearningHistoryEntry(activityType, referenceId, occurredAt));
        session.Store(ProgressProjectionBuilder.Rebuild(
            userId,
            current.PracticeSignals,
            current.Activities.Append(activity),
            reviewSignals: current.ReviewSignals ?? [],
            examSignals: current.ExamSignals ?? [],
            translationSignals: current.TranslationSignals ?? [],
            speakingSignals: current.SpeakingSignals ?? []));
        session.SaveChangesAsync().GetAwaiter().GetResult();
    }

    public UserProgressSnapshot GetSnapshot(string userId) => Read(userId)?.Snapshot ?? Empty(userId).Snapshot;

    public IReadOnlyList<UserKnowledgeMastery> GetMastery(string userId) => Read(userId)?.Mastery ?? [];

    public IReadOnlyList<UserWeakPoint> GetWeakPoints(string userId) => Read(userId)?.WeakPoints ?? [];

    public IReadOnlyList<LearningHistoryEntry> GetHistory(string userId) =>
        Read(userId)?.Activities
            .Select(activity => activity.Entry)
            .OrderByDescending(entry => entry.OccurredAt)
            .ToArray() ?? [];

    public StudyStreak GetStreak(string userId, string timezone)
    {
        var history = Read(userId)?.Activities.Select(activity => activity.Entry) ?? [];
        return ProgressProjectionBuilder.CalculateStreak(history, timezone, DateTimeOffset.UtcNow);
    }

    private ProgressProjectionDocument? Read(string userId)
    {
        using var session = documentStore.QuerySession();
        return session.LoadAsync<ProgressProjectionDocument>(userId).GetAwaiter().GetResult();
    }

    private static ProgressProjectionDocument? Load(IDocumentSession session, string userId) =>
        session.LoadAsync<ProgressProjectionDocument>(userId).GetAwaiter().GetResult();

    private static ProgressProjectionDocument Empty(string userId) =>
        ProgressProjectionBuilder.Rebuild(userId, [], []);
}
