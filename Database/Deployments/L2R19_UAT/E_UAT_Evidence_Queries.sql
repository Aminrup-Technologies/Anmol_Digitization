/*
    Package:     Database/Deployments/L2R19_UAT
    Script:      E_UAT_Evidence_Queries.sql
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN (AnmolDristi)

    UAT-ONLY CANDIDATE. Read-only evidence for one application-created claim.
    It does not INSERT, UPDATE, DELETE, or execute a procedure.
    Do not run it in this preparation task, and do not point it at protected
    claims 97, 100, 111, 112, 118, 119, 121, or 122.

    Set @ApprovalRequestId to the claim created through SalesMonthlyReview.
    Capture this output before the action and again after the action.
    FinalApprovedAmount is the reviewed payable total.
    TotalApprovedAmount is a computed column of claim + DA + hotel + travel
    and does not include Stationary. A Stationary difference between those
    two figures is the existing formula, not a new L2-R19 result.
*/

SET NOCOUNT ON;

DECLARE @ApprovalRequestId int = NULL;

IF DB_NAME() <> N'AnmolDristiDev'
   OR UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
    THROW 58302, N'ABORT: connected database is not AnmolDristiDev on MUMANMOAPP01\SQLEXPRESS.', 1;

IF @ApprovalRequestId IS NULL
    THROW 59002, N'Set @ApprovalRequestId to the application-created UAT claim before running this evidence script.', 1;

IF @ApprovalRequestId IN (97, 100, 111, 112, 118, 119, 121, 122)
    THROW 59001, N'This evidence script refuses protected claims 97, 100, 111, 112, 118, 119, 121, and 122.', 1;

SELECT
      DB_NAME() AS DatabaseName
    , @ApprovalRequestId AS ApprovalRequestId
    , SYSUTCDATETIME() AS EvidenceReadUtc;

SELECT
      r.ApprovalRequestId
    , r.Status
    , r.CurrentApproverLoginId
    , r.CurrentApproverLevel
    , r.ApprovedKM
    , r.ApprovedClaimAmount
    , r.ApprovedDAAmount
    , r.ApprovedHotelAmount
    , r.ApprovedTravelAmount
    , r.FinalApprovedAmount
    , r.TotalApprovedAmount
    , r.ModifiedBy
    , r.ModifiedOn
FROM dbo.SalesApprovalRequest AS r
WHERE r.ApprovalRequestId = @ApprovalRequestId;

SELECT
      a.ApprovalRequestApproverId
    , a.ApproverLevel
    , a.ApproverRole
    , a.ApproverLoginId
    , a.Status
    , a.ActionBy
    , a.ActionDate
    , a.Remarks
FROM dbo.SalesApprovalRequestApprover AS a
WHERE a.ApprovalRequestId = @ApprovalRequestId
ORDER BY a.ApproverLevel, a.ApprovalRequestApproverId;

;WITH VersionRank AS
(
    SELECT
          d.ActivityId
        , MAX(ISNULL(d.DetailVersion, 1)) AS LatestVersion
    FROM dbo.SalesApprovalRequestDetail AS d
    WHERE d.ApprovalRequestId = @ApprovalRequestId
    GROUP BY d.ActivityId
),
Latest AS
(
    SELECT
          d.ActivityId
        , MAX(d.ApprovalRequestDetailId) AS LatestDetailId
    FROM dbo.SalesApprovalRequestDetail AS d
    INNER JOIN VersionRank AS v
        ON v.ActivityId = d.ActivityId
       AND ISNULL(d.DetailVersion, 1) = v.LatestVersion
    WHERE d.ApprovalRequestId = @ApprovalRequestId
    GROUP BY d.ActivityId
)
SELECT
      d.ApprovalRequestDetailId
    , d.ActivityId
    , d.DetailVersion
    , d.DetailStatus
    , d.DetailApprovedLevel
    , d.Snapshot_TotalDistanceKM
    , d.Snapshot_CalculatedClaimAmount
    , d.Snapshot_DAAmount
    , d.Snapshot_HotelAmount
    , d.Snapshot_TravelAmount
    , d.Snapshot_StationaryAmount
    , d.Snapshot_ExpenseTotal
    , d.ModifiedOn
FROM dbo.SalesApprovalRequestDetail AS d
INNER JOIN Latest AS l
    ON d.ApprovalRequestDetailId = l.LatestDetailId
WHERE d.ApprovalRequestId = @ApprovalRequestId
ORDER BY d.ActivityId;

