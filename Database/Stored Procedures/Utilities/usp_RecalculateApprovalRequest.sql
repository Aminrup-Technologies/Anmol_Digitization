/*
    Object:      [dbo].[usp_RecalculateApprovalRequest]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

/******************************************************************************
Procedure Name : usp_RecalculateApprovalRequest

Purpose
-------
Recalculate all financial and activity summary values for an Approval Request.
******************************************************************************/

CREATE   PROCEDURE dbo.usp_RecalculateApprovalRequest
(
      @ApprovalRequestId INT
)
AS
BEGIN

    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -------------------------------------------------------------------------
    -- Variable Declaration
    -------------------------------------------------------------------------

    DECLARE
          @TotalActivities           INT = 0
        , @PendingActivities         INT = 0
        , @ApprovedActivities        INT = 0
        , @ReturnedActivities        INT = 0
        , @RejectedActivities        INT = 0

        , @ClaimedDA                 DECIMAL(18,2) = 0
        , @ApprovedDA                DECIMAL(18,2) = 0
        , @RejectedDA                DECIMAL(18,2) = 0

        , @ClaimedHotel              DECIMAL(18,2) = 0
        , @ApprovedHotel             DECIMAL(18,2) = 0
        , @RejectedHotel             DECIMAL(18,2) = 0

        , @ClaimedTravel             DECIMAL(18,2) = 0
        , @ApprovedTravel            DECIMAL(18,2) = 0
        , @RejectedTravel            DECIMAL(18,2) = 0

        , @ClaimedKM                 DECIMAL(18,2) = 0
        , @ApprovedKM                DECIMAL(18,2) = 0

        , @ApprovedExpenseAmount     DECIMAL(18,2) = 0
        , @TotalApprovedAmount       DECIMAL(18,2) = 0
		, @ApprovedKMClaimAmount     DECIMAL(18,2)=0
        , @FinalApprovedAmount       DECIMAL(18,2)=0;

    -------------------------------------------------------------------------
    -- Validation
    -------------------------------------------------------------------------

    IF @ApprovalRequestId IS NULL
    BEGIN
        THROW 50001,
              'ApprovalRequestId cannot be NULL.',
              1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId=@ApprovalRequestId
    )
    BEGIN
        THROW 50002,
              'Approval Request not found.',
              1;
    END;

    -------------------------------------------------------------------------
    -- Start Transaction
    -------------------------------------------------------------------------

    BEGIN TRY

        BEGIN TRANSACTION;

        ---------------------------------------------------------------------
        -- Next Section
        -- Aggregate Activity & Financial Data
        ---------------------------------------------------------------------
		-- Aggregate Activity Status Summary
		---------------------------------------------------------------------

		SELECT
			  @TotalActivities =
					COUNT(*)

			, @PendingActivities =
					SUM(CASE WHEN DetailStatus = 'Pending' THEN 1 ELSE 0 END)

			, @ApprovedActivities =
					SUM(CASE WHEN DetailStatus = 'Approved' THEN 1 ELSE 0 END)

			, @ReturnedActivities =
					SUM(CASE WHEN DetailStatus = 'Returned' THEN 1 ELSE 0 END)

			, @RejectedActivities =
					SUM(CASE WHEN DetailStatus = 'Rejected' THEN 1 ELSE 0 END)

		FROM dbo.SalesApprovalRequestDetail
		WHERE ApprovalRequestId = @ApprovalRequestId;


		SELECT
			@ClaimedDA      = SUM(ClaimedDA),
			@ApprovedDA     = SUM(ApprovedDA),
			@RejectedDA     = SUM(RejectedDA),

			@ClaimedHotel   = SUM(ClaimedHotel),
			@ApprovedHotel  = SUM(ApprovedHotel),
			@RejectedHotel  = SUM(RejectedHotel),

			@ClaimedTravel  = SUM(ClaimedTravel),
			@ApprovedTravel = SUM(ApprovedTravel),
			@RejectedTravel = SUM(RejectedTravel)

		FROM dbo.SalesDailyActivity
		WHERE ApprovalRequestId=@ApprovalRequestId;


		        ---------------------------------------------------------------------
        -- Calculate Request Financial Totals
        ---------------------------------------------------------------------

        ---------------------------------------------------------------
        -- Approved Expense Total
        ---------------------------------------------------------------

        SET @ApprovedExpenseAmount =
                ISNULL(@ApprovedDA,0)
              + ISNULL(@ApprovedHotel,0)
              + ISNULL(@ApprovedTravel,0);

        ---------------------------------------------------------------
        -- Approved KM Claim Amount
        ---------------------------------------------------------------

        /*
            NOTE

            Approved KM reimbursement should come from the
            ApprovedCalculatedClaimAmount maintained in
            SalesDailyActivity.

            Until ApprovedCalculatedClaimAmount is introduced,
            CalculatedClaimAmount represents the approved value.
        */

        ---------------------------------------------------------------
        -- Final Claim Amount
        ---------------------------------------------------------------
        -- Validation
        ---------------------------------------------------------------------

        IF @ApprovedExpenseAmount < 0
        BEGIN

            THROW 50020,
                  'Approved Expense Amount cannot be negative.',
                  1;

        END;

        IF @TotalApprovedAmount < 0
        BEGIN

            THROW 50021,
                  'Total Approved Amount cannot be negative.',
                  1;

        END;

		SELECT
			@ApprovedKM = ISNULL(SUM(ApprovedDistanceKM), 0),
			@ApprovedKMClaimAmount = ISNULL(SUM(ApprovedCalculatedClaimAmount), 0)
		FROM dbo.SalesDailyActivity
		WHERE ApprovalRequestId = @ApprovalRequestId
		  AND IsDeleted = 0;

		SET @FinalApprovedAmount = @ApprovedKMClaimAmount
								 + @ApprovedExpenseAmount;

		        ---------------------------------------------------------------------
        -- Update Approval Request Header
        ---------------------------------------------------------------------

        UPDATE dbo.SalesApprovalRequest
        SET

              TotalActivities         = @TotalActivities

            , PendingActivities       = @PendingActivities

            , ApprovedActivities      = @ApprovedActivities

            , RejectedActivities      = @RejectedActivities

            ------------------------------------------------------------
            -- Financial Summary
            ------------------------------------------------------------

            , ApprovedDAAmount        = @ApprovedDA

            , ApprovedHotelAmount     = @ApprovedHotel

            , ApprovedTravelAmount    = @ApprovedTravel

            , ApprovedKM              = @ApprovedKM

            , ApprovedClaimAmount     = @ApprovedKMClaimAmount

            ------------------------------------------------------------
            -- Final Summary
            ------------------------------------------------------------

            , FinalApprovedAmount     = @FinalApprovedAmount

            , TotalDeductionAmount = ISNULL(OriginalClaimAmount,0) - ISNULL(@FinalApprovedAmount,0)

            ------------------------------------------------------------
            -- Audit
            ------------------------------------------------------------

            , ModifiedOn              = SYSUTCDATETIME()

        WHERE ApprovalRequestId = @ApprovalRequestId;

	        COMMIT TRANSACTION;


			        ---------------------------------------------------------------------
        -- Return Updated Header
        ---------------------------------------------------------------------

        SELECT

              ApprovalRequestId

            , ApprovedKM

            , ApprovedClaimAmount

            , ApprovedDAAmount

            , ApprovedHotelAmount

            , ApprovedTravelAmount

            , ApprovedExpenseAmount

            , TotalApprovedAmount

            , FinalApprovedAmount

            , TotalDeductionAmount

            , TotalActivities

            , PendingActivities

            , ApprovedActivities

            , RejectedActivities

        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId;

		    END TRY

    BEGIN CATCH

        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE

              @ErrorMessage NVARCHAR(4000)
            , @ErrorNumber INT
            , @ErrorSeverity INT
            , @ErrorState INT;

        SELECT

              @ErrorMessage = ERROR_MESSAGE()
            , @ErrorNumber = ERROR_NUMBER()
            , @ErrorSeverity = ERROR_SEVERITY()
            , @ErrorState = ERROR_STATE();

        THROW @ErrorNumber,
              @ErrorMessage,
              @ErrorState;

    END CATCH

END
