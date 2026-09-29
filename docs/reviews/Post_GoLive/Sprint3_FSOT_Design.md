# Sprint 3 — Financial Source of Truth (FSOT) Design

**Captured:** 2026-09-15  
**Constraint:** design only. No application code, SQL, Git, tags, or deployments.  
**Evidence base:** `docs/reviews/Post_GoLive/Sprint3_Reporting_Reconciliation.md` (Request **97** net ₹ 7,736.44; overall FAIL)  
**Scoped pages:** `ClaimsApproval_v2.aspx.cs`, `MyClaimsView_v2.aspx.cs`, `ApprovedClaimsView.aspx.cs`  
**Scoped SPs:** `usp_SubmitSalesDailyActivities`, `usp_GetApprovalRequest`, `usp_GetApprovalRequestDetails` (repo paths under `Retrieval/` for the two Get* procedures)

This document defines the canonical ownership model so every reporting surface can derive money from one authoritative chain. It does not implement that chain.

---

## 1. Canonical Money Model

### 1.1 Layers

| Layer | Table / object | Role |
| --- | --- | --- |
| L0 Live lines | `SalesDailyExpense`, `SalesDailyActivity` | Mutable operational truth before / during approval |
| L1 Day snapshot | `SalesApprovalRequestDetail` latest row per `ActivityId` | **Atomic financial truth for reporting** |
| L2 Header roll-up | `SalesApprovalRequest` `Approved*` + net payable | **Authoritative request totals** for grids, Ready-for-Payment, payroll |
| L3 Presentation | Modal tiles, Excel columns, list-SP aliases | **Must read L1/L2.** Must not invent a fourth formula |

Latest-version predicate for L1 (design target, one predicate everywhere):

`MAX(ISNULL(DetailVersion,1))` then `MAX(ApprovalRequestDetailId)` per `ActivityId` on the request.

Identity-only `MAX(ApprovalRequestDetailId)` may remain temporarily for C# `LatestDetailWhereForD` only if it is proven identical; FSOT does not treat the two predicates as interchangeable forever.

### 1.2 Candidate objects — authoritative or not

| Object | Candidate | Verdict | Owner (table) | Calculation owner | Consumers | Mutation owner |
| --- | --- | --- | --- | --- | --- | --- |
| `Snapshot_ExpenseTotal` | Day expense bucket | **Authoritative day total** once defined as four-part | `SalesApprovalRequestDetail` | `Snapshot_DA + Hotel + Travel + Stationary` | Modal net, Excel expense/final (via snapshots), submit header `TotalExpenseAmount` | Single writer path after expense change / recalc. Today: **split** — expense SP (3-part) vs `ExecuteRecalculationSQL` (4-part) |
| `Snapshot_StationaryAmount` | Day stationary | **Authoritative component** | `SalesApprovalRequestDetail` | SUM live Stationary not Rejected/deleted | Footers, Excel `[Stationary]`, four-part ExpenseTotal | Recalc today; expense SP **does not** write it |
| `TotalApprovedAmount` | Header computed net | **Authoritative grid/payroll alias only if Stationary is in the formula** | `SalesApprovalRequest` (computed) | Today: Claim+DA+Hotel+Travel (`CK_TotalApprovedAmount_Formula`). Design: Claim+DA+Hotel+Travel+**Stationary** | `usp_GetMyClaims`, `usp_GetApprovedClaimsByUser`, `usp_GetApprovalRequest` | Schema/computed column + check. Components written by roll-up |
| `FinalApprovedAmount` | Header stored net | **Authoritative named net payable** (replace today’s null dead column) | `SalesApprovalRequest` | Persist `SUM(latest Claim + ExpenseTotal)` for days that count toward payable | Grids (preferred bind), payroll feed, MarkReady confirmation | One roll-up writer only. C# must not SET this inline |
| Excel `[Final Approved Amount]` | Export day net | **Presentation of L1**, not a separate truth | Export row = latest Approved snapshot | `Snapshot_CalculatedClaimAmount + Snapshot_ExpenseTotal` (ExpenseTotal already four-part) | My Claims / Approved Claims Excel, payroll file | `usp_Export_Auditor_Final_Excel` SELECT only; C# SUM is display |

