#!/usr/bin/env bash

# Exit script cleanly when stopped (Ctrl+C)
trap 'echo "\nStopping CarePoint processes..."; kill 0' EXIT INT TERM

echo "=========================================="
echo "🏥 CarePoint - Fullstack Runner"
echo "=========================================="

# 1. Start SQL Server via Docker if docker-compose exists
if command -v docker-compose &> /dev/null || docker compose version &> /dev/null; then
    echo "📦 Ensuring SQL Server database container is running..."
    docker compose up -d sqlserver 2>/dev/null || docker-compose up -d sqlserver 2>/dev/null
fi

echo ""
echo "⚙️  Starting Backend API (.NET 8)..."
dotnet run --project backend/CarePoint.API/CarePoint.API.csproj --launch-profile http &
BACKEND_PID=$!

echo "🎨 Starting Frontend (Vite/React)..."
npm --prefix frontend run dev &
FRONTEND_PID=$!

echo ""
echo "✅ Both services are running!"
echo "   - Backend API:  http://localhost:5005"
echo "   - Frontend App: http://localhost:5173"
echo "Press Ctrl+C to stop all services."
echo "=========================================="

wait
