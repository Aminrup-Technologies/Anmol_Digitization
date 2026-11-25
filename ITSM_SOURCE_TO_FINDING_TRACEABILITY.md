# ITSM source-to-finding traceability matrix

**Document:** ITSM_SOURCE_TO_FINDING_TRACEABILITY.md
**Deliverable:** R-01
**Status:** Draft for independent technical review by FreeBuff
**Date:** 24 November 2026
**Scope:** ITSM module only. Source inspection of the working tree.
**Change status:** No application file was modified. No SQL was executed. No UAT or LIVE connection was opened.

This matrix attaches findings F-01 through F-10 in the 24 November 2026 ITSM Technical Remediation Plan to the methods and schema declarations that produce them. Line numbers are from the files as read on that date. They are source locations, not a certified baseline and not a deployed-database result.

The Sales TA/DA tags named in `docs/INDEX.md` §10 are not an ITSM baseline. This trace does not use them.

## 1. How to read a row

| Column | Meaning |
|---|---|
| Root cause | What the source does, stated from the code that was read |
| Evidence class | `Source-verified` means the behaviour is in the cited lines. `Environment-unread` means the deployed row or grant was not queried |
| Design | The matching section of `ITSM_TECHNICAL_REMEDIATION_DESIGN.md` |

Business decisions D-1 through D-18 stay pending. A source location does not close one.

## 2. Matrix

