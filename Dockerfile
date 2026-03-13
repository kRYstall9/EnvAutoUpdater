FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .
RUN dotnet restore "EnvAutoUpdater.csproj"
RUN dotnet build "EnvAutoUpdater.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN apk add --no-cache tzdata
RUN dotnet publish "EnvAutoUpdater.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /usr/share/zoneinfo /usr/share/zoneinfo
ENTRYPOINT ["dotnet", "EnvAutoUpdater.dll"]