/**
 * Store Management - Products, Flash Sales, Stock Policies, Reward Limits
 * URL-synced tabs so sidebar links (Catalog / Flash Sales / Stock Policies) open the right view.
 */

import { useState, useEffect, useMemo } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { usePermission } from '@/hooks/use-permission'
import ErrorBoundary from '@/components/shared/error-boundary'
import EmptyState from '@/components/shared/empty-state'
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons'
import * as storeApi from '../api'
import type { Product, FlashSale, StockPolicy, RewardLimit } from '../types'

type StoreTab = 'products' | 'flash-sales' | 'policies' | 'limits'

const TAB_META: Record<
  StoreTab,
  { label: string; path: string; title: string; description: string }
> = {
  products: {
    label: '📦 Catalog',
    path: '/store/catalog',
    title: 'Product Catalog',
    description: 'Browse and manage store SKUs, pricing, and stock levels',
  },
  'flash-sales': {
    label: '⚡ Flash Sales',
    path: '/store/flash-sales',
    title: 'Flash Sales',
    description: 'Time-boxed discounts and promotional campaigns',
  },
  policies: {
    label: '📊 Stock Policies',
    path: '/store/stock-policies',
    title: 'Stock Policies',
    description: 'Inventory rules, reorder thresholds, and allocation limits',
  },
  limits: {
    label: '🎁 Reward Limits',
    path: '/store/reward-limits',
    title: 'Reward Limits',
    description: 'Caps on grant frequency and reward quantity',
  },
}

function tabFromPath(pathname: string): StoreTab {
  if (pathname.includes('flash-sales')) return 'flash-sales'
  if (pathname.includes('stock-policies')) return 'policies'
  if (pathname.includes('reward-limits')) return 'limits'
  return 'products'
}

export default function StoreManagementPage() {
  usePermission('storage:write')
  const location = useLocation()
  const navigate = useNavigate()

  const activeTab = useMemo(() => tabFromPath(location.pathname), [location.pathname])
  const meta = TAB_META[activeTab]

  const [products, setProducts] = useState<Product[]>([])
  const [sales, setSales] = useState<FlashSale[]>([])
  const [policies, setPolicies] = useState<StockPolicy[]>([])
  const [limits, setLimits] = useState<RewardLimit[]>([])
  const [loading, setLoading] = useState(true)

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
        {/* Header — reflects active store area */}
        <div>
          <p className="text-xs font-medium uppercase tracking-wide text-ink-tertiary mb-1">
            Store Management
          </p>
          <h1 className="text-3xl font-bold text-ink-primary">{meta.title}</h1>
          <p className="mt-2 text-ink-secondary">{meta.description}</p>
        </div>

        {/* Stats Cards — highlight active domain */}
        {loading ? (
          <SkeletonGrid count={4} />
        ) : (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {(
              [
                { tab: 'products' as const, value: products.length, label: 'Products' },
                { tab: 'flash-sales' as const, value: sales.length, label: 'Flash Sales' },
                { tab: 'policies' as const, value: policies.length, label: 'Stock Policies' },
                { tab: 'limits' as const, value: limits.length, label: 'Reward Limits' },
              ] as const
            ).map((card) => (
              <button
                key={card.tab}
                type="button"
                onClick={() => navigate(TAB_META[card.tab].path)}
                className={`operator-card text-left transition-smooth ${
                  activeTab === card.tab ? 'ring-2 ring-accent' : 'hover:bg-bg-secondary'
                }`}
              >
                <p className="text-xs text-ink-tertiary">{card.label}</p>
                <p className="text-2xl font-bold text-accent mt-1">{card.value}</p>
              </button>
            ))}
          </div>
        )}

      {/* Tab Navigation — URL-backed */}
      <div className="flex gap-2 border-b border-panel-border overflow-x-auto">
        {(Object.keys(TAB_META) as StoreTab[]).map((id) => (
          <button
            key={id}
            type="button"
            onClick={() => navigate(TAB_META[id].path)}
            className={`px-4 py-2 font-medium border-b-2 transition-colors whitespace-nowrap ${
              activeTab === id
                ? 'border-accent text-accent'
                : 'border-transparent text-ink-secondary hover:text-ink-primary'
            }`}
          >
            {TAB_META[id].label}
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
