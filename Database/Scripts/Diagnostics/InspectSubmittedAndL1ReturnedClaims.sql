/*
    Purpose:
        Read-only list of TA/DA claims submitted for approval, with L1 returns
        identified separately from claims still awaiting approval and from
        L1 rejections.

    Execution:
        Run in SSMS against the intended database.
        SELECT only. No inserts, updates, deletes, or procedure execution.

    Schema checked:
        dbo.SalesApprovalRequest
        dbo.SalesApprovalRequestApprover
        dbo.SalesApprovalRequestDetail
        dbo.ApprovalRequestStatusHistory (ChangedOn, Remarks, ApproverLevel, ActionType)
        dbo.MST_UserMaster (EmployeeCode, EmployeeName)

    How an L1 return is recognized:
        Level 1 is the L1 step. Its ApproverRole is L1 on current rows and
        Reporting_Manager1 on older rows.

        Current open return:
            SalesApprovalRequestApprover.Status = Returned at ApproverLevel = 1,
            and the header Status is Returned.
            usp_SubmitSalesDailyActivities clears that approver Status and
            Remarks on resubmit, so this signal covers claims not yet resubmitted.

        Recorded return:
            ApprovalRequestStatusHistory.NewStatus = Returned, written by the
            level-1 approver. usp_ReturnClaimToEmployee and
            usp_ProcessApprovalAction do not store ApproverLevel on that
            header history row, so the actor is matched to the level-1
            ApproverLoginId. Rows that do store ApproverLevel = 1 are included.

        Older whole-claim returns:
            Some history rows used NewStatus = Rejected with remarks beginning
            "Global Return to Employee". A header can also remain Rejected
            after a level-1 NewStatus = Returned event. Both are returns when
            no separate level-1 rejection is recorded. A level-1
            NewStatus = Rejected without that remark is an L1 rejection.

        Line remarks such as "Row Returned" stay on NewStatus = Pending and
        are not treated as a request-level L1 return. Result set 3 shows
        detail-level Returned counts.

    These results are a diagnostic reading of stored status and history.
    They are not a certified reconciliation of the live approval queue.
*/

SET NOCOUNT ON;

DECLARE @Year INT = NULL;  -- Example: 2026. NULL = all years.
DECLARE @Month INT = NULL; -- Example: 8. NULL = all months.

/* ================================================================
   RESULT SET 1: REQUEST STATUS, CURRENT APPROVER, L1 RETURN
   ================================================================ */

