/*
    Package:     Database/Deployments/L2R19_UAT
    Script:      C_PostApply_Verification_UAT.sql
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN (AnmolDristi)

    Read-only. It does not ALTER modules and it does not execute the five
    procedures. Run it only after an authorized apply commits.
    Catalog hashes use HASHBYTES SHA2_256 over the nvarchar definition as UTF-16LE.
*/

SET NOCOUNT ON;

IF DB_NAME() = N'AnmolDristi'
    THROW 58301, N'ABORT: Live database AnmolDristi is forbidden.', 1;

IF DB_NAME() <> N'AnmolDristiDev'
   OR UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
    THROW 58302, N'ABORT: connected database is not AnmolDristiDev on MUMANMOAPP01\SQLEXPRESS.', 1;

DECLARE @Expected TABLE (
    procedure_name sysname NOT NULL PRIMARY KEY,
    expected_len int NOT NULL,
    expected_sha256 varchar(64) NOT NULL
);

INSERT INTO @Expected (procedure_name, expected_len, expected_sha256)
VALUES
        (N'usp_RecalculateApprovalRequest', 7557, '3ECC03CBD6C1AFF40CF7F6B7ECF5E637399A7D07B4A6AA4AB1FB219231F7900F'),
        (N'usp_ProcessApprovalAction', 42021, '3E86754A9C605F752468C616D9923BD7B22F2396FE84363FAAB0E5CFBDA04EB9'),
        (N'usp_UpdateSalesDailyExpenseStatus', 12445, '7C9007750F36C24F696B56EBC1AD7FC0A2CEC6441AD45A00815934D4FBC7E040'),
        (N'usp_SubmitSalesDailyActivities', 28066, '18B9D5854ED396CCDD2CCC94AD80E6904FA1891C9DF3F907BD94D312F1A472BF'),
        (N'usp_MarkRequestReadyForPayment', 11179, '118A8333EC49E8C11AAA2BCE46F4490A3DE7D11FD9EC84E01D6990DA8EBB4274');

SELECT
      DB_NAME() AS DatabaseName
    , CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName')) AS ServerPropertyName;

SELECT
      e.procedure_name
    , CONVERT(varchar(23), o.modify_date, 121) AS modify_date
    , LEN(m.definition) AS len_chars
    , e.expected_len
    , CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), m.definition)), 2) AS sha256
    , e.expected_sha256
    , CASE
          WHEN m.definition IS NULL THEN N'FAIL missing procedure'
          WHEN LEN(m.definition) = e.expected_len
           AND CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), m.definition)), 2) = e.expected_sha256
              THEN N'PASS'
          ELSE N'FAIL'
      END AS Verification
FROM @Expected AS e
LEFT JOIN sys.objects AS o
    ON o.name = e.procedure_name
   AND o.type = N'P'
   AND SCHEMA_NAME(o.schema_id) = N'dbo'
LEFT JOIN sys.sql_modules AS m
    ON m.object_id = o.object_id
ORDER BY e.procedure_name;

SELECT
      o.name AS dependency_name
    , CONVERT(varchar(23), o.modify_date, 121) AS modify_date
    , LEN(m.definition) AS len_chars
    , CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), m.definition)), 2) AS sha256
    , CASE
          WHEN o.name = N'usp_ForwardApprovalRequest'
           AND m.definition LIKE N'%THROW 51013%' THEN N'COMPARE WITH PREFLIGHT. 51013 token present. This package must not change the hash.'
          WHEN o.name = N'usp_ForwardApprovalRequest' THEN N'FAIL 51013 token missing'
          ELSE N'COMPARE WITH PREFLIGHT. This package must not change the hash.'
      END AS Verification
FROM sys.objects AS o
INNER JOIN sys.sql_modules AS m ON m.object_id = o.object_id
WHERE o.type = N'P'
  AND SCHEMA_NAME(o.schema_id) = N'dbo'
  AND o.name IN (
        N'usp_ForwardApprovalRequest',
        N'usp_ReturnClaimToEmployee',
        N'usp_UpdateActivityKM',
        N'usp_UpdateActivityKM_WithHistory',
        N'usp_RecalculateSalesClaim',
        N'usp_GetApprovalRequest',
        N'usp_Export_Auditor_Final_Excel'
  )
ORDER BY o.name;

SELECT
      t.name AS table_name
    , i.name AS index_name
    , i.is_unique
    , i.has_filter
    , i.filter_definition
    , CASE
          WHEN i.name = N'IX_SalesApprovalRequestDetail_RequestId' AND i.has_filter = 0 AND i.is_unique = 0 THEN N'PASS'
          WHEN i.name = N'UQ_SalesDailyExpense_ActivityDateExpenseType_Seq'
           AND i.is_unique = 1 AND i.has_filter = 1 AND i.filter_definition = N'([IsDeleted]=(0))' THEN N'PASS'
          WHEN i.name = N'UX_SAR_Auditor_Once'
           AND i.is_unique = 1 AND i.has_filter = 1 AND i.filter_definition = N'([ApproverRole]=''Auditor'')' THEN N'PASS'
          ELSE N'FAIL'
      END AS Verification
FROM sys.indexes AS i
INNER JOIN sys.tables AS t ON t.object_id = i.object_id
WHERE i.name IN (
      N'IX_SalesApprovalRequestDetail_RequestId',
      N'UQ_SalesDailyExpense_ActivityDateExpenseType_Seq',
      N'UX_SAR_Auditor_Once'
  )
ORDER BY t.name, i.name;