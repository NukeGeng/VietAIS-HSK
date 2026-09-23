export type NavItem = {
  label: string
  to?: string
  children?: NavItem[]
}

export type NavGroup = {
  label: string
  items: NavItem[]
}

export type LearningState = {
  currentTrack: string | null
  selectedHskLevelId: string | null
  currentLessonId: string | null
  currentBeginnerStageId: string | null
  startedBeginnerStageIds: string[]
  completedBeginnerStageIds: string[]
  startedLessonIds: string[]
  completedLessonIds: string[]
  updatedAt: string
}

export type LearningHome = {
  state: LearningState
  continueTarget: string | null
}

export type ProgressSnapshot = {
  practiceAnswered: number
  practiceCorrect: number
  practiceIncorrect: number
  completedActivities: number
  reviewAnswered?: number
  reviewCorrect?: number
  reviewIncorrect?: number
  examAttempts?: number
  examQuestions?: number
  examCorrect?: number
  examIncorrect?: number
  translationAttempts?: number
  speakingSessions?: number
  speakingTurns?: number
}

export type AuthorizationContext = {
  userId: string
  permissions: string[]
}

export type ProgressMastery = {
  knowledgeType: string
  knowledgeId: string
  attemptCount: number
  correctCount: number
  incorrectCount: number
  scorePercent: number
  state: string
}

export type BeginnerStage = {
  id: string
  order: number
  name: string
  status: string
  masterDataRefs?: string[]
}

export type FoundationItem = {
  id: string
  category: string
  label: string
  description: string
  pinyin: string | null
  tone: number | null
  example: string | null
  exampleMeaning: string | null
}

export type FoundationCatalog = {
  name: string
  sourceType: string
  version: string
  editorialNote: string
  sources: { name: string; url: string; scope: string }[]
  items: FoundationItem[]
}

export type HanziCharacter = {
  id: string
  character: string
  pinyin: string
  meaning: string
  radical: string
  strokeCount: number
  hskContext: string
  relatedWords: string[]
  source: string
  sourceVersion: string
  licenseRef: string
}

export type HanziStroke = { id: string; order: number; description: string; path: string }

export type HanziStrokeSet = {
  hanziId: string
  strokeCount: number
  strokes: HanziStroke[]
  source: string
  sourceVersion: string
  licenseRef: string
}

export type HanziWritingMode = 'Guided' | 'Trace' | 'Recall'

export type HanziWritingStrokeResult = {
  order: number
  result: 'Correct' | 'NeedsRetry' | 'Incorrect'
  feedback: string
  evaluatedAt: string
}

export type HanziWritingAttempt = {
  id: string
  hanziId: string
  strokeCount: number
  acceptedStrokeCount: number
  strokeSourceVersion: string
  mode: HanziWritingMode
  status: 'Active' | 'Completed'
  strokeResults: HanziWritingStrokeResult[]
  overallResult: 'Correct' | 'Incorrect' | 'NeedsRetry' | null
  startedAt: string
  completedAt: string | null
}

export type VocabularyExample = { chinese: string; pinyin: string; vietnamese: string }

export type VocabularyEntry = {
  id: string
  simplified: string
  pinyin: string
  meaning: string
  partOfSpeech: string
  hskLevel: string
  topic: string
  relatedHanzi: string[]
  examples: VocabularyExample[]
  status: string
  sourceType: string
  sourceVersion: string
  licenseRef: string
}

export type GrammarExample = { chinese: string; pinyin: string; vietnamese: string }

export type GrammarPoint = {
  id: string
  pattern: string
  title: string
  explanation: string
  hskLevel: string
  topic: string
  commonMistakes: string[]
  examples: GrammarExample[]
  relatedLessonId: string | null
  status: string
  sourceType: string
  sourceVersion: string
  licenseRef: string
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

export type HskLesson = {
  id: string
  name: string
  status: string
  level?: HskLevel
  topicName?: string
  unitName?: string
}

export type HskUnit = { id: string; name: string; lessons: HskLesson[] }

export type HskTopic = { id: string; name: string; units: HskUnit[] }

export type HskLevelTree = { level: HskLevel; topics: HskTopic[] }

export type HskLearning = {
  curriculum: HskLevelTree
  state: LearningState
  completedUnitIds?: string[]
  isLevelCompleted?: boolean
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

export type ReviewSession = {
  id: string
  userId: string
  itemIds: string[]
  createdAt: string
  recordedResults: Record<string, boolean> | null
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
  questions: { id: string; prompt: string; questionType?: string }[]
}

export type ExamAttempt = {
  id: string
  examId: string
  contentVersion: string
  status: string
  questions: { id: string; prompt: string; questionType?: string }[]
  answers: Record<string, string>
  objectiveScore: number | null
  subjectiveGradingStatus?: string
  subjectiveScore?: number | null
  subjectiveFeedback?: string | null
}

export type ExamResult = {
  attemptId: string
  status: string
  objectiveScore: number | null
  correct: number
  total: number
  incorrectQuestionIds: string[]
  subjectiveGradingStatus?: string
  subjectiveScore?: number | null
  subjectiveFeedback?: string | null
}

export type PracticeQuestion = { id: string; type: string; prompt: string; status: string; options?: string[] | null; contentVersion?: number }

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
