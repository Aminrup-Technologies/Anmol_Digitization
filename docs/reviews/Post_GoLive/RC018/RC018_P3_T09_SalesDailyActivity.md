# RC-018 Phase 3 / Table 9 — SalesDailyActivity

**Status:** Scripts generated — **not executed**  
**Risk:** High  
**Date:** 2026-09-25  
**Source:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`, `Database/Tables/SalesDailyActivity.sql`  
**Target:** LIVE `AnmolDristi.dbo.SalesDailyActivity`  
**Scope:** Recreate `CK_SalesDailyActivity_ClaimStatus` only. Activity rows are not updated.

---

## 1. Difference summary

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Row count | 81,171 | 87,402 | Preserve LIVE |
| Columns (55) | Match | Match | None |
| PK | `ActivityId` | `ActivityId` | None |
| Identity | `ActivityId (1,1)` curr=81,171 | curr=87,402 | Do not reseed |
| `UQ_SalesDailyActivity_UserDate` | Yes | Yes | None |
| `CK_SalesDailyActivity_ClaimStatus` | Draft, Submitted, Approved, Rejected, **Returned** | Draft, Submitted, Approved, Rejected | Recreate to add Returned |
| Triggers | None | None | None |

`DetailVersion` stays on `SalesApprovalRequestDetail`. This package does not alter it.

Phase 0 LIVE definition:

```text
([ClaimStatus]='Rejected' OR [ClaimStatus]='Approved' OR [ClaimStatus]='Submitted' OR [ClaimStatus]='Draft')
```

Target definition:

```text
([ClaimStatus]='Rejected' OR [ClaimStatus]='Approved' OR [ClaimStatus]='Returned' OR [ClaimStatus]='Submitted' OR [ClaimStatus]='Draft')
```

---

## 2. Idempotent schema patch

`Database/Scripts/024_RC018_P3_T09_SalesDailyActivity_Patch.sql`

- Default `@DryRun = 1`.
- Prints the captured check definition.
- If the definition is already the target, it stops. No backup, no DDL.
- If the definition is not the known LIVE form and not the target, it stops. It will not drop an unknown status list.
- Counts rows whose `ClaimStatus` is outside Draft, Submitted, Approved, Rejected, and Returned. Any such row aborts the change.
- `@DryRun = 0` creates `RC018_Backup_SalesDailyActivity_<RunTag>` before the drop.
- Drops and recreates only `CK_SalesDailyActivity_ClaimStatus`, `WITH CHECK`.
- Aborts the transaction if the row count or index count changes, or if `ActivityId` is no longer an identity.

---

## 3. Validation SQL

`Database/Scripts/024b_RC018_P3_T09_SalesDailyActivity_Validate.sql`

Proves:

- Returned is in the check  
- Draft, Submitted, Approved, and Rejected remain  
- The normalized definition matches the target exactly  
- Row count is at least 87,402, and equals the backup when `@BackupTable` is set  
- PK is `ActivityId` and it is still an identity  
- `UQ_SalesDailyActivity_UserDate` remains  
- No non-null status sits outside the five allowed values  
- `DBCC CHECKCONSTRAINTS`

---

## 4. Rollback SQL

`Database/Scripts/024c_RC018_P3_T09_SalesDailyActivity_Rollback.sql`

Restores the Phase 0 LIVE check, without Returned, only when no activity row is `Returned`. Those rows would fail `WITH CHECK`. Row count must stay the same. `@Confirm` defaults to 0.

---

## 5. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **High** |
| Why | `SalesDailyActivity` is the claim-day table for return and resubmit |
| Data change | None |
| Schema change | One check constraint becomes a superset of the LIVE list |
| Workflow | Employee → L1 → Returned → resubmit can persist `ClaimStatus = Returned` |
| Lock | `WITH CHECK` scans about 87,402 rows. Expect a short schema lock. |
| Rollback | Safe only before any row is stored as Returned |

---

## 6. PASS checklist

| Check | Result (fill at exec) |
| --- | --- |
| Returned accepted | ☐ |
| Draft, Submitted, Approved, Rejected retained | ☐ |
| Row count preserved | ☐ |
| PK unchanged | ☐ |
| UQ_SalesDailyActivity_UserDate remains | ☐ |
| No violating rows | ☐ |
| DBCC CHECKCONSTRAINTS | ☐ |
| **Overall** | ☐ PASS / ☐ FAIL |

**Production gate:** Do not open `SalesDailyExpense` until Overall = PASS.

---

## Suggested execution order (when approved)

1. `024_*_Patch.sql` with `@DryRun = 1`. Confirm the captured definition is the Phase 0 LIVE form and the illegal-status count is 0.  
2. Set `@DryRun = 0`.  
3. `024b_*_Validate.sql` with `@BackupTable` set to the printed backup name.  
4. Require Overall **PASS**.
