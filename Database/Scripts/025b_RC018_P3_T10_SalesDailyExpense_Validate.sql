/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 10 — SalesDailyExpense VALIDATION
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.SalesDailyExpense (AnmolDristi)
   WHAT:        Read-only PASS/FAIL. No DML.
   NOTE:        Set @BackupTable to prove ClaimStatus and receipt columns
                still match the pre-DDL copy.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;
DECLARE @Floor INT = 30181;
DECLARE @BackupTable SYSNAME = NULL;
DECLARE @Sql NVARCHAR(MAX);

PRINT N'===== RC-018 P3-T10 SalesDailyExpense VALIDATION =====';

DECLARE @HasStatus BIT = CASE WHEN COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovalStatus') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @HasRemarks BIT = CASE WHEN COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovalRemarks') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @HasBy BIT = CASE WHEN COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovedBy') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @HasOn BIT = CASE WHEN COL_LENGTH(N'dbo.SalesDailyExpense', N'ApprovedOn') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @ColsOk BIT = CASE WHEN @HasStatus = 1 AND @HasRemarks = 1 AND @HasBy = 1 AND @HasOn = 1 THEN 1 ELSE 0 END;

PRINT N'CHECK Four columns | Status=' + CAST(@HasStatus AS NCHAR(1))
    + N' Remarks=' + CAST(@HasRemarks AS NCHAR(1))
    + N' By=' + CAST(@HasBy AS NCHAR(1))
    + N' On=' + CAST(@HasOn AS NCHAR(1))
    + N' | ' + CASE WHEN @ColsOk = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @ColsOk = 0 SET @Fail += 1;

DECLARE @Cnt INT = (SELECT COUNT(*) FROM dbo.SalesDailyExpense);
PRINT N'CHECK Row count preserved | Actual=' + CAST(@Cnt AS NVARCHAR(20))
    + N' Floor=' + CAST(@Floor AS NVARCHAR(20)) + N' | '
    + CASE WHEN @Cnt >= @Floor THEN N'PASS' ELSE N'FAIL' END;
IF @Cnt < @Floor SET @Fail += 1;

DECLARE @PkCols NVARCHAR(4000);
SELECT @PkCols = STUFF((
    SELECT N',' + c.name
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.SalesDailyExpense')
      AND kc.type = N'PK' AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');
PRINT N'CHECK PK | ' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'ExpenseId' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'ExpenseId' SET @Fail += 1;

DECLARE @Ident INT = COLUMNPROPERTY(OBJECT_ID(N'dbo.SalesDailyExpense'), N'ExpenseId', N'IsIdentity');
PRINT N'CHECK ExpenseId identity | ' + CASE WHEN @Ident = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @Ident <> 1 SET @Fail += 1;

DECLARE @Uq BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SalesDailyExpense')
      AND name = N'UQ_SalesDailyExpense_ActivityDateExpenseType_Seq'
) THEN 1 ELSE 0 END;
DECLARE @Fk BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.SalesDailyExpense')
      AND name = N'FK_SalesDailyExpense_Activity'
      AND is_disabled = 0
      AND is_not_trusted = 0
) THEN 1 ELSE 0 END;
PRINT N'CHECK UQ_SalesDailyExpense_ActivityDateExpenseType_Seq | '
    + CASE WHEN @Uq = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'CHECK FK_SalesDailyExpense_Activity trusted | '
    + CASE WHEN @Fk = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @Uq = 0 OR @Fk = 0 SET @Fail += 1;

DECLARE @ReceiptCols BIT = CASE
    WHEN COL_LENGTH(N'dbo.SalesDailyExpense', N'AttachmentPath') IS NOT NULL
     AND COL_LENGTH(N'dbo.SalesDailyExpense', N'AttachmentFileName') IS NOT NULL
     AND COL_LENGTH(N'dbo.SalesDailyExpense', N'AttachmentContent') IS NOT NULL
     AND COL_LENGTH(N'dbo.SalesDailyExpense', N'ClaimStatus') IS NOT NULL
    THEN 1 ELSE 0 END;
PRINT N'CHECK Receipt and ClaimStatus columns | '
    + CASE WHEN @ReceiptCols = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @ReceiptCols = 0 SET @Fail += 1;

IF @BackupTable IS NOT NULL AND OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    DECLARE @BakCnt INT;
    DECLARE @Drift INT;
    SET @Sql = N'SELECT @c = COUNT(*) FROM ' + @BackupTable;
    EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @BakCnt OUTPUT;
    PRINT N'CHECK Count equals backup | ' + CAST(@BakCnt AS NVARCHAR(20)) + N' | '
        + CASE WHEN @Cnt = @BakCnt THEN N'PASS' ELSE N'FAIL' END;
    IF @Cnt <> @BakCnt SET @Fail += 1;

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
    PRINT N'CHECK ClaimStatus and receipts match backup | drift=' + CAST(@Drift AS NVARCHAR(20)) + N' | '
        + CASE WHEN @Drift = 0 THEN N'PASS' ELSE N'FAIL' END;
    IF @Drift <> 0 SET @Fail += 1;
END
ELSE
    PRINT N'CHECK Receipt backup compare | skipped until @BackupTable is set';

PRINT N'--- DBCC CHECKCONSTRAINTS ---';
BEGIN TRY
    DBCC CHECKCONSTRAINTS (N'dbo.SalesDailyExpense') WITH NO_INFOMSGS;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
    SET @Fail += 1;
END CATCH;

PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'Four columns exist         | ' + CASE WHEN @ColsOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Indexes / FK remain        | ' + CASE WHEN @Uq = 1 AND @Fk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK unchanged               | ' + CASE WHEN ISNULL(@PkCols, N'') = N'ExpenseId' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'ExpenseId identity         | ' + CASE WHEN @Ident = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Row count preserved        | ' + CASE WHEN @Cnt >= @Floor THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Receipt columns present    | ' + CASE WHEN @ReceiptCols = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'DBCC CHECKCONSTRAINTS      | (see above)';
PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) — do not proceed to Table 11 =====';
GO
