/*
    Object:      [dbo].[usp_HSE_InsertNewEmployees]
    Type:        STORED PROCEDURE
    Phase:       3.6 — HRMS Synchronization Engine
    Purpose:     Insert NEW employees only into MST_UserMaster from a
                 staged HRMS batch. Does not update existing employees.
                 Does not change reporting hierarchy beyond columns
                 written on INSERT.

    Source:      dbo.Staging_EmployeeData WHERE BatchId = @BatchId
                 (Never uses KKM_SO_TADA_Export_14Nov25$)

    Business key:
      Staging.EmpCode  ↔  MST_UserMaster.EmployeeCode

    FK resolution (staging business code → master row → store master.Id):
      BranchId        ← MST_Branch.Id
      DepartmentId    ← MST_Department.Id
      Designation_ID  ← MST_Designation.Id   (NOT Designation_ID business code)
      GradeId         ← MST_Grade.Id
      PlantId         ← MST_Plant.Id
      RegionId        ← MST_Region.Id
      DivisionId      ← MST_Division.Id
      CategoryId      ← MST_Category.Id
      SubDepartmentId ← MST_SubDepartment.Id

    Orchestrator contract (usp_RunHRMSSynchronization):
      @BatchId, @ExecutedBy,
      @InsertedCount OUTPUT, @SkippedCount OUTPUT, @ErrorCount OUTPUT

    Additional outputs:
      @EmployeesInserted  (= @InsertedCount)
      @MissingMasterCount (= rows blocked by unresolved required masters;
                            also assigned to @ErrorCount for orchestrator)
*/

