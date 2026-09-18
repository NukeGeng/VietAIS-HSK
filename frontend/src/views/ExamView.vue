<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getJson, postJson } from '../services/api'
import type { ExamAttempt, ExamDefinition, ExamResult } from '../types'

const exams = ref<ExamDefinition[]>([])
const attempt = ref<ExamAttempt | null>(null)
const result = ref<ExamResult | null>(null)
const loading = ref(true)
const error = ref('')

onMounted(async () => { try { exams.value = await getJson<ExamDefinition[]>('/api/exams') } catch { error.value = 'Chưa tải được danh sách đề thi.' } finally { loading.value = false } })
async function start(examId: string) { try { attempt.value = await postJson<ExamAttempt>(`/api/exams/${examId}/attempts`) } catch { error.value = 'Không thể bắt đầu đề thi.' } }
async function submitAnswer(questionId: string, answer: string) { if (!attempt.value) return; try { attempt.value = await postJson<ExamAttempt>(`/api/exam-attempts/${attempt.value.id}/answers`, { questionId, answer }) } catch { error.value = 'Không thể lưu câu trả lời.' } }
async function submit() { if (!attempt.value) return; try { await postJson<ExamAttempt>(`/api/exam-attempts/${attempt.value.id}/submit`); result.value = await getJson<ExamResult>(`/api/exam-attempts/${attempt.value.id}/result`) } catch { error.value = 'Không thể nộp bài.' } }
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải đề thi…</div><div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>
  <template v-else>
    <section v-if="!attempt && !result" class="page-card page-card--soft page-feature-intro"><p class="page-kicker">KIỂM TRA HSK</p><h1>Thi thử</h1><p class="page-copy">Làm một đề nhỏ, nhận điểm objective và biết phần nào cần ôn tiếp.</p></section>
    <section v-if="result" class="page-card empty-page-card"><div class="empty-icon">✓</div><div><h2>{{ result.objectiveScore }} / {{ result.total }}</h2><p>Đúng {{ result.correct }} câu. Bạn có thể quay lại luyện các câu sai.</p></div></section>
    <section v-else-if="attempt" class="page-card exam-card"><div v-for="question in attempt.questions" :key="question.id" class="exam-question"><strong>{{ question.prompt }}</strong><input :value="attempt.answers[question.id] ?? ''" placeholder="Nhập câu trả lời" @change="submitAnswer(question.id, ($event.target as HTMLInputElement).value)" /></div><button class="page-button page-button--blue" type="button" @click="submit">Nộp bài →</button></section>
    <div v-else class="level-grid"><article v-for="exam in exams" :key="exam.id" class="level-card"><div class="level-badge">{{ exam.hskLevel }}</div><div><h2>{{ exam.name }}</h2><p>{{ exam.questions.length }} câu · {{ exam.contentVersion }}</p></div><button class="text-link button-link" type="button" @click="start(exam.id)">Bắt đầu <span>→</span></button></article></div>
  </template>
</template>
