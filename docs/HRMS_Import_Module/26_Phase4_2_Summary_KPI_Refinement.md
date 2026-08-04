# 26 — Phase 4.2 Summary KPI Refinement (ORCH-01 / ORCH-04)

**Status:** Delivered.  
**Scope:** Summary result set of `dbo.usp_RunHRMSSynchronization` only.  
**Addresses:** ORCH-01 (`SkippedCount` overloaded) and ORCH-04 (`NoChangeCount` dropped) from [`24_Orchestrator_Review.md`](24_Orchestrator_Review.md).  
**Unchanged:** Validation, child HSE business rules, module/mode gating, transaction boundaries, warning finalization (Phase 4.1), UI layout.

---

## 1. Problem

| Issue | Detail |
|---|---|
| `SkippedCount` overloaded | Orchestrator summed `Insert.SkippedCount` (already in `MST_UserMaster`) with `Update.SkippedCount` (EmpCode not in master / mode short-circuit). Different meanings, one KPI. |
| `NoChangeCount` dropped | `usp_HSE_UpdateChangedEmployees` already computes `@NoChangeCount` (existing employees compared, no field change) but the orchestrator never captured or returned it. |

No synchronization behaviour was wrong; **reporting semantics** were.

---

## 2. Previous KPI contract

Success path — single result set bound by `HrmsBatchManager` via `SqlDataAdapter.Fill` → `gvResult`:

| Column | Source |
|---|---|
| `BatchId` | Input |
| `TotalRecords` | `COUNT` on staging for batch |
| `InsertedCount` | `usp_HSE_InsertNewEmployees.@InsertedCount` |
| `UpdatedCount` | `usp_HSE_UpdateChangedEmployees.@UpdatedCount` |
| `SkippedCount` | Insert.`@SkippedCount` **+** Update.`@SkippedCount` |
| `ErrorCount` | Sum of child `@ErrorCount` (soft Missing* rollups) |
| `MastersInserted` | `usp_HSE_SynchronizeMasters.@MastersInserted` |
| `MastersUpdated` | `usp_HSE_SynchronizeMasters.@MastersUpdated` |
| `HierarchyUpdated` | `usp_HSE_UpdateReportingHierarchy.@HierarchyUpdatedCount` |
| `Status` | `ImportBatchMaster` batch status after run |
| `Message` | `N'Synchronization completed.'` |

**Not returned:** `NoChangeCount`, `MissingMasterCount`, `MissingManagerCount` (except Missing* folded into `ErrorCount` via child `@ErrorCount`).

---

## 3. New KPI contract

| Column | Meaning | Source |
|---|---|---|
| `BatchId` | Batch identifier | Input (retained) |
| `TotalRecords` | Staging row count | Unchanged |
| `MastersInserted` | New master rows | SynchronizeMasters |
| `MastersUpdated` | Updated master rows | SynchronizeMasters |
| `EmployeesInserted` | New employees inserted | Insert.`@InsertedCount` |
| `EmployeesUpdated` | Existing employees with ≥1 applied change | Update.`@UpdatedCount` |
| `ExistingEmployees` | Employees already present; insert skipped | Insert.`@SkippedCount` **only** |
| `NoChangeCount` | Existing employees compared; no update required | Update.`@NoChangeCount` |
| `HierarchyUpdated` | Hierarchy columns updated | Hierarchy.`@HierarchyUpdatedCount` |
| `MissingMasterCount` | Soft unresolved master refs | Insert.`@MissingMasterCount` + Update.`@MissingMasterCount` |
| `MissingManagerCount` | Soft unresolved manager refs | Hierarchy.`@MissingManagerCount` |
| `ErrorCount` | Soft-failure rollup | Sum of child `@ErrorCount` (unchanged rule) |
| `Status` | Batch status | Unchanged rules |
| `Message` | Completion message | Unchanged |

Column order matches the SELECT in `usp_RunHRMSSynchronization` (masters before employee KPIs for operator readability).

---

## 4. Mapping old → new

| Previous column | New column(s) | Notes |
|---|---|---|
| `InsertedCount` | `EmployeesInserted` | Rename only; same counter |
| `UpdatedCount` | `EmployeesUpdated` | Rename only; same counter |
| `SkippedCount` | `ExistingEmployees` + `NoChangeCount` | **Split.** Old sum is retired. See §4.1 |
| `ErrorCount` | `ErrorCount` | Same rollup rule |
| `MastersInserted` / `MastersUpdated` / `HierarchyUpdated` / `TotalRecords` / `Status` / `Message` / `BatchId` | Same names | Unchanged |
| *(absent)* | `NoChangeCount` | Now surfaced from Update optional OUTPUT |
| *(absent as named columns)* | `MissingMasterCount`, `MissingManagerCount` | Previously only inside `ErrorCount` |

### 4.1 `SkippedCount` decomposition

| Child OUTPUT | New summary column | Meaning |
|---|---|---|
| Insert.`@SkippedCount` | `ExistingEmployees` | EmpCode already in `MST_UserMaster` — insert not performed |
| Update.`@NoChangeCount` | `NoChangeCount` | EmpCode exists; in-scope fields unchanged |
| Update.`@SkippedCount` | *(not a summary column)* | EmpCode not in master (or mode-0 short-circuit). **Not** added into `ExistingEmployees`. After insert-first coordination this is typically near zero; still available on the child procedure OUTPUT if needed |

**Do not reconstruct** old `SkippedCount` as `ExistingEmployees + NoChangeCount` either — that would still mix insert-skip with update-no-change. Operators should read the two columns separately.

Approximate recovery of the **old numeric sum** (for historical comparison only):

```
Old SkippedCount ≈ ExistingEmployees + Update.SkippedCount
```

(`Update.SkippedCount` is captured in a step-local variable inside the orchestrator but not returned.)

---

## 5. SQL changes

| File | Change |
|---|---|
| `Database/Stored Procedures/HRMS/usp_RunHRMSSynchronization.sql` | Capture optional child OUTPUTs; replace summary `SELECT`; comments for mapping |

### 5.1 Child OUTPUTs now wired (optional params already existed)

| Child | Additional OUTPUT captured |
|---|---|
| `usp_HSE_InsertNewEmployees` | `@MissingMasterCount` |
| `usp_HSE_UpdateChangedEmployees` | `@NoChangeCount`, `@MissingMasterCount` |
| `usp_HSE_UpdateReportingHierarchy` | `@MissingManagerCount` |

No new OUTPUT parameters were added to child procedures.  
No new OUTPUT parameters were added to the orchestrator procedure signature (UI continues to use the result set only).

### 5.2 What did **not** change

- Module / mode gating  
- Call order  
- Child EXEC business arguments (`@AllowOverwrite`, `@ProcessingMode`, `@OverwriteFields`, …)  
- Transaction / CATCH / `ImportBatchMaster` status rules  
- Phase 4.1 warning finalization predicate  
- Validation (`usp_HSE_ValidateBatch`)  
- Any child HSE body / business rules  

---

## 6. UI impact (`HrmsBatchManager`)

| Item | Finding |
|---|---|
| Binding | `btnRun_Click` → `SqlDataAdapter.Fill(dt)` → `gvResult.DataSource = dt` |
| Grid | `gvResult` has `AutoGenerateColumns="true"` — no hardcoded BoundFields for sync KPIs |
| Code-behind | No references to `InsertedCount`, `UpdatedCount`, or `SkippedCount` column names on the sync result |
| Required C# change | **None** |
| Operator-visible change | Column headers rename / split / add as in §3 |

First result set remains the only success RS; `Fill` compatibility is preserved.

---

## 7. Backward compatibility

| Concern | Assessment |
|---|---|
| UI GridView | Compatible without code change (`AutoGenerateColumns`) |
| Column name consumers | Any external consumer of `InsertedCount` / `UpdatedCount` / `SkippedCount` must switch to the new names / split |
| Procedure signature | Unchanged (`@BatchId`, `@AllowOverwrite`, `@ProcessingMode`, `@Modules`, `@OverwriteFields`, `@ExecutedBy`) |
| Sync behaviour | Unchanged |
| Deprecated alias columns | **Not** emitted — contract is replaced (documented here) |

Deploy SQL script before relying on the new grid headers in an environment that still has the old procedure definition.

---

## 8. ErrorCount vs Missing* columns

Children continue to set `@ErrorCount = @MissingMasterCount` (Insert/Update) or `@ErrorCount = @MissingManagerCount` (Hierarchy).

Therefore on a normal soft-failure path:

```
ErrorCount = MissingMasterCount + MissingManagerCount
```

`MissingMasterCount` and `MissingManagerCount` are exposed so operators can see **which** soft-failure class contributed, without changing how `ErrorCount` drives batch `Status` (`Processed` vs `Failed`).

---

## 9. Acceptance checklist

- [x] `SkippedCount` no longer returned as a mixed sum  
- [x] `ExistingEmployees` = Insert skip only  
- [x] `NoChangeCount` returned from Update  
- [x] `EmployeesInserted` / `EmployeesUpdated` rename clarity  
- [x] `MissingMasterCount` / `MissingManagerCount` surfaced  
- [x] Sync / validation / business rules untouched  
- [x] `HrmsBatchManager` first-RS binding remains valid  
- [x] Mapping documented (this file)
