/* ============================================================================
   NAME:        RC-019 Script 031b - Generic validation template
   WHEN:        2026-09-28
   SOURCE:      Database/Scripts/031_RC019_Execution_Template.sql
   TARGET:      None. This file names no business table and no prior-release object.
   WHAT:        Read-only PASS/FAIL gate. No DML. No DDL.
   PRESERVE:    An unfilled template returns before any check and does not
                print OVERALL PASS. A configured run prints the canonical
                checklist and the OVERALL footer.
   NOTE:        Copy this file to the next approved validation script, then
                set the expectations below. Until those values are set, a
                configured target cannot finish OVERALL PASS.

   Derived scripts open their own batch with the catalog named by that gate:

       USE [<CatalogApprovedByExecutionGate>];
       GO

   Keep everything below in one batch.

   ------------------------------------------------------------------------
   HOW TO SCORE
   ------------------------------------------------------------------------
   Each canonical line has an expectation. NULL means the check is not set,
   which is a FAIL, so an unfinished copy cannot report PASS.

   Row count
   - Set @RowCountFloor when the table receives new production rows between
     discovery and this run. PASS is actual >= floor. Growth above the floor
     is expected. The other environment's row count is not the target.
   - Set @ExpectedCount, and leave @RowCountFloor NULL, when this change
     must keep an exact count.
   - Set one of the two. Setting both is a FAIL.
   - Set @BackupSchema and @BackupName together to require actual = backup
     count. Leave both NULL to skip that comparison.

   Primary key, foreign keys, defaults, checks, indexes, triggers, columns
   - The script prints the live inventory.
   - Set the matching expectation variable to score it.
   - A count of 0 is a real expectation and scores PASS when the inventory
     is empty. NULL does not mean "none".

   Duplicate keys
   - The primary-key duplicate probe always runs. It needs no column list.
   - An extra business-key probe is a placeholder. Assign @ExtraDupGroups
     there when that probe exists.

   MERGE idempotency
   - @IncludeIdempotentProbe = 0 records "not in scope" on the checklist.
   - A script that merged rows sets the bit to 1 and assigns @WouldInsert
     and @WouldUpdate. PASS is both equal to 0.

   Identity
   - Leave @IdentityColumn NULL when identity is not in scope.
   - Set it to the key column to require COLUMNPROPERTY IsIdentity = 1.

   DBCC CHECKCONSTRAINTS is read-only. It can take shared locks while it
   scans. NO_INFOMSGS matches the proven gate: no error means PASS. A
   violation grid returned above that line is still a failure.
============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

/* >>> PLACEHOLDER: target table. Unqualified names. Leave NULL in this file. */
DECLARE @TargetSchema SYSNAME = N'dbo';
DECLARE @TargetTable  SYSNAME = NULL;

/* Catalog named by the execution gate. NULL prints the banner and continues. */
DECLARE @ExpectedCatalog SYSNAME = NULL;

/* >>> PLACEHOLDER: row count. Set one. NULL and NULL is a FAIL once a target is set. */
DECLARE @RowCountFloor BIGINT = NULL; /* actual >= floor */
DECLARE @ExpectedCount BIGINT = NULL; /* actual = expected; used only when the floor is NULL */

/* >>> PLACEHOLDER: optional backup count. Set both, or leave both NULL. */
DECLARE @BackupSchema SYSNAME = NULL;
DECLARE @BackupName   SYSNAME = NULL;

/* >>> PLACEHOLDER: expectations. 0 means "none" and is scored. NULL is not set. */
DECLARE @ExpectedColumnCount     INT = NULL;
DECLARE @ExpectedPkCols          NVARCHAR(4000) = NULL; /* key order, comma-separated, no spaces */
DECLARE @ExpectedFkCount         INT = NULL;
DECLARE @RequireTrustedFks       BIT = 0; /* 1 = disabled or untrusted foreign keys FAIL */
DECLARE @ExpectedDefaultCount    INT = NULL;
DECLARE @ExpectedCheckCount      INT = NULL;
DECLARE @ExpectedNonPkIndexCount INT = NULL;
DECLARE @ExpectedTriggerCount    INT = NULL;

/* >>> PLACEHOLDER: identity. NULL = not in scope. */
DECLARE @IdentityColumn SYSNAME = NULL;

/* >>> PLACEHOLDER: MERGE idempotency. 0 = not in scope. */
DECLARE @IncludeIdempotentProbe BIT = 0;
DECLARE @WouldInsert            BIGINT = NULL;
DECLARE @WouldUpdate            BIGINT = NULL;

/* >>> PLACEHOLDER: additional uniqueness probe. Leave NULL when the PK probe is enough. */
DECLARE @ExtraDupGroups BIGINT = NULL;

DECLARE @RunTag        NVARCHAR(32) = CONVERT(CHAR(8), GETDATE(), 112)
                                   + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
DECLARE @ServerName    SYSNAME = @@SERVERNAME;
DECLARE @ActualCatalog SYSNAME = DB_NAME();
DECLARE @TargetTwoPart NVARCHAR(300);
DECLARE @BackupTwoPart NVARCHAR(300);
DECLARE @Sql           NVARCHAR(MAX);
DECLARE @Fail          INT = 0;

DECLARE @Cnt             BIGINT = NULL;
DECLARE @BakCnt          BIGINT = NULL;
DECLARE @ColumnCount     INT = NULL;
DECLARE @PkCols          NVARCHAR(4000) = NULL;
DECLARE @PkGroup         NVARCHAR(MAX) = NULL;
DECLARE @FkCount         INT = NULL;
DECLARE @UntrustedFk     INT = NULL;
DECLARE @DefaultCount    INT = NULL;
DECLARE @CheckCount      INT = NULL;
DECLARE @NonPkIndexCount INT = NULL;
DECLARE @TriggerCount    INT = NULL;
DECLARE @DupGroups       BIGINT = NULL;
DECLARE @IsIdent         INT = NULL;
DECLARE @DbccOk          BIT = 0;
DECLARE @RowOk           BIT = 0;
DECLARE @BackupOk        BIT = 0;
DECLARE @ColumnsOk       BIT = 0;
DECLARE @PkOk            BIT = 0;
DECLARE @FkOk            BIT = 0;
DECLARE @DefaultsOk      BIT = 0;
DECLARE @ChecksOk        BIT = 0;
DECLARE @IndexesOk       BIT = 0;
DECLARE @TriggersOk      BIT = 0;
DECLARE @DupOk           BIT = 0;
DECLARE @IdentOk         BIT = 0;
DECLARE @IdempotentOk    BIT = 0;

/* =========================================================================
   ENVIRONMENT BANNER
   Same shape as Script 031. Mode states that this batch does not write.
   ========================================================================= */
PRINT N'===== RC-019 Script 031b Validation Template'
    + N' | Server=' + COALESCE(@ServerName, N'(unknown)')
    + N' | Database=' + COALESCE(@ActualCatalog, N'(unknown)')
    + N' | ExpectedCatalog=' + COALESCE(@ExpectedCatalog, N'(unset)')
    + N' | RunTag=' + @RunTag
    + N' | Mode=READ-ONLY'
    + N' | ' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 126)
    + N' =====';

IF @ExpectedCatalog IS NOT NULL AND @ActualCatalog <> @ExpectedCatalog
BEGIN
    PRINT N'CHECK Session catalog | Actual=' + COALESCE(@ActualCatalog, N'(unknown)')
        + N' Expected=' + @ExpectedCatalog + N' | FAIL';
    PRINT N'';
    PRINT N'----- PASS CHECKLIST -----';
    PRINT N'Session catalog            | FAIL';
    PRINT N'';
    PRINT N'===== OVERALL: FAIL (1 checks) =====';
    RETURN;
END;

IF @TargetTable IS NULL
BEGIN
    PRINT N'Template placeholder @TargetTable is empty. No checks run.';
    RETURN;
END;

IF @TargetSchema LIKE N'%[^A-Za-z0-9_]%' OR @TargetSchema LIKE N'[0-9]%'
   OR @TargetTable  LIKE N'%[^A-Za-z0-9_]%' OR @TargetTable  LIKE N'[0-9]%'
   OR (@IdentityColumn IS NOT NULL AND (
          @IdentityColumn LIKE N'%[^A-Za-z0-9_]%' OR @IdentityColumn LIKE N'[0-9]%'))
   OR (@BackupSchema IS NOT NULL AND (
          @BackupSchema LIKE N'%[^A-Za-z0-9_]%' OR @BackupSchema LIKE N'[0-9]%'))
   OR (@BackupName IS NOT NULL AND (
          @BackupName LIKE N'%[^A-Za-z0-9_]%' OR @BackupName LIKE N'[0-9]%'))
