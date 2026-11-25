# ITSM — FreeBuff Findings Reconciliation

**Document type:** Independent reconciliation. Not an approval. Not an instruction to implement.
**Date:** 2026-11-24
**Repository HEAD inspected:** `f1f4bdb` on `Aug26_AILWorks_AmimnrupSupport`
**Primary claim source:** `ITSM_FREEBUFF_INDEPENDENT_DECISION_REVIEW.md`
**Compared with:** `ITSM_BUSINESS_DECISION_REGISTER.md`, `ITSM_TARGET_OPERATING_WORKFLOW_SPECIFICATION.md`, `ITSM_DOCUMENTATION_CONSOLIDATION_REVIEW.md`, `ITSM_Cursor_Independent_Validation.md`, `ITSM_STAGE_2_INDEPENDENT_REVIEW_PLAN.md`
**Mode:** Read-only source inspection. No application file was modified. No database object, SQL script, migration, or existing document was modified. No SQL was executed. No UAT or LIVE connection was opened. `AnmolDristi/Web.config` was not read. Nothing was staged, committed, pushed, or tagged.

**Status of D-1 through D-18 after this reconciliation:** **Pending.** This document does not approve, defer, sign, or renumber any decision. Where a technical control can be specified without a service-owner ruling, that control is listed in Section 4. Listing it here does not authorise a code or schema change.

---

## 1. Executive summary

The architectural direction in the target specification — one ticket foundation and thin type-specific policy — still fits the schema. The register's eighteen questions are still the right questions. Approving all eighteen would remove the governance queue. It would not make the operating model safe to build.

This pass re-read the ITSM source, the EF6 entities, and the ITSM migrations for every material FreeBuff claim. Nine of the eleven mandatory findings are confirmed as source facts. Four FreeBuff sentences overstate a consequence or mis-state a type, and those disagreements are in Section 3. None of them removes the underlying control.

Technical readiness and business approval are separate:

| Question | Result |
|---|---|
| Are the 18 decisions approved? | No. Every row remains **Pending**. |
| Does approving them authorise implementation? | No. Number allocation, output encoding, rejection validation, resolution history, and deterministic assignment are source defects or missing controls. A signature on a policy row does not repair them. |
| Can SLA deadlines be stored now? | No. Create passes `MST_UserMaster.BranchId` into `SlaService` and `MST_Branch.Id` into dispatch. `SlaService` compares the value it receives with `VMS_PlantTimings.Branch_ID`. Which of those two identifiers that column holds was not read. Persistence waits on that read. |
| Can automatic pickup be built on the current mapping? | No. `DispatchService` uses unordered `FirstOrDefault`, and `MST_SupportMapping` has no unique natural key. |
| Is the current application already stuck in `Resolved`? | No, on the repository seed. `Resolved` is non-terminal, and the map allows `Resolved → Closed` and `Resolved → In Progress` for the requester or the assignee. The permanent-`Resolved` outcome appears only if the register's D-2 option B, D-6 option A, and D-12 option A are implemented together with no administrative exit. The deployed `IsTerminal` flag was not read and can make both edges dead before any of that code is written. |

**Readiness verdict:** not ready to implement the Phase 2B operating model. Ready to *specify* a short list of technical controls. Not ready to apply them until the environment reads in Section 6 are done for the controls that touch existing rows.

---

## 2. Finding-by-finding reconciliation

Evidence labels match the FreeBuff review: **VERIFIED** means the statement was read in current source or a migration in this pass. **UNVERIFIED** means a deployed row or configuration value was not read. **DISAGREE** means the cited sentence does not match the source that was read.

### 2.1 D-7 — SLA branch-key mismatch

| Field | Reconciliation |
|---|---|
| FreeBuff's claim | Create passes legacy `MST_UserMaster.BranchId` into `SlaService` and resolved `MST_Branch.Id` into `DispatchService`. `GetPlantTiming` matches `VMS_PlantTimings.Branch_ID` to whatever integer it is given. Persisting that due date and mailing it would turn an unverified key into an authoritative control. FreeBuff recommends D-7 option C for the first build: persist start and due, with no pause and no mail. |
| Source evidence | `TicketService.CreateTicket` calls `_slaService.CalculateSla(requester.BranchId, createdOn, resolutionHours)` and `_dispatchService.ResolveAssignee(resolvedBranchId, ...)`. `resolvedBranchId` is `MST_Branch.Id` after a lookup `b.Branch_ID == requester.BranchId`. `SlaService.GetPlantTiming` filters `t.Branch_ID == branchId`. `CalculateSla` opens its own `ItsmDbContext`, outside the create transaction. `Tickets` has no SLA columns in `202608120550515_InitialItsmSchema.cs`. The two timestamps are written only into the creation-mail body. `AddBusinessHoursSkippingWeekends` consumes clock hours until midnight and skips calendar weekends. `CloseTime` is used in `ResolveSlaStartTime` only. |
| Cursor verification | **VERIFIED** that the two call sites pass different properties. **VERIFIED** that nothing in the ITSM migrations or entities proves `VMS_PlantTimings.Branch_ID` equals `MST_UserMaster.BranchId` or equals `MST_Branch.Id`. The names are consistent with a legacy business key, and they are also consistent with a mistaken identity key. A code comment on `Branch` that says `Branch_ID` uniqueness was verified is a comment, not a query run in this pass. **AGREE** with Cursor validation FR-23: the split is real, and the data relationship is unverified. **DISAGREE** with FreeBuff's option C. Storing the number before the relationship is known writes the same unverified value the mail would send. |
| Agreement | Agree on the unverified key split. Disagree on persisting deadlines in the first build. |
| Technical severity | High if a due date is stored or mailed from the current call. Low while the value lives only in an email that is already labelled as a calculation, provided nobody treats that email as the system of record. |
| Business impact | A wrong stored deadline becomes the number reports, pause maths, and escalation would trust. The current calculator is also not a plant open-to-close calendar. Either fact is enough to keep the clock out of `Tickets` until both the key and the accepted calendar are explicit. |
| Required corrective action | Do not add `SlaStartOn` or `SlaDueOn`, and do not send a breach mail, until a named-environment read shows whether `VMS_PlantTimings.Branch_ID` matches the legacy branch key passed today or the identity passed to dispatch. After that read, D-7 is still an owner ruling. If the keys differ, fix the argument before any persistence. Keep the existing email-only calculation until then. |
| Business approval required | Yes for any SLA scope (D-7 remains Pending). No for the decision to withhold persistence; that withhold is the safe technical position. |
| Blocks implementation | Blocks SLA persistence, pause-on-hold, and breach mail. Does not block numbering, encoding, rejection validation, or resolution history. |

### 2.2 D-4 — Ticket-number uniqueness

