# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EnvAutoUpdater/EnvAutoUpdater.csproj", "EnvAutoUpdater/"]
RUN dotnet restore "./EnvAutoUpdater/EnvAutoUpdater.csproj"
COPY . .
WORKDIR "/src/EnvAutoUpdater"
RUN dotnet build "./EnvAutoUpdater.csproj" -c $BUILD_CONFIGURATION -o /app/build

RUN apk add --no-cache tzdata

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EnvAutoUpdater.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /usr/share/zoneinfo /usr/share/zoneinfo
ENTRYPOINT ["dotnet", "EnvAutoUpdater.dll"]