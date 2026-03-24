'use client'

import { useCallback, useState } from 'react'

// MUI Imports
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import FormControlLabel from '@mui/material/FormControlLabel'
import Grid from '@mui/material/Grid'
import Switch from '@mui/material/Switch'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'

// Component Imports
import PageHeader from '@components/admin/PageHeader'
import DataTable from '@components/admin/DataTable'
import type { Column } from '@components/admin/DataTable'
import StatusBadge from '@components/admin/StatusBadge'

// Service Imports
import { moderationService } from '@/lib/services/moderationService'

// Type Imports
import type { EscalationDecision, ModerationStatus, RunEscalationResponse } from '@/lib/types/admin'

const columns: Column<EscalationDecision>[] = [
  {
    id: 'playerId',
    label: 'Player',
    render: row => row.playerId
  },
  {
    id: 'currentStatus',
    label: 'Current',
    width: 130,
    render: row => <StatusBadge status={row.currentStatus as ModerationStatus} />
  },
  {
    id: 'proposedStatus',
    label: 'Proposed',
    width: 130,
    render: row => <StatusBadge status={row.proposedStatus as ModerationStatus} />
  },
  {
    id: 'severeCount',
    label: 'Severe',
    width: 80,
    align: 'right',
    render: row => row.severeCount
  },
  {
    id: 'warningCount',
    label: 'Warnings',
    width: 80,
    align: 'right',
    render: row => row.warningCount
  },
  {
    id: 'reason',
    label: 'Reason',
    render: row => row.reason
  }
]

const EscalationView = () => {
  // Form state
  const [windowHours, setWindowHours] = useState(24)
  const [maxPlayers, setMaxPlayers] = useState(500)
  const [dryRun, setDryRun] = useState(true)
  const [running, setRunning] = useState(false)

  // Results state
  const [result, setResult] = useState<RunEscalationResponse | null>(null)

  const handleRun = useCallback(async () => {
    setRunning(true)

    try {
      const res = await moderationService.runEscalation({
        windowHours,
        maxPlayers,
        dryRun
      })

      setResult(res)
    } catch {
      // API error — keep current state
    } finally {
      setRunning(false)
    }
  }, [windowHours, maxPlayers, dryRun])

  return (
    <>
      <PageHeader title='Escalation' />

      <Card variant='outlined' sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant='subtitle1' sx={{ mb: 2 }}>
            Run Escalation
          </Typography>
          <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'center' }}>
            <TextField
              label='Window (hours)'
              type='number'
              size='small'
              value={windowHours}
              onChange={e => setWindowHours(Number(e.target.value))}
              sx={{ width: 140 }}
            />
            <TextField
              label='Max Players'
              type='number'
              size='small'
              value={maxPlayers}
              onChange={e => setMaxPlayers(Number(e.target.value))}
              sx={{ width: 140 }}
            />
            <FormControlLabel
              control={<Switch checked={dryRun} onChange={(_, checked) => setDryRun(checked)} />}
              label='Dry Run'
            />
            <Button variant='contained' onClick={handleRun} disabled={running}>
              {running ? 'Running...' : 'Run Escalation'}
            </Button>
          </Box>
        </CardContent>
      </Card>

      {result && (
        <>
          <Grid container spacing={2} sx={{ mb: 3 }}>
            <Grid item xs={6} sm={3}>
              <Card variant='outlined'>
                <CardContent sx={{ textAlign: 'center', py: 2 }}>
                  <Typography variant='body2' color='text.secondary'>
                    Evaluated Players
                  </Typography>
                  <Typography variant='h5' sx={{ mt: 0.5 }}>
                    {result.evaluatedPlayers.toLocaleString()}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
            <Grid item xs={6} sm={3}>
              <Card variant='outlined'>
                <CardContent sx={{ textAlign: 'center', py: 2 }}>
                  <Typography variant='body2' color='text.secondary'>
                    Changed Players
                  </Typography>
                  <Typography variant='h5' sx={{ mt: 0.5 }}>
                    {result.changedPlayers.toLocaleString()}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          <DataTable
            columns={columns}
            rows={result.decisions}
            rowKey={row => row.playerId}
            loading={false}
            page={1}
            pageSize={result.decisions.length || 25}
            total={result.decisions.length}
            onPageChange={() => {}}
            emptyMessage='No escalation decisions'
          />
        </>
      )}
    </>
  )
}

export default EscalationView
