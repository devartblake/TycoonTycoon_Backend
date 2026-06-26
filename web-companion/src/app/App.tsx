/**
 * Root App component with React Router setup
 */

import { useEffect } from 'react';
import { RouterProvider } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import ThemeProvider from '@components/layout/ThemeProvider';
import ErrorBoundary from '@components/layout/ErrorBoundary';
import ToastContainer from '@components/notifications/ToastContainer';
import { useAuthStore } from '@stores';
import { apiClient } from '@core/api/client';
import { queryClient } from './providers';
import router from './router';

function App() {
  useEffect(() => {
    // Restore auth state from localStorage on app initialization
    const restoreAuthState = async () => {
      const token = localStorage.getItem('auth_token');
      if (!token) return; // No token stored, user is not logged in

      try {
        // Verify token is still valid by fetching current user
        const userData = await apiClient.getCurrentUser();

        // Transform backend UserDto to frontend User format
        const user = {
          id: userData.id,
          email: userData.email,
          displayName: userData.handle || 'User',
          avatar: userData.avatarUrl || undefined,
          role: (userData.userRoles?.[0] || 'user').toLowerCase() as 'user' | 'admin',
          createdAt: new Date().toISOString(),
        };

        // Restore user to auth store
        useAuthStore.getState().setUser(user);
        console.log('[Auth] Auth state restored from localStorage');
      } catch (err: any) {
        console.error('[Auth] Failed to restore auth state:', err);
        // Token is invalid or expired, clear it
        localStorage.removeItem('auth_token');
        localStorage.removeItem('refresh_token');
        useAuthStore.getState().logout();
      }
    };

    restoreAuthState();
  }, []);

  return (
    <ErrorBoundary>
      <ThemeProvider>
        <QueryClientProvider client={queryClient}>
          <RouterProvider router={router} />
          <ToastContainer />
        </QueryClientProvider>
      </ThemeProvider>
    </ErrorBoundary>
  );
}

export default App;
