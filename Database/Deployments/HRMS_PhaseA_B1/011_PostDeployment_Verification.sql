/*
    Package:     Database/Deployments/HRMS_PhaseA_B1
    Script:      011_PostDeployment_Verification.sql
    Baseline:    Aug26_AILWorks_AmimnrupSupport @ 5ff3a3f (PR #203)
    Mode:        READ-ONLY
    Purpose:     Verify schema, procedures, signatures, and mapping tokens
                 after scripts 002–010. Does not modify employee data and
                 does not execute HRMS synchronization.

    Expected HASHBYTES SHA2_256 of OBJECT_DEFINITION (UAT AnmolDristiDev
    after CREATE OR ALTER of the approved repository files):
      usp_HSE_ValidateBatch              0C84CD55BF21274664390C6968A372EE9EA5702E64A13497F36EC321200698CD
      usp_RunHRMSSynchronization         2589DB21030252A0F0157741A630EA2EB9A45D0E545F4E0DC033F4F2CFCEB325
      usp_HSE_InsertNewEmployees         97FCB04920457A427DA5CDB8FCB1D5D0F28CC629666F7EB99078C6942A1A04D9
      usp_HSE_UpdateChangedEmployees     D67DAA1E717BDE279FD7E1DFC0A85008C984304EFF927B318A9FE51CFD6E024E
      usp_HSE_SynchronizeMasters         E64BC5EBC5A0C14B90BB939548032D98AE06CC730B24B63A87BA97E7FB296A3A
      usp_HSE_ReconcileValidationWarnings 694BE571F60C8825FF460C0C85E72BDB991B23B22AC721DDBCE926C79AE6EFEC
      usp_HSE_UpdateReportingHierarchy   5AF50C20DB061560558BB01DC6999B4B0C4B50121E7E92F0ADECFD732E7A4EF1
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ExpectedDatabase sysname = N'AnmolDristi';
DECLARE @AllowOtherDatabase bit = 0;
IF DB_NAME() <> @ExpectedDatabase AND @AllowOtherDatabase = 0
BEGIN
    DECLARE @msg nvarchar(4000) =
        N'Abort: connected to ' + QUOTENAME(DB_NAME())
        + N'. Expected ' + QUOTENAME(@ExpectedDatabase)
        + N'. Set @AllowOtherDatabase = 1 to override.';
    THROW 56101, @msg, 1;
END;

PRINT N'============================================================';
PRINT N'HRMS Phase A + B1  — POST-DEPLOYMENT VERIFICATION (read-only)';
PRINT N'============================================================';
PRINT N'DATABASE: ' + QUOTENAME(DB_NAME());
PRINT N'SERVER:   ' + CONVERT(nvarchar(128), @@SERVERNAME);
PRINT N'AT:       ' + CONVERT(nvarchar(30), SYSDATETIME(), 126);

DECLARE @V TABLE
(
    CheckId int IDENTITY(1,1) PRIMARY KEY,
    Area nvarchar(40) NOT NULL,
    CheckName nvarchar(200) NOT NULL,
    Observed nvarchar(400) NULL,
    Result nvarchar(10) NOT NULL
);

/* -------------------- schema -------------------- */
INSERT INTO @V (Area, CheckName, Observed, Result)
SELECT N'Schema', N'ImportBatchMaster.MasterSyncCompleted exists',
       CASE WHEN COL_LENGTH(N'dbo.ImportBatchMaster', N'MasterSyncCompleted') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
       CASE WHEN COL_LENGTH(N'dbo.ImportBatchMaster', N'MasterSyncCompleted') IS NULL THEN N'FAIL' ELSE N'PASS' END;

INSERT INTO @V (Area, CheckName, Observed, Result)
SELECT N'Schema', N'ImportBatchMaster.MasterSyncDate exists',
       CASE WHEN COL_LENGTH(N'dbo.ImportBatchMaster', N'MasterSyncDate') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
       CASE WHEN COL_LENGTH(N'dbo.ImportBatchMaster', N'MasterSyncDate') IS NULL THEN N'FAIL' ELSE N'PASS' END;

INSERT INTO @V (Area, CheckName, Observed, Result)
SELECT N'Schema', N'MasterSyncCompleted is bit NOT NULL',
       CONCAT(TYPE_NAME(c.user_type_id), N' null=', c.is_nullable),
       CASE WHEN TYPE_NAME(c.user_type_id) = N'bit' AND c.is_nullable = 0 THEN N'PASS' ELSE N'FAIL' END
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.ImportBatchMaster') AND c.name = N'MasterSyncCompleted';

IF NOT EXISTS (SELECT 1 FROM @V WHERE CheckName = N'MasterSyncCompleted is bit NOT NULL')
    INSERT INTO @V (Area, CheckName, Observed, Result)
    VALUES (N'Schema', N'MasterSyncCompleted is bit NOT NULL', N'column missing', N'FAIL');

