/*
    RC-014B behavior probe.
    Authoring status: NOT EXECUTED. This file is not UAT evidence.
    Review this header before any execution. G4-G7 are not authorized.
    This edit does not run the script and does not change
    01_definition_shape_readonly.sql, the definition transcripts, or
    dbo.usp_ProcessApprovalAction.

    Target, when a human later runs it:
      server  MUMANMOAPP01\SQLEXPRESS
      database AnmolDristiDev

    Transaction review of dbo.usp_ProcessApprovalAction (source, not a database run):
      Line 83  SET XACT_ABORT ON
               Runtime SET options are restored when a procedure returns.
               This script still records @@OPTIONS bit 16384 before and after.
      Line 243 BEGIN TRANSACTION
               Unnamed. Increments @@TRANCOUNT. No savepoint.
      Line 711 COMMIT TRANSACTION
               Unnamed. Decrements @@TRANCOUNT by 1.
               While an outer transaction is still open, this does not make the
               procedure's writes durable.
      Lines 714-715, inside CATCH:
               IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
               An unnamed ROLLBACK rolls back the outermost transaction and
               sets @@TRANCOUNT to 0. On that path this script's outer
               transaction is already gone. A second ROLLBACK would fail.
      Validation RAISERROR/RETURN paths sit before BEGIN TRY, so those
      rejections do not open the procedure's transaction and do not roll
      back this script's outer transaction.

    Approver role (procedure lines 177-182):
      SELECT TOP (1) ApproverRole
      WHERE ApprovalRequestId = @ApprovalRequestId
        AND ApproverLevel = @CurrentApproverLevel
      ORDER BY ApproverLevel.
      Every qualifying row ties on ApproverLevel, so TOP (1) is not a
      tie-break. This script does not use TOP (1) ORDER BY
      ApprovalRequestApproverId. @ExpectError uses the role only when every
      row at the current level has the same ApproverRole. More than one
      distinct role blocks the call.

    Auditor status:
      A bare SELECT @Status = Status WHERE ApproverRole = N'Auditor' is
      undefined when more than one row matches. Extracted index
      UX_SAR_Auditor_Once is not assumed to exist on the target. More than
      one Auditor row blocks the call.

    Activity and expense writes (procedure lines 267-295):
      SalesDailyActivity is updated by ActivityId only (ClaimStatus,
      ModifiedByUserId, ModifiedDateTime), including a deleted activity.
      SalesDailyExpense is updated by ActivityId for IsDeleted = 0 rows
      whose ClaimStatus is still eligible for that action (ClaimStatus,
      ClaimProcessedBy, ClaimProcessedDate, ModifiedBy, ModifiedOn, Remarks).
      The extracted detail schema does not forbid the same ActivityId on
      another ApprovalRequestId.

    Rollback proof:
      ROLLBACK PROVED is reported only when this script opened the only
      outer transaction, the procedure returned without error, an accepted
      write was observed on the acted detail, the activity, the header
      audit columns, and every eligible expense, at least one expense row
      was eligible, @@TRANCOUNT is still 1, XACT_STATE() is 1, this script
      then rolls back that outer transaction, and the captured request,
      detail, approver, history, activity, and expense rows match the
      pre-image. Comparisons are row EXCEPT. CHECKSUM_AGG is not used.
      A rejection is not rollback proof. Matching rows after a rejection
      only show that no captured write happened. Zero eligible expenses
      leaves the run INCONCLUSIVE even if the activity write was restored.

    Transaction anomalies:
      If the procedure returns with @@TRANCOUNT = 0, or with any state other
      than one committable outer transaction, the run is UNSAFE and
      inconclusive. This script never COMMITs. It does not try to reverse
      durable writes. A later match against the pre-image is a damage
      assessment, not proof.
      If this script's outer transaction is still open, cleanup rolls it
      back so the session is not left inside it. Cleanup is not, by itself,
      rollback proof.

    Mandatory pre-run checks. Source review did not query UAT and does not
    treat any of the following as absent or already satisfied:
      - DML triggers on SalesApprovalRequest, SalesApprovalRequestDetail,
        SalesApprovalRequestApprover, ApprovalRequestStatusHistory,
        SalesDailyActivity, and SalesDailyExpense. Disabled triggers block
        too. The script queries these only when a human executes it.
      - The selected ActivityId referenced by any other ApprovalRequestId.
      - SalesDailyActivity.ApprovalRequestId equal to the supplied request.
        NULL does not establish ownership.
      - Exactly one Auditor approver row, or zero. More than one blocks.
      - One distinct ApproverRole at the header's current level.
      - Database DDL triggers and server triggers. This script does not
        query them.
      - No other session writing the captured rows during the probe. This
        script does not take an application lock.
      - Columns not listed in the captured images, identity counters, and
        effects outside these six tables are outside the proof.
      - A fixture with no eligible expense row cannot show an expense UPDATE
        being rolled back. The script then reports INCONCLUSIVE, not
        ROLLBACK PROVED. Expense rows are still compared.
      - no C# override
      - Employee cannot skip L1
      - universal L1 cannot skip Auditor
        (this script only checks the request it was given)

    The non-Auditor Completed write (procedure lines 468-498) is accepted
    39a12b0 baseline. A fixture that reaches it is reported as accepted
    baseline, not as a failure.

    Result vocabulary (NO FAIL ROWS is not a result):
      NOT RUN | BLOCKED | REJECTED BEFORE WRITE | ROLLBACK PROVED |
      UNSAFE | INCONCLUSIVE | FAIL

    Default @ReviewedBeforeExecution is 0, so executing the file does not
    call the procedure until a reviewer sets it to 1 and supplies a real
    AnmolDristiDev request.
*/
SET NOCOUNT ON;
SET XACT_ABORT OFF;

IF (@@OPTIONS & 2) = 2
BEGIN
    PRINT N'RC014B behavior | RESULT | NOT RUN | IMPLICIT_TRANSACTIONS is ON.';
    PRINT N'PREREQ | not cleared for UAT: table triggers, shared ActivityId, Auditor cardinality, current-level role agreement, DDL/server triggers, and concurrent writers are unresolved in source review';
    PRINT N'UNVERIFIED | no C# override | SQL cannot see a later C# write';
    PRINT N'UNVERIFIED | Employee cannot skip L1 | this procedure does not submit a claim';
    PRINT N'UNVERIFIED | universal L1 cannot skip Auditor | one request cannot prove every request has an Auditor';
    RETURN;
END;

USE [AnmolDristiDev];

DECLARE @ReviewedBeforeExecution bit = 0;
DECLARE @ApprovalRequestId int = NULL;
DECLARE @ApprovalRequestDetailId int = NULL;
DECLARE @ActionBy nvarchar(100) = NULL;
DECLARE @Action nvarchar(20) = N'APPROVE';
DECLARE @Remarks nvarchar(max) = N'RC014B tran rollback probe — do not keep';

DECLARE @Server nvarchar(256) = CONVERT(nvarchar(256), SERVERPROPERTY(N'ServerName'));
DECLARE @Fail int = 0;
DECLARE @Outcome nvarchar(40) = N'NOT RUN';
DECLARE @Configured bit = 0;
DECLARE @TranStarted bit = 0;
DECLARE @DidExec bit = 0;
DECLARE @ProcError bit = 0;
DECLARE @ErrMsg nvarchar(4000) = NULL;
DECLARE @ErrNum int = NULL;
DECLARE @ScriptError nvarchar(4000) = NULL;
DECLARE @TranCountAfter int = NULL;
DECLARE @XactStateAfter smallint = NULL;
DECLARE @XactAbortBefore bit = CASE WHEN (@@OPTIONS & 16384) = 16384 THEN 1 ELSE 0 END;
DECLARE @ExpectError bit = 0;
DECLARE @NormalizedAction nvarchar(20) = NULL;
DECLARE @CanRead bit = 0;
DECLARE @UnexpectedCommit bit = 0;
DECLARE @OuterClearedByProc bit = 0;
DECLARE @RollbackIssued bit = 0;
DECLARE @Unsafe bit = 0;
DECLARE @ImagesCaptured bit = 0;
DECLARE @AcceptedWriteObserved bit = 0;
DECLARE @InTranUnchanged bit = 0;
DECLARE @LiveMatch bit = 0;
DECLARE @RollbackProved bit = 0;
DECLARE @RejectedBeforeWrite bit = 0;
DECLARE @BlockReason nvarchar(4000) = NULL;
DECLARE @WriteMiss int = 0;
DECLARE @LiveDiff int = 0;
DECLARE @Compared bit = 0;

DECLARE @PreStatus nvarchar(50) = NULL;
DECLARE @PreLogin nvarchar(100) = NULL;
DECLARE @PreLevel int = NULL;
DECLARE @PreModifiedOn datetime2(7) = NULL;
DECLARE @PreModifiedBy nvarchar(100) = NULL;
DECLARE @PreRole nvarchar(100) = NULL;
DECLARE @PreAuditorStatus nvarchar(50) = NULL;
DECLARE @PreHigher bit = 0;
DECLARE @PreCompletedHist int = 0;
DECLARE @PreHistCount int = 0;
DECLARE @PreClaim decimal(18, 2) = NULL;
DECLARE @PreDa decimal(18, 2) = NULL;
DECLARE @PreHotel decimal(18, 2) = NULL;
DECLARE @PreTravel decimal(18, 2) = NULL;
DECLARE @PreKm decimal(18, 2) = NULL;
DECLARE @DetailRequestId int = NULL;
DECLARE @ActivityId int = NULL;
DECLARE @DetailFound bit = 0;
DECLARE @HeaderFound bit = 0;
DECLARE @PreDetailStatus nvarchar(50) = NULL;
DECLARE @PreDetailRemarks nvarchar(1000) = NULL;
DECLARE @PreDetailLevel int = NULL;
DECLARE @PreDetailApprovedBy nvarchar(100) = NULL;
DECLARE @PreDetailApprovedOn datetime2(7) = NULL;
DECLARE @LatestDetailId int = NULL;
DECLARE @PreLatestStatus nvarchar(50) = NULL;
DECLARE @AuditorCount int = 0;
DECLARE @LevelRowCount int = 0;
DECLARE @LevelRoleCount int = 0;
DECLARE @ActivityRowCount int = 0;
DECLARE @ActivityOwnerRequestId int = NULL;
DECLARE @SharedRequestCount int = 0;
DECLARE @TriggerCount int = 0;
DECLARE @TriggerList nvarchar(4000) = NULL;
DECLARE @ExpenseCount int = 0;
DECLARE @ExpenseEligibleCount int = 0;

DECLARE @PostStatus nvarchar(50) = NULL;
DECLARE @PostLogin nvarchar(100) = NULL;
DECLARE @PostLevel int = NULL;
DECLARE @PostModifiedOn datetime2(7) = NULL;
DECLARE @PostModifiedBy nvarchar(100) = NULL;
DECLARE @PostAuditorStatus nvarchar(50) = NULL;
DECLARE @PostAuditorCount int = 0;
DECLARE @PostCompletedHist int = NULL;
DECLARE @PostHistCount int = NULL;
DECLARE @PostDetailStatus nvarchar(50) = NULL;
DECLARE @PostLatestStatus nvarchar(50) = NULL;
DECLARE @PostPending int = NULL;
DECLARE @PostReturned int = NULL;
DECLARE @PostClaim decimal(18, 2) = NULL;
DECLARE @PostDa decimal(18, 2) = NULL;
DECLARE @PostHotel decimal(18, 2) = NULL;
DECLARE @PostTravel decimal(18, 2) = NULL;
DECLARE @PostKm decimal(18, 2) = NULL;
DECLARE @ExpClaim decimal(18, 2) = NULL;
DECLARE @ExpDa decimal(18, 2) = NULL;
DECLARE @ExpHotel decimal(18, 2) = NULL;
DECLARE @ExpTravel decimal(18, 2) = NULL;
DECLARE @ExpKm decimal(18, 2) = NULL;
DECLARE @Mapped nvarchar(50) = NULL;

