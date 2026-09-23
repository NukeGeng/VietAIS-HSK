<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { getJson, postJson } from '../services/api'
import type { BeginnerLearning, FoundationCatalog, FoundationItem } from '../types'

const props = defineProps<{ mode: 'pinyin' | 'tones' }>()
const catalog = ref<FoundationCatalog | null>(null)
const learning = ref<BeginnerLearning | null>(null)
const loading = ref(true)
const error = ref('')
const search = ref('')
const actionMessage = ref('')
const actionBusy = ref(false)

const isPinyin = computed(() => props.mode === 'pinyin')
const endpoint = computed(() => isPinyin.value ? '/api/foundation/pinyin' : '/api/foundation/tones')

watch(endpoint, async (path) => {
  loading.value = true
  error.value = ''
  catalog.value = null
  learning.value = null
  search.value = ''
  actionMessage.value = ''
  try {
    catalog.value = await getJson<FoundationCatalog>(path)
  } catch {
    error.value = 'Chưa tải được dữ liệu nền tảng. Vui lòng thử tải lại trang.'
  }
  try { learning.value = await getJson<BeginnerLearning>('/api/learning/beginner') } catch { /* public foundation data can render without a learner context */ }
  loading.value = false
}, { immediate: true })

const pinyinGroups = computed(() => {
  const items = catalog.value?.items ?? []
  const query = search.value.trim().toLocaleLowerCase('vi')
  const visible = query
    ? items.filter(item => [item.label, item.category, item.description, item.pinyin, item.example, item.exampleMeaning]
      .some(value => value?.toLocaleLowerCase('vi').includes(query)))
    : items
  const groups = new Map<string, FoundationItem[]>()
  for (const item of visible) {
    const group = groups.get(item.category) ?? []
    group.push(item)
    groups.set(item.category, group)
  }
  return [...groups.entries()].map(([category, entries]) => ({ category, items: entries }))
})

const toneItems = computed(() => (catalog.value?.items ?? []).filter(item => item.category === 'Thanh điệu'))
const toneRule = computed(() => (catalog.value?.items ?? []).find(item => item.category === 'Quy tắc'))
const stageId = computed(() => isPinyin.value ? 'pinyin' : 'tones')
const stageStarted = computed(() => learning.value?.state.startedBeginnerStageIds.includes(stageId.value) ?? false)
const stageCompleted = computed(() => learning.value?.state.completedBeginnerStageIds.includes(stageId.value) ?? false)
const stageActionLabel = computed(() => stageCompleted.value ? 'Đã hoàn tất' : stageStarted.value ? 'Hoàn tất bước này' : 'Bắt đầu bước này')
const tonePath = (tone: number | null) => ({
  0: 'M 4 15 C 22 16, 42 17, 68 19',
  1: 'M 4 15 C 22 14, 48 14, 68 14',
  2: 'M 4 27 C 23 25, 46 10, 68 5',
  3: 'M 4 7 C 19 27, 33 30, 42 27 S 57 12, 68 7',
  4: 'M 4 5 C 24 7, 47 22, 68 28',
}[tone ?? 0] ?? 'M 4 15 L 68 15')

