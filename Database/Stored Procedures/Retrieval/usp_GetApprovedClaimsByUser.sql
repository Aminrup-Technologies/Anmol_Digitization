/*
    Object:      [dbo].[usp_GetApprovedClaimsByUser]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_GetApprovedClaimsByUser]
    @UserLoginId NVARCHAR(100),
    @FilterYear INT = NULL,          -- NEW
    @FilterMonth INT = NULL,         -- NEW
    @SearchEmpCode NVARCHAR(100) = NULL -- NEW
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
        (ISNULL(sar.ApprovedClaimAmount, 0) + 
         ISNULL(sar.ApprovedDAAmount, 0) + 
         ISNULL(sar.ApprovedHotelAmount, 0) + 
         ISNULL(sar.ApprovedTravelAmount, 0)) AS TotalApprovedAmount,
        saa.ApproverRole,
        saa.ApproverLevel,
        ISNULL((
            SELECT TOP 1 ApproverRole 
            FROM SalesApprovalRequestApprover 
            WHERE ApprovalRequestId = sar.ApprovalRequestId 
              AND Status = 'Pending'
            ORDER BY ApproverLevel ASC
        ), 'Fully Approved / Completed') AS PendingWithRole,
        CASE 
            WHEN sar.Status = 'Rejected' THEN 'Rejected'
            WHEN sar.Status = 'Approved' THEN 'Fully Approved'
            WHEN sar.Status = 'Completed' THEN 'Completed'
            WHEN sar.Status = 'ReadyForPayment' THEN 'Ready for Payment'
            ELSE 'In Progress'
        END AS ApprovalProgress

    FROM SalesApprovalRequest sar
    INNER JOIN SalesApprovalRequestApprover saa 
        ON saa.ApprovalRequestId = sar.ApprovalRequestId
    LEFT JOIN MST_UserMaster um 
        ON um.EmployeeCode = sar.RequesterLoginId
    WHERE 
        saa.ApproverLoginId = @UserLoginId
        AND saa.Status IN ('Approved', 'Completed', 'Processed', 'ReadyForPayment') 
        /* =========================================
           ✅ APPLIED FILTERS (Ignored if NULL)
        ========================================= */
        AND (@FilterYear IS NULL OR sar.YearNo = @FilterYear)
        AND (@FilterMonth IS NULL OR sar.MonthNo = @FilterMonth)
        AND (@SearchEmpCode IS NULL OR sar.RequesterLoginId = @SearchEmpCode)

    ORDER BY sar.RequestDate DESC, sar.ApprovalRequestId DESC;
END