;WITH RequestScope AS
(
    SELECT
          r.ApprovalRequestId
        , r.RequesterLoginId
        , r.YearNo
        , r.MonthNo
        , r.RequestDate
        , r.Status
        , r.CurrentApproverLevel
        , r.CurrentApproverLoginId
        , r.TotalClaimAmount
        , r.TotalExpenseAmount
    FROM dbo.SalesApprovalRequest r
    WHERE (@Year IS NULL OR r.YearNo = @Year)
      AND (@Month IS NULL OR r.MonthNo = @Month)
),
Level1Approver AS
(
    SELECT
          a.ApprovalRequestId
        , a.ApproverLoginId
        , a.ApproverRole
        , a.Status
        , a.Remarks
        , a.ActionBy
        , a.ActionDate
        , ROW_NUMBER() OVER
          (
              PARTITION BY a.ApprovalRequestId
              ORDER BY
                    CASE a.Status
                        WHEN N'Returned' THEN 0
                        WHEN N'Rejected' THEN 1
                        ELSE 2
                    END
                  , a.ApprovalRequestApproverId
          ) AS rn
    FROM dbo.SalesApprovalRequestApprover a
    INNER JOIN RequestScope s
        ON s.ApprovalRequestId = a.ApprovalRequestId
    WHERE a.ApproverLevel = 1
),
HistoryFlags AS
(
    SELECT
          h.ApprovalRequestId
        , SUM
          (
              CASE
                  WHEN h.NewStatus = N'Returned'
                   AND
                   (
                        h.ApproverLevel = 1
                     OR EXISTS
                        (
                            SELECT 1
                            FROM dbo.SalesApprovalRequestApprover a
                            WHERE a.ApprovalRequestId = h.ApprovalRequestId
                              AND a.ApproverLevel = 1
                              AND a.ApproverLoginId IS NOT NULL
                              AND UPPER(LTRIM(RTRIM(a.ApproverLoginId)))
                                  = UPPER(LTRIM(RTRIM(h.ChangedBy)))
                        )
                   )
                  THEN 1
                  ELSE 0
              END
          ) AS ReturnedEventCount
        , SUM
          (
              CASE
                  WHEN h.NewStatus = N'Rejected'
                   AND h.Remarks LIKE N'Global Return to Employee%'
                   AND
                   (
                        h.ApproverLevel = 1
                     OR EXISTS
                        (
                            SELECT 1
                            FROM dbo.SalesApprovalRequestApprover a
                            WHERE a.ApprovalRequestId = h.ApprovalRequestId
                              AND a.ApproverLevel = 1
                              AND a.ApproverLoginId IS NOT NULL
                              AND UPPER(LTRIM(RTRIM(a.ApproverLoginId)))
                                  = UPPER(LTRIM(RTRIM(h.ChangedBy)))
                        )
                   )
                  THEN 1
                  ELSE 0
              END
          ) AS LegacyReturnCount
        , SUM
          (
              CASE
                  WHEN h.NewStatus = N'Rejected'
                   AND ISNULL(h.Remarks, N'') NOT LIKE N'Global Return to Employee%'
                   AND
                   (
                        h.ApproverLevel = 1
                     OR EXISTS
                        (
                            SELECT 1
                            FROM dbo.SalesApprovalRequestApprover a
                            WHERE a.ApprovalRequestId = h.ApprovalRequestId
                              AND a.ApproverLevel = 1
                              AND a.ApproverLoginId IS NOT NULL
                              AND UPPER(LTRIM(RTRIM(a.ApproverLoginId)))
                                  = UPPER(LTRIM(RTRIM(h.ChangedBy)))
                        )
                   )
                  THEN 1
                  ELSE 0
              END
          ) AS RejectEventCount
    FROM dbo.ApprovalRequestStatusHistory h
    INNER JOIN RequestScope s
        ON s.ApprovalRequestId = h.ApprovalRequestId
    GROUP BY h.ApprovalRequestId
)
SELECT
      s.ApprovalRequestId
    , s.RequesterLoginId AS EmployeeCode
    , um.EmployeeName
    , um.ReportingManager
    , um.ReportingManagerCode
    , s.YearNo
    , s.MonthNo
    , CASE
          WHEN s.YearNo BETWEEN 1 AND 9999
           AND s.MonthNo BETWEEN 1 AND 12
          THEN DATENAME(MONTH, DATEFROMPARTS(s.YearNo, s.MonthNo, 1))
      END AS ClaimMonth
    , s.RequestDate
    , s.Status AS RequestStatus
    , s.CurrentApproverLevel
    , s.CurrentApproverLoginId
    , ap.EmployeeName AS CurrentApproverName
    , l1.ApproverLoginId AS L1ApproverLoginId
    , l1n.EmployeeName AS L1ApproverName
    , l1.ApproverRole AS L1ApproverRole
    , l1.Status AS L1ApproverStatus
    , CASE
          WHEN l1.Status = N'Returned'
            OR (s.Status = N'Returned' AND ISNULL(s.CurrentApproverLevel, 0) = 1)
          THEN N'L1 Returned'

          WHEN s.Status = N'Rejected'
           AND (ISNULL(hf.RejectEventCount, 0) > 0 OR l1.Status = N'Rejected')
          THEN N'L1 Rejected'

          WHEN s.Status = N'Rejected'
           AND (ISNULL(hf.ReturnedEventCount, 0) + ISNULL(hf.LegacyReturnCount, 0)) > 0
          THEN N'L1 Returned (legacy Rejected status)'

          WHEN s.Status IN (N'Pending', N'Submitted')
           AND (ISNULL(hf.ReturnedEventCount, 0) + ISNULL(hf.LegacyReturnCount, 0)) > 0
          THEN N'Submitted / Pending Approval (prior L1 return)'

          WHEN s.Status IN (N'Pending', N'Submitted')
          THEN N'Submitted / Pending Approval'

          WHEN s.Status = N'Completed'
           AND (ISNULL(hf.ReturnedEventCount, 0) + ISNULL(hf.LegacyReturnCount, 0)) > 0
          THEN N'Completed (prior L1 return)'

          WHEN s.Status = N'Completed'
          THEN N'Completed'

          WHEN s.Status = N'ReadyForPayment'
           AND (ISNULL(hf.ReturnedEventCount, 0) + ISNULL(hf.LegacyReturnCount, 0)) > 0
          THEN N'Ready for Payment (prior L1 return)'

          WHEN s.Status = N'ReadyForPayment'
          THEN N'Ready for Payment'

          WHEN (ISNULL(hf.ReturnedEventCount, 0) + ISNULL(hf.LegacyReturnCount, 0)) > 0
          THEN CONCAT(ISNULL(s.Status, N'Unknown'), N' (prior L1 return)')

          ELSE ISNULL(s.Status, N'Unknown')
      END AS DiagnosticClassification
    , ISNULL(hf.ReturnedEventCount, 0) + ISNULL(hf.LegacyReturnCount, 0) AS L1ReturnCount
    , latestReturn.ChangedOn AS LatestL1ReturnOn
    , latestReturn.ChangedBy AS LatestL1ReturnBy
    , COALESCE
      (
            latestReturn.Remarks
          , CASE WHEN l1.Status = N'Returned' THEN l1.Remarks END
      ) AS ReturnRemarks
    , s.TotalClaimAmount
    , s.TotalExpenseAmount
    , ISNULL(s.TotalClaimAmount, 0) + ISNULL(s.TotalExpenseAmount, 0) AS DisplayClaimAmount
