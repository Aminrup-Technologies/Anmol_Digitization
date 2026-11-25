# ITSM — Independent AS-IS Architecture Validation

**Date:** 2026-11-12  
**Scope:** `AnmolDristi/ITSM`, plus the permission SQL and the empty root script that the FreeBuff register cites.  
**Method:** Read-only inspection of executable C#, EF6 migrations, the EF seed, and the ASPX pages. Comments and design documents were used only where the code was silent, and were rejected where the code contradicts them.  
**Not done:** No source edit, no SQL, no connection to UAT or Production, no Web.config read, no runtime or browser pass. Deployed rows are therefore unverified even where the repository states an intended seed.

FreeBuff documents judged:

- `ITSM_AS_IS_Architecture.md`
- `ITSM_AS_IS_Workflows.md`
- `ITSM_AS_IS_Database.md`
- `ITSM_AS_IS_Security.md`
- `ITSM_AS_IS_Findings_and_Risks.md`
- `ITSM_Requirements_Gap_Analysis.md`

---

## 1. Executive summary

The module is a namespace-layered Web Forms feature inside the single `AnmolDristi` project. Persistence is EF6 Code First against schema `itsm`, with read-only maps of legacy `dbo` masters. Workflow state, ticket numbers, assignment, SLA, and audit are application code. There is no ITSM stored procedure in this module.

Most FreeBuff findings match the code. The material disagreement is the ticket-status seed.

FreeBuff treats `MST_TicketStatus.IsTerminal` as unknown and treats the entity comment ("e.g. Resolved / Closed / Rejected") as evidence that `Resolved` may be terminal, which would make `Resolved → Closed` dead. The repository does contain a seed. `AnmolDristi/ITSM/DAL/Migrations/Configuration.cs`, method `Seed`, inserts eight statuses only when the table is empty, and it sets `Resolved.IsTerminal = false`, with `Closed`, `Cancelled`, and `Rejected` set to `true`. Under that seed, the declared `Resolved → Closed` and `Resolved → In Progress` edges are reachable. The seed is not inside any migration `Up()`, and no `DbMigrator` or `SetInitializer` call exists in the application, so a database that already has status rows, or that never had `Update-Database` run, can differ. That deployed value remains unverified. The comment on `TicketStatus.IsTerminal` is not the seed.

A second overstatement is tenant access. Queues are cross-company. Opening a listed ticket is allowed for the requester, the assignee, and a pending approver without a company comparison, and for `ITSM.Ticket.View` when the ticket company matches the actor's current `MST_UserMaster.CompanyId`. The sentence that a listed cross-company ticket cannot be opened is broader than `ItsmTicketAccess.CanViewTicket`.

SLA start and due are computed and then used only in the creation email. They are not stored, and nothing detects a breach. The duration math itself does not walk plant open/close hours. Plant timing affects the start; the hour count then runs on the clock to midnight, skipping calendar weekends.

Attachments, CSAT, agent groups, SAP/Non-SAP routing, and closure automation are absent as behaviour. The attachment table and the CSAT columns exist.

---

## 2. Independent architecture observations

### 2.1 Shape

| Layer | What the code does |
|---|---|
| UI | `CreateTicket`, `TicketList`, `TicketDetail`, `ApprovalConfig`. All inherit `BasePage`. |
| BLL | `TicketService`, `TicketUpdateService`, `TicketWorkflow`, `TicketListService`, `TicketDetailService`, `TicketMasterDataService`, `DispatchService`, `SlaService`, `PriorityCalculationService`, `ApprovalConfigurationService`, `ItsmTicketAccess`, `ItsmTenantContext`. |
| DAL | `ItsmDbContext` on connection name `DbConn`. `SaveChanges` rejects writes to `UserMaster`, `Plant`, `Branch`, `Department`, `Calendar`, `PlantTimings`, and `Company`. |
| Schema | Five EF migrations, from `202608120550515_InitialItsmSchema` through `202608240451107_AddApprovalConfiguration`. |

`TicketWorkflow` is a static `IReadOnlyDictionary` of status names. It is not loaded from the database.

### 2.2 Ticket lifecycle (verified)

Declared map in `TicketWorkflow` static constructor:

| From | Allowed next names |
|---|---|
| New | In Progress, On Hold, Cancelled |
| In Progress | Resolved, On Hold, Cancelled |
| On Hold | In Progress, Cancelled |
| Resolved | Closed, In Progress |

`Pending Approval`, `Closed`, `Cancelled`, and `Rejected` are absent from the map.

`TicketUpdateService.ChangeStatus` order:

1. Load by id, require a rowversion, require requester or assignee, require non-null `Ticket.CompanyId`.
2. Refuse when `currentStatus.IsTerminal` is true.
3. Then call `TicketWorkflow.IsValidTransition`.
4. Require non-whitespace resolution text only when the target name equals `"Resolved"`.
5. Write `ResolutionText` and `ResolvedOn` for `"Resolved"`, `ClosedOn` for `"Closed"`.
6. Insert `TicketAuditLog` action `StatusChange` with `Detail = null`, then one `SaveChanges` and commit.

`GetAvailableStatuses` returns an empty list when the current status is terminal or when the current name is missing from the map. `TicketDetail.aspx.cs` `LoadTicket` hides `pnlWorkActions` when `IsTerminal` is true or `StatusName == "Pending Approval"`.

Repository seed (`Configuration.Seed`), applied only when `TicketStatuses` has no rows:

| Name | IsTerminal |
|---|---|
| New | false |
| Pending Approval | false |
| In Progress | false |
| On Hold | false |
| Resolved | false |
| Closed | true |
| Cancelled | true |
| Rejected | true |

Consequences that follow from the map plus this seed:

- `New → Resolved` is refused by `IsValidTransition`. The only map path to `Resolved` is `In Progress → Resolved`.
- `Resolved → Closed` is a declared edge, and the seed does not block it with `IsTerminal`.
- `Pending Approval` cannot be left through `ChangeStatus`. `ApproveTicket` and `RejectTicket` assign the next status by name and do not consult the map.
- Rejection reason is enforced in `RejectTicket` before the transaction: whitespace fails with "Rejection reason is mandatory...". The textbox has no `RequiredFieldValidator` and no `MaxLength`. The approval `Comments` column is `nvarchar(500)`. A longer reason fails EF validation and rolls the rejection back.
- `OpenOnly` queues use `!Status.IsTerminal`. Under this seed, a `Resolved` ticket stays on My Queue and All Open until it becomes `Closed`, `Cancelled`, or `Rejected`.

### 2.3 Tenant model

`Ticket.CompanyId` is a nullable snapshot of `MST_UserMaster.CompanyId` at create (`TicketService.CreateTicket`). Creation refuses a missing or non-positive requester company. Mutations call `RequireRecordedTenant` and refuse a null ticket company. They do not compare the actor's company to the ticket's company.

`CanViewTicket` allows the requester, the assignee, or a pending approver with no company test. The `ITSM.Ticket.View` path requires `ticketCompanyId == authorizedCompanyId`, where the authorized id is the live `ItsmTenantContext.ResolveCompanyId` lookup.

`TicketListService.BuildQueueQuery` has no `CompanyId` predicate. `GetCategoryHierarchy(int companyId)` never reads `companyId`. `MST_Category` has no company column. `GetActiveAgents` and `GetBranches` are global.

### 2.4 SLA

`SlaService.CalculateSla` opens its own `ItsmDbContext`. Start time uses `dbo.VMS_PlantTimings` for the supplied branch id and the English day name: before open defers to open, during hours starts immediately, after close searches up to 14 days for a non-weekend open time. Missing timing is treated as always open.

`AddBusinessHoursSkippingWeekends` then consumes `resolutionHours` as clock hours until midnight. It skips a day when `dbo.MST_Calendar.IsWeekend` is true, or Saturday/Sunday when no calendar row exists. It does not stop at `CloseTime`, and it does not resume at the next `OpenTime`.

`TicketService.CreateTicket` passes `requester.BranchId` into `CalculateSla` and `branch.Id` into `DispatchService.ResolveAssignee`. `Branch` documents these as different keys: `MST_UserMaster.BranchId` stores legacy `Branch_ID`; `Tickets.BranchId` and `MST_SupportMapping.BranchId` store `MST_Branch.Id`.

The `SlaResult` is passed only to `EnqueueTicketCreatedNotification`, which writes the two timestamps into `dbo.NotificationQueue.Body`. `Tickets` has no SLA columns in any migration. No method reads a stored due date or raises a breach.

### 2.5 Assignment

`DispatchService.ResolveAssignee` cascades, most specific first: item, subcategory with null item, category with null subcategory and item, then branch default. Each step is `FirstOrDefault` with no `OrderBy`. `MST_SupportMapping` has no unique index on that natural key and no `RequestTypeId`. `BranchId` is non-null. The resolved code is stored on `Tickets.AssigneeCode` and is not checked for `EmployeeStatus`.

Manual reassignment (`ReassignTicket`) accepts any `MST_UserMaster` row with `EmployeeStatus == "Active"`. `GetActiveAgents` returns that same population. There is no agent, group, team, or hierarchy entity.

### 2.6 Authorization

| Operation | Gate |
|---|---|
| Create page | Session `WORKMAN`, `USERID`, and `BRANCH` present. No `HasPermission`. |
| Create write | Server re-validates hierarchy and master ids inside `CreateTicket.aspx.cs` before `TicketService.CreateTicket`. Actor code is `Session["WORKMAN"]`. |
| Queue tabs | `BindQueue` requires `ITSM.Queue.View` for My Queue and All Open, `ITSM.Approval.View` for Approvals. My Tickets is any signed-in employee, filtered by `RequesterCode`. |
| Detail view | `CanViewTicket` as in §2.3. |
| Status, reassign, priority override, comment | `CanProcessTicket`: requester or assignee only. No permission argument. |
| Approve / reject | `CanApproveTicket`: pending approval row, and the actor must not be the requester. |
| Approval configuration | `RequirePermission("ITSM.ApprovalConfig.Manage")` on load and on each save. |

`ItsmTenantContext.IsActiveEmployee` has no caller. Page loads do not check `EmployeeStatus`.

`BasePage.EnsureSessionValid` returns immediately when `Session["LoginToken"]` is null. A database exception in that method is swallowed. Permissions are cached in `Session["PH_UserPermSet"]` until `RefreshUserPermissions`, which the ITSM pages do not call.

No ITSM page sets an anti-forgery token or `ViewStateUserKey`.

### 2.7 Attachments and audit

`itsm.TicketAttachments` is created by the initial migration: `TicketId`, `FileName nvarchar(255)`, `FilePath nvarchar(500)`, `UploadedByCode`, `UploadedOn`, with FKs to `Tickets` and `MST_UserMaster`. `TicketAttachment` and `ItsmDbContext.TicketAttachments` match that shape. No ITSM page, service, or handler inserts or reads a row. There is no upload control and no download handler, so there is no file authorization path to assess.

`TicketAuditLog` rows are inserted in the same `DbContextTransaction` as the mutation. Create uses several `SaveChanges` calls and one commit; a later exception calls `Rollback`. Each update method adds the audit entity and commits in one `SaveChanges`. There is no trigger, `DENY`, or append-only grant in the migrations. The BLL never updates an audit row. `ChangeStatus` stores the status names in `OldValue`/`NewValue` and leaves `Detail` null, so `ResolutionText` is not in the audit row. `AddAudit` truncates `Detail` to 1000 characters.

### 2.8 Transactions that do not cover the reads

`PriorityCalculationService.CalculatePriorityId`, `SlaService.CalculateSla`, and `DispatchService.ResolveAssignee` each open a separate context. Those reads are not enlisted in the create transaction. The ticket, audit, approval rows, and the `NotificationQueue` insert are enlisted. A failure in that insert rolls the ticket back, because `ExecuteSqlCommand` runs before `Commit`.

---

## 3. FreeBuff finding-by-finding validation

Verdicts: **Confirmed**, **Partially Confirmed**, **Refuted**, **Unverifiable**. "Partially Confirmed" means the code fact holds and a stated consequence or scope does not.

### 3.1 Findings register (`FR`)

| ID | Verdict | Evidence and actual behaviour |
|---|---|---|
| FR-01 | Confirmed | `TicketService.GenerateTicketNumber` reads matching numbers, parses the last 4 characters only when `Length >= 10`, adds 1, and inserts. `202608120550515_InitialItsmSchema` defines `TicketNumber varchar(10)` with no unique index. `BeginTransaction()` sets no isolation level. A one-character prefix after `Trim` yields a 9-character number, which the parser skips, so the next value stays `0001`. `Prefix` is `char(2)`; `Trim` can shorten a space-padded value. |
| FR-02 | Partially Confirmed | `TicketDetail.aspx.cs` `LoadTicket` assigns `Description`, `Title`, `ResolutionText`, CSAT feedback, and lookup names to `Label.Text`. Comments and approval comments in `TicketDetail.aspx` call `HttpUtility.HtmlEncode`. `Label` is an unencoded sink. No exploit was executed. The pages do not set `ValidateRequest="false"`. Whether request validation blocks a payload depends on configuration in `Web.config`, which was not read. |
| FR-03 | Confirmed | `RequireRecordedTenant` fails all six mutations when `CompanyId` is null. Migration `202608210700000_AddTicketCompanyId` adds a nullable column and does not backfill. |
| FR-04 | Partially Confirmed | The `IsTerminal` check does run before `IsValidTransition`, in both `ChangeStatus` and `GetAvailableStatuses`. The map does declare `Resolved → Closed` and `Resolved → In Progress`. The entity comment is an example, not a seed. `Configuration.Seed` sets `Resolved` to non-terminal. Under that seed the edges are live. A database whose status rows already existed is not updated by this seed. See §4. |
| FR-05 | Confirmed | `Pending Approval` is not a map key. The work-action panel is hidden for that name. Only `ApproveTicket` and `RejectTicket` change the status, and only when a pending row matches the actor. No administrative bypass exists. Creation blocks a requester who is their own manager. It does not block a requester who is the configured IT Head. |
| FR-06 | Confirmed | `CanProcessTicket` is requester or assignee. `ITSM.Ticket.View` is consulted only by `CanViewTicket`. A requester who passes `CanProcessTicket` can change status, reassign, override priority, and comment. |
| FR-07 | Partially Confirmed | The values are not stored and have no breach reader. The email body is the only consumer. Plant hours apply to the start time. The added duration is clock time to midnight with weekend skips, not a plant-hours calendar. See §4. |
| FR-08 | Partially Confirmed | `BuildQueueQuery` has no company predicate, and all four list methods use it. The grid shows title, requester, assignee, category, and branch, and links every row to `TicketDetail.aspx?id=`. My Queue rows are openable because the viewer is the assignee. Pending approvals are openable through `HasPendingApproval`. Same-company `ITSM.Ticket.View` can open a row. The blocked case is a queue viewer who is none of those and whose company differs. |
| FR-09 | Confirmed | `DispatchService` uses unordered `FirstOrDefault`. The initial migration indexes `MST_SupportMapping` columns separately and does not unique them. `TicketApprovals` likewise has no unique key; `ApproveTicket` / `RejectTicket` use `FirstOrDefault` on ticket, approver, and `Pending`. Whether duplicate rows exist is unverified. |
| FR-10 | Confirmed | `ChangeStatus` calls `AddAudit(..., detail: null)`. `ResolutionText` is stored on `Tickets` only. |
| FR-11 | Confirmed | `GetActiveAgents` and `ReassignTicket` use active employees. `ddlReassignTo` is bound from that list inside the work-action panel, so a requester receives the full active directory. |
| FR-12 | Confirmed | `CsatScore` and `CsatFeedback` are selected in `TicketDetailService.GetTicketDetail` and displayed. No assignment to either property exists under `AnmolDristi/ITSM`. No closure scheduler exists. `Closed` is reachable only through `ChangeStatus` when the map and `IsTerminal` allow it. |
| FR-13 | Confirmed | Table, entity, and `DbSet` exist. No upload, download, or validation service exists under `AnmolDristi/ITSM`. |
| FR-14 | Confirmed | No anti-forgery token and no `ViewStateUserKey` in the ITSM pages. Framework ViewState MAC was not tested, and `Web.config` was not read. |
| FR-15 | Confirmed | `CreateTicket.Page_Load` checks three session keys and never calls `HasPermission` or `RequirePermission`. `UI.ITSM_CreateTicket.View` is inserted and granted to `ITSM_Agent` and `ITSMAdmin` by SQL that this page does not consult. |
| FR-16 | Confirmed | `BasePage.UserPermissionSet` rebuilds only when the session set is missing. |
| FR-17 | Confirmed | The only `NotificationQueue` write in the module is `EnqueueTicketCreatedNotification`. |
| FR-18 | Confirmed | The email href is the literal `"~/ITSM/UI/TicketDetail.aspx?id=" + ticket.Id`. The browser redirect in `CreateTicket.aspx.cs` uses `ResolveUrl`; the email path does not. |
| FR-19 | Confirmed | `TicketList.BindQueue` catch sets `"Error loading ticket queue: " + ex.Message`. `ApprovalConfig.aspx.cs` also surfaces `ex.Message` on policy updates. `CreateTicket` surfaces `InvalidOperationException.Message` and `ArgumentException.Message`. |
| FR-20 | Confirmed | `BuildQueueQuery` applies `ToLower().Contains` to `Title`, `TicketNumber`, and `Description`. |
| FR-21 | Confirmed | `GetCategoryHierarchy` filters `IsActive` only. `Category` has `Id`, `Name`, `IsActive`. The method's own comment says company scoping applies when `companyId > 0`. The parameter is unused for every value. |
| FR-22 | Confirmed | `semanticLevel` in `CreateTicket.aspx` matches `"high"`, `"medium"`, `"low"`, then falls back to index. `TicketLookupItemDto` is `Id` and `Name`. `GetImpacts` / `GetUrgencies` order by `SortOrder` and then drop it. |
| FR-23 | Partially Confirmed | The two integers are real: legacy `requester.BranchId` for SLA, `MST_Branch.Id` for dispatch. `Branch` and `PlantTimings` document why a split can be correct. Whether `VMS_PlantTimings.Branch_ID` stores the legacy key is a data fact and is unverified. |
| FR-24 | Confirmed | `ITSM_TicketView_Permission.sql` comments name "ITSM Manager" and "General Employee". `itsm_role_permissions.sql` assigns keys to `RoleKey` `ITSM_Agent` and `ITSMAdmin` only. Neither script creates those commented role names. |
| FR-25 | Confirmed | `itsm_phase1b_migration.sql` at the repository root is 0 bytes and is tracked. `AnmolDristi/Database/itsm_phase1b_permissions.sql` is a different, non-empty file. |
| FR-26 | Confirmed | Audit immutability is a comment on `TicketAuditLog`. Migrations add indexes only. `ItsmDbContext.SaveChanges` does not special-case audit rows. |
| FR-27 | Confirmed | `Description` and `ResolutionText` are unbounded strings in the initial migration. `txtDescription` has no `MaxLength`. `TicketService.CreateTicket` trims the title and does not length-check the description. |
| FR-28 | Confirmed | `OnModelCreating` removes `OneToManyCascadeDeleteConvention`. No soft-delete column exists on `Ticket`. |

### 3.2 Architecture (`ARC`)

| ID | Verdict | Note |
|---|---|---|
| ARC-01 | Confirmed | BLL files under `AnmolDristi/ITSM/BLL` do not reference `System.Web`. Session and `HasPermission` stay in the pages and `BasePage`. |
| ARC-02 | Confirmed | `AssertNoLegacyMasterWrites` throws on Added/Modified/Deleted legacy masters, including EF proxies via `is`. |
| ARC-03 | Confirmed | Priority, SLA, and dispatch each construct `new ItsmDbContext()` while create holds a different context and transaction. |
| ARC-04 | Confirmed | `GenerateTicketNumber` is private on `TicketService`. There is no `TicketNumberService` type in the module. |
| ARC-05 | Confirmed | Same as FR-21. |
| ARC-06 | Partially Confirmed | Non-persistence is confirmed. Duration math is narrower than "working hours". Same correction as FR-07. |
| ARC-07 | Confirmed | Hierarchy is serialized once for the browser. `CreateTicket.aspx.cs` calls `ValidateHierarchy` before create. |
| ARC-08 | Confirmed | Queue comments and `BuildQueueQuery` are tenant-agnostic. Detail permission path is tenant-scoped. Relationship view is not. |
| ARC-09 | Confirmed | UI, BLL, and DAL are namespaces in the web project. |
| ARC-10 | Confirmed | Same file as FR-25. |

### 3.3 Workflow (`WF`)

| ID | Verdict | Note |
|---|---|---|
| WF-01 | Confirmed | UI validation plus `TicketService.CreateTicket` in one `BeginTransaction`, then redirect. |
| WF-02 | Confirmed | Same mechanism as FR-01. |
| WF-03 | Confirmed | Branch cascade, no group, unordered `FirstOrDefault`. Includes WF-03a through WF-03d. |
| WF-04 | Partially Confirmed | Gate order is confirmed. "May be unreachable" is true only when the deployed `Resolved.IsTerminal` is 1. The repository seed sets 0. |
| WF-05 | Confirmed | WF-05a through WF-05d match `ApproveTicket` / `RejectTicket` and `ResolveActiveItHead`, which takes the single active `ApprovalConfiguration` row. The filtered unique index is in `202608240451107_AddApprovalConfiguration`. |
| WF-06 | Confirmed | Same as FR-03. |
| WF-07 | Confirmed | No CSAT writer, no auto-close, no service that forces Resolved before Closed beyond the map itself. |
| WF-08 | Partially Confirmed | Same SLA correction as FR-07. |
| WF-09 | Partially Confirmed | Same as FR-23. |
| WF-10 | Confirmed | Same as FR-10. |
| WF-11 | Confirmed | `SupportMapping` has no `RequestTypeId`. Request type carries approval flags and the number prefix. |
| WF-12 | Confirmed | Same as FR-17 and FR-18. |
| WF-13 | Confirmed | `RejectTicket` marks every other pending row `Rejected` and moves the ticket to `Rejected`, or `Cancelled` when `Rejected` is missing. There is no resubmit method. If both status names are missing, the approval rows still update and the ticket status stays `Pending Approval`. |

### 3.4 Database (`DB`)

| ID | Verdict | Note |
|---|---|---|
| DB-01 | Confirmed | The `itsm` tables in scope are created by the five EF migrations. |
| DB-02 | Confirmed | No unique index on `TicketNumber`. |
| DB-03 | Confirmed | Number allocation is the C# read-modify-write in FR-01. |
| DB-04 | Confirmed | No SLA columns on `Tickets`. |
| DB-05 | Confirmed | `MST_RequestType`, `MST_Priority`, `MST_Impact`, and `MST_Urgency` primary keys are identity `Id` only. |
| DB-06 | Confirmed | `SupportMapping.BranchId` is non-nullable. |
| DB-07 | Confirmed | No natural-key unique index. |
| DB-08 | Confirmed | `TicketApprovals` indexes are `TicketId` and `ApproverCode` separately. |
| DB-09 | Confirmed | Same as FR-26. |
| DB-10 | Confirmed | Three fixed tiers. No `ParentId`, `Level`, `IsRoot`, or `CompanyId`. |
| DB-11 | Confirmed | Columns exist and are read. No writer. |
| DB-12 | Confirmed | No ITSM procedure, view, or trigger is defined under `AnmolDristi/ITSM` or by these migrations. `Database/` contains ITSM permission scripts, not workflow procedures. |
| DB-13 | Confirmed | Same as FR-25. |
| DB-14 | Confirmed | Nullable `Tickets.CompanyId`, no FK. Detail joins `MST_Company.Company_ID` with `DefaultIfEmpty`, and the badge falls back to `"Company " + id`. |
| DB-15 | Confirmed | Metadata table is complete. No writer. |
| DB-16 | Confirmed | Same as FR-27. |
| DB-17 | Confirmed | Cascade convention removed. Child FKs are required, so a parent delete fails while children exist. |

The schema-versus-document rows in Database §7 match the code where they describe missing types (`TicketNumberService`, `TicketClosureService`, `AttachmentValidationService`, `DownloadAttachment.ashx`) and the global IT Head row. Those document names are not types in `AnmolDristi/ITSM`.

### 3.5 Security (`SEC`)

| ID | Verdict | Note |
|---|---|---|
| SEC-01 | Partially Confirmed | Same boundary as FR-02. |
| SEC-02 | Confirmed | Same as FR-06. |
| SEC-03 | Partially Confirmed | Cross-company listing is confirmed. "All four tabs" list cross-company rows. Opening is relationship- or same-company-permission-based, as in FR-08. |
| SEC-04 | Confirmed | Same as FR-15. |
| SEC-05 | Confirmed | Same as FR-03. Fail-closed, with no remediation method. |
| SEC-06 | Confirmed | Same as FR-16. |
| SEC-07 | Confirmed | Actor `EmployeeStatus` is not checked on page load or in `LoadForProcess` / `LoadForApprove`. The unused helper is `ItsmTenantContext.IsActiveEmployee`. |
| SEC-08 | Confirmed | `EnsureSessionValid` returns when `LoginToken` is null, and the `catch` does not log the user out. |
| SEC-09 | Confirmed | Same as FR-14. |
| SEC-10 | Confirmed | Same as FR-19. |
| SEC-11 | Confirmed | Actor code is `Session["WORKMAN"]`. Ticket id, status id, and assignee code come from the post, and are then checked against stored relationships. Company id is not accepted from the browser. |
| SEC-12 | Confirmed | Each mutation reloads the row inside its transaction and re-evaluates `CanProcessTicket` or `CanApproveTicket`. |
| SEC-13 | Confirmed | `LoadMutableTicket` assigns the posted token to `RowVersion.OriginalValue`. `DbUpdateConcurrencyException` rolls back. This was read, not executed. |
| SEC-14 | Confirmed | No attachment implementation, so no attachment control is in force. |

### 3.6 Requirements gaps (`GAP` and `X`)

| ID | FreeBuff classification | Verdict | Note |
|---|---|---|---|
| GAP-01 | Partially Exists | Confirmed | Three-level hierarchy, client selection, server `ValidateHierarchy`, persisted ids. No IT-root flag and no tenant column. |
| GAP-02 | Already Exists | Confirmed | Title required on client and in `TicketService`. Item optional and checked when supplied. Impact, urgency, and request type must be active. |
| GAP-03 | Missing | Confirmed | Same as FR-13. |
| GAP-04 | Missing | Confirmed | Success path redirects to detail. Every `ShowMessage` call in `CreateTicket.aspx.cs` passes `success: false`. No confirmation dialog. |
| GAP-05 | Partially Exists | Confirmed | Create-page colour is name-derived. Detail impact/urgency is plain text. The list shows a priority badge only. |
| GAP-06 | Missing | Partially Confirmed | No agent group and no SAP/Non-SAP routing column. The claim of zero `SAP` tokens in `AnmolDristi/ITSM` is false: `Plant.SAP_Code` maps `dbo.MST_Plant.SAP_Code`. That field is not used by dispatch. |
| GAP-07 | Partially Exists | Confirmed | Cascade assignment exists. It is branch-mandatory, unordered, and has no maintenance page. |
| GAP-08 | Partially Exists | Confirmed | Assignment is independent of request type. No agent hierarchy exists to modify. |
| GAP-09 | Missing | Confirmed | Resolution capture is `ResolutionText` only. |
| GAP-10 | Missing | Confirmed | Same as FR-06. |
| GAP-11 | Missing | Confirmed | `New → Resolved` and `On Hold → Resolved` are absent from the map and fail `IsValidTransition`. This is the implemented rule. It is not evidence that `Resolved → Closed` is dead. |
| X1 | — | Confirmed | Workflow guarantees are C#. |
| X2 | Seed unknown | Refuted as a source claim | The seed values are in `Configuration.Seed`. They are unknown only on a deployed database. |
| X3 | — | Confirmed | No CSAT writer. |
| X4 | — | Confirmed | SLA result is not persisted. |
| X5 | — | Confirmed | No support-mapping admin page. |
| X6 | — | Confirmed | Request type is prefix plus two approval flags. |
| X7 | — | Confirmed | Categories are global. |

---

## 4. Disputed findings

### 4.1 `Resolved` is not an unknown seed

FreeBuff FR-04, WF-04, Database D3, Architecture A3, and gap observation X2 say the repository has no status seed and that closure depends on an unknown `IsTerminal` flag. The entity comment is cited as implying `Resolved` is terminal.

`Configuration.Seed` is executable seed logic:

```csharp
if (!context.TicketStatuses.Any())
{
    context.TicketStatuses.AddOrUpdate(
        s => s.Name,
        new TicketStatus { Name = "New", IsTerminal = false, IsActive = true },
        new TicketStatus { Name = "Pending Approval", IsTerminal = false, IsActive = true },
        new TicketStatus { Name = "In Progress", IsTerminal = false, IsActive = true },
        new TicketStatus { Name = "On Hold", IsTerminal = false, IsActive = true },
        new TicketStatus { Name = "Resolved", IsTerminal = false, IsActive = true },
        new TicketStatus { Name = "Closed", IsTerminal = true, IsActive = true },
        new TicketStatus { Name = "Cancelled", IsTerminal = true, IsActive = true },
        new TicketStatus { Name = "Rejected", IsTerminal = true, IsActive = true });
}
```

File: `AnmolDristi/ITSM/DAL/Migrations/Configuration.cs`, method `Seed`.

The same file records why this must not be treated as the live table: an earlier unconditional `AddOrUpdate` overwrote production `IsActive` / `IsTerminal`, and the current method returns immediately when any status row exists. None of the five migration `Up()` methods insert status rows. The application source contains no `MigrateDatabaseToLatestVersion` and no `Database.SetInitializer` call. `Seed` runs when EF's migrator is invoked, not on ordinary page requests.

`TicketStatus.IsTerminal`'s comment ("e.g. Resolved / Closed / Rejected") does not assign the column.

What remains true, and is not disputed: if a deployed `Resolved` row has `IsTerminal = 1`, `ChangeStatus` and `GetAvailableStatuses` will ignore the map and `Closed` will be unreachable. That is a dual control. The repository's own seed chooses the non-blocking side.

### 4.2 A listed cross-company ticket can be opened

FR-08 says detail access requires `ticket.CompanyId == authorizedCompanyId`, "so a listed ticket cannot be opened."

`ItsmTicketAccess.CanViewTicket` returns true before that comparison when the actor is the requester, the assignee, or has a `TicketApprovals` row in `Pending`. `GetAgentQueue` filters to the actor's `AssigneeCode`, so every My Queue row passes the assignee path. `GetApprovalQueue` filters to the actor's pending approvals, so those rows pass `HasPendingApproval`. `GetMyTickets` filters to the actor's `RequesterCode`.

The company comparison applies to the permission path only. Cross-company metadata in All Open is still a disclosure. It is not, by itself, an open denial.

### 4.3 SLA "working hours" describes the start, not the duration

FR-07, ARC-06, and WF-08 correctly say the result is discarded and that breach detection does not exist.

