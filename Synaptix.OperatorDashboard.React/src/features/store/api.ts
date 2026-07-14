/**
 * Store API client
 *
 * Reconciled to the real backend route surface under /admin/store
 * (Synaptix.Backend.Api/Features/AdminStore). Products map to the backend
 * "catalog" of store items; stock-policies and reward-limits are keyed by
 * sku/rewardId (not a separate id). Flash-sales already matched the backend and
 * are unchanged. Functions keep their existing return types and adapt shapes
 * internally.
 *
 * Known fidelity gaps (backend does not expose these fields today; see the store
 * reconciliation sub-issue #422):
 *   - Product.rarity/stock, StockPolicy.reorderLevel/reorderQuantity, and
 *     RewardLimit.currentAmount are best-effort placeholders.
 *   - StoreStats aggregates what is available (catalog count); revenue/low-stock
 *     are not exposed and default to 0.
 *   - create* map to the backend upsert semantics; deleteRewardLimit has no
 *     backend route and throws a clear error.
 */

import { apiGet, apiPost, apiPut, apiPatch, apiDelete } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type {
  ProductsListResponse,
  FlashSalesListResponse,
  StockPoliciesListResponse,
  RewardLimitsListResponse,
  StoreStats,
  Product,
  FlashSale,
  StockPolicy,
  RewardLimit,
  PlayerStockResponse,
  PurchaseAnalytics,
  StockResetsResponse,
  StockResetItem,
} from './types'

// ── Backend DTO shapes (camelCase) ───────────────────────────────────────────

interface BackendStoreItemDto {
  id: string
  sku: string
  name: string
  description: string
  itemType: string
  priceCoins: number
  priceDiamonds: number
  grantQuantity: number
  maxPerPlayer: number
  isActive: boolean
  mediaKey: string | null
  sortOrder: number
  createdAtUtc: string
  updatedAtUtc: string
}

interface BackendPage<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