;WITH VersionRank AS
(
    SELECT
          d.ActivityId
        , MAX(ISNULL(d.DetailVersion, 1)) AS LatestVersion
    FROM dbo.SalesApprovalRequestDetail AS d
    WHERE d.ApprovalRequestId = @ApprovalRequestId
    GROUP BY d.ActivityId
),
Latest AS
(
    SELECT
          d.ActivityId
        , MAX(d.ApprovalRequestDetailId) AS LatestDetailId
    FROM dbo.SalesApprovalRequestDetail AS d
    INNER JOIN VersionRank AS v
        ON v.ActivityId = d.ActivityId
       AND ISNULL(d.DetailVersion, 1) = v.LatestVersion
    WHERE d.ApprovalRequestId = @ApprovalRequestId
    GROUP BY d.ActivityId
)
SELECT
      ISNULL(SUM(CASE
          WHEN ISNULL(d.DetailStatus, N'') NOT IN (N'Rejected', N'Returned')
              THEN ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)
          ELSE 0
      END), 0) AS ExpectedFinalApprovedAmount
    , ISNULL(SUM(CASE
          WHEN ISNULL(d.DetailStatus, N'') NOT IN (N'Rejected', N'Returned')
              THEN ISNULL(d.Snapshot_TotalDistanceKM, 0)
          ELSE 0
      END), 0) AS ExpectedApprovedKM
    , SUM(CASE
          WHEN ISNULL(d.DetailStatus, N'Pending') NOT IN (N'Approved', N'Rejected')
              THEN 1
          ELSE 0
      END) AS UnresolvedLatestDayCount
FROM dbo.SalesApprovalRequestDetail AS d
INNER JOIN Latest AS l
    ON d.ApprovalRequestDetailId = l.LatestDetailId
WHERE d.ApprovalRequestId = @ApprovalRequestId;

;WITH VersionRank AS
(
    SELECT
          d.ActivityId
        , MAX(ISNULL(d.DetailVersion, 1)) AS LatestVersion
    FROM dbo.SalesApprovalRequestDetail AS d
    WHERE d.ApprovalRequestId = @ApprovalRequestId
    GROUP BY d.ActivityId
),
Latest AS
(
    SELECT
          d.ActivityId
        , MAX(d.ApprovalRequestDetailId) AS LatestDetailId
    FROM dbo.SalesApprovalRequestDetail AS d
    INNER JOIN VersionRank AS v
        ON v.ActivityId = d.ActivityId
       AND ISNULL(d.DetailVersion, 1) = v.LatestVersion
    WHERE d.ApprovalRequestId = @ApprovalRequestId
    GROUP BY d.ActivityId
)
SELECT
      e.ExpenseId
    , e.ActivityId
    , e.ExpenseType
    , e.Amount
    , e.ClaimStatus
    , e.IsDeleted
FROM dbo.SalesDailyExpense AS e
INNER JOIN Latest AS l
    ON e.ActivityId = l.ActivityId
WHERE e.IsDeleted = 0
ORDER BY e.ActivityId, e.ExpenseId;

;WITH VersionRank AS
(
    SELECT
          d.ActivityId
        , MAX(ISNULL(d.DetailVersion, 1)) AS LatestVersion
    FROM dbo.SalesApprovalRequestDetail AS d
    WHERE d.ApprovalRequestId = @ApprovalRequestId
    GROUP BY d.ActivityId
),
Latest AS
(
    SELECT
          d.ActivityId
        , MAX(d.ApprovalRequestDetailId) AS LatestDetailId
    FROM dbo.SalesApprovalRequestDetail AS d
    INNER JOIN VersionRank AS v
        ON v.ActivityId = d.ActivityId
       AND ISNULL(d.DetailVersion, 1) = v.LatestVersion
    WHERE d.ApprovalRequestId = @ApprovalRequestId
    GROUP BY d.ActivityId
)
SELECT
      COUNT(*) AS NonDeletedExpenseCount
    , SUM(CASE WHEN ISNULL(e.ClaimStatus, N'') IN (N'Pending', N'Returned') THEN 1 ELSE 0 END) AS PendingOrReturnedExpenseCount
FROM dbo.SalesDailyExpense AS e
INNER JOIN Latest AS l
    ON e.ActivityId = l.ActivityId
WHERE e.IsDeleted = 0;

SELECT
      h.HistoryId
    , h.OldStatus
    , h.NewStatus
    , h.ChangedBy
    , h.ChangedOn
    , h.Remarks
FROM dbo.ApprovalRequestStatusHistory AS h
WHERE h.ApprovalRequestId = @ApprovalRequestId
ORDER BY h.HistoryId;
