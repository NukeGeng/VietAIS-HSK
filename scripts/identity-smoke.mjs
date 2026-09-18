const baseUrl = process.env.IDENTITY_API_URL ?? "http://127.0.0.1:5055";

async function request(path, options = {}) {
  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(options.headers ?? {})
    }
  });

  const body = await response.text();
  let parsed = body;
  try {
    parsed = body ? JSON.parse(body) : null;
  } catch {
    // Keep plain text for diagnostics.
  }

  return { response, body: parsed };
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

const anonymous = await request("/api/me");
assert(anonymous.response.status === 401, `anonymous /api/me expected 401, got ${anonymous.response.status}`);

const userHeaders = { "X-Dev-User-Id": "smoke-learner" };
const update = await request("/api/me/profile", {
  method: "PUT",
  headers: userHeaders,
  body: JSON.stringify({
    displayName: "Học viên smoke",
    avatarUrl: null,
    timezone: "Asia/Ho_Chi_Minh",
    studyPreferences: { dailyMinutes: 30, preferredStudyTime: "20:00" }
  })
});
assert(update.response.status === 200, `profile update expected 200, got ${update.response.status}`);

const reloaded = await request("/api/me", { headers: userHeaders });
assert(reloaded.response.status === 200, `reload expected 200, got ${reloaded.response.status}`);
assert(reloaded.body?.profile?.displayName === "Học viên smoke", "profile display name did not persist");
assert(reloaded.body?.profile?.studyPreferences?.dailyMinutes === 30, "study preferences did not persist");

const learnerAdminAttempt = await request("/api/admin/users", { headers: userHeaders });
assert(learnerAdminAttempt.response.status === 403, `learner admin access expected 403, got ${learnerAdminAttempt.response.status}`);

const admin = await request("/api/admin/users", {
  headers: {
    "X-Dev-User-Id": "smoke-admin",
    "X-Dev-Permission": "users.manage"
  }
});
assert(admin.response.status === 200, `admin users expected 200, got ${admin.response.status}`);

const invalidTarget = await request("/api/me/learning-target", {
  method: "PUT",
  headers: userHeaders,
  body: JSON.stringify({ preferredHskLevelId: "HSK99", targetHskLevelId: "HSK3" })
});
assert(invalidTarget.response.status === 400, `invalid HSK target expected 400, got ${invalidTarget.response.status}`);

const beginner = await request("/api/curriculum/beginner");
assert(beginner.response.status === 200, `beginner curriculum expected 200, got ${beginner.response.status}`);
assert(beginner.body?.stages?.length === 7, "beginner curriculum stages are incomplete");

const curriculumForbidden = await request("/api/admin/curriculum/import", {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({
    syllabusVersionId: "cti-hsk-3.0-smoke",
    syllabusName: "HSK 3.0 smoke",
    sourceType: "test",
    levels: [{ id: "hsk3-smoke", levelNumber: 3, displayName: "HSK 3" }]
  })
});
assert(curriculumForbidden.response.status === 403, `curriculum import without permission expected 403, got ${curriculumForbidden.response.status}`);

const curriculumAdminHeaders = {
  "X-Dev-User-Id": "smoke-curriculum-admin",
  "X-Dev-Permission": "curriculum.manage"
};
const importRequest = {
  syllabusVersionId: "cti-hsk-3.0-smoke",
  syllabusName: "HSK 3.0 smoke",
  sourceType: "test",
  levels: [{ id: "hsk3-smoke", levelNumber: 3, displayName: "HSK 3" }]
};
const imported = await request("/api/admin/curriculum/import", {
  method: "POST",
  headers: curriculumAdminHeaders,
  body: JSON.stringify(importRequest)
});
assert(imported.response.status === 200, `curriculum import expected 200, got ${imported.response.status}`);

const hiddenDraft = await request("/api/curriculum/hsk-levels");
assert(hiddenDraft.response.status === 200 && hiddenDraft.body?.length === 0, "draft curriculum leaked into public list");

const published = await request("/api/admin/curriculum/hsk-levels/hsk3-smoke/publish", {
  method: "POST",
  headers: curriculumAdminHeaders
});
assert(published.response.status === 200, `curriculum publish expected 200, got ${published.response.status}`);

const publicLevels = await request("/api/curriculum/hsk-levels");
assert(publicLevels.response.status === 200, `public curriculum levels expected 200, got ${publicLevels.response.status}`);
assert(publicLevels.body?.some(level => level.id === "hsk3-smoke"), "published HSK level not visible");

const beginnerStart = await request("/api/learning/beginner/start", {
  method: "POST",
  headers: userHeaders
});
assert(beginnerStart.response.status === 200, `beginner start expected 200, got ${beginnerStart.response.status}`);
assert(beginnerStart.body?.currentBeginnerStageId === "pinyin", "beginner did not start at pinyin");

const learningHome = await request("/api/learning/home", { headers: userHeaders });
assert(learningHome.response.status === 200, `learning home expected 200, got ${learningHome.response.status}`);
assert(learningHome.body?.continueTarget === "beginner/pinyin", "continue target is incorrect");

