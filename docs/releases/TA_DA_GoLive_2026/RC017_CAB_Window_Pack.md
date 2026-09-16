# RC-017A — CAB Window Pack (Pre-Execution)

**RC:** RC-017A  
**Purpose:** Operator handoff so RC-017 can run immediately after CAB approval without in-window decision-making.  
**Prepared:** 2026-09-16  
**Constraint:** Prepare only. No code, SQL execution, Git push, IIS publish, or tag creation in this task.  
**Live evidence log:** `docs/releases/TA_DA_GoLive_2026/RC017_Production_Execution_Log.md`  
**Runbook:** `docs/releases/TA_DA_GoLive_2026/TA_DA_v1.1_Production_Promotion_Runbook.md`  
**Pre-CAB audit:** `docs/releases/TA_DA_GoLive_2026/RC017_PreCAB_Readiness_Report.md` — **CONDITIONAL GO**

---

## 1. Freeze Identity (maps to RC-017 Phase 1 — CAB & Change Window)

Verified at pack preparation (read-only):

| Item | Expected | Observed | Flag |
| --- | --- | --- | --- |
| Branch | `Aug26_AILWorks_AmimnrupSupport` | `Aug26_AILWorks_AmimnrupSupport` | None |
| HEAD | `3779d185` | `3779d185001b3182042d5c8d38e5b111675d8dee` | None |
| Tag | `tada-v1.1-postgolive-fsot-2026` | Present; tip = `3779d185` | None |
| Tag object | (local) | `2f361f3434de3985949073ec3a06ba08ccb3aaa6` | None |
| Tracked working tree | Clean vs freeze | No modified tracked files | None |
| Untracked outside freeze | RC-016 / RC-017 ops docs only | `TA_DA_v1.1_Production_Promotion_Runbook.md`, `RC017_Production_Execution_Log.md`, `RC017_PreCAB_Readiness_Report.md`, this pack, `.cursor/` | **NOTE** — ops docs + `.cursor/`; not release content |
| Origin relationship | Ahead pending CAB push | `0` behind / `1` ahead | Expected |

**Freeze status:** Intact. Proceed to CAB; do not retarget or amend `3779d185`.

---

## 2. Deployment Command Sheet (copy-paste; do not execute until CAB)

Maps to RC-017: **Phase 2** Git · **Phase 3** Backup · **Phase 4** SQL + Backfill · **Phase 5** IIS.  
Fill RC-017 timestamps as each command completes.

### 2.1 Git — RC-017 Phase 2 (Release Engineer — after CAB approval)

```powershell
# Identity check
git checkout Aug26_AILWorks_AmimnrupSupport
git rev-parse HEAD
# Expect: 3779d185001b3182042d5c8d38e5b111675d8dee
git rev-list -n 1 tada-v1.1-postgolive-fsot-2026
# Expect: same SHA

# Push freeze commit
git push origin HEAD

# Push annotated tag
git push origin refs/tags/tada-v1.1-postgolive-fsot-2026

# Verify remote SHA
git ls-remote origin refs/heads/Aug26_AILWorks_AmimnrupSupport
git ls-remote origin refs/tags/tada-v1.1-postgolive-fsot-2026
# Peeled tag commit must equal 3779d185001b3182042d5c8d38e5b111675d8dee
```

Record CAB approval / Change ID in RC-017 **Phase 1**. Record push evidence in RC-017 **Phase 2**.

### 2.2 Backup + SQL + Backfill — RC-017 Phase 3 then Phase 4 (DBA — RC-016 order)

**Mandatory checkpoint — RC-015B:**  
`usp_UpdateSalesDailyExpenseStatus` (four-part DA+Hotel+Travel+**Stationary**) **must** be applied on Production in this window. Do not skip even if UAT still shows the three-part body.

#### Pre-apply

```text
1) Full Production DB backup — record path in RC-017 Phase 3
2) Script-out live definitions (rollback source) for:
   usp_UpdateActivityKM
   usp_ForwardApprovalRequest
   usp_UpdateSalesDailyExpenseStatus
   usp_ProcessApprovalAction
   usp_MarkRequestReadyForPayment
   usp_RecalculateApprovalRequest
   usp_GetApprovalRequest
   usp_Export_Auditor_Final_Excel
```

