/*
    Rollback for 032_Add_SubmittedClaimBaseline.sql.

    This is not a routine deployment rollback. Dropping a populated
    SubmittedDistanceKM or SubmittedClaimAmount value destroys the submitted
    travel baseline. Kilometre history and a rate cannot reconstruct it.

    @Confirm defaults to 0. The script reports column state and returns
    without dropping anything.
    @AllowDropPopulated defaults to 0. A non-null value in either column
    aborts the rollback. Set @AllowDropPopulated = 1 only when that data
    loss has been explicitly accepted. Do not use that override as the
    normal way to undo this migration.

    Both columns are checked independently. The drop runs in one transaction,
    so a failure does not leave one column dropped and the other present
    from this execution.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Confirm bit = 0;
DECLARE @AllowDropPopulated bit = 0;

IF OBJECT_ID(N'dbo.SalesApprovalRequestDetail', N'U') IS NULL
BEGIN
    RAISERROR(N'Rollback aborted: dbo.SalesApprovalRequestDetail does not exist.', 16, 1);
    RETURN;
END;

DECLARE @HasDistance bit =
    CASE
        WHEN COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'SubmittedDistanceKM') IS NULL THEN 0
        ELSE 1
    END;
DECLARE @HasAmount bit =
    CASE
        WHEN COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'SubmittedClaimAmount') IS NULL THEN 0
        ELSE 1
    END;

DECLARE @DistancePopulated int = 0;
DECLARE @AmountPopulated int = 0;

IF @HasDistance = 1
BEGIN
    EXEC sys.sp_executesql
        N'SELECT @CountOut = COUNT(*) FROM dbo.SalesApprovalRequestDetail WHERE SubmittedDistanceKM IS NOT NULL;',
        N'@CountOut int OUTPUT',
        @CountOut = @DistancePopulated OUTPUT;
END;

IF @HasAmount = 1
BEGIN
    EXEC sys.sp_executesql
        N'SELECT @CountOut = COUNT(*) FROM dbo.SalesApprovalRequestDetail WHERE SubmittedClaimAmount IS NOT NULL;',
        N'@CountOut int OUTPUT',
        @CountOut = @AmountPopulated OUTPUT;
END;

PRINT N'SubmittedDistanceKM present=' + CONVERT(nvarchar(1), @HasDistance)
    + N', populated=' + CONVERT(nvarchar(20), @DistancePopulated) + N'.';
PRINT N'SubmittedClaimAmount present=' + CONVERT(nvarchar(1), @HasAmount)
    + N', populated=' + CONVERT(nvarchar(20), @AmountPopulated) + N'.';

IF @HasDistance = 0 AND @HasAmount = 0
BEGIN
    PRINT N'Both baseline columns are already absent. Nothing was dropped.';
    RETURN;
END;

IF ISNULL(@Confirm, 0) <> 1
BEGIN
    PRINT N'@Confirm is not 1. No column was dropped.';
    PRINT N'Dropping populated baselines destroys values that cannot be reconstructed.';
    RETURN;
END;

IF (@DistancePopulated > 0 OR @AmountPopulated > 0) AND ISNULL(@AllowDropPopulated, 0) <> 1
BEGIN
    RAISERROR(N'Rollback aborted: a baseline column contains values that cannot be reconstructed. @AllowDropPopulated is not 1, so nothing was dropped.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @DropList nvarchar(200) = N'';

    IF @HasDistance = 1
        SET @DropList = N'SubmittedDistanceKM';

    IF @HasAmount = 1
        SET @DropList =
            CASE
                WHEN @DropList = N'' THEN N'SubmittedClaimAmount'
                ELSE @DropList + N', SubmittedClaimAmount'
            END;

    DECLARE @DropSql nvarchar(400) =
        N'ALTER TABLE dbo.SalesApprovalRequestDetail DROP COLUMN ' + @DropList + N';';

    EXEC sys.sp_executesql @DropSql;

    IF COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'SubmittedDistanceKM') IS NOT NULL
        OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'SubmittedClaimAmount') IS NOT NULL
        THROW 53202, 'Submitted baseline columns were still present after the rollback drop.', 1;

    COMMIT TRANSACTION;

    PRINT N'Baseline columns dropped.';

    IF ISNULL(@AllowDropPopulated, 0) = 1
        AND (@DistancePopulated > 0 OR @AmountPopulated > 0)
        PRINT N'WARNING: populated submitted baselines were destroyed and cannot be reconstructed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
