/*
    RC-014B resubmission snapshot reconciliation. Read-only.
    Authoring status: NOT EXECUTED. This file is not evidence that a
    resubmission occurred, and a MATCH column is only an observation
    from the execution that produced it.

    Target, when a human later runs it after the application workflow:
      server   MUMANMOAPP01\SQLEXPRESS
      database AnmolDristiDev

    Set @ApprovalRequestId to the request that was resubmitted through
    the application. NULL does not choose a request.

    This script does not call dbo.usp_SubmitSalesDailyActivities.
    It does not EXEC any procedure.
    It does not INSERT, UPDATE, DELETE, or MERGE.
    It does not BEGIN, COMMIT, or ROLLBACK a transaction.

    Amount formula, matching dbo.usp_RecalculateApprovalRequest:
      category = SUM(Amount) for that activity and ExpenseType
                 where IsDeleted = 0
                   and ISNULL(ClaimStatus, '') <> 'Rejected'
      Snapshot_ExpenseTotal = DA + Hotel + Travel + Stationary
      A null ClaimStatus is included. A soft-deleted row is excluded.
    Amount is not null on dbo.SalesDailyExpense. There is no ROUND.

    Latest ownership, matching the submission insert:
      DetailVersion DESC, then ApprovalRequestDetailId DESC.
    Older detail rows are listed. This read has no before-image, so it
    cannot certify that an older row was left byte-for-byte unchanged.
    It can show that the older row is not the current owner.

    Header TotalClaimAmount and TotalExpenseAmount are compared with the
    sum of those latest detail rows. Snapshot_CalculatedClaimAmount is
    the activity claim copied at submission. It is not recomputed here.
*/
SET NOCOUNT ON;

IF (@@OPTIONS & 2) = 2
BEGIN
    PRINT N'NOT RUN | IMPLICIT_TRANSACTIONS is ON. This script does not open a transaction.';
    RETURN;
END;

IF @@TRANCOUNT > 0
BEGIN
    PRINT N'NOT RUN | @@TRANCOUNT is already greater than 0. This script does not run inside an open transaction.';
    RETURN;
END;

DECLARE @Server nvarchar(256) = CONVERT(nvarchar(256), SERVERPROPERTY(N'ServerName'));

IF UPPER(ISNULL(@Server, N'')) <> N'MUMANMOAPP01\SQLEXPRESS'
BEGIN
    PRINT N'NOT RUN | Refusing server ' + ISNULL(@Server, N'<null>') + N'. The authorized database instance is MUMANMOAPP01\SQLEXPRESS.';
    RETURN;
END;

IF DB_ID(N'AnmolDristiDev') IS NULL
BEGIN
    PRINT N'NOT RUN | Database AnmolDristiDev is not visible on this server.';
    RETURN;
END;

IF DB_ID(N'AnmolDristi') IS NOT NULL AND DB_NAME() = N'AnmolDristi'
BEGIN
    PRINT N'NOT RUN | Refusing live database AnmolDristi.';
    RETURN;
END;

USE [AnmolDristiDev];

IF DB_NAME() <> N'AnmolDristiDev'
BEGIN
    PRINT N'NOT RUN | Refusing database ' + ISNULL(DB_NAME(), N'<null>') + N'. The authorized database is AnmolDristiDev.';
    RETURN;
END;

DECLARE @ApprovalRequestId int = NULL;

IF @ApprovalRequestId IS NULL OR @ApprovalRequestId <= 0
BEGIN
    PRINT N'NOT RUN | Database=' + ISNULL(DB_NAME(), N'<null>')
        + N' | Set @ApprovalRequestId to the request resubmitted through the application.';
    RETURN;
END;

IF EXISTS (
        SELECT 1
        WHERE COL_LENGTH(N'dbo.SalesApprovalRequest', N'Status') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'CurrentApproverLoginId') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'CurrentApproverLevel') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'TotalKM') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'TotalClaimAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'TotalDAAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'TotalHotelAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'TotalTravelAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'TotalStationaryAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'TotalExpenseAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'DetailVersion') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'PreviousDetailId') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_DAAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_HotelAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_TravelAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_StationaryAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_ExpenseTotal') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_CalculatedClaimAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApproverLoginId') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApproverRole') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApproverLevel') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyExpense', N'ExpenseType') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyExpense', N'Amount') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyExpense', N'ClaimStatus') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyExpense', N'IsDeleted') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivity', N'ClaimStatus') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivity', N'CalculatedClaimAmount') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'OldStatus') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'NewStatus') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'Remarks') IS NULL
    )
