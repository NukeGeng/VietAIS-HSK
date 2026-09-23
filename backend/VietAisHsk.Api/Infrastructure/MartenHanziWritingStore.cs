using Marten;
using VietAisHsk.Api.Modules.Curriculum;
using VietAisHsk.Api.Modules.Practice;

namespace VietAisHsk.Api.Infrastructure;

public sealed class MartenHanziWritingStore(IDocumentStore documentStore, IHanziCatalog catalog) : IHanziWritingStore
{
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
        using var session = documentStore.LightweightSession();
        session.Store(attempt);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return attempt;
    }

    public HanziWritingAttempt? Get(string userId, string attemptId)
    {
        using var session = documentStore.QuerySession();
        var attempt = session.LoadAsync<HanziWritingAttempt>(attemptId).GetAwaiter().GetResult();
        return attempt?.UserId == userId ? attempt : null;
    }

    public HanziWritingStrokeSubmission? SubmitStroke(string userId, string attemptId, IReadOnlyList<HanziPoint> points)
    {
        using var session = documentStore.LightweightSession();
        var current = session.LoadAsync<HanziWritingAttempt>(attemptId).GetAwaiter().GetResult();
        if (current is null || current.UserId != userId || current.CompletedAt is not null)
        {
            return null;
        }

        var reference = catalog.GetStrokes(current.HanziId);
        if (reference is null || reference.SourceVersion != current.StrokeSourceVersion)
        {
            return null;
        }

        var submission = HanziWritingAttemptOperations.SubmitStroke(current, reference, points);
        session.Store(submission.Attempt);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return submission;
    }

    public HanziWritingAttempt? Complete(string userId, string attemptId)
    {
        using var session = documentStore.LightweightSession();
        var current = session.LoadAsync<HanziWritingAttempt>(attemptId).GetAwaiter().GetResult();
        if (current is null || current.UserId != userId)
        {
            return null;
        }

        if (current.CompletedAt is not null)
        {
            return current;
        }

        var reference = catalog.GetStrokes(current.HanziId);
        if (reference is null || reference.SourceVersion != current.StrokeSourceVersion)
        {
            return null;
        }

        var completed = HanziWritingAttemptOperations.Complete(current, reference);
        session.Store(completed);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return completed;
    }
}
