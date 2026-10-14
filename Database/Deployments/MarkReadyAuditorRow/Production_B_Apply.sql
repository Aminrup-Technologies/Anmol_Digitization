/*
    Package:     Database/Deployments/MarkReadyAuditorRow
    Target:      AnmolDristi on MUMANMOAPP01\SQLEXPRESS only.
    Refuses:     AnmolDristiDev and every other database name.
    Data:        No claim, approver, history, detail, or expense row is written.
    Requests:    100, 121, 130, and 131 are not referenced as update targets.

    Prepared 2026-10-14 from a read-back. @Execute defaults to 0 where a write
    exists. Generating this file did not execute it.

    Production preimage, read from AnmolDristi:
      object_id 1860917701
      modify_date 2026-10-08 12:46:41.807
      LEN 2223 / DATALENGTH 4446
      SHA2_256 6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451
      File: usp_MarkRequestReadyForPayment_AnmolDristi_preimage.txt
      Do not execute that file.

    UAT-verified stored form, read from AnmolDristiDev:
      modify_date 2026-10-13 14:11:49.980
      LEN 11181 / DATALENGTH 22362
      SHA2_256 E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409
      Repository source uses CREATE OR ALTER. SQL Server stores CREATE plus
      three spaces. That is the only repository-to-UAT text difference.
*/

/*
    Script:      Production_B_Apply.sql
    Mode:        PREPARED. @Execute defaults to 0 and returns before any change.
    This replaces the pinned Production procedure with the UAT-verified body.
    It does not copy UAT claim rows.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Execute bit = 0;
IF @Execute <> 1
BEGIN
    PRINT N'NOT RUN | @Execute is 0. Production procedure was not changed.';
    RETURN;
END;

BEGIN TRY
    IF @@TRANCOUNT <> 0
        THROW 58410, N'ABORT: this script must start with no open transaction.', 1;
    IF DB_NAME() = N'AnmolDristiDev'
        THROW 58401, N'ABORT: AnmolDristiDev is not the Production target.', 1;
    IF DB_NAME() <> N'AnmolDristi'
        THROW 58402, N'ABORT: connected database is not AnmolDristi.', 1;
    IF UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
        THROW 58403, N'ABORT: server is not MUMANMOAPP01\SQLEXPRESS.', 1;

    DECLARE @ObjectId int = OBJECT_ID(N'dbo.usp_MarkRequestReadyForPayment', N'P');
    IF @ObjectId IS NULL OR @ObjectId <> 1860917701
        THROW 58404, N'ABORT: procedure object id is not 1860917701.', 1;

    DECLARE @Definition nvarchar(max);
    DECLARE @ModifyDate datetime;
    DECLARE @UsesAnsiNulls bit;
    DECLARE @UsesQuotedIdentifier bit;
    SELECT
        @Definition = m.definition,
        @ModifyDate = o.modify_date,
        @UsesAnsiNulls = m.uses_ansi_nulls,
        @UsesQuotedIdentifier = m.uses_quoted_identifier
    FROM sys.sql_modules AS m
    INNER JOIN sys.objects AS o ON o.object_id = m.object_id
    WHERE m.object_id = @ObjectId;

    IF ISNULL(@UsesAnsiNulls, 0) <> 1 OR ISNULL(@UsesQuotedIdentifier, 0) <> 1
        THROW 58411, N'ABORT: ANSI_NULLS or QUOTED_IDENTIFIER is not ON.', 1;
    IF LEN(@Definition) <> 2223 OR DATALENGTH(@Definition) <> 4446
        THROW 58405, N'ABORT: Production definition length is not 2223 / 4446.', 1;
    IF CONVERT(varchar(23), @ModifyDate, 121) <> '2026-10-08 12:46:41.807'
        THROW 58406, N'ABORT: Production modify_date is not 2026-10-08 12:46:41.807.', 1;

    DECLARE @Hash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Definition)), 2);
    IF @Hash <> '6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451'
        THROW 58407, N'ABORT: Production definition hash is not the pinned preimage.', 1;

    DECLARE @Next nvarchar(max) = N'/* ============================================================================
   NAME:        l2r19_transactional_finalization_candidate
   WHEN:        2026-10-13
   WHY:         ReadyForPayment recalculated, validated, and wrote status on
                separate connections, so approved totals could go stale.
                A Pending request could also be finalized by the assigned
                Auditor while the queue owner was still L1.
   WHAT:        One transaction locks children before the header, recalculates,
                rechecks unresolved latest rows, then writes Completed, the
                Auditor row when it is still Pending, and ReadyForPayment.
                A Pending request is refused unless ActionBy is the current
                approver (error 59112). Completed is unchanged.
                Review candidate only. Not authorized for UAT or LIVE execution.
   ============================================================================ */

