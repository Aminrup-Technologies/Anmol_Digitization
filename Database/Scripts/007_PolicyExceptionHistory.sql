/*
    Policy Exception History – immutable audit trail.

    Deploy to AnmolDristiDev / target database.
    Additive only: creates PolicyExceptionHistory and history helpers,
    then recreates write/read procedures used by ManagePolicyException.

    Rollback:
      DROP PROCEDURE dbo.usp_GetPolicyExceptionHistoryVersion;
      DROP PROCEDURE dbo.usp_GetPolicyExceptionHistory;
      DROP PROCEDURE dbo.usp_InsertPolicyExceptionHistorySnapshot;
      -- Redeploy prior usp_Insert/Update/SetActiveStatus from 004 without history calls.
      -- DROP TABLE dbo.PolicyExceptionHistory;  -- only if discarding audit data is acceptable
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.PolicyExceptionHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PolicyExceptionHistory
    (
          HistoryId      INT IDENTITY(1,1) NOT NULL
        , ExceptionId    INT NOT NULL
        , VersionNo      INT NOT NULL
        , ActionType     NVARCHAR(20) NOT NULL
        , PolicyJson     NVARCHAR(MAX) NULL
        , Priority       INT NOT NULL
        , EffectiveFrom  DATE NULL
        , EffectiveTo    DATE NULL
        , IsActive       BIT NOT NULL
        , ChangedBy      NVARCHAR(100) NULL
        , ChangedOn      DATETIME2(7) NOT NULL
            CONSTRAINT DF_PolicyExceptionHistory_ChangedOn
            DEFAULT (CONVERT(DATETIME2, (SYSUTCDATETIME() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time'))
        , ChangeReason   NVARCHAR(500) NULL
        , CONSTRAINT PK_PolicyExceptionHistory PRIMARY KEY CLUSTERED (HistoryId ASC)
        , CONSTRAINT UQ_PolicyExceptionHistory_Exception_Version UNIQUE (ExceptionId, VersionNo)
        , CONSTRAINT FK_PolicyExceptionHistory_PolicyException
            FOREIGN KEY (ExceptionId) REFERENCES dbo.PolicyException (ExceptionId)
        , CONSTRAINT CK_PolicyExceptionHistory_ActionType
            CHECK (ActionType IN (N'Create', N'Update', N'Activate', N'Deactivate'))
        , CONSTRAINT CK_PolicyExceptionHistory_VersionNo
            CHECK (VersionNo >= 1)
    );

    CREATE NONCLUSTERED INDEX IX_PolicyExceptionHistory_ExceptionId_VersionNo
        ON dbo.PolicyExceptionHistory (ExceptionId ASC, VersionNo DESC);
END
GO

IF OBJECT_ID('dbo.usp_InsertPolicyExceptionHistorySnapshot', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_InsertPolicyExceptionHistorySnapshot;
GO

CREATE PROCEDURE dbo.usp_InsertPolicyExceptionHistorySnapshot
(
      @ExceptionId   INT
    , @ActionType    NVARCHAR(20)
    , @ChangedBy     NVARCHAR(100)
    , @ChangeReason  NVARCHAR(500) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @ExceptionId IS NULL OR @ExceptionId <= 0
        THROW 50040, 'ExceptionId is required for history.', 1;
    IF NULLIF(LTRIM(RTRIM(@ActionType)), '') IS NULL
        THROW 50041, 'ActionType is required for history.', 1;
    IF @ActionType NOT IN (N'Create', N'Update', N'Activate', N'Deactivate')
        THROW 50042, 'Invalid ActionType for history.', 1;

    DECLARE @VersionNo INT;

    SELECT @VersionNo = ISNULL(MAX(VersionNo), 0) + 1
    FROM dbo.PolicyExceptionHistory WITH (UPDLOCK, HOLDLOCK)
    WHERE ExceptionId = @ExceptionId;

    INSERT INTO dbo.PolicyExceptionHistory
    (
          ExceptionId
        , VersionNo
        , ActionType
        , PolicyJson
        , Priority
        , EffectiveFrom
        , EffectiveTo
        , IsActive
        , ChangedBy
        , ChangedOn
        , ChangeReason
    )
    SELECT
          PE.ExceptionId
        , @VersionNo
        , @ActionType
        , PE.PolicyJson
        , PE.Priority
        , PE.EffectiveFrom
        , PE.EffectiveTo
        , PE.IsActive
        , @ChangedBy
        , CONVERT(DATETIME2, (SYSUTCDATETIME() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')
        , NULLIF(LTRIM(RTRIM(@ChangeReason)), '')
    FROM dbo.PolicyException PE
    WHERE PE.ExceptionId = @ExceptionId;

    IF @@ROWCOUNT = 0
        THROW 50043, 'Policy Exception not found for history snapshot.', 1;
END
GO

IF OBJECT_ID('dbo.usp_GetPolicyExceptionHistory', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetPolicyExceptionHistory;
GO

CREATE PROCEDURE dbo.usp_GetPolicyExceptionHistory
(
    @ExceptionId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
          H.HistoryId
        , H.ExceptionId
        , H.VersionNo
        , H.ActionType
        , H.ChangedBy
        , H.ChangedOn
        , H.ChangeReason
        , H.IsActive
    FROM dbo.PolicyExceptionHistory H
    WHERE H.ExceptionId = @ExceptionId
    ORDER BY H.VersionNo DESC;
END
GO

IF OBJECT_ID('dbo.usp_GetPolicyExceptionHistoryVersion', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetPolicyExceptionHistoryVersion;
GO

CREATE PROCEDURE dbo.usp_GetPolicyExceptionHistoryVersion
(
      @ExceptionId INT
    , @VersionNo   INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
          H.HistoryId
        , H.ExceptionId
        , H.VersionNo
        , H.ActionType
        , H.PolicyJson
        , H.Priority
        , H.EffectiveFrom
        , H.EffectiveTo
        , H.IsActive
        , H.ChangedBy
        , H.ChangedOn
        , H.ChangeReason
    FROM dbo.PolicyExceptionHistory H
    WHERE H.ExceptionId = @ExceptionId
      AND H.VersionNo = @VersionNo;
END
GO

/* Recreate write procedures with same-transaction history snapshots. */

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
    , @ChangeReason      NVARCHAR(500) = NULL
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

        EXEC dbo.usp_InsertPolicyExceptionHistorySnapshot
              @ExceptionId = @ExceptionId
            , @ActionType = N'Create'
            , @ChangedBy = @CreatedBy
            , @ChangeReason = @ChangeReason;

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
    , @ChangeReason     NVARCHAR(500) = NULL
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

        EXEC dbo.usp_InsertPolicyExceptionHistorySnapshot
              @ExceptionId = @ExceptionId
            , @ActionType = N'Update'
            , @ChangedBy = @ModifiedBy
            , @ChangeReason = @ChangeReason;

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
    , @ChangeReason NVARCHAR(500) = NULL
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

        DECLARE @ActionType NVARCHAR(20) = CASE WHEN @IsActive = 1 THEN N'Activate' ELSE N'Deactivate' END;

        EXEC dbo.usp_InsertPolicyExceptionHistorySnapshot
              @ExceptionId = @ExceptionId
            , @ActionType = @ActionType
            , @ChangedBy = @ModifiedBy
            , @ChangeReason = @ChangeReason;

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
