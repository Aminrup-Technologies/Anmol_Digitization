# ITSM Phase 1 — Data Architecture & Schema

## Status

Draft (aligned with [ADR 0001](../04-adrs/0001-itsm-architecture-strategy.md))

## Overview

Phase 1 establishes the foundational `itsm` schema and EF6 data layer for basic service desk ticketing inside the `AnmolDristi` modular monolith (.NET Framework 4.8). All new ITSM tables live in the **`itsm`** schema on the existing SQL Server database (`DbConn`). Legacy `dbo` master tables are exposed as **read-only** EF6 entities for lookups and joins.

```
AnmolDristi/ITSM/
├── UI/     ← Web Forms pages; no inline SQL
├── BLL/    ← Ticket lifecycle, validation, SLA rules (calls DAL only)
└── DAL/    ← EF6 DbContext, entities, repositories (sole SQL access layer)
```

| Layer | Technology | Data access |
|-------|------------|-------------|
| `~/ITSM/UI/` | ASP.NET Web Forms 4.8 | None — calls BLL services only |
| `~/ITSM/BLL/` | C# service classes | Via DAL repositories / `DbContext` |
| `~/ITSM/DAL/` | **Entity Framework 6 (EF6)** | Read/write `itsm.*`; read-only `dbo.*` |

## Database conventions

- **Schema:** `itsm` for all new ITSM tables.
- **Primary keys:** `INT IDENTITY` unless noted.
- **Audit columns:** `CreatedBy`, `CreatedOn`, `ModifiedBy`, `ModifiedOn` on all mutable entities.
- **Soft delete:** `IsDeleted BIT NOT NULL DEFAULT 0` where applicable.
- **Employee references:** store `EmployeeCode` (`NVARCHAR(50)`) matching `dbo.MST_UserMaster.EmployeeCode` — no FK to legacy tables in Phase 1 (logical reference only).

## Phase 1 scope

| In scope | Out of scope (later phases) |
|----------|----------------------------|
| Incident / service request tickets | Full CMDB CI relationship graph |
| Ticket comments & status history | Discovery / federation |
| Categories, priorities, statuses (lookup tables) | Change / problem management |
| Assignment to agent (`EmployeeCode`) | SLA breach automation |
| Basic audit trail | Asset lifecycle management |

---

## `itsm` schema — new tables

### `itsm.TicketStatus`

Lookup table for ticket workflow states.

| Column | Type | Notes |
|--------|------|-------|
| `TicketStatusId` | `INT` PK | Identity |
| `Code` | `NVARCHAR(50)` | Unique — e.g. `Open`, `InProgress`, `Resolved`, `Closed` |
| `DisplayName` | `NVARCHAR(100)` | UI label |
| `SortOrder` | `INT` | Queue ordering |
| `IsTerminal` | `BIT` | `1` for `Closed` / `Cancelled` |
| `IsActive` | `BIT` | Default `1` |

### `itsm.TicketPriority`

| Column | Type | Notes |
|--------|------|-------|
| `TicketPriorityId` | `INT` PK | Identity |
| `Code` | `NVARCHAR(50)` | Unique — e.g. `Low`, `Medium`, `High`, `Critical` |
| `DisplayName` | `NVARCHAR(100)` | |
| `SortOrder` | `INT` | |
| `IsActive` | `BIT` | Default `1` |

### `itsm.TicketCategory`

| Column | Type | Notes |
|--------|------|-------|
| `TicketCategoryId` | `INT` PK | Identity |
| `Code` | `NVARCHAR(50)` | Unique |
| `DisplayName` | `NVARCHAR(100)` | |
| `ParentCategoryId` | `INT` NULL | Self-reference for hierarchy |
| `IsActive` | `BIT` | Default `1` |

### `itsm.Ticket`

Core incident / service-request record.

