/*
    Policy Exception maintenance stored procedures.
    Assumes existing dbo.PolicyException table (no schema changes).
*/

IF OBJECT_ID('dbo.usp_SearchPolicyExceptions', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SearchPolicyExceptions;
GO

CREATE PROCEDURE dbo.usp_SearchPolicyExceptions
    @EmployeeCode VARCHAR(50) = NULL,
    @EmployeeName NVARCHAR(200) = NULL,
    @RegionId INT = NULL,
    @BranchId INT = NULL,
    @DesignationId INT = NULL,
    @IsActive BIT = NULL,
    @EffectiveDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        pe.ExceptionId,
        pe.EmployeeCode,
        ISNULL(u.EmployeeName, '') AS EmployeeName,
        pe.RegionId,
        pe.BranchId,
        pe.GradeId,
        pe.DesignationId,
        r.Region_Description AS RegionName,
        b.Branch_Description AS BranchName,
        d.Designation_Description AS DesignationName,
        pe.Priority,
        pe.EffectiveFrom,
        pe.EffectiveTo,
        pe.IsActive,
        pe.ModifiedBy,
        pe.ModifiedOn
    FROM dbo.PolicyException pe WITH (NOLOCK)
    LEFT JOIN dbo.MST_UserMaster u WITH (NOLOCK) ON u.EmployeeCode = pe.EmployeeCode
    LEFT JOIN dbo.MST_Region r WITH (NOLOCK) ON r.Id = pe.RegionId
    LEFT JOIN dbo.MST_Branch b WITH (NOLOCK) ON b.Id = pe.BranchId
    LEFT JOIN dbo.MST_Designation d WITH (NOLOCK) ON d.Id = pe.DesignationId
    WHERE (@EmployeeCode IS NULL OR pe.EmployeeCode = @EmployeeCode)
      AND (@EmployeeName IS NULL OR u.EmployeeName LIKE @EmployeeName)
      AND (@RegionId IS NULL OR pe.RegionId = @RegionId)
      AND (@BranchId IS NULL OR pe.BranchId = @BranchId)
      AND (@DesignationId IS NULL OR pe.DesignationId = @DesignationId)
      AND (@IsActive IS NULL OR pe.IsActive = @IsActive)
      AND (
            @EffectiveDate IS NULL
            OR (pe.EffectiveFrom <= @EffectiveDate AND ISNULL(pe.EffectiveTo, '9999-12-31') >= @EffectiveDate)
          )
    ORDER BY pe.ModifiedOn DESC, pe.ExceptionId DESC;
END
GO

IF OBJECT_ID('dbo.usp_GetPolicyExceptionById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetPolicyExceptionById;
GO

CREATE PROCEDURE dbo.usp_GetPolicyExceptionById
    @ExceptionId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        pe.ExceptionId,
        pe.EmployeeCode,
        ISNULL(u.EmployeeName, '') AS EmployeeName,
        pe.RegionId,
        pe.BranchId,
        pe.GradeId,
        pe.DesignationId,
        r.Region_Description AS RegionName,
        b.Branch_Description AS BranchName,
        g.Grade_Description AS GradeName,
        d.Designation_Description AS DesignationName,
        pe.PolicyJson,
        pe.Priority,
        pe.EffectiveFrom,
        pe.EffectiveTo,
        pe.IsActive,
        pe.CreatedBy,
        pe.CreatedOn,
        pe.ModifiedBy,
        pe.ModifiedOn
    FROM dbo.PolicyException pe WITH (NOLOCK)
    LEFT JOIN dbo.MST_UserMaster u WITH (NOLOCK) ON u.EmployeeCode = pe.EmployeeCode
    LEFT JOIN dbo.MST_Region r WITH (NOLOCK) ON r.Id = pe.RegionId
    LEFT JOIN dbo.MST_Branch b WITH (NOLOCK) ON b.Id = pe.BranchId
    LEFT JOIN dbo.MST_Grade g WITH (NOLOCK) ON g.Id = pe.GradeId
    LEFT JOIN dbo.MST_Designation d WITH (NOLOCK) ON d.Id = pe.DesignationId
    WHERE pe.ExceptionId = @ExceptionId;
END
GO

IF OBJECT_ID('dbo.usp_InsertPolicyException', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_InsertPolicyException;
GO

CREATE PROCEDURE dbo.usp_InsertPolicyException
    @EmployeeCode VARCHAR(50),
    @RegionId INT = NULL,
    @BranchId INT = NULL,
    @GradeId INT = NULL,
    @DesignationId INT = NULL,
    @PolicyJson NVARCHAR(MAX),
    @Priority INT,
    @EffectiveFrom DATE,
    @EffectiveTo DATE = NULL,
    @IsActive BIT = 1,
    @User VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PolicyException
    (
        EmployeeCode, RegionId, BranchId, GradeId, DesignationId,
        PolicyJson, Priority, EffectiveFrom, EffectiveTo, IsActive,
        CreatedBy, CreatedOn, ModifiedBy, ModifiedOn
    )
    VALUES
    (
        @EmployeeCode, @RegionId, @BranchId, @GradeId, @DesignationId,
        @PolicyJson, @Priority, @EffectiveFrom, @EffectiveTo, @IsActive,
        @User, GETDATE(), @User, GETDATE()
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS ExceptionId;
END
GO

IF OBJECT_ID('dbo.usp_UpdatePolicyException', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_UpdatePolicyException;
GO

CREATE PROCEDURE dbo.usp_UpdatePolicyException
    @ExceptionId INT,
    @EmployeeCode VARCHAR(50),
    @RegionId INT = NULL,
    @BranchId INT = NULL,
    @GradeId INT = NULL,
    @DesignationId INT = NULL,
    @PolicyJson NVARCHAR(MAX),
    @Priority INT,
    @EffectiveFrom DATE,
    @EffectiveTo DATE = NULL,
    @IsActive BIT = 1,
    @User VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PolicyException
    SET EmployeeCode = @EmployeeCode,
        RegionId = @RegionId,
        BranchId = @BranchId,
        GradeId = @GradeId,
        DesignationId = @DesignationId,
        PolicyJson = @PolicyJson,
        Priority = @Priority,
        EffectiveFrom = @EffectiveFrom,
        EffectiveTo = @EffectiveTo,
        IsActive = @IsActive,
        ModifiedBy = @User,
        ModifiedOn = GETDATE()
    WHERE ExceptionId = @ExceptionId;
END
GO

IF OBJECT_ID('dbo.usp_SetPolicyExceptionActiveStatus', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetPolicyExceptionActiveStatus;
GO

CREATE PROCEDURE dbo.usp_SetPolicyExceptionActiveStatus
    @ExceptionId INT,
    @IsActive BIT,
    @ModifiedBy VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PolicyException
    SET IsActive = @IsActive,
        ModifiedBy = @ModifiedBy,
        ModifiedOn = GETDATE()
    WHERE ExceptionId = @ExceptionId;
END
GO
