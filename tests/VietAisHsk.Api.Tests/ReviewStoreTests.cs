using VietAisHsk.Api.Modules.Review;
using VietAisHsk.Api.Modules.Shared;

namespace VietAisHsk.Api.Tests;

public sealed class ReviewStoreTests
{
    private sealed class TestReviewClock(DateTimeOffset initial) : IReviewClock
    {
        public DateTimeOffset UtcNow { get; set; } = initial;
    }

    [Fact]
    public void Duplicate_wrong_signals_upsert_one_item_and_increment_mistake_count()
    {
        var store = new InMemoryReviewStore();
        var evaluatedAt = DateTimeOffset.UtcNow;
        var signal = new PracticeEvaluationSignal(
            "learner-1",
            "vocabulary",
            "你好",
            "Incorrect",
            evaluatedAt);

        store.ApplyPracticeSignal(signal);
        store.ApplyPracticeSignal(signal with { EvaluatedAt = evaluatedAt.AddMinutes(1) });

        var item = Assert.Single(store.GetItems("learner-1"));
        Assert.Equal(2, item.MistakeCount);
        Assert.Equal(70, item.Priority);
    }

    [Fact]
    public void Review_result_is_idempotent_per_item_and_session()
    {
        var store = new InMemoryReviewStore();
        store.ApplyPracticeSignal(new PracticeEvaluationSignal(
            "learner-1",
            "vocabulary",
            "你好",
            "Incorrect",
            DateTimeOffset.UtcNow));

        var item = Assert.Single(store.GetItems("learner-1"));
        var reviewSession = store.StartSession("learner-1", [item.Id]);
        Assert.NotNull(reviewSession);
        Assert.Null(store.GetSession("learner-2", reviewSession.Id));
        Assert.Null(store.RecordResult("learner-2", reviewSession.Id, item.Id, correct: true));

        var firstResult = store.RecordResult("learner-1", reviewSession.Id, item.Id, correct: true);
        var retriedResult = store.RecordResult("learner-1", reviewSession.Id, item.Id, correct: true);
        var conflictingRetry = store.RecordResult("learner-1", reviewSession.Id, item.Id, correct: false);

        Assert.NotNull(firstResult);
        Assert.Equal(firstResult, retriedResult);
        Assert.True(firstResult.NextReviewAt > DateTimeOffset.UtcNow);
        Assert.Null(conflictingRetry);
        Assert.Empty(store.GetItems("learner-1"));
        Assert.Empty(store.GetItems("learner-2"));
    }

    [Fact]
    public void Repeated_hanzi_writing_errors_create_writing_weak_item()
    {
        var store = new InMemoryReviewStore();
        var first = DateTimeOffset.UtcNow;

        store.ApplyPracticeSignal(new PracticeEvaluationSignal("learner-1", "hanzi-writing", "hanzi-xue", "Incorrect", first, "writing-1"));
        store.ApplyPracticeSignal(new PracticeEvaluationSignal("learner-1", "hanzi-writing", "hanzi-xue", "Incorrect", first.AddMinutes(1), "writing-2"));

        var item = Assert.Single(store.GetItems("learner-1"));
        Assert.Equal(ReviewReason.WritingWeak, item.Reason);
        Assert.Equal("hanzi-writing", item.KnowledgeType);
        Assert.Equal("hanzi-xue", item.KnowledgeId);
        Assert.Equal(2, item.MistakeCount);
    }

    [Fact]
    public void Review_scheduler_uses_injected_clock_for_due_boundary()
    {
        var now = new DateTimeOffset(2026, 9, 22, 8, 0, 0, TimeSpan.Zero);
        var clock = new TestReviewClock(now);
        var store = new InMemoryReviewStore(clock);
        store.ApplyPracticeSignal(new PracticeEvaluationSignal("learner-1", "vocabulary", "你好", "Incorrect", now, "practice-1"));
        store.ApplyPracticeSignal(new PracticeEvaluationSignal("learner-1", "vocabulary", "你好", "Incorrect", now.AddMinutes(1), "practice-2"));

        var item = Assert.Single(store.GetItems("learner-1"));
        var session = store.StartSession("learner-1", [item.Id]);
        Assert.NotNull(session);
        var resolved = store.RecordResult("learner-1", session.Id, item.Id, correct: true);

        Assert.NotNull(resolved);
        Assert.Equal(now.AddDays(1), resolved.NextReviewAt);
        Assert.Empty(store.GetItems("learner-1", dueOnly: true));

        clock.UtcNow = now.AddDays(1);
        Assert.Single(store.GetItems("learner-1", dueOnly: true));
    }

    [Fact]
    public void Exam_signal_maps_incorrect_knowledge_to_review_once()
    {
        var store = new InMemoryReviewStore();
        var signal = new ExamResultSignal(
            "learner-1",
            "bootstrap-hsk3-mini",
            "attempt-1",
            1,
            2,
            50,
            ["bootstrap-exam-q2"],
            DateTimeOffset.UtcNow,
            "exam:attempt-1",
            [new ExamKnowledgeResult("bootstrap-exam-q2", "vocabulary", "vocab-thanks", "Incorrect")]);

        store.ApplyExamSignal(signal);
        store.ApplyExamSignal(signal);

        var item = Assert.Single(store.GetItems("learner-1"));
        Assert.Equal("vocabulary", item.KnowledgeType);
        Assert.Equal("vocab-thanks", item.KnowledgeId);
        Assert.Equal(ReviewReason.WrongAnswer, item.Reason);
        Assert.Equal(1, item.MistakeCount);
        Assert.Equal("exam:attempt-1:bootstrap-exam-q2", item.LastSourceEventId);
    }
}
