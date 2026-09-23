<script setup lang="ts">
import { ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { getJson } from '../services/api'
import type { ProgressMastery, ProgressSnapshot, ProgressWeakPoint } from '../types'

const mode = defineProps<{ mode: 'overview' | 'weak-points' | 'history' | 'streak' }>()
const loading = ref(true)
const error = ref('')
const snapshot = ref<ProgressSnapshot | null>(null)
const mastery = ref<ProgressMastery[]>([])
const weakPoints = ref<ProgressWeakPoint[]>([])
const history = ref<{ activityType: string; referenceId: string; occurredAt: string }[]>([])
const streak = ref<{ currentDays: number; qualifyingDays: string[] } | null>(null)

const load = async () => {
  loading.value = true
  error.value = ''
  snapshot.value = null
  mastery.value = []
  weakPoints.value = []
  history.value = []
  streak.value = null

  try {
    if (mode.mode === 'overview') {
      const [nextSnapshot, nextMastery] = await Promise.all([
        getJson<ProgressSnapshot>('/api/progress'),
        getJson<ProgressMastery[]>('/api/progress/mastery'),
      ])
      snapshot.value = nextSnapshot
      mastery.value = nextMastery
    }
    if (mode.mode === 'weak-points') weakPoints.value = await getJson<ProgressWeakPoint[]>('/api/progress/weak-points')
    if (mode.mode === 'history') history.value = await getJson<typeof history.value>('/api/progress/history')
    if (mode.mode === 'streak') streak.value = await getJson<typeof streak.value>('/api/progress/streak')
  } catch { error.value = 'Chưa tải được tiến độ học.' } finally { loading.value = false }
}

watch(() => mode.mode, load, { immediate: true })

function weakPointTarget(item: ProgressWeakPoint) {
  const knowledgeId = item.knowledgeId.toLowerCase()
  const isQuestionFixture = knowledgeId.startsWith('bootstrap-')
  if (item.knowledgeType.toLowerCase() === 'hanzi-writing') {
    return `/app/hanzi/${encodeURIComponent(item.knowledgeId)}/write`
  }
  if (item.knowledgeType.toLowerCase() === 'vocabulary' && !isQuestionFixture) {
    return `/app/vocabulary/${encodeURIComponent(item.knowledgeId)}`
  }
  if (item.knowledgeType.toLowerCase() === 'grammar' && !isQuestionFixture) {
    return `/app/grammar/${encodeURIComponent(item.knowledgeId)}`
  }
  return '/app/needs-review'
}

function masteryLabel(type: string) {
  const labels: Record<string, string> = {
    vocabulary: 'Từ vựng',
    hanzi: 'Hán tự',
    'hanzi-writing': 'Luyện viết',
    grammar: 'Ngữ pháp',
    pinyin: 'Pinyin',
    tone: 'Thanh điệu',
  }
  return labels[type.toLowerCase()] ?? type
}

function masteryState(state: string) {
  return state === 'Strong' ? 'Nắm vững' : state === 'Learning' ? 'Đang học' : 'Cần ôn'
}
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải tiến độ…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>
  <template v-else>
    <template v-if="mode.mode === 'overview' && snapshot">
      <div class="metric-grid"><article><span>Đã trả lời</span><strong>{{ snapshot.practiceAnswered }}</strong></article><article><span>Đúng</span><strong>{{ snapshot.practiceCorrect }}</strong></article><article><span>Cần ôn</span><strong>{{ snapshot.practiceIncorrect }}</strong></article></div>
      <section class="page-card progress-mastery">
        <div class="page-section-title"><div><p class="app-eyebrow">MỨC ĐỘ NẮM VỮNG</p><h2>Tiến độ theo nội dung</h2><p>Kết quả được tính từ các lần luyện tập và ôn tập đã ghi nhận.</p></div></div>
        <div v-if="mastery.length" class="progress-mastery__grid">
          <article v-for="item in mastery" :key="`${item.knowledgeType}-${item.knowledgeId}`" class="progress-mastery__item">
            <div class="progress-mastery__top"><strong>{{ masteryLabel(item.knowledgeType) }}</strong><span>{{ item.scorePercent }}%</span></div>
            <div class="page-progress" role="progressbar" :aria-valuenow="item.scorePercent" aria-valuemin="0" aria-valuemax="100"><i :style="{ width: `${item.scorePercent}%` }"></i></div>
            <small>{{ item.knowledgeId }} · {{ item.correctCount }}/{{ item.attemptCount }} đúng · {{ masteryState(item.state) }}</small>
          </article>
        </div>
        <div v-else class="empty-inline">Chưa có kết quả đủ để tính mức độ nắm vững.</div>
      </section>
    </template>
    <section v-else-if="mode.mode === 'weak-points'" class="page-card list-card"><article v-for="item in weakPoints" :key="`${item.knowledgeType}-${item.knowledgeId}`" class="review-row"><div class="review-icon">!</div><div><strong>{{ item.knowledgeId }}</strong><span>{{ item.reason }} · {{ item.evidenceCount }} lần</span></div><RouterLink class="path-status text-link" :to="weakPointTarget(item)">{{ item.action }} →</RouterLink></article><div v-if="!weakPoints.length" class="empty-inline">Chưa có điểm yếu được ghi nhận.</div></section>
    <section v-else-if="mode.mode === 'history'" class="page-card list-card"><article v-for="entry in history" :key="`${entry.activityType}-${entry.referenceId}-${entry.occurredAt}`" class="review-row"><div class="review-icon">✓</div><div><strong>{{ entry.referenceId }}</strong><span>{{ entry.activityType }}</span></div><time>{{ new Date(entry.occurredAt).toLocaleDateString('vi-VN') }}</time></article><div v-if="!history.length" class="empty-inline">Chưa có hoạt động học.</div></section>
    <section v-else class="streak-card"><strong>{{ streak?.currentDays ?? 0 }}</strong><span>ngày học liên tiếp hiện tại</span><p>{{ streak?.qualifyingDays?.length ?? 0 }} ngày đã được ghi nhận trong lịch sử.</p></section>
  </template>
</template>
