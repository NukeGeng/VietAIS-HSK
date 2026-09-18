<script setup lang="ts">
import { useRoute } from 'vue-router'
import { useUiStore } from '../stores/ui'

const route = useRoute()
const ui = useUiStore()
</script>

<template>
  <header class="app-header">
    <button class="icon-button mobile-menu-button" type="button" aria-label="Mở menu" @click="ui.mobileDrawerOpen = true">
      <span class="menu-lines" />
    </button>
    <div class="breadcrumb">
      <span class="breadcrumb-brand">VietAIS</span>
      <span class="breadcrumb-separator">/</span>
      <span>{{ route.meta.title ?? 'Trang chủ' }}</span>
    </div>
    <div class="header-actions">
      <button class="context-chip" type="button">HSK 3 <span class="chip-chevron">⌄</span></button>
      <button class="account-button" type="button" aria-label="Mở tài khoản">HV</button>
    </div>
  </header>
</template>
