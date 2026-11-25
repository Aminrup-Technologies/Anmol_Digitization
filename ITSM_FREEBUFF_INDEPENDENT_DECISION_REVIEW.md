# ITSM — FreeBuff Independent Review of the Business Decision Register

**Document type:** Independent review. Not an approval. Not an instruction to implement.
**Date:** 2026-11-24
**Repository:** `Aug26_AILWorks_AmimnrupSupport`
**Branch / HEAD inspected:** `Aug26_AILWorks_AmimnrupSupport` / `f1f4bdb`
**Mode:** Read-only source inspection. No application file was modified. No database object, SQL script, migration or existing document was modified. No SQL was executed. No UAT or LIVE connection was opened. `AnmolDristi/Web.config` was not read. Nothing was staged, committed, pushed, or tagged.

**Decisions reviewed:** D-1 to D-18 of `ITSM_BUSINESS_DECISION_REGISTER.md`, plus the register's Section 3 session contract and Section 4.4 notification scope.
**Status of every decision after this review:** **Pending.** This document records no approval, no owner name, no signature, and no deferral. Section 9 lists questions; it does not answer them on an owner's behalf.

This review is independent in a specific sense: the target specification and the decision register are themselves FreeBuff products, and this pass re-derived every material statement from source. Where the register is right it is confirmed; where it is incomplete or wrong it is contradicted in writing, with the file and method named. Where Cursor's validation is right it is adopted even though it contradicts earlier FreeBuff documents, and where Cursor is wrong it is refuted.

---

## 1. Executive assessment

### 1.1 Short answer

The architectural direction — one ticket foundation plus thin type-specific policy — is sound, and it is the only direction the current schema can reach without a rewrite. The 18 decisions are the right *set* of questions. The register's *analysis* is largely accurate.

The proposed operating model is nevertheless **not implementable safely today**, and not because the business has not answered. Four of the blockers are not business questions at all, and two of the register's own recommended answers create new dead ends that the register does not name:

1. **`D-9` option A, as written, manufactures duplicate ticket numbers.** Splitting CR into a Standard row and a Normal row gives two `MST_RequestType` rows. `GenerateTicketNumber` allocates per `RequestTypeId` but labels by `Prefix`. Two CR rows sharing the prefix `CR` each issue `CR{yy}{mm}0001` in the same month. The register lists D-9 as dependent on D-10 and D-11, not on D-4. This is a new confirmed defect (Section 5, `FB-DEF-03`).
2. **`D-2` option B + `D-6` option A + `D-12` option A together make `Resolved` a graveyard.** The requester becomes the only actor who may close, there is no reopen window, no automatic closure, no administrative override, and `Closed` is terminal. A ticket whose requester never confirms, or has left the company, can never leave `Resolved`. Nothing in the 18 decisions supplies an escape (Section 5, `FB-DEF-04`; Section 7).
3. **`D-7` option B would persist, and then email, a number computed from an unverified key space.** Create passes the *legacy* `MST_UserMaster.BranchId` into `SlaService`, while dispatch receives the resolved `MST_Branch.Id`. `SlaService` looks up `VMS_PlantTimings.Branch_ID` with whatever it is given. Persisting that due date and mailing an escalation to a manager on the strength of it converts an unverified data mismatch into an authoritative-looking wrong control. I therefore disagree with the register's D-7 recommendation and recommend option C for the first build (Section 3, D-7).
4. **Two of the three "technical method" decisions cannot be treated as business questions first.** D-4 is a duplicate-key defect. Waiting for eighteen policy rulings before adding a unique index leaves a known data-integrity hole open for the duration of the governance cycle. I recommend splitting D-4 into a technical control (authorise now) and a business confirmation of the public number format and ceiling (Section 3, D-4; Section 8).

### 1.2 What is genuinely good in the current implementation

These are stated as positives because a review that only lists failures produces a redesign nobody needs.

- The actor identity is always `Session["WORKMAN"]`, never a form field or query string. No browser-supplied identity path was found (`AnmolDristi/ITSM/UI/TicketDetail.aspx.cs` `GetActorCode`, `TicketList.aspx.cs` `BindQueue`, `CreateTicket.aspx.cs` `btnSubmit_Click`). **VERIFIED.**
- Authorization is derived server-side from stored relationships on every mutation, and each mutation reloads the row inside its own transaction before deciding (`TicketUpdateService.LoadForProcess`, `LoadForApprove`, `RequireRecordedTenant`). **VERIFIED.**
- `CanApproveTicket` is genuinely separate from `CanProcessTicket`, and the requester is refused as an approver. The IT Head is refused while a Manager row is pending. **VERIFIED.**
- Audit rows commit in the same transaction as the state change on all six update paths. **VERIFIED.**
- Row-version concurrency is wired on all six update paths. The mechanism is correct; only its runtime behaviour is unexecuted. **VERIFIED IN SOURCE, NOT EXECUTED.**
- `CompanyId` is an immutable snapshot, written at create and never rewritten, and a null snapshot blocks mutation rather than silently defaulting to tenant 0. That is fail-closed and defensible. **VERIFIED.**
- Rejection already demands a non-blank reason before the transaction opens. **VERIFIED.**
- The three-level category hierarchy is re-validated server-side before insert rather than trusted from the browser (`TicketMasterDataService.ValidateHierarchy` called from `CreateTicket.aspx.cs`). **VERIFIED.**

### 1.3 Headline position on each decision

`Agree` = I reach the register's recommended option independently. `Agree +` = same option, with a control the register omits. `Differ` = I recommend a different option or a different blocking order.

| ID | Question (short) | Register | FreeBuff | Basis |
|---|---|---|---|---|
| D-1 | All Open across companies | A | **Agree +** | A, plus three controls the register omits (search predicate, approver work panel, null-company queue) |
| D-2 | Requester resolves / reassigns / reprioritises | B | **Agree +** | B needs a third predicate and an escape from `Resolved` |
| D-3 | Create permission and active employee | B | **Agree** | Verified: page checks no permission; master page forces the link visible |
| D-4 | Ticket-number uniqueness | A | **Differ (order)** | The uniqueness control is technical and should not wait on 18 rulings |
| D-5 | On Hold plus reason, no extra statuses | A | **Agree +** | Reason must not share the single `Detail` field with other reasons |
| D-6 | Reopen window and SLA effect | A | **Agree +** | A alone strands tickets without an admin close |
| D-7 | SLA package in scope | B | **Differ** | C for the first build until the branch-key space is verified |
| D-8 | Absolute mail base URL | A | **Agree** | The current link is a broken `~/` literal, confirmed |
| D-9 | IR off, SR/CR Normal on, CR Standard off | C then A | **Differ (detail)** | A creates prefix collisions unless each request type gets its own prefix |
| D-10 | IT Head only, no CAB | A | **Agree +** | Merged manager/IT-Head row must be recorded, not silently dropped |
| D-11 | Rejection terminal, new ticket to resubmit | A | **Agree +** | Add a rejection notice; today the employee learns nothing |
| D-12 | Automatic closure | A | **Agree** | Coupled to D-2/D-6 graveyard |
| D-13 | CSAT required to close | A | **Agree** | Columns exist with no writer |
| D-14 | Support group or one agent | A | **Agree +** | Blocked on a deterministic mapping rule |
| D-15 | Null `CompanyId` | A | **Agree +** | Frozen rows must leave the operational queue |
| D-16 | Department snapshot | A | **Agree** | B acceptable if labelled as live HR data |
| D-17 | Resolution minimum length | A | **Agree +** | Silent audit truncation already exists today on comments |
| D-18 | Duplicate by text | A | **Agree +** | Text in `ResolutionText` is not durable evidence |

### 1.4 The five facts that make "implement now" unsafe

1. Deployed `MST_TicketStatus.IsTerminal` for `Resolved`, deployed request-type prefixes and flags, and deployed `VMS_PlantTimings.Branch_ID` are all unread. The repository seed says `Resolved` is non-terminal; a deployed terminal flag makes both close and reopen dead before any new code runs. **UNVERIFIED — environmental.**
2. Ticket numbers can collide today, and a one-character or space-padded prefix stalls the sequence at `0001` permanently. **DEFECT.**
3. A rejection reason longer than 500 characters does not produce a validation message; it produces an unhandled page exception, because `TicketUpdateService` rethrows and `TicketDetail` has no `try/catch`. **DEFECT — new in this review.**
4. No ITSM mutation carries an anti-forgery token or `ViewStateUserKey`. All six mutations and the approval-configuration screen are same-site POSTs carrying an ambient session cookie. **RISK.**
5. `Resolved` can currently be reopened by the requester or the assignee with no reason and no window, and re-resolving silently overwrites `ResolutionText` and `ResolvedOn` with no audit copy of either. A duplicate reference written into the summary is therefore destroyable evidence. **DEFECT — new in this review.**

---

## 2. Review methodology

### 2.1 What was read

**Documents (read in full or in the sections cited):**

- `ITSM_TARGET_OPERATING_WORKFLOW_SPECIFICATION.md` — Sections 1–4 and 9–20.
- `ITSM_BUSINESS_DECISION_REGISTER.md` — full, all 642 lines.
- `ITSM_Cursor_Independent_Validation.md` — full.
- `ITSM_DOCUMENTATION_CONSOLIDATION_REVIEW.md` — Sections 1–4.
- `ITSM_STAGE_2_INDEPENDENT_REVIEW_PLAN.md` — Parts A and B opening.
- `ITSM_AS_IS_Security.md` — Sections 1–3.
- Relevant extracts of `ITSM_AS_IS_Findings_and_Risks.md` used only to check the identifier crosswalk.
- `AGENTS.md` of this repository (read before any edit).

**Source, read directly in this pass:**

| Area | Files |
|---|---|
| Authorization | `AnmolDristi/ITSM/BLL/ItsmTicketAccess.cs`, `TicketDetailService.cs` |
| Workflow | `AnmolDristi/ITSM/BLL/TicketWorkflow.cs`, `TicketUpdateService.cs` |
| Create | `AnmolDristi/ITSM/BLL/TicketService.cs`, `DispatchService.cs`, `SlaService.cs` |
| Queues | `AnmolDristi/ITSM/BLL/TicketListService.cs`, `ItsmTenantContext.cs` |
| Config | `AnmolDristi/ITSM/BLL/ApprovalConfigurationService.cs` |
| Schema | `AnmolDristi/ITSM/DAL/Entities/Ticket.cs`, `RequestType.cs`, `TicketApproval.cs`, `TicketComment.cs`, `TicketAuditLog.cs`; `Migrations/Configuration.cs`; `Migrations/202608120550515_InitialItsmSchema.cs`; `202608201200000_AddTicketAuditLog.cs` |
| UI | `AnmolDristi/ITSM/UI/CreateTicket.aspx.cs`, `TicketList.aspx.cs`, `TicketDetail.aspx.cs`, `TicketDetail.aspx`, `ApprovalConfig.aspx.cs` |
| Session | `AnmolDristi/BasePage.cs`, `AnmolDristi/login.aspx.cs` (sign-in assignment block), `AnmolDristi/Dristi.Master.cs` `RefreshChromeAfterIdentityChange` |

### 2.2 Evidence labels

| Label | Meaning in this document |
|---|---|
| **VERIFIED** | The statement was read in the current source (or a migration) during this pass. |
| **PARTIALLY VERIFIED** | The code fact was read; a deployed row, grant, seed population, or runtime result was not. |
| **UNVERIFIED** | No source or catalogue evidence in this pass can settle it. |
| **DEFECT** | The code contradicts a rule the repository itself implements, or a documented behaviour demonstrably fails. |
| **RISK** | An exposure or integrity gap visible in source, without a demonstrated bad row or reproduced exploit. |
| **RECOMMENDATION** | Advice. Not an existing failure, not an approval. |

A missing control is **not** called a defect. It is a risk or a decision. That discipline matters here: several documents in the pack label absent features as defects, which inflates the gap list and makes the genuine duplicate-key and exception-path failures harder to see.

### 2.3 What was not done

- No SQL of any kind. No `SELECT`, no count of duplicate numbers, no null-`CompanyId` count, no reading of `MST_RequestType`, `MST_TicketStatus`, `MST_SupportMapping`, `MST_PriorityMatrix`, or `VMS_PlantTimings`.
- No UAT or LIVE connection. No deployment. No IIS or browser pass.
- `AnmolDristi/Web.config` was not opened, per `AGENTS.md` §3.4. No environment address, connection string, or secret is reproduced here.
- No file in this repository was modified except the creation of this document. No staging, commit, push or tag.
- No owner approval was recorded, inferred, or drafted.

### 2.4 How the competing documents were treated

Three bodies of work bear on this review, and they do not fully agree with each other:

