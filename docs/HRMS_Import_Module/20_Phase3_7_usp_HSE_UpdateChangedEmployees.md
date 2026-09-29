# Phase 3.7 — `usp_HSE_UpdateChangedEmployees`

**Status:** SQL delivered for independent compile / unit test.  
**Scope:** Update **existing** `MST_UserMaster` rows only, and only when values actually changed. No inserts. No hierarchy-only procedure (reporting columns update only when those fields are in scope).

**Script:** [`Database/Stored Procedures/HRMS/usp_HSE_UpdateChangedEmployees.sql`](../../Database/Stored%20Procedures/HRMS/usp_HSE_UpdateChangedEmployees.sql)

---

## 1. Parameters

| Parameter | Type | Direction | Required | Description |
|---|---|---|---|---|
| `@BatchId` | `INT` | IN | Yes | Staging / batch key |
| `@AllowOverwrite` | `BIT` | IN | No | Legacy gate; `0` disables updates (same as mode 0) |
| `@ProcessingMode` | `INT` | IN | Yes* | `0` / `1` / `2` |
| `@OverwriteFields` | `NVARCHAR(MAX)` | IN | No | Pipe-delimited FieldCodes (mode 2) |
| `@ExecutedBy` | `NVARCHAR(128)` | IN | No | Echoed on standalone RS (audit trail only; does not rewrite CreatedBy) |
| `@UpdatedCount` | `INT` | OUT | — | Rows updated (orchestrator) |
| `@SkippedCount` | `INT` | OUT | — | Staging EmpCodes with no `MST_UserMaster` match (or mode-0 skipped existing) |
| `@ErrorCount` | `INT` | OUT | — | Orchestrator rollup (= `@MissingMasterCount`) |
| `@EmployeesUpdated` | `INT` | OUT | — | Alias of `@UpdatedCount` |
| `@NoChangeCount` | `INT` | OUT | — | Existing matches with no differing in-scope values |
| `@MissingMasterCount` | `INT` | OUT | — | Existing matches with ≥1 unresolved in-scope FK code |

\*Invalid mode raises an error.

### Example call (standalone)

```sql
DECLARE
    @UpdatedCount INT,
    @SkippedCount INT,
    @ErrorCount INT,
    @EmployeesUpdated INT,
    @NoChangeCount INT,
    @MissingMasterCount INT;

EXEC dbo.usp_HSE_UpdateChangedEmployees
    @BatchId            = 123,
    @AllowOverwrite     = 1,
    @ProcessingMode     = 2,
    @OverwriteFields    = N'EmployeeName|Email|Designation|Branch',
    @ExecutedBy         = N'test.user',
    @UpdatedCount       = @UpdatedCount OUTPUT,
    @SkippedCount       = @SkippedCount OUTPUT,
    @ErrorCount         = @ErrorCount OUTPUT,
    @EmployeesUpdated   = @EmployeesUpdated OUTPUT,
    @NoChangeCount      = @NoChangeCount OUTPUT,
    @MissingMasterCount = @MissingMasterCount OUTPUT;
```

### Orchestrator call (Phase 3.4)

```sql
EXEC dbo.usp_HSE_UpdateChangedEmployees
    @BatchId         = @BatchId,
    @AllowOverwrite  = @AllowOverwrite,
    @ProcessingMode  = @ProcessingMode,
    @OverwriteFields = @OverwriteFields,
    @ExecutedBy      = @ExecutedBy,
    @UpdatedCount    = @StepUpdated OUTPUT,
    @SkippedCount    = @StepSkipped OUTPUT,
    @ErrorCount      = @StepErrors OUTPUT;
```

No orchestrator / ASPX / C# changes in this phase.

---

## 2. ProcessingMode behaviour

| Mode | UI label | Behaviour |
|---|---|---|
| `0` | Insert Only | **No updates.** Counts existing EmpCode matches into `@SkippedCount` and returns. |
| `1` | Overwrite All | All supported fields are eligible. |
| `2` | Update Selected Fields | Only FieldCodes present in `@OverwriteFields` are eligible. |

Also: `@AllowOverwrite = 0` forces the same no-update path as mode `0`.

Orchestrator already gates this child to modes `1` and `2`; mode `0` handling remains for standalone safety.

---

## 3. OverwriteFields logic

- Format: pipe-delimited FieldCodes from `MST_HRMSImportField` (BatchManager `GetSelectedOverwriteFields`).
- Normalized: uppercased, spaces removed; commas treated as alternate separators.
- Matching is tolerant of common aliases:

| Logical field | Accepted FieldCode tokens (examples) |
|---|---|
| EmployeeName | `EmployeeName`, `EmpName` |
| EmployeeStatus | `EmployeeStatus`, `EmpStatus` |
| Email | `Email` |
| Mobile | `Mobile`, `MobileNo` |
| Gender | `Gender` |
| DOJ | `DOJ` |
| Branch | `Branch`, `BranchId`, `Branch_ID` |
| Department | `Department`, `DepartmentId`, `Department_ID` |
| Designation | `Designation`, `DesignationId`, `Designation_ID` |
| Grade | `Grade`, `GradeId`, `Grade_ID` |
| Plant | `Plant`, `PlantId`, `PlantID_ID` |
| Region | `Region`, `RegionId`, `Region_ID` |
| Division | `Division`, `DivisionId`, `Division_ID` |
| Category | `Category`, `CategoryId`, `Category_ID` |
| SubDepartment | `SubDepartment`, `SubDepartmentId`, `SubDepartment_ID` |
| ReportingManager | `ReportingManager` |
| ReportingManagerCode | `ReportingManagerCode` |
| ReportingManagersManagerName | `ReportingManagersManagerName` |
| ReportingManagersManagerCode | `ReportingManagersManagerCode` |

