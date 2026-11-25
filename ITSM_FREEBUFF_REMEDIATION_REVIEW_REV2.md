# ITSM remediation design, revision 2 — second independent review

**Document:** ITSM_FREEBUFF_REMEDIATION_REVIEW_REV2.md
**Reviewed document:** `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` (Revision 2, 24 November 2026)
**Review date:** 24 November 2026
**Review type:** Read-only independent technical review. Not an approval. Not an implementation authorisation.
**Working tree inspected:** `HEAD` `f1f4bdb`. Line numbers below are locations in that working tree. They are not a certified baseline and not a deployed-database result.

---

## 1. Overall verdict

**ACCEPTED WITH CORRECTIONS.**

Revision 2 has correctly resolved the direction of all five blocking defects X-1 through X-5, and it has correctly absorbed the earlier review's structural findings: the four mutually exclusive Gate B predicates, the guarded rollback, the single May-approve/May-reject helper, the four `lblMessage` sinks, the closed status-badge class set, the named pre-wrap container, the `Current`/`Superseded` audit discriminator, the half-populated resolution case, the `Detail`-null `StatusChange` row, and the split of Gate A into A1, A2 and A3.

It is **not** yet a complete, internally consistent, safely buildable design. This review raises:

- **four blocking corrections** that must be fixed in the design before any gate could be considered for authorisation (Y-1, Y-2, Y-3, Y-4), plus one more at the same severity for the audit contract (Y-5);
- **seven material corrections** (Y-6 through Y-12);
- **four precision corrections** (Y-13 through Y-16);
- **five risks introduced or left open by Revision 2** (Z-1 through Z-5);
- **six test-coverage gaps** (TG-1 through TG-6).

Revision 2's own verdict — **NOT READY FOR IMPLEMENTATION** — is correct and is retained. Every gate is closed. No business decision is treated as approved. No test was run. This review authorises nothing.

The most serious of the new findings is **Y-4**: the resolution-history rule in Gate A2 creates a permanent, unrepairable ticket state for any ticket whose stored resolution already exceeds 1000 characters. The second most serious is **Y-1**: X-1's parse rule accepts a sign or leading whitespace in the four sequence characters, and can emit a ticket number that is not a number.

---

## 2. Basis and method

### 2.1 Documents read in full

| Document | Role |
|---|---|
| `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` | R-02 Rev 2, the subject of this review. 529 lines. |
| `ITSM_SOURCE_TO_FINDING_TRACEABILITY.md` | R-01. 99 lines. |
| `ITSM_BUSINESS_DECISION_REGISTER.md` | D-1 through D-18. 642 lines. |
| `ITSM_FREEBUFF_REMEDIATION_REVIEW.md` | First independent review (X-1…X-5, C-01…C-32, AR-1…AR-16, TC-21…TC-50). 926 lines. |
| `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION.md` | Reconciliation and Gate A split. 806 lines. |
| `AGENTS.md` | Repository boundaries. |

`ITSM_TECHNICAL_REMEDIATION_DESIGN.md` (revision 1) was consulted only where Revision 2 refers to it.

### 2.2 Source files read or grepped for this review

- `AnmolDristi/ITSM/BLL/TicketService.cs` — whole file (506 lines).
- `AnmolDristi/ITSM/BLL/TicketUpdateService.cs` — whole file (648 lines).
- `AnmolDristi/ITSM/BLL/DispatchService.cs` — whole file (126 lines).
- `AnmolDristi/ITSM/BLL/TicketWorkflow.cs` — whole file (75 lines).
- `AnmolDristi/ITSM/BLL/TicketDetailService.cs` — lines 150–209.
- `AnmolDristi/ITSM/BLL/TicketListService.cs` — search and projection lines.
- `AnmolDristi/ITSM/BLL/ItsmTicketAccess.cs` — lines 70–100.
- `AnmolDristi/ITSM/BLL/ApprovalConfigurationService.cs` — method and rollback line numbers.
- `AnmolDristi/ITSM/UI/TicketDetail.aspx` (whole), `.aspx.cs` (lines 220–300 and 380–397).
- `AnmolDristi/ITSM/UI/TicketList.aspx`, `.aspx.cs` (lines 145–179).
- `AnmolDristi/ITSM/UI/CreateTicket.aspx`, `.aspx.cs` (lines 195–239).
- `AnmolDristi/ITSM/UI/ApprovalConfig.aspx`, `.aspx.cs` (lines 118–134).
- `AnmolDristi/BasePage.cs` — `RebuildSessionPermissions` location.
- Entities: `Ticket.cs`, `TicketApproval.cs`, `TicketAuditLog.cs`, `TicketComment.cs`, `TicketStatus.cs`, `SupportMapping.cs`, `UserMaster.cs`.
- Migrations: `202608120550515_InitialItsmSchema.cs`, `202608201200000_AddTicketAuditLog.cs`, `202608240451107_AddApprovalConfiguration.cs`, `Configuration.cs`.

### 2.3 Evidence labels

| Label | Meaning |
|---|---|
| Source-verified | Read in the cited file at the cited location on 24 November 2026. |
| Confirmed | The Revision 2 statement matches the source. |
| Disputed | The Revision 2 statement does not match the source, or is not implementable as written. |
| Environment-unread | A deployed row, grant, collation, edition, or timeout. Not queried. `AnmolDristi/Web.config` was not read. |

### 2.4 What this review did not do

It did not execute SQL, open a UAT or LIVE connection, build the solution, run a test, start a site, or post a payload. It did not read `AnmolDristi/Web.config`. It did not modify Revision 2, revision 1, R-01, the register, the first review, the reconciliation, or any application, SQL, migration or configuration file. Section 12 records the checks it could not run.

---

## 3. Verification of the five blocking corrections X-1 through X-5

### X-1 — Ticket-number parsing

**Verdict: DISPUTED — direction correct, two defects remain.**

Source-verified at `TicketService.cs` `GenerateTicketNumber`, lines 359–401:

- line 364: `var prefix = (requestType.Prefix ?? requestType.Code ?? "XX").Trim();`
- lines 366–368: `if (prefix.Length > 2) { prefix = prefix.Substring(0, 2); }`
- lines 370–372: `yy`, `MM`, `numberPrefix = prefix + yy + mm`
- lines 374–378: the maximum read, `AsNoTracking`, `RequestTypeId` filter, `StartsWith(numberPrefix)`, `.ToList()`
- lines 387–397: the parse, `n.Length >= 10` and `int.TryParse(n.Substring(n.Length - 4), out seq)`
- line 399: `numberPrefix + nextSequence.ToString("D4", CultureInfo.InvariantCulture)`

Revision 2's restatement of the defect is **Confirmed**, including the nine-character `CR2608001` example: a six-character namespace with a three-digit sequence yields a nine-character value that shares the namespace, and the source's last-four slice reads `8001`.

The proposed rule is correct in its central move — exact length, sequence read at offset `namespace.Length` instead of from the end, malformed values not rewritten. It is still **Disputed** on two points:

