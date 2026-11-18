# LIVE discovery capture

**Status: BLOCKED.** Read-only capture on 2026-11-17 through the configured reader connection. No procedure was executed. No schema was changed. No deployment script was written.

The reader session reported server `MUMANMOAPP01\SQLEXPRESS`, catalog `AnmolDristi`, product version `15.0.2000.5`, edition Express (64-bit). Database state `ONLINE`, recovery model `SIMPLE`, access `MULTI_USER`, read-only false, compatibility level 140. `@@TRANCOUNT` was 0. Effective login `sa`, `IS_SRVROLEMEMBER('sysadmin')` 1, `IS_MEMBER('db_owner')` 1.

`SIMPLE` recovery has no transaction-log tail. A later restore can return only to a full backup. `RESTORE VERIFYONLY` was not run. No isolated restore was performed.

## Backup metadata returned

Newest full backups (`type = D`, `is_damaged` false, checksums present). The same finish time was returned on two device paths. This capture does not prove both files are restorable.

| Finish (UTC) | Start (UTC) | Size | Device |
| --- | --- | --- | --- |
| 2026-10-27T18:41:59.000Z | 2026-10-27T18:41:50.000Z | 2631620608 | `D:\anmoldb\Backups\AnmolDristi_23Sep26.bak` |
| 2026-10-27T18:41:59.000Z | 2026-10-27T18:41:50.000Z | 2631620608 | `D:\anmoldb\Backups\AnmolDristi_27Oct26.bak` |
| 2026-10-27T16:52:44.000Z | 2026-10-27T16:52:27.000Z | 2611502080 | both device names above, same pair of rows |
| 2026-10-13T13:39:34.000Z | 2026-10-13T13:39:23.000Z | 2342628352 | `D:\anmoldb\Backups\AnmolDristi_23Sep26.bak` |

The 2026-10-27 finish is the newest row returned. It is not a backup taken for this change, and it is not an accepted restore.

## KM history schema

`dbo.SalesDailyActivityKMHistory` row count reported as 30819. Modified date `2026-06-08T13:39:25.690Z`.

Columns present: `HistoryId`, `ActivityId`, `OldKM decimal(10,2) NULL`, `NewKM decimal(10,2) NULL`, `ModifiedBy nvarchar(100) NULL`, `ModifiedDateTime datetime NOT NULL` default `(dateadd(minute,(330),sysutcdatetime()))`, `Reason nvarchar(1000) NULL`, `RouteOriginName nvarchar(255) NULL`, `RouteDestinationName nvarchar(255) NULL`, `RouteWaypointsJSON nvarchar(max) NULL`, `TravelMode nvarchar(20) NULL`.

`ApprovalRequestId`, `ApproverRole`, and `ApproverLevel` are absent. Primary key `PK__SalesDai__4D7B4ABD9107B9CA` on `HistoryId`. Foreign key `FK_KMHistory_Activity` on `ActivityId` to `dbo.SalesDailyActivity(ActivityId)`, delete and update `NO_ACTION`. No check constraints. Nonclustered index `IX_SalesDailyActivityKMHistory_Activity` on `(ActivityId ASC, ModifiedDateTime DESC)`.

## Counter columns on `dbo.SalesApprovalRequest`

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| `TotalActivities` | `int` | no | `((0))` |
| `PendingActivities` | `int` | no | `((0))` |
| `ApprovedActivities` | `int` | no | `((0))` |
| `RejectedActivities` | `int` | no | `((0))` |
| `ReturnedActivities` | `int` | no | `((0))` |
| `FinalApprovedAmount` | `decimal(18,2)` | yes | none |

## Procedure fingerprints

`SHA-256` is `HASHBYTES('SHA2_256', CONVERT(varbinary(max), definition))` on the stored module, returned as uppercase hex. These are current LIVE module hashes. They are not git hashes and they are not an approval.

| Procedure | `modify_date` (UTC) | LEN | DATALENGTH | SHA-256 |
| --- | --- | --- | --- | --- |
| `dbo.usp_UpdateActivityKM_WithHistory` | 2026-10-15T17:52:54.033Z | 15455 | 30910 | `EC6A97B97605B2A12857367B78C7C84F277289443DFF267C4BB29118E258E100` |
| `dbo.usp_RecalculateApprovalRequest` | 2026-10-15T17:53:39.587Z | 7567 | 15134 | `08F34AF8C5CC188F2A0877985FB7FC5E70DEB0067034B95264AE6EF4739ED5D7` |
| `dbo.usp_RecalculateSalesClaim` | 2026-10-14T13:33:28.887Z | 28786 | 57572 | `FC60A9CFC68BA480454A1D4A4653EAA9426D281267E6750AC3CD96DCC9A33ABC` |
| `dbo.usp_GetResolvedPolicy` | 2025-12-30T17:11:28.043Z | 3297 | 6594 | `99ADB00C37DE0C62400F90B0CE4F6BB195A98129267A7221E490E196ACC2DA90` |
| `dbo.usp_ResolveDesignation` | 2025-12-30T17:12:29.090Z | 1008 | 2016 | `3E65716AB518D0B17C59F2EAACF468CA6711575A0A98E4CC099750C460FE86E0` |
| `dbo.usp_ProcessApprovalAction` | 2026-10-15T17:53:59.940Z | 42031 | 84062 | `FF87E2AAD1319EB48F8237ED9293F674FE4F508E64F39CFDC41DBBA24AFC1392` |
| `dbo.usp_MarkRequestReadyForPayment` | 2026-10-15T17:54:13.583Z | 11189 | 22378 | `2D565A377B668BFC23611F8288B8532B1F24727609EFA32394900B95EC6A69E2` |
| `dbo.usp_ForwardApprovalRequest` | 2026-10-15T17:54:45.657Z | 16459 | 32918 | `02BC07F01C3EDB0B4519561427F679D6B897DBC74527BFC518970F0C000A9F77` |

