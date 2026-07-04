/**
 * useHealthMetrics Hook
 * Fetches and updates system health metrics from backend
 */
import { useState, useEffect, useCallback } from 'react';
import { healthCheckClient } from '../lib/health-check-client';
export function useHealthMetrics(options = {}) {
    const { enabled = true, pollInterval = 30000, onError, onSuccess, } = options;
    const [metrics, setMetrics] = useState();
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState();
    const fetchMetrics = useCallback(async () => {
        try {
            setIsLoading(true);
            const data = await healthCheckClient.getSystemMetrics();
            setMetrics(data);
            setError(undefined);
            if (onSuccess) {
                onSuccess(data);
            }
        }
        catch (err) {
            const error = err instanceof Error ? err : new Error('Failed to fetch health metrics');
            setError(error);
            if (onError) {
                onError(error);
            }
        }
        finally {
            setIsLoading(false);
        }
    }, [onError, onSuccess]);
    useEffect(() => {
        if (!enabled) {
            return;
        }
        // Fetch immediately
        fetchMetrics();
        // Set up polling
        const intervalId = setInterval(fetchMetrics, pollInterval);
        return () => {
            clearInterval(intervalId);
        };
    }, [enabled, pollInterval, fetchMetrics]);
    const refresh = useCallback(() => {
        fetchMetrics();
    }, [fetchMetrics]);
    return {
        metrics,
        isLoading,
        error,
        refresh,
    };
}
export default useHealthMetrics;
