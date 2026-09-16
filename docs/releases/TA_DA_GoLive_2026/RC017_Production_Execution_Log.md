# RC-017 — Production Execution & Go-Live Certification

**Document type:** Live production execution log and certification (not a plan).  
**Runbook:** `docs/releases/TA_DA_GoLive_2026/TA_DA_v1.1_Production_Promotion_Runbook.md` (RC-016)  
**UAT money reference:** `docs/reviews/Post_GoLive/RC015G_FSOT_UAT_Execution.md`  
**Freeze / package authority:** `docs/releases/TA_DA_GoLive_2026/TA_DA_PostGoLive_Consolidated_Certification.md`

| Field | Value |
| --- | --- |
| Release | TA/DA v1.1 Post-Go-Live FSOT |
| Branch | `Aug26_AILWorks_AmimnrupSupport` |
| Target tag | `tada-v1.1-postgolive-fsot-2026` |
| Freeze commit (known) | `3779d185001b3182042d5c8d38e5b111675d8dee` |
| Tag object (known, local at freeze) | `2f361f3434de3985949073ec3a06ba08ccb3aaa6` |
| UAT financial reference | Request **97** = **₹7,736.44** (Final = Total = Snapshots = Excel) |
| Log opened | 2026-09-16 |
| Change ID (prepared) | `TA-DA-v1.1-FSOT-20260916` |
| **Document status** | **PHASE 3 PARTIAL — schema/SP rollback artifacts COMPLETE; window DB backup + IIS backup still open** |

### Recording rule

**Do not invent deployment events.** Fill each field only when that action actually occurs in the production window. Leave blanks as `—` or `PENDING` until observed. This file is the permanent Production evidence record once Phases 1–8 are completed and signed.

---

## Phase 1 — CAB & Change Window

| Field | Value | Timestamp |
| --- | --- | --- |
| CAB Approval | **APPROVED** | **2026-09-16 11:00 +05:30** (operator-stated grant) |
| Change ID | `TA-DA-v1.1-FSOT-20260916` | 2026-09-16 11:00 +05:30 |
| Deployment Window (start) | **OPEN** | **2026-09-16 11:00 +05:30** |
| Deployment Window (end) | To be confirmed at close | PENDING |
| DBA | Deputy Manager | 2026-09-16 11:00 +05:30 |
| Application Engineer | Aminrup (Cursor Profile) | 2026-09-16 11:00 +05:30 |
| QA | Ayushi Jaiswal | 2026-09-16 11:00 +05:30 |
| Business Approver | IT | 2026-09-16 11:00 +05:30 |
| Phase 1 complete | **YES** | **2026-09-16 11:00 +05:30** |

CAB conditions (binding for this window):

1. Mandatory deployment of **RC-015B** (`usp_UpdateSalesDailyExpenseStatus`) before IIS validation.  
2. Complete RC-017 evidence capture before issuing Phase 8 verdict.

Notes (actual only):

```
2026-09-16 11:00 +05:30 — CAB approval confirmed by operator (RC-017B).
Change ID TA-DA-v1.1-FSOT-20260916.
Participants: DBA=Deputy Manager; App Eng=Aminrup (Cursor Profile); QA=Ayushi Jaiswal; Business=IT.
Release identity: Aug26_AILWorks_AmimnrupSupport / 3779d185 / tada-v1.1-postgolive-fsot-2026.
UAT financial reference retained as reference only: Request 97 = ₹7,736.44.
Log note: prior system-clock capture 11:10:14 +05:30 was superseded for Approval/Window Start by operator-stated 11:00 +05:30.
Phase 1 COMPLETE. Next: Phase 2 Git Promotion — operator executes push; Cursor waits for evidence (do not auto-push).
```

---

## Phase 2 — Git Promotion

| Step | Result | Evidence | Timestamp |
| --- | --- | --- | --- |
| `git checkout` / on branch | **PASS** | Already on `Aug26_AILWorks_AmimnrupSupport`; ahead by 1 before push | Operator session 2026-09-16 |
| `git rev-parse HEAD` | **PASS** | `3779d185001b3182042d5c8d38e5b111675d8dee` | Operator session 2026-09-16 |
| `git push origin HEAD` | **PASS** | `94bc208..3779d18  HEAD -> Aug26_AILWorks_AmimnrupSupport` | Operator session 2026-09-16 |
| `git push origin refs/tags/tada-v1.1-postgolive-fsot-2026` | **PASS** | `* [new tag] tada-v1.1-postgolive-fsot-2026 -> tada-v1.1-postgolive-fsot-2026` | Operator session 2026-09-16 |
| Remote branch SHA | **PASS** | `ls-remote` → `3779d185001b3182042d5c8d38e5b111675d8dee` | Operator session 2026-09-16 |
| Remote tag object | **PASS** | `ls-remote` → `2f361f3434de3985949073ec3a06ba08ccb3aaa6` (annotated tag object) | Operator session 2026-09-16 |
| Phase 2 complete | **YES** | Push + remote verify | Evidence logged **2026-09-16 11:21:01 +05:30** |

Notes (actual only):

```
Operator pasted PowerShell Package Manager Console output (RemoteException noise on git stderr is non-fatal; push succeeded).
Dependabot vulnerability notice on remote is informational only — not a push failure.
Remote branch tip matches freeze commit 3779d185.
Remote annotated tag object matches local tag object 2f361f34.
Exact push start/finish wall-clock not separately stated by operator; completion evidenced by successful push + ls-remote; intake timestamp 2026-09-16 11:21:01 +05:30.
Next: Phase 3 Backup Confirmation — DB backup, SP script-outs, IIS backup, package/tag verify.
```

---

## Phase 3 — Backup Confirmation

| Check | Result | Path / evidence | Timestamp |
| --- | --- | --- | --- |
| Database backup completed | **PARTIAL (stale full in msdb)** | Latest successful Full for `AnmolDristi`: `D:\anmoldb\Backups\AnmolDristi_200726.bkp` — finish **2026-07-20 18:47:44** (server datetime as returned). **Not** a same-window pre-deploy backup for 2026-09-16. | Harvested **2026-09-16 13:08:16 +05:30** via `anmol_prod_ro` |
| Live SP script-outs taken (rollback source) | **COMPLETE** | SSMS schema baselines + curated SP rollback package: `docs/releases/TA_DA_GoLive_2026/RC017_Production_Schema_20260916.sql`, `RC017_UAT_Schema_20260916.sql`, `RC017_SP_Rollback_Production_20260916.sql` | Generated / promoted **2026-09-16** (SSMS script date 16:19:54 Prod / 16:21:05 UAT) |
| IIS site backup completed | **PENDING — production evidence not yet available.** | No Production IIS site backup path found (`D:\AIL_AminrupTech` absent; `C:\inetpub` present but no stamped site backup) | Re-checked **2026-09-16 13:02:55 +05:30** |
| Pre-deploy DLL timestamp recorded | **PENDING — production evidence not yet available.** | Repo workspace only: `AnmolDristi\bin\AnmolDristi.dll` LastWriteTime **2026-09-16 08:14:16** (local build — **not** IIS Production pre-deploy backup) | Observed **2026-09-16 11:32:13 +05:30** |
| Application package verified from tag `tada-v1.1-postgolive-fsot-2026` | **PARTIAL (source only)** | Git HEAD exact-match tag `tada-v1.1-postgolive-fsot-2026` / SHA `3779d185` on build machine repo — **not** a stamped publish package path | Observed **2026-09-16 11:32:13 +05:30** |
| Tag present on build machine / deploy source | **PASS (git)** | `git describe --exact-match HEAD` → `tada-v1.1-postgolive-fsot-2026`; origin already holds commit + tag (Phase 2) | Observed **2026-09-16 11:32:13 +05:30** |
| Production SP inventory (read-only) | **PARTIAL** | **3 of 6** v1.1 procedures exist on `AnmolDristi`; 3 missing (see Task B table). No repo compare in this step. | Harvested **2026-09-16 13:08:16 +05:30**; count corrected **2026-09-16** |
| Phase 3 complete | **NO** | Schema/SP rollback artifacts COMPLETE; stale msdb Full + IIS/window backup still open — do **not** start Phase 4 until hard-gate backups recorded | |

### Task A — Database backup (`msdb` via `anmol_prod_ro`)

| Field | Evidence |
| --- | --- |
| Backup type | **Full** (`backupset.type = 'D'`, `is_copy_only = 0`) |
| Backup start | **2026-07-20 18:47:31** (msdb `backup_start_date`, as returned) |
| Backup finish | **2026-07-20 18:47:44** (msdb `backup_finish_date`, as returned) |
| Backup path | `D:\anmoldb\Backups\AnmolDristi_200726.bkp` |
| Backup size | **1,011,855,360** bytes (~964.6 MiB); compressed size same |
| Backup device | **Disk** (`backupmediafamily.device_type = 2`) |
| Backup set name | `AnmolDristi-Full Database Backup` |
| Source | `msdb.dbo.backupset` ⨝ `msdb.dbo.backupmediafamily`; server `MUMANMOAPP01\SQLEXPRESS`; database `AnmolDristi` |
| Status | **PARTIAL** — latest successful Full recorded; **not** a 2026-09-16 CAB-window pre-deploy backup |

