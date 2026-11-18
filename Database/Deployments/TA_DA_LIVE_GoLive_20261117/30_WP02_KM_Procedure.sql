/*
    Package:  TA/DA LIVE Go-Live 20261117
    File:     30_WP02_KM_Procedure.sql
    Source:   commit 21975c200d9c9bed1f24a7b97ec802ff22faa2f4
              Database/Stored Procedures/Approval/usp_UpdateActivityKM_WithHistory.sql
    Canonical module: that file, including its header comment, with the single
              token CREATE OR ALTER PROCEDURE replaced by CREATE   PROCEDURE
              (three spaces). SHA-256 of that nvarchar as UTF-16LE, no BOM, is
              F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9.
    Apply:    the same text with that one token replaced by ALTER   PROCEDURE.
    This revision aborts. @ClaimEvidenceFiled remains NOT-FILED.
    It does not modify usp_RecalculateSalesClaim or usp_RecalculateApprovalRequest.

    2026-11-18: AnmolDristi already stores KM hash F6B07BE8, matching AnmolDristiDev.
    Do not sign the gates in order to run this file.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @GateBackup nvarchar(40) = N'UNSIGNED';
DECLARE @GateVerify nvarchar(40) = N'UNSIGNED';
DECLARE @GateIsolated nvarchar(40) = N'UNSIGNED';
DECLARE @BackupFinish datetime = NULL;
DECLARE @IsolatedRestoreDatabase sysname = N'';
DECLARE @GateIdentity nvarchar(40) = N'UNSIGNED';
DECLARE @ApprovedLogin sysname = N'';
DECLARE @GateColumns nvarchar(40) = N'UNSIGNED';
DECLARE @GateNoFk nvarchar(40) = N'UNSIGNED';
DECLARE @GateKm nvarchar(40) = N'UNSIGNED';
DECLARE @GateClaimTx nvarchar(40) = N'UNSIGNED';
DECLARE @ClaimEvidenceFiled nvarchar(40) = N'NOT-FILED';
DECLARE @ExpectedResolveRegion char(64) = N'';
DECLARE @ExpectedResolveBranch char(64) = N'';
DECLARE @ExpectedResolveGrade char(64) = N'';

-- MODULE-LITERAL-START
DECLARE @Submitted nvarchar(max) = N'/*
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
-- MODULE-LITERAL-END

BEGIN TRY
    DECLARE @CandidateSha char(64) = N'F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9';
    DECLARE @LivePreimageSha char(64) = N'EC6A97B97605B2A12857367B78C7C84F277289443DFF267C4BB29118E258E100';

    IF CHARINDEX(N'Designation_ID', @Submitted) = 0
        OR CHARINDEX(N'CategoryId', @Submitted) > 0
        OR CHARINDEX(N'CREATE OR ALTER PROCEDURE', @Submitted) > 0
        OR (LEN(@Submitted) - LEN(REPLACE(@Submitted, N'CREATE   PROCEDURE', N''))) / LEN(N'CREATE   PROCEDURE') <> 1
        OR LEN(@Submitted) <> 16354
        OR CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Submitted)), 2) <> @CandidateSha
        THROW 59511, N'ABORT: submitted KM text is not the verified F6B07BE8 module.', 1;

    IF UPPER(CONVERT(nvarchar(256), SERVERPROPERTY('ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
        THROW 59501, N'ABORT: server is not MUMANMOAPP01\SQLEXPRESS.', 1;
    IF DB_NAME() = N'AnmolDristiDev'
        THROW 59502, N'ABORT: AnmolDristiDev is refused.', 1;
    IF DB_NAME() <> N'AnmolDristi'
        THROW 59502, N'ABORT: database is not AnmolDristi.', 1;
    IF @@TRANCOUNT <> 0
        THROW 59503, N'ABORT: a transaction is already open.', 1;
    IF DATABASEPROPERTYEX(DB_NAME(), N'Updateability') <> N'READ_WRITE'
        THROW 59502, N'ABORT: database is not read-write.', 1;

    IF @GateColumns <> N'DR-COLUMNS-A' OR @GateNoFk <> N'NO-FK-NO-INDEX'
        THROW 59504, N'ABORT: DR-COLUMNS is unsigned in this revision.', 1;
    IF @GateKm <> N'DR-KM-A'
        THROW 59504, N'ABORT: 21975c2 is not accepted in this revision.', 1;
    IF @GateClaimTx <> N'DR-CLAIM-TX-A' OR @ClaimEvidenceFiled <> N'FULL-MODULE-TEXT-FILED'
        THROW 59515, N'ABORT: the LIVE usp_RecalculateSalesClaim module text is not filed. Nested transaction behaviour is unreviewed.', 1;
    IF @GateBackup <> N'DR-BACKUP-A'
        OR @GateVerify <> N'VERIFYONLY-CHECKSUM-ACCEPTED'
        OR @GateIsolated <> N'ISOLATED-RESTORE-ACCEPTED'
        OR @BackupFinish IS NULL
        OR NULLIF(@IsolatedRestoreDatabase, N'') IS NULL
        OR @IsolatedRestoreDatabase IN (N'AnmolDristi', N'AnmolDristiDev')
        THROW 59506, N'ABORT: DR-BACKUP is unsigned in this revision.', 1;
    IF @GateIdentity <> N'DR-IDENTITY-A'
        OR NULLIF(@ApprovedLogin, N'') IS NULL
        OR @ApprovedLogin = N'sa'
        OR SUSER_SNAME() <> @ApprovedLogin
        OR ORIGINAL_LOGIN() = N'sa'
        OR SUSER_SNAME() = N'sa'
        OR IS_SRVROLEMEMBER(N'sysadmin') = 1
        OR IS_ROLEMEMBER(N'db_owner') = 1
        OR IS_ROLEMEMBER(N'db_ddladmin') = 1
        OR ISNULL(HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'ALTER ANY PROCEDURE'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo', N'SCHEMA', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo', N'SCHEMA', N'CONTROL'), 0) = 1
        THROW 59505, N'ABORT: deployment identity is unsigned or wider than this package.', 1;

    IF OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'U') IS NULL
        THROW 59512, N'ABORT: evidence table is absent. This identity does not create it.', 1;
    IF (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage')) <> 13
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'WorkPackage' AND TYPE_NAME(user_type_id) = N'varchar' AND max_length = 8 AND is_nullable = 0)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'ObjectName' AND TYPE_NAME(user_type_id) IN (N'sysname', N'nvarchar') AND max_length = 256 AND is_nullable = 0)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'PreimageSha256' AND TYPE_NAME(user_type_id) = N'char' AND max_length = 64 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'PreimageLen' AND TYPE_NAME(user_type_id) = N'int' AND max_length = 4 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'PreimageDataLength' AND TYPE_NAME(user_type_id) = N'int' AND max_length = 4 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'ModuleDefinition' AND TYPE_NAME(user_type_id) = N'nvarchar' AND max_length = -1 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'SubmittedSha256' AND TYPE_NAME(user_type_id) = N'char' AND max_length = 64 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'AppliedSha256' AND TYPE_NAME(user_type_id) = N'char' AND max_length = 64 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'HistoryRowCount' AND TYPE_NAME(user_type_id) = N'bigint' AND max_length = 8 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'HistoryIdMin' AND TYPE_NAME(user_type_id) = N'int' AND max_length = 4 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'HistoryIdSum' AND TYPE_NAME(user_type_id) = N'bigint' AND max_length = 8 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'SchemaSnapshot' AND TYPE_NAME(user_type_id) = N'nvarchar' AND max_length = -1 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'CapturedAtUtc' AND TYPE_NAME(user_type_id) = N'datetime2' AND scale = 3 AND is_nullable = 0)
        OR NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'PK_TaDaLiveGoLive_20261117_Preimage' AND type = 'PK')
        OR (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage')) <> 1
        OR (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage')) <> 1
        OR NOT EXISTS (
            SELECT 1 FROM sys.check_constraints
            WHERE parent_object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage')
              AND name = N'CK_TaDaLiveGoLive_20261117_WorkPackage'
              AND definition LIKE N'%WP01%' AND definition LIKE N'%WP02%' AND definition NOT LIKE N'%WP03%'
        )
        OR (SELECT COUNT(*) FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage')) <> 1
        OR NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints AS dc
            INNER JOIN sys.columns AS col ON col.object_id = dc.parent_object_id AND col.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage')
              AND dc.name = N'DF_TaDaLiveGoLive_20261117_Captured'
              AND col.name = N'CapturedAtUtc'
              AND LOWER(dc.definition) LIKE N'%sysutcdatetime%'
        )
        OR EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage'))
        THROW 59512, N'ABORT: evidence table shape is not the WP-01/WP-02 definition.', 1;

    IF ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_UpdateActivityKM_WithHistory', N'OBJECT', N'ALTER'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_UpdateActivityKM_WithHistory', N'OBJECT', N'VIEW DEFINITION'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_UpdateActivityKM_WithHistory', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo', N'SCHEMA', N'VIEW DEFINITION'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'SELECT'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'INSERT'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'UPDATE'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'DELETE'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'msdb.dbo.backupset', N'OBJECT', N'SELECT'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateApprovalRequest', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateApprovalRequest', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateSalesClaim', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateSalesClaim', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_GetResolvedPolicy', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ResolveDesignation', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ResolveRegion', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ResolveBranch', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ResolveGrade', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ProcessApprovalAction', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_MarkRequestReadyForPayment', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ForwardApprovalRequest', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_UpdateActivityKM', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'INSERT'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'UPDATE'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'DELETE'), 0) = 1
        THROW 59507, N'ABORT: object rights do not match the WP-01/WP-02 permission matrix.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM msdb.dbo.backupset AS b
        WHERE b.database_name = N'AnmolDristi' AND b.type = 'D' AND b.is_damaged = 0
          AND b.has_backup_checksums = 1 AND b.backup_finish_date = @BackupFinish
    )
        THROW 59506, N'ABORT: no undamaged checksum full backup matches the signed finish time.', 1;

    DECLARE @pinned TABLE (ObjectName sysname NOT NULL PRIMARY KEY, ExpectedLen int NOT NULL, ExpectedBytes int NOT NULL, ExpectedSha char(64) NOT NULL);
    INSERT INTO @pinned VALUES
     (N'usp_RecalculateApprovalRequest',    7567, 15134, '08F34AF8C5CC188F2A0877985FB7FC5E70DEB0067034B95264AE6EF4739ED5D7'),
     (N'usp_RecalculateSalesClaim',         28786, 57572, 'FC60A9CFC68BA480454A1D4A4653EAA9426D281267E6750AC3CD96DCC9A33ABC'),
     (N'usp_GetResolvedPolicy',              3297,  6594, '99ADB00C37DE0C62400F90B0CE4F6BB195A98129267A7221E490E196ACC2DA90'),
     (N'usp_ResolveDesignation',             1008,  2016, '3E65716AB518D0B17C59F2EAACF468CA6711575A0A98E4CC099750C460FE86E0'),
     (N'usp_ProcessApprovalAction',         42031, 84062, 'FF87E2AAD1319EB48F8237ED9293F674FE4F508E64F39CFDC41DBBA24AFC1392'),
     (N'usp_MarkRequestReadyForPayment',    11189, 22378, '2D565A377B668BFC23611F8288B8532B1F24727609EFA32394900B95EC6A69E2'),
     (N'usp_ForwardApprovalRequest',        16459, 32918, '02BC07F01C3EDB0B4519561427F679D6B897DBC74527BFC518970F0C000A9F77');

    IF EXISTS
    (
        SELECT 1
        FROM @pinned AS p
        CROSS APPLY (SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + p.ObjectName)) AS definition) AS d
        WHERE d.definition IS NULL
           OR LEN(d.definition) <> p.ExpectedLen
           OR DATALENGTH(d.definition) <> p.ExpectedBytes
           OR CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), d.definition)), 2) <> p.ExpectedSha
    )
        THROW 59508, N'ABORT: an excluded module fingerprint does not match the 2026-11-17 capture.', 1;

    DECLARE @regionSha char(64) = CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_ResolveRegion')))), 2);
    DECLARE @branchSha char(64) = CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_ResolveBranch')))), 2);
    DECLARE @gradeSha char(64) = CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_ResolveGrade')))), 2);
    IF NULLIF(@ExpectedResolveRegion, N'') IS NULL OR @regionSha <> @ExpectedResolveRegion
        OR NULLIF(@ExpectedResolveBranch, N'') IS NULL OR @branchSha <> @ExpectedResolveBranch
        OR NULLIF(@ExpectedResolveGrade, N'') IS NULL OR @gradeSha <> @ExpectedResolveGrade
    BEGIN
        DECLARE @resolverMsg nvarchar(400) = CONCAT(
            N'ABORT: resolver preimages are not pinned. Region=', ISNULL(@regionSha, N'MISSING'),
            N' Branch=', ISNULL(@branchSha, N'MISSING'),
            N' Grade=', ISNULL(@gradeSha, N'MISSING'), N'.');
        THROW 59516, @resolverMsg, 1;
    END

    IF COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ApprovalRequestId') IS NULL
        OR COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ApproverRole') IS NULL
        OR COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ApproverLevel') IS NULL
        OR NOT EXISTS
        (
            SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
              AND name = N'ApprovalRequestId' AND is_nullable = 1 AND max_length = 4
              AND TYPE_NAME(user_type_id) = N'int'
        )
        OR NOT EXISTS
        (
            SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
              AND name = N'ApproverRole' AND is_nullable = 1 AND max_length = 40
              AND TYPE_NAME(user_type_id) = N'nvarchar'
        )
        OR NOT EXISTS
        (
            SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
              AND name = N'ApproverLevel' AND is_nullable = 1 AND max_length = 4
              AND TYPE_NAME(user_type_id) = N'int'
        )
        THROW 59509, N'ABORT: WP-01 audit columns are not present in the approved shape.', 1;

    DECLARE @live nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_UpdateActivityKM_WithHistory'));
    DECLARE @liveSha char(64) = CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @live)), 2);
    DECLARE @liveCanon nvarchar(max) = REPLACE(@live, N'ALTER   PROCEDURE', N'CREATE   PROCEDURE');
    DECLARE @liveCanonSha char(64) = CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @liveCanon)), 2);
    DECLARE @savedPre char(64), @savedSub char(64), @savedApp char(64);
    EXEC sys.sp_executesql
        N'SELECT @pre = PreimageSha256, @sub = SubmittedSha256, @app = AppliedSha256
          FROM dbo.TaDaLiveGoLive_20261117_Preimage WHERE WorkPackage = N''WP02'';',
        N'@pre char(64) OUTPUT, @sub char(64) OUTPUT, @app char(64) OUTPUT',
        @pre = @savedPre OUTPUT, @sub = @savedSub OUTPUT, @app = @savedApp OUTPUT;

    IF @liveCanonSha = @CandidateSha
       AND @liveCanon = @Submitted
       AND @savedPre = @LivePreimageSha
       AND @savedSub = @CandidateSha
       AND @savedApp = @CandidateSha
    BEGIN
        PRINT N'WP-02 already matches the verified candidate. No change was made.';
        RETURN;
    END
    IF @liveCanonSha = @CandidateSha
        THROW 59512, N'ABORT: KM module is the candidate but the evidence row does not prove this package deployed it.', 1;
    IF @live IS NULL OR @liveSha <> @LivePreimageSha OR LEN(@live) <> 15455 OR DATALENGTH(@live) <> 30910
        THROW 59508, N'ABORT: LIVE KM preimage is not EC6A97B9 length 15455.', 1;

    BEGIN TRANSACTION;

    DECLARE @save nvarchar(max) = N'
IF EXISTS (SELECT 1 FROM dbo.TaDaLiveGoLive_20261117_Preimage WHERE WorkPackage = ''WP03'')
    THROW 59517, N''ABORT: a WP-03 evidence row is present. KM was not changed.'', 1;
IF EXISTS (SELECT 1 FROM dbo.TaDaLiveGoLive_20261117_Preimage WHERE WorkPackage = ''WP02'')
    THROW 59512, N''ABORT: WP-02 preimage row already exists.'', 1;
INSERT INTO dbo.TaDaLiveGoLive_20261117_Preimage
    (WorkPackage, ObjectName, PreimageSha256, PreimageLen, PreimageDataLength, ModuleDefinition, SubmittedSha256)
VALUES
    (''WP02'', N''usp_UpdateActivityKM_WithHistory'', @preSha, @preLen, @preBytes, @pre, @submittedSha);';
    EXEC sys.sp_executesql
          @save
        , N'@pre nvarchar(max), @preSha char(64), @preLen int, @preBytes int, @submittedSha char(64)'
        , @pre = @live
        , @preSha = @liveSha
        , @preLen = LEN(@live)
        , @preBytes = DATALENGTH(@live)
        , @submittedSha = @CandidateSha;

    DECLARE @ToApply nvarchar(max) = REPLACE(@Submitted, N'CREATE   PROCEDURE', N'ALTER   PROCEDURE');
    IF CHARINDEX(N'CREATE   PROCEDURE', @ToApply) > 0
        OR (LEN(@ToApply) - LEN(REPLACE(@ToApply, N'ALTER   PROCEDURE', N''))) / LEN(N'ALTER   PROCEDURE') <> 1
        THROW 59511, N'ABORT: apply batch is not the single ALTER   PROCEDURE form. Rolled back.', 1;

    EXEC sys.sp_executesql @ToApply;

    DECLARE @stored nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_UpdateActivityKM_WithHistory'));
    DECLARE @storedCanon nvarchar(max) = REPLACE(@stored, N'ALTER   PROCEDURE', N'CREATE   PROCEDURE');
    IF @storedCanon <> @Submitted
        OR CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @storedCanon)), 2) <> @CandidateSha
        OR (LEN(@storedCanon) - LEN(REPLACE(@storedCanon, N'CREATE   PROCEDURE', N''))) / LEN(N'CREATE   PROCEDURE') <> 1
        THROW 59511, N'ABORT: stored KM module does not equal the verified F6B07BE8 text. Rolled back.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @pinned AS p
        CROSS APPLY (SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + p.ObjectName)) AS definition) AS d
        WHERE d.definition IS NULL
           OR LEN(d.definition) <> p.ExpectedLen
           OR DATALENGTH(d.definition) <> p.ExpectedBytes
           OR CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), d.definition)), 2) <> p.ExpectedSha
    )
        THROW 59511, N'ABORT: an excluded module changed. Rolled back.', 1;

    IF CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_ResolveRegion')))), 2) <> @ExpectedResolveRegion
        OR CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_ResolveBranch')))), 2) <> @ExpectedResolveBranch
        OR CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_ResolveGrade')))), 2) <> @ExpectedResolveGrade
        THROW 59511, N'ABORT: a resolver fingerprint moved. Rolled back.', 1;

    EXEC sys.sp_executesql
        N'UPDATE dbo.TaDaLiveGoLive_20261117_Preimage SET AppliedSha256 = @applied WHERE WorkPackage = N''WP02'';',
        N'@applied char(64)',
        @applied = @CandidateSha;

    COMMIT TRANSACTION;
    PRINT N'WP-02 committed. usp_RecalculateSalesClaim and usp_RecalculateApprovalRequest were not modified.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH
