# Gap Analysis — Sprint 2, Task 02 (Phase A) — `usp_ForwardApprovalRequest`

**Phase:** A – Analysis (complete); B – Implementation reviewed against this document at finalization.  
**Object reviewed:** `Database/Stored Procedures/Approval/usp_ForwardApprovalRequest.sql`.  
**Authoritative references:** Functional Specification Phase-1 v1.0; Technical Design Specification Phase-1 v1.0.  
**Frozen peer:** `usp_ProcessApprovalAction` (Sprint 2 Task 01) — out of scope; not modified.

---

## Current Implementation Profile

### 1. Procedure inputs

| Parameter | Type | Role |
| --- | --- | --- |
| `@ApprovalRequestId` | `INT` | Claim header identifier |
| `@ActionBy` | `NVARCHAR(100)` | Acting user login |

No level, role, remarks, or status parameters are accepted. Level and status are read from the locked header row (consistent with TDS §4.5: “notably, not the level”).

### 2. Procedure outputs

Success path returns a single result set:

| Column | Content |
| --- | --- |
| `Success` | `1` |
| `Message` | Forward / final-approval text |
| `ApprovalRequestId` | Echo of input |
| `CurrentApproverLoginId` | Next login, or `NULL` on final branch |
| `CurrentApproverLevel` | Next level, or `NULL` on final branch |
| `RequestStatus` | `'Pending'` when a next approver exists; **`'Approved'`** on the no-next branch |

Error path (`CATCH`) returns a different shape: `Success = 0`, `ErrorNumber`, `ErrorMessage`, `ErrorProcedure`, `ErrorLine`. Errors are swallowed into the result set; they are not re-raised to the caller.

### 3. Transaction scope

- `SET XACT_ABORT ON`
- Transaction opens at the start of the `TRY`
- Header is read with `UPDLOCK, HOLDLOCK` (good concurrency pattern relative to many peer procedures)
- All subsequent reads/writes occur inside that transaction until `COMMIT` on either branch
- `CATCH` rolls back when `@@TRANCOUNT > 0`

**Spec alignment:** VF-06 requires forward checks inside the same transaction against locked rows. The lock pattern is present; several required checks are missing (see gaps below).

### 4. Current routing logic

1. Lock/read header: `CurrentApproverLoginId`, `CurrentApproverLevel`, `Status`
2. Fail if `@CurrentApproverLevel IS NULL` (message: “Approval request not found.”)
3. Require `Status = 'Pending'`
4. Require `CurrentApproverLoginId = @ActionBy` (no trim/case normalize)
5. Count details with `DetailStatus IN ('Pending','Returned')` on the request (all versions; no expense check)
6. `SELECT TOP (1)` next required approver with `ApproverLevel > current`, `IsRequired = 1`, `Status = 'Pending'` — **no `ORDER BY ApproverLevel`**
7. **If next found:** mark current approver `Approved`; set header routing to next; force next approver row `Status = 'Pending'`; commit; success (`RequestStatus = Pending`)
8. **If no next:** mark current approver `Approved`; set header `Status = 'Approved'`, clear routing columns; commit; success (`RequestStatus = Approved`)

### 5. Current status transitions

| Branch | Header `Status` before | Header `Status` after | Spec |
| --- | --- | --- | --- |
| Next exists | `Pending` | `Pending` (routing columns change) | Aligns with T-04 / diagram “Pending → Pending: Forward” |
| No next | `Pending` | **`Approved`** | Forbidden — H-1, F-01 |

Never writes `ReadyForPayment` or `Completed` today. Writing `Approved` is itself illegal.

### 6. Current approver completion

On both branches, updates `SalesApprovalRequestApprover` for (`ApprovalRequestId`, `@ActionBy`, `@CurrentApproverLevel`, `Status = Pending`) to `Status = Approved`, with `ActionBy` / `ActionDate` / `ModifiedBy` / `ModifiedOn`. Throws if `@@ROWCOUNT = 0`.

### 7. Next approver activation

When a next level is found:

- Header `CurrentApproverLoginId` / `CurrentApproverLevel` set to next
- Next approver row forced to `Status = Pending` (often a no-op; selection already required `Status = Pending`)
- Header status left `Pending`

### 8. Header updates

| Branch | Columns written |
| --- | --- |
| Next exists | `CurrentApproverLoginId`, `CurrentApproverLevel`, `ModifiedBy`, `ModifiedOn` |
| No next | `Status = 'Approved'`, both routing columns `NULL`, `ModifiedBy`, `ModifiedOn` |

No totals columns are recalculated.

### 9. `ApprovalRequestStatusHistory` usage

**None.** The procedure never inserts into `ApprovalRequestStatusHistory`.

### 10. Error handling

Business failures use `THROW` with numbers `51001`–`51009`. The `CATCH` block rolls back and returns a `Success = 0` result set instead of propagating the error. Callers that only inspect exceptions will miss failures; callers that bind the success-shaped columns will see a schema mismatch on error.

---

## Attention items (routing / final status)

### Routing ownership (R-07 / O-11 / G-04)

| | |
| --- | --- |
| **Current Behaviour** | This procedure writes `CurrentApproverLoginId` / `CurrentApproverLevel` on the next-approver branch and clears them on the final branch. Separately, frozen `usp_ProcessApprovalAction` still advances (and can clear) the same columns when the last day at a level is approved (Task 01 left that path functional with Task 02 TODOs). |
| **Required Behaviour** | `usp_ForwardApprovalRequest` is the **only** writer of those two columns (FS §7.4 R-07; FS O-11; TDS G-04). |
| **Implementation Required** | Harden this procedure’s forward path so it can safely own routing. Removing the day-SP routing writes is a **cross-procedure** step and must not be done until Forward is correct and the product accepts that day approval no longer auto-advances. Out of scope for SQL in Phase A; ProcessApprovalAction remains frozen. |
| **Risk** | **Critical** while dual writers remain: claims can advance without Forward validations (FS Current Behaviour T-04 / G-04). Hardening Forward alone does not close R-07 until the day-SP path is retired in a later coordinated change. |

### `CurrentApproverLoginId`

