/*
    Package:  TA/DA LIVE Go-Live 20261117
    File:     50_Postflight_ReadOnly.sql
    Writes:   none. The evidence table is read only when it already exists.
    NOT_APPLIED is expected before that work package commits.
    FAIL throws. This file does not record acceptance.

    2026-11-18: the evidence table is absent, the three audit columns are present,
    the KM hash is F6B07BE8, and the approval-recalc hash is no longer 08F34AF8.
    This file then throws 59511. That throw does not authorize 20, 30, 40, 60, or 61.
*/
SET NOCOUNT ON;

CREATE TABLE #result
(
    Item nvarchar(128) NOT NULL,
    State nvarchar(20) NOT NULL,
    Detail nvarchar(400) NOT NULL
);

IF UPPER(CONVERT(nvarchar(256), SERVERPROPERTY('ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
    INSERT INTO #result VALUES (N'server', N'FAIL', CONVERT(nvarchar(256), SERVERPROPERTY('ServerName')));
IF DB_NAME() <> N'AnmolDristi'
    INSERT INTO #result VALUES (N'database', N'FAIL', DB_NAME());

DECLARE @modules TABLE
(
    ObjectName sysname NOT NULL PRIMARY KEY,
    ExpectedLen int NOT NULL,
    ExpectedBytes int NOT NULL,
    ExpectedSha char(64) NOT NULL
);

INSERT INTO @modules VALUES
 (N'usp_RecalculateSalesClaim',         28786, 57572, 'FC60A9CFC68BA480454A1D4A4653EAA9426D281267E6750AC3CD96DCC9A33ABC'),
 (N'usp_GetResolvedPolicy',              3297,  6594, '99ADB00C37DE0C62400F90B0CE4F6BB195A98129267A7221E490E196ACC2DA90'),
 (N'usp_ResolveDesignation',             1008,  2016, '3E65716AB518D0B17C59F2EAACF468CA6711575A0A98E4CC099750C460FE86E0'),
 (N'usp_ProcessApprovalAction',         42031, 84062, 'FF87E2AAD1319EB48F8237ED9293F674FE4F508E64F39CFDC41DBBA24AFC1392'),
 (N'usp_MarkRequestReadyForPayment',    11189, 22378, '2D565A377B668BFC23611F8288B8532B1F24727609EFA32394900B95EC6A69E2'),
 (N'usp_ForwardApprovalRequest',        16459, 32918, '02BC07F01C3EDB0B4519561427F679D6B897DBC74527BFC518970F0C000A9F77');

INSERT INTO #result (Item, State, Detail)
SELECT m.ObjectName,
       CASE
           WHEN d.definition IS NOT NULL
            AND LEN(d.definition) = m.ExpectedLen
            AND DATALENGTH(d.definition) = m.ExpectedBytes
            AND CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), d.definition)), 2) = m.ExpectedSha
           THEN N'UNCHANGED' ELSE N'FAIL'
       END,
       ISNULL(CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), d.definition)), 2), N'NULL')
FROM @modules AS m
CROSS APPLY (SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + m.ObjectName)) AS definition) AS d;

DECLARE @km nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_UpdateActivityKM_WithHistory'));
DECLARE @kmSha char(64) = CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @km)), 2);
DECLARE @re nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest'));
DECLARE @reSha char(64) = CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @re)), 2);

IF OBJECT_ID(N'dbo.TaDaLiveGoLive_20261117_Preimage', N'U') IS NULL
BEGIN
    INSERT INTO #result VALUES
    (
        N'WP-01',
        CASE
            WHEN COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ApprovalRequestId') IS NULL
             AND COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ApproverRole') IS NULL
             AND COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ApproverLevel') IS NULL
            THEN N'NOT_APPLIED' ELSE N'FAIL'
        END,
        N'evidence table absent'
    );
    INSERT INTO #result VALUES
        (N'WP-02', CASE WHEN @kmSha = 'EC6A97B97605B2A12857367B78C7C84F277289443DFF267C4BB29118E258E100' THEN N'NOT_APPLIED' ELSE N'FAIL' END, ISNULL(@kmSha, N'NULL'));
    INSERT INTO #result VALUES
        (N'usp_RecalculateApprovalRequest', CASE WHEN @reSha = '08F34AF8C5CC188F2A0877985FB7FC5E70DEB0067034B95264AE6EF4739ED5D7' THEN N'EXCLUDED_UNCHANGED' ELSE N'FAIL' END, ISNULL(@reSha, N'NULL'));
