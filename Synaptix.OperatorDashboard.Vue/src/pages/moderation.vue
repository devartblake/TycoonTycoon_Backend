<script setup lang="ts">
import { moderationService } from '@/lib/services/moderationService'
import { handleApiError, applyErrorSideEffects } from '@/lib/apiErrors'
import type { ErrorHandlerResult } from '@/lib/apiErrors'
import type { ModerationLogItem } from '@/lib/types/admin'

const router = useRouter()
const error = ref<ErrorHandlerResult | null>(null)
const loading = ref(false)
const items = ref<ModerationLogItem[]>([])
const totalItems = ref(0)
const page = ref(1)

const statusLabels: Record<number, string> = { 0: 'Normal', 1: 'Suspected', 2: 'Restricted', 3: 'Banned' }
const statusColors: Record<number, string> = { 0: 'success', 1: 'warning', 2: 'error', 3: 'error' }

const headers = [
  { title: 'Player', key: 'playerId' },
  { title: 'Status', key: 'newStatus' },
  { title: 'Reason', key: 'reason' },
  { title: 'Admin', key: 'setByAdmin' },
  { title: 'Date', key: 'createdAtUtc' },
]

async function fetchLogs() {
  loading.value = true
  error.value = null

  try {
    const res = await moderationService.logs({ page: page.value, pageSize: 25 })

    items.value = res.items
    totalItems.value = res.totalItems
  }
  catch (err) {
    const r = handleApiError(err)

    error.value = r
    applyErrorSideEffects(r)
  }
  finally {
    loading.value = false
  }
}

function onRowClick(_: unknown, row: { item: ModerationLogItem }) {
  router.push(`/users/${row.item.playerId}`)
}

onMounted(fetchLogs)
</script>

<template>
  <VCard>
    <VCardTitle class="pa-4">
      Moderation Log
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
      :items-per-page="25"
      :page="page"
      class="cursor-pointer"
      @update:options="(o: { page: number }) => { page = o.page; fetchLogs() }"
      @click:row="onRowClick"
    >
      <template #item.playerId="{ item }">
        <span class="text-primary font-weight-medium">{{ item.playerId.slice(0, 8) }}...</span>
      </template>

      <template #item.newStatus="{ item }">
        <VChip
          :color="statusColors[item.newStatus] ?? 'default'"
          size="small"
        >
          {{ statusLabels[item.newStatus] ?? item.newStatus }}
        </VChip>
      </template>

      <template #item.reason="{ item }">
        {{ item.reason ?? '-' }}
      </template>

      <template #item.setByAdmin="{ item }">
        {{ item.setByAdmin ?? '-' }}
      </template>

      <template #item.createdAtUtc="{ item }">
        {{ new Date(item.createdAtUtc).toLocaleDateString() }}
      </template>
    </VDataTableServer>
  </VCard>
</template>
