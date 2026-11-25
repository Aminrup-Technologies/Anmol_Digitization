# ITSM technical remediation design — revision 2

**Document:** ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md
**Revision:** 2
**Date:** 24 November 2026
**Status:** Design revision for review. Not an approval. Not an instruction to build.
**Required verdict:** NOT READY FOR IMPLEMENTATION

This revision replaces the proposed design text of revision 1 for any future implementation discussion. It does not replace revision 1 as a historical record. Revision 1 remains `ITSM_TECHNICAL_REMEDIATION_DESIGN.md`, unmodified.

## 1. Document control and review history

| Item | Value |
|---|---|
| Revision | 2 |
| Revision date | 24 November 2026 |
| Supersedes as a proposal | `ITSM_TECHNICAL_REMEDIATION_DESIGN.md` (R-02, 24 November 2026). That file is unchanged. |
| Working tree inspected | `HEAD` `f1f4bdb`. Line numbers below are locations in that working tree. They are not a certified baseline and not a deployed-database result. |
| Sales TA/DA tags | Not an ITSM baseline. Not used. |

### Review chain, in order

| Order | Document | Role in this revision |
|---|---|---|
| 1 | `ITSM_SOURCE_TO_FINDING_TRACEABILITY.md` | R-01. Source locations for F-01 through F-10. |
| 2 | `ITSM_TECHNICAL_REMEDIATION_DESIGN.md` | R-02. Revision 1 proposal. Defects X-1 through X-5 are in this text. |
| 3 | `ITSM_FREEBUFF_REMEDIATION_REVIEW.md` | Independent review. Accepted with Corrections. Five blocking defects, C-01 through C-32, R-01a through R-01c, AR-1 through AR-16, TC-21 through TC-50. |
| 4 | `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION.md` | Confirmed the five blocking defects against source, adjusted the approval-status class reading, and split Gate A into A1, A2, and A3. |
| 5 | `ITSM_BUSINESS_DECISION_REGISTER.md` | D-1 through D-18. Prepared for owner review. Every status in section 6 is Pending. Recommendations are not approvals. |

No file in that chain was modified to produce this revision.

### How to read a statement

| Label | Meaning |
|---|---|
| Source-verified | Read in the cited file and line range on 24 November 2026. |
| Proposed correction | What a later authorised change would do. Not built. |
| Unresolved assumption | Required for the proposal to be true, and not established by a query or a ruling. |
| Required approval | A person who must rule before the item can be enabled. Not supplied in this revision. |
| Engine-contract | EF6, ADO.NET, or SQL Server call behaviour. Not executed here. |
| Environment-unread | A deployed row, grant, collation, edition, or timeout. Not queried. `AnmolDristi/Web.config` was not read. |

---

## 2. Executive status and implementation verdict

**NOT READY FOR IMPLEMENTATION.**

Revision 1's direction is kept: rules stay in `AnmolDristi/ITSM/BLL`, persistence stays in the EF6 DAL, pages gain no inline SQL, and no new workflow state or approval rule is added. The five blocking corrections are now part of the proposal. Gate A is no longer one bundle.

| Gate | What it contains | Enablement |
|---|---|---|
| A1 | Encoding, panel alignment, dispatch `OrderBy`, guarded rollback | Later change authorisation. No status-row dependency. |
| A2 | Length checks, audit fail-closed, resolution history | Later change authorisation, and the owner acknowledgements in §13. Those acknowledgements are not recorded. |
| A3 | F-08 fail-closed approval and rejection | Disabled until a recorded environment read shows the required active status rows. |
| B | Ticket-number allocation, unique indexes, retry | IT/DBA authorisation, precondition reads, and the shared-sequence acknowledgement. None of these exist. |
| C | D-1 through D-18 | A written owner ruling in the register. Every row is Pending. |

`ResolveInitialStatusId` stays fail-open. Making it fail-closed is not part of A3. Doing so while `Pending Approval` is absent or inactive would stop creation of approval-required tickets.

The register's D-4 recommendation (option A: unique index, locked allocation, current public format, 9999 ceiling) matches the Gate B method. The register's status for D-4 is Pending. A recommendation is not an authorisation to change the database. D-9 in the register is the approval-flag policy for incident, service request, and change. It is not a ruling on shared prefixes, and it stays Pending.

No test in §10 was executed. Every disposition is **NOT RUN**.

---

## 3. Source and review evidence

Inspected for this revision, in addition to the five documents: `TicketService.cs`, `TicketUpdateService.cs`, `DispatchService.cs`, `TicketWorkflow.cs`, `TicketDetail.aspx`, `TicketDetail.aspx.cs`, `TicketList.aspx.cs`, `CreateTicket.aspx.cs`, `ApprovalConfig.aspx.cs`, `BasePage.cs` permission load, the initial ITSM migration, `AddTicketAuditLog`, `AddBranchLegacyId`, `AddApprovalConfiguration`, `Configuration.cs` seed, `UserMaster.cs`, and `Branch.cs`.

Not evidence: entity comments that claim a column is unique, the status seed, permission script files, and any deployed row. A search of `*.sql` for `MST_RequestType`, `MST_TicketStatus`, and `MST_SupportMapping` returned no matches. A search of `AnmolDristi/ITSM` for `sp_getapplock`, `ParameterDirection`, `SqlQuery`, `AntiForgery`, `RequestVerificationToken`, and `ViewStateUserKey` returned no matches.

### Session contract this revision keeps

Source-verified, and consistent with the register section 3. Pages continue to require the keys they require today. `WORKMAN` remains the employee code passed into ITSM services. `USERID` and `BRANCH` remain presence checks. `CreateTicket` resolves `MST_Branch` with `Branch_ID == requester.BranchId` (`TicketService.cs` lines 115–117), stores `MST_Branch.Id` on the ticket (line 179), passes that identity to dispatch (line 153), and passes `requester.BranchId` to `CalculateSla` (line 151). This revision does not read `Session["BRANCH"]` as `MST_Branch.Id` and does not add a company session key.

---

## 4. F-01 through F-10 remediation matrix

