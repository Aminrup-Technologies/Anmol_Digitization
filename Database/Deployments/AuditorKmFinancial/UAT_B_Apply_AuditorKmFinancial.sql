/*
    UAT only. Auditor KM edit writes the latest detail, then
    dbo.usp_RecalculateSalesClaim calculates the travel amount and request totals.
    A missing or ambiguous resolved rate is rejected. This script does not supply a substitute rate.
    @Execute must stay 0 in the repository copy.
    It refuses catalog AnmolDristi.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Execute bit = 0;
IF @Execute = 0
BEGIN
    PRINT N'NOT EXECUTED | @Execute = 0. No database change was made.';
    RETURN;
END

DECLARE @Db sysname = DB_NAME();
DECLARE @Server nvarchar(128) = CONVERT(nvarchar(128), SERVERPROPERTY('ServerName'));
IF @Db = N'AnmolDristi'
    THROW 59240, N'Refusing catalog AnmolDristi.', 1;
IF @Db <> N'AnmolDristiDev'
    THROW 59241, N'Catalog must be AnmolDristiDev.', 1;
IF @Server <> N'MUMANMOAPP01\SQLEXPRESS'
    THROW 59242, N'Server must be MUMANMOAPP01\SQLEXPRESS.', 1;

DECLARE @Sql nvarchar(max) = N'/*
    Object:      [dbo].[usp_UpdateActivityKM_WithHistory]
    Type:        STORED PROCEDURE
    Purpose:     Auditor KM correction for the current approval detail.

    The travel amount is not calculated here. After the kilometre change is
    locked to the latest detail, dbo.usp_RecalculateSalesClaim writes the
    amount and the eligible request totals. That procedure is the financial
    authority: resolved policy through dbo.usp_GetResolvedPolicy with NULL
    organisation parameters, then an active PolicyException for the claim
    month-end, exception before matrix.

    This procedure refuses the change when no single current policy matches
    the employee, and when that authority would have no positive rate and no
    fixed travel allowance. It does not supply a substitute rate. A rate that
    is present on one matching policy is applied by dbo.usp_RecalculateSalesClaim.
    That procedure recalculates every current day on the request.

    Detail ownership is MAX(DetailVersion), then MAX(ApprovalRequestDetailId).
    Older versions are not updated. Rejected and Returned details are not
    editable here. Header status, approver level, and routing are not changed.
*/

CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateActivityKM_WithHistory]
    @ActivityId INT,
    @NewKM DECIMAL(10,2),
    @ChangedBy NVARCHAR(100),
    @Reason NVARCHAR(1000),
    @ExpectedDetailId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT OFF;

    BEGIN TRY
        IF @ActivityId IS NULL OR @ActivityId <= 0
            THROW 59201, ''Activity is required.'', 1;
        IF @ExpectedDetailId IS NULL OR @ExpectedDetailId <= 0
            THROW 59202, ''The activity detail is missing. Reopen the day and try again.'', 1;
        IF @NewKM IS NULL OR @NewKM < 0
            THROW 59203, ''New KM must be zero or greater.'', 1;
        IF NULLIF(LTRIM(RTRIM(@ChangedBy)), N'''') IS NULL
            THROW 59204, ''The signed-in Auditor is required.'', 1;
        IF NULLIF(LTRIM(RTRIM(@Reason)), N'''') IS NULL
            THROW 59205, ''A reason is required to modify KM.'', 1;

        SET @ChangedBy = LTRIM(RTRIM(@ChangedBy));
        SET @Reason = LTRIM(RTRIM(@Reason));

        BEGIN TRANSACTION;

        DECLARE
              @OldKM DECIMAL(10,2)
            , @ActivityDate DATE
            , @ApprovalRequestId INT
            , @RequestCount INT
            , @LatestDetailId INT
            , @DetailStatus NVARCHAR(50)
            , @HeaderStatus NVARCHAR(50)
            , @CurrentApprover NVARCHAR(100)
            , @CurrentLevel INT
            , @RequesterLoginId NVARCHAR(100)
            , @YearNo INT
            , @MonthNo INT
            , @ApproverRole NVARCHAR(100)
            , @ApproverStatus NVARCHAR(50)
            , @AccountsStatus NVARCHAR(50);

        SELECT
              @OldKM = TotalDistanceKM
            , @ActivityDate = ActivityDate
        FROM dbo.SalesDailyActivity WITH (UPDLOCK, HOLDLOCK)
        WHERE ActivityId = @ActivityId;

        IF @OldKM IS NULL
            THROW 59206, ''Activity not found.'', 1;

        SELECT @RequestCount = COUNT(DISTINCT ApprovalRequestId)
        FROM dbo.SalesApprovalRequestDetail WITH (UPDLOCK, HOLDLOCK)
        WHERE ActivityId = @ActivityId;

        IF ISNULL(@RequestCount, 0) = 0
            THROW 59207, ''This activity has no approval detail, so the kilometre change was not saved.'', 1;
        IF @RequestCount > 1
            THROW 59208, ''This activity belongs to more than one approval request, so the kilometre change was not saved.'', 1;

        SELECT TOP (1) @ApprovalRequestId = ApprovalRequestId
        FROM dbo.SalesApprovalRequestDetail
        WHERE ActivityId = @ActivityId;

        SELECT @LatestDetailId = MAX(d.ApprovalRequestDetailId)
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN (
            SELECT MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
            FROM dbo.SalesApprovalRequestDetail
            WHERE ApprovalRequestId = @ApprovalRequestId
              AND ActivityId = @ActivityId
        ) lv
            ON ISNULL(d.DetailVersion, 1) = lv.LatestVersion
        WHERE d.ApprovalRequestId = @ApprovalRequestId
          AND d.ActivityId = @ActivityId;

        IF @LatestDetailId IS NULL OR @LatestDetailId <> @ExpectedDetailId
            THROW 59209, ''This day was updated by someone else. Reopen the request and try again.'', 1;

        SELECT @DetailStatus = DetailStatus
        FROM dbo.SalesApprovalRequestDetail WITH (UPDLOCK, HOLDLOCK)
        WHERE ApprovalRequestDetailId = @LatestDetailId
          AND ApprovalRequestId = @ApprovalRequestId
          AND ActivityId = @ActivityId;

        IF ISNULL(@DetailStatus, N'''') IN (N''Rejected'', N''Returned'')
            THROW 59210, ''Returned and rejected days are corrected by the employee, not by the Auditor.'', 1;

        SELECT
              @HeaderStatus = Status
            , @CurrentApprover = CurrentApproverLoginId
            , @CurrentLevel = CurrentApproverLevel
            , @RequesterLoginId = RequesterLoginId
            , @YearNo = YearNo
            , @MonthNo = MonthNo
        FROM dbo.SalesApprovalRequest WITH (UPDLOCK, HOLDLOCK)
        WHERE ApprovalRequestId = @ApprovalRequestId;

        IF @HeaderStatus IS NULL
            THROW 59211, ''Approval request was not found.'', 1;
        IF @HeaderStatus <> N''Pending''
            OR NULLIF(LTRIM(RTRIM(@CurrentApprover)), N'''') IS NULL
            OR @CurrentApprover <> @ChangedBy
            THROW 59212, ''KM can be modified only while the request is pending with the Auditor.'', 1;

        SELECT
              @ApproverRole = ApproverRole
            , @ApproverStatus = Status
        FROM dbo.SalesApprovalRequestApprover WITH (UPDLOCK, HOLDLOCK)
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND ApproverLoginId = @ChangedBy
          AND ApproverLevel = @CurrentLevel;

        IF ISNULL(@ApproverRole, N'''') <> N''Auditor'' OR ISNULL(@ApproverStatus, N'''') <> N''Pending''
            THROW 59213, ''Only the Auditor who currently holds this request can modify KM.'', 1;

        SELECT @AccountsStatus = Status
        FROM dbo.SalesApprovalRequestApprover WITH (UPDLOCK, HOLDLOCK)
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND ApproverRole = N''Accounts'';

        IF @AccountsStatus IS NOT NULL AND @AccountsStatus <> N''Pending''
            THROW 59214, ''KM modification is locked as Accounts has already processed the request.'', 1;

        IF @OldKM = @NewKM
            THROW 59215, ''New KM must be different from the current KM.'', 1;

        IF @YearNo IS NULL OR @MonthNo IS NULL OR @MonthNo < 1 OR @MonthNo > 12
            OR NULLIF(LTRIM(RTRIM(@RequesterLoginId)), N'''') IS NULL
            THROW 59216, ''The request has no employee or claim month, so the travel allowance was not calculated.'', 1;

        DECLARE @EmployeeCode VARCHAR(50) = CONVERT(VARCHAR(50), LTRIM(RTRIM(@RequesterLoginId)));
        DECLARE @ClaimDate DATE = EOMONTH(DATEFROMPARTS(@YearNo, @MonthNo, 1));

        DECLARE @PolicyResult TABLE (
              PolicyId INT NOT NULL
            , RegionId INT NULL
            , BranchId INT NULL
            , GradeId INT NULL
            , DesignationId INT NULL
            , PolicyJson NVARCHAR(MAX) NULL
            , Priority INT NOT NULL
            , EffectiveFrom DATE NULL
            , EffectiveTo DATE NULL
            , IsActive BIT NOT NULL
            , CreatedBy NVARCHAR(100) NULL
            , CreatedOn DATETIME2(7) NOT NULL
            , ModifiedBy NVARCHAR(100) NULL
            , ModifiedOn DATETIME2(7) NULL
        );

        DECLARE
              @RegionRaw VARCHAR(200)
            , @BranchRaw VARCHAR(200)
            , @GradeRaw VARCHAR(200)
            , @DesignationRaw VARCHAR(200)
            , @ResolvedRegionId INT
            , @ResolvedBranchId INT
            , @ResolvedGradeId INT
            , @ResolvedDesignationId INT;

        SELECT
              @RegionRaw = NULLIF(LTRIM(RTRIM(TRY_CONVERT(VARCHAR(200), RegionId))), '''')
            , @BranchRaw = NULLIF(LTRIM(RTRIM(TRY_CONVERT(VARCHAR(200), BranchId))), '''')
            , @GradeRaw = NULLIF(LTRIM(RTRIM(TRY_CONVERT(VARCHAR(200), GradeId))), '''')
            , @DesignationRaw = NULLIF(LTRIM(RTRIM(TRY_CONVERT(VARCHAR(200), CategoryId))), '''')
        FROM dbo.MST_UserMaster
        WHERE EmployeeCode = @EmployeeCode;

        IF @RegionRaw IS NOT NULL
            EXEC dbo.usp_ResolveRegion @InputValue = @RegionRaw, @ResolvedId = @ResolvedRegionId OUTPUT;
        IF @BranchRaw IS NOT NULL
            EXEC dbo.usp_ResolveBranch @InputValue = @BranchRaw, @ResolvedId = @ResolvedBranchId OUTPUT;
        IF @GradeRaw IS NOT NULL
            EXEC dbo.usp_ResolveGrade @InputValue = @GradeRaw, @ResolvedId = @ResolvedGradeId OUTPUT;
        IF @DesignationRaw IS NOT NULL
            EXEC dbo.usp_ResolveDesignation @InputValue = @DesignationRaw, @ResolvedId = @ResolvedDesignationId OUTPUT;

        DECLARE @MatchCount INT;
        SELECT @MatchCount = COUNT(*)
        FROM dbo.PolicyMatrix pm
        WHERE pm.IsActive = 1
          AND (pm.EffectiveFrom IS NULL OR pm.EffectiveFrom <= CONVERT(date, GETDATE()))
          AND (pm.EffectiveTo IS NULL OR pm.EffectiveTo >= CONVERT(date, GETDATE()))
          AND (@ResolvedRegionId IS NULL OR pm.RegionId = @ResolvedRegionId)
          AND (@ResolvedBranchId IS NULL OR pm.BranchId = @ResolvedBranchId)
          AND (@ResolvedGradeId IS NULL OR pm.GradeId = @ResolvedGradeId)
          AND (@ResolvedDesignationId IS NULL OR pm.DesignationId = @ResolvedDesignationId);

        INSERT INTO @PolicyResult (
              PolicyId, RegionId, BranchId, GradeId, DesignationId, PolicyJson
            , Priority, EffectiveFrom, EffectiveTo, IsActive
            , CreatedBy, CreatedOn, ModifiedBy, ModifiedOn
        )
        EXEC dbo.usp_GetResolvedPolicy
              @EmployeeCode = @EmployeeCode
            , @RegionIdParam = NULL
            , @BranchIdParam = NULL
            , @GradeIdParam = NULL
            , @DesignationIdParam = NULL;

        DECLARE @MatrixJson NVARCHAR(MAX);
        SELECT TOP (1) @MatrixJson = PolicyJson
        FROM @PolicyResult;

        DECLARE @ExceptionJson NVARCHAR(MAX);
        SELECT TOP (1) @ExceptionJson = pe.PolicyJson
        FROM dbo.PolicyException pe
        WHERE pe.EmployeeCode = @EmployeeCode
          AND pe.IsActive = 1
          AND pe.EffectiveFrom <= @ClaimDate
          AND (pe.EffectiveTo IS NULL OR pe.EffectiveTo >= @ClaimDate)
        ORDER BY pe.Priority ASC, pe.ExceptionId ASC;

        IF @MatrixJson IS NOT NULL AND ISJSON(@MatrixJson) <> 1 SET @MatrixJson = NULL;
        IF @ExceptionJson IS NOT NULL AND ISJSON(@ExceptionJson) <> 1 SET @ExceptionJson = NULL;

        DECLARE @PolicyValues TABLE (
              SourceRank INT NOT NULL
            , KeyName NVARCHAR(100) NOT NULL
            , KeyValue NVARCHAR(200) NULL
        );

        INSERT INTO @PolicyValues (SourceRank, KeyName, KeyValue)
        SELECT 1, LOWER([key]), CONVERT(NVARCHAR(200), [value])
        FROM OPENJSON(ISNULL(@ExceptionJson, N''{}''))
        WHERE [type] IN (1, 2) AND [value] IS NOT NULL;

        INSERT INTO @PolicyValues (SourceRank, KeyName, KeyValue)
        SELECT 2, LOWER([key]), CONVERT(NVARCHAR(200), [value])
        FROM OPENJSON(ISNULL(@MatrixJson, N''{}''))
        WHERE [type] IN (1, 2) AND [value] IS NOT NULL;

        DECLARE @ResolvedRate DECIMAL(18,6);
        DECLARE @RateSource INT;
        DECLARE @FixedRaw NVARCHAR(200);
        DECLARE @HasFixed BIT = 0;

        SELECT TOP (1)
              @ResolvedRate = TRY_CONVERT(DECIMAL(18,6), KeyValue)
            , @RateSource = SourceRank
        FROM @PolicyValues
        WHERE KeyName IN (N''rateperkm'', N''resolved_rateperkm'')
        ORDER BY SourceRank ASC;

        SELECT TOP (1) @FixedRaw = KeyValue
        FROM @PolicyValues
        WHERE KeyName = N''fixedtravelallowance''
        ORDER BY SourceRank ASC;

        IF @FixedRaw IS NOT NULL AND TRY_CONVERT(DECIMAL(18,6), @FixedRaw) IS NOT NULL
            SET @HasFixed = 1;

        IF @HasFixed = 0 AND NOT (@RateSource = 1 AND @ResolvedRate > 0)
        BEGIN
            IF ISNULL(@MatchCount, 0) > 1
                THROW 59217, ''More than one current policy matches this employee, so the kilometre change was not saved.'', 1;
            IF ISNULL(@MatchCount, 0) <> 1 OR @ResolvedRate IS NULL OR @ResolvedRate <= 0
                THROW 59218, ''No resolved travel rate or fixed travel allowance applies to this employee, so the kilometre change was not saved.'', 1;
        END

        UPDATE dbo.SalesDailyActivity
        SET
              TotalDistanceKM = @NewKM
            , ModifiedByUserId = @ChangedBy
            , ModifiedDateTime = DATEADD(MINUTE, 330, SYSUTCDATETIME())
        WHERE ActivityId = @ActivityId;

        INSERT INTO dbo.SalesDailyActivityKMHistory
            (ActivityId, OldKM, NewKM, ModifiedBy, Reason)
        VALUES
            (@ActivityId, @OldKM, @NewKM, @ChangedBy, @Reason);

        CREATE TABLE #RecalcClaimActivity (
              ActivityId INT NULL
            , ActivityDate DATE NULL
            , TotalDistanceKM DECIMAL(18,2) NULL
            , RatePerKm DECIMAL(18,2) NULL
            , CalculatedClaimAmount DECIMAL(18,2) NULL
            , DAAmount DECIMAL(18,2) NULL
            , HotelAmount DECIMAL(18,2) NULL
            , TravelAmount DECIMAL(18,2) NULL
            , StationaryAmount DECIMAL(18,2) NULL
            , ExpenseTotal DECIMAL(18,2) NULL
            , OverallAmount DECIMAL(18,2) NULL
        );

        CREATE TABLE #RecalcClaimTotals (
              TotalKM DECIMAL(18,2) NULL
            , TotalClaimAmount DECIMAL(18,2) NULL
            , TotalDAAmount DECIMAL(18,2) NULL
            , TotalHotelAmount DECIMAL(18,2) NULL
            , TotalTravelAmount DECIMAL(18,2) NULL
            , TotalStationaryAmount DECIMAL(18,2) NULL
            , TotalExpenseAmount DECIMAL(18,2) NULL
            , OverallAmount DECIMAL(18,2) NULL
            , ApprovedKM DECIMAL(18,2) NULL
            , ApprovedClaimAmount DECIMAL(18,2) NULL
            , ApprovedDAAmount DECIMAL(18,2) NULL
            , ApprovedHotelAmount DECIMAL(18,2) NULL
            , ApprovedTravelAmount DECIMAL(18,2) NULL
            , FinalApprovedAmount DECIMAL(18,2) NULL
        );

        EXEC dbo.usp_RecalculateSalesClaim @ApprovalRequestId = @ApprovalRequestId;

        UPDATE a
        SET a.CalculatedClaimAmount = c.CalculatedClaimAmount
        FROM dbo.SalesDailyActivity a
        INNER JOIN #RecalcClaimActivity c ON c.ActivityId = a.ActivityId
        WHERE a.ActivityId = @ActivityId;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.SalesApprovalRequestDetail
            WHERE ApprovalRequestDetailId = @LatestDetailId
              AND Snapshot_TotalDistanceKM = @NewKM
        )
            THROW 59219, ''The current detail did not receive the new KM, so the change was not saved.'', 1;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
';
EXEC sys.sp_executesql @Sql;
PRINT N'APPLIED | Auditor KM financial procedure is installed on AnmolDristiDev.';