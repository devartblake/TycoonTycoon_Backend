# Tycoon Operator Dashboard (Django)

This is the Django-based Operator Dashboard replacement target for the legacy `Tycoon.OperatorDashboard` Blazor project.

It acts as a UI frontend/BFF layer for:

- **.NET API** (`Tycoon.Backend.Api`)
- **FastAPI sidecar** (`Synaptix.Sidecar`)

## What this includes

- Django project + `dashboard` app
- Health cards for both upstream APIs
- Container-ready `/healthz` endpoint
- Environment-based endpoint configuration
- Service layer (`dashboard/services/api_clients.py`) for future proxy/API aggregation

## Local quick start

```bash
cd Synaptix.OperatorDashboard.Django
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
cp .env.example .env
python manage.py migrate
python manage.py runserver 0.0.0.0:8300
```

Open http://localhost:8300 only for unauthenticated template/static checks such as `/login` and `/healthz`.
Authenticated dashboard pages still require a real `.NET` backend admin auth flow, a matching
`ADMIN_OPS_KEY`, and a seeded admin account. Use the Docker-backed preview flow below for normal
dashboard review.

## Docker

The main compose service now builds this dashboard with `docker/Dockerfile.dashboard-django` and serves it on container port `8200`.

### Authenticated local preview

Use Docker Compose for authenticated dashboard review. This keeps Django, the `.NET` API,
`Tycoon.MigrationService`, PostgreSQL, MinIO, and the sidecar wired the same way as dev/staging:

```bash
cp docker/.env.example docker/.env   # only if docker/.env does not already exist
docker compose -f docker/compose.yml up -d --build
```

The required local values are:

- `ADMIN_OPS_KEY=CHANGE_ME_IN_PRODUCTION`
- `ADMIN_AUTH_ALLOW_TRUSTED_BFF_PLAIN_JSON=true`
- `ADMIN_AUTH_TRANSPORT=auto`
- `SUPER_ADMIN_EMAIL=admin@tycoon.local`
- `SUPER_ADMIN_PASSWORD=ChangeMe123!`

Startup order is intentional: infrastructure starts first, `migration` applies/seeds data and
validates dashboard readiness, `backend-api` starts after migration completion, and
`operator-dashboard` starts after the backend is healthy.

Preview URL and login:

- URL: `http://localhost:8200/login`
- Email: `admin@tycoon.local`
- Password: `ChangeMe123!`

See [`../docs/OPERATOR_DASHBOARD_AUTHENTICATED_PREVIEW.md`](../docs/OPERATOR_DASHBOARD_AUTHENTICATED_PREVIEW.md)
for the verification workflow and common failure checks.

## Authentication

The dashboard now uses session-based operator login:

- `GET/POST /login` for admin auth against `DOTNET_API_BASE_URL/admin/auth/login`
- `GET /logout` to clear operator session
- Protected routes (`/`, `/api/operator/health`, `/api/operator/users`) require a valid operator session
- Session middleware auto-attempts refresh via `/admin/auth/refresh` when access token is near expiry
- API routes enforce permission checks from the operator profile (`users:read`, `users:write`)

### Development login

When running Docker Compose from `docker/.env.example` values after the migration service seeds the super-admin account:

- URL: `http://localhost:8200/login`
- Email: `admin@tycoon.local`
- Password: `ChangeMe123!`
- Required matching ops key: `ADMIN_OPS_KEY=CHANGE_ME_IN_PRODUCTION`

Smoke compose seeds `smoke-admin@synaptix.local` / `SmokeTest123!` instead. If `docker/.env` has not been created from `docker/.env.example`, compose defaults may leave the super-admin seed blank and no dev admin account will be created.

## Basic verification

```bash
python3 -m py_compile manage.py operator_dashboard/settings.py operator_dashboard/urls.py dashboard/views.py dashboard/services/api_clients.py
python manage.py test dashboard.tests
```

## API endpoints

- `/healthz` - container health endpoint for probes
- `/api/operator/health` - aggregated upstream service status JSON payload (`.NET`, `FastAPI`, `MinIO`)
- `/api/operator/audit/security` - security audit history endpoint (requires `events:read`)
- `/api/operator/moderation/logs` - moderation log list endpoint (requires `events:read`)
- `/api/operator/moderation/profile/{playerId}` - moderation profile endpoint (requires `events:read`)
- `/api/operator/moderation/set-status` - moderation status action endpoint (requires `events:write`)
- `/api/operator/media/intent` - media upload-intent endpoint (requires `questions:write`)
- `/api/operator/minio/diagnostics` - MinIO diagnostics endpoint (requires `users:read`)
- `/api/operator/users` - authenticated users list endpoint (requires `users:read`)
- `/api/operator/users/{userId}` - user detail endpoint (requires `users:read`)
- `/api/operator/users/{userId}/activity` - user activity endpoint (requires `users:read`)
- `/api/operator/users/{userId}/update` - user update endpoint (requires `users:write`)
- `/api/operator/users/{userId}/ban` - ban action endpoint (requires `users:write`)
- `/api/operator/users/{userId}/unban` - unban action endpoint (requires `users:write`)

## Configuration

| Variable | Purpose | Default |
| --- | --- | --- |
| `DOTNET_API_BASE_URL` | Base URL for ASP.NET API | `http://localhost:5000` |
| `FASTAPI_BASE_URL` | Base URL for FastAPI sidecar | `http://localhost:8100` |
| `MINIO_BASE_URL` | Base URL for MinIO | `http://localhost:9000` |
| `API_REQUEST_TIMEOUT_SECONDS` | Timeout for status checks | `5` |
| `ADMIN_OPS_HEADER` | Optional header name for admin auth ops key | `X-Admin-Ops-Key` |
| `ADMIN_OPS_KEY` | Optional ops key value for admin auth calls (fallbacks also support `AdminOps__Key`) | `` |

When running inside Docker Compose, these are overridden to:

- `DOTNET_API_BASE_URL=http://backend-api:5000`
- `FASTAPI_BASE_URL=http://sidecar:8100`
- `MINIO_BASE_URL=http://minio:9000`

## Current Status (May 12, 2026)

- Django remains the default operator dashboard service in Compose.
- Admin auth supports trusted internal plain JSON for dev plus secure-channel transport hooks for production.
- Django code-level parity is complete, including personalization and player stock support.
- Staging parallel-run sign-off, pending EF migration apply, and Blazor decommission remain external cutover gates.