CREATE OR ALTER PROCEDURE [dbo].[usp_HSE_InsertNewEmployees]
    @BatchId             INT,
    @ExecutedBy          NVARCHAR(128) = NULL,
    @InsertedCount       INT OUTPUT,
    @SkippedCount        INT OUTPUT,
    @ErrorCount          INT OUTPUT,
    @EmployeesInserted   INT = NULL OUTPUT,
    @MissingMasterCount  INT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @InsertedCount = 0;
    SET @SkippedCount = 0;
    SET @ErrorCount = 0;
    SET @EmployeesInserted = 0;
    SET @MissingMasterCount = 0;

    DECLARE @StartedTran BIT = 0;
    DECLARE @CreatedBy VARCHAR(50) =
        LEFT(COALESCE(NULLIF(LTRIM(RTRIM(@ExecutedBy)), N''), N'SYSTEM'), 50);
    DECLARE @CreationMode VARCHAR(50) = N'Bulk';
    DECLARE @CreatedDate SMALLDATETIME =
        CAST(DATEADD(MINUTE, 330, SYSUTCDATETIME()) AS SMALLDATETIME);

    IF @BatchId IS NULL OR @BatchId <= 0
    BEGIN
        RAISERROR(N'usp_HSE_InsertNewEmployees: @BatchId is required.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.ImportBatchMaster WHERE BatchId = @BatchId)
    BEGIN
        RAISERROR(N'usp_HSE_InsertNewEmployees: BatchId was not found in ImportBatchMaster.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Staging_EmployeeData WITH (NOLOCK)
        WHERE BatchId = @BatchId
    )
    BEGIN
        RAISERROR(N'usp_HSE_InsertNewEmployees: No staging records found for the specified BatchId.', 16, 1);
        RETURN;
    END;

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            BEGIN TRANSACTION;
            SET @StartedTran = 1;
        END;

        /* ------------------------------------------------------------------
           Resolve candidates once into a temp table:
             - One row per EmpCode (lowest StagingId wins on duplicates)
             - Skip staging rows already marked Error by validation
             - Resolve all FKs to master Id values
           ------------------------------------------------------------------ */
        IF OBJECT_ID(N'tempdb..#HseInsertCandidates') IS NOT NULL
            DROP TABLE #HseInsertCandidates;

        ;WITH ranked AS (
            SELECT
                s.StagingId,
                EmpCode = LTRIM(RTRIM(s.EmpCode)),
                EmpName = LTRIM(RTRIM(s.EmpName)),
                Email = NULLIF(LTRIM(RTRIM(s.Email)), N''),
                MobileNo = NULLIF(LTRIM(RTRIM(s.MobileNo)), N''),
                Gender = NULLIF(LTRIM(RTRIM(s.Gender)), N''),
                DOJRaw = NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(50), s.DOJ))), N''),
                EmployeeStatus = COALESCE(
                    NULLIF(LTRIM(RTRIM(s.EmployeeStatus)), N''),
                    NULLIF(LTRIM(RTRIM(s.EmpStatus)), N''),
                    N'Active'
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
              AND NULLIF(LTRIM(RTRIM(s.EmpName)), N'') IS NOT NULL
              AND ISNULL(s.ImportStatus, N'Pending') <> N'Error'
        )
        SELECT
            r.StagingId,
            r.EmpCode,
            r.EmpName,
            r.Email,
            r.MobileNo,
            r.Gender,
            DOJ = COALESCE(
                TRY_CONVERT(DATE, r.DOJRaw, 106),  /* dd mon yyyy (ZingHR dump) */
                TRY_CONVERT(DATE, r.DOJRaw, 23),   /* yyyy-mm-dd */
                TRY_CONVERT(DATE, r.DOJRaw, 103),  /* dd/mm/yyyy */
                TRY_CONVERT(DATE, r.DOJRaw)
            ),
            r.EmployeeStatus,
            r.ReportingManager,
            r.ReportingManagerCode,
            r.ReportingManagersManagerName,
            r.ReportingManagersManagerCode,
            BranchId = br.Id,
            DepartmentId = dep.Id,
            DesignationId = des.Id,
            GradeId = gr.Id,
            PlantId = pl.Id,
            RegionId = rg.Id,
            DivisionId = dv.Id,
            CategoryId = cat.Id,
            SubDepartmentId = sd.Id,
            AlreadyExists = CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM dbo.MST_UserMaster AS u
                    WHERE u.EmployeeCode = r.EmpCode
                ) THEN 1 ELSE 0
            END,
            MissingRequiredMaster = CASE
                WHEN br.Id IS NULL OR pl.Id IS NULL THEN 1 ELSE 0
            END,
            MissingMasterDetail = CASE
                WHEN br.Id IS NULL AND pl.Id IS NULL
                    THEN N'Missing required masters: Branch and Plant could not be resolved to Id.'
                WHEN br.Id IS NULL
                    THEN N'Missing required master: Branch_ID ''' + ISNULL(r.BranchCode, N'')
                         + N''' could not be resolved to MST_Branch.Id.'
                WHEN pl.Id IS NULL
                    THEN N'Missing required master: PlantID_ID ''' + ISNULL(r.PlantCode, N'')
                         + N''' could not be resolved to MST_Plant.Id.'
                ELSE NULL
            END
        INTO #HseInsertCandidates
        FROM ranked AS r
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

        /* ------------------------------------------------------------------
           Skipped: already in MST_UserMaster (no update in this procedure)
           ------------------------------------------------------------------ */
        SELECT @SkippedCount = COUNT(1)
        FROM #HseInsertCandidates
        WHERE AlreadyExists = 1;

        /* ------------------------------------------------------------------
           Missing required masters (BranchId / PlantId are NOT NULL on target)
           Mark staging rows Error; do not insert.
           ------------------------------------------------------------------ */
        UPDATE s
        SET
            s.ImportStatus = N'Error',
            s.ErrorMessage =
                CASE
                    WHEN s.ErrorMessage IS NULL OR LTRIM(RTRIM(s.ErrorMessage)) = N'' THEN N''
                    ELSE s.ErrorMessage + N'; '
                END
                + c.MissingMasterDetail
        FROM dbo.Staging_EmployeeData AS s
        INNER JOIN #HseInsertCandidates AS c
            ON c.StagingId = s.StagingId
        WHERE c.AlreadyExists = 0
          AND c.MissingRequiredMaster = 1;

        SELECT @MissingMasterCount = COUNT(1)
        FROM #HseInsertCandidates
        WHERE AlreadyExists = 0
          AND MissingRequiredMaster = 1;

        SET @ErrorCount = @MissingMasterCount;

        /* ------------------------------------------------------------------
           Insert new employees only
           ------------------------------------------------------------------ */
        INSERT INTO dbo.MST_UserMaster (
            EmployeeCode,
            EmployeeName,
            BranchId,
            PlantId,
            EmployeeStatus,
            DepartmentId,
            GradeId,
            SubDepartmentId,
            DOJ,
            CategoryId,
            Email,
            Mobile,
            Gender,
            RegionId,
            DivisionId,
            CreatedDate,
            CreatedBy,
            CreationMode,
            ReportingManager,
            ReportingManagerCode,
            ReportingManagersManagerName,
            ReportingManagersManagerCode,
            Designation_ID
        )
        SELECT
            c.EmpCode,
            LEFT(c.EmpName, 100),
            c.BranchId,
            c.PlantId,
            LEFT(c.EmployeeStatus, 50),
            c.DepartmentId,
            c.GradeId,
            c.SubDepartmentId,
            c.DOJ,
            c.CategoryId,
            LEFT(c.Email, 100),
            LEFT(c.MobileNo, 10),
            LEFT(c.Gender, 50),
            c.RegionId,
            c.DivisionId,
            @CreatedDate,
            @CreatedBy,
            @CreationMode,
            LEFT(c.ReportingManager, 100),
            LEFT(c.ReportingManagerCode, 50),
            LEFT(c.ReportingManagersManagerName, 100),
            LEFT(c.ReportingManagersManagerCode, 50),
            c.DesignationId
        FROM #HseInsertCandidates AS c
        WHERE c.AlreadyExists = 0
          AND c.MissingRequiredMaster = 0
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.MST_UserMaster AS u
                WHERE u.EmployeeCode = c.EmpCode
              );

        SET @InsertedCount = @@ROWCOUNT;
        SET @EmployeesInserted = @InsertedCount;

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

    /* ------------------------------------------------------------------
       Standalone summary RS (suppressed when nested under orchestrator)
       ------------------------------------------------------------------ */
    IF @StartedTran = 1
    BEGIN
        SELECT
            BatchId            = @BatchId,
            InsertedCount      = @InsertedCount,
            EmployeesInserted  = @EmployeesInserted,
            SkippedCount       = @SkippedCount,
            MissingMasterCount = @MissingMasterCount,
            ErrorCount         = @ErrorCount,
            CreatedBy          = @CreatedBy,
            CreationMode       = @CreationMode,
            Message            = N'Insert-new-employees completed.';
    END;
END
GO
