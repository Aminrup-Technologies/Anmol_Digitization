# Sprint 3 — Reporting Reconciliation Audit

**Captured:** 2026-09-15  
**Constraint:** read-only. No application code, SQL deploy, Git, tags, or deployments.  
**Anchor claim:** Request **97** (Ajay Kumar Sahu / `AHS1454`, August 2026, header `ReadyForPayment`)  
**SQL:** parameterized `SELECT` on MCP `anmol_uat` → `AnmolDristiDev`  
**Baseline:** `docs/reviews/Post_GoLive/Post_GoLive_Baseline.md` P1 Reporting reconciliation

Repo retrieval procedures live under `Database/Stored Procedures/Retrieval/` (`usp_GetApprovalRequest`, `usp_GetApprovalRequestDetails`). The kickoff listed them under Approval; bodies below are those files plus live UAT definitions.

**Verdict: FAIL** as a product-wide financial integrity baseline. Request 97 **net payable** agrees at **₹ 7,736.44** across latest snapshots, header `TotalApprovedAmount`, L1/Auditor/Ready modal net, Approved Claims grid, and Excel `Final Approved Amount`. Original/deduction formulas, Stationary ownership, unused header columns, and a legacy export predicate do not reconcile by construction.

---

## 1. Reporting Inventory

L1 dashboard and Auditor dashboard are the same page (`ClaimsApproval_v2`). Role overlay does not change the money SQL.

| Surface | Owning page | Source | Owning SP | C# aggregation | SQL aggregation | Latest-version |
| --- | --- | --- | --- | --- | --- | --- |
| My Claims grid | `MyClaimsView_v2.aspx` | Header stored + SP aliases | `usp_GetMyClaims` | none (bind) | `TotalRequestedAmount` = `TotalClaimAmount`+`TotalExpenseAmount`; `TotalApprovedAmount` = `ApprovedClaim`+`ApprovedDA`+`ApprovedHotel`+`ApprovedTravel` | **Legacy** (header; no detail predicate) |
| My Claims modal | `MyClaimsView_v2.aspx.cs` `BindDetailsGrid` | Detail snapshots | `usp_GetApprovalRequestDetails` | footer `_totalTAClaim` / `_totalDA` / `_totalHotel` / `_totalTravel` / `_totalSnapshot` (no Stationary field) | dashboard: original = all latest `Claim+ExpenseTotal`; approved = latest `DetailStatus <> Rejected`; KM deduct `× 3.75` | **Mixed** — UI `KeepLatestPerActivity` + SQL `LatestDetailWhereForD` |
| Approved Claims grid | `ApprovedClaimsView.aspx` | Header computed / SP alias | `usp_GetApprovedClaimsByUser` | none | same `TotalApprovedAmount` four-part sum (no Stationary) | **Legacy** (header) |
| Approved Claims modal | `ApprovedClaimsView.aspx.cs` `BindDetailsGrid` | Detail snapshots | `usp_GetApprovalRequestDetails` | footer includes `_totalStationary` | approved = latest not Rejected/Returned; original = approved + deducted; KM deduct uses snapshot ₹/KM | **Mixed** |
| L1 dashboard (queue) | `ClaimsApproval_v2.aspx` `gvRequests` | Header submitted totals | `usp_GetPendingApprovalsForUser_Filters` | markup `TotalClaimAmount + TotalExpenseAmount` | none on details | **Legacy** (header; Pending-only queue) |
| L1 dashboard (modal) | `ClaimsApproval_v2.aspx.cs` `BindDetailsGrid` | Detail snapshots | `usp_GetApprovalRequestDetails` + `usp_GetApprovalRequest` (header, not the ₹ tiles) | footer includes Stationary | same as Approved Claims modal | **Mixed** |
| Auditor dashboard | same as L1 | same | same | same | same | **Mixed** |
| Ready-for-Payment | `ClaimsApproval_v2` modal then `ApprovedClaimsView` | Status only on MarkReady; money from recalc + modal | `usp_MarkRequestReadyForPayment` writes **Status**, not amounts; recalc `ExecuteRecalculationSQL` first | modal labels `lblSummary*` | recalc writes `ApprovedKM` / `ApprovedClaim` / `ApprovedDA` / `ApprovedHotel` / `ApprovedTravel` on latest non-Rejected/Returned | Money: **SQL-filtered** (recalc MAX Id). Status: n/a |
| Excel exports | My Claims + Approved Claims `ExportClaimToExcel` | Day rows | `usp_Export_Auditor_Final_Excel` (`modify_date` 2026-07-23) | C# `Compute(SUM(...))` on SP columns including Stationary and `Final Approved Amount` | `DetailStatus = 'Approved'` on **all** versions; `Final Approved Amount` = `Snapshot_ExpenseTotal + Snapshot_CalculatedClaimAmount` | **Legacy** |

