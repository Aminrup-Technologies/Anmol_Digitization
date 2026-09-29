# 27 — Phase 4.3 Transaction & Error-Handling Hardening

**Status:** Delivered.  
**Scope:** `dbo.usp_RunHRMSSynchronization` transaction lifecycle and error propagation only.  
**Addresses:** Orchestrator review §1–§3 (transaction / TRY/CATCH / `XACT_STATE`).  
**Unchanged:** Synchronization rules, child HSE business logic, validation, Phase 4.2 summary result-set contract, UI.

---

## 1. Transaction lifecycle

```text
Pre-TRY guards (THROW 50001–50004)     ← no transaction opened
    │
BEGIN TRY
    IF @@TRANCOUNT = 0
        BEGIN TRAN; @StartedTran = 1   ← nested-safe ownership
    Status = Processing
    EXEC children (join ambient)
    Finalize staging Pending → Processed
    Status = Processed | Failed        ← success-path soft-error rules
    IF @StartedTran = 1 COMMIT
END TRY
BEGIN CATCH
    XACT_STATE()-gated ROLLBACK
    Best-effort Status = Failed        ← after rollback (autocommit)
    THROW;                             ← original error metadata preserved
END CATCH
SELECT summary…                        ← success path only (unchanged)
```

| Phase | Transaction | `ImportBatchMaster.Status` |
|---|---|---|
| Guards fail | None | Unchanged (sync never started) |
| Sync body start | Opened (or joined) | `Processing` |
| Sync success | Committed | `Processed` or `Failed` (soft-error rules) |
| Sync hard failure | Rolled back | `Failed` (post-rollback stamp) |

---

## 2. `XACT_STATE()` implementation

### Before

```sql
IF @@TRANCOUNT > 0
    ROLLBACK TRANSACTION;
```

### After

```sql
IF XACT_STATE() = -1
    ROLLBACK TRANSACTION;                          -- doomed: must clear
ELSE IF XACT_STATE() = 1 AND @StartedTran = 1
    ROLLBACK TRANSACTION;                          -- own committable tran: undo
-- XACT_STATE() = 0 → no ROLLBACK
-- XACT_STATE() = 1 AND @StartedTran = 0 → leave caller tran; THROW
```

| `XACT_STATE()` | Meaning | Action |
|---|---|---|
| `-1` | Uncommittable (typical with `XACT_ABORT ON`) | Always `ROLLBACK` |
| `1` | Committable transaction active | `ROLLBACK` only if `@StartedTran = 1` |
| `0` | No active transaction | Do not `ROLLBACK` |

`SET XACT_ABORT ON` remains at procedure start.

---

## 3. Error propagation

| Location | Before | After | Reason |
|---|---|---|---|
| Pre-TRY input guards | `RAISERROR` + `RETURN` | `THROW 50001`…`50004` | Stops immediately; documented app errors |
| `CATCH` re-throw | `RAISERROR(ERROR_MESSAGE(), severity, state)` + `RETURN` | bare `THROW;` | Preserves original error number, severity, state, procedure, line |

### Documented `THROW` numbers (application guards)

| Number | Condition |
|---|---|
| `50001` | `@BatchId` null / ≤ 0 |
| `50002` | Batch not in `ImportBatchMaster` |
| `50003` | `@Modules` has no MASTER / EMPLOYEE / HIERARCHY |
| `50004` | `@ProcessingMode` not in (0, 1, 2) |

`RAISERROR` is **not** used in this procedure after Phase 4.3.

UI impact: `HrmsBatchManager` already surfaces `ex.Message` in `catch`; message text remains readable. Original SQL error numbers are available to ADO.NET (`SqlException.Number`) for diagnostics.

---

## 4. Batch status transitions

| Event | Status | Who sets it |
|---|---|---|
| Upload | `Uploaded` | `HrmsDataImport` (unchanged) |
| Validation failure | *(batch status not written by ValidateBatch)*; sync **not** called | UI gate — **preserved**: orchestrator never runs, so it cannot overwrite a validation outcome |
| Sync start | `Processing` | Orchestrator (inside tran) |
| Sync success (no / partial soft errors per existing rules) | `Processed` | Orchestrator (inside tran, then commit) |
| Sync success path with only soft errors and no DML progress | `Failed` | Orchestrator (existing soft-error rule — unchanged) |
| Sync hard failure (`CATCH`) | `Failed` | Orchestrator **after** rollback |

### Preservation rules

| Status | Preserved how |
|---|---|
| `Processed` | Success path commit unchanged |
| `Failed` | Always stamped in `CATCH` for any synchronization hard failure |
| Validation outcome / `Validation Failed` | Orchestrator is not invoked when UI validation fails; pre-TRY guards do not open a tran or rewrite status |

---

## 5. Rollback strategy

| Principle | Implementation |
|---|---|
| Full atomic sync unit | One ambient transaction around all selected modules |
| No savepoints | Hard failure undoes masters + insert + update + hierarchy + in-tran status writes |
| Partial-commit prevention | Children do not commit when nested; orchestrator commits once or rolls back once |
| Persist failure flag | `Status = Failed` runs **after** rollback so it is not undone |
| Nested caller safety | Rollback of a *committable* tran only when `@StartedTran = 1`; doomed tran always rolled back |

