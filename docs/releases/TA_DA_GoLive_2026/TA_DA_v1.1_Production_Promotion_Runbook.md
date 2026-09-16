# TA/DA v1.1 Production Promotion Runbook (CAB Ready)

**RC:** RC-016  
**Document type:** Production promotion / release orchestration only  
**Constraint:** No new business logic. No application code, SQL script, Git, tag, or UAT evidence changes in this task.  
**Authority:** CAB-approved execution of the frozen package already certified under RC-001 through RC-015G.

| Field | Value |
| --- | --- |
| Release | TA/DA v1.1 Post-Go-Live FSOT |
| Branch | `Aug26_AILWorks_AmimnrupSupport` |
| Historical base tag | `tada-v1.0-go-live-2026` (`94bc2089`) — RC-001–RC-013 only; **do not ship alone** |
| Production target tag | `tada-v1.1-postgolive-fsot-2026` |
| Freeze commit | `3779d185001b3182042d5c8d38e5b111675d8dee` |
| Tag object | `2f361f3434de3985949073ec3a06ba08ccb3aaa6` |
| Delta vs v1.0 | **52 files** (+4316 / −269) |
| Package includes | RC-014B, RC-014D, RC-015A, RC-015B, RC-015C (+ certified evidence through RC-015G) |
| Push / deploy | Only after CAB approval |

### Evidence map (do not re-author RCs)

| Need | Source |
| --- | --- |
| Freeze / Go–No-Go | `TA_DA_PostGoLive_Consolidated_Certification.md` |
| Tick-box deploy order | `TA_DA_Deployment_Checklist.md` |
| FSOT UAT money proof | `docs/reviews/Post_GoLive/RC015G_FSOT_UAT_Execution.md` |
| Tag vs 014 gap (historical) | `docs/reviews/Post_GoLive/Release_Traceability_RC014.md` |
| Post-014D product baseline | `docs/reviews/Post_GoLive/Post_GoLive_Baseline.md` |
| Workflow smoke scenarios | `TA_DA_Smoke_Test_Execution.md` |

---

## 1. Executive Summary

Promote **TA/DA v1.1 Post-Go-Live FSOT** from the local freeze baseline to Production under change control.

| Item | Decision |
| --- | --- |
| What ships | Tag `tada-v1.1-postgolive-fsot-2026` (DLL + six post-go-live SQL procedures + inherited v1.0 objects if Production lags) |
| What must not ship | Tag `tada-v1.0-go-live-2026` alone — missing Auditor completion, Ready-for-Payment fix, and FSOT |
| UAT status | Workflow + FSOT Net Payable certified; Request **97** = **₹7,736.44** across Final / Total / Snapshots / Excel (RC-015G) |
| Known package gate | **RC-015B** four-part Stationary expense SP is **in the v1.1 package** and is **mandatory on Production**; live UAT expense SP was still three-part at certification time |
| Overall | **Conditional GO** after CAB: push tag → build DLL → execute this runbook → sign off |

Absorbed post-v1.0 capabilities:

- **RC-014B** — Auditor last-day `Completed` (gatekeeper)
- **RC-014D** — Ready-for-Payment persistence (named history INSERT + connection open)
- **RC-015A** — Canonical SQL-owned totals (`FinalApprovedAmount`)
- **RC-015B** — Stationary alignment in expense status writer
- **RC-015C** — Latest-version Excel export

---

## 2. Deployment Inventory

### 2.1 Six production SQL procedures (v1.1 mandatory)

Prefer Approval path scripts from the tag. Keep Retrieval twins in sync if Production deploys Get/Export from Retrieval.

| Procedure | RC | Repository path | Role |
| --- | --- | --- | --- |
| `usp_ProcessApprovalAction` | RC-014B | `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` | Day approve / Auditor last-day → `Completed` |
| `usp_MarkRequestReadyForPayment` | RC-014D | `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql` | `Completed` → `ReadyForPayment`; named history INSERT |
| `usp_RecalculateApprovalRequest` | RC-015A | `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql` | Roll-up owner of `FinalApprovedAmount` |
| `usp_GetApprovalRequest` | RC-015A | `Database/Stored Procedures/Approval/usp_GetApprovalRequest.sql` (+ Retrieval twin) | Header Net Payable read (`ISNULL(Final, Total)`) |
| `usp_UpdateSalesDailyExpenseStatus` | RC-015B | `Database/Stored Procedures/Approval/usp_UpdateSalesDailyExpenseStatus.sql` | Expense snapshot; **DA+Hotel+Travel+Stationary** |
| `usp_Export_Auditor_Final_Excel` | RC-015C | `Database/Stored Procedures/Approval/usp_Export_Auditor_Final_Excel.sql` (+ Retrieval twin) | Latest Approved Excel extract |

### 2.2 Inherited v1.0 SQL (only if Production is behind go-live)

| Procedure | RC | Path |
| --- | --- | --- |
| `usp_UpdateActivityKM` | RC-008 | `Database/Stored Procedures/Submission/usp_UpdateActivityKM.sql` |
| `usp_ForwardApprovalRequest` | RC-012A | `Database/Stored Procedures/Approval/usp_ForwardApprovalRequest.sql` |

### 2.3 Web Forms / application package

Build and publish from tag `tada-v1.1-postgolive-fsot-2026` (at minimum `bin\AnmolDristi.dll` and published ASPX).

| Page | Notes |
| --- | --- |
| `ClaimsApproval_v2.aspx` (+ `.cs` / `.designer.cs`) | L1/Auditor; RC-014D `Open()`; RC-015A Final bind / Recalc call sites |
| `SalesMonthlyReview.aspx` (+ `.cs`) | Employee Submit / Return / KM / RESUBMIT |
| `MyClaimsView_v2.aspx` (+ `.cs`) | Requestor grid + Excel; FSOT footer deltas |
| `ApprovedClaimsView.aspx` (+ `.cs`) | Approver grid; FSOT bind deltas |

### 2.4 Explicitly out of this promotion

| Item | Note |
| --- | --- |
| DDL / new tables / indexes | None |
| Ad-hoc money `UPDATE` outside Recalc | Forbidden |
| Unrelated HRMS / Switch User packages | Separate CAB only |
| Payroll ERP importer wiring | Finance ownership; payable field closed on TA/DA (RC-015E/G) |

---

## 3. Production Deployment Order

Certified sequence (consolidates `TA_DA_Deployment_Checklist.md` + RC-015F/G). **Do not reorder money steps.**

### Step 1 — Backup (hard gate)

| Action | Owner | Dependency |
| --- | --- | --- |
| Full Production SQL backup | DBA | None — **blocks all later steps** |
| Script-out live definitions for the eight rollback objects listed in the checklist | DBA | Backup window |
| Copy live IIS site (`bin\AnmolDristi.dll` + published ASPX) | App Team | Change window |
| Record backup paths, DLL timestamp, app-pool name | DBA + App | Sign-off artifact |

Rollback source for SQL procedures: `docs/releases/TA_DA_GoLive_2026/RC017_SP_Rollback_Production_20260916.sql` — a **curated extract** of the six v1.1 procedure definitions taken verbatim from `RC017_Production_Schema_20260916.sql` (SSMS schema-only baseline). It is **not** a reconstructed or hand-authored procedure body. Full DB / IIS backups from this Step 1 remain the hard-gate restore path for data and site files.

### Step 2 — Deploy SQL

Apply repository bodies (`CREATE OR ALTER` or shop-standard `ALTER`). Preserve signatures.

**If Production still lacks v1.0 go-live objects:** deploy `usp_UpdateActivityKM` then `usp_ForwardApprovalRequest` first.

**v1.1 mandatory order:**

1. `usp_RecalculateApprovalRequest` (RC-015A)  
2. *(Step 3 backfill — see below)*  
3. `usp_GetApprovalRequest` (RC-015A)  
4. `usp_Export_Auditor_Final_Excel` (RC-015C)  
5. `usp_UpdateSalesDailyExpenseStatus` (RC-015B) — **mandatory**  
6. `usp_ProcessApprovalAction` (RC-014B) — if not already on Production  
7. `usp_MarkRequestReadyForPayment` (RC-014D) — if not already on Production  

**Dependencies:** Recalc must exist before backfill. Get/Export after backfill so Production headers already carry Final when UI/export first hit the new contract. Expense SP (015B) closes continuous Stationary writer drift. 014B/D may already be live; still verify `modify_date` / body markers.

Record Production `modify_date` for each object after apply.

### Step 3 — Execute `FinalApprovedAmount` backfill

| Action | Detail |
| --- | --- |
| Mechanism | `EXEC dbo.usp_RecalculateApprovalRequest @ApprovalRequestId = <Id>;` only |
| Scope (certified UAT pattern) | At minimum all `Status IN ('ReadyForPayment','Completed')` |
| Forbidden | Separate hand-written `UPDATE ... FinalApprovedAmount = ...` |
| Evidence pattern | RC-015G: 58 Ready+Completed OK / 0 FAIL; Request 97 Final → **7,736.44** |

