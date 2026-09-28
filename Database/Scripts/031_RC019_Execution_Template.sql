/* ============================================================================
   NAME:        RC-019 Script 031 - Generic execution template
   WHEN:        2026-09-27
   SOURCE:      docs/reviews/RC019/RC019_Foundation_Gate.md
                docs/reviews/RC019/RC019_Scope_Gate.md
   TARGET:      None. This file names no business table and no prior-release object.
   WHAT:        Reusable dry-run / preflight / preview / backup / apply skeleton.
   PRESERVE:    @DryRun defaults to 1 and returns before any backup.
                DDL and MERGE exist only as placeholders.
   NOTE:        This template authorizes no catalog change.
                Copy it to the next approved script number, then fill the
                placeholders. Leave @DryRun = 1 until that script's execution gate.

   Derived scripts open their own batch with the catalog named by that gate:

       USE [<CatalogApprovedByExecutionGate>];
       GO

   Keep everything below in one batch. A GO between the declarations and the
   apply drops @DryRun and @RunTag.

   ------------------------------------------------------------------------
   IMPLEMENTATION GUIDANCE
   ------------------------------------------------------------------------

   When to create a backup
   - Create it on the apply path only, after the dry-run return, and only
     when a change will actually run.
   - Place it before the first data change, and before DDL that replaces a
     constraint or adds an index, so the copy is the pre-change image.
   - When this script adds a column and a later statement in the same run
     writes that column, add the column after the dry-run return and before
     the backup if rollback must see the new column. Keep that ADD on the
     apply path.
   - Return before the backup when the change is already present.
   - Return before the backup when the only remaining change is blocked and
     will not be executed (for example a unique index held back by duplicate
     keys, with no other index still missing).
   - Return before the backup on a verification pass that found nothing to
     change. Leave @DryRun = 1 on that pass. Applying it would copy the table
     and then change nothing.
   - A large table still gets a backup before a real change. Set @LargeCopy = 1
     so the dry run states that cost. The dry run still creates nothing.
   - If a table with this RunTag already exists, raise a blocker and return.
     Leave the existing backup in place.
   - Name: dbo.<ReleaseToken>_Backup_<ObjectToken>_<RunTag>
     RunTag is YYYYMMDD_HHMMSS from local GETDATE().
   - Copy with SELECT * INTO <backup> FROM <target> WITH (TABLOCKX), executed
     through sp_executesql because the RunTag is known only at runtime.
     TABLOCKX holds a stable image. It is brief on a small table and heavy
     on a large log; the dry-run note is how the operator sees that cost.
   - Print the backup name and its row count.
   - Keep the backup outside the change transaction so a later rollback
     leaves the pre-image in place.

   When to use dynamic SQL
   - The backup statement, and any count or join against that backup,
     because the table name contains the runtime RunTag.
   - Any statement that references a column, constraint, or index created
     earlier in this same batch. The batch is compiled before that object
     exists, so a static reference fails compile.
   - Leave statements whose objects already exist at compile time as static SQL.
   - A preview that needs a column this run has not added yet prints a
     deferred-preview message. It does not add the column during dry run.

   When to retain LIVE-only rows
   - LIVE-only rows are keys present on the target and absent from the
     approved source. Print them in the preview. The MERGE omits
     WHEN NOT MATCHED BY SOURCE, so those rows stay.
   - Columns and indexes that exist only on the target stay. The MERGE does
     not update those columns, and this script does not drop those objects.
   - After the write, compare retained values to the backup. A difference
     is a blocker; the CATCH rolls the write back. The backup remains.
   - Rows that arrived on the target after discovery stay. Absence from the
     source is not a delete.

   When a row-count floor replaces exact equality
   - Inside this transaction, exact equality is the default
     (@PreserveCardinality = 1): the count after the change equals the count
     before it. Use that when this script adds an index, recreates a check,
     adds a column, or updates existing keys in place.
   - Set @RowCountFloor to the discovery count when the table receives new
     production rows between discovery and this run. The check is
     count >= floor. Growth above the floor is expected. A count below the
     floor is a blocker. The other environment's row count is not the target.
   - The floor does not relax the in-transaction rule. When this script must
     preserve cardinality, still require the before and after counts to match.
   - When this script inserts a known set of keys, set @PreserveCardinality = 0
     and account for those inserts in the MERGE action counts. The apply
     section still raises a blocker when the count falls below the pre-image,
     so a delete cannot hide inside the insert.
   - Use the floor for the discovery baseline. Use exact equality for what
     this script itself is allowed to do.

   Failure reporting
   - Blockers use RAISERROR severity 16, state 1, then RETURN when the
     statement is outside TRY/CATCH.
   - Inside TRY, the same RAISERROR transfers to CATCH. CATCH rolls back
     while @@TRANCOUNT > 0, then raises the captured error text.
   - Dry-run skips, already-applied skips, and an unfilled template use
     PRINT and RETURN. They are not blockers.
   - If IDENTITY_INSERT is turned on for the apply, turn it off before
     COMMIT and again in CATCH inside a nested TRY/CATCH.
   - Do not reseed an identity. Do not run DBCC CHECKIDENT.
============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

/* 1 = preview and return before backup. 0 = apply. Leave 1 in this file. */
DECLARE @DryRun BIT = 1;

