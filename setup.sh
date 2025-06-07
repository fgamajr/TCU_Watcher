#!/usr/bin/env bash
set -euo pipefail

# ========= 1. BUILD =========
echo "🔨  Building solution…"
dotnet build -c Debug

# ========= 2. SEED / UPDATE WINDOW =========
echo "🌱  Seeding dev MonitoringWindow covering NOW…"
dotnet run --project tools/SeedMonitoringWindow.csproj

# ========= 3. START API (background) =========
echo "🚀  Starting API with SimTimeProvider…"
DOTNET_ENVIRONMENT=Development \
dotnet run --project TCUWatcher.API \
  --urls "http://localhost:5092" >/dev/null 2>&1 &
API_PID=$!
trap "kill $API_PID" EXIT

# Aguarda ficar de pé
until curl -s http://localhost:5092/healthz >/dev/null; do sleep 1; done
echo "✅  API is up (PID=$API_PID)."

# ========= 4. TIME-LAPSE SIMULATION =========
echo "⏩  Simulating 24 h in fast-forward…"
START=$(date -u -d "-12 hours" '+%Y-%m-%dT%H:%M:%SZ')

for h in $(seq 0 24); do
  SIM=$(date -u -d "$START +$h hours" '+%Y-%m-%dT%H:%M:%SZ')
  RESP=$(curl -s -H "X-Simulated-Time: $SIM" \
               http://localhost:5092/monitoring-window/current)
  echo -e "🕒 $SIM  ➜  $RESP"
  sleep 0.05
done

echo "🏁  Simulation finished."