### Task B — Live SP script-out / Production inventory

| Field | Evidence |
| --- | --- |
| Script-out / rollback package path | **COMPLETE** — `docs/releases/TA_DA_GoLive_2026/RC017_Production_Schema_20260916.sql` (Prod), `RC017_UAT_Schema_20260916.sql` (UAT), `RC017_SP_Rollback_Production_20260916.sql` (six-procedure rollback extract) |
| Contains six v1.1 procedures | **YES (rollback package sections 1–6)** — 3 restore CREATE from Prod baseline; 3 ABSENT→DROP IF EXISTS (`ProcessApprovalAction`, `RecalculateApprovalRequest`, `GetApprovalRequest`) |
| Additional / expected 8-object inventory (`usp_UpdateActivityKM`, `usp_ForwardApprovalRequest`) | Live: `usp_UpdateActivityKM` **present** (`modify_date` **2026-06-08 15:23:22**); `usp_ForwardApprovalRequest` **ABSENT** |
| Status | **COMPLETE** for generated schema/SP rollback artifacts; live IIS/window DB backup still open elsewhere in Phase 3 |

Required objects — live Production existence / `modify_date` (no repo compare):

| Procedure | Present on Production | `modify_date` |
| --- | --- | --- |
| `usp_ProcessApprovalAction` | **NO** | **ABSENT** |
| `usp_MarkRequestReadyForPayment` | **YES** | **2026-01-09 17:48:18.727** |
| `usp_RecalculateApprovalRequest` | **NO** | **ABSENT** |
| `usp_GetApprovalRequest` | **NO** | **ABSENT** |
| `usp_UpdateSalesDailyExpenseStatus` | **YES** | **2026-03-19 16:05:09.620** |
| `usp_Export_Auditor_Final_Excel` | **YES** | **2026-07-23 00:20:10.187** |
| `usp_UpdateActivityKM` (8-object set) | **YES** | **2026-06-08 15:23:22.267** |
| `usp_ForwardApprovalRequest` (8-object set) | **NO** | **ABSENT** |

### Task C — IIS backup (empty until App Team evidence)

| Field | Evidence |
| --- | --- |
| Site backup path | **PENDING — production evidence not yet available.** |
| Application package path | **PENDING — production evidence not yet available.** |
| Pre-deploy DLL timestamp (Production site) | **PENDING — production evidence not yet available.** (local repo DLL 2026-09-16 08:14:16 is not accepted as Production pre-deploy) |
| Status | **PENDING — production evidence not yet available.** |

### Task D — Package verification

| Check | Result |
| --- | --- |
| Deployment publish package exists (stamped path) | **PENDING — production evidence not yet available.** |
| Package matches promoted tag | **UNVERIFIED** (git tag present; publish folder not found) |
| No unexpected binaries in Production package | **PENDING — production evidence not yet available.** |
| Status | **PENDING — production evidence not yet available.** |

Evidence notes (actual only):

```
2026-09-16 11:32:13 +05:30 — Phase 3 automated search (RC-017B). Read-only.
Searched: repo tree, docs/releases, Database/, D:\Backups, D:\Backup, C:\Backups, D:\AIL_AminrupTech, C:\inetpub, Desktop — no Production .bak / IIS backup / script-out package found.
MCP DB connections available: anmol_uat only (UAT). No Production SQL connection for backup history.
NuGet packages\ folder at repo root is NOT a release deploy package.
Phase 3 NOT COMPLETE. Do not start Phase 4 SQL until operator/DBA/App paste the four evidence sets below.

2026-09-16 13:08:16 +05:30 — RC-017B Phase 3.1 harvest (read-only) via anmol_prod_ro connection
(profile in .cursor/mcp.json; SELECT against AnmolDristi / msdb only — no ALTER/DML/EXECUTE deploy).
Context: MUMANMOAPP01\SQLEXPRESS / AnmolDristi / login sa.
Latest Full backup: D:\anmoldb\Backups\AnmolDristi_200726.bkp (2026-07-20 18:47:31–18:47:44), Disk, 1,011,855,360 bytes.
WARN: backup finish is ~58 days before CAB window — not a pre-deploy window backup.
Live SPs present: MarkReady, UpdateSalesDailyExpenseStatus, Export_Auditor_Final_Excel, UpdateActivityKM.
Live SPs ABSENT: ProcessApprovalAction, RecalculateApprovalRequest, GetApprovalRequest, ForwardApprovalRequest.
Script-out package: still unavailable (no Production script-out path found).
IIS / publish package: still PENDING.
Phase 3 status PARTIAL — do NOT start Phase 4.

2026-09-16 — RC-017 documentation reconciliation (no deploy).
Inventory count corrected: 3 of 6 v1.1 SPs present on Production (not 4 of 6).
Generated artifacts marked COMPLETE:
  RC017_Production_Schema_20260916.sql
  RC017_UAT_Schema_20260916.sql
  RC017_SP_Rollback_Production_20260916.sql
Window DB backup + IIS site backup remain PENDING — Phase 3 still NOT COMPLETE for SQL deploy gate.
```

