/* ============================================================================
   NAME:        RC-019 Script 031c - Generic rollback template
   WHEN:        2026-09-28
   SOURCE:      Database/Scripts/031_RC019_Execution_Template.sql
                Database/Scripts/031b_RC019_Validation_Template.sql
   TARGET:      None. This file names no business table and no prior-release object.
   WHAT:        Reusable confirm / backup-check / restore skeleton.
   PRESERVE:    @Confirm defaults to 0 and returns before any write.
                The named backup is never dropped or updated.
   NOTE:        This template authorizes no catalog change.
                Copy it to the next approved rollback script, then fill the
                placeholders. Leave @Confirm = 0 until that script's
                execution gate. Leave @RollbackImplemented = 0 until the
                placeholders contain the approved statements.

   Derived scripts open their own batch with the catalog named by that gate:

       USE [<CatalogApprovedByExecutionGate>];
       GO

   Keep everything below in one batch. A GO here drops @Confirm.

   ------------------------------------------------------------------------
   SAFEGUARDS
   ------------------------------------------------------------------------
   Confirm
   - @Confirm = 0 prints the intended backup and returns. It creates no
     safety copy and runs no restore.

   Backup
   - The operator sets @BackupSchema and @BackupName to the copy the apply
     script printed. This template does not build that name from the clock.
   - The backup must already exist. A missing backup is a blocker and
     returns before any rollback.
   - The backup name must differ from the target name.
   - After commit, and after CATCH, the backup table is still there.
     CATCH rolls back the open transaction only. It does not drop the backup.

   Safety copy
   - @TakeSafetyCopy = 1 copies the target before the restore transaction.
     The copy is its own statement, so a later rollback of the restore
     leaves that pre-image in place. The original backup also stays.
   - @TakeSafetyCopy = 0 is for a rollback that only restores a check or
     drops indexes and must not copy the table.
   - Name: dbo.<ReleaseToken>_PreRollback_<ObjectToken>_<RunTag>
     RunTag is the rollback clock, YYYYMMDD_HHMMSS. It is not the apply tag
     embedded in the backup name.
   - If that safety name already exists, raise a blocker and return.

   Row restore
   - Update keys that exist in both the target and the backup.
   - Insert backup keys that are missing from the target.
   - Delete only target keys that are absent from the backup.
   - Do not truncate. Do not delete retained rows the backup also contains.
   - Leave retained columns and retained indexes out of the restore.
   - Set @RequireCountEqualsBackup = 1. PASS is target count = backup count.
   - Issue the statements through sp_executesql. The backup name is runtime.

   Constraint restore
   - Before any write, count rows that would violate the restored check.
     Assign that count to @BlockerRows. A count above zero returns and
     leaves the constraint in place.
   - Inside the transaction, drop the constraint only when it exists, then
     add the prior definition WITH CHECK.
   - Set @PreserveCardinality = 1. The row count must be unchanged.

   Index restore
   - Drop only an index this change created, and only when it exists.
   - Do not drop the primary key.
   - Do not delete or update rows on an index-only rollback.
   - Set @PreserveCardinality = 1.

   Identity
   - Turn IDENTITY_INSERT on only around the re-insert of an identity key,
     and off before COMMIT.
   - Turn it off again in CATCH, inside a nested TRY/CATCH.
   - Do not reseed. Do not run DBCC CHECKIDENT.

   Failure reporting
   - Blockers use RAISERROR severity 16, state 1, then RETURN outside TRY.
   - Inside TRY, the same RAISERROR transfers to CATCH. CATCH rolls back
     while @@TRANCOUNT > 0, then raises the captured error text.
   - Confirm = 0 uses PRINT and RETURN.
============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

/* 0 = print and return. 1 = rollback. Leave 0 in this file. */
DECLARE @Confirm BIT = 0;

/* TEMPLATE LATCH. Remains 0 in Script 031c.
   A derived script sets 1 only after the restore placeholders contain the
   approved statements. Until then Confirm = 1 raises a blocker and writes
   nothing. */
DECLARE @RollbackImplemented BIT = 0;

/* >>> PLACEHOLDER: target table. Unqualified names. Leave NULL in this file. */
DECLARE @TargetSchema SYSNAME = N'dbo';
DECLARE @TargetTable  SYSNAME = NULL;

/* >>> PLACEHOLDER: backup table.
   Set both to the apply script's backup. Leave NULL in this file.
   This is the explicit name. It is not generated here. */
DECLARE @BackupSchema SYSNAME = NULL;
DECLARE @BackupName   SYSNAME = NULL;

/* Safety-copy tokens. Required only when @TakeSafetyCopy = 1. */
DECLARE @ReleaseToken SYSNAME = NULL;
DECLARE @ObjectToken  SYSNAME = NULL;

/* 1 = copy the target before the restore transaction. 0 = DDL-only rollback. */
DECLARE @TakeSafetyCopy BIT = 1;

/* 1 = row count after the restore must equal the count before it. */
DECLARE @PreserveCardinality BIT = 1;

/* 1 = row count after the restore must equal the backup count. */
DECLARE @RequireCountEqualsBackup BIT = 0;

/* >>> PLACEHOLDER: rows that would violate a restored check or unique index.
   Leave NULL when this rollback does not tighten a rule.
   The derived script assigns the count. A value above zero aborts. */
DECLARE @BlockerRows BIGINT = NULL;

/* >>> PLACEHOLDER: validation hook. Companion validation script file name. */
DECLARE @ValidationScript NVARCHAR(260) = NULL;

/* Catalog named by the execution gate. NULL prints the banner and continues. */
DECLARE @ExpectedCatalog SYSNAME = NULL;

DECLARE @RunTag        NVARCHAR(32) = CONVERT(CHAR(8), GETDATE(), 112)
                                   + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
DECLARE @ServerName    SYSNAME = @@SERVERNAME;
DECLARE @ActualCatalog SYSNAME = DB_NAME();
DECLARE @TargetTwoPart NVARCHAR(300);
DECLARE @BackupTwoPart NVARCHAR(300);
DECLARE @SafetyName    SYSNAME;
DECLARE @SafetyTwoPart NVARCHAR(300);
DECLARE @Sql           NVARCHAR(MAX);
DECLARE @BeforeCnt     BIGINT = NULL;
DECLARE @AfterCnt      BIGINT = NULL;
DECLARE @BakCnt        BIGINT = NULL;
DECLARE @SafetyCnt     BIGINT = NULL;
DECLARE @BeforeText    NVARCHAR(20);
DECLARE @AfterText     NVARCHAR(20);
DECLARE @BakText       NVARCHAR(20);
DECLARE @BlockerText   NVARCHAR(20);

/* =========================================================================
   ENVIRONMENT BANNER
   ========================================================================= */
PRINT N'===== RC-019 Script 031c Rollback Template'
    + N' | Server=' + COALESCE(@ServerName, N'(unknown)')
    + N' | Database=' + COALESCE(@ActualCatalog, N'(unknown)')
    + N' | ExpectedCatalog=' + COALESCE(@ExpectedCatalog, N'(unset)')
    + N' | RunTag=' + @RunTag
    + N' | Confirm=' + CAST(@Confirm AS NVARCHAR(1))
    + N' | ' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 126)
    + N' =====';

IF @ExpectedCatalog IS NOT NULL AND @ActualCatalog <> @ExpectedCatalog
BEGIN
    RAISERROR(N'BLOCKER: session database [%s] does not match expected catalog [%s]. Abort.', 16, 1, @ActualCatalog, @ExpectedCatalog);
    RETURN;
END;

IF @TargetTable IS NULL OR @BackupSchema IS NULL OR @BackupName IS NULL
BEGIN
    IF @Confirm = 1
    BEGIN
        RAISERROR(N'BLOCKER: @TargetTable, @BackupSchema, and @BackupName must be set before execution. Abort.', 16, 1);
        RETURN;
    END;

    PRINT N'Confirm=0 - no changes. Set @TargetTable, @BackupSchema, and @BackupName before execution.';
    PRINT N'Next: ' + COALESCE(@ValidationScript, N'(validation hook not set)');
    RETURN;
END;

IF @TargetSchema LIKE N'%[^A-Za-z0-9_]%' OR @TargetSchema LIKE N'[0-9]%'
   OR @TargetTable  LIKE N'%[^A-Za-z0-9_]%' OR @TargetTable  LIKE N'[0-9]%'
   OR @BackupSchema LIKE N'%[^A-Za-z0-9_]%' OR @BackupSchema LIKE N'[0-9]%'
   OR @BackupName   LIKE N'%[^A-Za-z0-9_]%' OR @BackupName   LIKE N'[0-9]%'
   OR (@TakeSafetyCopy = 1 AND @ReleaseToken IS NOT NULL AND (
          @ReleaseToken LIKE N'%[^A-Za-z0-9_]%' OR @ReleaseToken LIKE N'[0-9]%'))
   OR (@TakeSafetyCopy = 1 AND @ObjectToken IS NOT NULL AND (
          @ObjectToken LIKE N'%[^A-Za-z0-9_]%' OR @ObjectToken LIKE N'[0-9]%'))
BEGIN
    RAISERROR(N'BLOCKER: schema, table, or token is not a plain identifier. Abort.', 16, 1);
    RETURN;
END;

SET @TargetTwoPart = QUOTENAME(@TargetSchema) + N'.' + QUOTENAME(@TargetTable);
SET @BackupTwoPart = QUOTENAME(@BackupSchema) + N'.' + QUOTENAME(@BackupName);

IF @TargetTwoPart = @BackupTwoPart
BEGIN
    RAISERROR(N'BLOCKER: backup name equals the target name. Abort.', 16, 1);
    RETURN;
END;

/* =========================================================================
   CONFIRM GATE
   ========================================================================= */
IF @Confirm <> 1
BEGIN
    PRINT N'Confirm=0 - no changes. Backup=' + @BackupTwoPart + N' Target=' + @TargetTwoPart;
    PRINT N'Next: ' + COALESCE(@ValidationScript, N'(validation hook not set)');
    RETURN;
END;

IF OBJECT_ID(@TargetTwoPart, N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: target table [%s].[%s] not found. Abort.', 16, 1, @TargetSchema, @TargetTable);
    RETURN;
END;

/* =========================================================================
   BACKUP EXISTS
   This check is the last gate before any rollback write.
   ========================================================================= */
IF OBJECT_ID(@BackupTwoPart, N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table [%s].[%s] not found. Abort.', 16, 1, @BackupSchema, @BackupName);
    RETURN;
END;

PRINT N'Backup found: ' + @BackupTwoPart;

IF @RollbackImplemented = 0
BEGIN
    RAISERROR(N'BLOCKER: rollback placeholders are not implemented. Backup not modified. No safety copy. Abort.', 16, 1);
    RETURN;
END;

/* >>> PLACEHOLDER: constraint pre-check
   When the restored check or unique index is tighter than the current
   rule, set @BlockerRows to the number of rows that would fail it.
   Return here, before the safety copy, when that count is above zero. */

IF @BlockerRows IS NOT NULL AND @BlockerRows > 0
BEGIN
    SET @BlockerText = CAST(@BlockerRows AS NVARCHAR(20));
    RAISERROR(N'BLOCKER: %s rows would violate the restored rule. No change made.', 16, 1, @BlockerText);
    RETURN;
END;

/* =========================================================================
   SAFETY COPY
   Outside the restore transaction. CATCH does not drop it.
   The original backup is not the safety copy and is not replaced.
   ========================================================================= */
IF @TakeSafetyCopy = 1
BEGIN
    IF @ReleaseToken IS NULL OR @ObjectToken IS NULL
    BEGIN
        RAISERROR(N'BLOCKER: @ReleaseToken and @ObjectToken are required for the safety copy. Abort.', 16, 1);
        RETURN;
    END;

    IF @ReleaseToken LIKE N'%[^A-Za-z0-9_]%' OR @ReleaseToken LIKE N'[0-9]%'
       OR @ObjectToken LIKE N'%[^A-Za-z0-9_]%' OR @ObjectToken LIKE N'[0-9]%'
    BEGIN
        RAISERROR(N'BLOCKER: safety-copy token is not a plain identifier. Abort.', 16, 1);
        RETURN;
    END;

    SET @SafetyName = @ReleaseToken + N'_PreRollback_' + @ObjectToken + N'_' + @RunTag;
    IF LEN(@SafetyName) > 128
    BEGIN
        RAISERROR(N'BLOCKER: safety-copy name exceeds 128 characters. Abort.', 16, 1);
        RETURN;
    END;

    SET @SafetyTwoPart = QUOTENAME(N'dbo') + N'.' + QUOTENAME(@SafetyName);
    IF (@BackupSchema = N'dbo' AND @BackupName = @SafetyName)
       OR (@TargetSchema = N'dbo' AND @TargetTable = @SafetyName)
    BEGIN
        RAISERROR(N'BLOCKER: safety-copy name collides with the backup or the target. Abort.', 16, 1);
        RETURN;
    END;

    IF OBJECT_ID(@SafetyTwoPart, N'U') IS NOT NULL
    BEGIN
        RAISERROR(N'BLOCKER: safety copy already exists for this RunTag. Abort.', 16, 1);
        RETURN;
    END;
    SET @Sql = N'SELECT * INTO ' + @SafetyTwoPart + N' FROM ' + @TargetTwoPart + N';';
    EXEC sys.sp_executesql @Sql;

    SET @Sql = N'SELECT @c = COUNT_BIG(*) FROM ' + @SafetyTwoPart + N';';
    EXEC sys.sp_executesql @Sql, N'@c BIGINT OUTPUT', @c = @SafetyCnt OUTPUT;
    PRINT N'Pre-rollback safety copy: ' + @SafetyTwoPart
        + N' rows=' + CAST(@SafetyCnt AS NVARCHAR(20));
END;

/* =========================================================================
   RESTORE
   ========================================================================= */
BEGIN TRY
    BEGIN TRAN;

    SET @Sql = N'SELECT @c = COUNT_BIG(*) FROM ' + @TargetTwoPart + N';';
    EXEC sys.sp_executesql @Sql, N'@c BIGINT OUTPUT', @c = @BeforeCnt OUTPUT;

    /* >>> PLACEHOLDER: DELETE/INSERT restoration
       Replace this comment with dynamic SQL against @BackupTwoPart.
       Required shape, in this order:
       1. UPDATE target rows whose key is in the backup. Set only the
          columns this change owns. Leave retained columns out of the SET list.
       2. INSERT backup rows whose key is missing from the target.
          When the key is an identity, the identity placeholder below wraps
          this INSERT only.
       3. DELETE target rows whose key is absent from the backup.
          This is the scoped delete. Do not truncate. Do not delete a row
          the backup also contains.
       Read the backup. Do not update, delete, or drop it. */

    /* >>> PLACEHOLDER: identity handling
       Around the INSERT above, and only when the key is an identity:

           SET IDENTITY_INSERT <target> ON;
           -- INSERT ...
           SET IDENTITY_INSERT <target> OFF;

       Turn it off before COMMIT. The CATCH below turns it off again.
       Do not reseed. Do not run DBCC CHECKIDENT. */

    /* >>> PLACEHOLDER: constraint restoration
       Drop the constraint only when sys.check_constraints (or
       sys.foreign_keys / sys.default_constraints) shows that name on the
       target, then add the prior definition WITH CHECK.
       A column drop, when required, follows its check and its default,
       and runs only after those drops. Prefer that DDL inside this
       transaction. Do not drop the backup. */

    /* >>> PLACEHOLDER: index restoration
       Drop an index only when sys.indexes shows that name on the target.
       Drop only an index this change created. Do not drop the primary key.
       An index-only rollback does not delete or update rows. */

    SET @Sql = N'SELECT @c = COUNT_BIG(*) FROM ' + @TargetTwoPart + N';';
    EXEC sys.sp_executesql @Sql, N'@c BIGINT OUTPUT', @c = @AfterCnt OUTPUT;

    IF @PreserveCardinality = 1 AND @AfterCnt <> @BeforeCnt
    BEGIN
        SET @BeforeText = CAST(@BeforeCnt AS NVARCHAR(20));
        SET @AfterText = CAST(@AfterCnt AS NVARCHAR(20));
        RAISERROR(N'BLOCKER: rollback changed row count %s -> %s.', 16, 1, @BeforeText, @AfterText);
    END;

    IF @RequireCountEqualsBackup = 1
    BEGIN
        SET @Sql = N'SELECT @c = COUNT_BIG(*) FROM ' + @BackupTwoPart + N';';
        EXEC sys.sp_executesql @Sql, N'@c BIGINT OUTPUT', @c = @BakCnt OUTPUT;
        IF @AfterCnt <> @BakCnt
        BEGIN
            SET @AfterText = CAST(@AfterCnt AS NVARCHAR(20));
            SET @BakText = CAST(@BakCnt AS NVARCHAR(20));
            RAISERROR(N'ROLLBACK VERIFY FAIL: live %s <> backup %s.', 16, 1, @AfterText, @BakText);
        END;
    END;

    COMMIT TRAN;

    /* =====================================================================
       ROLLBACK SUMMARY
       The backup object is not dropped here.
       ===================================================================== */
    IF OBJECT_ID(@BackupTwoPart, N'U') IS NULL
        RAISERROR(N'BLOCKER: backup table is missing after rollback.', 16, 1);

    PRINT N'ROLLBACK committed. Rows before=' + CAST(@BeforeCnt AS NVARCHAR(20))
        + N' after=' + CAST(@AfterCnt AS NVARCHAR(20));
    PRINT N'Backup left intact: ' + @BackupTwoPart;
    IF @TakeSafetyCopy = 1
        PRINT N'Safety copy: ' + @SafetyTwoPart;
    ELSE
        PRINT N'Safety copy: not taken.';
    PRINT N'Next: ' + COALESCE(@ValidationScript, N'(validation hook not set)');
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    /* >>> PLACEHOLDER: identity reset
       When the restore turned IDENTITY_INSERT on, turn it off here:

       BEGIN TRY
           SET IDENTITY_INSERT <target> OFF;
       END TRY
       BEGIN CATCH
       END CATCH;

       Do not drop the backup. Do not drop the safety copy.
    */

    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'Script 031c rollback FAILED: %s', 16, 1, @Err);
END CATCH;
GO
