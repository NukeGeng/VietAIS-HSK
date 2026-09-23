<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { getJson, postJson } from '../services/api'
import type { HanziCharacter, HanziStrokeSet, HanziWritingAttempt, HanziWritingMode } from '../types'

const props = defineProps<{ mode: 'list' | 'related' | 'detail' | 'strokes' | 'write'; id?: string }>()
const route = useRoute()
const characters = ref<HanziCharacter[]>([])
const selected = ref<HanziCharacter | null>(null)
const strokeSet = ref<HanziStrokeSet | null>(null)
const search = ref('')
const loading = ref(true)
const error = ref('')
const activeStroke = ref(0)
const isAnimating = ref(false)
const canvas = ref<HTMLCanvasElement | null>(null)
const drawing = ref(false)
const currentPoints = ref<{ x: number; y: number }[]>([])
const drawnStrokes = ref<{ x: number; y: number }[][]>([])
const writingAttempt = ref<HanziWritingAttempt | null>(null)
const writingMode = ref<HanziWritingMode>('Recall')
const writingFeedback = ref('')
const writingApiError = ref('')
const writingBusy = ref(false)
let animationToken = 0
let writingAttemptLoadKey = ''
let writingAttemptLoadPromise: Promise<void> | null = null

const effectiveId = computed(() => props.id ?? (route.params.id as string | undefined) ?? characters.value[0]?.id ?? '')
const writingSessionStorageKey = computed(() => effectiveId.value ? `vietais.hanzi-writing.attempt.${effectiveId.value}` : '')
const visibleCharacters = computed(() => {
  const query = search.value.trim().toLocaleLowerCase('vi')
  if (!query) return characters.value
  return characters.value.filter(item => [item.character, item.pinyin, item.meaning, item.hskContext].some(value => value.toLocaleLowerCase('vi').includes(query)))
})
const relatedGroups = computed(() => characters.value.filter(item => item.relatedWords.length > 0))
const relatedWordCount = computed(() => relatedGroups.value.reduce((count, item) => count + item.relatedWords.length, 0))
const pageTitle = computed(() => props.mode === 'list' ? 'Chữ Hán' : props.mode === 'related' ? 'Từ liên quan' : props.mode === 'strokes' ? 'Thứ tự nét' : props.mode === 'write' ? 'Luyện viết' : 'Chi tiết chữ Hán')

onMounted(async () => {
  try {
    characters.value = await getJson<HanziCharacter[]>('/api/hanzi')
    await loadSelected()
  } catch {
    error.value = 'Chưa tải được dữ liệu chữ Hán.'
  } finally {
    loading.value = false
  }
})

watch(effectiveId, async () => { if (!loading.value) await loadSelected() })
watch(() => props.mode, async () => { if (!loading.value) await loadSelected() })

async function loadSelected() {
  if (props.mode === 'list' || props.mode === 'related' || !effectiveId.value) return
  selected.value = characters.value.find(item => item.id === effectiveId.value || item.character === effectiveId.value) ?? null
  if (!selected.value) { error.value = 'Không tìm thấy chữ Hán này.'; return }
  if (props.mode === 'strokes' || props.mode === 'write') {
    try {
      strokeSet.value = await getJson<HanziStrokeSet>(`/api/hanzi/${selected.value.id}/strokes`)
      activeStroke.value = props.mode === 'strokes' ? strokeSet.value.strokes.length : 0
      writingAttempt.value = null
      writingFeedback.value = ''
      writingApiError.value = ''
      if (props.mode === 'write') {
        await loadWritingAttempt()
      }
      await nextTick()
      drawCanvas()
    } catch { error.value = 'Chưa có dữ liệu thứ tự nét cho chữ này.' }
  }
}

async function loadWritingAttempt() {
  const storageKey = writingSessionStorageKey.value
  if (writingAttemptLoadPromise && writingAttemptLoadKey === storageKey) {
    await writingAttemptLoadPromise
    return
  }

  writingAttemptLoadKey = storageKey
  writingAttemptLoadPromise = (async () => {
    const storedAttemptId = storageKey ? window.sessionStorage.getItem(storageKey) : null
    if (storedAttemptId) {
      try {
        writingAttempt.value = await getJson<HanziWritingAttempt>(`/api/practice/hanzi/attempts/${encodeURIComponent(storedAttemptId)}`)
        writingMode.value = writingAttempt.value.mode
        return
      } catch {
        window.sessionStorage.removeItem(storageKey)
      }
    }

    if (!selected.value) return
    try {
      writingAttempt.value = await postJson<HanziWritingAttempt>(`/api/practice/hanzi/${selected.value.id}/attempts`, { mode: writingMode.value })
      if (storageKey) window.sessionStorage.setItem(storageKey, writingAttempt.value.id)
    } catch {
      writingApiError.value = 'Chưa kết nối được attempt backend; bạn vẫn có thể luyện thử trên canvas.'
    }
  })()

  try {
    await writingAttemptLoadPromise
  } finally {
    if (writingAttemptLoadKey === storageKey) {
      writingAttemptLoadPromise = null
    }
  }
}

async function playStrokes() {
  if (!strokeSet.value || isAnimating.value) return
  const token = ++animationToken
  isAnimating.value = true
  activeStroke.value = 0
  for (let index = 1; index <= strokeSet.value.strokes.length; index += 1) {
    await new Promise(resolve => window.setTimeout(resolve, 360))
    if (token !== animationToken) return
    activeStroke.value = index
  }
  isAnimating.value = false
}

function stopAnimation() { animationToken += 1; isAnimating.value = false }
function clearCanvas() { drawnStrokes.value = []; currentPoints.value = []; drawCanvas() }

async function restartWriting() {
  clearCanvas()
  writingFeedback.value = ''
  writingApiError.value = ''
  if (!selected.value) return
  try {
    writingAttempt.value = await postJson<HanziWritingAttempt>(`/api/practice/hanzi/${selected.value.id}/attempts`, { mode: writingMode.value })
    const storageKey = writingSessionStorageKey.value
    if (storageKey) window.sessionStorage.setItem(storageKey, writingAttempt.value.id)
  } catch {
    writingAttempt.value = null
    const storageKey = writingSessionStorageKey.value
    if (storageKey) window.sessionStorage.removeItem(storageKey)
    writingApiError.value = 'Attempt mới chưa được lưu; kiểm tra phiên đăng nhập rồi thử lại.'
  }
}

async function selectWritingMode(mode: HanziWritingMode) {
  writingMode.value = mode
  await restartWriting()
}

function pointFromEvent(event: PointerEvent) {
  const element = canvas.value
  if (!element) return { x: 0, y: 0 }
  const bounds = element.getBoundingClientRect()
  return { x: ((event.clientX - bounds.left) / bounds.width) * 100, y: ((event.clientY - bounds.top) / bounds.height) * 100 }
}

function startStroke(event: PointerEvent) {
  if (!canvas.value || writingBusy.value || writingAttempt.value?.status === 'Completed') return
  drawing.value = true
  canvas.value.setPointerCapture(event.pointerId)
  currentPoints.value = [pointFromEvent(event)]
  drawCanvas()
}

function moveStroke(event: PointerEvent) { if (drawing.value) { currentPoints.value.push(pointFromEvent(event)); drawCanvas() } }

async function endStroke() {
  if (!drawing.value) return
  drawing.value = false
  const points = [...currentPoints.value]
  currentPoints.value = []
  drawCanvas()
  if (points.length < 2 || writingBusy.value) return

  if (!writingAttempt.value || !selected.value) {
    drawnStrokes.value.push(points)
    drawCanvas()
    return
  }

  writingBusy.value = true
  try {
    const response = await postJson<{ attempt: HanziWritingAttempt; result: { result: string; feedback: string } }>(`/api/practice/hanzi/attempts/${writingAttempt.value.id}/strokes`, { points })
    writingAttempt.value = response.attempt
    writingFeedback.value = response.result.feedback
    if (response.result.result === 'Correct') drawnStrokes.value.push(points)
    drawCanvas()
  } catch {
    writingApiError.value = 'Không gửi được nét viết lên backend.'
  } finally {
    writingBusy.value = false
  }
}

