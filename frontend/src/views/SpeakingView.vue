<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getJson, postJson } from '../services/api'
import type { HskLevel, LearningHome } from '../types'

type SpeakingTurn = {
  id: string
  transcript: string
  providerStatus: string
  assistantText: string | null
  createdAt: string
}

type SpeakingSession = {
  id: string
  hskContext: string
  mode: string
  status: 'Active' | 'Ended'
  turns: SpeakingTurn[]
}

const session = ref<SpeakingSession | null>(null)
const transcript = ref('')
const loading = ref(false)
const working = ref(false)
const error = ref('')
const notice = ref('')
const currentHsk = ref('HSK 3')

const canSend = computed(() => Boolean(session.value?.status === 'Active' && transcript.value.trim() && !working.value))

onMounted(async () => {
  try {
    const [levels, learning] = await Promise.all([
      getJson<HskLevel[]>('/api/curriculum/hsk-levels'),
      getJson<LearningHome>('/api/learning/home'),
    ])
    currentHsk.value = levels.find(level => level.id === learning.state.selectedHskLevelId)?.displayName
      ?? levels[0]?.displayName
      ?? 'HSK 3'
  } catch {
    // Keep the template fallback when the learner context is unavailable.
  }
})

async function start() {
  loading.value = true
  error.value = ''
  try {
    session.value = await postJson<SpeakingSession>('/api/speaking/sessions', { hskContext: currentHsk.value, mode: 'guided-dialogue' })
    notice.value = 'Phiên nói đã bắt đầu.'
  } catch {
    error.value = 'Không thể bắt đầu phiên nói.'
  } finally {
    loading.value = false
  }
}

async function sendTurn() {
  if (!session.value || !canSend.value) return
  working.value = true
  error.value = ''
  notice.value = ''
  try {
    const result = await postJson<{ turn: SpeakingTurn; nextAction: string }>(`/api/speaking/sessions/${session.value.id}/turns`, { transcript: transcript.value })
    session.value = { ...session.value, turns: [...session.value.turns, result.turn] }
    transcript.value = ''
    notice.value = result.nextAction === 'retry-provider' ? 'Đã lưu lượt nói. Có thể thử lại khi phản hồi sẵn sàng.' : 'Đã nhận lượt nói.'
  } catch {
    error.value = 'Không thể gửi lượt nói. Nội dung hiện tại chưa bị xóa.'
  } finally {
    working.value = false
  }
}

async function end() {
  if (!session.value) return
  working.value = true
  error.value = ''
  try {
    session.value = await postJson<SpeakingSession>(`/api/speaking/sessions/${session.value.id}/end`)
    notice.value = 'Đã kết thúc phiên nói.'
  } catch {
    error.value = 'Không thể kết thúc phiên nói.'
  } finally {
    working.value = false
  }
}
</script>

<template>
  <div v-if="error && !session" class="state-card state-card-muted">{{ error }}</div>
  <template v-else-if="!session">
    <section class="page-card speaking-start">
      <div><p class="page-kicker">NÓI - ĐỐI THOẠI</p><h2>Luyện hội thoại</h2><p>Chọn phiên {{ currentHsk }} để luyện phản hồi theo tình huống.</p></div>
      <button class="page-button page-button--blue" type="button" :disabled="loading" @click="start">{{ loading ? 'Đang mở…' : 'Bắt đầu phiên' }} <span aria-hidden="true">→</span></button>
    </section>
  </template>
  <section v-else class="speaking-layout">
    <div class="page-card speaking-card">
      <div class="page-section-title"><div><p class="page-kicker">{{ session.hskContext }}</p><h2>Nói - đối thoại</h2><p>{{ session.status === 'Active' ? 'Trả lời ngắn theo tình huống đang học.' : 'Phiên đã kết thúc.' }}</p></div><span class="page-status" :class="{ 'page-status--active': session.status === 'Active' }">{{ session.status === 'Active' ? 'Đang luyện' : 'Đã kết thúc' }}</span></div>
      <div v-if="session.turns.length" class="speaking-turns" aria-live="polite">
        <article v-for="turn in session.turns" :key="turn.id" class="speaking-turn">
          <span class="speaking-turn__avatar">HV</span>
          <div><strong>{{ turn.transcript }}</strong><p v-if="turn.assistantText">{{ turn.assistantText }}</p><small v-else-if="turn.providerStatus === 'timeout'">Đã lưu lượt nói; phản hồi quá lâu, bạn có thể thử lại sau.</small><small v-else-if="turn.providerStatus === 'error'">Đã lưu lượt nói; dịch vụ phản hồi đang tạm lỗi.</small><small v-else-if="turn.providerStatus === 'unavailable'">Đã lưu lượt nói; phản hồi sẽ thử lại sau.</small></div>
        </article>
      </div>
      <div v-else class="speaking-empty">Bắt đầu bằng một câu trả lời ngắn bằng tiếng Trung.</div>
      <div v-if="session.status === 'Active'" class="speaking-compose">
        <label class="page-form-label" for="speaking-transcript">Câu trả lời</label>
        <textarea id="speaking-transcript" v-model="transcript" class="page-textarea" placeholder="Ví dụ: 我每天晚上学习汉语。" @keyup.ctrl.enter="sendTurn" />
        <div class="speaking-actions"><button class="page-button page-button--blue" type="button" :disabled="!canSend" @click="sendTurn">{{ working ? 'Đang gửi…' : 'Gửi lượt nói' }} <span aria-hidden="true">→</span></button><button class="page-button page-button--quiet" type="button" :disabled="working" @click="end">Kết thúc</button></div>
      </div>
      <p v-if="notice" class="inline-message">{{ notice }}</p>
      <p v-if="error" class="inline-message inline-message--error">{{ error }}</p>
    </div>
    <aside class="page-card speaking-side"><div class="page-section-title"><div><h2>Phiên học</h2><p>Giữ câu trả lời ngắn và rõ.</p></div></div><div class="speaking-side__list"><span><small>Trình độ</small><strong>{{ session.hskContext }}</strong></span><span><small>Hình thức</small><strong>Hội thoại có hướng dẫn</strong></span><span><small>Lượt đã gửi</small><strong>{{ session.turns.length }}</strong></span></div></aside>
  </section>
