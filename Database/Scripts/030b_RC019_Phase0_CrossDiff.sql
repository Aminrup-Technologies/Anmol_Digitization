/* ============================================================================
   NAME:        RC-019 Phase 0 — Cross-DB Diff (READ ONLY) — Script 030b
   WHY:         Compare AnmolDristiDev (UAT) vs AnmolDristi (LIVE) for the 14 Script 030 tables.
============================================================================ */
SET NOCOUNT ON;

DECLARE @Tables TABLE (Ord INT, TableName SYSNAME);
INSERT INTO @Tables VALUES
 (1,N'FrequencyTypes'),(2,N'MST_Calendar'),(3,N'MST_EmployeeTolerance'),
 (4,N'MST_Plant_Lines'),(5,N'TRN_RM_CLASS_1'),(6,N'PolicyException'),
 (7,N'ApprovalRequestStatusHistory'),(8,N'SalesApprovalRequestDetail'),
 (9,N'SalesDailyActivity'),(10,N'SalesDailyExpense'),
 (11,N'VMS_VisitAttendanceHistory'),(12,N'CMS_RawConsumptionLogs'),
 (13,N'TRN_qc_packet_weight_hdr'),(14,N'TRN_qcinspector');

PRINT N'===== RC-019 SCRIPT 030B PHASE 0 MATRIX =====';
PRINT N'';

/* Row counts */
PRINT N'--- ROW COUNTS ---';
SELECT t.Ord, t.TableName,
  (SELECT SUM(p.rows) FROM AnmolDristiDev.sys.partitions p
   INNER JOIN AnmolDristiDev.sys.objects o ON o.object_id=p.object_id
   WHERE o.name=t.TableName AND o.schema_id=1 AND p.index_id IN (0,1)) AS UAT_ApproxRows,
  (SELECT SUM(p.rows) FROM AnmolDristi.sys.partitions p
   INNER JOIN AnmolDristi.sys.objects o ON o.object_id=p.object_id
   WHERE o.name=t.TableName AND o.schema_id=1 AND p.index_id IN (0,1)) AS LIVE_ApproxRows
FROM @Tables t ORDER BY t.Ord;

