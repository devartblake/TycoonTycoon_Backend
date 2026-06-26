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
      baseURL: env.apiV1Url,
      timeout: 30000,
      withCredentials: true,
      headers: {
        'Content-Type': 'application/json',
        'Access-Control-Allow-Origin': '*',
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
      // Generate unique device ID: web_UUID_timestamp
      const uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
        const r = (Math.random() * 16) | 0;
        const v = c === 'x' ? r : (r & 0x3) | 0x8;
        return v.toString(16);
      });
      deviceId = `web_${uuid}`;
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

  // Quiz/Question endpoints
  async getQuizQuestions(category: string, difficulty: string, count: number = 10) {
    const response = await this.get('/questions/set', {
      params: { category, difficulty, count },
    });
    return response.data.questions;
  }

  async checkAnswers(answers: Array<{ questionId: string; selectedOptionId: string }>) {
    const response = await this.post('/questions/check-batch', { answers });
    return response.data;
  }

  async getQuestionCategories() {
    const response = await this.get('/questions/categories');
    return response.data.categories;
  }

  // Match/Quiz endpoints
  async startMatch(mode: string = 'single') {
    const response = await this.post('/matches/start', { mode });
    return response.data;
  }

  async submitMatchResults(
    matchId: string,
    answers: Array<{ questionId: string; selectedOptionId: string }>,
    score: number
  ) {
    const response = await this.post('/matches/submit', {
      matchId,
      answers,
      score,
    });
    return response.data;
  }

  async getMatchDetails(matchId: string) {
    const response = await this.get(`/matches/${matchId}`);
    return response.data;
  }

  // User endpoints
  async getCurrentUser() {
    const response = await this.get('/users/me');
    return response.data;
  }

  async getUserWallet() {
    const response = await this.get('/users/me/wallet');
    return response.data;
  }

  async updateUser(displayName?: string, avatar?: string) {
    const response = await this.patch('/users/me', { displayName, avatar });
    return response.data;
  }

  // Season/XP endpoints
  async getPlayerSeasonState(playerId: string) {
    const response = await this.get(`/seasons/state/${playerId}`);
    return response.data;
  }

  async getActiveSeason() {
    const response = await this.get('/seasons/active');
    return response.data;
  }

  // Leaderboard endpoints
  async getLeaderboard(limit: number = 50, offset: number = 0) {
    const response = await this.get('/leaderboard', {
      params: { limit, offset },
    });
    return response.data.players || response.data;
  }

  async getPlayerRank(playerId: string) {
    const response = await this.get(`/leaderboard/rank/${playerId}`);
    return response.data;
  }

  // Social/Friends endpoints
  async getFriends() {
    const response = await this.get('/social/friends');
    return response.data.friends || response.data;
  }

  async addFriend(friendId: string) {
    const response = await this.post('/social/friends/add', { friendId });
    return response.data;
  }

  async removeFriend(friendId: string) {
    const response = await this.post('/social/friends/remove', { friendId });
    return response.data;
  }

  async getFriendRequests() {
    const response = await this.get('/social/friend-requests');
    return response.data.requests || response.data;
  }

  async acceptFriendRequest(requestId: string) {
    const response = await this.post(`/social/friend-requests/${requestId}/accept`, {});
    return response.data;
  }

  async declineFriendRequest(requestId: string) {
    const response = await this.post(`/social/friend-requests/${requestId}/decline`, {});
    return response.data;
  }

  // Store endpoints
  async getStoreItems() {
    const response = await this.get('/store/items');
    return response.data.items || response.data;
  }

  async purchaseItem(itemId: string, currencyType: 'coins' | 'diamonds') {
    const response = await this.post('/store/purchase', { itemId, currencyType });
    return response.data;
  }

  // Missions endpoints
  async getMissions() {
    const response = await this.get('/missions');
    return response.data.missions || response.data;
  }

  async completeMission(missionId: string) {
    const response = await this.post(`/missions/${missionId}/complete`, {});
    return response.data;
  }

  async claimMissionReward(missionId: string) {
    const response = await this.post(`/missions/${missionId}/claim-reward`, {});
    return response.data;
  }

  // Authentication endpoints
  async login(email: string, password: string) {
    const response = await this.post('/auth/login', {
      email,
      password,
      deviceId: this.getDeviceId(),
    });
    return response.data;
  }

  async signup(email: string, password: string, username?: string) {
    const response = await this.post('/auth/signup', {
      email,
      password,
      deviceId: this.getDeviceId(),
      ...(username && { username }),
    });
    return response.data;
  }

  async logout() {
    const response = await this.post('/auth/logout', {});
    return response.data;
  }

  async changePassword(currentPassword: string, newPassword: string) {
    const response = await this.post('/auth/change-password', {
      currentPassword,
      newPassword,
    });
    return response.data;
  }

  async requestPasswordReset(email: string, method: 'email' | 'sms' = 'email') {
    const response = await this.post('/auth/forgot-password', {
      email,
      method,
    });
    return response.data;
  }

  async verifyOtp(email: string, otp: string) {
    const response = await this.post('/auth/verify-otp', {
      email,
      otp,
    });
    return response.data;
  }

  async resetPassword(email: string, token: string, newPassword: string) {
    const response = await this.post('/auth/reset-password', {
      email,
      token,
      newPassword,
    });
    return response.data;
  }
}

export const apiClient = new ApiClient();
export default apiClient;
