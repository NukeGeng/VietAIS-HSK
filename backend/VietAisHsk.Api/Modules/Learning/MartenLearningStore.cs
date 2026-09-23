using Marten;

namespace VietAisHsk.Api.Modules.Learning;

public sealed class MartenLearningStore(IDocumentStore documentStore) : ILearningStore
{
    public LearningState GetOrCreate(string userId) => Read(userId);

    public LearningState StartBeginner(string userId) =>
        Mutate(userId, LearningStateTransitions.StartBeginner);

    public LearningState? StartBeginnerStage(string userId, string stageId) =>
        Mutate(userId, (state, at) => LearningStateTransitions.StartBeginnerStage(state, stageId, at));

    public LearningState? CompleteBeginnerStage(string userId, string stageId, string? nextStageId) =>
        Mutate(userId, (state, at) => LearningStateTransitions.CompleteBeginnerStage(state, stageId, nextStageId, at));

    public LearningState SelectHsk(string userId, string levelId) =>
        Mutate(userId, (state, at) => LearningStateTransitions.SelectHsk(state, levelId, at));

    public LearningState? StartLesson(string userId, string lessonId) =>
        Mutate(userId, (state, at) => LearningStateTransitions.StartLesson(state, lessonId, at));

    public LearningState? CompleteLesson(string userId, string lessonId) =>
        Mutate(userId, (state, at) => LearningStateTransitions.CompleteLesson(state, lessonId, at));

    private LearningState Read(string userId)
    {
        using var session = documentStore.QuerySession();
        var events = session.Events.FetchStreamAsync(StreamId(userId), long.MaxValue).GetAwaiter().GetResult();
        return LearningStateProjection.Replay(userId, events.Select(item => item.Data));
    }

    private LearningState Mutate(string userId, Func<LearningState, DateTimeOffset, object?> transition)
    {
        var streamId = StreamId(userId);
        IReadOnlyList<JasperFx.Events.IEvent> events;
        using (var readSession = documentStore.QuerySession())
        {
            events = readSession.Events.FetchStreamAsync(streamId, long.MaxValue).GetAwaiter().GetResult();
        }

        var current = LearningStateProjection.Replay(userId, events.Select(item => item.Data));
        var @event = transition(current, DateTimeOffset.UtcNow);
        if (@event is null)
        {
            return current;
        }

        var streamVersion = events.Count;
        using var session = documentStore.LightweightSession();
        if (streamVersion == 0)
        {
            session.Events.StartStream(streamId, @event);
        }
        else
        {
            // Marten's expected-version append takes the next 1-based event version.
            session.Events.Append(streamId, streamVersion + 1L, @event);
        }

        session.SaveChangesAsync().GetAwaiter().GetResult();
        return LearningStateProjection.Apply(current, @event);
    }

    private static string StreamId(string userId) => $"LearningPath-{userId}";
}
