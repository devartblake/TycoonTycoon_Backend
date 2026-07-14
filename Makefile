.PHONY: migrate migrate-reset migrate-status migrate-local migrate-local-reset bootstrap build up down logs smoke smoke-live smoke-routes dev dev-win happy-path react-route-inventory

# ── Local happy path (E3 / docs/setup/LOCAL_DEV_HAPPY_PATH.md) ───────────────

## One-command local bootstrap (Linux/macOS): secrets → compose stack
dev happy-path:
	@chmod +x ./scripts/bootstrap-local.sh ./scripts/bootstrap-stack.sh 2>/dev/null || true
	@./scripts/bootstrap-local.sh

## Windows PowerShell bootstrap
dev-win:
	@powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/bootstrap-local.ps1

## React admin path inventory vs backend (C / H3b)
react-route-inventory:
	@python scripts/compare-react-admin-routes.py

# ── Migration targets ────────────────────────────────────────────────────────

## Run EF migrations via Docker (default)
migrate:
	@./scripts/migrate.sh

## Drop database and re-run all migrations via Docker
migrate-reset:
	@./scripts/migrate.sh --reset

## Show applied migration history
migrate-status:
	@./scripts/migrate.sh --status

## Run EF migrations locally (requires dotnet + pg_isready)
migrate-local:
	@./scripts/migrate.sh --local

## Drop database and re-run all migrations locally
migrate-local-reset:
	@./scripts/migrate.sh --local --reset

# ── Docker Compose helpers ───────────────────────────────────────────────────

## Validate EF model snapshot, build images, then bring up the stack (runs setup + migration)
bootstrap:
	@chmod +x ./scripts/bootstrap-stack.sh
	@./scripts/bootstrap-stack.sh

## Build all Docker images
build:
	docker compose -f docker/compose.yml build

## Start all services
up:
	docker compose -f docker/compose.yml up -d

## Stop all services
down:
	docker compose -f docker/compose.yml down

## Tail logs for all services
logs:
	docker compose -f docker/compose.yml logs -f

## Validate EF schema for drift (CI-friendly)
validate-schema:
	@./scripts/validate-ef-schema.sh

# ── Smoke tests ──────────────────────────────────────────────────────────────

## Compose smoke test: start stack, verify operator login + BFF flows, tear down
smoke:
	@chmod +x ./scripts/compose-smoke.sh
	@./scripts/compose-smoke.sh

## Compose smoke test against already-running stack (skip start/stop)
smoke-live:
	@chmod +x ./scripts/compose-smoke.sh
	@STACK_RUNNING=true ./scripts/compose-smoke.sh

## Static route smoke test (no live services required)
smoke-routes:
	@chmod +x ./scripts/alpha-p0-smoke.sh
	@SMOKE_MODE=routes bash ./scripts/alpha-p0-smoke.sh
