/*
    Object:      [dbo].[usp_GetMyClaims]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_GetMyClaims]
    @UserLoginId NVARCHAR(100),
    @FilterYear INT = NULL,
    @FilterMonth INT = NULL,
    @FilterStatus NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        sar.ApprovalRequestId,
        sar.RequesterLoginId,
        ISNULL(um.EmployeeName, sar.RequesterLoginId) AS EmployeeName,
        sar.RequestDate,
        sar.YearNo,
        sar.MonthNo,
        DATENAME(MONTH, DATEFROMPARTS(sar.YearNo, sar.MonthNo, 1)) AS MonthName,
        sar.Status AS RequestStatus,
        sar.TotalKM,
        sar.TotalClaimAmount,
        sar.TotalExpenseAmount,
        -- Total Requested Amount
        (ISNULL(sar.TotalClaimAmount, 0) + ISNULL(sar.TotalExpenseAmount, 0)) AS TotalRequestedAmount,
        -- Total Approved Amount
        (ISNULL(sar.ApprovedClaimAmount, 0) + 
         ISNULL(sar.ApprovedDAAmount, 0) + 
         ISNULL(sar.ApprovedHotelAmount, 0) + 
         ISNULL(sar.ApprovedTravelAmount, 0)) AS TotalApprovedAmount,
        
        -- Find who the claim is currently pending with
        ISNULL((
            SELECT TOP 1 ApproverRole 
            FROM SalesApprovalRequestApprover 
            WHERE ApprovalRequestId = sar.ApprovalRequestId 
              AND Status = 'Pending'
            ORDER BY ApproverLevel ASC
        ), 'Fully Approved / Completed') AS PendingWithRole

    FROM SalesApprovalRequest sar
    LEFT JOIN MST_UserMaster um 
        ON um.EmployeeCode = sar.RequesterLoginId
    WHERE 
        sar.RequesterLoginId = @UserLoginId
        AND (@FilterYear IS NULL OR sar.YearNo = @FilterYear)
        AND (@FilterMonth IS NULL OR sar.MonthNo = @FilterMonth)
        AND (@FilterStatus IS NULL OR sar.Status = @FilterStatus)

    ORDER BY sar.RequestDate DESC, sar.ApprovalRequestId DESC;
END
