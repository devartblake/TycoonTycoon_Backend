# User Support Feature — Implementation Plan

## Context

> **Deprecated plan notice (2026-05-12):** This Web/BFF implementation plan is historical only. `Tycoon.OperatorDashboard.Django` is the canonical Operator Dashboard, and user-support workflows should be implemented there with Django RBAC/permission scopes if additional work is needed.

The operator dashboard needs a user support workflow so admins can look up players, review their history, investigate anti-cheat flags, take moderation actions, and manage escalations — all from a single unified interface. The backend already has all the required endpoints. This retired plan originally covered frontend pages in `Tycoon.OperatorDashboard.Web`; any future implementation should happen in `Tycoon.OperatorDashboard.Django`.

---

## Backend Endpoints Available (Already Exist)

### User Management (`/admin/users`)
| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/admin/users?q=&isBanned=&page=&pageSize=` | Search/list users with filters |
| GET | `/admin/users/{userId}` | Full user detail (stats, metadata) |
| POST | `/admin/users` | Create user |
| PATCH | `/admin/users/{userId}` | Update user |
| POST | `/admin/users/{userId}/ban` | Ban user (reason + optional until date) |
| POST | `/admin/users/{userId}/unban` | Unban user |
| DELETE | `/admin/users/{userId}` | Delete user |
| GET | `/admin/users/{userId}/activity` | Activity history (logins, events) |

### Moderation (`/admin/moderation`)
| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/admin/moderation/profile/{playerId}` | Full moderation profile |
| POST | `/admin/moderation/set-status` | Set moderation status (normal/suspected/restricted/banned) |
| GET | `/admin/moderation/logs?playerId=&page=&pageSize=` | Moderation action history |

### Escalations (`/admin/moderation/escalation`)
| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/admin/moderation/escalation/run` | Run auto-escalation (dry-run supported) |

### Anti-Cheat (`/admin/anti-cheat`)
| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/admin/anti-cheat/flags?unreviewedOnly=&severity=&page=&pageSize=` | List anti-cheat flags |
| PUT | `/admin/anti-cheat/flags/{id}/review` | Mark flag as reviewed |
| GET | `/admin/anti-cheat/summary?windowHours=` | Summary statistics |

### Party Detection (`/admin/party-detection`)
| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/admin/party-detection/flags?unreviewedOnly=&page=&pageSize=` | List party collusion flags |
| POST | `/admin/party-detection/flags/{id}/review` | Mark flag as reviewed |

### Player Economy (`/admin/players`)
| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/admin/players/{playerId}/economy-history?page=&pageSize=` | Transaction history |
| POST | `/admin/players/transactions` | Create manual transaction |
| GET | `/admin/players/{playerId}/powerups` | Player's powerups |
| POST | `/admin/players/powerups/grant` | Grant powerup to player |

