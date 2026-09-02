/*
    Package:     Database/Deployments/HRMS_PhaseB2_3_UAT_OrgRemediation
    Script:      D_Rollback.sql
    Phase:       B2.3
    Mode:        PREPARED — do NOT execute after a successful apply
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN

    Inverts one RemediationRunId:
      current column value must still equal Audit.NewValue
      then restore Audit.OldValue
      mark audit ROLLED_BACK

    Set @RemediationRunId before running.
    Expected restore count = 115.
    Any stale current value → ROLLBACK entire invert (no partial restore).
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

/* ===== OPERATOR: set this to the committed B2.3 run id ===== */
DECLARE @RemediationRunId varchar(40) = N'<<SET_B23_REMEDIATION_RUN_ID>>';

IF DB_NAME() = N'AnmolDristi'
    THROW 56201, N'ABORT: Live database AnmolDristi is forbidden for Phase B2.3.', 1;

IF DB_NAME() <> N'AnmolDristiDev'
BEGIN
    DECLARE @dbMsg nvarchar(400) =
        N'ABORT: connected to ' + QUOTENAME(DB_NAME()) + N'. Expected [AnmolDristiDev].';
    THROW 56202, @dbMsg, 1;
END;

IF @RemediationRunId IS NULL
    OR @RemediationRunId = N''
    OR @RemediationRunId = N'<<SET_B23_REMEDIATION_RUN_ID>>'
    THROW 56240, N'ABORT: set @RemediationRunId to the committed B23-APPLY-* run id.', 1;

