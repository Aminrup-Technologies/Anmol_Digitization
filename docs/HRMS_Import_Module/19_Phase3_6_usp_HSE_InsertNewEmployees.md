# Phase 3.6 — `usp_HSE_InsertNewEmployees`

**Status:** SQL delivered for independent compile / unit test.  
**Scope:** Insert **new** employees only into `MST_UserMaster`. No updates to existing employees. No hierarchy-only pass (reporting columns are written on INSERT only).

**Script:** [`Database/Stored Procedures/HRMS/usp_HSE_InsertNewEmployees.sql`](../../Database/Stored%20Procedures/HRMS/usp_HSE_InsertNewEmployees.sql)

---

## 1. Parameters

| Parameter | Type | Direction | Required | Description |
|---|---|---|---|---|
| `@BatchId` | `INT` | IN | Yes | Staging / batch key |
| `@ExecutedBy` | `NVARCHAR(128)` | IN | No | Maps to `CreatedBy` (default `SYSTEM`) |
| `@InsertedCount` | `INT` | OUT | — | Rows inserted into `MST_UserMaster` (orchestrator) |
| `@SkippedCount` | `INT` | OUT | — | Staging EmpCodes already present in `MST_UserMaster` |
| `@ErrorCount` | `INT` | OUT | — | Orchestrator rollup (= `@MissingMasterCount`) |
| `@EmployeesInserted` | `INT` | OUT | — | Alias of `@InsertedCount` (explicit insert contract) |
| `@MissingMasterCount` | `INT` | OUT | — | New EmpCodes blocked because required Branch/Plant Id could not be resolved |

### Example call (standalone)

```sql
DECLARE
    @InsertedCount INT,
    @SkippedCount INT,
    @ErrorCount INT,
    @EmployeesInserted INT,
    @MissingMasterCount INT;

EXEC dbo.usp_HSE_InsertNewEmployees
    @BatchId            = 123,
    @ExecutedBy         = N'test.user',
    @InsertedCount      = @InsertedCount OUTPUT,
    @SkippedCount       = @SkippedCount OUTPUT,
    @ErrorCount         = @ErrorCount OUTPUT,
    @EmployeesInserted  = @EmployeesInserted OUTPUT,
    @MissingMasterCount = @MissingMasterCount OUTPUT;

SELECT
    @InsertedCount AS InsertedCount,
    @EmployeesInserted AS EmployeesInserted,
    @SkippedCount AS SkippedCount,
    @MissingMasterCount AS MissingMasterCount,
    @ErrorCount AS ErrorCount;
```

### Orchestrator call (Phase 3.4)

```sql
EXEC dbo.usp_HSE_InsertNewEmployees
    @BatchId        = @BatchId,
    @ExecutedBy     = @ExecutedBy,
    @InsertedCount  = @StepInserted OUTPUT,
    @SkippedCount   = @StepSkipped OUTPUT,
    @ErrorCount     = @StepErrors OUTPUT;
```

`@EmployeesInserted` / `@MissingMasterCount` are optional OUTPUTs; orchestrator does not need to bind them.

---

## 2. FK resolution

Staging carries **business codes**. `MST_UserMaster` FKs store the related master table’s **identity `Id`**.

| `MST_UserMaster` column | Match staging code on | Store |
|---|---|---|
| `BranchId` | `MST_Branch.Branch_ID` (= `TRY_CONVERT(INT, Staging.Branch_ID)`) | `MST_Branch.Id` |
| `DepartmentId` | `MST_Department.Department_ID` | `MST_Department.Id` |
| `Designation_ID` | `MST_Designation.Designation_ID` (business code) | `MST_Designation.Id` |
| `GradeId` | `MST_Grade.Grade_ID` | `MST_Grade.Id` |
| `PlantId` | `CAST(MST_Plant.PlantID_ID AS NVARCHAR(50))` | `MST_Plant.Id` |
| `RegionId` | `MST_Region.Region_ID` | `MST_Region.Id` |
| `DivisionId` | `CAST(MST_Division.Division_ID AS NVARCHAR(50))` | `MST_Division.Id` |
| `CategoryId` | `CAST(MST_Category.Category_ID AS NVARCHAR(50))` | `MST_Category.Id` |
| `SubDepartmentId` | `CAST(MST_SubDepartment.SubDepartment_ID AS NVARCHAR(50))` | `MST_SubDepartment.Id` |

