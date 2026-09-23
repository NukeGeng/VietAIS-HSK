import crypto from "node:crypto";

const baseUrl = process.env.IDENTITY_API_URL ?? "http://127.0.0.1:5055";
const runId = `${Date.now()}-${process.pid}`;
const smokeUserId = `smoke-learner-${runId}`;
const curriculumVersionId = `cti-hsk-3.0-smoke-${runId}`;
const curriculumLevelId = `hsk3-smoke-${runId}`;
const contentQuestionId = `smoke-content-question-${runId}`;
const audioContentId = `lesson-audio-smoke-${runId}`;
const audioIdempotencyKey = `${audioContentId}:audio:v1`;
const workerCallbackSecret = process.env.WORKER_CALLBACK_SHARED_SECRET ?? "";

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

function subjectiveCallbackOptions(payload) {
  const serialized = JSON.stringify(payload);
  if (!workerCallbackSecret) {
    return {
      headers: { ...userHeaders, "X-Dev-Permission": "exam.subjective-grading" },
      body: serialized
    };
  }

  const timestamp = Math.floor(Date.now() / 1000).toString();
  const signature = crypto
    .createHmac("sha256", workerCallbackSecret)
    .update(`${timestamp}\n${serialized}`)
    .digest("base64url");
  return {
    headers: {
      "X-VietAIS-Worker-Timestamp": timestamp,
      "X-VietAIS-Worker-Signature": `v1=${signature}`
    },
    body: serialized
  };
}

const anonymous = await request("/api/me");
assert(anonymous.response.status === 401, `anonymous /api/me expected 401, got ${anonymous.response.status}`);

const userHeaders = { "X-Dev-User-Id": smokeUserId };
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
assert(beginner.body?.stages?.filter(stage => stage.status === "Published").map(stage => stage.id).join(",") === "pinyin,tones", "beginner track published stages do not match available foundation pages");
assert(beginner.body?.stages?.find(stage => stage.id === "pinyin")?.masterDataRefs?.includes("foundation:pinyin"), "beginner Pinyin stage is missing master-data reference");
assert(beginner.body?.stages?.find(stage => stage.id === "tones")?.masterDataRefs?.includes("foundation:tones"), "beginner tone stage is missing master-data reference");

const pinyinFoundation = await request("/api/foundation/pinyin");
assert(pinyinFoundation.response.status === 200, `Pinyin foundation expected 200, got ${pinyinFoundation.response.status}`);
assert(pinyinFoundation.body?.sourceType === "PlatformAuthored", "Pinyin catalog provenance is missing");
assert(pinyinFoundation.body?.items?.filter(item => item.id.startsWith("initial-")).length === 21, "Pinyin initial inventory is incomplete");
assert(pinyinFoundation.body?.items?.filter(item => item.id.startsWith("final-")).length === 35, "Pinyin final inventory is incomplete");

const toneFoundation = await request("/api/foundation/tones");
assert(toneFoundation.response.status === 200, `tone foundation expected 200, got ${toneFoundation.response.status}`);
assert(toneFoundation.body?.items?.filter(item => item.category === "Thanh điệu").length === 5, "four tones and neutral tone are incomplete");
assert(toneFoundation.body?.items?.some(item => item.category === "Quy tắc"), "tone sandhi reference is missing");

const curriculumForbidden = await request("/api/admin/curriculum/import", {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({
    syllabusVersionId: curriculumVersionId,
    syllabusName: "HSK 3.0 smoke",
    sourceType: "test",
    levels: [{ id: curriculumLevelId, levelNumber: 3, displayName: "HSK 3" }]
  })
});
assert(curriculumForbidden.response.status === 403, `curriculum import without permission expected 403, got ${curriculumForbidden.response.status}`);

const curriculumAdminHeaders = {
  "X-Dev-User-Id": "smoke-curriculum-admin",
  "X-Dev-Permission": "curriculum.manage"
};
const importRequest = {
  syllabusVersionId: curriculumVersionId,
  syllabusName: "HSK 3.0 smoke",
  sourceType: "test",
  levels: [{ id: curriculumLevelId, levelNumber: 3, displayName: "HSK 3" }]
};
const imported = await request("/api/admin/curriculum/import", {
  method: "POST",
  headers: curriculumAdminHeaders,
  body: JSON.stringify(importRequest)
});
assert(imported.response.status === 200, `curriculum import expected 200, got ${imported.response.status}`);

const learnerCurriculumAdmin = await request("/api/admin/curriculum/hsk-levels", { headers: userHeaders });
assert(learnerCurriculumAdmin.response.status === 403, "learner accessed curriculum admin list");

const curriculumAdminList = await request("/api/admin/curriculum/hsk-levels", { headers: curriculumAdminHeaders });
assert(
  curriculumAdminList.response.status === 200
    && curriculumAdminList.body?.some(level => level.id === curriculumLevelId && level.status === "Draft"),
  "curriculum admin did not see draft level"
);

const hiddenDraft = await request("/api/curriculum/hsk-levels");
assert(
  hiddenDraft.response.status === 200
    && !hiddenDraft.body?.some(level => level.id === curriculumLevelId),
  "draft curriculum leaked into public list"
);

const published = await request(`/api/admin/curriculum/hsk-levels/${curriculumLevelId}/publish`, {
  method: "POST",
  headers: curriculumAdminHeaders
});
assert(published.response.status === 200, `curriculum publish expected 200, got ${published.response.status}`);

const learnerPublish = await request(`/api/admin/curriculum/hsk-levels/${curriculumLevelId}/publish`, {
  method: "POST",
  headers: userHeaders
});
assert(learnerPublish.response.status === 403, "learner published curriculum without permission");

const publicLevels = await request("/api/curriculum/hsk-levels");
assert(publicLevels.response.status === 200, `public curriculum levels expected 200, got ${publicLevels.response.status}`);
assert(publicLevels.body?.some(level => level.id === curriculumLevelId), "published HSK level not visible");

const beginnerStart = await request("/api/learning/beginner/start", {
  method: "POST",
  headers: userHeaders
});
assert(beginnerStart.response.status === 200, `beginner start expected 200, got ${beginnerStart.response.status}`);
assert(beginnerStart.body?.currentBeginnerStageId === "pinyin", "beginner did not start at pinyin");

const learningHome = await request("/api/learning/home", { headers: userHeaders });
assert(learningHome.response.status === 200, `learning home expected 200, got ${learningHome.response.status}`);
assert(learningHome.body?.continueTarget === "beginner/pinyin", "continue target is incorrect");

const beginnerComplete = await request("/api/learning/beginner/stages/pinyin/complete", {
  method: "POST",
  headers: userHeaders
});
assert(beginnerComplete.response.status === 200, `beginner stage complete expected 200, got ${beginnerComplete.response.status}`);
assert(beginnerComplete.body?.currentBeginnerStageId === "tones", "beginner path did not advance to tones");
assert(beginnerComplete.body?.completedBeginnerStageIds?.includes("pinyin"), "completed beginner stage was not recorded");

const beginnerResume = await request("/api/learning/home", { headers: userHeaders });
assert(beginnerResume.body?.continueTarget === "beginner/tones", "beginner continue target did not advance");

const stageLearnerHeaders = { "X-Dev-User-Id": `smoke-stage-learner-${runId}` };
const firstStageStart = await request("/api/learning/beginner/stages/pinyin/start", {
  method: "POST",
  headers: stageLearnerHeaders
});
assert(firstStageStart.response.status === 200 && firstStageStart.body?.currentBeginnerStageId === "pinyin", "first beginner stage did not start");
const skippedStageStart = await request("/api/learning/beginner/stages/tones/start", {
  method: "POST",
  headers: stageLearnerHeaders
});
assert(skippedStageStart.response.status === 409, "beginner stage start allowed skipping the current stage");

const hskSelect = await request(`/api/learning/hsk/${curriculumLevelId}/select`, {
  method: "POST",
  headers: userHeaders
});
assert(hskSelect.response.status === 200, `HSK selection expected 200, got ${hskSelect.response.status}`);
assert(hskSelect.body?.selectedHskLevelId === curriculumLevelId, "selected HSK level did not persist");

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

const mastery = await request("/api/progress/mastery", { headers: userHeaders });
assert(
  mastery.response.status === 200
  && mastery.body?.some(item => item.knowledgeType === "vocabulary" && item.knowledgeId === "bootstrap-vocab-hello" && item.scorePercent === 0 && item.state === "NeedsReview"),
  "knowledge mastery projection is incorrect"
);

