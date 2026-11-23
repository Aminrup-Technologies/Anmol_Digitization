# Dev-to-LIVE read-only reconciliation, 2026-11-18

**Status: VERIFIED comparison. Deployment PREPARED — NOT EXECUTED. No state-changing script was run.**

**Closure status: DOCUMENTATION UPDATED — NO-CHANGE DEPLOYMENT DECISION PENDING FORMAL ACCEPTANCE.**

**Disposition.** Feature implementation for the tested TA/DA scope is CLOSED. Dev-to-LIVE SQL reconciliation for that scope is COMPLETE: no further functional SQL deployment is required. DBA/IT governance sign-off is pending. `DR-NOCHANGE`, C-1, and C-3 remain unsigned. Runtime financial and payment verification remain NOT TESTED. A local Git commit of these files is not a deployment, a backup verification, or a signature. The statement that feature implementation is closed refers only to that tested procedure-deployment scope. WP-03 stored-counter acceptance remains pending.

**Additive notice, WP-03 stored values.** A later read-only inspection found that the four-counter formula is already the executed procedure on both catalogs, and that the stored counter values do not match it. That is a stored-value accuracy defect. It does not change the procedure-body conclusion above, and it does not authorize file `40`. Status: `DESIGN CORRECTED — READY FOR INDEPENDENT REVIEW — EXECUTION NOT AUTHORIZED`. WP-03 stored-counter acceptance remains pending. The design is at the end of this file. No backfill was run. `ReturnedActivities` stays outside that design. DR-COUNTER stays unsigned.

For the tested TA/DA SQL scope, no functional difference requiring a deployment was identified. WP-01 and WP-02 are already present on `AnmolDristi` and match `AnmolDristiDev`. Other procedure differences are formatting only and are not normalized. WP-03 stays excluded. Scripts `20`, `30`, `40`, `60`, and `61` are NOT TO BE EXECUTED for this outcome. `RESTORE VERIFYONLY` and an isolated restore remain NOT AVAILABLE. Runtime financial and payment verification remain NOT TESTED. `DR-NOCHANGE` in `08_Owner_Decision_Register.md` records that result with choice, name, and date left blank. This paragraph is not a DBA or IT signature.

This file is an additive record. It does not replace `01_LIVE_Reconciliation_Matrix.md` or `10_LIVE_Discovery_Capture.md`. Those files remain the 2026-11-17 review and the discovery snapshot. Where this file and those files disagree about the current catalog, this file is the later read.

No row here is a signature. C-1, C-3, and the gates in `08_Owner_Decision_Register.md` stay unsigned.

## Session

| Fact | Value |
| --- | --- |
| Branch | `Aug26_AILWorks_AmimnrupSupport` |
| HEAD | `c9eba192a1e504a0c8e4da2869a1b57d29246cf4` |
| Annotated tag object `tada-v1.1-postgolive-fsot-2026` | `2f361f3434de3985949073ec3a06ba08ccb3aaa6` |
| Commit that tag peels to | `3779d185001b3182042d5c8d38e5b111675d8dee` |
| Baseline ancestry | `3779d185` is an ancestor of HEAD. `git rev-list --count 3779d185..HEAD` = 95 |
| Tracked working tree at the start of this read | Clean. `AnmolDristi/Web.config` was not modified and was not opened |
| Writes | None. No procedure was executed. No `GRANT`. No git stage, commit, or push |

`git rev-parse tada-v1.1-postgolive-fsot-2026` without `^{}` returns the annotated tag object, not the commit. The peeled commit is the immutable baseline named in `docs/INDEX.md`.

## Database identity

Two reader connections were queried independently. `DB_NAME()` was read from each session. A later comparison used three-part names from the LIVE session after those two identities were known. `flamex_uat` on `WIN-8H6JMQBVGP2\MSSQLSERVER2017` was not used.

| Item | LIVE session | Dev session |
| --- | --- | --- |
| `@@SERVERNAME` | `MUMANMOAPP01\SQLEXPRESS` | `MUMANMOAPP01\SQLEXPRESS` |
| `DB_NAME()` | `AnmolDristi` | `AnmolDristiDev` |
| `ORIGINAL_LOGIN()` / `SUSER_SNAME()` | `sa` / `sa` | `sa` / `sa` |
| `USER_NAME()` | `dbo` | `dbo` |
| State | `ONLINE` | `ONLINE` |
| Recovery model | `SIMPLE` | `SIMPLE` |
| `is_read_only` | 0 | 0 |
| `@@TRANCOUNT` | 0 | 0 |
| Compatibility level | 140 | 140 |
| `IS_SRVROLEMEMBER('sysadmin')` | 1 | not re-read; same login |
| `IS_MEMBER('db_owner')` | 1 | not re-read |
| `IS_MEMBER('db_ddladmin')` | 1 | not re-read |

`sa` is the inspection login. It is not a deployment identity. No permission was granted.

## Fingerprint method

`HASHBYTES('SHA2_256', CONVERT(varbinary(max), definition))` on `sys.sql_modules.definition`, uppercase hex. `LEN` and `DATALENGTH` are of that nvarchar. `modify_date` is the value returned by the server, style 121, with no timezone conversion applied in this file.

Normalization used only when the raw hashes differed:

1. Remove `[` and `]` and compare the remaining nvarchar. Used for `usp_RecalculateApprovalRequest`. Stripped SHA-256 of both sides: `276049428E88846B6CF5D0010B36C1241EC521BA13A07CE1A8598042A39EE1F8`.
2. Where that was not enough, also remove U+0020, CR, and LF, and compare. Tab counts were equal. This was used to test whether any non-whitespace, non-quote character differed. It was not used to declare an exact match.

Comments, amount formulas, and other statements were not stripped.

## Reconciliation matrix

