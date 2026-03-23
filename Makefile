.PHONY: migrate migrate-reset migrate-status migrate-local migrate-local-reset build up down logs

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
