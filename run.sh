#!/usr/bin/env bash

# Exit script cleanly when stopped (Ctrl+C)
trap 'echo "\nStopping CarePoint processes..."; kill 0' EXIT INT TERM

if [ -f .env ]; then
    set -a
    . ./.env
    set +a
fi

: "${MSSQL_SA_PASSWORD:?Create .env from .env.example and set MSSQL_SA_PASSWORD.}"
: "${JWT_SECRET:?Create .env from .env.example and set JWT_SECRET.}"
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=CarePointDb;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;"
export JwtSettings__Secret="${JWT_SECRET}"

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