const hskSelect = await request("/api/learning/hsk/hsk3-smoke/select", {
  method: "POST",
  headers: userHeaders
});
assert(hskSelect.response.status === 200, `HSK selection expected 200, got ${hskSelect.response.status}`);
assert(hskSelect.body?.selectedHskLevelId === "hsk3-smoke", "selected HSK level did not persist");

const unknownLesson = await request("/api/learning/lessons/not-published/start", {
  method: "POST",
  headers: userHeaders
});
assert(unknownLesson.response.status === 404, `unpublished lesson start expected 404, got ${unknownLesson.response.status}`);

const practiceUnauthenticated = await request("/api/practice/sessions", {
  method: "POST",
  body: JSON.stringify({ questionIds: ["bootstrap-vocab-hello"] })
});
assert(practiceUnauthenticated.response.status === 401, `practice unauthenticated expected 401, got ${practiceUnauthenticated.response.status}`);

const practiceSessionResponse = await request("/api/practice/sessions", {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({ questionIds: ["bootstrap-vocab-hello"] })
});
assert(practiceSessionResponse.response.status === 201, `practice session expected 201, got ${practiceSessionResponse.response.status}`);
assert(!practiceSessionResponse.body?.questions?.[0]?.acceptedAnswers, "practice session leaked accepted answers");
const practiceSessionId = practiceSessionResponse.body?.id;
assert(typeof practiceSessionId === "string", "practice session id is missing");

const wrongAnswer = await request(`/api/practice/sessions/${practiceSessionId}/answers`, {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({ questionId: "bootstrap-vocab-hello", answer: "tạm biệt" })
});
assert(wrongAnswer.response.status === 200 && wrongAnswer.body?.result === "Incorrect", "deterministic incorrect grading failed");

const otherUserSession = await request(`/api/practice/sessions/${practiceSessionId}`, {
  headers: { "X-Dev-User-Id": "different-user" }
});
assert(otherUserSession.response.status === 404, `cross-user practice access expected 404, got ${otherUserSession.response.status}`);

const completedPractice = await request(`/api/practice/sessions/${practiceSessionId}/complete`, {
  method: "POST",
  headers: userHeaders
});
assert(completedPractice.response.status === 200, `practice completion expected 200, got ${completedPractice.response.status}`);
assert(completedPractice.body?.incorrect === 1 && completedPractice.body?.answered === 1, "practice result summary is incorrect");

const completedAgain = await request(`/api/practice/sessions/${practiceSessionId}/complete`, {
  method: "POST",
  headers: userHeaders
});
assert(completedAgain.response.status === 200, "practice completion is not idempotent");

const progress = await request("/api/progress", { headers: userHeaders });
assert(progress.response.status === 200, `progress expected 200, got ${progress.response.status}`);
assert(progress.body?.practiceAnswered === 1 && progress.body?.practiceIncorrect === 1, "practice projection is incorrect");

const weakPoints = await request("/api/progress/weak-points", { headers: userHeaders });
assert(weakPoints.response.status === 200 && weakPoints.body?.length === 1, "practice weakness projection is missing");

const progressHistory = await request("/api/progress/history", { headers: userHeaders });
assert(progressHistory.response.status === 200 && progressHistory.body?.length === 1, "practice completion history is not idempotent");

const reviewSummary = await request("/api/review/summary", { headers: userHeaders });
assert(reviewSummary.response.status === 200, `review summary expected 200, got ${reviewSummary.response.status}`);
assert(reviewSummary.body?.mistakeCount === 1, "incorrect practice answer did not create review item");

const mistakes = await request("/api/review/mistakes", { headers: userHeaders });
assert(mistakes.response.status === 200 && mistakes.body?.length === 1, "mistake list did not contain practice item");
const reviewItemId = mistakes.body[0]?.id;
assert(typeof reviewItemId === "string", "review item id is missing");

const reviewSession = await request("/api/review/sessions", {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({ itemIds: [reviewItemId] })
});
assert(reviewSession.response.status === 201, `review session expected 201, got ${reviewSession.response.status}`);
const reviewSessionId = reviewSession.body?.id;

const reviewResult = await request(`/api/review/sessions/${reviewSessionId}/results`, {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({ itemId: reviewItemId, correct: true })
});
assert(reviewResult.response.status === 200, `review result expected 200, got ${reviewResult.response.status}`);

const reviewAfterResolve = await request("/api/review/mistakes", { headers: userHeaders });
assert(reviewAfterResolve.response.status === 200 && reviewAfterResolve.body?.length === 0, "resolved review item remained in mistakes");

const correctPracticeSessionResponse = await request("/api/practice/sessions", {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({ questionIds: ["bootstrap-vocab-hello"] })
});
assert(correctPracticeSessionResponse.response.status === 201, "second practice session was not created");
const correctPracticeSessionId = correctPracticeSessionResponse.body?.id;
const correctAnswer = await request(`/api/practice/sessions/${correctPracticeSessionId}/answers`, {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({ questionId: "bootstrap-vocab-hello", answer: "Xin chào" })
});
assert(correctAnswer.response.status === 200 && correctAnswer.body?.result === "Correct", "deterministic correct grading failed");