Mode `2` with empty `@OverwriteFields` → no fields eligible → existing matches counted as `@NoChangeCount`.

---

## 4. FK resolution

Same Id-store pattern as Insert (Phase 3.6):

| Target column | Match staging business code on | Store |
|---|---|---|
| `BranchId` | `MST_Branch.Branch_ID` | `MST_Branch.Id` |
| `DepartmentId` | `MST_Department.Department_ID` | `MST_Department.Id` |
| `Designation_ID` | `MST_Designation.Designation_ID` | `MST_Designation.Id` |
| `GradeId` | `MST_Grade.Grade_ID` | `MST_Grade.Id` |
| `PlantId` | `CAST(MST_Plant.PlantID_ID AS NVARCHAR(50))` | `MST_Plant.Id` |
| `RegionId` | `MST_Region.Region_ID` | `MST_Region.Id` |
| `DivisionId` | `CAST(MST_Division.Division_ID AS NVARCHAR(50))` | `MST_Division.Id` |
| `CategoryId` | `CAST(MST_Category.Category_ID AS NVARCHAR(50))` | `MST_Category.Id` |
| `SubDepartmentId` | `CAST(MST_SubDepartment.SubDepartment_ID AS NVARCHAR(50))` | `MST_SubDepartment.Id` |

### Designation chain

```text
Staging.Designation_ID → MST_Designation.Designation_ID → MST_Designation.Id → MST_UserMaster.Designation_ID
```

Employee match is **only** `EmployeeCode` (= staging `EmpCode`).

---

## 5. Update comparison rules

1. Field must be **in scope** (mode 1, or listed in mode 2).
2. Staging must supply a non-blank value for that field (blank does **not** clear the target).
3. For FKs: staging code must resolve to a master `Id`; unresolved codes leave the existing FK unchanged and count toward `@MissingMasterCount`.
4. UPDATE runs only when **at least one** in-scope proposed value differs from current (`ISNULL`-safe / date-safe compares).
5. Per-column `CASE` assignments avoid writing identical values; the UPDATE `WHERE` prevents no-op row touches.

### Never updated

`EmployeeCode`, `CreatedDate`, `CreatedBy`, `CreationMode`, `Password`, `PasswordHash`, `PasswordSalt`, `PasswordIterations`, `DPDPConsentGiven`, `DPDPConsentDate`.

---

## 6. Output contract

| Output | Meaning |
|---|---|
| `@UpdatedCount` / `@EmployeesUpdated` | Existing rows with ≥1 applied change |
| `@SkippedCount` | Staging EmpCodes not found in `MST_UserMaster` (or mode-0 existing matches) |
| `@NoChangeCount` | Existing matches with no differing in-scope values |
| `@MissingMasterCount` / `@ErrorCount` | Existing matches with unresolved in-scope FK code(s) |

Missing-master rows still receive updates for **other** resolvable in-scope fields; staging gets a `[WARNING]` appended to `ErrorMessage` (status not forced to `Error`).

### Standalone result set

Returned only when the procedure started its own transaction (suppressed when nested under orchestrator):

`BatchId`, `UpdatedCount`, `EmployeesUpdated`, `SkippedCount`, `NoChangeCount`, `MissingMasterCount`, `ErrorCount`, `ProcessingMode`, `ExecutedBy`, `Message`.

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
| T01 | Mode `0` | No UPDATEs; `@SkippedCount` = existing matches |
| T02 | Mode `1`, changed Email | `@UpdatedCount = 1`; Email updated |
| T03 | Mode `1`, identical values | `@NoChangeCount ≥ 1`; `@UpdatedCount = 0` |
| T04 | Mode `2`, only `EmployeeName` selected | Name can change; Email unchanged even if staging differs |
| T05 | Mode `2`, empty OverwriteFields | `@NoChangeCount` for existing; no UPDATEs |
| T06 | EmpCode not in `MST_UserMaster` | `@SkippedCount++`; no insert |
| T07 | Designation code → Id | `MST_UserMaster.Designation_ID` becomes `MST_Designation.Id` |
| T08 | In-scope Branch code unresolved | Branch left unchanged; `@MissingMasterCount++`; warning on staging |
| T09 | Match by EmpCode only | Different Email/Mobile/Name alone never used as match key |
| T10 | Never-update columns | Password / Created* / CreationMode / DPDP unchanged |
| T11 | Nested under orchestrator | No child client RS; OUTPUTs roll into sync summary |
| T12 | Idempotent re-run | Second run → `@UpdatedCount = 0`, `@NoChangeCount` rises |

---

## 9. Assumptions

1. Validated SSMS update intent: EmpCode match only; change-detection; mode/field gating; Id-based FKs; Designation chain preserved.
2. `MST_HRMSImportField.FieldCode` values align with the accepted tokens in §3 (aliases covered).
3. Prerequisite masters created by `usp_HSE_SynchronizeMasters` when possible.
4. No ASPX, C#, or orchestrator changes in this phase.

---

## 10. Deployment notes

| Item | Status |
|---|---|
| `usp_HSE_SynchronizeMasters` | Prerequisite |
| `usp_HSE_InsertNewEmployees` | Prerequisite for brand-new codes in mixed runs |
| `usp_HSE_UpdateChangedEmployees` | Delivered this phase |
| `usp_HSE_UpdateReportingHierarchy` | Still required next |
| `usp_RunHRMSSynchronization` | Already calls this child for modes `1`/`2` + EMPLOYEE module |
