# RC-017 Pre-CAB Operational Readiness Report

**RC:** Pre-CAB audit (read-only)  
**Captured:** 2026-09-16  
**Agent role:** Release Operations  
**Baseline under audit:** freeze commit `3779d185` / tag `tada-v1.1-postgolive-fsot-2026`  
**Constraint:** No code, SQL, Git, tag, push, or IIS publish actions were performed.

| Field | Value |
| --- | --- |
| Branch | `Aug26_AILWorks_AmimnrupSupport` |
| HEAD | `3779d185001b3182042d5c8d38e5b111675d8dee` |
| Tag object | `2f361f3434de3985949073ec3a06ba08ccb3aaa6` → commit `3779d185` |
| UAT MCP | `anmol_uat` → live procedure definitions (read-only) |
| Overall CAB readiness | **CONDITIONAL GO — READY FOR CAB** |

---

## Section scorecard

| # | Section | Result |
| --- | --- | --- |
| 1 | Git Integrity | **PASS** (with notes) |
| 2 | Release Artifacts | **PASS** (post-freeze ops docs untracked by design) |
| 3 | SQL Readiness (UAT vs repo) | **PASS with known 015B gap** |
| 4 | IIS / Build Readiness | **PASS with WARN** (local Express path drift; Production IIS not in scope of MCP) |
| 5 | RC-017 Readiness | **PASS** |
| — | **CAB readiness verdict** | **CONDITIONAL GO** |

---

## 1. Git Integrity — PASS

| Check | Observed | Result |
| --- | --- | --- |
| Current branch | `Aug26_AILWorks_AmimnrupSupport` | PASS |
| HEAD commit | `3779d185001b3182042d5c8d38e5b111675d8dee` | PASS |
| Tag `tada-v1.1-postgolive-fsot-2026` | Present locally; `git rev-list -n 1` = `3779d185` | PASS |
| HEAD vs tag tip | Identical (no commits after freeze on branch tip) | PASS |
| Working tree tracked dirtiness | No modified tracked files | PASS |
| Untracked (expected / non-package) | `docs/.../TA_DA_v1.1_Production_Promotion_Runbook.md` (RC-016), `docs/.../RC017_Production_Execution_Log.md` (RC-017), `.cursor/` | NOTE — not part of freeze package |
| Ahead of origin | `origin/Aug26_AILWorks_AmimnrupSupport...HEAD` = **0 behind / 1 ahead** | NOTE — push pending CAB |
| Remote tag | `git ls-remote --tags origin refs/tags/tada-v1.1-postgolive-fsot-2026` → **empty** | NOTE — push pending CAB |
| v1.0 → v1.1 delta | **52 files**, +4316 / −269 (matches freeze narrative) | PASS |

**Blockers:** None for CAB package integrity. Push of commit + annotated tag remains a **CAB-gated** action (not a freeze defect).

---

## 2. Release Artifacts — PASS

| Artifact | Exists | Consistency |
| --- | --- | --- |
| `TA_DA_PostGoLive_Consolidated_Certification.md` | Yes (in freeze commit) | Tag / SHA / RC-015B gap aligned |
| `TA_DA_Deployment_Checklist.md` | Yes (in freeze commit) | v1.1 order + 015B mandatory |
| `TA_DA_v1.1_Production_Promotion_Runbook.md` | Yes (disk; **untracked** post-freeze) | SHA `3779d185`, tag, 52-file delta match |
| `RC017_Production_Execution_Log.md` | Yes (disk; **untracked** post-freeze) | Status OPEN; evidence fields PENDING |

Internal consistency: freeze SHA, tag name, six-procedure inventory, and Request **97** / **₹7,736.44** UAT reference agree across certification, checklist, runbook, and RC-017 header.

**Note:** RC-016 / RC-017 are operational documentation after the freeze. They are **not** required inside `3779d185` for application/SQL promotion. Optional follow-up (after CAB or as a docs-only commit) can version them separately without retargeting the production tag.

---

## 3. SQL Readiness (MCP `anmol_uat`) — PASS with known 015B gap

Repo markers verified on freeze paths under `Database/Stored Procedures/Approval/`. Live UAT definitions retrieved 2026-09-16 via `get_stored_procedure_definition` (`execute_query` disabled).

