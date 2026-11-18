# KM audit schema delta proposal

**Status: BLOCKED.** Proposal only. No `ALTER TABLE` is authorized, and no script in this phase creates one.

## What the repository table contains (CONFIRMED)

`Database/Tables/SalesDailyActivityKMHistory.sql` was extracted 2026-08-02 and last committed in `1f1e8f2`. It does not define `ApprovalRequestId`, `ApproverRole`, or `ApproverLevel`.

Existing objects in that script, which this proposal leaves in place:

| Object | Definition in the table script |
| --- | --- |
| `HistoryId` | `int IDENTITY(1,1) NOT NULL`, clustered primary key |
| `ActivityId` | `int NOT NULL`, foreign key `FK_KMHistory_Activity` to `dbo.SalesDailyActivity(ActivityId)` |
| `OldKM`, `NewKM` | `decimal(10,2) NULL` |
| `ModifiedBy` | `nvarchar(100) NULL` |
| `ModifiedDateTime` | `datetime NOT NULL`, default `dateadd(minute,(330),sysutcdatetime())` |
| `Reason` | `nvarchar(1000) NULL` |
| Route and travel columns | `RouteOriginName nvarchar(255) NULL`, `RouteDestinationName nvarchar(255) NULL`, `RouteWaypointsJSON nvarchar(max) NULL`, `TravelMode nvarchar(20) NULL` |
| Index | `IX_SalesDailyActivityKMHistory_Activity` on `(ActivityId, ModifiedDateTime DESC)` |

## Intended additive columns (CONFIRMED in the UAT apply script, NOT VERIFIED on LIVE)

`Database/Deployments/ApproverKmEdit/B_Apply_ApproverKmEdit_UAT.sql` adds the columns only when `COL_LENGTH` is NULL, then checks the resulting type. `C_Apply_DesignationIdCorrection_UAT.sql` checks the same type and does not add columns. Neither script is a LIVE script: both abort when `DB_NAME()` is `AnmolDristi`.

| Column | Type and nullability in `B_Apply` | Default | FK or index in `B_Apply` | Meaning in `usp_UpdateActivityKM_WithHistory` |
| --- | --- | --- | --- | --- |
| `ApprovalRequestId` | `int NULL` | none | none | The single approval request that owns the activity at save time. |
| `ApproverRole` | `nvarchar(20) NULL` (`max_length` 40 in the script's check) | none | none | Canonical role stored by the procedure: `L1`, `L2`, `L3`, or `Auditor`. `Reporting_Manager1` is stored as `L1`. |
| `ApproverLevel` | `int NULL` | none | none | `SalesApprovalRequest.CurrentApproverLevel` at save time. |

`@CanonicalRole` is declared `NVARCHAR(20)`. The column width matches that variable. `ApproverRole` on `SalesApprovalRequestApprover` is read into `NVARCHAR(100)` before the canonical mapping. The history column stores the mapped value, not the raw role string.

## Smallest change that preserves existing rows

Add the three columns exactly as `B_Apply` specifies: nullable, no default, no foreign key, no new index, no rebuild, no rename, no drop.

Existing rows keep their current values. The new columns are NULL on those rows because the script supplies no default. NULL means the row was written before the columns existed, or was written by a statement that does not name them. It is not a backfill of approver identity. This proposal does not update historical rows.

Compatibility that is visible in source:

- `Database/Stored Procedures/Submission/usp_UpdateActivityKM.sql` inserts an explicit column list of the original ten columns. A nullable addition does not force that statement to supply the new names. Employee KM history stays without approver identity.
- `Database/Stored Procedures/Retrieval/usp_GetSalesDailyActivityKMHistory.sql` selects named original columns. It does not require the new columns.
- The approver procedure's `INSERT` names the three new columns. That statement fails if any of them is absent.

## References found

| Location | Use |
| --- | --- |
| `usp_UpdateActivityKM_WithHistory` | Inserts the three columns. |
| `usp_UpdateActivityKM` | Inserts the original columns only. |
| `usp_GetSalesDailyActivityKMHistory` | Selects the original columns only. |
| `B_Apply`, `C_Apply`, `D_Rollback` under `ApproverKmEdit` | UAT apply, type check, and a UAT rollback that can drop the columns. |
| C# pages `ClaimsApproval_v2.aspx.cs`, `ClaimsApproval.aspx.cs`, `SalesMonthlyReview.aspx.cs`, `MyClaimsView.aspx.cs`, `MyClaimsView_v2.aspx.cs`, `ApprovedClaimsView.aspx.cs` | Reference the history table or the KM procedures. No page was edited. |

No check constraint, foreign key, or index on the three columns exists in the ApproverKmEdit scripts.

## Unresolved decisions

These are not guessed:

1. Current LIVE absence of the three columns is SUPPLIED EVIDENCE ONLY.
2. If LIVE already has any of the names with a different type, length, or nullability, the additive `ADD` is the wrong action. Stop and return the actual `sys.columns` row.
3. A foreign key to `SalesApprovalRequest` is not in the UAT script. This proposal does not add one.
4. An index on `(ApprovalRequestId)` is not in the UAT script. This proposal does not add one.
5. `D_Rollback_ApproverKmEdit_UAT.sql` drops the three columns when every value is NULL. That script is not a LIVE rollback. See document 05.
6. Whether past approver edits should be reconstructed is an owner decision. This proposal does not backfill.

## Prerequisite before any future DDL

Read the current `AnmolDristi` column list, types, nullability, defaults, constraints, and indexes. Capture that result as evidence. Confirm a fresh full backup of `AnmolDristi` and a named least-privilege login. Until those exist, this delta stays a proposal.
