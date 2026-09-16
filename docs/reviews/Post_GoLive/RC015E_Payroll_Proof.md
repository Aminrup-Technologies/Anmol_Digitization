# RC-015E — Payroll Proof (Financial Governance Certificate)

**Captured:** 2026-09-15  
**Constraint:** read-only proof. No application code, SQL deploy, Git, tags, or deployments.  
**Anchor claim:** Request **97** (Ajay Kumar Sahu / `AHS1454`, August 2026)  
**SQL:** MCP `anmol_uat` → `AnmolDristiDev` (procedure definitions + `sample_data`; parameterized SELECT for Request 97 because MCP `execute_query` is disabled)  
**Repo contract sources:** `Sprint3_FSOT_Design.md`, `Sprint3_Reporting_Reconciliation.md`, scoped Approval SPs, `MyClaimsView_v2.aspx.cs`, `ApprovedClaimsView.aspx.cs`

**Overall verdict: FAIL**

Request 97 **operational net payable** reconciles at **₹ 7,736.44** across latest payable snapshots, computed `TotalApprovedAmount`, Employee/Approved grid aliases, and Excel SUM. Canonical `FinalApprovedAmount` is **NULL** on Request 97 and on **all 91** UAT headers. Live UAT does **not** yet run the RC-015A / RC-015C procedure bodies present in the repository. Actual payroll ERP import consumer is **Unknown**.

---

## Canonical contract under test

| Layer | Source |
| --- | --- |
| Day expense | `Snapshot_ExpenseTotal` |
| Day payable | `Snapshot_CalculatedClaimAmount + Snapshot_ExpenseTotal` |
| Request payable | `FinalApprovedAmount` |
| Ready-for-Payment | `SalesApprovalRequest.Status = 'ReadyForPayment'` |
| Export | Latest Approved snapshots |
| Payroll | Must consume the same request payable |

No formulas were changed in this RC.

---

## 1. Ready-for-Payment Inventory

### 1.1 Scoped matrix (required objects)

| Object | Reads | Writes |
| --- | --- | --- |
| `usp_MarkRequestReadyForPayment` | Approver role=`Auditor`; header `Status='Completed'` | Header `Status='ReadyForPayment'`; history `Completed → ReadyForPayment`. **No money columns.** |
| `usp_Export_Auditor_Final_Excel` | Header status ∈ (`Completed`,`ReadyForPayment`,`Processed`); day rows with `DetailStatus='Approved'`; snapshot money → `[Final Approved Amount]` | None (SELECT only) |
| `usp_GetApprovedClaimsByUser` | Approver rows with status ∈ (`Approved`,`Completed`,`Processed`,`ReadyForPayment`); header `Approved*` | None. Aliases `TotalApprovedAmount` = Claim+DA+Hotel+Travel. Displays Ready for Payment label. **Does not read `FinalApprovedAmount`.** |
| `usp_GetMyClaims` | Requester claims; optional `@FilterStatus` including `ReadyForPayment`; header `Approved*` | None. Same four-part `TotalApprovedAmount` alias. **Does not read `FinalApprovedAmount`.** |

### 1.2 Adjacent consumers (inventory completeness)