Soft failures (MissingMaster / MissingManager counted in `ErrorCount`) are **not** hard SQL failures — they commit with existing Processed/Failed rules. That is reporting/status logic, not a transaction change.

---

## 6. Nested transaction behaviour

### Children → orchestrator (normal BatchManager path)

| Child pattern | When orchestrator owns tran |
|---|---|
| `IF @@TRANCOUNT = 0 BEGIN TRAN; @StartedTran = 1` | `@@TRANCOUNT > 0` → child **does not** open a tran |
| Child `COMMIT` only if `@StartedTran = 1` | No child commit |
| Child `CATCH` rollback only if `@StartedTran = 1` | Child does not roll back; re-raises to orchestrator |

Result: children participate in the orchestrator transaction. A child hard failure surfaces to the orchestrator `CATCH`, which applies `XACT_STATE()` rollback.

### Orchestrator → outer caller (defensive)

| Scenario | Behaviour after Phase 4.3 |
|---|---|
| Standalone (`@@TRANCOUNT = 0` at entry) | Orchestrator opens, commits or rolls back — identical operator outcome to before |
| Nested under caller tran | Joins ambient; commits only its own work via caller; on failure rolls back only if doomed or if it opened the tran |

BatchManager does not wrap the call in an explicit ADO.NET transaction today.

---

## 7. Before / After comparison

| Topic | Before | After |
|---|---|---|
| Open tran | Always `BEGIN TRANSACTION` | Only if `@@TRANCOUNT = 0` (`@StartedTran`) |
| Commit | Always `COMMIT` | Only if `@StartedTran = 1` |
| Rollback gate | `@@TRANCOUNT > 0` | `XACT_STATE()` (+ ownership) |
| Re-throw | `RAISERROR(message,…)` | `THROW;` |
| Input guards | `RAISERROR` + `RETURN` | `THROW 5000x` |
| Failed stamp | Best-effort after rollback | Same intent; clarified vs validation |
| Sync rules / KPIs | — | **Unchanged** |
| Result set | Phase 4.2 contract | **Unchanged** |

---

## 8. SQL modified

| File | Change |
|---|---|
| `Database/Stored Procedures/HRMS/usp_RunHRMSSynchronization.sql` | Nested-safe tran ownership; `XACT_STATE()` rollback; `THROW` propagation |

No child HSE procedures modified in this phase.  
No ASP.NET / validation changes.

---

## 9. Production readiness checklist

### Database

- [x] Sync work is a single atomic unit (one ambient transaction)
- [x] Hard failure does not leave partial child DML committed
- [x] `ImportBatchMaster.Status = Failed` persists after rollback
- [x] Success path still commits `Processed` (or existing soft-error `Failed`)
- [x] `SET XACT_ABORT ON` retained
- [x] `XACT_STATE()` used before `ROLLBACK`

### Stored Procedures

- [x] Orchestrator remains thin (no business DML beyond status / staging finalize)
- [x] Child nested-join pattern unchanged (`@@TRANCOUNT = 0` ownership)
- [x] Phase 4.2 summary result-set contract unchanged
- [x] Phase 4.1 warning finalization predicate unchanged
- [x] Pre-sync validation remains UI → `usp_HSE_ValidateBatch` (not inside orchestrator)
- [ ] Child CATCH still uses `RAISERROR` for re-throw (optional follow-up; nested path relies on orchestrator `THROW`)

### ASP.NET

- [x] `HrmsBatchManager` still uses `Fill` → first result set (`AutoGenerateColumns`)
- [x] Sync exceptions still surface via `catch (Exception ex)` → `lblPageError`
- [x] Validation-failure path still blocks sync (orchestrator not called)
- [x] No UI redesign

### Batch Processing

- [x] Module / mode gating unchanged
- [x] Call order Master → Insert → Update → Hierarchy unchanged
- [x] Soft-error KPI rollup unchanged
- [x] Staging `ImportStatus` finalization unchanged

### Logging / diagnostics

- [x] `THROW;` preserves native SQL error metadata for `SqlException`
- [x] Application guard errors use stable numbers `50001`–`50004`
- [x] Staging `ErrorMessage` / `[WARNING]` text retained (Phase 4.1)

### Rollback

- [x] Doomed transactions always rolled back (`XACT_STATE() = -1`)
- [x] Owned committable transactions rolled back on failure
- [x] No invalid `ROLLBACK` when `XACT_STATE() = 0`
- [x] No savepoint-based partial child commit

### Validation

- [x] Validation procedure not modified
- [x] Validation failure still prevents synchronization in UI
- [x] Orchestrator does not rewrite validation outcomes on the blocked path

### Performance

- [x] No extra result sets on success path
- [x] No additional child round-trips
- [x] Hardening is control-flow only (negligible CPU vs DML)

---

## 10. Acceptance

- [x] `@@TRANCOUNT > 0 ROLLBACK` replaced with `XACT_STATE()` gating  
- [x] `ROLLBACK` only when valid  
- [x] Sync hard failure → `ImportBatchMaster.Status = Failed`  
- [x] Success → `Processed` preserved  
- [x] Validation path preserved (sync not entered)  
- [x] `THROW` used for re-throw and application guards  
- [x] Nested ownership documented and implemented  
- [x] No business / sync / result-set contract changes  
