# RC-018 Phase 3 / Table 7 — ApprovalRequestStatusHistory

**Status:** Scripts generated — **not executed**  
**Risk:** Medium  
**Date:** 2026-09-25  
**Source:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`, `Database/Tables/ApprovalRequestStatusHistory.sql`  
**Target:** LIVE `AnmolDristi.dbo.ApprovalRequestStatusHistory`  
**Scope:** Schema only. Existing history rows are not updated or deleted.

---

## 1. Difference summary

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Row count | 707 | 4,403 | Preserve LIVE |
| PK | `HistoryId` | `HistoryId` | None |
| Identity | `HistoryId` curr=742 | curr=4420 | Never reseed |
| Shared columns | Match | Match | None |
| `ApprovalRequestDetailId` int NULL | Yes | **No** | **Add** |
| `ActivityId` int NULL | Yes | **No** | **Add** |
| `ApproverLevel` int NULL | Yes | **No** | **Add** |
| `ActionType` nvarchar(30) NULL | Yes | **No** | **Add** |
| `DetailVersion` int NULL | Yes | **No** | **Add** |
| `FK_ApprovalHistory_Request` | Yes | Yes | Retain |
| `FK_ApprovalHistory_Detail` | Yes | **No** | **Add WITH CHECK** only if non-null orphans = 0 |
| Trigger / computed column | None | None | None |

New columns are nullable, so existing LIVE rows stay NULL. That does not create orphans.

---

## 2. Idempotent schema patch

`Database/Scripts/022_RC018_P3_T07_ApprovalRequestStatusHistory_Patch.sql`

- Default `@DryRun = 1`.
- `@DryRun = 0` creates `RC018_Backup_ApprovalRequestStatusHistory_<RunTag>` before DDL.
- Each column is added only when `COL_LENGTH` is null.
- `FK_ApprovalHistory_Detail` is created `WITH CHECK` only when every non-null `ApprovalRequestDetailId` exists on `SalesApprovalRequestDetail`.
- If orphans exist, the script lists them and does not create the FK. It does not use `WITH NOCHECK`.
- No `UPDATE`, `DELETE`, or table recreate.

---

## 3. Validation SQL

`Database/Scripts/022b_RC018_P3_T07_ApprovalRequestStatusHistory_Validate.sql`

Proves:

- The five columns exist  
- `FK_ApprovalHistory_Detail` exists, is trusted, and orphan count is 0; otherwise FAIL and print the orphan list  
- PK is still `HistoryId`  
- Row count is at least the Phase 0 floor of 4,403 (later audit inserts are allowed; a drop below 4,403 fails)  
- Optional exact match when `@BackupTable` is set  
- Zero orphan `ApprovalRequestId` values against `SalesApprovalRequest`  
- `DBCC CHECKCONSTRAINTS`  
- Triggers still absent

---

## 4. Rollback SQL

`Database/Scripts/022c_RC018_P3_T07_ApprovalRequestStatusHistory_Rollback.sql`

- Drops `FK_ApprovalHistory_Detail`.
- Drops each additive column only when every value in that column is NULL.
- Stops if any additive column already holds a value.
- Does not delete or rewrite history rows.
- Requires `@Confirm = 1`.

---

## 5. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **Medium** |
| Why | This is the approval audit ledger |
| Data change | None |
| Schema | Five nullable columns and one trusted FK |
| Downtime | Seconds for the backup lock, then metadata DDL |
| Rollback | Drop the FK and the still-null columns |

---

## 6. PASS checklist

| Check | Result (fill at exec) |
| --- | --- |
| Five columns exist | ☐ |
| FK detail trusted, orphans = 0 | ☐ |
| PK unchanged | ☐ |
| Row count preserved | ☐ |
| ApprovalRequestId orphans = 0 | ☐ |
| Triggers unchanged | ☐ |
| DBCC CHECKCONSTRAINTS | ☐ |
| **Overall** | ☐ PASS / ☐ FAIL |

**Production gate:** Do not open `SalesApprovalRequestDetail` until Overall = PASS. If the detail FK is withheld because of orphans, stop and review the printed `HistoryId` list.

---

## Suggested execution order (when approved)

1. `022_*_Patch.sql` with `@DryRun = 1`.  
2. Set `@DryRun = 0`. Confirm the printed row count is unchanged and the FK line says `WITH CHECK`.  
3. `022b_*_Validate.sql`. Set `@BackupTable` to the printed backup name for an exact count match.  
4. Require Overall **PASS**.