#### Apply order (six v1.1 procedures)

| Step | Procedure | RC | Repo path | Notes |
| --- | --- | --- | --- | --- |
| 1 | `usp_RecalculateApprovalRequest` | RC-015A | `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql` | First |
| 2 | **Backfill** `FinalApprovedAmount` | RC-015A | `EXEC dbo.usp_RecalculateApprovalRequest @ApprovalRequestId = <Id>;` | ReadyForPayment + Completed (min.); no hand `UPDATE` |
| 3 | `usp_GetApprovalRequest` | RC-015A | `Database/Stored Procedures/Approval/usp_GetApprovalRequest.sql` (+ Retrieval twin if used) | After backfill |
| 4 | `usp_Export_Auditor_Final_Excel` | RC-015C | `Database/Stored Procedures/Approval/usp_Export_Auditor_Final_Excel.sql` (+ Retrieval twin if used) | |
| 5 | `usp_UpdateSalesDailyExpenseStatus` | **RC-015B** | `Database/Stored Procedures/Approval/usp_UpdateSalesDailyExpenseStatus.sql` | **MANDATORY** |
| 6 | `usp_ProcessApprovalAction` | RC-014B | `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` | If Production behind |
| 7 | `usp_MarkRequestReadyForPayment` | RC-014D | `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql` | If Production behind |

If Production still lacks v1.0 go-live objects, apply **before** step 1:

1. `usp_UpdateActivityKM` (RC-008)  
2. `usp_ForwardApprovalRequest` (RC-012A)

#### Backfill pattern

```sql
EXEC dbo.usp_RecalculateApprovalRequest @ApprovalRequestId = <Id>;
-- Loop ReadyForPayment / Completed (and Finance-approved statuses).
-- Record OK / FAIL counts in RC-017 Phase 4.2
```

#### Post-apply markers to confirm

| Procedure | Marker |
| --- | --- |
| Recalc | Writes `FinalApprovedAmount` from latest DayNet |
| Get | `ISNULL(FinalApprovedAmount, TotalApprovedAmount)` |
| Export | Latest Approved detail per ActivityId |
| **Expense (015B)** | `Snapshot_StationaryAmount` + `ExpenseTotal = DA+Hotel+Travel+Stationary` |
| ProcessApprovalAction | `DetailApprovedLevel` unresolved gate; Auditor last day → `Completed` |
| MarkReady | Named-column history INSERT |

### 2.3 IIS — RC-017 Phase 5 (App Team)

```powershell
# 1) Backup live site (record path + pre-deploy DLL timestamp in RC-017)
#    Minimum: bin\AnmolDristi.dll + published ASPX

# 2) Build / publish from tag (after push)
git fetch --tags
git checkout tada-v1.1-postgolive-fsot-2026
# Build Release publish package for AnmolDristi (VS Publish or msbuild)

# 3) Publish path (fill shop paths before window)
#    Package source: ________________
#    Production site path: ________________
#    App pool name: ________________

# 4) Copy / publish package over Production site

# 5) Verify DLL newer than backup
Get-Item "<ProductionSite>\bin\AnmolDristi.dll" |
  Select-Object FullName, Length, LastWriteTime

# 6) Recycle (IIS Manager or)
Import-Module WebAdministration
Restart-WebAppPool -Name "<AppPoolName>"
# Or: iisreset /noforce   (only if shop standard allows)

# 7) Authenticated smoke URLs
#    /SalesMonthlyReview.aspx
#    /ClaimsApproval_v2.aspx
#    /MyClaimsView_v2.aspx
#    /ApprovedClaimsView.aspx
```

Record publish time, DLL LastWriteTime, recycle time, first successful page load in RC-017 **Phase 5**.

---

## 3. Live Validation Cards (maps to RC-017 Phase 6 — Smoke)

Use during RC-017 **Phase 6**. Capture evidence into the execution log (no screenshots required in this pack). Tick when done.

### Card A — Employee Submit

