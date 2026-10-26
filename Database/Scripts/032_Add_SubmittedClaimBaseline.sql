/*
    Submitted travel baseline for the kilometre-deduction display.

    Adds two nullable columns to dbo.SalesApprovalRequestDetail:
      SubmittedDistanceKM    decimal(10,2) NULL
      SubmittedClaimAmount   decimal(18,2) NULL

    Existing rows stay NULL. This script does not backfill from kilometre
    history, current snapshots, or a rate.

    Both additions run in one transaction. A failure rolls both back.
    When neither column exists they are added by one ALTER TABLE statement.
    When both already exist the script commits no schema change.
    When only one exists, the missing column is added so a rerun can finish
    a previously partial schema. The following existence check then requires
    both columns before commit.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.SalesApprovalRequestDetail', N'U') IS NULL
BEGIN
    RAISERROR(N'Migration aborted: dbo.SalesApprovalRequestDetail does not exist.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

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

    IF @HasDistance = 0 AND @HasAmount = 0
    BEGIN
        ALTER TABLE dbo.SalesApprovalRequestDetail ADD
            SubmittedDistanceKM decimal(10,2) NULL,
            SubmittedClaimAmount decimal(18,2) NULL;
        PRINT N'Added SubmittedDistanceKM and SubmittedClaimAmount.';
    END
    ELSE IF @HasDistance = 0
    BEGIN
        ALTER TABLE dbo.SalesApprovalRequestDetail
            ADD SubmittedDistanceKM decimal(10,2) NULL;
        PRINT N'Added the missing SubmittedDistanceKM column.';
    END
    ELSE IF @HasAmount = 0
    BEGIN
        ALTER TABLE dbo.SalesApprovalRequestDetail
            ADD SubmittedClaimAmount decimal(18,2) NULL;
        PRINT N'Added the missing SubmittedClaimAmount column.';
    END
    ELSE
    BEGIN
        PRINT N'SubmittedDistanceKM and SubmittedClaimAmount already exist. No schema change.';
    END;

    IF COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'SubmittedDistanceKM') IS NULL
        OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'SubmittedClaimAmount') IS NULL
        THROW 53201, 'Submitted baseline columns were not both present after the migration.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
