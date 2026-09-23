<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { getJson, postJson } from '../services/api'
import type { HskLesson, LearningHome, LearningState } from '../types'

const props = defineProps<{ id: string }>()
const loading = ref(true)
const error = ref('')
const actionMessage = ref('')
const lesson = ref<HskLesson | null>(null)
const state = ref<LearningState | null>(null)
const working = ref(false)
let requestSequence = 0

const started = computed(() => Boolean(state.value?.startedLessonIds.includes(props.id)))
const completed = computed(() => Boolean(state.value?.completedLessonIds.includes(props.id)))

async function load() {
  const requestId = ++requestSequence
  loading.value = true
  error.value = ''
  actionMessage.value = ''
  lesson.value = null
  state.value = null

  try {
    const [nextLesson, nextState] = await Promise.all([
      getJson<HskLesson>(`/api/curriculum/lessons/${encodeURIComponent(props.id)}`),
      getJson<LearningHome>('/api/learning/home').then(home => home.state),
    ])

    if (requestId !== requestSequence) return
    lesson.value = nextLesson
    state.value = nextState
  } catch {
    if (requestId !== requestSequence) return
    error.value = 'Không tải được bài học. Bài học có thể chưa được publish hoặc phiên đăng nhập đã hết.'
  } finally {
    if (requestId === requestSequence) loading.value = false
  }
}

watch(() => props.id, load, { immediate: true })

async function startLesson() {
  working.value = true
  actionMessage.value = ''
  try {
    state.value = await postJson<LearningState>(`/api/learning/lessons/${encodeURIComponent(props.id)}/start`)
    actionMessage.value = 'Đã bắt đầu bài học.'
  } catch {
    actionMessage.value = 'Không thể bắt đầu bài học lúc này.'
  } finally {
    working.value = false
  }
}

async function completeLesson() {
  working.value = true
  actionMessage.value = ''
  try {
    state.value = await postJson<LearningState>(`/api/learning/lessons/${encodeURIComponent(props.id)}/complete`)
    actionMessage.value = 'Đã hoàn thành bài học.'
  } catch {
    actionMessage.value = 'Hãy bắt đầu bài học trước khi hoàn thành.'
  } finally {
    working.value = false
  }
}
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải bài học…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>
  <template v-else-if="lesson">
    <RouterLink class="page-back" to="/app/lessons">← Bài học</RouterLink>
    <section class="page-detail">
      <article class="page-card page-detail__main">
        <p class="app-eyebrow">BÀI HỌC · {{ lesson.status === 'Published' ? 'ĐÃ PUBLISH' : lesson.status }}</p>
        <h1>{{ lesson.name }}</h1>
        <div class="page-meta"><span>{{ lesson.level?.displayName ?? 'HSK' }}</span><span>{{ completed ? 'Đã hoàn thành' : started ? 'Đang học' : 'Chưa bắt đầu' }}</span></div>
        <div class="page-content-block"><h2>Mục tiêu bài học</h2><p>Nội dung bài học, từ vựng, mẫu câu và phần luyện tập liên quan sẽ được nối từ Curriculum/Content theo version đã publish.</p></div>
        <p v-if="actionMessage" class="inline-message">{{ actionMessage }}</p>
        <div class="page-focus-actions">
          <button v-if="!started || completed" class="page-button page-button--blue" type="button" :disabled="working || completed" @click="startLesson">{{ completed ? 'Đã hoàn thành' : 'Bắt đầu bài học' }} →</button>
          <button v-else class="page-button page-button--blue" type="button" :disabled="working" @click="completeLesson">Hoàn thành bài học →</button>
        </div>
      </article>
      <aside class="page-card page-side-card"><p class="app-eyebrow">TRẠNG THÁI</p><strong>{{ completed ? 'Đã hoàn thành' : started ? 'Đang học' : 'Sẵn sàng' }}</strong><p class="page-callout">Trạng thái học được lưu theo learner và có thể tiếp tục sau khi tải lại.</p></aside>
    </section>
  </template>
</template>
