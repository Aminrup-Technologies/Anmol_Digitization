# ITSM Documentation Consolidation Review

**Document type:** Proposed consolidation and evidence audit. It does not edit the ten source documents.
**Date:** 2026-11-23
**Repository:** `Aug26_AILWorks_AmimnrupSupport`
**HEAD inspected:** `f1f4bdb` — Protect local configuration and conversation attachments.
**Prior commit:** `4e2f625` — Record the unsigned WP-03 stored-counter design without authorizing execution.
**ITSM tree:** `git diff --stat 9c0c037 HEAD -- AnmolDristi/ITSM` is empty. The AS-IS documents that name `9c0c037` were written against the same ITSM source that is at HEAD.
**Mode:** Read-only source inspection. No SQL was executed. No UAT or LIVE connection was opened. `AnmolDristi/Web.config` was not opened. No application file was modified. Nothing was staged or committed.

This review re-read the ten documents and the ITSM source they cite. Agreement with `ITSM_Cursor_Independent_Validation.md` is recorded only where this pass read the same source again. That document is a Cursor review of Cursor-written findings. It is not an outside validation, and neither is this report.

## Evidence labels used here

| Label | Meaning in this report |
|---|---|
| VERIFIED | The cited source line, migration, or file was read in this pass. |
| PARTIALLY VERIFIED | The code fact holds, and an important consequence, seed, or runtime condition does not. |
| UNVERIFIED | No source or catalog evidence in this pass can settle it. |
| DEFECT | A confirmed implementation failure or a false statement in the documentation set. A missing control is not called a defect unless the code violates a rule the repository itself implements. |
| RISK | An exposure or integrity gap visible in source, without a demonstrated bad row or a reproduced exploit. |
| RECOMMENDATION | A documentation or product improvement. It is not an existing failure. |

---

## 1. Executive summary

The ten documents are a draft evidence pack, not an agreed ITSM baseline. They should not be committed unchanged.

What the source actually is: ITSM is a namespace-layered feature inside the single `AnmolDristi` Web Forms project. Persistence is EF6 Code First. Five migrations create the `itsm` schema. Workflow, numbering, assignment, SLA, and audit are C#. There is no ITSM stored procedure in the module. That architectural picture is shared by the six AS-IS documents and by the independent validation, and it matches the files read here.

What the pack does not agree on:

1. **Stage 1 treats Claims procedures as ITSM.** `usp_ProcessApprovalAction` and `usp_MarkRequestReadyForPayment` operate on `dbo.SalesApprovalRequest*`. ITSM approval is `itsm.TicketApprovals` inside `TicketUpdateService`. The later verification report withdraws that finding. The baseline report still presents it as an ITSM Phase 1B blocker.
2. **The status seed is in the repository.** `AnmolDristi/ITSM/DAL/Migrations/Configuration.cs` `Seed` inserts eight statuses only when `TicketStatuses` is empty, and it sets `Resolved.IsTerminal = false`. The AS-IS architecture, database, workflow, findings, and gap documents say there is no seed and that closure may be dead. The independent validation corrects that. The deployed flag remains unverified, because `Seed` does not run on page requests and does not update an existing table.
3. **Cross-company listing is not the same as a denied open.** `CanViewTicket` allows the requester, the assignee, and a pending approver with no company comparison. `ITSM_AS_IS_Findings_and_Risks.md` FR-08 says a listed ticket cannot be opened. That sentence is broader than the method.
4. **SLA duration is clock time to midnight, with weekend skips.** Plant open and close times affect the start. `AddBusinessHoursSkippingWeekends` does not stop at `CloseTime`. Several AS-IS findings describe the whole calculation as a working-hours calendar.
5. **Stage 2 contradicts itself.** Part A says ticket numbers are allocated with `sp_getapplock`. Part B says the current code does not use `sp_getapplock`. The source has no `sp_getapplock`. Part A also says `Label` encodes `<` and `>`. `TicketDetail.aspx.cs` assigns user text to `Label.Text` with no `HtmlEncode`. The same page encodes comments explicitly.
6. **The verification report confirms a foreign key that does not exist.** It says `Tickets.RequestTypeId` references `itsm.RequestTypes.Id`. The migration references `itsm.MST_RequestType`. Stage 1's foreign-key list uses `itsm.Plants`, `itsm.Branches`, `itsm.Categories`, and the same style of invented names. The database AS-IS document uses the migration names.

The strongest reading of the implementation is the six-document AS-IS set after the corrections in `ITSM_Cursor_Independent_Validation.md` sections 4 and 5. Even that pair is not commit-ready until the AS-IS documents record the seed, the relationship view path, and the SLA duration limit. Stage 1, the verification report, and the Stage 2 plan need correction or an explicit superseded banner before they are treated as evidence.

No live schema, no seed row, no duplicate ticket number, and no XSS payload was observed. Those remain unverified.

---

## 2. Ten-document inventory

All ten files are untracked at the repository root. None was modified by this audit.

| # | File | Lines (approx.) | Stated role | HEAD named | What this pass found |
|---|---|---|---|---|---|
| 1 | `ITSM_AS_IS_Architecture.md` | 312 | AS-IS architecture, document 1 of 6, dated 2026-11-12 | `9c0c037` | Layering, file inventory of 66 ITSM files, and the create sequence match the source. Status seed is omitted. "Six ITSM SQL scripts" under `AnmolDristi/Database/` does not match the four files present. |
| 2 | `ITSM_AS_IS_Database.md` | 263 | AS-IS schema from migrations, document 2 of 6 | not named | Table names, nullability, indexes, and the absence of ITSM procedures match the migrations read here. "No seed script" is false for `Configuration.Seed`. Live catalog is correctly marked unverified. |
| 3 | `ITSM_AS_IS_Workflows.md` | 379 | AS-IS lifecycle, document 3 of 6 | not named | Transition map, approval order, dispatch cascade, and audit coverage match the methods read here. WF-04 treats `Resolved.IsTerminal` as unknown and cites the entity comment as if it were the seed. |
| 4 | `ITSM_AS_IS_Security.md` | 269 | AS-IS security, document 4 of 6 | not named | Authorization predicates, queue listing, session notes, and the refutation of Stage 1 M-03 match the source. Stored XSS is classified as an architectural gap from the rendering sink. Exploit execution is correctly not claimed. |
| 5 | `ITSM_Requirements_Gap_Analysis.md` | 270 | Requirements versus code, document 5 of 6 | not named | The eleven-item split (1 present, 4 partial, 6 missing) matches the files named in its evidence index, except GAP-06's "zero SAP tokens" claim. `Plant.SAP_Code` exists and is unused by dispatch. |
| 6 | `ITSM_AS_IS_Findings_and_Risks.md` | 304 | Consolidated register, document 6 of 6 | `9c0c037` | FR-01, FR-02, FR-03, FR-05 through FR-07, and the H-04 exclusion match source. FR-04 and FR-08 overstate consequences. Section 9's working-tree snapshot describes `9c0c037`, not current HEAD. |
| 7 | `ITSM_STAGE_1_BASELINE_AUDIT.md` | 335 | Earlier baseline. Verdict "CONDITIONAL GO" | not named | Useful on numbering, raw description output, null `CompanyId`, and queue listing. H-04 is a Claims finding placed inside an ITSM blocker list. Several foreign-key names are wrong. Final heading is the truncated word "CONDE". It reports `CreateTicketDiagnostics.cs` missing; the file is present. |
| 8 | `ITSM_STAGE_1_VERIFICATION_REPORT.md` | 124 | Check of the Stage 1 findings | not named | Correctly drops H-04 and corrects the M-03 interpretation. Repeats a wrong foreign-key name as confirmed. Cites `RequireRecordedTenant` at lines 691–696 and `LoadMutableTicket` at 634–653. Those lines are not those methods in the current file. |
| 9 | `ITSM_STAGE_2_INDEPENDENT_REVIEW_PLAN.md` | 225 | Corrected plan plus a source review. Verdict "ACCEPT WITH CONDITIONS" | names branch `feature/itsm-stage2-security-integrity` and commit `3779d18` | Both the branch name and the commit object exist. The plan states `sp_getapplock` as current and then denies it. It states that `Label` encodes `<` and `>`. It says duplicates arise "within the same minute"; the number prefix is `yy` + `MM`. |
| 10 | `ITSM_Cursor_Independent_Validation.md` | 326 | Challenge of the six AS-IS documents, dated 2026-11-12 | not named | The seed, the relationship view path, the SLA duration, and `Plant.SAP_Code` were re-read here and hold. The BLL inventory omits `CreateTicketDiagnostics`. The document is still a same-toolchain review. |

