/*
    Object:      [dbo].[usp_RunHRMSSynchronization]
    Type:        STORED PROCEDURE (ORCHESTRATOR ONLY)
    Phase:       3.4 — HRMS Synchronization Engine
                 4.1 — Warning finalization (ORCH-02)
                 4.2 — Summary KPI refinement (ORCH-01 / ORCH-04)
                 4.3 — Transaction / error-handling hardening
                 5.1 — Validation warning reconciliation (after masters)
    Purpose:     Coordinate modular HSE procedures. Contains no business
                 insert/update/master/hierarchy logic.

    Call order (module / mode gated):
      1. usp_HSE_SynchronizeMasters
      1b. usp_HSE_ReconcileValidationWarnings  (after masters when MASTER runs)
      2. usp_HSE_InsertNewEmployees
      3. usp_HSE_UpdateChangedEmployees
      4. usp_HSE_UpdateReportingHierarchy

    Notes:
      - Pre-sync validation is performed by the UI via usp_HSE_ValidateBatch
        (Phase 3.3). This orchestrator does not re-run validation.
      - After SynchronizeMasters, obsolete "expected to be created by
        SynchronizeMasters" warnings are reconciled (reporting only).
      - Child HSE sync procedures must be deployed before this procedure
        can execute successfully at runtime.
      - Source data for children: dbo.Staging_EmployeeData WHERE BatchId=@BatchId
      - Never references KKM_SO_TADA_Export_14Nov25$

    Summary result set (Phase 4.2 — first/only RS for HrmsBatchManager):
      BatchId, TotalRecords,
      MastersInserted, MastersUpdated,
      EmployeesInserted, EmployeesUpdated,
      ExistingEmployees,   -- Insert.SkippedCount (already in MST_UserMaster)
      NoChangeCount,       -- Update.NoChangeCount (compared, no field change)
      HierarchyUpdated,
      MissingMasterCount,  -- Insert.MissingMaster + Update.MissingMaster
      MissingManagerCount, -- Hierarchy.MissingManager
      ErrorCount,          -- soft-failure rollup (= Missing* when children map Error→Missing*)
      Status, Message

    Old → new KPI mapping:
      InsertedCount  → EmployeesInserted
      UpdatedCount   → EmployeesUpdated
      SkippedCount   → retired; Insert.Skipped → ExistingEmployees;
                       Update.NoChangeCount → NoChangeCount
                       (do not re-sum into one column)
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

    /* ---- KPI accumulators (summary result set) ---- */
    DECLARE @TotalRecords        INT = 0;
    DECLARE @MastersInserted     INT = 0;
    DECLARE @MastersUpdated      INT = 0;
    DECLARE @EmployeesInserted   INT = 0;  -- was InsertedCount
    DECLARE @EmployeesUpdated    INT = 0;  -- was UpdatedCount
    DECLARE @ExistingEmployees   INT = 0;  -- Insert.SkippedCount (already present)
    DECLARE @NoChangeCount       INT = 0;  -- Update.NoChangeCount
    DECLARE @HierarchyUpdated    INT = 0;
    DECLARE @MissingMasterCount  INT = 0;  -- Insert + Update MissingMaster
    DECLARE @MissingManagerCount INT = 0;  -- Hierarchy MissingManager
    DECLARE @ErrorCount          INT = 0;  -- soft-failure rollup from child @ErrorCount

    /* Step-local OUTPUT captures */
    DECLARE @StepMastersInserted       INT = 0;
    DECLARE @StepMastersUpdated        INT = 0;
    DECLARE @StepInserted              INT = 0;
    DECLARE @StepInsertSkipped         INT = 0;
    DECLARE @StepInsertErrors          INT = 0;
    DECLARE @StepInsertMissingMaster   INT = 0;
    DECLARE @StepUpdated               INT = 0;
    DECLARE @StepUpdateSkipped         INT = 0;  -- Update.SkippedCount (not-in-master); not summed into ExistingEmployees
    DECLARE @StepUpdateErrors          INT = 0;
    DECLARE @StepUpdateNoChange        INT = 0;
    DECLARE @StepUpdateMissingMaster   INT = 0;
    DECLARE @StepHierarchyUpdated      INT = 0;
    DECLARE @StepHierarchyErrors       INT = 0;
    DECLARE @StepMissingManager        INT = 0;

    DECLARE @BatchStatus NVARCHAR(50) = N'Failed';
    DECLARE @Message     NVARCHAR(500) = N'';
    DECLARE @ModulesNorm NVARCHAR(200);
    /* 1 = this procedure opened the ambient transaction; 0 = joined caller tran */
    DECLARE @StartedTran BIT = 0;

    /* ------------------------------------------------------------------
       Guard: batch must exist (lightweight; full validation is UI-side)
       Pre-TRY application guards use THROW (not RAISERROR) so execution
       stops immediately. Custom error numbers 50001–50004 are intentional
       application messages (not re-throws of caught SQL errors).
       ------------------------------------------------------------------ */
    IF @BatchId IS NULL OR @BatchId <= 0
        THROW 50001, N'usp_RunHRMSSynchronization: @BatchId is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.ImportBatchMaster WHERE BatchId = @BatchId)
        THROW 50002, N'usp_RunHRMSSynchronization: BatchId was not found in ImportBatchMaster.', 1;

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
        THROW 50003, N'usp_RunHRMSSynchronization: @Modules must include MASTER, EMPLOYEE, and/or HIERARCHY.', 1;

    IF @ProcessingMode NOT IN (0, 1, 2)
        THROW 50004, N'usp_RunHRMSSynchronization: @ProcessingMode must be 0, 1, or 2.', 1;

    BEGIN TRY
        /* Nested-safe: only open a transaction when none is active.
           Children already join ambient (@@TRANCOUNT > 0 → no child BEGIN TRAN). */
        IF @@TRANCOUNT = 0
        BEGIN
            BEGIN TRANSACTION;
            SET @StartedTran = 1;
        END;

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

            /* 1b) Clear obsolete ValidateBatch "expected to be created"
                  warnings now that masters have been synchronized.
                  Reporting accuracy only — no validation / FK / sync changes. */
            EXEC dbo.usp_HSE_ReconcileValidationWarnings
                @BatchId = @BatchId;
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
                SET @StepInsertSkipped = 0;
                SET @StepInsertErrors = 0;
                SET @StepInsertMissingMaster = 0;

                EXEC dbo.usp_HSE_InsertNewEmployees
                    @BatchId = @BatchId,
                    @ExecutedBy = @ExecutedBy,
                    @InsertedCount = @StepInserted OUTPUT,
                    @SkippedCount = @StepInsertSkipped OUTPUT,
                    @ErrorCount = @StepInsertErrors OUTPUT,
                    @MissingMasterCount = @StepInsertMissingMaster OUTPUT;

                SET @EmployeesInserted = @EmployeesInserted + ISNULL(@StepInserted, 0);
                /* ExistingEmployees ← Insert.SkippedCount only (already in MST_UserMaster) */
                SET @ExistingEmployees = @ExistingEmployees + ISNULL(@StepInsertSkipped, 0);
                SET @MissingMasterCount = @MissingMasterCount + ISNULL(@StepInsertMissingMaster, 0);
                SET @ErrorCount = @ErrorCount + ISNULL(@StepInsertErrors, 0);
            END;
        END;

        /* ==============================================================
           3) Update Changed Employees (modes 1 and 2 only)
           ============================================================== */
        IF @RunEmployee = 1 AND @ProcessingMode IN (1, 2)
        BEGIN
            SET @StepUpdated = 0;
            SET @StepUpdateSkipped = 0;
            SET @StepUpdateErrors = 0;
            SET @StepUpdateNoChange = 0;
            SET @StepUpdateMissingMaster = 0;

            EXEC dbo.usp_HSE_UpdateChangedEmployees
                @BatchId = @BatchId,
                @AllowOverwrite = @AllowOverwrite,
                @ProcessingMode = @ProcessingMode,
                @OverwriteFields = @OverwriteFields,
                @ExecutedBy = @ExecutedBy,
                @UpdatedCount = @StepUpdated OUTPUT,
                @SkippedCount = @StepUpdateSkipped OUTPUT,
                @ErrorCount = @StepUpdateErrors OUTPUT,
                @NoChangeCount = @StepUpdateNoChange OUTPUT,
                @MissingMasterCount = @StepUpdateMissingMaster OUTPUT;

            SET @EmployeesUpdated = @EmployeesUpdated + ISNULL(@StepUpdated, 0);
            /* NoChangeCount ← Update.NoChangeCount (do NOT use Update.SkippedCount) */
            SET @NoChangeCount = @NoChangeCount + ISNULL(@StepUpdateNoChange, 0);
            SET @MissingMasterCount = @MissingMasterCount + ISNULL(@StepUpdateMissingMaster, 0);
            SET @ErrorCount = @ErrorCount + ISNULL(@StepUpdateErrors, 0);
            /* @StepUpdateSkipped retained for observability only — not mixed into ExistingEmployees */
        END;

        /* ==============================================================
           4) Update Reporting Hierarchy
           ============================================================== */
        IF @RunHierarchy = 1
        BEGIN
            SET @StepHierarchyUpdated = 0;
            SET @StepHierarchyErrors = 0;
            SET @StepMissingManager = 0;

            EXEC dbo.usp_HSE_UpdateReportingHierarchy
                @BatchId = @BatchId,
                @ExecutedBy = @ExecutedBy,
                @HierarchyUpdatedCount = @StepHierarchyUpdated OUTPUT,
                @ErrorCount = @StepHierarchyErrors OUTPUT,
                @MissingManagerCount = @StepMissingManager OUTPUT;

            SET @HierarchyUpdated = ISNULL(@StepHierarchyUpdated, 0);
            SET @MissingManagerCount = ISNULL(@StepMissingManager, 0);
            SET @ErrorCount = @ErrorCount + ISNULL(@StepHierarchyErrors, 0);
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
        IF @ErrorCount > 0 AND (@EmployeesInserted > 0 OR @EmployeesUpdated > 0 OR @HierarchyUpdated > 0 OR @MastersInserted > 0 OR @MastersUpdated > 0)
            SET @BatchStatus = N'Processed'; /* partial success still processed; errors visible on staging */
        ELSE IF @ErrorCount > 0
            SET @BatchStatus = N'Failed';
        ELSE
            SET @BatchStatus = N'Processed';

        UPDATE dbo.ImportBatchMaster
        SET Status = @BatchStatus
        WHERE BatchId = @BatchId;

        SET @Message = N'Synchronization completed.';

        IF @StartedTran = 1
            COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        /*
           Rollback strategy (Phase 4.3):
             XACT_STATE() = -1 → uncommittable; ROLLBACK required
             XACT_STATE() =  1 → committable but failed; ROLLBACK only if we own the tran
             XACT_STATE() =  0 → no transaction; do not ROLLBACK

           No savepoints: a hard failure undoes all child DML in this run
           (partial-commit prevention by design).
        */
        IF XACT_STATE() = -1
            ROLLBACK TRANSACTION;
        ELSE IF XACT_STATE() = 1 AND @StartedTran = 1
            ROLLBACK TRANSACTION;

        /* Persist Failed after rollback so the stamp is not undone.
           Does not touch Validation Failed (validation runs in UI before sync
           and never enters this CATCH). */
        BEGIN TRY
            UPDATE dbo.ImportBatchMaster
            SET Status = N'Failed'
            WHERE BatchId = @BatchId;
        END TRY
        BEGIN CATCH
            /* ignore secondary failure — primary error is re-thrown below */
        END CATCH;

        /* Preserve original error number, severity, state, and line */
        THROW;
    END CATCH;

    /* ------------------------------------------------------------------
       Return contract for HrmsBatchManager (first/only result set)
       SqlDataAdapter.Fill binds this table to gvResult (AutoGenerateColumns).
       ------------------------------------------------------------------ */
    SELECT
        BatchId             = @BatchId,
        TotalRecords        = @TotalRecords,
        MastersInserted     = @MastersInserted,
        MastersUpdated      = @MastersUpdated,
        EmployeesInserted   = @EmployeesInserted,
        EmployeesUpdated    = @EmployeesUpdated,
        ExistingEmployees   = @ExistingEmployees,
        NoChangeCount       = @NoChangeCount,
        HierarchyUpdated    = @HierarchyUpdated,
        MissingMasterCount  = @MissingMasterCount,
        MissingManagerCount = @MissingManagerCount,
        ErrorCount          = @ErrorCount,
        Status              = @BatchStatus,
        Message             = @Message;
END
GO