| ID | Root cause | Primary location | Supporting locations | Evidence class |
|---|---|---|---|---|
| F-01 | Ticket numbers are allocated by reading the current maximum and formatting the next value. The read is `AsNoTracking`, has no lock hint, and no application lock. The insert is a later `SaveChanges` on the same transaction. The parameterless `BeginTransaction` does not raise isolation or hold a key range. `itsm.Tickets.TicketNumber` is `varchar(10)` and is not indexed uniquely in any ITSM migration. The maximum query is filtered by `RequestTypeId` and by the displayed prefix, so two request types that share a prefix allocate separately. A trimmed prefix shorter than two characters yields a number shorter than 10 characters; those values fail the `Length >= 10` parse and contribute 0. Sequence 10000 formats as five digits and makes an 11-character string, which the column and `[StringLength(10)]` both reject. | `AnmolDristi/ITSM/BLL/TicketService.cs` `GenerateTicketNumber` lines 359–401, called from `CreateTicket` lines 157–161 | Column and non-unique indexes: `AnmolDristi/ITSM/DAL/Migrations/202608120550515_InitialItsmSchema.cs` lines 78 and 117–128. Entity: `AnmolDristi/ITSM/DAL/Entities/Ticket.cs` lines 23–26. Prefix column: `RequestType.cs` lines 25–28 and migration line 243 (`char(2)`). Transaction wrapper: `TicketService.CreateTicket` lines 91–297. A search of `AnmolDristi/ITSM` finds no `sp_getapplock`, `UPDLOCK`, or `HOLDLOCK`. | Source-verified. Duplicate counts and deployed prefixes were not queried. |
| F-02 | `LoadTicket` assigns stored title, description, resolution, requester, assignee, category, branch, impact, urgency, request type, and CSAT feedback to `Label.Text`. `System.Web.UI.WebControls.Label` writes `Text` without HTML encoding. The same page encodes comment text and approval comments in the repeaters. The creation mail encodes the title and leaves the ticket number as plain text. | `AnmolDristi/ITSM/UI/TicketDetail.aspx.cs` `LoadTicket` lines 233–249 and 256–260 | Markup: `TicketDetail.aspx` line 226 (`lblDescription`), lines 229–233 (`lblResolution`), lines 361 and 367 (comment author unencoded, comment text encoded), lines 381–395 (approval comments encoded; level, name, and status bound with `Eval`). Mail: `TicketService.cs` lines 475–483. List grid: `TicketList.aspx` lines 140–166. `BoundField` encodes by framework default. The ticket-number `Eval` inside the link, and the priority and status `Label` bindings, do not. | Source-verified. Request-validation mode is a `Web.config` fact and was not read. No payload was posted. |
| F-03 | `TicketApproval.Comments` is limited to 500 characters in the entity and in the initial migration. `RejectTicket` rejects a blank reason, then assigns `reason.Trim()` with no length check. `txtRejectionReason` has no `MaxLength`. `txtOverrideReason` on the same page has `MaxLength="500"`. The service turns only `DbUpdateConcurrencyException` into `TicketUpdateResult.Fail`. Any other exception, including `DbEntityValidationException`, is rolled back and rethrown. `btnReject_Click` does not catch it. | `AnmolDristi/ITSM/BLL/TicketUpdateService.cs` `RejectTicket` lines 357–454, especially 363–367 and 407–409; catch lines 444–452 | Entity: `TicketApproval.cs` lines 41–42. Migration: `202608120550515_InitialItsmSchema.cs` line 139. Markup: `TicketDetail.aspx` lines 318–320. Page: `TicketDetail.aspx.cs` `btnReject_Click` lines 156–176. | Source-verified. `DbEntityValidationException` and `DbUpdateException` are siblings under `System.Data.DataException` in EF6. The page outcome does not depend on one deriving from the other: the service rethrows every exception other than the concurrency type. `customErrors` was not read. |
| F-04 | When the target status name is `Resolved`, `ChangeStatus` replaces `ResolutionText` and `ResolvedOn`. The following audit call passes `detail: null`. The transition map allows `Resolved` → `In Progress` and `Resolved` → `Closed`. Moving to `In Progress` does not clear the two columns. A later resolve replaces them. The previous text is not copied to `TicketAuditLog` first. | `AnmolDristi/ITSM/BLL/TicketUpdateService.cs` `ChangeStatus` lines 86–110 | Map: `TicketWorkflow.cs` lines 23–26. `IsTerminal` returns before the map is consulted: `ChangeStatus` lines 73–77 and `TicketWorkflow.GetAvailableStatuses` lines 36–38. Current-state columns: `Ticket.cs` lines 107 and 125. Audit capacity: `TicketAuditLog.cs` lines 59–64. | Source-verified. Whether a deployed `Resolved` row has `IsTerminal = 1` was not queried. The seed writes `Resolved` as non-terminal only when `MST_TicketStatus` is empty (`Configuration.cs` lines 26–37). That seed is not a deployed reading. |
| F-05 | `AddAudit` assigns `Detail = Truncate(detail, 1000)`. `Truncate` returns `Substring(0, maxLength)` and does not signal the cut. `AddComment` stores the full trimmed comment on `TicketComments.CommentText` (`nvarchar(max)` in the migration) and passes the same text, with an optional `"[Internal] "` prefix, to `AddAudit`. `txtComment` declares `MaxLength="2000"`. There is no server-side length check. | `AnmolDristi/ITSM/BLL/TicketUpdateService.cs` `AddComment` lines 489–504; `AddAudit` lines 602–622; `Truncate` lines 624–632 | Column: `202608201200000_AddTicketAuditLog.cs` line 19. Entity: `TicketAuditLog.cs` lines 63–64. Comment column: `TicketComment.cs` lines 20–21 and migration lines 167–172. Markup: `TicketDetail.aspx` line 339. | Source-verified. |
| F-06 | `GetTicketDetail` sets `CanApprove` from any pending `TicketApprovals` row for the actor. It does not require status `Pending Approval`, does not exclude the requester, and does not look at approval level. `LoadTicket` sets `pnlApprovalActions.Visible` from that flag. `CanApproveTicket`, `ApproveTicket`, and `RejectTicket` refuse the requester. `ApproveTicket` and `RejectTicket` require the current status name `Pending Approval`. `ApproveTicket` refuses an `ItHead` row while a `Manager` row is still `Pending`. `RejectTicket` does not apply that order check. Create inserts the IT Head row as `Pending` at the same time as the manager row. | `AnmolDristi/ITSM/BLL/TicketDetailService.cs` lines 176–180. `AnmolDristi/ITSM/UI/TicketDetail.aspx.cs` line 281 | Server policy: `ItsmTicketAccess.CanApproveTicket` lines 82–98. `ApproveTicket` lines 280–314. `RejectTicket` lines 381–405. Create inserts: `TicketService.cs` lines 233–273 (`ApprovalLevel` values `"Manager"` and `"ItHead"`). The approve-order comparison uses `"ITHead"` with `OrdinalIgnoreCase`, which matches the stored `"ItHead"`. | Source-verified. A population of pending rows on tickets that have left `Pending Approval` was not queried. The approve and reject methods in this file do not leave that population: reject marks the other pending rows `Rejected` in the same transaction, and approve leaves the ticket in `Pending Approval` until no other pending row remains. |
| F-07 | Each cascade step ends in `Select(SupportAgentCode).FirstOrDefault()` with no `OrderBy`. The initial migration indexes `BranchId`, `CategoryId`, `SubCategoryId`, `ItemId`, and `SupportAgentCode` as separate non-unique indexes. No later ITSM migration adds a unique natural key. | `AnmolDristi/ITSM/BLL/DispatchService.cs` `FindMapping` lines 88–90, `FindCategoryMapping` lines 98–108, `FindBranchDefault` lines 111–123 | Caller: `TicketService.CreateTicket` lines 153–154, passing `MST_Branch.Id`. Schema: `202608120550515_InitialItsmSchema.cs` lines 10–35. Entity: `SupportMapping.cs`. | Source-verified. Whether duplicate active rows exist was not queried. |
| F-08 | Each ticket mutation listed below opens one `BeginTransaction`, adds the audit entity, and calls `SaveChanges` once inside `Commit` before `transaction.Commit`. A concurrency exception rolls back and returns `Fail`. Any other exception rolls back and rethrows. `CreateTicket` uses one transaction on its context for the ticket insert, the `Created` audit, approval rows, and the `NotificationQueue` insert. `SlaService.CalculateSla` and `DispatchService.ResolveAssignee` each open a second `ItsmDbContext`. Those reads are not enlisted in the create transaction. They run before the ticket insert. | `AnmolDristi/ITSM/BLL/TicketUpdateService.cs` `Commit` lines 634–640, and the six public methods | Create: `TicketService.cs` lines 91–297 and `EnqueueTicketCreatedNotification` lines 491–503 (`ExecuteSqlCommand` on the create context, before `Commit`). Separate contexts: `SlaService.CalculateSla` lines 42–53; `DispatchService.ResolveAssignee` lines 24–25. | Source-verified as structure. No forced persistence failure was executed, so rollback behaviour is inferred from the transaction scope, not observed. |
| F-09 | Create resolves `MST_Branch` with `Branch_ID == requester.BranchId`, stores `MST_Branch.Id` on the ticket, and passes that identity to dispatch. The same method passes `requester.BranchId` to `CalculateSla`. `GetPlantTiming` compares that integer with `VMS_PlantTimings.Branch_ID`. The two results are written into the creation-mail body. `Ticket` has no SLA start or due column. | `AnmolDristi/ITSM/BLL/TicketService.cs` lines 115–127 and 151–154; mail lines 480–481 | `SlaService.GetPlantTiming` lines 165–174. Entities: `UserMaster.cs` lines 39–44 and 65–66; `Branch.cs` lines 22–27; `PlantTimings.cs` lines 15–16. | Source-verified that the call sites pass different properties. The equality of `MST_UserMaster.BranchId`, `MST_Branch.Branch_ID`, `MST_Branch.Id`, and `VMS_PlantTimings.Branch_ID` is environment-unread. Comments on `Branch` and `UserMaster` assert the legacy-key story. Those comments are not a query from this pass. `UserMaster` also declares `[ForeignKey("BranchId")]` against `Branch`, whose key is `Id`. `CreateTicket` does not use that navigation. |
| F-10 | Deployed terminal flags, request-type prefixes, approval flags, permission grants, and CompanyID populations are not in the source that was read. The status seed runs only when `TicketStatuses` has no rows, and it does not update an existing table. No request-type seed was found under `AnmolDristi/ITSM` or `Database`. Repository permission scripts are not a grant reading. | `AnmolDristi/ITSM/DAL/Migrations/Configuration.cs` lines 26–38 | Status flag consumption: `TicketUpdateService.ChangeStatus` lines 73–77; `TicketWorkflow.GetAvailableStatuses` lines 36–38. Permission scripts present as files: `AnmolDristi/Database/itsm_phase1b_permissions.sql`, `AnmolDristi/Database/itsm_role_permissions.sql`, `AnmolDristi/Database/ITSM_TicketView_Permission.sql`, `Database/Scripts/010_ITSM_Sidebar_Permissions.sql`. | Environment-unread. The seed text is source-verified and is not evidence of the deployed table. |

