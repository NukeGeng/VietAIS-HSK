using VietAisHsk.Api.Modules.Curriculum;
using VietAisHsk.Api.Modules.Practice;

namespace VietAisHsk.Api.Tests;

public sealed class HanziWritingTests
{
    [Fact]
    public void Validator_accepts_matching_start_end_and_direction()
    {
        var reference = new HanziStroke("stroke-1", 1, "Ngang", "M 16 50 Q 50 48 84 50");

        var result = HanziWritingValidator.Validate(
            reference,
            [new HanziPoint(17, 51), new HanziPoint(50, 49), new HanziPoint(83, 50)],
            HanziWritingMode.Recall);

        Assert.Equal(HanziWritingStrokeResult.Correct, result.Result);
    }

    [Fact]
    public void Validator_rejects_wrong_direction_and_start()
    {
        var reference = new HanziStroke("stroke-1", 1, "Ngang", "M 16 50 Q 50 48 84 50");

        var reversed = HanziWritingValidator.Validate(
            reference,
            [new HanziPoint(84, 50), new HanziPoint(16, 50)],
            HanziWritingMode.Recall);
        var wrongStart = HanziWritingValidator.Validate(
            reference,
            [new HanziPoint(90, 10), new HanziPoint(83, 50)],
            HanziWritingMode.Recall);

        Assert.Equal(HanziWritingStrokeResult.NeedsRetry, reversed.Result);
        Assert.Equal(HanziWritingStrokeResult.NeedsRetry, wrongStart.Result);
    }

    [Fact]
    public void Writing_attempt_allows_retry_and_completes_idempotently()
    {
        var catalog = new BootstrapHanziCatalog();
        var store = new InMemoryHanziWritingStore(catalog);
        var started = store.Start("learner-1", "hanzi-da", HanziWritingMode.Recall);

        Assert.NotNull(started);
        var wrong = store.SubmitStroke("learner-1", started.Id, [new HanziPoint(90, 10), new HanziPoint(83, 50)]);
        Assert.NotNull(wrong);
        Assert.Equal(HanziWritingStrokeResult.NeedsRetry, wrong.Result.Result);
        Assert.Empty(wrong.Attempt.AcceptedStrokes);

        var first = store.SubmitStroke("learner-1", started.Id, [new HanziPoint(17, 45), new HanziPoint(83, 45)]);
        var second = store.SubmitStroke("learner-1", started.Id, [new HanziPoint(52, 17), new HanziPoint(19, 82)]);
        var third = store.SubmitStroke("learner-1", started.Id, [new HanziPoint(50, 45), new HanziPoint(84, 82)]);

        Assert.Equal(HanziWritingStrokeResult.Correct, first?.Result.Result);
        Assert.Equal(HanziWritingStrokeResult.Correct, second?.Result.Result);
        Assert.Equal(HanziWritingStrokeResult.Correct, third?.Result.Result);
        var completed = store.Complete("learner-1", started.Id);
        var completedAgain = store.Complete("learner-1", started.Id);

        Assert.Equal(PracticeResult.Correct, completed?.OverallResult);
        Assert.Equal(completed, completedAgain);
        Assert.Null(store.Get("learner-2", started.Id));
    }
}
