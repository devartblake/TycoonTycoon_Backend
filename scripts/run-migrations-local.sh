#!/usr/bin/env bash
set -euo pipefail

export ASPNETCORE_ENVIRONMENT=Local
exec dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
