# LIVE TA/DA reconciliation matrix

**Additive notice, 2026-11-18.** A later read-only catalog comparison is in `12_Dev_LIVE_ReadOnly_Reconciliation_20261118.md`. The text below is the 2026-11-17 source review and is unchanged. It is not the current Dev-to-LIVE result.

**Status: BLOCKED.** This is a read-only source review dated 2026-11-17. No connection was made to `AnmolDristi` or `AnmolDristiDev`. No procedure was executed. No hash below was recomputed with `HASHBYTES`.

Classification used in this pack:

| Class | Meaning |
| --- | --- |
| CONFIRMED | Read from the current repository in this session. |
| SUPPLIED EVIDENCE ONLY | Stated in the task brief, or quoted from an older document. Not re-read from LIVE. |
| INFERRED | Follows from confirmed source, and is not a LIVE fact. |
| NOT VERIFIED | The current LIVE definition, schema, backup, or approval was not supplied and was not read. |

`docs/INDEX.md` §8 C-1 still labels `HEAD` as `52c1f32`. Current `HEAD` is `c9eba192a1e504a0c8e4da2869a1b57d29246cf4`. `git diff --stat 52c1f32 HEAD` shows no change to `usp_ProcessApprovalAction`, `usp_MarkRequestReadyForPayment`, `usp_ForwardApprovalRequest`, `usp_GetResolvedPolicy`, or `usp_ResolveDesignation`. The terminal-pair conflict in §8 remains open. This file does not resolve it.

## Git facts (CONFIRMED)

| Fact | Value |
| --- | --- |
| Branch | `Aug26_AILWorks_AmimnrupSupport` |
| `HEAD` | `c9eba192a1e504a0c8e4da2869a1b57d29246cf4` |
| `HEAD` subject | Fix approval request activity counters during recalculation |
| Upstream | `origin/Aug26_AILWorks_AmimnrupSupport` at `21975c200d9c9bed1f24a7b97ec802ff22faa2f4` |
| Ahead / behind | 2 / 0 |
| Unpushed commits | `755b94efba6d1de5a4a7b69794f319d75f25a38b` (`.gitignore` only); `c9eba192a1e504a0c8e4da2869a1b57d29246cf4` (activity counters) |
| Tag `tada-v1.1-postgolive-fsot-2026` | `3779d185001b3182042d5c8d38e5b111675d8dee` |
| Baseline ancestry | `3779d185` is an ancestor of `HEAD`. `git rev-list --count 3779d185..HEAD` = 95 |
| Tracked working-tree change | `AnmolDristi/Web.config` is modified. It was not opened. |
| Untracked items left untouched | `.cloud-attachments/`; `Database/Deployments/ApprovalRequestActivityCounters/`; `Database/Deployments/ApproverKmEdit/D_Rollback_ApproverKmEdit_UAT.sql`; the `ITSM_*.md` files |

What is committed, pushed, or only local:

| Change | Where it lives | Class |
| --- | --- | --- |
| Current-holder KM procedure and `Designation_ID` source | Commit `21975c2`, which is `origin` | CONFIRMED |
| `B_Apply_ApproverKmEdit_UAT.sql` and `C_Apply_DesignationIdCorrection_UAT.sql` | Same commit `21975c2`, tracked | CONFIRMED |
| Activity-counter edit in `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql` | Commit `c9eba19`, local only, not on `origin` | CONFIRMED |
| `Database/Deployments/ApprovalRequestActivityCounters/B_Apply_ActivityCounters_UAT.sql` | Untracked | CONFIRMED |
| `D_Rollback_ApproverKmEdit_UAT.sql` | Untracked | CONFIRMED |

Git blob hashes were not compared with SQL `HASHBYTES` hashes. Those encodings differ.

## Object matrix

