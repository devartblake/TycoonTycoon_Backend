/**
 * Axios HTTP client with auth interceptors
 * Mirrors lib/core/networking/synaptix_api_client.dart from Flutter
 */

import axios, { AxiosError } from 'axios';
import type { AxiosInstance, InternalAxiosRequestConfig } from 'axios';
import env from '../env';
import { useAuthStore } from '@stores/authStore';

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

        // Debug logging in development
        if (env.isDev) {
          console.debug(`📡 API Request: ${config.method?.toUpperCase()} ${config.baseURL}${config.url}`);
        }

        return config;
      },
      (error: AxiosError) => Promise.reject(error)
    );

    // Response interceptor: handle errors and token refresh
    this.client.interceptors.response.use(
      (response) => response,
      async (error: AxiosError<ApiErrorResponse>) => {
        // Log detailed error information for debugging
        if (error.code === 'ERR_NETWORK' || error.message === 'Network Error') {
          console.error('🌐 Network Error:', {
            message: error.message,
            baseURL: env.apiV1Url,
            url: error.config?.url,
            hint: 'Check if backend is running and CORS is configured correctly',
          });
        }

        if (error.response?.status === 0 && error.message.includes('CORS')) {
          console.error('🔒 CORS Error detected:', {
            backend: env.apiBaseUrl,
            frontend: window.location.origin,
            hint: 'Backend must have Access-Control-Allow-Origin header set to allow this frontend URL',
          });
        }

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

  // Current authenticated player id (needed by endpoints that take an explicit playerId)
  private requirePlayerId(): string {
    const playerId = useAuthStore.getState().user?.id;
    if (!playerId) throw new Error('Not authenticated');
    return playerId;
  }

  // Quiz/Question endpoints
  async getQuizQuestions(category: string, difficulty: string, count: number = 10) {
    const response = await this.get('/questions/set', {
      params: { category, difficulty, count },
    });
    // Backend serves GameplayQuestionDto and withholds correct answers
    // (server-side grading via /questions/check). Map to the client Question shape.
    const questions = response.data.questions || [];
    return questions.map((q: any) => ({
      id: q.id,
      question: q.text,
      category: q.category,
      difficulty: String(q.difficulty).toLowerCase(),
      options: (q.options || []).map((o: any) => o.text),
      optionIds: (q.options || []).map((o: any) => o.id),
      timeLimit: 30,
    }));
  }

  async checkAnswer(questionId: string, selectedOptionId: string) {
    const response = await this.post('/questions/check', { questionId, selectedOptionId });
    return response.data; // { questionId, selectedOptionId, correctOptionId, isCorrect }
  }

  async checkAnswers(answers: Array<{ questionId: string; selectedOptionId: string }>) {
    const response = await this.post('/questions/check-batch', { answers });
    return response.data;
  }

  async getQuestionCategories() {
    const response = await this.get('/questions/categories');
    return response.data.categories; // Array<{ key: string; count: number }>
  }

  // Match/Quiz endpoints
  async startMatch(mode: string = 'single') {
    const hostPlayerId = this.requirePlayerId();
    const response = await this.post('/matches/start', { hostPlayerId, mode });
    return response.data; // { matchId, startedAt }
  }

  async submitMatchResults(params: {
    matchId: string;
    category: string;
    questionCount: number;
    startedAtUtc: string;
    endedAtUtc: string;
    score: number;
    correct: number;
    wrong: number;
    avgAnswerTimeMs: number;
    answers: Array<{ questionId: string; selectedOptionId: string; answerTimeMs: number }>;
    mode?: string;
  }) {
    const playerId = this.requirePlayerId();
    const response = await this.post('/matches/submit', {
      eventId: crypto.randomUUID(), // idempotency key for submission + payouts
      matchId: params.matchId,
      mode: params.mode ?? 'single',
      category: params.category,
      questionCount: params.questionCount,
      startedAtUtc: params.startedAtUtc,
      endedAtUtc: params.endedAtUtc,
      status: 'Completed',
      participants: [
        {
          playerId,
          score: params.score,
          correct: params.correct,
          wrong: params.wrong,
          avgAnswerTimeMs: params.avgAnswerTimeMs,
        },
      ],
      answers: params.answers.map((a) => ({ playerId, ...a })),
    });
    return response.data; // { eventId, matchId, status, awards }
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
  async getLeaderboard(limit: number = 50) {
    const response = await this.get('/leaderboard', {
      params: { limit },
    });
    // Legacy rows: { user_id, playerName, score, rank, tier, tierRank, wins, avatar, level }
    const rows = Array.isArray(response.data) ? response.data : response.data.players || [];
    return rows.map((r: any) => ({
      rank: r.rank,
      playerId: r.user_id,
      username: r.playerName,
      xp: r.score,
      level: r.level,
      tier: String(r.tier),
      avatar: r.avatar ?? undefined,
    }));
  }

  async getPlayerRank(playerId: string) {
    try {
      const response = await this.get(`/leaderboards/me/${playerId}`);
      const d = response.data; // MyTierDto
      return {
        rank: d.globalRank,
        playerId: d.playerId,
        username: useAuthStore.getState().user?.displayName ?? 'You',
        xp: d.score,
        level: 0,
        tier: String(d.tierId),
      };
    } catch (error) {
      // No leaderboard entry yet — not an error worth surfacing
      if (axios.isAxiosError(error) && error.response?.status === 404) return null;
      throw error;
    }
  }

  // Social/Friends endpoints
  async getFriends() {
    const response = await this.get('/users/me/friends', {
      params: { page: 1, pageSize: 100 },
    });
    const items = response.data.items || [];
    return items.map((f: any) => ({
      playerId: f.friendPlayerId,
      username: f.username || f.displayName,
      level: 0,
      xp: 0,
      avatar: f.avatarUrl ?? undefined,
      isOnline: f.isOnline,
    }));
  }

  async addFriend(username: string) {
    // The backend takes a target user id, so resolve the username first
    const search = await this.get('/users/search', {
      params: { handle: username, page: 1, pageSize: 10 },
    });
    const candidates = search.data.items || [];
    const target =
      candidates.find((u: any) => u.handle?.toLowerCase() === username.toLowerCase()) ??
      candidates[0];
    if (!target) throw new Error(`User "${username}" not found`);

    const response = await this.post('/users/me/friends/request', {
      targetUserId: target.id,
    });
    return response.data;
  }

  async removeFriend(friendId: string) {
    const response = await this.delete(`/users/me/friends/${friendId}`);
    return response.data;
  }

  async getFriendRequests() {
    const response = await this.get('/users/me/friends/requests', {
      params: { page: 1, pageSize: 50 },
    });
    const items = response.data.items || [];
    return items.map((r: any) => ({
      requestId: r.requestId,
      fromPlayerId: r.fromPlayerId,
      fromUsername: r.senderUsername || r.senderDisplayName,
      timestamp: r.createdAtUtc,
      avatar: r.senderAvatarUrl ?? undefined,
    }));
  }

  async acceptFriendRequest(requestId: string) {
    const response = await this.post(`/users/me/friends/requests/${requestId}/accept`, {});
    return response.data;
  }

  async declineFriendRequest(requestId: string) {
    const response = await this.post(`/users/me/friends/requests/${requestId}/decline`, {});
    return response.data;
  }

  // Store endpoints
  async getStoreItems() {
    const response = await this.get('/store/catalog');
    const items = response.data.items || [];
    return items.map((i: any) => ({
      itemId: i.sku,
      name: i.name,
      description: i.description,
      category: i.itemType,
      price: i.priceCoins > 0 ? i.priceCoins : i.priceDiamonds,
      currencyType: i.priceCoins > 0 ? 'coins' : 'diamonds',
    }));
  }

  async purchaseItem(sku: string, currencyType: 'coins' | 'diamonds') {
    const playerId = this.requirePlayerId();
    const response = await this.post('/store/purchase', {
      playerId,
      sku,
      quantity: 1,
      currency: currencyType,
    });
    return response.data;
  }

  // Missions endpoints
  async getMissions() {
    const response = await this.get('/missions');
    // MissionDto: { id, type, key, goal, rewardXp } — global definitions, no per-player progress
    const missions = response.data.missions || response.data || [];
    return missions.map((m: any) => ({
      missionId: m.id,
      title: String(m.key || '')
        .replace(/[_-]+/g, ' ')
        .replace(/^\w/, (c: string) => c.toUpperCase()),
      description: '',
      type: String(m.type || '').toLowerCase(),
      progress: 0,
      target: m.goal,
      reward: { xp: m.rewardXp },
      completed: false,
      claimed: false,
    }));
  }

  async claimMissionReward(missionId: string) {
    const playerId = this.requirePlayerId();
    const response = await this.post(`/missions/${missionId}/claim`, {}, {
      params: { playerId },
    });
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
