/**
 * Questions filter bar
 */

import { useCategories } from '../hooks/useContent'
import { Button } from '@/components/ui/button'
import type { QuestionFilter } from '../types'

interface FilterBarProps {
  filters: QuestionFilter
  onFiltersChange: (filters: QuestionFilter) => void
}

const STATUS_OPTIONS = ['pending', 'approved', 'rejected'] as const
const DIFFICULTY_OPTIONS = ['easy', 'medium', 'hard'] as const

export function FilterBar({ filters, onFiltersChange }: FilterBarProps) {
  const categoriesQuery = useCategories()
  const categories = categoriesQuery.data || []

  const handleStatusChange = (status: typeof STATUS_OPTIONS[number]) => {
    onFiltersChange({
      ...filters,
      status: filters.status === status ? undefined : status,
    })
  }

  const handleDifficultyChange = (difficulty: typeof DIFFICULTY_OPTIONS[number]) => {
    onFiltersChange({
      ...filters,
      difficulty: filters.difficulty === difficulty ? undefined : difficulty,
    })
  }

  const handleCategoryChange = (category: string) => {
    onFiltersChange({
      ...filters,
      category: filters.category === category ? undefined : category,
    })
  }

  const handleClearFilters = () => {
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

      {/* Status */}
      <div>
        <label className="block text-xs font-medium text-ink-tertiary mb-2">Status</label>
        <div className="space-y-1">
          {STATUS_OPTIONS.map((status) => (
            <label key={status} className="flex items-center gap-2 p-2 rounded hover:bg-bg-secondary cursor-pointer">
              <input
                type="checkbox"
                checked={filters.status === status}
                onChange={() => handleStatusChange(status)}
                className="cursor-pointer"
              />
              <span className="text-xs text-ink-secondary capitalize">{status}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Difficulty */}
      <div>
        <label className="block text-xs font-medium text-ink-tertiary mb-2">Difficulty</label>
        <div className="space-y-1">
          {DIFFICULTY_OPTIONS.map((difficulty) => (
            <label key={difficulty} className="flex items-center gap-2 p-2 rounded hover:bg-bg-secondary cursor-pointer">
              <input
                type="checkbox"
                checked={filters.difficulty === difficulty}
                onChange={() => handleDifficultyChange(difficulty)}
                className="cursor-pointer"
              />
              <span className="text-xs text-ink-secondary capitalize">{difficulty}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Category */}
      {categories.length > 0 && (
        <div>
          <label className="block text-xs font-medium text-ink-tertiary mb-2">Category</label>
          <div className="space-y-1 max-h-40 overflow-y-auto">
            {categories.map((category) => (
              <label key={category} className="flex items-center gap-2 p-2 rounded hover:bg-bg-secondary cursor-pointer">
                <input
                  type="checkbox"
                  checked={filters.category === category}
                  onChange={() => handleCategoryChange(category)}
                  className="cursor-pointer"
                />
                <span className="text-xs text-ink-secondary">{category}</span>
              </label>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
