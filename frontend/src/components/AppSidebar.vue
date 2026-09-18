<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { navigation } from '../navigation'
import { useUiStore } from '../stores/ui'
import type { NavItem } from '../types'

const route = useRoute()
const ui = useUiStore()

const isActive = (item: NavItem) => {
  if (!item.to) return false
  if (item.to === '/app') return route.path === '/app'
  return route.path === item.to || route.path.startsWith(`${item.to}/`)
}

const hasActiveChild = (item: NavItem) => item.children?.some(isActive) ?? false
const isExpanded = (item: NavItem) => ui.expandedMenus[item.label] ?? hasActiveChild(item)
const closeOnNavigate = () => ui.closeMobileDrawer()

const currentSection = computed(() => {
  for (const group of navigation) {
    if (group.items.some((item) => isActive(item) || hasActiveChild(item))) return group.label
  }
  return 'TỔNG QUAN'
})

const icons: Record<string, string> = {
  'Trang chủ': '⌂', 'Lộ trình HSK': '↗', 'Người mới bắt đầu': '☆', 'Bài học': '课',
  'Kỹ năng': '◌', 'Luyện tập': '✓', Nghe: '听', Đọc: '读', Viết: '写',
  'Nói - đối thoại': '说', 'Dịch Việt - Trung': '译', 'Từ vựng': '词', 'Chữ Hán': '字',
  'Danh sách chữ': '字', 'Thứ tự nét': '笔', 'Luyện viết': '写', 'Từ liên quan': '词',
  Pinyin: '拼', 'Thanh điệu': '声', 'Ngữ pháp': '语', 'Ôn tập': '↺', 'Câu làm sai': '!',
  'Nội dung cần ôn': '◌', 'Thi thử': '▤', 'Tiến độ học': '◒', 'Điểm yếu': '⌁',
  'Lịch sử học': '◷', 'Chuỗi ngày học': '✦', 'Truyện song ngữ': '文', 'Video học': '▶',
  'Tài liệu': '▤', 'Công cụ': '⌘', 'Hồ sơ': '◎', 'Cài đặt': '⚙', 'Trợ giúp': '?',
}

const iconFor = (label: string) => icons[label] ?? '•'
</script>

<template>
  <aside class="app-sidebar" :class="{ 'is-open': ui.mobileDrawerOpen }" aria-label="Điều hướng chính">
    <div class="app-sidebar__top">
      <RouterLink class="app-logo" to="/app" aria-label="VietAIS HSK 3.0" @click="closeOnNavigate">
        <span class="app-logo__mark"><img src="/assets/logoOnly-remove-bg-io.png" alt=""></span>
        <span class="app-logo__words"><strong>Viet<span>AIS</span></strong><small>HSK 3.0</small></span>
      </RouterLink>
      <button class="app-sidebar__close" type="button" aria-label="Đóng menu" @click="ui.closeMobileDrawer">×</button>
    </div>

    <div class="app-current-level">
      <span class="app-current-level__icon">3</span>
      <span><small>Đang học</small><strong>HSK 3</strong></span>
      <span class="app-current-level__chevron">⌄</span>
    </div>

    <nav class="app-nav" aria-label="Điều hướng chính">
      <section v-for="group in navigation" :key="group.label" class="app-nav__group" :class="{ 'is-current': currentSection === group.label }">
        <p class="app-nav__label">{{ group.label }}</p>
        <div v-for="item in group.items" :key="item.label" class="app-nav__branch" :class="{ 'is-expanded': item.children && isExpanded(item) }">
          <div v-if="item.children" class="app-nav__parent-row">
            <button class="app-nav__item app-nav__parent-button" type="button" :class="{ 'is-parent-active': hasActiveChild(item) }" :aria-expanded="isExpanded(item)" :aria-label="`Mở nhóm ${item.label}`" @click="ui.toggleMenu(item.label)">
              <span class="app-nav__icon" aria-hidden="true">{{ iconFor(item.label) }}</span><span>{{ item.label }}</span><span class="app-nav__chevron" aria-hidden="true">⌄</span>
            </button>
          </div>
          <RouterLink v-else-if="item.to" :to="item.to" class="app-nav__item" :class="{ 'is-active': isActive(item) }" @click="closeOnNavigate">
            <span class="app-nav__icon" aria-hidden="true">{{ iconFor(item.label) }}</span><span>{{ item.label }}</span>
          </RouterLink>
          <span v-else class="app-nav__item"><span class="app-nav__icon" aria-hidden="true">{{ iconFor(item.label) }}</span><span>{{ item.label }}</span></span>

          <div v-if="item.children" class="app-subnav" :hidden="!isExpanded(item)">
            <RouterLink v-for="child in item.children" :key="child.label" :to="child.to!" class="app-nav__item app-nav__item--child" :class="{ 'is-active': isActive(child) }" @click="closeOnNavigate">
              <span class="app-nav__icon" aria-hidden="true">{{ iconFor(child.label) }}</span><span>{{ child.label }}</span>
            </RouterLink>
          </div>
        </div>
      </section>
    </nav>

    <div class="app-sidebar__bottom">
      <RouterLink class="app-account" to="/app/profile" @click="closeOnNavigate">
        <span class="app-account__avatar">HV</span>
        <span><strong>Học viên VietAIS</strong><small>HSK 3 · Đang học</small></span>
        <span class="app-account__more">•••</span>
      </RouterLink>
    </div>
  </aside>
</template>
