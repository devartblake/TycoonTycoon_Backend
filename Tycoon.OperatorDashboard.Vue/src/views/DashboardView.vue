<script setup>
import { onMounted, ref } from 'vue'
import { getDashboardOverview } from '../api/dashboard'

const loading = ref(true)
const error = ref('')
const overview = ref({})

function metricValue(source, key) {
  const raw = source?.[key]
  if (typeof raw === 'number') return raw.toLocaleString()
  if (typeof raw === 'string' && raw.trim().length > 0) return raw
  return '—'
}

onMounted(async () => {
  try {
    overview.value = (await getDashboardOverview()) ?? {}
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
    <div v-else style="display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:.75rem">
      <article style="background:white;border:1px solid #e2e8f0;border-radius:8px;padding:.75rem">
        <p style="margin:0;color:#64748b;font-size:.8rem">Active Users</p>
        <h3 style="margin:.35rem 0 0">{{ metricValue(overview, 'activeUsers') }}</h3>
      </article>
      <article style="background:white;border:1px solid #e2e8f0;border-radius:8px;padding:.75rem">
        <p style="margin:0;color:#64748b;font-size:.8rem">Matches (24h)</p>
        <h3 style="margin:.35rem 0 0">{{ metricValue(overview, 'matchesLast24h') }}</h3>
      </article>
      <article style="background:white;border:1px solid #e2e8f0;border-radius:8px;padding:.75rem">
        <p style="margin:0;color:#64748b;font-size:.8rem">Revenue (24h)</p>
        <h3 style="margin:.35rem 0 0">{{ metricValue(overview, 'revenueLast24h') }}</h3>
      </article>
      <article style="background:white;border:1px solid #e2e8f0;border-radius:8px;padding:.75rem">
        <p style="margin:0;color:#64748b;font-size:.8rem">Incidents</p>
        <h3 style="margin:.35rem 0 0">{{ metricValue(overview, 'openIncidents') }}</h3>
      </article>
    </div>
  </section>
</template>
