/**
 * Global UI state store (Zustand)
 * Manages theme, modals, sidebar, notifications
 */

import { create } from 'zustand';
import type { SynaptixMode } from '@theme/themes';

export type ThemeVariant = 'light' | 'dark';

export interface UIState {
  synaptixMode: SynaptixMode;
  themeVariant: ThemeVariant;
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
  setSynaptixMode: (mode: SynaptixMode) => void;
  setThemeVariant: (variant: ThemeVariant) => void;
  toggleThemeVariant: () => void;
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
  synaptixMode: 'adults',
  themeVariant: 'dark',
  sidebarOpen: true,
  modals: {},
  notifications: [],

  setSynaptixMode: (synaptixMode) => set({ synaptixMode }),

  setThemeVariant: (themeVariant) => set({ themeVariant }),

  toggleThemeVariant: () =>
    set((state) => ({
      themeVariant: state.themeVariant === 'dark' ? 'light' : 'dark',
    })),

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
