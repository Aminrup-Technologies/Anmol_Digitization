/* ============================================================================
   NAME:        persist_calculated_claim_on_km_save
   WHEN:        2026-10-11
   WHY:         Saving kilometres updated TotalDistanceKM and left CalculatedClaimAmount unchanged
   WHAT:        In the existing transaction, persist the travel allowance the monthly
                claim screen calculates for the saved kilometres.
   RULE:        Matrix policy: dbo.usp_GetResolvedPolicy, called with MST_UserMaster
                RegionId, BranchId, GradeId, and Designation_ID. That procedure
                applies its own effective date (server GETDATE), not the claim month.
                CategoryId is not used: the screen passes Designation_ID, and the
                CategoryId read applies only when the designation argument is empty.
                Exception: active PolicyException for the activity owner, effective on
                the last day of the activity month, lowest Priority value first.
                Amount: kilometres <= 0 store 0; a resolved fixed allowance replaces
                the per-km amount, including zero; otherwise ROUND(rate * kilometres, 2)
                with SQL Server half-away-from-zero rounding. A positive resolved
                rate is used. A missing or non-positive rate uses 3.75, which is
                SalesMonthlyReview.GetDefaultRatePerKm when DefaultRatePerKm is
                absent from Web.config. No policy row, or a missing Designation_ID,
                rolls the kilometre change back.
   ============================================================================ */

/*
    Object:      [dbo].[usp_UpdateActivityKM]
    Type:        STORED PROCEDURE
    Prior gate:  status gate allows Draft, Returned claim, or latest Returned detail

    Deploy only after confirming:
      1. dbo.usp_GetResolvedPolicy still returns the PolicyMatrix columns listed
         in the INSERT...EXEC target, in that order.
      2. Its matrix filter still uses CONVERT(date, GETDATE()) and still reads
         CategoryId only when @DesignationIdParam is empty.
    This script does not execute the procedure or update any activity row.
*/

CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateActivityKM]
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
                @UserLogin NVARCHAR(100),
                @ActivityDate DATE;

        SELECT @CurrentKM = TotalDistanceKM,
               @ClaimStatus = ClaimStatus,
               @UserLogin = Login_Id,
               @ActivityDate = ActivityDate
        FROM dbo.SalesDailyActivity WITH (UPDLOCK, HOLDLOCK)
        WHERE ActivityId = @ActivityId;

        IF @UserLogin IS NULL
        BEGIN
            THROW 50001, 'Activity not found.', 1;
        END

        IF @ActivityDate IS NULL
        BEGIN
            THROW 50006, 'Activity date is missing, so the travel allowance cannot be resolved.', 1;
        END

        IF @NewKM IS NULL
        BEGIN
            THROW 50006, 'Kilometres are required, so the travel allowance was not calculated.', 1;
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
            IF @AllowedLower < 0 SET @AllowedLower = 0;
        END

        IF @MaxAllowed IS NOT NULL
        BEGIN
            SET @AllowedUpper = @CurrentKM + @MaxAllowed;
        END

        IF @AllowedLower IS NOT NULL AND @NewKM < @AllowedLower
        BEGIN
            DECLARE @msg NVARCHAR(4000) = 'New KM (' + STR(@NewKM, 12, 2) + 
                ') is below allowed lower bound (Current ' + STR(@CurrentKM,12,2) + 
                ' - MinTolerance ' + STR(@MinAllowed,12,2) + ' = ' + STR(@AllowedLower,12,2) + ').';
            THROW 50003, @msg, 1;
        END

        IF @AllowedUpper IS NOT NULL AND @NewKM > @AllowedUpper
        BEGIN
            DECLARE @msg2 NVARCHAR(4000) = 'New KM (' + STR(@NewKM, 12, 2) + 
                ') exceeds allowed upper bound (Current ' + STR(@CurrentKM,12,2) + 
                ' + MaxTolerance ' + STR(@MaxAllowed,12,2) + ' = ' + STR(@AllowedUpper,12,2) + ').';
            THROW 50004, @msg2, 1;
        END

        /*
           Screen path: PolicyResolutionDataAccess.GetStandardPolicy passes the login
           session values to dbo.usp_GetResolvedPolicy. Login stores MST_UserMaster
           RegionId, BranchId, GradeId, and Designation_ID. Designation_ID is
           MST_Designation.Id. The procedure's CategoryId fallback runs only when
           the designation argument is empty, which is not the screen's path.
        */
        DECLARE @EmployeeCode VARCHAR(50) = CONVERT(VARCHAR(50), LTRIM(RTRIM(@UserLogin)));
        DECLARE @RegionParam VARCHAR(200),
                @BranchParam VARCHAR(200),
                @GradeParam VARCHAR(200),
                @DesignationParam VARCHAR(200);

        SELECT
            @RegionParam = NULLIF(LTRIM(RTRIM(CONVERT(VARCHAR(200), RegionId))), ''),
            @BranchParam = NULLIF(LTRIM(RTRIM(CONVERT(VARCHAR(200), BranchId))), ''),
            @GradeParam = NULLIF(LTRIM(RTRIM(CONVERT(VARCHAR(200), GradeId))), ''),
            @DesignationParam = NULLIF(LTRIM(RTRIM(CONVERT(VARCHAR(200), Designation_ID))), '')
        FROM dbo.MST_UserMaster
        WHERE EmployeeCode = @EmployeeCode;

        IF @DesignationParam IS NULL
        BEGIN
            THROW 50006, 'The employee has no Designation_ID, so the travel allowance was not calculated.', 1;
        END

        DECLARE @PolicyResult TABLE (
            PolicyId INT NOT NULL,
            RegionId INT NULL,
            BranchId INT NULL,
            GradeId INT NULL,
            DesignationId INT NULL,
            PolicyJson NVARCHAR(MAX) NULL,
            Priority INT NOT NULL,
            EffectiveFrom DATE NULL,
            EffectiveTo DATE NULL,
            IsActive BIT NOT NULL,
            CreatedBy NVARCHAR(100) NULL,
            CreatedOn DATETIME2(7) NOT NULL,
            ModifiedBy NVARCHAR(100) NULL,
            ModifiedOn DATETIME2(7) NULL
        );

        INSERT INTO @PolicyResult (
            PolicyId, RegionId, BranchId, GradeId, DesignationId, PolicyJson,
            Priority, EffectiveFrom, EffectiveTo, IsActive,
            CreatedBy, CreatedOn, ModifiedBy, ModifiedOn
        )
        EXEC dbo.usp_GetResolvedPolicy
            @EmployeeCode = @EmployeeCode,
            @RegionIdParam = @RegionParam,
            @BranchIdParam = @BranchParam,
            @GradeIdParam = @GradeParam,
            @DesignationIdParam = @DesignationParam;

        DECLARE @PolicyJson NVARCHAR(MAX);
        SELECT TOP (1) @PolicyJson = PolicyJson
        FROM @PolicyResult
        ORDER BY Priority DESC, EffectiveFrom DESC;

        IF NOT EXISTS (SELECT 1 FROM @PolicyResult)
        BEGIN
            THROW 50006, 'No active policy matched this activity, so the kilometre change was not saved.', 1;
        END

        /*
           Exception as-of date is the claim date used by SalesMonthlyReview:
           the last day of the selected month. For an activity in that month,
           that date is EOMONTH(ActivityDate). The matrix procedure does not
           receive this date.
           Sort is Priority ASC only, matching PolicyExceptionDataAccess.
           Equal priorities remain non-deterministic there as well.
        */
        DECLARE @AsOfDate DATE = EOMONTH(@ActivityDate);
        DECLARE @ExceptionJson NVARCHAR(MAX);

        SELECT TOP (1)
            @ExceptionJson = pe.PolicyJson
        FROM dbo.PolicyException pe
        WHERE pe.EmployeeCode = @EmployeeCode
          AND pe.IsActive = 1
          AND pe.EffectiveFrom <= @AsOfDate
          AND (pe.EffectiveTo IS NULL OR pe.EffectiveTo >= @AsOfDate)
        ORDER BY pe.Priority ASC;

        IF @ExceptionJson IS NOT NULL AND ISJSON(@ExceptionJson) <> 1
            SET @ExceptionJson = NULL;

        /*
           PolicyOverrideMergeHelper maps RatePerKm and Resolved_RatePerKm to the
           same value, case-insensitively, and the later JSON property wins.
           FixedTravelAllowance is separate. An exception overrides a key only
           when that key is present and parses. Invalid exception JSON is ignored.
           PolicyMatrix has no rate or fixed-allowance columns; the screen reads
           these values from PolicyJson after usp_GetResolvedPolicy returns.
        */
        DECLARE @ParsedKeys TABLE (
            Ord INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            KeyName NVARCHAR(200) NOT NULL,
            KeyValue NVARCHAR(200) NULL
        );

        IF @PolicyJson IS NOT NULL AND ISJSON(@PolicyJson) = 1
        BEGIN
            INSERT INTO @ParsedKeys (KeyName, KeyValue)
            SELECT LTRIM(RTRIM([key])), [value]
            FROM OPENJSON(@PolicyJson)
            WHERE [type] IN (1, 2)
              AND TRY_CONVERT(DECIMAL(18,6), [value]) IS NOT NULL
              AND LOWER(LTRIM(RTRIM([key]))) IN (N'rateperkm', N'resolved_rateperkm', N'fixedtravelallowance');
        END

        IF @ExceptionJson IS NOT NULL AND ISJSON(@ExceptionJson) = 1
        BEGIN
            INSERT INTO @ParsedKeys (KeyName, KeyValue)
            SELECT LTRIM(RTRIM([key])), [value]
            FROM OPENJSON(@ExceptionJson)
            WHERE [type] IN (1, 2)
              AND TRY_CONVERT(DECIMAL(18,6), [value]) IS NOT NULL
              AND LOWER(LTRIM(RTRIM([key]))) IN (N'rateperkm', N'resolved_rateperkm', N'fixedtravelallowance');
        END

        DECLARE @RatePerKm DECIMAL(18,6) = NULL;
        DECLARE @FixedTravelAllowance DECIMAL(18,6) = NULL;
        DECLARE @HasFixedAllowance BIT = 0;

        SELECT TOP (1) @RatePerKm = TRY_CONVERT(DECIMAL(18,6), KeyValue)
        FROM @ParsedKeys
        WHERE LOWER(KeyName) IN (N'rateperkm', N'resolved_rateperkm')
        ORDER BY Ord DESC;

        SELECT TOP (1)
            @FixedTravelAllowance = TRY_CONVERT(DECIMAL(18,6), KeyValue),
            @HasFixedAllowance = 1
        FROM @ParsedKeys
        WHERE LOWER(KeyName) = N'fixedtravelallowance'
        ORDER BY Ord DESC;

        /*
           BindGrid uses the resolved rate when it is greater than zero.
           Otherwise it uses GetDefaultRatePerKm. Web.config does not define
           DefaultRatePerKm, so that method returns its code value of 3.75.
           A resolved fixed allowance still takes precedence, including zero.
           Zero kilometres still produce zero, including when a fixed allowance exists.
        */
        DECLARE @ScreenDefaultRatePerKm DECIMAL(18,6) = 3.75;
        DECLARE @AppliedRate DECIMAL(18,6) =
            CASE
                WHEN @RatePerKm IS NOT NULL AND @RatePerKm > 0 THEN @RatePerKm
                ELSE @ScreenDefaultRatePerKm
            END;
        DECLARE @CalculatedClaimAmount DECIMAL(18,2);

        IF @NewKM <= 0
            SET @CalculatedClaimAmount = 0;
        ELSE IF @HasFixedAllowance = 1
            SET @CalculatedClaimAmount = CONVERT(DECIMAL(18,2), ROUND(@FixedTravelAllowance, 2));
        ELSE
            SET @CalculatedClaimAmount = CONVERT(DECIMAL(18,2), ROUND(CONVERT(DECIMAL(18,6), @AppliedRate) * CONVERT(DECIMAL(18,6), @NewKM), 2));

        IF @CalculatedClaimAmount IS NULL
        BEGIN
            THROW 50006, 'The travel allowance was not calculated, so the kilometre change was not saved.', 1;
        END

		UPDATE dbo.SalesDailyActivity
		SET
			TotalDistanceKM = @NewKM,
            CalculatedClaimAmount = @CalculatedClaimAmount,
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
        RAISERROR(@ErrMsg,16,1);
    END CATCH
END
