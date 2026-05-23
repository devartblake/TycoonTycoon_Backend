'use client'

import { useCallback, useEffect, useState } from 'react'

// MUI Imports
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Card from '@mui/material/Card'
import Chip from '@mui/material/Chip'
import Drawer from '@mui/material/Drawer'
import IconButton from '@mui/material/IconButton'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'

// Component Imports
import PageHeader from '@components/admin/PageHeader'
import DataTable from '@components/admin/DataTable'
import type { Column } from '@components/admin/DataTable'
import ApiErrorAlert from '@components/admin/ApiErrorAlert'

// Service & Hook Imports
import { auditService } from '@/lib/services/auditService'
import { useApiError } from '@/lib/hooks/useApiError'

// Type Imports
import type { NotificationHistoryItem } from '@/lib/types/admin'

// ─── Status chip helper ─────────────────────────────────────────────

const statusColor: Record<string, 'success' | 'error' | 'warning' | 'info' | 'default'> = {
  success: 'success',
  unauthorized: 'error',
  forbidden: 'error',
  error: 'error',
  not_found: 'warning',
  conflict: 'warning',
  accepted: 'info',
  created: 'info',
  queued: 'info',
  validation_error: 'warning'
}

function StatusChip({ status }: { status: string }) {
  return <Chip label={status} size='small' color={statusColor[status] ?? 'default'} variant='tonal' />
}

// ─── Prebuilt triage filters ────────────────────────────────────────

interface TriagePreset {
  label: string
  status: string
}

const triagePresets: TriagePreset[] = [
  { label: 'Unauthorized spikes', status: 'unauthorized' },
  { label: 'Forbidden spikes', status: 'forbidden' },
  { label: 'Rate-limit bursts', status: 'rate_limited' }
]

// ─── Column definitions ─────────────────────────────────────────────

const columns: Column<NotificationHistoryItem>[] = [
  {
    id: 'createdAt',
    label: 'Timestamp',
    width: 180,
    render: row => new Date(row.createdAt).toLocaleString()
  },
  { id: 'title', label: 'Operation', render: row => row.title },
  { id: 'status', label: 'Status', width: 140, render: row => <StatusChip status={row.status} /> },
  {
    id: 'actor',
    label: 'Actor',
    width: 200,
    render: row => {
      const actor = row.metadata?.['actor'] ?? row.metadata?.['email']

      return actor ? String(actor) : '—'
    }
  }
]

// ─── Helper: format date for datetime-local input ───────────────────