### What was inspected in source

Read directly in this pass:

- `AnmolDristi/ITSM/DAL/Migrations/Configuration.cs`
- `AnmolDristi/ITSM/DAL/Migrations/202608120550515_InitialItsmSchema.cs` (table and foreign-key block)
- `AnmolDristi/ITSM/DAL/Migrations/202608201200000_AddTicketAuditLog.cs`
- `AnmolDristi/ITSM/DAL/Migrations/202608210700000_AddTicketCompanyId.cs` (index lines, via search)
- `AnmolDristi/ITSM/DAL/Migrations/202608240451107_AddApprovalConfiguration.cs` (filtered unique index, via search)
- `AnmolDristi/ITSM/DAL/ItsmDbContext.cs` `SaveChanges` guard
- `AnmolDristi/ITSM/DAL/Entities/Ticket.cs`, `TicketStatus.cs`, `RequestType.cs`, and the `[Table]` attributes of the entity set
- `AnmolDristi/ITSM/BLL/TicketService.cs` create transaction, `GenerateTicketNumber`, IT Head insert, notification insert
- `AnmolDristi/ITSM/BLL/TicketUpdateService.cs` `ChangeStatus`, `ApproveTicket`, `RejectTicket`, `LoadMutableTicket`, `RequireRecordedTenant`
- `AnmolDristi/ITSM/BLL/TicketWorkflow.cs`
- `AnmolDristi/ITSM/BLL/ItsmTicketAccess.cs`
- `AnmolDristi/ITSM/BLL/SlaService.cs`
- `AnmolDristi/ITSM/BLL/TicketListService.cs` queue filters and `BuildQueueQuery`
- `AnmolDristi/ITSM/BLL/TicketMasterDataService.cs` `GetCategoryHierarchy`
- `AnmolDristi/ITSM/UI/TicketDetail.aspx.cs` `LoadTicket` label assignments and `btnApprove_Click`
- `AnmolDristi/ITSM/UI/TicketDetail.aspx` page directive and comment `HtmlEncode`
- `AnmolDristi/ITSM/UI/CreateTicket.aspx.cs` exception display
- `AnmolDristi/ITSM/UI/TicketList.aspx.cs` and `ApprovalConfig.aspx.cs` exception display, via search
- `docs/04-adrs/0001-itsm-architecture-strategy.md` and `0003-itsm-data-store-strategy.md` as accepted intent, not as implementation

Not inspected: deployed catalogs, seed rows, `Web.config`, request-validation mode, ViewState MAC, and a browser session. `docs/05-data-architecture/phase-1-schema.md` and `compliance-and-security.md` exist; they were not re-read line by line. The AS-IS documents' citations of missing types (`TicketNumberService`, `TicketClosureService`, `AttachmentValidationService`, `DownloadAttachment.ashx`, `ApproveTicket.aspx`) match a repository search that returned no ITSM matches for those names.

### Inventory facts that survived re-count

| Fact | Result |
|---|---|
| Files under `AnmolDristi/ITSM/` | 66, including designers, `.resx`, migrations, and `MENU_INTEGRATION.md` |
| BLL `.cs` files | 13, including `CreateTicketDiagnostics.cs` |
| Entity types with `[Table]` | 21 |
| EF migrations with `Up()` | 5: initial schema, audit log, company id, branch legacy id, approval configuration |
| `itsm.` inside `Database/**/*.sql` | No matches |
| ITSM permission SQL | Four files under `AnmolDristi/Database/` plus `Database/Scripts/010_ITSM_Sidebar_Permissions.sql` |
| `itsm_phase1b_migration.sql` | Tracked at the repository root. Length 0 |
| `sp_getapplock` under `AnmolDristi/ITSM` | No matches |
| `FileUpload` / `SaveAs` / attachment services under `AnmolDristi/ITSM` | No matches |
| `CsatScore =` or `CsatFeedback =` writers | No assignments. The detail projection reads both fields |

---

## 3. Cross-document consistency matrix

"Agree" means the documents state the same fact. "Split" means at least one document contradicts another or the source.

