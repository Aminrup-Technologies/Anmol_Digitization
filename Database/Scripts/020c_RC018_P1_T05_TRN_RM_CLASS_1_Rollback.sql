/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 5 — TRN_RM_CLASS_1 ROLLBACK
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.TRN_RM_CLASS_1 (AnmolDristi)
   WHAT:        Restore shared columns and Material_Name from
                RC018_Backup_TRN_RM_CLASS_1_<RunTag>.
                Does not delete the 179 LIVE-only keys that were already in
                the backup. Scoped DELETE only for keys inserted by this RC
                and absent from the backup.
   NOTE:        Set @BackupTable and @Confirm = 1.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @BackupTable SYSNAME = N'dbo.RC018_Backup_TRN_RM_CLASS_1_YYYYMMDD_HHMMSS'; /* <-- replace */
DECLARE @Confirm     BIT     = 0;

PRINT N'RC-018 P1-T05 TRN_RM_CLASS_1 ROLLBACK | Backup=' + @BackupTable;

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

IF COL_LENGTH(N'dbo.TRN_RM_CLASS_1', N'Material_Name') IS NULL
 OR COL_LENGTH(@BackupTable, N'Material_Name') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: Material_Name missing on live or backup. Abort.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @Safety SYSNAME = N'dbo.RC018_PreRollback_TRN_RM_CLASS_1_'
        + CONVERT(CHAR(8), GETDATE(), 112)
        + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
    DECLARE @Sql NVARCHAR(MAX) = N'SELECT * INTO ' + @Safety + N' FROM dbo.TRN_RM_CLASS_1;';
    EXEC sys.sp_executesql @Sql;
    PRINT N'Pre-rollback safety copy: ' + @Safety;

    DECLARE @SetList NVARCHAR(MAX);
    SELECT @SetList = STUFF((
        SELECT N',tgt.' + QUOTENAME(c.name) + N' = src.' + QUOTENAME(c.name)
        FROM sys.columns c
        INNER JOIN sys.columns b
            ON b.object_id = OBJECT_ID(@BackupTable) AND b.name = c.name
        WHERE c.object_id = OBJECT_ID(N'dbo.TRN_RM_CLASS_1')
          AND c.is_computed = 0
          AND c.is_identity = 0
          AND c.name <> N'RMFID'
        ORDER BY c.column_id
        FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');

    SET @Sql = N'
        UPDATE tgt
        SET ' + @SetList + N'
        FROM dbo.TRN_RM_CLASS_1 tgt
        INNER JOIN ' + @BackupTable + N' src ON src.RMFID = tgt.RMFID;';
    EXEC sys.sp_executesql @Sql;

    /* Re-insert backup keys missing from live (includes Material_Name, explicit Id) */
    DECLARE @Cols NVARCHAR(MAX);
    SELECT @Cols = STUFF((
        SELECT N',' + QUOTENAME(c.name)
        FROM sys.columns c
        INNER JOIN sys.columns b
            ON b.object_id = OBJECT_ID(@BackupTable) AND b.name = c.name
        WHERE c.object_id = OBJECT_ID(N'dbo.TRN_RM_CLASS_1')
          AND c.is_computed = 0
          AND c.is_identity = 0
        ORDER BY c.column_id
        FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');

    SET IDENTITY_INSERT dbo.TRN_RM_CLASS_1 ON;
    SET @Sql = N'
        INSERT INTO dbo.TRN_RM_CLASS_1 (Id, ' + @Cols + N')
        SELECT src.Id, ' + REPLACE(@Cols, N'[', N'src.[') + N'
        FROM ' + @BackupTable + N' src
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.TRN_RM_CLASS_1 t WHERE t.RMFID = src.RMFID
        );';
    EXEC sys.sp_executesql @Sql;
    SET IDENTITY_INSERT dbo.TRN_RM_CLASS_1 OFF;

    SET @Sql = N'
        DELETE tgt
        FROM dbo.TRN_RM_CLASS_1 AS tgt
        WHERE NOT EXISTS (
            SELECT 1 FROM ' + @BackupTable + N' AS src WHERE src.RMFID = tgt.RMFID
        );';
    EXEC sys.sp_executesql @Sql;

    DECLARE @LiveCnt INT = (SELECT COUNT(*) FROM dbo.TRN_RM_CLASS_1);
    DECLARE @BakCnt  INT;
    SET @Sql = N'SELECT @c = COUNT(*) FROM ' + @BackupTable;
    EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @BakCnt OUTPUT;

    IF @LiveCnt <> @BakCnt
        RAISERROR(N'ROLLBACK VERIFY FAIL: live %d <> backup %d', 16, 1, @LiveCnt, @BakCnt);

    IF COL_LENGTH(N'dbo.TRN_RM_CLASS_1', N'Material_Name') IS NULL
        RAISERROR(N'BLOCKER: Material_Name missing after rollback.', 16, 1);

    COMMIT TRAN;
    PRINT N'ROLLBACK committed. LiveCount=' + CAST(@LiveCnt AS NVARCHAR(20));
    PRINT N'Safety copy: ' + @Safety;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    BEGIN TRY SET IDENTITY_INSERT dbo.TRN_RM_CLASS_1 OFF; END TRY BEGIN CATCH END CATCH;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P1-T05 ROLLBACK FAILED: %s', 16, 1, @Err);
END CATCH;
GO
