/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 3 — MST_EmployeeTolerance schema + MERGE
   WHEN:        2026-09-24
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
   TARGET:      LIVE dbo.MST_EmployeeTolerance (AnmolDristi)
   AUTHORITY:   UAT AnmolDristiDev seed rows (ToleranceId 1..3)
   WHAT:        1) ADD MaxKmModificationCount + DF + CK if missing
                2) Backup then MERGE three seed rows (no DELETE)
   PRESERVE:    Identity column; never reseed downward; never DELETE LIVE rows
   NOTE:        DO NOT EXECUTE until Production Gate. Default @DryRun = 1.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @DryRun BIT = 1;  /* 1 = preview; 0 = apply */

DECLARE @RunTag      NVARCHAR(32) = CONVERT(CHAR(8), GETDATE(), 112)
                                 + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_MST_EmployeeTolerance_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @Inserted    INT = 0;
DECLARE @Updated     INT = 0;

PRINT N'RC-018 P1-T03 MST_EmployeeTolerance | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1));

/* =========================================================================
   0) PREFLIGHT
   ========================================================================= */
IF OBJECT_ID(N'dbo.MST_EmployeeTolerance', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.MST_EmployeeTolerance not found. Abort.', 16, 1);
    RETURN;
END;

DECLARE @LiveCnt INT = (SELECT COUNT(*) FROM dbo.MST_EmployeeTolerance);
PRINT N'Preflight LIVE row count=' + CAST(@LiveCnt AS NVARCHAR(20))
    + N' (Phase 0 expected 0)';

/* =========================================================================
   1) SCHEMA — MaxKmModificationCount + default + check
   ========================================================================= */
PRINT N'--- SCHEMA ---';

IF COL_LENGTH(N'dbo.MST_EmployeeTolerance', N'MaxKmModificationCount') IS NULL
BEGIN
    PRINT N'MaxKmModificationCount missing — will ADD INT NOT NULL DF(1).';
    IF @DryRun = 0
    BEGIN
        /* Empty or existing rows get DEFAULT (1); safe on LIVE (0 rows). */
        ALTER TABLE dbo.MST_EmployeeTolerance
            ADD MaxKmModificationCount INT NOT NULL
                CONSTRAINT DF_MST_EmployeeTolerance_MaxKmModificationCount DEFAULT (1);
        PRINT N'ADD COLUMN MaxKmModificationCount completed.';
    END
    ELSE
        PRINT N'DryRun=1 — ADD COLUMN skipped.';
END
ELSE
    PRINT N'MaxKmModificationCount already present — no column DDL.';

/* Ensure named default exists (idempotent) */
IF COL_LENGTH(N'dbo.MST_EmployeeTolerance', N'MaxKmModificationCount') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.default_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.MST_EmployeeTolerance')
          AND name = N'DF_MST_EmployeeTolerance_MaxKmModificationCount'
   )
BEGIN
    PRINT N'Default DF_MST_EmployeeTolerance_MaxKmModificationCount missing — will ADD.';
    IF @DryRun = 0
    BEGIN
        /* Dynamic: column may be absent when this batch is compiled. */
        SET @Sql = N'
            ALTER TABLE dbo.MST_EmployeeTolerance
            ADD CONSTRAINT DF_MST_EmployeeTolerance_MaxKmModificationCount
            DEFAULT (1) FOR MaxKmModificationCount;';
        EXEC sys.sp_executesql @Sql;
        PRINT N'ADD DEFAULT completed.';
    END
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.MST_EmployeeTolerance')
      AND name = N'CK_MST_EmployeeTolerance_MaxKmModificationCount'
)
BEGIN
    PRINT N'Check CK_MST_EmployeeTolerance_MaxKmModificationCount missing — will ADD.';
    IF @DryRun = 0
    BEGIN
        /* Dynamic: column may have been added earlier in this batch */
        SET @Sql = N'
            ALTER TABLE dbo.MST_EmployeeTolerance WITH CHECK
            ADD CONSTRAINT CK_MST_EmployeeTolerance_MaxKmModificationCount
            CHECK (MaxKmModificationCount >= (1));';
        EXEC sys.sp_executesql @Sql;
        PRINT N'ADD CHECK completed.';
    END
    ELSE
        PRINT N'DryRun=1 — ADD CHECK skipped.';
END
ELSE
    PRINT N'Check constraint already present.';

/* =========================================================================
   2) UAT SEED SOURCE (Phase 0 / live UAT evidence 2026-09-24)
   ========================================================================= */
IF OBJECT_ID(N'tempdb..#UatEmpTol') IS NOT NULL DROP TABLE #UatEmpTol;