| Procedure | Repo (tag `3779d185`) | UAT live | UAT `modify_date` | Deployment needed on **Production** |
| --- | --- | --- | --- | --- |
| `usp_ProcessApprovalAction` | RC-014B `DetailApprovedLevel` gatekeeper + Auditor → `Completed` | **Aligned** (014B body present) | 2026-09-15 **18:04:06** | **YES** (unless Production already matches UAT 014B) |
| `usp_MarkRequestReadyForPayment` | RC-014D named history INSERT | **Aligned** | 2026-09-15 **21:50:31** | **YES** (unless Production already matches) |
| `usp_RecalculateApprovalRequest` | RC-015A FSOT Final roll-up | **Aligned** (`fsot_request_canonical_totals`) | 2026-09-15 **23:55:42** | **YES** + backfill |
| `usp_GetApprovalRequest` | RC-015A `ISNULL(Final, Total)` | **Aligned** | 2026-09-15 **23:56:30** | **YES** |
| `usp_UpdateSalesDailyExpenseStatus` | **RC-015B** four-part Stationary | **Not aligned** — still **three-part** (DA+Hotel+Travel only); no `Snapshot_StationaryAmount` writer | 2026-09-15 **12:03:55** | **YES — MANDATORY** |
| `usp_Export_Auditor_Final_Excel` | RC-015C latest Approved | **Aligned** (`export_latest_approved_only`) | 2026-09-15 **23:56:30** | **YES** |

### Interpretation

- UAT already carries 014B/D and 015A/C; **015B remains the open UAT SQL gap** (re-confirmed live).  
- Production was **not** queried (no Production MCP connection). Treat all six package procedures as **in-scope for Production apply** per RC-016, and verify Production `modify_date` / body markers during the window.  
- `FinalApprovedAmount` backfill via `EXEC usp_RecalculateApprovalRequest` remains mandatory after Recalc deploy (RC-015G pattern).

**Blockers for package:** None. **Operational condition:** Production must receive **RC-015B** expense SP even if UAT still shows three-part.

---

## 4. IIS / Build Readiness — PASS with WARN

| Check | Observed | Result |
| --- | --- | --- |
| Build output `AnmolDristi\bin\AnmolDristi.dll` | Exists — **3,088,896** bytes; LastWriteTime **2026-09-16 08:14:16** | PASS (local build present) |
| `obj\rc014d\AnmolDristi.dll` | **2026-09-15 21:54:56** — matches RC-014D certified DLL timestamp | PASS (historical evidence DLL retained) |
| `obj\Release\AnmolDristi.dll` | **2026-09-15 15:58:26** (pre-v1.1 FSOT/014D era relative to freeze) | WARN — do **not** ship Release artifact without rebuild from tag |
| Tag source contains MarkReady `con.Open()` | Confirmed in `ClaimsApproval_v2.aspx.cs` at tag | PASS |
| IIS Express `.vs\config\applicationhost.config` | Site `AnmolDristi` → `...\Anmol_Digitization\AnmolDristi`, port **53713** | PASS (repo-aligned) |
| IIS Express `.vs\AnmolDristi\config\applicationhost.config` | Same site/port → physicalPath **`D:\AIL_AminrupTech\AnmolDristi`** | **WARN** — path missing (`Test-Path` False) |
| Production IIS site / app pool | Not inspected (out of local repo scope) | N/A — record in RC-017 Phase 2/4 |

**Blockers:** None for CAB. **WARN:** Before any local pre-prod smoke on IIS Express, use the `.vs\config` binding (repo path) or repair the stale `D:\AIL_AminrupTech\...` path. For Production, rebuild/publish **from tag `tada-v1.1-postgolive-fsot-2026`** after CAB-approved push; do not assume the 08:14 local `bin` DLL is the Production packet until Release Engineer stamps it.

---

## 5. RC-017 Readiness — PASS

| Check | Observed | Result |
| --- | --- | --- |
| Document status | `OPEN — AWAITING LIVE EXECUTION` | PASS |
| Phase 1–8 evidence fields | All operational results **PENDING** (~106 `PENDING` markers) | PASS |
| Filled PASS/FAIL execution rows | **0** `| PASS |` result cells | PASS |
| UAT leakage into Actual Production columns | None — ₹7,736.44 / Request 97 appear only as **UAT reference / Expected**, not as Production Actual | PASS |
| Exit criteria tracker | All **NO** | PASS |

RC-017 is correctly empty of production events and ready to receive live CAB-window evidence only.

---

## Blockers & conditions

| ID | Severity | Item | CAB impact |
| --- | --- | --- | --- |
| B-01 | Gate (expected) | Commit `3779d185` and tag not on `origin` | Push only after CAB approval |
| B-02 | Condition | RC-015B expense SP not live on UAT | Must still deploy from package to **Production**; Stationary≠0 money cutover depends on it |
| B-03 | WARN | Dual IIS Express physical paths; alt path missing | Local only; does not block Production CAB |
| B-04 | WARN | Post-freeze RC-016/RC-017 docs untracked | Ops hygiene; not a release-binary blocker |
| B-05 | Process | Production SQL/IIS state unknown until window | Use RC-016 backups + RC-017 logging |

