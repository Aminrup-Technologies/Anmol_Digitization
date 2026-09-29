/*
    Package:     Database/Deployments/HRMS_PhaseA_B1
    Script:      003_Staging_EmployeeData_SubDepartment_Description.sql
    Baseline:    Aug26_AILWorks_AmimnrupSupport @ 5ff3a3f
    Source:      Database/Scripts/006_Add_Staging_SubDepartment_Description.sql
    Purpose:     Idempotent add of Staging_EmployeeData.SubDepartment_Description.

    Required because:
      Live is missing this column.
      usp_HSE_SynchronizeMasters and usp_HSE_ValidateBatch read it.
      This column was not in PR #203 (already present on UAT) but is a
      LIVE BEHIND prerequisite for the approved workflow.

    Does not modify employee or staging row data.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ExpectedDatabase sysname = N'AnmolDristi';
DECLARE @AllowOtherDatabase bit = 0;
IF DB_NAME() <> @ExpectedDatabase AND @AllowOtherDatabase = 0
BEGIN
    DECLARE @msg nvarchar(4000) =
        N'Abort: connected to ' + QUOTENAME(DB_NAME())
        + N'. Expected ' + QUOTENAME(@ExpectedDatabase)
        + N'. Set @AllowOtherDatabase = 1 to override.';
    THROW 56101, @msg, 1;
END;

IF OBJECT_ID(N'dbo.Staging_EmployeeData', N'U') IS NULL
BEGIN
    THROW 56111, N'Migration aborted: dbo.Staging_EmployeeData does not exist.', 1;
END;

IF COL_LENGTH(N'dbo.Staging_EmployeeData', N'SubDepartment_Description') IS NULL
BEGIN
    ALTER TABLE dbo.Staging_EmployeeData
    ADD SubDepartment_Description VARCHAR(255) NULL;

    PRINT N'Added dbo.Staging_EmployeeData.SubDepartment_Description VARCHAR(255) NULL.';
END
ELSE
BEGIN
    PRINT N'dbo.Staging_EmployeeData.SubDepartment_Description already exists — no change.';
END;
GO
