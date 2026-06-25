/**
 * Theme system for Trivia Tycoon Web Companion
 * Supports Synaptix modes (Kids, Teens, Adults) × Light/Dark variants
 * Mirrors Flutter app theming system
 */

export type SynaptixMode = 'kids' | 'teens' | 'adults';
export type ThemeVariant = 'light' | 'dark';

export interface ThemeColors {
  // Background colors
  bg: {
    primary: string;      // Main background
    secondary: string;    // Card/panel background
    tertiary: string;     // Hover/focus background
    elevated: string;     // Modal/dialog background
  };
  // Text colors
  text: {
    primary: string;      // Main text
    secondary: string;    // Dimmed text
    tertiary: string;     // Faint text
    inverse: string;      // Inverse contrast
  };
  // Brand colors
  brand: {
    primary: string;      // Primary action color
    secondary: string;    // Secondary action
    accent: string;       // Accent highlight
  };
  // Status colors
  status: {
    success: string;
    warning: string;
    error: string;
    info: string;
  };
  // UI elements
  ui: {
    border: string;
    divider: string;
    overlay: string;      // Semi-transparent overlay
  };
}

export interface Theme {
  name: string;
  mode: SynaptixMode;
  variant: ThemeVariant;
  colors: ThemeColors;
}

// KIDS THEME - Bright, playful, approachable
const kidsLightColors: ThemeColors = {
  bg: {
    primary: '#FFFFFF',
    secondary: '#F8F9FA',
    tertiary: '#E9ECEF',
    elevated: '#FFFFFF',
  },
  text: {
    primary: '#212529',
    secondary: '#6C757D',
    tertiary: '#ADB5BD',
    inverse: '#FFFFFF',
  },
  brand: {
    primary: '#FF6B6B', // Bright red
    secondary: '#4ECDC4', // Teal
    accent: '#FFE66D', // Bright yellow
  },
  status: {
    success: '#51CF66',
    warning: '#FFA94D',
    error: '#FF8787',
    info: '#74C0FC',
  },
  ui: {
    border: '#DEE2E6',
    divider: '#E9ECEF',
    overlay: 'rgba(0, 0, 0, 0.15)',
  },
};

const kidsDarkColors: ThemeColors = {
  bg: {
    primary: '#1A1A2E',
    secondary: '#16213E',
    tertiary: '#0F3460',
    elevated: '#1A1A2E',
  },
  text: {
    primary: '#EAEAEA',
    secondary: '#B0B0B0',
    tertiary: '#808080',
    inverse: '#1A1A2E',
  },
  brand: {
    primary: '#FF6B6B',
    secondary: '#4ECDC4',
    accent: '#FFE66D',
  },
  status: {
    success: '#69DB7C',
    warning: '#FFA94D',
    error: '#FF8787',
    info: '#74C0FC',
  },
  ui: {
    border: '#2D3748',
    divider: '#2D3748',
    overlay: 'rgba(0, 0, 0, 0.4)',
  },
};

// TEENS THEME - Modern, balanced, energetic
const teensLightColors: ThemeColors = {
  bg: {
    primary: '#FAFBFC',
    secondary: '#F0F2F5',
    tertiary: '#E4E6EB',
    elevated: '#FFFFFF',
  },
  text: {
    primary: '#050505',
    secondary: '#65676B',
    tertiary: '#A0A2A4',
    inverse: '#FFFFFF',
  },
  brand: {
    primary: '#7C3AED', // Purple
    secondary: '#06B6D4', // Cyan
    accent: '#EC4899', // Pink
  },
  status: {
    success: '#10B981',
    warning: '#F59E0B',
    error: '#EF4444',
    info: '#3B82F6',
  },
  ui: {
    border: '#D1D5DB',
    divider: '#E5E7EB',
    overlay: 'rgba(0, 0, 0, 0.1)',
  },
};

const teensDarkColors: ThemeColors = {
  bg: {
    primary: '#0F172A',
    secondary: '#1E293B',
    tertiary: '#334155',
    elevated: '#1E293B',
  },
  text: {
    primary: '#F1F5F9',
    secondary: '#CBD5E1',
    tertiary: '#94A3B8',
    inverse: '#0F172A',
  },
  brand: {
    primary: '#7C3AED',
    secondary: '#06B6D4',
    accent: '#EC4899',
  },
  status: {
    success: '#10B981',
    warning: '#F59E0B',
    error: '#EF4444',
    info: '#3B82F6',
  },
  ui: {
    border: '#334155',
    divider: '#334155',
    overlay: 'rgba(0, 0, 0, 0.3)',
  },
};

