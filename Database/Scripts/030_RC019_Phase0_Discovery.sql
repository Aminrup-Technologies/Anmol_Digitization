/* ============================================================================
   NAME:        RC-019 Phase 0 — Discovery (READ ONLY) — Script 030
   WHEN:        2026-09-27
   WHY:         UAT → LIVE controlled reconciliation baseline.
   WHAT:        Emit schema + row-count inventory for the 14 Script 030 tables.
                No DDL. No DML. Safe to run on UAT and LIVE.
   USAGE:       sqlcmd -d AnmolDristiDev -i ... -o UAT.txt
                sqlcmd -d AnmolDristi    -i ... -o LIVE.txt
============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Env SYSNAME = DB_NAME();
PRINT N'===== RC-019 Script 030 Phase 0 Discovery | Database=' + @Env + N' | ' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 126) + N' =====';

DECLARE @Tables TABLE (SchemaName SYSNAME, TableName SYSNAME, Ord INT);
INSERT INTO @Tables (SchemaName, TableName, Ord) VALUES
 (N'dbo', N'FrequencyTypes',                  1),
 (N'dbo', N'MST_Calendar',                    2),
 (N'dbo', N'MST_EmployeeTolerance',           3),
 (N'dbo', N'MST_Plant_Lines',                 4),
 (N'dbo', N'TRN_RM_CLASS_1',                  5),
 (N'dbo', N'PolicyException',                 6),
 (N'dbo', N'ApprovalRequestStatusHistory',    7),
 (N'dbo', N'SalesApprovalRequestDetail',      8),
 (N'dbo', N'SalesDailyActivity',              9),
 (N'dbo', N'SalesDailyExpense',              10),
 (N'dbo', N'VMS_VisitAttendanceHistory',     11),
 (N'dbo', N'CMS_RawConsumptionLogs',         12),
 (N'dbo', N'TRN_qc_packet_weight_hdr',       13),
 (N'dbo', N'TRN_qcinspector',                14);

/* -------------------------------------------------------------------------
   0) Existence + row counts
--------------------------------------------------------------------------- */
PRINT N'';
PRINT N'--- 0) TABLE EXISTENCE + ROW COUNTS ---';
SELECT
      t.Ord
    , t.TableName
    , CASE WHEN o.object_id IS NULL THEN N'MISSING' ELSE N'EXISTS' END AS ObjectStatus
    , CASE WHEN o.object_id IS NULL THEN NULL ELSE p.rows END AS ApproxRowCount
FROM @Tables t
LEFT JOIN sys.objects o
    ON o.name = t.TableName AND o.schema_id = SCHEMA_ID(t.SchemaName) AND o.type = N'U'
LEFT JOIN sys.partitions p
    ON p.object_id = o.object_id AND p.index_id IN (0, 1)
ORDER BY t.Ord;

/* Exact counts (only for existing tables) */
PRINT N'';
PRINT N'--- 0b) EXACT ROW COUNTS ---';
DECLARE @Ord INT, @Tn SYSNAME, @Sql NVARCHAR(MAX);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR
    SELECT t.Ord, t.TableName
    FROM @Tables t
    INNER JOIN sys.objects o ON o.name = t.TableName AND o.schema_id = SCHEMA_ID(N'dbo') AND o.type = N'U'
    ORDER BY t.Ord;
