/*
    Package:     Database/Deployments/HRMS_PhaseA_B1
    Script:      002_ImportBatchMaster_MasterSyncColumns.sql
    Baseline:    Aug26_AILWorks_AmimnrupSupport @ 5ff3a3f (PR #203)
    Source:      Database/Scripts/011_HRMS_ImportBatchMaster_MasterSyncCompleted.sql
    Purpose:     Idempotent add of MasterSyncCompleted / MasterSyncDate.

    Live recon (read-only, 2026-09-02): both columns already exist with
    DF_ImportBatchMaster_MasterSyncCompleted DEFAULT ((0)). This script
    is therefore a no-op on current Live and is retained for repeatability.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ExpectedDatabase sysname = N'AnmolDristi';
DECLARE @AllowOtherDatabase bit = 0;
IF DB_NAME() <> @ExpectedDatabase AND @AllowOtherDatabase = 0
BEGIN
    DECLARE @msg nvarchar(4000) =
        N'Abort: connected to ' + QUOTENAME(DB_NAME())
        + N'. Expected ' + QUOTENAME(@ExpectedDatabase)
        + N'. Set @AllowOtherDatabase = 1 to override.';
    THROW 56101, @msg, 1;
END;

IF OBJECT_ID(N'dbo.ImportBatchMaster', N'U') IS NULL
BEGIN
    THROW 56110, N'Migration aborted: dbo.ImportBatchMaster does not exist.', 1;
END;

IF COL_LENGTH(N'dbo.ImportBatchMaster', N'MasterSyncCompleted') IS NULL
BEGIN
    ALTER TABLE dbo.ImportBatchMaster
    ADD MasterSyncCompleted BIT NOT NULL
        CONSTRAINT DF_ImportBatchMaster_MasterSyncCompleted DEFAULT (0);

    PRINT N'Added dbo.ImportBatchMaster.MasterSyncCompleted BIT NOT NULL DEFAULT 0.';
END
ELSE
BEGIN
    PRINT N'dbo.ImportBatchMaster.MasterSyncCompleted already exists — no change.';
END;

IF COL_LENGTH(N'dbo.ImportBatchMaster', N'MasterSyncDate') IS NULL
BEGIN
    ALTER TABLE dbo.ImportBatchMaster
    ADD MasterSyncDate DATETIME NULL;

    PRINT N'Added dbo.ImportBatchMaster.MasterSyncDate DATETIME NULL.';
END
ELSE
BEGIN
    PRINT N'dbo.ImportBatchMaster.MasterSyncDate already exists — no change.';
END;
GO
