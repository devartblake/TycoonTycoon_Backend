<script setup lang="ts">
import { userService } from '@/lib/services/userService'
import { moderationService } from '@/lib/services/moderationService'
import { antiCheatService } from '@/lib/services/antiCheatService'
import { economyService } from '@/lib/services/economyService'
import { handleApiError, applyErrorSideEffects } from '@/lib/apiErrors'
import type { ErrorHandlerResult } from '@/lib/apiErrors'
import type {
  AdminUserDetail,
  ModerationProfile,
  AdminUserActivityItem,
  AntiCheatFlag,
  EconomyTxnListItem,
  ModerationLogItem,
} from '@/lib/types/admin'

const route = useRoute()
const userId = computed(() => route.params.id as string)

const error = ref<ErrorHandlerResult | null>(null)
const user = ref<AdminUserDetail | null>(null)
const modProfile = ref<ModerationProfile | null>(null)
const loadFailed = ref(false)

// Tab
const tab = ref('activity')

// Activity
const activities = ref<AdminUserActivityItem[]>([])
const actPage = ref(1)
const actTotal = ref(0)
const actLoading = ref(false)

const actHeaders = [
  { title: 'Type', key: 'type' },
  { title: 'Description', key: 'description' },
  { title: 'Time', key: 'createdAt' },
]

// Anti-Cheat
const flags = ref<AntiCheatFlag[]>([])
const flagPage = ref(1)
const flagTotal = ref(0)
const flagLoading = ref(false)

const flagHeaders = [
  { title: 'Rule', key: 'ruleKey' },
  { title: 'Severity', key: 'severity' },
  { title: 'Message', key: 'message' },
  { title: 'Reviewed', key: 'reviewed', sortable: false },
  { title: 'Date', key: 'createdAtUtc' },
]

// Economy
const txns = ref<EconomyTxnListItem[]>([])
const txnPage = ref(1)
const txnTotal = ref(0)
const txnLoading = ref(false)

const txnHeaders = [
  { title: 'Kind', key: 'kind' },
  { title: 'Changes', key: 'lines', sortable: false },
  { title: 'Date', key: 'createdAtUtc' },
]

// Moderation Log
const modLogs = ref<ModerationLogItem[]>([])
const modLogPage = ref(1)
const modLogTotal = ref(0)
const modLogLoading = ref(false)

const modLogHeaders = [
  { title: 'Status', key: 'newStatus' },
  { title: 'Reason', key: 'reason' },
  { title: 'Admin', key: 'setByAdmin' },
  { title: 'Date', key: 'createdAtUtc' },
]

// Ban/Unban
const banDialog = ref(false)
const banReason = ref('')
const banLoading = ref(false)
const unbanDialog = ref(false)
const unbanLoading = ref(false)

const severityColors: Record<number, string> = { 2: 'error', 1: 'warning', 0: 'info' }
const statusLabels: Record<number, string> = { 0: 'Normal', 1: 'Suspected', 2: 'Restricted', 3: 'Banned' }
const statusColors: Record<number, string> = { 0: 'success', 1: 'warning', 2: 'error', 3: 'error' }
const currencyLabel: Record<number, string> = { 1: 'XP', 2: 'Coins', 3: 'Diamonds' }

// ─── Loaders ─────────────────────────────────────────────────────────
async function loadUser() {
  try {
    const [u, m] = await Promise.all([
      userService.get(userId.value),
      moderationService.getProfile(userId.value),
    ])

    user.value = u
    modProfile.value = m
  }
  catch (err) {
    loadFailed.value = true
    const r = handleApiError(err)

    error.value = r
    applyErrorSideEffects(r)
  }
}

async function loadActivity() {
  actLoading.value = true

  try {
    const res = await userService.activity(userId.value, { page: actPage.value, pageSize: 25 })

    activities.value = res.items
    actTotal.value = res.totalItems
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    actLoading.value = false
  }
}

