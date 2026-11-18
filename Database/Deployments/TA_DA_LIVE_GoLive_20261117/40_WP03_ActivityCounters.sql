/*
    Package:  TA/DA LIVE Go-Live 20261117
    File:     40_WP03_ActivityCounters.sql
    Status:   EXCLUDED — DO NOT EXECUTE
    Owner scope decision A (2026-11-18) removes WP-03 from the executable path.
    This file is a parked historical candidate. It is not a signature on C-1,
    C-3, or DR-COUNTER. Those decision fields stay blank.
    The THROW below ends the batch before any later statement runs.
    dbo.usp_RecalculateApprovalRequest is not changed.

    2026-11-18: Dev and LIVE differ by identifier quotes only. The stripped module
    text matches. Replacement is not recommended. This file stays excluded.
*/
THROW 59520, N'EXCLUDED — DO NOT EXECUTE. WP-03 is parked. dbo.usp_RecalculateApprovalRequest is not changed.', 1;

/*
    Historical candidate retained below the abort. It is not an execution step.
    Source:   commit c9eba19, the Approval file only.
              Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql
              from CREATE PROCEDURE through the end of that file.
    The Utilities file is not embedded. Gate literals below remain UNSIGNED.
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
DECLARE @GateC1 nvarchar(40) = N'UNSIGNED';
DECLARE @GateC3 nvarchar(40) = N'UNSIGNED';
DECLARE @GateCounter nvarchar(40) = N'UNSIGNED';

-- MODULE-LITERAL-START
DECLARE @Submitted nvarchar(max) = N'CREATE PROCEDURE dbo.usp_RecalculateApprovalRequest
(
    @ApprovalRequestId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @ApprovalRequestId IS NULL
        THROW 59121, ''ApprovalRequestId cannot be NULL.'', 1;

    DECLARE @HeaderStatus NVARCHAR(50);

    SELECT @HeaderStatus = Status
    FROM dbo.SalesApprovalRequest
    WHERE ApprovalRequestId = @ApprovalRequestId;

    IF @HeaderStatus IS NULL
        THROW 59122, ''Approval Request not found.'', 1;

    /* Payment lock. Mark Ready calls this procedure before it writes ReadyForPayment. */
    IF @HeaderStatus = N''ReadyForPayment''
        RETURN;

    /* Latest detail per activity: MAX(DetailVersion) then MAX(ApprovalRequestDetailId) — RC-012 */
    ;WITH Latest AS
    (
        SELECT
              d2.ActivityId
            , MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
        FROM dbo.SalesApprovalRequestDetail d2
        INNER JOIN (
            SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
            FROM dbo.SalesApprovalRequestDetail
            WHERE ApprovalRequestId = @ApprovalRequestId
            GROUP BY ActivityId
        ) lv
            ON lv.ActivityId = d2.ActivityId
           AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
        WHERE d2.ApprovalRequestId = @ApprovalRequestId
        GROUP BY d2.ActivityId
    )
    UPDATE d
    SET
        Snapshot_DAAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = ''DA''
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''
        ), 0),
        Snapshot_HotelAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = ''Hotel''
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''
        ), 0),
        Snapshot_TravelAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = ''Travel''
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''
        ), 0),
        Snapshot_StationaryAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = ''Stationary''
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''
        ), 0),
        ModifiedOn = SYSUTCDATETIME()
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN Latest l
        ON d.ApprovalRequestDetailId = l.LatestDetailId
    WHERE d.ApprovalRequestId = @ApprovalRequestId;

    ;WITH Latest AS
    (
        SELECT
              d2.ActivityId
            , MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
        FROM dbo.SalesApprovalRequestDetail d2
        INNER JOIN (
            SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
            FROM dbo.SalesApprovalRequestDetail
            WHERE ApprovalRequestId = @ApprovalRequestId
            GROUP BY ActivityId
        ) lv
            ON lv.ActivityId = d2.ActivityId
           AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
        WHERE d2.ApprovalRequestId = @ApprovalRequestId
        GROUP BY d2.ActivityId
    )
    UPDATE d
    SET Snapshot_ExpenseTotal =
          ISNULL(d.Snapshot_DAAmount, 0)
        + ISNULL(d.Snapshot_HotelAmount, 0)
        + ISNULL(d.Snapshot_TravelAmount, 0)
        + ISNULL(d.Snapshot_StationaryAmount, 0)
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN Latest l
        ON d.ApprovalRequestDetailId = l.LatestDetailId
    WHERE d.ApprovalRequestId = @ApprovalRequestId;

    DECLARE
          @ApprovedKM              DECIMAL(18,2) = 0
        , @ApprovedClaimAmount     DECIMAL(18,2) = 0
        , @ApprovedDAAmount        DECIMAL(18,2) = 0
        , @ApprovedHotelAmount     DECIMAL(18,2) = 0
        , @ApprovedTravelAmount    DECIMAL(18,2) = 0
        , @FinalApprovedAmount     DECIMAL(18,2) = 0
        , @TotalActivities         INT = 0
        , @PendingActivities       INT = 0
        , @ApprovedActivities      INT = 0
        , @RejectedActivities      INT = 0;

    ;WITH Latest AS
    (
        SELECT
              d2.ActivityId
            , MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
        FROM dbo.SalesApprovalRequestDetail d2
        INNER JOIN (
            SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
            FROM dbo.SalesApprovalRequestDetail
            WHERE ApprovalRequestId = @ApprovalRequestId
            GROUP BY ActivityId
        ) lv
            ON lv.ActivityId = d2.ActivityId
           AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
        WHERE d2.ApprovalRequestId = @ApprovalRequestId
        GROUP BY d2.ActivityId
    )
    SELECT
          @ApprovedKM           = ISNULL(SUM(d.Snapshot_TotalDistanceKM), 0)
        , @ApprovedClaimAmount  = ISNULL(SUM(d.Snapshot_CalculatedClaimAmount), 0)
        , @ApprovedDAAmount     = ISNULL(SUM(d.Snapshot_DAAmount), 0)
        , @ApprovedHotelAmount  = ISNULL(SUM(d.Snapshot_HotelAmount), 0)
        , @ApprovedTravelAmount = ISNULL(SUM(d.Snapshot_TravelAmount), 0)
        , @FinalApprovedAmount  = ISNULL(SUM(
              ISNULL(d.Snapshot_CalculatedClaimAmount, 0)
            + ISNULL(d.Snapshot_ExpenseTotal, 0)
          ), 0)
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN Latest l
        ON d.ApprovalRequestDetailId = l.LatestDetailId
    WHERE d.ApprovalRequestId = @ApprovalRequestId
      AND ISNULL(d.DetailStatus, '''') NOT IN (''Rejected'', ''Returned'');

    ;WITH LatestDetail AS
    (
        SELECT
              d.DetailStatus
            , ROW_NUMBER() OVER (
                  PARTITION BY d.ActivityId
                  ORDER BY ISNULL(d.DetailVersion, 1) DESC, d.ApprovalRequestDetailId DESC
              ) AS RN
        FROM dbo.SalesApprovalRequestDetail d
        WHERE d.ApprovalRequestId = @ApprovalRequestId
    )
    SELECT
          @TotalActivities      = COUNT(*)
        , @PendingActivities    = ISNULL(SUM(CASE WHEN DetailStatus = ''Pending''  THEN 1 ELSE 0 END), 0)
        , @ApprovedActivities   = ISNULL(SUM(CASE WHEN DetailStatus = ''Approved'' THEN 1 ELSE 0 END), 0)
        , @RejectedActivities   = ISNULL(SUM(CASE WHEN DetailStatus = ''Rejected'' THEN 1 ELSE 0 END), 0)
    FROM LatestDetail
    WHERE RN = 1;

    /* ApprovedExpenseAmount / TotalApprovedAmount are computed from components (no Stationary column yet).
       FinalApprovedAmount is the FSOT Net Payable (Claim + four-part ExpenseTotal). */
    UPDATE dbo.SalesApprovalRequest
    SET
          ApprovedKM           = @ApprovedKM
        , ApprovedClaimAmount  = @ApprovedClaimAmount
        , ApprovedDAAmount     = @ApprovedDAAmount
        , ApprovedHotelAmount  = @ApprovedHotelAmount
        , ApprovedTravelAmount = @ApprovedTravelAmount
        , FinalApprovedAmount  = @FinalApprovedAmount
        , TotalActivities      = @TotalActivities
        , PendingActivities    = @PendingActivities
        , ApprovedActivities   = @ApprovedActivities
        , RejectedActivities   = @RejectedActivities
        , ModifiedOn           = SYSUTCDATETIME()
    WHERE ApprovalRequestId = @ApprovalRequestId;
END
';
-- MODULE-LITERAL-END

BEGIN TRY
    IF @Submitted NOT LIKE N'CREATE PROCEDURE dbo.usp_RecalculateApprovalRequest%'
        OR CHARINDEX(N'BEGIN TRAN', @Submitted) > 0
        OR CHARINDEX(N'ReturnedActivities', @Submitted) > 0
        OR CHARINDEX(N'TotalDeductionAmount', @Submitted) > 0
        OR CHARINDEX(N'ReadyForPayment', @Submitted) = 0
        OR CHARINDEX(N'NOT IN (''Rejected'', ''Returned'')', @Submitted) = 0
        THROW 59511, N'ABORT: submitted text is not the Approval-file candidate from c9eba19.', 1;

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

    IF @GateC1 NOT IN (N'DR-C1-A', N'DR-C1-B', N'DR-C1-C')
        THROW 59504, N'ABORT: C-1 is unsigned. This script does not choose a workflow pair.', 1;
    IF @GateC3 <> N'DR-C3-A'
        THROW 59504, N'ABORT: C-3 choice A is not signed. Choices B and C do not deploy this file.', 1;
    IF @GateCounter <> N'DR-COUNTER-A'
        THROW 59504, N'ABORT: c9eba19 is not accepted in this revision.', 1;
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
        OR HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'CONTROL') = 1
        OR HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'ALTER ANY PROCEDURE') = 1
        OR HAS_PERMS_BY_NAME(N'dbo', N'SCHEMA', N'ALTER') = 1
        THROW 59505, N'ABORT: deployment identity is unsigned or wider than this package.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM msdb.dbo.backupset AS b
        WHERE b.database_name = N'AnmolDristi' AND b.type = 'D' AND b.is_damaged = 0
          AND b.has_backup_checksums = 1 AND b.backup_finish_date = @BackupFinish
    )
        THROW 59506, N'ABORT: no undamaged checksum full backup matches the signed finish time.', 1;

    IF HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateApprovalRequest', N'OBJECT', N'CONTROL') <> 1
        OR HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateApprovalRequest', N'OBJECT', N'VIEW DEFINITION') <> 1
        OR HAS_PERMS_BY_NAME(N'dbo', N'SCHEMA', N'CREATE TABLE') <> 1
        OR HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateSalesClaim', N'OBJECT', N'ALTER') = 1
        OR HAS_PERMS_BY_NAME(N'dbo.usp_ProcessApprovalAction', N'OBJECT', N'ALTER') = 1
        OR HAS_PERMS_BY_NAME(N'dbo.usp_MarkRequestReadyForPayment', N'OBJECT', N'ALTER') = 1
        OR HAS_PERMS_BY_NAME(N'dbo.usp_ForwardApprovalRequest', N'OBJECT', N'ALTER') = 1
        THROW 59507, N'ABORT: object rights do not match the manifest.', 1;

    DECLARE @pinned TABLE (ObjectName sysname NOT NULL PRIMARY KEY, ExpectedLen int NOT NULL, ExpectedBytes int NOT NULL, ExpectedSha char(64) NOT NULL);
    INSERT INTO @pinned VALUES
     (N'usp_RecalculateApprovalRequest',     7567, 15134, '08F34AF8C5CC188F2A0877985FB7FC5E70DEB0067034B95264AE6EF4739ED5D7'),
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
        THROW 59508, N'ABORT: a pinned module fingerprint does not match the 2026-11-17 capture.', 1;

    IF EXISTS
    (
        SELECT 1 FROM sys.sql_expression_dependencies
        WHERE referenced_id = OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest')
          AND is_schema_bound_reference = 1
    )
        THROW 59512, N'ABORT: a schema-bound reference points at the recalc procedure.', 1;

    IF LEFT(@Submitted, 17) <> N'CREATE PROCEDURE '
        THROW 59511, N'ABORT: submitted recalc text does not start with CREATE PROCEDURE.', 1;
    DECLARE @ToApply nvarchar(max) = STUFF(@Submitted, 1, 6, N'ALTER');
    IF SUBSTRING(@ToApply, 6, LEN(@ToApply)) <> SUBSTRING(@Submitted, 7, LEN(@Submitted))
        THROW 59511, N'ABORT: the ALTER batch is not the source text with the first word replaced.', 1;

    DECLARE @pre nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest'));
    DECLARE @preSha char(64) = CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @pre)), 2);
    DECLARE @submittedSha char(64) = CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @ToApply)), 2);
    DECLARE @kmBefore char(64) = CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_UpdateActivityKM_WithHistory')))), 2);

    BEGIN TRANSACTION;

    DECLARE @save nvarchar(max) = N'
IF OBJECT_ID(N''dbo.TaDaLiveGoLive_20261117_Preimage'', N''U'') IS NULL
    CREATE TABLE dbo.TaDaLiveGoLive_20261117_Preimage
    (
        WorkPackage varchar(8) NOT NULL CONSTRAINT PK_TaDaLiveGoLive_20261117_Preimage PRIMARY KEY,
        ObjectName sysname NOT NULL,
        PreimageSha256 char(64) NULL,
        PreimageLen int NULL,
        PreimageDataLength int NULL,
        ModuleDefinition nvarchar(max) NULL,
        SubmittedSha256 char(64) NULL,
        AppliedSha256 char(64) NULL,
        HistoryRowCount bigint NULL,
        HistoryIdMin int NULL,
        HistoryIdSum bigint NULL,
        SchemaSnapshot nvarchar(max) NULL,
        CapturedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_TaDaLiveGoLive_20261117_Captured DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_TaDaLiveGoLive_20261117_WorkPackage CHECK (WorkPackage IN (''WP01'', ''WP02'', ''WP03''))
    );
IF EXISTS (SELECT 1 FROM dbo.TaDaLiveGoLive_20261117_Preimage WHERE WorkPackage = ''WP03'')
    THROW 59512, N''ABORT: WP-03 preimage row already exists.'', 1;
INSERT INTO dbo.TaDaLiveGoLive_20261117_Preimage
    (WorkPackage, ObjectName, PreimageSha256, PreimageLen, PreimageDataLength, ModuleDefinition, SubmittedSha256)
VALUES
    (''WP03'', N''usp_RecalculateApprovalRequest'', @preSha, @preLen, @preBytes, @pre, @submittedSha);';
    EXEC sys.sp_executesql
          @save
        , N'@pre nvarchar(max), @preSha char(64), @preLen int, @preBytes int, @submittedSha char(64)'
        , @pre = @pre, @preSha = @preSha, @preLen = LEN(@pre), @preBytes = DATALENGTH(@pre), @submittedSha = @submittedSha;

    EXEC sys.sp_executesql @ToApply;

    IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest')) <> @ToApply
        THROW 59511, N'ABORT: stored recalc module does not equal the ALTER batch from c9eba19. Rolled back.', 1;

    DECLARE @applied char(64) = CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest')))), 2);
    IF @applied <> @submittedSha
        THROW 59511, N'ABORT: stored recalc hash does not equal the submitted text. Rolled back.', 1;
    IF CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_UpdateActivityKM_WithHistory')))), 2) <> @kmBefore
        THROW 59511, N'ABORT: the KM module changed. Rolled back.', 1;
    IF EXISTS
    (
        SELECT 1
        FROM @pinned AS p
        CROSS APPLY (SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + p.ObjectName)) AS definition) AS d
        WHERE p.ObjectName <> N'usp_RecalculateApprovalRequest'
          AND (d.definition IS NULL
            OR CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), d.definition)), 2) <> p.ExpectedSha)
    )
        THROW 59511, N'ABORT: an excluded module changed. Rolled back.', 1;

    EXEC sys.sp_executesql
        N'UPDATE dbo.TaDaLiveGoLive_20261117_Preimage SET AppliedSha256 = @applied WHERE WorkPackage = ''WP03'';',
        N'@applied char(64)',
        @applied = @applied;

    COMMIT TRANSACTION;
    PRINT N'WP-03 committed. Workflow procedures were not modified.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH
