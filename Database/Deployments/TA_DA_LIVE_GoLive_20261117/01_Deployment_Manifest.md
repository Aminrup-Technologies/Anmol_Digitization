# TA/DA LIVE deployment manifest

**Additive notice, 2026-11-18.** Read-only comparison of `AnmolDristiDev` and `AnmolDristi` found WP-01 and WP-02 already present and matched to Dev. Do not run `20_WP01_Schema.sql` or `30_WP02_KM_Procedure.sql`. WP-03 remains excluded. Formatting-only procedure differences are not a replacement. The evidence table is still absent, so this package is not the recorded source of the current modules. Detail: `docs/reviews/Post_GoLive/LIVE_TA_DA_Reconciliation_20261117/12_Dev_LIVE_ReadOnly_Reconciliation_20261118.md`. The historical scope statement below is retained and is not a current execute instruction.

**Closure status: DOCUMENTATION UPDATED — NO-CHANGE DEPLOYMENT DECISION PENDING FORMAL ACCEPTANCE.** Scripts `20`, `30`, `40`, `60`, and `61` are NOT TO BE EXECUTED for this outcome. `DR-NOCHANGE` is unsigned. Backup verification, isolated restore, and runtime financial verification are not claimed here.

**Status: BLOCKED.** Nothing in this folder has been executed. No gate is signed. Executable scope is WP-01 and WP-02. WP-03 is parked.

| Item | Value |
| --- | --- |
| Branch | `Aug26_AILWorks_AmimnrupSupport` |
| HEAD this package was built from | `c9eba192a1e504a0c8e4da2869a1b57d29246cf4` |
| Server | `MUMANMOAPP01\SQLEXPRESS` |
| Database | `AnmolDristi` only. Every state-changing script aborts on any other name, including `AnmolDristiDev` |
| Fingerprint source | Quoted from `docs/reviews/Post_GoLive/LIVE_TA_DA_Reconciliation_20261117/10_LIVE_Discovery_Capture.md`. This package did not reconnect to LIVE |
| KM candidate | `21975c2` (`21975c200d9c9bed1f24a7b97ec802ff22faa2f4`). Git blob `4c5ec0b4aa43b7237dac23a0274ece6d426fe8cb`, same blob at HEAD. Canonical stored form replaces the one token `CREATE OR ALTER PROCEDURE` with `CREATE   PROCEDURE`. Local UTF-16LE SHA-256 of that text is `F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9`, equal to the UAT module in `C_Apply_DesignationIdCorrection_UAT.sql`. This check did not connect to SQL Server |
| Counter candidate | Parked. `40_WP03_ActivityCounters.sql` is `EXCLUDED — DO NOT EXECUTE`. Owner scope decision A keeps LIVE `dbo.usp_RecalculateApprovalRequest` |
| Claim callee | `dbo.usp_RecalculateSalesClaim` is not modified. The complete LIVE text was supplied as conversation evidence on 2026-11-18. Identity: object id 743009728, captured 2026-11-17 23:21:37.3964546 +05:30, modified 2026-10-14 13:33:28.887, 28786 characters, 57572 bytes, SHA-256 `FC60A9CFC68BA480454A1D4A4653EAA9426D281267E6750AC3CD96DCC9A33ABC`. The body is not stored in this repository, so `@ClaimEvidenceFiled` remains `NOT-FILED`. Source-level temporary-table comparison is MATCH. Transaction acceptance is unsigned |

## Work packages

| Package | Change | Script | Rolls back with |
| --- | --- | --- | --- |
| WP-01 | Add nullable `ApprovalRequestId int`, `ApproverRole nvarchar(20)`, `ApproverLevel int` on `dbo.SalesDailyActivityKMHistory`. No default, foreign key, index, check, or backfill | `20_WP01_Schema.sql` | `61_Rollback_WP01_Columns.sql` |
| WP-02 | Replace `dbo.usp_UpdateActivityKM_WithHistory` with the verified `F6B07BE8` module | `30_WP02_KM_Procedure.sql` | `60_Rollback_Procedures.sql` |

**WP-03 exclusion.** Owner scope decision A (2026-11-18) removes activity counters from this executable package. `40_WP03_ActivityCounters.sql` is parked and marked `EXCLUDED — DO NOT EXECUTE`. Its first statement throws `59520`. No execution step, master runner, or rollback in this folder calls it. LIVE `dbo.usp_RecalculateApprovalRequest` stays at the quoted fingerprint. C-1, C-3, and DR-COUNTER stay unsigned and do not gate WP-01 or WP-02.

This package does not edit `usp_ProcessApprovalAction`, `usp_MarkRequestReadyForPayment`, or `usp_ForwardApprovalRequest`.

