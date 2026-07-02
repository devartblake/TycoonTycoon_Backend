/**
 * Audit event filter bar
 */

import { useState } from 'react'
import { Button } from '@/components/ui/button'
import type { AuditFilter, AuditEvent } from '../types'

interface FilterBarProps {
  filters: AuditFilter
  onFiltersChange: (filters: AuditFilter) => void
}

const EVENT_TYPES: AuditEvent['eventType'][] = ['login', 'api_call', 'permission_change', 'data_export', 'deletion', 'configuration_change']
const STATUSES: ('success' | 'failure')[] = ['success', 'failure']
const COUNTRIES = ['US', 'UK', 'CA', 'DE', 'JP', 'AU', 'FR', 'SG', 'Other']

export function FilterBar({ filters, onFiltersChange }: FilterBarProps) {
  const [searchText, setSearchText] = useState(filters.searchText || '')

  const handleEventTypeChange = (eventType: AuditEvent['eventType']) => {
    onFiltersChange({
      ...filters,
      eventType: filters.eventType === eventType ? undefined : eventType,
    })
  }

  const handleStatusChange = (status: 'success' | 'failure') => {
    onFiltersChange({
      ...filters,
      status: filters.status === status ? undefined : status,
    })
  }

  const handleCountryChange = (country: string) => {
    onFiltersChange({
      ...filters,
      country: filters.country === country ? undefined : country,
    })
  }

  const handleSearchChange = (text: string) => {
    setSearchText(text)
    onFiltersChange({
      ...filters,
      searchText: text || undefined,
    })
  }

  const handleClearFilters = () => {
    setSearchText('')
    onFiltersChange({})
  }

  const hasActiveFilters = Object.values(filters).some((v) => v != null)

  return (
    <div className="operator-card space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="font-semibold text-ink-primary">Filters</h3>
        {hasActiveFilters && (
          <Button variant="ghost" size="sm" onClick={handleClearFilters} className="text-xs">
            Clear Filters
          </Button>
        )}
      </div>

      {/* Search */}
      <div>
        <label className="block text-xs font-medium text-ink-tertiary mb-1">Search</label>
        <input
          type="text"
          value={searchText}
          onChange={(e) => handleSearchChange(e.target.value)}
          className="w-full px-3 py-2 border border-panel-border rounded text-sm focus-ring"
          placeholder="Search email, IP, resource..."
        />
      </div>

      {/* Event Type */}
      <div>
        <label className="block text-xs font-medium text-ink-tertiary mb-2">Event Type</label>
        <div className="flex flex-wrap gap-2">
          {EVENT_TYPES.map((type) => (
            <button
              key={type}
              onClick={() => handleEventTypeChange(type)}
              className={`px-2 py-1 text-xs rounded border transition-colors ${
                filters.eventType === type
                  ? 'bg-accent text-white border-accent'
                  : 'border-panel-border text-ink-secondary hover:bg-bg-secondary'
              }`}
            >
              {type}
            </button>
          ))}
        </div>
      </div>

      {/* Status */}
      <div>
        <label className="block text-xs font-medium text-ink-tertiary mb-2">Status</label>
        <div className="flex gap-2">
          {STATUSES.map((status) => (
            <button
              key={status}
              onClick={() => handleStatusChange(status)}
              className={`px-3 py-1 text-xs rounded border transition-colors ${
                filters.status === status
                  ? `border-${status === 'success' ? 'status-healthy' : 'status-offline'} bg-${status === 'success' ? 'status-healthy' : 'status-offline'}/10 text-${status === 'success' ? 'status-healthy' : 'status-offline'}`
                  : 'border-panel-border text-ink-secondary hover:bg-bg-secondary'
              }`}
            >
              {status === 'success' ? '✓ Success' : '✕ Failure'}
            </button>
          ))}
        </div>
      </div>

      {/* Country */}
      <div>
        <label className="block text-xs font-medium text-ink-tertiary mb-2">Country</label>
        <div className="flex flex-wrap gap-2">
          {COUNTRIES.map((country) => (
            <button
              key={country}
              onClick={() => handleCountryChange(country)}
              className={`px-2 py-1 text-xs rounded border transition-colors ${
                filters.country === country
                  ? 'bg-accent text-white border-accent'
                  : 'border-panel-border text-ink-secondary hover:bg-bg-secondary'
              }`}
            >
              {country}
            </button>
          ))}
        </div>
      </div>
    </div>
  )
}
