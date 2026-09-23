using Marten;
using VietAisHsk.Api.Modules.Translation;

namespace VietAisHsk.Api.Infrastructure;

public sealed class MartenTranslationStore(IDocumentStore documentStore) : ITranslationStore
{
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

        using var session = documentStore.LightweightSession();
        session.Store(attempt);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return attempt;
    }

    public TranslationAttempt? Get(string userId, string attemptId)
    {
        using var session = documentStore.QuerySession();
        var attempt = session.LoadAsync<TranslationAttempt>(attemptId).GetAwaiter().GetResult();
        return attempt is not null && string.Equals(attempt.UserId, userId, StringComparison.Ordinal)
            ? attempt
            : null;
    }

    public IReadOnlyList<TranslationAttempt> GetHistory(string userId)
    {
        using var session = documentStore.QuerySession();
        return session.Query<TranslationAttempt>()
            .Where(attempt => attempt.UserId == userId)
            .OrderByDescending(attempt => attempt.SubmittedAt)
            .ToListAsync()
            .GetAwaiter()
            .GetResult();
    }

    public TranslationAttempt? SetFeedback(string userId, string attemptId, TranslationFeedback feedback)
    {
        using var session = documentStore.LightweightSession();
        var attempt = session.LoadAsync<TranslationAttempt>(attemptId).GetAwaiter().GetResult();
        if (attempt is null || !string.Equals(attempt.UserId, userId, StringComparison.Ordinal))
        {
            return null;
        }

        var updated = attempt with { Feedback = feedback };
        session.Store(updated);
        session.SaveChangesAsync().GetAwaiter().GetResult();
        return updated;
    }
}
