using VietAisHsk.Api.Modules.Curriculum;
using VietAisHsk.Api.Modules.Learning;

namespace VietAisHsk.Api.Tests;

public sealed class LearningCompletionViewTests
{
    private static HskLearningView View(string[] completed, params UnitNode[] units) => new(
        new(new("hsk1", "v1", 1, "HSK 1", ContentStatus.Published, DateTimeOffset.UtcNow),
            [new("topic", "Topic", units)]),
        LearningStateProjection.Empty("learner") with { CompletedLessonIds = completed.ToHashSet() });

    [Fact]
    public void Empty_level_and_empty_units_are_not_complete()
    {
        Assert.False(View([]).IsLevelCompleted);
        Assert.False(View([], new UnitNode("empty", "Empty", [])).IsLevelCompleted);
    }

    [Fact]
    public void All_published_lessons_are_required_for_each_unit()
    {
        var unit = new UnitNode("unit", "Unit", [new("a", "A", ContentStatus.Published), new("b", "B", ContentStatus.Published)]);
        Assert.Empty(View(["a", "unrelated"], unit).CompletedUnitIds);
        var complete = View(["a", "b"], unit);
        Assert.Equal(["unit"], complete.CompletedUnitIds);
        Assert.True(complete.IsLevelCompleted);
        Assert.False(View(["a", "b"], unit, new("empty", "Empty", [])).IsLevelCompleted);
    }

    [Fact]
    public void Draft_lessons_do_not_count_as_available_learning_content()
    {
        var unit = new UnitNode("unit", "Unit", [new("a", "A", ContentStatus.Published), new("draft", "Draft", ContentStatus.Draft)]);
        Assert.True(View(["a"], unit).IsLevelCompleted);
        Assert.False(View(["draft"], new UnitNode("draft-unit", "Draft", [new("draft", "Draft", ContentStatus.Draft)])).IsLevelCompleted);
    }
}
