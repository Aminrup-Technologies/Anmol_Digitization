/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 5 — TRN_RM_CLASS_1 VALIDATION
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.TRN_RM_CLASS_1 (AnmolDristi)
   ACCEPTANCE:  LIVE ⊇ UAT by RMFID. UAT must be reachable.
                Do NOT require equal row counts.
                Shared Id values must match. Material_Name must exist.
                RMF330 must still differ from UAT (accepted production divergence).
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;

PRINT N'===== RC-018 P1-T05 TRN_RM_CLASS_1 VALIDATION =====';

DECLARE @LiveCnt INT = (SELECT COUNT(*) FROM dbo.TRN_RM_CLASS_1);
DECLARE @UatReachable BIT = CASE
    WHEN OBJECT_ID(N'AnmolDristiDev.dbo.TRN_RM_CLASS_1', N'U') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @UatCnt INT = 0;
IF @UatReachable = 1
    SELECT @UatCnt = COUNT(*) FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1;

DECLARE @SupersetOk BIT = CASE WHEN @UatReachable = 1 AND @LiveCnt >= @UatCnt THEN 1 ELSE 0 END;
PRINT N'CHECK UAT reachable | ' + CASE WHEN @UatReachable = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @UatReachable = 0 SET @Fail += 1;
PRINT N'CHECK LIVE count >= UAT count | LIVE=' + CAST(@LiveCnt AS NVARCHAR(20))
    + N' UAT=' + CAST(@UatCnt AS NVARCHAR(20)) + N' | '
    + CASE WHEN @SupersetOk = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @SupersetOk = 0 SET @Fail += 1;

DECLARE @Missing INT = 0;
IF @UatReachable = 1
    SELECT @Missing = COUNT(*)
    FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1 u
    WHERE NOT EXISTS (SELECT 1 FROM dbo.TRN_RM_CLASS_1 l WHERE l.RMFID = u.RMFID);
DECLARE @MissingOk BIT = CASE WHEN @UatReachable = 1 AND @Missing = 0 THEN 1 ELSE 0 END;
PRINT N'CHECK Missing UAT keys in LIVE | ' + CAST(@Missing AS NVARCHAR(20)) + N' | '
    + CASE WHEN @MissingOk = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @MissingOk = 0 SET @Fail += 1;

DECLARE @Dup INT = (
    SELECT COUNT(*) FROM (
        SELECT RMFID FROM dbo.TRN_RM_CLASS_1 GROUP BY RMFID HAVING COUNT(*) > 1
    ) d
);
PRINT N'CHECK Duplicate business keys (RMFID) | ' + CAST(@Dup AS NVARCHAR(20)) + N' | '
    + CASE WHEN @Dup = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Dup <> 0 SET @Fail += 1;

DECLARE @HasMat BIT = CASE WHEN COL_LENGTH(N'dbo.TRN_RM_CLASS_1', N'Material_Name') IS NOT NULL THEN 1 ELSE 0 END;
PRINT N'CHECK Material_Name exists | ' + CASE WHEN @HasMat = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @HasMat = 0 SET @Fail += 1;

