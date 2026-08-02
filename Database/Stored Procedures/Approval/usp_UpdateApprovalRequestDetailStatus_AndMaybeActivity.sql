/*
    Object:      [dbo].[usp_UpdateApprovalRequestDetailStatus_AndMaybeActivity]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_UpdateApprovalRequestDetailStatus_AndMaybeActivity]
    @ApprovalRequestDetailId INT,
    @NewStatus VARCHAR(50),
    @ManagerRemark NVARCHAR(4000) = NULL,
    @ProcessedBy NVARCHAR(200) = NULL,
    @ProcessedOn DATETIME = NULL,
    @ForceSetActivityClaim BIT = 0  -- if 1, always update the activity's ClaimStatus according to @NewStatus
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ProcessedOn IS NULL
        SET @ProcessedOn = SYSUTCDATETIME();

    BEGIN TRAN;

    BEGIN TRY
        DECLARE @ActivityId INT = NULL;
        DECLARE @ApprovalRequestId INT = NULL;

        -- update the detail row
        UPDATE dbo.SalesApprovalRequestDetail
        SET DetailStatus = @NewStatus,
            DetailRemarks = CASE WHEN @ManagerRemark IS NULL THEN DetailRemarks ELSE @ManagerRemark END,
            DetailApprovedBy = ISNULL(@ProcessedBy, DetailApprovedBy),
            DetailApprovedOn = ISNULL(@ProcessedOn, DetailApprovedOn),
            ModifiedOn = SYSUTCDATETIME(),
            ModifiedBy = ISNULL(@ProcessedBy, ModifiedBy)
        WHERE ApprovalRequestDetailId = @ApprovalRequestDetailId;

        -- get the activity id and approval request id for the updated detail
        SELECT @ActivityId = ActivityId, @ApprovalRequestId = ApprovalRequestId
        FROM dbo.SalesApprovalRequestDetail
        WHERE ApprovalRequestDetailId = @ApprovalRequestDetailId;

        IF @ActivityId IS NOT NULL
        BEGIN
            -- Map detail action to activity ClaimStatus when forced.
            DECLARE @ActivityClaimStatus VARCHAR(50) = 
                CASE WHEN LOWER(@NewStatus) = 'approved' THEN 'Approved'
                     WHEN LOWER(@NewStatus) = 'rejected' THEN 'Rejected'
                     ELSE NULL END;

            IF @ForceSetActivityClaim = 1
            BEGIN
                IF @ActivityClaimStatus IS NOT NULL
                BEGIN
                    -- set activity
                    UPDATE dbo.SalesDailyActivity
                    SET ClaimStatus = @ActivityClaimStatus,
                        ClaimProcessedBy = ISNULL(@ProcessedBy, ClaimProcessedBy),
                        ClaimProcessedDate = ISNULL(@ProcessedOn, ClaimProcessedDate),
                        ModifiedByUserId = ISNULL(@ProcessedBy, ModifiedByUserId),
                        ModifiedDateTime = ISNULL(@ProcessedOn, ModifiedDateTime)
                    WHERE ActivityId = @ActivityId;

                    -- keep expenses in sync (force)
                    UPDATE dbo.SalesDailyExpense
                    SET ClaimStatus = @ActivityClaimStatus,
                        ClaimProcessedBy = ISNULL(@ProcessedBy, ClaimProcessedBy),
                        ClaimProcessedDate = ISNULL(@ProcessedOn, ClaimProcessedDate),
                        ModifiedBy = ISNULL(@ProcessedBy, ModifiedBy),
                        ModifiedOn = ISNULL(@ProcessedOn, ModifiedOn)
                    WHERE ActivityId = @ActivityId;
                END
                -- if @ActivityClaimStatus is NULL, do nothing for claim fields (no mapping)
            END
            ELSE
            BEGIN
                -- Default behaviour: sibling-check logic for Approved; for Rejected revert to Submitted
                IF LOWER(@NewStatus) = 'approved'
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM dbo.SalesApprovalRequestDetail d
                        WHERE d.ApprovalRequestId = @ApprovalRequestId
                          AND ISNULL(d.DetailStatus, '') <> 'Approved'
                    )
                    BEGIN
                        -- All sibling details are approved => mark activity Approved
                        UPDATE dbo.SalesDailyActivity
                        SET ClaimStatus = 'Approved',
                            ClaimProcessedBy = ISNULL(@ProcessedBy, ClaimProcessedBy),
                            ClaimProcessedDate = ISNULL(@ProcessedOn, ClaimProcessedDate),
                            ModifiedByUserId = ISNULL(@ProcessedBy, ModifiedByUserId),
                            ModifiedDateTime = ISNULL(@ProcessedOn, ModifiedDateTime)
                        WHERE ActivityId = @ActivityId;

                        -- Mark related expenses Approved as well
                        UPDATE dbo.SalesDailyExpense
                        SET ClaimStatus = 'Approved',
                            ClaimProcessedBy = ISNULL(@ProcessedBy, ClaimProcessedBy),
                            ClaimProcessedDate = ISNULL(@ProcessedOn, ClaimProcessedDate),
                            ModifiedBy = ISNULL(@ProcessedBy, ModifiedBy),
                            ModifiedOn = ISNULL(@ProcessedOn, ModifiedOn)
                        WHERE ActivityId = @ActivityId;
                    END
                END
                ELSE IF LOWER(@NewStatus) = 'rejected'
                BEGIN
                    -- If a detail is Rejected, set activity ClaimStatus = 'Submitted'
                    UPDATE dbo.SalesDailyActivity
                    SET ClaimStatus = 'Submitted',
                        ClaimProcessedBy = ISNULL(@ProcessedBy, ClaimProcessedBy),
                        ClaimProcessedDate = ISNULL(@ProcessedOn, ClaimProcessedDate),
                        ModifiedByUserId = ISNULL(@ProcessedBy, ModifiedByUserId),
                        ModifiedDateTime = ISNULL(@ProcessedOn, ModifiedDateTime)
                    WHERE ActivityId = @ActivityId;

                    -- Revert related expenses to Submitted as well
                    UPDATE dbo.SalesDailyExpense
                    SET ClaimStatus = 'Submitted',
                        ClaimProcessedBy = ISNULL(@ProcessedBy, ClaimProcessedBy),
                        ClaimProcessedDate = ISNULL(@ProcessedOn, ClaimProcessedDate),
                        ModifiedBy = ISNULL(@ProcessedBy, ModifiedBy),
                        ModifiedOn = ISNULL(@ProcessedOn, ModifiedOn)
                    WHERE ActivityId = @ActivityId;
                END
                -- else: other statuses -> no automatic change to activity/expense
            END
        END

        -- Recalculate header totals (TotalClaimAmount and TotalApprovedAmount) for the ApprovalRequest
        IF @ApprovalRequestId IS NOT NULL
        BEGIN
            DECLARE @TotalKM decimal(12,2), @TotalClaimAmount decimal(18,2), @TotalDAAmount decimal(18,2),
                    @TotalHotelAmount decimal(18,2), @TotalTravelAmount decimal(18,2), @TotalExpenseAmount decimal(18,2),
                    @TotalApprovedAmount decimal(18,2);

            SELECT 
                @TotalKM = SUM(ISNULL(d.Snapshot_TotalDistanceKM,0)),
                @TotalClaimAmount = SUM(ISNULL(d.Snapshot_CalculatedClaimAmount,0)),
                @TotalDAAmount = SUM(ISNULL(d.Snapshot_DAAmount,0)),
                @TotalHotelAmount = SUM(ISNULL(d.Snapshot_HotelAmount,0)),
                @TotalTravelAmount = SUM(ISNULL(d.Snapshot_TravelAmount,0)),
                @TotalExpenseAmount = SUM(ISNULL(d.Snapshot_ExpenseTotal,0))
            FROM dbo.SalesApprovalRequestDetail d
            WHERE d.ApprovalRequestId = @ApprovalRequestId;

            SELECT @TotalApprovedAmount = SUM(ISNULL(d.Snapshot_CalculatedClaimAmount,0))
            FROM dbo.SalesApprovalRequestDetail d
            WHERE d.ApprovalRequestId = @ApprovalRequestId
              AND d.DetailStatus = 'Approved';

            UPDATE dbo.SalesApprovalRequest
            SET TotalKM = @TotalKM,
                TotalClaimAmount = @TotalClaimAmount,
                TotalDAAmount = @TotalDAAmount,
                TotalHotelAmount = @TotalHotelAmount,
                TotalTravelAmount = @TotalTravelAmount,
                TotalExpenseAmount = @TotalExpenseAmount,
                TotalApprovedAmount = ISNULL(@TotalApprovedAmount,0),
                ModifiedOn = SYSUTCDATETIME(),
                ModifiedBy = ISNULL(@ProcessedBy, ModifiedBy)
            WHERE ApprovalRequestId = @ApprovalRequestId;
        END

        -- Advance the approval queue: find next pending approver and set header CurrentApprover*
        IF @ApprovalRequestId IS NOT NULL
        BEGIN
            DECLARE @nextLevel INT = NULL, @nextLogin NVARCHAR(100) = NULL;

            SELECT TOP(1) @nextLevel = ApproverLevel, @nextLogin = ApproverLoginId
            FROM dbo.SalesApprovalRequestApprover
            WHERE ApprovalRequestId = @ApprovalRequestId AND Status = 'Pending'
            ORDER BY ISNULL(ApproverLevel,9999), ApprovalRequestApproverId;

            UPDATE dbo.SalesApprovalRequest
            SET CurrentApproverLevel = @nextLevel,
                CurrentApproverLoginId = @nextLogin,
                ModifiedOn = SYSUTCDATETIME(),
                ModifiedBy = ISNULL(@ProcessedBy, ModifiedBy)
            WHERE ApprovalRequestId = @ApprovalRequestId;

            -- If no next pending approver, finalize overall request status
            IF @nextLevel IS NULL
            BEGIN
                DECLARE @hasRejected INT = 0;
                SELECT @hasRejected = COUNT(*) FROM dbo.SalesApprovalRequestApprover WHERE ApprovalRequestId = @ApprovalRequestId AND Status = 'Rejected';

                IF @hasRejected > 0
                    UPDATE dbo.SalesApprovalRequest SET Status = 'Rejected', ModifiedOn = SYSUTCDATETIME(), ModifiedBy = ISNULL(@ProcessedBy, ModifiedBy) WHERE ApprovalRequestId = @ApprovalRequestId;
                ELSE
                    UPDATE dbo.SalesApprovalRequest SET Status = 'Approved', ModifiedOn = SYSUTCDATETIME(), ModifiedBy = ISNULL(@ProcessedBy, ModifiedBy) WHERE ApprovalRequestId = @ApprovalRequestId;
            END
        END

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRAN;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR('usp_UpdateApprovalRequestDetailStatus_AndMaybeActivity failed: %s', 16, 1, @ErrMsg);
        RETURN;
    END CATCH
END