async function advanceStage() {
  if (stageCompleted.value || actionBusy.value) return
  actionBusy.value = true
  actionMessage.value = ''
  const wasStarted = stageStarted.value
  try {
    const state = await postJson<BeginnerLearning['state']>(
      `/api/learning/beginner/stages/${stageId.value}/${wasStarted ? 'complete' : 'start'}`,
    )
    if (learning.value) learning.value = { ...learning.value, state }
    actionMessage.value = wasStarted ? 'Đã hoàn tất bước học.' : 'Đã bắt đầu bước học.'
  } catch {
    actionMessage.value = 'Chưa thể cập nhật bước học. Hãy thử lại sau.'
  } finally {
    actionBusy.value = false
  }
}
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải dữ liệu nền tảng…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>
  <template v-else-if="catalog">
    <div class="foundation-context">
      <nav class="foundation-switch" aria-label="Chuyển nội dung nền tảng">
        <router-link to="/app/pinyin" :class="{ 'is-current': isPinyin }" :aria-current="isPinyin ? 'page' : undefined">Pinyin</router-link>
        <router-link to="/app/tones" :class="{ 'is-current': !isPinyin }" :aria-current="!isPinyin ? 'page' : undefined">Thanh điệu</router-link>
      </nav>
      <RouterLink v-if="isPinyin" class="page-button page-button--blue foundation-practice-link" to="/app/practice/pinyin">Luyện Pinyin →</RouterLink>
      <div class="foundation-stage-action">
        <button class="page-button page-button--blue" type="button" :disabled="stageCompleted || actionBusy" @click="advanceStage">{{ stageActionLabel }}</button>
        <small v-if="actionMessage" class="inline-message">{{ actionMessage }}</small>
      </div>
      <div class="foundation-summary" aria-label="Tóm tắt nội dung">
        <template v-if="isPinyin">
          <span><strong>21</strong><small>âm đầu</small></span>
          <i aria-hidden="true" />
          <span><strong>35</strong><small>vần</small></span>
        </template>
        <template v-else>
          <span><strong>4</strong><small>thanh chính</small></span>
          <i aria-hidden="true" />
          <span><strong>1</strong><small>thanh nhẹ</small></span>
        </template>
      </div>
    </div>

    <section v-if="isPinyin" id="foundation-catalog" class="page-card foundation-panel">
      <div class="page-section-title foundation-panel__heading">
        <div><h2>Bảng âm đầu và vần</h2><p>Chọn một âm tiết mẫu để đọc thành tiếng.</p></div>
        <span>{{ catalog.items.length }} mục</span>
      </div>
      <div class="foundation-search-wrap">
        <label class="foundation-search-label" for="foundation-search">Tìm trong bảng âm</label>
        <input id="foundation-search" v-model="search" class="page-search foundation-search" type="search" placeholder="Ví dụ: zh, vần mũi, tiếng ‘mẹ’" />
      </div>
      <div v-if="pinyinGroups.length" class="foundation-groups">
        <section v-for="group in pinyinGroups" :key="group.category" class="foundation-group">
          <div class="foundation-group__heading"><h3>{{ group.category }}</h3><span>{{ group.items.length }}</span></div>
          <div class="foundation-grid">
            <article v-for="item in group.items" :key="item.id" class="foundation-card">
              <div class="foundation-card__top"><strong class="foundation-card__symbol">{{ item.label }}</strong><span v-if="item.tone" class="foundation-card__tone">Thanh {{ item.tone }}</span></div>
              <p>{{ item.description }}</p>
              <div v-if="item.example" class="foundation-example">
                <span class="foundation-example__hanzi" lang="zh-Hans">{{ item.example }}</span>
                <span><strong>{{ item.pinyin }}</strong><small>{{ item.exampleMeaning }}</small></span>
              </div>
            </article>
          </div>
        </section>
      </div>
      <div v-else class="foundation-empty">Không tìm thấy âm hoặc vần phù hợp.</div>
    </section>

    <section v-else id="foundation-catalog" class="page-card foundation-panel">
      <div class="page-section-title foundation-panel__heading">
        <div><h2>Bốn thanh và thanh nhẹ</h2><p>Cùng một âm tiết, đổi thanh có thể đổi nghĩa.</p></div>
        <span>mā · má · mǎ · mà · ma</span>
      </div>
      <div class="tone-grid">
        <article v-for="item in toneItems" :key="item.id" class="tone-card" :class="`tone-card--${item.tone === 0 ? 'neutral' : item.tone}`">
          <div class="tone-card__top">
            <span class="tone-card__number">{{ item.tone === 0 ? '·' : item.tone }}</span>
            <svg class="tone-contour" viewBox="0 0 72 36" role="img" :aria-label="`Đường nét minh họa ${item.label}`">
              <path :d="tonePath(item.tone)" />
            </svg>
          </div>
          <div class="tone-card__example">
            <strong>{{ item.pinyin }}</strong>
            <span lang="zh-Hans">{{ item.example }}</span>
          </div>
          <h3>{{ item.label }}</h3>
          <p>{{ item.description }}</p>
          <small>{{ item.exampleMeaning }}</small>
        </article>
      </div>
      <article v-if="toneRule" class="tone-rule">
        <span class="tone-rule__mark" lang="zh-Hans">你好</span>
        <div><p class="page-kicker">QUY TẮC KHI NÓI LIỀN</p><h3>{{ toneRule.label }}</h3><p>{{ toneRule.description }}</p></div>
      </article>
    </section>

    <aside class="foundation-provenance">
      <span class="foundation-provenance__dot" aria-hidden="true">i</span>
      <div><strong>Nguồn và phạm vi</strong><p>{{ catalog.editorialNote }}</p>
        <div class="foundation-provenance__links">
          <a v-for="source in catalog.sources" :key="source.url" :href="source.url" target="_blank" rel="noreferrer">{{ source.name }} <span aria-hidden="true">↗</span></a>
        </div>
      </div>
    </aside>
  </template>
