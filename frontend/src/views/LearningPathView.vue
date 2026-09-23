<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { getJson, postJson } from '../services/api'
import type { BeginnerLearning, BeginnerStage, HskLearning, HskLevel, LearningState } from '../types'

const props = defineProps<{ mode: 'hsk' | 'beginner'; level?: string }>()
const loading = ref(true)
const error = ref('')
const levels = ref<HskLevel[]>([])
const beginner = ref<BeginnerLearning | null>(null)
const hsk = ref<HskLearning | null>(null)
const actionMessage = ref('')
const foundationStages = computed(() => beginner.value?.track.stages.filter(stage => stage.id === 'pinyin' || stage.id === 'tones') ?? [])
const upcomingStages = computed(() => beginner.value?.track.stages.filter(stage => stage.id !== 'pinyin' && stage.id !== 'tones') ?? [])

const load = async () => {
  loading.value = true
  error.value = ''
  levels.value = []
  beginner.value = null
  hsk.value = null
  actionMessage.value = ''

  try {
    if (props.mode === 'beginner') {
      beginner.value = await getJson<BeginnerLearning>('/api/learning/beginner')
    } else if (props.level) {
      hsk.value = await getJson<HskLearning>(`/api/learning/hsk/${encodeURIComponent(props.level)}`)
    } else {
      levels.value = await getJson<HskLevel[]>('/api/curriculum/hsk-levels')
    }
  } catch {
    error.value = 'Chưa tải được dữ liệu lộ trình. Kiểm tra phiên đăng nhập hoặc dữ liệu curriculum đã publish.'
  } finally {
    loading.value = false
  }
}

watch(() => `${props.mode}:${props.level ?? ''}`, load, { immediate: true })

async function startBeginner() {
  actionMessage.value = ''
  try {
    const updated = await postJson<LearningState>('/api/learning/beginner/start')
    if (beginner.value) beginner.value.state = updated
    actionMessage.value = 'Đã bắt đầu lộ trình nền tảng.'
  } catch {
    actionMessage.value = 'Không thể bắt đầu lộ trình lúc này.'
  }
}

async function selectHsk() {
  if (!hsk.value) return
  actionMessage.value = ''
  try {
    hsk.value.state = await postJson<LearningState>(`/api/learning/hsk/${encodeURIComponent(hsk.value.curriculum.level.id)}/select`)
    actionMessage.value = `Đã chọn ${hsk.value.curriculum.level.displayName} làm lộ trình hiện tại.`
  } catch {
    actionMessage.value = 'Không thể chọn lộ trình lúc này.'
  }
}

function beginnerStageDescription(stage: BeginnerStage) {
  if (beginner.value?.state.completedBeginnerStageIds.includes(stage.id)) return 'Đã hoàn tất'
  if (beginner.value?.state.currentBeginnerStageId === stage.id) return 'Đang học'
  return stage.status === 'Published' ? 'Sẵn sàng học' : 'Đang chuẩn bị'
}

function beginnerStageAction(stage: BeginnerStage) {
  if (beginner.value?.state.completedBeginnerStageIds.includes(stage.id)) return 'Đã xong'
  if (beginner.value?.state.currentBeginnerStageId === stage.id) return 'Tiếp tục'
  return stage.status === 'Published' ? 'Mở nội dung' : 'Đang chuẩn bị'
}
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải lộ trình…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>

  <template v-else-if="mode === 'beginner' && beginner">
    <p v-if="actionMessage" class="inline-message">{{ actionMessage }}</p>
    <section class="page-card path-list">
      <div class="page-section-title"><div><h2>Học nền tảng theo thứ tự</h2><p>{{ beginner.track.name }}</p></div><div class="page-section-title__actions"><span>{{ beginner.track.stages.length }} bước</span><button class="page-button page-button--blue" type="button" @click="startBeginner">{{ beginner.state.currentTrack === 'beginner' ? 'Tiếp tục' : 'Bắt đầu' }} →</button></div></div>
      <RouterLink v-for="stage in foundationStages" :key="stage.id" class="path-row" :to="stage.id === 'pinyin' ? '/app/pinyin' : '/app/tones'">
        <span class="page-list-row__icon step-number">{{ stage.order }}</span>
        <div class="page-list-row__body"><strong>{{ stage.name }}</strong><span>{{ beginnerStageDescription(stage) }}</span></div>
        <span class="page-list-row__meta"><span class="page-status" :class="{ 'page-status--active': beginner.state.currentBeginnerStageId === stage.id }">{{ beginnerStageAction(stage) }}</span><span aria-hidden="true">→</span></span>
      </RouterLink>
      <article v-for="stage in upcomingStages" :key="stage.id" class="path-row path-row--upcoming">
        <span class="page-list-row__icon step-number">{{ stage.order }}</span>
        <div class="page-list-row__body"><strong>{{ stage.name }}</strong><span>Nội dung đang được xây dựng</span></div>
        <span class="page-list-row__meta"><span class="page-status">Đang chuẩn bị</span></span>
      </article>
    </section>
  </template>

  <template v-else-if="mode === 'hsk' && hsk">
    <p v-if="actionMessage" class="inline-message">{{ actionMessage }}</p>
    <section class="page-card path-list">
      <div class="page-section-title">
        <div><h2>{{ hsk.curriculum.level.displayName }}</h2><span v-if="hsk.isLevelCompleted" class="page-status">Đã hoàn tất các bài hiện có</span></div>
        <button class="page-button page-button--blue" type="button" @click="selectHsk">{{ hsk.state.selectedHskLevelId === hsk.curriculum.level.id ? 'Đang chọn' : 'Chọn lộ trình' }} →</button>
      </div>
      <div v-if="hsk.curriculum.topics.length" class="hsk-tree">
        <section v-for="topic in hsk.curriculum.topics" :key="topic.id" class="hsk-tree__topic">
          <div class="hsk-tree__topic-heading"><strong>{{ topic.name }}</strong><span>{{ topic.units.length }} unit</span></div>
          <div v-for="unit in topic.units" :key="unit.id" class="hsk-tree__unit">
            <div class="hsk-tree__unit-heading">{{ unit.name }} <span v-if="hsk.completedUnitIds?.includes(unit.id)" class="page-status">Đã hoàn tất</span></div>
            <RouterLink v-for="lesson in unit.lessons" :key="lesson.id" class="path-row" :to="`/app/lessons/${lesson.id}`">
              <span class="page-list-row__icon step-number">课</span>
              <div class="page-list-row__body"><strong>{{ lesson.name }}</strong><span>{{ hsk.state.completedLessonIds.includes(lesson.id) ? 'Đã hoàn tất' : hsk.state.startedLessonIds.includes(lesson.id) ? 'Đang học' : 'Chưa học' }}</span></div>
              <span class="page-list-row__meta"><span class="page-status page-status--active">{{ hsk.state.completedLessonIds.includes(lesson.id) ? 'Xem lại' : hsk.state.startedLessonIds.includes(lesson.id) ? 'Tiếp tục' : 'Mở bài' }}</span><span aria-hidden="true">→</span></span>
            </RouterLink>
          </div>
        </section>
      </div>
      <div v-else class="empty-inline">Cấp độ này chưa có bài học đã publish.</div>
    </section>
  </template>

  <template v-else>
    <div v-if="levels.length" class="level-grid">
      <article v-for="level in levels" :key="level.id" class="level-card">
        <div class="level-badge">HSK {{ level.levelNumber }}</div>
        <div><h2>{{ level.displayName }}</h2><p>Phiên dữ liệu: {{ level.syllabusVersionId }}</p></div>
        <RouterLink class="text-link" :to="`/app/hsk/${level.id}`">Mở cấp độ <span>→</span></RouterLink>
      </article>
    </div>
    <div v-else class="empty-page-card"><div class="empty-icon">+</div><div><h2>Chưa có HSK đã publish</h2><p>Admin cần import và publish syllabus trước khi learner chọn cấp độ.</p></div></div>
  </template>
</template>