## Out of the package

- `dbo.usp_RecalculateApprovalRequest`, including both amount formulas already present in the two repository files. File 40 does not run.
- Financial formulas inside `usp_RecalculateSalesClaim`.
- `usp_GetResolvedPolicy`, `usp_ResolveDesignation`, `usp_ResolveRegion`, `usp_ResolveBranch`, `usp_ResolveGrade`.
- Process Approval, Forward Approval, Mark Ready.
- Application pages, employee KM modification limits, `Web.config`, ITSM, HRMS, VMS.
- `Database/Deployments/ApproverKmEdit/`, `Database/Deployments/ApprovalRequestActivityCounters/`, `Database/Deployments/AuditorKmFinancial/`, and `Database/Deployments/L2R19_UAT/`. Those scripts target another catalog or another body. Do not run them against `AnmolDristi`.

## Quoted LIVE fingerprints

Algorithm, as recorded in the discovery file: `HASHBYTES('SHA2_256', CONVERT(varbinary(max), definition))`, uppercase hex. `LEN` and `DATALENGTH` are of that same `nvarchar` module.

| Object | LEN | DATALENGTH | SHA-256 |
| --- | --- | --- | --- |
| `dbo.usp_UpdateActivityKM_WithHistory` | 15455 | 30910 | `EC6A97B97605B2A12857367B78C7C84F277289443DFF267C4BB29118E258E100` |
| `dbo.usp_RecalculateApprovalRequest` | 7567 | 15134 | `08F34AF8C5CC188F2A0877985FB7FC5E70DEB0067034B95264AE6EF4739ED5D7` |
| `dbo.usp_RecalculateSalesClaim` | 28786 | 57572 | `FC60A9CFC68BA480454A1D4A4653EAA9426D281267E6750AC3CD96DCC9A33ABC` |
| `dbo.usp_GetResolvedPolicy` | 3297 | 6594 | `99ADB00C37DE0C62400F90B0CE4F6BB195A98129267A7221E490E196ACC2DA90` |
| `dbo.usp_ResolveDesignation` | 1008 | 2016 | `3E65716AB518D0B17C59F2EAACF468CA6711575A0A98E4CC099750C460FE86E0` |
| `dbo.usp_ProcessApprovalAction` | 42031 | 84062 | `FF87E2AAD1319EB48F8237ED9293F674FE4F508E64F39CFDC41DBBA24AFC1392` |
| `dbo.usp_MarkRequestReadyForPayment` | 11189 | 22378 | `2D565A377B668BFC23611F8288B8532B1F24727609EFA32394900B95EC6A69E2` |
| `dbo.usp_ForwardApprovalRequest` | 16459 | 32918 | `02BC07F01C3EDB0B4519561427F679D6B897DBC74527BFC518970F0C000A9F77` |

`usp_ResolveRegion`, `usp_ResolveBranch`, and `usp_ResolveGrade` have no fingerprint in that capture. WP-02 aborts until a later reviewed revision pins the three hashes that preflight prints.

The WP-02 postimage is the canonical module hash `F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9`. The apply script submits that text with `CREATE   PROCEDURE` replaced by `ALTER   PROCEDURE`, then accepts the stored module only when replacing `ALTER   PROCEDURE` back to `CREATE   PROCEDURE` equals the canonical text. That round-trip is the convention in `C_Apply_DesignationIdCorrection_UAT.sql`. This task did not re-execute it.

## Evidence table

The deployment identity does not create `dbo.TaDaLiveGoLive_20261117_Preimage`. `CREATE TABLE` on SQL Server also requires `ALTER` on schema `dbo`, and schema `ALTER` can change every object in `dbo`. That is not a permission of this identity. A DBA may create the table under a separate authorization that this package does not grant and does not put in the execution order. Until the table exists with the definition below, scripts `20`, `30`, `60`, and `61` abort.

```sql
CREATE TABLE dbo.TaDaLiveGoLive_20261117_Preimage
(
    WorkPackage varchar(8) NOT NULL CONSTRAINT PK_TaDaLiveGoLive_20261117_Preimage PRIMARY KEY,
    ObjectName sysname NOT NULL,
    PreimageSha256 char(64) NULL,
    PreimageLen int NULL,
    PreimageDataLength int NULL,
    ModuleDefinition nvarchar(max) NULL,
    SubmittedSha256 char(64) NULL,
    AppliedSha256 char(64) NULL,
    HistoryRowCount bigint NULL,
    HistoryIdMin int NULL,
    HistoryIdSum bigint NULL,
    SchemaSnapshot nvarchar(max) NULL,
    CapturedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_TaDaLiveGoLive_20261117_Captured DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_TaDaLiveGoLive_20261117_WorkPackage CHECK (WorkPackage IN ('WP01', 'WP02'))
);
```

