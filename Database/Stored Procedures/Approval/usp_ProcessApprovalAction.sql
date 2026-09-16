/* ============================================================================
   NAME:        auditor_unresolved_uses_approval_level
   WHEN:        2026-09-15
   WHY:         After L1 Forward, latest rows are Approved at level 1 so Pending/Submitted count was 0 and first Auditor approve wrote Completed
   WHAT:        Unresolved latest rows include Approved with DetailApprovedLevel < current approver level (same as ClaimsApproval_v2 gatekeeper)
   ============================================================================ */

/* ============================================================================
   NAME:        auditor_terminal_completed_status
   WHEN:        2026-09-15
   WHY:         Last Auditor approve left header Pending so ReadyForPayment was unreachable
   WHAT:        usp_ProcessApprovalAction Auditor terminal branch writes Completed and one history row
   ============================================================================ */

/*
    Object:      [dbo].[usp_ProcessApprovalAction]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

/*
    Sprint 2 - Task 01 (Approval Action Hardening)
    Change: Added authorization and state guards required by the approved
            Functional Specification and Technical Design Specification.
            This procedure remains responsible only for processing a single
            day-level approval action (APPROVE / RETURN / REJECT).

              - Current Approver validation (@ActionBy must match the
                header's CurrentApproverLoginId).
              - Header status validation (action permitted only while the
                request is actively Pending).
              - Payment Lock (a Completed request is immutable; no action
                of any kind is processed against it).
              - Role enforcement for day-level actions:
                  Manager : may Approve or Return a day. Must NOT Reject.
                  Auditor : may Approve or Reject a day. Must NOT Return.
                (Expense-level Approve/Return/Reject rules belong to
                usp_UpdateSalesDailyExpenseStatus and are out of scope
                for this procedure.)

            No workflow routing, forwarding, ReadyForPayment, or Completed
            logic was removed. The existing routing responsibilities this
            procedure currently performs are marked in place with
            TODO (Sprint 2 - Task 02) comments and remain fully functional
            until that responsibility is migrated to
            usp_ForwardApprovalRequest, so that the workflow does not break
            between the two tasks' merges.

            No totals, expense, distance, or policy calculation logic was
            modified. No schema changes were made. No other procedure was
            modified.
*/

