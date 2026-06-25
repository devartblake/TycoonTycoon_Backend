/**
 * Custom hook for showing toast notifications
 */

import { useNotificationStore, type NotificationType } from '@stores/notificationStore';

export function useToast() {
  const add = useNotificationStore((state) => state.add);

  return {
    success: (message: string, duration?: number) => add(message, 'success', duration),
    error: (message: string, duration?: number) => add(message, 'error', duration),
    info: (message: string, duration?: number) => add(message, 'info', duration),
    warning: (message: string, duration?: number) => add(message, 'warning', duration),
    show: (message: string, type: NotificationType, duration?: number) => add(message, type, duration),
  };
}

export default useToast;
