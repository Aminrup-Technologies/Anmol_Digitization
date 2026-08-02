/*
    Object:      [dbo].[usp_GetPendingApprovalsForUser_Fast]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_GetPendingApprovalsForUser_Fast]
    @ApproverLoginId NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
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
        sar.TotalDAAmount,
        sar.TotalHotelAmount,
        sar.TotalTravelAmount,
        sar.TotalExpenseAmount,
        sar.CurrentApproverLoginId,
        sar.CurrentApproverLevel,
        
        -- Current user's role for the UI
        saa.ApproverRole,
        saa.ApproverLevel,

        /* -------------------------------------------------
           ✅ NEW: "Pending With" Role Detection
        ------------------------------------------------- */
        ISNULL((
            SELECT TOP 1 ApproverRole 
            FROM SalesApprovalRequestApprover 
            WHERE ApprovalRequestId = sar.ApprovalRequestId 
              AND Status = 'Pending'
            ORDER BY ApproverLevel ASC
        ), 'Unknown') AS PendingWithRole,

        /* -------------------------------------------------
           ✅ FIXED: Approval Progress (Detailed)
        ------------------------------------------------- */
        CASE 
            WHEN sar.Status = 'Rejected' THEN 'Rejected'
            WHEN sar.Status = 'Approved' THEN 'Approved'
            WHEN sar.Status = 'Pending' AND sar.CurrentApproverLoginId = @ApproverLoginId 
                THEN 'Action Required: ' + ISNULL(saa.ApproverRole, 'Approver')
            ELSE 'In Progress'
        END AS ApprovalProgress

    FROM SalesApprovalRequest sar
    INNER JOIN SalesApprovalRequestApprover saa 
        ON saa.ApprovalRequestId = sar.ApprovalRequestId
       AND saa.Status = 'Pending'
       AND saa.ApproverLoginId = @ApproverLoginId
    LEFT JOIN MST_UserMaster um 
        ON um.EmployeeCode = sar.RequesterLoginId
    WHERE sar.Status = 'Pending'
      AND sar.CurrentApproverLoginId = @ApproverLoginId
    ORDER BY sar.RequestDate DESC, sar.ApprovalRequestId DESC;
END