/* Column diffs */
PRINT N'';
PRINT N'--- COLUMN DIFFS (UAT vs LIVE) ---';
;WITH UAT AS (
  SELECT t.Ord, t.TableName, c.name AS ColumnName, c.column_id,
    ty.name AS TypeName, c.max_length, c.precision, c.scale, c.is_nullable,
    c.is_identity, c.is_computed, cc.definition AS ComputedDef,
    dc.definition AS DefaultDef
  FROM @Tables t
  INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristiDev.sys.columns c ON c.object_id=o.object_id
  INNER JOIN AnmolDristiDev.sys.types ty ON ty.user_type_id=c.user_type_id
  LEFT JOIN AnmolDristiDev.sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
  LEFT JOIN AnmolDristiDev.sys.default_constraints dc ON dc.parent_object_id=c.object_id AND dc.parent_column_id=c.column_id
),
LIVE AS (
  SELECT t.Ord, t.TableName, c.name AS ColumnName, c.column_id,
    ty.name AS TypeName, c.max_length, c.precision, c.scale, c.is_nullable,
    c.is_identity, c.is_computed, cc.definition AS ComputedDef,
    dc.definition AS DefaultDef
  FROM @Tables t
  INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristi.sys.columns c ON c.object_id=o.object_id
  INNER JOIN AnmolDristi.sys.types ty ON ty.user_type_id=c.user_type_id
  LEFT JOIN AnmolDristi.sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
  LEFT JOIN AnmolDristi.sys.default_constraints dc ON dc.parent_object_id=c.object_id AND dc.parent_column_id=c.column_id
),
Joined AS (
  SELECT COALESCE(u.Ord,l.Ord) AS Ord,
         COALESCE(u.TableName,l.TableName) AS TableName,
         COALESCE(u.ColumnName,l.ColumnName) AS ColumnName,
         CASE WHEN u.ColumnName IS NULL THEN N'LIVE_ONLY'
              WHEN l.ColumnName IS NULL THEN N'UAT_ONLY'
              WHEN u.TypeName<>l.TypeName OR u.max_length<>l.max_length OR u.precision<>l.precision
                   OR u.scale<>l.scale THEN N'TYPE_DIFF'
              WHEN u.is_nullable<>l.is_nullable THEN N'NULL_DIFF'
              WHEN ISNULL(u.is_identity,0)<>ISNULL(l.is_identity,0) THEN N'IDENTITY_DIFF'
              WHEN ISNULL(u.is_computed,0)<>ISNULL(l.is_computed,0)
                   OR ISNULL(u.ComputedDef,N'')<>ISNULL(l.ComputedDef,N'') THEN N'COMPUTED_DIFF'
              WHEN ISNULL(u.DefaultDef,N'')<>ISNULL(l.DefaultDef,N'') THEN N'DEFAULT_DIFF'
              ELSE N'MATCH' END AS DiffKind,
         u.TypeName AS UAT_Type, l.TypeName AS LIVE_Type,
         u.max_length AS UAT_Len, l.max_length AS LIVE_Len,
         u.precision AS UAT_Prec, l.precision AS LIVE_Prec,
         u.scale AS UAT_Scale, l.scale AS LIVE_Scale,
         u.is_nullable AS UAT_Null, l.is_nullable AS LIVE_Null,
         u.is_identity AS UAT_Ident, l.is_identity AS LIVE_Ident,
         u.is_computed AS UAT_Comp, l.is_computed AS LIVE_Comp,
         u.DefaultDef AS UAT_Default, l.DefaultDef AS LIVE_Default,
         u.ComputedDef AS UAT_Computed, l.ComputedDef AS LIVE_Computed
  FROM UAT u
  FULL OUTER JOIN LIVE l ON u.TableName=l.TableName AND u.ColumnName=l.ColumnName
)
SELECT * FROM Joined
WHERE DiffKind <> N'MATCH'
ORDER BY Ord, ColumnName;

PRINT N'';
PRINT N'--- COLUMN MATCH SUMMARY ---';
;WITH UAT AS (
  SELECT t.TableName, c.name AS ColumnName, ty.name AS TypeName, c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity, c.is_computed,
         cc.definition AS ComputedDef, dc.definition AS DefaultDef
  FROM @Tables t
  INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristiDev.sys.columns c ON c.object_id=o.object_id
  INNER JOIN AnmolDristiDev.sys.types ty ON ty.user_type_id=c.user_type_id
  LEFT JOIN AnmolDristiDev.sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
  LEFT JOIN AnmolDristiDev.sys.default_constraints dc ON dc.parent_object_id=c.object_id AND dc.parent_column_id=c.column_id
),
LIVE AS (
  SELECT t.TableName, c.name AS ColumnName, ty.name AS TypeName, c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity, c.is_computed,
         cc.definition AS ComputedDef, dc.definition AS DefaultDef
  FROM @Tables t
  INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristi.sys.columns c ON c.object_id=o.object_id
  INNER JOIN AnmolDristi.sys.types ty ON ty.user_type_id=c.user_type_id
  LEFT JOIN AnmolDristi.sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
  LEFT JOIN AnmolDristi.sys.default_constraints dc ON dc.parent_object_id=c.object_id AND dc.parent_column_id=c.column_id
)
SELECT COALESCE(u.TableName,l.TableName) AS TableName,
  SUM(CASE WHEN u.ColumnName IS NOT NULL AND l.ColumnName IS NOT NULL
            AND u.TypeName=l.TypeName AND u.max_length=l.max_length AND u.precision=l.precision AND u.scale=l.scale
            AND u.is_nullable=l.is_nullable AND ISNULL(u.is_identity,0)=ISNULL(l.is_identity,0)
            AND ISNULL(u.is_computed,0)=ISNULL(l.is_computed,0)
            AND ISNULL(u.ComputedDef,N'')=ISNULL(l.ComputedDef,N'')
            AND ISNULL(u.DefaultDef,N'')=ISNULL(l.DefaultDef,N'') THEN 1 ELSE 0 END) AS MatchingCols,
  SUM(CASE WHEN u.ColumnName IS NOT NULL AND l.ColumnName IS NULL THEN 1 ELSE 0 END) AS UAT_OnlyCols,
  SUM(CASE WHEN u.ColumnName IS NULL AND l.ColumnName IS NOT NULL THEN 1 ELSE 0 END) AS LIVE_OnlyCols,
  SUM(CASE WHEN u.ColumnName IS NOT NULL AND l.ColumnName IS NOT NULL
            AND NOT (u.TypeName=l.TypeName AND u.max_length=l.max_length AND u.precision=l.precision AND u.scale=l.scale
            AND u.is_nullable=l.is_nullable AND ISNULL(u.is_identity,0)=ISNULL(l.is_identity,0)
            AND ISNULL(u.is_computed,0)=ISNULL(l.is_computed,0)
            AND ISNULL(u.ComputedDef,N'')=ISNULL(l.ComputedDef,N'')
            AND ISNULL(u.DefaultDef,N'')=ISNULL(l.DefaultDef,N'')) THEN 1 ELSE 0 END) AS DifferingCols
