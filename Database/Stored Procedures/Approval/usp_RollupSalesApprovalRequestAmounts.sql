/*
    Object:      [dbo].[usp_RollupSalesApprovalRequestAmounts]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE   PROCEDURE [dbo].[usp_RollupSalesApprovalRequestAmounts]
(
    @ApprovalRequestId INT,
    @ActionBy NVARCHAR(100) = 'SYSTEM'
)
AS
BEGIN
    SET NOCOUNT ON;

    /* ===============================
       1. Aggregate Approved Details
    =============================== */
    DECLARE
        @ApprovedKM            DECIMAL(12,2),
        @ApprovedClaimAmount   DECIMAL(18,2),
        @ApprovedDAAmount      DECIMAL(18,2),
        @ApprovedHotelAmount   DECIMAL(18,2),
        @ApprovedTravelAmount  DECIMAL(18,2);

    SELECT
        @ApprovedKM = ISNULL(SUM(Snapshot_TotalDistanceKM), 0),
        @ApprovedClaimAmount = ISNULL(SUM(Snapshot_CalculatedClaimAmount), 0),
        @ApprovedDAAmount = ISNULL(SUM(Snapshot_DAAmount), 0),
        @ApprovedHotelAmount = ISNULL(SUM(Snapshot_HotelAmount), 0),
        @ApprovedTravelAmount = ISNULL(SUM(Snapshot_TravelAmount), 0)
    FROM dbo.SalesApprovalRequestDetail
    WHERE ApprovalRequestId = @ApprovalRequestId
	  AND DetailStatus = 'Approved'
	  AND DetailApprovedLevel = 2;

    /* ===============================
       2. Update Header Amounts
    =============================== */
    UPDATE dbo.SalesApprovalRequest
    SET
        ApprovedKM = @ApprovedKM,
        ApprovedClaimAmount = @ApprovedClaimAmount,
        ApprovedDAAmount = @ApprovedDAAmount,
        ApprovedHotelAmount = @ApprovedHotelAmount,
        ApprovedTravelAmount = @ApprovedTravelAmount,
        ModifiedOn = SYSUTCDATETIME(),
        ModifiedBy = @ActionBy
    WHERE ApprovalRequestId = @ApprovalRequestId;

    /* ===============================
       3. Determine Header Status
       (NO premature approval)
    =============================== */
    /* ===============================
   3. Determine Header Status
   (FINAL – Auditor safe)
=============================== */
	DECLARE
		@TotalDetails INT,
		@FinalProcessed INT;

	SELECT
		@TotalDetails = COUNT(*),
		@FinalProcessed = SUM(
			CASE 
				WHEN DetailStatus = 'Rejected' THEN 1
				WHEN DetailStatus = 'Approved' AND DetailApprovedLevel = 2 THEN 1
				ELSE 0
			END
		)
	FROM dbo.SalesApprovalRequestDetail
	WHERE ApprovalRequestId = @ApprovalRequestId;

	DECLARE @NewStatus NVARCHAR(50);

	IF @FinalProcessed = @TotalDetails
		SET @NewStatus = 'Completed';
	ELSE
		SET @NewStatus = 'Pending';

	UPDATE dbo.SalesApprovalRequest
	SET Status = @NewStatus
	WHERE ApprovalRequestId = @ApprovalRequestId;


    /* ===============================
       4. Status History (Audit Safe)
    =============================== */
    INSERT INTO dbo.ApprovalRequestStatusHistory
    (
        ApprovalRequestId,
        OldStatus,
        NewStatus,
        ChangedBy,
        ChangedOn,
        Remarks
    )
    SELECT
        @ApprovalRequestId,
        Status,
        @NewStatus,
        @ActionBy,
        SYSUTCDATETIME(),
        'SYSTEM ROLLUP: Header totals recalculated'
    FROM dbo.SalesApprovalRequest
    WHERE ApprovalRequestId = @ApprovalRequestId;
END
