<script setup lang="ts">
import { antiCheatService } from '@/lib/services/antiCheatService'
import { handleApiError, applyErrorSideEffects } from '@/lib/apiErrors'
import type { ErrorHandlerResult } from '@/lib/apiErrors'
import type { AntiCheatFlag, AntiCheatSummary } from '@/lib/types/admin'

const error = ref<ErrorHandlerResult | null>(null)
const loading = ref(false)
const items = ref<AntiCheatFlag[]>([])
const totalItems = ref(0)
const page = ref(1)
const unreviewedOnly = ref(true)
const severityFilter = ref<number | ''>('')

const summary = ref<AntiCheatSummary | null>(null)
const summaryLoading = ref(false)

const headers = [
  { title: 'Rule', key: 'ruleKey' },
  { title: 'Severity', key: 'severity' },
  { title: 'Player', key: 'playerId' },
  { title: 'Message', key: 'message' },
  { title: 'Date', key: 'createdAtUtc' },
  { title: 'Actions', key: 'actions', sortable: false },
]

async function fetchFlags() {
  loading.value = true
  error.value = null

  try {
    const res = await antiCheatService.flags({
      unreviewedOnly: unreviewedOnly.value || undefined,
      severity: severityFilter.value !== '' ? severityFilter.value : undefined,
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

async function fetchSummary() {
  summaryLoading.value = true

  try {
    summary.value = await antiCheatService.summary(24)
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    summaryLoading.value = false
  }
}

async function reviewFlag(flag: AntiCheatFlag) {
  try {
    await antiCheatService.reviewFlag(flag.id, { reviewedBy: 'admin' })
    fetchFlags()
  }
  catch (err) {
    error.value = handleApiError(err)
  }
}

const severityColors: Record<number, string> = {
  0: 'info',
  1: 'warning',
  2: 'error',
}

onMounted(() => {
  fetchFlags()
  fetchSummary()
})
</script>

<template>
  <div>
    <!-- Summary Cards -->
    <VRow
      v-if="summary"
      class="mb-4"
    >
      <VCol
        cols="6"
        md="3"
      >
        <VCard>
          <VCardText class="text-center">
            <div class="text-h4">
              {{ summary.totalFlags }}
            </div>
            <div class="text-caption">
              Total Flags (24h)
            </div>
          </VCardText>
        </VCard>
      </VCol>
      <VCol
        cols="6"
        md="3"
      >
        <VCard color="error">
          <VCardText class="text-center">
            <div class="text-h4">
              {{ summary.severeFlags }}
            </div>
            <div class="text-caption">
              Severe
            </div>
          </VCardText>
        </VCard>
      </VCol>
      <VCol
        cols="6"
        md="3"
      >
        <VCard color="warning">
          <VCardText class="text-center">
            <div class="text-h4">
              {{ summary.warningFlags }}
            </div>
            <div class="text-caption">
              Warning
            </div>
          </VCardText>
        </VCard>
      </VCol>
      <VCol
        cols="6"
        md="3"
      >
        <VCard color="info">
          <VCardText class="text-center">
            <div class="text-h4">
              {{ summary.infoFlags }}
            </div>
            <div class="text-caption">
              Info
            </div>
          </VCardText>
        </VCard>
      </VCol>
    </VRow>

    <VCard>
      <VCardTitle class="d-flex align-center pa-4">
        <span>Anti-Cheat Flags</span>
        <VSpacer />
        <VSwitch
          v-model="unreviewedOnly"
          label="Unreviewed only"
          hide-details
          density="compact"
          class="me-4"
          @update:model-value="fetchFlags"
        />
        <VSelect
          v-model="severityFilter"
          :items="[{ title: 'All', value: '' }, { title: 'Info', value: 0 }, { title: 'Warning', value: 1 }, { title: 'Severe', value: 2 }]"
          density="compact"
          variant="outlined"
          hide-details
          style="max-width: 150px"
          @update:model-value="fetchFlags"
        />
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
        @update:options="(o: { page: number }) => { page = o.page; fetchFlags() }"
      >
        <template #item.severity="{ item }">
          <VChip
            :color="severityColors[item.severity] ?? 'default'"
            size="small"
          >
            {{ item.severity === 2 ? 'Severe' : item.severity === 1 ? 'Warning' : 'Info' }}
          </VChip>
        </template>

        <template #item.playerId="{ item }">
          {{ item.playerId?.slice(0, 8) ?? 'N/A' }}...
        </template>

        <template #item.createdAtUtc="{ item }">
          {{ new Date(item.createdAtUtc).toLocaleString() }}
        </template>

        <template #item.actions="{ item }">
          <VBtn
            v-if="!item.reviewedAtUtc"
            size="x-small"
            color="primary"
            @click="reviewFlag(item)"
          >
            Review
          </VBtn>
          <VChip
            v-else
            size="small"
            color="success"
          >
            Reviewed
          </VChip>
        </template>
      </VDataTableServer>
    </VCard>
  </div>
</template>