| Column | Type | Notes |
|--------|------|-------|
| `TicketId` | `INT` PK | Identity |
| `TicketNumber` | `NVARCHAR(20)` | Unique display number — e.g. `ITSM-2026-00001` |
| `Title` | `NVARCHAR(200)` | Required |
| `Description` | `NVARCHAR(MAX)` | |
| `TicketStatusId` | `INT` FK | → `itsm.TicketStatus` |
| `TicketPriorityId` | `INT` FK | → `itsm.TicketPriority` |
| `TicketCategoryId` | `INT` FK NULL | → `itsm.TicketCategory` |
| `RequesterEmployeeCode` | `NVARCHAR(50)` | Logical ref → `dbo.MST_UserMaster` |
| `AssignedToEmployeeCode` | `NVARCHAR(50)` NULL | Agent assignment |
| `PlantId` | `INT` NULL | Optional org context from legacy masters |
| `BranchId` | `INT` NULL | |
| `RegionId` | `INT` NULL | |
| `DueDate` | `DATETIME2` NULL | |
| `ResolvedOn` | `DATETIME2` NULL | |
| `ClosedOn` | `DATETIME2` NULL | |
| `IsDeleted` | `BIT` | Default `0` |
| `CreatedBy` | `NVARCHAR(50)` | `EmployeeCode` of creator |
| `CreatedOn` | `DATETIME2` | Default `SYSUTCDATETIME()` |
| `ModifiedBy` | `NVARCHAR(50)` NULL | |
| `ModifiedOn` | `DATETIME2` NULL | |

**Indexes:** `IX_Ticket_Status`, `IX_Ticket_AssignedTo`, `IX_Ticket_Requester`, `IX_Ticket_CreatedOn DESC`.

### `itsm.TicketComment`

| Column | Type | Notes |
|--------|------|-------|
| `TicketCommentId` | `INT` PK | Identity |
| `TicketId` | `INT` FK | → `itsm.Ticket` |
| `CommentText` | `NVARCHAR(MAX)` | |
| `IsInternal` | `BIT` | `1` = agent-only note |
| `AuthorEmployeeCode` | `NVARCHAR(50)` | |
| `CreatedOn` | `DATETIME2` | |

### `itsm.TicketHistory`

Append-only status / assignment change log.

| Column | Type | Notes |
|--------|------|-------|
| `TicketHistoryId` | `INT` PK | Identity |
| `TicketId` | `INT` FK | → `itsm.Ticket` |
| `FieldName` | `NVARCHAR(100)` | e.g. `TicketStatusId`, `AssignedToEmployeeCode` |
| `OldValue` | `NVARCHAR(500)` NULL | |
| `NewValue` | `NVARCHAR(500)` NULL | |
| `ChangedByEmployeeCode` | `NVARCHAR(50)` | |
| `ChangedOn` | `DATETIME2` | |

---

## Legacy `dbo` tables — read-only EF6 mappings

Legacy master tables are mapped in `~/ITSM/DAL/Entities/Legacy/` as **read-only** entities. EF6 must not insert, update, or delete rows in these tables. Use Data Annotations (or Fluent API) to pin schema and table names.

### Example: `MST_UserMaster`

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities.Legacy
{
    [Table("MST_UserMaster", Schema = "dbo")]
    public class LegacyUserMaster
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string EmployeeCode { get; set; }

        [MaxLength(200)]
        public string EmployeeName { get; set; }

        [MaxLength(50)]
        public string EmployeeStatus { get; set; }

        public int? RegionId { get; set; }
        public int? BranchId { get; set; }
        public int? PlantId { get; set; }
        public int? DepartmentId { get; set; }
        public int? GradeId { get; set; }
        public int? Designation_ID { get; set; }
    }
}
```

### Phase 1 legacy read-only entities

| Entity class | Table | Purpose |
|--------------|-------|---------|
| `LegacyUserMaster` | `dbo.MST_UserMaster` | Requester / assignee name resolution, active-user checks |
| `LegacyBranch` | `dbo.MST_Branch` | Branch dropdowns and filters |
| `LegacyPlant` | `dbo.MST_Plant` | Plant context on tickets |
| `LegacyRegion` | `dbo.MST_Region` | Region context on tickets |

> **Rule:** Mark legacy `DbSet` properties with no public setter on `SaveChanges` override, or use a dedicated read-only `LegacyDbContext` / repository that exposes `IQueryable<T>` only. Never call `SaveChanges` on legacy entities.

---

## EF6 `DbContext` design

Location: `~/ITSM/DAL/ItsmDbContext.cs`

```csharp
public class ItsmDbContext : DbContext
{
    public ItsmDbContext() : base("name=DbConn") { }

