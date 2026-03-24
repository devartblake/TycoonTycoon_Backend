'use client'

import { useCallback, useEffect, useState } from 'react'

// MUI Imports
import Grid from '@mui/material/Grid'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import CardHeader from '@mui/material/CardHeader'
import Typography from '@mui/material/Typography'
import Chip from '@mui/material/Chip'
import Divider from '@mui/material/Divider'
import Button from '@mui/material/Button'
import Box from '@mui/material/Box'
import Tab from '@mui/material/Tab'
import TabContext from '@mui/lab/TabContext'
import TabList from '@mui/lab/TabList'
import TabPanel from '@mui/lab/TabPanel'
import TextField from '@mui/material/TextField'
import Alert from '@mui/material/Alert'
import Skeleton from '@mui/material/Skeleton'

// Component Imports
import PageHeader from '@components/admin/PageHeader'
import StatusBadge from '@components/admin/StatusBadge'
import ConfirmDialog from '@components/admin/ConfirmDialog'
import DataTable from '@components/admin/DataTable'
import type { Column } from '@components/admin/DataTable'

// Service Imports
import { userService } from '@/lib/services/userService'
import { moderationService } from '@/lib/services/moderationService'
import { antiCheatService } from '@/lib/services/antiCheatService'
import { economyService } from '@/lib/services/economyService'

// Type Imports
import type {
  AdminUserDetail,
  ModerationProfile,
  ModerationLogItem,
  AdminUserActivityItem,
  AntiCheatFlag,
  EconomyTxnListItem,
  ModerationStatus
} from '@/lib/types/admin'

// ─── Activity Tab ───────────────────────────────────────────────────

const activityColumns: Column<AdminUserActivityItem>[] = [
  { id: 'type', label: 'Type', width: 140, render: row => <Chip label={row.type} size='small' variant='tonal' /> },
  { id: 'description', label: 'Description', render: row => row.description },
  {
    id: 'createdAt',
    label: 'Time',
    width: 180,
    render: row => new Date(row.createdAt).toLocaleString()
  }
]

// ─── Anti-Cheat Tab ─────────────────────────────────────────────────

const antiCheatColumns: Column<AntiCheatFlag>[] = [
  { id: 'ruleKey', label: 'Rule', render: row => row.ruleKey },
  {
    id: 'severity',
    label: 'Severity',
    width: 100,
    render: row => {
      const colors: Record<number, 'error' | 'warning' | 'info'> = { 3: 'error', 2: 'warning', 1: 'info' }

      return <Chip label={row.severity} size='small' color={colors[row.severity] ?? 'default'} variant='tonal' />
    }
  },
  { id: 'message', label: 'Message', render: row => row.message },
  {
    id: 'reviewed',
    label: 'Reviewed',
    width: 100,
    align: 'center',
    render: row =>
      row.reviewedAtUtc ? (
        <i className='ri-checkbox-circle-line text-success' />
      ) : (
        <i className='ri-close-circle-line text-textDisabled' />
      )
  },
  {
    id: 'createdAtUtc',
    label: 'Date',
    width: 160,
    render: row => new Date(row.createdAtUtc).toLocaleString()
  }
]

// ─── Economy Tab ────────────────────────────────────────────────────

const currencyLabel: Record<number, string> = { 1: 'XP', 2: 'Coins', 3: 'Diamonds' }

const economyColumns: Column<EconomyTxnListItem>[] = [
  { id: 'kind', label: 'Kind', render: row => row.kind },
  {
    id: 'lines',
    label: 'Changes',
    render: row =>
      row.lines.map((l, i) => (
        <Chip
          key={i}
          label={`${l.delta > 0 ? '+' : ''}${l.delta} ${currencyLabel[l.currency] ?? '?'}`}
          size='small'
          color={l.delta > 0 ? 'success' : 'error'}
          variant='tonal'
          sx={{ mr: 0.5 }}
        />
      ))
  },
  {
    id: 'createdAtUtc',
    label: 'Date',
    width: 160,
    render: row => new Date(row.createdAtUtc).toLocaleString()
  }
]

// ─── Moderation Log ─────────────────────────────────────────────────

const modLogColumns: Column<ModerationLogItem>[] = [
  {
    id: 'newStatus',
    label: 'Status',
    width: 120,
    render: row => <StatusBadge status={row.newStatus as ModerationStatus} />
  },
  { id: 'reason', label: 'Reason', render: row => row.reason ?? '-' },
  { id: 'setByAdmin', label: 'Admin', width: 140, render: row => row.setByAdmin ?? '-' },
  {
    id: 'createdAtUtc',
    label: 'Date',
    width: 160,
    render: row => new Date(row.createdAtUtc).toLocaleString()
  }
]

// ─── Main Component ─────────────────────────────────────────────────

interface UserDetailViewProps {
  userId: string
}

const UserDetailView = ({ userId }: UserDetailViewProps) => {
  // Core data
  const [user, setUser] = useState<AdminUserDetail | null>(null)
  const [modProfile, setModProfile] = useState<ModerationProfile | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Tab state
  const [tab, setTab] = useState('activity')

  // Activity tab
  const [activities, setActivities] = useState<AdminUserActivityItem[]>([])
  const [actPage, setActPage] = useState(1)
  const [actTotal, setActTotal] = useState(0)
  const [actLoading, setActLoading] = useState(false)

  // Anti-cheat tab
  const [flags, setFlags] = useState<AntiCheatFlag[]>([])
  const [flagPage, setFlagPage] = useState(1)
  const [flagTotal, setFlagTotal] = useState(0)
  const [flagLoading, setFlagLoading] = useState(false)

  // Economy tab
  const [txns, setTxns] = useState<EconomyTxnListItem[]>([])
  const [txnPage, setTxnPage] = useState(1)
  const [txnTotal, setTxnTotal] = useState(0)
  const [txnLoading, setTxnLoading] = useState(false)

  // Moderation log
  const [modLogs, setModLogs] = useState<ModerationLogItem[]>([])
  const [modLogPage, setModLogPage] = useState(1)
  const [modLogTotal, setModLogTotal] = useState(0)
  const [modLogLoading, setModLogLoading] = useState(false)

  // Ban dialog
  const [banOpen, setBanOpen] = useState(false)
  const [banReason, setBanReason] = useState('')
  const [banLoading, setBanLoading] = useState(false)

  // Unban dialog
  const [unbanOpen, setUnbanOpen] = useState(false)
  const [unbanLoading, setUnbanLoading] = useState(false)

  // ── Load user + moderation profile ──
  useEffect(() => {
    const load = async () => {
      try {
        const [u, m] = await Promise.all([userService.get(userId), moderationService.getProfile(userId)])

        setUser(u)
        setModProfile(m)
      } catch {
        setError('Failed to load user')
      }
    }

    load()
  }, [userId])

  // ── Tab data loaders ──
  const loadActivity = useCallback(async () => {
    setActLoading(true)

    try {
      const res = await userService.activity(userId, { page: actPage, pageSize: 25 })

      setActivities(res.items)
      setActTotal(res.totalItems)
    } catch {
      // silent
    } finally {
      setActLoading(false)
    }
  }, [userId, actPage])

  const loadFlags = useCallback(async () => {
    setFlagLoading(true)

    try {
      const res = await antiCheatService.flags({ playerId: userId, page: flagPage, pageSize: 25 })

      setFlags(res.items)
      setFlagTotal(res.totalItems)
    } catch {
      // silent
    } finally {
      setFlagLoading(false)
    }
  }, [userId, flagPage])

  const loadEconomy = useCallback(async () => {
    setTxnLoading(true)

    try {
      const res = await economyService.history(userId, { page: txnPage, pageSize: 25 })

      setTxns(res.items)
      setTxnTotal(res.total)
    } catch {
      // silent
    } finally {
      setTxnLoading(false)
    }
  }, [userId, txnPage])

  const loadModLogs = useCallback(async () => {
    setModLogLoading(true)

    try {
      const res = await moderationService.logs({ playerId: userId, page: modLogPage, pageSize: 25 })

      setModLogs(res.items)
      setModLogTotal(res.totalItems)
    } catch {
      // silent
    } finally {
      setModLogLoading(false)
    }
  }, [userId, modLogPage])

  // Load tab data on tab change or pagination
  useEffect(() => {
    if (tab === 'activity') loadActivity()
  }, [tab, loadActivity])

  useEffect(() => {
    if (tab === 'anticheat') loadFlags()
  }, [tab, loadFlags])

  useEffect(() => {
    if (tab === 'economy') loadEconomy()
  }, [tab, loadEconomy])

  // Always load moderation logs
  useEffect(() => {
    loadModLogs()
  }, [loadModLogs])

  // ── Actions ──
  const handleBan = async () => {
    setBanLoading(true)

    try {
      await userService.ban(userId, { reason: banReason })

      const u = await userService.get(userId)

      setUser(u)
      setBanOpen(false)
      setBanReason('')
    } catch {
      // keep dialog open
    } finally {
      setBanLoading(false)
    }
  }

  const handleUnban = async () => {
    setUnbanLoading(true)

    try {
      await userService.unban(userId)

      const u = await userService.get(userId)

      setUser(u)
      setUnbanOpen(false)
    } catch {
      // keep dialog open
    } finally {
      setUnbanLoading(false)
    }
  }

  if (error) {
    return <Alert severity='error'>{error}</Alert>
  }

  if (!user) {
    return (
      <>
        <Skeleton variant='text' width={200} height={40} />
        <Skeleton variant='rectangular' height={200} sx={{ mt: 2 }} />
      </>
    )
  }

  return (
    <>
      <PageHeader
        title={user.username}
        breadcrumbs={[
          { label: 'Users', href: '/users' },
          { label: user.username }
        ]}
        actions={
          <>
            {user.isBanned ? (
              <Button variant='contained' color='success' onClick={() => setUnbanOpen(true)}>
                Unban
              </Button>
            ) : (
              <Button variant='contained' color='error' onClick={() => setBanOpen(true)}>
                Ban
              </Button>
            )}
          </>
        }
      />

      <Grid container spacing={6}>
        {/* ── Profile Card ── */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardHeader title='Profile' />
            <CardContent>
              <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
                <Box>
                  <Typography variant='caption' color='text.secondary'>
                    Email
                  </Typography>
                  <Typography>{user.email}</Typography>
                </Box>
                <Box>
                  <Typography variant='caption' color='text.secondary'>
                    Role
                  </Typography>
                  <Typography>{user.role}</Typography>
                </Box>
                <Box>
                  <Typography variant='caption' color='text.secondary'>
                    Age Group
                  </Typography>
                  <Typography>{user.ageGroup}</Typography>
                </Box>
                <Box>
                  <Typography variant='caption' color='text.secondary'>
                    Verified
                  </Typography>
                  <Typography>
                    {user.isVerified ? (
                      <Chip label='Yes' size='small' color='success' variant='tonal' />
                    ) : (
                      <Chip label='No' size='small' color='default' variant='tonal' />
                    )}
                  </Typography>
                </Box>
                <Box>
                  <Typography variant='caption' color='text.secondary'>
                    Joined
                  </Typography>
                  <Typography>{new Date(user.createdAt).toLocaleDateString()}</Typography>
                </Box>
                <Box>
                  <Typography variant='caption' color='text.secondary'>
                    Last Active
                  </Typography>
                  <Typography>{user.lastActive ? new Date(user.lastActive).toLocaleDateString() : '-'}</Typography>
                </Box>
              </Box>

              <Divider sx={{ my: 2 }} />

              <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 2, textAlign: 'center' }}>
                <Box>
                  <Typography variant='h5'>{user.totalGamesPlayed.toLocaleString()}</Typography>
                  <Typography variant='caption' color='text.secondary'>
                    Games
                  </Typography>
                </Box>
                <Box>
                  <Typography variant='h5'>{user.totalPoints.toLocaleString()}</Typography>
                  <Typography variant='caption' color='text.secondary'>
                    Points
                  </Typography>
                </Box>
                <Box>
                  <Typography variant='h5'>{(user.winRate * 100).toFixed(1)}%</Typography>
                  <Typography variant='caption' color='text.secondary'>
                    Win Rate
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* ── Moderation Card ── */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardHeader title='Moderation' />
            <CardContent>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <Box>
                  <Typography variant='caption' color='text.secondary'>
                    Current Status
                  </Typography>
                  <Box sx={{ mt: 0.5 }}>
                    {modProfile ? (
                      <StatusBadge status={modProfile.status} size='medium' />
                    ) : (
                      <Skeleton width={80} />
                    )}
                  </Box>
                </Box>
                {modProfile?.reason && (
                  <Box>
                    <Typography variant='caption' color='text.secondary'>
                      Reason
                    </Typography>
                    <Typography>{modProfile.reason}</Typography>
                  </Box>
                )}
                {modProfile?.setByAdmin && (
                  <Box>
                    <Typography variant='caption' color='text.secondary'>
                      Set By
                    </Typography>
                    <Typography>
                      {modProfile.setByAdmin} on {new Date(modProfile.setAtUtc).toLocaleDateString()}
                    </Typography>
                  </Box>
                )}
                {modProfile?.expiresAtUtc && (
                  <Box>
                    <Typography variant='caption' color='text.secondary'>
                      Expires
                    </Typography>
                    <Typography>{new Date(modProfile.expiresAtUtc).toLocaleString()}</Typography>
                  </Box>
                )}
                {user.isBanned && (
                  <Chip label='Account Banned' color='error' sx={{ alignSelf: 'flex-start' }} />
                )}
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* ── Tabs ── */}
        <Grid item xs={12}>
          <Card>
            <TabContext value={tab}>
              <TabList onChange={(_, v) => setTab(v)} sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}>
                <Tab label='Activity' value='activity' />
                <Tab label='Anti-Cheat' value='anticheat' />
                <Tab label='Economy' value='economy' />
              </TabList>

              <TabPanel value='activity' sx={{ p: 0 }}>
                <DataTable
                  columns={activityColumns}
                  rows={activities}
                  rowKey={row => row.id}
                  loading={actLoading}
                  page={actPage}
                  pageSize={25}
                  total={actTotal}
                  onPageChange={setActPage}
                  emptyMessage='No activity recorded'
                />
              </TabPanel>

              <TabPanel value='anticheat' sx={{ p: 0 }}>
                <DataTable
                  columns={antiCheatColumns}
                  rows={flags}
                  rowKey={row => row.id}
                  loading={flagLoading}
                  page={flagPage}
                  pageSize={25}
                  total={flagTotal}
                  onPageChange={setFlagPage}
                  emptyMessage='No anti-cheat flags'
                />
              </TabPanel>

              <TabPanel value='economy' sx={{ p: 0 }}>
                <DataTable
                  columns={economyColumns}
                  rows={txns}
                  rowKey={row => row.eventId}
                  loading={txnLoading}
                  page={txnPage}
                  pageSize={25}
                  total={txnTotal}
                  onPageChange={setTxnPage}
                  emptyMessage='No transactions'
                />
              </TabPanel>
            </TabContext>
          </Card>
        </Grid>

        {/* ── Moderation Log ── */}
        <Grid item xs={12}>
          <Card>
            <CardHeader title='Moderation Log' />
            <DataTable
              columns={modLogColumns}
              rows={modLogs}
              rowKey={row => row.id}
              loading={modLogLoading}
              page={modLogPage}
              pageSize={25}
              total={modLogTotal}
              onPageChange={setModLogPage}
              emptyMessage='No moderation actions'
            />
          </Card>
        </Grid>
      </Grid>

      {/* ── Ban Dialog ── */}
      <ConfirmDialog
        open={banOpen}
        title={`Ban ${user.username}`}
        message='This will immediately ban the player. They will not be able to play until unbanned.'
        confirmLabel='Ban Player'
        confirmColor='error'
        onConfirm={handleBan}
        onCancel={() => setBanOpen(false)}
        loading={banLoading}
      />

      {/* Ban reason input — shown when dialog is open */}
      {banOpen && (
        <Box sx={{ position: 'fixed', zIndex: 1301, top: '50%', left: '50%', transform: 'translate(-50%, 60px)' }}>
          <TextField
            label='Ban reason'
            value={banReason}
            onChange={e => setBanReason(e.target.value)}
            size='small'
            fullWidth
            sx={{ minWidth: 300, bgcolor: 'background.paper', borderRadius: 1 }}
          />
        </Box>
      )}

      {/* ── Unban Dialog ── */}
      <ConfirmDialog
        open={unbanOpen}
        title={`Unban ${user.username}`}
        message='This will lift the ban and allow the player to resume playing.'
        confirmLabel='Unban Player'
        confirmColor='primary'
        onConfirm={handleUnban}
        onCancel={() => setUnbanOpen(false)}
        loading={unbanLoading}
      />
    </>
  )
}

export default UserDetailView
