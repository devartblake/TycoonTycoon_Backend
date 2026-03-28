<script setup lang="ts">
import { getAdmin } from '@/lib/auth'
import { getAnalyticsSnapshot } from '@/lib/adminAnalytics'
import type { AnalyticsSnapshot } from '@/lib/adminAnalytics'

const admin = computed(() => getAdmin())
const snapshot = ref<AnalyticsSnapshot>(getAnalyticsSnapshot())

function refreshSnapshot() {
  snapshot.value = getAnalyticsSnapshot()
}

onMounted(() => {
  const interval = setInterval(refreshSnapshot, 5000)

  onUnmounted(() => clearInterval(interval))
})
</script>

<template>
  <div>
    <VRow class="mb-4">
      <VCol cols="12">
        <h4 class="text-h4">
          Welcome, {{ admin?.displayName ?? 'Admin' }}
        </h4>
        <p class="text-body-1 text-medium-emphasis mt-1">
          Tycoon Operator Dashboard
        </p>
      </VCol>
    </VRow>

    <VRow>
      <VCol
        cols="12"
        md="3"
      >
        <VCard>
          <VCardText class="text-center">
            <div class="text-h4">
              {{ snapshot.totalRequests }}
            </div>
            <div class="text-caption text-medium-emphasis">
              API Requests
            </div>
          </VCardText>
        </VCard>
      </VCol>
      <VCol
        cols="12"
        md="3"
      >
        <VCard>
          <VCardText class="text-center">
            <div class="text-h4">
              {{ snapshot.successRate }}%
            </div>
            <div class="text-caption text-medium-emphasis">
              Success Rate
            </div>
          </VCardText>
        </VCard>
      </VCol>
      <VCol
        cols="12"
        md="3"
      >
        <VCard>
          <VCardText class="text-center">
            <div class="text-h4">
              {{ snapshot.avgLatencyMs }}ms
            </div>
            <div class="text-caption text-medium-emphasis">
              Avg Latency
            </div>
          </VCardText>
        </VCard>
      </VCol>
      <VCol
        cols="12"
        md="3"
      >
        <VCard>
          <VCardText class="text-center">
            <div class="text-h4">
              {{ snapshot.totalFailures }}
            </div>
            <div class="text-caption text-medium-emphasis">
              Failures
            </div>
          </VCardText>
        </VCard>
      </VCol>
    </VRow>

    <VRow class="mt-4">
      <VCol cols="12">
        <VCard>
          <VCardTitle class="d-flex align-center pa-4">
            <span>Quick Navigation</span>
          </VCardTitle>
          <VCardText>
            <VRow>
              <VCol
                cols="6"
                md="3"
              >
                <VBtn
                  block
                  variant="tonal"
                  to="/users"
                >
                  <VIcon
                    start
                    icon="ri-group-line"
                  />
                  Users
                </VBtn>
              </VCol>
              <VCol
                cols="6"
                md="3"
              >
                <VBtn
                  block
                  variant="tonal"
                  to="/economy"
                >
                  <VIcon
                    start
                    icon="ri-money-dollar-circle-line"
                  />
                  Economy
                </VBtn>
              </VCol>
              <VCol
                cols="6"
                md="3"
              >
                <VBtn
                  block
                  variant="tonal"
                  to="/anti-cheat"
                >
                  <VIcon
                    start
                    icon="ri-shield-line"
                  />
                  Anti-Cheat
                </VBtn>
              </VCol>
              <VCol
                cols="6"
                md="3"
              >
                <VBtn
                  block
                  variant="tonal"
                  to="/player-transactions"
                >
                  <VIcon
                    start
                    icon="ri-exchange-line"
                  />
                  Transactions
                </VBtn>
              </VCol>
            </VRow>
          </VCardText>
        </VCard>
      </VCol>
    </VRow>
  </div>
</template>
