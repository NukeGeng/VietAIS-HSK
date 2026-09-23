using Marten;
using JasperFx.Events;

namespace VietAisHsk.Api.Modules.Exam;

public sealed record ExamAttemptMetadata(
    string Id,
    string UserId,
    string ExamId,
    string ContentVersion,
    IReadOnlyList<ExamQuestion> Questions);

public sealed class MartenExamStore(
    IDocumentStore documentStore,
    IExamSubjectiveGradingQueue subjectiveQueue) : IExamStore
{
    private const string SubjectiveSchemaVersion = "subjective-grading-v1";

    public ExamAttempt Start(string userId, ExamDefinition exam)
    {
        var attemptId = Guid.NewGuid().ToString("N");
        var startedAt = DateTimeOffset.UtcNow;
        var metadata = new ExamAttemptMetadata(
            attemptId,
            userId,
            exam.Id,
            exam.ContentVersion,
            exam.Questions);
        var started = new ExamEvent(
            "ExamStarted",
            startedAt,
            exam.Questions.Select(question => question.Id).ToArray());

        using var session = documentStore.LightweightSession();
        session.Store(metadata);
        session.Events.StartStream(StreamId(attemptId), started);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return Build(metadata, [started]);
    }

    public ExamAttempt? Get(string userId, string attemptId)
    {
        var metadata = LoadMetadata(attemptId);
        return metadata is null || metadata.UserId != userId ? null : Rebuild(metadata);
    }

    public ExamAttempt? SubmitAnswer(string userId, string attemptId, ExamAnswerRequest request)
    {
        var current = Get(userId, attemptId);
        if (current is null || current.Status != ExamAttemptStatus.Active
            || string.IsNullOrWhiteSpace(request.QuestionId) || request.Answer is null)
        {
            return null;
        }

        var questionId = request.QuestionId.Trim();
        if (!current.Questions.Any(question => string.Equals(question.Id, questionId, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        if (current.Answers.TryGetValue(questionId, out var existingAnswer)
            && string.Equals(existingAnswer, request.Answer, StringComparison.Ordinal))
        {
            return current;
        }

        var events = ReadEvents(attemptId);
        var answerSubmitted = new ExamEvent(
            "AnswerSubmitted",
            DateTimeOffset.UtcNow,
            QuestionId: questionId,
            Answer: request.Answer);
        using var session = documentStore.LightweightSession();
        session.Events.Append(StreamId(attemptId), events.Count + 1L, answerSubmitted);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return Rebuild(LoadMetadata(attemptId)!);
    }

    public ExamAttempt? Submit(string userId, string attemptId)
    {
        var metadata = LoadMetadata(attemptId);
        if (metadata is null || metadata.UserId != userId)
        {
            return null;
        }

        var currentEvents = ReadEvents(attemptId);
        var current = Build(metadata, currentEvents);
        if (current.Status == ExamAttemptStatus.Scored)
        {
            return current;
        }

        var events = currentEvents.ToList();
        if (current.Status == ExamAttemptStatus.Active)
        {
            events.Add(new ExamEvent("ExamSubmitted", DateTimeOffset.UtcNow));
        }

        if (!events.Any(item => item.Type == "ObjectiveScoreCalculated"))
        {
            var projected = Build(metadata, events);
            var score = CalculateObjectiveScore(metadata.Questions, projected.Answers);
            events.Add(new ExamEvent("ObjectiveScoreCalculated", DateTimeOffset.UtcNow, Score: score));
        }

        var submitted = Build(metadata, events);
        var subjectiveQuestions = GetSubjectiveQuestions(submitted);
        var shouldQueue = subjectiveQuestions.Length > 0
            && submitted.Status == ExamAttemptStatus.Scored
            && !events.Any(item => item.Type == "SubjectiveGradingRequested");
        if (shouldQueue)
        {
            var jobId = SubjectiveJobId(submitted.Id);
            var requestedAt = DateTimeOffset.UtcNow;
            events.Add(new ExamEvent(
                "SubjectiveGradingRequested",
                requestedAt,
                QuestionIds: subjectiveQuestions.Select(question => question.Id).ToArray(),
                CorrelationId: jobId,
                SchemaVersion: SubjectiveSchemaVersion));
            submitted = Build(metadata, events);
            PersistNewEvents(attemptId, currentEvents.Count, events.Skip(currentEvents.Count));
            subjectiveQueue.Enqueue(BuildSubjectiveMessage(submitted, subjectiveQuestions, jobId, requestedAt));
            return Rebuild(metadata);
        }

        PersistNewEvents(attemptId, currentEvents.Count, events.Skip(currentEvents.Count));
        return Rebuild(metadata);
    }

    public SubjectiveGradingResultOutcome? ApplySubjectiveGradingResult(
        string userId,
        ApplySubjectiveGradingResultRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AttemptId) || string.IsNullOrWhiteSpace(request.JobId))
        {
            return null;
        }

        var attemptId = request.AttemptId.Trim();
        var metadata = LoadMetadata(attemptId);
        if (metadata is null || metadata.UserId != userId)
        {
            return null;
        }

        var events = ReadEvents(attemptId).ToList();
        var current = Build(metadata, events);
        var jobId = request.JobId.Trim();
        if (!string.Equals(request.Status, "Completed", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return new SubjectiveGradingResultOutcome(current, false, true);
        }

        var requested = events.LastOrDefault(item => item.Type == "SubjectiveGradingRequested");
        if (!string.Equals(requested?.CorrelationId, jobId, StringComparison.Ordinal))
        {
            return new SubjectiveGradingResultOutcome(current, false, true);
        }

        var existingResult = events.LastOrDefault(item =>
            item.Type is "SubjectiveGradingCompleted" or "SubjectiveGradingFailed");
        if (string.Equals(existingResult?.CorrelationId, jobId, StringComparison.Ordinal))
        {
            return new SubjectiveGradingResultOutcome(current, false, false);
        }

        var completed = string.Equals(request.Status, "Completed", StringComparison.OrdinalIgnoreCase);
        if (completed && request.Score is < 0 or > 100)
        {
            return new SubjectiveGradingResultOutcome(current, false, true);
        }

        var resultEvent = new ExamEvent(
            completed ? "SubjectiveGradingCompleted" : "SubjectiveGradingFailed",
            DateTimeOffset.UtcNow,
            Answer: request.Feedback?.Trim(),
            Score: completed ? request.Score : null,
            CorrelationId: jobId,
            SchemaVersion: request.SchemaVersion?.Trim() ?? SubjectiveSchemaVersion);
        PersistNewEvents(attemptId, events.Count, [resultEvent]);
        return new SubjectiveGradingResultOutcome(Rebuild(metadata), true, false);
    }

    public ExamResult? GetResult(string userId, string attemptId)
    {
        var attempt = Get(userId, attemptId);
        return attempt is null ? null : BuildResult(attempt);
    }

    private ExamAttempt Rebuild(ExamAttemptMetadata metadata) => Build(metadata, ReadEvents(metadata.Id));

    private ExamAttemptMetadata? LoadMetadata(string attemptId)
    {
        using var session = documentStore.QuerySession();
        return session.LoadAsync<ExamAttemptMetadata>(attemptId).GetAwaiter().GetResult();
    }

    private IReadOnlyList<ExamEvent> ReadEvents(string attemptId)
    {
        using var session = documentStore.QuerySession();
        return session.Events
            .FetchStreamAsync(StreamId(attemptId), long.MaxValue)
            .GetAwaiter()
            .GetResult()
            .Select(item => (ExamEvent)item.Data)
            .ToArray();
    }

    private void PersistNewEvents(string attemptId, int existingCount, IEnumerable<ExamEvent> newEvents)
    {
        using var session = documentStore.LightweightSession();
        var version = existingCount;
        foreach (var @event in newEvents)
        {
            version++;
            session.Events.Append(StreamId(attemptId), version, @event);
        }

        session.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private static ExamAttempt Build(ExamAttemptMetadata metadata, IReadOnlyList<ExamEvent> events)
    {
        var answers = events
            .Where(item => item.Type == "AnswerSubmitted" && item.QuestionId is not null)
            .GroupBy(item => item.QuestionId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Answer ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        var score = events.LastOrDefault(item => item.Type == "ObjectiveScoreCalculated")?.Score;
        var subjectiveRequested = events.Any(item => item.Type == "SubjectiveGradingRequested");
        var subjectiveCompleted = events.LastOrDefault(item => item.Type == "SubjectiveGradingCompleted");
        var subjectiveFailed = events.LastOrDefault(item => item.Type == "SubjectiveGradingFailed");
        var subjectiveResult = subjectiveCompleted ?? subjectiveFailed;
        var hasSubjective = metadata.Questions.Any(IsSubjective);
        var subjectiveStatus = !hasSubjective
            ? "NotRequired"
            : subjectiveCompleted is not null ? "Completed"
            : subjectiveFailed is not null ? "Failed"
            : subjectiveRequested ? "Pending"
            : "NotRequired";
        var status = score.HasValue
            ? ExamAttemptStatus.Scored
            : events.Any(item => item.Type == "ExamSubmitted") ? ExamAttemptStatus.Submitted : ExamAttemptStatus.Active;
        return new ExamAttempt(
            metadata.Id,
            metadata.UserId,
            metadata.ExamId,
            metadata.ContentVersion,
            status,
            metadata.Questions,
            answers,
            score,
            events,
            subjectiveStatus,
            subjectiveResult?.Score,
            subjectiveResult?.Answer);
    }

    private static int CalculateObjectiveScore(
        IReadOnlyList<ExamQuestion> questions,
        IReadOnlyDictionary<string, string> answers)
    {
        var objectiveQuestions = questions.Where(question => !IsSubjective(question)).ToArray();
        var correct = objectiveQuestions.Count(question =>
            answers.TryGetValue(question.Id, out var answer)
            && question.AcceptedAnswers.Contains(answer.Trim(), StringComparer.OrdinalIgnoreCase));
        return objectiveQuestions.Length == 0 ? 0 : (int)Math.Round(correct * 100d / objectiveQuestions.Length);
    }

    private static ExamResult BuildResult(ExamAttempt attempt)
    {
        var objectiveQuestions = attempt.Questions.Where(question => !IsSubjective(question)).ToArray();
        var incorrect = objectiveQuestions
            .Where(question => !attempt.Answers.TryGetValue(question.Id, out var answer)
                || !question.AcceptedAnswers.Contains(answer.Trim(), StringComparer.OrdinalIgnoreCase))
            .Select(question => question.Id)
            .ToArray();
        return new ExamResult(
            attempt.Id,
            attempt.Status,
            attempt.ObjectiveScore,
            objectiveQuestions.Length - incorrect.Length,
            objectiveQuestions.Length,
            incorrect,
            attempt.SubjectiveGradingStatus,
            attempt.SubjectiveScore,
            attempt.SubjectiveFeedback);
    }

    private static ExamQuestion[] GetSubjectiveQuestions(ExamAttempt attempt) =>
        attempt.Questions.Where(IsSubjective).ToArray();

    private static SubjectiveGradingRequestedMessage BuildSubjectiveMessage(
        ExamAttempt attempt,
        IReadOnlyList<ExamQuestion> questions,
        string jobId,
        DateTimeOffset requestedAt) =>
        new(
            jobId,
            attempt.UserId,
            attempt.Id,
            attempt.ExamId,
            attempt.ContentVersion,
            questions.Select(question => question.Id).ToArray(),
            attempt.Answers
                .Where(pair => questions.Any(question => question.Id.Equals(pair.Key, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
            SubjectiveSchemaVersion,
            requestedAt);

    private static bool IsSubjective(ExamQuestion question) =>
        question.QuestionType.Equals("subjective", StringComparison.OrdinalIgnoreCase);

    private static string SubjectiveJobId(string attemptId) => $"exam-subjective:{attemptId}:v1";

    private static string StreamId(string attemptId) => $"ExamAttempt-{attemptId}";
}
