/*
    Object:      [dbo].[usp_MarkRequestReadyForPayment]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE   PROCEDURE dbo.usp_MarkRequestReadyForPayment
(
    @ApprovalRequestId INT,
    @ActionBy NVARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;

    /* 1️⃣ Auditor validation */
    IF NOT EXISTS (
        SELECT 1
        FROM SalesApprovalRequestApprover
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND ApproverLoginId = @ActionBy
          AND ApproverRole = 'Auditor'
    )
    BEGIN
        RAISERROR('Only Auditor can mark Ready for Payment.',16,1);
        RETURN;
    END

    /* 2️⃣ Must be Completed */
    IF NOT EXISTS (
        SELECT 1
        FROM SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND Status = 'Completed'
    )
    BEGIN
        RAISERROR('Request not completed.',16,1);
        RETURN;
    END

    /* 3️⃣ Update header */
    UPDATE SalesApprovalRequest
    SET
        Status = 'ReadyForPayment',
        ModifiedBy = @ActionBy,
        ModifiedOn = SYSUTCDATETIME()
    WHERE ApprovalRequestId = @ApprovalRequestId;

    INSERT INTO ApprovalRequestStatusHistory
    VALUES
    (
        @ApprovalRequestId,'Completed','ReadyForPayment',
        @ActionBy,SYSUTCDATETIME(),
        'Auditor marked Ready for Payment'
    );
END
