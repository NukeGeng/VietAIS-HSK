<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getJson } from '../services/api'
import type { ProgressSnapshot, ProgressWeakPoint } from '../types'

const mode = defineProps<{ mode: 'overview' | 'weak-points' | 'history' | 'streak' }>()
const loading = ref(true)
const error = ref('')
const snapshot = ref<ProgressSnapshot | null>(null)
const weakPoints = ref<ProgressWeakPoint[]>([])
const history = ref<{ activityType: string; referenceId: string; occurredAt: string }[]>([])
const streak = ref<{ currentDays: number; qualifyingDays: string[] } | null>(null)

onMounted(async () => {
  try {
    if (mode.mode === 'overview') snapshot.value = await getJson<ProgressSnapshot>('/api/progress')
    if (mode.mode === 'weak-points') weakPoints.value = await getJson<ProgressWeakPoint[]>('/api/progress/weak-points')
    if (mode.mode === 'history') history.value = await getJson<typeof history.value>('/api/progress/history')
    if (mode.mode === 'streak') streak.value = await getJson<typeof streak.value>('/api/progress/streak')
  } catch { error.value = 'Chưa tải được tiến độ học.' } finally { loading.value = false }
})
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải tiến độ…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>
  <template v-else>
    <section class="page-card page-card--soft page-feature-intro"><p class="page-kicker">TIẾN ĐỘ</p><h1>{{ mode.mode === 'overview' ? 'Tiến độ học' : mode.mode === 'weak-points' ? 'Điểm yếu' : mode.mode === 'history' ? 'Lịch sử học' : 'Chuỗi ngày học' }}</h1><p class="page-copy">Chỉ giữ những tín hiệu giúp bạn biết bước tiếp theo.</p></section>
    <template v-if="mode.mode === 'overview' && snapshot"><div class="metric-grid"><article><span>Đã trả lời</span><strong>{{ snapshot.practiceAnswered }}</strong></article><article><span>Đúng</span><strong>{{ snapshot.practiceCorrect }}</strong></article><article><span>Cần ôn</span><strong>{{ snapshot.practiceIncorrect }}</strong></article></div></template>
    <section v-else-if="mode.mode === 'weak-points'" class="page-card list-card"><article v-for="item in weakPoints" :key="`${item.knowledgeType}-${item.knowledgeId}`" class="review-row"><div class="review-icon">!</div><div><strong>{{ item.knowledgeId }}</strong><span>{{ item.reason }} · {{ item.evidenceCount }} lần</span></div><span class="path-status">{{ item.action }}</span></article><div v-if="!weakPoints.length" class="empty-inline">Chưa có điểm yếu được ghi nhận.</div></section>
    <section v-else-if="mode.mode === 'history'" class="page-card list-card"><article v-for="entry in history" :key="`${entry.activityType}-${entry.referenceId}-${entry.occurredAt}`" class="review-row"><div class="review-icon">✓</div><div><strong>{{ entry.referenceId }}</strong><span>{{ entry.activityType }}</span></div><time>{{ new Date(entry.occurredAt).toLocaleDateString('vi-VN') }}</time></article><div v-if="!history.length" class="empty-inline">Chưa có hoạt động học.</div></section>
    <section v-else class="streak-card"><strong>{{ streak?.currentDays ?? 0 }}</strong><span>ngày học liên tiếp hiện tại</span><p>{{ streak?.qualifyingDays?.length ?? 0 }} ngày đã được ghi nhận trong lịch sử.</p></section>
  </template>
</template>
