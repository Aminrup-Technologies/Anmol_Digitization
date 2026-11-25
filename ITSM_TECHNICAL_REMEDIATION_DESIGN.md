# ITSM technical remediation design

**Document:** ITSM_TECHNICAL_REMEDIATION_DESIGN.md
**Deliverable:** R-02
**Status:** Draft for independent technical review by FreeBuff
**Date:** 24 November 2026
**Companion:** `ITSM_SOURCE_TO_FINDING_TRACEABILITY.md` (R-01)
**Governing brief:** ITSM Technical Remediation Plan, 24 November 2026
**Change status:** Design only. No application edit, schema change, migration, or deployment is authorised by this document.

## 1. Purpose

This design states the implementation that would satisfy F-01 through F-10 after a later authorisation. It keeps the current modular layout: rules in the BLL, persistence in the EF6 DAL, and no inline SQL in the pages. It adds no workflow state and no approval rule. D-1 through D-18 stay pending. Where a control would change who may act, or would persist a new fact, the design stops at the technical boundary and names the decision it waits on.

## 2. Gates

| Gate | Contents | May be built when |
|---|---|---|
| A | F-02 encoding, F-03 rejection length, F-05 audit length, F-04 history preservation within the existing 1000-character `Detail`, F-06 panel alignment, F-08 fail-closed status lookup, F-07 `OrderBy` stability | A change authorisation names these items. No schema change. No new workflow state. |
| B | F-01 unique `TicketNumber` constraint and serialised allocation, F-07 unique mapping key, any widening of `TicketAuditLog.Detail` or a resolution-event table | A reviewed SQL design, a read-only precondition result, and explicit IT authorisation. |
| C | D-1 through D-18 | A written owner ruling. This design does not supply one. |

F-09 and F-10 produce no code in either gate. They produce a read-only validation report under a separate authorisation.

## 3. Session contract the design keeps

Pages continue to require the keys they require today. `WORKMAN` remains the employee code passed into ITSM services. `USERID` and `BRANCH` remain presence checks on create, list, and detail. SLA calculation continues to receive `MST_UserMaster.BranchId` loaded for that employee code. Dispatch continues to receive `MST_Branch.Id` resolved by `Branch_ID == requester.BranchId`.

The design does not read `Session["BRANCH"]` as `MST_Branch.Id`. Login assigns `BRANCH` from the authentication row's `BranchId` (`login.aspx.cs` line 217). That is the legacy user-master value, which the create path already re-reads from `MST_UserMaster`.

## 4. F-01 — ticket-number allocation

### 4.1 Format the source already emits

`GenerateTicketNumber` builds:

`trimmedPrefix + yy + MM + sequence.ToString("D4")`

The prefix is `RequestType.Prefix`, then `RequestType.Code`, then the literal `XX`. It is trimmed. If the trimmed value is longer than two characters, the method keeps the first two. `MST_RequestType.Prefix` is `char(2)` in the initial migration, so a stored value can come back space-padded. Trim removes that padding. A one-character prefix is kept and produces a nine-character number.

The public format stays this shape until D-4 says otherwise. The design does not renumber stored tickets and does not rewrite a prefix.

### 4.2 Sequence ownership

One sequence belongs to one displayed namespace: the two-character prefix plus the two-digit year plus the two-digit month. The maximum is taken from every `TicketNumber` that starts with that six-character namespace. The `RequestTypeId` filter is removed, so two request types that display the same prefix cannot allocate the same visible number.

That ownership rule is the correction required by F-01. It is not a ruling on which request types should share a prefix. D-9 stays pending. If two active change types must remain distinguishable by number, they need different prefixes or this shared sequence. The design does not choose which.

### 4.3 Allocation algorithm, on the create connection

All of the following runs on the `ItsmDbContext` that already owns the create transaction, before `Tickets.Add`.

