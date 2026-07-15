@echo off
REM Builds the api/ui images from the current source without starting or
REM restarting any containers.
setlocal

docker compose build

endlocal
