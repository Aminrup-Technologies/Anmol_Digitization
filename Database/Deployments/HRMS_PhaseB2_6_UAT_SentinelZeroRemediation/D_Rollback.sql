/*
    Package:     Database/Deployments/HRMS_PhaseB2_6_UAT_SentinelZeroRemediation
    Script:      D_Rollback.sql
    Phase:       B2.6
    Mode:        PREPARED — do NOT execute after a successful apply
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN

    Inverts one explicit B2.6 RemediationRunId:
      current column value must still equal Audit.NewValue (0)
      then restore Audit.OldValue
      mark audit ROLLED_BACK

    The run ID placeholder must be set by an authorized operator.
    Do NOT default to the latest run.
    Expected restore count = 5.
    Any stale current value → ROLLBACK entire invert (no partial restore).
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

/* ===== OPERATOR: set this to the committed B2.6 run id ===== */
/* Do not default this variable to MAX(RemediationRunId). */
DECLARE @RemediationRunId varchar(40) = N'<<SET_B26_REMEDIATION_RUN_ID>>';

IF DB_NAME() = N'AnmolDristi'
    THROW 56601, N'ABORT: Live database AnmolDristi is forbidden for Phase B2.6.', 1;

IF DB_NAME() <> N'AnmolDristiDev'
BEGIN
    DECLARE @dbMsg nvarchar(400) =
        N'ABORT: connected to ' + QUOTENAME(DB_NAME()) + N'. Expected [AnmolDristiDev].';
    THROW 56602, @dbMsg, 1;
END;

IF @RemediationRunId IS NULL
    OR @RemediationRunId = N''
    OR @RemediationRunId = N'<<SET_B26_REMEDIATION_RUN_ID>>'
    OR @RemediationRunId NOT LIKE N'B26-APPLY-%'
    THROW 56650, N'ABORT: set @RemediationRunId to the committed B26-APPLY-* run id.', 1;

