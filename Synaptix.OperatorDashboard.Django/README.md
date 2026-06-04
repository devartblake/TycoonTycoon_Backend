# Synaptix Operator Dashboard (Django)

This is the canonical Synaptix Operator Dashboard. The legacy `Synaptix.OperatorDashboard` Blazor project remains a rollback/comparison target only.

It acts as a UI frontend/BFF layer for:

- **.NET API** (`Synaptix.Backend.Api`)
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
`Synaptix.Setup`, `Synaptix.MigrationService`, PostgreSQL, MinIO, and the sidecar wired the same way as dev/staging:

```bash
dotnet run --project Synaptix.Setup -- init-local
dotnet run --project Synaptix.Setup -- validate --local
docker compose --env-file docker/.env -f docker/compose.yml up -d --build
```

Do not copy `docker/.env.example` directly as a runnable credentials file. `init-local` generates
the required local secrets and writes the generated super-admin credentials under `.local/bootstrap/`.

Startup order is intentional: infrastructure starts first, `setup` provisions services and uploads
bundled seeds, `migration` applies/seeds application data and validates dashboard readiness,
`backend-api` starts after migration completion, and
`operator-dashboard` starts after the backend is healthy.

Preview URL and login:

- URL: `http://localhost:8200/login`
- Email/password: use the generated credentials recorded by `Synaptix.Setup init-local`

See [`../docs/operator-dashboard/OPERATOR_DASHBOARD_AUTHENTICATED_PREVIEW.md`](../docs/operator-dashboard/OPERATOR_DASHBOARD_AUTHENTICATED_PREVIEW.md)
for the verification workflow and common failure checks.

## Authentication

The dashboard now uses session-based operator login:

- `GET/POST /login` for admin auth against `DOTNET_API_BASE_URL/admin/auth/login`
- `GET /logout` to clear operator session
- Protected routes (`/`, `/api/operator/health`, `/api/operator/users`) require a valid operator session
- Session middleware auto-attempts refresh via `/admin/auth/refresh` when access token is near expiry
- API routes enforce permission checks from the operator profile (`users:read`, `users:write`)

### Development login

Run `Synaptix.Setup init-local` before starting Compose. It generates the local admin credentials and
writes them to the ignored `.local/bootstrap/` directory. Smoke Compose uses its dedicated smoke-test
account; do not reuse smoke credentials outside the smoke workflow.

## Basic verification

```bash
python3 -m py_compile manage.py operator_dashboard/settings.py operator_dashboard/urls.py dashboard/views.py dashboard/services/api_clients.py
python manage.py test dashboard.tests
```

## API endpoints

- `/healthz` - container health endpoint for probes
- `/api/operator/health` - aggregated upstream service status JSON payload (`.NET`, `FastAPI`, `MinIO`)
- `/api/operator/setup/{status|readiness|services|seeds|validation|history}` - sanitized read-only setup diagnostics (requires `setup:read`)
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

Read-only setup pages are available at `/settings/setup` and its readiness, services, seeds, validation, and history subpages. Provisioning and mutations remain `Synaptix.Setup` CLI-only.

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
