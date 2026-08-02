/*
    Object:      [dbo].[usp_ForwardApprovalRequest]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE   PROCEDURE [dbo].[usp_ForwardApprovalRequest]
(
      @ApprovalRequestId INT
    , @ActionBy          NVARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY

        BEGIN TRANSACTION;

        ------------------------------------------------------------
        -- Status Constants
        ------------------------------------------------------------

        DECLARE
              @STATUS_PENDING    NVARCHAR(20) = N'Pending'
            , @STATUS_APPROVED   NVARCHAR(20) = N'Approved'
            , @STATUS_RETURNED   NVARCHAR(20) = N'Returned'
            , @STATUS_REJECTED   NVARCHAR(20) = N'Rejected';

        ------------------------------------------------------------
        -- Variables
        ------------------------------------------------------------

        DECLARE
              @CurrentApproverLoginId NVARCHAR(100)
            , @CurrentApproverLevel   INT
            , @CurrentRequestStatus   NVARCHAR(50)

            , @NextApproverLoginId    NVARCHAR(100)
            , @NextApproverLevel      INT

            , @PendingActivities      INT
            , @UtcNow                 DATETIME2(7);

        SET @UtcNow = SYSUTCDATETIME();

        ------------------------------------------------------------
        -- Read & Lock Request
        ------------------------------------------------------------

        SELECT
              @CurrentApproverLoginId = r.CurrentApproverLoginId
            , @CurrentApproverLevel   = r.CurrentApproverLevel
            , @CurrentRequestStatus   = r.Status
        FROM SalesApprovalRequest r
             WITH (UPDLOCK, HOLDLOCK)
        WHERE r.ApprovalRequestId = @ApprovalRequestId;

        IF @CurrentApproverLevel IS NULL
        BEGIN
            THROW 51001,
                  'Approval request not found.',
                  1;
        END;

        ------------------------------------------------------------
        -- Request must be Pending
        ------------------------------------------------------------

        IF @CurrentRequestStatus <> @STATUS_PENDING
        BEGIN
            THROW 51002,
                  'Only Pending requests can be forwarded.',
                  1;
        END;

        ------------------------------------------------------------
        -- Verify Current Approver
        ------------------------------------------------------------

        IF @CurrentApproverLoginId <> @ActionBy
        BEGIN
            THROW 51003,
                  'Only the current approver can forward this request.',
                  1;
        END;

        ------------------------------------------------------------
        -- Ensure all activities are completed
        ------------------------------------------------------------

        SELECT
            @PendingActivities = COUNT(*)
        FROM SalesApprovalRequestDetail
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND DetailStatus IN
          (
              @STATUS_PENDING,
              @STATUS_RETURNED
          );

        IF @PendingActivities > 0
        BEGIN
            THROW 51004,
                  'Some activities are still Pending or Returned.',
                  1;
        END;

        ------------------------------------------------------------
        -- Find Next Required Approver
        ------------------------------------------------------------

        SELECT TOP (1)
              @NextApproverLoginId = a.ApproverLoginId
            , @NextApproverLevel   = a.ApproverLevel
        FROM SalesApprovalRequestApprover a
        WHERE a.ApprovalRequestId = @ApprovalRequestId
		  AND a.IsRequired = 1
		  AND a.ApproverLevel > @CurrentApproverLevel
		  AND a.Status = @STATUS_PENDING

		------------------------------------------------------------
        -- Forward to Next Approver
        ------------------------------------------------------------

		------------------------------------------------------------
-- Forward to Next Approver
------------------------------------------------------------

IF @NextApproverLevel IS NOT NULL
BEGIN

    --------------------------------------------------------
    -- Complete Current Approver
    --------------------------------------------------------

    UPDATE SalesApprovalRequestApprover
       SET Status      = @STATUS_APPROVED,
           ActionBy    = @ActionBy,
           ActionDate  = @UtcNow,
           ModifiedBy  = @ActionBy,
           ModifiedOn  = @UtcNow
     WHERE ApprovalRequestId = @ApprovalRequestId
       AND ApproverLoginId   = @ActionBy
       AND ApproverLevel     = @CurrentApproverLevel
       AND Status            = @STATUS_PENDING;

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 51005,
              'Unable to complete current approver.',
              1;
    END;

    --------------------------------------------------------
    -- Move Header to Next Approver
    --------------------------------------------------------

    UPDATE SalesApprovalRequest
       SET CurrentApproverLoginId = @NextApproverLoginId,
           CurrentApproverLevel   = @NextApproverLevel,
           ModifiedBy             = @ActionBy,
           ModifiedOn             = @UtcNow
     WHERE ApprovalRequestId = @ApprovalRequestId;

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 51006,
              'Unable to update approval request.',
              1;
    END;

    --------------------------------------------------------
    -- Activate Next Approver
    --------------------------------------------------------

    UPDATE SalesApprovalRequestApprover
       SET Status      = @STATUS_PENDING,
           ModifiedBy  = @ActionBy,
           ModifiedOn  = @UtcNow
     WHERE ApprovalRequestId = @ApprovalRequestId
       AND ApproverLoginId   = @NextApproverLoginId
       AND ApproverLevel     = @NextApproverLevel;

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 51007,
              'Unable to activate next approver.',
              1;
    END;

    COMMIT TRANSACTION;

    SELECT
          CAST(1 AS BIT) AS Success,
          'Request forwarded successfully.' AS Message,
          @ApprovalRequestId AS ApprovalRequestId,
          @NextApproverLoginId AS CurrentApproverLoginId,
          @NextApproverLevel AS CurrentApproverLevel,
          @STATUS_PENDING AS RequestStatus;

END
ELSE
BEGIN

    --------------------------------------------------------
    -- Complete Final Approver
    --------------------------------------------------------

    UPDATE SalesApprovalRequestApprover
       SET Status      = @STATUS_APPROVED,
           ActionBy    = @ActionBy,
           ActionDate  = @UtcNow,
           ModifiedBy  = @ActionBy,
           ModifiedOn  = @UtcNow
     WHERE ApprovalRequestId = @ApprovalRequestId
       AND ApproverLoginId   = @ActionBy
       AND ApproverLevel     = @CurrentApproverLevel
       AND Status            = @STATUS_PENDING;

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 51008,
              'Unable to complete final approver.',
              1;
    END;

    --------------------------------------------------------
    -- Final Approval
    --------------------------------------------------------

    UPDATE SalesApprovalRequest
       SET Status                 = @STATUS_APPROVED,
           CurrentApproverLoginId = NULL,
           CurrentApproverLevel   = NULL,
           ModifiedBy             = @ActionBy,
           ModifiedOn             = @UtcNow
     WHERE ApprovalRequestId = @ApprovalRequestId;

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 51009,
              'Unable to complete final approval.',
              1;
    END;

    COMMIT TRANSACTION;

    SELECT
          CAST(1 AS BIT) AS Success,
          'Request finally approved.' AS Message,
          @ApprovalRequestId AS ApprovalRequestId,
          NULL AS CurrentApproverLoginId,
          NULL AS CurrentApproverLevel,
          @STATUS_APPROVED AS RequestStatus;

END;


    END TRY
    BEGIN CATCH

        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SELECT
              CAST(0 AS BIT) AS Success
            , ERROR_NUMBER() AS ErrorNumber
            , ERROR_MESSAGE() AS ErrorMessage
            , ERROR_PROCEDURE() AS ErrorProcedure
            , ERROR_LINE() AS ErrorLine;

    END CATCH
END;
