# L2 / Auditor Status Transitions (As-Is)

**Captured:** 2026-09-15 from UAT procedure bodies + `ClaimsApproval_v2.aspx.cs`.  
No production runtime traces were queried.

Canonical words: `ClaimStatuses` (`Draft`, `Submitted`, `Pending`, `Approved`, `Rejected`, `Returned`, `Completed`, `ReadyForPayment`, `Processed`).

---

## 1. Request header (`SalesApprovalRequest.Status`)

| Before | Action | After | Evidence |
| --- | --- | --- | --- |
| (none) | Employee NEW submit | `Pending` | `usp_SubmitSalesDailyActivities` INSERT |
| `Returned` | Employee RESUBMIT of returned days | `Pending` | same SP UPDATE; history Old=`Returned` New=`Pending` |
| `Pending` | L1 day APPROVE (partial) | `Pending` | UAT `usp_ProcessApprovalAction` |
| `Pending` | L1 day RETURN | `Returned` | same SP; CurrentApprover not cleared |
| `Pending` | L1 last-day APPROVE with next required approver | `Pending` (pointer moves) | UAT `usp_ProcessApprovalAction` auto-forward **and/or** `usp_ForwardApprovalRequest` |
| `Pending` | Explicit L1 Forward | `Pending` (pointer to Auditor) | `usp_ForwardApprovalRequest` (UAT 2026-09-15). History Old=Pending New=Pending |
| `Pending` | Auditor day APPROVE (partial) | `Pending` | `usp_ProcessApprovalAction` |
| `Pending` | Auditor last-day APPROVE (role = Auditor, no next) | `Pending` | UAT SP comment: remain Pending until Mark Ready For Payment |
| `Pending` | Non-auditor last level, no next | `Completed` | UAT `usp_ProcessApprovalAction` `ELSE IF role <> 'Auditor'` |
| `Pending` | Auditor Ready for Payment | **refused** | UAT `usp_MarkRequestReadyForPayment` requires `Status = 'Completed'` |
| `Completed` | Auditor Ready for Payment | `ReadyForPayment` | same SP (reachable only if header was already Completed) |
| `Completed` | any Forward | refused | `usp_ForwardApprovalRequest` payment lock |
| `Pending` | Whole-claim Return (manager) | employee-correction (SP `usp_ReturnClaimToEmployee`; body not expanded in this pack) | UI hidden for Auditor |
| `Pending` | Auditor Forward | refused `THROW 51012` | `usp_ForwardApprovalRequest` |

Queue visibility: `usp_GetPendingApprovalsForUser_Filters` shows a request **only** while header `Status = 'Pending'` **and** `CurrentApproverLoginId` is the logged-in user. After Forward to Auditor, L1 no longer sees it. After a day RETURN, header becomes `Returned` and **drops out of both L1 and Auditor queues** until employee resubmit.

---

## 2. Day snapshot (`SalesApprovalRequestDetail.DetailStatus`)

Latest row per activity is the actionable row (`MAX(ApprovalRequestDetailId)` in ProcessApprovalAction).

| Before (latest) | Action | After | Notes |
| --- | --- | --- | --- |
| `Pending` / `Submitted` / '' | L1 or Auditor APPROVE | `Approved` + `DetailApprovedLevel = CurrentApproverLevel` | Receipts should already be resolved in UI; UAT ProcessApprovalAction does not itself require all expenses Approved |
| `Approved` at lower level | Auditor APPROVE | `Approved` at Auditor level | Gatekeeper treats prior-level Approved as actionable |
| `Pending` | Manager RETURN | `Returned` | Auditor C# throws before SP |
| `Pending` | Auditor REJECT | `Rejected` | Via `usp_ProcessSalesApprovalRequestDetail`, not ProcessApprovalAction |
| `Returned` | Auditor/L1 APPROVE | refused in C# | “Returned days cannot be approved until the employee resubmits” |
| `Returned` | Employee RESUBMIT | **new row** `Pending` (version+1); old row remains `Returned` | `usp_SubmitSalesDailyActivities` |
| `Approved` at current level | any | UI shows “✔ Done”; bulk skips “Already processed” | |
| `Rejected` | any | locked | |

