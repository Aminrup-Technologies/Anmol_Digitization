# L2 / Auditor Call Graph

**Captured:** 2026-09-15  
**Primary type:** `AnmolDristi.ClaimsApproval_v2`  
**Helpers:** `SalesApprovalDetailVersionHelper`, `ClaimStatuses`

---

## 1. Landing / queue

```
Page_Load
  └─ PopulateFilters                    previous calendar month defaults
  └─ BindRequestsGrid
        usp_GetPendingApprovalsForUser_Filters
          @ApproverLoginId, @Year, @Month, @EmpCode
        gvRequests.DataBind
        UpdateRequestSummaryCounters
btnSearch_Click / btnPrevMonth_Click / btnNextMonth_Click / btnCurrentMonth_Click / btnReset_Click
  └─ BindRequestsGrid
gvRequests_RowCommand ("View")
  └─ SetCurrentUserApproverDetails(reqId)
  └─ ShowDetailsModal(reqId)
        hfActionRequestId, lblModalRequestId
        BindDetailsGrid
        openDetailsModal()
```

`gvRequests_RowDataBound` is a no-op; badges come from `GetStatusBadgeCss` in markup.

---

## 2. Request modal bind

```
BindDetailsGrid(reqId)
  ├─ SetCurrentUserApproverDetails
  ├─ usp_GetApprovalRequestDetails          ← all detail versions
  ├─ KeepLatestPerActivity                  ← UI grid is latest-only
  ├─ usp_GetApprovalRequest                 ← header + MaxApproverLevel
  ├─ dashboard SQL (latest MAX(Id) only)    ← footer Original / Deduction / Approved
  ├─ count enabled day-action buttons
  ├─ unprocessed receipt count (latest join, ClaimStatus not Approved/Rejected)
  ├─ GetReturnedItemCount (latest days + expenses)
  ├─ SetFinalApproverFlag (current == max)
  └─ button visibility
        Auditor:  btnMarkReadyForPayment = canFinalize
                  btnRejectDaysSelected  = pendingActionExists
                  btnReturn / btnReturnToEmployee / btnForwardLevel / btnRejectSelected = off
        Manager:  inverse of the above
```

`canFinalize` = no enabled day actions AND unprocessed receipts = 0 AND returned items = 0.

---

## 3. Day-level actions

### 3.1 Grid row

```
gvDetails_RowDataBound
  ├─ accumulate footer totals
  ├─ version badge if DetailVersion > 1
  ├─ Rejected → table-danger, actions off
  ├─ Returned → ApplyReturnedDayRowState
  ├─ Approved at current level → "✔ Done"
  ├─ Approved at prior level OR Pending → ApplyGatekeeperLogic
  │     Approve disabled while receipts Pending/Returned
  │     Return enabled only if !Auditor
  │     Reject enabled only if Auditor
  └─ Auditor + pending receipts → pulse-warning on lnkReview

gvDetails_RowCommand
  ApproveRow | ReturnRow | RejectDayRow
    ├─ if Approve and ShouldConfirmForwardToNextApprover
    │     (current < max AND this approve would clear last unresolved latest day)
    │     showForwardNextApproverModal → btnConfirmForward_Click
    └─ ProcessDayDetailAction
    └─ ExecuteRecalculationSQL
    └─ BindDetailsGrid + BindRequestsGrid
```

For Auditor, `NextApproverExists` is false (`current == max`), so the Forward-next confirm is not used. Last-day approve proceeds immediately.

### 3.2 Unified Day modal (AJAX)

```
lnkReview (OnClientClick=false)
  → open unifiedDayModal
       renderUnifiedExpensesModal(..., isAuditor)
       btnUnifiedApproveDay → actionDayFromModal('APPROVE')
       btnUnifiedRejectDay  → actionDayFromModal('REJECT')   [Auditor]
       btnUnifiedReturnDay  → hidden                         [Auditor]
  → ActionDayRowSilent(detailId, reqId, action, forwardConfirmed)
       Auditor RETURN refused
       Manager REJECT refused
       APPROVE → ExecuteProcessApprovalAction
       REJECT  → ExecuteProcessSalesApprovalRequestDetail + MarkActivityRejectedForDay
```

### 3.3 Shared processor

```
ProcessDayDetailAction(detailId, reqId, action, remark)
  Auditor RETURN → throw
  Manager REJECT → throw
  Returned day APPROVE → throw
  Duplicate RETURN → no-op (postback still shows success; AJAX returns success:false)
  APPROVE / RETURN → usp_ProcessApprovalAction
  REJECT           → usp_ProcessSalesApprovalRequestDetail
                     MarkActivityRejectedForDay (inline SQL on Activity + expenses)
```

---

## 4. Bulk actions

