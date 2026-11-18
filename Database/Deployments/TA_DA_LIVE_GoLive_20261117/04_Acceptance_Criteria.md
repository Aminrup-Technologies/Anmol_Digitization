# Acceptance criteria

**Additive notice, 2026-11-18.** No row below is marked met. The criteria describe a package window that was not opened. The later catalog read found the three history columns and KM hash `F6B07BE8…` already on `AnmolDristi`, and found other module hashes no longer equal to the discovery snapshot named in A2, A7, A8, and A9. That read is not acceptance of this window. Document 12 records it.

**Closure status: DOCUMENTATION UPDATED — NO-CHANGE DEPLOYMENT DECISION PENDING FORMAL ACCEPTANCE.** No criterion below is marked met. Runtime financial verification and backup verification are not recorded here.

**Status: NOT MET.** No criterion below has been executed. A pasted grid belongs in `05_Execution_Log_Template.md`. This file never receives a PASS mark in advance.

Acceptance of the window requires every row.

| ID | Criterion | Met only when |
| --- | --- | --- |
| A1 | Server and database | Preflight and postflight show `MUMANMOAPP01\SQLEXPRESS` and `AnmolDristi` |
| A2 | Pinned preimages at start | Preflight failure grid is empty for the eight fingerprinted modules, the history column list, and the named primary key, foreign key, and activity index. Counter-column rows are parked with WP-03 and are not this criterion |
| A3 | WP-01 shape | The three new columns are nullable, types `int`, `nvarchar(20)`, and `int`, `max_length` 40 for the role, no default, no foreign key, no index, and no check. Existing history columns, the primary key, `FK_KMHistory_Activity`, and `IX_SalesDailyActivityKMHistory_Activity` are unchanged. Row count, minimum `HistoryId`, and the sum of `HistoryId` match the values stored in the evidence row |
| A4 | History data | No existing history row was updated or deleted by WP-01. New columns are NULL on those rows |
| A5 | WP-02 text | Replacing `ALTER   PROCEDURE` with `CREATE   PROCEDURE` in `OBJECT_DEFINITION` of `dbo.usp_UpdateActivityKM_WithHistory` equals the canonical module submitted by `30_WP02_KM_Procedure.sql`. That text's SHA-256 is `F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9` and equals `SubmittedSha256` and `AppliedSha256`. The module contains `Designation_ID` and the three audit names, and does not contain `CategoryId` |
| A6 | WP-02 behaviour bounds | The applied text still has `SET XACT_ABORT OFF`, one `BEGIN TRANSACTION`, `COMMIT` after the claim call, and `ROLLBACK` only when `XACT_STATE()` is not 0. It does not assign `SalesApprovalRequest.Status`, `CurrentApproverLoginId`, or `CurrentApproverLevel` |
| A7 | Financial callee | `usp_RecalculateSalesClaim` SHA-256, `LEN`, and `DATALENGTH` are still the quoted fingerprint. The procedure was not altered by this package |
| A8 | Policy and workflow | `usp_GetResolvedPolicy`, `usp_ResolveDesignation`, `usp_ProcessApprovalAction`, `usp_MarkRequestReadyForPayment`, and `usp_ForwardApprovalRequest` still match the quoted fingerprints |
| A9 | Recalc excluded | `usp_RecalculateApprovalRequest` SHA-256, `LEN`, and `DATALENGTH` are still the quoted fingerprint. File `40` was not executed. Postflight shows `EXCLUDED_UNCHANGED` for that procedure |
| A10 | Payable formula | No script on the execution path edits the `SUM` that writes `FinalApprovedAmount`. File `40` is parked and throws before its historical batch |
| A11 | Employee path | `usp_UpdateActivityKM` still has an explicit history insert list and does not require the three new columns. `usp_GetSalesDailyActivityKMHistory` is not a `SELECT *` of the history table |
| A12 | Postflight | The last `50_Postflight_ReadOnly.sql` grid shows `APPLIED` for WP-01 and WP-02, `EXCLUDED_UNCHANGED` for `usp_RecalculateApprovalRequest`, and no `FAIL` row |
| A13 | Log | `05_Execution_Log_Template.md` has the backup finish time, verify output, isolated database name, login, and the names and dates from document 08. Blank cells are not acceptance |
| A14 | Rollback proof, required before the window and not by experimenting on LIVE | The reviewed revision shows that `60` restores the saved KM module only when the current canonical hash is `F6B07BE8` and the saved hash is `EC6A97B9` at length 15455, and that `61` leaves columns in place when any audit value is non-NULL, a definition differs, or a module or dependency still references them. `60` does not restore `usp_RecalculateApprovalRequest`. The LIVE header bytes are not in this repository, so this criterion is not met |

A green preflight by itself does not accept the window. `NOT_APPLIED` after step 1 is the expected postflight state.
