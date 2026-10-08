/*
    Object:      [dbo].[usp_SubmitSalesDailyActivities]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

/*==============================================================================
 Part 1 : Header, Validation & Mode Detection
 Version : Approval Engine V2 (Revised)
==============================================================================*/

CREATE PROCEDURE dbo.usp_SubmitSalesDailyActivities

    @Updates dbo.TVP_ActivityUpdate READONLY,
    @LoginId NVARCHAR(100),
    @Year INT,
    @Month INT,
    @Remarks NVARCHAR(1000)=NULL,
    @L1Code NVARCHAR(100),
    @L2Code NVARCHAR(100)=NULL,
    @L3Code NVARCHAR(100)=NULL,
    @AuditorCode NVARCHAR(100)

AS
BEGIN

    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2 = SWITCHOFFSET(SYSDATETIMEOFFSET(),'+05:30');
    DECLARE @ApprovalRequestId INT;
    DECLARE @SubmissionMode VARCHAR(20);
    DECLARE @ExistingRequestCount INT;

	DECLARE @TotalDistanceKM            DECIMAL(18,2)=0;
	DECLARE @TotalCalculatedClaimAmount DECIMAL(18,2)=0;

	DECLARE @TotalDAAmount              DECIMAL(18,2)=0;
	DECLARE @TotalHotelAmount           DECIMAL(18,2)=0;
	DECLARE @TotalTravelAmount          DECIMAL(18,2)=0;
	DECLARE @TotalStationaryAmount      DECIMAL(18,2)=0;

	DECLARE @TotalExpenseAmount         DECIMAL(18,2)=0;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (SELECT 1 FROM @Updates)
        BEGIN
            RAISERROR('No activities supplied for submission.',16,1);
            RETURN;
        END;

        IF @L1Code IS NULL OR @AuditorCode IS NULL
        BEGIN
            RAISERROR('L1Code and AuditorCode are mandatory.',16,1);
            RETURN;
        END;

        IF @L1Code=@AuditorCode
        BEGIN
            RAISERROR('Approver hierarchy is invalid.',16,1);
            RETURN;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.SalesDailyActivity a
            INNER JOIN @Updates u ON a.ActivityId=u.ActivityId
            WHERE a.Login_Id<>@LoginId
               OR YEAR(a.ActivityDate)<>@Year
               OR MONTH(a.ActivityDate)<>@Month
        )
        BEGIN
            RAISERROR('Activities do not belong to the selected employee/month.',16,1);
            RETURN;
        END;

        -----------------------------------------------------------------
        -- One active header per employee/year/month.
        -- Returned and Pending are active lifecycles.
        -- Completed, ReadyForPayment, and Processed are terminal.
        -- Draft and Rejected are closed predecessors and must not be reused.
        -- Any other carried status fails closed.
        -- UPDLOCK, HOLDLOCK is held until COMMIT/ROLLBACK.
        -----------------------------------------------------------------
        DECLARE @ActiveMonthRequest TABLE
        (
            ApprovalRequestId INT NOT NULL PRIMARY KEY,
            Status NVARCHAR(50) NOT NULL
        );

        INSERT INTO @ActiveMonthRequest (ApprovalRequestId, Status)
        SELECT r.ApprovalRequestId, r.Status
        FROM dbo.SalesApprovalRequest r WITH (UPDLOCK, HOLDLOCK)
        WHERE r.RequesterLoginId = @LoginId
          AND r.YearNo = @Year
          AND r.MonthNo = @Month
          AND UPPER(LTRIM(RTRIM(r.Status))) IN
              (N'RETURNED', N'PENDING', N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED');

        DECLARE @ActiveHeaderCount INT = (SELECT COUNT(*) FROM @ActiveMonthRequest);
        DECLARE @ActiveRequestId INT = NULL;
        DECLARE @ActiveStatus NVARCHAR(50) = NULL;

        IF @ActiveHeaderCount > 1
        BEGIN
            IF @@TRANCOUNT > 0 ROLLBACK TRAN;
            RAISERROR('Multiple active approval requests exist for this employee and month.', 16, 1);
            RETURN;
        END;

        IF @ActiveHeaderCount = 1
            SELECT @ActiveRequestId = ApprovalRequestId, @ActiveStatus = Status
            FROM @ActiveMonthRequest;

        SELECT
            @ExistingRequestCount = COUNT(DISTINCT a.ApprovalRequestId),
            @ApprovalRequestId = MAX(a.ApprovalRequestId)
        FROM dbo.SalesDailyActivity a
        INNER JOIN @Updates u ON a.ActivityId = u.ActivityId
        WHERE a.ApprovalRequestId IS NOT NULL;

        IF @ExistingRequestCount > 1
        BEGIN
            IF @@TRANCOUNT > 0 ROLLBACK TRAN;
            RAISERROR('Activities belong to multiple approval requests.', 16, 1);
            RETURN;
        END;

        DECLARE @ResolvedStatus NVARCHAR(50) = NULL;
        DECLARE @ClosedActivityRequest BIT = 0;

        IF @ExistingRequestCount = 1
        BEGIN
            SELECT @ResolvedStatus = r.Status
            FROM dbo.SalesApprovalRequest r WITH (UPDLOCK, HOLDLOCK)
            WHERE r.ApprovalRequestId = @ApprovalRequestId
              AND r.RequesterLoginId = @LoginId
              AND r.YearNo = @Year
              AND r.MonthNo = @Month;

            DECLARE @NormalizedResolvedStatus NVARCHAR(50) =
                UPPER(LTRIM(RTRIM(ISNULL(@ResolvedStatus, N''))));

            -- Processed is terminal, same as Completed and ReadyForPayment.
            -- This return is before closed-predecessor clearing, so Processed
            -- cannot be dropped and reused as a new Pending request.
            IF @NormalizedResolvedStatus IN (N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED')
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRAN;
                RAISERROR('A finalized request already exists for this month and cannot be submitted.', 16, 1);
                RETURN;
            END;

            -- Returned and Pending keep this request.
            -- Draft, Rejected, and a missing header row are closed predecessors.
            -- Any other non-null carried status fails closed.
            IF @NormalizedResolvedStatus IN (N'RETURNED', N'PENDING')
            BEGIN
                SET @ClosedActivityRequest = 0;
            END
            ELSE IF @ResolvedStatus IS NULL
                OR @NormalizedResolvedStatus IN (N'DRAFT', N'REJECTED')
            BEGIN
                SET @ClosedActivityRequest = 1;
                SET @ApprovalRequestId = NULL;
                SET @ExistingRequestCount = 0;
            END
            ELSE
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRAN;
                RAISERROR('This month has an approval request in an unexpected status and cannot be submitted.', 16, 1);
                RETURN;
            END;
        END;

        IF @ExistingRequestCount = 0 AND @ActiveHeaderCount = 1
        BEGIN
            IF @ClosedActivityRequest = 1
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRAN;
                RAISERROR('Submitted activities belong to a closed approval request and cannot be attached to the active request for this month.', 16, 1);
                RETURN;
            END;

            IF UPPER(LTRIM(RTRIM(@ActiveStatus))) = N'RETURNED'
            BEGIN
                SET @ApprovalRequestId = @ActiveRequestId;
                SET @ExistingRequestCount = 1;
            END
            ELSE IF UPPER(LTRIM(RTRIM(@ActiveStatus))) = N'PENDING'
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRAN;
                RAISERROR('A pending request already exists.', 16, 1);
                RETURN;
            END
            ELSE
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRAN;
                RAISERROR('A finalized request already exists for this month and cannot be submitted.', 16, 1);
                RETURN;
            END;
        END;

        SET @SubmissionMode =
            CASE WHEN @ExistingRequestCount = 0 THEN 'NEW'
                 ELSE 'RESUBMIT' END;

        IF @SubmissionMode = 'RESUBMIT'
        BEGIN
            IF EXISTS
            (
                SELECT 1
                FROM dbo.SalesDailyActivity a
                INNER JOIN @Updates u ON a.ActivityId=u.ActivityId
                WHERE a.ClaimStatus<>'Returned'
            )
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRAN;
                RAISERROR('Resubmission can contain only Returned activities.',16,1);
                RETURN;
            END;

            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.SalesApprovalRequest
                WHERE ApprovalRequestId=@ApprovalRequestId
                  AND RequesterLoginId=@LoginId
            )
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRAN;
                RAISERROR('Approval Request not found.',16,1);
                RETURN;
            END;

            UPDATE dbo.SalesApprovalRequest
            SET Status='Pending',
                RequestRemarks=@Remarks,
                ModifiedOn=@Now,
                ModifiedBy=@LoginId,
                CurrentApproverLoginId=@L1Code,
                CurrentApproverLevel=1
            WHERE ApprovalRequestId=@ApprovalRequestId;

            -- Bind returned activities that do not already carry this request
            -- before later snapshot rows are inserted for the same id.
            UPDATE a
            SET a.ApprovalRequestId = @ApprovalRequestId
            FROM dbo.SalesDailyActivity a
            INNER JOIN @Updates u ON a.ActivityId = u.ActivityId
            WHERE a.Login_Id = @LoginId
              AND YEAR(a.ActivityDate) = @Year
              AND MONTH(a.ActivityDate) = @Month
              AND a.ClaimStatus = 'Returned'
              AND a.ApprovalRequestId IS NULL;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.SalesApprovalRequest
            (
                RequesterLoginId,RequestDate,YearNo,MonthNo,Status,
                RequestRemarks,CreatedOn,CreatedBy
            )
            VALUES
            (
                @LoginId,@Now,@Year,@Month,'Pending',
                @Remarks,@Now,@LoginId
            );

            SET @ApprovalRequestId=SCOPE_IDENTITY();
        END;

        /* End of Revised Part 1 */


		/*==============================================================================
	 Part 2 : Activity Update, Expense Aggregation & Immutable Snapshot
	==============================================================================*/

		-------------------------------------------------------------------------
		-- Update Activities
		-------------------------------------------------------------------------
		UPDATE a
		SET
			a.TotalDistanceKM          = u.TotalDistanceKM,
			a.CalculatedClaimAmount    = u.CalculatedClaimAmount,
			a.ClaimStatus              = 'Submitted',
			a.SubmittedBy              = @LoginId,
			a.SubmittedOn              = @Now,
			a.ApprovalRequestId        = @ApprovalRequestId,
			a.ModifiedByUserId         = @LoginId,
			a.ModifiedDateTime         = @Now
		FROM dbo.SalesDailyActivity a
		INNER JOIN @Updates u
			ON a.ActivityId = u.ActivityId
		WHERE
			a.Login_Id = @LoginId
		AND YEAR(a.ActivityDate) = @Year
		AND MONTH(a.ActivityDate) = @Month
		AND a.ClaimStatus IN ('Draft', 'Returned');


		IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @Updates)
		BEGIN
			RAISERROR
			(
				'One or more activities are not eligible for submission.',
				16,
				1
			);

			RETURN;
		END;
		-------------------------------------------------------------------------
		-- Update Expense Status
		-------------------------------------------------------------------------

		UPDATE e
		SET
			e.ClaimStatus = 'Submitted',
			e.ModifiedOn  = @Now,
			e.ModifiedBy  = @LoginId
		FROM dbo.SalesDailyExpense e
		INNER JOIN @Updates u
			ON e.ActivityId = u.ActivityId
		WHERE
			e.IsDeleted = 0
		AND
			e.ClaimStatus IN ('Draft','Returned');

		-------------------------------------------------------------------------
		-- Expense Aggregation
		-------------------------------------------------------------------------

    CREATE TABLE #ExpenseAgg
    (
        ActivityId          INT             NOT NULL PRIMARY KEY,

        DAAmount            DECIMAL(18,2)   NOT NULL DEFAULT(0),

        HotelAmount         DECIMAL(18,2)   NOT NULL DEFAULT(0),

        TravelAmount        DECIMAL(18,2)   NOT NULL DEFAULT(0),

        StationaryAmount    DECIMAL(18,2)   NOT NULL DEFAULT(0),

        ExpenseTotal        DECIMAL(18,2)   NOT NULL DEFAULT(0)
    );

    INSERT INTO #ExpenseAgg
    (
        ActivityId,
        DAAmount,
        HotelAmount,
        TravelAmount,
        StationaryAmount,
        ExpenseTotal
    )
    SELECT

        e.ActivityId,

        SUM(CASE WHEN e.ExpenseType='DA'
                 THEN ISNULL(e.Amount,0)
                 ELSE 0 END),

        SUM(CASE WHEN e.ExpenseType='Hotel'
                 THEN ISNULL(e.Amount,0)
                 ELSE 0 END),

        SUM(CASE WHEN e.ExpenseType='Travel'
                 THEN ISNULL(e.Amount,0)
                 ELSE 0 END),

        SUM(CASE WHEN e.ExpenseType='Stationary'
                 THEN ISNULL(e.Amount,0)
                 ELSE 0 END),

        SUM(ISNULL(e.Amount,0))

    FROM dbo.SalesDailyExpense e

    INNER JOIN @Updates u
        ON e.ActivityId = u.ActivityId

    WHERE
        e.IsDeleted = 0

    GROUP BY
        e.ActivityId;

    -------------------------------------------------------------------------
    -- Latest Detail Snapshot
    -------------------------------------------------------------------------

    ;WITH LatestDetailSnapshot AS
    (
        SELECT

            d.ActivityId,

            d.ApprovalRequestDetailId,

            d.DetailVersion,

            ROW_NUMBER() OVER
            (
                PARTITION BY d.ActivityId
                ORDER BY
                    d.DetailVersion DESC,
                    d.ApprovalRequestDetailId DESC
            ) AS RN

        FROM dbo.SalesApprovalRequestDetail d

        WHERE
            d.ApprovalRequestId = @ApprovalRequestId
    )

    -------------------------------------------------------------------------
    -- Insert Immutable Snapshot
    -------------------------------------------------------------------------

    INSERT INTO dbo.SalesApprovalRequestDetail
    (
        ApprovalRequestId,

        ActivityId,

        Snapshot_TotalDistanceKM,

        Snapshot_CalculatedClaimAmount,

        Snapshot_DAAmount,

        Snapshot_HotelAmount,

        Snapshot_TravelAmount,

        Snapshot_StationaryAmount,

        Snapshot_ExpenseTotal,

        DetailVersion,

        PreviousDetailId,

        DetailStatus,

        CreatedOn,

        CreatedBy
    )

    SELECT

        @ApprovalRequestId,

        a.ActivityId,

        a.TotalDistanceKM,

        a.CalculatedClaimAmount,

        ISNULL(ea.DAAmount,0),

        ISNULL(ea.HotelAmount,0),

        ISNULL(ea.TravelAmount,0),

        ISNULL(ea.StationaryAmount,0),

        ISNULL(ea.ExpenseTotal,0),

        ISNULL(lds.DetailVersion,0)+1,

        lds.ApprovalRequestDetailId,

        'Pending',

        @Now,

        @LoginId

    FROM dbo.SalesDailyActivity a

    INNER JOIN @Updates u
        ON a.ActivityId=u.ActivityId

    LEFT JOIN #ExpenseAgg ea
        ON a.ActivityId=ea.ActivityId

    LEFT JOIN LatestDetailSnapshot lds
        ON a.ActivityId=lds.ActivityId
       AND lds.RN=1;

    -------------------------------------------------------------------------
    -- Build Latest Snapshot (For Part 3)
    -------------------------------------------------------------------------

    SELECT

        d.ActivityId,

        d.DetailVersion,

        d.Snapshot_TotalDistanceKM,

        d.Snapshot_CalculatedClaimAmount,

        d.Snapshot_DAAmount,

        d.Snapshot_HotelAmount,

        d.Snapshot_TravelAmount,

        d.Snapshot_StationaryAmount,

        d.Snapshot_ExpenseTotal

    INTO #LatestSnapshot

    FROM
    (
        SELECT

            d.*,

            ROW_NUMBER() OVER
            (
                PARTITION BY d.ActivityId
                ORDER BY
                    d.DetailVersion DESC,
                    d.ApprovalRequestDetailId DESC
            ) AS RN

        FROM dbo.SalesApprovalRequestDetail d

        WHERE
            d.ApprovalRequestId=@ApprovalRequestId

    ) d

    WHERE
        d.RN=1;

    /*-----------------------------------------------------------------------
      End of Part 2
    -----------------------------------------------------------------------*/

	/*==============================================================================
 Part 3 : Request Totals, Approver Processing & Workflow History
==============================================================================*/

    -------------------------------------------------------------------------
    -- Calculate Request Totals
    -------------------------------------------------------------------------

    ;WITH LatestSnapshot AS
    (
        SELECT

            d.ActivityId,

            d.Snapshot_TotalDistanceKM,

            d.Snapshot_CalculatedClaimAmount,

            d.Snapshot_DAAmount,

            d.Snapshot_HotelAmount,

            d.Snapshot_TravelAmount,

            d.Snapshot_StationaryAmount,

            d.Snapshot_ExpenseTotal,

            ROW_NUMBER() OVER
            (
                PARTITION BY d.ActivityId
                ORDER BY
                    d.DetailVersion DESC,
                    d.ApprovalRequestDetailId DESC
            ) AS RN

        FROM dbo.SalesApprovalRequestDetail d

        WHERE
            d.ApprovalRequestId = @ApprovalRequestId
    )

    SELECT

        @TotalDistanceKM =
            SUM(Snapshot_TotalDistanceKM),

        @TotalCalculatedClaimAmount =
            SUM(Snapshot_CalculatedClaimAmount),

        @TotalDAAmount =
            SUM(Snapshot_DAAmount),

        @TotalHotelAmount =
            SUM(Snapshot_HotelAmount),

        @TotalTravelAmount =
            SUM(Snapshot_TravelAmount),

        @TotalStationaryAmount =
            SUM(Snapshot_StationaryAmount),

        @TotalExpenseAmount =
            SUM(Snapshot_ExpenseTotal)

    FROM LatestSnapshot

    WHERE
        RN = 1;

    -------------------------------------------------------------------------
    -- Update Request Header
    -------------------------------------------------------------------------

    UPDATE dbo.SalesApprovalRequest
	SET
		TotalKM                = @TotalDistanceKM,
		TotalClaimAmount       = @TotalCalculatedClaimAmount,
		TotalDAAmount          = @TotalDAAmount,
		TotalHotelAmount       = @TotalHotelAmount,
		TotalTravelAmount      = @TotalTravelAmount,
		TotalStationaryAmount  = @TotalStationaryAmount,
		TotalExpenseAmount     = @TotalExpenseAmount,

		TotalActivities =
		(
			SELECT COUNT(*)
			FROM dbo.SalesApprovalRequestDetail
			WHERE ApprovalRequestId = @ApprovalRequestId
			  AND DetailVersion =
			  (
				  SELECT MAX(d2.DetailVersion)
				  FROM dbo.SalesApprovalRequestDetail d2
				  WHERE d2.ApprovalRequestId = dbo.SalesApprovalRequestDetail.ApprovalRequestId
					AND d2.ActivityId = dbo.SalesApprovalRequestDetail.ActivityId
			  )
		),

		ModifiedOn = @Now,
		ModifiedBy = @LoginId

	WHERE ApprovalRequestId = @ApprovalRequestId;

    -------------------------------------------------------------------------
    -- Approver Processing
    -------------------------------------------------------------------------

    -------------------------------------------------------------------------
