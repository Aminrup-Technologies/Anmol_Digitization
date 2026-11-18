# Evidence request register

**Additive notice, 2026-11-17 discovery.** A read-only session later captured current `AnmolDristi` fingerprints. Those observations are in `10_LIVE_Discovery_Capture.md`. They do not by themselves accept a row or sign a decision. Where this register says an item was not captured, read `10` for the items it actually lists.

**Status: BLOCKED.** Action list for a later read-only capture. Analysis stays in `01`–`06` of this folder. Those six files are not amended by this register.

Capture target, when the DBA is separately authorized to read: database `AnmolDristi` on `MUMANMOAPP01\SQLEXPRESS`. This register was not executed. No connection was opened.

## Evidence rule

A script-declared hash, a git blob, a source file, an old backup row, or a historical checklist entry is not a current LIVE preimage and is not a deployment approval.

A capture counts only when all of the following are true:

1. The session recorded `@@SERVERNAME`, `DB_NAME()`, `SUSER_SNAME()`, and the clock.
2. The catalog is `AnmolDristi` and the server is `MUMANMOAPP01\SQLEXPRESS`.
3. The saved object text is the full `sys.sql_modules.definition` for that session, not a excerpt and not a file from git.
4. `LEN`, `DATALENGTH`, `modify_date`, and `HASHBYTES('SHA2_256', CONVERT(varbinary(max), definition))` were computed on that saved text in that session.
5. The acceptance reviewer named below signs the capture. An unsigned file does not close a gate.

Hashes inside `B_Apply_ApproverKmEdit_UAT.sql`, `C_Apply_DesignationIdCorrection_UAT.sql`, `B_Apply_ActivityCounters_UAT.sql`, and `TA_DA_Deployment_Checklist.md` fail this rule. They remain historical or script-declared only.

## How to capture a procedure

Run one read-only query per object. Save the full `definition` column and the fingerprint columns together. Do not run `ALTER`, `CREATE`, `DROP`, `EXEC` of the procedure, or any statement that writes.

```sql
SELECT
      @@SERVERNAME AS server_name
    , DB_NAME() AS catalog_name
    , SUSER_SNAME() AS login_name
    , SYSDATETIMEOFFSET() AS captured_at
    , o.modify_date
    , LEN(m.definition) AS module_len
    , DATALENGTH(m.definition) AS module_bytes
    , CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), m.definition)), 2) AS sha256_utf16le
    , m.definition
FROM sys.sql_modules AS m
INNER JOIN sys.objects AS o ON o.object_id = m.object_id
WHERE m.object_id = OBJECT_ID(N'dbo.<procedure_name>', N'P');
```

Completeness for every procedure row: one row returned; `definition` is not NULL; the text ends at the procedure's final `END`; the hash in the result matches a recomputation over the saved text; login, server, catalog, and time are on the same result.

## How to capture a table

Save columns, defaults, indexes, foreign keys, and check constraints from `sys.columns`, `sys.types`, `sys.default_constraints`, `sys.indexes`, `sys.foreign_keys`, and `sys.check_constraints` for that `object_id`. Save `COUNT_BIG(*)` for the history table. Do not add, drop, or update columns.

## Register