| ID | Source-verified defect | Proposed correction | Gate | Still required before enablement |
|---|---|---|---|---|
| F-01 | `GenerateTicketNumber` (`TicketService.cs` lines 359–401) reads the max with `AsNoTracking`, no lock, filtered by `RequestTypeId` and the displayed prefix. Parse requires `Length >= 10` and takes the last four characters (lines 389–390). `TicketNumber` is `varchar(10)` with no unique index (initial migration lines 78 and 117–128). `Prefix` is `char(2)` (line 243). `BeginTransaction()` at line 91 takes no isolation and no application lock. | Serialise allocation on the create transaction with `sp_getapplock`, using the batch in §5 X-2. Parse only exact 10-character values, sequence digits at offset 6 (§5 X-1). Remove the `RequestTypeId` filter so one visible namespace has one sequence. Unique nonclustered unfiltered index after the duplicate read is zero. Retry inside `TicketService` on a new context, at most 3 attempts. | B | IT/DBA authorisation. Precondition reads §9. Shared-sequence acknowledgement §13. D-4 remains Pending. Lock timeout not chosen. Normalisation case not chosen. |
| F-02 | `LoadTicket` assigns stored text to `Label.Text` (`TicketDetail.aspx.cs` lines 233–253). `Label` writes `Text` unencoded. Repeater bindings at `TicketDetail.aspx` lines 361, 381–386 are unencoded for name, level, and status text. Comment text and approval comments are already encoded (lines 367 and 394). `lblMessage` is assigned on four pages. | Encode at each output assignment, including the four `lblMessage` assignments and the approval-status text node. Whitelist status-badge classes. Add `itsm-prewrap` to the two anonymous containers. Leave `BoundField` columns and `ListItem` text unchanged. | A1 | Change authorisation. No business ruling. D-8 stays Pending for the mail origin. |
| F-03 | `TicketApproval.Comments` is 500 characters (entity and migration line 139). `RejectTicket` rejects a blank reason (lines 363–367) then assigns `reason.Trim()` (line 409) with no length check. `txtRejectionReason` has no `MaxLength`. Only `DbUpdateConcurrencyException` becomes `Fail`. | Trim, and if length is greater than 500 return `Fail` with `Rejection reason must be 500 characters or fewer.` Place the check beside the blank check, before the context opens. Set `MaxLength="500"` as a hint. Apply the same 500-character check in `ApproveTicket` before any row is modified. | A2 | Same release as the comment bound and the override-reason bound. No separate business ruling for the existing 500-character column. |
| F-04 | When the target name is `Resolved`, `ChangeStatus` replaces `ResolutionText` and `ResolvedOn` (lines 98–102) and audits `StatusChange` with `detail: null` (line 109). The map allows `Resolved` → `In Progress` and `Resolved` → `Closed` (`TicketWorkflow.cs` lines 23–26). `ResolutionText` is `nvarchar(max)`. `Detail` is 1000 characters. | Before replacing a resolution that fits in 1000 characters, write a `Resolution` audit row for the previous text in the same `SaveChanges`. Refuse a new or previous text longer than 1000. Reopen leaves both columns unchanged and leaves `StatusChange.Detail` null. One `DateTime.Now` per transition. No backfill of pre-change resolutions. | A2 | Owner acknowledgement of the new 1000-character maximum. D-17 (the minimum) stays Pending. The register's option A for D-17 describes this refusal and is not approved. |
| F-05 | `AddAudit` (lines 602–622) runs `Truncate` (lines 624–632), which cuts and does not signal. `AddComment` stores the full comment and passes `"[Internal] "` plus the text as `Detail` (lines 501–503). `txtComment` allows 2000 characters. `CommentText` has no `[StringLength]`. | Delete `Truncate` from `AddAudit`. Fail closed when any audit field exceeds its column. Public comment limit 1000. Internal user text limit 989. Return `Fail` before the comment is added and before `ModifiedOn` is set. Server-side 500-character bound on the priority-override reason in the same release. | A2 | Owner acknowledgement of the 2000-to-1000 / 989 reduction. D-5 stays Pending. |
| F-06 | `CanApprove` is any pending row for the actor (`TicketDetailService.cs` lines 176–180). The page shows `pnlApprovalActions` from that flag (line 281). `ApproveTicket` also requires status `Pending Approval` and refuses `ItHead` while a `Manager` row is `Pending`. `RejectTicket` does not apply that order check. `pnlWorkActions` (line 282) does not require `CanProcessTicket`. | One BLL helper computes May-approve and May-reject. The page and `ApproveTicket` / `RejectTicket` call it. May-reject does not add an order check. `pnlWorkActions` also requires `CanProcessTicket`. Control IDs stay. `CanProcessTicket` itself stays requester or assignee. | A1 | Change authorisation. D-2 and D-10 stay Pending. The helper preserves today's server outcomes, including IT Head rejection while the manager row is pending. |
| F-07 | Each cascade step ends in `Select(SupportAgentCode).FirstOrDefault()` with no `OrderBy` (`DispatchService.cs` lines 88–90, 107–108, 122–123). Indexes on `MST_SupportMapping` are separate and non-unique (migration lines 31–35). An empty string is not null, so it wins the cascade (lines 31–33). | `OrderBy(m => m.Id)` before the projection. Empty or whitespace agent code is no match, enabled only after the blank-row count is known. Four mutually exclusive filtered unique indexes, specified in §7 Gate B. Orphan rows stay unconstrained. No pickup. | A1 for `OrderBy`. Blank fall-through after its read. Indexes in B. | D-14 stays Pending. IT/DBA for the indexes. Schema mechanism not chosen. |
| F-08 | `ApproveTicket` lines 316–332 mark the approval approved even when no active `In Progress` row exists. `RejectTicket` lines 407–432 mark approvals rejected even when neither active `Rejected` nor active `Cancelled` exists. Both then commit. | When the required status row is missing, return `Fail` before any approval row is modified. Ship that behaviour only as A3, after the status read passes. Until then the current partial-commit behaviour remains. | A3 | Recorded read: environment, statement, timestamp, reviewer. Active `In Progress`, and active `Rejected` or `Cancelled`. If the read fails, A3 stays disabled. |
| F-09 | Create passes two different branch identifiers into SLA and dispatch. `Ticket` has no SLA column. `AddBranchLegacyId` is an empty migration. `Branch.Branch_ID` has a uniqueness comment and no unique index in that migration. `UserMaster` declares `[ForeignKey("BranchId")]` against `Branch` (lines 65–66) while the comment says the column stores `Branch_ID`. No ITSM query navigates `UserMaster.Branch`. `Ticket.Branch` is used by detail and list and follows `Tickets.BranchId`, which create sets to `MST_Branch.Id`. | No code. The F-09 read compares the four identifiers and counts duplicate `Branch_ID` values. The foreign-key attribute stays until that read exists. No SLA column is added. A future D-7 ruling does not authorise a due-date column until the read is attached. | Report only | Separate read authorisation. D-7 stays Pending. |
| F-10 | The status seed runs only when `TicketStatuses` is empty (`Configuration.cs` lines 26–38) and does not update an existing table. No request-type seed exists in `*.sql`. Permission scripts are not a grant reading. `ResolveInitialStatusId` (lines 404–432) falls back to the lowest active non-terminal status when the preferred name is missing, and create still inserts `Pending` approval rows. | The reads in §9. No correction script runs from a report. Do not infer deployed flags from the seed. Do not change the fallback in this revision. | Report only, and a precondition for A3 and B | Separate read authorisation. Whether to make the fallback fail closed is an owner decision and is not taken. |

---

## 5. Five blocking corrections X-1 through X-5

These are the proposed design. They correct revision 1. They are not built.

### X-1 — Ticket-number parsing

**Source-verified.** Lines 389–390 accept a value when `n.Length >= 10` and parse `n.Substring(n.Length - 4)`. A stored value of length 6, 7, 8, or 9 contributes 0. The column is `varchar(10)`, so `>= 10` and `== 10` accept the same values that can be stored today. Revision 1 §4.3 step 5 replaced that guard with "at least as long as the namespace" (six characters). A nine-character value such as `CR2608001` shares the namespace `CR2608`. Keeping the source's last-four slice under that weaker test parses it as 8001.

**Proposed correction.**

1. The namespace is the normalised two-character prefix plus `yy` plus `MM`. It is six characters.
2. A stored `TicketNumber` advances the sequence only when `value.Length == namespace.Length + 4` (exactly 10) and the value starts with that namespace under the collation recorded by the precondition read.
3. The sequence is `value.Substring(namespace.Length, 4)` parsed with `int.TryParse` using the invariant culture. That is offset 6 for a six-character namespace.
4. Malformed values do not advance the sequence and are not rewritten. A value is malformed when its length is not 10, it does not start with the namespace, or the four characters at offset 6 are not an integer. Lengths 6, 7, 8, and 9 are malformed for sequencing even when they share the namespace.
5. The next sequence is one more than the maximum parsed value, or 1 when none parse.
6. Exhaustion: if the next sequence is greater than 9999, throw `InvalidOperationException` stating that the monthly sequence for that prefix is exhausted. Do not call `ToString("D4")` on a value above 9999. Do not insert.
7. Also refuse when `(namespace + nextSequence.ToString("D4")).Length != 10`. With a six-character namespace this matches the 9999 check. It is what stops a later namespace-width change from emitting 11 characters into `varchar(10)`.
8. Historical numbers, including short ones, stay stored. New allocation emits only ten-character numbers.

**Unresolved assumption.** The collation that decides "starts with" is environment-unread. The single case used to normalise the prefix is not selected. D-4 stays Pending, including the service owner's confirmation that the displayed format stays stable.

### X-2 — Application lock return code

**Source-verified.** The only `ExecuteSqlCommand` in the ITSM tree is the notification `INSERT` at `TicketService.cs` lines 491–503. It does not read a procedure return code. Revision 1 said a timeout or a negative `sp_getapplock` code throws, and named `ExecuteSqlCommand` as the call.

**Engine-contract, not executed.** `ExecuteSqlCommand` returns the `ExecuteNonQuery` rows-affected integer. `sp_getapplock` reports status through its return code: `0` granted immediately, `1` granted after waiting, `-1` timeout, `-2` cancelled, `-3` deadlock victim, `-999` parameter or other call error. A timeout does not raise by itself. A procedure that changes no rows commonly yields an `ExecuteNonQuery` result of `-1`, which is not the applock code.

**Proposed correction.** On the `ItsmDbContext` that already owns the create transaction, before `Tickets.Add`, execute this batch. The lock owner is that transaction. The resource is one string built from the same normalised prefix and the same `yyMM` as the namespace.

```sql
DECLARE @rc int;
EXEC @rc = sys.sp_getapplock
    @Resource = @resource,
    @LockMode = 'Exclusive',
    @LockOwner = 'Transaction',
    @LockTimeout = @timeoutMs;
IF @rc < 0
    THROW 50000, 'ITSM ticket-number lock was not acquired.', 1;
```

