/*
    Object:      [dbo].[usp_UpdateActivityKM_WithHistory]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE   PROCEDURE [dbo].[usp_UpdateActivityKM_WithHistory]
    @ActivityId INT,
    @NewKM DECIMAL(10,2),
    @ChangedBy NVARCHAR(100),
    @Reason NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

		DECLARE @ApprovalRequestId INT;
		DECLARE @AccountsStatus NVARCHAR(50);

		SELECT TOP 1
			@ApprovalRequestId = ApprovalRequestId
		FROM dbo.SalesApprovalRequestDetail
		WHERE ActivityId = @ActivityId;

		SELECT TOP 1
			@AccountsStatus = Status
		FROM dbo.SalesApprovalRequestApprover
		WHERE ApprovalRequestId = @ApprovalRequestId
		  AND ApproverRole = 'Accounts';

		-- 🔒 Lock if Accounts has acted
		IF @AccountsStatus IS NOT NULL
		   AND @AccountsStatus <> 'Pending'
		BEGIN
			RAISERROR('KM modification is locked as Accounts has already processed the request.', 16, 1);
			ROLLBACK TRAN;
			RETURN;
		END

        DECLARE @OldKM DECIMAL(10,2);
        SELECT @OldKM = TotalDistanceKM FROM dbo.SalesDailyActivity WHERE ActivityId = @ActivityId;

        IF @OldKM IS NULL
        BEGIN
            RAISERROR('Activity not found', 16, 1);
            ROLLBACK TRAN;
            RETURN;
        END

        -- Only insert history if value actually changed
        IF @OldKM <> @NewKM
        BEGIN
            INSERT INTO dbo.SalesDailyActivityKMHistory
                (ActivityId, OldKM, NewKM, ModifiedBy, Reason)
            VALUES
                (@ActivityId, @OldKM, @NewKM, @ChangedBy, @Reason);
        END

        -- Update the main activity row
        UPDATE dbo.SalesDailyActivity
        SET TotalDistanceKM = @NewKM,
            ModifiedDateTime = SYSDATETIME()
        WHERE ActivityId = @ActivityId;

        -- Update any approval detail snapshots for this activity (if present)
		UPDATE d
		SET 
			d.Snapshot_TotalDistanceKM = @NewKM,
			d.Snapshot_CalculatedClaimAmount = 
				CASE 
					WHEN @NewKM IS NOT NULL 
						THEN CAST(@NewKM * 3.75 AS DECIMAL(10,2))
					ELSE d.Snapshot_CalculatedClaimAmount
				END
		FROM dbo.SalesApprovalRequestDetail d
		WHERE d.ActivityId = @ActivityId;

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrMsg, 16, 1);
    END CATCH
END