**Y-1 (blocking). `int.TryParse` is not a digit test.** `int.TryParse(string)` with default `NumberStyles.Integer` accepts a leading sign and leading/trailing white space. A stored ten-character value whose characters at offset 6 are `-123`, `+123`, or ` 123` therefore parses, and X-1 rule 3 treats it as a sequence. The consequence is not theoretical:

- If every stored value in the namespace parses negative, step 5 computes `DefaultIfEmpty(0).Max() + 1`, which is non-positive (for `-123` it is `-122`).
- `(-122).ToString("D4")` is `"-122"` — four characters, because `D4` pads the digit count and not the sign.
- Rule 6's exhaustion check is `> 9999`, so a non-positive value passes it. Rule 7's length check is satisfied.
- The design then inserts a ticket number containing a `-` into `varchar(10)`.

Rule 4 must define malformed as "the four characters at offset 6 are not all ASCII digits `0`–`9`", not "do not parse as an integer", and step 5 must additionally refuse a next sequence below 1.

**Y-1 (second part, internal inconsistency). Rule 2's parenthetical contradicts its formula.** Rule 2 says "`value.Length == namespace.Length + 4` (exactly 10)". Those are the same condition only when the namespace is six characters. Rule 7 already refuses to emit anything that is not ten characters, so the formula admits a length that rule 7 can never produce. State one rule: exactly 10, with the namespace length recorded as the reason.

**Y-2 (blocking). The collation rule is not implementable in C# as written, and §9 read 4 does not cover its failure mode.** Rule 2 requires "the value starts with that namespace under the collation recorded by the precondition read". A C# `string.StartsWith` has no collation; it has a `StringComparison`. The design does not say which. Under a case-insensitive column:

- the SQL maximum read (`LIKE 'CR2608%'`) returns a stored lower-case `cr2608999`;
- an ordinal C# test rejects it as malformed, so it does not advance the sequence;
- the CI unique index still treats `CR2608…` and `cr2608…` as the same value.

A collision then needs only one stored number at or above the current maximum. The insert violates the index, the bounded retry regenerates the same value, and the third failure returns `InvalidOperationException`. §9 read 4 counts case-variant *duplicate groups*; a single lower-case row produces no group and passes that read. Add a read that counts values whose prefix differs from the normalised prefix only by case, and specify the comparison operator rather than referring to "the collation".

### X-2 — Application lock return code

**Verdict: CONFIRMED.**

Source-verified: `ExecuteSqlCommand` appears exactly once under `AnmolDristi/ITSM`, at `TicketService.cs` line 497, the notification `INSERT` used by `EnqueueTicketCreatedNotification`. `GenerateTicketNumber` (line 159) is called inside the transaction opened at line 91, so a `@LockOwner = 'Transaction'` lock taken on that context is owned by the create transaction. Revision 2's engine-contract statement — `ExecuteSqlCommand` returns rows affected, not the procedure return code — is correct, and the proposed batch is the right shape.

Confirmed details:

- the batch tests `@rc < 0`, which covers `-1` timeout, `-2` cancelled, `-3` deadlock victim and `-999` parameter error, and continues on `0` and `1`;
- the resource is per prefix and month, so unrelated prefixes are not serialised;
- the design states the lock is database-scoped, so two databases are not one namespace;
- the failure becomes a controlled `InvalidOperationException` that the guarded rollback sees.

Two consequences the design states but does not bound, recorded here because they are the operational cost of the choice:

- the lock is held from `GenerateTicketNumber` (line 159) to `transaction.Commit()` (line 289), which includes the ticket insert, the audit insert, the approval inserts and the notification insert. Every create in one prefix-month is fully serialised, not just the number read;
- the retry loop can take the lock three times in sequence for one user action.

Neither is wrong. Both need a stated lock-wait budget. Revision 2 leaves `@timeoutMs` unnamed, so no queue depth can be assessed. That is recorded as an open item, not a defect.

### X-3 — Internal audit prefix

**Verdict: CONFIRMED.**

Source-verified at `TicketUpdateService.cs` `AddComment`, the `AddAudit` call: `(isInternal ? "[Internal] " : "") + commentText.Trim()`. The literal is `[`, `I`, `n`, `t`, `e`, `r`, `n`, `a`, `l`, `]`, space — **11 characters**. `TicketAuditLog.Detail` is `maxLength: 1000` (entity `[StringLength(1000)]`, migration `202608201200000_AddTicketAuditLog.cs` line 19). 1000 − 11 = **989**. Revision 2's arithmetic, its per-mode wording, and TC-08's corrected numbers are right, and the withdrawal of revision 1's "990 when marked internal" is explicit.

`TicketComment.CommentText` has no `[StringLength]` and no column maximum; `TicketDetail.aspx` line 339 is `MaxLength="2000"`. Both source claims are **Confirmed**, and the design's point that a client-side `MaxLength` cannot express 989 is correct.

### X-4 — Audit truncation

**Verdict: CONFIRMED IN DIRECTION, DISPUTED ON CONTRACT.**

Source-verified at `TicketUpdateService.cs`:

- `AddAudit` lines 602–622 assigns `Truncate(actionType, 50)`, `Truncate(oldValue, 200)`, `Truncate(newValue, 200)`, `Truncate(detail, 1000)`;
- `Truncate` lines 624–632 returns `value.Substring(0, maxLength)` and signals nothing;
- `CreateTicket` writes the `Created` audit row inline (`TicketService.cs` lines 196–208) and does not call `Truncate`, confirmed;
- the caller set named in X-4 is complete: `AddComment`, `RejectTicket`, `OverridePriority`, `ApproveTicket`, and the null-detail `ChangeStatus`.

The column limits X-4 relies on are **Confirmed**: `ActionType` 50, `OldValue` 200, `NewValue` 200, `Detail` 1000 (`202608201200000_AddTicketAuditLog.cs` lines 17–20); `MST_TicketStatus.Name` and `MST_Priority.Name` `maxLength: 100` (`202608120550515_InitialItsmSchema.cs` lines 255 and 218); `SupportMapping.SupportAgentCode` `[StringLength(50)]`.

Still **Disputed**, as **Y-5**: the design says a failed check is "`TicketUpdateResult.Fail` or a thrown exception" without choosing. `AddAudit` is `private static void` and every caller calls it *after* mutating the ticket (for example `ChangeStatus` lines 95–108). A `void` helper can only throw. A thrown `DbEntityValidationException` or `InvalidOperationException` reaches the page, and `btnApprove_Click` / `btnChangeStatus_Click` have no catch that converts it to `lblMessage`. The design must either change the return contract and place the check before mutation, or state that the invariant is a programmer error that throws and that no page gains a handler.

Also as **Y-14**, X-4 lists four fields. `AddAudit` writes a fifth, `ActorCode`, untruncated. `TicketAuditLog.ActorCode` is `[StringLength(50)]`, so an over-length code fails EF validation rather than being cut. Record that, so a later reader does not assume `AddAudit` bounds all five fields.

### X-5 — Gate ordering

**Verdict: CONFIRMED.**