| Field | Value |
| --- | --- |
| Actor | Employee |
| Page | `SalesMonthlyReview.aspx` |
| Action | Create / submit NEW daily activities for a test month |
| Expected | Approval request created; days visible; status Pending (or shop-normal new state) |
| Evidence | Request Id, date/time, status screenshot or SQL header Status |
| RC-017 row | Phase 6.1 Submit |

### Card B — L1 Return

| Field | Value |
| --- | --- |
| Actor | L1 Manager |
| Page | `ClaimsApproval_v2.aspx` |
| Action | Return one or more days with remarks |
| Expected | Header/detail Returned; employee can edit Returned days |
| Evidence | Request Id, Returned status, remarks present |
| RC-017 row | Phase 6.2 Return |

### Card C — RESUBMIT

| Field | Value |
| --- | --- |
| Actor | Employee |
| Page | `SalesMonthlyReview.aspx` |
| Action | Correct Returned day(s) and RESUBMIT |
| Expected | New detail version; UI shows one latest row per day |
| Evidence | Request Id; DetailVersion increased; no duplicate day rows in UI |
| RC-017 row | Phase 6.2 RESUBMIT |

### Card D — Forward Confirmation

| Field | Value |
| --- | --- |
| Actor | L1 Manager |
| Page | `ClaimsApproval_v2.aspx` |
| Action | Forward with confirmation modal (RC-011/013) |
| Expected | Confirm modal appears; forward succeeds; modal auto-closes (no F5) |
| Evidence | Timestamp; header routes to next approver; status remains workflow-valid |
| RC-017 row | Phase 6.2 Forward confirmation |

### Card E — Auditor Completion

| Field | Value |
| --- | --- |
| Actor | Auditor |
| Page | `ClaimsApproval_v2.aspx` |
| Action | Approve days; leave one last day for final approve |
| Expected | Intermediate days: header stays **Pending**; **last** latest day → header **Completed** (RC-014B) |
| Evidence | Request Id; history row Pending→Completed / “Final Auditor Approval”; queue exit |
| RC-017 row | Phase 6.3 Final approve |

### Card F — Ready for Payment

| Field | Value |
| --- | --- |
| Actor | Auditor |
| Page | `ClaimsApproval_v2.aspx` (Ready action on Completed claim) |
| Action | Mark Ready for Payment |
| Expected | Header **Completed → ReadyForPayment**; history persists (RC-014D) |
| Evidence | Request Id; Status ReadyForPayment; history remarks “Auditor marked Ready for Payment” |
| RC-017 row | Phase 6.3 Ready for Payment |

### Card G — Financial Proof

| Field | Value |
| --- | --- |
| Actor | QA / Business |
| Page / SQL | Header via `usp_GetApprovalRequest`; Excel via `usp_Export_Auditor_Final_Excel`; snapshots via latest details |
| Action | Reconcile Production ReadyForPayment claim (Request **97** equivalent if same lineage, else named Production Id) |
| Expected | Final = Snapshot DayNet sum = Excel SUM; Status ReadyForPayment; if Stationary ≠ 0, expense includes Stationary (**015B**) |
| Evidence | Fill §4 table Actuals; record Request Id and amounts in RC-017 Phase 6.4 / Phase 7 |
| UAT reference only | Request 97 = ₹7,736.44 — **do not paste into Actual unless Production yields it** |

---

## 4. Production Financial Proof Template (maps to RC-017 Phase 6.4 / Phase 7)

**Production request id:** ________________  
**Employee / period:** ________________  
**Captured by / time:** ________________  

UAT reference (not Production evidence): Request **97** Final = Total = Snapshots = Excel = **₹7,736.44**.

| Layer | Expected | Actual |
| --- | --- | --- |
| Latest Snapshots (DayNet sum) | Match Final / Excel | |
| `TotalApprovedAmount` | Match on Stationary=0; Final is authority if Stationary ≠ 0 | |
| `FinalApprovedAmount` | Match Snapshots / Excel | |
| Excel (`usp_Export_Auditor_Final_Excel` SUM) | Match Final; one latest Approved row per activity | |
| ReadyForPayment | Status unchanged through Recalc/Get/Export | |

