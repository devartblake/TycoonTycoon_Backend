# React vs Django Operator Dashboard — Parity + R1 Status

**Date:** 2026-07-13  
**Direction:** React replaces Django — [`OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md`](OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md)

---

## 1. Feature / surface matrix

| Capability | Django | React | Backend `/admin` | Notes |
|------------|:------:|:-----:|:----------------:|-------|
| Login / refresh / me / password reset | Yes | Yes | Yes | **R1 aligned** (paths already matched) |
| Users list / detail / ban / unban / activity | Yes | Yes | Yes | **R1:** React list query fixed to `page`/`pageSize`/`q` |
| User PATCH / player-lookup resolve | Yes | Yes* | Yes | **R1:** added to React API client |
| User investigation workbench | Yes | Yes (page) | Partial | Composition of users + other APIs |
| Saved views (operator-local) | Yes (Django DB) | Mock only | No | Not on .NET API; React returns empty / errors clearly |
| Moderation profile + set-status | Yes | Yes* | Yes | **R1:** React was on non-existent `/players/.../ban` paths — fixed |
| Moderation logs list/detail | Yes | Yes | Yes | Already correct |
| Security audit list/detail | Yes | Yes | Yes | Already adapted |
| Geo IP lookup | Yes | Yes | Yes | Via `/admin/audit/geo-lookup` |
| Anti-cheat flags | Yes | Yes | Yes | Verify path names separately (not R1) |
| Notifications hub | Yes | Yes | Yes | P1 path review |
| Store catalog / flash / stock | Yes | Yes | Yes | P1 |
| Economy player / balance | Yes | Yes | Yes | P1 |
| Questions queue | Yes | Yes | Yes | P1 |
| Seasons / game events | Yes | Yes | Yes | P1 |
| Event queue | Yes | Yes | Partial shape | P1 |
| Personalization overview/player/rules | Yes | Partial | Yes | P1 naming gaps |
| Skills | Yes | Yes | Partial shape | P1 |
| Matches | Yes | Yes | Yes | P1 |
| Feature flags / admin ACL UI | Yes | Yes | Partial | P1 |
| Storage browser | Yes | Yes | Path mismatch | P1 |
| Setup diagnostics (readiness/history) | Yes | Partial | Yes (`/admin/setup/*`) | Wire React installer vs setup |
| Backend installer UI | Yes | Yes | **No full API** | P2 — Setup CLI |
| MinIO / Mongo diagnostics | Yes | Partial (diagnostics) | Limited | P2 |
| Escalations | Yes | No dedicated page | Yes | **Missing in React** |
| Powerups | Yes | No page | Yes | **Missing in React** |
| Season points / reward claims | Yes | Partial via ops | Yes | Check coverage |
| Email ACL | Yes | No | Yes | **Missing in React** |
| Privacy request process | ? | No | Yes | **Missing in React** |
| Media intent | Yes | No | Yes | **Missing in React** |

\* = fixed in R1 this change.

---

## 2. R1 path alignment (P0)

### Auth — already matched Django

| Op | Path |
|----|------|
| Login | `POST /admin/auth/login` |
| Refresh | `POST /admin/auth/refresh` |
| Me | `GET /admin/auth/me` |
| Forgot / reset / validate | `/admin/auth/forgot-password`, `reset-password`, `validate-reset-token` |

React uses plain JSON + edge ops key (Vite/nginx). Django may use KMS secure-channel optionally.

### Users — fixed in R1

| Before (React bug) | After (Django/backend) |
|--------------------|------------------------|
| `?email=&limit=&offset=` | `?q=&page=&pageSize=` (+ `isBanned` / `status`) |
| Assumed list shape `total`/`offset` | Maps `totalItems`/`page`/`pageSize` → UI offset/limit |
| — | `PATCH /admin/users/{id}`, `GET /admin/player-lookup/resolve` |

### Moderation — fixed in R1

| Before (404) | After (Django/backend) |
|--------------|------------------------|
| `GET /admin/moderation/players/{id}` | `GET /admin/moderation/profile/{id}` + user detail + logs |
| `POST .../ban\|unban\|suspend\|warn\|note` | `POST /admin/moderation/set-status` with status int |
| | Ban=4, Restricted=3 (suspend), Suspected=2 (warn), Normal=1 (clear) |
| Logs | Unchanged: `GET /admin/moderation/logs` |

### Audit — already matched

