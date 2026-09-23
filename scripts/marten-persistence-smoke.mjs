const baseUrl = process.env.IDENTITY_API_URL ?? "http://127.0.0.1:5056";
const mode = process.env.PERSISTENCE_MODE ?? "write";
const headers = {
  "Content-Type": "application/json",
  "X-Dev-User-Id": process.env.MARTEN_PERSISTENCE_SMOKE_USER_ID ?? "marten-persistence-user"
};
const learningHeaders = {
  ...headers,
  "X-Dev-User-Id": process.env.MARTEN_LEARNING_SMOKE_USER_ID ?? headers["X-Dev-User-Id"]
};
const curriculumAdminHeaders = { ...headers, "X-Dev-Permission": "curriculum.manage" };
const reviewKnowledgeId = "bootstrap-tone-ma";
const curriculumImportPayload = {
  syllabusVersionId: "marten-hsk-3.0-tree-smoke",
  syllabusName: "HSK 3.0 curriculum tree persistence smoke",
  sourceType: "smoke-test",
  levels: [{
    id: "marten-hsk3-tree-smoke",
    levelNumber: 3,
    displayName: "HSK 3",
    topics: [{
      id: "marten-topic-tree-smoke",
      name: "Topic persistence probe",
      units: [{
        id: "marten-unit-tree-smoke",
        name: "Unit persistence probe",
        lessons: [
          { id: "marten-lesson-published-smoke", name: "Published lesson persistence probe" },
          { id: "marten-lesson-draft-smoke", name: "Draft lesson visibility probe" }
        ]
      }]
    }]
  }]
};

async function request(path, options = {}) {
  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers: { ...headers, ...(options.headers ?? {}) }
  });
  const text = await response.text();
  return { response, body: text ? JSON.parse(text) : null };
}

if (mode === "write") {
  const update = await request("/api/me/profile", {
    method: "PUT",
    body: JSON.stringify({
      displayName: "Marten persisted",
      avatarUrl: null,
      timezone: "Asia/Ho_Chi_Minh",
      studyPreferences: { dailyMinutes: 45, preferredStudyTime: "21:00" }
    })
  });
  if (update.response.status !== 200) {
    throw new Error(`write expected 200, got ${update.response.status}`);
  }

  const importResponse = await request("/api/admin/curriculum/import", {
    method: "POST",
    headers: curriculumAdminHeaders,
    body: JSON.stringify(curriculumImportPayload)
  });
  if (importResponse.response.status !== 200) {
    throw new Error(`curriculum write expected 200, got ${importResponse.response.status}`);
  }
  const hiddenTree = await request("/api/curriculum/hsk/marten-hsk3-tree-smoke/tree");
  if (![200, 404].includes(hiddenTree.response.status)) {
    throw new Error(`curriculum tree before publish expected 200 or 404, got ${hiddenTree.response.status}`);
  }
  if (hiddenTree.response.status === 200) {
    if (importResponse.body?.idempotent !== true) {
      throw new Error("re-importing identical published curriculum should be idempotent");
    }
    const alreadyVisibleIds = hiddenTree.body?.topics?.flatMap(topic => topic.units?.flatMap(unit => unit.lessons?.map(lesson => lesson.id) ?? []) ?? []) ?? [];
    if (!alreadyVisibleIds.includes("marten-lesson-published-smoke")
      || alreadyVisibleIds.includes("marten-lesson-draft-smoke")) {
      throw new Error("previously published tree should keep only the published lesson visible");
    }
  }

  const publishResponse = await request("/api/admin/curriculum/hsk-levels/marten-hsk3-tree-smoke/publish", {
    method: "POST",
    headers: curriculumAdminHeaders
  });
  if (publishResponse.response.status !== 200) {
    throw new Error(`curriculum publish expected 200, got ${publishResponse.response.status}`);
  }

  const levelTree = await request("/api/curriculum/hsk/marten-hsk3-tree-smoke/tree");
  if (levelTree.response.status !== 200
    || (hiddenTree.response.status === 404 && levelTree.body?.topics?.length !== 0)) {
    throw new Error(`draft lesson visibility expected an empty published tree, got ${levelTree.response.status}`);
  }

  const lessonPublishPath = "/api/admin/curriculum/hsk-levels/marten-hsk3-tree-smoke/lessons/marten-lesson-published-smoke/publish";
  const forbiddenLessonPublish = await request(lessonPublishPath, {
    method: "POST",
    headers
  });
  if (forbiddenLessonPublish.response.status !== 403) {
    throw new Error(`lesson publish without curriculum.manage expected 403, got ${forbiddenLessonPublish.response.status}`);
  }

  const lessonPublish = await request(lessonPublishPath, {
    method: "POST",
    headers: curriculumAdminHeaders
  });
  if (lessonPublish.response.status !== 200 || lessonPublish.body?.id !== "marten-lesson-published-smoke") {
    throw new Error(`lesson publish expected 200, got ${lessonPublish.response.status}`);
  }

  const changedPublishedImport = structuredClone(curriculumImportPayload);
  changedPublishedImport.levels[0].topics[0].units[0].lessons[0].name = "Mutated after publication";
  const immutableImport = await request("/api/admin/curriculum/import", {
    method: "POST",
    headers: curriculumAdminHeaders,
    body: JSON.stringify(changedPublishedImport)
  });
  if (immutableImport.response.status !== 409) {
    throw new Error(`published curriculum mutation expected 409, got ${immutableImport.response.status}`);
  }

  const conflictingVersionImport = structuredClone(curriculumImportPayload);
  conflictingVersionImport.syllabusVersionId = `${curriculumImportPayload.syllabusVersionId}-revision`;
  conflictingVersionImport.syllabusName = "HSK 3.0 curriculum tree persistence smoke revision";
  const conflictingVersionResponse = await request("/api/admin/curriculum/import", {
    method: "POST",
    headers: curriculumAdminHeaders,
    body: JSON.stringify(conflictingVersionImport)
  });
  if (conflictingVersionResponse.response.status !== 409) {
    throw new Error(`cross-version level id reuse expected 409, got ${conflictingVersionResponse.response.status}`);
  }

  const draftLessonStart = await request("/api/learning/lessons/marten-lesson-draft-smoke/start", {
    method: "POST",
    headers: learningHeaders
  });
  if (draftLessonStart.response.status !== 404) {
    throw new Error(`draft lesson start expected 404, got ${draftLessonStart.response.status}`);
  }

  const beginnerStart = await request("/api/learning/beginner/start", { method: "POST", headers: learningHeaders });
  if (beginnerStart.response.status !== 200 || beginnerStart.body?.currentBeginnerStageId !== "pinyin") {
    throw new Error(`beginner start expected 200 with pinyin stage, got ${beginnerStart.response.status}`);
  }
  const selectHsk = await request("/api/learning/hsk/marten-hsk3-tree-smoke/select", {
    method: "POST",
    headers: learningHeaders
  });
  if (selectHsk.response.status !== 200 || selectHsk.body?.selectedHskLevelId !== "marten-hsk3-tree-smoke") {
    throw new Error(`HSK selection expected 200, got ${selectHsk.response.status}`);
  }

  const lessonStart = await request("/api/learning/lessons/marten-lesson-published-smoke/start", {
    method: "POST",
    headers: learningHeaders
  });
  if (lessonStart.response.status !== 200 || lessonStart.body?.currentLessonId !== "marten-lesson-published-smoke") {
    throw new Error(`published lesson start expected 200, got ${lessonStart.response.status}`);
  }

  const practiceSession = await request("/api/practice/sessions", {
    method: "POST",
    body: JSON.stringify({ questionIds: ["bootstrap-vocab-hello"] })
  });
  if (practiceSession.response.status !== 201
    || practiceSession.body?.questions?.[0]?.acceptedAnswers
    || !practiceSession.body?.id) {
    throw new Error(`practice session create expected 201 without answer key, got ${practiceSession.response.status}`);
  }
  const practiceSessionId = practiceSession.body.id;
  const practiceAnswer = await request(`/api/practice/sessions/${practiceSessionId}/answers`, {
    method: "POST",
    body: JSON.stringify({ questionId: "bootstrap-vocab-hello", answer: "Xin chào" })
  });
  if (practiceAnswer.response.status !== 200 || practiceAnswer.body?.result !== "Correct") {
    throw new Error(`practice answer expected deterministic Correct, got ${practiceAnswer.response.status}`);
  }
  const duplicatePracticeAnswer = await request(`/api/practice/sessions/${practiceSessionId}/answers`, {
    method: "POST",
    body: JSON.stringify({ questionId: "bootstrap-vocab-hello", answer: "Xin chào" })
  });
  if (duplicatePracticeAnswer.response.status !== 200 || duplicatePracticeAnswer.body?.result !== "Correct") {
    throw new Error(`duplicate practice answer retry expected Correct, got ${duplicatePracticeAnswer.response.status}`);
  }

  const reviewPracticeSession = await request("/api/practice/sessions", {
    method: "POST",
    body: JSON.stringify({ questionIds: [reviewKnowledgeId] })
  });
  if (reviewPracticeSession.response.status !== 201 || !reviewPracticeSession.body?.id) {
    throw new Error(`review practice session create expected 201, got ${reviewPracticeSession.response.status}`);
  }
  const wrongReviewAnswer = await request(`/api/practice/sessions/${reviewPracticeSession.body.id}/answers`, {
    method: "POST",
    body: JSON.stringify({ questionId: reviewKnowledgeId, answer: "2" })
  });
  if (wrongReviewAnswer.response.status !== 200 || wrongReviewAnswer.body?.result !== "Incorrect") {
    throw new Error(`review signal source expected deterministic Incorrect, got ${wrongReviewAnswer.response.status}`);
  }

  const reviewItems = await request("/api/review/mistakes");
  const reviewItem = reviewItems.body?.find(item => item.knowledgeId === reviewKnowledgeId && !item.resolved);
  if (reviewItems.response.status !== 200 || !reviewItem || reviewItem.mistakeCount < 1) {
    throw new Error(`Practice wrong answer should create a persistent review item, got ${reviewItems.response.status}`);
  }
  const reviewSession = await request("/api/review/sessions", {
    method: "POST",
    body: JSON.stringify({ itemIds: [reviewItem.id] })
  });
  if (reviewSession.response.status !== 201 || !reviewSession.body?.id) {
    throw new Error(`review session create expected 201, got ${reviewSession.response.status}`);
  }
  const progress = await request("/api/progress");
  if (progress.response.status !== 200
    || progress.body?.practiceAnswered !== 2
    || progress.body?.practiceCorrect !== 1
    || progress.body?.practiceIncorrect !== 1) {
    throw new Error(`practice result progress projection expected 2 answers/1 correct/1 incorrect, got ${progress.response.status}`);
  }
  const weakPoints = await request("/api/progress/weak-points");
  if (weakPoints.response.status !== 200
    || !weakPoints.body?.some(item => item.knowledgeId === reviewKnowledgeId && item.evidenceCount === 1)) {
    throw new Error(`incorrect practice signal should create one progress weak point, got ${weakPoints.response.status}`);
  }
  console.log(`Marten persistence write passed; PRACTICE_SMOKE_SESSION_ID=${practiceSessionId}; REVIEW_SMOKE_SESSION_ID=${reviewSession.body.id}`);
} else if (mode === "read") {
  const read = await request("/api/me");
  if (read.response.status !== 200 || read.body?.profile?.displayName !== "Marten persisted" || read.body?.profile?.studyPreferences?.dailyMinutes !== 45) {
    throw new Error(`Marten persistence read failed: ${read.response.status}`);
  }
  const curriculum = await request("/api/curriculum/hsk-levels");
  if (curriculum.response.status !== 200 || !curriculum.body?.some(level => level.id === "marten-hsk3-tree-smoke")) {
    throw new Error(`Marten curriculum read failed: ${curriculum.response.status}`);
  }

  const tree = await request("/api/curriculum/hsk/marten-hsk3-tree-smoke/tree");
  const persistedLessonIds = tree.body?.topics?.flatMap(topic => topic.units?.flatMap(unit => unit.lessons?.map(lesson => lesson.id) ?? []) ?? []) ?? [];
  if (tree.response.status !== 200
    || !persistedLessonIds.includes("marten-lesson-published-smoke")
    || persistedLessonIds.includes("marten-lesson-draft-smoke")) {
    throw new Error(`Marten curriculum tree read failed: ${tree.response.status}`);
  }

  const learning = await request("/api/learning/home", { headers: learningHeaders });
  if (learning.response.status !== 200
    || learning.body?.state?.currentTrack !== "hsk"
    || learning.body?.state?.selectedHskLevelId !== "marten-hsk3-tree-smoke"
    || learning.body?.state?.currentBeginnerStageId !== "pinyin"
    || learning.body?.state?.currentLessonId !== "marten-lesson-published-smoke"
    || !learning.body?.state?.startedLessonIds?.includes("marten-lesson-published-smoke")) {
    throw new Error(`Marten learning replay failed: ${learning.response.status}`);
  }
  const progressBeforeCompletion = await request("/api/progress");
  if (progressBeforeCompletion.response.status !== 200
    || progressBeforeCompletion.body?.practiceAnswered !== 2
    || progressBeforeCompletion.body?.practiceCorrect !== 1
    || progressBeforeCompletion.body?.practiceIncorrect !== 1) {
    throw new Error(`Marten progress signal replay failed: ${progressBeforeCompletion.response.status}`);
  }
  const privateProgress = await request("/api/progress", {
    headers: { "X-Dev-User-Id": "marten-other-progress-user" }
  });
  if (privateProgress.response.status !== 200 || privateProgress.body?.practiceAnswered !== 0) {
    throw new Error(`cross-user progress isolation failed: ${privateProgress.response.status}`);
  }

  const complete = await request("/api/learning/lessons/marten-lesson-published-smoke/complete", {
    method: "POST",
    headers: learningHeaders
  });
  if (complete.response.status !== 200
    || !complete.body?.completedLessonIds?.includes("marten-lesson-published-smoke")) {
    throw new Error(`persisted lesson completion failed: ${complete.response.status}`);
  }

  const practiceSessionId = process.env.PRACTICE_SMOKE_SESSION_ID;
  if (!practiceSessionId) {
    throw new Error("PRACTICE_SMOKE_SESSION_ID is required for the persistence read phase");
  }
  const practiceSession = await request(`/api/practice/sessions/${practiceSessionId}`);
  if (practiceSession.response.status !== 200
    || practiceSession.body?.status !== "Active"
    || practiceSession.body?.attempts?.[0]?.result !== "Correct"
    || practiceSession.body?.questions?.[0]?.acceptedAnswers) {
    throw new Error(`Marten practice session replay failed: ${practiceSession.response.status}`);
  }
  const privatePracticeSession = await request(`/api/practice/sessions/${practiceSessionId}`, {
    headers: { "X-Dev-User-Id": "marten-other-practice-user" }
  });
  if (privatePracticeSession.response.status !== 404) {
    throw new Error(`cross-user persisted practice access expected 404, got ${privatePracticeSession.response.status}`);
  }

  const completePractice = await request(`/api/practice/sessions/${practiceSessionId}/complete`, { method: "POST" });
  if (completePractice.response.status !== 200
    || completePractice.body?.status !== "Completed"
    || completePractice.body?.correct !== 1) {
    throw new Error(`persisted practice completion expected 200 with one correct answer, got ${completePractice.response.status}`);
  }

  const reviewSessionId = process.env.REVIEW_SMOKE_SESSION_ID;
  if (!reviewSessionId) {
    throw new Error("REVIEW_SMOKE_SESSION_ID is required for the persistence read phase");
  }
  const reviewItems = await request("/api/review/mistakes");
  const reviewItem = reviewItems.body?.find(item => item.knowledgeId === reviewKnowledgeId && !item.resolved);
  if (reviewItems.response.status !== 200 || !reviewItem || reviewItem.mistakeCount < 1) {
    throw new Error(`Marten review item replay after restart failed: ${reviewItems.response.status}`);
  }
  const privateReviewItems = await request("/api/review/mistakes", {
    headers: { "X-Dev-User-Id": "marten-other-review-user" }
  });
  if (privateReviewItems.response.status !== 200
    || privateReviewItems.body?.some(item => item.knowledgeId === reviewKnowledgeId)) {
    throw new Error(`cross-user review isolation failed: ${privateReviewItems.response.status}`);
  }

  const reviewResultPayload = { itemId: reviewItem.id, correct: true };
  const reviewResult = await request(`/api/review/sessions/${reviewSessionId}/results`, {
    method: "POST",
    body: JSON.stringify(reviewResultPayload)
  });
  if (reviewResult.response.status !== 200 || reviewResult.body?.resolved !== true) {
    throw new Error(`persisted review result expected 200/resolved, got ${reviewResult.response.status}`);
  }
  const repeatedReviewResult = await request(`/api/review/sessions/${reviewSessionId}/results`, {
    method: "POST",
    body: JSON.stringify(reviewResultPayload)
  });
  if (repeatedReviewResult.response.status !== 200
    || JSON.stringify(repeatedReviewResult.body) !== JSON.stringify(reviewResult.body)) {
    throw new Error(`identical review result retry should be idempotent, got ${repeatedReviewResult.response.status}`);
  }
  const conflictingReviewResult = await request(`/api/review/sessions/${reviewSessionId}/results`, {
    method: "POST",
    body: JSON.stringify({ itemId: reviewItem.id, correct: false })
  });
  if (conflictingReviewResult.response.status !== 404) {
    throw new Error(`conflicting review result retry expected 404, got ${conflictingReviewResult.response.status}`);
  }
  const openReviewItems = await request("/api/review/mistakes");
  if (openReviewItems.response.status !== 200
    || openReviewItems.body?.some(item => item.id === reviewItem.id)) {
    throw new Error("resolved review item should be omitted from the active mistakes queue");
  }
  const persistedProgressHistory = await request("/api/progress/history");
  const persistedActivityTypes = persistedProgressHistory.body?.map(item => item.activityType) ?? [];
  if (persistedProgressHistory.response.status !== 200
    || !persistedActivityTypes.includes("practice-completed")
    || !persistedActivityTypes.includes("review-completed")
    || persistedActivityTypes.includes("lesson-completed")) {
    throw new Error(`Persistence learner history ownership check failed, got ${persistedProgressHistory.response.status}`);
  }
  const learningProgressHistory = await request("/api/progress/history", { headers: learningHeaders });
  const learningActivityTypes = learningProgressHistory.body?.map(item => item.activityType) ?? [];
  if (learningProgressHistory.response.status !== 200
    || !learningActivityTypes.includes("lesson-completed")
    || learningActivityTypes.includes("practice-completed")
    || learningActivityTypes.includes("review-completed")) {
    throw new Error(`Learning learner history ownership check failed, got ${learningProgressHistory.response.status}`);
  }
  console.log("Marten Curriculum, Learning, Practice and Review restart read passed");
} else if (mode === "verify") {
  const learning = await request("/api/learning/home", { headers: learningHeaders });
  if (learning.response.status !== 200
    || !learning.body?.state?.completedLessonIds?.includes("marten-lesson-published-smoke")) {
    throw new Error(`Marten lesson completion replay failed: ${learning.response.status}`);
  }
  const practiceSessionId = process.env.PRACTICE_SMOKE_SESSION_ID;
  if (!practiceSessionId) {
    throw new Error("PRACTICE_SMOKE_SESSION_ID is required for the persistence verify phase");
  }
  const practiceSession = await request(`/api/practice/sessions/${practiceSessionId}`);
  if (practiceSession.response.status !== 200
    || practiceSession.body?.status !== "Completed"
    || practiceSession.body?.attempts?.[0]?.result !== "Correct"
    || practiceSession.body?.questions?.[0]?.acceptedAnswers) {
    throw new Error(`Marten practice completion replay failed: ${practiceSession.response.status}`);
  }
  const reviewItems = await request("/api/review/mistakes");
  if (reviewItems.response.status !== 200
    || reviewItems.body?.some(item => item.knowledgeId === reviewKnowledgeId)) {
    throw new Error(`Marten review resolution replay failed: ${reviewItems.response.status}`);
  }
  const progress = await request("/api/progress");
  if (progress.response.status !== 200
    || progress.body?.practiceAnswered !== 2
    || progress.body?.practiceCorrect !== 1
    || progress.body?.practiceIncorrect !== 1
    || progress.body?.completedActivities < 2) {
    throw new Error(`Marten progress projection replay failed: ${progress.response.status}`);
  }
  const persistedProgressHistory = await request("/api/progress/history");
  if (persistedProgressHistory.response.status !== 200
    || persistedProgressHistory.body?.length < 2
    || !persistedProgressHistory.body.some(item => item.activityType === "practice-completed")
    || !persistedProgressHistory.body.some(item => item.activityType === "review-completed")) {
    throw new Error(`Marten persistence progress history replay failed: ${persistedProgressHistory.response.status}`);
  }
  const learningProgressHistory = await request("/api/progress/history", { headers: learningHeaders });
  if (learningProgressHistory.response.status !== 200
    || learningProgressHistory.body?.length < 1
    || !learningProgressHistory.body.some(item => item.activityType === "lesson-completed")) {
    throw new Error(`Marten learning history replay failed: ${learningProgressHistory.response.status}`);
  }
  console.log("Marten Learning, Practice, Review and Progress completion replay passed");
} else {
  throw new Error(`Unknown PERSISTENCE_MODE: ${mode}`);
}
