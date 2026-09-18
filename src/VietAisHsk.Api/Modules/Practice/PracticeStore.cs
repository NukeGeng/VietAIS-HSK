using System.Collections.Concurrent;

namespace VietAisHsk.Api.Modules.Practice;

public interface IPracticeQuestionReader
{
    IReadOnlyList<PracticeQuestion> GetPublishedQuestions(IReadOnlyList<string> questionIds);
}

// Temporary contract fixture. Official question data belongs to Content and is not embedded here.
public sealed class BootstrapPracticeQuestionReader : IPracticeQuestionReader
{
    private static readonly IReadOnlyDictionary<string, PracticeQuestion> Questions =
        new Dictionary<string, PracticeQuestion>(StringComparer.OrdinalIgnoreCase)
        {
            ["bootstrap-vocab-hello"] = new("bootstrap-vocab-hello", "vocabulary", "Dịch 你好 sang tiếng Việt.", ["xin chào", "chào bạn"]),
            ["bootstrap-tone-ma"] = new("bootstrap-tone-ma", "tone", "Chọn âm đúng cho mā.", ["1", "thanh 1"])
        };

    public IReadOnlyList<PracticeQuestion> GetPublishedQuestions(IReadOnlyList<string> questionIds) =>
        questionIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(Questions.ContainsKey)
            .Select(id => Questions[id])
            .ToArray();
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
