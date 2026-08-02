/*
    Object:      [dbo].[usp_ReopenApprovalRequestByAuditor]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE   PROCEDURE dbo.usp_ReopenApprovalRequestByAuditor
(
    @ApprovalRequestId INT,
    @ActionBy NVARCHAR(100),
    @Remarks NVARCHAR(1000) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentStatus NVARCHAR(50);

    /* 0️⃣ SECURITY: Ensure caller is Auditor for this request */
    IF NOT EXISTS (
        SELECT 1
        FROM SalesApprovalRequestApprover
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND ApproverLoginId = @ActionBy
          AND ApproverRole = 'Auditor'
    )
    BEGIN
        RAISERROR('Only Auditor is allowed to reopen this request.', 16, 1);
        RETURN;
    END

    /* 1️⃣ Get current header status */
    SELECT @CurrentStatus = Status
    FROM SalesApprovalRequest
    WHERE ApprovalRequestId = @ApprovalRequestId;

    /* 🚫 Accounts safety */
    IF @CurrentStatus IN ('ReadyForPayment', 'Processed')
    BEGIN
        RAISERROR('Request already moved to Accounts. Reopen not allowed.', 16, 1);
        RETURN;
    END

    /* 2️⃣ HISTORY */
    INSERT INTO ApprovalRequestStatusHistory
    (
        ApprovalRequestId,
        OldStatus,
        NewStatus,
        ChangedBy,
        Remarks
    )
    VALUES
    (
        @ApprovalRequestId,
        @CurrentStatus,
        'Reopened',
        @ActionBy,
        @Remarks
    );

    /* 3️⃣ RESET DETAILS (ALL DAY ROWS) */
    UPDATE SalesApprovalRequestDetail
    SET
        DetailStatus = 'Pending',
        DetailApprovedBy = NULL,
        DetailApprovedOn = NULL,
        DetailApprovedLevel = NULL,
        DetailRemarks = NULL
    WHERE ApprovalRequestId = @ApprovalRequestId;

    /* 4️⃣ RESET HEADER */
    UPDATE SalesApprovalRequest
    SET
        Status = 'Pending',
        ModifiedBy = @ActionBy,
        ModifiedOn = SYSDATETIME()
    WHERE ApprovalRequestId = @ApprovalRequestId;

    /* 5️⃣ RESET AUDITOR APPROVER ROW (ROLE-BASED ✅) */
    UPDATE SalesApprovalRequestApprover
    SET
        Status = 'Pending',
        ActionBy = NULL,
        ActionDate = NULL,
        Remarks = NULL
    WHERE ApprovalRequestId = @ApprovalRequestId
      AND ApproverRole = 'Auditor';

END
