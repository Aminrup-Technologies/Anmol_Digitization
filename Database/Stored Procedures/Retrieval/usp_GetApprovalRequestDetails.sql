/*
    Object:      [dbo].[usp_GetApprovalRequestDetails]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_GetApprovalRequestDetails]
    @ApprovalRequestId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.ApprovalRequestDetailId,
        d.ApprovalRequestId,
        d.ActivityId,
        a.ActivityDate,
        a.Day,
        a.User_Id,
        a.User_Name,
        a.Login_Id,

        -- Activity amounts (DISPLAY ONLY)
        a.TotalDistanceKM,
        a.CalculatedClaimAmount,
        a.ClaimStatus,                 -- keep for backward UI
        a.ClaimProcessedBy,
        a.ClaimProcessedDate,

        -- Snapshot (approval-time amounts)
        -- Aggregated Travel FromPlace
        TravelFromPlace =
            STUFF((
                SELECT DISTINCT ', ' + e.FromPlace
                FROM dbo.SalesDailyExpense e
                WHERE e.ActivityId = a.ActivityId
                  AND e.IsDeleted = 0
                  AND e.FromPlace IS NOT NULL
                  AND LTRIM(RTRIM(e.FromPlace)) <> ''
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'), 1, 2, ''),

        -- Aggregated Travel ToPlace
        TravelToPlace =
            STUFF((
                SELECT DISTINCT ', ' + e.ToPlace
                FROM dbo.SalesDailyExpense e
                WHERE e.ActivityId = a.ActivityId
                  AND e.IsDeleted = 0
                  AND e.ToPlace IS NOT NULL
                  AND LTRIM(RTRIM(e.ToPlace)) <> ''
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'), 1, 2, ''),

        d.Snapshot_TotalDistanceKM,
        d.Snapshot_CalculatedClaimAmount,
        d.Snapshot_DAAmount,
        d.Snapshot_HotelAmount,
        d.Snapshot_TravelAmount,
		ISNULL(d.Snapshot_StationaryAmount,0) AS Snapshot_StationaryAmount,
        d.Snapshot_ExpenseTotal,

        -- Approval workflow fields
        d.DetailStatus,
        d.DetailRemarks,
        d.DetailApprovedBy,
        d.DetailApprovedOn,
        d.DetailApprovedLevel,

        -- 🔑 Explicit final approval flag
        CASE 
            WHEN d.DetailStatus = 'Approved'
             AND d.DetailApprovedLevel = (
                SELECT MAX(ApproverLevel)
                FROM SalesApprovalRequestApprover
                WHERE ApprovalRequestId = @ApprovalRequestId
                  AND IsRequired = 1
             )
            THEN 1 ELSE 0
        END AS IsFinalApproved,

        d.CreatedOn AS DetailCreatedOn,
		ExpenseCount =
		(
			SELECT COUNT(*)
			FROM dbo.SalesDailyExpense e
			WHERE e.ActivityId = a.ActivityId
			  AND e.IsDeleted = 0
		),

		AttachmentCount =
		(
			SELECT COUNT(*)
			FROM dbo.SalesDailyExpense e
			WHERE e.ActivityId = a.ActivityId
			  AND e.IsDeleted = 0
			  AND ISNULL(e.AttachmentPath,'') <> ''
		),
		ManualReview =
		CASE
		WHEN EXISTS
		(
			SELECT 1
			FROM dbo.SalesDailyExpense e
			WHERE e.ActivityId = a.ActivityId
			  AND e.IsDeleted = 0
			  AND e.ExpenseType IN
			  (
				  'Hotel',
				  'Travel',
				  'Stationary'
			  )
		)
		THEN CAST(1 AS BIT)

		ELSE CAST(0 AS BIT)

		END,
		BulkEligible =
		CASE
		WHEN EXISTS
		(
			SELECT 1
			FROM dbo.SalesDailyExpense e
			WHERE e.ActivityId = a.ActivityId
			  AND e.IsDeleted = 0
			  AND e.ExpenseType IN
			  (
				  'Hotel',
				  'Travel',
				  'Stationary'
			  )
		)
		THEN CAST(0 AS BIT)

		ELSE CAST(1 AS BIT)

		END,

		ClaimType =
		(
			SELECT
				CASE

				WHEN COUNT(DISTINCT ExpenseType)=0
					THEN 'TA / DA Only'

				WHEN COUNT(DISTINCT ExpenseType)=1
					THEN MAX(ExpenseType)+' Claim'

				ELSE 'Multiple Expense Claim'

				END

			FROM dbo.SalesDailyExpense e

			WHERE e.ActivityId=a.ActivityId

			  AND e.IsDeleted=0

			  AND e.ExpenseType<>'DA'
		),
        a.Remarks AS ActivityRemarks,

		r.Status AS RequestStatus,
		r.CurrentApproverLoginId,
		r.CurrentApproverLevel,

        -- Expenses JSON (CORRECTED)
        ISNULL((
            SELECT
                e.ExpenseId,         -- ✅ Critical for Button Actions
                --e.ClaimStatus,       -- ✅ Critical for Badge Display
				CASE
					WHEN e.ClaimStatus IN ('Submitted','Draft','')
						 OR e.ClaimStatus IS NULL
					THEN 'Pending'
					ELSE e.ClaimStatus
				END AS ClaimStatus,
                e.ExpenseType,
                e.ExpenseDate,
                e.FromDate,
                e.ToDate,
                e.BillNo,
                e.Amount,
                e.Currency,
                e.Purpose,
                e.AttachmentFileName,
                e.AttachmentPath,
                e.TravelType,
                e.FromPlace,
                e.ToPlace,
                e.TravelDate,
                e.PolicyMode,
                e.PolicySnapshot,
                e.IsAutoDA,
                e.Source,
                e.CreatedBy,
                e.CreatedDateTime
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = a.ActivityId
              AND e.IsDeleted = 0   -- 🚩 ADDED: Hides deleted rows to prevent errors
            FOR JSON PATH
        ), '[]') AS ExpensesJson

    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN dbo.SalesDailyActivity a
        ON a.ActivityId = d.ActivityId
	INNER JOIN dbo.SalesApprovalRequest r
		ON r.ApprovalRequestId = d.ApprovalRequestId
    WHERE d.ApprovalRequestId = @ApprovalRequestId
    ORDER BY d.ApprovalRequestDetailId;
END
