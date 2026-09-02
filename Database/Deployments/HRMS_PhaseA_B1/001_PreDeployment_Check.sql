/*
    Package:     Database/Deployments/HRMS_PhaseA_B1
    Script:      001_PreDeployment_Check.sql
    Baseline:    Aug26_AILWorks_AmimnrupSupport @ 5ff3a3f (PR #203)
    Mode:        READ-ONLY
    Purpose:     Fail-fast confirmation of target database and current Live state
                 before any schema or procedure script is applied.

    Does not:
      - ALTER / CREATE / DROP
      - UPDATE / INSERT / DELETE
      - Execute employee synchronization
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ExpectedDatabase sysname = N'AnmolDristi';
DECLARE @AllowOtherDatabase bit = 0; /* set 1 only for an intentional non-Live dry run */

PRINT N'============================================================';
PRINT N'HRMS Phase A + B1  — PRE-DEPLOYMENT CHECK (read-only)';
PRINT N'============================================================';
PRINT N'TARGET DATABASE (expected): ' + QUOTENAME(@ExpectedDatabase);
PRINT N'TARGET DATABASE (connected): ' + QUOTENAME(DB_NAME());
PRINT N'SERVER: ' + CONVERT(nvarchar(128), @@SERVERNAME);
PRINT N'CHECKED AT: ' + CONVERT(nvarchar(30), SYSDATETIME(), 126);

IF DB_NAME() <> @ExpectedDatabase AND @AllowOtherDatabase = 0
BEGIN
    DECLARE @dbMsg nvarchar(4000) =
        N'Abort: connected to ' + QUOTENAME(DB_NAME())
        + N'. Expected ' + QUOTENAME(@ExpectedDatabase)
        + N'. Set @AllowOtherDatabase = 1 to override.';
    THROW 56101, @dbMsg, 1;
END;

IF @AllowOtherDatabase = 1 AND DB_NAME() <> @ExpectedDatabase
    PRINT N'WARNING: @AllowOtherDatabase=1 — continuing against ' + QUOTENAME(DB_NAME());

PRINT N'';
PRINT N'EXPECTED DEPLOYMENT:';
PRINT N'  002  ImportBatchMaster.MasterSyncCompleted / MasterSyncDate (idempotent)';
PRINT N'  003  Staging_EmployeeData.SubDepartment_Description (idempotent)';
PRINT N'  004  usp_HSE_ValidateBatch';
PRINT N'  005  usp_HSE_SynchronizeMasters          (Live-missing dependency)';
PRINT N'  006  usp_HSE_ReconcileValidationWarnings (Live-missing dependency)';
PRINT N'  007  usp_HSE_InsertNewEmployees';
PRINT N'  008  usp_HSE_UpdateChangedEmployees';
PRINT N'  009  usp_HSE_UpdateReportingHierarchy    (Live-missing dependency)';
PRINT N'  010  usp_RunHRMSSynchronization          (orchestrator last)';
PRINT N'  011  Post-deployment verification (read-only)';
PRINT N'';

DECLARE @Check TABLE
(
    CheckId     int IDENTITY(1,1) PRIMARY KEY,
    Area        nvarchar(40)  NOT NULL,
    ObjectName  nvarchar(128) NOT NULL,
    Detail      nvarchar(400) NULL,
    CurrentState nvarchar(200) NULL,
    Expected    nvarchar(200) NULL,
    Result      nvarchar(20)  NOT NULL
);

/* ---- core tables (must exist) ---- */
INSERT INTO @Check (Area, ObjectName, Detail, CurrentState, Expected, Result)
SELECT N'Table', N'dbo.ImportBatchMaster', N'existence',
       CASE WHEN OBJECT_ID(N'dbo.ImportBatchMaster', N'U') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
       N'EXISTS',
       CASE WHEN OBJECT_ID(N'dbo.ImportBatchMaster', N'U') IS NULL THEN N'FAIL' ELSE N'PASS' END;

INSERT INTO @Check (Area, ObjectName, Detail, CurrentState, Expected, Result)
SELECT N'Table', N'dbo.Staging_EmployeeData', N'existence',
       CASE WHEN OBJECT_ID(N'dbo.Staging_EmployeeData', N'U') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
       N'EXISTS',
       CASE WHEN OBJECT_ID(N'dbo.Staging_EmployeeData', N'U') IS NULL THEN N'FAIL' ELSE N'PASS' END;