| Op | Path |
|----|------|
| List | `GET /admin/audit/security` |
| Detail | `GET /admin/audit/security/{id}` |
| Geo | `POST /admin/audit/geo-lookup` |

---

## 3. Missing in React vs Django (product backlog)

**High (operators use daily in Django):**

1. Escalations console  
2. Powerups grant/state  
3. Full personalization player + rules editors (not just archetypes shell)  
4. Email ACL admin  
5. Storage path alignment (objects/prefixes vs browse/files)  
6. Setup readiness/history screens wired to `/admin/setup/*`  

**Medium:**

7. Season points / reward claims dedicated UI  
8. Media upload intent  
9. Mongo/MinIO deep diagnostics  
10. Saved views (port to API or localStorage)  

**Low / CLI:**

11. Full backend installer (prefer Setup CLI)

---

## 4. Files changed (R1)

- `Synaptix.OperatorDashboard.React/src/features/moderation/api.ts`  
- `Synaptix.OperatorDashboard.React/src/features/users/api.ts`  
- `Synaptix.OperatorDashboard.React/src/features/auth/api.ts` (docs only)  
- `Synaptix.OperatorDashboard.React/src/features/audit/api.ts` (docs only)  

---

## 5. R2 path alignment (P1) — 2026-07-13

Django clients are the source of truth; React adapters preserve existing UI types where possible.

| Module | Django client | React status |
|--------|---------------|--------------|
| **Notifications** | `admin_notifications_client` | Paths already correct; added `getNotificationHistory`; documented |
| **Store** | `admin_store_client` | Catalog/policies OK; **flash sales** fixed (`showAll` + `{ sales }` + create payload); `bulkResetStock` added |
| **Economy** | `admin_economy_client` | history/players/stats/txns OK; rollback uses `eventId`; added balance GET/PATCH + simulate |
| **Questions** | `admin_questions_client` | list/detail/approve/reject/stats OK; added create/update/delete/bulk import |
| **Storage** | `admin_storage_client` | Rewired browse → `prefixes` + `objects`; upload → `upload-proxy`; removed fake browse/files routes |
| **Personalization** | `admin_personalization_client` | archetypes/performance/rules/summary/player ops; engines/controls **adapted** from performance+rules |

### Storage UI notes
- Root lists allowed **prefixes** (policy), not free-form folders.  
- Upload posts multipart to `/admin/storage/upload-proxy`.  
- Delete/rename/move/quota/cleanup APIs are **not** on backend — clear errors.

### Personalization UI notes
- Archetypes = aggregate counts (not CRUD).  
- “Engines” tab = recommendation **performance** rows.  
- “Controls” tab = **rules** (`isEnabled`).  
- Player recalculate/reset require a **player GUID**.

### Files changed (R2)
- `features/notifications/api.ts`  
- `features/store/api.ts`  
- `features/economy/api.ts`  
- `features/content/api.ts`  
- `features/storage/api.ts`  
- `features/personalization/api.ts`  

---

## 6. R3 — Installer + diagnostics gated (default off)

- Flags: `src/lib/operator-feature-flags.ts`  
  - `VITE_ENABLE_INSTALLER` / `VITE_ENABLE_DIAGNOSTICS` (default false)  
  - Dev override: `localStorage OP_ENABLE_INSTALLER` / `OP_ENABLE_DIAGNOSTICS`  
- Sidebar hides Setup + Diagnostics when off  
- Deep links show **unavailable** pages (CLI / health alternatives)  
- Full UIs only when flags on  

## 7. Smoke tests

```bash
cd Synaptix.OperatorDashboard.React

# Path contract smoke (no network) — preferred local gate
npx vitest run src/lib/admin-api-paths.smoke.spec.ts src/lib/operator-feature-flags.spec.ts --environment node

# Playwright operator smoke (needs app + browsers)
npm run dev   # separate terminal
npx playwright test e2e/operator-r1-r2-smoke.spec.ts
```

Manual checklist (mock mode or live API + ops proxy):

1. Login → Dashboard  
2. Users list / detail ban  
3. Moderation logs + player profile actions  
4. Audit security  
5. Notifications hub  
6. Store catalog + flash sales  
7. Economy player  
8. Questions queue  
9. Storage (prefixes)  
10. Personalization tabs  
11. `/settings/setup` and `/diagnostics` → unavailable copy  
12. Sidebar has no Setup/Diagnostics links  

Regenerate admin path inventory:

```bash
make react-route-inventory
```