`@resource` is `ITSM.TicketNumber.` plus the normalised prefix plus `.` plus `yyMM`. `@rc` of `0` or `1` continues. `@rc` of `-1`, `-2`, `-3`, or `-999` fails the batch. The failure is surfaced as `InvalidOperationException` with a controlled message. The guarded rollback in AR-6 runs, and no ticket, audit, approval, or notification row is committed.

**Unresolved assumption.** `@timeoutMs` is an IT operational parameter. This revision does not choose a millisecond value. The `DbConn` identity's `EXECUTE` right on `sys.sp_getapplock` is environment-unread. The lock is scoped to the database that holds it. Two databases are not one numbering namespace.

### X-3 — Internal audit prefix

**Source-verified.** The literal at `TicketUpdateService.cs` line 503 is `"[Internal] "`: `[`, `I`, `n`, `t`, `e`, `r`, `n`, `a`, `l`, `]`, space. That is 11 characters. `Detail` is 1000 characters (`202608201200000_AddTicketAuditLog.cs` line 19). 1000 − 11 = 989. Revision 1 said ten characters and 990. TC-08 in revision 1 repeated 990.

**Proposed correction.** Build the audit detail first. A public comment is refused when the trimmed text is longer than 1000 characters. An internal note is refused when the trimmed user text is longer than 989 characters, because the 11-character prefix shares the column. The check uses the built string. The message for an internal refusal says why the limit is 989. `MaxLength="1000"` on `txtComment` is a hint for the public box and cannot express 989. The server check is the rule. TC-08 in §10 matches these numbers.

**Required approval.** This limit is part of the comment-capacity reduction in §13. A2 is not enabled until that acknowledgement is recorded. D-5 and D-17 stay Pending. Neither decision's question text is the comment-box size.

### X-4 — Audit truncation

**Source-verified.** `AddAudit` assigns `Truncate(actionType, 50)`, `Truncate(oldValue, 200)`, `Truncate(newValue, 200)`, and `Truncate(detail, 1000)`. `Truncate` returns `Substring(0, maxLength)`. Callers that pass caller-controlled text are `AddComment`, `RejectTicket`, `OverridePriority`, and `ApproveTicket` (`comments`). `ChangeStatus` passes a null detail. `ReassignTicket` passes an assignee code or `Unassigned`. `CreateTicket` writes the `Created` row directly (lines 198–209) and does not call `Truncate`.

`MST_TicketStatus.Name` and `MST_Priority.Name` are `maxLength: 100` (initial migration lines 255 and 218). Assignee codes used in reassignment are at most 50 characters. `"Approved (partial)"` is 18 characters. Those values fit the 200-character columns because of those limits, not because `Truncate` is a safe default.

**Proposed correction.**

1. Delete the `Truncate` calls in `AddAudit`. Delete `Truncate` if nothing else calls it.
2. Before `SaveChanges`, fail when `ActionType` exceeds 50, `OldValue` or `NewValue` exceeds 200, or `Detail` exceeds 1000. The failure is `TicketUpdateResult.Fail` or a thrown exception. It is not a `Substring`.
3. Each caller bounds its own input before it mutates the ticket and before it calls `AddAudit`: rejection reason 500, approval comment 500, override reason 500, resolution 1000, public comment 1000, internal user text 989.
4. A failure returns before the ticket row is modified and before an audit entity is added. `SaveChanges` is not called. The transaction commits nothing. There is no approval-status change without its audit row, and no audit row without its ticket change, because both are in the one existing `SaveChanges` inside `Commit` (lines 634–640) and a refused call never reaches `Commit`.
5. The direct `Created` insert stays free of `Truncate`. Its current values fit the columns. It must not gain a silent cut.

**Unresolved assumption.** Deployed name lengths were not queried. The migration limits are the source contract this proposal relies on.

### X-5 — Gate ordering

**Source-verified.** Revision 1 §2 places F-08 in Gate A with no precondition. `ApproveTicket` lines 325–332 update the approval row when `In Progress` is missing. `RejectTicket` lines 424–432 update approval rows when both rejection statuses are missing. The seed does not update an existing status table.

**Proposed correction.** F-08 is not in A1 or A2. It is A3, and A3 is a separate later release that is not promoted until §9 read 1 for that environment is attached and passes. A1 and A2 builds do not contain the fail-closed returns. A missing or inactive status row therefore cannot refuse approval or rejection on the day A1 or A2 ships.

Pass condition for promotion of A3: an active row named `In Progress`, and an active row named `Rejected` or `Cancelled`. The same extract records `IsTerminal`, duplicate names, and the status `ResolveInitialStatusId` would return. If the pass condition is false, A3 is not promoted. This revision does not insert a status row.

`ResolveInitialStatusId` is unchanged. Its fail-open fallback is AR-1. It is not made fail-closed in A3.

**Rollback condition.** Redeploy the previous application build, which does not contain the fail-closed returns. Do not delete tickets or audit rows. Do not "fix" a failed read by enabling A3.

---

## 6. Sixteen additional risk dispositions

Each risk from the FreeBuff review is kept. The disposition is the rule this revision adopts. None of it is authorised to build.

| ID | Source-verified location | Root cause and impact | Disposition in this revision | Gate | Approval |
|---|---|---|---|---|---|
| AR-1 | `ResolveInitialStatusId` lines 404–432, called at line 166 before approval inserts at lines 215–276 | Missing or inactive `Pending Approval` returns the lowest active non-terminal id. Approval rows are still inserted as `Pending`. `ApproveTicket` and `RejectTicket` then require status name `Pending Approval`, so those rows cannot be actioned and the ticket skipped the approval status. | Record what the method would return, per active request type, in the status read. Leave the code fail-open. A later fail-closed change needs an owner decision and the same read, because a missing row would stop creation. | Read with A3's precondition. Code change in no open gate. | Business owner for any later fail-closed change. IT for the read. Not granted. |
| AR-2 | Name `FirstOrDefault` with no `OrderBy`: `ApproveTicket` line 328, `RejectTicket` lines 424–427, `ResolveInitialStatusId` lines 407–411. `GetAvailableStatuses` (lines 48–56) returns every match. `ChangeStatus` loads by id (lines 63–66). No unique key on `MST_TicketStatus.Name` (migration lines 250–259). | Duplicate names make final approval, rejection, and the initial status attach to an arbitrary row. The transition map is also keyed by name. | The status read reports duplicate names. This revision does not add `OrderBy` and does not pick a survivor. If the read shows no duplicates, a later `OrderBy(s => s.Id)` on those three lookups would not change outcomes. If duplicates exist, choosing a row is a data decision. | F-10 read, before A3. | IT for the read. Business and IT if a survivor must be chosen. Not granted. |
| AR-3 | `DispatchService` lines 31–33, 88–90, 107–108, 122–123. `SupportAgentCode` is NOT NULL, max 50 (migration line 19). `AssigneeCode` is nullable (line 83) and is assigned at `TicketService.cs` line 177. | `FirstOrDefault` returns `""` for a blank code. `""` is not null, so the cascade stops and a later branch default is never used. Whitespace takes the same path. | A1 applies `OrderBy(m => m.Id).Select(m => m.SupportAgentCode).FirstOrDefault()` only. Empty or whitespace is treated as no match, and the cascade continues, only after the blank-row count is known. If that count is not zero, enablement waits for IT acknowledgement and business awareness. D-14 stays Pending. No check constraint is added. | A1 for `OrderBy`. Fall-through after the read. | IT. Business awareness if blank rows exist. Not granted. |
| AR-4 | Cascade filters require a category, a subcategory, or `IsBranchDefault`. | An active row with `IsBranchDefault = 0` and all three taxonomy keys NULL matches no step and matches none of the four index filters. It can repeat. | State the residual. Count the rows. Do not add a fifth index. | B, as a read and a design limit. | IT/DBA before any later fifth index. Not granted. |
| AR-5 | `ApproveTicket` lines 294–298 and 320–323. Approval indexes are `TicketId` and `ApproverCode` only (migration lines 146–147). | A second `Pending` row keeps `stillPending` true, so the ticket stays in `Pending Approval`. The same approver can clear their own second row with another approve. A remaining row whose approver cannot post `Approve` strands the ticket. `RejectTicket` marks the other pending rows `Rejected`, so the stranding path is approval. | Read the duplicate pairs before A3 is promoted, so stranded tickets are visible. Do not add a unique key in this revision. | Read before A3. The key is not in Gate B. | IT/DBA for any later key. Business if a repair changes who has approved. Not granted. |
| AR-6 | `catch (Exception) { transaction.Rollback(); throw; }` at `ChangeStatus` lines 119–122, `ReassignTicket` 185–188, `OverridePriority` 254–257, `ApproveTicket` 349–352, `RejectTicket` 449–452, `AddComment` 513–516. `CreateTicket` lines 292–295 is a bare `catch` with the same shape. | If `Rollback()` throws, the original exception is never rethrown. F-01's retry then cannot see `DbUpdateException` / `SqlException` 2601 or 2627. | Capture the original exception, attempt `Rollback()`, ignore only a failure of that rollback, and rethrow the original. `Dispose` may roll back again; a second rollback of a completed transaction is ignored. Apply this on the six update methods and on `CreateTicket`. | A1 for the six update methods. B for `CreateTicket`, with the retry. | IT review of the exception contract. No business approval. Not granted. |
| AR-7 | `lblMessage.Text` at `TicketDetail.aspx.cs` line 393, `TicketList.aspx.cs` line 336, `CreateTicket.aspx.cs` line 454, `ApprovalConfig.aspx.cs` line 155. | `Label` renders `Text` unencoded. Messages include stored names. | Encode at all four assignments, in A1 with F-02. | A1 | IT. Not granted. |
| AR-8 | `AddComment` lines 489–504 set `ModifiedOn` and `ModifiedByCode` before `AddAudit`. | A refusal that runs after those assignments depends on rollback to keep the ticket unchanged. | Validate the built detail immediately after the blank and internal-note checks. Return `Fail` before the comment entity is added and before lines 498–499. | A2 | Covered by the comment-capacity acknowledgement. Not granted. |
| AR-9 | No anti-forgery API under `AnmolDristi/ITSM`. ViewState MAC was not read. | State-changing posts have no anti-forgery token in that tree. Revision 1 deferred the token with no owner and no date. | Keep the token out of F-01 through F-10. Record the absence. This revision assigns neither an owner nor a date. | Outside A1, A2, A3, B, and C. | A later security owner. Not assigned. |
| AR-10 | Branch resolution lines 115–117. `AddBranchLegacyId.cs` is empty. The uniqueness sentence on `Branch.cs` lines 22–27 is a comment. | Duplicate `Branch_ID` values would make branch resolution non-deterministic. | Count duplicates in the F-09 read. Do not add an index and do not change the query. | F-09 report. | IT/DBA for the read. Not granted. |
| AR-11 | `UserMaster.cs` lines 65–66. No query navigates `UserMaster.Branch`. `TicketDetailService.cs` line 138 and `TicketListService.cs` line 246 use `Ticket.Branch`. | A future navigation of `UserMaster.Branch` would join `BranchId` to `Branch.Id`. The ticket navigation is the intended join. | State that grep requirement. Leave the attribute unchanged until the F-09 read exists. | F-09. No code. | IT when the attribute is later reviewed. Not granted. |
| AR-12 | Prefix trim at line 364. Column is `char(2)` at migration line 243. | A stored `' C'` trims to one character. The exact-2 rule then refuses every create for that request type. | Run the prefix read before the rule is enabled. Correct the row under a separate data authorisation, or record owner acceptance that the type is blocked. Do not pad, substring, or fall back to `Code`. | Before Gate B enablement. | Service owner for a blocked type or a prefix correction. IT for the read. Not granted. |
| AR-13 | `CreateTicket.aspx.cs` lines 203–222 show `InvalidOperationException.Message` and, for any other exception, a generic message. | After the unique index exists, a raw `DbUpdateException` becomes the generic message. | The last failed attempt throws `InvalidOperationException` with the controlled text. Intermediate retries run only when the guarded rollback preserved 2601 or 2627 and the index name is the ticket-number index. | B | IT. Not granted. |
| AR-14 | ITSM tables are EF6 migrations. The approval-configuration unique index is raw SQL inside `202608240451107_AddApprovalConfiguration.cs` lines 25–27. Revision 1 named standalone scripts. | Two mechanisms will diverge. | Before any Gate B object is written, IT/DBA name one owner for future ITSM indexes and why. This revision does not choose EF or a standalone script, and it does not create either. | B blocker. | IT and DBA. Not granted and not decided. |
| AR-15 | Revision 1 §11 uses a test-only trigger on `itsm.TicketAuditLog`. | The trigger needs `ALTER`. UAT and LIVE are not named as ineligible. | The verification procedure names a disposable database, the `ALTER` right, and `DROP TRIGGER` in the same procedure. UAT and LIVE are excluded. This revision does not create the trigger. | Verification only. | IT/DBA for the disposable database. Not granted. |
| AR-16 | `TicketListService.cs` lines 225–231 match `Description`. The list projection does not select it. | A queue search can reveal hidden description text. Company scope is the existing queue, which this revision does not change. | Leave the predicate unchanged. Record it as a D-1 residual. | C, with D-1. | Service owner and IT security, per the register. Not granted. |

`ApprovalConfigurationService.UpdateRequestTypeApproval` (lines 102–106) and `SetActiveItHead` (lines 243–247) use the same unguarded rollback. They do not change a ticket. They stay outside the F-08 set. No change to them is proposed.

---

## 7. Revised gates A1, A2, A3, B and C

No gate is open.

### Gate A1 — Application corrections

A later change authorisation is required. A1 does not read `MST_TicketStatus` to decide a new failure, and it does not contain F-08.

- Encode every untrusted string assigned to `Label.Text` in `LoadTicket`, each component before concatenation. Encode `CreatedByName`, `ApprovalLevel`, `ApproverName`, and the approval-status text node. The class attribute at `TicketDetail.aspx` lines 383–386 already emits only `success`, `danger`, or `warning`; keep that closed set. Encode `lblMessage` on `TicketDetail`, `TicketList`, `CreateTicket`, and `ApprovalConfig`.
- Derive `lblStatusBadge` classes from the raw status name through a whitelist of known names to class tokens, on `TicketDetail.aspx.cs` lines 252–253 and `TicketList.aspx.cs` lines 163–168. Encode only the visible text. Do not build the class from already-encoded label text.
- Add class `itsm-prewrap` to the description div (`TicketDetail.aspx` lines 225–227) and the resolution div (lines 231–233). Add `.itsm-prewrap { white-space: pre-wrap; }` to the page `<style>` block that begins at line 15. `HtmlEncode` preserves newlines, which is why the breaks survive.
- Record, and do not change: `BoundField` columns, `ddlTargetStatus` / `ddlReassignTo` / `ddlOverridePriority` `ListItem` text, `gvAudit` `BoundField`s, and `CreateTicket` `InnerText` / `Attributes` rendering including impact and urgency binders. The creation mail's raw ticket number is safe only after F-01's ten-character prefix rule; until Gate B, the number remains the value the current generator emits. D-8 stays Pending.
- One BLL helper for May-approve and May-reject. Bind `pnlApprovalActions`, `btnApprove`, `txtRejectionReason`, `btnReject`, `pnlWorkActions`, and `pnlActions` as specified in the reconciliation's C-24. `CanProcessTicket` is unchanged. Automatic pickup stays disabled. D-14 stays Pending.
- `OrderBy(m => m.Id)` on the three dispatch queries. Empty-agent fall-through is specified under AR-3 and is not part of unconditional A1.
- Guarded rollback on the six update methods (AR-6).

### Gate A2 — Input and history integrity

A2 is one release. It waits on a change authorisation and on the owner acknowledgements in §13 for the resolution maximum and the comment-capacity reduction. Those acknowledgements are not recorded. A2 is not implemented until they are.

- Rejection length: after the blank check and before `using (var context = ...)`, refuse a trimmed reason longer than 500 characters. `MaxLength="500"` is a hint. `ApproveTicket` applies the same check whenever `comments` is non-null, before any approval field is assigned.
- `OverridePriority` refuses a trimmed reason longer than 500 before the priority changes. This ships in the same release, because the page has no catch that would make a validation exception graceful (C-17).
- Comment and audit rules from X-3 and X-4, including the refusal before `ModifiedOn`.
- Resolution history. First resolve with no current `ResolutionText`: reject blank as today; reject trimmed length over 1000 with `Fail` and a message that the resolution must be 1000 characters or fewer so the audit row can store it; in the same `SaveChanges`, write `ActionType` `Resolution`, `Detail` the full trimmed text, `NewValue` `Current`, and `OldValue` exactly `current;resolvedOn=` plus the transition timestamp in round-trip form (`DateTime.ToString("o", CultureInfo.InvariantCulture)`). The prose form `ResolvedOn=` plus a timestamp is withdrawn. The `StatusChange` row remains, with `Detail` null.
- Reopen from `Resolved` to `In Progress`: leave `ResolutionText` and `ResolvedOn` unchanged. `StatusChange.Detail` stays null. `OldValue` on that status row records `Resolution retained`. Do not refuse reopen because the retained text is long.
- Re-resolve: if the previous text is longer than 1000 characters, return `Fail` and change nothing. If it fits, write one `Resolution` row with `Detail` the previous text, `NewValue` `Superseded`, and `OldValue` the superseded marker defined in the reconciliation, then assign the new text under the same 1000-character check and write the new row with `NewValue` `Current` and `OldValue` exactly `current;resolvedOn=` plus that transition's round-trip timestamp. A half-populated pair (`ResolutionText` set and `ResolvedOn` null, or the reverse) is treated as already resolved and recorded on the superseded row. Pre-change tickets have no `Resolution` row and are not backfilled. Reopening them does not insert a row that claims to be that history.
- One `now` per transition, passed into `AddAudit`.
- `Closed` still sets `ClosedOn` only. The transition map is unchanged. Who may resolve, reopen, or close stays D-2 and D-6, both Pending.

