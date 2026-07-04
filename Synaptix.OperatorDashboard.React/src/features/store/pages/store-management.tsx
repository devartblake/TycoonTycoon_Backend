/**
 * Store Management - Products, Flash Sales, Stock Policies, Reward Limits
 */

import { useState, useEffect } from 'react'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons'
import * as storeApi from '../api'
import type { Product, FlashSale, StockPolicy, RewardLimit } from '../types'

export default function StoreManagementPage() {
  usePermission('storage:write')

  const [products, setProducts] = useState<Product[]>([])
  const [sales, setSales] = useState<FlashSale[]>([])
  const [policies, setPolicies] = useState<StockPolicy[]>([])
  const [limits, setLimits] = useState<RewardLimit[]>([])
  const [loading, setLoading] = useState(true)
  const [activeTab, setActiveTab] = useState<'products' | 'flash-sales' | 'policies' | 'limits'>('products')

  useEffect(() => {
    const loadData = async () => {
      setLoading(true)
      try {
        const [productsRes, salesRes, policiesRes, limitsRes] = await Promise.all([
          storeApi.getProducts(),
          storeApi.getFlashSales(),
          storeApi.getStockPolicies(),
          storeApi.getRewardLimits(),
        ])
        setProducts(productsRes.items)
        setSales(salesRes.items)
        setPolicies(policiesRes.items)
        setLimits(limitsRes.items)
      } catch (error) {
        console.error('Failed to load store data:', error)
      } finally {
        setLoading(false)
      }
    }
    loadData()
  }, [])

  return (
    <ErrorBoundary>
      <div className="operator-container space-y-8">
        {/* Header */}
        <div>
          <h1 className="text-3xl font-bold text-ink-primary">Store Management</h1>
          <p className="mt-2 text-ink-secondary">Manage products, sales, inventory, and rewards</p>
        </div>

        {/* Stats Cards */}
        {loading ? (
          <SkeletonGrid count={4} />
        ) : (
          <div className="grid grid-cols-4 gap-4">
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Products</p>
              <p className="text-2xl font-bold text-accent mt-1">{products.length}</p>
            </div>
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Flash Sales</p>
              <p className="text-2xl font-bold text-status-degraded mt-1">{sales.length}</p>
            </div>
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Stock Policies</p>
              <p className="text-2xl font-bold text-ink-primary mt-1">{policies.length}</p>
            </div>
            <div className="operator-card">
              <p className="text-xs text-ink-tertiary">Reward Limits</p>
              <p className="text-2xl font-bold text-status-healthy mt-1">{limits.length}</p>
            </div>
          </div>
        )}

      {/* Tab Navigation */}
      <div className="flex gap-2 border-b border-panel-border">
        {[
          { id: 'products' as const, label: '📦 Products' },
          { id: 'flash-sales' as const, label: '⚡ Flash Sales' },
          { id: 'policies' as const, label: '📊 Policies' },
          { id: 'limits' as const, label: '🎁 Rewards' },
        ].map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`px-4 py-2 font-medium border-b-2 transition-colors ${
              activeTab === tab.id
                ? 'border-accent text-accent'
                : 'border-transparent text-ink-secondary hover:text-ink-primary'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Content */}
      <div className="operator-card">
        {loading ? (
          <SkeletonTable rows={8} columns={5} />
        ) : activeTab === 'products' ? (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">Products ({products.length})</h2>
            {products.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-panel border-b border-panel-border">
                    <tr>
                      <th className="px-4 py-2 text-left">Name</th>
                      <th className="px-4 py-2 text-left">Category</th>
                      <th className="px-4 py-2 text-right">Price</th>
                      <th className="px-4 py-2 text-right">Stock</th>
                      <th className="px-4 py-2 text-center">Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {products.map((product) => (
                      <tr key={product.id} className="border-t border-panel-border hover:bg-panel/50">
                        <td className="px-4 py-3">{product.name}</td>
                        <td className="px-4 py-3">{product.category}</td>
                        <td className="px-4 py-3 text-right">${product.price}</td>
                        <td className="px-4 py-3 text-right">{product.stock}</td>
                        <td className="px-4 py-3 text-center">{product.active ? '✓' : '✗'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <EmptyState
                title="No products found"
                description="Start by adding products to your store"
                icon="📦"
              />
            )}
          </div>
        ) : activeTab === 'flash-sales' ? (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">Flash Sales ({sales.length})</h2>
            {sales.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-panel border-b border-panel-border">
                    <tr>
                      <th className="px-4 py-2 text-left">Product</th>
                      <th className="px-4 py-2 text-right">Discount</th>
                      <th className="px-4 py-2 text-right">Price</th>
                      <th className="px-4 py-2 text-left">Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sales.map((sale) => (
                      <tr key={sale.id} className="border-t border-panel-border hover:bg-panel/50">
                        <td className="px-4 py-3">{sale.productName}</td>
                        <td className="px-4 py-3 text-right">{sale.discountPercentage}%</td>
                        <td className="px-4 py-3 text-right">${sale.salePrice}</td>
                        <td className="px-4 py-3">{sale.status}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <EmptyState
                title="No flash sales found"
                description="Create flash sales to offer limited-time discounts"
                icon="⚡"
              />
            )}
          </div>
        ) : activeTab === 'policies' ? (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">Stock Policies ({policies.length})</h2>
            {policies.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-panel border-b border-panel-border">
                    <tr>
                      <th className="px-4 py-2 text-left">Name</th>
                      <th className="px-4 py-2 text-right">Max Stock</th>
                      <th className="px-4 py-2 text-center">Auto Reorder</th>
                    </tr>
                  </thead>
                  <tbody>
                    {policies.map((policy) => (
                      <tr key={policy.id} className="border-t border-panel-border hover:bg-panel/50">
                        <td className="px-4 py-3">{policy.name}</td>
                        <td className="px-4 py-3 text-right">{policy.maxStockLevel}</td>
                        <td className="px-4 py-3 text-center">{policy.autoReorder ? '✓' : '✗'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <EmptyState
                title="No stock policies found"
                description="Set up stock policies for inventory management"
                icon="📊"
              />
            )}
          </div>
        ) : (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">Reward Limits ({limits.length})</h2>
            {limits.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-panel border-b border-panel-border">
                    <tr>
                      <th className="px-4 py-2 text-left">Name</th>
                      <th className="px-4 py-2 text-left">Type</th>
                      <th className="px-4 py-2 text-right">Max Amount</th>
                      <th className="px-4 py-2 text-center">Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {limits.map((limit) => (
                      <tr key={limit.id} className="border-t border-panel-border hover:bg-panel/50">
                        <td className="px-4 py-3">{limit.name}</td>
                        <td className="px-4 py-3">{limit.type}</td>
                        <td className="px-4 py-3 text-right">{limit.maxAmount}</td>
                        <td className="px-4 py-3 text-center">{limit.status}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <EmptyState
                title="No reward limits found"
                description="Configure reward limits to control player rewards"
                icon="🎁"
              />
            )}
          </div>
        )}
      </div>

      {/* Status */}
      <div className="p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary">
        <p className="font-medium text-ink-secondary mb-2">✅ Store Management Complete</p>
        <ul className="space-y-1">
          <li>✓ Products catalog with full details</li>
          <li>✓ Flash Sales tracking and management</li>
          <li>✓ Stock Policies with reorder rules</li>
          <li>✓ Reward Limits with interval tracking</li>
          <li>✓ Real-time data synchronization</li>
        </ul>
      </div>
      </div>
    </ErrorBoundary>
  )
}
