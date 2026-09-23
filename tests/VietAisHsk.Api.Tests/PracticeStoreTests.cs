using VietAisHsk.Api.Modules.Practice;

namespace VietAisHsk.Api.Tests;

public sealed class PracticeStoreTests
{
    [Fact]
    public void Session_is_owned_by_creator_and_completion_is_idempotent()
    {
        var store = new InMemoryPracticeStore();
        var question = new PracticeQuestion("q1", "vocabulary", "你好", ["xin chào"]);
        var created = store.Create("learner-1", [question]);

        Assert.NotNull(created);
        Assert.Null(store.Get("learner-2", created.Id));
        var answer = store.SubmitAnswer("learner-1", created.Id, question.Id, "xin chào");
        Assert.Equal(PracticeResult.Correct, answer?.Result);

        var completed = store.Complete("learner-1", created.Id);
        var completedAgain = store.Complete("learner-1", created.Id);

        Assert.NotNull(completed);
        Assert.Equal(PracticeSessionStatus.Completed, completed.Status);
        Assert.Equal(completed, completedAgain);
        Assert.Equal(1, store.GetResult(completed).Correct);
    }
}