Source-verified: `ApproveTicket` sets `approval.ApprovalStatus = "Approved"` and then only moves the ticket when `inProgressStatus != null` (lines 316–332); `RejectTicket` marks the approval rows `Rejected` and then only moves the ticket when `rejectedStatus != null` (lines 407–432). Both commit. Removing F-08 from A1 and A2 and holding it as A3 is the correct correction, and the pass condition — an active `In Progress`, and an active `Rejected` or `Cancelled` — matches what the two methods actually look up (`FirstOrDefault(s => s.Name == "In Progress" && s.IsActive)`; `Rejected` then `Cancelled`).

Confirmed supporting facts: the seed creates these names only when `MST_TicketStatus` is empty (`Configuration.cs` lines 26–38), and a search of every `*.sql` in the repository for `MST_RequestType`, `MST_TicketStatus` and `MST_SupportMapping` returns no file. The seed is not a deployed reading, as X-5 states.

One precision note, recorded as **Y-13**: AR-1 and §9 read 1 ask for the `ResolveInitialStatusId` result "per active request type". The method's input is only `requiresManagerApproval || requiresItHeadApproval` (`TicketService.cs` lines 166, 404–432), so two request types with the same two flags need one row, not two. Narrow the read or state that per-type rows are expected.

---

## 4. Finding-by-finding verdict

Every F-01 through F-10 root cause was re-checked against source. All ten hold. The verdict column judges the *remediation*, not the finding.

| ID | Root cause re-verified | Verdict | Required change |
|---|---|---|---|
| F-01 | Confirmed. See §3 X-1 and X-2. | **Accepted with Corrections** | Y-1, Y-2, Y-3, Y-11, Y-12 |
| F-02 | Confirmed, with two pages' scope narrowed. See below. | **Accepted with Corrections** | Y-7, Y-8, Y-15 |
| F-03 | Confirmed. | **Accepted with Corrections** | Y-6 |
| F-04 | Confirmed. | **Accepted with Corrections** | Y-4, Y-9, Y-10 |
| F-05 | Confirmed. | **Accepted with Corrections** | Y-5, Y-14 |
| F-06 | Confirmed. | **Accepted** | — |
| F-07 | Confirmed. | **Accepted** | — |
| F-08 | Confirmed. | **Accepted** | — |
| F-09 | Confirmed. | **Accepted** | — |
| F-10 | Confirmed. | **Accepted** | Y-13 (precision) |

### F-01 — Ticket-number allocation

Accepted with Corrections. The `RequestTypeId`-filter removal, the lock-plus-unique-index direction, the retry seam being inside `TicketService` (the page cannot retry: `CreateTicket` constructs its own context at line 91 and takes no context argument), the non-idempotent retry, the identity gap after a rolled-back insert, and the ten-character output are all correct. The parse rule needs Y-1 and Y-2, the retry predicate needs Y-3, and the precondition set needs Y-11 and Y-12.

### F-02 — Output encoding

Accepted with Corrections. Revision 2's source column is **Confirmed** for `TicketDetail.aspx` lines 361 and 381–386, for the already-encoded comment text (line 367) and approval comments (line 394), and for the four `lblMessage` sinks at `TicketDetail.aspx.cs` line 393, `TicketList.aspx.cs` line 336, `CreateTicket.aspx.cs` line 454 and `ApprovalConfig.aspx.cs` line 155 — all four verified at exactly those lines.

Confirmed and correctly handled: `lblStatusBadge.CssClass` is built from the raw name at `TicketDetail.aspx.cs` lines 252–253 and again from the bound label's text at `TicketList.aspx.cs` lines 163–168. Revision 2's instruction to derive both from a whitelist on the raw value, and not from already-encoded text, is the right correction and is the reason C-10 is closed.

**Y-7.** The earlier review listed three unencoded sinks on the list page: `TicketList.aspx` line 141 `<%# Eval("TicketNumber") %>` inside the anchor, line 151 `Text='<%# Eval("PriorityName") %>'`, and line 157 `Text='<%# Eval("StatusName") %>'`. All three are still unencoded in the tree. Revision 2's F-02 source column does not mention them, and A1's bullets name only `LoadTicket` and `lblMessage`. The sentence "Encode at each output assignment" in §4 is a principle, not an implementation instruction, and the page is not in A1's list. Name all three bindings in A1.

**Y-8.** `TicketDetail.aspx.cs` line 231 assigns `lblTenantBadge.Text = FormatTenantBadge(dto.CompanyName, dto.CompanyId)` — a stored `Company_Description`. The `dto.CsatScore.HasValue` branch at line 256 assigns `lblCsatFeedback.Text = dto.CsatFeedback`. Neither is inside the range Revision 2 cites (233–253). `ApprovalConfig.aspx.cs` lines 126–128 assign `lblCurrentItHead.Text` from `ItHeadApproverCode` and `ItHeadApproverName`, and `lblCurrentItHeadStatus.Text` from `ItHeadApproverStatus`. A1 encodes only `lblMessage` on that page. All four are `Label.Text` writes of stored values. Extend the cited ranges and the A1 enumeration so "every untrusted string" has a page-and-line list rather than a general clause.

**Y-15.** A1 encodes page output and leaves `ticket.TicketNumber` unencoded in the notification body and subject (`TicketService.cs` lines 490–503). Revision 2 correctly records that the number's safety depends on F-01's prefix rule — but that dependency points the wrong way for A1: A1 ships before Gate B, so for the whole life of A1 the value is whatever `Prefix` + `yyMM` + digits produces, and `MST_RequestType.Prefix` has no check constraint on its characters. Encode it in A1; it is one call and removes the dependency.

### F-03 — Rejection reason length

Accepted with Corrections. The source is **Confirmed**: `RejectTicket` refuses blank at lines 363–367, assigns `reason.Trim()` at line 409, and only `DbUpdateConcurrencyException` becomes `Fail`; `txtRejectionReason` (`TicketDetail.aspx` lines 317–320) has no `MaxLength`, while `txtOverrideReason` on the same page (line 296) is `MaxLength="500"`. Placing the check with the blank check, before the context opens, is correct.

**Y-6.** `ApproveTicket` assigns `approval.Comments = comments` **untrimmed**, while `RejectTicket` stores `reason.Trim()`. A2 says the reject path trims then checks, and that `ApproveTicket` "applies the same check whenever `comments` is non-null". If "the same check" means trim-then-check in a method that stores the untrimmed string, a 500-character comment followed by whitespace passes validation and throws at `SaveChanges`, which the page does not catch. State, for both methods, that the string validated is the string stored.

### F-04 — Resolution history

Accepted with Corrections. **Confirmed**: `ChangeStatus` replaces `ResolutionText` and `ResolvedOn` only when the target name is `Resolved` (lines 98–102), passes `detail: null` to `AddAudit` (line 108), and the map allows `Resolved` → `Closed` and `Resolved` → `In Progress` (`TicketWorkflow.cs` lines 23–26). `ResolutionText` has no `[StringLength]` (`Ticket.cs` line 107); `Detail` is 1000. `DateTime.Now` is called at lines 95, 101, 106 and again inside `AddAudit` — four calls, so "one `now` per transition" is a real change, correctly required.

