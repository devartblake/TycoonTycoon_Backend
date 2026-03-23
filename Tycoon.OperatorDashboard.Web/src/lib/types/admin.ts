// ─── Admin Auth DTOs ─────────────────────────────────────────────────
// Mirrors Tycoon.Shared.Contracts/Dtos/AdminContractDtos.cs

export interface AdminLoginRequest {
  email: string
  password: string
  otpCode?: string
}

export interface AdminLoginResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
  tokenType: string
  admin: AdminProfile
}

export interface AdminRefreshResponse {
  accessToken: string
  expiresIn: number
  tokenType: string
}

export interface AdminProfile {
  id: string
  email: string
  displayName: string
  roles: string[]
  permissions: string[]
}

// ─── Admin Users ─────────────────────────────────────────────────────

export interface AdminUserSummary {
  id: string
  email: string
  handle: string
  country?: string
  tier?: string
  mmr: number
  isBanned: boolean
  createdAt: string
}

export interface AdminUserListResponse {
  items: AdminUserSummary[]
  total: number
  page: number
  pageSize: number
}

// ─── Seasons ─────────────────────────────────────────────────────────

export interface SeasonSummary {
  id: string
  name: string
  status: string
  startsAt: string
  endsAt: string
}

// ─── Game Events ─────────────────────────────────────────────────────

export interface GameEventSummary {
  id: string
  name: string
  status: string
  scheduledAt: string
}

// ─── Config ──────────────────────────────────────────────────────────

export interface AppConfig {
  flags: Record<string, boolean>
}

// ─── Notifications ───────────────────────────────────────────────────

export interface NotificationChannel {
  key: string
  name: string
  enabled: boolean
}

export interface SendNotificationRequest {
  channelKey: string
  title: string
  body: string
}

// ─── Generic API envelope ────────────────────────────────────────────

export interface ApiError {
  code: string
  message: string
  details?: Record<string, string[]>
}
