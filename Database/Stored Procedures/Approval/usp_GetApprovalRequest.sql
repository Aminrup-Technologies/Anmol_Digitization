/* ============================================================================
   NAME:        get_request_canonical_header
   WHEN:        2026-09-15
   WHY:         Header read reconstructed OriginalClaimAmount and left FinalApprovedAmount unused
   WHAT:        usp_GetApprovalRequest returns stored Original and FSOT FinalApprovedAmount / Approved* as-is
   ============================================================================ */

/*
    Object:      [dbo].[usp_GetApprovalRequest]
    Type:        STORED PROCEDURE
*/

CREATE PROCEDURE dbo.usp_GetApprovalRequest
(
      @ApprovalRequestId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId
    )
    BEGIN
        THROW 52001,
              'Approval Request not found.',
              1;
    END;

    SELECT
        r.ApprovalRequestId,
        r.RequesterLoginId,
        um.EmployeeName AS RequesterName,

        r.RequestDate,
        r.YearNo,
        r.MonthNo,
        r.Status,
        r.RequestRemarks,
        r.CurrentApproverLoginId,
        r.CurrentApproverLevel,

        r.TotalKM,
        r.TotalClaimAmount,
        r.TotalDAAmount,
        r.TotalHotelAmount,
        r.TotalTravelAmount,
        r.TotalStationaryAmount,
        r.TotalExpenseAmount,

        r.OriginalClaimAmount,

        r.ApprovedKM,
        r.ApprovedClaimAmount,
        r.ApprovedDAAmount,
        r.ApprovedHotelAmount,
        r.ApprovedTravelAmount,
        r.ApprovedExpenseAmount,

        r.TotalApprovedAmount,
        /* Net Payable: roll-up FinalApprovedAmount; fall back to computed TotalApprovedAmount if never rolled up */
        ISNULL(r.FinalApprovedAmount, r.TotalApprovedAmount) AS FinalApprovedAmount,
        r.TotalDeductionAmount,

        r.TotalActivities,
        r.ApprovedActivities,
        r.RejectedActivities,
        r.PendingActivities,
        r.BulkEligibleActivities,
        r.ManualReviewActivities,

        r.TotalExpenseEntries,
        r.TotalAttachmentCount,

        r.CreatedOn,
        r.CreatedBy,
        r.ModifiedOn,
        r.ModifiedBy,

        (
            SELECT MAX(ApproverLevel)
            FROM dbo.SalesApprovalRequestApprover
            WHERE ApprovalRequestId = r.ApprovalRequestId
              AND IsRequired = 1
        ) AS MaxApproverLevel

    FROM dbo.SalesApprovalRequest r
    LEFT JOIN dbo.MST_UserMaster um
           ON um.EmployeeCode = r.RequesterLoginId
    WHERE r.ApprovalRequestId = @ApprovalRequestId;
END
