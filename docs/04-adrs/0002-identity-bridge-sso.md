# ADR 0002: Identity Bridge & Single Sign-On (SSO)

## Status

Proposed

## Context

Following [ADR 0001](./0001-itsm-architecture-strategy.md), the ITSM module will be built as a standalone .NET 8 API with a modern SPA frontend. The legacy application uses custom authentication backed by `MST_UserMaster` and tracks active sessions in an `ActiveUserSession` table via a `LoginToken`. We need a mechanism for users to seamlessly access the ITSM module without authenticating twice.

**Relevant legacy behavior (from codebase analysis):**

- On successful login, `login.aspx` calls `SessionHelper.CreateOrReuseActiveSession()` and stores:
  - `Session["LoginToken"]` — a `GUID` issued per active session
  - `Session["ActiveSessionId"]` — row identity in `ActiveUserSession`
  - `sessionStorage.setItem('LoginToken', ...)` — client-side copy for heartbeat/logout flows
- `SessionHelper.IsSessionActive(loginToken)` validates `ActiveUserSession` where `LoginToken = @t AND IsActive = 1`.
- `SessionHelper.EndActiveSession(loginToken)` sets `IsActive = 0` and records `LogoutTime`.
- `BasePage` periodically calls `SessionHelper.UpdateHeartbeat(token)`; stale sessions are cleaned up by a background timer in `Global.asax`.
- RBAC is resolved from `UserRole` → `RolePermission` → `Permission` (keyed by `PermissionKey`).

## Decision

We will implement a **Database-Backed Token Validation** strategy.

- The legacy application will securely pass the existing `LoginToken` to the ITSM frontend.
- The .NET 8 ITSM API will implement custom authentication middleware to read this token.
- The API will validate the token against the legacy `ActiveUserSession` SQL table.

## Rationale

- **Speed to Market:** It leverages the existing `ActiveUserSession` table, requiring minimal to no changes to the legacy `UserAuthService` or `login.aspx`.
- **Security:** Centralizes session invalidation. If a session is killed in the legacy app (e.g., by the `SessionHelper` heartbeat monitor or `LogoutHandler.ashx`), the ITSM API instantly loses authorization.
- **Avoids Big Bang Auth Rewrite:** Implementing a full Identity Provider (like Duende IdentityServer or Keycloak) would significantly delay the ITSM MVP.

## Implementation Notes

### Token handoff (legacy → ITSM SPA)

Preferred flow for MVP:

1. User is authenticated in `AnmolDristi` (session + `LoginToken` already exist).
2. User navigates to ITSM via a link from `home.aspx` (or equivalent menu entry).
3. Legacy page or redirect handler passes the token to the ITSM SPA using one of:
   - **Short-lived exchange:** Legacy backend endpoint validates session, returns a one-time code; SPA exchanges it for an ITSM session cookie/header.
   - **Direct header/cookie:** SPA reads `LoginToken` from a secure, `HttpOnly` cookie set by legacy on navigation (avoid exposing token in query strings or referrer logs).

The legacy app already writes `LoginToken` to `sessionStorage`; **do not rely on `sessionStorage` alone** for API auth — it is not sent automatically to the ITSM API on another origin.

### API validation middleware (.NET 8)

On each authenticated request:

```sql
SELECT TOP 1 s.ActiveSessionId, s.UserId, s.EmployeeCode, s.IsActive, s.LastHeartbeat
FROM ActiveUserSession s WITH (NOLOCK)
WHERE s.LoginToken = @LoginToken AND s.IsActive = 1
```

If no row is returned → `401 Unauthorized`.

Optionally join `MST_UserMaster` for display name, org context (`RegionId`, `BranchId`, `PlantId`, etc.), and `EmployeeStatus = 'Active'`.

### Claims mapping

Map validated session to .NET claims, for example:

| Claim | Source |
|-------|--------|
| `sub` / `employee_code` | `ActiveUserSession.EmployeeCode` |
| `user_id` | `ActiveUserSession.UserId` |
| `session_id` | `ActiveSessionId` |
| `permission` (multiple) | `Permission.PermissionKey` via `UserRole` / `RolePermission` |

ITSM-specific roles (e.g. `ITSM.Agent`, `ITSM.Admin`) can be layered on top via a mapping table or configuration without modifying legacy RBAC tables in MVP.

### Database access

The ITSM API requires **read-only** access (MVP) to:

- `ActiveUserSession`
- `MST_UserMaster`
- `UserRole`, `RolePermission`, `Permission`

Use a dedicated SQL login with least privilege. Writes remain owned by the legacy application.

## Consequences

### Positive

- No second login screen for users coming from `AnmolDristi`.
- Logout and session expiry remain **single source of truth** in `ActiveUserSession`.
- MVP can ship without standing up a separate IdP.

### Negative / risks

- The .NET 8 API requires read access to the legacy SQL database (specifically `ActiveUserSession`, `MST_UserMaster`, and RBAC tables).
- A secure token handoff mechanism (e.g. SameSite secure cookies or temporary exchange tokens) must be established between the legacy UI and the new SPA.
- We will need to map legacy custom roles/permissions to modern .NET 8 claims-based authorization.
- **Coupling:** ITSM availability depends on legacy DB connectivity for auth on every request (mitigate with short-lived ITSM-issued JWT after initial validation).
- **Cross-origin complexity:** If ITSM runs on a different subdomain, cookie and CORS policy must be designed explicitly.

## Follow-up

1. Spike: secure redirect from `home.aspx` → ITSM SPA with token exchange (no token in URL).
2. ADR 0003: ITSM data store (shared vs dedicated database).
3. Define ITSM permission keys and mapping from legacy `PermissionKey` values.
4. Post-MVP: evaluate migration to a dedicated IdP once ITSM is stable (optional ADR).