### Notifications (`/admin/notifications`)
| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/admin/notifications/send` | Send notification to user/audience |

---

## Implementation Plan

### Phase 1: Shared Components

Before building pages, create reusable components that all support pages share.

**File: `src/components/admin/DataTable.tsx`**
- Generic paginated table component
- Props: columns, data, loading, page, pageSize, total, onPageChange
- Built on MUI `Table` + `TablePagination`
- Supports sort indicators and click-to-sort

**File: `src/components/admin/SearchFilterBar.tsx`**
- Search input + filter dropdowns
- Debounced search (300ms) to avoid excessive API calls
- Filter chips showing active filters with clear buttons

**File: `src/components/admin/ConfirmDialog.tsx`**
- Reusable confirmation modal for destructive actions
- Props: open, title, message, confirmLabel, confirmColor, onConfirm, onCancel
- Used before ban, unban, delete, status change

**File: `src/components/admin/StatusBadge.tsx`**
- Colored chip for moderation statuses
- Normal (green), Suspected (yellow), Restricted (orange), Banned (red)

**File: `src/components/admin/PageHeader.tsx`**
- Consistent page title + optional breadcrumb + action buttons area

### Phase 2: Users Page (`/users`)

**Route:** `src/app/(dashboard)/users/page.tsx`

**Layout:**
```
┌─────────────────────────────────────────────────┐
│ Users                               [+ Create]  │
├─────────────────────────────────────────────────┤
│ Search: [__________]  Status: [All ▾]           │
├────┬──────────┬─────────┬────────┬──────┬───────┤
│ ID │ Username │ Email   │ Status │ MMR  │ Actions│
├────┼──────────┼─────────┼────────┼──────┼───────┤
│ .. │ player1  │ p@e.com │ ● Active│ 1200│ [View]│
│ .. │ cheater2 │ c@e.com │ ● Banned│  800│ [View]│
└────┴──────────┴─────────┴────────┴──────┴───────┘
│ Showing 1-25 of 1,203          [< 1 2 3 ... >]  │
└─────────────────────────────────────────────────┘
```

**Features:**
- Paginated user list from `GET /admin/users`
- Search by username/email (debounced)
- Filter by banned status
- Click row → navigate to user detail page

### Phase 3: User Detail Page (`/users/[userId]`)

**Route:** `src/app/(dashboard)/users/[userId]/page.tsx`

**Layout:**
```
┌─────────────────────────────────────────────────┐
│ ← Back to Users                                 │
├──────────────────────┬──────────────────────────┤
│ Profile Card         │ Moderation Status Card   │
│ - Avatar/Initials    │ - Current status badge   │
│ - Username, Email    │ - Set by / reason        │
│ - Country, Tier, MMR │ - Expires at             │
│ - Created, Last seen │ - [Change Status ▾]      │
│ - Verified badge     │ - [Ban] [Unban]          │
├──────────────────────┴──────────────────────────┤
│ Tabs: [Activity] [Anti-Cheat] [Economy] [Power] │
├─────────────────────────────────────────────────┤
│ Activity Tab:                                    │
│ - Timeline of logins, matches, events            │
│ - Date range filter                              │
│                                                  │
│ Anti-Cheat Tab:                                  │
│ - Flags for this player                          │
│ - Severity indicators                            │
│ - Review button per flag                         │
│                                                  │
│ Economy Tab:                                     │
│ - Transaction history table                      │
│ - [+ Create Transaction] button                  │
│                                                  │
│ Powerups Tab:                                    │
│ - Current powerups list                          │
│ - [Grant Powerup] button                         │
├─────────────────────────────────────────────────┤
│ Moderation Log (bottom section):                 │
│ - Chronological list of all moderation actions   │
└─────────────────────────────────────────────────┘
```

**API calls on this page:**
1. `GET /admin/users/{userId}` — profile data
2. `GET /admin/moderation/profile/{playerId}` — moderation status
3. `GET /admin/users/{userId}/activity` — activity tab
4. `GET /admin/anti-cheat/flags?playerId={id}` — anti-cheat tab
5. `GET /admin/players/{playerId}/economy-history` — economy tab
6. `GET /admin/players/{playerId}/powerups` — powerups tab
7. `GET /admin/moderation/logs?playerId={id}` — moderation log

**Actions available:**
- Ban user (`POST /admin/users/{userId}/ban`) with reason + optional expiry
- Unban user (`POST /admin/users/{userId}/unban`)
- Change moderation status (`POST /admin/moderation/set-status`)
- Review anti-cheat flag (`PUT /admin/anti-cheat/flags/{id}/review`)
- Create transaction (`POST /admin/players/transactions`)
- Grant powerup (`POST /admin/players/powerups/grant`)
- Send notification (`POST /admin/notifications/send`)

### Phase 4: Anti-Cheat Review Queue (`/anticheat`)

**Route:** `src/app/(dashboard)/anticheat/page.tsx`

**Layout:**
- Summary cards at top (total flags, unreviewed, severity breakdown) from `GET /admin/anti-cheat/summary`
- Filterable table of flags from `GET /admin/anti-cheat/flags`
- Click flag → opens dialog with detail + link to player profile
- Bulk review capability (select multiple → mark reviewed)

### Phase 5: Escalations Page (`/escalations`)

**Route:** `src/app/(dashboard)/escalations/page.tsx`

**Layout:**
- "Run Escalation" form: window hours, max players, dry-run toggle
- Results table showing decisions (playerId, current→proposed status, flag counts, reason)
- Dry-run mode shows preview without committing
- Link each player row to their detail page

### Phase 6: Moderation Overview (`/moderation`)

**Route:** `src/app/(dashboard)/moderation/page.tsx`

**Layout:**
- Global moderation log table from `GET /admin/moderation/logs`
- Filter by player, date range
- Party detection flags tab from `GET /admin/party-detection/flags`
- Quick-action review buttons

---

## TypeScript Types Needed

Add to `src/lib/types/admin.ts`:

```typescript
// User management
interface AdminUserListItem {
  id: string
  username: string
  email: string
  status: string
  role: string
  ageGroup: string
  createdAt: string
  lastActive: string
  totalGamesPlayed: number
  totalPoints: number
  winRate: number
  isVerified: boolean
  isBanned: boolean
}

