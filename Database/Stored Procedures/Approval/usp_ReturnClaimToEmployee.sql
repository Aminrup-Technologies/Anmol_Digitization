/*
    Object:      [dbo].[usp_ReturnClaimToEmployee]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE   PROCEDURE [dbo].[usp_ReturnClaimToEmployee]
(
    @ApprovalRequestId INT,
    @ActionBy NVARCHAR(100),
    @ReturnReason NVARCHAR(1000)
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRAN;

        -- 1. UNLOCK DAILY EXPENSES: Revert to Draft
        UPDATE e
        SET 
            e.ClaimStatus = 'Draft',
            e.ClaimProcessedBy = NULL,
            e.ClaimProcessedDate = NULL,
            e.ModifiedBy = @ActionBy,
            e.ModifiedOn = SYSUTCDATETIME()
        FROM dbo.SalesDailyExpense e
        INNER JOIN dbo.SalesApprovalRequestDetail d ON e.ActivityId = d.ActivityId
        WHERE d.ApprovalRequestId = @ApprovalRequestId;

        -- 2. UNLOCK DAILY ACTIVITIES: Revert to Draft & Detach from this failed Request
        UPDATE a
        SET 
            a.ClaimStatus = 'Draft',
            a.ApprovalRequestId = NULL,     -- Detaches it so it shows up in the employee's draft pool
            a.SubmittedBy = NULL,
            a.SubmittedOn = NULL,
            a.ModifiedByUserId = @ActionBy,
            a.ModifiedDateTime = SYSUTCDATETIME()
        FROM dbo.SalesDailyActivity a
        INNER JOIN dbo.SalesApprovalRequestDetail d ON a.ActivityId = d.ActivityId
        WHERE d.ApprovalRequestId = @ApprovalRequestId;

        -- 3. Reset Master Header to 'Rejected' (🔑 Changed from 'Returned' to trigger the Submit button)
        UPDATE dbo.SalesApprovalRequest
        SET 
            Status = 'Rejected',
            CurrentApproverLevel = 0,
            CurrentApproverLoginId = NULL,
            ModifiedBy = @ActionBy,
            ModifiedOn = SYSUTCDATETIME()
        WHERE ApprovalRequestId = @ApprovalRequestId;

        -- 4. Cancel all pending workflow steps so it drops off all approver dashboards
        UPDATE dbo.SalesApprovalRequestApprover
        SET 
            Status = 'Cancelled',
            Remarks = 'Claim returned to employee for correction',
            ModifiedBy = @ActionBy,
            ModifiedOn = SYSUTCDATETIME()
        WHERE ApprovalRequestId = @ApprovalRequestId AND Status = 'Pending';

        -- 5. Log it in the Audit Trail
        INSERT INTO dbo.ApprovalRequestStatusHistory 
        (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, Remarks)
        VALUES 
        (@ApprovalRequestId, 'Pending', 'Rejected', @ActionBy, CONCAT('Global Return to Employee: ', @ReturnReason));

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