| | |
| --- | --- |
| **Current Behaviour** | Compared to `@ActionBy` with `<>` only. No `UPPER` / `LTRIM` / `RTRIM`. Database CI collation absorbs case; whitespace can still fail. No explicit refuse when login is `NULL` beyond the level-null conflation. |
| **Required Behaviour** | Caller must be the current approver (FS §7.3 #1; R-01; A-00; VF context). Comparisons should be robust to case/whitespace consistent with Task 01 hardening. |
| **Implementation Required** | Normalize both sides before compare; refuse when current login is null/blank while header is `Pending`. |
| **Risk** | **Medium.** Legitimate forwards may fail on whitespace; CI collation already covers pure case variance. |

### `CurrentApproverLevel`

| | |
| --- | --- |
| **Current Behaviour** | Used to select next level (`ApproverLevel > @CurrentApproverLevel`) and to complete the current approver row. Existence check treats `@CurrentApproverLevel IS NULL` as “request not found,” which also matches a found header whose level was cleared. |
| **Required Behaviour** | Level is the routing pointer (FS §7.2). Missing request vs cleared pointer must be distinguishable. Next required approver must be selected in strict ascending level order (R-10; FS §7.3 #7). |
| **Implementation Required** | Separate “row not found” from “level is null”; add `ORDER BY ApproverLevel ASC` to the next-approver query. |
| **Risk** | **High** for missing `ORDER BY` (nondeterministic next level / possible skip — F-07). **Medium** for null-level conflation (wrong error / wrong refuse path). |

### Final approval — `Approved` vs `ReadyForPayment`

| | |
| --- | --- |
| **Current Behaviour** | When no next required pending approver exists, header `Status` is set to **`Approved`**, routing cleared, message “Request finally approved.” |
| **Required Behaviour** | **`Approved` must never be a header status** (FS Rule H-1; forbidden transition F-01). A fully approved claim remains **`Pending` until the Auditor marks `ReadyForPayment`** (H-1; T-09). Forwarding past the Auditor is **not** a forward; the Auditor’s terminal action is Mark Ready For Payment via `usp_MarkRequestReadyForPayment` (R-06; §8.6; O-12). If no next approver exists and the current level is **not** the Auditor, the configuration is invalid: **refuse**; do not complete; do not mark ready for payment (R-05; decision table §7.7). Authorization matrix: Forward = Manager **Yes**, Auditor **No** (FS §8.2). VF-05: a next required approver must exist for Forward. |
| **Implementation Required** | **Do not** replace the illegal `Approved` write with `ReadyForPayment` inside this procedure. That would violate R-06, O-12, T-09, and F-08 (Manager → ReadyForPayment). Correct final-branch behaviour for Forward: **refuse** when no next required approver exists (covers Manager-without-Auditor per R-05, and Auditor-as-current per R-06 / matrix). Clearance of routing and transition to `ReadyForPayment` remain owned by `usp_MarkRequestReadyForPayment` (O-12; VR-01–VR-07). |
| **Risk** | **Critical.** Today’s `Approved` header is an illegal state that breaks the published status model and any consumer that expects only Draft / Submitted / Pending / Returned / ReadyForPayment / Completed. Silently switching Forward to write `ReadyForPayment` would be a second critical defect (wrong owner, wrong actor). |

---

## Gap catalogue

For every gap: Current Behaviour → Required Behaviour → Implementation Required → Risk.

| # | Current Behaviour | Required Behaviour | Implementation Required | Risk |
| --- | --- | --- | --- | --- |
| G1 | On no-next branch, sets header `Status = 'Approved'` and clears routing. | `Approved` is never a header status (H-1, F-01). Forward requires a next required approver (T-04, VF-05). No next + non-Auditor → refuse (R-05). Auditor terminal action is Mark Ready For Payment, not Forward (R-06, O-12, T-09). | Remove the `Status = 'Approved'` branch. Refuse with a clear business error when no next required approver exists. Do **not** write `ReadyForPayment` here. | **Critical** |
| G2 | No expense Pending/Returned checks. | VF-03 / VF-04; FS §7.3 #4–#5; R-04; MG-04. | Inside the transaction, after header lock, refuse if any in-scope expense is Pending or Returned. | **High** |
| G3 | Detail check uses all detail rows with `DetailStatus IN (Pending, Returned)` only. No latest-version filter. No “resolved at current level” test via `DetailApprovedLevel`. | VF-01 / VF-02; §7.3 #2–#3; latest versions only (V-04 / VL-04). Days Approved at a prior level remain `Approved` and must still be resolved at the current level before forward (detail model §5; TDS level progression notes). | Restrict to latest detail version per activity; refuse Returned; refuse days not yet resolved at `@CurrentApproverLevel` (e.g. require `DetailApprovedLevel >= CurrentApproverLevel` for non-Rejected latest days, or equivalent FS-faithful predicate). | **Critical** |
| G4 | Never writes `ApprovalRequestStatusHistory`. | Forward creates a status history record describing the handover (FS §7.3 #10; AD-04; O-11; TDS G-16). | Insert history on successful forward (typically OldStatus/NewStatus both `Pending`, with remarks/action type describing level handover; ApproverLevel stamped). | **High** (audit gap) |
| G5 | `SELECT TOP (1)` next approver without `ORDER BY ApproverLevel`. | Next required approver in level order; strictly ascending (R-10; §7.3 #7; F-07 no skip). | Add `ORDER BY a.ApproverLevel ASC` (and stable tie-breaker if needed). | **High** |
| G6 | No role gate: any current approver, including Auditor, may call Forward. | Forward is a Manager action; Auditor **No** (FS §8.2). R-06: forwarding past Auditor is not a forward. | Resolve current approver role from `SalesApprovalRequestApprover`; refuse when role is Auditor (or when no next exists — see G1). | **High** |
| G7 | Dual ownership of routing columns with frozen `usp_ProcessApprovalAction` (and any legacy writers). | Sole owner is this procedure (R-07, O-11, G-04). | Phase B hardens Forward. Removal of day-SP routing writes is a later coordinated change; ProcessApprovalAction stays frozen for this task. | **Critical** until dual write ends; **deferred** for the day-SP half |
| G8 | No payment lock / no refuse for `ReadyForPayment` beyond “must be Pending”. | Pending-only for Forward (T-04). Completed immutable (F-11, PL-02). R-02: no current-approver actions while Returned / ReadyForPayment / Completed. | Keep/strengthen Pending-only guard; optionally explicit Completed message for defence in depth (Task 01 pattern). | **Medium** (Pending guard already blocks most; explicit Completed is clearer) |
| G9 | `@CurrentApproverLevel IS NULL` → “not found”, even if the header row exists. | Distinguish missing request from invalid/cleared routing pointer. | `IF NOT EXISTS` / `@@ROWCOUNT` for existence; separate throw when level or login is null on a Pending claim. | **Medium** |
| G10 | Login compare without normalize. | Robust current-approver match (R-01), consistent with Task 01. | `UPPER(LTRIM(RTRIM(...)))` on both sides. | **Medium** |
| G11 | No totals recalculation. | FS §7.3 #9; O-11 lists header totals; TDS §4.5 target puts recalc inside this procedure’s transaction (G-10 / G-20). | Candidate for Phase B if Task 02 implementation scope includes it; otherwise document as deferred with FS citation. Application today recalculates on a separate connection before the call (TDS §4.5) — still a G-20 risk until moved in-procedure. | **High** if left split across connections; scope decision needed before coding |
| G12 | `CATCH` returns `Success = 0` row; does not re-raise. | Failures must not leave partial writes (transaction rollback is good). Callers should observe failure reliably (hardening / X-series intent). | Prefer re-raise after rollback (or align deliberately with a documented app contract if the UI binds the Success column). Confirm call-site expectation before changing shape. | **Medium** |
| G13 | “Activate next” sets already-Pending next row to Pending. | Harmless if row exists; must still fail if next row missing. | Keep existence check; activation update may remain or become a no-op touch of `ModifiedOn` only. | **Low** |
| G14 | Declares `@STATUS_REJECTED` but never uses it. | Rejected days are terminal resolutions for Auditor completeness (VR-03); Forward at Manager should not treat Rejected as “pending.” | Use Rejected correctly in completeness predicates when implementing G3. | **Low** (dead declaration today; becomes relevant with G3) |

---

## Spec conclusions (evidence, not assumptions)

| Conclusion | Evidence |
| --- | --- |
| Header must never become `Approved` | FS §4.1 H-1; §4.4 F-01 |
| Successful Manager forward keeps header `Pending` and moves routing | FS §4.2 diagram; T-04; §7.7 Manager + next exists |
| Forward must not set `ReadyForPayment` | R-06; T-09 owns that transition via `usp_MarkRequestReadyForPayment`; O-12; §8.2 Auditor Forward = No; F-08 |
| No next + non-Auditor → refuse | R-05; §7.7 Manager row “Invalid configuration” |
| Forward requires next required approver | VF-05; T-04 preconditions |
| This procedure is sole routing-pointer owner (target) | R-07; O-11; TDS G-04 — **not met system-wide** while day SP still writes |
| History required on forward | FS §7.3 #10; AD-04; O-11 |
| Expense completeness required inside Forward transaction | VF-03, VF-04, VF-06; §7.3 #4–#5 |

---

## Gap disposition (Phase A)

| Disposition | Items |
| --- | --- |
| Must fix in Forward Phase B (SQL on this procedure only) | G1, G2, G3, G4, G5, G6, G8–G10, G12–G14 as scoped |
| Cross-procedure / frozen peer — document only in this task | G7 (day-SP half of R-07) |
| Confirm scope before coding | G11 (totals in-procedure) |
| Already partially aligned | Transaction + `UPDLOCK, HOLDLOCK`; Pending header guard; next-branch leaves status Pending; current approver completion pattern |

---

## Gap disposition (Phase B — implemented)

| Disposition | Items |
| --- | --- |
| Implemented in `usp_ForwardApprovalRequest.sql` | G1, G2, G3, G4, G5, G6, G8, G9, G10, G12, G13 (activation retained), G14 (Rejected treated as resolved / not blocking) |
| Deferred — frozen peer / out of scope | G7 (`usp_ProcessApprovalAction` dual write) |
| Deferred — explicit Phase B “Do NOT calculate totals” | G11 |
| Not implemented as a write | Any final header status other than remain `Pending` on success |
