FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base

# В dev-режиме Rider/VS монтирует UserSecrets и NuGet к /home/app.
# Поэтому используем встроенного non-root пользователя `app` из базовых образов .NET.
USER app

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG CACHE_BUST

WORKDIR /src

# Копируем solution и *.csproj для корректного restore всех ProjectReference и лучшего кеширования слоёв.
COPY ["MicroserviceArchitecture.sln", "."]
COPY ["src/SharedKernel/SharedKernel.csproj", "src/SharedKernel/"]
COPY ["src/AuthService/AuthService.Domain/AuthService.Domain.csproj", "src/AuthService/AuthService.Domain/"]
COPY ["src/AuthService/AuthService.Contracts/AuthService.Contracts.csproj", "src/AuthService/AuthService.Contracts/"]
COPY ["src/AuthService/AuthService.Application/AuthService.Application.csproj", "src/AuthService/AuthService.Application/"]
COPY ["src/AuthService/AuthService.Infrastructure.Postgres/AuthService.Infrastructure.Postgres.csproj", "src/AuthService/AuthService.Infrastructure.Postgres/"]
COPY ["src/Jobs/AuthService.MigrationsJob/AuthService.MigrationsJob.csproj", "src/Jobs/AuthService.MigrationsJob/"]

RUN echo "CACHE_BUST=${CACHE_BUST}" > /dev/null \
    && dotnet restore "src/Jobs/AuthService.MigrationsJob/AuthService.MigrationsJob.csproj"

COPY . .
WORKDIR "/src/src/Jobs/AuthService.MigrationsJob"
RUN dotnet build "AuthService.MigrationsJob.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AuthService.MigrationsJob.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.MigrationsJob.dll"]