| ID | Evidence | Object | Capture | Completeness | Responsible role | Acceptance reviewer | Gate it closes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| EV-KM | Current module of the approver KM procedure | `dbo.usp_UpdateActivityKM_WithHistory` | Procedure query above | Full text plus fingerprint. Reviewer can see whether `CategoryId` or `Designation_ID` is the column read into `@DesignationRaw`, and whether the `INSERT` names `ApprovalRequestId`, `ApproverRole`, and `ApproverLevel`. | DBA, read-only | IT / DB owner | DR-KM and the KM half of the stop/go matrix. Maps to E-1 in `06`. |
| EV-RECALC | Current module of the single database procedure | `dbo.usp_RecalculateApprovalRequest` | Procedure query above | Full text plus fingerprint. Reviewer can see the amount `SUM`, any `BEGIN TRAN` / `COMMIT`, the `ReadyForPayment` path, and whether `TotalActivities`, `PendingActivities`, `ApprovedActivities`, `RejectedActivities`, or `ReturnedActivities` are assigned. | DBA, read-only | IT / DB owner | DR-C3 and DR-COUNTER. Maps to E-2. Neither repository file is accepted in place of this capture. |
| EV-CLAIM | Current module of the claim recalculation procedure | `dbo.usp_RecalculateSalesClaim` | Procedure query above | Full text plus fingerprint. There is no `CREATE PROCEDURE` for this name in the repository, so a missing row is a failed capture, not an empty pass. The reviewer records parameters, temp-table writes to `#RecalcClaimActivity` and `#RecalcClaimTotals`, and what header and detail amounts it updates. | DBA, read-only | IT / DB owner | DR-KM. The KM candidate calls this procedure. Without EV-CLAIM, `21975c2` cannot be accepted for LIVE. Maps to E-3. |
| EV-POLICY | Current policy and designation modules | `dbo.usp_GetResolvedPolicy`, `dbo.usp_ResolveDesignation` | Procedure query, once each | Full text. Reviewer records which `MST_UserMaster` column supplies designation when the designation parameter is empty. | DBA, read-only | IT / DB owner, with BPO (Sales) informed | DR-KM. Maps to part of E-4. These modules are capture-only. This package does not change them. |
| EV-WORKFLOW | Current workflow modules | `dbo.usp_ProcessApprovalAction`, `dbo.usp_MarkRequestReadyForPayment`, `dbo.usp_ForwardApprovalRequest` | Procedure query, once each | Full text. Reviewer records the Auditor terminal write in the first, the header statuses accepted by the second, and error `51012` in the third. | DBA, read-only | Release owner | DR-C1. Maps to the rest of E-4. Quoted 2026-10-14 checklist hashes do not close this row. |
| EV-RESOLVERS | Current organisation resolvers called by the KM procedure | `dbo.usp_ResolveRegion`, `dbo.usp_ResolveBranch`, `dbo.usp_ResolveGrade` | Procedure query, once each | Full text plus fingerprint, or a recorded absence. | DBA, read-only | IT / DB owner | DR-KM. A missing callee blocks acceptance of `21975c2`. |
| EV-KM-SCHEMA | Current history-table shape and row count | `dbo.SalesDailyActivityKMHistory` | Table capture above, plus `COUNT_BIG(*)` | Every column's type, length, nullability, and default. Indexes, foreign keys, and check constraints listed. Row count saved. Presence or absence of `ApprovalRequestId`, `ApproverRole`, and `ApproverLevel` is explicit. | DBA, read-only | IT / DB owner | DR-COLUMNS. Maps to E-5. The table script `Database/Tables/SalesDailyActivityKMHistory.sql` does not close this row. |
| EV-COUNTER-SCHEMA | Current counter columns | `dbo.SalesApprovalRequest` columns `TotalActivities`, `PendingActivities`, `ApprovedActivities`, `RejectedActivities`, `ReturnedActivities` | Column metadata only | Type, nullability, and default for each name, or a recorded absence. | DBA, read-only | IT / DB owner | DR-COUNTER. Maps to E-6. No DDL is requested for these columns. |
| EV-BACKUP | Backup taken for this change, verify, and isolated restore | `AnmolDristi` backup in `msdb`, then a new database name chosen by the DBA | Read `msdb.dbo.backupset` and `backupmediafamily` for the new full backup (`type = 'D'`): finish time, device, size, `is_damaged`, checksum flag. Then `RESTORE VERIFYONLY … WITH CHECKSUM`. Then restore that same backup to a new database that is not `AnmolDristi` and not the application catalog. | Verify output names the device and finishes without error. Isolated restore's `DB_NAME()` is the new name. The restore is not applied onto `AnmolDristi`. The quoted finish `2026-10-13T13:39:34.000Z` does not close this row. | DBA | DBA and CAB | DR-BACKUP. Maps to E-7. |
| EV-LOGIN | Deployment identity | Server principal and database user that would later deploy | Read `SUSER_SNAME()`, `IS_SRVROLEMEMBER('sysadmin')`, and `sys.database_role_members` / `sys.database_permissions` for that user on `AnmolDristi`. | Login name is recorded. `sa` is not the deployment login. Server and database rights are listed. The rights cover only `ALTER` on `dbo.usp_UpdateActivityKM_WithHistory`, `dbo.usp_RecalculateApprovalRequest`, and `dbo.SalesDailyActivityKMHistory`, plus the right to read definitions. | DBA and security owner | Security owner | DR-IDENTITY. Maps to E-8. |
| EV-WINDOW | Shared-application window | Application publish and `Web.config` | Written statement from the application owner. No file in this repository is opened for secrets. | The statement says this window does not publish ITSM, HRMS import, or VMS, and does not change `Web.config` or the LIVE site files. | Application owner | Release owner | DR-GO. Maps to E-9. |

## Dependency that blocks the KM candidate

`dbo.usp_RecalculateSalesClaim` is required evidence, not an optional sample. `Database/Stored Procedures/Approval/usp_UpdateActivityKM_WithHistory.sql` at `21975c2` executes it with `@ApprovalRequestId` after the kilometre update and expects `#RecalcClaimActivity` and `#RecalcClaimTotals` to exist for that call. The repository has no body for that procedure. Until EV-CLAIM is accepted, the KM procedure candidate has no demonstrated financial callee on LIVE, and the stop/go matrix stays **BLOCKED**.

## Related records that do not close a row

| Record | Why it stays open |
| --- | --- |
| `docs/INDEX.md` §8 C-1 and C-3 | Pending conflicts. They name the decision, they do not supply LIVE text. |
| `docs/reviews/Post_GoLive/RC017C_SoftDelete_Face_Refresh.md`, `RC017C_UAT_Evidence_1136.txt`, `Database/Scripts/012_RC017A_RecalculatePendingReturned_Wrapper.sql`, `013_RC017B_TotalExpenseAmount_Ownership_Audit.sql`, `014_RC017C_SoftDeleteFaceRefresh_Proof.sql` | RC-017 material. None of these files is a current LIVE preimage of the objects in this register, and none signs C-1 or C-3. |
| `Database/Deployments/ApproverKmEdit/` and `Database/Deployments/ApprovalRequestActivityCounters/` | UAT or untracked scripts. They abort on `AnmolDristi` or embed a git-side hash. |
| `Database/Stored Procedures/Utilities/usp_RecalculateApprovalRequest.sql` | The competing repository body. It is not a LIVE capture. |
