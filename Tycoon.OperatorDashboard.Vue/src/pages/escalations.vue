<script setup lang="ts">
import { moderationService } from '@/lib/services/moderationService'
import { handleApiError } from '@/lib/apiErrors'
import type { ErrorHandlerResult } from '@/lib/apiErrors'
import type { RunEscalationResponse, EscalationDecision } from '@/lib/types/admin'

const error = ref<ErrorHandlerResult | null>(null)

const form = ref({
  windowHours: 24,
  maxPlayers: 100,
  dryRun: true,
})

const loading = ref(false)
const result = ref<RunEscalationResponse | null>(null)

const decisionHeaders = [
  { title: 'Player ID', key: 'playerId' },
  { title: 'Current', key: 'currentStatus' },
  { title: 'Proposed', key: 'proposedStatus' },
  { title: 'Severe', key: 'severeCount' },
  { title: 'Warning', key: 'warningCount' },
  { title: 'Reason', key: 'reason' },
]

async function runEscalation() {
  loading.value = true
  error.value = null
  result.value = null

  try {
    result.value = await moderationService.runEscalation({
      windowHours: form.value.windowHours,
      maxPlayers: form.value.maxPlayers,
      dryRun: form.value.dryRun,
    })
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    loading.value = false
  }
}

const statusLabels: Record<number, string> = {
  0: 'Normal',
  1: 'Suspected',
  2: 'Restricted',
  3: 'Banned',
}
</script>

<template>
  <VCard>
    <VCardTitle class="pa-4">
      Escalations
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

    <VCardText>
      <VRow>
        <VCol
          cols="12"
          md="3"
        >
          <VTextField
            v-model.number="form.windowHours"
            label="Window Hours"
            type="number"
          />
        </VCol>
        <VCol
          cols="12"
          md="3"
        >
          <VTextField
            v-model.number="form.maxPlayers"
            label="Max Players"
            type="number"
          />
        </VCol>
        <VCol
          cols="12"
          md="3"
          class="d-flex align-center"
        >
          <VSwitch
            v-model="form.dryRun"
            label="Dry Run"
            hide-details
          />
        </VCol>
        <VCol
          cols="12"
          md="3"
          class="d-flex align-center"
        >
          <VBtn
            :loading="loading"
            :color="form.dryRun ? 'primary' : 'error'"
            @click="runEscalation"
          >
            {{ form.dryRun ? 'Preview' : 'Execute' }}
          </VBtn>
        </VCol>
      </VRow>

      <div v-if="result">
        <VRow class="my-4">
          <VCol cols="4">
            <VCard variant="outlined">
              <VCardText class="text-center">
                <div class="text-h5">
                  {{ result.evaluatedPlayers }}
                </div>
                <div class="text-caption">
                  Evaluated
                </div>
              </VCardText>
            </VCard>
          </VCol>
          <VCol cols="4">
            <VCard variant="outlined">
              <VCardText class="text-center">
                <div class="text-h5">
                  {{ result.changedPlayers }}
                </div>
                <div class="text-caption">
                  Changed
                </div>
              </VCardText>
            </VCard>
          </VCol>
          <VCol cols="4">
            <VCard variant="outlined">
              <VCardText class="text-center">
                <div class="text-h5">
                  <VChip
                    :color="result.dryRun ? 'warning' : 'success'"
                    size="small"
                  >
                    {{ result.dryRun ? 'Dry Run' : 'Applied' }}
                  </VChip>
                </div>
                <div class="text-caption">
                  Mode
                </div>
              </VCardText>
            </VCard>
          </VCol>
        </VRow>

        <VDataTable
          :headers="decisionHeaders"
          :items="result.decisions"
          density="compact"
        >
          <template #item.playerId="{ item }">
            {{ (item as EscalationDecision).playerId.slice(0, 8) }}...
          </template>

          <template #item.currentStatus="{ item }">
            {{ statusLabels[(item as EscalationDecision).currentStatus] ?? (item as EscalationDecision).currentStatus }}
          </template>

          <template #item.proposedStatus="{ item }">
            {{ statusLabels[(item as EscalationDecision).proposedStatus] ?? (item as EscalationDecision).proposedStatus }}
          </template>
        </VDataTable>
      </div>
    </VCardText>
  </VCard>
</template>
