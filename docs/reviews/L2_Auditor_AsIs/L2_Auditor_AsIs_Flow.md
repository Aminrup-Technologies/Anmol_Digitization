# L2 / Auditor As-Is Flow Report

**Phase:** RC-014 Phase 1 (read-only)  
**Captured:** 2026-09-15  
**Sources:** repository Web Forms + SQL Server MCP `anmol_uat` → `AnmolDristiDev`  
**Scope:** L1 Forward → Auditor queue → day/expense action → final close  
**Constraint:** No repository, SQL, IIS, or data changes were made during this capture.

---

## 1. Auditor entry point

| Item | Evidence |
| --- | --- |
| Page | `AnmolDristi/ClaimsApproval_v2.aspx` |
| Code-behind | `AnmolDristi/ClaimsApproval_v2.aspx.cs` |
| Menu | `Dristi.Master` → `SO_TaDa_Approve` → `~/ClaimsApproval_v2.aspx` (“View Pending”) |
| Auth | `Page_Load` redirects to `~/Login.aspx` when `Session["USERID"]` is null. Actor identity for SPs is `Session["WORKMAN"]` / `Session["workman"]`. |
| Role detection | `SetCurrentUserApproverDetails`: `SalesApprovalRequestApprover` row where `ApproverLoginId = current user` and `Status = 'Pending'`. `ApproverRole = 'Auditor'` sets `hfIsAuditor = 1` and `ViewState["CurrentUserRole"]`. |
| Queue SP | `dbo.usp_GetPendingApprovalsForUser_Filters` |
| Detail SP | `dbo.usp_GetApprovalRequestDetails` then `SalesApprovalDetailVersionHelper.KeepLatestPerActivity` |
| Header SP | `dbo.usp_GetApprovalRequest` |

The same page serves L1/managers and Auditor. Auditor behavior is a role overlay on that page, not a separate screen.

Post-finalization viewing is a different page: `ApprovedClaimsView.aspx` (`RequirePermission("UI.SO_TaDa_ViewApproved.View")`). It is read-only against the same detail SP + latest-version helper. It is not the Auditor action queue.

---

## 2. End-to-end As-Is path (Employee → L1 → Auditor)

```
Employee Submit
    usp_SubmitSalesDailyActivities  (NEW | RESUBMIT)
        header Status = Pending
        CurrentApproverLoginId = @L1Code, CurrentApproverLevel = 1
        Approver chain: L1 (required) + optional L2/L3 + Auditor (required, last level)
        inserts latest SalesApprovalRequestDetail snapshot (DetailVersion + 1, DetailStatus = Pending)
        expenses Draft/Returned → Submitted
        ↓
L1 queue
    ClaimsApproval_v2.Page_Load → BindRequestsGrid
        usp_GetPendingApprovalsForUser_Filters
            sar.Status = Pending
            sar.CurrentApproverLoginId = @ApproverLoginId
            saa.Status = Pending AND saa.ApproverLoginId = @ApproverLoginId
        ↓
L1 View Request
    gvRequests_RowCommand "View"
        SetCurrentUserApproverDetails
        ShowDetailsModal → BindDetailsGrid
        ↓
L1 Approve / Return / expense review
    Day Approve/Return → usp_ProcessApprovalAction
    Expense Approve/Reject/Return → usp_UpdateSalesDailyExpenseStatus
    Whole-claim Return → usp_ReturnClaimToEmployee (manager only)
        ↓
L1 Forward (explicit UI) when all latest days/expenses are Approved or Rejected
    btnForwardLevel_Click
        EnsureCanFinalizeRequest
        ExecuteRecalculationSQL
        usp_ForwardApprovalRequest
            Manager-only (Auditor role is refused)
            Completeness on latest DetailVersion then latest ApprovalRequestDetailId
            Moves CurrentApproverLoginId / Level to next required Pending approver
            Header Status remains Pending
            Writes ApprovalRequestStatusHistory ("Level N completed. Forwarded to Auditor (...)")
        RC-013: CloseDetailsAfterSuccessfulForward closes modal + rebinds queue
        ↓
Auditor queue (same page, different login)
    usp_GetPendingApprovalsForUser_Filters now returns the request because
    CurrentApproverLoginId = Auditor and ApproverRole row is Pending
        ↓
Auditor View Request
    BindDetailsGrid
        KeepLatestPerActivity
        Days previously Approved at L1 appear as Approved with DetailApprovedLevel < current
        Gatekeeper re-enables Approve (and Auditor Reject) for those rows
        Return buttons hidden for Auditor
        Reject Selected shown; Return Selected hidden
        Ready for Payment shown only when no pending day actions, no unprocessed receipts, no Returned items
        ↓
Auditor expense / receipt review (Unified Day modal)
    lnkReview (client-only) → unifiedDayModal
    Expense Approve / Auditor Reject → UpdateIndividualExpense → usp_UpdateSalesDailyExpenseStatus
        ↓
Auditor Approve Day (grid, bulk, or unified modal)
    ProcessDayDetailAction / ActionDayRowSilent
        APPROVE → usp_ProcessApprovalAction
        REJECT → usp_ProcessSalesApprovalRequestDetail + MarkActivityRejectedForDay
    Recalc: ExecuteRecalculationSQL (latest MAX(ApprovalRequestDetailId) only)
        ↓
Last pending day at Auditor
    UAT usp_ProcessApprovalAction: no next required approver AND role = Auditor
        → header Status remains Pending
        → does not write Completed
        → does not write ReadyForPayment
        ↓
Auditor Finalize (UI: ✔ Ready for Payment)
    btnMarkReadyForPayment_Click
        EnsureCanFinalizeRequest
        ExecuteRecalculationSQL
        usp_MarkRequestReadyForPayment
            Requires header Status = 'Completed'
            Then sets Status = 'ReadyForPayment'
```

