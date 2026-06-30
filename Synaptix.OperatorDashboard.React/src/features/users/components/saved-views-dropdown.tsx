/**
 * Saved views dropdown for quick filter presets
 */

import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useSavedViews, useCreateSavedView, useDeleteSavedView } from '../hooks/useSavedViews'
import type { UserFilters, SavedView } from '../types'

interface SavedViewsDropdownProps {
  currentFilters: UserFilters
  onLoadView: (filters: UserFilters) => void
}

export function SavedViewsDropdown({ currentFilters, onLoadView }: SavedViewsDropdownProps) {
  const [isOpen, setIsOpen] = useState(false)
  const [newViewName, setNewViewName] = useState('')
  const [showSaveForm, setShowSaveForm] = useState(false)

  const { data: savedViews = [], isLoading } = useSavedViews()
  const createView = useCreateSavedView()
  const deleteView = useDeleteSavedView()

  const handleSaveView = async () => {
    if (!newViewName.trim()) return
    await createView.mutateAsync({ name: newViewName, filters: currentFilters })
    setNewViewName('')
    setShowSaveForm(false)
  }

  const handleDeleteView = (viewId: string) => {
    if (confirm('Delete this saved view?')) {
      deleteView.mutate(viewId)
    }
  }

  return (
    <div className="relative">
      <Button variant="outline" size="sm" onClick={() => setIsOpen(!isOpen)}>
        📌 Saved Views ({savedViews.length})
      </Button>

      {isOpen && (
        <div className="absolute top-full right-0 mt-2 w-64 bg-panel-bg border border-panel-border rounded shadow-lg z-50">
          {/* Saved views list */}
          <div className="max-h-64 overflow-y-auto">
            {isLoading ? (
              <div className="p-4 text-sm text-ink-secondary">Loading...</div>
            ) : savedViews.length === 0 ? (
              <div className="p-4 text-sm text-ink-secondary">No saved views yet</div>
            ) : (
              <ul className="space-y-1 p-2">
                {savedViews.map((view: SavedView) => (
                  <li key={view.id} className="flex items-center justify-between hover:bg-bg-secondary rounded p-2 text-sm">
                    <button
                      onClick={() => {
                        onLoadView(view.filters)
                        setIsOpen(false)
                      }}
                      className="flex-1 text-left text-accent hover:underline"
                    >
                      {view.name}
                    </button>
                    <button
                      onClick={() => handleDeleteView(view.id)}
                      className="ml-2 text-ink-tertiary hover:text-status-offline text-xs px-1"
                      title="Delete view"
                    >
                      ✕
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>

          {/* Save new view form */}
          {showSaveForm ? (
            <div className="border-t border-panel-border p-3 space-y-2">
              <input
                type="text"
                placeholder="View name..."
                value={newViewName}
                onChange={(e) => setNewViewName(e.target.value)}
                className="w-full px-2 py-1 border border-panel-border rounded text-sm focus-ring"
                autoFocus
              />
              <div className="flex gap-2">
                <Button
                  size="sm"
                  variant="default"
                  onClick={handleSaveView}
                  disabled={!newViewName.trim() || createView.isPending}
                  className="flex-1 text-xs"
                >
                  {createView.isPending ? 'Saving...' : 'Save'}
                </Button>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => {
                    setShowSaveForm(false)
                    setNewViewName('')
                  }}
                  className="flex-1 text-xs"
                >
                  Cancel
                </Button>
              </div>
            </div>
          ) : (
            <div className="border-t border-panel-border p-3">
              <Button
                size="sm"
                variant="secondary"
                onClick={() => setShowSaveForm(true)}
                className="w-full text-xs"
              >
                + Save Current View
              </Button>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