**No technical defect found in the frozen release package itself.**

---

## CAB readiness verdict

### **CONDITIONAL GO — READY FOR CAB**

The v1.1 candidate at `3779d185` / `tada-v1.1-postgolive-fsot-2026` is intact, the 52-file delta is confirmed, release docs are consistent, RC-017 is clean of fake production evidence, and UAT re-confirms 014B/D + 015A/C with the known **015B** gap still present on the expense writer.

Proceed to CAB with conditions:

1. Approve push of commit + annotated tag.  
2. Build Production DLL from that tag.  
3. Execute RC-016 order including **mandatory 015B** + Final backfill.  
4. Record all live evidence only in `RC017_Production_Execution_Log.md`.  
5. Issue Phase 8 verdict only after production proof.

---

## Exact commands for the production window

**Do not execute these as part of this audit.** Operators run them only after CAB approval, during the change window, and log outcomes into RC-017.

### A. Release Engineer — push (post-CAB)

```powershell
git checkout Aug26_AILWorks_AmimnrupSupport
git rev-parse HEAD
# Expect: 3779d185001b3182042d5c8d38e5b111675d8dee

git push origin HEAD
git push origin refs/tags/tada-v1.1-postgolive-fsot-2026
```

### B. App Team — build & package from tag

```powershell
git fetch --tags
git checkout tada-v1.1-postgolive-fsot-2026
# Build / publish AnmolDristi (Visual Studio or msbuild Release publish profile)
# Copy published output to a dated package folder; record DLL LastWriteTime into RC-017
```

### C. DBA — backup & script-out (before ALTER)

```sql
-- Shop-standard full backup of Production DB (record job name/path in RC-017)

-- Script-out current live definitions for rollback (SSMS / sqlpackage / SMO), at minimum:
-- usp_UpdateActivityKM
-- usp_ForwardApprovalRequest
-- usp_UpdateSalesDailyExpenseStatus
-- usp_ProcessApprovalAction
-- usp_MarkRequestReadyForPayment
-- usp_RecalculateApprovalRequest
-- usp_GetApprovalRequest
-- usp_Export_Auditor_Final_Excel
```

### D. DBA — SQL apply order (RC-016)

```sql
-- 1) Recalc (015A)
-- Apply: Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql

-- 2) Backfill Final (ReadyForPayment / Completed) — pattern only; loop per shop standard
EXEC dbo.usp_RecalculateApprovalRequest @ApprovalRequestId = <Id>;
-- Record OK/FAIL counts in RC-017 Phase 3.2

-- 3) Get (015A) — Approval (+ Retrieval twin if used)
-- 4) Export (015C) — Approval (+ Retrieval twin if used)
-- 5) Expense Stationary (015B) — MANDATORY
-- Apply: Database/Stored Procedures/Approval/usp_UpdateSalesDailyExpenseStatus.sql
-- 6) ProcessApprovalAction (014B) if Production behind
-- 7) MarkRequestReadyForPayment (014D) if Production behind

-- Verify markers / modify_date after each apply; paste into RC-017
```

### E. App Team — IIS

```powershell
# 1) Copy live site (bin\AnmolDristi.dll + published ASPX) to backup path — record in RC-017
# 2) Publish / copy v1.1 package over Production site
# 3) Recycle application pool
# 4) Authenticated hit: SalesMonthlyReview, ClaimsApproval_v2, MyClaimsView_v2, ApprovedClaimsView
```

### F. QA — smoke + financial proof

```text
Employee: Submit, View Claims
L1: Return, RESUBMIT, Forward confirmation
Auditor: Final approve (last day → Completed), Ready for Payment
Financial: Production ReadyForPayment analogue of Request 97
  Expect reconciliation Final = Snapshots = Excel
  UAT reference amount: ₹7,736.44 (do NOT copy into Actual columns unless Production yields it)
```

### G. Rollback (only if FAIL)

```text
1) Restore IIS backup → recycle
2) Re-apply pre-deploy scripted-out procedure definitions (not repo RC files)
3) Verify modify_date / site load
4) Record Executed in RC-017 Phase 7
```

---

## Audit closure

| Item | Status |
| --- | --- |
| Read-only rules respected | Yes |
| Freeze baseline verified | Yes |
| Report path | `docs/releases/TA_DA_GoLive_2026/RC017_PreCAB_Readiness_Report.md` |
| Next human action | CAB review → approve push/deploy → execute RC-017 live |