const weakPoints = await request("/api/progress/weak-points", { headers: userHeaders });
assert(weakPoints.response.status === 200 && weakPoints.body?.length === 1, "practice weakness projection is missing");

const progressHistory = await request("/api/progress/history", { headers: userHeaders });
assert(
  progressHistory.response.status === 200
  && progressHistory.body?.filter(entry => entry.activityType === "practice-completed").length === 1
  && progressHistory.body?.filter(entry => entry.activityType === "beginner-stage-completed").length === 1,
  "practice or beginner completion history is not idempotent"
);

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
assert(exams.response.status === 200 && exams.body?.length >= 2, "exam catalog did not load");
const objectiveExam = exams.body?.find(exam => exam.id === "bootstrap-hsk3-mini");
assert(objectiveExam && !objectiveExam.questions?.[0]?.acceptedAnswers, "exam catalog leaked accepted answers");

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

const examReviewItems = await request("/api/review/mistakes", { headers: userHeaders });
assert(
  examReviewItems.response.status === 200
    && examReviewItems.body?.some(item => item.knowledgeType === "vocabulary" && item.knowledgeId === "vocab-thanks"),
  "exam incorrect knowledge was not mapped into Review"
);

const subjectiveStart = await request("/api/exams/bootstrap-hsk3-subjective/attempts", {
  method: "POST",
  headers: userHeaders
});
assert(subjectiveStart.response.status === 201, "subjective exam start was not created");
const subjectiveAttemptId = subjectiveStart.body?.id;
assert(typeof subjectiveAttemptId === "string", "subjective attempt id is missing");

const subjectiveAnswer = await request(`/api/exam-attempts/${subjectiveAttemptId}/answers`, {
  method: "POST",
  headers: userHeaders,
  body: JSON.stringify({ questionId: "bootstrap-exam-subjective-q1", answer: "我叫小明。" })
});
assert(subjectiveAnswer.response.status === 200, "subjective answer was not saved");

const subjectiveSubmit = await request(`/api/exam-attempts/${subjectiveAttemptId}/submit`, {
  method: "POST",
  headers: userHeaders
});
assert(
  subjectiveSubmit.response.status === 200
    && subjectiveSubmit.body?.status === "Scored"
    && subjectiveSubmit.body?.subjectiveGradingStatus === "Pending",
  "subjective exam did not enter pending grading"
);

const subjectiveJobId = `exam-subjective:${subjectiveAttemptId}:v1`;
const subjectiveGradingPayload = {
  jobId: subjectiveJobId,
  status: "Completed",
  score: 82,
  feedback: "Câu giới thiệu rõ ràng.",
  userId: smokeUserId
};
const subjectiveGrading = await request(`/api/exam-attempts/${subjectiveAttemptId}/subjective-grading`, {
  method: "POST",
  ...subjectiveCallbackOptions(subjectiveGradingPayload)
});
assert(
  subjectiveGrading.response.status === 200
    && subjectiveGrading.body?.subjectiveGradingStatus === "Completed"
    && subjectiveGrading.body?.subjectiveScore === 82,
  "subjective grading result was not applied"
);

const subjectiveGradingAgain = await request(`/api/exam-attempts/${subjectiveAttemptId}/subjective-grading`, {
  method: "POST",
  ...subjectiveCallbackOptions(subjectiveGradingPayload)
});
assert(
  subjectiveGradingAgain.response.status === 200
    && subjectiveGradingAgain.body?.subjectiveGradingStatus === "Completed"
    && subjectiveGradingAgain.body?.subjectiveScore === 82,
  "duplicate subjective grading result was not idempotent"
);

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
assert(translationAttemptId, "translation attempt id missing");

const otherUserFeedback = await request(`/api/translation/attempts/${translationAttemptId}/feedback`, {
  method: "POST",
  headers: { "X-Dev-User-Id": "smoke-other-user" }
});
assert(otherUserFeedback.response.status === 404, "translation feedback exposed another user's attempt");

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
const otherSpeakingRead = await request(`/api/speaking/sessions/${speakingSessionId}`, {
  headers: { "X-Dev-User-Id": "smoke-other-user" }
});
assert(otherSpeakingRead.response.status === 404, "speaking session was readable by another user");

