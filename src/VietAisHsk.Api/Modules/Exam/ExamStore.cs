using System.Collections.Concurrent;

namespace VietAisHsk.Api.Modules.Exam;

public interface IExamCatalog
{
    IReadOnlyList<ExamDefinition> GetPublishedExams();
    ExamDefinition? GetPublishedExam(string examId);
}

public sealed class BootstrapExamCatalog : IExamCatalog
{
    private static readonly IReadOnlyList<ExamDefinition> Exams =
    [
        new ExamDefinition(
            "bootstrap-hsk3-mini",
            "Bài thi thử HSK 3 bootstrap",
            "HSK 3",
            "bootstrap-2026-01",
            [
                new ExamQuestion("bootstrap-exam-q1", "你好 nghĩa là gì?", ["xin chào", "chào bạn"]),
                new ExamQuestion("bootstrap-exam-q2", "谢谢 nghĩa là gì?", ["cảm ơn"])
            ])
    ];

    public IReadOnlyList<ExamDefinition> GetPublishedExams() => Exams;

    public ExamDefinition? GetPublishedExam(string examId) =>
        Exams.FirstOrDefault(exam => string.Equals(exam.Id, examId, StringComparison.OrdinalIgnoreCase));
}

public interface IExamStore
{
    ExamAttempt Start(string userId, ExamDefinition exam);
    ExamAttempt? Get(string userId, string attemptId);
    ExamAttempt? SubmitAnswer(string userId, string attemptId, ExamAnswerRequest request);
    ExamAttempt? Submit(string userId, string attemptId);
    ExamResult? GetResult(string userId, string attemptId);
}

public sealed class InMemoryExamStore : IExamStore
{
    private readonly ConcurrentDictionary<string, List<ExamEvent>> streams = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, (string UserId, string ExamId, string ContentVersion, IReadOnlyList<ExamQuestion> Questions)> metadata = new(StringComparer.Ordinal);

    public ExamAttempt Start(string userId, ExamDefinition exam)
    {
        var attemptId = Guid.NewGuid().ToString("N");
        var startedAt = DateTimeOffset.UtcNow;
        metadata[attemptId] = (userId, exam.Id, exam.ContentVersion, exam.Questions);
        streams[attemptId] =
        [
            new ExamEvent("ExamStarted", startedAt, exam.Questions.Select(question => question.Id).ToArray())
        ];
        return Rebuild(attemptId);
    }

    public ExamAttempt? Get(string userId, string attemptId) =>
        metadata.TryGetValue(attemptId, out var attemptMetadata) && attemptMetadata.UserId == userId
            ? Rebuild(attemptId)
            : null;

    public ExamAttempt? SubmitAnswer(string userId, string attemptId, ExamAnswerRequest request)
    {
        var current = Get(userId, attemptId);
        if (current is null || current.Status != ExamAttemptStatus.Active || string.IsNullOrWhiteSpace(request.QuestionId) || request.Answer is null)
        {
            return null;
        }

        if (!current.Questions.Any(question => string.Equals(question.Id, request.QuestionId.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        streams[attemptId].Add(new ExamEvent(
            "AnswerSubmitted",
            DateTimeOffset.UtcNow,
            QuestionId: request.QuestionId.Trim(),
            Answer: request.Answer));
        return Rebuild(attemptId);
    }

    public ExamAttempt? Submit(string userId, string attemptId)
    {
        var current = Get(userId, attemptId);
        if (current is null)
        {
            return null;
        }

        if (current.Status == ExamAttemptStatus.Scored)
        {
            return current;
        }

        if (current.Status == ExamAttemptStatus.Submitted)
        {
            CalculateObjectiveScore(attemptId, current);
            return Rebuild(attemptId);
        }

        streams[attemptId].Add(new ExamEvent("ExamSubmitted", DateTimeOffset.UtcNow));
        var submitted = Rebuild(attemptId);
        CalculateObjectiveScore(attemptId, submitted);
        return Rebuild(attemptId);
    }

    public ExamResult? GetResult(string userId, string attemptId)
    {
        var attempt = Get(userId, attemptId);
        return attempt is null ? null : BuildResult(attempt);
    }

    private void CalculateObjectiveScore(string attemptId, ExamAttempt attempt)
    {
        var correct = attempt.Questions.Count(question =>
            attempt.Answers.TryGetValue(question.Id, out var answer)
            && question.AcceptedAnswers.Contains(answer.Trim(), StringComparer.OrdinalIgnoreCase));
        var score = attempt.Questions.Count == 0 ? 0 : (int)Math.Round(correct * 100d / attempt.Questions.Count);
        streams[attemptId].Add(new ExamEvent("ObjectiveScoreCalculated", DateTimeOffset.UtcNow, Score: score));
    }

    private ExamAttempt Rebuild(string attemptId)
    {
        var attemptMetadata = metadata[attemptId];
        var events = streams[attemptId].ToArray();
        var answers = events
            .Where(item => item.Type == "AnswerSubmitted" && item.QuestionId is not null)
            .GroupBy(item => item.QuestionId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Answer ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        var score = events.LastOrDefault(item => item.Type == "ObjectiveScoreCalculated")?.Score;
        var status = score.HasValue
            ? ExamAttemptStatus.Scored
            : events.Any(item => item.Type == "ExamSubmitted") ? ExamAttemptStatus.Submitted : ExamAttemptStatus.Active;
        return new ExamAttempt(attemptId, attemptMetadata.UserId, attemptMetadata.ExamId, attemptMetadata.ContentVersion, status, attemptMetadata.Questions, answers, score, events);
    }

    private static ExamResult BuildResult(ExamAttempt attempt)
    {
        var incorrect = attempt.Questions
            .Where(question => !attempt.Answers.TryGetValue(question.Id, out var answer)
                || !question.AcceptedAnswers.Contains(answer.Trim(), StringComparer.OrdinalIgnoreCase))
            .Select(question => question.Id)
            .ToArray();
        return new ExamResult(attempt.Id, attempt.Status, attempt.ObjectiveScore, attempt.Questions.Count - incorrect.Length, attempt.Questions.Count, incorrect);
    }
}
