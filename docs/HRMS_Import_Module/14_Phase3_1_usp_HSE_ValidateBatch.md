# Phase 3.1 — `usp_HSE_ValidateBatch`

**Status:** SQL delivered for independent compile / unit test.  
**Scope:** Validation only. No sync, no master writes, no employee writes, no hierarchy updates, no `ImportBatchMaster.Status` updates.

**Script:** [`Database/Stored Procedures/HRMS/usp_HSE_ValidateBatch.sql`](../../Database/Stored%20Procedures/HRMS/usp_HSE_ValidateBatch.sql)

---

## 1. Complete SQL

See the script file above (`CREATE OR ALTER PROCEDURE [dbo].[usp_HSE_ValidateBatch]`).

---

## 2. Required parameters

| Parameter | Type | Direction | Required | Description |
|---|---|---|---|---|
| `@BatchId` | `INT` | IN | Yes | Staging / batch key |
| `@ProcessingMode` | `INT` | IN | Yes* | Must be `0`, `1`, or `2` |
| `@Modules` | `NVARCHAR(200)` | IN | Yes* | Pipe-delimited: `MASTER`, `EMPLOYEE`, `HIERARCHY` |
| `@IsValid` | `BIT` | OUT | — | `1` = no blocking row errors; `0` = batch-level or row-level blocking failure |

\*Required for a successful validation run. Missing/invalid values fail at batch level without mutating staging rows.

### Example call

```sql
DECLARE @IsValid BIT;

EXEC dbo.usp_HSE_ValidateBatch
    @BatchId        = 123,
    @ProcessingMode = 2,
    @Modules        = N'MASTER|EMPLOYEE|HIERARCHY',
    @IsValid        = @IsValid OUTPUT;

SELECT @IsValid AS IsValid;
```

---

## 3. Result set returned

Single summary result set:

| Column | Type (logical) | Meaning |
|---|---|---|
| `BatchId` | int | Echo of input |
| `TotalRecords` | int | Staging rows for batch (`0` if batch missing / empty) |
| `ValidCount` | int | Rows with `ImportStatus <> 'Error'` |
| `ErrorCount` | int | Rows with `ImportStatus = 'Error'` |
| `WarningCount` | int | Rows whose `ErrorMessage` contains `[WARNING]` |
| `DuplicateEmpCodeCount` | int | Count of EmpCode values that appear more than once |
| `MissingMandatoryCount` | int | Rows missing EmpCode and/or EmpName |
| `InvalidMasterRefCount` | int | Rows with blocking “Invalid master reference” text |
| `IsValid` | bit | Same as `@IsValid` |
| `ValidationMessage` | nvarchar | Human-readable batch outcome |

### Row-level outputs (on `Staging_EmployeeData`)

| Column | Values written by this proc |
|---|---|
| `ImportStatus` | Reset to `Pending`, then `Error` when blocking issues found |
| `ErrorMessage` | Cleared on reset; appended with mandatory / duplicate / master messages |

Does **not** update `ImportBatchMaster`.

---

## 4. Assumptions

1. `dbo.Staging_EmployeeData` has at least: `StagingId`, `BatchId`, `EmpCode`, `EmpName`, org master ID/Description columns used by `HrmsDataImport`, `ImportStatus`, `ErrorMessage`.  
2. `dbo.ImportBatchMaster` has `BatchId`.  
3. Master tables exist in the target database as used by the app:  
   `MST_Branch`, `MST_Department`, `MST_Designation`, `MST_Grade`, `MST_Plant`, `MST_Region`, `MST_Division`, `MST_Category`, `MST_Company`, `MST_SubDepartment`.  
