/**
 * Users Triage + Saved Views page
 */

import { useState } from 'react'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonTable } from '@/components/shared/skeletons'
import { FilterBar } from '../components/filter-bar'
import { SavedViewsDropdown } from '../components/saved-views-dropdown'
import { UsersTable } from '../components/users-table'
import { BulkActionsBar } from '../components/bulk-actions-bar'
import { useUsers } from '../hooks/useUsers'
import type { UserFilters } from '../types'

export default function UsersList() {
  usePermission('users:read')

  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const { users, filters, isLoading, isError, applyFilters, clearFilters } = useUsers()

  const handleLoadView = (viewFilters: UserFilters) => {
    applyFilters(viewFilters)
  }

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-6 pb-24">
        {/* Header */}
        <div>
          <h1 className="text-3xl font-bold text-ink-primary">Users Triage</h1>
          <p className="mt-2 text-ink-secondary">Investigate, filter, and manage player accounts</p>
        </div>

        {/* Saved views + filter controls */}
        <div className="flex items-center gap-4">
          <SavedViewsDropdown currentFilters={filters} onLoadView={handleLoadView} />
        </div>

        {/* Filter bar */}
        <FilterBar filters={filters} onFiltersChange={applyFilters} onClearFilters={clearFilters} />

        {/* Error state */}
        {isError && (
          <div className="p-4 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm">
            Failed to load users. Please try again.
          </div>
        )}

        {/* Users table */}
        {isLoading ? (
          <SkeletonTable rows={10} columns={5} />
        ) : users.length > 0 ? (
          <UsersTable users={users} isLoading={isLoading} onSelectionChange={setSelectedIds} />
        ) : (
          <EmptyState
            title="No users found"
            description="Try adjusting your filters to find users"
            icon="👥"
          />
        )}

        {/* Bulk actions bar */}
        <BulkActionsBar selectedIds={selectedIds} onActionComplete={() => setSelectedIds([])} />
      </div>
    </ErrorBoundary>
  )
}
