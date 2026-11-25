# ITSM FreeBuff independent remediation review

**Document:** ITSM_FREEBUFF_REMEDIATION_REVIEW.md
**Role:** Independent technical review (FreeBuff). Not an approval, not a design, not an implementation plan.
**Date:** 24 November 2026
**Inputs reviewed:**

1. `ITSM_SOURCE_TO_FINDING_TRACEABILITY.md` (R-01)
2. `ITSM_TECHNICAL_REMEDIATION_DESIGN.md` (R-02)

**Change status:** No application file, database object, migration, configuration file, or
deployment artefact was created or modified by this review. No SQL was executed. No UAT or
LIVE connection was opened. `AnmolDristi/Web.config` was not read. The only file added is
this review.

---

## 1. Overall verdict

**Accepted with Corrections.**

R-02 is technically sound in direction and unusually honest about what it does not know. Every
root cause it inherits from R-01 that I re-checked is present in the source. The proposed
changes stay inside the existing modular monolith and EF6 architecture: rules in
`AnmolDristi/ITSM/BLL`, persistence in `AnmolDristi/ITSM/DAL`, no inline SQL added to pages,
no new workflow state, no new approval rule, no new business fact persisted.

It is **not** accepted as written. Five defects block implementation authorisation, and none of
them is a matter of taste:

| # | Defect | Finding | Severity |
|---|---|---|---|
| X-1 | The sequence-parse rule is stated as "at least as long as the namespace" (6 characters), which parses the wrong substring for 6–9 character legacy values and can inflate or exhaust a monthly sequence. The source it replaces uses a 10-character test. | F-01 §4.3 step 5 | Blocking |
| X-2 | `sp_getapplock`'s return code cannot be observed through `ExecuteSqlCommand` as described; as written, the stated "timeout throws" behaviour is unimplemented. | F-01 §4.3 step 4 | Blocking |
| X-3 | The internal-note audit limit is arithmetically wrong: `"[Internal] "` is **11** characters, not 10, so the limit is 989 user characters, not 990. | F-05, TC-08 | Blocking |
| X-4 | `Truncate` is retained inside `AddAudit` as a "last-resort guard", so silent truncation remains reachable on `OldValue`, `NewValue`, `ActionType` and `Detail`. The requirement "no silent truncation remains in any audit path" is therefore not met. | F-05 §8 | Blocking |
| X-5 | Gate A ships F-08's fail-closed status lookup while the F-10 status read (names, `IsActive`, `IsTerminal`, duplicates) is not a Gate A precondition. The change can convert a silent partial commit into a hard approval/rejection outage on the day it deploys. | §2, §11, §13 | Blocking |

Secondary corrections (C-01 … C-32 and R-01a–R-01c) are listed in §6. Additional risks the design
does not identify are in §7. Test cases that must be added to TC-01…TC-20 are in §8.
Preconditions are in §9.

R-02's own exit line — **NOT READY FOR IMPLEMENTATION** — is correct, and this review does not
move it. The corrections above are prerequisites for a future authorisation, not authorisation.

---

## 2. Basis and method

Read and inspected in the working tree on 24 November 2026:

- `AnmolDristi/ITSM/BLL/TicketService.cs` (whole file)
- `AnmolDristi/ITSM/BLL/TicketUpdateService.cs` (whole file)
- `AnmolDristi/ITSM/BLL/TicketDetailService.cs` (whole file)
- `AnmolDristi/ITSM/BLL/ItsmTicketAccess.cs` (whole file)
- `AnmolDristi/ITSM/BLL/TicketWorkflow.cs` (whole file)
- `AnmolDristi/ITSM/BLL/DispatchService.cs` (whole file)
- `AnmolDristi/ITSM/BLL/SlaService.cs` (whole file)
- `AnmolDristi/ITSM/BLL/TicketListService.cs` (query section)
- `AnmolDristi/ITSM/UI/TicketDetail.aspx` and `.aspx.cs` (whole files)
- `AnmolDristi/ITSM/UI/TicketList.aspx` (whole file)
- `AnmolDristi/ITSM/UI/CreateTicket.aspx` (validators and MaxLength) and `.aspx.cs` (whole file)
- `AnmolDristi/ITSM/UI/ApprovalConfig.aspx.cs` (message sink)
- `AnmolDristi/ITSM/DAL/Entities/*` for `Ticket`, `TicketAuditLog`, `TicketApproval`,
  `TicketComment`, `TicketStatus`, `RequestType`, `SupportMapping`, `UserMaster`, `Branch`
- `AnmolDristi/ITSM/DAL/Migrations/202608120550515_InitialItsmSchema.cs`
- `AnmolDristi/ITSM/DAL/Migrations/202608201200000_AddTicketAuditLog.cs`
- `AnmolDristi/ITSM/DAL/Migrations/202608210700000_AddTicketCompanyId.cs`
- `AnmolDristi/ITSM/DAL/Migrations/202608220132564_AddBranchLegacyId.cs`
- `AnmolDristi/ITSM/DAL/Migrations/202608240451107_AddApprovalConfiguration.cs`
- `AnmolDristi/ITSM/DAL/Migrations/Configuration.cs`
- Repository-wide searches for `MST_RequestType` / `MST_TicketStatus` in `*.sql`

Not read, and therefore not evidence: `AnmolDristi/Web.config` (security boundary, AGENTS.md
§3.4), any deployed database, `MST_*` row contents, effective permission grants, IIS request
validation mode, ViewState MAC configuration.

Evidence classes used below:

- **Source-verified** — the behaviour is in the cited lines, in this working tree.
- **Environment-unread** — the deployed row, grant, or collation was not queried.
- **Not re-verified** — R-01 records it; I did not re-check it in this pass.

All comments in the source files (for example `Branch.Branch_ID` "Verified unique") are
treated as claims, not evidence.

---

## 3. Finding-by-finding verdict

Summary table; the detail for each finding follows.

| ID | Verdict | Reason in one line |
|---|---|---|
| F-01 | **Accepted with Corrections** | Root cause and lock-plus-index direction correct; parse rule, lock-code shape, retry ownership, and case handling are wrong or missing. |
| F-02 | **Accepted with Corrections** | Encoding at the assignment boundary is right; several render sinks are omitted and the pre-wrap instruction is not implementable as written. |
| F-03 | **Accepted** | Root cause, fix location, limit, and message are correct as specified once two placement details are tightened. |
| F-04 | **Accepted with Corrections** | History capture is adequate within the 1000-character column; it imposes a new product limit and leaves two audit-shape ambiguities. |
| F-05 | **Accepted with Corrections** | Refusal instead of truncation is right; the arithmetic, the residual `Truncate`, and the comment-limit reduction are wrong. |
| F-06 | **Accepted** | Predicates are faithful to the server rules and preserve the intentional IT Head rejection order. DTO shape must be stated. |
| F-07 | **Accepted with Corrections** | Deterministic ordering is correct; the four filtered unique indexes are mutually overlapping as specified. |
| F-08 | **Accepted with Corrections** | Fail-closed branches are correctly located; gate ordering and rollback robustness are defects. |
| F-09 | **Accepted** | Uncertainty preserved, no SLA persistence proposed, no dependency on unread rows. |
| F-10 | **Accepted with Corrections** | Seed-is-not-configuration is correct and I confirmed no ITSM seed exists anywhere in `*.sql`; one read is missing. |

---

### F-01 — Ticket-number allocation

**Verdict: Accepted with Corrections.**

**What R-01 and R-02 assert, and what the source shows.** All of it holds:

- `TicketService.cs` `GenerateTicketNumber` begins at line 359. The prefix is
  `(requestType.Prefix ?? requestType.Code ?? "XX").Trim()` (line 364) and is cut to two
  characters only when longer than two (lines 366–368). It builds `prefix + yy + MM`
  (line 371 area) and returns `numberPrefix + nextSequence.ToString("D4")`.
- The maximum read is `context.Tickets.AsNoTracking().Where(t => t.RequestTypeId == requestType.Id && t.TicketNumber.StartsWith(numberPrefix))` (line 374 onward) — confirmed `AsNoTracking`, no lock hint, filtered by `RequestTypeId` **and** by the displayed prefix.
- The parse is `(n != null && n.Length >= 10 && int.TryParse(n.Substring(n.Length - 4), out seq))` at line 389 — confirmed. A 9-character number contributes 0.
- `itsm.Tickets.TicketNumber` is `c.String(nullable: false, maxLength: 10, unicode: false)` at
  `202608120550515_InitialItsmSchema.cs` line 78. The index list for `itsm.Tickets`
  (lines 117–128) covers `RequestTypeId`, `RequesterCode`, `AssigneeCode`, `PlantId`,
  `BranchId`, `CategoryId`, `SubCategoryId`, `ItemId`, `ImpactId`, `UrgencyId`, `PriorityId`,
  `StatusId`. **No index of any kind on `TicketNumber`.** Confirmed.
- `itsm.MST_RequestType.Prefix` is `c.String(nullable: false, maxLength: 2, fixedLength: true,
  unicode: false)` at line 243 → `char(2)`. Confirmed. Trailing padding is therefore a real
  possibility and `Trim()` can therefore yield one character.
- `CreateTicket` opens `context.Database.BeginTransaction()` with no isolation argument. No
  `sp_getapplock`, `UPDLOCK`, `HOLDLOCK`, or isolation-level statement exists under
  `AnmolDristi/ITSM` in the files I read.

**Challenges to the proposed fix.**

**X-1 (blocking) — the parse rule in §4.3 step 5 is wrong.** The design says: *"Parse a trailing
integer only when the stored value is at least as long as the namespace."* The namespace is six
characters. A six-, seven-, eight- or nine-character stored value that matches the namespace
prefix would then be parsed — and whatever substring rule is implemented (last four characters,
or the four after the namespace) reads characters that are not a sequence. Concretely, a stored
nine-character `CR2608001` passes a "≥ 6" test; taking the last four characters yields `8001`,
which jumps the sequence to 8002 and can exhaust a month's range almost immediately. The rule
must be *exactly* namespace length + 4, i.e. ten characters, and the four characters must be
taken from offset `namespace.Length`, not from the end. This mirrors the source's own
`n.Length >= 10` guard, which the design is replacing while weakening it. §4.4's claim that "a
nine-character number has a different string prefix" is only true when the historical prefix was
one character; a **two**-character prefix with a short trailing sequence (a value written by some
other path, a manual correction, or a truncated insert) produces a nine-character value that
*shares* the six-character namespace and is exactly the case the weak test mishandles.

**X-2 (blocking) — `ExecuteSqlCommand` cannot see `sp_getapplock`'s return code.** §4.3 step 4 says
the lock "is executed with `ExecuteSqlCommand` on the create context" and that "a timeout or a
negative return code throws `InvalidOperationException`". EF6's `ExecuteSqlCommand` returns the
number of rows affected by the batch, not a procedure return code. Without an explicit
`DECLARE @rc int; EXEC @rc = sys.sp_getapplock …; IF @rc < 0 THROW 50000, …` (or a `SELECT @rc`
read back through a scalar query), a timeout is silently ignored and the allocation proceeds
unserialised — which is the exact bug F-01 exists to remove. The design must specify the batch
text, the timeout value in milliseconds (it says only "a short timeout"), and the specific return
codes treated as failure (`-1` timeout, `-2` cancelled, `-3` deadlock victim, `-999` parameter or
other call error).

**C-01 — lock-resource case and collation.** `sp_getapplock` resource names are compared by the
database engine, and the namespace used for the maximum read is compared by the column's
collation. If `itsm.Tickets.TicketNumber` is case-insensitive while the lock resource comparison
is not (or vice versa), a stored `cr` prefix and a stored `CR` prefix take different locks, both
read the same maximum, and both insert. The design must normalise the prefix once — one
invariant case, whitespace-trimmed, exactly two characters — and use that single normalised value
for the six-character namespace, the lock resource, and the parse test. It must also state the
`TicketNumber` collation assumption and make the precondition read check for case-variant
duplicates (`GROUP BY UPPER(TicketNumber)`), not only exact duplicates.

**C-02 — retry ownership is unspecified and cannot be implemented where the design implies.**
§4.3 says "the create transaction is rolled back, the context is disposed, and the whole
`CreateTicket` is retried on a new context". `CreateTicket` creates its own `ItsmDbContext`
inside `using` and takes no context parameter; there is no seam for a caller to retry "the whole
`CreateTicket` on a new context" without either (a) a new public wrapper (`CreateTicketWithRetry`)
that calls a private `CreateTicketOnce`, or (b) a retry loop in
`CreateTicket.aspx.cs`, which would put a persistence concern in the page and duplicate it in any
future caller. The design must name the seam, the exception predicate (EF6 surfaces a unique
index violation as `DbUpdateException` whose innermost `SqlException.Number` is 2601 or 2627 —
and 2627 may also arise from other constraints, so the index name must be checked), and the fact
that a rolled-back insert consumes identity values, so ticket ids will have gaps after a retry
and a retry must not be treated as idempotent.

**C-03 — the retry's side effects must be enumerated.** Each attempt re-reads requester, branch,
plant, priority, request type, SLA timing, and dispatch mapping, and recomputes `createdOn`.
A retry therefore can produce a *different* branch-derived SLA window and a *different*
assignee than the first attempt. That is acceptable — the first attempt persisted nothing — but
the design must say so, and the audit's `ActionedOn`/`CreatedOn` must come from the successful
attempt only.

**C-04 — the exhaustion guard should test the formatted string, not the integer.** §4.3 step 6
refuses a sequence above 9999. Keep that, and additionally refuse when
`(namespace + nextSequence.ToString("D4")).Length != 10`, so a future change to the namespace
width cannot silently produce an 11-character value that the column truncates or rejects.

**C-05 — prefix refusal is a capability removal and must be gated on data.** §4.3 step 2 refuses
creation for any request type whose trimmed prefix is not exactly two characters. Because
`Prefix` is `char(2)`, a stored value of `' C'` trims to one character and blocks that request
type entirely. The design records the refusal but places the prefix precondition read (§4.6,
row 3) in the Gate B read set while the refusal itself lives in the Gate A/B allocation change.
The read that identifies affected request types must run **before** the change is enabled, and
affected types must be corrected or explicitly accepted as blocked, with the owner's awareness
recorded. Workarounds inside the method (padding, substring, falling back to `Code`) are
correctly forbidden — keep that.

**C-06 — index feasibility is asserted, not specified.** The column is `NOT NULL`, so the
one-NULL-per-unique-index problem does not arise; the design should state that explicitly rather
than leave a reviewer to check. It should also specify: the index is nonclustered and unique, it
is *not* filtered, creation is a schema change that blocks writes on `itsm.Tickets` unless
`ONLINE = ON` is available on the target edition, and the rollback (`DROP INDEX`) preserves rows
— §4.5 already says the last part. The precondition read must also report the count of rows
whose `TicketNumber` has trailing whitespace or non-printing characters, which a unique index
would treat as duplicates of the visible value.

**C-07 — `sp_getapplock` permission.** The design must add a precondition: the application's SQL
login (the `DbConn` identity used by `ItsmDbContext`) can execute `sys.sp_getapplock`, and no
`EXECUTE AS` context in the connection path restricts it. This is a grant check, not a code
change, and it belongs in this review's §9 precondition list, not in an implementation step.

**C-08 — lock scope is per database.** Because the lock lives in the application database, two
application instances pointed at the same database are serialised (correct) and two instances
pointed at different databases for the same numbering namespace are not (expected). State the
assumption so nobody later believes the lock is cross-environment.

Everything else in R-02 §4.1–§4.6 is correct: the format is preserved, historical numbers are
not rewritten, the `RequestTypeId` filter removal is the right correction for shared prefixes,
and D-4/D-9 are properly left pending. §4.6's three reads are the right reads.

---

### F-02 — Output encoding

**Verdict: Accepted with Corrections.**

**What the source shows.** `TicketDetail.aspx.cs` `LoadTicket` assigns `lblTitle.Text = dto.Title`
(line 234), `lblDescription.Text` (line 244, with the `"(No description provided)"` placeholder),
`lblResolution.Text` (line 249), `lblStatusBadge.Text` (line 251), the concatenations for
category/impact/urgency, and `lblCsatFeedback.Text`. `System.Web.UI.WebControls.Label` writes
`Text` unencoded. The same page encodes comment text in `rptComments` via
`<%# System.Web.HttpUtility.HtmlEncode(Convert.ToString(Eval("CommentText"))) %>` and approval
comments via the same call inside an inline string expression — both confirmed present.

Unencoded rendered values I confirmed in the markup:

- `TicketDetail.aspx` `rptComments`: `<strong><%# Eval("CreatedByName") %></strong>` — unencoded.
- `TicketDetail.aspx` `rptApprovals`: `<%# Eval("ApprovalLevel") %>`, `<%# Eval("ApproverName") %>`,
  and `<%# Eval("ApprovalStatus") %>` — all unencoded, and `ApprovalStatus` is *also* used inside
  an attribute: `class="label label-<%# Eval("ApprovalStatus").ToString() == "Approved" ? "success" : … %>"`.
- `TicketList.aspx`: `<%# Eval("TicketNumber") %>` inside the anchor, and
  `Text='<%# Eval("PriorityName") %>'` / `Text='<%# Eval("StatusName") %>'` on `Label` controls.
- `TicketList.aspx` `Title`, `RequestTypeName`, `RequesterName`, `AssigneeName`, `CategoryName`,
  `BranchName` are `BoundField` and therefore framework-encoded. Confirmed; leave them alone.

**Challenges to the proposed fix.**

**C-09 — a render sink is missing from the table: `lblMessage`.** `TicketDetail.aspx.cs` line 393
(`ShowMessage` → `lblMessage.Text = message`), `TicketList.aspx.cs` line 336,
`CreateTicket.aspx.cs` line 454, and `ApprovalConfig.aspx.cs` line 155 all write a `Label` from
BLL/exception text. Today those strings are fixed text plus stored status names, so the exposure
is low — but R-02 §5's own rule is "encode at the output boundary", and this is an unencoded boundary
fed partly by database rows (`Status.Name`). Encode it, or route all four through one
`ShowMessage` helper that encodes once.

