namespace VietAisHsk.Api.Modules.Learning;

public sealed record LearningState(
    string UserId,
    string? CurrentTrack,
    string? SelectedHskLevelId,
    string? CurrentLessonId,
    string? CurrentBeginnerStageId,
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
    LearningState State);