INSERT INTO @Check (Area, ObjectName, Detail, CurrentState, Expected, Result)
SELECT N'Table', N'dbo.MST_UserMaster', N'existence',
       CASE WHEN OBJECT_ID(N'dbo.MST_UserMaster', N'U') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
       N'EXISTS',
       CASE WHEN OBJECT_ID(N'dbo.MST_UserMaster', N'U') IS NULL THEN N'FAIL' ELSE N'PASS' END;

DECLARE @MasterTables TABLE (Name sysname);
INSERT INTO @MasterTables (Name) VALUES
    (N'MST_Branch'), (N'MST_Department'), (N'MST_Designation'), (N'MST_Grade'),
    (N'MST_Plant'), (N'MST_Region'), (N'MST_Division'), (N'MST_Category'),
    (N'MST_SubDepartment'), (N'MST_Company');

INSERT INTO @Check (Area, ObjectName, Detail, CurrentState, Expected, Result)
SELECT N'Table', N'dbo.' + Name, N'existence (mapping dependency)',
       CASE WHEN OBJECT_ID(N'dbo.' + Name, N'U') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
       N'EXISTS',
       CASE WHEN OBJECT_ID(N'dbo.' + Name, N'U') IS NULL THEN N'FAIL' ELSE N'PASS' END
FROM @MasterTables;

/* ---- ImportBatchMaster sync columns (already present on Live as of recon) ---- */
INSERT INTO @Check (Area, ObjectName, Detail, CurrentState, Expected, Result)
SELECT N'Column', N'dbo.ImportBatchMaster.MasterSyncCompleted',
       N'type/nullability',
       CASE
           WHEN COL_LENGTH(N'dbo.ImportBatchMaster', N'MasterSyncCompleted') IS NULL THEN N'MISSING'
           ELSE CONCAT(TYPE_NAME(c.user_type_id), N' null=', CASE WHEN c.is_nullable = 1 THEN N'1' ELSE N'0' END)
       END,
       N'bit null=0 (script 002 is idempotent)',
       CASE
           WHEN COL_LENGTH(N'dbo.ImportBatchMaster', N'MasterSyncCompleted') IS NULL THEN N'INFO-WILL-ADD'
           WHEN TYPE_NAME(c.user_type_id) = N'bit' AND c.is_nullable = 0 THEN N'PASS'
           ELSE N'FAIL'
       END
FROM (SELECT 1 AS dummy) AS d
LEFT JOIN sys.columns c
    ON c.object_id = OBJECT_ID(N'dbo.ImportBatchMaster')
   AND c.name = N'MasterSyncCompleted';

INSERT INTO @Check (Area, ObjectName, Detail, CurrentState, Expected, Result)
SELECT N'Column', N'dbo.ImportBatchMaster.MasterSyncDate',
       N'type/nullability',
       CASE
           WHEN COL_LENGTH(N'dbo.ImportBatchMaster', N'MasterSyncDate') IS NULL THEN N'MISSING'
           ELSE CONCAT(TYPE_NAME(c.user_type_id), N' null=', CASE WHEN c.is_nullable = 1 THEN N'1' ELSE N'0' END)
       END,
       N'datetime null=1 (script 002 is idempotent)',
       CASE
           WHEN COL_LENGTH(N'dbo.ImportBatchMaster', N'MasterSyncDate') IS NULL THEN N'INFO-WILL-ADD'
           WHEN TYPE_NAME(c.user_type_id) = N'datetime' AND c.is_nullable = 1 THEN N'PASS'
           ELSE N'FAIL'
       END
FROM (SELECT 1 AS dummy) AS d
LEFT JOIN sys.columns c
    ON c.object_id = OBJECT_ID(N'dbo.ImportBatchMaster')
   AND c.name = N'MasterSyncDate';

/* ---- Staging SubDepartment_Description (Live behind) ---- */
INSERT INTO @Check (Area, ObjectName, Detail, CurrentState, Expected, Result)
SELECT N'Column', N'dbo.Staging_EmployeeData.SubDepartment_Description',
       N'type/nullability',
       CASE
           WHEN COL_LENGTH(N'dbo.Staging_EmployeeData', N'SubDepartment_Description') IS NULL THEN N'MISSING'
           ELSE CONCAT(TYPE_NAME(c.user_type_id), N'(', c.max_length, N') null=', CASE WHEN c.is_nullable = 1 THEN N'1' ELSE N'0' END)
       END,
       N'varchar(255) null=1 (script 003 will add if missing)',
       CASE
           WHEN COL_LENGTH(N'dbo.Staging_EmployeeData', N'SubDepartment_Description') IS NULL THEN N'INFO-WILL-ADD'
           WHEN TYPE_NAME(c.user_type_id) = N'varchar' AND c.max_length = 255 AND c.is_nullable = 1 THEN N'PASS'
           ELSE N'WARN'
       END
FROM (SELECT 1 AS dummy) AS d
LEFT JOIN sys.columns c
    ON c.object_id = OBJECT_ID(N'dbo.Staging_EmployeeData')
   AND c.name = N'SubDepartment_Description';

/* ---- mapping columns on MST_* ---- */
INSERT INTO @Check (Area, ObjectName, Detail, CurrentState, Expected, Result)
VALUES
    (N'Column', N'dbo.MST_Branch.Branch_ID', N'ERP key',
     CASE WHEN COL_LENGTH(N'dbo.MST_Branch', N'Branch_ID') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
     N'EXISTS', CASE WHEN COL_LENGTH(N'dbo.MST_Branch', N'Branch_ID') IS NULL THEN N'FAIL' ELSE N'PASS' END),
    (N'Column', N'dbo.MST_Department.Department_ID', N'ERP key',
     CASE WHEN COL_LENGTH(N'dbo.MST_Department', N'Department_ID') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
     N'EXISTS', CASE WHEN COL_LENGTH(N'dbo.MST_Department', N'Department_ID') IS NULL THEN N'FAIL' ELSE N'PASS' END),
    (N'Column', N'dbo.MST_Grade.Grade_ID', N'ERP key',
     CASE WHEN COL_LENGTH(N'dbo.MST_Grade', N'Grade_ID') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
     N'EXISTS', CASE WHEN COL_LENGTH(N'dbo.MST_Grade', N'Grade_ID') IS NULL THEN N'FAIL' ELSE N'PASS' END),
    (N'Column', N'dbo.MST_Plant.PlantID_ID', N'ERP key',
     CASE WHEN COL_LENGTH(N'dbo.MST_Plant', N'PlantID_ID') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
     N'EXISTS', CASE WHEN COL_LENGTH(N'dbo.MST_Plant', N'PlantID_ID') IS NULL THEN N'FAIL' ELSE N'PASS' END),
    (N'Column', N'dbo.MST_Region.Region_ID', N'ERP key',
     CASE WHEN COL_LENGTH(N'dbo.MST_Region', N'Region_ID') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
     N'EXISTS', CASE WHEN COL_LENGTH(N'dbo.MST_Region', N'Region_ID') IS NULL THEN N'FAIL' ELSE N'PASS' END),
    (N'Column', N'dbo.MST_Division.Division_ID', N'ERP key',
     CASE WHEN COL_LENGTH(N'dbo.MST_Division', N'Division_ID') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
     N'EXISTS', CASE WHEN COL_LENGTH(N'dbo.MST_Division', N'Division_ID') IS NULL THEN N'FAIL' ELSE N'PASS' END),
    (N'Column', N'dbo.MST_Category.Category_ID', N'ERP key',
     CASE WHEN COL_LENGTH(N'dbo.MST_Category', N'Category_ID') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
     N'EXISTS', CASE WHEN COL_LENGTH(N'dbo.MST_Category', N'Category_ID') IS NULL THEN N'FAIL' ELSE N'PASS' END),
    (N'Column', N'dbo.MST_SubDepartment.SubDepartment_ID', N'ERP key',
     CASE WHEN COL_LENGTH(N'dbo.MST_SubDepartment', N'SubDepartment_ID') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
     N'EXISTS', CASE WHEN COL_LENGTH(N'dbo.MST_SubDepartment', N'SubDepartment_ID') IS NULL THEN N'FAIL' ELSE N'PASS' END),
    (N'Column', N'dbo.MST_Designation.Id', N'internal PK exception target',
     CASE WHEN COL_LENGTH(N'dbo.MST_Designation', N'Id') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
     N'EXISTS', CASE WHEN COL_LENGTH(N'dbo.MST_Designation', N'Id') IS NULL THEN N'FAIL' ELSE N'PASS' END),
    (N'Column', N'dbo.MST_Designation.Designation_ID', N'lookup key',
     CASE WHEN COL_LENGTH(N'dbo.MST_Designation', N'Designation_ID') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
     N'EXISTS', CASE WHEN COL_LENGTH(N'dbo.MST_Designation', N'Designation_ID') IS NULL THEN N'FAIL' ELSE N'PASS' END);

