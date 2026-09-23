# TA/DA Production Deployment Checklist (v1.1 Post-Go-Live FSOT)

| Field | Value |
| --- | --- |
| Branch | `Aug26_AILWorks_AmimnrupSupport` |
| Production baseline tag | `tada-v1.1-postgolive-fsot-2026` |
| Supersedes | `tada-v1.0-go-live-2026` (`94bc2089`) — historical RC-001–RC-013 only; **do not ship alone** |
| Certification | `TA_DA_PostGoLive_Consolidated_Certification.md` (2026-09-16) |
| UAT financial proof | `docs/reviews/Post_GoLive/RC015G_FSOT_UAT_Execution.md` — Request **97** Final = Total = Snapshots = Excel = **₹7,736.44** |
| Schema / data scripts | None for DDL. Procedure bodies + `EXEC usp_RecalculateApprovalRequest` backfill only. |

Tick boxes during the production window. Do not push or publish until CAB approval.

---

## Pre-deployment

### Backup

- [ ] Full backup of the production SQL database (equivalent of `AnmolDristiDev`).
- [ ] Backup file / job name: `________________`
- [ ] Verify backup completed (size / job success).
- [ ] Script-out current live definitions (rollback source) for:

```text
dbo.usp_UpdateActivityKM
dbo.usp_ForwardApprovalRequest
dbo.usp_UpdateSalesDailyExpenseStatus
dbo.usp_ProcessApprovalAction
dbo.usp_MarkRequestReadyForPayment
dbo.usp_RecalculateApprovalRequest
dbo.usp_GetApprovalRequest
dbo.usp_Export_Auditor_Final_Excel
```

- [ ] Copy the live IIS site folder (at minimum `bin\AnmolDristi.dll` and published ASPX).
- [ ] IIS backup path: `________________`
- [ ] Live DLL timestamp / version: `________________`

### IIS readiness

- [ ] Application pool identified: `________________`
- [ ] Site bindings confirmed.
- [ ] Change window agreed; idle or stop the TA/DA site only if the window requires it.
- [ ] Do not deploy unrelated HRMS or Switch User packages in the same window unless separately signed off.

### Branch verification

- [ ] Package is built from `Aug26_AILWorks_AmimnrupSupport` at tag `tada-v1.1-postgolive-fsot-2026`.
- [ ] Release commit SHA: `________________`
- [ ] Confirm RC-015B four-part Stationary is in the package (`usp_UpdateSalesDailyExpenseStatus` — **mandatory**; not yet live on UAT expense SP).
- [ ] `.cursor/` is not in the package.
- [ ] No extra schema scripts in the packet.

### Package inventory (v1.1 delta over v1.0)

| Path | RCs |
| --- | --- |
| `AnmolDristi/ClaimsApproval_v2.aspx` / `.cs` / `.designer.cs` | RC-009–013 + RC-014D Open + RC-015A bind/Recalc |
| `AnmolDristi/SalesMonthlyReview.aspx` / `.cs` | RC-001, RC-007, RC-008, RC-010 |
| `AnmolDristi/MyClaimsView_v2.aspx` / `.cs` | RC-003–006, RC-010 + FSOT footer |
| `AnmolDristi/ApprovedClaimsView.aspx.cs` | RC-004 + FSOT bind |
| `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` | **RC-014B** |
| `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql` | **RC-014D** |
| `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql` | **RC-015A** |
| `Database/Stored Procedures/Approval/usp_GetApprovalRequest.sql` (+ Retrieval twin) | **RC-015A** |
| `Database/Stored Procedures/Approval/usp_Export_Auditor_Final_Excel.sql` (+ Retrieval twin) | **RC-015C** |
| `Database/Stored Procedures/Approval/usp_UpdateSalesDailyExpenseStatus.sql` | RC-012B + **RC-015B Stationary** |
| Tag-era (already in v1.0): `usp_UpdateActivityKM`, `usp_ForwardApprovalRequest` | RC-008, RC-012A |

Inherited v1.0 objects still required if Production is behind UAT go-live:

| Path | RCs |
| --- | --- |
| `Database/Stored Procedures/Submission/usp_UpdateActivityKM.sql` | RC-008 |
| `Database/Stored Procedures/Approval/usp_ForwardApprovalRequest.sql` | RC-012A |

---

## Deployment

### Publish application

- [ ] After **manual** push and CAB approval, publish / copy the application package to the IIS site.
- [ ] Confirm published output is newer than the IIS backup.
- [ ] Record publish timestamp: `________________`

### Deploy stored procedures (order)

Apply repository `CREATE OR ALTER` (or shop-standard ALTER) bodies. Preserve signatures. Prefer Approval path; keep Retrieval twins in sync if Production deploys from Retrieval.

**If Production is still on pre-go-live (RC-001–013 gap):**

1. `usp_UpdateActivityKM` (RC-008)
2. `usp_ForwardApprovalRequest` (RC-012A)

