# 25 — Phase 4.1 Warning Finalization (ORCH-02)

**Status:** Delivered.  
**Scope:** Staging `ImportStatus` finalization inside `usp_RunHRMSSynchronization` only.  
**Addresses:** ORCH-02 from [`24_Orchestrator_Review.md`](24_Orchestrator_Review.md).  
**Unchanged:** UI, validation rules, `ErrorMessage` text, result-set contract, child HSE business logic.

---

## 1. Where finalization happens

| Procedure | Touches `ImportStatus`? | Role |
|---|---|---|
| `usp_HSE_ValidateBatch` | Yes | Sets `Pending` / `Error` during validation (not finalization) |
| `usp_HSE_SynchronizeMasters` | No | Masters only |
| `usp_HSE_InsertNewEmployees` | Yes (hard fail) | Sets `Error` for missing required Branch/Plant |
| `usp_HSE_UpdateChangedEmployees` | No status flip to Processed | May **append** `[WARNING]…` while leaving `Pending` |
| `usp_HSE_UpdateReportingHierarchy` | No status flip to Processed | May **append** `[WARNING]…` while leaving `Pending` |
| **`usp_RunHRMSSynchronization`** | **Yes — sole Processed finalizer** | End-of-run `Pending` → `Processed` |

Only the orchestrator’s finalization predicate required change.

---

## 2. Previous behaviour

```sql
WHERE ImportStatus = 'Pending'
  AND (ErrorMessage IS NULL OR LTRIM(RTRIM(ErrorMessage)) = '')
```

| Staging state after sync | Result |
|---|---|
| `Pending` + `ErrorMessage` NULL / `''` | → `Processed` |
| `Pending` + `ErrorMessage` starts with `[WARNING]…` | **Stayed `Pending`** (bug) |
| `Error` (hard failure) | Unchanged `Error` |

Warnings are informational (unknown master creatable by SynchronizeMasters, unresolved hierarchy manager, etc.). Leaving those rows `Pending` after a successful orchestrator commit was incorrect for operators and batch aggregates (`WaitingCount`).

---

## 3. New behaviour

```sql
WHERE ImportStatus = 'Pending'
  AND (
        ErrorMessage IS NULL
     OR LTRIM(RTRIM(ErrorMessage)) = N''
     OR LTRIM(ErrorMessage) LIKE N'[[]WARNING]%'
      )
```

| Staging state after sync | Result |
|---|---|
| `Pending` + `ErrorMessage` NULL | → `Processed` |
| `Pending` + `ErrorMessage` `''` | → `Processed` |
| `Pending` + `ErrorMessage` starts with `[WARNING]` | → `Processed` |
| `ImportStatus = 'Error'` (hard error) | Remains `Error` |

`LIKE N'[[]WARNING]%'` matches a literal `[WARNING]` prefix (SQL Server character-class escape for `[`).

Leading spaces are ignored via `LTRIM` before the prefix test.

---

## 4. SQL modified

| File | Change |
|---|---|
| `Database/Stored Procedures/HRMS/usp_RunHRMSSynchronization.sql` | Finalization `WHERE` clause only |

No child HSE procedures modified.  
No validation procedure modified.  
No ASPX/C# modified.

---

## 5. Why warning text remains visible

Finalization updates **`ImportStatus` only**.

- `ErrorMessage` is not cleared, truncated, or rewritten.  
- Export / grid / `ErrorMessage LIKE '%[WARNING]%'` reporting continue to show informational warnings.  
- Operators can still see *why* a warning was raised while the row is correctly marked processed.

---

## 6. Why `Processed` is now correct

1. Synchronization completed inside the orchestrator transaction (commit path).  
2. A `[WARNING]`-prefixed message means “informational / non-blocking,” consistent with ValidateBatch createability warnings and child soft skips.  
3. Hard failures already set `ImportStatus = 'Error'` in validation or Insert; those rows are excluded by `ImportStatus = 'Pending'`.  
4. Batch waiting counts and “fully processed” reporting now align with successful sync outcomes that only carried warnings.

---

## 7. Explicit non-goals (this phase)

| Out of scope | Status |
|---|---|
| UI changes | Not done |
| Validation rule changes | Not done |
| Result-set contract | Unchanged |
| ORCH-01 SkippedCount semantics | Deferred to Phase 4.2 — see [`26_Phase4_2_Summary_KPI_Refinement.md`](26_Phase4_2_Summary_KPI_Refinement.md) |
| Clearing warning text after process | Intentionally not done |

---

## 8. Suggested verification

```sql
-- After a sync that produced warning-only staging rows:
SELECT ImportStatus, LEFT(ErrorMessage, 80) AS ErrorMessagePreview, COUNT(*) AS Cnt
FROM dbo.Staging_EmployeeData
WHERE BatchId = @BatchId
GROUP BY ImportStatus, LEFT(ErrorMessage, 80);

-- Expect: warning-prefixed rows have ImportStatus = 'Processed'
-- Expect: hard Error rows remain ImportStatus = 'Error'
-- Expect: ErrorMessage still contains [WARNING] text where applicable
```
