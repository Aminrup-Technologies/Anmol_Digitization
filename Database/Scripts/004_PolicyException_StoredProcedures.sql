/*
    Policy Exception stored procedures (production definitions).
    Deploy to AnmolDristiDev / target database before using ManagePolicyException.aspx.
*/

IF OBJECT_ID('dbo.usp_SearchPolicyExceptions', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SearchPolicyExceptions;
GO

CREATE PROCEDURE dbo.usp_SearchPolicyExceptions
(
      @EmployeeCode     NVARCHAR(50) = NULL
    , @EmployeeName     NVARCHAR(200) = NULL
    , @RegionId         INT = NULL
    , @BranchId         INT = NULL
    , @GradeId          INT = NULL
    , @DesignationId    INT = NULL
    , @IsActive         BIT = NULL
    , @EffectiveDate    DATE = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
          PE.ExceptionId
        , PE.EmployeeCode
        , E.EmployeeName
        , PE.RegionId
        , R.Region_Description
        , PE.BranchId
        , B.Branch_Description
        , PE.GradeId
        , G.Grade_Description
        , PE.DesignationId
        , D.Designation_Description
        , PE.Priority
        , PE.EffectiveFrom
        , PE.EffectiveTo
        , PE.IsActive
        , PE.CreatedBy
        , PE.CreatedOn
        , PE.ModifiedBy
        , PE.ModifiedOn
    FROM dbo.PolicyException PE
    LEFT JOIN dbo.MST_UserMaster E ON E.EmployeeCode = PE.EmployeeCode
    LEFT JOIN dbo.MST_Region R ON R.Region_ID = PE.RegionId
    LEFT JOIN dbo.MST_Branch B ON B.Branch_ID = PE.BranchId
    LEFT JOIN dbo.MST_Grade G ON G.Grade_ID = PE.GradeId
    LEFT JOIN dbo.MST_Designation D ON D.Id = PE.DesignationId
    WHERE (@EmployeeCode IS NULL OR PE.EmployeeCode LIKE '%' + @EmployeeCode + '%')
      AND (@EmployeeName IS NULL OR E.EmployeeName LIKE '%' + @EmployeeName + '%')
      AND (@RegionId IS NULL OR PE.RegionId = @RegionId)
      AND (@BranchId IS NULL OR PE.BranchId = @BranchId)
      AND (@GradeId IS NULL OR PE.GradeId = @GradeId)
      AND (@DesignationId IS NULL OR PE.DesignationId = @DesignationId)
      AND (@IsActive IS NULL OR PE.IsActive = @IsActive)
      AND (@EffectiveDate IS NULL OR (@EffectiveDate BETWEEN ISNULL(PE.EffectiveFrom,'19000101') AND ISNULL(PE.EffectiveTo,'99991231')))
    ORDER BY PE.EmployeeCode, PE.Priority, PE.EffectiveFrom DESC;
END
GO

IF OBJECT_ID('dbo.usp_GetPolicyExceptionById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetPolicyExceptionById;
GO

CREATE PROCEDURE dbo.usp_GetPolicyExceptionById
(
      @ExceptionId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
          PE.ExceptionId
        , PE.EmployeeCode
        , UM.EmployeeName
        , PE.RegionId
        , R.Region_Description AS RegionName
        , PE.BranchId
        , B.Branch_Description AS BranchName
        , PE.GradeId
        , G.Grade_Description AS GradeName
        , PE.DesignationId
        , D.Designation_Description AS DesignationName
        , PE.PolicyJson
        , PE.Priority
        , PE.EffectiveFrom
        , PE.EffectiveTo
        , PE.IsActive
        , PE.CreatedBy
        , PE.CreatedOn
        , PE.ModifiedBy
        , PE.ModifiedOn
    FROM dbo.PolicyException PE
    INNER JOIN dbo.MST_UserMaster UM ON UM.EmployeeCode = PE.EmployeeCode
    -- Join business keys stored on PolicyException (same keys as usp_SearchPolicyExceptions).
    -- MST_Region/Branch/Grade use Region_ID/Branch_ID/Grade_ID; surrogate Id does not match PE.*Id.
    -- MST_Designation uses Id (matches PE.DesignationId / UserMaster.Designation_ID).
    LEFT JOIN dbo.MST_Region R ON R.Region_ID = PE.RegionId
    LEFT JOIN dbo.MST_Branch B ON B.Branch_ID = PE.BranchId
    LEFT JOIN dbo.MST_Grade G ON G.Grade_ID = PE.GradeId
    LEFT JOIN dbo.MST_Designation D ON D.Id = PE.DesignationId
    WHERE PE.ExceptionId = @ExceptionId;
END
GO

IF OBJECT_ID('dbo.usp_InsertPolicyException', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_InsertPolicyException;
GO

CREATE PROCEDURE dbo.usp_InsertPolicyException
(
      @EmployeeCode      NVARCHAR(50)
    , @PolicyJson        NVARCHAR(MAX)
    , @Priority          INT
    , @EffectiveFrom     DATE
    , @EffectiveTo       DATE = NULL
    , @CreatedBy         NVARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NULLIF(LTRIM(RTRIM(@EmployeeCode)), '') IS NULL
            THROW 50001, 'Employee Code is required.', 1;
        IF NULLIF(LTRIM(RTRIM(@PolicyJson)), '') IS NULL
            THROW 50002, 'PolicyJson is required.', 1;
        IF @Priority <= 0
            THROW 50003, 'Priority must be greater than zero.', 1;
        IF @EffectiveFrom IS NULL
            THROW 50004, 'Effective From is required.', 1;
        IF @EffectiveTo IS NOT NULL AND @EffectiveTo < @EffectiveFrom
            THROW 50005, 'Effective To cannot be earlier than Effective From.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.MST_UserMaster WHERE EmployeeCode = @EmployeeCode)
            THROW 50006, 'Employee does not exist.', 1;

        IF EXISTS (SELECT 1 FROM dbo.MST_UserMaster WHERE EmployeeCode = @EmployeeCode AND ISNULL(EmployeeStatus,'') <> 'Active')
            THROW 50007, 'Employee is not active.', 1;

        IF EXISTS (
            SELECT 1 FROM dbo.PolicyException PE
            WHERE PE.EmployeeCode = @EmployeeCode AND PE.IsActive = 1
              AND (@EffectiveFrom <= ISNULL(PE.EffectiveTo,'99991231') AND ISNULL(@EffectiveTo,'99991231') >= PE.EffectiveFrom)
        )
            THROW 50008, 'An active Policy Exception already exists for the selected Effective Period.', 1;

        INSERT INTO dbo.PolicyException
        (EmployeeCode, RegionId, BranchId, GradeId, DesignationId, PolicyJson, Priority, EffectiveFrom, EffectiveTo, IsActive, CreatedBy, CreatedOn)
        SELECT UM.EmployeeCode, UM.RegionId, UM.BranchId, UM.GradeId, UM.Designation_ID, @PolicyJson, @Priority, @EffectiveFrom, @EffectiveTo, 1, @CreatedBy, SYSDATETIME()
        FROM dbo.MST_UserMaster UM
        WHERE UM.EmployeeCode = @EmployeeCode;

        DECLARE @ExceptionId INT = SCOPE_IDENTITY();
        COMMIT TRANSACTION;
        SELECT @ExceptionId AS ExceptionId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.usp_UpdatePolicyException', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_UpdatePolicyException;
GO

CREATE PROCEDURE dbo.usp_UpdatePolicyException
(
      @ExceptionId      INT
    , @EmployeeCode     VARCHAR(50)
    , @PolicyJson       NVARCHAR(MAX)
    , @Priority         INT
    , @EffectiveFrom    DATE
    , @EffectiveTo      DATE = NULL
    , @ModifiedBy       VARCHAR(50)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.PolicyException WHERE ExceptionId = @ExceptionId)
            THROW 50020, 'Policy Exception not found.', 1;
        IF NULLIF(LTRIM(RTRIM(@EmployeeCode)), '') IS NULL
            THROW 50021, 'Employee Code is required.', 1;
        IF NULLIF(LTRIM(RTRIM(@PolicyJson)), '') IS NULL
            THROW 50022, 'PolicyJson is required.', 1;
        IF @Priority <= 0
            THROW 50023, 'Priority must be greater than zero.', 1;
        IF @EffectiveFrom IS NULL
            THROW 50024, 'Effective From is required.', 1;
        IF @EffectiveTo IS NOT NULL AND @EffectiveTo < @EffectiveFrom
            THROW 50025, 'Effective To cannot be earlier than Effective From.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.MST_UserMaster WHERE EmployeeCode = @EmployeeCode)
            THROW 50026, 'Employee does not exist.', 1;
        IF EXISTS (SELECT 1 FROM dbo.MST_UserMaster WHERE EmployeeCode = @EmployeeCode AND ISNULL(EmployeeStatus, '') <> 'Active')
            THROW 50027, 'Employee is not active.', 1;
        IF EXISTS (
            SELECT 1 FROM dbo.PolicyException PE
            WHERE PE.EmployeeCode = @EmployeeCode AND PE.ExceptionId <> @ExceptionId AND PE.IsActive = 1
              AND (@EffectiveFrom <= ISNULL(PE.EffectiveTo, '99991231') AND ISNULL(@EffectiveTo, '99991231') >= PE.EffectiveFrom)
        )
            THROW 50028, 'Another active Policy Exception already exists for the selected Effective Period.', 1;

        UPDATE PE SET
              RegionId = UM.RegionId
            , BranchId = UM.BranchId
            , GradeId = UM.GradeId
            , DesignationId = UM.Designation_ID
            , PolicyJson = @PolicyJson
            , Priority = @Priority
            , EffectiveFrom = @EffectiveFrom
            , EffectiveTo = @EffectiveTo
            , ModifiedBy = @ModifiedBy
            , ModifiedOn = SYSDATETIME()
        FROM dbo.PolicyException PE
        INNER JOIN dbo.MST_UserMaster UM ON UM.EmployeeCode = @EmployeeCode
        WHERE PE.ExceptionId = @ExceptionId;

        COMMIT TRANSACTION;
        SELECT @ExceptionId AS ExceptionId, 'Policy Exception updated successfully.' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('dbo.usp_SetPolicyExceptionActiveStatus', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetPolicyExceptionActiveStatus;
GO

CREATE PROCEDURE dbo.usp_SetPolicyExceptionActiveStatus
(
      @ExceptionId INT
    , @IsActive BIT
    , @ModifiedBy VARCHAR(50)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.PolicyException WHERE ExceptionId = @ExceptionId)
            THROW 50030, 'Policy Exception not found.', 1;

        IF EXISTS (SELECT 1 FROM dbo.PolicyException WHERE ExceptionId = @ExceptionId AND IsActive = @IsActive)
            THROW 50031, 'Policy Exception is already in the requested status.', 1;

        UPDATE dbo.PolicyException
        SET IsActive = @IsActive, ModifiedBy = @ModifiedBy, ModifiedOn = SYSDATETIME()
        WHERE ExceptionId = @ExceptionId;

        COMMIT TRANSACTION;
        SELECT @ExceptionId AS ExceptionId, @IsActive AS IsActive,
            CASE WHEN @IsActive = 1 THEN 'Policy Exception activated successfully.' ELSE 'Policy Exception deactivated successfully.' END AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
