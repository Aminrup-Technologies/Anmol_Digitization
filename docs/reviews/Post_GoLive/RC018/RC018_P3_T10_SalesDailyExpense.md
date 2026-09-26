# RC-018 Phase 3 / Table 10 — SalesDailyExpense

**Status:** Scripts generated — **not executed**  
**Risk:** High  
**Date:** 2026-09-25  
**Source:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`, `Database/Tables/SalesDailyExpense.sql`  
**Target:** LIVE `AnmolDristi.dbo.SalesDailyExpense`  
**Scope:** Add four nullable approval columns. Existing expense rows are not updated.

---

## 1. Difference summary

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Row count | 2,377 | 30,181 | Preserve LIVE |
| Shared columns (35) | Match | Match | None |
| PK | `ExpenseId` | `ExpenseId` | None |
| Identity | `ExpenseId (1,1)` curr=2,467 | curr=30,271 | Do not reseed |
| `FK_SalesDailyExpense_Activity` | Yes | Yes | None |
| `UQ_SalesDailyExpense_ActivityDateExpenseType_Seq` | Yes | Yes | None |
| Checks `Amount >= 0` and deleted-row rule | Match | Match | None |
| `ApprovalStatus` nvarchar(50) NULL | Yes | **No** | **Add** |
| `ApprovalRemarks` nvarchar(1000) NULL | Yes | **No** | **Add** |
| `ApprovedBy` nvarchar(100) NULL | Yes | **No** | **Add** |
| `ApprovedOn` datetime2(7) NULL | Yes | **No** | **Add** |
| Receipt columns | `AttachmentPath`, `AttachmentFileName`, `AttachmentContent` | Same | Not altered |
| `ClaimStatus` | Present, default `Draft` | Same | Not altered |

UAT does not declare defaults on the four new columns. They stay nullable so existing rows remain NULL.

Latest-version ownership stays on `SalesApprovalRequestDetail`. This package does not change `DetailVersion` or expense foreign keys.

---

## 2. Idempotent schema patch

`Database/Scripts/025_RC018_P3_T10_SalesDailyExpense_Patch.sql`

- Default `@DryRun = 1`.
- If all four columns already exist, it stops with no backup and no DDL.
- `@DryRun = 0` creates `RC018_Backup_SalesDailyExpense_<RunTag>` before `ALTER`.
- Each column is added only when `COL_LENGTH` is null.
- After the add, it aborts if the row count or index count changed, if `ExpenseId` is no longer an identity, or if `ClaimStatus`, `AttachmentPath`, `AttachmentFileName`, or `AttachmentContent` length differs from the backup.

---

## 3. Validation SQL

`Database/Scripts/025b_RC018_P3_T10_SalesDailyExpense_Validate.sql`

Proves:

- The four approval columns exist  
- PK is `ExpenseId` and it is still an identity  
- The filtered unique index and the activity foreign key remain trusted  
- Receipt and `ClaimStatus` columns still exist  
- Row count is at least 30,181  
- When `@BackupTable` is set, count and receipt/`ClaimStatus` values match that backup  
- `DBCC CHECKCONSTRAINTS`

---

## 4. Rollback SQL

`Database/Scripts/025c_RC018_P3_T10_SalesDailyExpense_Rollback.sql`

Drops each new column only while every value in that column is still NULL. It does not delete expense rows or change receipt columns. `@Confirm` defaults to 0.

---

## 5. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **High** |
| Why | Expenses hold receipt linkage and `ClaimStatus` used by approval |
| Data change | None. New columns start NULL. |
| Schema | Four nullable columns |
| Preserved | `ExpenseId` identity, attachments, `ClaimStatus`, activity FK, unique index |
| Lock | Backup takes a table lock, then metadata `ALTER` |
| Rollback | Drop the four columns only while they are still NULL |

---

## 6. PASS checklist

| Check | Result (fill at exec) |
| --- | --- |
| Four columns exist | ☐ |
| Indexes and activity FK remain | ☐ |
| PK unchanged | ☐ |
| ExpenseId identity | ☐ |
| Row count preserved | ☐ |
| Receipts and ClaimStatus match backup | ☐ |
| DBCC CHECKCONSTRAINTS | ☐ |
| **Overall** | ☐ PASS / ☐ FAIL |

**Production gate:** Do not open `VMS_VisitAttendanceHistory` until Overall = PASS.

---

## Suggested execution order (when approved)

1. `025_*_Patch.sql` with `@DryRun = 1`.  
2. Set `@DryRun = 0`. Confirm the drift check prints no receipt or `ClaimStatus` difference.  
3. `025b_*_Validate.sql` with `@BackupTable` set to the printed backup name.  
4. Require Overall **PASS**.