| Topic | Source read in this pass | Six AS-IS docs | Stage 1 | Verification | Stage 2 plan | Independent validation |
|---|---|---|---|---|---|---|
| Module shape: UI / BLL / DAL, EF6, schema `itsm`, shared `DbConn` | `ItsmDbContext` constructor is `base("name=DbConn")` (lines 21–22) and `OnModelCreating` sets the default schema to `itsm`. The connection string value was not opened | Agree | Agree, then mixes Claims procedures into the inventory | Agree | Agree | Agree |
| ITSM owns no workflow procedures | No `itsm.` in `Database/**/*.sql`. Approval methods do not call the Claims procedures | Agree. Architecture explicitly drops H-04 | H-04 treats the Claims pair as an ITSM defect | Excludes H-04 | Silent | Agree |
| Ticket-number race at default isolation, no unique index in migrations | `GenerateTicketNumber` lines 359–402, called inside `BeginTransaction()` at line 91 with no isolation argument. `TicketNumber` is `varchar(10)` with indexes only on other columns | Agree, classified as an architectural gap | "Confirmed defect"; C-01 evidence says the method has no transaction; section 6 later admits the outer transaction | "Will" duplicate. Lines cited as 382–478 | Part A: app lock is used. Part B: app lock is absent. "Same minute" | Code fact confirmed. Occurrence not queried |
| `Resolved.IsTerminal` | `Configuration.Seed` sets false, only when the table is empty. Entity comment is an example. `ChangeStatus` checks the flag before the map | Treated as unknown. Comment used as a hint that Resolved may be terminal | Map described; seed not found | Not addressed | Not addressed | Seed found. Deployed row left unverified |
| Stored XSS | `lblDescription.Text` assignment is lines 244–246. Title, resolution, and CSAT labels are assigned the same way. Comments use `HtmlEncode` | Security and findings: unencoded `Label` sink. No exploit run | Critical confirmed defect | Critical confirmed defect. Method named `DataBinding` | Says `Label` encodes `<` and `>` | Sink confirmed. Exploit and `Web.config` left open |
| Null `CompanyId` blocks six mutations | `RequireRecordedTenant` lines 589–600. Migration adds a nullable column and says it does not backfill | Fail-closed policy. Operational risk. No backfill recommended | Critical denial-of-service defect and a Phase 1B blocker | Same, "must fix" | Assessment-only observation. Do not backfill | Confirmed fail-closed. No backfill chosen |
| Queue company filter | `BuildQueueQuery` has no `CompanyId` predicate. Comment at lines 61–63 says shared-service queues | Listing is cross-company. Findings say a listed ticket cannot be opened | High finding, called intentional | Partially confirmed, intentional | Design question | Listing confirmed. Open is allowed for requester, assignee, and pending approver |
| Rowversion | `LoadMutableTicket` lines 565–587 sets `OriginalValue` at line 584. Each mutation catches `DbUpdateConcurrencyException` | Security and workflows refute Stage 1 M-03 | M-03 says the check fails. Cites lines 634–653 | Interpretation refuted. Same wrong line range | Refuted | Mechanism confirmed from source. Not executed |
| SLA | `SlaService.CalculateSla` uses plant timing for the start. `AddBusinessHoursSkippingWeekends` consumes hours until midnight | "Computed and discarded." Duration described as business hours | "Business hours, weekends" | Not re-checked | Not re-checked | Non-persistence confirmed. Duration math narrowed |
| Foreign keys | `itsm.MST_RequestType`, `dbo.MST_Plant`, `dbo.MST_Branch`, `itsm.MST_Category` | Database document matches | `itsm.RequestTypes`, `itsm.Plants`, `itsm.Branches`, `itsm.Categories` | Repeats `itsm.RequestTypes` as confirmed | Not stated | Matches the migration |
| Attachments and CSAT behaviour | Table and columns exist. No writer, no upload, no download handler | Agree: modelled, not implemented | Not a focus | Not a focus | Not a focus | Agree |
| Requirements versus code | See section 4 | Gap analysis is internally consistent aside from the SAP search claim | Not a requirements matrix | Not a requirements matrix | Not a requirements matrix | Confirms the matrix and corrects the SAP search |

Accepted ADRs `0001` and `0003` describe the modular monolith and the `itsm` schema. They also describe future objects (`itsm.ConfigurationItem`) and a `Legacy/` entity folder that the current tree does not have. Those ADRs are intent. The AS-IS documents are right to prefer the migrations when the two differ. ADR 0003's sample connection string is illustrative; this audit did not open `Web.config` and does not repeat any connection value.

---

## 4. Confirmed implementation findings

These are facts read from current source. They are not a statement about UAT or Production.

### 4.1 Architecture — VERIFIED

- Four pages exist: `CreateTicket`, `TicketList`, `TicketDetail`, `ApprovalConfig`, each with a code-behind and a designer.
- BLL classes construct `ItsmDbContext` themselves. `TicketService.CreateTicket` opens one transaction at line 91, then calls `PriorityCalculationService`, `SlaService`, and `DispatchService`, which open their own contexts. Those reads are outside the create transaction. The ticket insert, audit insert, approval inserts, and `NotificationQueue` insert are inside it.
- `ItsmDbContext.SaveChanges` rejects Added, Modified, and Deleted `UserMaster`, `Plant`, `Branch`, `Department`, `Calendar`, `PlantTimings`, and `Company` (lines 98–133).
- BLL files do not import `System.Web`. `TicketService` HTML-encodes the email title with `System.Net.WebUtility.HtmlEncode`.
- UI code-behinds call BLL types directly. No ITSM `SqlConnection` was found in the UI files searched.
- `GenerateTicketNumber` is private on `TicketService` (line 359). There is no `TicketNumberService` type.
- `GetCategoryHierarchy(int companyId)` filters `IsActive` only (lines 95–100). The method comment says company scoping applies when `companyId > 0`. The parameter is unused. `MST_Category` has no company column in the initial migration.

Classification: the unused parameter and the split unit of work are architectural gaps (RISK to consistency of priority, SLA, and assignee if master data changes mid-create). They are not demonstrated data corruption.

### 4.2 Database — VERIFIED from migrations, UNVERIFIED on any server

`202608120550515_InitialItsmSchema.cs` creates `itsm.MST_SupportMapping`, `MST_Category`, `MST_SubCategory`, `MST_Item`, `Tickets`, `TicketApprovals`, and the other lookup tables named in the database AS-IS document. Foreign keys from `Tickets` go to `dbo.MST_UserMaster`, `dbo.MST_Plant`, `dbo.MST_Branch`, and `itsm.MST_*`. There is no unique index on `TicketNumber`.

`Tickets.CompanyId` is added nullable by `202608210700000_AddTicketCompanyId.cs`, with `CreateIndex("itsm.Tickets", "CompanyId")` and an explicit comment that the column is not backfilled and is not a foreign key.

`TicketAuditLog` indexes on `TicketId`, `CompanyId`, `(TicketId, CompanyId)`, and `ActionedOn` are created by `202608201200000_AddTicketAuditLog.cs` lines 25–28. They are not created by the company-id migration.

`Description` and `ResolutionText` are `c.String()` with no max length, which EF6 emits as `nvarchar(max)`. The entity has no `[StringLength]` on `Description`. `CsatFeedback` is `nvarchar(500)`. `Prefix` is `char(2)` (`maxLength: 2, fixedLength: true, unicode: false`).

`MST_ApprovalConfiguration` has one filtered unique index, `UIX_MST_ApprovalConfiguration_IsActive`, created with raw SQL in `202608240451107_AddApprovalConfiguration.cs`.

`OnModelCreating` removes `OneToManyCascadeDeleteConvention`.

`TicketAuditLog.CompanyId` is non-nullable. `AddAudit` assigns `ticket.CompanyId.Value` (line 614), which throws if a caller reaches it with a null snapshot. `RequireRecordedTenant` runs first on the six mutation paths.

Classification of missing uniqueness on ticket numbers, support-mapping natural keys, and approval rows: RISK. The database objects in the migrations do not enforce those invariants. No duplicate row was queried.

### 4.3 Security — VERIFIED as source behaviour

`ItsmTicketAccess.CanViewTicket` (lines 41–71) returns true for the requester, the assignee, or a pending approval row, and otherwise requires `ITSM.Ticket.View` plus `ticketCompanyId == authorizedCompanyId`.

