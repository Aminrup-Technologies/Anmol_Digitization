/*
    Object:      [dbo].[usp_GetApprovalRequest]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE   PROCEDURE dbo.usp_GetApprovalRequest
(
      @ApprovalRequestId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    ------------------------------------------------------------
    -- Validate
    ------------------------------------------------------------

    IF NOT EXISTS
    (
        SELECT 1
        FROM SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId
    )
    BEGIN
        THROW 52001,
              'Approval Request not found.',
              1;
    END;

    ------------------------------------------------------------
    -- Request Header
    ------------------------------------------------------------

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

    --r.OriginalClaimAmount,

	ISNULL(r.OriginalClaimAmount,
       ISNULL(r.TotalClaimAmount,0) + ISNULL(r.TotalExpenseAmount,0))
       AS OriginalClaimAmount,

    r.ApprovedKM,
    r.ApprovedClaimAmount,
    r.ApprovedDAAmount,
    r.ApprovedHotelAmount,
    r.ApprovedTravelAmount,
    r.ApprovedExpenseAmount,

    r.TotalApprovedAmount,
    r.FinalApprovedAmount,
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
