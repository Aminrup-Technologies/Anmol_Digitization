/*
    Object:      [dbo].[usp_RunHRMSSynchronization]
    Type:        STORED PROCEDURE (ORCHESTRATOR ONLY)
    Phase:       3.4 — HRMS Synchronization Engine
                 4.1 — Warning finalization (ORCH-02)
                 4.2 — Summary KPI refinement (ORCH-01 / ORCH-04)
                 4.3 — Transaction / error-handling hardening
                 5.1 — Validation warning reconciliation (after masters)
                 Phase A — Isolated single-action execution

    Purpose:     Coordinate modular HSE procedures. Contains no business
                 insert/update/master/hierarchy logic.

    Phase A contract:
      Exactly ONE @Action per call. Multi-module combined runs are rejected.
      Insert / Update / Hierarchy never run together.
      Update never inserts new employees.
      Employee actions require ImportBatchMaster.MasterSyncCompleted = 1.

    @Action (required):
      MASTER    → usp_HSE_SynchronizeMasters
                  + usp_HSE_ReconcileValidationWarnings
                  (does NOT finalize staging rows as Processed)
      INSERT    → usp_HSE_InsertNewEmployees only
      UPDATE    → usp_HSE_UpdateChangedEmployees only (mode 2 / selected fields)
      HIERARCHY → usp_HSE_UpdateReportingHierarchy only

    Notes:
      - Pre-sync validation is performed by the UI via usp_HSE_ValidateBatch.
        This orchestrator does not re-run validation.
      - Source data for children: dbo.Staging_EmployeeData WHERE BatchId=@BatchId
      - Never references KKM_SO_TADA_Export_14Nov25$
      - @Modules is accepted for call-site compatibility but is NOT used to
        select children. Execution is driven solely by @Action.
      - @ProcessingMode / @AllowOverwrite are forced from @Action so a stale
        or tampered client cannot restore combined insert+update behaviour.

    Summary result set (first/only RS for HrmsBatchManager):
      BatchId, Action, TotalRecords,
      MastersInserted, MastersUpdated,
      EmployeesInserted, EmployeesUpdated,
      ExistingEmployees, NoChangeCount, HierarchyUpdated,
      MissingMasterCount, MissingManagerCount, ErrorCount,
      Status, Message
*/

CREATE OR ALTER PROCEDURE [dbo].[usp_RunHRMSSynchronization]
    @BatchId         INT,
    @AllowOverwrite  BIT            = 0,
    @ProcessingMode  INT            = 0,
    @Modules         NVARCHAR(200)  = NULL,
    @OverwriteFields NVARCHAR(MAX)  = NULL,
    @ExecutedBy      NVARCHAR(128)  = NULL,
    @Action          NVARCHAR(20)   = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /* ---- KPI accumulators (summary result set) ---- */
    DECLARE @TotalRecords        INT = 0;
    DECLARE @MastersInserted     INT = 0;
    DECLARE @MastersUpdated      INT = 0;
    DECLARE @EmployeesInserted   INT = 0;
    DECLARE @EmployeesUpdated    INT = 0;
    DECLARE @ExistingEmployees   INT = 0;
    DECLARE @NoChangeCount       INT = 0;
    DECLARE @HierarchyUpdated    INT = 0;
    DECLARE @MissingMasterCount  INT = 0;
    DECLARE @MissingManagerCount INT = 0;
    DECLARE @ErrorCount          INT = 0;

    /* Step-local OUTPUT captures */
    DECLARE @StepMastersInserted       INT = 0;
    DECLARE @StepMastersUpdated        INT = 0;
    DECLARE @StepInserted              INT = 0;
    DECLARE @StepInsertSkipped         INT = 0;
    DECLARE @StepInsertErrors          INT = 0;
    DECLARE @StepInsertMissingMaster   INT = 0;
    DECLARE @StepUpdated               INT = 0;
    DECLARE @StepUpdateSkipped         INT = 0;
    DECLARE @StepUpdateErrors          INT = 0;
    DECLARE @StepUpdateNoChange        INT = 0;
    DECLARE @StepUpdateMissingMaster   INT = 0;
    DECLARE @StepHierarchyUpdated      INT = 0;
    DECLARE @StepHierarchyErrors       INT = 0;
    DECLARE @StepMissingManager        INT = 0;

    DECLARE @BatchStatus NVARCHAR(50) = N'Failed';
    DECLARE @Message     NVARCHAR(500) = N'';
    DECLARE @ActionNorm  NVARCHAR(20);
    DECLARE @MasterSynced BIT = 0;
    /* 1 = this procedure opened the ambient transaction; 0 = joined caller tran */
    DECLARE @StartedTran BIT = 0;

    /* ------------------------------------------------------------------
       Guard: batch must exist (lightweight; full validation is UI-side)
       Pre-TRY application guards use THROW so execution stops immediately.
       ------------------------------------------------------------------ */
    IF @BatchId IS NULL OR @BatchId <= 0
        THROW 50001, N'usp_RunHRMSSynchronization: @BatchId is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.ImportBatchMaster WHERE BatchId = @BatchId)
        THROW 50002, N'usp_RunHRMSSynchronization: BatchId was not found in ImportBatchMaster.', 1;

    SET @ActionNorm = UPPER(REPLACE(LTRIM(RTRIM(ISNULL(@Action, N''))), N' ', N''));

    IF @ActionNorm NOT IN (N'MASTER', N'INSERT', N'UPDATE', N'HIERARCHY')
        THROW 50003, N'usp_RunHRMSSynchronization: @Action must be MASTER, INSERT, UPDATE, or HIERARCHY.', 1;

    IF COL_LENGTH(N'dbo.ImportBatchMaster', N'MasterSyncCompleted') IS NULL
        THROW 50006, N'usp_RunHRMSSynchronization: ImportBatchMaster.MasterSyncCompleted is missing. Apply script 011_HRMS_ImportBatchMaster_MasterSyncCompleted.sql.', 1;

    SELECT @TotalRecords = COUNT(1)
    FROM dbo.Staging_EmployeeData WITH (NOLOCK)
    WHERE BatchId = @BatchId;

    SELECT @MasterSynced = ISNULL(MasterSyncCompleted, 0)
    FROM dbo.ImportBatchMaster
    WHERE BatchId = @BatchId;

    /* Employee actions require a durable master-sync success for THIS batch. */
    IF @ActionNorm IN (N'INSERT', N'UPDATE', N'HIERARCHY') AND ISNULL(@MasterSynced, 0) = 0
        THROW 50005, N'usp_RunHRMSSynchronization: Master synchronization must complete successfully for this batch before employee actions.', 1;

    IF @ActionNorm = N'UPDATE'
       AND (
            @OverwriteFields IS NULL
         OR LTRIM(RTRIM(@OverwriteFields)) = N''
           )
        THROW 50007, N'usp_RunHRMSSynchronization: @OverwriteFields is required for UPDATE. Select at least one field.', 1;

    /* Force isolated semantics from @Action. Ignore caller mode/module combos. */
    IF @ActionNorm = N'MASTER'
    BEGIN
        SET @ProcessingMode = 0;
        SET @AllowOverwrite = 0;
    END;
    ELSE IF @ActionNorm = N'INSERT'
    BEGIN
        SET @ProcessingMode = 0;
        SET @AllowOverwrite = 0;
    END;
    ELSE IF @ActionNorm = N'UPDATE'
    BEGIN
        SET @ProcessingMode = 2;
        SET @AllowOverwrite = 1;
    END;
    ELSE IF @ActionNorm = N'HIERARCHY'
    BEGIN
        SET @ProcessingMode = 0;
        SET @AllowOverwrite = 0;
    END;

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            BEGIN TRANSACTION;
            SET @StartedTran = 1;
        END;

        UPDATE dbo.ImportBatchMaster
        SET Status = N'Processing'
        WHERE BatchId = @BatchId;

        /* ==============================================================
           MASTER — synchronize org masters for this batch only
           ============================================================== */
        IF @ActionNorm = N'MASTER'
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

            EXEC dbo.usp_HSE_ReconcileValidationWarnings
                @BatchId = @BatchId;

            UPDATE dbo.ImportBatchMaster
            SET
                MasterSyncCompleted = 1,
                MasterSyncDate = CAST(DATEADD(MINUTE, 330, SYSUTCDATETIME()) AS DATETIME),
                Status = N'MastersSynced'
            WHERE BatchId = @BatchId;

            SET @BatchStatus = N'MastersSynced';
            SET @Message = N'Master synchronization completed.';
        END;

        /* ==============================================================
           INSERT — new employees only (never update, never hierarchy SP)
           Existing insert still writes reporting columns onto NEW rows
           (usp_HSE_InsertNewEmployees insert list). Dedicated hierarchy
           processing is NOT invoked.
           ============================================================== */
        IF @ActionNorm = N'INSERT'
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

            SET @EmployeesInserted = ISNULL(@StepInserted, 0);
            SET @ExistingEmployees = ISNULL(@StepInsertSkipped, 0);
            SET @MissingMasterCount = ISNULL(@StepInsertMissingMaster, 0);
            SET @ErrorCount = ISNULL(@StepInsertErrors, 0);
            SET @Message = N'Insert new employees completed.';
        END;

        /* ==============================================================
           UPDATE — existing employees, selected columns only
           Never inserts. Never calls hierarchy procedure.
           ============================================================== */
        IF @ActionNorm = N'UPDATE'
        BEGIN
            SET @StepUpdated = 0;
            SET @StepUpdateSkipped = 0;
            SET @StepUpdateErrors = 0;
            SET @StepUpdateNoChange = 0;
            SET @StepUpdateMissingMaster = 0;

            EXEC dbo.usp_HSE_UpdateChangedEmployees
                @BatchId = @BatchId,
                @AllowOverwrite = 1,
                @ProcessingMode = 2,
                @OverwriteFields = @OverwriteFields,
                @ExecutedBy = @ExecutedBy,
                @UpdatedCount = @StepUpdated OUTPUT,
                @SkippedCount = @StepUpdateSkipped OUTPUT,
                @ErrorCount = @StepUpdateErrors OUTPUT,
                @NoChangeCount = @StepUpdateNoChange OUTPUT,
                @MissingMasterCount = @StepUpdateMissingMaster OUTPUT;

            SET @EmployeesUpdated = ISNULL(@StepUpdated, 0);
            SET @NoChangeCount = ISNULL(@StepUpdateNoChange, 0);
            SET @MissingMasterCount = ISNULL(@StepUpdateMissingMaster, 0);
            SET @ErrorCount = ISNULL(@StepUpdateErrors, 0);
            SET @Message = N'Update existing employees completed.';
        END;

        /* ==============================================================
           HIERARCHY — reporting columns only
           ============================================================== */
        IF @ActionNorm = N'HIERARCHY'
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
            SET @ErrorCount = ISNULL(@StepHierarchyErrors, 0);
            SET @Message = N'Update reporting hierarchy completed.';
        END;

        /* ------------------------------------------------------------------
           Staging finalization (employee / hierarchy actions only).
           Master-only runs must leave rows Pending so operators can still
           run insert / update / hierarchy as separate steps.
           ------------------------------------------------------------------ */
        IF @ActionNorm IN (N'INSERT', N'UPDATE', N'HIERARCHY')
        BEGIN
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

            IF @ErrorCount > 0 AND (@EmployeesInserted > 0 OR @EmployeesUpdated > 0 OR @HierarchyUpdated > 0)
                SET @BatchStatus = N'Processed';
            ELSE IF @ErrorCount > 0
                SET @BatchStatus = N'Failed';
            ELSE
                SET @BatchStatus = N'Processed';

            UPDATE dbo.ImportBatchMaster
            SET Status = @BatchStatus
            WHERE BatchId = @BatchId;
            /* MasterSyncCompleted is intentionally left unchanged. */
        END;

        IF @StartedTran = 1
            COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() = -1
            ROLLBACK TRANSACTION;
        ELSE IF XACT_STATE() = 1 AND @StartedTran = 1
            ROLLBACK TRANSACTION;

        BEGIN TRY
            UPDATE dbo.ImportBatchMaster
            SET Status = N'Failed'
            WHERE BatchId = @BatchId;
        END TRY
        BEGIN CATCH
            /* ignore secondary failure — primary error is re-thrown below */
        END CATCH;

        THROW;
    END CATCH;

    SELECT
        BatchId             = @BatchId,
        Action              = @ActionNorm,
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
