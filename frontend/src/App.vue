<script setup lang="ts">
import { RouterView } from 'vue-router'
import AppHeader from './components/AppHeader.vue'
import AppSidebar from './components/AppSidebar.vue'
import { useUiStore } from './stores/ui'

const ui = useUiStore()
</script>

<template>
  <div class="app-shell">
    <div v-if="ui.mobileDrawerOpen" class="mobile-backdrop" @click="ui.closeMobileDrawer" />
    <AppSidebar />
    <div class="app-stage">
      <AppHeader />
      <main class="page-container">
        <RouterView />
      </main>
    </div>
  </div>
</template>