INSERT INTO @V (Area, CheckName, Observed, Result)
SELECT N'Schema', N'MasterSyncDate is datetime NULL',
       CONCAT(TYPE_NAME(c.user_type_id), N' null=', c.is_nullable),
       CASE WHEN TYPE_NAME(c.user_type_id) = N'datetime' AND c.is_nullable = 1 THEN N'PASS' ELSE N'FAIL' END
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.ImportBatchMaster') AND c.name = N'MasterSyncDate';

IF NOT EXISTS (SELECT 1 FROM @V WHERE CheckName = N'MasterSyncDate is datetime NULL')
    INSERT INTO @V (Area, CheckName, Observed, Result)
    VALUES (N'Schema', N'MasterSyncDate is datetime NULL', N'column missing', N'FAIL');

INSERT INTO @V (Area, CheckName, Observed, Result)
SELECT N'Schema', N'Staging_EmployeeData.SubDepartment_Description exists',
       CASE WHEN COL_LENGTH(N'dbo.Staging_EmployeeData', N'SubDepartment_Description') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
       CASE WHEN COL_LENGTH(N'dbo.Staging_EmployeeData', N'SubDepartment_Description') IS NULL THEN N'FAIL' ELSE N'PASS' END;

/* -------------------- procedures exist -------------------- */
DECLARE @Need TABLE (Name sysname);
INSERT INTO @Need VALUES
    (N'usp_HSE_ValidateBatch'),
    (N'usp_RunHRMSSynchronization'),
    (N'usp_HSE_InsertNewEmployees'),
    (N'usp_HSE_UpdateChangedEmployees'),
    (N'usp_HSE_SynchronizeMasters'),
    (N'usp_HSE_ReconcileValidationWarnings'),
    (N'usp_HSE_UpdateReportingHierarchy');

INSERT INTO @V (Area, CheckName, Observed, Result)
SELECT N'Procedure', N'exists dbo.' + n.Name,
       CASE WHEN OBJECT_ID(N'dbo.' + n.Name, N'P') IS NULL THEN N'MISSING' ELSE N'EXISTS' END,
       CASE WHEN OBJECT_ID(N'dbo.' + n.Name, N'P') IS NULL THEN N'FAIL' ELSE N'PASS' END
FROM @Need n;

/* -------------------- signatures / tokens -------------------- */
DECLARE @DefValidate nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_HSE_ValidateBatch'));
DECLARE @DefOrch     nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RunHRMSSynchronization'));
DECLARE @DefInsert   nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_HSE_InsertNewEmployees'));
DECLARE @DefUpdate   nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_HSE_UpdateChangedEmployees'));

