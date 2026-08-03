# Phase 3.8 — `usp_HSE_UpdateReportingHierarchy`

**Status:** SQL delivered for independent compile / unit test.  
**Scope:** Update **reporting hierarchy columns only** on existing `MST_UserMaster` rows. No other employee master fields. No inserts.

**Script:** [`Database/Stored Procedures/HRMS/usp_HSE_UpdateReportingHierarchy.sql`](../../Database/Stored%20Procedures/HRMS/usp_HSE_UpdateReportingHierarchy.sql)

---

## 1. Parameters

| Parameter | Type | Direction | Required | Description |
|---|---|---|---|---|
| `@BatchId` | `INT` | IN | Yes | Staging / batch key |
| `@ExecutedBy` | `NVARCHAR(128)` | IN | No | Echoed on standalone RS only |
| `@HierarchyUpdatedCount` | `INT` | OUT | — | Rows with hierarchy columns updated (orchestrator) |
| `@ErrorCount` | `INT` | OUT | — | Orchestrator rollup (= `@MissingManagerCount`) |
| `@HierarchyUpdated` | `INT` | OUT | — | Alias of `@HierarchyUpdatedCount` |
| `@NoChangeCount` | `INT` | OUT | — | Eligible rows whose hierarchy already matched |
| `@MissingManagerCount` | `INT` | OUT | — | L1 and/or L2 manager code not in `MST_UserMaster` |
| `@SkippedCount` | `INT` | OUT | — | Employee missing / no manager code / self-report / circular |

### Example call (standalone)

```sql
DECLARE
    @HierarchyUpdatedCount INT,
    @ErrorCount INT,
    @HierarchyUpdated INT,
    @NoChangeCount INT,
    @MissingManagerCount INT,
    @SkippedCount INT;

EXEC dbo.usp_HSE_UpdateReportingHierarchy
    @BatchId               = 123,
    @ExecutedBy            = N'test.user',
    @HierarchyUpdatedCount = @HierarchyUpdatedCount OUTPUT,
    @ErrorCount            = @ErrorCount OUTPUT,
    @HierarchyUpdated      = @HierarchyUpdated OUTPUT,
    @NoChangeCount         = @NoChangeCount OUTPUT,
    @MissingManagerCount   = @MissingManagerCount OUTPUT,
    @SkippedCount          = @SkippedCount OUTPUT;
```

### Orchestrator call (Phase 3.4)

```sql
EXEC dbo.usp_HSE_UpdateReportingHierarchy
    @BatchId               = @BatchId,
    @ExecutedBy            = @ExecutedBy,
    @HierarchyUpdatedCount = @StepHierarchyUpdated OUTPUT,
    @ErrorCount            = @StepErrors OUTPUT;
```

No ASPX / C# / orchestrator changes in this phase.

---

## 2. Hierarchy resolution

| Role | Staging column | Resolve against |
|---|---|---|
| Employee | `EmpCode` | `MST_UserMaster.EmployeeCode` |
| Manager (L1) | `ReportingManagerCode` | `MST_UserMaster.EmployeeCode` |
| Manager’s manager (L2) | `ReportingManagersManagerCode` | `MST_UserMaster.EmployeeCode` |

### Columns written (only these)

| Target column | Source when applied |
|---|---|
| `ReportingManager` | L1 manager’s `EmployeeName` from `MST_UserMaster` |
| `ReportingManagerCode` | Staging `ReportingManagerCode` |
| `ReportingManagersManagerName` | L2 manager’s `EmployeeName` (only if L2 code supplied) |
| `ReportingManagersManagerCode` | Staging `ReportingManagersManagerCode` (only if supplied) |

If L2 code is blank in staging, L2 columns are **left unchanged** (L1-only hierarchy refresh).

Display names are taken from `MST_UserMaster` when the code resolves (not free-text staging names), keeping hierarchy labels consistent with the employee master.

---

## 3. Missing manager handling

| Condition | Behaviour |
|---|---|
| L1 `ReportingManagerCode` not in `MST_UserMaster` | Skip employee; `@MissingManagerCount++`; staging `[WARNING]` |
| L2 code supplied but not in `MST_UserMaster` | Skip employee; `@MissingManagerCount++`; staging `[WARNING]` |
| No L1 code supplied | Skip hierarchy update for that row (`@SkippedCount++`); no master DML |

Missing-manager skips do **not** rewrite any `MST_UserMaster` columns.  
`@ErrorCount` is set equal to `@MissingManagerCount` for orchestrator rollup.

