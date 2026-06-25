/**
 * Root App component with React Router setup
 */

import { RouterProvider } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import ThemeProvider from '@components/layout/ThemeProvider';
import ErrorBoundary from '@components/layout/ErrorBoundary';
import ToastContainer from '@components/notifications/ToastContainer';
import { queryClient } from './providers';
import router from './router';

function App() {
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
