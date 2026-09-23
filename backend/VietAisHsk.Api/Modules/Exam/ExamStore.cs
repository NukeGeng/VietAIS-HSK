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
                new ExamQuestion("bootstrap-exam-q1", "你好 nghĩa là gì?", ["xin chào", "chào bạn"], KnowledgeType: "vocabulary", KnowledgeId: "vocab-hello"),
                new ExamQuestion("bootstrap-exam-q2", "谢谢 nghĩa là gì?", ["cảm ơn"], KnowledgeType: "vocabulary", KnowledgeId: "vocab-thanks")
            ]),
        new ExamDefinition(
            "bootstrap-hsk3-subjective",
            "Bài thi viết HSK 3 bootstrap",
            "HSK 3",
            "bootstrap-2026-01",
            [
                new ExamQuestion(
                    "bootstrap-exam-subjective-q1",
                    "Viết một câu tiếng Trung giới thiệu bản thân.",
                    [],
                    "subjective",
                    "writing",
                    "writing-introduction")
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
    SubjectiveGradingResultOutcome? ApplySubjectiveGradingResult(string userId, ApplySubjectiveGradingResultRequest request);
}

public sealed class InMemoryExamStore : IExamStore
{
    private const string SubjectiveSchemaVersion = "subjective-grading-v1";
    private readonly IExamSubjectiveGradingQueue subjectiveQueue;
    private readonly ConcurrentDictionary<string, List<ExamEvent>> streams = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, (string UserId, string ExamId, string ContentVersion, IReadOnlyList<ExamQuestion> Questions)> metadata = new(StringComparer.Ordinal);

    public InMemoryExamStore()
        : this(new InMemoryExamSubjectiveGradingQueue())
    {
    }

    public InMemoryExamStore(IExamSubjectiveGradingQueue subjectiveQueue)
    {
        this.subjectiveQueue = subjectiveQueue;
    }

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

        if (current.Status == ExamAttemptStatus.Active)
        {
            streams[attemptId].Add(new ExamEvent("ExamSubmitted", DateTimeOffset.UtcNow));
        }

        var submitted = Rebuild(attemptId);
        if (!submitted.ObjectiveScore.HasValue)
        {
            CalculateObjectiveScore(attemptId, submitted);
            submitted = Rebuild(attemptId);
        }

        QueueSubjectiveGradingIfNeeded(attemptId, submitted);
        return Rebuild(attemptId);
    }

    public SubjectiveGradingResultOutcome? ApplySubjectiveGradingResult(string userId, ApplySubjectiveGradingResultRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AttemptId) || string.IsNullOrWhiteSpace(request.JobId)) return null;
        var attemptId = request.AttemptId.Trim();
        var current = Get(userId, attemptId);
        if (current is null) return null;
        if (!string.Equals(request.Status, "Completed", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return new SubjectiveGradingResultOutcome(current, false, true);
        }

        var events = streams[attemptId];
        var requested = events.LastOrDefault(item => item.Type == "SubjectiveGradingRequested");
        if (requested?.CorrelationId != request.JobId.Trim())
        {
            return new SubjectiveGradingResultOutcome(current, false, true);
        }

        var existingResult = events.LastOrDefault(item => item.Type is "SubjectiveGradingCompleted" or "SubjectiveGradingFailed");
        if (existingResult?.CorrelationId == request.JobId.Trim())
        {
            return new SubjectiveGradingResultOutcome(current, false, false);
        }

        var completed = string.Equals(request.Status, "Completed", StringComparison.OrdinalIgnoreCase);
        if (completed && request.Score is < 0 or > 100)
        {
            return new SubjectiveGradingResultOutcome(current, false, true);
        }

        events.Add(new ExamEvent(
            completed ? "SubjectiveGradingCompleted" : "SubjectiveGradingFailed",
            DateTimeOffset.UtcNow,
            Answer: request.Feedback?.Trim(),
            Score: completed ? request.Score : null,
            CorrelationId: request.JobId.Trim(),
            SchemaVersion: request.SchemaVersion?.Trim() ?? SubjectiveSchemaVersion));
        return new SubjectiveGradingResultOutcome(Rebuild(attemptId), true, false);
    }

    public ExamResult? GetResult(string userId, string attemptId)
    {
        var attempt = Get(userId, attemptId);
        return attempt is null ? null : BuildResult(attempt);
    }

    private void CalculateObjectiveScore(string attemptId, ExamAttempt attempt)
    {
        var objectiveQuestions = attempt.Questions
            .Where(question => !question.QuestionType.Equals("subjective", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var correct = objectiveQuestions.Count(question =>
            attempt.Answers.TryGetValue(question.Id, out var answer)
            && question.AcceptedAnswers.Contains(answer.Trim(), StringComparer.OrdinalIgnoreCase));
        var score = objectiveQuestions.Length == 0 ? 0 : (int)Math.Round(correct * 100d / objectiveQuestions.Length);
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
        var subjectiveRequested = events.Any(item => item.Type == "SubjectiveGradingRequested");
        var subjectiveCompleted = events.LastOrDefault(item => item.Type == "SubjectiveGradingCompleted");
        var subjectiveFailed = events.LastOrDefault(item => item.Type == "SubjectiveGradingFailed");
        var subjectiveResult = subjectiveCompleted ?? subjectiveFailed;
        var subjectiveStatus = !attemptMetadata.Questions.Any(question => question.QuestionType.Equals("subjective", StringComparison.OrdinalIgnoreCase))
            ? "NotRequired"
            : subjectiveCompleted is not null ? "Completed"
            : subjectiveFailed is not null ? "Failed"
            : subjectiveRequested ? "Pending"
            : "NotRequired";
        var status = score.HasValue
            ? ExamAttemptStatus.Scored
            : events.Any(item => item.Type == "ExamSubmitted") ? ExamAttemptStatus.Submitted : ExamAttemptStatus.Active;
        return new ExamAttempt(
            attemptId,
            attemptMetadata.UserId,
            attemptMetadata.ExamId,
            attemptMetadata.ContentVersion,
            status,
            attemptMetadata.Questions,
            answers,
            score,
            events,
            subjectiveStatus,
            subjectiveResult?.Score,
            subjectiveResult?.Answer);
    }

    private void QueueSubjectiveGradingIfNeeded(string attemptId, ExamAttempt attempt)
    {
        var subjectiveQuestions = attempt.Questions
            .Where(question => question.QuestionType.Equals("subjective", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (subjectiveQuestions.Length == 0
            || attempt.Status != ExamAttemptStatus.Scored
            || streams[attemptId].Any(item => item.Type == "SubjectiveGradingRequested")) return;

        var jobId = $"exam-subjective:{attemptId}:v1";
        var requestedAt = DateTimeOffset.UtcNow;
        streams[attemptId].Add(new ExamEvent(
            "SubjectiveGradingRequested",
            requestedAt,
            QuestionIds: subjectiveQuestions.Select(question => question.Id).ToArray(),
            CorrelationId: jobId,
            SchemaVersion: SubjectiveSchemaVersion));
        subjectiveQueue.Enqueue(new SubjectiveGradingRequestedMessage(
            jobId,
            attempt.UserId,
            attempt.Id,
            attempt.ExamId,
            attempt.ContentVersion,
            subjectiveQuestions.Select(question => question.Id).ToArray(),
            attempt.Answers
                .Where(pair => subjectiveQuestions.Any(question => question.Id.Equals(pair.Key, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
            SubjectiveSchemaVersion,
            requestedAt));
    }

    private static ExamResult BuildResult(ExamAttempt attempt)
    {
        var objectiveQuestions = attempt.Questions
            .Where(question => !question.QuestionType.Equals("subjective", StringComparison.OrdinalIgnoreCase));
        var incorrect = objectiveQuestions
            .Where(question => !attempt.Answers.TryGetValue(question.Id, out var answer)
                || !question.AcceptedAnswers.Contains(answer.Trim(), StringComparer.OrdinalIgnoreCase))
            .Select(question => question.Id)
            .ToArray();
        var total = objectiveQuestions.Count();
        return new ExamResult(
            attempt.Id,
            attempt.Status,
            attempt.ObjectiveScore,
            total - incorrect.Length,
            total,
            incorrect,
            attempt.SubjectiveGradingStatus,
            attempt.SubjectiveScore,
            attempt.SubjectiveFeedback);
    }
}
