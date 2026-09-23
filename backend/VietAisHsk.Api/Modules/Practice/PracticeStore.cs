using System.Collections.Concurrent;
using VietAisHsk.Api.Modules.Content;

namespace VietAisHsk.Api.Modules.Practice;

public interface IPracticeQuestionReader
{
    IReadOnlyList<PracticeQuestion> GetPublishedQuestions(string? type = null);
    IReadOnlyList<PracticeQuestion> GetPublishedQuestions(IReadOnlyList<string> questionIds);
}

// Temporary contract fixture. Official question data belongs to Content and is not embedded here.
public sealed class ContentPracticeQuestionReader(IQuestionBank questionBank) : IPracticeQuestionReader
{
    public IReadOnlyList<PracticeQuestion> GetPublishedQuestions(IReadOnlyList<string> questionIds) =>
        questionBank.GetPublishedQuestions(questionIds).Select(ToPracticeQuestion).ToArray();

    public IReadOnlyList<PracticeQuestion> GetPublishedQuestions(string? type = null) =>
        questionBank.GetPublishedQuestions(type).Select(ToPracticeQuestion).ToArray();

    private static PracticeQuestion ToPracticeQuestion(ContentQuestion question) =>
        new(question.Id, question.Type, question.Prompt, question.AcceptedAnswers, question.Status, question.Options, question.ContentVersion);
}

public interface IPracticeStore
{
    PracticeSession? Create(string userId, IReadOnlyList<PracticeQuestion> questions);
    PracticeSession? Get(string userId, string sessionId);
    QuestionAttempt? SubmitAnswer(string userId, string sessionId, string questionId, string answer);
    PracticeSession? Complete(string userId, string sessionId);
    PracticeSessionResult GetResult(PracticeSession session);
}

public sealed class InMemoryPracticeStore : IPracticeStore
{
    private readonly ConcurrentDictionary<string, PracticeSession> sessions = new(StringComparer.Ordinal);

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
        sessions[session.Id] = session;
        return session;
    }

    public PracticeSession? Get(string userId, string sessionId) =>
        sessions.TryGetValue(sessionId, out var session) && session.UserId == userId ? session : null;

    public QuestionAttempt? SubmitAnswer(string userId, string sessionId, string questionId, string answer)
    {
        var session = Get(userId, sessionId);
        if (session is null || session.Status == PracticeSessionStatus.Completed)
        {
            return null;
        }

        var question = session.Questions.FirstOrDefault(item =>
            string.Equals(item.Id, questionId, StringComparison.OrdinalIgnoreCase));
        if (question is null)
        {
            return null;
        }

        var attempt = new QuestionAttempt(question.Id, answer, PracticeGrading.Grade(question, answer), DateTimeOffset.UtcNow);
        var attempts = session.Attempts
            .Where(existing => !string.Equals(existing.QuestionId, question.Id, StringComparison.OrdinalIgnoreCase))
            .Append(attempt)
            .ToArray();
        sessions[session.Id] = session with { Attempts = attempts, UpdatedAt = attempt.SubmittedAt };
        return attempt;
    }

    public PracticeSession? Complete(string userId, string sessionId)
    {
        var session = Get(userId, sessionId);
        if (session is null)
        {
            return null;
        }

        if (session.Status == PracticeSessionStatus.Completed)
        {
            return session;
        }

        var completed = session with
        {
            Status = PracticeSessionStatus.Completed,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        sessions[session.Id] = completed;
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
