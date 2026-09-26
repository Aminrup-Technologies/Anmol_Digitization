# RC-018 Phase 1 / Table 1 — FrequencyTypes Synchronization Package

**Status:** Scripts generated — **not executed**  
**Risk:** Low  
**Date:** 2026-09-24  
**Source of truth:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`  
**Target:** LIVE `AnmolDristi.dbo.FrequencyTypes`

---

## 1. Difference summary

| Object | UAT (`AnmolDristiDev`) | LIVE (`AnmolDristi`) | Action |
| --- | --- | --- | --- |
| Table exists | Yes | Yes | None |
| Row count | 6 | 6 | None expected after MERGE |
| PK | `FrequencyID` | `FrequencyID` | None |
| Identity | `(1,1)` curr=6 | `(1,1)` curr=6 | Never reseed |
| `FrequencyID` int NOT NULL | Yes | Yes | Shared — MERGE key |
| `FrequencyDescription` varchar(255) NOT NULL | Yes | Yes | Shared — MERGE update if drift |
| `FrequencyFactor` int NULL | **No** | **Yes** | **Retain** — never UPDATE/DROP |
| FK / Check / Default / Trigger / non-PK Index | None | None | None |
| Data (IDs 1–6 descriptions) | Match Phase 0 sample | Match | MERGE is no-op for descriptions today |

### Phase 0 certified row set

| FrequencyID | FrequencyDescription | LIVE FrequencyFactor (retain) |
| ---: | --- | ---: |
| 1 | TWICE IN A SHIFT | 2 |
| 2 | ONCE IN A SHIFT | 1 |
| 3 | 2 HOURS INTERVAL | 4 |
| 4 | BEFORE EVERY DESPATCH | 1 |
| 5 | AS PER REQUIREMENT | 1 |
| 6 | ONCE IN EVERY SHIFT | 1 |

### What this package does / does not do

| Does | Does not |
| --- | --- |
| Backup LIVE → `RC018_Backup_FrequencyTypes_<RunTag>` | DELETE LIVE rows |
| MERGE insert missing UAT keys (IDENTITY_INSERT) | DROP / alter `FrequencyFactor` |
| UPDATE `FrequencyDescription` when drifted | Reseed identity |
| Leave `FrequencyFactor` untouched on MATCH | Truncate / recreate table |
| Idempotent (second run → 0 INSERT/UPDATE) | Touch any other table |

---

## 2. Idempotent MERGE script

`Database/Scripts/016_RC018_P1_T01_FrequencyTypes_MERGE.sql`

- Default `@DryRun = 1` (backup + preview only).
- Set `@DryRun = 0` only after review to apply.
- `WHEN NOT MATCHED BY SOURCE` omitted (no deletes).
- `UPDATE` sets only `FrequencyDescription`.

---

## 3. Validation SQL

`Database/Scripts/016b_RC018_P1_T01_FrequencyTypes_Validate.sql`

Proves:

- Row count = 6  
- PK = `FrequencyID`  
- `FrequencyFactor` column present and values match Phase 0 baseline  
- No duplicate keys  
- Descriptions match UAT authority  
- Simulated re-MERGE would INSERT/UPDATE **0** rows (idempotent)

---

## 4. Rollback SQL

`Database/Scripts/016c_RC018_P1_T01_FrequencyTypes_Rollback.sql`

- Requires `@BackupTable` = name printed by MERGE run.  
- Requires `@Confirm = 1`.  
- Restores description + `FrequencyFactor` from backup.  
- Creates a pre-rollback safety copy first.

---

## 5. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **Low** |
| Schema change | None (retain-only) |
| Data change expected | None if Phase 0 still true (descriptions already match) |
| Blast radius | Single reference table, 6 rows |
| Downtime | ~seconds; TABLOCKX during backup only |
| Rollback | Full row restore from named backup table |

---

## 6. PASS checklist (gate before Table 2)

Run validation on LIVE after apply (or anytime for baseline). All must be **PASS**:

| Check | Result (fill at exec) |
| --- | --- |
| Columns Match (shared) | ☐ |
| PK Match | ☐ |
| FK Match | ☐ N/A → PASS |
| Defaults Match | ☐ N/A → PASS |
| Indexes Match | ☐ PK only → PASS |
| Triggers Match | ☐ none → PASS |
| Row Count = 6 | ☐ |
| FrequencyFactor Preserved | ☐ |
| No Duplicate Keys | ☐ |
| MERGE Idempotent | ☐ |
| **Overall** | ☐ PASS / ☐ FAIL |

**Production gate:** Do not proceed to `MST_Calendar` until Overall = PASS and evidence is attached under `docs/reviews/Post_GoLive/RC018/`.

---

## Suggested execution order (when approved)

1. Run `016_*_MERGE.sql` with `@DryRun = 1` — review preview.  
2. Run `016b_*_Validate.sql` — confirm baseline PASS (or note pre-existing FAIL).  
3. Set `@DryRun = 0`, re-run MERGE.  
4. Re-run `016b_*_Validate.sql` — require Overall PASS.  
5. Keep backup table name in the exec log for rollback.  
6. Only then open Phase 1 Table 2 (`MST_Calendar`).
