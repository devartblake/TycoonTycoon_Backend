/**
 * Player profile state store (Zustand)
 * Mirrors profile_providers.dart from Flutter
 */

import { create } from 'zustand';

export interface PlayerProfile {
  playerId: string;
  username: string;
  level: number;
  xp: number;
  xpForNextLevel: number;
  coins: number;
  diamonds: number;
  tier: 'bronze' | 'silver' | 'gold' | 'platinum' | 'diamond';
  rank: number;
  streak: number;
  totalQuizzesSolved: number;
  accuracy: number;
  unlockedSkills: string[];
  activeSkills: string[];
  achievements: string[];
  createdAt: string;
  lastPlayedAt: string;
}

export interface ProfileState {
  profile: PlayerProfile | null;
  isLoading: boolean;
  error: string | null;

  // Actions
  setProfile: (profile: PlayerProfile) => void;
  addXP: (amount: number) => void;
  addCoins: (amount: number) => void;
  addDiamonds: (amount: number) => void;
  unlockSkill: (skillId: string) => void;
  setActiveSkills: (skillIds: string[]) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  clearError: () => void;
}

export const useProfileStore = create<ProfileState>((set) => ({
  profile: null,
  isLoading: false,
  error: null,

  setProfile: (profile) => set({ profile }),

  addXP: (amount) =>
    set((state) => {
      if (!state.profile) return state;
      return {
        profile: {
          ...state.profile,
          xp: state.profile.xp + amount,
        },
      };
    }),

  addCoins: (amount) =>
    set((state) => {
      if (!state.profile) return state;
      return {
        profile: {
          ...state.profile,
          coins: state.profile.coins + amount,
        },
      };
    }),

  addDiamonds: (amount) =>
    set((state) => {
      if (!state.profile) return state;
      return {
        profile: {
          ...state.profile,
          diamonds: state.profile.diamonds + amount,
        },
      };
    }),

  unlockSkill: (skillId) =>
    set((state) => {
      if (!state.profile) return state;
      return {
        profile: {
          ...state.profile,
          unlockedSkills: [...new Set([...state.profile.unlockedSkills, skillId])],
        },
      };
    }),

  setActiveSkills: (skillIds) =>
    set((state) => {
      if (!state.profile) return state;
      return {
        profile: {
          ...state.profile,
          activeSkills: skillIds,
        },
      };
    }),

  setLoading: (isLoading) => set({ isLoading }),
  setError: (error) => set({ error }),
  clearError: () => set({ error: null }),
}));

export default useProfileStore;
