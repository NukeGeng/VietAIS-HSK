using System.Collections.Concurrent;

namespace VietAisHsk.Api.Modules.Learning;

public interface ILearningStore
{
    LearningState GetOrCreate(string userId);
    LearningState StartBeginner(string userId);
    LearningState? StartBeginnerStage(string userId, string stageId);
    LearningState? CompleteBeginnerStage(string userId, string stageId, string? nextStageId);
    LearningState SelectHsk(string userId, string levelId);
    LearningState? StartLesson(string userId, string lessonId);
    LearningState? CompleteLesson(string userId, string lessonId);
}

public sealed class InMemoryLearningStore : ILearningStore
{
    private readonly ConcurrentDictionary<string, LearningState> states = new(StringComparer.Ordinal);

    public LearningState GetOrCreate(string userId) => states.GetOrAdd(userId, id => LearningStateProjection.Empty(id));

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

    public LearningState? CompleteLesson(string userId, string lessonId)
    {
        if (!states.ContainsKey(userId))
        {
            return null;
        }

        return Mutate(userId, (state, at) => LearningStateTransitions.CompleteLesson(state, lessonId, at));
    }

    private LearningState Mutate(string userId, Func<LearningState, DateTimeOffset, object?> transition)
    {
        var at = DateTimeOffset.UtcNow;
        return states.AddOrUpdate(
            userId,
            id => Apply(LearningStateProjection.Empty(id, at)),
            (_, current) => Apply(current));

        LearningState Apply(LearningState current)
        {
            var @event = transition(current, at);
            return @event is null ? current : LearningStateProjection.Apply(current, @event);
        }
    }
}

public static class LearningStateTransitions
{
    public static object? StartBeginner(LearningState state, DateTimeOffset at) =>
        state.CurrentTrack == "beginner" && state.CurrentBeginnerStageId is not null
            ? null
            : new LearningTrackStarted(state.UserId, "beginner", at);

    public static object? StartBeginnerStage(LearningState state, string stageId, DateTimeOffset at) =>
        state.CurrentTrack == "beginner"
        && state.CurrentBeginnerStageId == stageId
        && state.StartedBeginnerStageIds.Contains(stageId)
            ? null
            : state.CurrentTrack == "beginner" && state.CurrentBeginnerStageId == stageId
                ? new BeginnerStageStarted(state.UserId, stageId, at)
                : null;

    public static object? CompleteBeginnerStage(LearningState state, string stageId, string? nextStageId, DateTimeOffset at) =>
        state.CurrentTrack == "beginner"
        && state.CurrentBeginnerStageId == stageId
        && state.StartedBeginnerStageIds.Contains(stageId)
        && !state.CompletedBeginnerStageIds.Contains(stageId)
            ? new BeginnerStageCompleted(state.UserId, stageId, nextStageId, at)
            : null;

    public static object? SelectHsk(LearningState state, string levelId, DateTimeOffset at) =>
        state.CurrentTrack == "hsk" && state.SelectedHskLevelId == levelId
            ? null
            : new HskLevelSelected(state.UserId, levelId, at);

    public static object? StartLesson(LearningState state, string lessonId, DateTimeOffset at) =>
        state.CurrentTrack == "hsk"
        && state.CurrentLessonId == lessonId
        && state.StartedLessonIds.Contains(lessonId)
            ? null
            : new LessonStarted(state.UserId, lessonId, at);

    public static object? CompleteLesson(LearningState state, string lessonId, DateTimeOffset at) =>
        state.CompletedLessonIds.Contains(lessonId)
            ? null
            : state.StartedLessonIds.Contains(lessonId)
                ? new LessonCompleted(state.UserId, lessonId, at)
                : null;
}

public static class LearningStateProjection
{
    public static LearningState Empty(string userId, DateTimeOffset? at = null) => new(
        userId,
        CurrentTrack: null,
        SelectedHskLevelId: null,
        CurrentLessonId: null,
        CurrentBeginnerStageId: null,
        StartedBeginnerStageIds: new HashSet<string>(StringComparer.Ordinal),
        CompletedBeginnerStageIds: new HashSet<string>(StringComparer.Ordinal),
        StartedLessonIds: new HashSet<string>(StringComparer.Ordinal),
        CompletedLessonIds: new HashSet<string>(StringComparer.Ordinal),
        UpdatedAt: at ?? DateTimeOffset.UtcNow);