// ADULTS THEME - Professional, dark, sophisticated
const adultsLightColors: ThemeColors = {
  bg: {
    primary: '#FFFFFF',
    secondary: '#F7F7F7',
    tertiary: '#EFEFEF',
    elevated: '#FFFFFF',
  },
  text: {
    primary: '#1F2937',
    secondary: '#6B7280',
    tertiary: '#9CA3AF',
    inverse: '#FFFFFF',
  },
  brand: {
    primary: '#6366F1', // Indigo
    secondary: '#8B5CF6', // Violet
    accent: '#F59E0B', // Amber
  },
  status: {
    success: '#059669',
    warning: '#D97706',
    error: '#DC2626',
    info: '#2563EB',
  },
  ui: {
    border: '#E5E7EB',
    divider: '#F3F4F6',
    overlay: 'rgba(0, 0, 0, 0.08)',
  },
};

const adultsDarkColors: ThemeColors = {
  bg: {
    primary: '#111827',
    secondary: '#1F2937',
    tertiary: '#374151',
    elevated: '#1F2937',
  },
  text: {
    primary: '#F3F4F6',
    secondary: '#D1D5DB',
    tertiary: '#9CA3AF',
    inverse: '#111827',
  },
  brand: {
    primary: '#6366F1',
    secondary: '#8B5CF6',
    accent: '#F59E0B',
  },
  status: {
    success: '#10B981',
    warning: '#F59E0B',
    error: '#EF4444',
    info: '#3B82F6',
  },
  ui: {
    border: '#374151',
    divider: '#374151',
    overlay: 'rgba(0, 0, 0, 0.4)',
  },
};

// Theme definitions
export const THEMES: Record<string, Theme> = {
  'kids-light': {
    name: 'Kids Light',
    mode: 'kids',
    variant: 'light',
    colors: kidsLightColors,
  },
  'kids-dark': {
    name: 'Kids Dark',
    mode: 'kids',
    variant: 'dark',
    colors: kidsDarkColors,
  },
  'teens-light': {
    name: 'Teens Light',
    mode: 'teens',
    variant: 'light',
    colors: teensLightColors,
  },
  'teens-dark': {
    name: 'Teens Dark',
    mode: 'teens',
    variant: 'dark',
    colors: teensDarkColors,
  },
  'adults-light': {
    name: 'Adults Light',
    mode: 'adults',
    variant: 'light',
    colors: adultsLightColors,
  },
  'adults-dark': {
    name: 'Adults Dark',
    mode: 'adults',
    variant: 'dark',
    colors: adultsDarkColors,
  },
};

// Get theme by mode and variant
export function getTheme(mode: SynaptixMode, variant: ThemeVariant): Theme {
  const key = `${mode}-${variant}`;
  return THEMES[key] || THEMES['adults-dark'];
}

// CSS variable generator
export function generateThemeVariables(theme: Theme): Record<string, string> {
  const { colors } = theme;
  return {
    // Background
    '--color-bg-primary': colors.bg.primary,
    '--color-bg-secondary': colors.bg.secondary,
    '--color-bg-tertiary': colors.bg.tertiary,
    '--color-bg-elevated': colors.bg.elevated,
    // Text
    '--color-text-primary': colors.text.primary,
    '--color-text-secondary': colors.text.secondary,
    '--color-text-tertiary': colors.text.tertiary,
    '--color-text-inverse': colors.text.inverse,
    // Brand
    '--color-brand-primary': colors.brand.primary,
    '--color-brand-secondary': colors.brand.secondary,
    '--color-brand-accent': colors.brand.accent,
    // Status
    '--color-status-success': colors.status.success,
    '--color-status-warning': colors.status.warning,
    '--color-status-error': colors.status.error,
    '--color-status-info': colors.status.info,
    // UI
    '--color-ui-border': colors.ui.border,
    '--color-ui-divider': colors.ui.divider,
    '--color-ui-overlay': colors.ui.overlay,
  };
}

// Apply theme to document
export function applyTheme(theme: Theme): void {
  const variables = generateThemeVariables(theme);
  const root = document.documentElement;

  Object.entries(variables).forEach(([key, value]) => {
    root.style.setProperty(key, value);
  });

  // Store current theme in localStorage
  localStorage.setItem('theme-mode', theme.mode);
  localStorage.setItem('theme-variant', theme.variant);
}

// Get saved theme preference
export function getSavedTheme(): { mode: SynaptixMode; variant: ThemeVariant } {
  const mode = (localStorage.getItem('theme-mode') || 'adults') as SynaptixMode;
  const variant = (localStorage.getItem('theme-variant') || 'dark') as ThemeVariant;
  return { mode, variant };
}