async function completeWriting() {
  if (!writingAttempt.value || writingBusy.value) return
  writingBusy.value = true
  try {
    writingAttempt.value = await postJson<HanziWritingAttempt>(`/api/practice/hanzi/attempts/${writingAttempt.value.id}/complete`)
    const storageKey = writingSessionStorageKey.value
    if (storageKey) window.sessionStorage.setItem(storageKey, writingAttempt.value.id)
    writingFeedback.value = writingAttempt.value.overallResult === 'Correct'
      ? 'Hoàn thành đúng các nét.'
      : 'Lượt viết cần thử lại; bạn có thể tạo lượt mới.'
  } catch {
    writingApiError.value = 'Không thể hoàn tất lượt viết.'
  } finally {
    writingBusy.value = false
  }
}

function drawCanvas() {
  const element = canvas.value
  if (!element) return
  const context = element.getContext('2d')
  if (!context) return
  const size = element.getBoundingClientRect().width || 300
  const ratio = window.devicePixelRatio || 1
  element.width = size * ratio
  element.height = size * ratio
  context.setTransform(ratio, 0, 0, ratio, 0, 0)
  context.clearRect(0, 0, size, size)
  context.strokeStyle = 'rgba(47,109,229,.13)'
  context.lineWidth = 1
  context.beginPath(); context.moveTo(size / 2, 0); context.lineTo(size / 2, size); context.moveTo(0, size / 2); context.lineTo(size, size / 2); context.stroke()
  context.strokeStyle = 'rgba(47,109,229,.2)'; context.setLineDash([5, 5]); context.strokeRect(1, 1, size - 2, size - 2); context.setLineDash([])
  const drawPoints = (points: { x: number; y: number }[], color: string) => {
    if (points.length < 2) return
    context.strokeStyle = color; context.lineWidth = 5; context.lineCap = 'round'; context.lineJoin = 'round'; context.beginPath()
    points.forEach((point, index) => index ? context.lineTo(point.x / 100 * size, point.y / 100 * size) : context.moveTo(point.x / 100 * size, point.y / 100 * size))
    context.stroke()
  }
  drawnStrokes.value.forEach(points => drawPoints(points, '#2f6de5'))
  drawPoints(currentPoints.value, '#161516')
}

