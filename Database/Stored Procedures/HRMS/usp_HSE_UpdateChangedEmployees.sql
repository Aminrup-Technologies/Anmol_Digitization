/*
    Object:      [dbo].[usp_HSE_UpdateChangedEmployees]
    Type:        STORED PROCEDURE
    Phase:       3.7 — HRMS Synchronization Engine
    Purpose:     Update EXISTING MST_UserMaster rows from a staged HRMS
                 batch when values actually changed. Does not insert.
                 Does not perform a hierarchy-only pass beyond writing
                 reporting columns when those fields are in scope.

    Source:      dbo.Staging_EmployeeData WHERE BatchId = @BatchId
                 (Never uses KKM_SO_TADA_Export_14Nov25$)

    Match key:   Staging.EmpCode = MST_UserMaster.EmployeeCode ONLY
                 (never Email / Mobile / Name)

    ProcessingMode:
      0 = Insert Only       → no updates
      1 = Overwrite All     → all supported fields eligible
      2 = Update Selected   → only @OverwriteFields (pipe-delimited FieldCodes)

    Orchestrator contract (usp_RunHRMSSynchronization):
      @BatchId, @AllowOverwrite, @ProcessingMode, @OverwriteFields, @ExecutedBy,
      @UpdatedCount OUTPUT, @SkippedCount OUTPUT, @ErrorCount OUTPUT

    Additional outputs:
      @EmployeesUpdated   (= @UpdatedCount)
      @NoChangeCount
      @MissingMasterCount (= @ErrorCount for orchestrator rollup)
*/

