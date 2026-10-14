# RC-014B UAT entry sequence — whole-claim return and same-request resubmission

Status: **NOT APPROVED FOR EXECUTION.** Behavioral steps below stay **NOT RUN** and, for the 2026-10-11 run, **BLOCKED**.
The sequence in this file does not record a PASS. The section "Preflight 2026-10-11" records a read-only look at candidate `ApprovalRequestId` 79. That section does not designate the fixture and does not authorize a write.

Existing files in this folder do not cover this sequence:

- `01_definition_shape_readonly.sql` checks `dbo.usp_ProcessApprovalAction` only. Its header says it was not executed.
- `02_behavior_tran_rollback.sql` probes `dbo.usp_ProcessApprovalAction` only. Its header says it was not executed and that G4–G7 are not authorized.
- `03_verify_return_catalog_readonly.sql` is the catalog script. The saved run `docs/reviews/L2_Auditor_AsIs/RC014B_catalog_readonly_2026-10-11T083600.4916002.txt` failed at compile time. That file was not modified.
- `docs/reviews/L2_Auditor_AsIs/RC014B_RC014D_UAT_Request97.md` records a different defect: Auditor approval reaching `Completed`. It is not evidence for this return and resubmission path.

No procedure was called while preparing this sequence. No application row was written.

## UAT entry gate

Behavioral execution stays stopped until every item below is closed by an explicit approval. Unresolved items are blockers.

| Gate | State | Evidence |
| --- | --- | --- |
| Catalog evidence for the six target tables | Recorded, incomplete as a catalog pass | Preflight in the 08:36 record, plus the read-only trigger query in this file. The 08:36 script output is four `Msg 207` errors and `sqlcmd` exit code 1. No verdict, index, or constraint result set was returned. |
| Trigger-definition query | Resolved for these six tables | Supported read-only query below. `sys.triggers.is_encrypted` and `sys.sql_modules.is_encrypted` are not columns on this instance. |
| Final uncommitted `usp_ReturnClaimToEmployee` diff | Not accepted | Working tree sets eligible current detail, activity, and active non-rejected expense statuses to `Returned`, then calls `dbo.usp_RecalculateApprovalRequest` in that same transaction. The object on `AnmolDristiDev` is still the 2026-06-15 11:10:29 definition: header `Rejected`, activities detached, expenses `Draft`. It does not call `dbo.usp_RecalculateApprovalRequest`. Deploy and execution of the working tree are not authorized. |
| Authorized UAT instance, separate from production | Identity confirmed, behavioral use not approved | Reader session: `MUMANMOAPP01\SQLEXPRESS`, database `AnmolDristiDev`, product version `15.0.2000.5`. This is the catalog target. This request does not approve behavioral writes there. |
| Disposable fixture and recovery plan | Missing | Read-only search on 2026-10-11 found no designated employee or month. `MST_UserMaster` has no name, code, or `CreationMode` containing test, uat, dummy, disposable, or sandbox. Status values present are Active, Inactive, Serving Notice Period, and Blocked. No `SalesApprovalRequest` remark designates a fixture. `sys.databases.source_database_id` is null for every database on this instance. `02_behavior_tran_rollback.sql` recovers one outer transaction around `dbo.usp_ProcessApprovalAction` only. |

### Catalog evidence already on file

Source: `docs/reviews/L2_Auditor_AsIs/RC014B_catalog_readonly_2026-10-11T083600.4916002.txt`.

Preflight, before the script batch failed:

| Table | Object id | VIEW DEFINITION |
| --- | ---: | ---: |
| `dbo.SalesApprovalRequestDetail` | 1108915022 | 1 |
| `dbo.SalesDailyActivity` | 580913141 | 1 |
| `dbo.SalesDailyExpense` | 1003866643 | 1 |
| `dbo.SalesApprovalRequestApprover` | 1188915307 | 1 |
| `dbo.SalesApprovalRequest` | 1556916618 | 1 |
| `dbo.ApprovalRequestStatusHistory` | 1348915877 | 1 |

Server `MUMANMOAPP01\SQLEXPRESS`, database `AnmolDristiDev`, product version `15.0.2000.5`. `dbo.usp_ReturnClaimToEmployee` was not called. That record is not a PASS.

### Trigger definitions, supported metadata

The 08:36 batch failed because `03_verify_return_catalog_readonly.sql` references `sys.triggers.is_encrypted`. A follow-up reference to `sys.sql_modules.is_encrypted` also failed with `Invalid column name 'is_encrypted'`.

The replacement query uses `sys.triggers` for row presence, `OBJECTPROPERTY(trigger_id, 'IsEncrypted')`, and `OBJECT_DEFINITION(trigger_id)`. It was run read-only on the UAT reader. It returned six rows. Object ids and `has_view_definition = 1` match the preflight.

| Table | Object id | Trigger name | `IsEncrypted` | Status |
| --- | ---: | --- | --- | --- |
| `dbo.ApprovalRequestStatusHistory` | 1348915877 | none | null | `NO TRIGGER ROWS FOUND` |
| `dbo.SalesApprovalRequest` | 1556916618 | none | null | `NO TRIGGER ROWS FOUND` |
| `dbo.SalesApprovalRequestApprover` | 1188915307 | none | null | `NO TRIGGER ROWS FOUND` |
| `dbo.SalesApprovalRequestDetail` | 1108915022 | none | null | `NO TRIGGER ROWS FOUND` |
| `dbo.SalesDailyActivity` | 580913141 | none | null | `NO TRIGGER ROWS FOUND` |
| `dbo.SalesDailyExpense` | 1003866643 | none | null | `NO TRIGGER ROWS FOUND` |

