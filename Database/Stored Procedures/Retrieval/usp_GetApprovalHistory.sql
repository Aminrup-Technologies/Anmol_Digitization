/*
    Object:      [dbo].[usp_GetApprovalHistory]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE   PROCEDURE dbo.usp_GetApprovalHistory
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
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId
    )
    BEGIN
        THROW 54001,
              'Approval Request not found.',
              1;
    END;

    ------------------------------------------------------------
    -- Approval History
    ------------------------------------------------------------

    SELECT

        h.HistoryId,

        h.ApprovalRequestId,

        h.ApprovalRequestDetailId,

        h.ActivityId,

        h.DetailVersion,

        h.OldStatus,

        h.NewStatus,

        h.ActionType,

        h.ApproverLevel,

        a.ApproverRole,

        h.ChangedBy,

        h.ChangedOn,

        h.Remarks

    FROM dbo.ApprovalRequestStatusHistory h

    LEFT JOIN dbo.SalesApprovalRequestApprover a
           ON a.ApprovalRequestId = h.ApprovalRequestId
          AND a.ApproverLevel     = h.ApproverLevel

    WHERE h.ApprovalRequestId = @ApprovalRequestId

    ORDER BY
        h.ChangedOn,
        h.HistoryId;

END