| Object | Repository source | Supplied or quoted LIVE evidence | Difference visible from source | Risk to existing claims | Disposition |
| --- | --- | --- | --- | --- | --- |
| `dbo.usp_UpdateActivityKM_WithHistory` | `Database/Stored Procedures/Approval/usp_UpdateActivityKM_WithHistory.sql`, last commit `21975c2` (on origin). Parameters: `@ActivityId INT`, `@NewKM DECIMAL(10,2)`, `@ChangedBy NVARCHAR(100)`, `@Reason NVARCHAR(1000)`, `@ExpectedDetailId INT`. | SUPPLIED EVIDENCE ONLY: LIVE body references `CategoryId`. Complete current LIVE text was not supplied. | Source reads `MST_UserMaster.Designation_ID` (line 209). It inserts `ApprovalRequestId`, `ApproverRole`, `ApproverLevel`. `B_Apply` still embeds a `CategoryId` candidate. `C_Apply` is the UAT step from that candidate to `Designation_ID`. | A KM save calls `dbo.usp_RecalculateSalesClaim` for the whole request. A different designation gate can allow or refuse that save. Amounts are written by the claim procedure, whose LIVE body is NOT VERIFIED. | HOLD. No existing script is a LIVE deployment of the current source. |
| `dbo.usp_RecalculateApprovalRequest` | Two files, one database object. Candidate with the 2026-11-17 counter edit: `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql` at `c9eba19` (not pushed). Other file: `Database/Stored Procedures/Utilities/usp_RecalculateApprovalRequest.sql`, last commit `1f1e8f2`. `docs/INDEX.md` C-3: no canonical body is selected. | SUPPLIED EVIDENCE ONLY: no detected references to the activity-counter columns. Complete current LIVE text was not supplied. | Approval candidate adds four counter assignments and leaves the approved-amount `SUM` unchanged (`git diff 755b94e HEAD`). Utilities file counts every detail row, starts its own transaction, and uses a different amount formula. | Replacing the LIVE procedure with either file rewrites `FinalApprovedAmount` and `Approved*` on the next call. Callers include day action, Mark Ready, submit, return-to-employee, expense status, and `ClaimsApproval_v2.aspx.cs`. | HOLD. Do not deploy either file until the current LIVE body is compared and C-3 is decided. |
| `dbo.usp_RecalculateSalesClaim` | No `CREATE PROCEDURE` source in this repository. | NOT VERIFIED. | The KM procedure executes it with `@ApprovalRequestId` and expects `#RecalcClaimActivity` / `#RecalcClaimTotals`. | This is the financial writer for a KM save. | Out of the change set. Blocking dependency for any KM deployment. |
| `dbo.usp_GetResolvedPolicy` | `Database/Stored Procedures/Utilities/usp_GetResolvedPolicy.sql`, last commit `1f1e8f2`. Header says extracted 2026-08-02 from `AnmolDristiDev`. Parameters: `@EmployeeCode VARCHAR(50)` plus four optional `VARCHAR(200)` organisation inputs. Returns `SELECT TOP (1) pm.*` ordered by `Priority DESC`, `EffectiveFrom DESC`. | NOT VERIFIED. The extraction note is historical source text, not a current LIVE read. | When the designation parameter is empty, the procedure reads `MST_UserMaster.CategoryId` (line 30). The KM procedure calls it with all organisation parameters NULL, so the rate path still uses `CategoryId` while the KM gate uses `Designation_ID`. | The two designation inputs can disagree. Changing this procedure is a policy change and is not proposed. | Out of the change set. Rule 9 stands. |
| `dbo.usp_ResolveDesignation` | `Database/Stored Procedures/Utilities/usp_ResolveDesignation.sql`, last commit `1f1e8f2`. Same 2026-08-02 extraction header. Parameters: `@InputValue VARCHAR(200)`, `@ResolvedId INT OUTPUT`. Order: numeric `MST_Designation.Id`, then `Designation_ID`, then `Designation_Description`. | NOT VERIFIED. | No change since the extraction commit was found in the file log for this path. | The KM gate passes `Designation_ID` through this resolver. | Out of the change set. |
| `dbo.usp_ProcessApprovalAction` | `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql`, last commit `52c1f32`. Parameters: `@ApprovalRequestDetailId INT`, `@Action NVARCHAR(20)`, `@ActionBy NVARCHAR(100)`, `@Remarks NVARCHAR(MAX) = NULL`. | NOT VERIFIED for the current catalog. Quoted record, not a current read: `TA_DA_Deployment_Checklist.md` and `RC014B_RC014D_UAT_Request97.md` record a 2026-10-14 `AnmolDristi` shape (modify date 2026-10-08, LEN 33961, SHA-256 `594AA8596BEE359D4226C1704E56326D0BA60EA368705279C8A28DE1A9939D01`). | At `HEAD`, the Auditor terminal branch leaves the header `Pending` (lines 622–627) and then calls `usp_RecalculateApprovalRequest` (line 839). Tag `3779d185` is a different pair (`docs/INDEX.md` C-1). | Replacing this procedure would change workflow. It is not in the proposed delta. A later counter deployment would still run inside this procedure's transaction. | Out of the change set. C-1 remains pending. |
| `dbo.usp_MarkRequestReadyForPayment` | `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql`, last commit `52c1f32`. Parameters: `@ApprovalRequestId INT`, `@ActionBy NVARCHAR(100)`, `@Remarks NVARCHAR(MAX) = NULL`. The file header says it is not authorized for UAT or LIVE execution. | NOT VERIFIED for the current catalog. Quoted record: the checklist records a 2026-10-14 promotion on `AnmolDristi` to LEN 11181, SHA-256 `E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409`, from a preimage SHA-256 `6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451`. `docs/INDEX.md` C-5 leaves acceptance of that promotion pending. | `HEAD` accepts `Pending` or `Completed`, calls recalc, approves one Pending Auditor row, writes `Pending` to `Completed`, then `Completed` to `ReadyForPayment`. | Payment gating. Not part of the proposed delta. | Out of the change set. C-1 and C-5 remain pending. |
| `dbo.usp_ForwardApprovalRequest` | `Database/Stored Procedures/Approval/usp_ForwardApprovalRequest.sql`, last commit `94bc208`. Parameters: `@ApprovalRequestId INT`, `@ActionBy NVARCHAR(100)`. | NOT VERIFIED. | Auditor role raises `51012`. Latest-detail completeness uses `MAX(DetailVersion)` then `MAX(ApprovalRequestDetailId)`. It does not write `ReadyForPayment`. | Routing. Not part of the proposed delta. | Out of the change set. Auditor-to-L1 routing stays parked (C-10). |
| `dbo.SalesDailyActivityKMHistory` | `Database/Tables/SalesDailyActivityKMHistory.sql`, last commit `1f1e8f2`. Columns in that script: `HistoryId`, `ActivityId`, `OldKM`, `NewKM`, `ModifiedBy`, `ModifiedDateTime`, `Reason`, `RouteOriginName`, `RouteDestinationName`, `RouteWaypointsJSON`, `TravelMode`. PK, FK to `SalesDailyActivity`, index on `(ActivityId, ModifiedDateTime DESC)`. | SUPPLIED EVIDENCE ONLY: `ApprovalRequestId`, `ApproverRole`, and `ApproverLevel` are absent. Current column list was not read from LIVE. | The KM procedure inserts the three names. `B_Apply_ApproverKmEdit_UAT.sql` adds them as nullable columns with no default, no FK, and no new index. The table script does not contain them. | Existing history rows stay. Employee inserts in `usp_UpdateActivityKM` name the original columns only, so the new columns would remain NULL on that path. | HOLD until current LIVE columns are read. Smallest additive shape is in document 02. |
| `dbo.SalesApprovalRequest` activity counters | `Database/Tables/SalesApprovalRequest.sql`: `TotalActivities`, `ApprovedActivities`, `RejectedActivities`, `PendingActivities`, and `ReturnedActivities` are `int NOT NULL` with default `0`. | SUPPLIED EVIDENCE ONLY for the procedure's lack of references. Column presence on LIVE was not read. | The `c9eba19` candidate writes the first four counters from the latest detail. It does not assign `ReturnedActivities`. | Counter values change on the next recalc of a request that is not `ReadyForPayment`. Amount formulas in that candidate's `SUM` are unchanged relative to its parent. | HOLD. No counter-column DDL is proposed. |