There is no trigger definition text to review because no trigger row exists. This does not query database DDL triggers or server triggers. It does not authorize execution of `dbo.usp_ReturnClaimToEmployee`.

### Diff review

Working tree versus `39a12b0a3dc9ebb9636e8b9fab62c20d3d9a235c` for `Database/Stored Procedures/Approval/usp_ReturnClaimToEmployee.sql`. Acceptance for UAT execution is not recorded. This section does not record a run.

Source inspection of the working tree, not a database execution: the procedure still keeps `ApprovalRequestId` and `CurrentApproverLoginId` / `CurrentApproverLevel`, selects latest rows with `MAX(ISNULL(DetailVersion, 1))` then `MAX(ApprovalRequestDetailId)`, requires exactly one matching pending L1 approver row, and rolls the open transaction back on failure. It sets the header and that L1 approver row to `Returned`. The reason is stored once, on that approver `Remarks` and on one history row `Pending` → `Returned`. It does not write `DetailRemarks` and it does not assign `SalesDailyExpense.Remarks` or `Amount`.

After the child status updates, and before commit, the procedure calls `dbo.usp_RecalculateApprovalRequest`. That procedure has no transaction of its own. `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql` was inspected and not changed. `ClaimsApproval_v2.aspx.cs` was inspected and not changed: `btnReturnToEmployee_Click` does not calculate amounts, and it does not call the recalculation again after the procedure commits. `dbo.usp_ProcessApprovalAction` was not changed.

Eligible current rows are those latest details whose `DetailStatus` is not `Rejected` and whose activity `ClaimStatus` is not `Rejected`, owned by this `ApprovalRequestId`. The procedure sets those `DetailStatus` and `SalesDailyActivity.ClaimStatus` values to `Returned`. On the same activities it sets `SalesDailyExpense.ClaimStatus` to `Returned` only where `IsDeleted = 0` and `ClaimStatus` is not `Rejected`. That expense predicate matches `dbo.usp_ProcessApprovalAction` for `RETURN`. `CK_SalesDailyExpense_NoUpdate_WhenDeleted` requires `ClaimStatus = 'Draft'` when `IsDeleted = 1`, so soft-deleted expense rows are not updated. `CK_SalesDailyActivity_ClaimStatus` allows `Returned`. Activity status `Pending` is not a legal activity value; a day awaiting L1 is stored as `Submitted`.

`dbo.usp_SubmitSalesDailyActivities`, `SalesMonthlyReview.aspx.cs`, `ClaimsApproval_v2.aspx.cs`, and `dbo.usp_ProcessApprovalAction` were inspected and not changed for this decision. `ClaimsApproval_v2.btnReturnToEmployee_Click` calls `dbo.usp_ReturnClaimToEmployee` with the posted request id and the session L1 user. Resubmission still uses `SubmissionMode = RESUBMIT` and the existing request id. `02_behavior_tran_rollback.sql` and `03_verify_return_catalog_readonly.sql` were not modified.

## Sequence

Every step below is **NOT RUN**. Do not fill request ids, statuses, versions, amounts, timestamps, or recovery results from source inspection. Capture the `ApprovalRequestId` returned by SQL Server in step 1 and use that value in every later read.

Latest-detail ownership for reads: `MAX(ISNULL(DetailVersion, 1))`, then `MAX(ApprovalRequestDetailId)` within that version. Submission inserts the next version with `ISNULL(previous DetailVersion, 0) + 1` and `PreviousDetailId` set to that prior latest id (`usp_SubmitSalesDailyActivities`).

A day-level `RETURN` in `dbo.usp_ProcessApprovalAction` sets the header to `Returned`. `dbo.usp_ReturnClaimToEmployee` then refuses the call because the header is no longer `Pending`. A current detail that is already `Returned` while the header is still `Pending` is not a state those two procedures produce together. The disposable fixture must record the starting statuses it actually has. Do not invent a `Returned` current line under a `Pending` header.

### 1. Record the original request id and current detail versions

- Evidence source: result set of `dbo.usp_SubmitSalesDailyActivities` (`Success`, `ApprovalRequestId`, `SubmissionMode`, `Message`), then `dbo.SalesApprovalRequestDetail` for that id. `SalesMonthlyReview.ExecuteSubmitForApproval` reads `ApprovalRequestId` from the second column.
- Expected state: `Success = 1`, `SubmissionMode = NEW`, header `Status = Pending`. Store the returned `ApprovalRequestId`. Store each activity's current `ApprovalRequestDetailId`, `DetailVersion`, `PreviousDetailId`, and `DetailStatus` before any return. Store the header's `ApprovedKM`, `ApprovedClaimAmount`, `ApprovedDAAmount`, `ApprovedHotelAmount`, `ApprovedTravelAmount`, and `FinalApprovedAmount`, and each current detail's snapshot amount columns, in the run record. Do not write those figures into this file before the run. If the approved fixture also contains older versions, store those ids and versions in the same image.
- Result: **NOT RUN.**

### 2. Whole-claim return by the authorized L1 approver

