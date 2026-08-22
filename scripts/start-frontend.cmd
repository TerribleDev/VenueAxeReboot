@echo off
echo ========================================
echo   Starting VenueAxe SvelteKit Frontend
echo ========================================

cd /d "%~dp0..\src\frontend"

echo Launching Vite Dev Server...
echo Frontend URL: http://localhost:5173
echo Press Ctrl+C to stop the frontend dev server.
echo.

pnpm dev
pause