CREATE TABLE #UatEmpTol
(
    ToleranceId            INT            NOT NULL PRIMARY KEY,
    EmployeeCode           NVARCHAR(100)  NOT NULL,
    MinAllowedKM           DECIMAL(10,2)  NULL,
    MaxAllowedKM           DECIMAL(10,2)  NULL,
    EffectiveFrom          DATE           NULL,
    EffectiveTo            DATE           NULL,
    CreatedBy              NVARCHAR(100)  NULL,
    CreatedDateTime        DATETIME       NOT NULL,
    ModifiedBy             NVARCHAR(100)  NULL,
    ModifiedDateTime       DATETIME       NULL,
    IsActive               BIT            NOT NULL,
    MaxKmModificationCount INT            NOT NULL
);

INSERT INTO #UatEmpTol
(
    ToleranceId, EmployeeCode, MinAllowedKM, MaxAllowedKM,
    EffectiveFrom, EffectiveTo, CreatedBy, CreatedDateTime,
    ModifiedBy, ModifiedDateTime, IsActive, MaxKmModificationCount
)
VALUES
 (1, N'AHS1562', 0.00, 100.00, '2026-04-01', '2027-03-31',
  N'AHO445', '2026-07-31T13:39:43.220', N'AHO445', '2026-07-31T14:57:45.583', 1, 2),
 (2, N'M1367',  10.00,  50.00, '2026-04-01', '2027-03-31',
  N'AHO539', '2026-08-04T18:16:25.967', N'AHO539', '2026-08-04T18:17:02.290', 1, 1),
 (3, N'AHS1530', 0.00,  30.00, '2026-08-01', '2026-12-31',
  N'AHO539', '2026-09-22T11:29:32.167', N'AHO539', '2026-09-22T11:31:14.423', 1, 1);

PRINT N'UAT seed loaded: 3 rows.';

PRINT N'--- PREVIEW: would INSERT ---';
SELECT u.*
FROM #UatEmpTol u
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.MST_EmployeeTolerance l WHERE l.ToleranceId = u.ToleranceId
);