```
btnApproveSelected_Click  → ProcessBulkAction(Approve)
btnReturnSelected_Click   → ProcessBulkAction(Return)     [Auditor early-exit]
btnRejectDaysSelected_Click → ProcessBulkAction(Reject)   [manager early-exit]

ProcessBulkAction
  collect checked gvDetails DataKeys (ApprovalRequestDetailId)
  LoadBulkDetailContextsFromDatabase
        usp_GetApprovalRequestDetails + KeepLatestPerActivity
        non-latest selected IDs → IsLatestDetailVersion = false → skip "Already processed"
  TryEvaluateBulkEligibility
        latest + actionable at current level
        Approve blocked by pending/returned/manual receipts
  optional Forward-next confirm (not for Auditor)
  transaction: ProcessDayDetailAction per row
  ExecuteRecalculationSQL
  BindDetailsGrid + BindRequestsGrid
```

---

## 5. Expense / receipt / KM

```
updateExpense(expenseId, APPROVE|REJECT|RETURN)
  → UpdateIndividualExpense WebMethod
       no Auditor/Manager branch in C#
       maps to ClaimStatuses.Approved | Rejected | Returned
       usp_UpdateSalesDailyExpenseStatus
            updates SalesDailyExpense by ExpenseId
            snapshot rewrite on latest DetailVersion/Id for that ActivityId
            header TotalClaimAmount / TotalExpenseAmount from latest snapshots

UpdateActivityKM
  → usp_UpdateActivityKM_WithHistory

GetKMHistory / GetDeductionBreakdown
  → read-only AJAX for unified modal / deduction popup
```

Unified expense cards: Auditor Pending = Approve + Reject. Auditor Approved = optional Reject. No Return button is rendered for expenses.

---

## 6. Routing / finalize

```
btnForwardLevel_Click                         [manager]
  Auditor → throw "Auditors forward claims only via final payment processing."
  EnsureCanFinalizeRequest
  ExecuteRecalculationSQL
  usp_ForwardApprovalRequest
  CloseDetailsAfterSuccessfulForward          [RC-013]

btnMarkReadyForPayment_Click                  [Auditor]
  !Auditor → throw
  EnsureCanFinalizeRequest
  ExecuteRecalculationSQL
  usp_MarkRequestReadyForPayment
  BindDetailsGrid (modal stays open; main grid is not rebound here)

btnReturnToEmployee_Click                     [manager]
  usp_ReturnClaimToEmployee
  close modal + BindRequestsGrid

btnRecalculate_Click / btnRefreshCalculations_Click
  ExecuteRecalculationSQL
```

`EnsureCanFinalizeRequest` counts latest details whose status is not Approved/Rejected plus latest-joined expenses whose `ClaimStatus` is not Approved/Rejected.

---

## 7. Employee submit / resubmit (upstream of Auditor)

```
usp_SubmitSalesDailyActivities
  NEW:
    insert SalesApprovalRequest Status=Pending
    insert approvers L1 / optional L2 / optional L3 / Auditor (level 2, 3, or 4)
    CurrentApprover = L1
  RESUBMIT:
    only Returned activities allowed
    header Status=Pending, CurrentApprover reset to L1 level 1
    all SalesApprovalRequestApprover rows reset to Pending
    insert new SalesApprovalRequestDetail snapshot (DetailVersion+1, PreviousDetailId, DetailStatus=Pending)
    expenses Draft/Returned → Submitted
```

Resubmit always restarts at L1, including after an Auditor-era return of a day (which Auditor UI currently blocks at day level).

---

## 8. Approved-claims view (downstream, not the queue)

```
ApprovedClaimsView.Page_Load
  RequirePermission UI.SO_TaDa_ViewApproved.View
  BindRequestsGrid (separate query, not the pending-approver SP)
BindDetailsGrid
  usp_GetApprovalRequestDetails
  KeepLatestPerActivity
  same latest-only dashboard SQL pattern as ClaimsApproval_v2
```

No Approve/Return/Reject/Ready-for-Payment actions on this page.

---

## 9. SP call inventory from the Auditor screen

| Call site | Stored procedure |
| --- | --- |
| `BindRequestsGrid` | `usp_GetPendingApprovalsForUser_Filters` |
| `BindDetailsGrid` | `usp_GetApprovalRequestDetails`, `usp_GetApprovalRequest` |
| `LoadBulkDetailContextsFromDatabase` | `usp_GetApprovalRequestDetails` |
| `ProcessDayDetailAction` APPROVE/RETURN | `usp_ProcessApprovalAction` |
| `ProcessDayDetailAction` REJECT | `usp_ProcessSalesApprovalRequestDetail` |
| `btnForwardLevel_Click` | `usp_ForwardApprovalRequest` |
| `btnMarkReadyForPayment_Click` | `usp_MarkRequestReadyForPayment` |
| `btnReturnToEmployee_Click` | `usp_ReturnClaimToEmployee` |
| `UpdateIndividualExpense` | `usp_UpdateSalesDailyExpenseStatus` |
| `UpdateActivityKM` | `usp_UpdateActivityKM_WithHistory` |
| `TryGetApproverLevelBounds` | `usp_GetApprovalRequest` |

Inline SQL (not SPs): `SetCurrentUserApproverDetails`, `GetLatestDetailStatus`, `GetLatestUnresolvedDetailCount`, `GetReturnedItemCount`, `EnsureCanFinalizeRequest`, receipt-pending count, dashboard totals, `ExecuteRecalculationSQL`, `MarkActivityRejectedForDay`, `AppendUserToRemark` name lookup.
