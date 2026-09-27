/* RC-019 Phase 0 — Script 030c — PK / FK / Index / Check / Trigger diffs (READ ONLY) */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Tables TABLE (Ord INT, TableName SYSNAME);
INSERT INTO @Tables VALUES
 (1,N'FrequencyTypes'),(2,N'MST_Calendar'),(3,N'MST_EmployeeTolerance'),
 (4,N'MST_Plant_Lines'),(5,N'TRN_RM_CLASS_1'),(6,N'PolicyException'),
 (7,N'ApprovalRequestStatusHistory'),(8,N'SalesApprovalRequestDetail'),
 (9,N'SalesDailyActivity'),(10,N'SalesDailyExpense'),
 (11,N'VMS_VisitAttendanceHistory'),(12,N'CMS_RawConsumptionLogs'),
 (13,N'TRN_qc_packet_weight_hdr'),(14,N'TRN_qcinspector');

PRINT N'--- PRIMARY KEYS ---';
;WITH UAT AS (
  SELECT t.TableName, kc.name AS PkName,
    STUFF((SELECT N','+c.name FROM AnmolDristiDev.sys.index_columns ic
      INNER JOIN AnmolDristiDev.sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=0
      ORDER BY ic.key_ordinal FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'),1,1,N'') AS PkCols
  FROM @Tables t
  INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
  INNER JOIN AnmolDristiDev.sys.key_constraints kc ON kc.parent_object_id=o.object_id AND kc.type=N'PK'
  INNER JOIN AnmolDristiDev.sys.indexes i ON i.object_id=kc.parent_object_id AND i.index_id=kc.unique_index_id
),
LIVE AS (
  SELECT t.TableName, kc.name AS PkName,
    STUFF((SELECT N','+c.name FROM AnmolDristi.sys.index_columns ic
      INNER JOIN AnmolDristi.sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=0
      ORDER BY ic.key_ordinal FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'),1,1,N'') AS PkCols
  FROM @Tables t
  INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
  INNER JOIN AnmolDristi.sys.key_constraints kc ON kc.parent_object_id=o.object_id AND kc.type=N'PK'
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

PRINT N'';
PRINT N'--- FOREIGN KEYS (non-matching only) ---';
;WITH UAT AS (
  SELECT t.TableName, fk.name AS FkName,
    OBJECT_NAME(fk.referenced_object_id, DB_ID(N'AnmolDristiDev')) AS RefTable,
    STUFF((SELECT N','+c.name FROM AnmolDristiDev.sys.foreign_key_columns fkc
      INNER JOIN AnmolDristiDev.sys.columns c ON c.object_id=fkc.parent_object_id AND c.column_id=fkc.parent_column_id
      WHERE fkc.constraint_object_id=fk.object_id ORDER BY fkc.constraint_column_id
      FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'),1,1,N'') AS ParentCols
  FROM @Tables t
  INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
  INNER JOIN AnmolDristiDev.sys.foreign_keys fk ON fk.parent_object_id=o.object_id
),
LIVE AS (
  SELECT t.TableName, fk.name AS FkName,
    OBJECT_NAME(fk.referenced_object_id, DB_ID(N'AnmolDristi')) AS RefTable,
    STUFF((SELECT N','+c.name FROM AnmolDristi.sys.foreign_key_columns fkc
      INNER JOIN AnmolDristi.sys.columns c ON c.object_id=fkc.parent_object_id AND c.column_id=fkc.parent_column_id
      WHERE fkc.constraint_object_id=fk.object_id ORDER BY fkc.constraint_column_id
      FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'),1,1,N'') AS ParentCols
  FROM @Tables t
  INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
  INNER JOIN AnmolDristi.sys.foreign_keys fk ON fk.parent_object_id=o.object_id
)
SELECT COALESCE(u.TableName,l.TableName) AS TableName,
  COALESCE(u.FkName,l.FkName) AS FkName,
  u.RefTable AS UAT_Ref, u.ParentCols AS UAT_Cols,
  l.RefTable AS LIVE_Ref, l.ParentCols AS LIVE_Cols,
  CASE WHEN u.FkName IS NULL THEN N'LIVE_ONLY'
       WHEN l.FkName IS NULL THEN N'UAT_ONLY'
       ELSE N'FK_DIFF' END AS DiffKind
FROM UAT u FULL OUTER JOIN LIVE l ON u.TableName=l.TableName AND u.FkName=l.FkName
WHERE u.FkName IS NULL OR l.FkName IS NULL
   OR ISNULL(u.ParentCols,N'')<>ISNULL(l.ParentCols,N'')
   OR ISNULL(u.RefTable,N'')<>ISNULL(l.RefTable,N'')
ORDER BY TableName, FkName;

PRINT N'';
PRINT N'--- ALL FOREIGN KEYS (inventory) ---';
SELECT N'UAT' AS Env, t.TableName, fk.name AS FkName,
  OBJECT_NAME(fk.referenced_object_id, DB_ID(N'AnmolDristiDev')) AS RefTable
FROM @Tables t
INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
INNER JOIN AnmolDristiDev.sys.foreign_keys fk ON fk.parent_object_id=o.object_id
UNION ALL
SELECT N'LIVE', t.TableName, fk.name,
  OBJECT_NAME(fk.referenced_object_id, DB_ID(N'AnmolDristi'))
FROM @Tables t
INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
INNER JOIN AnmolDristi.sys.foreign_keys fk ON fk.parent_object_id=o.object_id
ORDER BY TableName, Env, FkName;

PRINT N'';
PRINT N'--- INDEX DIFFS (non-PK, non-matching) ---';
;WITH UAT AS (
  SELECT t.TableName, i.name AS IndexName, i.is_unique,
    STUFF((SELECT N','+c.name FROM AnmolDristiDev.sys.index_columns ic
      INNER JOIN AnmolDristiDev.sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=0
      ORDER BY ic.key_ordinal FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'),1,1,N'') AS KeyCols,
    STUFF((SELECT N','+c.name FROM AnmolDristiDev.sys.index_columns ic
      INNER JOIN AnmolDristiDev.sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=1
      ORDER BY ic.index_column_id FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'),1,1,N'') AS InclCols
  FROM @Tables t
  INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
  INNER JOIN AnmolDristiDev.sys.indexes i ON i.object_id=o.object_id AND i.type>0 AND i.is_primary_key=0 AND i.name IS NOT NULL
),
LIVE AS (
  SELECT t.TableName, i.name AS IndexName, i.is_unique,
    STUFF((SELECT N','+c.name FROM AnmolDristi.sys.index_columns ic
      INNER JOIN AnmolDristi.sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=0
      ORDER BY ic.key_ordinal FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'),1,1,N'') AS KeyCols,
    STUFF((SELECT N','+c.name FROM AnmolDristi.sys.index_columns ic
      INNER JOIN AnmolDristi.sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=1
      ORDER BY ic.index_column_id FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'),1,1,N'') AS InclCols
  FROM @Tables t
  INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
  INNER JOIN AnmolDristi.sys.indexes i ON i.object_id=o.object_id AND i.type>0 AND i.is_primary_key=0 AND i.name IS NOT NULL
)
SELECT COALESCE(u.TableName,l.TableName) AS TableName,
  COALESCE(u.IndexName,l.IndexName) AS IndexName,
  u.KeyCols AS UAT_Keys, u.InclCols AS UAT_Incl, u.is_unique AS UAT_Unique,
  l.KeyCols AS LIVE_Keys, l.InclCols AS LIVE_Incl, l.is_unique AS LIVE_Unique,
  CASE WHEN u.IndexName IS NULL THEN N'LIVE_ONLY'
       WHEN l.IndexName IS NULL THEN N'UAT_ONLY'
       ELSE N'INDEX_DIFF' END AS DiffKind
FROM UAT u FULL OUTER JOIN LIVE l ON u.TableName=l.TableName AND u.IndexName=l.IndexName
WHERE u.IndexName IS NULL OR l.IndexName IS NULL
   OR ISNULL(u.KeyCols,N'')<>ISNULL(l.KeyCols,N'')
   OR ISNULL(u.InclCols,N'')<>ISNULL(l.InclCols,N'')
   OR ISNULL(u.is_unique,0)<>ISNULL(l.is_unique,0)
ORDER BY TableName, IndexName;

PRINT N'';
PRINT N'--- CHECK CONSTRAINTS ---';
;WITH UAT AS (
  SELECT t.TableName, cc.name AS CheckName, cc.definition
  FROM @Tables t
  INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
  INNER JOIN AnmolDristiDev.sys.check_constraints cc ON cc.parent_object_id=o.object_id
),
LIVE AS (
  SELECT t.TableName, cc.name AS CheckName, cc.definition
  FROM @Tables t
  INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
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
ORDER BY TableName, CheckName;

PRINT N'';
PRINT N'--- TRIGGERS ---';
;WITH UAT AS (
  SELECT t.TableName, tr.name AS TriggerName, tr.is_disabled
  FROM @Tables t
  INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
  INNER JOIN AnmolDristiDev.sys.triggers tr ON tr.parent_id=o.object_id
),
LIVE AS (
  SELECT t.TableName, tr.name AS TriggerName, tr.is_disabled
  FROM @Tables t
  INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
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
PRINT N'--- IDENTITY COLUMNS ---';
SELECT N'UAT' AS Env, t.TableName, c.name AS ColumnName,
  CAST(IDENT_SEED(N'AnmolDristiDev.dbo.'+t.TableName) AS BIGINT) AS Seed,
  CAST(IDENT_INCR(N'AnmolDristiDev.dbo.'+t.TableName) AS BIGINT) AS Incr,
  CAST(IDENT_CURRENT(N'AnmolDristiDev.dbo.'+t.TableName) AS BIGINT) AS CurrentVal
FROM @Tables t
INNER JOIN AnmolDristiDev.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
INNER JOIN AnmolDristiDev.sys.columns c ON c.object_id=o.object_id AND c.is_identity=1
UNION ALL
SELECT N'LIVE', t.TableName, c.name,
  CAST(IDENT_SEED(N'AnmolDristi.dbo.'+t.TableName) AS BIGINT),
  CAST(IDENT_INCR(N'AnmolDristi.dbo.'+t.TableName) AS BIGINT),
  CAST(IDENT_CURRENT(N'AnmolDristi.dbo.'+t.TableName) AS BIGINT)
FROM @Tables t
INNER JOIN AnmolDristi.sys.objects o ON o.name=t.TableName AND o.schema_id=1 AND o.type=N'U'
INNER JOIN AnmolDristi.sys.columns c ON c.object_id=o.object_id AND c.is_identity=1
ORDER BY TableName, Env;

PRINT N'===== END RC-019 SCRIPT 030C =====';
GO
