/*
    Object:      ImportBatchMaster.MasterSyncCompleted / MasterSyncDate
    Type:        SCHEMA MIGRATION
    Phase:       Phase A — HRMS workflow isolation
    Purpose:     Persist a durable, session-independent indication that
                 master synchronization succeeded for a specific batch.
                 Employee insert/update/hierarchy actions require this flag.

    Why a new column (not Status alone):
      ImportBatchMaster.Status is overwritten to Processing / Processed /
      Failed by later employee actions. A Status value of 'MastersSynced'
      would be lost after the first employee action. The BIT flag survives
      page refresh, session expiry, and subsequent employee processing.

    Backward compatible:
      - Columns are additive
      - Existing rows default MasterSyncCompleted = 0 (must re-run masters)
      - Idempotent (safe to re-run)
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.ImportBatchMaster', N'U') IS NULL
BEGIN
    RAISERROR(N'Migration aborted: dbo.ImportBatchMaster does not exist.', 16, 1);
    RETURN;
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
