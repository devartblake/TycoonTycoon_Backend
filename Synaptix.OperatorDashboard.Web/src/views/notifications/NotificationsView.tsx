'use client'

import { useCallback, useEffect, useState } from 'react'

// MUI Imports
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import Chip from '@mui/material/Chip'
import MenuItem from '@mui/material/MenuItem'
import Tab from '@mui/material/Tab'
import TabContext from '@mui/lab/TabContext'
import TabList from '@mui/lab/TabList'
import TabPanel from '@mui/lab/TabPanel'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'

// Component Imports
import PageHeader from '@components/admin/PageHeader'
import DataTable from '@components/admin/DataTable'
import type { Column } from '@components/admin/DataTable'
import ApiErrorAlert from '@components/admin/ApiErrorAlert'
import ConfirmDialog from '@components/admin/ConfirmDialog'

// Service Imports
import { notificationService } from '@/lib/services/notificationService'

// Hook Imports
import { useApiError } from '@/lib/hooks/useApiError'

// Type Imports
import type {
  NotificationChannel,
  NotificationHistoryItem,
  NotificationScheduledItem
} from '@/lib/types/admin'

// ─── Status chip helper ─────────────────────────────────────────────

const statusColor: Record<string, 'success' | 'error' | 'warning' | 'info' | 'default'> = {
  queued: 'info',
  sent: 'success',
  failed: 'error',
  scheduled: 'info',
  retry_pending: 'warning',
  cancelled: 'default'
}

function StatusChip({ status }: { status: string }) {
  return <Chip label={status} size='small' color={statusColor[status] ?? 'default'} variant='tonal' />
}

// ─── Column definitions ─────────────────────────────────────────────

const historyColumns: Column<NotificationHistoryItem>[] = [
  { id: 'title', label: 'Title', render: row => row.title },
  { id: 'channelKey', label: 'Channel', width: 140, render: row => row.channelKey },
  { id: 'status', label: 'Status', width: 120, render: row => <StatusChip status={row.status} /> },
  {
    id: 'createdAt',
    label: 'Sent',
    width: 180,
    render: row => new Date(row.createdAt).toLocaleString()
  }
]

const deadLetterColumns: Column<NotificationScheduledItem>[] = [
  { id: 'title', label: 'Title', render: row => row.title },
  { id: 'channelKey', label: 'Channel', width: 140, render: row => row.channelKey },
  { id: 'status', label: 'Status', width: 120, render: row => <StatusChip status={row.status} /> },
  {
    id: 'scheduledAt',
    label: 'Scheduled',
    width: 180,
    render: row => new Date(row.scheduledAt).toLocaleString()
  }
]

// ─── Main Component ─────────────────────────────────────────────────

