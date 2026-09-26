/* ============================================================================
   NAME:        RC-018 Phase 2 / Table 6 — PolicyException ROLLBACK
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.PolicyException (AnmolDristi)
   WHAT:        Restore rows from RC018_Backup_PolicyException_<RunTag>.
                Removes MERGE inserts that were absent from the backup.
                Optionally drops the index and checks this RC added.
   NOTE:        Set @BackupTable and @Confirm = 1.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @BackupTable     SYSNAME = N'dbo.RC018_Backup_PolicyException_YYYYMMDD_HHMMSS'; /* <-- replace */
DECLARE @Confirm         BIT     = 0;
DECLARE @DropAddedObjects BIT    = 0; /* 1 = drop index + checks added by this RC */

PRINT N'RC-018 P2-T06 PolicyException ROLLBACK | Backup=' + @BackupTable;

IF @Confirm <> 1
BEGIN
    PRINT N'Confirm=0 — no changes.';
    RETURN;
END;

IF OBJECT_ID(@BackupTable, N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table not found. Abort.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @Safety SYSNAME = N'dbo.RC018_PreRollback_PolicyException_'
        + CONVERT(CHAR(8), GETDATE(), 112)
        + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
    DECLARE @Sql NVARCHAR(MAX) = N'SELECT * INTO ' + @Safety + N' FROM dbo.PolicyException;';
    EXEC sys.sp_executesql @Sql;
    PRINT N'Pre-rollback safety copy: ' + @Safety;

    SET @Sql = N'
        UPDATE tgt SET
              tgt.EmployeeCode = src.EmployeeCode
            , tgt.RegionId = src.RegionId
            , tgt.BranchId = src.BranchId
            , tgt.GradeId = src.GradeId
            , tgt.DesignationId = src.DesignationId
            , tgt.PolicyJson = src.PolicyJson
            , tgt.Priority = src.Priority
            , tgt.EffectiveFrom = src.EffectiveFrom
            , tgt.EffectiveTo = src.EffectiveTo
            , tgt.IsActive = src.IsActive
            , tgt.CreatedBy = src.CreatedBy
            , tgt.CreatedOn = src.CreatedOn
            , tgt.ModifiedBy = src.ModifiedBy
            , tgt.ModifiedOn = src.ModifiedOn
        FROM dbo.PolicyException tgt
        INNER JOIN ' + @BackupTable + N' src ON src.ExceptionId = tgt.ExceptionId;';
    EXEC sys.sp_executesql @Sql;

    SET IDENTITY_INSERT dbo.PolicyException ON;
    SET @Sql = N'
        INSERT INTO dbo.PolicyException
        (
            ExceptionId, EmployeeCode, RegionId, BranchId, GradeId, DesignationId,
            PolicyJson, Priority, EffectiveFrom, EffectiveTo, IsActive,
            CreatedBy, CreatedOn, ModifiedBy, ModifiedOn
        )
        SELECT
            src.ExceptionId, src.EmployeeCode, src.RegionId, src.BranchId, src.GradeId, src.DesignationId,
            src.PolicyJson, src.Priority, src.EffectiveFrom, src.EffectiveTo, src.IsActive,
            src.CreatedBy, src.CreatedOn, src.ModifiedBy, src.ModifiedOn
        FROM ' + @BackupTable + N' src
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.PolicyException t WHERE t.ExceptionId = src.ExceptionId
        );';
    EXEC sys.sp_executesql @Sql;
    SET IDENTITY_INSERT dbo.PolicyException OFF;

    SET @Sql = N'
        DELETE tgt
        FROM dbo.PolicyException AS tgt
        WHERE NOT EXISTS (
            SELECT 1 FROM ' + @BackupTable + N' AS src WHERE src.ExceptionId = tgt.ExceptionId
        );';
    EXEC sys.sp_executesql @Sql;

    DECLARE @LiveCnt INT = (SELECT COUNT(*) FROM dbo.PolicyException);
    DECLARE @BakCnt  INT;
    SET @Sql = N'SELECT @c = COUNT(*) FROM ' + @BackupTable;
    EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @BakCnt OUTPUT;

    IF @LiveCnt <> @BakCnt
        RAISERROR(N'ROLLBACK VERIFY FAIL: live %d <> backup %d', 16, 1, @LiveCnt, @BakCnt);

    COMMIT TRAN;

    IF @DropAddedObjects = 1
    BEGIN
        IF EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_PolicyException_Employee_Active_Dates'
              AND object_id = OBJECT_ID(N'dbo.PolicyException')
        )
            DROP INDEX IX_PolicyException_Employee_Active_Dates ON dbo.PolicyException;

        IF EXISTS (
            SELECT 1 FROM sys.check_constraints
            WHERE name = N'CK_PolicyException_EffectiveDates'
              AND parent_object_id = OBJECT_ID(N'dbo.PolicyException')
        )
            ALTER TABLE dbo.PolicyException DROP CONSTRAINT CK_PolicyException_EffectiveDates;

        IF EXISTS (
            SELECT 1 FROM sys.check_constraints
            WHERE name = N'CK_PolicyException_Priority'
              AND parent_object_id = OBJECT_ID(N'dbo.PolicyException')
        )
            ALTER TABLE dbo.PolicyException DROP CONSTRAINT CK_PolicyException_Priority;

        PRINT N'Dropped index and check constraints added by this RC.';
    END;

    PRINT N'ROLLBACK committed. LiveCount=' + CAST(@LiveCnt AS NVARCHAR(20));
    PRINT N'Safety copy: ' + @Safety;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    BEGIN TRY SET IDENTITY_INSERT dbo.PolicyException OFF; END TRY BEGIN CATCH END CATCH;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P2-T06 ROLLBACK FAILED: %s', 16, 1, @Err);
END CATCH;
GO