---

## Phase 4 — SQL Deployment Log

Deploy order per RC-016 (do not reorder money steps): Recalc → Final backfill → Get → Export → Expense (015B) → ProcessApprovalAction → MarkReady (latter two if not already on Production).

### 4.1 Procedure apply

| Procedure | RC | Start | Finish | Result | Production `modify_date` / evidence |
| --- | --- | --- | --- | --- | --- |
| `usp_RecalculateApprovalRequest` | RC-015A | PENDING | PENDING | PENDING | |
| `usp_GetApprovalRequest` | RC-015A | PENDING | PENDING | PENDING | |
| `usp_Export_Auditor_Final_Excel` | RC-015C | PENDING | PENDING | PENDING | |
| `usp_UpdateSalesDailyExpenseStatus` | RC-015B | PENDING | PENDING | PENDING | |
| `usp_ProcessApprovalAction` | RC-014B | PENDING | PENDING | PENDING | |
| `usp_MarkRequestReadyForPayment` | RC-014D | PENDING | PENDING | PENDING | |

Inherited v1.0 (only if Production was behind go-live):

| Procedure | RC | Start | Finish | Result | Evidence |
| --- | --- | --- | --- | --- | --- |
| `usp_UpdateActivityKM` | RC-008 | PENDING / N/A | | | |
| `usp_ForwardApprovalRequest` | RC-012A | PENDING / N/A | | | |

### 4.2 `FinalApprovedAmount` backfill

| Field | Value |
| --- | --- |
| Mechanism used | PENDING — must be `EXEC dbo.usp_RecalculateApprovalRequest` only |
| Scope (statuses / filter) | PENDING |
| Rows / requests processed | PENDING |
| OK count | PENDING |
| FAIL count | PENDING |
| Start timestamp | PENDING |
| Finish timestamp | PENDING |
| Result | PENDING |

Backfill notes (actual only):

```

```

Phase 4 complete timestamp: **PENDING**

---

## Phase 5 — IIS Deployment

| Check | Result | Timestamp | Evidence |
| --- | --- | --- | --- |
| DLL / package published to Production site | PENDING | | |
| Published output newer than IIS backup | PENDING | | |
| Application pool recycle | PENDING | | |
| Site availability (HTTP reachability) | PENDING | | |
| First successful authenticated page load | PENDING | | |
| Page URL(s) loaded | PENDING | | |
| Phase 5 complete | PENDING | | |

IIS notes (actual only — no assumptions):

```

```

---

## Phase 6 — Live Smoke Tests

Execute certified scenarios from RC-016 §4. Record **observed** results only.

### 6.1 Employee

| Scenario | Result | Timestamp | Evidence / request id |
| --- | --- | --- | --- |
| Submit | PENDING | | |
| View Claims | PENDING | | |

### 6.2 L1

| Scenario | Result | Timestamp | Evidence / request id |
| --- | --- | --- | --- |
| Return | PENDING | | |
| RESUBMIT | PENDING | | |
| Forward confirmation | PENDING | | |

### 6.3 Auditor

| Scenario | Result | Timestamp | Evidence / request id |
| --- | --- | --- | --- |
| Final approve (last day → `Completed`) | PENDING | | |
| Ready for Payment | PENDING | | |

### 6.4 Financial proof

UAT certified reference (not a Production observation): Request **97** = **₹7,736.44** per RC-015G.