const NotificationsView = () => {
  const { error, handleError, clearError, isRateLimited } = useApiError()
  const [tab, setTab] = useState('history')

  // Channels
  const [channels, setChannels] = useState<NotificationChannel[]>([])

  // History tab
  const [historyRows, setHistoryRows] = useState<NotificationHistoryItem[]>([])
  const [historyTotal, setHistoryTotal] = useState(0)
  const [historyPage, setHistoryPage] = useState(1)
  const [historyLoading, setHistoryLoading] = useState(false)

  // Dead-letter tab
  const [dlRows, setDlRows] = useState<NotificationScheduledItem[]>([])
  const [dlTotal, setDlTotal] = useState(0)
  const [dlPage, setDlPage] = useState(1)
  const [dlLoading, setDlLoading] = useState(false)

  // Replay dialog
  const [replayId, setReplayId] = useState<string | null>(null)
  const [replayLoading, setReplayLoading] = useState(false)

  // Send form
  const [sendChannel, setSendChannel] = useState('')
  const [sendTitle, setSendTitle] = useState('')
  const [sendBody, setSendBody] = useState('')
  const [sending, setSending] = useState(false)
  const [sendSuccess, setSendSuccess] = useState<string | null>(null)

  // ── Load channels ──
  useEffect(() => {
    async function load() {
      try {
        const res = await notificationService.channels()

        setChannels(res)
      } catch (err) {
        handleError(err)
      }
    }

    load()
  }, [handleError])

  // ── History ──
  const loadHistory = useCallback(async () => {
    setHistoryLoading(true)

    try {
      const res = await notificationService.history({ page: historyPage, pageSize: 25 })

      setHistoryRows(res.items)
      setHistoryTotal(res.totalItems)
    } catch (err) {
      handleError(err)
    } finally {
      setHistoryLoading(false)
    }
  }, [historyPage, handleError])

  useEffect(() => {
    if (tab === 'history') loadHistory()
  }, [tab, loadHistory])

  // ── Dead-letter ──
  const loadDeadLetter = useCallback(async () => {
    setDlLoading(true)

    try {
      const res = await notificationService.deadLetter({ page: dlPage, pageSize: 25 })

      setDlRows(res.items)
      setDlTotal(res.totalItems)
    } catch (err) {
      handleError(err)
    } finally {
      setDlLoading(false)
    }
  }, [dlPage, handleError])

  useEffect(() => {
    if (tab === 'dead-letter') loadDeadLetter()
  }, [tab, loadDeadLetter])

  // ── Replay ──
  const handleReplay = async () => {
    if (!replayId || isRateLimited) return
    setReplayLoading(true)

    try {
      await notificationService.replay(replayId)
      setReplayId(null)
      await loadDeadLetter()
    } catch (err) {
      handleError(err)
    } finally {
      setReplayLoading(false)
    }
  }

  // ── Send ──
  const handleSend = async () => {
    if (isRateLimited || !sendChannel || !sendTitle) return
    setSending(true)
    setSendSuccess(null)

    try {
      const res = await notificationService.send({
        title: sendTitle,
        body: sendBody,
        channelKey: sendChannel,
        audience: {}
      })

      setSendSuccess(`Notification queued (Job ID: ${res.jobId})`)
      setSendTitle('')
      setSendBody('')
    } catch (err) {
      handleError(err)
    } finally {
      setSending(false)
    }
  }

  return (
    <>
      <PageHeader title='Notifications' />
      <ApiErrorAlert error={error} onClose={clearError} />

      <Card>
        <TabContext value={tab}>
          <TabList onChange={(_, v) => setTab(v)} sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}>
            <Tab label='History' value='history' />
            <Tab label='Dead Letter' value='dead-letter' />
            <Tab label='Send' value='send' />
          </TabList>

          {/* ── History Tab ── */}
          <TabPanel value='history' sx={{ p: 0 }}>
            <DataTable
              columns={historyColumns}
              rows={historyRows}
              rowKey={row => row.id}
              loading={historyLoading}
              page={historyPage}
              pageSize={25}
              total={historyTotal}
              onPageChange={setHistoryPage}
              emptyMessage='No notification history'
            />
          </TabPanel>

          {/* ── Dead Letter Tab ── */}
          <TabPanel value='dead-letter' sx={{ p: 0 }}>
            <DataTable
              columns={[
                ...deadLetterColumns,
                {
                  id: 'actions',
                  label: '',
                  width: 120,
                  align: 'center' as const,
                  render: (row: NotificationScheduledItem) => (
                    <Button
                      size='small'
                      variant='outlined'
                      disabled={isRateLimited}
                      onClick={() => setReplayId(row.scheduleId)}
                    >
                      Replay
                    </Button>
                  )
                }
              ]}
              rows={dlRows}
              rowKey={row => row.scheduleId}
              loading={dlLoading}
              page={dlPage}
              pageSize={25}
              total={dlTotal}
              onPageChange={setDlPage}
              emptyMessage='No dead-letter items'
            />
          </TabPanel>

          {/* ── Send Tab ── */}
          <TabPanel value='send'>
            <CardContent sx={{ maxWidth: 520 }}>
              <Typography variant='subtitle1' sx={{ mb: 2 }}>
                Send Notification
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <TextField
                  select
                  label='Channel'
                  value={sendChannel}
                  onChange={e => setSendChannel(e.target.value)}
                  size='small'
                  fullWidth
                >
                  {channels.map(ch => (
                    <MenuItem key={ch.key} value={ch.key}>
                      {ch.name}
                    </MenuItem>
                  ))}
                </TextField>
                <TextField
                  label='Title'
                  value={sendTitle}
                  onChange={e => setSendTitle(e.target.value)}
                  size='small'
                  fullWidth
                />
                <TextField
                  label='Body'
                  value={sendBody}
                  onChange={e => setSendBody(e.target.value)}
                  size='small'
                  fullWidth
                  multiline
                  rows={3}
                />
                <Button
                  variant='contained'
                  onClick={handleSend}
                  disabled={sending || isRateLimited || !sendChannel || !sendTitle}
                >
                  {sending ? 'Sending...' : isRateLimited ? 'Please wait...' : 'Send'}
                </Button>
                {sendSuccess && (
                  <Chip label={sendSuccess} color='success' variant='tonal' onDelete={() => setSendSuccess(null)} />
                )}
              </Box>
            </CardContent>
          </TabPanel>
        </TabContext>
      </Card>

      {/* ── Replay Confirmation ── */}
      <ConfirmDialog
        open={!!replayId}
        title='Replay Notification'
        message='This will re-schedule the failed notification for delivery. Are you sure?'
        confirmLabel='Replay'
        confirmColor='primary'
        onConfirm={handleReplay}
        onCancel={() => setReplayId(null)}
        loading={replayLoading}
      />
    </>
  )
}

export default NotificationsView