| Field | Reconciliation |
|---|---|
| FreeBuff's claim | `GenerateTicketNumber` is a read-modify-write under READ COMMITTED, with `AsNoTracking`, no lock, no sequence, and no unique index. Concurrent creates can insert the same number. A prefix that trims to one character makes a nine-character number, the parser skips it, and every later create returns `0001`. At 10000, `D4` emits five digits and the `varchar(10)` column cannot store them. FreeBuff splits D-4 into a technical control to authorise now and a business confirmation of format and ceiling. |
| Source evidence | `GenerateTicketNumber` (`TicketService.cs`) trims `RequestType.Prefix`, cuts it to two characters, builds `prefix + yy + MM`, and selects `TicketNumber` where `RequestTypeId` equals that type and the number starts with that prefix. It parses the last four characters only when `Length >= 10`, takes the max, adds one, and formats `D4`. The read sits inside `BeginTransaction()` with no isolation argument. `202608120550515_InitialItsmSchema.cs` defines `TicketNumber` as `varchar(10)` and indexes foreign keys only. `RequestType.Prefix` is `char(2)` (`StringLength(2)`, `TypeName = "char"`). No `sp_getapplock`, `UPDLOCK`, `SEQUENCE`, or retry exists under `AnmolDristi/ITSM`. `Tickets.Id` is an identity primary key. |
| Cursor verification | **VERIFIED. AGREE** with FreeBuff FB-DEF-01 and with Cursor FR-01. The race is structural: two transactions can read the same maximum and both inserts succeed because nothing unique rejects the second. A trimmed one-character prefix yields nine characters (`X` + `yy` + `MM` + `0001`). Those rows fail the `Length >= 10` guard and contribute 0, so the next value stays 1. Sequence 10000 formats as five digits. With a two-character prefix the result is 11 characters, which `[StringLength(10)]` and the column both reject. Integer overflow is not the failure that occurs first; the column length is. No duplicate count was queried. |
| Agreement | Agree. The defect is in current source. The format and the 9999 ceiling remain an owner confirmation. This document does not mark D-4 Approved and does not authorise the schema change. |
| Technical severity | High for reference integrity. Medium for operations: the collision is silent until something quotes the number. |
| Business impact | Mail subjects, rejection quotes (D-11), and duplicate sentences (D-18) become ambiguous. Rows are not lost. `Tickets.Id` stays unique. A one-character prefix, if any deployed row has one, stalls that type at `0001` on every subsequent create. |
| Required corrective action | See Section 4.1. Short form: a unique constraint on the public number, allocation serialised on the public prefix plus year and month across all request types, a bounded retry of the whole create, a hard stop at 9999 with a returned error, and a refusal when the trimmed prefix is not exactly two characters. Do not renumber tickets already issued. Do not create the unique index until Section 6 has counted existing duplicates. |
| Business approval required | The service-owner half (keep `prefix + yy + MM + 4 digits`, accept or raise 9999) stays inside Pending D-4. The integrity control does not need the other seventeen rulings. Applying it still needs IT leadership authorisation and the duplicate-count read. |
| Blocks implementation | Blocks any feature that treats the human number as unique, including D-9 option A and D-18. Does not block the owner from ruling unrelated rows. |

#### 2.2.1 Numbering controls this reconciliation requires

| Control | What the source does today | What has to be true before the number can be trusted |
|---|---|---|
| Atomic allocation | Max is read and the insert happens later in the same default transaction. Another session can read the same max. | One lock, held until commit, per public prefix + year + month. A transaction-scoped `sp_getapplock`, an `UPDLOCK`/`HOLDLOCK` read, or a counter row are all sufficient mechanisms. None of them is present. This document does not choose among them and does not implement one. |
| Database uniqueness | No unique index on `itsm.Tickets.TicketNumber` in any ITSM migration. | A unique constraint on the stored number. It is the backstop. It does not by itself stop two sessions computing the same value; without a retry, the loser fails the create. |
| Prefix collision | The max query filters `RequestTypeId` and `StartsWith(prefix)`. Two types with prefix `CR` each allocate `CR{yy}{MM}0001`. | The allocation set is every ticket whose number uses that public prefix, regardless of `RequestTypeId`. Distinct prefixes are a business choice under D-9. They are one way to avoid a collision. A prefix-scoped allocator plus the unique constraint prevents the collision even when two types share a prefix, and the sequence is then shared. |
| Concurrency | READ COMMITTED, `AsNoTracking`, no retry. `CreateTicket` rolls back and rethrows. | Bounded retry of the entire create on a new context after a uniqueness conflict. A retry inside the failed transaction is not sufficient. |
| Exhaustion | No ceiling check. `10000.ToString("D4")` is `10000`. The 11-character value fails validation or the column. | If the next value would exceed 9999, return a controlled failure and do not attempt the insert. Raising the ceiling changes the public format and stays in Pending D-4. |
| Existing references | The generator only reads existing numbers. It does not rewrite them. | Leave issued numbers as stored. A later unique index has to be able to be created against those values, which means duplicates, if any, are repaired under a separate data authorisation before the index is created. Nine-character numbers already stored remain valid references; new allocation stops emitting that shape. |

### 2.3 D-2 + D-6 + D-12 — permanent `Resolved`

| Field | Reconciliation |
|---|---|
| FreeBuff's claim | The register's own recommendations combine into a graveyard. D-2 option B makes the requester the only actor who may close. D-6 option A sets no window. D-12 option A authorises no automatic closure. `Closed` is terminal. There is no administrative override. A requester who never confirms, or who has left, leaves the ticket in `Resolved` forever, and the open queue keeps it because the seed marks `Resolved` non-terminal. FreeBuff labels this FB-DEF-04, a design defect in the proposed model. |
| Source evidence | Current code does not do this. `ItsmTicketAccess.CanProcessTicket` is requester or assignee. `TicketWorkflow` allows `Resolved → Closed` and `Resolved → In Progress`. `ChangeStatus` refuses the transition only when the current status row has `IsTerminal` true, and it checks that flag before the map. `Configuration.Seed` inserts `Resolved` with `IsTerminal = false`, and only when `MST_TicketStatus` is empty. `Closed`, `Cancelled`, and `Rejected` are terminal in that seed and have no map exit. No ITSM job writes `Closed`. Register option texts, still Pending: D-2 B is "assignee only for resolve, reassign, and priority override" and "the requester … is the only actor who may close or return a resolution." D-6 A is reopen from `Resolved` at any time, with a mandatory reason. D-12 A is "do not build a job." |
| Cursor verification | **VERIFIED** current edges and the seed. **VERIFIED** that the recommended combination has no third actor. **AGREE** that implementing B + A + A with no named escape strands confirmation: the assignee may resolve, and only the requester may close or return the resolution. If that person never acts, the ticket stays `Resolved` and, while `IsTerminal` is false, stays on the open queue. **DISAGREE** that this is a defect in the running code, and **DISAGREE** that D-6 option A removes reopen. Option A keeps reopen, at any time, for the actor D-2 names. The stranding comes from D-2 B restricting that actor to the requester, plus the absence of an administrative exit, plus D-12 A. |
| Agreement | Agree on the design hazard of the recommended combination. Disagree on calling the current application permanently resolved. |
| Technical severity | High for the proposed model if those three options are implemented as written. Not a current functional failure on the repository seed. |
| Business impact | A departed requester, or a requester who does not open the ticket, leaves a record that cannot be closed and cannot be returned to work. Agents cannot treat `Resolved` as done, and the queue accumulates it. |
| Required corrective action | Do not implement D-2 B together with D-6 A and D-12 A until the same ruling names an escape: an administrative close, a window after which a named role may close, or a different confirmation actor. This document does not choose the escape. Separately, preserve resolution evidence on the existing reopen path (Section 2.11 and Section 4.2) without waiting for that ruling. Read deployed `IsTerminal` before demonstrating either close or reopen (Section 6). |
| Business approval required | Yes. The escape is part of D-2, D-6, and D-12, all Pending. |
| Blocks implementation | Blocks shipping that option combination. Does not block the current requester-or-assignee behaviour, and does not block the evidence-preservation control. |

#### 2.3.1 How a resolved ticket can return to processing without losing the resolution

The existing edge, when `IsTerminal` is false, is `Resolved → In Progress → Resolved`. `ChangeStatus` overwrites `ResolutionText` and `ResolvedOn` on the second resolve and writes the status audit with a null detail. The recovery rule below is a technical control on that edge. It does not decide who may use the edge, how long it stays open, or whether an administrator may close.