The 1000-character resolution ceiling and the 989/1000 comment ceiling are product limits on fields the application currently accepts above those sizes. They are not technical details that can ship unnoticed. D-17 remains the minimum-length question and stays Pending. The register's recommended option A for D-17 describes refusing an audit copy over 1000 characters. That recommendation is not an approval, and the service owner plus IT leadership are the roles the register names for it.

### Gate A3 — Status-dependent fail-closed behaviour

Disabled. A separate release from A1 and A2.

**Proposed behaviour, once enabled.** In `ApproveTicket`, when no other pending row remains and no active status named `In Progress` exists, return `Fail` before the approval row is modified. The message states that the In Progress status is not configured. In `RejectTicket`, when neither an active `Rejected` nor an active `Cancelled` status exists, return `Fail` before any approval row is modified. The message states that the rejection status is not configured. Both checks stay after `RequireRecordedTenant`. No status is added. The names are the names the code already looks up.

**Safe enablement.**

1. A named environment, a statement, a timestamp, and a reviewer are attached to read 1 in §9. The seed is not that attachment.
2. The attachment shows an active `In Progress` row and an active `Rejected` or `Cancelled` row, and it records duplicate names and the `ResolveInitialStatusId` result.
3. Only then is an A3 build promoted to that environment.
4. If step 2 fails, A3 is not promoted. Repair of the status table is a separate data authorisation.

**Rollback.** Redeploy the previous build. Committed approval outcomes are not reversed. Audit rows written under A2, if A2 has already shipped, stay. A failed or missing read is a reason to withhold promotion, not a reason to enable the branch and watch it refuse.

`ResolveInitialStatusId` is not in this release.

### Gate B — Database integrity

Not written and not executed. No migration file and no standalone script is created by this revision.

Contents, after the blockers below:

- Allocation on the create context: normalise the prefix (trimmed, length exactly 2, one case stated by the later design note that IT records), build the namespace, take the application lock with the X-2 batch, read matching numbers without using `AsNoTracking` as a substitute for the lock, parse with X-1, refuse exhaustion, insert the ten-character number in the same transaction.
- Remove the `RequestTypeId` filter from the maximum read. Two request types that display the same normalised prefix share one monthly sequence and one 9999 ceiling. That is the consequence that makes a unique index on `TicketNumber` reachable. It is not a ruling on which types should share a prefix.
- Retry seam. `CreateTicket` (lines 62–85) constructs `ItsmDbContext` inside the method. The public method loops at most 3 times. Each attempt calls a private single-attempt method on a new context. The page does not own the retry. A retry is not idempotent. A rolled-back insert consumes an identity value (SQL Server identity behaviour, engine-contract, not observed here). Each attempt re-reads requester, branch, plant, priority, request type, SLA inputs, and dispatch, and takes a new `createdOn`. Only the successful attempt is persisted and audited. Diagnostic callbacks may run more than once and must not persist. The predicate is `DbUpdateException` whose innermost `SqlException.Number` is 2601 or 2627 and whose index name is the ticket-number index. Any other 2627 is not retried. The final failure throws `InvalidOperationException`.
- Unique nonclustered unfiltered index on `itsm.Tickets(TicketNumber)`. `TicketNumber` is NOT NULL, so the index has no one-NULL duplicate. Create it only when the duplicate read is zero groups. `DROP INDEX` rolls it back and keeps the rows. Online versus blocking build is the precondition in §9.
- Four filtered unique indexes on `itsm.MST_SupportMapping`. Predicates partition the rows the cascade matches. SQL Server will create overlapping filtered indexes; the defect in revision 1 is the wrong coverage, not a guaranteed syntax failure.

| Step | Filter | Key |
|---|---|---|
| Item | `IsActive = 1 AND IsBranchDefault = 0 AND ItemId IS NOT NULL` | `(BranchId, CategoryId, SubCategoryId, ItemId)` |
| Subcategory | `IsActive = 1 AND IsBranchDefault = 0 AND ItemId IS NULL AND SubCategoryId IS NOT NULL` | `(BranchId, CategoryId, SubCategoryId)` |
| Category | `IsActive = 1 AND IsBranchDefault = 0 AND SubCategoryId IS NULL AND ItemId IS NULL AND CategoryId IS NOT NULL` | `(BranchId, CategoryId)` |
| Branch default | `IsActive = 1 AND IsBranchDefault = 1 AND CategoryId IS NULL AND SubCategoryId IS NULL AND ItemId IS NULL` | `(BranchId)` |

An active row with `IsBranchDefault = 0` and all three taxonomy keys NULL is outside every filter (AR-4). No fifth index is proposed.

**Blockers before any Gate B object is written or enabled.**

1. IT/DBA name one schema mechanism (AR-14).
2. Precondition reads in §9 for duplicates, prefixes, collation, `sp_getapplock`, and index-build mode.
3. Owner acknowledgement of the shared monthly sequence (§13). D-4 and D-9 stay Pending. D-4's register question is how duplicates are prevented. D-9's register question is the approval-flag policy. The shared-sequence acknowledgement is additional. It does not close either row.
4. The millisecond timeout is named by IT.
5. The normalisation case is named. This revision does not pick it, and it does not use D-4 to pick it. If the collation read finds both cases, a separate data authorisation collapses them before the unique index.

`SlaService` and `DispatchService` keep their own contexts. They run before the insert. Moving them onto the create context is not required.

### Gate C — Business decisions

D-1 through D-18 stay Pending. The register section 6 status column is Pending on every row. Recommendations in that register, including D-4 option A and D-17 option A, are not approvals. This revision does not write a chosen option into the register.

Preserved workflow facts, because no ruling changed them:

- The transition map in `TicketWorkflow.cs` lines 23–26.
- `ApproveTicket` refuses the IT Head while a Manager row is `Pending`. `RejectTicket` does not add that order check.
- `CanProcessTicket` remains requester or assignee.
- Create still inserts Manager and IT Head rows as `Pending` when the request-type flags are set.
- No new status, no CAB, no pickup, no support group, no SLA column, no department snapshot, no parent-ticket column, no automatic close, no CSAT gate.
- Queue company visibility is unchanged, including the description search (AR-16).
- Null `CompanyId` mutations stay refused. No backfill.

---

## 8. Dependency and authorization matrix

| Item | Depends on | Kind of approval | State |
|---|---|---|---|
| A1 encoding, panels, `OrderBy`, guarded rollback | A change authorisation that names A1 | IT | Not granted |
| A1 empty-agent fall-through | Blank-agent count | IT, and business awareness if the count is not zero. D-14 stays Pending | Not granted |
| A2 length and history | A1 is not a technical prerequisite. A2 waits on §13 acknowledgements for the two capacity reductions, and on a change authorisation | Service owner for the visible limits. IT leadership for the 1000-character audit ceiling, as the register names that role for D-17's audit copy. D-17 itself stays Pending | Not granted |
| A2 override-reason bound | Same release as F-03 and F-05 | IT, inside the A2 authorisation | Not granted |
| A3 F-08 | Read 1 passes for the named environment. A1/A2 must be releasable without A3 | IT to promote. No business ruling to keep today's partial commit until the read passes | Not granted. Read not run |
| A3 does not include AR-1 | An owner decision plus the same status read | Business owner | Not requested as part of A3 |
| Gate B allocation and indexes | Schema-mechanism choice, §9 reads, shared-sequence acknowledgement, named timeout, named prefix case | IT/DBA for the method. Service owner for the shared-sequence acknowledgement and for any prefix this blocks. D-4 Pending | Not granted |
| Gate C | Written ruling in the register, option recorded, not an empty row | The owner named on each decision | All 18 Pending |
| F-09 / F-10 reports | A read authorisation that names the environment | IT / DBA | Not granted |
| Anti-forgery | A security change outside this design | Owner not named | Open, no date |
| Fault-injection tests | Disposable database and `ALTER` | IT/DBA for that database only. UAT and LIVE excluded | Not granted |

