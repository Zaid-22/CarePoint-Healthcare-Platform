#!/usr/bin/env bash

# Exit script cleanly when stopped (Ctrl+C)
trap 'echo "\nStopping CarePoint processes..."; kill 0' EXIT INT TERM

if [ -f .env ]; then
    set -a
    . ./.env
    set +a
fi

: "${MSSQL_SA_PASSWORD:?Copy .env.example to .env and set MSSQL_SA_PASSWORD.}"
: "${JWT_SECRET:?Copy .env.example to .env and set JWT_SECRET.}"
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=CarePointDb;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;"
export JwtSettings__Secret="${JWT_SECRET}"
export EmailSettings__FromAddress="${EMAIL_FROM_ADDRESS:-}"
export EmailSettings__FromName="${EMAIL_FROM_NAME:-CarePoint}"
export EmailSettings__SmtpHost="${SMTP_HOST:-}"
export EmailSettings__SmtpPort="${SMTP_PORT:-587}"
export EmailSettings__SmtpUser="${SMTP_USER:-}"
export EmailSettings__SmtpPassword="${SMTP_PASSWORD:-}"
export EmailSettings__UseSsl="${SMTP_USE_SSL:-true}"
export EmailSettings__PasswordResetUrl="${PASSWORD_RESET_URL:-http://localhost:5173/reset-password}"

echo "=========================================="
echo " CarePoint - Fullstack Runner"
echo "=========================================="

# 1. Start SQL Server via Docker if docker-compose exists
if command -v docker-compose &> /dev/null || docker compose version &> /dev/null; then
    echo " Ensuring SQL Server database container is running..."
    docker compose up -d sqlserver 2>/dev/null || docker-compose up -d sqlserver 2>/dev/null

    echo " Waiting for SQL Server to become healthy..."
    readiness_attempt=0
    until [ "$(docker inspect --format='{{.State.Health.Status}}' carepoint-sqlserver 2>/dev/null)" = "healthy" ]; do
        readiness_attempt=$((readiness_attempt + 1))
        if [ "$readiness_attempt" -ge 60 ]; then
            echo " SQL Server did not become healthy within two minutes."
            exit 1
        fi
        sleep 2
    done
fi

echo ""
echo "  Starting Backend API (.NET 10)..."
dotnet run --project backend/CarePoint.API/CarePoint.API.csproj --launch-profile http &
BACKEND_PID=$!

echo " Starting Frontend (Vite/React)..."
npm --prefix frontend run dev &
FRONTEND_PID=$!

echo ""
echo " Both services are running!"
echo "   - Backend API:  http://localhost:5005"
echo "   - Frontend App: http://localhost:5173"
echo "Press Ctrl+C to stop all services."
echo "=========================================="

wait
