export type NavItem = {
  label: string
  to?: string
  children?: NavItem[]
}

export type NavGroup = {
  label: string
  items: NavItem[]
}

export type LearningHome = {
  state: {
    currentTrack: string | null
    selectedHskLevelId: string | null
    currentLessonId: string | null
    currentBeginnerStageId: string | null
  }
  continueTarget: string | null
}

export type ProgressSnapshot = {
  practiceAnswered: number
  practiceCorrect: number
  practiceIncorrect: number
  completedActivities: number
}

export type BeginnerStage = {
  id: string
  order: number
  name: string
  status: string
}

export type LearningState = LearningHome['state'] & {
  startedLessonIds: string[]
  completedLessonIds: string[]
  updatedAt: string
}

export type BeginnerLearning = {
  track: { id: string; name: string; stages: BeginnerStage[] }
  state: LearningState
}

export type HskLevel = {
  id: string
  syllabusVersionId: string
  levelNumber: number
  displayName: string
  status: string
  updatedAt: string
}

export type ReviewSummary = { dueCount: number; mistakeCount: number; needsReviewCount: number }

export type ReviewItem = {
  id: string
  knowledgeType: string
  knowledgeId: string
  reason: string
  priority: number
  mistakeCount: number
  nextReviewAt: string
  resolved: boolean
}

export type ProgressWeakPoint = {
  knowledgeType: string
  knowledgeId: string
  reason: string
  evidenceCount: number
  action: string
}

export type ExamDefinition = {
  id: string
  name: string
  hskLevel: string
  contentVersion: string
  questions: { id: string; prompt: string }[]
}

export type ExamAttempt = {
  id: string
  examId: string
  status: string
  questions: { id: string; prompt: string }[]
  answers: Record<string, string>
  objectiveScore: number | null
}

export type ExamResult = {
  attemptId: string
  status: string
  objectiveScore: number | null
  correct: number
  total: number
  incorrectQuestionIds: string[]
}

export type PracticeQuestion = { id: string; type: string; prompt: string; status: string }

export type PracticeAttempt = { questionId: string; answer: string; result: string; submittedAt: string }

export type PracticeSession = {
  id: string
  status: string
  questions: PracticeQuestion[]
  attempts: PracticeAttempt[]
  createdAt: string
  updatedAt: string
}

export type IdentitySnapshot = {
  account: { userId: string; status: string }
  profile: {
    userId: string
    displayName: string | null
    avatarUrl: string | null
    preferredHskLevelId: string | null
    targetHskLevelId: string | null
    timezone: string
    studyPreferences: { dailyMinutes: number; preferredStudyTime: string | null }
  }
}