---

## 3. Auditor-specific UI contract (from markup + code-behind)

Landing page (`ClaimsApproval_v2.aspx`):

| Control | Behavior |
| --- | --- |
| Prev / Current / Next Month | Shifts `ddlYear` / `ddlMonth`; default is previous calendar month |
| Year, Month, Employee search | Passed to `usp_GetPendingApprovalsForUser_Filters` (`@Year`, `@Month`, `@EmpCode`) |
| Search / Reset | Rebind / restore previous-month defaults |
| Counters | Pending / Returned / Completed / Ready For Payment counted from the **current queue result set**. Queue SP only returns `Status = 'Pending'`, so Returned/Completed/Ready chips stay 0 on this page unless a Pending row is mislabeled. |
| `gvRequests` | ID, employee, requested date, month, total claim, status badge, stage (`ApprovalProgress` + `PendingWithRole`), **View** |

Request modal (`#detailsModal`):

| Visible action | Auditor? | Handler |
| --- | --- | --- |
| Row Approve | Yes, if receipts resolved | `gvDetails_RowCommand` → `ApproveRow` |
| Row Return | Hidden (`btnReturn.Visible = !IsCurrentUserAuditor()`) | would be `ReturnRow` |
| Row Reject | Visible for Auditor | `RejectDayRow` |
| Review (folder) | Yes | client `lnkReview` → `#unifiedDayModal` |
| Approve Selected | Yes when pending rows exist | `btnApproveSelected_Click` |
| Return Selected (`btnRejectSelected`) | Hidden for Auditor | `btnReturnSelected_Click` |
| Reject Selected | Visible for Auditor | `btnRejectDaysSelected_Click` |
| Bulk remarks | Shown when pending rows exist | `txtBulkRemarks` |
| Re-Calculate | Yes | `btnRecalculate_Click` |
| ✖ Return (whole claim) | Hidden (`btnReturnToEmployee.Visible = isIntermediate`) | `usp_ReturnClaimToEmployee` |
| ✔ Forward | Hidden (`isIntermediate && canFinalize`) | `usp_ForwardApprovalRequest` |
| ✔ Ready for Payment | Shown when `isAuditor && canFinalize` | `usp_MarkRequestReadyForPayment` |
| Close (header ×) | Yes | Bootstrap `data-dismiss="modal"` |
| Footer totals | Original / Deductions / Net Approved + KM | `BindDetailsGrid` dashboard SQL |

Unified Day modal (`#unifiedDayModal`):

| Action | Auditor |
| --- | --- |
| Approve Day | Shown (`actionDayFromModal('APPROVE')` → `ActionDayRowSilent`) |
| Return Day | Hidden |
| Reject Day | Shown (`REJECT`) |
| KM edit | Shown when `canAction` (`UpdateActivityKM`) |
| Receipt Preview | If `AttachmentPath` present |
| Expense Approve | Pending expenses |
| Expense Reject | Auditor only (also offered on already-Approved expenses) |
| Expense Return | Not rendered in the Pending card for either role |
| Close | Footer close |

Remarks:

- Day Return/Reject require remarks in SP/C#. Auditor Reject uses row/bulk remarks.
- Unified-modal Approve/Reject stamps `"Approved via Unified Day Modal"` / `"Rejected via Unified Day Modal"` rather than the grid remark box.
- Return/Reject remarks are appended with `" - Added by {Name} ({Code})"` via `AppendUserToRemark`.

---

## 4. Browser correlation status

No existing UAT browser tab was present in this session. Navigating to `https://appsuat.anmolindustries.com/ClaimsApproval_v2.aspx` returned `chrome-error://chromewebdata/` (host not reachable from the integrated browser).

**UI inventory in this pack is therefore repository markup + code-behind, not a live Auditor screenshot.** Live confirmation of filters, queue contents, and modal chrome remains an observation gap for RC-014.

---

## 5. Helper classes used by the Auditor screen

| Type | Path | Auditor use |
| --- | --- | --- |
| `SalesApprovalDetailVersionHelper` | `AnmolDristi/SalesApprovalDetailVersionHelper.cs` | `KeepLatestPerActivity`, `LatestDetailWhereForD`, `LatestDetailJoinForD` |
| `ClaimStatuses` | `AnmolDristi/ClaimStatuses.cs` | Canonical status words including `Completed`, `ReadyForPayment` |
| `ClaimEditabilityHelper` | referenced from legacy `ClaimsApproval.aspx`; KM editability on v2 is client-gated by `hfIsAuditor` / `canAction` |  |

---

## 6. What “final close” means in As-Is UAT

Intended product language (UI + Forward SP comments): Auditor does **not** Forward. Auditor marks **Ready for Payment**.

Live UAT procedures as of 2026-09-15:

1. After the last Auditor day is resolved, `usp_ProcessApprovalAction` keeps header `Status = 'Pending'`.
2. `usp_ForwardApprovalRequest` refuses Auditor (`THROW 51012`).
3. `usp_MarkRequestReadyForPayment` refuses unless header `Status = 'Completed'` (`RAISERROR 'Request not completed.'`).

There is no live UAT procedure on this path that writes `Completed` for an Auditor-held request. The Ready-for-Payment button can become visible while the SP it calls cannot succeed. This is recorded in the risk register; no fix is proposed in this phase.