DECLARE @LiveStatus nvarchar(50) = NULL;
DECLARE @LiveLogin nvarchar(100) = NULL;
DECLARE @LiveLevel int = NULL;
DECLARE @LiveModifiedOn datetime2(7) = NULL;
DECLARE @LiveModifiedBy nvarchar(100) = NULL;
DECLARE @LiveCompletedHist int = NULL;
DECLARE @LiveHistCount int = NULL;
DECLARE @LiveClaim decimal(18, 2) = NULL;
DECLARE @LiveDa decimal(18, 2) = NULL;
DECLARE @LiveHotel decimal(18, 2) = NULL;
DECLARE @LiveTravel decimal(18, 2) = NULL;
DECLARE @LiveKm decimal(18, 2) = NULL;
DECLARE @LiveDetailStatus nvarchar(50) = NULL;
DECLARE @LiveDetailRemarks nvarchar(1000) = NULL;
DECLARE @LiveDetailLevel int = NULL;
DECLARE @LiveDetailApprovedBy nvarchar(100) = NULL;
DECLARE @LiveDetailApprovedOn datetime2(7) = NULL;
DECLARE @LiveLatestStatus nvarchar(50) = NULL;

DECLARE @PreDetail TABLE (
      ApprovalRequestDetailId int NOT NULL
    , ApprovalRequestId int NOT NULL
    , ActivityId int NOT NULL
    , DetailStatus nvarchar(50) NULL
    , DetailRemarks nvarchar(1000) NULL
    , DetailApprovedBy nvarchar(100) NULL
    , DetailApprovedOn datetime2(7) NULL
    , DetailApprovedLevel int NULL
    , DetailVersion int NOT NULL
    , PreviousDetailId int NULL
    , Snapshot_TotalDistanceKM decimal(10, 2) NULL
    , Snapshot_CalculatedClaimAmount decimal(18, 2) NULL
    , Snapshot_DAAmount decimal(18, 2) NULL
    , Snapshot_HotelAmount decimal(18, 2) NULL
    , Snapshot_TravelAmount decimal(18, 2) NULL
    , PRIMARY KEY (ApprovalRequestDetailId)
);

DECLARE @PreApprover TABLE (
      ApprovalRequestApproverId int NOT NULL
    , ApproverLoginId nvarchar(100) NULL
    , ApproverRole nvarchar(100) NULL
    , ApproverLevel int NULL
    , IsRequired bit NOT NULL
    , Status nvarchar(50) NOT NULL
    , ActionBy nvarchar(100) NULL
    , ActionDate datetime2(7) NULL
    , Remarks nvarchar(1000) NULL
    , PRIMARY KEY (ApprovalRequestApproverId)
);

DECLARE @PreHistory TABLE (
      HistoryId int NOT NULL
    , OldStatus nvarchar(50) NULL
    , NewStatus nvarchar(50) NOT NULL
    , ChangedBy nvarchar(100) NULL
    , ChangedOn datetime2(7) NOT NULL
    , Remarks nvarchar(1000) NULL
    , ApprovalRequestDetailId int NULL
    , ActivityId int NULL
    , ApproverLevel int NULL
    , ActionType nvarchar(30) NULL
    , DetailVersion int NULL
    , PRIMARY KEY (HistoryId)
);

DECLARE @PreActivity TABLE (
      ActivityId int NOT NULL
    , ClaimStatus nvarchar(50) NULL
    , User_Id nvarchar(100) NOT NULL
    , Login_Id nvarchar(100) NULL
    , ApprovalRequestId int NULL
    , IsDeleted bit NOT NULL
    , ModifiedByUserId nvarchar(100) NULL
    , ModifiedDateTime datetime NULL
    , ClaimProcessedBy nvarchar(100) NULL
    , ClaimProcessedDate datetime NULL
    , SubmittedBy nvarchar(100) NULL
    , SubmittedOn datetime2(7) NULL
    , RowVersion varbinary(8) NOT NULL
    , PRIMARY KEY (ActivityId)
);

DECLARE @PreExpense TABLE (
      ExpenseId int NOT NULL
    , ActivityId int NOT NULL
    , ClaimStatus nvarchar(50) NULL
    , IsDeleted bit NOT NULL
    , ClaimProcessedBy nvarchar(200) NULL
    , ClaimProcessedDate datetime NULL
    , ModifiedBy nvarchar(100) NULL
    , ModifiedOn datetime2(7) NULL
    , Remarks nvarchar(1000) NULL
    , CreatedBy nvarchar(100) NULL
    , DeletedBy nvarchar(100) NULL
    , DeletedOn datetime2(7) NULL
    , IsEligible bit NOT NULL
    , PRIMARY KEY (ExpenseId)
);

PRINT N'===== RC-014B behavior probe (not a certified result) =====';
PRINT N'Server=' + ISNULL(@Server, N'<null>') + N' Database=' + DB_NAME();
PRINT N'INFO | XACT_ABORT before=' + CONVERT(nvarchar(1), @XactAbortBefore) + N' @@TRANCOUNT=' + CONVERT(nvarchar(12), @@TRANCOUNT);

IF UPPER(ISNULL(@Server, N'')) <> N'MUMANMOAPP01\SQLEXPRESS'
   OR DB_NAME() <> N'AnmolDristiDev'
BEGIN
    PRINT N'RC014B behavior | RESULT | NOT RUN | Refusing a server or database other than MUMANMOAPP01\SQLEXPRESS / AnmolDristiDev.';
    PRINT N'PREREQ | not cleared for UAT: table triggers, shared ActivityId, Auditor cardinality, current-level role agreement, DDL/server triggers, and concurrent writers are unresolved in source review';
    PRINT N'UNVERIFIED | no C# override | SQL cannot see a later C# write';
    PRINT N'UNVERIFIED | Employee cannot skip L1 | this procedure does not submit a claim';
    PRINT N'UNVERIFIED | universal L1 cannot skip Auditor | one request cannot prove every request has an Auditor';
    RETURN;
END;

IF @@TRANCOUNT <> 0
BEGIN
    PRINT N'RC014B behavior | RESULT | NOT RUN | @@TRANCOUNT is already ' + CONVERT(nvarchar(12), @@TRANCOUNT) + N'. Refusing to join an existing transaction.';
    PRINT N'PREREQ | not cleared for UAT: table triggers, shared ActivityId, Auditor cardinality, current-level role agreement, DDL/server triggers, and concurrent writers are unresolved in source review';
    PRINT N'UNVERIFIED | no C# override | SQL cannot see a later C# write';
    PRINT N'UNVERIFIED | Employee cannot skip L1 | this procedure does not submit a claim';
    PRINT N'UNVERIFIED | universal L1 cannot skip Auditor | one request cannot prove every request has an Auditor';
    RETURN;
END;

IF @ReviewedBeforeExecution = 1
   AND @ApprovalRequestId IS NOT NULL
   AND @ApprovalRequestDetailId IS NOT NULL
   AND NULLIF(LTRIM(RTRIM(@ActionBy)), N'') IS NOT NULL
   AND NULLIF(LTRIM(RTRIM(@Action)), N'') IS NOT NULL
    SET @Configured = 1;

IF @Configured = 0
BEGIN
    PRINT N'RC014B behavior | RESULT | NOT RUN | Set @ReviewedBeforeExecution = 1 only after reviewing the transaction-state header, and supply @ApprovalRequestId, @ApprovalRequestDetailId, and @ActionBy.';
    PRINT N'PREREQ | not cleared for UAT: table triggers, shared ActivityId, Auditor cardinality, current-level role agreement, DDL/server triggers, and concurrent writers are unresolved in source review';
    PRINT N'UNVERIFIED | no C# override | SQL cannot see a later C# write';
    PRINT N'UNVERIFIED | Employee cannot skip L1 | this procedure does not submit a claim';
    PRINT N'UNVERIFIED | universal L1 cannot skip Auditor | one request cannot prove every request has an Auditor';
    RETURN;
END;

SET @NormalizedAction = UPPER(LTRIM(RTRIM(@Action)));
SET @Mapped = CASE @NormalizedAction
    WHEN N'APPROVE' THEN N'Approved'
    WHEN N'RETURN' THEN N'Returned'
    WHEN N'REJECT' THEN N'Rejected'
    ELSE NULL
END;

