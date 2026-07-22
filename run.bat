@echo off
echo ==========================================
echo CarePoint - Fullstack Runner (Windows)
echo ==========================================

echo Ensuring SQL Server database container is running...
docker-compose up -d sqlserver

echo Starting Backend API (.NET 8)...
start "CarePoint Backend API" cmd /k "dotnet run --project backend\CarePoint.API\CarePoint.API.csproj --launch-profile http"

echo Starting Frontend (Vite/React)...
start "CarePoint Frontend" cmd /k "cd frontend && npm run dev"

echo ==========================================
echo Both services started in separate terminals.
echo ==========================================
