/*
    Package:     Database/Deployments/RC014B
    Script:      D_Rollback_ExpenseAggSnapshot.sql
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN (AnmolDristi)
    Mode:        PREPARED — do not execute until recovery is explicitly authorized

    Object:      dbo.usp_SubmitSalesDailyActivities
    Object id:   877962204
    Change:      Reverse only the two #ExpenseAgg edits from the reviewed
                 apply candidate. Submit one ALTER PROCEDURE.
                 Do not DROP the procedure. DROP plus CREATE is not
                 authorized: it assigns a new object id and can drop
                 permissions and other object metadata.

    This script does not assume the apply has been committed. It reads
    the current module and continues only when that module is the
    reviewed applied candidate. Any other text, including the untouched
    CREATE pre-image, aborts before ALTER.

    Fingerprints, SHA2_256 over the nvarchar module as UTF-16LE
    (HASHBYTES('SHA2_256', CONVERT(varbinary(max), definition))):

      Stored applied candidate this script requires before it changes
      anything. It begins with CREATE   PROCEDURE:
        LEN 24828 / DATALENGTH 49656
        5BEDA23C85279789D325C66B573C37BB1E037DA5C0153695A435DEA9E7D37547

      ALTER statement this script submits:
        LEN 24456 / DATALENGTH 48912
        83F5961A17D7A2F756343C97C10EF2783E06B4DCC031552DA2E09D23DEB550AD

      Stored text it requires before COMMIT, the pinned CREATE pre-image:
        LEN 24457 / DATALENGTH 48914
        339C66BFBD16C10CE4906A687D14E62B68D96B80BE85008E7FB139D2FAFF9CCC

    Stored versus submitted: a diagnostic run of B_Apply on
    2026-10-11 20:29:40 UTC submitted ALTER   PROCEDURE and
    OBJECT_DEFINITION returned CREATE   PROCEDURE with the rest of the
    text unchanged (49656 bytes, 5BEDA23C...). This script therefore
    reads a CREATE-form module, submits that module with the original
    #ExpenseAgg anchor and the ALTER keyword, and requires the stored
    result to be the CREATE pre-image. The stored-result fingerprint
    applies the same keyword behavior to this ALTER. It is checked
    before COMMIT; a different stored text rolls back with 58215.
    The procedure body is the pinned pre-image body: original
    ExpenseTotal, no rejected-expense predicate, same lifecycle, same
    DetailVersion DESC and ApprovalRequestDetailId DESC ordering.
    PROCESSED and TotalExpenseEntries stay absent.

    Execution: DISABLED. @Execute defaults to 0 and returns before any
    catalog read or change. ALTER PROCEDURE rollback was not executed
    on this instance while preparing this script.

    This script does not execute dbo.usp_SubmitSalesDailyActivities.
    It does not insert, update, or delete claim rows.

    Operator instructions:

    Before COMMIT, a failed post-check or a caught error rolls back the
    open transaction. That path is in this file. It was not run here, so
    it is not evidence that this instance restored the module.

    After COMMIT, this script has no second reversal. A committed
    recovery stays in place until a later authorized script changes it.
    B_Apply_ExpenseAggSnapshot.sql will refuse the recovered module:
    the definition hash can match the pre-image again, but B_Apply also
    requires modify_date 2026-10-07 19:05:41.930, and this ALTER changes
    modify_date. Do not use the working-tree procedure or
    B_Apply_TotalExpenseEntries.sql as the restore source.
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
        THROW 58203, N'ABORT: this script must start with no open transaction. Procedure not changed.', 1;

    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    IF UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
    BEGIN
        DECLARE @ServerMsg nvarchar(400) =
            N'ABORT: server is ' + ISNULL(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName')), N'<null>')
            + N'. Expected MUMANMOAPP01\SQLEXPRESS. Procedure not changed.';
        THROW 58201, @ServerMsg, 1;
    END;

    IF DB_NAME() = N'AnmolDristi'
        THROW 58202, N'ABORT: Live database AnmolDristi is forbidden. Procedure not changed.', 1;

    IF DB_NAME() <> N'AnmolDristiDev'
    BEGIN
        DECLARE @DbMsg nvarchar(400) =
            N'ABORT: connected to ' + ISNULL(QUOTENAME(DB_NAME()), N'<null>')
            + N'. Expected [AnmolDristiDev]. Procedure not changed.';
        THROW 58202, @DbMsg, 1;
    END;

    DECLARE @ObjectId int = OBJECT_ID(N'dbo.usp_SubmitSalesDailyActivities', N'P');

    IF @ObjectId IS NULL OR @ObjectId <> 877962204
    BEGIN
        DECLARE @IdMsg nvarchar(400) =
            N'ABORT: object id is ' + ISNULL(CONVERT(nvarchar(20), @ObjectId), N'<null>')
            + N'. Expected 877962204. Procedure not changed.';
        THROW 58204, @IdMsg, 1;
    END;

    DECLARE @Definition nvarchar(max);
    DECLARE @UsesAnsiNulls bit;
    DECLARE @UsesQuotedIdentifier bit;

    SELECT
        @Definition = m.definition,
        @UsesAnsiNulls = m.uses_ansi_nulls,
        @UsesQuotedIdentifier = m.uses_quoted_identifier
    FROM sys.sql_modules AS m
    INNER JOIN sys.objects AS o
        ON o.object_id = m.object_id
    WHERE m.object_id = @ObjectId
      AND o.type = N'P';

    IF @Definition IS NULL
        THROW 58205, N'ABORT: procedure definition could not be read. Procedure not changed.', 1;

    IF ISNULL(@UsesAnsiNulls, 0) <> 1 OR ISNULL(@UsesQuotedIdentifier, 0) <> 1
        THROW 58206, N'ABORT: module ANSI_NULLS or QUOTED_IDENTIFIER is not ON. Refusing ALTER so those settings are not changed. Procedure not changed.', 1;

    IF LEN(@Definition) <> 24828 OR DATALENGTH(@Definition) <> 49656
    BEGIN
        DECLARE @LenMsg nvarchar(400) =
            N'ABORT: definition length is ' + CONVERT(nvarchar(20), LEN(@Definition))
            + N' characters / ' + CONVERT(nvarchar(20), DATALENGTH(@Definition))
            + N' bytes. Expected the stored applied module 24828 / 49656. Procedure not changed.';
        THROW 58207, @LenMsg, 1;
    END;

    DECLARE @ExpectedAppliedHash varchar(64) = '5BEDA23C85279789D325C66B573C37BB1E037DA5C0153695A435DEA9E7D37547';
    DECLARE @ActualHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Definition)), 2);

    IF @ActualHash IS NULL OR @ActualHash <> @ExpectedAppliedHash
    BEGIN
        DECLARE @HashMsg nvarchar(500) =
            N'ABORT: definition SHA2_256 is ' + ISNULL(@ActualHash, N'<null>')
            + N'. Expected the stored CREATE-form applied module. The untouched pre-image is not a recovery source. Procedure not changed.';
        THROW 58208, @HashMsg, 1;
    END;

    IF CHARINDEX(N'PROCESSED', @Definition) > 0
        THROW 58209, N'ABORT: definition contains PROCESSED. Procedure not changed.', 1;

    IF CHARINDEX(N'TotalExpenseEntries', @Definition) > 0
        THROW 58210, N'ABORT: TotalExpenseEntries is present. Procedure not changed.', 1;

    DECLARE @DetailVersionToken nvarchar(40) = N'DetailVersion DESC';
    DECLARE @DetailIdToken nvarchar(40) = N'ApprovalRequestDetailId DESC';
    DECLARE @ClaimAmountToken nvarchar(40) = N'a.CalculatedClaimAmount';

    IF CHARINDEX(N'A Rejected (or missing) id is not an active lifecycle.', @Definition) = 0
        OR CHARINDEX(N'N''RETURNED'', N''PENDING'', N''COMPLETED'', N''READYFORPAYMENT''', @Definition) = 0
        OR (LEN(@Definition) - LEN(REPLACE(@Definition, @DetailVersionToken, N''))) / LEN(@DetailVersionToken) <> 3
        OR (LEN(@Definition) - LEN(REPLACE(@Definition, @DetailIdToken, N''))) / LEN(@DetailIdToken) <> 3
        OR (LEN(@Definition) - LEN(REPLACE(@Definition, @ClaimAmountToken, N''))) / LEN(@ClaimAmountToken) <> 2
        THROW 58211, N'ABORT: lifecycle or detail-ownership text does not match the reviewed applied module. Procedure not changed.', 1;

    IF (LEN(@Definition) - LEN(REPLACE(@Definition, N'CREATE   PROCEDURE', N''))) / LEN(N'CREATE   PROCEDURE') <> 1
        OR CHARINDEX(N'ALTER   PROCEDURE', @Definition) > 0
        THROW 58212, N'ABORT: the stored applied module must contain CREATE PROCEDURE once and must not contain the ALTER PROCEDURE token. Procedure not changed.', 1;

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

    IF (LEN(@Definition) - LEN(REPLACE(@Definition, @NewAnchor, N''))) / LEN(@NewAnchor) <> 1
        OR CHARINDEX(@OldAnchor, @Definition) > 0
        THROW 58213, N'ABORT: the applied #ExpenseAgg anchor was not found exactly once. Procedure not changed.', 1;

    IF (LEN(@Definition) - LEN(REPLACE(@Definition, N'ISNULL(e.ClaimStatus, '''') <> ''Rejected''', N'')))
        / LEN(N'ISNULL(e.ClaimStatus, '''') <> ''Rejected''') <> 1
        THROW 58213, N'ABORT: the applied module does not contain the rejected-expense predicate exactly once. Procedure not changed.', 1;

    -- The stored module begins with CREATE. The submitted statement must
    -- begin with ALTER, or it would try to create an existing object.
    DECLARE @Recovery nvarchar(max) =
        REPLACE(REPLACE(@Definition, @NewAnchor, @OldAnchor), N'CREATE   PROCEDURE', N'ALTER   PROCEDURE');
    DECLARE @RoundTrip nvarchar(max) =
        REPLACE(REPLACE(@Recovery, N'ALTER   PROCEDURE', N'CREATE   PROCEDURE'), @OldAnchor, @NewAnchor);

    IF @Recovery IS NULL
        OR @RoundTrip IS NULL
        OR @RoundTrip <> @Definition
        OR DATALENGTH(@RoundTrip) <> DATALENGTH(@Definition)
        OR CONVERT(varbinary(max), @RoundTrip) <> CONVERT(varbinary(max), @Definition)
        THROW 58214, N'ABORT: recovery text does not round-trip to the applied module. Procedure not changed.', 1;

    IF LEN(@Recovery) <> 24456 OR DATALENGTH(@Recovery) <> 48912
        THROW 58214, N'ABORT: recovery length is not 24456 characters / 48912 bytes. Procedure not changed.', 1;

    DECLARE @ExpectedRecoveryHash varchar(64) = '83F5961A17D7A2F756343C97C10EF2783E06B4DCC031552DA2E09D23DEB550AD';
    DECLARE @RecoveryHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Recovery)), 2);

    IF @RecoveryHash IS NULL OR @RecoveryHash <> @ExpectedRecoveryHash
        THROW 58214, N'ABORT: recovery SHA2_256 is not the reviewed ALTER restore. Procedure not changed.', 1;

    IF @RecoveryHash = @ExpectedAppliedHash
        OR @RecoveryHash = '339C66BFBD16C10CE4906A687D14E62B68D96B80BE85008E7FB139D2FAFF9CCC'
        THROW 58214, N'ABORT: recovery fingerprint collided with the applied module or the CREATE pre-image. Procedure not changed.', 1;

    IF (LEN(@Recovery) - LEN(REPLACE(@Recovery, N'ALTER   PROCEDURE', N''))) / LEN(N'ALTER   PROCEDURE') <> 1
        OR CHARINDEX(N'CREATE   PROCEDURE', @Recovery) > 0
        OR CHARINDEX(@OldAnchor, @Recovery) = 0
        OR CHARINDEX(@NewAnchor, @Recovery) > 0
        OR CHARINDEX(N'ISNULL(e.ClaimStatus, '''') <> ''Rejected''', @Recovery) > 0
        OR CHARINDEX(N'SUM(ISNULL(e.Amount,0))', @Recovery) = 0
        OR CHARINDEX(N'PROCESSED', @Recovery) > 0
        OR CHARINDEX(N'TotalExpenseEntries', @Recovery) > 0
        OR CHARINDEX(N'A Rejected (or missing) id is not an active lifecycle.', @Recovery) = 0
        OR CHARINDEX(N'N''RETURNED'', N''PENDING'', N''COMPLETED'', N''READYFORPAYMENT''', @Recovery) = 0
        OR (LEN(@Recovery) - LEN(REPLACE(@Recovery, @DetailVersionToken, N''))) / LEN(@DetailVersionToken) <> 3
        OR (LEN(@Recovery) - LEN(REPLACE(@Recovery, @DetailIdToken, N''))) / LEN(@DetailIdToken) <> 3
        OR (LEN(@Recovery) - LEN(REPLACE(@Recovery, @ClaimAmountToken, N''))) / LEN(@ClaimAmountToken) <> 2
        OR CHARINDEX(N'ExpenseType = ''DA'' THEN ISNULL(e.Amount, 0)', @Recovery) > 0
        OR CHARINDEX(N'ExpenseType = ''Hotel'' THEN ISNULL(e.Amount, 0)', @Recovery) > 0
        OR CHARINDEX(N'ExpenseType = ''Travel'' THEN ISNULL(e.Amount, 0)', @Recovery) > 0
        OR CHARINDEX(N'ExpenseType = ''Stationary'' THEN ISNULL(e.Amount, 0)', @Recovery) > 0
        THROW 58214, N'ABORT: recovery text failed the pre-image behavior review. Procedure not changed.', 1;

    -- Expected stored text after the ALTER: the submitted statement with
    -- the CREATE keyword, which is the pinned pre-image.
    DECLARE @ExpectedRestored nvarchar(max) = REPLACE(@Recovery, N'ALTER   PROCEDURE', N'CREATE   PROCEDURE');
    DECLARE @ExpectedRestoredHash varchar(64) = '339C66BFBD16C10CE4906A687D14E62B68D96B80BE85008E7FB139D2FAFF9CCC';
    DECLARE @ExpectedRestoredActualHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @ExpectedRestored)), 2);

    IF @ExpectedRestored IS NULL
        OR @ExpectedRestoredActualHash IS NULL
        OR @ExpectedRestoredActualHash <> @ExpectedRestoredHash
        OR LEN(@ExpectedRestored) <> 24457
        OR DATALENGTH(@ExpectedRestored) <> 48914
        OR (LEN(@ExpectedRestored) - LEN(REPLACE(@ExpectedRestored, N'CREATE   PROCEDURE', N''))) / LEN(N'CREATE   PROCEDURE') <> 1
        OR CHARINDEX(N'ALTER   PROCEDURE', @ExpectedRestored) > 0
        OR @ExpectedRestoredHash = @ExpectedAppliedHash
        OR @ExpectedRestoredHash = @ExpectedRecoveryHash
        THROW 58218, N'ABORT: expected restored definition is not the pinned CREATE pre-image. Procedure not changed.', 1;

    BEGIN TRANSACTION;

    DECLARE @Confirmed nvarchar(max) = OBJECT_DEFINITION(@ObjectId);
    DECLARE @ConfirmedHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Confirmed)), 2);

    IF @Confirmed IS NULL
        OR @ConfirmedHash IS NULL
        OR @ConfirmedHash <> @ExpectedAppliedHash
        OR DATALENGTH(@Confirmed) <> 49656
        OR CONVERT(varbinary(max), @Confirmed) <> CONVERT(varbinary(max), @Definition)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 58217, N'ABORT: the module changed after the pre-check. Recovery was rolled back.', 1;
    END;

    DECLARE @RecoveryNow nvarchar(max) =
        REPLACE(REPLACE(@Confirmed, @NewAnchor, @OldAnchor), N'CREATE   PROCEDURE', N'ALTER   PROCEDURE');
    DECLARE @RecoveryNowHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @RecoveryNow)), 2);

    IF @RecoveryNow IS NULL
        OR @RecoveryNowHash IS NULL
        OR @RecoveryNowHash <> @ExpectedRecoveryHash
        OR DATALENGTH(@RecoveryNow) <> 48912
        OR CONVERT(varbinary(max), @RecoveryNow) <> CONVERT(varbinary(max), @Recovery)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 58217, N'ABORT: the in-transaction module did not rebuild the reviewed ALTER restore. Recovery was rolled back.', 1;
    END;

    DECLARE @OwnerPrincipalId int;
    DECLARE @SchemaId int;

    SELECT
        @OwnerPrincipalId = o.principal_id,
        @SchemaId = o.schema_id
    FROM sys.objects AS o
    WHERE o.object_id = @ObjectId
      AND o.type = N'P';

    IF @SchemaId IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 58216, N'ABORT: procedure schema could not be read. Recovery was rolled back.', 1;
    END;

    DECLARE @PermissionXml xml = (
        SELECT
            dp.grantee_principal_id,
            dp.state_desc,
            dp.permission_name,
            dp.minor_id,
            dp.grantor_principal_id
        FROM sys.database_permissions AS dp
        WHERE dp.class = 1
          AND dp.major_id = @ObjectId
        ORDER BY
            dp.grantee_principal_id,
            dp.state_desc,
            dp.permission_name,
            dp.minor_id,
            dp.grantor_principal_id
        FOR XML PATH(N'p'), TYPE
    );
    DECLARE @PermissionText nvarchar(max) = ISNULL(CONVERT(nvarchar(max), @PermissionXml), N'');
    DECLARE @PermissionHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @PermissionText)), 2);

    IF @PermissionHash IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 58216, N'ABORT: permission fingerprint could not be read. Recovery was rolled back.', 1;
    END;

    -- @RecoveryNow is the applied module with the two #ExpenseAgg edits
    -- removed and the CREATE keyword changed to ALTER. It is one ALTER
    -- PROCEDURE batch. This does not call the procedure and does not
    -- change claim rows.
    EXEC (@RecoveryNow);

    DECLARE @Applied nvarchar(max) = OBJECT_DEFINITION(@ObjectId);
    DECLARE @AppliedHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Applied)), 2);

    DECLARE @PermissionXmlAfter xml = (
        SELECT
            dp.grantee_principal_id,
            dp.state_desc,
            dp.permission_name,
            dp.minor_id,
            dp.grantor_principal_id
        FROM sys.database_permissions AS dp
        WHERE dp.class = 1
          AND dp.major_id = @ObjectId
        ORDER BY
            dp.grantee_principal_id,
            dp.state_desc,
            dp.permission_name,
            dp.minor_id,
            dp.grantor_principal_id
        FOR XML PATH(N'p'), TYPE
    );
    DECLARE @PermissionTextAfter nvarchar(max) = ISNULL(CONVERT(nvarchar(max), @PermissionXmlAfter), N'');
    DECLARE @PermissionHashAfter varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @PermissionTextAfter)), 2);

    IF @Applied IS NULL
        OR @AppliedHash IS NULL
        OR @PermissionHashAfter IS NULL
        OR @Applied <> @ExpectedRestored
        OR CONVERT(varbinary(max), @Applied) <> CONVERT(varbinary(max), @ExpectedRestored)
        OR @AppliedHash <> @ExpectedRestoredHash
        OR LEN(@Applied) <> 24457
        OR DATALENGTH(@Applied) <> 48914
        OR OBJECT_ID(N'dbo.usp_SubmitSalesDailyActivities', N'P') <> 877962204
        OR @PermissionHashAfter <> @PermissionHash
        OR DATALENGTH(@PermissionTextAfter) <> DATALENGTH(@PermissionText)
        OR NOT EXISTS (
            SELECT @OwnerPrincipalId, @SchemaId
            INTERSECT
            SELECT o.principal_id, o.schema_id
            FROM sys.objects AS o
            WHERE o.object_id = @ObjectId
              AND o.type = N'P'
        )
        OR CHARINDEX(N'ISNULL(e.ClaimStatus, '''') <> ''Rejected''', @Applied) > 0
        OR CHARINDEX(N'SUM(ISNULL(e.Amount,0))', @Applied) = 0
        OR CHARINDEX(N'PROCESSED', @Applied) > 0
        OR CHARINDEX(N'TotalExpenseEntries', @Applied) > 0
        OR CHARINDEX(N'ExpenseType = ''DA'' THEN ISNULL(e.Amount, 0)', @Applied) > 0
        OR CHARINDEX(N'A Rejected (or missing) id is not an active lifecycle.', @Applied) = 0
        OR (LEN(@Applied) - LEN(REPLACE(@Applied, @DetailVersionToken, N''))) / LEN(@DetailVersionToken) <> 3
        OR (LEN(@Applied) - LEN(REPLACE(@Applied, @DetailIdToken, N''))) / LEN(@DetailIdToken) <> 3
        OR (LEN(@Applied) - LEN(REPLACE(@Applied, @ClaimAmountToken, N''))) / LEN(@ClaimAmountToken) <> 2
    BEGIN
        DECLARE @RestoreMessage nvarchar(2048) =
            N'ABORT: altered module did not match the pinned CREATE pre-image, or object identity or permissions changed. Change was rolled back.'
            + N' DefinitionNull=' + CASE WHEN @Applied IS NULL THEN N'1' ELSE N'0' END
            + N' AppliedBytes=' + ISNULL(CONVERT(nvarchar(20), DATALENGTH(@Applied)), N'NULL')
            + N' ExpectedBytes=48914'
            + N' AppliedHash=' + ISNULL(@AppliedHash, N'NULL')
            + N' ExpectedHash=' + @ExpectedRestoredHash
            + N' SubmittedHash=' + @ExpectedRecoveryHash
            + N' ObjectId=' + ISNULL(CONVERT(nvarchar(20), OBJECT_ID(N'dbo.usp_SubmitSalesDailyActivities', N'P')), N'NULL')
            + N' PermissionsEqual=' + CASE
                WHEN @PermissionHashAfter = @PermissionHash THEN N'1'
                WHEN @PermissionHashAfter <> @PermissionHash THEN N'0'
                ELSE N'U'
              END;

        IF @RestoreMessage IS NULL OR LEN(@RestoreMessage) > 2048
            SET @RestoreMessage = N'ABORT: altered module did not match the pinned CREATE pre-image, or object identity or permissions changed. Change was rolled back.';

        PRINT @RestoreMessage;
        ROLLBACK TRANSACTION;
        THROW 58215, @RestoreMessage, 1;
    END;

    COMMIT TRANSACTION;

    PRINT N'Restored the pinned #ExpenseAgg behavior on AnmolDristiDev with ALTER PROCEDURE.';
    PRINT N'Object id 877962204 was retained. The stored definition is the pinned CREATE pre-image, SHA2_256 ' + @AppliedHash + N'.';
    PRINT N'The submission procedure was not executed. No claim row was modified.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

/*
    Pre-commit and post-commit are different.

    Pre-commit: the ROLLBACK statements above, and the CATCH block, undo
    this script's open transaction when a check fails or a statement
    raises. On SQL Server, ALTER PROCEDURE participates in an explicit
    transaction. This file was not executed, so that engine behavior is
    not demonstrated here.

    Post-commit: CATCH does not run after a successful COMMIT, and this
    script contains no second reversal. COMMIT is reached only when the
    stored module equals the pinned CREATE pre-image by text, bytes, and
    SHA2_256
    339C66BFBD16C10CE4906A687D14E62B68D96B80BE85008E7FB139D2FAFF9CCC.
    The submitted ALTER statement hash
    83F5961A17D7A2F756343C97C10EF2783E06B4DCC031552DA2E09D23DEB550AD
    is not expected in sys.sql_modules. modify_date changes, so it no
    longer equals the 2026-10-07 19:05:41.930 baseline.
*/
