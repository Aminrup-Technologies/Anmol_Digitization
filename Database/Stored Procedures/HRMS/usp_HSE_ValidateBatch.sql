/*
    Object:      [dbo].[usp_HSE_ValidateBatch]
    Type:        STORED PROCEDURE
    Phase:       3.1 — HRMS Synchronization Engine
    Purpose:     Validation-only gate for a staged HRMS import batch.
                 Does not synchronize masters, insert/update employees,
                 update hierarchy, or change ImportBatchMaster.Status.

    Source:      dbo.Staging_EmployeeData WHERE BatchId = @BatchId
                 (Never uses KKM_SO_TADA_Export_14Nov25$)

    Phase A — action-aware row rules:
      Optional @Action (MASTER|INSERT|UPDATE|HIERARCHY) scopes which
      row-level checks run. Optional @OverwriteFields further scopes
      UPDATE master-ref / EmpName checks to selected FieldCodes.
      When @Action is NULL, legacy combined-run rules apply (all
      current checks, including Company) so unrelated callers are unchanged.
*/

CREATE OR ALTER PROCEDURE [dbo].[usp_HSE_ValidateBatch]
    @BatchId         INT,
    @ProcessingMode  INT            = NULL,
    @Modules         NVARCHAR(200)  = NULL,
    @IsValid         BIT            OUTPUT,
    @Action          NVARCHAR(20)   = NULL,
    @OverwriteFields NVARCHAR(MAX)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @IsValid = 0;

    DECLARE @TotalRecords            INT = 0;
    DECLARE @ValidCount              INT = 0;
    DECLARE @ErrorCount              INT = 0;
    DECLARE @WarningCount            INT = 0;
    DECLARE @DuplicateEmpCodeCount   INT = 0;
    DECLARE @MissingMandatoryCount   INT = 0;
    DECLARE @InvalidMasterRefCount   INT = 0;
    DECLARE @ValidationMessage       NVARCHAR(500) = N'';
    DECLARE @BatchLevelFailed        BIT = 0;

    /* ------------------------------------------------------------------
       1) Validate BatchId exists
       ------------------------------------------------------------------ */
    IF @BatchId IS NULL OR @BatchId <= 0
    BEGIN
        SET @ValidationMessage = N'BatchId is required and must be a positive integer.';
        GOTO ReturnSummary;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.ImportBatchMaster WITH (NOLOCK)
        WHERE BatchId = @BatchId
    )
    BEGIN
        SET @ValidationMessage = N'BatchId was not found in ImportBatchMaster.';
        GOTO ReturnSummary;
    END;

    /* ------------------------------------------------------------------
       2) Validate staging records exist
       ------------------------------------------------------------------ */
    SELECT @TotalRecords = COUNT(1)
    FROM dbo.Staging_EmployeeData WITH (NOLOCK)
    WHERE BatchId = @BatchId;

    IF @TotalRecords = 0
    BEGIN
        SET @ValidationMessage = N'No staging records found for the specified BatchId.';
        GOTO ReturnSummary;
    END;

    /* ------------------------------------------------------------------
       6) Validate ProcessingMode and Modules (batch-level)
       ------------------------------------------------------------------ */
    IF @ProcessingMode IS NULL OR @ProcessingMode NOT IN (0, 1, 2)
    BEGIN
        SET @BatchLevelFailed = 1;
        SET @ValidationMessage = N'ProcessingMode must be 0 (Insert), 1 (Update All), or 2 (Update Selected Fields).';
    END;

    IF @BatchLevelFailed = 0
       AND (
            @Modules IS NULL
            OR LTRIM(RTRIM(@Modules)) = N''
       )
    BEGIN
        SET @BatchLevelFailed = 1;
        SET @ValidationMessage = N'Modules is required (pipe-delimited: MASTER|EMPLOYEE|HIERARCHY).';
    END;

    IF @BatchLevelFailed = 0
    BEGIN
        DECLARE @ModuleToken NVARCHAR(50);
        DECLARE @ModulesNormalized NVARCHAR(200) = UPPER(REPLACE(LTRIM(RTRIM(@Modules)), N' ', N''));
        DECLARE @ModuleXml XML =
            CONVERT(XML, N'<r><i>' + REPLACE(@ModulesNormalized, N'|', N'</i><i>') + N'</i></r>');

        DECLARE module_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT LTRIM(RTRIM(T.c.value('.', 'NVARCHAR(50)')))
            FROM @ModuleXml.nodes('/r/i') AS T(c)
            WHERE LTRIM(RTRIM(T.c.value('.', 'NVARCHAR(50)'))) <> N'';

        OPEN module_cursor;
        FETCH NEXT FROM module_cursor INTO @ModuleToken;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF @ModuleToken NOT IN (N'MASTER', N'EMPLOYEE', N'HIERARCHY')
            BEGIN
                SET @BatchLevelFailed = 1;
                SET @ValidationMessage =
                    N'Invalid Modules token ''' + @ModuleToken
                    + N'''. Allowed values: MASTER, EMPLOYEE, HIERARCHY.';
                BREAK;
            END;

            FETCH NEXT FROM module_cursor INTO @ModuleToken;
        END;

        CLOSE module_cursor;
        DEALLOCATE module_cursor;
    END;

    IF @BatchLevelFailed = 1
    BEGIN
        /* Do not mutate staging rows when the run configuration itself is invalid. */
        GOTO ReturnSummary;
    END;

    /* ------------------------------------------------------------------
       Action scope (Phase A)
       @Action, when supplied, selects which row-level rules run.
       @OverwriteFields further scopes UPDATE org/name checks.
       NULL @Action → ALL (legacy combined-run behaviour).
       ------------------------------------------------------------------ */
    DECLARE @ActionNorm NVARCHAR(20) = UPPER(REPLACE(LTRIM(RTRIM(ISNULL(@Action, N''))), N' ', N''));
    DECLARE @Of NVARCHAR(MAX);
    DECLARE @ModuleCount INT = 0;
    DECLARE @RunEmpCode BIT = 1;
    DECLARE @RunEmpName BIT = 1;
    DECLARE @RunDuplicates BIT = 1;
    DECLARE @RunBranch BIT = 1;
    DECLARE @RunDepartment BIT = 1;
    DECLARE @RunDesignation BIT = 1;
    DECLARE @RunGrade BIT = 1;
    DECLARE @RunPlant BIT = 1;
    DECLARE @RunRegion BIT = 1;
    DECLARE @RunDivision BIT = 1;
    DECLARE @RunCategory BIT = 1;
    DECLARE @RunCompany BIT = 1;
    DECLARE @RunSubDepartment BIT = 1;

    IF @ActionNorm = N''
        SET @ActionNorm = NULL;

    IF @ActionNorm IS NOT NULL AND @ActionNorm NOT IN (N'MASTER', N'INSERT', N'UPDATE', N'HIERARCHY')
    BEGIN
        SET @IsValid = 0;
        SET @ValidationMessage = N'Action must be MASTER, INSERT, UPDATE, or HIERARCHY.';
        GOTO ReturnSummary;
    END;

    IF @ActionNorm IS NULL
    BEGIN
        /* Infer a single-token module when the caller did not pass @Action. */
        DECLARE @ModulesForScope NVARCHAR(200) = UPPER(REPLACE(LTRIM(RTRIM(ISNULL(@Modules, N''))), N' ', N''));
        DECLARE @ScopeXml XML =
            CONVERT(XML, N'<r><i>' + REPLACE(@ModulesForScope, N'|', N'</i><i>') + N'</i></r>');
        SET @ModuleCount = (
            SELECT COUNT(1)
            FROM @ScopeXml.nodes('/r/i') AS T(c)
            WHERE LTRIM(RTRIM(T.c.value('.', 'NVARCHAR(50)'))) <> N''
        );

        IF @ModuleCount = 1 AND @ModulesForScope = N'MASTER'
            SET @ActionNorm = N'MASTER';
        ELSE IF @ModuleCount = 1 AND @ModulesForScope = N'HIERARCHY'
            SET @ActionNorm = N'HIERARCHY';
        ELSE IF @ModuleCount = 1 AND @ModulesForScope = N'EMPLOYEE' AND @ProcessingMode = 0
            SET @ActionNorm = N'INSERT';
        ELSE IF @ModuleCount = 1 AND @ModulesForScope = N'EMPLOYEE' AND @ProcessingMode IN (1, 2)
            SET @ActionNorm = N'UPDATE';
        /* else leave NULL = ALL legacy rules */
    END;

    IF @ActionNorm = N'MASTER'
    BEGIN
        SET @RunEmpCode = 0;
        SET @RunEmpName = 0;
        SET @RunDuplicates = 0;
        SET @RunCompany = 0;
        /* Org master uncreatable-code checks remain: SynchronizeMasters consumes those IDs. */
    END;
    ELSE IF @ActionNorm = N'INSERT'
    BEGIN
        SET @RunCompany = 0; /* Insert does not write CompanyId */
    END;
    ELSE IF @ActionNorm = N'HIERARCHY'
    BEGIN
        SET @RunEmpName = 0;
        SET @RunBranch = 0;
        SET @RunDepartment = 0;
        SET @RunDesignation = 0;
        SET @RunGrade = 0;
        SET @RunPlant = 0;
        SET @RunRegion = 0;
        SET @RunDivision = 0;
        SET @RunCategory = 0;
        SET @RunCompany = 0;
        SET @RunSubDepartment = 0;
        /* EmpCode + duplicates remain: hierarchy matches EmpCode and collapses dupes by StagingId. */
    END;
    ELSE IF @ActionNorm = N'UPDATE'
    BEGIN
        SET @RunCompany = 0; /* Update does not write CompanyId */
        /* EmpCode + duplicates always: update match key / ambiguous source row.
           ProcessingMode 1 (overwrite all) or empty @OverwriteFields:
           keep EmpName + all org checks so we do not weaken integrity.
           ProcessingMode 2 with a field list: validate only selected fields. */
        IF @ProcessingMode = 2
           AND LEN(LTRIM(RTRIM(ISNULL(@OverwriteFields, N'')))) > 0
        BEGIN
            SET @Of = N'|' + UPPER(REPLACE(REPLACE(ISNULL(@OverwriteFields, N''), N' ', N''), N',', N'|')) + N'|';
            SET @RunEmpName = CASE WHEN CHARINDEX(N'|EMPLOYEENAME|', @Of) > 0 OR CHARINDEX(N'|EMPNAME|', @Of) > 0 THEN 1 ELSE 0 END;
            SET @RunBranch = CASE WHEN CHARINDEX(N'|BRANCH|', @Of) > 0 OR CHARINDEX(N'|BRANCHID|', @Of) > 0 OR CHARINDEX(N'|BRANCH_ID|', @Of) > 0 THEN 1 ELSE 0 END;
            SET @RunDepartment = CASE WHEN CHARINDEX(N'|DEPARTMENT|', @Of) > 0 OR CHARINDEX(N'|DEPARTMENTID|', @Of) > 0 OR CHARINDEX(N'|DEPARTMENT_ID|', @Of) > 0 THEN 1 ELSE 0 END;
            SET @RunDesignation = CASE WHEN CHARINDEX(N'|DESIGNATION|', @Of) > 0 OR CHARINDEX(N'|DESIGNATIONID|', @Of) > 0 OR CHARINDEX(N'|DESIGNATION_ID|', @Of) > 0 THEN 1 ELSE 0 END;
            SET @RunGrade = CASE WHEN CHARINDEX(N'|GRADE|', @Of) > 0 OR CHARINDEX(N'|GRADEID|', @Of) > 0 OR CHARINDEX(N'|GRADE_ID|', @Of) > 0 THEN 1 ELSE 0 END;
            SET @RunPlant = CASE WHEN CHARINDEX(N'|PLANT|', @Of) > 0 OR CHARINDEX(N'|PLANTID|', @Of) > 0 OR CHARINDEX(N'|PLANTID_ID|', @Of) > 0 THEN 1 ELSE 0 END;
            SET @RunRegion = CASE WHEN CHARINDEX(N'|REGION|', @Of) > 0 OR CHARINDEX(N'|REGIONID|', @Of) > 0 OR CHARINDEX(N'|REGION_ID|', @Of) > 0 THEN 1 ELSE 0 END;
            SET @RunDivision = CASE WHEN CHARINDEX(N'|DIVISION|', @Of) > 0 OR CHARINDEX(N'|DIVISIONID|', @Of) > 0 OR CHARINDEX(N'|DIVISION_ID|', @Of) > 0 THEN 1 ELSE 0 END;
            SET @RunCategory = CASE WHEN CHARINDEX(N'|CATEGORY|', @Of) > 0 OR CHARINDEX(N'|CATEGORYID|', @Of) > 0 OR CHARINDEX(N'|CATEGORY_ID|', @Of) > 0 THEN 1 ELSE 0 END;
            SET @RunSubDepartment = CASE WHEN CHARINDEX(N'|SUBDEPARTMENT|', @Of) > 0 OR CHARINDEX(N'|SUBDEPARTMENTID|', @Of) > 0 OR CHARINDEX(N'|SUBDEPARTMENT_ID|', @Of) > 0 THEN 1 ELSE 0 END;
        END;
    END;

    /* ------------------------------------------------------------------
       Reset prior validation markers for this batch, then apply checks.
       ------------------------------------------------------------------ */
    UPDATE s
    SET
        s.ImportStatus = N'Pending',
        s.ErrorMessage = NULL
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId;

    /* Helper: append an error fragment without wiping prior fragments */
    /* Implemented via repeated UPDATEs for SQL Server 2017 clarity.   */

    /* ------------------------------------------------------------------
       3) Mandatory fields (action-scoped)
          EmpCode: INSERT, UPDATE, HIERARCHY (match key). Not MASTER.
          EmpName: INSERT always; UPDATE only if EmployeeName selected.
       ------------------------------------------------------------------ */
    IF @RunEmpCode = 1 OR @RunEmpName = 1
    BEGIN
        UPDATE s
        SET
            s.ImportStatus = N'Error',
            s.ErrorMessage =
                CASE
                    WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                    ELSE s.ErrorMessage + N'; '
                END
                + CASE
                    WHEN @RunEmpCode = 1 AND NULLIF(LTRIM(RTRIM(s.EmpCode)), N'') IS NULL
                         AND @RunEmpName = 1 AND NULLIF(LTRIM(RTRIM(s.EmpName)), N'') IS NULL
                        THEN N'Missing mandatory fields: EmpCode, EmpName.'
                    WHEN @RunEmpCode = 1 AND NULLIF(LTRIM(RTRIM(s.EmpCode)), N'') IS NULL
                        THEN N'Missing mandatory field: EmpCode.'
                    ELSE N'Missing mandatory field: EmpName.'
                  END
        FROM dbo.Staging_EmployeeData AS s
        WHERE s.BatchId = @BatchId
          AND (
                (@RunEmpCode = 1 AND NULLIF(LTRIM(RTRIM(s.EmpCode)), N'') IS NULL)
             OR (@RunEmpName = 1 AND NULLIF(LTRIM(RTRIM(s.EmpName)), N'') IS NULL)
              );
    END;

    /* ------------------------------------------------------------------
       4) Duplicate Employee Codes within the batch
          Required where EmpCode is the match key (INSERT/UPDATE/HIERARCHY).
          Not required for MASTER (SynchronizeMasters groups by master codes).
       ------------------------------------------------------------------ */
    IF @RunDuplicates = 1
    BEGIN
        ;WITH Dupes AS
        (
            SELECT
                s.StagingId,
                NormalizedEmpCode = UPPER(LTRIM(RTRIM(s.EmpCode)))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.EmpCode)), N'') IS NOT NULL
        ),
        DupCounts AS
        (
            SELECT
                d.StagingId,
                d.NormalizedEmpCode,
                Cnt = COUNT(1) OVER (PARTITION BY d.NormalizedEmpCode)
            FROM Dupes AS d
        )
        UPDATE s
        SET
            s.ImportStatus = N'Error',
            s.ErrorMessage =
                CASE
                    WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                    ELSE s.ErrorMessage + N'; '
                END
                + N'Duplicate EmpCode within batch: ''' + LTRIM(RTRIM(s.EmpCode)) + N'''.'
        FROM dbo.Staging_EmployeeData AS s
        INNER JOIN DupCounts AS dc
            ON dc.StagingId = s.StagingId
        WHERE s.BatchId = @BatchId
          AND dc.Cnt > 1;
    END;

    /* ------------------------------------------------------------------
       5) Master reference checks
          - Non-blank staging code must resolve to the master business key.
          - Designation uses MST_Designation.Designation_ID (not Id),
            matching the approved HRMS → MST_UserMaster mapping chain.
          - Unresolved code with no companion Description → Error
            (cannot resolve and SynchronizeMasters cannot create).
          - Unresolved code with Description present → Warning text only;
            ImportStatus remains Error if already Error, else Pending
            (SynchronizeMasters can create the master).
       ------------------------------------------------------------------ */

    /* ---- Branch_ID → MST_Branch.Branch_ID ---- */
    IF @RunBranch = 1
    BEGIN
        UPDATE s
        SET
            s.ImportStatus =
                CASE
                    WHEN NULLIF(LTRIM(RTRIM(s.Branch_Description)), N'') IS NULL THEN N'Error'
                    WHEN s.ImportStatus = N'Error' THEN N'Error'
                    ELSE s.ImportStatus
                END,
            s.ErrorMessage =
                CASE
                    WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                    ELSE s.ErrorMessage + N'; '
                END
                + CASE
                    WHEN NULLIF(LTRIM(RTRIM(s.Branch_Description)), N'') IS NULL
                        THEN N'Invalid master reference: Branch_ID ''' + LTRIM(RTRIM(s.Branch_ID))
                             + N''' not found in MST_Branch and Branch_Description is empty.'
                    ELSE N'[WARNING] Branch_ID ''' + LTRIM(RTRIM(s.Branch_ID))
                         + N''' not found in MST_Branch; expected to be created by SynchronizeMasters.'
                  END
        FROM dbo.Staging_EmployeeData AS s
        WHERE s.BatchId = @BatchId
          AND NULLIF(LTRIM(RTRIM(s.Branch_ID)), N'') IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.MST_Branch AS m WITH (NOLOCK)
                WHERE m.Branch_ID = TRY_CONVERT(INT, LTRIM(RTRIM(s.Branch_ID)))
              );
    END;

    /* ---- Department_ID → MST_Department.Department_ID ---- */
    IF @RunDepartment = 1
    BEGIN
    UPDATE s
    SET
        s.ImportStatus =
            CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Department_Description)), N'') IS NULL THEN N'Error'
                WHEN s.ImportStatus = N'Error' THEN N'Error'
                ELSE s.ImportStatus
            END,
        s.ErrorMessage =
            CASE
                WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                ELSE s.ErrorMessage + N'; '
            END
            + CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Department_Description)), N'') IS NULL
                    THEN N'Invalid master reference: Department_ID ''' + LTRIM(RTRIM(s.Department_ID))
                         + N''' not found in MST_Department and Department_Description is empty.'
                ELSE N'[WARNING] Department_ID ''' + LTRIM(RTRIM(s.Department_ID))
                     + N''' not found in MST_Department; expected to be created by SynchronizeMasters.'
              END
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId
      AND NULLIF(LTRIM(RTRIM(s.Department_ID)), N'') IS NOT NULL
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.MST_Department AS m WITH (NOLOCK)
                WHERE m.Department_ID = LTRIM(RTRIM(s.Department_ID))
              );
    END;

    /* ---- Designation_ID → MST_Designation.Designation_ID (business code) ---- */
    IF @RunDesignation = 1
    BEGIN
    UPDATE s
    SET
        s.ImportStatus =
            CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Designation_Description)), N'') IS NULL THEN N'Error'
                WHEN s.ImportStatus = N'Error' THEN N'Error'
                ELSE s.ImportStatus
            END,
        s.ErrorMessage =
            CASE
                WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                ELSE s.ErrorMessage + N'; '
            END
            + CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Designation_Description)), N'') IS NULL
                    THEN N'Invalid master reference: Designation_ID ''' + LTRIM(RTRIM(s.Designation_ID))
                         + N''' not found in MST_Designation.Designation_ID and Designation_Description is empty.'
                ELSE N'[WARNING] Designation_ID ''' + LTRIM(RTRIM(s.Designation_ID))
                     + N''' not found in MST_Designation.Designation_ID; expected to be created by SynchronizeMasters.'
              END
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId
      AND NULLIF(LTRIM(RTRIM(s.Designation_ID)), N'') IS NOT NULL
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.MST_Designation AS m WITH (NOLOCK)
                WHERE m.Designation_ID = LTRIM(RTRIM(s.Designation_ID))
              );
    END;

    /* ---- Grade_ID → MST_Grade.Grade_ID ---- */
    IF @RunGrade = 1
    BEGIN
    UPDATE s
    SET
        s.ImportStatus =
            CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Grade_Description)), N'') IS NULL THEN N'Error'
                WHEN s.ImportStatus = N'Error' THEN N'Error'
                ELSE s.ImportStatus
            END,
        s.ErrorMessage =
            CASE
                WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                ELSE s.ErrorMessage + N'; '
            END
            + CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Grade_Description)), N'') IS NULL
                    THEN N'Invalid master reference: Grade_ID ''' + LTRIM(RTRIM(s.Grade_ID))
                         + N''' not found in MST_Grade and Grade_Description is empty.'
                ELSE N'[WARNING] Grade_ID ''' + LTRIM(RTRIM(s.Grade_ID))
                     + N''' not found in MST_Grade; expected to be created by SynchronizeMasters.'
              END
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId
      AND NULLIF(LTRIM(RTRIM(s.Grade_ID)), N'') IS NOT NULL
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.MST_Grade AS m WITH (NOLOCK)
                WHERE m.Grade_ID = LTRIM(RTRIM(s.Grade_ID))
              );
    END;

    /* ---- PlantID_ID → MST_Plant.PlantID_ID ---- */
    IF @RunPlant = 1
    BEGIN
    UPDATE s
    SET
        s.ImportStatus =
            CASE
                WHEN NULLIF(LTRIM(RTRIM(s.PlantID_Description)), N'') IS NULL THEN N'Error'
                WHEN s.ImportStatus = N'Error' THEN N'Error'
                ELSE s.ImportStatus
            END,
        s.ErrorMessage =
            CASE
                WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                ELSE s.ErrorMessage + N'; '
            END
            + CASE
                WHEN NULLIF(LTRIM(RTRIM(s.PlantID_Description)), N'') IS NULL
                    THEN N'Invalid master reference: PlantID_ID ''' + LTRIM(RTRIM(s.PlantID_ID))
                         + N''' not found in MST_Plant and PlantID_Description is empty.'
                ELSE N'[WARNING] PlantID_ID ''' + LTRIM(RTRIM(s.PlantID_ID))
                     + N''' not found in MST_Plant; expected to be created by SynchronizeMasters.'
              END
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId
      AND NULLIF(LTRIM(RTRIM(s.PlantID_ID)), N'') IS NOT NULL
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.MST_Plant AS m WITH (NOLOCK)
                WHERE CAST(m.PlantID_ID AS NVARCHAR(50)) = LTRIM(RTRIM(s.PlantID_ID))
              );
    END;

    /* ---- Region_ID → MST_Region.Region_ID ---- */
    IF @RunRegion = 1
    BEGIN
    UPDATE s
    SET
        s.ImportStatus =
            CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Region_Description)), N'') IS NULL THEN N'Error'
                WHEN s.ImportStatus = N'Error' THEN N'Error'
                ELSE s.ImportStatus
            END,
        s.ErrorMessage =
            CASE
                WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                ELSE s.ErrorMessage + N'; '
            END
            + CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Region_Description)), N'') IS NULL
                    THEN N'Invalid master reference: Region_ID ''' + LTRIM(RTRIM(s.Region_ID))
                         + N''' not found in MST_Region and Region_Description is empty.'
                ELSE N'[WARNING] Region_ID ''' + LTRIM(RTRIM(s.Region_ID))
                     + N''' not found in MST_Region; expected to be created by SynchronizeMasters.'
              END
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId
      AND NULLIF(LTRIM(RTRIM(s.Region_ID)), N'') IS NOT NULL
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.MST_Region AS m WITH (NOLOCK)
                WHERE m.Region_ID = LTRIM(RTRIM(s.Region_ID))
              );
    END;

    /* ---- Division_ID → MST_Division.Division_ID ---- */
    IF @RunDivision = 1
    BEGIN
    UPDATE s
    SET
        s.ImportStatus =
            CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Division_Description)), N'') IS NULL THEN N'Error'
                WHEN s.ImportStatus = N'Error' THEN N'Error'
                ELSE s.ImportStatus
            END,
        s.ErrorMessage =
            CASE
                WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                ELSE s.ErrorMessage + N'; '
            END
            + CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Division_Description)), N'') IS NULL
                    THEN N'Invalid master reference: Division_ID ''' + LTRIM(RTRIM(s.Division_ID))
                         + N''' not found in MST_Division and Division_Description is empty.'
                ELSE N'[WARNING] Division_ID ''' + LTRIM(RTRIM(s.Division_ID))
                     + N''' not found in MST_Division; expected to be created by SynchronizeMasters.'
              END
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId
      AND NULLIF(LTRIM(RTRIM(s.Division_ID)), N'') IS NOT NULL
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.MST_Division AS m WITH (NOLOCK)
                WHERE CAST(m.Division_ID AS NVARCHAR(50)) = LTRIM(RTRIM(s.Division_ID))
              );
    END;

    /* ---- Category_ID → MST_Category.Category_ID ---- */
    IF @RunCategory = 1
    BEGIN
    UPDATE s
    SET
        s.ImportStatus =
            CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Category_Description)), N'') IS NULL THEN N'Error'
                WHEN s.ImportStatus = N'Error' THEN N'Error'
                ELSE s.ImportStatus
            END,
        s.ErrorMessage =
            CASE
                WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                ELSE s.ErrorMessage + N'; '
            END
            + CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Category_Description)), N'') IS NULL
                    THEN N'Invalid master reference: Category_ID ''' + LTRIM(RTRIM(s.Category_ID))
                         + N''' not found in MST_Category and Category_Description is empty.'
                ELSE N'[WARNING] Category_ID ''' + LTRIM(RTRIM(s.Category_ID))
                     + N''' not found in MST_Category; expected to be created by SynchronizeMasters.'
              END
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId
      AND NULLIF(LTRIM(RTRIM(s.Category_ID)), N'') IS NOT NULL
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.MST_Category AS m WITH (NOLOCK)
                WHERE CAST(m.Category_ID AS NVARCHAR(50)) = LTRIM(RTRIM(s.Category_ID))
              );
    END;

    /* ---- Company_ID → MST_Company.Company_ID ----
          Not consumed by MASTER/INSERT/UPDATE/HIERARCHY child SPs.
          Retained only for legacy ALL (NULL @Action) callers.
       ------------------------------------------------------------------ */
    IF @RunCompany = 1
    BEGIN
    UPDATE s
    SET
        s.ImportStatus =
            CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Company_Description)), N'') IS NULL THEN N'Error'
                WHEN s.ImportStatus = N'Error' THEN N'Error'
                ELSE s.ImportStatus
            END,
        s.ErrorMessage =
            CASE
                WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                ELSE s.ErrorMessage + N'; '
            END
            + CASE
                WHEN NULLIF(LTRIM(RTRIM(s.Company_Description)), N'') IS NULL
                    THEN N'Invalid master reference: Company_ID ''' + LTRIM(RTRIM(s.Company_ID))
                         + N''' not found in MST_Company and Company_Description is empty.'
                ELSE N'[WARNING] Company_ID ''' + LTRIM(RTRIM(s.Company_ID))
                     + N''' not found in MST_Company; expected to be created by SynchronizeMasters.'
              END
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId
      AND NULLIF(LTRIM(RTRIM(s.Company_ID)), N'') IS NOT NULL
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.MST_Company AS m WITH (NOLOCK)
                WHERE CAST(m.Company_ID AS NVARCHAR(50)) = LTRIM(RTRIM(s.Company_ID))
              );
    END;

    /* ---- SubDepartment_ID → MST_SubDepartment.SubDepartment_ID ----
          Same createability rule as Department / Designation / etc.:
            - unresolved code + empty Description → Error
            - unresolved code + Description present → [WARNING]
              (SynchronizeMasters can create the master)
       ------------------------------------------------------------------ */
    IF @RunSubDepartment = 1
    BEGIN
    UPDATE s
    SET
        s.ImportStatus =
            CASE
                WHEN NULLIF(LTRIM(RTRIM(s.SubDepartment_Description)), N'') IS NULL THEN N'Error'
                WHEN s.ImportStatus = N'Error' THEN N'Error'
                ELSE s.ImportStatus
            END,
        s.ErrorMessage =
            CASE
                WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                ELSE s.ErrorMessage + N'; '
            END
            + CASE
                WHEN NULLIF(LTRIM(RTRIM(s.SubDepartment_Description)), N'') IS NULL
                    THEN N'Invalid master reference: SubDepartment_ID ''' + LTRIM(RTRIM(s.SubDepartment_ID))
                         + N''' not found in MST_SubDepartment and SubDepartment_Description is empty.'
                ELSE N'[WARNING] SubDepartment_ID ''' + LTRIM(RTRIM(s.SubDepartment_ID))
                     + N''' not found in MST_SubDepartment; expected to be created by SynchronizeMasters.'
              END
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId
      AND NULLIF(LTRIM(RTRIM(s.SubDepartment_ID)), N'') IS NOT NULL
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.MST_SubDepartment AS m WITH (NOLOCK)
                WHERE CAST(m.SubDepartment_ID AS NVARCHAR(50)) = LTRIM(RTRIM(s.SubDepartment_ID))
              );
    END;

    /* ------------------------------------------------------------------
       Aggregate counts for summary
       ------------------------------------------------------------------ */
    SELECT
        @ErrorCount = SUM(CASE WHEN s.ImportStatus = N'Error' THEN 1 ELSE 0 END),
        @WarningCount = SUM(CASE WHEN s.ErrorMessage LIKE N'%[[]WARNING]%' THEN 1 ELSE 0 END),
        @ValidCount = SUM(CASE WHEN s.ImportStatus <> N'Error' THEN 1 ELSE 0 END),
        @MissingMandatoryCount = SUM(
            CASE
                WHEN (@RunEmpCode = 1 AND NULLIF(LTRIM(RTRIM(s.EmpCode)), N'') IS NULL)
                  OR (@RunEmpName = 1 AND NULLIF(LTRIM(RTRIM(s.EmpName)), N'') IS NULL)
                THEN 1 ELSE 0
            END),
        @InvalidMasterRefCount = SUM(
            CASE
                WHEN s.ErrorMessage LIKE N'%Invalid master reference%' THEN 1 ELSE 0
            END)
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId;

    IF @RunDuplicates = 1
    BEGIN
        SELECT @DuplicateEmpCodeCount = COUNT(1)
        FROM (
            SELECT UPPER(LTRIM(RTRIM(s.EmpCode))) AS NormalizedEmpCode
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.EmpCode)), N'') IS NOT NULL
            GROUP BY UPPER(LTRIM(RTRIM(s.EmpCode)))
            HAVING COUNT(1) > 1
        ) AS d;
    END
    ELSE
        SET @DuplicateEmpCodeCount = 0;

    SET @ErrorCount = ISNULL(@ErrorCount, 0);
    SET @WarningCount = ISNULL(@WarningCount, 0);
    SET @ValidCount = ISNULL(@ValidCount, 0);
    SET @MissingMandatoryCount = ISNULL(@MissingMandatoryCount, 0);
    SET @InvalidMasterRefCount = ISNULL(@InvalidMasterRefCount, 0);
    SET @DuplicateEmpCodeCount = ISNULL(@DuplicateEmpCodeCount, 0);

    /* Batch is valid for sync continuation when no row-level Errors remain. */
    IF @ErrorCount = 0
    BEGIN
        SET @IsValid = 1;
        SET @ValidationMessage =
            CASE
                WHEN @WarningCount > 0
                    THEN N'Validation completed with warnings. No blocking errors.'
                ELSE N'Validation completed successfully.'
            END;
    END
    ELSE
    BEGIN
        SET @IsValid = 0;
        SET @ValidationMessage =
            N'Validation completed with blocking errors. Review Staging_EmployeeData.ImportStatus / ErrorMessage.';
    END;

ReturnSummary:
    IF @ValidationMessage = N'' AND @IsValid = 0 AND @TotalRecords = 0 AND @BatchId IS NOT NULL
        SET @ValidationMessage = N'Validation failed.';

    /* ------------------------------------------------------------------
       8) Validation summary result set
       ------------------------------------------------------------------ */
    SELECT
        BatchId                 = @BatchId,
        Action                  = @ActionNorm,
        TotalRecords            = @TotalRecords,
        ValidCount              = @ValidCount,
        ErrorCount              = @ErrorCount,
        WarningCount            = @WarningCount,
        DuplicateEmpCodeCount   = @DuplicateEmpCodeCount,
        MissingMandatoryCount   = @MissingMandatoryCount,
        InvalidMasterRefCount   = @InvalidMasterRefCount,
        IsValid                 = @IsValid,
        ValidationMessage       = @ValidationMessage;
END
GO