---

## 9. Database and environment preconditions

None of these was run. Each result records the environment name, the statement, the timestamp, and the person who ran it. A result copied from an earlier document would be a quoted record. None is copied here. No statement in this section is an instruction to execute SQL now.

**Before A3 can be promoted**

1. `itsm.MST_TicketStatus`: `Name`, `IsTerminal`, `IsActive`, duplicate `Name` groups, and the status `ResolveInitialStatusId` would return for each active request type. Pass: active `In Progress`, and active `Rejected` or `Cancelled`. Fail: do not promote A3. The fallback code is recorded and not changed.
2. Duplicate pending approval rows: more than one `Pending` row for the same `(TicketId, ApproverCode)`, and more than one `Pending` row per ticket. The read makes stranding visible. It does not authorise a unique key.

**Before empty-agent fall-through**

3. Count of active `MST_SupportMapping` rows whose `SupportAgentCode` is empty or whitespace, and count of active orphans (`IsBranchDefault = 0` and all taxonomy keys NULL).

**Before Gate B is enabled**

4. `TicketNumber` duplicate groups: exact, case-variant, and whitespace or non-printing. Zero groups, or a separate data authorisation.
5. Every `TicketNumber` whose length is not 10, recorded and not rewritten.
6. Every `MST_RequestType` row: trimmed prefix length, `Code`, `IsActive`, both approval flags. Types the exact-2 rule would block are corrected or explicitly accepted first.
7. Active duplicate counts at each of the four Gate B specificities, plus the orphan and blank-agent counts.
8. Effective `EXECUTE` on `sys.sp_getapplock` for the `DbConn` identity.
9. Collation of `itsm.Tickets.TicketNumber` and of the database.
10. Index build is `ONLINE = ON`, or a named maintenance window accepts a blocking build.
11. IT/DBA choice of one schema mechanism.

**F-09 report**

12. Whether `VMS_PlantTimings.Branch_ID` equals `MST_UserMaster.BranchId`, `MST_Branch.Branch_ID`, or `MST_Branch.Id`, for more than one company and branch.
13. Duplicate `MST_Branch.Branch_ID` count.

**F-10 report**

14. Effective grants for `ITSM.Queue.View`, `ITSM.Ticket.View`, `ITSM.Approval.View`, `ITSM.ApprovalConfig.Manage`, and `UI.ITSM_CreateTicket.View`, resolved the way `BasePage.RebuildSessionPermissions` loads them: `dbo.UserRole` joined to `dbo.RolePermission` joined to `dbo.Permission`, `p.IsObsolete = 0`, key `p.PermissionKey` (`BasePage.cs` lines 153–186).
15. `itsm.Tickets.CompanyId` null count, and non-null values that match no company the application joins.
16. `itsm.MST_ApprovalConfiguration` active-row count. The filtered unique index in the migration allows one active row. The read checks the environment. It does not assume the index is deployed.

**Verification environment**

17. A disposable database for concurrency and fault injection, with `ALTER` on `itsm.TicketAuditLog` for the temporary trigger and a `DROP` in the same procedure. UAT and LIVE are not that database.

**Not a read, and not yet given**

18. The owner acknowledgements in §13.

---

## 10. Test matrix TC-01 through TC-59

Every row is **NOT RUN**. No test was executed. A later run records expected result, actual result, environment, data, evidence, and a disposition that was observed. UAT and LIVE are not authorised for any row. "Disposable database" means a database created for the test, including rows or schema the test needs. "Named-environment read" needs its own authorisation and does not update rows.