BEGIN
    PRINT N'CHECK Identifiers | FAIL';
    PRINT N'';
    PRINT N'----- PASS CHECKLIST -----';
    PRINT N'Identifiers                | FAIL';
    PRINT N'';
    PRINT N'===== OVERALL: FAIL (1 checks) =====';
    RETURN;
END;

IF (@BackupSchema IS NULL AND @BackupName IS NOT NULL)
   OR (@BackupSchema IS NOT NULL AND @BackupName IS NULL)
BEGIN
    PRINT N'CHECK Backup name | set schema and name together | FAIL';
    PRINT N'';
    PRINT N'----- PASS CHECKLIST -----';
    PRINT N'Backup name                | FAIL';
    PRINT N'';
    PRINT N'===== OVERALL: FAIL (1 checks) =====';
    RETURN;
END;

SET @TargetTwoPart = QUOTENAME(@TargetSchema) + N'.' + QUOTENAME(@TargetTable);
IF @BackupName IS NOT NULL
    SET @BackupTwoPart = QUOTENAME(@BackupSchema) + N'.' + QUOTENAME(@BackupName);

/* =========================================================================
   0) TARGET
   ========================================================================= */
IF OBJECT_ID(@TargetTwoPart, N'U') IS NULL
BEGIN
    PRINT N'CHECK Target exists | ' + @TargetTwoPart + N' | FAIL';
    PRINT N'';
    PRINT N'----- PASS CHECKLIST -----';
    PRINT N'Target exists              | FAIL';
    PRINT N'';
    PRINT N'===== OVERALL: FAIL (1 checks) =====';
    RETURN;
END;

PRINT N'CHECK Target exists | ' + @TargetTwoPart + N' | PASS';

/* =========================================================================
   1) ROW COUNT
   ========================================================================= */
PRINT N'--- ROW COUNT ---';

SET @Sql = N'SELECT @c = COUNT_BIG(*) FROM ' + @TargetTwoPart + N';';
EXEC sys.sp_executesql @Sql, N'@c BIGINT OUTPUT', @c = @Cnt OUTPUT;

IF @RowCountFloor IS NOT NULL AND @ExpectedCount IS NOT NULL
    PRINT N'CHECK Row count | floor and exact count are both set | FAIL';
ELSE IF @RowCountFloor IS NOT NULL
BEGIN
    SET @RowOk = CASE WHEN @Cnt >= @RowCountFloor THEN 1 ELSE 0 END;
    PRINT N'CHECK Row count preserved | Actual=' + CAST(@Cnt AS NVARCHAR(20))
        + N' Floor=' + CAST(@RowCountFloor AS NVARCHAR(20)) + N' | '
        + CASE WHEN @RowOk = 1 THEN N'PASS' ELSE N'FAIL' END;
END
ELSE IF @ExpectedCount IS NOT NULL
BEGIN
    SET @RowOk = CASE WHEN @Cnt = @ExpectedCount THEN 1 ELSE 0 END;
    PRINT N'CHECK Row count | Actual=' + CAST(@Cnt AS NVARCHAR(20))
        + N' Expected=' + CAST(@ExpectedCount AS NVARCHAR(20)) + N' | '
        + CASE WHEN @RowOk = 1 THEN N'PASS' ELSE N'FAIL' END;
END
ELSE
    PRINT N'CHECK Row count | Actual=' + CAST(@Cnt AS NVARCHAR(20))
        + N' | FAIL (expectation not set)';

IF @RowOk = 0 SET @Fail += 1;

