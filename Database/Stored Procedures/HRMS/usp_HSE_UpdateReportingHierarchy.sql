/*
    Object:      [dbo].[usp_HSE_UpdateReportingHierarchy]
    Type:        STORED PROCEDURE
    Phase:       3.8 — HRMS Synchronization Engine
    Purpose:     Update reporting hierarchy columns on EXISTING
                 MST_UserMaster rows from a staged HRMS batch.
                 Does not modify any other employee master fields.

    Source:      dbo.Staging_EmployeeData WHERE BatchId = @BatchId
                 (Never uses KKM_SO_TADA_Export_14Nov25$)

    Match keys:
      Employee              → Staging.EmpCode = MST_UserMaster.EmployeeCode
      Manager               → Staging.ReportingManagerCode = MST_UserMaster.EmployeeCode
      Manager's manager     → Staging.ReportingManagersManagerCode = MST_UserMaster.EmployeeCode

    Columns updated (only when changed):
      ReportingManager
      ReportingManagerCode
      ReportingManagersManagerName
      ReportingManagersManagerCode

    Orchestrator contract (usp_RunHRMSSynchronization):
      @BatchId, @ExecutedBy,
      @HierarchyUpdatedCount OUTPUT, @ErrorCount OUTPUT

    Additional outputs:
      @HierarchyUpdated    (= @HierarchyUpdatedCount)
      @NoChangeCount
      @MissingManagerCount (= @ErrorCount for orchestrator rollup)
      @SkippedCount
*/

