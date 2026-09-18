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
</script>

<template>
  <aside class="app-sidebar" :class="{ 'is-open': ui.mobileDrawerOpen }">
    <div class="sidebar-brand">
      <div class="brand-mark" aria-hidden="true">⌁</div>
      <div>
        <strong>Viet<span>AIS</span></strong>
        <small>HSK 3.0</small>
      </div>
      <button class="icon-button sidebar-close" type="button" aria-label="Đóng menu" @click="ui.closeMobileDrawer">×</button>
    </div>

    <div class="sidebar-context">
      <span class="context-dot" />
      <span>Đang học</span>
      <strong>HSK 3</strong>
    </div>

    <nav class="sidebar-nav" aria-label="Điều hướng chính">
      <section v-for="group in navigation" :key="group.label" class="nav-group" :class="{ 'is-current': currentSection === group.label }">
        <p class="nav-group-label">{{ group.label }}</p>
        <div class="nav-group-items">
          <div v-for="item in group.items" :key="item.label" class="nav-item-wrap">
            <div class="nav-item-row">
              <RouterLink v-if="item.to" :to="item.to" class="nav-item" :class="{ 'is-active': isActive(item) || hasActiveChild(item) }" @click="closeOnNavigate">
                <span class="nav-item-icon" aria-hidden="true" />
                <span>{{ item.label }}</span>
              </RouterLink>
              <span v-else class="nav-item nav-item-static"><span class="nav-item-icon" aria-hidden="true" /><span>{{ item.label }}</span></span>
              <button v-if="item.children" class="submenu-toggle" type="button" :aria-expanded="isExpanded(item)" :aria-label="`Mở ${item.label}`" @click.stop="ui.toggleMenu(item.label)">
                <span :class="{ 'is-rotated': isExpanded(item) }">⌄</span>
              </button>
            </div>
            <div v-if="item.children && isExpanded(item)" class="nav-submenu">
              <RouterLink v-for="child in item.children" :key="child.label" :to="child.to!" class="nav-subitem" :class="{ 'is-active': isActive(child) }" @click="closeOnNavigate">
                <span class="subitem-dot" />
                <span>{{ child.label }}</span>
              </RouterLink>
            </div>
          </div>
        </div>
      </section>
    </nav>

    <div class="sidebar-account">
      <div class="account-avatar">HV</div>
      <div>
        <strong>Học viên VietAIS</strong>
        <span>HSK 3 · Đang học</span>
      </div>
    </div>
  </aside>
</template>
