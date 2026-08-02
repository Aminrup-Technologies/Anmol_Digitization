/*
    Object:      [dbo].[usp_SaveMonthlyDraftSummary]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_SaveMonthlyDraftSummary]
    @LoginId NVARCHAR(100),
    @YearNo INT,
    @MonthNo INT,
    @TotalKM DECIMAL(18,2),
    @TotalClaimAmount DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRAN;

        DECLARE @Now DATETIME = DATEADD(minute, 330, SYSUTCDATETIME()); -- IST

        -- Ensure temp table exists BEFORE MERGE OUTPUT writes to it
        IF OBJECT_ID('tempdb..#MergeOutput') IS NOT NULL
            DROP TABLE #MergeOutput;

        CREATE TABLE #MergeOutput (MergeAction NVARCHAR(20));

        -- Upsert into SalesMonthlyDraftSummary
        MERGE dbo.SalesMonthlyDraftSummary AS target
        USING (SELECT @LoginId AS Login_Id, @YearNo AS YearNo, @MonthNo AS MonthNo) AS src
            ON target.Login_Id = src.Login_Id
               AND target.YearNo = src.YearNo
               AND target.MonthNo = src.MonthNo
        WHEN MATCHED THEN
            UPDATE SET TotalKM = @TotalKM,
                       TotalClaimAmount = @TotalClaimAmount,
                       UpdatedOn = @Now
        WHEN NOT MATCHED THEN
            INSERT (Login_Id, YearNo, MonthNo, TotalKM, TotalClaimAmount, CreatedOn)
            VALUES (@LoginId, @YearNo, @MonthNo, @TotalKM, @TotalClaimAmount, @Now)
        OUTPUT $action INTO #MergeOutput;  -- safe now because table exists

        DECLARE @RowsUpserted INT = (SELECT COUNT(*) FROM #MergeOutput);

        -- Gather ActivityIds for this login/year/month
        -- Adjust predicate if SalesDailyActivity stores YearNo/MonthNo differently
        ;WITH UserActivities AS
        (
            SELECT ActivityId
            FROM dbo.SalesDailyActivity
            WHERE Login_Id = @LoginId
              AND YEAR(ActivityDate) = @YearNo
              AND MONTH(ActivityDate) = @MonthNo
        )
        -- Update related expenses to 'Draft'
        UPDATE E
        SET ClaimStatus = 'Draft'
        FROM dbo.SalesDailyExpense E
        INNER JOIN UserActivities UA ON UA.ActivityId = E.ActivityId;

        DECLARE @ExpensesUpdated INT = @@ROWCOUNT;

        COMMIT TRAN;

        -- Return diagnostics
        SELECT @RowsUpserted AS RowsUpserted, @ExpensesUpdated AS ExpensesUpdated;
    END TRY
    BEGIN CATCH
        -- If transaction is still active, roll it back
        IF XACT_STATE() <> 0
            ROLLBACK TRAN;

        -- Re-throw the original error so the caller gets full details
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorNumber INT = ERROR_NUMBER();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        -- Use THROW to preserve original error context (SQL Server 2012+)
        THROW;
    END CATCH;
END