const otherSpeakingTurn = await request(`/api/speaking/sessions/${speakingSessionId}/turns`, {
  method: "POST",
  headers: { "X-Dev-User-Id": "smoke-other-user" },
  body: JSON.stringify({ transcript: "你好", audioReference: null })
});
assert(otherSpeakingTurn.response.status === 404, "another user could write to a speaking session");

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

const speakingEndRetry = await request(`/api/speaking/sessions/${speakingSessionId}/end`, {
  method: "POST",
  headers: userHeaders
});
assert(speakingEndRetry.response.status === 200 && speakingEndRetry.body?.status === "Ended", "speaking end was not idempotent");

const speakingHistory = await request("/api/speaking/history", { headers: userHeaders });
assert(speakingHistory.response.status === 200 && speakingHistory.body?.length === 1, "speaking history did not persist");

const conversationProgress = await request("/api/progress", { headers: userHeaders });
assert(
  conversationProgress.response.status === 200
    && conversationProgress.body?.translationAttempts === 1
    && conversationProgress.body?.speakingSessions === 1
    && conversationProgress.body?.speakingTurns === 1,
  "translation/speaking progress signals were not projected once"
);

const publishedQuestions = await request("/api/content/questions?type=listening&hsk=HSK%203");
assert(
  publishedQuestions.response.status === 200
    && publishedQuestions.body?.length === 1
    && publishedQuestions.body[0]?.id === "bootstrap-listening-classroom"
    && !publishedQuestions.body[0]?.acceptedAnswers,
  "published question query leaked draft or answer key"
);

const learnerQuestionAdmin = await request("/api/admin/content/questions", { headers: userHeaders });
assert(learnerQuestionAdmin.response.status === 403, "learner accessed question bank admin");

const contentAdminHeaders = {
  "X-Dev-User-Id": "smoke-content-admin",
  "X-Dev-Permission": "content.manage"
};
const questionAdminList = await request("/api/admin/content/questions", { headers: contentAdminHeaders });
assert(
  questionAdminList.response.status === 200
    && questionAdminList.body?.some(item => item.id === "bootstrap-listening-draft" && item.status === "Draft"),
  "question bank admin did not see draft question"
);

const questionDraft = await request(`/api/admin/content/questions/${contentQuestionId}/draft`, {
  method: "POST",
  headers: contentAdminHeaders,
  body: JSON.stringify({
    type: "reading",
    prompt: "Đọc: 你好吗? Có nghĩa là gì?",
    acceptedAnswers: ["Bạn khỏe không?"],
    options: null,
    hskLevel: "HSK 3",
    skill: "Đọc",
    knowledgeId: "vocab-nihao",
    explanation: "Câu hỏi chào hỏi cơ bản.",
    difficulty: 1,
    sourceType: "PlatformAuthoredReferenceFixture",
    sourceVersion: "question-reference-v1",
    licenseRef: "platform-authored"
  })
});
assert(questionDraft.response.status === 200 && questionDraft.body?.status === "Draft" && questionDraft.body?.contentVersion === 1, "question draft save failed");

const publishedQuestion = await request(`/api/admin/content/questions/${contentQuestionId}/publish`, {
  method: "POST",
  headers: contentAdminHeaders
});
assert(publishedQuestion.response.status === 200 && publishedQuestion.body?.status === "Published", "question publish failed");

const publicQuestion = await request("/api/content/questions?type=reading&hsk=HSK%203&skill=%C4%90%E1%BB%8Dc");
assert(
  publicQuestion.response.status === 200
    && publicQuestion.body?.some(item => item.id === contentQuestionId)
    && !publicQuestion.body?.some(item => item.acceptedAnswers),
  "published question did not become visible without leaking answer key"
);

const mutatePublishedQuestion = await request(`/api/admin/content/questions/${contentQuestionId}/draft`, {
  method: "POST",
  headers: contentAdminHeaders,
  body: JSON.stringify({
    type: "reading",
    prompt: "Không được sửa câu đã publish.",
    acceptedAnswers: ["không"],
    options: null,
    hskLevel: "HSK 3",
    skill: "Đọc",
    knowledgeId: "vocab-nihao",
    explanation: null,
    difficulty: 1,
    sourceType: "PlatformAuthoredReferenceFixture",
    sourceVersion: "question-reference-v1",
    licenseRef: "platform-authored"
  })
});
assert(mutatePublishedQuestion.response.status === 409, "published question was mutable");

