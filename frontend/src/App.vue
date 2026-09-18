<script setup lang="ts">
import { RouterView } from 'vue-router'
import AppHeader from './components/AppHeader.vue'
import AppSidebar from './components/AppSidebar.vue'
import { useUiStore } from './stores/ui'

const ui = useUiStore()
</script>

<template>
  <div class="app-page app-shell">
    <div v-if="ui.mobileDrawerOpen" class="app-scrim is-visible" @click="ui.closeMobileDrawer" />
    <AppSidebar />
    <main class="app-main">
      <AppHeader />
      <div class="app-content">
        <RouterView />
      </div>
    </main>
  </div>
</template>
