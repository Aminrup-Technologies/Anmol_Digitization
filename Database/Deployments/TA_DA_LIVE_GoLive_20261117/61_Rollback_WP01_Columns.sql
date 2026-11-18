/*
    Package:  TA/DA LIVE Go-Live 20261117
    File:     61_Rollback_WP01_Columns.sql
    2026-11-18: nine history rows have the three audit values set. This file
    leaves columns in place when any audit value is non-NULL. Do not run it.
    Drops only ApprovalRequestId, ApproverRole, and ApproverLevel.
    Requires the WP-01 evidence marker, the exact column definitions,
    all-NULL values, and no dependent reference.
    History rows are not deleted. Existing keys and indexes are not changed.
    This revision aborts. It does not restore a procedure.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @GateIdentity nvarchar(40) = N'UNSIGNED';
DECLARE @ApprovedLogin sysname = N'';

BEGIN TRY
    IF UPPER(CONVERT(nvarchar(256), SERVERPROPERTY('ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
        THROW 59501, N'ABORT: server is not MUMANMOAPP01\SQLEXPRESS.', 1;
    IF DB_NAME() = N'AnmolDristiDev'
        THROW 59502, N'ABORT: AnmolDristiDev is refused.', 1;
    IF DB_NAME() <> N'AnmolDristi'
        THROW 59502, N'ABORT: database is not AnmolDristi.', 1;
    IF @@TRANCOUNT <> 0
        THROW 59503, N'ABORT: a transaction is already open.', 1;
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
    IF DATABASEPROPERTYEX(DB_NAME(), N'Updateability') <> N'READ_WRITE'
        THROW 59502, N'ABORT: database is not read-write.', 1;
    IF OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'U') IS NULL
        THROW 59512, N'ABORT: no saved preimage table. Nothing was restored.', 1;
    IF (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage')) <> 13
        OR NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage') AND name = N'CK_TaDaLiveGoLive_20261117_WorkPackage' AND definition LIKE N'%WP01%' AND definition LIKE N'%WP02%' AND definition NOT LIKE N'%WP03%')
        THROW 59512, N'ABORT: evidence table shape is not the WP-01/WP-02 definition. Columns were left unchanged.', 1;
    IF ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'ALTER'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'SELECT'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'DELETE'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo', N'SCHEMA', N'VIEW DEFINITION'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'SELECT'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'DELETE'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateApprovalRequest', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateApprovalRequest', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateSalesClaim', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_GetResolvedPolicy', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ProcessApprovalAction', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_MarkRequestReadyForPayment', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ForwardApprovalRequest', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_UpdateActivityKM', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_UpdateActivityKM_WithHistory', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'INSERT'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'UPDATE'), 0) = 1
        THROW 59507, N'ABORT: object rights do not match the WP-01/WP-02 permission matrix.', 1;

    DECLARE @logic nvarchar(max) = N'SET NOCOUNT ON;
SET XACT_ABORT ON;

IF @GateIdentity <> N''DR-IDENTITY-A''
    OR NULLIF(@ApprovedLogin, N'''') IS NULL
    OR @ApprovedLogin = N''sa''
    OR SUSER_SNAME() <> @ApprovedLogin
    OR ORIGINAL_LOGIN() = N''sa''
    OR SUSER_SNAME() = N''sa''
    THROW 59505, N''ABORT: deployment identity is unsigned or is sa.'', 1;
IF UPPER(CONVERT(nvarchar(256), SERVERPROPERTY(''ServerName''))) <> N''MUMANMOAPP01\SQLEXPRESS''
    THROW 59501, N''ABORT: server is not MUMANMOAPP01\SQLEXPRESS.'', 1;
IF DB_NAME() <> N''AnmolDristi''
    THROW 59502, N''ABORT: database is not AnmolDristi.'', 1;
IF @@TRANCOUNT <> 0
    THROW 59503, N''ABORT: a transaction is already open.'', 1;
IF OBJECT_ID(N''dbo.TaDaLiveGoLive_20261117_Preimage'', N''U'') IS NULL
    THROW 59512, N''ABORT: no saved preimage table. Columns were left unchanged.'', 1;

DECLARE @marker nvarchar(max);
SELECT @marker = ModuleDefinition
FROM dbo.TaDaLiveGoLive_20261117_Preimage
WHERE WorkPackage = ''WP01'';

IF @marker IS NULL
    THROW 59512, N''ABORT: WP-01 was not applied by this package. Columns were left unchanged.'', 1;
IF @marker <> N''COLUMNS_ABSENT''
    THROW 59512, N''ABORT: WP-01 preimage marker is unexpected. Columns were left unchanged.'', 1;
IF COL_LENGTH(N''dbo.SalesDailyActivityKMHistory'', N''ApprovalRequestId'') IS NULL
    OR COL_LENGTH(N''dbo.SalesDailyActivityKMHistory'', N''ApproverRole'') IS NULL
    OR COL_LENGTH(N''dbo.SalesDailyActivityKMHistory'', N''ApproverLevel'') IS NULL
    THROW 59514, N''ABORT: the three audit columns are not all present. Nothing was dropped.'', 1;
IF (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N''dbo.SalesDailyActivityKMHistory'')) <> 14
    OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N''dbo.SalesDailyActivityKMHistory'') AND name = N''ApprovalRequestId'' AND TYPE_NAME(user_type_id) = N''int'' AND max_length = 4 AND is_nullable = 1 AND is_identity = 0)
    OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N''dbo.SalesDailyActivityKMHistory'') AND name = N''ApproverRole'' AND TYPE_NAME(user_type_id) = N''nvarchar'' AND max_length = 40 AND is_nullable = 1)
    OR NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N''dbo.SalesDailyActivityKMHistory'') AND name = N''ApproverLevel'' AND TYPE_NAME(user_type_id) = N''int'' AND max_length = 4 AND is_nullable = 1 AND is_identity = 0)
    THROW 59514, N''ABORT: audit columns are not the expected definitions. Nothing was dropped.'', 1;
IF EXISTS (
    SELECT 1
    FROM sys.sql_modules AS m
    INNER JOIN sys.objects AS o ON o.object_id = m.object_id
    WHERE SCHEMA_NAME(o.schema_id) = N''dbo'' AND m.definition IS NULL
)
    THROW 59510, N''ABORT: a dbo module definition is not visible. Columns were left in place.'', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @filled bigint, @rows bigint;
    SELECT @filled = COUNT_BIG(*)
    FROM dbo.SalesDailyActivityKMHistory WITH (TABLOCKX)
    WHERE ApprovalRequestId IS NOT NULL
       OR ApproverRole IS NOT NULL
       OR ApproverLevel IS NOT NULL;
    IF @filled > 0
    BEGIN
        DECLARE @filledMsg nvarchar(240) = CONCAT(N''ABORT: '', @filled, N'' history rows have audit values. Columns were left in place. No row was deleted.'');
        THROW 59514, @filledMsg, 1;
    END

    IF EXISTS
    (
        SELECT 1
        FROM sys.sql_modules AS m
        WHERE m.definition LIKE N''%SalesDailyActivityKMHistory%''
          AND m.definition LIKE N''%ApproverLevel%''
    )
        THROW 59510, N''ABORT: a module still references the history audit columns. Columns were left in place.'', 1;

    IF EXISTS
    (
        SELECT 1
        FROM sys.sql_expression_dependencies AS d
        INNER JOIN sys.columns AS col
            ON col.object_id = d.referenced_id
           AND col.column_id = d.referenced_minor_id
        WHERE d.referenced_id = OBJECT_ID(N''dbo.SalesDailyActivityKMHistory'')
          AND col.name IN (N''ApprovalRequestId'', N''ApproverRole'', N''ApproverLevel'')
    )
        THROW 59510, N''ABORT: a dependency references an audit column. Columns were left in place.'', 1;

    IF EXISTS
    (
        SELECT 1
        FROM sys.columns AS col
        WHERE col.object_id = OBJECT_ID(N''dbo.SalesDailyActivityKMHistory'')
          AND col.name IN (N''ApprovalRequestId'', N''ApproverRole'', N''ApproverLevel'')
          AND
          (
              EXISTS (SELECT 1 FROM sys.default_constraints AS dc WHERE dc.parent_object_id = col.object_id AND dc.parent_column_id = col.column_id)
              OR EXISTS (SELECT 1 FROM sys.foreign_key_columns AS fkc WHERE fkc.parent_object_id = col.object_id AND fkc.parent_column_id = col.column_id)
              OR EXISTS (SELECT 1 FROM sys.index_columns AS ic WHERE ic.object_id = col.object_id AND ic.column_id = col.column_id)
              OR EXISTS (SELECT 1 FROM sys.check_constraints AS ck WHERE ck.parent_object_id = col.object_id AND ck.parent_column_id = col.column_id)
          )
    )
        THROW 59514, N''ABORT: an audit column has a default, foreign key, or index. Nothing was dropped.'', 1;

    SELECT @rows = COUNT_BIG(*) FROM dbo.SalesDailyActivityKMHistory WITH (TABLOCKX);

    ALTER TABLE dbo.SalesDailyActivityKMHistory DROP COLUMN ApproverLevel, ApproverRole, ApprovalRequestId;

    IF COL_LENGTH(N''dbo.SalesDailyActivityKMHistory'', N''ApprovalRequestId'') IS NOT NULL
        OR COL_LENGTH(N''dbo.SalesDailyActivityKMHistory'', N''ApproverRole'') IS NOT NULL
        OR COL_LENGTH(N''dbo.SalesDailyActivityKMHistory'', N''ApproverLevel'') IS NOT NULL
        THROW 59514, N''ABORT: audit columns are still present after the drop.'', 1;
    IF (SELECT COUNT_BIG(*) FROM dbo.SalesDailyActivityKMHistory WITH (TABLOCKX)) <> @rows
        THROW 59514, N''ABORT: history row count changed. Rolled back.'', 1;
    IF NOT EXISTS
    (
        SELECT 1 FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N''dbo.SalesDailyActivityKMHistory'')
          AND name = N''PK__SalesDai__4D7B4ABD9107B9CA''
    )
        THROW 59514, N''ABORT: primary key changed. Rolled back.'', 1;

    COMMIT TRANSACTION;
    PRINT N''WP-01 audit columns dropped. History rows were not deleted. The evidence table was kept.'';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH
';
    EXEC sys.sp_executesql
        @logic,
        N'@GateIdentity nvarchar(40), @ApprovedLogin sysname',
        @GateIdentity = @GateIdentity,
        @ApprovedLogin = @ApprovedLogin;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH