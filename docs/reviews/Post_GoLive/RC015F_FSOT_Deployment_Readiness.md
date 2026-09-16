# RC-015F — FSOT Deployment Readiness (Operational Runbook)

**Captured:** 2026-09-15  
**Constraint:** inventory / compare / sequence only. No new business logic. No ALTER. No deploy. No commits.  
**Closes gap proven by:** `docs/reviews/Post_GoLive/RC015E_Payroll_Proof.md`  
**Design refs:** `Sprint3_FSOT_Design.md`, `Sprint3_Reporting_Reconciliation.md`  
**Live check (read-only MCP `anmol_uat`):** procedure `modify_date` confirmed unchanged from RC-015E

**Purpose:** Give a DBA the exact objects, order, backfill, and validation needed so live UAT/Production money matches the certified repository FSOT contract.

**Overall readiness: READY to deploy (package). NOT READY to claim FSOT closed until post-deploy proof PASS.**

---

## Known evidence (reused — do not re-investigate)

| Fact | Value |
| --- | --- |
| Live `usp_RecalculateApprovalRequest` | Utilities body, `modify_date` **2026-07-31** |
| Live `usp_GetApprovalRequest` | Pre-FSOT body, `modify_date` **2026-07-30** |
| Live `usp_Export_Auditor_Final_Excel` | Legacy all-Approved-versions, `modify_date` **2026-07-23** |
| `FinalApprovedAmount` | **NULL on all 91** headers |
| Request 97 operational payable | **₹ 7,736.44** |
| Request 97 `FinalApprovedAmount` | **NULL** |
| Request 97 Status | `ReadyForPayment` |

---

## 1. Deployment Matrix

| Object | Repo | Live | Deploy |
| --- | --- | --- | --- |
| `usp_RecalculateApprovalRequest` | FSOT roll-up: latest snapshots → `Approved*` + `FinalApprovedAmount = SUM(Claim+ExpenseTotal)` over non-Rejected/Returned | Utilities body from `SalesDailyActivity`; does not own snapshot DayNet | **YES — Priority 1** |
| `usp_GetApprovalRequest` | Returns stored `OriginalClaimAmount`; `ISNULL(FinalApprovedAmount, TotalApprovedAmount)` as Net Payable | Reconstructs Original from TotalClaim+TotalExpense; returns raw nullable Final | **YES — Priority 3** |
| `usp_Export_Auditor_Final_Excel` | One latest Approved detail per `ActivityId` (version then Id); day final = Claim+ExpenseTotal | All `DetailStatus='Approved'` rows (no latest-version gate) | **YES — Priority 4** |

### Per-object detail

#### `usp_RecalculateApprovalRequest`

| | |
| --- | --- |
| **Repository file** | `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql` |
| **Repository intent** | Single SQL owner of request Net Payable. Refresh latest day DA/Hotel/Travel/Stationary + four-part `Snapshot_ExpenseTotal`; write header `ApprovedKM/Claim/DA/Hotel/Travel` and `FinalApprovedAmount`. |
| **Live behavior** | Aggregates `SalesDailyActivity` expense/claim fields; writes a different Final formula; does not implement the RC-012 latest-snapshot payable sum. |
| **Deployment priority** | **P1 — mandatory first.** Without this, backfill cannot write the certified Final. |
| **Action** | `ALTER PROCEDURE` from the Approval repo file (CREATE → ALTER if required by shop standard). |

#### `usp_GetApprovalRequest`

| | |
| --- | --- |
| **Repository file** | `Database/Stored Procedures/Approval/usp_GetApprovalRequest.sql` (keep Retrieval copy in sync if both are maintained) |
| **Repository intent** | Header read for Auditor/Ready modal: expose FSOT Final with fallback to computed `TotalApprovedAmount`. Stop reconstructing Original. |
| **Live behavior** | `FinalApprovedAmount` returned as NULL; modal C# that binds Final can show **0.00** while grids still show 7,736.44. |
| **Deployment priority** | **P3 — after backfill.** Fallback protects unrolled rows; populated Final is the target. |
| **Action** | `ALTER PROCEDURE` from Approval (and Retrieval twin if deployed from that path). |

#### `usp_Export_Auditor_Final_Excel`

| | |
| --- | --- |
| **Repository file** | `Database/Stored Procedures/Approval/usp_Export_Auditor_Final_Excel.sql` (Retrieval twin if used) |
| **Repository intent** | Payroll/file extract uses same latest-version Approved day set as UI; workbook SUM equals header Final. |
| **Live behavior** | Can double-count if a superseded snapshot remains Approved (masked on Request 97). |
| **Deployment priority** | **P4 — after header contract is live.** Export must not be “fixed” before Final is authoritative. |
| **Action** | `ALTER PROCEDURE` from Approval (and Retrieval twin if used). |

