#!/usr/bin/env bash
# Patches Web.config and bin/AnmolDristi.dll.config for local SQL Server.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CONN='Data Source=127.0.0.1,1433; Initial Catalog=AnmolDristiDev; Persist Security Info=True; User ID=sa; Password=DevPassword123!; TrustServerCertificate=True; Max Pool Size=100;'

for cfg in "$ROOT/AnmolDristi/Web.config" "$ROOT/AnmolDristi/bin/AnmolDristi.dll.config"; do
  if [[ -f "$cfg" ]]; then
    if [[ ! -f "${cfg}.remote.bak" ]]; then
      cp "$cfg" "${cfg}.remote.bak"
    fi
    python3 - "$cfg" "$CONN" <<'PY'
import re, sys
path, conn = sys.argv[1], sys.argv[2]
text = open(path, encoding='utf-8').read()
new = re.sub(
    r'(<add name="DbConn" connectionString=")[^"]*(" providerName="System\.Data\.SqlClient"/>)',
    r'\1' + conn.replace('\\', '\\\\') + r'\2',
    text,
    count=1,
)
open(path, 'w', encoding='utf-8').write(new)
PY
    echo "Updated $cfg"
  fi
done
