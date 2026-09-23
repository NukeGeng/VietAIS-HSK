<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getJson, putJson } from '../services/api'
import type { IdentitySnapshot } from '../types'

const snapshot = ref<IdentitySnapshot | null>(null)
const displayName = ref('')
const timezone = ref('UTC')
const dailyMinutes = ref(20)
const preferredStudyTime = ref('')
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const notice = ref('')

onMounted(async () => {
  try {
    snapshot.value = await getJson<IdentitySnapshot>('/api/me')
    fillForm(snapshot.value)
  } catch { error.value = 'Chưa tải được hồ sơ. Bạn cần đăng nhập để xem và chỉnh sửa thông tin.' }
  finally { loading.value = false }
})

function fillForm(value: IdentitySnapshot) {
  displayName.value = value.profile.displayName ?? ''
  timezone.value = value.profile.timezone
  dailyMinutes.value = value.profile.studyPreferences.dailyMinutes
  preferredStudyTime.value = value.profile.studyPreferences.preferredStudyTime ?? ''
}

async function save() {
  saving.value = true; error.value = ''; notice.value = ''
  try {
    const profile = await putJson<IdentitySnapshot['profile']>('/api/me/profile', { displayName: displayName.value, avatarUrl: snapshot.value?.profile.avatarUrl ?? null, timezone: timezone.value, studyPreferences: { dailyMinutes: dailyMinutes.value, preferredStudyTime: preferredStudyTime.value || null } })
    if (snapshot.value) snapshot.value = { ...snapshot.value, profile }
    notice.value = 'Đã lưu hồ sơ.'
  } catch { error.value = 'Không thể lưu hồ sơ. Kiểm tra timezone và thời lượng học.' }
  finally { saving.value = false }
}
</script>

<template>
  <div v-if="loading" class="state-card">Đang tải hồ sơ…</div>
  <div v-else-if="error && !snapshot" class="state-card state-card-muted">{{ error }}</div>
  <template v-else>
    <form class="page-card profile-card" @submit.prevent="save">
      <div class="profile-meta"><div class="account-avatar">HV</div><div><strong>{{ snapshot?.account.userId }}</strong><span>{{ snapshot?.account.status }}</span></div></div>
      <label>Tên hiển thị<input v-model="displayName" maxlength="120" placeholder="Tên của bạn" /></label>
      <div class="profile-fields"><label>Múi giờ<input v-model="timezone" placeholder="Asia/Ho_Chi_Minh" /></label><label>Phút học mỗi ngày<input v-model.number="dailyMinutes" type="number" min="5" max="240" /></label></div>
      <label>Giờ học ưu tiên<input v-model="preferredStudyTime" type="time" /></label>
      <div class="profile-actions"><button class="page-button page-button--blue" type="submit" :disabled="saving">{{ saving ? 'Đang lưu…' : 'Lưu hồ sơ' }} →</button><span v-if="notice" class="inline-message">{{ notice }}</span></div>
      <p v-if="error" class="form-error">{{ error }}</p>
    </form>
  </template>
</template>
