using VietAisHsk.Api.Modules.Exam;

namespace VietAisHsk.Api.Tests;

public sealed class ExamStoreTests
{
    [Fact]
    public void Exam_attempt_is_isolated_and_submit_is_idempotent()
    {
        var store = new InMemoryExamStore();
        var exam = new BootstrapExamCatalog().GetPublishedExam("bootstrap-hsk3-mini");
        Assert.NotNull(exam);

        var attempt = store.Start("learner-1", exam);
        Assert.Null(store.Get("learner-2", attempt.Id));

        store.SubmitAnswer("learner-1", attempt.Id, new ExamAnswerRequest("bootstrap-exam-q1", "xin chào"));
        store.SubmitAnswer("learner-1", attempt.Id, new ExamAnswerRequest("bootstrap-exam-q2", "tạm biệt"));

        var submitted = store.Submit("learner-1", attempt.Id);
        var submittedAgain = store.Submit("learner-1", attempt.Id);

        Assert.NotNull(submitted);
        Assert.Equal(ExamAttemptStatus.Scored, submitted.Status);
        Assert.Equal(50, submitted.ObjectiveScore);
        Assert.NotNull(submittedAgain);
        Assert.Equal(submitted.Id, submittedAgain.Id);
        Assert.Equal(submitted.Status, submittedAgain.Status);
        Assert.Equal(submitted.ObjectiveScore, submittedAgain.ObjectiveScore);
        Assert.Equal(submitted.Events.Count, submittedAgain.Events.Count);
        Assert.Null(store.SubmitAnswer("learner-1", attempt.Id, new ExamAnswerRequest("bootstrap-exam-q1", "xin chào")));
        Assert.Equal(50, store.GetResult("learner-1", attempt.Id)?.ObjectiveScore);
    }

    [Fact]
    public void Subjective_exam_queues_once_and_applies_duplicate_result_idempotently()
    {
        var queue = new InMemoryExamSubjectiveGradingQueue();
        var store = new InMemoryExamStore(queue);
        var exam = new BootstrapExamCatalog().GetPublishedExam("bootstrap-hsk3-subjective");
        Assert.NotNull(exam);

        var attempt = store.Start("learner-1", exam);
        store.SubmitAnswer("learner-1", attempt.Id, new ExamAnswerRequest("bootstrap-exam-subjective-q1", "我叫小明。"));

        var submitted = store.Submit("learner-1", attempt.Id);
        Assert.NotNull(submitted);
        Assert.Equal(ExamAttemptStatus.Scored, submitted.Status);
        Assert.Equal("Pending", submitted.SubjectiveGradingStatus);
        Assert.Single(queue.Messages);

        var submittedAgain = store.Submit("learner-1", attempt.Id);
        Assert.NotNull(submittedAgain);
        Assert.Single(queue.Messages);

        var jobId = queue.Messages[0].JobId;
        var request = new ApplySubjectiveGradingResultRequest(jobId, attempt.Id, "Completed", 82, "Câu rõ ràng.", null);
        var applied = store.ApplySubjectiveGradingResult("learner-1", request);
        var duplicate = store.ApplySubjectiveGradingResult("learner-1", request);

        Assert.NotNull(applied);
        Assert.True(applied.Applied);
        Assert.False(applied.Conflict);
        Assert.Equal("Completed", applied.Attempt.SubjectiveGradingStatus);
        Assert.Equal(82, applied.Attempt.SubjectiveScore);
        Assert.NotNull(duplicate);
        Assert.False(duplicate.Applied);
        Assert.False(duplicate.Conflict);
        Assert.Equal(applied.Attempt.Events.Count, duplicate.Attempt.Events.Count);
    }

    [Fact]
    public void Subjective_failure_feedback_survives_attempt_rebuild()
    {
        var queue = new InMemoryExamSubjectiveGradingQueue();
        var store = new InMemoryExamStore(queue);
        var exam = new BootstrapExamCatalog().GetPublishedExam("bootstrap-hsk3-subjective");
        Assert.NotNull(exam);

        var attempt = store.Start("learner-1", exam);
        store.SubmitAnswer("learner-1", attempt.Id, new ExamAnswerRequest("bootstrap-exam-subjective-q1", "我叫小明。"));
        store.Submit("learner-1", attempt.Id);

        var failed = store.ApplySubjectiveGradingResult(
            "learner-1",
            new ApplySubjectiveGradingResultRequest(
                queue.Messages[0].JobId,
                attempt.Id,
                "Failed",
                null,
                "AI grading provider chưa được cấu hình.",
                "subjective-grading-v1"));

        Assert.NotNull(failed);
        Assert.False(failed.Conflict);
        Assert.Equal("Failed", failed.Attempt.SubjectiveGradingStatus);
        Assert.Equal("AI grading provider chưa được cấu hình.", failed.Attempt.SubjectiveFeedback);
        Assert.Equal("AI grading provider chưa được cấu hình.", store.Get("learner-1", attempt.Id)?.SubjectiveFeedback);
    }
}
