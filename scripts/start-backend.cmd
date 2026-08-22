@echo off
echo ========================================
echo   Starting VenueAxe Backend ^& Database
echo ========================================

echo [1/2] Ensuring PostgreSQL 17 container is running...
docker compose up -d

echo [2/2] Launching VenueAxe.Web on http://localhost:5280...
echo API Swagger UI: http://localhost:5280/swagger
echo SignalR Hub:    http://localhost:5280/hubs/lane
echo Press Ctrl+C to stop the backend server.
echo.

dotnet run --project src\backend\VenueAxe.Web\VenueAxe.Web.csproj --urls http://localhost:5280
pause
