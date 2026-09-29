/* ============================================================================
   NAME:        RC-017B
   WHEN:        2026-09-24
   WHY:         Header TotalExpenseAmount can drift from SUM(latest Snapshot_ExpenseTotal)
                after soft-delete / Recalc paths that refresh snapshots but not Total*.
   WHAT:        Read-only LIVE audit: writers are documented in repo; this script
                quantifies affected requests and variance. No UPDATE.
   OWNERSHIP:   Total* intended as submit-owned submitted face totals (TDS / FSOT).
                usp_RecalculateApprovalRequest does NOT write Total*.
                usp_UpdateSalesDailyExpenseStatus currently co-writes Total* (rogue).
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;

/* -------------------------------------------------------------------------
   A) Request #1136 — header vs latest Snapshot_ExpenseTotal
--------------------------------------------------------------------------- */
;WITH Latest AS
(
    SELECT
          d2.ActivityId
        , MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
    FROM dbo.SalesApprovalRequestDetail d2
    INNER JOIN (
        SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
        FROM dbo.SalesApprovalRequestDetail
        WHERE ApprovalRequestId = 1136
        GROUP BY ActivityId
    ) lv
        ON lv.ActivityId = d2.ActivityId
       AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
    WHERE d2.ApprovalRequestId = 1136
    GROUP BY d2.ActivityId
)
SELECT
      r.ApprovalRequestId
    , r.Status
    , r.TotalExpenseAmount                          AS Header_TotalExpenseAmount
    , SUM(ISNULL(d.Snapshot_ExpenseTotal, 0))       AS Latest_Snapshot_ExpenseTotal
    , r.TotalExpenseAmount
      - SUM(ISNULL(d.Snapshot_ExpenseTotal, 0))     AS Variance
FROM dbo.SalesApprovalRequest r
INNER JOIN dbo.SalesApprovalRequestDetail d
    ON d.ApprovalRequestId = r.ApprovalRequestId
INNER JOIN Latest l
    ON d.ApprovalRequestDetailId = l.LatestDetailId
WHERE r.ApprovalRequestId = 1136
GROUP BY r.ApprovalRequestId, r.Status, r.TotalExpenseAmount;

/* -------------------------------------------------------------------------
   B) #1136 deleted draft expenses (IsDeleted=1) — expected variance source
--------------------------------------------------------------------------- */
SELECT
      e.ExpenseId
    , e.ActivityId
    , e.ExpenseType
    , e.Amount
    , e.ClaimStatus
    , e.IsDeleted
    , e.DeletedOn
    , e.DeleteReason
    , CAST(e.ExpenseDate AS date) AS ExpenseDate
FROM dbo.SalesDailyExpense e
INNER JOIN dbo.SalesApprovalRequestDetail d
    ON d.ActivityId = e.ActivityId
WHERE d.ApprovalRequestId = 1136
  AND e.IsDeleted = 1
ORDER BY e.DeletedOn, e.ExpenseId;

SELECT
      SUM(ISNULL(e.Amount, 0)) AS DeletedDraftExpense_Sum
FROM dbo.SalesDailyExpense e
INNER JOIN (
    SELECT DISTINCT ActivityId
    FROM dbo.SalesApprovalRequestDetail
    WHERE ApprovalRequestId = 1136
) a ON a.ActivityId = e.ActivityId
WHERE e.IsDeleted = 1;

/* -------------------------------------------------------------------------
   C) LIVE drift: Header TotalExpenseAmount != SUM(latest Snapshot_ExpenseTotal)
      Scope: all non-terminal statuses commonly treated as live workflow
      (Pending, Returned, Submitted). Adjust if product wants Completed too.
--------------------------------------------------------------------------- */
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
    GROUP BY d2.ApprovalRequestId, d2.ActivityId
),
Rollup AS
(
    SELECT
          r.ApprovalRequestId
        , r.Status
        , r.RequesterLoginId
        , r.YearNo
        , r.MonthNo
        , ISNULL(r.TotalExpenseAmount, 0) AS Header_TotalExpenseAmount
        , ISNULL(SUM(ISNULL(d.Snapshot_ExpenseTotal, 0)), 0) AS Latest_Snapshot_ExpenseTotal
    FROM dbo.SalesApprovalRequest r
    INNER JOIN Latest l
        ON l.ApprovalRequestId = r.ApprovalRequestId
    INNER JOIN dbo.SalesApprovalRequestDetail d
        ON d.ApprovalRequestDetailId = l.LatestDetailId
    WHERE r.Status IN (N'Pending', N'Returned', N'Submitted')
    GROUP BY
          r.ApprovalRequestId
        , r.Status
        , r.RequesterLoginId
        , r.YearNo
        , r.MonthNo
        , r.TotalExpenseAmount
)
SELECT
      ApprovalRequestId
    , Status
    , RequesterLoginId
    , YearNo
    , MonthNo
    , Header_TotalExpenseAmount
    , Latest_Snapshot_ExpenseTotal
    , Header_TotalExpenseAmount - Latest_Snapshot_ExpenseTotal AS Variance
INTO #LiveDrift
FROM Rollup
WHERE Header_TotalExpenseAmount <> Latest_Snapshot_ExpenseTotal;

SELECT *
FROM #LiveDrift
ORDER BY ABS(Variance) DESC;

SELECT
      COUNT(*) AS AffectedRequestCount
    , SUM(ABS(Variance)) AS AbsVariance_Sum
    , SUM(Variance) AS SignedVariance_Sum
FROM #LiveDrift;

SELECT
      Status
    , COUNT(*) AS AffectedRequestCount
    , SUM(ABS(Variance)) AS AbsVariance_Sum
    , SUM(Variance) AS SignedVariance_Sum
FROM #LiveDrift
GROUP BY Status
ORDER BY Status;

/* Optional: ReadyForPayment / Completed (payment-lock statuses) */
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
    GROUP BY d2.ApprovalRequestId, d2.ActivityId
),
Rollup AS
(
    SELECT
          r.ApprovalRequestId
        , r.Status
        , ISNULL(r.TotalExpenseAmount, 0) AS Header_TotalExpenseAmount
        , ISNULL(SUM(ISNULL(d.Snapshot_ExpenseTotal, 0)), 0) AS Latest_Snapshot_ExpenseTotal
    FROM dbo.SalesApprovalRequest r
    INNER JOIN Latest l
        ON l.ApprovalRequestId = r.ApprovalRequestId
    INNER JOIN dbo.SalesApprovalRequestDetail d
        ON d.ApprovalRequestDetailId = l.LatestDetailId
    WHERE r.Status IN (N'ReadyForPayment', N'Completed')
    GROUP BY r.ApprovalRequestId, r.Status, r.TotalExpenseAmount
)
SELECT
      Status
    , COUNT(*) AS AffectedRequestCount
    , SUM(ABS(Header_TotalExpenseAmount - Latest_Snapshot_ExpenseTotal)) AS AbsVariance_Sum
    , SUM(Header_TotalExpenseAmount - Latest_Snapshot_ExpenseTotal) AS SignedVariance_Sum
FROM Rollup
WHERE Header_TotalExpenseAmount <> Latest_Snapshot_ExpenseTotal
GROUP BY Status
ORDER BY Status;