| Object | Dev chars / bytes / SHA-256 | LIVE chars / bytes / SHA-256 | Class | Action |
| --- | --- | --- | --- | --- |
| `dbo.SalesDailyActivityKMHistory` | 14 columns. `ApproverRole` is `nvarchar`, `max_length` 40 bytes, so `nvarchar(20)`, NULL, no default. PK `PK__SalesDai__4D7B4ABD9107B9CA`. FK `FK_KMHistory_Activity`. Index `IX_SalesDailyActivityKMHistory_Activity`. No check. No trigger. 1024 rows. 5 audit rows, max role length 7 | Same column order, types, nullability, defaults, PK, FK, index, no check, no trigger. 30837 rows. 9 audit rows, all three columns set together, 0 partial rows, max role length 7. Roles: `Auditor` level 2 (8), `L1` level 1 (1) | MATCHED | No schema change. Do not backfill |
| `dbo.usp_UpdateActivityKM_WithHistory` | 16354 / 32708 / `F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9`. `modify_date` 2026-11-17 15:41:17.283 | Same length and hash. `modify_date` 2026-11-18 10:39:27.650 | MATCHED | No replacement |
| `dbo.usp_RecalculateApprovalRequest` | 8747 / 17494 / `57AF68BD0762111F88B687C336158151218968BA8C0325A19349DE607884733C`. `modify_date` 2026-11-17 17:55:43.880 | 8751 / 17502 / `670E2D99199599A33216EB490E4020C7D54474CA66D48D555A7F2975DF9F308C`. `modify_date` 2026-11-18 10:22:57.790 | DIFFERENT, formatting only | No replacement. WP-03 stays excluded |
| `dbo.usp_RecalculateSalesClaim` | 28782 / 57564 / `F9CF46C81A1673FE35762282E2C6A1844536A7EF7DF7934372B488726D2D8597`. `modify_date` 2026-10-08 08:50:40.357 | 28786 / 57572 / `FC60A9CFC68BA480454A1D4A4653EAA9426D281267E6750AC3CD96DCC9A33ABC`. `modify_date` 2026-10-14 13:33:28.887 | DIFFERENT, formatting only | No replacement |
| `dbo.usp_ProcessApprovalAction` | 45564 / 91128 / `BF3A26BF5468796D98C245795FAC08F054163BFC15A1EEB0960FEC1AC3A6F4B3`. `modify_date` 2026-10-18 04:43:18.507 | 45572 / 91144 / `379426A5DF44800E44ED0B1425AF778866110C35A5F6C2AEE8BFFD6C6D9F8ED0`. `modify_date` 2026-11-18 10:27:04.120 | DIFFERENT, formatting only | No workflow script |
| `dbo.usp_ReturnClaimToEmployee` | 17790 / 35580 / `F510B5C9F9E373A89B766C66A36202DB57C957276607048E158CAC32A0720F86`. `modify_date` 2026-11-13 16:58:30.377 | 17794 / 35588 / `7D83899D2316633A9F747D567293925F44192EF29999C0C881AB346F3E80B10F`. `modify_date` 2026-11-18 10:25:48.027 | DIFFERENT, formatting only | No workflow script |
| `dbo.usp_MarkRequestReadyForPayment` | 11181 / 22362 / `E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409`. `modify_date` 2026-10-13 14:11:49.980 | 11189 / 22378 / `2D565A377B668BFC23611F8288B8532B1F24727609EFA32394900B95EC6A69E2`. `modify_date` 2026-10-15 17:54:13.583 | DIFFERENT, formatting only | No workflow script |
| `dbo.usp_ForwardApprovalRequest` | 16455 / 32910 / `71B71698CD9B0F0555E9E213E508E3412824D932FE128D879AC40E2295068B1D`. `modify_date` 2026-10-08 08:50:40.420 | 16459 / 32918 / `02BC07F01C3EDB0B4519561427F679D6B897DBC74527BFC518970F0C000A9F77`. `modify_date` 2026-10-15 17:54:45.657 | DIFFERENT, formatting only | No workflow script |
| `dbo.usp_GetResolvedPolicy` | 3297 / 6594 / `99ADB00C37DE0C62400F90B0CE4F6BB195A98129267A7221E490E196ACC2DA90` | same hash and length | MATCHED | No change |
| `dbo.usp_ResolveDesignation` | 1008 / 2016 / `3E65716AB518D0B17C59F2EAACF468CA6711575A0A98E4CC099750C460FE86E0` | same hash and length | MATCHED | No change |
| `dbo.usp_ResolveRegion` | 1066 / 2132 / `AE775E56BA084315AEF5238D7FC0E4C1B780749B00678C39149959877DEFB601` | same hash and length | MATCHED | No change |
| `dbo.usp_ResolveBranch` | 958 / 1916 / `B90A07B587348A066F3BA1BC1D15AEC4F566E72C46296FB4415A72EEEC06DE40` | same hash and length | MATCHED | No change |
| `dbo.usp_ResolveGrade` | 948 / 1896 / `AF0E4E54B6B233FB587C5CD198F05653B2E3D6E2C9AC44EC54BF84D7796096A1` | same hash and length | MATCHED | No change |
| `dbo.usp_UpdateActivityKM` | 7039 / 14078 / `CD6205400D9CB66A5E791A55502B061D859569A6A822A285A5460E005484A1D0` | 7043 / 14086 / `6701BD23F2233FEF888C5E82476F332056D1D3B8411027D1685471F5048D25A1` | DIFFERENT, formatting only | Out of scope. Do not replace |
| `dbo.usp_GetSalesDailyActivityKMHistory` | 1329 / 2658 / `1267653A00C8C5DDC077527F070B2DE16DE531CD04139403035D8ADE08CFEDA7` | same hash and length | MATCHED | No change |

`SalesApprovalRequest` counter columns `TotalActivities`, `PendingActivities`, `ApprovedActivities`, `RejectedActivities`, and `ReturnedActivities` are `int NOT NULL` on both catalogs. `FinalApprovedAmount` is `decimal` nullable, 9 bytes, on both.

## What the formatting difference is

`usp_RecalculateApprovalRequest` differs only by identifier quotes. Dev declaration: `CREATE   PROCEDURE dbo.usp_RecalculateApprovalRequest`. LIVE declaration: `CREATE   PROCEDURE [dbo].[usp_RecalculateApprovalRequest]`. Space delta 0. Tab delta 0. Length delta 4.

The other DIFFERENT procedures have one extra CR and one extra LF on LIVE, two extra spaces, and in some cases the same `[dbo].[name]` quoting. After those characters are removed, the remaining text is equal. Observed declarations:

| Procedure | Dev declaration window | LIVE declaration window |
| --- | --- | --- |
| `usp_RecalculateSalesClaim` | `CREATE     PROCEDURE [dbo].[usp_RecalculateSalesClaim]` | `CREATE       PROCEDURE [dbo].[usp_RecalculateSalesClaim]` |
| `usp_ProcessApprovalAction` | `CREATE   PROCEDURE dbo.usp_ProcessApprovalAction` | `CREATE     PROCEDURE [dbo].[usp_ProcessApprovalAction]` |
| `usp_ReturnClaimToEmployee` | `CREATE   PROCEDURE [dbo].[usp_ReturnClaimToEmployee]` | `CREATE     PROCEDURE [dbo].[usp_ReturnClaimToEmployee]` |
| `usp_MarkRequestReadyForPayment` | `CREATE   PROCEDURE dbo.usp_MarkRequestReadyForPayment` | `CREATE     PROCEDURE [dbo].[usp_MarkRequestReadyForPayment]` |
| `usp_ForwardApprovalRequest` | `CREATE     PROCEDURE [dbo].[usp_ForwardApprovalRequest]` | `CREATE       PROCEDURE [dbo].[usp_ForwardApprovalRequest]` |
| `usp_UpdateActivityKM` | `CREATE     PROCEDURE [dbo].[usp_UpdateActivityKM]` | `CREATE       PROCEDURE [dbo].[usp_UpdateActivityKM]` |

No procedure is proposed for replacement on that basis.

## Behaviour checked inside the matched text

KM module, both catalogs, hash `F6B07BE8`. Markers found in the LIVE module, which is byte-identical to Dev: `Designation_ID`, `Reporting_Manager1`, `ApprovalRequestId`, `ApproverRole`, `ApproverLevel`, `#RecalcClaimActivity`, `#RecalcClaimTotals`. `@@TRANCOUNT` does not occur. The repository candidate with this canonical hash starts `BEGIN TRANSACTION` before `EXEC dbo.usp_RecalculateSalesClaim` and commits after it. Runtime of that path was NOT TESTED.

Claim module, both catalogs. The transaction window is the same text: if `@@TRANCOUNT = 0`, it starts a transaction and sets `@OwnTran = 1`. It commits only when `@OwnTran = 1`. When the KM procedure has already started a transaction, the claim procedure does not commit that transaction. `#RecalcClaimActivity` columns match the KM table: `ActivityId`, `ActivityDate`, `TotalDistanceKM`, `RatePerKm`, `CalculatedClaimAmount`, `DAAmount`, `HotelAmount`, `TravelAmount`, `StationaryAmount`, `ExpenseTotal`, `OverallAmount`. `#RecalcClaimTotals` columns match the KM table through `FinalApprovedAmount`. This is source compatibility, not a runtime test.