/*
    Object:      [dbo].[usp_MarkRequestReadyForPayment]
    Type:        STORED PROCEDURE
    Candidate:   L2-R19 review source. Do not deploy from this file until the
                 release owner accepts it and an authorized UAT run passes.

    Executable contract, read 2026-10-13, not from comments:
      Repository and UAT dbo.usp_ProcessApprovalAction exclude the final
      Auditor from the approver Approved write and leave the header Pending.
      LIVE dbo.usp_ProcessApprovalAction approves that Pending Auditor row
      and sets the header to Completed, clearing CurrentApproverLevel.
    This procedure performs the Auditor Pending -> Approved write once.
    An already Approved Auditor row is left unchanged. Any other status fails.

    Lock order is detail, non-deleted expense, Auditor approver, header.
    That follows usp_ProcessApprovalAction, which writes the detail before
    the approver and the header. Index hints match indexes present on both
    AnmolDristiDev and AnmolDristi on 2026-10-13.

    dbo.usp_RecalculateApprovalRequest is called after those locks.
    It does not begin its own transaction. Its financial formula is unchanged.
*/

CREATE   PROCEDURE dbo.usp_MarkRequestReadyForPayment
(
    @ApprovalRequestId INT,
    @ActionBy NVARCHAR(100),
    @Remarks NVARCHAR(MAX) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ApprovalRequestId IS NULL
        THROW 59101, ''ApprovalRequestId cannot be NULL.'', 1;

    IF NULLIF(LTRIM(RTRIM(@ActionBy)), N'''') IS NULL
        THROW 59102, ''ActionBy is required.'', 1;

    DECLARE @NowUtc DATETIME2(0) = SYSUTCDATETIME();
    DECLARE @Status NVARCHAR(50);
    DECLARE @CurrentLevel INT;
    DECLARE @CurrentApproverLoginId NVARCHAR(100);
    DECLARE @AuditorCount INT;
    DECLARE @AuditorPendingCount INT;
    DECLARE @AuditorApprovedCount INT;
    DECLARE @LockProbe BIGINT;
    DECLARE @UnresolvedCount INT;
    DECLARE @CompletedRows INT;
    DECLARE @ReadyRows INT;
    DECLARE @AuditorRows INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        /* 1. Detail rows, including the range of this request id. */
        SELECT @LockProbe = COUNT_BIG(*)
        FROM dbo.SalesApprovalRequestDetail WITH
        (
            UPDLOCK,
            HOLDLOCK,
            INDEX(IX_SalesApprovalRequestDetail_RequestId)
        )
        WHERE ApprovalRequestId = @ApprovalRequestId;

        /* 2. Non-deleted expenses for those activities. */
        SELECT @LockProbe = COUNT_BIG(*)
        FROM dbo.SalesDailyExpense AS e WITH
        (
            UPDLOCK,
            HOLDLOCK,
            INDEX(UQ_SalesDailyExpense_ActivityDateExpenseType_Seq)
        )
        INNER JOIN
        (
            SELECT DISTINCT d.ActivityId
            FROM dbo.SalesApprovalRequestDetail AS d
            WHERE d.ApprovalRequestId = @ApprovalRequestId
        ) AS act
            ON act.ActivityId = e.ActivityId
        WHERE e.IsDeleted = 0;

        /* 3. The one Auditor assignment. Filtered unique index. */
        SELECT
              @AuditorCount = COUNT(*)
            , @AuditorPendingCount = SUM(CASE WHEN a.Status = N''Pending'' THEN 1 ELSE 0 END)
            , @AuditorApprovedCount = SUM(CASE WHEN a.Status = N''Approved'' THEN 1 ELSE 0 END)
        FROM dbo.SalesApprovalRequestApprover AS a WITH
        (
            UPDLOCK,
            HOLDLOCK,
            INDEX(UX_SAR_Auditor_Once)
        )
        WHERE a.ApprovalRequestId = @ApprovalRequestId
          AND a.ApproverRole = N''Auditor''
          AND a.ApproverLoginId = @ActionBy;

        /* 4. Header last, after the child rows the other writers touch first. */
        SELECT
              @Status = r.Status
            , @CurrentLevel = ISNULL(r.CurrentApproverLevel, 0)
            , @CurrentApproverLoginId = r.CurrentApproverLoginId
        FROM dbo.SalesApprovalRequest AS r WITH (UPDLOCK, HOLDLOCK)
        WHERE r.ApprovalRequestId = @ApprovalRequestId;

        IF @Status IS NULL
            THROW 59103, ''Approval request was not found.'', 1;

        IF @Status = N''ReadyForPayment''
            THROW 59104, ''This approval request is already Ready for Payment.'', 1;

        IF @Status NOT IN (N''Pending'', N''Completed'')
            THROW 59105, ''Only a Pending or Completed request can be marked Ready for Payment.'', 1;

        IF @Status = N''Pending'' AND @CurrentLevel < 1
            THROW 59108, ''A Pending request has no current approver level, so finalization cannot apply the level rule.'', 1;

        /* Pending still has a queue owner. Completed may already have cleared that pointer. */
        IF @Status = N''Pending''
           AND UPPER(LTRIM(RTRIM(ISNULL(@CurrentApproverLoginId, N'''')))) <> UPPER(LTRIM(RTRIM(@ActionBy)))
            THROW 59112, ''Only the current approver can mark a Pending request Ready for Payment.'', 1;

        IF ISNULL(@AuditorCount, 0) <> 1
            THROW 59106, ''Only the assigned Auditor can mark Ready for Payment.'', 1;

        IF ISNULL(@AuditorPendingCount, 0) <> 1 AND ISNULL(@AuditorApprovedCount, 0) <> 1
            THROW 59107, ''The Auditor assignment must be Pending or already Approved.'', 1;

        EXEC dbo.usp_RecalculateApprovalRequest
            @ApprovalRequestId = @ApprovalRequestId;

        ;WITH Latest AS
        (
            SELECT MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
            FROM dbo.SalesApprovalRequestDetail AS d2
            INNER JOIN
            (
                SELECT
                      ActivityId
                    , MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
                FROM dbo.SalesApprovalRequestDetail
                WHERE ApprovalRequestId = @ApprovalRequestId
                GROUP BY ActivityId
            ) AS lv
                ON lv.ActivityId = d2.ActivityId
               AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
            WHERE d2.ApprovalRequestId = @ApprovalRequestId
            GROUP BY d2.ActivityId
        )
        SELECT @UnresolvedCount =
              (
                  SELECT COUNT(*)
                  FROM dbo.SalesApprovalRequestDetail AS d
                  INNER JOIN Latest AS l
                      ON d.ApprovalRequestDetailId = l.LatestDetailId
                  WHERE d.ApprovalRequestId = @ApprovalRequestId
                    AND (
                            ISNULL(d.DetailStatus, N''Pending'') NOT IN (N''Approved'', N''Rejected'')
                         OR (
                                @CurrentLevel >= 1
                            AND ISNULL(d.DetailStatus, N'''') = N''Approved''
                            AND ISNULL(d.DetailApprovedLevel, 0) < @CurrentLevel
                            )
                        )
              )
            + (
                  SELECT COUNT(*)
                  FROM dbo.SalesDailyExpense AS e
                  INNER JOIN dbo.SalesApprovalRequestDetail AS d
                      ON e.ActivityId = d.ActivityId
                  INNER JOIN Latest AS l
                      ON d.ApprovalRequestDetailId = l.LatestDetailId
                  WHERE d.ApprovalRequestId = @ApprovalRequestId
                    AND e.IsDeleted = 0
                    AND ISNULL(e.ClaimStatus, N'''') NOT IN (N''Approved'', N''Rejected'')
              );

        IF ISNULL(@UnresolvedCount, 0) > 0
            THROW 59109, ''All latest day rows and expense lines must be resolved at the current approval level before finalization.'', 1;

        IF ISNULL(@AuditorPendingCount, 0) = 1
        BEGIN
            UPDATE dbo.SalesApprovalRequestApprover
            SET
                  Status     = N''Approved''
                , ActionBy   = @ActionBy
                , ActionDate = @NowUtc
                , Remarks    = ISNULL(@Remarks, Remarks)
            WHERE ApprovalRequestId = @ApprovalRequestId
              AND ApproverLoginId = @ActionBy
              AND ApproverRole = N''Auditor''
              AND Status = N''Pending'';

            SET @AuditorRows = @@ROWCOUNT;

            IF @AuditorRows <> 1
                THROW 59110, ''The Pending Auditor assignment could not be approved.'', 1;
        END

        UPDATE dbo.SalesApprovalRequest
        SET
              Status     = N''Completed''
            , ModifiedBy = @ActionBy
            , ModifiedOn = @NowUtc
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND Status = N''Pending'';

        SET @CompletedRows = @@ROWCOUNT;

        IF @CompletedRows > 0
        BEGIN
            INSERT INTO dbo.ApprovalRequestStatusHistory
            (
                  ApprovalRequestId
                , OldStatus
                , NewStatus
                , ChangedBy
                , Remarks
            )
            VALUES
            (
                  @ApprovalRequestId
                , N''Pending''
                , N''Completed''
                , @ActionBy
                , N''Auditor finalized all rows before Ready for Payment.''
            );
        END

        UPDATE dbo.SalesApprovalRequest
        SET
              Status     = N''ReadyForPayment''
            , ModifiedBy = @ActionBy
            , ModifiedOn = @NowUtc
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND Status = N''Completed'';

        SET @ReadyRows = @@ROWCOUNT;

        IF @ReadyRows <> 1
            THROW 59111, ''Ready for Payment was not written because the request was no longer Completed.'', 1;

        INSERT INTO dbo.ApprovalRequestStatusHistory
        (
              ApprovalRequestId
            , OldStatus
            , NewStatus
            , ChangedBy
            , Remarks
        )
        VALUES
        (
              @ApprovalRequestId
            , N''Completed''
            , N''ReadyForPayment''
            , @ActionBy
            , N''Auditor marked Ready for Payment''
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
';
    IF LEN(@Next) <> 11181 OR DATALENGTH(@Next) <> 22362
        THROW 58412, N'ABORT: certified text length is not 11181 / 22362.', 1;

    DECLARE @NextHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Next)), 2);
    IF @NextHash <> 'E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409'
        THROW 58413, N'ABORT: certified text hash is not the UAT-verified module.', 1;

    IF CHARINDEX(N'THROW 59110', @Next) = 0
        OR CHARINDEX(N'ActionDate = @NowUtc', @Next) = 0
        OR CHARINDEX(N'ApproverRole = N''Auditor''', @Next) = 0
        OR CHARINDEX(N'Status = N''Pending''', @Next) = 0
        THROW 58414, N'ABORT: certified text does not contain the Auditor assignment update.', 1;

    DECLARE @CreateAt int = CHARINDEX(N'CREATE   PROCEDURE', @Next);
    IF @CreateAt < 1
        THROW 58415, N'ABORT: CREATE token was not found in the certified text.', 1;

    DECLARE @Alter nvarchar(max) = STUFF(@Next, @CreateAt, LEN(N'CREATE   PROCEDURE'), N'ALTER   PROCEDURE');

    BEGIN TRAN;
    EXEC (@Alter);

    DECLARE @Stored nvarchar(max) = OBJECT_DEFINITION(@ObjectId);
    IF @Stored IS NULL OR @Stored <> @Next
    BEGIN
        ROLLBACK TRAN;
        THROW 58416, N'ABORT: stored definition does not equal the UAT-verified text. Rolled back.', 1;
    END;

    COMMIT TRAN;
    PRINT N'COMMITTED | Production procedure matches E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;
    THROW;
END CATCH;