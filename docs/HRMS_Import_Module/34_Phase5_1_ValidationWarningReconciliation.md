# 34 — Phase 5.1 Validation Warning Reconciliation

**Status:** Delivered.  
**Scope:** Reporting accuracy — clear obsolete ValidateBatch createability warnings after masters sync.  
**Unchanged:** Validation rules, SynchronizeMasters, Insert/Update/Hierarchy business logic, FK resolution, summary KPI contract.

---

## 1. Problem

ValidateBatch correctly emits createability warnings when a master business code is missing but a description is present, for example:

```text
[WARNING] SubDepartment_ID '1559' not found in MST_SubDepartment; expected to be created by SynchronizeMasters.
```

`usp_HSE_SynchronizeMasters` then creates the master. Synchronization and FK resolution succeed, but `Staging_EmployeeData.ErrorMessage` still holds the pre-sync warning. Batch reports therefore look incorrect after a successful run.

| Layer | Status |
|---|---|
| Validation (pre-sync) | Correct |
| SynchronizeMasters | Correct |
| Insert / Update FK resolution | Correct |
| Final ErrorMessage reporting | **Stale warning retained** |

---

## 2. Design

Introduce a dedicated modular procedure:

`dbo.usp_HSE_ReconcileValidationWarnings`

| Principle | Implementation |
|---|---|
| Do not re-run validation | No call to `usp_HSE_ValidateBatch` |
| Do not change validation rules | ValidateBatch untouched |
| Do not change SynchronizeMasters / Insert / Update | Untouched |
| Only obsolete createability warnings | Exact fragment match + master now exists |
| Preserve other text | Hard errors, hierarchy/update warnings, unrelated `[WARNING]` kept |
| Empty → NULL | After cleanup, blank `ErrorMessage` becomes `NULL` |
| ImportStatus | **Not modified** |

---

## 3. Eligible warnings

Only fragments containing:

```text
expected to be created by SynchronizeMasters
```

for these ValidateBatch master checks:

| Staging field | Master table | Warning fragment pattern |
|---|---|---|
| `Company_ID` | `MST_Company` | `[WARNING] Company_ID '…' not found in MST_Company; expected to be created by SynchronizeMasters.` |
| `Region_ID` | `MST_Region` | `[WARNING] Region_ID '…' not found in MST_Region; expected to be created…` |
| `Division_ID` | `MST_Division` | `[WARNING] Division_ID '…' not found in MST_Division; expected to be created…` |
| `Category_ID` | `MST_Category` | `[WARNING] Category_ID '…' not found in MST_Category; expected to be created…` |
| `PlantID_ID` | `MST_Plant` | `[WARNING] PlantID_ID '…' not found in MST_Plant; expected to be created…` |
| `Branch_ID` | `MST_Branch` | `[WARNING] Branch_ID '…' not found in MST_Branch; expected to be created…` |
| `Department_ID` | `MST_Department` | `[WARNING] Department_ID '…' not found in MST_Department; expected to be created…` |
| `SubDepartment_ID` | `MST_SubDepartment` | `[WARNING] SubDepartment_ID '…' not found in MST_SubDepartment; expected to be created…` |
| `Grade_ID` | `MST_Grade` | `[WARNING] Grade_ID '…' not found in MST_Grade; expected to be created…` |
| `Designation_ID` | `MST_Designation` | `[WARNING] Designation_ID '…' not found in MST_Designation.Designation_ID; expected to be created…` |

**Not eligible:** hard `Invalid master reference: … Description is empty.` errors; update/hierarchy soft-warnings; any other `ErrorMessage` content.

---

## 4. Reconciliation algorithm

```text
FOR each staging row in @BatchId
  WHERE ErrorMessage LIKE '%expected to be created by SynchronizeMasters%'

  FOR each eligible master type
    IF staging code is non-blank
       AND master row now EXISTS (same predicate as ValidateBatch)
       AND ErrorMessage contains the exact ValidateBatch warning fragment for that code
    THEN
       REPLACE that fragment with ''

  Tidy leftover '; ' separators
  IF ErrorMessage is blank → NULL
  Persist only when the message changed
```

Existence predicates mirror ValidateBatch / SynchronizeMasters (e.g. Branch uses `TRY_CONVERT(INT,…)`, Plant/Division/Category/Company/SubDepartment use `CAST(… AS NVARCHAR(50))`, Designation matches `Designation_ID` business code).

If SynchronizeMasters could not create a master (still missing), the warning is **left in place**.

---

## 5. Call sequence

```text
UI: usp_HSE_ValidateBatch
        ↓
usp_RunHRMSSynchronization
  1.  usp_HSE_SynchronizeMasters          (when MASTER module selected)
  1b. usp_HSE_ReconcileValidationWarnings ← NEW (same MASTER gate)
  2.  usp_HSE_InsertNewEmployees
  3.  usp_HSE_UpdateChangedEmployees
  4.  usp_HSE_UpdateReportingHierarchy
  *.  Staging finalize (Pending → Processed, warnings retained if still present)
```

Reconciliation runs inside the orchestrator transaction, immediately after masters, before employee insert/update.

---

## 6. Before / After examples

### Example A — SubDepartment only (UAT case)

**Before reconciliation**

| Staging.SubDepartment_ID | MST_SubDepartment | ErrorMessage |
|---|---|---|
| `1559` | `Id=122`, code `1559` (just created) | `[WARNING] SubDepartment_ID '1559' not found in MST_SubDepartment; expected to be created by SynchronizeMasters.` |

**After reconciliation**

| ErrorMessage |
|---|
| `NULL` |

### Example B — Multiple warnings; one resolved

**Before**

```text
[WARNING] SubDepartment_ID '1559' not found in MST_SubDepartment; expected to be created by SynchronizeMasters.; [WARNING] Reporting manager code 'X' not found in MST_UserMaster.
```

(Second warning is illustrative of a non-eligible / other warning that must remain.)

**After** (SubDepartment master exists; other warning preserved)

```text
[WARNING] Reporting manager code 'X' not found in MST_UserMaster.
```

### Example C — Master still missing

SynchronizeMasters did not create the row (e.g. no description path failed earlier as Error). Warning fragment remains unchanged.

---

## 7. Files

| File | Change |
|---|---|
| `Database/Stored Procedures/HRMS/usp_HSE_ReconcileValidationWarnings.sql` | **New** procedure |
| `Database/Stored Procedures/HRMS/usp_RunHRMSSynchronization.sql` | Call after SynchronizeMasters when MASTER runs |
| `docs/HRMS_Import_Module/34_Phase5_1_ValidationWarningReconciliation.md` | This document |

Optional OUTPUT: `@ReconciledCount` = number of staging rows whose `ErrorMessage` changed.

---

## 8. Backward compatibility

| Concern | Assessment |
|---|---|
| Orchestrator signature | Unchanged |
| Summary result set | Unchanged |
| Validation behaviour | Unchanged |
| Sync / FK behaviour | Unchanged |
| Deploy order | Deploy `usp_HSE_ReconcileValidationWarnings` before/with updated orchestrator |

---

## 9. Acceptance

- [x] Dedicated procedure (not embedded in SynchronizeMasters / Insert / Update)  
- [x] Called after SynchronizeMasters, before Insert  
- [x] Only “expected to be created by SynchronizeMasters” fragments removed when master exists  
- [x] Other warnings/errors preserved  
- [x] Empty message → `NULL`  
- [x] No validation re-run; no business-rule changes  
