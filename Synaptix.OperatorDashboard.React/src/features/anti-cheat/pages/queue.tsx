/**
 * Anti-Cheat Review Queue page
 */

import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { QueueStats } from '../components/queue-stats'
import { FlagDetails } from '../components/flag-details'
import { VerdictForm } from '../components/verdict-form'
import {
  useQueueStats,
  useCurrentFlag,
  useSubmitVerdict,
} from '../hooks/useAntiCheatQueue'

export default function AntiCheatQueuePage() {
  usePermission('anti-cheat:read')

  const statsQuery = useQueueStats()
  const currentFlagQuery = useCurrentFlag()
  const submitVerdictMutation = useSubmitVerdict()

  const handleSubmitVerdict = async (payload: any) => {
    await submitVerdictMutation.mutateAsync(payload)
  }

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-8">
        {/* Header */}
        <div>
          <h1 className="text-3xl font-bold text-ink-primary">Anti-Cheat Review Queue</h1>
          <p className="mt-2 text-ink-secondary">Review and verdict suspected cheating activity</p>
        </div>

        {/* Stats */}
        <QueueStats
          stats={statsQuery.data || { pendingCount: 0, reviewedThisWeek: 0, completionRate: 0 }}
          isLoading={statsQuery.isLoading}
        />

        {/* Queue Workflow */}
        {statsQuery.data && statsQuery.data.pendingCount > 0 ? (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div className="lg:col-span-2">
              <FlagDetails
                flag={currentFlagQuery.data || null}
                isLoading={currentFlagQuery.isLoading}
              />
            </div>

            <div>
              <VerdictForm
                flagId={currentFlagQuery.data?.id || ''}
                onSubmit={handleSubmitVerdict}
                isLoading={submitVerdictMutation.isPending}
              />
            </div>
          </div>
        ) : (
          <EmptyState
            title="Queue is clear"
            description="All pending flags have been reviewed. Check back later."
            icon="✅"
          />
        )}

        {/* Success Message */}
        {submitVerdictMutation.isSuccess && (
          <div className="p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm">
            ✓ Verdict submitted. Loading next flag...
          </div>
        )}

        {/* Error Message */}
        {submitVerdictMutation.isError && (
          <div className="p-4 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm">
            ✕ Failed to submit verdict. Please try again.
          </div>
        )}
      </div>
    </ErrorBoundary>
  )
}