IF @BackupTwoPart IS NOT NULL
BEGIN
    IF OBJECT_ID(@BackupTwoPart, N'U') IS NULL
    BEGIN
        PRINT N'CHECK Count equals backup | backup missing | FAIL';
        SET @Fail += 1;
    END
    ELSE
    BEGIN
        SET @Sql = N'SELECT @c = COUNT_BIG(*) FROM ' + @BackupTwoPart + N';';
        EXEC sys.sp_executesql @Sql, N'@c BIGINT OUTPUT', @c = @BakCnt OUTPUT;
        SET @BackupOk = CASE WHEN @Cnt = @BakCnt THEN 1 ELSE 0 END;
        PRINT N'CHECK Count equals backup | Backup=' + CAST(@BakCnt AS NVARCHAR(20)) + N' | '
            + CASE WHEN @BackupOk = 1 THEN N'PASS' ELSE N'FAIL' END;
        IF @BackupOk = 0 SET @Fail += 1;
    END
END;

/* =========================================================================
   2) COLUMNS
   >>> PLACEHOLDER: column list and shared-attribute drift.
   Score the count here. A derived script adds the column-by-column compare
   beside this inventory and folds that result into @ColumnsOk.
   ========================================================================= */
PRINT N'--- COLUMNS ---';

SELECT @ColumnCount = COUNT(*)
FROM sys.columns
WHERE object_id = OBJECT_ID(@TargetTwoPart);

IF @ExpectedColumnCount IS NULL
    PRINT N'CHECK Column count | Actual=' + CAST(@ColumnCount AS NVARCHAR(20))
        + N' | FAIL (expectation not set)';
ELSE
BEGIN
    SET @ColumnsOk = CASE WHEN @ColumnCount = @ExpectedColumnCount THEN 1 ELSE 0 END;
    PRINT N'CHECK Column count | Actual=' + CAST(@ColumnCount AS NVARCHAR(20))
        + N' Expected=' + CAST(@ExpectedColumnCount AS NVARCHAR(20)) + N' | '
        + CASE WHEN @ColumnsOk = 1 THEN N'PASS' ELSE N'FAIL' END;
END;

IF @ColumnsOk = 0 SET @Fail += 1;

/* =========================================================================
   3) PRIMARY KEY
   >>> PLACEHOLDER: @ExpectedPkCols. Example shape: N'KeyColumn' or N'ColA,ColB'.
   ========================================================================= */
PRINT N'--- PRIMARY KEY ---';

