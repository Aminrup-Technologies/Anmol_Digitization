# Phase 1: Data Architecture & Schema Design (MVP)

## Status
Accepted

## Context
Following [ADR 0001](../04-adrs/0001-itsm-architecture-strategy.md) and [ADR 0003](../04-adrs/0003-itsm-data-store-strategy.md), the ITSM module is built as a Modular Monolith within the `.NET 4.8` `AnmolDristi` solution. The ITSM Data Access Layer (`~/ITSM/DAL/`) uses **Entity Framework 6 (EF6)** to manage incident data in a dedicated `itsm` schema on the shared SQL Server database.

To prevent data duplication and enable intelligent UI pre-selections, the EF6 `ItsmDbContext` physically joins new `itsm` transactional tables directly to the legacy `dbo.MST_*` master tables.

---

## 1. Utilization of Existing Legacy Masters (Read-Only)
These tables exist in the `dbo` schema. EF6 maps to these using `[Table("...", Schema="dbo")]` and strictly treats them as `AsNoTracking()` read-only lookup data. The `SaveChanges()` pipeline forbids updates to these entities.

| Table Name | Primary Key | Key Columns Mapped | ITSM Utilization & Relationships |
| :--- | :--- | :--- | :--- |
| **`dbo.MST_UserMaster`** | `EmployeeCode` | `EmployeeName`, `Email`, `Mobile`, `PlantId`, `DepartmentId`, `ReportingManagerCode`, `IsActive` | **Core Identity:** Auto-fills requester details on the UI. Acts as the Foreign Key target for `Tickets.RequesterCode` and `Tickets.AssigneeCode`. |
| **`dbo.MST_Plant`** | `Id` | `PlantID_Description`, `SAP_Code` | **Primary Routing Context:** Maps to `Tickets.PlantId`. Determines if the ticket belongs to a Plant/Depot or the Corporate Office. |
| **`dbo.MST_Branch`** | `Id` | `Branch_Description` | **Secondary Location:** Granular location tracking mapped via `UserMaster.BranchId`. |
| **`dbo.MST_Department`**| `Id` | `Department_Description` | **Organizational Context:** Helps IT agents prioritize tickets based on the requester's department. |
| **`dbo.MST_Designation`**| `Id` | `Designation_Description` | **User Role:** Contextual lookup. *(Note: Legacy `MST_UserMaster.Designation_ID` stores ERP Identity; mapped carefully in EF6).* |

---

## 2. Shared Infrastructure (Write-Append)
| Table Name | Interaction | Purpose in ITSM |
| :--- | :--- | :--- |
| **`dbo.NotificationQueue`**| Insert Only | The ITSM BLL queues emails by inserting rows here with `SourceModule = 'ITSM'`. The legacy background worker handles actual SMTP dispatch, requiring zero new email logic. |

---

## 3. Introduction of New ITSM Masters (Read/Write via EF6)
These tables live in the `itsm` schema. They manage the specific taxonomies required for IT Service Management and drive the Web Forms `.aspx` dropdowns.

* **`itsm.MST_Category`**: `Id`, `Name`, `IsActive` *(e.g., Hardware, Software, Network)*
* **`itsm.MST_SubCategory`**: `Id`, `CategoryId` (FK to `MST_Category.Id`), `Name`, `IsActive` *(e.g., Printer, ERP Login)*
* **`itsm.MST_Priority`**: `Id`, `Name`, `SlaResolutionHours` *(e.g., P1 - Critical, P2 - High)*
* **`itsm.MST_TicketStatus`**: `Id`, `Name`, `IsTerminal` *(e.g., New, In Progress, Resolved)*

---

## 4. Core Transactional Entities (`itsm` Schema)

### `itsm.Tickets`
The central incident record connecting legacy personnel/location data with ITSM workflows.

* **`Id`** (PK, INT IDENTITY)
* **`TicketNumber`** (VARCHAR, e.g., 'INC-10001')
* **`Title`** (NVARCHAR(200))
* **`Description`** (NVARCHAR(MAX))
* **`RequesterCode`** (FK -> `dbo.MST_UserMaster.EmployeeCode`)
* **`AssigneeCode`** (FK -> `dbo.MST_UserMaster.EmployeeCode`, Nullable)
* **`PlantId`** (FK -> `dbo.MST_Plant.Id`) - *Crucial for decentralized routing*
* **`CategoryId`** (FK -> `itsm.MST_Category.Id`)
* **`SubCategoryId`** (FK -> `itsm.MST_SubCategory.Id`)
* **`PriorityId`** (FK -> `itsm.MST_Priority.Id`)
* **`StatusId`** (FK -> `itsm.MST_TicketStatus.Id`)
* **`CreatedOn`** (DATETIME)
* **`ResolvedOn`** (DATETIME, Nullable)

### `itsm.TicketComments`
Audit trail of communication between the requester and IT.

* **`Id`** (PK, INT IDENTITY)
* **`TicketId`** (FK -> `itsm.Tickets.Id`)
* **`CommentText`** (NVARCHAR(MAX))
* **`CreatedByCode`** (FK -> `dbo.MST_UserMaster.EmployeeCode`)
* **`CreatedOn`** (DATETIME)
* **`IsInternal`** (BIT) - *If true, only visible to IT staff, hidden from requester.*

### `itsm.TicketAttachments`
Tracks uploaded files. Stored on the IIS file system, not as database blobs, for performance.

* **`Id`** (PK, INT IDENTITY)
* **`TicketId`** (FK -> `itsm.Tickets.Id`)
* **`FileName`** (NVARCHAR(255))
* **`FilePath`** (NVARCHAR(500)) - *Relative path to the secure upload folder.*
* **`UploadedByCode`** (FK -> `dbo.MST_UserMaster.EmployeeCode`)
* **`UploadedOn`** (DATETIME)

---

## 5. Entity Relationships & Routing Logic
The EF6 Navigation Properties power the business logic without complex ADO.NET `JOIN` statements:

1. **Auto-Population:** `ItsmDbContext.UserMasters.FirstOrDefault(u => u.EmployeeCode == Session["LoginToken"].UserCode)` instantly retrieves the user's `PlantId` and `DepartmentId` to pre-fill the "New Ticket" Web Form.
2. **Decentralized Routing:** A ticket raised by a user with `PlantId = 4` automatically associates the `Ticket.PlantId` with `4`. 
3. **Assignee Filtering:** The UI dropdown for "Assign To" queries `MST_UserMaster` where `Department = 'IT'` AND `PlantId == Ticket.PlantId`, ensuring Corporate IT is not assigned Plant-level hardware issues.
