/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 3 — MST_EmployeeTolerance ROLLBACK
   WHEN:        2026-09-24
   TARGET:      LIVE dbo.MST_EmployeeTolerance (AnmolDristi)
   WHAT:        Restore from RC018_Backup_MST_EmployeeTolerance_<RunTag>.
                Removes MERGE-inserted rows absent from backup; restores
                matched rows; optionally drops MaxKmModificationCount.
   NOTE:        Set @BackupTable and @Confirm=1. Scoped DELETE of post-MERGE
                inserts only (rollback path — not part of forward sync).
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @BackupTable              SYSNAME = N'dbo.RC018_Backup_MST_EmployeeTolerance_YYYYMMDD_HHMMSS'; /* <-- replace */
DECLARE @Confirm                  BIT     = 0;
DECLARE @DropMaxKmModificationCount BIT   = 0;  /* 1 = DROP COLUMN after data restore */

PRINT N'RC-018 P1-T03 MST_EmployeeTolerance ROLLBACK | Backup=' + @BackupTable;

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

    DECLARE @Safety SYSNAME = N'dbo.RC018_PreRollback_MST_EmployeeTolerance_'
        + CONVERT(CHAR(8), GETDATE(), 112)
        + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
    DECLARE @Sql NVARCHAR(MAX) =
        N'SELECT * INTO ' + @Safety + N' FROM dbo.MST_EmployeeTolerance;';
    EXEC sys.sp_executesql @Sql;
    PRINT N'Pre-rollback safety copy: ' + @Safety;

    /* Restore overlapping rows from backup (when backup has data) */
    IF COL_LENGTH(N'dbo.MST_EmployeeTolerance', N'MaxKmModificationCount') IS NOT NULL
       AND COL_LENGTH(@BackupTable, N'MaxKmModificationCount') IS NOT NULL
    BEGIN
        SET @Sql = N'
            UPDATE tgt SET
                  tgt.EmployeeCode = src.EmployeeCode
                , tgt.MinAllowedKM = src.MinAllowedKM
                , tgt.MaxAllowedKM = src.MaxAllowedKM
                , tgt.EffectiveFrom = src.EffectiveFrom
                , tgt.EffectiveTo = src.EffectiveTo
                , tgt.CreatedBy = src.CreatedBy
                , tgt.CreatedDateTime = src.CreatedDateTime
                , tgt.ModifiedBy = src.ModifiedBy
                , tgt.ModifiedDateTime = src.ModifiedDateTime
                , tgt.IsActive = src.IsActive
                , tgt.MaxKmModificationCount = src.MaxKmModificationCount
            FROM dbo.MST_EmployeeTolerance tgt
            INNER JOIN ' + @BackupTable + N' src ON src.ToleranceId = tgt.ToleranceId;';
        EXEC sys.sp_executesql @Sql;
    END
    ELSE IF COL_LENGTH(N'dbo.MST_EmployeeTolerance', N'MaxKmModificationCount') IS NULL
            OR COL_LENGTH(@BackupTable, N'MaxKmModificationCount') IS NULL
    BEGIN
        SET @Sql = N'
            UPDATE tgt SET
                  tgt.EmployeeCode = src.EmployeeCode
                , tgt.MinAllowedKM = src.MinAllowedKM
                , tgt.MaxAllowedKM = src.MaxAllowedKM
                , tgt.EffectiveFrom = src.EffectiveFrom
                , tgt.EffectiveTo = src.EffectiveTo
                , tgt.CreatedBy = src.CreatedBy
                , tgt.CreatedDateTime = src.CreatedDateTime
                , tgt.ModifiedBy = src.ModifiedBy
                , tgt.ModifiedDateTime = src.ModifiedDateTime
                , tgt.IsActive = src.IsActive
            FROM dbo.MST_EmployeeTolerance tgt
            INNER JOIN ' + @BackupTable + N' src ON src.ToleranceId = tgt.ToleranceId;';
        EXEC sys.sp_executesql @Sql;
    END;

    /* Re-insert backup rows missing from live */
    SET IDENTITY_INSERT dbo.MST_EmployeeTolerance ON;
    IF COL_LENGTH(N'dbo.MST_EmployeeTolerance', N'MaxKmModificationCount') IS NOT NULL
       AND COL_LENGTH(@BackupTable, N'MaxKmModificationCount') IS NOT NULL
        SET @Sql = N'
            INSERT INTO dbo.MST_EmployeeTolerance
            (ToleranceId, EmployeeCode, MinAllowedKM, MaxAllowedKM, EffectiveFrom, EffectiveTo,
             CreatedBy, CreatedDateTime, ModifiedBy, ModifiedDateTime, IsActive, MaxKmModificationCount)
            SELECT src.ToleranceId, src.EmployeeCode, src.MinAllowedKM, src.MaxAllowedKM,
                   src.EffectiveFrom, src.EffectiveTo, src.CreatedBy, src.CreatedDateTime,
                   src.ModifiedBy, src.ModifiedDateTime, src.IsActive, src.MaxKmModificationCount
            FROM ' + @BackupTable + N' src
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.MST_EmployeeTolerance t WHERE t.ToleranceId = src.ToleranceId
            );';
    ELSE
        SET @Sql = N'
            INSERT INTO dbo.MST_EmployeeTolerance
            (ToleranceId, EmployeeCode, MinAllowedKM, MaxAllowedKM, EffectiveFrom, EffectiveTo,
             CreatedBy, CreatedDateTime, ModifiedBy, ModifiedDateTime, IsActive)
            SELECT src.ToleranceId, src.EmployeeCode, src.MinAllowedKM, src.MaxAllowedKM,
                   src.EffectiveFrom, src.EffectiveTo, src.CreatedBy, src.CreatedDateTime,
                   src.ModifiedBy, src.ModifiedDateTime, src.IsActive
            FROM ' + @BackupTable + N' src
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.MST_EmployeeTolerance t WHERE t.ToleranceId = src.ToleranceId
            );';
    EXEC sys.sp_executesql @Sql;
    SET IDENTITY_INSERT dbo.MST_EmployeeTolerance OFF;

    /* Remove rows inserted by MERGE that are absent from backup (Phase 0 LIVE was empty) */
    SET @Sql = N'
        DELETE tgt
        FROM dbo.MST_EmployeeTolerance AS tgt
        WHERE NOT EXISTS (
            SELECT 1 FROM ' + @BackupTable + N' AS src WHERE src.ToleranceId = tgt.ToleranceId
        );';
    EXEC sys.sp_executesql @Sql;

    DECLARE @LiveCnt INT = (SELECT COUNT(*) FROM dbo.MST_EmployeeTolerance);
    DECLARE @BakCnt  INT;
    SET @Sql = N'SELECT @c = COUNT(*) FROM ' + @BackupTable;
    EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @BakCnt OUTPUT;

    IF @LiveCnt <> @BakCnt
        RAISERROR(N'ROLLBACK VERIFY FAIL: live %d <> backup %d', 16, 1, @LiveCnt, @BakCnt);

    COMMIT TRAN;

    IF @DropMaxKmModificationCount = 1
       AND COL_LENGTH(N'dbo.MST_EmployeeTolerance', N'MaxKmModificationCount') IS NOT NULL
    BEGIN
        IF EXISTS (
            SELECT 1 FROM sys.check_constraints
            WHERE name = N'CK_MST_EmployeeTolerance_MaxKmModificationCount'
              AND parent_object_id = OBJECT_ID(N'dbo.MST_EmployeeTolerance')
        )
            ALTER TABLE dbo.MST_EmployeeTolerance
                DROP CONSTRAINT CK_MST_EmployeeTolerance_MaxKmModificationCount;

        IF EXISTS (
            SELECT 1 FROM sys.default_constraints
            WHERE name = N'DF_MST_EmployeeTolerance_MaxKmModificationCount'
              AND parent_object_id = OBJECT_ID(N'dbo.MST_EmployeeTolerance')
        )
            ALTER TABLE dbo.MST_EmployeeTolerance
                DROP CONSTRAINT DF_MST_EmployeeTolerance_MaxKmModificationCount;

        ALTER TABLE dbo.MST_EmployeeTolerance DROP COLUMN MaxKmModificationCount;
        PRINT N'DROP COLUMN MaxKmModificationCount completed.';
    END;

    PRINT N'ROLLBACK committed. LiveCount=' + CAST(@LiveCnt AS NVARCHAR(20));
    PRINT N'Safety copy: ' + @Safety;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    BEGIN TRY SET IDENTITY_INSERT dbo.MST_EmployeeTolerance OFF; END TRY BEGIN CATCH END CATCH;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P1-T03 ROLLBACK FAILED: %s', 16, 1, @Err);
END CATCH;
GO
