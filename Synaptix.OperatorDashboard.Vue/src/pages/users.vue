<script setup lang="ts">
import { userService } from '@/lib/services/userService'
import { handleApiError, applyErrorSideEffects } from '@/lib/apiErrors'
import type { ErrorHandlerResult } from '@/lib/apiErrors'
import type { AdminUserListItem } from '@/lib/types/admin'

const loading = ref(false)
const error = ref<ErrorHandlerResult | null>(null)
const items = ref<AdminUserListItem[]>([])
const totalItems = ref(0)
const page = ref(1)
const pageSize = ref(25)
const search = ref('')
const bannedFilter = ref<string>('')

const headers = [
  { title: 'Username', key: 'username' },
  { title: 'Email', key: 'email' },
  { title: 'Role', key: 'role' },
  { title: 'Status', key: 'isBanned' },
  { title: 'Games', key: 'totalGamesPlayed' },
  { title: 'Win %', key: 'winRate' },
  { title: 'Joined', key: 'createdAt' },
]

async function fetchUsers() {
  loading.value = true
  error.value = null

  try {
    const res = await userService.list({
      q: search.value || undefined,
      isBanned: bannedFilter.value === 'banned' ? true : bannedFilter.value === 'active' ? false : undefined,
      page: page.value,
      pageSize: pageSize.value,
    })

    items.value = res.items
    totalItems.value = res.totalItems
  }
  catch (err) {
    const result = handleApiError(err)

    error.value = result
    applyErrorSideEffects(result)
  }
  finally {
    loading.value = false
  }
}

// Ban dialog
const banDialog = ref(false)
const banTarget = ref<AdminUserListItem | null>(null)
const banReason = ref('')
const banLoading = ref(false)

function openBan(user: AdminUserListItem) {
  banTarget.value = user
  banReason.value = ''
  banDialog.value = true
}

async function confirmBan() {
  if (!banTarget.value) return
  banLoading.value = true

  try {
    await userService.ban(banTarget.value.id, { reason: banReason.value })
    banDialog.value = false
    fetchUsers()
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    banLoading.value = false
  }
}

async function unbanUser(user: AdminUserListItem) {
  try {
    await userService.unban(user.id)
    fetchUsers()
  }
  catch (err) {
    error.value = handleApiError(err)
  }
}

function onPageUpdate(options: { page: number; itemsPerPage: number }) {
  page.value = options.page
  pageSize.value = options.itemsPerPage
  fetchUsers()
}

onMounted(fetchUsers)
</script>

<template>
  <VCard>
    <VCardTitle class="d-flex align-center pa-4">
      <span>Users</span>
      <VSpacer />
      <VTextField
        v-model="search"
        density="compact"
        placeholder="Search users..."
        prepend-inner-icon="ri-search-line"
        variant="outlined"
        hide-details
        style="max-width: 300px"
        class="me-4"
        @keyup.enter="fetchUsers"
      />
      <VSelect
        v-model="bannedFilter"
        :items="[{ title: 'All', value: '' }, { title: 'Active', value: 'active' }, { title: 'Banned', value: 'banned' }]"
        density="compact"
        variant="outlined"
        hide-details
        style="max-width: 150px"
        class="me-4"
        @update:model-value="fetchUsers"
      />
      <VBtn
        size="small"
        @click="fetchUsers"
      >
        Search
      </VBtn>
    </VCardTitle>

    <VAlert
      v-if="error"
      :type="error.severity"
      closable
      class="mx-4 mb-2"
      @click:close="error = null"
    >
      {{ error.message }}
    </VAlert>

    <VDataTableServer
      :headers="headers"
      :items="items"
      :items-length="totalItems"
      :loading="loading"
      :items-per-page="pageSize"
      :page="page"
      @update:options="onPageUpdate"
    >
      <template #item.isBanned="{ item }">
        <VChip
          :color="item.isBanned ? 'error' : 'success'"
          size="small"
        >
          {{ item.isBanned ? 'Banned' : 'Active' }}
        </VChip>
      </template>

      <template #item.winRate="{ item }">
        {{ (item.winRate * 100).toFixed(1) }}%
      </template>

      <template #item.createdAt="{ item }">
        {{ new Date(item.createdAt).toLocaleDateString() }}
      </template>

      <template #item.username="{ item }">
        <div class="d-flex align-center">
          <span>{{ item.username }}</span>
          <VSpacer />
          <VBtn
            v-if="!item.isBanned"
            size="x-small"
            color="error"
            variant="text"
            @click.stop="openBan(item)"
          >
            Ban
          </VBtn>
          <VBtn
            v-else
            size="x-small"
            color="success"
            variant="text"
            @click.stop="unbanUser(item)"
          >
            Unban
          </VBtn>
        </div>
      </template>
    </VDataTableServer>
  </VCard>

  <!-- Ban Dialog -->
  <VDialog
    v-model="banDialog"
    max-width="500"
  >
    <VCard>
      <VCardTitle>Ban User: {{ banTarget?.username }}</VCardTitle>
      <VCardText>
        <VTextField
          v-model="banReason"
          label="Reason"
          :disabled="banLoading"
        />
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn
          :disabled="banLoading"
          @click="banDialog = false"
        >
          Cancel
        </VBtn>
        <VBtn
          color="error"
          :loading="banLoading"
          :disabled="!banReason"
          @click="confirmBan"
        >
          Confirm Ban
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>
</template>
