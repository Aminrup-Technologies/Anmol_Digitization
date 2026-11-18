/*
    Package:  TA/DA LIVE Go-Live 20261117
    File:     10_Preflight_ReadOnly.sql
    Target:   MUMANMOAPP01\SQLEXPRESS / AnmolDristi
    Writes:   none
    Fingerprints quoted from 10_LIVE_Discovery_Capture.md.
    Resolver hashes are not pinned. This file prints them and does not invent them.

    2026-11-18: this file still expects the discovery snapshot, including KM hash
    EC6A97B9 and absent audit columns. The current catalog does not match that
    snapshot. A failure row is expected. Use 11_Current_State_ReadOnly.sql for
    the later Dev comparison. Do not run 20 or 30 because this file fails.
*/
SET NOCOUNT ON;

DECLARE @fail TABLE
(
    Item nvarchar(128) NOT NULL,
    Detail nvarchar(400) NOT NULL
);

DECLARE @parked TABLE
(
    Item nvarchar(128) NOT NULL,
    Detail nvarchar(400) NOT NULL
);

DECLARE @server nvarchar(256) = CONVERT(nvarchar(256), SERVERPROPERTY('ServerName'));
IF UPPER(@server) <> N'MUMANMOAPP01\SQLEXPRESS'
    INSERT INTO @fail VALUES (N'server', CONCAT(N'got ', @server));

IF DB_NAME() = N'AnmolDristiDev'
    INSERT INTO @fail VALUES (N'database', N'AnmolDristiDev is refused');
IF DB_NAME() <> N'AnmolDristi'
    INSERT INTO @fail VALUES (N'database', CONCAT(N'got ', DB_NAME()));

DECLARE @modules TABLE
(
    ObjectName sysname NOT NULL PRIMARY KEY,
    ExpectedLen int NOT NULL,
    ExpectedBytes int NOT NULL,
    ExpectedSha char(64) NOT NULL
);

INSERT INTO @modules VALUES
 (N'usp_UpdateActivityKM_WithHistory', 15455, 30910, 'EC6A97B97605B2A12857367B78C7C84F277289443DFF267C4BB29118E258E100'),
 (N'usp_RecalculateApprovalRequest',     7567, 15134, '08F34AF8C5CC188F2A0877985FB7FC5E70DEB0067034B95264AE6EF4739ED5D7'),
 (N'usp_RecalculateSalesClaim',         28786, 57572, 'FC60A9CFC68BA480454A1D4A4653EAA9426D281267E6750AC3CD96DCC9A33ABC'),
 (N'usp_GetResolvedPolicy',              3297,  6594, '99ADB00C37DE0C62400F90B0CE4F6BB195A98129267A7221E490E196ACC2DA90'),
 (N'usp_ResolveDesignation',             1008,  2016, '3E65716AB518D0B17C59F2EAACF468CA6711575A0A98E4CC099750C460FE86E0'),
 (N'usp_ProcessApprovalAction',         42031, 84062, 'FF87E2AAD1319EB48F8237ED9293F674FE4F508E64F39CFDC41DBBA24AFC1392'),
 (N'usp_MarkRequestReadyForPayment',    11189, 22378, '2D565A377B668BFC23611F8288B8532B1F24727609EFA32394900B95EC6A69E2'),
 (N'usp_ForwardApprovalRequest',        16459, 32918, '02BC07F01C3EDB0B4519561427F679D6B897DBC74527BFC518970F0C000A9F77');

INSERT INTO @fail (Item, Detail)
SELECT m.ObjectName,
       CONCAT(N'len=', CONVERT(varchar(20), LEN(d.definition)),
              N' bytes=', CONVERT(varchar(20), DATALENGTH(d.definition)),
              N' sha=', ISNULL(CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), d.definition)), 2), N'NULL'))
FROM @modules AS m
CROSS APPLY (SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + m.ObjectName)) AS definition) AS d
WHERE d.definition IS NULL
   OR LEN(d.definition) <> m.ExpectedLen
   OR DATALENGTH(d.definition) <> m.ExpectedBytes
   OR CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), d.definition)), 2) <> m.ExpectedSha;

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

