# RC-018 Phase 3 / Table 8 — SalesApprovalRequestDetail

**Status:** Scripts generated — **not executed**  
**Risk:** High  
**Date:** 2026-09-25  
**Source:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`, `Database/Tables/SalesApprovalRequestDetail.sql`  
**Target:** LIVE `AnmolDristi.dbo.SalesApprovalRequestDetail`  
**Mode:** Verification only. Phase 0 found the shared schema already matched.

---

## 1. Difference summary

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Row count | 3,023 | 34,257 | Preserve LIVE. Do not equalize. |
| Columns (21) | Match | Match | None |
| PK | `ApprovalRequestDetailId` | `ApprovalRequestDetailId` | None |
| Identity | `(1,1)` curr=3,141 | `(1,1)` curr=34,645 | Do not reseed |
| `FK_SARDetail_Activity` | Yes | Yes | None |
| `FK_SARDetail_Request` | Yes | Yes | None |
| `IX_SalesApprovalRequestDetail_RequestId` | Yes | Yes | None |
| `IX_SalesApprovalRequestDetail_Request_Activity_Version` | Yes | Yes | None |
| Defaults | `CreatedOn = sysutcdatetime()`, `DetailVersion = 1` | Same definitions | None. Constraint names may differ. |
| Triggers | None | None | None |
| Parent of `FK_ApprovalHistory_Detail` | PK `ApprovalRequestDetailId` | Same PK | Compatible with P3-T07 |

`DetailVersion`, `PreviousDetailId`, and the `Snapshot_*` columns are already on LIVE. They are not rewritten.

---

## 2. Verification-only patch

`Database/Scripts/023_RC018_P3_T08_SalesApprovalRequestDetail_Verify.sql`

- Default `@DryRun = 1`.
- When alignment gaps = 0, it returns without a backup and without DDL.
- A backup `RC018_Backup_SalesApprovalRequestDetail_<RunTag>` is created only when `@DryRun = 0` and a gap was found.
- Even then the script does not `ALTER`, delete, or reseed. The backup is for review.

---

## 3. Validation SQL

`Database/Scripts/023b_RC018_P3_T08_SalesApprovalRequestDetail_Validate.sql`

Proves:

- All 21 shared columns exist  
- PK is still `ApprovalRequestDetailId`  
- Identity seed 1, increment 1  
- Both required indexes exist  
- Both outbound FKs are trusted  
- `CreatedOn` and `DetailVersion` defaults match the UAT definitions  
- Row count is at least 34,257  
- If `ApprovalRequestStatusHistory.ApprovalRequestDetailId` exists, non-null values all exist on this table; otherwise there are no such references yet  
- `DBCC CHECKCONSTRAINTS`

---

## 4. Rollback SQL

`Database/Scripts/023c_RC018_P3_T08_SalesApprovalRequestDetail_Rollback.sql`

No data or schema is undone. The aligned path changes nothing. A review backup is not copied back over LIVE.

---

## 5. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **High** (table content), **Low** (this package) |
| Why high | Latest-version snapshots and `DetailVersion` live here |
| This package | Read-only when aligned. No identity reseed. |
| P3-T07 | The history FK can point at this primary key. Orphans block that FK, not this table. |
| Downtime | None on the aligned path. Backup lock only if a gap forces a review copy. |

---

## 6. PASS checklist

| Check | Result (fill at exec) |
| --- | --- |
| Shared columns 21/21 | ☐ |
| PK unchanged | ☐ |
| Required indexes | ☐ |
| Defaults | ☐ |
| Outbound FKs | ☐ |
| Identity preserved | ☐ |
| Row count preserved | ☐ |
| History detail orphans | ☐ |
| DBCC CHECKCONSTRAINTS | ☐ |
| **Overall** | ☐ PASS / ☐ FAIL |

**Production gate:** Do not open `SalesDailyActivity` until Overall = PASS.

---

## Suggested execution order (when approved)

1. `023_*_Verify.sql` with `@DryRun = 1`. Expect `Alignment gaps=0` and no backup.  
2. `023b_*_Validate.sql`. Require Overall **PASS**.  
3. Leave `@DryRun = 0` unused unless the verify script reports a gap.
