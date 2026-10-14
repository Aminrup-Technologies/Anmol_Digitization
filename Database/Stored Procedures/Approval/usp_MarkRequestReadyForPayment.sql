/* ============================================================================
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

CREATE OR ALTER PROCEDURE dbo.usp_MarkRequestReadyForPayment
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
        THROW 59101, 'ApprovalRequestId cannot be NULL.', 1;

    IF NULLIF(LTRIM(RTRIM(@ActionBy)), N'') IS NULL
        THROW 59102, 'ActionBy is required.', 1;

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
            , @AuditorPendingCount = SUM(CASE WHEN a.Status = N'Pending' THEN 1 ELSE 0 END)
            , @AuditorApprovedCount = SUM(CASE WHEN a.Status = N'Approved' THEN 1 ELSE 0 END)
        FROM dbo.SalesApprovalRequestApprover AS a WITH
        (
            UPDLOCK,
            HOLDLOCK,
            INDEX(UX_SAR_Auditor_Once)
        )
        WHERE a.ApprovalRequestId = @ApprovalRequestId
          AND a.ApproverRole = N'Auditor'
          AND a.ApproverLoginId = @ActionBy;

        /* 4. Header last, after the child rows the other writers touch first. */
        SELECT
              @Status = r.Status
            , @CurrentLevel = ISNULL(r.CurrentApproverLevel, 0)
            , @CurrentApproverLoginId = r.CurrentApproverLoginId
        FROM dbo.SalesApprovalRequest AS r WITH (UPDLOCK, HOLDLOCK)
        WHERE r.ApprovalRequestId = @ApprovalRequestId;

        IF @Status IS NULL
            THROW 59103, 'Approval request was not found.', 1;

        IF @Status = N'ReadyForPayment'
            THROW 59104, 'This approval request is already Ready for Payment.', 1;

        IF @Status NOT IN (N'Pending', N'Completed')
            THROW 59105, 'Only a Pending or Completed request can be marked Ready for Payment.', 1;

        IF @Status = N'Pending' AND @CurrentLevel < 1
            THROW 59108, 'A Pending request has no current approver level, so finalization cannot apply the level rule.', 1;

        /* Pending still has a queue owner. Completed may already have cleared that pointer. */
        IF @Status = N'Pending'
           AND UPPER(LTRIM(RTRIM(ISNULL(@CurrentApproverLoginId, N'')))) <> UPPER(LTRIM(RTRIM(@ActionBy)))
            THROW 59112, 'Only the current approver can mark a Pending request Ready for Payment.', 1;

        IF ISNULL(@AuditorCount, 0) <> 1
            THROW 59106, 'Only the assigned Auditor can mark Ready for Payment.', 1;

        IF ISNULL(@AuditorPendingCount, 0) <> 1 AND ISNULL(@AuditorApprovedCount, 0) <> 1
            THROW 59107, 'The Auditor assignment must be Pending or already Approved.', 1;

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
                            ISNULL(d.DetailStatus, N'Pending') NOT IN (N'Approved', N'Rejected')
                         OR (
                                @CurrentLevel >= 1
                            AND ISNULL(d.DetailStatus, N'') = N'Approved'
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
                    AND ISNULL(e.ClaimStatus, N'') NOT IN (N'Approved', N'Rejected')
              );

        IF ISNULL(@UnresolvedCount, 0) > 0
            THROW 59109, 'All latest day rows and expense lines must be resolved at the current approval level before finalization.', 1;

        IF ISNULL(@AuditorPendingCount, 0) = 1
        BEGIN
            UPDATE dbo.SalesApprovalRequestApprover
            SET
                  Status     = N'Approved'
                , ActionBy   = @ActionBy
                , ActionDate = @NowUtc
                , Remarks    = ISNULL(@Remarks, Remarks)
            WHERE ApprovalRequestId = @ApprovalRequestId
              AND ApproverLoginId = @ActionBy
              AND ApproverRole = N'Auditor'
              AND Status = N'Pending';

            SET @AuditorRows = @@ROWCOUNT;

            IF @AuditorRows <> 1
                THROW 59110, 'The Pending Auditor assignment could not be approved.', 1;
        END

        UPDATE dbo.SalesApprovalRequest
        SET
              Status     = N'Completed'
            , ModifiedBy = @ActionBy
            , ModifiedOn = @NowUtc
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND Status = N'Pending';

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
                , N'Pending'
                , N'Completed'
                , @ActionBy
                , N'Auditor finalized all rows before Ready for Payment.'
            );
        END

        UPDATE dbo.SalesApprovalRequest
        SET
              Status     = N'ReadyForPayment'
            , ModifiedBy = @ActionBy
            , ModifiedOn = @NowUtc
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND Status = N'Completed';

        SET @ReadyRows = @@ROWCOUNT;

        IF @ReadyRows <> 1
            THROW 59111, 'Ready for Payment was not written because the request was no longer Completed.', 1;

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
            , N'Completed'
            , N'ReadyForPayment'
            , @ActionBy
            , N'Auditor marked Ready for Payment'
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