### Explicitly out of this package (do not expand)

| Item | Note |
| --- | --- |
| `usp_MarkRequestReadyForPayment` | Already live (RC-014D). Status-only. |
| Grid SPs `usp_GetMyClaims` / `usp_GetApprovedClaimsByUser` | Still four-part aliases; unchanged by RC-015A/C. Stationary-in-computed-total is a later schema RC if required. |
| `usp_UpdateSalesDailyExpenseStatus` (RC-015B) | Day Stationary writer — confirm separately if not already promoted; not in the three-object FSOT close package. |
| New formulas / CK changes | Forbidden in this RC. |

---

## 2. Data Impact Assessment

| Object | Existing data affected |
| --- | --- |
| `FinalApprovedAmount` | **91 NULL** headers. After FSOT roll-up, should become DayNet for each request that has payable latest snapshots. |
| `TotalApprovedAmount` | Computed column (Claim+DA+Hotel+Travel). **Not written directly.** May change only if roll-up rewrites component `Approved*` values. On Request 97 those already equal DayNet (Stationary 0). |
| `Snapshot_ExpenseTotal` | Latest-day rows refreshed by roll-up from live non-rejected expenses (four-part). Historical superseded versions are not the roll-up target. |

### Backfill required?

**Yes.** Deploying the procedure alone does not populate the 91 NULL Finals. Existing ReadyForPayment / Completed / other headers stay NULL until `usp_RecalculateApprovalRequest` is **executed** per request.

### Live recalculation required?

**Yes — as the backfill mechanism.** Do **not** invent a separate UPDATE formula. After P1 deploy:

```sql
-- Pattern only (DBA executes after ALTER). One request:
EXEC dbo.usp_RecalculateApprovalRequest @ApprovalRequestId = 97;

-- Fleet backfill: cursor/loop over SalesApprovalRequest Ids (or status subset
-- ReadyForPayment + Completed + Pending as agreed by product). No ad-hoc SET Final = ...
```

Recommended minimum scope for UAT close-out: all headers with `Status IN ('ReadyForPayment','Completed')` (46 Ready + Completed count from RC-015E inventory), then optionally full 91.

### Historical safety?

| Risk | Assessment |
| --- | --- |
| Status overwrite | Roll-up does **not** change `Status`. ReadyForPayment stays ReadyForPayment. |
| Money drift on Request 97 | Expected Final = **7,736.44** (= current DayNet / `TotalApprovedAmount`). Low surprise. |
| Claims with Stationary ≠ 0 | Final may **diverge** from four-part `TotalApprovedAmount` (known FSOT design). Accept as correct under snapshot DayNet; do not “fix” by forcing equality in this package. |
| Dual Approved versions | Export change (P4) **reduces** row count on affected claims; validate before Production payroll file cutover. |
| Rollback | Keep pre-ALTER scripts of the three live bodies. Restoring old Recalc leaves Final values already written (harmless leftovers) unless explicitly nulled — document decision; default leave Final populated. |

**Do not perform updates in this RC.** Scripts above are for the future deployment window only.

---

## 3. Promotion Order

Exact sequence for UAT (repeat for Production after UAT PASS):

| Step | Action | Depends on previous because |
| --- | --- | --- |
| **1** | `ALTER` `usp_RecalculateApprovalRequest` from repo Approval file | Establishes the only authorized writer of `FinalApprovedAmount` from latest snapshots. |
| **2** | Backfill: `EXEC usp_RecalculateApprovalRequest` for target headers (at least Request **97**, then Ready/Completed fleet) | Procedure body must already be FSOT; otherwise backfill writes the wrong contract. |
| **3** | `ALTER` `usp_GetApprovalRequest` from repo Approval file | Header UI/API must read populated Final (with ISNULL fallback for any skipped Id). Doing this before backfill leaves Auditor/Ready tile on fallback/`TotalApprovedAmount` or NULL depending on DLL. |
| **4** | `ALTER` `usp_Export_Auditor_Final_Excel` from repo Approval file | Export must align to latest Approved after request payable is canonical; avoids proving the wrong file shape against a still-null Final. |
| **5** | IIS recycle (app pool hosting AnmolDristi) | Flushes any cached plan/connection oddities; ensures running DLL that calls `usp_RecalculateApprovalRequest` / binds Final sees new SQL. Confirm DLL already contains RC-015A `ExecuteRecalculationSQL` → SP call + Final bind. |
| **6** | Financial proof rerun (RC-015E checklist on Request 97) | Only live evidence that A–E can close. |

### DBA file list (copy to change ticket)

