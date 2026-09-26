/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 7 — ApprovalRequestStatusHistory ROLLBACK
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.ApprovalRequestStatusHistory (AnmolDristi)
   WHAT:        Drop FK_ApprovalHistory_Detail and the five additive columns.
                Does not delete or update history rows.
                Refuses to drop a column that contains a non-null value.
   NOTE:        Set @Confirm = 1. Backup from the patch is a reference copy only.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Confirm BIT = 0;

PRINT N'RC-018 P3-T07 ApprovalRequestStatusHistory ROLLBACK';

IF @Confirm <> 1
BEGIN
    PRINT N'Confirm=0 — no changes.';
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @Before INT = (SELECT COUNT(*) FROM dbo.ApprovalRequestStatusHistory);

    IF EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = N'FK_ApprovalHistory_Detail'
          AND parent_object_id = OBJECT_ID(N'dbo.ApprovalRequestStatusHistory')
    )
    BEGIN
        ALTER TABLE dbo.ApprovalRequestStatusHistory DROP CONSTRAINT FK_ApprovalHistory_Detail;
        PRINT N'Dropped FK_ApprovalHistory_Detail.';
    END;

    DECLARE @Sql NVARCHAR(MAX);
    DECLARE @NonNull INT;

    IF COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApprovalRequestDetailId') IS NOT NULL
    BEGIN
        SET @Sql = N'SELECT @c = COUNT(*) FROM dbo.ApprovalRequestStatusHistory WHERE ApprovalRequestDetailId IS NOT NULL;';
        EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @NonNull OUTPUT;
        IF @NonNull > 0
            RAISERROR(N'BLOCKER: ApprovalRequestDetailId has %d non-null values. Column not dropped.', 16, 1, @NonNull);
        ALTER TABLE dbo.ApprovalRequestStatusHistory DROP COLUMN ApprovalRequestDetailId;
    END;

    IF COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ActivityId') IS NOT NULL
    BEGIN
        SET @Sql = N'SELECT @c = COUNT(*) FROM dbo.ApprovalRequestStatusHistory WHERE ActivityId IS NOT NULL;';
        EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @NonNull OUTPUT;
        IF @NonNull > 0
            RAISERROR(N'BLOCKER: ActivityId has %d non-null values. Column not dropped.', 16, 1, @NonNull);
        ALTER TABLE dbo.ApprovalRequestStatusHistory DROP COLUMN ActivityId;
    END;

    IF COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApproverLevel') IS NOT NULL
    BEGIN
        SET @Sql = N'SELECT @c = COUNT(*) FROM dbo.ApprovalRequestStatusHistory WHERE ApproverLevel IS NOT NULL;';
        EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @NonNull OUTPUT;
        IF @NonNull > 0
            RAISERROR(N'BLOCKER: ApproverLevel has %d non-null values. Column not dropped.', 16, 1, @NonNull);
        ALTER TABLE dbo.ApprovalRequestStatusHistory DROP COLUMN ApproverLevel;
    END;

    IF COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ActionType') IS NOT NULL
    BEGIN
        SET @Sql = N'SELECT @c = COUNT(*) FROM dbo.ApprovalRequestStatusHistory WHERE ActionType IS NOT NULL;';
        EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @NonNull OUTPUT;
        IF @NonNull > 0
            RAISERROR(N'BLOCKER: ActionType has %d non-null values. Column not dropped.', 16, 1, @NonNull);
        ALTER TABLE dbo.ApprovalRequestStatusHistory DROP COLUMN ActionType;
    END;

    IF COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'DetailVersion') IS NOT NULL
    BEGIN
        SET @Sql = N'SELECT @c = COUNT(*) FROM dbo.ApprovalRequestStatusHistory WHERE DetailVersion IS NOT NULL;';
        EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @NonNull OUTPUT;
        IF @NonNull > 0
            RAISERROR(N'BLOCKER: DetailVersion has %d non-null values. Column not dropped.', 16, 1, @NonNull);
        ALTER TABLE dbo.ApprovalRequestStatusHistory DROP COLUMN DetailVersion;
    END;

    DECLARE @After INT = (SELECT COUNT(*) FROM dbo.ApprovalRequestStatusHistory);
    IF @After <> @Before
        RAISERROR(N'BLOCKER: rollback changed row count %d -> %d.', 16, 1, @Before, @After);

    COMMIT TRAN;
    PRINT N'ROLLBACK committed. History rows preserved. Count=' + CAST(@After AS NVARCHAR(20));
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P3-T07 ROLLBACK FAILED: %s', 16, 1, @Err);
END CATCH;
GO