UAT `usp_ProcessApprovalAction` also supports REJECT (maps to `Rejected`) with latest-version guard. The Auditor Web Forms path does **not** call it for Reject.

---

## 3. Approver rows (`SalesApprovalRequestApprover.Status`)

| Before | Action | After |
| --- | --- | --- |
| (insert) NEW submit | L1 Pending; optional L2/L3 Pending; Auditor Pending | all created Pending; header points at L1 |
| L1 Pending | L1 Forward **or** UAT ProcessApprovalAction last-approve auto-forward | L1 `Approved`; next (Auditor) remains/activated `Pending` |
| L1 Pending | L1 day RETURN | current-level approver `Returned` |
| Auditor Pending | Auditor last-approve (UAT ProcessApprovalAction) | current approver **not** completed when role = Auditor and no next (header stays Pending; approver row can remain Pending) |
| any | Employee RESUBMIT | **all** approver rows reset to Pending, ActionBy/Date/Remarks cleared |

---

## 4. Activity (`SalesDailyActivity.ClaimStatus`)

| Before | Action | After |
| --- | --- | --- |
| `Draft` / `Returned` | Submit / Resubmit | `Submitted` |
| (any) | ProcessApprovalAction APPROVE/RETURN/REJECT | `Approved` / `Returned` / `Rejected` |
| (any) | Auditor Reject via `MarkActivityRejectedForDay` | `Rejected` |

---

## 5. Expense (`SalesDailyExpense.ClaimStatus`)

| Before | Action | After |
| --- | --- | --- |
| `Draft` / `Returned` | Submit | `Submitted` |
| `Submitted` / `Pending` / `Draft` / '' | Display JSON | coerced to `Pending` in `usp_GetApprovalRequestDetails` |
| not Approved/Rejected | Day APPROVE (ProcessApprovalAction) | `Approved` |
| not Rejected | Day RETURN (ProcessApprovalAction) | `Returned` |
| not Approved | Day REJECT (ProcessApprovalAction) | `Rejected` |
| not Approved | `MarkActivityRejectedForDay` | `Rejected` |
| (any non-deleted) | `usp_UpdateSalesDailyExpenseStatus` | `@NewStatus` as passed (`Approved` / `Rejected` / `Returned`) |
| `Approved` | Auditor expense Reject in unified modal | `Rejected` (UI offers Reject on already-Approved receipts) |

`ApprovalStatus` / `ApprovedBy` / `ApprovedOn` on the expense table are not updated by these SPs.

---

## 6. Required matrix from the Phase 1 prompt

| Before | Action | After |
| --- | --- | --- |
| Pending (header) | Auditor Approve (day, not last) | Pending |
| Pending (header) | Auditor Approve (last day, UAT SP, role Auditor) | Pending |
| Pending (detail) | Auditor Approve | Approved (at current level) |
| Pending (header/detail) | Auditor Return (day) | **Blocked in C#** (`Auditors cannot return days`). UAT ProcessApprovalAction would write Returned if called. |
| Returned (header) | Employee Resubmit | Pending (reset to L1) |
| Last Pending (Auditor, all days resolved) | Auditor Finalize (`usp_MarkRequestReadyForPayment`) | **Error: Request not completed.** Intended after-state in SP is `ReadyForPayment` **from** `Completed`. |
| Expense Pending | Approve (`usp_UpdateSalesDailyExpenseStatus`) | Approved (expense). Latest detail snapshot amounts recalculated. Header totals recalculated. Day status unchanged until day Approve. |

---

## 7. Finalize visibility vs persist

`btnMarkReadyForPayment.Visible` when:

- `IsCurrentUserAuditor()`
- no enabled day-action buttons
- unprocessed receipts = 0 (`ClaimStatus` not Approved/Rejected on latest-joined expenses)
- `GetReturnedItemCount` = 0

That is a UI gate only. Persist still requires header `Completed` inside `usp_MarkRequestReadyForPayment` (UAT `modify_date` 2026-01-09).
