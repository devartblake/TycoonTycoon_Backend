/**
 * useNotifications hook - manage notification resources
 */
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as api from '../api';
export function useNotificationTemplates() {
    return useQuery({
        queryKey: ['notificationTemplates'],
        queryFn: () => api.getTemplates(),
        staleTime: 1000 * 60, // 1 minute
    });
}
export function useCreateTemplate() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (payload) => api.createTemplate(payload),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['notificationTemplates'] });
        },
    });
}
export function useUpdateTemplate() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ templateId, payload }) => api.updateTemplate(templateId, payload),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['notificationTemplates'] });
        },
    });
}
export function useDeleteTemplate() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (templateId) => api.deleteTemplate(templateId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['notificationTemplates'] });
        },
    });
}
export function useNotificationChannels() {
    return useQuery({
        queryKey: ['notificationChannels'],
        queryFn: () => api.getChannels(),
        staleTime: 1000 * 60 * 5, // 5 minutes
    });
}
export function useUpdateChannel() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ channelId, enabled, config, }) => api.updateChannel(channelId, enabled, config),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['notificationChannels'] });
        },
    });
}
export function useScheduledNotifications() {
    return useQuery({
        queryKey: ['scheduledNotifications'],
        queryFn: () => api.getSchedules(),
        staleTime: 1000 * 60, // 1 minute
    });
}
export function useCreateSchedule() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (payload) => api.createSchedule(payload),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['scheduledNotifications'] });
        },
    });
}
export function useCancelSchedule() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (scheduleId) => api.cancelSchedule(scheduleId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['scheduledNotifications'] });
        },
    });
}
export function useDeadLetterMessages() {
    return useQuery({
        queryKey: ['deadLetterMessages'],
        queryFn: () => api.getDeadLetterMessages(),
        staleTime: 1000 * 60, // 1 minute
    });
}
export function useRetryDeadLetter() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (messageId) => api.retryDeadLetterMessage(messageId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['deadLetterMessages'] });
        },
    });
}
export function useSendTestNotification() {
    return useMutation({
        mutationFn: (payload) => api.sendTestNotification(payload),
    });
}
