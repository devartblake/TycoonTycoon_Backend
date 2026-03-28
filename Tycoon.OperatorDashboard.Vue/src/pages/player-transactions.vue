<script setup lang="ts">
import { playerTransactionService } from '@/lib/services/playerTransactionService'
import { handleApiError, applyErrorSideEffects } from '@/lib/apiErrors'
import type { ErrorHandlerResult } from '@/lib/apiErrors'
import type { PlayerTransactionListItem, PlayerTransactionDetail } from '@/lib/types/admin'

const tab = ref('history')
const error = ref<ErrorHandlerResult | null>(null)

// ─── History ─────────────────────────────────────────────────────────
const filterPlayerId = ref('')
const filterCorrelatedEventId = ref('')
const historyLoading = ref(false)
const historyItems = ref<PlayerTransactionListItem[]>([])
const historyTotal = ref(0)
const historyPage = ref(1)

const historyHeaders = [
  { title: 'ID', key: 'id' },
  { title: 'Kind', key: 'kind' },
  { title: 'Status', key: 'status' },
  { title: 'Actors', key: 'actorCount' },
  { title: 'Econ Txns', key: 'economyTxnCount' },
  { title: 'Items', key: 'itemChangeCount' },
  { title: 'Date', key: 'createdAtUtc' },
]

