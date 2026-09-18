<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getJson } from '../services/api'
import type { ReviewItem, ReviewSummary } from '../types'

const props = defineProps<{ mode: 'summary' | 'mistakes' | 'needs-review' }>()
const loading = ref(true)
const error = ref('')
const summary = ref<ReviewSummary | null>(null)
const items = ref<ReviewItem[]>([])

onMounted(async () => {
  try {
    if (props.mode === 'summary') summary.value = await getJson<ReviewSummary>('/api/review/summary')
    else items.value = await getJson<ReviewItem[]>(`/api/review/${props.mode}`)
  } catch {
    error.value = 'Chưa tải được hàng đợi ôn tập.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải dữ liệu ôn tập…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>
  <template v-else-if="mode === 'summary' && summary">
    <section class="page-card page-card--soft page-feature-intro"><p class="page-kicker">PHIÊN HÔM NAY</p><h1>Ôn đúng lúc</h1><p class="page-copy">Ôn những nội dung đang gần quên trước khi chuyển sang bài mới.</p></section>
    <div class="metric-grid"><article><span>Đến hạn</span><strong>{{ summary.dueCount }}</strong></article><article><span>Câu làm sai</span><strong>{{ summary.mistakeCount }}</strong></article><article><span>Cần ôn</span><strong>{{ summary.needsReviewCount }}</strong></article></div>
    <section class="page-card empty-page-card"><div class="empty-icon">✓</div><div><h2>{{ summary.dueCount ? 'Bắt đầu phiên ôn tập' : 'Hôm nay chưa có nội dung đến hạn' }}</h2><p>{{ summary.dueCount ? 'Mở nội dung đến hạn từ sidebar để tiếp tục.' : 'Bạn có thể quay lại luyện tập hoặc học bài tiếp theo.' }}</p></div></section>
  </template>
  <template v-else>
    <section class="page-card page-card--soft page-feature-intro"><p class="page-kicker">ÔN TẬP</p><h1>{{ mode === 'mistakes' ? 'Câu làm sai' : 'Nội dung cần ôn' }}</h1><p class="page-copy">Ôn đúng lúc, tập trung vào những nội dung cần củng cố nhất.</p></section>
    <section class="page-card list-card">
    <article v-for="item in items" :key="item.id" class="review-row"><div class="review-icon">{{ item.knowledgeType.slice(0, 1) }}</div><div><strong>{{ item.knowledgeId }}</strong><span>{{ item.reason }} · {{ item.mistakeCount }} lần</span></div><span class="path-status">{{ item.resolved ? 'Đã xử lý' : 'Ôn lại' }}</span></article>
    <div v-if="!items.length" class="empty-inline">Chưa có nội dung trong danh sách này.</div>
    </section>
  </template>
</template>
