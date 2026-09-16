/* ============================================================================
   NAME:        export_latest_approved_only
   WHEN:        2026-09-15
   WHY:         Export selected every Approved detail version so Returned then RESUBMIT could duplicate rows
   WHAT:        usp_Export_Auditor_Final_Excel keeps one latest Approved detail per ActivityId
   ============================================================================ */

/*
    Object:      [dbo].[usp_Export_Auditor_Final_Excel]
    Type:        STORED PROCEDURE
    Note:        Same body as Approval/usp_Export_Auditor_Final_Excel.sql (RC-015C)
*/

CREATE PROCEDURE [dbo].[usp_Export_Auditor_Final_Excel]
(
    @ApprovalRequestId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SAR.ApprovalRequestId                 AS [Claim No],
        SAR.RequesterLoginId                  AS [Employee Code],
        UM.EmployeeName                       AS [Employee Name],
        SDA.ActivityDate                      AS [Date],
        SDA.Day                               AS [Day],

        ISNULL((SELECT TOP 1 FromPlace FROM dbo.SalesDailyExpense WHERE ActivityId = SARD.ActivityId AND IsDeleted = 0 AND FromPlace IS NOT NULL ORDER BY ExpenseId ASC), '-') AS [From Place],
        ISNULL((SELECT TOP 1 ToPlace FROM dbo.SalesDailyExpense WHERE ActivityId = SARD.ActivityId AND IsDeleted = 0 AND ToPlace IS NOT NULL ORDER BY ExpenseId DESC), '-') AS [To Place],

        ISNULL(SARD.Snapshot_TotalDistanceKM, 0) AS [KM Claimed],
        ISNULL(SARD.Snapshot_CalculatedClaimAmount, 0) AS [TA Bill],

        ISNULL(SARD.Snapshot_DAAmount, 0)     AS [DA],
        ISNULL(SARD.Snapshot_HotelAmount, 0)  AS [Hotel],
        ISNULL(SARD.Snapshot_TravelAmount, 0) AS [Travel / OPA],
        ISNULL(SARD.Snapshot_StationaryAmount, 0) AS [Stationary],

        ISNULL(SARD.Snapshot_ExpenseTotal, 0) AS [Total Expenses (DA+Hotel+Trv+Stat)],

        (ISNULL(SARD.Snapshot_ExpenseTotal, 0) + ISNULL(SARD.Snapshot_CalculatedClaimAmount, 0)) AS [Final Approved Amount]

    FROM dbo.SalesApprovalRequest SAR
    INNER JOIN dbo.SalesApprovalRequestDetail SARD
        ON SAR.ApprovalRequestId = SARD.ApprovalRequestId
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
        ON SARD.ApprovalRequestDetailId = latest.LatestDetailId
    INNER JOIN dbo.SalesDailyActivity SDA
        ON SDA.ActivityId = SARD.ActivityId
    INNER JOIN dbo.MST_UserMaster UM
        ON UM.EmployeeCode = SAR.RequesterLoginId
    WHERE SAR.ApprovalRequestId = @ApprovalRequestId
      AND SAR.Status IN ('Completed', 'ReadyForPayment', 'Processed')
      AND SARD.DetailStatus = 'Approved'
    ORDER BY SDA.ActivityDate ASC;
END