| Object | Role vs ReadyForPayment / payable |
| --- | --- |
| `ClaimsApproval_v2.aspx.cs` | Calls `usp_RecalculateApprovalRequest` then `usp_MarkRequestReadyForPayment`. Modal net binds `usp_GetApprovalRequest.FinalApprovedAmount` (RC-015A C#). Counters / badge read status string only. |
| `ApprovedClaimsView.aspx.cs` | Grid via `usp_GetApprovedClaimsByUser`; Excel via `usp_Export_Auditor_Final_Excel`; modal net = latest Claim+`Snapshot_ExpenseTotal` (not Rejected/Returned). |
| `MyClaimsView_v2.aspx.cs` | Grid via `usp_GetMyClaims`; Excel via export SP; modal net = latest Claim+`Snapshot_ExpenseTotal` (not Rejected). |
| `usp_GetApprovalRequest` | Returns stored / aliased header money including `FinalApprovedAmount` / `TotalApprovedAmount`. |
| `usp_RecalculateApprovalRequest` | Intended writer of `Approved*` + `FinalApprovedAmount` (repo FSOT body). |
| `usp_ReopenApprovalRequestByAuditor` | Blocks reopen when status ∈ (`ReadyForPayment`,`Processed`). No money. |
| `usp_ForwardApprovalRequest` / `usp_ProcessApprovalAction` | Explicitly do **not** write `ReadyForPayment`. |
| `ClaimStatuses.ReadyForPayment` | Shared status constant. |

### 1.3 Live UAT vs repository (deployment gate for this proof)

| Procedure | Repo FSOT intent (RC-015A/B/C) | Live UAT `modify_date` / body | Live match? |
| --- | --- | --- | --- |
| `usp_MarkRequestReadyForPayment` | Status-only named history INSERT | **2026-09-15 21:50:31** named INSERT | **Yes** (RC-014D) |
| `usp_RecalculateApprovalRequest` | Latest snapshots → `FinalApprovedAmount = SUM(Claim+ExpenseTotal)` | **2026-07-31** Utilities body (activity-based; not FSOT snapshot roll-up) | **No** |
| `usp_GetApprovalRequest` | `ISNULL(FinalApprovedAmount, TotalApprovedAmount)` | **2026-07-30** returns raw `FinalApprovedAmount`; reconstructs Original | **No** |
| `usp_Export_Auditor_Final_Excel` | Latest-version Approved only | **2026-07-23** all `DetailStatus='Approved'` versions | **No** |
| `usp_GetMyClaims` / `usp_GetApprovedClaimsByUser` | Still four-part `TotalApprovedAmount` alias | Unchanged **2026-06-15** | Match repo (grids still omit Stationary) |
| Computed `TotalApprovedAmount` | Design target includes Stationary | Live CK still Claim+DA+Hotel+Travel | Stationary still omitted |

**Finding:** RC-015A–C artifacts exist in the repository. Live UAT still serves pre-FSOT bodies for roll-up, header read, and export. This proof therefore certifies **live money behavior**, not assumed post-deploy FSOT behavior.

---

## 2. Request #97 Financial Proof

### 2.1 Header (live UAT)

| Field | Value |
| --- | --- |
| Status | `ReadyForPayment` |
| `FinalApprovedAmount` | **NULL** |
| `TotalApprovedAmount` | **7,736.44** |
| `ApprovedExpenseAmount` | **1,475.00** |
| `ApprovedClaimAmount` | 6,261.44 |
| `ApprovedDAAmount` | 575.00 |
| `ApprovedHotelAmount` | 450.00 |
| `ApprovedTravelAmount` | 450.00 |
| `TotalStationaryAmount` | 0.00 |
| `TotalClaimAmount` / `TotalExpenseAmount` | 6,261.44 / 1,475.00 |
| `ApprovedKM` / `TotalKM` | 1,669.72 / 1,669.72 |
| `OriginalClaimAmount` / `TotalDeductionAmount` | 0.00 / NULL |

### 2.2 Detail shape

| Metric | Value |
| --- | --- |
| Detail rows | 32 |
| Distinct activities | 31 |
| Rows with `DetailVersion > 1` | 1 |
| `DetailStatus='Approved'` rows | 31 |
| Latest rows per activity | 31 |
| Latest payable (not Rejected/Returned) | 31 (all Approved) |

### 2.3 Latest payable snapshots

Predicate: `MAX(ISNULL(DetailVersion,1))` then `MAX(ApprovalRequestDetailId)` per `ActivityId`; sum where `DetailStatus` not in (`Rejected`,`Returned`).

| Component | Amount (₹) |
| --- | --- |
| KM | 1,669.72 |
| Claim (`Snapshot_CalculatedClaimAmount`) | 6,261.44 |
| DA | 575.00 |
| Hotel | 450.00 |
| Travel | 450.00 |
| Stationary | 0.00 |
| `Snapshot_ExpenseTotal` | 1,475.00 |
| Four-part (DA+Hotel+Travel+Stationary) | 1,475.00 |
| **DayNetSum = Claim + ExpenseTotal** | **7,736.44** |

Identity checks on Request 97:

```
∑(Claim + Snapshot_ExpenseTotal) = 6,261.44 + 1,475.00 = 7,736.44
TotalApprovedAmount                = 6,261.44 + 575 + 450 + 450 = 7,736.44
DayNetSum − TotalApprovedAmount    = 0.00
ExpenseTotal − four-part           = 0.00   (Stationary = 0)
FinalApprovedAmount − DayNetSum    = NULL − 7,736.44  → NOT EQUAL
```

### 2.4 Export sums (Request 97)

| Export predicate | Rows | SUM(`[Final Approved Amount]`) |
| --- | --- | --- |
| Live UAT SP (all Approved versions) | 31 | **7,736.44** |
| Repo RC-015C latest-version predicate | 31 | **7,736.44** |

On this claim the live legacy export and the repo latest-version export agree because the superseded detail row is **not** `Approved`. That masks RR-02; it does not prove live export is FSOT-safe for every claim.

### 2.5 Fleet-wide `FinalApprovedAmount`

| Metric | Value |
| --- | --- |
| Headers | 91 |
| `FinalApprovedAmount` NULL | **91 (100%)** |
| `ReadyForPayment` with Final populated | **0** |
| `ReadyForPayment` with Final NULL | **46** |

`FinalApprovedAmount` is not an operational payroll field on live UAT today.

---

## 3. Cross-Surface Reconciliation (Request 97)

Expected canonical request payable under FSOT: **`FinalApprovedAmount = ₹ 7,736.44`**.

| Surface | Source of payable | Observed (₹) | vs 7,736.44 |
| --- | --- | --- | --- |
| Employee grid | `usp_GetMyClaims` → `TotalApprovedAmount` | **7,736.44** | Match (via computed alias) |
| Employee modal | Latest Claim + `Snapshot_ExpenseTotal` (not Rejected) | **7,736.44** | Match |
| L1 modal | Same page as Auditor (`ClaimsApproval_v2`); repo binds header `FinalApprovedAmount` | Live header Final = **NULL** → would display **0.00** if RC-015A C# is running against live Get/Recalc; pre-015A dashboard SQL would show **7,736.44** | **Variance risk** |
| Auditor modal | Same as L1 | Same as L1 | **Variance risk** |
| Ready-for-Payment | Status transition only; money from prior recalc + modal | Status **ReadyForPayment**; MarkReady writes **no** amounts | Status OK; money not rewritten |
| Excel | Live `usp_Export_Auditor_Final_Excel` SUM | **7,736.44** | Match |
| SQL header computed | `TotalApprovedAmount` | **7,736.44** | Match |
| SQL header canonical | `FinalApprovedAmount` | **NULL** | **FAIL** |

### Variances (do not fix)

| ID | Finding |
| --- | --- |
| P-01 | Canonical `FinalApprovedAmount` is NULL; operational payable is `TotalApprovedAmount` / snapshot DayNet. |
| P-02 | Grids never bind `FinalApprovedAmount`; they re-sum four `Approved*` components (no Stationary). Hidden here because Stationary = 0. |
| P-03 | Live export lacks latest-version filter; masked on 97. |
| P-04 | If RC-015A C# is live without FSOT SQL, Auditor/Ready green tile binds NULL Final → **0.00** while grids/Excel still show 7,736.44. |
| P-05 | Original/Deduction tiles remain reconstructed and are out of scope for net-payable certification (see Reporting Reconciliation V-01). |

---

## 4. Payroll Readiness Assessment

| Finding | Result | Evidence |
| --- | --- | --- |
| Ready-for-Payment is a status gate for export | **Proven** | Live + repo export `WHERE Status IN ('Completed','ReadyForPayment','Processed')` |
| MarkReady does not invent payable amounts | **Proven** | Live `usp_MarkRequestReadyForPayment` updates Status/audit only |
| Excel day net = Claim + `Snapshot_ExpenseTotal` | **Proven** | Live export SELECT |
| Excel total on Request 97 = 7,736.44 | **Proven** | Live SUM = 31 × day nets |
| Grids expose payable as `TotalApprovedAmount` | **Proven** | `usp_GetMyClaims` / `usp_GetApprovedClaimsByUser` |
| Payroll consumes `FinalApprovedAmount` | **Unknown** | Column exists; **0** populated rows on UAT; no TA/DA payroll importer found in scoped repo |
| Payroll consumes `TotalApprovedAmount` | **Unknown** | No consumer code in scoped surfaces beyond UI grids |
| Payroll consumes Excel export file | **Assumed handoff only** | `Post_GoLive_Baseline.md` documents Payroll ← export / Approved Claims; no automated import proven |
| Header advances to `Processed` after payroll | **Unknown** | Status value appears in filters/constants; none of the certified six mutators write `Processed` |
| External ERP payroll module linked to this payable | **Unknown** | Not present in scoped TA/DA paths; not inspected further |

**Explicit statement:** Payroll integration **cannot be proven** from this repository and UAT as an automated consumer of `FinalApprovedAmount`, `TotalApprovedAmount`, or the Excel file. The only proven Ready-for-Payment → money path inside scope is **human / file export** via `usp_Export_Auditor_Final_Excel` from My Claims and Approved Claims.

---

## 5. Financial Governance Certificate

### Proven

| Theme | Evidence |
| --- | --- |
| Latest-version ownership (UI / roll-up design) | RC-012 predicates; Request 97 latest DayNet = 7,736.44; repo export RC-015C body uses same predicate |
| Day snapshot ownership | Payable days use `Snapshot_CalculatedClaimAmount` + `Snapshot_ExpenseTotal`; four-part equals ExpenseTotal on 97 |
| Request payable via **computed** header | `TotalApprovedAmount = 7,736.44 = DayNetSum` on Request 97 |
| Ready-for-Payment transition | Status-only; history proven RC-014D; MarkReady does not mutate money |
| Export amount on this claim | Live Excel SUM = 7,736.44 |
| Ready-for-Payment consumer inventory | MarkReady, Export, GetApprovedClaims, GetMyClaims (+ UI callers) documented |

### Not Proven

| Theme | Gap |
| --- | --- |
| Canonical `FinalApprovedAmount` as live Net Payable | NULL on Request 97 and all UAT headers |
| Live deployment of RC-015A roll-up / Get header / RC-015C export | Live procedure bodies predate FSOT repo artifacts |
| Stationary in computed `TotalApprovedAmount` | Live CK still four-part; Stationary=0 on 97 masks divergence |
| Actual payroll import consumer | No proven ERP/payroll reader of Final / Total / Excel |
| Cross-surface identity when Stationary ≠ 0 or dual Approved versions | Not exercised by Request 97 |

### Recommendation

| Question | Answer |
| --- | --- |
| Can FSOT (RC-015A–E) be considered **complete**? | **No.** |
| Can Sprint 3 start UX / telemetry / performance assuming one live money formula? | **No** until RC-015A and RC-015C are **deployed to UAT**, `FinalApprovedAmount` is populated by roll-up, and this proof is re-run. |
| What is still safe to treat as frozen? | Workflow Ready-for-Payment (RC-014D). Operational net on Request 97 via snapshots + `TotalApprovedAmount` + Excel. |
| Minimum close-out for a PASS re-proof | Deploy FSOT `usp_RecalculateApprovalRequest`, `usp_GetApprovalRequest`, `usp_Export_Auditor_Final_Excel`; run roll-up on ReadyForPayment claims; re-certify Request 97 with `FinalApprovedAmount = 7,736.44`; document payroll’s actual intake (Excel column / header field) or keep it Explicitly Unknown. |

---

## 6. PASS / FAIL Verdict

| Success criterion | Result |
| --- | --- |
| Request #97 proves `FinalApprovedAmount = ₹ 7,736.44` | **FAIL** (NULL) |
| Latest snapshots reconcile exactly to request payable | **PASS** vs `TotalApprovedAmount` / DayNet (**7,736.44**); **FAIL** vs `FinalApprovedAmount` |
| Employee, L1, Auditor, Ready-for-Payment, Excel show same canonical payable | **PARTIAL** — grids/Excel/snapshots = 7,736.44; canonical Final absent; Auditor/Ready header bind risk if RC-015A C# is live |
| Every Ready-for-Payment consumer inventoried | **PASS** |
| Payroll dependency proven or marked Unknown | **PASS** (marked **Unknown**) |
| No application/SQL deploy/Git operations in this RC | **PASS** |
| Deliverable written | **PASS** (`docs/reviews/Post_GoLive/RC015E_Payroll_Proof.md`) |

### Overall: **FAIL**

RC-015E certifies that Request 97’s **operational** net payable is **₹ 7,736.44** and that Ready-for-Payment does not invent money, but it does **not** certify that payroll would consume the FSOT canonical field `FinalApprovedAmount`. Live UAT has not yet activated the RC-015A/C SQL ownership chain. FSOT program closure remains blocked on UAT deployment + a re-run of this proof (and an explicit payroll intake statement).

No application code, SQL objects, Git history, tags, or deployments were modified by this RC beyond creating this certificate document.
