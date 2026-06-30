/**
 * Filter bar for users list
 */

// import React from 'react'
import { Button } from '@/components/ui/button'
import type { User, UserFilters } from '../types'

interface FilterBarProps {
  filters: UserFilters
  onFiltersChange: (filters: Partial<UserFilters>) => void
  onClearFilters: () => void
}

export function FilterBar({ filters, onFiltersChange, onClearFilters }: FilterBarProps) {
  return (
    <div className="space-y-4 p-4 bg-bg-secondary rounded border border-panel-border">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Email filter */}
        <div>
          <label htmlFor="email-filter" className="block text-sm font-medium text-ink-primary mb-1">
            Email
          </label>
          <input
            id="email-filter"
            type="text"
            placeholder="Search email..."
            value={filters.email || ''}
            onChange={(e) => onFiltersChange({ email: e.target.value })}
            className="w-full px-3 py-2 border border-panel-border rounded focus-ring text-sm"
          />
        </div>

        {/* Status filter */}
        <div>
          <label htmlFor="status-filter" className="block text-sm font-medium text-ink-primary mb-1">
            Status
          </label>
          <select
            id="status-filter"
            value={filters.status || ''}
            onChange={(e) => onFiltersChange({ status: (e.target.value as User['status']) || undefined })}
            className="w-full px-3 py-2 border border-panel-border rounded focus-ring text-sm"
          >
            <option value="">All Statuses</option>
            <option value="active">Active</option>
            <option value="suspended">Suspended</option>
            <option value="banned">Banned</option>
            <option value="inactive">Inactive</option>
          </select>
        </div>

        {/* Flagged filter */}
        <div>
          <label htmlFor="flagged-filter" className="block text-sm font-medium text-ink-primary mb-1">
            Flagged
          </label>
          <select
            id="flagged-filter"
            value={filters.flagged ? 'true' : 'false'}
            onChange={(e) => onFiltersChange({ flagged: e.target.value === 'true' || undefined })}
            className="w-full px-3 py-2 border border-panel-border rounded focus-ring text-sm"
          >
            <option value="false">All</option>
            <option value="true">Flagged Only</option>
          </select>
        </div>
      </div>

      {/* Action buttons */}
      <div className="flex gap-2">
        <Button variant="secondary" size="sm" onClick={onClearFilters}>
          Clear Filters
        </Button>
      </div>
    </div>
  )
}
