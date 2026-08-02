/*
    Object:      [dbo].[usp_SaveActivitiesDraft]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE   PROCEDURE [dbo].[usp_SaveActivitiesDraft]
    @Updates dbo.TVP_ActivityUpdate READONLY,
    @LoginId nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @now datetime2 = SWITCHOFFSET(SYSDATETIMEOFFSET(), '+05:30'); -- IST

    BEGIN TRY
        BEGIN TRAN;

        UPDATE a
        SET
            a.TotalDistanceKM = ISNULL(u.TotalDistanceKM, a.TotalDistanceKM),
            a.CalculatedClaimAmount = ISNULL(u.CalculatedClaimAmount, a.CalculatedClaimAmount),
            a.ClaimStatus = 'Draft',
            a.ModifiedByUserId = @LoginId,
            a.ModifiedDateTime = @now
        FROM dbo.SalesDailyActivity a
        INNER JOIN @Updates u ON a.ActivityId = u.ActivityId
        WHERE a.Login_Id = @LoginId   -- <-- use Login_Id (matches table DDL)
          AND (a.ClaimStatus IS NULL OR a.ClaimStatus NOT IN ('Approved','Rejected'))
		  AND u.ActivityId = a.ActivityId;

        COMMIT;

        -- Return updated rows for caller
        SELECT @@ROWCOUNT AS UpdatedRows;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        DECLARE @err NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('usp_SaveActivitiesDraft failed: %s', 16, 1, @err);
    END CATCH
END;
