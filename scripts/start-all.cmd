@echo off
echo =========================================
echo   VenueAxe Development Environment Start
echo =========================================

echo [1/3] Starting PostgreSQL 17 database...
docker compose up -d

echo [2/3] Spawning Backend Server (http://localhost:5280)...
start "VenueAxe Backend API (Port 5280)" cmd /k "%~dp0start-backend.cmd"

echo [3/3] Spawning Frontend Server (http://localhost:5173)...
start "VenueAxe Svelte Frontend (Port 5173)" cmd /k "%~dp0start-frontend.cmd"

echo.
echo All services launched in separate windows!
echo   - Frontend Portal: http://localhost:5173
echo   - Venue Admin:     http://localhost:5173/admin
echo   - Tablet Console:  http://localhost:5173/tablet
echo   - TV Screen:       http://localhost:5173/screen
echo   - Backend API:     http://localhost:5280/swagger
echo =========================================
