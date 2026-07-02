/**
 * TanStack Query configuration
 * Centralized configuration for data fetching, caching, and synchronization
 */
import { QueryClient, } from '@tanstack/react-query';
const queryConfig = {
    queries: {
        // Stale time: 1 minute
        staleTime: 60 * 1000,
        // Garbage collection time: 5 minutes
        gcTime: 5 * 60 * 1000,
        // Retry on failure (except 4xx errors)
        retry: (failureCount, error) => {
            if (error instanceof Error) {
                // Don't retry on 4xx errors (client errors)
                if (error.message.includes('403') || error.message.includes('404')) {
                    return false;
                }
            }
            // Retry up to 3 times
            return failureCount < 3;
        },
        // Retry delay: exponential backoff
        retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
    },
    mutations: {
        // Retry mutations up to 1 time
        retry: 1,
        retryDelay: 1000,
    },
};
export const queryClientConfig = {
    defaultOptions: queryConfig,
};
export const queryClient = new QueryClient(queryClientConfig);
