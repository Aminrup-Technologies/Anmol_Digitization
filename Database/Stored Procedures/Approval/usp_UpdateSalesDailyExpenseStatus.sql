/* ============================================================================
   NAME:        expense_status_latest_detail
   WHEN:        2026-09-15
   WHY:        Receipt status updates targeted any snapshot for the activity
   WHAT:        usp_UpdateSalesDailyExpenseStatus resolves latest DetailVersion then latest ApprovalRequestDetailId
   ============================================================================ */

/*
    Object:      [dbo].[usp_UpdateSalesDailyExpenseStatus]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_UpdateSalesDailyExpenseStatus]
(
    @ExpenseId INT,
    @NewStatus NVARCHAR(50), -- 'Approved' or 'Rejected'
    @ActionBy  NVARCHAR(100),
    @Remarks   NVARCHAR(MAX) = NULL -- ⬅️ NEW: Added the Remarks parameter
)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1️⃣ Safety Check: Prevent update if deleted
    IF EXISTS (SELECT 1 FROM dbo.SalesDailyExpense WHERE ExpenseId = @ExpenseId AND IsDeleted = 1)
    BEGIN
        RAISERROR('Cannot update a deleted expense.', 16, 1);
        RETURN;
    END

    -- 2️⃣ Update the Expense Status AND Remarks
    UPDATE dbo.SalesDailyExpense
    SET 
        ClaimStatus = @NewStatus,
        Remarks = @Remarks, -- ⬅️ NEW: Save the rejection reason here!
        ClaimProcessedBy = @ActionBy,
        ClaimProcessedDate = DATEADD(MINUTE, 330, SYSUTCDATETIME()), 
        ModifiedBy = @ActionBy,
        ModifiedOn = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;

    -- 3️⃣ IDENTIFY PARENT RECORDS (ActivityId → latest DetailVersion → latest ApprovalRequestDetailId)
    DECLARE @ActivityId INT, @ApprovalRequestDetailId INT, @ApprovalRequestId INT;

    SELECT @ActivityId = ActivityId FROM dbo.SalesDailyExpense WHERE ExpenseId = @ExpenseId;

    SELECT TOP (1)
        @ApprovalRequestDetailId = d.ApprovalRequestDetailId,
        @ApprovalRequestId = d.ApprovalRequestId
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

    -- 4️⃣ CHAIN REACTION: RECALCULATE AMOUNTS (FIXED: Respects CK_TA_Calculated constraint)
    IF @ApprovalRequestDetailId IS NOT NULL
    BEGIN
        -- A. Recalculate the specific expense buckets for this Day/Row
        -- (Only summing expenses that are NOT rejected and NOT deleted)
        UPDATE dbo.SalesApprovalRequestDetail
        SET 
            Snapshot_DAAmount = ISNULL((SELECT SUM(Amount) FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND ExpenseType = 'DA' AND IsDeleted = 0 AND ISNULL(ClaimStatus,'') <> 'Rejected'), 0),
            Snapshot_HotelAmount = ISNULL((SELECT SUM(Amount) FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND ExpenseType = 'Hotel' AND IsDeleted = 0 AND ISNULL(ClaimStatus,'') <> 'Rejected'), 0),
            Snapshot_TravelAmount = ISNULL((SELECT SUM(Amount) FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND ExpenseType = 'Travel' AND IsDeleted = 0 AND ISNULL(ClaimStatus,'') <> 'Rejected'), 0),
            ModifiedOn = SYSUTCDATETIME()
        WHERE ApprovalRequestDetailId = @ApprovalRequestDetailId;

        -- B. Update the Total Expense Snapshot (DO NOT TOUCH Snapshot_CalculatedClaimAmount)
        UPDATE dbo.SalesApprovalRequestDetail
        SET Snapshot_ExpenseTotal = ISNULL(Snapshot_DAAmount, 0) + ISNULL(Snapshot_HotelAmount, 0) + ISNULL(Snapshot_TravelAmount, 0)
        WHERE ApprovalRequestDetailId = @ApprovalRequestDetailId;

        -- C. Recalculate the HEADER from latest snapshots only (never superseded Returned rows)
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
            TotalClaimAmount = @HeaderTotalClaim, -- This updates the main "Grand Total"
            TotalExpenseAmount = @HeaderTotalExpense,
            ModifiedOn = SYSUTCDATETIME()
        WHERE ApprovalRequestId = @ApprovalRequestId;
    END
END
