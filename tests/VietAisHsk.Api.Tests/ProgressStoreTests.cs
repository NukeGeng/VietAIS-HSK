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

        store.ApplyPracticeSignal(new PracticeEvaluationSignal("u1", "Hanzi", "学", "Incorrect", now, "attempt-1"));
        store.ApplyPracticeSignal(new PracticeEvaluationSignal("u1", "Hanzi", "学", "Correct", now.AddMinutes(1), "attempt-2"));

        var snapshot = store.GetSnapshot("u1");
        var weakPoints = store.GetWeakPoints("u1");

        Assert.Equal(2, snapshot.PracticeAnswered);
        Assert.Equal(1, snapshot.PracticeCorrect);
        Assert.Equal(1, snapshot.PracticeIncorrect);
        Assert.Single(weakPoints);
        Assert.Equal(1, weakPoints[0].EvidenceCount);
    }

    [Fact]
    public void Duplicate_practice_signal_is_idempotent_by_event_id()
    {
        var store = new InMemoryProgressStore();
        var signal = new PracticeEvaluationSignal("u1", "vocabulary", "你好", "Incorrect", DateTimeOffset.UtcNow, "session-1:hello");

        store.ApplyPracticeSignal(signal);
        store.ApplyPracticeSignal(signal);

        var snapshot = store.GetSnapshot("u1");
        Assert.Equal(1, snapshot.PracticeAnswered);
        Assert.Equal(1, snapshot.PracticeIncorrect);
        Assert.Equal(1, Assert.Single(store.GetWeakPoints("u1")).EvidenceCount);
    }

    [Fact]
    public void Updated_answer_replaces_the_previous_projection_for_same_session_question()
    {
        var store = new InMemoryProgressStore();
        var eventId = "session-1:hello";

        store.ApplyPracticeSignal(new PracticeEvaluationSignal("u1", "vocabulary", "你好", "Incorrect", DateTimeOffset.UtcNow, eventId));
        store.ApplyPracticeSignal(new PracticeEvaluationSignal("u1", "vocabulary", "你好", "Correct", DateTimeOffset.UtcNow.AddMinutes(1), eventId));

        var snapshot = store.GetSnapshot("u1");
        Assert.Equal(1, snapshot.PracticeAnswered);
        Assert.Equal(1, snapshot.PracticeCorrect);
        Assert.Equal(0, snapshot.PracticeIncorrect);
        Assert.Empty(store.GetWeakPoints("u1"));
    }

    [Fact]
    public void Correct_review_signal_reduces_the_weak_point_and_is_idempotent()
    {
        var store = new InMemoryProgressStore();
        var now = DateTimeOffset.UtcNow;

        store.ApplyPracticeSignal(new PracticeEvaluationSignal("u1", "vocabulary", "你好", "Incorrect", now, "practice-1"));
        var review = new ReviewEvaluationSignal("u1", "vocabulary", "你好", "Correct", now.AddDays(1), "review-1");

        store.ApplyReviewSignal(review);
        store.ApplyReviewSignal(review);

        var snapshot = store.GetSnapshot("u1");
        Assert.Equal(1, snapshot.ReviewAnswered);
        Assert.Equal(1, snapshot.ReviewCorrect);
        Assert.Equal(0, snapshot.ReviewIncorrect);
        Assert.Empty(store.GetWeakPoints("u1"));
    }

    [Fact]
    public void Practice_and_review_results_build_deterministic_knowledge_mastery()
    {
        var store = new InMemoryProgressStore();
        var now = DateTimeOffset.UtcNow;

        store.ApplyPracticeSignal(new PracticeEvaluationSignal("u1", "vocabulary", "你好", "Correct", now, "practice-1"));
        store.ApplyPracticeSignal(new PracticeEvaluationSignal("u1", "vocabulary", "你好", "Correct", now.AddMinutes(1), "practice-2"));
        store.ApplyPracticeSignal(new PracticeEvaluationSignal("u1", "vocabulary", "你好", "Incorrect", now.AddMinutes(2), "practice-3"));
        store.ApplyReviewSignal(new ReviewEvaluationSignal("u1", "vocabulary", "你好", "Correct", now.AddMinutes(3), "review-1"));

        var mastery = Assert.Single(store.GetMastery("u1"));

        Assert.Equal("vocabulary", mastery.KnowledgeType);
        Assert.Equal("你好", mastery.KnowledgeId);
        Assert.Equal(4, mastery.AttemptCount);
        Assert.Equal(3, mastery.CorrectCount);
        Assert.Equal(1, mastery.IncorrectCount);
        Assert.Equal(75, mastery.ScorePercent);
        Assert.Equal("Learning", mastery.State);
    }

    [Fact]
    public void Incorrect_review_signal_increases_weak_point_evidence()
    {
        var store = new InMemoryProgressStore();
        var now = DateTimeOffset.UtcNow;

        store.ApplyPracticeSignal(new PracticeEvaluationSignal("u1", "hanzi-writing", "学", "Incorrect", now, "practice-1"));
        store.ApplyReviewSignal(new ReviewEvaluationSignal("u1", "hanzi-writing", "学", "Incorrect", now.AddDays(1), "review-1"));

        var weakPoint = Assert.Single(store.GetWeakPoints("u1"));
        Assert.Equal("WritingWeak", weakPoint.Reason);
        Assert.Equal(2, weakPoint.EvidenceCount);
        Assert.Equal(1, store.GetSnapshot("u1").ReviewIncorrect);
    }

    [Fact]
    public void Exam_result_signal_updates_metrics_and_exam_weak_point_once()
    {
        var store = new InMemoryProgressStore();
        var signal = new ExamResultSignal(
            "u1",
            "exam-hsk3",
            "attempt-1",
            1,
            2,
            50,
            ["question-2"],
            DateTimeOffset.UtcNow,
            "exam:attempt-1");

        store.ApplyExamSignal(signal);
        store.ApplyExamSignal(signal);

        var snapshot = store.GetSnapshot("u1");
        Assert.Equal(1, snapshot.ExamAttempts);
        Assert.Equal(2, snapshot.ExamQuestions);
        Assert.Equal(1, snapshot.ExamCorrect);
        Assert.Equal(1, snapshot.ExamIncorrect);
        var weakPoint = Assert.Single(store.GetWeakPoints("u1"));
        Assert.Equal("ExamIncorrect", weakPoint.Reason);
        Assert.Equal("exam-hsk3:question-2", weakPoint.KnowledgeId);
    }

    [Fact]
    public void Translation_and_speaking_signals_update_progress_once()
    {
        var store = new InMemoryProgressStore();
        var submittedAt = DateTimeOffset.UtcNow;
        var translation = new TranslationAttemptSignal(
            "u1",
            "translation-attempt-1",
            "exercise-1",
            submittedAt,
            "translation:translation-attempt-1");
        var speaking = new SpeakingSessionCompletedSignal(
            "u1",
            "speaking-session-1",
            "HSK 3",
            3,
            submittedAt.AddMinutes(-5),
            submittedAt.AddMinutes(1),
            "speaking:speaking-session-1");

        store.ApplyTranslationSignal(translation);
        store.ApplyTranslationSignal(translation);
        store.ApplySpeakingSignal(speaking);
        store.ApplySpeakingSignal(speaking);
        store.RecordActivity("u1", "translation-attempt", translation.AttemptId, translation.SubmittedAt);
        store.RecordActivity("u1", "speaking-session-completed", speaking.SessionId, speaking.CompletedAt);

        var snapshot = store.GetSnapshot("u1");
        var history = store.GetHistory("u1");

        Assert.Equal(1, snapshot.TranslationAttempts);
        Assert.Equal(1, snapshot.SpeakingSessions);
        Assert.Equal(3, snapshot.SpeakingTurns);
        Assert.Equal(2, snapshot.CompletedActivities);
        Assert.Equal(2, history.Count);
    }

    [Fact]
    public void Activity_is_idempotent_by_type_and_reference()
    {
        var store = new InMemoryProgressStore();
        var now = DateTimeOffset.UtcNow;

        store.RecordActivity("u1", "LessonCompleted", "lesson-1", now);
        store.RecordActivity("u1", "LessonCompleted", "lesson-1", now.AddMinutes(1));

        Assert.Single(store.GetHistory("u1"));
        Assert.Equal(1, store.GetSnapshot("u1").CompletedActivities);
    }

    [Fact]
    public void Projection_rebuild_is_deterministic_from_persisted_inputs()
    {
        var now = DateTimeOffset.UtcNow;
        var signals = new[]
        {
            new PracticeEvaluationSignal("u1", "tone", "ma", "Incorrect", now, "attempt-1"),
            new PracticeEvaluationSignal("u1", "tone", "ma", "Correct", now.AddMinutes(1), "attempt-2")
        };
        var activities = new[]
        {
            new ProgressActivityRecord("lesson-1", new LearningHistoryEntry("lesson-completed", "lesson-1", now))
        };

        var first = ProgressProjectionBuilder.Rebuild("u1", signals, activities, emptyUpdatedAt: now);
        var second = ProgressProjectionBuilder.Rebuild("u1", signals, activities, emptyUpdatedAt: now);

        Assert.Equal(first.Snapshot, second.Snapshot);
        Assert.Equal(first.WeakPoints, second.WeakPoints);
        Assert.Equal(first.Activities, second.Activities);
    }

    [Fact]
    public void Streak_uses_learner_timezone_when_utc_dates_cross_midnight()
    {
        var now = new DateTimeOffset(2026, 9, 22, 0, 30, 0, TimeSpan.Zero);
        var history = new[]
        {
            new LearningHistoryEntry("practice-completed", "day-1", new DateTimeOffset(2026, 9, 20, 17, 30, 0, TimeSpan.Zero)),
            new LearningHistoryEntry("practice-completed", "day-2", new DateTimeOffset(2026, 9, 21, 17, 30, 0, TimeSpan.Zero))
        };

        var streak = ProgressProjectionBuilder.CalculateStreak(history, "Asia/Ho_Chi_Minh", now);

        Assert.Equal(2, streak.CurrentDays);
        Assert.Equal(
            new[] { new DateOnly(2026, 9, 22), new DateOnly(2026, 9, 21) },
            streak.QualifyingDays);
    }
}
