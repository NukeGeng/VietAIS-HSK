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
