/* ============================================================================
   NAME:        RC-017A
   WHEN:        2026-09-24
   WHY:         Global_Claim_Recalculation_Engine.sql is BLOCKED (no latest-version
                gate, Returned included in Approved*, missing FinalApprovedAmount,
                inline financial math). LIVE repair must reuse the production contract.
   WHAT:        Maintenance wrapper that enumerates Status IN ('Pending','Returned')
                and EXEC dbo.usp_RecalculateApprovalRequest once per request.
                No duplicated Snapshot_*/Approved*/FinalApprovedAmount formulas.
   OWNERSHIP:   Financial math remains exclusively in usp_RecalculateApprovalRequest
                (same path as ClaimsApproval_v2.ExecuteRecalculationSQL).
   SCOPE:       Pending + Returned only. Completed / ReadyForPayment intentionally
                excluded (payment lock — see usp_ProcessApprovalAction).
   NOTE:        usp_RecalculateApprovalRequest does NOT rewrite TotalClaimAmount /
                TotalExpenseAmount / TotalStationaryAmount (those are submit/expense
                face totals). This wrapper does not invent Total* math either.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/* -------------------------------------------------------------------------
   PARAMETERS
   @DryRun = 1  → backup + preview only (default; safe)
   @DryRun = 0  → backup, then EXEC usp_RecalculateApprovalRequest per request
--------------------------------------------------------------------------- */
DECLARE @DryRun BIT = 1;

DECLARE @RunTag           NVARCHAR(32)  = CONVERT(CHAR(8), GETDATE(), 112)
                                      + '_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), ':', '');
DECLARE @BackupHeader     SYSNAME       = N'dbo.RC017A_Backup_SAR_'  + @RunTag;
DECLARE @BackupDetail     SYSNAME       = N'dbo.RC017A_Backup_SARD_' + @RunTag;
DECLARE @Sql              NVARCHAR(MAX);
DECLARE @ApprovalRequestId INT;
DECLARE @Processed        INT = 0;
DECLARE @Failed           INT = 0;
DECLARE @ErrMsg           NVARCHAR(4000);

PRINT N'RC-017A | RunTag=' + @RunTag + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1));

/* =========================================================================
   0) PREFLIGHT — procedure must exist (ExecuteRecalculationSQL contract)
   ========================================================================= */
IF OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest', N'P') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.usp_RecalculateApprovalRequest not found. Abort.', 16, 1);
    RETURN;
END;

/* =========================================================================
   1) DRY-RUN VALIDATION (always runs)
   ========================================================================= */

-- 1a. Scope counts
SELECT
      r.Status
    , COUNT(*) AS RequestCount
FROM dbo.SalesApprovalRequest r
WHERE r.Status IN (N'Pending', N'Returned')
GROUP BY r.Status
ORDER BY r.Status;

-- 1b. Multi-version fan-out (wrapper must NOT touch superseded rows; SP uses Latest CTE)
SELECT
      COUNT(*) AS PendingReturnedDetailRows
    , SUM(CASE WHEN d.ApprovalRequestDetailId = latest.LatestDetailId THEN 1 ELSE 0 END) AS LatestRows
    , SUM(CASE WHEN d.ApprovalRequestDetailId <> latest.LatestDetailId THEN 1 ELSE 0 END) AS SupersededRows_MustRemainUntouched
FROM dbo.SalesApprovalRequest r
INNER JOIN dbo.SalesApprovalRequestDetail d
    ON d.ApprovalRequestId = r.ApprovalRequestId
INNER JOIN (
    SELECT
          d2.ApprovalRequestId
        , d2.ActivityId
        , MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
    FROM dbo.SalesApprovalRequestDetail d2
    INNER JOIN (
        SELECT
              ApprovalRequestId
            , ActivityId
            , MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
        FROM dbo.SalesApprovalRequestDetail
        GROUP BY ApprovalRequestId, ActivityId
    ) lv
        ON lv.ApprovalRequestId = d2.ApprovalRequestId
       AND lv.ActivityId = d2.ActivityId
       AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
    GROUP BY d2.ApprovalRequestId, d2.ActivityId
) latest
    ON latest.ApprovalRequestId = d.ApprovalRequestId
   AND latest.ActivityId = d.ActivityId
