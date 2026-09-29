/*
    Object:      [dbo].[usp_HSE_SynchronizeMasters]
    Type:        STORED PROCEDURE
    Phase:       3.5 — HRMS Synchronization Engine
    Purpose:     Upsert organization / lookup masters from a staged HRMS
                 batch. No employee synchronization. No hierarchy updates.

    Source:      dbo.Staging_EmployeeData WHERE BatchId = @BatchId
                 (Never uses KKM_SO_TADA_Export_14Nov25$)

    Sync order:
      1. MST_Region
      2. MST_Division
      3. MST_Category
      4. MST_Branch
      5. MST_Plant
      6. MST_Department
      7. MST_SubDepartment
      8. MST_Grade
      9. MST_Designation

    Orchestrator contract (usp_RunHRMSSynchronization):
      @BatchId, @ExecutedBy,
      @MastersInserted OUTPUT, @MastersUpdated OUTPUT

    Notes:
      - Nested-transaction safe: joins an ambient transaction when present;
        otherwise owns a local transaction.
      - Does not emit a client result set when nested (orchestrator Fill
        compatibility). Standalone calls return a per-master summary RS.
*/

CREATE OR ALTER PROCEDURE [dbo].[usp_HSE_SynchronizeMasters]
    @BatchId          INT,
    @ExecutedBy       NVARCHAR(128) = NULL,
    @MastersInserted  INT OUTPUT,
    @MastersUpdated   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @MastersInserted = 0;
    SET @MastersUpdated = 0;

    DECLARE @StartedTran BIT = 0;
    DECLARE @Rows INT = 0;

    /* Per-master counters (standalone result set + rollup) */
    DECLARE @RegionInserted        INT = 0, @RegionUpdated        INT = 0;
    DECLARE @DivisionInserted      INT = 0, @DivisionUpdated      INT = 0;
    DECLARE @CategoryInserted      INT = 0, @CategoryUpdated      INT = 0;
    DECLARE @BranchInserted        INT = 0, @BranchUpdated        INT = 0;
    DECLARE @PlantInserted         INT = 0, @PlantUpdated         INT = 0;
    DECLARE @DepartmentInserted    INT = 0, @DepartmentUpdated    INT = 0;
    DECLARE @SubDepartmentInserted INT = 0, @SubDepartmentUpdated INT = 0;
    DECLARE @GradeInserted         INT = 0, @GradeUpdated         INT = 0;
    DECLARE @DesignationInserted   INT = 0, @DesignationUpdated   INT = 0;

    IF @BatchId IS NULL OR @BatchId <= 0
    BEGIN
        RAISERROR(N'usp_HSE_SynchronizeMasters: @BatchId is required.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.ImportBatchMaster WHERE BatchId = @BatchId)
    BEGIN
        RAISERROR(N'usp_HSE_SynchronizeMasters: BatchId was not found in ImportBatchMaster.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Staging_EmployeeData WITH (NOLOCK)
        WHERE BatchId = @BatchId
    )
    BEGIN
        RAISERROR(N'usp_HSE_SynchronizeMasters: No staging records found for the specified BatchId.', 16, 1);
        RETURN;
    END;

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            BEGIN TRANSACTION;
            SET @StartedTran = 1;
        END;

        /* ==============================================================
           1) MST_Region  (Region_ID varchar, Region_Description varchar)
           ============================================================== */
        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.Region_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Region_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.Region_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.Region_ID))
        )
        UPDATE m
        SET m.Region_Description = LEFT(src.Descr, 50)
        FROM dbo.MST_Region AS m
        INNER JOIN src ON m.Region_ID = src.Code
        WHERE src.Descr IS NOT NULL
          AND ISNULL(m.Region_Description, N'') <> src.Descr;

        SET @Rows = @@ROWCOUNT;
        SET @RegionUpdated = @Rows;
        SET @MastersUpdated = @MastersUpdated + @Rows;

        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.Region_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Region_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.Region_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.Region_ID))
        )
        INSERT INTO dbo.MST_Region (Region_ID, Region_Description)
        SELECT src.Code, LEFT(src.Descr, 50)
        FROM src
        WHERE src.Descr IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.MST_Region AS m
                WHERE m.Region_ID = src.Code
              );

        SET @Rows = @@ROWCOUNT;
        SET @RegionInserted = @Rows;
        SET @MastersInserted = @MastersInserted + @Rows;

        /* ==============================================================
           2) MST_Division
           ============================================================== */
        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.Division_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Division_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.Division_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.Division_ID))
        )
        UPDATE m
        SET m.Division_Description = LEFT(src.Descr, 50)
        FROM dbo.MST_Division AS m
        INNER JOIN src ON CAST(m.Division_ID AS NVARCHAR(50)) = src.Code
        WHERE src.Descr IS NOT NULL
          AND ISNULL(m.Division_Description, N'') <> src.Descr;

        SET @Rows = @@ROWCOUNT;
        SET @DivisionUpdated = @Rows;
        SET @MastersUpdated = @MastersUpdated + @Rows;

        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.Division_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Division_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.Division_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.Division_ID))
        )
        INSERT INTO dbo.MST_Division (Division_ID, Division_Description)
        SELECT src.Code, LEFT(src.Descr, 50)
        FROM src
        WHERE src.Descr IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.MST_Division AS m
                WHERE CAST(m.Division_ID AS NVARCHAR(50)) = src.Code
              );

        SET @Rows = @@ROWCOUNT;
        SET @DivisionInserted = @Rows;
        SET @MastersInserted = @MastersInserted + @Rows;

        /* ==============================================================
           3) MST_Category
           ============================================================== */
        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.Category_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Category_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.Category_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.Category_ID))
        )
        UPDATE m
        SET m.Category_Description = LEFT(src.Descr, 50)
        FROM dbo.MST_Category AS m
        INNER JOIN src ON CAST(m.Category_ID AS NVARCHAR(50)) = src.Code
        WHERE src.Descr IS NOT NULL
          AND ISNULL(m.Category_Description, N'') <> src.Descr;

        SET @Rows = @@ROWCOUNT;
        SET @CategoryUpdated = @Rows;
        SET @MastersUpdated = @MastersUpdated + @Rows;

        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.Category_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Category_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.Category_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.Category_ID))
        )
        INSERT INTO dbo.MST_Category (Category_ID, Category_Description)
        SELECT src.Code, LEFT(src.Descr, 50)
        FROM src
        WHERE src.Descr IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.MST_Category AS m
                WHERE CAST(m.Category_ID AS NVARCHAR(50)) = src.Code
              );

        SET @Rows = @@ROWCOUNT;
        SET @CategoryInserted = @Rows;
        SET @MastersInserted = @MastersInserted + @Rows;

        /* ==============================================================
           4) MST_Branch  (Branch_ID int)
           ============================================================== */
        ;WITH src AS (
            SELECT
                Code  = TRY_CONVERT(INT, LTRIM(RTRIM(s.Branch_ID))),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Branch_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND TRY_CONVERT(INT, LTRIM(RTRIM(s.Branch_ID))) IS NOT NULL
            GROUP BY TRY_CONVERT(INT, LTRIM(RTRIM(s.Branch_ID)))
        )
        UPDATE m
        SET m.Branch_Description = LEFT(src.Descr, 50)
        FROM dbo.MST_Branch AS m
        INNER JOIN src ON m.Branch_ID = src.Code
        WHERE src.Descr IS NOT NULL
          AND ISNULL(m.Branch_Description, N'') <> src.Descr;

        SET @Rows = @@ROWCOUNT;
        SET @BranchUpdated = @Rows;
        SET @MastersUpdated = @MastersUpdated + @Rows;

        ;WITH src AS (
            SELECT
                Code  = TRY_CONVERT(INT, LTRIM(RTRIM(s.Branch_ID))),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Branch_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND TRY_CONVERT(INT, LTRIM(RTRIM(s.Branch_ID))) IS NOT NULL
            GROUP BY TRY_CONVERT(INT, LTRIM(RTRIM(s.Branch_ID)))
        )
        INSERT INTO dbo.MST_Branch (Branch_ID, Branch_Description)
        SELECT src.Code, LEFT(src.Descr, 50)
        FROM src
        WHERE src.Descr IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.MST_Branch AS m
                WHERE m.Branch_ID = src.Code
              );

        SET @Rows = @@ROWCOUNT;
        SET @BranchInserted = @Rows;
        SET @MastersInserted = @MastersInserted + @Rows;

        /* ==============================================================
           5) MST_Plant  (PlantID_ID / PlantID_Description)
           ============================================================== */
        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.PlantID_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.PlantID_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.PlantID_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.PlantID_ID))
        )
        UPDATE m
        SET m.PlantID_Description = LEFT(src.Descr, 50)
        FROM dbo.MST_Plant AS m
        INNER JOIN src ON CAST(m.PlantID_ID AS NVARCHAR(50)) = src.Code
        WHERE src.Descr IS NOT NULL
          AND ISNULL(m.PlantID_Description, N'') <> src.Descr;

        SET @Rows = @@ROWCOUNT;
        SET @PlantUpdated = @Rows;
        SET @MastersUpdated = @MastersUpdated + @Rows;

        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.PlantID_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.PlantID_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.PlantID_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.PlantID_ID))
        )
        INSERT INTO dbo.MST_Plant (PlantID_ID, PlantID_Description)
        SELECT src.Code, LEFT(src.Descr, 50)
        FROM src
        WHERE src.Descr IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.MST_Plant AS m
                WHERE CAST(m.PlantID_ID AS NVARCHAR(50)) = src.Code
              );

        SET @Rows = @@ROWCOUNT;
        SET @PlantInserted = @Rows;
        SET @MastersInserted = @MastersInserted + @Rows;

        /* ==============================================================
           6) MST_Department
           ============================================================== */
        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.Department_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Department_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.Department_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.Department_ID))
        )
        UPDATE m
        SET m.Department_Description = LEFT(src.Descr, 50)
        FROM dbo.MST_Department AS m
        INNER JOIN src ON m.Department_ID = src.Code
        WHERE src.Descr IS NOT NULL
          AND ISNULL(m.Department_Description, N'') <> src.Descr;

        SET @Rows = @@ROWCOUNT;
        SET @DepartmentUpdated = @Rows;
        SET @MastersUpdated = @MastersUpdated + @Rows;

        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.Department_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Department_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.Department_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.Department_ID))
        )
        INSERT INTO dbo.MST_Department (Department_ID, Department_Description)
        SELECT src.Code, LEFT(src.Descr, 50)
        FROM src
        WHERE src.Descr IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.MST_Department AS m
                WHERE m.Department_ID = src.Code
              );

        SET @Rows = @@ROWCOUNT;
        SET @DepartmentInserted = @Rows;
        SET @MastersInserted = @MastersInserted + @Rows;

        /* ==============================================================
           7) MST_SubDepartment
              Uses Staging_EmployeeData.SubDepartment_ID +
              SubDepartment_Description (schema-aligned with other masters).
              Insert/update only when description is non-empty.
           ============================================================== */
        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.SubDepartment_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.SubDepartment_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.SubDepartment_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.SubDepartment_ID))
        )
        UPDATE m
        SET m.SubDepartment_Description = LEFT(src.Descr, 50)
        FROM dbo.MST_SubDepartment AS m
        INNER JOIN src ON CAST(m.SubDepartment_ID AS NVARCHAR(50)) = src.Code
        WHERE src.Descr IS NOT NULL
          AND ISNULL(m.SubDepartment_Description, N'') <> src.Descr;

        SET @Rows = @@ROWCOUNT;
        SET @SubDepartmentUpdated = @Rows;
        SET @MastersUpdated = @MastersUpdated + @Rows;

        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.SubDepartment_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.SubDepartment_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.SubDepartment_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.SubDepartment_ID))
        )
        INSERT INTO dbo.MST_SubDepartment (SubDepartment_ID, SubDepartment_Description)
        SELECT src.Code, LEFT(src.Descr, 50)
        FROM src
        WHERE src.Descr IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.MST_SubDepartment AS m
                WHERE CAST(m.SubDepartment_ID AS NVARCHAR(50)) = src.Code
              );

        SET @Rows = @@ROWCOUNT;
        SET @SubDepartmentInserted = @Rows;
        SET @MastersInserted = @MastersInserted + @Rows;

        /* ==============================================================
           8) MST_Grade
           ============================================================== */
        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.Grade_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Grade_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.Grade_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.Grade_ID))
        )
        UPDATE m
        SET m.Grade_Description = LEFT(src.Descr, 50)
        FROM dbo.MST_Grade AS m
        INNER JOIN src ON m.Grade_ID = src.Code
        WHERE src.Descr IS NOT NULL
          AND ISNULL(m.Grade_Description, N'') <> src.Descr;

        SET @Rows = @@ROWCOUNT;
        SET @GradeUpdated = @Rows;
        SET @MastersUpdated = @MastersUpdated + @Rows;

        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.Grade_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Grade_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.Grade_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.Grade_ID))
        )
        INSERT INTO dbo.MST_Grade (Grade_ID, Grade_Description)
        SELECT src.Code, LEFT(src.Descr, 50)
        FROM src
        WHERE src.Descr IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.MST_Grade AS m
                WHERE m.Grade_ID = src.Code
              );

        SET @Rows = @@ROWCOUNT;
        SET @GradeInserted = @Rows;
        SET @MastersInserted = @MastersInserted + @Rows;

        /* ==============================================================
           9) MST_Designation
              Business key = Designation_ID (varchar code), NOT Id PK.
              Preserves chain:
                Staging.Designation_ID → MST_Designation.Designation_ID
                → MST_Designation.Id → MST_UserMaster.Designation_ID
           ============================================================== */
        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.Designation_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Designation_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.Designation_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.Designation_ID))
        )
        UPDATE m
        SET m.Designation_Description = LEFT(src.Descr, 50)
        FROM dbo.MST_Designation AS m
        INNER JOIN src ON m.Designation_ID = src.Code
        WHERE src.Descr IS NOT NULL
          AND ISNULL(m.Designation_Description, N'') <> src.Descr;

        SET @Rows = @@ROWCOUNT;
        SET @DesignationUpdated = @Rows;
        SET @MastersUpdated = @MastersUpdated + @Rows;

        ;WITH src AS (
            SELECT
                Code  = LTRIM(RTRIM(s.Designation_ID)),
                Descr = MAX(NULLIF(LTRIM(RTRIM(s.Designation_Description)), N''))
            FROM dbo.Staging_EmployeeData AS s
            WHERE s.BatchId = @BatchId
              AND NULLIF(LTRIM(RTRIM(s.Designation_ID)), N'') IS NOT NULL
            GROUP BY LTRIM(RTRIM(s.Designation_ID))
        )
        INSERT INTO dbo.MST_Designation (Designation_ID, Designation_Description)
        SELECT src.Code, LEFT(src.Descr, 50)
        FROM src
        WHERE src.Descr IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.MST_Designation AS m
                WHERE m.Designation_ID = src.Code
              );

        SET @Rows = @@ROWCOUNT;
        SET @DesignationInserted = @Rows;
        SET @MastersInserted = @MastersInserted + @Rows;

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
       Standalone result set (suppressed when nested under orchestrator)
       ------------------------------------------------------------------ */
    IF @StartedTran = 1
    BEGIN
        SELECT
            BatchId            = @BatchId,
            MasterTable        = v.MasterTable,
            InsertedCount      = v.InsertedCount,
            UpdatedCount       = v.UpdatedCount,
            ExecutedBy         = @ExecutedBy
        FROM (VALUES
            (N'MST_Region',        @RegionInserted,        @RegionUpdated),
            (N'MST_Division',      @DivisionInserted,      @DivisionUpdated),
            (N'MST_Category',      @CategoryInserted,      @CategoryUpdated),
            (N'MST_Branch',        @BranchInserted,        @BranchUpdated),
            (N'MST_Plant',         @PlantInserted,         @PlantUpdated),
            (N'MST_Department',    @DepartmentInserted,    @DepartmentUpdated),
            (N'MST_SubDepartment', @SubDepartmentInserted, @SubDepartmentUpdated),
            (N'MST_Grade',         @GradeInserted,         @GradeUpdated),
            (N'MST_Designation',   @DesignationInserted,   @DesignationUpdated)
        ) AS v(MasterTable, InsertedCount, UpdatedCount)
        ORDER BY
            CASE v.MasterTable
                WHEN N'MST_Region' THEN 1
                WHEN N'MST_Division' THEN 2
                WHEN N'MST_Category' THEN 3
                WHEN N'MST_Branch' THEN 4
                WHEN N'MST_Plant' THEN 5
                WHEN N'MST_Department' THEN 6
                WHEN N'MST_SubDepartment' THEN 7
                WHEN N'MST_Grade' THEN 8
                WHEN N'MST_Designation' THEN 9
                ELSE 99
            END;
    END;
END
GO
