/**
 * Typed API client with automatic auth header injection and error handling
 * Mirrors Django's service client pattern but in TypeScript
 */

import axios, { AxiosError, AxiosInstance, AxiosRequestConfig } from 'axios'
import { useAuthStore } from '@/features/auth/store'

// Empty = same-origin. Admin calls must go through the dev Vite proxy or the
// Docker nginx proxy, which inject the required X-Admin-Ops-Key header —
// pointing this directly at the API will 401 every request (key never ships
// in browser JS).
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || ''

/**
 * Create an Axios instance with default config
 */
function createApiInstance(): AxiosInstance {
  const instance = axios.create({
    baseURL: API_BASE_URL,
    timeout: 10000,
    headers: {
      'Content-Type': 'application/json',
    },
  })

  // Request interceptor: inject auth token
  instance.interceptors.request.use(
    (config) => {
      const { accessToken } = useAuthStore.getState()
      if (accessToken && config.headers) {
        config.headers.Authorization = `Bearer ${accessToken}`
      }
      return config
    },
    (error) => Promise.reject(error)
  )

  // Response interceptor: handle auth errors
  instance.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean }

      // A failed login is not an expired session: surface the backend message
      // on the login page instead of running the refresh/redirect flow.
      if (error.response?.status === 401 && originalRequest.url?.includes('/admin/auth/login')) {
        const errorData = error.response.data as Record<string, unknown> | undefined
        const message = typeof errorData?.message === 'string'
          ? errorData.message
          : 'Login failed. Please check your credentials.'
        return Promise.reject(new Error(message))
      }

      // Handle 401 Unauthorized
      if (error.response?.status === 401 && !originalRequest._retry) {
        originalRequest._retry = true

        try {
          const { refreshToken } = useAuthStore.getState()
          if (!refreshToken) {
            useAuthStore.getState().logout()
            window.location.href = '/auth/login'
            return Promise.reject(error)
          }

          // Attempt to refresh token
          const response = await axios.post(`${API_BASE_URL}/admin/auth/refresh`, {
            refreshToken,
          })

          const { accessToken, expiresIn } = response.data
          useAuthStore.getState().setTokens(accessToken, refreshToken, expiresIn)

          // Retry original request with new token
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${accessToken}`
          }
          return instance(originalRequest)
        } catch (refreshError) {
          useAuthStore.getState().logout()
          window.location.href = '/auth/login'
          return Promise.reject(refreshError)
        }
      }

      // Handle 403 Forbidden
      if (error.response?.status === 403) {
        const errorData = error.response.data as Record<string, unknown>
        const message = typeof errorData.message === 'string'
          ? errorData.message
          : 'You do not have permission to access this resource'
        return Promise.reject(new Error(message))
      }

      return Promise.reject(error)
    }
  )

  return instance
}

export const apiClient = createApiInstance()

/**
 * Typed API request helper
 */
export async function apiRequest<T>(
  method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH',
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig
): Promise<T> {
  const response = await apiClient.request<T>({
    method,
    url,
    data,
    ...config,
  })
  return response.data
}

/**
 * GET request
 */
export function apiGet<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
  return apiRequest<T>('GET', url, undefined, config)
}

/**
 * POST request
 */
export function apiPost<T>(
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig
): Promise<T> {
  return apiRequest<T>('POST', url, data, config)
}

/**
 * PUT request
 */
export function apiPut<T>(
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig
): Promise<T> {
  return apiRequest<T>('PUT', url, data, config)
}

/**
 * DELETE request
 */
export function apiDelete<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
  return apiRequest<T>('DELETE', url, undefined, config)
}

/**
 * PATCH request
 */
export function apiPatch<T>(
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig
): Promise<T> {
  return apiRequest<T>('PATCH', url, data, config)
}
