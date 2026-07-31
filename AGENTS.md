# Anmol Dristi — local development

ASP.NET Web Forms (.NET Framework 4.8) QAQC portal. Production targets Windows + IIS; this repo includes Linux dev tooling using Mono + XSP + SQL Server in Docker.

## Prerequisites (one-time, VM snapshot)

System packages installed on the dev VM:

- Docker (SQL Server container)
- `mono-complete` (ASP.NET runtime)
- XSP 4.7.1 — Ubuntu's `mono-xsp4` package fails on Mono 6.8; run `scripts/install-xsp.sh` if `xsp4` is missing or crashes on start

## Quick start

```bash
./scripts/dev-up.sh          # SQL Server + schema + local Web.config
./scripts/start-dev-server.sh # http://localhost:8080/login.aspx
```

**Test login:** `DEV001` / `devpass`

## Services

| Service | Port | Notes |
|---------|------|-------|
| XSP4 (web) | 8080 | `export MONO_IOMAP=all` required (case-insensitive paths on Linux) |
| SQL Server | 1433 | `docker compose up -d`; credentials in `docker-compose.yml` |

## Building (optional)

Precompiled binaries are in `AnmolDristi/bin/`. To rebuild on Windows, open `AnmolDristi.sln` in Visual Studio. Linux rebuild requires MSBuild for Mono (not preinstalled on this VM).

## Lint / tests

No automated lint or test suite is configured in this repository.

## Cursor Cloud specific instructions

### Startup order

1. Ensure Docker is running: `sudo service docker start`
2. `./scripts/dev-up.sh` — starts SQL Server, applies `scripts/dev-init.sql`, patches `Web.config` for localhost (backs up originals as `*.remote.bak`)
3. `./scripts/start-dev-server.sh` — run in tmux or a dedicated terminal; binds `0.0.0.0:8080`

### Gotchas

- **XSP vs Mono 6.8:** Stock Ubuntu `mono-xsp4` throws `TypeLoadException` for `Mono.Security.Protocol.Tls.PrivateKeySelectionCallback`. Use `scripts/install-xsp.sh` (builds XSP 4.7.1 from source).
- **Case sensitivity:** Always set `MONO_IOMAP=all` before starting XSP; run `start-dev-server.sh` from `AnmolDristi/` (the script does this).
- **Web.config:** `configure-local-db.sh` overwrites the `DbConn` connection string for local SQL Server. Restore from `Web.config.remote.bak` to point back at the remote DB.
- **Database schema:** `scripts/dev-init.sql` only seeds `MST_UserMaster` for login/home. Full QAQC forms need the production schema (not in repo).
- **Email / external apps:** SMTP and `apps.aspx` external links are optional; login and home work without them.
- **Apache/mod_mono:** Installed on the VM but not used; XSP4 standalone is simpler and more reliable here.

### Verify E2E (CLI)

```bash
curl -s -o /dev/null -w '%{http_code}\n' http://127.0.0.1:8080/login.aspx   # expect 200
# Login POST with VIEWSTATE from page → 302 to /home.aspx
```
