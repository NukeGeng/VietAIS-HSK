using VietAisHsk.Api.Modules.Learning;

namespace VietAisHsk.Api.Tests;

public sealed class LearningStateTests
{
    [Fact]
    public void Replay_restores_both_learning_paths_and_completed_lessons()
    {
        var at = DateTimeOffset.Parse("2026-09-22T10:00:00Z");
        var events = new object[]
        {
            new LearningTrackStarted("learner-1", "beginner", at),
            new HskLevelSelected("learner-1", "hsk-3", at.AddMinutes(1)),
            new LessonStarted("learner-1", "lesson-1", at.AddMinutes(2)),
            new LessonCompleted("learner-1", "lesson-1", at.AddMinutes(3))
        };

        var state = LearningStateProjection.Replay("learner-1", events);

        Assert.Equal("hsk", state.CurrentTrack);
        Assert.Equal("hsk-3", state.SelectedHskLevelId);
        Assert.Equal("pinyin", state.CurrentBeginnerStageId);
        Assert.Null(state.CurrentLessonId);
        Assert.Contains("lesson-1", state.StartedLessonIds);
        Assert.Contains("lesson-1", state.CompletedLessonIds);
        Assert.Equal(at.AddMinutes(3), state.UpdatedAt);
    }

    [Fact]
    public void Repeated_commands_do_not_add_duplicate_milestone_events()
    {
        var at = DateTimeOffset.UtcNow;
        var state = LearningStateProjection.Empty("learner-1", at);

        var beginnerStarted = LearningStateTransitions.StartBeginner(state, at);
        Assert.NotNull(beginnerStarted);
        state = LearningStateProjection.Apply(state, beginnerStarted);
        Assert.Null(LearningStateTransitions.StartBeginner(state, at.AddMinutes(1)));

        var hskSelected = LearningStateTransitions.SelectHsk(state, "hsk-3", at.AddMinutes(2));
        Assert.NotNull(hskSelected);
        state = LearningStateProjection.Apply(state, hskSelected);
        Assert.Null(LearningStateTransitions.SelectHsk(state, "hsk-3", at.AddMinutes(3)));

        var lessonStarted = LearningStateTransitions.StartLesson(state, "lesson-1", at.AddMinutes(4));
        Assert.NotNull(lessonStarted);
        state = LearningStateProjection.Apply(state, lessonStarted);
        Assert.Null(LearningStateTransitions.StartLesson(state, "lesson-1", at.AddMinutes(5)));

        var lessonCompleted = LearningStateTransitions.CompleteLesson(state, "lesson-1", at.AddMinutes(6));
        Assert.NotNull(lessonCompleted);
        state = LearningStateProjection.Apply(state, lessonCompleted);
        Assert.Null(LearningStateTransitions.CompleteLesson(state, "lesson-1", at.AddMinutes(7)));
    }

    [Fact]
    public void Lesson_cannot_be_completed_before_it_is_started()
    {
        var state = LearningStateProjection.Empty("learner-1");

        Assert.Null(LearningStateTransitions.CompleteLesson(state, "lesson-1", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Beginner_stage_completion_advances_to_the_next_published_stage()
    {
        var at = DateTimeOffset.Parse("2026-09-22T10:00:00Z");
        var state = LearningStateProjection.Empty("learner-1", at);

        state = LearningStateProjection.Apply(state, LearningStateTransitions.StartBeginner(state, at)!);
        Assert.Contains("pinyin", state.StartedBeginnerStageIds);

        var completed = LearningStateTransitions.CompleteBeginnerStage(state, "pinyin", "tones", at.AddMinutes(5));
        Assert.NotNull(completed);
        state = LearningStateProjection.Apply(state, completed!);

        Assert.Equal("tones", state.CurrentBeginnerStageId);
        Assert.Contains("pinyin", state.CompletedBeginnerStageIds);
        Assert.Contains("tones", state.StartedBeginnerStageIds);
        Assert.Null(LearningStateTransitions.CompleteBeginnerStage(state, "pinyin", "tones", at.AddMinutes(6)));
    }

    [Fact]
    public void Beginner_stage_cannot_skip_the_current_stage()
    {
        var at = DateTimeOffset.UtcNow;
        var state = LearningStateProjection.Apply(
            LearningStateProjection.Empty("learner-1", at),
            LearningStateTransitions.StartBeginner(LearningStateProjection.Empty("learner-1", at), at)!);

        Assert.Null(LearningStateTransitions.StartBeginnerStage(state, "tones", at.AddMinutes(1)));
        Assert.Null(LearningStateTransitions.CompleteBeginnerStage(state, "tones", null, at.AddMinutes(2)));
    }

    [Fact]
    public void Replay_rejects_an_event_from_another_learner()
    {
        var state = LearningStateProjection.Empty("learner-1");

        Assert.Throws<InvalidOperationException>(() =>
            LearningStateProjection.Apply(state, new LearningTrackStarted("learner-2", "beginner", DateTimeOffset.UtcNow)));
    }
}
