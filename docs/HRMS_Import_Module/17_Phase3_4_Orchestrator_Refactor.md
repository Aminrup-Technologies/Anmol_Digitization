# 17 — Phase 3.4 Orchestrator Refactor (`usp_RunHRMSSynchronization`)

**Scope:** `dbo.usp_RunHRMSSynchronization` only (orchestrator).  
**Script:** [`Database/Stored Procedures/HRMS/usp_RunHRMSSynchronization.sql`](../../Database/Stored%20Procedures/HRMS/usp_RunHRMSSynchronization.sql)  
**Unchanged:** ASPX, C#, CSS, JavaScript, `usp_HSE_ValidateBatch`, and (not generated here) child sync HSE procedures.

---

## 1. Old architecture

```text
HrmsBatchManager.btnRun_Click
    ↓
usp_HSE_ValidateBatch          ← Phase 3.3 (UI)
    ↓ (IsValid = 1)
usp_RunHRMSSynchronization     ← monolithic / opaque body
    └─ (historic) inline sync logic and/or nested legacy procs
         against staging / masters / MST_UserMaster
```

Problems with the old shape (from architecture analysis):

- Business logic co-located with coordination  
- Hard to unit-test masters / insert / update / hierarchy independently  
- PoC table risk if any legacy body still referenced `KKM_SO_TADA_Export_14Nov25$`  
- Return shape / multi-result-set ambiguity for `SqlDataAdapter.Fill`

---

## 2. New architecture

```text
HrmsBatchManager.btnRun_Click
    ↓
usp_HSE_ValidateBatch                 ← UI (Phase 3.3) — not inside orchestrator
    ↓ (IsValid = 1)
usp_RunHRMSSynchronization            ← ORCHESTRATOR ONLY (this phase)
    ├─ 1. usp_HSE_SynchronizeMasters
    ├─ 2. usp_HSE_InsertNewEmployees
    ├─ 3. usp_HSE_UpdateChangedEmployees
    └─ 4. usp_HSE_UpdateReportingHierarchy
    ↓
Update ImportBatchMaster.Status
Finalize Pending → Processed on clean staging rows
Return single summary result set
```

**Orchestrator may:**

- Parse `@Modules` / `@ProcessingMode` to decide which children to call  
- Open/commit/rollback a transaction around child EXEC calls  
- Aggregate OUTPUT counts  
- Update `ImportBatchMaster.Status`  
- Finalize staging `ImportStatus` for remaining `Pending` rows  
- Return the summary RS consumed by BatchManager  

**Orchestrator must not:**

- Contain master upsert / employee insert-update / hierarchy business SQL  
- Reference the PoC table  
- Replace or remove the UI validation step  

---

## 3. Call sequence

```mermaid
sequenceDiagram
    participant UI as HrmsBatchManager
    participant V as usp_HSE_ValidateBatch
    participant O as usp_RunHRMSSynchronization
    participant M as usp_HSE_SynchronizeMasters
    participant I as usp_HSE_InsertNewEmployees
    participant U as usp_HSE_UpdateChangedEmployees
    participant H as usp_HSE_UpdateReportingHierarchy

    UI->>V: EXEC (BatchId, Mode, Modules)
    V-->>UI: IsValid + summary
    alt IsValid = 0
        UI-->>UI: bind validation; stop
    else IsValid = 1
        UI->>O: EXEC (existing 6 params)
        O->>O: BEGIN TRAN; Status=Processing
        opt MASTER in Modules
            O->>M: EXEC
        end
        opt EMPLOYEE in Modules
            O->>I: EXEC (mode 0/1/2)
            opt Mode in (1,2)
                O->>U: EXEC
            end
        end
        opt HIERARCHY in Modules
            O->>H: EXEC
        end
        O->>O: Pending→Processed; Status=Processed/Failed
        O->>O: COMMIT
        O-->>UI: summary RS (first/only)
    end
```

### Module / mode gating (coordination)

| Module token | ProcessingMode | Child called |
|---|---|---|
| `MASTER` | any | `usp_HSE_SynchronizeMasters` |
| `EMPLOYEE` | `0`, `1`, `2` | `usp_HSE_InsertNewEmployees` |
| `EMPLOYEE` | `1`, `2` | `usp_HSE_UpdateChangedEmployees` |
| `HIERARCHY` | any | `usp_HSE_UpdateReportingHierarchy` |

Mode `0` does **not** call UpdateChangedEmployees.

---

## 4. Transaction scope

```text
BEGIN TRANSACTION
  UPDATE ImportBatchMaster.Status = 'Processing'
  EXEC child procedures (participate in same transaction)
  UPDATE staging Pending → Processed (clean rows)
  UPDATE ImportBatchMaster.Status = Processed | Failed
COMMIT
```

On error:

```text
ROLLBACK TRANSACTION
best-effort UPDATE ImportBatchMaster.Status = 'Failed'  (separate try)
RAISERROR (original error)
```

**Requirement on children:** must not issue independent `COMMIT`/`ROLLBACK` that breaks the outer transaction. They should use set-based DML only and let the orchestrator own the transaction boundary.

`SET XACT_ABORT ON` ensures most runtime errors abort the batch and trip the CATCH/ROLLBACK path.

---

## 5. Error handling

| Case | Behavior |
|---|---|
| Invalid `@BatchId` / missing batch | `RAISERROR` before TRAN; no child calls |
| Empty / invalid `@Modules` tokens | `RAISERROR` before TRAN |
| Invalid `@ProcessingMode` | `RAISERROR` before TRAN |
| Child procedure throws | CATCH → ROLLBACK → Status=`Failed` → re-raise |
| Partial row errors counted by children (`@ErrorCount`) | Do **not** auto-rollback; batch completes; Status=`Processed` if any work done, else `Failed` when only errors |

