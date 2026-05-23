'use client'

import { useCallback, useEffect, useState } from 'react'

// MUI Imports
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import Chip from '@mui/material/Chip'
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

// Service & Hook Imports
import { seasonPointsService } from '@/lib/services/seasonPointsService'
import { useApiError } from '@/lib/hooks/useApiError'

// Type Imports
import type {
  SeasonPointTxnListItem,
  ApplySeasonPointsResult
} from '@/lib/types/admin'

// ─── Helpers ────────────────────────────────────────────────────────

function generateEventId(): string {
  return crypto.randomUUID()
}

// ─── History columns ────────────────────────────────────────────────

const historyColumns: Column<SeasonPointTxnListItem>[] = [
  {
    id: 'createdAtUtc',
    label: 'Date',
    width: 180,
    render: row => new Date(row.createdAtUtc).toLocaleString()
  },
  { id: 'kind', label: 'Kind', width: 150, render: row => row.kind },
  {
    id: 'delta',
    label: 'Delta',
    width: 100,
    render: row => (
      <Typography
        variant='body2'
        sx={{ color: row.delta >= 0 ? 'success.main' : 'error.main', fontWeight: 600 }}
      >
        {row.delta >= 0 ? '+' : ''}{row.delta}
      </Typography>
    )
  },
  {
    id: 'seasonId',
    label: 'Season',
    width: 140,
    render: row => (
      <Typography variant='body2' sx={{ fontFamily: 'monospace', fontSize: '0.7rem' }}>
        {row.seasonId.slice(0, 8)}...
      </Typography>
    )
  },
  { id: 'note', label: 'Note', render: row => row.note ?? '' },
  {
    id: 'eventId',
    label: 'Event ID',
    width: 140,
    render: row => (
      <Typography variant='body2' sx={{ fontFamily: 'monospace', fontSize: '0.7rem' }}>
        {row.eventId.slice(0, 8)}...
      </Typography>
    )
  }
]

// ─── Main Component ─────────────────────────────────────────────────