- Evidence source: header and the single level-1 approver row before the call, then one call to `dbo.usp_ReturnClaimToEmployee` with the id from step 1, `@ActionBy` equal to `CurrentApproverLoginId`, and the return reason written in the run notes.
- Expected state before the call: header `Status = Pending`, `CurrentApproverLevel = 1`, and exactly one approver row at level 1 with role `L1`, that same login, and `Status = Pending`.
- Expected state after the call: the procedure commits. Header `Status = Returned`. `ApprovalRequestId`, `CurrentApproverLevel`, and `CurrentApproverLoginId` are unchanged. The matching L1 approver row is `Returned`. The reason is on that approver `Remarks` and on one new history row `Pending` → `Returned`. No second history row is inserted for the same transition. The six approved-amount columns are the step 3 result, written before this commit. A second call with the same id fails and leaves the committed rows unchanged.
- Result: **NOT RUN.**

### 3. Approved amounts match `usp_RecalculateApprovalRequest`

- Evidence source: the six header columns stored in step 1, the same columns after the call in step 2, and the latest `dbo.SalesApprovalRequestDetail` rows for that id. Compare the after image with the rule below. Do not calculate the expected figures in C# or write sample amounts into this file.
- The return procedure calls `dbo.usp_RecalculateApprovalRequest` after eligible latest details, activities, and active non-rejected expenses are `Returned`, and before it commits. The comparison is that procedure's result.
- Latest-detail selection, used both for the snapshot refresh and for the header sum: `MAX(ISNULL(DetailVersion, 1))`, then `MAX(ApprovalRequestDetailId)`, limited to this `ApprovalRequestId`. Historical detail versions are not source rows.
- Before the header sum, every latest detail is refreshed, including a latest row whose `DetailStatus` is `Rejected` or `Returned`. `Snapshot_DAAmount`, `Snapshot_HotelAmount`, `Snapshot_TravelAmount`, and `Snapshot_StationaryAmount` become `ISNULL(SUM(dbo.SalesDailyExpense.Amount), 0)` for that `ActivityId` and expense type, where `IsDeleted = 0` and `ISNULL(ClaimStatus, '') <> 'Rejected'`. `Snapshot_ExpenseTotal` becomes `ISNULL(Snapshot_DAAmount, 0) + ISNULL(Snapshot_HotelAmount, 0) + ISNULL(Snapshot_TravelAmount, 0) + ISNULL(Snapshot_StationaryAmount, 0)`. `Snapshot_TotalDistanceKM` and `Snapshot_CalculatedClaimAmount` are not rewritten. There is no `ROUND` call. Results are assigned to `decimal(18,2)`.
- Header source rows are the latest details whose `ISNULL(DetailStatus, '')` is not `Rejected` and not `Returned`. There is no other status filter.
- Expected state after the call:
  - `ApprovedKM` = `ISNULL(SUM(Snapshot_TotalDistanceKM), 0)`
  - `ApprovedClaimAmount` = `ISNULL(SUM(Snapshot_CalculatedClaimAmount), 0)`
  - `ApprovedDAAmount` = `ISNULL(SUM(Snapshot_DAAmount), 0)`
  - `ApprovedHotelAmount` = `ISNULL(SUM(Snapshot_HotelAmount), 0)`
  - `ApprovedTravelAmount` = `ISNULL(SUM(Snapshot_TravelAmount), 0)`
  - `FinalApprovedAmount` = `ISNULL(SUM(ISNULL(Snapshot_CalculatedClaimAmount, 0) + ISNULL(Snapshot_ExpenseTotal, 0)), 0)`
- `SUM` over no qualifying rows is null, and `ISNULL` stores 0. Eligible latest details are `Returned`, so they are not in this sum. A latest detail that stays out of the return because its activity is `Rejected` remains in the sum when that detail's own status is not `Rejected` or `Returned`. `ApprovalRequestId`, `CurrentApproverLevel`, and `CurrentApproverLoginId` stay as step 2 requires. This procedure does not assign `TotalDeductionAmount`.
- Result: **NOT RUN.**

### 4. Eligible current details and activities are Returned

- Evidence source: pre-return and post-return images of latest `dbo.SalesApprovalRequestDetail` and `dbo.SalesDailyActivity` for the id from step 1.
- Expected state: every latest detail that was not `Rejected`, and whose activity was not `Rejected`, has `DetailStatus = Returned`. The matching activity has `ClaimStatus = Returned` and the same `ApprovalRequestId`. That includes current rows that started as detail `Approved` or `Pending`, and activities that started as `Approved`, `Submitted`, `Draft`, or `Returned` when the header was still `Pending`. A rejected latest detail keeps `DetailStatus = Rejected`. A rejected activity keeps `ClaimStatus = Rejected`. Expense snapshot columns on latest rows are the step 3 refresh, not a status-only comparison. No other request's rows change.
- Result: **NOT RUN.**

### 5. Historical detail versions stay unchanged

- Evidence source: detail rows for this `ApprovalRequestId` whose `ApprovalRequestDetailId` is not the current id recorded in step 1.
- Expected state: those rows keep their `DetailStatus`, `DetailVersion`, `PreviousDetailId`, `DetailRemarks`, and snapshot amount columns. `dbo.usp_RecalculateApprovalRequest` updates only the latest detail id per activity.
- Result: **NOT RUN.**

### 6. Rejected and soft-deleted expenses stay as they were

- Evidence source: pre-return and post-return `dbo.SalesDailyExpense` for activities on this request.
- Expected state: an expense with `ClaimStatus = Rejected` keeps that status, `IsDeleted`, `Amount`, and `Remarks`. An expense with `IsDeleted = 1` stays deleted, keeps `ClaimStatus = Draft` as required by `CK_SalesDailyExpense_NoUpdate_WhenDeleted`, and is not reactivated. Expenses on a rejected activity are unchanged. Expenses on a different request are unchanged.
- Result: **NOT RUN.**