1. The **AS-IS pack** (six documents, 2026-11-12) — broad, but wrong about the status seed, and overstates the consequence of cross-company listing.
2. The **Cursor independent validation** (2026-11-12) and the **Cursor Stage 2 plan** — correct on the seed, correct on the relationship view path, correct on SLA duration; but the Stage 2 plan contradicts the validation on two source facts (Section 4).
3. The **FreeBuff target specification and decision register** (2026-11-23/24) — the best synthesis of the three, and the object of this review.

I treated all three as claims. Where a claim was checkable in source, it was checked and is marked VERIFIED or refuted. Where it was not checkable without a database, it is marked UNVERIFIED and is not used to justify a recommendation.

### 2.5 Independence statement

This review does not accept the register's framing where the code says otherwise. Three examples, developed later:

- The register presents D-4 as a business decision that blocks the first build. The duplicate-key exposure is a defect under the repository's own stated intent for a human-readable ticket reference, and its control does not require a policy answer.
- The register recommends D-7 option B. That option depends on a branch identifier whose key space is unverified, and it adds an outbound escalation mail. Persisting and mailing an unverified number is worse than leaving the clock in the email.
- The register does not connect D-9 to D-4. Its own recommended answer to D-9 is the mechanism that produces the collision.


---

## 3. The 18 decisions, independently assessed

Each decision keeps its own question and is not combined with another. Every row ends at **Pending**; no ruling is implied.

### D-1 — May All Open show other companies' titles and names?

| Field | Finding |
|---|---|
| Exact business question | Does one shared service desk take work from every company, with a single queue that shows each company's ticket titles, requester names and assignee names to anyone holding the queue permission? Or is queue visibility a tenant boundary? |
| Existing behaviour | **VERIFIED.** `TicketList.aspx.cs` `IsTabAuthorized` requires `ITSM.Queue.View` for `AllOpen` and `MyQueue`; `BindQueue` routes `AllOpen` to `TicketListService.GetOpenQueue`, which sets `OpenOnly` and calls `BuildQueueQuery`. `BuildQueueQuery` applies no `CompanyId` predicate. The grid projects ticket number, title, type, status, priority, requester name, assignee name, category, branch and created time. Search matches `Title`, `TicketNumber` **and `Description`**, which the grid does not display. `GetActiveAgents` and `GetBranches` are global, unfiltered reads. Detail is a different rule: `ItsmTicketAccess.CanViewTicket` permits requester, assignee or pending approver with no company comparison, and permits `ITSM.Ticket.View` only when the ticket snapshot equals the actor's live `MST_UserMaster.CompanyId` (`ItsmTenantContext.ResolveCompanyId`). |
| Cursor's position, challenged | Cursor's validation says the FreeBuff finding "a listed ticket cannot be opened" is broader than the method, and is right: every My Queue row is openable as assignee, every approval row as pending approver, every My Tickets row as requester. Cursor's own decision list then poses D-3 as a yes/no on cross-company titles. That framing is too narrow. The real question is not only *what the grid shows* but *what the search can be made to reveal*. A viewer can confirm whether an arbitrary phrase appears in another company's unpublished description text, one query at a time. That is a disclosure channel independent of the grid columns, and no option in the register removes it. |
| Alternatives the register omits | (a) Keep All Open cross-company but delete `Description` from the search predicate — a pure security fix that does not decide tenant scope. (b) Keep All Open, but bind the grid's requester and assignee columns to codes rather than names for cross-company rows. (c) Scope reassignment targets to the actor's company while leaving the list open — closing the "see everything, then move it" path without a queue redesign. |
| Operational implications | Branch-and-category dispatch (`MST_SupportMapping`) has no company column, so company-scoped queues would hide tickets the desk was legitimately given. Narrowing the agent directory or the reassignment target list would reduce the desk's ability to move work between companies, which is the current operating assumption. Restricting the list also hides null-company tickets, which is D-15. |
| Security and audit implications | **RISK (High).** Holders of `ITSM.Queue.View` read cross-company titles, employee names, branches and categories, and can probe description text. Whether the grants exist is UNVERIFIED. No audit row is written for a queue read or a search, so the disclosure is not reconstructable after the fact. |
| FreeBuff recommendation | **Option A as the register recommends, plus the three controls in the alternatives.** Accepting a shared desk does not require accepting a description search oracle. If leadership has any tenant boundary that survives contact with a shared desk, option C is the honest answer and option B is the worst answer, because it hides the list while leaving cross-company reassignment and cross-company search intact. |
| Approving role | Service owner, with IT security concurrence on the disclosure. |
| Status | **Pending** |

### D-2 — May the requester resolve, reassign, or override priority?

| Field | Finding |
|---|---|
| Exact business question | Which actor may move a ticket through its process — resolve, hold, cancel, close, reopen, reassign, reprioritise, comment — and does the employee who raised it keep any of those rights after an assignee exists? |
| Existing behaviour | **VERIFIED.** `ItsmTicketAccess.CanProcessTicket` is `requester OR assignee`, with no permission key, no support-mapping check, and no company check. `TicketUpdateService` applies it in `ChangeStatus`, `ReassignTicket`, `OverridePriority`, and `AddComment`. The requester therefore can resolve, close, cancel, place on hold, and move `Resolved` back to `In Progress`; can reassign to any active employee in any company (`ReassignTicket` validates `EmployeeStatus` only); and can override priority with a reason they write. Rights survive reassignment because the predicate is "requester or assignee", not "current assignee". `TicketDetail.aspx.cs` `LoadTicket` shows `pnlWorkActions` whenever the ticket is non-terminal and not `Pending Approval`, with no `CanProcessTicket` test, so the requester's manager viewing through `ITSM.Ticket.View` also sees the panel and the full agent directory. |
| Cursor's position, challenged | Cursor's D-4 is "may the requester resolve, close, reassign, and override priority?" and its evidence is correct. Its D-5 ("who may process a ticket they do not own?") is not a separate question — it is the negative half of D-2 and must be ruled with it, which the register correctly does. Where the register and Cursor both fall short: neither notes that changing the gate requires *three* predicates, not one. Close and reopen are not process actions in the target; they are confirmation actions. Splitting them means `CanProcessTicket`, `CanConfirmResolution` and `CanReassign` diverge, and `pnlWorkActions` must stop being a single panel. |
| Alternatives the register omits | (a) Assignee-only processing, with the requester retaining cancel-before-work and confirmation — this is B — plus an explicit `ITSM.Ticket.Admin`-equivalent override for a stranded ticket. (b) Keep the requester's rights but require a permission key for reassignment targets outside their own company. (c) Assignee-only processing *and* assignee-or-requester close, which keeps the employee able to confirm without letting them resolve. |
| Operational implications | Removing the requester is the single largest behaviour change in the programme. Every unassigned ticket loses its only actor, because dispatch returns `null` when no mapping matches. Employees who currently self-resolve will file duplicate tickets instead. The change needs a communication plan, not only a deployment plan. |
| Security and audit implications | **RISK (High, workflow integrity).** Today the requester can close work the assignee has not finished, and move it to any active employee in any company. Separation of duties holds for approve/reject and does not hold for resolve/reassign/priority. The audit row for reassignment records old and new assignee but no reason; `OverridePriority` records the reason the requester wrote; `ChangeStatus` records the status names and a null detail. |
| FreeBuff recommendation | **Option B, with two additions.** First, define the confirmation predicate separately and name its actor for close *and* reopen in the same ruling. Second, add an administrative closure path, or the model strands tickets permanently (see `FB-DEF-04`). Do not implement B before D-14 supplies a taker for unassigned work. |
| Approving role | Service owner, with IT security concurrence. |
| Status | **Pending** |

### D-3 — Must create require `UI.ITSM_CreateTicket.View` and an active employee?

| Field | Finding |
|---|---|
| Exact business question | Who may raise a ticket: every employee who can sign in, or only employees granted an explicit permission? And must the employee still be active at the moment of creation? |
| Existing behaviour | **VERIFIED.** `CreateTicket.aspx.cs` checks only that `WORKMAN`, `USERID` and `BRANCH` are non-null. It calls neither `HasPermission` nor `RequirePermission`, and does not call `ItsmTenantContext.IsActiveEmployee`. `TicketService.CreateTicket` requires a user-master row, a positive `CompanyId`, and a branch that resolves by `MST_Branch.Branch_ID`; it never reads `EmployeeStatus`. Sign-in refuses a non-Active employee, so the gap is a session that outlives deactivation. `Dristi.Master.cs` `RefreshChromeAfterIdentityChange` (247–267) runs `PermissionHelper.ApplyUiPermissions` and then forces `ITSM_Module`, `ITSM_TicketList` and `ITSM_CreateTicket` to `Visible = true`. |
| Cursor's position, challenged | Cursor's D-11 asks the same question and its evidence is accurate. The register's option B is right. Two corrections. First, the master-page override covers **three** controls, not one, so enabling the permission hides nothing on the sidebar while the page begins refusing — and the queue link stays advertised to users who cannot use it. Second, a page-level `IsActiveEmployee` check leaves a check-then-act window; putting it inside `CreateTicket` closes it. |
| Alternatives omitted | (a) Active-employee check in `TicketService`, plus a *new* key restricting only high-impact types such as CR. (b) Remove the master-page override so the `UI.*.View` keys actually govern the sidebar, then decide the create key. |
| Operational implications | Enabling the create permission before grants exist stops ticket creation for the workforce the service exists to serve. That is an outage, not a control. |
| Security and audit implications | **RISK (Medium).** Create is an authentication check, not an authorisation check. A still-valid session of a deactivated employee can insert a ticket. The permission key exists in SQL and is never consulted. |
| FreeBuff recommendation | **Option B.** Add the active-employee check inside `TicketService.CreateTicket`; leave the permission unused until the roles that must retain creation are named and granted; fix the master-page override in the same change so menu and page agree. |
| Approving role | Service owner for who may raise a ticket; IT security for when the check is enforced. |
| Status | **Pending** |

### D-4 — How should duplicate ticket numbers be prevented?

| Field | Finding |
|---|---|
| Exact business question | Two questions sharing one identifier. *Business:* must the human-readable number keep its `prefix + yy + MM + 4 digits` shape, and what monthly volume must it carry? *Technical:* how is uniqueness enforced under concurrency? |
| Existing behaviour | **VERIFIED, and a DEFECT.** `TicketService.GenerateTicketNumber` reads the maximum for `RequestTypeId == requestType.Id` **and** `TicketNumber.StartsWith(prefix + yy + MM)`, parses the last four characters only when the string is at least 10 characters, adds one, and formats `D4`. There is no lock hint, no sequence object, no retry, and no unique index: `202608120550515_InitialItsmSchema.cs` line 78 declares `varchar(10)`, indexed on foreign keys only. `RequestType.Prefix` is `[StringLength(2)]` on `char`, so a single-character value stored space-padded trims to one character; the number becomes nine characters, the parser's length guard skips it, and every later create for that type returns `0001`. At 10000, `D4` emits five digits, which neither the column nor the entity's `[StringLength(10)]` accepts. Because the read filters on `RequestTypeId`, two request types sharing a prefix cannot see each other's rows (mechanism of `FB-DEF-03`). |
| Cursor's position, challenged | Cursor's FR-01 is accurate on all four mechanisms. Its Stage 2 plan contradicts itself: Part A says transaction-owned `sp_getapplock` is used, Part B says it is not. The source has no `sp_getapplock`; Part A would mislead an implementer into believing the race is solved. Cursor's "unique index not applied" is true but understates it: no migration defines one, so there is nothing pending to apply. |
| Alternatives omitted | (a) Add the unique index **now**, with no generator change, and let concurrent creates fail visibly instead of duplicating silently — a smaller, reversible first step. (b) A per-prefix-per-month counter row updated in the same transaction. (c) One SQL Server `SEQUENCE` per prefix. |
| Operational implications | Option A keeps the number employees quote. The 9999-per-prefix-per-month ceiling must be stated in the ruling, not discovered at month end. Once the index lands, a collision becomes a failed create with a visible error: worse for the employee, better for the data. |
| Security and audit implications | **RISK (Medium, audit integrity).** A duplicate weakens every reference that depends on the number: the quoted number after rejection (D-11), the duplicate sentence (D-18), mail subjects. It is not a tenant bypass, and `Tickets.Id` stays unique, so no row is lost — only the human reference becomes ambiguous. |
| FreeBuff recommendation | **Split the decision.** Technical half — unique index, locked allocation, bounded retry, format unchanged — authorised as a defect fix. Business half — confirm the format and accept or raise the ceiling. I disagree with treating all of D-4 as blocked on eighteen policy answers. |
| Approving role | IT leadership for the control; service owner for the displayed format and ceiling. |
| Status | **Pending** |

