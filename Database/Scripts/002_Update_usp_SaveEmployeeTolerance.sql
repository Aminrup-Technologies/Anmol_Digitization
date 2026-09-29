/*
    RC-019: Complete implementation for dbo.usp_SaveEmployeeTolerance.

    Purpose:
      - Adds support for MaxKmModificationCount.
      - Preserves all existing insert/update behavior.
      - Normalizes invalid values to a minimum of 1.

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
    @MaxKmModificationCount INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF ISNULL(@MaxKmModificationCount, 0) < 1
        SET @MaxKmModificationCount = 1;

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