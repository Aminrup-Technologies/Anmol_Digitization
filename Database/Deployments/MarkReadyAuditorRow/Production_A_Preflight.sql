/*
    Package:     Database/Deployments/MarkReadyAuditorRow
    Target:      AnmolDristi on MUMANMOAPP01\SQLEXPRESS only.
    Refuses:     AnmolDristiDev and every other database name.
    Data:        No claim, approver, history, detail, or expense row is written.
    Requests:    100, 121, 130, and 131 are not referenced as update targets.

    Prepared 2026-10-14 from a read-back. @Execute defaults to 0 where a write
    exists. Generating this file did not execute it.

    Production preimage, read from AnmolDristi:
      object_id 1860917701
      modify_date 2026-10-08 12:46:41.807
      LEN 2223 / DATALENGTH 4446
      SHA2_256 6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451
      File: usp_MarkRequestReadyForPayment_AnmolDristi_preimage.txt
      Do not execute that file.

    UAT-verified stored form, read from AnmolDristiDev:
      modify_date 2026-10-13 14:11:49.980
      LEN 11181 / DATALENGTH 22362
      SHA2_256 E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409
      Repository source uses CREATE OR ALTER. SQL Server stores CREATE plus
      three spaces. That is the only repository-to-UAT text difference.
*/
SET NOCOUNT ON;

IF DB_NAME() = N'AnmolDristiDev'
    THROW 58401, N'ABORT: AnmolDristiDev is not the Production target.', 1;

IF DB_NAME() <> N'AnmolDristi'
    THROW 58402, N'ABORT: connected database is not AnmolDristi.', 1;

IF UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
    THROW 58403, N'ABORT: server is not MUMANMOAPP01\SQLEXPRESS.', 1;

DECLARE @ObjectId int = OBJECT_ID(N'dbo.usp_MarkRequestReadyForPayment', N'P');
IF @ObjectId IS NULL OR @ObjectId <> 1860917701
    THROW 58404, N'ABORT: procedure object id is not 1860917701.', 1;

DECLARE @Definition nvarchar(max);
DECLARE @ModifyDate datetime;
SELECT @Definition = m.definition, @ModifyDate = o.modify_date
FROM sys.sql_modules AS m
INNER JOIN sys.objects AS o ON o.object_id = m.object_id
WHERE m.object_id = @ObjectId;

IF LEN(@Definition) <> 2223 OR DATALENGTH(@Definition) <> 4446
    THROW 58405, N'ABORT: Production definition length is not 2223 / 4446.', 1;

IF CONVERT(varchar(23), @ModifyDate, 121) <> '2026-10-08 12:46:41.807'
    THROW 58406, N'ABORT: Production modify_date is not 2026-10-08 12:46:41.807.', 1;

DECLARE @Hash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Definition)), 2);
IF @Hash <> '6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451'
    THROW 58407, N'ABORT: Production definition hash is not the pinned preimage.', 1;

IF CHARINDEX(N'59110', @Definition) > 0
    THROW 58408, N'ABORT: preimage unexpectedly already contains 59110.', 1;

PRINT N'PREFLIGHT PASS | AnmolDristi still has the 2223-character preimage. No procedure was changed.';