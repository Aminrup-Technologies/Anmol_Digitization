/*
    Package:     Database/Deployments/L2R19_UAT
    Script:      D_Rollback_L2R19_Finalization_UAT.sql
    Database:    AnmolDristiDev ONLY
    Server:      MUMANMOAPP01\SQLEXPRESS ONLY
    Live:        FORBIDDEN (AnmolDristi)
    Mode:        UAT-ONLY. NOT AUTHORIZED TO EXECUTE.

    Restores the five L2-R19 modules to the live text captured before apply.
    It does not restore claim rows. Claim recovery is a separately authorized
    restore of the reviewed full backup.

    Verified preimage present:
      dbo.usp_MarkRequestReadyForPayment
      SHA-256 8B2ECBCE8AC258D67A2C3CFCC6C45CC385F01354B8F5D8B5594409889685E158
      LEN 2882 / DATALENGTH 5764
      Source: the module embedded in the previous rollback script and checked
      against the 2026-10-13 checklist. This file rechecks that hash.

    Verified UAT preimages pasted 2026-10-13 from OBJECT_DEFINITION on
    AnmolDristiDev. Each body was accepted only after its UTF-16LE SHA-256
    and LEN matched the preflight record:
      dbo.usp_UpdateSalesDailyExpenseStatus  3DA56C797403A1CC0EBFF57CE5F7FF5705DC7E036E9B6EFC7A1F272FBC34EC9E
      dbo.usp_ProcessApprovalAction          F6471A308779E26D74A2B84B00A6DAF71DAA84FDFD5A7C71189F0DA5B61E13EE
      dbo.usp_SubmitSalesDailyActivities     5BEDA23C85279789D325C66B573C37BB1E037DA5C0153695A435DEA9E7D37547
      dbo.usp_RecalculateApprovalRequest     D14FEF7DFF5F8C4E008B2485D445B897CA4B76FBB60D6E5CEF68745894BE5A9D
    Evidence: docs/reviews/L2_Auditor_AsIs/L2R19_preimage_export_2026-10-13T071700Z

    @Execute, @RollbackApproved, and @AllFivePreimagesPresent stay 0.
    The batch returns before any catalog read or ALTER.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Execute bit = 0;
DECLARE @RollbackApproved bit = 0;
DECLARE @AllFivePreimagesPresent bit = 0;

DECLARE @ExpensePreimage nvarchar(max) = N'
-- 5. dbo.usp_UpdateSalesDailyExpenseStatus
/* ============================================================================
   NAME:        rc023_expense_auth_payment_lock
   WHEN:        2026-09-23
   WHY:         Expense mutations had no current-approver, header-status, or payment lock
   WHAT:        usp_UpdateSalesDailyExpenseStatus enforces Pending + current approver + role rules
   ============================================================================ */

/* ============================================================================
   NAME:        expense_total_includes_stationary
   WHEN:        2026-09-15
   WHY:         Expense status rewrite omitted Stationary so Snapshot_ExpenseTotal drifted from recalc
   WHAT:        Latest-detail snapshot writes Stationary and ExpenseTotal = DA+Hotel+Travel+Stationary
   ============================================================================ */

/* ============================================================================
   NAME:        expense_status_latest_detail
   WHEN:        2026-09-15
   WHY:        Receipt status updates targeted any snapshot for the activity
   WHAT:        usp_UpdateSalesDailyExpenseStatus resolves latest DetailVersion then latest ApprovalRequestDetailId
   ============================================================================ */

/*
    Object:      [dbo].[usp_UpdateSalesDailyExpenseStatus]
    Type:        STORED PROCEDURE
*/