FROM UAT u
FULL OUTER JOIN LIVE l ON u.TableName=l.TableName AND u.ColumnName=l.ColumnName
GROUP BY COALESCE(u.TableName,l.TableName)
ORDER BY TableName;

/* PK diffs */
PRINT N'';
PRINT N'--- PRIMARY KEY DIFFS ---';
;WITH UAT AS (
  SELECT t.TableName, kc.name AS PkName,
    STUFF((SELECT N','+c.name FROM AnmolDristiDev.sys.index_columns ic
      INNER JOIN AnmolDristiDev.sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=0
      ORDER BY ic.key_ordinal FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,'') AS PkCols
  FROM @Tables t
  INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristiDev.sys.key_constraints kc ON kc.parent_object_id=o.object_id AND kc.type='PK'
  INNER JOIN AnmolDristiDev.sys.indexes i ON i.object_id=kc.parent_object_id AND i.index_id=kc.unique_index_id
),
LIVE AS (
  SELECT t.TableName, kc.name AS PkName,
    STUFF((SELECT N','+c.name FROM AnmolDristi.sys.index_columns ic
      INNER JOIN AnmolDristi.sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=0
      ORDER BY ic.key_ordinal FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,'') AS PkCols
  FROM @Tables t
  INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristi.sys.key_constraints kc ON kc.parent_object_id=o.object_id AND kc.type='PK'
  INNER JOIN AnmolDristi.sys.indexes i ON i.object_id=kc.parent_object_id AND i.index_id=kc.unique_index_id
)
SELECT COALESCE(u.TableName,l.TableName) AS TableName,
  u.PkName AS UAT_Pk, u.PkCols AS UAT_Cols,
  l.PkName AS LIVE_Pk, l.PkCols AS LIVE_Cols,
  CASE WHEN ISNULL(u.PkCols,N'')=ISNULL(l.PkCols,N'') THEN N'MATCH'
       WHEN u.TableName IS NULL THEN N'LIVE_ONLY_PK'
       WHEN l.TableName IS NULL THEN N'UAT_ONLY_PK'
       ELSE N'PK_DIFF' END AS DiffKind
FROM UAT u FULL OUTER JOIN LIVE l ON u.TableName=l.TableName
ORDER BY TableName;

/* FK diffs */
PRINT N'';
PRINT N'--- FOREIGN KEY DIFFS ---';
;WITH UAT AS (
  SELECT t.TableName, fk.name AS FkName,
    OBJECT_NAME(fk.referenced_object_id, DB_ID('AnmolDristiDev')) AS RefTable,
    STUFF((SELECT N','+c.name FROM AnmolDristiDev.sys.foreign_key_columns fkc
      INNER JOIN AnmolDristiDev.sys.columns c ON c.object_id=fkc.parent_object_id AND c.column_id=fkc.parent_column_id
      WHERE fkc.constraint_object_id=fk.object_id ORDER BY fkc.constraint_column_id
      FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,'') AS ParentCols
  FROM @Tables t
  INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristiDev.sys.foreign_keys fk ON fk.parent_object_id=o.object_id
),
LIVE AS (
  SELECT t.TableName, fk.name AS FkName,
    OBJECT_NAME(fk.referenced_object_id, DB_ID('AnmolDristi')) AS RefTable,
    STUFF((SELECT N','+c.name FROM AnmolDristi.sys.foreign_key_columns fkc
      INNER JOIN AnmolDristi.sys.columns c ON c.object_id=fkc.parent_object_id AND c.column_id=fkc.parent_column_id
      WHERE fkc.constraint_object_id=fk.object_id ORDER BY fkc.constraint_column_id
      FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,'') AS ParentCols
  FROM @Tables t
  INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristi.sys.foreign_keys fk ON fk.parent_object_id=o.object_id
)
SELECT COALESCE(u.TableName,l.TableName) AS TableName,
  COALESCE(u.FkName,l.FkName) AS FkName,
  u.RefTable AS UAT_Ref, u.ParentCols AS UAT_Cols,
  l.RefTable AS LIVE_Ref, l.ParentCols AS LIVE_Cols,
  CASE WHEN u.FkName IS NULL THEN N'LIVE_ONLY'
       WHEN l.FkName IS NULL THEN N'UAT_ONLY'
       WHEN ISNULL(u.ParentCols,N'')=ISNULL(l.ParentCols,N'') AND ISNULL(u.RefTable,N'')=ISNULL(l.RefTable,N'') THEN N'MATCH'
       ELSE N'FK_DIFF' END AS DiffKind
FROM UAT u FULL OUTER JOIN LIVE l
  ON u.TableName=l.TableName AND u.FkName=l.FkName
WHERE NOT (u.FkName IS NOT NULL AND l.FkName IS NOT NULL
           AND ISNULL(u.ParentCols,N'')=ISNULL(l.ParentCols,N'')
           AND ISNULL(u.RefTable,N'')=ISNULL(l.RefTable,N''))
ORDER BY TableName, FkName;

/* Index diffs (by name + key columns) */
PRINT N'';
PRINT N'--- INDEX DIFFS ---';
;WITH UAT AS (
  SELECT t.TableName, i.name AS IndexName, i.type_desc, i.is_unique, i.is_primary_key, i.has_filter, i.filter_definition,
    STUFF((SELECT N','+c.name FROM AnmolDristiDev.sys.index_columns ic
      INNER JOIN AnmolDristiDev.sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=0
      ORDER BY ic.key_ordinal FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,'') AS KeyCols,
    STUFF((SELECT N','+c.name FROM AnmolDristiDev.sys.index_columns ic
      INNER JOIN AnmolDristiDev.sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=1
      ORDER BY ic.index_column_id FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,'') AS InclCols
  FROM @Tables t
  INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristiDev.sys.indexes i ON i.object_id=o.object_id AND i.type>0 AND i.is_primary_key=0
),
LIVE AS (
  SELECT t.TableName, i.name AS IndexName, i.type_desc, i.is_unique, i.is_primary_key, i.has_filter, i.filter_definition,
    STUFF((SELECT N','+c.name FROM AnmolDristi.sys.index_columns ic
      INNER JOIN AnmolDristi.sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=0
      ORDER BY ic.key_ordinal FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,'') AS KeyCols,
    STUFF((SELECT N','+c.name FROM AnmolDristi.sys.index_columns ic
      INNER JOIN AnmolDristi.sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=1
      ORDER BY ic.index_column_id FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,1,'') AS InclCols
  FROM @Tables t
  INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristi.sys.indexes i ON i.object_id=o.object_id AND i.type>0 AND i.is_primary_key=0
)
SELECT COALESCE(u.TableName,l.TableName) AS TableName,
  COALESCE(u.IndexName,l.IndexName) AS IndexName,
  u.KeyCols AS UAT_Keys, u.InclCols AS UAT_Incl, u.is_unique AS UAT_Unique,
  l.KeyCols AS LIVE_Keys, l.InclCols AS LIVE_Incl, l.is_unique AS LIVE_Unique,
  CASE WHEN u.IndexName IS NULL THEN N'LIVE_ONLY'
       WHEN l.IndexName IS NULL THEN N'UAT_ONLY'
       WHEN ISNULL(u.KeyCols,N'')=ISNULL(l.KeyCols,N'') AND ISNULL(u.InclCols,N'')=ISNULL(l.InclCols,N'')
            AND ISNULL(u.is_unique,0)=ISNULL(l.is_unique,0) THEN N'MATCH'
       ELSE N'INDEX_DIFF' END AS DiffKind
FROM UAT u FULL OUTER JOIN LIVE l
  ON u.TableName=l.TableName AND u.IndexName=l.IndexName
WHERE NOT (u.IndexName IS NOT NULL AND l.IndexName IS NOT NULL
           AND ISNULL(u.KeyCols,N'')=ISNULL(l.KeyCols,N'')
           AND ISNULL(u.InclCols,N'')=ISNULL(l.InclCols,N'')
           AND ISNULL(u.is_unique,0)=ISNULL(l.is_unique,0))
ORDER BY TableName, IndexName;

/* Check constraint diffs */
PRINT N'';
PRINT N'--- CHECK CONSTRAINT DIFFS ---';
;WITH UAT AS (
  SELECT t.TableName, cc.name AS CheckName, cc.definition
  FROM @Tables t
  INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristiDev.sys.check_constraints cc ON cc.parent_object_id=o.object_id
),
LIVE AS (
  SELECT t.TableName, cc.name AS CheckName, cc.definition
  FROM @Tables t
  INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristi.sys.check_constraints cc ON cc.parent_object_id=o.object_id
)
SELECT COALESCE(u.TableName,l.TableName) AS TableName,
  COALESCE(u.CheckName,l.CheckName) AS CheckName,
  u.definition AS UAT_Def, l.definition AS LIVE_Def,
  CASE WHEN u.CheckName IS NULL THEN N'LIVE_ONLY'
       WHEN l.CheckName IS NULL THEN N'UAT_ONLY'
       WHEN ISNULL(u.definition,N'')=ISNULL(l.definition,N'') THEN N'MATCH'
       ELSE N'CHECK_DIFF' END AS DiffKind
FROM UAT u FULL OUTER JOIN LIVE l ON u.TableName=l.TableName AND u.CheckName=l.CheckName
WHERE NOT (u.CheckName IS NOT NULL AND l.CheckName IS NOT NULL AND ISNULL(u.definition,N'')=ISNULL(l.definition,N''))
ORDER BY TableName, CheckName;

/* Trigger diffs */
PRINT N'';
PRINT N'--- TRIGGER DIFFS ---';
;WITH UAT AS (
  SELECT t.TableName, tr.name AS TriggerName, tr.is_disabled
  FROM @Tables t
  INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristiDev.sys.triggers tr ON tr.parent_id=o.object_id
),
LIVE AS (
  SELECT t.TableName, tr.name AS TriggerName, tr.is_disabled
  FROM @Tables t
  INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type='U'
  INNER JOIN AnmolDristi.sys.triggers tr ON tr.parent_id=o.object_id
)
SELECT COALESCE(u.TableName,l.TableName) AS TableName,
  COALESCE(u.TriggerName,l.TriggerName) AS TriggerName,
  u.is_disabled AS UAT_Disabled, l.is_disabled AS LIVE_Disabled,
  CASE WHEN u.TriggerName IS NULL THEN N'LIVE_ONLY'
       WHEN l.TriggerName IS NULL THEN N'UAT_ONLY'
       ELSE N'MATCH' END AS DiffKind
FROM UAT u FULL OUTER JOIN LIVE l ON u.TableName=l.TableName AND u.TriggerName=l.TriggerName
ORDER BY TableName, TriggerName;

PRINT N'';
PRINT N'===== END RC-019 SCRIPT 030B PHASE 0 DIFF =====';
GO
