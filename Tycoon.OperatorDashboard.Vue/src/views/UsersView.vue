<script setup>
import { onMounted, ref } from 'vue'
import { getUsers } from '../api/users'

const loading = ref(true)
const error = ref('')
const users = ref(null)

onMounted(async () => {
  try {
    users.value = await getUsers()
  } catch (err) {
    error.value = err?.message ?? 'Failed to load users.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <section>
    <h2>Users (Wave A)</h2>
    <p v-if="loading">Loading users…</p>
    <p v-else-if="error" style="color:#ef4444">{{ error }}</p>
    <pre v-else>{{ JSON.stringify(users, null, 2) }}</pre>
  </section>
</template>
