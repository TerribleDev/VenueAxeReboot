# Start VenueAxe SvelteKit 2 (Svelte 5) Development Server
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Starting VenueAxe SvelteKit Frontend" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$FrontendPath = Join-Path $PSScriptRoot "..\src\frontend"
Push-Location $FrontendPath

Write-Host "Launching Vite Dev Server..." -ForegroundColor Green
Write-Host "Frontend URL: http://localhost:5173" -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop the frontend dev server." -ForegroundColor DarkGray
Write-Host ""

pnpm dev

Pop-Location