SELECT @PkCols = STUFF((
    SELECT N',' + c.name
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(@TargetTwoPart)
      AND kc.type = N'PK'
      AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');

SELECT @PkGroup = STUFF((
    SELECT N',' + QUOTENAME(c.name)
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(@TargetTwoPart)
      AND kc.type = N'PK'
      AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');

IF @ExpectedPkCols IS NULL
    PRINT N'CHECK PK | Columns=' + ISNULL(@PkCols, N'<none>')
        + N' | FAIL (expectation not set)';
ELSE
BEGIN
    SET @PkOk = CASE WHEN ISNULL(@PkCols, N'') = @ExpectedPkCols THEN 1 ELSE 0 END;
    PRINT N'CHECK PK | Columns=' + ISNULL(@PkCols, N'<none>') + N' | '
        + CASE WHEN @PkOk = 1 THEN N'PASS' ELSE N'FAIL' END;
END;

IF @PkOk = 0 SET @Fail += 1;

IF @IdentityColumn IS NOT NULL
BEGIN
    SET @IsIdent = COLUMNPROPERTY(OBJECT_ID(@TargetTwoPart), @IdentityColumn, N'IsIdentity');
    SET @IdentOk = CASE WHEN @IsIdent = 1 THEN 1 ELSE 0 END;
    PRINT N'CHECK Identity on ' + @IdentityColumn + N' | '
        + CASE WHEN @IdentOk = 1 THEN N'PASS' ELSE N'FAIL' END;
    IF @IdentOk = 0 SET @Fail += 1;
END;

/* =========================================================================
   4) FOREIGN KEYS
   >>> PLACEHOLDER: @ExpectedFkCount. Set 0 when the approved shape has none.
   ========================================================================= */
PRINT N'--- FOREIGN KEYS ---';

SELECT
      fk.name AS ForeignKeyName
    , OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ReferencedSchema
    , OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
    , fk.is_disabled
    , fk.is_not_trusted
    , fk.delete_referential_action_desc AS OnDelete
    , fk.update_referential_action_desc AS OnUpdate
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID(@TargetTwoPart)
ORDER BY fk.name;

SELECT @FkCount = COUNT(*)
FROM sys.foreign_keys
WHERE parent_object_id = OBJECT_ID(@TargetTwoPart);

SELECT @UntrustedFk = COUNT(*)
FROM sys.foreign_keys
WHERE parent_object_id = OBJECT_ID(@TargetTwoPart)
  AND (is_disabled = 1 OR is_not_trusted = 1);

IF @ExpectedFkCount IS NULL
    PRINT N'CHECK FK count | Actual=' + CAST(@FkCount AS NVARCHAR(20))
        + N' | FAIL (expectation not set)';
ELSE IF @FkCount = @ExpectedFkCount
        AND (@RequireTrustedFks = 0 OR @UntrustedFk = 0)
    SET @FkOk = 1;

IF @ExpectedFkCount IS NOT NULL
    PRINT N'CHECK FK count | Actual=' + CAST(@FkCount AS NVARCHAR(20))
        + N' Expected=' + CAST(@ExpectedFkCount AS NVARCHAR(20))
        + N' UntrustedOrDisabled=' + CAST(@UntrustedFk AS NVARCHAR(20)) + N' | '
        + CASE WHEN @FkOk = 1 THEN N'PASS' ELSE N'FAIL' END;

IF @FkOk = 0 SET @Fail += 1;

/* =========================================================================
   5) DEFAULTS
   >>> PLACEHOLDER: @ExpectedDefaultCount. Set 0 when none are required.
   ========================================================================= */
PRINT N'--- DEFAULTS ---';

SELECT
      dc.name AS DefaultName
    , COL_NAME(dc.parent_object_id, dc.parent_column_id) AS ColumnName
    , dc.definition AS DefaultDefinition
FROM sys.default_constraints dc
WHERE dc.parent_object_id = OBJECT_ID(@TargetTwoPart)
ORDER BY dc.name;

SELECT @DefaultCount = COUNT(*)
FROM sys.default_constraints
WHERE parent_object_id = OBJECT_ID(@TargetTwoPart);

IF @ExpectedDefaultCount IS NULL
    PRINT N'CHECK Default count | Actual=' + CAST(@DefaultCount AS NVARCHAR(20))
        + N' | FAIL (expectation not set)';
ELSE
BEGIN
    SET @DefaultsOk = CASE WHEN @DefaultCount = @ExpectedDefaultCount THEN 1 ELSE 0 END;
    PRINT N'CHECK Default count | Actual=' + CAST(@DefaultCount AS NVARCHAR(20))
        + N' Expected=' + CAST(@ExpectedDefaultCount AS NVARCHAR(20)) + N' | '
        + CASE WHEN @DefaultsOk = 1 THEN N'PASS' ELSE N'FAIL' END;
END;

IF @DefaultsOk = 0 SET @Fail += 1;

/* =========================================================================
   6) CHECK CONSTRAINTS
   >>> PLACEHOLDER: @ExpectedCheckCount. Set 0 when none are required.
   For an exact definition, normalize the catalog text and compare:
       replace spaces, CR, LF, and TAB with an empty string, then
       replace =N' with ='.
   Fold a mismatch into this check before the script is approved.
   ========================================================================= */
PRINT N'--- CHECK CONSTRAINTS ---';

SELECT
      cc.name AS CheckName
    , cc.is_disabled
    , cc.is_not_trusted
    , cc.definition AS CheckDefinition
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID(@TargetTwoPart)
ORDER BY cc.name;

SELECT @CheckCount = COUNT(*)
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(@TargetTwoPart);

IF @ExpectedCheckCount IS NULL
    PRINT N'CHECK Check count | Actual=' + CAST(@CheckCount AS NVARCHAR(20))
        + N' | FAIL (expectation not set)';
ELSE
BEGIN
    SET @ChecksOk = CASE WHEN @CheckCount = @ExpectedCheckCount THEN 1 ELSE 0 END;
    PRINT N'CHECK Check count | Actual=' + CAST(@CheckCount AS NVARCHAR(20))
        + N' Expected=' + CAST(@ExpectedCheckCount AS NVARCHAR(20)) + N' | '
        + CASE WHEN @ChecksOk = 1 THEN N'PASS' ELSE N'FAIL' END;
END;

IF @ChecksOk = 0 SET @Fail += 1;

/* =========================================================================
   7) INDEXES
   >>> PLACEHOLDER: @ExpectedNonPkIndexCount. The primary key is separate.
   Set 0 when no non-primary index is required. Name, uniqueness, and
   filter checks belong in the derived script beside this inventory.
   ========================================================================= */
PRINT N'--- INDEXES ---';

SELECT
      i.index_id
    , i.name AS IndexName
    , i.is_unique
    , i.is_primary_key
    , i.has_filter
    , i.filter_definition
    , STUFF((
        SELECT N',' + c.name
        FROM sys.index_columns ic
        INNER JOIN sys.columns c
            ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE ic.object_id = i.object_id
          AND ic.index_id = i.index_id
          AND ic.is_included_column = 0
        ORDER BY ic.key_ordinal
        FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'') AS KeyColumns
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(@TargetTwoPart)
  AND i.index_id > 0
  AND i.is_hypothetical = 0
ORDER BY i.index_id;

SELECT @NonPkIndexCount = COUNT(*)
FROM sys.indexes
WHERE object_id = OBJECT_ID(@TargetTwoPart)
  AND index_id > 0
  AND is_primary_key = 0
  AND is_hypothetical = 0;

IF @ExpectedNonPkIndexCount IS NULL
    PRINT N'CHECK Non-PK index count | Actual=' + CAST(@NonPkIndexCount AS NVARCHAR(20))
        + N' | FAIL (expectation not set)';
ELSE
BEGIN
    SET @IndexesOk = CASE WHEN @NonPkIndexCount = @ExpectedNonPkIndexCount THEN 1 ELSE 0 END;
    PRINT N'CHECK Non-PK index count | Actual=' + CAST(@NonPkIndexCount AS NVARCHAR(20))
        + N' Expected=' + CAST(@ExpectedNonPkIndexCount AS NVARCHAR(20)) + N' | '
        + CASE WHEN @IndexesOk = 1 THEN N'PASS' ELSE N'FAIL' END;
END;

IF @IndexesOk = 0 SET @Fail += 1;

/* =========================================================================
   8) DUPLICATE KEYS
   >>> PLACEHOLDER: additional uniqueness probe.
   Assign @ExtraDupGroups from the approved business key. Leave NULL when
   the primary-key probe below is the whole check.
   ========================================================================= */
PRINT N'--- DUPLICATE KEYS ---';

IF @PkGroup IS NULL
BEGIN
    PRINT N'CHECK Duplicate PK groups | FAIL (no primary key)';
END
ELSE
BEGIN
    SET @Sql = N'SELECT @d = COUNT_BIG(*) FROM (SELECT 1 AS Marker FROM ' + @TargetTwoPart
             + N' GROUP BY ' + @PkGroup + N' HAVING COUNT_BIG(*) > 1) AS d;';
    EXEC sys.sp_executesql @Sql, N'@d BIGINT OUTPUT', @d = @DupGroups OUTPUT;
    SET @DupOk = CASE WHEN @DupGroups = 0 THEN 1 ELSE 0 END;
    PRINT N'CHECK Duplicate PK groups | ' + CAST(@DupGroups AS NVARCHAR(20)) + N' | '
        + CASE WHEN @DupOk = 1 THEN N'PASS' ELSE N'FAIL' END;
END;

IF @DupOk = 0 SET @Fail += 1;

IF @ExtraDupGroups IS NOT NULL
BEGIN
    PRINT N'CHECK Additional duplicate groups | ' + CAST(@ExtraDupGroups AS NVARCHAR(20)) + N' | '
        + CASE WHEN @ExtraDupGroups = 0 THEN N'PASS' ELSE N'FAIL' END;
    IF @ExtraDupGroups <> 0 SET @Fail += 1;
END;

/* =========================================================================
   9) DBCC CHECKCONSTRAINTS
   ========================================================================= */
PRINT N'--- DBCC CHECKCONSTRAINTS ---';

BEGIN TRY
    SET @Sql = N'DBCC CHECKCONSTRAINTS (N''' + @TargetSchema + N'.' + @TargetTable + N''') WITH NO_INFOMSGS;';
    EXEC sys.sp_executesql @Sql;
    SET @DbccOk = 1;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
END CATCH;

IF @DbccOk = 0 SET @Fail += 1;

/* =========================================================================
   10) TRIGGER INVENTORY
   >>> PLACEHOLDER: @ExpectedTriggerCount. Set 0 when none are approved.
   ========================================================================= */
PRINT N'--- TRIGGER INVENTORY ---';

SELECT
      tr.name AS TriggerName
    , tr.is_disabled
    , tr.is_instead_of_trigger
FROM sys.triggers tr
WHERE tr.parent_id = OBJECT_ID(@TargetTwoPart)
ORDER BY tr.name;

SELECT @TriggerCount = COUNT(*)
FROM sys.triggers
WHERE parent_id = OBJECT_ID(@TargetTwoPart);

IF @ExpectedTriggerCount IS NULL
    PRINT N'CHECK Triggers | Actual=' + CAST(@TriggerCount AS NVARCHAR(20))
        + N' | FAIL (expectation not set)';
ELSE
BEGIN
    SET @TriggersOk = CASE WHEN @TriggerCount = @ExpectedTriggerCount THEN 1 ELSE 0 END;
    PRINT N'CHECK Triggers | Actual=' + CAST(@TriggerCount AS NVARCHAR(20))
        + N' Expected=' + CAST(@ExpectedTriggerCount AS NVARCHAR(20)) + N' | '
        + CASE WHEN @TriggersOk = 1 THEN N'PASS' ELSE N'FAIL' END;
END;

IF @TriggersOk = 0 SET @Fail += 1;

/* =========================================================================
   11) MERGE IDEMPOTENCY
   >>> PLACEHOLDER: when @IncludeIdempotentProbe = 1, set @WouldInsert and
   @WouldUpdate from the approved source. PASS is both zero.
   ========================================================================= */
PRINT N'--- MERGE IDEMPOTENCY ---';

IF @IncludeIdempotentProbe = 0
BEGIN
    SET @IdempotentOk = 1;
    PRINT N'CHECK Idempotent | not in scope | PASS';
END
ELSE IF @WouldInsert IS NULL OR @WouldUpdate IS NULL
    PRINT N'CHECK Idempotent | probe not set | FAIL';
ELSE
BEGIN
    SET @IdempotentOk = CASE WHEN @WouldInsert = 0 AND @WouldUpdate = 0 THEN 1 ELSE 0 END;
    PRINT N'CHECK Idempotent | WouldInsert=' + CAST(@WouldInsert AS NVARCHAR(20))
        + N' WouldUpdate=' + CAST(@WouldUpdate AS NVARCHAR(20)) + N' | '
        + CASE WHEN @IdempotentOk = 1 THEN N'PASS' ELSE N'FAIL' END;
END;

IF @IdempotentOk = 0 SET @Fail += 1;

/* =========================================================================
   CANONICAL PASS CHECKLIST
   ========================================================================= */
PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'Columns Match              | ' + CASE WHEN @ColumnsOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK Match                   | ' + CASE WHEN @PkOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'FK Match                   | ' + CASE WHEN @FkOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Defaults Match             | ' + CASE WHEN @DefaultsOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Check Constraint           | ' + CASE WHEN @ChecksOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Indexes Match              | ' + CASE WHEN @IndexesOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Triggers Match             | ' + CASE WHEN @TriggersOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Row Count                  | ' + CASE WHEN @RowOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'No Duplicate Keys          | ' + CASE WHEN @DupOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'DBCC CHECKCONSTRAINTS      | ' + CASE WHEN @DbccOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'MERGE Idempotent           | ' + CASE WHEN @IdempotentOk = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @IdentityColumn IS NOT NULL
    PRINT N'Identity preserved         | ' + CASE WHEN @IdentOk = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @BackupTwoPart IS NOT NULL
    PRINT N'Count equals backup        | ' + CASE WHEN @BackupOk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'';

/* =========================================================================
   CANONICAL OVERALL FOOTER
   A derived script may append the next-package stop text after this line.
   ========================================================================= */
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) =====';
GO
