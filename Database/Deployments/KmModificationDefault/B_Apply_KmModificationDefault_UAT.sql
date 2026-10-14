/*
    UAT only. Global KM modification default is 2.
    An active MST_EmployeeTolerance row keeps its stored MaxKmModificationCount.
    This script does not update existing employee rows.
    Omitted, null, and sub-1 values use 2.
    @Execute must stay 0 in the repository copy.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Execute bit = 0;
IF @Execute = 0
BEGIN
    PRINT N'NOT EXECUTED | @Execute = 0. No database change was made.';
    RETURN;
END

DECLARE @Db sysname = DB_NAME();
DECLARE @Server nvarchar(128) = CONVERT(nvarchar(128), SERVERPROPERTY('ServerName'));
IF @Db = N'AnmolDristi'
    THROW 58540, N'Refusing catalog AnmolDristi.', 1;
IF @Db <> N'AnmolDristiDev'
    THROW 58541, N'Catalog must be AnmolDristiDev.', 1;
IF @Server <> N'MUMANMOAPP01\SQLEXPRESS'
    THROW 58542, N'Server must be MUMANMOAPP01\SQLEXPRESS.', 1;

DECLARE @PrevSave nvarchar(max) = N'
CREATE   PROCEDURE [dbo].[usp_SaveEmployeeTolerance]
    @ToleranceId INT = 0,                   -- 0 => insert
    @EmployeeCode NVARCHAR(100),
    @MinAllowedKM DECIMAL(10,2) = NULL,
    @MaxAllowedKM DECIMAL(10,2) = NULL,
    @EffectiveFrom DATE = NULL,
    @EffectiveTo DATE = NULL,
    @ModifiedBy NVARCHAR(100),
	@MaxKmModificationCount INT = 1
AS
BEGIN
    SET NOCOUNT ON;

	IF ISNULL(@MaxKmModificationCount, 0) < 1
        SET @MaxKmModificationCount = 1;

    IF @ToleranceId = 0
    BEGIN
        INSERT INTO dbo.MST_EmployeeTolerance
            (EmployeeCode, MinAllowedKM, MaxAllowedKM, EffectiveFrom, EffectiveTo, CreatedBy, CreatedDateTime, ModifiedBy, ModifiedDateTime, IsActive)
        VALUES
            (@EmployeeCode, @MinAllowedKM, @MaxAllowedKM, @EffectiveFrom, @EffectiveTo, @ModifiedBy, DATEADD(MINUTE,330,SYSUTCDATETIME()), @ModifiedBy, DATEADD(MINUTE,330,SYSUTCDATETIME()), 1);

        SELECT SCOPE_IDENTITY() AS NewToleranceId;
    END
    ELSE
    BEGIN
        UPDATE dbo.MST_EmployeeTolerance
        SET EmployeeCode = @EmployeeCode,
            MinAllowedKM = @MinAllowedKM,
            MaxAllowedKM = @MaxAllowedKM,
            EffectiveFrom = @EffectiveFrom,
            EffectiveTo = @EffectiveTo,
            ModifiedBy = @ModifiedBy,
            ModifiedDateTime = DATEADD(MINUTE,330,SYSUTCDATETIME()),
            IsActive = 1
        WHERE ToleranceId = @ToleranceId;

        SELECT @ToleranceId AS NewToleranceId;
    END
END
';
DECLARE @NextSave nvarchar(max) = N'
CREATE   PROCEDURE [dbo].[usp_SaveEmployeeTolerance]
    @ToleranceId INT = 0,                   -- 0 => insert
    @EmployeeCode NVARCHAR(100),
    @MinAllowedKM DECIMAL(10,2) = NULL,
    @MaxAllowedKM DECIMAL(10,2) = NULL,
    @EffectiveFrom DATE = NULL,
    @EffectiveTo DATE = NULL,
    @ModifiedBy NVARCHAR(100),
    @MaxKmModificationCount INT = 2
AS
BEGIN
    SET NOCOUNT ON;

    IF @MaxKmModificationCount IS NULL OR @MaxKmModificationCount < 1
        SET @MaxKmModificationCount = 2;

    IF @ToleranceId = 0
    BEGIN
        INSERT INTO dbo.MST_EmployeeTolerance
        (
            EmployeeCode,
            MinAllowedKM,
            MaxAllowedKM,
            MaxKmModificationCount,
            EffectiveFrom,
            EffectiveTo,
            CreatedBy,
            CreatedDateTime,
            ModifiedBy,
            ModifiedDateTime,
            IsActive
        )
        VALUES
        (
            @EmployeeCode,
            @MinAllowedKM,
            @MaxAllowedKM,
            @MaxKmModificationCount,
            @EffectiveFrom,
            @EffectiveTo,
            @ModifiedBy,
            DATEADD(MINUTE,330,SYSUTCDATETIME()),
            @ModifiedBy,
            DATEADD(MINUTE,330,SYSUTCDATETIME()),
            1
        );

        SELECT SCOPE_IDENTITY() AS NewToleranceId;
    END
    ELSE
    BEGIN
        UPDATE dbo.MST_EmployeeTolerance
        SET EmployeeCode = @EmployeeCode,
            MinAllowedKM = @MinAllowedKM,
            MaxAllowedKM = @MaxAllowedKM,
            MaxKmModificationCount = @MaxKmModificationCount,
            EffectiveFrom = @EffectiveFrom,
            EffectiveTo = @EffectiveTo,
            ModifiedBy = @ModifiedBy,
            ModifiedDateTime = DATEADD(MINUTE,330,SYSUTCDATETIME()),
            IsActive = 1
        WHERE ToleranceId = @ToleranceId;

        SELECT @ToleranceId AS NewToleranceId;
    END
END
';
DECLARE @PrevGet nvarchar(max) = N'
-- 10. dbo.usp_GetEmployeeTolerance

CREATE     PROCEDURE [dbo].[usp_GetEmployeeTolerance]
    @EmployeeCode NVARCHAR(100) = NULL,
    @EffectiveDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ToleranceId,
        EmployeeCode,
        MinAllowedKM,
        MaxAllowedKM,
        ISNULL(MaxKmModificationCount, 1) AS MaxKmModificationCount,
        EffectiveFrom,
        EffectiveTo,
        CreatedBy,
        CreatedDateTime,
        ModifiedBy,
        ModifiedDateTime,
        IsActive
    FROM dbo.MST_EmployeeTolerance WITH (NOLOCK)
    WHERE (IsActive = 1)
      AND (@EmployeeCode IS NULL OR @EmployeeCode = '''' OR EmployeeCode = @EmployeeCode)
      AND (
            @EffectiveDate IS NULL
            OR (
                (EffectiveFrom IS NULL OR EffectiveFrom <= @EffectiveDate)
                AND (EffectiveTo IS NULL OR EffectiveTo >= @EffectiveDate)
               )
          )
    ORDER BY EmployeeCode, EffectiveFrom, ToleranceId;
END
';
DECLARE @NextGet nvarchar(max) = N'
-- 10. dbo.usp_GetEmployeeTolerance

CREATE     PROCEDURE [dbo].[usp_GetEmployeeTolerance]
    @EmployeeCode NVARCHAR(100) = NULL,
    @EffectiveDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ToleranceId,
        EmployeeCode,
        MinAllowedKM,
        MaxAllowedKM,
        ISNULL(MaxKmModificationCount, 2) AS MaxKmModificationCount,
        EffectiveFrom,
        EffectiveTo,
        CreatedBy,
        CreatedDateTime,
        ModifiedBy,
        ModifiedDateTime,
        IsActive
    FROM dbo.MST_EmployeeTolerance WITH (NOLOCK)
    WHERE (IsActive = 1)
      AND (@EmployeeCode IS NULL OR @EmployeeCode = '''' OR EmployeeCode = @EmployeeCode)
      AND (
            @EffectiveDate IS NULL
            OR (
                (EffectiveFrom IS NULL OR EffectiveFrom <= @EffectiveDate)
                AND (EffectiveTo IS NULL OR EffectiveTo >= @EffectiveDate)
               )
          )
    ORDER BY EmployeeCode, EffectiveFrom, ToleranceId;
END
';

DECLARE @LiveSave nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_SaveEmployeeTolerance'));
DECLARE @LiveGet nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_GetEmployeeTolerance'));
IF @LiveSave IS NULL OR @LiveGet IS NULL
    THROW 58543, N'Tolerance procedure is missing.', 1;
IF @LiveSave <> @PrevSave OR LEN(@LiveSave) <> 1513
    OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @LiveSave)), 2) <> 'E959D3BBADAC7F2010CAE85AE1CFA7A6CBD33DB61F438D9538F415F4A1F9924D'
    THROW 58544, N'Save procedure is not the expected preimage.', 1;
IF @LiveGet <> @PrevGet OR LEN(@LiveGet) <> 1010
    OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @LiveGet)), 2) <> '408255085E1D294C18893713DDCF14AD53349CBFF88305A7BECC1517BAD774F8'
    THROW 58545, N'Get procedure is not the expected preimage.', 1;

DECLARE @DefaultDef nvarchar(max);
SELECT @DefaultDef = dc.definition
FROM sys.default_constraints AS dc
INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
INNER JOIN sys.tables AS tb ON tb.object_id = c.object_id
WHERE tb.name = N'MST_EmployeeTolerance'
  AND c.name = N'MaxKmModificationCount'
  AND dc.name = N'DF_MST_EmployeeTolerance_MaxKmModificationCount';
IF @DefaultDef <> N'((1))'
    THROW 58546, N'Column default is not ((1)).', 1;

IF NOT (
    EXISTS (SELECT 1 FROM dbo.MST_EmployeeTolerance WHERE ToleranceId = 1 AND EmployeeCode = N'AHS1562' AND MaxKmModificationCount = 2 AND IsActive = 1)
    AND EXISTS (SELECT 1 FROM dbo.MST_EmployeeTolerance WHERE ToleranceId = 2 AND EmployeeCode = N'M1367' AND MaxKmModificationCount = 1 AND IsActive = 1)
    AND EXISTS (SELECT 1 FROM dbo.MST_EmployeeTolerance WHERE ToleranceId = 3 AND EmployeeCode = N'AHS1530' AND MaxKmModificationCount = 1 AND IsActive = 1)
    AND (SELECT COUNT(*) FROM dbo.MST_EmployeeTolerance) = 3
)
    THROW 58547, N'Existing tolerance rows are not the expected preserved set.', 1;

BEGIN TRAN;

DECLARE @AlterSave nvarchar(max) = STUFF(@NextSave, CHARINDEX(N'CREATE', @NextSave), 6, N'ALTER');
DECLARE @AlterGet nvarchar(max) = STUFF(@NextGet, CHARINDEX(N'CREATE', @NextGet), 6, N'ALTER');
EXEC sys.sp_executesql @AlterSave;
EXEC sys.sp_executesql @AlterGet;

ALTER TABLE dbo.MST_EmployeeTolerance DROP CONSTRAINT [DF_MST_EmployeeTolerance_MaxKmModificationCount];
ALTER TABLE dbo.MST_EmployeeTolerance ADD CONSTRAINT [DF_MST_EmployeeTolerance_MaxKmModificationCount] DEFAULT ((2)) FOR [MaxKmModificationCount];

DECLARE @StoredSave nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_SaveEmployeeTolerance'));
DECLARE @StoredGet nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_GetEmployeeTolerance'));
SELECT @DefaultDef = dc.definition
FROM sys.default_constraints AS dc
INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
INNER JOIN sys.tables AS tb ON tb.object_id = c.object_id
WHERE tb.name = N'MST_EmployeeTolerance'
  AND c.name = N'MaxKmModificationCount'
  AND dc.name = N'DF_MST_EmployeeTolerance_MaxKmModificationCount';

IF @StoredSave <> @NextSave OR @StoredGet <> @NextGet OR @DefaultDef <> N'((2))'
    OR NOT (
        EXISTS (SELECT 1 FROM dbo.MST_EmployeeTolerance WHERE ToleranceId = 1 AND EmployeeCode = N'AHS1562' AND MaxKmModificationCount = 2)
        AND EXISTS (SELECT 1 FROM dbo.MST_EmployeeTolerance WHERE ToleranceId = 2 AND EmployeeCode = N'M1367' AND MaxKmModificationCount = 1)
        AND EXISTS (SELECT 1 FROM dbo.MST_EmployeeTolerance WHERE ToleranceId = 3 AND EmployeeCode = N'AHS1530' AND MaxKmModificationCount = 1)
        AND (SELECT COUNT(*) FROM dbo.MST_EmployeeTolerance) = 3
    )
BEGIN
    ROLLBACK TRAN;
    THROW 58548, N'Post-check failed. Transaction rolled back. Employee rows were not updated by this script.', 1;
END

COMMIT TRAN;
PRINT N'COMMITTED | KM modification default is 2. Existing tolerance rows were not updated.';