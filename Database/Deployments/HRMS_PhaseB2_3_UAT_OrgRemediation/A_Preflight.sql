/*
    Package:     Database/Deployments/HRMS_PhaseB2_3_UAT_OrgRemediation
    Script:      A_Preflight.sql
    Phase:       B2.3
    Mode:        READ-ONLY
    Database:    AnmolDristiDev ONLY
    Isolation:   READ COMMITTED (no READ UNCOMMITTED / NOLOCK)

    Regenerates the INTERNAL_ID_DRIFT candidate set and reports STOP conditions.
    Does not INSERT / UPDATE / DELETE / CREATE.
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

IF OBJECT_ID('tempdb..#Candidates') IS NOT NULL DROP TABLE #Candidates;
SELECT *
INTO #Candidates
FROM #AllDrift
WHERE IsFixture = 0
  AND IsZeroTarget = 0
  AND MasterERPValue IS NOT NULL
  AND MasterERPValue <> 0
  AND ErpKeyRowCount = 1;

DECLARE @CandidateCount int = (SELECT COUNT(*) FROM #Candidates);
DECLARE @Branch int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'BranchId');
DECLARE @Dept int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'DepartmentId');
DECLARE @Grade int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'GradeId');
DECLARE @Plant int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'PlantId');
DECLARE @Region int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'RegionId');
DECLARE @Div int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'DivisionId');
DECLARE @Cat int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'CategoryId');
DECLARE @Sub int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'SubDepartmentId');
DECLARE @FixtureCand int = (SELECT COUNT(*) FROM #Candidates WHERE IsFixture = 1
    OR EmployeeCode IN (N'PHA9B2E_EX1', N'PHA9B2E_MGR', N'PHA9B2E_NW1', N'PHA9B2E_V7'));
DECLARE @ZeroCand int = (SELECT COUNT(*) FROM #Candidates WHERE IsZeroTarget = 1 OR MasterERPValue = 0);
DECLARE @DesigCand int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'Designation_ID');
DECLARE @BadCol int = (SELECT COUNT(*) FROM #Candidates
    WHERE ColumnName NOT IN (N'BranchId', N'DepartmentId', N'GradeId', N'PlantId',
                             N'RegionId', N'DivisionId', N'CategoryId', N'SubDepartmentId'));
DECLARE @Ambiguous int = (SELECT COUNT(*) FROM #Candidates WHERE ErpKeyRowCount <> 1);
DECLARE @NullTarget int = (SELECT COUNT(*) FROM #Candidates WHERE MasterERPValue IS NULL);
DECLARE @Plant3277 int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'PlantId' AND OldValue = 3277);

DECLARE @ZeroExcluded int = (
    SELECT COUNT(*) FROM #AllDrift WHERE IsFixture = 0 AND IsZeroTarget = 1);
DECLARE @FixtureInst int = (SELECT COUNT(*) FROM #AllDrift WHERE IsFixture = 1);

DECLARE @DistributionOk bit = CASE
    WHEN @CandidateCount = 115 AND @Branch = 17 AND @Dept = 18 AND @Grade = 6
     AND @Plant = 18 AND @Region = 18 AND @Div = 15 AND @Cat = 6 AND @Sub = 17
    THEN 1 ELSE 0 END;

DECLARE @ExclusionOk bit = CASE
    WHEN @FixtureCand = 0 AND @ZeroCand = 0 AND @DesigCand = 0 AND @BadCol = 0
     AND @Ambiguous = 0 AND @NullTarget = 0 AND @Plant3277 = 0
     AND @ZeroExcluded = 5 AND @FixtureInst = 32
    THEN 1 ELSE 0 END;

DECLARE @PreflightStatus varchar(10) =
    CASE WHEN @DistributionOk = 1 AND @ExclusionOk = 1 THEN N'PASS' ELSE N'STOP' END;

SELECT
    DB_NAME() AS DbName,
    @CandidateCount AS CandidateCount,
    @Branch AS BranchId,
    @Dept AS DepartmentId,
    @Grade AS GradeId,
    @Plant AS PlantId,
    @Region AS RegionId,
    @Div AS DivisionId,
    @Cat AS CategoryId,
    @Sub AS SubDepartmentId,
    @FixtureCand AS FixtureCandidates,
    @ZeroCand AS ZeroTargetCandidates,
    @DesigCand AS DesignationCandidates,
    @BadCol AS IneligibleColumns,
    @Ambiguous AS AmbiguousTargets,
    @NullTarget AS NullTargets,
    @Plant3277 AS Plant3277Conversions,
    @ZeroExcluded AS ZeroTargetExclusions,
    @FixtureInst AS FixtureInstances,
    @PreflightStatus AS PreflightStatus;

SELECT UserMasterId, EmployeeCode, ColumnName, OldValue, MasterInternalId, MasterERPValue, ErpKeyRowCount
FROM #Candidates
ORDER BY EmployeeCode, ColumnName;

SELECT UserMasterId, EmployeeCode, ColumnName, OldValue, MasterInternalId, MasterERPValue
FROM #AllDrift
WHERE IsFixture = 0 AND IsZeroTarget = 1
ORDER BY EmployeeCode, ColumnName;

SELECT DISTINCT EmployeeCode
FROM #AllDrift
WHERE IsFixture = 1
ORDER BY EmployeeCode;
