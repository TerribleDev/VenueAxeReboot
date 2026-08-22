# Start VenueAxe PostgreSQL and .NET 10 Web API
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Starting VenueAxe Backend & Database" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. Start PostgreSQL 17 Docker Container
Write-Host "[1/2] Ensuring PostgreSQL 17 container is running..." -ForegroundColor Yellow
docker compose up -d

# 2. Launch .NET 10 Web API on http://localhost:5280
Write-Host "[2/2] Launching VenueAxe.Web on http://localhost:5280..." -ForegroundColor Green
Write-Host "API Swagger UI: http://localhost:5280/swagger" -ForegroundColor Gray
Write-Host "SignalR Hub:    http://localhost:5280/hubs/lane" -ForegroundColor Gray
Write-Host "Press Ctrl+C to stop the backend server." -ForegroundColor DarkGray
Write-Host ""

dotnet run --project src/backend/VenueAxe.Web/VenueAxe.Web.csproj --urls http://localhost:5280
