# VenueAxe Add New Migration Script
param (
    [Parameter(Mandatory=$false, Position=0)]
    [string]$MigrationName
)

if (-not $MigrationName) {
    $MigrationName = Read-Host "Enter migration name (e.g. AddPaymentGateways)"
}

if (-not $MigrationName) {
    Write-Host "Migration name cannot be empty." -ForegroundColor Red
    exit 1
}

Write-Host "Adding migration: $MigrationName..." -ForegroundColor Cyan

$DataProject = Join-Path $PSScriptRoot "..\src\backend\VenueAxe.Data\VenueAxe.Data.csproj"
$WebProject = Join-Path $PSScriptRoot "..\src\backend\VenueAxe.Web\VenueAxe.Web.csproj"

dotnet ef migrations add $MigrationName --project $DataProject --startup-project $WebProject --output-dir Migrations
