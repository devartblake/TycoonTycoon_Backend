<script setup>
import { onMounted, ref } from 'vue'
import { getAuditLog } from '../api/auditLog'

const loading = ref(true)
const error = ref('')
const auditLog = ref(null)

onMounted(async () => {
  try {
    auditLog.value = await getAuditLog()
  } catch (err) {
    error.value = err?.message ?? 'Failed to load audit log.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <section>
    <h2>Audit Log (Wave A)</h2>
    <p v-if="loading">Loading audit log…</p>
    <p v-else-if="error" style="color:#ef4444">{{ error }}</p>
    <pre v-else>{{ JSON.stringify(auditLog, null, 2) }}</pre>
  </section>
</template>
