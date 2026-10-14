/*
    Package:     Database/Deployments/RC014B
    Script:      B_Apply_ExpenseAggSnapshot.sql
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN (AnmolDristi)

    Object:      dbo.usp_SubmitSalesDailyActivities
    Object id:   877962204
    Change:      Two edits inside the #ExpenseAgg statement only.
                 1. ExpenseTotal = DA + Hotel + Travel + Stationary.
                 2. AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
                    beside e.IsDeleted = 0.
                 A null ClaimStatus stays in the aggregate.

    Source of the module text:
      sys.sql_modules.definition, and only after it matches the pinned
      pre-image file
      Database/Deployments/RC014B/usp_SubmitSalesDailyActivities_live_preimage.sql.
      This script does not read the working-tree procedure.

    Pinned pre-image, verified 2026-10-12 from that file and from the
    2026-10-11 read of AnmolDristiDev:
      LEN              24457
      DATALENGTH       48914
      SHA2_256         339C66BFBD16C10CE4906A687D14E62B68D96B80BE85008E7FB139D2FAFF9CCC
      modify_date      2026-10-07 19:05:41.930
      PROCESSED        absent
      TotalExpenseEntries absent

    Submitted statement, @Candidate: ALTER plus the two edits.
      LEN              24827
      DATALENGTH       49654
      SHA2_256         54FE0ECD9C837C34754A4DBD199CE005117CFC558522778479D75764343114E4

    Expected stored definition, @ExpectedStored: the pinned pre-image
    with the two edits and its original CREATE   PROCEDURE token.
      LEN              24828
      DATALENGTH       49656
      SHA2_256         5BEDA23C85279789D325C66B573C37BB1E037DA5C0153695A435DEA9E7D37547

    Why the two differ: on 2026-10-11 20:29:40 UTC a diagnostic run of
    this script on MUMANMOAPP01\SQLEXPRESS / AnmolDristiDev submitted
    @Candidate. Before rollback, OBJECT_DEFINITION returned 49656 bytes
    with SHA2_256 5BEDA23C..., first difference at character 638, stored
    CREATE where @Candidate had ALTER. Rebuilding the pre-image with only
    the two edits gives the same 49656 bytes and the same SHA2_256. That
    run threw 58115 and rolled back; the baseline 339C66BF... was read
    afterwards. This script still submits @Candidate. It verifies the
    stored text against @ExpectedStored, derived from the same pinned
    definition, by text, by bytes, and by SHA2_256.

    Execution: DISABLED. @Execute defaults to 0 and returns before any
    catalog read is allowed to become a change. Deployment is NOT READY.
    ALTER PROCEDURE rollback was not executed on this instance during
    preparation. Do not set @Execute to 1 until that gate is authorized.

    Post-ALTER gate:
      The stored definition must equal @ExpectedStored by collation text,
      by DATALENGTH and binary value, and by SHA2_256. Object id, the
      rejected-expense predicate, the removed all-types expression,
      PROCESSED, TotalExpenseEntries, and the closed-predecessor rule are
      checked as before. Any failure prints the captured facts, rolls
      back, and throws 58115. A NULL definition or a NULL SHA-256 rolls
      back and throws 58117, so an unverified module cannot COMMIT.
      The difference class is measured: NoDefinition,
      NoCharacterDifference, OnlyTrailingWhitespace, OnlyHeaderText, or
      OtherContentDifference. OnlyHeaderText means the text from
      PROCEDURE dbo.usp_SubmitSalesDailyActivities to the end is
      byte-identical and the first difference is before it.

    This script does not execute dbo.usp_SubmitSalesDailyActivities.
    It does not insert, update, or delete claim rows.
    It does not alter any other object.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Execute bit = 0;

IF @Execute <> 1
BEGIN
    PRINT N'NOT RUN | @Execute is 0. dbo.usp_SubmitSalesDailyActivities was not altered.';
    RETURN;
END;

BEGIN TRY

    IF @@TRANCOUNT <> 0
        THROW 58103, N'ABORT: this script must start with no open transaction.', 1;

    IF UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
    BEGIN
        DECLARE @ServerMsg nvarchar(400) =
            N'ABORT: server is ' + ISNULL(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName')), N'<null>')
            + N'. Expected MUMANMOAPP01\SQLEXPRESS. Procedure not changed.';
        THROW 58101, @ServerMsg, 1;
    END;

    IF DB_NAME() = N'AnmolDristi'
        THROW 58101, N'ABORT: Live database AnmolDristi is forbidden.', 1;

    IF DB_NAME() <> N'AnmolDristiDev'
    BEGIN
        DECLARE @DbMsg nvarchar(400) =
            N'ABORT: connected to ' + ISNULL(QUOTENAME(DB_NAME()), N'<null>')
            + N'. Expected [AnmolDristiDev]. Procedure not changed.';
        THROW 58102, @DbMsg, 1;
    END;

    DECLARE @ObjectId int = OBJECT_ID(N'dbo.usp_SubmitSalesDailyActivities', N'P');

    IF @ObjectId IS NULL OR @ObjectId <> 877962204
    BEGIN
        DECLARE @IdMsg nvarchar(400) =
            N'ABORT: object id is ' + ISNULL(CONVERT(nvarchar(20), @ObjectId), N'<null>')
            + N'. Expected 877962204. Procedure not changed.';
        THROW 58104, @IdMsg, 1;
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
        THROW 58104, N'ABORT: procedure definition or modify_date could not be read. Procedure not changed.', 1;

    IF ISNULL(@UsesAnsiNulls, 0) <> 1 OR ISNULL(@UsesQuotedIdentifier, 0) <> 1
        THROW 58116, N'ABORT: module ANSI_NULLS or QUOTED_IDENTIFIER is not ON. Refusing ALTER so those settings are not changed. Procedure not changed.', 1;

    IF CONVERT(varchar(23), @ModifyDate, 121) <> '2026-10-07 19:05:41.930'
    BEGIN
        DECLARE @DateMsg nvarchar(400) =
            N'ABORT: modify_date is ' + CONVERT(varchar(23), @ModifyDate, 121)
            + N'. Expected 2026-10-07 19:05:41.930. Procedure not changed.';
        THROW 58105, @DateMsg, 1;
    END;

    IF LEN(@Definition) <> 24457 OR DATALENGTH(@Definition) <> 48914
    BEGIN
        DECLARE @LenMsg nvarchar(400) =
            N'ABORT: definition length is ' + CONVERT(nvarchar(20), LEN(@Definition))
            + N' characters / ' + CONVERT(nvarchar(20), DATALENGTH(@Definition))
            + N' bytes. Expected 24457 / 48914. Procedure not changed.';
        THROW 58106, @LenMsg, 1;
    END;

    DECLARE @ExpectedHash varchar(64) = '339C66BFBD16C10CE4906A687D14E62B68D96B80BE85008E7FB139D2FAFF9CCC';
    DECLARE @ActualHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Definition)), 2);

    IF @ActualHash <> @ExpectedHash
    BEGIN
        DECLARE @HashMsg nvarchar(500) =
            N'ABORT: definition SHA2_256 is ' + @ActualHash
            + N'. Expected the pinned pre-image. Procedure not changed.';
        THROW 58107, @HashMsg, 1;
    END;

    IF CHARINDEX(N'PROCESSED', @Definition) > 0
        THROW 58108, N'ABORT: definition contains PROCESSED. Refusing to mix in working-tree lifecycle text. Procedure not changed.', 1;

    IF CHARINDEX(N'TotalExpenseEntries', @Definition) > 0
        THROW 58109, N'ABORT: TotalExpenseEntries is present. Refusing to mix in the counter change. Procedure not changed.', 1;

    IF CHARINDEX(N'SUM(ISNULL(e.Amount,0))', @Definition) = 0
        THROW 58110, N'ABORT: all-types ExpenseTotal expression was not found. Procedure not changed.', 1;

    IF CHARINDEX(N'ISNULL(e.ClaimStatus, '''') <> ''Rejected''', @Definition) > 0
        THROW 58110, N'ABORT: rejected-expense predicate is already present. Procedure not changed.', 1;

    IF CHARINDEX(N'A Rejected (or missing) id is not an active lifecycle.', @Definition) = 0
        THROW 58111, N'ABORT: deployed closed-predecessor rule was not found. Procedure not changed.', 1;

    IF CHARINDEX(N'N''RETURNED'', N''PENDING'', N''COMPLETED'', N''READYFORPAYMENT''', @Definition) = 0
        THROW 58111, N'ABORT: deployed active-status list was not found. Procedure not changed.', 1;

    DECLARE @DetailVersionToken nvarchar(40) = N'DetailVersion DESC';
    DECLARE @DetailIdToken nvarchar(40) = N'ApprovalRequestDetailId DESC';
    DECLARE @ClaimAmountToken nvarchar(40) = N'a.CalculatedClaimAmount';

    IF (LEN(@Definition) - LEN(REPLACE(@Definition, @DetailVersionToken, N''))) / LEN(@DetailVersionToken) <> 3
        OR (LEN(@Definition) - LEN(REPLACE(@Definition, @DetailIdToken, N''))) / LEN(@DetailIdToken) <> 3
        OR (LEN(@Definition) - LEN(REPLACE(@Definition, @ClaimAmountToken, N''))) / LEN(@ClaimAmountToken) <> 2
        THROW 58111, N'ABORT: detail ordering or calculated-claim assignment does not match the pinned pre-image. Procedure not changed.', 1;

    IF (LEN(@Definition) - LEN(REPLACE(@Definition, N'CREATE   PROCEDURE', N''))) / LEN(N'CREATE   PROCEDURE') <> 1
        THROW 58112, N'ABORT: CREATE PROCEDURE token was not found exactly once. Procedure not changed.', 1;

    DECLARE @CrLf nchar(2) = NCHAR(13) + NCHAR(10);
    DECLARE @S4 nvarchar(4) = N'    ';
    DECLARE @S6 nvarchar(6) = N'      ';
    DECLARE @S8 nvarchar(8) = N'        ';

    DECLARE @OldAnchor nvarchar(max) =
        @S8 + N'SUM(ISNULL(e.Amount,0))' + @CrLf
        + @CrLf
        + @S4 + N'FROM dbo.SalesDailyExpense e' + @CrLf
        + @CrLf
        + @S4 + N'INNER JOIN @Updates u' + @CrLf
        + @S8 + N'ON e.ActivityId = u.ActivityId' + @CrLf
        + @CrLf
        + @S4 + N'WHERE' + @CrLf
        + @S8 + N'e.IsDeleted = 0' + @CrLf
        + @CrLf
        + @S4 + N'GROUP BY' + @CrLf
        + @S8 + N'e.ActivityId;';

    DECLARE @NewAnchor nvarchar(max) =
        @S8 + N'SUM(CASE WHEN e.ExpenseType = ''DA'' THEN ISNULL(e.Amount, 0) ELSE 0 END)' + @CrLf
        + @S6 + N'+ SUM(CASE WHEN e.ExpenseType = ''Hotel'' THEN ISNULL(e.Amount, 0) ELSE 0 END)' + @CrLf
        + @S6 + N'+ SUM(CASE WHEN e.ExpenseType = ''Travel'' THEN ISNULL(e.Amount, 0) ELSE 0 END)' + @CrLf
        + @S6 + N'+ SUM(CASE WHEN e.ExpenseType = ''Stationary'' THEN ISNULL(e.Amount, 0) ELSE 0 END) AS ExpenseTotal' + @CrLf
        + @CrLf
        + @S4 + N'FROM dbo.SalesDailyExpense e' + @CrLf
        + @CrLf
        + @S4 + N'INNER JOIN @Updates u' + @CrLf
        + @S8 + N'ON e.ActivityId = u.ActivityId' + @CrLf
        + @CrLf
        + @S4 + N'WHERE' + @CrLf
        + @S8 + N'e.IsDeleted = 0' + @CrLf
        + @S4 + N'AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''' + @CrLf
        + @CrLf
        + @S4 + N'GROUP BY' + @CrLf
        + @S8 + N'e.ActivityId;';

    IF (LEN(@Definition) - LEN(REPLACE(@Definition, @OldAnchor, N''))) / LEN(@OldAnchor) <> 1
        THROW 58113, N'ABORT: #ExpenseAgg anchor was not found exactly once. Procedure not changed.', 1;

    DECLARE @Candidate nvarchar(max) = REPLACE(@Definition, N'CREATE   PROCEDURE', N'ALTER   PROCEDURE');
    SET @Candidate = REPLACE(@Candidate, @OldAnchor, @NewAnchor);

    DECLARE @RoundTrip nvarchar(max) = REPLACE(@Candidate, @NewAnchor, @OldAnchor);
    SET @RoundTrip = REPLACE(@RoundTrip, N'ALTER   PROCEDURE', N'CREATE   PROCEDURE');

    IF @Candidate IS NULL
       OR @RoundTrip IS NULL
       OR @RoundTrip <> @Definition
       OR DATALENGTH(@RoundTrip) <> DATALENGTH(@Definition)
       OR CONVERT(varbinary(max), @RoundTrip) <> CONVERT(varbinary(max), @Definition)
        THROW 58114, N'ABORT: candidate does not round-trip to the pinned pre-image. Procedure not changed.', 1;

    IF LEN(@Candidate) <> 24827 OR DATALENGTH(@Candidate) <> 49654
        THROW 58114, N'ABORT: candidate length is not 24827 characters / 49654 bytes. Procedure not changed.', 1;

    DECLARE @ExpectedCandidateHash varchar(64) = '54FE0ECD9C837C34754A4DBD199CE005117CFC558522778479D75764343114E4';
    DECLARE @CandidateHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Candidate)), 2);

    IF @CandidateHash IS NULL OR @CandidateHash <> @ExpectedCandidateHash
        THROW 58114, N'ABORT: candidate SHA2_256 is not the reviewed module. Procedure not changed.', 1;

    IF (LEN(@Candidate) - LEN(REPLACE(@Candidate, N'ALTER   PROCEDURE', N''))) / LEN(N'ALTER   PROCEDURE') <> 1
       OR CHARINDEX(N'CREATE   PROCEDURE', @Candidate) > 0
        THROW 58114, N'ABORT: candidate must contain ALTER PROCEDURE once and no CREATE PROCEDURE token. Procedure not changed.', 1;

    -- SQL Server stores this ALTER with the CREATE keyword (diagnostic
    -- run 2026-10-11 20:29:40 UTC). The expected stored text is derived
    -- from the pinned definition, not from the catalog after the ALTER.
    DECLARE @ExpectedStored nvarchar(max) = REPLACE(@Definition, @OldAnchor, @NewAnchor);
    DECLARE @ExpectedStoredHash varchar(64) = '5BEDA23C85279789D325C66B573C37BB1E037DA5C0153695A435DEA9E7D37547';
    DECLARE @ExpectedStoredActualHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @ExpectedStored)), 2);
    DECLARE @StoredAsSubmitted nvarchar(max) = REPLACE(@ExpectedStored, N'CREATE   PROCEDURE', N'ALTER   PROCEDURE');

    IF @ExpectedStored IS NULL
       OR @ExpectedStoredActualHash IS NULL
       OR @StoredAsSubmitted IS NULL
       OR LEN(@ExpectedStored) <> 24828
       OR DATALENGTH(@ExpectedStored) <> 49656
       OR @ExpectedStoredActualHash <> @ExpectedStoredHash
       OR (LEN(@ExpectedStored) - LEN(REPLACE(@ExpectedStored, N'CREATE   PROCEDURE', N''))) / LEN(N'CREATE   PROCEDURE') <> 1
       OR CHARINDEX(N'ALTER   PROCEDURE', @ExpectedStored) > 0
       OR DATALENGTH(@StoredAsSubmitted) <> DATALENGTH(@Candidate)
       OR CONVERT(varbinary(max), @StoredAsSubmitted) <> CONVERT(varbinary(max), @Candidate)
        THROW 58118, N'ABORT: expected stored definition is not the reviewed CREATE-form candidate. Procedure not changed.', 1;

    IF (LEN(@Candidate) - LEN(REPLACE(@Candidate, N'ISNULL(e.ClaimStatus, '''') <> ''Rejected''', N'')))
        / LEN(N'ISNULL(e.ClaimStatus, '''') <> ''Rejected''') <> 1
        THROW 58114, N'ABORT: candidate does not contain the rejected-expense predicate exactly once. Procedure not changed.', 1;

    IF CHARINDEX(N'SUM(ISNULL(e.Amount,0))', @Candidate) > 0
        OR CHARINDEX(N'PROCESSED', @Candidate) > 0
        OR CHARINDEX(N'TotalExpenseEntries', @Candidate) > 0
        OR CHARINDEX(N'A Rejected (or missing) id is not an active lifecycle.', @Candidate) = 0
        OR (LEN(@Candidate) - LEN(REPLACE(@Candidate, @DetailVersionToken, N''))) / LEN(@DetailVersionToken) <> 3
        OR (LEN(@Candidate) - LEN(REPLACE(@Candidate, @DetailIdToken, N''))) / LEN(@DetailIdToken) <> 3
        OR (LEN(@Candidate) - LEN(REPLACE(@Candidate, @ClaimAmountToken, N''))) / LEN(@ClaimAmountToken) <> 2
        OR (LEN(@Candidate) - LEN(REPLACE(@Candidate, N'ExpenseType = ''DA'' THEN ISNULL(e.Amount, 0)', N''))) / LEN(N'ExpenseType = ''DA'' THEN ISNULL(e.Amount, 0)') <> 1
        OR (LEN(@Candidate) - LEN(REPLACE(@Candidate, N'ExpenseType = ''Hotel'' THEN ISNULL(e.Amount, 0)', N''))) / LEN(N'ExpenseType = ''Hotel'' THEN ISNULL(e.Amount, 0)') <> 1
        OR (LEN(@Candidate) - LEN(REPLACE(@Candidate, N'ExpenseType = ''Travel'' THEN ISNULL(e.Amount, 0)', N''))) / LEN(N'ExpenseType = ''Travel'' THEN ISNULL(e.Amount, 0)') <> 1
        OR (LEN(@Candidate) - LEN(REPLACE(@Candidate, N'ExpenseType = ''Stationary'' THEN ISNULL(e.Amount, 0)', N''))) / LEN(N'ExpenseType = ''Stationary'' THEN ISNULL(e.Amount, 0)') <> 1
        THROW 58114, N'ABORT: candidate failed the two-change review. Procedure not changed.', 1;

    BEGIN TRANSACTION;

    -- @Candidate is the pinned module with CREATE changed to ALTER and the
    -- two #ExpenseAgg edits applied. This batch does not call the procedure.
    EXEC (@Candidate);

    DECLARE @Applied nvarchar(max) = OBJECT_DEFINITION(@ObjectId);
    DECLARE @AppliedHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Applied)), 2);
    DECLARE @PostObjectId int = OBJECT_ID(N'dbo.usp_SubmitSalesDailyActivities', N'P');

    -- A NULL definition or a NULL SHA-256 must not fall through to COMMIT.
    -- <> ignores trailing spaces and follows the database collation, so
    -- the stored text is also compared by DATALENGTH and binary value.
    IF @Applied IS NULL
       OR @AppliedHash IS NULL
       OR @Applied <> @ExpectedStored
       OR DATALENGTH(@Applied) <> DATALENGTH(@ExpectedStored)
       OR CONVERT(varbinary(max), @Applied) <> CONVERT(varbinary(max), @ExpectedStored)
       OR @AppliedHash <> @ExpectedStoredHash
       OR @PostObjectId <> 877962204
       OR CHARINDEX(N'ISNULL(e.ClaimStatus, '''') <> ''Rejected''', @Applied) = 0
       OR CHARINDEX(N'SUM(ISNULL(e.Amount,0))', @Applied) > 0
       OR CHARINDEX(N'PROCESSED', @Applied) > 0
       OR CHARINDEX(N'TotalExpenseEntries', @Applied) > 0
       OR CHARINDEX(N'A Rejected (or missing) id is not an active lifecycle.', @Applied) = 0
    BEGIN
        DECLARE @P1 nchar(1) =
            CASE WHEN @Applied <> @ExpectedStored THEN N'1' WHEN @Applied = @ExpectedStored THEN N'0' ELSE N'U' END;
        DECLARE @P2 nchar(1) =
            CASE WHEN @AppliedHash <> @ExpectedStoredHash THEN N'1' WHEN @AppliedHash = @ExpectedStoredHash THEN N'0' ELSE N'U' END;
        DECLARE @P9 nchar(1) =
            CASE
                WHEN DATALENGTH(@Applied) <> DATALENGTH(@ExpectedStored)
                  OR CONVERT(varbinary(max), @Applied) <> CONVERT(varbinary(max), @ExpectedStored) THEN N'1'
                WHEN DATALENGTH(@Applied) = DATALENGTH(@ExpectedStored)
                 AND CONVERT(varbinary(max), @Applied) = CONVERT(varbinary(max), @ExpectedStored) THEN N'0'
                ELSE N'U'
            END;
        DECLARE @P3 nchar(1) =
            CASE WHEN @PostObjectId <> 877962204 THEN N'1' WHEN @PostObjectId = 877962204 THEN N'0' ELSE N'U' END;
        DECLARE @P4 nchar(1) =
            CASE
                WHEN CHARINDEX(N'ISNULL(e.ClaimStatus, '''') <> ''Rejected''', @Applied) = 0 THEN N'1'
                WHEN CHARINDEX(N'ISNULL(e.ClaimStatus, '''') <> ''Rejected''', @Applied) <> 0 THEN N'0'
                ELSE N'U'
            END;
        DECLARE @P5 nchar(1) =
            CASE
                WHEN CHARINDEX(N'SUM(ISNULL(e.Amount,0))', @Applied) > 0 THEN N'1'
                WHEN CHARINDEX(N'SUM(ISNULL(e.Amount,0))', @Applied) <= 0 THEN N'0'
                ELSE N'U'
            END;
        DECLARE @P6 nchar(1) =
            CASE
                WHEN CHARINDEX(N'PROCESSED', @Applied) > 0 THEN N'1'
                WHEN CHARINDEX(N'PROCESSED', @Applied) <= 0 THEN N'0'
                ELSE N'U'
            END;
        DECLARE @P7 nchar(1) =
            CASE
                WHEN CHARINDEX(N'TotalExpenseEntries', @Applied) > 0 THEN N'1'
                WHEN CHARINDEX(N'TotalExpenseEntries', @Applied) <= 0 THEN N'0'
                ELSE N'U'
            END;
        DECLARE @P8 nchar(1) =
            CASE
                WHEN CHARINDEX(N'A Rejected (or missing) id is not an active lifecycle.', @Applied) = 0 THEN N'1'
                WHEN CHARINDEX(N'A Rejected (or missing) id is not an active lifecycle.', @Applied) <> 0 THEN N'0'
                ELSE N'U'
            END;

        DECLARE @AppliedBin varbinary(max) = CONVERT(varbinary(max), @Applied);
        DECLARE @CandidateBin varbinary(max) = CONVERT(varbinary(max), @ExpectedStored);
        DECLARE @DiffByte int = NULL;
        DECLARE @BytePos int = 1;
        DECLARE @MaxBytes int = 0;
        DECLARE @LeftByte varbinary(1);
        DECLARE @RightByte varbinary(1);

        IF @AppliedBin IS NOT NULL AND @CandidateBin IS NOT NULL
        BEGIN
            SET @MaxBytes = CASE
                WHEN DATALENGTH(@AppliedBin) > DATALENGTH(@CandidateBin) THEN DATALENGTH(@AppliedBin)
                ELSE DATALENGTH(@CandidateBin)
            END;

            WHILE @BytePos <= @MaxBytes
            BEGIN
                SET @LeftByte = SUBSTRING(@AppliedBin, @BytePos, 1);
                SET @RightByte = SUBSTRING(@CandidateBin, @BytePos, 1);

                IF DATALENGTH(@LeftByte) <> DATALENGTH(@RightByte)
                   OR @LeftByte <> @RightByte
                BEGIN
                    SET @DiffByte = @BytePos;
                    BREAK;
                END;

                SET @BytePos += 1;
            END;
        END;

        DECLARE @DiffChar int =
            CASE WHEN @DiffByte IS NULL THEN NULL ELSE ((@DiffByte - 1) / 2) + 1 END;
        DECLARE @AppliedUnit int =
            CASE WHEN @DiffChar IS NULL OR @Applied IS NULL THEN NULL ELSE UNICODE(SUBSTRING(@Applied, @DiffChar, 1)) END;
        DECLARE @CandidateUnit int =
            CASE WHEN @DiffChar IS NULL OR @ExpectedStored IS NULL THEN NULL ELSE UNICODE(SUBSTRING(@ExpectedStored, @DiffChar, 1)) END;

        DECLARE @OnlyTrailingWhitespace bit = 0;
        DECLARE @OnlyHeaderText bit = 0;
        DECLARE @Scan int;
        DECLARE @ScanLimit int;
        DECLARE @UcA int;
        DECLARE @UcC int;
        DECLARE @WhitespaceTail bit;
        DECLARE @AppliedToken int;
        DECLARE @CandidateToken int;
        DECLARE @AppliedTail nvarchar(max);
        DECLARE @CandidateTail nvarchar(max);

        IF @Applied IS NOT NULL AND @ExpectedStored IS NOT NULL AND @DiffChar IS NOT NULL
        BEGIN
            SET @WhitespaceTail = 1;
            SET @Scan = @DiffChar;
            SET @ScanLimit = CASE
                WHEN DATALENGTH(@Applied) > DATALENGTH(@ExpectedStored) THEN DATALENGTH(@Applied)
                ELSE DATALENGTH(@ExpectedStored)
            END / 2;

            WHILE @Scan <= @ScanLimit
            BEGIN
                SET @UcA = UNICODE(SUBSTRING(@Applied, @Scan, 1));
                SET @UcC = UNICODE(SUBSTRING(@ExpectedStored, @Scan, 1));

                IF @UcA IS NULL AND @UcC IS NULL
                    BREAK;

                IF (@UcA IS NOT NULL AND @UcA NOT IN (9, 10, 13, 32))
                   OR (@UcC IS NOT NULL AND @UcC NOT IN (9, 10, 13, 32))
                BEGIN
                    SET @WhitespaceTail = 0;
                    BREAK;
                END;

                SET @Scan += 1;
            END;

            IF @WhitespaceTail = 1
                SET @OnlyTrailingWhitespace = 1;

            IF @OnlyTrailingWhitespace = 0
            BEGIN
                -- The anchor starts after the CREATE or ALTER keyword, so a
                -- keyword difference counts as header text.
                SET @AppliedToken = CHARINDEX(N'PROCEDURE dbo.usp_SubmitSalesDailyActivities', @Applied);
                SET @CandidateToken = CHARINDEX(N'PROCEDURE dbo.usp_SubmitSalesDailyActivities', @ExpectedStored);

                IF @AppliedToken >= 1
                   AND @CandidateToken >= 1
                   AND @DiffChar < @AppliedToken
                   AND @DiffChar < @CandidateToken
                BEGIN
                    SET @AppliedTail = SUBSTRING(@Applied, @AppliedToken, 2000000000);
                    SET @CandidateTail = SUBSTRING(@ExpectedStored, @CandidateToken, 2000000000);

                    IF DATALENGTH(@AppliedTail) = DATALENGTH(@CandidateTail)
                       AND CONVERT(varbinary(max), @AppliedTail) = CONVERT(varbinary(max), @CandidateTail)
                        SET @OnlyHeaderText = 1;
                END;
            END;
        END;

        DECLARE @DifferenceClass nvarchar(40) =
            CASE
                WHEN @Applied IS NULL THEN N'NoDefinition'
                WHEN @DiffChar IS NULL THEN N'NoCharacterDifference'
                WHEN @OnlyTrailingWhitespace = 1 THEN N'OnlyTrailingWhitespace'
                WHEN @OnlyHeaderText = 1 THEN N'OnlyHeaderText'
                ELSE N'OtherContentDifference'
            END;

        DECLARE @From int =
            CASE
                WHEN @DiffChar IS NULL THEN NULL
                WHEN @DiffChar > 24 THEN @DiffChar - 24
                ELSE 1
            END;
        DECLARE @AppliedNear nvarchar(300) =
            CASE WHEN @From IS NULL THEN NULL ELSE SUBSTRING(@Applied, @From, 48) END;
        DECLARE @CandidateNear nvarchar(300) =
            CASE WHEN @From IS NULL THEN NULL ELSE SUBSTRING(@ExpectedStored, @From, 48) END;

        SET @AppliedNear = REPLACE(REPLACE(REPLACE(REPLACE(@AppliedNear, NCHAR(13), N'<CR>'), NCHAR(10), N'<LF>'), NCHAR(9), N'<TAB>'), NCHAR(0), N'<NUL>');
        SET @CandidateNear = REPLACE(REPLACE(REPLACE(REPLACE(@CandidateNear, NCHAR(13), N'<CR>'), NCHAR(10), N'<LF>'), NCHAR(9), N'<TAB>'), NCHAR(0), N'<NUL>');

        PRINT N'RC014B DIAG | DefinitionNull='
            + CASE WHEN @Applied IS NULL THEN N'1' ELSE N'0' END
            + N' | HashNull=' + CASE WHEN @AppliedHash IS NULL THEN N'1' ELSE N'0' END
            + N' | ObjectId=' + ISNULL(CONVERT(nvarchar(20), @PostObjectId), N'NULL')
            + N' | ExpectedObjectId=877962204';
        PRINT N'RC014B DIAG | AppliedBytes=' + ISNULL(CONVERT(nvarchar(20), DATALENGTH(@Applied)), N'NULL')
            + N' | ExpectedStoredBytes=' + ISNULL(CONVERT(nvarchar(20), DATALENGTH(@ExpectedStored)), N'NULL')
            + N' | SubmittedBytes=' + ISNULL(CONVERT(nvarchar(20), DATALENGTH(@Candidate)), N'NULL');
        PRINT N'RC014B DIAG | AppliedHash=' + ISNULL(@AppliedHash, N'NULL')
            + N' | ExpectedStoredHash=' + @ExpectedStoredHash
            + N' | SubmittedHash=' + @ExpectedCandidateHash;
        PRINT N'RC014B DIAG | P1_TextDiff=' + @P1
            + N' | P2_HashDiff=' + @P2
            + N' | P3_ObjectId=' + @P3
            + N' | P4_RejectedMissing=' + @P4
            + N' | P5_AllTypesPresent=' + @P5
            + N' | P6_Processed=' + @P6
            + N' | P7_TotalExpenseEntries=' + @P7
            + N' | P8_LifecycleMissing=' + @P8
            + N' | P9_BinaryDiff=' + @P9;
        PRINT N'RC014B DIAG | DiffChar=' + ISNULL(CONVERT(nvarchar(20), @DiffChar), N'NULL')
            + N' | AppliedUnit=' + ISNULL(CONVERT(nvarchar(20), @AppliedUnit), N'NULL')
            + N' | ExpectedUnit=' + ISNULL(CONVERT(nvarchar(20), @CandidateUnit), N'NULL')
            + N' | DifferenceClass=' + @DifferenceClass;
        PRINT N'RC014B DIAG | AppliedNear=' + ISNULL(@AppliedNear, N'NULL');
        PRINT N'RC014B DIAG | ExpectedNear=' + ISNULL(@CandidateNear, N'NULL');

        DECLARE @GateMessage nvarchar(2048) =
            N'ABORT: altered module did not match the expected stored definition. Change was rolled back.'
            + N' Class=' + @DifferenceClass
            + N' P1=' + @P1 + N' P2=' + @P2 + N' P3=' + @P3 + N' P4=' + @P4
            + N' P5=' + @P5 + N' P6=' + @P6 + N' P7=' + @P7 + N' P8=' + @P8 + N' P9=' + @P9
            + N' AppliedBytes=' + ISNULL(CONVERT(nvarchar(20), DATALENGTH(@Applied)), N'NULL')
            + N' ExpectedStoredBytes=' + ISNULL(CONVERT(nvarchar(20), DATALENGTH(@ExpectedStored)), N'NULL')
            + N' AppliedHash=' + ISNULL(@AppliedHash, N'NULL')
            + N' ExpectedStoredHash=' + @ExpectedStoredHash
            + N' DiffChar=' + ISNULL(CONVERT(nvarchar(20), @DiffChar), N'NULL')
            + N' ObjectId=' + ISNULL(CONVERT(nvarchar(20), @PostObjectId), N'NULL');
        DECLARE @SafetyMessage nvarchar(2048) =
            N'ABORT: altered module could not be verified because the definition or its SHA-256 was NULL. Change was rolled back.'
            + N' Class=' + @DifferenceClass
            + N' P1=' + @P1 + N' P2=' + @P2 + N' P3=' + @P3 + N' P4=' + @P4
            + N' P5=' + @P5 + N' P6=' + @P6 + N' P7=' + @P7 + N' P8=' + @P8 + N' P9=' + @P9
            + N' ObjectId=' + ISNULL(CONVERT(nvarchar(20), @PostObjectId), N'NULL');

        IF @GateMessage IS NULL OR LEN(@GateMessage) > 2048
            SET @GateMessage = N'ABORT: altered module did not match the reviewed candidate. Change was rolled back.';
        IF @SafetyMessage IS NULL OR LEN(@SafetyMessage) > 2048
            SET @SafetyMessage = N'ABORT: altered module could not be verified because the definition or its SHA-256 was NULL. Change was rolled back.';

        ROLLBACK TRANSACTION;

        IF @Applied IS NULL OR @AppliedHash IS NULL
            THROW 58117, @SafetyMessage, 1;

        THROW 58115, @GateMessage, 1;
    END;

    COMMIT TRANSACTION;

    PRINT N'Applied the #ExpenseAgg predicate and four-category ExpenseTotal on AnmolDristiDev.';
    PRINT N'Stored definition SHA2_256 ' + @AppliedHash + N', the CREATE-form candidate.';
    PRINT N'The submission procedure was not executed. No claim row was modified.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

/*
    Failure recovery, reviewed and not demonstrated on this instance.

    Before COMMIT, a failed post-check or a caught error rolls the open
    transaction back. A NULL definition or a NULL SHA-256 throws 58117.
    Any other post-check failure throws 58115. Both paths print the
    diagnostic while the transaction is still open, then roll back.
    On SQL Server, ALTER PROCEDURE participates in an explicit
    transaction. The 2026-10-11 20:29:40 UTC diagnostic run threw 58115
    inside that transaction; the definition read afterwards was the
    baseline 339C66BF..., modify_date 2026-10-07 19:05:41.930.

    After COMMIT, this script has no second reversal. Do not repair a
    committed module from the working-tree procedure or from
    B_Apply_TotalExpenseEntries.sql. The reviewed recovery is
    D_Rollback_ExpenseAggSnapshot.sql. It requires the stored
    CREATE-form candidate
    5BEDA23C85279789D325C66B573C37BB1E037DA5C0153695A435DEA9E7D37547
    and verifies the restored definition against the pinned pre-image
    339C66BFBD16C10CE4906A687D14E62B68D96B80BE85008E7FB139D2FAFF9CCC.
*/
