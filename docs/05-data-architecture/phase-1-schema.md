# Phase 1: Data Architecture & Schema Design (MVP)

## Status

Accepted

## Context

Following [ADR 0001](../04-adrs/0001-itsm-architecture-strategy.md) and [ADR 0003](../04-adrs/0003-itsm-data-store-strategy.md), the ITSM module is built as a **Modular Monolith** within the `.NET 4.8` `AnmolDristi` solution. The ITSM Data Access Layer (`~/ITSM/DAL/`) uses **Entity Framework 6 (EF6)** to manage incident data in a dedicated `itsm` schema on the shared SQL Server database.

To prevent data duplication and enable intelligent UI pre-selections, the EF6 `ItsmDbContext` physically joins new `itsm` transactional tables directly to the legacy `dbo.MST_*` master tables. Business rules for ticket numbering, approval workflows, and agent dispatch live in `~/ITSM/BLL/` and consume these entities via EF6 navigation properties — never via inline SQL in `.aspx` code-behinds.

---

## 1. Utilization of Existing Legacy Masters (Read-Only)

These tables exist in the `dbo` schema. EF6 maps to these using `[Table("...", Schema = "dbo")]` and strictly treats them as `AsNoTracking()` read-only lookup data. The `SaveChanges()` pipeline forbids updates to these entities.

| Table Name | Primary Key | Key Columns Mapped | ITSM Utilization & Relationships |
| :--- | :--- | :--- | :--- |
| **`dbo.MST_UserMaster`** | `EmployeeCode` | `EmployeeName`, `Email`, `Mobile`, `PlantId`, `BranchId`, `DepartmentId`, `ReportingManagerCode`, `EmployeeStatus` | **Core Identity:** Auto-fills requester details on the UI. FK target for `Tickets.RequesterCode`, `Tickets.AssigneeCode`, `TicketApprovals.ApproverCode`, and `MST_SupportMapping.SupportAgentCode`. |
| **`dbo.MST_Plant`** | `Id` | `PlantID_Description`, `SAP_Code` | **Primary Routing Context:** Maps to `Tickets.PlantId`. Determines if the ticket belongs to a Plant/Depot or Corporate Office. |
| **`dbo.MST_Branch`** | `Id` | `Branch_Description` | **Dispatch Anchor:** Maps to `Tickets.BranchId` and `MST_SupportMapping.BranchId`. Drives the Intelligent Dispatch Matrix. |
| **`dbo.MST_Department`** | `Id` | `Department_Description` | **Organizational Context:** Helps IT agents prioritize tickets based on the requester's department. |
| **`dbo.MST_Designation`** | `Id` | `Designation_Description` | **User Role:** Contextual lookup. *(Note: Legacy `MST_UserMaster.Designation_ID` stores ERP identity; mapped carefully in EF6.)* |
| **`dbo.MST_Calendar`** | `CalendarDate` | `IsWeekend`, `FinancialYear`, `FinancialQuarter`, `IsMonthEnd`, `IsQuarterEnd` | **Business Calendar:** Read-only reference for SLA date math (business-day calculations), dashboard analytics grouping, and triggering scheduled maintenance tickets. Mapped via `[Table("MST_Calendar", Schema = "dbo")]`. |

---

## 2. Shared Infrastructure (Write-Append)

| Table Name | Interaction | Purpose in ITSM |
| :--- | :--- | :--- |
| **`dbo.NotificationQueue`** | Insert Only | The ITSM BLL queues emails by inserting rows here with `SourceModule = 'ITSM'`. The legacy background worker handles actual SMTP dispatch, requiring zero new email logic. Approval and assignment events also enqueue notifications here. |

---

## 3. Introduction of New ITSM Masters (Read/Write via EF6)

These tables live in the `itsm` schema. They manage ITSM taxonomies, request types, and dispatch configuration. All are managed via EF6 Code First migrations under `~/ITSM/DAL/`.

### 3.1 Request Type Master

**`itsm.MST_RequestType`** — Defines the three ITSM request families and their ticket-number prefixes.

| Column | Type | Notes |
| :--- | :--- | :--- |
| `Id` | `INT` PK | Identity |
| `Code` | `NVARCHAR(10)` | Unique — `IR`, `SR`, `CR` |
| `Name` | `NVARCHAR(100)` | Display name |
| `Prefix` | `CHAR(2)` | Ticket number prefix — must match `Code` |
| `RequiresManagerApproval` | `BIT` | `1` for SR, CR |
| `RequiresItHeadApproval` | `BIT` | `1` for CR (and optionally SR) |
| `IsActive` | `BIT` | Default `1` |

**Seed data:**

| Code | Name | Prefix | Manager Approval | IT Head Approval |
| :--- | :--- | :--- | :--- | :--- |
| `IR` | Incident Request | `IR` | No | No |
| `SR` | Service Request | `SR` | Yes | No |
| `CR` | Change Request | `CR` | Yes | Yes |

### 3.2 Three-Tier Categorization

**`itsm.MST_Category`** — Top-level taxonomy.

| Column | Type | Notes |
| :--- | :--- | :--- |
| `Id` | `INT` PK | Identity |
| `Name` | `NVARCHAR(100)` | e.g. Hardware, Software, Network |
| `IsActive` | `BIT` | Default `1` |

**`itsm.MST_SubCategory`** — Second tier.

| Column | Type | Notes |
| :--- | :--- | :--- |
| `Id` | `INT` PK | Identity |
| `CategoryId` | `INT` FK | → `itsm.MST_Category.Id` |
| `Name` | `NVARCHAR(100)` | e.g. Printer, ERP Login |
| `IsActive` | `BIT` | Default `1` |

**`itsm.MST_Item`** — Third tier (completes Category → SubCategory → Item).

| Column | Type | Notes |
| :--- | :--- | :--- |
| `Id` | `INT` PK | Identity |
| `SubCategoryId` | `INT` FK | → `itsm.MST_SubCategory.Id` |
| `Name` | `NVARCHAR(100)` | e.g. HP LaserJet Pro, SAP MM Module Access |
| `IsActive` | `BIT` | Default `1` |

```
MST_Category (Hardware)
  └── MST_SubCategory (Printer)
        └── MST_Item (HP LaserJet Pro M404)
```

### 3.3 Priority & Status Masters

**`itsm.MST_Priority`**: `Id`, `Name`, `SlaResolutionHours`, `IsActive` *(e.g. P1 - Critical, P2 - High)*

**`itsm.MST_TicketStatus`**: `Id`, `Name`, `IsTerminal`, `IsActive` *(e.g. New, Pending Approval, In Progress, Resolved, Closed)*

### 3.4 Intelligent Dispatch Configuration

**`itsm.MST_SupportMapping`** — Maps branch + taxonomy to a default support agent. Evaluated by the BLL **Intelligent Dispatch Matrix** (see Section 5).

| Column | Type | Notes |
| :--- | :--- | :--- |
| `Id` | `INT` PK | Identity |
| `BranchId` | `INT` FK | → `dbo.MST_Branch.Id` — **required** |
| `CategoryId` | `INT` FK NULL | → `itsm.MST_Category.Id` |
| `SubCategoryId` | `INT` FK NULL | → `itsm.MST_SubCategory.Id` |
| `ItemId` | `INT` FK NULL | → `itsm.MST_Item.Id` |
| `SupportAgentCode` | `NVARCHAR(50)` | → `dbo.MST_UserMaster.EmployeeCode` |
| `IsBranchDefault` | `BIT` | `1` = fallback row for the branch (no category set) |
| `IsActive` | `BIT` | Default `1` |
| `CreatedOn` | `DATETIME` | |
| `ModifiedOn` | `DATETIME` NULL | |

**Mapping granularity rules:**

- A row may specify `BranchId` only (`IsBranchDefault = 1`) — branch-wide default agent.
- A row may specify `BranchId` + `CategoryId` — category default for that branch.
- A row may specify `BranchId` + `CategoryId` + `SubCategoryId` — sub-category override.
- A row may specify `BranchId` + `CategoryId` + `SubCategoryId` + `ItemId` — most specific override.

**Cascading fallback logic** (most specific wins, evaluated in `~/ITSM/BLL/DispatchService.cs`):

