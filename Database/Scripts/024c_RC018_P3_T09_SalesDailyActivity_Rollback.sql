/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 9 — SalesDailyActivity ROLLBACK
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.SalesDailyActivity (AnmolDristi)
   WHAT:        Restore CK_SalesDailyActivity_ClaimStatus to the Phase 0 LIVE
                definition (no Returned) only when no row is Returned.
                Does not delete or update activity rows.
   NOTE:        Set @Confirm = 1.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Confirm BIT = 0;
DECLARE @ReturnedRows INT;

PRINT N'RC-018 P3-T09 SalesDailyActivity ROLLBACK';

IF @Confirm <> 1
BEGIN
    PRINT N'Confirm=0 — no changes.';
    RETURN;
END;

SELECT @ReturnedRows = COUNT(*)
FROM dbo.SalesDailyActivity
WHERE ClaimStatus = N'Returned';

IF @ReturnedRows > 0
BEGIN
    RAISERROR(N'BLOCKER: %d rows are ClaimStatus=Returned. Restoring the old check would fail WITH CHECK. No change made.', 16, 1, @ReturnedRows);
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @Before INT = (SELECT COUNT(*) FROM dbo.SalesDailyActivity);

    IF EXISTS (
        SELECT 1 FROM sys.check_constraints
        WHERE name = N'CK_SalesDailyActivity_ClaimStatus'
          AND parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivity')
    )
        ALTER TABLE dbo.SalesDailyActivity DROP CONSTRAINT CK_SalesDailyActivity_ClaimStatus;

    ALTER TABLE dbo.SalesDailyActivity WITH CHECK
        ADD CONSTRAINT CK_SalesDailyActivity_ClaimStatus
        CHECK ([ClaimStatus]='Rejected' OR [ClaimStatus]='Approved' OR [ClaimStatus]='Submitted' OR [ClaimStatus]='Draft');

    DECLARE @After INT = (SELECT COUNT(*) FROM dbo.SalesDailyActivity);
    IF @After <> @Before
        RAISERROR(N'BLOCKER: rollback changed row count.', 16, 1);

    COMMIT TRAN;
    PRINT N'Constraint restored without Returned. Rows preserved=' + CAST(@After AS NVARCHAR(20));
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P3-T09 ROLLBACK FAILED: %s', 16, 1, @Err);
END CATCH;
GO
