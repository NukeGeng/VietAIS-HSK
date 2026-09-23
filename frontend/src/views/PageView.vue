<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { getJson } from '../services/api'

type PageItem = {
  icon: string
  title: string
  description: string
  to?: string
}

type PageConfig = {
  eyebrow: string
  title: string
  action?: { label: string; to: string }
  items: PageItem[]
}

type RemoteContent = {
  id: string
  title: string
  description?: string
  chinese?: string
  vietnamese?: string
  pinyin?: string | null
  sourceUrl?: string
  transcript?: string
  resourceType?: string
  url?: string
  route?: string
  hskLevel?: string
  topic?: string
}

const props = defineProps<{ title: string; eyebrow: string; description: string }>()
const route = useRoute()
const remoteItems = ref<PageItem[]>([])
const remoteDetail = ref<RemoteContent | null>(null)
const remoteLoading = ref(false)
const remoteFailed = ref(false)
let contentRequest = 0
const item = (icon: string, title: string, description: string, to?: string): PageItem => ({ icon, title, description, to })

// Core learner routes have their own production views. Keep this fallback only
// for extended content plus the small account pages; it must not become a
// second, stale implementation of a core screen.
const pageConfigs: Record<string, PageConfig> = {
  stories: { eyebrow: 'MỞ RỘNG', title: 'Truyện song ngữ', items: [] },
  video: { eyebrow: 'MỞ RỘNG', title: 'Video học', items: [] },
  resources: { eyebrow: 'MỞ RỘNG', title: 'Tài liệu', items: [] },
  tools: { eyebrow: 'MỞ RỘNG', title: 'Công cụ', items: [] },
  settings: { eyebrow: 'TÀI KHOẢN', title: 'Cài đặt', items: [item('◐', 'Giao diện', 'Chủ đề sáng và panda.'), item('♪', 'Âm thanh', 'Âm lượng và tự phát audio.'), item('✓', 'Học tập', 'Nhắc ôn và cấp độ mặc định.')] },
  help: { eyebrow: 'TÀI KHOẢN', title: 'Trợ giúp', items: [item('?', 'Câu hỏi thường gặp', 'Cách học, làm bài và xem tiến độ.'), item('→', 'Hướng dẫn sử dụng', 'Các luồng học chính trong VietAIS HSK.'), item('!', 'Báo lỗi / liên hệ', 'Gửi thông tin khi gặp vấn đề.')] },
}

function configForPath(path: string): PageConfig {
  const key = path.replace('/app/', '').split('/')[0]
  return pageConfigs[key] ?? { eyebrow: props.eyebrow, title: props.title, items: [] }
}

function extendedKind(path: string) {
  if (path === '/app/stories' || path.startsWith('/app/stories/')) return 'stories'
  if (path === '/app/video' || path.startsWith('/app/video/')) return 'videos'
  if (path === '/app/resources' || path.startsWith('/app/resources/')) return 'resources'
  if (path === '/app/tools' || path.startsWith('/app/tools/')) return 'tools'
  return null
}

function extendedRoute(kind: string, id: string) {
  const segment = kind === 'videos' ? 'video' : kind
  return `/app/${segment}/${encodeURIComponent(id)}`
}

function extendedListRoute(kind: string) {
  return `/app/${kind === 'videos' ? 'video' : kind}`
}

function remoteItem(kind: string, content: RemoteContent): PageItem {
  const icon = kind === 'stories' ? '文' : kind === 'videos' ? '▶' : kind === 'resources' ? '▤' : '⌘'
  const meta = [content.hskLevel, content.topic].filter(Boolean).join(' · ')
  const description = [content.description, meta].filter(Boolean).join(' · ')
  return content.route && kind === 'tools'
    ? item(icon, content.title, description, content.route)
    : item(icon, content.title, description, extendedRoute(kind, content.id))
}

async function loadExtendedContent() {
  const request = ++contentRequest
  const kind = extendedKind(route.path)
  const detailId = route.params.id as string | undefined
  remoteItems.value = []
  remoteDetail.value = null
  remoteFailed.value = false
  remoteLoading.value = false
  if (!kind) return

  remoteLoading.value = true
  try {
    if (detailId && kind !== 'tools') {
      const detail = await getJson<RemoteContent>(`/api/content/${kind}/${encodeURIComponent(detailId)}`)
      if (request !== contentRequest) return
      remoteDetail.value = detail
    } else {
      const list = await getJson<RemoteContent[]>(`/api/content/${kind}`)
      if (request !== contentRequest) return
      remoteItems.value = list.map(content => remoteItem(kind, content))
    }
  } catch {
    if (request !== contentRequest) return
    remoteFailed.value = true
  } finally {
    if (request === contentRequest) remoteLoading.value = false
  }
}

