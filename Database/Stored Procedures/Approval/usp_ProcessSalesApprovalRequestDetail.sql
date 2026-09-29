/*
    Object:      [dbo].[usp_ProcessSalesApprovalRequestDetail]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_ProcessSalesApprovalRequestDetail]
(
    @ApprovalRequestDetailId INT,
    @ApprovalRequestId INT,
    @Action NVARCHAR(10),              -- APPROVE / REJECT
    @ActionBy NVARCHAR(100),
    @ActionRemarks NVARCHAR(1000)=NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @CurrentStatus NVARCHAR(50),
        @UserLevel INT,
        @ActivityId INT,
        @NowUtc DATETIME2 = SYSUTCDATETIME();

    ---------------------------------------------------------
    -- 1. Validate Action
    ---------------------------------------------------------
    IF @Action NOT IN ('APPROVE','REJECT')
    BEGIN
        RAISERROR('Invalid action.',16,1);
        RETURN;
    END

    ---------------------------------------------------------
    -- 2. Load Detail
    ---------------------------------------------------------
    SELECT
        @CurrentStatus = DetailStatus,
        @ActivityId = ActivityId
    FROM SalesApprovalRequestDetail
    WHERE ApprovalRequestDetailId=@ApprovalRequestDetailId
      AND ApprovalRequestId=@ApprovalRequestId;

    IF @@ROWCOUNT=0
    BEGIN
        RAISERROR('Invalid detail id or mismatch.',16,1);
        RETURN;
    END

    ---------------------------------------------------------
    -- 3. Validate Current Approver
    ---------------------------------------------------------
    SELECT
        @UserLevel = CurrentApproverLevel
    FROM SalesApprovalRequest
    WHERE ApprovalRequestId=@ApprovalRequestId
      AND CurrentApproverLoginId=@ActionBy;

    IF @UserLevel IS NULL
    BEGIN
        RAISERROR('You are not the current active approver.',16,1);
        RETURN;
    END

    ---------------------------------------------------------
    -- 4. Prevent Duplicate Processing
    ---------------------------------------------------------
    IF @CurrentStatus='Approved'
    BEGIN
        RAISERROR('This day has already been approved.',16,1);
        RETURN;
    END

    IF @CurrentStatus='Rejected'
    BEGIN
        RAISERROR('This day has already been returned.',16,1);
        RETURN;
    END

    ---------------------------------------------------------
    -- 5. REJECT
    ---------------------------------------------------------
    IF @Action='REJECT'
    BEGIN

        IF ISNULL(LTRIM(RTRIM(@ActionRemarks)),'')=''
        BEGIN
            RAISERROR('Remarks required for rejection.',16,1);
            RETURN;
        END

        UPDATE SalesApprovalRequestDetail
        SET
            DetailStatus='Rejected',
            DetailApprovedLevel=@UserLevel,
            DetailApprovedBy=@ActionBy,
            DetailApprovedOn=@NowUtc,
            DetailRemarks=@ActionRemarks,
            ModifiedBy=@ActionBy,
            ModifiedOn=@NowUtc
        WHERE ApprovalRequestDetailId=@ApprovalRequestDetailId;

        INSERT INTO ApprovalRequestStatusHistory
        (
            ApprovalRequestId,
            OldStatus,
            NewStatus,
            ChangedBy,
            ChangedOn,
            Remarks
        )
        VALUES
        (
            @ApprovalRequestId,
            'Pending',
            'Pending',
            @ActionBy,
            @NowUtc,
            CONCAT('Row Returned : ',@ActionRemarks)
        );

        RETURN;
    END

    ---------------------------------------------------------
    -- 6. APPROVE
    ---------------------------------------------------------
    IF @Action='APPROVE'
    BEGIN

        -----------------------------------------------------
        -- Parent Day can be approved ONLY when
        -- all active expense lines are approved.
        -----------------------------------------------------

        IF EXISTS
        (
            SELECT 1
            FROM dbo.SalesDailyExpense
            WHERE ActivityId=@ActivityId
              AND IsDeleted=0
              AND ISNULL(ClaimStatus,'Pending')<>'Approved'
        )
        BEGIN

            UPDATE SalesApprovalRequestDetail
            SET
                DetailStatus='Pending',
                DetailRemarks='Awaiting approval of all active expense line items.',
                ModifiedBy=@ActionBy,
                ModifiedOn=@NowUtc
            WHERE ApprovalRequestDetailId=@ApprovalRequestDetailId;

            INSERT INTO ApprovalRequestStatusHistory
            (
                ApprovalRequestId,
                OldStatus,
                NewStatus,
                ChangedBy,
                ChangedOn,
                Remarks
            )
            VALUES
            (
                @ApprovalRequestId,
                'Pending',
                'Pending',
                @ActionBy,
                @NowUtc,
                'Parent day kept pending as one or more expense line items are not yet approved.'
            );

            RETURN;
        END

        -----------------------------------------------------
        -- All child expense lines approved
        -----------------------------------------------------

        UPDATE SalesApprovalRequestDetail
        SET
            DetailStatus='Approved',
            DetailApprovedLevel=@UserLevel,
            DetailApprovedBy=@ActionBy,
            DetailApprovedOn=@NowUtc,
            DetailRemarks=@ActionRemarks,
            ModifiedBy=@ActionBy,
            ModifiedOn=@NowUtc
        WHERE ApprovalRequestDetailId=@ApprovalRequestDetailId;

        INSERT INTO ApprovalRequestStatusHistory
        (
            ApprovalRequestId,
            OldStatus,
            NewStatus,
            ChangedBy,
            ChangedOn,
            Remarks
        )
        VALUES
        (
            @ApprovalRequestId,
            'Pending',
            'Pending',
            @ActionBy,
            @NowUtc,
            'Row Approved'
        );

    END
END
