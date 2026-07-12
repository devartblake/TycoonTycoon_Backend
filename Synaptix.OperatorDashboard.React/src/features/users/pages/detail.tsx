/**
 * User Detail page — profile, stats, activity, and ban/unban actions.
 */

import { useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons'
import { getUserDetail, getUserActivity, banUser, unbanUser } from '../api'

const STATUS_STYLES: Record<string, string> = {
  active: 'text-status-healthy',
  suspended: 'text-status-degraded',
  banned: 'text-status-offline',
  inactive: 'text-ink-tertiary',
}

export default function UserDetailPage() {
  usePermission('users:read')

  const { userId } = useParams<{ userId: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [reason, setReason] = useState('')
  const [message, setMessage] = useState<string | null>(null)

  const userQuery = useQuery({
    queryKey: ['user-detail', userId],
    queryFn: () => getUserDetail(userId!),
    enabled: !!userId,
  })
  const activityQuery = useQuery({
    queryKey: ['user-activity', userId],
    queryFn: () => getUserActivity(userId!),
    enabled: !!userId,
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['user-detail', userId] })
  const banMutation = useMutation({
    mutationFn: () => banUser(userId!, reason || undefined),
    onSuccess: () => { setMessage('User banned'); invalidate() },
  })
  const unbanMutation = useMutation({
    mutationFn: () => unbanUser(userId!),
    onSuccess: () => { setMessage('User unbanned'); invalidate() },
  })

  if (!userId) {
    return (
      <div className="operator-container py-12">
        <EmptyState title="User not found" description="No user id in the URL." />
      </div>
    )
  }

  const user = userQuery.data
  const isMutating = banMutation.isPending || unbanMutation.isPending

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <button onClick={() => navigate(-1)} className="text-accent hover:underline text-sm mb-2">
              ← Back
            </button>
            <h1 className="text-3xl font-bold text-ink-primary">User Detail</h1>
            <p className="mt-1 text-ink-secondary font-mono text-sm">{userId}</p>
          </div>
          <Link
            to={`/users/${userId}/investigation`}
            className="px-4 py-2 bg-accent text-white rounded font-medium hover:bg-accent-dark transition-smooth"
          >
            Open Investigation
          </Link>
        </div>

        {message && (
          <div className="p-3 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm">
            {message}
          </div>
        )}

        {userQuery.isLoading ? (
          <SkeletonGrid count={4} />
        ) : userQuery.isError ? (
          <EmptyState title="Failed to load user" description={(userQuery.error as Error)?.message} />
        ) : user ? (
          <>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <div className="operator-card">
                <p className="text-xs text-ink-tertiary">Email</p>
                <p className="text-sm font-medium text-ink-primary mt-1 break-all">{user.email}</p>
                <p className="text-xs text-ink-tertiary mt-2">Username</p>
                <p className="text-sm text-ink-primary">{user.username}</p>
              </div>
              <div className="operator-card">
                <p className="text-xs text-ink-tertiary">Status</p>
                <p className={`text-2xl font-bold mt-1 capitalize ${STATUS_STYLES[user.status] ?? ''}`}>{user.status}</p>
                <p className="text-xs text-ink-tertiary mt-2">
                  {user.isVerified ? '✓ Verified' : 'Not verified'} · {user.role} · {user.ageGroup}
                </p>
              </div>
              <div className="operator-card">
                <p className="text-xs text-ink-tertiary">Games / Points</p>
                <p className="text-2xl font-bold text-accent mt-1">{user.totalGamesPlayed.toLocaleString()}</p>
                <p className="text-xs text-ink-tertiary mt-1">{user.totalPoints.toLocaleString()} pts · {Math.round(user.winRate * 100)}% win rate</p>
              </div>
              <div className="operator-card">
                <p className="text-xs text-ink-tertiary">Created</p>
                <p className="text-sm text-ink-primary mt-1">{new Date(user.createdAt).toLocaleString()}</p>
                <p className="text-xs text-ink-tertiary mt-2">Last active</p>
                <p className="text-sm text-ink-primary">
                  {user.lastActiveAt ? new Date(user.lastActiveAt).toLocaleString() : 'Never'}
                </p>
              </div>
            </div>

            {/* Moderation actions */}
            <div className="operator-card space-y-3">
              <h2 className="text-lg font-semibold">Actions</h2>
              <input
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                placeholder="Reason (required for ban)"
                className="w-full px-3 py-2 border border-panel-border rounded focus-ring text-sm"
              />
              <div className="flex gap-3">
                {user.isBanned || user.status === 'banned' ? (
                  <button
                    onClick={() => unbanMutation.mutate()}
                    disabled={isMutating}
                    className="px-4 py-2 bg-status-healthy text-white rounded text-sm font-medium disabled:opacity-50"
                  >
                    Unban user
                  </button>
                ) : (
                  <button
                    onClick={() => banMutation.mutate()}
                    disabled={isMutating || !reason.trim()}
                    className="px-4 py-2 bg-status-offline text-white rounded text-sm font-medium disabled:opacity-50"
                  >
                    Ban user
                  </button>
                )}
              </div>
              {(banMutation.isError || unbanMutation.isError) && (
                <p className="text-xs text-status-offline">
                  {((banMutation.error || unbanMutation.error) as Error)?.message}
                </p>
              )}
            </div>
          </>
        ) : null}

        {/* Activity */}
        <div className="operator-card p-0">
          <h2 className="text-lg font-semibold p-4 border-b border-panel-border">Recent Activity</h2>
          {activityQuery.isLoading ? (
            <div className="p-4"><SkeletonTable rows={5} columns={3} /></div>
          ) : activityQuery.data && activityQuery.data.items.length > 0 ? (
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-xs text-ink-tertiary border-b border-panel-border">
                  <th className="p-3">Type</th>
                  <th className="p-3">Description</th>
                  <th className="p-3">When</th>
                </tr>
              </thead>
              <tbody>
                {activityQuery.data.items.map((a) => (
                  <tr key={a.id} className="border-b border-panel-border last:border-0">
                    <td className="p-3 font-mono text-xs">{a.type}</td>
                    <td className="p-3">{a.description}</td>
                    <td className="p-3 text-ink-tertiary">{new Date(a.createdAt).toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <div className="p-6">
              <EmptyState title="No activity" description="No recent activity recorded for this user." />
            </div>
          )}
        </div>
      </div>
    </ErrorBoundary>
  )
}