### D-5 — Is On Hold plus a reason enough, with no extra pending statuses?

| Field | Finding |
|---|---|
| Exact business question | When work pauses, must the actor say *why* (requester, vendor, internal), and is one pause status plus a reason better than a status per wait reason? |
| Existing behaviour | **VERIFIED.** The map allows `New → On Hold`, `In Progress → On Hold`, `On Hold → In Progress`, `On Hold → Cancelled`. `ChangeStatus` requires text only for `Resolved`. The `StatusChange` audit row writes old and new names with `Detail = null`. There is no hold-reason column and no hold-reason field in the write path. The seed marks `On Hold` non-terminal. |
| Cursor's position, challenged | Cursor folds the hold reason into its other answers and never asks where it is stored. The register calls storage shape an implementation choice. Mostly true, but not free: `TicketAuditLog.Detail` already carries the priority-override reason, the rejection reason and the full comment text. A class written as prose into that field cannot be separated from justification by a later reader, and the field is capped at 1000 characters, enforced by truncation in `AddAudit`. |
| Alternatives omitted | (a) A nullable `HoldReasonCode` column (Requester/Vendor/Internal) with the note in `Detail`. (b) A coded prefix inside `Detail` — cheapest, weakest. (c) Derive the class from an internal comment by convention — rejected: it makes the queue filter depend on parsing. |
| Operational implications | One pause status plus a class keeps the map at four keys and yields a trustworthy "waiting on requester" queue. A status per reason multiplies map, badge CSS, dropdown and training for no analytical gain. |
| Security and audit implications | **RISK (Medium, auditability).** A pause with no reason leaves a gap in the record of accountability while the ticket sat. It does not widen visibility. |
| FreeBuff recommendation | **Option A**, with the class in its own coded field rather than free prose, so the queue filter is a predicate. Refuse over-length reason text rather than truncating it on the audit copy. |
| Approving role | Service owner. |
| Status | **Pending** |

### D-6 — Is there a reopen window, and does reopen restart the SLA?

| Field | Finding |
|---|---|
| Exact business question | Can a resolved ticket return to work, by whom, for how long, with what justification, and what happens to the SLA clock when it does? |
| Existing behaviour | **VERIFIED.** The map allows `Resolved → Closed` and `Resolved → In Progress`; `Closed`, `Cancelled`, `Rejected` have no exit. `ChangeStatus` requires no reason for `In Progress`, compares no dates, reads no SLA column. `Resolved` is non-terminal in `Configuration.Seed`, so both edges are reachable if the deployed flag matches. The actor is whoever passes `CanProcessTicket`. `ChangeStatus` does not clear `ResolvedOn` on reopen and overwrites `ResolutionText` on the next resolve, with `Detail` null on both audit rows. |
| Cursor's position, challenged | Cursor's decision list omits a reopen window entirely. The register answers A (any time, mandatory reason, extend the due time, do not reset the start). Coherent, but it leaves the completion signal unstable: with no window, "resolved" never means done. Paired with D-12 option A (no auto-close) and D-2 option B (requester-only close), it produces the permanent-`Resolved` state in `FB-DEF-04`. |
| Alternatives omitted | (a) A window the owner names, after which only `Closed` remains. (b) No window but an administrative close beyond a threshold. (c) Reopen requires a different actor from the one who resolved — a genuine separation-of-duties control nobody has proposed. |
| Operational implications | Without a window, agents cannot rely on `Resolved` as completion, and the proposed "resolved" queue filter accumulates stale rows. With a window, a requester on leave loses the chance to reject a bad resolution — which is why the length is an owner value, not a default. |
| Security and audit implications | **RISK (Medium, workflow integrity and evidence).** Reopen by either party with no reason rewrites a completed outcome; because the audit detail is null and the ticket row is mutable, the earlier resolution is destroyed rather than superseded. |
| FreeBuff recommendation | **Option A plus an escape hatch plus a history fix.** Mandatory reason, no invented day count, do not reset the start, and either a named window or an administrative close. Independently of the business answer, stop overwriting the previous resolution silently: write the summary and prior `ResolvedOn` into the audit `Detail` on every resolve and reopen. |
| Approving role | Service owner. |
| Status | **Pending** |

### D-7 — Are response SLA, stored due time, pause on hold, and one breach mail in scope?

| Field | Finding |
|---|---|
| Exact business question | Does the first build carry a persisted resolution clock (start and due), a pause on hold, a breach escalation mail, and a separate response target — or only some? |
| Existing behaviour | **VERIFIED.** `MST_Priority.SlaResolutionHours` exists; `SlaService.CalculateSla` runs at create. `GetPlantTiming` matches `t.Branch_ID == branchId`; `AddBusinessHoursSkippingWeekends` consumes clock hours to midnight, skipping calendar weekends or Saturday/Sunday. `Tickets` has no SLA columns in any migration. The timestamps reach only `EnqueueTicketCreatedNotification`. `TicketService.CreateTicket` passes legacy `requester.BranchId` into `CalculateSla` and resolved `branch.Id` into `DispatchService`. No job computes a breach. |
| Cursor's position, challenged | Cursor's D-7 correctly reports non-persistence and the duration narrowing, but does not connect the branch-key mismatch to the consequence of persisting. The register recommends B. I disagree on evidence: persisting a due date whose branch key space is unverified, then mailing a manager when it breaches, creates an authoritative wrong control. A wrong number in an email is a rumour; a wrong number in a column plus an escalation is a governance artefact. |
| Alternatives omitted | (a) Persist start and due for reporting only, no pause, no mail, until the key space is verified. (b) Persist and pause, but mail the assignee only, not a manager across a company boundary. (c) Leave SLA out of the first build and spend the effort on actor and tenant decisions. |
| Operational implications | A stored clock enables the "at risk" and "breached" filters the queue design assumes. Pause without a hold reason is not implementable. Escalation changes a manager's workload and discloses ticket detail to a person who may hold no ITSM permission at all. |
| Security and audit implications | **RISK (Medium).** Escalation discloses the ticket to the assignee's reporting manager regardless of company, permission or role. Mail must not carry a relative link (D-8). Recalculation on priority override must audit old and new due dates. |
| FreeBuff recommendation | **Option C for the first build** — persist start and due, no pause, no mail — then B after a read-only verification that `VMS_PlantTimings.Branch_ID` holds the key create passes. I differ from the register: storing a number nobody acts on is safe; mailing a wrong number to a manager is not. |
| Approving role | Service owner for clock rules; IT leadership for scope; IT operations for the key verification. |
| Status | **Pending** |

### D-8 — What absolute base URL should mail links use?

| Field | Finding |
|---|---|
| Exact business question | Which organisation-operated origin may appear in ticket mail, and does the first build add the proposed notification events or only fix the link on the existing creation mail? |
| Existing behaviour | **VERIFIED, and a DEFECT.** `EnqueueTicketCreatedNotification` builds `var ticketLink = "~/ITSM/UI/TicketDetail.aspx?id=" + ticket.Id` into an `href`. No ITSM class reads a configured origin; no `Request.Url` is used. The browser redirect in `CreateTicket.aspx.cs` uses `ResolveUrl`, so the page path is right while the mail path is not. `Title` is HTML-encoded; the link is not, correctly, because it is a literal plus an integer. |
| Cursor's position, challenged | Cursor's FR-18 confirms the literal and the `ResolveUrl` contrast but does not classify it. The register calls the link "not clickable", which understates it: `~` is not a valid scheme, so most clients render a broken relative target. It is a defect in the stored message, not a cosmetic gap. |
| Alternatives omitted | (a) A per-environment configuration key with no default, read by the BLL. (b) One absolute URL composed at send time from a host allow-list. (c) Quote the number only until notification scope is decided. |
| Operational implications | Several host names exist in practice. One origin per environment, supplied by operations; more than one simultaneous origin produces mail that works for some recipients and not others. |
| Security and audit implications | **RISK (Medium).** The origin decides which site receives the employee's session on click. It must be organisation-controlled HTTPS, must not come from a committed local configuration file, and must not be a session key. The numeric id in the link is acceptable because detail authorises independently — which is exactly why that authorization must stay correct. |
| FreeBuff recommendation | **Option A.** Operations writes the origin into the ruling; the creation mail gains the absolute prefix in the same change. The Section 14 recipient list is a separate scope question the same ruling should mark accepted, reduced or deferred. |
| Approving role | IT operations for the origin; service owner for Section 14 scope. |
| Status | **Pending** |

### D-9 — IR approval off, SR and CR Normal on, CR Standard off?

| Field | Finding |
|---|---|
| Exact business question | Which request types require manager approval, which require IT Head approval, and how are Standard, Normal and Emergency changes represented when the schema offers only two booleans on `MST_RequestType`? |
| Existing behaviour | **VERIFIED mechanism, UNVERIFIED data.** Create reads both flags and lands on `Pending Approval` when either is true, else `New`. Manager is `ReportingManagerCode`; IT Head is the single active `MST_ApprovalConfiguration` row. `UpdateRequestTypeApproval` flips flags and writes no audit row. No request-type seed exists, so deployed codes, prefixes and flags are unknown. There is no change-class column. |
| Cursor's position, challenged | Cursor confirms the mechanism and omits the policy from its decision list. The register's C-then-A instinct is right. But its option A ("two change rows, standard off, normal both on") has a consequence neither document identifies: **two rows with the same prefix collide**, because allocation filters on `RequestTypeId` and labels with `Prefix`. Both rows issue `0001` in the same month. Option A therefore *causes* the D-4 defect while looking like a policy change (`FB-DEF-03`). |
| Alternatives omitted | (a) Two change types with **distinct prefixes** (`CS`, `CN`) — avoids the collision and puts the class in the number, at the cost of a format change. (b) One CR type with both flags on, and Standard changes raised as an SR type. (c) A `ChangeClass` column — a schema change the first build should avoid. |
| Operational implications | The policy is already editable data; approving it changes nothing until an administrator applies it. Standard changes starting without approval is the intended efficiency gain and the largest control reduction in the model. Emergency cannot express "implement first, approve after", because flags carry no order. |
| Security and audit implications | **RISK (Medium).** A wrong flag lets change work start with no approver, or blocks it behind an unintended one. Flag changes are unaudited, so a disputed approval path has no configuration history. The IT Head is global across companies (D-10). |
| FreeBuff recommendation | **Option C then A, with distinct prefixes and audited flag changes.** Read the deployed rows in a named environment first. If only one change type exists, rule B until a second row with its own prefix is approved. Audit every flag and IT Head change. |
| Approving role | Service owner for policy; administrator applies the data; IT leadership commissions the read-only check. |
| Status | **Pending** |

### D-10 — Is the IT Head the only change authority, with no CAB?

| Field | Finding |
|---|---|
| Exact business question | Is a single named IT Head sufficient authority for change approval, with no board, quorum or scheduled window — and what happens when that single person is the requester? |
| Existing behaviour | **VERIFIED.** Change uses the same ticket and the same two flags. There is no CAB table, no membership, no planned-start column. One active IT Head is enforced by a filtered unique index on `MST_ApprovalConfiguration`. `ApproveTicket` refuses the IT Head while a Manager row is pending, and refuses the requester. Create throws when the requester's reporting manager is the requester. Create does **not** compare the requester with the active IT Head: it inserts the row, and `ApproveTicket` then refuses that person, leaving the ticket in `Pending Approval`, which has no map exit and no administrative override. When the manager and the IT Head are the same employee, two approval rows are inserted for the same person, and `ApproveTicket` takes the first pending row by `FirstOrDefault`. `SetActiveItHead` and `UpdateRequestTypeApproval` write no workflow audit. |
| Cursor's position, challenged | Cursor's D-12 confirms the single global IT Head and the filtered unique index. Its D-1 asks whether the *repository seed* is the rule for `Resolved` — a misframing, because a seed is not a rule and the deployed row is a fact to be read, not decided. On D-10 the register is right. Neither document notes that dropping the duplicate IT Head row would erase the fact that the IT Head level was satisfied. |
| Alternatives omitted | (a) Keep both rows but have `ApproveTicket` mark both levels in one action and audit "Manager+ITHead". (b) Refuse such a request type entirely when manager and IT Head resolve to the same employee, and force an administrator to nominate a different IT Head. (c) A delegate field on the configuration row for absence — explicitly out of the first build. |
| Operational implications | One named person is a single point of failure for all change approval. Absence is handled today by an administrator switching the configuration, which is unaudited. A ticket whose requester is that person is unrecoverable in the application. |
| Security and audit implications | **RISK (High, authorization).** One global approval identity can approve change for every company; the authority is not limited by `Tickets.CompanyId`. Changing that identity is a privileged action with no ITSM audit row. Stranded `Pending Approval` tickets have no bypass. |
| FreeBuff recommendation | **Option A**, plus two additions: record the merged approval as a single audited action naming both levels rather than silently discarding the IT Head row, and audit every change to the active IT Head and to either request-type flag with actor, old value and new value. |
| Approving role | Service owner, with IT security concurrence on cross-company authority. |
| Status | **Pending** |