    // itsm schema — read/write
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<TicketComment> TicketComments { get; set; }
    public DbSet<TicketHistory> TicketHistories { get; set; }
    public DbSet<TicketStatus> TicketStatuses { get; set; }
    public DbSet<TicketPriority> TicketPriorities { get; set; }
    public DbSet<TicketCategory> TicketCategories { get; set; }

    // dbo schema — read-only lookups
    public DbSet<LegacyUserMaster> LegacyUsers { get; set; }
    public DbSet<LegacyBranch> LegacyBranches { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        // Default schema for ITSM-owned entities
        modelBuilder.HasDefaultSchema("itsm");

        modelBuilder.Entity<Ticket>()
            .Property(t => t.TicketNumber)
            .IsRequired()
            .HasMaxLength(20);

        // Legacy entities explicitly mapped to dbo (see Data Annotations on each class)
        modelBuilder.Entity<LegacyUserMaster>().ToTable("MST_UserMaster", "dbo");
    }

    public override int SaveChanges()
    {
        // Block accidental writes to legacy entity types
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is LegacyUserMaster ||
                entry.Entity is LegacyBranch)
            {
                throw new InvalidOperationException(
                    "Writes to legacy dbo tables are not permitted from ITSM DAL.");
            }
        }
        return base.SaveChanges();
    }
}
```

Migrations: EF6 Code First migrations scoped to `itsm` schema only. Seed scripts for `TicketStatus`, `TicketPriority`, and default categories.

---

## ITSM Business Logic Layer (BLL) responsibilities

BLL services in `~/ITSM/BLL/` orchestrate use cases. They **must not** reference `System.Web` or open `SqlConnection` directly.

| Service | Responsibility |
|---------|----------------|
| `TicketService` | Create, update, assign, resolve, close tickets; enforce status transitions |
| `TicketCommentService` | Add public / internal comments |
| `TicketQueryService` | Paged list, filter by status / assignee / requester |
| `TicketNumberGenerator` | Allocate `ITSM-YYYY-#####` sequence |

**Example flow:** `TicketList.aspx.cs` → `TicketQueryService.GetOpenTickets(filter)` → `TicketRepository` (DAL) → EF6 LINQ over `itsm.Ticket` joined with read-only `LegacyUserMaster` for display names.

---

## Seed data (Phase 1)

### `itsm.TicketStatus`

| Code | DisplayName | IsTerminal |
|------|-------------|------------|
| `Open` | Open | 0 |
| `InProgress` | In Progress | 0 |
| `Resolved` | Resolved | 0 |
| `Closed` | Closed | 1 |
| `Cancelled` | Cancelled | 1 |

### `itsm.TicketPriority`

| Code | DisplayName | SortOrder |
|------|-------------|-----------|
| `Low` | Low | 1 |
| `Medium` | Medium | 2 |
| `High` | High | 3 |
| `Critical` | Critical | 4 |

---

## Deployment & migration

1. DBA / release script: `CREATE SCHEMA itsm` on the shared database.
2. Apply EF6 migration to create `itsm.*` tables.
3. Run seed script for lookup tables.
4. Deploy updated `AnmolDristi` IIS site (single deployable unit per ADR 0001).
5. No changes to legacy `dbo` table structures in Phase 1.

---

## Related documents

- [ADR 0001 — ITSM Application Architecture Strategy](../04-adrs/0001-itsm-architecture-strategy.md)
- [ADR 0002 — Identity Bridge & SSO](../04-adrs/0002-identity-bridge-sso.md) *(Rejected / Deprecated — in-process `BasePage` auth used instead)*
