/**
 * useSavedViews hook - manage saved filter views
 */
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as api from '../api';
export function useSavedViews() {
    return useQuery({
        queryKey: ['savedViews'],
        queryFn: () => api.getSavedViews(),
        staleTime: 1000 * 60 * 5, // 5 minutes
    });
}
export function useCreateSavedView() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ name, filters }) => api.createSavedView(name, filters),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['savedViews'] });
        },
    });
}
export function useDeleteSavedView() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (viewId) => api.deleteSavedView(viewId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['savedViews'] });
        },
    });
}
