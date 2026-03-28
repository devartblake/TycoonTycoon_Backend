'use client'

import { useCallback, useEffect, useState } from 'react'

// MUI Imports
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import Chip from '@mui/material/Chip'
import Dialog from '@mui/material/Dialog'
import DialogTitle from '@mui/material/DialogTitle'
import DialogContent from '@mui/material/DialogContent'
import DialogActions from '@mui/material/DialogActions'
import Divider from '@mui/material/Divider'
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
import { playerTransactionService } from '@/lib/services/playerTransactionService'
import { useApiError } from '@/lib/hooks/useApiError'

// Type Imports
import type {
  PlayerTransactionListItem,
  PlayerTransactionDetail
} from '@/lib/types/admin'

// ─── Helpers ────────────────────────────────────────────────────────

const statusColor: Record<string, 'success' | 'warning' | 'error' | 'info' | 'default'> = {
  Applied: 'success',
  Pending: 'warning',
  Disputed: 'warning',
  Reversed: 'error',
  Failed: 'error'
}

// ─── History columns ────────────────────────────────────────────────

const historyColumns: Column<PlayerTransactionListItem>[] = [
  {
    id: 'createdAtUtc',
    label: 'Date',
    width: 170,
    render: row => new Date(row.createdAtUtc).toLocaleString()
  },
  { id: 'kind', label: 'Kind', width: 160, render: row => row.kind },
  {
    id: 'status',
    label: 'Status',
    width: 110,
    render: row => (
      <Chip label={row.status} size='small' color={statusColor[row.status] ?? 'default'} variant='tonal' />
    )
  },
  { id: 'actorCount', label: 'Actors', width: 70, render: row => row.actorCount },
  { id: 'economyTxnCount', label: 'Econ Txns', width: 90, render: row => row.economyTxnCount },
  { id: 'itemChangeCount', label: 'Items', width: 70, render: row => row.itemChangeCount },
  {
    id: 'correlatedEventId',
    label: 'Correlated',
    width: 140,
    render: row =>
      row.correlatedEventId ? (
        <Typography variant='body2' sx={{ fontFamily: 'monospace', fontSize: '0.7rem' }}>
          {row.correlatedEventId.slice(0, 8)}...
        </Typography>
      ) : (
        '—'
      )
  },
  {
    id: 'id',
    label: 'ID',
    width: 140,
    render: row => (
      <Typography variant='body2' sx={{ fontFamily: 'monospace', fontSize: '0.7rem' }}>
        {row.id.slice(0, 8)}...
      </Typography>
    )
  }
]

// ─── Main Component ─────────────────────────────────────────────────