END
ELSE
BEGIN
    EXEC sys.sp_executesql
        N'
        DECLARE @km nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N''dbo.usp_UpdateActivityKM_WithHistory''));
        DECLARE @kmSha char(64) = CONVERT(char(64), HASHBYTES(''SHA2_256'', CONVERT(varbinary(max), @km)), 2);
        DECLARE @kmCanon nvarchar(max) = REPLACE(@km, N''ALTER   PROCEDURE'', N''CREATE   PROCEDURE'');
        DECLARE @kmCanonSha char(64) = CONVERT(char(64), HASHBYTES(''SHA2_256'', CONVERT(varbinary(max), @kmCanon)), 2);
        DECLARE @re nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N''dbo.usp_RecalculateApprovalRequest''));
        DECLARE @reSha char(64) = CONVERT(char(64), HASHBYTES(''SHA2_256'', CONVERT(varbinary(max), @re)), 2);

        IF COL_LENGTH(N''dbo.SalesDailyActivityKMHistory'', N''ApprovalRequestId'') IS NULL
           AND COL_LENGTH(N''dbo.SalesDailyActivityKMHistory'', N''ApproverRole'') IS NULL
           AND COL_LENGTH(N''dbo.SalesDailyActivityKMHistory'', N''ApproverLevel'') IS NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.TaDaLiveGoLive_20261117_Preimage WHERE WorkPackage = ''WP01'')
            INSERT INTO #result VALUES (N''WP-01'', N''NOT_APPLIED'', N''audit columns absent'');
        ELSE IF EXISTS (SELECT 1 FROM dbo.TaDaLiveGoLive_20261117_Preimage WHERE WorkPackage = ''WP01'' AND ModuleDefinition = N''COLUMNS_ABSENT'')
            AND COL_LENGTH(N''dbo.SalesDailyActivityKMHistory'', N''ApprovalRequestId'') IS NOT NULL
            AND COL_LENGTH(N''dbo.SalesDailyActivityKMHistory'', N''ApproverRole'') IS NOT NULL
            AND COL_LENGTH(N''dbo.SalesDailyActivityKMHistory'', N''ApproverLevel'') IS NOT NULL
            AND EXISTS (
                SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N''dbo.SalesDailyActivityKMHistory'')
                  AND name = N''ApproverRole'' AND is_nullable = 1 AND max_length = 40
                  AND TYPE_NAME(user_type_id) = N''nvarchar'')
            AND NOT EXISTS (
                SELECT 1 FROM sys.columns AS col
                WHERE col.object_id = OBJECT_ID(N''dbo.SalesDailyActivityKMHistory'')
                  AND col.name IN (N''ApprovalRequestId'', N''ApproverRole'', N''ApproverLevel'')
                  AND (col.is_nullable <> 1
                       OR EXISTS (SELECT 1 FROM sys.default_constraints AS dc WHERE dc.parent_object_id = col.object_id AND dc.parent_column_id = col.column_id)
                       OR EXISTS (SELECT 1 FROM sys.index_columns AS ic WHERE ic.object_id = col.object_id AND ic.column_id = col.column_id)))
            AND EXISTS (
                SELECT 1 FROM sys.key_constraints
                WHERE parent_object_id = OBJECT_ID(N''dbo.SalesDailyActivityKMHistory'')
                  AND name = N''PK__SalesDai__4D7B4ABD9107B9CA'')
            INSERT INTO #result VALUES (N''WP-01'', N''APPLIED'', N''three nullable audit columns'');
        ELSE
            INSERT INTO #result VALUES (N''WP-01'', N''FAIL'', N''partial or unexpected history shape'');

        IF @kmSha = ''EC6A97B97605B2A12857367B78C7C84F277289443DFF267C4BB29118E258E100''
           AND NOT EXISTS (SELECT 1 FROM dbo.TaDaLiveGoLive_20261117_Preimage WHERE WorkPackage = ''WP02'')
            INSERT INTO #result VALUES (N''WP-02'', N''NOT_APPLIED'', N''KM module still at the quoted preimage'');
        ELSE IF EXISTS (
                SELECT 1 FROM dbo.TaDaLiveGoLive_20261117_Preimage
                WHERE WorkPackage = ''WP02''
                  AND PreimageSha256 = ''EC6A97B97605B2A12857367B78C7C84F277289443DFF267C4BB29118E258E100''
                  AND SubmittedSha256 = ''F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9''
                  AND AppliedSha256 = ''F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9'')
            AND @kmCanonSha = ''F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9''
            AND CHARINDEX(N''Designation_ID'', @kmCanon) > 0
            AND CHARINDEX(N''CategoryId'', @kmCanon) = 0
            AND CHARINDEX(N''usp_RecalculateSalesClaim'', @kmCanon) > 0
            AND CHARINDEX(N''ApproverLevel'', @kmCanon) > 0
            AND CHARINDEX(N''SET XACT_ABORT OFF'', @kmCanon) > 0
            INSERT INTO #result VALUES (N''WP-02'', N''APPLIED'', @kmCanonSha);
        ELSE
            INSERT INTO #result VALUES (N''WP-02'', N''FAIL'', ISNULL(@kmCanonSha, @kmSha));

        IF EXISTS (SELECT 1 FROM dbo.TaDaLiveGoLive_20261117_Preimage WHERE WorkPackage = ''WP03'')
            OR EXISTS (
                SELECT 1 FROM sys.check_constraints
                WHERE parent_object_id = OBJECT_ID(N''dbo.TaDaLiveGoLive_20261117_Preimage'')
                  AND definition LIKE N''%WP03%''
            )
            INSERT INTO #result VALUES (N''WP-03'', N''FAIL'', N''WP-03 evidence is present'');
        ELSE IF @reSha = ''08F34AF8C5CC188F2A0877985FB7FC5E70DEB0067034B95264AE6EF4739ED5D7''
            INSERT INTO #result VALUES (N''usp_RecalculateApprovalRequest'', N''EXCLUDED_UNCHANGED'', @reSha);
        ELSE
            INSERT INTO #result VALUES (N''usp_RecalculateApprovalRequest'', N''FAIL'', ISNULL(@reSha, N''NULL''));
        ';
END

SELECT Item, State, Detail FROM #result ORDER BY Item;

IF EXISTS (SELECT 1 FROM #result WHERE State = N'FAIL')
    THROW 59511, N'ABORT: postflight found a mismatch. No object was changed by this file.', 1;

IF EXISTS (SELECT 1 FROM #result WHERE State = N'NOT_APPLIED')
    PRINT N'POSTFLIGHT: WP-01 or WP-02 is NOT_APPLIED. That is not window acceptance. WP-03 is excluded.';
ELSE
    PRINT N'POSTFLIGHT: WP-01 and WP-02 are APPLIED and the excluded fingerprints match. WP-03 was not deployed. Paste this grid into the execution log. This print is not a signature.';
