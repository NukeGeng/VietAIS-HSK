<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { getJson, postJson } from '../services/api'
import type { PracticeQuestion, PracticeSession } from '../types'

const props = defineProps<{ type?: string }>()
const questions = ref<PracticeQuestion[]>([])
const session = ref<PracticeSession | null>(null)
const answers = ref<Record<string, string>>({})
const loading = ref(true)
const working = ref(false)
const error = ref('')
const notice = ref('')

const typeLabels: Record<string, string> = {
  vocabulary: 'Từ vựng',
  pinyin: 'Pinyin',
  tone: 'Thanh điệu',
  hanzi: 'Chữ Hán',
  grammar: 'Ngữ pháp',
  listening: 'Nghe',
  reading: 'Đọc',
  writing: 'Viết',
}

const typeLabel = computed(() => props.type ? typeLabels[props.type] ?? props.type : 'Luyện tập')
const title = computed(() => props.type ? `Luyện ${typeLabel.value}` : 'Luyện tập')
const sessionStorageKey = computed(() => `vietais.practice.session.${props.type ?? 'all'}`)

async function load() {
  loading.value = true
  error.value = ''
  notice.value = ''
  questions.value = []
  session.value = null
  answers.value = {}
  const query = props.type ? `?type=${encodeURIComponent(props.type)}` : ''
  try {
    questions.value = await getJson<PracticeQuestion[]>(`/api/practice/questions${query}`)
    const storedSessionId = window.sessionStorage.getItem(sessionStorageKey.value)
    if (storedSessionId) {
      try {
        session.value = await getJson<PracticeSession>(`/api/practice/sessions/${encodeURIComponent(storedSessionId)}`)
        for (const attempt of session.value.attempts) answers.value[attempt.questionId] = attempt.answer
      } catch {
        window.sessionStorage.removeItem(sessionStorageKey.value)
      }
    }
  } catch {
    error.value = 'Chưa tải được bộ câu hỏi luyện tập.'
  } finally {
    loading.value = false
  }
}

watch(() => props.type, load, { immediate: true })

async function startSession() {
  working.value = true; error.value = ''; notice.value = ''
  try {
    session.value = await postJson<PracticeSession>('/api/practice/sessions', { questionIds: questions.value.map(question => question.id) })
    window.sessionStorage.setItem(sessionStorageKey.value, session.value.id)
  }
  catch { error.value = 'Không thể bắt đầu phiên luyện tập.' }
  finally { working.value = false }
}

async function answer(question: PracticeQuestion) {
  if (!session.value || !answers.value[question.id]) return
  working.value = true; error.value = ''
  try {
    const attempt = await postJson<PracticeSession['attempts'][number]>(`/api/practice/sessions/${session.value.id}/answers`, { questionId: question.id, answer: answers.value[question.id] })
    const existing = session.value.attempts.filter(item => item.questionId !== question.id)
    session.value = { ...session.value, attempts: [...existing, attempt], updatedAt: attempt.submittedAt }
  } catch { error.value = 'Không thể chấm câu trả lời lúc này.' }
  finally { working.value = false }
}

async function complete() {
  if (!session.value) return
  working.value = true; error.value = ''
  try {
    const result = await postJson<{ correct: number; incorrect: number; answered: number; total: number }>(`/api/practice/sessions/${session.value.id}/complete`)
    notice.value = `Hoàn thành: đúng ${result.correct}/${result.total} câu.`
    session.value = { ...session.value, status: 'Completed' }
    window.sessionStorage.removeItem(sessionStorageKey.value)
  } catch { error.value = 'Không thể hoàn thành phiên luyện tập.' }
  finally { working.value = false }
}

function resultFor(questionId: string) { return session.value?.attempts.find(attempt => attempt.questionId === questionId)?.result }
function selectOption(questionId: string, option: string) { answers.value[questionId] = option }
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải câu hỏi…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>
  <template v-else-if="!session"><section class="practice-start"><div><h2>{{ title }}</h2><p>{{ questions.length }} câu {{ props.type ? `cho ${typeLabel.toLowerCase()}` : 'đã publish' }} · Chỉ câu hỏi đã được duyệt từ Content mới xuất hiện.</p></div><button class="page-button page-button--blue" type="button" :disabled="working || !questions.length" @click="startSession">Bắt đầu →</button></section></template>
  <template v-else><section class="practice-list"><article v-for="question in session.questions" :key="question.id" class="practice-card"><div class="practice-card-head"><span class="level-badge">{{ typeLabels[question.type] ?? question.type }}</span><span v-if="resultFor(question.id)" class="path-status">{{ resultFor(question.id) }}</span></div><h2>{{ question.prompt }}</h2><div v-if="question.options?.length" class="page-option-grid practice-option-grid"><button v-for="(option, optionIndex) in question.options" :key="option" class="page-option" :class="{ 'is-selected': answers[question.id] === option }" type="button" :disabled="session.status === 'Completed'" @click="selectOption(question.id, option)"><b>{{ String.fromCharCode(65 + optionIndex) }}</b><span>{{ option }}</span></button></div><div class="practice-answer"><input v-if="!question.options?.length" v-model="answers[question.id]" :disabled="session.status === 'Completed'" placeholder="Nhập câu trả lời" @keyup.enter="answer(question)" /><button class="text-link button-link" type="button" :disabled="working || session.status === 'Completed' || !answers[question.id]" @click="answer(question)">Chấm câu <span>→</span></button></div></article></section><button v-if="session.status !== 'Completed'" class="primary-button complete-button" type="button" :disabled="working" @click="complete">Hoàn thành phiên <span>→</span></button></template>
  <p v-if="notice" class="inline-message">{{ notice }}</p>
</template>
