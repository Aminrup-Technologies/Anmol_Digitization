# RC-018 Phase 1 / Table 3 — MST_EmployeeTolerance Synchronization Package

**Status:** Scripts generated — **not executed**  
**Risk:** Low  
**Date:** 2026-09-24  
**Source of truth:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`  
**Target:** LIVE `AnmolDristi.dbo.MST_EmployeeTolerance`  
**Also aligns with:** `Database/Scripts/001_Add_MaxKmModificationCount.sql`, `Database/Tables/MST_EmployeeTolerance.sql`

---

## 1. Difference summary

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Exists | Yes | Yes | None |
| Row count | 3 | **0** | MERGE insert 3 seeds |
| PK | `ToleranceId` | `ToleranceId` | None |
| Identity | `ToleranceId (1,1)` curr=3 | `(1,1)` curr=1 | Preserve via `IDENTITY_INSERT`; never reseed |
| Shared columns (11) | Match | Match | MERGE update on drift |
| `MaxKmModificationCount` int NOT NULL DF(1) | Yes | **No** | **ADD** + default |
| Check `CK_..._MaxKmModificationCount` (≥1) | Yes | **No** | **ADD** |
| Index `IX_MST_EmployeeTolerance_Lookup` | Yes | Yes | None |
| FK / Trigger | None | None | None |
| Datatype conflicts | — | — | None |

### Seed rows (UAT authority)

| ToleranceId | EmployeeCode | MinAllowedKM | MaxAllowedKM | EffectiveFrom | EffectiveTo | IsActive | MaxKmModificationCount |
| ---: | --- | ---: | ---: | --- | --- | ---: | ---: |
| 1 | AHS1562 | 0.00 | 100.00 | 2026-04-01 | 2027-03-31 | 1 | 2 |
| 2 | M1367 | 10.00 | 50.00 | 2026-04-01 | 2027-03-31 | 1 | 1 |
| 3 | AHS1530 | 0.00 | 30.00 | 2026-08-01 | 2026-12-31 | 1 | 1 |

---

## 2. Idempotent schema patch

`Database/Scripts/018_RC018_P1_T03_MST_EmployeeTolerance_Patch.sql` (section 1)

- `IF COL_LENGTH(...MaxKmModificationCount) IS NULL` → `ADD ... NOT NULL CONSTRAINT DF_... DEFAULT (1)`
- `IF NOT EXISTS` check constraint `CK_MST_EmployeeTolerance_MaxKmModificationCount`
- Default `@DryRun = 1`

---

## 3. MERGE script (three seed rows)

Same file (section 3), after backup `RC018_Backup_MST_EmployeeTolerance_<RunTag>`:

- Match on `ToleranceId`
- `WHEN NOT MATCHED BY TARGET` → INSERT (with `IDENTITY_INSERT ON`)
- `WHEN MATCHED` + attribute drift → UPDATE shared cols including `MaxKmModificationCount`
- **No** `WHEN NOT MATCHED BY SOURCE` (never DELETE)
- Re-run → 0 INSERT / 0 UPDATE

---

## 4. Validation SQL

`Database/Scripts/018b_RC018_P1_T03_MST_EmployeeTolerance_Validate.sql`

Proves:

- Row count = 3  
- `MaxKmModificationCount` + DF + CK exist  
- All three seeds match  
- No duplicate `ToleranceId` / Emp+date keys  
- Identity still on `ToleranceId`  
- `DBCC CHECKCONSTRAINTS`  
- Idempotent (WouldInsert/Update = 0)

---

## 5. Rollback SQL

`Database/Scripts/018c_RC018_P1_T03_MST_EmployeeTolerance_Rollback.sql`

- Restores from named backup (Phase 0 LIVE backup is empty → removes the 3 inserts)  
- Optional `@DropMaxKmModificationCount = 1` drops CK + DF + column  
- Safety copy before changes

---

## 6. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **Low** |
| Schema | Additive NOT NULL column with default on empty table |
| Data | Insert-only into empty LIVE table (3 reference rows) |
| Blast radius | KM tolerance lookups on claims pages / SPs |
| Downtime | Seconds |
| Rollback | Backup restore + optional column drop |

---

## 7. PASS checklist (gate before Table 4)

| Check | Result (fill at exec) |
| --- | --- |
| Columns Match (+MaxKmModificationCount) | ☐ |
| PK Match (`ToleranceId`) | ☐ |
| FK Match | ☐ N/A → PASS |
| Defaults Match | ☐ |
| Check Constraint | ☐ |
| Indexes Match | ☐ pre-existing Lookup → PASS |
| Triggers Match | ☐ none → PASS |
| Row Count = 3 | ☐ |
| Seed records present (Ids 1–3) | ☐ |
| No Duplicate Keys | ☐ |
| Identity preserved | ☐ |
| DBCC CHECKCONSTRAINTS | ☐ |
| MERGE Idempotent | ☐ |
| **Overall** | ☐ PASS / ☐ FAIL |

**Production gate:** Do not open `MST_Plant_Lines` until Overall = PASS.

---

## Suggested execution order (when approved)

1. `018_*_Patch.sql` `@DryRun = 1` — review INSERT preview (3 rows).  
2. `018b_*_Validate.sql` — expect FAIL on count/column pre-apply.  
3. `@DryRun = 0`, re-run patch.  
4. Re-run validate — require Overall **PASS**.  
5. Record backup table name.  
6. Proceed to Phase 1 Table 4 (`MST_Plant_Lines`).