**Reject as independent truths:** C# modal `lblSummaryApproved` reconstructed from inline SQL; My Claims “Original” vs L1 “Original”; list-SP aliases that re-sum a subset of `Approved*`; header `OriginalClaimAmount` / `TotalDeductionAmount` as currently maintained (unused / zero).

### 1.3 Canonical day formula

```
Day_TA            = Snapshot_CalculatedClaimAmount
Day_Expenses      = Snapshot_DAAmount + Snapshot_HotelAmount
                    + Snapshot_TravelAmount + Snapshot_StationaryAmount
Day_ExpenseTotal  = Day_Expenses          -- persisted as Snapshot_ExpenseTotal
Day_Net           = Day_TA + Day_ExpenseTotal
```

Payable day set (design): latest row per activity where `DetailStatus` not in (`Rejected`, `Returned`).

```
Request_NetPayable = SUM(Day_Net) over payable latest days
```

That `Request_NetPayable` is what `FinalApprovedAmount` must store and what grids/export/payroll must show under one English name: **Net Payable**.

---

## 2. Formula Ownership Matrix

| Field | Owner SP / writer | Persist where | UI consumer | Export consumer |
| --- | --- | --- | --- | --- |
| KM | Submit copies activity → snapshot; KM SPs update activity + latest snapshot | `Snapshot_TotalDistanceKM`; header `TotalKM` / `ApprovedKM` | Modal KM, queue KM labels | `[KM Claimed]` |
| DA | Expense status + roll-up from live expenses | `Snapshot_DAAmount`; `ApprovedDAAmount` | Modal / footer | `[DA]` |
| Hotel | same | `Snapshot_HotelAmount`; `ApprovedHotelAmount` | Modal / footer | `[Hotel]` |
| Travel | same | `Snapshot_TravelAmount`; `ApprovedTravelAmount` | Modal / footer | `[Travel / OPA]` |
| Stationary | same (must include expense SP) | `Snapshot_StationaryAmount`; **new** `ApprovedStationaryAmount` | Modal / footer (My Claims must add) | `[Stationary]` |
| Expense Total | Derived only from four snapshot components | `Snapshot_ExpenseTotal`; header `TotalExpenseAmount` / `ApprovedExpenseAmount` (redefine to include Stationary) | Modal expense / net | `[Total Expenses (DA+Hotel+Trv+Stat)]` = four-part (= ExpenseTotal) |
| Approved Total (components) | One roll-up after day/expense action | Header `ApprovedKM`, `ApprovedClaimAmount`, `ApprovedDA/Hotel/Travel/Stationary` | Grids via SP | Component columns |
| Net Payable | Same roll-up | Header `FinalApprovedAmount` (= Request_NetPayable). `TotalApprovedAmount` computed to match | All grids, Ready confirm, Approved Claims | Excel TOTAL / payroll SUM of day nets = header |

**Not owned by UI:** net payable, expense total, approved component roll-ups.  
**Allowed in UI:** bind L1 rows for day breakdown; bind L2 header for request totals; reconstruct **deduction breakdown** only as explanatory display if it reconciles to `Original − Net` where Original is also defined once (future UX RC — not FSOT core).

---

## 3. Single Source Strategy

### 3.1 Recommended path

1. **Atomic truth:** latest `SalesApprovalRequestDetail` snapshot components (KM, TA, DA, Hotel, Travel, Stationary).  
2. **Derived day truth:** `Snapshot_ExpenseTotal` = four-part; `Day_Net` = TA + ExpenseTotal.  
3. **Request truth:** one SQL roll-up writes `Approved*` (including Stationary) and sets `FinalApprovedAmount = Request_NetPayable`.  
4. **Consumers:** grids and payroll read `FinalApprovedAmount` (or `TotalApprovedAmount` once the computed formula matches). Modals may SUM latest payable days for tiles **only if** that SUM is proven equal to the header (same predicate, same payable filter). Prefer binding the header for the green “Approved / Net” tile.  
5. **Export:** latest Approved (payable) rows only; day `[Final Approved Amount]` = Day_Net; workbook total equals `FinalApprovedAmount`.