### D-11 — Is rejection terminal, with a new ticket as the only resubmit?

| Field | Finding |
|---|---|
| Exact business question | When an approver rejects a request, can the employee correct and resubmit the same ticket, or must they raise a new one? |
| Existing behaviour | **VERIFIED.** `RejectTicket` requires a non-blank reason, marks the acting row and every other pending row `Rejected`, and moves the ticket to `Rejected`, falling back to `Cancelled`. The seed marks both terminal. `Rejected` is not a key in the transition map, so no screen path returns to `Pending Approval`. There is no resubmit method and no edit of title, description or category after create. The reason is stored on `TicketApprovals.Comments`, which is `nvarchar(500)`, and on the audit detail. No rejection mail is sent. |
| Cursor's position, challenged | Cursor's WF-13 and AV-05 are both accurate: rejection marks all pending rows, and the fallback to `Cancelled` exists; the rejection textbox has **no** `MaxLength` (`TicketDetail.aspx` line 318, unlike `txtOverrideReason` at line 296 which has one). Cursor stops at "the transaction rolls back". The consequence is worse: `TicketUpdateService` catches `Exception`, rolls back and rethrows, and `btnReject_Click` has no `try/catch`, so an over-length reason produces an unhandled ASP.NET error page rather than a validation message (`FB-DEF-02`). |
| Alternatives omitted | (a) Terminal rejection plus an automatic rejection notification carrying the reason — closes the "employee learns nothing" gap without a workflow change. (b) One controlled re-open of the same ticket for edits, with a new approval cycle and an audit of the edit. (c) A "copy to new ticket" action linking new id to old. |
| Operational implications | A rejected request cannot be corrected in place; the employee retypes it. History of the refusal stays on the old ticket. Employees who expect to fix and resubmit will instead see a dead end and, today, no notification. |
| Security and audit implications | **RISK (Medium).** Terminal rejection prevents self-repair of a refused request, which is a control, not a gap. The over-length failure path is a genuine defect: it exits the transaction with no audit row and no user-facing explanation. |
| FreeBuff recommendation | **Option A for this build**, plus the rejection notification from the alternatives list and a `MaxLength` or server-side length check that refuses with a message instead of throwing. Quote the old number only after D-4. |
| Approving role | Service owner. |
| Status | **Pending** |

### D-12 — Is automatic closure authorised?

| Field | Finding |
|---|---|
| Exact business question | May the system close a ticket by age, without a person acting? |
| Existing behaviour | **VERIFIED absence.** `Ticket.ClosedOn` carries a comment about closure "after CSAT or 72-hour auto-close", but the only writer is `ChangeStatus` when a person targets `Closed`. No ITSM job, scheduler, or service closes by age. |
| Cursor's position, challenged | Cursor's D-8 and FR-12 correctly identify the absence and the comment. The register answers A. I agree, but the register does not connect it to D-2 and D-6: with requester-only close and no window, the absence of auto-close converts `Resolved` into a permanent resting state (`FB-DEF-04`). Refusing auto-close is correct in isolation and unsafe in combination. |
| Alternatives omitted | (a) No job now, but an administrative close permission so a person can clear a stranded ticket. (b) A job with a named system actor and an exception queue. |
| Operational implications | Resolved tickets accumulate. Any later 72-hour job will close tickets the requester never saw, because no resolve mail exists today. |
| Security and audit implications | **RISK (Low today).** A future job without a named actor would change state with no accountable human. |
| FreeBuff recommendation | **Option A** — no job — coupled with the administrative closure path recommended under D-2 and D-6. Do not treat the entity comment as a specification. |
| Approving role | Service owner. |
| Status | **Pending** |

### D-13 — Is CSAT required to close?

| Field | Finding |
|---|---|
| Exact business question | Must the requester submit a satisfaction score before a ticket can be closed? |
| Existing behaviour | **VERIFIED.** `CsatScore` and `CsatFeedback` exist, are projected by `TicketDetailService` and displayed when a score is present. No method under `AnmolDristi/ITSM` assigns either property. `ChangeStatus` does not read the score. |
| Cursor's position, challenged | Cursor's FR-12 and X3 agree. No disagreement exists on the fact; the only difference is that Cursor folds CSAT into a combined "attachments and CSAT" decision, which obscures that CSAT has a closure consequence and attachments do not. The register correctly separates them. |
| Alternatives omitted | (a) Optional score, closure never blocked. (b) Mandatory score with a defined default and an expiry. |
| Operational implications | Requiring a score needs a form that does not exist and will block closure whenever the requester does not answer. Leaving the columns unused preserves them for a later decision at no cost. |
| Security and audit implications | None for access control. A mandatory score adds a requester action before `Closed` and would compete with D-6's confirmation model. |
| FreeBuff recommendation | **Option A.** Leave the columns unused; do not block closure. If leadership later wants satisfaction data, option (a) — optional, never blocking — is the safe increment, and it still needs a writer that does not exist. |
| Approving role | Service owner. |
| Status | **Pending** |

### D-14 — Must a support-group entity exist, or is branch + category + one agent enough?

| Field | Finding |
|---|---|
| Exact business question | Is one named employee per branch-and-taxonomy cell an adequate accountable owner, or does the business need a group that several agents share — and, if it stays one employee, who may take an unassigned ticket? |
| Existing behaviour | **VERIFIED.** `MST_SupportMapping` stores one `SupportAgentCode` per branch with optional category, subcategory and item. `DispatchService.ResolveAssignee` walks item, subcategory, category then branch default, each with `FirstOrDefault` and no `OrderBy`. A null assignee is allowed. There is no group table and no team-lead role. `ReassignTicket` does not consult the mapping. There is no maintenance screen; mappings are a script. Multiple active rows at the same specificity are possible because no unique index exists on the natural key. |
| Cursor's position, challenged | Cursor's WF-03 and DB-06/DB-07 confirm the cascade, the single agent code, and the absence of a natural-key unique index. Its D-5/D-6 treat this as one "agent hierarchy" question. The register separates the entity question from the take-ownership rule, which is the better cut. What both miss: an authorization rule of the form "the mapped agent may take an unassigned ticket" is only well-defined if "the mapped agent" is deterministic. Today it is not — two equal rows return whichever the engine reads first. |
| Alternatives omitted | (a) One mapped agent plus an explicit take-ownership permission (`ITSM.Ticket.Take`) granted to a role, instead of deriving authority from a mapping row. (b) One mapped agent, but with a primary/secondary pair on the mapping row. (c) A group entity — a new table plus a maintenance screen. |
| Operational implications | One person is accountable per cell; a shared queue where any peer may own the work is not representable. Unassigned tickets, which dispatch produces whenever no row matches, have no owner at all, and D-2 removes the only actor that exists today. |
| Security and audit implications | **RISK (Medium).** Dispatch and reassignment are not company-scoped, so the single agent can sit in a different company from the requester. Mapping rows change by script, outside the ITSM audit trail, so an authority change leaves no trace. |
| FreeBuff recommendation | **Option A**, conditional on making the mapping deterministic first: a unique index on the natural key, or an explicit ordering column. Without that, "the mapped agent" is not a predicate that can be enforced, and D-2 cannot be implemented safely. Option C waits for a demonstrated need and is out of this build. |
| Approving role | Service owner. |
| Status | **Pending** |

### D-15 — What happens to tickets whose `CompanyId` is null?

| Field | Finding |
|---|---|
| Exact business question | Do historical tickets with no recorded company stay frozen, get remediated from evidence, or get stamped from current HR data? |
| Existing behaviour | **VERIFIED.** `CompanyId` is nullable. `202608210700000_AddTicketCompanyId` adds it and explicitly does not backfill. Create refuses a requester with no positive company. All six mutations call `RequireRecordedTenant`, which returns an error for a null snapshot — including `AddComment`. View by requester, assignee or pending approver still succeeds; the `ITSM.Ticket.View` path does not, because it requires a company match. All Open and My Queue still list such tickets while they are non-terminal. Audit insert uses `CompanyId.Value` and is unreachable for them because the mutation is refused first. No count was queried. |
| Cursor's position, challenged | Cursor's Stage 1 called the freeze a critical denial-of-service defect; its later validation and the register call it fail-closed. The latter is correct as an assessment: the behaviour protects the tenant. But both miss the operational half. Because `AddComment` is also blocked, support cannot even annotate the history, and because the queue still lists the rows, the desk sees work it can never clear. |
| Alternatives omitted | (a) Frozen, not backfilled, and **excluded from the operational queues** with a dedicated read-only list. (b) Evidenced remediation only, from evidence that predates the ticket, as a separately authorised data exercise. (c) Backfill from current HR company — rejected by both the migration comment and the entity comment. |
| Operational implications | These tickets never leave the queue and never progress. Without (a) they will be re-reported by employees and re-created by the desk, producing duplicates that D-18 cannot catch. |
| Security and audit implications | **RISK (High if backfilled).** A backfill would make the ticket mutable under a company that was not evidenced at create time — the one thing the snapshot exists to prevent. Leaving them listed continues the D-1 disclosure for rows with no tenant badge. |
| FreeBuff recommendation | **Option A**, with the queue exclusion from the alternatives. Option B stays available as its own authorised data exercise, outside the application build. Option C is rejected. |
| Approving role | IT leadership and the data owner. An implementer does not make this ruling. |
| Status | **Pending** |

### D-16 — Is a department snapshot required on the ticket?

| Field | Finding |
|---|---|
| Exact business question | Must the ticket record the requester's department at creation so history does not change, or is no department needed on the ticket at all? |
| Existing behaviour | **VERIFIED.** `MST_UserMaster.DepartmentId` exists. `Tickets` has no department column and no list or detail projection selects one. The sign-in path copies `DepartmentId` into `Session["U_SITE"]`; ITSM never reads it. |
| Cursor's position, challenged | Cursor does not raise the question, and its inventory lists department only as a user field. The register recommends A (no snapshot and no department on screen), differing from the specification's "show the current department". I agree with the register's reasoning: live HR data on a historical ticket reads as history. |
| Alternatives omitted | (a) No department. (b) Current department displayed and explicitly labelled as current HR data. (c) A snapshot column. |
| Operational implications | Reports cannot say which department raised a ticket. Option B costs nothing and answers most of the reporting need honestly; option C is the only one that survives an HR transfer, at the cost of a column the first build avoids. |
| Security and audit implications | **Low.** Department is not the tenant; `CompanyId` is. Using department as an access boundary would be a new authorization model and is not proposed. |
| FreeBuff recommendation | **Option A for this build.** Option B is acceptable if the label states that the value is live HR data. Option C waits for a report owner to require frozen history. |
| Approving role | Service owner; a later snapshot also needs the HR data owner. |
| Status | **Pending** |

### D-17 — Minimum length of a resolution summary?

| Field | Finding |
|---|---|
| Exact business question | Is "non-blank" an adequate definition of a resolution, or must the summary meet a minimum length or content rule? |
| Existing behaviour | **VERIFIED.** `ChangeStatus` refuses `Resolved` when `ResolutionText` is null or whitespace, trims and stores it, and sets `ResolvedOn`. The status audit row passes `null` as detail, so the summary is not copied into history. `ResolutionText` is unbounded; the UI textbox has no `MaxLength`. `AddAudit` truncates `Detail` to 1000 characters. |
| Cursor's position, challenged | Cursor's FR-10 confirms that the summary is stored only on the ticket and the audit detail is null. The register adds the right observation that a future audit copy must refuse over-length text rather than truncate. Neither notes that silent truncation **already exists today** on comments: `txtComment` allows 2000 characters (`TicketDetail.aspx` line 339), `AddComment` writes the full text to `TicketComments`, and `AddAudit` cuts the audit copy at 1000 with no error (`FB-DEF-05`). |
| Alternatives omitted | (a) Non-blank, no minimum, audit copy refuses over-length. (b) A minimum the owner names. (c) A structured field set (what was done / how verified) instead of free text. |
| Operational implications | "Resolved" can be one character. A long minimum rejects genuine short outcomes. Whatever is chosen, the summary needs to reach the audit row or the audit trail cannot evidence the outcome after a reopen. |
| Security and audit implications | **RISK (Medium, auditability) and DEFECT (silent truncation, on the existing comment path).** The business fact of the resolution lives on a mutable ticket row, not on the append-only-intended audit row. |
| FreeBuff recommendation | **Option A** — non-blank after trim, no invented minimum, and when the summary is copied to the audit row, refuse text over the column limit instead of cutting it. Fix the existing comment truncation in the same change or state it as accepted. |
| Approving role | Service owner for any minimum; IT leadership for the audit ceiling. |
| Status | **Pending** |