UI catch block in `btnRun_Click` still surfaces `ex.Message` on hard failures (unchanged C#).

---

## 6. Return contract (BatchManager-compatible)

`HrmsBatchManager` uses `SqlDataAdapter.Fill(DataTable)` → **first result set only**.

Orchestrator returns **one** summary result set:

| Column | Meaning |
|---|---|
| `BatchId` | Echo |
| `TotalRecords` | Staging rows for batch (pre-sync count) |
| `InsertedCount` | From insert child OUTPUT |
| `UpdatedCount` | From update child OUTPUT |
| `SkippedCount` | Sum of skip OUTPUTs |
| `ErrorCount` | Sum of error OUTPUTs |
| `MastersInserted` | From masters child |
| `MastersUpdated` | From masters child |
| `HierarchyUpdated` | From hierarchy child |
| `Status` | Final `ImportBatchMaster.Status` |
| `Message` | Short completion text |

No leading debug result set (avoids binding the wrong table in `gvResult`).

### Preserved input parameters (UI contract)

| Parameter | From BatchManager |
|---|---|
| `@BatchId` | Selected batch |
| `@AllowOverwrite` | `ProcessingMode != 0` |
| `@ProcessingMode` | `0\|1\|2` |
| `@Modules` | `MASTER\|EMPLOYEE\|HIERARCHY` |
| `@OverwriteFields` | Pipe-delimited field codes |
| `@ExecutedBy` | Session user |

---

## 7. Rollback strategy

| Layer | Strategy |
|---|---|
| Hard SQL error / missing child proc | Full `ROLLBACK` of all child DML in the orchestrator transaction |
| Status after rollback | Best-effort `ImportBatchMaster.Status = 'Failed'` outside the rolled-back work |
| Row-level business errors inside children | Recorded on staging by children; orchestrator commits and reports `ErrorCount` |
| UI validation failure | Sync never called (Phase 3.3); no orchestrator transaction |

---

## 8. Expected child procedure contracts

These are **not** generated in this phase. The orchestrator expects:

### `usp_HSE_SynchronizeMasters`
```text
@BatchId INT,
@ExecutedBy NVARCHAR(128) = NULL,
@MastersInserted INT OUTPUT,
@MastersUpdated INT OUTPUT
```

### `usp_HSE_InsertNewEmployees`
```text
@BatchId INT,
@ExecutedBy NVARCHAR(128) = NULL,
@InsertedCount INT OUTPUT,
@SkippedCount INT OUTPUT,
@ErrorCount INT OUTPUT
```

### `usp_HSE_UpdateChangedEmployees`
```text
@BatchId INT,
@AllowOverwrite BIT,
@ProcessingMode INT,
@OverwriteFields NVARCHAR(MAX) = NULL,
@ExecutedBy NVARCHAR(128) = NULL,
@UpdatedCount INT OUTPUT,
@SkippedCount INT OUTPUT,
@ErrorCount INT OUTPUT
```

### `usp_HSE_UpdateReportingHierarchy`
```text
@BatchId INT,
@ExecutedBy NVARCHAR(128) = NULL,
@HierarchyUpdatedCount INT OUTPUT,
@ErrorCount INT OUTPUT
```

All children must read `dbo.Staging_EmployeeData WHERE BatchId = @BatchId` only.

---

## 9. Staging / batch status rules (orchestrator)

| Object | Action |
|---|---|
| `ImportBatchMaster.Status` | `Processing` → `Processed` or `Failed` |
| `Staging_EmployeeData.ImportStatus` | After successful children: `Pending` + empty `ErrorMessage` → `Processed` |
| Error rows | Left as `Error` (set by validation / children) |

---

## 10. Deployment prerequisites

| Item | Status |
|---|---|
| `usp_HSE_ValidateBatch` | Exists (Phase 3.1); called from UI |
| `usp_HSE_SynchronizeMasters` | **Required next** — not in repo yet |
| `usp_HSE_InsertNewEmployees` | **Required next** — not in repo yet |
| `usp_HSE_UpdateChangedEmployees` | **Required next** — not in repo yet |
| `usp_HSE_UpdateReportingHierarchy` | **Required next** — not in repo yet |
| `usp_RunHRMSSynchronization` (this script) | Delivered as orchestrator |

Until child procedures are deployed, EXEC of the orchestrator will fail at the first missing child (transaction rolled back; batch marked `Failed`).

---

## 11. Assumptions (explicit)

1. Validated SSMS business SQL will live **inside** the four child procedures, not the orchestrator.  
2. UI continues to call `usp_HSE_ValidateBatch` before the orchestrator (Phase 3.3).  
3. Mode `1`/`2` still invoke Insert for brand-new codes (coordination choice documented above); change only if validated SQL forbids it.  
4. Single summary RS is intentional for BatchManager `Fill` compatibility.  
5. No ASPX/C# changes in this phase.

---

## 12. Recommended next implementation phases

1. Implement `usp_HSE_SynchronizeMasters` (validated master upsert logic).  
2. Implement `usp_HSE_InsertNewEmployees`.  
3. Implement `usp_HSE_UpdateChangedEmployees`.  
4. Implement `usp_HSE_UpdateReportingHierarchy`.  
5. Integration test: Validate → Orchestrator → summary → staging/batch statuses.