`CanProcessTicket` (lines 74–79) is requester or assignee only. It takes no permission argument.

`CanApproveTicket` requires a pending row and rejects the requester.

Actor code on the detail, list, and create pages is taken from `Session["WORKMAN"]`. This pass found no ITSM read of a company id from the query string.

`CreateTicket.aspx.cs` checks session keys and does not call `HasPermission` in the file. `UI.ITSM_CreateTicket.View` is therefore not a page gate in that code-behind.

`ItsmTenantContext.IsActiveEmployee` has a definition and no call site under `AnmolDristi/ITSM`.

`TicketList.BindQueue` appends `ex.Message` to the page (line 301). `ApprovalConfig.aspx.cs` does the same for policy and IT Head errors (lines 80 and 106). `CreateTicket` shows `InvalidOperationException.Message` and `ArgumentException.Message` with a correlation id (lines 203–215) and hides other exceptions behind `GetUserFacingMessage()`.

`TicketDetail.aspx` does not set `ValidateRequest="false"` on the page directive (lines 10–12). Whether the application enables request validation lives in configuration that was not opened.

Rendering sink, VERIFIED: lines 234–246 and 249 and 260 assign title, names, description, resolution text, and CSAT feedback to `Label.Text` with no `HtmlEncode`. Comment and approval-comment templates call `HttpUtility.HtmlEncode`. Audit grid encoding was not re-tested; `BoundField.HtmlEncode` default was not executed.

Classification: unencoded `Label.Text` is a RISK. It is not a confirmed exploited defect. Calling it a critical defect requires a stored payload and a browser execution, neither of which was performed. Request validation may reject `<` on a normal postback. That possibility is UNVERIFIED.

Cross-company queue metadata is a RISK whose acceptance is an owner decision. The code comment says the listing is intentional. Detail open is still allowed to the requester, the assignee, and a pending approver across companies. All Open can show another company's title, requester, assignee, category, and branch to a holder of `ITSM.Queue.View`.

Null `CompanyId` is a VERIFIED fail-closed rule. It is a RISK only if such rows exist. The migration refuses to invent them. Their count is UNVERIFIED. It is not a security defect and not a denial-of-service defect in the code.

### 4.4 Workflows — VERIFIED

`TicketWorkflow` static map:

| From | Allowed |
|---|---|
| New | In Progress, On Hold, Cancelled |
| In Progress | Resolved, On Hold, Cancelled |
| On Hold | In Progress, Cancelled |
| Resolved | Closed, In Progress |

`Pending Approval`, `Closed`, `Cancelled`, and `Rejected` are not keys. `ChangeStatus` refuses a terminal current status before `IsValidTransition` (lines 73–84). Resolution text is required only when the target name is `"Resolved"` (lines 86–91). The audit row is `StatusChange` with `Detail` null (lines 109–110). `New → Resolved` and `On Hold → Resolved` are absent. Under the repository seed, `Resolved → Closed` is not blocked by `IsTerminal`.

`ApproveTicket` requires status name `"Pending Approval"`, blocks IT Head while a Manager row is pending, and moves to `"In Progress"` only when that active status row exists (lines 306–332). If it does not, the approval row can still be saved and the ticket can remain `Pending Approval`. `btnApprove_Click` passes `comments: null` (lines 147–151).

`RejectTicket` rejects a blank reason before the transaction (lines 363–367), cascades other pending rows, and looks up `"Rejected"` then `"Cancelled"` (lines 424–432). If neither name exists, `StatusId` is left unchanged.

Create-time IT Head handling checks that the configured employee is active. It does not compare that code with the requester (lines 247–270). A later approve call rejects the requester. That can leave an IT Head row pending with no other exit. This is the independent validation's AV-03, and the two methods read here support it. Classification: RISK, conditional on a requester who is also the configured IT Head.

`DispatchService` uses `FirstOrDefault` without `OrderBy`. The initial migration indexes support-mapping columns separately and does not unique the natural key. `MST_PriorityMatrix` has no unique pair index in that migration; `PriorityCalculationService` also uses `FirstOrDefault`. Duplicate-row behaviour is UNVERIFIED. The missing constraint is VERIFIED. Classification: RISK.

`GetActiveAgents` returns every active `MST_UserMaster` row. `ReassignTicket` accepts any active employee code. There is no agent-group entity.

Rejection reason length: `Comments` is `nvarchar(500)` in the migration (line 139). `RejectTicket` assigns `reason.Trim()` with no truncation (line 409). A longer reason fails at save and rolls back. The textbox `MaxLength` was not re-read in the `.aspx`. Classification of the server path: VERIFIED. UI max length: not re-confirmed in this pass; the independent validation states there is none.

### 4.5 Requirements gap — VERIFIED as code-versus-claim

| ID | AS-IS classification | This pass |
|---|---|---|
| GAP-01 hierarchy | Partially exists | VERIFIED three-tier tables, client selection, `ValidateHierarchy`. No parent/root/company column. Seed names of categories are UNVERIFIED. |
| GAP-02 title and item | Already exists, with an alternation caveat | VERIFIED title required in `CreateTicket` (`title.Trim()` and the documented validator claim was not re-opened in the `.aspx` markup). Item remains optional in the entity. The alternation reading is a requirements question, not a code fact. |
| GAP-03 attachments | Missing as behaviour | VERIFIED absence of upload, download, and validation types. Table exists. |
| GAP-04 confirmation popup | Missing | VERIFIED redirect to detail (lines 197–200) and `ShowMessage(..., false)` on the error paths. No dialog was found in the submit path read. |
| GAP-05 impact/urgency visuals | Partially exists | Not re-walked in CSS. The detail page assigns plain text at line 242. The name-derived client colour is accepted from the gap document and was not re-executed. PARTIALLY VERIFIED. |
| GAP-06 SAP / PAN India | Missing | The capability is absent from dispatch and `MST_SupportMapping`. The "zero SAP tokens" sentence is false: `Plant.SAP_Code` is mapped in `AnmolDristi/ITSM/DAL/Entities/Plant.cs`. |
| GAP-07 assignment matrix | Partially exists | VERIFIED cascade shape and missing admin page (no support-mapping writer was found). Whether rows exist is UNVERIFIED. |
| GAP-08 hierarchy independence | Partially exists | VERIFIED: `MST_SupportMapping` has no `RequestTypeId`. No hierarchy entity exists. |
| GAP-09 resolution impact | Missing | VERIFIED: resolution storage is `ResolutionText` and `ResolvedOn`. |
| GAP-10 authorized-agent resolution | Missing | VERIFIED: `CanProcessTicket` is relationship-only, so a requester can resolve. |
| GAP-11 direct to Resolved | Missing | VERIFIED: the map forbids `New → Resolved`. That does not make `Resolved → Closed` dead under the repository seed. |

### 4.6 What the independent validation added, and what this pass re-checked