### 7. Whole-claim return preserves expense amounts and remarks

- Evidence source: the same expense image as step 6, for active expenses on eligible activities.
- Expected state: `Amount` and `Remarks` equal the pre-return values. The return reason is not written into `SalesDailyExpense.Remarks` or into `DetailRemarks`. Active expenses on eligible activities whose `ClaimStatus` was not `Rejected` have `ClaimStatus = Returned`. `IsDeleted` stays `0` on those rows. `ClaimProcessedBy`, `ClaimProcessedDate`, `ModifiedBy`, and `ModifiedOn` may change. The return update does not assign expense `Amount`. Latest-detail expense snapshots follow step 3.
- Result: **NOT RUN.**

### 8. Employee correction on one eligible returned activity

- Evidence source: `SalesMonthlyReview.aspx.cs` paths only, after step 4 has left the header and that activity's latest detail `Returned` and the activity not `Rejected`. Capture the target activity's expenses and `TotalDistanceKM` before each action.
- Expected state, soft delete: the chosen active expense remains in `dbo.SalesDailyExpense`. `IsDeleted` becomes `1` and `ClaimStatus` becomes `Draft`. `Amount` and `Remarks` stay as they were. `DeletedBy`, `DeletedOn`, and `DeleteReason` may be set. No row is physically deleted.
- Expected state, add expense: `dbo.usp_SaveDailyExpense` inserts one new row for this `ActivityId` only, with `IsDeleted = 0`. No new `SalesApprovalRequest` row is inserted.
- Expected state, KM: `dbo.usp_UpdateActivityKM` changes `TotalDistanceKM` only on this `ActivityId`, and one `SalesDailyActivityKMHistory` row records that change.
- Expected state, other lines: rejected activities, their details, and their expenses stay unchanged by these three actions.
- Result: **NOT RUN.**

### 9. Soft-deleted expenses remain deleted and out of active totals

- Evidence source: `dbo.SalesDailyExpense` for the activity from step 8, including the row soft-deleted there and any expense that was already `IsDeleted = 1` before the return.
- Expected state: each of those rows is still present with `IsDeleted = 1`. Active totals used by the employee display and by the later submission snapshot count only `IsDeleted = 0`.
- Result: **NOT RUN.**

### 10. Resubmit reuses the original ApprovalRequestId

- Evidence source: result set of `dbo.usp_SubmitSalesDailyActivities` after the employee submits the corrected returned activities.
- Expected state: `Success = 1`, `SubmissionMode = RESUBMIT`, and `ApprovalRequestId` equals the id stored in step 1. No second `dbo.SalesApprovalRequest` row is inserted. The batch contains only activities whose `ClaimStatus` is `Returned`. A rejected activity is not in the batch.
- Result: **NOT RUN.**

### 11. Only eligible returned activities gain a new detail version

- Evidence source: `dbo.SalesApprovalRequestDetail` for the id from step 1, compared with the version image from step 1.
- Expected state: each activity whose latest detail was `Returned` before this submit has one new detail row. `DetailVersion = ISNULL(previous DetailVersion, 0) + 1`. `PreviousDetailId` equals the prior latest `ApprovalRequestDetailId`. The previous row is unchanged. A rejected activity does not gain a detail row. Activities outside this request do not gain a detail row.
- Result: **NOT RUN.**

### 12. Resubmitted activities are Pending for L1

- Evidence source: the new detail rows from step 11, `dbo.SalesDailyActivity` for those activities, and `dbo.SalesApprovalRequest` plus `dbo.SalesApprovalRequestApprover` for the id from step 1.
- Expected state: new `DetailStatus = Pending`. Those activities are no longer `Returned` (`usp_SubmitSalesDailyActivities` sets `ClaimStatus = Submitted` for the submitted batch). Header `Status = Pending`, `CurrentApproverLevel = 1`, and `CurrentApproverLoginId` is the L1 login passed into resubmission. Existing approver rows for this request are `Pending`, with `ActionBy`, `ActionDate`, and `Remarks` cleared.
- Result: **NOT RUN.**

### 13. SQL snapshot excludes soft-deleted expenses

- Evidence source: the new detail snapshot columns written by `dbo.usp_SubmitSalesDailyActivities`, and a read of `dbo.SalesDailyExpense` for the resubmitted activities where `IsDeleted = 0`, grouped by `ExpenseType`.
- Expected state: `Snapshot_DAAmount`, `Snapshot_HotelAmount`, `Snapshot_TravelAmount`, `Snapshot_StationaryAmount`, and `Snapshot_ExpenseTotal` equal those SQL sums. Rows with `IsDeleted = 1` contribute nothing. The comparison is the database sum, not a figure calculated in C#.
- Result: **NOT RUN.**

### 14. L1 to Auditor to Completed to ReadyForPayment

