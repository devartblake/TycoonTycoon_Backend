import { jsx as _jsx } from "react/jsx-runtime";
import { createContext, useContext, useEffect, useState } from 'react';
const ThemeContext = createContext(undefined);
export function ThemeProvider({ children }) {
    const [theme, setThemeState] = useState('system');
    const [isDark, setIsDark] = useState(false);
    // Load theme from localStorage
    useEffect(() => {
        const stored = localStorage.getItem('theme');
        if (stored) {
            setThemeState(stored);
        }
        // Apply theme on mount
        applyTheme(stored || 'system');
    }, []);
    const applyTheme = (selectedTheme) => {
        const html = document.documentElement;
        let actualTheme = selectedTheme;
        if (selectedTheme === 'system') {
            actualTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        }
        if (actualTheme === 'dark') {
            html.classList.add('dark');
            setIsDark(true);
        }
        else {
            html.classList.remove('dark');
            setIsDark(false);
        }
    };
    const setTheme = (newTheme) => {
        setThemeState(newTheme);
        localStorage.setItem('theme', newTheme);
        applyTheme(newTheme);
    };
    // Listen for system theme changes
    useEffect(() => {
        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
        const handleChange = () => {
            if (theme === 'system') {
                applyTheme('system');
            }
        };
        mediaQuery.addEventListener('change', handleChange);
        return () => mediaQuery.removeEventListener('change', handleChange);
    }, [theme]);
    return (_jsx(ThemeContext.Provider, { value: { theme, setTheme, isDark }, children: children }));
}
export function useTheme() {
    const context = useContext(ThemeContext);
    if (!context) {
        throw new Error('useTheme must be used within ThemeProvider');
    }
    return context;
}
