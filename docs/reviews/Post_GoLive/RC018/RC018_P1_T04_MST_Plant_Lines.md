# RC-018 Phase 1 / Table 4 — MST_Plant_Lines Synchronization Package

**Status:** Scripts generated — **not executed**  
**Risk:** Low  
**Date:** 2026-09-25  
**Source of truth:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`  
**Target:** LIVE `AnmolDristi.dbo.MST_Plant_Lines`  
**Mode:** **Verification-only** (shared attribute drift = 0)

---

## 1. Difference summary

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Exists | Yes | Yes | None |
| Row count | 37 | 37 | Preserve |
| Key parity | — | — | **0** UAT-only / **0** LIVE-only `line_id` |
| Shared attribute drift | — | — | **0** (checksum `-426110649` both) |
| PK | `line_id` (`PK_MST_Plant_Lines`) | same | None |
| Identity | `id` (not PK) | same | Preserve; no reseed |
| Columns (9) | Match | Match | None |
| FK `FK_MST_Plant_Lines_MST_PlantDetails` | Yes | Yes | None |
| `UQ_line_id` | Yes | Yes | Retain |
| `IX_MST_Plant_Lines` | Yes | Yes | Retain |
| `IX_MST_Plant_Lines_Report` | **No** | **Yes** | **Retain** — never DROP |
| Trigger | None | None | None |
| Datatype conflicts | — | — | None |

---

## 2. Idempotent synchronization script

`Database/Scripts/019_RC018_P1_T04_MST_Plant_Lines_Patch.sql`

- Default `@DryRun = 1` (drift report only).
- `@DryRun = 0` always creates `RC018_Backup_MST_Plant_Lines_<RunTag>`.
- When UAT-only keys = 0, LIVE-only keys = 0, and shared drift = 0: **stops after backup** (verification-only). No MERGE.
- MERGE is compiled but gated. It runs only if drift or missing UAT keys appear later.
- MERGE matches on `line_id`, updates shared columns only, inserts missing UAT keys with `IDENTITY_INSERT`, and has **no** `WHEN NOT MATCHED BY SOURCE`.
- No index DDL. LIVE report index is asserted before and after.

---

## 3. Validation SQL

`Database/Scripts/019b_RC018_P1_T04_MST_Plant_Lines_Validate.sql`

Proves:

- Row count = 37  
- PK = `line_id`  
- `IX_MST_Plant_Lines_Report`, `IX_MST_Plant_Lines`, `UQ_line_id` exist  
- No duplicate `line_id`  
- FK present  
- Shared drift vs UAT = 0  
- `DBCC CHECKCONSTRAINTS (N'dbo.MST_Plant_Lines')`  
- Triggers = 0

---

## 4. Rollback SQL

`Database/Scripts/019c_RC018_P1_T04_MST_Plant_Lines_Rollback.sql`

- Restores from named backup if the gated MERGE path ever ran.  
- Does not drop indexes.  
- Requires `@Confirm = 1` and the backup table name.

On the verification-only path, LIVE equals the backup, so rollback is a no-op restore.

---

## 5. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **Low** |
| Schema change | None |
| Data change expected | None (drift = 0) |
| Blast radius | Reference plant lines; report index must stay |
| Downtime | Seconds (TABLOCKX during backup only) |
| Rollback | Full row restore from named backup |

---

## 6. PASS checklist (gate before Table 5)

| Check | Result (fill at exec) |
| --- | --- |
| Columns Match | ☐ |
| PK Match (`line_id`) | ☐ |
| FK Match | ☐ |
| Defaults Match | ☐ pre-existing → PASS |
| Indexes Match (`IX_MST_Plant_Lines_Report` retained) | ☐ |
| Triggers Match | ☐ none → PASS |
| Row Count Preserved = 37 | ☐ |
| No Duplicate Keys | ☐ |
| Shared drift = 0 | ☐ |
| DBCC CHECKCONSTRAINTS | ☐ |
| **Overall** | ☐ PASS / ☐ FAIL |

**Production gate:** Do not open `TRN_RM_CLASS_1` until Overall = PASS.

---

## Suggested execution order (when approved)

1. `019_*_Patch.sql` `@DryRun = 1` — confirm MODE = verification-only.  
2. `019b_*_Validate.sql` — Overall PASS is expected before any apply.  
3. Set `@DryRun = 0` to take the backup only.  
4. Re-run validate.  
5. Record backup table name.  
6. Proceed to Phase 1 Table 5 (`TRN_RM_CLASS_1`) — count-equality blocker still applies.
