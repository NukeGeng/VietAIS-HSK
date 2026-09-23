<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { getJson, postJson } from '../services/api'
import type { ExamAttempt, ExamDefinition, ExamResult } from '../types'

type ExamMode = 'catalog' | 'detail' | 'attempt' | 'result'

const props = defineProps<{ mode?: ExamMode; examId?: string; attemptId?: string }>()
const router = useRouter()
const exams = ref<ExamDefinition[]>([])
const exam = ref<ExamDefinition | null>(null)
const attempt = ref<ExamAttempt | null>(null)
const result = ref<ExamResult | null>(null)
const loading = ref(true)
const working = ref(false)
const error = ref('')
const actionMessage = ref('')
const currentQuestionIndex = ref(0)
const draftAnswer = ref('')
const submitConfirm = ref(false)

const currentQuestion = computed(() => attempt.value?.questions[currentQuestionIndex.value] ?? null)
const answeredCount = computed(() => {
  const saved = Object.values(attempt.value?.answers ?? {}).filter(answer => answer.trim().length > 0).length
  const questionId = currentQuestion.value?.id
  const draftCounts = questionId
    && draftAnswer.value.trim().length > 0
    && !(attempt.value?.answers[questionId] ?? '').trim()
  return saved + (draftCounts ? 1 : 0)
})
const unansweredCount = computed(() => Math.max(0, (attempt.value?.questions.length ?? 0) - answeredCount.value))

watch(() => currentQuestion.value?.id, () => {
  const questionId = currentQuestion.value?.id
  draftAnswer.value = questionId ? attempt.value?.answers[questionId] ?? '' : ''
})

async function load() {
  loading.value = true
  error.value = ''
  actionMessage.value = ''
  exams.value = []
  exam.value = null
  attempt.value = null
  result.value = null
  currentQuestionIndex.value = 0
  draftAnswer.value = ''
  submitConfirm.value = false
  try {
    if (props.attemptId) {
      attempt.value = await getJson<ExamAttempt>(`/api/exam-attempts/${encodeURIComponent(props.attemptId)}`)
      if (props.mode === 'result' || attempt.value.status !== 'Active') {
        result.value = await getJson<ExamResult>(`/api/exam-attempts/${encodeURIComponent(props.attemptId)}/result`)
      }
    } else if (props.examId) {
      exam.value = await getJson<ExamDefinition>(`/api/exams/${encodeURIComponent(props.examId)}`)
    } else {
      exams.value = await getJson<ExamDefinition[]>('/api/exams')
    }
  } catch {
    error.value = 'Chưa tải được nội dung đề thi hoặc phiên thi.'
  } finally {
    loading.value = false
  }
}

watch(
  () => `${props.mode ?? 'catalog'}:${props.examId ?? ''}:${props.attemptId ?? ''}`,
  load,
  { immediate: true },
)

async function start(examId: string) {
  working.value = true
  actionMessage.value = ''
  try {
    const created = await postJson<ExamAttempt>(`/api/exams/${encodeURIComponent(examId)}/attempts`)
    attempt.value = created
    await router.push(`/app/exam-attempts/${created.id}`)
  } catch {
    actionMessage.value = 'Không thể bắt đầu đề thi.'
  } finally {
    working.value = false
  }
}

async function submitAnswer(questionId: string, answer: string): Promise<boolean> {
  if (!attempt.value || attempt.value.status !== 'Active') return false
  working.value = true
  try {
    attempt.value = await postJson<ExamAttempt>(`/api/exam-attempts/${attempt.value.id}/answers`, { questionId, answer })
    return true
  } catch {
    actionMessage.value = 'Không thể lưu câu trả lời.'
    return false
  } finally {
    working.value = false
  }
}

async function saveCurrentAnswer() {
  const questionId = currentQuestion.value?.id
  if (!questionId || !attempt.value || draftAnswer.value === (attempt.value.answers[questionId] ?? '')) return true
  return submitAnswer(questionId, draftAnswer.value)
}

async function goToQuestion(index: number) {
  if (index === currentQuestionIndex.value || !attempt.value || index < 0 || index >= attempt.value.questions.length) return
  if (!await saveCurrentAnswer()) return
  currentQuestionIndex.value = index
}

async function goPrevious() {
  await goToQuestion(currentQuestionIndex.value - 1)
}

async function goNext() {
  if (!attempt.value) return
  if (currentQuestionIndex.value === attempt.value.questions.length - 1) {
    submitConfirm.value = true
    return
  }
  await goToQuestion(currentQuestionIndex.value + 1)
}

function openSubmitConfirm() {
  submitConfirm.value = true
}

async function confirmSubmit() {
  submitConfirm.value = false
  if (!await saveCurrentAnswer()) return
  await submit()
}