Writers that feed those surfaces (not themselves displays):

| Writer | What it rolls up |
| --- | --- |
| `usp_SubmitSalesDailyActivities` | Header `TotalKM` / `TotalClaimAmount` / `TotalDA` / `TotalHotel` / `TotalTravel` / `TotalStationary` / `TotalExpenseAmount` from latest snapshot `RN=1` |
| `usp_UpdateSalesDailyExpenseStatus` | Latest snapshot DA/Hotel/Travel; `Snapshot_ExpenseTotal` = DA+Hotel+Travel (**no Stationary**); header `TotalClaimAmount` / `TotalExpenseAmount` |
| `ExecuteRecalculationSQL` | Latest MAX(Id) snapshot DA/Hotel/Travel/**Stationary**; `Snapshot_ExpenseTotal` = four-part; header `Approved*` except Stationary |
| `usp_ForwardApprovalRequest` | Routing only. No money. |
| `usp_MarkRequestReadyForPayment` | `Status = ReadyForPayment` only. No money. |

`usp_GetApprovalRequest` returns stored `Total*` / `Approved*` / `TotalApprovedAmount` / `FinalApprovedAmount`. ClaimsApproval v2 does not bind those columns to the ₹ tiles; the tiles use the inline dashboard SQL.

---

## 2. Financial Field Matrix

Canonical day snapshot lives on `SalesApprovalRequestDetail`. Live expense lines live on `SalesDailyExpense`. Header roll-ups live on `SalesApprovalRequest`.

| Field | Definition | Originating table | Snapshot column | Roll-up column | Exported column |
| --- | --- | --- | --- | --- | --- |
| KM Claimed | Distance at approval time | `SalesDailyActivity.TotalDistanceKM` copied at submit | `Snapshot_TotalDistanceKM` | `TotalKM` (submitted); `ApprovedKM` (recalc, latest non-Rejected/Returned) | `[KM Claimed]` |
| TA Bill | Calculated TA/KM claim | `SalesDailyActivity.CalculatedClaimAmount` | `Snapshot_CalculatedClaimAmount` | `TotalClaimAmount`; `ApprovedClaimAmount` | `[TA Bill]` |
| DA | Daily allowance receipts not Rejected | `SalesDailyExpense` `ExpenseType='DA'` | `Snapshot_DAAmount` | `TotalDAAmount`; `ApprovedDAAmount` | `[DA]` |
| Hotel | Hotel receipts not Rejected | `SalesDailyExpense` `ExpenseType='Hotel'` | `Snapshot_HotelAmount` | `TotalHotelAmount`; `ApprovedHotelAmount` | `[Hotel]` |
| Travel/OPA | Travel receipts not Rejected | `SalesDailyExpense` `ExpenseType='Travel'` | `Snapshot_TravelAmount` | `TotalTravelAmount`; `ApprovedTravelAmount` | `[Travel / OPA]` |
| Stationary | Stationary receipts not Rejected | `SalesDailyExpense` `ExpenseType='Stationary'` | `Snapshot_StationaryAmount` | `TotalStationaryAmount` (submit only). **No `ApprovedStationaryAmount`.** Recalc writes the snapshot; expense SP does not. Computed `ApprovedExpenseAmount` / `TotalApprovedAmount` **omit** it. | `[Stationary]` |
| Total Expenses | Day expense bucket | derived | `Snapshot_ExpenseTotal` | `TotalExpenseAmount`; computed `ApprovedExpenseAmount` = DA+Hotel+Travel | `[Total Expenses (DA+Hotel+Trv+Stat)]` = four-part **or** `Snapshot_ExpenseTotal` depending on writer |
| Final Approved Amount | Net payable | derived | `Snapshot_CalculatedClaimAmount + Snapshot_ExpenseTotal` on the consumer | computed `TotalApprovedAmount` = Claim+DA+Hotel+Travel (**no Stationary**). Column `FinalApprovedAmount` exists, is **not computed**, and is **null** on Request 97. | `[Final Approved Amount]` = `ExpenseTotal + TA Bill` |

`ApprovedExpenseAmount` computed definition: `ApprovedDA + ApprovedHotel + ApprovedTravel`.  
`TotalApprovedAmount` computed definition: `ApprovedClaim + ApprovedDA + ApprovedHotel + ApprovedTravel`.  
Check `CK_TotalApprovedAmount_Formula` enforces that four-part identity.

Unused / stale header money: `OriginalClaimAmount` (0.00 on Request 97), `FinalApprovedAmount` (null), `TotalDeductionAmount` (null). Modal Original/Deduction are reconstructed in C#, not stored.

---

## 3. Latest-Version Compliance Matrix

| Consumer | `KeepLatestPerActivity` | `LatestDetailWhereForD` | `LatestDetailJoinForD` | `MAX(DetailVersion)` | `MAX(ApprovalRequestDetailId)` | Class |
| --- | --- | --- | --- | --- | --- | --- |
| My Claims grid | — | — | — | — | — | **Legacy** |
| My Claims modal grid | Yes | — | — | via helper (version then Id) | helper tie-break | **UI-filtered** |
| My Claims modal dashboard | — | Yes | Yes (rejected expenses, KM) | — | Yes (WHERE) | **SQL-filtered** |
| Approved Claims grid | — | — | — | — | — | **Legacy** |
| Approved Claims modal | Yes | Yes | Yes | helper | Yes | **Mixed** |
| L1 / Auditor queue | — | — | — | — | — | **Legacy** |
| L1 / Auditor / Ready modal | Yes | Yes | Yes | helper | Yes | **Mixed** |
| `ExecuteRecalculationSQL` | — | Yes (Approved* roll-up) | — | — | Yes (snapshot UPDATE) | **SQL-filtered** (Id-only) |
| `usp_SubmitSalesDailyActivities` | — | — | — | Yes (`LatestDetailSnapshot` / `RN=1`) | Yes (order) | **SQL-filtered** |
| `usp_UpdateSalesDailyExpenseStatus` | — | — | — | Yes then | Yes | **SQL-filtered** |
| `usp_ForwardApprovalRequest` | — | — | — | Yes then | Yes | **SQL-filtered** (completeness, not money) |
| `usp_MarkRequestReadyForPayment` | — | — | — | — | — | n/a (status) |
| `usp_GetApprovalRequestDetails` | — | — | — | — | — | **Legacy** (all versions; callers filter) |
| `usp_GetApprovalRequest` | — | — | — | — | — | **Legacy** (header) |
| Excel `usp_Export_Auditor_Final_Excel` | — | — | — | — | — | **Legacy** (`DetailStatus='Approved'` only) |

On Request 97, `MAX(Id)` latest and `MAX(DetailVersion)` then `MAX(Id)` latest returned the **same** money. Unfiltered all-version sum did not (`₹ 9,305.45`).

---

## 4. Cross-Surface Reconciliation (Request 97)

31 activities. 32 detail rows (one `DetailVersion > 1`; superseded row is not `Approved`). Stationary **₹ 0.00**. Live non-rejected expenses match latest snapshots (DA 575 / Hotel 450 / Travel 450). Status `ReadyForPayment`.

| Surface | KM | TA Bill | DA | Hotel | Travel | Stationary | Expenses | Final / net |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Latest snapshots (`MAX(Id)` and version-then-Id) | 1,669.72 | 6,261.44 | 575.00 | 450.00 | 450.00 | 0.00 | 1,475.00 | **7,736.44** |
| Header `Total*` (submit / expense-SP maintained) | 1,669.72 | 6,261.44 | 575.00 | 450.00 | 450.00 | 0.00 | 1,475.00 | 7,736.44 (Claim+Expense) |
| Header `Approved*` / computed `TotalApprovedAmount` | 1,669.72 | 6,261.44 | 575.00 | 450.00 | 450.00 | *(none)* | 1,475.00 (`ApprovedExpenseAmount`) | **7,736.44** |
| L1 queue formula `TotalClaim+TotalExpense` (would show if still Pending) | — | — | — | — | — | — | — | **7,736.44** |
| L1 / Auditor / Ready modal **net** (`lblSummaryApproved`) | 1,669.72 | in Claim+ExpenseTotal | in ExpenseTotal | in ExpenseTotal | in ExpenseTotal | in ExpenseTotal after recalc | 1,475.00 | **7,736.44** |
| L1 / Auditor / Ready modal **original / deduction** | reconstructed | — | — | — | — | — | — | Original **8,017.95** / Deduction **281.51** (RC-014D UI; original = net + KM/reject reconstruction) |
| My Claims grid `TotalRequestedAmount` / `TotalApprovedAmount` | — | via header | via header | via header | via header | omitted from approved alias | — | both **7,736.44** on this claim |
| My Claims modal original (all latest, no KM reconstruct) | 1,669.72 | — | — | — | — | — | — | original **7,736.44** (all 31 latest are Approved) |
| Approved Claims grid | — | — | — | — | — | omitted | — | **7,736.44** (live UI) |
| Excel (31 `Approved` rows) | 1,669.72 | 6,261.44 | 575.00 | 450.00 | 450.00 | 0.00 | 1,475.00 (four-part) | **7,736.44** |
| All detail versions unfiltered | — | 6,580.45 | — | — | — | 0.00 | 2,725.00 | **9,305.45** |
| Header `FinalApprovedAmount` / `OriginalClaimAmount` / `TotalDeductionAmount` | — | — | — | — | — | — | — | null / **0.00** / null |

Employee → L1 → Auditor → Ready-for-Payment → Export **net payable** on Request 97: **7,736.44 = 7,736.44**.

MarkReady did not change any money column. Recalc before MarkReady is why `Approved*` matches latest snapshots.

---

## 5. Variance Table

| ID | Surfaces | What differs | Request 97 | Structural? |
| --- | --- | --- | --- | --- |
| V-01 | Employee modal Original vs L1/Auditor/Ready Original | My Claims original = SUM latest Claim+ExpenseTotal. L1/Auditor original = approved + reconstructed deductions (rejected/returned days, rejected receipts, KM history × snapshot rate). My Claims KM deduct uses **fixed 3.75**; L1/Auditor uses **Claim/KM** (fallback 3.75). | Employee original 7,736.44 vs L1 original 8,017.95 | Yes |
| V-02 | Grid approved vs modal net vs computed header | Grids use `ApprovedClaim+DA+Hotel+Travel` (no Stationary). Modals use `Claim + Snapshot_ExpenseTotal` (Stationary only if last writer put it in ExpenseTotal). | Hidden (Stationary 0.00) | Yes |
| V-03 | Expense SP vs Recalc `Snapshot_ExpenseTotal` | Expense SP = DA+Hotel+Travel. Recalc = DA+Hotel+Travel+Stationary. Last writer wins. | Recalc last (MarkReady); both 1,475.00 | Yes (L2-R06) |
| V-04 | Excel vs latest-version | Export selects every `DetailStatus='Approved'` row. No `KeepLatest` / MAX version. Duplicate if a superseded snapshot stayed Approved. | 31 Approved = 31 activities; 7,736.44 | Yes; masked here because the extra version is not Approved |
| V-05 | Excel `Final Approved Amount` vs Excel `Total Expenses` | Final = `ExpenseTotal + TA`. Total Expenses = four-part sum. If ExpenseTotal omits Stationary, Final understates vs four-part+TA. If ExpenseTotal includes Stationary, Final matches four-part+TA. | Match (Stationary 0 and ExpenseTotal = four-part) | Yes |
| V-06 | Unfiltered details SP | `usp_GetApprovalRequestDetails` returns 32 rows; money would be 9,305.45 without C# filter | UI-filtered to 7,736.44 | Yes |
| V-07 | Header `FinalApprovedAmount` / `OriginalClaimAmount` / `TotalDeductionAmount` | Not maintained. `usp_GetApprovalRequest` still returns them. | 0 / null / null vs modal 8,017.95 / 281.51 / 7,736.44 | Yes |
| V-08 | Queue money vs approved money | Queue shows submitted `TotalClaim+TotalExpense`, not `TotalApprovedAmount`. After expense SP / submit rewrite they can converge. | Converged at 7,736.44 | Yes (timing) |
| V-09 | Recalc header vs computed `TotalApprovedAmount` | Recalc does not write Stationary into any Approved* column. Computed total cannot include it. | n/a at 0.00 | Yes |
| V-10 | Duplicate aggregation | Same net recomputed in recalc, dashboard SQL, grid SP aliases, export SUM, and C# footer | Same answer on 97 | Yes |

---

## 6. Deferred Risks

Candidates only. No fixes in this audit.

| ID | Candidate | Why it matters |
| --- | --- | --- |
| RR-01 | Duplicate aggregation | Five independent formulas for “net payable”. They agreed on Request 97; they will diverge on Stationary or dual Approved versions. |
| RR-02 | Legacy export predicate | `usp_Export_Auditor_Final_Excel` (2026-07-23) has no latest-version filter. Payroll can double-count if an old Approved snapshot remains. |
| RR-03 | Inconsistent export naming | `[Final Approved Amount]` is snapshot Claim+ExpenseTotal, not header `FinalApprovedAmount` (null) and not computed `TotalApprovedAmount` when Stationary ≠ 0. |
| RR-04 | Snapshot drift (Stationary) | Expense SP vs Recalc (L2-R06). Modal net follows `Snapshot_ExpenseTotal`; grid approved never includes Stationary. |
| RR-05 | Unnecessary recomputation | `ExecuteRecalculationSQL` rewrites latest snapshots on every approve/forward/ready. Header `Total*` and `Approved*` can be seconds apart from different writers. |
| RR-06 | Legacy aliases | `TotalRequestedAmount` / `TotalApprovedAmount` in list SPs vs modal `lblSummary*`. Same English words, different formulas (V-01, V-02). |
| RR-07 | Dead header money | `OriginalClaimAmount`, `FinalApprovedAmount`, `TotalDeductionAmount` are not the operational dashboard. |
| RR-08 | My Claims footer omits Stationary | `_totalStationary` exists on Approved/ClaimsApproval row-bind, not on My Claims. |
| RR-09 | All-version retrieval | Future caller of `usp_GetApprovalRequestDetails` without `KeepLatestPerActivity` reports ₹ 9,305.45 on this claim. |

---

## 7. PASS / FAIL

| Check | Result |
| --- | --- |
| Every reporting surface mapped | **PASS** |
| Every listed monetary field has an ownership chain | **PASS** (Stationary’s approved roll-up is documented as **missing**) |
| Latest-version classified per consumer | **PASS** |
| Employee → L1 → Auditor → Ready-for-Payment → Export **net** on Request 97 | **PASS** at ₹ **7,736.44** |
| Employee → L1 **original** on Request 97 | **FAIL** (7,736.44 vs 8,017.95) |
| Stationary / `FinalApprovedAmount` / export latest-version by construction | **FAIL** |
| No repo / SQL / Git changes | **PASS** |

**Overall: FAIL.**

Request 97 is a valid net-payable proof after RC-012 + recalc-before-MarkReady, with Stationary zero and one Approved row per day. It is **not** a proof that every surface derives the same latest-version financial total. Sprint 3 must treat reporting formulas as unfixed before UX or performance work that assumes a single source of truth.
