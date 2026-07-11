/**
 * User Investigation page — cross-feature view combining identity, moderation
 * history, and economy for one user. Each section loads and fails independently.
 */

import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons'
import { getUserDetail } from '../api'
import { getPlayerModeration } from '@/features/moderation/api'
import { getPlayerEconomy, getPlayerTransactions } from '@/features/economy/api'

// Backend guid routes need the raw id; user ids arrive as usr_{guid} contract ids.
const toRawId = (id: string) => id.replace(/^(usr_|ply_)/, '')

function SectionError({ label, error }: { label: string; error: unknown }) {
  return (
    <div className="p-4 text-sm text-status-offline">
      {label} unavailable: {(error as Error)?.message ?? 'unknown error'}
    </div>
  )
}

export default function UserInvestigationPage() {
  usePermission('users:read')

  const { userId } = useParams<{ userId: string }>()
  const navigate = useNavigate()
  const rawId = userId ? toRawId(userId) : ''

  const userQuery = useQuery({
    queryKey: ['user-detail', userId],
    queryFn: () => getUserDetail(userId!),
    enabled: !!userId,
  })
  const moderationQuery = useQuery({
    queryKey: ['investigation-moderation', rawId],
    queryFn: () => getPlayerModeration(rawId),
    enabled: !!rawId,
    retry: false,
  })
  const economyQuery = useQuery({
    queryKey: ['investigation-economy', rawId],
    queryFn: () => getPlayerEconomy(rawId),
    enabled: !!rawId,
    retry: false,
  })
  const transactionsQuery = useQuery({
    queryKey: ['investigation-transactions', rawId],
    queryFn: () => getPlayerTransactions(rawId, undefined, 0, 10),
    enabled: !!rawId,
    retry: false,
  })

  if (!userId) {
    return (
      <div className="operator-container py-12">
        <EmptyState title="User not found" description="No user id in the URL." />
      </div>
    )
  }

  const user = userQuery.data
  const moderation = moderationQuery.data
  const economy = economyQuery.data

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-6">
        <div>
          <button onClick={() => navigate(-1)} className="text-accent hover:underline text-sm mb-2">
            ← Back
          </button>
          <h1 className="text-3xl font-bold text-ink-primary">User Investigation</h1>
          <p className="mt-1 text-ink-secondary font-mono text-sm">{userId}</p>
        </div>

        {/* Identity */}
        {userQuery.isLoading ? (
          <SkeletonGrid count={3} />
        ) : userQuery.isError ? (
          <div className="operator-card"><SectionError label="Identity" error={userQuery.error} /></div>
        ) : user ? (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Identity</p>
              <p className="text-sm font-medium text-ink-primary mt-1 break-all">{user.email}</p>
              <p className="text-xs text-ink-tertiary mt-1">
                {user.username} · {user.role} · {user.isVerified ? 'verified' : 'unverified'}
              </p>
              <Link to={`/users/${userId}`} className="text-accent hover:underline text-xs mt-2 inline-block">
                Full profile →
              </Link>
            </div>
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Account status</p>
              <p className="text-2xl font-bold mt-1 capitalize">{user.status}</p>
              <p className="text-xs text-ink-tertiary mt-1">
                Created {new Date(user.createdAt).toLocaleDateString()} · last active{' '}
                {user.lastActiveAt ? new Date(user.lastActiveAt).toLocaleDateString() : 'never'}
              </p>
            </div>
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Gameplay</p>
              <p className="text-2xl font-bold text-accent mt-1">{user.totalGamesPlayed.toLocaleString()} games</p>
              <p className="text-xs text-ink-tertiary mt-1">
                {user.totalPoints.toLocaleString()} pts · {Math.round(user.winRate * 100)}% win rate
              </p>
            </div>
          </div>
        ) : null}

        {/* Moderation */}
        <div className="operator-card p-0">
          <div className="flex items-center justify-between p-4 border-b border-panel-border">
            <h2 className="text-lg font-semibold">Moderation History</h2>
            <Link to={`/moderation/players/${rawId}`} className="text-accent hover:underline text-xs">
              Moderation profile →
            </Link>
          </div>
          {moderationQuery.isLoading ? (
            <div className="p-4"><SkeletonTable rows={3} columns={4} /></div>
          ) : moderationQuery.isError ? (
            <SectionError label="Moderation history" error={moderationQuery.error} />
          ) : moderation && moderation.actions.length > 0 ? (
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-xs text-ink-tertiary border-b border-panel-border">
                  <th className="p-3">Action</th>
                  <th className="p-3">Reason</th>
                  <th className="p-3">By</th>
                  <th className="p-3">When</th>
                </tr>
              </thead>
              <tbody>
                {moderation.actions.slice(0, 10).map((a) => (
                  <tr key={a.id} className="border-b border-panel-border last:border-0">
                    <td className="p-3 font-medium capitalize">{a.action}</td>
                    <td className="p-3">{a.reason}</td>
                    <td className="p-3 text-ink-tertiary">{a.adminEmail}</td>
                    <td className="p-3 text-ink-tertiary">{new Date(a.createdAt).toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <div className="p-6">
              <EmptyState title="No moderation actions" description="This user has a clean moderation record." />
            </div>
          )}
        </div>

        {/* Economy */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div className="operator-card">
            <h2 className="text-lg font-semibold mb-3">Economy</h2>
            {economyQuery.isLoading ? (
              <SkeletonGrid count={2} />
            ) : economyQuery.isError ? (
              <SectionError label="Economy summary" error={economyQuery.error} />
            ) : economy ? (
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-xs text-ink-tertiary">Balance</p>
                  <p className="text-2xl font-bold text-accent mt-1">{economy.currentBalance.toLocaleString()}</p>
                </div>
                <div>
                  <p className="text-xs text-ink-tertiary">Earned / Spent</p>
                  <p className="text-sm text-ink-primary mt-1">
                    +{economy.totalEarned.toLocaleString()} / −{economy.totalSpent.toLocaleString()}
                  </p>
                  <p className="text-xs text-ink-tertiary mt-1">
                    Last txn:{' '}
                    {economy.lastTransactionAt ? new Date(economy.lastTransactionAt).toLocaleString() : 'none'}
                  </p>
                </div>
              </div>
            ) : null}
          </div>

          <div className="operator-card p-0">
            <h2 className="text-lg font-semibold p-4 border-b border-panel-border">Recent Transactions</h2>
            {transactionsQuery.isLoading ? (
              <div className="p-4"><SkeletonTable rows={4} columns={3} /></div>
            ) : transactionsQuery.isError ? (
              <SectionError label="Transactions" error={transactionsQuery.error} />
            ) : transactionsQuery.data && transactionsQuery.data.items.length > 0 ? (
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-ink-tertiary border-b border-panel-border">
                    <th className="p-3">Kind</th>
                    <th className="p-3">Amount</th>
                    <th className="p-3">When</th>
                  </tr>
                </thead>
                <tbody>
                  {transactionsQuery.data.items.map((t) => (
                    <tr key={t.id} className="border-b border-panel-border last:border-0">
                      <td className="p-3">{t.description}</td>
                      <td className={`p-3 font-mono ${t.amount >= 0 ? 'text-status-healthy' : 'text-status-offline'}`}>
                        {t.amount >= 0 ? '+' : ''}{t.amount.toLocaleString()}
                      </td>
                      <td className="p-3 text-ink-tertiary">{new Date(t.createdAt).toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <div className="p-6">
                <EmptyState title="No transactions" description="No economy activity for this user." />
              </div>
            )}
          </div>
        </div>
      </div>
    </ErrorBoundary>
  )
}