async function submit() {
  if (!attempt.value || attempt.value.status !== 'Active') return
  working.value = true
  actionMessage.value = ''
  try {
    attempt.value = await postJson<ExamAttempt>(`/api/exam-attempts/${attempt.value.id}/submit`)
    result.value = await getJson<ExamResult>(`/api/exam-attempts/${attempt.value.id}/result`)
    await router.push(`/app/exam-attempts/${attempt.value.id}/result`)
  } catch {
    actionMessage.value = 'Không thể nộp bài.'
  } finally {
    working.value = false
  }
}
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải đề thi…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>
  <template v-else>
    <p v-if="actionMessage" class="inline-message">{{ actionMessage }}</p>

    <section v-if="result && attempt" class="page-card empty-page-card">
      <div class="empty-icon">✓</div>
      <div>
        <p class="app-eyebrow">KẾT QUẢ THI THỬ</p>
        <h2>{{ result.objectiveScore }} / 100</h2>
        <p>Đúng {{ result.correct }} / {{ result.total }} câu. {{ result.incorrectQuestionIds.length ? 'Bạn có thể quay lại luyện các câu sai.' : 'Bạn đã hoàn thành toàn bộ câu hỏi.' }}</p>
        <p v-if="result.subjectiveGradingStatus === 'Pending'" class="inline-message">Phần tự luận đang chờ chấm. Kết quả sẽ được cập nhật khi hoàn tất.</p>
        <p v-else-if="result.subjectiveGradingStatus === 'Completed'" class="inline-message">Điểm tự luận: {{ result.subjectiveScore ?? 0 }} / 100<span v-if="result.subjectiveFeedback"> · {{ result.subjectiveFeedback }}</span></p>
        <p v-else-if="result.subjectiveGradingStatus === 'Failed'" class="inline-message">Phần tự luận chưa chấm được. Bạn có thể thử lại sau.</p>
      </div>
    </section>

    <section v-else-if="attempt" class="page-card exam-card">
      <div class="page-section-title"><div><p class="app-eyebrow">THI THỬ · {{ attempt.contentVersion }}</p><h2>{{ attempt.status === 'Active' ? 'Đang làm bài' : 'Bài thi đã nộp' }}</h2><p>{{ answeredCount }} / {{ attempt.questions.length }} câu đã lưu</p></div><span>{{ attempt.subjectiveGradingStatus === 'Pending' ? 'Chờ chấm tự luận' : attempt.status }}</span></div>
      <nav class="exam-question-nav" aria-label="Điều hướng câu hỏi"><button v-for="(question, index) in attempt.questions" :key="question.id" type="button" :class="{ 'is-current': index === currentQuestionIndex, 'is-answered': Boolean(attempt.answers[question.id]?.trim()) }" :disabled="attempt.status !== 'Active' || working" :aria-label="`Mở câu ${index + 1}`" @click="goToQuestion(index)">{{ index + 1 }}</button></nav>
      <div v-if="currentQuestion" class="exam-question"><span class="exam-question__label">Câu {{ currentQuestionIndex + 1 }} / {{ attempt.questions.length }}</span><strong>{{ currentQuestion.prompt }}</strong><input v-model="draftAnswer" :disabled="attempt.status !== 'Active' || working" placeholder="Nhập câu trả lời" @keyup.enter="goNext" /></div>
      <div class="page-focus-actions"><button class="page-button page-button--quiet" type="button" :disabled="working || currentQuestionIndex === 0 || attempt.status !== 'Active'" @click="goPrevious">← Câu trước</button><button v-if="currentQuestionIndex < attempt.questions.length - 1" class="page-button page-button--blue" type="button" :disabled="working || attempt.status !== 'Active'" @click="goNext">Câu tiếp theo →</button><button v-else class="page-button page-button--blue" type="button" :disabled="working || attempt.status !== 'Active'" @click="openSubmitConfirm">Nộp bài →</button></div>
      <div v-if="submitConfirm" class="page-confirm" role="dialog" aria-modal="true" aria-labelledby="exam-submit-title"><strong id="exam-submit-title">Xác nhận nộp bài?</strong><p>{{ unansweredCount ? `${unansweredCount} câu chưa có câu trả lời.` : 'Bạn đã trả lời tất cả câu hỏi.' }} Sau khi nộp, bạn không thể sửa đáp án.</p><div class="page-focus-actions"><button class="page-button page-button--quiet" type="button" @click="submitConfirm = false">Quay lại</button><button class="page-button page-button--blue" type="button" :disabled="working" @click="confirmSubmit">Xác nhận nộp bài</button></div></div>
    </section>

    <section v-else-if="exam" class="page-card page-detail__main">
      <p class="app-eyebrow">THI THỬ · {{ exam.hskLevel }}</p><h1>{{ exam.name }}</h1><div class="page-meta"><span>{{ exam.questions.length }} câu hỏi</span><span>Version {{ exam.contentVersion }}</span></div><p class="page-copy">Làm bài theo từng câu. Câu trả lời được lưu theo phiên để bạn có thể refresh và tiếp tục.</p><div class="page-focus-actions"><button class="page-button page-button--blue" type="button" :disabled="working" @click="start(exam.id)">Bắt đầu →</button></div>
    </section>

    <div v-else class="level-grid"><article v-for="item in exams" :key="item.id" class="level-card"><div class="level-badge">{{ item.hskLevel }}</div><div><h2>{{ item.name }}</h2><p>{{ item.questions.length }} câu · {{ item.contentVersion }}</p></div><button class="text-link button-link" type="button" @click="router.push(`/app/exams/${item.id}`)">Xem đề <span>→</span></button></article><div v-if="!exams.length" class="empty-page-card"><div class="empty-icon">+</div><div><h2>Chưa có đề thi</h2><p>Admin cần publish exam content trước khi learner bắt đầu.</p></div></div></div>
  </template>
</template>