function toDateTimeLocal(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')

  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

// ─── Main Component ─────────────────────────────────────────────────

const SecurityAuditView = () => {
  const { error, handleError, clearError } = useApiError()

  // Filters
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [statusFilter, setStatusFilter] = useState('')

  // Data
  const [rows, setRows] = useState<NotificationHistoryItem[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize] = useState(25)
  const [loading, setLoading] = useState(false)

  // Detail drawer
  const [selected, setSelected] = useState<NotificationHistoryItem | null>(null)

  // ── Load data ──
  const loadData = useCallback(async () => {
    setLoading(true)

    try {
      const params: Record<string, unknown> = { page, pageSize }

      if (fromDate) params.from = new Date(fromDate).toISOString()
      if (toDate) params.to = new Date(toDate).toISOString()
      if (statusFilter) params.status = statusFilter

      const res = await auditService.securityEvents(params as Parameters<typeof auditService.securityEvents>[0])

      setRows(res.items)
      setTotal(res.totalItems)
    } catch (err) {
      handleError(err)
    } finally {
      setLoading(false)
    }
  }, [page, pageSize, fromDate, toDate, statusFilter, handleError])

  useEffect(() => {
    loadData()
  }, [loadData])

  // ── Apply triage preset ──
  const applyPreset = (preset: TriagePreset) => {
    setStatusFilter(preset.status)
    setPage(1)

    // Default to last 24 hours for triage presets
    const now = new Date()
    const yesterday = new Date(now.getTime() - 24 * 60 * 60 * 1000)

    setFromDate(toDateTimeLocal(yesterday))
    setToDate(toDateTimeLocal(now))
  }

  // ── Clear filters ──
  const clearFilters = () => {
    setFromDate('')
    setToDate('')
    setStatusFilter('')
    setPage(1)
  }

  return (
    <>
      <PageHeader title='Security Audit' />
      <ApiErrorAlert error={error} onClose={clearError} />

      {/* ── Triage presets ── */}
      <Stack direction='row' spacing={1} sx={{ mb: 2 }}>
        {triagePresets.map(preset => (
          <Chip
            key={preset.status}
            label={preset.label}
            variant={statusFilter === preset.status ? 'filled' : 'outlined'}
            color={statusFilter === preset.status ? 'primary' : 'default'}
            onClick={() => applyPreset(preset)}
            size='small'
          />
        ))}
      </Stack>

      {/* ── Filters ── */}
      <Card sx={{ mb: 2 }}>
        <Box sx={{ display: 'flex', gap: 2, p: 2, flexWrap: 'wrap', alignItems: 'center' }}>
          <TextField
            label='From'
            type='datetime-local'
            value={fromDate}
            onChange={e => { setFromDate(e.target.value); setPage(1) }}
            size='small'
            InputLabelProps={{ shrink: true }}
            sx={{ minWidth: 200 }}
          />
          <TextField
            label='To'
            type='datetime-local'
            value={toDate}
            onChange={e => { setToDate(e.target.value); setPage(1) }}
            size='small'
            InputLabelProps={{ shrink: true }}
            sx={{ minWidth: 200 }}
          />
          <TextField
            select
            label='Status'
            value={statusFilter}
            onChange={e => { setStatusFilter(e.target.value); setPage(1) }}
            size='small'
            sx={{ minWidth: 160 }}
          >
            <MenuItem value=''>All statuses</MenuItem>
            <MenuItem value='success'>Success</MenuItem>
            <MenuItem value='unauthorized'>Unauthorized</MenuItem>
            <MenuItem value='forbidden'>Forbidden</MenuItem>
            <MenuItem value='error'>Error</MenuItem>
            <MenuItem value='not_found'>Not Found</MenuItem>
            <MenuItem value='conflict'>Conflict</MenuItem>
            <MenuItem value='rate_limited'>Rate Limited</MenuItem>
          </TextField>
          <Button variant='text' size='small' onClick={clearFilters}>
            Clear
          </Button>
        </Box>
      </Card>

      {/* ── Data table ── */}
      <DataTable
        columns={columns}
        rows={rows}
        rowKey={row => row.id}
        loading={loading}
        page={page}
        pageSize={pageSize}
        total={total}
        onPageChange={setPage}
        onRowClick={setSelected}
        emptyMessage='No security audit events'
      />

      {/* ── Detail Drawer ── */}
      <Drawer
        anchor='right'
        open={!!selected}
        onClose={() => setSelected(null)}
        PaperProps={{ sx: { width: 400, p: 3 } }}
      >
        {selected && (
          <>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant='h6'>Event Detail</Typography>
              <IconButton onClick={() => setSelected(null)} size='small'>
                <i className='ri-close-line' />
              </IconButton>
            </Box>

            <Stack spacing={2}>
              <Box>
                <Typography variant='caption' color='text.secondary'>ID</Typography>
                <Typography variant='body2' sx={{ fontFamily: 'monospace', wordBreak: 'break-all' }}>{selected.id}</Typography>
              </Box>
              <Box>
                <Typography variant='caption' color='text.secondary'>Operation</Typography>
                <Typography variant='body2'>{selected.title}</Typography>
              </Box>
              <Box>
                <Typography variant='caption' color='text.secondary'>Status</Typography>
                <Box sx={{ mt: 0.5 }}><StatusChip status={selected.status} /></Box>
              </Box>
              <Box>
                <Typography variant='caption' color='text.secondary'>Timestamp</Typography>
                <Typography variant='body2'>{new Date(selected.createdAt).toLocaleString()}</Typography>
              </Box>
              {selected.metadata && (
                <Box>
                  <Typography variant='caption' color='text.secondary'>Metadata</Typography>
                  <Box
                    component='pre'
                    sx={{
                      mt: 0.5,
                      p: 1.5,
                      borderRadius: 1,
                      bgcolor: 'action.hover',
                      fontSize: '0.8rem',
                      fontFamily: 'monospace',
                      overflow: 'auto',
                      maxHeight: 300,
                      whiteSpace: 'pre-wrap',
                      wordBreak: 'break-all'
                    }}
                  >
                    {JSON.stringify(selected.metadata, null, 2)}
                  </Box>
                </Box>
              )}
            </Stack>
          </>
        )}
      </Drawer>
    </>
  )
}

export default SecurityAuditView
