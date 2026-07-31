#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PORT="${ANMOL_DRISTI_PORT:-8080}"

if ! xsp4 --help >/dev/null 2>&1; then
  "$ROOT/scripts/install-xsp.sh"
fi

cd "$ROOT/AnmolDristi"
export MONO_IOMAP=all

echo "Starting Anmol Dristi on http://0.0.0.0:${PORT}/login.aspx"
exec xsp4 --port "$PORT" --address 0.0.0.0 --nonstop