1. In the same transaction, before either column is replaced, write an audit row that contains the previous `ResolutionText` and the previous `ResolvedOn`. The status names alone are not the evidence.
2. If that prior text cannot be stored in `TicketAuditLog.Detail` (1000 characters) without cutting it, refuse the transition with a validation result. Do not truncate the evidence. A longer history needs a wider column or a resolution-history row, and that schema change is an IT authorisation, not a service-owner ruling on length.
3. On `Resolved → In Progress`, leave `ResolutionText` and `ResolvedOn` on the ticket until a later resolve replaces them, and only after step 1 has been written in that later transaction. The status name shows that work has resumed. Clearing the columns on reopen destroys the only copy the product has today.
4. `Closed`, `Cancelled`, and `Rejected` stay terminal. This control adds no exit from them.
5. If the deployed `Resolved` row is terminal, `ChangeStatus` returns "Cannot modify a ticket in terminal status" before the map is consulted. Both close and reopen are then dead. That flag is an environment fact (Section 6). Do not bypass `IsTerminal` in code to invent a recovery the deployed row forbids.

### 2.4 D-9 — duplicate CR prefix collision

| Field | Reconciliation |
|---|---|
| FreeBuff's claim | D-9 option A ("two change rows, standard off and normal both on") creates two `MST_RequestType` rows. Because allocation filters on `RequestTypeId` and labels with `Prefix`, two rows that share `CR` each issue `CR{yy}{MM}0001`. FreeBuff says neither the register nor Cursor identified this, and that D-9 is dependent on D-10 and D-11 rather than on D-4. |
| Source evidence | The filter and the label are the method in Section 2.2. Register D-4 already says: "Two request types that share a prefix can issue the same number because the read is filtered by `RequestTypeId`." Register D-9 option A does prescribe two change rows and does not require distinct prefixes. Its dependency line names D-10 and D-11. It does not name D-4. No request-type seed exists. Deployed prefixes were not read. |
| Cursor verification | **VERIFIED** mechanism. **AGREE** the latent collision: option A, applied to two rows with one prefix, produces duplicate public numbers. **DISAGREE** that the mechanism was unidentified. The register states it under D-4 and does not connect it to D-9. The missing artefact is the dependency, not the discovery of the race. |
| Agreement | Agree on the collision. Disagree on the "neither document identifies it" sentence. |
| Technical severity | High if option A is applied with a shared prefix. Latent until two active types share a prefix. Whether they already do is **UNVERIFIED**. |
| Business impact | A policy change that looks like reference data silently duplicates the number employees quote. Standard versus Normal then cannot be told apart by the ticket number. |
| Required corrective action | Keep D-9 Pending. Do not insert a second change type until Section 6 has listed deployed codes and prefixes, and until allocation is scoped to the public prefix (Section 4.1). If the owner wants two change types, the ruling has to name each prefix or accept one shared sequence. A `ChangeClass` column is out of this reconciliation. |
| Business approval required | Yes for the policy and for the prefixes (D-9 remains Pending). The allocator change that prevents the collision is the D-4 technical control. |
| Blocks implementation | Blocks applying D-9 option A. Does not block reading the deployed rows. |

### 2.5 D-14 — nondeterministic agent mapping

| Field | Reconciliation |
|---|---|
| FreeBuff's claim | `DispatchService.ResolveAssignee` walks item, subcategory, category, then branch default, each with `FirstOrDefault` and no `OrderBy`. Multiple active rows at the same specificity are possible because no unique natural key exists. "The mapped agent" is therefore not a predicate that can authorise automatic pickup. |
| Source evidence | `FindMapping`, `FindCategoryMapping`, and `FindBranchDefault` each end in `.Select(m => m.SupportAgentCode).FirstOrDefault()` with no `OrderBy`. The initial migration indexes `BranchId`, `CategoryId`, `SubCategoryId`, `ItemId`, and `SupportAgentCode` separately. It does not create a unique index on the natural key. A null assignee is returned when no row matches, and `Ticket.AssigneeCode` is nullable. `ItsmTicketAccess` states that support mapping is create-time dispatch, not a live access list. `ReassignTicket` checks `EmployeeStatus` only. |
| Cursor verification | **VERIFIED. AGREE** with FreeBuff and with Cursor FR-09. Unordered `FirstOrDefault` is deterministic only when zero or one active row matches. The schema allows more than one. Whether duplicates exist in a database was not queried. |
| Agreement | Agree. |
| Technical severity | High as an authorisation input. Medium as today's dispatch: the wrong agent is chosen only when duplicate active rows exist, and that population is **UNVERIFIED**. |
| Business impact | Two agents can be "the" owner of one branch and category, and a second create can assign the other. A take-ownership rule built on that result grants the right to whichever row the engine returned. |
| Required corrective action | Before any automatic pickup or "mapped agent may take an unassigned ticket" rule: a unique constraint on the active natural key (branch, category, subcategory, item, branch-default flag), or an explicit ordering column that every read uses. Count duplicate active rows first (Section 6). D-14 option A (one agent, no group entity) can remain the register's recommendation and still stays Pending. Option C, a group entity, stays out of this build unless the owner rejects A. |
| Business approval required | Yes for who may take unassigned work (D-14 Pending). No for refusing to automate pickup on an unordered read. |
| Blocks implementation | Blocks automatic pickup and blocks implementing D-2 B, because that option removes the requester and needs a deterministic taker for unassigned tickets. |

### 2.6 Rejection reason length — unhandled error

| Field | Reconciliation |
|---|---|
| FreeBuff's claim | `TicketApprovals.Comments` is `nvarchar(500)`. `txtRejectionReason` has no `MaxLength`. `RejectTicket` assigns `reason.Trim()` with no length check. EF6 throws `DbEntityValidationException`, which FreeBuff says derives from `DbUpdateException`. The service catches `DbUpdateConcurrencyException`, then `Exception`, rolls back, and rethrows. `btnReject_Click` has no try/catch, so the approver gets an ASP.NET error page, no status change, and no audit row. |
| Source evidence | Entity `[StringLength(500)]` on `Comments`. Migration `Comments = c.String(maxLength: 500)` at line 139. `TicketDetail.aspx` line 318 declares `txtRejectionReason` with no `MaxLength`; `txtOverrideReason` at line 296 has `MaxLength="500"`. `RejectTicket` requires non-blank text, then sets `approval.Comments = reason.Trim()` and passes `reason.Trim()` to `AddAudit`, which truncates the audit copy to 1000. The catch that returns a message is only `DbUpdateConcurrencyException`. The following `catch (Exception)` rolls back and rethrows. `btnReject_Click` calls the service and `ApplyResult` with no try/catch. `ItsmDbContext.SaveChanges` does not set `ValidateOnSaveEnabled = false`. |
| Cursor verification | **VERIFIED** path and outcome. **DISAGREE** with the inheritance sentence. In Entity Framework 6, `DbEntityValidationException` and `DbUpdateException` both extend `System.Data.DataException`. They are siblings. The page outcome does not depend on that derivation: any exception other than `DbUpdateConcurrencyException` is rethrown, the transaction is rolled back, and the detail page does not convert it into `ShowMessage`. A reason of 501 characters or more therefore produces an unhandled page exception. The audit row is not committed. `ApplyResult` never runs. The exact error chrome depends on `customErrors`, which lives in `Web.config` and was not read. |
| Agreement | Agree on the defect. Disagree on the exception's base type. |
| Technical severity | Medium. The blank-reason check already works. The failure is the over-length path, and it discards a legitimate rejection. |
| Business impact | An approver who writes a long reason sees a framework error instead of "reason must be 500 characters or fewer." The ticket stays in `Pending Approval`. The rejection is not recorded. |
| Required corrective action | Before `SaveChanges`, if the trimmed reason length is greater than 500, return `TicketUpdateResult.Fail` with that limit. Set `MaxLength="500"` on `txtRejectionReason` as a client hint. Do not truncate the stored reason. The same shape applies to any unexpected exception on approve, reassign, override, comment, and status change, because those handlers also call the service directly. A page-level catch that shows a generic message is a separate hardening item; the length check is the specific fix. |
| Business approval required | No. D-11 (terminal rejection versus in-place resubmit) stays Pending. The length check does not choose that policy. |
| Blocks implementation | Blocks treating rejection as a reliable control. Does not block the D-11 ruling. |

