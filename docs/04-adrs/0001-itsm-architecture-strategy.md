# ADR 0001: ITSM Application Architecture Strategy

## Status

Accepted

## Context

The existing `AnmolDristi` application is a tightly coupled ASP.NET Web Forms 4.8 monolith using ADO.NET and inline SQL. We are introducing a custom, scalable ITSM module (from basic ticketing to enterprise CMDB). We must decide whether to couple this new system to the legacy architecture or build it as a modern standalone service.

**What we know about the legacy system:**

- Single web assembly (`AnmolDristi`) with **168 `.aspx` code-behinds**; most business logic, validation, and SQL live in those files.
- Data access is **ADO.NET-first** (`SqlConnection` / inline SQL), with stored procedures only in newer pockets and **no active ORM** (EF 6 is referenced but unused).
- Auth is **custom and session-centric**: `MST_UserMaster` for credentials, `ActiveUserSession` for server-side session tracking, and a custom RBAC model (`UserRole` → `RolePermission` → `Permission`). Forms Authentication is configured but not the real gatekeeper.
- Layering is **partial at best**: only a few modules (policy exceptions, VMS, some QC) use `DAL/` + `BLL/` folders inside the same project. `DB_Utility_OH4Y` mixes data access with UI helpers.
- The delivery team has **deep Web Forms expertise** and established patterns (`BasePage`, session checks, master pages, AdminLTE-style UI).

**What ITSM will require:**

- A flexible, evolvable data model (tickets → assets → relationships → CMDB).
- Rich UI for queues, search, dashboards, and CMDB views.
- APIs for integrations (email, monitoring, HR, procurement).
- Independent evolution of domain logic and CMDB schema as the module grows toward enterprise scope.

## Decision

We will adopt a **Hybrid Headless Sidecar** architecture:

| Layer | Technology | Location |
|-------|------------|----------|
| **Core backend, domain logic, CMDB** | Standalone **.NET 8 Web API** with **Entity Framework Core** | Separate deployable service (sidecar) |
| **UI presentation** | **ASP.NET Web Forms** (`.aspx` pages) | Existing `AnmolDristi` project |

**How it works:**

- The **ITSM API** owns all business rules, persistence, workflows, SLA logic, and CMDB relationships. It exposes REST (or minimal-API) endpoints consumed by the UI and future integrations.
- **Web Forms pages** in `AnmolDristi` act as a **thin presentation shell**: they handle layout, navigation, session gates, and data binding, but **do not embed ITSM domain logic or direct SQL** for tickets, assets, or CMDB entities.
- ITSM `.aspx` code-behinds call the sidecar API via `HttpClient` (server-side), forwarding the user's `LoginToken` (per [ADR 0002](./0002-identity-bridge-sso.md)) on each request.
- Existing QA/QC and production modules remain unchanged; new ITSM pages are additive under a dedicated menu area (e.g. from `home.aspx`).

This is **not** Option A (full monolith with inline SQL in code-behinds) and **not** a pure SPA sidecar. It is a deliberate split: **modern headless backend + familiar Web Forms frontend**.

## Rationale

### Why not Option A (full monolith extension)

1. **Page-level coupling is the dominant legacy anti-pattern.** Adding CMDB and workflow logic directly into `.aspx.cs` files (as seen in large pages like `qaqc_process_rpt.aspx.cs`) would reproduce the same testability and maintenance problems.

2. **CMDB schema evolution is high-churn.** Asset types, CI relationships, discovery sources, and workflow definitions change frequently. Keeping that model inside the Web Forms project with ADO.NET inline SQL would tie schema migrations to the entire `AnmolDristi` deployment.

3. **No active ORM in the monolith.** Introducing EF Core only inside `AnmolDristi` would create two data paradigms (ADO.NET + EF) in one assembly without a clean boundary.

### Why not a pure SPA sidecar (full Option B UI)

1. **Team expertise.** The team is proficient in Web Forms, `BasePage`, session patterns, and the existing master page shell. A React/Vue SPA would introduce a new frontend stack, build pipeline, and skill gap without a proportional MVP benefit.

2. **Navigation and auth are already solved in Web Forms.** Users stay within the familiar `AnmolDristi` shell; `LoginToken` handoff to the API (ADR 0002) is simpler when the caller is a server-side code-behind than a cross-origin browser SPA.

3. **Incremental delivery.** New `.aspx` pages can be added one feature at a time (ticket list, ticket detail, asset view) without standing up a separate frontend deployment.

### Why the Hybrid Headless Sidecar fits

1. **Separation where it matters.** Domain logic, CMDB graph, SLA engine, and EF Core migrations live in the .NET 8 API — testable, versioned, and deployable independently.

2. **Familiar UI where it helps.** Web Forms handles presentation, grids, postbacks, and permission-gated menus using patterns the team already uses (`RequirePermission`, `BasePage`, `Dristi.Master`).

3. **API-first enables the roadmap.** The same endpoints that power `.aspx` pages also support email ingestion, monitoring webhooks, mobile clients, and CMDB federation later — without rewriting the backend.

4. **Controlled coupling.** Web Forms code-behinds are constrained to HTTP calls + view models, not `SqlConnection` against ITSM tables. This prevents ITSM from inheriting the worst monolith habits while avoiding a greenfield UI.

5. **Strangler-fig path.** ITSM grows as new pages under `AnmolDristi`; the API can scale or move to its own host without touching existing 168 legacy pages.

### Decision summary

| Criterion | Full monolith (A) | Pure SPA sidecar | Hybrid headless sidecar |
|-----------|-------------------|------------------|-------------------------|
| Time to first ticket | Fastest | Slowest (UI + API) | Moderate |
| CMDB / domain fit | Poor | Strong | Strong |
| Testability of domain logic | Low | High | High |
| Backend release independence | None | Full | Full (API only) |
| UI release independence | N/A | Full | Coupled to `AnmolDristi` deploy |
| Team skill alignment | High | Low (new SPA stack) | High (Web Forms) + moderate (API) |
| Long-term TCO | High (debt compounds) | Moderate | Lower than A, pragmatic vs pure SPA |

## Consequences

### Positive

- **Clean domain boundary:** EF Core, CMDB schema, and business rules live in the .NET 8 API — not scattered across code-behinds.
- **Leverages team strength:** Web Forms for UI; no mandatory SPA adoption for MVP.
- **API-ready:** Integrations and future clients consume the same endpoints as the `.aspx` shell.
- **Legacy isolation:** Existing QA/QC modules are untouched; ITSM pages are additive.
- **Aligns with ADR 0002:** Server-side `HttpClient` from code-behind can attach `LoginToken` without cross-origin cookie complexity.

### Negative / costs

- **Two deployable units:** `AnmolDristi` (UI) + ITSM API (backend). UI changes require a Web Forms deploy even when only presentation changes.
- **Thin-client discipline required:** Code-behinds must not drift back into inline SQL or business logic; enforce via review and shared API client library.
- **Latency:** Every page action is an HTTP round-trip to the API (mitigate with caching, pagination, and batch endpoints).
- **Dual-stack maintenance:** Web Forms 4.8 + .NET 8 API during transition.
- **Shared cross-cutting concerns** (logging, email, audit) need conventions across both projects.

### Required follow-up

1. **ADR 0002:** Identity bridge — `LoginToken` validation in API middleware (accepted approach: database-backed).
2. **ADR 0003:** ITSM data store (dedicated DB vs shared SQL Server; CMDB schema ownership).
3. Define a **shared API client** (`AnmolDristi.ITSM.Client` or similar) for typed HTTP calls from `.aspx.cs` to the sidecar.
4. Establish **code-behind conventions:** no `SqlConnection` for ITSM entities; DTOs only; error handling and permission checks at the page layer.
5. Spike: one end-to-end flow (e.g. ticket list page → API → grid bind) to validate the hybrid pattern.
