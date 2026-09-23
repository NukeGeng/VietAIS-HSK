<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { getJson } from '../services/api'
import type { VocabularyEntry } from '../types'

const route = useRoute()
const entries = ref<VocabularyEntry[]>([])
const selected = ref<VocabularyEntry | null>(null)
const search = ref('')
const hsk = ref('Tất cả')
const topic = ref('Tất cả')
const loading = ref(true)
const error = ref('')
const audioError = ref('')

const topics = computed(() => ['Tất cả', ...new Set(entries.value.map(item => item.topic).sort((a, b) => a.localeCompare(b, 'vi')))])
const levels = computed(() => [...new Set(entries.value.map(item => item.hskLevel))]
  .sort((a, b) => a.localeCompare(b, 'vi', { numeric: true })))
const requestedId = computed(() => typeof route.params.id === 'string' ? route.params.id : null)
const filtered = computed(() => entries.value.filter(item => {
  const query = search.value.trim().toLocaleLowerCase('vi')
  const matchesQuery = !query || [item.simplified, item.pinyin, item.meaning, item.topic].some(value => value.toLocaleLowerCase('vi').includes(query))
  return matchesQuery
    && (hsk.value === 'Tất cả' || item.hskLevel === hsk.value)
    && (topic.value === 'Tất cả' || item.topic === topic.value)
}))

function speak(text: string) {
  audioError.value = ''
  if (typeof window.speechSynthesis === 'undefined' || typeof SpeechSynthesisUtterance === 'undefined') {
    audioError.value = 'Trình duyệt chưa hỗ trợ phát âm thanh.'
    return
  }

  try {
    window.speechSynthesis.cancel()
    const utterance = new SpeechSynthesisUtterance(text)
    utterance.lang = 'zh-CN'
    utterance.rate = 0.82
    window.speechSynthesis.speak(utterance)
  } catch {
    audioError.value = 'Không phát được âm thanh trên trình duyệt này.'
  }
}

function syncSelected() {
  selected.value = requestedId.value
    ? entries.value.find(item => item.id === requestedId.value || item.simplified === requestedId.value) ?? null
    : null
  audioError.value = ''
}

watch(() => route.params.id, syncSelected)