| ID | Re-check |
|---|---|
| AV-01 missing `In Progress` leaves the ticket in `Pending Approval` after the last approval row is cleared | VERIFIED in `ApproveTicket` lines 325–332 |
| AV-02 missing `Rejected` and `Cancelled` leaves `StatusId` unchanged | VERIFIED in `RejectTicket` lines 424–432 |
| AV-03 requester who is IT Head is not blocked at create | VERIFIED by absence of that comparison in the create block |
| AV-04 approve comments are null | VERIFIED at `btnApprove_Click` |
| AV-05 rejection length versus `nvarchar(500)` | VERIFIED on the server assignment |
| AV-06 priority matrix `FirstOrDefault` | PARTIALLY VERIFIED: the service uses `FirstOrDefault`; the matrix body was not re-read line by line. The migration has no unique pair index in the section read |
| AV-10 notification insert is inside the create transaction | VERIFIED lines 278–287. Empty email returns before the insert (lines 470–473) and the ticket can still commit |
| AV-11 open queues keep non-terminal statuses, including `Resolved` under the seed | VERIFIED `OpenOnly` uses `!Status.IsTerminal` (lines 195–197) |
| AV-15 `Seed` is not application startup | VERIFIED: `Configuration.Seed` is the EF migrator seed. No `SetInitializer` or `MigrateDatabaseToLatestVersion` call was found under `AnmolDristi/ITSM` |

AV-07, AV-08, AV-09, AV-12, AV-13, and AV-14 were not re-read in full in this pass. They remain PARTIALLY VERIFIED claims of the independent validation, not new confirmations.

---

## 5. Unverified claims and evidence gaps

| Item | Why it stays open | How documents treat it |
|---|---|---|
| Deployed `MST_TicketStatus.IsTerminal` for `Resolved` | `Seed` runs only on an empty table, and only when the EF migrator runs. No catalog query was issued | AS-IS pack says unknown and sometimes implies terminal. Independent validation says the repository seed is non-terminal and the live row is unknown. The second statement is the accurate one |
| Whether any environment has applied the five migrations | No catalog | Correctly unverified everywhere that says so |
| Duplicate `TicketNumber` values | No data query | Stage 1 and the verification report speak as if duplicates will occur. The code permits them. Occurrence is unverified |
| `MST_RequestType.Prefix` contents | A one-character value after `Trim` makes the `Length >= 10` parser skip every existing number (`GenerateTicketNumber` lines 389–398). `Prefix` is `char(2)`, so this needs stored spaces or a one-character value | Findings mark it conditional. That is right |
| `MST_SupportMapping` and `MST_PriorityMatrix` duplicate rows | No data query | Code risk only |
| `dbo.VMS_PlantTimings.Branch_ID` versus legacy `Branch_ID` and `MST_Branch.Id` | Create passes `requester.BranchId` to SLA and `MST_Branch.Id` to dispatch (lines 151–154). Row values were not read | WF-09 / FR-23 correctly stay at clarification |
| Tickets with null `CompanyId` | Migration does not backfill. Count unknown | Stage 1 assumes a historical population and calls it a critical outage |
| Permission grants actually applied | Scripts exist. Execution was not evidenced | Correctly unverified |
| `dbo.NotificationQueue` column list | The ITSM insert lists `FormSubmissionId`, `EscalationLevel`, `RecipientEmail`, `RecipientName`, `Subject`, `Body`, `NotificationType`, `Status`, `SourceModule`. The same column list appears in VMS insert statements. No `CREATE TABLE` for `NotificationQueue` was opened | Independent validation left this open. This pass can say the insert shape matches other in-repo inserts. It cannot say the live table matches |
| Request validation, ViewState MAC, cookie flags | `Web.config` was not read | Stage 1 says `Web.config` was inspected. This audit does not rely on that claim and does not repeat any configuration value |
| A working XSS or CSRF exploit | Not executed | Must not be written up as a reproduced incident |
| `MST_Calendar` coverage and plant-timing population | Unread | SLA fallbacks are in code; their data is not |
| IIS execution of the empty `itsm_phase1b_migration.sql` | The file has no statements | Operational question |

Stage 1 also says the audit "could not access production data" and, in the same numbering section, that two concurrent requests "can and will" generate `CR26080006`. The example number is an illustration. It is not an observed ticket.

---

## 6. Contradictions requiring resolution

These are conflicts inside the ten documents or between a document and the source. They need an owner or a corrected sentence. This report does not pick a product design.

### C-A. Claims procedures versus ITSM approval

`ITSM_STAGE_1_BASELINE_AUDIT.md` findings H-04, sections 9 and 12.D, and the Phase 1B gate require resolution of `usp_ProcessApprovalAction` / `usp_MarkRequestReadyForPayment`.

`ITSM_AS_IS_Architecture.md` section 2.2, `ITSM_AS_IS_Findings_and_Risks.md` section 6, and `ITSM_STAGE_1_VERIFICATION_REPORT.md` section 5 say those procedures are Claims objects and are not ITSM blockers.

Source: ITSM approval is `TicketUpdateService.ApproveTicket` / `RejectTicket` on `itsm.TicketApprovals`. The Claims pair remains an unresolved owner conflict for Claims, as `AGENTS.md` section 5 already says. It is not an ITSM workflow defect.

### C-B. Status seed and whether `Closed` is reachable

AS-IS architecture A3, database D3, workflow WF-04, findings FR-04, and gap observation X2 say the repository has no status seed and that the entity comment implies `Resolved` may be terminal.

`Configuration.cs` lines 24–38 seed `Resolved` with `IsTerminal = false` and `Closed`, `Cancelled`, and `Rejected` with `IsTerminal = true`, only when the table has no rows. The comment on `TicketStatus.IsTerminal` (lines 21–23) is an example. It does not assign the column.

The independent validation is the document that matches the file. What remains unresolved is the deployed row, not the existence of the seed.

### C-C. "A listed cross-company ticket cannot be opened"

Findings FR-08 says detail access requires equal company ids, so a listed ticket cannot be opened.

`CanViewTicket` returns true before that comparison for the requester, the assignee, and a pending approver. `GetAgentQueue` filters to the viewer's assignee code, so My Queue rows are openable by that relationship. The company test is the permission path only.

Security SEC-03 is narrower and talks about metadata disclosure. It does not need the FR-08 sentence.

### C-D. SLA "working hours"

Architecture ARC-06, database DB-04, workflow WF-08, and findings FR-07 correctly say the result is not stored.

`SlaService` lines 123–162 add clock hours until midnight and skip weekends. `CloseTime` is used in `ResolveSlaStartTime`, not in the duration loop. Describing the whole result as a plant working-hours calendar overstates the method. The class comment says it adds hours while skipping weekend calendar days, which is closer to the code than the findings summary.

### C-E. Ticket-number control: defect, race, or app lock

