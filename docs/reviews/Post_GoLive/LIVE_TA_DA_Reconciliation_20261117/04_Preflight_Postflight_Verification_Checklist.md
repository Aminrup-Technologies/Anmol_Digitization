# Preflight and postflight verification checklist

**Status: BLOCKED.** None of these checks was run. They are the checks a later, separately authorized read-only session would run before any apply. They are not a deployment script.

**Additive scope note, 2026-11-18.** Owner scope decision A excludes WP-03. Rows F5 and F8 below remain the earlier counter-candidate checks. They are not acceptance criteria for the WP-01/WP-02 package. The recalc fingerprint must stay unchanged.

Record the operator, the login, the clock, and the full result text. A blank cell is not a pass.

## Identity, before any catalog query

| # | Check | Required result | This review |
| --- | --- | --- | --- |
| P0 | Session login is a named least-privilege account. `IS_SRVROLEMEMBER('sysadmin')` and membership of `db_owner` are known. | Login is not `sa`. The account can read definitions. It cannot alter objects during the read. | NOT VERIFIED. Supplied evidence says an earlier audit used `sa`. |
| P1 | `@@SERVERNAME` and `DB_NAME()`. | `MUMANMOAPP01\SQLEXPRESS` and `AnmolDristi` for a LIVE read. Any other catalog stops the LIVE checklist. | NOT VERIFIED. No connection was opened. |
| P2 | `@@TRANCOUNT`. | 0 at the start of the read. | NOT VERIFIED. |

## Backup and recovery, before any future apply

| # | Check | Required result | This review |
| --- | --- | --- | --- |
| P3 | Newest full backup of `AnmolDristi` in `msdb.dbo.backupset`: finish time, device, `is_damaged`, checksum flag, size. | A full backup taken immediately before the change window, `is_damaged = 0`, checksums present. | NOT VERIFIED. Quoted checklist record, not a current read: newest finish cited there is `2026-10-13T13:39:34.000Z`, device `D:\anmoldb\Backups\AnmolDristi_23Sep26.bak`. That file path also held older backups, so the path alone does not identify the set. |
| P4 | `RESTORE VERIFYONLY … WITH CHECKSUM` of that new backup, on a decision recorded by the DBA. | Verify completes for the backup that will be the restore source. | NOT VERIFIED. No such result was supplied for this review. |
| P5 | Restore rehearsal to a database name that is not `AnmolDristi` and not the application catalog. | Owner decision. This review does not require a rehearsal to be invented. | NOT VERIFIED. |

The quoted 2026-10-13 backup is historical evidence. It does not clear P3 or P4 for a change on or after 2026-11-17.

## Schema preimage

| # | Check | Required result | This review |
| --- | --- | --- | --- |
| P6 | `sys.columns` and `sys.types` for `dbo.SalesDailyActivityKMHistory`, plus defaults, indexes, foreign keys, and check constraints. | Full current list saved. The three audit names are confirmed absent, or their exact type is saved if they exist. | NOT VERIFIED. Source table script lacks the three names. Supplied evidence says LIVE lacks them. |
| P7 | Row count of `dbo.SalesDailyActivityKMHistory`. | Saved. Postflight count must equal this count. | NOT VERIFIED. |
| P8 | `sys.columns` for `TotalActivities`, `PendingActivities`, `ApprovedActivities`, `RejectedActivities`, `ReturnedActivities` on `dbo.SalesApprovalRequest`. | Types and nullability saved. Repository script shows `int NOT NULL` with default 0, including `ReturnedActivities`. | NOT VERIFIED on LIVE. |

## Procedure preimage

For each procedure, save `OBJECT_DEFINITION`, `LEN`, `DATALENGTH`, `modify_date`, and `HASHBYTES('SHA2_256', CONVERT(varbinary(max), definition))`. Compare that hash only with a hash computed the same way on the same Unicode text. Do not compare it with a git blob hash.