**Y-4 (blocking).** The design creates an unrepairable state. On re-resolve: "if the previous text is longer than 1000 characters, return `Fail` and change nothing". On reopen: "leave `ResolutionText` and `ResolvedOn` unchanged". No method in `TicketUpdateService` clears `ResolutionText`. A ticket that already holds a 1200-character resolution when A2 ships can therefore never be resolved again: re-resolve refuses, reopen preserves the offending text, and re-resolving after the reopen meets the same previous text. The user's only path is a manual data change. This is the register's D-17 option A consequence arriving as design text while D-17 stays Pending, and TC-06, TC-32 and TC-35 do not cover a pre-existing over-length resolution. Either define an escape (for example, write the `Resolution` row with `Detail` null and `OldValue` stating that the previous text was not copied because it exceeds the audit capacity, under an owner rule) or move the refusal out of design text into the D-17 decision. As written, A2 cannot ship safely.

**Y-9.** C-18 required the maximum to be surfaced in the UI (`The UI states the maximum before the user types`). Revision 2 registers the acknowledgement in §13 and says the limits "are not technical details that can ship unnoticed", but does not say the page states the limit: `txtResolution` (`TicketDetail.aspx` lines 235–239) still has no `MaxLength` and no help text.

**Y-10.** A2 has no precondition on the deployed `Resolved` row. `ChangeStatus` returns before the resolution branch when `currentStatus.IsTerminal` (lines 73–77). The seed writes `Resolved` as non-terminal (`Configuration.cs` line 34), but a seed is not a deployed reading. If the environment's `Resolved` is terminal, A2's resolution history is unreachable, the owner acknowledgement is moot, and TC-05/07/32/33/34/35 pass only on a disposable database that seeds its own row. §9 read 1 already records `IsTerminal`; attach it to A2 as well as to A3.

### F-05 — Silent truncation

Accepted with Corrections. See §3 X-3 and X-4 for the verified arithmetic and the contract gap (Y-5, Y-14). The placement requirement is **Confirmed** and correct: `AddComment` adds the `TicketComment`, then assigns `ModifiedOn`/`ModifiedByCode` at lines 498–499, then calls `AddAudit`. A refusal before the entity is added and before 498–499 is exactly right, and it is what makes the rollback argument unnecessary rather than load-bearing.

### F-06 — Approve/reject visibility

Accepted. `TicketDetailService.cs` line 176 sets `CanApprove` from any pending row for the actor, with no status, no level and no requester filter — **Confirmed**. `TicketDetail.aspx.cs` line 281 binds `pnlApprovalActions` from it, line 282 binds `pnlWorkActions` to non-terminal and not-`Pending Approval` with no `CanProcessTicket` requirement, and line 283 makes `pnlActions` dependent on the two panels. All three verified exactly. The single helper, the C-24 binding list, the preserved IT Head rejection while a manager row is `Pending`, and leaving `CanProcessTicket` as requester-or-assignee are correct and match `ItsmTicketAccess.cs` lines 74–98. No correction.

### F-07 — Dispatch determinism

Accepted. Source **Confirmed** at `DispatchService.cs`: `FirstOrDefault()` with no `OrderBy` at lines 88–90, 107–108 and 122–123; the cascade predicates are `IsActive`, `BranchId`, `CategoryId`, `SubCategoryId`, `!IsBranchDefault`, with `ItemId` non-null for the item step and null for the other two; and the branch default requires `IsBranchDefault` with all three taxonomy keys null. A blank `SupportAgentCode` is returned as `""`, which is not null, so the cascade stops — the defect Revision 2 describes.

Two confirmations that make Gate B's four indexes correct rather than merely plausible: the cascade's own predicates match the four Gate B filters exactly (including `IsBranchDefault = 0` for the first three and all-null taxonomy for the fourth), and `SupportMapping.BranchId` is a non-nullable `int`, so no filtered unique key has a NULL-duplicate problem. Gating empty-agent fall-through on the blank-row count is right. The residual — an active row with `IsBranchDefault = 0` and all three taxonomy keys null matches no cascade step and none of the four filters — is stated and correctly left outside Gate B (AR-4).

### F-08 — Partial commit under a missing status

Accepted. See §3 X-5. The A3 condition, message, placement after `RequireRecordedTenant`, refusal to insert a status row, and the rollback that redeploys the previous build instead of reversing committed outcomes are all correct.

### F-09 — Branch identity

Accepted. "Report only" is the right disposition, and the four identifiers named in §9 reads 12–13 are the ones the question needs. Confirmed: `TicketService.cs` line 151 passes `requester.BranchId` to `CalculateSla` and line 153 passes `resolvedBranchId` (`branch.Id`, line 120) to dispatch; line 179 stores `MST_Branch.Id`; `AddBranchLegacyId` is an empty migration; the uniqueness claim lives in a `Branch.cs` comment. No SLA column is proposed and D-7 stays Pending. No correction.

### F-10 — Seed is not configuration

Accepted. Confirmed: the seed runs only under `if (!context.TicketStatuses.Any())` (`Configuration.cs` lines 26–38) and uses `AddOrUpdate`, so an existing table is never updated; a repository-wide search of `*.sql` for `MST_RequestType` returns no file at all, while `ITSM_RequestType` is read by create (`TicketService.cs` line 136) and drives the prefix and both approval flags. The 16 precondition reads in §9 are a proportionate response, and read 14 correctly reproduces `BasePage.RebuildSessionPermissions` (`AnmolDristi/BasePage.cs` line 153 — note the file is at the project root, not under `UI/`; the line number Revision 2 cites is right).

---

## 5. Re-verification of AR-1 through AR-16

All sixteen are present. Each disposition was checked against source. None is contradicted by the code; four need the correction named.

| ID | Verified against source | Verdict |
|---|---|---|
| AR-1 | `ResolveInitialStatusId` lines 404–432: name lookup without `OrderBy`, then `.Where(s => s.IsActive && !s.IsTerminal).OrderBy(s => s.Id).FirstOrDefault()`. Called at line 166, before the approval inserts. Approval rows are inserted regardless. | **Holds.** Leaving it fail-open and reporting the fallback is right. Narrow the read (Y-13). |
| AR-2 | Unordered name lookups confirmed at `ApproveTicket` line 328 (`In Progress`), `RejectTicket` lines 424–427 (`Rejected`, then `Cancelled`) and `ResolveInitialStatusId` lines 407–412 (`Pending Approval` / `New`). `TicketWorkflow.GetAvailableStatuses` filters `IsActive` and `nextNames.Contains` and returns every match. `MST_TicketStatus` has no unique key on `Name`. | **Holds.** Not adding `OrderBy` and not choosing a survivor is correct. |
| AR-3 | Blank `SupportAgentCode` returns `""`, not null; `""` wins the cascade. `SupportAgentCode` is `[Required] [StringLength(50)]`. | **Holds.** Read-gated fall-through is right. |
| AR-4 | All-null-taxonomy, `IsBranchDefault = 0` rows match no step and no filter. | **Holds.** Correctly left as a stated limit. |
| AR-5 | `ApproveTicket` computes `stillPending` excluding the current row; a second `Pending` row keeps the ticket in `Pending Approval`. `RejectTicket` marks the other pending rows `Rejected` with a fixed 53-character comment, so stranding is an approve-path outcome. | **Holds.** |
| AR-6 | Six unguarded `transaction.Rollback()` calls confirmed at lines 119–122, 185–188, 254–257, 349–352, 449–452, 513–516, and `CreateTicket` lines 292–295 is a bare `catch` with the same shape. | **Holds.** Lines exact. |
| AR-7 | `lblMessage.Text` at 393 / 336 / 454 / 155 and the four `ClearMessage` assignments at 388 / 331 / 449 / 150. | **Holds.** "The four assignments" should say the four that carry text; the `string.Empty` ones need no encoding. |
| AR-8 | `AddComment` adds the comment entity, then lines 498–499, then `AddAudit`. | **Holds.** Lines exact. |
| AR-9 | No `AntiForgery`, `RequestVerificationToken` or `ViewStateUserKey` reference under `AnmolDristi/ITSM`. | **Holds as a record.** Owner and date still unnamed; see Z-4. |
| AR-10 | `Branch.cs` uniqueness is a comment; `AddBranchLegacyId` is an empty migration. | **Holds.** |
| AR-11 | No ITSM query navigates `UserMaster.Branch`; `TicketDetailService` and `TicketListService` use `Ticket.Branch`. | **Holds.** |
| AR-12 | Line 364 trim; `Prefix` is `char(2)`; `length > 2` is truncated at lines 366–368. | **Holds, incomplete.** See Y-12. |
| AR-13 | `CreateTicket.aspx.cs` lines 203–208 and 210–215 show `ex.Message` for `InvalidOperationException` and `ArgumentException`; the generic catch shows `diag.GetUserFacingMessage()`. | **Holds.** |
| AR-14 | `AddApprovalConfiguration` creates its index with raw `Sql(...)`: `CREATE UNIQUE INDEX UIX_MST_ApprovalConfiguration_IsActive … WHERE IsActive = 1` (lines 25–27), inside an EF6 migration. | **Holds.** |
| AR-15 | The trigger needs `ALTER`; UAT and LIVE exclusion is right. | **Holds.** |
| AR-16 | `TicketListService` line 231 matches `Description`; the projection does not select it. | **Holds.** |

`ApprovalConfigurationService.UpdateRequestTypeApproval` (line 70) and `SetActiveItHead` (line 163) do use the same unguarded rollback, at lines 104 and 245 as Revision 2 states, and they change no ticket. Correctly outside the F-08 set.

---

## 6. Reconciliation of the first review's C-01 through C-32

Every C item was checked for whether Revision 2 closes it. The result: **24 closed, 6 closed with a correction, 2 not closed.**

| C | Subject | State in Revision 2 |
|---|---|---|
| C-01 | One normalised prefix for namespace, lock resource and parse; case-variant duplicate read | **Closed with correction** — Y-2 |
| C-02 | Retry seam inside `TicketService`, three attempts, new context, 2601/2627 plus index name | **Closed with correction** — Y-3 |
| C-03 | Each attempt re-reads everything and takes a new `createdOn` | Closed |
| C-04 | Refuse when the formatted result is not ten characters | Closed |
| C-05 | Prefix read before the exact-2 rule; no padding, substring or `Code` fallback | **Closed with correction** — Y-12 |
| C-06 | `NOT NULL` means no one-NULL duplicate; blocking build; `DROP INDEX` rollback | Closed |
| C-07 | `sys.sp_getapplock` execute permission for `DbConn` | Closed (§9 read 8) |
| C-08 | The lock is database-scoped | Closed |
| C-09 | `lblMessage` on all four pages | Closed |
| C-10 | Closed status-badge class set; whitelist instead of the `ToLower().Replace` transform | Closed |
| C-11 | Name the pre-wrap target and where the rule is added | Closed — anonymous divs at `TicketDetail.aspx` 225–227 and 231–233, style block at line 15, rule added explicitly |
| C-12 | `ListItem` text is framework-encoded and unchanged | Closed |
| C-13 | Mail's raw number depends on F-01 | Closed as a record; **Y-15** asks for the one-line fix instead |
| C-14 | `CreateTicket` `InnerText` / `Attributes` recorded clean | Closed |
| C-15 | 500-character check with the blank check, before the context | Closed |
| C-16 | `ApproveTicket` check unconditional on a non-null comment | **Closed with correction** — Y-6 |
| C-17 | Rethrow acceptable only if the comment and override bounds ship together | Closed — Gate A2 is one release and states the reason |
| C-18 | 1000-character resolution maximum as an owner-visible item, surfaced in the UI | **Not closed** — Y-9 (UI surfacing absent) |
| C-19 | `Current` / `Superseded` on every `Resolution` row | Closed |
| C-20 | One timestamp per transition | Closed |
| C-21 | Half-populated pair; no backfill of pre-change resolutions | Closed |
| C-22 | `StatusChange.Detail` stays null; retention recorded in `OldValue` | Closed |
| C-23 | 2000 → 1000 / 989 as an owner-visible change; per-mode client hint; refusal before `ModifiedOn` | Closed |
| C-24 | DTO members, every panel and control binding, one shared helper | Closed — cites the reconciliation's C-24, which is where that table lives |
| C-25 | Exact `OrderBy(...).Select(...).FirstOrDefault()` shape; empty-string behaviour; the read | Closed |
| C-26 | Four mutually exclusive predicates and keys; the uncovered orphan | Closed |
| C-27 | Ordering is per call and does not freeze an assignment | **Not closed** — no sentence states that a test must not assert stability across a data change |
| C-28 | Guarded rollback rethrowing the original exception | Closed |
| C-29 | (a) duplicate pending rows and the missing key, (b) trigger privileges, (c) identity gaps, (d) after `RequireRecordedTenant` | Closed — (a) read 2, (b) AR-15, (c) Gate B, (d) A3 |
| C-30 | Fault-injection environment named and excluded from UAT/LIVE | Closed — §9 read 17 and AR-15 |
| C-31 | UAT and LIVE excluded from every write test | Closed — §10 preamble and the per-row "Needs" column |
| C-32 | No status row inserted to make a branch pass | Closed — A3 and §15 |

C-18 and C-27 are the only C items the design still leaves open. C-18 matters because the 1000-character cap is a product limit on a field the application accepts without one today; a user who types 1400 characters discovers the limit after submitting.

---

## 7. Corrections still required

Numbered Y-1 through Y-16. Y-1 to Y-5 are blocking: a gate cannot be considered for authorisation while one is open.

