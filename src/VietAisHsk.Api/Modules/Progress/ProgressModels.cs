namespace VietAisHsk.Api.Modules.Progress;

public sealed record UserProgressSnapshot(
    string UserId,
    int PracticeAnswered,
    int PracticeCorrect,
    int PracticeIncorrect,
    int CompletedActivities,
    DateTimeOffset UpdatedAt);

public sealed record UserWeakPoint(
    string KnowledgeType,
    string KnowledgeId,
    string Reason,
    int EvidenceCount,
    string Action);

public sealed record LearningHistoryEntry(
    string ActivityType,
    string ReferenceId,
    DateTimeOffset OccurredAt);

public sealed record StudyStreak(
    int CurrentDays,
    IReadOnlyList<DateOnly> QualifyingDays);