### Designation chain (preserved)

```text
Staging_EmployeeData.Designation_ID   (HRMS business code)
        ↓ match
MST_Designation.Designation_ID
        ↓ take PK
MST_Designation.Id
        ↓ store
MST_UserMaster.Designation_ID         (int)
```

Optional org FKs may be `NULL` when the staging code is blank or unresolved.  
**Required** for insert: `BranchId` and `PlantId` (`NOT NULL` on `MST_UserMaster`).

`CompanyId` is not populated by this procedure (left `NULL`).

---

## 3. Insert rules

1. **Source:** `Staging_EmployeeData WHERE BatchId = @BatchId`.
2. **Candidates:** non-blank `EmpCode` + `EmpName`, and `ImportStatus <> 'Error'` (validation failures are left alone).
3. **Deduplicate:** one staging row per `EmpCode` (`MIN`/`ROW_NUMBER` by `StagingId`).
4. **Skip existing:** if `MST_UserMaster.EmployeeCode` already exists → count toward `@SkippedCount`; **no UPDATE**.
5. **Insert only** when EmpCode is new and required Branch/Plant Ids resolved.
6. **Populate** from staging / defaults:

| Target column | Source / default |
|---|---|
| `EmployeeCode` | `EmpCode` |
| `EmployeeName` | `EmpName` |
| `Email` | `Email` |
| `Mobile` | `LEFT(MobileNo, 10)` |
| `Gender` | `Gender` |
| `DOJ` | Parsed from staging `DOJ` (`106` / `23` / `103` / general `TRY_CONVERT`) |
| `EmployeeStatus` | `EmployeeStatus`, else `EmpStatus`, else `'Active'` |
| `ReportingManager` / `ReportingManagerCode` | staging |
| `ReportingManagersManagerName` / `ReportingManagersManagerCode` | staging |
| `CreatedDate` | IST = `DATEADD(MINUTE, 330, SYSUTCDATETIME())` (matches table default pattern) |
| `CreatedBy` | `@ExecutedBy` or `'SYSTEM'` (`varchar(50)`) |
| `CreationMode` | `'Bulk'` (application bulk-create default vs `'Single'` for manual New User) |

7. Does **not** set `Password` / hash fields (remain `NULL` for HRMS-imported users until password setup).
8. Nested-transaction safe (same pattern as SynchronizeMasters).

---

## 4. Missing master handling

| Condition | Behaviour |
|---|---|
| Branch and/or Plant Id unresolved for a **new** EmpCode | Row **not** inserted |
| Staging row | `ImportStatus = 'Error'`; `ErrorMessage` appended with resolution detail |
| Counters | `@MissingMasterCount++`; `@ErrorCount = @MissingMasterCount` |
| Optional FK unresolved (Dept, Grade, Designation, …) | Insert proceeds with that FK `NULL` (not counted as missing-master error) |

Prerequisite: run `usp_HSE_SynchronizeMasters` (and UI `usp_HSE_ValidateBatch`) before this step so creatable masters exist.

---

## 5. Output contract

### OUTPUT parameters

| Output | Meaning |
|---|---|
| `@InsertedCount` / `@EmployeesInserted` | Successful `MST_UserMaster` inserts |
| `@SkippedCount` | EmpCodes already in `MST_UserMaster` |
| `@MissingMasterCount` / `@ErrorCount` | New EmpCodes blocked by missing Branch/Plant Id |

### Standalone result set

Returned only when the procedure started its own transaction (not when nested under the orchestrator):

