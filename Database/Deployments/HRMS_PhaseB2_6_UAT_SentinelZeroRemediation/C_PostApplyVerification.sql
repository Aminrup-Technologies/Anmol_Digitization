/*
    Package:     Database/Deployments/HRMS_PhaseB2_6_UAT_SentinelZeroRemediation
    Script:      C_PostApplyVerification.sql
    Phase:       B2.6
    Mode:        READ-ONLY (independent of the apply transaction)
    Database:    AnmolDristiDev ONLY
    Isolation:   READ COMMITTED

    Confirms the five B2.6 mappings are ERP sentinel 0, B2.3/fixtures/
    designation/Plant 3277 are untouched, and B2.6 audit is APPLIED.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

IF DB_NAME() = N'AnmolDristi'
    THROW 56601, N'ABORT: Live database AnmolDristi is forbidden for Phase B2.6.', 1;

IF DB_NAME() <> N'AnmolDristiDev'
BEGIN
    DECLARE @dbMsg nvarchar(400) =
        N'ABORT: connected to ' + QUOTENAME(DB_NAME()) + N'. Expected [AnmolDristiDev].';
    THROW 56602, @dbMsg, 1;
END;

IF OBJECT_ID(N'dbo.AUDIT_UserMasterOrgSentinelZero_B26', N'U') IS NULL
    THROW 56603, N'ABORT: B2.6 audit table missing.', 1;

DECLARE @RunCount int = (
    SELECT COUNT(DISTINCT RemediationRunId)
    FROM dbo.AUDIT_UserMasterOrgSentinelZero_B26
    WHERE ApplyStatus = N'APPLIED'
);
DECLARE @RunId varchar(40) = NULL;
IF @RunCount = 1
    SELECT @RunId = MIN(RemediationRunId)
    FROM dbo.AUDIT_UserMasterOrgSentinelZero_B26
    WHERE ApplyStatus = N'APPLIED';

SELECT
    DB_NAME() AS DbName,
    @RunCount AS DistinctAppliedRunIds,
    @RunId AS RemediationRunId,
    (SELECT COUNT(*) FROM dbo.AUDIT_UserMasterOrgSentinelZero_B26 WHERE RemediationRunId = @RunId AND ApplyStatus = N'APPLIED') AS AppliedAuditRows,
    (SELECT MIN(AppliedAt) FROM dbo.AUDIT_UserMasterOrgSentinelZero_B26 WHERE RemediationRunId = @RunId) AS AppliedAt;

IF OBJECT_ID('tempdb..#Verify') IS NOT NULL DROP TABLE #Verify;
CREATE TABLE #Verify
(
    Seq int NOT NULL,
    Validation varchar(90) NOT NULL,
    Expected varchar(40) NOT NULL,
    Actual varchar(40) NOT NULL,
    Status varchar(10) NOT NULL
);

DECLARE @E0001Div int = (SELECT DivisionId FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0001');
DECLARE @E0006Div int = (SELECT DivisionId FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0006');
DECLARE @E0034Br  int = (SELECT BranchId FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0034');
DECLARE @E0034Div int = (SELECT DivisionId FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0034');
DECLARE @E0034Sub int = (SELECT SubDepartmentId FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0034');

DECLARE @Compliant int =
      CASE WHEN @E0001Div = 0 THEN 1 ELSE 0 END
    + CASE WHEN @E0006Div = 0 THEN 1 ELSE 0 END
    + CASE WHEN @E0034Br  = 0 THEN 1 ELSE 0 END
    + CASE WHEN @E0034Div = 0 THEN 1 ELSE 0 END
    + CASE WHEN @E0034Sub = 0 THEN 1 ELSE 0 END;
DECLARE @StillOld int =
      CASE WHEN @E0001Div = 4 THEN 1 ELSE 0 END
    + CASE WHEN @E0006Div = 4 THEN 1 ELSE 0 END
    + CASE WHEN @E0034Br  = 19 THEN 1 ELSE 0 END
    + CASE WHEN @E0034Div = 4 THEN 1 ELSE 0 END
    + CASE WHEN @E0034Sub = 110 THEN 1 ELSE 0 END;

DECLARE @DivErp int = (SELECT TRY_CONVERT(INT, Division_ID) FROM dbo.MST_Division WHERE Id = 4);
DECLARE @BrErp int = (SELECT TRY_CONVERT(INT, Branch_ID) FROM dbo.MST_Branch WHERE Id = 19);
DECLARE @SubErp int = (SELECT TRY_CONVERT(INT, SubDepartment_ID) FROM dbo.MST_SubDepartment WHERE Id = 110);

DECLARE @ErpCompliant int =
      CASE WHEN @E0001Div = @DivErp THEN 1 ELSE 0 END
    + CASE WHEN @E0006Div = @DivErp THEN 1 ELSE 0 END
    + CASE WHEN @E0034Br  = @BrErp  THEN 1 ELSE 0 END
    + CASE WHEN @E0034Div = @DivErp THEN 1 ELSE 0 END
    + CASE WHEN @E0034Sub = @SubErp THEN 1 ELSE 0 END;

/* Remaining internal-Id drift whose master ERP is 0 (B2.6 candidate definition) */
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

