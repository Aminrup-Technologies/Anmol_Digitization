# ADR 0001: ITSM Application Architecture Strategy

## Status

Accepted

## Context

The existing `AnmolDristi` application is a tightly coupled ASP.NET Web Forms 4.8 monolith using ADO.NET and inline SQL. We are introducing a custom, scalable ITSM module (from basic ticketing to enterprise CMDB). We must choose an architecture that delivers ITSM capabilities without repeating the coupling problems that affect the legacy codebase.

**What we know about the legacy system:**

- Single web assembly (`AnmolDristi`) targeting **.NET Framework 4.8**, with **168 `.aspx` code-behinds**; most business logic, validation, and SQL live in those files.
- Data access is **ADO.NET-first** (`SqlConnection` / inline SQL), with stored procedures only in newer pockets.
- **Entity Framework 6.4.4** is already referenced in the project but is **not used** for application data access today.
- Auth is **custom and session-centric**: `MST_UserMaster` for credentials, `ActiveUserSession` for session tracking, and RBAC via `UserRole` → `RolePermission` → `Permission`.
- Layering is **partial at best**: a small `DAL/` and `BLL/` exist for policy exceptions and VMS, but most modules bypass them. `DB_Utility_OH4Y` mixes data access with UI helpers.
- The delivery team has **deep Web Forms expertise** and established patterns (`BasePage`, session checks, `Dristi.Master`, AdminLTE-style UI).

**What ITSM will require:**

- A flexible, evolvable data model (tickets → assets → relationships → CMDB).
- Rich UI for queues, search, dashboards, and CMDB views.
- Integrations with email, monitoring, HR, and procurement over time.
- Clear boundaries so ITSM domain logic does not devolve into the inline-SQL code-behind pattern seen elsewhere in the app.

## Decision

We will build ITSM as a **Modular Monolith** entirely inside the existing `AnmolDristi` .NET Framework 4.8 web project, using the **same SQL Server database** with a dedicated **`itsm` schema** for all new ITSM tables.

### Physical structure

All ITSM code and UI live under a single root folder in the web project:

```
AnmolDristi/
└── ITSM/
    ├── UI/          ← Web Forms pages (.aspx, .aspx.cs, designers)
    ├── BLL/         ← Business logic, services, validators, workflow rules
    └── DAL/         ← Data access only (EF6 DbContext, repositories, mappings)
```

| Folder | Responsibility | Rules |
|--------|----------------|-------|
| **`~/ITSM/UI/`** | Presentation only — `.aspx` pages, controls, view binding, navigation | May call BLL services. **Must not** contain inline SQL, `SqlConnection`, or domain/business rules. |
| **`~/ITSM/BLL/`** | Business logic — ticket lifecycle, SLA evaluation, CMDB rules, approval workflows | May call DAL. **Must not** reference `System.Web.UI` or Web Forms controls. |
| **`~/ITSM/DAL/`** | Persistence — EF6 entities, `DbContext`, repositories, query composition | **Only layer** that talks to SQL Server for ITSM data. No UI or workflow logic. |

### Data access

- **Entity Framework 6 (EF6)** is the sole data-access technology for the ITSM module.
- **Write target:** all new ITSM tables are created and managed in the **`itsm` schema** (e.g. `itsm.Ticket`, `itsm.ConfigurationItem`, `itsm.TicketComment`).
- **Read access to legacy data:** EF6 maps selected existing **`dbo`** tables as read-only entities (e.g. `MST_UserMaster`, org/plant masters) for lookups and joins — **no schema changes to legacy `dbo` tables** without a separate approved migration.
- Code-first or EDMX-based EF6 mappings live exclusively under `~/ITSM/DAL/`.
- Legacy modules outside `~/ITSM/` continue using ADO.NET; ITSM does **not** extend `DB_Utility_OH4Y`.

### Authentication and authorization