BEGIN TRY
    PRINT N'PREREQ | DDL triggers, server triggers, and concurrent writers are not queried. Do not treat them as absent.';
    BEGIN TRANSACTION RC014B_Outer;
    SET @TranStarted = 1;

    SELECT
          @DetailRequestId = d.ApprovalRequestId
        , @ActivityId = d.ActivityId
        , @PreDetailStatus = d.DetailStatus
        , @PreDetailRemarks = d.DetailRemarks
        , @PreDetailLevel = d.DetailApprovedLevel
        , @PreDetailApprovedBy = d.DetailApprovedBy
        , @PreDetailApprovedOn = d.DetailApprovedOn
        , @DetailFound = 1
    FROM dbo.SalesApprovalRequestDetail d
    WHERE d.ApprovalRequestDetailId = @ApprovalRequestDetailId;

    SELECT
          @PreStatus = r.Status
        , @PreLogin = r.CurrentApproverLoginId
        , @PreLevel = r.CurrentApproverLevel
        , @PreModifiedOn = r.ModifiedOn
        , @PreModifiedBy = r.ModifiedBy
        , @PreClaim = r.ApprovedClaimAmount
        , @PreDa = r.ApprovedDAAmount
        , @PreHotel = r.ApprovedHotelAmount
        , @PreTravel = r.ApprovedTravelAmount
        , @PreKm = r.ApprovedKM
        , @HeaderFound = 1
    FROM dbo.SalesApprovalRequest r
    WHERE r.ApprovalRequestId = @ApprovalRequestId;

    IF @DetailFound = 0 OR @HeaderFound = 0 OR @DetailRequestId IS NULL OR @DetailRequestId <> @ApprovalRequestId
        SET @BlockReason = N'detail, header, or request/detail pair was not found. Procedure was not called.';
    ELSE IF @ActivityId IS NULL
        SET @BlockReason = N'detail ActivityId is NULL. Activity and expense scope cannot be established. Procedure was not called.';
    ELSE
    BEGIN
        SELECT TOP (1)
            @LatestDetailId = d2.ApprovalRequestDetailId
        FROM dbo.SalesApprovalRequestDetail d2
        INNER JOIN (
            SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
            FROM dbo.SalesApprovalRequestDetail
            WHERE ApprovalRequestId = @ApprovalRequestId
              AND ActivityId = @ActivityId
            GROUP BY ActivityId
        ) lv
            ON lv.ActivityId = d2.ActivityId
           AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
        WHERE d2.ApprovalRequestId = @ApprovalRequestId
          AND d2.ActivityId = @ActivityId
        ORDER BY d2.ApprovalRequestDetailId DESC;

        SELECT @AuditorCount = COUNT(*)
        FROM dbo.SalesApprovalRequestApprover a
        WHERE a.ApprovalRequestId = @ApprovalRequestId
          AND a.ApproverRole = N'Auditor';

        IF @AuditorCount > 1
            SET @BlockReason = N'Auditor approver rows = ' + CONVERT(nvarchar(12), @AuditorCount)
                + N'. More than one row makes Status assignment ambiguous. Extracted index UX_SAR_Auditor_Once is not assumed on this database. Procedure was not called.';
        ELSE IF @AuditorCount = 1
        BEGIN
            SELECT @PreAuditorStatus = a.Status
            FROM dbo.SalesApprovalRequestApprover a
            WHERE a.ApprovalRequestId = @ApprovalRequestId
              AND a.ApproverRole = N'Auditor';
        END;

        IF @BlockReason IS NULL
        BEGIN
            SELECT @LevelRowCount = COUNT(*)
            FROM dbo.SalesApprovalRequestApprover a
            WHERE a.ApprovalRequestId = @ApprovalRequestId
              AND a.ApproverLevel = @PreLevel;

            SELECT @LevelRoleCount = COUNT(*)
            FROM (
                SELECT a.ApproverRole
                FROM dbo.SalesApprovalRequestApprover a
                WHERE a.ApprovalRequestId = @ApprovalRequestId
                  AND a.ApproverLevel = @PreLevel
                GROUP BY a.ApproverRole
            ) roles;

            IF @LevelRoleCount > 1
                SET @BlockReason = N'current level has ' + CONVERT(nvarchar(12), @LevelRoleCount)
                    + N' distinct ApproverRole values across ' + CONVERT(nvarchar(12), @LevelRowCount)
                    + N' rows. dbo.usp_ProcessApprovalAction uses TOP (1) ORDER BY ApproverLevel, which does not break that tie. This script will not calculate @ExpectError from TOP (1) ORDER BY ApprovalRequestApproverId.';
            ELSE IF @LevelRowCount = 0
                SET @PreRole = NULL;
            ELSE
                SELECT @PreRole = MIN(a.ApproverRole)
                FROM dbo.SalesApprovalRequestApprover a
                WHERE a.ApprovalRequestId = @ApprovalRequestId
                  AND a.ApproverLevel = @PreLevel;
        END;

        IF @BlockReason IS NULL
        BEGIN
            SELECT @ActivityRowCount = COUNT(*)
            FROM dbo.SalesDailyActivity a
            WHERE a.ActivityId = @ActivityId;

            IF @ActivityRowCount = 1
                SELECT @ActivityOwnerRequestId = a.ApprovalRequestId
                FROM dbo.SalesDailyActivity a
                WHERE a.ActivityId = @ActivityId;

            IF @ActivityRowCount <> 1
                SET @BlockReason = N'SalesDailyActivity rows for ActivityId = ' + CONVERT(nvarchar(12), @ActivityRowCount)
                    + N'. Exactly one row is required. Procedure was not called.';
            ELSE IF @ActivityOwnerRequestId IS NULL OR @ActivityOwnerRequestId <> @ApprovalRequestId
                SET @BlockReason = N'SalesDailyActivity.ApprovalRequestId is NULL or does not equal the supplied request. Exact activity ownership is not established. The procedure updates by ActivityId only. Procedure was not called.';
        END;

        IF @BlockReason IS NULL
        BEGIN
            SELECT @SharedRequestCount = COUNT(DISTINCT d.ApprovalRequestId)
            FROM dbo.SalesApprovalRequestDetail d
            WHERE d.ActivityId = @ActivityId
              AND d.ApprovalRequestId <> @ApprovalRequestId;

            IF @SharedRequestCount > 0
                SET @BlockReason = N'ActivityId is referenced by ' + CONVERT(nvarchar(12), @SharedRequestCount)
                    + N' other approval request(s). The procedure would update that shared activity and its expenses. Procedure was not called.';
        END;

        IF @BlockReason IS NULL
           AND (
                OBJECT_ID(N'dbo.SalesApprovalRequest') IS NULL
                OR OBJECT_ID(N'dbo.SalesApprovalRequestDetail') IS NULL
                OR OBJECT_ID(N'dbo.SalesApprovalRequestApprover') IS NULL
                OR OBJECT_ID(N'dbo.ApprovalRequestStatusHistory') IS NULL
                OR OBJECT_ID(N'dbo.SalesDailyActivity') IS NULL
                OR OBJECT_ID(N'dbo.SalesDailyExpense') IS NULL
           )
            SET @BlockReason = N'one of the affected tables is missing. Trigger scope and rollback scope cannot be established. Procedure was not called.';

        IF @BlockReason IS NULL
        BEGIN
            SELECT @TriggerCount = COUNT(*)
            FROM sys.triggers AS t
            WHERE t.parent_id IN (
                  OBJECT_ID(N'dbo.SalesApprovalRequest')
                , OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
                , OBJECT_ID(N'dbo.SalesApprovalRequestApprover')
                , OBJECT_ID(N'dbo.ApprovalRequestStatusHistory')
                , OBJECT_ID(N'dbo.SalesDailyActivity')
                , OBJECT_ID(N'dbo.SalesDailyExpense')
            );

            SELECT @TriggerList = STUFF((
                SELECT N'; ' + OBJECT_SCHEMA_NAME(t.parent_id) + N'.' + OBJECT_NAME(t.parent_id) + N'.' + t.name
                    + CASE WHEN t.is_disabled = 1 THEN N' (disabled)' ELSE N' (enabled)' END
                FROM sys.triggers AS t
                WHERE t.parent_id IN (
                      OBJECT_ID(N'dbo.SalesApprovalRequest')
                    , OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
                    , OBJECT_ID(N'dbo.SalesApprovalRequestApprover')
                    , OBJECT_ID(N'dbo.ApprovalRequestStatusHistory')
                    , OBJECT_ID(N'dbo.SalesDailyActivity')
                    , OBJECT_ID(N'dbo.SalesDailyExpense')
                )
                ORDER BY OBJECT_NAME(t.parent_id), t.name
                FOR XML PATH(''), TYPE
            ).value(N'.', N'nvarchar(max)'), 1, 2, N'');

            IF @TriggerCount > 0
                SET @BlockReason = N'DML triggers on affected tables = ' + CONVERT(nvarchar(12), @TriggerCount)
                    + N' [' + ISNULL(LEFT(@TriggerList, 1500), N'')
                    + N']. Disabled triggers are included. Their absence was not assumed. Procedure was not called.';
            ELSE
                PRINT N'INFO | DML triggers on the six affected tables = 0 | DDL and server triggers were not queried';
        END;

        IF @BlockReason IS NULL
        BEGIN
            PRINT N'INFO | Auditor approver rows=' + CONVERT(nvarchar(12), @AuditorCount)
                + N' | current-level approver rows=' + CONVERT(nvarchar(12), @LevelRowCount)
                + N' | distinct roles=' + CONVERT(nvarchar(12), @LevelRoleCount)
                + N' | role=' + ISNULL(@PreRole, N'<null>')
                + N' | procedure TOP (1) ORDER BY ApproverLevel agrees because the role is unique or absent';
            PRINT N'INFO | other requests sharing ActivityId=' + CONVERT(nvarchar(12), @SharedRequestCount);

            SELECT @PreLatestStatus = d.DetailStatus
            FROM dbo.SalesApprovalRequestDetail d
            WHERE d.ApprovalRequestDetailId = @LatestDetailId;

            INSERT INTO @PreDetail (
                  ApprovalRequestDetailId, ApprovalRequestId, ActivityId
                , DetailStatus, DetailRemarks, DetailApprovedBy, DetailApprovedOn, DetailApprovedLevel
                , DetailVersion, PreviousDetailId
                , Snapshot_TotalDistanceKM, Snapshot_CalculatedClaimAmount
                , Snapshot_DAAmount, Snapshot_HotelAmount, Snapshot_TravelAmount
            )
            SELECT
                  d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
                , d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                , d.DetailVersion, d.PreviousDetailId
                , d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
            FROM dbo.SalesApprovalRequestDetail d
            WHERE d.ApprovalRequestId = @ApprovalRequestId;

            INSERT INTO @PreApprover (
                  ApprovalRequestApproverId, ApproverLoginId, ApproverRole, ApproverLevel
                , IsRequired, Status, ActionBy, ActionDate, Remarks
            )
            SELECT
                  a.ApprovalRequestApproverId, a.ApproverLoginId, a.ApproverRole, a.ApproverLevel
                , a.IsRequired, a.Status, a.ActionBy, a.ActionDate, a.Remarks
            FROM dbo.SalesApprovalRequestApprover a
            WHERE a.ApprovalRequestId = @ApprovalRequestId;

            INSERT INTO @PreHistory (
                  HistoryId, OldStatus, NewStatus, ChangedBy, ChangedOn, Remarks
                , ApprovalRequestDetailId, ActivityId, ApproverLevel, ActionType, DetailVersion
            )
            SELECT
                  h.HistoryId, h.OldStatus, h.NewStatus, h.ChangedBy, h.ChangedOn, h.Remarks
                , h.ApprovalRequestDetailId, h.ActivityId, h.ApproverLevel, h.ActionType, h.DetailVersion
            FROM dbo.ApprovalRequestStatusHistory h
            WHERE h.ApprovalRequestId = @ApprovalRequestId;

            SELECT
                  @PreCompletedHist = ISNULL(SUM(CASE WHEN h.NewStatus = N'Completed' THEN 1 ELSE 0 END), 0)
                , @PreHistCount = COUNT(*)
            FROM @PreHistory h;

            INSERT INTO @PreActivity (
                  ActivityId, ClaimStatus, User_Id, Login_Id, ApprovalRequestId, IsDeleted
                , ModifiedByUserId, ModifiedDateTime, ClaimProcessedBy, ClaimProcessedDate
                , SubmittedBy, SubmittedOn, RowVersion
            )
            SELECT
                  a.ActivityId, a.ClaimStatus, a.User_Id, a.Login_Id, a.ApprovalRequestId, a.IsDeleted
                , a.ModifiedByUserId, a.ModifiedDateTime, a.ClaimProcessedBy, a.ClaimProcessedDate
                , a.SubmittedBy, a.SubmittedOn, CONVERT(varbinary(8), a.RowVersion)
            FROM dbo.SalesDailyActivity a
            WHERE a.ActivityId = @ActivityId;

            INSERT INTO @PreExpense (
                  ExpenseId, ActivityId, ClaimStatus, IsDeleted
                , ClaimProcessedBy, ClaimProcessedDate, ModifiedBy, ModifiedOn, Remarks
                , CreatedBy, DeletedBy, DeletedOn, IsEligible
            )
            SELECT
                  e.ExpenseId, e.ActivityId, e.ClaimStatus, e.IsDeleted
                , e.ClaimProcessedBy, e.ClaimProcessedDate, e.ModifiedBy, e.ModifiedOn, e.Remarks
                , e.CreatedBy, e.DeletedBy, e.DeletedOn
                , CASE
                    WHEN e.IsDeleted = 0 AND (
                           (@NormalizedAction = N'APPROVE' AND ISNULL(e.ClaimStatus, N'') NOT IN (N'Approved', N'Rejected'))
                        OR (@NormalizedAction = N'RETURN' AND ISNULL(e.ClaimStatus, N'') NOT IN (N'Rejected'))
                        OR (@NormalizedAction = N'REJECT' AND ISNULL(e.ClaimStatus, N'') NOT IN (N'Approved'))
                    ) THEN 1 ELSE 0
                  END
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = @ActivityId;

            SELECT
                  @ExpenseCount = COUNT(*)
                , @ExpenseEligibleCount = ISNULL(SUM(CASE WHEN e.IsEligible = 1 THEN 1 ELSE 0 END), 0)
            FROM @PreExpense e;

            SET @ImagesCaptured = 1;
            PRINT N'INFO | expenseRows=' + CONVERT(nvarchar(12), @ExpenseCount)
                + N' eligibleExpenseRows=' + CONVERT(nvarchar(12), @ExpenseEligibleCount);

            SET @PreHigher = CASE WHEN EXISTS (
                SELECT 1
                FROM dbo.SalesApprovalRequestApprover nxt
                WHERE nxt.ApprovalRequestId = @ApprovalRequestId
                  AND nxt.ApproverLevel > @PreLevel
                  AND nxt.Status = N'Pending'
                  AND ISNULL(nxt.IsRequired, 1) = 1
            ) THEN 1 ELSE 0 END;

            SET @ExpectError = 0;
            IF @NormalizedAction NOT IN (N'APPROVE', N'RETURN', N'REJECT')
                SET @ExpectError = 1;
            IF ISNULL(@PreStatus, N'') <> N'Pending'
                SET @ExpectError = 1;
            IF @PreLogin IS NULL
               OR UPPER(LTRIM(RTRIM(@PreLogin))) <> UPPER(LTRIM(RTRIM(@ActionBy)))
                SET @ExpectError = 1;
            IF @LatestDetailId IS NULL OR @LatestDetailId <> @ApprovalRequestDetailId
                SET @ExpectError = 1;
            IF @NormalizedAction = N'REJECT' AND ISNULL(@PreRole, N'') <> N'Auditor'
                SET @ExpectError = 1;
            IF @NormalizedAction = N'RETURN' AND ISNULL(@PreRole, N'') = N'Auditor'
                SET @ExpectError = 1;
            IF @NormalizedAction IN (N'RETURN', N'REJECT')
               AND NULLIF(LTRIM(RTRIM(@Remarks)), N'') IS NULL
                SET @ExpectError = 1;

            BEGIN TRY
                EXEC dbo.usp_ProcessApprovalAction
                      @ApprovalRequestDetailId = @ApprovalRequestDetailId
                    , @Action = @Action
                    , @ActionBy = @ActionBy
                    , @Remarks = @Remarks;
                SET @ProcError = 0;
            END TRY
            BEGIN CATCH
                SET @ProcError = 1;
                SET @ErrNum = ERROR_NUMBER();
                SET @ErrMsg = ERROR_MESSAGE();
            END CATCH;

            SET @DidExec = 1;
        END;
    END;

    IF @BlockReason IS NOT NULL
    BEGIN
        PRINT N'BLOCKED | ' + @BlockReason;
        SET @Outcome = N'BLOCKED';
    END;

    SET @TranCountAfter = @@TRANCOUNT;
    SET @XactStateAfter = XACT_STATE();
    PRINT N'INFO | after probe @@TRANCOUNT=' + CONVERT(nvarchar(12), @TranCountAfter)
        + N' XACT_STATE=' + CONVERT(nvarchar(12), @XactStateAfter)
        + N' didExec=' + CONVERT(nvarchar(1), @DidExec)
        + N' procError=' + CONVERT(nvarchar(1), @ProcError)
        + N' expectError=' + CONVERT(nvarchar(1), @ExpectError);

    IF @DidExec = 1 AND @TranCountAfter = 0
    BEGIN
        SET @Unsafe = 1;
        SET @Fail += 1;
        IF @ProcError = 0
            SET @UnexpectedCommit = 1;
        ELSE
            SET @OuterClearedByProc = 1;
        PRINT N'UNSAFE | procedure returned with @@TRANCOUNT = 0 before the harness rollback. Inconclusive. This script will not COMMIT and will not try to reverse data.'
            + CASE WHEN @ProcError = 1 THEN N' error ' + ISNULL(CONVERT(nvarchar(12), @ErrNum), N'?') + N' ' + ISNULL(@ErrMsg, N'') ELSE N'' END;
    END
    ELSE IF @DidExec = 1 AND (@TranCountAfter <> 1 OR ISNULL(@XactStateAfter, 0) <> 1)
    BEGIN
        SET @Unsafe = 1;
        SET @Fail += 1;
        PRINT N'UNSAFE | transaction state after the call is not one committable outer transaction. Inconclusive. No repair.';
    END;

    IF @DidExec = 1 AND @Unsafe = 0 AND @TranCountAfter = 1 AND @XactStateAfter = 1
        SET @CanRead = 1;

    IF @CanRead = 1
    BEGIN
        SELECT
              @PostStatus = r.Status
            , @PostLogin = r.CurrentApproverLoginId
            , @PostLevel = r.CurrentApproverLevel
            , @PostModifiedOn = r.ModifiedOn
            , @PostModifiedBy = r.ModifiedBy
            , @PostClaim = r.ApprovedClaimAmount
            , @PostDa = r.ApprovedDAAmount
            , @PostHotel = r.ApprovedHotelAmount
            , @PostTravel = r.ApprovedTravelAmount
            , @PostKm = r.ApprovedKM
        FROM dbo.SalesApprovalRequest r
        WHERE r.ApprovalRequestId = @ApprovalRequestId;

        SELECT @PostDetailStatus = d.DetailStatus
        FROM dbo.SalesApprovalRequestDetail d
        WHERE d.ApprovalRequestDetailId = @ApprovalRequestDetailId;

        SELECT @PostLatestStatus = d.DetailStatus
        FROM dbo.SalesApprovalRequestDetail d
        WHERE d.ApprovalRequestDetailId = @LatestDetailId;

        SELECT @PostAuditorCount = COUNT(*)
        FROM dbo.SalesApprovalRequestApprover a
        WHERE a.ApprovalRequestId = @ApprovalRequestId
          AND a.ApproverRole = N'Auditor';

        IF @PostAuditorCount = 1
            SELECT @PostAuditorStatus = a.Status
            FROM dbo.SalesApprovalRequestApprover a
            WHERE a.ApprovalRequestId = @ApprovalRequestId
              AND a.ApproverRole = N'Auditor';
        ELSE IF @PostAuditorCount > 1
        BEGIN
            PRINT N'CHECK Auditor-row cardinality stayed unambiguous | FAIL';
            SET @Fail += 1;
        END;

        SELECT
              @PostCompletedHist = ISNULL(SUM(CASE WHEN h.NewStatus = N'Completed' THEN 1 ELSE 0 END), 0)
            , @PostHistCount = COUNT(*)
        FROM dbo.ApprovalRequestStatusHistory h
        WHERE h.ApprovalRequestId = @ApprovalRequestId;

        IF @LatestDetailId IS NULL
        BEGIN
            PRINT N'CHECK detail rows other than the acted activity latest id are unchanged inside the open transaction | FAIL | latest detail id was not resolved';
            SET @Fail += 1;
        END
        ELSE IF EXISTS (
                SELECT
                      d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
                    , d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                    , d.DetailVersion, d.PreviousDetailId
                    , d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                    , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
                FROM @PreDetail d
                WHERE d.ApprovalRequestDetailId <> @LatestDetailId
                EXCEPT
                SELECT
                      d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
                    , d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                    , d.DetailVersion, d.PreviousDetailId
                    , d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                    , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
                FROM dbo.SalesApprovalRequestDetail d
                WHERE d.ApprovalRequestId = @ApprovalRequestId
                  AND d.ApprovalRequestDetailId <> @LatestDetailId
            )
            OR EXISTS (
                SELECT
                      d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
                    , d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                    , d.DetailVersion, d.PreviousDetailId
                    , d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                    , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
                FROM dbo.SalesApprovalRequestDetail d
                WHERE d.ApprovalRequestId = @ApprovalRequestId
                  AND d.ApprovalRequestDetailId <> @LatestDetailId
                EXCEPT
                SELECT
                      d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
                    , d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                    , d.DetailVersion, d.PreviousDetailId
                    , d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                    , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
                FROM @PreDetail d
                WHERE d.ApprovalRequestDetailId <> @LatestDetailId
            )
        BEGIN
            PRINT N'CHECK detail rows other than the acted activity latest id are unchanged inside the open transaction | FAIL';
            SET @Fail += 1;
        END
        ELSE
            PRINT N'CHECK detail rows other than the acted activity latest id are unchanged inside the open transaction | PASS';

        IF @PostStatus = N'ReadyForPayment' AND ISNULL(@PreStatus, N'') <> N'ReadyForPayment'
        BEGIN
            PRINT N'CHECK procedure did not move the header to ReadyForPayment | FAIL';
            SET @Fail += 1;
        END
        ELSE
            PRINT N'CHECK procedure did not move the header to ReadyForPayment | PASS';

        IF @ProcError = 1
        BEGIN
            IF EXISTS (
                    SELECT @PreStatus, @PreLogin, @PreLevel, @PreModifiedOn, @PreModifiedBy, @PreClaim, @PreDa, @PreHotel, @PreTravel, @PreKm
                    EXCEPT
                    SELECT @PostStatus, @PostLogin, @PostLevel, @PostModifiedOn, @PostModifiedBy, @PostClaim, @PostDa, @PostHotel, @PostTravel, @PostKm
                )
                OR EXISTS (
                    SELECT
                          d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
                        , d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                        , d.DetailVersion, d.PreviousDetailId, d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                        , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
                    FROM @PreDetail d
                    EXCEPT
                    SELECT
                          d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
                        , d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                        , d.DetailVersion, d.PreviousDetailId, d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                        , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
                    FROM dbo.SalesApprovalRequestDetail d
                    WHERE d.ApprovalRequestId = @ApprovalRequestId
                )
                OR EXISTS (
                    SELECT
                          d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
                        , d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                        , d.DetailVersion, d.PreviousDetailId, d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                        , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
                    FROM dbo.SalesApprovalRequestDetail d
                    WHERE d.ApprovalRequestId = @ApprovalRequestId
                    EXCEPT
                    SELECT
                          d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
                        , d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                        , d.DetailVersion, d.PreviousDetailId, d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                        , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
                    FROM @PreDetail d
                )
                OR EXISTS (
                    SELECT
                          a.ApprovalRequestApproverId, a.ApproverLoginId, a.ApproverRole, a.ApproverLevel
                        , a.IsRequired, a.Status, a.ActionBy, a.ActionDate, a.Remarks
                    FROM @PreApprover a
                    EXCEPT
                    SELECT
                          a.ApprovalRequestApproverId, a.ApproverLoginId, a.ApproverRole, a.ApproverLevel
                        , a.IsRequired, a.Status, a.ActionBy, a.ActionDate, a.Remarks
                    FROM dbo.SalesApprovalRequestApprover a
                    WHERE a.ApprovalRequestId = @ApprovalRequestId
                )
                OR EXISTS (
                    SELECT
                          a.ApprovalRequestApproverId, a.ApproverLoginId, a.ApproverRole, a.ApproverLevel
                        , a.IsRequired, a.Status, a.ActionBy, a.ActionDate, a.Remarks
                    FROM dbo.SalesApprovalRequestApprover a
                    WHERE a.ApprovalRequestId = @ApprovalRequestId
                    EXCEPT
                    SELECT
                          a.ApprovalRequestApproverId, a.ApproverLoginId, a.ApproverRole, a.ApproverLevel
                        , a.IsRequired, a.Status, a.ActionBy, a.ActionDate, a.Remarks
                    FROM @PreApprover a
                )
                OR EXISTS (
                    SELECT
                          h.HistoryId, h.OldStatus, h.NewStatus, h.ChangedBy, h.ChangedOn, h.Remarks
                        , h.ApprovalRequestDetailId, h.ActivityId, h.ApproverLevel, h.ActionType, h.DetailVersion
                    FROM @PreHistory h
                    EXCEPT
                    SELECT
                          h.HistoryId, h.OldStatus, h.NewStatus, h.ChangedBy, h.ChangedOn, h.Remarks
                        , h.ApprovalRequestDetailId, h.ActivityId, h.ApproverLevel, h.ActionType, h.DetailVersion
                    FROM dbo.ApprovalRequestStatusHistory h
                    WHERE h.ApprovalRequestId = @ApprovalRequestId
                )
                OR EXISTS (
                    SELECT
                          h.HistoryId, h.OldStatus, h.NewStatus, h.ChangedBy, h.ChangedOn, h.Remarks
                        , h.ApprovalRequestDetailId, h.ActivityId, h.ApproverLevel, h.ActionType, h.DetailVersion
                    FROM dbo.ApprovalRequestStatusHistory h
                    WHERE h.ApprovalRequestId = @ApprovalRequestId
                    EXCEPT
                    SELECT
                          h.HistoryId, h.OldStatus, h.NewStatus, h.ChangedBy, h.ChangedOn, h.Remarks
                        , h.ApprovalRequestDetailId, h.ActivityId, h.ApproverLevel, h.ActionType, h.DetailVersion
                    FROM @PreHistory h
                )
                OR EXISTS (
                    SELECT
                          p.ActivityId, p.ClaimStatus, p.User_Id, p.Login_Id, p.ApprovalRequestId, p.IsDeleted
                        , p.ModifiedByUserId, p.ModifiedDateTime, p.ClaimProcessedBy, p.ClaimProcessedDate
                        , p.SubmittedBy, p.SubmittedOn, p.RowVersion
                    FROM @PreActivity p
                    EXCEPT
                    SELECT
                          a.ActivityId, a.ClaimStatus, a.User_Id, a.Login_Id, a.ApprovalRequestId, a.IsDeleted
                        , a.ModifiedByUserId, a.ModifiedDateTime, a.ClaimProcessedBy, a.ClaimProcessedDate
                        , a.SubmittedBy, a.SubmittedOn, CONVERT(varbinary(8), a.RowVersion)
                    FROM dbo.SalesDailyActivity a
                    WHERE a.ActivityId = @ActivityId
                )
                OR EXISTS (
                    SELECT
                          a.ActivityId, a.ClaimStatus, a.User_Id, a.Login_Id, a.ApprovalRequestId, a.IsDeleted
                        , a.ModifiedByUserId, a.ModifiedDateTime, a.ClaimProcessedBy, a.ClaimProcessedDate
                        , a.SubmittedBy, a.SubmittedOn, CONVERT(varbinary(8), a.RowVersion)
                    FROM dbo.SalesDailyActivity a
                    WHERE a.ActivityId = @ActivityId
                    EXCEPT
                    SELECT
                          p.ActivityId, p.ClaimStatus, p.User_Id, p.Login_Id, p.ApprovalRequestId, p.IsDeleted
                        , p.ModifiedByUserId, p.ModifiedDateTime, p.ClaimProcessedBy, p.ClaimProcessedDate
                        , p.SubmittedBy, p.SubmittedOn, p.RowVersion
                    FROM @PreActivity p
                )
                OR EXISTS (
                    SELECT
                          p.ExpenseId, p.ActivityId, p.ClaimStatus, p.IsDeleted, p.ClaimProcessedBy, p.ClaimProcessedDate
                        , p.ModifiedBy, p.ModifiedOn, p.Remarks, p.CreatedBy, p.DeletedBy, p.DeletedOn
                    FROM @PreExpense p
                    EXCEPT
                    SELECT
                          e.ExpenseId, e.ActivityId, e.ClaimStatus, e.IsDeleted, e.ClaimProcessedBy, e.ClaimProcessedDate
                        , e.ModifiedBy, e.ModifiedOn, e.Remarks, e.CreatedBy, e.DeletedBy, e.DeletedOn
                    FROM dbo.SalesDailyExpense e
                    WHERE e.ActivityId = @ActivityId
                )
                OR EXISTS (
                    SELECT
                          e.ExpenseId, e.ActivityId, e.ClaimStatus, e.IsDeleted, e.ClaimProcessedBy, e.ClaimProcessedDate
                        , e.ModifiedBy, e.ModifiedOn, e.Remarks, e.CreatedBy, e.DeletedBy, e.DeletedOn
                    FROM dbo.SalesDailyExpense e
                    WHERE e.ActivityId = @ActivityId
                    EXCEPT
                    SELECT
                          p.ExpenseId, p.ActivityId, p.ClaimStatus, p.IsDeleted, p.ClaimProcessedBy, p.ClaimProcessedDate
                        , p.ModifiedBy, p.ModifiedOn, p.Remarks, p.CreatedBy, p.DeletedBy, p.DeletedOn
                    FROM @PreExpense p
                )
            BEGIN
                PRINT N'CHECK procedure error left captured rows unchanged inside the open transaction | FAIL | a write was observed | not a before-write rejection';
                SET @Fail += 1;
            END
            ELSE IF @ExpectError = 1
            BEGIN
                SET @InTranUnchanged = 1;
                PRINT N'CHECK expected rejection left captured rows unchanged inside the open transaction | PASS | not rollback proof';
            END
            ELSE
                PRINT N'INFO | procedure error left captured rows unchanged | not rollback proof';

            IF @ExpectError = 1
               AND ISNULL(@PostDetailStatus, N'') = ISNULL(@PreDetailStatus, N'')
               AND ISNULL(@PostLatestStatus, N'') = ISNULL(@PreLatestStatus, N'')
                PRINT N'CHECK expected rejection left detail status unchanged | PASS';
            ELSE IF @ExpectError = 1
            BEGIN
                PRINT N'CHECK expected rejection left detail status unchanged | FAIL';
                SET @Fail += 1;
            END;
        END;

        IF @ProcError = 0 AND @ExpectError = 0 AND @LatestDetailId = @ApprovalRequestDetailId
           AND ISNULL(@PostDetailStatus, N'') = ISNULL(@Mapped, N'')
            PRINT N'CHECK acted latest detail status | PASS';
        ELSE IF @ProcError = 0 AND @ExpectError = 0 AND @LatestDetailId = @ApprovalRequestDetailId
        BEGIN
            PRINT N'CHECK acted latest detail status | FAIL | actual=' + ISNULL(@PostDetailStatus, N'<null>');
            SET @Fail += 1;
            SET @WriteMiss += 1;
        END;

        IF @ProcError = 0 AND @ExpectError = 0
        BEGIN
            IF EXISTS (
                    SELECT p.DetailStatus, p.DetailRemarks, p.DetailApprovedBy, p.DetailApprovedOn, p.DetailApprovedLevel
                    FROM @PreDetail p
                    WHERE p.ApprovalRequestDetailId = @ApprovalRequestDetailId
                    INTERSECT
                    SELECT d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                    FROM dbo.SalesApprovalRequestDetail d
                    WHERE d.ApprovalRequestDetailId = @ApprovalRequestDetailId
                )
                OR NOT EXISTS (
                    SELECT 1
                    FROM dbo.SalesApprovalRequestDetail d
                    WHERE d.ApprovalRequestDetailId = @ApprovalRequestDetailId
                      AND ISNULL(d.DetailStatus, N'') = ISNULL(@Mapped, N'')
                )
            BEGIN
                PRINT N'CHECK accepted action changed the acted detail inside the open transaction | FAIL';
                SET @Fail += 1;
                SET @WriteMiss += 1;
            END
            ELSE
                PRINT N'CHECK accepted action changed the acted detail inside the open transaction | PASS';

            IF EXISTS (
                    SELECT
                          p.ApprovalRequestId, p.ActivityId, p.DetailVersion, p.PreviousDetailId
                        , p.Snapshot_TotalDistanceKM, p.Snapshot_CalculatedClaimAmount
                        , p.Snapshot_DAAmount, p.Snapshot_HotelAmount, p.Snapshot_TravelAmount
                    FROM @PreDetail p
                    WHERE p.ApprovalRequestDetailId = @ApprovalRequestDetailId
                    INTERSECT
                    SELECT
                          d.ApprovalRequestId, d.ActivityId, d.DetailVersion, d.PreviousDetailId
                        , d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                        , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
                    FROM dbo.SalesApprovalRequestDetail d
                    WHERE d.ApprovalRequestDetailId = @ApprovalRequestDetailId
                )
                PRINT N'CHECK acted detail ownership, version, and snapshots stayed unchanged inside the open transaction | PASS';
            ELSE
            BEGIN
                PRINT N'CHECK acted detail ownership, version, and snapshots stayed unchanged inside the open transaction | FAIL';
                SET @Fail += 1;
                SET @WriteMiss += 1;
            END;

            IF EXISTS (
                    SELECT p.ClaimStatus, p.ModifiedByUserId, p.ModifiedDateTime
                    FROM @PreActivity p
                    EXCEPT
                    SELECT a.ClaimStatus, a.ModifiedByUserId, a.ModifiedDateTime
                    FROM dbo.SalesDailyActivity a
                    WHERE a.ActivityId = @ActivityId
                )
               AND EXISTS (
                    SELECT p.RowVersion
                    FROM @PreActivity p
                    EXCEPT
                    SELECT CONVERT(varbinary(8), a.RowVersion)
                    FROM dbo.SalesDailyActivity a
                    WHERE a.ActivityId = @ActivityId
                )
               AND EXISTS (
                    SELECT 1
                    FROM dbo.SalesDailyActivity a
                    WHERE a.ActivityId = @ActivityId
                      AND ISNULL(a.ClaimStatus, N'') = ISNULL(@Mapped, N'')
                )
               AND EXISTS (
                    SELECT p.User_Id, p.Login_Id, p.ApprovalRequestId, p.IsDeleted, p.ClaimProcessedBy, p.ClaimProcessedDate, p.SubmittedBy, p.SubmittedOn
                    FROM @PreActivity p
                    INTERSECT
                    SELECT a.User_Id, a.Login_Id, a.ApprovalRequestId, a.IsDeleted, a.ClaimProcessedBy, a.ClaimProcessedDate, a.SubmittedBy, a.SubmittedOn
                    FROM dbo.SalesDailyActivity a
                    WHERE a.ActivityId = @ActivityId
                )
                PRINT N'CHECK accepted action changed the activity inside the open transaction | PASS';
            ELSE
            BEGIN
                PRINT N'CHECK accepted action changed the activity inside the open transaction | FAIL';
                SET @Fail += 1;
                SET @WriteMiss += 1;
            END;

            IF EXISTS (
                    SELECT @PreModifiedOn, @PreModifiedBy
                    EXCEPT
                    SELECT @PostModifiedOn, @PostModifiedBy
                )
                PRINT N'CHECK accepted action changed header audit columns inside the open transaction | PASS';
            ELSE
            BEGIN
                PRINT N'CHECK accepted action changed header audit columns inside the open transaction | FAIL';
                SET @Fail += 1;
                SET @WriteMiss += 1;
            END;

            IF @ExpenseEligibleCount = 0
                PRINT N'INFO | no eligible SalesDailyExpense row. An expense UPDATE was not observed. Restoration still compares every expense row for this activity.';
            ELSE IF EXISTS (
                    SELECT 1
                    FROM @PreExpense p
                    WHERE p.IsEligible = 1
                      AND (
                            NOT EXISTS (SELECT 1 FROM dbo.SalesDailyExpense e WHERE e.ExpenseId = p.ExpenseId)
                            OR NOT EXISTS (
                                SELECT 1
                                FROM dbo.SalesDailyExpense e
                                WHERE e.ExpenseId = p.ExpenseId
                                  AND ISNULL(e.ClaimStatus, N'') = ISNULL(@Mapped, N'')
                            )
                            OR EXISTS (
                                SELECT src.ClaimStatus, src.ClaimProcessedBy, src.ClaimProcessedDate, src.ModifiedBy, src.ModifiedOn, src.Remarks
                                FROM @PreExpense src
                                WHERE src.ExpenseId = p.ExpenseId
                                INTERSECT
                                SELECT e.ClaimStatus, e.ClaimProcessedBy, e.ClaimProcessedDate, e.ModifiedBy, e.ModifiedOn, e.Remarks
                                FROM dbo.SalesDailyExpense e
                                WHERE e.ExpenseId = p.ExpenseId
                            )
                            OR NOT EXISTS (
                                SELECT src.IsDeleted, src.CreatedBy, src.DeletedBy, src.DeletedOn, src.ActivityId
                                FROM @PreExpense src
                                WHERE src.ExpenseId = p.ExpenseId
                                INTERSECT
                                SELECT e.IsDeleted, e.CreatedBy, e.DeletedBy, e.DeletedOn, e.ActivityId
                                FROM dbo.SalesDailyExpense e
                                WHERE e.ExpenseId = p.ExpenseId
                            )
                      )
                )
            BEGIN
                PRINT N'CHECK accepted action changed every eligible expense inside the open transaction | FAIL';
                SET @Fail += 1;
                SET @WriteMiss += 1;
            END
            ELSE
                PRINT N'CHECK accepted action changed every eligible expense inside the open transaction | PASS';

            IF EXISTS (
                    SELECT
                          p.ExpenseId, p.ActivityId, p.ClaimStatus, p.IsDeleted, p.ClaimProcessedBy, p.ClaimProcessedDate
                        , p.ModifiedBy, p.ModifiedOn, p.Remarks, p.CreatedBy, p.DeletedBy, p.DeletedOn
                    FROM @PreExpense p
                    WHERE p.IsEligible = 0
                    EXCEPT
                    SELECT
                          e.ExpenseId, e.ActivityId, e.ClaimStatus, e.IsDeleted, e.ClaimProcessedBy, e.ClaimProcessedDate
                        , e.ModifiedBy, e.ModifiedOn, e.Remarks, e.CreatedBy, e.DeletedBy, e.DeletedOn
                    FROM dbo.SalesDailyExpense e
                    INNER JOIN @PreExpense p ON p.ExpenseId = e.ExpenseId AND p.IsEligible = 0
                )
                OR EXISTS (
                    SELECT
                          e.ExpenseId, e.ActivityId, e.ClaimStatus, e.IsDeleted, e.ClaimProcessedBy, e.ClaimProcessedDate
                        , e.ModifiedBy, e.ModifiedOn, e.Remarks, e.CreatedBy, e.DeletedBy, e.DeletedOn
                    FROM dbo.SalesDailyExpense e
                    INNER JOIN @PreExpense p ON p.ExpenseId = e.ExpenseId AND p.IsEligible = 0
                    EXCEPT
                    SELECT
                          p.ExpenseId, p.ActivityId, p.ClaimStatus, p.IsDeleted, p.ClaimProcessedBy, p.ClaimProcessedDate
                        , p.ModifiedBy, p.ModifiedOn, p.Remarks, p.CreatedBy, p.DeletedBy, p.DeletedOn
                    FROM @PreExpense p
                    WHERE p.IsEligible = 0
                )
                OR EXISTS (
                    SELECT e.ExpenseId
                    FROM dbo.SalesDailyExpense e
                    WHERE e.ActivityId = @ActivityId
                      AND NOT EXISTS (SELECT 1 FROM @PreExpense p WHERE p.ExpenseId = e.ExpenseId)
                )
            BEGIN
                PRINT N'CHECK ineligible expenses and expense cardinality stayed unchanged inside the open transaction | FAIL';
                SET @Fail += 1;
                SET @WriteMiss += 1;
            END
            ELSE
                PRINT N'CHECK ineligible expenses and expense cardinality stayed unchanged inside the open transaction | PASS';

            IF @WriteMiss = 0
                SET @AcceptedWriteObserved = 1;

            SELECT
                  @PostPending = ISNULL(SUM(CASE
                        WHEN ISNULL(ld.DetailStatus, N'') IN (N'', N'Pending', N'Submitted') THEN 1
                        WHEN ISNULL(ld.DetailStatus, N'') = N'Approved'
                         AND ISNULL(ld.DetailApprovedLevel, 0) < ISNULL(@PreLevel, 0) THEN 1
                        ELSE 0
                    END), 0)
                , @PostReturned = ISNULL(SUM(CASE
                        WHEN ISNULL(ld.DetailStatus, N'') = N'Returned' THEN 1
                        ELSE 0
                    END), 0)
            FROM dbo.SalesApprovalRequestDetail ld
            INNER JOIN (
                SELECT d2.ActivityId, MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
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
            ) latest
                ON ld.ApprovalRequestDetailId = latest.LatestDetailId
            WHERE ld.ApprovalRequestId = @ApprovalRequestId;

            SELECT
                  @ExpClaim = ISNULL(SUM(d.Snapshot_CalculatedClaimAmount), 0)
                , @ExpDa = ISNULL(SUM(d.Snapshot_DAAmount), 0)
                , @ExpHotel = ISNULL(SUM(d.Snapshot_HotelAmount), 0)
                , @ExpTravel = ISNULL(SUM(d.Snapshot_TravelAmount), 0)
                , @ExpKm = ISNULL(SUM(CONVERT(decimal(18, 2), d.Snapshot_TotalDistanceKM)), 0)
            FROM dbo.SalesApprovalRequestDetail d
            INNER JOIN (
                SELECT d2.ActivityId, MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
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
            ) latest
                ON d.ApprovalRequestDetailId = latest.LatestDetailId
            WHERE d.ApprovalRequestId = @ApprovalRequestId
              AND ISNULL(d.DetailStatus, N'') NOT IN (N'Rejected', N'Returned');

            IF ISNULL(@PostClaim, 0) = ISNULL(@ExpClaim, 0)
               AND ISNULL(@PostDa, 0) = ISNULL(@ExpDa, 0)
               AND ISNULL(@PostHotel, 0) = ISNULL(@ExpHotel, 0)
               AND ISNULL(@PostTravel, 0) = ISNULL(@ExpTravel, 0)
               AND ISNULL(CONVERT(decimal(18, 2), @PostKm), 0) = ISNULL(@ExpKm, 0)
                PRINT N'CHECK header amounts match latest SQL snapshot sums | PASS';
            ELSE
            BEGIN
                PRINT N'CHECK header amounts match latest SQL snapshot sums | FAIL';
                SET @Fail += 1;
            END;

            IF @PreHigher = 1 AND @PostStatus = N'Completed'
            BEGIN
                PRINT N'CHECK no Completed while a higher Pending required approver existed on this request | FAIL';
                SET @Fail += 1;
            END
            ELSE IF @PreHigher = 1
                PRINT N'CHECK no Completed while a higher Pending required approver existed on this request | PASS';

            IF @PostPending > 0 AND @PostStatus = N'Completed'
            BEGIN
                PRINT N'CHECK no Completed while a latest row is still unresolved at the acting level | FAIL';
                SET @Fail += 1;
            END;

            IF ISNULL(@PreRole, N'') = N'Auditor' AND @PostStatus = N'Completed'
            BEGIN
                PRINT N'CHECK Auditor action did not write Completed | FAIL';
                SET @Fail += 1;
            END
            ELSE IF ISNULL(@PreRole, N'') = N'Auditor'
                PRINT N'CHECK Auditor action did not write Completed | PASS';

            IF @PostPending > 0 OR @PostReturned > 0
            BEGIN
                IF @PostStatus <> N'Completed'
                   AND @PostStatus <> N'ReadyForPayment'
                   AND ISNULL(@PostLogin, N'') = ISNULL(@PreLogin, N'')
                   AND ISNULL(@PostLevel, -1) = ISNULL(@PreLevel, -1)
                   AND @PostCompletedHist = @PreCompletedHist
                    PRINT N'CHECK unresolved latest rows left the current approver in place | PASS';
                ELSE
                BEGIN
                    PRINT N'CHECK unresolved latest rows left the current approver in place | FAIL | status=' + ISNULL(@PostStatus, N'<null>');
                    SET @Fail += 1;
                END;
            END;

            IF @NormalizedAction = N'APPROVE' AND @PreHigher = 1 AND @PostPending = 0 AND @PostReturned = 0
            BEGIN
                IF @PostStatus = N'Pending'
                   AND @PostLevel > @PreLevel
                   AND @PostCompletedHist = @PreCompletedHist
                    PRINT N'CHECK this request moved to the next required approver instead of Completed | PASS';
                ELSE
                BEGIN
                    PRINT N'CHECK this request moved to the next required approver instead of Completed | FAIL | status=' + ISNULL(@PostStatus, N'<null>');
                    SET @Fail += 1;
                END;
            END;

            IF ISNULL(@PreRole, N'') = N'Auditor'
               AND @PreHigher = 0
               AND @NormalizedAction = N'APPROVE'
               AND @PostPending = 0
               AND @PostReturned = 0
            BEGIN
                IF @PostAuditorCount = 1
                   AND @PostStatus = N'Pending'
                   AND ISNULL(@PostLogin, N'') = ISNULL(@PreLogin, N'')
                   AND ISNULL(@PostLevel, -1) = ISNULL(@PreLevel, -1)
                   AND ISNULL(@PostAuditorStatus, N'') = ISNULL(@PreAuditorStatus, N'')
                   AND ISNULL(@PostAuditorStatus, N'') = N'Pending'
                   AND @PostCompletedHist = @PreCompletedHist
                    PRINT N'CHECK final Auditor pass stayed Pending and assigned to the Auditor | PASS';
                ELSE
                BEGIN
                    PRINT N'CHECK final Auditor pass stayed Pending and assigned to the Auditor | FAIL'
                        + N' | status=' + ISNULL(@PostStatus, N'<null>')
                        + N' auditorStatus=' + ISNULL(@PostAuditorStatus, N'<null>')
                        + N' auditorRows=' + CONVERT(nvarchar(12), @PostAuditorCount);
                    SET @Fail += 1;
                END;
            END;

            IF ISNULL(@PreRole, N'') <> N'Auditor'
               AND @PreHigher = 0
               AND @NormalizedAction = N'APPROVE'
               AND @PostPending = 0
               AND @PostReturned = 0
            BEGIN
                IF @PostStatus = N'Completed' AND @PostLogin IS NULL AND @PostLevel IS NULL
                    PRINT N'INFO | accepted 39a12b0 baseline: non-Auditor final branch wrote Completed and cleared the current approver';
                ELSE
                BEGIN
                    PRINT N'CHECK accepted non-Auditor final branch wrote Completed | FAIL | status=' + ISNULL(@PostStatus, N'<null>');
                    SET @Fail += 1;
                END;
            END;
        END;

        IF @DidExec = 1 AND @ProcError = 0 AND @ExpectError = 1
        BEGIN
            PRINT N'CHECK procedure outcome | FAIL | expected a rejection and the procedure returned without error';
            SET @Fail += 1;
        END
        ELSE IF @DidExec = 1 AND @ProcError = 1 AND @ExpectError = 0
        BEGIN
            PRINT N'CHECK procedure outcome | FAIL | error ' + ISNULL(CONVERT(nvarchar(12), @ErrNum), N'?') + N' ' + ISNULL(@ErrMsg, N'');
            SET @Fail += 1;
        END
        ELSE IF @DidExec = 1 AND @ProcError = 1 AND @ExpectError = 1
            PRINT N'CHECK procedure outcome | PASS | expected rejection before the procedure opened its transaction | not rollback proof';
        ELSE IF @DidExec = 1 AND @ProcError = 0 AND @ExpectError = 0
            PRINT N'CHECK procedure outcome | PASS | procedure accepted the action | not rollback proof until restoration';
    END;
