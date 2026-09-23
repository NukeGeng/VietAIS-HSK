<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { getJson, postJson } from '../services/api'
import type { ReviewItem, ReviewSession, ReviewSummary } from '../types'

const props = defineProps<{ mode: 'summary' | 'mistakes' | 'needs-review' }>()
const loading = ref(true)
const working = ref(false)
const error = ref('')
const actionMessage = ref('')
const summary = ref<ReviewSummary | null>(null)
const items = ref<ReviewItem[]>([])
const session = ref<ReviewSession | null>(null)
const sessionItems = ref<ReviewItem[]>([])
const currentIndex = ref(0)
const typeFilter = ref('Tất cả')

const currentItem = computed(() => sessionItems.value[currentIndex.value] ?? null)
const sessionDone = computed(() => Boolean(session.value && currentIndex.value >= sessionItems.value.length))
const modeTitle = computed(() => props.mode === 'mistakes' ? 'Câu làm sai' : 'Nội dung cần ôn')
const typeOptions = computed(() => ['Tất cả', ...new Set(items.value.map(item => item.knowledgeType))])
const filteredItems = computed(() => {
  if (props.mode !== 'needs-review' || typeFilter.value === 'Tất cả') return items.value
  return items.value.filter(item => item.knowledgeType === typeFilter.value)
})
const reasonLabels: Record<string, string> = {
  WrongAnswer: 'Làm sai câu hỏi',
  RepeatedMistake: 'Sai lặp lại',
  WritingWeak: 'Nét chữ cần luyện thêm',
  LowMastery: 'Mức độ ghi nhớ còn thấp',
  ScheduledReview: 'Đến lịch ôn',
}

const load = async () => {
  loading.value = true
  error.value = ''
  actionMessage.value = ''
  summary.value = null
  items.value = []
  session.value = null
  sessionItems.value = []
  currentIndex.value = 0
  typeFilter.value = 'Tất cả'

  try {
    if (props.mode === 'summary') {
      const [nextSummary, dueItems] = await Promise.all([
        getJson<ReviewSummary>('/api/review/summary'),
        getJson<ReviewItem[]>('/api/review/needs-review'),
      ])
      summary.value = nextSummary
      items.value = dueItems
    } else {
      items.value = await getJson<ReviewItem[]>(`/api/review/${props.mode}`)
    }
  } catch {
    error.value = 'Chưa tải được hàng đợi ôn tập.'
  } finally {
    loading.value = false
  }
}

watch(() => props.mode, load, { immediate: true })

async function startSession() {
  if (!filteredItems.value.length) return
  working.value = true
  actionMessage.value = ''
  try {
    session.value = await postJson<ReviewSession>('/api/review/sessions', {
      itemIds: filteredItems.value.map(item => item.id),
    })
    sessionItems.value = [...filteredItems.value]
    currentIndex.value = 0
  } catch {
    actionMessage.value = 'Không thể bắt đầu phiên ôn lúc này.'
  } finally {
    working.value = false
  }
}

async function recordResult(correct: boolean) {
  if (!session.value || !currentItem.value) return
  working.value = true
  actionMessage.value = ''
  try {
    await postJson<ReviewItem>(`/api/review/sessions/${session.value.id}/results`, {
      itemId: currentItem.value.id,
      correct,
    })
    currentIndex.value += 1
    actionMessage.value = correct ? 'Đã ghi nhận kết quả đúng.' : 'Đã giữ mục này trong hàng đợi ôn lại.'
  } catch {
    actionMessage.value = 'Không thể ghi nhận kết quả. Hãy thử lại.'
  } finally {
    working.value = false
  }
}

function closeSession() {
  session.value = null
  sessionItems.value = []
  currentIndex.value = 0
}

function reasonLabel(reason: string) {
  return reasonLabels[reason] ?? reason
}

function itemTarget(item: ReviewItem) {
  const type = item.knowledgeType.toLocaleLowerCase('en')
  const id = encodeURIComponent(item.knowledgeId)
  if (type.includes('hanzi') && type.includes('writing')) return `/app/hanzi/${id}/write`
  if (type.includes('hanzi')) return `/app/hanzi/${id}`
  if (type.includes('vocab')) return item.knowledgeId.startsWith('bootstrap-')
    ? '/app/practice/vocabulary'
    : `/app/vocabulary/${id}`
  if (type.includes('grammar')) return `/app/grammar/${id}`
  return null
}
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải dữ liệu ôn tập…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>
  <template v-else>
    <p v-if="actionMessage" class="inline-message">{{ actionMessage }}</p>

    <section v-if="session" class="page-card page-focus-card review-session-card">
      <div class="page-focus-card__top">
        <div>
          <small>PHIÊN ÔN TẬP</small>
          <strong>{{ sessionDone ? 'Đã hoàn thành phiên ôn' : `Mục ${currentIndex + 1} / ${sessionItems.length}` }}</strong>
        </div>
        <button class="page-button page-button--quiet" type="button" @click="closeSession">Đóng phiên</button>
      </div>
      <template v-if="currentItem">
        <div class="page-question">
          <span class="page-question__label">{{ currentItem.knowledgeType }}</span>
          <h2>{{ currentItem.knowledgeId }}</h2>
          <p>{{ reasonLabel(currentItem.reason) }} · {{ currentItem.mistakeCount }} lần</p>
        </div>
        <div class="page-focus-actions review-session-actions">
          <button class="page-button page-button--quiet" type="button" :disabled="working" @click="recordResult(false)">Cần ôn lại</button>
          <button class="page-button page-button--blue" type="button" :disabled="working" @click="recordResult(true)">Đã nhớ →</button>
        </div>
      </template>
      <div v-else class="empty-page-card review-session-complete"><div class="empty-icon">✓</div><div><h2>Phiên ôn đã xong</h2><p>Kết quả đã được lưu và lịch ôn tiếp theo đã được cập nhật.</p></div></div>
    </section>

    <template v-else-if="mode === 'summary' && summary">
      <div class="metric-grid"><article><span>Đến hạn</span><strong>{{ summary.dueCount }}</strong></article><article><span>Câu làm sai</span><strong>{{ summary.mistakeCount }}</strong></article><article><span>Cần ôn</span><strong>{{ summary.needsReviewCount }}</strong></article></div>
      <section class="page-card review-start-card">
        <div><p class="app-eyebrow">ÔN TẬP HÔM NAY</p><h2>{{ summary.dueCount ? 'Bắt đầu phiên ôn tập' : 'Hôm nay chưa có nội dung đến hạn' }}</h2><p>{{ summary.dueCount ? `${items.length} mục đang chờ bạn ôn lại.` : 'Bạn có thể quay lại luyện tập hoặc học bài tiếp theo.' }}</p></div>
        <button v-if="items.length" class="page-button page-button--blue" type="button" :disabled="working" @click="startSession">Bắt đầu ôn →</button>
      </section>
    </template>

    <section v-else class="page-card list-card">
      <div class="page-section-title"><div><p class="app-eyebrow">{{ mode === 'mistakes' ? 'MISTAKES' : 'NEEDS REVIEW' }}</p><h2>{{ modeTitle }}</h2><p>{{ filteredItems.length ? `${filteredItems.length} mục trong danh sách.` : 'Chưa có nội dung trong danh sách này.' }}</p></div><div class="page-section-title__actions"><select v-if="mode === 'needs-review' && items.length" v-model="typeFilter" class="page-select" aria-label="Lọc loại nội dung"><option v-for="option in typeOptions" :key="option">{{ option }}</option></select><button v-if="filteredItems.length" class="page-button page-button--blue" type="button" :disabled="working" @click="startSession">Ôn lại →</button></div></div>
      <article v-for="item in filteredItems" :key="item.id" class="review-row"><div class="review-icon">{{ item.knowledgeType.slice(0, 1) }}</div><div><strong>{{ item.knowledgeId }}</strong><span>{{ reasonLabel(item.reason) }} · {{ item.mistakeCount }} lần</span></div><RouterLink v-if="itemTarget(item)" class="path-status review-row__link" :to="itemTarget(item)!">Mở nội dung →</RouterLink><span v-else class="path-status">{{ item.resolved ? 'Đã xử lý' : 'Ôn lại' }}</span></article>
      <div v-if="!filteredItems.length" class="empty-inline">{{ items.length ? 'Không có mục phù hợp với bộ lọc.' : 'Chưa có nội dung trong danh sách này.' }}</div>
    </section>
  </template>
</template>
