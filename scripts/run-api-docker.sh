#!/usr/bin/env bash
set -euo pipefail

export ASPNETCORE_ENVIRONMENT=Docker
exec docker compose -f docker/compose.yml up -d --build backend-api