END TRY
BEGIN CATCH
    SET @ScriptError = ERROR_MESSAGE();
    IF @DidExec = 1 AND @@TRANCOUNT = 0
    BEGIN
        SET @Unsafe = 1;
        PRINT N'UNSAFE | script error after the procedure returned with @@TRANCOUNT = 0. Inconclusive. No repair. ' + ISNULL(@ScriptError, N'');
    END
    ELSE
    BEGIN
        PRINT N'BLOCKED | script error before cleanup: ' + ISNULL(@ScriptError, N'');
        SET @Outcome = N'BLOCKED';
    END;
    SET @Fail += 1;
END CATCH;

IF @TranStarted = 1 AND @@TRANCOUNT > 0 AND XACT_STATE() <> 0
BEGIN
    BEGIN TRY
        ROLLBACK TRANSACTION RC014B_Outer;
        SET @RollbackIssued = 1;
        PRINT N'INFO | ROLLBACK TRANSACTION RC014B_Outer issued as session cleanup. This line alone is not rollback proof.';
    END TRY
    BEGIN CATCH
        PRINT N'BLOCKED | named ROLLBACK failed: ' + ERROR_MESSAGE();
        SET @Fail += 1;
        SET @Outcome = N'BLOCKED';
        IF @DidExec = 1
            SET @Unsafe = 1;
    END CATCH;
