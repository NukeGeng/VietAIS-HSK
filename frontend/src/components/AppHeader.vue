<script setup lang="ts">
import { RouterLink, useRoute } from 'vue-router'
import { useUiStore } from '../stores/ui'

const route = useRoute()
const ui = useUiStore()
</script>

<template>
  <header class="app-topbar">
    <button class="app-menu-button" type="button" aria-label="Mở menu" @click="ui.mobileDrawerOpen = true"><span>☰</span></button>
    <div class="app-topbar__title"><strong>{{ route.meta.title ?? 'Tổng quan' }}</strong></div>
    <div class="app-topbar__actions">
      <button class="app-icon-button" type="button" aria-label="Thông báo">♢<span class="app-notification-dot" /></button>
      <RouterLink class="app-avatar-button" to="/app/profile" aria-label="Mở tài khoản">HV</RouterLink>
    </div>
  </header>
</template>