**Dependency:** Requires Step 2 Recalc body. Must complete before claiming FSOT closed. Pending/Rejected/Returned Final may remain null until next Recalc — acceptable per certification.

### Step 4 — Deploy IIS binaries

| Action | Detail |
| --- | --- |
| Package | Built from `tada-v1.1-postgolive-fsot-2026` after CAB-approved push |
| Publish | Copy / publish to Production site; confirm newer than IIS backup |
| Why after SQL | Avoids old DLL against null Final **and** new DLL against pre-FSOT Get (modal 0.00 risk mitigated once Final populated + Get ISNULL, but certified order remains SQL→backfill→app) |

### Step 5 — Recycle IIS

Recycle the application pool (or equivalent AppDomain recycle). Hit authenticated entry pages to confirm load:

- `SalesMonthlyReview.aspx`
- `ClaimsApproval_v2.aspx`
- `MyClaimsView_v2.aspx`
- `ApprovedClaimsView.aspx`

Instruct L1/Auditor testers to hard-refresh (Ctrl+F5) for Forward modal / auto-close scripts (RC-011/013).

### Step 6 — Smoke verification

Execute §4. **Go** only if workflow smoke and financial proof both PASS. Any FAIL → §5 Rollback + CAB decision gate.

---

## 4. Smoke Tests

Reuse already-certified scenarios. Do not invent new acceptance formulas.

### 4.1 Workflow smoke (v1.0 paths)

Source: `TA_DA_Smoke_Test_Execution.md` / Go-Live Certification.

| # | Scenario | Expected |
| --- | --- | --- |
| 1 | Employee submit (NEW) | Request created; days visible |
| 2 | L1 Return | Header/detail Returned; employee editable |
| 3 | RESUBMIT | New detail version; latest-version UI one row per day |
| 4 | L1 Forward confirmation | Confirm modal; header routes; modal auto-close (RC-011/013) |
| 5 | Auditor completion | Intermediate days stay Pending; **last** latest day → header `Completed` (RC-014B) |
| 6 | Ready for Payment | From `Completed`, Mark Ready persists `ReadyForPayment` + history (RC-014D) |

### 4.2 Financial proof (v1.1 — mandatory)

Source: `RC015G_FSOT_UAT_Execution.md`.

| Check | Expected |
| --- | --- |
| Reference (UAT certified) | Request **97** |
| Production analogue | Same request if shared lineage, else a Ready-for-Payment claim with known DayNet |
| `FinalApprovedAmount` | Equals latest snapshot DayNet sum |
| `TotalApprovedAmount` | Reconciles on Stationary=0 claims; **Final is authority** if Stationary ≠ 0 |
| Excel (`usp_Export_Auditor_Final_Excel`) | SUM = Final; one latest Approved row per activity |
| Status | Remains `ReadyForPayment` through Recalc / Get / Export |
| Certified UAT amount | **₹7,736.44** |

| Layer (Request 97 UAT) | Amount | Result in RC-015G |
| --- | --- | --- |
| Latest snapshot DayNet | ₹7,736.44 | PASS |
| `TotalApprovedAmount` | ₹7,736.44 | PASS |
| `FinalApprovedAmount` | ₹7,736.44 | PASS |
| Excel export SUM (31 rows) | ₹7,736.44 | PASS |

If any Production day has Stationary ≠ 0, confirm expense SP snapshot includes Stationary and Final still matches (RC-015B gate).

Record Production request id / amounts on the CAB checklist (§7).

---

## 5. Rollback Plan

Trigger: smoke FAIL, financial proof FAIL, site will not start, or CAB abort during window.

### 5.1 IIS rollback

1. Copy the **pre-deploy IIS backup** over the live site (`AnmolDristi.dll` + published files).  
2. Recycle the application pool.  
3. Confirm authenticated pages load without compile / yellow-screen errors.

### 5.2 SQL rollback

1. Re-apply the **pre-deploy** procedure definitions via `RC017_SP_Rollback_Production_20260916.sql` (curated extract from `RC017_Production_Schema_20260916.sql`) or the original Step 1 script-outs — **not** the repository RC promotion files.  
2. Confirm each object’s `modify_date` / body matches the pre-window baseline.  
3. Do **not** restore full database unless Finance explicitly authorizes data undo. Recalc is re-runnable; Final values written by backfill are not automatically reversed by SP rollback alone — treat Final undo as a separate Finance decision if required.

