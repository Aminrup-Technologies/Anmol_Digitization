/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 4 — MST_Plant_Lines ROLLBACK
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.MST_Plant_Lines (AnmolDristi)
   WHAT:        Restore rows from RC018_Backup_MST_Plant_Lines_<RunTag>.
                Does not DROP indexes (including IX_MST_Plant_Lines_Report).
   NOTE:        Verification-only apply is a no-op vs backup. This script
                exists for the gated MERGE path. Set @BackupTable and @Confirm=1.
                Scoped DELETE only for keys inserted by MERGE and absent from backup.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @BackupTable SYSNAME = N'dbo.RC018_Backup_MST_Plant_Lines_YYYYMMDD_HHMMSS'; /* <-- replace */
DECLARE @Confirm     BIT     = 0;

PRINT N'RC-018 P1-T04 MST_Plant_Lines ROLLBACK | Backup=' + @BackupTable;

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

    DECLARE @Safety SYSNAME = N'dbo.RC018_PreRollback_MST_Plant_Lines_'
        + CONVERT(CHAR(8), GETDATE(), 112)
        + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
    DECLARE @Sql NVARCHAR(MAX) = N'SELECT * INTO ' + @Safety + N' FROM dbo.MST_Plant_Lines;';
    EXEC sys.sp_executesql @Sql;
    PRINT N'Pre-rollback safety copy: ' + @Safety;

    SET @Sql = N'
        UPDATE tgt SET
              tgt.plant_id      = src.plant_id
            , tgt.line_name     = src.line_name
            , tgt.local_name    = src.local_name
            , tgt.line_sap_code = src.line_sap_code
            , tgt.added_on      = src.added_on
            , tgt.view_status   = src.view_status
            , tgt.delete_status = src.delete_status
        FROM dbo.MST_Plant_Lines tgt
        INNER JOIN ' + @BackupTable + N' src ON src.line_id = tgt.line_id;';
    EXEC sys.sp_executesql @Sql;

    SET IDENTITY_INSERT dbo.MST_Plant_Lines ON;
    SET @Sql = N'
        INSERT INTO dbo.MST_Plant_Lines
            (id, plant_id, line_id, line_name, local_name, line_sap_code, added_on, view_status, delete_status)
        SELECT src.id, src.plant_id, src.line_id, src.line_name, src.local_name,
               src.line_sap_code, src.added_on, src.view_status, src.delete_status
        FROM ' + @BackupTable + N' src
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.MST_Plant_Lines t WHERE t.line_id = src.line_id
        );';
    EXEC sys.sp_executesql @Sql;
    SET IDENTITY_INSERT dbo.MST_Plant_Lines OFF;

    SET @Sql = N'
        DELETE tgt
        FROM dbo.MST_Plant_Lines AS tgt
        WHERE NOT EXISTS (
            SELECT 1 FROM ' + @BackupTable + N' AS src WHERE src.line_id = tgt.line_id
        );';
    EXEC sys.sp_executesql @Sql;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.MST_Plant_Lines')
          AND name = N'IX_MST_Plant_Lines_Report'
    )
        RAISERROR(N'BLOCKER: LIVE report index missing after rollback.', 16, 1);

    DECLARE @LiveCnt INT = (SELECT COUNT(*) FROM dbo.MST_Plant_Lines);
    DECLARE @BakCnt  INT;
    SET @Sql = N'SELECT @c = COUNT(*) FROM ' + @BackupTable;
    EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @BakCnt OUTPUT;

    IF @LiveCnt <> @BakCnt
        RAISERROR(N'ROLLBACK VERIFY FAIL: live %d <> backup %d', 16, 1, @LiveCnt, @BakCnt);

    COMMIT TRAN;
    PRINT N'ROLLBACK committed. LiveCount=' + CAST(@LiveCnt AS NVARCHAR(20));
    PRINT N'Safety copy: ' + @Safety;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    BEGIN TRY SET IDENTITY_INSERT dbo.MST_Plant_Lines OFF; END TRY BEGIN CATCH END CATCH;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P1-T04 ROLLBACK FAILED: %s', 16, 1, @Err);
END CATCH;
GO
