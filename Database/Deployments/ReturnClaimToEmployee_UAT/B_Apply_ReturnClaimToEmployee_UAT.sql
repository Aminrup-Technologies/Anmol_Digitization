/*
    Package:     Database/Deployments/ReturnClaimToEmployee_UAT
    Script:      B_Apply_ReturnClaimToEmployee_UAT.sql
    Target:      AnmolDristiDev on MUMANMOAPP01\SQLEXPRESS only.
    Refuses:     AnmolDristi and every other database name.
    Module:      dbo.usp_ReturnClaimToEmployee only.
    Data:        No claim, approver, history, detail, activity, or expense row is written.

    NOT AUTHORIZED TO EXECUTE.
    Approval to prepare this script is not approval to run it.
    @Execute defaults to 0 and returns before any catalog change.
    Generating this file did not execute it.

    UAT preimage, read only from AnmolDristiDev on 2026-10-14:
      object_id 865438157
      modify_date 2026-06-15 11:10:29.793
      LEN 2577 / DATALENGTH 5154
      SHA2_256 E1093DEE0BFD51FA30A10D481DE33E819905E55D63FBFA60B3CC5527406434C1
      uses_ansi_nulls = 1
      uses_quoted_identifier = 1
      The embedded preimage is that captured module, including its leading
      carriage return and line feed.

    Repository body, read from
      Database/Stored Procedures/Approval/usp_ReturnClaimToEmployee.sql
      The file already uses CREATE plus three spaces. Its UTF-16LE SHA-256 is
      75E03EDE68201556CEED37C06DEBF1C3730FDBE108907355EE36FDC16EAC140A
      and LEN is 13737. SQL Server stores that submitted text as the module.
      This script changes CREATE to ALTER only for the executed batch, with the
      same three spaces, then requires the stored module to equal the
      repository text.

    Hash method: HASHBYTES('SHA2_256', CONVERT(varbinary(max), definition))
    over the nvarchar module as UTF-16LE, with no BOM. LEN is the nvarchar
    character count. The embedded literals are checked with that same method
    before the procedure is changed.

    dbo.usp_RecalculateApprovalRequest and dbo.usp_SubmitSalesDailyActivities
    are read and then left unchanged. No other procedure, application file,
    or release baseline is changed.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Execute bit = 0;
IF @Execute <> 1
BEGIN
    PRINT N'NOT EXECUTED | @Execute is 0. dbo.usp_ReturnClaimToEmployee was not changed.';
    RETURN;
END;

BEGIN TRY
    IF @@TRANCOUNT <> 0
        THROW 58510, N'ABORT: this script must start with no open transaction. Procedure not changed.', 1;
    IF DB_NAME() = N'AnmolDristi'
        THROW 58501, N'ABORT: AnmolDristi is not the UAT target. Procedure not changed.', 1;
    IF DB_NAME() <> N'AnmolDristiDev'
        THROW 58502, N'ABORT: connected database is not AnmolDristiDev. Procedure not changed.', 1;
    IF UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
        THROW 58503, N'ABORT: server is not MUMANMOAPP01\SQLEXPRESS. Procedure not changed.', 1;

    DECLARE @ObjectId int = OBJECT_ID(N'dbo.usp_ReturnClaimToEmployee', N'P');
    IF @ObjectId IS NULL OR @ObjectId <> 865438157
        THROW 58504, N'ABORT: dbo.usp_ReturnClaimToEmployee is missing or its object id is not 865438157. Procedure not changed.', 1;

    DECLARE @Definition nvarchar(max);
    DECLARE @UsesAnsiNulls bit;
    DECLARE @UsesQuotedIdentifier bit;
    SELECT
          @Definition = m.definition
        , @UsesAnsiNulls = m.uses_ansi_nulls
        , @UsesQuotedIdentifier = m.uses_quoted_identifier
    FROM sys.sql_modules AS m
    WHERE m.object_id = @ObjectId;

    IF ISNULL(@UsesAnsiNulls, 0) <> 1 OR ISNULL(@UsesQuotedIdentifier, 0) <> 1
        THROW 58511, N'ABORT: ANSI_NULLS or QUOTED_IDENTIFIER is not ON. Procedure not changed.', 1;

    DECLARE @Previous nvarchar(max) = N'
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
            e.ClaimStatus = ''Draft'',
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
            a.ClaimStatus = ''Draft'',
            a.ApprovalRequestId = NULL,     -- Detaches it so it shows up in the employee''s draft pool
            a.SubmittedBy = NULL,
            a.SubmittedOn = NULL,
            a.ModifiedByUserId = @ActionBy,
            a.ModifiedDateTime = SYSUTCDATETIME()
        FROM dbo.SalesDailyActivity a
        INNER JOIN dbo.SalesApprovalRequestDetail d ON a.ActivityId = d.ActivityId
        WHERE d.ApprovalRequestId = @ApprovalRequestId;

        -- 3. Reset Master Header to ''Rejected'' (🔑 Changed from ''Returned'' to trigger the Submit button)
        UPDATE dbo.SalesApprovalRequest
        SET 
            Status = ''Rejected'',
            CurrentApproverLevel = 0,
            CurrentApproverLoginId = NULL,
            ModifiedBy = @ActionBy,
            ModifiedOn = SYSUTCDATETIME()
        WHERE ApprovalRequestId = @ApprovalRequestId;

        -- 4. Cancel all pending workflow steps so it drops off all approver dashboards
        UPDATE dbo.SalesApprovalRequestApprover
        SET 
            Status = ''Cancelled'',
            Remarks = ''Claim returned to employee for correction'',
            ModifiedBy = @ActionBy,
            ModifiedOn = SYSUTCDATETIME()
        WHERE ApprovalRequestId = @ApprovalRequestId AND Status = ''Pending'';

        -- 5. Log it in the Audit Trail
        INSERT INTO dbo.ApprovalRequestStatusHistory 
        (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, Remarks)
        VALUES 
        (@ApprovalRequestId, ''Pending'', ''Rejected'', @ActionBy, CONCAT(''Global Return to Employee: '', @ReturnReason));

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
'; /*__END_PREVIOUS__*/
    IF LEN(@Previous) <> 2577 OR DATALENGTH(@Previous) <> 5154
        THROW 58512, N'ABORT: embedded preimage length is not 2577 / 5154. Procedure not changed.', 1;
    IF CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Previous)), 2)
        <> 'E1093DEE0BFD51FA30A10D481DE33E819905E55D63FBFA60B3CC5527406434C1'
        THROW 58513, N'ABORT: embedded preimage hash is not the captured UAT module. Procedure not changed.', 1;
    IF @Definition IS NULL OR @Definition <> @Previous OR LEN(@Definition) <> 2577 OR DATALENGTH(@Definition) <> 5154
        THROW 58505, N'ABORT: live definition is not the 2577-character preimage. Procedure not changed.', 1;
    IF CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Definition)), 2)
        <> 'E1093DEE0BFD51FA30A10D481DE33E819905E55D63FBFA60B3CC5527406434C1'
        THROW 58506, N'ABORT: live definition hash is not the captured UAT preimage. Procedure not changed.', 1;

    DECLARE @RecalcId int = OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest', N'P');
    DECLARE @RecalcDefinition nvarchar(max);
    SELECT @RecalcDefinition = m.definition
    FROM sys.sql_modules AS m
    WHERE m.object_id = @RecalcId;
    IF @RecalcId IS NULL
        OR LEN(@RecalcDefinition) <> 7559
        OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @RecalcDefinition)), 2)
           <> '05D126B559E6FB182604F0E37C2921B3F4A357BD02D8B0A9706714C6B5F0BF6D'
        THROW 58507, N'ABORT: dbo.usp_RecalculateApprovalRequest is not the verified UAT module. Procedure not changed.', 1;

    DECLARE @SubmitDefinition nvarchar(max);
    SELECT @SubmitDefinition = m.definition
    FROM sys.sql_modules AS m
    INNER JOIN sys.objects AS o ON o.object_id = m.object_id
    WHERE o.type = N'P'
      AND o.name = N'usp_SubmitSalesDailyActivities';
    IF @SubmitDefinition IS NULL
        OR CHARINDEX(N'Resubmission can contain only Returned activities', @SubmitDefinition) = 0
        OR CHARINDEX(N'Draft and Rejected are closed predecessors', @SubmitDefinition) = 0
        THROW 58508, N'ABORT: dbo.usp_SubmitSalesDailyActivities does not contain the same-request Returned resubmission contract. Procedure not changed.', 1;

    DECLARE @Next nvarchar(max) = N'/* ============================================================================
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

    DECLARE @TrimmedReason NVARCHAR(1000) = NULLIF(LTRIM(RTRIM(@ReturnReason)), N'''');
    DECLARE @TrimmedActor NVARCHAR(100) = NULLIF(LTRIM(RTRIM(@ActionBy)), N'''');

    IF @ApprovalRequestId IS NULL OR @ApprovalRequestId <= 0
    BEGIN
        RAISERROR(''ApprovalRequestId is required.'', 16, 1);
        RETURN;
    END;

    IF @TrimmedActor IS NULL
    BEGIN
        RAISERROR(''ActionBy is required.'', 16, 1);
        RETURN;
    END;

    IF @TrimmedReason IS NULL
    BEGIN
        RAISERROR(''A return reason is required.'', 16, 1);
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
            RAISERROR(''Approval request %d was not found.'', 16, 1, @ApprovalRequestId);
        END;

        IF ISNULL(@OldRequestStatus, N'''') <> N''Pending''
        BEGIN
            RAISERROR(''This approval request is not currently awaiting approval and cannot be returned.'', 16, 1);
        END;

        IF ISNULL(@CurrentApproverLevel, 0) <> 1
           OR @CurrentApproverLoginId IS NULL
           OR UPPER(LTRIM(RTRIM(@CurrentApproverLoginId))) <> UPPER(@TrimmedActor)
        BEGIN
            RAISERROR(''You are not the current L1 approver for this approval request.'', 16, 1);
        END;

        DECLARE @PendingL1Count INT;

        SELECT @PendingL1Count = COUNT(*)
        FROM dbo.SalesApprovalRequestApprover a WITH (UPDLOCK, HOLDLOCK)
        WHERE a.ApprovalRequestId = @ApprovalRequestId
          AND a.ApproverLevel = 1
          AND a.Status = N''Pending''
          AND UPPER(LTRIM(RTRIM(a.ApproverRole))) = N''L1''
          AND UPPER(LTRIM(RTRIM(a.ApproverLoginId))) = UPPER(@TrimmedActor);

        IF @PendingL1Count = 0
        BEGIN
            RAISERROR(''You are not the current L1 approver for this approval request.'', 16, 1);
        END;

        IF @PendingL1Count > 1
        BEGIN
            RAISERROR(''More than one pending L1 approver row matches the current approver, so the return was not applied.'', 16, 1);
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.SalesDailyActivity a WITH (UPDLOCK, HOLDLOCK)
            WHERE a.ApprovalRequestId = @ApprovalRequestId
              AND ISNULL(a.ClaimStatus, N'''') <> N''Rejected''
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.SalesApprovalRequestDetail d WITH (UPDLOCK, HOLDLOCK)
                  WHERE d.ActivityId = a.ActivityId
                    AND d.ApprovalRequestId = @ApprovalRequestId
              )
        )
        BEGIN
            RAISERROR(''One or more non-rejected activities belong to this approval request but have no corresponding detail row.'', 16, 1);
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
            WHERE ISNULL(l.DetailStatus, N'''') <> N''Rejected''
              AND ISNULL(l.ActivityClaimStatus, N'''') <> N''Rejected''
              AND
              (
                    l.ActivityRequestId IS NULL
                 OR l.ActivityRequestId <> @ApprovalRequestId
              )
        )
        BEGIN
            RAISERROR(''One or more activities are not owned by this approval request.'', 16, 1);
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
        WHERE ISNULL(l.DetailStatus, N'''') <> N''Rejected''
          AND ISNULL(l.ActivityClaimStatus, N'''') <> N''Rejected''
          AND l.ActivityRequestId = @ApprovalRequestId;

        IF NOT EXISTS (SELECT 1 FROM @Eligible)
        BEGIN
            RAISERROR(''There are no non-rejected activities on this approval request.'', 16, 1);
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
        SET d.DetailStatus = N''Returned''
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN @Eligible e
            ON e.ApprovalRequestDetailId = d.ApprovalRequestDetailId
        WHERE d.ApprovalRequestId = @ApprovalRequestId
          AND ISNULL(d.DetailStatus, N'''') <> N''Rejected'';

        SET @DetailUpdateCount = @@ROWCOUNT;

        IF @DetailUpdateCount <> @EligibleCount
        BEGIN
            RAISERROR(''The current detail status transition was inconsistent.'', 16, 1);
        END;

        -- CK_SalesDailyActivity_ClaimStatus allows Returned. Pending is a detail
        -- status; a claim awaiting L1 is stored on the activity as Submitted.
        UPDATE a
        SET
              a.ClaimStatus = N''Returned''
            , a.ModifiedByUserId = @TrimmedActor
            , a.ModifiedDateTime = @NowIst
        FROM dbo.SalesDailyActivity a
        INNER JOIN @Eligible e
            ON e.ActivityId = a.ActivityId
        WHERE a.ApprovalRequestId = @ApprovalRequestId
          AND ISNULL(a.ClaimStatus, N'''') <> N''Rejected'';

        SET @ActivityUpdateCount = @@ROWCOUNT;

        IF @ActivityUpdateCount <> @EligibleCount
        BEGIN
            RAISERROR(''The activity status transition was inconsistent.'', 16, 1);
        END;

        -- Same expense predicate as usp_ProcessApprovalAction RETURN: active rows
        -- whose ClaimStatus is not Rejected, including Approved and Submitted.
        -- CK_SalesDailyExpense_NoUpdate_WhenDeleted requires ClaimStatus = Draft
        -- when IsDeleted = 1, so deleted rows are not updated and are not reactivated.
        -- Amount, Remarks, and IsDeleted are not assigned. The return reason stays
        -- on the L1 approver row and the single history row below.
        UPDATE ex
        SET
              ex.ClaimStatus = N''Returned''
            , ex.ClaimProcessedBy = @TrimmedActor
            , ex.ClaimProcessedDate = @NowIst
            , ex.ModifiedBy = @TrimmedActor
            , ex.ModifiedOn = @NowUtc
        FROM dbo.SalesDailyExpense ex
        INNER JOIN @Eligible e
            ON e.ActivityId = ex.ActivityId
        WHERE ex.IsDeleted = 0
          AND ISNULL(ex.ClaimStatus, N'''') <> N''Rejected'';

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
              Status = N''Returned''
            , ActionBy = @TrimmedActor
            , ActionDate = @NowUtc
            , Remarks = ISNULL(@TrimmedReason, Remarks)
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND ApproverLevel = 1
          AND Status = N''Pending''
          AND UPPER(LTRIM(RTRIM(ApproverRole))) = N''L1''
          AND UPPER(LTRIM(RTRIM(ApproverLoginId))) = UPPER(@TrimmedActor);

        IF @@ROWCOUNT <> 1
        BEGIN
            RAISERROR(''The pending L1 approver row could not be updated.'', 16, 1);
        END;

        -- Status and the return actor only. The six approved-amount columns
        -- written above are not assigned here, and the approver pointer is not
        -- assigned here.
        UPDATE dbo.SalesApprovalRequest
        SET
              Status = N''Returned''
            , ModifiedBy = @TrimmedActor
            , ModifiedOn = @NowUtc
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND Status = N''Pending''
          AND CurrentApproverLevel = 1
          AND UPPER(LTRIM(RTRIM(CurrentApproverLoginId))) = UPPER(@TrimmedActor);

        IF @@ROWCOUNT <> 1
        BEGIN
            RAISERROR(''The approval request header transition was inconsistent.'', 16, 1);
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
            , N''Pending''
            , N''Returned''
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
'; /*__END_NEXT__*/
    IF LEN(@Next) <> 13737 OR DATALENGTH(@Next) <> 27474
        THROW 58514, N'ABORT: embedded repository body length is not 13737 / 27474. Procedure not changed.', 1;
    IF CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Next)), 2)
        <> '75E03EDE68201556CEED37C06DEBF1C3730FDBE108907355EE36FDC16EAC140A'
        THROW 58515, N'ABORT: embedded repository body hash is not the reviewed module. Procedure not changed.', 1;
    IF CHARINDEX(N'MAX(ISNULL(DetailVersion, 1))', @Next) = 0
        OR CHARINDEX(N'MAX(d2.ApprovalRequestDetailId)', @Next) = 0
        OR CHARINDEX(N'dbo.usp_RecalculateApprovalRequest', @Next) = 0
        OR CHARINDEX(N'Status = N''Returned''', @Next) = 0
        THROW 58516, N'ABORT: embedded repository body is missing latest-version, recalculation, or Returned behavior. Procedure not changed.', 1;

    DECLARE @CreateAt int = CHARINDEX(N'CREATE   PROCEDURE', @Next);
    IF @CreateAt < 1
        THROW 58517, N'ABORT: CREATE token was not found in the repository body. Procedure not changed.', 1;

    DECLARE @Alter nvarchar(max) = STUFF(@Next, @CreateAt, LEN(N'CREATE   PROCEDURE'), N'ALTER   PROCEDURE');

    BEGIN TRAN;
    EXEC (@Alter);

    DECLARE @Stored nvarchar(max) = OBJECT_DEFINITION(@ObjectId);
    IF @Stored IS NULL
        OR @Stored <> @Next
        OR LEN(@Stored) <> 13737
        OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Stored)), 2)
           <> '75E03EDE68201556CEED37C06DEBF1C3730FDBE108907355EE36FDC16EAC140A'
    BEGIN
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;
        PRINT N'FAIL | Stored dbo.usp_ReturnClaimToEmployee does not match LEN 13737 and SHA-256 75E03EDE68201556CEED37C06DEBF1C3730FDBE108907355EE36FDC16EAC140A.';
        PRINT N'The explicit transaction was rolled back before COMMIT. That rollback withdraws only this uncommitted procedure change.';
        PRINT N'A procedure definition change that has already committed cannot be undone by rolling back a later transaction.';
        PRINT N'If the live hash is 75E03EDE68201556CEED37C06DEBF1C3730FDBE108907355EE36FDC16EAC140A, run D_Rollback_ReturnClaimToEmployee_UAT.sql after a separate execution approval.';
        PRINT N'If the live hash is neither the preimage nor that post-change hash, do not run the rollback script. Capture the definition and stop.';
        PRINT N'Neither script reverses claim, detail, activity, expense, approver, or history rows that an earlier call already committed.';
        THROW 58518, N'ABORT: post-change verification failed. See the FAIL messages above.', 1;
    END;

    COMMIT TRAN;
    PRINT N'COMMITTED | dbo.usp_ReturnClaimToEmployee matches LEN 13737 and SHA-256 75E03EDE68201556CEED37C06DEBF1C3730FDBE108907355EE36FDC16EAC140A.';
    PRINT N'This commit is final for the procedure text. Rolling back a later transaction does not restore the preimage.';
    PRINT N'Future calls use the repository body. This script did not return, resubmit, or recalculate a claim.';
    PRINT N'Committed claim transitions already stored before this script are unchanged. Recovery of the procedure text requires D_Rollback_ReturnClaimToEmployee_UAT.sql and affects future calls only.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;
    THROW;
END CATCH;