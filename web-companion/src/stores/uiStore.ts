/**
 * Global UI state store (Zustand)
 * Manages theme, modals, sidebar, notifications
 */

import { create } from 'zustand';

export type Theme = 'light' | 'dark' | 'auto';
export type SynaptixMode = 'kids' | 'teens' | 'adults';

export interface UIState {
  theme: Theme;
  synaptixMode: SynaptixMode;
  sidebarOpen: boolean;
  modals: {
    [key: string]: boolean;
  };
  notifications: Array<{
    id: string;
    message: string;
    type: 'success' | 'error' | 'info' | 'warning';
    duration?: number;
  }>;

  // Actions
  setTheme: (theme: Theme) => void;
  setSynaptixMode: (mode: SynaptixMode) => void;
  toggleSidebar: () => void;
  setSidebarOpen: (open: boolean) => void;
  openModal: (modalId: string) => void;
  closeModal: (modalId: string) => void;
  toggleModal: (modalId: string) => void;
  addNotification: (
    message: string,
    type: 'success' | 'error' | 'info' | 'warning',
    duration?: number
  ) => string; // Returns notification ID
  removeNotification: (id: string) => void;
  clearNotifications: () => void;
}

export const useUIStore = create<UIState>((set) => ({
  theme: 'dark',
  synaptixMode: 'adults',
  sidebarOpen: true,
  modals: {},
  notifications: [],

  setTheme: (theme) => set({ theme }),

  setSynaptixMode: (synaptixMode) => set({ synaptixMode }),

  toggleSidebar: () =>
    set((state) => ({ sidebarOpen: !state.sidebarOpen })),

  setSidebarOpen: (sidebarOpen) => set({ sidebarOpen }),

  openModal: (modalId) =>
    set((state) => ({
      modals: { ...state.modals, [modalId]: true },
    })),

  closeModal: (modalId) =>
    set((state) => ({
      modals: { ...state.modals, [modalId]: false },
    })),

  toggleModal: (modalId) =>
    set((state) => ({
      modals: { ...state.modals, [modalId]: !state.modals[modalId] },
    })),

  addNotification: (message, type, duration = 3000) => {
    const id = `notif_${Date.now()}_${Math.random()}`;
    set((state) => ({
      notifications: [
        ...state.notifications,
        { id, message, type, duration },
      ],
    }));

    // Auto-remove after duration
    if (duration > 0) {
      setTimeout(() => {
        useUIStore.setState((state) => ({
          notifications: state.notifications.filter((n) => n.id !== id),
        }));
      }, duration);
    }

    return id;
  },

  removeNotification: (id) =>
    set((state) => ({
      notifications: state.notifications.filter((n) => n.id !== id),
    })),

  clearNotifications: () => set({ notifications: [] }),
}));

export default useUIStore;