</template>

<style scoped>
.foundation-context { display: flex; align-items: center; justify-content: space-between; gap: 12px 20px; margin-bottom: 2px; }
.foundation-practice-link { margin-left: auto; white-space: nowrap; }
.foundation-stage-action { display: grid; gap: 5px; margin-left: auto; }
.foundation-stage-action .page-button { white-space: nowrap; }
.foundation-stage-action .inline-message { margin: 0; font-size: 10px; white-space: nowrap; }
.foundation-summary { display: flex; align-items: center; gap: 17px; padding: 13px 16px; border: 1px solid var(--app-line); border-radius: 14px; background: rgba(255,255,255,.76); }
.foundation-summary span { display: grid; gap: 2px; text-align: center; }
.foundation-summary strong { color: var(--app-blue); font-size: 21px; line-height: 1; letter-spacing: -.05em; }
.foundation-summary small { color: var(--app-muted); font-size: 10px; font-weight: 700; white-space: nowrap; }
.foundation-summary i { width: 1px; height: 27px; background: var(--app-line); }
.foundation-switch { display: flex; flex-wrap: wrap; gap: 7px; }
.foundation-switch a { padding: 7px 11px; border: 1px solid var(--app-line); border-radius: 999px; background: #fff; color: var(--app-muted); font-size: 11px; font-weight: 800; }
.foundation-switch a.is-current { border-color: rgba(47,109,229,.25); background: var(--app-blue-soft); color: var(--app-blue); }
.foundation-panel { overflow: hidden; }
.foundation-panel__heading { padding-bottom: 10px; }
.foundation-search-wrap { display: grid; grid-template-columns: auto minmax(200px, 360px); align-items: center; gap: 12px; padding: 0 22px 15px; }
.foundation-search-label { color: var(--app-muted); font-size: 11px; font-weight: 800; }
.foundation-search { min-height: 38px; }
.foundation-groups { display: grid; gap: 20px; padding: 4px 22px 22px; }
.foundation-group__heading { display: flex; align-items: center; gap: 8px; margin-bottom: 10px; }
.foundation-group__heading h3 { color: var(--app-ink); font-size: 14px; letter-spacing: -.02em; }
.foundation-group__heading span { display: grid; min-width: 22px; height: 22px; place-items: center; border-radius: 999px; background: var(--app-soft); color: var(--app-muted); font-size: 10px; font-weight: 800; }
.foundation-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 9px; }
.foundation-card { min-width: 0; padding: 13px; border: 1px solid var(--app-line); border-radius: 12px; background: #fff; }
.foundation-card__top { display: flex; align-items: center; justify-content: space-between; gap: 7px; }
.foundation-card__symbol { color: var(--app-blue); font-size: 21px; line-height: 1; letter-spacing: -.04em; }
.foundation-card__tone { padding: 4px 6px; border-radius: 999px; background: var(--app-soft); color: var(--app-muted); font-size: 9px; font-weight: 800; white-space: nowrap; }
.foundation-card p { min-height: 32px; margin: 8px 0 0; color: var(--app-muted); font-size: 10px; line-height: 1.5; }
.foundation-example { display: flex; align-items: center; gap: 8px; margin-top: 10px; padding-top: 9px; border-top: 1px solid rgba(33,29,30,.07); }
.foundation-example__hanzi { color: var(--app-ink); font-family: "Noto Sans SC", sans-serif; font-size: 22px; line-height: 1; }
.foundation-example > span:last-child { display: grid; min-width: 0; gap: 2px; }
.foundation-example strong { color: var(--app-blue); font-size: 10px; }
.foundation-example small { overflow: hidden; color: var(--app-muted); font-size: 9px; text-overflow: ellipsis; white-space: nowrap; }
.foundation-empty { padding: 28px 20px; color: var(--app-muted); font-size: 12px; text-align: center; }
.tone-grid { display: grid; grid-template-columns: repeat(5, minmax(0, 1fr)); gap: 10px; padding: 0 22px 20px; }
.tone-card { min-width: 0; padding: 15px; border: 1px solid var(--app-line); border-radius: 14px; background: #fff; }
.tone-card__top { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.tone-card__number { display: grid; width: 28px; height: 28px; place-items: center; border-radius: 9px; background: var(--app-blue-soft); color: var(--app-blue); font-size: 13px; font-weight: 900; }
.tone-card--neutral .tone-card__number { background: var(--app-soft); color: var(--app-muted); }
.tone-contour { width: 68px; height: 36px; overflow: visible; }
.tone-contour path { fill: none; stroke: var(--app-blue); stroke-linecap: round; stroke-width: 3; }
.tone-card--neutral .tone-contour path { stroke: #8f8b87; stroke-dasharray: 3 4; }
.tone-card__example { display: flex; align-items: baseline; gap: 7px; margin-top: 14px; }
.tone-card__example strong { color: var(--app-ink); font-size: 27px; letter-spacing: -.06em; }
.tone-card__example span { color: var(--app-muted); font-family: "Noto Sans SC", sans-serif; font-size: 18px; }
.tone-card h3 { margin-top: 8px; font-size: 12px; letter-spacing: -.02em; }
.tone-card > p { min-height: 52px; margin: 6px 0 0; color: var(--app-muted); font-size: 10px; line-height: 1.55; }
.tone-card > small { display: block; margin-top: 8px; color: var(--app-blue); font-size: 10px; font-weight: 800; }
.tone-rule { display: flex; align-items: center; gap: 15px; margin: 0 22px 22px; padding: 15px 17px; border: 1px solid rgba(47,109,229,.16); border-radius: 13px; background: linear-gradient(110deg, var(--app-blue-soft), #fff 75%); }
.tone-rule__mark { display: grid; width: 52px; height: 52px; flex: 0 0 auto; place-items: center; border-radius: 14px; background: #fff; color: var(--app-blue); font-family: "Noto Sans SC", sans-serif; font-size: 22px; font-weight: 800; }
.tone-rule h3 { margin-top: 4px; font-size: 13px; }
.tone-rule p:last-child { margin: 5px 0 0; color: var(--app-muted); font-size: 11px; line-height: 1.6; }
.foundation-provenance { display: flex; align-items: flex-start; gap: 10px; padding: 2px 4px; color: var(--app-muted); }
.foundation-provenance__dot { display: grid; width: 18px; height: 18px; flex: 0 0 auto; place-items: center; border: 1px solid var(--app-line); border-radius: 50%; font-size: 10px; font-weight: 800; }
.foundation-provenance strong { color: var(--app-ink); font-size: 10px; }
.foundation-provenance p { margin: 4px 0 0; font-size: 10px; line-height: 1.55; }
.foundation-provenance__links { display: flex; flex-wrap: wrap; gap: 8px 14px; margin-top: 6px; }
.foundation-provenance__links a { color: var(--app-blue); font-size: 10px; font-weight: 700; }

@media (max-width: 1100px) {
  .foundation-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); }
  .tone-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); }
}
@media (max-width: 720px) {
  .foundation-context { align-items: flex-start; flex-direction: column; }
  .foundation-practice-link, .foundation-stage-action { margin-left: 0; }
  .foundation-summary { padding: 10px 12px; }
  .foundation-search-wrap { grid-template-columns: 1fr; gap: 6px; padding-inline: 16px; }
  .foundation-groups { padding-inline: 16px; }
  .foundation-grid, .tone-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .tone-grid { padding-inline: 16px; }
  .tone-rule { align-items: flex-start; margin-inline: 16px; }
}
@media (max-width: 420px) {
  .foundation-grid { gap: 7px; }
  .foundation-card { padding: 10px; }
  .foundation-card__symbol { font-size: 18px; }
  .foundation-card p { min-height: 44px; }
  .tone-grid { grid-template-columns: 1fr; }
  .tone-card > p { min-height: 0; }
  .foundation-example { gap: 6px; }
  .foundation-example__hanzi { font-size: 20px; }
  .foundation-example strong { font-size: 9px; }
}
</style>
