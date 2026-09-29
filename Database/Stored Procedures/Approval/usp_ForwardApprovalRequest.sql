/* ============================================================================
   NAME:        forward_latest_version_count
   WHEN:        2026-09-15
   WHY:         Forward counted superseded Pending/Returned snapshots after RESUBMIT
   WHAT:        usp_ForwardApprovalRequest day/expense completeness uses latest DetailVersion then latest ApprovalRequestDetailId
   ============================================================================ */

/*
    Object:      [dbo].[usp_ForwardApprovalRequest]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Baseline extracted verbatim; subsequently hardened under
                 Sprint 2 - Task 02 (see change note below).
*/

/*
    Sprint 2 - Task 02 (Forward Approval Hardening)
    Change: Align usp_ForwardApprovalRequest with the approved Functional
            Specification and Technical Design Specification for Forward
            (FS §7.3, §7.4 R-07, §7.7, §8.2, §11.7 VF-01–VF-06; O-11;
            TDS §4.5 / G-04 / G-16).

            - Distinguish missing request from null routing pointer.
            - Payment lock / Pending-only header guard.
            - Current approver match (case/whitespace normalized).
            - Manager-only Forward; Auditor must use Mark Ready For Payment
              (R-06; FS §8.2).
            - Day completeness on latest detail versions, including days
              Approved at a prior level but not yet resolved at the current
              level (DetailApprovedLevel).
            - Expense Pending/Returned completeness (VF-03 / VF-04).
            - Next required approver selected with ORDER BY ApproverLevel ASC.
            - Refuse when no next required approver exists (VF-05 / R-05).
              Do NOT write header Status = 'Approved' (H-1 / F-01).
              Do NOT write ReadyForPayment (O-12 / T-09 / R-06).
            - Write ApprovalRequestStatusHistory on successful handover.
            - Re-raise errors after rollback so callers using ExecuteNonQuery
              observe refusal (application call site).

            Out of scope for this task (explicit constraints):
            - Totals / KM / policy calculation
            - Detail or expense row updates
            - Changes to usp_ProcessApprovalAction (frozen)
            - Schema / frontend / C# changes
*/

CREATE   PROCEDURE [dbo].[usp_ForwardApprovalRequest]
(
      @ApprovalRequestId INT
    , @ActionBy          NVARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY

        BEGIN TRANSACTION;

        ------------------------------------------------------------
        -- Status Constants
        ------------------------------------------------------------

        DECLARE
              @STATUS_PENDING    NVARCHAR(20) = N'Pending'
            , @STATUS_APPROVED   NVARCHAR(20) = N'Approved'
            , @STATUS_RETURNED   NVARCHAR(20) = N'Returned'
            , @STATUS_REJECTED   NVARCHAR(20) = N'Rejected'
            , @STATUS_COMPLETED  NVARCHAR(20) = N'Completed';

        ------------------------------------------------------------
        -- Variables
        ------------------------------------------------------------

        DECLARE
              @CurrentApproverLoginId NVARCHAR(100)
            , @CurrentApproverLevel   INT
            , @CurrentRequestStatus   NVARCHAR(50)
            , @CurrentApproverRole    NVARCHAR(100)

            , @NextApproverLoginId    NVARCHAR(100)
            , @NextApproverLevel      INT
            , @NextApproverRole       NVARCHAR(100)

            , @UnresolvedDays         INT
            , @UnresolvedExpenses     INT
            , @HeaderFound            BIT
            , @UtcNow                 DATETIME2(7);

        SET @UtcNow = SYSUTCDATETIME();
        SET @HeaderFound = 0;

        ------------------------------------------------------------
        -- Read & Lock Request
        ------------------------------------------------------------

        SELECT
              @CurrentApproverLoginId = r.CurrentApproverLoginId
            , @CurrentApproverLevel   = r.CurrentApproverLevel
            , @CurrentRequestStatus   = r.Status
            , @HeaderFound            = 1
        FROM SalesApprovalRequest r
             WITH (UPDLOCK, HOLDLOCK)
        WHERE r.ApprovalRequestId = @ApprovalRequestId;

        IF @HeaderFound = 0
        BEGIN
            THROW 51001,
                  'Approval request not found.',
                  1;
        END;

        ------------------------------------------------------------
        -- Payment Lock: Completed is immutable (FS F-11 / PL-02)
        ------------------------------------------------------------

        IF ISNULL(@CurrentRequestStatus, '') = @STATUS_COMPLETED
        BEGIN
            THROW 51010,
                  'This approval request has been completed and is read-only. No further action can be processed.',
                  1;
        END;

        ------------------------------------------------------------
        -- Request must be Pending (T-04)
        ------------------------------------------------------------

        IF ISNULL(@CurrentRequestStatus, '') <> @STATUS_PENDING
        BEGIN
            THROW 51002,
                  'Only Pending requests can be forwarded.',
                  1;
        END;

        ------------------------------------------------------------
        -- Routing pointer must be present on a Pending claim
        ------------------------------------------------------------

        IF @CurrentApproverLevel IS NULL
           OR NULLIF(LTRIM(RTRIM(@CurrentApproverLoginId)), '') IS NULL
        BEGIN
            THROW 51011,
                  'This approval request has no current approver and cannot be forwarded.',
                  1;
        END;

        ------------------------------------------------------------
        -- Verify Current Approver (FS §7.3 #1 / R-01)
        ------------------------------------------------------------

        IF UPPER(LTRIM(RTRIM(@CurrentApproverLoginId)))
           <> UPPER(LTRIM(RTRIM(ISNULL(@ActionBy, ''))))
        BEGIN
            THROW 51003,
                  'Only the current approver can forward this request.',
                  1;
        END;

        ------------------------------------------------------------
        -- Resolve current approver role; Forward is Manager-only
        -- (FS §8.2; R-06 — Auditor terminal action is Ready For Payment)
        ------------------------------------------------------------

        SELECT TOP (1)
              @CurrentApproverRole = a.ApproverRole
        FROM SalesApprovalRequestApprover a
        WHERE a.ApprovalRequestId = @ApprovalRequestId
          AND a.ApproverLevel     = @CurrentApproverLevel
        ORDER BY a.ApproverLevel;

        IF ISNULL(@CurrentApproverRole, '') = N'Auditor'
        BEGIN
            THROW 51012,
                  'The Auditor cannot forward a request. Use Mark Ready For Payment when all items are resolved.',
                  1;
        END;

        ------------------------------------------------------------
        -- Day completeness on latest versions only (VF-01 / VF-02;
        -- FS §7.3 #2–#3; "resolved at this level" via DetailApprovedLevel)
        ------------------------------------------------------------

        SELECT
            @UnresolvedDays = COUNT(*)
        FROM SalesApprovalRequestDetail d
        INNER JOIN
        (
            SELECT
                  d2.ActivityId
                , MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
            FROM SalesApprovalRequestDetail d2
            INNER JOIN
            (
                SELECT
                      ActivityId
                    , MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
                FROM SalesApprovalRequestDetail
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
          AND
          (
                ISNULL(d.DetailStatus, '') IN (N'', @STATUS_PENDING, N'Submitted', @STATUS_RETURNED)
             OR
                (
                    ISNULL(d.DetailStatus, '') = @STATUS_APPROVED
                    AND ISNULL(d.DetailApprovedLevel, 0) < @CurrentApproverLevel
                )
          );

        IF @UnresolvedDays > 0
        BEGIN
            THROW 51004,
                  'Some activities are still Pending or Returned, or are not yet resolved at the current approval level.',
                  1;
        END;

        ------------------------------------------------------------
        -- Expense completeness (VF-03 / VF-04; FS §7.3 #4–#5)
        -- Assigned to this procedure by FS §11.7 / TDS §8.6.
        -- Read-only check; expenses are not updated here.
        ------------------------------------------------------------

        SELECT
            @UnresolvedExpenses = COUNT(*)
        FROM SalesDailyExpense e
        INNER JOIN
        (
            SELECT
                  d.ActivityId
            FROM SalesApprovalRequestDetail d
            INNER JOIN
            (
                SELECT
                      d2.ActivityId
                    , MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
                FROM SalesApprovalRequestDetail d2
                INNER JOIN
                (
                    SELECT
                          ActivityId
                        , MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
                    FROM SalesApprovalRequestDetail
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
        ) claimActivities
            ON e.ActivityId = claimActivities.ActivityId
        WHERE e.IsDeleted = 0
          AND ISNULL(e.ClaimStatus, '') IN (@STATUS_PENDING, @STATUS_RETURNED);

        IF @UnresolvedExpenses > 0
        BEGIN
            THROW 51013,
                  'Some expenses are still Pending or Returned and must be resolved before forwarding.',
                  1;
        END;

        ------------------------------------------------------------
        -- Find Next Required Approver (level order — R-10 / VF-05)
        ------------------------------------------------------------

        SELECT TOP (1)
              @NextApproverLoginId = a.ApproverLoginId
            , @NextApproverLevel   = a.ApproverLevel
            , @NextApproverRole    = a.ApproverRole
        FROM SalesApprovalRequestApprover a
        WHERE a.ApprovalRequestId = @ApprovalRequestId
          AND a.IsRequired = 1
          AND a.ApproverLevel > @CurrentApproverLevel
          AND a.Status = @STATUS_PENDING
        ORDER BY a.ApproverLevel ASC;

        ------------------------------------------------------------
        -- No next required approver: refuse (VF-05 / R-05).
        -- Do not write Status = 'Approved' (H-1 / F-01).
        -- Do not write ReadyForPayment (O-12 / T-09 / R-06).
        ------------------------------------------------------------

        IF @NextApproverLevel IS NULL
        BEGIN
            THROW 51014,
                  'No next required approver is configured. Forwarding is refused. If this claim is at the Auditor level, use Mark Ready For Payment instead.',
                  1;
        END;

        ------------------------------------------------------------
        -- Complete Current Approver
        ------------------------------------------------------------

        UPDATE SalesApprovalRequestApprover
           SET Status      = @STATUS_APPROVED,
               ActionBy    = @ActionBy,
               ActionDate  = @UtcNow,
               ModifiedBy  = @ActionBy,
               ModifiedOn  = @UtcNow
         WHERE ApprovalRequestId = @ApprovalRequestId
           AND ApproverLevel     = @CurrentApproverLevel
           AND Status            = @STATUS_PENDING;

        IF @@ROWCOUNT = 0
        BEGIN
            THROW 51005,
                  'Unable to complete current approver.',
                  1;
        END;

        ------------------------------------------------------------
        -- Move Header to Next Approver (sole routing write — R-07)
        -- Header Status remains Pending (T-04).
        ------------------------------------------------------------

        UPDATE SalesApprovalRequest
           SET CurrentApproverLoginId = @NextApproverLoginId,
               CurrentApproverLevel   = @NextApproverLevel,
               ModifiedBy             = @ActionBy,
               ModifiedOn             = @UtcNow
         WHERE ApprovalRequestId = @ApprovalRequestId;

        IF @@ROWCOUNT = 0
        BEGIN
            THROW 51006,
                  'Unable to update approval request.',
                  1;
        END;

        ------------------------------------------------------------
        -- Activate Next Approver
        ------------------------------------------------------------

        UPDATE SalesApprovalRequestApprover
           SET Status      = @STATUS_PENDING,
               ModifiedBy  = @ActionBy,
               ModifiedOn  = @UtcNow
         WHERE ApprovalRequestId = @ApprovalRequestId
           AND ApproverLoginId   = @NextApproverLoginId
           AND ApproverLevel     = @NextApproverLevel;

        IF @@ROWCOUNT = 0
        BEGIN
            THROW 51007,
                  'Unable to activate next approver.',
                  1;
        END;

        ------------------------------------------------------------
        -- Routing history (FS §7.3 #10; AD-04; O-11)
        ------------------------------------------------------------

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
            , @STATUS_PENDING
            , @STATUS_PENDING
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

        COMMIT TRANSACTION;

        SELECT
              CAST(1 AS BIT) AS Success,
              'Request forwarded successfully.' AS Message,
              @ApprovalRequestId AS ApprovalRequestId,
              @NextApproverLoginId AS CurrentApproverLoginId,
              @NextApproverLevel AS CurrentApproverLevel,
              @STATUS_PENDING AS RequestStatus;

    END TRY
    BEGIN CATCH

        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrState INT = ERROR_STATE();

        -- Re-raise so ExecuteNonQuery callers observe the refusal.
        -- (Prior behaviour returned a Success=0 result set and swallowed the error.)
        RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);

    END CATCH
END;