INSERT INTO @V (Area, CheckName, Observed, Result)
VALUES
    (N'Signature', N'ValidateBatch contains @Action',
     CASE WHEN @DefValidate LIKE N'%@Action%' THEN N'FOUND' ELSE N'NOT FOUND' END,
     CASE WHEN @DefValidate LIKE N'%@Action%' THEN N'PASS' ELSE N'FAIL' END),
    (N'Signature', N'ValidateBatch contains @OverwriteFields',
     CASE WHEN @DefValidate LIKE N'%@OverwriteFields%' THEN N'FOUND' ELSE N'NOT FOUND' END,
     CASE WHEN @DefValidate LIKE N'%@OverwriteFields%' THEN N'PASS' ELSE N'FAIL' END),
    (N'Signature', N'ValidateBatch uses DECLARE @ModuleXml XML (not inline CONVERT().nodes)',
     CASE WHEN @DefValidate LIKE N'%DECLARE @ModuleXml XML%' AND @DefValidate LIKE N'%@ModuleXml.nodes%'
          THEN N'XML variable + .nodes()' ELSE N'MISSING/INCORRECT' END,
     CASE WHEN @DefValidate LIKE N'%DECLARE @ModuleXml XML%' AND @DefValidate LIKE N'%@ModuleXml.nodes%'
          THEN N'PASS' ELSE N'FAIL' END),
    (N'Signature', N'RunHRMSSynchronization contains MasterSyncCompleted gate',
     CASE WHEN @DefOrch LIKE N'%MasterSyncCompleted%' THEN N'FOUND' ELSE N'NOT FOUND' END,
     CASE WHEN @DefOrch LIKE N'%MasterSyncCompleted%' THEN N'PASS' ELSE N'FAIL' END),
    (N'Signature', N'RunHRMSSynchronization contains THROW 50005',
     CASE WHEN @DefOrch LIKE N'%THROW 50005%' THEN N'FOUND' ELSE N'NOT FOUND' END,
     CASE WHEN @DefOrch LIKE N'%THROW 50005%' THEN N'PASS' ELSE N'FAIL' END),
    (N'Signature', N'RunHRMSSynchronization is not the Live stub (usp_ProcessEmployeeBatch)',
     CASE WHEN @DefOrch LIKE N'%usp_ProcessEmployeeBatch%' THEN N'STILL STUB' ELSE N'REPLACED' END,
     CASE WHEN @DefOrch LIKE N'%usp_ProcessEmployeeBatch%' THEN N'FAIL' ELSE N'PASS' END),
    (N'Signature', N'InsertNewEmployees ERP Branch_ID write',
     CASE WHEN @DefInsert LIKE N'%BranchId = br.Branch_ID%' THEN N'FOUND' ELSE N'NOT FOUND' END,
     CASE WHEN @DefInsert LIKE N'%BranchId = br.Branch_ID%' THEN N'PASS' ELSE N'FAIL' END),
    (N'Signature', N'InsertNewEmployees Designation uses MST_Designation.Id',
     CASE WHEN @DefInsert LIKE N'%DesignationId = des.Id%' THEN N'FOUND' ELSE N'NOT FOUND' END,
     CASE WHEN @DefInsert LIKE N'%DesignationId = des.Id%' THEN N'PASS' ELSE N'FAIL' END),
    (N'Signature', N'UpdateChangedEmployees ERP Branch_ID write',
     CASE WHEN @DefUpdate LIKE N'%NewBranchId%br.Branch_ID%' THEN N'FOUND' ELSE N'NOT FOUND' END,
     CASE WHEN @DefUpdate LIKE N'%NewBranchId%br.Branch_ID%' THEN N'PASS' ELSE N'FAIL' END),
    (N'Signature', N'UpdateChangedEmployees Designation uses MST_Designation.Id',
     CASE WHEN @DefUpdate LIKE N'%NewDesignationId%des.Id%' THEN N'FOUND' ELSE N'NOT FOUND' END,
     CASE WHEN @DefUpdate LIKE N'%NewDesignationId%des.Id%' THEN N'PASS' ELSE N'FAIL' END),
    (N'Signature', N'UpdateChangedEmployees selected-field @OverwriteFields',
     CASE WHEN @DefUpdate LIKE N'%@OverwriteFields%' THEN N'FOUND' ELSE N'NOT FOUND' END,
     CASE WHEN @DefUpdate LIKE N'%@OverwriteFields%' THEN N'PASS' ELSE N'FAIL' END);

/* mapping tokens in insert+update definitions */
DECLARE @Map TABLE (Token nvarchar(80), Kind nvarchar(20));
INSERT INTO @Map VALUES
    (N'Branch_ID', N'ERP'),
    (N'Department_ID', N'ERP'),
    (N'Grade_ID', N'ERP'),
    (N'PlantID_ID', N'ERP'),
    (N'Region_ID', N'ERP'),
    (N'Division_ID', N'ERP'),
    (N'Category_ID', N'ERP'),
    (N'SubDepartment_ID', N'ERP'),
    (N'MST_Designation.Id', N'DesignationPK');

INSERT INTO @V (Area, CheckName, Observed, Result)
SELECT N'Mapping', N'Insert+Update contain ' + Token,
       CASE
           WHEN Kind = N'DesignationPK'
                AND (@DefInsert LIKE N'%MST_Designation.Id%' OR @DefInsert LIKE N'%des.Id%')
                AND (@DefUpdate LIKE N'%MST_Designation.Id%' OR @DefUpdate LIKE N'%des.Id%')
               THEN N'FOUND'
           WHEN Kind = N'ERP' AND @DefInsert LIKE N'%' + Token + N'%' AND @DefUpdate LIKE N'%' + Token + N'%'
               THEN N'FOUND'
           ELSE N'NOT FOUND'
       END,
       CASE
           WHEN Kind = N'DesignationPK'
                AND (@DefInsert LIKE N'%MST_Designation.Id%' OR @DefInsert LIKE N'%des.Id%')
                AND (@DefUpdate LIKE N'%MST_Designation.Id%' OR @DefUpdate LIKE N'%des.Id%')
               THEN N'PASS'
           WHEN Kind = N'ERP' AND @DefInsert LIKE N'%' + Token + N'%' AND @DefUpdate LIKE N'%' + Token + N'%'
               THEN N'PASS'
           ELSE N'FAIL'
       END
FROM @Map;

/* Designation must NOT write Designation_ID into UserMaster (exception: lookup by Designation_ID, store Id) */
INSERT INTO @V (Area, CheckName, Observed, Result)
SELECT N'Mapping', N'Insert write token is DesignationId = des.Id (not des.Designation_ID)',
       CASE
           WHEN @DefInsert LIKE N'%DesignationId = des.Id%'
                AND @DefInsert NOT LIKE N'%DesignationId = des.Designation_ID%'
               THEN N'OK'
           ELSE N'INCORRECT WRITE'
       END,
       CASE
           WHEN @DefInsert LIKE N'%DesignationId = des.Id%'
                AND @DefInsert NOT LIKE N'%DesignationId = des.Designation_ID%'
               THEN N'PASS'
           ELSE N'FAIL'
       END;