---

## 4. Circular reference prevention

| Guard | Rule |
|---|---|
| Self-reporting | `EmpCode = ReportingManagerCode` **or** `EmpCode = ReportingManagersManagerCode` → skip (`@SkippedCount`) |
| Cycle walk | From the **proposed L1 manager**, walk upward using the **effective** manager graph: proposed `ReportingManagerCode` for EmpCodes in this batch, else current `MST_UserMaster.ReportingManagerCode` |
| Cycle hit | If the walk encounters the employee’s own `EmpCode` within 50 levels → skip as circular |
| Batch-aware | Catches A→B and B→A (and longer cycles) when both sides are present in staging |

Circular / self-report rows get a staging `[WARNING]`; hierarchy columns remain unchanged.

---

## 5. Update comparison rules

1. Employee must already exist in `MST_UserMaster`.
2. L1 manager code required and must exist; L2 code optional but must exist when present.
3. Pass self-report and circular checks.
4. UPDATE only when at least one in-scope hierarchy value differs (`HasChange = 1`).
5. Never update EmployeeCode, org FKs, status, contact fields, password, Created*, CreationMode, or DPDP fields.

---

## 6. Output contract

| Output | Meaning |
|---|---|
| `@HierarchyUpdatedCount` / `@HierarchyUpdated` | Rows whose hierarchy columns were updated |
| `@NoChangeCount` | Eligible rows already matching proposed hierarchy |
| `@MissingManagerCount` / `@ErrorCount` | Missing L1/L2 manager codes |
| `@SkippedCount` | Not in master / blank L1 / self-report / circular |

### Standalone result set

Returned only when the procedure started its own transaction (suppressed when nested):

`BatchId`, `HierarchyUpdatedCount`, `HierarchyUpdated`, `NoChangeCount`, `MissingManagerCount`, `SkippedCount`, `ErrorCount`, `ExecutedBy`, `Message`.

---

## 7. Transaction scope

| Scenario | Behaviour |
|---|---|
| Standalone | Owns local transaction |
| Nested under orchestrator | Joins ambient transaction |
| Hard error | Local rollback if owned; re-raise for orchestrator |

---

## 8. Test scenarios

| ID | Scenario | Expected |
|---|---|---|
| T01 | Existing emp, valid L1 manager, values differ | `@HierarchyUpdatedCount = 1` |
| T02 | Same hierarchy already present | `@NoChangeCount = 1`; no UPDATE |
| T03 | Manager code not in `MST_UserMaster` | `@MissingManagerCount = 1`; hierarchy unchanged |
| T04 | L2 code missing in master | `@MissingManagerCount = 1`; skip |
| T05 | `EmpCode = ReportingManagerCode` | `@SkippedCount ≥ 1`; no UPDATE |
| T06 | A→B and B→A in same batch | Both flagged circular / skipped appropriately |
| T07 | Longer cycle A→B→C→A | Skip A (and similarly others when processed) |
| T08 | Blank `ReportingManagerCode` | `@SkippedCount++`; no clear of existing hierarchy |
| T09 | EmpCode not in master | `@SkippedCount++` |
| T10 | Only hierarchy columns change | Branch/Dept/Email/etc. untouched |
| T11 | Nested under orchestrator | No child client RS; OUTPUTs roll into summary |
| T12 | Idempotent re-run | Second run → `@HierarchyUpdatedCount = 0` |

---

## 9. Assumptions

1. Validated SSMS hierarchy intent: EmpCode match; manager codes must exist; change-only writes; self/cycle guards; staging by `BatchId` only.
2. Prefer manager display names from `MST_UserMaster` when codes resolve.
3. Hierarchy module may run after Insert/Update so new employees from the same batch already exist when codes refer to them.
4. No ASPX, C#, or orchestrator changes in this phase.

---

## 10. Deployment notes

| Item | Status |
|---|---|
| `usp_HSE_SynchronizeMasters` | Prerequisite for org masters |
| `usp_HSE_InsertNewEmployees` | Recommended before hierarchy when batch introduces new managers |
| `usp_HSE_UpdateChangedEmployees` | Independent; may also touch hierarchy fields when selected |
| `usp_HSE_UpdateReportingHierarchy` | Delivered this phase — final HSE child |
| `usp_RunHRMSSynchronization` | Already calls this child when `HIERARCHY` module selected |

With this procedure deployed, the modular HSE child set expected by the Phase 3.4 orchestrator is complete.