const PlayerTransactionsView = () => {
  const { error, handleError, clearError, isRateLimited } = useApiError()
  const [tab, setTab] = useState('history')

  // ── History tab state ──
  const [filterPlayerId, setFilterPlayerId] = useState('')
  const [filterCorrelatedId, setFilterCorrelatedId] = useState('')
  const [searchPlayerId, setSearchPlayerId] = useState('')
  const [searchCorrelatedId, setSearchCorrelatedId] = useState('')
  const [rows, setRows] = useState<PlayerTransactionListItem[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(false)

  // ── Detail dialog state ──
  const [detailOpen, setDetailOpen] = useState(false)
  const [detail, setDetail] = useState<PlayerTransactionDetail | null>(null)
  const [detailLoading, setDetailLoading] = useState(false)

  // ── Dispute state ──
  const [disputeId, setDisputeId] = useState('')
  const [disputeReason, setDisputeReason] = useState('')
  const [disputeSubmitting, setDisputeSubmitting] = useState(false)
  const [disputeConfirmOpen, setDisputeConfirmOpen] = useState(false)
  const [disputeResult, setDisputeResult] = useState<string | null>(null)

  // ── Reverse state ──
  const [reverseId, setReverseId] = useState('')
  const [reverseReason, setReverseReason] = useState('')
  const [reverseSubmitting, setReverseSubmitting] = useState(false)
  const [reverseConfirmOpen, setReverseConfirmOpen] = useState(false)
  const [reverseResult, setReverseResult] = useState<string | null>(null)

  // ── Load history ──
  const loadHistory = useCallback(
    async (playerId: string, correlatedId: string, p: number) => {
      setLoading(true)

      try {
        const res = await playerTransactionService.history({
          playerId: playerId || undefined,
          correlatedEventId: correlatedId || undefined,
          page: p,
          pageSize: 25
        })

        setRows(res.items)
        setTotal(res.total)
      } catch (err) {
        handleError(err)
      } finally {
        setLoading(false)
      }
    },
    [handleError]
  )

  useEffect(() => {
    if (tab === 'history') {
      loadHistory(searchPlayerId, searchCorrelatedId, page)
    }
  }, [tab, searchPlayerId, searchCorrelatedId, page, loadHistory])

  // ── Load detail ──
  const openDetail = async (id: string) => {
    setDetailLoading(true)
    setDetailOpen(true)
    setDetail(null)

    try {
      const res = await playerTransactionService.detail(id)

      setDetail(res)
    } catch (err) {
      handleError(err)
      setDetailOpen(false)
    } finally {
      setDetailLoading(false)
    }
  }

  // ── Dispute ──
  const handleDispute = async () => {
    if (isRateLimited || !disputeId.trim() || !disputeReason.trim()) return
    setDisputeSubmitting(true)
    setDisputeResult(null)
    setDisputeConfirmOpen(false)

    try {
      await playerTransactionService.dispute({
        playerTransactionId: disputeId.trim(),
        reason: disputeReason.trim()
      })

      setDisputeResult('Disputed successfully')
    } catch (err) {
      handleError(err)
    } finally {
      setDisputeSubmitting(false)
    }
  }

  // ── Reverse ──
  const handleReverse = async () => {
    if (isRateLimited || !reverseId.trim() || !reverseReason.trim()) return
    setReverseSubmitting(true)
    setReverseResult(null)
    setReverseConfirmOpen(false)

    try {
      await playerTransactionService.reverse({
        playerTransactionId: reverseId.trim(),
        reason: reverseReason.trim()
      })

      setReverseResult('Reversed successfully')
    } catch (err) {
      handleError(err)
    } finally {
      setReverseSubmitting(false)
    }
  }

  // History columns with click-to-detail
  const clickableColumns: Column<PlayerTransactionListItem>[] = historyColumns.map(col =>
    col.id === 'id'
      ? {
          ...col,
          render: (row: PlayerTransactionListItem) => (
            <Button
              size='small'
              variant='text'
              sx={{ fontFamily: 'monospace', fontSize: '0.7rem', textTransform: 'none', p: 0, minWidth: 0 }}
              onClick={() => openDetail(row.id)}
            >
              {row.id.slice(0, 8)}...
            </Button>
          )
        }
      : col
  )

  return (
    <>
      <PageHeader title='Player Transactions' />
      <ApiErrorAlert error={error} onClose={clearError} />

      <Card>
        <TabContext value={tab}>
          <TabList onChange={(_, v) => setTab(v)} sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}>
            <Tab label='History' value='history' />
            <Tab label='Dispute' value='dispute' />
            <Tab label='Reverse' value='reverse' />
          </TabList>

          {/* ── History Tab ── */}
          <TabPanel value='history' sx={{ p: 0 }}>
            <Box sx={{ p: 2, display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
              <TextField
                label='Player ID (optional)'
                value={filterPlayerId}
                onChange={e => setFilterPlayerId(e.target.value)}
                size='small'
                sx={{ minWidth: 280 }}
                placeholder='Filter by player UUID'
              />
              <TextField
                label='Correlated Event ID (optional)'
                value={filterCorrelatedId}
                onChange={e => setFilterCorrelatedId(e.target.value)}
                size='small'
                sx={{ minWidth: 280 }}
                placeholder='Filter by match/event UUID'
              />
              <Button
                variant='contained'
                size='small'
                disabled={loading}
                onClick={() => {
                  setPage(1)
                  setSearchPlayerId(filterPlayerId.trim())
                  setSearchCorrelatedId(filterCorrelatedId.trim())
                }}
              >
                Search
              </Button>
            </Box>
            <DataTable
              columns={clickableColumns}
              rows={rows}
              rowKey={row => row.id}
              loading={loading}
              page={page}
              pageSize={25}
              total={total}
              onPageChange={setPage}
              emptyMessage='No player transactions found'
            />
          </TabPanel>

          {/* ── Dispute Tab ── */}
          <TabPanel value='dispute'>
            <CardContent sx={{ maxWidth: 520 }}>
              <Typography variant='subtitle1' sx={{ mb: 2 }}>
                Dispute Transaction
              </Typography>
              <Typography variant='body2' color='text.secondary' sx={{ mb: 2 }}>
                Mark a player transaction as disputed. The transaction must be in Applied status.
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <TextField
                  label='Player Transaction ID'
                  value={disputeId}
                  onChange={e => setDisputeId(e.target.value)}
                  size='small'
                  fullWidth
                />
                <TextField
                  label='Reason'
                  value={disputeReason}
                  onChange={e => setDisputeReason(e.target.value)}
                  size='small'
                  fullWidth
                  multiline
                  rows={2}
                />
                <Button
                  variant='contained'
                  color='warning'
                  onClick={() => setDisputeConfirmOpen(true)}
                  disabled={disputeSubmitting || isRateLimited || !disputeId.trim() || !disputeReason.trim()}
                >
                  {disputeSubmitting ? 'Disputing...' : 'Dispute'}
                </Button>
                {disputeResult && (
                  <Chip label={disputeResult} color='success' variant='tonal' />
                )}
              </Box>
            </CardContent>
          </TabPanel>

          {/* ── Reverse Tab ── */}
          <TabPanel value='reverse'>
            <CardContent sx={{ maxWidth: 520 }}>
              <Typography variant='subtitle1' sx={{ mb: 2 }}>
                Reverse Transaction
              </Typography>
              <Typography variant='body2' color='text.secondary' sx={{ mb: 2 }}>
                Fully reverses a player transaction: rolls back all child economy transactions and
                reverts item changes. This cannot be undone.
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <TextField
                  label='Player Transaction ID'
                  value={reverseId}
                  onChange={e => setReverseId(e.target.value)}
                  size='small'
                  fullWidth
                />
                <TextField
                  label='Reason'
                  value={reverseReason}
                  onChange={e => setReverseReason(e.target.value)}
                  size='small'
                  fullWidth
                  multiline
                  rows={2}
                />
                <Button
                  variant='contained'
                  color='error'
                  onClick={() => setReverseConfirmOpen(true)}
                  disabled={reverseSubmitting || isRateLimited || !reverseId.trim() || !reverseReason.trim()}
                >
                  {reverseSubmitting ? 'Reversing...' : 'Reverse'}
                </Button>
                {reverseResult && (
                  <Chip label={reverseResult} color='success' variant='tonal' />
                )}
              </Box>
            </CardContent>
          </TabPanel>
        </TabContext>
      </Card>

      {/* ── Detail Dialog ── */}
      <Dialog open={detailOpen} onClose={() => setDetailOpen(false)} maxWidth='md' fullWidth>
        <DialogTitle>Transaction Detail</DialogTitle>
        <DialogContent>
          {detailLoading && <Typography>Loading...</Typography>}
          {detail && (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
              <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                <Chip label={detail.status} color={statusColor[detail.status] ?? 'default'} variant='tonal' />
                <Chip label={detail.kind} variant='outlined' size='small' />
              </Box>

              <Typography variant='body2'>
                <strong>ID:</strong> {detail.id}
              </Typography>
              <Typography variant='body2'>
                <strong>Event ID:</strong> {detail.eventId}
              </Typography>
              {detail.correlatedEventId && (
                <Typography variant='body2'>
                  <strong>Correlated Event:</strong> {detail.correlatedEventId}
                </Typography>
              )}
              {detail.receipt && (
                <Typography variant='body2'>
                  <strong>Receipt:</strong> {detail.receipt}
                </Typography>
              )}
              {detail.disputeReason && (
                <Typography variant='body2' color='warning.main'>
                  <strong>Dispute Reason:</strong> {detail.disputeReason}
                </Typography>
              )}

              <Divider />

              <Typography variant='subtitle2'>Actors ({detail.actors.length})</Typography>
              {detail.actors.map((a, i) => (
                <Typography key={i} variant='body2' sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>
                  {a.role} — {a.playerId} ({a.allocationPercent}%)
                </Typography>
              ))}

              {detail.economyTransactions.length > 0 && (
                <>
                  <Divider />
                  <Typography variant='subtitle2'>Economy Transactions ({detail.economyTransactions.length})</Typography>
                  {detail.economyTransactions.map((e, i) => (
                    <Box key={i} sx={{ pl: 1 }}>
                      <Typography variant='body2'>
                        <strong>{e.kind}</strong> — {e.lines.map(l => `${l.delta >= 0 ? '+' : ''}${l.delta} ${l.currency}`).join(', ')}
                      </Typography>
                      <Typography variant='caption' color='text.secondary'>
                        {new Date(e.createdAtUtc).toLocaleString()} — {e.eventId.slice(0, 8)}...
                      </Typography>
                    </Box>
                  ))}
                </>
              )}

              {detail.itemChanges.length > 0 && (
                <>
                  <Divider />
                  <Typography variant='subtitle2'>Item Changes ({detail.itemChanges.length})</Typography>
                  {detail.itemChanges.map((item, i) => (
                    <Typography key={i} variant='body2'>
                      {item.operation} {item.quantity}x {item.itemType}
                    </Typography>
                  ))}
                </>
              )}
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDetailOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* ── Confirmations ── */}
      <ConfirmDialog
        open={disputeConfirmOpen}
        title='Confirm Dispute'
        message={`Mark transaction "${disputeId.slice(0, 8)}..." as disputed?`}
        confirmLabel='Dispute'
        confirmColor='warning'
        onConfirm={handleDispute}
        onCancel={() => setDisputeConfirmOpen(false)}
        loading={disputeSubmitting}
      />
      <ConfirmDialog
        open={reverseConfirmOpen}
        title='Confirm Reverse'
        message={`This will roll back all economy transactions and revert item changes for "${reverseId.slice(0, 8)}...". This cannot be undone.`}
        confirmLabel='Reverse'
        confirmColor='error'
        onConfirm={handleReverse}
        onCancel={() => setReverseConfirmOpen(false)}
        loading={reverseSubmitting}
      />
    </>
  )
}

export default PlayerTransactionsView
