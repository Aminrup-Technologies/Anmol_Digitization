# TA/DA Post-Go-Live Consolidated Certification

**Captured:** 2026-09-16  
**Freeze executed:** 2026-09-16 — production baseline commit + annotated tag `tada-v1.1-postgolive-fsot-2026` (local; push only after CAB).  
**Absorbs:** RC-001–RC-013 go-live tag + post-tag RC-014B/D + FSOT RC-015A–G  
**Primary evidence:** `docs/releases/TA_DA_GoLive_2026/`, `docs/reviews/Post_GoLive/`, `docs/reviews/L2_Auditor_AsIs/`  
**Live UAT stamps re-confirmed:** MCP `anmol_uat` → `AnmolDristiDev` (2026-09-15/16)

| Item | Value |
| --- | --- |
| Branch | `Aug26_AILWorks_AmimnrupSupport` |
| Historical go-live SHA / tag | `94bc2089` / `tada-v1.0-go-live-2026` (local) — **RC-001–RC-013 only** |
| Production baseline tag | `tada-v1.1-postgolive-fsot-2026` — absorbs RC-014B/D + RC-015A/B/C (+ certified evidence through RC-015G) |
| Certified product (runtime) | Workflow through Ready-for-Payment + FSOT Net Payable on UAT |
| FSOT reference claim | Request **97** — Final = Total = Snapshots = Excel = **₹7,736.44** |
| Open UAT SQL gap | **RC-015B** expense SP still three-part on live UAT; **included in this baseline package** |

---

## Executive summary

| Question | Answer |
| --- | --- |
| Is the platform UAT-certified for workflow + money? | **Yes**, with one open Stationary writer gap (RC-015B SQL not live). |
| Does tag `tada-v1.0-go-live-2026` match certified runtime? | **No.** Tag is RC-001–RC-013 only. |
| Is a new release tag required? | **Yes.** |
| Should RC-014/015 roll into a new production baseline? | **Yes.** |
| Production Go / No-Go | **Conditional GO** — freeze + tag + promote SQL/DLL package first; close RC-015B UAT deploy before Production money cutover if Stationary can be non-zero. |

---

## 1. Release Traceability Matrix (RC-001 → RC-015G)

Legend: **T** = in tag `94bc2089` / `tada-v1.0-go-live-2026` · **W** = working tree · **U** = live UAT · **E** = UAT evidence pack

| RC | Title | Status | T | W | U | E | Evidence / notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| RC-001 | Line-item RESUBMIT | PASS | Y | Y | Y | Y | Go-Live Certification |
| RC-002 | Sequence reserved | PASS | Y | Y | Y | Y | Go-Live Certification |
| RC-003 | My Claims Excel hardening | PASS | Y | Y* | Y | Y | *later FSOT edits on same files |
| RC-004 | Approved Claims latest-version UI | PASS | Y | Y* | Y | Y | |
| RC-005 | My Claims paging | PASS | Y | Y* | Y | Y | |
| RC-006 | Returned visibility | PASS | Y | Y* | Y | Y | |
| RC-007 | Returned TVP filter | PASS | Y | Y | Y | Y | |
| RC-008 | Returned editability + KM gate | PASS | Y | Y | Y | Y | `usp_UpdateActivityKM` 2026-09-14 |
| RC-009 | Already Returned UI | PASS | Y | Y* | Y | Y | |
| RC-010 | Requestor UX polish | PASS | Y | Y | Y | Y | |
| RC-011 | L1 commit UX | PASS | Y | Y* | Y | Y | Smoke Test Execution |
| RC-012 | Latest-version SQL (Forward + expense) | PASS | Y | Y* | Y | Y | Forward / Expense 2026-09-15 12:03:54–55 |
| RC-013 | Modal auto-close after Forward | PASS | Y | Y* | Y | Y | Smoke Test Execution |
| RC-014A | Premature Auditor `Completed` | FAIL (superseded) | N | N | N | Y | Request 96 pack |
| RC-014B | Gatekeeper `DetailApprovedLevel`; last day → `Completed` | PASS | N | Y | Y | Y | Request 97; SP 18:04:06; Hist 687 |
| RC-014C | MarkReady positional INSERT 213 | FAIL (superseded) | N | N | N | Y | Request 95 contract |
| RC-014D | Named history INSERT + `con.Open()` | PASS | N | Y | Y | Y | Request 97; SP 21:50:31; Hist 688; DLL 21:54:56 |
| RC-015A | Canonical totals (roll-up + header bind) | PASS (UAT SQL) | N | Y | Y | Y | Recalc 23:55:42; Get 23:56:30; RC-015G |
| RC-015B | Stationary in expense SP / footers | PASS (repo) / **UAT SQL gap** | N | Y | **N** | Partial | Repo four-part; live expense SP still **three-part** (12:03:55) |
| RC-015C | Excel latest-version | PASS | N | Y | Y | Y | Export 23:56:30; RC-015G |
| RC-015D | Header field cleanup (design) | Closed as design / deferred | N | Docs | N/A | Docs | Absorbed into Get SP Original/Final behavior; no separate prod RC |
| RC-015E | Payroll proof (pre-deploy FAIL → post-deploy re-proven) | PASS after 015G | N | Docs | Y | Y | `RC015E` then `RC015G` Request 97 |
| RC-015F | Deployment readiness runbook | PASS | N | Docs | N/A | Y | `RC015F_FSOT_Deployment_Readiness.md` |
| RC-015G | UAT deploy + financial proof | PASS | N | Docs | Y | Y | `RC015G_FSOT_UAT_Execution.md` |

\* Working-tree files for these RCs also carry later RC-014/015 edits; the original RC behavior remains certified.

### How to read presence

| Location | Meaning |
| --- | --- |
| **Tag only era** | RC-001–RC-013 frozen at `94bc2089` |
| **Working tree only (not tagged)** | RC-014B/D sources; RC-015A/B/C SQL + C#; all Post_GoLive / L2 docs |
| **UAT only (not in tag)** | Live SP bodies for RC-014B/D and RC-015A/C (+ Final backfill data); RC-014D DLL era |
| **Both working tree + UAT** | Certified runtime for workflow + FSOT roll-up/export/header |
| **Working tree but not UAT** | **RC-015B** `usp_UpdateSalesDailyExpenseStatus` four-part Stationary writer |

---

## 2. Gap Matrix

| Gap ID | Item | Tag `94bc2089` | Working tree | Live UAT | Risk if ignored |
| --- | --- | --- | --- | --- | --- |
| G-01 | RC-014B `usp_ProcessApprovalAction` | Missing | Present | Present (18:04:06) | Premature `Completed` (RC-014A) |
| G-02 | RC-014D MarkReady SP + C# `Open()` | Missing | Present | Present (21:50:31 + DLL) | Ready never persists (RC-014C/D) |
| G-03 | RC-015A `usp_RecalculateApprovalRequest` | Missing | Present (new file) | Present (23:55:42) | Final stays NULL |
| G-04 | RC-015A `usp_GetApprovalRequest` | Missing / stale | Present | Present (23:56:30) | Modal binds wrong Original / null Final |
| G-05 | RC-015C `usp_Export_Auditor_Final_Excel` | Missing / stale | Present | Present (23:56:30) | Duplicate Approved rows after RESUBMIT |
| G-06 | RC-015B expense Stationary writer | Missing | Present | **Absent** (still DA+Hotel+Travel only @ 12:03:55) | Snapshot drift when Stationary ≠ 0 until next Recalc |
| G-07 | ClaimsApproval Final bind / Recalc call sites | Partial at tag | Present | DLL may lag working tree | UI shows 0 if old C# + null Final; mitigated now that Final is populated + Get ISNULL |
| G-08 | My Claims / Approved Claims FSOT footer/bind deltas | Tag-era | Modified | Depends on deployed DLL | Display-only unless Stationary ≠ 0 |
| G-09 | Git commit / new tag | Tag = old baseline | Uncommitted delta | Advanced past tag | Production cannot ship tag alone |
| G-10 | Payroll ERP consumer | N/A | Unknown | Unknown | Finance must name intake; ownership of payable is closed |

