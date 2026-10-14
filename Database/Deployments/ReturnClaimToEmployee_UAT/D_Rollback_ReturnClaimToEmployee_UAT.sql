/*
    Package:     Database/Deployments/ReturnClaimToEmployee_UAT
    Script:      D_Rollback_ReturnClaimToEmployee_UAT.sql
    Target:      AnmolDristiDev on MUMANMOAPP01\SQLEXPRESS only.
    Refuses:     AnmolDristi and every other database name.
    Module:      dbo.usp_ReturnClaimToEmployee only.
    Data:        No claim, approver, history, detail, activity, or expense row is written.

    NOT AUTHORIZED TO EXECUTE.
    Approval to prepare this script is not approval to run it.
    @Execute defaults to 0 and returns before any catalog change.
    Generating this file did not execute it.

    This restores the UAT module captured on 2026-10-14:
      object_id 865438157
      LEN 2577 / DATALENGTH 5154
      SHA2_256 E1093DEE0BFD51FA30A10D481DE33E819905E55D63FBFA60B3CC5527406434C1
      uses_ansi_nulls = 1
      uses_quoted_identifier = 1

    It runs only when the current module is still the post-change body:
      LEN 13737
      SHA2_256 75E03EDE68201556CEED37C06DEBF1C3730FDBE108907355EE36FDC16EAC140A

    Any other definition is left unchanged.
    Restoring the procedure affects future calls only. It does not reverse
    claim, detail, activity, expense, approver, or history rows that already
    committed. A later transaction rollback cannot undo this restore after
    this script commits.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Execute bit = 0;
IF @Execute <> 1
BEGIN
    PRINT N'NOT EXECUTED | @Execute is 0. dbo.usp_ReturnClaimToEmployee was not rolled back.';
    RETURN;
END;

BEGIN TRY
    IF @@TRANCOUNT <> 0
        THROW 58530, N'ABORT: this script must start with no open transaction. Procedure not changed.', 1;
    IF DB_NAME() = N'AnmolDristi'
        THROW 58521, N'ABORT: AnmolDristi is not the UAT target. Procedure not changed.', 1;
    IF DB_NAME() <> N'AnmolDristiDev'
        THROW 58522, N'ABORT: connected database is not AnmolDristiDev. Procedure not changed.', 1;
    IF UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
        THROW 58523, N'ABORT: server is not MUMANMOAPP01\SQLEXPRESS. Procedure not changed.', 1;

    DECLARE @ObjectId int = OBJECT_ID(N'dbo.usp_ReturnClaimToEmployee', N'P');
    IF @ObjectId IS NULL OR @ObjectId <> 865438157
        THROW 58524, N'ABORT: dbo.usp_ReturnClaimToEmployee is missing or its object id is not 865438157. Procedure not changed.', 1;

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
        THROW 58531, N'ABORT: ANSI_NULLS or QUOTED_IDENTIFIER is not ON. Procedure not changed.', 1;
    IF @Definition IS NULL
        OR LEN(@Definition) <> 13737
        OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Definition)), 2)
           <> '75E03EDE68201556CEED37C06DEBF1C3730FDBE108907355EE36FDC16EAC140A'
        THROW 58525, N'ABORT: current definition is not the post-change module. Rollback refused and the procedure was not changed.', 1;

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
        THROW 58532, N'ABORT: embedded original body length is not 2577 / 5154. Procedure not changed.', 1;
    IF CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Previous)), 2)
        <> 'E1093DEE0BFD51FA30A10D481DE33E819905E55D63FBFA60B3CC5527406434C1'
        THROW 58533, N'ABORT: embedded original body hash is not the captured UAT module. Procedure not changed.', 1;

    DECLARE @CreateAt int = CHARINDEX(N'CREATE   PROCEDURE', @Previous);
    IF @CreateAt < 1
        THROW 58534, N'ABORT: CREATE token was not found in the captured body. Procedure not changed.', 1;

    DECLARE @Alter nvarchar(max) = STUFF(@Previous, @CreateAt, LEN(N'CREATE   PROCEDURE'), N'ALTER   PROCEDURE');

    BEGIN TRAN;
    EXEC (@Alter);

    DECLARE @Stored nvarchar(max) = OBJECT_DEFINITION(@ObjectId);
    IF @Stored IS NULL
        OR @Stored <> @Previous
        OR LEN(@Stored) <> 2577
        OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Stored)), 2)
           <> 'E1093DEE0BFD51FA30A10D481DE33E819905E55D63FBFA60B3CC5527406434C1'
    BEGIN
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;
        PRINT N'FAIL | Stored dbo.usp_ReturnClaimToEmployee does not match the captured preimage LEN 2577 and SHA-256 E1093DEE0BFD51FA30A10D481DE33E819905E55D63FBFA60B3CC5527406434C1.';
        PRINT N'The explicit transaction was rolled back before COMMIT. That rollback withdraws only this uncommitted procedure change.';
        PRINT N'A procedure definition change that has already committed cannot be undone by rolling back a later transaction.';
        PRINT N'If the live hash is neither the post-change hash nor the preimage hash, capture the definition and stop. Do not run either script again.';
        PRINT N'This script does not reverse claim transitions that already committed.';
        THROW 58535, N'ABORT: post-rollback verification failed. See the FAIL messages above.', 1;
    END;

    COMMIT TRAN;
    PRINT N'COMMITTED | dbo.usp_ReturnClaimToEmployee matches the captured preimage LEN 2577 and SHA-256 E1093DEE0BFD51FA30A10D481DE33E819905E55D63FBFA60B3CC5527406434C1.';
    PRINT N'This commit is final for the procedure text. Rolling back a later transaction does not undo it.';
    PRINT N'Future calls use the captured 2026-06-15 body. Claim, detail, activity, expense, approver, and history rows already committed are unchanged.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;
    THROW;
END CATCH;