BEGIN
    PRINT N'NOT RUN | A column required by this reconciliation is not present. No substitute column is used.';
    RETURN;
END;

IF NOT EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId
    )
BEGIN
    PRINT N'NOT RUN | ApprovalRequestId ' + CONVERT(nvarchar(20), @ApprovalRequestId) + N' was not found on AnmolDristiDev.';
    RETURN;
END;

PRINT N'CONTEXT | Server=' + ISNULL(CONVERT(nvarchar(256), SERVERPROPERTY(N'ServerName')), N'<null>');
PRINT N'CONTEXT | Database=' + ISNULL(DB_NAME(), N'<null>');
PRINT N'CONTEXT | ApprovalRequestId=' + CONVERT(nvarchar(20), @ApprovalRequestId);
PRINT N'CONTEXT | Read-only. MATCH below is this execution''s observation, not a certified pass.';

/* Deployment observation. Module text is not proof of a resubmitted snapshot. */
SELECT
      DB_NAME() AS DatabaseName
    , CASE
          WHEN OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_SubmitSalesDailyActivities'))
               LIKE N'%ISNULL(e.ClaimStatus, '''') <> ''Rejected''%'
              THEN N'OBSERVATION | Deployed module text contains the rejected-expense exclusion.'
          ELSE N'OBSERVATION | Deployed module text does not contain the rejected-expense exclusion.'
      END AS DeploymentObservation
    , OBJECT_ID(N'dbo.usp_SubmitSalesDailyActivities') AS SubmitProcedureId;

/* Header routing against the stored L1 approver row. */
SELECT
      r.ApprovalRequestId
    , r.Status
    , r.CurrentApproverLevel
    , r.CurrentApproverLoginId
    , l1.L1ApproverCount
    , l1.L1ApproverLoginId
    , l1.L1ApproverStatus
    , CASE
          WHEN r.Status = N'Pending'
           AND r.CurrentApproverLevel = 1
           AND l1.L1ApproverCount = 1
           AND r.CurrentApproverLoginId = l1.L1ApproverLoginId
           AND l1.L1ApproverStatus = N'Pending'
              THEN N'MATCH'
          ELSE N'DIFFERENCE'
      END AS L1QueueObservation
FROM dbo.SalesApprovalRequest r
OUTER APPLY
(
    SELECT
          COUNT(*) AS L1ApproverCount
        , MIN(a.ApproverLoginId) AS L1ApproverLoginId
        , MIN(a.Status) AS L1ApproverStatus
    FROM dbo.SalesApprovalRequestApprover a
    WHERE a.ApprovalRequestId = r.ApprovalRequestId
      AND a.ApproverLevel = 1
      AND UPPER(LTRIM(RTRIM(a.ApproverRole))) = N'L1'
) l1
WHERE r.ApprovalRequestId = @ApprovalRequestId;

SELECT
      a.ApprovalRequestApproverId
    , a.ApproverLoginId
    , a.ApproverRole
    , a.ApproverLevel
    , a.Status
    , a.ActionBy
    , a.ActionDate
FROM dbo.SalesApprovalRequestApprover a
WHERE a.ApprovalRequestId = @ApprovalRequestId
ORDER BY a.ApproverLevel, a.ApprovalRequestApproverId;

/* Latest detail against the expense formula. Older versions are not owners. */
;WITH Versioned AS
(
    SELECT
          d.ApprovalRequestDetailId
        , d.ActivityId
        , d.DetailVersion
        , d.PreviousDetailId
        , d.DetailStatus
        , d.Snapshot_CalculatedClaimAmount
        , d.Snapshot_DAAmount
        , d.Snapshot_HotelAmount
        , d.Snapshot_TravelAmount
        , d.Snapshot_StationaryAmount
        , d.Snapshot_ExpenseTotal
        , d.Snapshot_TotalDistanceKM
        , ROW_NUMBER() OVER
          (
              PARTITION BY d.ActivityId
              ORDER BY d.DetailVersion DESC, d.ApprovalRequestDetailId DESC
          ) AS OwnerRank
        , COUNT(*) OVER (PARTITION BY d.ActivityId) AS VersionCount
    FROM dbo.SalesApprovalRequestDetail d
    WHERE d.ApprovalRequestId = @ApprovalRequestId
),
Formula AS
(
    SELECT
          e.ActivityId
        , SUM(CASE WHEN e.ExpenseType = N'DA' THEN e.Amount ELSE 0 END) AS FormulaDAAmount
        , SUM(CASE WHEN e.ExpenseType = N'Hotel' THEN e.Amount ELSE 0 END) AS FormulaHotelAmount
        , SUM(CASE WHEN e.ExpenseType = N'Travel' THEN e.Amount ELSE 0 END) AS FormulaTravelAmount
        , SUM(CASE WHEN e.ExpenseType = N'Stationary' THEN e.Amount ELSE 0 END) AS FormulaStationaryAmount
        , SUM(CASE
                  WHEN e.ExpenseType IN (N'DA', N'Hotel', N'Travel', N'Stationary') THEN 0
                  ELSE e.Amount
              END) AS OtherTypeAmount
        , SUM(e.Amount) AS IncludedAmount
    FROM dbo.SalesDailyExpense e
    WHERE e.IsDeleted = 0
      AND ISNULL(e.ClaimStatus, N'') <> N'Rejected'
      AND e.ActivityId IN
          (
              SELECT v.ActivityId
              FROM Versioned v
          )
    GROUP BY e.ActivityId
)
SELECT
      v.ActivityId
    , v.ApprovalRequestDetailId
    , v.DetailVersion
    , v.PreviousDetailId
    , v.DetailStatus
    , v.VersionCount
    , v.OwnerRank
    , act.ClaimStatus AS ActivityClaimStatus
    , v.Snapshot_DAAmount
    , ISNULL(f.FormulaDAAmount, 0) AS FormulaDAAmount
    , v.Snapshot_HotelAmount
    , ISNULL(f.FormulaHotelAmount, 0) AS FormulaHotelAmount
    , v.Snapshot_TravelAmount
    , ISNULL(f.FormulaTravelAmount, 0) AS FormulaTravelAmount
    , v.Snapshot_StationaryAmount
    , ISNULL(f.FormulaStationaryAmount, 0) AS FormulaStationaryAmount
    , v.Snapshot_ExpenseTotal
    , ISNULL(v.Snapshot_DAAmount, 0)
        + ISNULL(v.Snapshot_HotelAmount, 0)
        + ISNULL(v.Snapshot_TravelAmount, 0)
        + ISNULL(v.Snapshot_StationaryAmount, 0) AS StoredCategorySum
    , ISNULL(f.FormulaDAAmount, 0)
        + ISNULL(f.FormulaHotelAmount, 0)
        + ISNULL(f.FormulaTravelAmount, 0)
        + ISNULL(f.FormulaStationaryAmount, 0) AS FormulaCategorySum
    , ISNULL(f.IncludedAmount, 0) AS FormulaIncludedAmount
    , ISNULL(f.OtherTypeAmount, 0) AS OtherTypeAmount
    , v.Snapshot_CalculatedClaimAmount
    , act.CalculatedClaimAmount AS ActivityCalculatedClaimAmount
    , CASE
          WHEN v.OwnerRank = 1
           AND v.Snapshot_DAAmount = ISNULL(f.FormulaDAAmount, 0)
           AND v.Snapshot_HotelAmount = ISNULL(f.FormulaHotelAmount, 0)
           AND v.Snapshot_TravelAmount = ISNULL(f.FormulaTravelAmount, 0)
           AND v.Snapshot_StationaryAmount = ISNULL(f.FormulaStationaryAmount, 0)
           AND v.Snapshot_ExpenseTotal =
                  ISNULL(v.Snapshot_DAAmount, 0)
                + ISNULL(v.Snapshot_HotelAmount, 0)
                + ISNULL(v.Snapshot_TravelAmount, 0)
                + ISNULL(v.Snapshot_StationaryAmount, 0)
           AND v.Snapshot_ExpenseTotal =
                  ISNULL(f.FormulaDAAmount, 0)
                + ISNULL(f.FormulaHotelAmount, 0)
                + ISNULL(f.FormulaTravelAmount, 0)
                + ISNULL(f.FormulaStationaryAmount, 0)
              THEN N'MATCH'
          WHEN v.OwnerRank = 1 THEN N'DIFFERENCE'
          ELSE N'PREVIOUS VERSION'
      END AS SnapshotObservation
FROM Versioned v
LEFT JOIN Formula f
    ON f.ActivityId = v.ActivityId
LEFT JOIN dbo.SalesDailyActivity act
    ON act.ActivityId = v.ActivityId
ORDER BY v.ActivityId, v.DetailVersion, v.ApprovalRequestDetailId;

/* Rejected and soft-deleted expenses. Amounts are not rewritten by this read. */
SELECT
      e.ExpenseId
    , e.ActivityId
    , e.ExpenseType
    , e.Amount
    , e.ClaimStatus
    , e.IsDeleted
    , CASE
          WHEN e.IsDeleted = 1 THEN N'EXCLUDED | soft-deleted'
          WHEN ISNULL(e.ClaimStatus, N'') = N'Rejected' THEN N'EXCLUDED | rejected; status remains on this row'
          ELSE N'INCLUDED | not rejected and not deleted'
      END AS FormulaTreatment
FROM dbo.SalesDailyExpense e
WHERE e.ActivityId IN
      (
          SELECT d.ActivityId
          FROM dbo.SalesApprovalRequestDetail d
          WHERE d.ApprovalRequestId = @ApprovalRequestId
      )
  AND (
          e.IsDeleted = 1
       OR ISNULL(e.ClaimStatus, N'') = N'Rejected'
      )
ORDER BY e.ActivityId, e.ExpenseId;

/* Header amount columns against the latest detail rows only. */
;WITH Latest AS
(
    SELECT
          d.ActivityId
        , d.Snapshot_TotalDistanceKM
        , d.Snapshot_CalculatedClaimAmount
        , d.Snapshot_DAAmount
        , d.Snapshot_HotelAmount
        , d.Snapshot_TravelAmount
        , d.Snapshot_StationaryAmount
        , d.Snapshot_ExpenseTotal
        , ROW_NUMBER() OVER
          (
              PARTITION BY d.ActivityId
              ORDER BY d.DetailVersion DESC, d.ApprovalRequestDetailId DESC
          ) AS OwnerRank
    FROM dbo.SalesApprovalRequestDetail d
    WHERE d.ApprovalRequestId = @ApprovalRequestId
),
Rolled AS
(
    SELECT
          SUM(l.Snapshot_TotalDistanceKM) AS LatestKM
        , SUM(l.Snapshot_CalculatedClaimAmount) AS LatestClaimAmount
        , SUM(l.Snapshot_DAAmount) AS LatestDAAmount
        , SUM(l.Snapshot_HotelAmount) AS LatestHotelAmount
        , SUM(l.Snapshot_TravelAmount) AS LatestTravelAmount
        , SUM(l.Snapshot_StationaryAmount) AS LatestStationaryAmount
        , SUM(l.Snapshot_ExpenseTotal) AS LatestExpenseAmount
    FROM Latest l
    WHERE l.OwnerRank = 1
)
SELECT
      r.TotalKM
    , x.LatestKM
    , r.TotalClaimAmount
    , x.LatestClaimAmount
    , r.TotalDAAmount
    , x.LatestDAAmount
    , r.TotalHotelAmount
    , x.LatestHotelAmount
    , r.TotalTravelAmount
    , x.LatestTravelAmount
    , r.TotalStationaryAmount
    , x.LatestStationaryAmount
    , r.TotalExpenseAmount
    , x.LatestExpenseAmount
    , CASE
          WHEN (r.TotalClaimAmount = x.LatestClaimAmount
                OR (r.TotalClaimAmount IS NULL AND x.LatestClaimAmount IS NULL))
           AND (r.TotalExpenseAmount = x.LatestExpenseAmount
                OR (r.TotalExpenseAmount IS NULL AND x.LatestExpenseAmount IS NULL))
           AND (r.TotalDAAmount = x.LatestDAAmount
                OR (r.TotalDAAmount IS NULL AND x.LatestDAAmount IS NULL))
           AND (r.TotalHotelAmount = x.LatestHotelAmount
                OR (r.TotalHotelAmount IS NULL AND x.LatestHotelAmount IS NULL))
           AND (r.TotalTravelAmount = x.LatestTravelAmount
                OR (r.TotalTravelAmount IS NULL AND x.LatestTravelAmount IS NULL))
           AND (r.TotalStationaryAmount = x.LatestStationaryAmount
                OR (r.TotalStationaryAmount IS NULL AND x.LatestStationaryAmount IS NULL))
           AND (r.TotalKM = x.LatestKM
                OR (r.TotalKM IS NULL AND x.LatestKM IS NULL))
              THEN N'MATCH'
          ELSE N'DIFFERENCE'
      END AS HeaderRollupObservation
FROM dbo.SalesApprovalRequest r
CROSS JOIN Rolled x
WHERE r.ApprovalRequestId = @ApprovalRequestId;

SELECT
      h.HistoryId
    , h.OldStatus
    , h.NewStatus
    , h.ChangedBy
    , h.ChangedOn
    , h.Remarks
    , h.ApproverLevel
    , h.ActionType
    , h.DetailVersion
FROM dbo.ApprovalRequestStatusHistory h
WHERE h.ApprovalRequestId = @ApprovalRequestId
ORDER BY h.HistoryId;