/* TEMPLATE LATCH. Remains 0 in Script 031.
   A derived script sets 1 only after the DDL and MERGE placeholders below
   contain the approved statements. Until then the apply path raises a
   blocker and creates no backup. */
DECLARE @ApplyImplemented BIT = 0;

/* >>> PLACEHOLDER: target table
   Unqualified names. Schema defaults to dbo. Leave NULL in this file. */
DECLARE @TargetSchema SYSNAME = N'dbo';
DECLARE @TargetTable  SYSNAME = NULL;

/* >>> PLACEHOLDER: backup table
   The backup name is built below. Supply the two tokens, not a literal
   business table. Leave NULL in this file.
   Result: dbo.<ReleaseToken>_Backup_<ObjectToken>_<RunTag> */
DECLARE @ReleaseToken SYSNAME = NULL;
DECLARE @ObjectToken  SYSNAME = NULL;

/* >>> PLACEHOLDER: validation hook
   Companion validation script file name. Leave NULL in this file. */
DECLARE @ValidationScript NVARCHAR(260) = NULL;

/* Catalog named by the execution gate. NULL prints the banner and continues. */
DECLARE @ExpectedCatalog SYSNAME = NULL;

/* 1 = this script must not change the row count. Seed inserts set 0. */
DECLARE @PreserveCardinality BIT = 1;

/* Discovery floor. NULL skips the floor check. See the guidance above. */
DECLARE @RowCountFloor BIGINT = NULL;

/* NULL skips the index-count check. 0 means this script keeps the count. */
DECLARE @ExpectedIndexDelta INT = NULL;

/* 1 = dry run should say the apply-path copy is large. */
DECLARE @LargeCopy BIT = 0;

/* Idempotent guard outputs. Predicates that set these live in section 1. */
DECLARE @ChangeAlreadyApplied BIT = 0;
DECLARE @ChangeBlocked        BIT = 0;

DECLARE @RunTag NVARCHAR(32) = CONVERT(CHAR(8), GETDATE(), 112)
                             + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');

DECLARE @ServerName    SYSNAME = @@SERVERNAME;
DECLARE @ActualCatalog SYSNAME = DB_NAME();
DECLARE @BackupName    SYSNAME = NULL;
DECLARE @BackupTable   SYSNAME = NULL;
DECLARE @TargetTwoPart NVARCHAR(300) = NULL;
DECLARE @Sql           NVARCHAR(MAX);
DECLARE @BeforeCnt     BIGINT = NULL;
DECLARE @AfterCnt      BIGINT = NULL;
DECLARE @BackupCnt     BIGINT = NULL;
DECLARE @IndexBefore   INT = NULL;
DECLARE @IndexAfter    INT = NULL;
DECLARE @Inserted      INT = 0;
DECLARE @Updated       INT = 0;
DECLARE @BeforeText    NVARCHAR(20);
DECLARE @AfterText     NVARCHAR(20);
DECLARE @FloorText     NVARCHAR(20);
DECLARE @IdxBeforeText NVARCHAR(20);
DECLARE @IdxAfterText  NVARCHAR(20);

/* =========================================================================
   ENVIRONMENT BANNER
   RunTag uses local GETDATE() so the backup suffix is YYYYMMDD_HHMMSS.
   The UTC stamp aligns evidence across time zones.
   ========================================================================= */
PRINT N'===== RC-019 Script 031 Execution Template'
    + N' | Server=' + COALESCE(@ServerName, N'(unknown)')
    + N' | Database=' + COALESCE(@ActualCatalog, N'(unknown)')
    + N' | ExpectedCatalog=' + COALESCE(@ExpectedCatalog, N'(unset)')
    + N' | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1))
    + N' | ' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 126)
    + N' =====';

IF @ExpectedCatalog IS NOT NULL AND @ActualCatalog <> @ExpectedCatalog
BEGIN
    RAISERROR(N'BLOCKER: session database [%s] does not match expected catalog [%s]. Abort.', 16, 1, @ActualCatalog, @ExpectedCatalog);
    RETURN;
END;

IF @TargetTable IS NULL OR @ReleaseToken IS NULL OR @ObjectToken IS NULL
BEGIN
    PRINT N'Template placeholders are empty (@TargetTable, @ReleaseToken, @ObjectToken).';
    PRINT N'No backup. No DDL. No MERGE.';
    PRINT N'COMPLETE | DryRun=' + CAST(@DryRun AS NVARCHAR(1))
        + N' | Database=' + COALESCE(@ActualCatalog, N'(unknown)')
        + N' | RunTag=' + @RunTag
        + N' | backup skipped | apply skipped'
        + N' | Next=' + COALESCE(@ValidationScript, N'(validation hook not set)');
    RETURN;
END;

IF @TargetSchema LIKE N'%[^A-Za-z0-9_]%' OR @TargetSchema LIKE N'[0-9]%'
   OR @TargetTable  LIKE N'%[^A-Za-z0-9_]%' OR @TargetTable  LIKE N'[0-9]%'
   OR @ReleaseToken LIKE N'%[^A-Za-z0-9_]%' OR @ReleaseToken LIKE N'[0-9]%'
   OR @ObjectToken  LIKE N'%[^A-Za-z0-9_]%' OR @ObjectToken  LIKE N'[0-9]%'
BEGIN
    RAISERROR(N'BLOCKER: schema, table, or backup token is not a plain identifier. Abort.', 16, 1);
    RETURN;
END;

SET @BackupName = @ReleaseToken + N'_Backup_' + @ObjectToken + N'_' + @RunTag;
IF LEN(@BackupName) > 128
BEGIN
    RAISERROR(N'BLOCKER: backup name exceeds 128 characters. Abort.', 16, 1);
    RETURN;
END;

SET @BackupTable = N'dbo.' + @BackupName;
SET @TargetTwoPart = QUOTENAME(@TargetSchema) + N'.' + QUOTENAME(@TargetTable);

/* =========================================================================
   0) PREFLIGHT
   ========================================================================= */
PRINT N'--- PREFLIGHT ---';

