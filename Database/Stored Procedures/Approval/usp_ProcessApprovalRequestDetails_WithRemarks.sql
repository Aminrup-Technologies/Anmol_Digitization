/*
    Object:      [dbo].[usp_ProcessApprovalRequestDetails_WithRemarks]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_ProcessApprovalRequestDetails_WithRemarks]
(
    @ApprovalRequestId INT,
    @DetailIds dbo.TVP_IntIds READONLY,
    @DetailRemarks dbo.TVP_DetailRemarks READONLY,
    @Action NVARCHAR(20), -- 'Approve' or 'Reject'
    @ChangedBy NVARCHAR(100),
    @BulkRemarks NVARCHAR(MAX) = NULL,

    @ApproverRowsUpdated INT OUTPUT,
    @NextApproverLoginId NVARCHAR(100) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @NowUtc DATETIME2 = SYSUTCDATETIME(),
        @CurrentLevel INT;

    /* 1️⃣ Validate approver */
    SELECT @CurrentLevel = CurrentApproverLevel
    FROM SalesApprovalRequest
    WHERE ApprovalRequestId = @ApprovalRequestId
      AND CurrentApproverLoginId = @ChangedBy;

    IF @CurrentLevel IS NULL
    BEGIN
        RAISERROR('Unauthorized approval: You are not the current approver.',16,1);
        RETURN;
    END

    /* 2️⃣ Reject Logic (Kept as is - strict rejection) */
    IF @Action = 'Reject'
    BEGIN
        -- Update Details
        UPDATE d
        SET
            d.DetailStatus = 'Rejected',
            d.DetailRemarks = COALESCE(r.Remark, @BulkRemarks),
            d.ModifiedBy = @ChangedBy,
            d.ModifiedOn = @NowUtc
        FROM SalesApprovalRequestDetail d
        INNER JOIN @DetailIds i ON i.Id = d.ApprovalRequestDetailId
        LEFT JOIN @DetailRemarks r ON r.ApprovalRequestDetailId = d.ApprovalRequestDetailId;

        -- Reject Header immediately
        UPDATE SalesApprovalRequest
        SET
            Status = 'Rejected',
            ModifiedBy = @ChangedBy,
            ModifiedOn = @NowUtc
        WHERE ApprovalRequestId = @ApprovalRequestId;

        -- Log History
        INSERT INTO ApprovalRequestStatusHistory
        (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, ChangedOn, Remarks)
        VALUES
        (@ApprovalRequestId, 'Pending', 'Rejected', @ChangedBy, @NowUtc, @BulkRemarks);

        RETURN;
    END

    /* 3️⃣ Approve Logic: Update Details */
    UPDATE d
    SET
        d.DetailStatus = 'Approved',
        d.DetailApprovedLevel = @CurrentLevel,
        d.DetailApprovedBy = @ChangedBy,
        d.DetailApprovedOn = @NowUtc
    FROM SalesApprovalRequestDetail d
    INNER JOIN @DetailIds i ON i.Id = d.ApprovalRequestDetailId;

    /* 4️⃣ CHECK COMPLETION & MOVE LEVEL */
    
    -- Check if ANY details are still NULL (Pending) or explicitly 'Pending'
    -- If even one row is left untouched, we do NOT move the level yet.
    IF NOT EXISTS (
        SELECT 1 FROM SalesApprovalRequestDetail
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND (DetailStatus IS NULL OR DetailStatus = 'Pending')
    )
    BEGIN
        -- >>> ALL LINE ITEMS PROCESSED FOR THIS LEVEL <<<

        -- A. Mark the Current Level as 'Approved' in the Workflow Table
        UPDATE dbo.SalesApprovalRequestApprover
        SET Status = 'Approved',
            ActionBy = @ChangedBy,
            ActionDate = @NowUtc,
            Remarks = @BulkRemarks
        WHERE ApprovalRequestId = @ApprovalRequestId 
          AND ApproverLevel = @CurrentLevel;

        -- B. Check for the Next Level
        DECLARE @NextLevel INT;
        DECLARE @NextLogin NVARCHAR(100);

        SELECT TOP 1 
            @NextLevel = ApproverLevel,
            @NextLogin = ApproverLoginId
        FROM dbo.SalesApprovalRequestApprover
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND ApproverLevel > @CurrentLevel
        ORDER BY ApproverLevel ASC;

        IF @NextLevel IS NOT NULL
        BEGIN
            -- SCENARIO: HANDOVER TO NEXT LEVEL
            UPDATE dbo.SalesApprovalRequest
            SET CurrentApproverLevel = @NextLevel,
                CurrentApproverLoginId = @NextLogin,
                Status = 'Pending', -- Ensure it stays Pending for the next person
                ModifiedBy = @ChangedBy,
                ModifiedOn = @NowUtc
            WHERE ApprovalRequestId = @ApprovalRequestId;

            -- Audit Log for Level Change
            INSERT INTO ApprovalRequestStatusHistory
            (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, ChangedOn, Remarks)
            VALUES
            (@ApprovalRequestId, 'Pending', 'Pending', @ChangedBy, @NowUtc, 
             CONCAT('Level ', @CurrentLevel, ' Approved. Assigned to Level ', @NextLevel));

            -- Set Outputs
            SET @ApproverRowsUpdated = 1;
            SET @NextApproverLoginId = @NextLogin;
        END
        ELSE
        BEGIN
            -- SCENARIO: FINAL COMPLETION (No more levels)
            UPDATE dbo.SalesApprovalRequest
            SET Status = 'Completed', -- Or 'Approved' based on your preference
                CurrentApproverLevel = NULL, 
                CurrentApproverLoginId = NULL,
                ModifiedBy = @ChangedBy,
                ModifiedOn = @NowUtc
            WHERE ApprovalRequestId = @ApprovalRequestId;

            -- Audit Log for Completion
            INSERT INTO ApprovalRequestStatusHistory
            (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, ChangedOn, Remarks)
            VALUES
            (@ApprovalRequestId, 'Pending', 'Completed', @ChangedBy, @NowUtc, 'Final Approval Completed');

            -- Set Outputs
            SET @ApproverRowsUpdated = 1;
            SET @NextApproverLoginId = NULL;
        END
    END
    ELSE
    BEGIN
        -- Still items remaining for this level (Partial Approval)
        SET @ApproverRowsUpdated = @@ROWCOUNT;
        SET @NextApproverLoginId = @ChangedBy; -- Still with current user
    END
END