DECLARE @PkCols NVARCHAR(4000);
SELECT @PkCols = STUFF((
    SELECT N',' + c.name
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.TRN_RM_CLASS_1')
      AND kc.type = N'PK' AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');
PRINT N'CHECK PK | Columns=' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'RMFID' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'RMFID' SET @Fail += 1;

DECLARE @Ident INT = COLUMNPROPERTY(OBJECT_ID(N'dbo.TRN_RM_CLASS_1'), N'Id', N'IsIdentity');
PRINT N'CHECK Identity still on Id | ' + CASE WHEN @Ident = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @Ident <> 1 SET @Fail += 1;

DECLARE @IdMismatch INT = 0;
IF @UatReachable = 1
    SELECT @IdMismatch = COUNT(*)
    FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1 u
    INNER JOIN dbo.TRN_RM_CLASS_1 l ON l.RMFID = u.RMFID
    WHERE u.Id <> l.Id;
DECLARE @IdOk BIT = CASE WHEN @UatReachable = 1 AND @IdMismatch = 0 THEN 1 ELSE 0 END;
PRINT N'CHECK Shared RMFID Id values unchanged | ' + CAST(@IdMismatch AS NVARCHAR(20)) + N' | '
    + CASE WHEN @IdOk = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @IdOk = 0 SET @Fail += 1;

/* Post-apply: shared attributes should match, except accepted production divergence RMF330. */
DECLARE @Drift INT = 0;
DECLARE @Accepted INT = 0;
DECLARE @Rmf330OnLive BIT = CASE WHEN EXISTS (
    SELECT 1 FROM dbo.TRN_RM_CLASS_1 WHERE RMFID = 'RMF330'
) THEN 1 ELSE 0 END;

IF @UatReachable = 1
BEGIN
    DECLARE @SrcCols NVARCHAR(MAX), @TgtCols NVARCHAR(MAX), @Sql NVARCHAR(MAX);
    SELECT @SrcCols = STUFF((
        SELECT N',src.' + QUOTENAME(c.name)
        FROM AnmolDristiDev.sys.columns c
        INNER JOIN sys.columns lc
            ON lc.object_id = OBJECT_ID(N'dbo.TRN_RM_CLASS_1') AND lc.name = c.name
        WHERE c.object_id = OBJECT_ID(N'AnmolDristiDev.dbo.TRN_RM_CLASS_1')
          AND c.is_computed = 0 AND c.is_identity = 0 AND c.name <> N'Material_Name'
        ORDER BY c.column_id
        FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');
    SET @TgtCols = REPLACE(@SrcCols, N'src.[', N'tgt.[');
    SET @Sql = N'
        SELECT @d = COUNT(*)
        FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1 src
        INNER JOIN dbo.TRN_RM_CLASS_1 tgt ON tgt.RMFID = src.RMFID
        WHERE src.RMFID <> ''RMF330''
          AND EXISTS (SELECT ' + @SrcCols + N' EXCEPT SELECT ' + @TgtCols + N');
        SELECT @a = COUNT(*)
        FROM AnmolDristiDev.dbo.TRN_RM_CLASS_1 src
        INNER JOIN dbo.TRN_RM_CLASS_1 tgt ON tgt.RMFID = src.RMFID
        WHERE src.RMFID = ''RMF330''
          AND EXISTS (SELECT ' + @SrcCols + N' EXCEPT SELECT ' + @TgtCols + N');';
    EXEC sys.sp_executesql @Sql, N'@d INT OUTPUT, @a INT OUTPUT', @d = @Drift OUTPUT, @a = @Accepted OUTPUT;
END
DECLARE @DriftOk BIT = CASE WHEN @UatReachable = 1 AND @Drift = 0 THEN 1 ELSE 0 END;
PRINT N'CHECK Actionable shared drift (excludes RMF330) | ' + CAST(@Drift AS NVARCHAR(20)) + N' | '
    + CASE WHEN @DriftOk = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @DriftOk = 0 SET @Fail += 1;

DECLARE @Rmf330Ok BIT = CASE
    WHEN @UatReachable = 1 AND @Rmf330OnLive = 1 AND @Accepted = 1 THEN 1 ELSE 0 END;
PRINT N'CHECK RMF330 accepted production divergence | LIVE='
    + CASE WHEN @Rmf330OnLive = 1 THEN N'present' ELSE N'MISSING' END
    + N' DriftVsUat=' + CAST(@Accepted AS NVARCHAR(20)) + N' | '
    + CASE WHEN @Rmf330Ok = 1 THEN N'ACCEPTED' ELSE N'FAIL' END;
IF @Rmf330Ok = 0 SET @Fail += 1;

DECLARE @Trig INT = (SELECT COUNT(*) FROM sys.triggers WHERE parent_id = OBJECT_ID(N'dbo.TRN_RM_CLASS_1'));
PRINT N'CHECK Triggers (expect 0) | ' + CAST(@Trig AS NVARCHAR(10)) + N' | '
    + CASE WHEN @Trig = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Trig <> 0 SET @Fail += 1;

PRINT N'--- DBCC CHECKCONSTRAINTS ---';
BEGIN TRY
    DBCC CHECKCONSTRAINTS (N'dbo.TRN_RM_CLASS_1') WITH NO_INFOMSGS;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
    SET @Fail += 1;
END CATCH;

PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'UAT reachable              | ' + CASE WHEN @UatReachable = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Missing UAT keys = 0       | ' + CASE WHEN @MissingOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Duplicate business keys=0  | ' + CASE WHEN @Dup = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Material_Name exists       | ' + CASE WHEN @HasMat = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'LIVE count >= UAT count    | ' + CASE WHEN @SupersetOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK Match (RMFID)           | ' + CASE WHEN ISNULL(@PkCols, N'') = N'RMFID' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Identity property on Id    | ' + CASE WHEN @Ident = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Shared Id values unchanged | ' + CASE WHEN @IdOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Shared drift excl RMF330  | ' + CASE WHEN @DriftOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'RMF330 accepted divergence | ' + CASE WHEN @Rmf330Ok = 1 THEN N'ACCEPTED' ELSE N'FAIL' END;
PRINT N'Row count equality         | NOT REQUIRED';
PRINT N'DBCC CHECKCONSTRAINTS      | (see above)';
PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) — do not leave Phase 1 =====';
GO
