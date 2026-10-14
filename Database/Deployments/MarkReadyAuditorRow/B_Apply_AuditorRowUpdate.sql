/*
    OBSOLETE for the module deployed on AnmolDristiDev as of 2026-10-14.
    That catalog body is LEN 11181 / SHA2_256
    E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409.
    This script still pins the older LEN 2267 preimage and LEN 2882 expected
    text. Do not execute it. Production promotion is Production_B_Apply.sql,
    which is also closed (@Execute = 0) and was not run on 2026-10-14.

    Package:     Database/Deployments/MarkReadyAuditorRow
    Script:      B_Apply_AuditorRowUpdate.sql
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN (AnmolDristi)
    Mode:        PREPARED — do not execute. Deployment is not authorized.

    Object:      dbo.usp_MarkRequestReadyForPayment
    Object id:   1860917701

    Change, and only this change:
      1. Add @Remarks NVARCHAR(MAX) = NULL after @ActionBy.
      2. Declare @NowUtc DATETIME2(0) once, as the repository procedure does.
      3. Keep the Auditor-login / Auditor-role guard and the Completed-header guard.
      4. Before the header update, approve the matching Pending Auditor row,
         set ActionBy and ActionDate, and keep existing Remarks when @Remarks is null.
      5. Leave the header update and the named history INSERT as they are deployed.

    No amounts, detail versions, expense rows, or transaction wrapper are introduced.
    The script does not execute the procedure and does not update claim rows.

    Fingerprints, SHA2_256 over the nvarchar module as UTF-16LE
    (HASHBYTES('SHA2_256', CONVERT(varbinary(max), definition))):

      Required before ALTER. Pinned from AnmolDristiDev on 2026-10-12.
      The text is usp_MarkRequestReadyForPayment_AnmolDristiDev_preimage.txt.
      Do not execute that file.
        LEN 2267 / DATALENGTH 4534
        97ACDA7C1DDD052C8AB53451316B1F5B11977C4EE6FB2BE73106578B6347A901
        modify_date 2026-10-08 08:50:40.710

      Required stored text before COMMIT. Reviewed CREATE form:
      usp_MarkRequestReadyForPayment_expected_stored.txt. Do not execute that file.
        LEN 2882 / DATALENGTH 5764
        8B2ECBCE8AC258D67A2C3CFCC6C45CC385F01354B8F5D8B5594409889685E158

    This script builds the next text from the live module with four replacements,
    refuses to ALTER unless that text hashes to the reviewed value, submits
    ALTER for the same characters, and commits only when OBJECT_DEFINITION
    equals that CREATE text. A different stored result rolls back.
    On 2026-10-11 the RC-014B apply observed OBJECT_DEFINITION storing CREATE
    where the submitted batch had ALTER, with the rest of the text unchanged.
    This script requires that same result. It does not assume it.

    Execution: DISABLED. @Execute defaults to 0 and returns before any
    catalog read or change.

    After a later rollback, modify_date will no longer be 2026-10-08 08:50:40.710.
    This script will then refuse the restored text until a new review pins the
    new modify_date. Do not relax that pin in order to re-apply.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Execute bit = 0;

IF @Execute <> 1
BEGIN
    PRINT N'NOT RUN | @Execute is 0. dbo.usp_MarkRequestReadyForPayment was not altered.';
    RETURN;
END;

BEGIN TRY

    IF @@TRANCOUNT <> 0
        THROW 58303, N'ABORT: this script must start with no open transaction. Procedure not changed.', 1;

    IF DB_NAME() = N'AnmolDristi'
        THROW 58301, N'ABORT: Live database AnmolDristi is forbidden. Procedure not changed.', 1;

    IF UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
    BEGIN
        DECLARE @ServerMsg nvarchar(400) =
            N'ABORT: server is ' + ISNULL(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName')), N'<null>')
            + N'. Expected MUMANMOAPP01\SQLEXPRESS. Procedure not changed.';
        THROW 58304, @ServerMsg, 1;
    END;

    IF DB_NAME() <> N'AnmolDristiDev'
    BEGIN
        DECLARE @DbMsg nvarchar(400) =
            N'ABORT: connected to ' + ISNULL(QUOTENAME(DB_NAME()), N'<null>')
            + N'. Expected [AnmolDristiDev]. Procedure not changed.';
        THROW 58302, @DbMsg, 1;
    END;

    DECLARE @ObjectId int = OBJECT_ID(N'dbo.usp_MarkRequestReadyForPayment', N'P');

    IF @ObjectId IS NULL OR @ObjectId <> 1860917701
    BEGIN
        DECLARE @IdMsg nvarchar(400) =
            N'ABORT: object id is ' + ISNULL(CONVERT(nvarchar(20), @ObjectId), N'<null>')
            + N'. Expected 1860917701. Procedure not changed.';
        THROW 58305, @IdMsg, 1;
    END;

    DECLARE @Definition nvarchar(max);
    DECLARE @ModifyDate datetime;
    DECLARE @UsesAnsiNulls bit;
    DECLARE @UsesQuotedIdentifier bit;

    SELECT
        @Definition = m.definition,
        @ModifyDate = o.modify_date,
        @UsesAnsiNulls = m.uses_ansi_nulls,
        @UsesQuotedIdentifier = m.uses_quoted_identifier
    FROM sys.sql_modules AS m
    INNER JOIN sys.objects AS o
        ON o.object_id = m.object_id
    WHERE m.object_id = @ObjectId
      AND o.type = N'P';

    IF @Definition IS NULL OR @ModifyDate IS NULL
        THROW 58305, N'ABORT: procedure definition or modify_date could not be read. Procedure not changed.', 1;

    IF ISNULL(@UsesAnsiNulls, 0) <> 1 OR ISNULL(@UsesQuotedIdentifier, 0) <> 1
        THROW 58306, N'ABORT: module ANSI_NULLS or QUOTED_IDENTIFIER is not ON. Procedure not changed.', 1;

    IF CONVERT(varchar(23), @ModifyDate, 121) <> '2026-10-08 08:50:40.710'
    BEGIN
        DECLARE @DateMsg nvarchar(400) =
            N'ABORT: modify_date is ' + CONVERT(varchar(23), @ModifyDate, 121)
            + N'. Expected 2026-10-08 08:50:40.710. Procedure not changed.';
        THROW 58307, @DateMsg, 1;
    END;

    IF LEN(@Definition) <> 2267 OR DATALENGTH(@Definition) <> 4534
    BEGIN
        DECLARE @LenMsg nvarchar(400) =
            N'ABORT: definition length is ' + CONVERT(nvarchar(20), LEN(@Definition))
            + N' / ' + CONVERT(nvarchar(20), DATALENGTH(@Definition))
            + N'. Expected 2267 / 4534. Procedure not changed.';
        THROW 58308, @LenMsg, 1;
    END;

    DECLARE @PreimageHash varchar(64) = '97ACDA7C1DDD052C8AB53451316B1F5B11977C4EE6FB2BE73106578B6347A901';
    DECLARE @ActualHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Definition)), 2);

    IF @ActualHash <> @PreimageHash
    BEGIN
        DECLARE @HashMsg nvarchar(500) =
            N'ABORT: definition SHA2_256 is ' + @ActualHash
            + N'. Expected the pinned pre-image. Procedure not changed.';
        THROW 58309, @HashMsg, 1;
    END;

    DECLARE @Nl nchar(2) = NCHAR(13) + NCHAR(10);
    DECLARE @Arrow nchar(1) = NCHAR(8594);
    DECLARE @K4 nvarchar(8) = NCHAR(52) + NCHAR(65039) + NCHAR(8419);

    DECLARE @OldParam nvarchar(200) =
        N'    @ActionBy NVARCHAR(100)' + @Nl + N')';
    DECLARE @NewParam nvarchar(300) =
        N'    @ActionBy NVARCHAR(100),' + @Nl
        + N'    @Remarks NVARCHAR(MAX) = NULL' + @Nl
        + N')';
    DECLARE @OldNocount nvarchar(80) =
        N'    SET NOCOUNT ON;' + @Nl + @Nl + N'    /' + N'*';
    DECLARE @NewNocount nvarchar(200) =
        N'    SET NOCOUNT ON;' + @Nl + @Nl
        + N'    DECLARE @NowUtc DATETIME2(0) = SYSUTCDATETIME();' + @Nl + @Nl
        + N'    /' + N'*';
    DECLARE @OldCompleted nvarchar(80) = N'Must be Completed */';
    DECLARE @NewCompleted nvarchar(250) =
        N'Must be Completed.' + @Nl
        + N'          EnsureHeaderCompletedForPayment writes Pending ' + @Arrow + N' Completed before this call. */';
    DECLARE @OldHeader nvarchar(40) = N'Update header */';
    DECLARE @NewHeader nvarchar(max) =
          N'Consume the Auditor assignment that HF231 left Pending. */' + @Nl
        + N'    UPDATE dbo.SalesApprovalRequestApprover' + @Nl
        + N'    SET' + @Nl
        + N'          Status     = ''Approved''' + @Nl
        + N'        , ActionBy   = @ActionBy' + @Nl
        + N'        , ActionDate = @NowUtc' + @Nl
        + N'        , Remarks    = ISNULL(@Remarks, Remarks)' + @Nl
        + N'    WHERE ApprovalRequestId = @ApprovalRequestId' + @Nl
        + N'      AND ApproverLoginId = @ActionBy' + @Nl
        + N'      AND ApproverRole = ''Auditor''' + @Nl
        + N'      AND Status = ''Pending'';' + @Nl
        + @Nl
        + N'    /* ' + @K4 + N' Update header */';

    DECLARE @Next nvarchar(max) = @Definition;

    IF (LEN(@Next) - LEN(REPLACE(@Next, @OldParam, N''))) / LEN(@OldParam) <> 1
        THROW 58310, N'ABORT: parameter anchor count is not 1. Procedure not changed.', 1;
    SET @Next = REPLACE(@Next, @OldParam, @NewParam);

    IF (LEN(@Next) - LEN(REPLACE(@Next, @OldNocount, N''))) / LEN(@OldNocount) <> 1
        THROW 58310, N'ABORT: NOCOUNT anchor count is not 1. Procedure not changed.', 1;
    SET @Next = REPLACE(@Next, @OldNocount, @NewNocount);

    IF (LEN(@Next) - LEN(REPLACE(@Next, @OldCompleted, N''))) / LEN(@OldCompleted) <> 1
        THROW 58310, N'ABORT: Completed-comment anchor count is not 1. Procedure not changed.', 1;
    SET @Next = REPLACE(@Next, @OldCompleted, @NewCompleted);

    IF (LEN(@Next) - LEN(REPLACE(@Next, @OldHeader, N''))) / LEN(@OldHeader) <> 1
        THROW 58310, N'ABORT: header-comment anchor count is not 1. Procedure not changed.', 1;
    SET @Next = REPLACE(@Next, @OldHeader, @NewHeader);

    IF LEN(@Next) <> 2882 OR DATALENGTH(@Next) <> 5764
        THROW 58311, N'ABORT: built definition length is not 2882 / 5764. Procedure not changed.', 1;

    DECLARE @ExpectedHash varchar(64) = '8B2ECBCE8AC258D67A2C3CFCC6C45CC385F01354B8F5D8B5594409889685E158';
    DECLARE @BuiltHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Next)), 2);

    IF @BuiltHash <> @ExpectedHash
    BEGIN
        DECLARE @BuiltMsg nvarchar(500) =
            N'ABORT: built SHA2_256 is ' + @BuiltHash
            + N'. Expected the reviewed module. Procedure not changed.';
        THROW 58311, @BuiltMsg, 1;
    END;

    IF CHARINDEX(N'@Remarks NVARCHAR(MAX) = NULL', @Next) = 0
        OR CHARINDEX(N'Consume the Auditor assignment that HF231 left Pending.', @Next) = 0
        OR CHARINDEX(N'Only Auditor can mark Ready for Payment.', @Next) = 0
        OR CHARINDEX(N'Request not completed.', @Next) = 0
        OR CHARINDEX(N'AND Status = ''Pending''', @Next) = 0
        OR CHARINDEX(N'AND ApproverRole = ''Auditor''', @Next) = 0
        OR CHARINDEX(N'BEGIN TRAN', @Next) > 0
        OR CHARINDEX(N'SalesDailyExpense', @Next) > 0
        OR CHARINDEX(N'SalesApprovalRequestDetail', @Next) > 0
        THROW 58314, N'ABORT: built text failed a guard check. Procedure not changed.', 1;

    DECLARE @CreateAt int = CHARINDEX(N'CREATE   PROCEDURE', @Next);
    IF @CreateAt <> 864
        THROW 58312, N'ABORT: CREATE token is not at character 864. Procedure not changed.', 1;

    DECLARE @Alter nvarchar(max) = STUFF(@Next, @CreateAt, LEN(N'CREATE   PROCEDURE'), N'ALTER   PROCEDURE');

    BEGIN TRAN;

    EXEC (@Alter);

    DECLARE @Stored nvarchar(max) = OBJECT_DEFINITION(@ObjectId);

    IF @Stored IS NULL OR @Stored <> @Next
    BEGIN
        ROLLBACK TRAN;
        THROW 58313, N'ABORT: stored definition does not equal the reviewed CREATE text. Rolled back.', 1;
    END;

    COMMIT TRAN;
    PRINT N'COMMITTED | dbo.usp_MarkRequestReadyForPayment now matches 8B2ECBCE8AC258D67A2C3CFCC6C45CC385F01354B8F5D8B5594409889685E158.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;
    THROW;
END CATCH;
