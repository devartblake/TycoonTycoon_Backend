/**
 * useUsers hook - manage user list with filters
 */
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import * as api from '../api';
export function useUsers(initialFilters) {
    const [searchParams, setSearchParams] = useSearchParams();
    // Parse filters from URL params
    const filters = {
        email: searchParams.get('email') || initialFilters?.email,
        status: searchParams.get('status') || initialFilters?.status,
        flagged: searchParams.get('flagged') === 'true' || initialFilters?.flagged,
        limit: parseInt(searchParams.get('limit') || '50'),
        offset: parseInt(searchParams.get('offset') || '0'),
    };
    // Fetch users with current filters
    const query = useQuery({
        queryKey: ['users', filters],
        queryFn: () => api.getUsers(filters),
        staleTime: 1000 * 60, // 1 minute
    });
    // Apply filters (update URL params)
    const applyFilters = (newFilters) => {
        const params = new URLSearchParams();
        const merged = { ...filters, ...newFilters, offset: 0 }; // Reset to first page on filter change
        if (merged.email)
            params.set('email', merged.email);
        if (merged.status)
            params.set('status', merged.status);
        if (merged.flagged)
            params.set('flagged', 'true');
        params.set('limit', String(merged.limit));
        params.set('offset', '0');
        setSearchParams(params);
    };
    // Clear all filters
    const clearFilters = () => {
        setSearchParams({});
    };
    // Pagination
    const goToPage = (offset) => {
        const params = new URLSearchParams(searchParams);
        params.set('offset', String(offset));
        setSearchParams(params);
    };
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
    };
}
export function useBanUser() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (userId) => api.banUser(userId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['users'] });
        },
    });
}
export function useUnbanUser() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (userId) => api.unbanUser(userId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['users'] });
        },
    });
}
export function useSuspendUser() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ userId, reason }) => api.suspendUser(userId, reason),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['users'] });
        },
    });
}
export function useUnsuspendUser() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (userId) => api.unsuspendUser(userId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['users'] });
        },
    });
}
export function useBulkBanUsers() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ userIds, reason }) => api.bulkBanUsers(userIds, reason),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['users'] });
        },
    });
}
export function useBulkUnbanUsers() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (userIds) => api.bulkUnbanUsers(userIds),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['users'] });
        },
    });
}
