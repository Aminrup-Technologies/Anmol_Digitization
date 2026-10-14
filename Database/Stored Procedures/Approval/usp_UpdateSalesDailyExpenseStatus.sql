/* ============================================================================
   NAME:        expense_return_opens_employee_correction
   WHEN:        2026-10-13
   WHY:         Returning one expense set only SalesDailyExpense.ClaimStatus.
                The latest detail, activity, header, and L1 approver stayed
                Pending. Forward then refused the Returned expense, and the
                employee page only reopens a day whose latest detail is
                Returned. The receipt could not be corrected or resubmitted.
   WHAT:        A Returned expense calls dbo.usp_ProcessApprovalAction for that
                latest detail with RETURN, in the same transaction as the
                expense update. That is the existing day-return path: latest
                detail, activity, non-rejected expenses on that day, the L1
                approver row, and the header become Returned, then recalc runs
                before commit. Approve and Reject still update the one expense,
                then recalc writes FinalApprovedAmount before commit.
   ============================================================================ */

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

CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateSalesDailyExpenseStatus]
(
    @ExpenseId INT,
    @NewStatus NVARCHAR(50), -- 'Approved', 'Rejected', or 'Returned'
    @ActionBy  NVARCHAR(100),
    @Remarks   NVARCHAR(MAX) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
          @NormalizedStatus         NVARCHAR(50) = UPPER(LTRIM(RTRIM(ISNULL(@NewStatus, N''))))
        , @ActivityId               INT
        , @ApprovalRequestDetailId  INT
        , @ApprovalRequestId        INT
        , @RequestStatus            NVARCHAR(50)
        , @CurrentApproverLoginId   NVARCHAR(100)
        , @CurrentApproverLevel     INT
        , @CurrentApproverRole      NVARCHAR(100);

    IF @ExpenseId IS NULL OR @ExpenseId <= 0
    BEGIN
        RAISERROR('ExpenseId is required.', 16, 1);
        RETURN;
    END;

    IF NULLIF(LTRIM(RTRIM(@ActionBy)), N'') IS NULL
    BEGIN
        RAISERROR('ActionBy is required.', 16, 1);
        RETURN;
    END;

    IF @NormalizedStatus NOT IN (N'APPROVED', N'REJECTED', N'RETURNED')
    BEGIN
        RAISERROR('Invalid expense status. Expected Approved, Rejected, or Returned.', 16, 1);
        RETURN;
    END;

    -- Canonical persisted status words
    SET @NewStatus =
        CASE @NormalizedStatus
            WHEN N'APPROVED' THEN N'Approved'
            WHEN N'REJECTED' THEN N'Rejected'
            WHEN N'RETURNED' THEN N'Returned'
        END;

    -- 1️⃣ Safety Check: Prevent update if deleted / missing
    IF NOT EXISTS (SELECT 1 FROM dbo.SalesDailyExpense WHERE ExpenseId = @ExpenseId)
    BEGIN
        RAISERROR('Expense %d was not found.', 16, 1, @ExpenseId);
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.SalesDailyExpense WHERE ExpenseId = @ExpenseId AND IsDeleted = 1)
    BEGIN
        RAISERROR('Cannot update a deleted expense.', 16, 1);
        RETURN;
    END;

    SELECT @ActivityId = ActivityId
    FROM dbo.SalesDailyExpense
    WHERE ExpenseId = @ExpenseId;

    IF @ActivityId IS NULL
    BEGIN
        RAISERROR('Expense %d is not linked to an activity.', 16, 1, @ExpenseId);
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
        RAISERROR('No approval request detail found for activity %d.', 16, 1, @ActivityId);
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

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
    IF ISNULL(@RequestStatus, N'') IN (N'Completed', N'ReadyForPayment')
        THROW 59131, 'This approval request is locked for payment and cannot be modified.', 1;

    --------------------------------------------------------------------
    -- Header status: expense actions only while Pending.
    --------------------------------------------------------------------
    IF ISNULL(@RequestStatus, N'') <> N'Pending'
        THROW 59132, 'This approval request is not currently awaiting approval and cannot be processed.', 1;

    --------------------------------------------------------------------
    -- Current Approver: only the holder of the request may mutate expenses.
    --------------------------------------------------------------------
    IF @CurrentApproverLoginId IS NULL
       OR UPPER(LTRIM(RTRIM(@CurrentApproverLoginId))) <> UPPER(LTRIM(RTRIM(@ActionBy)))
        THROW 59133, 'You are not the current approver for this approval request.', 1;

    --------------------------------------------------------------------
    -- Role enforcement (expense-level; mirrors day-level ProcessApprovalAction).
    --   Manager : Approve or Return. Must NOT Reject.
    --   Auditor : Approve or Reject. Must NOT Return.
    --------------------------------------------------------------------
    IF @NormalizedStatus = N'REJECTED' AND ISNULL(@CurrentApproverRole, N'') <> N'Auditor'
        THROW 59134, 'Only the Auditor may reject an expense. Managers return an expense for correction instead.', 1;

    IF @NormalizedStatus = N'RETURNED' AND ISNULL(@CurrentApproverRole, N'') = N'Auditor'
        THROW 59135, 'The Auditor cannot return an expense. Auditors approve or reject; managers return for correction.', 1;

        -- 2️⃣ Update the Expense Status AND Remarks
        UPDATE dbo.SalesDailyExpense
        SET
            ClaimStatus = @NewStatus,
            Remarks = @Remarks,
            ClaimProcessedBy = @ActionBy,
            ClaimProcessedDate = DATEADD(MINUTE, 330, SYSUTCDATETIME()),
            ModifiedBy = @ActionBy,
            ModifiedOn = SYSUTCDATETIME()
        WHERE ExpenseId = @ExpenseId;

        IF @@ROWCOUNT <> 1
            THROW 59136, 'The expense row was not updated.', 1;

        IF @NewStatus = N'Returned'
        BEGIN
            -- Day return owns the correction lifecycle: latest detail, activity,
            -- non-rejected expenses on that day, L1 approver, header, history,
            -- and dbo.usp_RecalculateApprovalRequest. A failure rolls back the
            -- expense update above.
            EXEC dbo.usp_ProcessApprovalAction
                  @ApprovalRequestDetailId = @ApprovalRequestDetailId
                , @Action = N'RETURN'
                , @ActionBy = @ActionBy
                , @Remarks = @Remarks;
        END
        ELSE IF @ApprovalRequestDetailId IS NOT NULL
        BEGIN
        UPDATE dbo.SalesApprovalRequestDetail
        SET
            Snapshot_DAAmount = ISNULL((SELECT SUM(Amount) FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND ExpenseType = 'DA' AND IsDeleted = 0 AND ISNULL(ClaimStatus,'') <> 'Rejected'), 0),
            Snapshot_HotelAmount = ISNULL((SELECT SUM(Amount) FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND ExpenseType = 'Hotel' AND IsDeleted = 0 AND ISNULL(ClaimStatus,'') <> 'Rejected'), 0),
            Snapshot_TravelAmount = ISNULL((SELECT SUM(Amount) FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND ExpenseType = 'Travel' AND IsDeleted = 0 AND ISNULL(ClaimStatus,'') <> 'Rejected'), 0),
            Snapshot_StationaryAmount = ISNULL((SELECT SUM(Amount) FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND ExpenseType = 'Stationary' AND IsDeleted = 0 AND ISNULL(ClaimStatus,'') <> 'Rejected'), 0),
            ModifiedOn = SYSUTCDATETIME()
        WHERE ApprovalRequestDetailId = @ApprovalRequestDetailId;

        UPDATE dbo.SalesApprovalRequestDetail
        SET Snapshot_ExpenseTotal = ISNULL(Snapshot_DAAmount, 0) + ISNULL(Snapshot_HotelAmount, 0) + ISNULL(Snapshot_TravelAmount, 0) + ISNULL(Snapshot_StationaryAmount, 0)
        WHERE ApprovalRequestDetailId = @ApprovalRequestDetailId;

        DECLARE @HeaderTotalClaim DECIMAL(18,2);
        DECLARE @HeaderTotalExpense DECIMAL(18,2);

        SELECT
            @HeaderTotalClaim = SUM(ISNULL(d.Snapshot_CalculatedClaimAmount, 0)),
            @HeaderTotalExpense = SUM(ISNULL(d.Snapshot_ExpenseTotal, 0))
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN (
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
        WHERE d.ApprovalRequestId = @ApprovalRequestId;

            UPDATE dbo.SalesApprovalRequest
            SET
                TotalClaimAmount = @HeaderTotalClaim,
                TotalExpenseAmount = @HeaderTotalExpense,
                ModifiedOn = SYSUTCDATETIME()
            WHERE ApprovalRequestId = @ApprovalRequestId;

            EXEC dbo.usp_RecalculateApprovalRequest
                @ApprovalRequestId = @ApprovalRequestId;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
