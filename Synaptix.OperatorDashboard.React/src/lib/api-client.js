/**
 * Typed API client with automatic auth header injection and error handling
 * Mirrors Django's service client pattern but in TypeScript
 */
import axios from 'axios';
import { useAuthStore } from '@/features/auth/store';
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';
/**
 * Create an Axios instance with default config
 */
function createApiInstance() {
    const instance = axios.create({
        baseURL: API_BASE_URL,
        timeout: 10000,
        headers: {
            'Content-Type': 'application/json',
        },
    });
    // Request interceptor: inject auth token
    instance.interceptors.request.use((config) => {
        const { accessToken } = useAuthStore.getState();
        if (accessToken && config.headers) {
            config.headers.Authorization = `Bearer ${accessToken}`;
        }
        return config;
    }, (error) => Promise.reject(error));
    // Response interceptor: handle auth errors
    instance.interceptors.response.use((response) => response, async (error) => {
        const originalRequest = error.config;
        // Handle 401 Unauthorized
        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;
            try {
                const { refreshToken } = useAuthStore.getState();
                if (!refreshToken) {
                    useAuthStore.getState().logout();
                    window.location.href = '/login';
                    return Promise.reject(error);
                }
                // Attempt to refresh token
                const response = await axios.post(`${API_BASE_URL}/admin/auth/refresh`, {
                    refreshToken,
                });
                const { accessToken, expiresIn } = response.data;
                useAuthStore.getState().setTokens(accessToken, refreshToken, expiresIn);
                // Retry original request with new token
                if (originalRequest.headers) {
                    originalRequest.headers.Authorization = `Bearer ${accessToken}`;
                }
                return instance(originalRequest);
            }
            catch (refreshError) {
                useAuthStore.getState().logout();
                window.location.href = '/login';
                return Promise.reject(refreshError);
            }
        }
        // Handle 403 Forbidden
        if (error.response?.status === 403) {
            const errorData = error.response.data;
            const message = typeof errorData.message === 'string'
                ? errorData.message
                : 'You do not have permission to access this resource';
            return Promise.reject(new Error(message));
        }
        return Promise.reject(error);
    });
    return instance;
}
export const apiClient = createApiInstance();
/**
 * Typed API request helper
 */
export async function apiRequest(method, url, data, config) {
    const response = await apiClient.request({
        method,
        url,
        data,
        ...config,
    });
    return response.data;
}
/**
 * GET request
 */
export function apiGet(url, config) {
    return apiRequest('GET', url, undefined, config);
}
/**
 * POST request
 */
export function apiPost(url, data, config) {
    return apiRequest('POST', url, data, config);
}
/**
 * PUT request
 */
export function apiPut(url, data, config) {
    return apiRequest('PUT', url, data, config);
}
/**
 * DELETE request
 */
export function apiDelete(url, config) {
    return apiRequest('DELETE', url, undefined, config);
}
/**
 * PATCH request
 */
export function apiPatch(url, data, config) {
    return apiRequest('PATCH', url, data, config);
}