/* ---- procedures: missing is expected on Live for HSE children ---- */
DECLARE @Procs TABLE (Name sysname, Role nvarchar(80));
INSERT INTO @Procs (Name, Role) VALUES
    (N'usp_HSE_ValidateBatch', N'will CREATE OR ALTER (script 004)'),
    (N'usp_HSE_SynchronizeMasters', N'will CREATE OR ALTER (script 005) — currently missing on Live'),
    (N'usp_HSE_ReconcileValidationWarnings', N'will CREATE OR ALTER (script 006) — currently missing on Live'),
    (N'usp_HSE_InsertNewEmployees', N'will CREATE OR ALTER (script 007) — currently missing on Live'),
    (N'usp_HSE_UpdateChangedEmployees', N'will CREATE OR ALTER (script 008) — currently missing on Live'),
    (N'usp_HSE_UpdateReportingHierarchy', N'will CREATE OR ALTER (script 009) — currently missing on Live'),
    (N'usp_RunHRMSSynchronization', N'will CREATE OR ALTER (script 010) — Live currently a stub');

INSERT INTO @Check (Area, ObjectName, Detail, CurrentState, Expected, Result)
SELECT N'Procedure', N'dbo.' + p.Name, p.Role,
       CASE
           WHEN o.object_id IS NULL THEN N'MISSING'
           ELSE CONCAT(
                N'EXISTS modify_date=', CONVERT(nvarchar(19), o.modify_date, 120),
                N' bytes=', CONVERT(nvarchar(20), DATALENGTH(m.definition)))
       END,
       N'Approved Phase A+B1 definition after deploy',
       CASE WHEN o.object_id IS NULL THEN N'INFO-WILL-CREATE' ELSE N'INFO-WILL-REPLACE' END
FROM @Procs p
LEFT JOIN sys.objects o ON o.name = p.Name AND o.type = N'P'
LEFT JOIN sys.sql_modules m ON m.object_id = o.object_id;

/* Live stub detection */
INSERT INTO @Check (Area, ObjectName, Detail, CurrentState, Expected, Result)
SELECT N'Procedure', N'dbo.usp_RunHRMSSynchronization',
       N'Live stub currently EXEC usp_ProcessEmployeeBatch',
       CASE
           WHEN OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RunHRMSSynchronization')) LIKE N'%usp_ProcessEmployeeBatch%'
               THEN N'STUB_CALLS_usp_ProcessEmployeeBatch'
           WHEN OBJECT_ID(N'dbo.usp_RunHRMSSynchronization') IS NULL THEN N'MISSING'
           ELSE N'NON-STUB'
       END,
       N'Phase A orchestrator with @Action + MasterSyncCompleted gate',
       N'INFO';

INSERT INTO @Check (Area, ObjectName, Detail, CurrentState, Expected, Result)
SELECT N'Procedure', N'dbo.usp_ProcessEmployeeBatch',
       N'legacy engine — NOT dropped by this package',
       CASE WHEN OBJECT_ID(N'dbo.usp_ProcessEmployeeBatch', N'P') IS NULL THEN N'MISSING' ELSE N'EXISTS (preserve)' END,
       N'Preserve (out of scope)',
       N'INFO';

PRINT N'CURRENT STATE:';
SELECT CheckId, Area, ObjectName, Detail, CurrentState, Expected, Result
FROM @Check
ORDER BY CheckId;

DECLARE @FailCount int = (SELECT COUNT(*) FROM @Check WHERE Result = N'FAIL');
PRINT N'';
PRINT N'FAIL COUNT: ' + CONVERT(nvarchar(10), @FailCount);

IF @FailCount > 0
BEGIN
    THROW 56102, N'Pre-deployment check failed. Review FAIL rows. No scripts after 001 should be applied until resolved.', 1;
END;

PRINT N'Pre-deployment check completed. Fail-fast conditions were not raised.';
PRINT N'This script made no database changes.';
GO
