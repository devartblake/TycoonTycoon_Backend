<script setup lang="ts">
import { notificationService } from '@/lib/services/notificationService'
import { handleApiError, applyErrorSideEffects } from '@/lib/apiErrors'
import type { ErrorHandlerResult } from '@/lib/apiErrors'
import type { NotificationHistoryItem, NotificationScheduledItem, NotificationChannel } from '@/lib/types/admin'

const tab = ref('history')
const error = ref<ErrorHandlerResult | null>(null)

// ─── History ─────────────────────────────────────────────────────────
const historyLoading = ref(false)
const historyItems = ref<NotificationHistoryItem[]>([])
const historyTotal = ref(0)
const historyPage = ref(1)

const historyHeaders = [
  { title: 'Title', key: 'title' },
  { title: 'Channel', key: 'channelKey' },
  { title: 'Status', key: 'status' },
  { title: 'Date', key: 'createdAt' },
]

async function fetchHistory() {
  historyLoading.value = true
  error.value = null

  try {
    const res = await notificationService.history({ page: historyPage.value, pageSize: 25 })

    historyItems.value = res.items
    historyTotal.value = res.totalItems
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

// ─── Dead Letter ─────────────────────────────────────────────────────
const deadLetterLoading = ref(false)
const deadLetterItems = ref<NotificationScheduledItem[]>([])
const deadLetterTotal = ref(0)

const deadLetterHeaders = [
  { title: 'Title', key: 'title' },
  { title: 'Channel', key: 'channelKey' },
  { title: 'Status', key: 'status' },
  { title: 'Scheduled', key: 'scheduledAt' },
  { title: 'Actions', key: 'actions', sortable: false },
]

async function fetchDeadLetter() {
  deadLetterLoading.value = true

  try {
    const res = await notificationService.deadLetter({ pageSize: 25 })

    deadLetterItems.value = res.items
    deadLetterTotal.value = res.totalItems
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    deadLetterLoading.value = false
  }
}

async function replayItem(item: NotificationScheduledItem) {
  try {
    await notificationService.replay(item.scheduleId)
    fetchDeadLetter()
  }
  catch (err) {
    error.value = handleApiError(err)
  }
}

// ─── Send ────────────────────────────────────────────────────────────
const channels = ref<NotificationChannel[]>([])
const sendForm = ref({
  channelKey: '',
  title: '',
  body: '',
})

const sendLoading = ref(false)
const sendSuccess = ref(false)

async function loadChannels() {
  try {
    channels.value = await notificationService.channels()
    if (channels.value.length)
      sendForm.value.channelKey = channels.value[0].key
  }
  catch (err) {
    error.value = handleApiError(err)
  }
}

async function submitSend() {
  sendLoading.value = true
  error.value = null
  sendSuccess.value = false

  try {
    await notificationService.send({
      channelKey: sendForm.value.channelKey,
      title: sendForm.value.title,
      body: sendForm.value.body,
      audience: {},
    })

    sendSuccess.value = true
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    sendLoading.value = false
  }
}

onMounted(() => {
  fetchHistory()
  loadChannels()
})
</script>

<template>
  <VCard>
    <VCardTitle class="pa-4">
      Notifications
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
      <VTab
        value="deadletter"
        @click="fetchDeadLetter"
      >
        Dead Letter
      </VTab>
      <VTab value="send">
        Send
      </VTab>
    </VTabs>

    <VCardText>
      <VTabsWindow v-model="tab">
        <VTabsWindowItem value="history">
          <VDataTableServer
            :headers="historyHeaders"
            :items="historyItems"
            :items-length="historyTotal"
            :loading="historyLoading"
            :items-per-page="25"
            :page="historyPage"
            @update:options="(o: { page: number }) => { historyPage = o.page; fetchHistory() }"
          >
            <template #item.createdAt="{ item }">
              {{ new Date(item.createdAt).toLocaleString() }}
            </template>
          </VDataTableServer>
        </VTabsWindowItem>

        <VTabsWindowItem value="deadletter">
          <VDataTableServer
            :headers="deadLetterHeaders"
            :items="deadLetterItems"
            :items-length="deadLetterTotal"
            :loading="deadLetterLoading"
            :items-per-page="25"
          >
            <template #item.scheduledAt="{ item }">
              {{ new Date(item.scheduledAt).toLocaleString() }}
            </template>

            <template #item.actions="{ item }">
              <VBtn
                size="x-small"
                color="primary"
                @click="replayItem(item)"
              >
                Replay
              </VBtn>
            </template>
          </VDataTableServer>
        </VTabsWindowItem>

        <VTabsWindowItem value="send">
          <VRow>
            <VCol
              cols="12"
              md="4"
            >
              <VSelect
                v-model="sendForm.channelKey"
                :items="channels.map(c => ({ title: c.name, value: c.key }))"
                label="Channel"
              />
            </VCol>
            <VCol
              cols="12"
              md="8"
            >
              <VTextField
                v-model="sendForm.title"
                label="Title"
              />
            </VCol>
            <VCol cols="12">
              <VTextarea
                v-model="sendForm.body"
                label="Body"
                rows="4"
              />
            </VCol>
            <VCol cols="12">
              <VBtn
                :loading="sendLoading"
                :disabled="!sendForm.title || !sendForm.body"
                @click="submitSend"
              >
                Send Notification
              </VBtn>
            </VCol>
          </VRow>

          <VAlert
            v-if="sendSuccess"
            type="success"
            class="mt-4"
          >
            Notification sent successfully.
          </VAlert>
        </VTabsWindowItem>
      </VTabsWindow>
    </VCardText>
  </VCard>
</template>
