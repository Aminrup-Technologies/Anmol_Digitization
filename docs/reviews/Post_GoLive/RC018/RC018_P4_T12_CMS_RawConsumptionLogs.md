# RC-018 Phase 4 / Table 12 — CMS_RawConsumptionLogs

**Status:** Scripts generated — **not executed**  
**Risk:** Low  
**Date:** 2026-09-25  
**Source:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`  
**Target:** LIVE `AnmolDristi.dbo.CMS_RawConsumptionLogs`  
**Mode:** Verification only. No TA/DA workflow uses this punch log.

---

## 1. Difference summary

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Row count | 0 | 772,493 | Preserve LIVE. Do not migrate logs. |
| Columns (10) | Match | Match | None |
| PK | `LogId` | `LogId` | None |
| Identity | `LogId` | `LogId` | Do not reseed |
| `UX_CMS_RawConsumptionLogs_Dedup` | Absent | Unique on `MachineIP`, `EnrollNumber`, `PunchTime` | **Retain.** Do not alter. |
| `CreatedOn` default | `getdate()` | `getdate()` | None |
| Triggers | None | None | None |

Shared columns: `LogId`, `EmployeeCode`, `EnrollNumber`, `VerifyMode`, `InOutMode`, `PunchTime`, `CreatedOn`, `BranchID`, `MachineIP`, `EmployeeName`.

---

## 2. Verification-only patch

`Database/Scripts/027_RC018_P4_T12_CMS_RawConsumptionLogs_Verify.sql`

- Default `@DryRun = 1`.
- When alignment gaps = 0, it returns with no backup and no DDL.
- It never drops or rebuilds `UX_CMS_RawConsumptionLogs_Dedup`.
- `RC018_Backup_CMS_RawConsumptionLogs_<RunTag>` is created only when `@DryRun = 0` and a gap was found. That copy is large. The script still does not `ALTER` the table.

---

## 3. Validation SQL

`Database/Scripts/027b_RC018_P4_T12_CMS_RawConsumptionLogs_Validate.sql`

Proves:

- All 10 shared columns exist  
- PK is `LogId` and it is still an identity  
- `UX_CMS_RawConsumptionLogs_Dedup` is unique on `MachineIP`, `EnrollNumber`, `PunchTime`  
- Row count is at least 772,493  
- No triggers  
- `DBCC CHECKCONSTRAINTS`

---

## 4. Rollback SQL

`Database/Scripts/027c_RC018_P4_T12_CMS_RawConsumptionLogs_Rollback.sql`

No data or schema is undone. The aligned path changes nothing. A review backup is not loaded back over the live log.

---

## 5. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **Low** |
| Why | Schema already matches. The unique punch-dedup index stays. No claim workflow reads this as a policy source. |
| Data change | None |
| Downtime | None on the aligned path. A gap backup would lock and copy about 772,493 rows. |
| Rollback | Not applicable unless a future, separate change is approved |

---

## 6. PASS checklist

| Check | Result (fill at exec) |
| --- | --- |
| Schema unchanged (10 columns) | ☐ |
| Unique index retained | ☐ |
| Row count floor 772,493 | ☐ |
| PK unchanged | ☐ |
| LogId identity | ☐ |
| DBCC CHECKCONSTRAINTS | ☐ |
| **Overall** | ☐ PASS / ☐ FAIL |

**Production gate:** Do not open `TRN_qc_packet_weight_hdr` until Overall = PASS.

---

## Suggested execution order (when approved)

1. `027_*_Verify.sql` with `@DryRun = 1`. Expect `Alignment gaps=0` and no backup.  
2. `027b_*_Validate.sql`. Require Overall **PASS**.  
3. Leave `@DryRun = 0` unused unless the verify script reports a gap.
