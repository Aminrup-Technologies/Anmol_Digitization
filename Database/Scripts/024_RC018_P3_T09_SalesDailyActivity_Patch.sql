/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 9 — SalesDailyActivity ClaimStatus check
   WHEN:        2026-09-25
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
                Database/Tables/SalesDailyActivity.sql
   TARGET:      LIVE dbo.SalesDailyActivity (AnmolDristi)
   WHAT:        Recreate CK_SalesDailyActivity_ClaimStatus so it also allows
                Returned. Other allowed statuses stay Draft, Submitted,
                Approved, Rejected. No row updates. No identity change.
   FORBIDDEN:   DELETE, TRUNCATE, recreate table, reseed, touch DetailVersion.
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
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_SalesDailyActivity_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @Def         NVARCHAR(MAX);
DECLARE @Norm        NVARCHAR(MAX);
DECLARE @BeforeCnt   INT;
DECLARE @AfterCnt    INT;
DECLARE @IndexBefore INT;
DECLARE @IndexAfter  INT;
DECLARE @Illegal     INT;

DECLARE @LiveNorm NVARCHAR(MAX) =
    N'([ClaimStatus]=''Rejected''OR[ClaimStatus]=''Approved''OR[ClaimStatus]=''Submitted''OR[ClaimStatus]=''Draft'')';
DECLARE @TargetNorm NVARCHAR(MAX) =
    N'([ClaimStatus]=''Rejected''OR[ClaimStatus]=''Approved''OR[ClaimStatus]=''Returned''OR[ClaimStatus]=''Submitted''OR[ClaimStatus]=''Draft'')';

PRINT N'RC-018 P3-T09 SalesDailyActivity | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1));

IF OBJECT_ID(N'dbo.SalesDailyActivity', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.SalesDailyActivity not found. Abort.', 16, 1);
    RETURN;
END;

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.SalesDailyActivity'), N'ActivityId', N'IsIdentity') <> 1
BEGIN
    RAISERROR(N'BLOCKER: ActivityId identity missing. Abort. Identity will not be altered.', 16, 1);
    RETURN;
END;

SELECT @BeforeCnt = COUNT(*) FROM dbo.SalesDailyActivity;
SELECT @IndexBefore = COUNT(*)
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivity') AND index_id > 0;

PRINT N'Preflight rows=' + CAST(@BeforeCnt AS NVARCHAR(20))
    + N' indexes=' + CAST(@IndexBefore AS NVARCHAR(10))
    + N' (Phase 0 LIVE rows 87402).';

SELECT @Def = definition
FROM sys.check_constraints
WHERE name = N'CK_SalesDailyActivity_ClaimStatus'
  AND parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivity');

IF @Def IS NULL
BEGIN
    RAISERROR(N'BLOCKER: CK_SalesDailyActivity_ClaimStatus not found. This package will not create a new constraint family. Abort.', 16, 1);
    RETURN;
END;

SET @Norm = REPLACE(REPLACE(REPLACE(REPLACE(@Def, N' ', N''), CHAR(13), N''), CHAR(10), N''), CHAR(9), N'');
/* Catalog text may persist nvarchar literals as N'...' or '...'. */
SET @Norm = REPLACE(@Norm, N'=N''', N'=''');

PRINT N'Captured current check definition:';
PRINT @Def;

IF @Norm = @TargetNorm
BEGIN
    PRINT N'Constraint already allows Returned plus Draft, Submitted, Approved, Rejected. No DDL.';
    RETURN;
END;

IF @Norm <> @LiveNorm
BEGIN
    RAISERROR(N'BLOCKER: check definition is neither the Phase 0 LIVE form nor the target form. Not dropped, so an unknown status is not removed.', 16, 1);
    RETURN;
END;

SELECT @Illegal = COUNT(*)
FROM dbo.SalesDailyActivity
WHERE ClaimStatus IS NOT NULL
  AND ClaimStatus NOT IN (N'Draft', N'Submitted', N'Approved', N'Rejected', N'Returned');

PRINT N'Rows outside the target status set=' + CAST(@Illegal AS NVARCHAR(20));

IF @Illegal > 0
BEGIN
    SELECT ClaimStatus, COUNT(*) AS [RowCount]
    FROM dbo.SalesDailyActivity
    WHERE ClaimStatus IS NOT NULL
      AND ClaimStatus NOT IN (N'Draft', N'Submitted', N'Approved', N'Rejected', N'Returned')
    GROUP BY ClaimStatus
    ORDER BY ClaimStatus;

    RAISERROR(N'BLOCKER: existing rows would violate the recreated check. Constraint not dropped.', 16, 1);
    RETURN;
END;

PRINT N'Target check keeps Draft, Submitted, Approved, Rejected and adds Returned.';

IF @DryRun = 1
BEGIN
    PRINT N'DryRun=1 — backup and DDL skipped.';
    RETURN;
END;

IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
    RETURN;
END;

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.SalesDailyActivity WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup created: ' + @BackupTable;

BEGIN TRY
    BEGIN TRAN;

    ALTER TABLE dbo.SalesDailyActivity DROP CONSTRAINT CK_SalesDailyActivity_ClaimStatus;

    ALTER TABLE dbo.SalesDailyActivity WITH CHECK
        ADD CONSTRAINT CK_SalesDailyActivity_ClaimStatus
        CHECK ([ClaimStatus]='Rejected' OR [ClaimStatus]='Approved' OR [ClaimStatus]='Returned' OR [ClaimStatus]='Submitted' OR [ClaimStatus]='Draft');

    SELECT @AfterCnt = COUNT(*) FROM dbo.SalesDailyActivity;
    SELECT @IndexAfter = COUNT(*)
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivity') AND index_id > 0;

    IF @AfterCnt <> @BeforeCnt
        RAISERROR(N'BLOCKER: row count changed %d -> %d.', 16, 1, @BeforeCnt, @AfterCnt);

    IF @IndexAfter <> @IndexBefore
        RAISERROR(N'BLOCKER: index count changed %d -> %d.', 16, 1, @IndexBefore, @IndexAfter);

    IF COLUMNPROPERTY(OBJECT_ID(N'dbo.SalesDailyActivity'), N'ActivityId', N'IsIdentity') <> 1
        RAISERROR(N'BLOCKER: ActivityId identity changed.', 16, 1);

    COMMIT TRAN;

    PRINT N'CK_SalesDailyActivity_ClaimStatus recreated WITH CHECK. Returned is allowed.';
    PRINT N'Rows preserved=' + CAST(@AfterCnt AS NVARCHAR(20));
    PRINT N'Next: 024b_RC018_P3_T09_SalesDailyActivity_Validate.sql';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P3-T09 FAILED: %s', 16, 1, @Err);
END CATCH;
GO
