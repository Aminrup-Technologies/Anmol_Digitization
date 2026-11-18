/*
    Package:  TA/DA LIVE Go-Live 20261117
    File:     60_Rollback_Procedures.sql
    Restores only the captured LIVE KM preimage for WP-02.
    Requires the current module to be the verified candidate
    F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9
    and the saved preimage to be EC6A97B9, length 15455.
    Uses ALTER PROCEDURE. It does not DROP and it does not GRANT.
    It does not restore or modify usp_RecalculateApprovalRequest,
    usp_RecalculateSalesClaim, or the workflow procedures.
    This revision aborts. The CategoryId exception literal is UNSIGNED.
    2026-11-18: do not revise the literals to force a restore. The current KM
    module already matches Dev. This file still expects recalc hash 08F34AF8,
    which is no longer the LIVE module, so it aborts before any restore.
    Limitation: the LIVE preimage bytes are not in this repository.
    If the saved header is not exactly one CREATE   PROCEDURE token,
    this script aborts and leaves the candidate in place.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @GateIdentity nvarchar(40) = N'UNSIGNED';
DECLARE @ApprovedLogin sysname = N'';
DECLARE @GateCategoryPreimage nvarchar(40) = N'UNSIGNED';

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
        THROW 59512, N'ABORT: evidence table is absent. Nothing was restored.', 1;
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
        THROW 59512, N'ABORT: evidence table shape is not the WP-01/WP-02 definition. Nothing was restored.', 1;

    IF ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_UpdateActivityKM_WithHistory', N'OBJECT', N'ALTER'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_UpdateActivityKM_WithHistory', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo', N'SCHEMA', N'VIEW DEFINITION'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'SELECT'), 0) <> 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'DELETE'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateApprovalRequest', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateApprovalRequest', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateSalesClaim', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_RecalculateSalesClaim', N'OBJECT', N'CONTROL'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_GetResolvedPolicy', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ProcessApprovalAction', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_MarkRequestReadyForPayment', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ForwardApprovalRequest', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ResolveDesignation', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ResolveRegion', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ResolveBranch', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_ResolveGrade', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.usp_UpdateActivityKM', N'OBJECT', N'ALTER'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'INSERT'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'UPDATE'), 0) = 1
        OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.SalesDailyActivityKMHistory', N'OBJECT', N'DELETE'), 0) = 1
        THROW 59507, N'ABORT: object rights do not match the WP-01/WP-02 permission matrix.', 1;

    DECLARE @logic nvarchar(max) = N'SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF @GateIdentity <> N''DR-IDENTITY-A''
    OR NULLIF(@ApprovedLogin, N'''') IS NULL
    OR @ApprovedLogin = N''sa''
    OR SUSER_SNAME() <> @ApprovedLogin
    OR ORIGINAL_LOGIN() = N''sa''
    OR SUSER_SNAME() = N''sa''
    OR IS_SRVROLEMEMBER(N''sysadmin'') = 1
    OR IS_ROLEMEMBER(N''db_owner'') = 1
    OR IS_ROLEMEMBER(N''db_ddladmin'') = 1
    OR ISNULL(HAS_PERMS_BY_NAME(DB_NAME(), N''DATABASE'', N''CONTROL''), 0) = 1
    OR ISNULL(HAS_PERMS_BY_NAME(DB_NAME(), N''DATABASE'', N''ALTER ANY PROCEDURE''), 0) = 1
    OR ISNULL(HAS_PERMS_BY_NAME(N''dbo'', N''SCHEMA'', N''ALTER''), 0) = 1
    OR ISNULL(HAS_PERMS_BY_NAME(N''dbo'', N''SCHEMA'', N''CONTROL''), 0) = 1
    THROW 59505, N''ABORT: deployment identity is unsigned or is wider than this package.'', 1;
IF UPPER(CONVERT(nvarchar(256), SERVERPROPERTY(''ServerName''))) <> N''MUMANMOAPP01\SQLEXPRESS''
    THROW 59501, N''ABORT: server is not MUMANMOAPP01\SQLEXPRESS.'', 1;
IF DB_NAME() <> N''AnmolDristi''
    THROW 59502, N''ABORT: database is not AnmolDristi.'', 1;
IF @@TRANCOUNT <> 0
    THROW 59503, N''ABORT: a transaction is already open.'', 1;
IF OBJECT_ID(N''dbo.TaDaLiveGoLive_20261117_Preimage'', N''U'') IS NULL
    THROW 59512, N''ABORT: no saved preimage table. Nothing was restored.'', 1;
IF EXISTS (SELECT 1 FROM dbo.TaDaLiveGoLive_20261117_Preimage WHERE WorkPackage = N''WP03'')
    THROW 59517, N''ABORT: a WP-03 evidence row exists. usp_RecalculateApprovalRequest was not restored.'', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.TaDaLiveGoLive_20261117_Preimage WHERE WorkPackage = N''WP02'')
    THROW 59512, N''ABORT: no WP-02 preimage row. Nothing was restored.'', 1;

DECLARE @saved nvarchar(max), @applied char(64);
SELECT @saved = ModuleDefinition, @applied = AppliedSha256
FROM dbo.TaDaLiveGoLive_20261117_Preimage
WHERE WorkPackage = N''WP02'';

IF @saved IS NULL OR @applied IS NULL
    THROW 59512, N''ABORT: WP-02 preimage row is incomplete. The KM module was left in place.'', 1;
IF CONVERT(char(64), HASHBYTES(''SHA2_256'', CONVERT(varbinary(max), @saved)), 2) <> N''EC6A97B97605B2A12857367B78C7C84F277289443DFF267C4BB29118E258E100''
    OR LEN(@saved) <> 15455
    OR DATALENGTH(@saved) <> 30910
    THROW 59508, N''ABORT: saved KM preimage is not EC6A97B9 length 15455.'', 1;
IF @applied <> N''F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9''
    THROW 59512, N''ABORT: saved applied fingerprint is not the verified candidate. No restore.'', 1;
IF (LEN(@saved) - LEN(REPLACE(@saved, N''CREATE   PROCEDURE'', N''''))) / LEN(N''CREATE   PROCEDURE'') <> 1
    THROW 59512, N''ABORT: saved KM preimage does not contain exactly one CREATE   PROCEDURE token. No DROP was attempted.'', 1;
IF CHARINDEX(N''CategoryId'', @saved) > 0 AND @GateCategoryPreimage <> N''CATEGORYID-PREIMAGE-APPROVED''
    THROW 59513, N''ABORT: saved KM preimage contains CategoryId. The current KM module was left in place. Columns were not dropped.'', 1;

DECLARE @current nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N''dbo.usp_UpdateActivityKM_WithHistory''));
DECLARE @currentCanon nvarchar(max) = REPLACE(@current, N''ALTER   PROCEDURE'', N''CREATE   PROCEDURE'');
IF @current IS NULL
    OR CONVERT(char(64), HASHBYTES(''SHA2_256'', CONVERT(varbinary(max), @currentCanon)), 2) <> @applied
    OR CHARINDEX(N''Designation_ID'', @currentCanon) = 0
    OR CHARINDEX(N''CategoryId'', @currentCanon) > 0
    THROW 59512, N''ABORT: current KM module is not the applied candidate. No restore.'', 1;
IF EXISTS (SELECT 1 FROM sys.sql_modules WHERE object_id = OBJECT_ID(N''dbo.usp_UpdateActivityKM_WithHistory'') AND is_schema_bound = 1)
    THROW 59512, N''ABORT: KM procedure is schema-bound.'', 1;
IF EXISTS (SELECT 1 FROM sys.database_permissions WHERE class = 1 AND major_id = OBJECT_ID(N''dbo.usp_UpdateActivityKM_WithHistory'') AND minor_id <> 0)
    THROW 59512, N''ABORT: column permissions exist on the KM procedure.'', 1;
IF EXISTS (
    SELECT 1 FROM sys.sql_expression_dependencies
    WHERE referenced_id = OBJECT_ID(N''dbo.usp_UpdateActivityKM_WithHistory'')
      AND is_schema_bound_reference = 1
)
    THROW 59512, N''ABORT: a schema-bound reference points at the KM procedure.'', 1;

DECLARE @claimSha char(64) = CONVERT(char(64), HASHBYTES(''SHA2_256'', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N''dbo.usp_RecalculateSalesClaim'')))), 2);
DECLARE @recalcSha char(64) = CONVERT(char(64), HASHBYTES(''SHA2_256'', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N''dbo.usp_RecalculateApprovalRequest'')))), 2);
IF @claimSha <> N''FC60A9CFC68BA480454A1D4A4653EAA9426D281267E6750AC3CD96DCC9A33ABC''
    OR @recalcSha <> N''08F34AF8C5CC188F2A0877985FB7FC5E70DEB0067034B95264AE6EF4739ED5D7''
    THROW 59508, N''ABORT: claim or approval-recalc fingerprint is not the quoted preimage. No restore.'', 1;

DECLARE @permBefore nvarchar(max);
SELECT @permBefore = STRING_AGG(CONCAT(pe.state_desc, N''|'', pe.permission_name, N''|'', CONVERT(varchar(12), pe.grantee_principal_id)), N'','')
    WITHIN GROUP (ORDER BY pe.grantee_principal_id, pe.permission_name, pe.state_desc)
FROM sys.database_permissions AS pe
WHERE pe.class = 1
  AND pe.major_id = OBJECT_ID(N''dbo.usp_UpdateActivityKM_WithHistory'')
  AND pe.minor_id = 0;

BEGIN TRANSACTION;

DECLARE @saved2 nvarchar(max), @applied2 char(64);
SELECT @saved2 = ModuleDefinition, @applied2 = AppliedSha256
FROM dbo.TaDaLiveGoLive_20261117_Preimage WITH (UPDLOCK, HOLDLOCK)
WHERE WorkPackage = N''WP02'';
IF @saved2 <> @saved OR @applied2 <> @applied
    THROW 59512, N''ABORT: WP-02 evidence row changed before restore. Rolled back.'', 1;

DECLARE @exec nvarchar(max) = REPLACE(@saved, N''CREATE   PROCEDURE'', N''ALTER   PROCEDURE'');
IF CHARINDEX(N''CREATE   PROCEDURE'', @exec) > 0
    OR (LEN(@exec) - LEN(REPLACE(@exec, N''ALTER   PROCEDURE'', N''''))) / LEN(N''ALTER   PROCEDURE'') <> 1
    THROW 59512, N''ABORT: restore batch is not a single ALTER   PROCEDURE. Rolled back.'', 1;

EXEC sys.sp_executesql @exec;

DECLARE @restored nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N''dbo.usp_UpdateActivityKM_WithHistory''));
DECLARE @restoredCanon nvarchar(max) = REPLACE(@restored, N''ALTER   PROCEDURE'', N''CREATE   PROCEDURE'');
IF @restoredCanon <> @saved
    OR CONVERT(char(64), HASHBYTES(''SHA2_256'', CONVERT(varbinary(max), @restoredCanon)), 2) <> N''EC6A97B97605B2A12857367B78C7C84F277289443DFF267C4BB29118E258E100''
    OR LEN(@restoredCanon) <> 15455
    OR DATALENGTH(@restoredCanon) <> 30910
    THROW 59511, N''ABORT: restored KM text does not equal the saved preimage. Rolled back.'', 1;

DECLARE @permAfter nvarchar(max);
SELECT @permAfter = STRING_AGG(CONCAT(pe.state_desc, N''|'', pe.permission_name, N''|'', CONVERT(varchar(12), pe.grantee_principal_id)), N'','')
    WITHIN GROUP (ORDER BY pe.grantee_principal_id, pe.permission_name, pe.state_desc)
FROM sys.database_permissions AS pe
WHERE pe.class = 1
  AND pe.major_id = OBJECT_ID(N''dbo.usp_UpdateActivityKM_WithHistory'')
  AND pe.minor_id = 0;
IF ISNULL(@permAfter, N'''') <> ISNULL(@permBefore, N'''')
    THROW 59512, N''ABORT: KM procedure permissions changed. Rolled back. No GRANT was issued.'', 1;

IF CONVERT(char(64), HASHBYTES(''SHA2_256'', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N''dbo.usp_RecalculateSalesClaim'')))), 2) <> @claimSha
    OR CONVERT(char(64), HASHBYTES(''SHA2_256'', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N''dbo.usp_RecalculateApprovalRequest'')))), 2) <> @recalcSha
    THROW 59511, N''ABORT: claim or approval-recalc fingerprint moved. Rolled back.'', 1;

COMMIT TRANSACTION;
PRINT N''WP-02 module restored from the saved preimage. usp_RecalculateApprovalRequest was not modified.'';';
    EXEC sys.sp_executesql
        @logic,
        N'@GateCategoryPreimage nvarchar(40), @GateIdentity nvarchar(40), @ApprovedLogin sysname',
        @GateIdentity = @GateIdentity,
        @ApprovedLogin = @ApprovedLogin,
        @GateCategoryPreimage = @GateCategoryPreimage;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH
