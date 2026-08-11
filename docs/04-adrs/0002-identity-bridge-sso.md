# ADR 0002: Identity Bridge & Single Sign-On (SSO)

## Status

Rejected / Deprecated

## Context

> **Deprecation notice:** [ADR 0001](./0001-itsm-architecture-strategy.md) was revised to a **Modular Monolith** architecture within the same `AnmolDristi` .NET Framework 4.8 project. An identity bridge is **no longer required**. The ITSM module will natively utilize the existing `Session["LoginToken"]` and `BasePage` auth model — the same session, heartbeat, and `RequirePermission` patterns used by every other module in the application. **Do not implement** the token-forwarding or API middleware approach described below.

This ADR is retained for historical reference only. It was written when ADR 0001 proposed a separate backend service that required cross-application authentication.

---

**Original context (superseded):**

Following a prior revision of ADR 0001, ITSM domain logic was planned to run outside the main web project while the UI remained in Web Forms. The legacy application uses custom authentication backed by `MST_UserMaster` and tracks active sessions in an `ActiveUserSession` table via a `LoginToken`. A bridge mechanism was proposed so users could access ITSM without authenticating twice.

**Relevant legacy behavior (still applicable to ITSM under ADR 0001):**

- On successful login, `login.aspx` calls `SessionHelper.CreateOrReuseActiveSession()` and stores:
  - `Session["LoginToken"]` — a `GUID` issued per active session
  - `Session["ActiveSessionId"]` — row identity in `ActiveUserSession`
- `SessionHelper.IsSessionActive(loginToken)` validates `ActiveUserSession` where `LoginToken = @t AND IsActive = 1`.
- `SessionHelper.EndActiveSession(loginToken)` sets `IsActive = 0` and records `LogoutTime`.
- `BasePage` periodically calls `SessionHelper.UpdateHeartbeat(token)`; stale sessions are cleaned up by a background timer in `Global.asax`.
- RBAC is resolved from `UserRole` → `RolePermission` → `Permission` (keyed by `PermissionKey`).

**Current approach (per accepted ADR 0001):**

ITSM pages under `~/ITSM/UI/` inherit from `BasePage` (or an ITSM-specific subclass). Session gates, `LoginToken` heartbeat validation, and `RequirePermission("UI.ITSM.*")` checks apply identically to legacy pages. No `HttpClient`, custom headers, or separate auth middleware is needed.

## Decision

~~We will implement a **Database-Backed Token Validation** strategy with **server-to-server** token forwarding.~~

**Rejected.** See [ADR 0001](./0001-itsm-architecture-strategy.md) for the accepted Modular Monolith approach.

<details>
<summary>Original decision (historical)</summary>

- Web Forms code-behinds read `Session["LoginToken"]` on the legacy server.
- Each call to the ITSM API is made via `HttpClient`, passing the token in an HTTP request header (e.g. `X-ITSM-Token`).
- A separate ITSM API implements custom authentication middleware to read and validate that header.
- The API validates the token against the legacy `ActiveUserSession` SQL table.

</details>

## Rationale

*Historical — no longer applicable.*

- **Speed to Market:** Leveraged the existing `ActiveUserSession` table without changes to `UserAuthService` or `login.aspx`.
- **Security:** Centralized session invalidation across a separate service boundary.
- **Avoids Big Bang Auth Rewrite:** Deferred a full Identity Provider for MVP.

These goals are now met **in-process** by reusing `BasePage` and `Session["LoginToken"]` within the same application.

## Implementation Notes

*Historical — do not implement.*

The sections below described token forwarding to a separate API host. Under the Modular Monolith, ITSM BLL/DAL layers run in the same app domain as the UI and inherit the existing ASP.NET session automatically.

## Consequences

*Historical — superseded by in-process auth per ADR 0001.*

## Follow-up

*Cancelled — replaced by ADR 0001 follow-up items:*

1. Define ITSM `BasePage` subclass under `~/ITSM/UI/` with standard session and permission checks.
2. Register `UI.ITSM.*` permission keys in the existing RBAC tables.
3. ADR 0003: `itsm` schema design and EF6 `DbContext` setup.