-- Create Approver Chain (NEW REQUEST)
-------------------------------------------------------------------------

IF @SubmissionMode = 'NEW'
BEGIN

    -- Level 1 (Mandatory)
    INSERT INTO dbo.SalesApprovalRequestApprover
    (
        ApprovalRequestId,
        ApproverLoginId,
        ApproverRole,
        ApproverLevel,
        IsRequired,
        Status,
        CreatedOn,
        CreatedBy
    )
    VALUES
    (
        @ApprovalRequestId,
        @L1Code,
        'L1',
        1,
        1,
        'Pending',
        @Now,
        @LoginId
    );

    -- Level 2 (Optional)
    IF @L2Code IS NOT NULL
    BEGIN
        INSERT INTO dbo.SalesApprovalRequestApprover
        (
            ApprovalRequestId,
            ApproverLoginId,
            ApproverRole,
            ApproverLevel,
            IsRequired,
            Status,
            CreatedOn,
            CreatedBy
        )
        VALUES
        (
            @ApprovalRequestId,
            @L2Code,
            'L2',
            2,
            1,
            'Pending',
            @Now,
            @LoginId
        );
    END;

    -- Level 3 (Optional)
    IF @L3Code IS NOT NULL
    BEGIN
        INSERT INTO dbo.SalesApprovalRequestApprover
        (
            ApprovalRequestId,
            ApproverLoginId,
            ApproverRole,
            ApproverLevel,
            IsRequired,
            Status,
            CreatedOn,
            CreatedBy
        )
        VALUES
        (
            @ApprovalRequestId,
            @L3Code,
            'L3',
            3,
            1,
            'Pending',
            @Now,
            @LoginId
        );
    END;

    -- Auditor (Mandatory)
    INSERT INTO dbo.SalesApprovalRequestApprover
    (
        ApprovalRequestId,
        ApproverLoginId,
        ApproverRole,
        ApproverLevel,
        IsRequired,
        Status,
        CreatedOn,
        CreatedBy
    )
    VALUES
    (
        @ApprovalRequestId,
        @AuditorCode,
        'Auditor',
        CASE
            WHEN @L3Code IS NOT NULL THEN 4
            WHEN @L2Code IS NOT NULL THEN 3
            ELSE 2
        END,
        1,
        'Pending',
        @Now,
        @LoginId
    );

END
ELSE
BEGIN

    ---------------------------------------------------------------------
    -- Reset existing approvers (RESUBMIT)
    ---------------------------------------------------------------------

    UPDATE dbo.SalesApprovalRequestApprover
    SET
        Status      = 'Pending',
        ActionBy    = NULL,
        ActionDate  = NULL,
        Remarks     = NULL,
        ModifiedOn  = @Now,
        ModifiedBy  = @LoginId
    WHERE ApprovalRequestId = @ApprovalRequestId;

END;

    -------------------------------------------------------------------------
    -- Reset Current Approver
    -------------------------------------------------------------------------

    UPDATE dbo.SalesApprovalRequest

    SET

        CurrentApproverLoginId=@L1Code,

        CurrentApproverLevel=1

    WHERE
        ApprovalRequestId=@ApprovalRequestId;

    -------------------------------------------------------------------------
    -- Workflow History
    -------------------------------------------------------------------------

    INSERT INTO dbo.ApprovalRequestStatusHistory
    (
        ApprovalRequestId,

        OldStatus,

        NewStatus,

        ChangedBy,

        ChangedOn,

        Remarks
    )
    VALUES
    (
        @ApprovalRequestId,

        CASE
            WHEN @SubmissionMode='NEW'
                THEN NULL
            ELSE 'Returned'
        END,

        'Pending',

        @LoginId,

        @Now,

        CASE
            WHEN @SubmissionMode='NEW'
                THEN 'Initial Submission'
            ELSE 'Resubmitted after corrections'
        END
    );

	/*==============================================================================
 Part 4 : Commit / Rollback / Error Handling
==============================================================================*/

        ---------------------------------------------------------------------
        -- Commit Transaction
        ---------------------------------------------------------------------

        COMMIT TRAN;

        ---------------------------------------------------------------------
        -- Success Result
        ---------------------------------------------------------------------

        SELECT

            1 AS Success,

            @ApprovalRequestId AS ApprovalRequestId,

            @SubmissionMode AS SubmissionMode,

            'Claim submitted successfully.' AS Message;

    END TRY

    BEGIN CATCH

    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    DECLARE @ErrMsg NVARCHAR(MAX);

    SET @ErrMsg =
        CONCAT(
            'usp_SubmitSalesDailyActivities failed.',
            CHAR(13),
            'Error : ', ERROR_MESSAGE(),
            CHAR(13),
            'Procedure : ', ISNULL(ERROR_PROCEDURE(), 'N/A'),
            CHAR(13),
            'Line : ', ERROR_LINE()
        );

    RAISERROR(@ErrMsg, 16, 1);

END CATCH;

END
