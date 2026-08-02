/*
    Object:      [dbo].[usp_AdminRollbackApprovalRequest]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE   PROCEDURE dbo.usp_AdminRollbackApprovalRequest
(
      @ApprovalRequestId INT
    , @RollbackBy NVARCHAR(100) = 'Admin_Rollback'
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ISTNow DATETIME = DATEADD(MINUTE,330,GETUTCDATE());

    BEGIN TRY

        BEGIN TRAN;

        ----------------------------------------------------------------------
        -- Validate Request
        ----------------------------------------------------------------------

        DECLARE
            @RequesterLoginId NVARCHAR(100),
            @Status NVARCHAR(50);

        SELECT
            @RequesterLoginId = RequesterLoginId,
            @Status = Status
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId;

        IF @RequesterLoginId IS NULL
            THROW 50001,'Approval Request not found.',1;

        ----------------------------------------------------------------------
        -- Optional Safety
        ----------------------------------------------------------------------

        IF @Status = 'Paid'
            THROW 50002,'Paid requests cannot be rolled back.',1;

        ----------------------------------------------------------------------
        -- Capture Activities from Detail Table
        ----------------------------------------------------------------------

        DECLARE @Activities TABLE
        (
            ActivityId INT PRIMARY KEY
        );

        INSERT INTO @Activities(ActivityId)
        SELECT DISTINCT ActivityId
        FROM dbo.SalesApprovalRequestDetail
        WHERE ApprovalRequestId=@ApprovalRequestId;

        ----------------------------------------------------------------------
        -- Fallback (older requests)
        ----------------------------------------------------------------------

        IF NOT EXISTS (SELECT 1 FROM @Activities)
        BEGIN
            INSERT INTO @Activities(ActivityId)
            SELECT ActivityId
            FROM dbo.SalesDailyActivity
            WHERE ApprovalRequestId=@ApprovalRequestId;
        END

        ----------------------------------------------------------------------
        -- Reset Expenses
        ----------------------------------------------------------------------

        UPDATE e
        SET
              ClaimStatus          = 'Draft'
            , ClaimProcessedBy     = NULL
            , ClaimProcessedDate   = NULL
            , ModifiedBy           = @RollbackBy
            , ModifiedOn           = @ISTNow
        FROM dbo.SalesDailyExpense e
        INNER JOIN @Activities a
            ON a.ActivityId=e.ActivityId;

        ----------------------------------------------------------------------
        -- Reset Activities
        ----------------------------------------------------------------------

        UPDATE a
        SET
              ClaimStatus          = 'Draft'
            , ApprovalRequestId    = NULL
            , SubmittedBy          = NULL
            , SubmittedOn          = NULL
            , ClaimProcessedBy     = NULL
            , ClaimProcessedDate   = NULL
            , ModifiedByUserId     = @RollbackBy
            , ModifiedDateTime     = @ISTNow
        FROM dbo.SalesDailyActivity a
        INNER JOIN @Activities x
            ON x.ActivityId=a.ActivityId;

        ----------------------------------------------------------------------
        -- Delete History
        ----------------------------------------------------------------------

        DELETE
        FROM dbo.ApprovalRequestStatusHistory
        WHERE ApprovalRequestId=@ApprovalRequestId;

        ----------------------------------------------------------------------
        -- Delete Approvers
        ----------------------------------------------------------------------

        DELETE
        FROM dbo.SalesApprovalRequestApprover
        WHERE ApprovalRequestId=@ApprovalRequestId;

        ----------------------------------------------------------------------
        -- Delete Detail Versions
        ----------------------------------------------------------------------

        DELETE
        FROM dbo.SalesApprovalRequestDetail
        WHERE ApprovalRequestId=@ApprovalRequestId;

        ----------------------------------------------------------------------
        -- Delete Master Request
        ----------------------------------------------------------------------

        DELETE
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId=@ApprovalRequestId;

        IF @@ROWCOUNT<>1
            THROW 50003,'Failed deleting Approval Request.',1;

        ----------------------------------------------------------------------
        -- Verification
        ----------------------------------------------------------------------

        IF EXISTS
        (
            SELECT 1
            FROM dbo.SalesApprovalRequest
            WHERE ApprovalRequestId=@ApprovalRequestId
        )
            THROW 50004,'Rollback verification failed (Request).',1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.SalesApprovalRequestDetail
            WHERE ApprovalRequestId=@ApprovalRequestId
        )
            THROW 50005,'Rollback verification failed (Detail).',1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.SalesApprovalRequestApprover
            WHERE ApprovalRequestId=@ApprovalRequestId
        )
            THROW 50006,'Rollback verification failed (Approver).',1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.ApprovalRequestStatusHistory
            WHERE ApprovalRequestId=@ApprovalRequestId
        )
            THROW 50007,'Rollback verification failed (History).',1;

        COMMIT TRAN;

        SELECT
            Success = 1,
            Message = 'Rollback completed successfully.',
            ApprovalRequestId = @ApprovalRequestId;

    END TRY
    BEGIN CATCH

        IF @@TRANCOUNT>0
            ROLLBACK TRAN;

        THROW;

    END CATCH
END
