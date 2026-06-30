/**
 * useUsers hook - manage user list with filters
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import * as api from '../api'
import type { User, UserFilters, UsersListResponse } from '../types'

export function useUsers(initialFilters?: UserFilters) {
  const [searchParams, setSearchParams] = useSearchParams()

  // Parse filters from URL params
  const filters: UserFilters = {
    email: searchParams.get('email') || initialFilters?.email,
    status: (searchParams.get('status') as User['status']) || initialFilters?.status,
    flagged: searchParams.get('flagged') === 'true' || initialFilters?.flagged,
    limit: parseInt(searchParams.get('limit') || '50'),
    offset: parseInt(searchParams.get('offset') || '0'),
  }

  // Fetch users with current filters
  const query = useQuery<UsersListResponse>({
    queryKey: ['users', filters],
    queryFn: () => api.getUsers(filters),
    staleTime: 1000 * 60, // 1 minute
  })

  // Apply filters (update URL params)
  const applyFilters = (newFilters: Partial<UserFilters>) => {
    const params = new URLSearchParams()
    const merged = { ...filters, ...newFilters, offset: 0 } // Reset to first page on filter change

    if (merged.email) params.set('email', merged.email)
    if (merged.status) params.set('status', merged.status)
    if (merged.flagged) params.set('flagged', 'true')
    params.set('limit', String(merged.limit))
    params.set('offset', '0')

    setSearchParams(params)
  }

  // Clear all filters
  const clearFilters = () => {
    setSearchParams({})
  }

  // Pagination
  const goToPage = (offset: number) => {
    const params = new URLSearchParams(searchParams)
    params.set('offset', String(offset))
    setSearchParams(params)
  }

  return {
    users: query.data?.items || [],
    total: query.data?.total || 0,
    filters,
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
    applyFilters,
    clearFilters,
    goToPage,
  }
}

export function useBanUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (userId: string) => api.banUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
    },
  })
}

export function useUnbanUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (userId: string) => api.unbanUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
    },
  })
}

export function useSuspendUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ userId, reason }: { userId: string; reason?: string }) => api.suspendUser(userId, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
    },
  })
}

export function useUnsuspendUser() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (userId: string) => api.unsuspendUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
    },
  })
}

export function useBulkBanUsers() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ userIds, reason }: { userIds: string[]; reason?: string }) => api.bulkBanUsers(userIds, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
    },
  })
}

export function useBulkUnbanUsers() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (userIds: string[]) => api.bulkUnbanUsers(userIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
    },
  })
}