Approval recalculation, both catalogs, equal after removing identifier quotes. The Dev module declares `@TotalActivities`, `@PendingActivities`, `@ApprovedActivities`, and `@RejectedActivities`. The name `ReturnedActivities` does not occur in either module, so neither module assigns that column. `DetailVersion`, `ROW_NUMBER`, `ReadyForPayment`, and `FinalApprovedAmount` occur in both. The repository file `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql` has the same header name and assigns the four counters from the latest detail (`RN = 1`) without assigning `ReturnedActivities`, and it returns before writes when status is `ReadyForPayment`. That file was not hash-compared with the Dev module in this session. The Dev-to-LIVE conclusion does not depend on it: the stored modules have no non-quote difference.

The employee procedure does not contain `ApprovalRequestId`, `ApproverRole`, or `ApproverLevel`. Its history insert can leave the three columns NULL. That is the same compact text on both catalogs. LIVE consumers of `SalesDailyActivityKMHistory` are `usp_UpdateActivityKM`, `usp_UpdateActivityKM_WithHistory`, and `usp_GetSalesDailyActivityKMHistory`.

## LIVE change inventory against the 2026-11-17 discovery snapshot

The discovery file recorded these LIVE fingerprints. Current values that differ are below. `modify_date` says when the catalog last recorded a metadata change. It does not say who changed the object.

| Object | Discovery snapshot | Current LIVE | Server `modify_date` |
| --- | --- | --- | --- |
| `usp_UpdateActivityKM_WithHistory` | `EC6A97B9…` / 15455 / 30910, discovery `modify_date` 2026-10-15T17:52:54.033Z | `F6B07BE8…` / 16354 / 32708 | 2026-11-18 10:39:27.650 |
| `usp_RecalculateApprovalRequest` | `08F34AF8…` / 7567 / 15134, discovery `modify_date` 2026-10-15T17:53:39.587Z | `670E2D99…` / 8751 / 17502 | 2026-11-18 10:22:57.790 |
| `usp_ProcessApprovalAction` | `FF87E2AA…` / 42031 / 84062, discovery `modify_date` 2026-10-15T17:53:59.940Z | `379426A5…` / 45572 / 91144 | 2026-11-18 10:27:04.120 |
| `SalesDailyActivityKMHistory` | three audit columns absent; reported row count 30819; table modified 2026-06-08T13:39:25.690Z | three columns present; `COUNT_BIG(*)` 30837; table modified 2026-11-18 10:35:51.047 | 2026-11-18 10:35:51.047 |

Unchanged against that snapshot: `usp_RecalculateSalesClaim` `FC60A9CF…`, `usp_GetResolvedPolicy` `99ADB00C…`, `usp_ResolveDesignation` `3E65716A…`, `usp_MarkRequestReadyForPayment` `2D565A37…`, `usp_ForwardApprovalRequest` `02BC07F0…`.

`usp_ReturnClaimToEmployee` had no row in that snapshot. Its current LIVE hash is `7D83899D…`, which is the fingerprint supplied for this reconciliation. It is not byte-identical to Dev.

`dbo.TaDaLiveGoLive_20261117_Preimage` is absent. This package has no evidence row for the objects above. The current modules are not attributed to scripts `20` or `30`.

## Backup and restore

Backup set 71, media set 31, `type = D`, finish `2026-11-18 10:16:40.000`, checksums present, `is_damaged` 0, `is_copy_only` 0, size 3252291584. It is one striped backup: `family_sequence_number` 1, 2, and 3, `mirror` 0, device type 2:

- `D:\anmoldb\Backups\AnmolDristi_23Sep26.bak`
- `D:\anmoldb\Backups\AnmolDristi_27Oct26.bak`
- `D:\anmoldb\Backups\AnmolDristi_LiveDB_18Nov26.bak`

The same `23Sep26` and `27Oct26` paths are also recorded for the 2026-10-27 backup sets. This catalog does not prove those older bytes are still in the files.

The 10:16 finish is before the `modify_date` values of the KM procedure, the approval recalculation, the process and return procedures, and the history table, and before the nine audit rows (first `2026-11-18 10:36:29.643`, last `2026-11-18 11:14:21.200`). A restore of set 71 would not contain those catalog changes or those nine rows. `RESTORE VERIFYONLY` was not run. No isolated restore is recorded. `msdb.dbo.restorehistory` has one `AnmolDristi` row, `2024-05-13 11:56:58.080`. Restore readiness is NOT AVAILABLE.

## Deployment conclusion

No schema script and no procedure script is required to make LIVE match tested Dev behaviour for this scope.

| Item | Disposition |
| --- | --- |
| WP-01 columns | Already present and matched. Do not run `20_WP01_Schema.sql` |
| WP-02 KM procedure | Already present and matched. Do not run `30_WP02_KM_Procedure.sql` |
| WP-03 | Formatting-only difference. File `40` stays `EXCLUDED — DO NOT EXECUTE`. Replacement is not recommended |
| Workflow procedures | Formatting-only. No proposed script |
| Claim, policy, and resolvers | No replacement |
| Employee KM procedure | Formatting-only and outside this package |
| Evidence table | Still absent. Creating it is not required for a no-change outcome |
| Rollback `60` / `61` | Do not run. `61` is also blocked by nine non-NULL audit values. `60` still expects the discovery recalc hash `08F34AF8…`, which is no longer current, and it still aborts on unsigned gates |

`10_Preflight_ReadOnly.sql` and `50_Postflight_ReadOnly.sql` still judge the 2026-11-17 discovery snapshot and the package evidence row. On the current catalog they report failure or throw. That result is expected. It is not a reason to apply or roll back.

Least privilege for a future state-changing login remains the matrix in `01_Deployment_Manifest.md`. This reconciliation does not name that login and does not grant anything.

## Not tested

Procedure execution, claim recalculation, payment transition, `RESTORE VERIFYONLY`, and an isolated restore were not performed. Financial correctness is not claimed from the text comparison.

## WP-03 stored-counter defect and correction design

**Status: `DESIGN CORRECTED — READY FOR INDEPENDENT REVIEW — EXECUTION NOT AUTHORIZED`.** WP-03 stored-counter acceptance remains pending.

This section is additive. The procedure-replacement row above still stands: file `40_WP03_ActivityCounters.sql` stays `EXCLUDED — DO NOT EXECUTE`. This design does not alter `dbo.usp_RecalculateApprovalRequest`, does not assign `ReturnedActivities`, and does not write any amount, status, or detail column. No statement in this section has been executed.

### Confirmed defect

On 2026-11-18 the reader sessions were `AnmolDristi` and `AnmolDristiDev` on `MUMANMOAPP01\SQLEXPRESS`, both `ONLINE`. The executed modules were the Approval-file shape. LIVE was 8,751 characters, `modify_date` 2026-11-18 10:22:57.790. Dev was 8,747 characters, `modify_date` 2026-11-17 17:55:43.880. The visible difference is the LIVE brackets in `[dbo].[usp_RecalculateApprovalRequest]`. SHA-256 was not recomputed in that inspection. The earlier hashes remain historical evidence: LIVE `670E2D99199599A33216EB490E4020C7D54474CA66D48D555A7F2975DF9F308C`, Dev `57AF68BD0762111F88B687C336158151218968BA8C0325A19349DE607884733C`. Neither module contains `ReturnedActivities`.

The formula counts one latest detail per activity, ordered by `ISNULL(DetailVersion, 1) DESC`, then `ApprovalRequestDetailId DESC`. `TotalActivities` is `COUNT(*)`. `PendingActivities`, `ApprovedActivities`, and `RejectedActivities` count the exact strings `Pending`, `Approved`, and `Rejected`. Any other latest status, including `Returned` and NULL, increases `TotalActivities` only. `ReadyForPayment` returns before those assignments.

