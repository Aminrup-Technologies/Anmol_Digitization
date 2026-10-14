/*
    Package:     Database/Deployments/L2R19_UAT
    Script:      A_Preflight_ReadOnly_UAT.sql
    Database:    AnmolDristiDev ONLY
    Server:      MUMANMOAPP01\SQLEXPRESS ONLY
    Live:        FORBIDDEN (AnmolDristi)

    Read-only. It does not ALTER, INSERT, UPDATE, DELETE, EXEC a procedure,
    back up the database, or read claims 97, 100, 111, 112, 118, 119, 121, or 122.

    Save every result set with Results to File. The grid truncates nvarchar(max).
    The module result is the source for D_Rollback. Paste a definition only after
    its saved text hashes to the sha256 column with HASHBYTES SHA2_256 over
    UTF-16LE. Do not open B_Apply from this script.
*/

SET NOCOUNT ON;

SELECT
      DB_NAME() AS DatabaseName
    , CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName')) AS ServerPropertyName
    , @@SERVERNAME AS AtServerName
    , CASE
          WHEN DB_NAME() = N'AnmolDristiDev'
           AND UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) = N'MUMANMOAPP01\SQLEXPRESS'
              THEN N'IDENTITY MATCHES THE AUTHORIZED UAT TARGET'
          ELSE N'STOP. This is not AnmolDristiDev on MUMANMOAPP01\SQLEXPRESS.'
      END AS IdentityResult;

IF DB_NAME() <> N'AnmolDristiDev'
   OR UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
    THROW 58302, N'ABORT: connected database is not AnmolDristiDev on MUMANMOAPP01\SQLEXPRESS. No module was changed.', 1;

SELECT
      o.name AS procedure_name
    , CONVERT(varchar(23), o.modify_date, 121) AS modify_date
    , LEN(m.definition) AS len_chars
    , DATALENGTH(m.definition) AS data_length
    , CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), m.definition)), 2) AS sha256
    , CASE
          WHEN o.name IN (
                N'usp_UpdateSalesDailyExpenseStatus',
                N'usp_ProcessApprovalAction',
                N'usp_SubmitSalesDailyActivities',
                N'usp_MarkRequestReadyForPayment',
                N'usp_RecalculateApprovalRequest'
          ) THEN N'SAVE definition and sha256. This is a rollback preimage only if it is the live module before apply.'
          ELSE N'DEPENDENCY. Record the hash. This package must not change it.'
      END AS SaveInstruction
FROM sys.objects AS o
INNER JOIN sys.sql_modules AS m ON m.object_id = o.object_id
WHERE o.type = N'P'
  AND SCHEMA_NAME(o.schema_id) = N'dbo'
  AND o.name IN (
        N'usp_UpdateSalesDailyExpenseStatus',
        N'usp_ProcessApprovalAction',
        N'usp_SubmitSalesDailyActivities',
        N'usp_MarkRequestReadyForPayment',
        N'usp_RecalculateApprovalRequest',
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
      v.object_name
    , CASE WHEN v.object_id IS NULL THEN N'FAIL' ELSE N'PASS' END AS Verification
FROM (VALUES
    (N'dbo.TVP_ActivityUpdate', TYPE_ID(N'dbo.TVP_ActivityUpdate')),
    (N'dbo.usp_ForwardApprovalRequest', OBJECT_ID(N'dbo.usp_ForwardApprovalRequest', N'P')),
    (N'SalesApprovalRequest.FinalApprovedAmount', COL_LENGTH(N'dbo.SalesApprovalRequest', N'FinalApprovedAmount')),
    (N'SalesApprovalRequest.CurrentApproverLoginId', COL_LENGTH(N'dbo.SalesApprovalRequest', N'CurrentApproverLoginId')),
    (N'SalesApprovalRequest.CurrentApproverLevel', COL_LENGTH(N'dbo.SalesApprovalRequest', N'CurrentApproverLevel')),
    (N'SalesApprovalRequest.Status', COL_LENGTH(N'dbo.SalesApprovalRequest', N'Status')),
    (N'SalesApprovalRequestDetail.DetailVersion', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'DetailVersion')),
    (N'SalesApprovalRequestDetail.ApprovalRequestDetailId', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'ApprovalRequestDetailId')),
    (N'SalesApprovalRequestDetail.DetailApprovedLevel', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'DetailApprovedLevel')),
    (N'SalesDailyExpense.ClaimStatus', COL_LENGTH(N'dbo.SalesDailyExpense', N'ClaimStatus')),
    (N'SalesDailyExpense.IsDeleted', COL_LENGTH(N'dbo.SalesDailyExpense', N'IsDeleted')),
    (N'SalesApprovalRequestApprover.ApproverRole', COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApproverRole')),
    (N'ApprovalRequestStatusHistory.OldStatus', COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'OldStatus'))
) AS v(object_name, object_id);

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

SELECT TOP (5)
      b.database_name
    , b.type
    , b.backup_start_date
    , b.backup_finish_date
    , b.is_damaged
    , b.has_backup_checksums
    , f.physical_device_name
    , CASE
          WHEN b.backup_finish_date > CONVERT(datetime2(3), '2026-10-12 14:02:57.583')
           AND b.is_damaged = 0
           AND b.has_backup_checksums = 1
           AND b.type = N'D'
              THEN N'CANDIDATE. Run RESTORE VERIFYONLY WITH CHECKSUM before any apply. This script does not run it.'
          ELSE N'DOES NOT QUALIFY'
      END AS BackupGate
FROM msdb.dbo.backupset AS b
INNER JOIN msdb.dbo.backupmediafamily AS f ON f.media_set_id = b.media_set_id
WHERE b.database_name = N'AnmolDristiDev'
  AND b.type = N'D'
ORDER BY b.backup_finish_date DESC;

SELECT
      o.name AS procedure_name
    , m.definition
FROM sys.objects AS o
INNER JOIN sys.sql_modules AS m ON m.object_id = o.object_id
WHERE o.type = N'P'
  AND SCHEMA_NAME(o.schema_id) = N'dbo'
  AND o.name IN (
        N'usp_UpdateSalesDailyExpenseStatus',
        N'usp_ProcessApprovalAction',
        N'usp_SubmitSalesDailyActivities',
        N'usp_MarkRequestReadyForPayment',
        N'usp_RecalculateApprovalRequest'
  )
ORDER BY o.name;