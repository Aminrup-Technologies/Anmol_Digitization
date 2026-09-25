# RC-018 Phase 1 / Table 2 — MST_Calendar Synchronization Package

**Status:** Scripts generated — **not executed**  
**Risk:** Low  
**Date:** 2026-09-24  
**Source of truth:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`  
**Target:** LIVE `AnmolDristi.dbo.MST_Calendar`  
**UAT source (FinancialWeek only):** `AnmolDristiDev.dbo.MST_Calendar`

---

## 1. Difference summary

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Exists | Yes | Yes | None |
| Row count | 7,671 | 7,671 | Preserve (no DELETE) |
| Date-set parity (Phase 1 probe) | — | — | **0** UAT-only / **0** LIVE-only dates |
| Shared attribute drift | — | — | **0** (FinancialYear / WeekNo / IsWeekend) |
| PK | `CalendarDate` | `CalendarDate` | None |
| Identity | No | No | — |
| Shared columns (22) | Match | Match | None |
| `FinancialWeek` int NULL | Yes (all 7,671 non-null; range 1–53) | **Missing** | **ADD** + populate from UAT where NULL |
| LIVE-only columns (13) | No | Yes | **Retain** — never DROP/ALTER |
| LIVE defaults | — | `IsHoliday=(0)`, `IsWorkingDay=(1)`, `IsShutdownDay=(0)` | **Retain** |
| LIVE indexes | — | `IX_MST_Calendar_Date/Financial/Month/YearMonth` | **Retain** |
| FK / Check / Trigger | None | None | None |
| Datatype conflicts | — | — | None |

### LIVE-only columns (retain list)

1. `DayOfYear`  
2. `FinancialMonthName`  
3. `FinancialHalfYear`  
4. `IsHoliday` (+ default)  
5. `IsWorkingDay` (+ default)  
6. `HolidayName`  
7. `HolidayType`  
8. `StateCode`  
9. `YearMonth`  
10. `YearMonthKey`  
11. `IsShutdownDay` (+ default)  
12. `ShutdownReason`  
13. `Remarks`

### FinancialWeek authority sample (UAT)

| CalendarDate | FinancialYear | FinancialWeek |
| --- | --- | ---: |
| 2024-04-01 | 2024-25 | 1 |
| 2024-04-08 | 2024-25 | 2 |
| 2020-01-01 | 2019-20 | 40 |

---

## 2. Idempotent schema patch

`Database/Scripts/017_RC018_P1_T02_MST_Calendar_Patch.sql`

```sql
IF COL_LENGTH(N'dbo.MST_Calendar', N'FinancialWeek') IS NULL
    ALTER TABLE dbo.MST_Calendar ADD FinancialWeek INT NULL;
```

- Default `@DryRun = 1`.  
- Never drops/alters the 13 LIVE-only columns.  
- Never recreates the table.

---

## 3. Data synchronization (FinancialWeek only)

Same script, after ADD + backup `RC018_Backup_MST_Calendar_<RunTag>`:

```sql
UPDATE l
SET l.FinancialWeek = u.FinancialWeek
FROM dbo.MST_Calendar l
INNER JOIN AnmolDristiDev.dbo.MST_Calendar u
    ON u.CalendarDate = l.CalendarDate
WHERE l.FinancialWeek IS NULL
  AND u.FinancialWeek IS NOT NULL;
```

- Updates **only** NULL LIVE values.  
- No DELETE / no shared-column MERGE.  
- Post-update guard aborts if LIVE-only columns drifted vs backup.  
- Re-run: 0 rows updated (idempotent).

Requires same-instance UAT database `AnmolDristiDev` (matches Phase 0 topology).

---

## 4. Validation SQL

`Database/Scripts/017b_RC018_P1_T02_MST_Calendar_Validate.sql`

Proves:

- Row count = 7,671  
- `FinancialWeek` exists and matches UAT (no NULLs / no drift)  
- All 13 LIVE-only columns + defaults + 4 LIVE indexes present  
- PK = `CalendarDate`  
- No duplicate dates  
- `DBCC CHECKCONSTRAINTS (N'dbo.MST_Calendar')`  
- Triggers = 0  
- Idempotent (0 remaining NULL fills)

---

## 5. Rollback SQL

`Database/Scripts/017c_RC018_P1_T02_MST_Calendar_Rollback.sql`

- Restores `FinancialWeek` from named backup.  
- Optional `@DropFinancialWeek = 1` to remove the column if this RC added it.  
- Never drops LIVE-only columns/indexes.  
- Creates `RC018_PreRollback_MST_Calendar_<timestamp>` safety copy first.

---

## 6. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **Low** |
| Schema | Additive nullable column only |
| Data | Single-column fill from UAT; NULL-only predicate |
| Blast radius | Reference calendar; SLA/analytics readers |
| Downtime | Seconds (TABLOCKX during backup) |
| Rollback | Data restore + optional DROP COLUMN |

---

## 7. PASS checklist (gate before Table 3)

| Check | Result (fill at exec) |
| --- | --- |
| Columns Match (shared + FinancialWeek) | ☐ |
| LIVE-only columns retained (13) | ☐ |
| PK Match (`CalendarDate`) | ☐ |
| FK Match | ☐ N/A → PASS |
| Defaults Match (LIVE IsHoliday / IsWorkingDay) | ☐ |
| Indexes Match (4 LIVE indexes retained) | ☐ |
| Triggers Match (none) | ☐ |
| Row Count Preserved = 7,671 | ☐ |
| No Duplicate CalendarDate | ☐ |
| FinancialWeek vs UAT aligned | ☐ |
| DBCC CHECKCONSTRAINTS | ☐ |
| Idempotent (0 NULL fills left) | ☐ |
| **Overall** | ☐ PASS / ☐ FAIL |

**Production gate:** Do not open `MST_EmployeeTolerance` until Overall = PASS.

---

## Suggested execution order (when approved)

1. `017_*_Patch.sql` with `@DryRun = 1` — review preview.  
2. `017b_*_Validate.sql` — expect FinancialWeek **FAIL** pre-apply (column missing).  
3. Set `@DryRun = 0`, re-run patch.  
4. Re-run `017b_*_Validate.sql` — require Overall **PASS**.  
5. Record backup table name for rollback.  
6. Proceed to Phase 1 Table 3 (`MST_EmployeeTolerance`).