**Correction to informal status tables:** RC-015B is **repo-complete and footer-aware in working tree**, but **not UAT-SQL-proven**. Recalc (015A) refreshes Stationary on roll-up/backfill; the continuous expense-status writer on UAT is still RC-012B three-part.

---

## 3. Certifications

### 3.1 Workflow certification

| Path | Verdict | Anchor |
| --- | --- | --- |
| Employee Submit / Return / KM / RESUBMIT | PASS | Go-Live + Smoke (Req 96) |
| L1 Forward + modal auto-close | PASS | RC-011/013 |
| Latest-version Forward / expense (RC-012) | PASS | UAT 12:03:54–55 |
| Auditor gatekeeper → `Completed` | PASS | RC-014B Request 97 / Hist 687 |
| Ready-for-Payment | PASS | RC-014D Request 97 / Hist 688 |

### 3.2 Auditor certification

| Item | Verdict |
| --- | --- |
| First-day premature Completed closed | PASS (014B supersedes 014A) |
| MarkReady reachable from Completed | PASS |
| Named history INSERT | PASS (014D supersedes 014C) |
| Connection open before MarkReady | PASS |

### 3.3 FSOT certification

| Layer | Request 97 | Verdict |
| --- | --- | --- |
| Latest snapshot DayNet | ₹7,736.44 | PASS |
| `TotalApprovedAmount` | ₹7,736.44 | PASS |
| `FinalApprovedAmount` | ₹7,736.44 | PASS (was NULL pre-015G) |
| Excel export SUM | ₹7,736.44 (31 rows) | PASS |
| Status ReadyForPayment | Unchanged through FSOT deploy | PASS |
| Ready+Completed Final backfill | 58/58 populated | PASS |
| RC-015A–E closed on UAT | Per RC-015G | PASS |
| Stationary continuous writer (015B) | Live expense SP | **OPEN** |

### 3.4 SQL deployment inventory (certified UAT)

| Procedure | Role | UAT `modify_date` | Package |
| --- | --- | --- | --- |
| `usp_SubmitSalesDailyActivities` | Submit | 2026-07-28 12:39:02 | Tag |
| `usp_UpdateActivityKM` | KM Returned gate | 2026-09-14 13:51:00 | Tag |
| `usp_ForwardApprovalRequest` | Forward latest-version | 2026-09-15 12:03:54 | Tag |
| `usp_UpdateSalesDailyExpenseStatus` | Expense status | 2026-09-15 **12:03:55** | Tag RC-012; **015B not applied** |
| `usp_ProcessApprovalAction` | Day approve / Auditor Completed | 2026-09-15 **18:04:06** | Post-tag 014B |
| `usp_MarkRequestReadyForPayment` | ReadyForPayment | 2026-09-15 **21:50:31** | Post-tag 014D |
| `usp_RecalculateApprovalRequest` | FSOT roll-up + Final | 2026-09-15 **23:55:42** | Post-tag 015A/G |
| `usp_GetApprovalRequest` | Header Net Payable | 2026-09-15 **23:56:30** | Post-tag 015A/G |
| `usp_Export_Auditor_Final_Excel` | Latest Approved export | 2026-09-15 **23:56:30** | Post-tag 015C/G |

Data: Ready+Completed headers backfilled via `EXEC usp_RecalculateApprovalRequest` (RC-015G). Pending/Rejected/Returned Final may remain null until next Recalc — acceptable for those statuses.

### 3.5 UAT evidence inventory

| Pack | Covers |
| --- | --- |
| `docs/releases/TA_DA_GoLive_2026/TA_DA_GoLive_Certification.md` | RC-001–013 |
| `docs/releases/TA_DA_GoLive_2026/TA_DA_Smoke_Test_Execution.md` | Smoke / Forward UX |
| `docs/releases/TA_DA_GoLive_2026/TA_DA_Deployment_Checklist.md` | Tag-era production checklist (**stale for 014/015**) |
| `docs/reviews/L2_Auditor_AsIs/*` | RC-014A–D |
| `docs/reviews/Post_GoLive/Post_GoLive_Baseline.md` | Post-014D baseline (pre-FSOT deploy) |
| `docs/reviews/Post_GoLive/Release_Traceability_RC014.md` | Tag vs 014 gap |
| `docs/reviews/Post_GoLive/Sprint3_Reporting_Reconciliation.md` | Pre-FSOT money audit |
| `docs/reviews/Post_GoLive/Sprint3_FSOT_Design.md` | FSOT design |
| `docs/reviews/Post_GoLive/RC015E_Payroll_Proof.md` | Pre-deploy FAIL |
| `docs/reviews/Post_GoLive/RC015F_FSOT_Deployment_Readiness.md` | Deploy runbook |
| `docs/reviews/Post_GoLive/RC015G_FSOT_UAT_Execution.md` | Deploy PASS + Final = 7,736.44 |
| **This file** | Consolidated freeze / traceability |

