/*
    RC-019: Complete implementation for dbo.usp_SaveEmployeeTolerance.

    Purpose:
      - Adds support for MaxKmModificationCount.
      - Preserves all existing insert/update behavior.
      - Normalizes an omitted, null, or sub-1 count to the global default of 2.
      - An explicit count of 1 or greater is stored as supplied.

    Dependency:
      - Requires 001_Add_MaxKmModificationCount.sql.
*/

ALTER PROCEDURE [dbo].[usp_SaveEmployeeTolerance]
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
END;
GO