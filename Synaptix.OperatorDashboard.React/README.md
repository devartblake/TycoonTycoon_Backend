# Synaptix Operator Dashboard (React)

Admin SPA for the Synaptix backend. All data comes from the backend's `/admin/*`
API, which is protected by **three** gates — understanding them explains most
"blank page / nothing loads" situations:

1. **Ops key** — every `/admin/*` route (including login) requires the
   `X-Admin-Ops-Key` header matching the backend's `AdminOps:Key`. The key must
   never ship in browser JS, so a proxy injects it:
   - **Dev**: the Vite dev server proxy (`vite.config.ts`) forwards `/admin` to
     the API and adds the header (`ADMIN_OPS_KEY` env, default
     `dev-admin-ops-key` — the backend's Development default).
   - **Docker**: nginx in the container proxies `/admin` to `API_UPSTREAM` and
     adds the header from the `ADMIN_OPS_KEY` container env
     (`docker/nginx-react.conf.template`).
   Consequently `VITE_API_BASE_URL` must stay **empty** (same-origin) in both
   setups; setting it to the API's address bypasses the proxy and every call
   401s with "Missing admin ops key".
2. **Admin JWT** — `/admin/auth/login` must be called with an admin account;
   the token then carries `role=admin`, `aud=admin-app` and admin scopes.
3. **Email allowlist (fail-closed)** — the JWT's email must have an `Allow`
   entry in the backend's `AdminEmailAcls` table. An **empty table means nobody
   gets in.** Seed the super admin (which also creates the allowlist entry) with
   the setup CLI: `Synaptix.Setup` (`SuperAdminSetupTask`, uses
   `SUPER_ADMIN_EMAIL` / `SUPER_ADMIN_PASSWORD`).

## Run against the real backend (dev)

```bash
# 1. Start the backend in the Development environment (AdminOps:Key = dev-admin-ops-key)
# 2. Seed the super admin + email allowlist once (Synaptix.Setup CLI)
# 3. Start the dashboard — the proxy injects the ops key:
npm install
npm run dev            # http://localhost:3000
# Non-default backend or key:
# API_PROXY_TARGET=http://localhost:5000 ADMIN_OPS_KEY=... npm run dev
```

## Run with mock data (no backend)

Click **Enable Mock Mode** on the login page (or set `localStorage.MOCK_API_MODE = 'true'`).
Any email/password logs in; all data is simulated.

## Scripts

- `npm run dev` — Vite dev server with the ops-key proxy
- `npm run type-check` / `npm run lint` / `npm run test:unit`
- `npm run build` — production build (served by nginx in Docker)

## Docker

`docker/Dockerfile.dashboard-react` builds the SPA and serves it with nginx on
port 8300. The compose service (`operator-dashboard-react`) wires
`API_UPSTREAM` and `ADMIN_OPS_KEY` — the same `ADMIN_OPS_KEY` value the
`backend-api` service uses for `AdminOps__Key`.