### 3.2 Persist vs compute vs never recompute in C#

| Kind | Fields |
| --- | --- |
| **Persist** | `Snapshot_*` components; `Snapshot_ExpenseTotal`; header `Total*` (submitted); header `ApprovedKM/Claim/DA/Hotel/Travel/Stationary`; header `FinalApprovedAmount` |
| **Compute (DB)** | `ApprovedExpenseAmount` (= DA+Hotel+Travel+Stationary); `TotalApprovedAmount` (= Claim + those expenses) — after CK change — **must equal** `FinalApprovedAmount` |
| **Never recompute in C#** | Net payable, approved total, expense total, export day final, grid approved amount |
| **C# may still** | Call the roll-up SP; filter UI with `KeepLatestPerActivity` until details SP is latest-only; build deduction **explanation** rows that sum to a stored deduction total (once that total is owned in SQL) |

### 3.3 Writer consolidation (design target)

| Today | FSOT target |
| --- | --- |
| `usp_UpdateSalesDailyExpenseStatus` (3-part ExpenseTotal) | Same SP updates **all four** components + four-part ExpenseTotal on latest detail |
| `ExecuteRecalculationSQL` in `ClaimsApproval_v2.aspx.cs` | Move to a named SQL procedure (e.g. `usp_RecalculateApprovalRequestAmounts`) owned by SQL; C# only invokes |
| Submit sets `Total*` | Remains submit-owned for **submitted** totals |
| MarkReady | Remains status-only; must run after roll-up so `FinalApprovedAmount` is already current |
| Five independent net formulas | One roll-up + consumers read |

---

## 4. Consumer Impact Matrix

| Consumer | Today’s money source | Impact class | Notes |
| --- | --- | --- | --- |
| My Claims grid | `usp_GetMyClaims` aliases on `Approved*` (no Stationary) | **Formula alignment** | Bind `FinalApprovedAmount` / aligned `TotalApprovedAmount` |
| My Claims modal | Inline dashboard + footer without Stationary | **Formula alignment** | Net tile ← header; add Stationary footer; retire independent original formula or align to FSOT Original definition |
| Approved Claims grid | `usp_GetApprovedClaimsByUser` four-part sum | **Formula alignment** | Same as My Claims grid |
| Approved Claims modal | Same pattern as L1 modal | **Formula alignment** | Net ← header |
| L1 dashboard (queue) | `TotalClaim + TotalExpense` (submitted) | **UI wording only** (short term) / **Formula alignment** (optional later) | Keep as “Submitted total” label; do not call it Approved |
| L1 dashboard (modal) | Inline Claim+ExpenseTotal | **Formula alignment** | Net ← header after roll-up |
| Auditor dashboard | Same page as L1 | **Formula alignment** | Same |
| ReadyForPayment | Recalc then MarkReady; modal net | **No change** to status SP; **Formula alignment** on money tile | MarkReady must not invent amounts |
| Excel export | All `DetailStatus='Approved'` versions | **Latest-version alignment** + **Formula alignment** | Latest payable only; Stationary in ExpenseTotal |
| Payroll feed | Same export SP / file | **Latest-version alignment** + **Formula alignment** | Must equal `FinalApprovedAmount` |

Classification key: **No change** = no formula work in that RC. **Formula alignment** = same latest rows, wrong/missing math. **Latest-version alignment** = wrong row set. **UI wording only** = label / which header column is shown.

---

## 5. RC-015 Roadmap

Do not implement in this task. Atomic candidates only.

### RC-015A — Canonical totals

| | |
| --- | --- |
| **Goal** | One roll-up writes `Approved*` and `FinalApprovedAmount = Request_NetPayable` from latest payable snapshots |
| **Affected** | New or extended SQL roll-up; `ClaimsApproval_v2.aspx.cs` (`ExecuteRecalculationSQL` call sites); `usp_GetApprovalRequest`; list SPs bind |
| **Risk** | Medium — header amounts change for claims with Stationary or Rejected days |
| **Rollback** | Restore prior roll-up body / DLL; header columns remain |

### RC-015B — Stationary alignment

| | |
| --- | --- |
| **Goal** | Expense SP and roll-up both maintain `Snapshot_StationaryAmount` and four-part `Snapshot_ExpenseTotal`; add `ApprovedStationaryAmount`; extend computed `ApprovedExpenseAmount` / `TotalApprovedAmount` + CK |
| **Affected** | `usp_UpdateSalesDailyExpenseStatus.sql`; roll-up; table computed columns / CK; My Claims footer |
| **Risk** | High — schema check change; existing rows need one-time roll-up |
| **Rollback** | Revert SP bodies; schema CK harder — require scripted reverse |

### RC-015C — Excel latest-version

| | |
| --- | --- |
| **Goal** | `usp_Export_Auditor_Final_Excel` emits one row per activity (latest version) among payable/`Approved` days; day final = Day_Net; workbook SUM = `FinalApprovedAmount` |
| **Affected** | `usp_Export_Auditor_Final_Excel`; My Claims / Approved Claims export callers (SUM only) |
| **Risk** | Medium — payroll file row counts change if duplicates existed |
| **Rollback** | Restore prior export SP |

### RC-015D — Header field cleanup

| | |
| --- | --- |
| **Goal** | Document and enforce: `FinalApprovedAmount` is Net Payable; retire or freeze `OriginalClaimAmount` / `TotalDeductionAmount` until a single Original definition exists; stop exposing null/zero as if live |
| **Affected** | `usp_GetApprovalRequest`; grid SP aliases; optional UI labels |
| **Risk** | Low–medium — consumers of dead columns |
| **Rollback** | Restore SELECT list / labels |

### RC-015E — Payroll reconciliation

| | |
| --- | --- |
| **Goal** | UAT proof: Employee grid = Approved grid = Auditor/Ready net = Excel total = `FinalApprovedAmount` on claims with Stationary ≠ 0 and with version > 1 |
| **Affected** | Evidence pack only (+ any residual consumer gaps found) |
| **Risk** | Low if A–D done; High if run early |
| **Rollback** | n/a (read-only proof) |

**Suggested order:** 015B (stop snapshot drift) → 015A (single header net) → 015C (export) → 015D (cleanup) → 015E (proof).  
015B before 015A avoids locking a net formula that still omits Stationary.

---

## 6. Recommendation

Adopt **latest day snapshots as atomic FSOT**, with **`FinalApprovedAmount` as the request-level Net Payable** written by one SQL roll-up.

Require:

- `Snapshot_ExpenseTotal` always = DA + Hotel + Travel + Stationary.  
- `TotalApprovedAmount` computed formula expanded to include Stationary so it equals `FinalApprovedAmount`.  
- All grids, Ready confirmation, Excel totals, and payroll read that net — never a parallel C# SUM with a different payable filter.  
- Excel and any payroll extract use the same latest-version predicate as RC-012 Forward/Expense.

Do **not** treat Request 97’s matching ₹ 7,736.44 as proof of FSOT. It passed because Stationary was 0 and the extra detail version was not Approved.

Defer Original/Deduction tile unification to a follow-on UX RC after Net Payable is single-sourced (addresses V-01 without blocking payroll integrity).

---

## 7. PASS / FAIL readiness assessment

| Check | Result |
| --- | --- |
| Canonical objects evaluated | **PASS** |
| Formula ownership matrix complete | **PASS** |
| Single source strategy stated | **PASS** |
| Consumer impacts classified | **PASS** |
| RC-015A–E roadmap with risk/rollback | **PASS** |
| FSOT implemented in product | **FAIL** (design only) |
| Safe to start Auditor UX / performance assuming one money formula | **FAIL** until RC-015B + RC-015A (+ 015C for payroll file) land |
| Safe to start RC-015 implementation planning | **PASS** |

**Overall readiness for implementation planning: PASS.**  
**Overall readiness to claim a live Financial Source of Truth: FAIL.**

No repository files outside this document were modified. No SQL was deployed. No Git operations were performed.
