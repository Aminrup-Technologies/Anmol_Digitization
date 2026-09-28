/* ============================================================================
   NAME:        hf231b_consume_auditor_assignment
   WHEN:        2026-09-28
   WHY:         Final Auditor approval leaves the approver row Pending so the claim
                stays in View Pending until Mark Ready for Payment
   WHAT:        usp_MarkRequestReadyForPayment approves that Auditor row, then writes
                Completed → ReadyForPayment
   ============================================================================ */

/* ============================================================================
   NAME:        rc023_markready_payment_lock_note
   WHEN:        2026-09-23
   WHY:         Align MarkReady contract with RC-023 payment lock (Completed precondition unchanged)
   WHAT:        Document that ReadyForPayment is terminal; only Auditor + Completed may call this procedure
   ============================================================================ */

/* ============================================================================
   NAME:        mark_ready_named_history_insert
   WHEN:        2026-09-15
   WHY:         Positional history INSERT failed after schema drift so ReadyForPayment never persisted
   WHAT:        usp_MarkRequestReadyForPayment history write uses explicit column list
   ============================================================================ */

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
    @ActionBy NVARCHAR(100),
    @Remarks NVARCHAR(MAX) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NowUtc DATETIME2(0) = SYSUTCDATETIME();

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

    /* 2️⃣ Must be Completed.
          EnsureHeaderCompletedForPayment writes Pending → Completed before this call. */
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

    /* 3️⃣ Consume the Auditor assignment that HF231 left Pending. */
    UPDATE dbo.SalesApprovalRequestApprover
    SET
          Status     = 'Approved'
        , ActionBy   = @ActionBy
        , ActionDate = @NowUtc
        , Remarks    = ISNULL(@Remarks, Remarks)
    WHERE ApprovalRequestId = @ApprovalRequestId
      AND ApproverLoginId = @ActionBy
      AND ApproverRole = 'Auditor'
      AND Status = 'Pending';

    /* 4️⃣ Update header */
    UPDATE SalesApprovalRequest
    SET
        Status = 'ReadyForPayment',
        ModifiedBy = @ActionBy,
        ModifiedOn = SYSUTCDATETIME()
    WHERE ApprovalRequestId = @ApprovalRequestId;

    INSERT INTO dbo.ApprovalRequestStatusHistory
    (
          ApprovalRequestId
        , OldStatus
        , NewStatus
        , ChangedBy
        , Remarks
    )
    VALUES
    (
          @ApprovalRequestId
        , 'Completed'
        , 'ReadyForPayment'
        , @ActionBy
        , 'Auditor marked Ready for Payment'
    );
END