IF OBJECT_ID(N'dbo.AUDIT_UserMasterOrgRemediation_B23', N'U') IS NULL
    THROW 56203, N'ABORT: audit table missing.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#Rb') IS NOT NULL DROP TABLE #Rb;
    SELECT
        a.AuditId, a.RemediationRunId, a.UserMasterId, a.EmployeeCode, a.ColumnName,
        a.OldValue, a.NewValue
    INTO #Rb
    FROM dbo.AUDIT_UserMasterOrgRemediation_B23 AS a WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
    WHERE a.RemediationRunId = @RemediationRunId
      AND a.ApplyStatus = N'APPLIED';

    IF (SELECT COUNT(*) FROM #Rb) <> 115
    BEGIN
        DECLARE @cntMsg nvarchar(200) =
            N'STOP: APPLIED audit rows for this run = '
            + CONVERT(varchar(10), (SELECT COUNT(*) FROM #Rb))
            + N'; expected 115.';
        THROW 56241, @cntMsg, 1;
    END;

    /* Lock candidate users */
    IF OBJECT_ID('tempdb..#Lock') IS NOT NULL DROP TABLE #Lock;
    SELECT u.Id
    INTO #Lock
    FROM dbo.MST_UserMaster AS u WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
    WHERE u.Id IN (SELECT DISTINCT UserMasterId FROM #Rb);

    /* Stale rollback conflict: current must still equal NewValue */
    IF EXISTS (
        SELECT 1
        FROM #Rb AS r
        INNER JOIN dbo.MST_UserMaster AS u ON u.Id = r.UserMasterId
        WHERE (r.ColumnName = N'BranchId'        AND u.BranchId <> r.NewValue)
           OR (r.ColumnName = N'DepartmentId'    AND ISNULL(u.DepartmentId,-1) <> r.NewValue)
           OR (r.ColumnName = N'GradeId'         AND ISNULL(u.GradeId,-1) <> r.NewValue)
           OR (r.ColumnName = N'PlantId'         AND u.PlantId <> r.NewValue)
           OR (r.ColumnName = N'RegionId'        AND ISNULL(u.RegionId,-1) <> r.NewValue)
           OR (r.ColumnName = N'DivisionId'      AND ISNULL(u.DivisionId,-1) <> r.NewValue)
           OR (r.ColumnName = N'CategoryId'      AND ISNULL(u.CategoryId,-1) <> r.NewValue)
           OR (r.ColumnName = N'SubDepartmentId' AND ISNULL(u.SubDepartmentId,-1) <> r.NewValue)
    )
        THROW 56242, N'STOP: stale rollback conflict — current value no longer equals Audit.NewValue.', 1;

    DECLARE @RbBranch int, @RbDept int, @RbGrade int, @RbPlant int,
            @RbRegion int, @RbDiv int, @RbCat int, @RbSub int;

    UPDATE u SET u.BranchId = r.OldValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Rb AS r ON r.UserMasterId = u.Id AND r.ColumnName = N'BranchId'
    WHERE u.BranchId = r.NewValue;
    SET @RbBranch = @@ROWCOUNT;

    UPDATE u SET u.DepartmentId = r.OldValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Rb AS r ON r.UserMasterId = u.Id AND r.ColumnName = N'DepartmentId'
    WHERE u.DepartmentId = r.NewValue;
    SET @RbDept = @@ROWCOUNT;

    UPDATE u SET u.GradeId = r.OldValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Rb AS r ON r.UserMasterId = u.Id AND r.ColumnName = N'GradeId'
    WHERE u.GradeId = r.NewValue;
    SET @RbGrade = @@ROWCOUNT;

    UPDATE u SET u.PlantId = r.OldValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Rb AS r ON r.UserMasterId = u.Id AND r.ColumnName = N'PlantId'
    WHERE u.PlantId = r.NewValue;
    SET @RbPlant = @@ROWCOUNT;

    UPDATE u SET u.RegionId = r.OldValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Rb AS r ON r.UserMasterId = u.Id AND r.ColumnName = N'RegionId'
    WHERE u.RegionId = r.NewValue;
    SET @RbRegion = @@ROWCOUNT;

    UPDATE u SET u.DivisionId = r.OldValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Rb AS r ON r.UserMasterId = u.Id AND r.ColumnName = N'DivisionId'
    WHERE u.DivisionId = r.NewValue;
    SET @RbDiv = @@ROWCOUNT;

    UPDATE u SET u.CategoryId = r.OldValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Rb AS r ON r.UserMasterId = u.Id AND r.ColumnName = N'CategoryId'
    WHERE u.CategoryId = r.NewValue;
    SET @RbCat = @@ROWCOUNT;

    UPDATE u SET u.SubDepartmentId = r.OldValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Rb AS r ON r.UserMasterId = u.Id AND r.ColumnName = N'SubDepartmentId'
    WHERE u.SubDepartmentId = r.NewValue;
    SET @RbSub = @@ROWCOUNT;

    DECLARE @Restored int = @RbBranch + @RbDept + @RbGrade + @RbPlant
                          + @RbRegion + @RbDiv + @RbCat + @RbSub;

    IF @Restored <> 115
    BEGIN
        DECLARE @rbMsg nvarchar(400) =
            N'STOP: rollback restore count=' + CONVERT(varchar(10), @Restored) + N'; expected 115.';
        THROW 56243, @rbMsg, 1;
    END;

    UPDATE dbo.AUDIT_UserMasterOrgRemediation_B23
    SET ApplyStatus = N'ROLLED_BACK'
    WHERE RemediationRunId = @RemediationRunId
      AND ApplyStatus = N'APPLIED';

    IF @@ROWCOUNT <> 115
        THROW 56244, N'STOP: could not mark all 115 audit rows ROLLED_BACK.', 1;

    COMMIT TRANSACTION;

    SELECT
        N'ROLLBACK_COMMITTED' AS RollbackOutcome,
        @RemediationRunId AS RemediationRunId,
        @Restored AS RestoredMappings,
        @RbBranch AS RbBranch, @RbDept AS RbDept, @RbGrade AS RbGrade, @RbPlant AS RbPlant,
        @RbRegion AS RbRegion, @RbDiv AS RbDiv, @RbCat AS RbCat, @RbSub AS RbSub;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    SELECT
        N'ROLLBACK_ABORTED' AS RollbackOutcome,
        ERROR_NUMBER() AS ErrNum,
        ERROR_MESSAGE() AS ErrMsg;

    THROW;
END CATCH;