DECLARE @dsql nvarchar(max) = N'';
SELECT @dsql = @dsql + N'
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
EXEC sp_executesql @dsql;

DECLARE @RemainZeroDrift int = (
    SELECT COUNT(*) FROM #AllDrift WHERE IsFixture = 0 AND IsZeroTarget = 1);
DECLARE @RemainNonZeroDrift int = (
    SELECT COUNT(*) FROM #AllDrift
    WHERE IsFixture = 0 AND IsZeroTarget = 0
      AND MasterERPValue IS NOT NULL AND MasterERPValue <> 0 AND ErpKeyRowCount = 1);
DECLARE @FixtureDrift int = (SELECT COUNT(*) FROM #AllDrift WHERE IsFixture = 1);

DECLARE @AuditRows int = (
    SELECT COUNT(*) FROM dbo.AUDIT_UserMasterOrgSentinelZero_B26
    WHERE RemediationRunId = @RunId AND ApplyStatus = N'APPLIED');
DECLARE @AuditNewZero int = (
    SELECT COUNT(*) FROM dbo.AUDIT_UserMasterOrgSentinelZero_B26
    WHERE RemediationRunId = @RunId AND ApplyStatus = N'APPLIED' AND NewValue = 0);
DECLARE @AuditOldOk int = (
    SELECT COUNT(*) FROM dbo.AUDIT_UserMasterOrgSentinelZero_B26
    WHERE RemediationRunId = @RunId AND ApplyStatus = N'APPLIED'
      AND (
            (EmployeeCode = N'0001' AND ColumnName = N'DivisionId' AND OldValue = 4)
         OR (EmployeeCode = N'0006' AND ColumnName = N'DivisionId' AND OldValue = 4)
         OR (EmployeeCode = N'0034' AND ColumnName = N'BranchId' AND OldValue = 19)
         OR (EmployeeCode = N'0034' AND ColumnName = N'DivisionId' AND OldValue = 4)
         OR (EmployeeCode = N'0034' AND ColumnName = N'SubDepartmentId' AND OldValue = 110)
      ));

DECLARE @B23Applied int = 0;
DECLARE @B23StillErp int = 0;
IF OBJECT_ID(N'dbo.AUDIT_UserMasterOrgRemediation_B23', N'U') IS NOT NULL
BEGIN
    SELECT @B23Applied = COUNT(*)
    FROM dbo.AUDIT_UserMasterOrgRemediation_B23
    WHERE ApplyStatus = N'APPLIED';

    IF OBJECT_ID('tempdb..#B23Match') IS NOT NULL DROP TABLE #B23Match;
    CREATE TABLE #B23Match (Matched int NOT NULL);
    DECLARE @b23sql nvarchar(max) = N'';
    SELECT @b23sql = @b23sql + N'
    INSERT INTO #B23Match (Matched)
    SELECT SUM(CASE WHEN u.[' + ColumnName + N'] = a.NewValue THEN 1 ELSE 0 END)
    FROM dbo.AUDIT_UserMasterOrgRemediation_B23 AS a
    INNER JOIN dbo.MST_UserMaster AS u ON u.Id = a.UserMasterId
    WHERE a.ApplyStatus = N''APPLIED''
      AND a.ColumnName = N''' + ColumnName + N''';
    '
    FROM #OrgMap;
    EXEC sp_executesql @b23sql;
    SELECT @B23StillErp = ISNULL(SUM(Matched), 0) FROM #B23Match;
END;

DECLARE @M2668Plant int = (SELECT PlantId FROM dbo.MST_UserMaster WHERE EmployeeCode = N'M2668');

;WITH desig AS (
    SELECT
        u.Designation_ID AS StoredValue,
        CASE WHEN EXISTS (SELECT 1 FROM dbo.MST_Designation d WHERE d.Id = u.Designation_ID) THEN 1 ELSE 0 END AS MatchInternal,
        CASE WHEN EXISTS (SELECT 1 FROM dbo.MST_Designation d WHERE TRY_CONVERT(INT, d.Designation_ID) = u.Designation_ID) THEN 1 ELSE 0 END AS MatchErp
    FROM dbo.MST_UserMaster AS u
)
SELECT
    SUM(CASE WHEN StoredValue IS NOT NULL AND StoredValue <> 0 AND MatchInternal = 0 AND MatchErp = 1 THEN 1 ELSE 0 END) AS DesignationErpCodeDrift
INTO #DesigDrift
FROM desig;

DECLARE @DesigErpDrift int = (SELECT DesignationErpCodeDrift FROM #DesigDrift);

INSERT INTO #Verify (Seq, Validation, Expected, Actual, Status)
VALUES
    (1,  N'0001.DivisionId = 0', N'0', CONVERT(varchar(12), @E0001Div), CASE WHEN @E0001Div = 0 THEN N'PASS' ELSE N'FAIL' END),
    (2,  N'0006.DivisionId = 0', N'0', CONVERT(varchar(12), @E0006Div), CASE WHEN @E0006Div = 0 THEN N'PASS' ELSE N'FAIL' END),
    (3,  N'0034.BranchId = 0', N'0', CONVERT(varchar(12), @E0034Br), CASE WHEN @E0034Br = 0 THEN N'PASS' ELSE N'FAIL' END),
    (4,  N'0034.DivisionId = 0', N'0', CONVERT(varchar(12), @E0034Div), CASE WHEN @E0034Div = 0 THEN N'PASS' ELSE N'FAIL' END),
    (5,  N'0034.SubDepartmentId = 0', N'0', CONVERT(varchar(12), @E0034Sub), CASE WHEN @E0034Sub = 0 THEN N'PASS' ELSE N'FAIL' END),
    (6,  N'Contract-compliant mappings', N'5', CONVERT(varchar(12), @Compliant), CASE WHEN @Compliant = 5 THEN N'PASS' ELSE N'FAIL' END),
    (7,  N'Still old values', N'0', CONVERT(varchar(12), @StillOld), CASE WHEN @StillOld = 0 THEN N'PASS' ELSE N'FAIL' END),
    (8,  N'ERP-domain compliant', N'5', CONVERT(varchar(12), @ErpCompliant), CASE WHEN @ErpCompliant = 5 THEN N'PASS' ELSE N'FAIL' END),
    (9,  N'Remaining B2.6 zero-target drift', N'0', CONVERT(varchar(12), @RemainZeroDrift), CASE WHEN @RemainZeroDrift = 0 THEN N'PASS' ELSE N'FAIL' END),
    (10, N'Remaining approved non-zero drift (B2.3 class)', N'0', CONVERT(varchar(12), @RemainNonZeroDrift), CASE WHEN @RemainNonZeroDrift = 0 THEN N'PASS' ELSE N'FAIL' END),
    (11, N'Fixture drift instances unchanged class', N'32', CONVERT(varchar(12), @FixtureDrift), CASE WHEN @FixtureDrift = 32 THEN N'PASS' ELSE N'FAIL' END),
    (12, N'B2.3 APPLIED audit rows', N'115', CONVERT(varchar(12), @B23Applied), CASE WHEN @B23Applied = 115 THEN N'PASS' ELSE N'FAIL' END),
    (13, N'B2.3 mappings still equal NewValue', N'115', CONVERT(varchar(12), @B23StillErp), CASE WHEN @B23StillErp = 115 THEN N'PASS' ELSE N'FAIL' END),
    (14, N'Distinct B2.6 APPLIED run ids', N'1', CONVERT(varchar(12), @RunCount), CASE WHEN @RunCount = 1 THEN N'PASS' ELSE N'FAIL' END),
    (15, N'B2.6 APPLIED audit rows', N'5', CONVERT(varchar(12), @AuditRows), CASE WHEN @AuditRows = 5 THEN N'PASS' ELSE N'FAIL' END),
    (16, N'B2.6 NewValue = 0 rows', N'5', CONVERT(varchar(12), @AuditNewZero), CASE WHEN @AuditNewZero = 5 THEN N'PASS' ELSE N'FAIL' END),
    (17, N'B2.6 OldValue exact internals', N'5', CONVERT(varchar(12), @AuditOldOk), CASE WHEN @AuditOldOk = 5 THEN N'PASS' ELSE N'FAIL' END),
    (18, N'M2668.PlantId remains 3277', N'3277', CONVERT(varchar(12), @M2668Plant), CASE WHEN @M2668Plant = 3277 THEN N'PASS' ELSE N'FAIL' END),
    (19, N'Designation ERP-code drift', N'0', CONVERT(varchar(12), @DesigErpDrift), CASE WHEN @DesigErpDrift = 0 THEN N'PASS' ELSE N'FAIL' END);

DECLARE @FailCount int = (SELECT COUNT(*) FROM #Verify WHERE Status = N'FAIL');
DECLARE @PostStatus varchar(10) = CASE WHEN @FailCount = 0 THEN N'PASS' ELSE N'FAIL' END;

SELECT Validation, Expected, Actual, Status
FROM #Verify
ORDER BY Seq;

SELECT
    @Compliant AS CurrentEqualsNew,
    @StillOld AS CurrentStillOld,
    @ErpCompliant AS ErpDomainCompliant,
    @RemainZeroDrift AS RemainingB26CandidateDrift,
    @RemainNonZeroDrift AS RemainingApprovedNonZeroDrift,
    @FixtureDrift AS FixtureDriftInstances,
    @FailCount AS FailedAssertions,
    @PostStatus AS PostApplyStatus;

SELECT UserMasterId, EmployeeCode, ColumnName, OldValue, NewValue,
       TargetMasterTable, TargetMasterInternalId, TargetERPValue, ApplyStatus, AppliedAt
FROM dbo.AUDIT_UserMasterOrgSentinelZero_B26
WHERE RemediationRunId = @RunId
ORDER BY EmployeeCode, ColumnName;

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

SELECT CASE WHEN @PostStatus = N'PASS'
            THEN N'POST-APPLY STATUS: PASS'
            ELSE N'POST-APPLY STATUS: FAIL' END AS PostApplyBanner;

PRINT CASE WHEN @PostStatus = N'PASS'
           THEN N'POST-APPLY STATUS: PASS'
           ELSE N'POST-APPLY STATUS: FAIL' END;
