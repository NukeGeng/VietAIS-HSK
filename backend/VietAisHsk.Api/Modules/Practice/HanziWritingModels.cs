using VietAisHsk.Api.Modules.Curriculum;

namespace VietAisHsk.Api.Modules.Practice;

public enum HanziWritingMode
{
    Guided,
    Trace,
    Recall
}

public enum HanziWritingStrokeResult
{
    Correct,
    NeedsRetry,
    Incorrect
}

public sealed record HanziPoint(double X, double Y);

public sealed record HanziInputStroke(IReadOnlyList<HanziPoint> Points);

public sealed record HanziStrokeAttemptResult(
    int Order,
    HanziWritingStrokeResult Result,
    string Feedback,
    DateTimeOffset EvaluatedAt);

public sealed record HanziWritingAttempt(
    string Id,
    string UserId,
    string HanziId,
    int StrokeCount,
    string StrokeSourceVersion,
    HanziWritingMode Mode,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<HanziInputStroke> AcceptedStrokes,
    IReadOnlyList<HanziStrokeAttemptResult> StrokeResults,
    PracticeResult? OverallResult);

public sealed record StartHanziWritingAttemptRequest(string? Mode);

public sealed record SubmitHanziStrokeRequest(IReadOnlyList<HanziPoint>? Points);

public sealed record HanziWritingStrokeSubmission(
    HanziWritingAttempt Attempt,
    HanziStrokeAttemptResult Result);

public sealed record HanziWritingAttemptView(
    string Id,
    string HanziId,
    int StrokeCount,
    int AcceptedStrokeCount,
    string StrokeSourceVersion,
    HanziWritingMode Mode,
    string Status,
    IReadOnlyList<HanziStrokeAttemptResult> StrokeResults,
    PracticeResult? OverallResult,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);

public static class HanziWritingAttemptMapper
{
    public static HanziWritingAttemptView ToView(HanziWritingAttempt attempt) => new(
        attempt.Id,
        attempt.HanziId,
        attempt.StrokeCount,
        attempt.AcceptedStrokes.Count,
        attempt.StrokeSourceVersion,
        attempt.Mode,
        attempt.CompletedAt is null ? "Active" : "Completed",
        attempt.StrokeResults,
        attempt.OverallResult,
        attempt.StartedAt,
        attempt.CompletedAt);
}

public static class HanziWritingValidator
{
    public static (HanziWritingStrokeResult Result, string Feedback) Validate(
        HanziStroke reference,
        IReadOnlyList<HanziPoint> points,
        HanziWritingMode mode)
    {
        if (points.Count < 2)
        {
            return (HanziWritingStrokeResult.NeedsRetry, "Nét cần ít nhất hai điểm.");
        }

        if (points.Any(point => point.X < 0 || point.X > 100 || point.Y < 0 || point.Y > 100))
        {
            return (HanziWritingStrokeResult.NeedsRetry, "Nét đang nằm ngoài vùng luyện viết.");
        }

        var referenceEndpoints = ParseEndpoints(reference.Path);
        if (referenceEndpoints is null)
        {
            return (HanziWritingStrokeResult.Incorrect, "Chưa có hình học tham chiếu hợp lệ cho nét này.");
        }

        var tolerance = mode switch
        {
            HanziWritingMode.Trace => 26d,
            HanziWritingMode.Guided => 21d,
            _ => 17d
        };
        var start = points[0];
        var end = points[^1];
        var startDistance = Distance(start, referenceEndpoints.Value.Start);
        var endDistance = Distance(end, referenceEndpoints.Value.End);
        if (startDistance > tolerance)
        {
            return (HanziWritingStrokeResult.NeedsRetry, "Điểm bắt đầu chưa đúng vị trí.");
        }

        if (endDistance > tolerance)
        {
            return (HanziWritingStrokeResult.NeedsRetry, "Điểm kết thúc chưa đúng vị trí.");
        }

        var referenceVector = Vector(referenceEndpoints.Value.Start, referenceEndpoints.Value.End);
        var inputVector = Vector(start, end);
        if (Dot(referenceVector, inputVector) < 0)
        {
            return (HanziWritingStrokeResult.NeedsRetry, "Hướng nét đang bị ngược.");
        }

        return (HanziWritingStrokeResult.Correct, "Nét đúng. Tiếp tục nét tiếp theo.");
    }

    private static (HanziPoint Start, HanziPoint End)? ParseEndpoints(string path)
    {
        var values = System.Text.RegularExpressions.Regex.Matches(path, @"-?\d+(?:\.\d+)?")
            .Select(match => double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();
        return values.Length < 4
            ? null
            : (new HanziPoint(values[0], values[1]), new HanziPoint(values[^2], values[^1]));
    }

    private static double Distance(HanziPoint first, HanziPoint second) =>
        Math.Sqrt(Math.Pow(first.X - second.X, 2) + Math.Pow(first.Y - second.Y, 2));

    private static (double X, double Y) Vector(HanziPoint start, HanziPoint end) =>
        (end.X - start.X, end.Y - start.Y);

    private static double Dot((double X, double Y) first, (double X, double Y) second) =>
        first.X * second.X + first.Y * second.Y;
}

public static class HanziWritingAttemptOperations
{
    public static HanziWritingStrokeSubmission SubmitStroke(
        HanziWritingAttempt current,
        HanziStrokeSet reference,
        IReadOnlyList<HanziPoint> points)
    {
        var order = current.AcceptedStrokes.Count + 1;
        var now = DateTimeOffset.UtcNow;
        if (order > reference.StrokeCount)
        {
            var extra = new HanziStrokeAttemptResult(
                order,
                HanziWritingStrokeResult.Incorrect,
                "Chữ này đã đủ số nét.",
                now);
            return new HanziWritingStrokeSubmission(current with
            {
                StrokeResults = ReplaceResult(current.StrokeResults, extra)
            }, extra);
        }

        var referenceStroke = reference.Strokes.First(stroke => stroke.Order == order);
        var validation = HanziWritingValidator.Validate(referenceStroke, points, current.Mode);
        var result = new HanziStrokeAttemptResult(order, validation.Result, validation.Feedback, now);
        var results = ReplaceResult(current.StrokeResults, result);
        var accepted = validation.Result == HanziWritingStrokeResult.Correct
            ? current.AcceptedStrokes.Append(new HanziInputStroke(points.ToArray())).ToArray()
            : current.AcceptedStrokes;
        return new HanziWritingStrokeSubmission(current with
        {
            AcceptedStrokes = accepted,
            StrokeResults = results
        }, result);
    }

    public static HanziWritingAttempt Complete(HanziWritingAttempt current, HanziStrokeSet reference)
    {
        if (current.CompletedAt is not null)
        {
            return current;
        }

        var complete = current.AcceptedStrokes.Count == reference.StrokeCount
            && current.StrokeResults.Count == reference.StrokeCount
            && current.StrokeResults.All(result => result.Result == HanziWritingStrokeResult.Correct);
        return current with
        {
            CompletedAt = DateTimeOffset.UtcNow,
            OverallResult = complete ? PracticeResult.Correct : PracticeResult.NeedsRetry
        };
    }

    private static IReadOnlyList<HanziStrokeAttemptResult> ReplaceResult(
        IReadOnlyList<HanziStrokeAttemptResult> existing,
        HanziStrokeAttemptResult result) => existing
            .Where(item => item.Order != result.Order)
            .Append(result)
            .OrderBy(item => item.Order)
            .ToArray();
}

public interface IHanziWritingStore
{
    HanziWritingAttempt? Start(string userId, string hanziId, HanziWritingMode mode);
    HanziWritingAttempt? Get(string userId, string attemptId);
    HanziWritingStrokeSubmission? SubmitStroke(string userId, string attemptId, IReadOnlyList<HanziPoint> points);
    HanziWritingAttempt? Complete(string userId, string attemptId);
}

public sealed class InMemoryHanziWritingStore(IHanziCatalog catalog) : IHanziWritingStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, HanziWritingAttempt> attempts = new(StringComparer.Ordinal);

    public HanziWritingAttempt? Start(string userId, string hanziId, HanziWritingMode mode)
    {
        var reference = catalog.GetStrokes(hanziId);
        if (reference is null)
        {
            return null;
        }

        var attempt = new HanziWritingAttempt(
            Guid.NewGuid().ToString("N"),
            userId,
            reference.HanziId,
            reference.StrokeCount,
            reference.SourceVersion,
            mode,
            DateTimeOffset.UtcNow,
            null,
            [],
            [],
            null);
        attempts[attempt.Id] = attempt;
        return attempt;
    }

    public HanziWritingAttempt? Get(string userId, string attemptId) =>
        attempts.TryGetValue(attemptId, out var attempt) && attempt.UserId == userId ? attempt : null;

    public HanziWritingStrokeSubmission? SubmitStroke(string userId, string attemptId, IReadOnlyList<HanziPoint> points)
    {
        var current = Get(userId, attemptId);
        if (current is null || current.CompletedAt is not null)
        {
            return null;
        }

        var reference = catalog.GetStrokes(current.HanziId);
        if (reference is null || reference.SourceVersion != current.StrokeSourceVersion)
        {
            return null;
        }

        var submission = HanziWritingAttemptOperations.SubmitStroke(current, reference, points);
        attempts[attemptId] = submission.Attempt;
        return submission;
    }

    public HanziWritingAttempt? Complete(string userId, string attemptId)
    {
        var current = Get(userId, attemptId);
        if (current is null || current.CompletedAt is not null)
        {
            return current;
        }

        var reference = catalog.GetStrokes(current.HanziId);
        if (reference is null || reference.SourceVersion != current.StrokeSourceVersion)
        {
            return null;
        }

        var completed = HanziWritingAttemptOperations.Complete(current, reference);
        attempts[attemptId] = completed;
        return completed;
    }
}
