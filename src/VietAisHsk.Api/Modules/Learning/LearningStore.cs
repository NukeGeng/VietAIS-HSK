using System.Collections.Concurrent;
using VietAisHsk.Api.Modules.Curriculum;

namespace VietAisHsk.Api.Modules.Learning;

public interface ILearningStore
{
    LearningState GetOrCreate(string userId);
    LearningState StartBeginner(string userId);
    LearningState SelectHsk(string userId, string levelId);
    LearningState? StartLesson(string userId, string lessonId);
    LearningState? CompleteLesson(string userId, string lessonId);
}

public sealed class InMemoryLearningStore : ILearningStore
{
    private readonly ConcurrentDictionary<string, LearningState> states = new(StringComparer.Ordinal);

    public LearningState GetOrCreate(string userId) => states.GetOrAdd(userId, static id => NewState(id));

    public LearningState StartBeginner(string userId)
    {
        return states.AddOrUpdate(
            userId,
            _ => NewState(userId) with
            {
                CurrentTrack = "beginner",
                CurrentBeginnerStageId = "pinyin",
                UpdatedAt = DateTimeOffset.UtcNow
            },
            (_, current) => current with
            {
                CurrentTrack = "beginner",
                CurrentBeginnerStageId = current.CurrentBeginnerStageId ?? "pinyin",
                UpdatedAt = DateTimeOffset.UtcNow
            });
    }

    public LearningState SelectHsk(string userId, string levelId)
    {
        return states.AddOrUpdate(
            userId,
            _ => NewState(userId) with
            {
                CurrentTrack = "hsk",
                SelectedHskLevelId = levelId,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            (_, current) => current with
            {
                CurrentTrack = "hsk",
                SelectedHskLevelId = levelId,
                UpdatedAt = DateTimeOffset.UtcNow
            });
    }

    public LearningState? StartLesson(string userId, string lessonId)
    {
        if (!states.TryGetValue(userId, out var current))
        {
            current = GetOrCreate(userId);
        }

        var started = current.StartedLessonIds.ToHashSet(StringComparer.Ordinal);
        started.Add(lessonId);
        var updated = current with
        {
            CurrentTrack = "hsk",
            CurrentLessonId = lessonId,
            StartedLessonIds = started,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        states[userId] = updated;
        return updated;
    }

    public LearningState? CompleteLesson(string userId, string lessonId)
    {
        if (!states.TryGetValue(userId, out var current))
        {
            return null;
        }

        if (!current.StartedLessonIds.Contains(lessonId) && !current.CompletedLessonIds.Contains(lessonId))
        {
            return null;
        }

        var completed = current.CompletedLessonIds.ToHashSet(StringComparer.Ordinal);
        completed.Add(lessonId);
        var updated = current with
        {
            CurrentLessonId = current.CurrentLessonId == lessonId ? null : current.CurrentLessonId,
            CompletedLessonIds = completed,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        states[userId] = updated;
        return updated;
    }

    private static LearningState NewState(string userId) => new(
        userId,
        CurrentTrack: null,
        SelectedHskLevelId: null,
        CurrentLessonId: null,
        CurrentBeginnerStageId: null,
        StartedLessonIds: new HashSet<string>(StringComparer.Ordinal),
        CompletedLessonIds: new HashSet<string>(StringComparer.Ordinal),
        UpdatedAt: DateTimeOffset.UtcNow);
}
