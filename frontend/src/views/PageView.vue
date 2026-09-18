<script setup lang="ts">
defineProps<{ title: string; eyebrow: string; description: string }>()
</script>

<template>
  <section class="page-card page-card--soft page-feature-intro">
    <p class="page-kicker">{{ eyebrow }}</p>
    <h1>{{ title }}</h1>
    <p class="page-copy">{{ description }}</p>
  </section>
  <section class="page-card empty-page-card">
    <div class="empty-icon">+</div>
    <div>
      <h2>Trang đang được chuẩn bị</h2>
      <p>Nội dung sẽ được nối vào module backend tương ứng theo tài liệu hệ thống.</p>
    </div>
  </section>
</template>