| Document | Statement |
|---|---|
| Stage 1 C-01 | No transaction, critical defect, duplicates will be generated |
| Stage 1 section 6 | The call sits inside `BeginTransaction`, which is not `SERIALIZABLE` |
| Verification | Transaction exists, isolation is READ COMMITTED, duplicates will occur |
| Stage 2 Part A item 4 | `sp_getapplock` is used across allocation and insert |
| Stage 2 Part B | No `sp_getapplock`. Read-modify-write. "Same minute" |
| Findings FR-01 | Architectural gap inside the creation transaction. Same month |
| Source | `BeginTransaction()` with no isolation argument, lines 91 and 359–402. No `sp_getapplock`. Prefix is `yy` + `MM` |

The accurate source sentence is: allocation and insert share one transaction at the provider default isolation; the migrations create no unique index; concurrent creates of the same request type in the same month can compute the same number; no duplicate was looked up.

### C-F. XSS severity and the Label claim

Stage 1, the verification report, and Stage 2 call unencoded description a critical defect that must be fixed before Phase 1B.

Security SEC-01 and findings FR-02 call it an architectural gap and say `Label` does not encode. They also say no exploit was run.

Stage 2 Part A item 3 and Part B say `Label.Text` auto-encodes `<` and `>`. Findings section 6 refutes that sentence. The page's own comment templates call `HtmlEncode` because the binding expression does not encode by itself. The description assignment does not call `HtmlEncode`.

Unresolved, and not decidable from these documents: whether request validation stops the payload before it is stored, and whether the owner accepts explicit encoding as a Phase 1B gate. The rendering inconsistency is real either way.

### C-G. Null `CompanyId`

| Document | Classification |
|---|---|
| Stage 1 C-03 and the verification report | Critical defect. Must remediate or weaken the gate before Phase 1B |
| Stage 2 | Confirmed observation. Assessment only. Do not backfill. Do not weaken `RequireRecordedTenant` |
| Security SEC-05 and findings FR-03 | Deliberate fail-closed policy. Operational risk. Not a coding defect |
| Migration comment | Nullable on purpose. Current requester company is not historical ownership |

The code fact is one fact. The release decision is another. The documents currently publish both "must fix" and "do not weaken" as if both were the audit result.

### C-H. Foreign-key names in Stage 1 and the verification report

Stage 1 section 11.E lists `itsm.RequestTypes`, `itsm.Plants`, `itsm.Branches`, `itsm.Categories`, `itsm.SubCategories`, `itsm.Impacts`, `itsm.Urgencies`, `itsm.Priorities`, and `itsm.TicketStatuses`.

The initial migration uses `itsm.MST_RequestType`, `dbo.MST_Plant`, `dbo.MST_Branch`, `itsm.MST_Category`, `itsm.MST_SubCategory`, `itsm.MST_Impact`, `itsm.MST_Urgency`, `itsm.MST_Priority`, and `itsm.MST_TicketStatus`.

The verification report section 6 marks `itsm.RequestTypes.Id` as present. That confirmation is false.

The same report says the company-id migration creates the `TicketAuditLog.CompanyId` index. That index is created in `202608201200000_AddTicketAuditLog.cs`.

### C-I. Line numbers that do not match the stable ITSM tree

`AnmolDristi/ITSM` is unchanged from `9c0c037` to `f1f4bdb`. Against that tree:

| Citation | Document | Actual |
|---|---|---|
| `GenerateTicketNumber` lines 359–489, and "no BEGIN TRANSACTION" | Stage 1 C-01 | Method is lines 359–402. `BeginTransaction` is line 91 in `CreateTicket` |
| Lines 382–478 | Verification summary | Same method is 359–402 |
| Lines 393–425, and `CreateTicket` lines 130–240 | Stage 2 | Number method is 359–402. Create transaction starts at line 91 |
| `lblDescription` line 246, method `DataBinding` | Verification | Assignment is lines 244–246 inside `LoadTicket` |
| `RequireRecordedTenant` lines 691–696 | Verification | Lines 589–600 |
| `RequireRecordedTenant` lines 591–593 | Stage 1 | The `if` body is inside 589–600. Close, and not the whole method |
| `LoadMutableTicket` lines 634–653 | Stage 1 and verification | Method is lines 565–587. `OriginalValue` is line 584. Line 634 is `Commit` |

This audit did not reconstruct the files as they were before `9c0c037`. A citation can be a leftover from an older draft. It is not a description of the implementation those later documents claim to verify.

### C-J. Stage 1 internal defects of fact

- Section 16 says `CreateTicketDiagnostics.cs` was not found. It is `AnmolDristi/ITSM/BLL/CreateTicketDiagnostics.cs`, and `CreateTicket.aspx.cs` constructs it.
- Section 11.B says `Description` is `varchar(MAX)` or carries `[StringLength(2000)]`. The migration uses unbounded `c.String()`. The entity property has no `[StringLength]`.
- Section 11.G says audit text columns are `varchar`. `ActionType`, `OldValue`, `NewValue`, and `Detail` are `c.String(...)` (`nvarchar`). `ActorCode` is `unicode: false`.
- Section 17 heading is "CONDE".
- The executive summary lists raw description twice (items 2 and 6).

### C-K. Working-tree snapshot is historical

Findings section 9 says `.gitignore` is modified and `.cloud-attachments/` is untracked, at HEAD `9c0c037`. Current HEAD is `f1f4bdb`. Current `git status` for those paths shows `AnmolDristi/Web.config` modified and does not show `.gitignore` modified. The snapshot was a point-in-time note. It is not the tree a reader sees now.

### C-L. GAP-06 search claim

The gap analysis says a search of `AnmolDristi/ITSM` for SAP returned zero matches. `Plant.SAP_Code` is a match. Dispatch still does not use it. The missing SAP/Non-SAP agent grouping stands. The search sentence does not.

### C-M. "Six" permission scripts

Architecture section 3.1 says `AnmolDristi/Database/` holds six ITSM permission scripts. The directory contains four: `itsm_phase1b_permissions.sql`, `itsm_role_permissions.sql`, `itsm_navigation_permissions.sql`, and `ITSM_TicketView_Permission.sql`. A fifth script is `Database/Scripts/010_ITSM_Sidebar_Permissions.sql`. The empty root file is a sixth path only if it is counted, and it is not under `AnmolDristi/Database/` and it is not a permission script.

---

## 7. Duplicate or redundant documentation

The same facts are written four to six times, and the copies have drifted.