Stored values on that read:

| Catalog | Requests | Rows matching the formula | `ReturnedActivities` not 0 |
| --- | --- | --- | --- |
| `AnmolDristi` | 2,269 | 45 | 0 |
| `AnmolDristiDev` | 137 | 5 | 0 |

LIVE `Pending`: 308 requests, 13 matching. 290 of those 308 had classified latest details while the three status counters were still 0. `TotalActivities` differed on 6 of the 308. LIVE `ReadyForPayment`: 1,810 requests, 1,779 not matching, 1,320 all four counters at 0. Those paid rows are excluded from the correction because the procedure itself does not rewrite them.

No module other than `usp_RecalculateApprovalRequest`, `usp_SubmitSalesDailyActivities`, and `usp_GetApprovalRequest` references the four counters. No module references `ReturnedActivities`. The approval screen counts detail rows. Payment eligibility counts unresolved latest details. The column remains `int NOT NULL` default `((0))` and is left unchanged.

### Target and exclusions

The proposal below is for `AnmolDristi` only. Dev is evidence, not a target. At run time the script must recompute the population. It must not require the historical counts above, because ordinary workflow can change them before a later approval.

| In the update | Out of the update |
| --- | --- |
| Header rows whose `Status` is not `ReadyForPayment` and whose four counters differ from the formula | Every `ReadyForPayment` row |
| `TotalActivities`, `PendingActivities`, `ApprovedActivities`, `RejectedActivities` | `ReturnedActivities`, every amount column, `Status`, approver columns, detail rows, expenses, and the procedure text |

A request with no detail rows uses the same result as the procedure's initialized variables: all four counters 0.

### Schema evidence used by this design

`Database/Tables/SalesApprovalRequest.sql` is the repository extraction dated 2026-08-02 from `AnmolDristiDev`. This task did not query the live catalog. In that script, `Status` is `nvarchar(50) NOT NULL` with default `Pending`. `ModifiedOn` is `datetime2(7) NULL`. `ModifiedBy` and `CurrentApproverLoginId` are `nvarchar(100) NULL`. `CurrentApproverLevel` is `int NULL`. `OriginalClaimAmount` and `TotalDeductionAmount` are `decimal(18,2) NULL`. The preflight below requires `Status` to be non-nullable `nvarchar(50)`. There is no approved NULL-status population.

That script contains no trigger. A search of `Database/**/*.sql` finds one `CREATE TRIGGER`, `dbo.TR_PolicyExceptionHistory_PreventMutation`, and it is not on `SalesApprovalRequest`. No `SCHEMABINDING` view was found. Four foreign keys reference `ApprovalRequestId` and declare no `ON UPDATE` or `ON DELETE` action: `FK_ApprovalHistory_Request`, `FK_SARDetail_Request`, `FK_SalesApprovalRequestApprover_Request`, and `FK_Adjustment_Request`. This design does not change `ApprovalRequestId`, so those keys do not rewrite child rows. `sys.triggers` was not read. A live trigger outside this extraction remains unverified. The checks below can see a change only in the snapshotted columns. They do not prove that an unlisted trigger left every other table unchanged.

### Safety controls

The text aborts while `@CorrectionApproved` is `UNSIGNED` and while `@CommitMode` is `ROLLBACK-ONLY`. Replacing either literal is a later reviewed source change, not something to type into a session. It also aborts when the server, database, open transaction, counter column shape, or LIVE procedure fingerprint differs from this section. The fingerprint gate is the historical LIVE hash and length 8,751, plus the `ReadyForPayment` return and the absence of `ReturnedActivities`.

This design acquires `UPDLOCK, HOLDLOCK` on the header rows before it acquires those hints on the detail rows. `usp_MarkRequestReadyForPayment` may acquire detail locks before header locks. Concurrent execution can therefore deadlock. SQL Server rolls back the deadlock victim's transaction. The surviving transaction may continue after the victim releases its locks. This design does not retry. The update predicate still requires the locked `Status`, `ModifiedOn`, four counters, `ReturnedActivities`, and amount columns to equal the before-image, so a change that committed before those locks were taken is not overwritten.

There is no separate rollback file. Failure uses `ROLLBACK` and rethrows. The only committed-data reversal is a later reviewed update that restores the four counters from the before-image result set saved by the DBA. That reversal must use the same locks and must not touch amount or status columns. Scripts `40`, `60`, and `61` are not that reversal.

### Before and after evidence required

Before the update, while the transaction is open, the session returns:

1. Server, database, procedure length, SHA-256, and the NULL-counter count. The SHA-256 gate uses the historical LIVE hash. This file does not re-verify that hash.
2. Non-payment mismatch counts from `#before` and `#formula`, grouped by `Status`.
3. The row-level before-image of every non-payment row whose four counters differ from `#formula`.
4. One diagnostic count, `ReadyForPaymentFormulaMismatch`. It uses the same latest-detail formula and reads `#payBefore`. It is not an update target, and it is not the preservation check.

`#payBefore` is the locked `ReadyForPayment` snapshot. Error `59540` compares the row count, the snapshotted values, and the row set. A current `ReadyForPayment` id missing from `#payBefore`, or a `#payBefore` id that is no longer `ReadyForPayment`, fails that check. The formula-mismatch diagnostic does not.

After the preservation checks, and before throw `59537`, the session returns the summary grids and one row-level grid, `CounterReconciliation`, with `ApprovalRequestId` and the old, proposed, and current values of the four counters. The transaction is still open. Those grids are a rollback-only rehearsal, not committed after-state evidence.

A future committed run would still need the saved grids, the DBA name, and the actual commit time. This document does not supply them. DR-COUNTER choice, name, and date stay blank. WP-03 stored-counter acceptance remains pending.

