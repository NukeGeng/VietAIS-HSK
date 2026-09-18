using VietAisHsk.Api.Modules.Progress;
using VietAisHsk.Api.Modules.Shared;

namespace VietAisHsk.Api.Tests;

public sealed class ProgressStoreTests
{
    [Fact]
    public void Practice_signal_creates_a_weak_point_only_for_incorrect_answer()
    {
        var store = new InMemoryProgressStore();
        var now = DateTimeOffset.UtcNow;

        store.ApplyPracticeSignal(new PracticeEvaluationSignal("u1", "Hanzi", "学", "Incorrect", now));
        store.ApplyPracticeSignal(new PracticeEvaluationSignal("u1", "Hanzi", "学", "Correct", now.AddMinutes(1)));

        var snapshot = store.GetSnapshot("u1");
        var weakPoints = store.GetWeakPoints("u1");

        Assert.Equal(2, snapshot.PracticeAnswered);
        Assert.Equal(1, snapshot.PracticeCorrect);
        Assert.Equal(1, snapshot.PracticeIncorrect);
        Assert.Single(weakPoints);
        Assert.Equal(1, weakPoints[0].EvidenceCount);
    }

    [Fact]
    public void Activity_is_idempotent_by_type_and_reference()
    {
        var store = new InMemoryProgressStore();
        var now = DateTimeOffset.UtcNow;

        store.RecordActivity("u1", "LessonCompleted", "lesson-1", now);
        store.RecordActivity("u1", "LessonCompleted", "lesson-1", now.AddMinutes(1));

        Assert.Single(store.GetHistory("u1"));
    }
}