watch(() => route.fullPath, loadExtendedContent, { immediate: true })

const config = computed(() => {
  const base = configForPath(route.path)
  const kind = extendedKind(route.path)
  if (!kind) return base
  return { ...base, items: remoteItems.value }
})

function extendedListTitle(kind: string) {
  return kind === 'stories' ? 'Truyện song ngữ' : kind === 'videos' ? 'Video học' : kind === 'resources' ? 'Tài liệu' : 'Công cụ'
}
</script>

<template>
  <section class="page-canvas page-content-view" :aria-label="config.title">
    <div v-if="!remoteDetail" class="page-inline-heading">
      <div>
        <p class="page-kicker">{{ config.eyebrow }}</p>
        <h1>{{ config.title }}</h1>
      </div>
      <RouterLink v-if="config.action" class="page-button page-button--blue" :to="config.action.to">{{ config.action.label }} <span aria-hidden="true">→</span></RouterLink>
    </div>
    <section v-if="remoteLoading" class="page-card page-empty"><strong>Đang tải nội dung…</strong><span>Đang kiểm tra nội dung đã được publish.</span></section>
    <section v-else-if="remoteFailed" class="page-card page-empty" role="alert">
      <strong>Không tải được nội dung</strong>
      <span>Nội dung có thể không còn khả dụng hoặc kết nối đang gặp lỗi.</span>
      <button class="page-button" type="button" @click="loadExtendedContent">Thử lại</button>
    </section>
    <section v-else-if="remoteDetail" class="page-card page-detail page-detail--single">
      <div class="page-detail__main">
        <RouterLink class="page-back" :to="extendedListRoute(extendedKind(route.path)!)">← {{ extendedListTitle(extendedKind(route.path)!) }}</RouterLink>
        <h1>{{ remoteDetail.title }}</h1>
        <p class="page-inline-heading__note">{{ remoteDetail.description }}</p>
        <div v-if="remoteDetail.chinese" class="content-detail__language"><strong>{{ remoteDetail.chinese }}</strong><span v-if="remoteDetail.pinyin">{{ remoteDetail.pinyin }}</span><p>{{ remoteDetail.vietnamese }}</p></div>
        <div v-if="remoteDetail.transcript" class="page-callout"><strong>Transcript</strong><br>{{ remoteDetail.transcript }}</div>
        <a v-if="remoteDetail.sourceUrl || remoteDetail.url" class="page-button page-button--blue content-detail__action" :href="remoteDetail.sourceUrl || remoteDetail.url" target="_blank" rel="noreferrer">Mở tài nguyên →</a>
      </div>
    </section>
    <section v-else-if="config.items.length" class="page-card">
      <div class="page-list">
        <RouterLink v-for="entry in config.items.filter((pageItem) => pageItem.to)" :key="entry.icon + '-' + entry.title" :to="entry.to!" class="page-list-row">
          <span class="page-list-row__icon">{{ entry.icon }}</span>
          <span class="page-list-row__body"><strong>{{ entry.title }}</strong><small>{{ entry.description }}</small></span>
          <span class="page-list-row__meta">→</span>
        </RouterLink>
        <div v-for="entry in config.items.filter((pageItem) => !pageItem.to)" :key="entry.icon + '-' + entry.title" class="page-list-row page-list-row--disabled" aria-disabled="true">
          <span class="page-list-row__icon">{{ entry.icon }}</span>
          <span class="page-list-row__body"><strong>{{ entry.title }}</strong><small>{{ entry.description }}</small></span>
          <span class="page-list-row__meta">Đang chuẩn bị</span>
        </div>
      </div>
    </section>
    <section v-else class="page-card page-empty"><strong>{{ config.title }}</strong><span>{{ extendedKind(route.path) ? 'Chưa có nội dung đã publish.' : 'Nội dung đang được nối vào module tương ứng.' }}</span></section>
  </section>
</template>

<style scoped>
.page-detail--single { display: block; }
.page-detail--single h1 { margin-top: 8px; font-size: clamp(24px, 3vw, 34px); }
.content-detail__language { display: grid; gap: 6px; margin-top: 22px; padding: 18px; border: 1px solid var(--app-line); border-radius: 14px; background: #fafaf8; }
.content-detail__language strong { font-family: "Noto Sans SC", sans-serif; font-size: 24px; }
.content-detail__language span { color: var(--app-blue); font-size: 12px; font-weight: 800; }
.content-detail__language p { margin: 0; color: var(--app-muted); font-size: 13px; }
.content-detail__action { display: inline-flex; margin-top: 18px; }
</style>