```
1. Item      → BranchId + CategoryId + SubCategoryId + ItemId
2. SubCategory → BranchId + CategoryId + SubCategoryId  (ItemId IS NULL)
3. Category  → BranchId + CategoryId                   (SubCategoryId IS NULL)
4. Branch Default → BranchId + IsBranchDefault = 1     (all taxonomy NULL)
```

If no mapping resolves, the ticket is created **unassigned** and flagged for the ITSM supervisor queue.

---

## 4. Core Transactional Entities (`itsm` Schema)

### 4.1 `itsm.Tickets`

The central record connecting legacy personnel/location data with ITSM workflows, request types, and dispatch rules.

| Column | Type | Notes |
| :--- | :--- | :--- |
| `Id` | `INT` PK | Identity |
| `TicketNumber` | `VARCHAR(10)` | Unique — dynamically generated (see below) |
| `RequestTypeId` | `INT` FK | → `itsm.MST_RequestType.Id` |
| `Title` | `NVARCHAR(200)` | Required |
| `Description` | `NVARCHAR(MAX)` | |
| `RequesterCode` | `NVARCHAR(50)` | FK → `dbo.MST_UserMaster.EmployeeCode` |
| `AssigneeCode` | `NVARCHAR(50)` NULL | FK → `dbo.MST_UserMaster.EmployeeCode` — set by Intelligent Dispatch Matrix |
| `PlantId` | `INT` FK NULL | → `dbo.MST_Plant.Id` |
| `BranchId` | `INT` FK | → `dbo.MST_Branch.Id` — from requester context; drives dispatch |
| `CategoryId` | `INT` FK | → `itsm.MST_Category.Id` |
| `SubCategoryId` | `INT` FK | → `itsm.MST_SubCategory.Id` |
| `ItemId` | `INT` FK NULL | → `itsm.MST_Item.Id` — finest taxonomy selection |
| `PriorityId` | `INT` FK | → `itsm.MST_Priority.Id` |
| `StatusId` | `INT` FK | → `itsm.MST_TicketStatus.Id` |
| `CreatedOn` | `DATETIME` | |
| `ResolvedOn` | `DATETIME` NULL | |
| `CreatedByCode` | `NVARCHAR(50)` | Audit — typically same as `RequesterCode` |
| `ModifiedOn` | `DATETIME` NULL | |
| `ModifiedByCode` | `NVARCHAR(50)` NULL | |

#### Dynamic ticket numbering

Ticket numbers are allocated by `~/ITSM/BLL/TicketNumberService.cs` at creation time. Format:

```
{Prefix}{YY}{MM}{Sequence}
```

| Segment | Length | Example | Source |
| :--- | :--- | :--- | :--- |
| `Prefix` | 2 chars | `CR` | `MST_RequestType.Prefix` |
| `YY` | 2 digits | `26` | Year of `CreatedOn` |
| `MM` | 2 digits | `08` | Month of `CreatedOn` |
| `Sequence` | 4 digits | `0001` | Monthly per-prefix counter |

**Example:** `CR26080001` = Change Request, August 2026, sequence 0001.

**Example:** `IR26080042` = Incident Request, August 2026, sequence 0042.

The BLL maintains a counter table or uses a transactional `MAX+1` query scoped to `(RequestTypeId, Year, Month)` inside `itsm.TicketNumberSequence` (optional helper table) to prevent duplicate numbers under concurrent creation.

### 4.2 `itsm.TicketApprovals`

Tracks multi-level approval workflows for **SR** and **CR** request types. **IR** tickets skip this table.

| Column | Type | Notes |
| :--- | :--- | :--- |
| `Id` | `INT` PK | Identity |
| `TicketId` | `INT` FK | → `itsm.Tickets.Id` |
| `ApprovalLevel` | `NVARCHAR(20)` | `Manager` or `ITHead` |
| `ApproverCode` | `NVARCHAR(50)` | FK → `dbo.MST_UserMaster.EmployeeCode` |
| `ApprovalStatus` | `NVARCHAR(20)` | `Pending`, `Approved`, `Rejected` |
| `Comments` | `NVARCHAR(500)` NULL | Approver remarks |
| `ActionedOn` | `DATETIME` NULL | When approved/rejected |
| `CreatedOn` | `DATETIME` | When approval row was created |