    public static LearningState Replay(string userId, IEnumerable<object> events)
    {
        var state = Empty(userId);
        foreach (var @event in events)
        {
            state = Apply(state, @event);
        }

        return state;
    }

    public static LearningState Apply(LearningState state, object @event)
    {
        return @event switch
        {
            LearningTrackStarted started when BelongsTo(state, started.UserId) && started.TrackKey == "beginner" => state with
            {
                CurrentTrack = "beginner",
                CurrentBeginnerStageId = state.CurrentBeginnerStageId ?? "pinyin",
                StartedBeginnerStageIds = state.StartedBeginnerStageIds.Append(state.CurrentBeginnerStageId ?? "pinyin").ToHashSet(StringComparer.Ordinal),
                UpdatedAt = started.StartedAt
            },
            BeginnerStageStarted started when BelongsTo(state, started.UserId) => state with
            {
                CurrentTrack = "beginner",
                CurrentBeginnerStageId = started.StageId,
                StartedBeginnerStageIds = state.StartedBeginnerStageIds.Append(started.StageId).ToHashSet(StringComparer.Ordinal),
                UpdatedAt = started.StartedAt
            },
            BeginnerStageCompleted completed when BelongsTo(state, completed.UserId) => state with
            {
                CurrentTrack = "beginner",
                CurrentBeginnerStageId = completed.NextStageId,
                StartedBeginnerStageIds = completed.NextStageId is null
                    ? state.StartedBeginnerStageIds
                    : state.StartedBeginnerStageIds.Append(completed.NextStageId).ToHashSet(StringComparer.Ordinal),
                CompletedBeginnerStageIds = state.CompletedBeginnerStageIds.Append(completed.StageId).ToHashSet(StringComparer.Ordinal),
                UpdatedAt = completed.CompletedAt
            },
            HskLevelSelected selected when BelongsTo(state, selected.UserId) => state with
            {
                CurrentTrack = "hsk",
                SelectedHskLevelId = selected.LevelId,
                UpdatedAt = selected.SelectedAt
            },
            LessonStarted started when BelongsTo(state, started.UserId) => state with
            {
                CurrentTrack = "hsk",
                CurrentLessonId = started.LessonId,
                StartedLessonIds = state.StartedLessonIds.Append(started.LessonId).ToHashSet(StringComparer.Ordinal),
                UpdatedAt = started.StartedAt
            },
            LessonCompleted completed when BelongsTo(state, completed.UserId) => state with
            {
                CurrentLessonId = state.CurrentLessonId == completed.LessonId ? null : state.CurrentLessonId,
                CompletedLessonIds = state.CompletedLessonIds.Append(completed.LessonId).ToHashSet(StringComparer.Ordinal),
                UpdatedAt = completed.CompletedAt
            },
            LearningTrackStarted started => throw WrongUser(state, started.UserId),
            BeginnerStageStarted started => throw WrongUser(state, started.UserId),
            BeginnerStageCompleted completed => throw WrongUser(state, completed.UserId),
            HskLevelSelected selected => throw WrongUser(state, selected.UserId),
            LessonStarted started => throw WrongUser(state, started.UserId),
            LessonCompleted completed => throw WrongUser(state, completed.UserId),
            _ => throw new InvalidOperationException($"Unsupported learning event: {@event.GetType().Name}.")
        };
    }

    private static bool BelongsTo(LearningState state, string userId) => state.UserId == userId;

    private static InvalidOperationException WrongUser(LearningState state, string userId) =>
        new($"Learning event for user '{userId}' cannot be applied to user '{state.UserId}'.");
}

public sealed record LearningTrackStarted(string UserId, string TrackKey, DateTimeOffset StartedAt);
public sealed record BeginnerStageStarted(string UserId, string StageId, DateTimeOffset StartedAt);
public sealed record BeginnerStageCompleted(string UserId, string StageId, string? NextStageId, DateTimeOffset CompletedAt);
public sealed record HskLevelSelected(string UserId, string LevelId, DateTimeOffset SelectedAt);
public sealed record LessonStarted(string UserId, string LessonId, DateTimeOffset StartedAt);
public sealed record LessonCompleted(string UserId, string LessonId, DateTimeOffset CompletedAt);