| Case | Covers | Expected result | Needs | Disposition |
|---|---|---|---|---|
| TC-01 | F-02 | Title, description, resolution, and comment containing `<` and `>` render as text. Line breaks remain on `itsm-prewrap`. | Rendered page | NOT RUN |
| TC-02 | F-02 | The same title renders as text in the list. | Rendered page | NOT RUN |
| TC-03 | F-03 | A 500-character rejection reason is stored, the ticket moves to the rejection status, and one rejection audit row exists. | Disposable database | NOT RUN |
| TC-04 | F-03 | A 501-character reason returns the validation message. Status, approvals, and audit are unchanged. The length failure happens before a context is required. | Service call | NOT RUN |
| TC-05 | F-04 | Resolve, reopen, resolve. The first resolution row has `NewValue` `Superseded`. The second has `NewValue` `Current`. The ticket shows the second text. `StatusChange.Detail` is null. | Disposable database | NOT RUN |
| TC-06 | F-04 | A resolution longer than 1000 characters is refused. Status is unchanged. | Disposable database. Waits on the §13 acknowledgement before it is an acceptance test of an enabled rule. | NOT RUN |
| TC-07 | F-04 | Reopen leaves `ResolutionText` and `ResolvedOn` on the ticket. | Disposable database | NOT RUN |
| TC-08 | F-05, X-3 | **Corrected.** A public comment of 1000 characters is stored in full on the comment row and on `Detail`. 1001 is refused and neither row is committed. An internal note of 989 user characters succeeds and `Detail` is exactly 1000. 990 user characters internal is refused and neither row is committed. The revision 1 wording "990 when marked internal" is withdrawn. | Disposable database. Waits on the §13 acknowledgement before enablement. | NOT RUN |
| TC-09 | F-06 | The requester does not see Approve or Reject. A direct approve post returns the separation-of-duties failure and does not change status. | Page and service | NOT RUN |
| TC-10 | F-06 | With both rows pending, the IT Head does not see Approve and does see Reject. An approve post returns the manager-first failure. | Page and service | NOT RUN |
| TC-11 | F-06 | A user who is not requester or assignee does not see the work-action controls. A direct status post is refused. | Page and service | NOT RUN |
| TC-12 | F-07 | Two active rows at the same specificity resolve to the lower `Id` on repeated calls against unchanged data. | Disposable database | NOT RUN |
| TC-13 | F-08, AR-15 | A forced audit-insert failure leaves the ticket status unchanged. A forced ticket-update failure leaves no new audit row. The trigger is dropped (TC-58). | Disposable database with `ALTER`. UAT and LIVE excluded | NOT RUN |
| TC-14 | F-08 | Approving the last pending row when `In Progress` is absent leaves the approval row and the ticket status unchanged. This proves the branch. It does not authorise promoting A3. | Disposable database | NOT RUN |
| TC-15 | F-01 | Two concurrent creates in one prefix and month receive different ten-character numbers, or one receives the controlled exhaustion or lock-timeout error. Neither persists a duplicate. | Disposable database, Gate B objects | NOT RUN |
| TC-16 | F-01, X-1 | The 10000th number in a prefix-month is refused. The last stored sequence remains 9999. An 11-character formatted result is refused. Existing numbers are unchanged. | Disposable database | NOT RUN |
| TC-17 | F-01 | Two request types with the same two-character prefix share one monthly sequence. This waits on the shared-sequence acknowledgement before it accepts an enabled rule. | Disposable database | NOT RUN |
| TC-18 | Regression | Existing detail and list pages open. Existing numbers, approvals, and audit rows still display. Session expiry still returns to login. | Non-production site | NOT RUN |
| TC-19 | F-09 | No test writes an SLA deadline. Mail timestamps are not a stored control. | Inspection | NOT RUN |
| TC-20 | F-10 | The reads in §9 are attached with environment, statement, timestamp, and reviewer. No row is updated. | Named-environment read | NOT RUN |
| TC-21 | F-01, AR-12 | A prefix that trims to one character is refused. No ticket, audit, approval, or notification row is persisted. | Disposable database | NOT RUN |
| TC-22 | F-01, X-1 | Stored values of length 6, 7, 8, and 9 that share the namespace do not affect the next allocation. The next number is ten characters and its sequence comes only from ten-character values. | Disposable database | NOT RUN |
| TC-23 | F-01, X-2 | A held application lock fails the create with the controlled message, persists nothing, and returns within the configured timeout. Injected `@rc` of `-1`, `-2`, `-3`, and `-999` fail. `0` and `1` do not. | Disposable database, two sessions | NOT RUN |
| TC-24 | F-01 | Case variants of one prefix produce one namespace under the stated normalisation, and the page does not see a unique-index collision. | Disposable database matching the recorded collation | NOT RUN |
| TC-25 | F-01 | A deliberate duplicate makes index creation fail and the read report the group. After a data authorisation removes it, creation succeeds and a duplicate insert is rejected. | Disposable copy. Not UAT, not LIVE | NOT RUN |
| TC-26 | F-01, AR-13 | A unique-index violation produces the controlled `InvalidOperationException` after the bounded retry. Ticket, audit, approvals, and notification are absent. | Disposable database | NOT RUN |
| TC-27 | F-02, AR-7 | `lblMessage` renders `<`, `>`, and `"` as text on all four ITSM pages. | Rendered page | NOT RUN |
| TC-28 | F-02 | An approval status containing `"` and `<` renders as text. The class stays `label-success`, `label-danger`, or `label-warning`. | Rendered page | NOT RUN |
| TC-29 | F-02 | A description containing a newline and `<script>` renders as literal text with the line break preserved. Computed `white-space` on `itsm-prewrap` is `pre-wrap`. | Rendered page | NOT RUN |
| TC-30 | F-03 | `ApproveTicket` with a 501-character comment returns `Fail`. No approval change, no audit row, ticket unchanged. | Service call | NOT RUN |
| TC-31 | F-03 | A 501-character override reason is refused by the service. No priority change and no priority audit row. | Service call | NOT RUN |
| TC-32 | F-04 | A 1001-character resolution is refused. Status, `ResolutionText`, and `ResolvedOn` are unchanged. No `Resolution` row and no `StatusChange` row. | Disposable database | NOT RUN |
| TC-33 | F-04 | Resolve, reopen, resolve. `NewValue` distinguishes `Current` from `Superseded`. | Disposable database | NOT RUN |
| TC-34 | F-04 | `ResolutionText` set and `ResolvedOn` null follows the preserve-and-record rule and writes no misleading history. | Disposable database | NOT RUN |
| TC-35 | F-04 | A pre-change resolution (text present, no `Resolution` row) reopens without gaining a `Resolution` row. | Disposable database | NOT RUN |
| TC-36 | F-05, AR-8 | 989-character internal note succeeds with `Detail` length 1000. 990 is refused. `ModifiedOn` is unchanged. | Disposable database | NOT RUN |
| TC-37 | F-05, X-4 | A status name and a priority name of 100 characters are stored in full in `OldValue` and `NewValue`. | Disposable database | NOT RUN |
| TC-38 | F-06 | A requester who is also the IT Head sees neither button. A direct approve post returns the separation-of-duties failure. D-10 is still Pending. | Page and service | NOT RUN |
| TC-39 | F-06 | The IT Head rejects while the Manager row is `Pending`. The Manager row becomes `Rejected` with the fixed comment. The ticket reaches the rejection status. One `Rejection` audit row exists. The outcome matches today's server behaviour. | Disposable database | NOT RUN |
| TC-40 | F-06 | A viewer who is not requester, assignee, or pending approver sees neither panel. Direct posts are refused. | Page and service | NOT RUN |
| TC-41 | F-07, AR-3 | An active mapping with an empty or whitespace `SupportAgentCode` falls through and does not assign an empty agent. | Disposable database | NOT RUN |
| TC-42 | F-07 | Duplicate active rows at each of the four specificities resolve to the lowest `Id`. The Gate B index rejects a second active insert and permits an inactive duplicate. | Disposable database | NOT RUN |
| TC-43 | F-08, X-5 | With `In Progress` inactive, approving the last pending row returns `Fail`, leaves that row `Pending`, and leaves the ticket in `Pending Approval`. | Disposable database. Not an A3 promotion | NOT RUN |
| TC-44 | F-08, X-5 | With `Rejected` and `Cancelled` both inactive, rejection returns `Fail` and changes no approval row. | Disposable database. Not an A3 promotion | NOT RUN |
| TC-45 | F-08 | Two sessions approve or reject the same ticket. One succeeds. The other receives the concurrency message. No orphan audit row. | Disposable database | NOT RUN |
| TC-46 | F-08 | A forced failure on the notification insert leaves no ticket, no audit, and no approval rows. | Disposable database | NOT RUN |
| TC-47 | F-08 | Forced audit-insert failure leaves status unchanged. Forced ticket-update failure leaves no audit row. A forced failure on the create path before commit leaves no ticket. | Disposable database with `ALTER` | NOT RUN |
| TC-48 | AR-1 | With `Pending Approval` absent, the test records the status `ResolveInitialStatusId` returns. The fallback is a known defect. A fail-closed result is expected only after an owner decision that does not exist. The test must not be read as accepting the fallback. | Disposable database | NOT RUN |
| TC-49 | AR-2 | The duplicate-name read shows no duplicate `MST_TicketStatus.Name`, or every name-based lookup is specified and deterministic. Neither is established. | Named-environment read, or a disposable copy | NOT RUN |
| TC-50 | X-1 | A pre-change nine-character number remains openable, listable, searchable, and auditable, and is not rewritten. | Disposable database | NOT RUN |
| TC-51 | AR-4 | An active all-null taxonomy row with `IsBranchDefault = 0` is ignored by every cascade step, is outside the four indexes, and a second such row can be inserted. | Disposable database | NOT RUN |
| TC-52 | AR-6 | After a ticket-number unique violation, the retry predicate observes `SqlException` 2601 or 2627 and the ticket-number index name. When `Rollback()` throws, the original exception is still the one observed. | Disposable database | NOT RUN |
| TC-53 | AR-5 | Two `Pending` rows for one approver: approving one leaves the ticket in `Pending Approval` while the other stays `Pending`. The read reports the pair. No unique index is created. | Disposable database | NOT RUN |
| TC-54 | AR-10 | The F-09 read reports the duplicate `Branch_ID` count. No index is created. | Named-environment read | NOT RUN |
| TC-55 | AR-11 | No ITSM query navigates `UserMaster.Branch`. `Ticket.Branch` in detail and list remains. | Source inspection | NOT RUN |
| TC-56 | AR-9 | No anti-forgery token API is referenced under `AnmolDristi/ITSM`. This is not a security-test pass. ViewState MAC remains unread. | Source inspection | NOT RUN |
| TC-57 | AR-14 | The Gate B package names one schema mechanism. | Document checkpoint | NOT RUN |
| TC-58 | AR-15 | After TC-13 and TC-47, the test trigger is absent. | Disposable database | NOT RUN |
| TC-59 | AR-16 | Description search is unchanged and the grid still does not show `Description`. D-1 remains Pending. This row is a residual, not a fix and not a pass. | Source inspection | NOT RUN |

Tests that need a database write, a schema object, or fault injection: TC-03 through TC-08, TC-12 through TC-17, TC-21 through TC-26, TC-30 through TC-37, TC-39, TC-41 through TC-48, TC-50 through TC-53, and TC-58. TC-13, TC-47, and TC-58 also need `ALTER`. None of these are authorised against UAT or LIVE.

Tests that need a named-environment read: TC-20, TC-49 when run against a shared environment, and TC-54.

Tests that need an owner decision before they can accept an enabled rule: TC-06, TC-08, TC-17, and TC-48's fail-closed branch. Until those decisions exist, the tests may be drafted and must not be reported as acceptance of a shipped limit.

Rendered-page tests need a site that was not started: TC-01, TC-02, TC-09, TC-10, TC-11, TC-18, TC-27, TC-28, TC-29, TC-38, TC-40.

---

## 11. Backward compatibility

- Historical `TicketNumber` values are not rewritten, including values shorter than 10 characters. They remain openable and searchable (TC-50). They do not advance the new sequence unless they are exactly 10 characters in the namespace (X-1).
- Existing `TicketAuditLog` rows are not updated or deleted. Pre-change resolutions are not backfilled.
- Request-type prefixes are not changed by this design. A prefix the exact-2 rule would block stays usable until the read and the owner acceptance in AR-12.
- The transition map and the approval order checks stay as they are. Buttons the server already refuses may be hidden in A1. F-08's fail-closed returns are absent until A3 is promoted.
- Session keys stay `WORKMAN`, `USERID`, and `BRANCH`.
- `IsTerminal` is not updated from application code.
- Queue company visibility and the description search stay as they are. D-1 stays Pending.
- SLA deadlines are not persisted. D-7 stays Pending.
- `ResolveInitialStatusId` keeps its fallback. AR-1 stays a known defect until an owner decides.
- Identity gaps after a rolled-back insert are expected and are not repaired. Audit order remains `ActionedOn`.
- Application rollback after A2 leaves `Resolution` audit rows in place. Those rows are history.

---

## 12. Deployment and rollback strategy