**C-10 — the `label-<%# %>` class binding is an attribute-context sink, not a text sink.** §5 says
the CSS class "is restricted to the existing lowercase-and-hyphen transform and is not a second
HTML sink". That sentence is wrong about the transform (`ToLower().Replace(" ", "-")` removes
nothing that matters inside an attribute) and it also mislocates the control: the
`lblStatusBadge.CssClass` assignment is framework-encoded on render (the `Label` renderer encodes
attribute values), but the `class="label label-<%# Eval("ApprovalStatus") %>"` expression in
`rptApprovals` writes the stored value directly into the markup stream with no renderer in
between. The design must (a) correct the sentence, and (b) fix the repeater case with a whitelist
map (`Approved` → `success`, `Rejected` → `danger`, otherwise `warning`) rather than
`HtmlEncode`, because the class must also remain a valid CSS token. The same whitelist should
replace `ToLower().Replace(" ", "-")` for `lblStatusBadge.CssClass`, so an unexpected status name
cannot produce a broken class name either.

**C-11 — the `pre-wrap` instruction does not identify its target.** §5 says "Set
`white-space: pre-wrap` on the existing description container". The description's container in
`TicketDetail.aspx` is an anonymous `<div style="background: #f8f9fa; padding: 12px; …">` with no
ID, and the resolution's is a second anonymous div inside `pnlResolution`. Nothing in the markup
can currently be addressed by that sentence. The design must name the change: give each container
a CSS class (or an `ID`) and add the rule to the page's `<style>` block. It must also state that
`pre-wrap` belongs on the *container*, not on the `Label`, because `Label` renders a `<span>` and
inherits the property either way, and that the encoded text's newlines survive only because
`HtmlEncode` preserves `\r\n`.

**C-12 — `ddlTargetStatus`, `ddlReassignTo`, `ddlOverridePriority` should be recorded, not assumed.**
`ListItem` text assigned from master data (`status.Name`, `EmployeeName`, `Priority.Name`) is
framework-encoded on render, so these are not defects. But §5's claim that the design covers
"every untrusted field rendered by ITSM pages" should be true by enumeration, not by omission.
Add one line recording that these are framework-encoded and require no change.

**C-13 — the design's claim about the creation mail is incomplete in one respect.**
`TicketService.cs` `EnqueueTicketCreatedNotification` HTML-encodes the title with
`System.Net.WebUtility.HtmlEncode` and writes `ticket.TicketNumber` into the subject and body
unencoded. The number is system-generated and after F-01 is two uppercase letters plus six
digits, so this is safe — but the design should record that the mail's safety for the number now
depends on F-01's prefix validation, which it does not say. The `ticketLink` contains an integer
id and a literal `~/`, so no encoding issue exists there; D-8 still owns the origin question.

**C-14 — a clean surface should be recorded as clean.** `CreateTicket.aspx.cs`
`RenderCategoryCards` and `RenderRequestTypeCards` build `HtmlGenericControl` nodes using
`InnerText` for the visible name (framework-encoded) and put the name into `aria-label` through
`Attributes[...]`, which renders through `WriteAttribute` (encoded). `icon.InnerHtml` is a
constant. This is the one ITSM page that renders master data into markup and it is already
correct; the design should say so, so the next reviewer does not have to re-derive it.

---

### F-03 — Rejection reason length

**Verdict: Accepted.**

**What the source shows.** `TicketApproval.Comments` is `[StringLength(500)]` and the initial
migration declares `Comments = c.String(maxLength: 500)` at line 139. `RejectTicket` rejects a
blank reason before opening the context, then assigns `approval.Comments = reason.Trim()` at line
409 with no length check. The only specialised catch is `DbUpdateConcurrencyException`
(lines 444–452 in the traceability's numbering); every other exception is rolled back and
rethrown. `btnReject_Click` catches nothing. `txtRejectionReason` has no `MaxLength`, while
`txtOverrideReason` on the same page declares `MaxLength="500"`. All confirmed.

The design's fix — trim, refuse over 500 with `TicketUpdateResult.Fail`, set `MaxLength="500"` as
a hint only, keep the existing rethrow for unexpected failures, and do not add a page-level
catch-all — is correct and minimal. Two placement details must be tightened.

**C-15 — put the check beside the existing blank check, outside the transaction.** §6 says "after
the existing blank check and before any approval row is modified". Read literally, that permits
the check to sit inside the `using` block after `LoadForApprove`, which means an invalid post
still opens a transaction and performs two or three reads. Put it directly after the blank check
at line ~363, before `using (var context = ...)`. Behaviour is identical; the cost and the
failure surface are smaller.

**C-16 — `ApproveTicket`'s check must be unconditional now, not "if a comment is later supplied".**
§6 defers it. `ApproveTicket` already executes `approval.Comments = comments` with whatever the
caller passes; the page passes `null` today, but the BLL is the contract boundary and the same
500-character column is at risk from any future caller, from a test harness, and from the F-06
work if a comment box is ever added to the approval panel. Enforce the same 500-character check
in `ApproveTicket` in the same change. It is four lines and removes a latent identical defect.

**C-17 — record why the rethrow is acceptable, and what that depends on.** The design declines a
page-level catch-all and relies on every input being bounded. That is only true once F-05's
comment bound and `OverridePriority`'s reason bound (currently client-side only) are in the same
release. The design must state that dependency explicitly; otherwise F-03 can ship alone and leave
`btnOverridePriority_Click` able to produce an unhandled `DbEntityValidationException` on a
501-character reason. I confirmed the page has no catch that would make that graceful.

---

### F-04 — Resolution history

**Verdict: Accepted with Corrections.**

**What the source shows.** `ChangeStatus` refuses a terminal current status before consulting the
map; the map in `TicketWorkflow.cs` allows `Resolved` → `Closed` and `Resolved` → `In Progress`;
`ChangeStatus` overwrites `ResolutionText` and `ResolvedOn` whenever the target name is `Resolved`,
then calls `AddAudit(context, ticket, "StatusChange", oldStatusName, targetStatus.Name, null, actorCode)`
at line 109 — `detail: null`. Moving to `In Progress` does not clear either column. `ResolutionText`
is `c.String()` in the migration (nvarchar(max)); `txtResolution` in the markup has **no**
`MaxLength`. `TicketAuditLog.Detail` is `c.String(maxLength: 1000)` (migration, and
`[StringLength(1000)]` on the entity). All confirmed.

The design's three-branch structure (first resolve, reopen, re-resolve) is a correct way to
capture history inside an existing 1000-character column, and writing the audit row in the same
`SaveChanges` as the status change is the right transaction shape. The corrections are about
scope and about the shape of what is written.

**C-18 (owner decision, not technical) — the 1000-character cap is a new product restriction.**
Today a resolution of any length is accepted and stored on the ticket; the audit simply records
`null`. §7.1 and §7.3 make anything over 1000 characters a hard failure. That is defensible
("refuse rather than store incompletely") but it is a *capability removal on a currently unbounded
field*, and §7.1's own sentence — "It is not a ruling on D-17's minimum length. D-17 stays
pending" — is only half the picture: D-17 is about the minimum, and the design is setting a
maximum. The maximum must be recorded against the decision register (D-17, and D-4 if the resolve
narrative is customer-visible) as an owner-visible consequence, and the UI must say so before the
user types. Do not ship the cap as an unremarked technical detail.

**C-19 — `NewValue` for the `Resolution` row is unspecified.** `AddAudit` sets all five fields.
§7.1 specifies `ActionType`, `Detail`, `ActorCode`, `ActionedOn` and `OldValue`, and says nothing
about `NewValue`. Pick one (`ResolvedOn=<round-trip>` for a first resolve; a superseded marker for
the prior row) and state it, so a later reader can tell a current resolution from a superseded
one without parsing prose in `Detail`.

**C-20 — one timestamp per transition.** `ChangeStatus` calls `DateTime.Now` once for
`ModifiedOn`, once for `ResolvedOn` (and again for `ClosedOn` in the closed branch), and `AddAudit`
calls `DateTime.Now` internally for `ActionedOn`. If `OldValue` embeds `ResolvedOn`, the value
written can differ by milliseconds from the ticket's own `ResolvedOn`. Capture one `var now =
DateTime.Now` at the top of the transition and use it for every field, including a new
`AddAudit` overload that accepts the timestamp. The design should say this.

**C-21 — two audit-shape edge cases are unspecified.**
(a) A ticket with `ResolutionText` set but `ResolvedOn` null, or the reverse, is not described;
§7.3 branches on `ResolutionText` alone. State the behaviour (recommended: treat as "already
resolved", preserve, and record the half-populated pair in `OldValue`).
(b) Resolutions written before this change have `ResolutionText` on the ticket and **no**
`Resolution` audit row, and never will. The design must say that pre-change history is not
reconstructible, that no backfill is proposed, and that reopening such a ticket must not create a
`Resolution` row that pretends to be its history.

**C-22 — §7.2 changes existing audit semantics without saying so.** The reopen branch writes the
fixed detail `Resolution text retained on the ticket.` on what is otherwise a `StatusChange` row
whose `Detail` is `null` today (R-01 §4 table says "StatusChange, Detail null"). Either keep
`null` and record the retention in the new row's `OldValue`, or state explicitly that
`StatusChange` rows now carry a fixed detail sentence and that R-01 §4 must be annotated
accordingly. Documentation drift inside an audit trail is exactly what this programme is trying to
stop.

---

### F-05 — Audit detail length

**Verdict: Accepted with Corrections.**

**What the source shows.** `AddAudit` assigns `ActionType = Truncate(actionType, 50)`,
`OldValue = Truncate(oldValue, 200)`, `NewValue = Truncate(newValue, 200)`,
`Detail = Truncate(detail, 1000)`. `Truncate` returns `Substring(0, maxLength)` with no signal.
`AddComment` stores the full trimmed text on `TicketComments.CommentText` and passes
`(isInternal ? "[Internal] " : "") + commentText.Trim()` as `Detail`. `txtComment` declares
`MaxLength="2000"`. `TicketComment.CommentText` has no `StringLength`; the migration declares
`c.String(nullable: false)` → nvarchar(max). `TicketAuditLog.Detail` is `c.String(maxLength: 1000)`.
All confirmed.

The direction — refuse instead of truncate, and require the comment row and the audit row to
agree — is correct. Three defects.

**X-3 (blocking) — the limit arithmetic is wrong.** `"[Internal] "` is eleven characters
(`[`, `I`, `n`, `t`, `e`, `r`, `n`, `a`, `l`, `]`, space). §8 says "ten-character prefix" and
"990 characters of user text". The correct figures are eleven and **989**. TC-08 encodes the wrong
number and would fail against a correct implementation, or — worse — pass against an
implementation that truncates by one character. Correct §8 and TC-08.

**X-4 (blocking) — `Truncate` must go, not stay.** §8 retains `Truncate` in `AddAudit` "only as a
last-resort guard for values this design has already bounded". That leaves four silent truncations
in the audit path:

| Field | Column | Sources that can exceed it |
|---|---|---|
| `ActionType` | 50 | fixed tokens today; any future action name |
| `OldValue` | 200 | status names (≤100), priority names (≤100) — safe today |
| `NewValue` | 200 | status names, `"Approved (partial)"`, rejection status name — safe today |
| `Detail` | 1000 | comment, rejection reason, override reason, resolution, `"[Internal] "` prefix |

The two `200`-character fields are currently safe only because `MST_Priority.Name` and
`MST_TicketStatus.Name` are both declared `maxLength: 100` in the migrations — a fact the design
does not rely on and does not state. The requirement "no silent truncation remains in any audit
path" is not satisfied while `Truncate` sits inside `AddAudit`. Required: delete `Truncate` from
`AddAudit` and make it fail closed — throw or return a failure result when any value exceeds its
column — with each caller bounding its own input. If a reviewer prefers to keep a guard, it must
be a guard that *raises*, not one that cuts. §8's final paragraph ("Silent truncation is not
reintroduced in that change") is therefore currently contradicted by §8 itself.

**C-23 — the comment-limit reduction is a product change and is under-specified in the UI.**
`txtComment` says 2000; §8 says set it to 1000 and enforce 989 for internal notes. Three
consequences the design must state: (a) users lose half the box's stated capacity — an
owner-visible reduction like C-18, adjacent to D-5 and D-17; (b) `MaxLength="1000"` cannot also
enforce 989, so either the client limit must track the checkbox (script) or the message must be
the only guidance for internal notes; (c) a 1000-character public comment is accepted while a
990-character internal note is refused, which reads as arbitrary unless the message says why.
Also note: `AddComment` writes `ticket.ModifiedOn`/`ModifiedByCode` *before* `AddAudit`; the
failure return must precede those assignments, so a refused comment does not touch the ticket row
at all. And existing rows where `TicketComments` and the audit `Detail` disagree (any historical
comment over 1000 characters) are left as they are — say so.

---

### F-06 — Approval panel eligibility

**Verdict: Accepted.**

**What the source shows.** `TicketDetailService.cs` line 176: `dto.CanApprove` is
`context.TicketApprovals.AsNoTracking().Any(a => a.TicketId == ticketId && a.ApproverCode == actorCode && a.ApprovalStatus == "Pending")`
— no status filter, no requester exclusion, no level consideration. `TicketDetail.aspx.cs` line
281: `pnlApprovalActions.Visible = dto.CanApprove`. `ItsmTicketAccess.CanApproveTicket` excludes
the requester and requires a pending row. `ApproveTicket` requires current status name
`Pending Approval`, refuses an `ItHead` row while a `Manager` row is `Pending`
(`StringComparison.OrdinalIgnoreCase` against `"ITHead"`, which matches the stored `"ItHead"`).
`RejectTicket` has no order check. `CreateTicket` inserts both rows as `"Pending"` at creation
when the request-type flags are set. All confirmed.

The design's two predicates are faithful: May-approve mirrors `ApproveTicket`, May-reject
deliberately omits the order check so the intentional IT Head-rejects-before-manager behaviour
survives. Correct, and non-obvious — it is the right call to spell it out.

**C-24 — name the DTO and panel changes.** `TicketDetailDto` carries only `CanApprove`. Adding a
distinct reject flag and a process flag means new members, and §9 must state them and their
binding points: `pnlApprovalActions` (line 281), `btnApprove`, `txtRejectionReason`, `btnReject`,
`pnlWorkActions` (line 282), `pnlActions` (line 283), `chkInternal`. As written, the design cannot
be implemented without someone choosing names and deciding what `pnlActions` becomes when neither
flag is true.

Also required (see §6, C-24): the predicate must be **one** helper used by both the page
and `ApproveTicket`/`RejectTicket`. The design says the service "keeps their checks", which today
are written inline in two methods that differ from each other. If the page gets a new predicate and
the service keeps its inline copies, F-06's class of bug — page advertisement drifting from server
authority — recurs at the next change. This is a structural requirement, not a nicety.

Verified as correct in the design: the requester-who-is-IT-Head sees nothing and the service still
refuses a direct post (D-10 stays pending); a pending row on a ticket that has left `Pending
Approval` shows nothing and the service refuses; `pnlWorkActions` gating on `CanProcessTicket`
hides controls the server already refuses without changing who may process (D-2 stays pending).

---

### F-07 — Dispatch determinism

**Verdict: Accepted with Corrections.**

**What the source shows.** `DispatchService.cs`: `FindMapping` ends `.Select(m => m.SupportAgentCode).FirstOrDefault()` at line 90; `FindCategoryMapping` at line 108; `FindBranchDefault` at line 123. No `OrderBy` anywhere in the file. The initial migration creates separate non-unique indexes on `BranchId`, `CategoryId`, `SubCategoryId`, `ItemId`, `SupportAgentCode` for `itsm.MST_SupportMapping` (lines 10–35). No later migration adds a natural key. Confirmed. `SupportMapping.SupportAgentCode` is `nullable: false, maxLength: 50` — NOT NULL, so a mapping row cannot have a null agent, but an empty string is possible. Automatic pickup does not exist: `DispatchService.ResolveAssignee` is called only from `TicketService.CreateTicket` (line 153), and `ItsmTicketAccess` records "SupportMapping is create-time dispatch, not a live ACL". Correct that D-14 is untouched.

**C-25 — `OrderBy(m => m.Id)` is right; specify the exact query shape and the empty-string case.**
Write it as `OrderBy(m => m.Id).Select(m => m.SupportAgentCode).FirstOrDefault()` so the sort key
is the identity and the projection is the agent. More importantly: an active mapping row whose
`SupportAgentCode` is `''` (empty) currently *wins* the cascade and assigns an empty assignee,
because `FirstOrDefault` returns `''`, which is not `null`, so `ResolveAssignee` returns `''` and
the branch default is never consulted. `Ticket.AssigneeCode` accepts `''` (max 50, nullable). The
design should specify the intended behaviour (recommended: treat empty/whitespace as no match and
continue the cascade) and add the read for such rows to the precondition set, rather than leaving
the behaviour whichever way the `FirstOrDefault` lands.

**C-26 (blocking for Gate B) — the four filtered unique indexes overlap as specified.** §10's
table describes four keys but not four mutually exclusive filters. Two concrete problems follow.
First, a *category*-level row (`SubCategoryId NULL`, `ItemId NULL`) satisfies an item-level filter
expressed only as `IsBranchDefault = 0` — so the item index would also constrain category rows,
which is not what the table says. Second, SQL Server needs distinct, non-conflicting filter
predicates to create four indexes over the same columns; overlapping predicates on the same key
values make the second `CREATE INDEX` redundant or invalid rather than complementary. Specify
exact predicates that partition the space:

| Step | Filter predicate | Key |
|---|---|---|
| Item | `IsActive = 1 AND IsBranchDefault = 0 AND ItemId IS NOT NULL` | `(BranchId, CategoryId, SubCategoryId, ItemId)` |
| Subcategory | `IsActive = 1 AND IsBranchDefault = 0 AND ItemId IS NULL AND SubCategoryId IS NOT NULL` | `(BranchId, CategoryId, SubCategoryId)` |
| Category | `IsActive = 1 AND IsBranchDefault = 0 AND SubCategoryId IS NULL AND CategoryId IS NOT NULL` | `(BranchId, CategoryId)` |
| Branch default | `IsActive = 1 AND IsBranchDefault = 1 AND CategoryId IS NULL AND SubCategoryId IS NULL AND ItemId IS NULL` | `(BranchId)` |

Also state the residual: an active row with `IsBranchDefault = 0` and all three taxonomy keys
NULL is matched by **no** cascade step and falls outside every filter, so it can repeat and is not
covered by the index. Either add a fifth filtered index, or add a precondition read for such
orphan rows and record the decision not to constrain them. The design currently implies full
coverage it does not have.

**C-27 — deterministic ordering is per call, not across a mapping change.** `OrderBy(Id)` makes two
identical calls agree; it does not freeze an assignment. A mapping row inserted or deactivated
between two creates legitimately changes the result. Say so, so the acceptance test does not
assert stability across data change.

---

### F-08 — Transaction boundaries

**Verdict: Accepted with Corrections.**

**What the source shows.** `Commit` is `context.SaveChanges(); transaction.Commit();` then
`Ok()`. All six public update methods open one `BeginTransaction`, validate, mutate, `AddAudit`,
and call `Commit`. Validation failures return `Fail` before `Commit`, and the `using` disposal rolls
back. `ApproveTicket` at lines 327–333 updates `inProgressStatus` only when it is non-null and
otherwise leaves the ticket in `Pending Approval`; `RejectTicket` at lines 424–432 assigns the
rejection status only when one exists. `CreateTicket` uses one context and one transaction for the
ticket, `Created` audit, approval rows, and `EnqueueTicketCreatedNotification`, whose
`ExecuteSqlCommand` therefore participates in that transaction. `SlaService.CalculateSla` (line 32)
and `DispatchService.ResolveAssignee` (line 22) each construct `new ItsmDbContext()` and are called
at `TicketService.cs` lines 151 and 153, before the insert. All confirmed. R-01 §4's
`StatusChange`/`Detail null` read is confirmed.

The two fail-closed corrections are correctly located and correctly scoped: return `Fail` before
any approval row is mutated, use only the status names the code already looks up, add no status.

**X-5 (blocking) — Gate A depends on a Gate B/F-10 read.** §2 puts F-08 in Gate A with "no schema
change" and no precondition. But the correction converts today's silent partial commit into a
hard refusal whenever `In Progress` is absent or inactive (approve path) or neither `Rejected` nor
`Cancelled` is present and active (reject path). The rows that decide this are exactly what §13
read #1 describes, and that read has no gate. Ship Gate A into an environment where `In Progress`
is `IsActive = 0` and every final approval stops working, with a message ("the In Progress status
is not configured") that the operator will read as a bug in the release. Gate A must therefore
require read #1 (names, `IsActive`, `IsTerminal`, duplicate names) for the target environment
before the change is enabled. This is a sequencing correction, not a change to the fix.

**C-28 — `transaction.Rollback()` inside the catch can mask the original exception.** Every
method does `catch (Exception) { transaction.Rollback(); throw; }`. If the transaction is already
aborted or completed — a commit that failed, a broken connection, a deadlock victim — `Rollback()`
throws, and the caller sees the rollback error instead of the cause. For F-01's retry predicate
this matters directly: the caller must be able to see the `DbUpdateException` for the unique index.
Specify a guarded rollback (attempt it, swallow only the second failure, rethrow the original), and
state that the `using` block's `Dispose` performs a further rollback that is harmless.

**C-29 — additional F-08 gaps the design does not address.**
(a) `ApproveTicket` selects the approver's pending row with `FirstOrDefault`. There is no unique key
on `(TicketId, ApproverCode, ApprovalLevel)`, so a duplicate pending row for the same approver can
exist; one is approved and the other stays `Pending` forever, which blocks the `stillPending`
completion path indefinitely. Record as a risk and a candidate precondition read.
(b) The F-08 verification uses a test-only trigger on `itsm.TicketAuditLog`. State that the trigger
must be dropped, that it requires `ALTER` on the table, and that it is never a production object —
§11 says the last part only.
(c) A rolled-back insert burns identity values, so ticket ids gain gaps after F-01's retry; audit
ordering by `ActionedOn` still works, but any reviewer comparing "ticket 5001 wrote audit 9001"
will see the gaps. Say it.
(d) `TicketAuditLog.CompanyId` is `ticket.CompanyId.Value`; the `RequireRecordedTenant` guard makes
that safe. State that F-08's corrections must stay *after* that guard, so a future reordering does
not introduce a `NullReferenceException` on historical tickets.

---

### F-09 — SLA branch key

**Verdict: Accepted.**

No code is proposed, and that is right. Confirmed in source: `TicketService.cs` line 151 passes
`requester.BranchId` to `CalculateSla`, line 153 passes `resolvedBranchId` (i.e. `MST_Branch.Id`)
to `ResolveAssignee`, and `SlaService.GetPlantTiming` compares `t.Branch_ID == branchId`. `Ticket`
has no SLA start or due column, and no code writes one. `Branch.Branch_ID` carries a comment
claiming "Verified unique", and `202608220132564_AddBranchLegacyId.cs` is an empty migration — so
that comment is not evidence and there is no unique index behind it. `UserMaster` declares
`[ForeignKey("BranchId")]` against `Branch`, whose key is `Id`, while `CreateTicket` uses the
explicit `Branch_ID` query and never the navigation. `CreateTicket`'s own header comment asserts the
legacy-key story; it is a comment, not a query.

The design keeps the four identifiers distinct (§12's table matches the source), keeps the
session contract, refuses to change the FK attribute until the read exists, and explicitly says
that a D-7 ruling alone does not authorise persisting a due date. That is correct.

Two additions the design should make: (1) the F-09 read must also establish whether `Branch_ID` is
in fact unique before anyone relies on `FirstOrDefault(b => b.Branch_ID == requester.BranchId)` —
duplicate `Branch_ID` rows would make branch resolution non-deterministic too, the same class of
defect as F-07; (2) the FK-attribute risk should be stated as "if any EF query navigates
`UserMaster.Branch`, it joins on the wrong key today" — I found no such navigation in the files I
read, so this is a latent risk, not a present bug, and the design should say which.

---

### F-10 — Deployed configuration

**Verdict: Accepted with Corrections.**

**What the source shows.** `Migrations/Configuration.cs` seed runs only inside
`if (!context.TicketStatuses.Any())` and does not update an existing table. A repository-wide search
of every `*.sql` file for `MST_RequestType`, `MST_TicketStatus`, and `MST_SupportMapping` returned
**zero** matches, so there is no request-type seed and no status seed anywhere outside the EF
seeder — the design's claim is stronger than it states, and R-01's claim is confirmed. Permission
scripts do exist as files (`AnmolDristi/Database/itsm_phase1b_permissions.sql`,
`AnmolDristi/Database/itsm_role_permissions.sql`,
`AnmolDristi/Database/ITSM_TicketView_Permission.sql`,
`Database/Scripts/010_ITSM_Sidebar_Permissions.sql`).

The seven reads in §13 are the right reads, they are labelled not-run, and §13's final paragraph
records that a misconfigured `Resolved.IsTerminal = 1` makes the reopen edges unreachable without
changing the flag. Correct.

**C-30 — one read is missing, and it has a code consequence.**
`ResolveInitialStatusId` (`TicketService.cs`) looks up `IsActive && Name == "Pending Approval"`, and
when that returns nothing it falls back to `any active non-terminal status` ordered by `Id`. In an
environment where `Pending Approval` is absent or inactive, ticket creation therefore silently
lands in `In Progress` (or whatever the lowest-id active non-terminal status is) and bypasses
approval entirely — while the approval rows are still inserted as `Pending`. That is the same
fail-open class of defect F-08 is fixing on the transition path, on the create path, and it is
invisible to §13's read #1 as written. Add to the read set: the resolved initial status for each
active request type (i.e. what `ResolveInitialStatusId` would return), and record the fallback's
current behaviour for each environment. Whether to make it fail closed is a behaviour change that
needs an owner decision — the design must at least *specify* it as a finding rather than leave it
silent.

**C-31 — duplicate status names affect every name-based lookup.** §13 #1 asks about duplicate
names, which is right. Extend the requirement to the code paths: `ChangeStatus`, `ApproveTicket`,
`RejectTicket`, `TicketWorkflow.GetAvailableStatuses` and `ResolveInitialStatusId` all resolve
statuses by `Name` with `FirstOrDefault` and no `OrderBy`. If duplicates exist, the outcome is
arbitrary per call. Either the read must prove name uniqueness, or those lookups must become
deterministic. State which, per environment.

**C-32 — read #3 cannot be reconciled to code as written.** It lists permission names but not the
`BasePage.HasPermission` resolution path or the grant table, so a result cannot be compared to a
source expectation. Add the lookup the permission check performs, so the read has a fixed shape.

Confirmed as correct and worth praising: no deployed configuration is inferred from seed scripts
anywhere in R-02; §4.6 and §13 keep every environment fact as a read; and each read is required to
carry environment name, statement, timestamp and operator.

---

## 4. Business decisions D-1 … D-18

Verified against `ITSM_BUSINESS_DECISION_REGISTER.md`. **No decision is treated as approved.**
R-02's §2 Gate C, §17 table, and §18 exit state consistently say pending, and §1 states it at the
top. The specific checks:

| Decision | Design's treatment | Assessment |
|---|---|---|
| D-1 cross-company visibility | Unchanged; queue search disclosure noted in R-01 §5 and left. | Not decided. Correct. |
| D-2 who may resolve/reassign/close | `CanProcessTicket` unchanged; only panel visibility narrowed. | Not decided. Correct. |
| D-3 who may raise | Untouched. | Not decided. Correct. |
| D-4 public number format | Format preserved; §4.1 and §17 say D-4 owns the format and the 9999 ceiling as a business rule. | Not decided, but see R-01 below — the *namespace ownership* change is observable and should be recorded against D-4/D-9. |
| D-5 one pause status | Untouched; On Hold reason would reuse the refusal rule. | Not decided. Correct. |
| D-6 reopen window | Map unchanged; §7 says who may reopen stays D-2/D-6. | Not decided. Correct. |
| D-7 stored SLA / breach mail | No column, no mail, §12's last line ties a D-7 ruling to the F-09 read. | Not decided. Correct. |
| D-8 mail origin | `~/` literal kept; §17 lists it as waiting on D-8. | Not decided. Correct. |
| D-9 shared prefixes | §4.2 states the shared sequence as a technical consequence and explicitly refuses to rule which types may share. | Borderline — see R-01 below. |
| D-10 who may authorise a change | Requester-IT-Head panel hidden, service refusal kept, D-10 named. | Not decided. Correct. |
| D-11 correct a rejected ticket in place | Untouched. | Not decided. Correct. |
| D-12 age-based close | Untouched; no administrative close added. | Not decided. Correct. |
| D-13 score blocks closure | Untouched; `Closed` sets `ClosedOn` only. | Not decided. Correct. |
| D-14 one employee or a group | Pickup and support groups explicitly excluded; `OrderBy` framed as stability, not as "the approved winner". | Not decided. Correct. |
| D-15 tickets with no company snapshot | Mutations still refuse null `CompanyId`; no backfill. | Not decided. Correct. |
| D-16 department snapshot | Untouched. | Not decided. Correct. |
| D-17 minimum resolution length | Explicitly left to D-17; but see C-18 — the design sets a maximum and does not record it. | Not decided, but incomplete. |
| D-18 duplicate as text | Untouched. | Not decided. Correct. |

**R-01 on the decision boundary.** Three places where the design's technical framing is thinner
than the business effect, and where a reviewer should not let "technical" stand in for "decided":

1. **F-01 §4.2 — numbering namespace ownership.** Removing the `RequestTypeId` filter changes which
   ticket gets which visible number for every pair of request types that share a prefix. §4.2
   handles this honestly ("not a ruling on which request types should share a prefix"), but the
   *consequence* — two types that today number independently will share one monthly sequence and
   one 9999 ceiling after the change — is a catalogue/visibility decision that belongs in D-9 and
   touches D-4. Record it as a pending item the owner must acknowledge, not only as a technical
   ownership rule.
2. **F-04 — the 1000-character maximum (C-18).** D-17 is about the *minimum*; the design introduces
   a *maximum* on a currently unbounded field. That must be registered.
3. **F-05 — the 2000 → 1000 comment reduction (C-23).** Same class: a visible capacity reduction
   presented as a length fix.

Also worth recording: F-06's decision to keep IT Head rejection available before manager approval
is *preserved* behaviour, not a new rule, and the design says so. That is the correct treatment of
an unresolved question — preserve the code's current behaviour and leave the decision open — and it
should be the model for the three items above.

---

## 5. Architecture conformance

Checked every proposed change against "existing modular monolith and EF6" (AGENTS.md §1, §4):

| Proposed change | Conformance |
|---|---|
| `HttpUtility.HtmlEncode` in `TicketDetail.aspx.cs` / `TicketList.aspx` | UI layer, `System.Web` already referenced. Conforms. |
| `pre-wrap` CSS | Markup only. Conforms. |
| Length refusals returning `TicketUpdateResult.Fail` | Existing BLL result type; no exception contract added. Conforms. |
| `AddAudit` fail-closed | Stays in `TicketUpdateService`; no new DAL type. Conforms, provided the signature change is internal. |
| Resolution audit rows | Reuses `TicketAuditLog`; `ActionType` already documents `"Resolution"` in the entity comment. No schema change. Conforms. |
| New approval/process predicates | New members on `TicketDetailDto` and one helper in `AnmolDristi/ITSM/BLL`. Conforms. |
| `OrderBy(m => m.Id)` | LINQ-to-Entities; no `SqlFunctions` or raw SQL. Conforms. |
| Fail-closed status branches | Existing queries. Conforms. |
| `sp_getapplock` via `ExecuteSqlCommand` | **Conforms, with a caveat.** It is the only new raw SQL. It sits on the create context inside the existing transaction, not in a page. AGENTS.md forbids inline SQL in pages, which this respects. However it introduces a SQL Server-specific dependency into a BLL method — acceptable here because the module already depends on SQL Server semantics (rowversion, `ExecuteSqlCommand` in `EnqueueTicketCreatedNotification`) — and the design should record that a counter table was the alternative (it does, §4.3) so the choice is reviewable. |
| Unique indexes via a separate script | Not an EF migration. **Call this out.** Every other ITSM schema object in this repository is created by an EF6 migration; §4.5 and §10 propose standalone `.sql` scripts. That may be deliberate (Gate B is a reviewed SQL change with preconditions and rollback), but it splits schema authority between two mechanisms and will make `Update-Database` and the script diverge. State which mechanism owns future ITSM index changes, and justify the split, before Gate B. |

No proposed change adds a bundler, package manager, project, service, or workflow state. No
proposed change patches `SalesApprovalRequest.Status` or `FinalApprovedAmount` — out of scope and
untouched. No proposed change crosses into the `Database/Stored Procedures/Approval` conflict in
AGENTS.md §5.

---

## 6. Corrections required in the remediation design

Ordered by finding. "Blocking" means R-02 cannot be authorised until it is fixed in the document.

| Ref | Correction | Blocking |
|---|---|---|
| X-1 | F-01 §4.3 step 5: parse only when `value.Length == namespace.Length + 4`, and take the four characters at offset `namespace.Length`. Align with the source's 10-character test; do not weaken it. | Yes |
| X-2 | F-01 §4.3 step 4: specify the exact batch (`DECLARE @rc int; EXEC @rc = sys.sp_getapplock …; IF @rc < 0 THROW …`), the timeout in ms, and the failure return codes. | Yes |
| X-3 | F-05 §8 and TC-08: `"[Internal] "` is 11 characters; the user limit is 989. | Yes |
| X-4 | F-05 §8: delete `Truncate` from `AddAudit` and fail closed; bound each caller. State the 100-character max on `MST_Priority.Name` / `MST_TicketStatus.Name` that makes the 200-character fields safe today. | Yes |
| X-5 | §2/§11/§13: make the F-10 status read (names, `IsActive`, `IsTerminal`, duplicates) a Gate A precondition for the target environment. | Yes |
| C-01 | F-01: normalise one prefix value (case, whitespace, length 2) for the namespace, the lock resource, and the parse; state the `TicketNumber` collation assumption; add a case-variant duplicate check to the precondition read. | Yes |
| C-02 | F-01: name the retry seam (private `CreateTicketOnce` + public wrapper), the exception predicate (inner `SqlException.Number` 2601/2627 **and** the index name), and that retries are not idempotent and consume identity values. | Yes |
| C-26 | F-07 §10: replace the four row descriptions with four mutually exclusive filter predicates and keys; state the uncovered `IsBranchDefault = 0` all-null row. | Yes (Gate B) |
| C-03 | F-01: state that a retry recomputes SLA, assignee and `createdOn`, and that only the successful attempt is audited. | No |
| C-04 | F-01: refuse when the formatted number is not exactly 10 characters, in addition to the 9999 test. | No |
| C-05 | F-01/F-10: run the prefix read **before** the exact-2 requirement is enabled; record owner acceptance for any type it blocks. | Yes (deployment sequencing) |
| C-06 | F-01 §4.5: state `NOT NULL` (no null-duplicate issue), nonclustered unique unfiltered, index-creation write-blocking, and add a whitespace/non-printing-character check to the read. | No |
| C-07 | F-01/F-13: add the `sys.sp_getapplock` execute-permission check for the `DbConn` identity to the preconditions. | No |
| C-08 | F-01: state that the lock is database-scoped. | No |
| C-09 | F-02: add `lblMessage` (all four pages, one shared encode point) to the sink list. | Yes |
| C-10 | F-02: fix the incorrect claim about the CSS-class transform; replace the `label-<%# Eval("ApprovalStatus") %>` class binding with a whitelist map, and whitelist `lblStatusBadge.CssClass`. | Yes |
| C-11 | F-02: name the pre-wrap target (container ID or class) and state where the rule is added. | Yes (not implementable as written) |
| C-12 | F-02: record that `DropDownList` / `ListItem` text from master data is framework-encoded and needs no change. | No |
| C-13 | F-02: note that the mail's safe ticket-number rendering now depends on F-01's prefix validation. | No |
| C-14 | F-02: record `CreateTicket`'s `InnerText` / `Attributes` rendering as verified clean. | No |
| C-15 | F-03 §6: place the length check with the blank check, before the context is opened. | No |
| C-16 | F-03 §6: enforce the 500-character check in `ApproveTicket` unconditionally, not conditionally on future callers. | No |
| C-17 | F-03: state that the rethrow-is-acceptable argument depends on F-05's comment bound and the `OverridePriority` reason bound shipping in the same release. | Yes (gate contents) |
| C-18 | F-04: register the 1000-character resolution maximum as an owner-visible item against D-17 (and D-4 if visible), and surface it in the UI. | Yes |
| C-19 | F-04 §7.1/§7.3: specify `NewValue` for each `Resolution` row so current and superseded rows are distinguishable. | Yes |
| C-20 | F-04: capture one timestamp per transition and pass it to the audit writer. | No |
| C-21 | F-04: specify the half-populated `ResolutionText`/`ResolvedOn` case, and state that pre-change resolutions have no `Resolution` audit row and are not backfilled. | Yes |
| C-22 | F-04 §7.2: reconcile the fixed reopen detail with R-01 §4's "Detail null" table, or keep `null`. | No |
| C-23 | F-05: state the 2000 → 1000 (989 internal) reduction as an owner-visible change, specify the client-limit behaviour per mode, place the failure return before the ticket `ModifiedOn`/`ModifiedByCode` assignment, and record that historical divergent rows are left as-is. | Yes |
| C-24 | F-06 §9: name the new DTO members and every control binding, and require one shared predicate helper used by the page and by `ApproveTicket`/`RejectTicket`. | Yes |
| C-25 | F-07 §10: give the exact `OrderBy(...).Select(...).FirstOrDefault()` shape and specify empty-string `SupportAgentCode` behaviour; add the read. | Yes |
| C-27 | F-07: state that ordering is per call and does not freeze an assignment across data change. | No |
| C-28 | F-08: specify a guarded rollback that rethrows the original exception. | Yes |
| C-29 | F-08: record (a) duplicate pending approval rows and the missing `(TicketId, ApproverCode, ApprovalLevel)` key, (b) trigger privileges and removal, (c) identity gaps after retry, (d) keep the corrections after `RequireRecordedTenant`. | No (a is Yes — add to precondition reads) |
| C-30 | F-10 §13: add the resolved initial status per active request type, and record `ResolveInitialStatusId`'s fail-open fallback as a finding requiring an owner decision. | Yes |
| C-31 | F-10 §13: extend read #1 to prove status-name uniqueness, or make every name-based lookup deterministic. | Yes |
| C-32 | F-10 §13: give read #3 the grant-resolution path so a result can be compared to code. | No |
| R-01a | Record the F-01 namespace-sharing change against D-9/D-4 as an owner acknowledgement. | No |
| R-01b | R-02 §16 and §4.5/§10: decide and justify standalone SQL scripts versus EF migrations for ITSM indexes. | Yes (Gate B) |
| R-01c | R-02 §8: reconcile "no silent truncation remains" with the retained `Truncate` (covered by X-4). | Yes |

---

## 7. Additional risks not identified by the design

| # | Risk | Evidence | Why it matters |
|---|---|---|---|
| AR-1 | `ResolveInitialStatusId` fails open: when `Pending Approval` is missing or inactive it returns any active non-terminal status by lowest id, while the approval rows are still created `Pending`. Approval is silently bypassed at create. | `TicketService.cs` `ResolveInitialStatusId`, fallback query `Where(s => s.IsActive && !s.IsTerminal).OrderBy(s => s.Id).FirstOrDefault()` | Same fail-open class as F-08, on the create path, and invisible to the F-10 read set as written. |
| AR-2 | Status lookups by `Name` use `FirstOrDefault` with no ordering in `ChangeStatus`, `ApproveTicket`, `RejectTicket`, `GetAvailableStatuses`, and `ResolveInitialStatusId`. Duplicate `MST_TicketStatus.Name` rows make the outcome arbitrary. | `TicketUpdateService.cs` (status lookups), `TicketWorkflow.cs`, `TicketService.cs` | F-10 read #1 only *asks* about duplicates; nothing requires determinism if they exist. |
| AR-3 | `SupportMapping` rows with `SupportAgentCode = ''` win the cascade and assign an empty assignee, because `FirstOrDefault` returns `''` rather than `null`. | `DispatchService.cs` lines 90/108/123; `SupportMapping.SupportAgentCode` is `NOT NULL` but not `NOT ''` | Silent unassigned ticket with a branch default available. |
| AR-4 | Orphan `MST_SupportMapping` rows (`IsBranchDefault = 0`, all taxonomy keys NULL) are matched by no cascade step and covered by none of the proposed indexes, so they may repeat. | `DispatchService.cs` filters vs §10's proposed keys | The design implies unique coverage it does not provide. |
| AR-5 | Duplicate pending approval rows for one approver on one ticket are possible and strand `stillPending` at `true`, so the ticket never leaves `Pending Approval`. | `TicketUpdateService.ApproveTicket`, `stillPending` query excludes only the current `Id`; no unique key exists | A ticket can become permanently unapprovable with no error. |
| AR-6 | `transaction.Rollback()` inside `catch (Exception)` can throw and replace the original exception. | All seven transaction blocks | F-01's retry predicate and any diagnosis both depend on seeing the real `DbUpdateException`/`SqlException`. |
| AR-7 | `lblMessage` is an unencoded render sink on four ITSM pages. | `TicketDetail.aspx.cs` line 393, `TicketList.aspx.cs` line 336, `CreateTicket.aspx.cs` line 454, `ApprovalConfig.aspx.cs` line 155 | Low today (fixed text plus status names) but it is the boundary the design says it is encoding. |
| AR-8 | `AddComment` writes `ticket.ModifiedOn`/`ModifiedByCode` before `AddAudit`, so any failure after that point depends on rollback to avoid a phantom modification. | `TicketUpdateService.AddComment` | The design's refusal must precede those assignments, not merely precede the insert. |
| AR-9 | No anti-forgery token found under `AnmolDristi/ITSM` (R-01 §5, not re-verified here); the design defers it to a separate security change. | R-01 §5 | All F-03/F-04/F-05/F-06 work is a state-changing POST surface; deferring the token is a decision, and it should be recorded with an owner and a date rather than left as an open technical note. |
| AR-10 | `MST_Branch.Branch_ID` has no unique index (`AddBranchLegacyId` is an empty migration) while branch resolution uses `FirstOrDefault`. | `TicketService.cs` branch resolution; `AddBranchLegacyId.cs` | If `Branch_ID` is not unique, branch resolution is non-deterministic — the F-07 defect in a second table. |
| AR-11 | `UserMaster`'s `[ForeignKey("BranchId")]` joins on `Branch.Id` while the column stores `Branch_ID`. Any future navigation of `UserMaster.Branch` silently returns the wrong branch. | `UserMaster.cs`, `Branch.cs`, `CreateTicket` uses the explicit query | Latent; the design defers the attribute change to an F-09 read, correctly, but should state the grep requirement. |
| AR-12 | F-01's exact-two-character rule blocks any request type whose stored `char(2)` prefix trims to one character (e.g. `' C'`), with no data correction in the same change. | `GenerateTicketNumber` line 364; `Prefix char(2)` at migration line 243; §4.3 step 2 | Creation stops for that type on the day the change deploys. Sequencing, not code. |
| AR-13 | The proposed unique index changes create behaviour on duplicates from "two tickets share a number" to "the second create fails". Without the retry seam (C-02) the operator sees an unhandled exception, not a message. | §4.3, §4.5; `CreateTicket.aspx.cs` catches `InvalidOperationException`/`ArgumentException`/`Exception` and shows `diag.GetUserFacingMessage()` for the last | Acceptable, but only if C-02 makes the failure a controlled `InvalidOperationException`. |
| AR-14 | Two schema authorities: EF6 migrations create every existing ITSM object; Gate B creates indexes as standalone scripts. | `Migrations/`, §4.5, §10 | Divergence between `Update-Database` and the scripts is a future incident. |
| AR-15 | Test-only trigger verification requires `ALTER` rights on `itsm.TicketAuditLog` and a disposable database; neither is named as a precondition. | §11 verification paragraph | A verification step that cannot be run is not a verification step. |
| AR-16 | `Description` participates in queue search (`TicketListService`, `ToLower().Contains` on `Title`, `TicketNumber`, `Description`) while the grid does not show it. | `TicketListService.cs` search block; R-01 §5 | A user can infer text they cannot see, across companies, which is a D-1-adjacent disclosure. R-01 records it; R-02 should note it as a residual it deliberately leaves to D-1. |

---

## 8. Test cases that must be added

TC-01 … TC-20 in R-02 are adequate in intent but incomplete in coverage and, for TC-08,
incorrect. Add the following. All remain **Not run** by this review.

| Case | Finding | Requirement |
|---|---|---|
| TC-21 | F-01 | A request type whose stored prefix trims to one character is refused with the controlled message. No ticket row, no audit row, no approval row, and no `NotificationQueue` row is persisted. |
| TC-22 | F-01 | A legacy nine-character number sharing the six-character namespace does not affect the next allocation: the next create emits a ten-character number with the sequence derived only from ten-character values. (Guards X-1.) |
| TC-23 | F-01 | Lock timeout: a second session holds the app lock; the create fails with the controlled message, persists nothing, and does not hang beyond the configured timeout. (Guards X-2.) |
| TC-24 | F-01 | Prefixes `CR` and `cr` in a case-insensitive database produce distinct numbers with no unique-index violation surfacing to the page. (Guards C-01.) |
| TC-25 | F-01 | With a deliberate duplicate present on a restored copy, index creation is refused and the precondition read reports the duplicate group. With duplicates removed, creation succeeds and a duplicate insert is rejected. |
| TC-26 | F-01 | A unique-index violation during create produces the controlled error after the bounded retry, and leaves no partial state — ticket, audit, approvals, and notification all absent. |
| TC-27 | F-02 | `lblMessage` renders a message containing `<`, `>` and `"` as text on all four ITSM pages. |
| TC-28 | F-02 | An approval status value containing `"` and `<` renders as text in the approval history and does not break out of the `class="label label-…"` attribute. |
| TC-29 | F-02 | A description containing `\r\n` and `<script>` renders as literal text with line breaks preserved, and the pre-wrap rule is demonstrably applied to the named container. |
| TC-30 | F-03 | `ApproveTicket` invoked directly with a 501-character comment returns `Fail`; no approval row changes; no audit row; ticket unchanged. |
| TC-31 | F-03 | A 501-character override reason is refused by the server and no priority audit row is written, tested by direct service call (not through the UI). |
| TC-32 | F-04 | A 1001-character resolution is refused; status, `ResolutionText`, `ResolvedOn` unchanged; no `Resolution` and no `StatusChange` audit row. |
| TC-33 | F-04 | Resolve, reopen, resolve twice. Exactly the expected number of `Resolution` rows exist, ordered, with `NewValue` distinguishing current from superseded. |
| TC-34 | F-04 | A ticket with `ResolutionText` set and `ResolvedOn` null follows the specified behaviour and writes no misleading history. |
| TC-35 | F-04 | A ticket resolved before the change (text present, no `Resolution` row) reopens without gaining a `Resolution` row that implies captured history. |
| TC-36 | F-05 | 989-character internal note and 990-character internal note: the first succeeds with a comment row and an audit `Detail` of exactly 1000 characters; the second is refused with neither row committed and no ticket `ModifiedOn` change. |
| TC-37 | F-05 | A status name and a priority name at their 100-character maximum record untruncated in `OldValue`/`NewValue`. (Guards X-4.) |
| TC-38 | F-06 | A requester who is also the configured IT Head sees no Approve and no Reject; a direct approve post returns the separation-of-duties failure; D-10 is recorded as pending, not resolved. |
| TC-39 | F-06 | The IT Head rejects while the Manager row is `Pending`: the Manager row becomes `Rejected` with the fixed comment, the ticket reaches the rejection status, exactly one `Rejection` audit row exists, and the outcome matches the pre-change behaviour. |
| TC-40 | F-06 | A viewer who is not requester, assignee, or pending approver sees neither the approval panel nor the work-action panel; direct posts to both are refused. |
| TC-41 | F-07 | An active mapping row with `SupportAgentCode = ''` behaves as specified (falls through to the next cascade step) rather than assigning an empty agent. |
| TC-42 | F-07 | Duplicate active rows at each of the four specificities resolve to the lowest `Id` on repeated calls, and the Gate B index rejects the second insert while permitting an inactive duplicate. |
| TC-43 | F-08 | With `In Progress` set `IsActive = 0`, approving the last pending row returns `Fail`, leaves the approval row `Pending`, and leaves the ticket in `Pending Approval`. |
| TC-44 | F-08 | With `Rejected` and `Cancelled` both inactive, rejection returns `Fail` and changes no approval row. |
| TC-45 | F-08 | Two sessions approve/reject the same ticket concurrently: one succeeds, the other receives the concurrency message, and no orphan audit row exists. |
| TC-46 | F-08 | A forced failure on the notification insert leaves no ticket, no audit, and no approval rows. |
| TC-47 | F-08 | A forced audit-insert failure leaves the ticket status unchanged; a forced ticket-update failure leaves no audit row. (R-02's TC-13, retained and extended to the create path.) |
| TC-48 | F-10 | With `Pending Approval` absent, create either refuses with a controlled message or — if the owner keeps the fallback — the resulting status is recorded as a known defect, not accepted silently. |
| TC-49 | F-10 | Duplicate `MST_TicketStatus.Name` rows are either absent (read proves it) or every name-based lookup behaves deterministically. |
| TC-50 | Regression | A ticket created before the change with a nine-character number is still openable, listable, searchable, and auditable after the change, and its number is not rewritten. |

TC-08 in R-02 must be corrected from "990 when marked internal" to **989**, and its "one more
character is refused" case must be 990 for internal and 1001 for public.

---

## 9. Database and environment preconditions

None of these was run. Each requires a named environment, an authorisation, and a recorded result.

**Gate A (application only)**

1. `itsm.MST_TicketStatus`: `Name`, `IsTerminal`, `IsActive`, duplicate names, and the status `ResolveInitialStatusId` would return for each active request type. (X-5, C-30, C-31.)
2. An explicit statement that `Expanded`/`In Progress`/`Rejected`/`Cancelled`/`Pending Approval`/`Resolved`/`Closed` exist and are active in the target environment, or acceptance that the fail-closed branches will refuse.
3. Confirmation that no `SupportMapping` row has a blank `SupportAgentCode` (C-25) or is an orphan (`IsBranchDefault = 0` with all taxonomy keys NULL) (AR-4).
4. Confirmation that no duplicate pending approval rows exist for a `(TicketId, ApproverCode)` pair (AR-5).

**Gate B (schema)**

5. `itsm.Tickets.TicketNumber` duplicate groups — exact, case-variant, and whitespace/non-printing variants. Must be zero groups, or a separate data authorisation.
6. All `TicketNumber` values whose length is not 10, recorded (not rewritten).
7. `itsm.MST_RequestType` trimmed prefix lengths for every row, with `Code`, `IsActive`, `RequiresManagerApproval`, `RequiresItHeadApproval`. Any type that would be blocked by the exact-2 rule must be corrected or explicitly accepted (C-05).
8. Active duplicate counts for `itsm.MST_SupportMapping` at each of the four specificities, plus the orphan and blank-agent counts (C-26, AR-4).
9. Effective `EXECUTE` permission on `sys.sp_getapplock` for the `DbConn` identity (C-07).
10. The collation of `itsm.Tickets.TicketNumber` and of the database (C-01).
11. Confirmation that `itsm.Tickets` can take a blocking index build in the target maintenance window, or that `ONLINE = ON` is available (C-06).

**F-09 read (report only, no code)**

12. `VMS_PlantTimings.Branch_ID` compared against `MST_UserMaster.BranchId`, `MST_Branch.Branch_ID`, and `MST_Branch.Id` for more than one company and branch.
13. Duplicate `MST_Branch.Branch_ID` count (AR-10).

**F-10 read (report only)**

14. Effective grants for `ITSM.Queue.View`, `ITSM.Ticket.View`, `ITSM.Approval.View`, `ITSM.ApprovalConfig.Manage`, `UI.ITSM_CreateTicket.View`, with the resolution path the code uses (C-32).
15. `itsm.Tickets.CompanyId` null count and non-matching count.
16. `itsm.MST_ApprovalConfiguration` active rows — exactly one, per the entity's filtered unique index.

**Verification environment**

17. A disposable database for the concurrency and fault-injection cases (TC-23, TC-25, TC-26, TC-45, TC-46, TC-47), with `ALTER` rights for the temporary trigger and a documented removal step. Neither UAT nor LIVE is authorised for these by any document in this review.

---

## 10. Explicit confirmation of change status

- **No application file was modified.** `AnmolDristi/ITSM` and every other application path are as they were read.
- **No database object, migration, or SQL script was created or altered.** No `CREATE`, `ALTER`, `DROP`, or `INSERT` was executed.
- **No SQL was executed at all, and no UAT or LIVE connection was opened.**
- **No configuration or deployment file was modified.** `AnmolDristi/Web.config` was neither read nor written.
- **The two input documents were not modified.** `ITSM_SOURCE_TO_FINDING_TRACEABILITY.md` and `ITSM_TECHNICAL_REMEDIATION_DESIGN.md` are unchanged.
- **Working tree.** `git status --porcelain` was taken at the start of this review and again after the review file was written. The pre-existing modification `M AnmolDristi/Web.config` and the pre-existing untracked ITSM documents and `Database/Deployments/*` directories are present before and after; none of them was touched. The only addition is this file, `ITSM_FREEBUFF_REMEDIATION_REVIEW.md`. `HEAD` is `f1f4bdb`.
- **No decision was closed.** D-1 through D-18 remain pending. This review adds three items (C-18, C-23, R-01a) that should be registered as owner-visible, and does not decide any of them.
- **No implementation, migration, SQL execution, or deployment is authorised by this review.** R-02's exit state — **NOT READY FOR IMPLEMENTATION** — stands. The verdict "Accepted with Corrections" means the *direction* is accepted for a corrected revision, not that any change may be built.

---

## 11. What this review did not do

- It did not read `AnmolDristi/Web.config`, so request-validation mode, `customErrors`, and the
  connection target remain unknown.
- It did not query any database, so every "environment-unread" item in R-01 and R-02 remains
  unread.
- It did not re-verify R-01 §5's anti-forgery-token search, the `ApprovalConfig.aspx.cs` session
  gate, or `login.aspx.cs` line 217's `BRANCH` assignment.
- It did not build the solution. No compile was attempted, because no source file was changed.
- It did not execute any test, including the ones it lists.
- It did not review the Sales TA/DA, HRMS import, or VMS code paths, which are outside this
  scope and outside the AGENTS.md §5/§6 conflict area.
