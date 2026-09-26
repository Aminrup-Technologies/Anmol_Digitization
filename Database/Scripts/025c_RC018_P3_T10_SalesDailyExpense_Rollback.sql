/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 10 — SalesDailyExpense ROLLBACK
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.SalesDailyExpense (AnmolDristi)
   WHAT:        Drop the four additive approval columns only while every value
                in each column is NULL. Does not delete rows or touch receipts.
   NOTE:        Set @Confirm = 1.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Confirm BIT = 0;

PRINT N'RC-018 P3-T10 SalesDailyExpense ROLLBACK';

IF @Confirm <> 1
BEGIN
    PRINT N'Confirm=0 — no changes.';
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @Before INT = (SELECT COUNT(*) FROM dbo.SalesDailyExpense);
    DECLARE @Sql NVARCHAR(MAX);
    DECLARE @NonNull INT;

    IF COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovalStatus') IS NOT NULL
    BEGIN
        SET @Sql = N'SELECT @c = COUNT(*) FROM dbo.SalesDailyExpense WHERE ApprovalStatus IS NOT NULL;';
        EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @NonNull OUTPUT;
        IF @NonNull > 0
            RAISERROR(N'BLOCKER: ApprovalStatus has %d non-null values. Column not dropped.', 16, 1, @NonNull);
        ALTER TABLE dbo.SalesDailyExpense DROP COLUMN ApprovalStatus;
    END;

    IF COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovalRemarks') IS NOT NULL
    BEGIN
        SET @Sql = N'SELECT @c = COUNT(*) FROM dbo.SalesDailyExpense WHERE ApprovalRemarks IS NOT NULL;';
        EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @NonNull OUTPUT;
        IF @NonNull > 0
            RAISERROR(N'BLOCKER: ApprovalRemarks has %d non-null values. Column not dropped.', 16, 1, @NonNull);
        ALTER TABLE dbo.SalesDailyExpense DROP COLUMN ApprovalRemarks;
    END;

    IF COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovedBy') IS NOT NULL
    BEGIN
        SET @Sql = N'SELECT @c = COUNT(*) FROM dbo.SalesDailyExpense WHERE ApprovedBy IS NOT NULL;';
        EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @NonNull OUTPUT;
        IF @NonNull > 0
            RAISERROR(N'BLOCKER: ApprovedBy has %d non-null values. Column not dropped.', 16, 1, @NonNull);
        ALTER TABLE dbo.SalesDailyExpense DROP COLUMN ApprovedBy;
    END;

    IF COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovedOn') IS NOT NULL
    BEGIN
        SET @Sql = N'SELECT @c = COUNT(*) FROM dbo.SalesDailyExpense WHERE ApprovedOn IS NOT NULL;';
        EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @NonNull OUTPUT;
        IF @NonNull > 0
            RAISERROR(N'BLOCKER: ApprovedOn has %d non-null values. Column not dropped.', 16, 1, @NonNull);
        ALTER TABLE dbo.SalesDailyExpense DROP COLUMN ApprovedOn;
    END;

    DECLARE @After INT = (SELECT COUNT(*) FROM dbo.SalesDailyExpense);
    IF @After <> @Before
        RAISERROR(N'BLOCKER: rollback changed row count %d -> %d.', 16, 1, @Before, @After);

    COMMIT TRAN;
    PRINT N'ROLLBACK committed. Expense rows preserved. Count=' + CAST(@After AS NVARCHAR(20));
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P3-T10 ROLLBACK FAILED: %s', 16, 1, @Err);
END CATCH;
GO
