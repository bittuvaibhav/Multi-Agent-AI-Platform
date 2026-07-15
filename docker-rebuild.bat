@echo off
REM Stops the stack, removes the app images (api/ui) so they get rebuilt from
REM scratch, then rebuilds and starts everything. Database volumes are left
REM intact -- your SQL Server/Postgres/Redis data is not touched.
setlocal

echo Stopping containers...
docker compose down

echo Removing app images (enterprise-agent-api, enterprise-agent-ui)...
docker rmi -f enterprise-agent-api:latest enterprise-agent-ui:latest 2>nul

echo Rebuilding and starting the stack...
docker compose up --build -d

echo Done. Tail logs with: docker compose logs -f api
endlocal
