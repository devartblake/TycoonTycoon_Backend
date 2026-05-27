#!/usr/bin/env bash
set -euo pipefail

export ASPNETCORE_ENVIRONMENT=Local
exec dotnet run --project Synaptix.Backend.Api/Synaptix.Backend.Api.csproj
