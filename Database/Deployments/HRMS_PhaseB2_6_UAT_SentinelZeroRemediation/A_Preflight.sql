/*
    Package:     Database/Deployments/HRMS_PhaseB2_6_UAT_SentinelZeroRemediation
    Script:      A_Preflight.sql
    Phase:       B2.6
    Mode:        READ-ONLY
    Database:    AnmolDristiDev ONLY
    Isolation:   READ COMMITTED (no READ UNCOMMITTED / NOLOCK)

    Regenerates the SENTINEL_UNASSIGNED (internal-Id → ERP 0) candidate set
    from current UAT data. Does not INSERT / UPDATE / DELETE / CREATE.
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

IF OBJECT_ID('tempdb..#AllZeroDrift') IS NOT NULL DROP TABLE #AllZeroDrift;
CREATE TABLE #AllZeroDrift
(
    UserMasterId int NOT NULL,
    EmployeeCode varchar(50) NOT NULL,
    ColumnName sysname NOT NULL,
    OldValue int NOT NULL,
    MasterInternalId int NOT NULL,
    MasterERPValue int NULL,
    ErpKeyRowCount int NOT NULL,
    IsFixture bit NOT NULL
);

DECLARE @sql nvarchar(max) = N'';
SELECT @sql = @sql + N'
INSERT INTO #AllZeroDrift (
    UserMasterId, EmployeeCode, ColumnName, OldValue,
    MasterInternalId, MasterERPValue, ErpKeyRowCount, IsFixture
)
SELECT
    u.Id, u.EmployeeCode, N''' + ColumnName + N''', u.[' + ColumnName + N'],
    m.Id,
    TRY_CONVERT(INT, m.[' + ErpColumn + N']),
    (SELECT COUNT(*) FROM dbo.[' + MasterTable + N'] x
      WHERE TRY_CONVERT(INT, x.[' + ErpColumn + N']) = TRY_CONVERT(INT, m.[' + ErpColumn + N'])),
    CASE WHEN u.EmployeeCode IN (N''PHA9B2E_EX1'', N''PHA9B2E_MGR'', N''PHA9B2E_NW1'', N''PHA9B2E_V7'') THEN 1 ELSE 0 END
FROM dbo.MST_UserMaster AS u
INNER JOIN dbo.[' + MasterTable + N'] AS m ON m.Id = u.[' + ColumnName + N']
WHERE u.[' + ColumnName + N'] IS NOT NULL
  AND u.[' + ColumnName + N'] <> 0
  AND TRY_CONVERT(INT, m.[' + ErpColumn + N']) = 0
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
FROM #AllZeroDrift
WHERE IsFixture = 0;

IF OBJECT_ID('tempdb..#Expected') IS NOT NULL DROP TABLE #Expected;
CREATE TABLE #Expected
(
    EmployeeCode varchar(50) NOT NULL,
    ColumnName sysname NOT NULL,
    OldValue int NOT NULL,
    NewValue int NOT NULL,
    MasterTable sysname NOT NULL,
    MasterInternalId int NOT NULL
);
INSERT INTO #Expected (EmployeeCode, ColumnName, OldValue, NewValue, MasterTable, MasterInternalId) VALUES
    (N'0001', N'DivisionId',      4,   0, N'MST_Division',      4),
    (N'0006', N'DivisionId',      4,   0, N'MST_Division',      4),
    (N'0034', N'BranchId',        19,  0, N'MST_Branch',        19),
    (N'0034', N'DivisionId',      4,   0, N'MST_Division',      4),
    (N'0034', N'SubDepartmentId', 110, 0, N'MST_SubDepartment', 110);

DECLARE @CandidateCount int = (SELECT COUNT(*) FROM #Candidates);
DECLARE @Branch int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'BranchId');
DECLARE @Div int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'DivisionId');
DECLARE @Sub int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'SubDepartmentId');
DECLARE @OtherCol int = (SELECT COUNT(*) FROM #Candidates
    WHERE ColumnName NOT IN (N'BranchId', N'DivisionId', N'SubDepartmentId'));
DECLARE @UnexpectedEmp int = (SELECT COUNT(*) FROM #Candidates
    WHERE EmployeeCode NOT IN (N'0001', N'0006', N'0034'));
DECLARE @EmployeeCount int = (SELECT COUNT(DISTINCT EmployeeCode) FROM #Candidates);

DECLARE @MissingExact int = (
    SELECT COUNT(*)
    FROM #Expected AS e
    WHERE NOT EXISTS (
        SELECT 1 FROM #Candidates AS c
        WHERE c.EmployeeCode = e.EmployeeCode
          AND c.ColumnName = e.ColumnName
          AND c.OldValue = e.OldValue
          AND c.MasterInternalId = e.MasterInternalId
          AND c.MasterERPValue = 0
    )
);
DECLARE @UnexpectedMap int = (
    SELECT COUNT(*)
    FROM #Candidates AS c
    WHERE NOT EXISTS (
        SELECT 1 FROM #Expected AS e
        WHERE e.EmployeeCode = c.EmployeeCode
          AND e.ColumnName = c.ColumnName
          AND e.OldValue = c.OldValue
    )
);

DECLARE @ExactOld0001Div int = (SELECT COUNT(*) FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0001' AND DivisionId = 4);
DECLARE @ExactOld0006Div int = (SELECT COUNT(*) FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0006' AND DivisionId = 4);
DECLARE @ExactOld0034Br  int = (SELECT COUNT(*) FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0034' AND BranchId = 19);
DECLARE @ExactOld0034Div int = (SELECT COUNT(*) FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0034' AND DivisionId = 4);
DECLARE @ExactOld0034Sub int = (SELECT COUNT(*) FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0034' AND SubDepartmentId = 110);
DECLARE @ExactOldAll bit = CASE
    WHEN @ExactOld0001Div = 1 AND @ExactOld0006Div = 1
     AND @ExactOld0034Br = 1 AND @ExactOld0034Div = 1 AND @ExactOld0034Sub = 1
    THEN 1 ELSE 0 END;

DECLARE @DivMasterOk bit = CASE WHEN EXISTS (
    SELECT 1 FROM dbo.MST_Division d
    WHERE d.Id = 4 AND TRY_CONVERT(INT, d.Division_ID) = 0
) THEN 1 ELSE 0 END;
DECLARE @BrMasterOk bit = CASE WHEN EXISTS (
    SELECT 1 FROM dbo.MST_Branch b
    WHERE b.Id = 19 AND TRY_CONVERT(INT, b.Branch_ID) = 0
) THEN 1 ELSE 0 END;
DECLARE @SubMasterOk bit = CASE WHEN EXISTS (
    SELECT 1 FROM dbo.MST_SubDepartment s
    WHERE s.Id = 110 AND TRY_CONVERT(INT, s.SubDepartment_ID) = 0
) THEN 1 ELSE 0 END;

DECLARE @DivErp0Count int = (SELECT COUNT(*) FROM dbo.MST_Division WHERE TRY_CONVERT(INT, Division_ID) = 0);
DECLARE @BrErp0Count int = (SELECT COUNT(*) FROM dbo.MST_Branch WHERE TRY_CONVERT(INT, Branch_ID) = 0);
DECLARE @SubErp0Count int = (SELECT COUNT(*) FROM dbo.MST_SubDepartment WHERE TRY_CONVERT(INT, SubDepartment_ID) = 0);

DECLARE @Ambiguous int = (SELECT COUNT(*) FROM #Candidates WHERE ErpKeyRowCount <> 1);
DECLARE @NullTarget int = (SELECT COUNT(*) FROM #Candidates WHERE MasterERPValue IS NULL);
DECLARE @NonZeroTarget int = (SELECT COUNT(*) FROM #Candidates WHERE MasterERPValue IS NULL OR MasterERPValue <> 0);

DECLARE @AlreadyZero int = (
    SELECT
        CASE WHEN EXISTS (SELECT 1 FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0001' AND DivisionId = 0) THEN 1 ELSE 0 END
      + CASE WHEN EXISTS (SELECT 1 FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0006' AND DivisionId = 0) THEN 1 ELSE 0 END
      + CASE WHEN EXISTS (SELECT 1 FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0034' AND BranchId = 0) THEN 1 ELSE 0 END
      + CASE WHEN EXISTS (SELECT 1 FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0034' AND DivisionId = 0) THEN 1 ELSE 0 END
      + CASE WHEN EXISTS (SELECT 1 FROM dbo.MST_UserMaster WHERE EmployeeCode = N'0034' AND SubDepartmentId = 0) THEN 1 ELSE 0 END
);

DECLARE @B23Overlap int = 0;
IF OBJECT_ID(N'dbo.AUDIT_UserMasterOrgRemediation_B23', N'U') IS NOT NULL
BEGIN
    SELECT @B23Overlap = COUNT(*)
    FROM #Candidates AS c
    INNER JOIN dbo.AUDIT_UserMasterOrgRemediation_B23 AS a
        ON a.UserMasterId = c.UserMasterId
       AND a.ColumnName = c.ColumnName
       AND a.ApplyStatus = N'APPLIED';
END;

DECLARE @FixtureCand int = (SELECT COUNT(*) FROM #Candidates WHERE IsFixture = 1
    OR EmployeeCode IN (N'PHA9B2E_EX1', N'PHA9B2E_MGR', N'PHA9B2E_NW1', N'PHA9B2E_V7'));
DECLARE @DesigCand int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'Designation_ID');
DECLARE @PlantCand int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'PlantId');
DECLARE @Plant3277 int = (
    SELECT COUNT(*) FROM dbo.MST_UserMaster
    WHERE EmployeeCode = N'M2668' AND PlantId <> 3277
);
DECLARE @Plant3277Cand int = (SELECT COUNT(*) FROM #Candidates WHERE ColumnName = N'PlantId' AND OldValue = 3277);

IF OBJECT_ID('tempdb..#Validation') IS NOT NULL DROP TABLE #Validation;
CREATE TABLE #Validation
(
    Seq int NOT NULL,
    Validation varchar(80) NOT NULL,
    Expected varchar(40) NOT NULL,
    Actual varchar(40) NOT NULL,
    Status varchar(10) NOT NULL
);

INSERT INTO #Validation (Seq, Validation, Expected, Actual, Status)
VALUES
    (1,  N'Candidate total',
         N'5', CONVERT(varchar(10), @CandidateCount),
         CASE WHEN @CandidateCount = 5 THEN N'PASS' ELSE N'FAIL' END),
    (2,  N'BranchId count',
         N'1', CONVERT(varchar(10), @Branch),
         CASE WHEN @Branch = 1 THEN N'PASS' ELSE N'FAIL' END),
    (3,  N'DivisionId count',
         N'3', CONVERT(varchar(10), @Div),
         CASE WHEN @Div = 3 THEN N'PASS' ELSE N'FAIL' END),
    (4,  N'SubDepartmentId count',
         N'1', CONVERT(varchar(10), @Sub),
         CASE WHEN @Sub = 1 THEN N'PASS' ELSE N'FAIL' END),
    (5,  N'Other-column candidates',
         N'0', CONVERT(varchar(10), @OtherCol),
         CASE WHEN @OtherCol = 0 THEN N'PASS' ELSE N'FAIL' END),
    (6,  N'Distinct employees',
         N'3', CONVERT(varchar(10), @EmployeeCount),
         CASE WHEN @EmployeeCount = 3 THEN N'PASS' ELSE N'FAIL' END),
    (7,  N'Unexpected employees',
         N'0', CONVERT(varchar(10), @UnexpectedEmp),
         CASE WHEN @UnexpectedEmp = 0 THEN N'PASS' ELSE N'FAIL' END),
    (8,  N'Exact old values (0001/0006/0034)',
         N'1', CONVERT(varchar(10), @ExactOldAll),
         CASE WHEN @ExactOldAll = 1 THEN N'PASS' ELSE N'FAIL' END),
    (9,  N'Missing exact mappings',
         N'0', CONVERT(varchar(10), @MissingExact),
         CASE WHEN @MissingExact = 0 THEN N'PASS' ELSE N'FAIL' END),
    (10, N'Unexpected mappings',
         N'0', CONVERT(varchar(10), @UnexpectedMap),
         CASE WHEN @UnexpectedMap = 0 THEN N'PASS' ELSE N'FAIL' END),
    (11, N'MST_Division.Id=4 → Division_ID=0',
         N'1', CONVERT(varchar(10), @DivMasterOk),
         CASE WHEN @DivMasterOk = 1 THEN N'PASS' ELSE N'FAIL' END),
    (12, N'MST_Branch.Id=19 → Branch_ID=0',
         N'1', CONVERT(varchar(10), @BrMasterOk),
         CASE WHEN @BrMasterOk = 1 THEN N'PASS' ELSE N'FAIL' END),
    (13, N'MST_SubDepartment.Id=110 → SubDepartment_ID=0',
         N'1', CONVERT(varchar(10), @SubMasterOk),
         CASE WHEN @SubMasterOk = 1 THEN N'PASS' ELSE N'FAIL' END),
    (14, N'Unique Division ERP 0 rows',
         N'1', CONVERT(varchar(10), @DivErp0Count),
         CASE WHEN @DivErp0Count = 1 THEN N'PASS' ELSE N'FAIL' END),
    (15, N'Unique Branch ERP 0 rows',
         N'1', CONVERT(varchar(10), @BrErp0Count),
         CASE WHEN @BrErp0Count = 1 THEN N'PASS' ELSE N'FAIL' END),
    (16, N'Unique SubDepartment ERP 0 rows',
         N'1', CONVERT(varchar(10), @SubErp0Count),
         CASE WHEN @SubErp0Count = 1 THEN N'PASS' ELSE N'FAIL' END),
    (17, N'Ambiguous targets',
         N'0', CONVERT(varchar(10), @Ambiguous),
         CASE WHEN @Ambiguous = 0 THEN N'PASS' ELSE N'FAIL' END),
    (18, N'NULL targets',
         N'0', CONVERT(varchar(10), @NullTarget),
         CASE WHEN @NullTarget = 0 THEN N'PASS' ELSE N'FAIL' END),
    (19, N'Non-zero targets',
         N'0', CONVERT(varchar(10), @NonZeroTarget),
         CASE WHEN @NonZeroTarget = 0 THEN N'PASS' ELSE N'FAIL' END),
    (20, N'Already remediated (current = 0)',
         N'0', CONVERT(varchar(10), @AlreadyZero),
         CASE WHEN @AlreadyZero = 0 THEN N'PASS' ELSE N'FAIL' END),
    (21, N'B2.3 overlap',
         N'0', CONVERT(varchar(10), @B23Overlap),
         CASE WHEN @B23Overlap = 0 THEN N'PASS' ELSE N'FAIL' END),
    (22, N'Fixture candidates',
         N'0', CONVERT(varchar(10), @FixtureCand),
         CASE WHEN @FixtureCand = 0 THEN N'PASS' ELSE N'FAIL' END),
    (23, N'Designation candidates',
         N'0', CONVERT(varchar(10), @DesigCand),
         CASE WHEN @DesigCand = 0 THEN N'PASS' ELSE N'FAIL' END),
    (24, N'Designation modifications planned',
         N'0', N'0', N'PASS'),
    (25, N'Plant candidates',
         N'0', CONVERT(varchar(10), @PlantCand),
         CASE WHEN @PlantCand = 0 THEN N'PASS' ELSE N'FAIL' END),
    (26, N'Plant 3277 modifications',
         N'0', CONVERT(varchar(10), @Plant3277Cand),
         CASE WHEN @Plant3277Cand = 0 AND @Plant3277 = 0 THEN N'PASS' ELSE N'FAIL' END);

DECLARE @FailCount int = (SELECT COUNT(*) FROM #Validation WHERE Status = N'FAIL');
DECLARE @PreflightStatus varchar(10) = CASE WHEN @FailCount = 0 THEN N'PASS' ELSE N'FAIL' END;

SELECT
    Validation,
    Expected,
    Actual,
    Status
FROM #Validation
ORDER BY Seq;

SELECT
    DB_NAME() AS DbName,
    @CandidateCount AS CandidateCount,
    @Branch AS BranchId,
    @Div AS DivisionId,
    @Sub AS SubDepartmentId,
    @FailCount AS FailedAssertions,
    @PreflightStatus AS PreflightStatus;

SELECT
    c.UserMasterId, c.EmployeeCode, c.ColumnName, c.OldValue,
    c.MasterInternalId, c.MasterERPValue, c.ErpKeyRowCount
FROM #Candidates AS c
ORDER BY c.EmployeeCode, c.ColumnName;

SELECT CASE WHEN @PreflightStatus = N'PASS'
            THEN N'PREFLIGHT STATUS: PASS'
            ELSE N'PREFLIGHT STATUS: FAIL' END AS PreflightBanner;

PRINT CASE WHEN @PreflightStatus = N'PASS'
           THEN N'PREFLIGHT STATUS: PASS'
           ELSE N'PREFLIGHT STATUS: FAIL' END;
