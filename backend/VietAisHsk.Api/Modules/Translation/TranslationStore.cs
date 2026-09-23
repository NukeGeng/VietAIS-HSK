using System.Collections.Concurrent;

namespace VietAisHsk.Api.Modules.Translation;

public interface ITranslationCatalog
{
    IReadOnlyList<TranslationExercise> GetExercises();
    TranslationExercise? GetExercise(string id);
}

public sealed class BootstrapTranslationCatalog : ITranslationCatalog
{
    private static readonly IReadOnlyList<TranslationExercise> Exercises =
    [
        new TranslationExercise(
            "bootstrap-translation-1",
            "Tôi thích học tiếng Trung.",
            "我喜欢学中文。",
            "HSK 3")
    ];

    public IReadOnlyList<TranslationExercise> GetExercises() => Exercises;

    public TranslationExercise? GetExercise(string id) =>
        Exercises.FirstOrDefault(exercise => string.Equals(exercise.Id, id, StringComparison.OrdinalIgnoreCase));
}

public interface ITranslationFeedbackGateway
{
    Task<TranslationFeedback?> RequestAsync(TranslationAttempt attempt, CancellationToken cancellationToken);
}

public sealed class UnconfiguredTranslationFeedbackGateway : ITranslationFeedbackGateway
{
    public Task<TranslationFeedback?> RequestAsync(TranslationAttempt attempt, CancellationToken cancellationToken) =>
        Task.FromResult<TranslationFeedback?>(null);
}

public interface ITranslationStore
{
    TranslationAttempt Create(string userId, TranslationExercise exercise, string answerChinese);
    TranslationAttempt? Get(string userId, string attemptId);
    IReadOnlyList<TranslationAttempt> GetHistory(string userId);
    TranslationAttempt? SetFeedback(string userId, string attemptId, TranslationFeedback feedback);
}

public sealed class InMemoryTranslationStore : ITranslationStore
{
    private readonly ConcurrentDictionary<string, TranslationAttempt> attempts = new(StringComparer.Ordinal);

    public TranslationAttempt Create(string userId, TranslationExercise exercise, string answerChinese)
    {
        var attempt = new TranslationAttempt(
            Guid.NewGuid().ToString("N"),
            userId,
            exercise.Id,
            answerChinese,
            exercise.ReferenceChinese,
            DateTimeOffset.UtcNow,
            null);
        attempts[attempt.Id] = attempt;
        return attempt;
    }

    public TranslationAttempt? Get(string userId, string attemptId) =>
        attempts.TryGetValue(attemptId, out var attempt) && attempt.UserId == userId ? attempt : null;

    public IReadOnlyList<TranslationAttempt> GetHistory(string userId) =>
        attempts.Values.Where(attempt => attempt.UserId == userId).OrderByDescending(attempt => attempt.SubmittedAt).ToArray();

    public TranslationAttempt? SetFeedback(string userId, string attemptId, TranslationFeedback feedback)
    {
        if (!attempts.TryGetValue(attemptId, out var attempt) || attempt.UserId != userId)
        {
            return null;
        }

        var updated = attempt with { Feedback = feedback };
        attempts[attemptId] = updated;
        return updated;
    }
}
