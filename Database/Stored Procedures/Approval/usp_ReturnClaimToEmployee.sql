/* ============================================================================
   NAME:        whole_claim_return_sets_eligible_current_lines
   WHEN:        2026-10-11
   WHY:         Employee correction requires the request Returned, the latest
                detail Returned, and the activity not Rejected. Leaving Approved
                and Pending lines unchanged blocked same-request correction.
   WHAT:        Pending L1 whole-claim return keeps ApprovalRequestId and the
                current-approver pointer. It sets the request, the matching L1
                approver row, each non-rejected latest detail, and that activity
                to Returned. Active non-rejected expenses on those activities
                become Returned. In that same transaction, after those statuses
                are set, it calls dbo.usp_RecalculateApprovalRequest. Rejected
                lines, historical versions, soft-deleted expenses, expense
                amounts, and expense remarks stay.
   ============================================================================ */

/*
    Object:      [dbo].[usp_ReturnClaimToEmployee]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Signature is unchanged: @ApprovalRequestId, @ActionBy, @ReturnReason.
                 Latest-row ownership matches usp_ProcessApprovalAction:
                 MAX(ISNULL(DetailVersion, 1)) then MAX(ApprovalRequestDetailId).
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
    SET XACT_ABORT ON;

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

    DECLARE @NowUtc DATETIME2(3) = SYSUTCDATETIME();

    DECLARE @Latest TABLE
    (
          ApprovalRequestDetailId INT NOT NULL PRIMARY KEY
        , ActivityId INT NOT NULL UNIQUE
        , DetailStatus NVARCHAR(50) NULL
        , ActivityClaimStatus NVARCHAR(50) NULL
        , ActivityRequestId INT NULL
    );

    DECLARE @Eligible TABLE
    (
          ApprovalRequestDetailId INT NOT NULL PRIMARY KEY
        , ActivityId INT NOT NULL UNIQUE
    );

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
            RAISERROR('This approval request is not currently awaiting approval and cannot be returned.', 16, 1);
        END;

        IF ISNULL(@CurrentApproverLevel, 0) <> 1
           OR @CurrentApproverLoginId IS NULL
           OR UPPER(LTRIM(RTRIM(@CurrentApproverLoginId))) <> UPPER(@TrimmedActor)
        BEGIN
            RAISERROR('You are not the current L1 approver for this approval request.', 16, 1);
        END;

        DECLARE @PendingL1Count INT;

        SELECT @PendingL1Count = COUNT(*)
        FROM dbo.SalesApprovalRequestApprover a WITH (UPDLOCK, HOLDLOCK)
        WHERE a.ApprovalRequestId = @ApprovalRequestId
          AND a.ApproverLevel = 1
          AND a.Status = N'Pending'
          AND UPPER(LTRIM(RTRIM(a.ApproverRole))) = N'L1'
          AND UPPER(LTRIM(RTRIM(a.ApproverLoginId))) = UPPER(@TrimmedActor);

        IF @PendingL1Count = 0
        BEGIN
            RAISERROR('You are not the current L1 approver for this approval request.', 16, 1);
        END;

        IF @PendingL1Count > 1
        BEGIN
            RAISERROR('More than one pending L1 approver row matches the current approver, so the return was not applied.', 16, 1);
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.SalesDailyActivity a WITH (UPDLOCK, HOLDLOCK)
            WHERE a.ApprovalRequestId = @ApprovalRequestId
              AND ISNULL(a.ClaimStatus, N'') <> N'Rejected'
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.SalesApprovalRequestDetail d WITH (UPDLOCK, HOLDLOCK)
                  WHERE d.ActivityId = a.ActivityId
                    AND d.ApprovalRequestId = @ApprovalRequestId
              )
        )
        BEGIN
            RAISERROR('One or more non-rejected activities belong to this approval request but have no corresponding detail row.', 16, 1);
        END;

        INSERT INTO @Latest
        (
              ApprovalRequestDetailId
            , ActivityId
            , DetailStatus
            , ActivityClaimStatus
            , ActivityRequestId
        )
        SELECT
              d.ApprovalRequestDetailId
            , d.ActivityId
            , d.DetailStatus
            , a.ClaimStatus
            , a.ApprovalRequestId
        FROM dbo.SalesApprovalRequestDetail d WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN
        (
            SELECT
                  d2.ActivityId
                , MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
            FROM dbo.SalesApprovalRequestDetail d2 WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN
            (
                SELECT
                      ActivityId
                    , MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
                FROM dbo.SalesApprovalRequestDetail WITH (UPDLOCK, HOLDLOCK)
                WHERE ApprovalRequestId = @ApprovalRequestId
                GROUP BY ActivityId
            ) lv
                ON lv.ActivityId = d2.ActivityId
               AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
            WHERE d2.ApprovalRequestId = @ApprovalRequestId
            GROUP BY d2.ActivityId
        ) latest
            ON d.ApprovalRequestDetailId = latest.LatestDetailId
        LEFT JOIN dbo.SalesDailyActivity a WITH (UPDLOCK, HOLDLOCK)
            ON a.ActivityId = d.ActivityId
        WHERE d.ApprovalRequestId = @ApprovalRequestId;

        IF EXISTS
        (
            SELECT 1
            FROM @Latest l
            WHERE ISNULL(l.DetailStatus, N'') <> N'Rejected'
              AND ISNULL(l.ActivityClaimStatus, N'') <> N'Rejected'
              AND
              (
                    l.ActivityRequestId IS NULL
                 OR l.ActivityRequestId <> @ApprovalRequestId
              )
        )
        BEGIN
            RAISERROR('One or more activities are not owned by this approval request.', 16, 1);
        END;

        -- Non-rejected current rows for this request. Rejected details and
        -- rejected activities stay out of this set and are not updated.
        INSERT INTO @Eligible
        (
              ApprovalRequestDetailId
            , ActivityId
        )
        SELECT
              l.ApprovalRequestDetailId
            , l.ActivityId
        FROM @Latest l
        WHERE ISNULL(l.DetailStatus, N'') <> N'Rejected'
          AND ISNULL(l.ActivityClaimStatus, N'') <> N'Rejected'
          AND l.ActivityRequestId = @ApprovalRequestId;

        IF NOT EXISTS (SELECT 1 FROM @Eligible)
        BEGIN
            RAISERROR('There are no non-rejected activities on this approval request.', 16, 1);
        END;

        DECLARE @EligibleCount INT;
        DECLARE @DetailUpdateCount INT;
        DECLARE @ActivityUpdateCount INT;
        DECLARE @NowIst DATETIME = DATEADD(MINUTE, 330, @NowUtc);

        SELECT @EligibleCount = COUNT(*) FROM @Eligible;

        -- Current rows only: @Eligible is the latest version, then the highest
        -- ApprovalRequestDetailId, and it excludes Rejected details and activities.
        -- Historical versions are different ApprovalRequestDetailId values.
        UPDATE d
        SET d.DetailStatus = N'Returned'
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN @Eligible e
            ON e.ApprovalRequestDetailId = d.ApprovalRequestDetailId
        WHERE d.ApprovalRequestId = @ApprovalRequestId
          AND ISNULL(d.DetailStatus, N'') <> N'Rejected';

        SET @DetailUpdateCount = @@ROWCOUNT;

        IF @DetailUpdateCount <> @EligibleCount
        BEGIN
            RAISERROR('The current detail status transition was inconsistent.', 16, 1);
        END;

        -- CK_SalesDailyActivity_ClaimStatus allows Returned. Pending is a detail
        -- status; a claim awaiting L1 is stored on the activity as Submitted.
        UPDATE a
        SET
              a.ClaimStatus = N'Returned'
            , a.ModifiedByUserId = @TrimmedActor
            , a.ModifiedDateTime = @NowIst
        FROM dbo.SalesDailyActivity a
        INNER JOIN @Eligible e
            ON e.ActivityId = a.ActivityId
        WHERE a.ApprovalRequestId = @ApprovalRequestId
          AND ISNULL(a.ClaimStatus, N'') <> N'Rejected';

        SET @ActivityUpdateCount = @@ROWCOUNT;

        IF @ActivityUpdateCount <> @EligibleCount
        BEGIN
            RAISERROR('The activity status transition was inconsistent.', 16, 1);
        END;

        -- Same expense predicate as usp_ProcessApprovalAction RETURN: active rows
        -- whose ClaimStatus is not Rejected, including Approved and Submitted.
        -- CK_SalesDailyExpense_NoUpdate_WhenDeleted requires ClaimStatus = Draft
        -- when IsDeleted = 1, so deleted rows are not updated and are not reactivated.
        -- Amount, Remarks, and IsDeleted are not assigned. The return reason stays
        -- on the L1 approver row and the single history row below.
        UPDATE ex
        SET
              ex.ClaimStatus = N'Returned'
            , ex.ClaimProcessedBy = @TrimmedActor
            , ex.ClaimProcessedDate = @NowIst
            , ex.ModifiedBy = @TrimmedActor
            , ex.ModifiedOn = @NowUtc
        FROM dbo.SalesDailyExpense ex
        INNER JOIN @Eligible e
            ON e.ActivityId = ex.ActivityId
        WHERE ex.IsDeleted = 0
          AND ISNULL(ex.ClaimStatus, N'') <> N'Rejected';

        -- Eligible details are already Returned. dbo.usp_RecalculateApprovalRequest
        -- has no transaction of its own, so this call stays inside the return
        -- transaction and rolls back with it. It is the only approved-amount
        -- formula: latest detail by MAX(ISNULL(DetailVersion, 1)) then
        -- MAX(ApprovalRequestDetailId). It refreshes latest expense snapshots,
        -- then writes ApprovedKM, ApprovedClaimAmount, ApprovedDAAmount,
        -- ApprovedHotelAmount, ApprovedTravelAmount, and FinalApprovedAmount
        -- from latest rows whose DetailStatus is not Rejected or Returned.
        EXEC dbo.usp_RecalculateApprovalRequest
            @ApprovalRequestId = @ApprovalRequestId;

        UPDATE dbo.SalesApprovalRequestApprover
        SET
              Status = N'Returned'
            , ActionBy = @TrimmedActor
            , ActionDate = @NowUtc
            , Remarks = ISNULL(@TrimmedReason, Remarks)
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND ApproverLevel = 1
          AND Status = N'Pending'
          AND UPPER(LTRIM(RTRIM(ApproverRole))) = N'L1'
          AND UPPER(LTRIM(RTRIM(ApproverLoginId))) = UPPER(@TrimmedActor);

        IF @@ROWCOUNT <> 1
        BEGIN
            RAISERROR('The pending L1 approver row could not be updated.', 16, 1);
        END;

        -- Status and the return actor only. The six approved-amount columns
        -- written above are not assigned here, and the approver pointer is not
        -- assigned here.
        UPDATE dbo.SalesApprovalRequest
        SET
              Status = N'Returned'
            , ModifiedBy = @TrimmedActor
            , ModifiedOn = @NowUtc
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND Status = N'Pending'
          AND CurrentApproverLevel = 1
          AND UPPER(LTRIM(RTRIM(CurrentApproverLoginId))) = UPPER(@TrimmedActor);

        IF @@ROWCOUNT <> 1
        BEGIN
            RAISERROR('The approval request header transition was inconsistent.', 16, 1);
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
            , N'Returned'
            , @TrimmedActor
            , @TrimmedReason
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