---

## 4. Open risks

| ID | Risk | Severity | Mitigation |
| --- | --- | --- | --- |
| R-01 | Tag ≠ certified runtime | High for Production | New commit + new tag before prod |
| R-02 | RC-015B expense SP not on UAT | Medium (Stationary) | Deploy repo expense SP; optional re-proof with Stationary ≠ 0 |
| R-03 | Working-tree C# / DLL not committed | High for Production | Build & commit ClaimsApproval / MyClaims / ApprovedClaims with SQL |
| R-04 | Deployment checklist still RC-001–013 | High | Refresh checklist in freeze commit |
| R-05 | Payroll ERP intake Unknown | Medium (ops) | Finance names Excel vs Final; FSOT ownership already closed |
| R-06 | `TotalApprovedAmount` CK omits Stationary | Medium | Known FSOT design leftover; Final is authority when Stationary ≠ 0 |
| R-07 | appsuat / local IIS flaky in 015G window | Low | SQL proof stands; optional UI screenshot when site up |
| R-08 | Shipping `94bc2089` alone | Critical | Forbidden — reopens 014A/C and null Final |

---

## 5. Freeze recommendation

### 5.1 Is a new tag required?

**Yes.** Do **not** move or retarget `tada-v1.0-go-live-2026`.

| Existing | Keep as |
| --- | --- |
| `tada-v1.0-go-live-2026` → `94bc2089` | Historical RC-001–RC-013 go-live object |

| Proposed new tag | Suggested name |
| --- | --- |
| Post-go-live baseline | `tada-v1.1-postgolive-fsot-2026` (or equivalent annotated tag) |

### 5.2 Should RC-014/015 roll into the new production baseline?

**Yes.** The production baseline must be the **certified runtime**, not the historical tag.

### 5.3 Freeze commit contents (do not execute here)

Stage at minimum:

**SQL**

1. `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` (014B)  
2. `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql` (014D)  
3. `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql` (015A)  
4. `Database/Stored Procedures/Approval/usp_GetApprovalRequest.sql` (015A)  
5. `Database/Stored Procedures/Approval/usp_Export_Auditor_Final_Excel.sql` (015C)  
6. `Database/Stored Procedures/Approval/usp_UpdateSalesDailyExpenseStatus.sql` (**015B — must include**)  
7. Retrieval twins for Get / Export if the shop deploys from Retrieval  

**Application**

8. `AnmolDristi/ClaimsApproval_v2.aspx.cs` (014D Open + 015A Final bind / Recalc)  
9. `AnmolDristi/MyClaimsView_v2.aspx(.cs)` FSOT-related deltas  
10. `AnmolDristi/ApprovedClaimsView.aspx.cs` FSOT-related deltas  

**Docs (this consolidation + evidence)**

11. `docs/reviews/Post_GoLive/**`  
12. `docs/reviews/L2_Auditor_AsIs/**` (as needed)  
13. Update `docs/releases/TA_DA_GoLive_2026/TA_DA_Deployment_Checklist.md` to list 014/015 objects  

Then: new commit on `Aug26_AILWorks_AmimnrupSupport` → annotated tag `tada-v1.1-postgolive-fsot-2026` → rebuild `AnmolDristi.dll` → Production change ticket using RC-015F order for FSOT SPs + 014B/D + **015B**.

### 5.4 Production promotion order (SQL money)

Reuse RC-015F:

