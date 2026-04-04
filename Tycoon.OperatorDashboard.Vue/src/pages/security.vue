<script setup lang="ts">
import { auditService } from '@/lib/services/auditService'
import { handleApiError, applyErrorSideEffects } from '@/lib/apiErrors'
import type { ErrorHandlerResult } from '@/lib/apiErrors'
import type { NotificationHistoryItem } from '@/lib/types/admin'

const error = ref<ErrorHandlerResult | null>(null)
const loading = ref(false)
const items = ref<NotificationHistoryItem[]>([])
const totalItems = ref(0)
const page = ref(1)

const filterFrom = ref('')
const filterTo = ref('')
const filterStatus = ref('')

const headers = [
  { title: 'Time', key: 'createdAt' },
  { title: 'Title', key: 'title' },
  { title: 'Status', key: 'status' },
  { title: 'Channel', key: 'channelKey' },
]

async function fetchEvents() {
  loading.value = true
  error.value = null

  try {
    const res = await auditService.securityEvents({
      from: filterFrom.value || undefined,
      to: filterTo.value || undefined,
      status: filterStatus.value || undefined,
      page: page.value,
      pageSize: 25,
    })

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

// Detail drawer
const selectedEvent = ref<NotificationHistoryItem | null>(null)
const drawer = ref(false)

function openDetail(item: NotificationHistoryItem) {
  selectedEvent.value = item
  drawer.value = true
}

onMounted(fetchEvents)
</script>

<template>
  <VCard>
    <VCardTitle class="d-flex align-center pa-4">
      <span>Security Audit</span>
      <VSpacer />
      <VTextField
        v-model="filterFrom"
        label="From"
        type="datetime-local"
        density="compact"
        variant="outlined"
        hide-details
        style="max-width: 200px"
        class="me-2"
      />
      <VTextField
        v-model="filterTo"
        label="To"
        type="datetime-local"
        density="compact"
        variant="outlined"
        hide-details
        style="max-width: 200px"
        class="me-2"
      />
      <VSelect
        v-model="filterStatus"
        :items="[{ title: 'All', value: '' }, { title: '401', value: '401' }, { title: '403', value: '403' }, { title: '429', value: '429' }]"
        density="compact"
        variant="outlined"
        hide-details
        style="max-width: 120px"
        class="me-2"
      />
      <VBtn
        size="small"
        @click="fetchEvents"
      >
        Filter
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
      :items-per-page="25"
      :page="page"
      @update:options="(o: { page: number }) => { page = o.page; fetchEvents() }"
      @click:row="(_: unknown, row: { item: NotificationHistoryItem }) => openDetail(row.item)"
    >
      <template #item.createdAt="{ item }">
        {{ new Date(item.createdAt).toLocaleString() }}
      </template>

      <template #item.status="{ item }">
        <VChip
          :color="item.status === '401' || item.status === '403' ? 'error' : item.status === '429' ? 'warning' : 'default'"
          size="small"
        >
          {{ item.status }}
        </VChip>
      </template>
    </VDataTableServer>
  </VCard>

  <!-- Detail Drawer -->
  <VNavigationDrawer
    v-model="drawer"
    temporary
    location="right"
    width="400"
  >
    <VCardTitle class="pa-4">
      Event Detail
    </VCardTitle>
    <VDivider />
    <VCardText v-if="selectedEvent">
      <div class="mb-2">
        <strong>ID:</strong> {{ selectedEvent.id }}
      </div>
      <div class="mb-2">
        <strong>Title:</strong> {{ selectedEvent.title }}
      </div>
      <div class="mb-2">
        <strong>Channel:</strong> {{ selectedEvent.channelKey }}
      </div>
      <div class="mb-2">
        <strong>Status:</strong> {{ selectedEvent.status }}
      </div>
      <div class="mb-2">
        <strong>Time:</strong> {{ new Date(selectedEvent.createdAt).toLocaleString() }}
      </div>
      <div
        v-if="selectedEvent.metadata"
        class="mt-4"
      >
        <strong>Metadata:</strong>
        <pre class="text-caption mt-1 pa-2 bg-grey-lighten-4 rounded">{{ JSON.stringify(selectedEvent.metadata, null, 2) }}</pre>
      </div>
    </VCardText>
  </VNavigationDrawer>
</template>
