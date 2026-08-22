# VenueAxe All-in-One Development Launcher
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  VenueAxe Development Environment Start" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Start PostgreSQL 17 Docker Container
Write-Host "[1/3] Starting PostgreSQL 17 database..." -ForegroundColor Yellow
docker compose up -d

# 2. Launch Backend in a new PowerShell window
$BackendScript = Join-Path $PSScriptRoot "start-backend.ps1"
Write-Host "[2/3] Spawning Backend Server (http://localhost:5280)..." -ForegroundColor Green
Start-Process pwsh -ArgumentList "-NoExit", "-File", "`"$BackendScript`""

# 3. Launch Frontend in a new PowerShell window
$FrontendScript = Join-Path $PSScriptRoot "start-frontend.ps1"
Write-Host "[3/3] Spawning Frontend Server (http://localhost:5173)..." -ForegroundColor Green
Start-Process pwsh -ArgumentList "-NoExit", "-File", "`"$FrontendScript`""

Write-Host ""
Write-Host "All services launched successfully!" -ForegroundColor Cyan
Write-Host "  - Frontend Portal: http://localhost:5173" -ForegroundColor White
Write-Host "  - Venue Admin:     http://localhost:5173/admin" -ForegroundColor White
Write-Host "  - Tablet Console:  http://localhost:5173/tablet" -ForegroundColor White
Write-Host "  - TV Screen:       http://localhost:5173/screen" -ForegroundColor White
Write-Host "  - Backend API:     http://localhost:5280/swagger" -ForegroundColor White
Write-Host "=========================================" -ForegroundColor Cyan
