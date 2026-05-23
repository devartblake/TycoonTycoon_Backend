<script setup lang="ts">
import { getAnalyticsSnapshot, clearAnalytics } from '@/lib/adminAnalytics'
import type { AnalyticsSnapshot, AdminApiEvent } from '@/lib/adminAnalytics'

const SLO_SUCCESS_RATE = 99.0
const SLO_THROTTLED_PCT = 1.0
const SLO_LATENCY_MS = 500

const snapshot = ref<AnalyticsSnapshot>(getAnalyticsSnapshot())

function refreshData() {
  snapshot.value = getAnalyticsSnapshot()
}

function handleClear() {
  clearAnalytics()
  refreshData()
}

function sloColor(actual: number, target: number, higherIsBetter: boolean): string {
  if (higherIsBetter) {
    if (actual >= target) return 'success'
    if (actual >= target * 0.95) return 'warning'

    return 'error'
  }

  if (actual <= target) return 'success'
  if (actual <= target * 1.5) return 'warning'

  return 'error'
}

const throttledPct = computed(() =>
  snapshot.value.totalRequests > 0
    ? (snapshot.value.throttledCount / snapshot.value.totalRequests) * 100
    : 0,
)

const errorCodeColor: Record<string, string> = {
  UNAUTHORIZED: 'error',
  FORBIDDEN: 'error',
  RATE_LIMITED: 'warning',
  VALIDATION_ERROR: 'warning',
  NOT_FOUND: 'info',
  CONFLICT: 'warning',
}

const backendCorrelation = [
  { frontendCode: 'UNAUTHORIZED', backendMetric: 'admin_auth_events_total{outcome="unauthorized"}', runbook: 'Check JWT expiry / token rotation. Alert at >= 20/min over 5m.' },
  { frontendCode: 'FORBIDDEN', backendMetric: 'admin_auth_events_total{outcome="forbidden"}', runbook: 'Verify role/scope claims. May indicate misconfigured ACL.' },
  { frontendCode: 'RATE_LIMITED', backendMetric: 'admin_rate_limit_rejected_total{path=...}', runbook: 'Alert at >= 10/min over 5m. Check if legitimate spike or abuse.' },
  { frontendCode: 'NOT_FOUND', backendMetric: 'admin_notification_events_total{outcome="not_found"}', runbook: 'Alert at >= 5/min over 15m. Indicates config drift or stale references.' },
  { frontendCode: 'CONFLICT', backendMetric: 'N/A (contextual)', runbook: 'Usually replay on non-failed schedule. Refresh state and retry.' },
]

function formatTime(ts: number): string {
  return new Date(ts).toLocaleTimeString()
}

onMounted(() => {
  const interval = setInterval(refreshData, 5000)

  onUnmounted(() => clearInterval(interval))
})
</script>

