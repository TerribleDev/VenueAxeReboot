@echo off
echo =========================================
echo   Applying VenueAxe Database Migrations
echo =========================================

echo [1/2] Verifying PostgreSQL 17 container is up...
docker compose up -d

echo [2/2] Running EF Core database migrations...
dotnet ef database update --project "%~dp0..\src\backend\VenueAxe.Data\VenueAxe.Data.csproj" --startup-project "%~dp0..\src\backend\VenueAxe.Web\VenueAxe.Web.csproj"

if %ERRORLEVEL% equ 0 (
    echo.
    echo Database migration applied successfully!
) else (
    echo.
    echo Migration failed. Please check error logs above.
)
pause