END
ELSE IF @TranStarted = 1 AND @@TRANCOUNT = 0
BEGIN
    IF @Unsafe = 1
        PRINT N'INFO | no ROLLBACK issued. @@TRANCOUNT was already 0. No repair was attempted.';
    ELSE
        PRINT N'INFO | no ROLLBACK issued by this script because @@TRANCOUNT = 0.';
END;

IF @@TRANCOUNT > 0 AND XACT_STATE() <> 0
BEGIN
    BEGIN TRY
        ROLLBACK TRANSACTION;
        PRINT N'INFO | unnamed ROLLBACK issued because a transaction was still open. This is cleanup, not proof, and not a repair of committed data.';
    END TRY
    BEGIN CATCH
        PRINT N'BLOCKED | unnamed ROLLBACK failed: ' + ERROR_MESSAGE();
        SET @Fail += 1;
        SET @Outcome = N'BLOCKED';
        IF @DidExec = 1
            SET @Unsafe = 1;
    END CATCH;
END;

IF @@TRANCOUNT <> 0
BEGIN
    PRINT N'BLOCKED | @@TRANCOUNT is still ' + CONVERT(nvarchar(12), @@TRANCOUNT) + N' after cleanup. Do not COMMIT. Human review required.';
    SET @Outcome = N'BLOCKED';
    SET @Fail += 1;
    IF @DidExec = 1
        SET @Unsafe = 1;
