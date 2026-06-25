# Theming System Documentation

The web companion features a comprehensive theming system similar to the Flutter app, with support for multiple user modes (Kids, Teens, Adults) and light/dark variants.

## Overview

The theming system consists of:

1. **Theme Definitions** (`src/theme/themes.ts`)
   - 6 theme combinations: 3 Synaptix modes × 2 light/dark variants
   - Each theme has a complete color palette
   - CSS variable generation for global styling

2. **Theme Hook** (`src/hooks/useTheme.ts`)
   - Easy access to current theme state
   - Functions to change theme mode and variant
   - Automatic persistence to localStorage

3. **Theme Provider** (`src/components/layout/ThemeProvider.tsx`)
   - Initializes theme on app startup
   - Applies theme to DOM via CSS variables

4. **CSS Variables** (`src/index.css`)
   - All colors use CSS variables
   - Smooth transitions between themes
   - Base components styled with variables

## Synaptix Modes

### Kids Mode 👧
- **Light**: Bright, playful colors (red, teal, yellow)
- **Dark**: Playful colors on dark background
- **Use case**: Younger players (6-12 years old)

### Teens Mode 👦  
- **Light**: Modern, energetic (purple, cyan, pink)
- **Dark**: Contemporary dark theme
- **Use case**: Teenagers (13-17 years old)

### Adults Mode 👔
- **Light**: Professional, clean (indigo, amber)
- **Dark**: Sophisticated dark theme
- **Use case**: Adults (18+ years old)

## Color Palette Structure

Each theme defines colors in these categories:

```typescript
colors: {
  bg: { primary, secondary, tertiary, elevated },        // Backgrounds
  text: { primary, secondary, tertiary, inverse },       // Text colors
  brand: { primary, secondary, accent },                 // Brand colors
  status: { success, warning, error, info },            // Status indicators
  ui: { border, divider, overlay }                      // UI elements
}
```

## Using the Theme System

### Apply Theme in Component

```tsx
import { useTheme } from '@hooks/useTheme';

export function MyComponent() {
  const { synaptixMode, themeVariant } = useTheme();

  return (
    <div style={{ 
      backgroundColor: 'var(--color-bg-primary)',
      color: 'var(--color-text-primary)'
    }}>
      Current mode: {synaptixMode} ({themeVariant})
    </div>
  );
}
```

### Change Theme

```tsx
import { useTheme } from '@hooks/useTheme';

export function ThemeSwitcher() {
  const { setSynaptixMode, setThemeVariant, toggleThemeVariant } = useTheme();

  return (
    <>
      <button onClick={() => setSynaptixMode('kids')}>Kids Mode</button>
      <button onClick={() => toggleThemeVariant()}>Toggle Light/Dark</button>
    </>
  );
}
```

### CSS Variables Available

All themes use these CSS variables (set automatically):

```css
/* Background colors */
--color-bg-primary      /* Main background */
--color-bg-secondary    /* Card/panel background */
--color-bg-tertiary     /* Hover/focus background */
--color-bg-elevated     /* Modal/dialog background */

/* Text colors */
--color-text-primary    /* Main text */
--color-text-secondary  /* Dimmed text */
--color-text-tertiary   /* Faint text */
--color-text-inverse    /* Inverse contrast */

/* Brand colors */
--color-brand-primary   /* Primary action */
--color-brand-secondary /* Secondary action */
--color-brand-accent    /* Accent highlight */

/* Status colors */
--color-status-success
--color-status-warning
--color-status-error
--color-status-info

/* UI colors */
--color-ui-border       /* Borders */
--color-ui-divider      /* Dividers */
--color-ui-overlay      /* Semi-transparent overlay */
```

## Implementation Details

### Storage
- Current theme mode: `localStorage['theme-mode']`
- Current variant: `localStorage['theme-variant']`
- Persisted automatically on change

### Performance
- CSS variables for instant theme switching
- Smooth transitions via CSS (300ms duration)
- No component re-renders needed for theme changes
- All styling uses CSS variables, not hardcoded colors

### Accessibility
- High contrast ratios maintained across all themes
- Status colors meet WCAG AA standards
- Prefers-reduced-motion support built in

## Theme Settings Page

The Settings page (`src/features/dashboard/pages/SettingsPage.tsx`) allows users to:

1. **Toggle Light/Dark Mode**
   - Click Sun/Moon buttons
   - Instant theme switch

2. **Select Synaptix Mode**
   - 3 card buttons for Kids/Teens/Adults
   - Descriptions for each mode
   - Visual feedback (highlighted when selected)

3. **Preview Changes**
   - UI updates instantly
   - Settings persist on page reload

## Adding New Colors

To add a new color to all themes:

1. Add property to `ThemeColors` interface:
```typescript
export interface ThemeColors {
  // ... existing colors
  custom: {
    myColor: string;
  };
}
```

2. Add value to each of the 6 theme color definitions:
```typescript
const kidsLightColors: ThemeColors = {
  // ... existing
  custom: { myColor: '#FF6B6B' },
};
```

3. Generate CSS variable in `generateThemeVariables()`:
```typescript
'--color-custom-my-color': colors.custom.myColor,
```

4. Use in components:
```tsx
<div style={{ backgroundColor: 'var(--color-custom-my-color)' }}>
```

## Testing Themes

### Manual Testing
1. Open Settings page
2. Toggle between light/dark
3. Select each Synaptix mode
4. Verify colors change smoothly
5. Reload page - settings should persist

### Cross-Browser Testing
- Chrome/Edge: Full support
- Firefox: Full support
- Safari: Full support
- Mobile browsers: Full support

## Future Enhancements

Potential improvements for Phase 2+:

- [ ] Custom theme builder (user-defined colors)
- [ ] Preset theme library (high contrast, colorblind-friendly)
- [ ] Time-based theme switching (dark mode at night)
- [ ] Per-game theme (different themes for different activities)
- [ ] Theme preview carousel
- [ ] Export/import theme settings

## Troubleshooting

### Theme not applying?
1. Check `ThemeProvider` is in component tree
2. Verify CSS variables in DevTools: `--color-*`
3. Clear localStorage and reload

### Colors look wrong?
1. Check theme is loading from `localStorage`
2. Verify CSS file is loaded
3. Check for CSS conflicts with Tailwind

### Performance issues?
1. Reduce number of re-renders
2. Use CSS variables instead of JS styling
3. Batch theme changes

## Related Files

- `src/theme/themes.ts` - Theme definitions
- `src/hooks/useTheme.ts` - Theme hook
- `src/components/layout/ThemeProvider.tsx` - Provider component
- `src/stores/uiStore.ts` - Zustand state
- `src/features/dashboard/pages/SettingsPage.tsx` - Settings UI
- `src/index.css` - Global styles with CSS variables