interface AdminUserDetail extends AdminUserListItem {
  country?: string
  tier?: string
  mmr: number
}

interface AdminBanRequest {
  reason: string
  until?: string  // ISO date, optional = permanent
}

interface AdminUserActivity {
  id: string
  type: string
  description: string
  createdAt: string
  metadata: Record<string, unknown>
}

// Moderation
interface ModerationProfile {
  playerId: string
  status: ModerationStatus
  reason: string
  notes: string
  setByAdmin: string
  setAtUtc: string
  expiresAtUtc?: string
}

enum ModerationStatus {
  Normal = 0,
  Suspected = 1,
  Restricted = 2,
  Banned = 3
}

interface ModerationLogItem {
  id: string
  playerId: string
  fromStatus: number
  toStatus: number
  reason: string
  adminUser: string
  createdAt: string
}

// Anti-cheat
interface AntiCheatFlag {
  id: string
  playerId: string
  severity: string
  type: string
  description: string
  isReviewed: boolean
  createdAt: string
}

interface AntiCheatSummary {
  totalFlags: number
  unreviewedCount: number
  bySeverity: Record<string, number>
}

// Escalation
interface EscalationDecision {
  playerId: string
  currentStatus: number
  proposedStatus: number
  severeCount: number
  warningCount: number
  windowStartUtc: string
  windowEndUtc: string
  reason: string
}

interface RunEscalationRequest {
  windowHours: number
  maxPlayers: number
  dryRun: boolean
}

interface RunEscalationResponse {
  dryRun: boolean
  evaluatedPlayers: number
  changedPlayers: number
  decisions: EscalationDecision[]
}
```

---

## API Service Layer

Add to `src/lib/apiClient.ts` or create `src/lib/services/userService.ts`:

```typescript
// User operations
export const userService = {
  list: (params) => apiClient.get('/admin/users?' + toQuery(params)),
  get: (id) => apiClient.get(`/admin/users/${id}`),
  ban: (id, req) => apiClient.post(`/admin/users/${id}/ban`, req),
  unban: (id) => apiClient.post(`/admin/users/${id}/unban`),
  activity: (id, params) => apiClient.get(`/admin/users/${id}/activity?` + toQuery(params)),
}

// Moderation operations
export const moderationService = {
  getProfile: (id) => apiClient.get(`/admin/moderation/profile/${id}`),
  setStatus: (req) => apiClient.post('/admin/moderation/set-status', req),
  logs: (params) => apiClient.get('/admin/moderation/logs?' + toQuery(params)),
  runEscalation: (req) => apiClient.post('/admin/moderation/escalation/run', req),
}

// Anti-cheat operations
export const antiCheatService = {
  flags: (params) => apiClient.get('/admin/anti-cheat/flags?' + toQuery(params)),
  reviewFlag: (id, body) => apiClient.put(`/admin/anti-cheat/flags/${id}/review`, body),
  summary: (windowHours) => apiClient.get(`/admin/anti-cheat/summary?windowHours=${windowHours}`),
}
```

---

## Recommended Build Order

1. **Shared components** (DataTable, SearchFilterBar, ConfirmDialog, StatusBadge) — 1 session
2. **`/users` list page** — 1 session
3. **`/users/[userId]` detail page** (profile + moderation cards) — 1 session
4. **User detail tabs** (activity, anti-cheat, economy, powerups) — 1 session
5. **`/anticheat` review queue** — 1 session
6. **`/escalations` page** — 1 session
7. **`/moderation` overview** — 1 session

Each "session" is roughly one focused implementation task.

---

## File Structure When Complete

```
src/app/(dashboard)/
├── users/
│   ├── page.tsx                    # User list
│   └── [userId]/
│       └── page.tsx                # User detail with tabs
├── anticheat/
│   └── page.tsx                    # Anti-cheat review queue
├── escalations/
│   └── page.tsx                    # Escalation runner
├── moderation/
│   └── page.tsx                    # Moderation overview + logs

src/components/admin/
├── DataTable.tsx
├── SearchFilterBar.tsx
├── ConfirmDialog.tsx
├── StatusBadge.tsx
└── PageHeader.tsx

src/lib/services/
├── userService.ts
├── moderationService.ts
└── antiCheatService.ts

src/lib/types/
└── admin.ts                        # Extended with support types
```