function onResize() { if (props.mode === 'write') drawCanvas() }
onMounted(() => window.addEventListener('resize', onResize))
onBeforeUnmount(() => { stopAnimation(); window.removeEventListener('resize', onResize) })
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải chữ Hán…</div>
  <div v-else-if="error && !selected" class="state-card state-card-muted">{{ error }}</div>
  <template v-else-if="props.mode === 'list'">
    <section class="page-card hanzi-list-card">
      <div class="page-section-title"><div><p class="page-kicker">NỀN TẢNG · CHỮ HÁN</p><h2>Danh sách chữ</h2><p>Chữ, Pinyin, nghĩa và số nét trong reference catalog hiện tại.</p></div><span>{{ visibleCharacters.length }} chữ</span></div>
      <div class="hanzi-search"><label class="page-form-label" for="hanzi-search">Tìm chữ</label><input id="hanzi-search" v-model="search" class="page-search" type="search" placeholder="Ví dụ: dà, lớn, HSK 1" /></div>
      <div class="hanzi-grid"><RouterLink v-for="item in visibleCharacters" :key="item.id" class="hanzi-card" :to="`/app/hanzi/${item.id}`"><strong>{{ item.character }}</strong><span>{{ item.pinyin }}</span><small>{{ item.meaning }} · {{ item.strokeCount }} nét</small></RouterLink></div>
      <p class="hanzi-provenance">Reference hiện tại: {{ characters[0]?.source }} · {{ characters[0]?.sourceVersion }}. Dataset HSK chính thức sẽ được import/publish qua Curriculum.</p>
    </section>
  </template>
  <template v-else-if="props.mode === 'related'">
    <section class="page-card hanzi-related-page">
      <div class="page-inline-heading"><div><p class="page-kicker">CHỮ HÁN</p><h1>Từ liên quan</h1></div><span>{{ relatedWordCount }} từ</span></div>
      <div class="hanzi-related-groups">
        <article v-for="group in relatedGroups" :key="group.id" class="hanzi-related-group">
          <div class="hanzi-related-group__head"><RouterLink class="hanzi-related-glyph" :to="`/app/hanzi/${group.id}`">{{ group.character }}</RouterLink><div><strong>{{ group.pinyin }} · {{ group.meaning }}</strong><small>{{ group.hskContext }} · {{ group.strokeCount }} nét</small></div></div>
          <div class="hanzi-related-words"><RouterLink v-for="word in group.relatedWords" :key="word" :to="`/app/hanzi/${group.id}`">{{ word }}</RouterLink></div>
        </article>
      </div>
      <p class="hanzi-provenance">Reference hiện tại: {{ characters[0]?.source }} · {{ characters[0]?.sourceVersion }}. Từ liên quan sẽ mở rộng khi catalog được duyệt thêm.</p>
    </section>
  </template>
  <template v-else-if="selected">
    <section class="page-card hanzi-detail-card">
      <RouterLink class="page-back" to="/app/hanzi">← Danh sách chữ</RouterLink>
      <div class="hanzi-detail-head"><div class="hanzi-detail-character">{{ selected.character }}</div><div><p class="page-kicker">{{ pageTitle.toUpperCase() }}</p><h2>{{ selected.pinyin }} · {{ selected.meaning }}</h2><p>Bộ {{ selected.radical }} · {{ selected.strokeCount }} nét · {{ selected.hskContext }}</p><div class="hanzi-related"><span v-for="word in selected.relatedWords" :key="word">{{ word }}</span></div></div></div>
      <div v-if="props.mode === 'detail'" class="hanzi-detail-actions"><RouterLink class="page-button page-button--blue" :to="`/app/hanzi/${selected.id}/strokes`">Xem thứ tự nét →</RouterLink><RouterLink class="page-button page-button--quiet" :to="`/app/hanzi/${selected.id}/write`">Luyện viết</RouterLink></div>
      <template v-else-if="props.mode === 'strokes' && strokeSet">
        <div class="stroke-toolbar"><span>{{ activeStroke }} / {{ strokeSet.strokeCount }} nét</span><button class="page-button page-button--quiet" type="button" :disabled="isAnimating" @click="playStrokes">Xem từng nét</button><button class="page-button page-button--quiet" type="button" @click="stopAnimation(); activeStroke = strokeSet.strokes.length">Hiện tất cả</button></div>
        <svg class="stroke-stage" viewBox="0 0 100 100" role="img" :aria-label="`Thứ tự nét chữ ${selected.character}`"><path v-for="stroke in strokeSet.strokes" :key="stroke.id" :d="stroke.path" :class="{ 'is-visible': stroke.order <= activeStroke }" /><template v-for="stroke in strokeSet.strokes" :key="`${stroke.id}-number`"><text v-if="stroke.order <= activeStroke" :x="15 + stroke.order * 5" :y="15 + stroke.order * 8">{{ stroke.order }}</text></template></svg>
        <ol class="stroke-list"><li v-for="stroke in strokeSet.strokes" :key="stroke.id" :class="{ 'is-active': stroke.order === activeStroke }"><span>{{ stroke.order }}</span><strong>{{ stroke.description }}</strong></li></ol>
        <p class="hanzi-provenance">Nguồn: {{ strokeSet.source }} · {{ strokeSet.sourceVersion }} · {{ strokeSet.licenseRef }}</p>
      </template>
      <template v-else-if="props.mode === 'write' && strokeSet">
        <div class="writing-toolbar"><div><strong>Vùng luyện viết</strong><p>Validator deterministic kiểm tra điểm đầu/cuối, hướng và thứ tự nét; không dùng AI.</p></div><span>{{ writingAttempt?.acceptedStrokeCount ?? drawnStrokes.length }} / {{ strokeSet.strokeCount }} nét</span></div>
        <div class="writing-modes" aria-label="Chế độ luyện viết"><span>Chế độ</span><button v-for="mode in (['Guided', 'Trace', 'Recall'] as HanziWritingMode[])" :key="mode" class="writing-mode" :class="{ 'is-active': writingMode === mode }" type="button" :disabled="writingBusy" @click="selectWritingMode(mode)">{{ mode === 'Guided' ? 'Theo hướng dẫn' : mode === 'Trace' ? 'Tô theo mẫu' : 'Tự nhớ viết' }}</button></div>
        <canvas ref="canvas" class="hanzi-writing-canvas" aria-label="Vùng luyện viết chữ Hán" @pointerdown="startStroke" @pointermove="moveStroke" @pointerup="endStroke" @pointercancel="endStroke" />
        <div class="hanzi-detail-actions"><button class="page-button page-button--blue" type="button" :disabled="writingBusy || writingAttempt?.status === 'Completed'" @click="completeWriting">Hoàn tất lượt viết</button><button class="page-button page-button--quiet" type="button" :disabled="writingBusy" @click="restartWriting">Làm lại</button><RouterLink class="page-button page-button--quiet" :to="`/app/hanzi/${selected.id}/strokes`">Xem nét mẫu</RouterLink></div>
        <p v-if="writingFeedback" class="writing-feedback">{{ writingFeedback }}</p>
        <p v-if="writingApiError" class="inline-message inline-message--error">{{ writingApiError }}</p>
        <p class="hanzi-provenance">Kết quả hoàn tất đã gửi signal sang Review/Progress; dataset stroke hiện tại vẫn là reference fixture.</p>
      </template>
      <p v-if="error" class="inline-message inline-message--error">{{ error }}</p>
    </section>
  </template>