IF OBJECT_ID(N'dbo.AUDIT_UserMasterOrgSentinelZero_B26', N'U') IS NULL
    THROW 56603, N'ABORT: B2.6 audit table missing.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#Rb') IS NOT NULL DROP TABLE #Rb;
    SELECT
        a.AuditId, a.RemediationRunId, a.UserMasterId, a.EmployeeCode, a.ColumnName,
        a.OldValue, a.NewValue
    INTO #Rb
    FROM dbo.AUDIT_UserMasterOrgSentinelZero_B26 AS a WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
    WHERE a.RemediationRunId = @RemediationRunId
      AND a.ApplyStatus = N'APPLIED';

    IF (SELECT COUNT(*) FROM #Rb) <> 5
    BEGIN
        DECLARE @cntMsg nvarchar(200) =
            N'STOP: APPLIED audit rows for this run = '
            + CONVERT(varchar(10), (SELECT COUNT(*) FROM #Rb))
            + N'; expected 5.';
        THROW 56651, @cntMsg, 1;
    END;

    IF EXISTS (SELECT 1 FROM #Rb WHERE NewValue <> 0)
        THROW 56652, N'STOP: rollback expects Audit.NewValue = 0 for every B2.6 row.', 1;

    IF OBJECT_ID('tempdb..#Lock') IS NOT NULL DROP TABLE #Lock;
    SELECT u.Id, u.EmployeeCode
    INTO #Lock
    FROM dbo.MST_UserMaster AS u WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
    WHERE u.Id IN (SELECT DISTINCT UserMasterId FROM #Rb)
      AND u.EmployeeCode IN (SELECT DISTINCT EmployeeCode FROM #Rb);

    /* Stale rollback conflict: current must still equal NewValue (0) */
    IF EXISTS (
        SELECT 1
        FROM #Rb AS r
        INNER JOIN dbo.MST_UserMaster AS u ON u.Id = r.UserMasterId AND u.EmployeeCode = r.EmployeeCode
        WHERE (r.ColumnName = N'BranchId'        AND u.BranchId <> r.NewValue)
           OR (r.ColumnName = N'DivisionId'      AND ISNULL(u.DivisionId,-1) <> r.NewValue)
           OR (r.ColumnName = N'SubDepartmentId' AND ISNULL(u.SubDepartmentId,-1) <> r.NewValue)
    )
        THROW 56653, N'STOP: stale rollback conflict — current value no longer equals Audit.NewValue (0).', 1;

    DECLARE @Rb0001Div int, @Rb0006Div int, @Rb0034Br int, @Rb0034Div int, @Rb0034Sub int;

    UPDATE u SET u.DivisionId = r.OldValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Rb AS r
        ON r.UserMasterId = u.Id AND r.EmployeeCode = u.EmployeeCode
       AND r.ColumnName = N'DivisionId' AND r.EmployeeCode = N'0001'
    WHERE u.EmployeeCode = N'0001'
      AND u.DivisionId = r.NewValue
      AND r.NewValue = 0
      AND r.OldValue = 4;
    SET @Rb0001Div = @@ROWCOUNT;
    IF @Rb0001Div <> 1
        THROW 56654, N'STOP: rollback 0001.DivisionId rowcount is not 1.', 1;

    UPDATE u SET u.DivisionId = r.OldValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Rb AS r
        ON r.UserMasterId = u.Id AND r.EmployeeCode = u.EmployeeCode
       AND r.ColumnName = N'DivisionId' AND r.EmployeeCode = N'0006'
    WHERE u.EmployeeCode = N'0006'
      AND u.DivisionId = r.NewValue
      AND r.NewValue = 0
      AND r.OldValue = 4;
    SET @Rb0006Div = @@ROWCOUNT;
    IF @Rb0006Div <> 1
        THROW 56655, N'STOP: rollback 0006.DivisionId rowcount is not 1.', 1;

    UPDATE u SET u.BranchId = r.OldValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Rb AS r
        ON r.UserMasterId = u.Id AND r.EmployeeCode = u.EmployeeCode
       AND r.ColumnName = N'BranchId' AND r.EmployeeCode = N'0034'
    WHERE u.EmployeeCode = N'0034'
      AND u.BranchId = r.NewValue
      AND r.NewValue = 0
      AND r.OldValue = 19;
    SET @Rb0034Br = @@ROWCOUNT;
    IF @Rb0034Br <> 1
        THROW 56656, N'STOP: rollback 0034.BranchId rowcount is not 1.', 1;

    UPDATE u SET u.DivisionId = r.OldValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Rb AS r
        ON r.UserMasterId = u.Id AND r.EmployeeCode = u.EmployeeCode
       AND r.ColumnName = N'DivisionId' AND r.EmployeeCode = N'0034'
    WHERE u.EmployeeCode = N'0034'
      AND u.DivisionId = r.NewValue
      AND r.NewValue = 0
      AND r.OldValue = 4;
    SET @Rb0034Div = @@ROWCOUNT;
    IF @Rb0034Div <> 1
        THROW 56657, N'STOP: rollback 0034.DivisionId rowcount is not 1.', 1;

    UPDATE u SET u.SubDepartmentId = r.OldValue
    FROM dbo.MST_UserMaster AS u
    INNER JOIN #Rb AS r
        ON r.UserMasterId = u.Id AND r.EmployeeCode = u.EmployeeCode
       AND r.ColumnName = N'SubDepartmentId' AND r.EmployeeCode = N'0034'
    WHERE u.EmployeeCode = N'0034'
      AND u.SubDepartmentId = r.NewValue
      AND r.NewValue = 0
      AND r.OldValue = 110;
    SET @Rb0034Sub = @@ROWCOUNT;
    IF @Rb0034Sub <> 1
        THROW 56658, N'STOP: rollback 0034.SubDepartmentId rowcount is not 1.', 1;

    DECLARE @RbBranch int = @Rb0034Br;
    DECLARE @RbDiv int = @Rb0001Div + @Rb0006Div + @Rb0034Div;
    DECLARE @RbSub int = @Rb0034Sub;
    DECLARE @Restored int = @RbBranch + @RbDiv + @RbSub;

    IF @Restored <> 5 OR @RbBranch <> 1 OR @RbDiv <> 3 OR @RbSub <> 1
    BEGIN
        DECLARE @rbMsg nvarchar(400) =
            N'STOP: rollback restore count=' + CONVERT(varchar(10), @Restored) + N'; expected 5.';
        THROW 56659, @rbMsg, 1;
    END;

    UPDATE dbo.AUDIT_UserMasterOrgSentinelZero_B26
    SET ApplyStatus = N'ROLLED_BACK'
    WHERE RemediationRunId = @RemediationRunId
      AND ApplyStatus = N'APPLIED';

    IF @@ROWCOUNT <> 5
        THROW 56660, N'STOP: could not mark all 5 audit rows ROLLED_BACK.', 1;

    COMMIT TRANSACTION;

    SELECT
        N'ROLLBACK_COMMITTED' AS RollbackOutcome,
        @RemediationRunId AS RemediationRunId,
        @Restored AS RestoredMappings,
        @RbBranch AS RbBranch, @RbDiv AS RbDiv, @RbSub AS RbSub;
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
