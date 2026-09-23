<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { getJson } from '../services/api'
import type { HskLevel, HskLevelTree, LearningHome, ProgressSnapshot } from '../types'

const loading = ref(true)
const error = ref('')
const home = ref<LearningHome | null>(null)
const progress = ref<ProgressSnapshot | null>(null)
const levels = ref<HskLevel[]>([])
const tree = ref<HskLevelTree | null>(null)

type LessonTarget = { id: string; name: string; topic: string; unit: string }

const lessonTargets = computed<LessonTarget[]>(() => tree.value?.topics.flatMap(topic => topic.units.flatMap(unit => unit.lessons.map(lesson => ({
  id: lesson.id,
  name: lesson.name,
  topic: topic.name,
  unit: unit.name,
})))) ?? [])
const completedLessonIds = computed(() => new Set(home.value?.state.completedLessonIds ?? []))
const completedLessonCount = computed(() => lessonTargets.value.filter(lesson => completedLessonIds.value.has(lesson.id)).length)
const nextLessons = computed(() => {
  const current = lessonTargets.value.find(lesson =>
    lesson.id === home.value?.state.currentLessonId && !completedLessonIds.value.has(lesson.id))
  const rest = lessonTargets.value.filter(lesson =>
    !completedLessonIds.value.has(lesson.id) && lesson.id !== current?.id)
  return [...(current ? [current] : []), ...rest].slice(0, 3)
})
const continueRoute = computed(() => {
  const target = home.value?.continueTarget
  if (!target) return null
  if (target.startsWith('beginner/')) return `/app/${target}`
  return `/app/lessons/${encodeURIComponent(target)}`
})
const selectedLevel = computed(() => levels.value.find(level => level.id === home.value?.state.selectedHskLevelId) ?? levels.value[0] ?? null)
const currentLevelLabel = computed(() => selectedLevel.value?.displayName ?? 'Chưa chọn HSK')
const overall = computed(() => {
  if (!lessonTargets.value.length) return 0
  return Math.round(completedLessonCount.value / lessonTargets.value.length * 100)
})

const skillCards = [
  { label: 'Từ vựng', icon: '词', value: 'Tra cứu', to: '/app/vocabulary' },
  { label: 'Hán tự', icon: '字', value: 'Luyện chữ', to: '/app/hanzi' },
  { label: 'Ngữ pháp', icon: '语', value: 'Mẫu câu', to: '/app/grammar' },
  { label: 'Kỹ năng', icon: '◌', value: 'Mở nhóm trên sidebar' },
]

const skillOverview = [
  { label: 'Nghe', icon: '听', value: 'Mở bài luyện', to: '/app/skills/listening' },
  { label: 'Đọc', icon: '读', value: 'Mở bài luyện', to: '/app/skills/reading' },
  { label: 'Viết', icon: '写', value: 'Mở bài luyện', to: '/app/skills/writing' },
  { label: 'Nói - đối thoại', icon: '说', value: 'Mở phiên nói', to: '/app/skills/speaking' },
  { label: 'Dịch Việt - Trung', icon: '译', value: 'Mở bài dịch', to: '/app/skills/translation' },
]

onMounted(async () => {
  try {
    ;[home.value, progress.value, levels.value] = await Promise.all([
      getJson<LearningHome>('/api/learning/home'),
      getJson<ProgressSnapshot>('/api/progress'),
      getJson<HskLevel[]>('/api/curriculum/hsk-levels'),
    ])
    if (selectedLevel.value) {
      tree.value = await getJson<HskLevelTree>(`/api/curriculum/hsk/${encodeURIComponent(selectedLevel.value.id)}/tree`)
    }
  } catch {
    error.value = 'Chưa tải được dữ liệu học tập. Bạn có thể tiếp tục với các trang nền tảng.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <section class="app-page-heading app-page-heading--minimal app-reveal" aria-label="Ngữ cảnh trang">
    <p class="app-eyebrow">TỔNG QUAN HỌC VIÊN</p>
  </section>

  <div v-if="loading" class="state-card">Đang tải dữ liệu học tập…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>

  <template v-else>
    <section class="app-progress-card app-reveal app-delay-1" aria-labelledby="progress-title">
      <div class="app-progress-card__top">
        <div class="app-progress-card__identity">
          <span class="app-progress-card__icon">↗</span>
            <div><p class="app-eyebrow">LỘ TRÌNH HSK</p><h2 id="progress-title">{{ currentLevelLabel }} · Tiến độ học</h2></div>
          </div>
        <div class="app-progress-stats"><span>{{ progress?.practiceCorrect ?? 0 }} câu đúng</span><span>{{ progress?.practiceIncorrect ?? 0 }} cần xem lại</span></div>
      </div>
      <div class="app-progress-bar" role="progressbar" aria-label="Tiến độ học HSK" aria-valuemin="0" aria-valuemax="100" :aria-valuenow="overall" tabindex="0">
        <span class="app-progress-bar__fill" :style="{ width: `${overall}%` }" />
        <span class="app-progress-bar__value" aria-hidden="true">{{ overall }}%</span>
      </div>
      <div v-if="levels.length" class="app-levels"><span v-for="level in levels" :key="level.id" :class="{ 'is-current': level.id === selectedLevel?.id }">{{ level.displayName }}</span></div>
      <div v-else class="empty-inline">Chưa có cấp độ HSK đã publish.</div>
      <div class="app-skill-grid">
        <component :is="card.to ? RouterLink : 'div'" v-for="card in skillCards" :key="card.label" class="app-skill-card" :to="card.to">
          <span class="app-skill-card__icon">{{ card.icon }}</span>
          <span><strong>{{ card.label }}</strong><small>{{ card.value }}</small></span>
          <span v-if="card.to" class="app-skill-card__arrow">→</span>
        </component>
      </div>
    </section>

    <section class="app-next-card app-reveal app-delay-2" aria-labelledby="next-title">
      <div class="app-section-heading">
        <h2 id="next-title">Bài học tiếp theo</h2>
        <div class="app-section-heading__actions">
          <RouterLink v-if="continueRoute" class="app-inline-action" :to="continueRoute">Tiếp tục học →</RouterLink>
          <span class="app-tag">{{ currentLevelLabel }}</span>
        </div>
      </div>
      <RouterLink v-for="lesson in nextLessons" :key="lesson.id" class="app-lesson-row" :to="`/app/lessons/${lesson.id}`"><span class="app-lesson-row__hanzi">课</span><span class="app-lesson-row__body"><strong>{{ lesson.name }}</strong><small>{{ lesson.topic }} · {{ lesson.unit }}</small></span><span class="app-lesson-row__arrow">→</span></RouterLink>
      <div v-if="!nextLessons.length" class="empty-inline">Chưa có bài học tiếp theo trong curriculum đã publish.</div>
    </section>

    <section class="app-next-card app-skill-overview app-reveal app-delay-2" id="skills" aria-labelledby="skills-overview-title">
      <div class="app-section-heading"><h2 id="skills-overview-title">Kỹ năng</h2><span class="app-tag">{{ currentLevelLabel }}</span></div>
      <p class="app-skill-overview__copy">Theo dõi từng kỹ năng và đi thẳng vào nội dung cần luyện.</p>
      <div class="app-skill-overview__grid">
        <RouterLink v-for="card in skillOverview" :key="card.label" class="app-skill-card" :to="card.to"><span class="app-skill-card__icon">{{ card.icon }}</span><span><strong>{{ card.label }}</strong><small>{{ card.value }}</small></span><span class="app-skill-card__arrow">→</span></RouterLink>
      </div>
    </section>
  </template>
</template>