CREATE PROCEDURE dbo.usp_ProcessApprovalAction
(
      @ApprovalRequestDetailId INT
    , @Action                  NVARCHAR(20)   -- APPROVE | RETURN | REJECT
    , @ActionBy                NVARCHAR(100)
    , @Remarks                 NVARCHAR(MAX) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
          @NormalizedAction       NVARCHAR(20)
        , @ApprovalRequestId      INT
        , @ActivityId             INT
        , @CurrentApproverLevel   INT
        , @CurrentApproverLoginId NVARCHAR(100)
        , @OldRequestStatus       NVARCHAR(50)
        , @NewRequestStatus       NVARCHAR(50)
        , @CurrentApproverRole    NVARCHAR(100)
        , @MappedDetailStatus     NVARCHAR(50)
        , @MappedActivityStatus   NVARCHAR(50)
        , @NowUtc                 DATETIME2(3) = SYSUTCDATETIME()
        , @NowIst                 DATETIME2(3) = DATEADD(MINUTE, 330, SYSUTCDATETIME())
        , @LatestDetailCount      INT = 0
        , @PendingLatestCount     INT = 0
        , @ReturnedLatestCount    INT = 0
        , @RejectedLatestCount    INT = 0
        , @ApprovedLatestCount    INT = 0
        , @NextApproverLevel      INT
        , @NextApproverLoginId    NVARCHAR(100)
        , @NextApproverRole       NVARCHAR(100);

    SET @NormalizedAction = UPPER(LTRIM(RTRIM(ISNULL(@Action, ''))));

    IF @NormalizedAction NOT IN ('APPROVE', 'RETURN', 'REJECT')
    BEGIN
        RAISERROR('Invalid action. Expected APPROVE, RETURN, or REJECT.', 16, 1);
        RETURN;
    END;

    IF @ApprovalRequestDetailId IS NULL OR @ApprovalRequestDetailId <= 0
    BEGIN
        RAISERROR('ApprovalRequestDetailId is required.', 16, 1);
        RETURN;
    END;

    IF NULLIF(LTRIM(RTRIM(@ActionBy)), '') IS NULL
    BEGIN
        RAISERROR('ActionBy is required.', 16, 1);
        RETURN;
    END;

    IF @NormalizedAction IN ('RETURN', 'REJECT')
       AND NULLIF(LTRIM(RTRIM(@Remarks)), '') IS NULL
    BEGIN
        RAISERROR('Remarks are required for RETURN and REJECT actions.', 16, 1);
        RETURN;
    END;

    SELECT
          @ApprovalRequestId      = d.ApprovalRequestId
        , @ActivityId             = d.ActivityId
    FROM dbo.SalesApprovalRequestDetail d
    WHERE d.ApprovalRequestDetailId = @ApprovalRequestDetailId;

    IF @ApprovalRequestId IS NULL
    BEGIN
        RAISERROR('Approval request detail %d was not found.', 16, 1, @ApprovalRequestDetailId);
        RETURN;
    END;

    -- Only the latest immutable snapshot per activity can be actioned.
    IF @ApprovalRequestDetailId <>
    (
        SELECT MAX(d2.ApprovalRequestDetailId)
        FROM dbo.SalesApprovalRequestDetail d2
        WHERE d2.ApprovalRequestId = @ApprovalRequestId
          AND d2.ActivityId = @ActivityId
    )
    BEGIN
        RAISERROR('Only the latest detail version for this activity can be processed.', 16, 1);
        RETURN;
    END;

    SELECT
          @CurrentApproverLevel   = r.CurrentApproverLevel
        , @CurrentApproverLoginId = r.CurrentApproverLoginId
        , @OldRequestStatus       = r.Status
    FROM dbo.SalesApprovalRequest r
    WHERE r.ApprovalRequestId = @ApprovalRequestId;

    SELECT TOP (1)
          @CurrentApproverRole = a.ApproverRole
    FROM dbo.SalesApprovalRequestApprover a
    WHERE a.ApprovalRequestId = @ApprovalRequestId
      AND a.ApproverLevel = @CurrentApproverLevel
    ORDER BY a.ApproverLevel;

    --------------------------------------------------------------------
    -- Payment Lock: a Completed request is immutable. No approval action
    -- of any kind may be processed against it, regardless of actor.
    --------------------------------------------------------------------
    IF ISNULL(@OldRequestStatus, '') = 'Completed'
    BEGIN
        RAISERROR('This approval request has been completed and is read-only. No further action can be processed.', 16, 1);
        RETURN;
    END;

    --------------------------------------------------------------------
    -- Header status validation: an approval action may only be processed
    -- while the request is actively awaiting approval.
    --------------------------------------------------------------------
    IF ISNULL(@OldRequestStatus, '') <> 'Pending'
    BEGIN
        RAISERROR('This approval request is not currently awaiting approval and cannot be processed.', 16, 1);
        RETURN;
    END;

    --------------------------------------------------------------------
    -- Current Approver validation: only the approver currently holding
    -- the request may process an action against it.
    --------------------------------------------------------------------
    IF @CurrentApproverLoginId IS NULL
       OR UPPER(LTRIM(RTRIM(@CurrentApproverLoginId))) <> UPPER(LTRIM(RTRIM(@ActionBy)))
    BEGIN
        RAISERROR('You are not the current approver for this approval request.', 16, 1);
        RETURN;
    END;

    --------------------------------------------------------------------
    -- Role enforcement (day-level actions only; expense-level role rules
    -- are enforced by usp_UpdateSalesDailyExpenseStatus).
    --   Manager : may Approve or Return a day. Must NOT Reject a day.
    --   Auditor : may Approve or Reject a day. Must NOT Return a day.
    --------------------------------------------------------------------
    IF @NormalizedAction = 'REJECT' AND ISNULL(@CurrentApproverRole, '') <> 'Auditor'
    BEGIN
        RAISERROR('Only the Auditor may reject a day. Managers return a day for correction instead.', 16, 1);
        RETURN;
    END;

    IF @NormalizedAction = 'RETURN' AND ISNULL(@CurrentApproverRole, '') = 'Auditor'
    BEGIN
        RAISERROR('The Auditor cannot return a day. Auditors approve or reject; managers return a day for correction.', 16, 1);
        RETURN;
    END;

    -- Map action to persisted status words (aligned with ClaimStatuses.cs).
    SET @MappedDetailStatus =
        CASE @NormalizedAction
            WHEN 'APPROVE' THEN 'Approved'
            WHEN 'RETURN'  THEN 'Returned'
            WHEN 'REJECT'  THEN 'Rejected'
        END;

    SET @MappedActivityStatus = @MappedDetailStatus;

    BEGIN TRY
        BEGIN TRANSACTION;

        --------------------------------------------------------------------
        -- 1) Detail snapshot transition (immutable row is updated in place;
        --    history is preserved via DetailVersion / PreviousDetailId chain).
        --------------------------------------------------------------------
        UPDATE dbo.SalesApprovalRequestDetail
        SET
              DetailStatus        = @MappedDetailStatus
            , DetailRemarks       = ISNULL(@Remarks, DetailRemarks)
            , DetailApprovedBy    = @ActionBy
            , DetailApprovedOn    = @NowUtc
            , DetailApprovedLevel = @CurrentApproverLevel
        WHERE ApprovalRequestDetailId = @ApprovalRequestDetailId
          AND ApprovalRequestId = @ApprovalRequestId;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Unable to update approval request detail %d.', 16, 1, @ApprovalRequestDetailId);
        END;

        --------------------------------------------------------------------
        -- 2) Synchronize SalesDailyActivity.ClaimStatus with detail outcome.
        --------------------------------------------------------------------
        UPDATE dbo.SalesDailyActivity
        SET
              ClaimStatus        = @MappedActivityStatus
            , ModifiedByUserId   = @ActionBy
            , ModifiedDateTime   = @NowIst
        WHERE ActivityId = @ActivityId;

        --------------------------------------------------------------------
        -- 3) Synchronize child expenses via ClaimStatus (authoritative column).
        --    Approved/Rejected terminal rows are left untouched on RETURN.
        --------------------------------------------------------------------
        UPDATE dbo.SalesDailyExpense
        SET
              ClaimStatus        = @MappedDetailStatus
            , ClaimProcessedBy   = @ActionBy
            , ClaimProcessedDate = @NowIst
            , ModifiedBy         = @ActionBy
            , ModifiedOn         = @NowUtc
            , Remarks            = CASE
                                       WHEN @Remarks IS NOT NULL THEN ISNULL(Remarks, @Remarks)
                                       ELSE Remarks
                                   END
        WHERE ActivityId = @ActivityId
          AND IsDeleted = 0
          AND (
                (@NormalizedAction = 'APPROVE' AND ISNULL(ClaimStatus, '') NOT IN ('Approved', 'Rejected'))
             OR (@NormalizedAction = 'RETURN'  AND ISNULL(ClaimStatus, '') NOT IN ('Rejected'))
             OR (@NormalizedAction = 'REJECT'  AND ISNULL(ClaimStatus, '') NOT IN ('Approved'))
          );

        --------------------------------------------------------------------
        -- 4) Mixed-state evaluation across latest detail versions only.
        --------------------------------------------------------------------
        SELECT
              @LatestDetailCount   = COUNT(1)
            , @PendingLatestCount  = SUM(CASE
                                              -- Unresolved at the current approver level (matches ClaimsApproval_v2 gatekeeper).
                                              WHEN ISNULL(ld.DetailStatus, '') IN ('', 'Pending', 'Submitted')
                                                  THEN 1
                                              WHEN ISNULL(ld.DetailStatus, '') = 'Approved'
                                               AND ISNULL(ld.DetailApprovedLevel, 0) < ISNULL(@CurrentApproverLevel, 0)
                                                  THEN 1
                                              ELSE 0
                                          END)
            , @ReturnedLatestCount = SUM(CASE
                                              WHEN ISNULL(ld.DetailStatus, '') = 'Returned' THEN 1 ELSE 0
                                          END)
            , @RejectedLatestCount  = SUM(CASE
                                              WHEN ISNULL(ld.DetailStatus, '') = 'Rejected' THEN 1 ELSE 0
                                          END)
            , @ApprovedLatestCount  = SUM(CASE
                                              WHEN ISNULL(ld.DetailStatus, '') = 'Approved' THEN 1 ELSE 0
                                          END)
        FROM dbo.SalesApprovalRequestDetail ld
        INNER JOIN
        (
            SELECT ActivityId, MAX(ApprovalRequestDetailId) AS LatestDetailId
            FROM dbo.SalesApprovalRequestDetail
            WHERE ApprovalRequestId = @ApprovalRequestId
            GROUP BY ActivityId
        ) latest
            ON ld.ApprovalRequestDetailId = latest.LatestDetailId
        WHERE ld.ApprovalRequestId = @ApprovalRequestId;

        SET @NewRequestStatus = @OldRequestStatus;

        IF @ReturnedLatestCount > 0
            SET @NewRequestStatus = 'Returned';
        ELSE IF @PendingLatestCount = 0
             AND (@ApprovedLatestCount + @RejectedLatestCount) = @LatestDetailCount
            SET @NewRequestStatus = 'Pending'; -- remain in workflow until forward / payment
        ELSE
            SET @NewRequestStatus = ISNULL(NULLIF(@OldRequestStatus, ''), 'Pending');

        --------------------------------------------------------------------
        -- 5) Approver + routing transitions.
        --------------------------------------------------------------------
        IF @NormalizedAction = 'RETURN'
        BEGIN
            -- Record the approver who returned the day.
            UPDATE dbo.SalesApprovalRequestApprover
            SET
                  Status     = 'Returned'
                , ActionBy   = @ActionBy
                , ActionDate = @NowUtc
                , Remarks    = ISNULL(@Remarks, Remarks)
            WHERE ApprovalRequestId = @ApprovalRequestId
              AND ApproverLevel = @CurrentApproverLevel;

            -- Request header reflects employee-correction state. Current approver is
            -- left unchanged here; usp_SubmitSalesDailyActivities resets to L1 on resubmit.
            UPDATE dbo.SalesApprovalRequest
            SET
                  Status                 = 'Returned'
                , ModifiedBy             = @ActionBy
                , ModifiedOn             = @NowUtc
            WHERE ApprovalRequestId = @ApprovalRequestId;

            SET @NewRequestStatus = 'Returned';
        END
        ELSE IF @NormalizedAction = 'APPROVE'
        BEGIN
            -- When every latest detail is resolved at the current level (Pending/Submitted
            -- or Approved at a lower DetailApprovedLevel), complete the current approver
            -- and advance routing (manager -> auditor, etc.).
            IF @PendingLatestCount = 0 AND @ReturnedLatestCount = 0
            BEGIN
                UPDATE dbo.SalesApprovalRequestApprover
                SET
                      Status     = 'Approved'
                    , ActionBy   = @ActionBy
                    , ActionDate = @NowUtc
                    , Remarks    = ISNULL(@Remarks, Remarks)
                WHERE ApprovalRequestId = @ApprovalRequestId
                  AND ApproverLevel = @CurrentApproverLevel
                  AND Status = 'Pending';

                -- TODO (Sprint 2 - Task 02):
                -- Routing responsibility will be migrated to usp_ForwardApprovalRequest.
                -- The next-approver lookup and the CurrentApproverLevel/CurrentApproverLoginId
                -- update below are left fully functional so the workflow continues to advance
                -- until that migration is complete.
                SELECT TOP (1)
                      @NextApproverLevel    = a.ApproverLevel
                    , @NextApproverLoginId  = a.ApproverLoginId
                    , @NextApproverRole      = a.ApproverRole
                FROM dbo.SalesApprovalRequestApprover a
                WHERE a.ApprovalRequestId = @ApprovalRequestId
                  AND a.ApproverLevel > @CurrentApproverLevel
                  AND a.Status = 'Pending'
                  AND ISNULL(a.IsRequired, 1) = 1
                ORDER BY a.ApproverLevel ASC;

                IF @NextApproverLevel IS NOT NULL
                BEGIN
                    -- Activate next approver; request stays Pending for the next review pass.
                    UPDATE dbo.SalesApprovalRequest
                    SET
                          CurrentApproverLevel    = @NextApproverLevel
                        , CurrentApproverLoginId  = @NextApproverLoginId
                        , Status                  = 'Pending'
                        , ModifiedBy              = @ActionBy
                        , ModifiedOn              = @NowUtc
                    WHERE ApprovalRequestId = @ApprovalRequestId;

                    SET @NewRequestStatus = 'Pending';

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
                        , ISNULL(@OldRequestStatus, 'Pending')
                        , 'Pending'
                        , @ActionBy
                        , CONCAT(
                              'Level ',
                              @CurrentApproverLevel,
                              ' completed. Forwarded to ',
                              ISNULL(@NextApproverRole, 'Approver'),
                              ' (',
                              ISNULL(@NextApproverLoginId, 'Pool'),
                              ').'
                          )
                    );
                END
                -- TODO (Sprint 2 - Task 02):
                -- Routing responsibility will be migrated to usp_ForwardApprovalRequest.
                -- Setting the header to Completed and clearing the routing pointer below is
                -- left fully functional so a claim can still reach Completed until that
                -- migration is complete.
                ELSE IF ISNULL(@CurrentApproverRole, '') <> 'Auditor'
                BEGIN
                    -- Non-auditor final level with all days resolved -> Completed.
                    UPDATE dbo.SalesApprovalRequest
                    SET
                          Status                  = 'Completed'
                        , CurrentApproverLevel    = NULL
                        , CurrentApproverLoginId  = NULL
                        , ModifiedBy              = @ActionBy
                        , ModifiedOn              = @NowUtc
                    WHERE ApprovalRequestId = @ApprovalRequestId;

                    SET @NewRequestStatus = 'Completed';

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
                        , ISNULL(@OldRequestStatus, 'Pending')
                        , 'Completed'
                        , @ActionBy
                        , 'All approval levels finished. Awaiting payment processing.'
                    );
                END
                ELSE
                BEGIN
                    -- Auditor pass complete: header Completed so usp_MarkRequestReadyForPayment is reachable.
                    UPDATE dbo.SalesApprovalRequest
                    SET
                          Status                 = 'Completed'
                        , CurrentApproverLevel   = NULL
                        , CurrentApproverLoginId = NULL
                        , ModifiedBy             = @ActionBy
                        , ModifiedOn             = @NowUtc
                    WHERE ApprovalRequestId = @ApprovalRequestId
                      AND Status = 'Pending';

                    IF @@ROWCOUNT > 0
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
                            , 'Pending'
                            , 'Completed'
                            , @ActionBy
                            , 'Final Auditor Approval'
                        );
                    END

                    SET @NewRequestStatus = 'Completed';
                END
            END
            ELSE
            BEGIN
                -- Partial approval: keep request Pending while days remain open.
                UPDATE dbo.SalesApprovalRequest
                SET
                      Status      = CASE WHEN @ReturnedLatestCount > 0 THEN 'Returned' ELSE 'Pending' END
                    , ModifiedBy  = @ActionBy
                    , ModifiedOn  = @NowUtc
                WHERE ApprovalRequestId = @ApprovalRequestId;

                SET @NewRequestStatus =
                    CASE WHEN @ReturnedLatestCount > 0 THEN 'Returned' ELSE 'Pending' END;
            END
        END
        ELSE -- REJECT
        BEGIN
            UPDATE dbo.SalesApprovalRequest
            SET
                  Status      = CASE WHEN @ReturnedLatestCount > 0 THEN 'Returned' ELSE 'Pending' END
                , ModifiedBy  = @ActionBy
                , ModifiedOn  = @NowUtc
            WHERE ApprovalRequestId = @ApprovalRequestId;

            SET @NewRequestStatus =
                CASE WHEN @ReturnedLatestCount > 0 THEN 'Returned' ELSE 'Pending' END;
        END

        --------------------------------------------------------------------
        -- 6) Header totals from latest non-returned / non-rejected snapshots.
        --------------------------------------------------------------------
        UPDATE dbo.SalesApprovalRequest
        SET
              ApprovedKM =
              (
                  SELECT ISNULL(SUM(d.Snapshot_TotalDistanceKM), 0)
                  FROM dbo.SalesApprovalRequestDetail d
                  INNER JOIN
                  (
                      SELECT ActivityId, MAX(ApprovalRequestDetailId) AS LatestDetailId
                      FROM dbo.SalesApprovalRequestDetail
                      WHERE ApprovalRequestId = @ApprovalRequestId
                      GROUP BY ActivityId
                  ) latest
                      ON d.ApprovalRequestDetailId = latest.LatestDetailId
                  WHERE d.ApprovalRequestId = @ApprovalRequestId
                    AND ISNULL(d.DetailStatus, '') NOT IN ('Rejected', 'Returned')
              )
            , ApprovedClaimAmount =
              (
                  SELECT ISNULL(SUM(d.Snapshot_CalculatedClaimAmount), 0)
                  FROM dbo.SalesApprovalRequestDetail d
                  INNER JOIN
                  (
                      SELECT ActivityId, MAX(ApprovalRequestDetailId) AS LatestDetailId
                      FROM dbo.SalesApprovalRequestDetail
                      WHERE ApprovalRequestId = @ApprovalRequestId
                      GROUP BY ActivityId
                  ) latest
                      ON d.ApprovalRequestDetailId = latest.LatestDetailId
                  WHERE d.ApprovalRequestId = @ApprovalRequestId
                    AND ISNULL(d.DetailStatus, '') NOT IN ('Rejected', 'Returned')
              )
            , ApprovedDAAmount =
              (
                  SELECT ISNULL(SUM(d.Snapshot_DAAmount), 0)
                  FROM dbo.SalesApprovalRequestDetail d
                  INNER JOIN
                  (
                      SELECT ActivityId, MAX(ApprovalRequestDetailId) AS LatestDetailId
                      FROM dbo.SalesApprovalRequestDetail
                      WHERE ApprovalRequestId = @ApprovalRequestId
                      GROUP BY ActivityId
                  ) latest
                      ON d.ApprovalRequestDetailId = latest.LatestDetailId
                  WHERE d.ApprovalRequestId = @ApprovalRequestId
                    AND ISNULL(d.DetailStatus, '') NOT IN ('Rejected', 'Returned')
              )
            , ApprovedHotelAmount =
              (
                  SELECT ISNULL(SUM(d.Snapshot_HotelAmount), 0)
                  FROM dbo.SalesApprovalRequestDetail d
                  INNER JOIN
                  (
                      SELECT ActivityId, MAX(ApprovalRequestDetailId) AS LatestDetailId
                      FROM dbo.SalesApprovalRequestDetail
                      WHERE ApprovalRequestId = @ApprovalRequestId
                      GROUP BY ActivityId
                  ) latest
                      ON d.ApprovalRequestDetailId = latest.LatestDetailId
                  WHERE d.ApprovalRequestId = @ApprovalRequestId
                    AND ISNULL(d.DetailStatus, '') NOT IN ('Rejected', 'Returned')
              )
            , ApprovedTravelAmount =
              (
                  SELECT ISNULL(SUM(d.Snapshot_TravelAmount), 0)
                  FROM dbo.SalesApprovalRequestDetail d
                  INNER JOIN
                  (
                      SELECT ActivityId, MAX(ApprovalRequestDetailId) AS LatestDetailId
                      FROM dbo.SalesApprovalRequestDetail
                      WHERE ApprovalRequestId = @ApprovalRequestId
                      GROUP BY ActivityId
                  ) latest
                      ON d.ApprovalRequestDetailId = latest.LatestDetailId
                  WHERE d.ApprovalRequestId = @ApprovalRequestId
                    AND ISNULL(d.DetailStatus, '') NOT IN ('Rejected', 'Returned')
              )
            , ModifiedBy = @ActionBy
            , ModifiedOn = @NowUtc
        WHERE ApprovalRequestId = @ApprovalRequestId;

        --------------------------------------------------------------------
        -- 7) Status history when header status changes (RETURN / completion).
        --------------------------------------------------------------------
        IF ISNULL(@OldRequestStatus, '') <> ISNULL(@NewRequestStatus, '')
           AND NOT (
               @NormalizedAction = 'APPROVE'
               AND @NewRequestStatus = 'Pending'
               AND ISNULL(@OldRequestStatus, 'Pending') = 'Pending'
           )
           AND NOT (
               @NormalizedAction = 'APPROVE'
               AND @NewRequestStatus = 'Completed'
               AND ISNULL(@CurrentApproverRole, '') = 'Auditor'
           )
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
                , ISNULL(@OldRequestStatus, 'Pending')
                , ISNULL(@NewRequestStatus, 'Pending')
                , @ActionBy
                , CONCAT(
                      'Detail ',
                      @ApprovalRequestDetailId,
                      ' ',
                      @NormalizedAction,
                      CASE
                          WHEN @Remarks IS NULL THEN ''
                          ELSE CONCAT(': ', @Remarks)
                      END
                  )
            );
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrState INT = ERROR_STATE();

        RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
    END CATCH
END
