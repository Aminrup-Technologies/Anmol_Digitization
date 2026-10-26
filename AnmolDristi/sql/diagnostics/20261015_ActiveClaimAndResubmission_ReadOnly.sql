/*
    September 2026 read-only claims census.

    Tables, confirmed from the live catalog:
      dbo.SalesApprovalRequest
      dbo.SalesApprovalRequestDetail
      dbo.SalesDailyActivity
      dbo.ApprovalRequestStatusHistory

    There is no dbo.ApprovalRequest and no dbo.ApprovalRequestDetail.
    Activity ownership is SalesDailyActivity.ApprovalRequestId.
    A detail that mentions an activity does not own that activity.

    Active header statuses match SalesMonthlyReview.IsActiveMonthClaimStatus:
      Returned, Pending, Completed, ReadyForPayment, Processed.
    Draft and Rejected are closed.

    Latest detail, within one request and one activity:
      highest ISNULL(DetailVersion, 1), then highest ApprovalRequestDetailId.
    An older Returned version is retained history. It is not the current detail.

    Unresolved register, and only these conditions:
      1. More than one active header for the same employee and period.
      2. The latest detail is Returned and the activity owned by that same
         request has a different ClaimStatus.
      3. A newer detail has no PreviousDetailId, or PreviousDetailId does
         not point at the same activity and the same request.
      4. A returned activity dated in the period does not belong to a
         request for that same YearNo and MonthNo. A null or unknown
         ApprovalRequestId is reported at activity level. A real request
         from another period is reported on the request register.

    Returned work, resubmission history, and intact DetailVersion > 1 are
    visibility results. They do not enter the unresolved register.
    History uses OldStatus and NewStatus. A remark alone is not a transition.

    Stored FinalApprovedAmount is selected. It is not recalculated.
    This script has no INSERT, UPDATE, DELETE, MERGE, or procedure call.
    Employee correction and resubmission stay in the application.
*/
SET NOCOUNT ON;

DECLARE @YearNo int = 2026;
DECLARE @MonthNo int = 9;
DECLARE @RequesterLoginId nvarchar(100) = NULL;

IF @YearNo IS NULL
   OR @YearNo < 2000
   OR @YearNo > 2100
   OR @MonthNo IS NULL
   OR @MonthNo < 1
   OR @MonthNo > 12
    THROW 53101, 'YearNo must be 2000 through 2100 and MonthNo must be 1 through 12.', 1;

IF @RequesterLoginId IS NOT NULL
   AND LTRIM(RTRIM(@RequesterLoginId)) = N''
    THROW 53102, 'RequesterLoginId must be NULL for the full population, or a non-blank employee code.', 1;

DECLARE @PeriodStart date = DATEFROMPARTS(@YearNo, @MonthNo, 1);
DECLARE @PeriodEnd date = DATEADD(MONTH, 1, @PeriodStart);
DECLARE @FullSeptemberCensus bit =
    CASE
        WHEN @YearNo = 2026 AND @MonthNo = 9 AND @RequesterLoginId IS NULL THEN 1
        ELSE 0
    END;
DECLARE @EnvironmentOk bit =
    CASE
        WHEN @@SERVERNAME = N'MUMANMOAPP01\SQLEXPRESS'
         AND DB_NAME() = N'AnmolDristi'
        THEN 1
        ELSE 0
    END;

/* 1. Connection and parameters. */
SELECT
    N'Session' AS ResultSetName,
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')) AS ProductVersion,
    CONVERT(nvarchar(128), SERVERPROPERTY('Edition')) AS Edition,
    SYSDATETIMEOFFSET() AS ExecutedAt,
    DATEPART(TZOFFSET, SYSDATETIMEOFFSET()) AS OffsetMinutes,
    @YearNo AS YearNo,
    @MonthNo AS MonthNo,
    @RequesterLoginId AS RequesterLoginId,
    @PeriodStart AS PeriodStart,
    @PeriodEnd AS PeriodEndExclusive,
    CASE WHEN @EnvironmentOk = 1 THEN N'PASS' ELSE N'BLOCKED' END AS EnvironmentClassification,
    CASE
        WHEN @EnvironmentOk = 1 THEN N'Server and database match the September 2026 census target.'
        ELSE N'Environment-dependent checks are BLOCKED. Server or database is not MUMANMOAPP01\SQLEXPRESS / AnmolDristi.'
    END AS EnvironmentNote;

/* 2. Census population. Expected 571 is the unfiltered September 2026 count. */
SELECT
    N'Population' AS ResultSetName,
    COUNT(*) AS RequestCount,
    COUNT(DISTINCT r.RequesterLoginId) AS RequesterCount,
    CASE
        WHEN @EnvironmentOk = 0 THEN N'BLOCKED'
        WHEN @FullSeptemberCensus = 0 THEN N'NOT TESTED'
        WHEN COUNT(*) = 571 THEN N'PASS'
        ELSE N'FAIL'
    END AS PopulationClassification
