<script setup>
import { onMounted, ref } from 'vue'
import { banUser, getUsers, unbanUser } from '../api/users'

const loading = ref(true)
const error = ref('')
const users = ref([])
const page = ref(1)
const pageSize = 20
const total = ref(0)
const query = ref('')
const isBanned = ref('')
const actionMessage = ref('')
const banReason = ref('Policy violation')
const actionBusyId = ref('')

function readItems(payload) {
  return Array.isArray(payload?.items) ? payload.items : []
}

function readTotal(payload, itemCount) {
  return Number.isFinite(payload?.total) ? payload.total : itemCount
}

async function loadUsers() {
  loading.value = true
  actionMessage.value = ''
  try {
    const payload = await getUsers({
      page: page.value,
      pageSize,
      query: query.value.trim(),
      isBanned: isBanned.value
    })
    const items = readItems(payload)
    users.value = items
    total.value = readTotal(payload, items.length)
  } catch (err) {
    error.value = err?.message ?? 'Failed to load users.'
  } finally {
    loading.value = false
  }
}

async function toggleBan(user) {
  if (!user?.id || actionBusyId.value) return

  actionBusyId.value = user.id
  actionMessage.value = ''

  try {
    if (user.isBanned) {
      await unbanUser(user.id)
      actionMessage.value = `Unbanned ${user.handle || user.id}.`
    } else {
      await banUser(user.id, banReason.value || 'Policy violation')
      actionMessage.value = `Banned ${user.handle || user.id}.`
    }
    await loadUsers()
  } catch (err) {
    actionMessage.value = err?.message ?? 'User action failed.'
  } finally {
    actionBusyId.value = ''
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

function applyFilters() {
  page.value = 1
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
      <div style="display:flex;gap:.5rem;align-items:center;margin-bottom:.75rem;flex-wrap:wrap">
        <input v-model="query" placeholder="Search handle/email…" style="padding:.35rem .5rem;border:1px solid #cbd5e1;border-radius:6px" />
        <select v-model="isBanned" style="padding:.35rem .5rem;border:1px solid #cbd5e1;border-radius:6px">
          <option value="">All</option>
          <option value="true">Banned</option>
          <option value="false">Not banned</option>
        </select>
        <input v-model="banReason" placeholder="Ban reason…" style="padding:.35rem .5rem;border:1px solid #cbd5e1;border-radius:6px;min-width:220px" />
        <button @click="applyFilters">Apply</button>
      </div>

      <table v-if="users.length > 0" style="width:100%;border-collapse:collapse;background:white">
        <thead>
          <tr>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">Id</th>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">Handle</th>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">Email</th>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">Banned</th>
            <th style="text-align:left;padding:.5rem;border-bottom:1px solid #e2e8f0">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="user in users" :key="user.id">
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">{{ user.id }}</td>
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">{{ user.handle || '—' }}</td>
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">{{ user.email || '—' }}</td>
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">{{ user.isBanned ? 'Yes' : 'No' }}</td>
            <td style="padding:.5rem;border-bottom:1px solid #f1f5f9">
              <button @click="toggleBan(user)" :disabled="actionBusyId === user.id">
                {{ actionBusyId === user.id ? 'Working…' : user.isBanned ? 'Unban' : 'Ban' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
      <p v-else>No users found.</p>

      <div style="display:flex;gap:.5rem;align-items:center;margin-top:.75rem">
        <button @click="prevPage" :disabled="page <= 1">Prev</button>
        <span>Page {{ page }}</span>
        <button @click="nextPage" :disabled="page * pageSize >= total">Next</button>
      </div>

      <p v-if="actionMessage" style="margin-top:.5rem;color:#0f766e">{{ actionMessage }}</p>
    </template>
  </section>
</template>
