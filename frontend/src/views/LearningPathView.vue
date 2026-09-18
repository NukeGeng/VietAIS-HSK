<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { getJson, postJson } from '../services/api'
import type { BeginnerLearning, HskLevel, LearningState } from '../types'

const props = defineProps<{ mode: 'hsk' | 'beginner' }>()
const loading = ref(true)
const error = ref('')
const levels = ref<HskLevel[]>([])
const beginner = ref<BeginnerLearning | null>(null)
const actionMessage = ref('')

onMounted(async () => {
  try {
    if (props.mode === 'beginner') {
      beginner.value = await getJson<BeginnerLearning>('/api/learning/beginner')
    } else {
      levels.value = await getJson<HskLevel[]>('/api/curriculum/hsk-levels')
    }
  } catch {
    error.value = 'Chưa tải được dữ liệu lộ trình. Kiểm tra phiên đăng nhập hoặc dữ liệu curriculum đã publish.'
  } finally {
    loading.value = false
  }
})

async function startBeginner() {
  actionMessage.value = ''
  try {
    const updated = await postJson<LearningState>('/api/learning/beginner/start')
    if (beginner.value) beginner.value.state = updated
    actionMessage.value = 'Đã bắt đầu lộ trình nền tảng.'
  } catch {
    actionMessage.value = 'Không thể bắt đầu lộ trình lúc này.'
  }
}
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải lộ trình…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>

  <template v-else-if="mode === 'beginner' && beginner">
    <section class="page-card page-card--soft page-feature-intro">
      <div>
        <p class="page-kicker">BẮT ĐẦU TỪ SỐ 0</p>
        <h1>Làm quen với tiếng Trung</h1>
        <p class="page-copy">Đi từng bước nhỏ từ âm, thanh điệu đến chữ Hán cơ bản trước khi vào lộ trình HSK.</p>
      </div>
      <div class="page-actions"><button class="page-button page-button--blue" type="button" @click="startBeginner">{{ beginner.state.currentTrack === 'beginner' ? 'Tiếp tục' : 'Bắt đầu' }} →</button></div>
    </section>
    <p v-if="actionMessage" class="inline-message">{{ actionMessage }}</p>
    <section class="page-card path-list">
      <div class="page-section-title"><div><h2>Học nền tảng theo thứ tự</h2><p>{{ beginner.track.name }}</p></div><span>{{ beginner.track.stages.length }} bước</span></div>
      <article v-for="stage in beginner.track.stages" :key="stage.id" class="path-row">
        <span class="page-list-row__icon step-number">{{ stage.order }}</span>
        <div class="page-list-row__body"><strong>{{ stage.name }}</strong><span>{{ stage.status === 'Published' ? 'Sẵn sàng học' : 'Đang chuẩn bị' }}</span></div>
        <span class="page-list-row__meta"><span class="page-status page-status--active">{{ beginner.state.currentBeginnerStageId === stage.id ? 'Đang học' : stage.status }}</span></span>
      </article>
    </section>
  </template>

  <template v-else>
    <section class="page-card page-card--soft page-feature-intro">
      <p class="page-kicker">ĐANG HỌC</p><h1>Lộ trình HSK</h1><p class="page-copy">Chọn cấp độ, theo dõi bài học và tiếp tục đúng mạch học.</p>
    </section>
    <div v-if="levels.length" class="level-grid">
      <article v-for="level in levels" :key="level.id" class="level-card">
        <div class="level-badge">HSK {{ level.levelNumber }}</div>
        <div><h2>{{ level.displayName }}</h2><p>Phiên dữ liệu: {{ level.syllabusVersionId }}</p></div>
        <RouterLink class="text-link" :to="`/app/hsk/${level.id}`">Mở cấp độ <span>→</span></RouterLink>
      </article>
    </div>
    <div v-else class="empty-page-card"><div class="empty-icon">+</div><div><h2>Chưa có HSK đã publish</h2><p>Admin cần import và publish syllabus trước khi learner chọn cấp độ.</p></div></div>
  </template>
</template>
