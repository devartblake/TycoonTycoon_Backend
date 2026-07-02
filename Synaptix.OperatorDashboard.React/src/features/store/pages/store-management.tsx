/**
 * Store Management - Products, Flash Sales, Stock Policies, Reward Limits
 */

import { useState } from 'react'
import { usePermission } from '@/hooks/use-permission'
import { Button } from '@/components/ui/button'

export default function StoreManagementPage() {
  usePermission('storage:write')

  const [activeTab, setActiveTab] = useState<'products' | 'flash-sales' | 'stock-policies' | 'reward-limits'>('products')
  const [_offset, setOffset] = useState(0)
  const [_successMessage, _setSuccessMessage] = useState<string | null>(null)

  // Tab configuration
  const tabs = [
    { id: 'products', label: '📦 Products', icon: '📦' },
    { id: 'flash-sales', label: '⚡ Flash Sales', icon: '⚡' },
    { id: 'stock-policies', label: '📊 Stock Policies', icon: '📊' },
    { id: 'reward-limits', label: '🎁 Reward Limits', icon: '🎁' },
  ]

  return (
    <div className="operator-container space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-ink-primary">Store Management</h1>
        <p className="mt-2 text-ink-secondary">Manage products, sales, inventory, and rewards</p>
      </div>


      {/* Tab Navigation */}
      <div className="flex gap-2 border-b border-panel-border">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            onClick={() => {
              setActiveTab(tab.id as any)
              setOffset(0)
            }}
            className={`px-4 py-2 font-medium border-b-2 transition-colors ${
              activeTab === tab.id
                ? 'border-accent text-accent'
                : 'border-transparent text-ink-secondary hover:text-ink-primary'
            }`}
          >
            {tab.icon} {tab.label}
          </button>
        ))}
      </div>

      {/* Content Area */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold text-ink-primary">
            {tabs.find((t) => t.id === activeTab)?.label}
          </h2>
          <Button size="sm" className="text-xs">
            + Add {activeTab === 'products' ? 'Product' : activeTab === 'flash-sales' ? 'Sale' : activeTab === 'stock-policies' ? 'Policy' : 'Limit'}
          </Button>
        </div>

        {/* Coming Soon Placeholder */}
        <div className="text-center py-16 text-ink-secondary operator-card">
          <p className="text-lg font-medium">Full CRUD Interface</p>
          <p className="text-sm mt-2">
            Complete {activeTab.replace('-', ' ')} management coming soon
          </p>
          <p className="text-xs mt-4 text-ink-tertiary">
            API endpoints and mock data ready • List/Create/Edit/Delete operations functional
          </p>
        </div>

        {/* Placeholder Stats */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Total Items</p>
            <p className="text-2xl font-bold text-accent mt-1">—</p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Active</p>
            <p className="text-2xl font-bold text-status-healthy mt-1">—</p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Last Updated</p>
            <p className="text-xs text-ink-secondary mt-2">—</p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Status</p>
            <p className="text-xs text-ink-secondary mt-2">—</p>
          </div>
        </div>
      </div>

      {/* Implementation Note */}
      <div className="p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary">
        <p className="font-medium text-ink-secondary mb-2">Store Management Status:</p>
        <ul className="space-y-1">
          <li>✓ API client with full CRUD operations</li>
          <li>✓ Mock data generators for realistic testing</li>
          <li>✓ Type safety with TypeScript interfaces</li>
          <li>⏳ UI components for list/form views (Phase 2)</li>
          <li>⏳ Modal dialogs for create/edit operations (Phase 2)</li>
        </ul>
      </div>
    </div>
  )
}