CREATE OR ALTER PROCEDURE [dbo].[usp_HSE_UpdateChangedEmployees]
    @BatchId             INT,
    @AllowOverwrite      BIT            = 1,
    @ProcessingMode      INT            = 1,
    @OverwriteFields     NVARCHAR(MAX)  = NULL,
    @ExecutedBy          NVARCHAR(128)  = NULL,
    @UpdatedCount        INT OUTPUT,
    @SkippedCount        INT OUTPUT,
    @ErrorCount          INT OUTPUT,
    @EmployeesUpdated    INT = NULL OUTPUT,
    @NoChangeCount       INT = NULL OUTPUT,
    @MissingMasterCount  INT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @UpdatedCount = 0;
    SET @SkippedCount = 0;
    SET @ErrorCount = 0;
    SET @EmployeesUpdated = 0;
    SET @NoChangeCount = 0;
    SET @MissingMasterCount = 0;

    DECLARE @StartedTran BIT = 0;
    DECLARE @ExistingMatchCount INT = 0;

    /* Field enable flags */
    DECLARE @ModeAll BIT = 0;
    DECLARE @Of NVARCHAR(MAX);

    DECLARE @UpdEmployeeName BIT = 0;
    DECLARE @UpdEmployeeStatus BIT = 0;
    DECLARE @UpdEmail BIT = 0;
    DECLARE @UpdMobile BIT = 0;
    DECLARE @UpdGender BIT = 0;
    DECLARE @UpdDOJ BIT = 0;
    DECLARE @UpdBranch BIT = 0;
    DECLARE @UpdDepartment BIT = 0;
    DECLARE @UpdDesignation BIT = 0;
    DECLARE @UpdGrade BIT = 0;
    DECLARE @UpdPlant BIT = 0;
    DECLARE @UpdRegion BIT = 0;
    DECLARE @UpdDivision BIT = 0;
    DECLARE @UpdCategory BIT = 0;
    DECLARE @UpdSubDepartment BIT = 0;
    DECLARE @UpdReportingManager BIT = 0;
    DECLARE @UpdReportingManagerCode BIT = 0;
    DECLARE @UpdReportingManagersManagerName BIT = 0;
    DECLARE @UpdReportingManagersManagerCode BIT = 0;

    IF @BatchId IS NULL OR @BatchId <= 0
    BEGIN
        RAISERROR(N'usp_HSE_UpdateChangedEmployees: @BatchId is required.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.ImportBatchMaster WHERE BatchId = @BatchId)
    BEGIN
        RAISERROR(N'usp_HSE_UpdateChangedEmployees: BatchId was not found in ImportBatchMaster.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Staging_EmployeeData WITH (NOLOCK)
        WHERE BatchId = @BatchId
    )
    BEGIN
        RAISERROR(N'usp_HSE_UpdateChangedEmployees: No staging records found for the specified BatchId.', 16, 1);
        RETURN;
    END;

    IF @ProcessingMode NOT IN (0, 1, 2)
    BEGIN
        RAISERROR(N'usp_HSE_UpdateChangedEmployees: @ProcessingMode must be 0, 1, or 2.', 16, 1);
        RETURN;
    END;

    /* ------------------------------------------------------------------
       Mode 0 / AllowOverwrite=0 → no updates
       ------------------------------------------------------------------ */
    IF @ProcessingMode = 0 OR ISNULL(@AllowOverwrite, 0) = 0
    BEGIN
        SELECT @SkippedCount = COUNT(DISTINCT LTRIM(RTRIM(s.EmpCode)))
        FROM dbo.Staging_EmployeeData AS s
        INNER JOIN dbo.MST_UserMaster AS u
            ON u.EmployeeCode = LTRIM(RTRIM(s.EmpCode))
        WHERE s.BatchId = @BatchId
          AND NULLIF(LTRIM(RTRIM(s.EmpCode)), N'') IS NOT NULL
          AND ISNULL(s.ImportStatus, N'Pending') <> N'Error';

        IF @@TRANCOUNT = 0
        BEGIN
            SELECT
                BatchId            = @BatchId,
                UpdatedCount       = @UpdatedCount,
                EmployeesUpdated   = @EmployeesUpdated,
                SkippedCount       = @SkippedCount,
                NoChangeCount      = @NoChangeCount,
                MissingMasterCount = @MissingMasterCount,
                ErrorCount         = @ErrorCount,
                ProcessingMode     = @ProcessingMode,
                Message            = N'Update skipped (Insert Only / AllowOverwrite disabled).';
        END;

        RETURN;
    END;

    /* ------------------------------------------------------------------
       Resolve which fields are in scope
       ------------------------------------------------------------------ */
    IF @ProcessingMode = 1
        SET @ModeAll = 1;

    SET @Of = N'|' + UPPER(REPLACE(REPLACE(ISNULL(@OverwriteFields, N''), N' ', N''), N',', N'|')) + N'|';

    SET @UpdEmployeeName = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|EMPLOYEENAME|', @Of) > 0 OR CHARINDEX(N'|EMPNAME|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdEmployeeStatus = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|EMPLOYEESTATUS|', @Of) > 0 OR CHARINDEX(N'|EMPSTATUS|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdEmail = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|EMAIL|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdMobile = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|MOBILE|', @Of) > 0 OR CHARINDEX(N'|MOBILENO|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdGender = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|GENDER|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdDOJ = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|DOJ|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdBranch = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|BRANCH|', @Of) > 0 OR CHARINDEX(N'|BRANCHID|', @Of) > 0 OR CHARINDEX(N'|BRANCH_ID|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdDepartment = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|DEPARTMENT|', @Of) > 0 OR CHARINDEX(N'|DEPARTMENTID|', @Of) > 0 OR CHARINDEX(N'|DEPARTMENT_ID|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdDesignation = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|DESIGNATION|', @Of) > 0 OR CHARINDEX(N'|DESIGNATIONID|', @Of) > 0 OR CHARINDEX(N'|DESIGNATION_ID|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdGrade = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|GRADE|', @Of) > 0 OR CHARINDEX(N'|GRADEID|', @Of) > 0 OR CHARINDEX(N'|GRADE_ID|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdPlant = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|PLANT|', @Of) > 0 OR CHARINDEX(N'|PLANTID|', @Of) > 0 OR CHARINDEX(N'|PLANTID_ID|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdRegion = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|REGION|', @Of) > 0 OR CHARINDEX(N'|REGIONID|', @Of) > 0 OR CHARINDEX(N'|REGION_ID|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdDivision = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|DIVISION|', @Of) > 0 OR CHARINDEX(N'|DIVISIONID|', @Of) > 0 OR CHARINDEX(N'|DIVISION_ID|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdCategory = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|CATEGORY|', @Of) > 0 OR CHARINDEX(N'|CATEGORYID|', @Of) > 0 OR CHARINDEX(N'|CATEGORY_ID|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdSubDepartment = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|SUBDEPARTMENT|', @Of) > 0 OR CHARINDEX(N'|SUBDEPARTMENTID|', @Of) > 0 OR CHARINDEX(N'|SUBDEPARTMENT_ID|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdReportingManager = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|REPORTINGMANAGER|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdReportingManagerCode = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|REPORTINGMANAGERCODE|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdReportingManagersManagerName = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|REPORTINGMANAGERSMANAGERNAME|', @Of) > 0 THEN 1 ELSE 0 END;
    SET @UpdReportingManagersManagerCode = CASE WHEN @ModeAll = 1 OR CHARINDEX(N'|REPORTINGMANAGERSMANAGERCODE|', @Of) > 0 THEN 1 ELSE 0 END;

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            BEGIN TRANSACTION;
            SET @StartedTran = 1;
        END;

        IF OBJECT_ID(N'tempdb..#HseUpdateCandidates') IS NOT NULL
            DROP TABLE #HseUpdateCandidates;

        /* ------------------------------------------------------------------
           Build candidates: existing employees only, EmpCode match
           ------------------------------------------------------------------ */
        ;WITH ranked AS (
            SELECT
                s.StagingId,
                EmpCode = LTRIM(RTRIM(s.EmpCode)),
                EmpName = NULLIF(LTRIM(RTRIM(s.EmpName)), N''),
                Email = NULLIF(LTRIM(RTRIM(s.Email)), N''),
                MobileNo = NULLIF(LTRIM(RTRIM(s.MobileNo)), N''),
                Gender = NULLIF(LTRIM(RTRIM(s.Gender)), N''),
                DOJRaw = NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(50), s.DOJ))), N''),
                EmployeeStatus = COALESCE(
                    NULLIF(LTRIM(RTRIM(s.EmployeeStatus)), N''),
                    NULLIF(LTRIM(RTRIM(s.EmpStatus)), N'')
                ),
                ReportingManager = NULLIF(LTRIM(RTRIM(s.ReportingManager)), N''),
                ReportingManagerCode = NULLIF(LTRIM(RTRIM(s.ReportingManagerCode)), N''),
                ReportingManagersManagerName = NULLIF(LTRIM(RTRIM(s.ReportingManagersManagerName)), N''),
                ReportingManagersManagerCode = NULLIF(LTRIM(RTRIM(s.ReportingManagersManagerCode)), N''),
                BranchCode = NULLIF(LTRIM(RTRIM(s.Branch_ID)), N''),
                DepartmentCode = NULLIF(LTRIM(RTRIM(s.Department_ID)), N''),
                DesignationCode = NULLIF(LTRIM(RTRIM(s.Designation_ID)), N''),
                GradeCode = NULLIF(LTRIM(RTRIM(s.Grade_ID)), N''),
                PlantCode = NULLIF(LTRIM(RTRIM(s.PlantID_ID)), N''),
                RegionCode = NULLIF(LTRIM(RTRIM(s.Region_ID)), N''),
                DivisionCode = NULLIF(LTRIM(RTRIM(s.Division_ID)), N''),
                CategoryCode = NULLIF(LTRIM(RTRIM(s.Category_ID)), N''),
                SubDepartmentCode = NULLIF(LTRIM(RTRIM(s.SubDepartment_ID)), N''),
                rn = ROW_NUMBER() OVER (
                    PARTITION BY LTRIM(RTRIM(s.EmpCode))
                    ORDER BY s.StagingId
                )
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.EmpCode)), N'') IS NOT NULL
              AND ISNULL(s.ImportStatus, N'Pending') <> N'Error'
        )
        SELECT
            r.StagingId,
            r.EmpCode,

            /* Proposed scalar values (NULL = leave existing column unchanged) */
            NewEmployeeName = CASE WHEN @UpdEmployeeName = 1 THEN LEFT(r.EmpName, 100) ELSE NULL END,
            NewEmployeeStatus = CASE WHEN @UpdEmployeeStatus = 1 THEN LEFT(r.EmployeeStatus, 50) ELSE NULL END,
            NewEmail = CASE WHEN @UpdEmail = 1 THEN LEFT(r.Email, 100) ELSE NULL END,
            NewMobile = CASE WHEN @UpdMobile = 1 THEN LEFT(r.MobileNo, 10) ELSE NULL END,
            NewGender = CASE WHEN @UpdGender = 1 THEN LEFT(r.Gender, 50) ELSE NULL END,
            NewDOJ = CASE
                WHEN @UpdDOJ = 1 THEN COALESCE(
                    TRY_CONVERT(DATE, r.DOJRaw, 106),
                    TRY_CONVERT(DATE, r.DOJRaw, 23),
                    TRY_CONVERT(DATE, r.DOJRaw, 103),
                    TRY_CONVERT(DATE, r.DOJRaw)
                )
                ELSE NULL
            END,
            NewReportingManager = CASE WHEN @UpdReportingManager = 1 THEN LEFT(r.ReportingManager, 100) ELSE NULL END,
            NewReportingManagerCode = CASE WHEN @UpdReportingManagerCode = 1 THEN LEFT(r.ReportingManagerCode, 50) ELSE NULL END,
            NewReportingManagersManagerName = CASE WHEN @UpdReportingManagersManagerName = 1 THEN LEFT(r.ReportingManagersManagerName, 100) ELSE NULL END,
            NewReportingManagersManagerCode = CASE WHEN @UpdReportingManagersManagerCode = 1 THEN LEFT(r.ReportingManagersManagerCode, 50) ELSE NULL END,

            /* Proposed FK Ids (NULL = leave existing; unresolved codes also NULL) */
            NewBranchId = CASE WHEN @UpdBranch = 1 AND r.BranchCode IS NOT NULL THEN br.Id ELSE NULL END,
            NewDepartmentId = CASE WHEN @UpdDepartment = 1 AND r.DepartmentCode IS NOT NULL THEN dep.Id ELSE NULL END,
            NewDesignationId = CASE WHEN @UpdDesignation = 1 AND r.DesignationCode IS NOT NULL THEN des.Id ELSE NULL END,
            NewGradeId = CASE WHEN @UpdGrade = 1 AND r.GradeCode IS NOT NULL THEN gr.Id ELSE NULL END,
            NewPlantId = CASE WHEN @UpdPlant = 1 AND r.PlantCode IS NOT NULL THEN pl.Id ELSE NULL END,
            NewRegionId = CASE WHEN @UpdRegion = 1 AND r.RegionCode IS NOT NULL THEN rg.Id ELSE NULL END,
            NewDivisionId = CASE WHEN @UpdDivision = 1 AND r.DivisionCode IS NOT NULL THEN dv.Id ELSE NULL END,
            NewCategoryId = CASE WHEN @UpdCategory = 1 AND r.CategoryCode IS NOT NULL THEN cat.Id ELSE NULL END,
            NewSubDepartmentId = CASE WHEN @UpdSubDepartment = 1 AND r.SubDepartmentCode IS NOT NULL THEN sd.Id ELSE NULL END,

            ExistsInMaster = CASE WHEN u.EmployeeCode IS NULL THEN 0 ELSE 1 END,

            MissingMaster = CASE
                WHEN (@UpdBranch = 1 AND r.BranchCode IS NOT NULL AND br.Id IS NULL)
                  OR (@UpdPlant = 1 AND r.PlantCode IS NOT NULL AND pl.Id IS NULL)
                  OR (@UpdDepartment = 1 AND r.DepartmentCode IS NOT NULL AND dep.Id IS NULL)
                  OR (@UpdDesignation = 1 AND r.DesignationCode IS NOT NULL AND des.Id IS NULL)
                  OR (@UpdGrade = 1 AND r.GradeCode IS NOT NULL AND gr.Id IS NULL)
                  OR (@UpdRegion = 1 AND r.RegionCode IS NOT NULL AND rg.Id IS NULL)
                  OR (@UpdDivision = 1 AND r.DivisionCode IS NOT NULL AND dv.Id IS NULL)
                  OR (@UpdCategory = 1 AND r.CategoryCode IS NOT NULL AND cat.Id IS NULL)
                  OR (@UpdSubDepartment = 1 AND r.SubDepartmentCode IS NOT NULL AND sd.Id IS NULL)
                THEN 1 ELSE 0
            END,
            MissingMasterDetail =
                NULLIF(
                    STUFF(
                        CONCAT(
                            CASE WHEN @UpdBranch = 1 AND r.BranchCode IS NOT NULL AND br.Id IS NULL
                                THEN N'; Branch_ID ''' + r.BranchCode + N'''' ELSE N'' END,
                            CASE WHEN @UpdPlant = 1 AND r.PlantCode IS NOT NULL AND pl.Id IS NULL
                                THEN N'; PlantID_ID ''' + r.PlantCode + N'''' ELSE N'' END,
                            CASE WHEN @UpdDepartment = 1 AND r.DepartmentCode IS NOT NULL AND dep.Id IS NULL
                                THEN N'; Department_ID ''' + r.DepartmentCode + N'''' ELSE N'' END,
                            CASE WHEN @UpdDesignation = 1 AND r.DesignationCode IS NOT NULL AND des.Id IS NULL
                                THEN N'; Designation_ID ''' + r.DesignationCode + N'''' ELSE N'' END,
                            CASE WHEN @UpdGrade = 1 AND r.GradeCode IS NOT NULL AND gr.Id IS NULL
                                THEN N'; Grade_ID ''' + r.GradeCode + N'''' ELSE N'' END,
                            CASE WHEN @UpdRegion = 1 AND r.RegionCode IS NOT NULL AND rg.Id IS NULL
                                THEN N'; Region_ID ''' + r.RegionCode + N'''' ELSE N'' END,
                            CASE WHEN @UpdDivision = 1 AND r.DivisionCode IS NOT NULL AND dv.Id IS NULL
                                THEN N'; Division_ID ''' + r.DivisionCode + N'''' ELSE N'' END,
                            CASE WHEN @UpdCategory = 1 AND r.CategoryCode IS NOT NULL AND cat.Id IS NULL
                                THEN N'; Category_ID ''' + r.CategoryCode + N'''' ELSE N'' END,
                            CASE WHEN @UpdSubDepartment = 1 AND r.SubDepartmentCode IS NOT NULL AND sd.Id IS NULL
                                THEN N'; SubDepartment_ID ''' + r.SubDepartmentCode + N'''' ELSE N'' END
                        ),
                        1, 2, N''
                    ),
                    N''
                )
        INTO #HseUpdateCandidates
        FROM ranked AS r
        LEFT JOIN dbo.MST_UserMaster AS u
            ON u.EmployeeCode = r.EmpCode
        LEFT JOIN dbo.MST_Branch AS br
            ON br.Branch_ID = TRY_CONVERT(INT, r.BranchCode)
        LEFT JOIN dbo.MST_Department AS dep
            ON dep.Department_ID = r.DepartmentCode
        LEFT JOIN dbo.MST_Designation AS des
            ON des.Designation_ID = r.DesignationCode
        LEFT JOIN dbo.MST_Grade AS gr
            ON gr.Grade_ID = r.GradeCode
        LEFT JOIN dbo.MST_Plant AS pl
            ON CAST(pl.PlantID_ID AS NVARCHAR(50)) = r.PlantCode
        LEFT JOIN dbo.MST_Region AS rg
            ON rg.Region_ID = r.RegionCode
        LEFT JOIN dbo.MST_Division AS dv
            ON CAST(dv.Division_ID AS NVARCHAR(50)) = r.DivisionCode
        LEFT JOIN dbo.MST_Category AS cat
            ON CAST(cat.Category_ID AS NVARCHAR(50)) = r.CategoryCode
        LEFT JOIN dbo.MST_SubDepartment AS sd
            ON CAST(sd.SubDepartment_ID AS NVARCHAR(50)) = r.SubDepartmentCode
        WHERE r.rn = 1;

        /* Skipped: staging EmpCode not found in MST_UserMaster */
        SELECT @SkippedCount = COUNT(1)
        FROM #HseUpdateCandidates
        WHERE ExistsInMaster = 0;

        /* Annotate missing-master warnings (do not block other field updates) */
        UPDATE s
        SET
            s.ErrorMessage =
                CASE
                    WHEN s.ErrorMessage IS NULL OR LTRIM(RTRIM(s.ErrorMessage)) = N'' THEN N''
                    ELSE s.ErrorMessage + N'; '
                END
                + N'[WARNING] Unresolved master reference(s) during update:'
                + ISNULL(N' ' + c.MissingMasterDetail, N'')
                + N'. Unresolved FK fields were left unchanged.'
        FROM dbo.Staging_EmployeeData AS s
        INNER JOIN #HseUpdateCandidates AS c
            ON c.StagingId = s.StagingId
        WHERE c.ExistsInMaster = 1
          AND c.MissingMaster = 1
          AND c.MissingMasterDetail IS NOT NULL;

        SELECT @MissingMasterCount = COUNT(1)
        FROM #HseUpdateCandidates
        WHERE ExistsInMaster = 1
          AND MissingMaster = 1;

        SET @ErrorCount = @MissingMasterCount;

        /* ------------------------------------------------------------------
           Apply change-only UPDATE for existing employees
           Never touch: EmployeeCode, CreatedDate, CreatedBy, CreationMode,
           Password*, DPDPConsent*
           ------------------------------------------------------------------ */
        UPDATE u
        SET
            u.EmployeeName = CASE
                WHEN c.NewEmployeeName IS NOT NULL
                 AND ISNULL(u.EmployeeName, N'') <> c.NewEmployeeName
                THEN c.NewEmployeeName ELSE u.EmployeeName END,
            u.EmployeeStatus = CASE
                WHEN c.NewEmployeeStatus IS NOT NULL
                 AND ISNULL(u.EmployeeStatus, N'') <> c.NewEmployeeStatus
                THEN c.NewEmployeeStatus ELSE u.EmployeeStatus END,
            u.Email = CASE
                WHEN c.NewEmail IS NOT NULL
                 AND ISNULL(u.Email, N'') <> c.NewEmail
                THEN c.NewEmail ELSE u.Email END,
            u.Mobile = CASE
                WHEN c.NewMobile IS NOT NULL
                 AND ISNULL(u.Mobile, N'') <> c.NewMobile
                THEN c.NewMobile ELSE u.Mobile END,
            u.Gender = CASE
                WHEN c.NewGender IS NOT NULL
                 AND ISNULL(u.Gender, N'') <> c.NewGender
                THEN c.NewGender ELSE u.Gender END,
            u.DOJ = CASE
                WHEN c.NewDOJ IS NOT NULL
                 AND (u.DOJ IS NULL OR u.DOJ <> c.NewDOJ)
                THEN c.NewDOJ ELSE u.DOJ END,
            u.BranchId = CASE
                WHEN c.NewBranchId IS NOT NULL
                 AND u.BranchId <> c.NewBranchId
                THEN c.NewBranchId ELSE u.BranchId END,
            u.DepartmentId = CASE
                WHEN c.NewDepartmentId IS NOT NULL
                 AND ISNULL(u.DepartmentId, -1) <> c.NewDepartmentId
                THEN c.NewDepartmentId ELSE u.DepartmentId END,
            u.Designation_ID = CASE
                WHEN c.NewDesignationId IS NOT NULL
                 AND ISNULL(u.Designation_ID, -1) <> c.NewDesignationId
                THEN c.NewDesignationId ELSE u.Designation_ID END,
            u.GradeId = CASE
                WHEN c.NewGradeId IS NOT NULL
                 AND ISNULL(u.GradeId, -1) <> c.NewGradeId
                THEN c.NewGradeId ELSE u.GradeId END,
            u.PlantId = CASE
                WHEN c.NewPlantId IS NOT NULL
                 AND u.PlantId <> c.NewPlantId
                THEN c.NewPlantId ELSE u.PlantId END,
            u.RegionId = CASE
                WHEN c.NewRegionId IS NOT NULL
                 AND ISNULL(u.RegionId, -1) <> c.NewRegionId
                THEN c.NewRegionId ELSE u.RegionId END,
            u.DivisionId = CASE
                WHEN c.NewDivisionId IS NOT NULL
                 AND ISNULL(u.DivisionId, -1) <> c.NewDivisionId
                THEN c.NewDivisionId ELSE u.DivisionId END,
            u.CategoryId = CASE
                WHEN c.NewCategoryId IS NOT NULL
                 AND ISNULL(u.CategoryId, -1) <> c.NewCategoryId
                THEN c.NewCategoryId ELSE u.CategoryId END,
            u.SubDepartmentId = CASE
                WHEN c.NewSubDepartmentId IS NOT NULL
                 AND ISNULL(u.SubDepartmentId, -1) <> c.NewSubDepartmentId
                THEN c.NewSubDepartmentId ELSE u.SubDepartmentId END,
            u.ReportingManager = CASE
                WHEN c.NewReportingManager IS NOT NULL
                 AND ISNULL(u.ReportingManager, N'') <> c.NewReportingManager
                THEN c.NewReportingManager ELSE u.ReportingManager END,
            u.ReportingManagerCode = CASE
                WHEN c.NewReportingManagerCode IS NOT NULL
                 AND ISNULL(u.ReportingManagerCode, N'') <> c.NewReportingManagerCode
                THEN c.NewReportingManagerCode ELSE u.ReportingManagerCode END,
            u.ReportingManagersManagerName = CASE
                WHEN c.NewReportingManagersManagerName IS NOT NULL
                 AND ISNULL(u.ReportingManagersManagerName, N'') <> c.NewReportingManagersManagerName
                THEN c.NewReportingManagersManagerName ELSE u.ReportingManagersManagerName END,
            u.ReportingManagersManagerCode = CASE
                WHEN c.NewReportingManagersManagerCode IS NOT NULL
                 AND ISNULL(u.ReportingManagersManagerCode, N'') <> c.NewReportingManagersManagerCode
                THEN c.NewReportingManagersManagerCode ELSE u.ReportingManagersManagerCode END
        FROM dbo.MST_UserMaster AS u
        INNER JOIN #HseUpdateCandidates AS c
            ON c.EmpCode = u.EmployeeCode
        WHERE c.ExistsInMaster = 1
          AND (
                (c.NewEmployeeName IS NOT NULL AND ISNULL(u.EmployeeName, N'') <> c.NewEmployeeName)
             OR (c.NewEmployeeStatus IS NOT NULL AND ISNULL(u.EmployeeStatus, N'') <> c.NewEmployeeStatus)
             OR (c.NewEmail IS NOT NULL AND ISNULL(u.Email, N'') <> c.NewEmail)
             OR (c.NewMobile IS NOT NULL AND ISNULL(u.Mobile, N'') <> c.NewMobile)
             OR (c.NewGender IS NOT NULL AND ISNULL(u.Gender, N'') <> c.NewGender)
             OR (c.NewDOJ IS NOT NULL AND (u.DOJ IS NULL OR u.DOJ <> c.NewDOJ))
             OR (c.NewBranchId IS NOT NULL AND u.BranchId <> c.NewBranchId)
             OR (c.NewDepartmentId IS NOT NULL AND ISNULL(u.DepartmentId, -1) <> c.NewDepartmentId)
             OR (c.NewDesignationId IS NOT NULL AND ISNULL(u.Designation_ID, -1) <> c.NewDesignationId)
             OR (c.NewGradeId IS NOT NULL AND ISNULL(u.GradeId, -1) <> c.NewGradeId)
             OR (c.NewPlantId IS NOT NULL AND u.PlantId <> c.NewPlantId)
             OR (c.NewRegionId IS NOT NULL AND ISNULL(u.RegionId, -1) <> c.NewRegionId)
             OR (c.NewDivisionId IS NOT NULL AND ISNULL(u.DivisionId, -1) <> c.NewDivisionId)
             OR (c.NewCategoryId IS NOT NULL AND ISNULL(u.CategoryId, -1) <> c.NewCategoryId)
             OR (c.NewSubDepartmentId IS NOT NULL AND ISNULL(u.SubDepartmentId, -1) <> c.NewSubDepartmentId)
             OR (c.NewReportingManager IS NOT NULL AND ISNULL(u.ReportingManager, N'') <> c.NewReportingManager)
             OR (c.NewReportingManagerCode IS NOT NULL AND ISNULL(u.ReportingManagerCode, N'') <> c.NewReportingManagerCode)
             OR (c.NewReportingManagersManagerName IS NOT NULL AND ISNULL(u.ReportingManagersManagerName, N'') <> c.NewReportingManagersManagerName)
             OR (c.NewReportingManagersManagerCode IS NOT NULL AND ISNULL(u.ReportingManagersManagerCode, N'') <> c.NewReportingManagersManagerCode)
          );

        SET @UpdatedCount = @@ROWCOUNT;
        SET @EmployeesUpdated = @UpdatedCount;

        /* Existing matches that did not receive an UPDATE */
        SELECT @ExistingMatchCount = COUNT(1)
        FROM #HseUpdateCandidates
        WHERE ExistsInMaster = 1;

        SET @NoChangeCount = @ExistingMatchCount - @UpdatedCount;
        IF @NoChangeCount < 0
            SET @NoChangeCount = 0;

        IF @StartedTran = 1
            COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @StartedTran = 1 AND @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrState INT = ERROR_STATE();

        RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
        RETURN;
    END CATCH;

    /* Standalone summary RS (suppressed when nested under orchestrator) */
    IF @StartedTran = 1
    BEGIN
        SELECT
            BatchId            = @BatchId,
            UpdatedCount       = @UpdatedCount,
            EmployeesUpdated   = @EmployeesUpdated,
            SkippedCount       = @SkippedCount,
            NoChangeCount      = @NoChangeCount,
            MissingMasterCount = @MissingMasterCount,
            ErrorCount         = @ErrorCount,
            ProcessingMode     = @ProcessingMode,
            ExecutedBy         = @ExecutedBy,
            Message            = N'Update-changed-employees completed.';
    END;
END
GO