## Activity-counter behaviour in the Approval candidate (CONFIRMED source)

The parent of `c9eba19` matches `98947ebcebdd8e5db8da14029ae4ecb94a8445a3` for this file (`git diff --stat` between those two commits is empty). The commit adds variables, one latest-detail count, and four columns on the existing header `UPDATE`. It does not change the approved-amount `SUM`.

| Check | Source result |
| --- | --- |
| Latest detail | `ROW_NUMBER()` partitioned by `ActivityId`, ordered by `ISNULL(DetailVersion, 1) DESC`, `ApprovalRequestDetailId DESC`, keep `RN = 1`. The money roll-up uses the existing `MAX(DetailVersion)` then `MAX(ApprovalRequestDetailId)` pattern. |
| Counters | `TotalActivities = COUNT(*)`. `Pending` / `Approved` / `Rejected` match those exact `DetailStatus` strings. |
| `Returned` | A latest `Returned` row increments `TotalActivities` only. `ReturnedActivities` is not set. `TotalActivities` can exceed the sum of the other three. |
| Duplicate versions | The `RN = 1` filter keeps one row per activity in the counter query. |
| `ReadyForPayment` | The procedure returns before any write when `Status = N'ReadyForPayment'`. Requests already in that status are not backfilled. |
| Header identity | The `UPDATE` does not set `Status`, `CurrentApproverLoginId`, or `CurrentApproverLevel`. |
| Amounts | `ApprovedKM`, `ApprovedClaimAmount`, `ApprovedDAAmount`, `ApprovedHotelAmount`, `ApprovedTravelAmount`, and `FinalApprovedAmount` use the pre-existing snapshot `SUM`. `TotalKM` and `TotalClaimAmount` are not assigned. |

This describes the git candidate. It is not a statement that LIVE matches it.

## Why this review is BLOCKED

Current LIVE definitions of the eight procedures were not supplied and were not read. Current LIVE column metadata for `SalesDailyActivityKMHistory` was not read. No fresh backup or `RESTORE VERIFYONLY` evidence for `AnmolDristi` was supplied for this review. C-1 and C-3 are still pending. No deployment is authorized by this document.