IF OBJECT_ID(N'dbo.SalesDailyActivityKMHistory', N'U') IS NULL
    INSERT INTO @fail VALUES (N'SalesDailyActivityKMHistory', N'missing');
ELSE
BEGIN
    INSERT INTO @fail (Item, Detail)
    SELECT CONCAT(N'column ', COALESCE(e.ColumnName, c.name, N'?')),
           N'baseline column preimage mismatch'
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
    WHERE e.ColumnName IS NULL OR c.name IS NULL;

    IF COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ApprovalRequestId') IS NOT NULL
        OR COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ApproverRole') IS NOT NULL
        OR COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ApproverLevel') IS NOT NULL
        INSERT INTO @fail VALUES (N'audit columns', N'one or more of the three names already exists');

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
        INSERT INTO @fail VALUES (N'ModifiedDateTime default', N'missing the captured 330-minute expression');

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
          AND name = N'PK__SalesDai__4D7B4ABD9107B9CA'
          AND type = 'PK'
    )
        INSERT INTO @fail VALUES (N'primary key', N'PK__SalesDai__4D7B4ABD9107B9CA missing');

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
          AND name = N'FK_KMHistory_Activity'
    )
        INSERT INTO @fail VALUES (N'foreign key', N'FK_KMHistory_Activity missing');

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')
          AND name = N'IX_SalesDailyActivityKMHistory_Activity'
    )
        INSERT INTO @fail VALUES (N'index', N'IX_SalesDailyActivityKMHistory_Activity missing');

    IF (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory')) <> 2
        INSERT INTO @fail VALUES (N'index count', N'expected the clustered key and one nonclustered index');

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory'))
        INSERT INTO @fail VALUES (N'check constraint', N'unexpected check constraint');

    IF EXISTS (SELECT 1 FROM sys.triggers WHERE parent_id = OBJECT_ID(N'dbo.SalesDailyActivityKMHistory'))
        INSERT INTO @fail VALUES (N'trigger', N'unexpected trigger on the history table');
END

IF EXISTS
(
    SELECT 1
    FROM sys.views AS v
    INNER JOIN sys.sql_modules AS m ON m.object_id = v.object_id
    WHERE m.definition LIKE N'%SalesDailyActivityKMHistory%'
)
    INSERT INTO @fail VALUES (N'view', N'a view references SalesDailyActivityKMHistory');

DECLARE @counters TABLE
(
    ColumnName sysname NOT NULL PRIMARY KEY,
    IsNullable bit NOT NULL,
    TypeName sysname NOT NULL
);

INSERT INTO @counters VALUES
 (N'TotalActivities', 0, N'int'),
 (N'PendingActivities', 0, N'int'),
 (N'ApprovedActivities', 0, N'int'),
 (N'RejectedActivities', 0, N'int'),
 (N'ReturnedActivities', 0, N'int');

INSERT INTO @parked (Item, Detail)
SELECT CONCAT(N'counter ', c.ColumnName), N'missing or not int NOT NULL'
FROM @counters AS c
WHERE NOT EXISTS
(
    SELECT 1
    FROM sys.columns AS col
    WHERE col.object_id = OBJECT_ID(N'dbo.SalesApprovalRequest')
      AND col.name = c.ColumnName
      AND TYPE_NAME(col.user_type_id) = c.TypeName
      AND col.is_nullable = c.IsNullable
);

INSERT INTO @parked (Item, Detail)
SELECT CONCAT(N'counter default ', c.ColumnName), N'expected a zero default'
FROM @counters AS c
WHERE NOT EXISTS
(
    SELECT 1
    FROM sys.columns AS col
    INNER JOIN sys.default_constraints AS dc
        ON dc.parent_object_id = col.object_id
       AND dc.parent_column_id = col.column_id
    WHERE col.object_id = OBJECT_ID(N'dbo.SalesApprovalRequest')
      AND col.name = c.ColumnName
      AND REPLACE(dc.definition, N' ', N'') IN (N'((0))', N'(0)')
);

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns AS col
    WHERE col.object_id = OBJECT_ID(N'dbo.SalesApprovalRequest')
      AND col.name = N'FinalApprovedAmount'
      AND TYPE_NAME(col.user_type_id) = N'decimal'
      AND col.precision = 18
      AND col.scale = 2
      AND col.is_nullable = 1
      AND NOT EXISTS
      (
          SELECT 1 FROM sys.default_constraints AS dc
          WHERE dc.parent_object_id = col.object_id
            AND dc.parent_column_id = col.column_id
      )
)
    INSERT INTO @parked VALUES (N'FinalApprovedAmount', N'expected decimal(18,2) NULL with no default');

DECLARE @employee nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_UpdateActivityKM'));
IF @employee IS NULL
    OR CHARINDEX(N'SalesDailyActivityKMHistory', @employee) = 0
    OR CHARINDEX(N'RouteWaypointsJSON', @employee) = 0
    OR CHARINDEX(N'ApproverLevel', @employee) > 0
    INSERT INTO @fail VALUES (N'usp_UpdateActivityKM', N'explicit history insert preimage was not confirmed');

DECLARE @reader nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_GetSalesDailyActivityKMHistory'));
IF @reader IS NULL
    OR (@reader LIKE N'%SELECT *%' AND @reader LIKE N'%SalesDailyActivityKMHistory%')
    INSERT INTO @fail VALUES (N'usp_GetSalesDailyActivityKMHistory', N'missing, or it selects every history column');

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
    INSERT INTO @fail VALUES (N'history insert', N'a procedure inserts into the history table without the known column list');

SELECT N'FAIL' AS Grid, Item, Detail FROM @fail ORDER BY Item;
SELECT N'PARKED_WP03' AS Grid, Item, Detail FROM @parked ORDER BY Item;

SELECT
      m.ObjectName
    , LEN(OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + m.ObjectName))) AS ModuleLen
    , DATALENGTH(OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + m.ObjectName))) AS ModuleBytes
    , CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + m.ObjectName)))), 2) AS Sha256
    , p.modify_date
    , OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + m.ObjectName)) AS ModuleText
FROM @modules AS m
INNER JOIN sys.procedures AS p
    ON p.object_id = OBJECT_ID(N'dbo.' + m.ObjectName);

SELECT
      N'UNBOUND' AS PinStatus
    , v.ObjectName
    , CASE WHEN d.definition IS NULL THEN N'MISSING' ELSE N'PRESENT' END AS ObjectState
    , LEN(d.definition) AS ModuleLen
    , DATALENGTH(d.definition) AS ModuleBytes
    , CONVERT(char(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), d.definition)), 2) AS Sha256
    , d.definition AS ModuleText
FROM (VALUES (N'usp_ResolveRegion'), (N'usp_ResolveBranch'), (N'usp_ResolveGrade')) AS v(ObjectName)
CROSS APPLY (SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + v.ObjectName)) AS definition) AS d;

DECLARE @historyRows bigint = NULL;
IF OBJECT_ID(N'dbo.SalesDailyActivityKMHistory', N'U') IS NOT NULL
    EXEC sys.sp_executesql
        N'SELECT @n = COUNT_BIG(*) FROM dbo.SalesDailyActivityKMHistory;',
        N'@n bigint OUTPUT',
        @n = @historyRows OUTPUT;

SELECT
      DB_NAME() AS DatabaseName
    , @server AS ServerName
    , SUSER_SNAME() AS LoginName
    , DATABASEPROPERTYEX(DB_NAME(), 'Updateability') AS Updateability
    , @historyRows AS HistoryRows;

IF EXISTS (SELECT 1 FROM @fail)
    THROW 59508, N'ABORT: preflight mismatch. The failure grid is the detail. No object was changed.', 1;

PRINT N'PREFLIGHT PINNED CHECKS RETURNED NO FAILURE ROW. Resolver fingerprints are UNBOUND. WP-02 aborts until a reviewed revision pins them. Counter rows are PARKED with WP-03 and do not gate this package. File 40 must not be executed. This result is not deployment acceptance.';
