#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "Starting SQL Server container..."
sg docker -c "docker compose -f \"$ROOT/docker-compose.yml\" up -d"

"$ROOT/scripts/init-database.sh"
"$ROOT/scripts/configure-local-db.sh"

if ! xsp4 --help >/dev/null 2>&1; then
  "$ROOT/scripts/install-xsp.sh"
fi

echo ""
echo "Dev environment is ready."
echo "  SQL Server: localhost:1433 (sa / DevPassword123!, database AnmolDristiDev)"
echo "  Test user:  DEV001 / devpass"
echo ""
echo "Start the web app with: scripts/start-dev-server.sh"
