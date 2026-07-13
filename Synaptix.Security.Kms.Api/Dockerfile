FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# curl is needed for the compose healthcheck; not present in the slim aspnet image
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 5050

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy central package management files first (infrequently changing — cache layer)
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

# Copy all project files referenced by the API (restore layer)
COPY ["Synaptix.Security.Kms.Api/Synaptix.Security.Kms.Api.csproj",             "Synaptix.Security.Kms.Api/"]
COPY ["Synaptix.Security.Kms.Application/Synaptix.Security.Kms.Application.csproj", "Synaptix.Security.Kms.Application/"]
COPY ["Synaptix.Security.Kms.Infrastructure/Synaptix.Security.Kms.Infrastructure.csproj", "Synaptix.Security.Kms.Infrastructure/"]
COPY ["Synaptix.Security.Kms.Contracts/Synaptix.Security.Kms.Contracts.csproj",   "Synaptix.Security.Kms.Contracts/"]
COPY ["Synaptix.ServiceDefaults/Synaptix.ServiceDefaults.csproj",                  "Synaptix.ServiceDefaults/"]

RUN dotnet restore "Synaptix.Security.Kms.Api/Synaptix.Security.Kms.Api.csproj"

# Copy source and build
COPY . .
WORKDIR "/src/Synaptix.Security.Kms.Api"
RUN dotnet build "Synaptix.Security.Kms.Api.csproj" -c $BUILD_CONFIGURATION --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Synaptix.Security.Kms.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# In-image healthcheck (compose defines its own; this covers standalone `docker run`)
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5050/health || exit 1

ENTRYPOINT ["dotnet", "Synaptix.Security.Kms.Api.dll"]
