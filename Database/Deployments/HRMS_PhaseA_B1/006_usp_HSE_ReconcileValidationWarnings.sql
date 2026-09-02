/*
    Package:     Database/Deployments/HRMS_PhaseA_B1
    Baseline:    Aug26_AILWorks_AmimnrupSupport @ 5ff3a3f (PR #203)
    Source:      Database/Stored Procedures/HRMS/usp_HSE_ReconcileValidationWarnings.sql
    Target:      AnmolDristi (Live) — REVIEW ONLY, do not apply from this package without approval
    ANSI_NULLS:  ON
    QUOTED_IDENT: ON
    Notes:       Body is the approved repository definition (CREATE OR ALTER).
                 SQL Server catalogs CREATE OR ALTER as CREATE   PROCEDURE; that is expected.
*/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
DECLARE @ExpectedDatabase sysname = N'AnmolDristi';
DECLARE @AllowOtherDatabase bit = 0; /* set 1 only for an intentional non-Live dry run */
IF DB_NAME() <> @ExpectedDatabase AND @AllowOtherDatabase = 0
BEGIN
    DECLARE @msg nvarchar(4000) =
        N'Abort: connected to ' + QUOTENAME(DB_NAME())
        + N'. Expected ' + QUOTENAME(@ExpectedDatabase)
        + N'. Set @AllowOtherDatabase = 1 to override.';
    THROW 56101, @msg, 1;
END;
GO

/*
    Object:      [dbo].[usp_HSE_ReconcileValidationWarnings]
    Type:        STORED PROCEDURE
    Phase:       5.1 — Validation Warning Reconciliation
    Purpose:     After SynchronizeMasters, remove obsolete ValidateBatch
                 warnings of the form:
                   [WARNING] … not found in MST_*; expected to be created by SynchronizeMasters.
                 when the corresponding master row now exists.

    Scope:       Staging_EmployeeData.ErrorMessage for @BatchId only.
                 Does NOT re-run validation.
                 Does NOT change ImportStatus.
                 Does NOT remove hard Errors or unrelated warnings
                 (e.g. hierarchy / update soft-warnings).

    Eligible masters (ValidateBatch createability warnings):
      Company, Region, Division, Category, Plant, Branch,
      Department, SubDepartment, Grade, Designation

    Orchestrator placement:
      usp_HSE_SynchronizeMasters
           ↓
      usp_HSE_ReconcileValidationWarnings   ← this procedure
           ↓
      usp_HSE_InsertNewEmployees
*/

CREATE OR ALTER PROCEDURE [dbo].[usp_HSE_ReconcileValidationWarnings]
    @BatchId          INT,
    @ReconciledCount  INT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @ReconciledCount = 0;

    IF @BatchId IS NULL OR @BatchId <= 0
        THROW 51001, N'usp_HSE_ReconcileValidationWarnings: @BatchId is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.ImportBatchMaster WHERE BatchId = @BatchId)
        THROW 51002, N'usp_HSE_ReconcileValidationWarnings: BatchId was not found in ImportBatchMaster.', 1;

    /* Working set: rows that still carry SynchronizeMasters createability warnings */
    IF OBJECT_ID(N'tempdb..#HseReconcileWarnings') IS NOT NULL
        DROP TABLE #HseReconcileWarnings;

    SELECT
        s.StagingId,
        Msg = s.ErrorMessage,
        BranchCode         = NULLIF(LTRIM(RTRIM(s.Branch_ID)), N''),
        DepartmentCode     = NULLIF(LTRIM(RTRIM(s.Department_ID)), N''),
        DesignationCode    = NULLIF(LTRIM(RTRIM(s.Designation_ID)), N''),
        GradeCode          = NULLIF(LTRIM(RTRIM(s.Grade_ID)), N''),
        PlantCode          = NULLIF(LTRIM(RTRIM(s.PlantID_ID)), N''),
        RegionCode         = NULLIF(LTRIM(RTRIM(s.Region_ID)), N''),
        DivisionCode       = NULLIF(LTRIM(RTRIM(s.Division_ID)), N''),
        CategoryCode       = NULLIF(LTRIM(RTRIM(s.Category_ID)), N''),
        CompanyCode        = NULLIF(LTRIM(RTRIM(s.Company_ID)), N''),
        SubDepartmentCode  = NULLIF(LTRIM(RTRIM(s.SubDepartment_ID)), N'')
    INTO #HseReconcileWarnings
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId
      AND s.ErrorMessage LIKE N'%expected to be created by SynchronizeMasters%';

    IF NOT EXISTS (SELECT 1 FROM #HseReconcileWarnings)
        RETURN;

    /*
       For each eligible master: if the business code now resolves in the
       master table, strip ONLY the matching ValidateBatch warning fragment.
       Existence predicates mirror usp_HSE_ValidateBatch / SynchronizeMasters.
    */

    /* Branch */
    UPDATE w
    SET w.Msg = REPLACE(
            w.Msg,
            N'[WARNING] Branch_ID ''' + w.BranchCode
                + N''' not found in MST_Branch; expected to be created by SynchronizeMasters.',
            N''
        )
    FROM #HseReconcileWarnings AS w
    WHERE w.BranchCode IS NOT NULL
      AND EXISTS (
            SELECT 1
            FROM dbo.MST_Branch AS m WITH (NOLOCK)
            WHERE m.Branch_ID = TRY_CONVERT(INT, w.BranchCode)
          );

    /* Department */
    UPDATE w
    SET w.Msg = REPLACE(
            w.Msg,
            N'[WARNING] Department_ID ''' + w.DepartmentCode
                + N''' not found in MST_Department; expected to be created by SynchronizeMasters.',
            N''
        )
    FROM #HseReconcileWarnings AS w
    WHERE w.DepartmentCode IS NOT NULL
      AND EXISTS (
            SELECT 1
            FROM dbo.MST_Department AS m WITH (NOLOCK)
            WHERE m.Department_ID = w.DepartmentCode
          );

    /* Designation (wording includes .Designation_ID) */
    UPDATE w
    SET w.Msg = REPLACE(
            w.Msg,
            N'[WARNING] Designation_ID ''' + w.DesignationCode
                + N''' not found in MST_Designation.Designation_ID; expected to be created by SynchronizeMasters.',
            N''
        )
    FROM #HseReconcileWarnings AS w
    WHERE w.DesignationCode IS NOT NULL
      AND EXISTS (
            SELECT 1
            FROM dbo.MST_Designation AS m WITH (NOLOCK)
            WHERE m.Designation_ID = w.DesignationCode
          );

    /* Grade */
    UPDATE w
    SET w.Msg = REPLACE(
            w.Msg,
            N'[WARNING] Grade_ID ''' + w.GradeCode
                + N''' not found in MST_Grade; expected to be created by SynchronizeMasters.',
            N''
        )
    FROM #HseReconcileWarnings AS w
    WHERE w.GradeCode IS NOT NULL
      AND EXISTS (
            SELECT 1
            FROM dbo.MST_Grade AS m WITH (NOLOCK)
            WHERE m.Grade_ID = w.GradeCode
          );

    /* Plant */
    UPDATE w
    SET w.Msg = REPLACE(
            w.Msg,
            N'[WARNING] PlantID_ID ''' + w.PlantCode
                + N''' not found in MST_Plant; expected to be created by SynchronizeMasters.',
            N''
        )
    FROM #HseReconcileWarnings AS w
    WHERE w.PlantCode IS NOT NULL
      AND EXISTS (
            SELECT 1
            FROM dbo.MST_Plant AS m WITH (NOLOCK)
            WHERE CAST(m.PlantID_ID AS NVARCHAR(50)) = w.PlantCode
          );

    /* Region */
    UPDATE w
    SET w.Msg = REPLACE(
            w.Msg,
            N'[WARNING] Region_ID ''' + w.RegionCode
                + N''' not found in MST_Region; expected to be created by SynchronizeMasters.',
            N''
        )
    FROM #HseReconcileWarnings AS w
    WHERE w.RegionCode IS NOT NULL
      AND EXISTS (
            SELECT 1
            FROM dbo.MST_Region AS m WITH (NOLOCK)
            WHERE m.Region_ID = w.RegionCode
          );

    /* Division */
    UPDATE w
    SET w.Msg = REPLACE(
            w.Msg,
            N'[WARNING] Division_ID ''' + w.DivisionCode
                + N''' not found in MST_Division; expected to be created by SynchronizeMasters.',
            N''
        )
    FROM #HseReconcileWarnings AS w
    WHERE w.DivisionCode IS NOT NULL
      AND EXISTS (
            SELECT 1
            FROM dbo.MST_Division AS m WITH (NOLOCK)
            WHERE CAST(m.Division_ID AS NVARCHAR(50)) = w.DivisionCode
          );

    /* Category */
    UPDATE w
    SET w.Msg = REPLACE(
            w.Msg,
            N'[WARNING] Category_ID ''' + w.CategoryCode
                + N''' not found in MST_Category; expected to be created by SynchronizeMasters.',
            N''
        )
    FROM #HseReconcileWarnings AS w
    WHERE w.CategoryCode IS NOT NULL
      AND EXISTS (
            SELECT 1
            FROM dbo.MST_Category AS m WITH (NOLOCK)
            WHERE CAST(m.Category_ID AS NVARCHAR(50)) = w.CategoryCode
          );

    /* Company */
    UPDATE w
    SET w.Msg = REPLACE(
            w.Msg,
            N'[WARNING] Company_ID ''' + w.CompanyCode
                + N''' not found in MST_Company; expected to be created by SynchronizeMasters.',
            N''
        )
    FROM #HseReconcileWarnings AS w
    WHERE w.CompanyCode IS NOT NULL
      AND EXISTS (
            SELECT 1
            FROM dbo.MST_Company AS m WITH (NOLOCK)
            WHERE CAST(m.Company_ID AS NVARCHAR(50)) = w.CompanyCode
          );

    /* SubDepartment */
    UPDATE w
    SET w.Msg = REPLACE(
            w.Msg,
            N'[WARNING] SubDepartment_ID ''' + w.SubDepartmentCode
                + N''' not found in MST_SubDepartment; expected to be created by SynchronizeMasters.',
            N''
        )
    FROM #HseReconcileWarnings AS w
    WHERE w.SubDepartmentCode IS NOT NULL
      AND EXISTS (
            SELECT 1
            FROM dbo.MST_SubDepartment AS m WITH (NOLOCK)
            WHERE CAST(m.SubDepartment_ID AS NVARCHAR(50)) = w.SubDepartmentCode
          );

    /* Tidy separators left by fragment removal; empty → NULL */
    UPDATE w
    SET w.Msg =
        NULLIF(
            LTRIM(RTRIM(
                CASE
                    WHEN LEFT(v.Clean1, 2) = N'; ' THEN SUBSTRING(v.Clean1, 3, 8000)
                    WHEN LEFT(v.Clean1, 1) = N';' THEN SUBSTRING(v.Clean1, 2, 8000)
                    ELSE v.Clean1
                END
            )),
            N''
        )
    FROM #HseReconcileWarnings AS w
    CROSS APPLY (
        SELECT Clean0 =
            LTRIM(RTRIM(
                REPLACE(
                    REPLACE(
                        REPLACE(ISNULL(w.Msg, N''), N'; ; ', N'; '),
                        N';  ', N'; '
                    ),
                    N'  ', N' '
                )
            ))
    ) AS x
    CROSS APPLY (
        SELECT Clean1 =
            CASE
                WHEN RIGHT(x.Clean0, 2) = N'; ' THEN LEFT(x.Clean0, LEN(x.Clean0) - 2)
                WHEN RIGHT(x.Clean0, 1) = N';' THEN LEFT(x.Clean0, LEN(x.Clean0) - 1)
                ELSE x.Clean0
            END
    ) AS v;

    /* Persist only rows whose message actually changed */
    UPDATE s
    SET s.ErrorMessage = w.Msg
    FROM dbo.Staging_EmployeeData AS s
    INNER JOIN #HseReconcileWarnings AS w
        ON w.StagingId = s.StagingId
    WHERE ISNULL(s.ErrorMessage, N'') <> ISNULL(w.Msg, N'');

    SET @ReconciledCount = @@ROWCOUNT;
END
GO