1. Recalc → 2. Backfill Final (Ready/Completed) → 3. Get → 4. Export → 5. **also** expense Stationary SP (015B) → 6. ProcessApprovalAction / MarkReady if not already prod → 7. IIS recycle / DLL → 8. Re-proof Request 97 (or Production analogue).

---

## 6. Go / No-Go Verdict

| Area | Verdict |
| --- | --- |
| Workflow UAT | **GO** |
| Auditor close UAT | **GO** |
| FSOT Net Payable UAT (Request 97) | **GO** |
| Tag `tada-v1.0-go-live-2026` as Production package | **NO-GO** (incomplete; historical only) |
| Tag `tada-v1.1-postgolive-fsot-2026` as Production package | **Conditional GO** — after CAB push + DLL build + deploy order (incl. **015B**) |
| Production planning | **Conditional GO** |
| Start Sprint 3 UX / telemetry / performance from FSOT baseline | **GO** (money formula is live on UAT; do not regress Final ownership) |
| Production money cutover with Stationary possible | **HOLD** until RC-015B SQL is applied on target (UAT preferred first, then prod) |

### Overall program

| Gate | Result |
| --- | --- |
| Design-complete → UAT-certified (FSOT) | **Achieved** (RC-015G) |
| Release freeze / packaging complete | **Achieved** — freeze commit + `tada-v1.1-postgolive-fsot-2026` + refreshed checklist |
| Production ready to ship from v1.0 tag alone | **NO-GO** |
| Recommended next action | CAB approve → push tag → build DLL → deploy per checklist (015A Recalc/backfill → Get → Export → **015B** → 014B/D if needed → IIS recycle → Req 97 pattern proof) |

---

## 7. Return summary (requested)

### Traceability matrix

See §1 (RC-001 through RC-015G).

### Gap matrix

See §2 (G-01–G-10). Critical packaging gaps: tag missing 014/015; **015B not on UAT**.

### Recommended release tag

`tada-v1.1-postgolive-fsot-2026` (new annotated tag; keep `tada-v1.0-go-live-2026` historical).

### Go / No-Go

| Decision | Verdict |
| --- | --- |
| UAT product certification (workflow + FSOT Final on Req 97) | **GO** |
| Production ship from `tada-v1.0-go-live-2026` | **NO-GO** |
| Production after `tada-v1.1-postgolive-fsot-2026` + 015B deploy + DLL | **Conditional GO** (CAB + push required) |

Freeze packaging complete locally. Do not push until CAB approval.

---

## 8. RC-023 — Production Hardening (additive; does not supersede §1–§7)

**Captured:** 2026-09-23  
**Commit:** `4644baf` (`4644baf4caa051b0f5d32cc76f1479a0d344a4d4`)  
**Title:** Harden approval authorization, latest-version ownership, and payment locking  
**Branch:** `Aug26_AILWorks_AmimnrupSupport`  
**RC-023 UAT Tag:** `rc-023-uat-certified` @ `4644baf` (UAT certified 2026-09-23)
**Prior certified baseline:** `tada-v1.1-postgolive-fsot-2026` / `rc-017-022-uat-certified` remain historical; this section documents the RC-023 delta only.

### 8.1 Scope (5 files)

| Path | Role |
| --- | --- |
| `AnmolDristi/ClaimsApproval_v2.aspx.cs` | Level-aware `EnsureCanFinalizeRequest`; ReadyForPayment payment lock |
| `AnmolDristi/SalesApprovalDetailVersionHelper.cs` | Latest-detail = `MAX(DetailVersion)` then `MAX(ApprovalRequestDetailId)` |
| `Database/Stored Procedures/Approval/usp_UpdateSalesDailyExpenseStatus.sql` | Expense auth, current approver, Pending-only, payment lock, Stationary (RC-015B) |
| `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` | Version→Id latest ownership; Completed/ReadyForPayment lock |
| `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql` | Contract note only (runtime: Auditor + Completed + named history INSERT) |

Diff: **+328 / −73** (5 files).

### 8.2 DLL / build

