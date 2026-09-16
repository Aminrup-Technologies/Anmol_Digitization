/* ============================================================================
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
        RAISERROR('ApprovalRequestId cannot be NULL.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId
    )
    BEGIN
        RAISERROR('Approval Request not found.', 16, 1);
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
              AND e.ExpenseType = 'DA'
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
        ), 0),
        Snapshot_HotelAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = 'Hotel'
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
        ), 0),
        Snapshot_TravelAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = 'Travel'
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
        ), 0),
        Snapshot_StationaryAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = 'Stationary'
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
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
      AND ISNULL(d.DetailStatus, '') NOT IN ('Rejected', 'Returned');

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