const publishedStories = await request("/api/content/stories?hsk=HSK%203&topic=L%E1%BB%9Bp%20h%E1%BB%8Dc");
assert(
  publishedStories.response.status === 200
  && publishedStories.body?.length === 1
  && publishedStories.body[0]?.id === "story-classroom",
  "published story filter is incorrect"
);

const draftStory = await request("/api/content/stories/story-weekend-draft");
assert(draftStory.response.status === 404, "unpublished story leaked to learner");

const learnerContentAdmin = await request("/api/admin/content/stories", { headers: userHeaders });
assert(learnerContentAdmin.response.status === 403, "learner accessed content admin");

const contentAdminList = await request("/api/admin/content/stories", { headers: contentAdminHeaders });
assert(
  contentAdminList.response.status === 200
  && contentAdminList.body?.some(item => item.id === "story-weekend-draft" && item.status === "Draft"),
  "content admin did not see draft story"
);

const publishStory = await request("/api/admin/content/stories/story-weekend-draft/publish", {
  method: "POST",
  headers: contentAdminHeaders
});
assert(publishStory.response.status === 200 && publishStory.body?.status === "Published", "content publish failed");

const publishedDraftStory = await request("/api/content/stories/story-weekend-draft");
assert(publishedDraftStory.response.status === 200, "published story did not become visible to learner");

const publicReadyAudio = await request("/api/content/audio/audio-story-classroom");
assert(
  publicReadyAudio.response.status === 200
    && publicReadyAudio.body?.status === "Ready"
    && publicReadyAudio.body?.audioUrl,
  "ready audio was not available to learner"
);

const learnerAudioAdmin = await request("/api/admin/content/audio", { headers: userHeaders });
assert(learnerAudioAdmin.response.status === 403, "learner accessed audio admin");

const audioAdminList = await request("/api/admin/content/audio", { headers: contentAdminHeaders });
assert(
  audioAdminList.response.status === 200
    && audioAdminList.body?.some(item => item.id === "audio-video-pronunciation" && item.status === "Failed"),
  "audio admin did not expose failed asset"
);

const audioRequest = await request("/api/admin/content/audio", {
  method: "POST",
  headers: contentAdminHeaders,
  body: JSON.stringify({
    contentId: audioContentId,
    text: "你好，我叫小明。",
    voice: "cosyvoice-v1",
    idempotencyKey: audioIdempotencyKey
  })
});
assert(audioRequest.response.status === 201 && audioRequest.body?.status === "Pending", "audio request did not create pending asset");
const audioAssetId = audioRequest.body?.id;
assert(typeof audioAssetId === "string", "audio asset id is missing");

const duplicateAudioRequest = await request("/api/admin/content/audio", {
  method: "POST",
  headers: contentAdminHeaders,
  body: JSON.stringify({
    contentId: audioContentId,
    text: "你好，我叫小明。",
    voice: "cosyvoice-v1",
    idempotencyKey: audioIdempotencyKey
  })
});
assert(duplicateAudioRequest.response.status === 200 && duplicateAudioRequest.body?.id === audioAssetId, "duplicate audio request was not idempotent");

const conflictingAudioRequest = await request("/api/admin/content/audio", {
  method: "POST",
  headers: contentAdminHeaders,
  body: JSON.stringify({
    contentId: `${audioContentId}-other`,
    text: "再见。",
    voice: "other-voice",
    idempotencyKey: audioIdempotencyKey
  })
});
assert(conflictingAudioRequest.response.status === 409, "audio idempotency key conflict was accepted");

const pendingAudio = await request(`/api/content/audio/${audioAssetId}`);
assert(pendingAudio.response.status === 404, "pending audio leaked to learner");

const retriedAudio = await request("/api/admin/content/audio/audio-video-pronunciation/retry", {
  method: "POST",
  headers: contentAdminHeaders
});
assert(retriedAudio.response.status === 200 && retriedAudio.body?.status === "Pending" && retriedAudio.body?.attemptCount === 2, "failed audio retry did not enqueue attempt 2");

const repeatedAudioRetry = await request("/api/admin/content/audio/audio-video-pronunciation/retry", {
  method: "POST",
  headers: contentAdminHeaders
});
assert(repeatedAudioRetry.response.status === 409, "non-failed audio was retried again");

console.log("Identity + Curriculum + Learning + Practice + Review + Exam + Translation + Speaking + Content + Audio smoke tests passed");