| Item | Value |
| --- | --- |
| DLL SHA256 | `04D0250EE80DABB519992CCC70FAB1CB8FCF35D562ACBE33D5A9E9E055AB09BF` |
| Build / publish timestamp | 2026-09-23 12:54:28 +0530 |
| Artifact | `D:\DeployPackages\bin\AnmolDristi.dll` (= `AnmolDristi\bin\AnmolDristi.dll`) |

### 8.3 SQL delta (repository vs certified UAT stamps in §3.4)

| Procedure | RC-023 repo change | Certified UAT `modify_date` (pre-RC-023 deploy) |
| --- | --- | --- |
| `usp_UpdateSalesDailyExpenseStatus` | Auth + payment/header locks + four-part Stationary | 2026-09-15 12:03:55 (three-part; **015B/023 not applied**) |
| `usp_ProcessApprovalAction` | Version→Id latest; Completed/ReadyForPayment lock | 2026-09-15 18:04:06 (Id-only latest; Completed-only lock era) |
| `usp_MarkRequestReadyForPayment` | Documentation comment only | 2026-09-15 21:50:31 (unchanged runtime contract) |

FSOT ownership unchanged: `usp_RecalculateApprovalRequest` remains the sole `FinalApprovedAmount` writer; MarkReady remains the sole `ReadyForPayment` writer.

### 8.4 UAT evidence (executed 2026-09-23)

Pack: `pvt-evidence/2026-09-23-rc023-uat/`. Rollback bodies under `rollback/AnmolDristiDev/`.

| Gate | Result | Evidence / notes |
| --- | --- | --- |
| ALTER `usp_UpdateSalesDailyExpenseStatus` (RC-023 + RC-015B) | **PASS** | `modify_date`: `2026-09-15 12:03:55.347` → `2026-09-23 13:51:38.003` (QI ON) |
| ALTER `usp_ProcessApprovalAction` (RC-023) | **PASS** | `modify_date`: `2026-09-15 18:04:06.090` → `2026-09-23 13:51:38.750` (QI ON) |
| Publish / recycle DLL (`4644baf` / SHA above) | **PASS** | IIS recycle time: `2026-09-23 13:44:38.581 +05:30`; SHA match; Web.config not retargeted |
| Expense auth — non-current approver refused | **PASS** | Request **106** / actor `AHS1605` refused; current `AHS1870` mutate PASS (rolled back) |
| Expense / day mutation blocked on ReadyForPayment | **PASS** | Request **107** ExpenseId `2285` + DetailId `3076` |
| Expense / day mutation blocked on Completed | **PASS** | Request **98** ExpenseId `2154` + DetailId `2789` |
| Latest-version parity (Version→Id) after RESUBMIT | **PASS** | Request **107** / 31 latest of 34 rows; multi-ver ActivityIds `47445`,`47447`,`47448`; one actionable/ActivityId |
| Level-aware finalize (RC-022 alignment) — L1-Approved days block Auditor Ready | **PASS** | Request **94**: UnresolvedCount=`21`; MarkReady → *Request not completed.* |
| Stationary ≠ 0 — snapshot + Final reconcile (RC-015B) | **PASS** | Request **106** / Final `6971.06` / Stationary sum `800` (Snap=Live=Header) |
| Excel / UI / Final reconcile | **PASS** | R107 Excel 31 rows FinalSum=`9384.29`=header Stationary=`242`; IIS Express `:53713` live post-recycle; R106 Excel excluded while Pending (export Status filter) — FSOT SQL PASS |

### 8.5 CAB deployment inventory (RC-023 package)

| Order | Object | Action |
| --- | --- | --- |
| 1 | Script-out LIVE/UAT current bodies (rollback) | `usp_UpdateSalesDailyExpenseStatus`, `usp_ProcessApprovalAction`, `usp_MarkRequestReadyForPayment` |
| 2 | `usp_UpdateSalesDailyExpenseStatus.sql` @ `4644baf` | ALTER (auth + Stationary + Version→Id) |
| 3 | `usp_ProcessApprovalAction.sql` @ `4644baf` | ALTER (Version→Id + payment lock) |
| 4 | `usp_MarkRequestReadyForPayment.sql` @ `4644baf` | Optional unless target body differs from RC-014D. |
| 5 | Publish app from `4644baf` | `ClaimsApproval_v2` + helper → recycle IIS |
| 6 | Fill §8.4 UAT evidence | Required before Production money cutover |
| 7 | Production | Same SQL order + DLL; retarget `DbConn` per shop standard (not part of this commit) |