| Cross-check | Actual amount / status | PASS? |
| --- | --- | --- |
| Employee surface | | |
| L1 surface | | |
| Auditor / modal | | |
| Ready-for-Payment | | |
| Excel | | |
| SQL Header | | |

**Stationary ≠ 0?** Yes / No — if Yes, confirm expense SP wrote Stationary (RC-015B): ________

Copy completed Actuals into `RC017_Production_Execution_Log.md` Phases 5.4 and 6.

---

## 5. RC-015B Mandatory Checkpoint

| Gate | Requirement | Operator check |
| --- | --- | --- |
| Package | Tag includes four-part `usp_UpdateSalesDailyExpenseStatus` | Confirm file in deploy packet |
| Apply | Step 5 in SQL order (after Get/Export) | Record Start/Finish/Result in RC-017 |
| Body marker | `Snapshot_StationaryAmount` + ExpenseTotal includes Stationary | Script-out or OBJECT_DEFINITION after apply |
| UAT myth | “Already on UAT” is **false** for expense writer as of Pre-CAB | Do **not** skip on Production |
| Money risk if skipped | Snapshot drift when Stationary ≠ 0 until next Recalc | CAB / DBA acknowledge |

**Hard stop:** If SQL window ends without 015B applied → **GO-LIVE WITH CONDITIONS** or extend window; do not claim full FSOT Stationary writer closed.

---

## 6. Go / No-Go Decision Checklist

Complete at end of window before **Phase 9** Final Certification verdict.

### Pre-start

- [ ] CAB Approval recorded (Change ID: ________)
- [ ] Freeze still `3779d185` / `tada-v1.1-postgolive-fsot-2026`
- [ ] DB backup + SP script-outs taken
- [ ] IIS backup + pre-deploy DLL timestamp recorded
- [ ] Operators have this pack + RC-017 log open

### Execute

- [ ] Git push commit + tag verified on origin
- [ ] SQL steps 1–5 complete including **RC-015B**
- [ ] Final backfill OK count recorded (FAIL = 0 or accepted with note)
- [ ] 014B / 014D applied or confirmed already on Production
- [ ] IIS published from tag + recycled
- [ ] Cards A–G executed

### Prove

- [ ] Financial proof Actuals filled (not copied from UAT)
- [ ] Cross-surface amounts reconcile
- [ ] Rollback **Not required** (or Executed + verified)

### Verdict (choose one in RC-017 Phase 9)

| Option | When |
| --- | --- |
| **GO-LIVE CERTIFIED** | All checks PASS including 015B + financial reconcile |
| **GO-LIVE WITH CONDITIONS** | Core workflow/money PASS but a documented condition remains (e.g. 015B deferred with CAB risk acceptance) |
| **ROLLBACK REQUIRED** | Critical smoke or financial FAIL |

- [ ] DBA / App / QA / Business / CAB sign RC-017 Phase 9

---

## 7. Role cheat-sheet (who does what)

| Role | This pack section | RC-017 phases |
| --- | --- | --- |
| Change / CAB | §6 Pre-start | Phase 1 |
| Release Engineer | §2.1 Git | Phase 2 |
| DBA | §2.2 Backup + SQL + §5 015B | Phase 3–4 |
| App Team | §2.3 IIS | Phase 3 (IIS backup), Phase 5 |
| QA | §3 Cards A–G + §4 | Phase 6–7 |
| Business | §4 sign-off + §6 Verdict | Phase 7, 9 |
| All | §6 Rollback awareness | Phase 8 |

---

## 8. Pack closure

| Deliverable | Status |
| --- | --- |
| Freeze verification | Complete — no deviation on SHA/tag |
| Copy-paste command sheet | §2 |
| Validation cards | §3 (A–G) |
| Financial proof template | §4 (Actual blank) |
| RC-015B mandatory checkpoint | §5 |
| Go/No-Go checklist | §6 |
| Deployment actions executed | **None** |

**Next action after CAB approval:** Open `RC017_Production_Execution_Log.md` → execute §2 in order → fill cards → complete Phase 9.
