FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

# В dev-режиме Rider/VS монтирует UserSecrets и NuGet к /home/app.
# Поэтому используем встроенного non-root пользователя `app` из базовых образов .NET.
USER app

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

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
COPY ["src/AuthService/AuthService.Presentation/AuthService.Presentation.csproj", "src/AuthService/AuthService.Presentation/"]
COPY ["src/AuthService/AuthService.WebApi/AuthService.WebApi.csproj", "src/AuthService/AuthService.WebApi/"]

RUN echo "CACHE_BUST=${CACHE_BUST}" > /dev/null \
    && dotnet restore "src/AuthService/AuthService.WebApi/AuthService.WebApi.csproj"

COPY . .
WORKDIR "/src/src/AuthService/AuthService.WebApi"
RUN dotnet build "AuthService.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AuthService.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.WebApi.dll"]