CREATE     PROCEDURE [dbo].[usp_UpdateSalesDailyExpenseStatus]
(
    @ExpenseId INT,
    @NewStatus NVARCHAR(50), -- ''Approved'', ''Rejected'', or ''Returned''
    @ActionBy  NVARCHAR(100),
    @Remarks   NVARCHAR(MAX) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
          @NormalizedStatus         NVARCHAR(50) = UPPER(LTRIM(RTRIM(ISNULL(@NewStatus, N''''))))
        , @ActivityId               INT
        , @ApprovalRequestDetailId  INT
        , @ApprovalRequestId        INT
        , @RequestStatus            NVARCHAR(50)
        , @CurrentApproverLoginId   NVARCHAR(100)
        , @CurrentApproverLevel     INT
        , @CurrentApproverRole      NVARCHAR(100);

    IF @ExpenseId IS NULL OR @ExpenseId <= 0
    BEGIN
        RAISERROR(''ExpenseId is required.'', 16, 1);
        RETURN;
    END;

    IF NULLIF(LTRIM(RTRIM(@ActionBy)), N'''') IS NULL
    BEGIN
        RAISERROR(''ActionBy is required.'', 16, 1);
        RETURN;
    END;

    IF @NormalizedStatus NOT IN (N''APPROVED'', N''REJECTED'', N''RETURNED'')
    BEGIN
        RAISERROR(''Invalid expense status. Expected Approved, Rejected, or Returned.'', 16, 1);
        RETURN;
    END;

    -- Canonical persisted status words
    SET @NewStatus =
        CASE @NormalizedStatus
            WHEN N''APPROVED'' THEN N''Approved''
            WHEN N''REJECTED'' THEN N''Rejected''
            WHEN N''RETURNED'' THEN N''Returned''
        END;

    -- 1️⃣ Safety Check: Prevent update if deleted / missing
    IF NOT EXISTS (SELECT 1 FROM dbo.SalesDailyExpense WHERE ExpenseId = @ExpenseId)
    BEGIN
        RAISERROR(''Expense %d was not found.'', 16, 1, @ExpenseId);
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.SalesDailyExpense WHERE ExpenseId = @ExpenseId AND IsDeleted = 1)
    BEGIN
        RAISERROR(''Cannot update a deleted expense.'', 16, 1);
        RETURN;
    END;

    SELECT @ActivityId = ActivityId
    FROM dbo.SalesDailyExpense
    WHERE ExpenseId = @ExpenseId;

    IF @ActivityId IS NULL
    BEGIN
        RAISERROR(''Expense %d is not linked to an activity.'', 16, 1, @ExpenseId);
        RETURN;
    END;

    -- Latest detail for activity: DetailVersion then MAX(ApprovalRequestDetailId)
    SELECT TOP (1)
          @ApprovalRequestDetailId = d.ApprovalRequestDetailId
        , @ApprovalRequestId       = d.ApprovalRequestId
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN (
        SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
        FROM dbo.SalesApprovalRequestDetail
        WHERE ActivityId = @ActivityId
        GROUP BY ActivityId
    ) lv
        ON lv.ActivityId = d.ActivityId
       AND ISNULL(d.DetailVersion, 1) = lv.LatestVersion
    WHERE d.ActivityId = @ActivityId
    ORDER BY d.ApprovalRequestDetailId DESC;

    IF @ApprovalRequestId IS NULL
    BEGIN
        RAISERROR(''No approval request detail found for activity %d.'', 16, 1, @ActivityId);
        RETURN;
    END;

    SELECT
          @RequestStatus          = r.Status
        , @CurrentApproverLoginId = r.CurrentApproverLoginId
        , @CurrentApproverLevel   = r.CurrentApproverLevel
    FROM dbo.SalesApprovalRequest r WITH (UPDLOCK, HOLDLOCK)
    WHERE r.ApprovalRequestId = @ApprovalRequestId;

    SELECT TOP (1)
          @CurrentApproverRole = a.ApproverRole
    FROM dbo.SalesApprovalRequestApprover a
    WHERE a.ApprovalRequestId = @ApprovalRequestId
      AND a.ApproverLevel = @CurrentApproverLevel
    ORDER BY a.ApproverLevel;

    --------------------------------------------------------------------
    -- Payment Lock: Completed / ReadyForPayment are immutable for expense mutations.
    --------------------------------------------------------------------
    IF ISNULL(@RequestStatus, N'''') IN (N''Completed'', N''ReadyForPayment'')
    BEGIN
        RAISERROR(''This approval request is locked for payment and cannot be modified.'', 16, 1);
        RETURN;
    END;

    --------------------------------------------------------------------
    -- Header status: expense actions only while Pending.
    --------------------------------------------------------------------
    IF ISNULL(@RequestStatus, N'''') <> N''Pending''
    BEGIN
        RAISERROR(''This approval request is not currently awaiting approval and cannot be processed.'', 16, 1);
        RETURN;
    END;

    --------------------------------------------------------------------
    -- Current Approver: only the holder of the request may mutate expenses.
    --------------------------------------------------------------------
    IF @CurrentApproverLoginId IS NULL
       OR UPPER(LTRIM(RTRIM(@CurrentApproverLoginId))) <> UPPER(LTRIM(RTRIM(@ActionBy)))
    BEGIN
        RAISERROR(''You are not the current approver for this approval request.'', 16, 1);
        RETURN;
    END;

    --------------------------------------------------------------------
    -- Role enforcement (expense-level; mirrors day-level ProcessApprovalAction).
    --   Manager : Approve or Return. Must NOT Reject.
    --   Auditor : Approve or Reject. Must NOT Return.
    --------------------------------------------------------------------
    IF @NormalizedStatus = N''REJECTED'' AND ISNULL(@CurrentApproverRole, N'''') <> N''Auditor''
    BEGIN
        RAISERROR(''Only the Auditor may reject an expense. Managers return an expense for correction instead.'', 16, 1);
        RETURN;
    END;

    IF @NormalizedStatus = N''RETURNED'' AND ISNULL(@CurrentApproverRole, N'''') = N''Auditor''
    BEGIN
        RAISERROR(''The Auditor cannot return an expense. Auditors approve or reject; managers return for correction.'', 16, 1);
        RETURN;
    END;

    -- Status and financial snapshot commit together. The engine joins this transaction.
    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.SalesDailyExpense
        SET
            ClaimStatus = @NewStatus,
            Remarks = @Remarks,
            ClaimProcessedBy = @ActionBy,
            ClaimProcessedDate = DATEADD(MINUTE, 330, SYSUTCDATETIME()),
            ModifiedBy = @ActionBy,
            ModifiedOn = SYSUTCDATETIME()
        WHERE ExpenseId = @ExpenseId;

        IF @ApprovalRequestDetailId IS NOT NULL
            EXEC dbo.usp_RecalculateSalesClaim @ApprovalRequestId = @ApprovalRequestId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
';
DECLARE @ExpensePreimageHash varchar(64) = '3DA56C797403A1CC0EBFF57CE5F7FF5705DC7E036E9B6EFC7A1F272FBC34EC9E';
DECLARE @ProcessPreimage nvarchar(max) = N'
/* ============================================================================
   NAME:        hf231_auditor_ready_for_payment_checkpoint
   WHEN:        2026-09-28
   WHY:         Last Auditor approve wrote Completed and cleared the current approver,
                so View Pending dropped the claim before Mark Ready for Payment
   WHAT:        Auditor terminal path leaves the header Pending, the current approver
                unchanged, and the Auditor approver row Pending. No Pending → Completed history.
   ============================================================================ */

/* ============================================================================
   NAME:        rc023_latest_version_ownership
   WHEN:        2026-09-23
   WHY:         Day-action latest predicate used MAX(Id) only while Forward/Expense/Recalc use DetailVersion then Id
   WHAT:        usp_ProcessApprovalAction latest-detail resolution uses MAX(DetailVersion) then MAX(ApprovalRequestDetailId)
   ============================================================================ */

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
                header''s CurrentApproverLoginId).
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
            between the two tasks'' merges.

            No totals, expense, distance, or policy calculation logic was
            modified. No schema changes were made. No other procedure was
            modified.
*/

CREATE   PROCEDURE dbo.usp_ProcessApprovalAction
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

    SET @NormalizedAction = UPPER(LTRIM(RTRIM(ISNULL(@Action, ''''))));

    IF @NormalizedAction NOT IN (''APPROVE'', ''RETURN'', ''REJECT'')
    BEGIN
        RAISERROR(''Invalid action. Expected APPROVE, RETURN, or REJECT.'', 16, 1);
        RETURN;
    END;

    IF @ApprovalRequestDetailId IS NULL OR @ApprovalRequestDetailId <= 0
    BEGIN
        RAISERROR(''ApprovalRequestDetailId is required.'', 16, 1);
        RETURN;
    END;

    IF NULLIF(LTRIM(RTRIM(@ActionBy)), '''') IS NULL
    BEGIN
        RAISERROR(''ActionBy is required.'', 16, 1);
        RETURN;
    END;

    IF @NormalizedAction IN (''RETURN'', ''REJECT'')
       AND NULLIF(LTRIM(RTRIM(@Remarks)), '''') IS NULL
    BEGIN
        RAISERROR(''Remarks are required for RETURN and REJECT actions.'', 16, 1);
        RETURN;
    END;

    SELECT
          @ApprovalRequestId      = d.ApprovalRequestId
        , @ActivityId             = d.ActivityId
    FROM dbo.SalesApprovalRequestDetail d
    WHERE d.ApprovalRequestDetailId = @ApprovalRequestDetailId;

    IF @ApprovalRequestId IS NULL
    BEGIN
        RAISERROR(''Approval request detail %d was not found.'', 16, 1, @ApprovalRequestDetailId);
        RETURN;
    END;

    -- Only the latest immutable snapshot per activity can be actioned
    -- (DetailVersion then ApprovalRequestDetailId — RC-012 / RC-023).
    IF @ApprovalRequestDetailId <>
    (
        SELECT TOP (1) d2.ApprovalRequestDetailId
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
        ORDER BY d2.ApprovalRequestDetailId DESC
    )
    BEGIN
        RAISERROR(''Only the latest detail version for this activity can be processed.'', 16, 1);
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
    -- Payment Lock: Completed / ReadyForPayment are immutable.
    --------------------------------------------------------------------
    IF ISNULL(@OldRequestStatus, '''') IN (''Completed'', ''ReadyForPayment'')
    BEGIN
        RAISERROR(''This approval request is locked for payment and cannot be modified.'', 16, 1);
        RETURN;
    END;

    --------------------------------------------------------------------
    -- Header status validation: an approval action may only be processed
    -- while the request is actively awaiting approval.
    --------------------------------------------------------------------
    IF ISNULL(@OldRequestStatus, '''') <> ''Pending''
    BEGIN
        RAISERROR(''This approval request is not currently awaiting approval and cannot be processed.'', 16, 1);
        RETURN;
    END;

    --------------------------------------------------------------------
    -- Current Approver validation: only the approver currently holding
    -- the request may process an action against it.
    --------------------------------------------------------------------
    IF @CurrentApproverLoginId IS NULL
       OR UPPER(LTRIM(RTRIM(@CurrentApproverLoginId))) <> UPPER(LTRIM(RTRIM(@ActionBy)))
    BEGIN
        RAISERROR(''You are not the current approver for this approval request.'', 16, 1);
        RETURN;
    END;

    --------------------------------------------------------------------
    -- Role enforcement (day-level actions only; expense-level role rules
    -- are enforced by usp_UpdateSalesDailyExpenseStatus).
    --   Manager : may Approve or Return a day. Must NOT Reject a day.
    --   Auditor : may Approve or Reject a day. Must NOT Return a day.
    --------------------------------------------------------------------
    IF @NormalizedAction = ''REJECT'' AND ISNULL(@CurrentApproverRole, '''') <> ''Auditor''
    BEGIN
        RAISERROR(''Only the Auditor may reject a day. Managers return a day for correction instead.'', 16, 1);
        RETURN;
    END;

    IF @NormalizedAction = ''RETURN'' AND ISNULL(@CurrentApproverRole, '''') = ''Auditor''
    BEGIN
        RAISERROR(''The Auditor cannot return a day. Auditors approve or reject; managers return a day for correction.'', 16, 1);
        RETURN;
    END;

    -- Map action to persisted status words (aligned with ClaimStatuses.cs).
    SET @MappedDetailStatus =
        CASE @NormalizedAction
            WHEN ''APPROVE'' THEN ''Approved''
            WHEN ''RETURN''  THEN ''Returned''
            WHEN ''REJECT''  THEN ''Rejected''
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
            RAISERROR(''Unable to update approval request detail %d.'', 16, 1, @ApprovalRequestDetailId);
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
                (@NormalizedAction = ''APPROVE'' AND ISNULL(ClaimStatus, '''') NOT IN (''Approved'', ''Rejected''))
             OR (@NormalizedAction = ''RETURN''  AND ISNULL(ClaimStatus, '''') NOT IN (''Rejected''))
             OR (@NormalizedAction = ''REJECT''  AND ISNULL(ClaimStatus, '''') NOT IN (''Approved''))
          );

        --------------------------------------------------------------------
        -- 4) Mixed-state evaluation across latest detail versions only.
        --------------------------------------------------------------------
        SELECT
              @LatestDetailCount   = COUNT(1)
            , @PendingLatestCount  = SUM(CASE
                                              -- Unresolved at the current approver level (matches ClaimsApproval_v2 gatekeeper).
                                              WHEN ISNULL(ld.DetailStatus, '''') IN ('''', ''Pending'', ''Submitted'')
                                                  THEN 1
                                              WHEN ISNULL(ld.DetailStatus, '''') = ''Approved''
                                               AND ISNULL(ld.DetailApprovedLevel, 0) < ISNULL(@CurrentApproverLevel, 0)
                                                  THEN 1
                                              ELSE 0
                                          END)
            , @ReturnedLatestCount = SUM(CASE
                                              WHEN ISNULL(ld.DetailStatus, '''') = ''Returned'' THEN 1 ELSE 0
                                          END)
            , @RejectedLatestCount  = SUM(CASE
                                              WHEN ISNULL(ld.DetailStatus, '''') = ''Rejected'' THEN 1 ELSE 0
                                          END)
            , @ApprovedLatestCount  = SUM(CASE
                                              WHEN ISNULL(ld.DetailStatus, '''') = ''Approved'' THEN 1 ELSE 0
                                          END)
        FROM dbo.SalesApprovalRequestDetail ld
        INNER JOIN
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
        ) latest
            ON ld.ApprovalRequestDetailId = latest.LatestDetailId
        WHERE ld.ApprovalRequestId = @ApprovalRequestId;

        SET @NewRequestStatus = @OldRequestStatus;

        IF @ReturnedLatestCount > 0
            SET @NewRequestStatus = ''Returned'';
        ELSE IF @PendingLatestCount = 0
             AND (@ApprovedLatestCount + @RejectedLatestCount) = @LatestDetailCount
            SET @NewRequestStatus = ''Pending''; -- remain in workflow until forward / payment
        ELSE
            SET @NewRequestStatus = ISNULL(NULLIF(@OldRequestStatus, ''''), ''Pending'');

        --------------------------------------------------------------------
        -- 5) Approver + routing transitions.
        --------------------------------------------------------------------
        IF @NormalizedAction = ''RETURN''
        BEGIN
            -- Record the approver who returned the day.
            UPDATE dbo.SalesApprovalRequestApprover
            SET
                  Status     = ''Returned''
                , ActionBy   = @ActionBy
                , ActionDate = @NowUtc
                , Remarks    = ISNULL(@Remarks, Remarks)
            WHERE ApprovalRequestId = @ApprovalRequestId
              AND ApproverLevel = @CurrentApproverLevel;

            -- Request header reflects employee-correction state. Current approver is
            -- left unchanged here; usp_SubmitSalesDailyActivities resets to L1 on resubmit.
            UPDATE dbo.SalesApprovalRequest
            SET
                  Status                 = ''Returned''
                , ModifiedBy             = @ActionBy
                , ModifiedOn             = @NowUtc
            WHERE ApprovalRequestId = @ApprovalRequestId;

            SET @NewRequestStatus = ''Returned'';
        END
        ELSE IF @NormalizedAction = ''APPROVE''
        BEGIN
            -- When every latest detail is resolved at the current level (Pending/Submitted
            -- or Approved at a lower DetailApprovedLevel), complete the current approver
            -- and advance routing (manager -> auditor, etc.).
            IF @PendingLatestCount = 0 AND @ReturnedLatestCount = 0
            BEGIN
                UPDATE dbo.SalesApprovalRequestApprover
                SET
                      Status     = ''Approved''
                    , ActionBy   = @ActionBy
                    , ActionDate = @NowUtc
                    , Remarks    = ISNULL(@Remarks, Remarks)
                WHERE ApprovalRequestId = @ApprovalRequestId
                  AND ApproverLevel = @CurrentApproverLevel
                  AND Status = ''Pending''
                  -- Auditor with no higher required level keeps this row Pending
                  -- until Mark Ready for Payment. Routing and non-Auditor completion
                  -- still approve the current level here.
                  AND NOT (
                        ISNULL(@CurrentApproverRole, '''') = ''Auditor''
                        AND NOT EXISTS (
                            SELECT 1
                            FROM dbo.SalesApprovalRequestApprover nxt
                            WHERE nxt.ApprovalRequestId = @ApprovalRequestId
                              AND nxt.ApproverLevel > @CurrentApproverLevel
                              AND nxt.Status = ''Pending''
                              AND ISNULL(nxt.IsRequired, 1) = 1
                        )
                  );

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
                  AND a.Status = ''Pending''
                  AND ISNULL(a.IsRequired, 1) = 1
                ORDER BY a.ApproverLevel ASC;

                IF @NextApproverLevel IS NOT NULL
                BEGIN
                    -- Activate next approver; request stays Pending for the next review pass.
                    UPDATE dbo.SalesApprovalRequest
                    SET
                          CurrentApproverLevel    = @NextApproverLevel
                        , CurrentApproverLoginId  = @NextApproverLoginId
                        , Status                  = ''Pending''
                        , ModifiedBy              = @ActionBy
                        , ModifiedOn              = @NowUtc
                    WHERE ApprovalRequestId = @ApprovalRequestId;

                    SET @NewRequestStatus = ''Pending'';

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
                        , ISNULL(@OldRequestStatus, ''Pending'')
                        , ''Pending''
                        , @ActionBy
                        , CONCAT(
                              ''Level '',
                              @CurrentApproverLevel,
                              '' completed. Forwarded to '',
                              ISNULL(@NextApproverRole, ''Approver''),
                              '' ('',
                              ISNULL(@NextApproverLoginId, ''Pool''),
                              '').''
                          )
                    );
                END
                -- TODO (Sprint 2 - Task 02):
                -- Routing responsibility will be migrated to usp_ForwardApprovalRequest.
                -- Setting the header to Completed and clearing the routing pointer below is
                -- left fully functional so a claim can still reach Completed until that
                -- migration is complete.
                ELSE IF ISNULL(@CurrentApproverRole, '''') <> ''Auditor''
                BEGIN
                    -- Non-auditor final level with all days resolved -> Completed.
                    UPDATE dbo.SalesApprovalRequest
                    SET
                          Status                  = ''Completed''
                        , CurrentApproverLevel    = NULL
                        , CurrentApproverLoginId  = NULL
                        , ModifiedBy              = @ActionBy
                        , ModifiedOn              = @NowUtc
                    WHERE ApprovalRequestId = @ApprovalRequestId;

                    SET @NewRequestStatus = ''Completed'';

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
                        , ISNULL(@OldRequestStatus, ''Pending'')
                        , ''Completed''
                        , @ActionBy
                        , ''All approval levels finished. Awaiting payment processing.''
                    );
                END
                ELSE
                BEGIN
                    -- Auditor pass complete: keep the request Pending and still
                    -- assigned to this Auditor until Mark Ready for Payment.
                    -- EnsureHeaderCompletedForPayment writes Pending → Completed.
                    -- usp_MarkRequestReadyForPayment writes Completed → ReadyForPayment.
                    SET @NewRequestStatus = ''Pending'';
                END
            END
            ELSE
            BEGIN
                -- Partial approval: keep request Pending while days remain open.
                UPDATE dbo.SalesApprovalRequest
                SET
                      Status      = CASE WHEN @ReturnedLatestCount > 0 THEN ''Returned'' ELSE ''Pending'' END
                    , ModifiedBy  = @ActionBy
                    , ModifiedOn  = @NowUtc
                WHERE ApprovalRequestId = @ApprovalRequestId;

                SET @NewRequestStatus =
                    CASE WHEN @ReturnedLatestCount > 0 THEN ''Returned'' ELSE ''Pending'' END;
            END
        END
        ELSE -- REJECT
        BEGIN
            UPDATE dbo.SalesApprovalRequest
            SET
                  Status      = CASE WHEN @ReturnedLatestCount > 0 THEN ''Returned'' ELSE ''Pending'' END
                , ModifiedBy  = @ActionBy
                , ModifiedOn  = @NowUtc
            WHERE ApprovalRequestId = @ApprovalRequestId;

            SET @NewRequestStatus =
                CASE WHEN @ReturnedLatestCount > 0 THEN ''Returned'' ELSE ''Pending'' END;
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
                  ) latest
                      ON d.ApprovalRequestDetailId = latest.LatestDetailId
                  WHERE d.ApprovalRequestId = @ApprovalRequestId
                    AND ISNULL(d.DetailStatus, '''') NOT IN (''Rejected'', ''Returned'')
              )
            , ApprovedClaimAmount =
              (
                  SELECT ISNULL(SUM(d.Snapshot_CalculatedClaimAmount), 0)
                  FROM dbo.SalesApprovalRequestDetail d
                  INNER JOIN
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
                  ) latest
                      ON d.ApprovalRequestDetailId = latest.LatestDetailId
                  WHERE d.ApprovalRequestId = @ApprovalRequestId
                    AND ISNULL(d.DetailStatus, '''') NOT IN (''Rejected'', ''Returned'')
              )
            , ApprovedDAAmount =
              (
                  SELECT ISNULL(SUM(d.Snapshot_DAAmount), 0)
                  FROM dbo.SalesApprovalRequestDetail d
                  INNER JOIN
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
                  ) latest
                      ON d.ApprovalRequestDetailId = latest.LatestDetailId
                  WHERE d.ApprovalRequestId = @ApprovalRequestId
                    AND ISNULL(d.DetailStatus, '''') NOT IN (''Rejected'', ''Returned'')
              )
            , ApprovedHotelAmount =
              (
                  SELECT ISNULL(SUM(d.Snapshot_HotelAmount), 0)
                  FROM dbo.SalesApprovalRequestDetail d
                  INNER JOIN
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
                  ) latest
                      ON d.ApprovalRequestDetailId = latest.LatestDetailId
                  WHERE d.ApprovalRequestId = @ApprovalRequestId
                    AND ISNULL(d.DetailStatus, '''') NOT IN (''Rejected'', ''Returned'')
              )
            , ApprovedTravelAmount =
              (
                  SELECT ISNULL(SUM(d.Snapshot_TravelAmount), 0)
                  FROM dbo.SalesApprovalRequestDetail d
                  INNER JOIN
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
                  ) latest
                      ON d.ApprovalRequestDetailId = latest.LatestDetailId
                  WHERE d.ApprovalRequestId = @ApprovalRequestId
                    AND ISNULL(d.DetailStatus, '''') NOT IN (''Rejected'', ''Returned'')
              )
            , ModifiedBy = @ActionBy
            , ModifiedOn = @NowUtc
        WHERE ApprovalRequestId = @ApprovalRequestId;

        --------------------------------------------------------------------
        -- 7) Status history when header status changes (RETURN / completion).
        --------------------------------------------------------------------
        IF ISNULL(@OldRequestStatus, '''') <> ISNULL(@NewRequestStatus, '''')
           AND NOT (
               @NormalizedAction = ''APPROVE''
               AND @NewRequestStatus = ''Pending''
               AND ISNULL(@OldRequestStatus, ''Pending'') = ''Pending''
           )
           AND NOT (
               @NormalizedAction = ''APPROVE''
               AND @NewRequestStatus = ''Completed''
               AND ISNULL(@CurrentApproverRole, '''') = ''Auditor''
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
                , ISNULL(@OldRequestStatus, ''Pending'')
                , ISNULL(@NewRequestStatus, ''Pending'')
                , @ActionBy
                , CONCAT(
                      ''Detail '',
                      @ApprovalRequestDetailId,
                      '' '',
                      @NormalizedAction,
                      CASE
                          WHEN @Remarks IS NULL THEN ''''
                          ELSE CONCAT('': '', @Remarks)
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
';
DECLARE @ProcessPreimageHash varchar(64) = 'F6471A308779E26D74A2B84B00A6DAF71DAA84FDFD5A7C71189F0DA5B61E13EE';
DECLARE @SubmitPreimage nvarchar(max) = N'/*
    Object:      [dbo].[usp_SubmitSalesDailyActivities]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

/*==============================================================================
 Part 1 : Header, Validation & Mode Detection
 Version : Approval Engine V2 (Revised)
==============================================================================*/

CREATE   PROCEDURE dbo.usp_SubmitSalesDailyActivities

    @Updates dbo.TVP_ActivityUpdate READONLY,
    @LoginId NVARCHAR(100),
    @Year INT,
    @Month INT,
    @Remarks NVARCHAR(1000)=NULL,
    @L1Code NVARCHAR(100),
    @L2Code NVARCHAR(100)=NULL,
    @L3Code NVARCHAR(100)=NULL,
    @AuditorCode NVARCHAR(100)

AS
BEGIN

    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2 = SWITCHOFFSET(SYSDATETIMEOFFSET(),''+05:30'');
    DECLARE @ApprovalRequestId INT;
    DECLARE @SubmissionMode VARCHAR(20);
    DECLARE @ExistingRequestCount INT;

	DECLARE @TotalDistanceKM            DECIMAL(18,2)=0;
	DECLARE @TotalCalculatedClaimAmount DECIMAL(18,2)=0;

	DECLARE @TotalDAAmount              DECIMAL(18,2)=0;
	DECLARE @TotalHotelAmount           DECIMAL(18,2)=0;
	DECLARE @TotalTravelAmount          DECIMAL(18,2)=0;
	DECLARE @TotalStationaryAmount      DECIMAL(18,2)=0;

	DECLARE @TotalExpenseAmount         DECIMAL(18,2)=0;

    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (SELECT 1 FROM @Updates)
        BEGIN
            RAISERROR(''No activities supplied for submission.'',16,1);
            RETURN;
        END;

        IF @L1Code IS NULL OR @AuditorCode IS NULL
        BEGIN
            RAISERROR(''L1Code and AuditorCode are mandatory.'',16,1);
            RETURN;
        END;

        IF @L1Code=@AuditorCode
        BEGIN
            RAISERROR(''Approver hierarchy is invalid.'',16,1);
            RETURN;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.SalesDailyActivity a
            INNER JOIN @Updates u ON a.ActivityId=u.ActivityId
            WHERE a.Login_Id<>@LoginId
               OR YEAR(a.ActivityDate)<>@Year
               OR MONTH(a.ActivityDate)<>@Month
        )
        BEGIN
            RAISERROR(''Activities do not belong to the selected employee/month.'',16,1);
            RETURN;
        END;

        -----------------------------------------------------------------
        -- One active header per employee/year/month.
        -- Returned, Pending, Completed, ReadyForPayment are active.
        -- Rejected is a closed predecessor and must not be reused.
        -- UPDLOCK, HOLDLOCK is held until COMMIT/ROLLBACK.
        -----------------------------------------------------------------
        DECLARE @ActiveMonthRequest TABLE
        (
            ApprovalRequestId INT NOT NULL PRIMARY KEY,
            Status NVARCHAR(50) NOT NULL
        );

        INSERT INTO @ActiveMonthRequest (ApprovalRequestId, Status)
        SELECT r.ApprovalRequestId, r.Status
        FROM dbo.SalesApprovalRequest r WITH (UPDLOCK, HOLDLOCK)
        WHERE r.RequesterLoginId = @LoginId
          AND r.YearNo = @Year
          AND r.MonthNo = @Month
          AND UPPER(LTRIM(RTRIM(r.Status))) IN
              (N''RETURNED'', N''PENDING'', N''COMPLETED'', N''READYFORPAYMENT'');

        DECLARE @ActiveHeaderCount INT = (SELECT COUNT(*) FROM @ActiveMonthRequest);
        DECLARE @ActiveRequestId INT = NULL;
        DECLARE @ActiveStatus NVARCHAR(50) = NULL;

        IF @ActiveHeaderCount > 1
        BEGIN
            IF @@TRANCOUNT > 0 ROLLBACK TRAN;
            RAISERROR(''Multiple active approval requests exist for this employee and month.'', 16, 1);
            RETURN;
        END;

        IF @ActiveHeaderCount = 1
            SELECT @ActiveRequestId = ApprovalRequestId, @ActiveStatus = Status
            FROM @ActiveMonthRequest;

        SELECT
            @ExistingRequestCount = COUNT(DISTINCT a.ApprovalRequestId),
            @ApprovalRequestId = MAX(a.ApprovalRequestId)
        FROM dbo.SalesDailyActivity a
        INNER JOIN @Updates u ON a.ActivityId = u.ActivityId
        WHERE a.ApprovalRequestId IS NOT NULL;

        IF @ExistingRequestCount > 1
        BEGIN
            IF @@TRANCOUNT > 0 ROLLBACK TRAN;
            RAISERROR(''Activities belong to multiple approval requests.'', 16, 1);
            RETURN;
        END;

        DECLARE @ResolvedStatus NVARCHAR(50) = NULL;
        DECLARE @ClosedActivityRequest BIT = 0;

        IF @ExistingRequestCount = 1
        BEGIN
            SELECT @ResolvedStatus = r.Status
            FROM dbo.SalesApprovalRequest r WITH (UPDLOCK, HOLDLOCK)
            WHERE r.ApprovalRequestId = @ApprovalRequestId
              AND r.RequesterLoginId = @LoginId
              AND r.YearNo = @Year
              AND r.MonthNo = @Month;

            IF UPPER(LTRIM(RTRIM(ISNULL(@ResolvedStatus, N'''')))) IN (N''COMPLETED'', N''READYFORPAYMENT'')
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRAN;
                RAISERROR(''A finalized request already exists for this month and cannot be submitted.'', 16, 1);
                RETURN;
            END;

            -- A Rejected (or missing) id is not an active lifecycle.
            IF UPPER(LTRIM(RTRIM(ISNULL(@ResolvedStatus, N'''')))) NOT IN (N''RETURNED'', N''PENDING'')
            BEGIN
                SET @ClosedActivityRequest = 1;
                SET @ApprovalRequestId = NULL;
                SET @ExistingRequestCount = 0;
            END;
        END;

        IF @ExistingRequestCount = 0 AND @ActiveHeaderCount = 1
        BEGIN
            IF @ClosedActivityRequest = 1
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRAN;
                RAISERROR(''Submitted activities belong to a closed approval request and cannot be attached to the active request for this month.'', 16, 1);
                RETURN;
            END;

            IF UPPER(LTRIM(RTRIM(@ActiveStatus))) = N''RETURNED''
            BEGIN
                SET @ApprovalRequestId = @ActiveRequestId;
                SET @ExistingRequestCount = 1;
            END
            ELSE IF UPPER(LTRIM(RTRIM(@ActiveStatus))) = N''PENDING''
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRAN;
                RAISERROR(''A pending request already exists.'', 16, 1);
                RETURN;
            END
            ELSE
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRAN;
                RAISERROR(''A finalized request already exists for this month and cannot be submitted.'', 16, 1);
                RETURN;
            END;
        END;

        SET @SubmissionMode =
            CASE WHEN @ExistingRequestCount = 0 THEN ''NEW''
                 ELSE ''RESUBMIT'' END;

        IF @SubmissionMode = ''RESUBMIT''
        BEGIN
            IF EXISTS
            (
                SELECT 1
                FROM dbo.SalesDailyActivity a
                INNER JOIN @Updates u ON a.ActivityId=u.ActivityId
                WHERE a.ClaimStatus<>''Returned''
            )
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRAN;
                RAISERROR(''Resubmission can contain only Returned activities.'',16,1);
                RETURN;
            END;

            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.SalesApprovalRequest
                WHERE ApprovalRequestId=@ApprovalRequestId
                  AND RequesterLoginId=@LoginId
            )
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRAN;
                RAISERROR(''Approval Request not found.'',16,1);
                RETURN;
            END;

            UPDATE dbo.SalesApprovalRequest
            SET Status=''Pending'',
                RequestRemarks=@Remarks,
                ModifiedOn=@Now,
                ModifiedBy=@LoginId,
                CurrentApproverLoginId=@L1Code,
                CurrentApproverLevel=1
            WHERE ApprovalRequestId=@ApprovalRequestId;

            -- Bind returned activities that do not already carry this request
            -- before later snapshot rows are inserted for the same id.
            UPDATE a
            SET a.ApprovalRequestId = @ApprovalRequestId
            FROM dbo.SalesDailyActivity a
            INNER JOIN @Updates u ON a.ActivityId = u.ActivityId
            WHERE a.Login_Id = @LoginId
              AND YEAR(a.ActivityDate) = @Year
              AND MONTH(a.ActivityDate) = @Month
              AND a.ClaimStatus = ''Returned''
              AND a.ApprovalRequestId IS NULL;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.SalesApprovalRequest
            (
                RequesterLoginId,RequestDate,YearNo,MonthNo,Status,
                RequestRemarks,CreatedOn,CreatedBy
            )
            VALUES
            (
                @LoginId,@Now,@Year,@Month,''Pending'',
                @Remarks,@Now,@LoginId
            );

            SET @ApprovalRequestId=SCOPE_IDENTITY();
        END;

        /* End of Revised Part 1 */


		/*==============================================================================
	 Part 2 : Activity Update, Expense Aggregation & Immutable Snapshot
	==============================================================================*/

		-------------------------------------------------------------------------
		-- Update Activities
		-------------------------------------------------------------------------
		UPDATE a
		SET
			a.TotalDistanceKM          = u.TotalDistanceKM,
			a.CalculatedClaimAmount    = u.CalculatedClaimAmount,
			a.ClaimStatus              = ''Submitted'',
			a.SubmittedBy              = @LoginId,
			a.SubmittedOn              = @Now,
			a.ApprovalRequestId        = @ApprovalRequestId,
			a.ModifiedByUserId         = @LoginId,
			a.ModifiedDateTime         = @Now
		FROM dbo.SalesDailyActivity a
		INNER JOIN @Updates u
			ON a.ActivityId = u.ActivityId
		WHERE
			a.Login_Id = @LoginId
		AND YEAR(a.ActivityDate) = @Year
		AND MONTH(a.ActivityDate) = @Month
		AND a.ClaimStatus IN (''Draft'', ''Returned'');


		IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @Updates)
		BEGIN
			RAISERROR
			(
				''One or more activities are not eligible for submission.'',
				16,
				1
			);

			RETURN;
		END;
		-------------------------------------------------------------------------
		-- Update Expense Status
		-------------------------------------------------------------------------

		UPDATE e
		SET
			e.ClaimStatus = ''Submitted'',
			e.ModifiedOn  = @Now,
			e.ModifiedBy  = @LoginId
		FROM dbo.SalesDailyExpense e
		INNER JOIN @Updates u
			ON e.ActivityId = u.ActivityId
		WHERE
			e.IsDeleted = 0
		AND
			e.ClaimStatus IN (''Draft'',''Returned'');

		-------------------------------------------------------------------------
		-- Expense Aggregation
		-------------------------------------------------------------------------

    CREATE TABLE #ExpenseAgg
    (
        ActivityId          INT             NOT NULL PRIMARY KEY,

        DAAmount            DECIMAL(18,2)   NOT NULL DEFAULT(0),

        HotelAmount         DECIMAL(18,2)   NOT NULL DEFAULT(0),

        TravelAmount        DECIMAL(18,2)   NOT NULL DEFAULT(0),

        StationaryAmount    DECIMAL(18,2)   NOT NULL DEFAULT(0),

        ExpenseTotal        DECIMAL(18,2)   NOT NULL DEFAULT(0)
    );

    INSERT INTO #ExpenseAgg
    (
        ActivityId,
        DAAmount,
        HotelAmount,
        TravelAmount,
        StationaryAmount,
        ExpenseTotal
    )
    SELECT

        e.ActivityId,

        SUM(CASE WHEN e.ExpenseType=''DA''
                 THEN ISNULL(e.Amount,0)
                 ELSE 0 END),

        SUM(CASE WHEN e.ExpenseType=''Hotel''
                 THEN ISNULL(e.Amount,0)
                 ELSE 0 END),

        SUM(CASE WHEN e.ExpenseType=''Travel''
                 THEN ISNULL(e.Amount,0)
                 ELSE 0 END),

        SUM(CASE WHEN e.ExpenseType=''Stationary''
                 THEN ISNULL(e.Amount,0)
                 ELSE 0 END),

        SUM(CASE WHEN e.ExpenseType = ''DA'' THEN ISNULL(e.Amount, 0) ELSE 0 END)
      + SUM(CASE WHEN e.ExpenseType = ''Hotel'' THEN ISNULL(e.Amount, 0) ELSE 0 END)
      + SUM(CASE WHEN e.ExpenseType = ''Travel'' THEN ISNULL(e.Amount, 0) ELSE 0 END)
      + SUM(CASE WHEN e.ExpenseType = ''Stationary'' THEN ISNULL(e.Amount, 0) ELSE 0 END) AS ExpenseTotal

    FROM dbo.SalesDailyExpense e

    INNER JOIN @Updates u
        ON e.ActivityId = u.ActivityId

    WHERE
        e.IsDeleted = 0
    AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''

    GROUP BY
        e.ActivityId;

    -------------------------------------------------------------------------
    -- Latest Detail Snapshot
    -------------------------------------------------------------------------

    ;WITH LatestDetailSnapshot AS
    (
        SELECT

            d.ActivityId,

            d.ApprovalRequestDetailId,

            d.DetailVersion,

            ROW_NUMBER() OVER
            (
                PARTITION BY d.ActivityId
                ORDER BY
                    d.DetailVersion DESC,
                    d.ApprovalRequestDetailId DESC
            ) AS RN

        FROM dbo.SalesApprovalRequestDetail d

        WHERE
            d.ApprovalRequestId = @ApprovalRequestId
    )

    -------------------------------------------------------------------------
    -- Insert Immutable Snapshot
    -------------------------------------------------------------------------

    INSERT INTO dbo.SalesApprovalRequestDetail
    (
        ApprovalRequestId,

        ActivityId,

        Snapshot_TotalDistanceKM,

        Snapshot_CalculatedClaimAmount,

        Snapshot_DAAmount,

        Snapshot_HotelAmount,

        Snapshot_TravelAmount,

        Snapshot_StationaryAmount,

        Snapshot_ExpenseTotal,

        DetailVersion,

        PreviousDetailId,

        DetailStatus,

        CreatedOn,

        CreatedBy
    )

    SELECT

        @ApprovalRequestId,

        a.ActivityId,

        a.TotalDistanceKM,

        a.CalculatedClaimAmount,

        ISNULL(ea.DAAmount,0),

        ISNULL(ea.HotelAmount,0),

        ISNULL(ea.TravelAmount,0),

        ISNULL(ea.StationaryAmount,0),

        ISNULL(ea.ExpenseTotal,0),

        ISNULL(lds.DetailVersion,0)+1,

        lds.ApprovalRequestDetailId,

        ''Pending'',

        @Now,

        @LoginId

    FROM dbo.SalesDailyActivity a

    INNER JOIN @Updates u
        ON a.ActivityId=u.ActivityId

    LEFT JOIN #ExpenseAgg ea
        ON a.ActivityId=ea.ActivityId

    LEFT JOIN LatestDetailSnapshot lds
        ON a.ActivityId=lds.ActivityId
       AND lds.RN=1;

    -------------------------------------------------------------------------
    -- Build Latest Snapshot (For Part 3)
    -------------------------------------------------------------------------

    SELECT

        d.ActivityId,

        d.DetailVersion,

        d.Snapshot_TotalDistanceKM,

        d.Snapshot_CalculatedClaimAmount,

        d.Snapshot_DAAmount,

        d.Snapshot_HotelAmount,

        d.Snapshot_TravelAmount,

        d.Snapshot_StationaryAmount,

        d.Snapshot_ExpenseTotal

    INTO #LatestSnapshot

    FROM
    (
        SELECT

            d.*,

            ROW_NUMBER() OVER
            (
                PARTITION BY d.ActivityId
                ORDER BY
                    d.DetailVersion DESC,
                    d.ApprovalRequestDetailId DESC
            ) AS RN

        FROM dbo.SalesApprovalRequestDetail d

        WHERE
            d.ApprovalRequestId=@ApprovalRequestId

    ) d

    WHERE
        d.RN=1;

    /*-----------------------------------------------------------------------
      End of Part 2
    -----------------------------------------------------------------------*/

	/*==============================================================================
 Part 3 : Request Totals, Approver Processing & Workflow History
==============================================================================*/

    -------------------------------------------------------------------------
    -- Calculate Request Totals
    -------------------------------------------------------------------------

    ;WITH LatestSnapshot AS
    (
        SELECT

            d.ActivityId,

            d.Snapshot_TotalDistanceKM,

            d.Snapshot_CalculatedClaimAmount,

            d.Snapshot_DAAmount,

            d.Snapshot_HotelAmount,

            d.Snapshot_TravelAmount,

            d.Snapshot_StationaryAmount,

            d.Snapshot_ExpenseTotal,

            ROW_NUMBER() OVER
            (
                PARTITION BY d.ActivityId
                ORDER BY
                    d.DetailVersion DESC,
                    d.ApprovalRequestDetailId DESC
            ) AS RN

        FROM dbo.SalesApprovalRequestDetail d

        WHERE
            d.ApprovalRequestId = @ApprovalRequestId
    )

    SELECT

        @TotalDistanceKM =
            SUM(Snapshot_TotalDistanceKM),

        @TotalCalculatedClaimAmount =
            SUM(Snapshot_CalculatedClaimAmount),

        @TotalDAAmount =
            SUM(Snapshot_DAAmount),

        @TotalHotelAmount =
            SUM(Snapshot_HotelAmount),

        @TotalTravelAmount =
            SUM(Snapshot_TravelAmount),

        @TotalStationaryAmount =
            SUM(Snapshot_StationaryAmount),

        @TotalExpenseAmount =
            SUM(Snapshot_ExpenseTotal)

    FROM LatestSnapshot

    WHERE
        RN = 1;

    -------------------------------------------------------------------------
    -- Update Request Header
    -------------------------------------------------------------------------

    UPDATE dbo.SalesApprovalRequest
	SET
		TotalKM                = @TotalDistanceKM,
		TotalClaimAmount       = @TotalCalculatedClaimAmount,
		TotalDAAmount          = @TotalDAAmount,
		TotalHotelAmount       = @TotalHotelAmount,
		TotalTravelAmount      = @TotalTravelAmount,
		TotalStationaryAmount  = @TotalStationaryAmount,
		TotalExpenseAmount     = @TotalExpenseAmount,

		TotalActivities =
		(
			SELECT COUNT(*)
			FROM dbo.SalesApprovalRequestDetail
			WHERE ApprovalRequestId = @ApprovalRequestId
			  AND DetailVersion =
			  (
				  SELECT MAX(d2.DetailVersion)
				  FROM dbo.SalesApprovalRequestDetail d2
				  WHERE d2.ApprovalRequestId = dbo.SalesApprovalRequestDetail.ApprovalRequestId
					AND d2.ActivityId = dbo.SalesApprovalRequestDetail.ActivityId
			  )
		),

		ModifiedOn = @Now,
		ModifiedBy = @LoginId

	WHERE ApprovalRequestId = @ApprovalRequestId;

    -------------------------------------------------------------------------
    -- Approver Processing
    -------------------------------------------------------------------------

    -------------------------------------------------------------------------
-- Create Approver Chain (NEW REQUEST)
-------------------------------------------------------------------------

IF @SubmissionMode = ''NEW''
BEGIN

    -- Level 1 (Mandatory)
    INSERT INTO dbo.SalesApprovalRequestApprover
    (
        ApprovalRequestId,
        ApproverLoginId,
        ApproverRole,
        ApproverLevel,
        IsRequired,
        Status,
        CreatedOn,
        CreatedBy
    )
    VALUES
    (
        @ApprovalRequestId,
        @L1Code,
        ''L1'',
        1,
        1,
        ''Pending'',
        @Now,
        @LoginId
    );

    -- Level 2 (Optional)
    IF @L2Code IS NOT NULL
    BEGIN
        INSERT INTO dbo.SalesApprovalRequestApprover
        (
            ApprovalRequestId,
            ApproverLoginId,
            ApproverRole,
            ApproverLevel,
            IsRequired,
            Status,
            CreatedOn,
            CreatedBy
        )
        VALUES
        (
            @ApprovalRequestId,
            @L2Code,
            ''L2'',
            2,
            1,
            ''Pending'',
            @Now,
            @LoginId
        );
    END;

    -- Level 3 (Optional)
    IF @L3Code IS NOT NULL
    BEGIN
        INSERT INTO dbo.SalesApprovalRequestApprover
        (
            ApprovalRequestId,
            ApproverLoginId,
            ApproverRole,
            ApproverLevel,
            IsRequired,
            Status,
            CreatedOn,
            CreatedBy
        )
        VALUES
        (
            @ApprovalRequestId,
            @L3Code,
            ''L3'',
            3,
            1,
            ''Pending'',
            @Now,
            @LoginId
        );
    END;

    -- Auditor (Mandatory)
    INSERT INTO dbo.SalesApprovalRequestApprover
    (
        ApprovalRequestId,
        ApproverLoginId,
        ApproverRole,
        ApproverLevel,
        IsRequired,
        Status,
        CreatedOn,
        CreatedBy
    )
    VALUES
    (
        @ApprovalRequestId,
        @AuditorCode,
        ''Auditor'',
        CASE
            WHEN @L3Code IS NOT NULL THEN 4
            WHEN @L2Code IS NOT NULL THEN 3
            ELSE 2
        END,
        1,
        ''Pending'',
        @Now,
        @LoginId
    );

END
ELSE
BEGIN

    ---------------------------------------------------------------------
    -- Reset existing approvers (RESUBMIT)
    ---------------------------------------------------------------------

    UPDATE dbo.SalesApprovalRequestApprover
    SET
        Status      = ''Pending'',
        ActionBy    = NULL,
        ActionDate  = NULL,
        Remarks     = NULL,
        ModifiedOn  = @Now,
        ModifiedBy  = @LoginId
    WHERE ApprovalRequestId = @ApprovalRequestId;

END;

    -------------------------------------------------------------------------
    -- Reset Current Approver
    -------------------------------------------------------------------------

    UPDATE dbo.SalesApprovalRequest

    SET

        CurrentApproverLoginId=@L1Code,

        CurrentApproverLevel=1

    WHERE
        ApprovalRequestId=@ApprovalRequestId;

    -------------------------------------------------------------------------
    -- Workflow History
    -------------------------------------------------------------------------

    INSERT INTO dbo.ApprovalRequestStatusHistory
    (
        ApprovalRequestId,

        OldStatus,

        NewStatus,

        ChangedBy,

        ChangedOn,

        Remarks
    )
    VALUES
    (
        @ApprovalRequestId,

        CASE
            WHEN @SubmissionMode=''NEW''
                THEN NULL
            ELSE ''Returned''
        END,

        ''Pending'',

        @LoginId,

        @Now,

        CASE
            WHEN @SubmissionMode=''NEW''
                THEN ''Initial Submission''
            ELSE ''Resubmitted after corrections''
        END
    );

	/*==============================================================================
 Part 4 : Commit / Rollback / Error Handling
==============================================================================*/

        ---------------------------------------------------------------------
        -- Commit Transaction
        ---------------------------------------------------------------------

        COMMIT TRAN;

        ---------------------------------------------------------------------
        -- Success Result
        ---------------------------------------------------------------------

        SELECT

            1 AS Success,

            @ApprovalRequestId AS ApprovalRequestId,

            @SubmissionMode AS SubmissionMode,

            ''Claim submitted successfully.'' AS Message;

    END TRY

    BEGIN CATCH

    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    DECLARE @ErrMsg NVARCHAR(MAX);

    SET @ErrMsg =
        CONCAT(
            ''usp_SubmitSalesDailyActivities failed.'',
            CHAR(13),
            ''Error : '', ERROR_MESSAGE(),
            CHAR(13),
            ''Procedure : '', ISNULL(ERROR_PROCEDURE(), ''N/A''),
            CHAR(13),
            ''Line : '', ERROR_LINE()
        );

    RAISERROR(@ErrMsg, 16, 1);

END CATCH;

END
';
DECLARE @SubmitPreimageHash varchar(64) = '5BEDA23C85279789D325C66B573C37BB1E037DA5C0153695A435DEA9E7D37547';
DECLARE @RecalcPreimage nvarchar(max) = N'/* ============================================================================
   NAME:        fsot_request_canonical_totals
   WHEN:        2026-09-15
   WHY:         Request approved totals were recomputed in C# from multiple formulas
   WHAT:        usp_RecalculateApprovalRequest refreshes latest day snapshots and writes header Approved* + FinalApprovedAmount
   ============================================================================ */

/*
    Object:      [dbo].[usp_RecalculateApprovalRequest]
    Type:        STORED PROCEDURE
    Purpose:     FSOT request-level roll-up from latest SalesApprovalRequestDetail snapshots
*/
CREATE PROCEDURE dbo.usp_RecalculateApprovalRequest
(
    @ApprovalRequestId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @ApprovalRequestId IS NULL
    BEGIN
        RAISERROR(''ApprovalRequestId cannot be NULL.'', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId
    )
    BEGIN
        RAISERROR(''Approval Request not found.'', 16, 1);
        RETURN;
    END;

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
        , @FinalApprovedAmount     DECIMAL(18,2) = 0;

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
        , ModifiedOn           = SYSUTCDATETIME()
    WHERE ApprovalRequestId = @ApprovalRequestId;
END
';
DECLARE @RecalcPreimageHash varchar(64) = 'D14FEF7DFF5F8C4E008B2485D445B897CA4B76FBB60D6E5CEF68745894BE5A9D';
DECLARE @MarkPreimage nvarchar(max) = N'
-- 7. dbo.usp_MarkRequestReadyForPayment
/* ============================================================================
   NAME:        mark_ready_named_history_insert
   WHEN:        2026-09-15
   WHY:         Positional history INSERT failed after schema drift so ReadyForPayment never persisted
   WHAT:        usp_MarkRequestReadyForPayment history write uses explicit column list
   ============================================================================ */

/*
    Object:      [dbo].[usp_MarkRequestReadyForPayment]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
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

    DECLARE @NowUtc DATETIME2(0) = SYSUTCDATETIME();

    /* 1️⃣ Auditor validation */
    IF NOT EXISTS (
        SELECT 1
        FROM SalesApprovalRequestApprover
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND ApproverLoginId = @ActionBy
          AND ApproverRole = ''Auditor''
    )
    BEGIN
        RAISERROR(''Only Auditor can mark Ready for Payment.'',16,1);
        RETURN;
    END

    /* 2️⃣ Must be Completed.
          EnsureHeaderCompletedForPayment writes Pending → Completed before this call. */
    IF NOT EXISTS (
        SELECT 1
        FROM SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND Status = ''Completed''
    )
    BEGIN
        RAISERROR(''Request not completed.'',16,1);
        RETURN;
    END

    /* 3️⃣ Consume the Auditor assignment that HF231 left Pending. */
    UPDATE dbo.SalesApprovalRequestApprover
    SET
          Status     = ''Approved''
        , ActionBy   = @ActionBy
        , ActionDate = @NowUtc
        , Remarks    = ISNULL(@Remarks, Remarks)
    WHERE ApprovalRequestId = @ApprovalRequestId
      AND ApproverLoginId = @ActionBy
      AND ApproverRole = ''Auditor''
      AND Status = ''Pending'';

    /* 4️⃣ Update header */
    UPDATE SalesApprovalRequest
    SET
        Status = ''ReadyForPayment'',
        ModifiedBy = @ActionBy,
        ModifiedOn = SYSUTCDATETIME()
    WHERE ApprovalRequestId = @ApprovalRequestId;

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
        , ''Completed''
        , ''ReadyForPayment''
        , @ActionBy
        , ''Auditor marked Ready for Payment''
    );
END
';
DECLARE @MarkPreimageHash varchar(64) = '8B2ECBCE8AC258D67A2C3CFCC6C45CC385F01354B8F5D8B5594409889685E158';

IF CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @MarkPreimage)), 2) <> @MarkPreimageHash
   OR LEN(@MarkPreimage) <> 2882
    THROW 58313, N'ABORT: embedded MarkReady preimage does not match the reviewed hash. Procedure not changed.', 1;

IF @Execute <> 1
   OR @RollbackApproved <> 1
   OR @AllFivePreimagesPresent <> 1
   OR @ExpensePreimage IS NULL
   OR LEN(@ExpensePreimageHash) <> 64
   OR @ProcessPreimage IS NULL
   OR LEN(@ProcessPreimageHash) <> 64
   OR @SubmitPreimage IS NULL
   OR LEN(@SubmitPreimageHash) <> 64
   OR @RecalcPreimage IS NULL
   OR LEN(@RecalcPreimageHash) <> 64
   OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @ExpensePreimage)), 2) <> @ExpensePreimageHash
   OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @ProcessPreimage)), 2) <> @ProcessPreimageHash
   OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @SubmitPreimage)), 2) <> @SubmitPreimageHash
   OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @RecalcPreimage)), 2) <> @RecalcPreimageHash
BEGIN
    PRINT N'NOT RUN | L2-R19 five-procedure UAT rollback did not read the catalog and did not alter any procedure.';
    PRINT N'BLOCKED | Four UAT preimages are embedded and hash-checked. @Execute, @RollbackApproved, and @AllFivePreimagesPresent are still 0.';
    PRINT N'NOT RUN | Catalog was not read. No procedure was altered.';
    PRINT N'VERIFIED | usp_MarkRequestReadyForPayment preimage hash matches 8B2ECBCE8AC258D67A2C3CFCC6C45CC385F01354B8F5D8B5594409889685E158.';
    RETURN;
END;

BEGIN TRY
    IF @@TRANCOUNT <> 0
        THROW 58303, N'ABORT: this script must start with no open transaction. Procedure not changed.', 1;

    IF DB_NAME() = N'AnmolDristi'
        THROW 58301, N'ABORT: Live database AnmolDristi is forbidden. Procedure not changed.', 1;

    IF UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
        THROW 58304, N'ABORT: server is not MUMANMOAPP01\SQLEXPRESS. Procedure not changed.', 1;

    IF DB_NAME() <> N'AnmolDristiDev'
        THROW 58302, N'ABORT: connected database is not AnmolDristiDev. Procedure not changed.', 1;

    DECLARE @ExpenseId int = OBJECT_ID(N'dbo.usp_UpdateSalesDailyExpenseStatus', N'P');
    DECLARE @ProcessId int = OBJECT_ID(N'dbo.usp_ProcessApprovalAction', N'P');
    DECLARE @SubmitId int = OBJECT_ID(N'dbo.usp_SubmitSalesDailyActivities', N'P');
    DECLARE @RecalcId int = OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest', N'P');
    DECLARE @MarkId int = OBJECT_ID(N'dbo.usp_MarkRequestReadyForPayment', N'P');

    IF @ExpenseId IS NULL OR @ProcessId IS NULL OR @SubmitId IS NULL OR @RecalcId IS NULL OR @MarkId IS NULL
        THROW 58305, N'ABORT: one of the five procedures is missing. Procedure not changed.', 1;

    DECLARE @ExpenseLive nvarchar(max) = OBJECT_DEFINITION(@ExpenseId);
    DECLARE @ProcessLive nvarchar(max) = OBJECT_DEFINITION(@ProcessId);
    DECLARE @SubmitLive nvarchar(max) = OBJECT_DEFINITION(@SubmitId);
    DECLARE @RecalcLive nvarchar(max) = OBJECT_DEFINITION(@RecalcId);
    DECLARE @MarkLive nvarchar(max) = OBJECT_DEFINITION(@MarkId);

    IF CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @ExpenseLive)), 2) <> '7C9007750F36C24F696B56EBC1AD7FC0A2CEC6441AD45A00815934D4FBC7E040'
       OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @ProcessLive)), 2) <> '3E86754A9C605F752468C616D9923BD7B22F2396FE84363FAAB0E5CFBDA04EB9'
       OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @SubmitLive)), 2) <> '18B9D5854ED396CCDD2CCC94AD80E6904FA1891C9DF3F907BD94D312F1A472BF'
       OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @RecalcLive)), 2) <> '3ECC03CBD6C1AFF40CF7F6B7ECF5E637399A7D07B4A6AA4AB1FB219231F7900F'
       OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @MarkLive)), 2) <> '118A8333EC49E8C11AAA2BCE46F4490A3DE7D11FD9EC84E01D6990DA8EBB4274'
        THROW 58314, N'ABORT: live modules are not the five-procedure post-apply text. Procedure not changed.', 1;

    DECLARE @ExpenseSubmit nvarchar(max) = STUFF(@ExpensePreimage, CHARINDEX(N'CREATE', @ExpensePreimage), 6, N'ALTER');
    DECLARE @ProcessSubmit nvarchar(max) = STUFF(@ProcessPreimage, CHARINDEX(N'CREATE', @ProcessPreimage), 6, N'ALTER');
    DECLARE @SubmitSubmit nvarchar(max) = STUFF(@SubmitPreimage, CHARINDEX(N'CREATE', @SubmitPreimage), 6, N'ALTER');
    DECLARE @RecalcSubmit nvarchar(max) = STUFF(@RecalcPreimage, CHARINDEX(N'CREATE', @RecalcPreimage), 6, N'ALTER');
    DECLARE @MarkSubmit nvarchar(max) = STUFF(@MarkPreimage, CHARINDEX(N'CREATE', @MarkPreimage), 6, N'ALTER');

    BEGIN TRANSACTION;
    EXEC sys.sp_executesql @RecalcSubmit;
    EXEC sys.sp_executesql @ProcessSubmit;
    EXEC sys.sp_executesql @ExpenseSubmit;
    EXEC sys.sp_executesql @SubmitSubmit;
    EXEC sys.sp_executesql @MarkSubmit;

    IF OBJECT_DEFINITION(@ExpenseId) <> @ExpensePreimage
       OR OBJECT_DEFINITION(@ProcessId) <> @ProcessPreimage
       OR OBJECT_DEFINITION(@SubmitId) <> @SubmitPreimage
       OR OBJECT_DEFINITION(@RecalcId) <> @RecalcPreimage
       OR OBJECT_DEFINITION(@MarkId) <> @MarkPreimage
        THROW 58331, N'ABORT: rollback did not restore the reviewed preimage. Transaction rolled back.', 1;

    COMMIT TRANSACTION;
    PRINT N'ROLLED BACK | Five UAT procedures match the reviewed preimages. Claim rows were not restored.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH