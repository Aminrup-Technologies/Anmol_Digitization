/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 8 — SalesApprovalRequestDetail VALIDATION
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.SalesApprovalRequestDetail (AnmolDristi)
   WHAT:        Read-only PASS/FAIL. No DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;
DECLARE @Floor INT = 34257;
DECLARE @Sql NVARCHAR(MAX);

PRINT N'===== RC-018 P3-T08 SalesApprovalRequestDetail VALIDATION =====';

DECLARE @Cnt INT = (SELECT COUNT(*) FROM dbo.SalesApprovalRequestDetail);
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
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND kc.type = N'PK' AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');
PRINT N'CHECK PK | ' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'ApprovalRequestDetailId' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'ApprovalRequestDetailId' SET @Fail += 1;

DECLARE @Ident INT = COLUMNPROPERTY(OBJECT_ID(N'dbo.SalesApprovalRequestDetail'), N'ApprovalRequestDetailId', N'IsIdentity');
PRINT N'CHECK Identity on ApprovalRequestDetailId | '
    + CASE WHEN @Ident = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @Ident <> 1 SET @Fail += 1;

DECLARE @Seed SQL_VARIANT = IDENT_SEED(N'dbo.SalesApprovalRequestDetail');
DECLARE @Incr SQL_VARIANT = IDENT_INCR(N'dbo.SalesApprovalRequestDetail');
PRINT N'CHECK Identity seed/increment | seed=' + CONVERT(NVARCHAR(20), @Seed)
    + N' incr=' + CONVERT(NVARCHAR(20), @Incr) + N' | '
    + CASE WHEN CONVERT(INT, @Seed) = 1 AND CONVERT(INT, @Incr) = 1 THEN N'PASS' ELSE N'FAIL' END;
IF CONVERT(INT, @Seed) <> 1 OR CONVERT(INT, @Incr) <> 1 SET @Fail += 1;

DECLARE @IxReq BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND name = N'IX_SalesApprovalRequestDetail_RequestId'
) THEN 1 ELSE 0 END;
DECLARE @IxVer BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND name = N'IX_SalesApprovalRequestDetail_Request_Activity_Version'
) THEN 1 ELSE 0 END;
PRINT N'CHECK Required indexes | ' + CASE WHEN @IxReq = 1 AND @IxVer = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @IxReq = 0 OR @IxVer = 0 SET @Fail += 1;

DECLARE @FkAct BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND name = N'FK_SARDetail_Activity' AND is_disabled = 0 AND is_not_trusted = 0
) THEN 1 ELSE 0 END;
DECLARE @FkReq BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND name = N'FK_SARDetail_Request' AND is_disabled = 0 AND is_not_trusted = 0
) THEN 1 ELSE 0 END;
PRINT N'CHECK Outbound FKs trusted | '
    + CASE WHEN @FkAct = 1 AND @FkReq = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @FkAct = 0 OR @FkReq = 0 SET @Fail += 1;

DECLARE @DfOn BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND c.name = N'CreatedOn' AND dc.definition LIKE N'%sysutcdatetime%'
) THEN 1 ELSE 0 END;
DECLARE @DfVer BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND c.name = N'DetailVersion'
      AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(dc.definition, N''), N' ', N''), CHAR(9), N''), CHAR(13), N''), CHAR(10), N''), N'(', N''), N')', N'') = N'1'
) THEN 1 ELSE 0 END;
PRINT N'CHECK Defaults CreatedOn / DetailVersion | '
    + CASE WHEN @DfOn = 1 AND @DfVer = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @DfOn = 0 OR @DfVer = 0 SET @Fail += 1;

DECLARE @Shared INT = (
    SELECT COUNT(*)
    FROM (VALUES
        (N'ApprovalRequestDetailId'), (N'ApprovalRequestId'), (N'ActivityId'),
        (N'Snapshot_TotalDistanceKM'), (N'Snapshot_CalculatedClaimAmount'),
        (N'Snapshot_DAAmount'), (N'Snapshot_HotelAmount'), (N'Snapshot_TravelAmount'),
        (N'Snapshot_ExpenseTotal'), (N'CreatedOn'), (N'CreatedBy'), (N'DetailStatus'),
        (N'DetailRemarks'), (N'DetailApprovedBy'), (N'DetailApprovedOn'),
        (N'ModifiedOn'), (N'ModifiedBy'), (N'DetailApprovedLevel'),
        (N'Snapshot_StationaryAmount'), (N'DetailVersion'), (N'PreviousDetailId')
    ) v(ColumnName)
    WHERE COL_LENGTH(N'dbo.SalesApprovalRequestDetail', v.ColumnName) IS NOT NULL
);
PRINT N'CHECK Shared columns present | ' + CAST(@Shared AS NVARCHAR(10)) + N'/21 | '
    + CASE WHEN @Shared = 21 THEN N'PASS' ELSE N'FAIL' END;
IF @Shared <> 21 SET @Fail += 1;

DECLARE @HistOrphans INT = 0;
DECLARE @HistCol BIT = CASE WHEN COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApprovalRequestDetailId') IS NOT NULL THEN 1 ELSE 0 END;
IF @HistCol = 1
BEGIN
    SET @Sql = N'
        SELECT @c = COUNT(*)
        FROM dbo.ApprovalRequestStatusHistory h
        WHERE h.ApprovalRequestDetailId IS NOT NULL
          AND NOT EXISTS (
              SELECT 1 FROM dbo.SalesApprovalRequestDetail d
              WHERE d.ApprovalRequestDetailId = h.ApprovalRequestDetailId
          );';
    EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @HistOrphans OUTPUT;
    PRINT N'CHECK History detail orphans | ' + CAST(@HistOrphans AS NVARCHAR(20)) + N' | '
        + CASE WHEN @HistOrphans = 0 THEN N'PASS' ELSE N'FAIL' END;
    IF @HistOrphans <> 0
    BEGIN
        SET @Fail += 1;
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
    END
END
ELSE
    PRINT N'CHECK History detail orphans | column not present yet (P3-T07 pending) | PASS (no references exist)';

DECLARE @Trig INT = (SELECT COUNT(*) FROM sys.triggers WHERE parent_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail'));
PRINT N'CHECK Triggers (expect 0) | ' + CAST(@Trig AS NVARCHAR(10)) + N' | '
    + CASE WHEN @Trig = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Trig <> 0 SET @Fail += 1;

PRINT N'--- DBCC CHECKCONSTRAINTS ---';
BEGIN TRY
    DBCC CHECKCONSTRAINTS (N'dbo.SalesApprovalRequestDetail') WITH NO_INFOMSGS;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
    SET @Fail += 1;
END CATCH;

PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'Schema shared columns      | ' + CASE WHEN @Shared = 21 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK unchanged               | ' + CASE WHEN ISNULL(@PkCols, N'') = N'ApprovalRequestDetailId' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Required indexes           | ' + CASE WHEN @IxReq = 1 AND @IxVer = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Defaults                   | ' + CASE WHEN @DfOn = 1 AND @DfVer = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Outbound FKs               | ' + CASE WHEN @FkAct = 1 AND @FkReq = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Identity preserved         | ' + CASE WHEN @Ident = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Row count preserved        | ' + CASE WHEN @Cnt >= @Floor THEN N'PASS' ELSE N'FAIL' END;
PRINT N'History detail orphans     | ' + CASE WHEN @HistCol = 0 OR @HistOrphans = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'DBCC CHECKCONSTRAINTS      | (see above)';
PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) — do not proceed to Table 9 =====';
GO