No deployment is authorised. A future package, after a separate authorisation, contains the change list, the diff, this revision, the FreeBuff review, the reconciliation, the precondition reads for any Gate B object, backup confirmation, the schema-mechanism choice, smoke tests drawn from §10, and a rollback that drops only a new index or redeploys the previous application build.

| Release | Contains | Does not contain | Rollback |
|---|---|---|---|
| A1 | Encoding, panel bindings, `OrderBy`, guarded rollback on the six update methods | F-08 fail-closed returns, length caps, resolution audit rows, indexes, applock | Redeploy the previous build. No ticket delete. |
| A2 | Length refusals and resolution history, only after §13 is recorded | F-08, Gate B objects | Redeploy the previous build. `Resolution` rows already written stay. |
| A3 | The two fail-closed returns, only after read 1 passes for that environment | A change to `ResolveInitialStatusId`. A status insert | Redeploy the previous build. Do not enable A3 to discover a missing row. |
| B | Allocation, lock batch, unique indexes, retry, guarded rollback on create. The corrected shared-prefix allocator is deployed before the unfiltered `TicketNumber` index, or in the same window in which the previous per-request-type allocator serves no create (§16) | A counter table, unless IT rejects application locks in a later ruling. This revision does not implement either store. The previous allocator must not run while the new index is present | Drop the `TicketNumber` index first and confirm it is absent. Only then redeploy the previous application build. Rows remain. Starting the previous allocator while the index is still present is forbidden. |

Rollback does not delete tickets or audit rows created after deployment. A unique index can be dropped without deleting those rows.

The verification trigger is not in any release. It exists only on the disposable database and is dropped before the procedure ends (TC-58).

---

## 13. Outstanding owner acknowledgements

These are not D-19 through D-n. They are not written into the register. They are not approvals. A2 and the shared-sequence part of Gate B stay disabled until the named acknowledgement exists as a written record. An empty cell is not that record.

| Acknowledgement | Why it is separate from a closed decision | Who the register already names, where it names someone | State |
|---|---|---|---|
| Resolution text longer than 1000 characters will be refused | D-17 asks for the minimum. The column is unbounded today. The register's option A describes this refusal and is Pending. | Service owner for any minimum. IT leadership for the audit-copy ceiling | Not recorded |
| Public comments move from a 2000-character hint to 1000, and internal notes to 989 user characters | No D-id owns the comment box. D-5 is the On Hold question and stays Pending | Service owner, because users lose stated capacity | Not recorded |
| Two request types that share a prefix will share one monthly sequence and one 9999 ceiling | D-4 asks how duplicates are prevented and is Pending. D-9 asks which approval flags apply and is Pending. The catalogue effect is the additional acknowledgement | IT leadership for the method (D-4). Service owner for the displayed number (D-4) and for the request-type catalogue | Not recorded |
| A request type whose trimmed prefix is not two characters will be unable to create tickets | The exact-2 rule is a capability removal for that type | Service owner to accept the block or to authorise a data correction | Not recorded |
| Empty `SupportAgentCode` will fall through to the next cascade step, if any such row exists | Changes who is assigned. D-14 stays Pending either way | IT, and the service owner if the count is not zero | Not recorded |
| `ResolveInitialStatusId` continues to fail open | Changing it would stop creation when `Pending Approval` is missing | Service owner | No decision requested by A3. The code stays as it is |
| Prefix normalisation case, and the lock timeout in milliseconds | Technical parameters. Selecting them is not a D-4 ruling | IT | Not selected |
| Schema mechanism for the new indexes | EF migration or standalone script | IT and DBA | Not selected |
| Anti-forgery owner and date | Outside this design | Not named | Not assigned |

D-1 through D-18, as questions in the register, all remain Pending. This table does not mark any of them Approved or Deferred.

---

## 14. Implementation-readiness checklist

Every line is unmet.

| # | Check | Met |
|---|---|---|
| 1 | A change authorisation names A1 | No |
| 2 | Owner acknowledgements for the resolution maximum and the comment limits are recorded | No |
| 3 | A change authorisation names A2, and it is the same release as the override-reason bound | No |
| 4 | Read 1 is attached for the target environment and passes the A3 condition | No |
| 5 | A3 is withheld from the A1 and A2 builds | Required by this design, and not yet a release plan |
| 6 | IT/DBA have named one schema mechanism | No |
| 7 | Gate B precondition reads are attached and pass, or a separate data authorisation covers each failure | No |
| 8 | Shared-sequence acknowledgement is recorded | No |
| 9 | Lock timeout and prefix case are named by IT | No |
| 10 | `EXECUTE` on `sys.sp_getapplock` is confirmed for the application identity | No |
| 11 | D-1 through D-18 remain pending, and no Gate C behaviour is in the build | True as a constraint. Not a reason to start |
| 12 | TC-01 through TC-59 have an observed disposition other than NOT RUN | No. All are NOT RUN |
| 13 | Disposable database for fault injection is named, and UAT and LIVE are excluded | No |
| 14 | `Web.config` remains unread and unchanged by this work | True for this revision. Not a build step |

---

## 15. Explicit authorization status

**NOT READY FOR IMPLEMENTATION.**

- No application file may be edited on the authority of this revision.
- No SQL file, migration, index, trigger, or configuration change may be written or executed on the authority of this revision.
- Gates A1, A2, A3, B, and C are closed.
- A3 must not be enabled in order to test a live status table.
- D-1 through D-18 are Pending. Register recommendations are not approvals.
- The acknowledgements in §13 are not recorded.
- Every test disposition is **NOT RUN**.
- The business decision register is unchanged. D-1 through D-18 stay Pending.
- Application files, SQL, migrations, and `AnmolDristi/Web.config` are unchanged by the in-place correction in §16.

This revision is the proposed design a later authorisation would implement. The authorisation is not this file. §16 does not open a gate.

---

## 16. In-place corrections — Y-26, Y-27, Z-8

**Date:** 25 November 2026. These corrections are part of this file. They are not a new revision and they are not an implementation authorisation. The same rules are recorded in `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV2.md` and `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV3.md`. Every test they name is **NOT RUN**.

### Y-26 — Actor length

`TicketComment.CreatedByCode` is `[Required]` and `[StringLength(50)]` (`TicketComment.cs` lines 26–28). `TicketAuditLog.ActorCode` is `[Required]` and `[StringLength(50)]` (`TicketAuditLog.cs` lines 69–72). `Ticket.ModifiedByCode` is `[StringLength(50)]` (`Ticket.cs` lines 138–139). `ItsmDbContext` does not set `ValidateOnSaveEnabled`, so the EF6 default applies and `SaveChanges` validates those annotations.

No method in `TicketUpdateService` or `TicketService` returns `Fail` because an actor string is longer than 50. This design does not add that guard. An actor value longer than 50 is assigned as the code assigns it today. `SaveChanges` then fails the validation. The existing `catch` rolls the transaction back and rethrows. No ticket column, comment, or audit row from that attempt is committed. The result is not `TicketUpdateResult.Fail`, and `ApplyResult` does not run. The validation exception was not executed in a harness for this correction. TC-89 covers it. TC-64's actor clause no longer expects `Fail`.

### Y-27 — `OldValue` on the `Current` resolution row

Every `Resolution` audit row whose `NewValue` is `Current` stores this `OldValue` and no other text:

```text
current;resolvedOn={stamp}
```

`{stamp}` is the transition's single `now`, formatted with `DateTime.ToString("o", CultureInfo.InvariantCulture)`, and it is the same value written to `ticket.ResolvedOn`. `ResolvedOn` is included. `ModifiedByCode` is not repeated in this marker; the actor is `ActorCode` on the same row. `Detail` remains the new trimmed resolution, which is within the 1000-character audit maximum. If the marker would exceed 200 characters, return `Fail` and write nothing. A round-trip timestamp with this prefix fits in 200. The check was not executed. TC-90 asserts the persisted value. The superseded row keeps its own marker and is not given this format.

### Z-8 — Index and allocator order

The corrected shared-prefix allocator, which does not filter the maximum read by `RequestTypeId`, is deployed before the unfiltered unique index on `itsm.Tickets(TicketNumber)`, or both are applied in one maintenance window during which the previous per-request-type allocator serves no create. The previous allocator must not run while that index exists. Creating the index and leaving the previous build in service is withdrawn.

Rollback stops ticket creation, drops that index, confirms it is absent, and only then allows the previous build to serve creates. Rows are not deleted. Creation stays stopped across that sequence. If the index drop fails, the previous allocator stays stopped. Collision evidence is read 4a at zero before the index is created, and TC-88 on a disposable database. Neither was run. UAT and LIVE are excluded. Gate B stays closed. D-4 stays Pending.
