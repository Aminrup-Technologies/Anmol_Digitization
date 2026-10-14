/*
    UAT rollback for the KM modification default change.
    Restores the captured procedure bodies and the column default ((1)).
    Does not change employee rows or KM history.
    @Execute must stay 0 until a separate execution is approved.
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
    THROW 58550, N'Refusing catalog AnmolDristi.', 1;
IF @Db <> N'AnmolDristiDev'
    THROW 58551, N'Catalog must be AnmolDristiDev.', 1;
IF @Server <> N'MUMANMOAPP01\SQLEXPRESS'
    THROW 58552, N'Server must be MUMANMOAPP01\SQLEXPRESS.', 1;

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
IF @LiveSave <> @NextSave OR @LiveGet <> @NextGet
    THROW 58553, N'Current procedures are not the deployed KM-default bodies. Nothing was changed.', 1;

DECLARE @DefaultDef nvarchar(max);
SELECT @DefaultDef = dc.definition
FROM sys.default_constraints AS dc
INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
INNER JOIN sys.tables AS tb ON tb.object_id = c.object_id
WHERE tb.name = N'MST_EmployeeTolerance' AND c.name = N'MaxKmModificationCount';
IF @DefaultDef <> N'((2))'
    THROW 58554, N'Column default is not ((2)). Nothing was changed.', 1;

BEGIN TRAN;
DECLARE @AlterSave nvarchar(max) = STUFF(@PrevSave, CHARINDEX(N'CREATE', @PrevSave), 6, N'ALTER');
DECLARE @AlterGet nvarchar(max) = STUFF(@PrevGet, CHARINDEX(N'CREATE', @PrevGet), 6, N'ALTER');
EXEC sys.sp_executesql @AlterSave;
EXEC sys.sp_executesql @AlterGet;
ALTER TABLE dbo.MST_EmployeeTolerance DROP CONSTRAINT [DF_MST_EmployeeTolerance_MaxKmModificationCount];
ALTER TABLE dbo.MST_EmployeeTolerance ADD CONSTRAINT [DF_MST_EmployeeTolerance_MaxKmModificationCount] DEFAULT ((1)) FOR [MaxKmModificationCount];

DECLARE @StoredSave nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_SaveEmployeeTolerance'));
DECLARE @StoredGet nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_GetEmployeeTolerance'));
SELECT @DefaultDef = dc.definition
FROM sys.default_constraints AS dc
INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
INNER JOIN sys.tables AS tb ON tb.object_id = c.object_id
WHERE tb.name = N'MST_EmployeeTolerance' AND c.name = N'MaxKmModificationCount';
IF @StoredSave <> @PrevSave OR @StoredGet <> @PrevGet OR @DefaultDef <> N'((1))'
BEGIN
    ROLLBACK TRAN;
    THROW 58555, N'Rollback post-check failed. Transaction rolled back.', 1;
END
COMMIT TRAN;
PRINT N'COMMITTED | Prior KM modification default and procedure bodies were restored. Employee rows were not updated.';