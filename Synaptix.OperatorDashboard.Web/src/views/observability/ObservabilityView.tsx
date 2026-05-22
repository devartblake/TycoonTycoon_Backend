'use client'

import { useCallback, useEffect, useState } from 'react'

// MUI Imports
import Box from '@mui/material/Box'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import Chip from '@mui/material/Chip'
import Grid from '@mui/material/Grid'
import IconButton from '@mui/material/IconButton'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableContainer from '@mui/material/TableContainer'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import Tooltip from '@mui/material/Tooltip'
import Typography from '@mui/material/Typography'

// Component Imports
import PageHeader from '@components/admin/PageHeader'

// Analytics Imports
import type { AnalyticsSnapshot, AdminApiEvent } from '@/lib/adminAnalytics'
import { getAnalyticsSnapshot, clearAnalytics } from '@/lib/adminAnalytics'

// ─── SLO thresholds ─────────────────────────────────────────────────

const SLO_SUCCESS_RATE = 99.0 // target success %
const SLO_THROTTLED_PCT = 1.0 // max throttled %
const SLO_LATENCY_MS = 500 // target p50 latency

// ─── Helpers ────────────────────────────────────────────────────────

function sloColor(actual: number, target: number, higherIsBetter: boolean): 'success' | 'warning' | 'error' {
  if (higherIsBetter) {
    if (actual >= target) return 'success'
    if (actual >= target * 0.95) return 'warning'

    return 'error'
  }

  if (actual <= target) return 'success'
  if (actual <= target * 1.5) return 'warning'

  return 'error'
}

const statusColor: Record<string, 'success' | 'error' | 'warning' | 'info' | 'default'> = {
  UNAUTHORIZED: 'error',
  FORBIDDEN: 'error',
  RATE_LIMITED: 'warning',
  VALIDATION_ERROR: 'warning',
  NOT_FOUND: 'info',
  CONFLICT: 'warning',
  UNKNOWN: 'default'
}

function formatTime(ts: number): string {
  return new Date(ts).toLocaleTimeString()
}

// ─── SLO Widget Card ────────────────────────────────────────────────

function SloCard({
  title,
  value,
  unit,
  target,
  color
}: {
  title: string
  value: string
  unit: string
  target: string
  color: 'success' | 'warning' | 'error'
}) {
  const colorMap = { success: 'success.main', warning: 'warning.main', error: 'error.main' }

  return (
    <Card variant='outlined'>
      <CardContent>
        <Typography variant='caption' color='text.secondary'>
          {title}
        </Typography>
        <Typography variant='h4' sx={{ color: colorMap[color], my: 0.5 }}>
          {value}
          <Typography component='span' variant='body2' color='text.secondary'>
            {' '}
            {unit}
          </Typography>
        </Typography>
        <Typography variant='caption' color='text.secondary'>
          Target: {target}
        </Typography>
      </CardContent>
    </Card>
  )
}

// ─── Backend Metric Correlation Table ───────────────────────────────

const backendCorrelation = [
  {
    frontendCode: 'UNAUTHORIZED',
    backendMetric: 'admin_auth_events_total{outcome="unauthorized"}',
    runbook: 'Check JWT expiry / token rotation. Alert at >= 20/min over 5m.'
  },
  {
    frontendCode: 'FORBIDDEN',
    backendMetric: 'admin_auth_events_total{outcome="forbidden"}',
    runbook: 'Verify role/scope claims. May indicate misconfigured ACL.'
  },
  {
    frontendCode: 'RATE_LIMITED',
    backendMetric: 'admin_rate_limit_rejected_total{path=...}',
    runbook: 'Alert at >= 10/min over 5m. Check if legitimate spike or abuse.'
  },
  {
    frontendCode: 'NOT_FOUND',
    backendMetric: 'admin_notification_events_total{outcome="not_found"}',
    runbook: 'Alert at >= 5/min over 15m. Indicates config drift or stale references.'
  },
  {
    frontendCode: 'CONFLICT',
    backendMetric: 'N/A (contextual)',
    runbook: 'Usually replay on non-failed schedule. Refresh state and retry.'
  }
]

// ─── Main Component ─────────────────────────────────────────────────