| Topic | Copies | What to keep if a canonical set is chosen later |
|---|---|---|
| Module layering and the 66-file inventory | Architecture; Stage 1 sections 1 and 10; independent validation section 2 | Architecture, after the seed and script-count corrections |
| Schema tables, indexes, missing uniqueness | Database; Stage 1 section 11; verification section 6 | Database. Do not keep the Stage 1 foreign-key list |
| Numbering race | Architecture, database, workflows, findings FR-01, Stage 1 C-01, verification, Stage 2, independent validation | One paragraph in the findings register, citing `TicketService.cs` 91 and 359–402 and the initial migration |
| XSS rendering table | Security section 5; findings FR-02; Stage 1 sections 2 and 7; verification; Stage 2 Part B; independent validation FR-02 | Security section 5, plus the independent validation's exploitability caveat |
| Null `CompanyId` | Security, workflows, findings, Stage 1, verification, Stage 2 | Findings FR-03's policy wording. Drop the "must backfill" gate |
| Transition map | Workflows, gap GAP-11, Stage 1 section 8, independent validation | Workflows, with the seed table added |
| H-04 Claims pair | Stage 1 only, among documents that still treat it as in scope | A one-line exclusion. The Claims conflict stays in `docs/INDEX.md` |
| M-03 rowversion | Stage 1 asserts it; four later documents refute it | One refutation. Do not leave the original assertion unmarked |
| Eleven requirements | Gap analysis; architecture executive summary; findings verdict | Gap analysis, after the SAP sentence and the X2 seed sentence |
| Stage 2 "plan" and "implementation review" | Stage 2 Part A is a desired control list. Part B is an as-is review that contradicts Part A | Split them, or label Part A as unimplemented target state |

`ITSM_STAGE_1_BASELINE_AUDIT.md` and `ITSM_STAGE_1_VERIFICATION_REPORT.md` together are not a verification pair a reader can trust without the later corrections. The verification report fixes H-04 and M-03's interpretation, then introduces its own false foreign key and stale lines. Keeping both unmarked would preserve the error and the correction side by side.

The six AS-IS documents overlap by design (architecture, database, workflow, security, gaps, register). That split is usable. The register repeats every other document's IDs, which is appropriate for an index and redundant as a second narrative. The independent validation is a challenge log, not a seventh AS-IS narrative.

---

## 8. Recommended corrections, by document

These are proposed edits. This audit did not apply them.

### `ITSM_AS_IS_Architecture.md`

- Replace A3 and question 1 with the `Configuration.Seed` table, the empty-table guard, and the statement that no initializer runs on page load. Keep the deployed flag unverified.
- Change "six ITSM-related SQL scripts" to the four files under `AnmolDristi/Database/` plus `Database/Scripts/010_ITSM_Sidebar_Permissions.sql`.
- In ARC-06, limit "business hours" to the start-time calculation.
- State the review HEAD as `9c0c037` and that `AnmolDristi/ITSM` is unchanged through `f1f4bdb`, or re-date the document if it is revised.

### `ITSM_AS_IS_Database.md`

- Replace D3. The seed is `Migrations/Configuration.cs`, not a SQL script, and it does not run inside any migration `Up()`.
- Add one sentence that `TicketAuditLog` string columns other than `ActorCode` are Unicode (`c.String`), so a later reader does not prefer Stage 1's `varchar` wording.
- Leave D1, D2, and D6 as unverified. They are correctly bounded.

### `ITSM_AS_IS_Workflows.md`

- WF-04: the map-versus-flag order is verified. "May be unreachable" is true only when the deployed `Resolved.IsTerminal` is 1. The repository seed sets 0 and does not update existing rows.
- Keep WF-09 as clarification required.
- Add the approve/reject dead-ends when `"In Progress"` or `"Rejected"`/`"Cancelled"` are missing (independent validation AV-01 and AV-02). Those are in the methods and are absent from the workflow register.

### `ITSM_AS_IS_Security.md`

- SEC-03: state that My Queue, My Tickets, and Pending Approvals are openable through relationship checks, and that the company comparison is the `ITSM.Ticket.View` path.
- SEC-01: keep the unencoded sink. Keep the statement that no exploit was run. Add that the pages inspected do not set `ValidateRequest="false"`, and that `Web.config` was not read.
- SEC-05: the fail-closed wording is the one that should win over Stage 1 C-03. No change of substance is required.

### `ITSM_Requirements_Gap_Analysis.md`

- GAP-06: delete "zero SAP tokens." State that `Plant.SAP_Code` is mapped and unused by dispatch.
- X2: replace "seed unknown" with "repository seed is known; deployed seed is unknown."
- GAP-11: keep `New → Resolved` as missing. Remove any implication, if a reader takes X2 that way, that `Resolved → Closed` is therefore dead.

### `ITSM_AS_IS_Findings_and_Risks.md`

- FR-04: confidence should be "conditional on the deployed flag," with the repository seed named. Do not cite the entity comment as the seed.
- FR-08: replace "cannot be opened" with the four `CanViewTicket` paths.
- FR-07: state that plant hours govern the start and that the added duration is clock time to midnight with weekend skips.
- Section 6 already refutes M-03, confirms C-01/C-02/C-03 as code facts, and drops H-04. Point C-02 at "unencoded sink, exploit not run" so it does not inherit Stage 1's "confirmed XSS" wording.
- Section 9: mark the working-tree table as the tree at `9c0c037`, or replace it with the current status. Current status at this audit: `AnmolDristi/Web.config` modified; the ten ITSM markdown files untracked; unrelated untracked deployment files present and not part of ITSM. `.gitignore` was not modified in that status check.

### `ITSM_STAGE_1_BASELINE_AUDIT.md`

Do not leave this file as a current finding list without a banner. Minimum banner:

- H-04 is out of ITSM scope. It remains a Claims owner conflict in `docs/INDEX.md`.
- M-03's interpretation is withdrawn. The mechanism is `OriginalValue` at `TicketUpdateService.cs` line 584.
- Foreign-key names in section 11.E are wrong. Use the migration names.
- C-01 is a source-level race, not a demonstrated duplicate, and the method does run inside `CreateTicket`'s transaction.
- C-02 is an unencoded render. It was not executed.
- C-03 is the fail-closed rule. It is not, by itself, a Phase 1B code defect.
- `CreateTicketDiagnostics.cs` exists.
- The heading "CONDE" should be restored to the intended verdict word or the verdict should be withdrawn.

### `ITSM_STAGE_1_VERIFICATION_REPORT.md`

- Remove `itsm.RequestTypes` or replace it with `itsm.MST_RequestType`.
- Attribute `TicketAuditLog` indexes to `202608201200000_AddTicketAuditLog.cs`.
- Replace line citations with the current lines in section 6 of this report.
- Replace "can and will" with "can."
- Stop listing C-03 as a mandatory code fix if the Stage 2 and findings decision is "do not weaken the gate."
- The H-04 exclusion and the M-03 interpretation can stay, once the line numbers are fixed.

### `ITSM_STAGE_2_INDEPENDENT_REVIEW_PLAN.md`

- Part A item 4 must not say `sp_getapplock` is used. The source does not contain it. If the sentence was a target design, label it as not implemented.
- Delete the claim that `Label` encodes `<` and `>`.
- Replace "same minute" with "same request type and same calendar month."
- Separate "desired controls" from "behaviour observed in source." The acceptance table currently mixes both, including check marks on controls the same document says are absent.
- Re-verify the branch tip before citing `3779d18` as the tip of `feature/itsm-stage2-security-integrity`. This audit confirmed the commit object and the branch name. It did not print the branch tip.

### `ITSM_Cursor_Independent_Validation.md`