WHERE r.Status IN (N'Pending', N'Returned');

-- 1c. Candidate request list
SELECT
      r.ApprovalRequestId
    , r.RequesterLoginId
    , r.YearNo
    , r.MonthNo
    , r.Status
    , r.ApprovedKM
    , r.ApprovedClaimAmount
    , r.ApprovedDAAmount
    , r.ApprovedHotelAmount
    , r.ApprovedTravelAmount
    , r.FinalApprovedAmount
    , r.TotalClaimAmount
    , r.TotalExpenseAmount
    , r.TotalStationaryAmount
    , r.ModifiedOn
FROM dbo.SalesApprovalRequest r
WHERE r.Status IN (N'Pending', N'Returned')
ORDER BY r.ApprovalRequestId;

/* =========================================================================
   2) BACKUP (always runs before any EXEC)
   ========================================================================= */

SET @Sql = N'
IF OBJECT_ID(N''' + @BackupHeader + N''', N''U'') IS NOT NULL DROP TABLE ' + @BackupHeader + N';
SELECT
      r.ApprovalRequestId
    , r.Status
    , r.ApprovedKM
    , r.ApprovedClaimAmount
    , r.ApprovedDAAmount
    , r.ApprovedHotelAmount
    , r.ApprovedTravelAmount
    , r.FinalApprovedAmount
    , r.TotalClaimAmount
    , r.TotalExpenseAmount
    , r.TotalStationaryAmount
    , r.ModifiedOn
    , r.ModifiedBy
    , SYSUTCDATETIME() AS BackupTakenUtc
INTO ' + @BackupHeader + N'
FROM dbo.SalesApprovalRequest r
WHERE r.Status IN (N''Pending'', N''Returned'');
';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup header → ' + @BackupHeader;

SET @Sql = N'
IF OBJECT_ID(N''' + @BackupDetail + N''', N''U'') IS NOT NULL DROP TABLE ' + @BackupDetail + N';
SELECT
      d.ApprovalRequestDetailId
    , d.ApprovalRequestId
    , d.ActivityId
    , d.DetailVersion
    , d.DetailStatus
    , d.Snapshot_DAAmount
    , d.Snapshot_HotelAmount
    , d.Snapshot_TravelAmount
    , d.Snapshot_StationaryAmount
    , d.Snapshot_ExpenseTotal
    , d.Snapshot_TotalDistanceKM
    , d.Snapshot_CalculatedClaimAmount
    , d.ModifiedOn
    , SYSUTCDATETIME() AS BackupTakenUtc
INTO ' + @BackupDetail + N'
FROM dbo.SalesApprovalRequestDetail d
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = d.ApprovalRequestId
WHERE r.Status IN (N''Pending'', N''Returned'');
';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup detail → ' + @BackupDetail;

IF @DryRun = 1
BEGIN
    PRINT N'DryRun=1 — no EXEC. Set @DryRun=0 after reviewing validation + backup rowcounts.';
    SET @Sql = N'SELECT N''HEADER_BACKUP'' AS Which, COUNT(*) AS Rows FROM ' + @BackupHeader
             + N' UNION ALL SELECT N''DETAIL_BACKUP'', COUNT(*) FROM ' + @BackupDetail + N';';
    EXEC sys.sp_executesql @Sql;
    RETURN;
END;

/* =========================================================================
   3) EXECUTE — one call per request; no inline financial math
   ========================================================================= */