1. Resolve the request type as today.
2. Take the trimmed prefix. If its length is not exactly 2, throw `InvalidOperationException` with a message that the request type prefix is not a two-character numbering prefix. Do not substring, and do not insert. `CreateTicket.aspx.cs` lines 203–208 already show `InvalidOperationException.Message` on the page.
3. Build the six-character namespace.
4. Take a transaction-scoped application lock on the resource `ITSM.TicketNumber.{prefix}.{yy}{MM}` for the life of the create transaction. The lock call is `sp_getapplock` with `@LockOwner = 'Transaction'`, `@LockMode = 'Exclusive'`, and a short timeout, executed with `ExecuteSqlCommand` on the create context. A timeout or a negative return code throws `InvalidOperationException` and the existing `catch` rolls the transaction back. No ticket row is inserted.
5. Read the matching ticket numbers on that same context, without `AsNoTracking` being used as a substitute for the lock. Parse a trailing integer only when the stored value is at least as long as the namespace. Take the maximum. The next sequence is that maximum plus one, or 1 when none parse.
6. If the next sequence is greater than 9999, throw `InvalidOperationException` stating that the monthly sequence for that prefix is exhausted. Do not call `ToString("D4")` on a value above 9999, and do not insert.
7. Insert the ten-character number with the rest of the create, still inside the same transaction. The lock is released on commit or rollback.

The unique constraint in §4.5 is the integrity backstop. The lock is the serialisation that keeps ordinary concurrent creates from depending on a retry. Both are required. Neither is present in the source.

A uniqueness conflict still has to be handled, because a lock timeout or a historical duplicate race can surface as a constraint violation. On that violation the create transaction is rolled back, the context is disposed, and the whole `CreateTicket` is retried on a new context. The bound is 3 attempts. The third failure returns the controlled error through `InvalidOperationException`. A retry inside the failed transaction is not used.

This chooses `sp_getapplock` rather than a new counter table so Gate B does not add a second numbering store. A counter table remains a valid alternative if IT rejects application locks. This document does not implement either.

### 4.4 Historical numbers

Stored values stay as they are, including any nine-character numbers already issued. New allocation emits only ten-character numbers in the six-character namespace. The maximum scan includes every stored number in that namespace, so an existing ten-character number still advances the sequence. A nine-character number has a different string prefix when its prefix was one character, and the unique constraint prevents an accidental collision if one is ever formatted into the new namespace.

### 4.5 Database constraint

Proposed object, not created by this design:

- Unique nonclustered index on `itsm.Tickets(TicketNumber)`.
- Created only after the precondition read in §4.6 returns zero duplicate groups.
- Existing duplicates are a separate data authorisation. This design does not include an update that rewrites them.

Proposed script name, to be written later under the plan's SQL rules: `ticket_number_unique_index.sql`.

Header the future script must carry:

- When: the authorisation date.
- Why: concurrent creates can insert the same `TicketNumber` because the column has no unique index.
- What: create the unique index after the duplicate count is zero.
- Preconditions: the §4.6 read, named environment, reviewer, timestamp.
- Transaction and rollback: index creation in its own transaction; drop the index to roll back. Dropping the index does not delete tickets.
- Environment and authorisation: named in the script. This design grants none.

### 4.6 Precondition reads, not run

These are the reads a later authorisation would run. They are not in this change.

| Read | Pass condition for the index |
|---|---|
| Groups of `itsm.Tickets.TicketNumber` with `COUNT(*) > 1` | Zero groups |
| `TicketNumber` values whose length is not 10 | Recorded. They block nothing except a decision to rewrite them, which this design does not take |
| `MST_RequestType` rows whose trimmed `Prefix` length is not 2, with `Code`, `IsActive`, and both approval flags | Recorded. Create will refuse those types after the Gate B change until the prefix is corrected under a separate authorisation |

## 5. F-02 — HTML encoding at the output boundary

Encode in `TicketDetail.aspx.cs` `LoadTicket`, at the assignment to each label that shows stored or master text. Use `System.Web.HttpUtility.HtmlEncode`. Keep every control ID.

| Label | Source property | Rendering |
|---|---|---|
| `lblTitle` | `Title` | Encode |
| `lblDescription` | `Description`, or the existing empty placeholder | Encode. Set `white-space: pre-wrap` on the existing description container so newlines remain line breaks. The stylesheet rule does that without inserting markup |
| `lblResolution` | `ResolutionText` | Same encode-and-pre-wrap treatment on the existing resolution container |
| `lblRequestType`, `lblRequester`, `lblAssignee`, `lblCategory`, `lblBranch`, `lblImpactUrgency`, `lblCsatFeedback` | The DTO strings already assigned | Encode each component before concatenation |
| `lblTicketNumber` | system-issued number | Encode the number. The leading `#` stays literal |
| `lblTenantBadge` | company description | Encode the name. The numeric fallback stays a formatted integer |
| `lblStatusBadge`, `lblPriorityBadge` | status and priority names | Encode. The CSS class continues to be derived from the status name; the class string is restricted to the existing lowercase-and-hyphen transform and is not a second HTML sink for the raw name if the name is encoded only for the text |

Comment text and approval comments stay on their current `HtmlEncode` bindings. Also encode `CreatedByName` in `rptComments` and `ApproverName` in `rptApprovals`. Approval level and status are short stored tokens; encode them in the same binding so a stored value cannot be markup.

On `TicketList.aspx`, encode the ticket-number `Eval` inside the link. Priority and status badge labels that bind with `Eval` get the same encode. `BoundField` columns already encode by framework default; leave those columns as `BoundField`.

The creation mail already encodes the title. Leave that call. Do not add the description to the mail as raw HTML.

Database storage is not the boundary. A later rich-text requirement is a separate review. This design does not set `ValidateRequest` and does not read `Web.config`. Encoding does not wait on that reading.

## 6. F-03 — rejection reason length

In `RejectTicket`, after the existing blank check and before any approval row is modified:

- Trim the reason.
- If the trimmed length is greater than 500, return `TicketUpdateResult.Fail` with the message `Rejection reason must be 500 characters or fewer.`
- Do not assign `Comments`, do not change approval status, and do not call `AddAudit`.

Set `MaxLength="500"` on `txtRejectionReason`. The server check is the rule. The attribute is a client hint and can be bypassed by a direct post.

Acceptance, once a later build is tested: 500 characters succeed; 501 characters return the message through `ApplyResult`; the ticket stays in `Pending Approval`; no approval row and no audit row are committed. The existing concurrency and rethrow behaviour stays for unexpected failures. This design does not add a page-level catch-all as the length fix.

`ApproveTicket` assigns `comments` with no length check. The approve button currently passes null. If a comment is later supplied on that path, the same 500-character check applies before assignment. That is the same column.

## 7. F-04 — resolution history

The transition map is unchanged. `Resolved` → `In Progress` and `Resolved` → `Closed` stay legal when the deployed status is non-terminal. Who may use those edges stays in D-2 and D-6. This design does not add an administrative close and does not clear `IsTerminal` from application code.

`ResolutionText` and `ResolvedOn` remain the current-state fields. They are not the history.

### 7.1 First resolve

When the target name is `Resolved` and the ticket has no current `ResolutionText`:

- Reject a blank resolution, as today.
- Reject a trimmed resolution longer than 1000 characters with `TicketUpdateResult.Fail` and the message that the resolution must be 1000 characters or fewer so the audit row can store it. Do not truncate.
- In the same `SaveChanges` as the status change, write a `TicketAuditLog` row with `ActionType` `Resolution`, `Detail` equal to the full trimmed text, `ActorCode` equal to the actor, and `ActionedOn` equal to the resolve time. `OldValue` holds `ResolvedOn=` plus the new timestamp in round-trip form. That value fits the 200-character `OldValue`.
- The existing `StatusChange` row remains.

The 1000-character ceiling is the capacity of `TicketAuditLog.Detail`. It is the Gate A way to keep the audit complete. It is not a ruling on D-17's minimum length. D-17 stays pending. A wider column, or a resolution-event table, is Gate B and needs IT authorisation. Until that exists, a longer summary is refused rather than stored only on the ticket.

### 7.2 Reopen

When the target is `In Progress` and the current status is `Resolved`:

- Leave `ResolutionText` and `ResolvedOn` unchanged.
- Write the status audit with a fixed detail `Resolution text retained on the ticket.` The fixed sentence fits in 1000 characters.
- Do not refuse reopen because the retained text is long. The text is still on the ticket, so the transition does not destroy it.

### 7.3 Re-resolve

When the target is `Resolved` and `ResolutionText` already has a value:

- If that previous text is longer than 1000 characters, return `Fail` and change nothing. The long text stays on the ticket. Replacing it would drop history that `Detail` cannot hold. A Gate B table is the later way to re-resolve those rows.
- If it fits, add one audit row in the same `SaveChanges`, before the columns are replaced: `ActionType` `Resolution`, `Detail` the previous text, `OldValue` the previous `ResolvedOn` and the previous `ModifiedByCode`. Then assign the new text and the new `ResolvedOn`, subject to the same 1000-character check on the new text, and write the new `Resolution` audit row.
- A failure at any check returns before assignment. The uncommitted transaction rolls back on dispose. No status change and no audit row are committed.

`Closed` still sets `ClosedOn` only. It does not clear the resolution columns.

If the F-10 read finds `Resolved.IsTerminal = 1`, close and reopen are already blocked by the existing terminal check. The design does not clear that flag. The preservation code still guards the resolve assignment so a later flag change cannot overwrite history silently.

## 8. F-05 — audit detail length

Gate A aligns input with the column that already exists.

| Input | Rule |
|---|---|
| Comment text | Build the audit detail first: `"[Internal] "` plus the trimmed text when the note is internal, otherwise the trimmed text. If that string's length is greater than 1000, return `Fail` with a message that names the limit. Do not insert the comment and do not call `Truncate`. |
| Rejection reason | Already limited to 500 by F-03. `AddAudit` receives a value inside 1000. |
| Priority override reason | `txtOverrideReason` declares 500. The server does not check it, and the reason is the audit `Detail`. Reject a trimmed reason longer than 500 before the priority is changed. 500 fits in `Detail`. |
| Resolution text | Limited to 1000 by F-04 when it is copied into `Detail`. |

Set `txtComment.MaxLength` to 1000. An internal note's server limit is 990 characters of user text because the ten-character prefix shares the 1000-character column. The message states that. `MaxLength` remains a hint.

`Truncate` stays in `AddAudit` only as a last-resort guard for values this design has already bounded, until a reviewer prefers it removed. It must not be the mechanism that accepts a 2000-character comment. The comment row and the audit row either both contain the full text or neither is committed.

Gate B, if IT later authorises a wider `Detail`, replaces the 1000-character refusal with the new capacity. This design does not widen the column. Silent truncation is not reintroduced in that change.

D-5's On Hold reason, if an owner later requires one, uses this same refusal. It does not reuse `Truncate`.

## 9. F-06 — approval panel eligibility

One server predicate decides each button. The page binds visibility to those predicates. `ApproveTicket` and `RejectTicket` keep their checks.

| Predicate | True when |
|---|---|
| May approve | `CanApproveTicket` is true (actor is not the requester, and a pending row exists for the actor), the ticket status name is `Pending Approval`, and the actor's pending row is not `ItHead` while a `Manager` row for the ticket is still `Pending` |
| May reject | `CanApproveTicket` is true and the status name is `Pending Approval`. Order is not added. `RejectTicket` does not require the manager row to be completed, and this design does not invent that rule |

`GetTicketDetail` computes both flags. `pnlApprovalActions` is visible when either flag is true. `btnApprove` is visible only when May approve is true. `txtRejectionReason` and `btnReject` stay visible when May reject is true. Control IDs are unchanged.

Effects that follow from the existing server rules:

- A requester who is also the IT Head does not see Approve or Reject. The service still returns the separation-of-duties failure on a direct post. Who can approve that ticket stays in D-10.
- The IT Head does not see Approve while the manager row is pending. The IT Head can still reject, because the server accepts that post today.
- A pending row on a ticket whose status is not `Pending Approval` does not show the panel. `ApproveTicket` and `RejectTicket` still reject the post.

