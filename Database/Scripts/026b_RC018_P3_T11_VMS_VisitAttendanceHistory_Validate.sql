/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 11 — VMS_VisitAttendanceHistory VALIDATION
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.VMS_VisitAttendanceHistory (AnmolDristi)
   WHAT:        Read-only PASS/FAIL. No DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;
DECLARE @Floor INT = 4;
DECLARE @BackupTable SYSNAME = NULL;
DECLARE @Sql NVARCHAR(MAX);

PRINT N'===== RC-018 P3-T11 VMS_VisitAttendanceHistory VALIDATION =====';

DECLARE @Cnt INT = (SELECT COUNT(*) FROM dbo.VMS_VisitAttendanceHistory);
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
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.VMS_VisitAttendanceHistory')
      AND kc.type = N'PK' AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');
PRINT N'CHECK PK | ' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'HistoryID' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'HistoryID' SET @Fail += 1;

DECLARE @DayIx BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.VMS_VisitAttendanceHistory')
      AND name = N'IX_VMS_VisitAttendanceHistory_DayLogID'
) THEN 1 ELSE 0 END;
DECLARE @LogIx BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.VMS_VisitAttendanceHistory')
      AND name = N'IX_VMS_VisitAttendanceHistory_LogID'
) THEN 1 ELSE 0 END;
DECLARE @UqIx BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.VMS_VisitAttendanceHistory')
      AND name = N'UQ_VMS_VisitAttendanceHistory_OpenDayLog'
      AND is_unique = 1
      AND has_filter = 1
) THEN 1 ELSE 0 END;

PRINT N'CHECK IX_VMS_VisitAttendanceHistory_DayLogID | ' + CASE WHEN @DayIx = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'CHECK IX_VMS_VisitAttendanceHistory_LogID | ' + CASE WHEN @LogIx = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @DayIx = 0 OR @LogIx = 0 SET @Fail += 1;

DECLARE @DupGroups INT = (
    SELECT COUNT(*)
    FROM (
        SELECT DayLogID
        FROM dbo.VMS_VisitAttendanceHistory
        WHERE CheckOutTime IS NULL
        GROUP BY DayLogID
        HAVING COUNT(*) > 1
    ) d
);

IF @UqIx = 1 AND @DupGroups = 0
    PRINT N'CHECK UQ_VMS_VisitAttendanceHistory_OpenDayLog | present, open duplicates=0 | PASS';
ELSE IF @UqIx = 0 AND @DupGroups > 0
BEGIN
    PRINT N'CHECK UQ_VMS_VisitAttendanceHistory_OpenDayLog | SKIPPED because open DayLogID duplicates='
        + CAST(@DupGroups AS NVARCHAR(20)) + N' | FAIL';
    SELECT h.HistoryID, h.LogID, h.DayLogID, h.CheckInTime, h.CheckOutTime, h.Source
    FROM dbo.VMS_VisitAttendanceHistory h
    INNER JOIN (
        SELECT DayLogID
        FROM dbo.VMS_VisitAttendanceHistory
        WHERE CheckOutTime IS NULL
        GROUP BY DayLogID
        HAVING COUNT(*) > 1
    ) d ON d.DayLogID = h.DayLogID
    WHERE h.CheckOutTime IS NULL
    ORDER BY h.DayLogID, h.HistoryID;
    SET @Fail += 1;
END
ELSE
BEGIN
    PRINT N'CHECK UQ_VMS_VisitAttendanceHistory_OpenDayLog | Exists=' + CAST(@UqIx AS NCHAR(1))
        + N' OpenDupGroups=' + CAST(@DupGroups AS NVARCHAR(20)) + N' | FAIL';
    SET @Fail += 1;
END;

PRINT N'CHECK No duplicate open DayLogID | ' + CAST(@DupGroups AS NVARCHAR(20)) + N' | '
    + CASE WHEN @DupGroups = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @DupGroups <> 0 SET @Fail += 1;

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
        FROM dbo.VMS_VisitAttendanceHistory h
        FULL OUTER JOIN ' + @BackupTable + N' b ON b.HistoryID = h.HistoryID
        WHERE h.HistoryID IS NULL
           OR b.HistoryID IS NULL
           OR ISNULL(h.LogID, -1) <> ISNULL(b.LogID, -1)
           OR ISNULL(h.DayLogID, -1) <> ISNULL(b.DayLogID, -1)
           OR ISNULL(h.CheckInTime, ''19000101'') <> ISNULL(b.CheckInTime, ''19000101'')
           OR ISNULL(h.CheckOutTime, ''19000101'') <> ISNULL(b.CheckOutTime, ''19000101'')
           OR ISNULL(h.Source, '''') <> ISNULL(b.Source, '''')
           OR ISNULL(h.Actor, '''') <> ISNULL(b.Actor, '''');';
    EXEC sys.sp_executesql @Sql, N'@d INT OUTPUT', @d = @Drift OUTPUT;
    PRINT N'CHECK Data unchanged vs backup | drift=' + CAST(@Drift AS NVARCHAR(20)) + N' | '
        + CASE WHEN @Drift = 0 THEN N'PASS' ELSE N'FAIL' END;
    IF @Drift <> 0 SET @Fail += 1;
END;

PRINT N'--- DBCC CHECKCONSTRAINTS ---';
BEGIN TRY
    DBCC CHECKCONSTRAINTS (N'dbo.VMS_VisitAttendanceHistory') WITH NO_INFOMSGS;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
    SET @Fail += 1;
END CATCH;

PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'DayLogID index             | ' + CASE WHEN @DayIx = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'LogID index                | ' + CASE WHEN @LogIx = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'OpenDayLog unique index    | ' + CASE WHEN @UqIx = 1 AND @DupGroups = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'No open DayLogID duplicates| ' + CASE WHEN @DupGroups = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Row count preserved        | ' + CASE WHEN @Cnt >= @Floor THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK unchanged               | ' + CASE WHEN ISNULL(@PkCols, N'') = N'HistoryID' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'DBCC CHECKCONSTRAINTS      | (see above)';
PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) — do not proceed to Phase 4 =====';
GO
