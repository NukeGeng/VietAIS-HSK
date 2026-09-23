using Marten;
using VietAisHsk.Api.Modules.Practice;

namespace VietAisHsk.Api.Infrastructure;

public sealed class MartenPracticeStore(IDocumentStore documentStore) : IPracticeStore
{
    public PracticeSession? Create(string userId, IReadOnlyList<PracticeQuestion> questions)
    {
        if (questions.Count == 0)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var session = new PracticeSession(
            Guid.NewGuid().ToString("N"),
            userId,
            questions,
            Array.Empty<QuestionAttempt>(),
            PracticeSessionStatus.Active,
            now,
            now);

        using var documentSession = documentStore.LightweightSession();
        documentSession.Store(session);
        documentSession.SaveChangesAsync().GetAwaiter().GetResult();
        return session;
    }

    public PracticeSession? Get(string userId, string sessionId)
    {
        using var documentSession = documentStore.QuerySession();
        var session = documentSession.LoadAsync<PracticeSession>(sessionId).GetAwaiter().GetResult();
        return session?.UserId == userId ? session : null;
    }

    public QuestionAttempt? SubmitAnswer(string userId, string sessionId, string questionId, string answer)
    {
        using var documentSession = documentStore.LightweightSession();
        var current = documentSession.LoadAsync<PracticeSession>(sessionId).GetAwaiter().GetResult();
        if (current is null || current.UserId != userId || current.Status == PracticeSessionStatus.Completed)
        {
            return null;
        }

        var question = current.Questions.FirstOrDefault(item =>
            string.Equals(item.Id, questionId, StringComparison.OrdinalIgnoreCase));
        if (question is null)
        {
            return null;
        }

        var attempt = new QuestionAttempt(question.Id, answer, PracticeGrading.Grade(question, answer), DateTimeOffset.UtcNow);
        var updated = current with
        {
            Attempts = current.Attempts
                .Where(existing => !string.Equals(existing.QuestionId, question.Id, StringComparison.OrdinalIgnoreCase))
                .Append(attempt)
                .ToArray(),
            UpdatedAt = attempt.SubmittedAt
        };
        documentSession.Store(updated);
        documentSession.SaveChangesAsync().GetAwaiter().GetResult();
        return attempt;
    }

    public PracticeSession? Complete(string userId, string sessionId)
    {
        using var documentSession = documentStore.LightweightSession();
        var current = documentSession.LoadAsync<PracticeSession>(sessionId).GetAwaiter().GetResult();
        if (current is null || current.UserId != userId)
        {
            return null;
        }

        if (current.Status == PracticeSessionStatus.Completed)
        {
            return current;
        }

        var completed = current with
        {
            Status = PracticeSessionStatus.Completed,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        documentSession.Store(completed);
        documentSession.SaveChangesAsync().GetAwaiter().GetResult();
        return completed;
    }

    public PracticeSessionResult GetResult(PracticeSession session)
    {
        var attempts = session.Attempts;
        return new PracticeSessionResult(
            session.Id,
            session.Status,
            attempts.Count(attempt => attempt.Result == PracticeResult.Correct),
            attempts.Count(attempt => attempt.Result == PracticeResult.Incorrect),
            attempts.Count(attempt => attempt.Result == PracticeResult.NeedsRetry),
            attempts.Count,
            session.Questions.Count);
    }
}
