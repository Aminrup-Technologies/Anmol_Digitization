# RC-018 Phase 4 / Table 14 — TRN_qcinspector

**Status:** Scripts generated — **not executed**  
**Risk:** Medium  
**Date:** 2026-09-25  
**Source:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`  
**Target:** LIVE `AnmolDristi.dbo.TRN_qcinspector`  
**Mode:** Verification only. This is the last of the 14 RC-018 tables. No TA/DA workflow reads it.

---

## 1. Difference summary

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Row count | 62,926 | 80,474 | Preserve LIVE. Do not equalize. |
| Shared columns (65) | Match | Match | None |
| `ShiftDate` date NULL | Absent | Present | **Retain.** Do not alter. |
| PK | `ID` | `ID` | None |
| PK index `PK__TRN_qcin__3214EC270C3985F8` | Yes | Yes | None |
| `IX_TRN_qcinspector_Compliance` | Absent | Keys `FormID, SubmittedDate, PlantName, Line, Shift` | **Retain** |
| `IX_TRN_qcinspector_Report` | Absent | Same keys as Compliance | **Retain** |
| FKs to `MST_UserMaster` (Approver1, Approver2, DottedLine) | Match | Match | None |
| Triggers | None | None | None |

---

## 2. Verification / reconciliation patch

`Database/Scripts/029_RC018_P4_T14_TRN_qcinspector_Verify.sql`

- Default `@DryRun = 1`.
- When the 65 shared columns, `ShiftDate`, the primary key, both LIVE indexes, and the three user-master foreign keys are present, it returns with no backup and no DDL.
- It never issues `ALTER` on `ShiftDate`.
- `RC018_Backup_TRN_qcinspector_<RunTag>` is created only when `@DryRun = 0` and a genuine gap was printed. That copy is large. The script still does not change the table.

---

## 3. Validation SQL

`Database/Scripts/029b_RC018_P4_T14_TRN_qcinspector_Validate.sql`

Proves:

- All 65 shared columns exist  
- `ShiftDate` exists  
- PK is `ID` and the Phase 0 PK index remains  
- Compliance and Report indexes still use `FormID, SubmittedDate, PlantName, Line, Shift`  
- The three `MST_UserMaster` foreign keys are trusted  
- Row count is at least 80,474  
- `DBCC CHECKCONSTRAINTS`

---

## 4. Rollback SQL

`Database/Scripts/029c_RC018_P4_T14_TRN_qcinspector_Rollback.sql`

No data or schema is undone. The aligned path changes nothing. A review backup is not loaded back over production inspector rows. `ShiftDate` and the two LIVE indexes stay as they are.

---

## 5. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **Medium** |
| Why | Production QC inspector history. `ShiftDate` is LIVE-only. |
| This package | Read-only when the shared schema matches |
| Data change | None |
| Downtime | None on the aligned path. A gap backup copies about 80,474 rows. |
| Rollback | Not applicable unless a later, separate change is approved |

---

## 6. PASS checklist

| Check | Result (fill at exec) |
| --- | --- |
| Shared schema unchanged (65) | ☐ |
| ShiftDate retained | ☐ |
| PK unchanged | ☐ |
| PK, Compliance, and Report indexes remain | ☐ |
| Row count floor 80,474 | ☐ |
| DBCC CHECKCONSTRAINTS | ☐ |
| **Overall** | ☐ PASS / ☐ FAIL |

**Production gate:** This is the final RC-018 table package. Do not write the synchronization report as certified until this validation is Overall **PASS** and the earlier table gates that were held (notably PolicyException financial overrides and any open-day duplicate gate) are recorded.

---

## Suggested execution order (when approved)

1. `029_*_Verify.sql` with `@DryRun = 1`. Expect `Alignment gaps=0` and no backup.  
2. `029b_*_Validate.sql`. Require Overall **PASS**.  
3. Leave `@DryRun = 0` unused unless the verify script reports a gap.