</template>

<style scoped>
.hanzi-list-card, .hanzi-detail-card { overflow: hidden; padding-bottom: 20px; }
.hanzi-related-page { display: grid; gap: 16px; }
.hanzi-related-groups { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; }
.hanzi-related-group { display: grid; gap: 14px; min-width: 0; padding: 16px; border: 1px solid var(--app-line); border-radius: 13px; background: #fff; }
.hanzi-related-group__head { display: flex; align-items: center; gap: 10px; min-width: 0; }
.hanzi-related-group__head strong, .hanzi-related-group__head small { display: block; }
.hanzi-related-group__head strong { color: var(--app-ink); font-size: 12px; }
.hanzi-related-group__head small { margin-top: 4px; color: var(--app-muted); font-size: 10px; }
.hanzi-related-glyph { display: grid; width: 48px; height: 48px; flex: 0 0 auto; place-items: center; border-radius: 11px; background: var(--app-soft); color: var(--app-ink); font-family: "Noto Sans SC", sans-serif; font-size: 28px; line-height: 1; }
.hanzi-related-words { display: flex; flex-wrap: wrap; gap: 6px; }
.hanzi-related-words a { padding: 6px 8px; border-radius: 8px; background: var(--app-blue-soft); color: var(--app-blue); font-family: "Noto Sans SC", sans-serif; font-size: 12px; font-weight: 700; }
.hanzi-search { display: grid; grid-template-columns: auto minmax(180px, 340px); align-items: center; gap: 12px; padding: 0 22px 16px; }
.hanzi-search .page-form-label { margin: 0; }
.hanzi-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 10px; padding: 0 22px; }
.hanzi-card { display: grid; gap: 5px; min-width: 0; padding: 15px; border: 1px solid var(--app-line); border-radius: 13px; background: #fff; }
.hanzi-card:hover { border-color: rgba(47,109,229,.35); box-shadow: 0 10px 22px rgba(33,29,30,.08); transform: translateY(-1px); }
.hanzi-card strong { color: var(--app-ink); font-family: "Noto Sans SC", sans-serif; font-size: 38px; line-height: 1; }
.hanzi-card span { color: var(--app-blue); font-size: 12px; font-weight: 800; }
.hanzi-card small { color: var(--app-muted); font-size: 10px; }
.hanzi-provenance { margin: 16px 22px 0; color: var(--app-muted); font-size: 10px; line-height: 1.5; }
.hanzi-detail-card { padding: 22px; }
.hanzi-detail-head { display: flex; align-items: center; gap: 20px; }
.hanzi-detail-character { display: grid; width: 132px; height: 132px; flex: 0 0 auto; place-items: center; border: 1px solid var(--app-line); border-radius: 20px; background: #fafaf8; color: var(--app-ink); font-family: "Noto Sans SC", sans-serif; font-size: 88px; line-height: 1; }
.hanzi-detail-head h2 { margin: 5px 0 0; font-size: 22px; letter-spacing: -.04em; }
.hanzi-detail-head p:not(.page-kicker) { margin: 6px 0 0; color: var(--app-muted); font-size: 12px; }
.hanzi-related { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 12px; }
.hanzi-related span { padding: 6px 8px; border-radius: 8px; background: var(--app-soft); color: var(--app-muted); font-family: "Noto Sans SC", sans-serif; font-size: 11px; }
.hanzi-detail-actions { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 22px; }
.stroke-toolbar, .writing-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-top: 24px; }
.stroke-toolbar > span, .writing-toolbar > span { color: var(--app-blue); font-size: 12px; font-weight: 800; }
.stroke-toolbar .page-button { min-height: 34px; }
.stroke-stage { display: block; width: min(100%, 340px); height: auto; margin: 18px auto 0; border: 1px dashed var(--app-line); border-radius: 16px; background: #fff; }
.stroke-stage path { fill: none; stroke: #d7d5d1; stroke-linecap: round; stroke-linejoin: round; stroke-width: 5; }
.stroke-stage path.is-visible { stroke: var(--app-blue); }
.stroke-stage text { fill: var(--app-ink); font-size: 5px; font-weight: 800; }
.stroke-list { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 8px; margin: 18px 0 0; padding: 0; list-style: none; }
.stroke-list li { display: flex; align-items: center; gap: 7px; min-width: 0; padding: 9px; border: 1px solid var(--app-line); border-radius: 10px; color: var(--app-muted); font-size: 10px; }
.stroke-list li.is-active { border-color: rgba(47,109,229,.3); background: var(--app-blue-soft); color: var(--app-blue); }
.stroke-list li span { display: grid; width: 20px; height: 20px; flex: 0 0 auto; place-items: center; border-radius: 6px; background: var(--app-soft); font-weight: 800; }
.stroke-list li strong { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.writing-toolbar { align-items: flex-start; }
.writing-toolbar strong { font-size: 14px; }
.writing-toolbar p { margin: 5px 0 0; color: var(--app-muted); font-size: 11px; }
.writing-modes { display: flex; flex-wrap: wrap; align-items: center; gap: 6px; margin-top: 16px; color: var(--app-muted); font-size: 11px; }
.writing-mode { min-height: 30px; padding: 5px 9px; border: 1px solid var(--app-line); border-radius: 8px; background: #fff; color: var(--app-muted); font-size: 11px; cursor: pointer; }
.writing-mode.is-active { border-color: rgba(47,109,229,.32); background: var(--app-blue-soft); color: var(--app-blue); font-weight: 800; }
.writing-mode:disabled { cursor: wait; opacity: .6; }
.hanzi-writing-canvas { display: block; width: min(100%, 360px); aspect-ratio: 1; margin: 18px auto 0; border: 1px solid rgba(47,109,229,.22); border-radius: 16px; background: #fff; touch-action: none; }
.writing-feedback { margin: 12px 0 0; color: var(--app-blue); font-size: 11px; font-weight: 700; }
.inline-message { margin: 12px 0 0; color: #a34444; font-size: 11px; }
@media (max-width: 900px) { .hanzi-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); } .hanzi-related-groups { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
@media (max-width: 560px) { .hanzi-search { grid-template-columns: 1fr; gap: 6px; padding-inline: 16px; } .hanzi-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); padding-inline: 16px; } .hanzi-related-groups { grid-template-columns: 1fr; } .hanzi-detail-card { padding: 16px; } .hanzi-detail-head { align-items: flex-start; gap: 14px; } .hanzi-detail-character { width: 96px; height: 96px; font-size: 62px; } .hanzi-detail-head h2 { font-size: 18px; } .stroke-toolbar, .writing-toolbar { align-items: flex-start; flex-direction: column; } .stroke-list { grid-template-columns: 1fr; } .hanzi-provenance { margin-inline: 0; } }
</style>