| Column | Meaning |
|---|---|
| `BatchId` | Echo |
| `InsertedCount` | Same as OUTPUT |
| `EmployeesInserted` | Same as OUTPUT |
| `SkippedCount` | Same as OUTPUT |
| `MissingMasterCount` | Same as OUTPUT |
| `ErrorCount` | Same as OUTPUT |
| `CreatedBy` | Effective creator |
| `CreationMode` | `'Bulk'` |
| `Message` | Completion text |

### Not mutated by this procedure

| Object | Behaviour |
|---|---|
| Existing `MST_UserMaster` rows | Untouched |
| `ImportBatchMaster.Status` | Untouched (orchestrator) |
| Hierarchy-only refresh | Not performed (separate Phase procedure) |

---

## 6. Transaction scope

| Scenario | Behaviour |
|---|---|
| Standalone | Owns local transaction; commit on success; rollback on error |
| Nested under orchestrator | Joins ambient transaction; no local commit/rollback of outer work |
| Hard error (nested) | Re-raises for orchestrator `CATCH` / full rollback |

---

## 7. Test scenarios

| ID | Scenario | Expected |
|---|---|---|
| T01 | Bad / missing BatchId | `RAISERROR`; OUTPUTs 0 |
| T02 | Empty staging | `RAISERROR` |
| T03 | New EmpCode, all required masters resolved | `@InsertedCount = 1`; row in `MST_UserMaster` with FK **Ids** |
| T04 | EmpCode already exists | `@SkippedCount = 1`; no UPDATE of existing row |
| T05 | New EmpCode, Branch unresolved | `@MissingMasterCount = 1`; staging `Error`; no insert |
| T06 | New EmpCode, Plant unresolved | Same as T05 for Plant |
| T07 | Designation business code → Id | `MST_UserMaster.Designation_ID = MST_Designation.Id` for matched code |
| T08 | Duplicate EmpCode in staging | One insert; second staging duplicate not double-inserted |
| T09 | Validation `Error` staging row | Ignored by insert candidates |
| T10 | Nested under orchestrator | No client RS from child; OUTPUTs roll into sync summary |
| T11 | Idempotent re-run | First run inserts; second run all skipped |
| T12 | No hierarchy-only side effects beyond INSERT columns | No separate hierarchy UPDATE pass |
| T13 | `CreationMode` / `CreatedBy` | `'Bulk'` / `@ExecutedBy` (or `SYSTEM`) |

### Suggested checks

```sql
SELECT u.EmployeeCode, u.BranchId, b.Id AS BranchPk, b.Branch_ID,
       u.Designation_ID, d.Id AS DesigPk, d.Designation_ID AS DesigCode
FROM dbo.MST_UserMaster u
LEFT JOIN dbo.MST_Branch b ON b.Id = u.BranchId
LEFT JOIN dbo.MST_Designation d ON d.Id = u.Designation_ID
WHERE u.EmployeeCode IN (
    SELECT LTRIM(RTRIM(EmpCode)) FROM dbo.Staging_EmployeeData WHERE BatchId = @BatchId
);
```

---

## 8. Assumptions

1. Validated insert intent preserved: **new EmpCodes only**, FK store **master `Id`**, source = staging by `BatchId`.
2. `usp_HSE_SynchronizeMasters` has already created missing org masters when descriptions were present.
3. `BranchId` / `PlantId` remain the only non-nullable org FKs on `MST_UserMaster`.
4. Application defaults for bulk create: `CreationMode = 'Bulk'`, `CreatedDate` IST offset, `CreatedBy` from executor.
5. No ASPX/C# changes in this phase.

---

## 9. Deployment notes

| Item | Status |
|---|---|
| `usp_HSE_SynchronizeMasters` | Prerequisite (Phase 3.5) |
| `usp_HSE_InsertNewEmployees` | Delivered this phase |
| `usp_HSE_UpdateChangedEmployees` | Still required next |
| `usp_HSE_UpdateReportingHierarchy` | Still required next |
| `usp_RunHRMSSynchronization` | Already calls this child when `EMPLOYEE` module selected |
