/* ============================================================================
   NAME:        auditor_return_to_l1_routing_only
   WHEN:        2026-10-11
   WHY:         The assigned Auditor needs to send a pending level-2 request
                back to the assigned L1 approver for correction without
                changing activities, snapshots, expenses, or amounts.
   WHAT:        Header stays Pending. Current approver becomes the assigned L1
                login at level 1. The Approved L1 row becomes Pending and its
                action fields are cleared. The Auditor row stays Pending.
                One history row records Pending to Pending.
   NOT APPLIED: This file is a definition only. Do not execute it, and do not
                run it against approval request 118, until a separate
                deployment authorization.
   ============================================================================ */

/*
    Object:      [dbo].[usp_ReturnApprovalRequestToL1]
    Type:        STORED PROCEDURE
    Signature:   @ApprovalRequestId, @ActionBy, @ReturnReason.
                 @ActionBy is the trusted session login. This procedure
                 revalidates that login against the locked header and the
                 pending level-2 Auditor row. It does not accept a role.
    Ownership:   Does not read or write detail snapshots, activities, or
                 expenses. Latest-row ownership stays in the procedures
                 that already own those rows: highest detail version,
                 then highest detail id.
    Excluded:    Does not call recalculation, submission, employee return,
                 auditor reopen, day action, or expense-status procedures.
                 Does not write a non-canonical status value.
*/

CREATE OR ALTER PROCEDURE [dbo].[usp_ReturnApprovalRequestToL1]
(
    @ApprovalRequestId INT,
    @ActionBy NVARCHAR(100),
    @ReturnReason NVARCHAR(1000)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @HistoryPrefix NVARCHAR(100) = N'Auditor returned to L1 for correction: ';
    DECLARE @TrimmedReason NVARCHAR(1000) = NULLIF(LTRIM(RTRIM(@ReturnReason)), N'');
    DECLARE @TrimmedActor NVARCHAR(100) = NULLIF(LTRIM(RTRIM(@ActionBy)), N'');

    IF @ApprovalRequestId IS NULL OR @ApprovalRequestId <= 0
    BEGIN
        RAISERROR('ApprovalRequestId is required.', 16, 1);
        RETURN;
    END;

    IF @TrimmedActor IS NULL
    BEGIN
        RAISERROR('ActionBy is required.', 16, 1);
        RETURN;
    END;

    IF @TrimmedReason IS NULL
    BEGIN
        RAISERROR('A return reason is required.', 16, 1);
        RETURN;
    END;

    -- LEN ignores trailing spaces. The prefix ends with a space, so count bytes.
    IF (DATALENGTH(@HistoryPrefix) / 2) + LEN(@TrimmedReason) > 1000
    BEGIN
        RAISERROR('The return reason exceeds 1000 characters once the required history prefix is included.', 16, 1);
        RETURN;
    END;

    DECLARE @HistoryRemarks NVARCHAR(1000) = @HistoryPrefix + @TrimmedReason;
    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE
              @OldRequestStatus NVARCHAR(50)
            , @CurrentApproverLevel INT
            , @CurrentApproverLoginId NVARCHAR(100);

        SELECT
              @OldRequestStatus = r.Status
            , @CurrentApproverLevel = r.CurrentApproverLevel
            , @CurrentApproverLoginId = r.CurrentApproverLoginId
        FROM dbo.SalesApprovalRequest r WITH (UPDLOCK, HOLDLOCK)
        WHERE r.ApprovalRequestId = @ApprovalRequestId;

        IF @OldRequestStatus IS NULL
        BEGIN
            RAISERROR('Approval request %d was not found.', 16, 1, @ApprovalRequestId);
        END;

        IF ISNULL(@OldRequestStatus, N'') <> N'Pending'
        BEGIN
            RAISERROR('This approval request is not currently awaiting approval and cannot be returned to L1.', 16, 1);
        END;

        IF ISNULL(@CurrentApproverLevel, 0) <> 2
           OR @CurrentApproverLoginId IS NULL
           OR UPPER(LTRIM(RTRIM(@CurrentApproverLoginId))) <> UPPER(@TrimmedActor)
        BEGIN
            RAISERROR('You are not the current assigned Auditor for this pending level 2 approval request.', 16, 1);
        END;

        DECLARE @PendingAuditorCount INT;

        SELECT @PendingAuditorCount = COUNT(*)
        FROM dbo.SalesApprovalRequestApprover a WITH (UPDLOCK, HOLDLOCK)
        WHERE a.ApprovalRequestId = @ApprovalRequestId
          AND a.ApproverLevel = 2
          AND a.Status = N'Pending'
          AND UPPER(LTRIM(RTRIM(a.ApproverRole))) = N'AUDITOR'
          AND UPPER(LTRIM(RTRIM(a.ApproverLoginId))) = UPPER(@TrimmedActor);

        IF @PendingAuditorCount = 0
        BEGIN
            RAISERROR('You are not the current assigned Auditor for this pending level 2 approval request.', 16, 1);
        END;

        IF @PendingAuditorCount > 1
        BEGIN
            RAISERROR('More than one pending level 2 Auditor row matches the caller, so the return was not applied.', 16, 1);
        END;

        DECLARE @ApprovedL1Count INT;
        DECLARE @L1ApproverId INT;
        DECLARE @L1LoginId NVARCHAR(100);

        SELECT
              @ApprovedL1Count = COUNT(*)
            , @L1ApproverId = MIN(a.ApprovalRequestApproverId)
            , @L1LoginId = MIN(a.ApproverLoginId)
        FROM dbo.SalesApprovalRequestApprover a WITH (UPDLOCK, HOLDLOCK)
        WHERE a.ApprovalRequestId = @ApprovalRequestId
          AND a.ApproverLevel = 1
          AND a.Status = N'Approved'
          AND UPPER(LTRIM(RTRIM(a.ApproverRole))) = N'L1';

        IF @ApprovedL1Count <> 1
           OR @L1ApproverId IS NULL
           OR NULLIF(LTRIM(RTRIM(@L1LoginId)), N'') IS NULL
        BEGIN
            RAISERROR('The assigned L1 approver row is not a single Approved level 1 row, so the return was not applied.', 16, 1);
        END;

        -- Same Pending reset as resubmission: status back to Pending and the
        -- current-action fields cleared. Login, role, level, and IsRequired stay.
        UPDATE dbo.SalesApprovalRequestApprover
        SET
              Status = N'Pending'
            , ActionBy = NULL
            , ActionDate = NULL
            , Remarks = NULL
            , ModifiedBy = @TrimmedActor
            , ModifiedOn = @NowUtc
        WHERE ApprovalRequestApproverId = @L1ApproverId
          AND ApprovalRequestId = @ApprovalRequestId
          AND ApproverLevel = 1
          AND Status = N'Approved'
          AND UPPER(LTRIM(RTRIM(ApproverRole))) = N'L1';

        IF @@ROWCOUNT <> 1
        BEGIN
            RAISERROR('The Approved L1 approver row could not be returned to Pending.', 16, 1);
        END;

        -- Routing pointer only. Status remains Pending. Amount, activity,
        -- and count columns are not assigned.
        UPDATE dbo.SalesApprovalRequest
        SET
              Status = N'Pending'
            , CurrentApproverLoginId = @L1LoginId
            , CurrentApproverLevel = 1
            , ModifiedBy = @TrimmedActor
            , ModifiedOn = @NowUtc
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND Status = N'Pending'
          AND CurrentApproverLevel = 2
          AND UPPER(LTRIM(RTRIM(CurrentApproverLoginId))) = UPPER(@TrimmedActor);

        IF @@ROWCOUNT <> 1
        BEGIN
            RAISERROR('The approval request header transition was inconsistent.', 16, 1);
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.SalesApprovalRequestApprover a
            WHERE a.ApprovalRequestId = @ApprovalRequestId
              AND a.ApproverLevel = 2
              AND UPPER(LTRIM(RTRIM(a.ApproverRole))) = N'AUDITOR'
              AND UPPER(LTRIM(RTRIM(a.ApproverLoginId))) = UPPER(@TrimmedActor)
              AND a.Status <> N'Pending'
        )
        BEGIN
            RAISERROR('The Auditor approver row did not remain Pending.', 16, 1);
        END;

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
            , N'Pending'
            , N'Pending'
            , @TrimmedActor
            , @HistoryRemarks
        );

        IF @@ROWCOUNT <> 1
        BEGIN
            RAISERROR('The approval request history row was not written.', 16, 1);
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
