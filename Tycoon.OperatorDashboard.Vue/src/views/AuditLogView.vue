<script setup>
import { onMounted, ref } from 'vue'
import { getAuditLog } from '../api/auditLog'

const loading = ref(true)
const error = ref('')
const rows = ref([])
const page = ref(1)
const pageSize = 20
const total = ref(0)

function readItems(payload) {
  return Array.isArray(payload?.items) ? payload.items : []
}

function readTotal(payload, itemCount) {
  return Number.isFinite(payload?.total) ? payload.total : itemCount
}

async function loadAuditLog() {
  loading.value = true
  try {
    const payload = await getAuditLog(page.value, pageSize)
    const items = readItems(payload)
    rows.value = items
    total.value = readTotal(payload, items.length)
  } catch (err) {
    error.value = err?.message ?? 'Failed to load audit log.'
  } finally {
    loading.value = false
  }
}

function prevPage() {
  if (page.value <= 1) return
  page.value -= 1
  loadAuditLog()
}

function nextPage() {
  if (page.value * pageSize >= total.value) return
  page.value += 1
  loadAuditLog()
}

onMounted(loadAuditLog)
</script>

<template>
  <section>
    <h2>Audit Log (Wave A)</h2>
    <p v-if="loading">Loading audit log…</p>
    <p v-else-if="error" style="color:#ef4444">{{ error }}</p>
    <template v-else>
      <table v-if="rows.length > 0" style="width:100%;border-collapse:collapse;background:white">
        <thead>
          <tr>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">Timestamp</th>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">Admin</th>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">Operation</th>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">Status</th>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">IP</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="entry in rows" :key="entry.id ?? `${entry.timestamp}-${entry.operation}`">
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">{{ entry.timestamp || '—' }}</td>
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">{{ entry.adminEmail || '—' }}</td>
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">{{ entry.operation || '—' }}</td>
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">{{ entry.status || '—' }}</td>
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">{{ entry.ipAddress || '—' }}</td>
          </tr>
        </tbody>
      </table>
      <p v-else>No audit records found.</p>

      <div style="display:flex;gap:.5rem;align-items:center;margin-top:.75rem">
        <button @click="prevPage" :disabled="page <= 1">Prev</button>
        <span>Page {{ page }}</span>
        <button @click="nextPage" :disabled="page * pageSize >= total">Next</button>
      </div>
    </template>
  </section>
</template>
