<script setup lang="ts">
import { economyService } from '@/lib/services/economyService'
import { handleApiError, applyErrorSideEffects } from '@/lib/apiErrors'
import type { ErrorHandlerResult } from '@/lib/apiErrors'
import type { EconomyTxnListItem, EconomyTxnResult } from '@/lib/types/admin'

const tab = ref('history')
const error = ref<ErrorHandlerResult | null>(null)

// ─── History Tab ─────────────────────────────────────────────────────
const historyPlayerId = ref('')
const historySearchId = ref('')
const historyLoading = ref(false)
const historyItems = ref<EconomyTxnListItem[]>([])
const historyTotal = ref(0)
const historyPage = ref(1)

const historyHeaders = [
  { title: 'Event ID', key: 'eventId' },
  { title: 'Kind', key: 'kind' },
  { title: 'XP', key: 'xp' },
  { title: 'Coins', key: 'coins' },
  { title: 'Diamonds', key: 'diamonds' },
  { title: 'Date', key: 'createdAtUtc' },
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
    const res = await economyService.history(historySearchId.value, { page: historyPage.value, pageSize: 25 })

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

function getDelta(item: EconomyTxnListItem, currency: number): number {
  return item.lines?.find(l => l.currency === currency)?.delta ?? 0
}

// ─── Create Tab ──────────────────────────────────────────────────────
const createForm = ref({
  playerId: '',
  kind: 'admin-adjust',
  xp: 0,
  coins: 0,
  diamonds: 0,
  note: '',
})

const createLoading = ref(false)
const createResult = ref<EconomyTxnResult | null>(null)

async function submitCreate() {
  createLoading.value = true
  error.value = null
  createResult.value = null

  try {
    const lines = []

    if (createForm.value.xp !== 0) lines.push({ currency: 1, delta: createForm.value.xp })
    if (createForm.value.coins !== 0) lines.push({ currency: 2, delta: createForm.value.coins })
    if (createForm.value.diamonds !== 0) lines.push({ currency: 3, delta: createForm.value.diamonds })

    const res = await economyService.createTransaction({
      eventId: crypto.randomUUID(),
      playerId: createForm.value.playerId,
      kind: createForm.value.kind,
      lines,
      note: createForm.value.note || undefined,
    })

    createResult.value = res
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    createLoading.value = false
  }
}

// ─── Rollback Tab ────────────────────────────────────────────────────
const rollbackEventId = ref('')
const rollbackReason = ref('')
const rollbackLoading = ref(false)
const rollbackResult = ref<EconomyTxnResult | null>(null)
const rollbackDialog = ref(false)

async function confirmRollback() {
  rollbackLoading.value = true
  error.value = null
  rollbackResult.value = null

  try {
    const res = await economyService.rollback(rollbackEventId.value, rollbackReason.value)

    rollbackResult.value = res
    rollbackDialog.value = false
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    rollbackLoading.value = false
  }
}
</script>

<template>
  <VCard>
    <VCardTitle class="pa-4">
      Economy
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
      <VTab value="create">
        Create Transaction
      </VTab>
      <VTab value="rollback">
        Rollback
      </VTab>
    </VTabs>

    <VCardText>
      <VTabsWindow v-model="tab">
        <!-- History Tab -->
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
            <template #item.eventId="{ item }">
              <span class="text-caption font-weight-medium">{{ item.eventId.slice(0, 8) }}...</span>
            </template>

            <template #item.xp="{ item }">
              <span :class="getDelta(item, 1) >= 0 ? 'text-success' : 'text-error'">
                {{ getDelta(item, 1) }}
              </span>
            </template>

            <template #item.coins="{ item }">
              <span :class="getDelta(item, 2) >= 0 ? 'text-success' : 'text-error'">
                {{ getDelta(item, 2) }}
              </span>
            </template>

            <template #item.diamonds="{ item }">
              <span :class="getDelta(item, 3) >= 0 ? 'text-success' : 'text-error'">
                {{ getDelta(item, 3) }}
              </span>
            </template>

            <template #item.createdAtUtc="{ item }">
              {{ new Date(item.createdAtUtc).toLocaleString() }}
            </template>
          </VDataTableServer>
        </VTabsWindowItem>

        <!-- Create Tab -->
        <VTabsWindowItem value="create">
          <VRow>
            <VCol
              cols="12"
              md="6"
            >
              <VTextField
                v-model="createForm.playerId"
                label="Player ID"
              />
            </VCol>
            <VCol
              cols="12"
              md="6"
            >
              <VTextField
                v-model="createForm.kind"
                label="Kind"
              />
            </VCol>
            <VCol
              cols="12"
              md="4"
            >
              <VTextField
                v-model.number="createForm.xp"
                label="XP Delta"
                type="number"
              />
            </VCol>
            <VCol
              cols="12"
              md="4"
            >
              <VTextField
                v-model.number="createForm.coins"
                label="Coins Delta"
                type="number"
              />
            </VCol>
            <VCol
              cols="12"
              md="4"
            >
              <VTextField
                v-model.number="createForm.diamonds"
                label="Diamonds Delta"
                type="number"
              />
            </VCol>
            <VCol cols="12">
              <VTextField
                v-model="createForm.note"
                label="Note (optional)"
              />
            </VCol>
            <VCol cols="12">
              <VBtn
                :loading="createLoading"
                :disabled="!createForm.playerId"
                @click="submitCreate"
              >
                Create Transaction
              </VBtn>
            </VCol>
          </VRow>

          <VAlert
            v-if="createResult"
            type="success"
            class="mt-4"
          >
            Transaction applied. Balance: XP={{ createResult.balanceXp }}, Coins={{ createResult.balanceCoins }}, Diamonds={{ createResult.balanceDiamonds }}
          </VAlert>
        </VTabsWindowItem>

        <!-- Rollback Tab -->
        <VTabsWindowItem value="rollback">
          <VRow>
            <VCol
              cols="12"
              md="6"
            >
              <VTextField
                v-model="rollbackEventId"
                label="Event ID to rollback"
              />
            </VCol>
            <VCol
              cols="12"
              md="6"
            >
              <VTextField
                v-model="rollbackReason"
                label="Reason"
              />
            </VCol>
            <VCol cols="12">
              <VBtn
                color="error"
                :disabled="!rollbackEventId || !rollbackReason"
                @click="rollbackDialog = true"
              >
                Rollback
              </VBtn>
            </VCol>
          </VRow>

          <VAlert
            v-if="rollbackResult"
            type="success"
            class="mt-4"
          >
            Rollback applied. New balance: XP={{ rollbackResult.balanceXp }}, Coins={{ rollbackResult.balanceCoins }}, Diamonds={{ rollbackResult.balanceDiamonds }}
          </VAlert>
        </VTabsWindowItem>
      </VTabsWindow>
    </VCardText>
  </VCard>

  <!-- Rollback Confirmation -->
  <VDialog
    v-model="rollbackDialog"
    max-width="400"
  >
    <VCard>
      <VCardTitle>Confirm Rollback</VCardTitle>
      <VCardText>
        Are you sure you want to rollback event <strong>{{ rollbackEventId }}</strong>?
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn @click="rollbackDialog = false">
          Cancel
        </VBtn>
        <VBtn
          color="error"
          :loading="rollbackLoading"
          @click="confirmRollback"
        >
          Confirm
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>
</template>
