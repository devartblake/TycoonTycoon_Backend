/**
 * Economy feature types
 */

export interface PlayerEconomy {
  playerId: string
  email: string
  handle: string
  currentBalance: number
  totalEarned: number
  totalSpent: number
  totalRefunded: number
  lastTransactionAt: string | null
  accountCreatedAt: string
}

export interface Transaction {
  id: string
  playerId: string
  type: 'purchase' | 'earn' | 'refund' | 'adjustment' | 'reward' | 'penalty'
  amount: number
  balanceBefore: number
  balanceAfter: number
  description: string
  reference?: string // order ID, achievement, etc.
  adminNote?: string
  status: 'completed' | 'pending' | 'failed' | 'reversed'
  createdAt: string
  completedAt?: string
}

export interface TransactionListResponse {
  items: Transaction[]
  total: number
  offset: number
  limit: number
}

export interface TransactionFilter {
  type?: Transaction['type']
  status?: Transaction['status']
  amountMin?: number
  amountMax?: number
  dateFrom?: string
  dateTo?: string
  searchText?: string
}

export interface BalanceAdjustment {
  playerId: string
  amount: number
  reason: string
  adminNote?: string
}

export interface EconomyStats {
  totalPlayers: number
  totalCurrency: number
  averageBalance: number
  largestBalance: number
  smallestBalance: number
}
