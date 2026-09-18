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

const overall = 62
const skillCards = [
  { label: 'Từ vựng', icon: '词', value: '82%', to: '/app/vocabulary' },
  { label: 'Hán tự', icon: '字', value: '73%', to: '/app/hanzi' },
  { label: 'Ngữ pháp', icon: '语', value: '61%', to: '/app/grammar' },
  { label: 'Nghe · Đọc · Viết', icon: '听', value: '58%', to: '/app/skills' },
]

const skillOverview = [
  { label: 'Nghe', icon: '听', value: '58% · Đang luyện', to: '/app/skills/listening' },
  { label: 'Đọc', icon: '读', value: '64% · Đang luyện', to: '/app/skills/reading' },
  { label: 'Viết', icon: '写', value: '46% · Cần ôn', to: '/app/skills/writing' },
  { label: 'Nói - đối thoại', icon: '说', value: '32% · Cần ôn', to: '/app/skills/speaking' },
  { label: 'Dịch Việt - Trung', icon: '译', value: '28% · Cần ôn', to: '/app/skills/translation' },
]
</script>

<template>
  <section class="app-page-heading app-page-heading--minimal app-reveal" aria-label="Ngữ cảnh trang">
    <p class="app-eyebrow">TỔNG QUAN HỌC VIÊN</p>
  </section>

  <div v-if="loading" class="state-card">Đang tải dữ liệu học tập…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>

  <template v-else>
    <section class="app-progress-card app-reveal app-delay-1" aria-labelledby="progress-title">
      <div class="app-progress-card__top">
        <div class="app-progress-card__identity">
          <span class="app-progress-card__icon">↗</span>
          <div><p class="app-eyebrow">LỘ TRÌNH HSK</p><h2 id="progress-title">HSK 3 · Tiến độ học</h2></div>
        </div>
        <div class="app-progress-stats"><span>3 kỹ năng tốt</span><span>2 cần ôn</span></div>
      </div>
      <div class="app-progress-bar" role="progressbar" aria-label="Tiến độ học HSK" aria-valuemin="0" aria-valuemax="100" :aria-valuenow="overall" tabindex="0">
        <span class="app-progress-bar__fill" :style="{ width: `${overall}%` }" />
        <span class="app-progress-bar__value" aria-hidden="true">{{ overall }}%</span>
      </div>
      <div class="app-levels"><span>HSK 1</span><span>HSK 2</span><strong>HSK 3</strong><span>HSK 4</span><span>HSK 6</span></div>
      <div class="app-skill-grid">
        <RouterLink v-for="card in skillCards" :key="card.label" class="app-skill-card" :to="card.to"><span class="app-skill-card__icon">{{ card.icon }}</span><span><strong>{{ card.label }}</strong><small>{{ card.value }}</small></span><span class="app-skill-card__arrow">→</span></RouterLink>
      </div>
    </section>

    <section class="app-next-card app-reveal app-delay-2" aria-labelledby="next-title">
      <div class="app-section-heading"><h2 id="next-title">Bài học tiếp theo</h2><span class="app-tag">HSK 3</span></div>
      <RouterLink class="app-lesson-row" to="/app/lessons/grammar-context"><span class="app-lesson-row__hanzi">把</span><span class="app-lesson-row__body"><strong>Ngữ pháp trong ngữ cảnh</strong><small>Bài 08 · 12 phút</small></span><span class="app-lesson-row__arrow">→</span></RouterLink>
      <RouterLink class="app-lesson-row" to="/app/lessons/vocabulary-work"><span class="app-lesson-row__hanzi">工</span><span class="app-lesson-row__body"><strong>Từ vựng · Công việc</strong><small>18 từ · Ôn SRS</small></span><span class="app-lesson-row__arrow">→</span></RouterLink>
      <RouterLink class="app-lesson-row" to="/app/skills/speaking"><span class="app-lesson-row__hanzi">说</span><span class="app-lesson-row__body"><strong>Speaking · Daily routines</strong><small>Luyện hội thoại</small></span><span class="app-lesson-row__arrow">→</span></RouterLink>
    </section>

    <section class="app-next-card app-skill-overview app-reveal app-delay-2" id="skills" aria-labelledby="skills-overview-title">
      <div class="app-section-heading"><h2 id="skills-overview-title">Kỹ năng</h2><span class="app-tag">HSK 3</span></div>
      <p class="app-skill-overview__copy">Theo dõi từng kỹ năng và đi thẳng vào nội dung cần luyện.</p>
      <div class="app-skill-overview__grid">
        <RouterLink v-for="card in skillOverview" :key="card.label" class="app-skill-card" :to="card.to"><span class="app-skill-card__icon">{{ card.icon }}</span><span><strong>{{ card.label }}</strong><small>{{ card.value }}</small></span><span class="app-skill-card__arrow">→</span></RouterLink>
      </div>
    </section>
  </template>
</template>