**Workflow rules (enforced in BLL):**

| Request Type | Manager Approval | IT Head Approval |
| :--- | :--- | :--- |
| `IR` | Not required | Not required |
| `SR` | Required (`ReportingManagerCode` from requester) | Not required |
| `CR` | Required | Required (configured IT Head per branch/plant) |

On ticket creation, the BLL inserts `TicketApprovals` rows with `ApprovalStatus = 'Pending'` and sets `Tickets.StatusId` to **Pending Approval** until all required levels are `Approved`. Rejection sets status to **Rejected** and notifies the requester via `NotificationQueue`.

### 4.3 `itsm.TicketComments`

Audit trail of communication between the requester and IT.

| Column | Type | Notes |
| :--- | :--- | :--- |
| `Id` | `INT` PK | Identity |
| `TicketId` | `INT` FK | → `itsm.Tickets.Id` |
| `CommentText` | `NVARCHAR(MAX)` | |
| `CreatedByCode` | `NVARCHAR(50)` | FK → `dbo.MST_UserMaster.EmployeeCode` |
| `CreatedOn` | `DATETIME` | |
| `IsInternal` | `BIT` | `1` = visible to IT staff only |

### 4.4 `itsm.TicketAttachments`

Tracks uploaded files on the IIS file system (not database blobs).

| Column | Type | Notes |
| :--- | :--- | :--- |
| `Id` | `INT` PK | Identity |
| `TicketId` | `INT` FK | → `itsm.Tickets.Id` |
| `FileName` | `NVARCHAR(255)` | |
| `FilePath` | `NVARCHAR(500)` | Relative path to secure upload folder |
| `UploadedByCode` | `NVARCHAR(50)` | FK → `dbo.MST_UserMaster.EmployeeCode` |
| `UploadedOn` | `DATETIME` | |

### 4.5 Entity Relationship Summary

```
dbo.MST_UserMaster ─────┬──< itsm.Tickets (RequesterCode, AssigneeCode)
                        ├──< itsm.TicketApprovals (ApproverCode)
                        └──< itsm.MST_SupportMapping (SupportAgentCode)

dbo.MST_Branch ─────────┬──< itsm.Tickets (BranchId)
                        └──< itsm.MST_SupportMapping (BranchId)

dbo.MST_Plant ─────────────< itsm.Tickets (PlantId)

itsm.MST_RequestType ──────< itsm.Tickets (RequestTypeId)

itsm.MST_Category ──┬──< itsm.MST_SubCategory ──┬──< itsm.MST_Item
                    │                            │
                    └──────────< itsm.Tickets ──┘ (CategoryId, SubCategoryId, ItemId)
                    └──────────< itsm.MST_SupportMapping

itsm.Tickets ──┬──< itsm.TicketComments
               ├──< itsm.TicketAttachments
               └──< itsm.TicketApprovals
```

---

## 5. Entity Relationships & Routing Logic

The EF6 navigation properties and BLL services in `~/ITSM/BLL/` power routing and workflow without ADO.NET `JOIN` statements in the UI layer.

### 5.1 Auto-Population (New Ticket Form)

When a user opens the "New Ticket" Web Form, the code-behind calls `TicketService.GetRequesterContext(Session["WORKMAN"].ToString())`:

1. EF6 loads the requester from `dbo.MST_UserMaster` (read-only).
2. `PlantId`, `BranchId`, and `DepartmentId` pre-fill hidden fields and drive downstream dropdowns.
3. `ReportingManagerCode` is cached for SR/CR approval row creation.

*(Note: `WORKMAN` is the legacy session key that stores the logged-in user's `EmployeeCode` — set by `login.aspx` alongside `Session["USERNAME"]` and `Session["LoginToken"]`.)*

### 5.2 Intelligent Dispatch Matrix

When a ticket is submitted, `DispatchService.ResolveAssignee(ticket)` queries `itsm.MST_SupportMapping` using the **cascading fallback**:

```csharp
// Pseudocode — lives in ~/ITSM/BLL/DispatchService.cs
public string ResolveAssignee(int branchId, int categoryId, int subCategoryId, int? itemId)
{
    // 1. Most specific: Item-level mapping
    if (itemId.HasValue)
    {
        var agent = FindMapping(branchId, categoryId, subCategoryId, itemId);
        if (agent != null) return agent;
    }

    // 2. SubCategory-level mapping
    var subAgent = FindMapping(branchId, categoryId, subCategoryId, itemId: null);
    if (subAgent != null) return subAgent;

    // 3. Category-level mapping
    var catAgent = FindMapping(branchId, categoryId, subCategoryId: null, itemId: null);
    if (catAgent != null) return catAgent;

    // 4. Branch default
    var defaultAgent = FindBranchDefault(branchId);
    if (defaultAgent != null) return defaultAgent;

    return null; // Unassigned — supervisor queue
}
```

**Outcome:** `Tickets.AssigneeCode` is set before the first `SaveChanges()`. The requester sees the assigned agent on the confirmation screen. A `NotificationQueue` row notifies the agent.

**Decentralized routing:** A user at `BranchId = 12` / `PlantId = 4` raises a Hardware → Printer → HP LaserJet ticket. Dispatch resolves the item-level mapping for Branch 12 first; if none exists, it walks up the cascade until a `SupportAgentCode` is found.

### 5.3 Approval Workflow Routing (SR / CR)

After dispatch assignment, the BLL evaluates `MST_RequestType`:

1. **IR** — Status moves directly to **New** (or **In Progress** if auto-assigned). No `TicketApprovals` rows.
2. **SR** — Inserts `TicketApprovals` row (`ApprovalLevel = Manager`, `ApproverCode = requester.ReportingManagerCode`). Status = **Pending Approval**. Manager action in `~/ITSM/UI/ApproveTicket.aspx` transitions to **In Progress** or **Rejected**.
3. **CR** — Inserts two rows: Manager (sequential step 1), then IT Head (step 2). IT Head `ApproverCode` resolved from branch-level IT Head configuration. Status remains **Pending Approval** until both levels approve.

### 5.5 SLA Calendar & Analytics (`dbo.MST_Calendar`)

The BLL queries `dbo.MST_Calendar` (read-only via EF6) to:

- **Calculate accurate SLA deadlines** — when applying `MST_Priority.SlaResolutionHours` to a ticket's `CreatedOn`, `SlaService` advances the deadline across business days only, skipping rows where `IsWeekend = 1`. This avoids naive `DATEADD` calculations that would over-count weekends.
- **Group dashboard analytics** — ticket volume, resolution time, and backlog reports aggregate by `FinancialYear` and `FinancialQuarter` from `MST_Calendar`, aligned with corporate reporting periods rather than calendar months alone.
- **Trigger scheduled maintenance tickets** — batch jobs (e.g. month-end or quarter-end windows flagged by `IsMonthEnd` / `IsQuarterEnd`) consult `MST_Calendar` to auto-raise recurring SR/CR tickets for preventive maintenance workflows.

### 5.6 Manual Assignee Override

IT supervisors may override `AssigneeCode` via the ticket detail page. Overrides are logged in `TicketComments` (`IsInternal = 1`) and do not alter `MST_SupportMapping` — they are ticket-level exceptions only.

### 5.7 EF6 Layer Responsibilities

| Layer | Routing responsibility |
| :--- | :--- |
| `~/ITSM/UI/` | Collects form input; calls BLL; displays resolved assignee and approval status |
| `~/ITSM/BLL/` | `DispatchService`, `TicketNumberService`, `ApprovalWorkflowService`, `SlaService` — all routing and SLA rules |
| `~/ITSM/DAL/` | EF6 queries against `itsm.*` and read-only `dbo.*`; no business logic |

---

## Related Documents

- [ADR 0001 — ITSM Application Architecture Strategy](../04-adrs/0001-itsm-architecture-strategy.md)
- [ADR 0003 — ITSM Data Store Strategy](../04-adrs/0003-itsm-data-store-strategy.md)
