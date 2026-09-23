namespace VietAisHsk.Api.Modules.Review;

public interface IReviewClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemReviewClock : IReviewClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public static class ReviewScheduler
{
    public static DateTimeOffset NextAfterIncorrect(DateTimeOffset evaluatedAt) => evaluatedAt;

    public static DateTimeOffset NextAfterCorrect(DateTimeOffset evaluatedAt) => evaluatedAt.AddDays(1);
}
