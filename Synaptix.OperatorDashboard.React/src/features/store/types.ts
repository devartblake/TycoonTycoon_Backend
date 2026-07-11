/**
 * Store feature types
 */

export interface Product {
  id: string
  name: string
  description: string
  price: number
  category: string
  rarity: 'common' | 'uncommon' | 'rare' | 'epic' | 'legendary'
  stock: number
  maxStock: number
  active: boolean
  createdAt: string
  updatedAt: string
}

export interface FlashSale {
  id: string
  name: string
  productId: string
  productName: string
  discountPercentage: number
  originalPrice: number
  salePrice: number
  startTime: string
  endTime: string
  maxUnits: number
  unitsSold: number
  status: 'scheduled' | 'active' | 'ended'
  createdAt: string
}

export interface StockPolicy {
  id: string
  name: string
  description: string
  reorderLevel: number
  reorderQuantity: number
  maxStockLevel: number
  autoReorder: boolean
  createdAt: string
  updatedAt: string
}

export interface RewardLimit {
  id: string
  name: string
  type: 'daily' | 'weekly' | 'seasonal'
  maxAmount: number
  currentAmount: number
  resetDate?: string
  status: 'active' | 'paused'
  createdAt: string
}

export interface StoreStats {
  totalProducts: number
  activeFlashSales: number
  totalRevenue: number
  lowStockCount: number
}

export interface ProductsListResponse {
  items: Product[]
  total: number
  offset: number
  limit: number
}

export interface FlashSalesListResponse {
  items: FlashSale[]
  total: number
  offset: number
  limit: number
}

export interface StockPoliciesListResponse {
  items: StockPolicy[]
  total: number
  offset: number
  limit: number
}

export interface RewardLimitsListResponse {
  items: RewardLimit[]
  total: number
  offset: number
  limit: number
}

// GET /admin/store/player-stock/{playerId} (AdminPlayerStockStateDto)
export interface PlayerStockItem {
  sku: string
  quantityUsed: number
  maxQuantity: number
  remaining: number
  effectiveMaxQuantity: number | null
  lastResetAtUtc: string | null
  nextResetAtUtc: string | null
  updatedAtUtc: string
}

export interface PlayerStockResponse {
  playerId: string
  items: PlayerStockItem[]
}

// GET /admin/store/analytics/purchases
export interface PurchaseAnalytics {
  from: string | null
  to: string | null
  totalPurchases: number
  totalCoinsSpent: number
  topSkus: Array<{ sku: string; purchaseCount: number }>
}

// GET /admin/store/analytics/stock-resets (paged)
export interface StockResetItem {
  playerId: string
  sku: string
  lastResetAt: string | null
  nextResetAt: string | null
  quantityUsed: number
}

export interface StockResetsResponse {
  items: StockResetItem[]
  total: number
  offset: number
  limit: number
}