The work-action panel is the same class of mismatch. In the same page change, set `pnlWorkActions.Visible` only when the ticket is non-terminal, the status is not `Pending Approval`, and `ItsmTicketAccess.CanProcessTicket` is true for the actor. That hides controls the server already refuses. It does not change `CanProcessTicket`. D-2 stays pending, so the requester-or-assignee rule stays as it is.

## 10. F-07 — dispatch determinism

The cascade order stays item, then subcategory, then category, then branch default.

Each of `FindMapping`, `FindCategoryMapping`, and `FindBranchDefault` gains `.OrderBy(m => m.Id)` before `FirstOrDefault`. For a given set of active rows, the lowest identity is the selected agent. That makes the read stable. It does not make a duplicate row the approved business winner.

Automatic pickup, and any new rule that "the mapped agent may take this ticket," stay out of this design. D-14 stays pending. The current column is one `SupportAgentCode` per row. A support-group model is not designed here.

The unique key, Gate B, after a duplicate count:

| Cascade step | One active row for |
|---|---|
| Item | `BranchId`, `CategoryId`, `SubCategoryId`, `ItemId`, `IsBranchDefault = 0` |
| Subcategory | the same, with `ItemId` null |
| Category | `BranchId`, `CategoryId`, both finer keys null, `IsBranchDefault = 0` |
| Branch default | `BranchId`, `IsBranchDefault = 1`, category keys null |

The index is filtered to `IsActive = 1`. Inactive history can repeat. The index is created only when the precondition count of active duplicates is zero. Duplicate repair is a separate data authorisation.

Proposed later script name: `support_mapping_unique_key.sql`. Same header rules as §4.5. This document does not create the file.

`OrderBy` may ship in Gate A without the index. The index waits on the read and on IT authorisation. Pickup waits on both, and on D-14.

## 11. F-08 — transaction boundaries

The six update methods and `CreateTicket` already put the ticket write and the audit insert in one transaction, as R-01 §4 records. The design does not wrap them in a second `TransactionScope` or a second `BeginTransaction`.

Two fail-closed corrections belong in the existing methods, before `Commit`:

- `ApproveTicket`: when no other pending row remains and no active status named `In Progress` exists, return `Fail` and leave the approval row unchanged. The message states that the In Progress status is not configured.
- `RejectTicket`: when neither an active `Rejected` nor an active `Cancelled` status exists, return `Fail` and leave every approval row unchanged. The message states that the rejection status is not configured.

Those branches today can commit an approval outcome without the matching ticket status. The correction uses the status names the code already looks up. It adds no status.

`SlaService` and `DispatchService` keep their own contexts for this design. They only read, and they run before the ticket insert. Number allocation and the application lock stay on the create context, which is the F-01 requirement. Moving SLA and dispatch onto the create context is optional cleanup and is not required to meet the F-08 acceptance criteria.

Verification, when a test environment is authorised, uses a disposable database. A test-only trigger that throws on `INSERT` into `itsm.TicketAuditLog` must roll the ticket status back. A failure on the ticket update must leave no audit row. The trigger is not a production object and is not part of the application change. This design does not add a fault-injection switch to the service.

No transition path in the ITSM pages writes status or audit except through `TicketService` and `TicketUpdateService`. Pages stay that way.

## 12. F-09 — SLA branch key

No SLA column is added to `Ticket`. No breach mail is added. The creation mail may continue to include the two timestamps it already computes. Those timestamps are not a stored deadline.

The four identifiers, and what the source does with them:

| Identifier | Role in the source |
|---|---|
| `MST_UserMaster.BranchId` | Passed to `CalculateSla`. Login copies the authentication row's `BranchId` into `Session["BRANCH"]`. |
| `MST_Branch.Branch_ID` | Matched to `MST_UserMaster.BranchId` to find the branch row. |
| `MST_Branch.Id` | Stored on `Tickets.BranchId` and passed to `ResolveAssignee`. |
| `VMS_PlantTimings.Branch_ID` | Compared with the integer `CalculateSla` received, which is `MST_UserMaster.BranchId`. |

A later read, in a named environment, has to show whether `VMS_PlantTimings.Branch_ID` equals `MST_UserMaster.BranchId`, equals `MST_Branch.Branch_ID`, or equals `MST_Branch.Id`, for more than one company and branch. The entity comments are not that read. The `[ForeignKey("BranchId")]` on `UserMaster` points at `Branch.Id` and disagrees with the comment that `BranchId` stores `Branch_ID`. `CreateTicket` uses the explicit `Branch_ID` query and does not use the navigation. The design does not change that mapping until the read exists. Changing the foreign-key attribute is a separate review after the read, because it can alter EF joins elsewhere.

D-7 stays pending. Persisting a due date is not authorised by a future ruling on D-7 until this read is attached to the ruling.

## 13. F-10 — deployed configuration

The following reads are the validation report R-04. They were not run. Migration files, `Configuration.Seed`, and the permission scripts in the repository are not substitutes.

| # | Read | Why |
|---|---|---|
| 1 | `itsm.MST_TicketStatus`: `Name`, `IsTerminal`, `IsActive`, and any duplicate names. `Resolved` is the row that gates close and reopen | The seed does not update an existing table |
| 2 | `itsm.MST_RequestType`: `Code`, `Prefix`, trimmed prefix length, `RequiresManagerApproval`, `RequiresItHeadApproval`, `IsActive` | No request-type seed exists in the ITSM migrations that were read |
| 3 | Effective grants for `ITSM.Queue.View`, `ITSM.Ticket.View`, `ITSM.Approval.View`, `ITSM.ApprovalConfig.Manage`, and `UI.ITSM_CreateTicket.View` | Script files can differ from the database |
| 4 | `itsm.Tickets.CompanyId` null count, and tickets whose `CompanyId` is not null and matches no company lookup the application joins | F-10's null and inconsistent CompanyID check. Mutations already refuse a null `CompanyId`. This read sizes that population and does not authorise a backfill |
| 5 | The F-01 duplicate and prefix reads | Index precondition |
| 6 | The F-07 duplicate-mapping count | Index precondition |
| 7 | The F-09 branch-key sample | SLA persistence gate |

Each result records the environment name, the statement, the timestamp, and the person who ran it. No correction script runs from that report without a separate authorisation.

`ChangeStatus` trusts `IsTerminal` before the transition map. If the read shows `Resolved` terminal, the map edges out of `Resolved` are unreachable in that environment. The design records that fact and does not update the flag.

## 14. Test cases

These cases are specified. None was executed. A later run records expected result, actual result, environment, test data, evidence, and disposition. A disposition of pass in that run has to be observed. This document's disposition for every row is **Not run**.