CREATE OR ALTER PROCEDURE [dbo].[usp_HSE_UpdateReportingHierarchy]
    @BatchId                INT,
    @ExecutedBy             NVARCHAR(128) = NULL,
    @HierarchyUpdatedCount  INT OUTPUT,
    @ErrorCount             INT OUTPUT,
    @HierarchyUpdated       INT = NULL OUTPUT,
    @NoChangeCount          INT = NULL OUTPUT,
    @MissingManagerCount    INT = NULL OUTPUT,
    @SkippedCount           INT = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @HierarchyUpdatedCount = 0;
    SET @ErrorCount = 0;
    SET @HierarchyUpdated = 0;
    SET @NoChangeCount = 0;
    SET @MissingManagerCount = 0;
    SET @SkippedCount = 0;

    DECLARE @StartedTran BIT = 0;

    IF @BatchId IS NULL OR @BatchId <= 0
    BEGIN
        RAISERROR(N'usp_HSE_UpdateReportingHierarchy: @BatchId is required.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.ImportBatchMaster WHERE BatchId = @BatchId)
    BEGIN
        RAISERROR(N'usp_HSE_UpdateReportingHierarchy: BatchId was not found in ImportBatchMaster.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Staging_EmployeeData WITH (NOLOCK)
        WHERE BatchId = @BatchId
    )
    BEGIN
        RAISERROR(N'usp_HSE_UpdateReportingHierarchy: No staging records found for the specified BatchId.', 16, 1);
        RETURN;
    END;

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            BEGIN TRANSACTION;
            SET @StartedTran = 1;
        END;

        IF OBJECT_ID(N'tempdb..#HseHierarchyCandidates') IS NOT NULL
            DROP TABLE #HseHierarchyCandidates;

        /* ------------------------------------------------------------------
           1) Candidate rows (one per EmpCode)
           ------------------------------------------------------------------ */
        ;WITH ranked AS (
            SELECT
                s.StagingId,
                EmpCode = LTRIM(RTRIM(s.EmpCode)),
                StagingManagerName = NULLIF(LTRIM(RTRIM(s.ReportingManager)), N''),
                ManagerCode = NULLIF(LTRIM(RTRIM(s.ReportingManagerCode)), N''),
                StagingManager2Name = NULLIF(LTRIM(RTRIM(s.ReportingManagersManagerName)), N''),
                Manager2Code = NULLIF(LTRIM(RTRIM(s.ReportingManagersManagerCode)), N''),
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
            r.ManagerCode,
            r.Manager2Code,

            ExistsInMaster = CASE WHEN emp.EmployeeCode IS NULL THEN 0 ELSE 1 END,
            ManagerExists = CASE WHEN r.ManagerCode IS NULL THEN CAST(NULL AS INT)
                                 WHEN mgr.EmployeeCode IS NULL THEN 0 ELSE 1 END,
            Manager2Exists = CASE WHEN r.Manager2Code IS NULL THEN CAST(NULL AS INT)
                                  WHEN mgr2.EmployeeCode IS NULL THEN 0 ELSE 1 END,

            IsSelfReporting = CASE
                WHEN r.ManagerCode IS NOT NULL AND r.ManagerCode = r.EmpCode THEN 1
                WHEN r.Manager2Code IS NOT NULL AND r.Manager2Code = r.EmpCode THEN 1
                ELSE 0
            END,

            /* Proposed values: names preferred from MST_UserMaster when code resolves */
            NewReportingManager = CASE
                WHEN r.ManagerCode IS NULL THEN NULL
                WHEN mgr.EmployeeCode IS NOT NULL THEN LEFT(mgr.EmployeeName, 100)
                ELSE LEFT(r.StagingManagerName, 100)
            END,
            NewReportingManagerCode = r.ManagerCode,
            NewReportingManagersManagerName = CASE
                WHEN r.Manager2Code IS NULL THEN NULL
                WHEN mgr2.EmployeeCode IS NOT NULL THEN LEFT(mgr2.EmployeeName, 100)
                ELSE LEFT(r.StagingManager2Name, 100)
            END,
            NewReportingManagersManagerCode = r.Manager2Code,

            CurrentReportingManager = emp.ReportingManager,
            CurrentReportingManagerCode = emp.ReportingManagerCode,
            CurrentReportingManagersManagerName = emp.ReportingManagersManagerName,
            CurrentReportingManagersManagerCode = emp.ReportingManagersManagerCode,

            SkipReason = CAST(NULL AS NVARCHAR(400)),
            IsCircular = CAST(0 AS BIT),
            IsEligible = CAST(0 AS BIT),
            HasChange = CAST(0 AS BIT)
        INTO #HseHierarchyCandidates
        FROM ranked AS r
        LEFT JOIN dbo.MST_UserMaster AS emp
            ON emp.EmployeeCode = r.EmpCode
        LEFT JOIN dbo.MST_UserMaster AS mgr
            ON mgr.EmployeeCode = r.ManagerCode
        LEFT JOIN dbo.MST_UserMaster AS mgr2
            ON mgr2.EmployeeCode = r.Manager2Code
        WHERE r.rn = 1;

        /* ------------------------------------------------------------------
           2) Classify skips / missing managers
           ------------------------------------------------------------------ */
        UPDATE c
        SET
            SkipReason = N'EmployeeCode not found in MST_UserMaster.',
            IsEligible = 0
        FROM #HseHierarchyCandidates AS c
        WHERE c.ExistsInMaster = 0;

        UPDATE c
        SET
            SkipReason = N'No ReportingManagerCode supplied; hierarchy left unchanged.',
            IsEligible = 0
        FROM #HseHierarchyCandidates AS c
        WHERE c.ExistsInMaster = 1
          AND c.ManagerCode IS NULL
          AND c.SkipReason IS NULL;

        UPDATE c
        SET
            SkipReason = N'Self-reporting is not allowed (EmployeeCode = ReportingManagerCode / ReportingManagersManagerCode).',
            IsEligible = 0
        FROM #HseHierarchyCandidates AS c
        WHERE c.ExistsInMaster = 1
          AND c.IsSelfReporting = 1
          AND c.SkipReason IS NULL;

        UPDATE c
        SET
            SkipReason = N'ReportingManagerCode ''' + c.ManagerCode
                         + N''' does not exist in MST_UserMaster.',
            IsEligible = 0
        FROM #HseHierarchyCandidates AS c
        WHERE c.ExistsInMaster = 1
          AND c.ManagerCode IS NOT NULL
          AND c.ManagerExists = 0
          AND c.SkipReason IS NULL;

        UPDATE c
        SET
            SkipReason = N'ReportingManagersManagerCode ''' + c.Manager2Code
                         + N''' does not exist in MST_UserMaster.',
            IsEligible = 0
        FROM #HseHierarchyCandidates AS c
        WHERE c.ExistsInMaster = 1
          AND c.ManagerCode IS NOT NULL
          AND c.ManagerExists = 1
          AND c.Manager2Code IS NOT NULL
          AND c.Manager2Exists = 0
          AND c.SkipReason IS NULL;

        /* Remaining rows with a resolvable L1 manager are eligible pending cycle check */
        UPDATE c
        SET IsEligible = 1
        FROM #HseHierarchyCandidates AS c
        WHERE c.ExistsInMaster = 1
          AND c.ManagerCode IS NOT NULL
          AND c.ManagerExists = 1
          AND c.IsSelfReporting = 0
          AND (c.Manager2Code IS NULL OR c.Manager2Exists = 1)
          AND c.SkipReason IS NULL;

        /* ------------------------------------------------------------------
           3) Circular reference prevention
              Effective graph = proposed manager for batch EmpCodes,
              else current MST_UserMaster.ReportingManagerCode.
              Walk upward from the proposed manager; if Root EmpCode is
              encountered, the assignment would create a cycle.
           ------------------------------------------------------------------ */
        ;WITH ProposedMgr AS (
            SELECT
                EmpCode,
                ManagerCode
            FROM #HseHierarchyCandidates
            WHERE IsEligible = 1
        ),
        Effective AS (
            SELECT
                u.EmployeeCode,
                MgrCode = COALESCE(p.ManagerCode, NULLIF(LTRIM(RTRIM(u.ReportingManagerCode)), N''))
            FROM dbo.MST_UserMaster AS u
            LEFT JOIN ProposedMgr AS p
                ON p.EmpCode = u.EmployeeCode
        ),
        Walk AS (
            SELECT
                c.EmpCode AS RootEmp,
                e.MgrCode AS CurrentMgr,
                0 AS Lvl
            FROM #HseHierarchyCandidates AS c
            INNER JOIN Effective AS e
                ON e.EmployeeCode = c.ManagerCode
            WHERE c.IsEligible = 1

            UNION ALL

            SELECT
                w.RootEmp,
                e.MgrCode,
                w.Lvl + 1
            FROM Walk AS w
            INNER JOIN Effective AS e
                ON e.EmployeeCode = w.CurrentMgr
            WHERE w.Lvl < 50
              AND NULLIF(w.CurrentMgr, N'') IS NOT NULL
              AND w.CurrentMgr <> w.RootEmp
        )
        UPDATE c
        SET
            IsCircular = 1,
            IsEligible = 0,
            SkipReason = N'Circular reporting reference detected for EmployeeCode '''
                         + c.EmpCode + N''' via manager ''' + c.ManagerCode + N'''.'
        FROM #HseHierarchyCandidates AS c
        WHERE c.IsEligible = 1
          AND (
                c.ManagerCode = c.EmpCode
             OR EXISTS (
                    SELECT 1
                    FROM Walk AS w
                    WHERE w.RootEmp = c.EmpCode
                      AND w.CurrentMgr = c.EmpCode
                )
              )
        OPTION (MAXRECURSION 50);

        /* Also treat direct Manager2 == Employee as already handled by self-report.
           Treat ManagerCode == Manager2Code as allowed (same person unlikely but not a cycle). */

        /* ------------------------------------------------------------------
           4) Change detection for eligible rows
           ------------------------------------------------------------------ */
        UPDATE c
        SET HasChange = CASE
            WHEN ISNULL(c.CurrentReportingManager, N'') <> ISNULL(c.NewReportingManager, N'')
              OR ISNULL(c.CurrentReportingManagerCode, N'') <> ISNULL(c.NewReportingManagerCode, N'')
              OR (
                    c.NewReportingManagersManagerCode IS NOT NULL
                    AND (
                        ISNULL(c.CurrentReportingManagersManagerName, N'')
                            <> ISNULL(c.NewReportingManagersManagerName, N'')
                     OR ISNULL(c.CurrentReportingManagersManagerCode, N'')
                            <> ISNULL(c.NewReportingManagersManagerCode, N'')
                    )
                 )
            THEN 1 ELSE 0
        END
        FROM #HseHierarchyCandidates AS c
        WHERE c.IsEligible = 1;

        /* ------------------------------------------------------------------
           5) Annotate staging for skipped / missing / circular
           ------------------------------------------------------------------ */
        UPDATE s
        SET
            s.ErrorMessage =
                CASE
                    WHEN s.ErrorMessage IS NULL OR LTRIM(RTRIM(s.ErrorMessage)) = N'' THEN N''
                    ELSE s.ErrorMessage + N'; '
                END
                + N'[WARNING] Hierarchy not updated: ' + c.SkipReason
        FROM dbo.Staging_EmployeeData AS s
        INNER JOIN #HseHierarchyCandidates AS c
            ON c.StagingId = s.StagingId
        WHERE c.SkipReason IS NOT NULL
          AND c.ExistsInMaster = 1
          AND (
                c.ManagerExists = 0
             OR c.Manager2Exists = 0
             OR c.IsSelfReporting = 1
             OR c.IsCircular = 1
              );

        /* ------------------------------------------------------------------
           6) Apply hierarchy-only UPDATE
           ------------------------------------------------------------------ */
        UPDATE u
        SET
            u.ReportingManager = c.NewReportingManager,
            u.ReportingManagerCode = c.NewReportingManagerCode,
            u.ReportingManagersManagerName = CASE
                WHEN c.NewReportingManagersManagerCode IS NOT NULL
                THEN c.NewReportingManagersManagerName
                ELSE u.ReportingManagersManagerName
            END,
            u.ReportingManagersManagerCode = CASE
                WHEN c.NewReportingManagersManagerCode IS NOT NULL
                THEN c.NewReportingManagersManagerCode
                ELSE u.ReportingManagersManagerCode
            END
        FROM dbo.MST_UserMaster AS u
        INNER JOIN #HseHierarchyCandidates AS c
            ON c.EmpCode = u.EmployeeCode
        WHERE c.IsEligible = 1
          AND c.HasChange = 1;

        SET @HierarchyUpdatedCount = @@ROWCOUNT;
        SET @HierarchyUpdated = @HierarchyUpdatedCount;

        /* ------------------------------------------------------------------
           7) Counters
           ------------------------------------------------------------------ */
        SELECT @MissingManagerCount = COUNT(1)
        FROM #HseHierarchyCandidates
        WHERE ExistsInMaster = 1
          AND (
                (ManagerCode IS NOT NULL AND ManagerExists = 0)
             OR (Manager2Code IS NOT NULL AND ManagerExists = 1 AND Manager2Exists = 0)
              );

        SET @ErrorCount = @MissingManagerCount;

        SELECT @SkippedCount = COUNT(1)
        FROM #HseHierarchyCandidates
        WHERE ExistsInMaster = 0
           OR IsSelfReporting = 1
           OR IsCircular = 1
           OR (ExistsInMaster = 1 AND ManagerCode IS NULL);

        SELECT @NoChangeCount = COUNT(1)
        FROM #HseHierarchyCandidates
        WHERE IsEligible = 1
          AND HasChange = 0;

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
            BatchId               = @BatchId,
            HierarchyUpdatedCount = @HierarchyUpdatedCount,
            HierarchyUpdated      = @HierarchyUpdated,
            NoChangeCount         = @NoChangeCount,
            MissingManagerCount   = @MissingManagerCount,
            SkippedCount          = @SkippedCount,
            ErrorCount            = @ErrorCount,
            ExecutedBy            = @ExecutedBy,
            Message               = N'Update-reporting-hierarchy completed.';
    END;
END
GO