ITSM pages use the **existing** `AnmolDristi` session model (`Session["LoginToken"]`, `Session["WORKMAN"]`, `BasePage`, `RequirePermission`). No separate auth subsystem is introduced for ITSM.

### Deployment

- **Single deployable unit:** `AnmolDristi` IIS site, same as today.
- **Single database:** existing connection string (`DbConn`); `itsm` schema is additive.

## Rationale

### Why a Modular Monolith (not unbounded extension of legacy patterns)

1. **The legacy anti-pattern is unbounded code-behind coupling.** Pages like `qaqc_process_rpt.aspx.cs` mix UI, rules, and SQL in one file. ITSM must not follow that model, even though it lives in the same project.

2. **Strict folders enforce discipline.** `UI / BLL / DAL` gives testable seams without the operational cost of a second application, second host, or second deployment pipeline.

3. **EF6 is already in the project.** Using EF6 for the `itsm` schema introduces a modern ORM boundary for new work while staying on .NET Framework 4.8 — no framework upgrade required for MVP.

4. **Dedicated `itsm` schema isolates churn.** CMDB tables, ticket history, and workflow state evolve independently of legacy `dbo` objects. Migrations are scoped to `itsm.*` without risking QA/QC production tables.

5. **Team and runtime alignment.** Web Forms, `BasePage`, session auth, and single-IIS deployment match current team skills and infrastructure. Users navigate ITSM from `home.aspx` like any other module.

6. **Incremental adoption.** Existing 168 legacy pages are untouched. ITSM is additive under `~/ITSM/`.

### Why not alternatives considered earlier

| Approach | Why rejected |
|----------|--------------|
| **Unstructured monolith** (inline SQL in new `.aspx.cs` files) | Reproduces the worst maintainability problems in the codebase. |
| **Separate service / headless backend** | Unnecessary operational complexity for MVP; splits deployment and auth for little gain given Web Forms expertise. |

## Consequences

### Positive

- **Single deployment:** one IIS site, one database, one release process.
- **Clear module boundary:** all ITSM code under `~/ITSM/`; easy to locate, review, and eventually extract if needed.
- **ORM for new schema:** EF6 migrations manage `itsm` tables without hand-written DDL scattered in code-behinds.
- **Reuses existing auth:** `BasePage`, `Session["LoginToken"]`, and `PermissionKey` RBAC — no second login or token bridge.
- **Legacy isolation:** QA/QC and production modules outside `~/ITSM/` are not modified.
- **Read-only legacy access:** EF6 can query `dbo.MST_UserMaster` and related masters without altering legacy write paths.

### Negative / costs

- **Two data paradigms in one project:** ITSM uses EF6; legacy modules remain ADO.NET. Developers must know which pattern applies where.
- **Discipline is conventional, not enforced by the compiler:** folder rules and "no inline SQL in UI" must be enforced by code review and CI checks.
- **Shared runtime risk:** a defect in ITSM BLL/DAL could affect the same app pool as legacy modules (mitigate with testing and feature flags).
- **EF6 on .NET Framework 4.8** is in maintenance mode; long-term migration to EF Core would require a framework upgrade (acceptable deferred cost).
- **`itsm` schema migrations** must be coordinated with DBA release practices on the shared database.

### Required follow-up

1. **ADR 0003:** `itsm` schema design — core entities (Ticket, Asset/CI, Relationship, SLA, Comment, Audit).
2. Scaffold `~/ITSM/DAL/` — EF6 `DbContext` with `itsm` schema default and read-only `dbo` entity mappings.
3. Define **ITSM `BasePage`** (or inherit existing `BasePage`) with `RequirePermission` keys under a `UI.ITSM.*` namespace.
4. Spike: end-to-end ticket list — `~/ITSM/UI/TicketList.aspx` → BLL → DAL → `itsm.Ticket` grid bind.
5. Add **architecture tests or build rules** to block `SqlConnection` / inline SQL in `~/ITSM/UI/`.
6. Register ITSM menu entries on `home.aspx` pointing to `~/ITSM/UI/` pages.