| Case | Finding | Expected result, once the corresponding gate is built |
|---|---|---|
| TC-01 | F-02 | A stored title, description, resolution, and comment containing angle brackets render as text on ticket detail. The description's line breaks remain visible. |
| TC-02 | F-02 | The same title renders as text in the ticket list. |
| TC-03 | F-03 | A 500-character rejection reason is stored, the ticket moves to the rejection status, and one rejection audit row exists. |
| TC-04 | F-03 | A 501-character rejection reason shows the validation message. Status, approval rows, and audit rows are unchanged. |
| TC-05 | F-04 | Resolve, reopen to In Progress, and resolve again. The first resolution text and its timestamp are present on an audit row after the second resolve. The ticket shows the second text. |
| TC-06 | F-04 | A resolution longer than 1000 characters is refused. The ticket status is unchanged. |
| TC-07 | F-04 | Reopen leaves `ResolutionText` and `ResolvedOn` on the ticket. |
| TC-08 | F-05 | A comment of 1000 characters (990 when marked internal) is stored in full on the comment row and on the audit row. One more character is refused, and neither row is added. |
| TC-09 | F-06 | The requester does not see Approve or Reject. A direct approve post returns the separation-of-duties failure and does not change status. |
| TC-10 | F-06 | With manager and IT Head rows both pending, the IT Head does not see Approve and does see Reject. An approve post returns the manager-first failure. |
| TC-11 | F-06 | A user who is not the requester or the assignee does not see the work-action controls. A direct status post is refused. |
| TC-12 | F-07 | Two active rows at the same cascade specificity resolve to the lower `Id` on repeated calls. |
| TC-13 | F-08 | On a disposable database, a forced audit-insert failure leaves the ticket status unchanged. A forced ticket-update failure leaves no new audit row. |
| TC-14 | F-08 | Approving the last pending row when `In Progress` is absent leaves the approval row and the ticket status unchanged. |
| TC-15 | F-01 | After Gate B, two concurrent creates in one prefix and month receive different ten-character numbers, or one receives the controlled exhaustion or lock-timeout error. Neither path persists a duplicate. |
| TC-16 | F-01 | The 10000th number in a prefix-month is refused. The last stored number remains `9999` in that namespace. Existing numbers are unchanged. |
| TC-17 | F-01 | Two request types with the same two-character prefix share one monthly sequence. |
| TC-18 | Regression | Existing detail and list pages open. Existing ticket numbers, approval rows, and audit rows are still displayed. Session expiry still returns to login. |
| TC-19 | F-09 | No test writes an SLA deadline. Mail-only output may be inspected and is not treated as a stored control. |
| TC-20 | F-10 | The R-04 reads are attached with environment, timestamp, and reviewer. No row in this design is updated by those reads. |

Concurrent-creation and forced-failure cases run only in an environment the authorisation names. They are not run against UAT or LIVE under this document.

## 15. Backward compatibility

- Historical `TicketNumber` values are not rewritten.
- Existing `TicketAuditLog` rows are not updated or deleted.
- Request-type prefixes are not changed by this design.
- The transition map and the approval order checks stay as they are, apart from hiding buttons the server already refuses and failing closed when a required status row is missing.
- Session keys stay `WORKMAN`, `USERID`, and `BRANCH`.
- `IsTerminal` is not updated from application code.
- Queue company visibility is unchanged. D-1 stays pending.
- SLA deadlines are not persisted.

## 16. Deployment shape, for a later package

No deployment is authorised. A future package that implements an authorised subset contains the change list, the diff, FreeBuff's review, the precondition reads for any Gate B object, backup confirmation, migration order, application order, smoke tests drawn from §14, and a rollback that drops only the new index or redeploys the previous application build. Rollback does not delete tickets or audit rows created after deployment. A unique index can be dropped without deleting those rows. Application rollback after Gate A leaves any newly written `Resolution` audit rows in place; those rows are history and are not deleted to restore the previous build.

## 17. Items this design deliberately leaves open

| Item | Waiting on |
|---|---|
| Public number format and the 9999 ceiling as a business rule | D-4. The technical ceiling is the existing `varchar(10)` column |
| Shared prefixes as a catalogue decision | D-9, after F-01's shared sequence is in place if the same prefix remains |
| Who may resolve, reopen, or close | D-2, D-6, D-12 |
| SLA persistence, pause, and breach mail | F-09 read, then D-7 |
| Automatic pickup and support groups | F-07 duplicate read and unique key, then D-14 |
| Absolute mail origin | D-8 |
| Minimum resolution length | D-17. The 1000-character maximum in this design is the audit column |
| Cross-company queue visibility and null `CompanyId` handling | D-1 and D-15 |
| Anti-forgery tokens | A separate security change. Absence is recorded in R-01 §5. ViewState MAC was not read |
| `UserMaster.Branch` foreign-key attribute | The F-09 read |

## 18. Exit state

Implementation remains unauthorised. Source locations for F-01 through F-10 are in R-01. This design is the proposed fix for FreeBuff to review. Database preconditions are specified and unread. D-1 through D-18 are pending. Test cases are specified and not run. The IT owner has not authorised a scope.

**Overall status: NOT READY FOR IMPLEMENTATION**