### D-18 — May an agent mark a ticket duplicate without a link column?

| Field | Finding |
|---|---|
| Exact business question | Is a sentence in the resolution summary sufficient to record that a ticket duplicates another, or is a durable relationship required? |
| Existing behaviour | **VERIFIED absence of a link.** `Tickets` has no parent or duplicate foreign key. Nothing validates that a resolution mentions another ticket. An agent who can resolve can type any sentence. The application does not verify that the number exists, is unique, or belongs to a ticket the agent may view. |
| Cursor's position, challenged | Cursor's WF-10/FR-10 confirm there is no relationship and no audit copy of the summary. Neither document notices the durability problem: `ResolutionText` is overwritten on every subsequent resolve, and a reopen followed by a second resolve replaces the duplicate reference with no audit record of the earlier text, because the status audit writes `Detail = null` (`FB-DEF-06`). A duplicate label is therefore editable and erasable evidence. |
| Alternatives omitted | (a) Text in the summary, accepted as weak, with the duplicate number also written into the audit detail so the fact survives a re-resolve. (b) Refuse duplicate marking until a parent column exists. (c) A nullable parent id, validated against a ticket the actor may view, in a later build. |
| Operational implications | Duplicates can be closed with a sentence and no new table, but cannot be listed as a set, and a wrong number is not caught. While D-4 is open, the sentence can point at two rows. |
| Security and audit implications | **RISK (Medium).** The sentence is visible to everyone who can view the resolved ticket; if it names a ticket the writer should not have seen, it becomes a pointer across tenants. A future link column needs the same company rule as D-1. |
| FreeBuff recommendation | **Option A, gated on D-4**, with the duplicate number also recorded on the audit row so it survives a reopen and re-resolve. No link column in this build. The writer must be a process actor under D-2. |
| Approving role | Service owner. |
| Status | **Pending** |
---

## 4. Cursor recommendation versus FreeBuff assessment

### 4.1 Coverage: Cursor's 12 decisions against the register's 18

Cursor's validation ends with twelve owner decisions. They are not a subset of the register's eighteen, and the mapping matters because two registers with overlapping identifiers invite a ruling on the wrong question.

| Cursor decision | Register equivalent | Assessment |
|---|---|---|
| D-1 Is the repository seed the rule for `Resolved`? | Not one of the 18; register Section 5.3 technical check | **Misframed.** A seed is not a rule. The question is what the deployed row says, which is a read-only catalogue check, not an owner decision. Cursor's own Section 4.1 proves the point. |
| D-2 Is `New → Resolved` forbidden on purpose? | Not one of the 18 | Correctly excluded. `IsValidTransition` refuses it; there is no policy choice left. |
| D-3 All Open cross-company | D-1 | Same question, wider analysis needed (search oracle). |
| D-4 Requester resolve, close, reassign, priority | D-2 | Same question; the register adds close/reopen explicitly, which is necessary. |
| D-5 Who may process a ticket they do not own? | Folded into D-2 and D-14 | Correctly not a separate decision. |
| D-6 Agent hierarchy | D-14 | Same subject; Cursor's framing invites a group entity, the register's does not. |
| D-7 Must SLA be stored and checked? | D-7 | Same question, narrower scope (no pause, no mail). |
| D-8 Attachments and CSAT in this release? | D-13 for CSAT; attachments not one of the 18 | Correctly split by the register. |
| D-9 Who maintains `MST_SupportMapping`? | Not one of the 18 | A real gap. Mapping maintenance is a script with no screen, and D-14 makes the mapping an authorization input. It should be an explicit operational risk, even if not a nineteenth workflow decision. |
| D-10 Null `CompanyId` | D-15 | Same question. |
| D-11 Create permission and active employee | D-3 | Same question. |
| D-12 Global IT Head | D-10 | Same subject; the register adds the requester-as-IT-Head stall. |
| — | D-4, D-5, D-6, D-9, D-11, D-16, D-17, D-18 | **No Cursor equivalent.** Numbering, hold reason, reopen window, approval policy, rejection recovery, department, summary length, duplicate marking. The register is the more complete instrument. |

### 4.2 Claim-level verdicts where the documents disagree

| # | Claim | Source | FreeBuff verdict | Evidence |
|---|---|---|---|---|
| 1 | The status seed exists and sets `Resolved` non-terminal | Cursor §4.1 | **VERIFIED.** Cursor is right and the earlier FreeBuff AS-IS documents were wrong. | `Migrations/Configuration.cs` `Seed` inserts eight rows only when the table is empty, `Resolved` `IsTerminal = false`. |
| 2 | A listed cross-company ticket "cannot be opened" | FreeBuff FR-08 | **REFUTED.** Cursor's correction is right. | `ItsmTicketAccess.CanViewTicket` returns true before the company comparison for requester, assignee, pending approver. |
| 3 | SLA duration is clock hours to midnight, not plant open-to-close | Cursor §4.3 | **VERIFIED.** | `SlaService.AddBusinessHoursSkippingWeekends`; `CloseTime` is read only in `ResolveSlaStartTime`. |
| 4 | `Label` auto-encodes `<` and `>` so encoding is unnecessary | Cursor Stage 2 Part A | **REFUTED.** `Label.Text` is written to the response unencoded. The claim also contradicts Cursor's own FR-02, which treats `Label` as the unencoded sink. Relying on it would ship stored XSS. | `TicketDetail.aspx.cs` `LoadTicket` assigns `lblTitle.Text`, `lblDescription.Text`, `lblResolution.Text` from DTO values; `TicketDetail.aspx` line 227 binds `lblDescription` with no `HtmlEncode`, while comments use it. |
| 5 | `sp_getapplock` is used for number allocation | Cursor Stage 2 Part A | **REFUTED.** No `sp_getapplock` exists under `AnmolDristi/ITSM`. Cursor's own Part B says so. | Repository search; `GenerateTicketNumber` body. |
| 6 | Stage 1/2 FK names `itsm.RequestTypes`, `itsm.Plants`, `itsm.Branches` | Stage 1 and verification report | **REFUTED.** The migration uses `itsm.MST_RequestType`, `dbo.MST_Plant`, `dbo.MST_Branch`, `itsm.MST_Category`. | `202608120550515_InitialItsmSchema.cs`. |
| 7 | The rejection textbox has no `MaxLength` | Cursor AV-05 | **VERIFIED**, and the consequence is worse than stated. | `TicketDetail.aspx` line 318 has none; line 296 (`txtOverrideReason`) does. `TicketApprovals.Comments` is 500. |
| 8 | Null `CompanyId` is a critical denial-of-service defect | Cursor Stage 1 | **PARTIALLY REFUTED.** It is fail-closed by design and correct as tenant protection. It is an operational dead end, not a defect. | `RequireRecordedTenant`; migration comment. |
| 9 | The register's D-9 option A is safe policy data | Register | **REFUTED.** It creates duplicate numbers unless each request type gets a distinct prefix. | `GenerateTicketNumber` filters `RequestTypeId`, labels with `Prefix`. |
| 10 | The register's D-7 option B is the right first increment | Register | **DISPUTED.** Persisting and mailing an unverified branch-derived due date is worse than not persisting. | `TicketService` passes legacy `BranchId`; `SlaService` matches `VMS_PlantTimings.Branch_ID`. |

### 4.3 Where this review agrees with Cursor against FreeBuff's own earlier documents

Stated explicitly, because an independent review that only defends its own prior work is not independent:

- The seed exists. The AS-IS pack's repeated "no seed" claim is wrong, and every statement in the pack that depends on `Resolved` being terminal must be withdrawn.
- Cross-company **listing** is not cross-company **denial**. The FreeBuff finding overstated the consequence.
- SLA plant hours govern the **start**, not the duration. The FreeBuff descriptions of a "working-hours calendar" overstate the method.
- `Plant.SAP_Code` exists, so "zero SAP tokens" was a false search claim, even though SAP-based routing is genuinely absent.

### 4.4 Where this review disagrees with Cursor

- Cursor's Stage 2 plan should be withdrawn or re-issued. Two of its Part A statements are contradicted by the source and by its own Part B. A plan that an implementer can read as "the number race is solved and the detail page is encoded" is a hazard, not a draft.
- Cursor treats the duplicate-key exposure as something to remediate inside a broader stage. It should be a standalone integrity fix, because it is cheap, reversible, and independent of every policy answer.
- Cursor's decision list omits the hold reason, the reopen window, the rejection recovery path, the numbering format and the duplicate marking. Those are not optional details; three of them (D-5, D-6, D-11) change what "done" and "waiting" mean.

---

## 5. Confirmed implementation defects

Only failures that the current source demonstrates are listed here. Absent controls are in Section 6 as risks; proposed improvements are in Section 8. Each item names the file and method so it can be reproduced without this document.

### FB-DEF-01 — Concurrent creation can issue the same ticket number

**DEFECT. VERIFIED.** `TicketService.GenerateTicketNumber` performs a read-modify-write: it projects existing numbers for one request type and one year-month prefix, takes the numeric maximum, adds one, and returns the formatted string. The read runs under the default READ COMMITTED isolation inside the create transaction, with `AsNoTracking` and no lock hint. Two concurrent creates for the same request type read the same maximum. The insert occurs in a later `SaveChanges` inside the same transaction, and no unique index exists on `itsm.Tickets.TicketNumber` (`202608120550515_InitialItsmSchema.cs` line 78), so the database accepts both rows. **Impact:** duplicate human references; audit and mail ambiguity. **Not a row-loss defect** — `Tickets.Id` remains unique. Occurrence in any environment is UNVERIFIED.

### FB-DEF-02 — An over-length rejection reason throws an unhandled page exception

**DEFECT. VERIFIED by code path.** `TicketApprovals.Comments` is `nvarchar(500)` (`202608120550515_InitialItsmSchema.cs` line 139; entity `[StringLength(500)]`). The rejection textbox has no `MaxLength` (`TicketDetail.aspx` line 318). `RejectTicket` assigns `reason.Trim()` without a length check. EF6 validates string length on `SaveChanges` and throws `DbEntityValidationException`, which derives from `DbUpdateException`. `TicketUpdateService.RejectTicket` catches `DbUpdateConcurrencyException`, then `Exception` — rollback and `throw;`. `TicketDetail.btnReject_Click` has no `try/catch`. **Result:** an ASP.NET error page, no status change, no audit row, and no message telling the approver what went wrong. The same shape applies to any unexpected SQL failure on approve, reassign, override, comment or status change, because every handler calls the service directly. Contrast `CreateTicket.btnSubmit_Click`, which catches and shows a message: the pattern exists in the codebase and was not applied to the detail page.

### FB-DEF-03 — Splitting CR into two request types with one prefix duplicates numbers

**DEFECT (latent; triggered by the register's D-9 option A).** `GenerateTicketNumber` filters `t.RequestTypeId == requestType.Id` **and** `StartsWith(prefix + yy + MM)`, then allocates the next four-digit sequence. Two request types sharing the prefix `CR` each see only their own rows, so both allocate `CR{yy}{MM}0001` in the same month. This is exactly what "two change rows, standard off and normal both on" produces. **Impact:** duplicate numbers from a policy change that looks like data. **Mitigation:** distinct prefixes per request type, or one CR type with a class field.

### FB-DEF-04 — The recommended combination makes `Resolved` permanent

**DEFECT (design-level, in the proposed model rather than current code).** Composing the register's own recommendations: D-2 option B makes the requester the only actor who may close; D-6 option A sets no window; D-12 option A authorises no automatic closure; `Closed` is terminal; `ChangeStatus` has no administrative override and no permission key can substitute for the actor test. A ticket whose requester never confirms, or who has left the organisation, can never leave `Resolved`. It will also remain on the open queue, because `BuildQueueQuery` excludes only terminal statuses and the seed marks `Resolved` non-terminal. **Impact:** permanent queue noise and an unclosable record set. **Mitigation:** an administrative close (permission-gated, audited) or an owner-named window. This must be ruled together with D-2 and D-6, not afterwards.

### FB-DEF-05 — Audit text is silently truncated

**DEFECT. VERIFIED.** `TicketAuditLog.Detail` is `nvarchar(1000)` (`202608201200000_AddTicketAuditLog.cs` line 19). `TicketUpdateService.AddAudit` calls `Truncate(detail, 1000)` before insert. `AddComment` passes the full comment text; `txtComment` permits 2000 characters (`TicketDetail.aspx` line 339), and there is no server-side limit, so a longer value is accepted by the browser-independent path too. **Result:** the ticket comment is complete; the audit copy is silently cut. The register warns that this *would* happen if the resolution summary were copied to the audit row — it already happens on comments. **Impact:** the audit trail disagrees with the business record with no error, which is the failure mode D-17 is meant to prevent.

### FB-DEF-06 — Resolution evidence is overwritten without history

**DEFECT. VERIFIED.** `ChangeStatus` sets `ResolutionText` and `ResolvedOn` when the target is `Resolved`, and neither is cleared when the ticket leaves `Resolved`. The map permits `Resolved → In Progress → Resolved`. The `StatusChange` audit row is written with `Detail = null`, so it records the status names and nothing else. **Result:** a reopen followed by a second resolve replaces the previous summary and timestamp; the previous text is not recoverable from the audit trail. This matters directly for D-18, whose entire mechanism is a sentence in that field, and for D-6, whose reopen reason is also expected to live there.

### FB-DEF-07 — The creation mail contains an unusable link

**DEFECT. VERIFIED.** `EnqueueTicketCreatedNotification` writes `href="~/ITSM/UI/TicketDetail.aspx?id={id}"`. `~` is an application-relative token, not a URI scheme, and the application does not expand it in a string written to `NotificationQueue`. **Impact:** recipients receive a message whose only action is broken. D-8 exists because of this defect; the defect itself does not need a policy answer to be described.

### FB-DEF-08 — The approval panel is shown to actors the server will refuse

**DEFECT. VERIFIED.** `TicketDetailService.GetTicketDetail` sets `dto.CanApprove` from the existence of *any* pending approval row for the actor. It does not exclude the requester and does not check that the ticket's status is `Pending Approval`. `TicketDetail.aspx.cs` then binds `pnlApprovalActions.Visible = dto.CanApprove`. Two cases: (a) a requester who is also the configured IT Head sees the Approve button and receives "Separation of Duties violation" on click; (b) a stale pending row on a ticket that has left `Pending Approval` shows approve and reject controls that always fail. **Impact:** UI offers an action the server guarantees to refuse; on a support desk this trains users to distrust the screen.

### FB-DEF-09 — The sidebar overrides three permission-controlled controls

**DEFECT. VERIFIED.** `Dristi.Master.cs` `RefreshChromeAfterIdentityChange` calls `PermissionHelper.ApplyUiPermissions(this)`, then unconditionally sets `ITSM_Module`, `ITSM_TicketList` and `ITSM_CreateTicket` to `Visible = true` (lines 261–267). The `UI.ITSM_Module.View`, `UI.ITSM_TicketList.View` and `UI.ITSM_CreateTicket.View` keys are therefore decorative for these three controls. **Impact:** the menu advertises a queue to users who do not hold `ITSM.Queue.View`; the page then falls back to My Tickets and displays an authorisation message, which reads as a fault rather than a permission decision.

### FB-DEF-10 — The approval-configuration page accepts a session it cannot serve

**DEFECT. VERIFIED.** `ApprovalConfig.aspx.cs` gates on `USERID` and `BRANCH` only — unlike the other three ITSM pages, it does not require `WORKMAN` — then requires `ITSM.ApprovalConfig.Manage`. The actor recorded for every mutation is `CurrentEmployeeCode`, which is empty when `WORKMAN` is absent. `ApprovalConfigurationService.EnsureActiveActor` then throws "The configuration actor is not an active employee", and the page renders `ex.Message`. **Impact:** narrow (it requires a session shaped without `WORKMAN`), but it is an inconsistency between the four pages about what a valid ITSM session is, and it leaks a raw exception message.

---

## 6. Security-critical findings

### 6.1 Tenant isolation

**S-01 — Queue reads are not tenant-scoped, and this is deliberate. RISK (High). VERIFIED.**
`TicketListService.BuildQueueQuery` applies status, type, branch, assignee, priority and search predicates. It applies no `CompanyId` predicate for any of its four callers. `GetOpenQueue` is All Open. `GetAgentQueue` is My Queue, filtered to the actor as assignee. `GetMyTickets` filters by requester. `GetApprovalQueue` joins pending approvals. Only My Tickets and My Queue are inherently scoped by relationship. **Consequence:** any holder of `ITSM.Queue.View` reads every company's non-terminal tickets. Whether the role grants exist in the deployed database is UNVERIFIED. **Not a defect** in the sense of violating an implemented rule — the comments state the shared-service intent explicitly — but it is an unreviewed disclosure until D-1 is ruled and the grant population is known.

**S-02 — Search reveals text the grid withholds. RISK (Medium). VERIFIED.**
`BuildQueueQuery` matches `t.Description.ToLower().Contains(search)` while `TicketListItemDto` does not expose a description column. A queue viewer can therefore test hypotheses about another company's free-text ticket body, including names, systems and personal data, one phrase at a time, and observe a hit. No audit row records the search. This is a disclosure channel that survives every option in D-1 except removing the predicate. **Recommendation:** remove `Description` from the queue search as a security fix, independent of the tenant decision.

**S-03 — The agent directory is global and is rendered to viewers who cannot act. RISK (Medium). VERIFIED.**
`TicketListService.GetActiveAgents` returns every active employee with no company, branch or role filter. `TicketDetail.aspx.cs` `BindAgentDropdown` binds it into `ddlReassignTo` whenever the page renders, and `pnlWorkActions` is shown for any non-terminal ticket that is not `Pending Approval` — without testing `CanProcessTicket`. A pending approver in Company A, viewing a Company B ticket through the approval relationship, therefore sees the full employee directory of every company plus an action panel, though each mutation will be refused. **Recommendation:** gate the panel on `CanProcessTicket` and scope the reassignment list by the same rule D-1 chooses for queues.

**S-04 — Tenant enforcement is consistent in the mutation path and inconsistent in the read path. PARTIALLY VERIFIED.**
Consistent: create writes the snapshot from live `MST_UserMaster.CompanyId` and refuses a missing or non-positive value; all six mutations refuse a null snapshot (`RequireRecordedTenant`); the audit row copies the snapshot; `ITSM.Ticket.View` requires the ticket snapshot to equal the actor's *current* company. Inconsistent: relationship view (requester, assignee, pending approver) performs no company comparison at all; queues perform none; `GetBranches` and `GetActiveAgents` perform none; dispatch performs none (`MST_SupportMapping` has no company column). **Consequence:** an assignee whose HR company changes keeps working a ticket snapshotted to the old company — which is the intended shared-desk behaviour — while the same person loses the permission view path for that ticket. That asymmetry is worth stating to the owner alongside D-1, because it will be reported as a bug.

**S-05 — Cross-company mutation by relationship. RISK (Medium). VERIFIED.**
`LoadForProcess` requires only `CanProcessTicket` plus a non-null snapshot. Nothing compares the actor's company with the ticket's. An assignee in Company B may resolve, reassign, reprioritise and comment on a ticket belonging to Company A, and may reassign it to any active employee in any company. `ReassignTicket` validates `EmployeeStatus` only. **This is a design decision, not an accident** — but it means "tenant isolation" in ITSM means *write-time snapshot integrity plus one restricted read path*, not access isolation. Any statement to leadership that ITSM is tenant-isolated should be corrected.

### 6.2 Authorization

**S-06 — The requester holds the same process rights as the assignee. VERIFIED.** Covered as D-2. The security consequence: no separation of duties on resolve, reassign, priority, hold or close. The control that exists for approve/reject (requester excluded, `CanApproveTicket`) was not extended to processing, deliberately, per the file header comments.

**S-07 — No agent permission exists. VERIFIED.** There is no permission key that grants processing. `ITSM.Ticket.View` and `ITSM.Queue.View` confer reading and listing. A holder of both who is neither requester nor assignee cannot act on a ticket. This inverts the intuitive model: an authorised colleague cannot help, while the reporter can close. Whatever D-2 decides, an explicit processing capability should exist, because otherwise D-14's take-ownership rule has nothing to grant.

**S-08 — Manager and IT Head authority are relationship-based and correctly ordered, with two gaps. VERIFIED.** `ApproveTicket` requires a pending row for the actor, refuses the requester, and refuses an IT Head while any Manager row is pending. Gaps: the IT Head's identity is global and not company-scoped (D-10), and the approval identity can be changed by an administrator with no audit row (`SetActiveItHead`, `UpdateRequestTypeApproval`).

**S-09 — There is no administrative override anywhere. VERIFIED.** No permission bypasses the actor test, no role can move a `Pending Approval` ticket outside approve/reject, and no one can close a `Resolved` ticket they are not related to. This is a control in the sense that it prevents quiet intervention; it is a hazard in that a stalled ticket is unrecoverable (`FB-DEF-04`). The business must decide whether an override exists and who holds it; it cannot be inferred from the decision register, which is silent on it.

**S-10 — No anti-forgery protection on any ITSM mutation. RISK (High). VERIFIED absence.** No `ViewStateUserKey`, no `ValidateAntiForgeryToken`, no `__RequestVerificationToken` anywhere under `AnmolDristi/ITSM`. All six mutations and both configuration actions are same-site POSTs authenticated solely by the session cookie. Whether the framework's ViewState MAC plus request validation makes a cross-site POST infeasible depends on configuration that this review did not open. **Recommendation:** treat a per-session `ViewStateUserKey` plus token verification as a build gate before broadening mutation rights under D-2, because narrowing the actor set makes the remaining sessions more valuable to an attacker.

**S-11 — User-controlled text reaches unencoded label sinks. RISK (Medium). PARTIALLY VERIFIED.** `TicketDetail.aspx.cs` `LoadTicket` assigns `dto.Title`, `dto.Description`, `dto.ResolutionText` and `dto.CsatFeedback` to `Label.Text`; `TicketDetail.aspx` line 227 renders `lblDescription` with no `HtmlEncode`, while comments and approval comments are encoded explicitly. A stored-XSS payload in a ticket title or description would render to any viewer of the detail page. Exploitability was not executed, and request validation is a configuration fact that was not read. **Recommendation:** apply explicit `HtmlEncode` at every user-controlled sink; do not rely on `Label` behaviour, and specifically do not rely on the Cursor Stage 2 claim that `Label` encodes angle brackets.

**S-12 — Raw exception text reaches the page. RISK (Low). VERIFIED.** `TicketList.BindQueue` renders `"Error loading ticket queue: " + ex.Message`; `ApprovalConfig.aspx.cs` renders `ex.Message` for policy and IT Head failures; `CreateTicket` renders `InvalidOperationException` and `ArgumentException` messages with a correlation id. Database and schema details can therefore be disclosed to an authenticated user. Combined with `FB-DEF-02`, an unexpected failure on the detail page produces a full framework error page instead of a message.

**S-13 — Audit integrity is application-enforced only. RISK (Medium). VERIFIED.** Audit rows are inserted in the same transaction as the mutation, and no BLL method updates or deletes them. The database does not enforce append-only: no trigger, no `DENY`, no grant restriction in the migrations. `AddAudit` truncates as described in `FB-DEF-05`. `CompanyId` on the audit row is the ticket snapshot and is non-nullable, which is correct.

### 6.3 Session security and the actual session contract

**S-14 — The session contract is `WORKMAN`, `USERID`, `BRANCH`; there is no `BranchID` and no `CompanyID`. VERIFIED.**
`login.aspx.cs` (sign-in button path, lines 211–221) sets `CurrentUserId` (numeric id), `WORKMAN` (employee code), `USERID` (the same numeric id), `BRANCH` (the legacy `MST_UserMaster.BranchId` value as a string), `U_SITE` (department id), plus display fields. Older methods in the same file (`CredentialChecker1`, `CredentialCheckerSecure_Debug_OLD2`) set `USERID` to the employee code; they are not the sign-in button path, but a session created by them would carry a non-numeric `USERID`, which `BasePage.TryGetCurrentUserId` tolerates by falling back to `CurrentUserId` and then failing quietly. **Conclusions:**

- The register's Section 3 is **accurate**, including its warning that gating ITSM on `Session["BranchID"]` would send every current session back to login, and that `Session["BRANCH"]` is the legacy branch business key rather than `MST_Branch.Id`.
- Any specification text that asserts a `Session["BranchID"]` check "on every Page_Load" is **wrong**. The specification itself records this as a deviation (its Section 16.2 table). No code under `AnmolDristi/ITSM` reads `BranchID` or `CompanyID` from session.
- `Session["USERID"]` is a *presence* check on all four ITSM pages, not an identity source. `ApprovalConfig` is the outlier: it omits `WORKMAN` (`FB-DEF-10`).
- `BRANCH` is likewise a presence check. Ticket branch is resolved inside `TicketService` from the user master, so the session value cannot be used to forge a branch.

**S-15 — Session validation is weaker than it appears. RISK (Medium). VERIFIED.** `BasePage.EnsureSessionValid` returns immediately when `Session["LoginToken"]` is null, so a session without a token is never heart-beaten or soft-validated; the per-page key check is then the only gate. The method's `catch` swallows all exceptions by design. `ValidateTokenOnEveryRequest` is declared and never referenced. **Consequence:** session revocation depends on a token that some sign-in paths may not set, and on a database call that fails open.

**S-16 — Permission changes do not take effect within a session. RISK (Medium). VERIFIED.** `BasePage.UserPermissionSet` caches the permission set in `Session["PH_UserPermSet"]` and rebuilds only when the key is absent. No ITSM page calls `RefreshUserPermissions`. Revoking `ITSM.ApprovalConfig.Manage` from a live session therefore does not stop that session from changing the IT Head or the approval flags. With S-10, this is the pair that makes administrative actions weakly bounded.

**S-17 — The actor's employment status is not re-checked. RISK (Low/Medium). VERIFIED.** `ItsmTenantContext.IsActiveEmployee` exists and has no caller on any ITSM page or mutation. Create, detail, list and every mutation accept a session whose employee has since been deactivated. This is the same gap D-3 addresses for creation and it extends to processing.

---

## 7. Business decisions that block implementation

The register already divides the 18 into a blocking set and feature-local rows. This section keeps that division but adds the reason each block is real and, where relevant, the technical precondition that must be satisfied even after the ruling.

### 7.1 Blocks the first workflow build

| ID | What is blocked | Why the ruling alone is not enough |
|---|---|---|
| D-2 | Every mutation's actor test, the work panel, the reassign target list, the comment visibility rule | Needs D-14 for a taker of unassigned work, and needs an administrative close or D-6 window, or `Resolved` becomes permanent (`FB-DEF-04`). Needs the panel visibility to stop being a single switch. |
| D-14 | Take-ownership, and therefore D-2's replacement actor | Needs a deterministic mapping first. `DispatchService` uses unordered `FirstOrDefault` and no natural-key unique index exists, so "the mapped agent" is not yet an enforceable predicate. |
| D-9 | Which tickets start in `Pending Approval` | Needs a read-only look at the deployed request-type rows, and needs distinct prefixes if CR is split, or it triggers `FB-DEF-03`. |
| D-10 | Change authority, and the two create-time guards | Needs an audit row for IT Head and flag changes; otherwise the authority change itself is invisible. |
| D-11 | Whether `Rejected` is terminal | Needs D-8 if a rejection notice is added; needs the exception path fixed (`FB-DEF-02`) regardless. |
| D-5 | The status model, and any "waiting on requester" queue | Needs a storage decision for the class that is not free prose in `Detail`. |
| D-4 | Number integrity and every feature that quotes the number | The technical half does **not** need to wait for the other seventeen; I recommend splitting it out. |

### 7.2 Blocks only the feature it names

| ID | Feature that waits |
|---|---|
| D-1 | Any change to queue scope. Note that two sub-items should not wait: removing `Description` from search (S-02) and gating the work panel on `CanProcessTicket` (S-03) are defect/security fixes, not scope changes. |
| D-3 | Enforcing the create permission. The active-employee check can proceed under existing authority as a defect fix. |
| D-6 | Reopen window, mandatory reason, and any SLA effect on reopen. Note that stopping the silent overwrite of `ResolutionText`/`ResolvedOn` (`FB-DEF-06`) is a data-integrity fix, not a policy change. |
| D-7 | Persisted SLA, pause, breach mail. Blocked additionally on the `VMS_PlantTimings.Branch_ID` verification. |
| D-8 | Any absolute link, including the fix to the existing creation mail. |
| D-12 | Any closure job. |
| D-13 | Any CSAT gate. |
| D-15 | Any backfill or unfreeze. Note that excluding frozen rows from operational queues is an operational necessity, not a data change, and should be decided with D-1. |
| D-16 | A department column or display. |
| D-17 | A minimum length, and the audit-copy rule. The existing comment truncation (`FB-DEF-05`) should be fixed or explicitly accepted regardless. |
| D-18 | A parent-ticket column, and even the text convention until D-4 is ruled and the summary is written to audit. |

### 7.3 Technical gates that are not owner decisions

These have no D-number and cannot be closed by a business ruling. They must be closed before a demonstration of the operating model is credible.

| Gate | State | Why it blocks a demonstration |
|---|---|---|
| Deployed `MST_TicketStatus.IsTerminal` for `Resolved` | **UNVERIFIED** | A terminal `Resolved` makes close and reopen dead before any new code runs. The repository seed says non-terminal; the deployed table is the fact. |
| Deployed request-type codes, names, prefixes and flags | **UNVERIFIED** | D-9 cannot be applied or verified without them, and a one-character prefix stalls numbering at `0001`. |
| `VMS_PlantTimings.Branch_ID` versus the id create passes | **UNVERIFIED** | Decides whether any SLA start time is meaningful. |
| Deployed role grants for `ITSM.Queue.View`, `ITSM.Ticket.View`, `ITSM.Approval.View`, `ITSM.ApprovalConfig.Manage` | **UNVERIFIED** | Decides who currently holds the disclosure in S-01 and the configuration power in S-08. |
| Duplicate `TicketNumber` count | **UNVERIFIED** | Decides whether a backfill or a repair is needed before the unique index can be created. |
| Null `CompanyId` count | **UNVERIFIED** | Decides the size of the frozen population and whether queue exclusion is urgent. |
| Request-validation mode and ViewState MAC | **UNVERIFIED** | Decides the practical exploitability of S-11 and S-10. This is a configuration question; the file was not opened by design. |

---

## 8. Recommended decisions and rationale

### 8.1 Recommended answer per decision

Every recommendation below is advice. None closes a decision, records a signature, or authorises work. Where I differ from the register the difference is marked.

| ID | FreeBuff recommended answer | Rationale, in one line | Differs from register |
|---|---|---|---|
| D-1 | A, plus remove `Description` from queue search, plus gate the work panel on `CanProcessTicket` | A shared desk is what dispatch already is; the search oracle and the action panel are separate defects that no tenant ruling should preserve | No (adds controls) |
| D-2 | B, with a separate confirmation predicate, and an administrative close | Assignee accountability is the point of the model; without an escape hatch the model strands tickets | No (adds predicate and escape) |
| D-3 | B — active employee at create; permission deferred until grants exist | Requiring a permission that ordinary employees do not hold is an outage disguised as a control | No |
| D-4 | Technical: unique index + locked allocation + bounded retry now. Business: format unchanged, ceiling accepted | The duplicate key is a defect, not a policy; the number format is a business fact | **Yes** (split, and un-block the technical half) |
| D-5 | A — one pause status, mandatory coded class, class in its own field | A single pause keeps the map small; a coded class is what makes a waiting queue honest | No (adds storage constraint) |
| D-6 | A, plus either an owner-named window or an administrative close, plus audit of the previous resolution | Reopen must be possible and must not destroy evidence | No (adds escape and history fix) |
| D-7 | C for the first build — persist start and due, no pause, no mail | Do not mail an escalation derived from an unverified branch key space | **Yes** |
| D-8 | A — one operations-supplied HTTPS origin; Section 14 marked accepted, reduced or deferred | The current link is broken and the origin must be an owned address | No |
| D-9 | C then A, with a distinct prefix per change type and audited flag changes | Reading the deployed rows first prevents a policy change that silently duplicates numbers | **Yes** (adds prefix and audit) |
| D-10 | A, with the merged manager/IT-Head action recorded as both levels and authority changes audited | One authority is thin, but it is the smallest model that works; the stall and the missing audit are the real gaps | No (adds audit) |
| D-11 | A, plus a rejection notification and a length check that refuses instead of throwing | Terminal rejection is defensible only if the employee is told | No (adds notice) |
| D-12 | A — no automatic closure, with an administrative close available to a person | An age-based job without a named actor is worse than a manual exception | No |
| D-13 | A — no CSAT gate; columns stay unused | A mandatory survey blocks closure and needs a form that does not exist | No |
| D-14 | A, conditional on a deterministic mapping (unique natural key or explicit ordering) | An authorization rule cannot be built on an unordered `FirstOrDefault` | **Yes** (adds precondition) |
| D-15 | A — frozen, no HR backfill, and excluded from operational queues | Freezing protects the tenant; leaving them listed creates permanent queue noise and duplicate reports | No (adds queue exclusion) |
| D-16 | A — no snapshot, no department on the ticket | Live HR data on a historical record reads as history | No |
| D-17 | A — non-blank, no minimum, refuse over-length rather than truncate | A minimum length rejects genuine short outcomes; silent truncation is the real defect | No |
| D-18 | A, gated on D-4, with the duplicate number also written to the audit detail | Text in a mutable field is not evidence unless the fact is copied somewhere durable | No (adds audit copy) |
| Sec. 3 | Keep `WORKMAN`, `USERID`, `BRANCH`. Add no `BranchID` or `CompanyID` session key | The register's Section 3 is correct and should be confirmed as written | No |
| Sec. 4.4 | Section 14 mail list marked accepted, reduced or deferred with D-8 | Notification scope is not an extra decision, but silence leaves it unowned | No |

### 8.2 Operational simplification

The request asks for unnecessary steps, excessive approvals, redundant statuses and avoidable employee interactions. Findings, ordered by how much effort they remove:

**Redundant statuses — none should be added, and one is effectively unrepresentable.**
The eight seeded names are sufficient. `Submitted`, `Assigned`, `Acknowledged`, `Pending Requester`, `Pending Information` and `Pending Vendor` all collapse into an existing status plus a field or a reason class. The specification is right to refuse them, and the register's D-5 preserves that. The one status that exists but cannot be reached is `Pending Approval` via `ChangeStatus` — correct by design, and worth stating in training so agents do not hunt for it.

**Excessive approvals — one genuine excess and one genuine gap.**
The excess: when the requester's reporting manager is also the configured IT Head, two approval rows are created for one person and the same person is asked to approve twice. Recording one action that satisfies both levels removes a click and a duplicate audit row. The gap: there is no way to approve *with a comment*, because `btnApprove_Click` passes `comments: null` and the page has no field. Approvers who want to record a condition must either reject or leave no trace. Adding an optional comment costs one textbox and materially improves the audit trail.

**Avoidable employee interactions.**
- The employee receives no notification on approval, rejection, resolution or reassignment. Every one of those events currently requires the employee to open the ticket to discover the outcome. A rejection notice is the single highest-value addition, and it is the difference between "terminal rejection" being a control and being a dead end.
- The created ticket reports "New" with no acknowledgement expectation. A single line on the detail page naming who must act next would remove most status queries without a workflow change.
- The work panel is shown to people who cannot use it (S-03). Hiding it removes a failed click and a support call.

**Unnecessary workflow steps.**
- Reassignment requires no reason, so a ticket can change hands silently; adding a mandatory reason (already recommended by the register outside the 18) is a *strengthening*, not a simplification. The simplification is elsewhere: the reassignment dropdown lists every active employee in every company, which makes the correct target hard to find. Scoping it to the support mapping for the ticket's branch and category is both a control and a usability gain.
- The create form requires impact and urgency separately, then computes priority. That is two inputs for one derived value, and the employee's own urgency judgement is the weakest signal in the model. It is not worth changing in this build, but the owner should know the priority matrix effectively lets the reporter set their own urgency.
- The category hierarchy is correctly server-validated, but the concierge value of a per-category mandatory field set is not implemented and should not be inferred.

**What not to add.** No CAB, no CSAT gate, no automatic closure, no support-group entity, no multi-level approval chain, no separate catalogue product. Each would add maintenance surface for a benefit the current volume does not demonstrate, and each is correctly excluded from the first build.

### 8.3 Recommended ruling sequence

This differs from the register's sequence in two places: the numbering control moves out of the business queue entirely, and the tenant pair is separated from the actor pair so that security concurrence is not blocked behind an operational debate.

| Step | Ruling or action | Unblocks |
|---|---|---|
| 0 | Read-only verification: `MST_TicketStatus`, `MST_RequestType`, `VMS_PlantTimings.Branch_ID`, permission grants, duplicate numbers, null-company count | Every later step, and every demonstration |
| 1 | **D-4 technical half** — unique index, locked allocation, retry; format unchanged | Number integrity, independent of policy |
| 2 | **D-1 and D-15 together** — shared desk or tenant boundary, and the fate of frozen rows; IT security concurrence | Queue scope, and whether frozen rows leave the queue |
| 3 | **D-14** — one agent or a group, and who may take unassigned work, with a deterministic mapping | A safe D-2 |
| 4 | **D-2** — actor rules including close and reopen, plus the administrative close or window | Every mutation |
| 5 | **D-3** — active employee now; permission deferred | Create |
| 6 | **D-5** — pause plus coded reason | Status model, later pause |
| 7 | **D-11** — terminal rejection, with a notice | Recovery path |
| 8 | **D-10** — IT Head only, merged action recorded, authority changes audited | Change authority |
| 9 | **D-9** — policy as data, after the read-only check, with distinct prefixes | Approval flags |
| 10 | **D-6** — window, reason, history fix | Completion signal |
| 11 | **D-17** — non-blank, refuse rather than truncate | Resolution quality |
| 12 | **D-12 and D-13** — no auto-close, no CSAT gate | Exclusions |
| 13 | **D-18** — text plus audit copy, after D-4 | Duplicate handling |
| 14 | **D-16** — no department | Exclusion |
| 15 | **D-7** — C now, B after the branch-key verification | SLA work |
| 16 | **D-8** — origin plus Section 14 scope | Clickable mail |
| 17 | **Section 3** — confirm the existing session contract in writing | Prevents a future redesign by inference |

Note that items 1, 3 (the determinism part), 5 (the active check), 13 and 16 (the link fix) are technical or defect-level and can begin as soon as they are authorised as such. Holding them behind sixteen owner rulings is the wrong sequencing, and it is the one material criticism I make of the register's plan.

---

## 9. Questions requiring management clarification

These are questions the evidence cannot answer and that an implementer must not decide. Each names who can answer and why it matters.

| # | Question | Who | Why it blocks work |
|---|---|---|---|
| Q-1 | Is the service desk one shared desk for all companies, or does any company boundary survive contact with it? If a boundary survives, does it apply to titles only, or to requester and assignee names, description search, and reassignment targets? | Service owner + IT security | D-1 and D-15 cannot be applied partially without creating a false sense of isolation |
| Q-2 | Is there a legal, contractual or group-policy constraint on one company's employee seeing another company's ticket titles? | IT security / legal | Turns a design preference into a compliance requirement |
| Q-3 | Who may clear a ticket the requester never confirms, and under what permission? | Service owner | Without an answer, D-2 B + D-6 A + D-12 A strand tickets permanently |
| Q-4 | If the requester leaves the organisation with a ticket in `Resolved`, what is the intended disposition? | HR + service owner | The same stranding question, in its most common operational form |
| Q-5 | For a change request raised by the configured IT Head, who approves? | Service owner + IT leadership | The ticket is currently unrecoverable |
| Q-6 | Does the business accept 9999 tickets per request type per month, or must the number accommodate more? | Service owner + IT leadership | Decides whether the public format changes |
| Q-7 | How many distinct change types will exist, and what prefix will each carry? | Service owner + ITSM administrator | Two CR types sharing `CR` duplicate numbers |
| Q-8 | Is rejection genuinely terminal from the employee's point of view — that is, is "raise it again" acceptable to the people who will do it? | Service owner | If not, D-11 needs option B and a larger build |
| Q-9 | Which roles must keep the ability to raise tickets if `UI.ITSM_CreateTicket.View` is enforced, and are those grants in place? | Service owner + ITSM administrator | Enforcing without grants is an outage |
| Q-10 | Should the breach mail go to the assignee's reporting manager even when that manager is in another company and holds no ITSM permission? | Service owner + IT security | A cross-company disclosure introduced by D-7 |
| Q-11 | Does the business want a satisfaction score at all, and if so may it be optional? | Service owner | Determines whether the CSAT columns stay dormant |
| Q-12 | What is the single published HTTPS origin for ticket links, per environment? | IT operations | D-8 cannot be implemented without the value |
| Q-13 | Who maintains `MST_SupportMapping`, and how is a change evidenced? | Service owner + ITSM administrator | No screen exists; mapping rows become authorization inputs under D-14 |
| Q-14 | Are the frozen null-company tickets to be excluded from operational queues, or kept visible with a badge? | Service owner + data owner | Determines whether the desk sees work it can never clear |
| Q-15 | Is a resolution "done" when the assignee says so, or only when the requester confirms? | Service owner | The single sentence that decides D-2, D-6 and D-12 together |
| Q-16 | Does the business require an administrative override for a stalled ticket, and if so who may use it and what is audited? | Service owner + IT security | Every "no override" design in the register creates an unrecoverable state |

---

## 10. Final readiness assessment

### 10.1 Direct answer to the final question

**No. The proposed operating model cannot be implemented safely today, and it will not become safe by approving all 18 decisions alone.**

Approving every decision removes the *governance* blocker. It does not remove five factual blockers, four of which are confirmed defects or absent controls in the current code:

1. **Ticket numbers can duplicate, and one class of prefix stalls the sequence permanently.** No business ruling fixes a read-modify-write with no lock and no unique index. (`FB-DEF-01`, `FB-DEF-03`)
2. **A rejection longer than 500 characters produces an unhandled framework error rather than a refusal**, so the approver gets no feedback and the transaction exits with no audit row. (`FB-DEF-02`)
3. **`Resolved` can become a permanent state** under the register's own recommended combination, with no administrative exit. (`FB-DEF-04`)
4. **Resolution evidence is destroyed on re-resolve**, which undermines D-18 and any later dispute. (`FB-DEF-06`)
5. **Deployed `IsTerminal`, request-type flags and prefixes, the SLA branch key space, and the permission grants are all unread.** Any of them can invalidate a demonstration on the day it is given. (Section 7.3)

Three further conditions are not defects in the sense of broken code, but they make "safe" false if left unaddressed while mutation rights are narrowed: no CSRF protection (S-10), no output encoding at the detail page's user-text sinks (S-11), and permission changes that do not take effect within a session (S-16). Narrowing who may act raises the value of the sessions that remain.

### 10.2 Is it efficient once those are fixed?

**Yes, with two scope reductions.** The architectural direction is correct and cheap: one table, one transition map, three thin policies implemented as data plus a small allow-list. The register's instinct to refuse new statuses, a group entity, a CAB and an auto-close job is right, and it keeps the build small.

The two reductions I recommend are the register's own scope, not the direction:

- **Do not put the SLA mail and the pause in the first build.** Persist start and due only. That removes the branch-key dependency, the pause-reason dependency and the cross-company escalation question from the critical path, and it still gives reporting a clock.
- **Do not put the notification matrix in the first build.** Fix the broken link on the creation mail and add exactly one new message — rejection — because that is the one whose absence turns a control into a dead end. The rest can follow.

With those reductions, the first build is: number integrity, actor rules, hold reason, rejection notice, status audit detail, and the security gates. That is a coherent, demonstrable increment that does not depend on the tenant debate concluding first.

### 10.3 What "ready" would look like

Ready means all of the following, in this order:

1. The read-only verification in Section 7.3 is complete and recorded against a named environment, with the actual values, not a restatement of the seed.
2. `FB-DEF-01`, `FB-DEF-02`, `FB-DEF-05`, `FB-DEF-06`, `FB-DEF-08` and `FB-DEF-09` are fixed, and `FB-DEF-03` is prevented by distinct prefixes.
3. S-02 and S-03 are fixed; S-10 and S-11 are fixed or explicitly accepted in writing with a compensating control.
4. D-1, D-15, D-14 and D-2 are ruled as a set, with IT security concurrence, and the administrative close or window is ruled in the same meeting.
5. D-4 is authorised as a defect fix rather than waiting behind the policy set.
6. A rollback and a queue-exclusion path exist for the frozen null-company population.
7. All 18 rows are actually marked, by the named owner role, with the chosen option and a date. An empty row is not a deferral, and this review does not fill any of them in.

### 10.4 Statement on approvals and repository state

No decision in this review is approved, deferred, or signed. Every one of the 18 remains **Pending** with its owner role unchanged from the register. This document does not edit `ITSM_BUSINESS_DECISION_REGISTER.md`, the target specification, or any other existing document, and it does not resolve a conflict between them: where the register and Cursor disagree, both positions are preserved above with the evidence, and the choice is left to the named owner.

---

## Appendix A — Evidence index

| Evidence | File | What it establishes |
|---|---|---|
| A1 | `AnmolDristi/ITSM/BLL/ItsmTicketAccess.cs` | The three predicates; relationship view with no company test; permission view with company test; approve excludes requester |
| A2 | `AnmolDristi/ITSM/BLL/TicketWorkflow.cs` | Four-key transition map; `Pending Approval`, `Closed`, `Cancelled`, `Rejected` absent |
| A3 | `AnmolDristi/ITSM/BLL/TicketUpdateService.cs` | Terminal check order; resolution-text rule; `ResolvedOn`/`ClosedOn` writes; audit detail null; `AddAudit` truncation; rethrow on unexpected failure; null-tenant block on all six mutations |
| A4 | `AnmolDristi/ITSM/BLL/TicketService.cs` | Company snapshot and refusal; branch resolution by `Branch_ID`; legacy id into SLA and resolved id into dispatch; number allocation; status resolution with fallback; approval-row creation and the manager-only guard; notification insert inside the transaction; relative mail link |
| A5 | `AnmolDristi/ITSM/BLL/TicketListService.cs` | No company predicate; search includes description; global agent and branch lists; `OpenOnly` excludes terminal only |
| A6 | `AnmolDristi/ITSM/BLL/TicketDetailService.cs` | `CanViewTicket` call; `CanApprove` from a pending row only; internal-note rule and comment filtering |
| A7 | `AnmolDristi/ITSM/BLL/SlaService.cs` | `VMS_PlantTimings.Branch_ID` lookup; start deferral; clock-hours duration to midnight |
| A8 | `AnmolDristi/ITSM/BLL/DispatchService.cs` | Four-step cascade; unordered `FirstOrDefault`; null assignee |
| A9 | `AnmolDristi/ITSM/BLL/ApprovalConfigurationService.cs` | Flag update and IT Head switch without workflow audit; active-actor requirement |
| A10 | `AnmolDristi/ITSM/UI/CreateTicket.aspx.cs` | Session-only gate; no permission check; hierarchy and master validation; exception display |
| A11 | `AnmolDristi/ITSM/UI/TicketList.aspx.cs` | Tab authorisation boundary; fallback behaviour; exception text |
| A12 | `AnmolDristi/ITSM/UI/TicketDetail.aspx.cs` | Actor from `WORKMAN`; work-panel visibility without `CanProcessTicket`; agent dropdown binding; no try/catch on mutations |
| A13 | `AnmolDristi/ITSM/UI/TicketDetail.aspx` | Line 227 unencoded description label; line 240 work panel; line 269 reassign list; line 296 override `MaxLength`; line 318 rejection textbox without one; line 339 comment 2000 |
| A14 | `AnmolDristi/ITSM/DAL/Entities/*` | `TicketNumber` 10 characters; `Prefix` 2 characters `char`; approval comments 500; audit detail 1000; comment unbounded |
| A15 | `AnmolDristi/ITSM/DAL/Migrations/Configuration.cs` | Eight-status seed, `Resolved` non-terminal, applied only when empty |
| A16 | `AnmolDristi/ITSM/DAL/Migrations/202608120550515_InitialItsmSchema.cs` | `TicketNumber varchar(10)` line 78; approval comments 500 line 139; no unique index on the number |
| A17 | `AnmolDristi/BasePage.cs` | `LoginToken` early return; permission cache; `USERID` numeric fallback |
| A18 | `AnmolDristi/login.aspx.cs` | Session assignment block, lines 211–221 |
| A19 | `AnmolDristi/Dristi.Master.cs` | `RefreshChromeAfterIdentityChange` lines 247–267 forcing three ITSM controls visible |

**Repository searches performed:** no `ViewStateUserKey`, no anti-forgery token, no `sp_getapplock` under `AnmolDristi/ITSM`; no unique index on `TicketNumber` in any migration; no session assignment to `BranchID` or `CompanyID`.

**Not performed:** SQL of any kind; UAT or LIVE access; deployment; browser or runtime pass; reading `Web.config`; any modification to existing files; any staging, commit, push or tag.

## Appendix B — Record of this review's own footprint

This review added exactly one file, `ITSM_FREEBUFF_INDEPENDENT_DECISION_REVIEW.md`. It used temporary part files under `.freebuff/` only to assemble the text, and removed them. The working tree already contained, before this review, a modified `AnmolDristi/Web.config` (not opened, not touched), thirteen untracked ITSM documents at the repository root, and untracked files under `Database/Deployments/`. Those pre-existing changes were left exactly as found. The final working-tree status is recorded in the completion message rather than in this file, because a status written here would be a claim that cannot be re-verified after the fact.
