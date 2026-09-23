namespace VietAisHsk.Api.Modules.Learning;

public sealed record LearningState(
    string UserId,
    string? CurrentTrack,
    string? SelectedHskLevelId,
    string? CurrentLessonId,
    string? CurrentBeginnerStageId,
    IReadOnlySet<string> StartedBeginnerStageIds,
    IReadOnlySet<string> CompletedBeginnerStageIds,
    IReadOnlySet<string> StartedLessonIds,
    IReadOnlySet<string> CompletedLessonIds,
    DateTimeOffset UpdatedAt);

public sealed record LearningHome(
    LearningState State,
    string? ContinueTarget);

public sealed record BeginnerLearningView(
    Curriculum.BeginnerTrack Track,
    LearningState State);

public sealed record HskLearningView(
    Curriculum.HskLevelTree Curriculum,
    LearningState State)
{
    public IReadOnlyList<string> CompletedUnitIds => Curriculum.Topics
        .SelectMany(topic => topic.Units)
        .Where(unit =>
        {
            var lessons = unit.Lessons.Where(lesson => lesson.Status == VietAisHsk.Api.Modules.Curriculum.ContentStatus.Published).ToArray();
            return lessons.Length > 0 && lessons.All(lesson => State.CompletedLessonIds.Contains(lesson.Id));
        })
        .Select(unit => unit.Id).ToArray();

    public bool IsLevelCompleted
    {
        get
        {
            var units = Curriculum.Topics.SelectMany(topic => topic.Units).ToArray();
            return units.Length > 0 && CompletedUnitIds.Count == units.Length;
        }
    }
}
