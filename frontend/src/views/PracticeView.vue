<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
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

const title = computed(() => props.type ? `Luyện ${props.type}` : 'Luyện tập')

onMounted(async () => {
  try { questions.value = await getJson<PracticeQuestion[]>('/api/practice/questions') }
  catch { error.value = 'Chưa tải được bộ câu hỏi luyện tập.' }
  finally { loading.value = false }
})

async function startSession() {
  working.value = true; error.value = ''; notice.value = ''
  try { session.value = await postJson<PracticeSession>('/api/practice/sessions', { questionIds: questions.value.map(question => question.id) }) }
  catch { error.value = 'Không thể bắt đầu phiên luyện tập.' }
  finally { working.value = false }
}

async function answer(question: PracticeQuestion) {
  if (!session.value || !answers.value[question.id]) return
  working.value = true; error.value = ''
  try {
    const attempt = await postJson<PracticeSession['attempts'][number]>(`/api/practice/sessions/${session.value.id}/answers`, { questionId: question.id, answer: answers.value[question.id] })
    const existing = session.value.attempts.filter(item => item.questionId !== question.id)
    session.value = { ...session.value, attempts: [...existing, attempt] }
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
  } catch { error.value = 'Không thể hoàn thành phiên luyện tập.' }
  finally { working.value = false }
}

function resultFor(questionId: string) { return session.value?.attempts.find(attempt => attempt.questionId === questionId)?.result }
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải câu hỏi…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>
  <template v-else-if="!session"><section class="page-card page-card--soft page-feature-intro"><p class="page-kicker">HỌC TẬP · {{ title.toUpperCase() }}</p><h1>{{ title }}</h1><p class="page-copy">Làm một câu, nhận feedback ngay và đưa nội dung cần ôn vào đúng hàng đợi.</p><div class="page-actions"><button class="page-button page-button--blue" type="button" :disabled="working || !questions.length" @click="startSession">Bắt đầu →</button></div><p class="toolbar-copy">{{ questions.length }} câu đã publish · Question bank chính thức sẽ được nối qua Content module sau khi có dữ liệu được duyệt.</p></section></template>
  <template v-else><section class="practice-list"><article v-for="question in session.questions" :key="question.id" class="practice-card"><div class="practice-card-head"><span class="level-badge">{{ question.type }}</span><span v-if="resultFor(question.id)" class="path-status">{{ resultFor(question.id) }}</span></div><h2>{{ question.prompt }}</h2><div class="practice-answer"><input v-model="answers[question.id]" :disabled="session.status === 'Completed'" placeholder="Nhập câu trả lời" @keyup.enter="answer(question)" /><button class="text-link button-link" type="button" :disabled="working || session.status === 'Completed'" @click="answer(question)">Chấm câu <span>→</span></button></div></article></section><button v-if="session.status !== 'Completed'" class="primary-button complete-button" type="button" :disabled="working" @click="complete">Hoàn thành phiên <span>→</span></button></template>
  <p v-if="notice" class="inline-message">{{ notice }}</p>
</template>
