/*
    Package:     Database/Deployments/MarkReadyAuditorRow
    Target:      AnmolDristi on MUMANMOAPP01\SQLEXPRESS only.
    Refuses:     AnmolDristiDev and every other database name.
    Data:        No claim, approver, history, detail, or expense row is written.
    Requests:    100, 121, 130, and 131 are not referenced as update targets.

    Prepared 2026-10-14 from a read-back. @Execute defaults to 0 where a write
    exists. Generating this file did not execute it.

    Production preimage, read from AnmolDristi:
      object_id 1860917701
      modify_date 2026-10-08 12:46:41.807
      LEN 2223 / DATALENGTH 4446
      SHA2_256 6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451
      File: usp_MarkRequestReadyForPayment_AnmolDristi_preimage.txt
      Do not execute that file.

    UAT-verified stored form, read from AnmolDristiDev:
      modify_date 2026-10-13 14:11:49.980
      LEN 11181 / DATALENGTH 22362
      SHA2_256 E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409
      Repository source uses CREATE OR ALTER. SQL Server stores CREATE plus
      three spaces. That is the only repository-to-UAT text difference.
*/

/*
    Script:      Production_D_Rollback.sql
    Mode:        PREPARED. @Execute defaults to 0.
    Restores the captured AnmolDristi preimage only after the promoted hash is present.
    Does not restore or copy any UAT claim row.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Execute bit = 0;
IF @Execute <> 1
BEGIN
    PRINT N'NOT RUN | @Execute is 0. Production procedure was not rolled back.';
    RETURN;
END;

BEGIN TRY
    IF @@TRANCOUNT <> 0
        THROW 58430, N'ABORT: this script must start with no open transaction.', 1;
    IF DB_NAME() = N'AnmolDristiDev'
        THROW 58401, N'ABORT: AnmolDristiDev is not the Production target.', 1;
    IF DB_NAME() <> N'AnmolDristi'
        THROW 58402, N'ABORT: connected database is not AnmolDristi.', 1;
    IF UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
        THROW 58403, N'ABORT: server is not MUMANMOAPP01\SQLEXPRESS.', 1;

    DECLARE @ObjectId int = OBJECT_ID(N'dbo.usp_MarkRequestReadyForPayment', N'P');
    IF @ObjectId IS NULL OR @ObjectId <> 1860917701
        THROW 58404, N'ABORT: procedure object id is not 1860917701.', 1;

    DECLARE @Definition nvarchar(max);
    SELECT @Definition = m.definition
    FROM sys.sql_modules AS m
    WHERE m.object_id = @ObjectId;

    DECLARE @Hash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Definition)), 2);
    IF @Hash <> 'E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409'
        THROW 58431, N'ABORT: current definition is not the promoted module. Rollback refused.', 1;

    DECLARE @Previous nvarchar(max) = N'/* ============================================================================
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
          AND ApproverRole = ''Auditor''
    )
    BEGIN
        RAISERROR(''Only Auditor can mark Ready for Payment.'',16,1);
        RETURN;
    END

    /* 2️⃣ Must be Completed */
    IF NOT EXISTS (
        SELECT 1
        FROM SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND Status = ''Completed''
    )
    BEGIN
        RAISERROR(''Request not completed.'',16,1);
        RETURN;
    END

    /* 3️⃣ Update header */
    UPDATE SalesApprovalRequest
    SET
        Status = ''ReadyForPayment'',
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
        , ''Completed''
        , ''ReadyForPayment''
        , @ActionBy
        , ''Auditor marked Ready for Payment''
    );
END
';
    IF LEN(@Previous) <> 2223
        OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Previous)), 2)
           <> '6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451'
        THROW 58432, N'ABORT: embedded preimage does not match the captured Production text.', 1;

    DECLARE @CreateAt int = CHARINDEX(N'CREATE   PROCEDURE', @Previous);
    IF @CreateAt < 1
        THROW 58433, N'ABORT: CREATE token was not found in the preimage.', 1;
    DECLARE @Alter nvarchar(max) = STUFF(@Previous, @CreateAt, LEN(N'CREATE   PROCEDURE'), N'ALTER   PROCEDURE');

    BEGIN TRAN;
    EXEC (@Alter);
    DECLARE @Stored nvarchar(max) = OBJECT_DEFINITION(@ObjectId);
    IF @Stored IS NULL OR @Stored <> @Previous
    BEGIN
        ROLLBACK TRAN;
        THROW 58434, N'ABORT: stored definition does not equal the preimage. Rolled back.', 1;
    END;
    COMMIT TRAN;
    PRINT N'ROLLED BACK | Production procedure matches 6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;
    THROW;
END CATCH;