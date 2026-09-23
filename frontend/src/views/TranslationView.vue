<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ApiError, getJson, postJson } from '../services/api'

type TranslationExercise = {
  id: string
  promptVietnamese: string
  hskContext: string
  status: string
}

type TranslationFeedback = {
  meaning: string
  grammar: string
  wordChoice: string
  naturalness: string
  suggestedRevision: string
  status: string
}

type TranslationAttempt = {
  id: string
  exerciseId: string
  answerChinese: string
  submittedAt: string
  feedback: TranslationFeedback | null
}

const exercises = ref<TranslationExercise[]>([])
const history = ref<TranslationAttempt[]>([])
const selectedId = ref('')
const answer = ref('')
const attempt = ref<TranslationAttempt | null>(null)
const loading = ref(true)
const working = ref(false)
const feedbackLoading = ref(false)
const error = ref('')
const notice = ref('')

const selectedExercise = computed(() => exercises.value.find(exercise => exercise.id === selectedId.value) ?? exercises.value[0] ?? null)

onMounted(async () => {
  try {
    const [catalog, previous] = await Promise.all([
      getJson<TranslationExercise[]>('/api/translation/exercises'),
      getJson<TranslationAttempt[]>('/api/translation/history'),
    ])
    exercises.value = catalog
    history.value = previous
    selectedId.value = catalog[0]?.id ?? ''
  } catch {
    error.value = 'Chưa tải được bài dịch. Vui lòng thử tải lại trang.'
  } finally {
    loading.value = false
  }
})

function chooseExercise(id: string) {
  selectedId.value = id
  answer.value = ''
  attempt.value = null
  notice.value = ''
  error.value = ''
}

async function submit() {
  if (!selectedExercise.value || !answer.value.trim()) return
  working.value = true
  error.value = ''
  notice.value = ''
  try {
    const created = await postJson<TranslationAttempt>(`/api/translation/exercises/${selectedExercise.value.id}/attempts`, { answerChinese: answer.value })
    attempt.value = created
    history.value = [created, ...history.value.filter(item => item.id !== created.id)]
    notice.value = 'Đã lưu câu trả lời. Bạn có thể tự đối chiếu hoặc yêu cầu góp ý.'
  } catch {
    error.value = 'Không thể lưu câu trả lời lúc này.'
  } finally {
    working.value = false
  }
}

async function requestFeedback() {
  if (!attempt.value) return
  feedbackLoading.value = true
  error.value = ''
  try {
    const updated = await postJson<TranslationAttempt>(`/api/translation/attempts/${attempt.value.id}/feedback`)
    attempt.value = updated
    history.value = history.value.map(item => item.id === updated.id ? updated : item)
  } catch (caught) {
    if (caught instanceof ApiError && caught.status === 504) {
      error.value = 'Dịch vụ góp ý phản hồi quá lâu. Câu trả lời của bạn vẫn được lưu.'
    } else if (caught instanceof ApiError && caught.status === 502) {
      error.value = 'Dịch vụ góp ý đang tạm thời không khả dụng. Câu trả lời của bạn vẫn được lưu.'
    } else {
      error.value = 'Góp ý chưa sẵn sàng. Câu trả lời của bạn vẫn được lưu.'
    }
  } finally {
    feedbackLoading.value = false
  }
}
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải bài dịch…</div>
  <div v-else-if="error && !selectedExercise" class="state-card state-card-muted">{{ error }}</div>
  <template v-else-if="selectedExercise">
    <div class="translation-layout">
      <section class="page-card translation-card">
        <div class="page-section-title">
          <div><p class="page-kicker">DỊCH VIỆT - TRUNG</p><h2>Luyện một câu</h2><p>Viết câu tiếng Trung theo tình huống {{ selectedExercise.hskContext }}.</p></div>
          <span>{{ selectedExercise.hskContext }}</span>
        </div>
        <div class="translation-exercise-switch" aria-label="Chọn câu dịch">
          <button v-for="exercise in exercises" :key="exercise.id" type="button" :class="{ 'is-active': exercise.id === selectedExercise.id }" @click="chooseExercise(exercise.id)">{{ exercise.id }}</button>
        </div>
        <div class="translation-prompt">
          <span class="page-kicker">CÂU TIẾNG VIỆT</span>
          <strong>{{ selectedExercise.promptVietnamese }}</strong>
        </div>
        <label class="page-form-label" for="translation-answer">Câu trả lời tiếng Trung</label>
        <textarea id="translation-answer" v-model="answer" class="page-textarea translation-answer" :disabled="Boolean(attempt)" placeholder="Nhập câu tiếng Trung của bạn…" />
        <div class="translation-actions">
          <button class="page-button page-button--blue" type="button" :disabled="working || Boolean(attempt) || !answer.trim()" @click="submit">{{ working ? 'Đang lưu…' : 'Lưu câu trả lời' }} <span aria-hidden="true">→</span></button>
          <button v-if="attempt && !attempt.feedback" class="page-button page-button--quiet" type="button" :disabled="feedbackLoading" @click="requestFeedback">{{ feedbackLoading ? 'Đang lấy góp ý…' : 'Nhận góp ý' }}</button>
        </div>
        <p v-if="notice" class="inline-message">{{ notice }}</p>
        <p v-if="error" class="inline-message inline-message--error">{{ error }}</p>
        <div v-if="attempt?.feedback" class="translation-feedback">
          <div class="translation-feedback__heading"><strong>Góp ý cho câu trả lời</strong><span>{{ attempt.feedback.status }}</span></div>
          <dl><div><dt>Ý nghĩa</dt><dd>{{ attempt.feedback.meaning }}</dd></div><div><dt>Ngữ pháp</dt><dd>{{ attempt.feedback.grammar }}</dd></div><div><dt>Dùng từ</dt><dd>{{ attempt.feedback.wordChoice }}</dd></div><div><dt>Độ tự nhiên</dt><dd>{{ attempt.feedback.naturalness }}</dd></div><div><dt>Gợi ý sửa</dt><dd>{{ attempt.feedback.suggestedRevision }}</dd></div></dl>
        </div>
      </section>

      <aside class="page-card translation-history">
        <div class="page-section-title"><div><h2>Lịch sử gần đây</h2><p>Các câu đã lưu của bạn.</p></div><span>{{ history.length }}</span></div>
        <div v-if="history.length" class="translation-history__list"><button v-for="item in history.slice(0, 5)" :key="item.id" type="button" :class="{ 'is-active': item.id === attempt?.id }" @click="selectedId = item.exerciseId; attempt = item; answer = item.answerChinese"><strong>{{ item.answerChinese }}</strong><small>{{ item.feedback ? 'Đã có góp ý' : 'Chưa yêu cầu góp ý' }}</small></button></div>
        <p v-else class="empty-inline">Chưa có câu trả lời nào.</p>
      </aside>
    </div>
  </template>
</template>

<style scoped>
.translation-layout { display: grid; grid-template-columns: minmax(0, 1.3fr) minmax(240px, .7fr); gap: 16px; }
.translation-card { overflow: hidden; padding-bottom: 20px; }
.translation-exercise-switch { display: flex; gap: 7px; padding: 0 22px 16px; }
.translation-exercise-switch button { min-width: 32px; height: 30px; border: 1px solid var(--app-line); border-radius: 9px; background: #fff; color: var(--app-muted); cursor: pointer; font-size: 11px; font-weight: 800; }
.translation-exercise-switch button.is-active { border-color: rgba(47,109,229,.3); background: var(--app-blue-soft); color: var(--app-blue); }
.translation-prompt { display: grid; gap: 9px; margin: 0 22px 18px; padding: 17px; border: 1px solid rgba(47,109,229,.16); border-radius: 13px; background: linear-gradient(110deg, var(--app-blue-soft), #fff 80%); }
.translation-prompt strong { font-size: 19px; letter-spacing: -.03em; line-height: 1.35; }
.translation-card > .page-form-label, .translation-answer { margin-inline: 22px; width: calc(100% - 44px); }
.translation-answer { min-height: 128px; }
.translation-actions { display: flex; flex-wrap: wrap; gap: 8px; margin: 12px 22px 0; }
.translation-actions .page-button { min-height: 36px; }
.inline-message { margin: 12px 22px 0; color: #39714c; font-size: 11px; line-height: 1.5; }
.inline-message--error { color: #a34444; }
.translation-feedback { margin: 18px 22px 0; padding: 15px; border: 1px solid var(--app-line); border-radius: 13px; background: #fafaf8; }
.translation-feedback__heading { display: flex; align-items: center; justify-content: space-between; gap: 10px; font-size: 12px; }
.translation-feedback__heading span { color: var(--app-blue); font-size: 10px; font-weight: 800; }
.translation-feedback dl { display: grid; gap: 9px; margin: 13px 0 0; }
.translation-feedback dl div { display: grid; grid-template-columns: 92px minmax(0, 1fr); gap: 8px; }
.translation-feedback dt { color: var(--app-muted); font-size: 10px; font-weight: 800; }
.translation-feedback dd { margin: 0; color: #4f4a47; font-size: 11px; line-height: 1.45; }
.translation-history { align-self: start; overflow: hidden; }
.translation-history__list { display: grid; gap: 1px; padding: 0 12px 12px; }
.translation-history__list button { display: grid; gap: 4px; padding: 12px 10px; border: 0; border-bottom: 1px solid rgba(33,29,30,.07); background: transparent; color: var(--app-ink); cursor: pointer; text-align: left; }
.translation-history__list button:hover, .translation-history__list button.is-active { background: var(--app-blue-soft); }
.translation-history__list strong { overflow: hidden; font-family: "Noto Sans SC", sans-serif; font-size: 14px; text-overflow: ellipsis; white-space: nowrap; }
.translation-history__list small { color: var(--app-muted); font-size: 10px; }
.empty-inline { margin: 0; padding: 0 22px 22px; color: var(--app-muted); font-size: 11px; }
@media (max-width: 900px) { .translation-layout { grid-template-columns: 1fr; } }
@media (max-width: 520px) { .translation-feedback dl div { grid-template-columns: 1fr; gap: 3px; } .translation-prompt { margin-inline: 16px; } .translation-card > .page-form-label, .translation-answer { margin-inline: 16px; width: calc(100% - 32px); } .translation-actions { margin-inline: 16px; } .translation-feedback { margin-inline: 16px; } }
</style>
