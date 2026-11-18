/*
    Package:  TA/DA LIVE Go-Live 20261117
    File:     20_WP01_Schema.sql
    Change:   three nullable columns on dbo.SalesDailyActivityKMHistory
    Writes:   one evidence row and those three columns
    Does not create the evidence table. Does not update or delete history rows.
    This revision aborts. Gate literals are UNSIGNED.
    Apply requires the three names to be absent. A later run changes nothing
    only when the evidence row and the exact column postcondition both match.

    2026-11-18: the three columns are already on AnmolDristi and match Dev.
    Do not sign the gates in order to run this file.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @GateBackup nvarchar(40) = N'UNSIGNED';
DECLARE @GateVerify nvarchar(40) = N'UNSIGNED';
DECLARE @GateIsolated nvarchar(40) = N'UNSIGNED';
DECLARE @BackupFinish datetime = NULL;
DECLARE @IsolatedRestoreDatabase sysname = N'';
DECLARE @GateIdentity nvarchar(40) = N'UNSIGNED';
DECLARE @ApprovedLogin sysname = N'';
DECLARE @GateColumns nvarchar(40) = N'UNSIGNED';
DECLARE @GateNoFk nvarchar(40) = N'UNSIGNED';

BEGIN TRY
    IF UPPER(CONVERT(nvarchar(256), SERVERPROPERTY('ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
        THROW 59501, N'ABORT: server is not MUMANMOAPP01\SQLEXPRESS.', 1;
    IF DB_NAME() = N'AnmolDristiDev'
        THROW 59502, N'ABORT: AnmolDristiDev is refused.', 1;
    IF DB_NAME() <> N'AnmolDristi'
        THROW 59502, N'ABORT: database is not AnmolDristi.', 1;
    IF @@TRANCOUNT <> 0
        THROW 59503, N'ABORT: a transaction is already open.', 1;
    IF DATABASEPROPERTYEX(DB_NAME(), N'Updateability') <> N'READ_WRITE'
        THROW 59502, N'ABORT: database is not read-write.', 1;

    IF @GateColumns <> N'DR-COLUMNS-A' OR @GateNoFk <> N'NO-FK-NO-INDEX'
        THROW 59504, N'ABORT: DR-COLUMNS is unsigned in this revision.', 1;
    IF @GateBackup <> N'DR-BACKUP-A'
        OR @GateVerify <> N'VERIFYONLY-CHECKSUM-ACCEPTED'
        OR @GateIsolated <> N'ISOLATED-RESTORE-ACCEPTED'
        OR @BackupFinish IS NULL
        OR NULLIF(@IsolatedRestoreDatabase, N'') IS NULL
        OR @IsolatedRestoreDatabase IN (N'AnmolDristi', N'AnmolDristiDev')
        THROW 59506, N'ABORT: DR-BACKUP is unsigned in this revision.', 1;
    IF @GateIdentity <> N'DR-IDENTITY-A'
        OR NULLIF(@ApprovedLogin, N'') IS NULL
        OR @ApprovedLogin = N'sa'
        OR SUSER_SNAME() <> @ApprovedLogin
        OR ORIGINAL_LOGIN() = N'sa'
        OR SUSER_SNAME() = N'sa'
        OR IS_SRVROLEMEMBER(N'sysadmin') = 1
        OR IS_ROLEMEMBER(N'db_owner') = 1
        OR IS_ROLEMEMBER(N'db_ddladmin') = 1
        OR ISNULL(HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'ALTER ANY PROCEDURE'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo', N'SCHEMA', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo', N'SCHEMA', N'CONTROL'), 0) = 1
        THROW 59505, N'ABORT: deployment identity is unsigned or wider than this package.', 1;

    IF OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'U') IS NULL
        THROW 59512, N'ABORT: evidence table is absent. This identity does not create it.', 1;
    IF (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage')) <> 13
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'WorkPackage' AND TYPE_NAME(user_type_id) = N'varchar' AND max_length = 8 AND is_nullable = 0)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'ObjectName' AND TYPE_NAME(user_type_id) IN (N'sysname', N'nvarchar') AND max_length = 256 AND is_nullable = 0)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'PreimageSha256' AND TYPE_NAME(user_type_id) = N'char' AND max_length = 64 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'PreimageLen' AND TYPE_NAME(user_type_id) = N'int' AND max_length = 4 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'PreimageDataLength' AND TYPE_NAME(user_type_id) = N'int' AND max_length = 4 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'ModuleDefinition' AND TYPE_NAME(user_type_id) = N'nvarchar' AND max_length = -1 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'SubmittedSha256' AND TYPE_NAME(user_type_id) = N'char' AND max_length = 64 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'AppliedSha256' AND TYPE_NAME(user_type_id) = N'char' AND max_length = 64 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'HistoryRowCount' AND TYPE_NAME(user_type_id) = N'bigint' AND max_length = 8 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'HistoryIdMin' AND TYPE_NAME(user_type_id) = N'int' AND max_length = 4 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'HistoryIdSum' AND TYPE_NAME(user_type_id) = N'bigint' AND max_length = 8 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'SchemaSnapshot' AND TYPE_NAME(user_type_id) = N'nvarchar' AND max_length = -1 AND is_nullable = 1)
        OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'CapturedAtUtc' AND TYPE_NAME(user_type_id) = N'datetime2' AND scale = 3 AND is_nullable = 0)
        OR NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'PK_TaDaLiveGoLive_20261117_Preimage' AND type = 'PK')
        OR (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage')) <> 1
        OR (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage')) <> 1
        OR NOT EXISTS (
            SELECT 1 FROM sys.check_constraints
            WHERE parent_object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage')
              AND name = N'CK_TaDaLiveGoLive_20261117_WorkPackage'
              AND definition LIKE N'%WP01%' AND definition LIKE N'%WP02%' AND definition NOT LIKE N'%WP03%'
        )
        OR (SELECT COUNT(*) FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage')) <> 1
        OR NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints AS dc
            INNER JOIN sys.columns AS col ON col.object_id = dc.parent_object_id AND col.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage')
              AND dc.name = N'DF_TaDaLiveGoLive_20261117_Captured'
              AND col.name = N'CapturedAtUtc'
              AND LOWER(dc.definition) LIKE N'%sysutcdatetime%'
        )
        OR EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage'))
        THROW 59512, N'ABORT: evidence table shape is not the WP-01/WP-02 definition.', 1;

    IF ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'ALTER'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'SELECT'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'INSERT'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'UPDATE'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'DELETE'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo', N'SCHEMA', N'VIEW DEFINITION'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'SELECT'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'INSERT'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'DELETE'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'msdb.dbo.backupset', N'OBJECT', N'SELECT'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateApprovalRequest', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateApprovalRequest', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateSalesClaim', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateSalesClaim', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_GetResolvedPolicy', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ResolveDesignation', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ResolveRegion', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ResolveBranch', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ResolveGrade', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ProcessApprovalAction', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_MarkRequestReadyForPayment', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ForwardApprovalRequest', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_UpdateActivityKM', N'OBJECT', N'ALTER'), 0) = 1
        THROW 59507, N'ABORT: object rights do not match the WP-01/WP-02 permission matrix.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM msdb.dbo.backupset AS b
        WHERE b.database_name = N'AnmolDristi'
          AND b.type = 'D'
          AND b.is_damaged = 0
          AND b.has_backup_checksums = 1
          AND b.backup_finish_date = @BackupFinish
    )
        THROW 59506, N'ABORT: no undamaged checksum full backup matches the signed finish time.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
          AND name = N'PK__SalesDai__4D7B4ABD9107B9CA' AND type = 'PK'
    )
    OR NOT EXISTS
    (
        SELECT 1 FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
          AND name = N'FK_KMHistory_Activity'
    )
    OR NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
          AND name = N'IX_SalesDailyActivityKMHistory_Activity'
    )
    OR (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')) <> 2
    OR EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory'))
    OR EXISTS (SELECT 1 FROM sys.triggers WHERE parent_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory'))
        THROW 59509, N'ABORT: history key, foreign key, index, check, or trigger preimage mismatch.', 1;

    DECLARE @employee nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_UpdateActivityKM'));
    IF @employee IS NULL
        OR CHARINDEX(N'RouteWaypointsJSON', @employee) = 0
        OR CHARINDEX(N'ApproverLevel', @employee) > 0
        THROW 59510, N'ABORT: employee KM history insert was not confirmed.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM sys.sql_modules AS m
        WHERE m.definition LIKE N'%SalesDailyActivityKMHistory%'
          AND m.definition LIKE N'%INSERT%'
          AND m.definition NOT LIKE N'%RouteWaypointsJSON%'
          AND m.definition NOT LIKE N'%ApproverLevel%'
          AND OBJECTPROPERTY(m.object_id, 'IsProcedure') = 1
    )
        THROW 59510, N'ABORT: a procedure inserts history rows without the known column list.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM sys.sql_modules AS m
        INNER JOIN sys.objects AS o ON o.object_id = m.object_id
        WHERE SCHEMA_NAME(o.schema_id) = N'dbo'
          AND m.definition IS NULL
    )
        THROW 59510, N'ABORT: a dbo module definition is not visible. The dependency scan cannot prove the history insert list.', 1;

    DECLARE @expected TABLE
    (
        ColumnName sysname NOT NULL PRIMARY KEY,
        TypeName sysname NOT NULL,
        [Precision] tinyint NOT NULL,
        Scale tinyint NOT NULL,
        MaxLength smallint NOT NULL,
        IsNullable bit NOT NULL,
        IsIdentity bit NOT NULL
    );
    INSERT INTO @expected VALUES
     (N'HistoryId',            N'int',             10, 0,  4, 0, 1),
     (N'ActivityId',           N'int',             10, 0,  4, 0, 0),
     (N'OldKM',                N'decimal',         10, 2,  9, 1, 0),
     (N'NewKM',                N'decimal',         10, 2,  9, 1, 0),
     (N'ModifiedBy',           N'nvarchar',         0, 0, 200, 1, 0),
     (N'ModifiedDateTime',     N'datetime',         0, 0,  8, 0, 0),
     (N'Reason',               N'nvarchar',         0, 0, 2000, 1, 0),
     (N'RouteOriginName',      N'nvarchar',         0, 0, 510, 1, 0),
     (N'RouteDestinationName', N'nvarchar',         0, 0, 510, 1, 0),
     (N'RouteWaypointsJSON',   N'nvarchar',         0, 0, -1, 1, 0),
     (N'TravelMode',           N'nvarchar',         0, 0, 40, 1, 0);
    IF EXISTS
    (
        SELECT 1
        FROM @expected AS e
        FULL OUTER JOIN
        (
            SELECT col.name, TYPE_NAME(col.user_type_id) AS TypeName,
                   col.precision, col.scale, col.max_length, col.is_nullable, col.is_identity
            FROM sys.columns AS col
            WHERE col.object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
              AND col.name NOT IN (N'ApprovalRequestId', N'ApproverRole', N'ApproverLevel')
        ) AS c
            ON c.name = e.ColumnName
           AND c.TypeName = e.TypeName
           AND c.precision = e.[Precision]
           AND c.scale = e.Scale
           AND c.max_length = e.MaxLength
           AND c.is_nullable = e.IsNullable
           AND c.is_identity = e.IsIdentity
        WHERE e.ColumnName IS NULL OR c.name IS NULL
    )
        THROW 59509, N'ABORT: an existing history column does not match the captured preimage.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.default_constraints AS dc
        INNER JOIN sys.columns AS col
            ON col.object_id = dc.parent_object_id
           AND col.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
          AND col.name = N'ModifiedDateTime'
          AND REPLACE(REPLACE(dc.definition, N' ', N''), N'[', N'') LIKE N'%330%'
          AND dc.definition LIKE N'%sysutcdatetime%'
    )
        THROW 59509, N'ABORT: ModifiedDateTime default is not the captured 330-minute expression.', 1;

    DECLARE @auditPresent int =
          CASE WHEN COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ApprovalRequestId') IS NULL THEN 0 ELSE 1 END
        + CASE WHEN COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ApproverRole') IS NULL THEN 0 ELSE 1 END
        + CASE WHEN COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ApproverLevel') IS NULL THEN 0 ELSE 1 END;
    IF @auditPresent NOT IN (0, 3)
        THROW 59509, N'ABORT: audit columns are only partly present. No repair was attempted.', 1;
    IF @auditPresent = 3
    BEGIN
        IF (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')) <> 14
            OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory') AND name = N'ApprovalRequestId' AND TYPE_NAME(user_type_id) = N'int' AND max_length = 4 AND is_nullable = 1 AND is_identity = 0)
            OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory') AND name = N'ApproverRole' AND TYPE_NAME(user_type_id) = N'nvarchar' AND max_length = 40 AND is_nullable = 1)
            OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory') AND name = N'ApproverLevel' AND TYPE_NAME(user_type_id) = N'int' AND max_length = 4 AND is_nullable = 1 AND is_identity = 0)
            OR EXISTS
            (
                SELECT 1
                FROM sys.columns AS col
                WHERE col.object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
                  AND col.name IN (N'ApprovalRequestId', N'ApproverRole', N'ApproverLevel')
                  AND
                  (
                      EXISTS (SELECT 1 FROM sys.default_constraints AS dc WHERE dc.parent_object_id = col.object_id AND dc.parent_column_id = col.column_id)
                      OR EXISTS (SELECT 1 FROM sys.foreign_key_columns AS fkc WHERE fkc.parent_object_id = col.object_id AND fkc.parent_column_id = col.column_id)
                      OR EXISTS (SELECT 1 FROM sys.index_columns AS ic WHERE ic.object_id = col.object_id AND ic.column_id = col.column_id)
                      OR EXISTS (SELECT 1 FROM sys.check_constraints AS ck WHERE ck.parent_object_id = col.object_id AND ck.parent_column_id = col.column_id)
                  )
            )
            THROW 59509, N'ABORT: audit columns are present but not the approved shape. No repair was attempted.', 1;
        DECLARE @marker nvarchar(max);
        EXEC sys.sp_executesql
            N'SELECT @out = ModuleDefinition FROM dbo.TaDaLiveGoLive_20261117_Preimage WHERE WorkPackage = N''WP01'';',
            N'@out nvarchar(max) OUTPUT',
            @out = @marker OUTPUT;
        IF @marker <> N'COLUMNS_ABSENT'
            THROW 59512, N'ABORT: audit columns exist but this package has no proof it added them.', 1;
        PRINT N'WP-01 already matches the approved postcondition. No change was made.';
        RETURN;
    END

    BEGIN TRANSACTION;

    DECLARE @rows bigint, @idMin int, @idSum bigint, @snap nvarchar(max);
    SELECT
          @rows = COUNT_BIG(*)
        , @idMin = MIN(HistoryId)
        , @idSum = SUM(CONVERT(bigint, HistoryId))
    FROM dbo.SalesDailyActivityKMHistory WITH (TABLOCKX);

    SELECT @snap = STRING_AGG(CONCAT(col.name, N':', TYPE_NAME(col.user_type_id)), N',')
                   WITHIN GROUP (ORDER BY col.column_id)
    FROM sys.columns AS col
    WHERE col.object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory');

    IF @snap LIKE N'%ApproverLevel%'
        THROW 59509, N'ABORT: snapshot already contains ApproverLevel.', 1;

    DECLARE @evidence nvarchar(max) = N'
IF EXISTS (SELECT 1 FROM dbo.TaDaLiveGoLive_20261117_Preimage WHERE WorkPackage = ''WP03'')
    THROW 59517, N''ABORT: a WP-03 evidence row is present. Columns were not added.'', 1;
IF EXISTS (SELECT 1 FROM dbo.TaDaLiveGoLive_20261117_Preimage WHERE WorkPackage = ''WP01'')
    THROW 59512, N''ABORT: WP-01 preimage row already exists.'', 1;
INSERT INTO dbo.TaDaLiveGoLive_20261117_Preimage
    (WorkPackage, ObjectName, ModuleDefinition, HistoryRowCount, HistoryIdMin, HistoryIdSum, SchemaSnapshot)
VALUES
    (''WP01'', N''SalesDailyActivityKMHistory'', N''COLUMNS_ABSENT'', @rows, @idMin, @idSum, @snap);';
    EXEC sys.sp_executesql
          @evidence
        , N'@rows bigint, @idMin int, @idSum bigint, @snap nvarchar(max)'
        , @rows = @rows, @idMin = @idMin, @idSum = @idSum, @snap = @snap;

    ALTER TABLE dbo.SalesDailyActivityKMHistory ADD
          ApprovalRequestId int NULL
        , ApproverRole nvarchar(20) NULL
        , ApproverLevel int NULL;

    IF EXISTS
    (
        SELECT 1
        FROM sys.columns AS col
        WHERE col.object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
          AND col.name IN (N'ApprovalRequestId', N'ApproverRole', N'ApproverLevel')
          AND
          (
              col.is_nullable <> 1
              OR EXISTS
              (
                  SELECT 1 FROM sys.default_constraints AS dc
                  WHERE dc.parent_object_id = col.object_id
                    AND dc.parent_column_id = col.column_id
              )
              OR EXISTS
              (
                  SELECT 1
                  FROM sys.foreign_key_columns AS fkc
                  WHERE fkc.parent_object_id = col.object_id
                    AND fkc.parent_column_id = col.column_id
              )
              OR EXISTS
              (
                  SELECT 1
                  FROM sys.index_columns AS ic
                  WHERE ic.object_id = col.object_id
                    AND ic.column_id = col.column_id
              )
          )
    )
        THROW 59509, N'ABORT: a new column is not a nullable column without default, key, or index.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM sys.columns AS col
        WHERE col.object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
          AND
          (
              (col.name = N'ApprovalRequestId' AND (TYPE_NAME(col.user_type_id) <> N'int' OR col.max_length <> 4))
              OR (col.name = N'ApproverRole' AND (TYPE_NAME(col.user_type_id) <> N'nvarchar' OR col.max_length <> 40))
              OR (col.name = N'ApproverLevel' AND (TYPE_NAME(col.user_type_id) <> N'int' OR col.max_length <> 4))
          )
    )
        THROW 59509, N'ABORT: new column type does not match the approved shape.', 1;

    IF (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')) <> 14
        THROW 59509, N'ABORT: history column count is not the eleven captured columns plus the three audit columns.', 1;

    DECLARE @rowsAfter bigint, @idMinAfter int, @idSumAfter bigint;
    SELECT
          @rowsAfter = COUNT_BIG(*)
        , @idMinAfter = MIN(HistoryId)
        , @idSumAfter = SUM(CONVERT(bigint, HistoryId))
    FROM dbo.SalesDailyActivityKMHistory WITH (TABLOCKX);

    IF @rowsAfter <> @rows OR ISNULL(@idMinAfter, -1) <> ISNULL(@idMin, -1) OR ISNULL(@idSumAfter, -1) <> ISNULL(@idSum, -1)
        THROW 59509, N'ABORT: history row identity changed. Rolled back.', 1;

    IF (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')) <> 2
        THROW 59509, N'ABORT: index set changed.', 1;

    COMMIT TRANSACTION;
    PRINT N'WP-01 committed. History rows were not updated.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH
