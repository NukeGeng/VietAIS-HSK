using VietAisHsk.Api.Modules.Shared;

namespace VietAisHsk.Api.Modules.Progress;

public static class ProgressProjectionBuilder
{
    public static string GetPracticeEventKey(PracticeEvaluationSignal signal) =>
        string.IsNullOrWhiteSpace(signal.EventId)
            ? $"legacy:{signal.KnowledgeType}\u001f{signal.KnowledgeId}\u001f{signal.Result}\u001f{signal.EvaluatedAt:O}"
            : $"id:{signal.EventId}";

    public static string GetReviewEventKey(ReviewEvaluationSignal signal) =>
        string.IsNullOrWhiteSpace(signal.EventId)
            ? $"legacy:{signal.KnowledgeType}\u001f{signal.KnowledgeId}\u001f{signal.Result}\u001f{signal.EvaluatedAt:O}"
            : $"id:{signal.EventId}";

    public static string GetExamEventKey(ExamResultSignal signal) =>
        string.IsNullOrWhiteSpace(signal.EventId)
            ? $"legacy:{signal.ExamId}\u001f{signal.AttemptId}\u001f{signal.EvaluatedAt:O}"
            : $"id:{signal.EventId}";

    public static string GetTranslationEventKey(TranslationAttemptSignal signal) =>
        string.IsNullOrWhiteSpace(signal.EventId)
            ? $"legacy:{signal.AttemptId}\u001f{signal.SubmittedAt:O}"
            : $"id:{signal.EventId}";

    public static string GetSpeakingEventKey(SpeakingSessionCompletedSignal signal) =>
        string.IsNullOrWhiteSpace(signal.EventId)
            ? $"legacy:{signal.SessionId}\u001f{signal.CompletedAt:O}"
            : $"id:{signal.EventId}";

    public static string GetActivityEventKey(string activityType, string referenceId) =>
        $"{activityType}\u001f{referenceId}";

