<script setup>
import { onMounted, ref } from 'vue'
import { getUsers } from '../api/users'

const loading = ref(true)
const error = ref('')
const users = ref([])
const page = ref(1)
const pageSize = 20
const total = ref(0)

function readItems(payload) {
  return Array.isArray(payload?.items) ? payload.items : []
}

function readTotal(payload, itemCount) {
  return Number.isFinite(payload?.total) ? payload.total : itemCount
}

async function loadUsers() {
  loading.value = true
  try {
    const payload = await getUsers(page.value, pageSize)
    const items = readItems(payload)
    users.value = items
    total.value = readTotal(payload, items.length)
  } catch (err) {
    error.value = err?.message ?? 'Failed to load users.'
  } finally {
    loading.value = false
  }
}

function prevPage() {
  if (page.value <= 1) return
  page.value -= 1
  loadUsers()
}

function nextPage() {
  if (page.value * pageSize >= total.value) return
  page.value += 1
  loadUsers()
}

onMounted(loadUsers)
</script>

<template>
  <section>
    <h2>Users (Wave A)</h2>
    <p v-if="loading">Loading users…</p>
    <p v-else-if="error" style="color:#ef4444">{{ error }}</p>
    <template v-else>
      <table v-if="users.length > 0" style="width:100%;border-collapse:collapse;background:white">
        <thead>
          <tr>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">Id</th>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">Handle</th>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">Email</th>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">Banned</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="user in users" :key="user.id">
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">{{ user.id }}</td>
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">{{ user.handle || '—' }}</td>
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">{{ user.email || '—' }}</td>
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">{{ user.isBanned ? 'Yes' : 'No' }}</td>
          </tr>
        </tbody>
      </table>
      <p v-else>No users found.</p>

      <div style="display:flex;gap:.5rem;align-items:center;margin-top:.75rem">
        <button @click="prevPage" :disabled="page <= 1">Prev</button>
        <span>Page {{ page }}</span>
        <button @click="nextPage" :disabled="page * pageSize >= total">Next</button>
      </div>
    </template>
  </section>
</template>
