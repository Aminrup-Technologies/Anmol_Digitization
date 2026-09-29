/*
    Policy Exception – database integrity hardening.

    Deploy AFTER 007/008 history scripts.

    Changes:
      - ISJSON validation on Insert/Update
      - Activation expiry + overlap checks (no history on failure)
      - Employee-scoped sp_getapplock + UPDLOCK for concurrency
      - Align CreatedOn/ModifiedOn to IST (match history ChangedOn)
      - CHECK constraints on Priority / Effective dates
      - Supporting index for employee active/date lookups
      - Immutable history trigger (block UPDATE/DELETE)

    Does NOT change Search/GetById signatures or policy resolution.
*/

SET NOCOUNT ON;
GO

/* --------------------------------------------------------------------------
   DDL – constraints, index, history immutability
   -------------------------------------------------------------------------- */

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PolicyException_Priority' AND parent_object_id = OBJECT_ID(N'dbo.PolicyException')
)
BEGIN
    ALTER TABLE dbo.PolicyException WITH CHECK
    ADD CONSTRAINT CK_PolicyException_Priority CHECK (Priority > 0);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PolicyException_EffectiveDates' AND parent_object_id = OBJECT_ID(N'dbo.PolicyException')
)
BEGIN
    ALTER TABLE dbo.PolicyException WITH CHECK
    ADD CONSTRAINT CK_PolicyException_EffectiveDates
    CHECK (EffectiveTo IS NULL OR EffectiveFrom IS NULL OR EffectiveTo >= EffectiveFrom);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PolicyException_Employee_Active_Dates'
      AND object_id = OBJECT_ID(N'dbo.PolicyException')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PolicyException_Employee_Active_Dates
        ON dbo.PolicyException (EmployeeCode ASC, IsActive ASC, EffectiveFrom ASC, EffectiveTo ASC)
        INCLUDE (Priority, ExceptionId);
END
GO

