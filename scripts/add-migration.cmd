@echo off
set /p MIGRATION_NAME="Enter migration name (e.g. AddPaymentGateways): "
if "%MIGRATION_NAME%"=="" (
    echo Migration name cannot be empty.
    pause
    exit /b 1
)

echo Adding migration: %MIGRATION_NAME%...
dotnet ef migrations add %MIGRATION_NAME% --project "%~dp0..\src\backend\VenueAxe.Data\VenueAxe.Data.csproj" --startup-project "%~dp0..\src\backend\VenueAxe.Web\VenueAxe.Web.csproj" --output-dir Migrations
pause
