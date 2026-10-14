/*
    Package:     Database/Deployments/MarkReadyAuditorRow
    Script:      D_Rollback_AuditorRowUpdate.sql
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN (AnmolDristi)
    Mode:        PREPARED — do not execute until a separate rollback authorization.

    Object:      dbo.usp_MarkRequestReadyForPayment
    Object id:   1860917701

    Reverses only the four edits in B_Apply_AuditorRowUpdate.sql.
    Does not DROP the procedure. DROP plus CREATE is not authorized.

    Continues only when the live module hashes to the reviewed applied text
    8B2ECBCE8AC258D67A2C3CFCC6C45CC385F01354B8F5D8B5594409889685E158.
    The untouched pre-image is not a rollback target: this script stops
    without ALTER when that hash is still
    97ACDA7C1DDD052C8AB53451316B1F5B11977C4EE6FB2BE73106578B6347A901.

    Before COMMIT the stored text must equal the pre-image
    (LEN 2267 / DATALENGTH 4534 / that same 97ACDA7C... hash).
    modify_date will change. B_Apply will then refuse, because it also
    requires modify_date 2026-10-08 08:50:40.710.

    Execution: DISABLED. @Execute defaults to 0 and returns before any
    catalog read or change.

    This script does not execute the procedure and does not update claim rows.
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
        THROW 58305, N'ABORT: object id is not 1860917701. Procedure not changed.', 1;

    DECLARE @Definition nvarchar(max);
    DECLARE @UsesAnsiNulls bit;
    DECLARE @UsesQuotedIdentifier bit;

    SELECT
        @Definition = m.definition,
        @UsesAnsiNulls = m.uses_ansi_nulls,
        @UsesQuotedIdentifier = m.uses_quoted_identifier
    FROM sys.sql_modules AS m
    WHERE m.object_id = @ObjectId;

    IF @Definition IS NULL
        THROW 58305, N'ABORT: procedure definition could not be read. Procedure not changed.', 1;

    IF ISNULL(@UsesAnsiNulls, 0) <> 1 OR ISNULL(@UsesQuotedIdentifier, 0) <> 1
        THROW 58306, N'ABORT: module ANSI_NULLS or QUOTED_IDENTIFIER is not ON. Procedure not changed.', 1;

    DECLARE @LiveHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Definition)), 2);
    DECLARE @PreimageHash varchar(64) = '97ACDA7C1DDD052C8AB53451316B1F5B11977C4EE6FB2BE73106578B6347A901';
    DECLARE @AppliedHash varchar(64) = '8B2ECBCE8AC258D67A2C3CFCC6C45CC385F01354B8F5D8B5594409889685E158';

    IF @LiveHash = @PreimageHash
        THROW 58321, N'ABORT: module is still the pre-image. Rollback is not required. Procedure not changed.', 1;

    IF @LiveHash <> @AppliedHash
    BEGIN
        DECLARE @HashMsg nvarchar(500) =
            N'ABORT: definition SHA2_256 is ' + @LiveHash
            + N'. Expected the reviewed applied module. Procedure not changed.';
        THROW 58322, @HashMsg, 1;
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

    DECLARE @Back nvarchar(max) = @Definition;

    IF (LEN(@Back) - LEN(REPLACE(@Back, @NewHeader, N''))) / LEN(@NewHeader) <> 1
        THROW 58310, N'ABORT: applied header block count is not 1. Procedure not changed.', 1;
    SET @Back = REPLACE(@Back, @NewHeader, @OldHeader);

    IF (LEN(@Back) - LEN(REPLACE(@Back, @NewCompleted, N''))) / LEN(@NewCompleted) <> 1
        THROW 58310, N'ABORT: applied Completed-comment count is not 1. Procedure not changed.', 1;
    SET @Back = REPLACE(@Back, @NewCompleted, @OldCompleted);

    IF (LEN(@Back) - LEN(REPLACE(@Back, @NewNocount, N''))) / LEN(@NewNocount) <> 1
        THROW 58310, N'ABORT: applied NOCOUNT block count is not 1. Procedure not changed.', 1;
    SET @Back = REPLACE(@Back, @NewNocount, @OldNocount);

    IF (LEN(@Back) - LEN(REPLACE(@Back, @NewParam, N''))) / LEN(@NewParam) <> 1
        THROW 58310, N'ABORT: applied parameter block count is not 1. Procedure not changed.', 1;
    SET @Back = REPLACE(@Back, @NewParam, @OldParam);

    IF LEN(@Back) <> 2267 OR DATALENGTH(@Back) <> 4534
        THROW 58311, N'ABORT: restored text length is not 2267 / 4534. Procedure not changed.', 1;

    DECLARE @BackHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Back)), 2);
    IF @BackHash <> @PreimageHash
        THROW 58311, N'ABORT: restored text is not the pinned pre-image. Procedure not changed.', 1;

    DECLARE @CreateAt int = CHARINDEX(N'CREATE   PROCEDURE', @Back);
    IF @CreateAt <> 864
        THROW 58312, N'ABORT: CREATE token is not at character 864. Procedure not changed.', 1;

    DECLARE @Alter nvarchar(max) = STUFF(@Back, @CreateAt, LEN(N'CREATE   PROCEDURE'), N'ALTER   PROCEDURE');

    BEGIN TRAN;

    EXEC (@Alter);

    DECLARE @Stored nvarchar(max) = OBJECT_DEFINITION(@ObjectId);
    IF @Stored IS NULL OR @Stored <> @Back
    BEGIN
        ROLLBACK TRAN;
        THROW 58313, N'ABORT: stored definition does not equal the pre-image. Rolled back.', 1;
    END;

    COMMIT TRAN;
    PRINT N'COMMITTED | dbo.usp_MarkRequestReadyForPayment restored to 97ACDA7C1DDD052C8AB53451316B1F5B11977C4EE6FB2BE73106578B6347A901.';
    PRINT N'modify_date has changed. B_Apply_AuditorRowUpdate.sql will refuse until that pin is reviewed again.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;
    THROW;
END CATCH;
