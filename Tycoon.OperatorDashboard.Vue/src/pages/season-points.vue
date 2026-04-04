<script setup lang="ts">
import { seasonPointsService } from '@/lib/services/seasonPointsService'
import { handleApiError, applyErrorSideEffects } from '@/lib/apiErrors'
import type { ErrorHandlerResult } from '@/lib/apiErrors'
import type { SeasonPointTxnListItem, ApplySeasonPointsResult } from '@/lib/types/admin'

const tab = ref('history')
const error = ref<ErrorHandlerResult | null>(null)

// ─── History ─────────────────────────────────────────────────────────
const historyPlayerId = ref('')
const historySearchId = ref('')
const historyLoading = ref(false)
const historyItems = ref<SeasonPointTxnListItem[]>([])
const historyTotal = ref(0)
const historyPage = ref(1)

const historyHeaders = [
  { title: 'Date', key: 'createdAtUtc' },
  { title: 'Kind', key: 'kind' },
  { title: 'Delta', key: 'delta' },
  { title: 'Season', key: 'seasonId' },
  { title: 'Note', key: 'note' },
]

function searchHistory() {
  historySearchId.value = historyPlayerId.value
  historyPage.value = 1
  fetchHistory()
}

async function fetchHistory() {
  if (!historySearchId.value) return
  historyLoading.value = true
  error.value = null

  try {
    const res = await seasonPointsService.history(historySearchId.value, { page: historyPage.value, pageSize: 25 })

    historyItems.value = res.items
    historyTotal.value = res.total
  }
  catch (err) {
    const r = handleApiError(err)

    error.value = r
    applyErrorSideEffects(r)
  }
  finally {
    historyLoading.value = false
  }
}

// ─── Apply Points ────────────────────────────────────────────────────
const applyForm = ref({
  seasonId: '',
  playerId: '',
  kind: 'admin-adjust',
  delta: 0,
  note: '',
})

const applyLoading = ref(false)
const applyResult = ref<ApplySeasonPointsResult | null>(null)

async function submitApply() {
  applyLoading.value = true
  error.value = null
  applyResult.value = null

  try {
    const res = await seasonPointsService.applyPoints({
      eventId: crypto.randomUUID(),
      seasonId: applyForm.value.seasonId,
      playerId: applyForm.value.playerId,
      kind: applyForm.value.kind,
      delta: applyForm.value.delta,
      note: applyForm.value.note || undefined,
    })

    applyResult.value = res
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    applyLoading.value = false
  }
}
</script>

<template>
  <VCard>
    <VCardTitle class="pa-4">
      Season Points
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

    <VTabs v-model="tab">
      <VTab value="history">
        History
      </VTab>
      <VTab value="apply">
        Apply Points
      </VTab>
    </VTabs>

    <VCardText>
      <VTabsWindow v-model="tab">
        <VTabsWindowItem value="history">
          <div class="d-flex align-center mb-4 gap-4">
            <VTextField
              v-model="historyPlayerId"
              label="Player ID"
              density="compact"
              variant="outlined"
              hide-details
              style="max-width: 400px"
              @keyup.enter="searchHistory"
            />
            <VBtn @click="searchHistory">
              Search
            </VBtn>
          </div>

          <VDataTableServer
            v-if="historySearchId"
            :headers="historyHeaders"
            :items="historyItems"
            :items-length="historyTotal"
            :loading="historyLoading"
            :items-per-page="25"
            :page="historyPage"
            @update:options="(o: { page: number }) => { historyPage = o.page; fetchHistory() }"
          >
            <template #item.delta="{ item }">
              <VChip
                :color="item.delta >= 0 ? 'success' : 'error'"
                size="small"
              >
                {{ item.delta >= 0 ? '+' : '' }}{{ item.delta }}
              </VChip>
            </template>

            <template #item.seasonId="{ item }">
              <span class="text-caption">{{ item.seasonId.slice(0, 8) }}...</span>
            </template>

            <template #item.createdAtUtc="{ item }">
              {{ new Date(item.createdAtUtc).toLocaleString() }}
            </template>
          </VDataTableServer>
        </VTabsWindowItem>

        <VTabsWindowItem value="apply">
          <VRow>
            <VCol
              cols="12"
              md="6"
            >
              <VTextField
                v-model="applyForm.seasonId"
                label="Season ID"
              />
            </VCol>
            <VCol
              cols="12"
              md="6"
            >
              <VTextField
                v-model="applyForm.playerId"
                label="Player ID"
              />
            </VCol>
            <VCol
              cols="12"
              md="4"
            >
              <VTextField
                v-model="applyForm.kind"
                label="Kind"
              />
            </VCol>
            <VCol
              cols="12"
              md="4"
            >
              <VTextField
                v-model.number="applyForm.delta"
                label="Delta"
                type="number"
              />
            </VCol>
            <VCol
              cols="12"
              md="4"
            >
              <VTextField
                v-model="applyForm.note"
                label="Note (optional)"
              />
            </VCol>
            <VCol cols="12">
              <VBtn
                :loading="applyLoading"
                :disabled="!applyForm.seasonId || !applyForm.playerId"
                @click="submitApply"
              >
                Apply Points
              </VBtn>
            </VCol>
          </VRow>

          <VAlert
            v-if="applyResult"
            type="success"
            class="mt-4"
          >
            {{ applyResult.status }}: New rank points = {{ applyResult.newRankPoints }}
          </VAlert>
        </VTabsWindowItem>
      </VTabsWindow>
    </VCardText>
  </VCard>
</template>
