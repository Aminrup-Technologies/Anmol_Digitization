# Policy Exception Integrity Review

Scope: database integrity only. No UI, Policy Resolution, or Claim Processing changes.

**Status:** Implemented and merged (PR #69). Deploy script: `Database/Scripts/009_PolicyException_IntegrityHardening.sql`. Write procedures also synced in `Database/Scripts/004_PolicyException_StoredProcedures.sql`.

**Application timestamp standard:** **IST** (`SYSUTCDATETIME()` → UTC → `India Standard Time`). History `ChangedOn` already used IST; write-path `CreatedOn` / `ModifiedOn` were aligned to IST in this hardening.

---

## 1. Integrity Review Report (Phase 1 – pre-change findings)

### Reviewed procedures

| Procedure | Current validations (before hardening) | Gaps |
|-----------|----------------------------------------|------|
| `usp_InsertPolicyException` | Employee required/exists/active; PolicyJson non-empty; Priority > 0; EffectiveFrom required; EffectiveTo ≥ From; active overlap | No `ISJSON`; overlap check not concurrency-safe (empty-set race); timestamps used `SYSDATETIME()` |
| `usp_UpdatePolicyException` | Same as insert + ExceptionId exists; overlap excludes self | No `ISJSON`; no row lock before update; empty-set/overlap race; `SYSDATETIME()` |
| `usp_SetPolicyExceptionActiveStatus` | Exists; not already in requested status | No expired-date check on Activate; no overlap check on Activate; no concurrency lock |
| `usp_SearchPolicyExceptions` | Optional filters | OK for integrity (read-only) |
| `usp_GetPolicyExceptionById` | By id | OK |
| `usp_InsertPolicyExceptionHistorySnapshot` | ActionType check; `UPDLOCK, HOLDLOCK` for VersionNo | OK; history writes IST |

### Findings summary

1. **JSON** – malformed JSON could be stored (`ISJSON` not used).
2. **Activation** – expired rows (`EffectiveTo < today`) could be activated; activating could create overlapping actives.
3. **Concurrency** – Insert/Update/Activate race on overlap checks when no conflicting row exists yet (`UPDLOCK` alone insufficient for empty-set).
4. **Indexes** – `PolicyException` had PK only; employee active/date lookups scanned.
5. **Data quality** – no table CHECKs for Priority / date order; EmployeeCode validated in SP only (no FK; intentional — UserMaster key types / lifecycle differ).
6. **Audit timestamps** – `CreatedOn`/`ModifiedOn` = server `SYSDATETIME()`; history `ChangedOn` = IST. **Inconsistency.**
7. **History security** – no UPDATE/DELETE procedures, but table not protected against direct DML.

---

## 2. Stored Procedures Modified

| SP | Change |
|----|--------|
| `usp_InsertPolicyException` | `ISJSON` → throw `50009`; employee-scoped `sp_getapplock`; overlap with `UPDLOCK, HOLDLOCK`; IST `CreatedOn` |
| `usp_UpdatePolicyException` | `ISJSON` → throw `50029`; lock target row; app lock; overlap with `UPDLOCK, HOLDLOCK`; IST `ModifiedOn` |
| `usp_SetPolicyExceptionActiveStatus` | Expired block `50032`; overlap on activate `50033`; app lock + row lock; history only after successful mutate |

Read SPs (`Search`, `GetById`, history get/compare paths) unchanged in signature and behaviour.

### JSON validation (Phase 2)

Before Insert and Update:

```sql
IF ISJSON(@PolicyJson) <> 1
    THROW 50009/50029, 'PolicyJson must be valid JSON.', 1;
```

Format only — no schema validation. Backward compatible for all valid JSON payloads.

### Activation safety (Phase 3)

Before `@IsActive = 1`:

1. If `EffectiveTo < today (IST)` → `"Expired Policy Exceptions cannot be activated."` (`50032`).
2. Same overlap predicate as Insert/Update → `50033`.
3. History snapshot runs only after the status update succeeds (same transaction; failures roll back before history).

---

## 3. DDL Changes

| Object | Purpose |
|--------|---------|
| `CK_PolicyException_Priority` | `Priority > 0` |
| `CK_PolicyException_EffectiveDates` | `EffectiveTo IS NULL OR EffectiveFrom IS NULL OR EffectiveTo >= EffectiveFrom` |
| `IX_PolicyException_Employee_Active_Dates` | See §4 |
| `TR_PolicyExceptionHistory_PreventMutation` | AFTER UPDATE/DELETE → throw `50050` |

Deploy: `Database/Scripts/009_PolicyException_IntegrityHardening.sql` (idempotent CHECKs/index; recreates trigger + write SPs).

---

## 4. Index Recommendations

| Recommendation | Action | Expected benefit |
|----------------|--------|------------------|
| `(EmployeeCode, IsActive, EffectiveFrom, EffectiveTo) INCLUDE (Priority, ExceptionId)` | **Created** as `IX_PolicyException_Employee_Active_Dates` | Speeds overlap checks, Search-by-employee, and Policy Resolution employee/date lookups without changing resolution code |
| History indexes from 007/008 | **Keep** | Version/timeline queries already covered |
| Standalone `Priority` index | **Not created** | Low selectivity; covered by INCLUDE when employee filtered |
| Duplicate indexes on same keys | **Avoided** | Single composite only |

---

## 5. Concurrency Strategy

| Approach considered | Why not / why |
|---------------------|---------------|
| `SERIALIZABLE` alone | Broader range locks; higher deadlock risk |
| `UPDLOCK` only | Insufficient for empty-set race (two inserts both see no overlap) |
| `UPDLOCK, HOLDLOCK` alone | Helps when a row exists; empty-set still races |
| **`sp_getapplock` + `UPDLOCK, HOLDLOCK`** | **Chosen** |

**Chosen solution:** transaction-scoped exclusive `sp_getapplock` on resource `PolicyException:{EmployeeCode}` (timeout 5s → `50010`), plus `UPDLOCK, HOLDLOCK` on overlap/target rows.

**Why least intrusive:** serializes only writers for the same employee; other employees unaffected; lock auto-released on commit/rollback; no isolation-level change for the session; no UI/app changes.

**Races addressed:** concurrent Insert overlap; concurrent Update of same/overlapping policies; concurrent Activate creating overlap.

---

## 6. Data Quality Review (Phase 6)

| Rule | Enforcement |
|------|-------------|
| EmployeeCode not orphaned at write time | SP checks `MST_UserMaster` exists + Active (Insert/Update). No FK (intentional). |
| Priority always > 0 | SP validation + `CK_PolicyException_Priority` |
| EffectiveTo ≥ EffectiveFrom | SP validation + `CK_PolicyException_EffectiveDates` |
| History immutable | Trigger `TR_PolicyExceptionHistory_PreventMutation`; no UPDATE/DELETE SPs |
| PolicyException = latest state | Unchanged: current row updated in place; history snapshots versions |

---

## 7. Audit Consistency (Phase 7)

| Column | Before | After |
|--------|--------|-------|
| `PolicyException.CreatedOn` | `SYSDATETIME()` (server) | IST |
| `PolicyException.ModifiedOn` | `SYSDATETIME()` (server) | IST |
| `PolicyExceptionHistory.ChangedOn` | IST | IST (unchanged) |

**Standard documented as IST.** Only changed because inconsistency existed.

---

## 8. Security Assessment (Phase 8)

| Control | Status |
|---------|--------|
| History cannot be modified | Trigger blocks UPDATE (`50050`) |
| History cannot be deleted | Trigger blocks DELETE (`50050`) |
| No app/SP path for history UPDATE | Confirmed — only insert snapshot helper |
| SPs expose only required ops | Search/CRUD/Activate/History read/snapshot; no history mutate APIs |
| Direct table UPDATE on history | Blocked at DB layer |

---

## 9. Regression Checklist

| Area | Expected | Dev verified |
|------|----------|--------------|
| Search grid / filters | Unchanged | Pass (rows returned) |
| Create with valid JSON | Works | Covered by existing data + SP path |
| Create/Update invalid JSON | Rejected | Pass `50009` / `50029` |
| Update valid | Works | Pass |
| Activate expired | Blocked, no history | Pass `50032`, history count unchanged |
| Activate overlapping | Blocked, no history | Pass `50033`, no history row |
| Overlap insert | Blocked | Pass `50008` |
| Deactivate / Activate valid | Works (unchanged happy path) | SP path unchanged for non-expired non-overlap |
| History View / Compare / Export / Timeline | Unchanged app behaviour | No app changes in this PR |
| Policy Resolution | Unchanged | No resolution code/SP changes |
| Claim Processing | Unchanged | Out of scope / untouched |
| History direct UPDATE/DELETE | Blocked | Pass `50050` |
| CHECK Priority / dates | Enforced | Pass |

---

## 10. Deployment Steps

1. Backup `dbo.PolicyException` and `dbo.PolicyExceptionHistory` (or full DB backup).
2. Ensure history scripts **007** / **008** already applied.
3. Run `Database/Scripts/009_PolicyException_IntegrityHardening.sql` on the target database.
4. Smoke: Search → View → Edit (valid) → invalid JSON attempt → Activate/Deactivate → View History.
5. Confirm objects exist: two CHECKs, index, immutability trigger, updated write SPs.

---

## 11. Rollback Plan

1. Drop trigger: `DROP TRIGGER dbo.TR_PolicyExceptionHistory_PreventMutation`.
2. Drop index: `DROP INDEX IX_PolicyException_Employee_Active_Dates ON dbo.PolicyException`.
3. Drop CHECKs: `CK_PolicyException_Priority`, `CK_PolicyException_EffectiveDates`.
4. Restore prior definitions of `usp_InsertPolicyException`, `usp_UpdatePolicyException`, `usp_SetPolicyExceptionActiveStatus` from pre-009 script / backup.
5. Re-smoke Search / CRUD / Activate / History.

Note: rows created after deploy remain valid; IST timestamps already written are left as-is.

---

## 12. Performance Impact Assessment

| Change | Impact |
|--------|--------|
| `sp_getapplock` | Short exclusive lock per employee write; 5s timeout; no cross-employee blocking |
| New index | Improves overlap/Search/resolution employee lookups; small extra cost on Insert/Update/Activate |
| `ISJSON` | Negligible vs existing JSON size/handling |
| History trigger | Fires only on illegal UPDATE/DELETE; zero cost on normal INSERT snapshots |

Overall: low risk; write latency increase expected to be sub-millisecond under normal load for a single employee lock.

---

## Success criteria

| Criterion | Result |
|-----------|--------|
| Invalid JSON cannot be stored | ✓ `50009` / `50029` |
| Expired policies cannot be activated | ✓ `50032` |
| Overlapping active policies cannot be activated | ✓ `50033` |
| Concurrent updates cannot create inconsistent data | ✓ app lock + `UPDLOCK, HOLDLOCK` |
| History remains immutable | ✓ `50050` |
| Search, CRUD, History, Compare, Export, Timeline unchanged | ✓ no app/UI changes |
| No regression in Policy Resolution | ✓ resolution not modified |