async function loadFlags() {
  flagLoading.value = true

  try {
    const res = await antiCheatService.flags({ playerId: userId.value, page: flagPage.value, pageSize: 25 })

    flags.value = res.items
    flagTotal.value = res.totalItems
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    flagLoading.value = false
  }
}

async function loadEconomy() {
  txnLoading.value = true

  try {
    const res = await economyService.history(userId.value, { page: txnPage.value, pageSize: 25 })

    txns.value = res.items
    txnTotal.value = res.total
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    txnLoading.value = false
  }
}

async function loadModLogs() {
  modLogLoading.value = true

  try {
    const res = await moderationService.logs({ playerId: userId.value, page: modLogPage.value, pageSize: 25 })

    modLogs.value = res.items
    modLogTotal.value = res.totalItems
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    modLogLoading.value = false
  }
}

// Tab watchers
watch(tab, (val) => {
  if (val === 'activity') loadActivity()
  else if (val === 'anticheat') loadFlags()
  else if (val === 'economy') loadEconomy()
})

// ─── Actions ─────────────────────────────────────────────────────────
async function confirmBan() {
  banLoading.value = true

  try {
    await userService.ban(userId.value, { reason: banReason.value })

    const u = await userService.get(userId.value)

    user.value = u
    banDialog.value = false
    banReason.value = ''
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    banLoading.value = false
  }
}

async function confirmUnban() {
  unbanLoading.value = true

  try {
    await userService.unban(userId.value)

    const u = await userService.get(userId.value)

    user.value = u
    unbanDialog.value = false
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    unbanLoading.value = false
  }
}

onMounted(() => {
  loadUser()
  loadActivity()
  loadModLogs()
})
</script>

