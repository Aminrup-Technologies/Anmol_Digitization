# RC-018 Phase 1 / Table 5 — TRN_RM_CLASS_1 Reconciliation Package

**Status:** Scripts generated — **not executed**  
**Risk:** Medium  
**Date:** 2026-09-25  
**Source of truth:** `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md`  
**Target:** LIVE `AnmolDristi.dbo.TRN_RM_CLASS_1`  
**Business key:** `RMFID` (primary key)  
**Acceptance:** LIVE ⊇ UAT. Row-count equality is **not** required.

---

## 1. Difference summary

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Row count | 330 | 509 | Do not equalize |
| Distinct `RMFID` | 330 | 509 | No duplicates either side |
| UAT keys missing on LIVE | 0 | — | MERGE insert is a no-op today |
| LIVE-only keys | — | **179** | **Ignore** — never delete |
| Shared columns | 70 | 70 | Update only where values differ |
| `Material_Name` varchar(50) NULL | No | Yes | **Retain** — excluded from UPDATE and INSERT |
| PK | `RMFID` | `RMFID` | None |
| Identity `Id` | curr=330 | curr=509 | Preserve. Shared keys have matching `Id` (mismatch = 0). Never UPDATE `Id`. |
| Datatype conflicts | — | — | None |
| Triggers | None | None | None |

### Shared drift (2026-09-25 probe)

One business key differs: **`RMF330`** (`Id` 330 on both).

Examples of the conflict (UAT vs LIVE):

| Column | UAT | LIVE |
| --- | --- | --- |
| SubmittedDate | 2026-06-18 | 2026-03-26 |
| SubmittedByEmployeeCode | AHO539 | 302 |
| MaterialName | 2 | 4 |
| Moisture | NULL | 1.9500 |
| Fat_Content | NULL | 68.0000 |
| Material_Image | empty | production upload path |

`@DryRun = 0` does **not** copy UAT values onto `RMF330`. That key is excluded from the MERGE source. See Accepted Production Divergence below.

---

## Accepted Production Divergence

| Field | Value |
| --- | --- |
| RMFID | `RMF330` |
| Reason | LIVE contains operational measurements and submit history that must not be overwritten by UAT reference data. |

Examples of the retained LIVE values:

| Column | UAT (ignored) | LIVE (kept) |
| --- | --- | --- |
| SubmittedDate | 2026-06-18 | 2026-03-26 |
| SubmittedByEmployeeCode | AHO539 | 302 |
| Moisture | NULL | 1.9500 |
| Fat_Content | NULL | 68.0000 |
| Material_Image | empty | production upload path |

Validation reports this row as **ACCEPTED**. It is not a FAIL. The MERGE predicate is `RMFID <> 'RMF330'`.

---

## 2. Idempotent reconciliation script

`Database/Scripts/020_RC018_P1_T05_TRN_RM_CLASS_1_Reconcile.sql`

- Default `@DryRun = 1` (counts, missing keys, drift list).
- `@DryRun = 0` creates `RC018_Backup_TRN_RM_CLASS_1_<RunTag>`, then MERGE.
- Match on `RMFID`.
- `WHEN NOT MATCHED BY TARGET` inserts the UAT row with `IDENTITY_INSERT` and does not set `Material_Name`.
- `WHEN MATCHED` updates shared non-key, non-identity columns only when `EXCEPT` finds a difference.
- UAT source is filtered with `WHERE RMFID <> 'RMF330'`.
- No `WHEN NOT MATCHED BY SOURCE`.
- Aborts if `Id` differs for a shared `RMFID`.
- Re-run after apply: 0 insert, 0 update.

---

## 3. Validation SQL

`Database/Scripts/020b_RC018_P1_T05_TRN_RM_CLASS_1_Validate.sql`

Proves:

- Missing UAT keys in LIVE = 0  
- Duplicate `RMFID` = 0  
- `Material_Name` exists  
- LIVE count >= UAT count  
- PK = `RMFID`  
- Identity still on `Id`  
- Shared attribute drift excluding `RMF330` = 0
- `RMF330` present on LIVE and reported **ACCEPTED**
- `DBCC CHECKCONSTRAINTS`
- Does **not** require 330 = 509

---

## 4. Rollback SQL

`Database/Scripts/020c_RC018_P1_T05_TRN_RM_CLASS_1_Rollback.sql`

- Restores non-identity columns, including `Material_Name`, from the named backup.
- Deletes only keys that this RC inserted and that are absent from the backup.
- Does not remove the original 179 LIVE-only rows (they are in the backup).
- Requires `@Confirm = 1`.

---

## 5. Risk assessment

| Factor | Assessment |
| --- | --- |
| Risk level | **Medium** (accepted data divergence; no production overwrite) |
| Schema | None (`Material_Name` retained) |
| Missing keys | None |
| `RMF330` | Excluded from MERGE. LIVE values kept. |
| LIVE-only rows | 179 preserved |
| Downtime | Seconds (TABLOCKX on backup) if `@DryRun = 0` |
| Rollback | Unchanged. Full non-identity restore from named backup. |

---

## 6. PASS checklist

| Check | Result |
| --- | --- |
| Missing UAT keys in LIVE = 0 | PASS |
| Duplicate business keys = 0 | PASS |
| Material_Name exists | PASS |
| LIVE count >= UAT count | PASS (509 >= 330) |
| PK Match (`RMFID`) | PASS |
| Identity preserved | PASS |
| Shared drift excluding RMF330 = 0 | PASS |
| RMF330 accepted divergence | ACCEPTED |
| Row count equality | Not required |
| DBCC CHECKCONSTRAINTS | PASS |
| **Overall** | **PASS** |

Evidence: `docs/reviews/Post_GoLive/RC018/P1_T05A_Validate.txt` (read-only, 2026-09-25). No MERGE was executed.

**Production gate:** `RMF330` is an accepted exclusion. `@DryRun = 0` backs up LIVE and does not update that key. Rollback script is unchanged.

---

## Suggested execution order (when approved)

1. `020_*_Reconcile.sql` with `@DryRun = 1`. Confirm missing = 0, actionable drift = 0, `RMF330` = ACCEPTED.  
2. `@DryRun = 0` takes the backup only (MERGE skipped while actionable drift stays 0).  
3. `020b_*_Validate.sql` — Overall PASS, with `RMF330` documented as Accepted.  
4. Keep the backup table name. Rollback script is unchanged.
