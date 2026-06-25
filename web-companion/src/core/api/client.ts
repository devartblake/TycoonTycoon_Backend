/**
 * Axios HTTP client with auth interceptors
 * Mirrors lib/core/networking/synaptix_api_client.dart from Flutter
 */

import axios, { AxiosError } from 'axios';
import type { AxiosInstance, InternalAxiosRequestConfig } from 'axios';
import env from '../env';

export interface ApiErrorResponse {
  message: string;
  code: string;
  statusCode: number;
  errors?: Record<string, string[]>;
}

class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: env.apiUrl,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    // Request interceptor: attach auth token
    this.client.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        const token = this.getAuthToken();
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }

        // Add custom headers (e.g., device ID, app version)
        config.headers['X-App-Version'] = env.appVersion;
        config.headers['X-Device-Id'] = this.getDeviceId();

        return config;
      },
      (error: AxiosError) => Promise.reject(error)
    );

    // Response interceptor: handle errors and token refresh
    this.client.interceptors.response.use(
      (response) => response,
      async (error: AxiosError<ApiErrorResponse>) => {
        if (error.response?.status === 401) {
          // Unauthorized: attempt token refresh or redirect to login
          const refreshed = await this.refreshToken();
          if (refreshed) {
            return this.client(error.config!);
          }
          // Refresh failed: redirect to login
          window.location.href = '/login';
        }

        return Promise.reject(error);
      }
    );
  }

  private getAuthToken(): string | null {
    // TODO: retrieve from secure storage (Web Crypto API)
    return localStorage.getItem('auth_token');
  }

  private async refreshToken(): Promise<boolean> {
    try {
      const refreshToken = localStorage.getItem('refresh_token');
      if (!refreshToken) return false;

      const response = await this.client.post('/auth/refresh', { refreshToken });
      const { token, refreshToken: newRefreshToken } = response.data;

      localStorage.setItem('auth_token', token);
      localStorage.setItem('refresh_token', newRefreshToken);
      return true;
    } catch (error) {
      console.error('Token refresh failed:', error);
      return false;
    }
  }

  private getDeviceId(): string {
    let deviceId = localStorage.getItem('device_id');
    if (!deviceId) {
      deviceId = `web_${Math.random().toString(36).substr(2, 9)}`;
      localStorage.setItem('device_id', deviceId);
    }
    return deviceId;
  }

  // Public methods
  get<T = any>(url: string, config?: any) {
    return this.client.get<T>(url, config);
  }

  post<T = any>(url: string, data?: any, config?: any) {
    return this.client.post<T>(url, data, config);
  }

  put<T = any>(url: string, data?: any, config?: any) {
    return this.client.put<T>(url, data, config);
  }

  patch<T = any>(url: string, data?: any, config?: any) {
    return this.client.patch<T>(url, data, config);
  }

  delete<T = any>(url: string, config?: any) {
    return this.client.delete<T>(url, config);
  }

  // Raw client for advanced usage
  getClient() {
    return this.client;
  }
}

export const apiClient = new ApiClient();
export default apiClient;
