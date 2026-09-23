<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { ApiError, getJson, postJson } from '../services/api'
import type { AuthorizationContext } from '../types'

type AdminKind = 'curriculum' | 'questions' | 'audio' | 'stories' | 'videos' | 'resources' | 'tools'
type AdminViewMode = AdminKind | 'users'
type AdminContent = {
  id: string
  title?: string
  name?: string
  prompt?: string
  description?: string
  status?: string
  hskLevel?: string
  topic?: string
  contentVersion?: number
  type?: string
  skill?: string
  displayName?: string
  levelNumber?: number
  contentId?: string
  text?: string
  voice?: string
  attemptCount?: number
  audioUrl?: string | null
  lastError?: string | null
}
type AdminUser = {
  userId: string
  status: string
  displayName: string | null
  targetHskLevelId: string | null
  updatedAt: string
}

const navigation: { label: string; mode: AdminViewMode; permission: string }[] = [
  { label: 'Giáo trình', mode: 'curriculum', permission: 'curriculum.manage' },
  { label: 'Ngân hàng câu hỏi', mode: 'questions', permission: 'content.manage' },
  { label: 'Audio', mode: 'audio', permission: 'content.manage' },
  { label: 'Truyện', mode: 'stories', permission: 'content.manage' },
  { label: 'Video', mode: 'videos', permission: 'content.manage' },
  { label: 'Tài liệu', mode: 'resources', permission: 'content.manage' },
  { label: 'Công cụ', mode: 'tools', permission: 'content.manage' },
  { label: 'Người dùng', mode: 'users', permission: 'users.manage' },
]

const authorization = ref<AuthorizationContext | null>(null)
const mode = ref<AdminViewMode>('stories')
const content = ref<AdminContent[]>([])
const users = ref<AdminUser[]>([])
const loading = ref(true)
const working = ref(false)
const error = ref('')
const notice = ref('')
const curriculumVersionId = ref('')
const curriculumName = ref('')
const curriculumSourceType = ref('PlatformAuthoredReferenceFixture')
const curriculumLevelId = ref('')
const curriculumLevelNumber = ref(3)
const curriculumDisplayName = ref('HSK 3')
const curriculumTopicsJson = ref('[]')

const permissions = computed(() => new Set(authorization.value?.permissions ?? []))
const visibleNavigation = computed(() => navigation.filter(item => permissions.value.has(item.permission)))
const canManageContent = computed(() => permissions.value.has('content.manage'))
const canManageUsers = computed(() => permissions.value.has('users.manage'))
const forbidden = computed(() => Boolean(authorization.value && !visibleNavigation.value.length))
const contentMode = computed(() => mode.value !== 'users')
const pageTitle = computed(() => mode.value === 'users' ? 'Người dùng' : navigation.find(item => item.mode === mode.value)?.label ?? 'Nội dung')

function itemTitle(item: AdminContent) {
  return item.title ?? item.name ?? item.displayName ?? item.prompt ?? item.text ?? item.id
}

function itemMeta(item: AdminContent) {
  if (mode.value === 'audio') {
    return `${item.contentId ?? '—'} · ${item.voice ?? '—'} · lần ${item.attemptCount ?? 0}`
  }

  return item.hskLevel || item.topic || (item.status === 'Published' ? 'Đã public' : 'Cần duyệt')
}

function hasPermission(permission: string) {
  return permissions.value.has(permission)
}

async function loadContent(kind: AdminKind) {
  content.value = []
  const endpoint = kind === 'curriculum'
    ? '/api/admin/curriculum/hsk-levels'
    : kind === 'questions' ? '/api/admin/content/questions' : `/api/admin/content/${kind}`
  content.value = await getJson<AdminContent[]>(endpoint)
}

async function loadUsers() {
  users.value = await getJson<AdminUser[]>('/api/admin/users')
}

async function loadMode() {
  if (!authorization.value) return
  loading.value = true
  error.value = ''
  notice.value = ''
  try {
    if (mode.value === 'users') {
      if (!canManageUsers.value) return
      await loadUsers()
    } else {
      if (!canManageContent.value) return
      await loadContent(mode.value)
    }
  } catch (cause) {
    error.value = cause instanceof ApiError && cause.status === 403
      ? 'Bạn không có capability để xem khu vực này.'
      : 'Không tải được dữ liệu quản trị.'
  } finally {
    loading.value = false
  }
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    authorization.value = await getJson<AuthorizationContext>('/api/me/authorization')
    const firstVisible = visibleNavigation.value[0]
    if (firstVisible && !hasPermission(navigation.find(item => item.mode === mode.value)?.permission ?? '')) {
      mode.value = firstVisible.mode
    }
    await loadMode()
  } catch (cause) {
    error.value = cause instanceof ApiError && cause.status === 401
      ? 'Bạn cần đăng nhập bằng tài khoản quản trị để tiếp tục.'
      : 'Không tải được quyền quản trị.'
    loading.value = false
  }
}

async function publish(item: AdminContent) {
  if (!contentMode.value || mode.value === 'audio' || mode.value === 'questions' && !item.id) return
  working.value = true
  error.value = ''
  notice.value = ''
  try {
    const endpoint = mode.value === 'curriculum'
      ? `/api/admin/curriculum/hsk-levels/${encodeURIComponent(item.id)}/publish`
      : `/api/admin/content/${mode.value}/${encodeURIComponent(item.id)}/publish`
    const published = await postJson<AdminContent>(endpoint)
    const index = content.value.findIndex(entry => entry.id === item.id)
    if (index >= 0) content.value[index] = published
    notice.value = `Đã publish “${itemTitle(published)}”.`
  } catch {
    error.value = 'Không thể publish nội dung này.'
  } finally {
    working.value = false
  }
}

async function retryAudio(item: AdminContent) {
  working.value = true
  error.value = ''
  notice.value = ''
  try {
    const retried = await postJson<AdminContent>(`/api/admin/content/audio/${encodeURIComponent(item.id)}/retry`)
    const index = content.value.findIndex(entry => entry.id === item.id)
    if (index >= 0) content.value[index] = retried
    notice.value = `Đã đưa “${itemTitle(retried)}” vào hàng đợi tạo audio.`
  } catch {
    error.value = 'Không thể retry audio này.'
  } finally {
    working.value = false
  }
}

async function importCurriculum() {
  if (!hasPermission('curriculum.manage')) return
  error.value = ''
  notice.value = ''

  if (!curriculumVersionId.value.trim() || !curriculumName.value.trim() || !curriculumSourceType.value.trim()
    || !curriculumLevelId.value.trim() || !curriculumDisplayName.value.trim()) {
    error.value = 'Điền đủ version, tên syllabus, source, level id và tên level trước khi import.'
    return
  }

  let topics: unknown
  try {
    topics = JSON.parse(curriculumTopicsJson.value || '[]')
    if (!Array.isArray(topics)) throw new Error('topics must be an array')
  } catch {
    error.value = 'Topics phải là JSON array hợp lệ theo cấu trúc Topic → Unit → Lesson.'
    return
  }

  working.value = true
  try {
    const result = await postJson<{ syllabusVersionId: string; levelsUpserted: number; idempotent: boolean }>('/api/admin/curriculum/import', {
      syllabusVersionId: curriculumVersionId.value.trim(),
      syllabusName: curriculumName.value.trim(),
      sourceType: curriculumSourceType.value.trim(),
      levels: [{
        id: curriculumLevelId.value.trim(),
        levelNumber: Number(curriculumLevelNumber.value),
        displayName: curriculumDisplayName.value.trim(),
        topics,
      }],
    })
    notice.value = result.idempotent
      ? `Import không thay đổi dữ liệu: ${result.syllabusVersionId}.`
      : `Đã import ${result.levelsUpserted} level vào ${result.syllabusVersionId}.`
    await loadContent('curriculum')
  } catch {
    error.value = 'Không thể import curriculum. Kiểm tra schema, Id trùng hoặc version đã publish.'
  } finally {
    working.value = false
  }
}

watch(mode, loadMode)
onMounted(load)
</script>

<template>
  <div class="admin-page">
    <header class="admin-topbar">
      <div>
        <p class="admin-kicker">VIETAIS HSK 3.0 · ADMIN</p>
        <h1>{{ pageTitle }}</h1>
      </div>
      <RouterLink class="admin-back" to="/app">← Learner app</RouterLink>
    </header>

    <div v-if="loading" class="admin-state">Đang tải quyền quản trị…</div>
    <div v-else-if="error && !authorization" class="admin-state admin-state--error">{{ error }}</div>
    <div v-else-if="forbidden" class="admin-state admin-state--error">Tài khoản hiện tại không có quyền quản trị.</div>
    <div v-else class="admin-layout">
      <aside class="admin-nav" aria-label="Điều hướng quản trị">
        <p>KHÔNG GIAN QUẢN TRỊ</p>
        <button v-for="entry in visibleNavigation" :key="entry.mode" type="button" :class="{ 'is-active': mode === entry.mode }" @click="mode = entry.mode">{{ entry.label }}</button>
        <small v-if="!visibleNavigation.length">Không có module được cấp quyền.</small>
      </aside>

      <main class="admin-content">
        <div v-if="error" class="admin-notice admin-notice--error" role="alert">{{ error }}</div>
        <div v-if="notice" class="admin-notice" role="status">{{ notice }}</div>
        <section v-if="loading" class="admin-card"><strong>Đang tải…</strong></section>
        <section v-else-if="mode === 'users'" class="admin-card">
          <div class="admin-section-head"><div><h2>Người dùng</h2><p>Danh sách chỉ hiển thị khi capability `users.manage` được cấp.</p></div><span>{{ users.length }} user</span></div>
          <div class="admin-table" role="table" aria-label="Danh sách người dùng">
            <div v-for="user in users" :key="user.userId" class="admin-row"><strong>{{ user.displayName || user.userId }}</strong><span>{{ user.userId }}</span><span>{{ user.targetHskLevelId || 'Chưa đặt HSK' }}</span><b>{{ user.status }}</b></div>
            <p v-if="!users.length" class="admin-empty">Chưa có user đã provision.</p>
          </div>
        </section>
        <section v-else class="admin-card">
          <div class="admin-section-head"><div><h2>{{ pageTitle }}</h2><p>{{ mode === 'curriculum' ? 'Theo dõi HSK level draft/published và publish khi dữ liệu đã được duyệt.' : mode === 'audio' ? 'Theo dõi trạng thái tạo audio và đưa các asset lỗi vào hàng đợi retry.' : 'Chỉ record Content đã được cấp quyền mới xuất hiện ở đây.' }}</p></div><span>{{ content.length }} record</span></div>
          <form v-if="mode === 'curriculum'" class="admin-import" @submit.prevent="importCurriculum">
            <div class="admin-import__head"><div><strong>Import syllabus draft</strong><p>Nhập metadata provenance và một level. Topics là JSON array theo Topic → Unit → Lesson.</p></div><span>curriculum.manage</span></div>
            <div class="admin-import__grid">
              <label>Version<input v-model="curriculumVersionId" required placeholder="hsk3-2026-01" /></label>
              <label>Tên syllabus<input v-model="curriculumName" required placeholder="HSK 3.0 reference" /></label>
              <label>Source type<input v-model="curriculumSourceType" required placeholder="Official / PlatformAuthored..." /></label>
              <label>Level id<input v-model="curriculumLevelId" required placeholder="hsk3" /></label>
              <label>Số HSK<input v-model.number="curriculumLevelNumber" required min="1" max="9" type="number" /></label>
              <label>Tên level<input v-model="curriculumDisplayName" required placeholder="HSK 3" /></label>
            </div>
            <label class="admin-import__topics">Topics JSON<textarea v-model="curriculumTopicsJson" rows="4" spellcheck="false" aria-label="Topics JSON" /></label>
            <div class="admin-import__actions"><small>Import luôn ở trạng thái Draft; publish level sau khi review.</small><button class="admin-publish" type="submit" :disabled="working">{{ working ? 'Đang import…' : 'Import draft' }}</button></div>
          </form>
          <div class="admin-table" role="table" :aria-label="pageTitle">
            <div v-for="item in content" :key="item.id" class="admin-row"><div><strong>{{ itemTitle(item) }}</strong><span>{{ item.id }}<template v-if="item.levelNumber"> · HSK {{ item.levelNumber }}</template><template v-if="item.type"> · {{ item.type }}</template><template v-if="item.skill"> · {{ item.skill }}</template><template v-if="item.lastError"> · {{ item.lastError }}</template></span></div><span>{{ itemMeta(item) }}</span><b :class="{ 'is-draft': item.status === 'Draft', 'is-failed': item.status === 'Failed' }">{{ item.status || '—' }}</b><button v-if="mode === 'audio' && item.status === 'Failed'" class="admin-publish" type="button" :disabled="working" @click="retryAudio(item)">Thử lại</button><button v-else-if="mode !== 'audio' && item.status === 'Draft'" class="admin-publish" type="button" :disabled="working" @click="publish(item)">Publish</button></div>
            <p v-if="!content.length" class="admin-empty">Chưa có record trong module này.</p>
          </div>
        </section>
      </main>
    </div>
  </div>