4. **Designation** is validated against `MST_Designation.Designation_ID` (business code), not `Id`, preserving the approved mapping chain.  
5. Blank master ID columns are ignored (optional fields).  
6. Unresolved master code **with** Description → `[WARNING]` only (SynchronizeMasters can create).  
7. Unresolved master code **without** Description → blocking `Error`.  
8. `SubDepartment_ID` has no Description in the upload column list → unresolved code is always blocking `Error`.  
9. Reporting-manager fields are **not** validated here (hierarchy is Phase 3.5).  
10. Validated SSMS PoC script was not in the repository; master checklist is derived from staging columns + live app master usage. Adjust in a later patch if the PoC used a narrower master set.  
11. `CREATE OR ALTER` requires SQL Server 2016+ (environment is SQL Server 2017).  
12. `@IsValid = 1` allows warnings; only `ImportStatus = 'Error'` blocks.

---

## 5. Test cases

| # | Scenario | Setup | Expected |
|---|---|---|---|
| T01 | Missing batch | `@BatchId` not in `ImportBatchMaster` | `@IsValid=0`; summary message; staging untouched |
| T02 | Empty staging | Batch exists, 0 staging rows | `@IsValid=0`; `TotalRecords=0` |
| T03 | Invalid ProcessingMode | `@ProcessingMode=9` | `@IsValid=0`; staging untouched |
| T04 | Invalid Modules token | `@Modules='FOO'` | `@IsValid=0`; staging untouched |
| T05 | Empty Modules | `@Modules=NULL` or `''` | `@IsValid=0`; staging untouched |
| T06 | Happy path | Valid mode/modules; EmpCode+EmpName; known or blank masters | `@IsValid=1`; rows `Pending`; empty `ErrorMessage` |
| T07 | Missing EmpCode | EmpName present, EmpCode blank | Row `Error`; `MissingMandatoryCount>=1`; `@IsValid=0` |
| T08 | Missing EmpName | EmpCode present, EmpName blank | Row `Error`; `@IsValid=0` |
| T09 | Duplicate EmpCode | Two rows same EmpCode (case/trim insensitive) | Both `Error`; `DuplicateEmpCodeCount>=1` |
| T10 | Unknown Designation with description | New `Designation_ID` + description | `[WARNING]` in `ErrorMessage`; status not forced to Error from this alone; `@IsValid=1` if no other errors |
| T11 | Unknown Designation without description | New `Designation_ID`, blank description | Row `Error`; `InvalidMasterRefCount>=1` |
| T12 | Known Designation code | `Designation_ID` exists in `MST_Designation.Designation_ID` | No designation error |
| T13 | Unknown Branch without description | Bad `Branch_ID`, blank `Branch_Description` | Row `Error` |
| T14 | Unknown Branch with description | Bad `Branch_ID` + description | Warning only for that issue |
| T15 | Unknown SubDepartment | Bad `SubDepartment_ID` | Row `Error` (no description column) |
| T16 | Modules case/spaces | `master \| employee` | Accepted after normalize |
| T17 | No PoC table | Search proc text | Must not contain `KKM_SO_TADA_Export_14Nov25$` |
| T18 | No master DML | Trace execution | No INSERT/UPDATE/DELETE on MST_* except reads; no `ImportBatchMaster` UPDATE |

### Minimal unit-test harness (manual)

```sql
-- After deploying the procedure and seeding a test batch:
DECLARE @IsValid BIT;

EXEC dbo.usp_HSE_ValidateBatch
    @BatchId = <TestBatchId>,
    @ProcessingMode = 0,
    @Modules = N'MASTER|EMPLOYEE|HIERARCHY',
    @IsValid = @IsValid OUTPUT;

SELECT @IsValid AS IsValid;

SELECT StagingId, EmpCode, EmpName, ImportStatus, ErrorMessage
FROM dbo.Staging_EmployeeData
WHERE BatchId = <TestBatchId>
ORDER BY StagingId;
```

---

## 6. Out of scope (explicit)

- `usp_HSE_SynchronizeMasters` and later Phase 3.x procedures  
- UI / `HrmsDataImport` / `HrmsBatchManager`  
- Employee insert/update/hierarchy  
- `ImportBatchMaster.Status` updates  
