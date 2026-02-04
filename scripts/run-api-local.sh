#!/usr/bin/env bash
set -euo pipefail

export ASPNETCORE_ENVIRONMENT=Local
exec dotnet run --project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj
