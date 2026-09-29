# ADR 0003: ITSM Data Store Strategy

## Status

Accepted

## Context

Following the [Modular Monolith decision in ADR 0001](./0001-itsm-architecture-strategy.md), the ITSM module will be built inside the existing `AnmolDristi` .NET Framework 4.8 web project. We must decide where ITSM data lives: a separate database, a separate SQL Server instance, or within the database the application already uses.

**Current state:**

- `AnmolDristi` connects to SQL Server via the **`DbConn`** connection string in `Web.config` (database: `AnmolDristi` / `AnmolDristiDev` depending on environment).
- Legacy modules store data overwhelmingly in the **`dbo`** schema using ADO.NET (`SqlConnection`, inline SQL, some stored procedures).
- Organizational and user master data already exists in `dbo` — e.g. `MST_UserMaster`, `MST_Branch`, `MST_Plant`, `MST_Region`.
- Entity Framework 6.4.4 is referenced in the project but unused for application data access today.
- [Phase 1 schema design](../05-data-architecture/phase-1-schema.md) defines initial `itsm.*` tables (tickets, comments, history, lookups).

**Options considered:**

| Option | Summary |
|--------|---------|
| **A. Dedicated ITSM database** | New SQL Server database; sync or query legacy masters cross-database |
| **B. Shared database, dedicated schema** | Same database as `AnmolDristi`; new tables under `itsm` schema |
| **C. Shared `dbo` schema** | New ITSM tables mixed into `dbo` alongside legacy tables |

## Decision

We will use a **Shared Database with a Dedicated Schema (`itsm`)** inside the existing AnmolDristi SQL Server database.

- All new ITSM tables are created under the **`itsm`** schema (e.g. `itsm.Ticket`, `itsm.TicketComment`).
- Legacy tables remain in **`dbo`** and are **not modified** by ITSM migrations without a separate approved change.
- The ITSM module uses the **existing `DbConn` connection string** — no second database connection is introduced for MVP.

## Rationale

1. **Zero new DB infrastructure.** No additional SQL Server databases, connection strings, firewall rules, or backup policies to provision. ITSM rides on the database `AnmolDristi` already operates against.

2. **No cross-database distributed transactions.** A dedicated ITSM database would require cross-database queries or replication to resolve employee names, plant/branch context, and permission lookups from `dbo.MST_UserMaster` and related masters. Keeping everything in one database avoids linked servers, sync jobs, and two-phase commit complexity.

3. **Native join capabilities.** EF6 queries in `~/ITSM/DAL/` can join `itsm.Ticket` to `dbo.MST_UserMaster` (read-only) in a single SQL statement — e.g. resolving `RequesterEmployeeCode` to `EmployeeName` without an application-layer fan-out or API call.

4. **Schema boundary without operational split.** The `itsm` schema provides a clear ownership boundary for migrations, permissions, and code (EF6 default schema = `itsm`) while remaining co-located with legacy data.

5. **Aligns with Modular Monolith deployment.** Single IIS site, single database, single release pipeline — consistent with ADR 0001.

### Why not the alternatives

| Alternative | Why rejected |
|-------------|--------------|
| **Dedicated ITSM database** | Operational overhead; cross-db reads for every user/org lookup; harder MVP delivery |
| **Tables in `dbo`** | No isolation; ITSM migrations risk naming collisions; blurs ownership with 168 legacy modules |

## Implementation Details

### Connection string

ITSM DAL uses the existing application connection string — no new entry in `Web.config`:

```xml
<connectionStrings>
  <add name="DbConn"
       connectionString="Data Source=...; Initial Catalog=AnmolDristi; ..."
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

```csharp
// ~/ITSM/DAL/ItsmDbContext.cs
public class ItsmDbContext : DbContext
{
    public ItsmDbContext() : base("name=DbConn") { }
    // ...
}
```

### EF6 in `~/ITSM/DAL/`

The ITSM Data Access Layer is the **only** layer that accesses SQL Server for ITSM entities.

| Concern | Approach |
|---------|----------|
| **New ITSM tables** | EF6 Code First; default schema `itsm`; migrations create/alter `itsm.*` only |
| **Legacy master tables** | Mapped as **read-only** entities under `~/ITSM/DAL/Entities/Legacy/` |
| **Writes to `dbo`** | **Forbidden** — `SaveChanges` guard or separate read-only context |
| **Legacy ADO.NET** | Unchanged; modules outside `~/ITSM/` continue using `DB_Utility_OH4Y` |

### Read-only legacy entity mapping

Legacy `dbo` tables are mapped with EF6 Data Annotations (or Fluent API) and must not be inserted, updated, or deleted by ITSM code.

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
    }
}
```

**Phase 1 read-only legacy entities:** `MST_UserMaster`, `MST_Branch`, `MST_Plant`, `MST_Region` (extend as needed).

### Example join (single database, two schemas)

```csharp
var tickets = from t in context.Tickets
              join u in context.LegacyUsers
                on t.RequesterEmployeeCode equals u.EmployeeCode
              where t.TicketStatus.Code == "Open"
              select new TicketListDto
              {
                  TicketId = t.TicketId,
                  TicketNumber = t.TicketNumber,
                  Title = t.Title,
                  RequesterName = u.EmployeeName
              };
```

### Schema provisioning

1. DBA / release script: `CREATE SCHEMA itsm` (idempotent).
2. EF6 migration applies `itsm` table DDL only.
3. Grant the application SQL login `SELECT` on required `dbo` masters and `SELECT/INSERT/UPDATE/DELETE` on `itsm` objects (principle of least privilege).

### Naming and FK conventions

- ITSM tables: `itsm.<EntityName>` (PascalCase entity, singular table name per Phase 1 doc).
- References to employees: store `EmployeeCode` (`NVARCHAR(50)`) — logical link to `dbo.MST_UserMaster`, no physical FK to legacy tables in Phase 1 (avoids cross-schema FK enforcement on legacy-owned objects).

## Consequences

### Positive

- Minimal infrastructure change; fastest path to MVP.
- Single-transaction consistency when ITSM BLL writes ticket data and reads master context in one unit of work.
- EF6 migrations scoped to `itsm` — low risk to legacy `dbo` objects.
- DBA can grant schema-level permissions (`itsm` vs `dbo`).

### Negative / risks

- **Shared fate:** ITSM and legacy modules share database capacity, locks, and outage windows.
- **Migration coordination:** `itsm` DDL releases must be scheduled alongside legacy deployments on the same database.
- **Discipline required:** Developers must not add ITSM tables to `dbo` or write to legacy entities via EF6.
- **Future extraction cost:** If ITSM later moves to its own database, data migration and cross-db read patterns will be required (acceptable deferred cost).

## Related documents

- [ADR 0001 — ITSM Application Architecture Strategy](./0001-itsm-architecture-strategy.md)
- [Phase 1 schema design](../05-data-architecture/phase-1-schema.md)