| # | Procedure | What to record | This review |
| --- | --- | --- | --- |
| P9 | `dbo.usp_UpdateActivityKM_WithHistory` | Full text. Presence or absence of `CategoryId` and `Designation_ID`. Insert column list. | NOT VERIFIED. Supplied evidence: LIVE references `CategoryId`. Source uses `Designation_ID`. |
| P10 | `dbo.usp_RecalculateApprovalRequest` | Full text. Whether the four counter names occur. Whether `ReadyForPayment` returns before writes. Whether the module contains `BEGIN TRAN` or `COMMIT`. The approved-amount expression. | NOT VERIFIED. Supplied evidence: no detected counter-column references. |
| P11 | `dbo.usp_RecalculateSalesClaim` | Full text, because the KM procedure calls it. | NOT VERIFIED. No repository body exists. |
| P12 | `dbo.usp_GetResolvedPolicy` | Full text, in particular the `MST_UserMaster` column used for designation. | NOT VERIFIED. Source uses `CategoryId`. |
| P13 | `dbo.usp_ResolveDesignation` | Full text. | NOT VERIFIED. |
| P14 | `dbo.usp_ProcessApprovalAction` | Full text of the Auditor terminal branch and the recalc call. | NOT VERIFIED. Quoted 2026-10-14 catalog row is not a current definition. |
| P15 | `dbo.usp_MarkRequestReadyForPayment` | Full text, including which header statuses it accepts. | NOT VERIFIED. Quoted 2026-10-14 hash is not a current definition. |
| P16 | `dbo.usp_ForwardApprovalRequest` | Full text, including error 51012. | NOT VERIFIED. |

Also save definitions of `usp_ResolveRegion`, `usp_ResolveBranch`, and `usp_ResolveGrade`, because the KM procedure calls them. Their source was not part of the eight-procedure comparison.

## Stop before any apply

Stop, and leave the catalog unchanged, when any of these is true:

1. P0–P16 are incomplete.
2. The server or database is not the named LIVE target.
3. The login is `sa`, or the login's rights are broader than the later change needs.
4. The backup verify in P4 is missing.
5. An audit column exists with a type other than the proposal in document 02.
6. The LIVE KM module is neither an owner-accepted preimage nor already the `Designation_ID` candidate.
7. The LIVE recalc module's financial section differs from the Approval parent and C-3 has no owner decision.
8. `usp_RecalculateSalesClaim` is missing or its captured body is not accepted.
9. Another session holds an open transaction or a schema lock on these objects.
10. The working tree or a UAT script is about to be executed with its catalog guard removed.

## Postflight, only after a future authorized apply

| # | Check | Required result |
| --- | --- | --- |
| F1 | Same login, server, and database as the preimage. | Unchanged identity. |
| F2 | History row count. | Equal to P7. No row deleted. |
| F3 | The three new columns. | `int NULL`, `nvarchar(20) NULL`, `int NULL`. No default, FK, or extra index. All existing rows NULL in those columns. |
| F4 | KM procedure module. | Matches the accepted candidate after SQL Server's `CREATE`/`ALTER` normalization. `Designation_ID` is the designation source. The insert names the three columns. |
| F5 | Recalc procedure module. | Matches the accepted Approval candidate. `ReadyForPayment` still returns before writes. The approved-amount `SUM` is the accepted one. The four counters are assigned from `RN = 1`. `ReturnedActivities` is still not assigned, unless the owner added that assignment in the accepted candidate. |
| F6 | Untouched modules. | `OBJECT_DEFINITION` of `usp_RecalculateSalesClaim`, `usp_GetResolvedPolicy`, `usp_ResolveDesignation`, `usp_ProcessApprovalAction`, `usp_MarkRequestReadyForPayment`, and `usp_ForwardApprovalRequest` equals the preimage byte for byte. |
| F7 | One non-payment request, read only, chosen by the owner. | Latest-detail counts can be recomputed by a `SELECT` and compared with the header. This review does not authorize an `EXEC` of recalc to force that comparison. |
| F8 | A `ReadyForPayment` request, read only. | Header amounts and counters equal the preimage for that request, because the candidate returns before writing. |
| F9 | `@@TRANCOUNT` and the script's own commit message. | The apply transaction committed, or it rolled back and F2–F6 show the preimage. |

F1–F9 have no result in this review. They must not be marked PASS from the repository text.