const ObservabilityView = () => {
  const [snapshot, setSnapshot] = useState<AnalyticsSnapshot | null>(null)

  const refresh = useCallback(() => {
    setSnapshot(getAnalyticsSnapshot())
  }, [])

  // Auto-refresh every 5s
  useEffect(() => {
    refresh()
    const interval = setInterval(refresh, 5000)

    return () => clearInterval(interval)
  }, [refresh])

  const handleClear = () => {
    clearAnalytics()
    refresh()
  }

  const s = snapshot ?? {
    totalRequests: 0,
    totalFailures: 0,
    successRate: 100,
    throttledCount: 0,
    avgLatencyMs: 0,
    errorsByCode: [],
    recentEvents: [],
    endpointStats: []
  }

  const throttledPct = s.totalRequests > 0 ? (s.throttledCount / s.totalRequests) * 100 : 0

  return (
    <>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <PageHeader title='Observability' />
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Tooltip title='Refresh'>
            <IconButton size='small' onClick={refresh}>
              <i className='ri-refresh-line' />
            </IconButton>
          </Tooltip>
          <Tooltip title='Clear session data'>
            <IconButton size='small' onClick={handleClear}>
              <i className='ri-delete-bin-line' />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      {/* ── SLO Widgets ── */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={4}>
          <SloCard
            title='Admin Action Success Rate'
            value={s.successRate.toFixed(1)}
            unit='%'
            target={`>= ${SLO_SUCCESS_RATE}%`}
            color={sloColor(s.successRate, SLO_SUCCESS_RATE, true)}
          />
        </Grid>
        <Grid item xs={12} sm={4}>
          <SloCard
            title='Throttled Action Rate'
            value={throttledPct.toFixed(1)}
            unit='%'
            target={`<= ${SLO_THROTTLED_PCT}%`}
            color={sloColor(throttledPct, SLO_THROTTLED_PCT, false)}
          />
        </Grid>
        <Grid item xs={12} sm={4}>
          <SloCard
            title='Median Response Latency'
            value={String(s.avgLatencyMs)}
            unit='ms'
            target={`<= ${SLO_LATENCY_MS}ms`}
            color={sloColor(s.avgLatencyMs, SLO_LATENCY_MS, false)}
          />
        </Grid>
      </Grid>

      {/* ── Errors by Code ── */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={12} md={6}>
          <Card variant='outlined'>
            <CardContent>
              <Typography variant='subtitle1' sx={{ mb: 1 }}>
                Frontend Errors by Code
              </Typography>
              {s.errorsByCode.length === 0 ? (
                <Typography variant='body2' color='text.secondary'>
                  No errors in this session
                </Typography>
              ) : (
                <Table size='small'>
                  <TableHead>
                    <TableRow>
                      <TableCell>Error Code</TableCell>
                      <TableCell align='right'>Count</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {s.errorsByCode.map(e => (
                      <TableRow key={e.code}>
                        <TableCell>
                          <Chip
                            label={e.code}
                            size='small'
                            color={statusColor[e.code] ?? 'default'}
                            variant='tonal'
                          />
                        </TableCell>
                        <TableCell align='right'>{e.count}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* ── Endpoint Stats ── */}
        <Grid item xs={12} md={6}>
          <Card variant='outlined'>
            <CardContent>
              <Typography variant='subtitle1' sx={{ mb: 1 }}>
                Endpoint Stats
              </Typography>
              {s.endpointStats.length === 0 ? (
                <Typography variant='body2' color='text.secondary'>
                  No requests in this session
                </Typography>
              ) : (
                <TableContainer sx={{ maxHeight: 260 }}>
                  <Table size='small' stickyHeader>
                    <TableHead>
                      <TableRow>
                        <TableCell>Endpoint</TableCell>
                        <TableCell align='right'>Reqs</TableCell>
                        <TableCell align='right'>Fails</TableCell>
                        <TableCell align='right'>Avg ms</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {s.endpointStats.map(ep => (
                        <TableRow key={ep.endpoint}>
                          <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>
                            {ep.endpoint}
                          </TableCell>
                          <TableCell align='right'>{ep.totalRequests}</TableCell>
                          <TableCell
                            align='right'
                            sx={{ color: ep.failures > 0 ? 'error.main' : 'text.secondary' }}
                          >
                            {ep.failures}
                          </TableCell>
                          <TableCell align='right'>{ep.avgLatencyMs}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* ── Backend Metric Correlation ── */}
      <Card variant='outlined' sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant='subtitle1' sx={{ mb: 1 }}>
            Frontend → Backend Metric Correlation
          </Typography>
          <Typography variant='body2' color='text.secondary' sx={{ mb: 2 }}>
            Maps frontend error codes to backend Prometheus counters and runbook steps.
          </Typography>
          <TableContainer>
            <Table size='small'>
              <TableHead>
                <TableRow>
                  <TableCell>Frontend Code</TableCell>
                  <TableCell>Backend Metric</TableCell>
                  <TableCell>Runbook</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {backendCorrelation.map(row => (
                  <TableRow key={row.frontendCode}>
                    <TableCell>
                      <Chip
                        label={row.frontendCode}
                        size='small'
                        color={statusColor[row.frontendCode] ?? 'default'}
                        variant='tonal'
                      />
                    </TableCell>
                    <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>
                      {row.backendMetric}
                    </TableCell>
                    <TableCell>{row.runbook}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      {/* ── Recent Events ── */}
      <Card variant='outlined'>
        <CardContent>
          <Typography variant='subtitle1' sx={{ mb: 1 }}>
            Recent API Events ({s.recentEvents.length})
          </Typography>
          {s.recentEvents.length === 0 ? (
            <Typography variant='body2' color='text.secondary'>
              No events recorded yet. Navigate the dashboard to generate API activity.
            </Typography>
          ) : (
            <TableContainer sx={{ maxHeight: 320 }}>
              <Table size='small' stickyHeader>
                <TableHead>
                  <TableRow>
                    <TableCell>Time</TableCell>
                    <TableCell>Method</TableCell>
                    <TableCell>Endpoint</TableCell>
                    <TableCell align='center'>Status</TableCell>
                    <TableCell align='right'>Latency</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {s.recentEvents.map((evt: AdminApiEvent, i: number) => (
                    <TableRow key={`${evt.timestamp}-${i}`}>
                      <TableCell sx={{ fontSize: '0.75rem' }}>{formatTime(evt.timestamp)}</TableCell>
                      <TableCell>{evt.method}</TableCell>
                      <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>
                        {evt.endpoint}
                      </TableCell>
                      <TableCell align='center'>
                        {evt.success ? (
                          <Chip label={evt.status} size='small' color='success' variant='tonal' />
                        ) : (
                          <Chip
                            label={evt.errorCode ?? evt.status}
                            size='small'
                            color={statusColor[evt.errorCode ?? ''] ?? 'error'}
                            variant='tonal'
                          />
                        )}
                      </TableCell>
                      <TableCell align='right'>{evt.latencyMs}ms</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>
    </>
  )
}

export default ObservabilityView