interface BackendStockPolicyDto {
  sku: string
  maxQuantityPerUser: number
  resetInterval: string
  isActive: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

interface BackendRewardLimitDto {
  rewardId: string
  maxClaimsPerInterval: number
  resetInterval: string
  isActive: boolean
  updatedAtUtc: string
}

// ── Helpers ──────────────────────────────────────────────────────────────────

function offsetToPage(offset: number, limit: number): number {
  return Math.floor(offset / Math.max(1, limit)) + 1
}

function slug(value: string): string {
  return value.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
}

function toProduct(dto: BackendStoreItemDto): Product {
  return {
    id: dto.id,
    name: dto.name,
    description: dto.description,
    price: dto.priceCoins,
    category: dto.itemType,
    rarity: 'common',
    stock: dto.grantQuantity,
    maxStock: dto.maxPerPlayer,
    active: dto.isActive,
    createdAt: dto.createdAtUtc,
    updatedAt: dto.updatedAtUtc,
  }
}

function mapRewardType(resetInterval: string): RewardLimit['type'] {
  const v = resetInterval.toLowerCase()
  if (v.includes('week')) return 'weekly'
  if (v.includes('season')) return 'seasonal'
  return 'daily'
}

function toStockPolicy(dto: BackendStockPolicyDto): StockPolicy {
  return {
    id: dto.sku,
    name: dto.sku,
    description: '',
    reorderLevel: 0,
    reorderQuantity: 0,
    maxStockLevel: dto.maxQuantityPerUser,
    autoReorder: dto.isActive,
    createdAt: dto.createdAtUtc,
    updatedAt: dto.updatedAtUtc,
  }
}

function toRewardLimit(dto: BackendRewardLimitDto): RewardLimit {
  return {
    id: dto.rewardId,
    name: dto.rewardId,
    type: mapRewardType(dto.resetInterval),
    maxAmount: dto.maxClaimsPerInterval,
    currentAmount: 0,
    status: dto.isActive ? 'active' : 'paused',
    createdAt: dto.updatedAtUtc,
  }
}

async function getBackendProduct(id: string): Promise<BackendStoreItemDto | undefined> {
  const res = await apiGet<BackendPage<BackendStoreItemDto>>('/admin/store/catalog?page=1&pageSize=200')
  return res.items.find((i) => i.id === id)
}

// ── Products (backend catalog) ───────────────────────────────────────────────

export async function getProducts(offset: number = 0, limit: number = 50): Promise<ProductsListResponse> {
  if (getMockMode()) return mockApi.mockGetProducts(offset, limit)
  const res = await apiGet<BackendPage<BackendStoreItemDto>>(
    `/admin/store/catalog?page=${offsetToPage(offset, limit)}&pageSize=${limit}`
  )
  return { items: res.items.map(toProduct), total: res.totalItems, offset, limit }
}

export async function createProduct(product: Omit<Product, 'id' | 'createdAt' | 'updatedAt'>): Promise<Product> {
  if (getMockMode()) return mockApi.mockCreateProduct(product)
  const created = await apiPost<{ id: string; sku: string }>('/admin/store/catalog', {
    sku: slug(product.name),
    name: product.name,
    description: product.description,
    itemType: product.category,
    priceCoins: product.price,
    priceDiamonds: 0,
    grantQuantity: Math.max(1, product.stock),
    maxPerPlayer: product.maxStock,
    mediaKey: null,
    sortOrder: 0,
  })
  return { ...product, id: created.id, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }
}

export async function updateProduct(id: string, product: Partial<Product>): Promise<Product> {
  if (getMockMode()) return mockApi.mockUpdateProduct(id, product)
  await apiPatch(`/admin/store/catalog/${id}`, {
    name: product.name,
    description: product.description,
    priceCoins: product.price,
    isActive: product.active,
  })
  const dto = await getBackendProduct(id)
  if (!dto) throw new Error(`Store item ${id} not found`)
  return toProduct(dto)
}

export async function deleteProduct(id: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockDeleteProduct(id)
  await apiDelete(`/admin/store/catalog/${id}`)
  return { success: true }
}

// ── Flash Sales (Django: showAll; response { sales: AdminFlashSaleDto[] }) ───

interface BackendFlashSaleDto {
  id: string
  sku: string
  discountPercent: number
  startsAtUtc: string
  endsAtUtc: string
  isActive: boolean
  reason?: string | null
  createdAtUtc: string
}

function toFlashSale(dto: BackendFlashSaleDto): FlashSale {
  const now = Date.now()
  const start = new Date(dto.startsAtUtc).getTime()
  const end = new Date(dto.endsAtUtc).getTime()
  let status: FlashSale['status'] = 'scheduled'
  if (now >= end || !dto.isActive) status = 'ended'
  else if (now >= start) status = 'active'
  return {
    id: dto.id,
    name: dto.reason || dto.sku,
    productId: dto.sku,
    productName: dto.sku,
    discountPercentage: dto.discountPercent,
    originalPrice: 0,
    salePrice: 0,
    startTime: dto.startsAtUtc,
    endTime: dto.endsAtUtc,
    maxUnits: 0,
    unitsSold: 0,
    status,
    createdAt: dto.createdAtUtc,
  }
}

export async function getFlashSales(offset: number = 0, limit: number = 50): Promise<FlashSalesListResponse> {
  if (getMockMode()) return mockApi.mockGetFlashSales(offset, limit)
  // Django/backend: GET /admin/store/flash-sales?showAll=true (not offset/limit)
  const res = await apiGet<{ sales: BackendFlashSaleDto[] }>('/admin/store/flash-sales?showAll=true')
  const all = (res.sales ?? []).map(toFlashSale)
  const items = all.slice(offset, offset + limit)
  return { items, total: all.length, offset, limit }
}

export async function createFlashSale(sale: Omit<FlashSale, 'id' | 'createdAt'>): Promise<FlashSale> {
  if (getMockMode()) return mockApi.mockCreateFlashSale(sale)
  const created = await apiPost<{ id: string }>('/admin/store/flash-sales', {
    sku: sale.productId || sale.productName || slug(sale.name),
    discountPercent: sale.discountPercentage,
    startsAtUtc: sale.startTime,
    endsAtUtc: sale.endTime,
    reason: sale.name || null,
  })
  return {
    ...sale,
    id: created.id,
    createdAt: new Date().toISOString(),
  }
}

export async function updateFlashSale(id: string, sale: Partial<FlashSale>): Promise<FlashSale> {
  if (getMockMode()) return mockApi.mockUpdateFlashSale(id, sale)
  await apiPut(`/admin/store/flash-sales/${id}`, {
    discountPercent: sale.discountPercentage,
    startsAtUtc: sale.startTime,
    endsAtUtc: sale.endTime,
    reason: sale.name ?? null,
  })
  const list = await getFlashSales(0, 500)
  const found = list.items.find((s) => s.id === id)
  if (!found) throw new Error(`Flash sale ${id} not found after update`)
  return found
}

export async function deleteFlashSale(id: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockDeleteFlashSale(id)
  await apiDelete(`/admin/store/flash-sales/${id}`)
  return { success: true }
}

/** Django bulk_reset_stock parity */
export async function bulkResetStock(skus: string[], reason?: string): Promise<{ success: boolean }> {
  if (getMockMode()) return { success: true }
  await apiPost('/admin/store/stock-policies/bulk-reset', { skus, reason: reason ?? null })
  return { success: true }
}

// ── Stock Policies (keyed by sku) ────────────────────────────────────────────

export async function getStockPolicies(offset: number = 0, limit: number = 50): Promise<StockPoliciesListResponse> {
  if (getMockMode()) return mockApi.mockGetStockPolicies(offset, limit)
  const res = await apiGet<{ policies: BackendStockPolicyDto[] }>('/admin/store/stock-policies')
  const items = res.policies.map(toStockPolicy)
  return { items, total: items.length, offset, limit }
}

export async function createStockPolicy(policy: Omit<StockPolicy, 'id' | 'createdAt' | 'updatedAt'>): Promise<StockPolicy> {
  if (getMockMode()) return mockApi.mockCreateStockPolicy(policy)
  // Backend upserts by sku via PUT; there is no separate create route.
  const sku = slug(policy.name)
  const dto = await apiPut<BackendStockPolicyDto>(`/admin/store/stock-policies/${sku}`, {
    maxQuantityPerUser: policy.maxStockLevel,
    resetInterval: 'daily',
    isActive: policy.autoReorder,
  })
  return toStockPolicy(dto)
}

export async function updateStockPolicy(id: string, policy: Partial<StockPolicy>): Promise<StockPolicy> {
  if (getMockMode()) return mockApi.mockUpdateStockPolicy(id, policy)
  // `id` is the sku (see toStockPolicy).
  const dto = await apiPut<BackendStockPolicyDto>(`/admin/store/stock-policies/${id}`, {
    maxQuantityPerUser: policy.maxStockLevel,
    isActive: policy.autoReorder,
  })
  return toStockPolicy(dto)
}

export async function deleteStockPolicy(id: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockDeleteStockPolicy(id)
  await apiDelete(`/admin/store/stock-policies/${id}`)
  return { success: true }
}

// ── Reward Limits (keyed by rewardId) ────────────────────────────────────────

export async function getRewardLimits(offset: number = 0, limit: number = 50): Promise<RewardLimitsListResponse> {
  if (getMockMode()) return mockApi.mockGetRewardLimits(offset, limit)
  const res = await apiGet<{ items: BackendRewardLimitDto[]; total: number }>('/admin/store/reward-limits')
  return { items: res.items.map(toRewardLimit), total: res.total, offset, limit }
}

export async function createRewardLimit(limit: Omit<RewardLimit, 'id' | 'createdAt'>): Promise<RewardLimit> {
  if (getMockMode()) return mockApi.mockCreateRewardLimit(limit)
  // Backend upserts by rewardId via PUT; there is no separate create route.
  const rewardId = slug(limit.name)
  const dto = await apiPut<BackendRewardLimitDto>(`/admin/store/reward-limits/${rewardId}`, {
    maxClaimsPerInterval: limit.maxAmount,
    resetInterval: limit.type,
    isActive: limit.status === 'active',
  })
  return toRewardLimit(dto)
}

export async function updateRewardLimit(id: string, limit: Partial<RewardLimit>): Promise<RewardLimit> {
  if (getMockMode()) return mockApi.mockUpdateRewardLimit(id, limit)
  // `id` is the rewardId (see toRewardLimit).
  const dto = await apiPut<BackendRewardLimitDto>(`/admin/store/reward-limits/${id}`, {
    maxClaimsPerInterval: limit.maxAmount,
    resetInterval: limit.type,
    isActive: limit.status ? limit.status === 'active' : undefined,
  })
  return toRewardLimit(dto)
}

export async function deleteRewardLimit(id: string): Promise<{ success: boolean }> {
  if (getMockMode()) return mockApi.mockDeleteRewardLimit(id)
  // No backend DELETE route for reward limits (see #422); deactivate instead.
  void id
  throw new Error('Deleting reward limits is not supported by the backend; deactivate the limit instead.')
}

// ── Stats (no backend /store/stats; aggregate what is available) ─────────────

export async function getStoreStats(): Promise<StoreStats> {
  if (getMockMode()) return mockApi.mockGetStoreStats()
  const catalog = await apiGet<BackendPage<BackendStoreItemDto>>('/admin/store/catalog?page=1&pageSize=1')
  return {
    totalProducts: catalog.totalItems,
    activeFlashSales: 0,
    totalRevenue: 0,
    lowStockCount: 0,
  }
}

// ── Player stock (GET /admin/store/player-stock/{playerId}) ─────────────────

export async function getPlayerStock(playerId: string): Promise<PlayerStockResponse> {
  if (getMockMode()) return mockApi.mockGetPlayerStock(playerId)
  return apiGet(`/admin/store/player-stock/${playerId}`)
}

export async function overridePlayerStock(
  playerId: string,
  sku: string,
  effectiveMaxQuantity: number | null,
  reason?: string
): Promise<{ success: boolean }> {
  if (getMockMode()) return { success: true }
  await apiPost(`/admin/store/player-stock/${playerId}/override`, { sku, effectiveMaxQuantity, reason })
  return { success: true }
}

// ── Analytics (GET /admin/store/analytics/*) ────────────────────────────────

export async function getPurchaseAnalytics(from?: string, to?: string, sku?: string): Promise<PurchaseAnalytics> {
  if (getMockMode()) return mockApi.mockGetPurchaseAnalytics()
  const params = new URLSearchParams()
  if (from) params.set('from', from)
  if (to) params.set('to', to)
  if (sku) params.set('sku', sku)
  const qs = params.toString()
  return apiGet(`/admin/store/analytics/purchases${qs ? `?${qs}` : ''}`)
}

export async function getStockResetAnalytics(sku?: string, offset: number = 0, limit: number = 25): Promise<StockResetsResponse> {
  if (getMockMode()) return mockApi.mockGetStockResetAnalytics(offset, limit)
  const params = new URLSearchParams({
    page: String(Math.floor(offset / Math.max(1, limit)) + 1),
    pageSize: String(limit),
  })
  if (sku) params.set('sku', sku)
  const res = await apiGet<BackendPage<StockResetItem>>(`/admin/store/analytics/stock-resets?${params}`)
  return { items: res.items, total: res.totalItems, offset, limit }
}
