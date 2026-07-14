# React Operator Dashboard ↔ Backend Admin Route Gap Inventory

**Program:** Track C / H3b — [`docs/status/BCE_EXECUTION_PLAN.md`](../status/BCE_EXECUTION_PLAN.md)  
**Dashboard direction:** **React is canonical** — [`OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md`](OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md)  
**Generated approach:** static path extraction (re-run anytime)  
**Date:** 2026-07-13 (priorities revised for React replacement of Django)

## How to regenerate

```bash
python scripts/inventory-react-admin-routes.py
python scripts/inventory-admin-backend-routes.py
python scripts/compare-react-admin-routes.py
# or: make react-route-inventory
```

Normalization: `{param}` / `{id:guid}` → `{id}`; trailing slashes and query strings stripped.

## Snapshot (2026-07-13)

| Metric | Count |
|--------|------:|
| React `/admin/*` path literals | 134 |
| Backend `/admin/*` maps (heuristic inventory) | 161 |
| Normalized matches | ~56 |
| **React-only (client path not found on backend inventory)** | **~78** |
| Backend-only (not referenced by React literals) | ~105 |

> **Interpretation (updated):** Low match rate is **launch-critical**. React is the **replacement** for Django, not a secondary metrics UI. Every React-only path is a likely **404** when pointed at `Synaptix.Backend.Api`. Resolve by (1) retargeting client paths to existing admin endpoints, (2) implementing missing backend routes, or (3) disabling the UI surface until ready.

## Priority for Alpha (React-first)

| Priority | React surface | Action |
|----------|---------------|--------|
| **P0** | Auth (`/admin/auth/login`, refresh, me) | Must match real backend auth; smoke in staging |
| **P0** | Users, moderation, audit | Align paths to `AdminUsers*`, `AdminModeration*`, `AdminAudit*` |
| **P0** | Health used by ops | Prefer API `/healthz`, `/health/ready`, `/metrics` — do not depend on fictional `/admin/diagnostics/*` unless implemented |
| **P1** | Notifications, store stock, questions, economy | Align React `api.ts` to existing `Admin*Endpoints` |
| **P1** | Storage browser | Map React storage paths → backend `/admin/storage/objects|prefixes|upload-*` |
| **P1** | Personalization | Map engines/controls naming → backend `rules` / `summary` / `player/{id}` |
| **P2** | Installer cluster | Implement as admin API **or** keep Setup CLI only and hide UI |
| **P2** | Diagnostics probes/logs | Implement backend **or** hide UI; do not block other modules |

## React-only clusters (gaps)

These appear in React source but **not** in the backend inventory (likely 404 if called against `Synaptix.Backend.Api`):

### Installer (full cluster) — P2
`/admin/installer/*` — status, start/pause/resume, bundles, logs, rollback, etc.  
**Recommendation:** Prefer Setup CLI for Alpha; feature-flag off installer UI until APIs exist.

### Diagnostics — P2 / P0 for health only
`/admin/diagnostics/logs|metrics|probes/*`  
**Recommendation:** Wire dashboard health to real `/health*` / metrics; implement diagnostics API only if product requires it.

### Storage browser (path shape mismatch) — P1
React: `/admin/storage/browse`, `/files`, `/folders`, `/upload`, …  
Backend inventory: `/admin/storage/objects`, `/prefixes`, `/upload-intent`, `/upload-proxy`, …  
**Recommendation:** Update React `storage/api.ts` to backend shapes (preferred) or add compatibility aliases on API.

### Config shape mismatch — P1
React: `/admin/config/feature-flags`, `/admin-acl`, `/system`, …  
Backend: `/admin/config` (+ related) — verify exact maps in `AdminConfigEndpoints`.  
**Recommendation:** Path-by-path alignment.

### Skills — P1
React: `/admin/skills`, `/seeds`, `/stats`  
Backend: `/admin/skills/seed` (and related)  
**Recommendation:** Rename client paths.

### Personalization naming — P1
React: `recommendation-engines`, `recommendation-controls`, `stats`  
Backend: `rules`, `summary`, `player/{id}`, `recommendations/performance`  
**Recommendation:** Align client to `AdminPersonalizationEndpoints`.

### Event queue — P1
React: `/admin/event-queue/stats`, `clear-failed`  
Backend: `/admin/event-queue/upload`, `reprocess`  
**Recommendation:** Align or hide.

### Moderation players — P0
React uses `/admin/moderation/players/{id}/ban|suspend|…`  
Backend inventory shows profile/set-status/escalation style routes — **verify** `AdminModerationEndpoints` and update client.

## Backend-only (React does not call)

Not automatically dead code — may still be needed as we port Django workflows:

- Experiments, learning modules, powerups, email-acl, privacy process  
- Extra anti-cheat party analytics  
- Season points / taxonomy / bulk question tools  
- Auth helpers (`/admin/auth/me`, refresh, password reset)  

**Action:** For each Django-only workflow operators still need, either wire React or accept feature cut for Alpha.

## Recommended engineering sequence (React replaces Django)

1. **P0 auth + users + moderation + audit** — operators can log in and act.  
2. **P1 notifications, store, questions, economy, storage, personalization** — daily ops.  
3. **Hide or flag** installer/diagnostics until APIs exist.  
4. **Staging parallel-run on React** (H1 evidence retargeted).  
5. **Decommission Django** after rollback window.

## Related

- Direction: [`OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md`](OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md)  
- Mobile/API parity: `RouteParityContractTests` (player `/api/v1/*`, not admin)  
- Inventory scripts: `scripts/inventory-*-routes.py`, `scripts/compare-react-admin-routes.py`
