# ADR 0001: ITSM Application Architecture Strategy

## Status

Proposed

## Context

The existing `AnmolDristi` application is a tightly coupled ASP.NET Web Forms 4.8 monolith using ADO.NET and inline SQL. We are introducing a custom, scalable ITSM module (from basic ticketing to enterprise CMDB). We must decide whether to couple this new system to the legacy architecture or build it as a modern standalone service.

**What we know about the legacy system:**

- Single web assembly (`AnmolDristi`) with **168 `.aspx` code-behinds**; most business logic, validation, and SQL live in those files.
- Data access is **ADO.NET-first** (`SqlConnection` / inline SQL), with stored procedures only in newer pockets and **no active ORM** (EF 6 is referenced but unused).
- Auth is **custom and session-centric**: `MST_UserMaster` for credentials, `ActiveUserSession` for server-side session tracking, and a custom RBAC model (`UserRole` → `RolePermission` → `Permission`). Forms Authentication is configured but not the real gatekeeper.
- Layering is **partial at best**: only a few modules (policy exceptions, VMS, some QC) use `DAL/` + `BLL/` folders inside the same project. `DB_Utility_OH4Y` mixes data access with UI helpers.

**What ITSM will require:**

- A flexible, evolvable data model (tickets → assets → relationships → CMDB).
- Dynamic UIs (queues, Kanban, search, dashboards, workflow designer).
- APIs for integrations (email, monitoring, HR, procurement).
- Independent scaling and release cadence as the module grows.

## Decision

**Option B: Build ITSM as a modern standalone service (sidecar), integrated with the legacy app for identity and navigation — not as new Web Forms pages inside `AnmolDristi`.**

## Rationale

### Why not Option A (extend the monolith)

1. **The dominant pattern is page-level coupling.** New ITSM features would follow the same model as large legacy pages (e.g. `qaqc_process_rpt.aspx.cs`, ~3,300 lines mixing UI, rules, and SQL), making CMDB and workflow logic hard to test, reuse, or scale.

2. **Web Forms is a poor fit for enterprise ITSM UX.** CMDB, ticket queues, SLA dashboards, and relationship graphs need component-driven, API-backed UIs. Bolting that onto `.aspx` + postbacks + `[WebMethod]` AJAX will be slower to build and harder to maintain than a modern SPA or Blazor front end on a dedicated API.

3. **Introducing an ORM into the monolith creates two data paradigms.** The codebase is overwhelmingly inline SQL + `DataTable`. Adding EF Core (or similar) only for ITSM inside the same project increases inconsistency without fixing the structural coupling.

4. **CMDB schema evolution is high-churn.** Asset types, CI relationships, discovery sources, and workflow definitions change frequently. Embedding that in the legacy SQL Server schema and deployment unit ties ITSM releases to the entire `AnmolDristi` release cycle.

5. **Partial DAL/BLL is not a foundation to extend.** Only ~9 classes in `DAL/` and `BLL/` exist today; they are folders in the web project, not reusable libraries. ITSM would not inherit a mature architecture — only the worst coupling habits.

### Why Option B fits

1. **Separation of concerns.** ITSM gets its own bounded context: tickets, SLAs, changes, assets, CMDB graph, audit trail — with a proper domain model and ORM (e.g. EF Core + PostgreSQL or SQL Server).

2. **Scalability and operability.** The ITSM API and UI can scale, deploy, and fail independently. Incidents in QA/QC modules do not take down the service desk.

3. **API-first enables the roadmap.** Basic ticketing, approvals, email ingestion, monitoring webhooks, and CMDB discovery all need stable APIs. A sidecar is natural; a Web Forms monolith is not.

4. **Legacy identity can be integrated without replicating its flaws.** We do not need to embed `ActiveUserSession` heartbeats inside ITSM. We need a thin **identity bridge**:
   - Treat `MST_UserMaster.EmployeeCode` as the canonical user key.
   - On login from `AnmolDristi`, issue a signed token (JWT) or use OpenID Connect with a bridge that validates legacy session / credentials once.
   - Map legacy roles/permissions to ITSM roles at the integration boundary (sync job or on-demand lookup), rather than querying `UserRole` from every ITSM page.

5. **Strangler-fig path.** Users can reach ITSM from `home.aspx` via a link to the new app (same domain or subdomain). Legacy stays stable; ITSM grows without rewriting 168 pages.

### Decision summary

| Criterion | Monolith (A) | Sidecar (B) |
|-----------|--------------|-------------|
| Time to first ticket | Faster initially | Slightly slower (scaffolding) |
| CMDB / workflow fit | Poor | Strong |
| Testability | Low | High |
| Release independence | None | Full |
| Auth reuse | Trivial but couples session model | Bridge required, decoupled runtime |
| Long-term TCO | High (debt compounds) | Lower |

## Consequences

### Positive

- ITSM can use a **modern stack** (e.g. ASP.NET Core 8 API + React/Blazor + EF Core) without contaminating the legacy codebase.
- **Independent versioning** of ITSM schema, APIs, and UI.
- **Clear domain boundaries** for tickets, changes, assets, and CMDB relationships.
- Easier path to **enterprise features**: multi-tenant queues, SLA engine, workflow designer, CMDB federation, external integrations.
- Legacy `AnmolDristi` **remains unchanged** in scope and risk profile for existing QA/QC/production modules.

### Negative / costs

- **Identity integration work upfront:** bridge from legacy login/session to ITSM tokens; define employee-code mapping and role sync.
- **Two deployments** to operate (legacy IIS app + ITSM service), plus possibly a reverse proxy (e.g. `/itsm` → new app).
- **Duplicate cross-cutting concerns** unless shared carefully: logging, email (today via `EmailNotifier` in monolith), audit standards.
- **User context fragmentation** until navigation is seamless (single sign-on feel from `home.aspx` → ITSM).
- Team needs **dual-stack skills** during transition (Web Forms maintenance + modern API/SPA development).

### Required follow-up

1. **ADR 0002:** Identity bridge design (`MST_UserMaster` / session → JWT or OIDC).
2. **ADR 0003:** ITSM data store (shared SQL Server vs dedicated DB; CMDB schema ownership).
3. **ADR 0004:** UI strategy (embedded iframe vs subdomain vs full SPA with deep link from legacy menu).
4. Spike: prototype login flow from `login.aspx` → ITSM landing page with shared `EmployeeCode` and permission mapping.
