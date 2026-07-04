/**
 * Moderation Player Profile page
 */

import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import { SkeletonGrid } from '@/components/shared/skeletons'
import { PlayerHeader } from '../components/player-header'
import { ActionPanel } from '../components/action-panel'
import { ActionHistory } from '../components/action-history'
import { ActivityTimeline } from '../components/activity-timeline'
import {
  usePlayerModeration,
  useBanPlayer,
  useUnbanPlayer,
  useSuspendPlayer,
  useUnsuspendPlayer,
  useWarnPlayer,
} from '../hooks/useModeration'

export default function PlayerProfilePage() {
  usePermission('moderation:write')

  const { playerId } = useParams<{ playerId: string }>()
  const navigate = useNavigate()
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  if (!playerId) {
    return (
      <div className="operator-container text-center py-12">
        <p className="text-ink-secondary">Player not found</p>
      </div>
    )
  }

  const moderationQuery = usePlayerModeration(playerId)
  const banMutation = useBanPlayer()
  const unbanMutation = useUnbanPlayer()
  const suspendMutation = useSuspendPlayer()
  const unsuspendMutation = useUnsuspendPlayer()
  const warnMutation = useWarnPlayer()

  const moderation = moderationQuery.data

  const handleBan = async (reason: string, notes?: string) => {
    await banMutation.mutateAsync({ playerId, reason, notes })
    setSuccessMessage('Player banned successfully')
    setTimeout(() => setSuccessMessage(null), 3000)
  }

  const handleUnban = async (reason: string) => {
    await unbanMutation.mutateAsync({ playerId, reason })
    setSuccessMessage('Player unbanned successfully')
    setTimeout(() => setSuccessMessage(null), 3000)
  }

  const handleSuspend = async (durationHours: number, reason: string, notes?: string) => {
    await suspendMutation.mutateAsync({ playerId, durationHours, reason, notes })
    setSuccessMessage('Player suspended successfully')
    setTimeout(() => setSuccessMessage(null), 3000)
  }

  const handleUnsuspend = async (reason: string) => {
    await unsuspendMutation.mutateAsync({ playerId, reason })
    setSuccessMessage('Player unsuspended successfully')
    setTimeout(() => setSuccessMessage(null), 3000)
  }

  const handleWarn = async (reason: string, notes?: string) => {
    await warnMutation.mutateAsync({ playerId, reason, notes })
    setSuccessMessage('Player warned successfully')
    setTimeout(() => setSuccessMessage(null), 3000)
  }

  const isLoading = moderationQuery.isLoading
  const isMutating = banMutation.isPending || unbanMutation.isPending || suspendMutation.isPending || unsuspendMutation.isPending || warnMutation.isPending

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <button
              onClick={() => navigate(-1)}
              className="text-accent hover:underline text-sm mb-2"
            >
              ← Back
            </button>
            <h1 className="text-2xl font-bold text-ink-primary">Player Moderation</h1>
          </div>
        </div>

        {/* Success Message */}
        {successMessage && (
          <div className="p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm">
            ✓ {successMessage}
          </div>
        )}

        {/* Player Header */}
        {isLoading ? (
          <SkeletonGrid count={3} />
        ) : moderation ? (
          <PlayerHeader profile={moderation.profile} isLoading={false} />
        ) : null}

        {/* Main Content Grid */}
        {moderation ? (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Left: Action Panel */}
            <div>
              <ActionPanel
                profile={moderation.profile}
                onBan={handleBan}
                onUnban={handleUnban}
                onSuspend={handleSuspend}
                onUnsuspend={handleUnsuspend}
                onWarn={handleWarn}
                isLoading={isMutating}
              />
            </div>

            {/* Right: History and Activity */}
            <div className="lg:col-span-2 space-y-6">
              {/* Moderation History */}
              <ActionHistory actions={moderation.actions} isLoading={isLoading} />

              {/* Activity Timeline */}
              <ActivityTimeline activities={moderation.activity} isLoading={isLoading} />
            </div>
          </div>
        ) : null}
      </div>
    </ErrorBoundary>
  )
}
