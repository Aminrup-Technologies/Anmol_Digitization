# Dev-to-LIVE read-only reconciliation, 2026-11-18

**Status: VERIFIED comparison. Deployment PREPARED — NOT EXECUTED. No state-changing script was run.**

**Closure status: DOCUMENTATION UPDATED — NO-CHANGE DEPLOYMENT DECISION PENDING FORMAL ACCEPTANCE.**

**Disposition.** Feature implementation for the tested TA/DA scope is CLOSED. Dev-to-LIVE SQL reconciliation for that scope is COMPLETE: no further functional SQL deployment is required. DBA/IT governance sign-off is pending. `DR-NOCHANGE`, C-1, and C-3 remain unsigned. Runtime financial and payment verification remain NOT TESTED. A local Git commit of these files is not a deployment, a backup verification, or a signature.

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