All eight names were found. `usp_RecalculateSalesClaim` exists on LIVE. Its repository source does not.

## Comparison with repository candidates

| Object | What the stored text shows | What it does not match |
| --- | --- | --- |
| `usp_UpdateActivityKM_WithHistory` | Header says Auditor KM correction. The designation read is `MST_UserMaster.CategoryId`. History insert names `ActivityId`, `OldKM`, `NewKM`, `ModifiedBy`, `Reason` only. Refusal text limits the edit to the current Auditor. It calls `usp_RecalculateSalesClaim`. | Not `21975c2`, which reads `Designation_ID`, allows the current L1/L2/L3/Auditor holder, and inserts the three audit columns. Not the `B_Apply` declared preimage length 15451. Live length is 15455. |
| `usp_RecalculateApprovalRequest` | Approval-file shape: latest detail by `DetailVersion` then `ApprovalRequestDetailId`, `ReadyForPayment` returns before writes, no `BEGIN TRANSACTION`, approved-amount `SUM` excludes `Rejected` and `Returned`. `TotalActivities` is absent. | Not the Utilities file, which counts every detail row and starts its own transaction. Not `c9eba19`, which assigns the four counters. Not the untracked counter script's declared hashes `3ECC03CB…` or `6FF6F1F5…`. |
| `usp_RecalculateSalesClaim` | Definition was returned. It references `#RecalcClaimActivity`, `#RecalcClaimTotals`, `FinalApprovedAmount`, `Snapshot_TotalDistanceKM`, and `BEGIN TRANSACTION`. | No line-by-line formula review was completed. The definition is not accepted. |
| `usp_GetResolvedPolicy` | The empty-parameter designation read is `TRY_CONVERT(VARCHAR(200), CategoryId)`. `Designation_ID` is not that conversion. | Unchanged policy dependency. Not approved for alteration. |
| `usp_ResolveDesignation` | Stored text contains `Designation_ID`. | Not approved for alteration. |
| `usp_ProcessApprovalAction` | Contains both `Auditor pass complete: keep the request Pending` and a non-auditor `Completed` write, and calls `usp_RecalculateApprovalRequest`. | Not the quoted 2026-10-14 checklist hash `594AA859…` (that record said LEN 33961). C-1 is not signed. |
| `usp_MarkRequestReadyForPayment` | Accepts Pending or Completed, contains error `59112`, calls recalc, and still contains the comment that the candidate is not authorized for UAT or LIVE execution. | Not the quoted 2026-10-14 hash `E9097D3A…` (that record said LEN 11181). Live LEN is 11189. C-1 and C-5 are not signed. |
| `usp_ForwardApprovalRequest` | Contains `The Auditor cannot forward a request.` | Not approved for alteration. |

## Why no deployment package was produced

The minimum approved delta is empty.

1. C-1 and C-3 are unsigned in `08_Owner_Decision_Register.md`.
2. `21975c2` and `c9eba19` are not accepted.
3. The deployment login observed is `sa`.
4. The newest backup row is 2026-10-27 and has no verify or isolated restore.
5. `usp_RecalculateSalesClaim` is fingerprinted and not reviewed.
6. Existing UAT apply and rollback scripts declare different lengths or hashes, and they abort on `AnmolDristi`. They were not reused.

Phase 5 did not run. Deployment success is not claimed.

## Additive note — claim module text received

**2026-11-18 closeout.** This note does not replace the 2026-11-17 rows above and does not sign a decision. No SQL was executed for this note. The module text is not copied into the repository. The evidence reference is the conversation closeout that supplied the complete LIVE definition and the identity below. That identity matches the fingerprint already in the table above.

| Field | Value |
| --- | --- |
| Server | `MUMANMOAPP01\SQLEXPRESS` |
| Database | `AnmolDristi` |
| Procedure | `dbo.usp_RecalculateSalesClaim` |
| CapturedAt | 2026-11-17 23:21:37.3964546 +05:30 |
| ObjectId | 743009728 |
| ModifyDate | 2026-10-14 13:33:28.887 |
| DefinitionBytes | 57572 |
| Characters | 28786 |
| SHA-256 | `FC60A9CFC68BA480454A1D4A4653EAA9426D281267E6750AC3CD96DCC9A33ABC` |

The source-capture prerequisite for this procedure is satisfied. The text is no longer an absent capture.

Caller lists counted from `Database/Stored Procedures/Approval/usp_UpdateActivityKM_WithHistory.sql` at `21975c2`: `#RecalcClaimActivity` has 11 columns and `#RecalcClaimTotals` has 14 columns. The closeout states that the callee insert order matches those lists. That comparison is a source-level **MATCH**. It is not a runtime result.

The same closeout states that the callee participates in the caller-owned transaction and commits or rolls back only a transaction it owns, and that it does not modify approval status, the current approver, or routing. Owner acceptance of that transaction behaviour, and any runtime verification, stay unsigned. `usp_RecalculateSalesClaim` is not altered. The definition is still not an owner-accepted deployment input. Item 5 above remains the 2026-11-17 statement. This note is the later receipt record.