- Evidence source: `dbo.usp_ProcessApprovalAction` for each latest pending detail, first as the current L1 approver and then as the Auditor, followed by `ClaimsApproval_v2.EnsureHeaderCompletedForPayment` and `dbo.usp_MarkRequestReadyForPayment`. With L2 and L3 null at submission, Auditor is level 2.
- Expected state after every latest detail is resolved at L1: header `Status = Pending`, `CurrentApproverLevel` is the Auditor level, `CurrentApproverLoginId` is the Auditor login, the L1 approver row is `Approved`, and the Auditor approver row is `Pending`.
- Expected state after one Auditor approval while another latest detail is still unresolved: the acted latest detail is `Approved` at the Auditor level. Every latest detail that was not acted on remains `Pending`. Header `Status = Pending`. The Auditor approver row remains `Pending`. The header is not `Completed`.
- Expected state before Mark Ready, after the remaining latest details are approved: header `Pending`, Auditor approver row `Pending`, latest details `Approved` or `Rejected`.
- Expected state after `EnsureHeaderCompletedForPayment`: header `Completed`, and one history row `Pending` → `Completed` with remark `Auditor finalized all rows before Ready for Payment.`
- Expected state after `usp_MarkRequestReadyForPayment`: header `ReadyForPayment`, the Auditor approver row `Approved`, and one history row `Completed` → `ReadyForPayment`.
- Result: **NOT RUN.**

### 15. Execution record

- Evidence source: only rows, result sets, and recovery output observed during an authorized run.
- Expected state: this step stays empty until that run. It is the place to write the actual `ApprovalRequestId`, before/after statuses, detail versions, SQL results, timestamps, and recovery evidence.
- Result: **NOT RUN.** No request id, status, version, amount, timestamp, or recovery result is recorded in this file.

## Rollback scope of `02_behavior_tran_rollback.sql`

This script was inspected and not modified. It was not executed.

Its actual scope is one outer transaction, `RC014B_Outer`, around a single call to `dbo.usp_ProcessApprovalAction`. `@ReviewedBeforeExecution` defaults to 0, so the file returns before that call. On a reviewed run it rolls `RC014B_Outer` back and compares the captured request, detail, approver, history, one activity, and that activity's expenses with the pre-image. `ROLLBACK PROVED` applies only to that one procedure call, and only when an eligible expense update was observed inside the still-open outer transaction.

The script does not call `dbo.usp_SubmitSalesDailyActivities`, `dbo.usp_ReturnClaimToEmployee`, `dbo.usp_SaveDailyExpense`, `dbo.usp_UpdateActivityKM`, `dbo.usp_MarkRequestReadyForPayment`, or `ClaimsApproval_v2.EnsureHeaderCompletedForPayment`. Those procedures commit their own transactions. A rollback of `usp_ProcessApprovalAction` does not undo a whole-claim return, a resubmission, an expense soft-delete, a KM edit, or Mark Ready. It is not evidence that this sequence is recoverable.

## Test data and recovery required before any run

This section is not an approved plan. No fixture was created. Behavioral execution is blocked.

Repository search found no artifact that can recover this sequence:

| Artifact | Why it is not this sequence's recovery |
| --- | --- |
| `02_behavior_tran_rollback.sql` | One outer transaction around `dbo.usp_ProcessApprovalAction` only. It does not wrap the other procedures this sequence commits. |
| `dbo.usp_AdminRollbackApprovalRequest` | Deletes history, approver rows, and detail versions, then detaches activities. That destroys the historical evidence this UAT has to keep. |
| `Database/Scripts/031c_RC019_Rollback_Template.sql` and `docs/reviews/RC019/templates/RC019_Rollback_Register_Template.md` | Empty RC-019 skeleton. `@Confirm` defaults to 0. No claim tables are named. |
| `Database/Deployments/HRMS_PhaseB2_3_UAT_OrgRemediation/D_Rollback.sql` and `Database/Deployments/HRMS_PhaseB2_6_UAT_SentinelZeroRemediation/D_Rollback.sql` | HRMS deployment rollbacks. They do not touch this claim workflow. |

The database owner must provide all of the following before any behavioral write:

1. Explicit authorization to write on `MUMANMOAPP01\SQLEXPRESS` / `AnmolDristiDev`, and confirmation that this database is not production. `AnmolDristi` is also online on this instance. The database name is not that confirmation.
2. A separate authorization to deploy the working-tree `dbo.usp_ReturnClaimToEmployee` before step 2. The deployed procedure does not implement the approved whole-claim return.
3. A named disposable employee code and month whose loss is acceptable. None is designated in `MST_UserMaster` or in approval remarks.
4. A fixture image taken before the first write: at least two activities so step 14 can leave one latest detail unresolved; current details that start `Approved` and `Pending` while the header is still `Pending` at L1; a rejected latest detail and a rejected expense if those comparisons are in scope; one active expense with a known `Amount` and `Remarks`; one expense already `IsDeleted = 1`; and a KM value that can change on one eligible activity only. A current `Returned` detail under a `Pending` header is not produced by day-level return followed by this procedure. Do not fabricate that row.
5. A recovery method that, after each of these commits, restores the pre-image or removes only rows this run created:
   - `dbo.usp_SubmitSalesDailyActivities` commits the new or reused header, activity status and amounts, active expense status, detail versions, approver rows, and history.
   - `dbo.usp_ReturnClaimToEmployee` commits eligible latest-detail, activity, and active expense statuses, the `dbo.usp_RecalculateApprovalRequest` snapshot and six header amount columns, the L1 approver row, the header status, and one history row. The deployed recalculate procedure, modified 2026-09-15, has no transaction of its own.
   - Expense soft-delete commits `IsDeleted`, `ClaimStatus = Draft`, `DeletedBy`, `DeletedOn`, and `DeleteReason`. `dbo.usp_SaveDailyExpense` commits one inserted expense row. `dbo.usp_UpdateActivityKM` commits `TotalDistanceKM` and one `dbo.SalesDailyActivityKMHistory` row.
   - Resubmission commits the same request id, new detail versions, activity and expense status, approver reset, and history.
   - Each `dbo.usp_ProcessApprovalAction` call commits its own transaction. `EnsureHeaderCompletedForPayment` commits the header `Completed` update and one history row. `dbo.usp_MarkRequestReadyForPayment` has no explicit transaction, so its approver update, header update, and history insert can commit separately.

   Deleting history is not that method. `dbo.usp_AdminRollbackApprovalRequest` is not approved and does not restore KM history, soft-delete flags, inserted expenses, snapshot amounts, or pre-existing detail versions. An outer transaction around the whole sequence is not that method, because steps 4 through 14 read committed results. No snapshot database is available to restore.