### 5.3 Verification after rollback

| Check | Pass criteria |
| --- | --- |
| L1 inbox | `ClaimsApproval_v2` loads |
| Employee month grid | `SalesMonthlyReview` loads |
| Procedure versions | Match pre-deploy script-outs |
| Incident | Logged with tag SHA, backup paths, failed scenario, time |

### 5.4 Decision gate

| Outcome | Action |
| --- | --- |
| Smoke + financial PASS | Proceed to Business sign-off (§6 / §7) |
| Any critical FAIL | Rollback IIS + SQL → stop promotion → CAB re-plan |
| Partial (SQL OK, UI FAIL) | Prefer full rollback of both tiers unless CAB explicitly accepts SQL-only hold |

---

## 6. Production Sign-Off Matrix

| Role | Responsibility |
| --- | --- |
| **DBA** | SQL backup, script-outs, procedure deploy, Final backfill via Recalc, `modify_date` recording, SQL rollback |
| **App Team** | Build from target tag, IIS backup, publish DLL/ASPX, recycle pool, IIS rollback |
| **QA** | Workflow smoke (§4.1), financial proof (§4.2), Excel validation, Ready-for-Payment validation |
| **Business** | Accept Production money/workflow result; final Go / No-Go sign-off |
| **CAB / Change** | Approve push and production window; authorize abort / rollback |

| Check | Owner | Time | Result |
| --- | --- | --- | --- |
| CAB approval recorded | Change | | |
| SQL backup + script-outs | DBA | | |
| IIS backup | App Team | | |
| Six v1.1 SPs applied (+ v1.0 if needed) | DBA | | |
| Final backfill complete | DBA | | |
| IIS published + recycled | App Team | | |
| Workflow smoke PASS | QA | | |
| Request #97 pattern financial PASS | QA | | |
| Business accept | Business | | |

Deployer: ________________  
Approver (CAB): ________________  
Date: ________________

---

## 7. Final CAB Checklist (one page)

**Release:** TA/DA v1.1 Post-Go-Live FSOT · **Tag:** `tada-v1.1-postgolive-fsot-2026` · **SHA:** `3779d185`  
**Window:** ____________ · **App pool:** ____________ · **Prod request for money proof:** ____________

### Pre-window

- [ ] CAB approval granted; push of commit + tag authorized  
- [ ] Package built from `tada-v1.1-postgolive-fsot-2026` (not v1.0)  
- [ ] RC-015B expense SP confirmed in package (four-part Stationary)  
- [ ] SQL full backup completed — path: ________________  
- [ ] Live SP script-outs taken (8 objects) — path: ________________  
- [ ] IIS site backup taken — path: ________________ · DLL ts: ________________  

### Execute

- [ ] SQL deployed in certified order (Recalc → **backfill Final** → Get → Export → **015B** → 014B/D as needed)  
- [ ] Production `modify_date` recorded for all six v1.1 procedures  
- [ ] IIS binaries published  
- [ ] IIS recycled; pages load  

### Verify

- [ ] Employee submit / L1 Return / RESUBMIT PASS  
- [ ] L1 Forward confirmation PASS  
- [ ] Auditor completion (last day → `Completed`) PASS  
- [ ] Ready-for-Payment PASS  
- [ ] Request #97 pattern: Final = Snapshots = Excel = **₹7,736.44** (or recorded Production equivalent)  
- [ ] Excel latest-version (no duplicate RESUBMIT rows) PASS  

### Close

- [ ] QA sign-off  
- [ ] Business sign-off  
- [ ] Final CAB approval to leave Production on v1.1  

**Abort?** → Restore IIS backup → restore scripted-out SPs → recycle → log incident → stop.

| Role | Name | Signature | Time |
| --- | --- | --- | --- |
| DBA | | | |
| App Team | | | |
| QA | | | |
| Business | | | |
| CAB | | | |

---

## Appendix A — Quick reference amounts & gates

| Gate | Status at runbook authoring |
| --- | --- |
| Freeze commit created | Yes — `3779d185` |
| Tag `tada-v1.1-postgolive-fsot-2026` | Yes — local; push after CAB |
| RC-015B in package | Yes — mandatory on Production |
| Deployment checklist refreshed | Yes — `TA_DA_Deployment_Checklist.md` |
| Request #97 financial proof retained | Yes — RC-015G |
| Push to remote | **Only after CAB approval** |

**Production Go / No-Go:** Conditional GO — execute this runbook after CAB; do not promote from `tada-v1.0-go-live-2026`.
