#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SQL_FILE="$ROOT/scripts/dev-init.sql"
PASSWORD="${MSSQL_SA_PASSWORD:-DevPassword123!}"

echo "Waiting for SQL Server..."
for i in $(seq 1 60); do
  if sg docker -c "docker exec anmoldristi-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$PASSWORD\" -C -Q 'SELECT 1'" >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

echo "Applying schema and seed data..."
sg docker -c "docker exec -i anmoldristi-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$PASSWORD\" -C" < "$SQL_FILE"
echo "Database ready."