Until those five items exist, do not call these procedures and do not write application data.

## Preflight 2026-10-11

Read-only. No procedure was executed. No application row was written. No portal session was opened. Recovery was not run.

`Database/Tests/RC014B/05_uat_fixture_discovery_readonly.txt` is not in the workspace. `Database/Tests/RC014B/05_uat_fixture_discovery_readonly.sql` is present, and its header says the script was not executed. There is no saved discovery result set to diff against this read. `ApprovalRequestId` 79 was re-queried because this run named it as a candidate.

### Environment

| Item | Evidence | Observed at |
| --- | --- | --- |
| Dev reader | Server `MUMANMOAPP01\SQLEXPRESS`, database `AnmolDristiDev`, product version `15.0.2000.5` | `2026-10-11T07:11:33.958Z` (`SYSDATETIMEOFFSET`) |
| Live reader | Same server, database `AnmolDristi`, same product version | Connection test in this run |
| Flamex UAT reader | Default connection not configured | Not used |
| Workspace `Web.config` `DbConn` | Catalog name `AnmolDristiDev` | File inspection. The saved SQL login was not used. The running site's configuration was not read. |
| Portal sign-in and Switch User | **NOT RUN** | No administrator session was opened |

Deployed `dbo.usp_ReturnClaimToEmployee`, retrieved with `OBJECT_DEFINITION`:

| Database | `modify_date` | Definition length | Retrieved at |
| --- | --- | ---: | --- |
| `AnmolDristiDev` | `2026-06-15T11:10:29.793Z` | 2577 | `2026-10-11T07:08:04.766Z` |
| `AnmolDristi` | `2026-03-06T15:33:41.347Z` | 2577 | `2026-10-11T07:11:37.355Z` |

Both retrieved definitions set every expense joined to a detail of the request to `Draft`, set those activities to `Draft` and `ApprovalRequestId` null, set the header to `Rejected` with `CurrentApproverLevel = 0` and `CurrentApproverLoginId` null, cancel pending approver rows, and insert history `Pending` → `Rejected`. Neither definition calls `dbo.usp_RecalculateApprovalRequest` or checks that `@ActionBy` is the pending L1 approver.

The working tree file `Database/Stored Procedures/Approval/usp_ReturnClaimToEmployee.sql` is uncommitted (`git diff --stat`: 378 lines changed in that file). It keeps `ApprovalRequestId` and the current approver pointer, requires one pending L1 row for the actor, sets eligible latest detail, activity, and active non-rejected expense statuses to `Returned`, then calls `dbo.usp_RecalculateApprovalRequest`. Two rows at the same latest version are resolved by `MAX(ApprovalRequestDetailId)`. That path does not raise an ambiguity error.

This run's acceptance text requires the header to become `Returned` and child activity, detail, and expense statuses to stay as they were. The deployed text and the working-tree text each do something else. The diff is not accepted. Nothing was deployed.

`AnmolDristi/ClaimsApproval_v2.aspx.cs` working tree: `btnReturnToEmployee_Click` checks the session user against the current L1 approver, then calls `dbo.usp_ReturnClaimToEmployee`. It does not calculate amounts. `AnmolDristi/SalesMonthlyReview.aspx.cs` working tree: when the month header is `Returned` or `Pending` and latest details are `Returned`, resubmission keeps that `ApprovalRequestId` and filters the batch to those returned activities. `CanEmployeeEditActivityExpenses` treats a day as eligible for the returned-header path only when the header and the latest detail are both `Returned` and the activity is not `Rejected`. Both C# files are uncommitted. They were inspected and not changed in this run. Switch User remains the allowlisted session switch in `SwitchUser.aspx.cs` and `SwitchUserHelper.cs`. The workspace allowlist was not treated as the running site's allowlist.

### Candidate 79 on `AnmolDristiDev`

| Field | Value |
| --- | --- |
| Requester | `AHS1121` |
| Period | 2026-06, activity dates `2026-06-01` through `2026-06-30` |
| Header status | `Pending` |
| Current approver | `AHS1624`, level 1 |
| Header `ModifiedBy` / `ModifiedOn` | `AHS1121` / `2026-07-30T09:47:48.828Z` |
| Approver `153` | `AHS1624`, role `L1`, level 1, `Pending` |
| Approver `154` | `Audit`, role `Auditor`, level 2, `Pending` |
| Details | 30 rows, 30 activities, `DetailVersion` 1 on every row, `PreviousDetailId` null on every row |
| Detail status | `Approved` 7, `Returned` 3, `Pending` 20, `Rejected` 0 |
| Returned current rows | Detail `2194` activity `19681`; detail `2196` activity `19683`; detail `2197` activity `19684` |
| Activities | All 30 have `Login_Id` `AHS1121` and `ApprovalRequestId` 79. `Returned` 3, `Submitted` 27. No activity is linked to another request. |
| History | 10 rows. Day actions are `RETURN_DAY` and `APPROVE_DAY` by `AHS1624`. Remark text was not copied. `HistoryId` 456 `ChangedOn` is `2026-07-30T14:55:46.454Z`. `HistoryId` 461 `ChangedOn` is `2026-07-30T09:44:36.464Z`. |
| KM history rows for these activities | 0 |
| `TotalDeductionAmount` | null |

