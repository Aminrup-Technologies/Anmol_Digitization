# RC-018 Phase 3 / Table 11 — VMS_VisitAttendanceHistory

**Status:** Scripts generated — **not executed**  
**Risk:** High  
**Date:** 2026-09-25  
**Source:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`, `AnmolDristi/Database/VMS_AttendanceHistory_AfterHoursAudit_Migration.sql`  
**Target:** LIVE `AnmolDristi.dbo.VMS_VisitAttendanceHistory`  
**Scope:** Indexes only. Rows are not updated or deleted.

---

## 1. Difference summary

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Row count | 6 | 4 | Preserve LIVE |
| Columns (8) | Match | Match | None |
| PK | `HistoryID` | `HistoryID` | None |
| `IX_VMS_VisitAttendanceHistory_DayLogID` (`DayLogID`, `CheckInTime`) | Yes | **No** | **Create** if absent |
| `IX_VMS_VisitAttendanceHistory_LogID` (`LogID`, `CheckInTime`) | Yes | **No** | **Create** if absent |
| `UQ_VMS_VisitAttendanceHistory_OpenDayLog` | Yes, unique filtered | **No** | **Create** only if no duplicate open `DayLogID` |
| Triggers | None | None | None |

The unique index is:

```sql
CREATE UNIQUE INDEX UQ_VMS_VisitAttendanceHistory_OpenDayLog
    ON dbo.VMS_VisitAttendanceHistory (DayLogID)
    WHERE CheckOutTime IS NULL;
```

It enforces one in-progress attendance row per day log.

---

## 2. Idempotent index patch

`Database/Scripts/026_RC018_P3_T11_VMS_VisitAttendanceHistory_Patch.sql`

- Default `@DryRun = 1`.
- Each index is created only when its name is absent.
- Before the unique index, it counts `DayLogID` groups where `CheckOutTime IS NULL` and `COUNT(*) > 1`.
- If any group exists, it prints `HistoryID`, `LogID`, `DayLogID`, `CheckInTime`, `CheckOutTime`, `Source`, and `Actor`, and it does not create the unique index.
- `@DryRun = 0` creates `RC018_Backup_VMS_VisitAttendanceHistory_<RunTag>` only when at least one index will actually be created.
- Aborts if the row count changes.

---

## 3. Validation SQL

`Database/Scripts/026b_RC018_P3_T11_VMS_VisitAttendanceHistory_Validate.sql`

Proves:

- Both non-unique indexes exist  
- The filtered unique index exists and open-day duplicates are 0  
- If the unique index is missing because duplicates exist, it prints those rows and the check fails  
- Row count is at least 4  
- PK is still `HistoryID`  
- When `@BackupTable` is set, row values match that backup  
- `DBCC CHECKCONSTRAINTS`

---

## 4. Rollback SQL

`Database/Scripts/026c_RC018_P3_T11_VMS_VisitAttendanceHistory_Rollback.sql`

Drops the three added indexes if they exist. It does not drop the primary key and it does not change rows. `@Confirm` defaults to 0.

---

## 5. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **High** |
| Why | This is the open visit-attendance ledger. The unique index blocks two open cycles for one day log. |
| Data change | None |
| Non-unique indexes | Safe to add. They do not reject existing rows. |
| Unique index | Refused when LIVE already has two open rows for one `DayLogID` |
| Lock | Short schema lock while each index builds on 4 rows |
| Rollback | Drop the three indexes |

---

## 6. PASS checklist

| Check | Result (fill at exec) |
| --- | --- |
| DayLogID index | ☐ |
| LogID index | ☐ |
| OpenDayLog unique index | ☐ |
| No duplicate open DayLogID | ☐ |
| Row count preserved | ☐ |
| PK unchanged | ☐ |
| Data unchanged vs backup | ☐ |
| DBCC CHECKCONSTRAINTS | ☐ |
| **Overall** | ☐ PASS / ☐ FAIL |

**Production gate:** Do not open Phase 4 (`CMS_RawConsumptionLogs`) until Overall = PASS. If validation fails only because duplicate open rows were printed, fix those rows in a separate approved change, then rerun this package. Do not create the unique index with `WITH (IGNORE_DUP_KEY)` or without the filter.

---

## Suggested execution order (when approved)

1. `026_*_Patch.sql` with `@DryRun = 1`. Read the duplicate-group count.  
2. If that count is 0, set `@DryRun = 0`.  
3. `026b_*_Validate.sql` with `@BackupTable` set to the printed backup name.  
4. Require Overall **PASS**.
