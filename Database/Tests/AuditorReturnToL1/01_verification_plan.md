# Auditor to L1 routing — verification plan

Status: **NOT RUN.** This plan does not authorize a database write, a deployment, or any change to approval request 118.

The implementation pass did not execute `dbo.usp_ReturnApprovalRequestToL1`, did not deploy it, and did not update request 118.

`Database/Tests/AuditorReturnToL1/02_readonly_verification.sql` is a read-only script. It returns immediately unless the reviewer sets `@RunReadOnlyChecks = 1` in that file. It does not call the new procedure.

## Scope that must stay true

`dbo.usp_ReturnApprovalRequestToL1` only reroutes a pending level-2 request:

- Header status stays `Pending`.
- `CurrentApproverLoginId` becomes the assigned L1 login and `CurrentApproverLevel` becomes 1.
- The single Approved level-1 `L1` row becomes `Pending`, and `ActionBy`, `ActionDate`, and `Remarks` are cleared.
- The level-2 `Auditor` row stays `Pending` and is not updated.
- Exactly one `dbo.ApprovalRequestStatusHistory` row is inserted: `Pending` to `Pending`, `ChangedBy` the Auditor, remarks starting with `Auditor returned to L1 for correction:`.
- Activities, detail versions, expenses, claim totals, approved totals, and `FinalApprovedAmount` are not written.
- The procedure does not call recalculation, submission, employee return, reopen, day action, or expense-status procedures.

The page action is separate from expense rejection, day rejection, and the existing L1 whole-claim return. `btnReturnToEmployee` remains limited to the current level-1 approver. `btnReturnToL1` is rendered only when the session login is the assigned Auditor on a `Pending` request at level 2, with one Approved L1 row. L1, the employee, an unassigned Auditor, and AuditBDO do not see it unless that same assignment check identifies them as the assigned Auditor. No client login or role is sent to the procedure.

## Matrix

| # | Case | Expected result | What must not change |
| --- | --- | --- | --- |
| 1 | Assigned Auditor, pending level 2, non-empty reason within the history limit | Header stays `Pending`. Current approver is the L1 login at level 1. L1 row is `Pending` with action fields cleared. Auditor row stays `Pending`. One new history row uses the prefix. | Every amount column, activity status, detail id, detail version, detail status, expense amount, and expense status |
| 2 | Same Auditor repeats the action, or submits a stale page | Procedure raises. No second history row. State remains the post-case-1 state. | History count and the case-1 checksum |
| 3 | L1 login, employee login, an Auditor who is not the current level-2 assignee, or AuditBDO when that login is not the assigned Auditor | Procedure raises `You are not the current assigned Auditor for this pending level 2 approval request.` Page hides the button for those sessions. | Header, approver rows, history, amounts, expenses |
| 4 | Header is `Completed`, `ReadyForPayment`, or `Returned` | Procedure raises `This approval request is not currently awaiting approval and cannot be returned to L1.` | Those terminal or returned rows |
| 5 | After case 1, L1 returns the 01-Sep day through the existing day-return action | Only 01-Sep and its eligible expenses follow the existing day-return rules below. The other days stay as they were. | The other 29 days' activities, details, and expenses |
| 6 | Employee replaces the 01-Sep stationery attachment and resubmits the returned day | `ApprovalRequestId` stays 118. That activity gains the next `DetailVersion`. Header is `Pending` at L1. Approver rows restart at `Pending`. | A new request id must not be created |
| 7 | Resubmission totals for `Rejected` expenses | Existing submission behavior, documented below. Not changed by this feature. | No new aggregation rule in v1 |

Reason limit: `@ReturnReason` is `nvarchar(1000)`, the same parameter limit as `dbo.usp_ReturnClaimToEmployee`. History `Remarks` is also `nvarchar(1000)`, so the procedure rejects a reason that does not fit after the prefix `Auditor returned to L1 for correction: ` (the prefix includes the trailing space).

## Case 5 uses the existing day-return rules

After the routing action, L1 is the current approver. Returning 01-Sep calls the existing `dbo.usp_ProcessApprovalAction` with `RETURN`. That procedure, not the new routing procedure, does all of the following:

- The latest detail for that activity becomes `Returned`.
- That activity's `ClaimStatus` becomes `Returned`.
- Active expenses on that activity whose `ClaimStatus` is not `Rejected` become `Returned`. `Rejected` expenses and soft-deleted expenses on that day stay as they are.
- Other activities are not in that update. Their details, claim statuses, and expenses stay unchanged.
- The existing day-return also sets the request header to `Returned` and the current L1 approver row to `Returned`. That header effect is current day-return behavior. It is not produced by `dbo.usp_ReturnApprovalRequestToL1`.

A passing case 5 shows one returned day, eligible expenses on that day returned, rejected expenses on that day still rejected, and the other 29 days unchanged.

## Case 6 ownership

`dbo.usp_SubmitSalesDailyActivities` resubmission keeps the active request id. The new snapshot uses `ISNULL(previous DetailVersion, 0) + 1` and stores the previous `ApprovalRequestDetailId` in `PreviousDetailId`. The header current approver is reset to L1 at level 1, and existing approver rows are set back to `Pending`.

The stationery row is `ExpenseType = N'Stationary'`. Compare `AttachmentFileName`, `AttachmentPath`, and `AttachmentContent` on that expense with the baseline captured before the employee replaces the file.

## Case 7 — existing limitation, unchanged in v1

`dbo.usp_SubmitSalesDailyActivities` builds the resubmission snapshot from active expenses with `IsDeleted = 0` and no `ClaimStatus` predicate. A `Rejected` expense amount is included in `Snapshot_DAAmount`, `Snapshot_HotelAmount`, `Snapshot_TravelAmount`, `Snapshot_StationaryAmount`, and `Snapshot_ExpenseTotal` on the new detail version.

`dbo.usp_UpdateSalesDailyExpenseStatus` uses a different snapshot predicate and excludes `Rejected` rows. This routing feature does not change either procedure. v1 accepts the submission aggregate as it exists today.

`TotalExpenseEntries` on resubmission counts active expenses on the latest detail activities and also does not exclude `Rejected`. That count is unchanged by this feature.

## Reviewer sequence

Do not start this sequence until deployment of the new procedure and use of request 118 are both explicitly authorized. Run it on the authorized non-live database only.

1. Arm `02_readonly_verification.sql` and save the baseline result sets for request 118. Record `MAX(HistoryId)`.
2. Deploy only `dbo.usp_ReturnApprovalRequestToL1`. Confirm the definition check in the read-only script: no `EXEC` / `EXECUTE`, no detail, activity, or expense table names, and no `Reopened` token.
3. As the assigned Auditor, open request 118 and submit **Return to L1 for Correction** once, with a reason. Refresh and compare with case 1.
4. Submit the same action again from the Auditor. Expect the safe failure in case 2 and the same history count.
5. Sign in as L1, the employee, another Auditor, and AuditBDO. The button is absent. A direct call with those logins fails and leaves the checksum unchanged.
6. Against separate fixtures whose headers are `Completed`, `ReadyForPayment`, and `Returned`, the call fails and those fixtures match their baselines. Do not move request 118 into those statuses for this test.
7. As the assigned L1, return only 01-Sep. Compare case 5.
8. As the employee, replace the 01-Sep stationery attachment and resubmit. Compare case 6 and record the case 7 aggregate.

## Failure rule

Stop if any amount, expense status, or detail version changes during case 1, if a second history row appears during case 2, or if request 118 is replaced by a new `ApprovalRequestId` during case 6. Do not repair that state with an ad hoc update.