### 2.7 Stored XSS — ticket detail rendering

| Field | Reconciliation |
|---|---|
| FreeBuff's claim | `LoadTicket` assigns `Title`, `Description`, and `ResolutionText` to `Label.Text`. `TicketDetail.aspx` renders `lblDescription` with no `HtmlEncode`. Comments and approval comments are encoded. Stage 2 Part A says `Label` auto-encodes `<` and `>`, and relying on that would ship stored XSS. FreeBuff says `Label.Text` is written unencoded, and that this refutes Cursor. |
| Source evidence | `TicketDetail.aspx.cs` `LoadTicket` assigns `lblTitle.Text = dto.Title`, `lblDescription.Text = dto.Description` (or a fixed empty placeholder), and `lblResolution.Text = dto.ResolutionText`. The same method assigns requester, assignee, category, branch, impact, urgency, and CSAT feedback to labels with no `HtmlEncode`. `TicketDetail.aspx` line 226 is a bare `lblDescription`. Line 367 encodes `CommentText`. The approval repeater encodes `Comments`. The creation mail encodes `Title` with `WebUtility.HtmlEncode` and does not encode the link, which is a literal plus an integer. No page directive on `TicketDetail.aspx` sets `ValidateRequest="false"`. |
| Cursor verification | **VERIFIED** that the application performs no encode on those label assignments. **AGREE** that `System.Web.UI.WebControls.Label` writes `Text` to the response without `HtmlEncode`. Stage 2 Part A line 30 says the opposite: "Label.Text auto-encodes `<` and `>` only." That sentence is wrong. Cursor's own later validation already corrected it: FR-02 says "`Label` is an unencoded sink" and records that no exploit was executed. **DISAGREE** with FreeBuff's paraphrase that Stage 2 says encoding is unnecessary. Stage 2 line 31 still requires explicit `HttpUtility.HtmlEncode` on every user-controlled field. The hazard is the false auto-encode sentence, which would let an implementer trust `Label` for title and resolution. Exploitability of a stored payload is **UNVERIFIED**: request validation is a `Web.config` fact and was not read. A payload that cannot be posted can still be rendered if it is already in the column. |
| Agreement | Agree the sink is unencoded and that the Stage 2 auto-encode sentence is false. Disagree that Stage 2 waived encoding. |
| Technical severity | High as an unencoded HTML sink for title, description, and resolution. Exploitability through the create form is **UNVERIFIED** until request-validation mode is known. |
| Business impact | Anyone who can open the ticket — requester, assignee, pending approver, or a same-company `ITSM.Ticket.View` holder — receives the rendered description. A stored script runs in that viewer's session. |
| Required corrective action | Apply `HttpUtility.HtmlEncode` when assigning every user-controlled or user-influenced plain-text label on the detail page, including title, description, resolution, CSAT feedback, and the name fields. Leave the repeater encodes in place. Do not pre-encode into the database. Do not rely on `Label`. Confirm there is no second encode on those labels, because the control does not encode on its own. |
| Business approval required | No. |
| Blocks implementation | Blocks calling the detail page safe, and blocks any demo that asks an owner to trust text rendered there. Does not block the policy rulings. |

### 2.8 Incorrect `sp_getapplock` claim

| Field | Reconciliation |
|---|---|
| FreeBuff's claim | Cursor Stage 2 Part A says transaction-owned `sp_getapplock` is used for number allocation. Part B says it is not. The source has no `sp_getapplock`. An implementer who trusts Part A will believe the race is solved. |
| Source evidence | `ITSM_STAGE_2_INDEPENDENT_REVIEW_PLAN.md` line 36: "Transaction-owned `sp_getapplock` is used for number allocation." Line 80: "No `sp_getapplock` is used in the current implementation." A search of `AnmolDristi/ITSM` finds no `sp_getapplock`, no app-lock resource name, and no `UPDLOCK`. `GenerateTicketNumber` is the read-modify-write in Section 2.2. The only `ExecuteSqlCommand` in the module inserts `NotificationQueue`. |
| Cursor verification | **VERIFIED. AGREE** with FreeBuff. Part A and Part B of the same plan contradict each other. Part B matches the source. Part A would mislead an implementer. `ITSM_Cursor_Independent_Validation.md` FR-01 describes the race and does not claim an app lock. The false claim lives in the Stage 2 plan, not in the later validation. |
| Agreement | Agree. |
| Technical severity | High as a documentation hazard. The code defect is Section 2.2. This finding is the false statement that the defect is already fixed. |
| Business impact | A build that "keeps the existing lock" ships the race. Duplicate numbers then enter mail and audit. |
| Required corrective action | Treat Stage 2 Part A item 4 as withdrawn for allocation. Any future lock has to be added; it cannot be preserved. This reconciliation does not edit the Stage 2 plan. |
| Business approval required | No. Adding the lock is the D-4 technical control and still needs the authorisation in Section 2.2. |
| Blocks implementation | Blocks any plan that assumes allocation is already serialised. |

### 2.9 Audit text truncation

| Field | Reconciliation |
|---|---|
| FreeBuff's claim | `TicketAuditLog.Detail` is `nvarchar(1000)`. `AddAudit` calls `Truncate(detail, 1000)`. `txtComment` allows 2000 characters. `AddComment` stores the full text on `TicketComments` and passes the full text to the audit. The audit copy is cut with no error. The register warns about this only as a future risk for the resolution summary. |
| Source evidence | Migration `202608201200000_AddTicketAuditLog.cs` line 19: `Detail` max length 1000. `AddAudit` assigns `Detail = Truncate(detail, 1000)`. `Truncate` returns `Substring(0, maxLength)` when longer. `AddComment` passes `(isInternal ? "[Internal] " : "") + commentText.Trim()`. `TicketDetail.aspx` line 339 sets `MaxLength="2000"` on `txtComment`. There is no server-side length check. `TicketComments.CommentText` is `c.String(nullable: false)` with no max length. `ChangeStatus` passes `null` as the status-change detail, so the resolution summary is omitted rather than truncated. |
| Cursor verification | **VERIFIED. AGREE.** The comment row and the audit row can disagree, and the caller is not told. A client can post more than 2000 characters because `MaxLength` is not enforced on the server. The register's D-17 warning is about a future audit copy of the summary. The comment path already truncates. |
| Agreement | Agree. |
| Technical severity | Medium for audit integrity. The business comment is complete. The trail is not. |
| Business impact | A dispute that relies on the audit row sees a cut comment. Internal notes lose the tail first because the `"[Internal] "` prefix consumes part of the 1000 characters. |
| Required corrective action | For comments, either refuse text whose audit copy would exceed 1000, or store the audit detail without silent cutting (a wider column or a pointer at the comment row). State the chosen ceiling in the validation message. Do not keep `Truncate` as the way evidence is preserved. The same refusal applies when resolution text is copied to the audit row (Section 4.2). D-17's minimum length stays Pending; this fix does not invent one. |
| Business approval required | No for stopping silent truncation. Yes if the owner wants a minimum resolution length (D-17). |
| Blocks implementation | Blocks treating `TicketAuditLog` as a complete copy of comments or, once copied, of resolutions. |