**RC-023 UAT:** **PASS** (2026-09-23).

### 8.6 RC-024 LIVE CAB execution (2026-09-23)

Pack: `pvt-evidence/2026-09-23-rc024-live-cab/` (retained outside Git). Anchors: RC `4644baf` / tag `rc-023-uat-certified` / UAT docs `cc1beb8`.

| Gate | Result | Evidence / notes |
| --- | --- | --- |
| LIVE backup verified | **PASS** | Cutover authorized after ops verification (pre-CAB pack retained) |
| IIS package backup | **PASS** | `2026-09-23 14:08:14 +05:30` — DLL + published Web.config |
| RC-012 schema baseline | **PASS** | `DetailVersion`, `PreviousDetailId`, version index on LIVE |
| LIVE rollback script-out | **PASS** | `rollback/AnmolDristi/` |
| ALTER `usp_UpdateSalesDailyExpenseStatus` | **PASS** | LIVE `modify_date` `2026-09-23 15:50:10.997`; `QuotedIdentOn=1` |
| ALTER `usp_ProcessApprovalAction` | **PASS** | LIVE `modify_date` `2026-09-23 15:50:12.267`; `QuotedIdentOn=1` (created on LIVE) |
| ALTER `usp_MarkRequestReadyForPayment` | **PASS** | LIVE `modify_date` `2026-09-23 15:50:13.437`; `QuotedIdentOn=1` |
| Publish certified DLL + DbConn retarget | **PASS** | SHA `04D0250E…AB09BF`; catalog `AnmolDristi`; VMS `https://apps.anmolindustries.com` preserved; publish `16:02:51.339` |
| IIS App Pool recycle | **PASS** | `2026-09-23 16:02:54.952 +05:30` (UAT / DefaultAppPool / .NET v4.5) |
| Canary (appsuat → LIVE DB, read-only) | **PASS** | Login / Claims / My Claims / ReadyForPayment / #107 view — SAFE TO REFRESH |
| Production refresh | **PASS** | https://apps.anmolindustries.com |
| UAT restore | **PASS** | DeployPackages restored to `AnmolDristiDev` `2026-09-24 02:02:50 +05:30` |

**RC-023 Production Go / No-Go:** **GO** (RC-024, 2026-09-23). Does not reopen or rewrite RC-001–RC-022 certified sections above.

---

## Appendix - conflict notice added by repository review 2026-10-15 (not part of the certification)

The certification content above is unchanged. This appendix records that two of its conclusions are qualified by later commits and records. Both sides are preserved; the decision is **pending**.

| Certification conclusion | Qualifying later evidence |
| --- | --- |
| Section 3.1: "Ready-for-Payment - PASS", anchored on Request 97 with RC-014B/RC-014D | `L2_Auditor_Finalization_Contract.md` records, dated 2026-10-12 and 2026-10-13, that this historical proof "is not the current finalization path", that repository and UAT `usp_ProcessApprovalAction` leave the final Auditor row and the header `Pending`, that LIVE executable text approves that Auditor row and writes `Completed`, and that the release status remains "L2-R19: BLOCKED - OWNER APPROVAL REQUIRED" |
| Section 3.4 SQL deployment inventory (UAT `modify_date` values of 2026-09-15) | `L2_Auditor_Risk_Register.md` (L2-R25) records repository, UAT, and LIVE bodies differing for several procedures; `TA_DA_Deployment_Checklist.md` L837-851 records different Production stamp values |
| Section 3.3 and section 4 leave RC-015B "OPEN" | Nothing in this document, or in the later records checked, closes it |
| This document does not mention the 2026-09-28 checkpoint change or the 2026-10-13 finalization candidate | Commit `e1ddf06`; `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql`; `Database/Deployments/L2R19_UAT/` |

The evidence packs this document cites are unaffected by this appendix. It supersedes nothing and certifies nothing. Conflict identifiers are listed in `docs/INDEX.md` section 8.
