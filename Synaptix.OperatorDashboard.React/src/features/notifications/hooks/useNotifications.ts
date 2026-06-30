/**
 * useNotifications hook - manage notification resources
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import * as api from '../api'
import type {
  NotificationTemplate,
  NotificationChannel,
  ScheduledNotification,
  DeadLetterMessage,
  TestSendPayload,
  CreateTemplatePayload,
} from '../types'

export function useNotificationTemplates() {
  return useQuery<NotificationTemplate[]>({
    queryKey: ['notificationTemplates'],
    queryFn: () => api.getTemplates(),
    staleTime: 1000 * 60, // 1 minute
  })
}

export function useCreateTemplate() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateTemplatePayload) => api.createTemplate(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationTemplates'] })
    },
  })
}

export function useUpdateTemplate() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ templateId, payload }: { templateId: string; payload: Partial<CreateTemplatePayload> }) =>
      api.updateTemplate(templateId, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationTemplates'] })
    },
  })
}

export function useDeleteTemplate() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (templateId: string) => api.deleteTemplate(templateId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationTemplates'] })
    },
  })
}

export function useNotificationChannels() {
  return useQuery<NotificationChannel[]>({
    queryKey: ['notificationChannels'],
    queryFn: () => api.getChannels(),
    staleTime: 1000 * 60 * 5, // 5 minutes
  })
}

export function useUpdateChannel() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      channelId,
      enabled,
      config,
    }: {
      channelId: string
      enabled: boolean
      config?: Record<string, unknown>
    }) => api.updateChannel(channelId, enabled, config),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationChannels'] })
    },
  })
}

export function useScheduledNotifications() {
  return useQuery<ScheduledNotification[]>({
    queryKey: ['scheduledNotifications'],
    queryFn: () => api.getSchedules(),
    staleTime: 1000 * 60, // 1 minute
  })
}

export function useCreateSchedule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: { templateId: string; scheduledFor: string; targetFilter?: Record<string, unknown> }) =>
      api.createSchedule(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['scheduledNotifications'] })
    },
  })
}

export function useCancelSchedule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (scheduleId: string) => api.cancelSchedule(scheduleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['scheduledNotifications'] })
    },
  })
}

export function useDeadLetterMessages() {
  return useQuery<DeadLetterMessage[]>({
    queryKey: ['deadLetterMessages'],
    queryFn: () => api.getDeadLetterMessages(),
    staleTime: 1000 * 60, // 1 minute
  })
}

export function useRetryDeadLetter() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (messageId: string) => api.retryDeadLetterMessage(messageId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['deadLetterMessages'] })
    },
  })
}

export function useSendTestNotification() {
  return useMutation({
    mutationFn: (payload: TestSendPayload) => api.sendTestNotification(payload),
  })
}