</template>

<style scoped>
.admin-page { min-height: 100dvh; background: #f7f7f5; color: var(--app-ink); }
.admin-topbar { display: flex; align-items: center; justify-content: space-between; gap: 20px; padding: 26px clamp(20px, 4vw, 60px); border-bottom: 1px solid var(--app-line); background: #fff; }
.admin-kicker { margin: 0 0 8px; color: var(--app-blue); font-size: 10px; font-weight: 800; letter-spacing: .17em; }
.admin-topbar h1 { margin: 0; font-size: clamp(22px, 3vw, 34px); letter-spacing: -.055em; }
.admin-back { color: var(--app-blue); font-size: 12px; font-weight: 800; }
.admin-layout { display: grid; grid-template-columns: 240px minmax(0, 1fr); gap: 22px; width: min(1240px, calc(100% - 40px)); margin: 0 auto; padding: 28px 0 60px; }
.admin-nav { display: grid; align-content: start; gap: 5px; padding: 14px; border: 1px solid var(--app-line); border-radius: 16px; background: #fff; box-shadow: var(--app-card-shadow); }
.admin-nav p { margin: 4px 10px 9px; color: var(--app-muted); font-size: 9px; font-weight: 800; letter-spacing: .12em; }
.admin-nav button { min-height: 38px; border: 0; border-radius: 10px; background: transparent; color: var(--app-muted); cursor: pointer; font-size: 12px; font-weight: 750; text-align: left; }
.admin-nav button:hover, .admin-nav button.is-active { background: var(--app-blue-soft); color: var(--app-blue); }
.admin-nav small { padding: 8px 10px; color: var(--app-muted); font-size: 11px; line-height: 1.45; }
.admin-content { min-width: 0; }
.admin-card { overflow: hidden; border: 1px solid var(--app-line); border-radius: 18px; background: #fff; box-shadow: var(--app-card-shadow); }
.admin-section-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; padding: 22px; border-bottom: 1px solid var(--app-line); }
.admin-section-head h2 { margin: 0; font-size: 21px; letter-spacing: -.045em; }
.admin-section-head p { max-width: 560px; margin: 6px 0 0; color: var(--app-muted); font-size: 12px; line-height: 1.5; }
.admin-section-head > span { flex: 0 0 auto; color: var(--app-blue); font-size: 11px; font-weight: 800; }
.admin-table { display: grid; }
.admin-row { display: grid; grid-template-columns: minmax(180px, 1.8fr) minmax(100px, .8fr) auto auto; align-items: center; gap: 14px; padding: 14px 22px; border-bottom: 1px solid var(--app-line); font-size: 12px; }
.admin-row:last-child { border-bottom: 0; }
.admin-row strong, .admin-row span { display: block; min-width: 0; }
.admin-row span { margin-top: 4px; color: var(--app-muted); font-size: 10px; overflow-wrap: anywhere; }
.admin-row > span { margin: 0; }
.admin-row b { color: var(--app-blue); font-size: 10px; white-space: nowrap; }
.admin-row b.is-draft { color: #a86b14; }
.admin-row b.is-failed { color: #b04747; }
.admin-publish { min-height: 30px; padding: 0 11px; border: 1px solid rgba(47,109,229,.25); border-radius: 9px; background: var(--app-blue-soft); color: var(--app-blue); cursor: pointer; font-size: 10px; font-weight: 800; }
.admin-publish:disabled { cursor: wait; opacity: .6; }
.admin-empty, .admin-state { margin: 0; padding: 28px 22px; color: var(--app-muted); font-size: 13px; }
.admin-state { display: grid; min-height: 40vh; place-items: center; text-align: center; }
.admin-state--error, .admin-notice--error { color: #a33d3d; }
.admin-notice { margin-bottom: 12px; padding: 12px 14px; border: 1px solid rgba(47,109,229,.16); border-radius: 12px; background: var(--app-blue-soft); color: var(--app-blue); font-size: 12px; font-weight: 700; }
.admin-import { display: grid; gap: 14px; padding: 18px 22px; border-bottom: 1px solid var(--app-line); background: #fcfcfb; }
.admin-import__head, .admin-import__actions { display: flex; align-items: flex-start; justify-content: space-between; gap: 14px; }
.admin-import__head strong { font-size: 13px; }
.admin-import__head p, .admin-import__actions small { margin: 4px 0 0; color: var(--app-muted); font-size: 10px; line-height: 1.45; }
.admin-import__head span { flex: 0 0 auto; color: var(--app-blue); font-size: 10px; font-weight: 800; }
.admin-import__grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 10px; }
.admin-import label { display: grid; gap: 5px; color: var(--app-muted); font-size: 10px; font-weight: 800; }
.admin-import input, .admin-import textarea { width: 100%; min-width: 0; box-sizing: border-box; border: 1px solid var(--app-line); border-radius: 9px; background: #fff; color: var(--app-ink); font: inherit; font-size: 11px; font-weight: 500; outline: 0; }
.admin-import input { min-height: 32px; padding: 0 9px; }
.admin-import textarea { resize: vertical; padding: 9px; font-family: ui-monospace, SFMono-Regular, Menlo, monospace; line-height: 1.45; }
.admin-import input:focus, .admin-import textarea:focus { border-color: rgba(47,109,229,.55); box-shadow: 0 0 0 3px rgba(47,109,229,.1); }
.admin-import__topics { max-width: 780px; }
.admin-import__actions { align-items: center; }
.admin-import__actions small { margin: 0; }
@media (max-width: 720px) { .admin-topbar { align-items: flex-start; flex-direction: column; padding: 20px; } .admin-layout { display: block; width: min(100% - 24px, 520px); padding-top: 14px; } .admin-nav { margin-bottom: 14px; } .admin-nav button { padding-inline: 10px; } .admin-row { grid-template-columns: minmax(0, 1fr) auto; gap: 7px 12px; padding: 14px 16px; } .admin-row > span { grid-column: 1 / -1; } .admin-row b { grid-column: 2; grid-row: 1; } .admin-publish { grid-column: 2; grid-row: 2; } .admin-section-head { padding: 18px 16px; } }
@media (max-width: 720px) { .admin-import { padding: 16px; } .admin-import__grid { grid-template-columns: 1fr; } .admin-import__head, .admin-import__actions { align-items: stretch; flex-direction: column; } .admin-import__actions .admin-publish { align-self: flex-start; } }
</style>
