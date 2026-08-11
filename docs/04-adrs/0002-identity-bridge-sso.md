# ADR 0002: Identity Bridge & Single Sign-On (SSO)

## Status

Accepted

## Context

Following the [Hybrid Headless Sidecar architecture in ADR 0001](./0001-itsm-architecture-strategy.md), ITSM domain logic runs in a standalone .NET 8 Web API while the UI is rendered by Web Forms (`.aspx`) pages inside `AnmolDristi`. The legacy application uses custom authentication backed by `MST_UserMaster` and tracks active sessions in an `ActiveUserSession` table via a `LoginToken`. We need a mechanism for users to seamlessly access ITSM features without authenticating twice.

**Relevant legacy behavior (from codebase analysis):**

- On successful login, `login.aspx` calls `SessionHelper.CreateOrReuseActiveSession()` and stores:
  - `Session["LoginToken"]` — a `GUID` issued per active session
  - `Session["ActiveSessionId"]` — row identity in `ActiveUserSession`
- `SessionHelper.IsSessionActive(loginToken)` validates `ActiveUserSession` where `LoginToken = @t AND IsActive = 1`.
- `SessionHelper.EndActiveSession(loginToken)` sets `IsActive = 0` and records `LogoutTime`.
- `BasePage` periodically calls `SessionHelper.UpdateHeartbeat(token)`; stale sessions are cleaned up by a background timer in `Global.asax`.
- RBAC is resolved from `UserRole` → `RolePermission` → `Permission` (keyed by `PermissionKey`).

## Decision

We will implement a **Database-Backed Token Validation** strategy with **server-to-server** token forwarding.

- Web Forms code-behinds read `Session["LoginToken"]` on the legacy server.
- Each call to the ITSM API is made via `HttpClient`, passing the token in an HTTP request header (e.g. `X-ITSM-Token`).
- The .NET 8 ITSM API implements custom authentication middleware to read and validate that header.
- The API validates the token against the legacy `ActiveUserSession` SQL table.

The token **never** transits through the browser, query strings, or page markup.

## Rationale

- **Speed to Market:** It leverages the existing `ActiveUserSession` table, requiring minimal to no changes to the legacy `UserAuthService` or `login.aspx`.
- **Security:** Centralizes session invalidation. If a session is killed in the legacy app (e.g., by the `SessionHelper` heartbeat monitor or `LogoutHandler.ashx`), the ITSM API instantly loses authorization.
- **Simplicity under Hybrid Headless Sidecar:** Server-side `HttpClient` calls from `.aspx.cs` avoid browser token exposure and keep auth aligned with the existing ASP.NET session model.
- **Avoids Big Bang Auth Rewrite:** Implementing a full Identity Provider (like Duende IdentityServer or Keycloak) would significantly delay the ITSM MVP.

## Implementation Notes

### Token handoff (legacy Web Forms → ITSM API)

Preferred flow for MVP:

1. User is authenticated in `AnmolDristi`; `Session["LoginToken"]` and `Session["ActiveSessionId"]` are already set by `login.aspx`.
2. User navigates to an ITSM Web Forms page (e.g. ticket list) linked from `home.aspx` or the existing menu.
3. The `.aspx.cs` code-behind (or a shared `ITSM.ApiClient` helper) reads the token from session:

   ```csharp
   var token = Session["LoginToken"]?.ToString();
   if (string.IsNullOrEmpty(token))
       Response.Redirect("~/login.aspx");
   ```

4. The code-behind calls the ITSM API using `HttpClient`, attaching the token as a request header:

   ```csharp
   request.Headers.Add("X-ITSM-Token", token);
   ```

5. The API middleware validates the header value against `ActiveUserSession` before the controller executes.

**Rules:**

- Do **not** pass `LoginToken` in URL query strings, hidden form fields rendered to HTML, or JavaScript variables.
- Use a shared API client class in `AnmolDristi` so every ITSM page applies the header consistently.
- On `401` from the API, redirect the user to `login.aspx` (session expired or revoked).

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

- No second login screen for users navigating to ITSM pages within `AnmolDristi`.
- Logout and session expiry remain **single source of truth** in `ActiveUserSession`.
- MVP can ship without standing up a separate IdP.
- **Token stays server-side:** the `LoginToken` is read from ASP.NET session and forwarded over `HttpClient`; it is not exposed to the browser.
- **Straightforward integration:** aligns naturally with Web Forms code-behinds and the Hybrid Headless Sidecar pattern from ADR 0001.

### Negative / risks

- The .NET 8 API requires read access to the legacy SQL database (specifically `ActiveUserSession`, `MST_UserMaster`, and RBAC tables).
- We will need to map legacy custom roles/permissions to modern .NET 8 claims-based authorization.
- **Coupling:** ITSM availability depends on legacy DB connectivity for auth on every API request (mitigate later with short-lived server-side caching or ITSM-issued tokens after initial validation).
- **Network trust:** The ITSM API must only accept `X-ITSM-Token` from trusted callers (internal network, API gateway, or mutual TLS between `AnmolDristi` and the API host).
- **Discipline in code-behinds:** Developers must use the shared API client; ad-hoc `HttpClient` calls that omit the header will fail auth.

## Follow-up

1. Spike: one end-to-end flow — ITSM Web Forms page → `HttpClient` with `X-ITSM-Token` → API → grid bind.
2. ADR 0003: ITSM data store (shared vs dedicated database).
3. Define ITSM permission keys and mapping from legacy `PermissionKey` values.
4. Implement shared `ITSM.ApiClient` in `AnmolDristi` with header injection and `401` handling.
5. Post-MVP: evaluate migration to a dedicated IdP once ITSM is stable (optional ADR).