1. `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql`  
2. Backfill EXEC batch (step 2 — DBA-authored loop; no new formula)  
3. `Database/Stored Procedures/Approval/usp_GetApprovalRequest.sql`  
4. `Database/Stored Procedures/Approval/usp_Export_Auditor_Final_Excel.sql`  
5. IIS recycle  
6. Re-run proof queries / UI checks from §4  

If the shop deploys Retrieval duplicates, ALTER the same bodies from:

- `Database/Stored Procedures/Retrieval/usp_GetApprovalRequest.sql`  
- `Database/Stored Procedures/Retrieval/usp_Export_Auditor_Final_Excel.sql`  

after confirming they match Approval.

---

## 4. Validation Plan (post-deploy)

**Reference claim:** Request **97**. Do not change workflow data during validation.

| Check | Expected |
| --- | --- |
| `FinalApprovedAmount` | **₹ 7,736.44** |
| Header | `FinalApprovedAmount` = latest DayNet = `TotalApprovedAmount` on this claim (Stationary 0) |
| Excel | SUM(`[Final Approved Amount]`) = **7,736.44** = header Final; **31** rows (one per activity) |
| ReadyForPayment | Status remains `ReadyForPayment`; MarkReady not re-run |
| Latest-version | Export uses latest Approved only; superseded non-Approved version still excluded |

### SQL smoke (read-only after deploy)

```sql
-- Header
SELECT Status, FinalApprovedAmount, TotalApprovedAmount, ApprovedExpenseAmount
FROM dbo.SalesApprovalRequest
WHERE ApprovalRequestId = 97;
-- Expect: ReadyForPayment | 7736.44 | 7736.44 | 1475.00

-- Procedure stamps
SELECT name, modify_date
FROM sys.procedures
WHERE name IN (
  'usp_RecalculateApprovalRequest',
  'usp_GetApprovalRequest',
  'usp_Export_Auditor_Final_Excel'
);
-- Expect: modify_date = deployment window (not 2026-07-31 / 07-30 / 07-23)

-- Fleet sanity
SELECT
  COUNT(*) AS Headers,
  SUM(CASE WHEN FinalApprovedAmount IS NULL THEN 1 ELSE 0 END) AS FinalNull
FROM dbo.SalesApprovalRequest
WHERE Status IN ('ReadyForPayment','Completed');
-- Expect: FinalNull = 0 for backfilled scope
```

### UI smoke

| Surface | Expect |
| --- | --- |
| Auditor / Ready modal (ClaimsApproval_v2) | Green net **7,736.44** (header Final) |
| Approved Claims grid | **7,736.44** |
| My Claims grid | **7,736.44** |
| Excel from Approved Claims / My Claims | TOTAL Final **7,736.44**, 31 day rows |

### PASS gate for closing RC-015A–E

All of: Final = 7,736.44 on 97; Excel = header; Status unchanged; three SP `modify_date` post-deploy; no payroll formula invented.

---

## 5. Go / No-Go Matrix

| Area | Status |
| --- | --- |
| Repository | **PASS** — FSOT bodies for Recalc / Get / Export exist and match design |
| Live UAT | **FAIL** — pre-FSOT money contract still running |
| Deployment package | **READY** — three ALTERs + EXEC backfill + IIS + proof |
| Financial proof | **Pending rerun** after deploy |

### Conditions before RC-015A–E can be considered closed

1. UAT steps 1–6 complete with Request 97 validation **PASS**.  
2. `FinalApprovedAmount` populated for the agreed backfill scope (minimum: ReadyForPayment + Completed).  
3. Live procedure definitions match repo FSOT bodies (stamp + spot-check definition).  
4. Optional but recommended: Production promotion using the **same** order after UAT PASS.  
5. Payroll intake remains **Unknown** until business names Excel vs Final — does **not** block FSOT ownership close, but must stay explicit (RC-015E).

**Go for deployment window:** YES (UAT).  
**Go for “FSOT complete” claim:** NO until §4 PASS.

---

## 6. Executive Recommendation

Treat FSOT as a **deployment-governance** close, not another feature RC.

1. Schedule a short UAT SQL window and promote the three procedures in the order above.  
2. Backfill by **executing** the new roll-up — never hand-SET Final.  
3. Recycle IIS and re-run the RC-015E proof on Request 97.  
4. Only after that PASS, mark RC-015A through RC-015E closed and allow Sprint 3 UX / telemetry / performance work that assumes one live money formula.  
5. Keep payroll’s actual consumer documented as Unknown until Finance confirms intake; export alignment is still mandatory for file integrity.

**RC-015F itself:** PASS as readiness / runbook (this document).  
**FSOT program:** still open pending deploy + proof.

No application code, SQL objects, Git history, tags, or deployments were modified by this RC beyond creating this runbook.
