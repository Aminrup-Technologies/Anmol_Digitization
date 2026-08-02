/*
    Object:      [dbo].[usp_Export_Auditor_Final_Excel]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
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
        
        -- NEW: Explicitly select the Stationary amount
        ISNULL(SARD.Snapshot_StationaryAmount, 0) AS [Stationary],

        -- UPDATE: Include Stationary in the sum
        (ISNULL(SARD.Snapshot_DAAmount, 0) + ISNULL(SARD.Snapshot_HotelAmount, 0) + ISNULL(SARD.Snapshot_TravelAmount, 0) + ISNULL(SARD.Snapshot_StationaryAmount, 0)) AS [Total Expenses (DA+Hotel+Trv+Stat)],

        (ISNULL(SARD.Snapshot_ExpenseTotal, 0) + ISNULL(SARD.Snapshot_CalculatedClaimAmount, 0)) AS [Final Approved Amount]

    FROM SalesApprovalRequest SAR
    INNER JOIN SalesApprovalRequestDetail SARD ON SAR.ApprovalRequestId = SARD.ApprovalRequestId
    INNER JOIN SalesDailyActivity SDA ON SDA.ActivityId = SARD.ActivityId
    INNER JOIN MST_UserMaster UM ON UM.EmployeeCode = SAR.RequesterLoginId
    WHERE SAR.ApprovalRequestId = @ApprovalRequestId
      AND SAR.Status IN ('Completed', 'ReadyForPayment', 'Processed')
      AND SARD.DetailStatus = 'Approved' 
    ORDER BY SDA.ActivityDate ASC;
END