## 3. Session contract observed while tracing

ITSM page gates read three session keys. They do not read a key named `BranchID`.

| Page | Gate | Actor used for ITSM calls |
|---|---|---|
| `CreateTicket.aspx.cs` lines 57–59 and 101 | `WORKMAN`, `USERID`, and `BRANCH` must be non-null | `Session["WORKMAN"]` is the requester employee code |
| `TicketDetail.aspx.cs` lines 25–27 and 339–341 | same three keys | `Session["WORKMAN"]` is the actor code |
| `TicketList.aspx.cs` lines 31–33 | same three keys | `Session["WORKMAN"]` |
| `ApprovalConfig.aspx.cs` line 23 | `USERID` and `BRANCH` only | `WORKMAN` is not required on this page |

`login.aspx.cs` line 217 assigns `Session["BRANCH"]` from the authentication row's `BranchId` column. Create, detail, and list use that key as a presence check. SLA and dispatch do not read `Session["BRANCH"]`. They read `MST_UserMaster.BranchId` for the employee code in `WORKMAN`, then resolve `MST_Branch.Id` for dispatch only.

`BRANCH` and `MST_Branch.Id` are different values in this source. A later change must not treat one as a substitute for the other.

## 4. State-changing methods enumerated for F-08

| Method | Ticket write | Audit write | Transaction |
|---|---|---|---|
| `TicketService.CreateTicket` | `Tickets` insert, then optional `TicketApprovals` inserts, then `NotificationQueue` insert on the same context | `Created` row in the same transaction | One `BeginTransaction`. Rollback and rethrow on any exception. |
| `TicketUpdateService.ChangeStatus` | status, and resolution columns when the target is `Resolved` | `StatusChange`, `Detail` null | One transaction, one `SaveChanges` |
| `TicketUpdateService.ReassignTicket` | `AssigneeCode` | `Reassignment`, `Detail` null | One transaction, one `SaveChanges` |
| `TicketUpdateService.OverridePriority` | `PriorityId` | `PriorityOverride`, `Detail` = trimmed reason, then subject to `Truncate(..., 1000)` | One transaction, one `SaveChanges` |
| `TicketUpdateService.ApproveTicket` | approval row; ticket status moves to `In Progress` only when no other pending row remains and that status row exists | `Approval`, `Detail` = comments | One transaction, one `SaveChanges` |
| `TicketUpdateService.RejectTicket` | approval rows and ticket status when a `Rejected` or `Cancelled` row exists | `Rejection`, `Detail` = trimmed reason, then subject to `Truncate` | One transaction, one `SaveChanges` |
| `TicketUpdateService.AddComment` | comment insert and ticket modified columns | `Comment`, `Detail` truncated to 1000 | One transaction, one `SaveChanges` |

`ApprovalConfigurationService` opens its own transactions for request-type flags and the IT Head configuration. Those methods do not change a ticket. They are outside the F-08 ticket-transition set.

Validation failures inside `TicketUpdateService` return `Fail` before `Commit`. The `using` disposal rolls back the uncommitted transaction. No second transaction is wrapped around these methods today.

Two source branches commit a partial workflow outcome and belong in the F-08 design:

- `ApproveTicket` lines 326–332 update the approval row even when no active `In Progress` status exists. The ticket can remain `Pending Approval` with no pending approval row.
- `RejectTicket` lines 424–432 update approval rows even when neither `Rejected` nor `Cancelled` exists. The ticket status is then left unchanged.

## 5. Related source facts

These were visible while tracing F-01 through F-10. They are not additional finding numbers. The design treats the first as part of the same page change as F-06 and leaves the others recorded for FreeBuff.

| Observation | Location | Why it is recorded |
|---|---|---|
| Work-action panel | `TicketDetail.aspx.cs` line 282 sets `pnlWorkActions.Visible` from non-terminal status and a status name other than `Pending Approval`. `LoadForProcess` then requires `ItsmTicketAccess.CanProcessTicket`, which is requester or assignee (`ItsmTicketAccess.cs` lines 74–79). | A viewer who is neither requester nor assignee can see status, reassignment, and priority controls. The server returns a failure. This does not change who may process. |
| Queue search includes description | `TicketListService.cs` line 231 matches `Description`. The grid does not show that column. | Adjacent disclosure fact. It does not decide cross-company visibility. |
| Creation-mail link | `TicketService.cs` line 475 stores `~/ITSM/UI/TicketDetail.aspx?id=` plus the id. | The tilde is not expanded in the string written to `NotificationQueue`. The absolute origin is business decision D-8 and stays pending. |
| Approval configuration session | `ApprovalConfig.aspx.cs` line 23 | The page can pass the `USERID` and `BRANCH` gate without `WORKMAN`. |

No anti-forgery token was found under `AnmolDristi/ITSM`. Whether ViewState MAC makes a cross-site post infeasible was not tested. `Web.config` was not read.

## 6. What this trace did not do

- It did not modify `AnmolDristi/ITSM` or any other application file.
- It did not execute SQL or open a UAT or LIVE connection.
- It did not count duplicate ticket numbers, duplicate mappings, null `CompanyId` rows, or deployed status and permission rows.
- It did not post a stored-script payload and did not force an audit insert to fail.
- It did not read `AnmolDristi/Web.config`.
- It did not mark any test passed, and it did not close D-1 through D-18.

## 7. Review request

FreeBuff should confirm or dispute each primary location against the files named above. A disagreement should name the file, method, and the line range that supports the other reading. Environment rows stay unread until a separate read authorisation names the environment.