The first detail and activity lists from the reader were capped at 20 rows. The counts above come from separate aggregate queries over all 30 rows. The 7 `Approved` details and 3 `Returned` details are inside activity ids `19680`–`19699`.

Stored header amounts, and the SQL sum of the 27 latest details whose `DetailStatus` is not `Rejected` or `Returned`:

| Column | Stored | SQL sum of those 27 details |
| --- | ---: | ---: |
| `ApprovedKM` | 577.45 | 577.45 |
| `ApprovedClaimAmount` | 2165.45 | 2165.45 |
| `ApprovedDAAmount` | 1675 | 1675 |
| `ApprovedHotelAmount` | 580 | 580 |
| `ApprovedTravelAmount` | 900 | 900 |
| `FinalApprovedAmount` | null | 6520.45 |
| Stationary snapshot sum | no header column | 1200 |
| Expense snapshot sum | no header column | 4355 |

Expenses on these activities: 25 rows. `Approved` 3 rows, amount sum 1075, 2 with remarks. `Draft` 8 rows, amount sum 3015, remarks absent. `Returned` 4 rows, amount sum 910, remarks absent. `Submitted` 10 rows, amount sum 3280, remarks absent. Remark text was not copied. Soft-delete flags were not returned: the reader rejected a query that named the deleted-flag column. The 8 `Draft` rows are therefore not classified as soft-deleted.

### Same id on `AnmolDristi`

`ApprovalRequestId` 79 also exists on `AnmolDristi`. It is a different claim: requester `AHS750`, year 2026 month 3, status `ReadyForPayment`, current approver `Audit` level 2, `ApprovedKM` 1911.39, `ApprovedClaimAmount` 5256.33, `ApprovedDAAmount` 4400, hotel 0, travel 0, `FinalApprovedAmount` null. A write aimed at the wrong database would hit that claim.

### Reconciliation with the sequence above

The sequence text says a current `Returned` detail under a `Pending` header is not a state produced by day-level return followed by whole-claim return. This read shows that combination on `AnmolDristiDev`: header `Pending`, three current details and three activities `Returned`, and `RETURN_DAY` history. The claim also has seven `Approved` details whose activities are still `Submitted`. It is an in-progress L1 claim, not a recorded disposable fixture.

### This run's step record

| Step | Status | Evidence |
| --- | --- | --- |
| Record environment, deployed procedure text, and candidate 79 | Read-only preflight recorded | This section. Server time `2026-10-11T07:11:33.958Z`. |
| Existing sequence steps 1–15 | **BLOCKED** | Owner designation, accepted procedure text, and a validated recovery plan are absent. |
| Administrator sign-in | **NOT RUN** | No portal session. |
| Switch User to L1 `AHS1624` | **BLOCKED** | Write gates are open. |
| Whole-claim return | **BLOCKED** | Deployed procedure is the `Rejected` / detach definition. Working tree is not accepted and was not deployed. |
| Header, child status, expense, resubmit, financial, and history checks | **BLOCKED** | They follow the return. |
| Recovery and post-recovery compare | **NOT RUN** | No approved recovery procedure. Proposal: `Database/Tests/RC014B/06_recovery_design_proposal.md`. |

Business data changed in this run: none. Recovery verified: no.

### Browser and running-site check 2026-10-11

Read-only. The login page was opened. No credentials were entered. Switch User was not opened. No procedure was executed. No application row was written. `Database/Tests/RC014B/06_recovery_design_proposal.md` was read again and left as **PROPOSAL ONLY**. Recovery was not run.

#### Browser

| Item | Result |
| --- | --- |
| URL | `http://localhost:53713/login.aspx` |
| Status | **PASS** — page loaded |
| Title | `Apps - Anmol Industries` |
| Form | Login id, password, Login, and Forgot Password were visible |
| IIS Express log | Process `41024` recorded `GET /login.aspx` ending HTTP 200 |
| Missing-page probe | `GET /__rc014b_path_probe_missing.aspx` ended HTTP 404, title `The resource cannot be found.` |

IIS Express process `41024` started `2026-10-11 12:45:29` local time. Its parent is `devenv.exe`. The process command line was not readable. Its log says it registered `http://localhost:53713/` for site `AnmolDristi`.

#### Database the running site opened

Two workspace `applicationhost.config` files bind port `53713`. One site root, `D:\AIL_AminrupTech\AnmolDristi`, is not on this machine. The other site root is `D:\PersonalRnD\AminrupWorks\Anmol_Digitization\AnmolDristi`, and the logo the login page requested is in that folder.

The active `DbConn` in that site's `Web.config` was opened for one read of server identity. The connection string, login, and password were not written here. Observed at `2026-10-11T07:27:32.8274464Z`:

| Item | Value |
| --- | --- |
| Server | `MUMANMOAPP01\SQLEXPRESS` |
| Catalog | `AnmolDristiDev` |
| Product version | `15.0.2000.5` |
| Session port | `1433` |

The UAT reader session at `2026-10-11T07:22:46.946Z` returned the same server name, catalog, and product version. `AnmolDristi` is `ONLINE` on the same instance and was not the catalog that application connection opened. The live reader still shows `ApprovalRequestId` 79 as requester `AHS750`, year 2026 month 3, status `ReadyForPayment`, current approver `Audit` level 2. That catalog was not updated.