IF OBJECT_ID(@TargetTwoPart, N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: target table [%s].[%s] not found. Abort.', 16, 1, @TargetSchema, @TargetTable);
    RETURN;
END;

/* >>> PLACEHOLDER: identity and required-column guards
   When the key is an identity this script must preserve, abort here when
   COLUMNPROPERTY(OBJECT_ID(@TargetTwoPart), N'<KeyColumn>', N'IsIdentity') <> 1.
   Abort the same way when a required shared column is missing.
   This script does not reseed and does not create a missing table. */

SET @Sql = N'SELECT @c = COUNT_BIG(*) FROM ' + @TargetTwoPart + N';';
EXEC sys.sp_executesql @Sql, N'@c BIGINT OUTPUT', @c = @BeforeCnt OUTPUT;

SELECT @IndexBefore = COUNT(*)
FROM sys.indexes
WHERE object_id = OBJECT_ID(@TargetTwoPart)
  AND index_id > 0;

PRINT N'Preflight rows=' + CAST(@BeforeCnt AS NVARCHAR(20))
    + N' indexes=' + CAST(@IndexBefore AS NVARCHAR(10))
    + N' target=' + @TargetTwoPart
    + N' intended backup=' + @BackupTable;

IF @RowCountFloor IS NOT NULL
    PRINT N'Preflight discovery floor=' + CAST(@RowCountFloor AS NVARCHAR(20))
        + N'. Growth above the floor is expected. The other environment is not the target.';

/* =========================================================================
   1) IDEMPOTENT GUARDS
   >>> PLACEHOLDER: idempotent guards
   Set @ChangeAlreadyApplied or @ChangeBlocked from the target's current
   shape before the checks below. Suggested predicates, all read-only:
   - Column, default, check, or index already in the approved form:
     skip that statement. When nothing remains, set @ChangeAlreadyApplied = 1.
   - A check definition is neither the known current form nor the approved
     form: RAISERROR and RETURN. Leave the constraint in place.
   - Existing rows would violate a check that is about to be recreated:
     print the offending groups, RAISERROR, and RETURN. Leave the constraint.
   - Duplicate keys would reject a unique index: print those rows. Set
     @ChangeBlocked = 1 when that index is the only remaining work. When
     other indexes are still missing, create those later and skip the
     unique index.
   ========================================================================= */
IF @ChangeAlreadyApplied = 1
BEGIN
    PRINT N'Change already present. No DDL. No backup.';
    PRINT N'COMPLETE | already applied | DryRun=' + CAST(@DryRun AS NVARCHAR(1))
        + N' | Database=' + COALESCE(@ActualCatalog, N'(unknown)')
        + N' | RunTag=' + @RunTag
        + N' | backup skipped'
        + N' | Next=' + COALESCE(@ValidationScript, N'(validation hook not set)');
    RETURN;
END;

IF @ChangeBlocked = 1
BEGIN
    PRINT N'Pending change is blocked. No DDL. No backup.';
    PRINT N'COMPLETE | blocked | DryRun=' + CAST(@DryRun AS NVARCHAR(1))
        + N' | Database=' + COALESCE(@ActualCatalog, N'(unknown)')
        + N' | RunTag=' + @RunTag
        + N' | backup skipped'
        + N' | Next=' + COALESCE(@ValidationScript, N'(validation hook not set)');
    RETURN;
END;

/* =========================================================================
   2) PREVIEW  (read only)
   ========================================================================= */
PRINT N'--- PREVIEW: would INSERT ---';
PRINT N'--- PREVIEW: would UPDATE ---';
PRINT N'--- PREVIEW: LIVE-only rows (RETAINED) ---';

/* >>> PLACEHOLDER: preview
   Under the three headings above, add read-only result sets:
   - source keys absent from the target (would INSERT)
   - shared keys whose approved attributes differ (would UPDATE)
   - target keys absent from the source (LIVE-only rows, RETAINED)
   When a retained column must survive the write, select it here too.
   When that column is created later in this batch, print a deferred-preview
   line and leave the column uncreated while @DryRun = 1.
   Stage the approved source in a temp table. Do not embed a sample target. */

/* =========================================================================
   3) DRY-RUN EXIT
   This return is the last statement before any backup or other write.
   ========================================================================= */
IF @DryRun = 1
BEGIN
    IF @LargeCopy = 1
        PRINT N'DryRun=1 - backup and apply skipped. The apply path copies the whole table. Review that cost before setting @DryRun = 0.';
    ELSE
        PRINT N'DryRun=1 - backup and apply skipped. Set @DryRun = 0 to apply.';

    PRINT N'COMPLETE | DryRun=1 | Database=' + COALESCE(@ActualCatalog, N'(unknown)')
        + N' | RunTag=' + @RunTag
        + N' | intended backup=' + @BackupTable
        + N' | backup not created'
        + N' | Next=' + COALESCE(@ValidationScript, N'(validation hook not set)');
    RETURN;
END;

IF @ApplyImplemented = 0
BEGIN
    RAISERROR(N'BLOCKER: DDL and MERGE placeholders are not implemented. Backup not created. Abort.', 16, 1);
    RETURN;
END;

/* =========================================================================
   4) BACKUP
   Reachable only when @DryRun = 0 and @ApplyImplemented = 1.
   ========================================================================= */
PRINT N'--- BACKUP ---';

IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
    RETURN;
END;

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM ' + @TargetTwoPart + N' WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;

SET @Sql = N'SELECT @c = COUNT_BIG(*) FROM ' + @BackupTable + N';';
EXEC sys.sp_executesql @Sql, N'@c BIGINT OUTPUT', @c = @BackupCnt OUTPUT;

PRINT N'Backup created: ' + @BackupTable
    + N' rows=' + CAST(@BackupCnt AS NVARCHAR(20));

/* =========================================================================
   5) APPLY
   ========================================================================= */
PRINT N'--- APPLY ---';

BEGIN TRY
    BEGIN TRAN;

    /* >>> PLACEHOLDER: DDL
       Replace this comment with the approved statements.
       Each statement needs an existence guard, and it is already on the
       apply path because section 3 returned when @DryRun = 1.
       Reference any object created earlier in this batch through @Sql and
       sp_executesql.
       Examples of work that belongs here, written by the derived script:
       add a missing column, add its default and check, recreate one named
       check, create a missing nonclustered index.
       Skip a unique index when the preflight showed duplicate keys.
       Leave the constraint in place when its definition is unrecognized. */

    /* >>> PLACEHOLDER: MERGE
       Replace this comment with the approved MERGE, issued through
       sp_executesql when it references an object created earlier in this
       batch. Required shape:
       - Match on the approved key.
       - WHEN MATCHED updates shared attributes only.
       - WHEN NOT MATCHED BY TARGET inserts.
       - Omit WHEN NOT MATCHED BY SOURCE. LIVE-only rows stay.
       - Omit retained columns from the UPDATE list.
       - OUTPUT $action into a temp table, then set @Inserted and @Updated.
       - For an identity key: SET IDENTITY_INSERT <target> ON around the
         MERGE and OFF before COMMIT. Reset it in the CATCH below inside
         a nested TRY/CATCH. Do not reseed.
       After the write, compare retained columns to the backup. RAISERROR
       on a difference so CATCH rolls the write back. */

    SET @Sql = N'SELECT @c = COUNT_BIG(*) FROM ' + @TargetTwoPart + N';';
    EXEC sys.sp_executesql @Sql, N'@c BIGINT OUTPUT', @c = @AfterCnt OUTPUT;

    IF @PreserveCardinality = 1 AND @AfterCnt <> @BeforeCnt
    BEGIN
        SET @BeforeText = CAST(@BeforeCnt AS NVARCHAR(20));
        SET @AfterText = CAST(@AfterCnt AS NVARCHAR(20));
        RAISERROR(N'BLOCKER: row count changed %s -> %s.', 16, 1, @BeforeText, @AfterText);
    END;

    IF @PreserveCardinality = 0 AND @AfterCnt < @BeforeCnt
    BEGIN
        SET @BeforeText = CAST(@BeforeCnt AS NVARCHAR(20));
        SET @AfterText = CAST(@AfterCnt AS NVARCHAR(20));
        RAISERROR(N'BLOCKER: row count fell %s -> %s.', 16, 1, @BeforeText, @AfterText);
    END;

    IF @RowCountFloor IS NOT NULL AND @AfterCnt < @RowCountFloor
    BEGIN
        SET @AfterText = CAST(@AfterCnt AS NVARCHAR(20));
        SET @FloorText = CAST(@RowCountFloor AS NVARCHAR(20));
        RAISERROR(N'BLOCKER: row count %s is below the discovery floor %s.', 16, 1, @AfterText, @FloorText);
    END;

    SELECT @IndexAfter = COUNT(*)
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(@TargetTwoPart)
      AND index_id > 0;

    IF @ExpectedIndexDelta IS NOT NULL
       AND (@IndexAfter - @IndexBefore) <> @ExpectedIndexDelta
    BEGIN
        SET @IdxBeforeText = CAST(@IndexBefore AS NVARCHAR(20));
        SET @IdxAfterText = CAST(@IndexAfter AS NVARCHAR(20));
        RAISERROR(N'BLOCKER: index count changed %s -> %s.', 16, 1, @IdxBeforeText, @IdxAfterText);
    END;

    COMMIT TRAN;

    /* =====================================================================
       6) COMPLETION SUMMARY
       ===================================================================== */
    PRINT N'Apply committed. Inserted=' + CAST(@Inserted AS NVARCHAR(10))
        + N' Updated=' + CAST(@Updated AS NVARCHAR(10));
    PRINT N'Rows before=' + CAST(@BeforeCnt AS NVARCHAR(20))
        + N' after=' + CAST(@AfterCnt AS NVARCHAR(20))
        + N' indexes before=' + CAST(@IndexBefore AS NVARCHAR(10))
        + N' after=' + CAST(@IndexAfter AS NVARCHAR(10));
    PRINT N'Backup for rollback: ' + @BackupTable;
    PRINT N'Next: ' + COALESCE(@ValidationScript, N'(validation hook not set)');
    PRINT N'COMPLETE | committed | DryRun=0'
        + N' | Database=' + COALESCE(@ActualCatalog, N'(unknown)')
        + N' | RunTag=' + @RunTag
        + N' | Backup=' + @BackupTable;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    /* >>> PLACEHOLDER: identity reset
       When the MERGE turned IDENTITY_INSERT on, turn it off here:

       BEGIN TRY
           SET IDENTITY_INSERT <target> OFF;
       END TRY
       BEGIN CATCH
       END CATCH;
    */

    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'Script 031 apply FAILED: %s', 16, 1, @Err);
END CATCH;
GO
