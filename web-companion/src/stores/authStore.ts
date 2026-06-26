/**
 * Authentication state store (Zustand)
 * Mirrors auth_providers.dart from Flutter
 */

import { create } from 'zustand';

export interface User {
  id: string;
  email: string;
  displayName: string;
  avatar?: string;
  role: 'user' | 'admin';
  createdAt: string;
}

export interface AuthState {
  user: User | null;
  isLoading: boolean;
  error: string | null;
  isAuthenticated: boolean;

  // Actions
  setUser: (user: User | null) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  logout: () => void;
  clearError: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isLoading: false,
  error: null,
  isAuthenticated: false,

  setUser: (user) => set({ user, isAuthenticated: !!user }),
  setLoading: (isLoading) => set({ isLoading }),
  setError: (error) => set({ error }),
  logout: () => {
    // Clear tokens from localStorage
    localStorage.removeItem('auth_token');
    localStorage.removeItem('refresh_token');
    // Clear auth state
    set({ user: null, isAuthenticated: false });
  },
  clearError: () => set({ error: null }),
}));

export default useAuthStore;