    public static ProgressProjectionDocument Rebuild(
        string userId,
        IEnumerable<PracticeEvaluationSignal> practiceSignals,
        IEnumerable<ProgressActivityRecord> activities,
        DateTimeOffset? emptyUpdatedAt = null,
        IEnumerable<ReviewEvaluationSignal>? reviewSignals = null,
        IEnumerable<ExamResultSignal>? examSignals = null,
        IEnumerable<TranslationAttemptSignal>? translationSignals = null,
        IEnumerable<SpeakingSessionCompletedSignal>? speakingSignals = null)
    {
        var normalizedSignals = new Dictionary<string, PracticeEvaluationSignal>(StringComparer.Ordinal);
        foreach (var signal in practiceSignals
                     .OrderBy(item => item.EvaluatedAt)
                     .ThenBy(item => item.Result, StringComparer.Ordinal))
        {
            normalizedSignals[GetPracticeEventKey(signal)] = signal;
        }

        var normalizedActivities = new Dictionary<string, ProgressActivityRecord>(StringComparer.Ordinal);
        foreach (var activity in activities.OrderBy(item => item.Entry.OccurredAt))
        {
            normalizedActivities.TryAdd(activity.EventKey, activity);
        }

        var normalizedReviewSignals = new Dictionary<string, ReviewEvaluationSignal>(StringComparer.Ordinal);
        foreach (var signal in reviewSignals ?? [])
        {
            normalizedReviewSignals[GetReviewEventKey(signal)] = signal;
        }

        var signals = normalizedSignals
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Value)
            .ToArray();
        var activityRecords = normalizedActivities
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Value)
            .ToArray();
        var reviewSignalRecords = normalizedReviewSignals
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Value)
            .ToArray();
        var normalizedExamSignals = new Dictionary<string, ExamResultSignal>(StringComparer.Ordinal);
        foreach (var signal in examSignals ?? [])
        {
            normalizedExamSignals[GetExamEventKey(signal)] = signal;
        }

        var examSignalRecords = normalizedExamSignals
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Value)
            .ToArray();
        var normalizedTranslationSignals = new Dictionary<string, TranslationAttemptSignal>(StringComparer.Ordinal);
        foreach (var signal in translationSignals ?? [])
        {
            normalizedTranslationSignals[GetTranslationEventKey(signal)] = signal;
        }

        var translationSignalRecords = normalizedTranslationSignals
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Value)
            .ToArray();
        var normalizedSpeakingSignals = new Dictionary<string, SpeakingSessionCompletedSignal>(StringComparer.Ordinal);
        foreach (var signal in speakingSignals ?? [])
        {
            normalizedSpeakingSignals[GetSpeakingEventKey(signal)] = signal;
        }

        var speakingSignalRecords = normalizedSpeakingSignals
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Value)
            .ToArray();
        var mastery = signals
            .Select(signal => new { signal.KnowledgeType, signal.KnowledgeId, signal.Result })
            .Concat(reviewSignalRecords.Select(signal => new { signal.KnowledgeType, signal.KnowledgeId, signal.Result }))
            .GroupBy(item => (item.KnowledgeType, item.KnowledgeId))
            .Select(group =>
            {
                var attempts = group.Count();
                var correct = group.Count(item => string.Equals(item.Result, "Correct", StringComparison.OrdinalIgnoreCase));
                var incorrect = group.Count(item => string.Equals(item.Result, "Incorrect", StringComparison.OrdinalIgnoreCase));
                var score = attempts == 0
                    ? 0
                    : (int)Math.Round(correct * 100d / attempts, MidpointRounding.AwayFromZero);
                var state = score >= 80 ? "Strong" : score >= 50 ? "Learning" : "NeedsReview";
                return new UserKnowledgeMastery(
                    group.Key.KnowledgeType,
                    group.Key.KnowledgeId,
                    attempts,
                    correct,
                    incorrect,
                    score,
                    state);
            })
            .OrderByDescending(item => item.ScorePercent)
            .ThenBy(item => item.KnowledgeType, StringComparer.Ordinal)
            .ThenBy(item => item.KnowledgeId, StringComparer.Ordinal)
            .ToArray();
        var weakPoints = signals
            .Where(signal => string.Equals(signal.Result, "Incorrect", StringComparison.OrdinalIgnoreCase))
            .GroupBy(signal => (signal.KnowledgeType, signal.KnowledgeId))
            .Select(group => new
            {
                group.Key.KnowledgeType,
                group.Key.KnowledgeId,
                EvidenceCount = group.Count(),
            })
            .Concat(reviewSignalRecords
                .GroupBy(signal => (signal.KnowledgeType, signal.KnowledgeId))
                .Select(group => new
                {
                    group.Key.KnowledgeType,
                    group.Key.KnowledgeId,
                    EvidenceCount = group.Count(signal => string.Equals(signal.Result, "Incorrect", StringComparison.OrdinalIgnoreCase))
                        - group.Count(signal => string.Equals(signal.Result, "Correct", StringComparison.OrdinalIgnoreCase)),
                }))
            .Concat(examSignalRecords
                .SelectMany(signal => signal.IncorrectQuestionIds.Select(questionId => new
                {
                    KnowledgeType = "exam",
                    KnowledgeId = $"{signal.ExamId}:{questionId}",
                    EvidenceCount = 1,
                })))
            .GroupBy(item => (item.KnowledgeType, item.KnowledgeId))
            .Select(group => new
            {
                group.Key.KnowledgeType,
                group.Key.KnowledgeId,
                EvidenceCount = Math.Max(0, group.Sum(item => item.EvidenceCount)),
            })
            .Where(item => item.EvidenceCount > 0)
            .Select(item => new UserWeakPoint(
                item.KnowledgeType,
                item.KnowledgeId,
                string.Equals(item.KnowledgeType, "hanzi-writing", StringComparison.OrdinalIgnoreCase)
                    ? "WritingWeak"
                    : string.Equals(item.KnowledgeType, "exam", StringComparison.OrdinalIgnoreCase) ? "ExamIncorrect" : "WrongAnswer",
                item.EvidenceCount,
                string.Equals(item.KnowledgeType, "hanzi-writing", StringComparison.OrdinalIgnoreCase) ? "Luyện viết" : "Ôn ngay"))
            .OrderByDescending(item => item.EvidenceCount)
            .ThenBy(item => item.KnowledgeType, StringComparer.Ordinal)
            .ThenBy(item => item.KnowledgeId, StringComparer.Ordinal)
            .ToArray();
        var history = activityRecords.Select(activity => activity.Entry).ToArray();
        var updateTimes = signals.Select(signal => signal.EvaluatedAt)
            .Concat(reviewSignalRecords.Select(signal => signal.EvaluatedAt))
            .Concat(examSignalRecords.Select(signal => signal.EvaluatedAt))
            .Concat(translationSignalRecords.Select(signal => signal.SubmittedAt))
            .Concat(speakingSignalRecords.Select(signal => signal.CompletedAt))
            .Concat(history.Select(entry => entry.OccurredAt))
            .ToArray();
        var updatedAt = updateTimes.Length == 0
            ? emptyUpdatedAt ?? DateTimeOffset.UtcNow
            : updateTimes.Max();

        return new ProgressProjectionDocument(
            userId,
            signals,
            activityRecords,
            new UserProgressSnapshot(
                userId,
                signals.Length,
                signals.Count(signal => string.Equals(signal.Result, "Correct", StringComparison.OrdinalIgnoreCase)),
                signals.Count(signal => string.Equals(signal.Result, "Incorrect", StringComparison.OrdinalIgnoreCase)),
                history.Length,
                updatedAt,
                reviewSignalRecords.Length,
                reviewSignalRecords.Count(signal => string.Equals(signal.Result, "Correct", StringComparison.OrdinalIgnoreCase)),
                reviewSignalRecords.Count(signal => string.Equals(signal.Result, "Incorrect", StringComparison.OrdinalIgnoreCase)),
                examSignalRecords.Length,
                examSignalRecords.Sum(signal => signal.Total),
                examSignalRecords.Sum(signal => signal.Correct),
                examSignalRecords.Sum(signal => signal.IncorrectQuestionIds.Count),
                translationSignalRecords.Length,
                speakingSignalRecords.Length,
                speakingSignalRecords.Sum(signal => Math.Max(0, signal.TurnCount))),
            weakPoints,
            reviewSignalRecords,
            examSignalRecords,
            translationSignalRecords,
            speakingSignalRecords,
            mastery);
    }

    public static StudyStreak CalculateStreak(IEnumerable<LearningHistoryEntry> history, string timezone, DateTimeOffset now)
    {
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

        var days = history
            .Select(entry => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(entry.OccurredAt, zone).DateTime))
            .Distinct()
            .OrderByDescending(day => day)
            .ToArray();
        var current = 0;
        var cursor = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
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
