/*
    Package:     Database/Deployments/HRMS_PhaseB2_6_UAT_SentinelZeroRemediation
    Script:      B_ControlledApply.sql
    Phase:       B2.6 — permanent UAT apply of exactly 5 sentinel-zero mappings
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN

    BEGIN TRAN
      regenerate SENTINEL_UNASSIGNED candidates
      assert exactly 5 / exact old values / master ERP 0
      UPDLOCK the 3 candidate UserMaster rows
      insert 5 PENDING audit rows
      5 column-level guarded UPDATEs (identity + old-value predicate)
      post-apply validation (NewValue = 0 is required)
      mark audit APPLIED
    COMMIT only if every assertion passes.

    Any failure → ROLLBACK entire transaction (no partial apply).
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
    THROW 56603, N'ABORT: audit table missing. Run 000_CreateAuditTable.sql first.', 1;

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

    IF OBJECT_ID('tempdb..#Mapping') IS NOT NULL DROP TABLE #Mapping;
    CREATE TABLE #Mapping
    (
        RemediationRunId varchar(40) NOT NULL,
        UserMasterId int NOT NULL,
        EmployeeCode varchar(50) NOT NULL,
        ColumnName sysname NOT NULL,
        OldValue int NOT NULL,
        NewValue int NOT NULL,
        TargetMasterTable sysname NOT NULL,
        TargetMasterInternalId int NOT NULL,
        TargetERPValue int NOT NULL,
        Classification varchar(40) NOT NULL,
        IsFixture bit NOT NULL,
        ValidationTimestamp datetime2 NOT NULL,
        GuardOldValue bit NOT NULL DEFAULT (0),
        GuardEmployeeIdentity bit NOT NULL DEFAULT (0),
        GuardMasterIdentity bit NOT NULL DEFAULT (0),
        GuardErpTarget bit NOT NULL DEFAULT (0),
        GuardTargetValidity bit NOT NULL DEFAULT (0),
        GuardFixture bit NOT NULL DEFAULT (0),
        GuardColumnContract bit NOT NULL DEFAULT (0),
        GuardPass bit NOT NULL DEFAULT (0)
    );

    DECLARE @RunId varchar(40) = N'B26-APPLY-' + CONVERT(varchar(19), SYSDATETIME(), 126);
    DECLARE @Ts datetime2 = SYSDATETIME();
    DECLARE @B23AppliedBefore int = 0;
    IF OBJECT_ID(N'dbo.AUDIT_UserMasterOrgRemediation_B23', N'U') IS NOT NULL
        SELECT @B23AppliedBefore = COUNT(*)
        FROM dbo.AUDIT_UserMasterOrgRemediation_B23
        WHERE ApplyStatus = N'APPLIED';

    DECLARE @sql nvarchar(max) = N'';
    SELECT @sql = @sql + N'
    INSERT INTO #Mapping (
        RemediationRunId, UserMasterId, EmployeeCode, ColumnName,
        OldValue, NewValue, TargetMasterTable, TargetMasterInternalId, TargetERPValue,
        Classification, IsFixture, ValidationTimestamp
    )
    SELECT
        @RunId, u.Id, u.EmployeeCode, N''' + ColumnName + N''',
        u.[' + ColumnName + N'],
        0,
        N''' + MasterTable + N''',
        m.Id,
        TRY_CONVERT(INT, m.[' + ErpColumn + N']),
        N''SENTINEL_UNASSIGNED'',
        CASE WHEN u.EmployeeCode IN (N''PHA9B2E_EX1'', N''PHA9B2E_MGR'', N''PHA9B2E_NW1'', N''PHA9B2E_V7'') THEN 1 ELSE 0 END,
        @Ts
    FROM dbo.MST_UserMaster AS u
    INNER JOIN dbo.[' + MasterTable + N'] AS m ON m.Id = u.[' + ColumnName + N']
    WHERE u.[' + ColumnName + N'] IS NOT NULL
      AND u.[' + ColumnName + N'] <> 0
      AND u.EmployeeCode NOT IN (N''PHA9B2E_EX1'', N''PHA9B2E_MGR'', N''PHA9B2E_NW1'', N''PHA9B2E_V7'')
      AND TRY_CONVERT(INT, m.[' + ErpColumn + N']) = 0
      AND NOT EXISTS (
            SELECT 1 FROM dbo.[' + MasterTable + N'] e
            WHERE TRY_CONVERT(INT, e.[' + ErpColumn + N']) = u.[' + ColumnName + N']
          );
    '
    FROM #OrgMap;

    EXEC sp_executesql @sql, N'@RunId varchar(40), @Ts datetime2', @RunId = @RunId, @Ts = @Ts;

    DECLARE @MapCount int = (SELECT COUNT(*) FROM #Mapping);
    DECLARE @CBranch int = (SELECT COUNT(*) FROM #Mapping WHERE ColumnName = N'BranchId');
    DECLARE @CDiv int = (SELECT COUNT(*) FROM #Mapping WHERE ColumnName = N'DivisionId');
    DECLARE @CSub int = (SELECT COUNT(*) FROM #Mapping WHERE ColumnName = N'SubDepartmentId');

    IF @MapCount <> 5 OR @CBranch <> 1 OR @CDiv <> 3 OR @CSub <> 1
    BEGIN
        DECLARE @distMsg nvarchar(400) =
            N'STOP: candidate count/distribution mismatch. Count='
            + CONVERT(varchar(10), @MapCount)
            + N' B=' + CONVERT(varchar(10), @CBranch)
            + N' V=' + CONVERT(varchar(10), @CDiv)
            + N' S=' + CONVERT(varchar(10), @CSub);
        THROW 56610, @distMsg, 1;
    END;

    IF EXISTS (
        SELECT 1 FROM #Expected AS e
        WHERE NOT EXISTS (
            SELECT 1 FROM #Mapping AS mp
            WHERE mp.EmployeeCode = e.EmployeeCode
              AND mp.ColumnName = e.ColumnName
              AND mp.OldValue = e.OldValue
              AND mp.NewValue = 0
              AND mp.TargetMasterInternalId = e.MasterInternalId
              AND mp.TargetMasterTable = e.MasterTable
              AND mp.TargetERPValue = 0
        )
    )
        THROW 56611, N'STOP: regenerated candidates do not match the exact authorized five mappings.', 1;

    IF EXISTS (
        SELECT 1 FROM #Mapping AS mp
        WHERE NOT EXISTS (
            SELECT 1 FROM #Expected AS e
            WHERE e.EmployeeCode = mp.EmployeeCode
              AND e.ColumnName = mp.ColumnName
              AND e.OldValue = mp.OldValue
        )
    )
        THROW 56612, N'STOP: unexpected candidate mapping present.', 1;

    IF EXISTS (SELECT 1 FROM #Mapping WHERE NewValue <> 0 OR TargetERPValue <> 0 OR NewValue IS NULL)
        THROW 56613, N'STOP: non-zero or NULL ERP target leaked into mapping.', 1;

    IF EXISTS (SELECT 1 FROM #Mapping WHERE EmployeeCode NOT IN (N'0001', N'0006', N'0034') OR IsFixture = 1)
        THROW 56614, N'STOP: unexpected employee or fixture leaked into mapping.', 1;

    IF EXISTS (SELECT 1 FROM #Mapping WHERE ColumnName = N'Designation_ID'
        OR ColumnName NOT IN (N'BranchId', N'DivisionId', N'SubDepartmentId'))
        THROW 56615, N'STOP: ineligible column in mapping.', 1;

    IF EXISTS (SELECT 1 FROM #Mapping WHERE ColumnName = N'PlantId')
        THROW 56616, N'STOP: PlantId conversion candidate present.', 1;

    IF EXISTS (
        SELECT 1 FROM #Mapping AS mp
        INNER JOIN dbo.AUDIT_UserMasterOrgRemediation_B23 AS a
            ON a.UserMasterId = mp.UserMasterId
           AND a.ColumnName = mp.ColumnName
           AND a.ApplyStatus = N'APPLIED'
    )
        THROW 56617, N'STOP: B2.6 candidate overlaps a B2.3 APPLIED audit mapping.', 1;

    /* Lock only the distinct candidate UserMaster rows */
    IF OBJECT_ID('tempdb..#LockedUsers') IS NOT NULL DROP TABLE #LockedUsers;
    SELECT
        u.Id AS UserMasterId, u.EmployeeCode,
        u.BranchId, u.DepartmentId, u.GradeId, u.PlantId,
        u.RegionId, u.DivisionId, u.CategoryId, u.SubDepartmentId, u.Designation_ID
    INTO #LockedUsers
    FROM dbo.MST_UserMaster AS u WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
    WHERE u.Id IN (SELECT DISTINCT UserMasterId FROM #Mapping)
      AND u.EmployeeCode IN (SELECT DISTINCT EmployeeCode FROM #Mapping);

    IF (SELECT COUNT(*) FROM #LockedUsers) <> (SELECT COUNT(DISTINCT UserMasterId) FROM #Mapping)
        THROW 56618, N'STOP: could not lock every candidate UserMaster row.', 1;

    IF (SELECT COUNT(*) FROM #LockedUsers) <> 3
        THROW 56619, N'STOP: expected to lock exactly 3 UserMaster rows.', 1;

    UPDATE mp
    SET
        GuardOldValue = CASE
            WHEN (mp.ColumnName = N'BranchId'        AND lu.BranchId = mp.OldValue)
              OR (mp.ColumnName = N'DivisionId'      AND lu.DivisionId = mp.OldValue)
              OR (mp.ColumnName = N'SubDepartmentId' AND lu.SubDepartmentId = mp.OldValue)
            THEN 1 ELSE 0 END,
        GuardEmployeeIdentity = CASE
            WHEN lu.EmployeeCode = mp.EmployeeCode
             AND lu.EmployeeCode IN (N'0001', N'0006', N'0034')
            THEN 1 ELSE 0 END,
        GuardFixture = CASE
            WHEN lu.EmployeeCode NOT IN (N'PHA9B2E_EX1', N'PHA9B2E_MGR', N'PHA9B2E_NW1', N'PHA9B2E_V7')
             AND mp.IsFixture = 0 THEN 1 ELSE 0 END,
        GuardColumnContract = CASE
            WHEN mp.ColumnName IN (N'BranchId', N'DivisionId', N'SubDepartmentId')
            THEN 1 ELSE 0 END
    FROM #Mapping AS mp
    INNER JOIN #LockedUsers AS lu ON lu.UserMasterId = mp.UserMasterId AND lu.EmployeeCode = mp.EmployeeCode;

    DECLARE @gsql nvarchar(max) = N'';
    SELECT @gsql = @gsql + N'
    UPDATE mp
    SET
        GuardMasterIdentity = CASE
            WHEN EXISTS (
                SELECT 1 FROM dbo.[' + MasterTable + N'] m
                WHERE m.Id = mp.OldValue
                  AND m.Id = mp.TargetMasterInternalId
                  AND mp.ColumnName = N''' + ColumnName + N'''
            ) THEN 1 ELSE 0 END,
        GuardErpTarget = CASE
            WHEN EXISTS (
                SELECT 1 FROM dbo.[' + MasterTable + N'] m
                WHERE m.Id = mp.TargetMasterInternalId
                  AND mp.ColumnName = N''' + ColumnName + N'''
                  AND TRY_CONVERT(INT, m.[' + ErpColumn + N']) = 0
                  AND TRY_CONVERT(INT, m.[' + ErpColumn + N']) = mp.NewValue
                  AND TRY_CONVERT(INT, m.[' + ErpColumn + N']) = mp.TargetERPValue
            ) THEN 1 ELSE 0 END,
        GuardTargetValidity = CASE
            WHEN mp.NewValue = 0 AND mp.TargetERPValue = 0
             AND (
                SELECT COUNT(*) FROM dbo.[' + MasterTable + N'] x
                WHERE TRY_CONVERT(INT, x.[' + ErpColumn + N']) = 0
             ) = 1
            THEN 1 ELSE 0 END
    FROM #Mapping AS mp
    WHERE mp.ColumnName = N''' + ColumnName + N''';
    '
    FROM #OrgMap;
    EXEC sp_executesql @gsql;

    UPDATE #Mapping
    SET GuardPass = CASE
        WHEN GuardOldValue = 1 AND GuardEmployeeIdentity = 1 AND GuardMasterIdentity = 1
         AND GuardErpTarget = 1 AND GuardTargetValidity = 1 AND GuardFixture = 1
         AND GuardColumnContract = 1
        THEN 1 ELSE 0 END;

    IF EXISTS (SELECT 1 FROM #Mapping WHERE GuardPass = 0)
        THROW 56620, N'STOP: one or more mapping guards failed (STALE_MAPPING or master drift).', 1;

    IF (SELECT COUNT(*) FROM #Mapping WHERE GuardPass = 1) <> 5
        THROW 56621, N'STOP: GuardPass count is not 5.', 1;

    IF OBJECT_ID('tempdb..#BeforeState') IS NOT NULL DROP TABLE #BeforeState;
    SELECT
        u.Id AS UserMasterId, u.EmployeeCode,
        u.BranchId, u.DepartmentId, u.GradeId, u.PlantId,
        u.RegionId, u.DivisionId, u.CategoryId, u.SubDepartmentId, u.Designation_ID
    INTO #BeforeState
    FROM dbo.MST_UserMaster AS u
    WHERE u.Id IN (SELECT UserMasterId FROM #Mapping)
       OR u.EmployeeCode IN (
            N'PHA9B2E_EX1', N'PHA9B2E_MGR', N'PHA9B2E_NW1', N'PHA9B2E_V7',
            N'0001', N'0006', N'0034', N'M2668');

    INSERT INTO dbo.AUDIT_UserMasterOrgSentinelZero_B26
    (
        RemediationRunId, AppliedAt, UserMasterId, EmployeeCode, ColumnName,
        OldValue, NewValue, TargetMasterTable, TargetMasterInternalId, TargetERPValue,
        Classification, ApplyStatus, ValidationEvidence
    )
    SELECT
        mp.RemediationRunId, @Ts, mp.UserMasterId, mp.EmployeeCode, mp.ColumnName,
        mp.OldValue, mp.NewValue, mp.TargetMasterTable, mp.TargetMasterInternalId, mp.TargetERPValue,
        mp.Classification, N'PENDING',
        N'B26|READ_COMMITTED|UPDLOCK|old=' + CONVERT(varchar(12), mp.OldValue)
            + N'|new=0|masterId=' + CONVERT(varchar(12), mp.TargetMasterInternalId)
    FROM #Mapping AS mp
    WHERE mp.GuardPass = 1;

    IF @@ROWCOUNT <> 5
        THROW 56622, N'STOP: audit insert count is not 5.', 1;

    /* Five identity + old-value guarded updates. @@ROWCOUNT must be 1 each. */
    DECLARE @Upd0001Div int, @Upd0006Div int, @Upd0034Br int, @Upd0034Div int, @Upd0034Sub int;

    UPDATE u
    SET u.DivisionId = 0
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Mapping AS mp
        ON mp.UserMasterId = u.Id
       AND mp.EmployeeCode = u.EmployeeCode
       AND mp.ColumnName = N'DivisionId'
       AND mp.EmployeeCode = N'0001'
       AND mp.GuardPass = 1
    WHERE u.EmployeeCode = N'0001'
      AND u.DivisionId = 4
      AND mp.OldValue = 4
      AND mp.NewValue = 0;
    SET @Upd0001Div = @@ROWCOUNT;
    IF @Upd0001Div <> 1
        THROW 56630, N'STOP: 0001.DivisionId update rowcount is not 1.', 1;

    UPDATE u
    SET u.DivisionId = 0
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Mapping AS mp
        ON mp.UserMasterId = u.Id
       AND mp.EmployeeCode = u.EmployeeCode
       AND mp.ColumnName = N'DivisionId'
       AND mp.EmployeeCode = N'0006'
       AND mp.GuardPass = 1
    WHERE u.EmployeeCode = N'0006'
      AND u.DivisionId = 4
      AND mp.OldValue = 4
      AND mp.NewValue = 0;
    SET @Upd0006Div = @@ROWCOUNT;
    IF @Upd0006Div <> 1
        THROW 56631, N'STOP: 0006.DivisionId update rowcount is not 1.', 1;

    UPDATE u
    SET u.BranchId = 0
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Mapping AS mp
        ON mp.UserMasterId = u.Id
       AND mp.EmployeeCode = u.EmployeeCode
       AND mp.ColumnName = N'BranchId'
       AND mp.EmployeeCode = N'0034'
       AND mp.GuardPass = 1
    WHERE u.EmployeeCode = N'0034'
      AND u.BranchId = 19
      AND mp.OldValue = 19
      AND mp.NewValue = 0;
    SET @Upd0034Br = @@ROWCOUNT;
    IF @Upd0034Br <> 1
        THROW 56632, N'STOP: 0034.BranchId update rowcount is not 1.', 1;

    UPDATE u
    SET u.DivisionId = 0
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Mapping AS mp
        ON mp.UserMasterId = u.Id
       AND mp.EmployeeCode = u.EmployeeCode
       AND mp.ColumnName = N'DivisionId'
       AND mp.EmployeeCode = N'0034'
       AND mp.GuardPass = 1
    WHERE u.EmployeeCode = N'0034'
      AND u.DivisionId = 4
      AND mp.OldValue = 4
      AND mp.NewValue = 0;
    SET @Upd0034Div = @@ROWCOUNT;
    IF @Upd0034Div <> 1
        THROW 56633, N'STOP: 0034.DivisionId update rowcount is not 1.', 1;

    UPDATE u
    SET u.SubDepartmentId = 0
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Mapping AS mp
        ON mp.UserMasterId = u.Id
       AND mp.EmployeeCode = u.EmployeeCode
       AND mp.ColumnName = N'SubDepartmentId'
       AND mp.EmployeeCode = N'0034'
       AND mp.GuardPass = 1
    WHERE u.EmployeeCode = N'0034'
      AND u.SubDepartmentId = 110
      AND mp.OldValue = 110
      AND mp.NewValue = 0;
    SET @Upd0034Sub = @@ROWCOUNT;
    IF @Upd0034Sub <> 1
        THROW 56634, N'STOP: 0034.SubDepartmentId update rowcount is not 1.', 1;

    DECLARE @UpdBranch int = @Upd0034Br;
    DECLARE @UpdDiv int = @Upd0001Div + @Upd0006Div + @Upd0034Div;
    DECLARE @UpdSub int = @Upd0034Sub;
    DECLARE @Applied int = @UpdBranch + @UpdDiv + @UpdSub;

    IF @UpdBranch <> 1 OR @UpdDiv <> 3 OR @UpdSub <> 1 OR @Applied <> 5
    BEGIN
        DECLARE @updMsg nvarchar(400) =
            N'STOP: update count mismatch. Applied=' + CONVERT(varchar(10), @Applied)
            + N' B=' + CONVERT(varchar(10), @UpdBranch)
            + N' V=' + CONVERT(varchar(10), @UpdDiv)
            + N' S=' + CONVERT(varchar(10), @UpdSub);
        THROW 56635, @updMsg, 1;
    END;

    IF OBJECT_ID('tempdb..#AfterState') IS NOT NULL DROP TABLE #AfterState;
    SELECT
        u.Id AS UserMasterId, u.EmployeeCode,
        u.BranchId, u.DepartmentId, u.GradeId, u.PlantId,
        u.RegionId, u.DivisionId, u.CategoryId, u.SubDepartmentId, u.Designation_ID
    INTO #AfterState
    FROM dbo.MST_UserMaster AS u
    WHERE u.Id IN (SELECT UserMasterId FROM #BeforeState)
       OR u.EmployeeCode IN (SELECT EmployeeCode FROM #BeforeState);

    IF (
        SELECT COUNT(*)
        FROM #Mapping AS mp
        INNER JOIN #AfterState AS a ON a.UserMasterId = mp.UserMasterId AND a.EmployeeCode = mp.EmployeeCode
        WHERE mp.GuardPass = 1
          AND (
               (mp.ColumnName = N'BranchId'        AND a.BranchId = 0)
            OR (mp.ColumnName = N'DivisionId'      AND a.DivisionId = 0)
            OR (mp.ColumnName = N'SubDepartmentId' AND a.SubDepartmentId = 0)
          )
    ) <> 5
        THROW 56636, N'STOP: post-apply CurrentEqualsNew is not 5.', 1;

    IF EXISTS (
        SELECT 1 FROM #AfterState WHERE EmployeeCode = N'0001' AND DivisionId <> 0
    ) OR EXISTS (
        SELECT 1 FROM #AfterState WHERE EmployeeCode = N'0006' AND DivisionId <> 0
    ) OR EXISTS (
        SELECT 1 FROM #AfterState WHERE EmployeeCode = N'0034'
            AND (BranchId <> 0 OR DivisionId <> 0 OR SubDepartmentId <> 0)
    )
        THROW 56637, N'STOP: a B2.6 candidate is not 0 after apply.', 1;

    IF EXISTS (
        SELECT 1
        FROM #Mapping AS mp
        INNER JOIN #AfterState AS a ON a.UserMasterId = mp.UserMasterId
        WHERE (mp.ColumnName = N'BranchId'        AND a.BranchId = mp.OldValue)
           OR (mp.ColumnName = N'DivisionId'      AND a.DivisionId = mp.OldValue)
           OR (mp.ColumnName = N'SubDepartmentId' AND a.SubDepartmentId = mp.OldValue)
    )
        THROW 56638, N'STOP: CurrentStillOld is not 0.', 1;

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
        THROW 56639, N'STOP: an unmapped organizational column changed.', 1;

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
        THROW 56640, N'STOP: fixture row was modified.', 1;

    IF EXISTS (
        SELECT 1
        FROM #BeforeState AS b
        INNER JOIN #AfterState AS a ON a.UserMasterId = b.UserMasterId
        WHERE ISNULL(a.Designation_ID,-1) <> ISNULL(b.Designation_ID,-1)
    )
        THROW 56641, N'STOP: Designation_ID changed.', 1;

    IF EXISTS (SELECT 1 FROM #AfterState WHERE EmployeeCode = N'M2668' AND PlantId <> 3277)
        THROW 56642, N'STOP: M2668 PlantId 3277 was modified.', 1;

    IF EXISTS (
        SELECT 1
        FROM #BeforeState AS b
        INNER JOIN #AfterState AS a ON a.UserMasterId = b.UserMasterId
        WHERE a.PlantId <> b.PlantId
    )
        THROW 56643, N'STOP: a PlantId value changed.', 1;

    IF OBJECT_ID(N'dbo.AUDIT_UserMasterOrgRemediation_B23', N'U') IS NOT NULL
       AND (SELECT COUNT(*) FROM dbo.AUDIT_UserMasterOrgRemediation_B23 WHERE ApplyStatus = N'APPLIED') <> @B23AppliedBefore
        THROW 56644, N'STOP: B2.3 audit row count changed.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.AUDIT_UserMasterOrgSentinelZero_B26
        WHERE RemediationRunId = @RunId AND (OldValue = NewValue OR NewValue <> 0)
    )
        THROW 56645, N'STOP: audit OldValue=NewValue or NewValue<>0.', 1;

    IF (SELECT COUNT(*) FROM dbo.AUDIT_UserMasterOrgSentinelZero_B26 WHERE RemediationRunId = @RunId) <> 5
        THROW 56646, N'STOP: audit row count is not 5.', 1;

    IF (SELECT COUNT(*) FROM dbo.AUDIT_UserMasterOrgSentinelZero_B26 WHERE RemediationRunId = @RunId AND NewValue = 0) <> 5
        THROW 56647, N'STOP: audit NewValue=0 count is not 5.', 1;

    UPDATE dbo.AUDIT_UserMasterOrgSentinelZero_B26
    SET ApplyStatus = N'APPLIED'
    WHERE RemediationRunId = @RunId
      AND ApplyStatus = N'PENDING';

    IF @@ROWCOUNT <> 5
        THROW 56648, N'STOP: could not mark all 5 audit rows APPLIED.', 1;

    COMMIT TRANSACTION;

    SELECT
        N'COMMITTED' AS ApplyOutcome,
        @RunId AS RemediationRunId,
        @Ts AS AppliedAt,
        DB_NAME() AS DbName,
        5 AS AuditRows,
        @Applied AS ColumnUpdates,
        @UpdBranch AS UpdBranch,
        @UpdDiv AS UpdDiv,
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
