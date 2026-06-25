/**
 * Theme provider component - initializes and applies theme system
 */

import type { ReactNode } from 'react';
import { useEffect } from 'react';
import { useTheme } from '@hooks/useTheme';
import { getSavedTheme, getTheme, applyTheme } from '@theme/themes';

interface ThemeProviderProps {
  children: ReactNode;
}

export function ThemeProvider({ children }: ThemeProviderProps) {
  // Initialize theme on mount
  useEffect(() => {
    const savedTheme = getSavedTheme();
    const theme = getTheme(savedTheme.mode, savedTheme.variant);
    applyTheme(theme);
  }, []);

  // Use theme hook to keep theme in sync
  useTheme();

  return <>{children}</>;
}

export default ThemeProvider;