async function fetchHistory() {
  historyLoading.value = true
  error.value = null

  try {
    const res = await playerTransactionService.history({
      playerId: filterPlayerId.value || undefined,
      correlatedEventId: filterCorrelatedEventId.value || undefined,
      page: historyPage.value,
      pageSize: 25,
    })

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

// ─── Detail Dialog ───────────────────────────────────────────────────
const detailDialog = ref(false)
const detailLoading = ref(false)
const detail = ref<PlayerTransactionDetail | null>(null)

async function openDetail(item: PlayerTransactionListItem) {
  detailDialog.value = true
  detailLoading.value = true

  try {
    detail.value = await playerTransactionService.detail(item.id)
  }
  catch (err) {
    error.value = handleApiError(err)
    detailDialog.value = false
  }
  finally {
    detailLoading.value = false
  }
}

// ─── Dispute ─────────────────────────────────────────────────────────
const disputeId = ref('')
const disputeReason = ref('')
const disputeLoading = ref(false)
const disputeDialog = ref(false)
const disputeSuccess = ref(false)

async function confirmDispute() {
  disputeLoading.value = true
  error.value = null
  disputeSuccess.value = false

  try {
    await playerTransactionService.dispute({
      playerTransactionId: disputeId.value,
      reason: disputeReason.value,
    })

    disputeSuccess.value = true
    disputeDialog.value = false
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    disputeLoading.value = false
  }
}

// ─── Reverse ─────────────────────────────────────────────────────────
const reverseId = ref('')
const reverseReason = ref('')
const reverseLoading = ref(false)
const reverseDialog = ref(false)
const reverseSuccess = ref(false)

async function confirmReverse() {
  reverseLoading.value = true
  error.value = null
  reverseSuccess.value = false

  try {
    await playerTransactionService.reverse({
      playerTransactionId: reverseId.value,
      reason: reverseReason.value,
    })

    reverseSuccess.value = true
    reverseDialog.value = false
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    reverseLoading.value = false
  }
}

const statusColors: Record<string, string> = {
  Applied: 'success',
  Pending: 'warning',
  Disputed: 'error',
  Reversed: 'info',
  Failed: 'error',
}

onMounted(fetchHistory)
</script>

<template>
  <VCard>
    <VCardTitle class="pa-4">
      Player Transactions
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
      <VTab value="dispute">
        Dispute
      </VTab>
      <VTab value="reverse">
        Reverse
      </VTab>
    </VTabs>

    <VCardText>
      <VTabsWindow v-model="tab">
        <!-- History -->
        <VTabsWindowItem value="history">
          <div class="d-flex align-center mb-4 gap-4">
            <VTextField
              v-model="filterPlayerId"
              label="Player ID"
              density="compact"
              variant="outlined"
              hide-details
              style="max-width: 300px"
            />
            <VTextField
              v-model="filterCorrelatedEventId"
              label="Correlated Event ID"
              density="compact"
              variant="outlined"
              hide-details
              style="max-width: 300px"
            />
            <VBtn @click="fetchHistory">
              Search
            </VBtn>
          </div>

          <VDataTableServer
            :headers="historyHeaders"
            :items="historyItems"
            :items-length="historyTotal"
            :loading="historyLoading"
            :items-per-page="25"
            :page="historyPage"
            @update:options="(o: { page: number }) => { historyPage = o.page; fetchHistory() }"
            @click:row="(_: unknown, row: { item: PlayerTransactionListItem }) => openDetail(row.item)"
          >
            <template #item.id="{ item }">
              <span class="text-caption font-weight-medium cursor-pointer text-primary">
                {{ item.id.slice(0, 8) }}...
              </span>
            </template>

            <template #item.status="{ item }">
              <VChip
                :color="statusColors[item.status] ?? 'default'"
                size="small"
              >
                {{ item.status }}
              </VChip>
            </template>

            <template #item.createdAtUtc="{ item }">
              {{ new Date(item.createdAtUtc).toLocaleString() }}
            </template>
          </VDataTableServer>
        </VTabsWindowItem>

        <!-- Dispute -->
        <VTabsWindowItem value="dispute">
          <VRow>
            <VCol
              cols="12"
              md="6"
            >
              <VTextField
                v-model="disputeId"
                label="Player Transaction ID"
              />
            </VCol>
            <VCol
              cols="12"
              md="6"
            >
              <VTextField
                v-model="disputeReason"
                label="Reason"
              />
            </VCol>
            <VCol cols="12">
              <VBtn
                color="warning"
                :disabled="!disputeId || !disputeReason"
                @click="disputeDialog = true"
              >
                Dispute Transaction
              </VBtn>
            </VCol>
          </VRow>

          <VAlert
            v-if="disputeSuccess"
            type="success"
            class="mt-4"
          >
            Transaction disputed successfully.
          </VAlert>
        </VTabsWindowItem>

        <!-- Reverse -->
        <VTabsWindowItem value="reverse">
          <VRow>
            <VCol
              cols="12"
              md="6"
            >
              <VTextField
                v-model="reverseId"
                label="Player Transaction ID"
              />
            </VCol>
            <VCol
              cols="12"
              md="6"
            >
              <VTextField
                v-model="reverseReason"
                label="Reason"
              />
            </VCol>
            <VCol cols="12">
              <VBtn
                color="error"
                :disabled="!reverseId || !reverseReason"
                @click="reverseDialog = true"
              >
                Reverse Transaction
              </VBtn>
            </VCol>
          </VRow>

          <VAlert
            v-if="reverseSuccess"
            type="success"
            class="mt-4"
          >
            Transaction reversed successfully.
          </VAlert>
        </VTabsWindowItem>
      </VTabsWindow>
    </VCardText>
  </VCard>

  <!-- Detail Dialog -->
  <VDialog
    v-model="detailDialog"
    max-width="700"
  >
    <VCard>
      <VCardTitle>Transaction Detail</VCardTitle>
      <VCardText>
        <div v-if="detailLoading">
          <VProgressLinear indeterminate />
        </div>
        <div v-else-if="detail">
          <VRow>
            <VCol cols="6">
              <strong>ID:</strong> {{ detail.id }}
            </VCol>
            <VCol cols="6">
              <strong>Status:</strong>
              <VChip
                :color="statusColors[detail.status] ?? 'default'"
                size="small"
                class="ms-2"
              >
                {{ detail.status }}
              </VChip>
            </VCol>
            <VCol cols="6">
              <strong>Kind:</strong> {{ detail.kind }}
            </VCol>
            <VCol cols="6">
              <strong>Created:</strong> {{ new Date(detail.createdAtUtc).toLocaleString() }}
            </VCol>
          </VRow>

          <h4 class="mt-4 mb-2">
            Actors ({{ detail.actors.length }})
          </h4>
          <VTable
            v-if="detail.actors.length"
            density="compact"
          >
            <thead>
              <tr>
                <th>Player ID</th>
                <th>Role</th>
                <th>Allocation %</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="a in detail.actors"
                :key="a.playerId"
              >
                <td>{{ a.playerId }}</td>
                <td>{{ a.role }}</td>
                <td>{{ a.allocationPercent }}%</td>
              </tr>
            </tbody>
          </VTable>

          <h4 class="mt-4 mb-2">
            Item Changes ({{ detail.itemChanges.length }})
          </h4>
          <VTable
            v-if="detail.itemChanges.length"
            density="compact"
          >
            <thead>
              <tr>
                <th>Item Type</th>
                <th>Operation</th>
                <th>Quantity</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="(ic, idx) in detail.itemChanges"
                :key="idx"
              >
                <td>{{ ic.itemType }}</td>
                <td>{{ ic.operation }}</td>
                <td>{{ ic.quantity }}</td>
              </tr>
            </tbody>
          </VTable>

          <h4 class="mt-4 mb-2">
            Economy Transactions ({{ detail.economyTransactions.length }})
          </h4>
          <VTable
            v-if="detail.economyTransactions.length"
            density="compact"
          >
            <thead>
              <tr>
                <th>Event ID</th>
                <th>Kind</th>
                <th>Date</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="et in detail.economyTransactions"
                :key="et.eventId"
              >
                <td>{{ et.eventId.slice(0, 8) }}...</td>
                <td>{{ et.kind }}</td>
                <td>{{ new Date(et.createdAtUtc).toLocaleString() }}</td>
              </tr>
            </tbody>
          </VTable>
        </div>
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn @click="detailDialog = false">
          Close
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>

  <!-- Dispute Confirmation -->
  <VDialog
    v-model="disputeDialog"
    max-width="400"
  >
    <VCard>
      <VCardTitle>Confirm Dispute</VCardTitle>
      <VCardText>
        Mark transaction <strong>{{ disputeId.slice(0, 8) }}...</strong> as disputed?
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn @click="disputeDialog = false">
          Cancel
        </VBtn>
        <VBtn
          color="warning"
          :loading="disputeLoading"
          @click="confirmDispute"
        >
          Confirm
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>

  <!-- Reverse Confirmation -->
  <VDialog
    v-model="reverseDialog"
    max-width="400"
  >
    <VCard>
      <VCardTitle>Confirm Reversal</VCardTitle>
      <VCardText>
        Reverse transaction <strong>{{ reverseId.slice(0, 8) }}...</strong>? This will roll back all economy and item changes.
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn @click="reverseDialog = false">
          Cancel
        </VBtn>
        <VBtn
          color="error"
          :loading="reverseLoading"
          @click="confirmReverse"
        >
          Confirm
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>
</template>