onMounted(async () => {
  try {
    entries.value = await getJson<VocabularyEntry[]>('/api/vocabulary')
    syncSelected()
  } catch {
    error.value = 'Chưa tải được danh sách từ vựng.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải từ vựng…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>
  <section v-else-if="selected" class="page-card knowledge-detail-card">
    <RouterLink class="page-back" to="/app/vocabulary">← Danh sách từ</RouterLink>
    <p v-if="audioError" class="knowledge-audio-error" role="alert">{{ audioError }}</p>
    <div class="knowledge-detail-top"><div class="knowledge-glyph">{{ selected.simplified }}</div><div><p class="page-kicker">TỪ VỰNG · {{ selected.hskLevel }}</p><h1>{{ selected.pinyin }}</h1><p class="knowledge-muted">{{ selected.meaning }} · {{ selected.partOfSpeech }} · {{ selected.topic }}</p></div></div>
    <button class="knowledge-audio" type="button" @click="speak(selected.simplified)">🔊 Nghe phát âm</button>
    <div class="knowledge-block"><h2>Ví dụ</h2><article v-for="example in selected.examples" :key="example.chinese" class="knowledge-example"><strong>{{ example.chinese }}</strong><span>{{ example.pinyin }}</span><small>{{ example.vietnamese }}</small><button class="knowledge-example__audio" type="button" @click="speak(example.chinese)">Nghe câu</button></article></div>
    <div class="knowledge-block"><h2>Chữ cấu tạo</h2><div class="page-chips"><span v-for="hanzi in selected.relatedHanzi" :key="hanzi" class="page-chip">{{ hanzi }}</span></div></div>
    <p class="knowledge-provenance">Nguồn: {{ selected.sourceType }} · {{ selected.sourceVersion }} · {{ selected.licenseRef }}</p>
  </section>
  <section v-else-if="requestedId" class="page-card knowledge-not-found">
    <RouterLink class="page-back" to="/app/vocabulary">← Danh sách từ</RouterLink>
    <p>Không tìm thấy từ vựng này.</p>
  </section>
  <section v-else class="page-canvas knowledge-page">
    <div class="page-inline-heading"><div><p class="page-kicker">NỀN TẢNG</p><h1>Từ vựng</h1></div><span class="knowledge-count">{{ filtered.length }} từ</span></div>
    <p v-if="audioError" class="knowledge-audio-error" role="alert">{{ audioError }}</p>
    <div class="page-filter-bar"><input v-model="search" class="page-search" type="search" placeholder="Tìm chữ, Pinyin hoặc nghĩa" aria-label="Tìm từ vựng"><select v-model="hsk" class="page-select" aria-label="Chọn cấp độ"><option>Tất cả</option><option v-for="level in levels" :key="level">{{ level }}</option></select><select v-model="topic" class="page-select" aria-label="Chọn chủ đề"><option v-for="item in topics" :key="item">{{ item }}</option></select></div>
    <section class="page-card"><div class="knowledge-grid"><article v-for="item in filtered" :key="item.id" class="knowledge-card"><RouterLink class="knowledge-card__link" :to="`/app/vocabulary/${item.id}`"><strong>{{ item.simplified }}</strong><span>{{ item.pinyin }}</span><p>{{ item.meaning }}</p><small>{{ item.hskLevel }} · {{ item.topic }}</small></RouterLink><button class="knowledge-card__audio" type="button" :aria-label="`Nghe ${item.simplified}`" @click="speak(item.simplified)">🔊</button></article><p v-if="!filtered.length" class="empty-inline">Không có từ phù hợp.</p></div></section>
  </section>
</template>

<style scoped>
.knowledge-page { gap: 14px; }
.knowledge-count { color: var(--app-muted); font-size: 11px; font-weight: 800; }
.knowledge-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 10px; padding: 18px; }
.knowledge-card { min-width: 0; padding: 15px; border: 1px solid var(--app-line); border-radius: 13px; background: #fff; }
.knowledge-card:hover { border-color: rgba(47,109,229,.35); box-shadow: 0 10px 22px rgba(33,29,30,.08); transform: translateY(-1px); }
.knowledge-card__link { display: block; }
.knowledge-card__audio, .knowledge-audio, .knowledge-example__audio { border: 1px solid var(--app-line); border-radius: 999px; background: #fff; color: var(--app-blue); cursor: pointer; font-size: 11px; font-weight: 800; }
.knowledge-card__audio { margin-top: 11px; padding: 5px 9px; }
.knowledge-card__audio:hover, .knowledge-audio:hover, .knowledge-example__audio:hover { border-color: var(--app-blue); background: var(--app-blue-soft); }
.knowledge-card strong, .knowledge-glyph { font-family: "Noto Sans SC", sans-serif; }
.knowledge-card strong { display: block; color: var(--app-ink); font-size: 28px; line-height: 1; }
.knowledge-card span { display: block; margin-top: 6px; color: var(--app-blue); font-size: 11px; font-weight: 800; }
.knowledge-card p { min-height: 30px; margin: 8px 0 0; color: var(--app-ink); font-size: 12px; }
.knowledge-card small, .knowledge-muted, .knowledge-provenance { color: var(--app-muted); font-size: 10px; }
.knowledge-detail-card { padding: 22px; }
.knowledge-detail-top { display: flex; align-items: center; gap: 18px; }
.knowledge-glyph { display: grid; width: 120px; height: 120px; place-items: center; border: 1px solid var(--app-line); border-radius: 18px; background: #fafaf8; font-size: 56px; font-weight: 700; letter-spacing: -.12em; white-space: nowrap; }
.knowledge-detail-top h1 { margin: 5px 0 0; font-size: 26px; letter-spacing: -.05em; }
.knowledge-muted { margin: 6px 0 0; font-size: 12px; }
.knowledge-audio { margin-top: 18px; padding: 9px 13px; }
.knowledge-block { margin-top: 24px; padding-top: 18px; border-top: 1px solid var(--app-line); }
.knowledge-block h2 { margin: 0 0 12px; font-size: 17px; }
.knowledge-example { display: grid; gap: 4px; padding: 14px; border: 1px solid var(--app-line); border-radius: 12px; background: #fafaf8; }
.knowledge-example strong { font-family: "Noto Sans SC", sans-serif; font-size: 22px; }
.knowledge-example span { color: var(--app-blue); font-size: 11px; font-weight: 800; }
.knowledge-example small { color: var(--app-muted); font-size: 12px; }
.knowledge-example__audio { justify-self: start; padding: 5px 9px; }
.knowledge-provenance { margin: 18px 0 0; line-height: 1.5; }
.knowledge-not-found { padding: 22px; }
.knowledge-not-found p { margin: 16px 0 0; color: var(--app-muted); font-size: 13px; }
.knowledge-audio-error { margin: 14px 0; color: var(--app-muted); font-size: 12px; }
@media (max-width: 900px) { .knowledge-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); } }
@media (max-width: 560px) { .knowledge-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); padding: 14px; } .knowledge-detail-card { padding: 16px; } .knowledge-glyph { width: 92px; height: 92px; font-size: 40px; } .knowledge-detail-top { align-items: flex-start; gap: 12px; } }
</style>
