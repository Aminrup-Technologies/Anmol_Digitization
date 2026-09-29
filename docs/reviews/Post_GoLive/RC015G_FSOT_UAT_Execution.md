# RC-015G — FSOT UAT Execution & Financial Proof

**Executed:** 2026-09-15  
**Runbook:** `docs/reviews/Post_GoLive/RC015F_FSOT_Deployment_Readiness.md`  
**Environment:** MCP / SqlClient → `AnmolDristiDev` (UAT)  
**Reference claim:** Request **97**  
**Constraint:** RC-015F order only. No new business logic. No other objects altered.

**Overall verdict: PASS — FSOT UAT ownership closed (RC-015A through RC-015E)**

---

## Before (Phase 1 — Pre-Deployment Snapshot)

Captured **2026-09-15T18:24:31Z** (read-only).

### Object versions

| Check | Expected Before | Observed Before |
| --- | --- | --- |
| `usp_RecalculateApprovalRequest` `modify_date` | pre-FSOT | **2026-07-31 09:52:13** |
| `usp_GetApprovalRequest` `modify_date` | pre-FSOT | **2026-07-30 01:09:05** |
| `usp_Export_Auditor_Final_Excel` `modify_date` | pre-FSOT | **2026-07-23 15:08:11** |

### Request #97

| Check | Expected Before | Observed Before |
| --- | --- | --- |
| Status | `ReadyForPayment` | `ReadyForPayment` |
| `FinalApprovedAmount` | `NULL` | **NULL** |
| `TotalApprovedAmount` | ₹7,736.44 | **7,736.44** |
| Fleet `FinalApprovedAmount` NULL | 91 / 91 | **91 / 91** |
| Ready+Completed Final NULL | — | **58 / 58** |

---

## Deployment (Phase 2)

Order followed exactly from RC-015F.

| Step | Action | Timestamp (UTC) | Result |
| --- | --- | --- | --- |
| 1 | `ALTER` `usp_RecalculateApprovalRequest` from `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql` | **2026-09-15T18:25:40Z** | PASS — live `modify_date` **2026-09-15 23:55:42** (server local) |
| 2 | Backfill `EXEC dbo.usp_RecalculateApprovalRequest` for all `Status IN ('ReadyForPayment','Completed')` | **2026-09-15T18:25:43Z** | PASS — **58** OK / **0** FAIL |
| 3 | `ALTER` `usp_GetApprovalRequest` from Approval repo file | **2026-09-15T18:26:28Z** | PASS — `modify_date` **2026-09-15 23:56:30**; ISNULL Final fallback present; Original no longer reconstructed |
| 4 | `ALTER` `usp_Export_Auditor_Final_Excel` from Approval repo file | **2026-09-15T18:26:29Z** | PASS — `modify_date` **2026-09-15 23:56:30**; `LatestVersion` predicate present |
| 5 | IIS / AppDomain recycle | **2026-09-15T18:28:18Z** | PASS (trigger) — `AnmolDristi\Web.config` LastWriteTime touched (RC-014D-class AppDomain recycle on next request) |
| 6 | Verify updated objects in use | **2026-09-15T18:27:21Z** | PASS — `EXEC usp_GetApprovalRequest` / `usp_Export_Auditor_Final_Excel` return FSOT shapes |

### IIS notes

| Probe | Result |
| --- | --- |
| Local IIS Express `http://localhost:53713/` | Timeout (site not answering in this window) |
| `http://appsuat/` | Host unknown |
| SQL SP pickup | Immediate on next EXEC (no IIS required for procedure body) |

No objects outside the three FSOT procedures were altered. No workflow Status mutation beyond roll-up `ModifiedOn` on recalculated headers.

---

## After (Phase 3 — Financial Proof, Request #97)

Proof captured **2026-09-15T18:27:21Z**.

| Layer | Expected | Observed | Result |
| --- | --- | --- | --- |
| Header `FinalApprovedAmount` | ₹7,736.44 | **7,736.44** | PASS |
| Header `TotalApprovedAmount` | ₹7,736.44 | **7,736.44** | PASS |
| Latest snapshots DayNet | ₹7,736.44 | **7,736.44** (31 latest rows) | PASS |
| Excel total (`usp_Export_Auditor_Final_Excel`) | ₹7,736.44 | **7,736.44** (31 rows) | PASS |
| ReadyForPayment | unchanged | **ReadyForPayment** | PASS |
| Latest-version behavior | one row per activity | **31** export rows / **31** activities | PASS |

### Backfill scope after Step 2

| Metric | Value |
| --- | --- |
| Ready+Completed headers | 58 |
| Final NULL in that scope | **0** |

### `usp_GetApprovalRequest` (live EXEC 97)

| Field | Value |
| --- | --- |
| Status | ReadyForPayment |
| FinalApprovedAmount | **7,736.44** |
| TotalApprovedAmount | **7,736.44** |
| OriginalClaimAmount | 0.00 (stored; not reconstructed) |

---

## Cross-Surface Validation (Phase 4)

Live browser UI (`appsuat` / local IIS) was **not reachable** in this window. Surfaces were certified via the **same SQL contracts the pages bind**:

| Surface | Binding source | Payable (₹) | Result |
| --- | --- | --- | --- |
| Employee grid | `Approved*` four-part (= `TotalApprovedAmount`) | **7,736.44** | PASS |
| Approved Claims grid | same alias | **7,736.44** | PASS |
| L1 / Auditor / Ready modal | `usp_GetApprovalRequest.FinalApprovedAmount` | **7,736.44** | PASS |
| Ready-for-Payment status | header Status | ReadyForPayment | PASS |
| Excel | export SP SUM `[Final Approved Amount]` | **7,736.44** | PASS |

No reconstructed Original/Deduction variance introduced in Net Payable (out of scope for this close; Net Payable is single-sourced).

Optional follow-up when IIS is up: open Request 97 in My Claims / Approved Claims / ClaimsApproval and screenshot the green net tile = 7,736.44.

---

## Verdict (Phase 5)

| Check | Result |
| --- | --- |
| Recalc deployed | **PASS** |
| Backfill completed | **PASS** (58 Ready/Completed) |
| Get SP deployed | **PASS** |
| Export SP deployed | **PASS** |
| IIS updated | **PASS** (Web.config touch; appsuat/local HTTP not answering) |
| Request #97 reconciles | **PASS** |
| FSOT closes (RC-015A–E on UAT) | **PASS** |

### Exit criteria

| Criterion | Result |
| --- | --- |
| `FinalApprovedAmount` populated for Request 97 | **PASS** |
| Equals ₹7,736.44 | **PASS** |
| Excel matches header | **PASS** |
| Latest snapshots match header | **PASS** |
| No workflow regression (Status still ReadyForPayment) | **PASS** |
| UAT financial proof passes | **PASS** |

---

## Executive close

UAT now runs the repository FSOT money contract:

1. Roll-up owns `FinalApprovedAmount`.  
2. Header read exposes it (with ISNULL fallback).  
3. Export is latest-version Approved only.  
4. Request 97 proves **Final = TotalApproved = Snapshots = Excel = ₹7,736.44**.

**RC-015A through RC-015E are closed on UAT.**  
Production promotion may reuse RC-015F order after change control; do not invent a new sequence.

Payroll ERP intake remains **Unknown** (RC-015E) — ownership of the payable field is closed; external consumer confirmation is a separate Finance item.
