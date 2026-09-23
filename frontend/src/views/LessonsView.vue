<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { getJson } from '../services/api'
import type { HskLevel, HskLevelTree } from '../types'

type LessonRow = {
  id: string
  name: string
  levelId: string
  levelName: string
  topicName: string
  unitName: string
  status: string
}

const loading = ref(true)
const error = ref('')
const levels = ref<HskLevel[]>([])
const lessons = ref<LessonRow[]>([])
const selectedLevel = ref('all')
const search = ref('')

const visibleLessons = computed(() => {
  const query = search.value.trim().toLocaleLowerCase('vi-VN')
  return lessons.value.filter(lesson => {
    const matchesLevel = selectedLevel.value === 'all' || lesson.levelId === selectedLevel.value
    const searchable = `${lesson.name} ${lesson.levelName} ${lesson.topicName} ${lesson.unitName}`.toLocaleLowerCase('vi-VN')
    return matchesLevel && (!query || searchable.includes(query))
  })
})

function statusLabel(status: string) {
  return status === 'Published' ? 'Đã publish' : status
}

onMounted(async () => {
  try {
    const publishedLevels = await getJson<HskLevel[]>('/api/curriculum/hsk-levels')
    levels.value = publishedLevels

    const trees = await Promise.all(publishedLevels.map(async level => {
      try {
        return await getJson<HskLevelTree>(`/api/curriculum/hsk/${encodeURIComponent(level.id)}/tree`)
      } catch {
        return null
      }
    }))

    lessons.value = trees.flatMap(tree => tree?.topics.flatMap(topic => topic.units.flatMap(unit => unit.lessons.map(lesson => ({
      id: lesson.id,
      name: lesson.name,
      levelId: tree.level.id,
      levelName: tree.level.displayName,
      topicName: topic.name,
      unitName: unit.name,
      status: lesson.status,
    })))) ?? [])
  } catch {
    error.value = 'Chưa tải được danh sách bài học. Kiểm tra curriculum đã publish và phiên đăng nhập.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải bài học…</div>
  <div v-else-if="error" class="state-card state-card-muted">{{ error }}</div>
  <section v-else class="page-canvas page-content-view" aria-label="Bài học">
    <div class="page-inline-heading">
      <div>
        <p class="page-kicker">HỌC TẬP</p>
        <h1>Bài học</h1>
        <p class="page-inline-heading__note">{{ visibleLessons.length }} bài đã publish trong curriculum hiện tại.</p>
      </div>
      <RouterLink class="page-button page-button--blue" to="/app/hsk">Mở lộ trình <span aria-hidden="true">→</span></RouterLink>
    </div>

    <div class="page-filter-bar" aria-label="Bộ lọc bài học">
      <input v-model="search" class="page-search" type="search" placeholder="Tìm bài học, topic hoặc unit" aria-label="Tìm bài học" />
      <select v-model="selectedLevel" class="page-select" aria-label="Lọc theo cấp độ">
        <option value="all">Tất cả cấp độ</option>
        <option v-for="level in levels" :key="level.id" :value="level.id">{{ level.displayName }}</option>
      </select>
    </div>

    <section v-if="visibleLessons.length" class="page-card page-list" aria-label="Danh sách bài học">
      <RouterLink v-for="lesson in visibleLessons" :key="lesson.id" class="page-list-row" :to="`/app/lessons/${lesson.id}`">
        <span class="page-list-row__icon">课</span>
        <span class="page-list-row__body">
          <strong>{{ lesson.name }}</strong>
          <small>{{ lesson.levelName }} · {{ lesson.topicName }} · {{ lesson.unitName }}</small>
        </span>
        <span class="page-list-row__meta"><span class="page-status page-status--active">{{ statusLabel(lesson.status) }}</span><span aria-hidden="true">→</span></span>
      </RouterLink>
    </section>
    <section v-else class="page-card page-empty">
      <strong>Không tìm thấy bài học</strong>
      <span>Thử đổi cấp độ hoặc từ khóa tìm kiếm.</span>
    </section>
  </section>
</template>
