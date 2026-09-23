<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { getJson } from '../services/api'
import type { GrammarPoint } from '../types'

const route = useRoute()
const entries = ref<GrammarPoint[]>([])
const selected = ref<GrammarPoint | null>(null)
const search = ref('')
const hsk = ref('Tất cả')
const topic = ref('Tất cả')
const loading = ref(true)
const error = ref('')

const topics = computed(() => ['Tất cả', ...new Set(entries.value.map(item => item.topic).sort((a, b) => a.localeCompare(b, 'vi')))])
const filtered = computed(() => entries.value.filter(item => {
  const query = search.value.trim().toLocaleLowerCase('vi')
  const matchesQuery = !query || [item.pattern, item.title, item.explanation, item.topic].some(value => value.toLocaleLowerCase('vi').includes(query))
  return matchesQuery
    && (hsk.value === 'Tất cả' || item.hskLevel === hsk.value)
    && (topic.value === 'Tất cả' || item.topic === topic.value)
}))

function syncSelected() {
  const id = route.params.id
  selected.value = typeof id === 'string'
    ? entries.value.find(item => item.id === id || item.pattern === id) ?? null
    : null
}

watch(() => route.params.id, syncSelected)

onMounted(async () => {
  try {
    entries.value = await getJson<GrammarPoint[]>('/api/grammar')
    syncSelected()
  } catch {
    error.value = 'Chưa tải được danh sách ngữ pháp.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải ngữ pháp…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>
  <section v-else-if="selected" class="page-card knowledge-detail-card">
    <RouterLink class="page-back" to="/app/grammar">← Danh sách ngữ pháp</RouterLink>
    <p class="page-kicker">NGỮ PHÁP · {{ selected.hskLevel }}</p><h1 class="grammar-pattern">{{ selected.pattern }}</h1><p class="knowledge-muted">{{ selected.title }} · {{ selected.topic }}</p>
    <div class="knowledge-block"><h2>Cách dùng</h2><p class="grammar-explanation">{{ selected.explanation }}</p></div>
    <div class="knowledge-block"><h2>Ví dụ</h2><article v-for="example in selected.examples" :key="example.chinese" class="knowledge-example"><strong>{{ example.chinese }}</strong><span>{{ example.pinyin }}</span><small>{{ example.vietnamese }}</small></article></div>
    <div class="knowledge-block"><h2>Lỗi thường gặp</h2><ul class="grammar-mistakes"><li v-for="mistake in selected.commonMistakes" :key="mistake">{{ mistake }}</li></ul></div>
    <RouterLink v-if="selected.relatedLessonId" class="page-button page-button--blue" :to="`/app/lessons/${encodeURIComponent(selected.relatedLessonId)}`">Xem bài học liên quan →</RouterLink>
    <RouterLink v-else class="page-button page-button--blue" to="/app/practice/grammar">Luyện tập cấu trúc →</RouterLink>
    <p class="knowledge-provenance">Nguồn: {{ selected.sourceType }} · {{ selected.sourceVersion }} · {{ selected.licenseRef }}</p>
  </section>
  <section v-else class="page-canvas knowledge-page">
    <div class="page-inline-heading"><div><p class="page-kicker">NỀN TẢNG</p><h1>Ngữ pháp</h1></div><span class="knowledge-count">{{ filtered.length }} cấu trúc</span></div>
    <div class="page-filter-bar"><input v-model="search" class="page-search" type="search" placeholder="Tìm cấu trúc hoặc ví dụ" aria-label="Tìm ngữ pháp"><select v-model="hsk" class="page-select" aria-label="Chọn cấp độ"><option>Tất cả</option><option>HSK 3</option></select><select v-model="topic" class="page-select" aria-label="Chọn chủ đề"><option v-for="item in topics" :key="item">{{ item }}</option></select></div>
    <section class="page-card"><div class="page-list"><RouterLink v-for="item in filtered" :key="item.id" class="page-list-row" :to="`/app/grammar/${item.id}`"><span class="page-list-row__icon">语</span><span class="page-list-row__body"><strong>{{ item.pattern }}</strong><small>{{ item.title }} · {{ item.hskLevel }} · {{ item.topic }}</small></span><span class="page-list-row__meta">→</span></RouterLink><p v-if="!filtered.length" class="empty-inline">Không có cấu trúc phù hợp.</p></div></section>
  </section>
</template>

<style scoped>
.knowledge-page { gap: 14px; }
.knowledge-count { color: var(--app-muted); font-size: 11px; font-weight: 800; }
.knowledge-detail-card { padding: 22px; }
.grammar-pattern { margin: 5px 0 0; color: var(--app-ink); font-size: clamp(28px, 3vw, 38px); letter-spacing: -.06em; }
.knowledge-muted, .knowledge-provenance { color: var(--app-muted); font-size: 12px; }
.knowledge-muted { margin: 6px 0 0; }
.knowledge-block { margin-top: 24px; padding-top: 18px; border-top: 1px solid var(--app-line); }
.knowledge-block h2 { margin: 0 0 12px; font-size: 17px; }
.grammar-explanation { max-width: 720px; margin: 0; color: #585452; font-size: 13px; line-height: 1.7; }
.knowledge-example { display: grid; gap: 4px; padding: 14px; border: 1px solid var(--app-line); border-radius: 12px; background: #fafaf8; }
.knowledge-example strong { font-family: "Noto Sans SC", sans-serif; font-size: 22px; }
.knowledge-example span { color: var(--app-blue); font-size: 11px; font-weight: 800; }
.knowledge-example small { color: var(--app-muted); font-size: 12px; }
.grammar-mistakes { display: grid; gap: 8px; margin: 0; padding-left: 18px; color: #585452; font-size: 12px; line-height: 1.5; }
.knowledge-detail-card > .page-button { margin-top: 22px; }
.knowledge-provenance { margin: 18px 0 0; font-size: 10px; line-height: 1.5; }
</style>
