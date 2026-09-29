/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 5 — TRN_RM_CLASS_1 reconciliation
   WHEN:        2026-09-25
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
   TARGET:      LIVE dbo.TRN_RM_CLASS_1 (AnmolDristi)
   KEY:         Business key = RMFID (PK). Acceptance is LIVE ⊇ UAT, not equal counts.
   FINDING:     2026-09-25 probe: UAT keys missing on LIVE = 0; LIVE-only = 179.
                Shared drift on RMF330 is an accepted production divergence (P1-T05A)
                and is excluded from MERGE. Ids match on shared keys.
   PRESERVE:    Material_Name (LIVE-only). Identity Id (never UPDATE). LIVE-only rows.
                LIVE operational values on RMF330.
   FORBIDDEN:   DELETE of LIVE-only rows, TRUNCATE, DROP/ADD of Material_Name, recreate,
                UPDATE/INSERT of RMF330.
   NOTE:        DO NOT EXECUTE until Production Gate. Default @DryRun = 1.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @DryRun BIT = 1;

DECLARE @RunTag      NVARCHAR(32) = CONVERT(CHAR(8), GETDATE(), 112)
                                 + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_TRN_RM_CLASS_1_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @Cols        NVARCHAR(MAX);
DECLARE @SrcCols     NVARCHAR(MAX);
DECLARE @TgtCols     NVARCHAR(MAX);
DECLARE @SetList     NVARCHAR(MAX);
DECLARE @LiveCnt     INT;
DECLARE @UatCnt      INT;
DECLARE @Missing     INT;
DECLARE @LiveOnly    INT;
DECLARE @Drift       INT;
DECLARE @Accepted    INT;
DECLARE @IdMismatch  INT;
DECLARE @Inserted    INT = 0;
DECLARE @Updated     INT = 0;

PRINT N'RC-018 P1-T05 TRN_RM_CLASS_1 | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1));

/* =========================================================================
   0) PREFLIGHT
   ========================================================================= */
IF OBJECT_ID(N'dbo.TRN_RM_CLASS_1', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.TRN_RM_CLASS_1 not found on LIVE. Abort.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'AnmolDristiDev.dbo.TRN_RM_CLASS_1', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: UAT AnmolDristiDev.dbo.TRN_RM_CLASS_1 not reachable. Abort.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'dbo.TRN_RM_CLASS_1', N'Material_Name') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: LIVE Material_Name is missing. Abort (retain rule).', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'dbo.TRN_RM_CLASS_1', N'RMFID') IS NULL
 OR COL_LENGTH(N'AnmolDristiDev.dbo.TRN_RM_CLASS_1', N'RMFID') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: business key RMFID missing. Abort.', 16, 1);
    RETURN;
END;

