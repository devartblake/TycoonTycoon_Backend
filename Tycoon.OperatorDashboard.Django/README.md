# Tycoon Operator Dashboard (Django)

This is the Django-based Operator Dashboard replacement target for the legacy `Tycoon.OperatorDashboard` Blazor project.

It acts as a UI frontend/BFF layer for:

- **.NET API** (`Tycoon.Backend.Api`)
- **FastAPI sidecar** (`Tycoon.Sidecar`)

## What this includes

- Django project + `dashboard` app
- Health cards for both upstream APIs
- Container-ready `/healthz` endpoint
- Environment-based endpoint configuration
- Service layer (`dashboard/services/api_clients.py`) for future proxy/API aggregation

## Local quick start

```bash
cd Tycoon.OperatorDashboard.Django
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
cp .env.example .env
python manage.py migrate
python manage.py runserver 0.0.0.0:8300
```

Open http://localhost:8300.

## Docker

The main compose service now builds this dashboard with `docker/Dockerfile.dashboard-django` and serves it on container port `8200`.

## Authentication

The dashboard now uses session-based operator login:

- `GET/POST /login` for admin auth against `DOTNET_API_BASE_URL/admin/auth/login`
- `GET /logout` to clear operator session
- Protected routes (`/`, `/api/operator/health`, `/api/operator/users`) require a valid operator session
- Session middleware auto-attempts refresh via `/admin/auth/refresh` when access token is near expiry

## Basic verification

```bash
python3 -m py_compile manage.py operator_dashboard/settings.py operator_dashboard/urls.py dashboard/views.py dashboard/services/api_clients.py
python manage.py test dashboard.tests
```

## API endpoints

- `/healthz` - container health endpoint for probes
- `/api/operator/health` - aggregated upstream service status JSON payload (`.NET`, `FastAPI`, `MinIO`)
- `/api/operator/users` - authenticated BFF pass-through for admin users list

## Configuration

| Variable | Purpose | Default |
| --- | --- | --- |
| `DOTNET_API_BASE_URL` | Base URL for ASP.NET API | `http://localhost:5000` |
| `FASTAPI_BASE_URL` | Base URL for FastAPI sidecar | `http://localhost:8100` |
| `MINIO_BASE_URL` | Base URL for MinIO | `http://localhost:9000` |
| `API_REQUEST_TIMEOUT_SECONDS` | Timeout for status checks | `5` |
| `ADMIN_OPS_KEY` | Optional value for `X-Admin-Ops-Key` on admin auth calls | `` |

When running inside Docker Compose, these are overridden to:

- `DOTNET_API_BASE_URL=http://backend-api:5000`
- `FASTAPI_BASE_URL=http://sidecar:8100`
- `MINIO_BASE_URL=http://minio:9000`