IF OBJECT_ID(N'dbo.TR_PolicyExceptionHistory_PreventMutation', N'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_PolicyExceptionHistory_PreventMutation;
GO

CREATE TRIGGER dbo.TR_PolicyExceptionHistory_PreventMutation
ON dbo.PolicyExceptionHistory
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 50050, 'PolicyExceptionHistory is immutable and cannot be updated or deleted.', 1;
END
GO

/* --------------------------------------------------------------------------
   Write procedures
   -------------------------------------------------------------------------- */

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
        IF ISJSON(@PolicyJson) <> 1
            THROW 50009, 'PolicyJson must be valid JSON.', 1;
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

        /* Employee-scoped exclusive lock: prevents empty-set overlap races. */
        DECLARE @LockResult INT;
        DECLARE @LockResource NVARCHAR(200) = N'PolicyException:' + LTRIM(RTRIM(@EmployeeCode));
        EXEC @LockResult = sys.sp_getapplock
              @Resource = @LockResource
            , @LockMode = N'Exclusive'
            , @LockOwner = N'Transaction'
            , @LockTimeout = 5000;
        IF @LockResult < 0
            THROW 50010, 'Could not acquire Policy Exception lock. Please retry.', 1;

        IF EXISTS (
            SELECT 1
            FROM dbo.PolicyException PE WITH (UPDLOCK, HOLDLOCK)
            WHERE PE.EmployeeCode = @EmployeeCode
              AND PE.IsActive = 1
              AND (@EffectiveFrom <= ISNULL(PE.EffectiveTo,'99991231')
                   AND ISNULL(@EffectiveTo,'99991231') >= PE.EffectiveFrom)
        )
            THROW 50008, 'An active Policy Exception already exists for the selected Effective Period.', 1;

        DECLARE @NowIst DATETIME2 =
            CONVERT(DATETIME2, (SYSUTCDATETIME() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time');

        INSERT INTO dbo.PolicyException
        (EmployeeCode, RegionId, BranchId, GradeId, DesignationId, PolicyJson, Priority, EffectiveFrom, EffectiveTo, IsActive, CreatedBy, CreatedOn)
        SELECT UM.EmployeeCode, UM.RegionId, UM.BranchId, UM.GradeId, UM.Designation_ID, @PolicyJson, @Priority, @EffectiveFrom, @EffectiveTo, 1, @CreatedBy, @NowIst
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

        IF NULLIF(LTRIM(RTRIM(@EmployeeCode)), '') IS NULL
            THROW 50021, 'Employee Code is required.', 1;
        IF NULLIF(LTRIM(RTRIM(@PolicyJson)), '') IS NULL
            THROW 50022, 'PolicyJson is required.', 1;
        IF ISJSON(@PolicyJson) <> 1
            THROW 50029, 'PolicyJson must be valid JSON.', 1;
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

        /* Lock target row first. */
        IF NOT EXISTS (
            SELECT 1
            FROM dbo.PolicyException WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
            WHERE ExceptionId = @ExceptionId
        )
            THROW 50020, 'Policy Exception not found.', 1;

        DECLARE @LockResult INT;
        DECLARE @LockResource NVARCHAR(200) = N'PolicyException:' + LTRIM(RTRIM(@EmployeeCode));
        EXEC @LockResult = sys.sp_getapplock
              @Resource = @LockResource
            , @LockMode = N'Exclusive'
            , @LockOwner = N'Transaction'
            , @LockTimeout = 5000;
        IF @LockResult < 0
            THROW 50010, 'Could not acquire Policy Exception lock. Please retry.', 1;

        IF EXISTS (
            SELECT 1
            FROM dbo.PolicyException PE WITH (UPDLOCK, HOLDLOCK)
            WHERE PE.EmployeeCode = @EmployeeCode
              AND PE.ExceptionId <> @ExceptionId
              AND PE.IsActive = 1
              AND (@EffectiveFrom <= ISNULL(PE.EffectiveTo, '99991231')
                   AND ISNULL(@EffectiveTo, '99991231') >= PE.EffectiveFrom)
        )
            THROW 50028, 'Another active Policy Exception already exists for the selected Effective Period.', 1;

        DECLARE @NowIst DATETIME2 =
            CONVERT(DATETIME2, (SYSUTCDATETIME() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time');

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
            , ModifiedOn = @NowIst
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

        DECLARE
              @EmployeeCode NVARCHAR(50)
            , @EffectiveFrom DATE
            , @EffectiveTo DATE
            , @CurrentIsActive BIT;

        SELECT
              @EmployeeCode = PE.EmployeeCode
            , @EffectiveFrom = PE.EffectiveFrom
            , @EffectiveTo = PE.EffectiveTo
            , @CurrentIsActive = PE.IsActive
        FROM dbo.PolicyException PE WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
        WHERE PE.ExceptionId = @ExceptionId;

        IF @EmployeeCode IS NULL
            THROW 50030, 'Policy Exception not found.', 1;

        IF @CurrentIsActive = @IsActive
            THROW 50031, 'Policy Exception is already in the requested status.', 1;

        IF @IsActive = 1
        BEGIN
            DECLARE @TodayIst DATE =
                CAST(CONVERT(DATETIME2, (SYSUTCDATETIME() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time') AS DATE);

            IF @EffectiveTo IS NOT NULL AND @EffectiveTo < @TodayIst
                THROW 50032, 'Expired Policy Exceptions cannot be activated.', 1;

            DECLARE @LockResult INT;
            DECLARE @LockResource NVARCHAR(200) = N'PolicyException:' + LTRIM(RTRIM(@EmployeeCode));
            EXEC @LockResult = sys.sp_getapplock
                  @Resource = @LockResource
                , @LockMode = N'Exclusive'
                , @LockOwner = N'Transaction'
                , @LockTimeout = 5000;
            IF @LockResult < 0
                THROW 50010, 'Could not acquire Policy Exception lock. Please retry.', 1;

            IF EXISTS (
                SELECT 1
                FROM dbo.PolicyException PE WITH (UPDLOCK, HOLDLOCK)
                WHERE PE.EmployeeCode = @EmployeeCode
                  AND PE.ExceptionId <> @ExceptionId
                  AND PE.IsActive = 1
                  AND (@EffectiveFrom <= ISNULL(PE.EffectiveTo, '99991231')
                       AND ISNULL(@EffectiveTo, '99991231') >= PE.EffectiveFrom)
            )
                THROW 50033, 'An active Policy Exception already exists for the selected Effective Period.', 1;
        END

        DECLARE @NowIst DATETIME2 =
            CONVERT(DATETIME2, (SYSUTCDATETIME() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time');

        UPDATE dbo.PolicyException
        SET IsActive = @IsActive, ModifiedBy = @ModifiedBy, ModifiedOn = @NowIst
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