**Post-go-live / FSOT package (mandatory for v1.1):**

3. **RC-015A Recalc** — `usp_RecalculateApprovalRequest.sql`
4. **Backfill `FinalApprovedAmount`** — execute Recalc for ReadyForPayment / Completed (and any other statuses Finance requires). Pattern: `EXEC dbo.usp_RecalculateApprovalRequest @ApprovalRequestId = <Id>;` — do **not** invent a separate UPDATE formula.
5. **RC-015A Get** — `usp_GetApprovalRequest.sql` (Approval + Retrieval twin if used)
6. **RC-015C Export** — `usp_Export_Auditor_Final_Excel.sql` (Approval + Retrieval twin if used)
7. **RC-015B expense SP** — `usp_UpdateSalesDailyExpenseStatus.sql` (four-part: DA+Hotel+Travel+**Stationary**). **Mandatory in this release** even if UAT still shows the three-part body.
8. **RC-014B** — `usp_ProcessApprovalAction.sql` (if not already on Production)
9. **RC-014D** — `usp_MarkRequestReadyForPayment.sql` (if not already on Production)

Post-apply checks:

- [ ] Recalc writes `FinalApprovedAmount` from latest non-Rejected/Returned DayNet.
- [ ] Get returns `ISNULL(FinalApprovedAmount, TotalApprovedAmount)` as Net Payable.
- [ ] Export uses latest Approved detail per ActivityId (no duplicate RESUBMIT rows).
- [ ] Expense status SP sets `Snapshot_StationaryAmount` and `ExpenseTotal = DA+Hotel+Travel+Stationary`.
- [ ] ProcessApprovalAction last-day Auditor path → `Completed` (not premature on first day).
- [ ] MarkReady uses named history INSERT; C# opens connection before call.
- [ ] No table, index, or ad-hoc money UPDATE script executed outside Recalc.

| Object | Production modify_date |
| --- | --- |
| `usp_UpdateActivityKM` | |
| `usp_ForwardApprovalRequest` | |
| `usp_UpdateSalesDailyExpenseStatus` (**015B**) | |
| `usp_ProcessApprovalAction` (**014B**) | |
| `usp_MarkRequestReadyForPayment` (**014D**) | |
| `usp_RecalculateApprovalRequest` (**015A**) | |
| `usp_GetApprovalRequest` (**015A**) | |
| `usp_Export_Auditor_Final_Excel` (**015C**) | |

---

## Post-deployment

### IIS recycle

- [ ] Recycle the application pool.
- [ ] Hit `SalesMonthlyReview.aspx`, `ClaimsApproval_v2.aspx`, `MyClaimsView_v2.aspx`, `ApprovedClaimsView.aspx` (authenticated).
- [ ] Confirm no yellow-screen / compile error.

### Browser cache

- [ ] Instruct L1/Auditor testers to hard-refresh (Ctrl+F5) or clear cache for `ClaimsApproval_v2` and `SalesMonthlyReview`.
- [ ] Confirm Forward confirmation modal and auto-close scripts load (RC-011 / RC-013).

### Smoke verification (v1.0 paths)

Repeat the scenarios in `TA_DA_Smoke_Test_Execution.md` on production after recycle:

- [ ] NEW submission
- [ ] Return
- [ ] KM edit
- [ ] RESUBMIT
- [ ] Latest-version (one row per day after RESUBMIT)
- [ ] Return notification
- [ ] Forward confirmation
- [ ] Auditor routing (header stays Pending until last day)
- [ ] Modal auto-close (no F5)

### Financial / FSOT proof (v1.1 — mandatory)

Retain Request **97** UAT proof as the certified reference (`RC015G`). On Production, re-proof an equivalent Ready-for-Payment claim (or Request 97 if same DB lineage):

- [ ] Header `FinalApprovedAmount` = latest snapshot DayNet sum
- [ ] `TotalApprovedAmount` reconciles (Stationary may still be omitted from computed Total; **Final is authority**)
- [ ] Excel export SUM = Final (latest-version Approved rows only)
- [ ] Status remains ReadyForPayment through Recalc/Get/Export
- [ ] If Stationary ≠ 0 on any day: confirm expense SP snapshot includes Stationary and Final still matches (RC-015B gate)
- [ ] Record Production request id / amounts: `________________`

---

## Rollback

Use only if production smoke or financial proof fails, or the site will not start.

1. Copy the IIS backup over the live site (`AnmolDristi.dll` and published files).
2. Recycle the application pool.
3. Run the **pre-deploy** scripted-out procedure definitions (not the repository RC files).
4. Confirm `modify_date` reverted.
5. Do not restore tables or data unless Finance explicitly authorizes a Final backfill undo (Recalc is re-runnable).

Exit criteria after rollback: L1 inbox loads; employee month grid loads; incident logged with SHA, backup path, and failed scenario.

---

## Sign-off

| Check | Owner | Time | Result |
| --- | --- | --- | --- |
| IIS backup taken | | | |
| DB backup taken | | | |
| App published | | | |
| RC-015A Recalc + Final backfill applied | | | |
| RC-015A Get applied | | | |
| RC-015C Export applied | | | |
| RC-015B expense Stationary SP applied | | | |
| RC-014B / RC-014D applied (if needed) | | | |
| IIS recycled | | | |
| Browser cache cleared | | | |
| Production smoke PASS | | | |
| Financial proof (Req 97 pattern) PASS | | | |
| Production approved / rolled back | | | |

Deployer: ________________  
Approver (CAB): ________________  
Date: ________________

---

## RC-023 — Production Hardening (additive package)

Use this section when promoting commit `4644baf` after the v1.1 / RC-017–022 baselines. Does not replace the v1.1 steps above.

| Field | Value |
| --- | --- |
| Commit (short) | `4644baf` |
| Commit (full) | `4644baf4caa051b0f5d32cc76f1479a0d344a4d4` |
| Commit timestamp | 2026-09-23 13:04:41 +0530 |
| RC-023 UAT Tag | `rc-023-uat-certified` @ `4644baf` (pushed) |
| DLL SHA256 | `04D0250EE80DABB519992CCC70FAB1CB8FCF35D562ACBE33D5A9E9E055AB09BF` |
| Build timestamp | 2026-09-23 12:54:28 +0530 |
| Diff | 5 files, +328 / −73 |

### Scope (5 files)

| Path | Notes |
| --- | --- |
| `AnmolDristi/ClaimsApproval_v2.aspx.cs` | RC-022-aligned finalize + ReadyForPayment lock |
| `AnmolDristi/SalesApprovalDetailVersionHelper.cs` | `DetailVersion` → `MAX(ApprovalRequestDetailId)` |
| `Database/Stored Procedures/Approval/usp_UpdateSalesDailyExpenseStatus.sql` | Auth + payment lock + **RC-015B Stationary** |
| `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` | Version→Id + Completed/ReadyForPayment lock |
| `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql` | Optional unless target body differs from RC-014D. |

### SQL delta (apply after rollback script-out)

| Order | Procedure | CAB note |
| --- | --- | --- |
| 1 | `usp_UpdateSalesDailyExpenseStatus` | **Mandatory** — closes RC-015B Stationary + RC-023 auth/locks |
| 2 | `usp_ProcessApprovalAction` | **Mandatory** — latest-version + payment lock |
| 3 | `usp_MarkRequestReadyForPayment` | Optional unless target body differs from RC-014D. |

### UAT evidence placeholders

| Check | Result | Evidence |
| --- | --- | --- |
| UAT expense SP `modify_date` after ALTER | **PASS** | `2026-09-15 12:03:55.347` → `2026-09-23 13:51:38.003` (QI ON redeploy); pack `pvt-evidence/2026-09-23-rc023-uat/` |
| UAT ProcessApprovalAction `modify_date` after ALTER | **PASS** | `2026-09-15 18:04:06.090` → `2026-09-23 13:51:38.750` (CREATE OR ALTER + QI ON) |
| DLL published / AppDomain recycle | **PASS** | SHA `04D0250E…AB09BF`; recycle `2026-09-23 13:44:38.581 +05:30`; build stamp `2026-09-23 12:54:28`; Web.config not retargeted |
| Non-current approver expense refused | **PASS** | R106 ExpenseId `2410`; ActionBy `AHS1605` → *You are not the current approver…*; current `AHS1870` mutate PASS (txn rolled back) |
| Mutation blocked on Completed / ReadyForPayment | **PASS** | R98/R107 expense + day locks → *locked for payment…* (`06-auth-finalize-gates.txt`) |
| Stationary ≠ 0 Final / snapshot / Excel | **PASS** | R106 Stationary `800` Snap=Live=Header; Final `6971.06` PASS. R107 Excel 31 rows / Stationary `242` / FinalSum=`9384.29` = header |
| Level-aware finalize (L1-Approved blocks Auditor Ready) | **PASS** | R94: UnresolvedCount=`21` L1-Approved; `usp_MarkRequestReadyForPayment` → *Request not completed.* |

### CAB deployment inventory checklist

- [x] Rollback scripts captured for the three SQL objects above.
- [x] Package built from `4644baf` (DLL SHA matches table).
- [x] UAT ALTER order 1 → 2 (→ 3 optional) completed. MarkReady **SKIPPED** (UAT body = RC-014D runtime).
- [x] UAT evidence placeholders filled (**no Production cutover with blanks**).
- [ ] Production ALTER same order + IIS publish/recycle.
- [ ] Production `DbConn` / catalog retarget confirmed by ops (outside this commit).
- [ ] RC-023 sign-off: Deployer Cursor Agent / CAB ________ / Date 2026-09-23