</template>

<style scoped>
.speaking-start { display: flex; align-items: center; justify-content: space-between; gap: 18px; padding: 22px; }
.speaking-start h2 { margin: 5px 0 0; font-size: 22px; letter-spacing: -.045em; }
.speaking-start p:last-child { margin: 6px 0 0; color: var(--app-muted); font-size: 12px; }
.speaking-layout { display: grid; grid-template-columns: minmax(0, 1.3fr) minmax(230px, .7fr); gap: 16px; }
.speaking-card { overflow: hidden; }
.speaking-turns { display: grid; gap: 10px; padding: 0 22px 17px; max-height: 350px; overflow: auto; }
.speaking-turn { display: flex; align-items: flex-start; gap: 10px; padding: 11px 12px; border: 1px solid var(--app-line); border-radius: 12px; background: #fafaf8; }
.speaking-turn__avatar { display: grid; width: 28px; height: 28px; flex: 0 0 auto; place-items: center; border-radius: 9px; background: var(--app-ink); color: #fff; font-size: 9px; font-weight: 800; }
.speaking-turn strong { display: block; font-family: "Noto Sans SC", sans-serif; font-size: 13px; line-height: 1.5; }
.speaking-turn p { margin: 5px 0 0; color: var(--app-blue); font-family: "Noto Sans SC", sans-serif; font-size: 12px; line-height: 1.5; }
.speaking-turn small { display: block; margin-top: 5px; color: var(--app-muted); font-size: 10px; line-height: 1.45; }
.speaking-empty { margin: 0 22px 17px; padding: 28px 16px; border: 1px dashed var(--app-line); border-radius: 12px; color: var(--app-muted); font-size: 12px; text-align: center; }
.speaking-compose { padding: 0 22px 20px; }
.speaking-compose .page-textarea { min-height: 110px; }
.speaking-actions { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 10px; }
.speaking-side { align-self: start; overflow: hidden; }
.speaking-side__list { display: grid; gap: 1px; padding: 0 16px 16px; }
.speaking-side__list span { display: grid; gap: 4px; padding: 11px 6px; border-bottom: 1px solid rgba(33,29,30,.07); }
.speaking-side__list small { color: var(--app-muted); font-size: 10px; }
.speaking-side__list strong { font-size: 13px; }
.inline-message { margin: 0 22px 18px; color: #39714c; font-size: 11px; }
.inline-message--error { color: #a34444; }
@media (max-width: 900px) { .speaking-layout { grid-template-columns: 1fr; } }
@media (max-width: 520px) { .speaking-start { align-items: stretch; flex-direction: column; padding: 18px; } .speaking-start .page-button { align-self: flex-start; } .speaking-turns, .speaking-compose { padding-inline: 16px; } .speaking-empty { margin-inline: 16px; } .inline-message { margin-inline: 16px; } }
</style>
