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

// ─── Pagination ─────────────────────────────────────────────────────

export interface PaginatedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

// ─── Admin Users ────────────────────────────────────────────────────
// Mirrors Tycoon.Shared.Contracts/Dtos/AdminUserDtos.cs

export interface AdminUsersListRequest {
  q?: string
  status?: string
  role?: string
  ageGroup?: string
  isVerified?: boolean
  isBanned?: boolean
  page?: number
  pageSize?: number
  sortBy?: string
  sortOrder?: string
}

export interface AdminUserListItem {
  id: string
  username: string
  email: string
  status: string
  role: string
  ageGroup: string
  createdAt: string
  lastActive: string | null
  totalGamesPlayed: number
  totalPoints: number
  winRate: number
  isVerified: boolean
  isBanned: boolean
}

export interface AdminUserDetail extends AdminUserListItem {
  metadata: Record<string, unknown>
}

export interface AdminCreateUserRequest {
  username: string
  email: string
  role: string
  ageGroup: string
  isVerified: boolean
  temporaryPassword: string
}

export interface AdminUpdateUserRequest {
  username?: string
  role?: string
  isVerified?: boolean
}

export interface AdminBanUserRequest {
  reason: string
  until?: string
}

export interface AdminBanUserResponse {
  id: string
  isBanned: boolean
  bannedUntil: string | null
}

export interface AdminUserActivityItem {
  id: string
  type: string
  description: string
  createdAt: string
  metadata: Record<string, unknown>
}

// ─── Moderation ─────────────────────────────────────────────────────
// Mirrors Tycoon.Shared.Contracts/Dtos/ModerationDtos.cs

export enum ModerationStatus {
  Normal = 0,
  Suspected = 1,
  Restricted = 2,
  Banned = 3
}

export const ModerationStatusLabel: Record<ModerationStatus, string> = {
  [ModerationStatus.Normal]: 'Normal',
  [ModerationStatus.Suspected]: 'Suspected',
  [ModerationStatus.Restricted]: 'Restricted',
  [ModerationStatus.Banned]: 'Banned'
}

export const ModerationStatusColor: Record<ModerationStatus, 'success' | 'warning' | 'error' | 'default'> = {
  [ModerationStatus.Normal]: 'success',
  [ModerationStatus.Suspected]: 'warning',
  [ModerationStatus.Restricted]: 'error',
  [ModerationStatus.Banned]: 'error'
}

export interface ModerationProfile {
  playerId: string
  status: ModerationStatus
  reason: string | null
  notes: string | null
  setByAdmin: string | null
  setAtUtc: string
  expiresAtUtc: string | null
}

export interface SetModerationStatusRequest {
  playerId: string
  status: ModerationStatus
  reason?: string
  notes?: string
  expiresAtUtc?: string
  relatedFlagId?: string
}

export interface ModerationLogItem {
  id: string
  playerId: string
  newStatus: number
  reason: string | null
  notes: string | null
  setByAdmin: string | null
  createdAtUtc: string
  expiresAtUtc: string | null
  relatedFlagId: string | null
}

// ─── Escalation ─────────────────────────────────────────────────────
// Mirrors Tycoon.Shared.Contracts/Dtos/EscalationDtos.cs

export interface RunEscalationRequest {
  windowHours: number
  maxPlayers: number
  dryRun: boolean
}

export interface EscalationDecision {
  playerId: string
  currentStatus: number
  proposedStatus: number
  severeCount: number
  warningCount: number
  windowStartUtc: string
  windowEndUtc: string
  reason: string
}

export interface RunEscalationResponse {
  dryRun: boolean
  evaluatedPlayers: number
  changedPlayers: number
  decisions: EscalationDecision[]
}

// ─── Anti-Cheat ─────────────────────────────────────────────────────
// Mirrors Tycoon.Shared.Contracts/Dtos/AntiCheatDtos.cs

export interface AntiCheatFlag {
  id: string
  matchId: string
  playerId: string | null
  ruleKey: string
  severity: number
  action: number
  message: string
  createdAtUtc: string
  reviewedAtUtc: string | null
  reviewedBy: string | null
  reviewNote: string | null
}

export interface ReviewAntiCheatFlagRequest {
  reviewedBy: string
  note?: string
}

export interface AntiCheatRuleCount {
  ruleKey: string
  severity: number
  count: number
}

export interface AntiCheatSummary {
  windowStartUtc: string
  windowEndUtc: string
  totalFlags: number
  severeFlags: number
  warningFlags: number
  infoFlags: number
  byRule: AntiCheatRuleCount[]
}

export interface PlayerRiskRow {
  playerId: string
  severeCount: number
  warningCount: number
  currentStatus: number
  lastFlagAtUtc: string
}

// ─── Party Anti-Cheat ───────────────────────────────────────────────
// Mirrors Tycoon.Shared.Contracts/Dtos/AdminPartyAntiCheatDtos.cs

export interface PartyAntiCheatFlag {
  id: string
  createdAtUtc: string
  matchId: string
  playerId: string | null
  ruleKey: string
  severity: string
  action: string
  message: string
  partyId: string | null
  evidenceJson: string | null
}

// ─── Economy ────────────────────────────────────────────────────────
// Mirrors Tycoon.Shared.Contracts/Dtos/EconomyDtos.cs

export enum CurrencyType {
  Xp = 1,
  Coins = 2,
  Diamonds = 3
}

export interface EconomyLine {
  currency: CurrencyType
  delta: number
}

export interface CreateEconomyTxnRequest {
  eventId: string
  playerId: string
  kind: string
  lines: EconomyLine[]
  note?: string
}

export interface EconomyTxnResult {
  eventId: string
  playerId: string
  status: number
  appliedLines: EconomyLine[]
  balanceXp: number
  balanceCoins: number
  balanceDiamonds: number
  processedAtUtc: string
}

export interface EconomyTxnListItem {
  eventId: string
  kind: string
  lines: EconomyLine[]
  createdAtUtc: string
}

export interface EconomyHistory {
  playerId: string
  page: number
  pageSize: number
  total: number
  items: EconomyTxnListItem[]
}

// ─── Season Points ──────────────────────────────────────────────────
// Mirrors Tycoon.Shared.Contracts/Dtos/SeasonDtos.cs (season-point txns)

export interface ApplySeasonPointsRequest {
  eventId: string
  seasonId: string
  playerId: string
  kind: string
  delta: number
  note?: string
}

export interface ApplySeasonPointsResult {
  eventId: string
  seasonId: string
  playerId: string
  status: string // "Applied" | "Duplicate"
  newRankPoints: number
}

export interface SeasonPointTxnListItem {
  eventId: string
  seasonId: string
  kind: string
  delta: number
  note: string | null
  createdAtUtc: string
}

export interface SeasonPointHistory {
  playerId: string
  page: number
  pageSize: number
  total: number
  items: SeasonPointTxnListItem[]
}

// ─── Seasons ────────────────────────────────────────────────────────

export interface SeasonSummary {
  id: string
  name: string
  status: string
  startsAt: string
  endsAt: string
}

// ─── Game Events ────────────────────────────────────────────────────

export interface GameEventSummary {
  id: string
  name: string
  status: string
  scheduledAt: string
}

// ─── Config ─────────────────────────────────────────────────────────

export interface AppConfig {
  flags: Record<string, boolean>
}

// ─── Notifications ──────────────────────────────────────────────────

export interface NotificationChannel {
  key: string
  name: string
  description: string
  importance: string
  enabled: boolean
}

export interface SendNotificationRequest {
  title: string
  body: string
  channelKey: string
  audience: Record<string, unknown>
  payload?: Record<string, unknown>
}

export interface SendNotificationResponse {
  jobId: string
  estimatedRecipients: number
}

export interface NotificationScheduledItem {
  scheduleId: string
  title: string
  channelKey: string
  scheduledAt: string
  status: string
}

export interface NotificationScheduledListResponse {
  items: NotificationScheduledItem[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface NotificationHistoryItem {
  id: string
  channelKey: string
  title: string
  status: string
  createdAt: string
  metadata: Record<string, unknown> | null
}

export interface NotificationHistoryResponse {
  items: NotificationHistoryItem[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface NotificationScheduleResponse {
  scheduleId: string
}

// ─── Generic API envelope ───────────────────────────────────────────

export interface ApiError {
  code: string
  message: string
  details?: Record<string, string[]>
}
