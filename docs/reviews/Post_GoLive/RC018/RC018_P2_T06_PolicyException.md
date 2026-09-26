# RC-018 Phase 2 / Table 6 — PolicyException Synchronization Package

**Status:** Scripts generated — **not executed**  
**Risk:** Medium  
**Date:** 2026-09-25  
**Source of truth:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`  
**Schema contract:** `Database/Scripts/009_PolicyException_IntegrityHardening.sql`  
**Target:** LIVE `AnmolDristi.dbo.PolicyException`  
**UAT authority:** four rows exported from `AnmolDristiDev` on 2026-09-25

---

## 1. Difference summary

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Columns (15) | Match | Match | None |
| Row count | 4 | **0** | MERGE insert 4 |
| PK | `ExceptionId` | `ExceptionId` | None |
| Identity | curr=90002 | curr=1 | `IDENTITY_INSERT`; never reseed downward |
| Index `IX_PolicyException_Employee_Active_Dates` | Yes | **No** | **Add** if absent |
| `CK_PolicyException_Priority` (`Priority > 0`) | Yes | **No** | **Add** if absent |
| `CK_PolicyException_EffectiveDates` | Yes | **No** | **Add** if absent |
| Trigger | None | None | None |
| Datatype conflicts | — | — | None |

### Configuration rows

| ExceptionId | Employee | Active | From | To | Policy snapshot |
| ---: | --- | --- | --- | --- | --- |
| 1 | AHS1562 | Yes | 2026-04-01 | 2027-04-30 | MaxKM 2250, Rate 5.75, DA 250 |
| 2 | AHS628 | Yes | 2026-04-01 | 2027-03-31 | MaxKM 2500, Rate 5.00, DA 230 |
| 4 | AHS1562 | **No** | 2000-01-01 | 2000-01-02 | Inactive overlap fixture (`DA` 99) |
| 90002 | AHS1530 | Yes | 2026-08-01 | 2026-12-31 | MaxKM 3, Rate 6.95, DA 600 |

All four rows share RegionId 1096, BranchId 1965, GradeId 1102, DesignationId 65, Priority 1.

Applying this on LIVE changes TA/DA policy resolution for AHS1562, AHS628, and AHS1530. ExceptionId 4 stays inactive.

---

## 2. Idempotent patch

`Database/Scripts/021_RC018_P2_T06_PolicyException_Patch.sql`

- Default `@DryRun = 1`.
- Checks and index use `IF NOT EXISTS`.
- `@DryRun = 0` creates `RC018_Backup_PolicyException_<RunTag>` before MERGE.
- MERGE matches `ExceptionId`, inserts with `IDENTITY_INSERT`, updates on attribute drift.
- No `WHEN NOT MATCHED BY SOURCE`.
- `@EnableFinancialOverrides` defaults to 0. ExceptionId 4 is always eligible. ExceptionIds 1, 2, and 90002 are merged only when that switch is 1. Skipped rows are printed.

---

## Financial Policy Activation Gate

Default execution does not load the three active financial overrides. Set `@EnableFinancialOverrides = 1` only after the approvals below.

| ExceptionId | Employee | Gate |
| ---: | --- | --- |
| 1 | AHS1562 | Owner approval required before activation. |
| 2 | AHS628 | Owner approval required before activation. |
| 90002 | AHS1530 | Explicit confirmation required for the monthly KM cap of 3 km. |
| 4 | AHS1562 | Always eligible. Inactive and outside the current claim window. No financial override. |

---

## 3. Validation SQL

`Database/Scripts/021b_RC018_P2_T06_PolicyException_Validate.sql`

Proves:

- Row count = 4  
- The four expected records match employee, dates, active flag, priority, and `PolicyJson`  
- Active count = 3  
- Index and both check constraints exist  
- PK = `ExceptionId`  
- No duplicate keys  
- `DBCC CHECKCONSTRAINTS`

---

## 4. Rollback SQL

`Database/Scripts/021c_RC018_P2_T06_PolicyException_Rollback.sql`

- Restores from the named backup (Phase 0 LIVE backup is empty, so the four inserts are removed).
- `@DropAddedObjects = 0` by default. Set it to 1 to drop the index and the two checks.
- Requires `@Confirm = 1`.

---

## 5. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **Medium** |
| Why | Empty LIVE table receives live TA/DA policy exceptions used by policy resolution |
| Schema | Additive index and two checks only |
| Data | Insert-only into an empty table |
| Blast radius | Employees AHS1562, AHS628, AHS1530 |
| Downtime | Seconds |
| Rollback | Backup restore; optional drop of added objects |

---

## 6. PASS checklist

| Check | Result (fill at exec) |
| --- | --- |
| Row count = 4 | ☐ |
| Expected four records | ☐ |
| Active records = 3 | ☐ |
| Index exists | ☐ |
| Check constraints exist | ☐ |
| PK Match | ☐ |
| No duplicate keys | ☐ |
| DBCC CHECKCONSTRAINTS | ☐ |
| **Overall** | ☐ PASS / ☐ FAIL |

**Production gate:** Do not open Phase 3 (`ApprovalRequestStatusHistory`) until Overall = PASS.

---

## Suggested execution order (when approved)

1. `021_*_Patch.sql` with `@DryRun = 1`. Confirm four INSERT previews and no LIVE-only rows.  
2. `021b_*_Validate.sql`. Expect FAIL before apply.  
3. Set `@DryRun = 0` and re-run the patch.  
4. Re-run validation. Require Overall **PASS**.  
5. Keep the backup table name.