| Field | Expected (UAT ref) | Actual (Production) | Result |
| --- | --- | --- | --- |
| Request id used | 97 (UAT) / Production equivalent | PENDING | PENDING |
| `FinalApprovedAmount` | ₹7,736.44 (UAT) | PENDING | PENDING |
| `TotalApprovedAmount` | ₹7,736.44 (UAT) | PENDING | PENDING |
| Latest snapshot DayNet sum | ₹7,736.44 (UAT) | PENDING | PENDING |
| Excel export SUM | ₹7,736.44 (UAT) | PENDING | PENDING |
| Status | ReadyForPayment | PENDING | PENDING |
| Stationary ≠ 0 check (015B) if applicable | Final matches snapshots | PENDING / N/A | PENDING |

Financial proof notes (actual only):

```

```

Phase 6 complete timestamp: **PENDING**

---

## Phase 7 — Cross-Surface Validation

Every amount must reconcile to the same Production payable. Fill only observed surfaces.

| Surface | Amount / status observed | Result | Timestamp |
| --- | --- | --- | --- |
| Employee | PENDING | PENDING | |
| L1 | PENDING | PENDING | |
| Auditor | PENDING | PENDING | |
| Ready-for-Payment | PENDING | PENDING | |
| Excel | PENDING | PENDING | |
| SQL Header (`FinalApprovedAmount`) | PENDING | PENDING | |

Reconciliation statement (actual only):

```

```

Phase 7 complete timestamp: **PENDING**

---

## Phase 8 — Rollback Checkpoint

| Field | Value |
| --- | --- |
| Rollback status | **PENDING** — choose when known: Not required / Prepared / Executed |
| SQL rollback performed | PENDING |
| IIS rollback performed | PENDING |
| Reason (if not executed) | PENDING — document only after Go decision or abort |
| Reason (if executed) | PENDING |
| Post-rollback verification | PENDING / N/A |
| Checkpoint timestamp | PENDING |

Rollback notes (actual only):

```

```

---

## Phase 9 — Final Certification

### 9.1 Deployment Summary

Fill only after Phases 1–8 have observed outcomes.

| Item | Result |
| --- | --- |
| Git Promotion | **PASS** |
| SQL | PENDING |
| IIS | PENDING |
| Smoke | PENDING |
| Financial Proof | PENDING |
| Rollback | PENDING |

### 9.2 Executive Verdict

**PENDING** — choose exactly one when evidence supports it:

- GO-LIVE CERTIFIED  
- GO-LIVE WITH CONDITIONS  
- ROLLBACK REQUIRED  

Verdict rationale (evidence citations only — no speculation):

```

```

### 9.3 Sign-off

| Role | Name | Signature | Timestamp | Result |
| --- | --- | --- | --- | --- |
| DBA | Deputy Manager | | | PENDING |
| Application Engineer | Aminrup (Cursor Profile) | | | PENDING |
| QA | Ayushi Jaiswal | | | PENDING |
| Business Approver | IT | | | PENDING |
| CAB / Change | | | | PENDING |

---

## Exit Criteria Tracker

RC-017 is **complete** only when all are true with recorded evidence:

| Criterion | Met? |
| --- | --- |
| CAB details recorded | YES — approval 2026-09-16 11:00 +05:30; DBA=Deputy Manager; QA=Ayushi Jaiswal; Business=IT |
| Git promotion verified on origin | YES — branch `3779d185`; tag object `2f361f34` |
| Every deployment phase has timestamps | NO |
| Six SQL procedures logged | NO |
| IIS deployment recorded | NO |
| Employee, L1, Auditor, Ready-for-Payment smoke PASS | NO |
| Financial proof reconciles | NO |
| Rollback status documented | NO |
| Final certification signed off | NO |

**Overall RC-017 status:** **IN PROGRESS — Phase 2 complete; Phase 3 BLOCKED pending Production backup evidence**

---

## Appendix — Known package facts (pre-window; not Production proof)

These are freeze/UAT facts already certified elsewhere. They do **not** satisfy Phases 2–8.

| Fact | Source |
| --- | --- |
| Tag `tada-v1.1-postgolive-fsot-2026` exists locally on freeze commit `3779d185` | Freeze / RC-016 |
| Package includes RC-014B/D and RC-015A/B/C | Consolidated certification |
| UAT Request 97 Final = Total = Snapshots = Excel = ₹7,736.44 | RC-015G |
| RC-015B mandatory in Production package | RC-016 / checklist |
| Push / Production deploy require CAB | RC-016 |