<template>
  <div>
    <!-- Loading / Error State -->
    <VAlert
      v-if="error"
      :type="error.severity"
      closable
      class="mb-4"
      @click:close="error = null"
    >
      {{ error.message }}
    </VAlert>

    <div
      v-if="!user && !loadFailed"
      class="d-flex flex-column gap-4"
    >
      <VSkeletonLoader type="heading" />
      <VSkeletonLoader type="card" />
    </div>

    <template v-if="user">
      <!-- Header -->
      <div class="d-flex align-center justify-space-between mb-4">
        <div>
          <VBreadcrumbs
            :items="[{ title: 'Users', to: '/users' }, { title: user.username, disabled: true }]"
            class="pa-0"
          />
          <h4 class="text-h4 mt-1">
            {{ user.username }}
          </h4>
        </div>
        <VBtn
          v-if="user.isBanned"
          color="success"
          @click="unbanDialog = true"
        >
          Unban
        </VBtn>
        <VBtn
          v-else
          color="error"
          @click="banDialog = true"
        >
          Ban
        </VBtn>
      </div>

      <VRow>
        <!-- Profile Card -->
        <VCol
          cols="12"
          md="6"
        >
          <VCard>
            <VCardTitle>Profile</VCardTitle>
            <VCardText>
              <VRow dense>
                <VCol cols="6">
                  <div class="text-caption text-medium-emphasis">
                    Email
                  </div>
                  <div>{{ user.email }}</div>
                </VCol>
                <VCol cols="6">
                  <div class="text-caption text-medium-emphasis">
                    Role
                  </div>
                  <div>{{ user.role }}</div>
                </VCol>
                <VCol cols="6">
                  <div class="text-caption text-medium-emphasis">
                    Age Group
                  </div>
                  <div>{{ user.ageGroup }}</div>
                </VCol>
                <VCol cols="6">
                  <div class="text-caption text-medium-emphasis">
                    Verified
                  </div>
                  <VChip
                    :color="user.isVerified ? 'success' : 'default'"
                    size="small"
                  >
                    {{ user.isVerified ? 'Yes' : 'No' }}
                  </VChip>
                </VCol>
                <VCol cols="6">
                  <div class="text-caption text-medium-emphasis">
                    Joined
                  </div>
                  <div>{{ new Date(user.createdAt).toLocaleDateString() }}</div>
                </VCol>
                <VCol cols="6">
                  <div class="text-caption text-medium-emphasis">
                    Last Active
                  </div>
                  <div>{{ user.lastActive ? new Date(user.lastActive).toLocaleDateString() : '-' }}</div>
                </VCol>
              </VRow>

              <VDivider class="my-4" />

              <VRow dense>
                <VCol
                  cols="4"
                  class="text-center"
                >
                  <div class="text-h5">
                    {{ user.totalGamesPlayed.toLocaleString() }}
                  </div>
                  <div class="text-caption text-medium-emphasis">
                    Games
                  </div>
                </VCol>
                <VCol
                  cols="4"
                  class="text-center"
                >
                  <div class="text-h5">
                    {{ user.totalPoints.toLocaleString() }}
                  </div>
                  <div class="text-caption text-medium-emphasis">
                    Points
                  </div>
                </VCol>
                <VCol
                  cols="4"
                  class="text-center"
                >
                  <div class="text-h5">
                    {{ (user.winRate * 100).toFixed(1) }}%
                  </div>
                  <div class="text-caption text-medium-emphasis">
                    Win Rate
                  </div>
                </VCol>
              </VRow>
            </VCardText>
          </VCard>
        </VCol>

        <!-- Moderation Card -->
        <VCol
          cols="12"
          md="6"
        >
          <VCard>
            <VCardTitle>Moderation</VCardTitle>
            <VCardText>
              <div class="d-flex flex-column gap-3">
                <div>
                  <div class="text-caption text-medium-emphasis">
                    Current Status
                  </div>
                  <VChip
                    v-if="modProfile"
                    :color="statusColors[modProfile.status] ?? 'default'"
                    size="small"
                    class="mt-1"
                  >
                    {{ statusLabels[modProfile.status] ?? modProfile.status }}
                  </VChip>
                  <VSkeletonLoader
                    v-else
                    type="chip"
                    width="80"
                  />
                </div>
                <div v-if="modProfile?.reason">
                  <div class="text-caption text-medium-emphasis">
                    Reason
                  </div>
                  <div>{{ modProfile.reason }}</div>
                </div>
                <div v-if="modProfile?.setByAdmin">
                  <div class="text-caption text-medium-emphasis">
                    Set By
                  </div>
                  <div>{{ modProfile.setByAdmin }} on {{ new Date(modProfile.setAtUtc).toLocaleDateString() }}</div>
                </div>
                <div v-if="modProfile?.expiresAtUtc">
                  <div class="text-caption text-medium-emphasis">
                    Expires
                  </div>
                  <div>{{ new Date(modProfile.expiresAtUtc).toLocaleString() }}</div>
                </div>
                <VChip
                  v-if="user.isBanned"
                  color="error"
                >
                  Account Banned
                </VChip>
              </div>
            </VCardText>
          </VCard>
        </VCol>
      </VRow>

      <!-- Tabs Card -->
      <VCard class="mt-4">
        <VTabs v-model="tab">
          <VTab value="activity">
            Activity
          </VTab>
          <VTab value="anticheat">
            Anti-Cheat
          </VTab>
          <VTab value="economy">
            Economy
          </VTab>
        </VTabs>

        <VTabsWindow v-model="tab">
          <!-- Activity -->
          <VTabsWindowItem value="activity">
            <VDataTableServer
              :headers="actHeaders"
              :items="activities"
              :items-length="actTotal"
              :loading="actLoading"
              :items-per-page="25"
              :page="actPage"
              @update:options="(o: { page: number }) => { actPage = o.page; loadActivity() }"
            >
              <template #item.type="{ item }">
                <VChip
                  size="small"
                  variant="tonal"
                >
                  {{ item.type }}
                </VChip>
              </template>

              <template #item.createdAt="{ item }">
                {{ new Date(item.createdAt).toLocaleString() }}
              </template>
            </VDataTableServer>
          </VTabsWindowItem>

          <!-- Anti-Cheat -->
          <VTabsWindowItem value="anticheat">
            <VDataTableServer
              :headers="flagHeaders"
              :items="flags"
              :items-length="flagTotal"
              :loading="flagLoading"
              :items-per-page="25"
              :page="flagPage"
              @update:options="(o: { page: number }) => { flagPage = o.page; loadFlags() }"
            >
              <template #item.severity="{ item }">
                <VChip
                  :color="severityColors[item.severity] ?? 'default'"
                  size="small"
                >
                  {{ item.severity === 2 ? 'Severe' : item.severity === 1 ? 'Warning' : 'Info' }}
                </VChip>
              </template>

              <template #item.reviewed="{ item }">
                <VIcon
                  v-if="item.reviewedAtUtc"
                  icon="ri-checkbox-circle-line"
                  color="success"
                />
                <VIcon
                  v-else
                  icon="ri-close-circle-line"
                  color="disabled"
                />
              </template>

              <template #item.createdAtUtc="{ item }">
                {{ new Date(item.createdAtUtc).toLocaleString() }}
              </template>
            </VDataTableServer>
          </VTabsWindowItem>

          <!-- Economy -->
          <VTabsWindowItem value="economy">
            <VDataTableServer
              :headers="txnHeaders"
              :items="txns"
              :items-length="txnTotal"
              :loading="txnLoading"
              :items-per-page="25"
              :page="txnPage"
              @update:options="(o: { page: number }) => { txnPage = o.page; loadEconomy() }"
            >
              <template #item.lines="{ item }">
                <VChip
                  v-for="(l, i) in item.lines"
                  :key="i"
                  :color="l.delta > 0 ? 'success' : 'error'"
                  size="small"
                  variant="tonal"
                  class="me-1"
                >
                  {{ l.delta > 0 ? '+' : '' }}{{ l.delta }} {{ currencyLabel[l.currency] ?? '?' }}
                </VChip>
              </template>

              <template #item.createdAtUtc="{ item }">
                {{ new Date(item.createdAtUtc).toLocaleString() }}
              </template>
            </VDataTableServer>
          </VTabsWindowItem>
        </VTabsWindow>
      </VCard>

      <!-- Moderation Log -->
      <VCard class="mt-4">
        <VCardTitle>Moderation Log</VCardTitle>
        <VDataTableServer
          :headers="modLogHeaders"
          :items="modLogs"
          :items-length="modLogTotal"
          :loading="modLogLoading"
          :items-per-page="25"
          :page="modLogPage"
          @update:options="(o: { page: number }) => { modLogPage = o.page; loadModLogs() }"
        >
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
            {{ new Date(item.createdAtUtc).toLocaleString() }}
          </template>
        </VDataTableServer>
      </VCard>
    </template>

    <!-- Ban Dialog -->
    <VDialog
      v-model="banDialog"
      max-width="500"
    >
      <VCard>
        <VCardTitle>Ban {{ user?.username }}</VCardTitle>
        <VCardText>
          <p class="mb-4">
            This will immediately ban the player. They will not be able to play until unbanned.
          </p>
          <VTextField
            v-model="banReason"
            label="Ban reason"
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
            Ban Player
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>

    <!-- Unban Dialog -->
    <VDialog
      v-model="unbanDialog"
      max-width="400"
    >
      <VCard>
        <VCardTitle>Unban {{ user?.username }}</VCardTitle>
        <VCardText>
          This will lift the ban and allow the player to resume playing.
        </VCardText>
        <VCardActions>
          <VSpacer />
          <VBtn
            :disabled="unbanLoading"
            @click="unbanDialog = false"
          >
            Cancel
          </VBtn>
          <VBtn
            color="primary"
            :loading="unbanLoading"
            @click="confirmUnban"
          >
            Unban Player
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </div>
</template>
