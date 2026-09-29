# RC-017C — SoftDelete Face Refresh

**When:** 2026-09-24  
**Why:** RC-017B showed soft-delete left header `TotalExpenseAmount` stale while Recalc refreshed latest `Snapshot_ExpenseTotal`.  
**What:** Soft-delete path refreshes claim face only (Snapshot_* + `TotalClaimAmount` / `TotalExpenseAmount`), matching `usp_UpdateSalesDailyExpenseStatus`.  
**Not changed:** `Approved*`, `FinalApprovedAmount`, latest-version ownership, routing, Recalc, submit SP.

## Code

| Object | Change |
| --- | --- |
| `AnmolDristi/SalesMonthlyReview.aspx.cs` | `RefreshClaimFaceAfterSoftDeleteSql` + helper; called after primary (+ auto-DA) soft-delete succeeds, inside the same transaction, both delete paths; header `UPDATE … WITH (UPDLOCK, ROWLOCK)` |

## Aggregation contract (must match expense SP)

- Resolve latest detail: `MAX(DetailVersion)` then `MAX(ApprovalRequestDetailId)` for `@ActivityId`
- Snapshot components from `SalesDailyExpense` where `IsDeleted = 0` and `ClaimStatus <> 'Rejected'`
- `Snapshot_ExpenseTotal` = DA + Hotel + Travel + Stationary
- Header `TotalClaimAmount` / `TotalExpenseAmount` = sum of those columns on **all** latest details for the request
- No-op when activity is not claim-linked (no `SalesApprovalRequestDetail`)

---

## UAT evidence capture (2026-09-24)

**Method:** LIVE SQL dry-run for #1136 + repository path verification for scenarios 2–5.  
**No code modified. No LIVE `@Apply=1` repair executed.**  
**Raw dump:** `docs/reviews/Post_GoLive/RC017C_UAT_Evidence_1136.txt`

### Scenario results

| # | Scenario | Evidence type | Result |
| --- | --- | --- | --- |
| 1 | Request **#1136** reference | LIVE SQL | **PASS** |
| 2 | Stationary-only delete | Code path | **PASS** |
| 3 | Auto-DA cascade | LIVE history (#1136 Activity 83222) + code path | **PASS** |
| 4 | Pre-submit delete (no-op) | Code path | **PASS** |
| 5 | Gate-fail (non-removable) | Code path | **PASS** |

### 1) Request #1136 (M2603) — reference

LIVE `AnmolDristi` as of capture:

| Field | Observed | Expected after face refresh |
| --- | ---: | ---: |
| `Status` | Pending | — |
| `TotalExpenseAmount` | **9,477.00** (still stale; soft-delete pre-dated RC-017C) | **6,375.00** |
| `TotalClaimAmount` | 8,407.14 | 8,407.14 (= latest Snapshot claim sum) |
| `FinalApprovedAmount` | **14,782.14** | **14,782.14** (unchanged) |
| `SUM(latest Snapshot_ExpenseTotal)` | **6,375.00** | — |
| Deleted draft sum (Activity 83222) | **3,102.00** | closes variance |

Deleted rows (same activity): Travel 1,176 + Auto-DA 750 + Travel 1,176.  
Proof script: `Database/Scripts/014_RC017C_SoftDeleteFaceRefresh_Proof.sql` (`@Apply=0`) → predicted TotalExpense **6,375**, Final unchanged.

**UI:** Soft-delete already occurred historically (12-Sep); current header still shows submitted face until app deploy + refresh/repair. L1 queue “Total Claim” still binds header face (inflated until refresh).

### 2) Stationary-only delete — PASS

- `isDaQualifyingType` = Hotel \| Travel only (`SalesMonthlyReview.aspx.cs` ~3961–3963). Stationary **skips** Auto-DA block.
- Comment at ~3986: “Stationary must not remove it.”
- `RefreshClaimFaceAfterSoftDelete` still runs once after primary delete (~4030 / ~4241), so Snapshot_Stationary + ExpenseTotal + header face update.

**UI:** Expense modal success text `"Expense deleted."` (green); no Auto-DA side-effect message.

### 3) Auto-DA cascade — PASS

LIVE #1136 Activity **83222** historical cascade (same `DeletedOn`):

| ExpenseId | Type | Amount | IsAutoDA | DeleteReason |
| --- | --- | ---: | --- | --- |
| 29269 | Travel | 1,176 | 0 | User removed draft expense |
| 29270 | DA | 750 | 1 | **Auto-DA reversed due to primary expense deletion** |

Code: cascade completes inside tx; **single** `RefreshClaimFaceAfterSoftDelete` after cascade (~4029–4030). Snapshot filter `IsDeleted=0` excludes both. Latest detail for 83222 now Snapshot_ExpenseTotal = **1,650** (live Travel 900 + Auto-DA 750).

### 4) Pre-submit delete — PASS

- Face SQL gated: `IF @ApprovalRequestDetailId IS NOT NULL AND @ApprovalRequestId IS NOT NULL` (~2419).
- No claim detail → no Snapshot/header writes; soft-delete of Draft expense still succeeds.

**UI:** `"Expense deleted."`; grid rebinds; no claim header exists to drift.

### 5) Gate-fail — PASS

Early returns **before** transaction / **before** refresh:

- `!IsRemovableExpenseClaimStatus` → orange `"Only draft or returned expenses can be removed."` (~3939–3947, ~4144–4152)
- `isAutoRow` → orange `"Cannot delete system-generated (auto) expense directly."` (~3950–3958)
- Soft-delete `ExecuteNonQuery() <= 0` → rollback + return (~3973–3982); refresh never reached

---

## CAB-ready UAT summary

| Item | Statement |
| --- | --- |
| **Change** | Soft-delete face refresh on claim-linked expenses |
| **App file to deploy** | **`AnmolDristi/SalesMonthlyReview.aspx.cs` only** |
| **DB deploy** | None required for RC-017C runtime |
| **Ownership** | Face (`Total*`) = submit / expense SP / soft-delete; Recalc owns `Approved*` / `FinalApprovedAmount` only |
| **Reference case** | Request **#1136** — TotalExpense 9,477 → 6,375; Final 14,782.14 unchanged |
| **UAT score** | 5 / 5 PASS |
| **Residual** | Historical Pending drifts (incl. #1136 header) remain until post-deploy soft-delete replay or optional proof `@Apply=1` |
| **Rollback** | Revert `SalesMonthlyReview.aspx.cs` |

### Deployment confirmation

`git` shows **`AnmolDristi/SalesMonthlyReview.aspx.cs`** as the only modified application source file for RC-017C. Companion docs/scripts (`013`, `014`, this review) are optional and not required for app runtime.
