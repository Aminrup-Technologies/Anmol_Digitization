/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 10 — SalesDailyExpense schema
   WHEN:        2026-09-25
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
                Database/Tables/SalesDailyExpense.sql
   TARGET:      LIVE dbo.SalesDailyExpense (AnmolDristi)
   WHAT:        Add four nullable approval columns if absent.
                Do not update ClaimStatus, attachments, or ExpenseId.
   FORBIDDEN:   DELETE, TRUNCATE, recreate, reseed, WITH updates of existing rows.
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
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_SalesDailyExpense_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @BeforeCnt   INT;
DECLARE @AfterCnt    INT;
DECLARE @IndexBefore INT;
DECLARE @IndexAfter  INT;
DECLARE @Drift       INT;

PRINT N'RC-018 P3-T10 SalesDailyExpense | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1));

IF OBJECT_ID(N'dbo.SalesDailyExpense', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.SalesDailyExpense not found. Abort.', 16, 1);
    RETURN;
END;

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.SalesDailyExpense'), N'ExpenseId', N'IsIdentity') <> 1
BEGIN
    RAISERROR(N'BLOCKER: ExpenseId identity missing. Abort. Identity will not be altered.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'dbo.SalesDailyExpense', N'AttachmentPath') IS NULL
 OR COL_LENGTH(N'dbo.SalesDailyExpense', N'AttachmentFileName') IS NULL
 OR COL_LENGTH(N'dbo.SalesDailyExpense', N'ClaimStatus') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: receipt or ClaimStatus column missing. Abort.', 16, 1);
    RETURN;
END;

SELECT @BeforeCnt = COUNT(*) FROM dbo.SalesDailyExpense;
SELECT @IndexBefore = COUNT(*)
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.SalesDailyExpense') AND index_id > 0;

PRINT N'Preflight rows=' + CAST(@BeforeCnt AS NVARCHAR(20))
    + N' indexes=' + CAST(@IndexBefore AS NVARCHAR(10))
    + N' (Phase 0 LIVE rows 30181).';

PRINT N'--- PLANNED DDL (nullable, no row rewrite) ---';
PRINT N'ADD ApprovalStatus NVARCHAR(50) NULL if absent';
PRINT N'ADD ApprovalRemarks NVARCHAR(1000) NULL if absent';
PRINT N'ADD ApprovedBy NVARCHAR(100) NULL if absent';
PRINT N'ADD ApprovedOn DATETIME2(7) NULL if absent';

IF COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovalStatus') IS NOT NULL
 AND COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovalRemarks') IS NOT NULL
 AND COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovedBy') IS NOT NULL
 AND COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovedOn') IS NOT NULL
BEGIN
    PRINT N'All four approval columns already exist. No DDL.';
    RETURN;
END;

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

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.SalesDailyExpense WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup created: ' + @BackupTable;

BEGIN TRY
    BEGIN TRAN;

    IF COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovalStatus') IS NULL
        ALTER TABLE dbo.SalesDailyExpense ADD ApprovalStatus NVARCHAR(50) NULL;

    IF COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovalRemarks') IS NULL
        ALTER TABLE dbo.SalesDailyExpense ADD ApprovalRemarks NVARCHAR(1000) NULL;

    IF COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovedBy') IS NULL
        ALTER TABLE dbo.SalesDailyExpense ADD ApprovedBy NVARCHAR(100) NULL;

    IF COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovedOn') IS NULL
        ALTER TABLE dbo.SalesDailyExpense ADD ApprovedOn DATETIME2(7) NULL;

    SELECT @AfterCnt = COUNT(*) FROM dbo.SalesDailyExpense;
    SELECT @IndexAfter = COUNT(*)
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SalesDailyExpense') AND index_id > 0;

    IF @AfterCnt <> @BeforeCnt
        RAISERROR(N'BLOCKER: row count changed %d -> %d.', 16, 1, @BeforeCnt, @AfterCnt);

    IF @IndexAfter <> @IndexBefore
        RAISERROR(N'BLOCKER: index count changed %d -> %d.', 16, 1, @IndexBefore, @IndexAfter);

    IF COLUMNPROPERTY(OBJECT_ID(N'dbo.SalesDailyExpense'), N'ExpenseId', N'IsIdentity') <> 1
        RAISERROR(N'BLOCKER: ExpenseId identity changed.', 16, 1);

    SET @Sql = N'
        SELECT @d = COUNT(*)
        FROM dbo.SalesDailyExpense e
        FULL OUTER JOIN ' + @BackupTable + N' b ON b.ExpenseId = e.ExpenseId
        WHERE e.ExpenseId IS NULL
           OR b.ExpenseId IS NULL
           OR ISNULL(e.ClaimStatus, N''~'') <> ISNULL(b.ClaimStatus, N''~'')
           OR ISNULL(e.AttachmentPath, N''~'') <> ISNULL(b.AttachmentPath, N''~'')
           OR ISNULL(e.AttachmentFileName, N''~'') <> ISNULL(b.AttachmentFileName, N''~'')
           OR ISNULL(DATALENGTH(e.AttachmentContent), -1) <> ISNULL(DATALENGTH(b.AttachmentContent), -1);';
    EXEC sys.sp_executesql @Sql, N'@d INT OUTPUT', @d = @Drift OUTPUT;

    IF @Drift <> 0
        RAISERROR(N'BLOCKER: ClaimStatus or receipt columns drifted (%d rows).', 16, 1, @Drift);

    COMMIT TRAN;

    PRINT N'Four nullable approval columns ensured. Existing expense rows were not updated.';
    PRINT N'Rows preserved=' + CAST(@AfterCnt AS NVARCHAR(20));
    PRINT N'Next: 025b_RC018_P3_T10_SalesDailyExpense_Validate.sql';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P3-T10 FAILED: %s', 16, 1, @Err);
END CATCH;
GO
