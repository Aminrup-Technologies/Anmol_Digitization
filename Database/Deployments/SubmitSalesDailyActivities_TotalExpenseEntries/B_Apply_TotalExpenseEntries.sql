/*
    Package:     Database/Deployments/SubmitSalesDailyActivities_TotalExpenseEntries
    Script:      B_Apply_TotalExpenseEntries.sql
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN (AnmolDristi)

    Object:      dbo.usp_SubmitSalesDailyActivities
    Change:      Add TotalExpenseEntries to the existing header roll-up only.

    Authoritative body:
      The module currently stored on AnmolDristiDev, reviewed 2026-10-11.
      This script does not read or apply the longer repository procedure.
      Repository lifecycle text is left untouched.

    Reviewed fingerprint:
      modify_date          2026-10-07 19:05:41.930
      LEN(definition)      24457
      DATALENGTH           48914
      SHA2_256             339C66BFBD16C10CE4906A687D14E62B68D96B80BE85008E7FB139D2FAFF9CCC
      TotalExpenseEntries  absent
      PROCESSED token      absent

    Behavior preserved from that definition:
      NEW and RESUBMIT control flow
      Returned-claim request-id reuse
      Processed / Completed / ReadyForPayment / unexpected-status handling
      Transaction, validation, approver, snapshot, and financial totals

    This script does not execute dbo.usp_SubmitSalesDailyActivities.
    It does not update request 118 or any claim row.
    It does not alter any other database object.

    Do not run until deployment is explicitly authorized.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY

    IF @@TRANCOUNT <> 0
        THROW 57003, N'ABORT: this script must start with no open transaction.', 1;

    IF DB_NAME() = N'AnmolDristi'
        THROW 57001, N'ABORT: Live database AnmolDristi is forbidden.', 1;

    IF DB_NAME() <> N'AnmolDristiDev'
    BEGIN
        DECLARE @DbMsg nvarchar(400) =
            N'ABORT: connected to ' + QUOTENAME(DB_NAME()) + N'. Expected [AnmolDristiDev].';
        THROW 57002, @DbMsg, 1;
    END;

    DECLARE @ObjectId int = OBJECT_ID(N'dbo.usp_SubmitSalesDailyActivities', N'P');
    IF @ObjectId IS NULL
        THROW 57004, N'ABORT: dbo.usp_SubmitSalesDailyActivities was not found.', 1;

    DECLARE @Definition nvarchar(max);
    DECLARE @ModifyDate datetime;

    SELECT
        @Definition = m.definition,
        @ModifyDate = o.modify_date
    FROM sys.sql_modules AS m
    INNER JOIN sys.objects AS o
        ON o.object_id = m.object_id
    WHERE m.object_id = @ObjectId
      AND o.type = N'P';

    IF @Definition IS NULL OR @ModifyDate IS NULL
        THROW 57004, N'ABORT: procedure definition or modify_date could not be read.', 1;

    IF CONVERT(varchar(23), @ModifyDate, 121) <> '2026-10-07 19:05:41.930'
    BEGIN
        DECLARE @DateMsg nvarchar(400) =
            N'ABORT: modify_date is ' + CONVERT(varchar(23), @ModifyDate, 121)
            + N'. Expected 2026-10-07 19:05:41.930. Procedure not changed.';
        THROW 57005, @DateMsg, 1;
    END;

    IF LEN(@Definition) <> 24457 OR DATALENGTH(@Definition) <> 48914
    BEGIN
        DECLARE @LenMsg nvarchar(400) =
            N'ABORT: definition length is ' + CONVERT(nvarchar(20), LEN(@Definition))
            + N' characters / ' + CONVERT(nvarchar(20), DATALENGTH(@Definition))
            + N' bytes. Expected 24457 / 48914. Procedure not changed.';
        THROW 57006, @LenMsg, 1;
    END;

    DECLARE @ExpectedHash varchar(64) = '339C66BFBD16C10CE4906A687D14E62B68D96B80BE85008E7FB139D2FAFF9CCC';
    DECLARE @ActualHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', @Definition), 2);

    IF @ActualHash <> @ExpectedHash
    BEGIN
        DECLARE @HashMsg nvarchar(400) =
            N'ABORT: definition SHA2_256 is ' + @ActualHash
            + N'. Expected the reviewed AnmolDristiDev fingerprint. Procedure not changed.';
        THROW 57007, @HashMsg, 1;
    END;

    IF CHARINDEX(N'TotalExpenseEntries', @Definition) > 0
        THROW 57008, N'ABORT: TotalExpenseEntries is already present. Procedure not changed.', 1;

    IF CHARINDEX(N'PROCESSED', @Definition) > 0
        THROW 57009, N'ABORT: definition contains PROCESSED. Refusing to mix in lifecycle changes. Procedure not changed.', 1;

    IF CHARINDEX(N'A Rejected (or missing) id is not an active lifecycle.', @Definition) = 0
        THROW 57010, N'ABORT: deployed closed-predecessor rule was not found. Procedure not changed.', 1;

    IF CHARINDEX(N'N''RETURNED'', N''PENDING'', N''COMPLETED'', N''READYFORPAYMENT''', @Definition) = 0
        THROW 57010, N'ABORT: deployed active-status list was not found. Procedure not changed.', 1;

    DECLARE @CrLf nchar(2) = NCHAR(13) + NCHAR(10);
    DECLARE @T1 nvarchar(1) = NCHAR(9);
    DECLARE @T2 nvarchar(2) = @T1 + @T1;
    DECLARE @T3 nvarchar(3) = @T2 + @T1;
    DECLARE @T4 nvarchar(4) = @T3 + @T1;
    DECLARE @T5 nvarchar(5) = @T4 + @T1;
    DECLARE @T6 nvarchar(6) = @T5 + @T1;
    DECLARE @T7 nvarchar(7) = @T6 + @T1;
    DECLARE @T8 nvarchar(8) = @T7 + @T1;

    DECLARE @Assignment nvarchar(max) =
        @T2 + N'TotalExpenseEntries =' + @CrLf
        + @T2 + N'(' + @CrLf
        + @T3 + N'SELECT COUNT(*)' + @CrLf
        + @T3 + N'FROM dbo.SalesDailyExpense e' + @CrLf
        + @T3 + N'INNER JOIN' + @CrLf
        + @T3 + N'(' + @CrLf
        + @T4 + N'SELECT canon.ActivityId' + @CrLf
        + @T4 + N'FROM' + @CrLf
        + @T4 + N'(' + @CrLf
        + @T5 + N'SELECT' + @CrLf
        + @T6 + N'd.ActivityId,' + @CrLf
        + @T6 + N'ROW_NUMBER() OVER' + @CrLf
        + @T6 + N'(' + @CrLf
        + @T7 + N'PARTITION BY d.ApprovalRequestId, d.ActivityId' + @CrLf
        + @T7 + N'ORDER BY' + @CrLf
        + @T8 + N'd.DetailVersion DESC,' + @CrLf
        + @T8 + N'd.ApprovalRequestDetailId DESC' + @CrLf
        + @T6 + N') AS RN' + @CrLf
        + @T5 + N'FROM dbo.SalesApprovalRequestDetail d' + @CrLf
        + @T5 + N'WHERE d.ApprovalRequestId = @ApprovalRequestId' + @CrLf
        + @T4 + N') canon' + @CrLf
        + @T4 + N'WHERE canon.RN = 1' + @CrLf
        + @T3 + N') latest' + @CrLf
        + @T4 + N'ON latest.ActivityId = e.ActivityId' + @CrLf
        + @T3 + N'WHERE e.IsDeleted = 0' + @CrLf
        + @T2 + N'),';

    DECLARE @OldAnchor nvarchar(max) =
        @T2 + N'),' + @CrLf
        + @CrLf
        + @T2 + N'ModifiedOn = @Now,' + @CrLf
        + @T2 + N'ModifiedBy = @LoginId' + @CrLf
        + @CrLf
        + @T1 + N'WHERE ApprovalRequestId = @ApprovalRequestId;';

    DECLARE @NewAnchor nvarchar(max) =
        @T2 + N'),' + @CrLf
        + @CrLf
        + @Assignment + @CrLf
        + @CrLf
        + @T2 + N'ModifiedOn = @Now,' + @CrLf
        + @T2 + N'ModifiedBy = @LoginId' + @CrLf
        + @CrLf
        + @T1 + N'WHERE ApprovalRequestId = @ApprovalRequestId;';

    IF (LEN(@Definition) - LEN(REPLACE(@Definition, @OldAnchor, N''))) / LEN(@OldAnchor) <> 1
        THROW 57011, N'ABORT: header roll-up anchor was not found exactly once. Procedure not changed.', 1;

    IF (LEN(@Definition) - LEN(REPLACE(@Definition, N'CREATE   PROCEDURE', N''))) / LEN(N'CREATE   PROCEDURE') <> 1
        THROW 57012, N'ABORT: CREATE PROCEDURE token was not found exactly once. Procedure not changed.', 1;

    DECLARE @Candidate nvarchar(max) = REPLACE(@Definition, N'CREATE   PROCEDURE', N'ALTER   PROCEDURE');
    SET @Candidate = REPLACE(@Candidate, @OldAnchor, @NewAnchor);

    IF LEN(@Candidate) <> LEN(@Definition) + LEN(@Assignment) + (2 * LEN(@CrLf))
        THROW 57013, N'ABORT: constructed definition length is not the reviewed body plus the counter. Procedure not changed.', 1;

    IF (LEN(@Candidate) - LEN(REPLACE(@Candidate, N'TotalExpenseEntries', N''))) / LEN(N'TotalExpenseEntries') <> 1
        THROW 57013, N'ABORT: constructed definition does not contain the counter exactly once. Procedure not changed.', 1;

    IF CHARINDEX(N'PROCESSED', @Candidate) > 0
        THROW 57013, N'ABORT: constructed definition contains PROCESSED. Procedure not changed.', 1;

    IF CHARINDEX(N'A Rejected (or missing) id is not an active lifecycle.', @Candidate) = 0
        THROW 57013, N'ABORT: constructed definition lost the deployed closed-predecessor rule. Procedure not changed.', 1;

    DECLARE @RoundTrip nvarchar(max) = REPLACE(@Candidate, @NewAnchor, @OldAnchor);
    SET @RoundTrip = REPLACE(@RoundTrip, N'ALTER   PROCEDURE', N'CREATE   PROCEDURE');

    IF @RoundTrip <> @Definition
        THROW 57013, N'ABORT: constructed definition does not round-trip to the reviewed body. Procedure not changed.', 1;

    BEGIN TRANSACTION;

    -- @Candidate is the reviewed module with CREATE changed to ALTER
    -- and the counter inserted. This does not call the procedure.
    EXEC (@Candidate);

    DECLARE @Applied nvarchar(max) = OBJECT_DEFINITION(@ObjectId);

    IF @Applied <> @Candidate
       OR CHARINDEX(N'TotalExpenseEntries', @Applied) = 0
       OR CHARINDEX(N'PROCESSED', @Applied) > 0
       OR CHARINDEX(N'A Rejected (or missing) id is not an active lifecycle.', @Applied) = 0
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 57014, N'ABORT: altered module did not match the constructed definition. Change was rolled back.', 1;
    END;

    COMMIT TRANSACTION;

    PRINT N'Applied TotalExpenseEntries to dbo.usp_SubmitSalesDailyActivities on AnmolDristiDev.';
    PRINT N'The submission procedure was not executed. No claim row was modified.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

/*
    Post-deployment verification is not part of this apply batch.
    Run it separately, read-only, after an authorized deployment.

    DECLARE @ApprovalRequestId int = NULL;

    ;WITH CanonicalLatestDetail AS
    (
        SELECT
            d.ApprovalRequestId,
            d.ActivityId,
            ROW_NUMBER() OVER
            (
                PARTITION BY d.ApprovalRequestId, d.ActivityId
                ORDER BY
                    d.DetailVersion DESC,
                    d.ApprovalRequestDetailId DESC
            ) AS RN
        FROM dbo.SalesApprovalRequestDetail AS d
        WHERE d.ApprovalRequestId = @ApprovalRequestId
    )
    SELECT
        r.ApprovalRequestId,
        r.Status,
        r.TotalExpenseEntries AS HeaderTotalExpenseEntries,
        r.ModifiedOn,
        r.ModifiedBy,
        COUNT(e.ExpenseId) AS LiveExpenseRows,
        COUNT(DISTINCT e.ExpenseId) AS DistinctExpenseIds
    FROM dbo.SalesApprovalRequest AS r
    LEFT JOIN CanonicalLatestDetail AS c
        ON c.ApprovalRequestId = r.ApprovalRequestId
       AND c.RN = 1
    LEFT JOIN dbo.SalesDailyExpense AS e
        ON e.ActivityId = c.ActivityId
       AND e.IsDeleted = 0
    WHERE r.ApprovalRequestId = @ApprovalRequestId
    GROUP BY
        r.ApprovalRequestId,
        r.Status,
        r.TotalExpenseEntries,
        r.ModifiedOn,
        r.ModifiedBy;
*/
