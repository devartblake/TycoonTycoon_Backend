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
import ConfirmDialog from '@components/admin/ConfirmDialog'

// Service & Hook Imports
import { economyService } from '@/lib/services/economyService'
import { useApiError } from '@/lib/hooks/useApiError'

// Type Imports
import type {
  EconomyTxnListItem,
  EconomyTxnResult,
  EconomyLine
} from '@/lib/types/admin'
import { CurrencyType } from '@/lib/types/admin'

// ─── Helpers ────────────────────────────────────────────────────────

const currencyLabel: Record<number, string> = {
  [CurrencyType.Xp]: 'Neural XP',
  [CurrencyType.Coins]: 'Credits',
  [CurrencyType.Diamonds]: 'Diamonds'
}

function formatLines(lines: EconomyLine[]): string {
  return lines.map(l => {
    const sign = l.delta >= 0 ? '+' : ''

    return `${sign}${l.delta} ${currencyLabel[l.currency] ?? l.currency}`
  }).join(', ')
}

const statusLabel: Record<number, string> = {
  1: 'Applied',
  2: 'Duplicate',
  3: 'Insufficient Funds',
  4: 'Invalid'
}

const statusColor: Record<number, 'success' | 'warning' | 'error' | 'info'> = {
  1: 'success',
  2: 'info',
  3: 'error',
  4: 'error'
}

function generateEventId(): string {
  return crypto.randomUUID()
}

// ─── History columns ────────────────────────────────────────────────

const historyColumns: Column<EconomyTxnListItem>[] = [
  {
    id: 'createdAtUtc',
    label: 'Date',
    width: 180,
    render: row => new Date(row.createdAtUtc).toLocaleString()
  },
  { id: 'kind', label: 'Kind', width: 180, render: row => row.kind },
  { id: 'lines', label: 'Deltas', render: row => formatLines(row.lines) },
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

const EconomyView = () => {
  const { error, handleError, clearError, isRateLimited } = useApiError()
  const [tab, setTab] = useState('history')

  // ── History tab state ──
  const [historyPlayerId, setHistoryPlayerId] = useState('')
  const [historySearchId, setHistorySearchId] = useState('')
  const [historyRows, setHistoryRows] = useState<EconomyTxnListItem[]>([])
  const [historyTotal, setHistoryTotal] = useState(0)
  const [historyPage, setHistoryPage] = useState(1)
  const [historyLoading, setHistoryLoading] = useState(false)

  // ── Create transaction tab state ──
  const [txnPlayerId, setTxnPlayerId] = useState('')
  const [txnKind, setTxnKind] = useState('')
  const [txnNote, setTxnNote] = useState('')
  const [txnXp, setTxnXp] = useState('')
  const [txnCoins, setTxnCoins] = useState('')
  const [txnDiamonds, setTxnDiamonds] = useState('')
  const [txnSubmitting, setTxnSubmitting] = useState(false)
  const [txnResult, setTxnResult] = useState<EconomyTxnResult | null>(null)

  // ── Rollback tab state ──
  const [rollbackEventId, setRollbackEventId] = useState('')
  const [rollbackReason, setRollbackReason] = useState('')
  const [rollbackSubmitting, setRollbackSubmitting] = useState(false)
  const [rollbackResult, setRollbackResult] = useState<EconomyTxnResult | null>(null)
  const [rollbackConfirmOpen, setRollbackConfirmOpen] = useState(false)

  // ── Load history ──
  const loadHistory = useCallback(async (playerId: string, page: number) => {
    if (!playerId) return
    setHistoryLoading(true)

    try {
      const res = await economyService.history(playerId, { page, pageSize: 25 })

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

  // ── Create transaction ──
  const handleCreate = async () => {
    if (isRateLimited) return

    const lines: EconomyLine[] = []
    const xp = parseInt(txnXp, 10)
    const coins = parseInt(txnCoins, 10)
    const diamonds = parseInt(txnDiamonds, 10)

    if (!isNaN(xp) && xp !== 0) lines.push({ currency: CurrencyType.Xp, delta: xp })
    if (!isNaN(coins) && coins !== 0) lines.push({ currency: CurrencyType.Coins, delta: coins })
    if (!isNaN(diamonds) && diamonds !== 0) lines.push({ currency: CurrencyType.Diamonds, delta: diamonds })

    if (lines.length === 0 || !txnPlayerId.trim() || !txnKind.trim()) return

    setTxnSubmitting(true)
    setTxnResult(null)

    try {
      const res = await economyService.createTransaction({
        eventId: generateEventId(),
        playerId: txnPlayerId.trim(),
        kind: txnKind.trim(),
        lines,
        note: txnNote.trim() || undefined
      })

      setTxnResult(res)
    } catch (err) {
      handleError(err)
    } finally {
      setTxnSubmitting(false)
    }
  }

  // ── Rollback ──
  const handleRollback = async () => {
    if (isRateLimited || !rollbackEventId.trim() || !rollbackReason.trim()) return
    setRollbackSubmitting(true)
    setRollbackResult(null)
    setRollbackConfirmOpen(false)

    try {
      const res = await economyService.rollback(rollbackEventId.trim(), rollbackReason.trim())

      setRollbackResult(res)
    } catch (err) {
      handleError(err)
    } finally {
      setRollbackSubmitting(false)
    }
  }

  return (
    <>
      <PageHeader title='Economy Transactions' />
      <ApiErrorAlert error={error} onClose={clearError} />

      <Card>
        <TabContext value={tab}>
          <TabList onChange={(_, v) => setTab(v)} sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}>
            <Tab label='History' value='history' />
            <Tab label='Create Transaction' value='create' />
            <Tab label='Rollback' value='rollback' />
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

          {/* ── Create Transaction Tab ── */}
          <TabPanel value='create'>
            <CardContent sx={{ maxWidth: 520 }}>
              <Typography variant='subtitle1' sx={{ mb: 2 }}>
                Create Economy Transaction
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <TextField
                  label='Player ID'
                  value={txnPlayerId}
                  onChange={e => setTxnPlayerId(e.target.value)}
                  size='small'
                  fullWidth
                  placeholder='Player UUID'
                />
                <TextField
                  label='Kind'
                  value={txnKind}
                  onChange={e => setTxnKind(e.target.value)}
                  size='small'
                  fullWidth
                  placeholder='e.g. admin-grant, referral-redeem'
                />
                <Typography variant='caption' color='text.secondary'>
                  Currency deltas (positive = credit, negative = debit). At least one must be non-zero.
                </Typography>
                <Box sx={{ display: 'flex', gap: 2 }}>
                  <TextField
                    label='Neural XP'
                    type='number'
                    value={txnXp}
                    onChange={e => setTxnXp(e.target.value)}
                    size='small'
                    sx={{ flex: 1 }}
                  />
                  <TextField
                    label='Credits'
                    type='number'
                    value={txnCoins}
                    onChange={e => setTxnCoins(e.target.value)}
                    size='small'
                    sx={{ flex: 1 }}
                  />
                  <TextField
                    label='Diamonds'
                    type='number'
                    value={txnDiamonds}
                    onChange={e => setTxnDiamonds(e.target.value)}
                    size='small'
                    sx={{ flex: 1 }}
                  />
                </Box>
                <TextField
                  label='Note (optional)'
                  value={txnNote}
                  onChange={e => setTxnNote(e.target.value)}
                  size='small'
                  fullWidth
                  multiline
                  rows={2}
                />
                <Button
                  variant='contained'
                  onClick={handleCreate}
                  disabled={txnSubmitting || isRateLimited || !txnPlayerId.trim() || !txnKind.trim()}
                >
                  {txnSubmitting ? 'Submitting...' : 'Create Transaction'}
                </Button>
                {txnResult && (
                  <Card variant='outlined'>
                    <CardContent>
                      <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', mb: 1 }}>
                        <Typography variant='subtitle2'>Result:</Typography>
                        <Chip
                          label={statusLabel[txnResult.status] ?? txnResult.status}
                          size='small'
                          color={statusColor[txnResult.status] ?? 'default'}
                          variant='tonal'
                        />
                      </Box>
                      {txnResult.status === 1 && (
                        <Typography variant='body2' color='text.secondary'>
                          Balances — Neural XP: {txnResult.balanceXp}, Credits: {txnResult.balanceCoins}, Diamonds: {txnResult.balanceDiamonds}
                        </Typography>
                      )}
                    </CardContent>
                  </Card>
                )}
              </Box>
            </CardContent>
          </TabPanel>

          {/* ── Rollback Tab ── */}
          <TabPanel value='rollback'>
            <CardContent sx={{ maxWidth: 520 }}>
              <Typography variant='subtitle1' sx={{ mb: 2 }}>
                Rollback Transaction
              </Typography>
              <Typography variant='body2' color='text.secondary' sx={{ mb: 2 }}>
                Creates a counter-transaction that negates the original. The original Event ID must exist and must not have been rolled back already.
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <TextField
                  label='Original Event ID'
                  value={rollbackEventId}
                  onChange={e => setRollbackEventId(e.target.value)}
                  size='small'
                  fullWidth
                  placeholder='UUID of the transaction to reverse'
                />
                <TextField
                  label='Reason'
                  value={rollbackReason}
                  onChange={e => setRollbackReason(e.target.value)}
                  size='small'
                  fullWidth
                  multiline
                  rows={2}
                  placeholder='Why is this being rolled back?'
                />
                <Button
                  variant='contained'
                  color='warning'
                  onClick={() => setRollbackConfirmOpen(true)}
                  disabled={rollbackSubmitting || isRateLimited || !rollbackEventId.trim() || !rollbackReason.trim()}
                >
                  {rollbackSubmitting ? 'Rolling back...' : 'Rollback'}
                </Button>
                {rollbackResult && (
                  <Card variant='outlined'>
                    <CardContent>
                      <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', mb: 1 }}>
                        <Typography variant='subtitle2'>Result:</Typography>
                        <Chip
                          label={statusLabel[rollbackResult.status] ?? rollbackResult.status}
                          size='small'
                          color={statusColor[rollbackResult.status] ?? 'default'}
                          variant='tonal'
                        />
                      </Box>
                      {rollbackResult.status === 1 && (
                        <Typography variant='body2' color='text.secondary'>
                          Updated balances — Neural XP: {rollbackResult.balanceXp}, Credits: {rollbackResult.balanceCoins}, Diamonds: {rollbackResult.balanceDiamonds}
                        </Typography>
                      )}
                    </CardContent>
                  </Card>
                )}
              </Box>
            </CardContent>
          </TabPanel>
        </TabContext>
      </Card>

      {/* ── Rollback Confirmation ── */}
      <ConfirmDialog
        open={rollbackConfirmOpen}
        title='Confirm Rollback'
        message={`This will create a counter-transaction reversing Event ID "${rollbackEventId.slice(0, 8)}...". This action cannot be undone. Continue?`}
        confirmLabel='Rollback'
        confirmColor='warning'
        onConfirm={handleRollback}
        onCancel={() => setRollbackConfirmOpen(false)}
        loading={rollbackSubmitting}
      />
    </>
  )
}

export default EconomyView
