# syntax=docker/dockerfile:1
# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-level build configuration first for better layer caching.
COPY global.json Directory.Build.props Directory.Packages.props nuget.config ./
COPY EnterpriseAIAgent.sln ./

# Copy project files and restore.
COPY src/ ./src/
COPY tests/ ./tests/
RUN dotnet restore src/Enterprise.Agent.Api/Enterprise.Agent.Api.csproj

# Publish the API (Release, framework-dependent).
RUN dotnet publish src/Enterprise.Agent.Api/Enterprise.Agent.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as a non-root user.
RUN adduser --disabled-password --gecos "" appuser
COPY --from=build /app/publish ./
RUN mkdir -p /app/logs && chown -R appuser /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_TieredPGO=1

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD ["dotnet", "--info"]

ENTRYPOINT ["dotnet", "Enterprise.Agent.Api.dll"]
