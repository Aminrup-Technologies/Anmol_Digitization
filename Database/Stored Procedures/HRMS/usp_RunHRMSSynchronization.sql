/*
    Object:      [dbo].[usp_RunHRMSSynchronization]
    Type:        STORED PROCEDURE (ORCHESTRATOR ONLY)
    Phase:       3.4 — HRMS Synchronization Engine
    Purpose:     Coordinate modular HSE procedures. Contains no business
                 insert/update/master/hierarchy logic.

    Call order (module / mode gated):
      1. usp_HSE_SynchronizeMasters
      2. usp_HSE_InsertNewEmployees
      3. usp_HSE_UpdateChangedEmployees
      4. usp_HSE_UpdateReportingHierarchy

    Notes:
      - Pre-sync validation is performed by the UI via usp_HSE_ValidateBatch
        (Phase 3.3). This orchestrator does not re-run validation.
      - Child HSE sync procedures must be deployed before this procedure
        can execute successfully at runtime.
      - Source data for children: dbo.Staging_EmployeeData WHERE BatchId=@BatchId
      - Never references KKM_SO_TADA_Export_14Nov25$
*/

CREATE OR ALTER PROCEDURE [dbo].[usp_RunHRMSSynchronization]
    @BatchId         INT,
    @AllowOverwrite  BIT            = 0,
    @ProcessingMode  INT            = 0,
    @Modules         NVARCHAR(200)  = NULL,
    @OverwriteFields NVARCHAR(MAX)  = NULL,
    @ExecutedBy      NVARCHAR(128)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @RunMaster    BIT = 0;
    DECLARE @RunEmployee  BIT = 0;
    DECLARE @RunHierarchy BIT = 0;

    DECLARE @TotalRecords      INT = 0;
    DECLARE @InsertedCount     INT = 0;
    DECLARE @UpdatedCount      INT = 0;
    DECLARE @SkippedCount      INT = 0;
    DECLARE @ErrorCount        INT = 0;
    DECLARE @MastersInserted   INT = 0;
    DECLARE @MastersUpdated    INT = 0;
    DECLARE @HierarchyUpdated  INT = 0;

    DECLARE @StepMastersInserted  INT = 0;
    DECLARE @StepMastersUpdated   INT = 0;
    DECLARE @StepInserted         INT = 0;
    DECLARE @StepUpdated          INT = 0;
    DECLARE @StepSkipped          INT = 0;
    DECLARE @StepErrors           INT = 0;
    DECLARE @StepHierarchyUpdated INT = 0;

    DECLARE @BatchStatus NVARCHAR(50) = N'Failed';
    DECLARE @Message     NVARCHAR(500) = N'';
    DECLARE @ModulesNorm NVARCHAR(200);

    /* ------------------------------------------------------------------
       Guard: batch must exist (lightweight; full validation is UI-side)
       ------------------------------------------------------------------ */
    IF @BatchId IS NULL OR @BatchId <= 0
    BEGIN
        RAISERROR(N'usp_RunHRMSSynchronization: @BatchId is required.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.ImportBatchMaster WHERE BatchId = @BatchId)
    BEGIN
        RAISERROR(N'usp_RunHRMSSynchronization: BatchId was not found in ImportBatchMaster.', 16, 1);
        RETURN;
    END;

    SELECT @TotalRecords = COUNT(1)
    FROM dbo.Staging_EmployeeData WITH (NOLOCK)
    WHERE BatchId = @BatchId;

    /* ------------------------------------------------------------------
       Parse Modules (coordination only)
       ------------------------------------------------------------------ */
    SET @ModulesNorm = UPPER(REPLACE(LTRIM(RTRIM(ISNULL(@Modules, N''))), N' ', N''));

    IF @ModulesNorm LIKE N'%MASTER%'
        SET @RunMaster = 1;
    IF @ModulesNorm LIKE N'%EMPLOYEE%'
        SET @RunEmployee = 1;
    IF @ModulesNorm LIKE N'%HIERARCHY%'
        SET @RunHierarchy = 1;

    IF @RunMaster = 0 AND @RunEmployee = 0 AND @RunHierarchy = 0
    BEGIN
        RAISERROR(N'usp_RunHRMSSynchronization: @Modules must include MASTER, EMPLOYEE, and/or HIERARCHY.', 16, 1);
        RETURN;
    END;

    IF @ProcessingMode NOT IN (0, 1, 2)
    BEGIN
        RAISERROR(N'usp_RunHRMSSynchronization: @ProcessingMode must be 0, 1, or 2.', 16, 1);
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        /* Mark batch as in-progress for operators observing ImportBatchMaster */
        UPDATE dbo.ImportBatchMaster
        SET Status = N'Processing'
        WHERE BatchId = @BatchId;

        /* ==============================================================
           1) Synchronize Masters
           ============================================================== */
        IF @RunMaster = 1
        BEGIN
            SET @StepMastersInserted = 0;
            SET @StepMastersUpdated = 0;

            EXEC dbo.usp_HSE_SynchronizeMasters
                @BatchId = @BatchId,
                @ExecutedBy = @ExecutedBy,
                @MastersInserted = @StepMastersInserted OUTPUT,
                @MastersUpdated = @StepMastersUpdated OUTPUT;

            SET @MastersInserted = ISNULL(@StepMastersInserted, 0);
            SET @MastersUpdated = ISNULL(@StepMastersUpdated, 0);
        END;

        /* ==============================================================
           2) Insert New Employees
              Mode 0: always when EMPLOYEE selected.
              Mode 1/2: also insert brand-new EmpCodes so update runs
              do not skip missing employees (coordination gate; insert
              predicates remain inside the child procedure).
           ============================================================== */
        IF @RunEmployee = 1 AND @ProcessingMode IN (0, 1, 2)
        BEGIN
            /* Mode 0 = insert-only path; Mode 1/2 still allow new inserts */
            IF @ProcessingMode = 0 OR @ProcessingMode IN (1, 2)
            BEGIN
                SET @StepInserted = 0;
                SET @StepSkipped = 0;
                SET @StepErrors = 0;

                EXEC dbo.usp_HSE_InsertNewEmployees
                    @BatchId = @BatchId,
                    @ExecutedBy = @ExecutedBy,
                    @InsertedCount = @StepInserted OUTPUT,
                    @SkippedCount = @StepSkipped OUTPUT,
                    @ErrorCount = @StepErrors OUTPUT;

                SET @InsertedCount = @InsertedCount + ISNULL(@StepInserted, 0);
                SET @SkippedCount = @SkippedCount + ISNULL(@StepSkipped, 0);
                SET @ErrorCount = @ErrorCount + ISNULL(@StepErrors, 0);
            END;
        END;

        /* ==============================================================
           3) Update Changed Employees (modes 1 and 2 only)
           ============================================================== */
        IF @RunEmployee = 1 AND @ProcessingMode IN (1, 2)
        BEGIN
            SET @StepUpdated = 0;
            SET @StepSkipped = 0;
            SET @StepErrors = 0;

            EXEC dbo.usp_HSE_UpdateChangedEmployees
                @BatchId = @BatchId,
                @AllowOverwrite = @AllowOverwrite,
                @ProcessingMode = @ProcessingMode,
                @OverwriteFields = @OverwriteFields,
                @ExecutedBy = @ExecutedBy,
                @UpdatedCount = @StepUpdated OUTPUT,
                @SkippedCount = @StepSkipped OUTPUT,
                @ErrorCount = @StepErrors OUTPUT;

            SET @UpdatedCount = @UpdatedCount + ISNULL(@StepUpdated, 0);
            SET @SkippedCount = @SkippedCount + ISNULL(@StepSkipped, 0);
            SET @ErrorCount = @ErrorCount + ISNULL(@StepErrors, 0);
        END;

        /* ==============================================================
           4) Update Reporting Hierarchy
           ============================================================== */
        IF @RunHierarchy = 1
        BEGIN
            SET @StepHierarchyUpdated = 0;
            SET @StepErrors = 0;

            EXEC dbo.usp_HSE_UpdateReportingHierarchy
                @BatchId = @BatchId,
                @ExecutedBy = @ExecutedBy,
                @HierarchyUpdatedCount = @StepHierarchyUpdated OUTPUT,
                @ErrorCount = @StepErrors OUTPUT;

            SET @HierarchyUpdated = ISNULL(@StepHierarchyUpdated, 0);
            SET @ErrorCount = @ErrorCount + ISNULL(@StepErrors, 0);
        END;

        /* ------------------------------------------------------------------
           Mark staging rows appropriately (finalization only)
           - Pending + NULL / '' ErrorMessage → Processed
           - Pending + ErrorMessage starting with [WARNING] → Processed
             (warnings are informational; text is retained)
           - ImportStatus = Error (hard failures) remains Error
           ------------------------------------------------------------------ */
        UPDATE s
        SET s.ImportStatus = N'Processed'
        FROM dbo.Staging_EmployeeData AS s
        WHERE s.BatchId = @BatchId
          AND s.ImportStatus = N'Pending'
          AND (
                s.ErrorMessage IS NULL
             OR LTRIM(RTRIM(s.ErrorMessage)) = N''
             OR LTRIM(s.ErrorMessage) LIKE N'[[]WARNING]%'
              );

        /* ------------------------------------------------------------------
           Update ImportBatchMaster status
           ------------------------------------------------------------------ */
        IF @ErrorCount > 0 AND (@InsertedCount > 0 OR @UpdatedCount > 0 OR @HierarchyUpdated > 0 OR @MastersInserted > 0 OR @MastersUpdated > 0)
            SET @BatchStatus = N'Processed'; /* partial success still processed; errors visible on staging */
        ELSE IF @ErrorCount > 0
            SET @BatchStatus = N'Failed';
        ELSE
            SET @BatchStatus = N'Processed';

        UPDATE dbo.ImportBatchMaster
        SET Status = @BatchStatus
        WHERE BatchId = @BatchId;

        SET @Message = N'Synchronization completed.';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        /* Best-effort status update outside the rolled-back work */
        BEGIN TRY
            UPDATE dbo.ImportBatchMaster
            SET Status = N'Failed'
            WHERE BatchId = @BatchId;
        END TRY
        BEGIN CATCH
            /* ignore secondary failure */
        END CATCH;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrState INT = ERROR_STATE();

        RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
        RETURN;
    END CATCH;

    /* ------------------------------------------------------------------
       Return contract for HrmsBatchManager (first/only result set)
       SqlDataAdapter.Fill binds this table to gvResult.
       ------------------------------------------------------------------ */
    SELECT
        BatchId            = @BatchId,
        TotalRecords       = @TotalRecords,
        InsertedCount      = @InsertedCount,
        UpdatedCount       = @UpdatedCount,
        SkippedCount       = @SkippedCount,
        ErrorCount         = @ErrorCount,
        MastersInserted    = @MastersInserted,
        MastersUpdated     = @MastersUpdated,
        HierarchyUpdated   = @HierarchyUpdated,
        Status             = @BatchStatus,
        Message            = @Message;
END
GO
