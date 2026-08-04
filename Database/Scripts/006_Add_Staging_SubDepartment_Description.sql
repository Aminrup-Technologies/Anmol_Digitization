/*
    Object:      Staging_EmployeeData.SubDepartment_Description
    Type:        SCHEMA MIGRATION
    Phase:       HRMS Import — SubDepartment schema alignment
    Purpose:     Add SubDepartment_Description so staging can carry the
                 companion description used by usp_HSE_SynchronizeMasters
                 (and aligned validation) exactly like other org masters.

    Backward compatible:
      - Column is NULLABLE
      - Existing rows remain valid (NULL description)
      - Idempotent (safe to re-run)
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Staging_EmployeeData', N'U') IS NULL
BEGIN
    RAISERROR(N'Migration aborted: dbo.Staging_EmployeeData does not exist.', 16, 1);
    RETURN;
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
END
GO
