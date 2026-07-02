/**
 * useDashboard hook - fetch dashboard stats and service history
 */
import { useQuery } from '@tanstack/react-query';
import * as api from '../api';
export function useDashboardStats() {
    return useQuery({
        queryKey: ['dashboardStats'],
        queryFn: () => api.getDashboardStats(),
        staleTime: 1000 * 15, // 15 seconds - auto-refresh feels responsive
        refetchInterval: 1000 * 30, // Refetch every 30 seconds for live updates
    });
}
export function useServiceHistory(serviceId, hours = 24) {
    return useQuery({
        queryKey: ['serviceHistory', serviceId, hours],
        queryFn: () => api.getServiceHistory(serviceId, hours),
        staleTime: 1000 * 60, // 1 minute
    });
}
export function useAllServiceHistory(hours = 24) {
    return useQuery({
        queryKey: ['allServiceHistory', hours],
        queryFn: () => api.getAllServiceHistory(hours),
        staleTime: 1000 * 60, // 1 minute
    });
}
