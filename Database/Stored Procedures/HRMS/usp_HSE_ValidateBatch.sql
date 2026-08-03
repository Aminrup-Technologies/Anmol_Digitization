/*
    Object:      [dbo].[usp_HSE_ValidateBatch]
    Type:        STORED PROCEDURE
    Phase:       3.1 — HRMS Synchronization Engine
    Purpose:     Validation-only gate for a staged HRMS import batch.
                 Does not synchronize masters, insert/update employees,
                 update hierarchy, or change ImportBatchMaster.Status.

    Source:      dbo.Staging_EmployeeData WHERE BatchId = @BatchId
                 (Never uses KKM_SO_TADA_Export_14Nov25$)
*/

CREATE OR ALTER PROCEDURE [dbo].[usp_HSE_ValidateBatch]
    @BatchId        INT,
    @ProcessingMode INT            = NULL,
    @Modules        NVARCHAR(200)  = NULL,
    @IsValid        BIT            OUTPUT
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
       3) Mandatory fields: EmpCode, EmpName
       ------------------------------------------------------------------ */
    UPDATE s
    SET
        s.ImportStatus = N'Error',
        s.ErrorMessage =
            CASE
                WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                ELSE s.ErrorMessage + N'; '
            END
            + CASE
                WHEN NULLIF(LTRIM(RTRIM(s.EmpCode)), N'') IS NULL
                     AND NULLIF(LTRIM(RTRIM(s.EmpName)), N'') IS NULL
                    THEN N'Missing mandatory fields: EmpCode, EmpName.'
                WHEN NULLIF(LTRIM(RTRIM(s.EmpCode)), N'') IS NULL
                    THEN N'Missing mandatory field: EmpCode.'
                ELSE N'Missing mandatory field: EmpName.'
              END
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId
      AND (
            NULLIF(LTRIM(RTRIM(s.EmpCode)), N'') IS NULL
         OR NULLIF(LTRIM(RTRIM(s.EmpName)), N'') IS NULL
          );

    /* ------------------------------------------------------------------
       4) Duplicate Employee Codes within the batch
       ------------------------------------------------------------------ */
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

    /* ---- Department_ID → MST_Department.Department_ID ---- */
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

    /* ---- Designation_ID → MST_Designation.Designation_ID (business code) ---- */
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

    /* ---- Grade_ID → MST_Grade.Grade_ID ---- */
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

    /* ---- PlantID_ID → MST_Plant.PlantID_ID ---- */
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

    /* ---- Region_ID → MST_Region.Region_ID ---- */
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

    /* ---- Division_ID → MST_Division.Division_ID ---- */
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

    /* ---- Category_ID → MST_Category.Category_ID ---- */
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

    /* ---- Company_ID → MST_Company.Company_ID ---- */
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

    /* ---- SubDepartment_ID → MST_SubDepartment.SubDepartment_ID ---- */
    UPDATE s
    SET
        s.ImportStatus = N'Error',
        s.ErrorMessage =
            CASE
                WHEN s.ErrorMessage IS NULL OR s.ErrorMessage = N'' THEN N''
                ELSE s.ErrorMessage + N'; '
            END
            + N'Invalid master reference: SubDepartment_ID ''' + LTRIM(RTRIM(s.SubDepartment_ID))
              + N''' not found in MST_SubDepartment.'
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId
      AND NULLIF(LTRIM(RTRIM(s.SubDepartment_ID)), N'') IS NOT NULL
      AND NOT EXISTS (
            SELECT 1
            FROM dbo.MST_SubDepartment AS m WITH (NOLOCK)
            WHERE CAST(m.SubDepartment_ID AS NVARCHAR(50)) = LTRIM(RTRIM(s.SubDepartment_ID))
          );
    /* SubDepartment has no Description column in ExpectedDbColumns; unresolved code is Error. */

    /* ------------------------------------------------------------------
       Aggregate counts for summary
       ------------------------------------------------------------------ */
    SELECT
        @ErrorCount = SUM(CASE WHEN s.ImportStatus = N'Error' THEN 1 ELSE 0 END),
        @WarningCount = SUM(CASE WHEN s.ErrorMessage LIKE N'%[WARNING]%' THEN 1 ELSE 0 END),
        @ValidCount = SUM(CASE WHEN s.ImportStatus <> N'Error' THEN 1 ELSE 0 END),
        @MissingMandatoryCount = SUM(
            CASE
                WHEN NULLIF(LTRIM(RTRIM(s.EmpCode)), N'') IS NULL
                  OR NULLIF(LTRIM(RTRIM(s.EmpName)), N'') IS NULL
                THEN 1 ELSE 0
            END),
        @InvalidMasterRefCount = SUM(
            CASE
                WHEN s.ErrorMessage LIKE N'%Invalid master reference%' THEN 1 ELSE 0
            END)
    FROM dbo.Staging_EmployeeData AS s
    WHERE s.BatchId = @BatchId;

    SELECT @DuplicateEmpCodeCount = COUNT(1)
    FROM (
        SELECT UPPER(LTRIM(RTRIM(s.EmpCode))) AS NormalizedEmpCode
        FROM dbo.Staging_EmployeeData AS s
        WHERE s.BatchId = @BatchId
          AND NULLIF(LTRIM(RTRIM(s.EmpCode)), N'') IS NOT NULL
        GROUP BY UPPER(LTRIM(RTRIM(s.EmpCode)))
        HAVING COUNT(1) > 1
    ) AS d;

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