/* Shared, non-identity columns. Material_Name is excluded even if it appears later on UAT. */
SELECT @Cols = STUFF((
    SELECT N',' + QUOTENAME(c.name)
    FROM AnmolDristiDev.sys.columns c
    INNER JOIN AnmolDristi.sys.columns lc
        ON lc.object_id = OBJECT_ID(N'dbo.TRN_RM_CLASS_1')
       AND lc.name = c.name
    WHERE c.object_id = OBJECT_ID(N'AnmolDristiDev.dbo.TRN_RM_CLASS_1')
      AND c.is_computed = 0
      AND c.is_identity = 0
      AND c.name <> N'Material_Name'
    ORDER BY c.column_id
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');

IF @Cols IS NULL OR CHARINDEX(N'[RMFID]', @Cols) = 0
BEGIN
    RAISERROR(N'BLOCKER: shared column list could not be built. Abort.', 16, 1);
    RETURN;
END;

SELECT @SrcCols = STUFF((
    SELECT N',src.' + QUOTENAME(c.name)
    FROM AnmolDristiDev.sys.columns c
    INNER JOIN AnmolDristi.sys.columns lc
        ON lc.object_id = OBJECT_ID(N'dbo.TRN_RM_CLASS_1') AND lc.name = c.name
    WHERE c.object_id = OBJECT_ID(N'AnmolDristiDev.dbo.TRN_RM_CLASS_1')
      AND c.is_computed = 0 AND c.is_identity = 0 AND c.name <> N'Material_Name'
    ORDER BY c.column_id
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');

SELECT @TgtCols = REPLACE(@SrcCols, N'src.[', N'tgt.[');

SELECT @SetList = STUFF((
    SELECT N',tgt.' + QUOTENAME(c.name) + N' = src.' + QUOTENAME(c.name)
    FROM AnmolDristiDev.sys.columns c
    INNER JOIN AnmolDristi.sys.columns lc
        ON lc.object_id = OBJECT_ID(N'dbo.TRN_RM_CLASS_1') AND lc.name = c.name
    WHERE c.object_id = OBJECT_ID(N'AnmolDristiDev.dbo.TRN_RM_CLASS_1')
      AND c.is_computed = 0 AND c.is_identity = 0
      AND c.name NOT IN (N'Material_Name', N'RMFID')
    ORDER BY c.column_id
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');

SELECT @LiveCnt = COUNT(*) FROM dbo.TRN_RM_CLASS_1;
SELECT @UatCnt  = COUNT(*) FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1;

SELECT @Missing = COUNT(*)
FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1 u
WHERE NOT EXISTS (SELECT 1 FROM dbo.TRN_RM_CLASS_1 l WHERE l.RMFID = u.RMFID);

SELECT @LiveOnly = COUNT(*)
FROM dbo.TRN_RM_CLASS_1 l
WHERE NOT EXISTS (SELECT 1 FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1 u WHERE u.RMFID = l.RMFID);

SET @Sql = N'
SELECT @DriftOut = COUNT(*)
FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1 src
INNER JOIN dbo.TRN_RM_CLASS_1 tgt ON tgt.RMFID = src.RMFID
WHERE src.RMFID <> ''RMF330''
  AND EXISTS (SELECT ' + @SrcCols + N' EXCEPT SELECT ' + @TgtCols + N');';
EXEC sys.sp_executesql @Sql, N'@DriftOut INT OUTPUT', @DriftOut = @Drift OUTPUT;

SET @Sql = N'
SELECT @AccOut = COUNT(*)
FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1 src
INNER JOIN dbo.TRN_RM_CLASS_1 tgt ON tgt.RMFID = src.RMFID
WHERE src.RMFID = ''RMF330''
  AND EXISTS (SELECT ' + @SrcCols + N' EXCEPT SELECT ' + @TgtCols + N');';
EXEC sys.sp_executesql @Sql, N'@AccOut INT OUTPUT', @AccOut = @Accepted OUTPUT;

SELECT @IdMismatch = COUNT(*)
FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1 u
INNER JOIN dbo.TRN_RM_CLASS_1 l ON l.RMFID = u.RMFID
WHERE u.Id <> l.Id;

PRINT N'Counts | LIVE=' + CAST(@LiveCnt AS NVARCHAR(20))
    + N' UAT=' + CAST(@UatCnt AS NVARCHAR(20))
    + N' | acceptance LIVE>=UAT = '
    + CASE WHEN @LiveCnt >= @UatCnt THEN N'YES' ELSE N'NO' END;
PRINT N'Keys | MissingUatOnLive=' + CAST(@Missing AS NVARCHAR(20))
    + N' LiveOnly=' + CAST(@LiveOnly AS NVARCHAR(20))
    + N' ActionableDrift=' + CAST(@Drift AS NVARCHAR(20))
    + N' AcceptedRMF330=' + CAST(@Accepted AS NVARCHAR(20))
    + N' IdMismatch=' + CAST(@IdMismatch AS NVARCHAR(20));

IF @IdMismatch > 0
BEGIN
    RAISERROR(N'BLOCKER: identity Id differs for a shared RMFID. Do not rewrite identity. Abort.', 16, 1);
    RETURN;
END;

PRINT N'--- PREVIEW: UAT keys missing on LIVE (will INSERT; RMF330 excluded) ---';
SELECT u.Id, u.RMFID
FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1 u
WHERE u.RMFID <> 'RMF330'
  AND NOT EXISTS (SELECT 1 FROM dbo.TRN_RM_CLASS_1 l WHERE l.RMFID = u.RMFID);

PRINT N'--- PREVIEW: actionable shared drift (will UPDATE; RMF330 excluded) ---';
SET @Sql = N'
SELECT src.RMFID, src.Id
FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1 src
INNER JOIN dbo.TRN_RM_CLASS_1 tgt ON tgt.RMFID = src.RMFID
WHERE src.RMFID <> ''RMF330''
  AND EXISTS (SELECT ' + @SrcCols + N' EXCEPT SELECT ' + @TgtCols + N');';
EXEC sys.sp_executesql @Sql;

/* Accepted production divergence — reported, never merged */
PRINT N'--- ACCEPTED PRODUCTION DIVERGENCE: RMF330 ---';
IF EXISTS (SELECT 1 FROM dbo.TRN_RM_CLASS_1 WHERE RMFID = 'RMF330')
   AND @Accepted = 1
    PRINT N'RMF330 | LIVE present | shared attributes differ from UAT | ACCEPTED (excluded from MERGE)';
ELSE IF EXISTS (SELECT 1 FROM dbo.TRN_RM_CLASS_1 WHERE RMFID = 'RMF330')
    PRINT N'RMF330 | LIVE present | shared attributes match UAT | NOT ACCEPTED (divergence lost; still excluded from MERGE)';
ELSE
    PRINT N'RMF330 | MISSING ON LIVE | NOT ACCEPTED';

PRINT N'LIVE-only rows are ignored (not deleted). Material_Name is not in the UPDATE list. RMF330 is not in the MERGE source.';

IF @DryRun = 0
   AND NOT (
        EXISTS (SELECT 1 FROM dbo.TRN_RM_CLASS_1 WHERE RMFID = 'RMF330')
        AND @Accepted = 1
   )
BEGIN
    RAISERROR(N'BLOCKER: RMF330 accepted production divergence is not intact. Abort.', 16, 1);
    RETURN;
END;

IF @DryRun = 1
BEGIN
    PRINT N'DryRun=1 — backup and MERGE skipped. Set @DryRun=0 to apply.';
    PRINT N'Next: 020b_RC018_P1_T05_TRN_RM_CLASS_1_Validate.sql';
    RETURN;
END;

/* =========================================================================
   1) BACKUP then MERGE
   ========================================================================= */
IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
    RETURN;
END;

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.TRN_RM_CLASS_1 WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup created: ' + @BackupTable;

IF @Missing = 0 AND @Drift = 0
BEGIN
    PRINT N'No missing keys and no shared drift. MERGE skipped after backup.';
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID(N'tempdb..#ActionLog') IS NOT NULL DROP TABLE #ActionLog;
    CREATE TABLE #ActionLog
    (
        MergeAction NVARCHAR(10) NOT NULL,
        RMFID       VARCHAR(50)  NOT NULL
    );

    SET IDENTITY_INSERT dbo.TRN_RM_CLASS_1 ON;

    SET @Sql = N'
    MERGE dbo.TRN_RM_CLASS_1 AS tgt
    USING (
        SELECT *
        FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1
        WHERE RMFID <> ''RMF330''
    ) AS src
        ON tgt.RMFID = src.RMFID
    WHEN MATCHED AND EXISTS (
            SELECT ' + @SrcCols + N'
            EXCEPT
            SELECT ' + @TgtCols + N'
        )
        THEN UPDATE SET ' + @SetList + N'
    WHEN NOT MATCHED BY TARGET
        THEN INSERT (Id, ' + @Cols + N')
             VALUES (src.Id, ' + @SrcCols + N')
    OUTPUT $action, inserted.RMFID INTO #ActionLog (MergeAction, RMFID);';
    EXEC sys.sp_executesql @Sql;

    SET IDENTITY_INSERT dbo.TRN_RM_CLASS_1 OFF;

    SELECT @Inserted = COUNT(*) FROM #ActionLog WHERE MergeAction = N'INSERT';
    SELECT @Updated  = COUNT(*) FROM #ActionLog WHERE MergeAction = N'UPDATE';

    IF COL_LENGTH(N'dbo.TRN_RM_CLASS_1', N'Material_Name') IS NULL
        RAISERROR(N'BLOCKER: Material_Name disappeared. Rolling back.', 16, 1);

    DECLARE @LiveOnlyAfter INT = (
        SELECT COUNT(*)
        FROM dbo.TRN_RM_CLASS_1 l
        WHERE NOT EXISTS (
            SELECT 1 FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1 u WHERE u.RMFID = l.RMFID
        )
    );
    IF @LiveOnlyAfter < @LiveOnly
        RAISERROR(N'BLOCKER: LIVE-only row count dropped. Rolling back.', 16, 1);

    COMMIT TRAN;

    PRINT N'MERGE committed. Inserted=' + CAST(@Inserted AS NVARCHAR(10))
        + N' Updated=' + CAST(@Updated AS NVARCHAR(10));
    PRINT N'Backup for rollback: ' + @BackupTable;
    PRINT N'Next: 020b_RC018_P1_T05_TRN_RM_CLASS_1_Validate.sql';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    BEGIN TRY SET IDENTITY_INSERT dbo.TRN_RM_CLASS_1 OFF; END TRY BEGIN CATCH END CATCH;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P1-T05 FAILED: %s', 16, 1, @Err);
END CATCH;
GO
