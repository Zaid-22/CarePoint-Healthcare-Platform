#!/usr/bin/env bash
set -euo pipefail

# Clean shutdown: kill background jobs on exit.
cleanup() {
    printf '\nStopping CarePoint processes...\n'
    kill 0 2>/dev/null || true
}
trap cleanup EXIT

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

printf '==========================================\n'
printf ' CarePoint - Fullstack Runner\n'
printf '==========================================\n'

# 1. Start SQL Server via Docker if docker-compose exists
if command -v docker-compose &> /dev/null || docker compose version &> /dev/null; then
    printf ' Ensuring SQL Server database container is running...\n'
    docker compose up -d sqlserver 2>/dev/null || docker-compose up -d sqlserver 2>/dev/null

    printf ' Waiting for SQL Server to become healthy...\n'
    readiness_attempt=0
    until [ "$(docker inspect --format='{{.State.Health.Status}}' carepoint-sqlserver 2>/dev/null)" = "healthy" ]; do
        readiness_attempt=$((readiness_attempt + 1))
        if [ "$readiness_attempt" -ge 60 ]; then
            printf ' SQL Server did not become healthy within two minutes.\n'
            exit 1
        fi
        sleep 2
    done
fi

# 2. Ensure frontend dependencies are installed
if [ ! -d frontend/node_modules ]; then
    printf ' Installing frontend dependencies...\n'
    npm --prefix frontend install
fi

printf '\n  Starting Backend API (.NET 10)...\n'
dotnet run --project backend/CarePoint.API/CarePoint.API.csproj --launch-profile http &
BACKEND_PID=$!

printf ' Starting Frontend (Vite/React)...\n'
npm --prefix frontend run dev &
FRONTEND_PID=$!

printf '\n Both services are running!\n'
printf '   - Backend API:  http://localhost:5005\n'
printf '   - Frontend App: http://localhost:5173\n'
printf 'Press Ctrl+C to stop all services.\n'
printf '==========================================\n'

wait
