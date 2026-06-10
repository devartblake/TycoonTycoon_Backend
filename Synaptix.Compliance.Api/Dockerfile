ARG BUILD_CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5070

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION
WORKDIR /src

COPY ["Synaptix.Compliance.Api/Synaptix.Compliance.Api.csproj", "Synaptix.Compliance.Api/"]
COPY ["Synaptix.Compliance.Application/Synaptix.Compliance.Application.csproj", "Synaptix.Compliance.Application/"]
COPY ["Synaptix.Compliance.Contracts/Synaptix.Compliance.Contracts.csproj", "Synaptix.Compliance.Contracts/"]
COPY ["Synaptix.Compliance.Infrastructure/Synaptix.Compliance.Infrastructure.csproj", "Synaptix.Compliance.Infrastructure/"]
COPY ["Synaptix.ServiceDefaults/Synaptix.ServiceDefaults.csproj", "Synaptix.ServiceDefaults/"]
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]

RUN dotnet restore "Synaptix.Compliance.Api/Synaptix.Compliance.Api.csproj"

COPY . .
RUN dotnet build "Synaptix.Compliance.Api/Synaptix.Compliance.Api.csproj" \
    -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "Synaptix.Compliance.Api/Synaptix.Compliance.Api.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Synaptix.Compliance.Api.dll"]