OPEN c;
FETCH NEXT FROM c INTO @Ord, @Tn;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = N'SELECT ' + CAST(@Ord AS NVARCHAR(10)) + N' AS Ord, N''' + @Tn
             + N''' AS TableName, COUNT_BIG(*) AS ExactRowCount FROM dbo.' + QUOTENAME(@Tn) + N';';
    EXEC sys.sp_executesql @Sql;
    FETCH NEXT FROM c INTO @Ord, @Tn;
END
CLOSE c; DEALLOCATE c;

/* -------------------------------------------------------------------------
   1) Columns (type, null, identity, computed, default)
--------------------------------------------------------------------------- */
PRINT N'';
PRINT N'--- 1) COLUMNS ---';
SELECT
      t.Ord
    , t.TableName
    , c.column_id
    , c.name AS ColumnName
    , ty.name AS TypeName
    , c.max_length
    , c.precision
    , c.scale
    , c.is_nullable
    , c.is_identity
    , CASE WHEN c.is_identity = 1 THEN IDENT_SEED(QUOTENAME(t.SchemaName)+N'.'+QUOTENAME(t.TableName)) END AS IdentSeed
    , CASE WHEN c.is_identity = 1 THEN IDENT_INCR(QUOTENAME(t.SchemaName)+N'.'+QUOTENAME(t.TableName)) END AS IdentIncr
    , c.is_computed
    , cc.definition AS ComputedDefinition
    , dc.name AS DefaultConstraintName
    , dc.definition AS DefaultDefinition
    , c.collation_name
FROM @Tables t
INNER JOIN sys.objects o ON o.name = t.TableName AND o.schema_id = SCHEMA_ID(t.SchemaName) AND o.type = N'U'
INNER JOIN sys.columns c ON c.object_id = o.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
LEFT JOIN sys.default_constraints dc ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
ORDER BY t.Ord, c.column_id;

/* -------------------------------------------------------------------------
   2) Primary Keys
--------------------------------------------------------------------------- */
PRINT N'';
PRINT N'--- 2) PRIMARY KEYS ---';
SELECT
      t.Ord
    , t.TableName
    , kc.name AS PkName
    , i.type_desc AS IndexType
    , STUFF((
        SELECT N',' + col.name
        FROM sys.index_columns ic2
        INNER JOIN sys.columns col ON col.object_id = ic2.object_id AND col.column_id = ic2.column_id
        WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id AND ic2.is_included_column = 0
        ORDER BY ic2.key_ordinal
        FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'') AS PkColumns
FROM @Tables t
INNER JOIN sys.objects o ON o.name = t.TableName AND o.schema_id = SCHEMA_ID(t.SchemaName) AND o.type = N'U'
INNER JOIN sys.key_constraints kc ON kc.parent_object_id = o.object_id AND kc.type = N'PK'
INNER JOIN sys.indexes i ON i.object_id = kc.parent_object_id AND i.index_id = kc.unique_index_id
ORDER BY t.Ord;

/* -------------------------------------------------------------------------
   3) Foreign Keys
--------------------------------------------------------------------------- */
PRINT N'';
PRINT N'--- 3) FOREIGN KEYS ---';
SELECT
      t.Ord
    , t.TableName
    , fk.name AS FkName
    , OBJECT_SCHEMA_NAME(fk.referenced_object_id) + N'.' + OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
    , STUFF((
        SELECT N',' + c1.name
        FROM sys.foreign_key_columns fkc
        INNER JOIN sys.columns c1 ON c1.object_id = fkc.parent_object_id AND c1.column_id = fkc.parent_column_id
        WHERE fkc.constraint_object_id = fk.object_id
        ORDER BY fkc.constraint_column_id
        FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'') AS ParentColumns
    , STUFF((
        SELECT N',' + c2.name
        FROM sys.foreign_key_columns fkc
        INNER JOIN sys.columns c2 ON c2.object_id = fkc.referenced_object_id AND c2.column_id = fkc.referenced_column_id
        WHERE fkc.constraint_object_id = fk.object_id
        ORDER BY fkc.constraint_column_id
        FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'') AS ReferencedColumns
    , fk.delete_referential_action_desc
    , fk.update_referential_action_desc
    , fk.is_disabled
    , fk.is_not_trusted
FROM @Tables t
INNER JOIN sys.objects o ON o.name = t.TableName AND o.schema_id = SCHEMA_ID(t.SchemaName) AND o.type = N'U'
INNER JOIN sys.foreign_keys fk ON fk.parent_object_id = o.object_id
ORDER BY t.Ord, fk.name;

/* -------------------------------------------------------------------------
   4) Check Constraints
--------------------------------------------------------------------------- */
PRINT N'';
PRINT N'--- 4) CHECK CONSTRAINTS ---';
SELECT
      t.Ord
    , t.TableName
    , cc.name AS CheckName
    , cc.definition
    , cc.is_disabled
    , cc.is_not_trusted
FROM @Tables t
INNER JOIN sys.objects o ON o.name = t.TableName AND o.schema_id = SCHEMA_ID(t.SchemaName) AND o.type = N'U'
INNER JOIN sys.check_constraints cc ON cc.parent_object_id = o.object_id
ORDER BY t.Ord, cc.name;

/* -------------------------------------------------------------------------
   5) Indexes (non-PK)
--------------------------------------------------------------------------- */
PRINT N'';
PRINT N'--- 5) INDEXES ---';
SELECT
      t.Ord
    , t.TableName
    , i.name AS IndexName
    , i.type_desc
    , i.is_unique
    , i.is_primary_key
    , i.is_unique_constraint
    , i.has_filter
    , i.filter_definition
    , STUFF((
        SELECT N',' + col.name + CASE WHEN ic2.is_descending_key = 1 THEN N' DESC' ELSE N'' END
        FROM sys.index_columns ic2
        INNER JOIN sys.columns col ON col.object_id = ic2.object_id AND col.column_id = ic2.column_id
        WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id AND ic2.is_included_column = 0
        ORDER BY ic2.key_ordinal
        FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'') AS KeyColumns
    , STUFF((
        SELECT N',' + col.name
        FROM sys.index_columns ic2
        INNER JOIN sys.columns col ON col.object_id = ic2.object_id AND col.column_id = ic2.column_id
        WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id AND ic2.is_included_column = 1
        ORDER BY ic2.index_column_id
        FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'') AS IncludeColumns
FROM @Tables t
INNER JOIN sys.objects o ON o.name = t.TableName AND o.schema_id = SCHEMA_ID(t.SchemaName) AND o.type = N'U'
INNER JOIN sys.indexes i ON i.object_id = o.object_id AND i.type > 0
ORDER BY t.Ord, i.is_primary_key DESC, i.name;

/* -------------------------------------------------------------------------
   6) Triggers
--------------------------------------------------------------------------- */
PRINT N'';
PRINT N'--- 6) TRIGGERS ---';
SELECT
      t.Ord
    , t.TableName
    , tr.name AS TriggerName
    , tr.is_disabled
    , OBJECTPROPERTY(tr.object_id, 'ExecIsInsertTrigger') AS IsInsert
    , OBJECTPROPERTY(tr.object_id, 'ExecIsUpdateTrigger') AS IsUpdate
    , OBJECTPROPERTY(tr.object_id, 'ExecIsDeleteTrigger') AS IsDelete
FROM @Tables t
INNER JOIN sys.objects o ON o.name = t.TableName AND o.schema_id = SCHEMA_ID(t.SchemaName) AND o.type = N'U'
INNER JOIN sys.triggers tr ON tr.parent_id = o.object_id
ORDER BY t.Ord, tr.name;

/* -------------------------------------------------------------------------
   7) CREATE TABLE (scripted via sp_help / column dump already above)
      Emit compact DDL fingerprint per table
--------------------------------------------------------------------------- */
PRINT N'';
PRINT N'--- 7) DDL FINGERPRINT (ordered column signature) ---';
SELECT
      t.Ord
    , t.TableName
    , STUFF((
        SELECT N'|' + c.name + N':' + ty.name
            + CASE
                WHEN ty.name IN (N'nvarchar', N'varchar', N'nchar', N'char', N'varbinary', N'binary')
                    THEN N'(' + CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(
                        CASE WHEN ty.name LIKE N'n%' THEN c.max_length/2 ELSE c.max_length END AS NVARCHAR(20)) END + N')'
                WHEN ty.name IN (N'decimal', N'numeric')
                    THEN N'(' + CAST(c.precision AS NVARCHAR(10)) + N',' + CAST(c.scale AS NVARCHAR(10)) + N')'
                WHEN ty.name IN (N'datetime2', N'time', N'datetimeoffset')
                    THEN N'(' + CAST(c.scale AS NVARCHAR(10)) + N')'
                ELSE N''
              END
            + CASE WHEN c.is_nullable = 1 THEN N' NULL' ELSE N' NOT NULL' END
            + CASE WHEN c.is_identity = 1 THEN N' IDENTITY' ELSE N'' END
            + CASE WHEN c.is_computed = 1 THEN N' COMPUTED' ELSE N'' END
        FROM sys.columns c
        INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE c.object_id = o.object_id
        ORDER BY c.column_id
        FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'') AS ColumnSignature
FROM @Tables t
INNER JOIN sys.objects o ON o.name = t.TableName AND o.schema_id = SCHEMA_ID(t.SchemaName) AND o.type = N'U'
ORDER BY t.Ord;

PRINT N'';
PRINT N'===== END RC-019 Script 030 Phase 0 | ' + @Env + N' =====';
GO