The check must not allow `WP03`. Rollback reads the WP-02 row and aborts if its hash differs from the KM fingerprint above. Dropping the table is not part of rollback. Creating it is not authorized by this task.

## Least-privilege permission matrix

This matrix is not a grant and not an approval. No login is named. `DR-IDENTITY` stays unsigned. A prior `sa` read-only discovery is not this identity.

One deployment login runs WP-01, WP-02, and their rollback. It is not `sa`. It is not a member of `sysadmin`, `db_owner`, or `db_ddladmin`. It does not have `CONTROL` on the database, on schema `dbo`, or on any object below. It does not have `ALTER` on schema `dbo` and does not have `ALTER ANY PROCEDURE`. The scripts issue no `GRANT`.

| Statement | Script | Permission | Object |
| --- | --- | --- | --- |
| `ALTER TABLE ... ADD` | `20` | `ALTER` | `dbo.SalesDailyActivityKMHistory` |
| `SELECT` of the row proof | `20`, `61` | `SELECT` | `dbo.SalesDailyActivityKMHistory` |
| `ALTER TABLE ... DROP COLUMN` | `61` | `ALTER` | `dbo.SalesDailyActivityKMHistory` |
| `INSERT` of the evidence row | `20`, `30` | `INSERT` | `dbo.TaDaLiveGoLive_20261117_Preimage` |
| `UPDATE` of `AppliedSha256` | `30` | `UPDATE` | `dbo.TaDaLiveGoLive_20261117_Preimage` |
| `SELECT` of the evidence row | `20`, `30`, `50`, `60`, `61` | `SELECT` | `dbo.TaDaLiveGoLive_20261117_Preimage` |
| `ALTER PROCEDURE` after the token swap | `30`, `60` | `ALTER` | `dbo.usp_UpdateActivityKM_WithHistory` |
| `OBJECT_DEFINITION` and `sys.sql_modules` | `10`, `20`, `30`, `50`, `60`, `61` | `VIEW DEFINITION` | schema `dbo` |
| `backup_finish_date` lookup | `20`, `30` | `SELECT` | `msdb.dbo.backupset` |

Denied, and checked by the state-changing scripts: `ALTER` and `CONTROL` on `dbo.usp_RecalculateApprovalRequest`, `dbo.usp_RecalculateSalesClaim`, `dbo.usp_GetResolvedPolicy`, `dbo.usp_ResolveDesignation`, `dbo.usp_ResolveRegion`, `dbo.usp_ResolveBranch`, `dbo.usp_ResolveGrade`, `dbo.usp_ProcessApprovalAction`, `dbo.usp_MarkRequestReadyForPayment`, `dbo.usp_ForwardApprovalRequest`, and `dbo.usp_UpdateActivityKM`. Also `CONTROL` on `dbo.usp_UpdateActivityKM_WithHistory`, because rollback uses `ALTER` and does not `DROP`. Also `INSERT`, `UPDATE`, `DELETE`, and `CONTROL` on the history table, and `DELETE`, `ALTER`, and `CONTROL` on the evidence table.

Preflight and postflight are read-only. They need `VIEW DEFINITION` on schema `dbo`. They do not need the write permissions.

The earlier sentence in this file that required `CONTROL` on both procedures is replaced by this matrix. No such grant was made.

## Recovery, before any state-changing script

1. A new full backup of `AnmolDristi`, `type = D`, `is_damaged = 0`, checksums present. The 2026-10-27 rows in the discovery file are not this backup.
2. `RESTORE VERIFYONLY … WITH CHECKSUM` of that device, recorded in `05_Execution_Log_Template.md`.
3. An isolated restore to a new database name. The name must not be `AnmolDristi` or `AnmolDristiDev`.
4. The evidence table above, created by a DBA under a separate unsigned authorization. This package does not create it.
5. A deployment login that matches the matrix. The name is not `sa`.
6. The execution log filled from those results, then a new revision of scripts `20`, `30`, `60`, and `61` that replaces only the unsigned literals named in `03_Decision_Gates.md`. File `40` is not in that revision path.

This revision's literals are `UNSIGNED`, `NOT-FILED`, or blank. Editing them during a live window, without a reviewed file change, is not the approval path.

## Session rules

Run one file at a time with `sqlcmd -b -I -f 65001`. `-b` stops the file after an error. Do not run `40_WP03_ActivityCounters.sql`. Do not use `sa`. A state-changing file expects `@@TRANCOUNT = 0` and aborts otherwise. If a session still has an open transaction after a failure, roll it back before disconnecting.