FROM dbo.SalesApprovalRequest r
WHERE r.YearNo = @YearNo
  AND r.MonthNo = @MonthNo
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId);

SELECT
    N'PopulationByStatus' AS ResultSetName,
    r.Status,
    COUNT(*) AS RequestCount
FROM dbo.SalesApprovalRequest r
WHERE r.YearNo = @YearNo
  AND r.MonthNo = @MonthNo
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
GROUP BY r.Status
ORDER BY r.Status;

/* 3. Active-request collisions. Every conflicting request is listed. */
SELECT
    N'ActiveRequestCollisions' AS ResultSetName,
    r.ApprovalRequestId,
    r.RequesterLoginId,
    r.YearNo,
    r.MonthNo,
    r.Status,
    r.CurrentApproverLoginId,
    r.CurrentApproverLevel,
    g.ActiveHeaderCount
FROM dbo.SalesApprovalRequest r
INNER JOIN (
    SELECT
        x.RequesterLoginId,
        x.YearNo,
        x.MonthNo,
        COUNT(*) AS ActiveHeaderCount
    FROM dbo.SalesApprovalRequest x
    WHERE x.YearNo = @YearNo
      AND x.MonthNo = @MonthNo
      AND (@RequesterLoginId IS NULL OR x.RequesterLoginId = @RequesterLoginId)
      AND UPPER(LTRIM(RTRIM(ISNULL(x.Status, N'')))) IN
          (N'RETURNED', N'PENDING', N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED')
    GROUP BY x.RequesterLoginId, x.YearNo, x.MonthNo
    HAVING COUNT(*) > 1
) g
    ON g.RequesterLoginId = r.RequesterLoginId
   AND g.YearNo = r.YearNo
   AND g.MonthNo = r.MonthNo
WHERE UPPER(LTRIM(RTRIM(ISNULL(r.Status, N'')))) IN
      (N'RETURNED', N'PENDING', N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED')
ORDER BY r.RequesterLoginId, r.ApprovalRequestId;

/*
    4. Latest Returned detail whose activity is owned by the same request
    and has a different ClaimStatus.
*/
SELECT
    N'ReturnedDetailActivityDisagreements' AS ResultSetName,
    r.ApprovalRequestId,
    r.RequesterLoginId,
    r.Status AS HeaderStatus,
    d.ApprovalRequestDetailId,
    d.ActivityId,
    d.DetailVersion,
    d.PreviousDetailId,
    d.DetailStatus,
    a.ClaimStatus AS ActivityStatus,
    a.ApprovalRequestId AS ActivityOwnerRequestId
FROM dbo.SalesApprovalRequestDetail d
INNER JOIN (
    SELECT
        d2.ApprovalRequestId,
        d2.ActivityId,
        MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
    FROM dbo.SalesApprovalRequestDetail d2
    INNER JOIN (
        SELECT
            p.ApprovalRequestId,
            p.ActivityId,
            MAX(ISNULL(p.DetailVersion, 1)) AS LatestVersion
        FROM dbo.SalesApprovalRequestDetail p
        INNER JOIN dbo.SalesApprovalRequest hr
            ON hr.ApprovalRequestId = p.ApprovalRequestId
        WHERE hr.YearNo = @YearNo
          AND hr.MonthNo = @MonthNo
          AND (@RequesterLoginId IS NULL OR hr.RequesterLoginId = @RequesterLoginId)
        GROUP BY p.ApprovalRequestId, p.ActivityId
    ) v
        ON v.ApprovalRequestId = d2.ApprovalRequestId
       AND v.ActivityId = d2.ActivityId
       AND ISNULL(d2.DetailVersion, 1) = v.LatestVersion
    GROUP BY d2.ApprovalRequestId, d2.ActivityId
) latest
    ON latest.LatestDetailId = d.ApprovalRequestDetailId
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = d.ApprovalRequestId
INNER JOIN dbo.SalesDailyActivity a
    ON a.ActivityId = d.ActivityId
   AND a.ApprovalRequestId = d.ApprovalRequestId
WHERE r.YearNo = @YearNo
  AND r.MonthNo = @MonthNo
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
  AND d.DetailStatus = N'Returned'
  AND ISNULL(a.ClaimStatus, N'') <> N'Returned'
ORDER BY r.ApprovalRequestId, d.ActivityId;

/*
    5. Newer details with a missing or cross-activity / cross-request predecessor.
    Version 1 with a null predecessor is the start of a chain.
*/
SELECT
    N'BrokenPredecessors' AS ResultSetName,
    r.ApprovalRequestId,
    r.RequesterLoginId,
    r.YearNo,
    r.MonthNo,
    d.ApprovalRequestDetailId,
    d.ActivityId,
    d.DetailVersion,
    d.PreviousDetailId,
    CASE
        WHEN d.PreviousDetailId IS NULL THEN N'Newer detail has no PreviousDetailId'
        WHEN pred.ApprovalRequestDetailId IS NULL THEN N'PreviousDetailId does not exist'
        WHEN pred.ActivityId <> d.ActivityId THEN N'Previous detail belongs to another activity'
        WHEN pred.ApprovalRequestId <> d.ApprovalRequestId THEN N'Previous detail belongs to another request'
        ELSE N'Predecessor check did not fail'
    END AS BreakReason
FROM dbo.SalesApprovalRequestDetail d
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = d.ApprovalRequestId
LEFT JOIN dbo.SalesApprovalRequestDetail pred
    ON pred.ApprovalRequestDetailId = d.PreviousDetailId
WHERE r.YearNo = @YearNo
  AND r.MonthNo = @MonthNo
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
  AND (
        (d.DetailVersion > 1 AND d.PreviousDetailId IS NULL)
        OR (
            d.PreviousDetailId IS NOT NULL
            AND (
                pred.ApprovalRequestDetailId IS NULL
                OR pred.ActivityId <> d.ActivityId
                OR pred.ApprovalRequestId <> d.ApprovalRequestId
            )
        )
      )
ORDER BY r.ApprovalRequestId, d.ActivityId, d.DetailVersion, d.ApprovalRequestDetailId;

/*
    6. Returned activities in the half-open period that do not belong to a
    request for the same YearNo and MonthNo. Null and unknown owners stay here.
*/
SELECT
    N'ReturnedActivitiesWithoutPeriodRequest' AS ResultSetName,
    a.ActivityId,
    a.Login_Id,
    a.ActivityDate,
    a.ClaimStatus,
    a.ApprovalRequestId,
    owner.YearNo AS OwnerYearNo,
    owner.MonthNo AS OwnerMonthNo,
    owner.Status AS OwnerStatus,
    owner.RequesterLoginId AS OwnerRequesterLoginId,
    CASE
        WHEN a.ApprovalRequestId IS NULL THEN N'Missing ownership'
        WHEN owner.ApprovalRequestId IS NULL THEN N'ApprovalRequestId does not match a request'
        ELSE N'Owner request exists in a different period'
    END AS OwnershipFinding
FROM dbo.SalesDailyActivity a
LEFT JOIN dbo.SalesApprovalRequest owner
    ON owner.ApprovalRequestId = a.ApprovalRequestId
WHERE a.ClaimStatus = N'Returned'
  AND a.ActivityDate >= @PeriodStart
  AND a.ActivityDate < @PeriodEnd
  AND (@RequesterLoginId IS NULL OR a.Login_Id = @RequesterLoginId)
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.SalesApprovalRequest periodRequest
      WHERE periodRequest.ApprovalRequestId = a.ApprovalRequestId
        AND periodRequest.YearNo = @YearNo
        AND periodRequest.MonthNo = @MonthNo
        AND (@RequesterLoginId IS NULL OR periodRequest.RequesterLoginId = @RequesterLoginId)
  )
ORDER BY a.Login_Id, a.ActivityDate, a.ActivityId;

/* 7. Distinct unresolved requests. One row per request, reasons combined. */
SELECT
    N'UnresolvedRequests' AS ResultSetName,
    u.ApprovalRequestId,
    STRING_AGG(u.Reason, N' | ') WITHIN GROUP (ORDER BY u.Reason) AS Reasons
FROM (
    SELECT DISTINCT src.ApprovalRequestId, src.Reason
    FROM (
        SELECT
            r.ApprovalRequestId,
            N'Active-request collision' AS Reason
        FROM dbo.SalesApprovalRequest r
        INNER JOIN (
            SELECT x.RequesterLoginId, x.YearNo, x.MonthNo
            FROM dbo.SalesApprovalRequest x
            WHERE x.YearNo = @YearNo
              AND x.MonthNo = @MonthNo
              AND (@RequesterLoginId IS NULL OR x.RequesterLoginId = @RequesterLoginId)
              AND UPPER(LTRIM(RTRIM(ISNULL(x.Status, N'')))) IN
                  (N'RETURNED', N'PENDING', N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED')
            GROUP BY x.RequesterLoginId, x.YearNo, x.MonthNo
            HAVING COUNT(*) > 1
        ) g
            ON g.RequesterLoginId = r.RequesterLoginId
           AND g.YearNo = r.YearNo
           AND g.MonthNo = r.MonthNo
        WHERE UPPER(LTRIM(RTRIM(ISNULL(r.Status, N'')))) IN
              (N'RETURNED', N'PENDING', N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED')

        UNION ALL

        SELECT
            r.ApprovalRequestId,
            N'Returned latest detail disagrees with the activity owned by this request'
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN (
            SELECT d2.ApprovalRequestId, d2.ActivityId, MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
            FROM dbo.SalesApprovalRequestDetail d2
            INNER JOIN (
                SELECT p.ApprovalRequestId, p.ActivityId, MAX(ISNULL(p.DetailVersion, 1)) AS LatestVersion
                FROM dbo.SalesApprovalRequestDetail p
                INNER JOIN dbo.SalesApprovalRequest hr
                    ON hr.ApprovalRequestId = p.ApprovalRequestId
                WHERE hr.YearNo = @YearNo
                  AND hr.MonthNo = @MonthNo
                  AND (@RequesterLoginId IS NULL OR hr.RequesterLoginId = @RequesterLoginId)
                GROUP BY p.ApprovalRequestId, p.ActivityId
            ) v
                ON v.ApprovalRequestId = d2.ApprovalRequestId
               AND v.ActivityId = d2.ActivityId
               AND ISNULL(d2.DetailVersion, 1) = v.LatestVersion
            GROUP BY d2.ApprovalRequestId, d2.ActivityId
        ) latest
            ON latest.LatestDetailId = d.ApprovalRequestDetailId
        INNER JOIN dbo.SalesApprovalRequest r
            ON r.ApprovalRequestId = d.ApprovalRequestId
        INNER JOIN dbo.SalesDailyActivity a
            ON a.ActivityId = d.ActivityId
           AND a.ApprovalRequestId = d.ApprovalRequestId
        WHERE r.YearNo = @YearNo
          AND r.MonthNo = @MonthNo
          AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
          AND d.DetailStatus = N'Returned'
          AND ISNULL(a.ClaimStatus, N'') <> N'Returned'

        UNION ALL

        SELECT
            r.ApprovalRequestId,
            N'Broken detail predecessor'
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN dbo.SalesApprovalRequest r
            ON r.ApprovalRequestId = d.ApprovalRequestId
        LEFT JOIN dbo.SalesApprovalRequestDetail pred
            ON pred.ApprovalRequestDetailId = d.PreviousDetailId
        WHERE r.YearNo = @YearNo
          AND r.MonthNo = @MonthNo
          AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
          AND (
                (d.DetailVersion > 1 AND d.PreviousDetailId IS NULL)
                OR (
                    d.PreviousDetailId IS NOT NULL
                    AND (
                        pred.ApprovalRequestDetailId IS NULL
                        OR pred.ActivityId <> d.ActivityId
                        OR pred.ApprovalRequestId <> d.ApprovalRequestId
                    )
                )
              )

        UNION ALL

        SELECT
            owner.ApprovalRequestId,
            N'Returned period activity belongs to a request outside this period'
        FROM dbo.SalesDailyActivity a
        INNER JOIN dbo.SalesApprovalRequest owner
            ON owner.ApprovalRequestId = a.ApprovalRequestId
        WHERE a.ClaimStatus = N'Returned'
          AND a.ActivityDate >= @PeriodStart
          AND a.ActivityDate < @PeriodEnd
          AND (@RequesterLoginId IS NULL OR a.Login_Id = @RequesterLoginId)
          AND NOT (
              owner.YearNo = @YearNo
              AND owner.MonthNo = @MonthNo
              AND (@RequesterLoginId IS NULL OR owner.RequesterLoginId = @RequesterLoginId)
          )
    ) src
) u
GROUP BY u.ApprovalRequestId
ORDER BY u.ApprovalRequestId;

/*
    8. Regression for the repaired collisions.
    Stored totals are compared. They are not recomputed.
*/
SELECT
    N'RepairedCollisionRegression' AS ResultSetName,
    v.RequesterLoginId,
    v.KeptRequestId,
    v.DuplicateRequestId,
    v.ActivityId,
    kept.Status AS KeptStatus,
    kept.CurrentApproverLoginId AS KeptApprover,
    kept.CurrentApproverLevel AS KeptApproverLevel,
    kept.FinalApprovedAmount AS KeptStoredFinalAmount,
    v.ExpectedFinalAmount,
    duplicate.Status AS DuplicateStatus,
    duplicate.ModifiedBy AS DuplicateModifiedBy,
    activity.ClaimStatus AS ActivityStatus,
    activity.ApprovalRequestId AS ActivityOwnerRequestId,
    latest.ApprovalRequestDetailId AS LatestDetailId,
    latest.DetailVersion AS LatestDetailVersion,
    latest.DetailStatus AS LatestDetailStatus,
    activeHeaders.ActiveHeaderCount,
    CASE
        WHEN @EnvironmentOk = 0 THEN N'BLOCKED'
        WHEN @YearNo <> 2026 OR @MonthNo <> 9 THEN N'NOT TESTED'
        WHEN @RequesterLoginId IS NOT NULL AND @RequesterLoginId <> v.RequesterLoginId THEN N'NOT TESTED'
        WHEN kept.ApprovalRequestId IS NULL
          OR duplicate.ApprovalRequestId IS NULL
          OR activity.ActivityId IS NULL
          OR latest.ApprovalRequestDetailId IS NULL
        THEN N'FAIL'
        WHEN kept.RequesterLoginId = v.RequesterLoginId
         AND kept.YearNo = 2026
         AND kept.MonthNo = 9
         AND kept.Status = N'Returned'
         AND UPPER(LTRIM(RTRIM(ISNULL(kept.Status, N'')))) IN
             (N'RETURNED', N'PENDING', N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED')
         AND duplicate.RequesterLoginId = v.RequesterLoginId
         AND duplicate.YearNo = 2026
         AND duplicate.MonthNo = 9
         AND UPPER(LTRIM(RTRIM(ISNULL(duplicate.Status, N'')))) = N'REJECTED'
         AND kept.FinalApprovedAmount = v.ExpectedFinalAmount
         AND activity.ClaimStatus = N'Returned'
         AND activity.ApprovalRequestId = v.KeptRequestId
         AND latest.DetailStatus = N'Returned'
         AND activeHeaders.ActiveHeaderCount = 1
         AND activeHeaders.ActiveKeptCount = 1
        THEN N'PASS'
        ELSE N'FAIL'
    END AS Classification
FROM (
    VALUES
        (N'AHS1180', 1424, 1495, 91457, CONVERT(decimal(18, 2), 16274.92)),
        (N'M2659', 1483, 1490, 104263, CONVERT(decimal(18, 2), 16909.46))
) v (RequesterLoginId, KeptRequestId, DuplicateRequestId, ActivityId, ExpectedFinalAmount)
LEFT JOIN dbo.SalesApprovalRequest kept
    ON kept.ApprovalRequestId = v.KeptRequestId
LEFT JOIN dbo.SalesApprovalRequest duplicate
    ON duplicate.ApprovalRequestId = v.DuplicateRequestId
LEFT JOIN dbo.SalesDailyActivity activity
    ON activity.ActivityId = v.ActivityId
OUTER APPLY (
    SELECT TOP (1)
        d.ApprovalRequestDetailId,
        d.DetailVersion,
        d.DetailStatus
    FROM dbo.SalesApprovalRequestDetail d
    WHERE d.ApprovalRequestId = v.KeptRequestId
      AND d.ActivityId = v.ActivityId
    ORDER BY ISNULL(d.DetailVersion, 1) DESC, d.ApprovalRequestDetailId DESC
) latest
OUTER APPLY (
    SELECT
        COUNT(*) AS ActiveHeaderCount,
        SUM(CASE WHEN x.ApprovalRequestId = v.KeptRequestId THEN 1 ELSE 0 END) AS ActiveKeptCount
    FROM dbo.SalesApprovalRequest x
    WHERE x.RequesterLoginId = v.RequesterLoginId
      AND x.YearNo = @YearNo
      AND x.MonthNo = @MonthNo
      AND UPPER(LTRIM(RTRIM(ISNULL(x.Status, N'')))) IN
          (N'RETURNED', N'PENDING', N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED')
) activeHeaders
ORDER BY v.KeptRequestId;

/*
    9. Normal correction visibility.
    These requests are not unresolved solely because their days are Returned.
*/
SELECT
    N'ExpectedCorrectionVisibility' AS ResultSetName,
    expected.ApprovalRequestId AS ExpectedRequestId,
    expected.RequesterLoginId AS ExpectedRequesterLoginId,
    r.ApprovalRequestId,
    r.RequesterLoginId,
    r.YearNo,
    r.MonthNo,
    r.Status AS HeaderStatus,
    r.CurrentApproverLoginId,
    r.CurrentApproverLevel,
    r.ModifiedBy,
    COUNT(latest.LatestDetailId) AS LatestDetailCount,
    SUM(CASE WHEN d.DetailStatus = N'Returned' THEN 1 ELSE 0 END) AS ReturnedLatestDetails,
    SUM(CASE
            WHEN a.ActivityId IS NOT NULL AND a.ClaimStatus = N'Returned' THEN 1
            ELSE 0
        END) AS OwnedReturnedActivities,
    CASE
        WHEN @YearNo <> 2026 OR @MonthNo <> 9 THEN N'NOT TESTED'
        WHEN @RequesterLoginId IS NOT NULL
         AND @RequesterLoginId <> expected.RequesterLoginId
        THEN N'NOT TESTED'
        WHEN r.ApprovalRequestId IS NULL THEN N'FAIL'
        ELSE N'VISIBILITY'
    END AS Classification
FROM (
    VALUES
        (1200, N'AHS1714'),
        (1493, N'M2562')
) expected (ApprovalRequestId, RequesterLoginId)
LEFT JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = expected.ApprovalRequestId
LEFT JOIN (
    SELECT
        d2.ApprovalRequestId,
        d2.ActivityId,
        MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
    FROM dbo.SalesApprovalRequestDetail d2
    INNER JOIN (
        SELECT
            p.ApprovalRequestId,
            p.ActivityId,
            MAX(ISNULL(p.DetailVersion, 1)) AS LatestVersion
        FROM dbo.SalesApprovalRequestDetail p
        WHERE p.ApprovalRequestId IN (1200, 1493)
        GROUP BY p.ApprovalRequestId, p.ActivityId
    ) ver
        ON ver.ApprovalRequestId = d2.ApprovalRequestId
       AND ver.ActivityId = d2.ActivityId
       AND ISNULL(d2.DetailVersion, 1) = ver.LatestVersion
    WHERE d2.ApprovalRequestId IN (1200, 1493)
    GROUP BY d2.ApprovalRequestId, d2.ActivityId
) latest
    ON latest.ApprovalRequestId = r.ApprovalRequestId
LEFT JOIN dbo.SalesApprovalRequestDetail d
    ON d.ApprovalRequestDetailId = latest.LatestDetailId
LEFT JOIN dbo.SalesDailyActivity a
    ON a.ActivityId = d.ActivityId
   AND a.ApprovalRequestId = d.ApprovalRequestId
GROUP BY
    r.ApprovalRequestId,
    r.RequesterLoginId,
    r.YearNo,
    r.MonthNo,
    r.Status,
    r.CurrentApproverLoginId,
    r.CurrentApproverLevel,
    r.ModifiedBy,
    expected.ApprovalRequestId,
    expected.RequesterLoginId
ORDER BY expected.ApprovalRequestId;

/* 10. Recorded Returned to Pending transitions for requests in the period. */
SELECT
    N'RecordedResubmissions' AS ResultSetName,
    h.HistoryId,
    h.ApprovalRequestId,
    r.RequesterLoginId,
    r.Status AS CurrentHeaderStatus,
    h.OldStatus,
    h.NewStatus,
    h.ChangedBy,
    h.ChangedOn,
    h.Remarks
FROM dbo.ApprovalRequestStatusHistory h
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = h.ApprovalRequestId
WHERE r.YearNo = @YearNo
  AND r.MonthNo = @MonthNo
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
  AND h.OldStatus = N'Returned'
  AND h.NewStatus = N'Pending'
ORDER BY h.HistoryId;

/*
    11. Requests with DetailVersion > 1.
    Intact lineage is visibility. Broken rows are also in BrokenPredecessors.
*/
SELECT
    N'VersionedDetailVisibility' AS ResultSetName,
    r.ApprovalRequestId,
    r.RequesterLoginId,
    r.Status AS HeaderStatus,
    COUNT(*) AS DetailsAboveVersion1,
    MAX(d.DetailVersion) AS MaxDetailVersion,
    SUM(CASE
            WHEN d.PreviousDetailId IS NULL
              OR pred.ApprovalRequestDetailId IS NULL
              OR pred.ActivityId <> d.ActivityId
              OR pred.ApprovalRequestId <> d.ApprovalRequestId
            THEN 1
            ELSE 0
        END) AS BrokenPredecessorCount
FROM dbo.SalesApprovalRequestDetail d
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = d.ApprovalRequestId
LEFT JOIN dbo.SalesApprovalRequestDetail pred
    ON pred.ApprovalRequestDetailId = d.PreviousDetailId
WHERE r.YearNo = @YearNo
  AND r.MonthNo = @MonthNo
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
  AND d.DetailVersion > 1
GROUP BY r.ApprovalRequestId, r.RequesterLoginId, r.Status
ORDER BY r.ApprovalRequestId;

/* 12. Category counts. Unresolved request count is distinct. */
SELECT
    N'CategorySummary' AS ResultSetName,
    collision.CollisionGroupCount,
    collision.CollisionRequestCount,
    disagreement.DisagreementCount,
    broken.BrokenPredecessorCount,
    unmatched.UnmatchedReturnedActivityCount,
    unmatched.UnmatchedWithNoRequestCount,
    unresolved.DistinctUnresolvedRequestCount,
    resubmission.ResubmissionRowCount,
    resubmission.ResubmissionRequestCount,
    versioned.VersionedRequestCount,
    population.RequestCount AS PopulationRequestCount,
    CASE
        WHEN @EnvironmentOk = 0 THEN N'BLOCKED'
        WHEN @FullSeptemberCensus = 0 THEN N'NOT TESTED'
        WHEN population.RequestCount = 571
         AND unresolved.DistinctUnresolvedRequestCount = 0
        THEN N'PASS'
        ELSE N'FAIL'
    END AS CensusClassification,
    CASE
        WHEN @EnvironmentOk = 0 THEN N'BLOCKED'
        WHEN unresolved.DistinctUnresolvedRequestCount = 0 THEN N'PASS'
        ELSE N'FAIL'
    END AS UnresolvedRegisterClassification
FROM (
    SELECT COUNT(*) AS RequestCount
    FROM dbo.SalesApprovalRequest r
    WHERE r.YearNo = @YearNo
      AND r.MonthNo = @MonthNo
      AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
) population
CROSS JOIN (
    SELECT
        COUNT(*) AS CollisionGroupCount,
        ISNULL(SUM(g.ActiveHeaderCount), 0) AS CollisionRequestCount
    FROM (
        SELECT COUNT(*) AS ActiveHeaderCount
        FROM dbo.SalesApprovalRequest x
        WHERE x.YearNo = @YearNo
          AND x.MonthNo = @MonthNo
          AND (@RequesterLoginId IS NULL OR x.RequesterLoginId = @RequesterLoginId)
          AND UPPER(LTRIM(RTRIM(ISNULL(x.Status, N'')))) IN
              (N'RETURNED', N'PENDING', N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED')
        GROUP BY x.RequesterLoginId, x.YearNo, x.MonthNo
        HAVING COUNT(*) > 1
    ) g
) collision
CROSS JOIN (
    SELECT COUNT(*) AS DisagreementCount
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN (
        SELECT d2.ApprovalRequestId, d2.ActivityId, MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
        FROM dbo.SalesApprovalRequestDetail d2
        INNER JOIN (
            SELECT p.ApprovalRequestId, p.ActivityId, MAX(ISNULL(p.DetailVersion, 1)) AS LatestVersion
            FROM dbo.SalesApprovalRequestDetail p
            INNER JOIN dbo.SalesApprovalRequest hr
                ON hr.ApprovalRequestId = p.ApprovalRequestId
            WHERE hr.YearNo = @YearNo
              AND hr.MonthNo = @MonthNo
              AND (@RequesterLoginId IS NULL OR hr.RequesterLoginId = @RequesterLoginId)
            GROUP BY p.ApprovalRequestId, p.ActivityId
        ) v
            ON v.ApprovalRequestId = d2.ApprovalRequestId
           AND v.ActivityId = d2.ActivityId
           AND ISNULL(d2.DetailVersion, 1) = v.LatestVersion
        GROUP BY d2.ApprovalRequestId, d2.ActivityId
    ) latest
        ON latest.LatestDetailId = d.ApprovalRequestDetailId
    INNER JOIN dbo.SalesApprovalRequest r
        ON r.ApprovalRequestId = d.ApprovalRequestId
    INNER JOIN dbo.SalesDailyActivity act
        ON act.ActivityId = d.ActivityId
       AND act.ApprovalRequestId = d.ApprovalRequestId
    WHERE r.YearNo = @YearNo
      AND r.MonthNo = @MonthNo
      AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
      AND d.DetailStatus = N'Returned'
      AND ISNULL(act.ClaimStatus, N'') <> N'Returned'
) disagreement
CROSS JOIN (
    SELECT COUNT(*) AS BrokenPredecessorCount
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN dbo.SalesApprovalRequest r
        ON r.ApprovalRequestId = d.ApprovalRequestId
    LEFT JOIN dbo.SalesApprovalRequestDetail pred
        ON pred.ApprovalRequestDetailId = d.PreviousDetailId
    WHERE r.YearNo = @YearNo
      AND r.MonthNo = @MonthNo
      AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
      AND (
            (d.DetailVersion > 1 AND d.PreviousDetailId IS NULL)
            OR (
                d.PreviousDetailId IS NOT NULL
                AND (
                    pred.ApprovalRequestDetailId IS NULL
                    OR pred.ActivityId <> d.ActivityId
                    OR pred.ApprovalRequestId <> d.ApprovalRequestId
                )
            )
          )
) broken
CROSS JOIN (
    SELECT
        COUNT(*) AS UnmatchedReturnedActivityCount,
        ISNULL(SUM(CASE WHEN a.ApprovalRequestId IS NULL OR owner.ApprovalRequestId IS NULL THEN 1 ELSE 0 END), 0) AS UnmatchedWithNoRequestCount
    FROM dbo.SalesDailyActivity a
    LEFT JOIN dbo.SalesApprovalRequest owner
        ON owner.ApprovalRequestId = a.ApprovalRequestId
    WHERE a.ClaimStatus = N'Returned'
      AND a.ActivityDate >= @PeriodStart
      AND a.ActivityDate < @PeriodEnd
      AND (@RequesterLoginId IS NULL OR a.Login_Id = @RequesterLoginId)
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.SalesApprovalRequest periodRequest
          WHERE periodRequest.ApprovalRequestId = a.ApprovalRequestId
            AND periodRequest.YearNo = @YearNo
            AND periodRequest.MonthNo = @MonthNo
            AND (@RequesterLoginId IS NULL OR periodRequest.RequesterLoginId = @RequesterLoginId)
      )
) unmatched
CROSS JOIN (
    SELECT COUNT(*) AS DistinctUnresolvedRequestCount
    FROM (
        SELECT DISTINCT src.ApprovalRequestId
        FROM (
            SELECT r.ApprovalRequestId
            FROM dbo.SalesApprovalRequest r
            INNER JOIN (
                SELECT x.RequesterLoginId, x.YearNo, x.MonthNo
                FROM dbo.SalesApprovalRequest x
                WHERE x.YearNo = @YearNo
                  AND x.MonthNo = @MonthNo
                  AND (@RequesterLoginId IS NULL OR x.RequesterLoginId = @RequesterLoginId)
                  AND UPPER(LTRIM(RTRIM(ISNULL(x.Status, N'')))) IN
                      (N'RETURNED', N'PENDING', N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED')
                GROUP BY x.RequesterLoginId, x.YearNo, x.MonthNo
                HAVING COUNT(*) > 1
            ) g
                ON g.RequesterLoginId = r.RequesterLoginId
               AND g.YearNo = r.YearNo
               AND g.MonthNo = r.MonthNo
            WHERE UPPER(LTRIM(RTRIM(ISNULL(r.Status, N'')))) IN
                  (N'RETURNED', N'PENDING', N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED')

            UNION

            SELECT r.ApprovalRequestId
            FROM dbo.SalesApprovalRequestDetail d
            INNER JOIN (
                SELECT d2.ApprovalRequestId, d2.ActivityId, MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
                FROM dbo.SalesApprovalRequestDetail d2
                INNER JOIN (
                    SELECT p.ApprovalRequestId, p.ActivityId, MAX(ISNULL(p.DetailVersion, 1)) AS LatestVersion
                    FROM dbo.SalesApprovalRequestDetail p
                    INNER JOIN dbo.SalesApprovalRequest hr
                        ON hr.ApprovalRequestId = p.ApprovalRequestId
                    WHERE hr.YearNo = @YearNo
                      AND hr.MonthNo = @MonthNo
                      AND (@RequesterLoginId IS NULL OR hr.RequesterLoginId = @RequesterLoginId)
                    GROUP BY p.ApprovalRequestId, p.ActivityId
                ) v
                    ON v.ApprovalRequestId = d2.ApprovalRequestId
                   AND v.ActivityId = d2.ActivityId
                   AND ISNULL(d2.DetailVersion, 1) = v.LatestVersion
                GROUP BY d2.ApprovalRequestId, d2.ActivityId
            ) latest
                ON latest.LatestDetailId = d.ApprovalRequestDetailId
            INNER JOIN dbo.SalesApprovalRequest r
                ON r.ApprovalRequestId = d.ApprovalRequestId
            INNER JOIN dbo.SalesDailyActivity act
                ON act.ActivityId = d.ActivityId
               AND act.ApprovalRequestId = d.ApprovalRequestId
            WHERE r.YearNo = @YearNo
              AND r.MonthNo = @MonthNo
              AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
              AND d.DetailStatus = N'Returned'
              AND ISNULL(act.ClaimStatus, N'') <> N'Returned'

            UNION

            SELECT r.ApprovalRequestId
            FROM dbo.SalesApprovalRequestDetail d
            INNER JOIN dbo.SalesApprovalRequest r
                ON r.ApprovalRequestId = d.ApprovalRequestId
            LEFT JOIN dbo.SalesApprovalRequestDetail pred
                ON pred.ApprovalRequestDetailId = d.PreviousDetailId
            WHERE r.YearNo = @YearNo
              AND r.MonthNo = @MonthNo
              AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
              AND (
                    (d.DetailVersion > 1 AND d.PreviousDetailId IS NULL)
                    OR (
                        d.PreviousDetailId IS NOT NULL
                        AND (
                            pred.ApprovalRequestDetailId IS NULL
                            OR pred.ActivityId <> d.ActivityId
                            OR pred.ApprovalRequestId <> d.ApprovalRequestId
                        )
                    )
                  )

            UNION

            SELECT owner.ApprovalRequestId
            FROM dbo.SalesDailyActivity a
            INNER JOIN dbo.SalesApprovalRequest owner
                ON owner.ApprovalRequestId = a.ApprovalRequestId
            WHERE a.ClaimStatus = N'Returned'
              AND a.ActivityDate >= @PeriodStart
              AND a.ActivityDate < @PeriodEnd
              AND (@RequesterLoginId IS NULL OR a.Login_Id = @RequesterLoginId)
              AND NOT (
                  owner.YearNo = @YearNo
                  AND owner.MonthNo = @MonthNo
                  AND (@RequesterLoginId IS NULL OR owner.RequesterLoginId = @RequesterLoginId)
              )
        ) src
    ) ids
) unresolved
CROSS JOIN (
    SELECT
        COUNT(*) AS ResubmissionRowCount,
        COUNT(DISTINCT h.ApprovalRequestId) AS ResubmissionRequestCount
    FROM dbo.ApprovalRequestStatusHistory h
    INNER JOIN dbo.SalesApprovalRequest r
        ON r.ApprovalRequestId = h.ApprovalRequestId
    WHERE r.YearNo = @YearNo
      AND r.MonthNo = @MonthNo
      AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
      AND h.OldStatus = N'Returned'
      AND h.NewStatus = N'Pending'
) resubmission
CROSS JOIN (
    SELECT COUNT(DISTINCT d.ApprovalRequestId) AS VersionedRequestCount
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN dbo.SalesApprovalRequest r
        ON r.ApprovalRequestId = d.ApprovalRequestId
    WHERE r.YearNo = @YearNo
      AND r.MonthNo = @MonthNo
      AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
      AND d.DetailVersion > 1
) versioned;