INSERT INTO @V (Area, CheckName, Observed, Result)
SELECT N'Mapping', N'Update write token is THEN des.Id for NewDesignationId (not THEN des.Designation_ID)',
       CASE
           WHEN @DefUpdate LIKE N'%THEN des.Id ELSE NULL END%'
                AND CHARINDEX(N'NewDesignationId = CASE WHEN @UpdDesignation = 1 AND r.DesignationCode IS NOT NULL THEN des.Id', @DefUpdate) > 0
                AND CHARINDEX(N'NewDesignationId = CASE WHEN @UpdDesignation = 1 AND r.DesignationCode IS NOT NULL THEN des.Designation_ID', @DefUpdate) = 0
               THEN N'OK'
           ELSE N'INCORRECT WRITE'
       END,
       CASE
           WHEN @DefUpdate LIKE N'%THEN des.Id ELSE NULL END%'
                AND CHARINDEX(N'NewDesignationId = CASE WHEN @UpdDesignation = 1 AND r.DesignationCode IS NOT NULL THEN des.Id', @DefUpdate) > 0
                AND CHARINDEX(N'NewDesignationId = CASE WHEN @UpdDesignation = 1 AND r.DesignationCode IS NOT NULL THEN des.Designation_ID', @DefUpdate) = 0
               THEN N'PASS'
           ELSE N'FAIL'
       END;

/* -------------------- HASHBYTES vs UAT expected -------------------- */
DECLARE @ExpectedHash TABLE (Name sysname PRIMARY KEY, ExpectedSha varchar(64));
INSERT INTO @ExpectedHash VALUES
    (N'usp_HSE_ValidateBatch',               '0C84CD55BF21274664390C6968A372EE9EA5702E64A13497F36EC321200698CD'),
    (N'usp_RunHRMSSynchronization',          '2589DB21030252A0F0157741A630EA2EB9A45D0E545F4E0DC033F4F2CFCEB325'),
    (N'usp_HSE_InsertNewEmployees',          '97FCB04920457A427DA5CDB8FCB1D5D0F28CC629666F7EB99078C6942A1A04D9'),
    (N'usp_HSE_UpdateChangedEmployees',      'D67DAA1E717BDE279FD7E1DFC0A85008C984304EFF927B318A9FE51CFD6E024E'),
    (N'usp_HSE_SynchronizeMasters',          'E64BC5EBC5A0C14B90BB939548032D98AE06CC730B24B63A87BA97E7FB296A3A'),
    (N'usp_HSE_ReconcileValidationWarnings', '694BE571F60C8825FF460C0C85E72BDB991B23B22AC721DDBCE926C79AE6EFEC'),
    (N'usp_HSE_UpdateReportingHierarchy',    '5AF50C20DB061560558BB01DC6999B4B0C4B50121E7E92F0ADECFD732E7A4EF1');

INSERT INTO @V (Area, CheckName, Observed, Result)
SELECT N'Hash', N'HASHBYTES SHA2_256 ' + e.Name,
       CONCAT(N'actual=', ISNULL(CONVERT(varchar(64), HASHBYTES(N'SHA2_256', CONVERT(varbinary(max), m.definition)), 2), N'<missing>')),
       CASE
           WHEN m.definition IS NULL THEN N'FAIL'
           WHEN CONVERT(varchar(64), HASHBYTES(N'SHA2_256', CONVERT(varbinary(max), m.definition)), 2) = e.ExpectedSha THEN N'PASS'
           ELSE N'FAIL'
       END
FROM @ExpectedHash e
LEFT JOIN sys.objects o ON o.name = e.Name AND o.type = N'P'
LEFT JOIN sys.sql_modules m ON m.object_id = o.object_id;

PRINT N'';
SELECT CheckId, Area, CheckName, Observed, Result
FROM @V
ORDER BY CheckId;

DECLARE @Fail int = (SELECT COUNT(*) FROM @V WHERE Result = N'FAIL');
DECLARE @Pass int = (SELECT COUNT(*) FROM @V WHERE Result = N'PASS');
PRINT N'';
PRINT N'PASS=' + CONVERT(nvarchar(10), @Pass) + N'  FAIL=' + CONVERT(nvarchar(10), @Fail);

IF @Fail > 0
    THROW 56190, N'Post-deployment verification FAILED. Review FAIL rows. No employee data was modified.', 1;

PRINT N'Post-deployment verification PASSED. No employee data was modified.';
GO
