/*
    Package:     Database/Deployments/HRMS_PhaseB2_3_UAT_OrgRemediation
    Script:      C_PostApplyVerification.sql
    Phase:       B2.3
    Mode:        READ-ONLY (independent of the apply transaction)
    Database:    AnmolDristiDev ONLY
    Isolation:   READ COMMITTED

    Confirms:
      - 115 audited mappings are now ERP-domain compliant
      - 5 zero-targets unchanged
      - 4 fixtures unchanged vs remaining INTERNAL_ID_DRIFT
      - Designation_ID has no ERP-code drift
      - Plant 3277 was not converted
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

IF DB_NAME() = N'AnmolDristi'
    THROW 56201, N'ABORT: Live database AnmolDristi is forbidden for Phase B2.3.', 1;

IF DB_NAME() <> N'AnmolDristiDev'
BEGIN
    DECLARE @dbMsg nvarchar(400) =
        N'ABORT: connected to ' + QUOTENAME(DB_NAME()) + N'. Expected [AnmolDristiDev].';
    THROW 56202, @dbMsg, 1;
END;

DECLARE @RunId varchar(40) = (
    SELECT MAX(RemediationRunId)
    FROM dbo.AUDIT_UserMasterOrgRemediation_B23
    WHERE ApplyStatus = N'APPLIED'
);

SELECT
    DB_NAME() AS DbName,
    @RunId AS RemediationRunId,
    (SELECT COUNT(*) FROM dbo.AUDIT_UserMasterOrgRemediation_B23 WHERE RemediationRunId = @RunId AND ApplyStatus = N'APPLIED') AS AppliedAuditRows,
    (SELECT MIN(AppliedAt) FROM dbo.AUDIT_UserMasterOrgRemediation_B23 WHERE RemediationRunId = @RunId) AS AppliedAt;

/* Each APPLIED audit row: current UserMaster column == NewValue, and NewValue is unique ERP */
IF OBJECT_ID('tempdb..#OrgMap') IS NOT NULL DROP TABLE #OrgMap;
CREATE TABLE #OrgMap
(
    ColumnName sysname NOT NULL,
    MasterTable sysname NOT NULL,
    ErpColumn sysname NOT NULL
);
INSERT INTO #OrgMap (ColumnName, MasterTable, ErpColumn) VALUES
    (N'BranchId',        N'MST_Branch',        N'Branch_ID'),
    (N'DepartmentId',    N'MST_Department',    N'Department_ID'),
    (N'GradeId',         N'MST_Grade',         N'Grade_ID'),
    (N'PlantId',         N'MST_Plant',         N'PlantID_ID'),
    (N'RegionId',        N'MST_Region',        N'Region_ID'),
    (N'DivisionId',      N'MST_Division',      N'Division_ID'),
    (N'CategoryId',      N'MST_Category',      N'Category_ID'),
    (N'SubDepartmentId', N'MST_SubDepartment', N'SubDepartment_ID');

IF OBJECT_ID('tempdb..#AllDrift') IS NOT NULL DROP TABLE #AllDrift;
CREATE TABLE #AllDrift
(
    UserMasterId int NOT NULL,
    EmployeeCode varchar(50) NOT NULL,
    ColumnName sysname NOT NULL,
    OldValue int NOT NULL,
    MasterInternalId int NOT NULL,
    MasterERPValue int NULL,
    ErpKeyRowCount int NOT NULL,
    IsFixture bit NOT NULL,
    IsZeroTarget bit NOT NULL
);

DECLARE @sql nvarchar(max) = N'';
SELECT @sql = @sql + N'
INSERT INTO #AllDrift (UserMasterId, EmployeeCode, ColumnName, OldValue, MasterInternalId, MasterERPValue, ErpKeyRowCount, IsFixture, IsZeroTarget)
SELECT
    u.Id, u.EmployeeCode, N''' + ColumnName + N''', u.[' + ColumnName + N'], m.Id,
    TRY_CONVERT(INT, m.[' + ErpColumn + N']),
    (SELECT COUNT(*) FROM dbo.[' + MasterTable + N'] x
      WHERE TRY_CONVERT(INT, x.[' + ErpColumn + N']) = TRY_CONVERT(INT, m.[' + ErpColumn + N'])),
    CASE WHEN u.EmployeeCode IN (N''PHA9B2E_EX1'', N''PHA9B2E_MGR'', N''PHA9B2E_NW1'', N''PHA9B2E_V7'') THEN 1 ELSE 0 END,
    CASE WHEN TRY_CONVERT(INT, m.[' + ErpColumn + N']) = 0 THEN 1 ELSE 0 END
FROM dbo.MST_UserMaster AS u
INNER JOIN dbo.[' + MasterTable + N'] AS m ON m.Id = u.[' + ColumnName + N']
WHERE u.[' + ColumnName + N'] IS NOT NULL
  AND u.[' + ColumnName + N'] <> 0
  AND NOT EXISTS (
        SELECT 1 FROM dbo.[' + MasterTable + N'] e
        WHERE TRY_CONVERT(INT, e.[' + ErpColumn + N']) = u.[' + ColumnName + N']
      );
'
FROM #OrgMap;
EXEC sp_executesql @sql;

SELECT
    SUM(CASE WHEN IsFixture = 0 AND IsZeroTarget = 0
              AND MasterERPValue IS NOT NULL AND MasterERPValue <> 0
              AND ErpKeyRowCount = 1 THEN 1 ELSE 0 END) AS RemainingApprovedHistoricalNonZeroDrift,
    SUM(CASE WHEN IsFixture = 1 THEN 1 ELSE 0 END) AS FixtureDriftInstances,
    SUM(CASE WHEN IsFixture = 0 AND IsZeroTarget = 1 THEN 1 ELSE 0 END) AS ZeroTargetExclusions,
    COUNT(*) AS RemainingInternalIdDriftInstances
FROM #AllDrift;

SELECT UserMasterId, EmployeeCode, ColumnName, OldValue, MasterInternalId, MasterERPValue
FROM #AllDrift
WHERE IsFixture = 0 AND IsZeroTarget = 1
ORDER BY EmployeeCode, ColumnName;

SELECT DISTINCT EmployeeCode
FROM #AllDrift
WHERE IsFixture = 1
ORDER BY EmployeeCode;

/* Designation: internal Id is compliant; ERP-code would be drift */
SELECT
    SUM(CASE WHEN u.Designation_ID IS NOT NULL AND u.Designation_ID <> 0
              AND EXISTS (SELECT 1 FROM dbo.MST_Designation d WHERE d.Id = u.Designation_ID)
              AND NOT EXISTS (SELECT 1 FROM dbo.MST_Designation d WHERE TRY_CONVERT(INT, d.Designation_ID) = u.Designation_ID)
             THEN 1 ELSE 0 END) AS DesignationInternalCompliant,
    SUM(CASE WHEN u.Designation_ID IS NOT NULL AND u.Designation_ID <> 0
              AND NOT EXISTS (SELECT 1 FROM dbo.MST_Designation d WHERE d.Id = u.Designation_ID)
              AND EXISTS (SELECT 1 FROM dbo.MST_Designation d WHERE TRY_CONVERT(INT, d.Designation_ID) = u.Designation_ID)
             THEN 1 ELSE 0 END) AS DesignationErpCodeDrift
FROM dbo.MST_UserMaster AS u;

SELECT u.EmployeeCode, u.PlantId
FROM dbo.MST_UserMaster AS u
WHERE u.EmployeeCode = N'M2668' OR u.PlantId = 3277;

/* Audit vs current: every APPLIED row current value = NewValue */
IF OBJECT_ID('tempdb..#AuditMatch') IS NOT NULL DROP TABLE #AuditMatch;
CREATE TABLE #AuditMatch
(
    ColumnName sysname NOT NULL,
    AuditRows int NOT NULL,
    CurrentEqualsNew int NOT NULL,
    CurrentStillOld int NOT NULL
);

DECLARE @csql nvarchar(max) = N'';
SELECT @csql = @csql + N'
INSERT INTO #AuditMatch (ColumnName, AuditRows, CurrentEqualsNew, CurrentStillOld)
SELECT
    N''' + ColumnName + N''',
    COUNT(*),
    SUM(CASE WHEN u.[' + ColumnName + N'] = a.NewValue THEN 1 ELSE 0 END),
    SUM(CASE WHEN u.[' + ColumnName + N'] = a.OldValue THEN 1 ELSE 0 END)
FROM dbo.AUDIT_UserMasterOrgRemediation_B23 AS a
INNER JOIN dbo.MST_UserMaster AS u ON u.Id = a.UserMasterId
WHERE a.RemediationRunId = @RunId
  AND a.ApplyStatus = N''APPLIED''
  AND a.ColumnName = N''' + ColumnName + N''';
'
FROM #OrgMap;
EXEC sp_executesql @csql, N'@RunId varchar(40)', @RunId = @RunId;

SELECT * FROM #AuditMatch ORDER BY ColumnName;

SELECT
    SUM(AuditRows) AS AuditRowsChecked,
    SUM(CurrentEqualsNew) AS MappingsNowErp,
    SUM(CurrentStillOld) AS MappingsStillOld
FROM #AuditMatch;

/* Explicit exclusion values */
SELECT
    u.EmployeeCode, u.BranchId, u.DepartmentId, u.GradeId, u.PlantId,
    u.RegionId, u.DivisionId, u.CategoryId, u.SubDepartmentId, u.Designation_ID
FROM dbo.MST_UserMaster AS u
WHERE u.EmployeeCode IN (
    N'0001', N'0006', N'0034',
    N'PHA9B2E_EX1', N'PHA9B2E_MGR', N'PHA9B2E_NW1', N'PHA9B2E_V7',
    N'M2668'
)
ORDER BY u.EmployeeCode;
