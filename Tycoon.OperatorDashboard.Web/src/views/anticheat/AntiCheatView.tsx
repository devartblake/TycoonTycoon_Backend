'use client'

import { useCallback, useEffect, useState } from 'react'

// MUI Imports
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import FormControlLabel from '@mui/material/FormControlLabel'
import Grid from '@mui/material/Grid'
import Switch from '@mui/material/Switch'
import Typography from '@mui/material/Typography'

// Component Imports
import PageHeader from '@components/admin/PageHeader'
import SearchFilterBar from '@components/admin/SearchFilterBar'
import type { FilterDef } from '@components/admin/SearchFilterBar'
import DataTable from '@components/admin/DataTable'
import type { Column } from '@components/admin/DataTable'

// Service Imports
import { antiCheatService } from '@/lib/services/antiCheatService'

// Type Imports
import type { AntiCheatFlag, AntiCheatSummary } from '@/lib/types/admin'

const severityFilters: FilterDef[] = [
  {
    key: 'severity',
    label: 'Severity',
    options: [
      { label: 'Severe', value: '3' },
      { label: 'Warning', value: '2' },
      { label: 'Info', value: '1' }
    ]
  }
]

const columns: Column<AntiCheatFlag>[] = [
  {
    id: 'ruleKey',
    label: 'Rule',
    sortable: true,
    render: row => row.ruleKey
  },
  {
    id: 'severity',
    label: 'Severity',
    width: 100,
    render: row => {
      const map: Record<number, string> = { 1: 'Info', 2: 'Warning', 3: 'Severe' }

      return map[row.severity] ?? String(row.severity)
    }
  },
  {
    id: 'playerId',
    label: 'Player',
    render: row => row.playerId ?? '—'
  },
  {
    id: 'message',
    label: 'Message',
    render: row => row.message
  },
  {
    id: 'createdAtUtc',
    label: 'Date',
    width: 140,
    sortable: true,
    render: row => new Date(row.createdAtUtc).toLocaleDateString()
  },
  {
    id: 'reviewedAtUtc',
    label: 'Reviewed',
    width: 120,
    render: row => (row.reviewedAtUtc ? 'Yes' : 'No')
  }
]

interface SummaryCardProps {
  title: string
  value: number
  color?: string
}

const SummaryCard = ({ title, value, color }: SummaryCardProps) => (
  <Card variant='outlined'>
    <CardContent sx={{ textAlign: 'center', py: 2 }}>
      <Typography variant='body2' color='text.secondary'>
        {title}
      </Typography>
      <Typography variant='h5' sx={{ color, fontWeight: 600 }}>
        {value.toLocaleString()}
      </Typography>
    </CardContent>
  </Card>
)

const AntiCheatView = () => {
  // Summary state
  const [summary, setSummary] = useState<AntiCheatSummary | null>(null)

  // Table state
  const [rows, setRows] = useState<AntiCheatFlag[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)
  const [filterValues, setFilterValues] = useState<Record<string, string>>({})
  const [unreviewedOnly, setUnreviewedOnly] = useState(false)
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)

  const fetchSummary = useCallback(async () => {
    try {
      const res = await antiCheatService.summary()

      setSummary(res)
    } catch {
      // keep current state
    }
  }, [])

  const fetchFlags = useCallback(async () => {
    setLoading(true)

    try {
      const res = await antiCheatService.flags({
        unreviewedOnly: unreviewedOnly || undefined,
        severity: filterValues.severity ? Number(filterValues.severity) : undefined,
        page,
        pageSize
      })

      setRows(res.items)
      setTotal(res.totalItems)
    } catch {
      // keep current state
    } finally {
      setLoading(false)
    }
  }, [unreviewedOnly, filterValues, page, pageSize])

  useEffect(() => {
    fetchSummary()
  }, [fetchSummary])

  useEffect(() => {
    fetchFlags()
  }, [fetchFlags])

  const handleSearchChange = useCallback((value: string) => {
    setSearch(value)
    setPage(1)
  }, [])

  const handleFilterChange = useCallback((key: string, value: string) => {
    setFilterValues(prev => ({ ...prev, [key]: value }))
    setPage(1)
  }, [])

  const handleReview = useCallback(
    async (flagId: string) => {
      try {
        await antiCheatService.reviewFlag(flagId, { reviewedBy: 'admin' })
        await fetchFlags()
        await fetchSummary()
      } catch {
        // keep current state
      }
    },
    [fetchFlags, fetchSummary]
  )

  return (
    <>
      <PageHeader title='Anti-Cheat' />

      {summary && (
        <Grid container spacing={2} sx={{ mb: 3 }}>
          <Grid item xs={6} sm={4} md={2.4}>
            <SummaryCard title='Total Flags' value={summary.totalFlags} />
          </Grid>
          <Grid item xs={6} sm={4} md={2.4}>
            <SummaryCard title='Unreviewed' value={summary.totalFlags} color='warning.main' />
          </Grid>
          <Grid item xs={6} sm={4} md={2.4}>
            <SummaryCard title='Severe' value={summary.severeFlags} color='error.main' />
          </Grid>
          <Grid item xs={6} sm={4} md={2.4}>
            <SummaryCard title='Warning' value={summary.warningFlags} color='warning.main' />
          </Grid>
          <Grid item xs={6} sm={4} md={2.4}>
            <SummaryCard title='Info' value={summary.infoFlags} color='info.main' />
          </Grid>
        </Grid>
      )}

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
        <FormControlLabel
          control={
            <Switch
              checked={unreviewedOnly}
              onChange={(_, checked) => {
                setUnreviewedOnly(checked)
                setPage(1)
              }}
            />
          }
          label='Unreviewed only'
        />
      </Box>

      <SearchFilterBar
        searchPlaceholder='Search flags...'
        searchValue={search}
        onSearchChange={handleSearchChange}
        filters={severityFilters}
        filterValues={filterValues}
        onFilterChange={handleFilterChange}
      />

      <DataTable
        columns={[
          ...columns,
          {
            id: 'actions',
            label: '',
            width: 120,
            align: 'center',
            render: row =>
              !row.reviewedAtUtc ? (
                <Button size='small' variant='outlined' onClick={() => handleReview(row.id)}>
                  Review
                </Button>
              ) : null
          }
        ]}
        rows={rows}
        rowKey={row => row.id}
        loading={loading}
        page={page}
        pageSize={pageSize}
        total={total}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        emptyMessage='No anti-cheat flags found'
      />
    </>
  )
}

export default AntiCheatView
