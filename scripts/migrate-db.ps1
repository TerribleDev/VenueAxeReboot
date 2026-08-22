# VenueAxe Database Migration Script
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  Applying VenueAxe Database Migrations" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Ensure PostgreSQL is running
Write-Host "[1/2] Verifying PostgreSQL 17 container is up..." -ForegroundColor Yellow
docker compose up -d

# 2. Run EF Core database update
Write-Host "[2/2] Running EF Core database migrations..." -ForegroundColor Green
$DataProject = Join-Path $PSScriptRoot "..\src\backend\VenueAxe.Data\VenueAxe.Data.csproj"
$WebProject = Join-Path $PSScriptRoot "..\src\backend\VenueAxe.Web\VenueAxe.Web.csproj"

dotnet ef database update --project $DataProject --startup-project $WebProject

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Database migration applied successfully!" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "Migration failed. Please check error logs above." -ForegroundColor Red
}