- Add the HEAD that was read. The ITSM tree at `f1f4bdb` matches `9c0c037`.
- Add `CreateTicketDiagnostics` to the BLL list or say the list is the workflow set and omits diagnostics.
- Keep the unverified table. It is the right boundary.
- Add a sentence that the review is a Cursor pass over Cursor-written AS-IS documents, so an outside reader still has to check the cited lines.

---

## 9. Proposed canonical ITSM documentation structure

Do not delete the ten files as part of adopting this structure. Mark superseded text. Keep both sides where a decision is still open, which is the rule already stated in `AGENTS.md` section 7.

Proposed set, after a correction pass:

| Role | Canonical file | Contents |
|---|---|---|
| Intent | `docs/04-adrs/0001` and `0003`, plus `docs/05-data-architecture/` | What was accepted as design. Not a description of the code |
| AS-IS architecture | Corrected `ITSM_AS_IS_Architecture.md` | Boundaries, inventory, create sequence, cross-module reads |
| AS-IS schema | Corrected `ITSM_AS_IS_Database.md` | Migrations only, with a standing "deployed catalog unverified" banner |
| AS-IS workflow | Corrected `ITSM_AS_IS_Workflows.md` | Map, seed, approval, assignment, audit |
| AS-IS security | Corrected `ITSM_AS_IS_Security.md` | Session, RBAC, tenant, rendering, attachments |
| Requirements | Corrected `ITSM_Requirements_Gap_Analysis.md` | The eleven items, each marked implemented or not |
| Register | Corrected `ITSM_AS_IS_Findings_and_Risks.md` | One ID per finding, one classification, links to the five narratives |
| Challenge log | `ITSM_Cursor_Independent_Validation.md` | Disagreements with the register. Not a second register |
| Historical | Stage 1, verification, Stage 2 | Kept, with a banner that they are not the finding list. Stage 2's unimplemented control list moves to a future design note only after an owner asks for a design |

One classification scale should be used in the register. The scale in section 1 of this report matches the request for this audit. The AS-IS documents' labels ("Already Exists", "Architectural Gap", "Configuration Required", "Clarification Required", "Verified Absence") can remain as product-fit labels, provided they are not silently converted into "Critical defect."

Suggested mapping, so the two vocabularies stop fighting:

| AS-IS label | This audit's label when the source was read |
|---|---|
| Already Exists | VERIFIED behaviour |
| Architectural Gap | RISK or RECOMMENDATION, depending on whether a control is missing or a design was never claimed by the code |
| Verified Absence | VERIFIED absence |
| Clarification Required | UNVERIFIED or PARTIALLY VERIFIED |
| Configuration Required (policy) | Owner decision. The code fact can still be VERIFIED |

Phase 1B "GO" language in Stage 1, the verification report, and Stage 2 should not be copied into the canonical register. Those verdicts depend on decisions C-E, C-F, and C-G, which the documents do not settle in one direction.

---

## 10. Commit recommendation

**The ten documents are not ready to commit as the ITSM baseline.**

They can enter a separate review-and-commit phase only as draft evidence, after the corrections in section 8, or with a banner on each historical file that points at a corrected register. Committing them unchanged would put false foreign-key names, a self-contradictory app-lock statement, a wrong Label-encoding claim, and a Claims procedure conflict into git as ITSM fact.

Nothing in this audit authorizes that commit. No file was staged. `git status` immediately before this report was written showed:

- `M AnmolDristi/Web.config` (pre-existing; not read and not modified here)
- untracked `Database/Deployments/ApprovalRequestActivityCounters/`
- untracked `Database/Deployments/ApproverKmEdit/D_Rollback_ApproverKmEdit_UAT.sql`
- the ten untracked ITSM markdown files

Those deployment paths were not opened and were not changed. This report is a new untracked file, `ITSM_DOCUMENTATION_CONSOLIDATION_REVIEW.md`.

---

## 11. What an outside reviewer should challenge

This section is for FreeBuff or any reviewer who was not the author of the ten documents. Checking this report against the cited lines is the review. Accepting it because it agrees with `ITSM_Cursor_Independent_Validation.md` is not.

1. **Same-toolchain limit.** Re-open `Configuration.cs` lines 24–38, `ItsmTicketAccess.cs` lines 41–71, `SlaService.cs` lines 123–162, and `TicketService.cs` lines 91 and 359–402. Do not take the seed, the view rule, the SLA duration, or the missing app lock from this narrative alone.
2. **`Label` encoding.** This report treats `Label.Text` as an unencoded response write because the assignment contains no `HtmlEncode` and because comments on the same page are encoded explicitly. Confirm that against the `System.Web` build the site actually runs. This pass did not decompile `System.Web.dll`.
3. **Request validation.** Read the application `httpRuntime` / `pages validateRequest` setting in a redacted way. Do not paste connection strings. A blocked `<script>` postback would make the XSS item a latent sink rather than a reachable store.
4. **Race versus incident.** Decide whether "no unique index and a read-modify-write" is a defect the project will fix, or a risk the owner accepts. This report does not call it a demonstrated duplicate.
5. **Null `CompanyId` population.** A read-only count on a named environment is the only way to know whether FR-03 has any rows. Do not backfill from the requester's current company as part of that count.
6. **Deployed `IsTerminal`.** Read `itsm.MST_TicketStatus` on a named environment. The repository seed is not that table.
7. **`VMS_PlantTimings.Branch_ID` key space.** One read-only sample of `Branch_ID` against `dbo.MST_Branch.Branch_ID` and `dbo.MST_Branch.Id` settles WF-09.
8. **Stage 2 branch tip.** Confirm whether `feature/itsm-stage2-security-integrity` still points at `3779d185001b3182042d5c8d38e5b111675d8dee`.
9. **NotificationQueue DDL.** The insert column list matches other modules' inserts. A `CREATE TABLE` or catalog check is still required before calling the insert safe.
10. **Whether this report is too soft.** An owner may classify the numbering race and the unencoded sink as defects without a production specimen. That is a policy choice. It should be written as a choice, not as a test result.
11. **Files this pass did not re-read line by line.** `DispatchService.cs` body beyond the `FirstOrDefault` search, `PriorityCalculationService.cs` body, `TicketDetail.aspx` repeater markup beyond the `HtmlEncode` lines, `CreateTicket.aspx` CSS, the permission SQL grant lists, and `phase-1-schema.md`. A challenge on those points needs its own read.
12. **Pre-existing tree.** Confirm `AnmolDristi/Web.config` and the two untracked deployment paths are still the only non-ITSM working-tree changes, and that this report did not stage them.

---

## 12. Limits of this audit

- No MSBuild, no browser session, no login, no postback, and no SQL.
- `Web.config` was not read. Upload limits, request validation, and the connection target are unknown here.
- The five migration `Up()` methods were not executed. "The migration creates X" means the C# migration contains that call.
- Designer files and `.resx` files were counted, not reviewed for behaviour.
- ADR samples that contain a connection-string shape were not copied into this report.
- This file is a proposed review. It is not an approval, a signature, or a Phase 1B decision.
