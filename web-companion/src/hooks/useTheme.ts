/**
 * Hook to manage theme
 */

import { useEffect } from 'react';
import { useUIStore } from '@stores';
import { getTheme, applyTheme, getSavedTheme } from '@theme/themes';

export function useTheme() {
  const synaptixMode = useUIStore((state) => state.synaptixMode);
  const themeVariant = useUIStore((state) => state.themeVariant);
  const setSynaptixMode = useUIStore((state) => state.setSynaptixMode);
  const setThemeVariant = useUIStore((state) => state.setThemeVariant);
  const toggleThemeVariant = useUIStore((state) => state.toggleThemeVariant);

  // Apply theme when mode or variant changes
  useEffect(() => {
    const theme = getTheme(synaptixMode, themeVariant);
    applyTheme(theme);
  }, [synaptixMode, themeVariant]);

  // Initialize theme on mount from localStorage
  useEffect(() => {
    const savedTheme = getSavedTheme();
    setSynaptixMode(savedTheme.mode);
    setThemeVariant(savedTheme.variant);
  }, [setSynaptixMode, setThemeVariant]);

  return {
    synaptixMode,
    themeVariant,
    setSynaptixMode,
    setThemeVariant,
    toggleThemeVariant,
  };
}

export default useTheme;
