namespace VietAisHsk.Api.Modules.Review;

public enum ReviewReason
{
    WrongAnswer,
    RepeatedMistake,
    WritingWeak,
    LowMastery,
    ScheduledReview
}

public sealed record ReviewItem(
    string Id,
    string UserId,
    string KnowledgeType,
    string KnowledgeId,
    ReviewReason Reason,
    int Priority,
    int MistakeCount,
    DateTimeOffset NextReviewAt,
    DateTimeOffset? LastAttemptedAt,
    bool Resolved);

public sealed record ReviewSummary(
    int DueCount,
    int MistakeCount,
    int NeedsReviewCount);

public sealed record StartReviewSessionRequest(IReadOnlyList<string>? ItemIds);

public sealed record ReviewSession(
    string Id,
    string UserId,
    IReadOnlyList<string> ItemIds,
    DateTimeOffset CreatedAt);

public sealed record RecordReviewResultRequest(string? ItemId, bool Correct);