const exams = await request("/api/exams");
assert(exams.response.status === 200 && exams.body?.length === 1, "exam catalog did not load");
assert(!exams.body?.[0]?.questions?.[0]?.acceptedAnswers, "exam catalog leaked accepted answers");

const examStart = await request("/api/exams/bootstrap-hsk3-mini/attempts", {
  method: "POST",
  headers: userHeaders
});
assert(examStart.response.status === 201, `exam start expected 201, got ${examStart.response.status}`);
assert(!examStart.body?.questions?.[0]?.acceptedAnswers && !examStart.body?.events, "exam attempt leaked answer keys or event stream");
const attemptId = examStart.body?.id;
assert(typeof attemptId === "string", "exam attempt id is missing");

const otherUserAttempt = await request(`/api/exam-attempts/${attemptId}`, {
  headers: { "X-Dev-User-Id": "different-user" }
});
assert(otherUserAttempt.response.status === 404, `cross-user exam access expected 404, got ${otherUserAttempt.response.status}`);

const examAnswerOne = await request(`/api/exam-attempts/${attemptId}/answers`, {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({ questionId: "bootstrap-exam-q1", answer: "xin chào" })
});
assert(examAnswerOne.response.status === 200, "exam answer one was not saved");

const examAnswerTwo = await request(`/api/exam-attempts/${attemptId}/answers`, {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({ questionId: "bootstrap-exam-q2", answer: "tạm biệt" })
});
assert(examAnswerTwo.response.status === 200, "exam answer two was not saved");

const examSubmit = await request(`/api/exam-attempts/${attemptId}/submit`, {
  method: "POST",
  headers: userHeaders
});
assert(examSubmit.response.status === 200 && examSubmit.body?.status === "Scored", "exam objective scoring failed");

const examSubmitAgain = await request(`/api/exam-attempts/${attemptId}/submit`, {
  method: "POST",
  headers: userHeaders
});
assert(examSubmitAgain.response.status === 200 && examSubmitAgain.body?.objectiveScore === 50, "exam submit was not idempotent");

const examAnswerAfterSubmit = await request(`/api/exam-attempts/${attemptId}/answers`, {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({ questionId: "bootstrap-exam-q1", answer: "xin chào" })
});
assert(examAnswerAfterSubmit.response.status === 409, "submitted exam accepted a new answer");

const examResult = await request(`/api/exam-attempts/${attemptId}/result`, { headers: userHeaders });
assert(examResult.response.status === 200 && examResult.body?.objectiveScore === 50, "exam result was not persisted");

const translationExercises = await request("/api/translation/exercises");
assert(translationExercises.response.status === 200 && translationExercises.body?.length === 1, "translation exercise catalog did not load");
assert(!translationExercises.body?.[0]?.referenceChinese, "translation catalog leaked reference answer");

const translationAttemptResponse = await request("/api/translation/exercises/bootstrap-translation-1/attempts", {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({ answerChinese: "我喜欢学习中文。" })
});
assert(translationAttemptResponse.response.status === 201, "translation attempt was not saved");
assert(!translationAttemptResponse.body?.referenceChinese, "translation attempt leaked reference answer");
const translationAttemptId = translationAttemptResponse.body?.id;

const translationFeedback = await request(`/api/translation/attempts/${translationAttemptId}/feedback`, {
  method: "POST",
  headers: userHeaders
});
assert(translationFeedback.response.status === 503, "translation feedback was called without a configured provider");

const translationHistory = await request("/api/translation/history", { headers: userHeaders });
assert(translationHistory.response.status === 200 && translationHistory.body?.length === 1, "translation attempt was lost after feedback provider failure");

const speakingStart = await request("/api/speaking/sessions", {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({ hskContext: "HSK 3", mode: "guided-dialogue" })
});
assert(speakingStart.response.status === 201, "speaking session was not created");
const speakingSessionId = speakingStart.body?.id;

const speakingTurn = await request(`/api/speaking/sessions/${speakingSessionId}/turns`, {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({ transcript: "你好", audioReference: null })
});
assert(speakingTurn.response.status === 200 && speakingTurn.body?.turn?.providerStatus === "unavailable", "speaking provider fallback state is incorrect");

const speakingEnd = await request(`/api/speaking/sessions/${speakingSessionId}/end`, {
  method: "POST",
  headers: userHeaders
});
assert(speakingEnd.response.status === 200 && speakingEnd.body?.status === "Ended", "speaking session did not end");

const speakingHistory = await request("/api/speaking/history", { headers: userHeaders });
assert(speakingHistory.response.status === 200 && speakingHistory.body?.length === 1, "speaking history did not persist");

console.log("Identity + Curriculum + Learning + Practice + Review + Exam + Translation + Speaking smoke tests passed");
