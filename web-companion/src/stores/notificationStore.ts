/**
 * Notification store for toast messages
 */

import { create } from 'zustand';

export type NotificationType = 'success' | 'error' | 'info' | 'warning';

export interface Notification {
  id: string;
  message: string;
  type: NotificationType;
  duration?: number; // ms, 0 = no auto-dismiss
}

export interface NotificationState {
  notifications: Notification[];
  add: (message: string, type: NotificationType, duration?: number) => string;
  remove: (id: string) => void;
  clear: () => void;
}

const generateId = () => `notif_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

export const useNotificationStore = create<NotificationState>((set) => ({
  notifications: [],

  add: (message, type, duration = 3000) => {
    const id = generateId();

    set((state) => ({
      notifications: [...state.notifications, { id, message, type, duration }],
    }));

    // Auto-dismiss after duration
    if (duration > 0) {
      setTimeout(() => {
        set((state) => ({
          notifications: state.notifications.filter((n) => n.id !== id),
        }));
      }, duration);
    }

    return id;
  },

  remove: (id) =>
    set((state) => ({
      notifications: state.notifications.filter((n) => n.id !== id),
    })),

  clear: () => set({ notifications: [] }),
}));

export default useNotificationStore;