END;

IF (@@OPTIONS & 16384) = 16384 AND @XactAbortBefore = 0
BEGIN
    SET XACT_ABORT OFF;
    PRINT N'CHECK session XACT_ABORT restored by procedure exit | FAIL | this script turned it back OFF';
    SET @Fail += 1;
END
ELSE IF (@@OPTIONS & 16384) <> 16384 AND @XactAbortBefore = 1
BEGIN
    SET XACT_ABORT ON;
    PRINT N'CHECK session XACT_ABORT restored by procedure exit | FAIL | this script turned it back ON';
    SET @Fail += 1;
END
ELSE IF @DidExec = 1 AND @ScriptError IS NULL
    PRINT N'CHECK session XACT_ABORT matches the pre-call setting | PASS';
ELSE IF @DidExec = 1
    PRINT N'INFO | session XACT_ABORT was not given a procedure-exit result because the script raised its own error';
ELSE
    PRINT N'INFO | session XACT_ABORT unchanged; procedure was not called';

IF @DidExec = 1 AND @ImagesCaptured = 1 AND @@TRANCOUNT = 0
BEGIN
    SET @Compared = 1;

    SELECT
          @LiveStatus = r.Status
        , @LiveLogin = r.CurrentApproverLoginId
        , @LiveLevel = r.CurrentApproverLevel
        , @LiveModifiedOn = r.ModifiedOn
        , @LiveModifiedBy = r.ModifiedBy
        , @LiveClaim = r.ApprovedClaimAmount
        , @LiveDa = r.ApprovedDAAmount
        , @LiveHotel = r.ApprovedHotelAmount
        , @LiveTravel = r.ApprovedTravelAmount
        , @LiveKm = r.ApprovedKM
    FROM dbo.SalesApprovalRequest r
    WHERE r.ApprovalRequestId = @ApprovalRequestId;

    SELECT
          @LiveDetailStatus = d.DetailStatus
        , @LiveDetailRemarks = d.DetailRemarks
        , @LiveDetailLevel = d.DetailApprovedLevel
        , @LiveDetailApprovedBy = d.DetailApprovedBy
        , @LiveDetailApprovedOn = d.DetailApprovedOn
    FROM dbo.SalesApprovalRequestDetail d
    WHERE d.ApprovalRequestDetailId = @ApprovalRequestDetailId;

    SELECT @LiveLatestStatus = d.DetailStatus
    FROM dbo.SalesApprovalRequestDetail d
    WHERE d.ApprovalRequestDetailId = @LatestDetailId;

    SELECT
          @LiveCompletedHist = ISNULL(SUM(CASE WHEN h.NewStatus = N'Completed' THEN 1 ELSE 0 END), 0)
        , @LiveHistCount = COUNT(*)
    FROM dbo.ApprovalRequestStatusHistory h
    WHERE h.ApprovalRequestId = @ApprovalRequestId;

    IF EXISTS (
        SELECT
              @PreStatus, @PreLogin, @PreLevel, @PreModifiedOn, @PreModifiedBy
            , @PreClaim, @PreDa, @PreHotel, @PreTravel, @PreKm
            , @PreCompletedHist, @PreHistCount
            , @PreDetailStatus, @PreDetailRemarks, @PreDetailLevel, @PreDetailApprovedBy, @PreDetailApprovedOn
            , @PreLatestStatus
        EXCEPT
        SELECT
              @LiveStatus, @LiveLogin, @LiveLevel, @LiveModifiedOn, @LiveModifiedBy
            , @LiveClaim, @LiveDa, @LiveHotel, @LiveTravel, @LiveKm
            , @LiveCompletedHist, @LiveHistCount
            , @LiveDetailStatus, @LiveDetailRemarks, @LiveDetailLevel, @LiveDetailApprovedBy, @LiveDetailApprovedOn
            , @LiveLatestStatus
    )
    BEGIN
        SET @LiveDiff += 1;
        SET @Fail += 1;
        IF @Unsafe = 1 OR @ScriptError IS NOT NULL
            PRINT N'DAMAGE ASSESSMENT | request, acted detail, and history counts differ from the pre-image | not rollback proof';
        ELSE
            PRINT N'CHECK live request, acted detail, and history counts match the pre-image | FAIL';
    END
    ELSE IF @Unsafe = 1 OR @ScriptError IS NOT NULL
        PRINT N'DAMAGE ASSESSMENT | request, acted detail, and history counts match the pre-image | not rollback proof';
    ELSE
        PRINT N'CHECK live request, acted detail, and history counts match the pre-image | PASS';

    IF EXISTS (
            SELECT
                  d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
                , d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                , d.DetailVersion, d.PreviousDetailId
                , d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
            FROM @PreDetail d
            EXCEPT
            SELECT
                  d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
                , d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                , d.DetailVersion, d.PreviousDetailId
                , d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
            FROM dbo.SalesApprovalRequestDetail d
            WHERE d.ApprovalRequestId = @ApprovalRequestId
        )
        OR EXISTS (
            SELECT
                  d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
                , d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                , d.DetailVersion, d.PreviousDetailId
                , d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
            FROM dbo.SalesApprovalRequestDetail d
            WHERE d.ApprovalRequestId = @ApprovalRequestId
            EXCEPT
            SELECT
                  d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
                , d.DetailStatus, d.DetailRemarks, d.DetailApprovedBy, d.DetailApprovedOn, d.DetailApprovedLevel
                , d.DetailVersion, d.PreviousDetailId
                , d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount
                , d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount
            FROM @PreDetail d
        )
    BEGIN
        SET @LiveDiff += 1;
        SET @Fail += 1;
        IF @Unsafe = 1 OR @ScriptError IS NOT NULL
            PRINT N'DAMAGE ASSESSMENT | detail rows differ from the pre-image | not rollback proof';
        ELSE
            PRINT N'CHECK live detail rows match the pre-image | FAIL';
    END
    ELSE IF @Unsafe = 1 OR @ScriptError IS NOT NULL
        PRINT N'DAMAGE ASSESSMENT | detail rows match the pre-image | not rollback proof';
    ELSE
        PRINT N'CHECK live detail rows match the pre-image | PASS';

    IF EXISTS (
            SELECT
                  a.ApprovalRequestApproverId, a.ApproverLoginId, a.ApproverRole, a.ApproverLevel
                , a.IsRequired, a.Status, a.ActionBy, a.ActionDate, a.Remarks
            FROM @PreApprover a
            EXCEPT
            SELECT
                  a.ApprovalRequestApproverId, a.ApproverLoginId, a.ApproverRole, a.ApproverLevel
                , a.IsRequired, a.Status, a.ActionBy, a.ActionDate, a.Remarks
            FROM dbo.SalesApprovalRequestApprover a
            WHERE a.ApprovalRequestId = @ApprovalRequestId
        )
        OR EXISTS (
            SELECT
                  a.ApprovalRequestApproverId, a.ApproverLoginId, a.ApproverRole, a.ApproverLevel
                , a.IsRequired, a.Status, a.ActionBy, a.ActionDate, a.Remarks
            FROM dbo.SalesApprovalRequestApprover a
            WHERE a.ApprovalRequestId = @ApprovalRequestId
            EXCEPT
            SELECT
                  a.ApprovalRequestApproverId, a.ApproverLoginId, a.ApproverRole, a.ApproverLevel
                , a.IsRequired, a.Status, a.ActionBy, a.ActionDate, a.Remarks
            FROM @PreApprover a
        )
    BEGIN
        SET @LiveDiff += 1;
        SET @Fail += 1;
        IF @Unsafe = 1 OR @ScriptError IS NOT NULL
            PRINT N'DAMAGE ASSESSMENT | approver rows differ from the pre-image | not rollback proof';
        ELSE
            PRINT N'CHECK live approver rows match the pre-image | FAIL';
    END
    ELSE IF @Unsafe = 1 OR @ScriptError IS NOT NULL
        PRINT N'DAMAGE ASSESSMENT | approver rows match the pre-image | not rollback proof';
    ELSE
        PRINT N'CHECK live approver rows match the pre-image | PASS';

    IF EXISTS (
            SELECT
                  h.HistoryId, h.OldStatus, h.NewStatus, h.ChangedBy, h.ChangedOn, h.Remarks
                , h.ApprovalRequestDetailId, h.ActivityId, h.ApproverLevel, h.ActionType, h.DetailVersion
            FROM @PreHistory h
            EXCEPT
            SELECT
                  h.HistoryId, h.OldStatus, h.NewStatus, h.ChangedBy, h.ChangedOn, h.Remarks
                , h.ApprovalRequestDetailId, h.ActivityId, h.ApproverLevel, h.ActionType, h.DetailVersion
            FROM dbo.ApprovalRequestStatusHistory h
            WHERE h.ApprovalRequestId = @ApprovalRequestId
        )
        OR EXISTS (
            SELECT
                  h.HistoryId, h.OldStatus, h.NewStatus, h.ChangedBy, h.ChangedOn, h.Remarks
                , h.ApprovalRequestDetailId, h.ActivityId, h.ApproverLevel, h.ActionType, h.DetailVersion
            FROM dbo.ApprovalRequestStatusHistory h
            WHERE h.ApprovalRequestId = @ApprovalRequestId
            EXCEPT
            SELECT
                  h.HistoryId, h.OldStatus, h.NewStatus, h.ChangedBy, h.ChangedOn, h.Remarks
                , h.ApprovalRequestDetailId, h.ActivityId, h.ApproverLevel, h.ActionType, h.DetailVersion
            FROM @PreHistory h
        )
    BEGIN
        SET @LiveDiff += 1;
        SET @Fail += 1;
        IF @Unsafe = 1 OR @ScriptError IS NOT NULL
            PRINT N'DAMAGE ASSESSMENT | history rows differ from the pre-image | not rollback proof';
        ELSE
            PRINT N'CHECK live history rows match the pre-image | FAIL';
    END
    ELSE IF @Unsafe = 1 OR @ScriptError IS NOT NULL
        PRINT N'DAMAGE ASSESSMENT | history rows match the pre-image | not rollback proof';
    ELSE
        PRINT N'CHECK live history rows match the pre-image | PASS';

    IF EXISTS (
            SELECT
                  p.ActivityId, p.ClaimStatus, p.User_Id, p.Login_Id, p.ApprovalRequestId, p.IsDeleted
                , p.ModifiedByUserId, p.ModifiedDateTime, p.ClaimProcessedBy, p.ClaimProcessedDate
                , p.SubmittedBy, p.SubmittedOn, p.RowVersion
            FROM @PreActivity p
            EXCEPT
            SELECT
                  a.ActivityId, a.ClaimStatus, a.User_Id, a.Login_Id, a.ApprovalRequestId, a.IsDeleted
                , a.ModifiedByUserId, a.ModifiedDateTime, a.ClaimProcessedBy, a.ClaimProcessedDate
                , a.SubmittedBy, a.SubmittedOn, CONVERT(varbinary(8), a.RowVersion)
            FROM dbo.SalesDailyActivity a
            WHERE a.ActivityId = @ActivityId
        )
        OR EXISTS (
            SELECT
                  a.ActivityId, a.ClaimStatus, a.User_Id, a.Login_Id, a.ApprovalRequestId, a.IsDeleted
                , a.ModifiedByUserId, a.ModifiedDateTime, a.ClaimProcessedBy, a.ClaimProcessedDate
                , a.SubmittedBy, a.SubmittedOn, CONVERT(varbinary(8), a.RowVersion)
            FROM dbo.SalesDailyActivity a
            WHERE a.ActivityId = @ActivityId
            EXCEPT
            SELECT
                  p.ActivityId, p.ClaimStatus, p.User_Id, p.Login_Id, p.ApprovalRequestId, p.IsDeleted
                , p.ModifiedByUserId, p.ModifiedDateTime, p.ClaimProcessedBy, p.ClaimProcessedDate
                , p.SubmittedBy, p.SubmittedOn, p.RowVersion
            FROM @PreActivity p
        )
    BEGIN
        SET @LiveDiff += 1;
        SET @Fail += 1;
        IF @Unsafe = 1 OR @ScriptError IS NOT NULL
            PRINT N'DAMAGE ASSESSMENT | activity row differs from the pre-image | not rollback proof';
        ELSE
            PRINT N'CHECK live activity row matches the pre-image | FAIL';
    END
    ELSE IF @Unsafe = 1 OR @ScriptError IS NOT NULL
        PRINT N'DAMAGE ASSESSMENT | activity row matches the pre-image | not rollback proof';
    ELSE
        PRINT N'CHECK live activity row matches the pre-image | PASS';

    IF EXISTS (
            SELECT
                  p.ExpenseId, p.ActivityId, p.ClaimStatus, p.IsDeleted, p.ClaimProcessedBy, p.ClaimProcessedDate
                , p.ModifiedBy, p.ModifiedOn, p.Remarks, p.CreatedBy, p.DeletedBy, p.DeletedOn
            FROM @PreExpense p
            EXCEPT
            SELECT
                  e.ExpenseId, e.ActivityId, e.ClaimStatus, e.IsDeleted, e.ClaimProcessedBy, e.ClaimProcessedDate
                , e.ModifiedBy, e.ModifiedOn, e.Remarks, e.CreatedBy, e.DeletedBy, e.DeletedOn
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = @ActivityId
        )
        OR EXISTS (
            SELECT
                  e.ExpenseId, e.ActivityId, e.ClaimStatus, e.IsDeleted, e.ClaimProcessedBy, e.ClaimProcessedDate
                , e.ModifiedBy, e.ModifiedOn, e.Remarks, e.CreatedBy, e.DeletedBy, e.DeletedOn
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = @ActivityId
            EXCEPT
            SELECT
                  p.ExpenseId, p.ActivityId, p.ClaimStatus, p.IsDeleted, p.ClaimProcessedBy, p.ClaimProcessedDate
                , p.ModifiedBy, p.ModifiedOn, p.Remarks, p.CreatedBy, p.DeletedBy, p.DeletedOn
            FROM @PreExpense p
        )
    BEGIN
        SET @LiveDiff += 1;
        SET @Fail += 1;
        IF @Unsafe = 1 OR @ScriptError IS NOT NULL
            PRINT N'DAMAGE ASSESSMENT | expense rows differ from the pre-image | not rollback proof';
        ELSE
            PRINT N'CHECK live expense rows match the pre-image | FAIL';
    END
    ELSE IF @Unsafe = 1 OR @ScriptError IS NOT NULL
        PRINT N'DAMAGE ASSESSMENT | expense rows match the pre-image | not rollback proof';
    ELSE
        PRINT N'CHECK live expense rows match the pre-image | PASS';

    IF @LiveDiff = 0
        SET @LiveMatch = 1;

    IF @Unsafe = 1 AND @UnexpectedCommit = 1 AND @LiveMatch = 0
        PRINT N'BLOCKED | pre-image does not match live data after a non-error return cleared @@TRANCOUNT. Treat the request as possibly changed.';
    ELSE IF @Unsafe = 1 AND @UnexpectedCommit = 1 AND @LiveMatch = 1
        PRINT N'DAMAGE ASSESSMENT | live rows match the pre-image after @@TRANCOUNT returned 0 | match is not rollback proof | inspect the request before reuse';
END
ELSE IF @DidExec = 1
BEGIN
    PRINT N'BLOCKED | live pre-image comparison skipped because a transaction is still open or the pre-image was not captured.';
    SET @Outcome = N'BLOCKED';
    SET @Unsafe = 1;
    SET @Fail += 1;
END;

IF @Unsafe = 0
   AND @Outcome <> N'BLOCKED'
   AND @ScriptError IS NULL
   AND @@TRANCOUNT = 0
   AND @RollbackIssued = 1
   AND @AcceptedWriteObserved = 1
   AND @ExpenseEligibleCount > 0
   AND @LiveMatch = 1
   AND @Fail = 0
   AND @ProcError = 0
   AND @ExpectError = 0
   AND @DidExec = 1
   AND @TranCountAfter = 1
   AND @XactStateAfter = 1
    SET @RollbackProved = 1;

IF @Unsafe = 0
   AND @Outcome <> N'BLOCKED'
   AND @ScriptError IS NULL
   AND @@TRANCOUNT = 0
   AND @RollbackIssued = 1
   AND @InTranUnchanged = 1
   AND @LiveMatch = 1
   AND @Fail = 0
   AND @ProcError = 1
   AND @ExpectError = 1
   AND @DidExec = 1
   AND @TranCountAfter = 1
   AND @XactStateAfter = 1
    SET @RejectedBeforeWrite = 1;

IF @RollbackProved = 1
    PRINT N'INFO | rollback proof requires the observed accepted write plus the harness rollback. eligibleExpenseWrites='
        + CONVERT(nvarchar(12), @ExpenseEligibleCount);
IF @RejectedBeforeWrite = 1
    PRINT N'INFO | pre-image match after an expected rejection is not rollback proof.';

PRINT N'UNVERIFIED | no C# override | SQL cannot see a later C# write';
PRINT N'UNVERIFIED | Employee cannot skip L1 | this procedure does not submit a claim';
PRINT N'UNVERIFIED | universal L1 cannot skip Auditor | one request cannot prove every request has an Auditor';
PRINT N'G4-G7 | NOT AUTHORIZED';

IF @Unsafe = 1
    PRINT N'RC014B behavior | RESULT | UNSAFE | inconclusive | not rollback proof | fails=' + CONVERT(nvarchar(12), @Fail);
ELSE IF @Outcome = N'BLOCKED'
    PRINT N'RC014B behavior | RESULT | BLOCKED | fails=' + CONVERT(nvarchar(12), @Fail);
ELSE IF @RollbackProved = 1
    PRINT N'RC014B behavior | RESULT | ROLLBACK PROVED | accepted write restored by harness rollback | eligible expense UPDATE observed | eligibleExpenseWrites='
        + CONVERT(nvarchar(12), @ExpenseEligibleCount) + N' | fails=0';
ELSE IF @RejectedBeforeWrite = 1
    PRINT N'RC014B behavior | RESULT | REJECTED BEFORE WRITE | not rollback proof | fails=0';
ELSE IF @DidExec = 1
   AND @Unsafe = 0
   AND @Outcome <> N'BLOCKED'
   AND @AcceptedWriteObserved = 1
   AND @ExpenseEligibleCount = 0
   AND @LiveMatch = 1
   AND @Fail = 0
   AND @RollbackIssued = 1
    PRINT N'RC014B behavior | RESULT | INCONCLUSIVE | activity and detail write restored | expense UPDATE not observed | not full rollback proof | eligibleExpenseWrites=0 | fails=0';
ELSE IF @Fail > 0
    PRINT N'RC014B behavior | RESULT | FAIL | fails=' + CONVERT(nvarchar(12), @Fail);
ELSE IF @DidExec = 1
    PRINT N'RC014B behavior | RESULT | INCONCLUSIVE | not rollback proof | fails=' + CONVERT(nvarchar(12), @Fail);
ELSE
    PRINT N'RC014B behavior | RESULT | ' + @Outcome + N' | fails=' + CONVERT(nvarchar(12), @Fail);
