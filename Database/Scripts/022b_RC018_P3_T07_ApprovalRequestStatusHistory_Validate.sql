/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 7 — ApprovalRequestStatusHistory VALIDATION
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.ApprovalRequestStatusHistory (AnmolDristi)
   WHAT:        Read-only PASS/FAIL. No DML.
   BASELINE:    Phase 0 LIVE count 4403 is a floor (new audit rows may arrive).
                Set @BackupTable to the patch backup to require an exact match.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;
DECLARE @BaselineFloor INT = 4403;
DECLARE @BackupTable SYSNAME = NULL; /* optional: RC018_Backup_ApprovalRequestStatusHistory_<RunTag> */
DECLARE @Sql NVARCHAR(MAX);

PRINT N'===== RC-018 P3-T07 ApprovalRequestStatusHistory VALIDATION =====';

DECLARE @HasDetail BIT = CASE WHEN COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApprovalRequestDetailId') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @HasActivity BIT = CASE WHEN COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ActivityId') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @HasLevel BIT = CASE WHEN COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApproverLevel') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @HasAction BIT = CASE WHEN COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ActionType') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @HasVersion BIT = CASE WHEN COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'DetailVersion') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @ColsOk BIT = CASE WHEN @HasDetail = 1 AND @HasActivity = 1 AND @HasLevel = 1 AND @HasAction = 1 AND @HasVersion = 1 THEN 1 ELSE 0 END;

PRINT N'CHECK Five columns | Detail=' + CAST(@HasDetail AS NCHAR(1))
    + N' Activity=' + CAST(@HasActivity AS NCHAR(1))
    + N' Level=' + CAST(@HasLevel AS NCHAR(1))
    + N' Action=' + CAST(@HasAction AS NCHAR(1))
    + N' Version=' + CAST(@HasVersion AS NCHAR(1))
    + N' | ' + CASE WHEN @ColsOk = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @ColsOk = 0 SET @Fail += 1;

DECLARE @Cnt INT = (SELECT COUNT(*) FROM dbo.ApprovalRequestStatusHistory);
PRINT N'CHECK Row count preserved | Actual=' + CAST(@Cnt AS NVARCHAR(20))
    + N' Floor=' + CAST(@BaselineFloor AS NVARCHAR(20)) + N' | '
    + CASE WHEN @Cnt >= @BaselineFloor THEN N'PASS' ELSE N'FAIL' END;
IF @Cnt < @BaselineFloor SET @Fail += 1;

IF @BackupTable IS NOT NULL AND OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    DECLARE @BakCnt INT;
    SET @Sql = N'SELECT @c = COUNT(*) FROM ' + @BackupTable;
    EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @BakCnt OUTPUT;
    PRINT N'CHECK Count equals backup | Backup=' + CAST(@BakCnt AS NVARCHAR(20)) + N' | '
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
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.ApprovalRequestStatusHistory')
      AND kc.type = N'PK' AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');
PRINT N'CHECK PK | Columns=' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'HistoryId' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'HistoryId' SET @Fail += 1;

DECLARE @ReqFk BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_ApprovalHistory_Request'
      AND parent_object_id = OBJECT_ID(N'dbo.ApprovalRequestStatusHistory')
      AND is_disabled = 0
      AND is_not_trusted = 0
) THEN 1 ELSE 0 END;
PRINT N'CHECK FK_ApprovalHistory_Request trusted | '
    + CASE WHEN @ReqFk = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @ReqFk = 0 SET @Fail += 1;

DECLARE @ReqOrphans INT = (
    SELECT COUNT(*)
    FROM dbo.ApprovalRequestStatusHistory h
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.SalesApprovalRequest r
        WHERE r.ApprovalRequestId = h.ApprovalRequestId
    )
);
PRINT N'CHECK ApprovalRequestId orphans | ' + CAST(@ReqOrphans AS NVARCHAR(20)) + N' | '
    + CASE WHEN @ReqOrphans = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @ReqOrphans <> 0 SET @Fail += 1;

DECLARE @DetailFk BIT = 0;
DECLARE @DetailTrusted BIT = 0;
DECLARE @DetailOrphans INT = 0;

IF @HasDetail = 1
BEGIN
    SELECT
          @DetailFk = 1
        , @DetailTrusted = CASE WHEN fk.is_disabled = 0 AND fk.is_not_trusted = 0 THEN 1 ELSE 0 END
    FROM sys.foreign_keys fk
    WHERE fk.name = N'FK_ApprovalHistory_Detail'
      AND fk.parent_object_id = OBJECT_ID(N'dbo.ApprovalRequestStatusHistory');

    IF @DetailFk = 0 SET @DetailTrusted = 0;

    SET @Sql = N'
        SELECT @c = COUNT(*)
        FROM dbo.ApprovalRequestStatusHistory h
        WHERE h.ApprovalRequestDetailId IS NOT NULL
          AND NOT EXISTS (
              SELECT 1 FROM dbo.SalesApprovalRequestDetail d
              WHERE d.ApprovalRequestDetailId = h.ApprovalRequestDetailId
          );';
    EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @DetailOrphans OUTPUT;

    IF @DetailOrphans = 0 AND @DetailFk = 1 AND @DetailTrusted = 1
        PRINT N'CHECK FK_ApprovalHistory_Detail | orphans=0 trusted WITH CHECK | PASS';
    ELSE IF @DetailOrphans > 0 AND @DetailFk = 0
    BEGIN
        PRINT N'CHECK FK_ApprovalHistory_Detail | NOT CREATED | orphans='
            + CAST(@DetailOrphans AS NVARCHAR(20)) + N' | FAIL';
        SET @Sql = N'
            SELECT h.HistoryId, h.ApprovalRequestId, h.ApprovalRequestDetailId
            FROM dbo.ApprovalRequestStatusHistory h
            WHERE h.ApprovalRequestDetailId IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.SalesApprovalRequestDetail d
                  WHERE d.ApprovalRequestDetailId = h.ApprovalRequestDetailId
              )
            ORDER BY h.HistoryId;';
        EXEC sys.sp_executesql @Sql;
        SET @Fail += 1;
    END
    ELSE
    BEGIN
        PRINT N'CHECK FK_ApprovalHistory_Detail | Exists=' + CAST(@DetailFk AS NCHAR(1))
            + N' Trusted=' + CAST(@DetailTrusted AS NCHAR(1))
            + N' Orphans=' + CAST(@DetailOrphans AS NVARCHAR(20)) + N' | FAIL';
        SET @Fail += 1;
    END
END
ELSE
BEGIN
    PRINT N'CHECK FK_ApprovalHistory_Detail | column absent, FK not evaluated | FAIL';
    SET @Fail += 1;
END;

DECLARE @Trig INT = (SELECT COUNT(*) FROM sys.triggers WHERE parent_id = OBJECT_ID(N'dbo.ApprovalRequestStatusHistory'));
PRINT N'CHECK Triggers (expect 0) | ' + CAST(@Trig AS NVARCHAR(10)) + N' | '
    + CASE WHEN @Trig = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Trig <> 0 SET @Fail += 1;

PRINT N'--- DBCC CHECKCONSTRAINTS ---';
BEGIN TRY
    DBCC CHECKCONSTRAINTS (N'dbo.ApprovalRequestStatusHistory') WITH NO_INFOMSGS;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
    SET @Fail += 1;
END CATCH;

PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'Five columns exist         | ' + CASE WHEN @ColsOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'FK detail                  | ' + CASE WHEN @DetailFk = 1 AND @DetailTrusted = 1 AND @DetailOrphans = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK unchanged               | ' + CASE WHEN ISNULL(@PkCols, N'') = N'HistoryId' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Row count preserved        | ' + CASE WHEN @Cnt >= @BaselineFloor THEN N'PASS' ELSE N'FAIL' END;
PRINT N'ApprovalRequestId orphans  | ' + CASE WHEN @ReqOrphans = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Triggers Match             | ' + CASE WHEN @Trig = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'DBCC CHECKCONSTRAINTS      | (see above)';
PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) — do not proceed to Table 8 =====';
GO