<template>
  <div>
    <!-- Header -->
    <div class="d-flex align-center justify-space-between mb-4">
      <h4 class="text-h4">
        Observability
      </h4>
      <div class="d-flex gap-2">
        <VBtn
          icon="ri-refresh-line"
          size="small"
          variant="text"
          @click="refreshData"
        />
        <VBtn
          icon="ri-delete-bin-line"
          size="small"
          variant="text"
          @click="handleClear"
        />
      </div>
    </div>

    <!-- SLO Cards -->
    <VRow class="mb-4">
      <VCol
        cols="12"
        sm="4"
      >
        <VCard variant="outlined">
          <VCardText>
            <div class="text-caption text-medium-emphasis">
              Admin Action Success Rate
            </div>
            <div
              class="text-h4 my-1"
              :class="`text-${sloColor(snapshot.successRate, SLO_SUCCESS_RATE, true)}`"
            >
              {{ snapshot.successRate.toFixed(1) }}
              <span class="text-body-2 text-medium-emphasis"> %</span>
            </div>
            <div class="text-caption text-medium-emphasis">
              Target: >= {{ SLO_SUCCESS_RATE }}%
            </div>
          </VCardText>
        </VCard>
      </VCol>
      <VCol
        cols="12"
        sm="4"
      >
        <VCard variant="outlined">
          <VCardText>
            <div class="text-caption text-medium-emphasis">
              Throttled Action Rate
            </div>
            <div
              class="text-h4 my-1"
              :class="`text-${sloColor(throttledPct, SLO_THROTTLED_PCT, false)}`"
            >
              {{ throttledPct.toFixed(1) }}
              <span class="text-body-2 text-medium-emphasis"> %</span>
            </div>
            <div class="text-caption text-medium-emphasis">
              Target: &lt;= {{ SLO_THROTTLED_PCT }}%
            </div>
          </VCardText>
        </VCard>
      </VCol>
      <VCol
        cols="12"
        sm="4"
      >
        <VCard variant="outlined">
          <VCardText>
            <div class="text-caption text-medium-emphasis">
              Median Response Latency
            </div>
            <div
              class="text-h4 my-1"
              :class="`text-${sloColor(snapshot.avgLatencyMs, SLO_LATENCY_MS, false)}`"
            >
              {{ snapshot.avgLatencyMs }}
              <span class="text-body-2 text-medium-emphasis"> ms</span>
            </div>
            <div class="text-caption text-medium-emphasis">
              Target: &lt;= {{ SLO_LATENCY_MS }}ms
            </div>
          </VCardText>
        </VCard>
      </VCol>
    </VRow>

    <!-- Errors by Code + Endpoint Stats -->
    <VRow class="mb-4">
      <VCol
        cols="12"
        md="6"
      >
        <VCard variant="outlined">
          <VCardText>
            <div class="text-subtitle-1 mb-2">
              Frontend Errors by Code
            </div>
            <div
              v-if="!snapshot.errorsByCode.length"
              class="text-body-2 text-medium-emphasis"
            >
              No errors in this session
            </div>
            <VTable
              v-else
              density="compact"
            >
              <thead>
                <tr>
                  <th>Error Code</th>
                  <th class="text-right">
                    Count
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="e in snapshot.errorsByCode"
                  :key="e.code"
                >
                  <td>
                    <VChip
                      :color="errorCodeColor[e.code] ?? 'default'"
                      size="small"
                      variant="tonal"
                    >
                      {{ e.code }}
                    </VChip>
                  </td>
                  <td class="text-right">
                    {{ e.count }}
                  </td>
                </tr>
              </tbody>
            </VTable>
          </VCardText>
        </VCard>
      </VCol>

      <VCol
        cols="12"
        md="6"
      >
        <VCard variant="outlined">
          <VCardText>
            <div class="text-subtitle-1 mb-2">
              Endpoint Stats
            </div>
            <div
              v-if="!snapshot.endpointStats.length"
              class="text-body-2 text-medium-emphasis"
            >
              No requests in this session
            </div>
            <VTable
              v-else
              density="compact"
              style="max-height: 260px; overflow-y: auto"
            >
              <thead>
                <tr>
                  <th>Endpoint</th>
                  <th class="text-right">
                    Reqs
                  </th>
                  <th class="text-right">
                    Fails
                  </th>
                  <th class="text-right">
                    Avg ms
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="ep in snapshot.endpointStats"
                  :key="ep.endpoint"
                >
                  <td style="font-family: monospace; font-size: 0.75rem">
                    {{ ep.endpoint }}
                  </td>
                  <td class="text-right">
                    {{ ep.totalRequests }}
                  </td>
                  <td
                    class="text-right"
                    :class="ep.failures > 0 ? 'text-error' : 'text-medium-emphasis'"
                  >
                    {{ ep.failures }}
                  </td>
                  <td class="text-right">
                    {{ ep.avgLatencyMs }}
                  </td>
                </tr>
              </tbody>
            </VTable>
          </VCardText>
        </VCard>
      </VCol>
    </VRow>

    <!-- Backend Metric Correlation -->
    <VCard
      variant="outlined"
      class="mb-4"
    >
      <VCardText>
        <div class="text-subtitle-1 mb-1">
          Frontend -> Backend Metric Correlation
        </div>
        <div class="text-body-2 text-medium-emphasis mb-3">
          Maps frontend error codes to backend Prometheus counters and runbook steps.
        </div>
        <VTable density="compact">
          <thead>
            <tr>
              <th>Frontend Code</th>
              <th>Backend Metric</th>
              <th>Runbook</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="row in backendCorrelation"
              :key="row.frontendCode"
            >
              <td>
                <VChip
                  :color="errorCodeColor[row.frontendCode] ?? 'default'"
                  size="small"
                  variant="tonal"
                >
                  {{ row.frontendCode }}
                </VChip>
              </td>
              <td style="font-family: monospace; font-size: 0.75rem">
                {{ row.backendMetric }}
              </td>
              <td>{{ row.runbook }}</td>
            </tr>
          </tbody>
        </VTable>
      </VCardText>
    </VCard>

    <!-- Recent Events -->
    <VCard variant="outlined">
      <VCardText>
        <div class="text-subtitle-1 mb-2">
          Recent API Events ({{ snapshot.recentEvents.length }})
        </div>
        <div
          v-if="!snapshot.recentEvents.length"
          class="text-body-2 text-medium-emphasis"
        >
          No events recorded yet. Navigate the dashboard to generate API activity.
        </div>
        <VTable
          v-else
          density="compact"
          style="max-height: 320px; overflow-y: auto"
        >
          <thead>
            <tr>
              <th>Time</th>
              <th>Method</th>
              <th>Endpoint</th>
              <th class="text-center">
                Status
              </th>
              <th class="text-right">
                Latency
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="(evt, i) in snapshot.recentEvents"
              :key="`${evt.timestamp}-${i}`"
            >
              <td style="font-size: 0.75rem">
                {{ formatTime(evt.timestamp) }}
              </td>
              <td>{{ evt.method }}</td>
              <td style="font-family: monospace; font-size: 0.75rem">
                {{ evt.endpoint }}
              </td>
              <td class="text-center">
                <VChip
                  v-if="evt.success"
                  :color="'success'"
                  size="small"
                  variant="tonal"
                >
                  {{ evt.status }}
                </VChip>
                <VChip
                  v-else
                  :color="errorCodeColor[evt.errorCode ?? ''] ?? 'error'"
                  size="small"
                  variant="tonal"
                >
                  {{ evt.errorCode ?? evt.status }}
                </VChip>
              </td>
              <td class="text-right">
                {{ (evt as AdminApiEvent).latencyMs }}ms
              </td>
            </tr>
          </tbody>
        </VTable>
      </VCardText>
    </VCard>
  </div>
</template>
