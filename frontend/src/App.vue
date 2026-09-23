<script setup lang="ts">
import { RouterView, useRoute } from 'vue-router'
import AppHeader from './components/AppHeader.vue'
import AppSidebar from './components/AppSidebar.vue'
import { useUiStore } from './stores/ui'

const ui = useUiStore()
const route = useRoute()
</script>

<template>
  <RouterView v-if="route.path.startsWith('/admin')" />
  <div v-else class="app-page app-shell app-shell-pages">
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
