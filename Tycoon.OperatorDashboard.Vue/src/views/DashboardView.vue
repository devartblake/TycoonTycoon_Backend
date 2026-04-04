<script setup>
import { onMounted, ref } from 'vue'
import { getDashboardOverview } from '../api/dashboard'

const loading = ref(true)
const error = ref('')
const overview = ref(null)

onMounted(async () => {
  try {
    overview.value = await getDashboardOverview()
  } catch (err) {
    error.value = err?.message ?? 'Failed to load dashboard overview.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <section>
    <h2>Dashboard (Wave A)</h2>
    <p v-if="loading">Loading dashboard overview…</p>
    <p v-else-if="error" style="color:#ef4444">{{ error }}</p>
    <pre v-else>{{ JSON.stringify(overview, null, 2) }}</pre>
  </section>
</template>