BEGIN TRY
    DECLARE req_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT r.ApprovalRequestId
        FROM dbo.SalesApprovalRequest r
        WHERE r.Status IN (N'Pending', N'Returned')
        ORDER BY r.ApprovalRequestId;

    OPEN req_cursor;
    FETCH NEXT FROM req_cursor INTO @ApprovalRequestId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            EXEC dbo.usp_RecalculateApprovalRequest @ApprovalRequestId = @ApprovalRequestId;
            SET @Processed = @Processed + 1;
        END TRY
        BEGIN CATCH
            SET @Failed = @Failed + 1;
            SET @ErrMsg = ERROR_MESSAGE();
            PRINT N'FAIL ApprovalRequestId=' + CAST(@ApprovalRequestId AS NVARCHAR(20))
                + N' | ' + @ErrMsg;
            -- Continue remaining requests; do not abort the batch mid-fleet.
        END CATCH;

        FETCH NEXT FROM req_cursor INTO @ApprovalRequestId;
    END;

    CLOSE req_cursor;
    DEALLOCATE req_cursor;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'req_cursor') >= -1
    BEGIN
        CLOSE req_cursor;
        DEALLOCATE req_cursor;
    END;
    THROW;
END CATCH;

PRINT N'RC-017A complete | Processed=' + CAST(@Processed AS NVARCHAR(20))
    + N' | Failed=' + CAST(@Failed AS NVARCHAR(20));

IF @Failed > 0
    RAISERROR(N'RC-017A finished with failures. Review PRINT output; restore from backup if needed.', 16, 1);

/* =========================================================================
   4) POST-EXECUTION PARITY (Approved* / FinalApprovedAmount vs latest contract)
      Total* are reported for visibility only — SP does not rewrite them.
   ========================================================================= */
;WITH Latest AS
(
    SELECT
          d2.ApprovalRequestId
        , d2.ActivityId
        , MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
    FROM dbo.SalesApprovalRequestDetail d2
    INNER JOIN (
        SELECT
              ApprovalRequestId
            , ActivityId
            , MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
        FROM dbo.SalesApprovalRequestDetail
        GROUP BY ApprovalRequestId, ActivityId
    ) lv
        ON lv.ApprovalRequestId = d2.ApprovalRequestId
       AND lv.ActivityId = d2.ActivityId
       AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
    WHERE d2.ApprovalRequestId IN (
        SELECT ApprovalRequestId
        FROM dbo.SalesApprovalRequest
        WHERE Status IN (N'Pending', N'Returned')
    )
    GROUP BY d2.ApprovalRequestId, d2.ActivityId
),
Expected AS
(
    SELECT
          d.ApprovalRequestId
        , ISNULL(SUM(CASE WHEN ISNULL(d.DetailStatus, N'') NOT IN (N'Rejected', N'Returned')
                          THEN d.Snapshot_TotalDistanceKM ELSE 0 END), 0) AS Exp_ApprovedKM
        , ISNULL(SUM(CASE WHEN ISNULL(d.DetailStatus, N'') NOT IN (N'Rejected', N'Returned')
                          THEN d.Snapshot_CalculatedClaimAmount ELSE 0 END), 0) AS Exp_ApprovedClaimAmount
        , ISNULL(SUM(CASE WHEN ISNULL(d.DetailStatus, N'') NOT IN (N'Rejected', N'Returned')
                          THEN d.Snapshot_DAAmount ELSE 0 END), 0) AS Exp_ApprovedDAAmount
        , ISNULL(SUM(CASE WHEN ISNULL(d.DetailStatus, N'') NOT IN (N'Rejected', N'Returned')
                          THEN d.Snapshot_HotelAmount ELSE 0 END), 0) AS Exp_ApprovedHotelAmount
        , ISNULL(SUM(CASE WHEN ISNULL(d.DetailStatus, N'') NOT IN (N'Rejected', N'Returned')
                          THEN d.Snapshot_TravelAmount ELSE 0 END), 0) AS Exp_ApprovedTravelAmount
        , ISNULL(SUM(CASE WHEN ISNULL(d.DetailStatus, N'') NOT IN (N'Rejected', N'Returned')
                          THEN ISNULL(d.Snapshot_CalculatedClaimAmount, 0)
                             + ISNULL(d.Snapshot_ExpenseTotal, 0)
                          ELSE 0 END), 0) AS Exp_FinalApprovedAmount
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN Latest l
        ON d.ApprovalRequestDetailId = l.LatestDetailId
    GROUP BY d.ApprovalRequestId
)
SELECT
      r.ApprovalRequestId
    , r.Status
    , CASE WHEN ISNULL(r.ApprovedKM, 0)              = e.Exp_ApprovedKM
            AND ISNULL(r.ApprovedClaimAmount, 0)     = e.Exp_ApprovedClaimAmount
            AND ISNULL(r.ApprovedDAAmount, 0)        = e.Exp_ApprovedDAAmount
            AND ISNULL(r.ApprovedHotelAmount, 0)     = e.Exp_ApprovedHotelAmount
            AND ISNULL(r.ApprovedTravelAmount, 0)    = e.Exp_ApprovedTravelAmount
            AND ISNULL(r.FinalApprovedAmount, 0)     = e.Exp_FinalApprovedAmount
           THEN N'PASS' ELSE N'FAIL' END AS Parity
    , r.ApprovedKM, e.Exp_ApprovedKM
    , r.ApprovedClaimAmount, e.Exp_ApprovedClaimAmount
    , r.ApprovedDAAmount, e.Exp_ApprovedDAAmount
    , r.ApprovedHotelAmount, e.Exp_ApprovedHotelAmount
    , r.ApprovedTravelAmount, e.Exp_ApprovedTravelAmount
    , r.FinalApprovedAmount, e.Exp_FinalApprovedAmount
    , r.TotalClaimAmount
    , r.TotalExpenseAmount
    , r.TotalStationaryAmount  -- visibility only; not owned by usp_RecalculateApprovalRequest
FROM dbo.SalesApprovalRequest r
INNER JOIN Expected e
    ON e.ApprovalRequestId = r.ApprovalRequestId
WHERE r.Status IN (N'Pending', N'Returned')
ORDER BY
      CASE WHEN ISNULL(r.ApprovedKM, 0)              = e.Exp_ApprovedKM
            AND ISNULL(r.ApprovedClaimAmount, 0)     = e.Exp_ApprovedClaimAmount
            AND ISNULL(r.ApprovedDAAmount, 0)        = e.Exp_ApprovedDAAmount
            AND ISNULL(r.ApprovedHotelAmount, 0)     = e.Exp_ApprovedHotelAmount
            AND ISNULL(r.ApprovedTravelAmount, 0)    = e.Exp_ApprovedTravelAmount
            AND ISNULL(r.FinalApprovedAmount, 0)     = e.Exp_FinalApprovedAmount
           THEN 1 ELSE 0 END,
      r.ApprovalRequestId;
GO

/*
============================================================================
5) ROLLBACK SEQUENCE (manual — run only if post-parity FAIL or ops abort)
   Substitute the backup table names printed at runtime for <RunTag>.

BEGIN TRAN;

UPDATE d
SET
      d.Snapshot_DAAmount              = b.Snapshot_DAAmount
    , d.Snapshot_HotelAmount           = b.Snapshot_HotelAmount
    , d.Snapshot_TravelAmount          = b.Snapshot_TravelAmount
    , d.Snapshot_StationaryAmount      = b.Snapshot_StationaryAmount
    , d.Snapshot_ExpenseTotal          = b.Snapshot_ExpenseTotal
    , d.ModifiedOn                     = b.ModifiedOn
FROM dbo.SalesApprovalRequestDetail d
INNER JOIN dbo.RC017A_Backup_SARD_<RunTag> b
    ON b.ApprovalRequestDetailId = d.ApprovalRequestDetailId;

UPDATE r
SET
      r.ApprovedKM             = b.ApprovedKM
    , r.ApprovedClaimAmount    = b.ApprovedClaimAmount
    , r.ApprovedDAAmount       = b.ApprovedDAAmount
    , r.ApprovedHotelAmount    = b.ApprovedHotelAmount
    , r.ApprovedTravelAmount   = b.ApprovedTravelAmount
    , r.FinalApprovedAmount    = b.FinalApprovedAmount
    , r.ModifiedOn             = b.ModifiedOn
    , r.ModifiedBy             = b.ModifiedBy
FROM dbo.SalesApprovalRequest r
INNER JOIN dbo.RC017A_Backup_SAR_<RunTag> b
    ON b.ApprovalRequestId = r.ApprovalRequestId;

-- Spot-check, then COMMIT TRAN;  (or ROLLBACK TRAN;)
============================================================================
*/
