/* ============================================================================
   NAME:        allow_returned_km_update
   WHEN:        2026-09-14
   WHY:         Line-item Return leaves ClaimStatus=Submitted while DetailStatus=Returned, blocking KM save
   WHAT:        usp_UpdateActivityKM status gate allows Draft, Returned claim, or latest Returned detail
   ============================================================================ */

/*
    Object:      [dbo].[usp_UpdateActivityKM]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_UpdateActivityKM]
    @ActivityId INT,
    @NewKM DECIMAL(10,2),
    @ModifiedBy NVARCHAR(100),
    @Reason NVARCHAR(400) = NULL,
	@RouteOriginName       NVARCHAR(255) = NULL,
	@RouteDestinationName  NVARCHAR(255) = NULL,
	@RouteWaypointsJSON    NVARCHAR(MAX) = NULL,
	@TravelMode            NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRAN;

        DECLARE @CurrentKM DECIMAL(10,2),
                @ClaimStatus NVARCHAR(50),
                @UserLogin NVARCHAR(100); -- holds EmployeeCode (Login_Id)

        SELECT @CurrentKM = TotalDistanceKM,
               @ClaimStatus = ClaimStatus,
               @UserLogin = Login_Id
        FROM dbo.SalesDailyActivity WITH (UPDLOCK, HOLDLOCK)
        WHERE ActivityId = @ActivityId;

        IF @UserLogin IS NULL
        BEGIN
            -- activity not found
            THROW 50001, 'Activity not found.', 1;
        END

        IF @ClaimStatus IS NULL SET @ClaimStatus = 'Draft';

        DECLARE @LatestDetailStatus NVARCHAR(50);
        SELECT @LatestDetailStatus = d.DetailStatus
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN (
            SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
            FROM dbo.SalesApprovalRequestDetail
            WHERE ActivityId = @ActivityId
            GROUP BY ActivityId
        ) latest
            ON latest.ActivityId = d.ActivityId
           AND ISNULL(d.DetailVersion, 1) = latest.LatestVersion
        WHERE d.ActivityId = @ActivityId;

        IF @ClaimStatus IN ('Approved', 'Pending', 'Rejected')
           OR (
                @ClaimStatus NOT IN ('Draft', 'Returned')
                AND ISNULL(@LatestDetailStatus, '') <> 'Returned'
              )
        BEGIN
            THROW 50002, 'Cannot modify KM unless ClaimStatus is Draft or the activity is Returned.', 1;
        END

		IF @RouteWaypointsJSON IS NOT NULL
		AND ISJSON(@RouteWaypointsJSON) <> 1
		BEGIN
			THROW 50005, 'Invalid Route JSON.', 1;
		END

        DECLARE @MinAllowed DECIMAL(10,2), @MaxAllowed DECIMAL(10,2);

        SELECT TOP 1
               @MinAllowed = MinAllowedKM,
               @MaxAllowed = MaxAllowedKM
        FROM dbo.MST_EmployeeTolerance
        WHERE EmployeeCode = @UserLogin
          AND (EffectiveFrom IS NULL OR EffectiveFrom <= CAST(GETDATE() AS DATE))
          AND (EffectiveTo IS NULL OR EffectiveTo >= CAST(GETDATE() AS DATE))
        ORDER BY ToleranceId DESC;

        /*
         New logic: treat MinAllowed/MaxAllowed as deltas from the CURRENT value.
         AllowedLower = @CurrentKM - @MinAllowed   (if @MinAllowed is not NULL)
         AllowedUpper = @CurrentKM + @MaxAllowed   (if @MaxAllowed is not NULL)
         If either bound is NULL => that side is unbounded.
        */

        DECLARE @AllowedLower DECIMAL(10,2) = NULL,
                @AllowedUpper DECIMAL(10,2) = NULL;

        IF @MinAllowed IS NOT NULL
        BEGIN
            SET @AllowedLower = @CurrentKM - @MinAllowed;
            -- don't allow negative lower bound
            IF @AllowedLower < 0 SET @AllowedLower = 0;
        END

        IF @MaxAllowed IS NOT NULL
        BEGIN
            SET @AllowedUpper = @CurrentKM + @MaxAllowed;
        END

        -- check lower bound
        IF @AllowedLower IS NOT NULL AND @NewKM < @AllowedLower
        BEGIN
            DECLARE @msg NVARCHAR(4000) = 'New KM (' + STR(@NewKM, 12, 2) + 
                ') is below allowed lower bound (Current ' + STR(@CurrentKM,12,2) + 
                ' - MinTolerance ' + STR(@MinAllowed,12,2) + ' = ' + STR(@AllowedLower,12,2) + ').';
            THROW 50003, @msg, 1;
        END

        -- check upper bound
        IF @AllowedUpper IS NOT NULL AND @NewKM > @AllowedUpper
        BEGIN
            DECLARE @msg2 NVARCHAR(4000) = 'New KM (' + STR(@NewKM, 12, 2) + 
                ') exceeds allowed upper bound (Current ' + STR(@CurrentKM,12,2) + 
                ' + MaxTolerance ' + STR(@MaxAllowed,12,2) + ' = ' + STR(@AllowedUpper,12,2) + ').';
            THROW 50004, @msg2, 1;
        END

        ---- perform update
        --UPDATE dbo.SalesDailyActivity
        --SET TotalDistanceKM = @NewKM,
        --    ModifiedByUserId = @ModifiedBy,
        --    ModifiedDateTime = DATEADD(MINUTE,330,SYSUTCDATETIME())
        --WHERE ActivityId = @ActivityId;

		UPDATE dbo.SalesDailyActivity
		SET
			TotalDistanceKM = @NewKM,
			RouteOriginName =
				COALESCE(
					NULLIF(@RouteOriginName,''),
					RouteOriginName
				),

			RouteDestinationName =
				COALESCE(
					NULLIF(@RouteDestinationName,''),
					RouteDestinationName
				),

			RouteWaypointsJSON =
				COALESCE(
					NULLIF(@RouteWaypointsJSON,''),
					RouteWaypointsJSON
				),

			TravelMode =
				COALESCE(
					NULLIF(@TravelMode,''),
					TravelMode
				),
			ModifiedByUserId = @ModifiedBy,
			ModifiedDateTime = DATEADD(MINUTE,330,SYSUTCDATETIME())
		WHERE ActivityId = @ActivityId;

        -- insert history (now includes Reason)
        --INSERT INTO dbo.SalesDailyActivityKMHistory
        --    (ActivityId, OldKM, NewKM, ModifiedBy, ModifiedDateTime, Reason)
        --VALUES
        --    (@ActivityId, @CurrentKM, @NewKM, @ModifiedBy, DATEADD(MINUTE,330,SYSUTCDATETIME()), @Reason);

		INSERT INTO dbo.SalesDailyActivityKMHistory
		(
			ActivityId,
			OldKM,
			NewKM,
			ModifiedBy,
			ModifiedDateTime,
			Reason,
			RouteOriginName,
			RouteDestinationName,
			RouteWaypointsJSON,
			TravelMode
		)
		VALUES
		(
			@ActivityId,
			@CurrentKM,
			@NewKM,
			@ModifiedBy,
			DATEADD(MINUTE,330,SYSUTCDATETIME()),
			@Reason,
			@RouteOriginName,
			@RouteDestinationName,
			@RouteWaypointsJSON,
			@TravelMode
		);

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        -- re-raise as RAISERROR for compatibility; this will surface the message to callers
        RAISERROR(@ErrMsg,16,1);
    END CATCH
END