`source_database_id` is null for `AnmolDristi`, `AnmolDristiDev`, `bookings`, `webface`, `WEBTOS`, and `webtos_new`. No snapshot database is attached.

#### Candidate 79 re-read on `AnmolDristiDev`

The compared fields still match the earlier preflight in this section. `ModifiedOn` is the same instant, now shown with more fractional digits: `2026-07-30T09:47:48.8282251`.

| Field | Value |
| --- | --- |
| Requester | `AHS1121` |
| Period | 2026-06, activity dates `2026-06-01` through `2026-06-30` |
| Header status | `Pending` |
| Current approver | `AHS1624`, level 1 |
| Header `ModifiedBy` | `AHS1121` |
| Approver `153` | `AHS1624`, role `L1`, level 1, `Pending` |
| Approver `154` | `Audit`, role `Auditor`, level 2, `Pending` |
| Latest details | 30 rows, version 1, `PreviousDetailId` null. `Approved`/`Submitted` 7, `Pending`/`Submitted` 20, `Returned`/`Returned` 3, `Rejected` 0 |
| Returned current rows | Detail `2194` activity `19681`; detail `2196` activity `19683`; detail `2197` activity `19684` |
| Activities | 30, all `Login_Id` `AHS1121`, all `ApprovalRequestId` 79 |
| History | 10 rows. `HistoryId` 456 is `Pending` by `AHS1121` at `2026-07-30T14:55:46.4545293`. `HistoryId` 461 is `RETURN_DAY`, `Pending` → `Returned`, by `AHS1624` at `2026-07-30T09:44:36.4648544`. `RETURN_DAY` covers history ids 461–465 (3 rows). `APPROVE_DAY` covers 462–469 (6 rows). Remark text was not copied. |
| KM history rows | 0 |
| `TotalDeductionAmount` | null |

Latest-detail ownership used `MAX(ISNULL(DetailVersion, 1))` and then `MAX(ApprovalRequestDetailId)`. Stored header amounts, and the SQL sum of the 27 latest details whose `DetailStatus` is not `Rejected` or `Returned`:

| Column | Stored | SQL sum of those 27 details |
| --- | ---: | ---: |
| `ApprovedKM` | 577.45 | 577.45 |
| `ApprovedClaimAmount` | 2165.45 | 2165.45 |
| `ApprovedDAAmount` | 1675 | 1675 |
| `ApprovedHotelAmount` | 580 | 580 |
| `ApprovedTravelAmount` | 900 | 900 |
| `FinalApprovedAmount` | null | 6520.45 |
| Stationary snapshot sum | no header column | 1200 |
| Expense snapshot sum | no header column | 4355 |

Expenses on these activities: 25 rows. `Approved` 3 rows, amount sum 1075, 2 with remarks. `Draft` 8 rows, amount sum 3015, remarks absent. `Returned` 4 rows, amount sum 910, remarks absent. `Submitted` 10 rows, amount sum 3280, remarks absent. Remark text was not copied. The soft-delete flag was not returned: the reader rejected the query that named that column.

This claim is still an in-progress L1 claim. It is not a designated disposable fixture.

#### Deployed procedure and recovery gate

`dbo.usp_ReturnClaimToEmployee` on `AnmolDristiDev` was read again with `OBJECT_DEFINITION` at `2026-10-11T07:25:48.725Z`. `modify_date` is still `2026-06-15T11:10:29.793Z`. Definition length is still 2577. It still sets joined expenses to `Draft`, sets those activities to `Draft` with `ApprovalRequestId` null, sets the header to `Rejected` with `CurrentApproverLevel = 0` and `CurrentApproverLoginId` null, cancels pending approver rows, and inserts history `Pending` → `Rejected`. It does not call `dbo.usp_RecalculateApprovalRequest`.

The working-tree file still calls `dbo.usp_RecalculateApprovalRequest`. It was not accepted for UAT and was not deployed. `06_recovery_design_proposal.md` is still a proposal. It has not been validated on a disposable database copy.

`SwitchUser.aspx.cs` and `SwitchUserHelper.cs` are present. They were not used.

#### This browser attempt's step record

| Step | Status | Evidence |
| --- | --- | --- |
| Login page at `localhost:53713` | **PASS** | Page loaded. HTTP 200. No credentials entered. |
| Running site database | **PASS** as identification only | Application `DbConn` opened `MUMANMOAPP01\SQLEXPRESS` / `AnmolDristiDev`. Not `AnmolDristi`. |
| Candidate 79 read-only preflight | **PASS** as a match to the earlier preflight | No discrepancy in the compared fields. Soft-delete flag still unread. |
| Owner designation of a disposable fixture | **BLOCKED** | Absent. Request 79 remains an in-progress claim. |
| Accepted working-tree procedure deployed | **BLOCKED** | Deployed text is the `Rejected` / detach definition. Working tree was not accepted or deployed. |
| Write authorization on `AnmolDristiDev` | **BLOCKED** | Not granted. |
| Recovery validated on a disposable copy | **BLOCKED** | Proposal only. No snapshot database. |
| Administrator sign-in | **NOT RUN** | Stopped before the first state-changing step. |
| Switch User to L1 `AHS1624` | **NOT RUN** | Sign-in did not happen. |
| Whole-claim return, employee correction, resubmit, later workflow | **NOT RUN** | They follow an accepted return. |
| Recovery and post-recovery compare | **NOT RUN** | No approved recovery method. |

Business data changed in this browser attempt: none. Recovery verified: no.