| ID | Gate | Correction |
|---|---|---|
| **Y-1** | B | **Blocking.** X-1 rule 4 must treat a sequence that is not four ASCII digits as malformed — `int.TryParse` accepts `-123`, `+123` and ` 123`, and a non-positive next sequence formats to four characters and passes both the exhaustion and length tests. Step 5 must also refuse a next sequence below 1. Rule 2's "(exactly 10)" conflicts with its own formula for any namespace other than six characters; state one rule. |
| **Y-2** | B | **Blocking.** X-1 rule 2's "under the collation recorded by the precondition read" has no C# form. Name the comparison, and add a read that counts values whose prefix differs from the normalised prefix only by case. Read 4's duplicate-group count does not detect the single-row case, which can exhaust the retry. |
| **Y-3** | B | **Blocking.** The retry predicate matches the ticket-number index *name* inside `SqlException.Message`, which SQL Server renders in the instance language. Specify how the name is obtained, or add "a non-English server message language" to §9 and provide a schema-independent discriminator. |
| **Y-4** | A2 | **Blocking.** A ticket whose stored resolution exceeds 1000 characters can never be resolved again: re-resolve refuses, reopen preserves the text, and nothing clears the column. Define the escape, or move the refusal into D-17 as a decision. Add a test for a pre-existing over-length resolution. |
| **Y-5** | A2 | **Blocking.** X-4 does not say how `AddAudit` reports failure. It is `static void` and every caller has already mutated the ticket. Choose the contract (return a result and check before mutation, or throw an invariant exception), and state the page's behaviour. Add a refusal test; TC-37 only proves 100-character names are stored in full. |
| **Y-6** | A2 | Validate the exact string that is stored. `ApproveTicket` stores `comments` untrimmed; a trim-then-check rule lets a 500-character comment plus whitespace through to `SaveChanges`, which the page does not catch. |
| **Y-7** | A1 | Name `TicketList.aspx` lines 141, 151 and 157 in A1 and add a test. They are the unencoded sinks the first review listed and A1 does not mention them. |
| **Y-8** | A1 | Extend F-02's cited range and A1's enumeration to `lblTenantBadge` (line 231), `lblCsatFeedback` (line 256 branch) and `ApprovalConfig.aspx.cs` lines 126–128. |
| **Y-9** | A2 | State the 1000-character maximum on the page, as C-18 required (`txtResolution`, `TicketDetail.aspx` lines 235–237), and record that in §13. |
| **Y-10** | A2 | Attach §9 read 1's `Resolved` `IsTerminal` and `IsActive` to A2. If the deployed `Resolved` is terminal, the resolution history is unreachable and A2's acceptance tests pass only on a disposable database. |
| **Y-11** | B | Add a read for other writers of `itsm.Tickets.TicketNumber`. The lock serialises only writers that take it; ITSM shares the application and `DbConn` with HRMS import and VMS, and `Database/` holds ad-hoc SQL. |
| **Y-12** | B | AR-12 covers a prefix that trims to one character. A prefix of three or more characters is *truncated* to two today (lines 366–368) and creates successfully; the exact-2 rule newly blocks it. Name that branch in AR-12 and in the §13 acknowledgement. |
| **Y-13** | A3 | Narrow §9 read 1's "per active request type" for the `ResolveInitialStatusId` result: the method's input is the two approval flags, so two types with the same flags need one row. |
| **Y-14** | A2 | Record that `AddAudit` writes `ActorCode` untruncated and that EF's `[StringLength(50)]` turns an over-length code into an exception rather than a cut, so `AddAudit` does not bound all five fields. |
| **Y-15** | A1 | `HtmlEncode` the ticket number in the notification body and subject now, instead of depending on Gate B's prefix rule. |
| **Y-16** | — | Tests that need an owner decision are listed as TC-06, TC-08, TC-17 and TC-48. TC-32 is the same class of case and is not listed. Add it, or state why not. |

---

## 8. Risks introduced or left open by Revision 2

These are not in AR-1 through AR-16. Each is a consequence of the revised design.

| ID | Risk | Basis | Severity |
|---|---|---|---|
| **Z-1** | **A permanent un-resolvable ticket state.** Y-4's mechanism: a stored resolution above 1000 characters blocks every future resolve, and no path clears it. | `ChangeStatus` lines 98–102; A2's re-resolve rule | High. It is a data-dependent dead end that support cannot clear through the UI. |
| **Z-2** | **A ticket number that is not a number.** Y-1's mechanism: signed four-character sequences can reach `ToString("D4")` and insert `CR2608-122`. | `int.TryParse` semantics; `D4` formatting | High if it happens, low likelihood; the value is user-visible, mails it, and the eventual Gate B index accepts it. |
| **Z-3** | **Create throughput is serialised and unbounded.** The applock is held from number generation to commit, covering four `SaveChanges` calls and one raw insert, and a retry can acquire it three times. No lock-wait budget is stated, so no queue depth can be assessed. | `TicketService.cs` lines 159–289; X-2 | Medium. Operational, not correctness. |
| **Z-4** | **A1 ships state-changing pages with no anti-forgery control and no owner.** AR-9 is recorded, given no owner and no date, and left outside every gate — while A1 is the release that touches those pages. ViewState MAC remains unread, so the residual cannot be sized. | AR-9; §15 | Medium. The design states it honestly; the change authorisation for A1 must carry it explicitly. |
| **Z-5** | **The A2 limits are forward-only refusals on unbounded historical data.** Nothing counts how many existing `CommentText` or `TicketAuditLog.Detail` rows exceed the new caps, or how many tickets hold a resolution above 1000 characters. The owner is asked to accept a capacity reduction without the size of the affected population. | §9 (no such read); §13 | Medium. It is the difference between an informed acknowledgement and an empty cell. |

Also worth recording as a design strength, not a risk: concurrency on approve/reject is protected by `Ticket.RowVersion`, not by the approval row (which has no concurrency token). `ApproveTicket` and `RejectTicket` always touch the ticket row (`ticket.ModifiedOn` is set unconditionally at lines 335 and 434), so the `[Timestamp]` predicate is always in the `UPDATE`. TC-45's expected result is therefore achievable in both data shapes — two approvers on their own rows, and one approver twice.

---

## 9. Test matrix TC-01 through TC-59

### 9.1 Structure check

**Confirmed.** The table contains exactly 59 rows, IDs TC-01 through TC-59 with no gap and no duplicate. Every row's disposition is **NOT RUN**. The "not run" statement in §2, §10 and §15 is consistent, and no row claims a pass, a partial pass, or an observation.

### 9.2 Reconciliation against the first review

TC-01 through TC-20 are retained from revision 1 with TC-08 corrected to 1000/1001 public and 989/990 internal. TC-21 through TC-50 are the first review's new cases and all fifty are present with their coverage improved: TC-24 now references the normalisation and the recorded collation, and TC-28 states the closed class set. TC-51 through TC-59 are new and map one-to-one onto AR-4, AR-6, AR-5, AR-10, AR-11, AR-9, AR-14, AR-15 and AR-16. Nothing was silently dropped.

### 9.3 Coverage gaps

| ID | Gap |
|---|---|
| **TG-1** | No case covers a stored ten-character value whose sequence characters are not digits (`CR2608-123`, `CR2608 123`). TC-22 covers length only. This is Y-1's path. |
| **TG-2** | No case covers a stored value whose prefix differs from the normalised prefix only by case, where that row is the current maximum. TC-24 covers two request types normalising to one namespace; it does not cover one stored lower-case row and the retry exhaustion that follows. This is Y-2's path. |
| **TG-3** | No case covers `AddAudit`'s refusal path — an over-length `OldValue`, `NewValue` or `Detail` that is refused rather than truncated. TC-37 proves 100-character names are stored in full, which is the opposite direction. This is Y-5. |
| **TG-4** | No case covers a ticket that already holds a resolution above 1000 characters before A2 ships: reopen it, then attempt to resolve. This is Y-4, and without it the dead end is discovered by a user. |
| **TG-5** | No case covers encoding on `TicketList.aspx`: the ticket-number anchor, the priority label and the status label. TC-02 covers the title in the list only. This is Y-7. |
| **TG-6** | No case proves the applock is **released** after commit, so that the next create in the same prefix-month does not wait. TC-23 covers failed acquisition only. A held lock after commit would surface as a timeout on the second create. |

Placed correctly and not to be widened: the list of tests needing an owner decision (TC-06, TC-08, TC-17, TC-48), the list needing a named-environment read (TC-20, TC-49, TC-54), and the rendered-page list. TC-19 is an inspection, not a test, and is labelled as such.

### 9.4 What the matrix does not claim

No row asserts that a limit is a shipped acceptance criterion. TC-06, TC-08 and TC-17 say they wait on the §13 acknowledgements, and TC-48 says its fail-closed branch is expected only after an owner decision that does not exist. That is correct handling and should be preserved.

---

## 10. Gate readiness

No gate is open. This is an assessment of technical completeness, not an authorisation.

| Gate | Technical state | Blocking corrections | Can it be authorised now |
|---|---|---|---|
| **A1** — encoding, panels, `OrderBy`, guarded rollback | Substantially complete. The encoding boundary, the class whitelist, the pre-wrap container, the C-24 binding set, the dispatch `OrderBy` and the six guarded rollbacks are specified to the line and verified. | Y-7, Y-8, Y-15. Z-4 (no anti-forgery owner) is a disclosure the change authorisation must carry. | **No.** Two sink lists are incomplete, and AR-9 is unowned. |
| **A2** — lengths, audit fail-closed, resolution history | Direction correct, contract incomplete, and one rule creates an unrecoverable state. | Y-4, Y-5, Y-6, Y-9, Y-10, Y-14. §13 acknowledgements unrecorded. | **No.** Y-4 alone prevents it. |
| **A3** — F-08 fail-closed | The most complete gate. The two refusal conditions match what the code looks up, the placement is after `RequireRecordedTenant`, the message states the missing configuration, no status row is inserted, and rollback is a redeploy that does not reverse committed outcomes. Correctly separated from A1 and A2. | None in the design. Read 1 is unrun, so the gate is closed by its own rule, as intended. | **Not yet** — the precondition read does not exist. This is a data gap, not a design defect. |
| **B** — allocation, lock, indexes, retry | The best-specified part of the document, and still not buildable. The four predicates correctly partition the cascade, `BranchId` is non-nullable so no key has a NULL-duplicate problem, `TicketNumber` is `NOT NULL` so the unfiltered unique index is sound, and the retry seam is correctly placed. | Y-1, Y-2, Y-3, Y-11, Y-12. Plus the document's own five blockers: schema mechanism, precondition reads, shared-sequence acknowledgement, timeout, normalisation case. | **No.** |
| **C** — D-1 through D-18 | Closed by construction. No behaviour in any build depends on a Gate C ruling, and the preserved workflow facts are correctly listed. | — | **No** — all 18 rows are Pending. |

### 10.1 Dependency check

The dependency matrix in §8 is internally consistent with §7 and §9. Two observations:

- A2 is correctly declared independent of A1 as a technical prerequisite. That is true: A2's length checks and resolution rows do not need A1's encoding. But A2 does change user-visible refusals on pages A1 encodes, so shipping A2 without A1 puts a refusal message through an unencoded `lblMessage`. The design does not state an ordering preference. It is not a defect — the message texts are fixed strings — but a release plan should say which ships first.
- Gate B's retry depends on AR-6's guarded rollback on `CreateTicket`, which §8 places in B. Correct: a retry that cannot see the original exception cannot retry.

### 10.2 Sequencing constraint the design does not state

The `RequestTypeId` filter removal (Gate B) widens the maximum read to every ticket whose number starts with the namespace. There is no index on `TicketNumber` in the tree today (initial migration lines 117–128), so that read is a scan, and it now runs while the exclusive applock is held. Gate B contains both the read change and the index, so the scan exists only within that one release — but the design should state that the index is created **before** the application build that widens the read is deployed, otherwise the first creates after deployment hold a serialising lock across a full scan of `itsm.Tickets`.

---

## 11. Business decisions D-1 through D-18

**No pending decision is treated as approved.** Verified across every section that mentions them:

- §2: "The register's status for D-4 is Pending. A recommendation is not an authorisation to change the database. D-9 … stays Pending."
- §4: every row's "Still required" column names the pending decision where one applies (D-4, D-5, D-7, D-8, D-10, D-14, D-17).
- §7 Gate C: "D-1 through D-18 stay Pending. The register section 6 status column is Pending on every row. Recommendations in that register, including D-4 option A and D-17 option A, are not approvals."
- §8: "All 18 Pending."
- §13: the acknowledgement table is deliberately *not* D-19 through D-n, and says so; it closes no row.
- §14 item 11, §15: both restate the constraint.

Three specific checks the task asked for:

- **D-4** (how uniqueness is enforced). Revision 2 adopts option A's *method* — unique index, locked allocation, current public format, 9999 ceiling — and repeatedly states that adopting a recommendation is not a ruling. Correct. It also does not treat D-4 as covering the prefix-normalisation case, which is right.
- **D-17** (what counts as a resolution). Revision 2 keeps the minimum-length question Pending and treats the 1000-character maximum as a separate owner-visible consequence. Correct in principle; Y-4 is the consequence of shipping the maximum while the minimum stays open.
- **D-9** (approval-flag policy). Correctly separated from the shared-prefix question. The shared-sequence acknowledgement is additional and closes neither D-4 nor D-9.

No environment row is assumed. No implementation, migration or deployment is authorised anywhere in the document; §12 and §15 are explicit.

---

## 12. Backward compatibility, transactions, concurrency, security

### 12.1 Backward compatibility — sound

§11 is accurate on every point checked. Historical `TicketNumber` values are not rewritten and the ten-character rule governs only allocation. `TicketAuditLog` rows are not updated. Request-type prefixes are not changed by the design, and a blocked prefix stays usable until AR-12's read and owner acceptance. Session keys are unchanged: A1 through B add no session key, and `GetActorCode` still reads `Session["WORKMAN"]` (`TicketDetail.aspx.cs` line 341). The transition map is untouched. `ResolveInitialStatusId` keeps its fallback. Identity gaps after a rolled-back insert are stated and not repaired. `Resolution` audit rows written under A2 survive an application rollback, which is the right call.

One omission: §11 does not mention the historical rows where `TicketComment.CommentText` (unbounded) and the audit `Detail` (truncated to 1000) already disagree. After A2 no new divergence is possible, but the existing ones stay and are not counted (Z-5).

### 12.2 Transaction integrity — sound with one contract gap

Every update method opens one `BeginTransaction`, mutates, writes one audit row in the same `SaveChanges`, and commits once (`Commit`, lines 634–640). The design's claim that "no approval-status change without its audit row, and no audit row without its ticket change" is **Confirmed** for the update methods, and the refusal-before-mutation ordering in A2 and AR-8 is what makes it hold without relying on rollback. `CreateTicket` is different: it calls `SaveChanges` four times (ticket, audit, manager row, IT Head row) inside one transaction and writes the notification through raw SQL, so its atomicity comes from the transaction, not from one `SaveChanges`. The design says this correctly in X-4 point 4 and in Gate B's retry text.

The gap is Y-5: `AddAudit` cannot return `Fail`, so the promised "fail closed before `SaveChanges`" has no reporting channel.

### 12.3 Concurrency — sound

`Ticket.RowVersion` is the only concurrency token, and every mutating method both sets `OriginalValue` from the client (`LoadMutableTicket`) and touches the ticket row, so the token is always in the `UPDATE`. `DbUpdateConcurrencyException` becomes `Fail` with the fixed message and rolls back. `TicketApproval` has no token, and the design's decision to compare against the ticket row is the reason approve/reject races are still detected. `RejectTicket`'s loop over `remainingPending` is inside the same `SaveChanges`, so a concurrent approval of another row is caught by the ticket's token.

The applock makes allocation serial for cooperating writers. The design correctly says it does not make non-cooperating writers safe, and Y-11 asks for the read that would show whether any exist.

### 12.4 Security — naming improved, one gap unowned

Confirmed: no `AntiForgery`, `RequestVerificationToken` or `ViewStateUserKey` reference under `AnmolDristi/ITSM`. Revision 2 improves the security analysis materially by separating the text sink from the attribute sink at `TicketDetail.aspx` lines 383–386 and by requiring a whitelist rather than a "lowercase and hyphen" transform, which was the first review's C-10 and is correct.

Outstanding: AR-9 has no owner and no date (Z-4). ViewState MAC remains unread, so the residual cannot be sized in this review either. Tenant isolation is unchanged and correct in scope: `RequireRecordedTenant` refuses a null `CompanyId` on every mutation, and A3's new returns stay after it. Queue company visibility is unchanged, and the description-search disclosure stays a D-1 residual (TC-59).

---

## 13. Environment preconditions

§9 names 17 reads plus one non-read. The set is proportionate and correctly labelled "none of these was run". Four gaps:

1. **Y-2** — no read counts a stored prefix that differs from the normalised prefix only by case. Read 4 counts duplicate groups, which is a different population.
2. **Y-11** — no read looks for other writers of `itsm.Tickets.TicketNumber`.
3. **Z-5** — no read counts existing `CommentText`, audit `Detail` or `ResolutionText` values above the new caps. The owner acknowledgement in §13 is asked for without the size of the affected population.
4. **Y-10** — read 1 records `MST_TicketStatus.IsTerminal`, but the design attaches it only to A3. A2 needs the same row.

Correctly present and correctly specified: the `TicketNumber` duplicate read including case variants and non-printing characters (read 4), the "length is not 10" inventory that is recorded and not rewritten (read 5), the request-type prefix inventory (read 6), the four Gate B specificity counts plus orphan and blank-agent counts (read 7), the `sp_getapplock` grant (read 8), the collation read (read 9), the index-build mode (read 10), the schema-mechanism choice (read 11), the F-09 four-identifier comparison and duplicate `Branch_ID` count (reads 12–13), the permission resolution that reproduces `BasePage.RebuildSessionPermissions` (read 14), the null-`CompanyId` count (read 15), the active approval-configuration count that does not assume the migration's index is deployed (read 16), and the disposable database for fault injection that excludes UAT and LIVE (read 17).

The failure rule is right: a failed or missing read withholds promotion; it is never a reason to enable a branch and observe it refuse.

---

## 14. Explicit implementation authorisation status

**NOT AUTHORISED. NOT READY FOR IMPLEMENTATION.**

- This review authorises no change to any application file, SQL file, migration, index, trigger or configuration.
- No gate is open. Gates A1, A2, A3, B and C are closed, and A1, A2 and B additionally carry blocking corrections (Y-1 through Y-5).
- A3 must not be enabled in order to observe a live status table.
- D-1 through D-18 remain Pending. No recommendation in the business decision register is treated as a ruling.
- The §13 acknowledgements are not recorded, and no cell in that table may be treated as satisfied.
- Every test disposition in §10 of Revision 2 is **NOT RUN**, and this review adds six uncovered cases (TG-1 through TG-6) that are also not run.
- No SQL was executed and no UAT or LIVE connection was opened to produce this review.
- `AnmolDristi/Web.config` was not read.

Revision 2 remains the proposed design that a later authorisation would implement. That authorisation is not this file, and it is not Revision 2.

---

## 15. What this review changed, and the checks it could not run

### 15.1 Files

One file was created: `ITSM_FREEBUFF_REMEDIATION_REVIEW_REV2.md`, in the repository root, untracked.

No other file was created, modified or deleted. `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md`, `ITSM_TECHNICAL_REMEDIATION_DESIGN.md`, `ITSM_SOURCE_TO_FINDING_TRACEABILITY.md`, `ITSM_FREEBUFF_REMEDIATION_REVIEW.md`, `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION.md` and `ITSM_BUSINESS_DECISION_REGISTER.md` are byte-identical to their state at the start of this review. No application, SQL, migration or configuration file was touched.

### 15.2 Checks run

- Source inspection with file-and-line verification of every citation this review relies on. Where Revision 2's line number was checked and matched, the review says so rather than repeating the claim.
- Repository-wide searches: `sp_getapplock`, `ParameterDirection`, `SqlQuery`, `AntiForgery`, `RequestVerificationToken`, `ViewStateUserKey` under `AnmolDristi/ITSM` — no matches, as Revision 2 states; `ExecuteSqlCommand` under `AnmolDristi/ITSM` — one match at `TicketService.cs` line 497; `MST_RequestType` in every `*.sql` — no file.
- Structural checks on Revision 2: 59 test rows with no gap or duplicate; AR-1 through AR-16 present with no gap; X-1 through X-5 present.
- `git status --porcelain` before and after, `git log -1 --oneline`, `wc -l`, and a heading listing of Revision 2.

### 15.3 Checks not run, and therefore not reported as passes

- **No build.** MSBuild was not invoked; the solution was not compiled.
- **No test.** No row of TC-01 through TC-59 was executed, and none of TG-1 through TG-6 exists as a test.
- **No SQL.** No statement was executed. No query was run against any database.
- **No environment read.** No deployed status row, grant, collation, edition, permission or duplicate count was observed. Every `Environment-unread` label above is unread because this review did not read it.
- **No page rendered.** No ITSM page was started or viewed, so nothing in this review should be read as evidence that any encoding change works.
- **`AnmolDristi/Web.config` was not read**, per `AGENTS.md` §3.4, so request-validation mode, `customErrors`, and the ViewState MAC setting remain unknown to this review.
- **No approval of any kind** is implied by the source verification. A confirmed line number is not a signature, a date, or a ruling.