```sql
/*
    WP-03 counter-only correction DESIGN.
    Status: NOT TO BE EXECUTED.
    Not a deployment script. Does not replace or run 40_WP03_ActivityCounters.sql.
    Target: AnmolDristi on MUMANMOAPP01\SQLEXPRESS.
    Writes, only after both gate literals are replaced in a reviewed revision:
      TotalActivities, PendingActivities, ApprovedActivities, RejectedActivities
      on headers whose Status is not ReadyForPayment.
    Does not assign ReturnedActivities and does not change amounts, status, or details.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @CorrectionApproved nvarchar(40) = N'UNSIGNED';
DECLARE @CommitMode nvarchar(20) = N'ROLLBACK-ONLY';

IF @CorrectionApproved <> N'DR-COUNTER-CORRECTION-A'
    THROW 59530, N'ABORT: counter-only correction is unsigned. No row was changed.', 1;
IF UPPER(CONVERT(nvarchar(256), SERVERPROPERTY('ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
    OR UPPER(CONVERT(nvarchar(256), @@SERVERNAME)) <> N'MUMANMOAPP01\SQLEXPRESS'
    THROW 59531, N'ABORT: server is not the approved target.', 1;
IF DB_NAME() <> N'AnmolDristi'
    THROW 59532, N'ABORT: database is not AnmolDristi.', 1;
IF @@TRANCOUNT <> 0
    THROW 59533, N'ABORT: a transaction is already open.', 1;
IF DATABASEPROPERTYEX(DB_NAME(), 'Updateability') <> N'READ_WRITE'
    THROW 59532, N'ABORT: database is not read-write.', 1;

IF EXISTS (
    SELECT 1
    FROM (VALUES
        (N'TotalActivities'),
        (N'PendingActivities'),
        (N'ApprovedActivities'),
        (N'RejectedActivities'),
        (N'ReturnedActivities')
    ) AS n(ColumnName)
    WHERE NOT EXISTS (
        SELECT 1
        FROM sys.columns AS c
        INNER JOIN sys.types AS t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(N'dbo.SalesApprovalRequest')
          AND c.name = n.ColumnName
          AND t.name = N'int'
          AND c.is_nullable = 0
    )
)
    THROW 59534, N'ABORT: a counter column is missing or is not int NOT NULL.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns AS c
    INNER JOIN sys.types AS t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SalesApprovalRequest')
      AND c.name = N'Status'
      AND t.name = N'nvarchar'
      AND c.max_length = 100
      AND c.is_nullable = 0
)
    THROW 59541, N'ABORT: Status is not nvarchar(50) NOT NULL. The repository extraction has no NULL-status population.', 1;

IF EXISTS (
    SELECT 1
    FROM (VALUES
        (N'ModifiedOn', N'datetime2', 1),
        (N'ModifiedBy', N'nvarchar', 1),
        (N'CurrentApproverLoginId', N'nvarchar', 1),
        (N'CurrentApproverLevel', N'int', 1),
        (N'OriginalClaimAmount', N'decimal', 1),
        (N'TotalDeductionAmount', N'decimal', 1)
    ) AS e(ColumnName, TypeName, NullableBit)
    WHERE NOT EXISTS (
        SELECT 1
        FROM sys.columns AS c
        INNER JOIN sys.types AS t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(N'dbo.SalesApprovalRequest')
          AND c.name = e.ColumnName
          AND t.name = e.TypeName
          AND c.is_nullable = e.NullableBit
    )
)
    THROW 59542, N'ABORT: an approver, routing, audit, or payment column is missing or has a different nullability.', 1;

DECLARE @def nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest'));
DECLARE @sha char(64) = CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @def)), 2);
IF @def IS NULL
    OR LEN(@def) <> 8751
    OR @sha <> N'670E2D99199599A33216EB490E4020C7D54474CA66D48D555A7F2975DF9F308C'
    OR CHARINDEX(N'ReturnedActivities', @def) > 0
    OR CHARINDEX(N'IF @HeaderStatus = N''ReadyForPayment''', @def) = 0
    OR CHARINDEX(N'TotalActivities      = @TotalActivities', @def) = 0
    OR CHARINDEX(N'PendingActivities    = @PendingActivities', @def) = 0
    OR CHARINDEX(N'ApprovedActivities   = @ApprovedActivities', @def) = 0
    OR CHARINDEX(N'RejectedActivities   = @RejectedActivities', @def) = 0
    THROW 59535, N'ABORT: usp_RecalculateApprovalRequest is not the reviewed LIVE module.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    CREATE TABLE #before
    (
        ApprovalRequestId int NOT NULL PRIMARY KEY,
        Status nvarchar(50) NOT NULL,
        ModifiedOn datetime2 NULL,
        ModifiedBy nvarchar(100) NULL,
        CurrentApproverLoginId nvarchar(100) NULL,
        CurrentApproverLevel int NULL,
        TotalActivities int NOT NULL,
        PendingActivities int NOT NULL,
        ApprovedActivities int NOT NULL,
        RejectedActivities int NOT NULL,
        ReturnedActivities int NOT NULL,
        FinalApprovedAmount decimal(18,2) NULL,
        ApprovedKM decimal(12,2) NULL,
        ApprovedClaimAmount decimal(18,2) NULL,
        ApprovedDAAmount decimal(18,2) NULL,
        ApprovedHotelAmount decimal(18,2) NULL,
        ApprovedTravelAmount decimal(18,2) NULL,
        OriginalClaimAmount decimal(18,2) NULL,
        TotalDeductionAmount decimal(18,2) NULL
    );

    CREATE TABLE #payBefore
    (
        ApprovalRequestId int NOT NULL PRIMARY KEY,
        Status nvarchar(50) NOT NULL,
        ModifiedOn datetime2 NULL,
        ModifiedBy nvarchar(100) NULL,
        CurrentApproverLoginId nvarchar(100) NULL,
        CurrentApproverLevel int NULL,
        TotalActivities int NOT NULL,
        PendingActivities int NOT NULL,
        ApprovedActivities int NOT NULL,
        RejectedActivities int NOT NULL,
        ReturnedActivities int NOT NULL,
        FinalApprovedAmount decimal(18,2) NULL,
        ApprovedKM decimal(12,2) NULL,
        ApprovedClaimAmount decimal(18,2) NULL,
        ApprovedDAAmount decimal(18,2) NULL,
        ApprovedHotelAmount decimal(18,2) NULL,
        ApprovedTravelAmount decimal(18,2) NULL,
        OriginalClaimAmount decimal(18,2) NULL,
        TotalDeductionAmount decimal(18,2) NULL
    );

    INSERT INTO #before
    (
        ApprovalRequestId, Status, ModifiedOn, ModifiedBy, CurrentApproverLoginId, CurrentApproverLevel,
        TotalActivities, PendingActivities, ApprovedActivities, RejectedActivities, ReturnedActivities,
        FinalApprovedAmount, ApprovedKM, ApprovedClaimAmount, ApprovedDAAmount, ApprovedHotelAmount, ApprovedTravelAmount,
        OriginalClaimAmount, TotalDeductionAmount
    )
    SELECT
        h.ApprovalRequestId, h.Status, h.ModifiedOn, h.ModifiedBy, h.CurrentApproverLoginId, h.CurrentApproverLevel,
        h.TotalActivities, h.PendingActivities, h.ApprovedActivities, h.RejectedActivities, h.ReturnedActivities,
        h.FinalApprovedAmount, h.ApprovedKM, h.ApprovedClaimAmount, h.ApprovedDAAmount, h.ApprovedHotelAmount, h.ApprovedTravelAmount,
        h.OriginalClaimAmount, h.TotalDeductionAmount
    FROM dbo.SalesApprovalRequest AS h WITH (UPDLOCK, HOLDLOCK)
    WHERE h.Status <> N'ReadyForPayment';

    INSERT INTO #payBefore
    (
        ApprovalRequestId, Status, ModifiedOn, ModifiedBy, CurrentApproverLoginId, CurrentApproverLevel,
        TotalActivities, PendingActivities, ApprovedActivities, RejectedActivities, ReturnedActivities,
        FinalApprovedAmount, ApprovedKM, ApprovedClaimAmount, ApprovedDAAmount, ApprovedHotelAmount, ApprovedTravelAmount,
        OriginalClaimAmount, TotalDeductionAmount
    )
    SELECT
        h.ApprovalRequestId, h.Status, h.ModifiedOn, h.ModifiedBy, h.CurrentApproverLoginId, h.CurrentApproverLevel,
        h.TotalActivities, h.PendingActivities, h.ApprovedActivities, h.RejectedActivities, h.ReturnedActivities,
        h.FinalApprovedAmount, h.ApprovedKM, h.ApprovedClaimAmount, h.ApprovedDAAmount, h.ApprovedHotelAmount, h.ApprovedTravelAmount,
        h.OriginalClaimAmount, h.TotalDeductionAmount
    FROM dbo.SalesApprovalRequest AS h WITH (UPDLOCK, HOLDLOCK)
    WHERE h.Status = N'ReadyForPayment';

    DECLARE @detailLock bigint;
    SELECT @detailLock = COUNT_BIG(*)
    FROM dbo.SalesApprovalRequestDetail AS d WITH (UPDLOCK, HOLDLOCK)
    WHERE d.ApprovalRequestId IN (
        SELECT b.ApprovalRequestId FROM #before AS b
        UNION ALL
        SELECT p.ApprovalRequestId FROM #payBefore AS p
    );

    CREATE TABLE #formula
    (
        ApprovalRequestId int NOT NULL PRIMARY KEY,
        TotalActivities int NOT NULL,
        PendingActivities int NOT NULL,
        ApprovedActivities int NOT NULL,
        RejectedActivities int NOT NULL
    );

    INSERT INTO #formula
    (
        ApprovalRequestId, TotalActivities, PendingActivities, ApprovedActivities, RejectedActivities
    )
    SELECT
        ranked.ApprovalRequestId,
        COUNT(*),
        ISNULL(SUM(CASE WHEN ranked.DetailStatus = 'Pending'  THEN 1 ELSE 0 END), 0),
        ISNULL(SUM(CASE WHEN ranked.DetailStatus = 'Approved' THEN 1 ELSE 0 END), 0),
        ISNULL(SUM(CASE WHEN ranked.DetailStatus = 'Rejected' THEN 1 ELSE 0 END), 0)
    FROM
    (
        SELECT
            d.ApprovalRequestId,
            d.DetailStatus,
            ROW_NUMBER() OVER (
                PARTITION BY d.ApprovalRequestId, d.ActivityId
                ORDER BY ISNULL(d.DetailVersion, 1) DESC, d.ApprovalRequestDetailId DESC
            ) AS RN
        FROM dbo.SalesApprovalRequestDetail AS d
        INNER JOIN #before AS b ON b.ApprovalRequestId = d.ApprovalRequestId
    ) AS ranked
    WHERE ranked.RN = 1
    GROUP BY ranked.ApprovalRequestId;

    INSERT INTO #formula
    (
        ApprovalRequestId, TotalActivities, PendingActivities, ApprovedActivities, RejectedActivities
    )
    SELECT b.ApprovalRequestId, 0, 0, 0, 0
    FROM #before AS b
    WHERE NOT EXISTS (SELECT 1 FROM #formula AS f WHERE f.ApprovalRequestId = b.ApprovalRequestId);

    SELECT
        DB_NAME() AS DatabaseName,
        @@SERVERNAME AS ServerName,
        LEN(@def) AS ProcedureCharacters,
        @sha AS ProcedureSha256,
        (SELECT COUNT(*) FROM dbo.SalesApprovalRequest WHERE TotalActivities IS NULL OR PendingActivities IS NULL OR ApprovedActivities IS NULL OR RejectedActivities IS NULL OR ReturnedActivities IS NULL) AS NullCounterRows;

    SELECT
        b.Status,
        COUNT(*) AS RequestRows,
        SUM(CASE
            WHEN b.TotalActivities <> f.TotalActivities
              OR b.PendingActivities <> f.PendingActivities
              OR b.ApprovedActivities <> f.ApprovedActivities
              OR b.RejectedActivities <> f.RejectedActivities
            THEN 1 ELSE 0 END) AS NonPaymentMismatch
    FROM #before AS b
    INNER JOIN #formula AS f ON f.ApprovalRequestId = b.ApprovalRequestId
    GROUP BY b.Status;

    SELECT
        b.ApprovalRequestId, b.Status, b.ModifiedOn, b.ReturnedActivities,
        b.TotalActivities AS OldTotal, f.TotalActivities AS NewTotal,
        b.PendingActivities AS OldPending, f.PendingActivities AS NewPending,
        b.ApprovedActivities AS OldApproved, f.ApprovedActivities AS NewApproved,
        b.RejectedActivities AS OldRejected, f.RejectedActivities AS NewRejected
    FROM #before AS b
    INNER JOIN #formula AS f ON f.ApprovalRequestId = b.ApprovalRequestId
    WHERE b.TotalActivities <> f.TotalActivities
       OR b.PendingActivities <> f.PendingActivities
       OR b.ApprovedActivities <> f.ApprovedActivities
       OR b.RejectedActivities <> f.RejectedActivities;

    DECLARE @target int = (
        SELECT COUNT(*)
        FROM #before AS b
        INNER JOIN #formula AS f ON f.ApprovalRequestId = b.ApprovalRequestId
        WHERE b.TotalActivities <> f.TotalActivities
           OR b.PendingActivities <> f.PendingActivities
           OR b.ApprovedActivities <> f.ApprovedActivities
           OR b.RejectedActivities <> f.RejectedActivities
    );
    DECLARE @paymentRows int = (SELECT COUNT(*) FROM #payBefore);

    /* Read-only diagnostic. #payBefore rows are not inserted into #formula and are not an update target. */
    SELECT
        N'ReadyForPaymentFormulaMismatch' AS EvidenceSet,
        COUNT(*) AS ReadyForPaymentFormulaMismatch
    FROM #payBefore AS p
    LEFT JOIN
    (
        SELECT
            ranked.ApprovalRequestId,
            COUNT(*) AS TotalActivities,
            ISNULL(SUM(CASE WHEN ranked.DetailStatus = 'Pending'  THEN 1 ELSE 0 END), 0) AS PendingActivities,
            ISNULL(SUM(CASE WHEN ranked.DetailStatus = 'Approved' THEN 1 ELSE 0 END), 0) AS ApprovedActivities,
            ISNULL(SUM(CASE WHEN ranked.DetailStatus = 'Rejected' THEN 1 ELSE 0 END), 0) AS RejectedActivities
        FROM
        (
            SELECT
                d.ApprovalRequestId,
                d.DetailStatus,
                ROW_NUMBER() OVER (
                    PARTITION BY d.ApprovalRequestId, d.ActivityId
                    ORDER BY ISNULL(d.DetailVersion, 1) DESC, d.ApprovalRequestDetailId DESC
                ) AS RN
            FROM dbo.SalesApprovalRequestDetail AS d
            INNER JOIN #payBefore AS pay ON pay.ApprovalRequestId = d.ApprovalRequestId
        ) AS ranked
        WHERE ranked.RN = 1
        GROUP BY ranked.ApprovalRequestId
    ) AS payFormula
        ON payFormula.ApprovalRequestId = p.ApprovalRequestId
    WHERE ISNULL(payFormula.TotalActivities, 0) <> p.TotalActivities
       OR ISNULL(payFormula.PendingActivities, 0) <> p.PendingActivities
       OR ISNULL(payFormula.ApprovedActivities, 0) <> p.ApprovedActivities
       OR ISNULL(payFormula.RejectedActivities, 0) <> p.RejectedActivities;

    UPDATE h
    SET
        h.TotalActivities = f.TotalActivities,
        h.PendingActivities = f.PendingActivities,
        h.ApprovedActivities = f.ApprovedActivities,
        h.RejectedActivities = f.RejectedActivities
    FROM dbo.SalesApprovalRequest AS h
    INNER JOIN #before AS b ON b.ApprovalRequestId = h.ApprovalRequestId
    INNER JOIN #formula AS f ON f.ApprovalRequestId = h.ApprovalRequestId
    WHERE h.Status = b.Status
      AND h.Status <> N'ReadyForPayment'
      AND ((h.ModifiedOn = b.ModifiedOn) OR (h.ModifiedOn IS NULL AND b.ModifiedOn IS NULL))
      AND h.TotalActivities = b.TotalActivities
      AND h.PendingActivities = b.PendingActivities
      AND h.ApprovedActivities = b.ApprovedActivities
      AND h.RejectedActivities = b.RejectedActivities
      AND h.ReturnedActivities = b.ReturnedActivities
      AND ((h.FinalApprovedAmount = b.FinalApprovedAmount) OR (h.FinalApprovedAmount IS NULL AND b.FinalApprovedAmount IS NULL))
      AND ((h.ApprovedKM = b.ApprovedKM) OR (h.ApprovedKM IS NULL AND b.ApprovedKM IS NULL))
      AND ((h.ApprovedClaimAmount = b.ApprovedClaimAmount) OR (h.ApprovedClaimAmount IS NULL AND b.ApprovedClaimAmount IS NULL))
      AND ((h.ApprovedDAAmount = b.ApprovedDAAmount) OR (h.ApprovedDAAmount IS NULL AND b.ApprovedDAAmount IS NULL))
      AND ((h.ApprovedHotelAmount = b.ApprovedHotelAmount) OR (h.ApprovedHotelAmount IS NULL AND b.ApprovedHotelAmount IS NULL))
      AND ((h.ApprovedTravelAmount = b.ApprovedTravelAmount) OR (h.ApprovedTravelAmount IS NULL AND b.ApprovedTravelAmount IS NULL))
      AND EXISTS (SELECT h.ModifiedBy INTERSECT SELECT b.ModifiedBy)
      AND EXISTS (SELECT h.CurrentApproverLoginId INTERSECT SELECT b.CurrentApproverLoginId)
      AND EXISTS (SELECT h.CurrentApproverLevel INTERSECT SELECT b.CurrentApproverLevel)
      AND EXISTS (SELECT h.OriginalClaimAmount INTERSECT SELECT b.OriginalClaimAmount)
      AND EXISTS (SELECT h.TotalDeductionAmount INTERSECT SELECT b.TotalDeductionAmount)
      AND (
            b.TotalActivities <> f.TotalActivities
         OR b.PendingActivities <> f.PendingActivities
         OR b.ApprovedActivities <> f.ApprovedActivities
         OR b.RejectedActivities <> f.RejectedActivities
      );

    DECLARE @updated int = @@ROWCOUNT;

    IF @updated <> @target
        THROW 59536, N'ABORT: a locked row changed or the update count moved. Rolled back.', 1;

    IF (SELECT COUNT(*) FROM dbo.SalesApprovalRequest WHERE Status = N'ReadyForPayment') <> @paymentRows
        OR EXISTS (
            SELECT h.ApprovalRequestId
            FROM dbo.SalesApprovalRequest AS h
            WHERE h.Status = N'ReadyForPayment'
            EXCEPT
            SELECT p.ApprovalRequestId FROM #payBefore AS p
        )
        OR EXISTS (
            SELECT p.ApprovalRequestId FROM #payBefore AS p
            EXCEPT
            SELECT h.ApprovalRequestId
            FROM dbo.SalesApprovalRequest AS h
            WHERE h.Status = N'ReadyForPayment'
        )
        OR EXISTS (
            SELECT 1
            FROM #payBefore AS p
            INNER JOIN dbo.SalesApprovalRequest AS h ON h.ApprovalRequestId = p.ApprovalRequestId
            WHERE h.Status <> p.Status
               OR h.TotalActivities <> p.TotalActivities
               OR h.PendingActivities <> p.PendingActivities
               OR h.ApprovedActivities <> p.ApprovedActivities
               OR h.RejectedActivities <> p.RejectedActivities
               OR h.ReturnedActivities <> p.ReturnedActivities
               OR EXISTS (SELECT h.ModifiedOn EXCEPT SELECT p.ModifiedOn)
               OR EXISTS (SELECT h.ModifiedBy EXCEPT SELECT p.ModifiedBy)
               OR EXISTS (SELECT h.CurrentApproverLoginId EXCEPT SELECT p.CurrentApproverLoginId)
               OR EXISTS (SELECT h.CurrentApproverLevel EXCEPT SELECT p.CurrentApproverLevel)
               OR EXISTS (SELECT h.FinalApprovedAmount EXCEPT SELECT p.FinalApprovedAmount)
               OR EXISTS (SELECT h.ApprovedKM EXCEPT SELECT p.ApprovedKM)
               OR EXISTS (SELECT h.ApprovedClaimAmount EXCEPT SELECT p.ApprovedClaimAmount)
               OR EXISTS (SELECT h.ApprovedDAAmount EXCEPT SELECT p.ApprovedDAAmount)
               OR EXISTS (SELECT h.ApprovedHotelAmount EXCEPT SELECT p.ApprovedHotelAmount)
               OR EXISTS (SELECT h.ApprovedTravelAmount EXCEPT SELECT p.ApprovedTravelAmount)
               OR EXISTS (SELECT h.OriginalClaimAmount EXCEPT SELECT p.OriginalClaimAmount)
               OR EXISTS (SELECT h.TotalDeductionAmount EXCEPT SELECT p.TotalDeductionAmount)
        )
        THROW 59540, N'ABORT: the ReadyForPayment row set or snapshot changed. This is not a formula-mismatch check. Rolled back.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest AS h
        INNER JOIN #before AS b ON b.ApprovalRequestId = h.ApprovalRequestId
        WHERE h.ReturnedActivities <> b.ReturnedActivities
           OR h.Status <> b.Status
           OR EXISTS (SELECT h.ModifiedOn EXCEPT SELECT b.ModifiedOn)
           OR EXISTS (SELECT h.ModifiedBy EXCEPT SELECT b.ModifiedBy)
           OR EXISTS (SELECT h.CurrentApproverLoginId EXCEPT SELECT b.CurrentApproverLoginId)
           OR EXISTS (SELECT h.CurrentApproverLevel EXCEPT SELECT b.CurrentApproverLevel)
           OR EXISTS (SELECT h.FinalApprovedAmount EXCEPT SELECT b.FinalApprovedAmount)
           OR EXISTS (SELECT h.ApprovedKM EXCEPT SELECT b.ApprovedKM)
           OR EXISTS (SELECT h.ApprovedClaimAmount EXCEPT SELECT b.ApprovedClaimAmount)
           OR EXISTS (SELECT h.ApprovedDAAmount EXCEPT SELECT b.ApprovedDAAmount)
           OR EXISTS (SELECT h.ApprovedHotelAmount EXCEPT SELECT b.ApprovedHotelAmount)
           OR EXISTS (SELECT h.ApprovedTravelAmount EXCEPT SELECT b.ApprovedTravelAmount)
           OR EXISTS (SELECT h.OriginalClaimAmount EXCEPT SELECT b.OriginalClaimAmount)
           OR EXISTS (SELECT h.TotalDeductionAmount EXCEPT SELECT b.TotalDeductionAmount)
    )
        THROW 59538, N'ABORT: an excluded column changed. Rolled back.', 1;

    IF EXISTS (
        SELECT 1
        FROM #before AS b
        INNER JOIN #formula AS f ON f.ApprovalRequestId = b.ApprovalRequestId
        INNER JOIN dbo.SalesApprovalRequest AS h ON h.ApprovalRequestId = b.ApprovalRequestId
        WHERE h.TotalActivities <> f.TotalActivities
           OR h.PendingActivities <> f.PendingActivities
           OR h.ApprovedActivities <> f.ApprovedActivities
           OR h.RejectedActivities <> f.RejectedActivities
    )
        THROW 59539, N'ABORT: a non-payment counter still mismatches. Rolled back.', 1;

    /* Rehearsal diagnostics. The transaction is still open. These grids are not committed after-state evidence. */
    SELECT
        N'NonPaymentFormulaMismatchAfterUpdate' AS EvidenceSet,
        COUNT(*) AS MismatchRows
    FROM #before AS b
    INNER JOIN #formula AS f ON f.ApprovalRequestId = b.ApprovalRequestId
    INNER JOIN dbo.SalesApprovalRequest AS h ON h.ApprovalRequestId = b.ApprovalRequestId
    WHERE h.TotalActivities <> f.TotalActivities
       OR h.PendingActivities <> f.PendingActivities
       OR h.ApprovedActivities <> f.ApprovedActivities
       OR h.RejectedActivities <> f.RejectedActivities;

    SELECT
        N'ReadyForPaymentUnexpectedCurrent' AS EvidenceSet,
        h.ApprovalRequestId
    FROM dbo.SalesApprovalRequest AS h
    WHERE h.Status = N'ReadyForPayment'
      AND NOT EXISTS (SELECT 1 FROM #payBefore AS p WHERE p.ApprovalRequestId = h.ApprovalRequestId);

    SELECT
        N'ReadyForPaymentMissingCurrent' AS EvidenceSet,
        p.ApprovalRequestId
    FROM #payBefore AS p
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest AS h
        WHERE h.ApprovalRequestId = p.ApprovalRequestId
          AND h.Status = N'ReadyForPayment'
    );

    SELECT
        N'ReadyForPaymentSnapshotDifference' AS EvidenceSet,
        p.ApprovalRequestId
    FROM #payBefore AS p
    INNER JOIN dbo.SalesApprovalRequest AS h ON h.ApprovalRequestId = p.ApprovalRequestId
    WHERE h.Status <> p.Status
       OR h.TotalActivities <> p.TotalActivities
       OR h.PendingActivities <> p.PendingActivities
       OR h.ApprovedActivities <> p.ApprovedActivities
       OR h.RejectedActivities <> p.RejectedActivities
       OR h.ReturnedActivities <> p.ReturnedActivities
       OR EXISTS (SELECT h.ModifiedOn EXCEPT SELECT p.ModifiedOn)
       OR EXISTS (SELECT h.ModifiedBy EXCEPT SELECT p.ModifiedBy)
       OR EXISTS (SELECT h.CurrentApproverLoginId EXCEPT SELECT p.CurrentApproverLoginId)
       OR EXISTS (SELECT h.CurrentApproverLevel EXCEPT SELECT p.CurrentApproverLevel)
       OR EXISTS (SELECT h.FinalApprovedAmount EXCEPT SELECT p.FinalApprovedAmount)
       OR EXISTS (SELECT h.ApprovedKM EXCEPT SELECT p.ApprovedKM)
       OR EXISTS (SELECT h.ApprovedClaimAmount EXCEPT SELECT p.ApprovedClaimAmount)
       OR EXISTS (SELECT h.ApprovedDAAmount EXCEPT SELECT p.ApprovedDAAmount)
       OR EXISTS (SELECT h.ApprovedHotelAmount EXCEPT SELECT p.ApprovedHotelAmount)
       OR EXISTS (SELECT h.ApprovedTravelAmount EXCEPT SELECT p.ApprovedTravelAmount)
       OR EXISTS (SELECT h.OriginalClaimAmount EXCEPT SELECT p.OriginalClaimAmount)
       OR EXISTS (SELECT h.TotalDeductionAmount EXCEPT SELECT p.TotalDeductionAmount);

    SELECT
        N'ExcludedColumnDifference' AS EvidenceSet,
        b.ApprovalRequestId
    FROM #before AS b
    INNER JOIN dbo.SalesApprovalRequest AS h ON h.ApprovalRequestId = b.ApprovalRequestId
        WHERE h.ReturnedActivities <> b.ReturnedActivities
           OR h.Status <> b.Status
           OR EXISTS (SELECT h.ModifiedOn EXCEPT SELECT b.ModifiedOn)
           OR EXISTS (SELECT h.ModifiedBy EXCEPT SELECT b.ModifiedBy)
           OR EXISTS (SELECT h.CurrentApproverLoginId EXCEPT SELECT b.CurrentApproverLoginId)
           OR EXISTS (SELECT h.CurrentApproverLevel EXCEPT SELECT b.CurrentApproverLevel)
           OR EXISTS (SELECT h.FinalApprovedAmount EXCEPT SELECT b.FinalApprovedAmount)
           OR EXISTS (SELECT h.ApprovedKM EXCEPT SELECT b.ApprovedKM)
           OR EXISTS (SELECT h.ApprovedClaimAmount EXCEPT SELECT b.ApprovedClaimAmount)
           OR EXISTS (SELECT h.ApprovedDAAmount EXCEPT SELECT b.ApprovedDAAmount)
           OR EXISTS (SELECT h.ApprovedHotelAmount EXCEPT SELECT b.ApprovedHotelAmount)
           OR EXISTS (SELECT h.ApprovedTravelAmount EXCEPT SELECT b.ApprovedTravelAmount)
           OR EXISTS (SELECT h.OriginalClaimAmount EXCEPT SELECT b.OriginalClaimAmount)
           OR EXISTS (SELECT h.TotalDeductionAmount EXCEPT SELECT b.TotalDeductionAmount);

    SELECT
        N'CounterReconciliation' AS EvidenceSet,
        b.ApprovalRequestId,
        b.TotalActivities AS OldTotal,
        f.TotalActivities AS ProposedTotal,
        h.TotalActivities AS CurrentTotal,
        b.PendingActivities AS OldPending,
        f.PendingActivities AS ProposedPending,
        h.PendingActivities AS CurrentPending,
        b.ApprovedActivities AS OldApproved,
        f.ApprovedActivities AS ProposedApproved,
        h.ApprovedActivities AS CurrentApproved,
        b.RejectedActivities AS OldRejected,
        f.RejectedActivities AS ProposedRejected,
        h.RejectedActivities AS CurrentRejected
    FROM #before AS b
    INNER JOIN #formula AS f ON f.ApprovalRequestId = b.ApprovalRequestId
    INNER JOIN dbo.SalesApprovalRequest AS h ON h.ApprovalRequestId = b.ApprovalRequestId
    WHERE b.TotalActivities <> f.TotalActivities
       OR b.PendingActivities <> f.PendingActivities
       OR b.ApprovedActivities <> f.ApprovedActivities
       OR b.RejectedActivities <> f.RejectedActivities;

    SELECT
        N'HeaderStatusDistribution' AS EvidenceSet,
        h.Status,
        COUNT(*) AS RequestRows
    FROM dbo.SalesApprovalRequest AS h
    GROUP BY h.Status;

    SELECT
        N'UpdateCount' AS EvidenceSet,
        @updated AS UpdatedRows,
        @target AS TargetRows;

    IF @CommitMode <> N'COMMIT-APPROVED'
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 59537, N'ABORT: this revision rolls back. No row was committed.', 1;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
```

`#payBefore` is the locked `ReadyForPayment` snapshot. Error `59540` compares the row count, the snapshotted column values, and the row set: a current `ReadyForPayment` id absent from `#payBefore`, or a `#payBefore` id that is no longer `ReadyForPayment`, fails it. That is not a formula-mismatch calculation. `ReadyForPaymentFormulaMismatch` is the separate diagnostic selected before the update, and those rows are not part of `@target`. `CounterReconciliation` returns `ApprovalRequestId` with the old, proposed, and current four counters for the update target. The summary grids remain. All of those results are selected while the transaction is open. `@CommitMode` remains `ROLLBACK-ONLY`, so this revision then rolls back and throws `59537`. It does not reach `COMMIT`. The historical procedure hash was not recomputed. Live triggers were not read.