const SeasonPointsView = () => {
  const { error, handleError, clearError, isRateLimited } = useApiError()
  const [tab, setTab] = useState('history')

  // ── History tab state ──
  const [historyPlayerId, setHistoryPlayerId] = useState('')
  const [historySearchId, setHistorySearchId] = useState('')
  const [historyRows, setHistoryRows] = useState<SeasonPointTxnListItem[]>([])
  const [historyTotal, setHistoryTotal] = useState(0)
  const [historyPage, setHistoryPage] = useState(1)
  const [historyLoading, setHistoryLoading] = useState(false)

  // ── Apply points tab state ──
  const [applySeasonId, setApplySeasonId] = useState('')
  const [applyPlayerId, setApplyPlayerId] = useState('')
  const [applyKind, setApplyKind] = useState('admin-adjust')
  const [applyDelta, setApplyDelta] = useState('')
  const [applyNote, setApplyNote] = useState('')
  const [applySubmitting, setApplySubmitting] = useState(false)
  const [applyResult, setApplyResult] = useState<ApplySeasonPointsResult | null>(null)

  // ── Load history ──
  const loadHistory = useCallback(async (playerId: string, page: number) => {
    if (!playerId) return
    setHistoryLoading(true)

    try {
      const res = await seasonPointsService.history(playerId, { page, pageSize: 25 })

      setHistoryRows(res.items)
      setHistoryTotal(res.total)
    } catch (err) {
      handleError(err)
    } finally {
      setHistoryLoading(false)
    }
  }, [handleError])

  useEffect(() => {
    if (tab === 'history' && historySearchId) {
      loadHistory(historySearchId, historyPage)
    }
  }, [tab, historySearchId, historyPage, loadHistory])

  // ── Apply points ──
  const handleApply = async () => {
    if (isRateLimited) return

    const delta = parseInt(applyDelta, 10)

    if (isNaN(delta) || delta === 0 || !applySeasonId.trim() || !applyPlayerId.trim() || !applyKind.trim()) return

    setApplySubmitting(true)
    setApplyResult(null)

    try {
      const res = await seasonPointsService.applyPoints({
        eventId: generateEventId(),
        seasonId: applySeasonId.trim(),
        playerId: applyPlayerId.trim(),
        kind: applyKind.trim(),
        delta,
        note: applyNote.trim() || undefined
      })

      setApplyResult(res)
    } catch (err) {
      handleError(err)
    } finally {
      setApplySubmitting(false)
    }
  }

  return (
    <>
      <PageHeader title='Season Point Transactions' />
      <ApiErrorAlert error={error} onClose={clearError} />

      <Card>
        <TabContext value={tab}>
          <TabList onChange={(_, v) => setTab(v)} sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}>
            <Tab label='History' value='history' />
            <Tab label='Apply Points' value='apply' />
          </TabList>

          {/* ── History Tab ── */}
          <TabPanel value='history' sx={{ p: 0 }}>
            <Box sx={{ p: 2, display: 'flex', gap: 2, alignItems: 'center' }}>
              <TextField
                label='Player ID'
                value={historyPlayerId}
                onChange={e => setHistoryPlayerId(e.target.value)}
                size='small'
                sx={{ minWidth: 320 }}
                placeholder='Enter player UUID'
              />
              <Button
                variant='contained'
                size='small'
                disabled={!historyPlayerId.trim() || historyLoading}
                onClick={() => { setHistoryPage(1); setHistorySearchId(historyPlayerId.trim()) }}
              >
                Search
              </Button>
            </Box>
            <DataTable
              columns={historyColumns}
              rows={historyRows}
              rowKey={row => row.eventId}
              loading={historyLoading}
              page={historyPage}
              pageSize={25}
              total={historyTotal}
              onPageChange={setHistoryPage}
              emptyMessage={historyPlayerId.trim() ? 'No transactions found' : 'Enter a Player ID to search'}
            />
          </TabPanel>

          {/* ── Apply Points Tab ── */}
          <TabPanel value='apply'>
            <CardContent sx={{ maxWidth: 520 }}>
              <Typography variant='subtitle1' sx={{ mb: 2 }}>
                Apply Season Points
              </Typography>
              <Typography variant='body2' color='text.secondary' sx={{ mb: 2 }}>
                Manually adjust rank points for a player in a specific season. Use a negative delta to deduct points.
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <TextField
                  label='Season ID'
                  value={applySeasonId}
                  onChange={e => setApplySeasonId(e.target.value)}
                  size='small'
                  fullWidth
                  placeholder='Season UUID'
                />
                <TextField
                  label='Player ID'
                  value={applyPlayerId}
                  onChange={e => setApplyPlayerId(e.target.value)}
                  size='small'
                  fullWidth
                  placeholder='Player UUID'
                />
                <TextField
                  label='Kind'
                  value={applyKind}
                  onChange={e => setApplyKind(e.target.value)}
                  size='small'
                  fullWidth
                  placeholder='e.g. admin-adjust, match-result'
                />
                <TextField
                  label='Delta (points)'
                  type='number'
                  value={applyDelta}
                  onChange={e => setApplyDelta(e.target.value)}
                  size='small'
                  fullWidth
                  placeholder='Positive to add, negative to deduct'
                />
                <TextField
                  label='Note (optional)'
                  value={applyNote}
                  onChange={e => setApplyNote(e.target.value)}
                  size='small'
                  fullWidth
                  multiline
                  rows={2}
                />
                <Button
                  variant='contained'
                  onClick={handleApply}
                  disabled={applySubmitting || isRateLimited || !applySeasonId.trim() || !applyPlayerId.trim() || !applyDelta}
                >
                  {applySubmitting ? 'Submitting...' : 'Apply Points'}
                </Button>
                {applyResult && (
                  <Card variant='outlined'>
                    <CardContent>
                      <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', mb: 1 }}>
                        <Typography variant='subtitle2'>Result:</Typography>
                        <Chip
                          label={applyResult.status}
                          size='small'
                          color={applyResult.status === 'Applied' ? 'success' : 'info'}
                          variant='tonal'
                        />
                      </Box>
                      <Typography variant='body2' color='text.secondary'>
                        New Rank Points: {applyResult.newRankPoints}
                      </Typography>
                    </CardContent>
                  </Card>
                )}
              </Box>
            </CardContent>
          </TabPanel>
        </TabContext>
      </Card>
    </>
  )
}

export default SeasonPointsView
