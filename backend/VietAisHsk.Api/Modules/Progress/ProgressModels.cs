using VietAisHsk.Api.Modules.Shared;

namespace VietAisHsk.Api.Modules.Progress;

public sealed record UserProgressSnapshot(
    string UserId,
    int PracticeAnswered,
    int PracticeCorrect,
    int PracticeIncorrect,
    int CompletedActivities,
    DateTimeOffset UpdatedAt,
    int ReviewAnswered = 0,
    int ReviewCorrect = 0,
    int ReviewIncorrect = 0,
    int ExamAttempts = 0,
    int ExamQuestions = 0,
    int ExamCorrect = 0,
    int ExamIncorrect = 0,
    int TranslationAttempts = 0,
    int SpeakingSessions = 0,
    int SpeakingTurns = 0);

public sealed record UserWeakPoint(
    string KnowledgeType,
    string KnowledgeId,
    string Reason,
    int EvidenceCount,
    string Action);

public sealed record UserKnowledgeMastery(
    string KnowledgeType,
    string KnowledgeId,
    int AttemptCount,
    int CorrectCount,
    int IncorrectCount,
    int ScorePercent,
    string State);

public sealed record LearningHistoryEntry(
    string ActivityType,
    string ReferenceId,
    DateTimeOffset OccurredAt);

public sealed record StudyStreak(
    int CurrentDays,
    IReadOnlyList<DateOnly> QualifyingDays);

public sealed record ProgressActivityRecord(
    string EventKey,
    LearningHistoryEntry Entry);

public sealed record ProgressProjectionDocument(
    string Id,
    IReadOnlyList<PracticeEvaluationSignal> PracticeSignals,
    IReadOnlyList<ProgressActivityRecord> Activities,
    UserProgressSnapshot Snapshot,
    IReadOnlyList<UserWeakPoint> WeakPoints,
    IReadOnlyList<ReviewEvaluationSignal>? ReviewSignals = null,
    IReadOnlyList<ExamResultSignal>? ExamSignals = null,
    IReadOnlyList<TranslationAttemptSignal>? TranslationSignals = null,
    IReadOnlyList<SpeakingSessionCompletedSignal>? SpeakingSignals = null,
    IReadOnlyList<UserKnowledgeMastery>? Mastery = null);
