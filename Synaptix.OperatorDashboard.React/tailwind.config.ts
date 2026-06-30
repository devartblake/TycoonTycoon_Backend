import type { Config } from 'tailwindcss'
import defaultTheme from 'tailwindcss/defaultTheme'

export default {
  darkMode: ['class'],
  content: [
    './index.html',
    './src/**/*.{js,ts,jsx,tsx}',
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', ...defaultTheme.fontFamily.sans],
      },
      colors: {
        // Current design palette: teal accent, off-white background
        accent: '#0f766e',
        'accent-light': '#14b8a6',
        'accent-dark': '#0d5c53',
        'bg-primary': '#f7f6f3',
        'bg-secondary': '#f0ede8',
        'bg-tertiary': '#e8e4dd',
        'ink-primary': '#1f1f1f',
        'ink-secondary': '#5a5a5a',
        'ink-tertiary': '#8a8a8a',
        'panel-bg': '#fafaf8',
        'panel-border': '#e0dcd5',
        status: {
          healthy: '#22c55e',
          degraded: '#f59e0b',
          offline: '#ef4444',
          unknown: '#94a3b8',
        },
      },
      spacing: {
        'sidebar-w': '292px',
      },
      borderRadius: {
        lg: 'var(--radius)',
        md: 'calc(var(--radius) - 2px)',
        sm: 'calc(var(--radius) - 4px)',
      },
      keyframes: {
        'accordion-down': {
          from: { height: '0' },
          to: { height: 'var(--radix-accordion-content-height)' },
        },
        'accordion-up': {
          from: { height: 'var(--radix-accordion-content-height)' },
          to: { height: '0' },
        },
      },
      animation: {
        'accordion-down': 'accordion-down 0.2s ease-out',
        'accordion-up': 'accordion-up 0.2s ease-out',
      },
    },
  },
  plugins: [require('tailwindcss/plugin')(
    function ({ addBase, addComponents, addUtilities }) {
      addBase({
        ':root': {
          '--radius': '0.5rem',
        },
      })
    }
  )],
} satisfies Config
