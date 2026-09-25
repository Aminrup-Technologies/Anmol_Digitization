/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 9 — SalesDailyActivity VALIDATION
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.SalesDailyActivity (AnmolDristi)
   WHAT:        Read-only PASS/FAIL. No DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;
DECLARE @Floor INT = 87402;
DECLARE @BackupTable SYSNAME = NULL; /* optional exact-count check */
DECLARE @Sql NVARCHAR(MAX);
DECLARE @Def NVARCHAR(MAX);
DECLARE @Norm NVARCHAR(MAX);
DECLARE @TargetNorm NVARCHAR(MAX) =
    N'([ClaimStatus]=''Rejected''OR[ClaimStatus]=''Approved''OR[ClaimStatus]=''Returned''OR[ClaimStatus]=''Submitted''OR[ClaimStatus]=''Draft'')';

PRINT N'===== RC-018 P3-T09 SalesDailyActivity VALIDATION =====';

DECLARE @Cnt INT = (SELECT COUNT(*) FROM dbo.SalesDailyActivity);
PRINT N'CHECK Row count preserved | Actual=' + CAST(@Cnt AS NVARCHAR(20))
    + N' Floor=' + CAST(@Floor AS NVARCHAR(20)) + N' | '
    + CASE WHEN @Cnt >= @Floor THEN N'PASS' ELSE N'FAIL' END;
IF @Cnt < @Floor SET @Fail += 1;

IF @BackupTable IS NOT NULL AND OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    DECLARE @BakCnt INT;
    SET @Sql = N'SELECT @c = COUNT(*) FROM ' + @BackupTable;
    EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @BakCnt OUTPUT;
    PRINT N'CHECK Count equals backup | ' + CAST(@BakCnt AS NVARCHAR(20)) + N' | '
        + CASE WHEN @Cnt = @BakCnt THEN N'PASS' ELSE N'FAIL' END;
    IF @Cnt <> @BakCnt SET @Fail += 1;
END;

DECLARE @PkCols NVARCHAR(4000);
SELECT @PkCols = STUFF((
    SELECT N',' + c.name
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivity')
      AND kc.type = N'PK' AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');
PRINT N'CHECK PK | ' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'ActivityId' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'ActivityId' SET @Fail += 1;

DECLARE @Ident INT = COLUMNPROPERTY(OBJECT_ID(N'dbo.SalesDailyActivity'), N'ActivityId', N'IsIdentity');
PRINT N'CHECK ActivityId identity | ' + CASE WHEN @Ident = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @Ident <> 1 SET @Fail += 1;

DECLARE @Uq BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivity')
      AND name = N'UQ_SalesDailyActivity_UserDate'
) THEN 1 ELSE 0 END;
PRINT N'CHECK UQ_SalesDailyActivity_UserDate | ' + CASE WHEN @Uq = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @Uq = 0 SET @Fail += 1;

SELECT @Def = definition
FROM sys.check_constraints
WHERE name = N'CK_SalesDailyActivity_ClaimStatus'
  AND parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivity');

SET @Norm = REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(@Def, N''), N' ', N''), CHAR(13), N''), CHAR(10), N''), CHAR(9), N'');

DECLARE @HasReturned BIT = CASE WHEN @Norm LIKE N'%''Returned''%' THEN 1 ELSE 0 END;
DECLARE @HasDraft BIT = CASE WHEN @Norm LIKE N'%''Draft''%' THEN 1 ELSE 0 END;
DECLARE @HasSubmitted BIT = CASE WHEN @Norm LIKE N'%''Submitted''%' THEN 1 ELSE 0 END;
DECLARE @HasApproved BIT = CASE WHEN @Norm LIKE N'%''Approved''%' THEN 1 ELSE 0 END;
DECLARE @HasRejected BIT = CASE WHEN @Norm LIKE N'%''Rejected''%' THEN 1 ELSE 0 END;
DECLARE @Exact BIT = CASE WHEN @Norm = @TargetNorm THEN 1 ELSE 0 END;

PRINT N'CHECK Returned accepted | ' + CASE WHEN @HasReturned = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'CHECK Draft/Submitted/Approved/Rejected retained | '
    + CASE WHEN @HasDraft = 1 AND @HasSubmitted = 1 AND @HasApproved = 1 AND @HasRejected = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'CHECK Exact target definition | ' + CASE WHEN @Exact = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @HasReturned = 0 OR @HasDraft = 0 OR @HasSubmitted = 0 OR @HasApproved = 0 OR @HasRejected = 0 OR @Exact = 0
    SET @Fail += 1;

DECLARE @Bad INT = (
    SELECT COUNT(*)
    FROM dbo.SalesDailyActivity
    WHERE ClaimStatus IS NOT NULL
      AND ClaimStatus NOT IN (N'Draft', N'Submitted', N'Approved', N'Rejected', N'Returned')
);
PRINT N'CHECK Rows violating target set | ' + CAST(@Bad AS NVARCHAR(20)) + N' | '
    + CASE WHEN @Bad = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Bad <> 0 SET @Fail += 1;

PRINT N'--- DBCC CHECKCONSTRAINTS ---';
BEGIN TRY
    DBCC CHECKCONSTRAINTS (N'dbo.SalesDailyActivity') WITH NO_INFOMSGS;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
    SET @Fail += 1;
END CATCH;

PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'Returned accepted           | ' + CASE WHEN @HasReturned = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Existing statuses retained  | ' + CASE WHEN @HasDraft = 1 AND @HasSubmitted = 1 AND @HasApproved = 1 AND @HasRejected = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Row count preserved         | ' + CASE WHEN @Cnt >= @Floor THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK unchanged                | ' + CASE WHEN ISNULL(@PkCols, N'') = N'ActivityId' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Index UQ_UserDate remains   | ' + CASE WHEN @Uq = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'No violating rows           | ' + CASE WHEN @Bad = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'DBCC CHECKCONSTRAINTS       | (see above)';
PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) — do not proceed to Table 10 =====';
GO