### 2.10 Approval panel visibility versus server authorisation

| Field | Reconciliation |
|---|---|
| FreeBuff's claim | `GetTicketDetail` sets `CanApprove` from any pending approval row for the actor. It does not exclude the requester and does not require status `Pending Approval`. The page binds `pnlApprovalActions.Visible` to that flag. Case (a): a requester who is also the IT Head sees Approve and receives "Separation of Duties violation." Case (b): a stale pending row on a ticket that has left `Pending Approval` shows buttons that always fail. |
| Source evidence | `TicketDetailService` lines 176–180 set `CanApprove` from `ApproverCode` and `ApprovalStatus == "Pending"` only. `ItsmTicketAccess.CanApproveTicket` returns false when the actor is the requester, then requires a pending row. `ApproveTicket` and `RejectTicket` both refuse the requester, both require current status name `Pending Approval`, and `ApproveTicket` refuses an IT Head row while a Manager row is still pending. `LoadForApprove` calls `CanApproveTicket`, so the service refusal happens even if the panel was shown. Create inserts an IT Head row without comparing the requester to the IT Head. `ApproveTicket` moves the ticket to `In Progress` only when no other pending row remains. `RejectTicket` marks every other pending row `Rejected`. `Pending Approval` is not a key in `TicketWorkflow`, so `ChangeStatus` cannot leave it. |
| Cursor verification | **VERIFIED** that the panel predicate is weaker than the server. **AGREE** case (a): the requester-IT-Head sees the buttons; the click returns the separation-of-duties message through `ApplyResult`, because that path returns `Fail` rather than throwing. **AGREE** a second, sharper mismatch the review names less clearly: the IT Head sees Approve while the Manager row is still pending, and the server then returns "Manager approval must be completed before IT Head can approve." **DISAGREE** case (b) as a state the current methods produce. Approve keeps the ticket in `Pending Approval` until the last pending row is cleared. Reject clears the other pending rows in the same transaction. A stale pending row on a non-pending ticket would require a write outside these methods. That population was not queried. |
| Agreement | Agree the panel offers actions the server will refuse. Disagree that the application paths leave a stale pending row after a successful approve or reject. |
| Technical severity | Medium. Authorisation holds on the server. The screen trains the approver to click a button that cannot succeed. |
| Business impact | The IT Head, and a requester who is the IT Head, see a control the transaction will reject. Support time goes into explaining a guaranteed failure. The requester-IT-Head ticket also has no map exit, which is the D-10 strand and remains a Pending business question. |
| Required corrective action | Set the panel from the server predicate: actor is not the requester, a pending row exists for the actor, the ticket status is `Pending Approval`, and an IT Head row is hidden while a Manager row is pending. Keep the server checks. Do not add an override that lets the requester approve. Who approves when the requester is the IT Head stays in Pending D-10. |
| Business approval required | No for aligning the panel with the existing server rules. Yes for an administrative bypass of a stranded IT-Head ticket (D-10). |
| Blocks implementation | Blocks a demonstration of approval. Does not block the D-10 ruling. |

The work panel is a related gap and is not the approval finding. `pnlWorkActions.Visible` is set from "non-terminal and not Pending Approval" with no `CanProcessTicket` test (`TicketDetail.aspx.cs` line 282). A same-company viewer, or a pending approver, sees status, reassignment, and priority controls the server will refuse. Hiding that panel unless `CanProcessTicket` is true is a technical control in Section 4. It does not decide D-2.

### 2.11 Resolution evidence destroyed on re-resolution

| Field | Reconciliation |
|---|---|
| FreeBuff's claim | `ChangeStatus` sets `ResolutionText` and `ResolvedOn` when the target is `Resolved` and does not clear them on the way out. The map allows `Resolved → In Progress → Resolved`. The status audit is written with `Detail = null`. A second resolve replaces the summary and the timestamp. The earlier text is not in the audit. That destroys the D-18 duplicate sentence and any reopen reason expected to live in the summary. |
| Source evidence | `ChangeStatus` lines 98–110: the two columns are assigned only when the target name is `Resolved`. There is no assignment when the target is `In Progress` or `Closed`. `AddAudit(..., null, actorCode)` follows. `TicketWorkflow` line 26 lists `Closed` and `In Progress` as the successors of `Resolved`. `ResolutionText` is an unbounded string on `Tickets`. Register D-17 says the application "does not expose an edit of `ResolutionText` except by resolving again, which the map does not allow from `Resolved`." That sentence does not match the map. The two-step path is allowed. |
| Cursor verification | **VERIFIED. AGREE** with FreeBuff FB-DEF-06. The current code destroys the previous summary on re-resolve. Cursor FR-10 confirms the null audit detail and stops short of the overwrite. The register's "map does not allow" sentence is the inaccurate one. |
| Agreement | Agree. |
| Technical severity | High for evidence. The latest summary remains on the ticket. The previous summary does not survive in any ITSM table. |
| Business impact | A duplicate reference, a workaround, or a disputed outcome can be replaced by a later sentence with an audit row that says only "Resolved → In Progress" and "In Progress → Resolved." D-18 option A, text in the summary, is erasable evidence while this stands. |
| Required corrective action | Section 2.3.1. Implement that write-before-replace on the existing edge. Do not wait for D-6. Do not implement D-18's text convention until D-4 is under a uniqueness control and the prior summary is in the audit. |
| Business approval required | No for preserving the text the application already stored. Yes for the reopen window, the actor, and whether a duplicate sentence is acceptable (D-6, D-2, D-18, all Pending). |
| Blocks implementation | Blocks D-18 and blocks any claim that reopen is auditable. Does not block ruling those rows. |

### 2.12 Other material findings, verified in short

These were re-read because FreeBuff treats them as material. They are not given the nine-field treatment above.

| ID | Claim | Verification | Class |
|---|---|---|---|
| FB-DEF-07 | Creation mail href is `~/ITSM/UI/TicketDetail.aspx?id=` plus the id. | **VERIFIED** in `EnqueueTicketCreatedNotification`. `~` is not expanded in the string written to `NotificationQueue`. | Defect. The fix needs an origin from D-8, which stays Pending. |
| FB-DEF-09 | `Dristi.Master.cs` forces `ITSM_Module`, `ITSM_TicketList`, and `ITSM_CreateTicket` visible after permission visibility. | **VERIFIED** as described. Removing the override before grants exist hides the menu. | Confirmed source fact. Corrective action waits on D-3 and a grant read. |
| FB-DEF-10 | `ApprovalConfig.aspx.cs` does not require `WORKMAN`, then records `CurrentEmployeeCode`, which is empty, and the service throws. | **VERIFIED** shape. Narrow: it needs a session that passed `USERID` and `BRANCH` without `WORKMAN`. | Technical consistency fix. Low. Does not block the workflow. |
| S-02 | Queue search matches `Description`, which the grid does not show. | **VERIFIED** in `BuildQueueQuery`. | Technical control. Remove `Description` from the predicate. Does not decide D-1. |
| S-10 | No anti-forgery token and no `ViewStateUserKey` under `AnmolDristi/ITSM`. | **VERIFIED** absence in the ITSM tree. Whether ViewState MAC makes a cross-site post infeasible was not tested. `Web.config` was not read. | Technical gate before D-2 narrows the set of valuable sessions. |
| S-01 / D-1 | All Open has no `CompanyId` predicate. | **VERIFIED**. The register and the code comments describe a shared desk. | Pending business decision. Not reclassified as a defect. |
| Null `CompanyId` | Mutations call `RequireRecordedTenant` and refuse. Queues still list the rows. | **VERIFIED**. Fail-closed for writes. Operational dead end for the desk. | D-15 stays Pending. Queue exclusion is an owner choice tied to D-1. |
| D-17 register sentence | A second resolve is impossible because the map disallows it from `Resolved`. | **REFUTED.** The map allows `Resolved → In Progress → Resolved`. | Documentation error in the register. This reconciliation does not edit the register. |

---

## 3. Cursor versus FreeBuff disagreements

These are the places this pass does not adopt a sentence as written. The underlying control, where one exists, stays in Section 4.

| # | Topic | What was claimed | What the source supports | Adopted position |
|---|---|---|---|---|
| 1 | SLA persistence | FreeBuff: persist start and due now (D-7 option C), and withhold pause and mail until the branch key is read. | The due value is computed from an argument whose relationship to `VMS_PlantTimings.Branch_ID` is not established. The duration is clock time to midnight, not plant close. | Do not persist SLA deadlines until the BranchId / Branch_ID relationship is established in a named environment. D-7 stays Pending. Register option B is not adopted either. |
| 2 | Permanent `Resolved` | FreeBuff: current design defect; D-6 A sets no window and the ticket can never leave `Resolved`. | Current map and seed allow the requester or the assignee to close or reopen, unless the deployed row is terminal. D-6 A keeps reopen at any time for the actor D-2 names. | Design hazard of D-2 B + D-6 A + D-12 A with no escape. Not current behaviour. D-6 A is not the clause that removes reopen. |
| 3 | Prefix collision "unidentified" | FreeBuff: neither document identified that D-9 A duplicates numbers. | Register D-4 current behaviour already states the shared-prefix collision. D-9's dependency list omits D-4. | Collision confirmed. The gap is the missing dependency. |
| 4 | Exception inheritance | FreeBuff: `DbEntityValidationException` derives from `DbUpdateException`. | The two EF6 types both derive from `DataException`. Only `DbUpdateConcurrencyException` is turned into a page message. | Defect confirmed. Base-type sentence not adopted. |
| 5 | Stage 2 and encoding | FreeBuff: Stage 2 says encoding is unnecessary, and Cursor is thereby refuted. | Stage 2 says `Label` auto-encodes angle brackets, which is false, and also requires explicit `HtmlEncode`. Cursor validation FR-02 already calls `Label` an unencoded sink. | Sink is unencoded. Auto-encode sentence is withdrawn for any implementer. The "encoding is unnecessary" paraphrase is not what Stage 2 wrote. |
| 6 | Stale approval row | FreeBuff case (b): a pending row remains after the ticket leaves `Pending Approval`. | `ApproveTicket` leaves the status in `Pending Approval` while any other row is pending. `RejectTicket` rejects the other pending rows in the same transaction. `ChangeStatus` cannot leave `Pending Approval`. | Panel-versus-server mismatch is real for the requester and for the IT Head who is early. The stale-row case is not produced by these methods. |
| 7 | `sp_getapplock` | FreeBuff: Part A is false. | Part A line 36 asserts the lock. Part B line 80 and `GenerateTicketNumber` show no lock. | FreeBuff is right. Part A item 4 is not an implementation. |
| 8 | Register D-17 | The register says the map does not allow resolving again from `Resolved`. | `Resolved → In Progress → Resolved` is a legal pair of edges, and the second resolve overwrites the summary. | FreeBuff FB-DEF-06 is right. The register sentence is wrong. The register is not edited by this document. |

Where this pass agrees with both documents, it is not repeated here: the status seed exists and sets `Resolved` non-terminal; a listed cross-company ticket can be opened by a relationship holder; SLA plant hours govern the start and not the duration; null `CompanyId` is fail-closed for mutation.

---

## 4. Mandatory technical remediation register

These controls do not require a service-owner ruling on D-1 through D-18. They do not flip any Pending row to Approved. A schema change still needs IT leadership authorisation, and any control that can fail on existing rows waits on Section 6.

| ID | Control | Depends on a deployed read | Blocks |
|---|---|---|---|
| T-1 | Serialise ticket-number allocation on the public prefix + year + month. Add a unique constraint on `TicketNumber`. Retry the whole create a bounded number of times. Refuse a trimmed prefix that is not exactly two characters. Return a controlled error above 9999. Do not renumber existing tickets. | Yes. Duplicate `TicketNumber` count, and the set of prefixes whose trimmed length is not 2. | D-9 A, D-18, any quote of the human number |
| T-2 | Reject a rejection reason longer than 500 characters with `TicketUpdateResult.Fail`. Set the textbox `MaxLength`. | No | Reliable rejection |
| T-3 | HTML-encode user-controlled detail labels at render. Do not trust `Label`. | Request-validation mode explains exploitability. The encode does not wait on it. | A safe detail page |
| T-4 | On reopen and on re-resolve, write the previous `ResolutionText` and `ResolvedOn` into the audit in the same transaction before replacing them. Refuse when the evidence would be truncated. | Deployed `IsTerminal` for `Resolved`, before anyone demonstrates the edge. The write itself is safe on the current code path. | D-18, auditable reopen |
| T-5 | Stop silent `Truncate` of comment text into `Detail`. Refuse or store the full audit copy. | No | Audit completeness |
| T-6 | Show approval buttons only when the server would accept that actor: not the requester, status is `Pending Approval`, and IT Head is not shown while Manager is pending. | No | Approval demo |
| T-7 | Show the work panel only when `CanProcessTicket` is true. | No | False actions on the detail page |
| T-8 | Remove `Description` from the queue search predicate. | No | Description search oracle. Does not decide who may see the queue. |
| T-9 | Make `MST_SupportMapping` deterministic: unique active natural key, or an `OrderBy` every dispatch read shares. | Yes. Count of duplicate active rows at the same specificity. | Automatic pickup, D-2 B |
| T-10 | Add a per-session anti-forgery check on the six mutations and the approval-configuration posts before actor rights are narrowed. | ViewState MAC was not read. The absence of a token is source-verified. | Safe narrowing under D-2 |
| T-11 | Withhold SLA columns and breach mail. | The withhold is the control. The key comparison is Section 6. | Nothing else, provided nobody stores the due date |

T-1 is the D-4 technical half. Specifying it here is not an approval of D-4 and not permission to run a migration.

### 4.1 Numbering (detail for T-1)

The public number stays `prefix + yy + MM + four digits` until the service owner rules otherwise. Allocation must:

- Lock one resource for that prefix and that year-month for the duration of the create transaction.
- Compute the max across every `TicketNumber` that starts with that prefix, not across one `RequestTypeId`.
- Insert under a unique constraint on `TicketNumber`.
- On a uniqueness conflict, roll back, open a new context, and retry the whole create a small fixed number of times, then return an error.
- Refuse the create when the trimmed prefix length is not 2, so the parser, the column, and the displayed form describe the same string.
- Refuse the create when the next sequence is greater than 9999, with a message that the monthly ceiling is exhausted. Do not emit an 11-character number.

Compatibility: existing values, including any nine-character numbers already stored, are left as they are. The unique index is created only after a read shows no duplicate `TicketNumber`. Repair of duplicates is a separate data authorisation. It is not part of T-1's specification and is not authorised here.

### 4.2 Workflow recovery (detail for T-4)

Use the procedure in Section 2.3.1. The actor and the window stay in Pending D-2 and D-6. `Closed`, `Cancelled`, and `Rejected` gain no exit. If Section 6 finds `Resolved.IsTerminal = 1`, stop and report that fact. Do not clear the flag from application code.

### 4.3 SLA (detail for T-11)

Do not recommend, and do not add, persisted SLA deadlines until the relationship between `MST_UserMaster.BranchId`, `MST_Branch.Id`, `MST_Branch.Branch_ID`, and `VMS_PlantTimings.Branch_ID` is established by a read in a named environment. Mail-only calculation may continue. Pause and breach mail stay behind D-5, D-6, D-7, and D-8, all Pending.

### 4.4 Agent mapping (detail for T-9)

Do not implement automatic pickup, and do not implement "the mapped agent may take this ticket," until one active row is guaranteed for each specificity the cascade queries, or until every query orders by an explicit column. D-14's choice of one agent versus a group stays Pending.

### 4.5 Security (T-2, T-3, T-5, T-6, T-7, T-8, T-10)

Output encoding, approval-panel alignment, rejection-length validation, and audit preservation are the four security controls the user asked to evaluate. All four are source-confirmed gaps. None requires a business ruling. Anti-forgery and the work-panel gate belong in the same technical set because narrowing who may act, under a future D-2, raises the value of the sessions that remain.

---

## 5. Business decisions still pending

Every identifier is preserved. Status is unchanged. Recommendations in the register and in the FreeBuff review are advice. This table does not convert them into rulings.

| ID | Question | Register recommendation (advice only) | This reconciliation | Status |
|---|---|---|---|---|
| D-1 | May All Open show other companies' titles and names? | A, shared desk, written acceptance | Unchanged. T-8 removes the description search without deciding the desk. | Pending |
| D-2 | May the requester resolve, reassign, or override priority? | B, assignee processes, requester confirms | Do not implement B without a named escape for a requester who never confirms, and without T-9. Evidence preservation (T-4) can proceed on today's gate. | Pending |
| D-3 | Must create require the permission and an active employee? | B, active employee now, permission later | Unchanged. The master-page override stays until grants are known. | Pending |
| D-4 | How should duplicate ticket numbers be prevented? | A, unique index, locked allocation, keep the format, accept 9999 | Technical half specified as T-1 and not approved. Format and ceiling still need the service owner. | Pending |
| D-5 | Is On Hold plus a reason enough? | A, one pause status, mandatory class | Unchanged. Storage of the class must not reuse a truncated `Detail` (T-5). | Pending |
| D-6 | Is there a reopen window, and does reopen restart the SLA? | A, any time, mandatory reason, extend due, do not reset start | Reopen remains available under A for the D-2 actor. The window and the SLA effect stay unresolved. T-4 is independent. No SLA effect until T-11 is lifted. | Pending |
| D-7 | Are response SLA, stored due time, pause, and one breach mail in scope? | B, store, pause, one mail; defer response SLA | Not adopted. Do not persist until Section 6.3. FreeBuff option C is not adopted. | Pending |
| D-8 | What absolute base URL should mail links use? | A, one operations-supplied HTTPS origin | Unchanged. FB-DEF-07 stays until the owner writes the origin. This document does not invent a host. | Pending |
| D-9 | IR off, SR and CR Normal on, CR Standard off? | C, then A after a read | Unchanged, with an added dependency on D-4 / T-1. Two change rows need distinct prefixes or one shared prefix-scoped sequence. | Pending |
| D-10 | Is the IT Head the only change authority, with no CAB? | A, plus guards and audit | Unchanged. Panel alignment (T-6) does not create a bypass for the requester who is the IT Head. | Pending |
| D-11 | Is rejection terminal, with a new ticket as the only resubmit? | A | Unchanged. T-2 is required whichever option is later chosen. Quote the old number only after T-1. | Pending |
| D-12 | Is automatic closure authorised? | A, no job | Unchanged. Combined with D-2 B and no escape, this is the strand in Section 2.3. | Pending |
| D-13 | Is CSAT required to close? | A, no | Unchanged. | Pending |
| D-14 | Support group, or one agent per branch and category? | A, one agent, plus take-ownership | Take-ownership waits on T-9. The entity question stays Pending. | Pending |
| D-15 | What happens to null `CompanyId` tickets? | A, frozen, no HR backfill | Unchanged. Whether they leave the operational queue is part of this ruling with D-1. | Pending |
| D-16 | Is a department snapshot required? | A, no | Unchanged. | Pending |
| D-17 | Minimum length of a resolution summary? | A, non-blank, refuse over-length on a future audit copy | The future copy is a current overwrite defect (T-4). Minimum length stays Pending. The register sentence that a second resolve is impossible is inaccurate and is left in place. | Pending |
| D-18 | May an agent mark a duplicate without a link column? | A, text in the summary, after D-4 | Blocked on T-1 and T-4. The column question stays Pending. | Pending |

No row in this table is Approved, Deferred, or signed. An empty owner-ruling cell in the register remains empty.

---

## 6. Environment-dependent verification requirements

None of these reads was performed. No UAT or LIVE connection is authorised by this document. Each read is a `SELECT` or a catalogue query in a named environment, recorded with the environment name and the time. A code comment that says a fact was "verified" is not a substitute.

| # | Read | Why it gates work | Blocks |
|---|---|---|---|
| 6.1 | `itsm.MST_TicketStatus` for `Resolved`: `IsTerminal`, `IsActive`, and whether more than one row shares the name | `ChangeStatus` trusts the flag before the map. A terminal `Resolved` makes close and reopen dead. The seed does not update an existing table. | Any demonstration of close or reopen |
| 6.2 | `itsm.MST_RequestType`: `Code`, `Prefix`, `DATALENGTH`/`LEN` of `Prefix`, both approval flags, `IsActive` | D-9 cannot be compared with production. A prefix whose trimmed length is not 2 stalls T-1's current generator at `0001`. | D-9, and the prefix half of T-1 |
| 6.3 | For a sample of branches: `MST_UserMaster.BranchId`, `MST_Branch.Branch_ID`, `MST_Branch.Id`, and `VMS_PlantTimings.Branch_ID`, showing which numbers are equal | This is the relationship T-11 is waiting on. | Any persisted SLA deadline |
| 6.4 | Count of `itsm.Tickets` rows that share a `TicketNumber`, and count of numbers whose length is not 10 | A unique index fails if duplicates exist. | Creating the T-1 index |
| 6.5 | Count of active `MST_SupportMapping` rows that share branch, category, subcategory, item, and branch-default flag | T-9's unique index fails if duplicates exist, and dispatch is already non-deterministic if the count is greater than zero. | Automatic pickup |
| 6.6 | Count of `Tickets.CompanyId` IS NULL | Sizes the frozen population for D-15. Does not authorise a backfill. | Nothing technical. Informs the owner. |
| 6.7 | Role grants for `ITSM.Queue.View`, `ITSM.Ticket.View`, `ITSM.Approval.View`, `ITSM.ApprovalConfig.Manage`, `UI.ITSM_CreateTicket.View` | Decides who currently holds the queue disclosure and whether removing the master-page override would hide the menu. | FB-DEF-09 removal, D-3 option A |
| 6.8 | Request-validation mode and ViewState MAC, read by an authorised person under the configuration boundary | Decides practical exploitability of the unencoded labels and of the missing anti-forgery token. This reconciliation did not open `Web.config`. | Severity confirmation only. T-3 does not wait. |

---

## 7. Deferred improvements

Safe to leave out of the first technical controls. Each stays subject to its Pending decision if an owner later pulls it forward.

| Item | Why it can wait |
|---|---|
| CSAT form or a closure block (D-13) | Columns exist, no writer, closure does not read them. |
| Department snapshot or a live department label (D-16) | No ticket column and no ITSM read of `Session["U_SITE"]`. |
| Support-group entity (D-14 option C) | The cascade can stay one agent once T-9 makes that agent deterministic. |
| CAB, quorum, change window (D-10 option C) | No table exists. One IT Head is the current mechanism. |
| Automatic closure job (D-12) | No job exists. The entity comment about 72 hours is not a specification. |
| Response-time SLA and an at-risk mail | Behind D-7, which is itself behind Section 6.3. |
| Parent-ticket foreign key (D-18 option C) | Text convention is already blocked on T-1 and T-4. A column is a later product. |
| Full notification matrix in the target specification Section 14 | One creation mail exists and its link is broken. Fixing the link waits on D-8. Additional events wait on that ruling. |
| In-place edit and resubmit after rejection (D-11 option B) | The map has no path back to `Pending Approval`. |
| Mapping maintenance screen | Mappings are a script. Who maintains them is an operational question the register left outside the 18. It should be answered before T-9's rows become an authorisation source, and it is not itself a nineteenth workflow decision. |
| `ApprovalConfig` session shape (FB-DEF-10) | Real, narrow, and independent. It can ship with any later edit of that page. |

---

## 8. Revised implementation readiness assessment

**Not ready.**

Technical readiness is the state of the code and the migrations. Business approval is the state of D-1 through D-18. Both are closed gates.

| Gate | State |
|---|---|
| Business decisions D-1–D-18 | Pending. Unchanged by this document. |
| Approving all 18 | Would not authorise implementation. T-1 through T-10 would still be open, and Section 6 would still be unread. |
| Ticket numbers | Defect confirmed. Control specified. Index not applicable until 6.4. |
| SLA storage | Withheld. Key relationship unread. |
| Automatic pickup | Withheld. Mapping is non-deterministic in source. |
| `Resolved` recovery | Current code can leave `Resolved` when the seed flag holds. Evidence of the previous resolution is destroyed on the way back. Deployed flag unread. |
| Detail page encoding | Unencoded sink confirmed. Exploit path through request validation unread. |
| Rejection | Blank reason is validated. Over-length reason is an unhandled exception. |
| Approval authorisation | Server checks hold. The panel shows a wider set of buttons. |

A demonstration of the operating model is credible only after Section 6 is recorded against a named environment, T-1 through T-7 are in the build that is demonstrated, and D-1, D-2, D-14, and D-15 have a written ruling that includes an escape from `Resolved`. This document supplies none of those signatures and applies none of those controls.

---

## 9. Recommended implementation sequence

Order of specification and, later, of authorised work. Nothing in this sequence is permission to edit code, run SQL, or connect to UAT or LIVE.

| Step | Work | May start when | Still Pending after the step |
|---|---|---|---|
| 0 | Section 6 reads, in a named environment, by a person authorised to read it | A separate read authorisation exists | All 18 |
| 1 | T-3, T-2, T-5, T-6, T-7, T-8 | Immediately, under a change authorisation. They do not need an owner ruling or a data repair. | All 18 |
| 2 | T-4, write-before-replace of resolution evidence | With step 1. Demonstrate reopen only after 6.1. | D-2, D-6, D-17, D-18 |
| 3 | T-1 numbering | After 6.4 shows no duplicates, or after a separately authorised repair. IT leadership authorises the schema change. Format stays as issued unless D-4 says otherwise. | D-4's format and ceiling |
| 4 | T-9 mapping uniqueness | After 6.5. | D-14's take-ownership rule |
| 5 | T-10 anti-forgery | Before any build that narrows `CanProcessTicket` | D-2 |
| 6 | Owner ruling set: D-1 with D-15, then D-14, then D-2 with an explicit escape, considered with D-6 and D-12 | After the owners meet. This document does not hold the meeting. | Until they sign |
| 7 | D-9 data change | After 6.2 and after T-1 is in place | The policy itself |
| 8 | D-8 origin, then the creation-mail link | When operations names the origin | Section 14's other events |
| 9 | SLA persistence | Only after 6.3 establishes the branch-key relationship, and only if D-7 is then ruled | D-5 pause and D-8 if mail is in the ruling |
| 10 | D-11, D-5, D-10, D-17, D-18, D-3, D-13, D-16 | In any order once their technical dependencies above are met | Each row, until signed |

Steps 1 through 5 are technical. Steps 6 through 10 are owner rulings plus the data or product changes those rulings name. Completing step 6 does not skip steps 1 through 5.

---

## Appendix A — Source read in this pass

| Area | File | What was checked |
|---|---|---|
| Numbering and create | `AnmolDristi/ITSM/BLL/TicketService.cs` | `GenerateTicketNumber`, branch resolution, SLA versus dispatch arguments, mail link, transaction |
| SLA | `AnmolDristi/ITSM/BLL/SlaService.cs` | `GetPlantTiming`, start versus duration |
| Dispatch | `AnmolDristi/ITSM/BLL/DispatchService.cs` | Four unordered `FirstOrDefault` reads |
| Workflow | `AnmolDristi/ITSM/BLL/TicketWorkflow.cs` | `Resolved` successors; `Pending Approval` absent |
| Mutations | `AnmolDristi/ITSM/BLL/TicketUpdateService.cs` | `ChangeStatus`, `RejectTicket`, `ApproveTicket`, `AddComment`, `AddAudit`, `Truncate` |
| Authorisation | `AnmolDristi/ITSM/BLL/ItsmTicketAccess.cs` | `CanApproveTicket` excludes the requester; `CanProcessTicket` does not use a permission |
| Detail | `AnmolDristi/ITSM/BLL/TicketDetailService.cs`, `UI/TicketDetail.aspx`, `UI/TicketDetail.aspx.cs` | `CanApprove`, label assignments, panel visibility, `MaxLength`, `HtmlEncode` on comments |
| Schema | `DAL/Entities/Ticket.cs`, `RequestType.cs`, `TicketApproval.cs`, `TicketComment.cs`, `TicketAuditLog.cs`, `PlantTimings.cs`, `SupportMapping.cs` | Lengths and key columns |
| Migrations | `202608120550515_InitialItsmSchema.cs`, `202608201200000_AddTicketAuditLog.cs`, `Configuration.cs` | `varchar(10)`, no unique number, `char(2)` prefix, audit 1000, seed only when empty |
| Plans | `ITSM_STAGE_2_INDEPENDENT_REVIEW_PLAN.md` lines 30–40 and 76–80 | The `sp_getapplock` contradiction and the `Label` auto-encode sentence |
| Validation | `ITSM_Cursor_Independent_Validation.md` FR-01, FR-02, FR-09, FR-10, FR-23 | Where Cursor already matches this pass |

**Not performed:** SQL of any kind; UAT or LIVE access; a browser pass; reading `Web.config`; any edit to an existing file; any staging, commit, push, or tag.

## Appendix B — Footprint

Before this file was added, `git status --short` on `f1f4bdb` (`Aug26_AILWorks_AmimnrupSupport`) showed `AnmolDristi/Web.config` modified, untracked files under `Database/Deployments/ApprovalRequestActivityCounters/` and `Database/Deployments/ApproverKmEdit/`, and the untracked ITSM documents already at the repository root, including the FreeBuff review. `Web.config` was not opened. Those paths were not staged or edited. This reconciliation adds one file, `ITSM_FREEBUFF_FINDINGS_RECONCILIATION.md`.