`SlaService.AddBusinessHoursSkippingWeekends` advances a cursor by `min(remaining, hours until next midnight)` and skips weekend dates. `CloseTime` is read only in `ResolveSlaStartTime`. An SLA that starts during the day can fall past plant close and into the next calendar morning without waiting for the next `OpenTime`. Calling the whole calculation a working-hours calendar overstates what the method does.

### 4.4 "Zero SAP tokens" is false; SAP agent grouping is still absent

GAP-06 reports zero `SAP` matches under `AnmolDristi/ITSM`. `Plant.SAP_Code` is mapped in `AnmolDristi/ITSM/DAL/Entities/Plant.cs`. Dispatch, support mapping, and the ticket form do not read it. The missing capability stands. The search claim does not.

---

## 5. Additional findings

These are present in the source and are absent from the FreeBuff registers, or are only implied there.

| ID | Area | Behaviour |
|---|---|---|
| AV-01 | Approval exit | `ApproveTicket` moves to `"In Progress"` only when that active row exists. If it does not, the approval row is still saved and the ticket remains `Pending Approval`. After the last pending row is cleared, `CanApprove` is false and `pnlWorkActions` stays hidden. The ticket has no further exit. |
| AV-02 | Rejection exit | `RejectTicket` looks up `"Rejected"`, then `"Cancelled"`. If neither exists, approval rows are marked `Rejected` and `StatusId` is left unchanged, with the same dead-end as AV-01. |
| AV-03 | IT Head separation of duties | Create rejects a requester whose reporting manager is themselves. It does not compare the requester with `ResolveActiveItHead`. A later `ApproveTicket` then rejects that person, which can leave their level pending forever. |
| AV-04 | Approval comments | `btnApprove_Click` calls `ApproveTicket` with `comments: null`. The detail page has no approval-comment field. |
| AV-05 | Rejection length | `TicketApproval.Comments` is 500 characters. The rejection textbox has no `MaxLength`. `RejectTicket` assigns `reason.Trim()` without truncating. EF string-length validation fails the save, and the transaction rolls back. |
| AV-06 | Priority ties | `PriorityCalculationService.CalculatePriorityId` uses `FirstOrDefault` on `ImpactId` and `UrgencyId`. `MST_PriorityMatrix` has no unique index on that pair. Duplicate matrix rows make priority non-deterministic, then `SlaResolutionHours` follows that priority. |
| AV-07 | Directory disclosure | The reassignment dropdown is bound whenever work actions are visible, including for the requester, from every active employee. |
| AV-08 | Search oracle | All Open search matches `Description` even though `TicketListItemDto` does not return it. A match discloses that the hidden text contains the search string, together with the visible title and names. |
| AV-09 | Create permission defined and unused | `Database/Scripts/010_ITSM_Sidebar_Permissions.sql` and `itsm_role_permissions.sql` define `UI.ITSM_CreateTicket.View`. The page never checks it. Menu hiding, if implemented from that key, does not bind the URL. |
| AV-10 | Notification failure blocks create | The queue insert runs inside the create transaction. A SQL error rolls back the ticket. An empty recipient email returns without inserting and still commits the ticket. |
| AV-11 | Open queues include Resolved | Under the repository seed, `GetAgentQueue` and `GetOpenQueue` set `OpenOnly`, which excludes only terminal statuses. `Resolved` remains listed. |
| AV-12 | Status-name fallback | `ResolveInitialStatusId` falls back to the lowest active non-terminal id when `"New"` or `"Pending Approval"` is missing. Approval rows can then exist on a ticket whose status is not `Pending Approval`, which shows work actions and approval actions together. |
| AV-13 | Audit action names | `TicketAuditLog` comments list an action type `Resolution`. `ChangeStatus` writes `StatusChange`. No method writes `Resolution`. |
| AV-14 | Approval Config session | `ApprovalConfig.Page_Load` requires `USERID` and `BRANCH`, then the manage permission. It does not require `WORKMAN`. The other three pages require all three session keys. |
| AV-15 | Seed is not an app startup behaviour | Because no initializer is registered, deploying the web application does not apply `Configuration.Seed`. |

---

## 6. Security and data-integrity risks

Ordered by what the current code allows a signed-in user, or a concurrent request, to cause.

1. **Ticket numbers can duplicate.** Two creates for the same request type and month can read the same maximum. Nothing in the schema rejects the duplicate. A one-character prefix after trim makes every such create attempt `0001` and collide with the first row. Past 9999, `D4` emits five digits and the `varchar(10)` insert fails.
2. **Unencoded ticket text on the detail page.** Title, description, and resolution text are stored and written to `Label.Text`. Comments on the same page are encoded. Exploitability through a normal postback depends on request validation, which was not executed.
3. **Cross-company queue disclosure.** All Open returns other companies' ticket numbers, titles, requester names, assignee names, categories, and branches to any holder of `ITSM.Queue.View`. Search can probe description text.
4. **Requester can resolve, reassign, and override priority on their own ticket.** No agent permission is required. The override reason is audited. The resolution text is not copied into the audit row.
5. **Processing ignores company.** A requester or assignee can mutate a ticket whose `CompanyId` is a different company from the actor's current HR company, provided the snapshot is non-null.
6. **Null `CompanyId` freezes the ticket.** Historical rows added before the nullable column cannot be approved, rejected, commented, reassigned, or moved.
7. **Pending approval can strand.** No map exit. An inactive approver, a requester configured as IT Head, or a missing `In Progress` / `Rejected` status name leaves the ticket with no work actions.
8. **Assignment and priority can flip between equal rows.** No unique constraint and no `OrderBy`.
9. **SLA cannot be enforced.** Due time exists in an email body. Nothing stores it or compares it with the clock. The duration math also ignores plant close.
10. **Session and permission freshness.** Missing `LoginToken` skips `EnsureSessionValid`. Permission changes wait for a new session cache. Create is available to any session that has the three keys, regardless of `UI.ITSM_CreateTicket.View` and regardless of `EmployeeStatus`.
11. **Exception text on the queue and on approval configuration.** `ex.Message` is written to the page.
12. **Audit can be updated by any future EF code.** Current methods only insert, and they insert in the same transaction as the business write, so a failed mutation does not leave an audit row behind. The database does not enforce that.

Attachment malware, path traversal, and download authorization have no implementation to attack. That absence is also a functional gap: the table does not make files safe, because no file path is written.

---

## 7. Unverified items requiring database or runtime access

No query was sent to UAT or Production. `Web.config` was not read.

| Item | Why the repository cannot close it |
|---|---|
| Deployed `MST_TicketStatus` names and `IsTerminal` | `Seed` runs only on an empty table, and only when the EF migrator runs. |
| Whether `Update-Database` has been applied, and which migration is current | No environment catalog was read. |
| Duplicate `TicketNumber` values | Requires a data query. |
| `MST_RequestType.Prefix` contents | A stored one-character or space-padded prefix makes FR-01's sequence stall certain. |
| `MST_SupportMapping` rows per branch, including duplicate natural keys | No seed. |
| `MST_PriorityMatrix` duplicate `(ImpactId, UrgencyId)` rows | No unique index in the migration. |
| `VMS_PlantTimings.Branch_ID` key space versus `MST_Branch.Id` and `MST_Branch.Branch_ID` | Entity comments describe the split. Row values were not read. |
| `MST_Calendar` coverage | Missing dates fall back to Saturday and Sunday. |
| Tickets with `CompanyId` null | The migration does not backfill. The count is a data question. |
| Whether `ITSM.Ticket.View`, `ITSM.Queue.View`, and `ITSM.Approval.View` are actually granted | Scripts exist. Execution is not evidenced here. |
| `dbo.NotificationQueue` column list versus the insert | The insert is inline SQL. The table definition was not opened in this pass. |
| Request-validation mode and ViewState MAC settings | They live in configuration that this review did not open. |
| A demonstrated XSS or CSRF payload | Not executed. |
| IIS behaviour of the empty `itsm_phase1b_migration.sql` | The file has no statements. Whether anyone runs it is operational. |

---

## 8. Recommended owner decisions

These are decisions the code cannot settle. They are not a design.

| # | Decision | What the code does today |
|---|---|---|
| D-1 | Is the repository seed the rule for `Resolved`? | `Configuration.Seed` says `Resolved` is not terminal, so `Closed` and `In Progress` are allowed. The flag still overrides the map. A live row with `IsTerminal = 1` strands the ticket. Confirm the deployed row before treating closure as either guaranteed or impossible. |
| D-2 | Is `New → Resolved` forbidden on purpose? | The map forbids it. `In Progress` is the only predecessor of `Resolved`. |
| D-3 | May All Open show other companies' titles and names? | It does, for `ITSM.Queue.View`. Detail open is still allowed to the requester, assignee, pending approver, and same-company `ITSM.Ticket.View`. |
| D-4 | May the requester resolve, close, reassign, and override priority? | `CanProcessTicket` allows it. |
| D-5 | Who may process a ticket they do not own? | `ITSM.Ticket.View` does not grant processing. There is no agent-role check. |
| D-6 | What an agent hierarchy would be | No group, team, SAP flag, or reporting line is used in assignment. Support mapping is one employee code per branch and category tier. |
| D-7 | Must SLA be stored and checked? | It is calculated, emailed, and dropped. Breach detection does not exist. Duration is not plant-close-aware. |
| D-8 | Are attachments and CSAT in this release? | The table and the columns exist. No writer exists. |
| D-9 | Who maintains `MST_SupportMapping`? | No ITSM page writes it. Ties are unordered. |
| D-10 | What should happen to tickets whose `CompanyId` is null? | Every mutation refuses them. The migration does not backfill. This review does not choose a backfill. |
| D-11 | Should create require `UI.ITSM_CreateTicket.View` and an active employee? | The permission script and `IsActiveEmployee` exist. The page uses neither. |
| D-12 | Is a global IT Head, rather than one per branch, the approval rule? | `ResolveActiveItHead` loads the single active `MST_ApprovalConfiguration` row. The filtered unique index allows only one active row. |

---

## Verdict

FreeBuff's structural reading of the module is largely accurate: application-owned workflow, relationship-based processing, cross-company queues, unpersisted SLA, schema-only attachments, and no database uniqueness on ticket numbers or support mappings.

The independent review does not accept three claims as stated. The status seed exists and marks `Resolved` non-terminal. Cross-company listing does not mean cross-company detail is denied. SLA plant hours govern the start time, and the added duration is clock time with weekend skips.

Deployed status flags, mapping data, and request-validation settings remain unverified until a named environment is authorized for a read-only catalog check.
