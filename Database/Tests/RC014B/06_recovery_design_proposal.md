# RC-014B recovery design proposal

Status: **PROPOSAL ONLY.** Not approved. Not validated. Not an executable script. Not applied to any database.

This file does not authorize a write to `AnmolDristiDev` or `AnmolDristi`. `dbo.usp_AdminRollbackApprovalRequest` is excluded: it deletes history, approver rows, and detail versions, then detaches activities. `Database/Tests/RC014B/02_behavior_tran_rollback.sql` is excluded: it wraps one call to `dbo.usp_ProcessApprovalAction` and rolls that outer transaction back. The UAT sequence commits several procedures separately, so one outer transaction cannot put the claim back.

Candidate `ApprovalRequestId` 79 is not designated. On `AnmolDristiDev` it is requester `AHS1121`, June 2026, header `Pending`, with approved, returned, and pending days. On `AnmolDristi` the same id is requester `AHS750`, March 2026, header `ReadyForPayment`. Any future script must name the database and refuse `AnmolDristi`.

## What a before-image has to cover

Taken in one read-only consistent snapshot, before the first behavioral write, for every activity linked to the chosen request:

- `dbo.SalesApprovalRequest`: status, requester, year, month, current approver login and level, the six approved-amount columns, `TotalDeductionAmount`, `ModifiedBy`, `ModifiedOn`, and the original `ApprovalRequestId`.
- `dbo.SalesApprovalRequestApprover`: every row, including role, level, status, actor, action time, and remarks.
- `dbo.SalesApprovalRequestDetail`: every version, `PreviousDetailId`, `DetailStatus`, and every snapshot amount column. Latest ownership is `MAX(ISNULL(DetailVersion, 1))` then `MAX(ApprovalRequestDetailId)`.
- `dbo.SalesDailyActivity`: `ClaimStatus`, `ApprovalRequestId`, `TotalDistanceKM`, `CalculatedClaimAmount`, and the modified-by stamp.
- `dbo.SalesDailyExpense`: `ExpenseId`, type, date, `Amount`, `Remarks`, `ClaimStatus`, soft-delete flag, delete actor, delete time, and delete reason.
- `dbo.ApprovalRequestStatusHistory`: every existing row, plus a record of the highest `HistoryId` so rows created during the test can be identified.
- `dbo.SalesDailyActivityKMHistory`: every existing row for those activities, plus the highest `HistoryId`.

The 2026-10-11 read did not capture soft-delete flags or remark text. A future before-image has to include both. The reader used for that preflight rejected a query that named the deleted-flag column.

## Concurrency

Another session can change this claim during UAT. The restore must abort when any of these differ from the before-image unless the change is one the test itself recorded by primary key:

- header `ModifiedOn`
- detail row count and the set of `ApprovalRequestDetailId` values
- expense row count and the set of `ExpenseId` values
- maximum history id and maximum KM-history id
- activity `ApprovalRequestId` and `TotalDistanceKM`

If any other change is present, the restore stops and the owner reconciles. Overwriting a concurrent edit is outside this proposal.

## Method to validate before it touches the chosen fixture

1. Restore a backup of the chosen database onto a new database name that is neither `AnmolDristi` nor the database the portal uses.
2. On that copy only, capture the before-image into side tables in a schema created for this test.
3. Run the full sequence that will later be authorized: whole-claim return, expense soft-delete, expense insert, KM edit, resubmit, L1 approval, auditor approval, header completion, and mark ready. Each of those steps commits on its own.
4. Restore from the side tables by primary key. Delete only history and KM-history rows whose ids are greater than the captured maximum and whose request or activity belongs to the copy's fixture. Put back header, approver, detail, activity, and expense columns from the side tables, including soft-delete flags, amounts, remarks, `PreviousDetailId`, and `ApprovalRequestId`.
5. Compare the copy with the before-image. The proposal becomes eligible for owner approval only when that compare matches and the side tables are still intact.
6. Repeat the compare after a deliberate concurrent update on the copy. The restore must abort on that update.

Until step 5 has been observed on a disposable database, this design stays unused. The preflight of request 79 is not that validation.
