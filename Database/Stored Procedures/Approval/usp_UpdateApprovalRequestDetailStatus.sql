/*
    Object:      [dbo].[usp_UpdateApprovalRequestDetailStatus]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_UpdateApprovalRequestDetailStatus]
    @ApprovalRequestDetailId INT,
    @NewStatus VARCHAR(50),
    @ManagerRemark NVARCHAR(4000) = NULL,
    @ProcessedBy NVARCHAR(200) = NULL,
    @ProcessedOn DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SalesApprovalRequestDetail
    SET DetailStatus = @NewStatus,
        DetailRemarks = ISNULL(@ManagerRemark, DetailRemarks),
        DetailApprovedBy = @ProcessedBy,
        DetailApprovedOn = ISNULL(@ProcessedOn, GETDATE())
    WHERE ApprovalRequestDetailId = @ApprovalRequestDetailId;
END
