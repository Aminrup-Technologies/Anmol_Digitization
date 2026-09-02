# HRMS Phase A + B1 Live deployment package

**Status: ANALYSIS + SCRIPT GENERATION ONLY. Do not apply these scripts to Live from this PR without an explicit deployment approval.**

| Item | Value |
|---|---|
| Repository baseline | `Aug26_AILWorks_AmimnrupSupport` |
| Merge commit | `5ff3a3f2a3588a574e23d9920cbfce5a89b798fa` |
| Source PR | [#203](https://github.com/Aminrup-Technologies/Anmol_Digitization/pull/203) |
| UAT / Dev database | `AnmolDristiDev` on `115.112.186.71` (`MUMANMOAPP01\SQLEXPRESS`) |
| Live database | `AnmolDristi` on the same instance (read-only inspection only) |
| Extraction date | 2026-09-02 |
| Phase B2 | Not started. No historical `MST_UserMaster` remapping. |
| Plant ERP `3277` | Not fixed (out of scope). |

Application code from PR #203 is **not** in this package. SQL and the Phase A Batch Manager UI must be released together: Live today still has a stub `usp_RunHRMSSynchronization` that delegates to `usp_ProcessEmployeeBatch`. The new orchestrator **requires** `@Action` in (`MASTER`,`INSERT`,`UPDATE`,`HIERARCHY`) and will `THROW 50003` if the old combined-run UI calls it without `@Action`.

---

## 1. Object inventory

| Object | Type | Repository (`5ff3a3f`) | UAT `AnmolDristiDev` | Live `AnmolDristi` |
|---|---|---|---|---|
| `dbo.ImportBatchMaster.MasterSyncCompleted` | Column (bit NOT NULL, DF=0) | Script `011_...sql` | Present (`modify` table 2026-09-01 17:22:16) | Present (`modify` table 2026-09-02 07:59:53) |
| `dbo.ImportBatchMaster.MasterSyncDate` | Column (datetime NULL) | Script `011_...sql` | Present | Present |
| `dbo.Staging_EmployeeData.SubDepartment_Description` | Column (varchar(255) NULL) | Script `006_...sql` (pre-#203) | Present | **Missing** |
| `dbo.usp_HSE_ValidateBatch` | Procedure | File present (Phase A) | Present 2026-09-02 06:38:38 | **Missing** |
| `dbo.usp_RunHRMSSynchronization` | Procedure (orchestrator) | File present (Phase A) | Present 2026-09-02 06:39:03 | **Stub** 2026-07-06 16:17:29 (552 bytes; `EXEC usp_ProcessEmployeeBatch`) |
| `dbo.usp_HSE_InsertNewEmployees` | Procedure | File present (Phase B1) | Present 2026-09-02 06:39:30 | **Missing** |
| `dbo.usp_HSE_UpdateChangedEmployees` | Procedure | File present (Phase B1) | Present 2026-09-02 06:39:59 | **Missing** |
| `dbo.usp_HSE_SynchronizeMasters` | Procedure (dependency) | File present (unchanged by #203) | Present 2026-08-04 07:58:40 | **Missing** |
| `dbo.usp_HSE_ReconcileValidationWarnings` | Procedure (dependency) | File present (unchanged by #203) | Present 2026-08-04 12:02:57 | **Missing** |
| `dbo.usp_HSE_UpdateReportingHierarchy` | Procedure (dependency) | File present (unchanged by #203) | Present 2026-08-03 20:57:17 | **Missing** |
| `dbo.usp_ProcessEmployeeBatch` | Procedure (legacy) | Not in `Database/Stored Procedures/HRMS` | Present (hash `889329D3…`) | Present (same hash). **Preserve.** |
| `dbo.MST_UserMaster` org columns | Table columns | Mapping contract only | Types match Live | Types match UAT (`BranchId`/`PlantId` int NOT NULL; others int NULL; `Designation_ID` int NULL) |
| `MST_Branch` / `Department` / `Grade` / `Plant` / `Region` / `Division` / `Category` / `SubDepartment` / `Designation` / `Company` | Master tables | Referenced | Present, ERP key columns present | Present, same key column types |

PR #203 SQL files (exactly five):

- `Database/Scripts/011_HRMS_ImportBatchMaster_MasterSyncCompleted.sql`
- `Database/Stored Procedures/HRMS/usp_HSE_ValidateBatch.sql`
- `Database/Stored Procedures/HRMS/usp_RunHRMSSynchronization.sql`
- `Database/Stored Procedures/HRMS/usp_HSE_InsertNewEmployees.sql`
- `Database/Stored Procedures/HRMS/usp_HSE_UpdateChangedEmployees.sql`

Orchestrator `EXEC` dependencies (verified in repository + UAT `sys.sql_expression_dependencies` / source):

`MASTER` → `usp_HSE_SynchronizeMasters` + `usp_HSE_ReconcileValidationWarnings`  
`INSERT` → `usp_HSE_InsertNewEmployees`  
`UPDATE` → `usp_HSE_UpdateChangedEmployees`  
`HIERARCHY` → `usp_HSE_UpdateReportingHierarchy`

SQL Server deferred name resolution would let the orchestrator *create* without children present; runtime would then fail. Children are therefore included.

---

## 2. Three-way difference matrix

| Object | Repository vs UAT | UAT vs Live | Classification |
|---|---|---|---|
| `ImportBatchMaster.MasterSyncCompleted` | MATCH (types/default/constraint name) | MATCH | **MATCH** |
| `ImportBatchMaster.MasterSyncDate` | MATCH | MATCH | **MATCH** |
| `Staging_EmployeeData.SubDepartment_Description` | MATCH (UAT has the column from script 006) | UAT has it; Live does not | **LIVE BEHIND** |
| `usp_HSE_ValidateBatch` | MATCH after catalog rewrite `CREATE OR ALTER` → `CREATE   PROCEDURE`. XML `.nodes()` uses `@ModuleXml` / `@ScopeXml` variables on both. `@Action` / `@OverwriteFields` present. | Live missing | **LIVE BEHIND** |
| `usp_RunHRMSSynchronization` | MATCH (canonical body) | Live is pre-HSE stub | **LIVE BEHIND** |
| `usp_HSE_InsertNewEmployees` | MATCH (canonical body); ERP writes + `des.Id` | Live missing | **LIVE BEHIND** |
| `usp_HSE_UpdateChangedEmployees` | MATCH (canonical body); ERP writes + selected-field `@OverwriteFields` | Live missing | **LIVE BEHIND** |
| `usp_HSE_SynchronizeMasters` | MATCH (canonical body) | Live missing | **LIVE BEHIND** (dependency) |
| `usp_HSE_ReconcileValidationWarnings` | MATCH (canonical body) | Live missing | **LIVE BEHIND** (dependency) |
| `usp_HSE_UpdateReportingHierarchy` | MATCH (canonical body) | Live missing | **LIVE BEHIND** (dependency) |
| `usp_ProcessEmployeeBatch` | Not a #203 object | Same HASHBYTES on Dev and Live | **LIVE DRIFT** — preserve |
| `MST_Plant` row `PlantID_ID='3277'` | Data, not a definition | Dev: two rows (Id 40 Greater Noida, Id 41 Nirmali). Live: one row (Id 28 Noida) | **LIVE DRIFT** — do not change (out of scope) |

UAT `OBJECT_DEFINITION` HASHBYTES SHA2_256 values are in `EVIDENCE_HASHES.md`.

---

## 3. Required Live changes

### `dbo.Staging_EmployeeData.SubDepartment_Description`

- **Current Live state:** column missing (table `modify_date` 2026-05-16).
- **Expected state:** `VARCHAR(255) NULL` (script 006).
- **Reason:** `usp_HSE_SynchronizeMasters` and `usp_HSE_ValidateBatch` read this column. Without it those procedures will not create/run.
- **Risk:** Low. Additive nullable column. Existing staging rows stay valid (NULL description).
- **Deployment script:** `003_Staging_EmployeeData_SubDepartment_Description.sql`

### `dbo.usp_HSE_ValidateBatch`

- **Current Live state:** object missing.
- **Expected state:** UAT/repository Phase A action-aware validator (`@Action`, `@OverwriteFields`, XML variable `.nodes()`).
- **Reason:** UI validation gate for the approved workflow.
- **Risk:** Medium. New object. Requires staging column from script 003.
- **Deployment script:** `004_usp_HSE_ValidateBatch.sql`

### `dbo.usp_HSE_SynchronizeMasters`

- **Current Live state:** missing.
- **Expected state:** UAT/repository master sync (unchanged by #203).
- **Reason:** Orchestrator `MASTER` action. Without it Phase A cannot set `MasterSyncCompleted`.
- **Risk:** Medium. Inserts/updates master *description* rows from staging (operational, not UserMaster).
- **Deployment script:** `005_usp_HSE_SynchronizeMasters.sql`

### `dbo.usp_HSE_ReconcileValidationWarnings`

- **Current Live state:** missing.
- **Expected state:** UAT/repository definition.
- **Reason:** Called after masters by the orchestrator.
- **Risk:** Low–medium. Updates staging validation flags only.
- **Deployment script:** `006_usp_HSE_ReconcileValidationWarnings.sql`

### `dbo.usp_HSE_InsertNewEmployees`

- **Current Live state:** missing.
- **Expected state:** Phase B1 ERP ID writes; `Designation_ID` ← `MST_Designation.Id`.
- **Reason:** Isolated INSERT action.
- **Risk:** High if executed against production employees (this package does not execute it). Object deploy itself is CREATE OR ALTER only.
- **Deployment script:** `007_usp_HSE_InsertNewEmployees.sql`

### `dbo.usp_HSE_UpdateChangedEmployees`

- **Current Live state:** missing.
- **Expected state:** Phase B1 ERP ID writes + selected-field enforcement.
- **Reason:** Isolated UPDATE action.
- **Risk:** High if executed (not executed here). Duplicate `MST_Plant.PlantID_ID='3277'` is **not** present on Live today (single row); Dev still has two rows.
- **Deployment script:** `008_usp_HSE_UpdateChangedEmployees.sql`

### `dbo.usp_HSE_UpdateReportingHierarchy`

- **Current Live state:** missing.
- **Expected state:** UAT/repository hierarchy updater.
- **Reason:** Orchestrator `HIERARCHY` action.
- **Risk:** Medium if executed later (writes reporting columns only).
- **Deployment script:** `009_usp_HSE_UpdateReportingHierarchy.sql`

### `dbo.usp_RunHRMSSynchronization`

- **Current Live state:** 552-byte stub: `EXEC dbo.usp_ProcessEmployeeBatch @BatchId, @AllowOverwrite`. Parameters: `@Modules VARCHAR(MAX)`, no `@Action`, no `@ExecutedBy`.
- **Expected state:** Phase A orchestrator with `@Action`, `MasterSyncCompleted` gate, `THROW 50005` / `50007`.
- **Reason:** Replaces combined-run stub. Required for Batch Manager.
- **Risk:** **High if SQL is deployed without the PR #203 UI.** Old callers without `@Action` will fail. `usp_ProcessEmployeeBatch` is **not** dropped.
- **Deployment script:** `010_usp_RunHRMSSynchronization.sql`

### `dbo.ImportBatchMaster` sync columns

- **Current Live state:** already present (bit NOT NULL default 0; datetime NULL; constraint `DF_ImportBatchMaster_MasterSyncCompleted`). All 9 Live batches have `MasterSyncCompleted=0` and `MasterSyncDate` NULL.
- **Expected state:** same.
- **Reason:** Included for idempotent repeatability; expected no-op on current Live.
- **Risk:** None if columns already match.
- **Deployment script:** `002_ImportBatchMaster_MasterSyncColumns.sql`

---

## 4. UAT drift

**No functional UAT drift** was found for the in-scope procedures.

| Observation | Classification |
|---|---|
| SQL Server catalogs `CREATE OR ALTER PROCEDURE` as `CREATE   PROCEDURE` (extra spaces where `OR ALTER` was). HASHBYTES of raw `OBJECT_DEFINITION` therefore differs from the `.sql` file bytes. Canonical body (header-unified) **matches** repository exactly for all seven procedures. | Catalog storage, not a hotfix |
| `usp_HSE_ValidateBatch` UAT body uses `DECLARE @ModuleXml XML` / `DECLARE @ScopeXml XML` then `.nodes('/r/i')`. Inline `CONVERT(XML,...).nodes()` is **absent**. This is the compilation fix from commit `22405be`, present in both repository and UAT. | Intentional UAT-validated fix, already in repo |
| Procedure `modify_date` on UAT for Validate/Orch/Insert/Update is 2026-09-02 06:38–06:39 (UAT deploy of the approved definitions). SyncMasters / Reconcile / Hierarchy dates are earlier (2026-08-03/04) and still hash-match repository. | Expected |

No test-only dummy procedure bodies were found. Synthetic employee *rows* exist on UAT (`PHAB*` / `PHB1*` from prior testing); they are **data**, not definitions, and are not copied into this package.

---

## 5. Live drift

| Difference | Classification |
|---|---|
| `usp_RunHRMSSynchronization` stub calling `usp_ProcessEmployeeBatch` | **Must reconcile** — replace with approved orchestrator (script 010) |
| Six HSE procedures missing | **Must reconcile** — create from repository/UAT |
| `Staging_EmployeeData.SubDepartment_Description` missing | **Must reconcile** — additive column |
| `usp_ProcessEmployeeBatch` present (same hash as Dev) | **Safe to preserve** — leave in place; new orchestrator does not call it |
| `ImportBatchMaster` sync columns already added (constraint name matches script 011) | **Safe to preserve** — already at expected schema; script 002 no-op |
| 9 Live batches, all `MasterSyncCompleted=0` | **Safe to preserve** — Phase A requires MASTER before employee actions |
| `MST_Plant` `PlantID_ID='3277'`: Live 1 row vs Dev 2 rows | **Requires manual decision / out of scope** — do not fix in this package |
| `MST_HRMSImportField` / `MST_HRMSMasterConfiguration` exist on both (default names differ) | **Safe to preserve** — not in #203 SQL scope |

---

## 6. Deployment order

Verified dependency order (schema → validation → children → orchestrator last):

1. `001_PreDeployment_Check.sql` (read-only; fail-fast)
2. `002_ImportBatchMaster_MasterSyncColumns.sql`
3. `003_Staging_EmployeeData_SubDepartment_Description.sql`
4. `004_usp_HSE_ValidateBatch.sql`
5. `005_usp_HSE_SynchronizeMasters.sql`
6. `006_usp_HSE_ReconcileValidationWarnings.sql`
7. `007_usp_HSE_InsertNewEmployees.sql`
8. `008_usp_HSE_UpdateChangedEmployees.sql`
9. `009_usp_HSE_UpdateReportingHierarchy.sql`
10. `010_usp_RunHRMSSynchronization.sql`
11. `011_PostDeployment_Verification.sql` (read-only)

Deviation from the originally suggested 003–007 numbering: Live is missing the entire HSE child surface, so dependency procedures and the staging column were inserted, and the orchestrator was moved **after** children.

Each mutating script aborts unless `DB_NAME() = N'AnmolDristi'` (override: `@AllowOtherDatabase = 1`).

---

## 7. Generated files

| File | Role |
|---|---|
| `001_PreDeployment_Check.sql` | Read-only fail-fast |
| `002_ImportBatchMaster_MasterSyncColumns.sql` | Idempotent schema (expected no-op on current Live) |
| `003_Staging_EmployeeData_SubDepartment_Description.sql` | Idempotent schema (Live behind) |
| `004_usp_HSE_ValidateBatch.sql` | CREATE OR ALTER from repository |
| `005_usp_HSE_SynchronizeMasters.sql` | CREATE OR ALTER from repository (dependency) |
| `006_usp_HSE_ReconcileValidationWarnings.sql` | CREATE OR ALTER from repository (dependency) |
| `007_usp_HSE_InsertNewEmployees.sql` | CREATE OR ALTER from repository |
| `008_usp_HSE_UpdateChangedEmployees.sql` | CREATE OR ALTER from repository |
| `009_usp_HSE_UpdateReportingHierarchy.sql` | CREATE OR ALTER from repository (dependency) |
| `010_usp_RunHRMSSynchronization.sql` | CREATE OR ALTER from repository (orchestrator last) |
| `011_PostDeployment_Verification.sql` | Read-only token + HASHBYTES checks |
| `EVIDENCE_HASHES.md` | SHA2_256 / HASHBYTES evidence |
| `README.md` | This report |

Procedure scripts wrap the **exact** repository file body with `SET ANSI_NULLS ON` / `SET QUOTED_IDENTIFIER ON` (both are ON on UAT) plus the database-name guard. No formatting rewrite of procedure logic.

---

## 8. Pre-deployment checks (fail-fast)

`001_PreDeployment_Check.sql` throws:

| Code | Condition |
|---|---|
| `56101` | Connected database is not `AnmolDristi` and `@AllowOtherDatabase = 0` |
| `56102` | Any **FAIL** row: missing `ImportBatchMaster` / `Staging_EmployeeData` / `MST_UserMaster` / required `MST_*` tables or ERP key columns; wrong `MasterSync*` types if the column exists |

Missing HSE procedures are **INFO-WILL-CREATE**, not FAIL (expected on Live).  
Live stub orchestrator is **INFO**, not FAIL.

---

## 9. Post-deployment verification (expected PASS)

`011_PostDeployment_Verification.sql` must PASS all of:

- `MasterSyncCompleted` exists, `bit` NOT NULL
- `MasterSyncDate` exists, `datetime` NULL
- `SubDepartment_Description` exists on staging
- All seven procedures exist
- ValidateBatch: `@Action`, `@OverwriteFields`, `DECLARE @ModuleXml XML` + `.nodes()`
- Orchestrator: `MasterSyncCompleted`, `THROW 50005`, and **not** `usp_ProcessEmployeeBatch`
- Insert: `BranchId = br.Branch_ID`, `DesignationId = des.Id`
- Update: `NewBranchId` ← `br.Branch_ID`, `NewDesignationId` ← `des.Id`, `@OverwriteFields`
- Insert+Update contain `Branch_ID`, `Department_ID`, `Grade_ID`, `PlantID_ID`, `Region_ID`, `Division_ID`, `Category_ID`, `SubDepartment_ID`
- Insert does **not** assign `DesignationId = des.Designation_ID`
- `HASHBYTES('SHA2_256', OBJECT_DEFINITION)` equals the UAT hashes in `EVIDENCE_HASHES.md`

The script does **not** run employee synchronization and does not update `MST_UserMaster`.

Dry-run: `001` and `011` were executed read-only against **AnmolDristiDev** with `@AllowOtherDatabase = 1`. Script `001` produced 0 FAIL rows. Script `011` produced **41/41 PASS**. Neither script was executed against Live.

---

## 10. Risks

| Area | Assessment |
|---|---|
| Live schema compatibility | Core tables and UserMaster org column types already match UAT. Only additive staging column required. Sync columns already on Live. |
| Existing batches | Live has 9 batches (8 Processed, 1 Uploaded), all `MasterSyncCompleted=0`. After deploy, employee actions will `THROW 50005` until MASTER is run per batch. That is the approved Phase A gate, not data loss. |
| Existing employee data | This package does not update `MST_UserMaster` or convert historical IDs (Phase B2). Future INSERT/UPDATE will write ERP IDs; existing Live rows remain as they are until an action is executed. |
| Stored procedure dependencies | Live is missing the whole HSE child set. Package includes them. Legacy `usp_ProcessEmployeeBatch` is preserved but will no longer be called by the orchestrator. |
| ERP ID mapping compatibility | UserMaster org columns are already `int`; procedures `TRY_CONVERT` ERP keys. Designation remains internal `Id`. Live `PlantID_ID='3277'` currently has one row (unlike Dev). |
| App/SQL coupling | **Must ship PR #203 UI with these procedures.** Old Live UI + new orchestrator will fail closed (`THROW 50003`). New UI + old stub will not expose MASTER/INSERT/UPDATE/HIERARCHY correctly. |
| Transaction/failure behavior | Schema scripts use `XACT_ABORT`. Procedures keep repository `SET XACT_ABORT ON`. A failed child leaves the orchestrator error path (repository behavior). |
| Rollback feasibility | Columns are additive (cannot easily drop if a default constraint is bound; 002 is no-op today). Procedures can be rolled back only if a prior definition is kept; Live has **no** prior HSE child definitions to restore. Orchestrator rollback would mean restoring the stub (script not generated; capture current stub before apply). |
| Plant 3277 | Not fixed. Live currently has a single row; Dev has duplicates. Re-introducing a duplicate later can inflate UPDATE candidate counts / INSERT PK failures. |

---

## 11. Final verdict

**READY FOR REVIEW**
