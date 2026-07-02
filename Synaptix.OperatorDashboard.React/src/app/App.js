import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Main App component
 * Providers and routing setup
 */
import { useEffect } from 'react';
import { RouterProvider } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from '@/components/ui/toaster';
import { router } from './router';
import { queryClient } from '@/lib/query-client';
import { useAuthStore, restoreAuthState } from '@/features/auth/store';
import '@/index.css';
function AppContent() {
    const setLoading = useAuthStore((state) => state.setLoading);
    // Restore auth state from storage on app init
    useEffect(() => {
        setLoading(true);
        restoreAuthState().finally(() => {
            setLoading(false);
        });
    }, [setLoading]);
    return (_jsxs(QueryClientProvider, { client: queryClient, children: [_jsx(RouterProvider, { router: router }), _jsx(Toaster, {})] }));
}
export default AppContent;