PRINT N'--- PREVIEW: would UPDATE (shared attr drift) ---';
IF COL_LENGTH(N'dbo.MST_EmployeeTolerance', N'MaxKmModificationCount') IS NOT NULL
BEGIN
    SET @Sql = N'
        SELECT
              l.ToleranceId
            , l.EmployeeCode AS LIVE_EmployeeCode, u.EmployeeCode AS UAT_EmployeeCode
            , l.MinAllowedKM AS LIVE_Min, u.MinAllowedKM AS UAT_Min
            , l.MaxAllowedKM AS LIVE_Max, u.MaxAllowedKM AS UAT_Max
            , l.EffectiveFrom AS LIVE_From, u.EffectiveFrom AS UAT_From
            , l.EffectiveTo AS LIVE_To, u.EffectiveTo AS UAT_To
            , l.IsActive AS LIVE_Active, u.IsActive AS UAT_Active
            , l.MaxKmModificationCount AS LIVE_MaxKmMod, u.MaxKmModificationCount AS UAT_MaxKmMod
        FROM dbo.MST_EmployeeTolerance l
        INNER JOIN #UatEmpTol u ON u.ToleranceId = l.ToleranceId
        WHERE ISNULL(l.EmployeeCode, N'''') <> ISNULL(u.EmployeeCode, N'''')
           OR ISNULL(l.MinAllowedKM, -1) <> ISNULL(u.MinAllowedKM, -1)
           OR ISNULL(l.MaxAllowedKM, -1) <> ISNULL(u.MaxAllowedKM, -1)
           OR ISNULL(l.EffectiveFrom, ''19000101'') <> ISNULL(u.EffectiveFrom, ''19000101'')
           OR ISNULL(l.EffectiveTo, ''19000101'') <> ISNULL(u.EffectiveTo, ''19000101'')
           OR ISNULL(l.CreatedBy, N'''') <> ISNULL(u.CreatedBy, N'''')
           OR ISNULL(l.ModifiedBy, N'''') <> ISNULL(u.ModifiedBy, N'''')
           OR ISNULL(l.IsActive, 0) <> ISNULL(u.IsActive, 0)
           OR ISNULL(l.MaxKmModificationCount, -1) <> ISNULL(u.MaxKmModificationCount, -1);';
    EXEC sys.sp_executesql @Sql;
END
ELSE
    PRINT N'(Column MaxKmModificationCount absent in dry-run — UPDATE preview deferred until after ADD.)';

PRINT N'--- PREVIEW: LIVE-only rows (RETAINED; no DELETE) ---';
SELECT l.ToleranceId, l.EmployeeCode
FROM dbo.MST_EmployeeTolerance l
WHERE NOT EXISTS (SELECT 1 FROM #UatEmpTol u WHERE u.ToleranceId = l.ToleranceId);

IF @DryRun = 1
BEGIN
    PRINT N'DryRun=1 — backup + MERGE skipped. Set @DryRun=0 to apply.';
    RETURN;
END;

/* =========================================================================
   3) BACKUP then MERGE
   ========================================================================= */
IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'dbo.MST_EmployeeTolerance', N'MaxKmModificationCount') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: MaxKmModificationCount still missing after schema step. Abort.', 16, 1);
    RETURN;
END;

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.MST_EmployeeTolerance WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup created: ' + @BackupTable;

/* MERGE via dynamic SQL so MaxKmModificationCount binds after ADD in this batch */
IF OBJECT_ID(N'tempdb..#ActionLog') IS NOT NULL DROP TABLE #ActionLog;
CREATE TABLE #ActionLog
(
    MergeAction NVARCHAR(10) NOT NULL,
    ToleranceId INT          NOT NULL
);

BEGIN TRY
    BEGIN TRAN;

    SET IDENTITY_INSERT dbo.MST_EmployeeTolerance ON;

    SET @Sql = N'
    MERGE dbo.MST_EmployeeTolerance AS tgt
    USING #UatEmpTol AS src
        ON tgt.ToleranceId = src.ToleranceId
    WHEN MATCHED AND (
            ISNULL(tgt.EmployeeCode, N'''') <> ISNULL(src.EmployeeCode, N'''')
         OR ISNULL(tgt.MinAllowedKM, -1) <> ISNULL(src.MinAllowedKM, -1)
         OR ISNULL(tgt.MaxAllowedKM, -1) <> ISNULL(src.MaxAllowedKM, -1)
         OR ISNULL(tgt.EffectiveFrom, ''19000101'') <> ISNULL(src.EffectiveFrom, ''19000101'')
         OR ISNULL(tgt.EffectiveTo, ''19000101'') <> ISNULL(src.EffectiveTo, ''19000101'')
         OR ISNULL(tgt.CreatedBy, N'''') <> ISNULL(src.CreatedBy, N'''')
         OR ISNULL(CONVERT(VARCHAR(30), tgt.CreatedDateTime, 121), N'''')
            <> ISNULL(CONVERT(VARCHAR(30), src.CreatedDateTime, 121), N'''')
         OR ISNULL(tgt.ModifiedBy, N'''') <> ISNULL(src.ModifiedBy, N'''')
         OR ISNULL(CONVERT(VARCHAR(30), tgt.ModifiedDateTime, 121), N'''')
            <> ISNULL(CONVERT(VARCHAR(30), src.ModifiedDateTime, 121), N'''')
         OR ISNULL(tgt.IsActive, 0) <> ISNULL(src.IsActive, 0)
         OR ISNULL(tgt.MaxKmModificationCount, -1) <> ISNULL(src.MaxKmModificationCount, -1)
        )
        THEN UPDATE SET
              tgt.EmployeeCode           = src.EmployeeCode
            , tgt.MinAllowedKM           = src.MinAllowedKM
            , tgt.MaxAllowedKM           = src.MaxAllowedKM
            , tgt.EffectiveFrom          = src.EffectiveFrom
            , tgt.EffectiveTo            = src.EffectiveTo
            , tgt.CreatedBy              = src.CreatedBy
            , tgt.CreatedDateTime        = src.CreatedDateTime
            , tgt.ModifiedBy             = src.ModifiedBy
            , tgt.ModifiedDateTime       = src.ModifiedDateTime
            , tgt.IsActive               = src.IsActive
            , tgt.MaxKmModificationCount = src.MaxKmModificationCount
    WHEN NOT MATCHED BY TARGET
        THEN INSERT
        (
            ToleranceId, EmployeeCode, MinAllowedKM, MaxAllowedKM,
            EffectiveFrom, EffectiveTo, CreatedBy, CreatedDateTime,
            ModifiedBy, ModifiedDateTime, IsActive, MaxKmModificationCount
        )
        VALUES
        (
            src.ToleranceId, src.EmployeeCode, src.MinAllowedKM, src.MaxAllowedKM,
            src.EffectiveFrom, src.EffectiveTo, src.CreatedBy, src.CreatedDateTime,
            src.ModifiedBy, src.ModifiedDateTime, src.IsActive, src.MaxKmModificationCount
        )
    OUTPUT $action, inserted.ToleranceId INTO #ActionLog (MergeAction, ToleranceId);
    ';
    EXEC sys.sp_executesql @Sql;

    SET IDENTITY_INSERT dbo.MST_EmployeeTolerance OFF;

    SELECT @Inserted = COUNT(*) FROM #ActionLog WHERE MergeAction = N'INSERT';
    SELECT @Updated  = COUNT(*) FROM #ActionLog WHERE MergeAction = N'UPDATE';

    COMMIT TRAN;

    PRINT N'MERGE committed. Inserted=' + CAST(@Inserted AS NVARCHAR(10))
        + N' Updated=' + CAST(@Updated AS NVARCHAR(10));
    PRINT N'Identity preserved via IDENTITY_INSERT; no DBCC CHECKIDENT reseed.';
    PRINT N'Backup for rollback: ' + @BackupTable;
    PRINT N'Next: run 018b_RC018_P1_T03_MST_EmployeeTolerance_Validate.sql';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    BEGIN TRY SET IDENTITY_INSERT dbo.MST_EmployeeTolerance OFF; END TRY BEGIN CATCH END CATCH;

    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P1-T03 FAILED: %s', 16, 1, @Err);
END CATCH;
GO
