# Operator Dashboard — Deploy-Time Ops Checklist

Deploy-time values and steps required for the operator dashboard to function in
staging/production. The **Django** dashboard is the deployed operator dashboard
(served at `admin.${DOMAIN}` behind Traefik TLS); the React dashboard is
currently dev-only and not wired into the prod/staging compose.

Compose: base `docker/compose.yml` + `docker/compose.prod.yml` (or
`compose.staging.yml`). Required vars use `${VAR:?...}` and abort `compose up`
if unset.

## 1. Required environment values

Set these in the deploy environment / `docker/.env` (see `docker/.env.example`):

| Variable | Why | Notes |
|---|---|---|
| `ADMIN_OPS_KEY` | Gates every `/admin/*` route. **The backend and the dashboard must use the same value.** | Both `backend-api` and the dashboard now fail closed if unset (no more silent `CHANGE_ME`). Generate a strong random value (`Synaptix.Setup` can emit one). |
| `DOMAIN` | Traefik host routing + CORS + `DJANGO_ALLOWED_HOSTS`/CSRF. | e.g. `synaptixplay.com` → dashboard at `admin.synaptixplay.com`. |
| `SUPER_ADMIN_EMAIL` / `SUPER_ADMIN_PASSWORD` (`SUPER_ADMIN_HANDLE` optional) | Seeds the super admin **and** its `AdminEmailAcls` allowlist entry (see §2). | Read by `Synaptix.Setup` (`SuperAdmin:Email/Password/Handle` or `SUPER_ADMIN_*`). |
| DB / infra secrets | `POSTGRES_PASSWORD`, `MONGO_*`, `ELASTIC_PASSWORD`, `KMS_SERVICE_TOKEN`, Linode object-storage keys. | All `${VAR:?}` in prod. |
| `DASHBOARD_PROMETHEUS_URL` | System-metrics tiles. **Leave empty in prod/staging** (Prometheus is dev-profile only) → tiles show 0. | Point at a reachable Prometheus HTTP API only if one is deployed. |

GeoIP (audit IP map) uses ip-api.com by default (`GeoIp:IpApiBaseUrl`,
`GeoIp:CacheHours` in `appsettings.json`); no env value required. A
MaxMind/GeoLite2 resolver can replace `IGeoIpResolver` later.

## 2. Admin access gates (all three must be satisfied)

The dashboard's data comes from `/admin/*`, which is protected by three gates —
most "blank dashboard / 403" issues are one of these:

1. **Ops key** — `X-Admin-Ops-Key` must match `ADMIN_OPS_KEY` (§1). The Django
   dashboard injects it server-side; the React dev dashboard injects it via its
   proxy (dev only).
2. **Admin JWT** — log in with an admin account; the token carries `role=admin`,
   `aud=admin-app`, and admin scopes.
3. **Email allowlist (fail-closed)** — the JWT's email must have an `Allow` entry
   in the backend's `AdminEmailAcls` table. **An empty table blocks everyone.**

## 3. One-time seeding (`Synaptix.Setup`)

Run the setup CLI against the target environment before first login. Its
`SuperAdminSetupTask` creates the super admin **and** the `AdminEmailAcls` `Allow`
row from `SUPER_ADMIN_EMAIL`. Without this, gate #3 blocks all logins.

## 4. TLS

Traefik is the only public ingress (ports 80/443) and terminates TLS via the
`le` cert resolver; `api.${DOMAIN}` and `admin.${DOMAIN}` route to `backend-api`
and the dashboard on internal ports. No per-service TLS config needed.

## 5. Verification

1. `docker compose -f docker/compose.yml -f docker/compose.prod.yml config` —
   resolves with no `VAR is required` errors (confirms required vars are set).
2. After `up`: `curl -H "X-Admin-Ops-Key: $ADMIN_OPS_KEY" https://api.${DOMAIN}/admin/auth/login` (with valid admin creds) → 200; without the header → 401.
3. Log into `https://admin.${DOMAIN}` with the seeded super admin → dashboard
   loads real data (not blank / not 403).
4. `curl https://api.${DOMAIN}/healthz` → healthy; `/metrics` returns Prometheus
   text (used only if `DASHBOARD_PROMETHEUS_URL` is set).
