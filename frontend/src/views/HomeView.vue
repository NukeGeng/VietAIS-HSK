<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { getJson } from '../services/api'
import type { LearningHome, ProgressSnapshot } from '../types'

const loading = ref(true)
const error = ref('')
const home = ref<LearningHome | null>(null)
const progress = ref<ProgressSnapshot | null>(null)

onMounted(async () => {
  try {
    ;[home.value, progress.value] = await Promise.all([
      getJson<LearningHome>('/api/learning/home'),
      getJson<ProgressSnapshot>('/api/progress'),
    ])
  } catch {
    error.value = 'Chưa tải được dữ liệu học tập. Bạn có thể tiếp tục với các trang nền tảng.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <section class="page-heading compact-heading">
    <p class="eyebrow">TỔNG QUAN HỌC VIÊN</p>
    <h1>Lộ trình học HSK rõ ràng</h1>
    <p class="lede">Tiếp tục bài học hiện tại, ôn đúng lúc và biết mình nên làm gì tiếp theo.</p>
  </section>

  <div v-if="loading" class="state-card">Đang tải dữ liệu học tập…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>

  <section v-else class="dashboard-grid">
    <article class="feature-card feature-card-blue">
      <div>
        <p class="card-kicker">TIẾP TỤC HỌC</p>
        <h2>{{ home?.continueTarget ? 'Quay lại bài đang học' : 'Bắt đầu lộ trình của bạn' }}</h2>
        <p>{{ home?.continueTarget ?? 'Chọn HSK hoặc bắt đầu từ nền tảng Pinyin.' }}</p>
      </div>
      <RouterLink class="primary-button" :to="home?.continueTarget ? `/app/${home.continueTarget}` : '/app/hsk'">{{ home?.continueTarget ? 'Tiếp tục' : 'Mở lộ trình' }} <span>→</span></RouterLink>
    </article>

    <article class="summary-card">
      <p class="card-kicker">TỔNG QUAN</p>
      <div class="summary-number">{{ progress?.practiceAnswered ?? 0 }}</div>
      <p>lượt luyện tập đã trả lời</p>
      <div class="summary-meta"><span>Đúng {{ progress?.practiceCorrect ?? 0 }}</span><span>Cần ôn {{ progress?.practiceIncorrect ?? 0 }}</span></div>
    </article>

    <article class="next-card">
      <div>
        <p class="card-kicker">NỀN TẢNG</p>
        <h2>Người mới bắt đầu</h2>
        <p>Pinyin → Thanh điệu → Chữ Hán cơ bản</p>
      </div>
      <RouterLink class="text-link" to="/app/beginner">Xem lộ trình <span>→</span></RouterLink>
    </article>
  </section>
</template>
