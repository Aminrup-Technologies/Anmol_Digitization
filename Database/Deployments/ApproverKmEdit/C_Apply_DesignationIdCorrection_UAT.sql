/*
    UAT only. Corrects the designation source in
    dbo.usp_UpdateActivityKM_WithHistory on AnmolDristiDev.

    The candidate deployed by B_Apply_ApproverKmEdit_UAT.sql reads
    MST_UserMaster.CategoryId into @DesignationRaw. This script replaces
    that one column with MST_UserMaster.Designation_ID. No other procedure
    text changes.

    B_Apply_ApproverKmEdit_UAT.sql stays as the consumed deployment of that
    candidate. Its preimage guard is the older module and must not be rerun.
    This script does not alter tables, dbo.usp_RecalculateSalesClaim, or
    dbo.usp_ResolveDesignation, and it does not execute the KM procedure.

    @Execute must stay 0 in the repository copy.
    @Execute = 0 is read-only preflight: server, catalog, live fingerprint,
    dependencies, and schema are checked, then the script returns.
    @Execute = 1 applies the corrected procedure only after those checks pass.
    The live module must be the deployed candidate, catalog SHA-256
    B1C1F94904E4F84DD22C23E0ECE21F62B1AF43F83AEE614589D47EF760A04EC2,
    or already be this correction.

    Catalog comparison uses the stored module with ALTER PROCEDURE
    normalized to CREATE   PROCEDURE. The fingerprint is
    HASHBYTES('SHA2_256', CONVERT(varbinary(max), module)) over UTF-16LE,
    with no byte-order mark. Leading comments, the three spaces in
    CREATE   PROCEDURE, and CRLF line endings stay significant.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Execute bit = 0;
DECLARE @ExpectedPrevHash varchar(64) = 'B1C1F94904E4F84DD22C23E0ECE21F62B1AF43F83AEE614589D47EF760A04EC2';
DECLARE @ExpectedNextHash varchar(64) = 'F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9';

IF @@TRANCOUNT <> 0
    THROW 59301, N'ABORT: this script must start with no open transaction.', 1;
IF CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName')) <> N'MUMANMOAPP01\SQLEXPRESS'
    THROW 59302, N'ABORT: server must be MUMANMOAPP01\SQLEXPRESS.', 1;
IF DB_NAME() = N'AnmolDristi'
    THROW 59303, N'ABORT: Live catalog AnmolDristi is forbidden.', 1;
IF DB_NAME() <> N'AnmolDristiDev'
    THROW 59304, N'ABORT: catalog must be AnmolDristiDev.', 1;

DECLARE @Prev nvarchar(max) = N'/*
    Object:      [dbo].[usp_UpdateActivityKM_WithHistory]
    Type:        STORED PROCEDURE
    Purpose:     Current-holder KM correction for the latest approval detail.

    The current approver may increase or decrease KM while the request is
    Pending with that approver. Eligible roles are L1, L2, L3, and Auditor.
    Reporting_Manager1 is accepted as L1 and the history row stores L1.
    Historical Reporting_Manager1 assignments are not rewritten. This path
    does not read or consume the employee KM modification count.

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

CREATE   PROCEDURE [dbo].[usp_UpdateActivityKM_WithHistory]
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
            THROW 59204, ''The signed-in approver is required.'', 1;
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
            , @CanonicalRole NVARCHAR(20)
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
            THROW 59210, ''Returned and rejected days are corrected by the employee.'', 1;

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
            THROW 59212, ''KM can be modified only while the request is pending with the current approver.'', 1;

        SELECT
              @ApproverRole = ApproverRole
            , @ApproverStatus = Status
        FROM dbo.SalesApprovalRequestApprover WITH (UPDLOCK, HOLDLOCK)
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND ApproverLoginId = @ChangedBy
          AND ApproverLevel = @CurrentLevel;

        SET @CanonicalRole =
            CASE UPPER(LTRIM(RTRIM(@ApproverRole)))
                WHEN N''L1'' THEN N''L1''
                WHEN N''REPORTING_MANAGER1'' THEN N''L1''
                WHEN N''L2'' THEN N''L2''
                WHEN N''L3'' THEN N''L3''
                WHEN N''AUDITOR'' THEN N''Auditor''
                ELSE NULL
            END;

        IF @CanonicalRole IS NULL OR ISNULL(@ApproverStatus, N'''') <> N''Pending''
            THROW 59213, ''Only the approver who currently holds this request can modify KM.'', 1;

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
            (ActivityId, OldKM, NewKM, ModifiedBy, ModifiedDateTime, Reason, ApprovalRequestId, ApproverRole, ApproverLevel)
        VALUES
            (@ActivityId, @OldKM, @NewKM, @ChangedBy, DATEADD(MINUTE, 330, SYSUTCDATETIME()), @Reason, @ApprovalRequestId, @CanonicalRole, @CurrentLevel);

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
DECLARE @Next nvarchar(max) = N'/*
    Object:      [dbo].[usp_UpdateActivityKM_WithHistory]
    Type:        STORED PROCEDURE
    Purpose:     Current-holder KM correction for the latest approval detail.

    The current approver may increase or decrease KM while the request is
    Pending with that approver. Eligible roles are L1, L2, L3, and Auditor.
    Reporting_Manager1 is accepted as L1 and the history row stores L1.
    Historical Reporting_Manager1 assignments are not rewritten. This path
    does not read or consume the employee KM modification count.

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

CREATE   PROCEDURE [dbo].[usp_UpdateActivityKM_WithHistory]
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
            THROW 59204, ''The signed-in approver is required.'', 1;
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
            , @CanonicalRole NVARCHAR(20)
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
            THROW 59210, ''Returned and rejected days are corrected by the employee.'', 1;

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
            THROW 59212, ''KM can be modified only while the request is pending with the current approver.'', 1;

        SELECT
              @ApproverRole = ApproverRole
            , @ApproverStatus = Status
        FROM dbo.SalesApprovalRequestApprover WITH (UPDLOCK, HOLDLOCK)
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND ApproverLoginId = @ChangedBy
          AND ApproverLevel = @CurrentLevel;

        SET @CanonicalRole =
            CASE UPPER(LTRIM(RTRIM(@ApproverRole)))
                WHEN N''L1'' THEN N''L1''
                WHEN N''REPORTING_MANAGER1'' THEN N''L1''
                WHEN N''L2'' THEN N''L2''
                WHEN N''L3'' THEN N''L3''
                WHEN N''AUDITOR'' THEN N''Auditor''
                ELSE NULL
            END;

        IF @CanonicalRole IS NULL OR ISNULL(@ApproverStatus, N'''') <> N''Pending''
            THROW 59213, ''Only the approver who currently holds this request can modify KM.'', 1;

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
            , @DesignationRaw = NULLIF(LTRIM(RTRIM(TRY_CONVERT(VARCHAR(200), Designation_ID))), '''')
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
            (ActivityId, OldKM, NewKM, ModifiedBy, ModifiedDateTime, Reason, ApprovalRequestId, ApproverRole, ApproverLevel)
        VALUES
            (@ActivityId, @OldKM, @NewKM, @ChangedBy, DATEADD(MINUTE, 330, SYSUTCDATETIME()), @Reason, @ApprovalRequestId, @CanonicalRole, @CurrentLevel);

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

IF LEN(@Prev) <> 16350 OR DATALENGTH(@Prev) <> 32700
    OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Prev)), 2) <> @ExpectedPrevHash
    THROW 59305, N'ABORT: embedded deployed candidate is not the reviewed fingerprint. Nothing was changed.', 1;
IF LEN(@Next) <> 16354 OR DATALENGTH(@Next) <> 32708
    OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Next)), 2) <> @ExpectedNextHash
    THROW 59306, N'ABORT: embedded corrected procedure is not the reviewed fingerprint. Nothing was changed.', 1;
IF (LEN(@Prev) - LEN(REPLACE(@Prev, N'CREATE   PROCEDURE', N''))) / LEN(N'CREATE   PROCEDURE') <> 1
    OR (LEN(@Next) - LEN(REPLACE(@Next, N'CREATE   PROCEDURE', N''))) / LEN(N'CREATE   PROCEDURE') <> 1
    THROW 59307, N'ABORT: embedded procedure text does not contain exactly one CREATE   PROCEDURE token. Nothing was changed.', 1;
IF (LEN(@Prev) - LEN(REPLACE(@Prev, N'TRY_CONVERT(VARCHAR(200), CategoryId)', N''))) / LEN(N'TRY_CONVERT(VARCHAR(200), CategoryId)') <> 1
    OR CHARINDEX(N'CategoryId', @Next) <> 0
    OR (LEN(@Next) - LEN(REPLACE(@Next, N'TRY_CONVERT(VARCHAR(200), Designation_ID)', N''))) / LEN(N'TRY_CONVERT(VARCHAR(200), Designation_ID)') <> 1
    OR REPLACE(@Prev, N'TRY_CONVERT(VARCHAR(200), CategoryId)', N'TRY_CONVERT(VARCHAR(200), Designation_ID)') <> @Next
    THROW 59308, N'ABORT: embedded texts differ by more than the designation source. Nothing was changed.', 1;

DECLARE @Live nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_UpdateActivityKM_WithHistory', N'P'));
IF @Live IS NULL
    THROW 59309, N'ABORT: dbo.usp_UpdateActivityKM_WithHistory is missing.', 1;
DECLARE @Recalc nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RecalculateSalesClaim', N'P'));
IF @Recalc IS NULL
    THROW 59310, N'ABORT: dbo.usp_RecalculateSalesClaim is missing. It was not modified.', 1;

IF OBJECT_ID(N'dbo.usp_ResolveRegion', N'P') IS NULL
    OR OBJECT_ID(N'dbo.usp_ResolveBranch', N'P') IS NULL
    OR OBJECT_ID(N'dbo.usp_ResolveGrade', N'P') IS NULL
    OR OBJECT_ID(N'dbo.usp_ResolveDesignation', N'P') IS NULL
    OR OBJECT_ID(N'dbo.usp_GetResolvedPolicy', N'P') IS NULL
    OR OBJECT_ID(N'dbo.MST_UserMaster', N'U') IS NULL
    OR OBJECT_ID(N'dbo.MST_Designation', N'U') IS NULL
    OR OBJECT_ID(N'dbo.PolicyMatrix', N'U') IS NULL
    OR OBJECT_ID(N'dbo.PolicyException', N'U') IS NULL
    OR OBJECT_ID(N'dbo.SalesDailyActivity', N'U') IS NULL
    OR OBJECT_ID(N'dbo.SalesDailyActivityKMHistory', N'U') IS NULL
    OR OBJECT_ID(N'dbo.SalesApprovalRequest', N'U') IS NULL
    OR OBJECT_ID(N'dbo.SalesApprovalRequestDetail', N'U') IS NULL
    OR OBJECT_ID(N'dbo.SalesApprovalRequestApprover', N'U') IS NULL
    THROW 59312, N'ABORT: a required procedure or table is missing. Nothing was changed.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns AS c
    INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.MST_UserMaster')
      AND c.name = N'Designation_ID' AND ty.name = N'int' AND c.is_nullable = 1
) THROW 59313, N'ABORT: MST_UserMaster.Designation_ID is not int NULL. Nothing was changed.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns AS c
    INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.MST_Designation')
      AND c.name = N'Id' AND ty.name = N'int' AND c.is_nullable = 0
) THROW 59313, N'ABORT: MST_Designation.Id is not int NOT NULL. Nothing was changed.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns AS c
    INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.PolicyMatrix')
      AND c.name = N'DesignationId' AND ty.name = N'int' AND c.is_nullable = 1
) THROW 59313, N'ABORT: PolicyMatrix.DesignationId is not int NULL. Nothing was changed.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns AS c
    INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
      AND c.name = N'Reason' AND ty.name = N'nvarchar' AND c.max_length = 2000 AND c.is_nullable = 1
) THROW 59313, N'ABORT: Reason is not nvarchar(1000) NULL. Nothing was changed.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns AS c
    INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
      AND c.name = N'OldKM' AND ty.name = N'decimal' AND c.precision = 10 AND c.scale = 2 AND c.is_nullable = 1
) THROW 59313, N'ABORT: OldKM is not decimal(10,2) NULL. Nothing was changed.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns AS c
    INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
      AND c.name = N'ModifiedBy' AND ty.name = N'nvarchar' AND c.max_length = 200 AND c.is_nullable = 1
) THROW 59313, N'ABORT: ModifiedBy is not nvarchar(100) NULL. Nothing was changed.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns AS c
    INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
      AND c.name = N'NewKM' AND ty.name = N'decimal' AND c.precision = 10 AND c.scale = 2 AND c.is_nullable = 1
) THROW 59313, N'ABORT: NewKM is not decimal(10,2) NULL. Nothing was changed.', 1;

IF EXISTS (
    SELECT 1
    FROM sys.default_constraints AS dc
    INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
      AND c.name IN (N'ModifiedBy', N'NewKM')
) THROW 59313, N'ABORT: ModifiedBy or NewKM has a default. Nothing was changed.', 1;

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns AS c
    INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
      AND c.name = N'ModifiedDateTime' AND ty.name = N'datetime' AND c.is_nullable = 0
) THROW 59313, N'ABORT: ModifiedDateTime is not datetime NOT NULL. Nothing was changed.', 1;

DECLARE @ModifiedDateDefault nvarchar(max);
SELECT @ModifiedDateDefault = dc.definition
FROM sys.default_constraints AS dc
INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
  AND c.name = N'ModifiedDateTime';
IF (
    SELECT COUNT(*)
    FROM sys.default_constraints AS dc
    INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
      AND c.name = N'ModifiedDateTime'
) <> 1
    OR REPLACE(REPLACE(REPLACE(REPLACE(LOWER(ISNULL(@ModifiedDateDefault, N'')), N' ', N''), CHAR(9), N''), CHAR(13), N''), CHAR(10), N'')
       NOT IN (N'(dateadd(minute,(330),sysutcdatetime()))', N'(dateadd(minute,330,sysutcdatetime()))')
    THROW 59313, N'ABORT: ModifiedDateTime default is not DATEADD(MINUTE, 330, SYSUTCDATETIME()). Nothing was changed.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns AS c INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
      AND c.name = N'ApprovalRequestId' AND ty.name = N'int' AND c.is_nullable = 1
) OR NOT EXISTS (
    SELECT 1 FROM sys.columns AS c INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
      AND c.name = N'ApproverRole' AND ty.name = N'nvarchar' AND c.max_length = 40 AND c.is_nullable = 1
) OR NOT EXISTS (
    SELECT 1 FROM sys.columns AS c INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
      AND c.name = N'ApproverLevel' AND ty.name = N'int' AND c.is_nullable = 1
)
    THROW 59313, N'ABORT: approver KM audit columns are missing or the wrong type. Nothing was changed.', 1;

DECLARE @Module nvarchar(max) = REPLACE(@Live, N'ALTER   PROCEDURE', N'CREATE   PROCEDURE');
DECLARE @ModuleHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Module)), 2);
PRINT N'CAPTURE | usp_UpdateActivityKM_WithHistory LEN='
    + CONVERT(nvarchar(20), LEN(@Live))
    + N' SHA2_256='
    + CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Live)), 2);
PRINT N'CAPTURE | usp_RecalculateSalesClaim LEN='
    + CONVERT(nvarchar(20), LEN(@Recalc))
    + N' SHA2_256='
    + CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Recalc)), 2)
    + N' | definition was not changed.';

IF @Module = @Next AND @ModuleHash = @ExpectedNextHash
BEGIN
    PRINT N'ALREADY APPLIED | Designation_ID correction is already stored. No change was made.';
    RETURN;
END;
IF @Module <> @Prev OR @ModuleHash <> @ExpectedPrevHash
    THROW 59311, N'ABORT: stored procedure is not the expected deployed candidate. Nothing was changed.', 1;

IF @Execute <> 1
BEGIN
    PRINT N'PREFLIGHT | Live fingerprint matches the deployed candidate. @Execute = 0. No database change was made.';
    RETURN;
END;

BEGIN TRAN;

DECLARE @Alter nvarchar(max) = REPLACE(@Next, N'CREATE   PROCEDURE', N'ALTER   PROCEDURE');
EXEC sys.sp_executesql @Alter;

DECLARE @Stored nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_UpdateActivityKM_WithHistory', N'P'));
DECLARE @StoredModule nvarchar(max) = REPLACE(@Stored, N'ALTER   PROCEDURE', N'CREATE   PROCEDURE');
DECLARE @StoredHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @StoredModule)), 2);
IF @StoredModule IS NULL OR @StoredModule <> @Next OR @StoredHash <> @ExpectedNextHash
    OR (LEN(@StoredModule) - LEN(REPLACE(@StoredModule, N'CREATE   PROCEDURE', N''))) / LEN(N'CREATE   PROCEDURE') <> 1
BEGIN
    ROLLBACK TRAN;
    THROW 59314, N'ABORT: stored procedure did not match the corrected candidate. Transaction rolled back.', 1;
END;

DECLARE @RecalcAfter nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RecalculateSalesClaim', N'P'));
IF @RecalcAfter IS NULL OR @RecalcAfter <> @Recalc
BEGIN
    ROLLBACK TRAN;
    THROW 59315, N'ABORT: dbo.usp_RecalculateSalesClaim changed. Transaction rolled back.', 1;
END;

COMMIT TRAN;
PRINT N'COMMITTED | usp_UpdateActivityKM_WithHistory SHA2_256='
    + @StoredHash
    + N'. usp_RecalculateSalesClaim was not modified. SHA2_256='
    + CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @RecalcAfter)), 2);