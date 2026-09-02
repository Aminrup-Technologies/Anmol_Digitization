/*
    Package:     Database/Deployments/HRMS_PhaseB2_3_UAT_OrgRemediation
    Script:      B_ControlledApply.sql
    Phase:       B2.3 — permanent UAT apply
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN

    BEGIN TRAN
      validate 115 mappings
      UPDLOCK candidate UserMaster rows
      insert PENDING audit
      8 column-level guarded UPDATEs
      post-apply validation
      mark audit APPLIED
    COMMIT only if every assertion passes.

    Any failure → ROLLBACK entire transaction (no partial apply).
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

IF OBJECT_ID(N'dbo.AUDIT_UserMasterOrgRemediation_B23', N'U') IS NULL
    THROW 56203, N'ABORT: audit table missing. Run 000_CreateAuditTable.sql first.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

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

    IF OBJECT_ID('tempdb..#Mapping') IS NOT NULL DROP TABLE #Mapping;
    CREATE TABLE #Mapping
    (
        RemediationRunId varchar(40) NOT NULL,
        UserMasterId int NOT NULL,
        EmployeeCode varchar(50) NOT NULL,
        ColumnName sysname NOT NULL,
        OldValue int NOT NULL,
        NewValue int NOT NULL,
        MasterTable sysname NOT NULL,
        MasterInternalId int NOT NULL,
        MasterERPValue int NOT NULL,
        Classification varchar(40) NOT NULL,
        IsFixture bit NOT NULL,
        IsZeroTarget bit NOT NULL,
        ValidationTimestamp datetime2 NOT NULL,
        GuardOldValue bit NOT NULL DEFAULT (0),
        GuardMasterIdentity bit NOT NULL DEFAULT (0),
        GuardErpTarget bit NOT NULL DEFAULT (0),
        GuardTargetValidity bit NOT NULL DEFAULT (0),
        GuardFixture bit NOT NULL DEFAULT (0),
        GuardColumnContract bit NOT NULL DEFAULT (0),
        GuardPass bit NOT NULL DEFAULT (0)
    );

    DECLARE @RunId varchar(40) = N'B23-APPLY-' + CONVERT(varchar(19), SYSDATETIME(), 126);
    DECLARE @Ts datetime2 = SYSDATETIME();
    DECLARE @sql nvarchar(max) = N'';

    SELECT @sql = @sql + N'
    INSERT INTO #Mapping (
        RemediationRunId, UserMasterId, EmployeeCode, ColumnName,
        OldValue, NewValue, MasterTable, MasterInternalId, MasterERPValue,
        Classification, IsFixture, IsZeroTarget, ValidationTimestamp
    )
    SELECT
        @RunId, u.Id, u.EmployeeCode, N''' + ColumnName + N''',
        u.[' + ColumnName + N'],
        TRY_CONVERT(INT, m.[' + ErpColumn + N']),
        N''' + MasterTable + N''',
        m.Id,
        TRY_CONVERT(INT, m.[' + ErpColumn + N']),
        N''INTERNAL_ID_DRIFT'',
        0, 0, @Ts
    FROM dbo.MST_UserMaster AS u
    INNER JOIN dbo.[' + MasterTable + N'] AS m ON m.Id = u.[' + ColumnName + N']
    WHERE u.[' + ColumnName + N'] IS NOT NULL
      AND u.[' + ColumnName + N'] <> 0
      AND u.EmployeeCode NOT IN (N''PHA9B2E_EX1'', N''PHA9B2E_MGR'', N''PHA9B2E_NW1'', N''PHA9B2E_V7'')
      AND TRY_CONVERT(INT, m.[' + ErpColumn + N']) IS NOT NULL
      AND TRY_CONVERT(INT, m.[' + ErpColumn + N']) <> 0
      AND (SELECT COUNT(*) FROM dbo.[' + MasterTable + N'] x
            WHERE TRY_CONVERT(INT, x.[' + ErpColumn + N']) = TRY_CONVERT(INT, m.[' + ErpColumn + N'])) = 1
      AND NOT EXISTS (
            SELECT 1 FROM dbo.[' + MasterTable + N'] e
            WHERE TRY_CONVERT(INT, e.[' + ErpColumn + N']) = u.[' + ColumnName + N']
          );
    '
    FROM #OrgMap;

    EXEC sp_executesql @sql, N'@RunId varchar(40), @Ts datetime2', @RunId = @RunId, @Ts = @Ts;

    DECLARE @MapCount int = (SELECT COUNT(*) FROM #Mapping);
    DECLARE @CBranch int = (SELECT COUNT(*) FROM #Mapping WHERE ColumnName = N'BranchId');
    DECLARE @CDept int = (SELECT COUNT(*) FROM #Mapping WHERE ColumnName = N'DepartmentId');
    DECLARE @CGrade int = (SELECT COUNT(*) FROM #Mapping WHERE ColumnName = N'GradeId');
    DECLARE @CPlant int = (SELECT COUNT(*) FROM #Mapping WHERE ColumnName = N'PlantId');
    DECLARE @CRegion int = (SELECT COUNT(*) FROM #Mapping WHERE ColumnName = N'RegionId');
    DECLARE @CDiv int = (SELECT COUNT(*) FROM #Mapping WHERE ColumnName = N'DivisionId');
    DECLARE @CCat int = (SELECT COUNT(*) FROM #Mapping WHERE ColumnName = N'CategoryId');
    DECLARE @CSub int = (SELECT COUNT(*) FROM #Mapping WHERE ColumnName = N'SubDepartmentId');

    IF @MapCount <> 115
        OR @CBranch <> 17 OR @CDept <> 18 OR @CGrade <> 6 OR @CPlant <> 18
        OR @CRegion <> 18 OR @CDiv <> 15 OR @CCat <> 6 OR @CSub <> 17
    BEGIN
        DECLARE @distMsg nvarchar(400) =
            N'STOP: candidate count/distribution mismatch. Count='
            + CONVERT(varchar(10), @MapCount)
            + N' B=' + CONVERT(varchar(10), @CBranch)
            + N' D=' + CONVERT(varchar(10), @CDept)
            + N' G=' + CONVERT(varchar(10), @CGrade)
            + N' P=' + CONVERT(varchar(10), @CPlant)
            + N' R=' + CONVERT(varchar(10), @CRegion)
            + N' V=' + CONVERT(varchar(10), @CDiv)
            + N' C=' + CONVERT(varchar(10), @CCat)
            + N' S=' + CONVERT(varchar(10), @CSub);
        THROW 56210, @distMsg, 1;
    END;

    IF EXISTS (SELECT 1 FROM #Mapping WHERE NewValue = 0 OR NewValue IS NULL OR IsZeroTarget = 1)
        THROW 56211, N'STOP: zero/NULL target leaked into mapping.', 1;

    IF EXISTS (SELECT 1 FROM #Mapping WHERE EmployeeCode IN (N'PHA9B2E_EX1', N'PHA9B2E_MGR', N'PHA9B2E_NW1', N'PHA9B2E_V7') OR IsFixture = 1)
        THROW 56212, N'STOP: fixture leaked into mapping.', 1;

    IF EXISTS (SELECT 1 FROM #Mapping WHERE ColumnName = N'Designation_ID'
        OR ColumnName NOT IN (N'BranchId', N'DepartmentId', N'GradeId', N'PlantId',
                              N'RegionId', N'DivisionId', N'CategoryId', N'SubDepartmentId'))
        THROW 56213, N'STOP: ineligible column in mapping.', 1;

    IF EXISTS (SELECT 1 FROM #Mapping WHERE ColumnName = N'PlantId' AND OldValue = 3277)
        THROW 56214, N'STOP: PlantId=3277 conversion candidate present.', 1;

    /* Lock only the distinct candidate UserMaster rows */
    IF OBJECT_ID('tempdb..#LockedUsers') IS NOT NULL DROP TABLE #LockedUsers;
    SELECT
        u.Id AS UserMasterId, u.EmployeeCode,
        u.BranchId, u.DepartmentId, u.GradeId, u.PlantId,
        u.RegionId, u.DivisionId, u.CategoryId, u.SubDepartmentId, u.Designation_ID
    INTO #LockedUsers
    FROM dbo.MST_UserMaster AS u WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
    WHERE u.Id IN (SELECT DISTINCT UserMasterId FROM #Mapping);

    IF (SELECT COUNT(*) FROM #LockedUsers) <> (SELECT COUNT(DISTINCT UserMasterId) FROM #Mapping)
        THROW 56215, N'STOP: could not lock every candidate UserMaster row.', 1;

    /* Guards against locked current state + re-joined masters */
    UPDATE mp
    SET
        GuardOldValue = CASE
            WHEN (mp.ColumnName = N'BranchId'        AND lu.BranchId = mp.OldValue)
              OR (mp.ColumnName = N'DepartmentId'    AND lu.DepartmentId = mp.OldValue)
              OR (mp.ColumnName = N'GradeId'         AND lu.GradeId = mp.OldValue)
              OR (mp.ColumnName = N'PlantId'         AND lu.PlantId = mp.OldValue)
              OR (mp.ColumnName = N'RegionId'        AND lu.RegionId = mp.OldValue)
              OR (mp.ColumnName = N'DivisionId'      AND lu.DivisionId = mp.OldValue)
              OR (mp.ColumnName = N'CategoryId'      AND lu.CategoryId = mp.OldValue)
              OR (mp.ColumnName = N'SubDepartmentId' AND lu.SubDepartmentId = mp.OldValue)
            THEN 1 ELSE 0 END,
        GuardFixture = CASE
            WHEN lu.EmployeeCode NOT IN (N'PHA9B2E_EX1', N'PHA9B2E_MGR', N'PHA9B2E_NW1', N'PHA9B2E_V7')
             AND mp.IsFixture = 0 THEN 1 ELSE 0 END,
        GuardColumnContract = CASE
            WHEN mp.ColumnName IN (N'BranchId', N'DepartmentId', N'GradeId', N'PlantId',
                                   N'RegionId', N'DivisionId', N'CategoryId', N'SubDepartmentId')
            THEN 1 ELSE 0 END
    FROM #Mapping AS mp
    INNER JOIN #LockedUsers AS lu ON lu.UserMasterId = mp.UserMasterId;

    DECLARE @gsql nvarchar(max) = N'';
    SELECT @gsql = @gsql + N'
    UPDATE mp
    SET
        GuardMasterIdentity = CASE
            WHEN EXISTS (
                SELECT 1 FROM dbo.[' + MasterTable + N'] m
                WHERE m.Id = mp.OldValue
                  AND m.Id = mp.MasterInternalId
                  AND mp.ColumnName = N''' + ColumnName + N'''
            ) THEN 1 ELSE 0 END,
        GuardErpTarget = CASE
            WHEN EXISTS (
                SELECT 1 FROM dbo.[' + MasterTable + N'] m
                WHERE m.Id = mp.MasterInternalId
                  AND mp.ColumnName = N''' + ColumnName + N'''
                  AND TRY_CONVERT(INT, m.[' + ErpColumn + N']) = mp.NewValue
                  AND TRY_CONVERT(INT, m.[' + ErpColumn + N']) = mp.MasterERPValue
            ) THEN 1 ELSE 0 END,
        GuardTargetValidity = CASE
            WHEN mp.NewValue IS NOT NULL AND mp.NewValue <> 0 AND mp.IsZeroTarget = 0
             AND (
                SELECT COUNT(*) FROM dbo.[' + MasterTable + N'] x
                WHERE TRY_CONVERT(INT, x.[' + ErpColumn + N']) = mp.NewValue
             ) = 1
            THEN 1 ELSE 0 END
    FROM #Mapping AS mp
    WHERE mp.ColumnName = N''' + ColumnName + N''';
    '
    FROM #OrgMap;
    EXEC sp_executesql @gsql;

    UPDATE #Mapping
    SET GuardPass = CASE
        WHEN GuardOldValue = 1 AND GuardMasterIdentity = 1 AND GuardErpTarget = 1
         AND GuardTargetValidity = 1 AND GuardFixture = 1 AND GuardColumnContract = 1
        THEN 1 ELSE 0 END;

    IF EXISTS (SELECT 1 FROM #Mapping WHERE GuardPass = 0)
        THROW 56216, N'STOP: one or more mapping guards failed (STALE_MAPPING or master drift).', 1;

    IF (SELECT COUNT(*) FROM #Mapping WHERE GuardPass = 1) <> 115
        THROW 56217, N'STOP: GuardPass count is not 115.', 1;

    /* Before-state: candidates + fixtures + known zero-target users */
    IF OBJECT_ID('tempdb..#BeforeState') IS NOT NULL DROP TABLE #BeforeState;
    SELECT
        u.Id AS UserMasterId, u.EmployeeCode,
        u.BranchId, u.DepartmentId, u.GradeId, u.PlantId,
        u.RegionId, u.DivisionId, u.CategoryId, u.SubDepartmentId, u.Designation_ID
    INTO #BeforeState
    FROM dbo.MST_UserMaster AS u WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
    WHERE u.Id IN (SELECT UserMasterId FROM #Mapping)
       OR u.EmployeeCode IN (
            N'PHA9B2E_EX1', N'PHA9B2E_MGR', N'PHA9B2E_NW1', N'PHA9B2E_V7',
            N'0001', N'0006', N'0034', N'M2668');

    /* Immutable pre-change audit (PENDING until post-validation) */
    INSERT INTO dbo.AUDIT_UserMasterOrgRemediation_B23
    (
        RemediationRunId, AppliedAt, UserMasterId, EmployeeCode, ColumnName,
        OldValue, NewValue, MasterTable, MasterInternalId, MasterERPValue,
        Classification, ApplyStatus, ValidationEvidence
    )
    SELECT
        mp.RemediationRunId, @Ts, mp.UserMasterId, mp.EmployeeCode, mp.ColumnName,
        mp.OldValue, mp.NewValue, mp.MasterTable, mp.MasterInternalId, mp.MasterERPValue,
        mp.Classification, N'PENDING',
        N'B23|READ_COMMITTED|UPDLOCK|old=' + CONVERT(varchar(12), mp.OldValue)
            + N'|new=' + CONVERT(varchar(12), mp.NewValue)
            + N'|masterId=' + CONVERT(varchar(12), mp.MasterInternalId)
    FROM #Mapping AS mp
    WHERE mp.GuardPass = 1;

    IF @@ROWCOUNT <> 115
        THROW 56218, N'STOP: audit insert count is not 115.', 1;

    /* Column-level apply — one statement per column, old-value predicate */
    DECLARE @UpdBranch int, @UpdDept int, @UpdGrade int, @UpdPlant int,
            @UpdRegion int, @UpdDiv int, @UpdCat int, @UpdSub int;

    UPDATE u SET u.BranchId = mp.NewValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Mapping AS mp ON mp.UserMasterId = u.Id AND mp.ColumnName = N'BranchId' AND mp.GuardPass = 1
    WHERE u.BranchId = mp.OldValue;
    SET @UpdBranch = @@ROWCOUNT;

    UPDATE u SET u.DepartmentId = mp.NewValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Mapping AS mp ON mp.UserMasterId = u.Id AND mp.ColumnName = N'DepartmentId' AND mp.GuardPass = 1
    WHERE u.DepartmentId = mp.OldValue;
    SET @UpdDept = @@ROWCOUNT;

    UPDATE u SET u.GradeId = mp.NewValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Mapping AS mp ON mp.UserMasterId = u.Id AND mp.ColumnName = N'GradeId' AND mp.GuardPass = 1
    WHERE u.GradeId = mp.OldValue;
    SET @UpdGrade = @@ROWCOUNT;

    UPDATE u SET u.PlantId = mp.NewValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Mapping AS mp ON mp.UserMasterId = u.Id AND mp.ColumnName = N'PlantId' AND mp.GuardPass = 1
    WHERE u.PlantId = mp.OldValue;
    SET @UpdPlant = @@ROWCOUNT;

    UPDATE u SET u.RegionId = mp.NewValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Mapping AS mp ON mp.UserMasterId = u.Id AND mp.ColumnName = N'RegionId' AND mp.GuardPass = 1
    WHERE u.RegionId = mp.OldValue;
    SET @UpdRegion = @@ROWCOUNT;

    UPDATE u SET u.DivisionId = mp.NewValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Mapping AS mp ON mp.UserMasterId = u.Id AND mp.ColumnName = N'DivisionId' AND mp.GuardPass = 1
    WHERE u.DivisionId = mp.OldValue;
    SET @UpdDiv = @@ROWCOUNT;

    UPDATE u SET u.CategoryId = mp.NewValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Mapping AS mp ON mp.UserMasterId = u.Id AND mp.ColumnName = N'CategoryId' AND mp.GuardPass = 1
    WHERE u.CategoryId = mp.OldValue;
    SET @UpdCat = @@ROWCOUNT;

    UPDATE u SET u.SubDepartmentId = mp.NewValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Mapping AS mp ON mp.UserMasterId = u.Id AND mp.ColumnName = N'SubDepartmentId' AND mp.GuardPass = 1
    WHERE u.SubDepartmentId = mp.OldValue;
    SET @UpdSub = @@ROWCOUNT;

    DECLARE @Applied int = @UpdBranch + @UpdDept + @UpdGrade + @UpdPlant
                         + @UpdRegion + @UpdDiv + @UpdCat + @UpdSub;

    IF @UpdBranch <> 17 OR @UpdDept <> 18 OR @UpdGrade <> 6 OR @UpdPlant <> 18
        OR @UpdRegion <> 18 OR @UpdDiv <> 15 OR @UpdCat <> 6 OR @UpdSub <> 17
        OR @Applied <> 115
    BEGIN
        DECLARE @updMsg nvarchar(400) =
            N'STOP: update count mismatch. Applied=' + CONVERT(varchar(10), @Applied)
            + N' B=' + CONVERT(varchar(10), @UpdBranch)
            + N' D=' + CONVERT(varchar(10), @UpdDept)
            + N' G=' + CONVERT(varchar(10), @UpdGrade)
            + N' P=' + CONVERT(varchar(10), @UpdPlant)
            + N' R=' + CONVERT(varchar(10), @UpdRegion)
            + N' V=' + CONVERT(varchar(10), @UpdDiv)
            + N' C=' + CONVERT(varchar(10), @UpdCat)
            + N' S=' + CONVERT(varchar(10), @UpdSub);
        THROW 56220, @updMsg, 1;
    END;

    IF OBJECT_ID('tempdb..#AfterState') IS NOT NULL DROP TABLE #AfterState;
    SELECT
        u.Id AS UserMasterId, u.EmployeeCode,
        u.BranchId, u.DepartmentId, u.GradeId, u.PlantId,
        u.RegionId, u.DivisionId, u.CategoryId, u.SubDepartmentId, u.Designation_ID
    INTO #AfterState
    FROM dbo.MST_UserMaster AS u
    WHERE u.Id IN (SELECT UserMasterId FROM #BeforeState);

    /* Contract: remapped columns now equal NewValue */
    IF (
        SELECT COUNT(*)
        FROM #Mapping AS mp
        INNER JOIN #AfterState AS a ON a.UserMasterId = mp.UserMasterId
        WHERE mp.GuardPass = 1
          AND (
               (mp.ColumnName = N'BranchId'        AND a.BranchId = mp.NewValue)
            OR (mp.ColumnName = N'DepartmentId'    AND a.DepartmentId = mp.NewValue)
            OR (mp.ColumnName = N'GradeId'         AND a.GradeId = mp.NewValue)
            OR (mp.ColumnName = N'PlantId'         AND a.PlantId = mp.NewValue)
            OR (mp.ColumnName = N'RegionId'        AND a.RegionId = mp.NewValue)
            OR (mp.ColumnName = N'DivisionId'      AND a.DivisionId = mp.NewValue)
            OR (mp.ColumnName = N'CategoryId'      AND a.CategoryId = mp.NewValue)
            OR (mp.ColumnName = N'SubDepartmentId' AND a.SubDepartmentId = mp.NewValue)
          )
    ) <> 115
        THROW 56221, N'STOP: post-apply contract count is not 115.', 1;

    /* Unaffected columns unchanged */
    IF OBJECT_ID('tempdb..#MapCols') IS NOT NULL DROP TABLE #MapCols;
    SELECT DISTINCT UserMasterId, ColumnName INTO #MapCols FROM #Mapping;

    IF EXISTS (
        SELECT 1
        FROM #BeforeState AS b
        INNER JOIN #AfterState AS a ON a.UserMasterId = b.UserMasterId
        LEFT JOIN #MapCols AS mb ON mb.UserMasterId = b.UserMasterId AND mb.ColumnName = N'BranchId'
        LEFT JOIN #MapCols AS md ON md.UserMasterId = b.UserMasterId AND md.ColumnName = N'DepartmentId'
        LEFT JOIN #MapCols AS mg ON mg.UserMasterId = b.UserMasterId AND mg.ColumnName = N'GradeId'
        LEFT JOIN #MapCols AS mp ON mp.UserMasterId = b.UserMasterId AND mp.ColumnName = N'PlantId'
        LEFT JOIN #MapCols AS mr ON mr.UserMasterId = b.UserMasterId AND mr.ColumnName = N'RegionId'
        LEFT JOIN #MapCols AS mv ON mv.UserMasterId = b.UserMasterId AND mv.ColumnName = N'DivisionId'
        LEFT JOIN #MapCols AS mc ON mc.UserMasterId = b.UserMasterId AND mc.ColumnName = N'CategoryId'
        LEFT JOIN #MapCols AS ms ON ms.UserMasterId = b.UserMasterId AND ms.ColumnName = N'SubDepartmentId'
        WHERE (mb.UserMasterId IS NULL AND a.BranchId <> b.BranchId)
           OR (md.UserMasterId IS NULL AND ISNULL(a.DepartmentId,-1) <> ISNULL(b.DepartmentId,-1))
           OR (mg.UserMasterId IS NULL AND ISNULL(a.GradeId,-1) <> ISNULL(b.GradeId,-1))
           OR (mp.UserMasterId IS NULL AND a.PlantId <> b.PlantId)
           OR (mr.UserMasterId IS NULL AND ISNULL(a.RegionId,-1) <> ISNULL(b.RegionId,-1))
           OR (mv.UserMasterId IS NULL AND ISNULL(a.DivisionId,-1) <> ISNULL(b.DivisionId,-1))
           OR (mc.UserMasterId IS NULL AND ISNULL(a.CategoryId,-1) <> ISNULL(b.CategoryId,-1))
           OR (ms.UserMasterId IS NULL AND ISNULL(a.SubDepartmentId,-1) <> ISNULL(b.SubDepartmentId,-1))
    )
        THROW 56222, N'STOP: an unmapped organizational column changed.', 1;

    /* Fixtures unchanged */
    IF EXISTS (
        SELECT 1
        FROM #BeforeState AS b
        INNER JOIN #AfterState AS a ON a.UserMasterId = b.UserMasterId
        WHERE b.EmployeeCode IN (N'PHA9B2E_EX1', N'PHA9B2E_MGR', N'PHA9B2E_NW1', N'PHA9B2E_V7')
          AND (
               a.BranchId <> b.BranchId OR ISNULL(a.DepartmentId,-1) <> ISNULL(b.DepartmentId,-1)
            OR ISNULL(a.GradeId,-1) <> ISNULL(b.GradeId,-1) OR a.PlantId <> b.PlantId
            OR ISNULL(a.RegionId,-1) <> ISNULL(b.RegionId,-1) OR ISNULL(a.DivisionId,-1) <> ISNULL(b.DivisionId,-1)
            OR ISNULL(a.CategoryId,-1) <> ISNULL(b.CategoryId,-1) OR ISNULL(a.SubDepartmentId,-1) <> ISNULL(b.SubDepartmentId,-1)
            OR ISNULL(a.Designation_ID,-1) <> ISNULL(b.Designation_ID,-1)
          )
    )
        THROW 56223, N'STOP: fixture row was modified.', 1;

    /* Designation unchanged for every snapshotted user */
    IF EXISTS (
        SELECT 1
        FROM #BeforeState AS b
        INNER JOIN #AfterState AS a ON a.UserMasterId = b.UserMasterId
        WHERE ISNULL(a.Designation_ID,-1) <> ISNULL(b.Designation_ID,-1)
    )
        THROW 56224, N'STOP: Designation_ID changed.', 1;

    /* Five zero-targets unchanged */
    IF EXISTS (
        SELECT 1 FROM #AfterState
        WHERE EmployeeCode = N'0001' AND DivisionId <> 4
    ) OR EXISTS (
        SELECT 1 FROM #AfterState
        WHERE EmployeeCode = N'0006' AND DivisionId <> 4
    ) OR EXISTS (
        SELECT 1 FROM #AfterState
        WHERE EmployeeCode = N'0034' AND (BranchId <> 19 OR DivisionId <> 4 OR SubDepartmentId <> 110)
    )
        THROW 56225, N'STOP: a zero-target exclusion was modified.', 1;

    /* Plant 3277 user untouched as a conversion */
    IF EXISTS (SELECT 1 FROM #AfterState WHERE EmployeeCode = N'M2668' AND PlantId <> 3277)
        THROW 56226, N'STOP: M2668 PlantId 3277 was modified.', 1;

    IF EXISTS (SELECT 1 FROM dbo.AUDIT_UserMasterOrgRemediation_B23 WHERE RemediationRunId = @RunId AND OldValue = NewValue)
        THROW 56227, N'STOP: audit contains OldValue = NewValue.', 1;

    IF (SELECT COUNT(*) FROM dbo.AUDIT_UserMasterOrgRemediation_B23 WHERE RemediationRunId = @RunId) <> 115
        THROW 56228, N'STOP: audit row count is not 115.', 1;

    UPDATE dbo.AUDIT_UserMasterOrgRemediation_B23
    SET ApplyStatus = N'APPLIED'
    WHERE RemediationRunId = @RunId
      AND ApplyStatus = N'PENDING';

    IF @@ROWCOUNT <> 115
        THROW 56229, N'STOP: could not mark all 115 audit rows APPLIED.', 1;

    COMMIT TRANSACTION;

    SELECT
        N'COMMITTED' AS ApplyOutcome,
        @RunId AS RemediationRunId,
        @Ts AS AppliedAt,
        DB_NAME() AS DbName,
        115 AS AuditRows,
        @Applied AS ColumnUpdates,
        @UpdBranch AS UpdBranch,
        @UpdDept AS UpdDept,
        @UpdGrade AS UpdGrade,
        @UpdPlant AS UpdPlant,
        @UpdRegion AS UpdRegion,
        @UpdDiv AS UpdDiv,
        @UpdCat AS UpdCat,
        @UpdSub AS UpdSub,
        0 AS UnexpectedUpdates,
        0 AS FailedMappings;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    SELECT
        N'ROLLED_BACK' AS ApplyOutcome,
        ERROR_NUMBER() AS ErrNum,
        ERROR_MESSAGE() AS ErrMsg,
        DB_NAME() AS DbName;

    THROW;
END CATCH;
