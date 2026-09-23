namespace VietAisHsk.Api.Modules.Exam;

public sealed record SubjectiveGradingRequestedMessage(
    string JobId,
    string UserId,
    string AttemptId,
    string ExamId,
    string ContentVersion,
    IReadOnlyList<string> QuestionIds,
    IReadOnlyDictionary<string, string> Answers,
    string SchemaVersion,
    DateTimeOffset RequestedAt);

public sealed record ApplySubjectiveGradingResultRequest(
    string? JobId,
    string? AttemptId,
    string? Status,
    int? Score,
    string? Feedback,
    string? SchemaVersion,
    string? UserId = null);

public sealed record SubjectiveGradingResultOutcome(ExamAttempt Attempt, bool Applied, bool Conflict);

public interface IExamSubjectiveGradingQueue
{
    IReadOnlyList<SubjectiveGradingRequestedMessage> Messages { get; }
    bool Enqueue(SubjectiveGradingRequestedMessage message);
}

public sealed class InMemoryExamSubjectiveGradingQueue : IExamSubjectiveGradingQueue
{
    private readonly object gate = new();
    private readonly List<SubjectiveGradingRequestedMessage> messages = [];

    public IReadOnlyList<SubjectiveGradingRequestedMessage> Messages
    {
        get
        {
            lock (gate)
            {
                return messages.ToArray();
            }
        }
    }

    public bool Enqueue(SubjectiveGradingRequestedMessage message)
    {
        lock (gate)
        {
            if (messages.Any(existing => existing.JobId.Equals(message.JobId, StringComparison.Ordinal))) return false;
            messages.Add(message);
            return true;
        }
    }
}