FROM RequestScope s
LEFT JOIN Level1Approver l1
    ON l1.ApprovalRequestId = s.ApprovalRequestId
   AND l1.rn = 1
LEFT JOIN HistoryFlags hf
    ON hf.ApprovalRequestId = s.ApprovalRequestId
LEFT JOIN dbo.MST_UserMaster um
    ON um.EmployeeCode = s.RequesterLoginId
LEFT JOIN dbo.MST_UserMaster ap
    ON ap.EmployeeCode = s.CurrentApproverLoginId
LEFT JOIN dbo.MST_UserMaster l1n
    ON l1n.EmployeeCode = l1.ApproverLoginId
OUTER APPLY
(
    SELECT TOP (1)
          h.ChangedOn
        , h.ChangedBy
        , h.Remarks
    FROM dbo.ApprovalRequestStatusHistory h
    WHERE h.ApprovalRequestId = s.ApprovalRequestId
      AND
      (
            (
                h.NewStatus = N'Returned'
                AND
                (
                     h.ApproverLevel = 1
                  OR EXISTS
                     (
                         SELECT 1
                         FROM dbo.SalesApprovalRequestApprover a
                         WHERE a.ApprovalRequestId = h.ApprovalRequestId
                           AND a.ApproverLevel = 1
                           AND a.ApproverLoginId IS NOT NULL
                           AND UPPER(LTRIM(RTRIM(a.ApproverLoginId)))
                               = UPPER(LTRIM(RTRIM(h.ChangedBy)))
                     )
                )
            )
         OR
            (
                h.NewStatus = N'Rejected'
                AND h.Remarks LIKE N'Global Return to Employee%'
                AND
                (
                     h.ApproverLevel = 1
                  OR EXISTS
                     (
                         SELECT 1
                         FROM dbo.SalesApprovalRequestApprover a
                         WHERE a.ApprovalRequestId = h.ApprovalRequestId
                           AND a.ApproverLevel = 1
                           AND a.ApproverLoginId IS NOT NULL
                           AND UPPER(LTRIM(RTRIM(a.ApproverLoginId)))
                               = UPPER(LTRIM(RTRIM(h.ChangedBy)))
                     )
                )
            )
      )
    ORDER BY h.ChangedOn DESC, h.HistoryId DESC
) latestReturn
ORDER BY
      s.RequestDate DESC
    , s.ApprovalRequestId DESC;


/* ================================================================
   RESULT SET 2: RETURN, REJECTION, AND RESUBMIT HISTORY
   ================================================================ */

SELECT
      h.HistoryId
    , h.ApprovalRequestId
    , r.RequesterLoginId AS EmployeeCode
    , um.EmployeeName
    , r.YearNo
    , r.MonthNo
    , h.OldStatus
    , h.NewStatus
    , h.ApproverLevel
    , h.ActionType
    , h.ChangedBy
    , h.ChangedOn
    , h.Remarks
    , CASE
          WHEN h.OldStatus = N'Returned'
           AND h.NewStatus = N'Pending'
          THEN N'Resubmitted after return'

          WHEN h.NewStatus = N'Rejected'
           AND h.Remarks LIKE N'Global Return to Employee%'
           AND
           (
                h.ApproverLevel = 1
             OR EXISTS
                (
                    SELECT 1
                    FROM dbo.SalesApprovalRequestApprover a
                    WHERE a.ApprovalRequestId = h.ApprovalRequestId
                      AND a.ApproverLevel = 1
                      AND a.ApproverLoginId IS NOT NULL
                      AND UPPER(LTRIM(RTRIM(a.ApproverLoginId)))
                          = UPPER(LTRIM(RTRIM(h.ChangedBy)))
                )
           )
          THEN N'L1 Returned (legacy Rejected status)'

          WHEN h.NewStatus = N'Returned'
           AND
           (
                h.ApproverLevel = 1
             OR EXISTS
                (
                    SELECT 1
                    FROM dbo.SalesApprovalRequestApprover a
                    WHERE a.ApprovalRequestId = h.ApprovalRequestId
                      AND a.ApproverLevel = 1
                      AND a.ApproverLoginId IS NOT NULL
                      AND UPPER(LTRIM(RTRIM(a.ApproverLoginId)))
                          = UPPER(LTRIM(RTRIM(h.ChangedBy)))
                )
           )
          THEN N'L1 Returned'

          WHEN h.NewStatus = N'Returned'
          THEN N'Returned at another level'

          WHEN h.NewStatus = N'Rejected'
           AND
           (
                h.ApproverLevel = 1
             OR EXISTS
                (
                    SELECT 1
                    FROM dbo.SalesApprovalRequestApprover a
                    WHERE a.ApprovalRequestId = h.ApprovalRequestId
                      AND a.ApproverLevel = 1
                      AND a.ApproverLoginId IS NOT NULL
                      AND UPPER(LTRIM(RTRIM(a.ApproverLoginId)))
                          = UPPER(LTRIM(RTRIM(h.ChangedBy)))
                )
           )
          THEN N'L1 Rejected'

          WHEN h.NewStatus = N'Rejected'
          THEN N'Rejected at another level'

          ELSE N'Other'
      END AS HistoryClassification
FROM dbo.ApprovalRequestStatusHistory h
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = h.ApprovalRequestId
LEFT JOIN dbo.MST_UserMaster um
    ON um.EmployeeCode = r.RequesterLoginId
WHERE
    (@Year IS NULL OR r.YearNo = @Year)
    AND (@Month IS NULL OR r.MonthNo = @Month)
    AND
    (
        h.NewStatus IN (N'Returned', N'Rejected')
        OR (h.OldStatus = N'Returned' AND h.NewStatus = N'Pending')
    )
ORDER BY
      h.ApprovalRequestId DESC
    , h.ChangedOn DESC
    , h.HistoryId DESC;


/* ================================================================
   RESULT SET 3: DETAIL-LEVEL STATUS COUNTS
   ================================================================ */

SELECT
      d.ApprovalRequestId
    , d.DetailStatus
    , COUNT_BIG(*) AS DetailCount
FROM dbo.SalesApprovalRequestDetail d
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = d.ApprovalRequestId
WHERE
    (@Year IS NULL OR r.YearNo = @Year)
    AND (@Month IS NULL OR r.MonthNo = @Month)
GROUP BY
      d.ApprovalRequestId
    , d.DetailStatus
ORDER BY
      d.ApprovalRequestId DESC
    , d.DetailStatus;
