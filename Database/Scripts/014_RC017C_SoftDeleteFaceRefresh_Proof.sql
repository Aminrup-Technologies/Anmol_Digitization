/* ============================================================================
   NAME:        RC-017C
   WHEN:        2026-09-24
   WHY:         Soft-delete left header TotalExpenseAmount / day Snapshot_* stale
                (RC-017B #1136: Header 9477 vs Latest Snapshot 6375 = deleted drafts).
   WHAT:        Read-only proof that applying the soft-delete face-refresh contract
                (same math as usp_UpdateSalesDailyExpenseStatus) would set
                TotalExpenseAmount = SUM(latest Snapshot_ExpenseTotal) without
                touching FinalApprovedAmount / Approved*.
   OWNERSHIP:   Face totals only. Does NOT EXEC usp_RecalculateApprovalRequest.
   SCOPE:       Dry-run for ApprovalRequestId = 1136. No UPDATE unless @Apply = 1.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @ApprovalRequestId INT = 1136;
DECLARE @Apply BIT = 0; -- keep 0 for proof; set 1 only for explicit one-shot repair

DECLARE @BeforeTotalExpense DECIMAL(18,2);
DECLARE @BeforeFinal        DECIMAL(18,2);
DECLARE @BeforeClaim        DECIMAL(18,2);
DECLARE @PredictedExpense   DECIMAL(18,2);
DECLARE @PredictedClaim     DECIMAL(18,2);

SELECT
      @BeforeTotalExpense = r.TotalExpenseAmount
    , @BeforeClaim        = r.TotalClaimAmount
    , @BeforeFinal        = r.FinalApprovedAmount
FROM dbo.SalesApprovalRequest r
WHERE r.ApprovalRequestId = @ApprovalRequestId;

;WITH Latest AS
(
    SELECT
          d2.ActivityId
        , MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
    FROM dbo.SalesApprovalRequestDetail d2
    INNER JOIN (
        SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
        FROM dbo.SalesApprovalRequestDetail
        WHERE ApprovalRequestId = @ApprovalRequestId
        GROUP BY ActivityId
    ) lv
        ON lv.ActivityId = d2.ActivityId
       AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
    WHERE d2.ApprovalRequestId = @ApprovalRequestId
    GROUP BY d2.ActivityId
)
SELECT
      @PredictedClaim   = SUM(ISNULL(d.Snapshot_CalculatedClaimAmount, 0))
    , @PredictedExpense = SUM(ISNULL(d.Snapshot_ExpenseTotal, 0))
FROM dbo.SalesApprovalRequestDetail d
INNER JOIN Latest l
    ON d.ApprovalRequestDetailId = l.LatestDetailId
WHERE d.ApprovalRequestId = @ApprovalRequestId;

SELECT
      @ApprovalRequestId AS ApprovalRequestId
    , @BeforeTotalExpense AS Before_TotalExpenseAmount
    , @PredictedExpense AS Predicted_TotalExpenseAmount
    , @BeforeClaim AS Before_TotalClaimAmount
    , @PredictedClaim AS Predicted_TotalClaimAmount
    , @BeforeFinal AS Before_FinalApprovedAmount
    , @BeforeFinal AS Predicted_FinalApprovedAmount_Unchanged
    , CASE
          WHEN @PredictedExpense = 6375.00
           AND @BeforeFinal = 14782.14
          THEN N'PASS'
          ELSE N'FAIL'
      END AS RC017C_1136_Proof;

IF @Apply = 1
BEGIN
    BEGIN TRAN;

    /* Same header face rollup as SalesMonthlyReview.RefreshClaimFaceAfterSoftDelete
       (snapshots already current on #1136 from prior Recalc; repair Total* only). */
    UPDATE dbo.SalesApprovalRequest
    SET
          TotalClaimAmount   = @PredictedClaim
        , TotalExpenseAmount = @PredictedExpense
        , ModifiedOn         = SYSUTCDATETIME()
    WHERE ApprovalRequestId = @ApprovalRequestId;

    /* Guard: FinalApprovedAmount must not change */
    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND ISNULL(FinalApprovedAmount, -1) <> ISNULL(@BeforeFinal, -1)
    )
    BEGIN
        ROLLBACK TRAN;
        RAISERROR(N'RC-017C guard failed: FinalApprovedAmount changed.', 16, 1);
        RETURN;
    END;

    COMMIT TRAN;

    SELECT
          TotalExpenseAmount
        , TotalClaimAmount
        , FinalApprovedAmount
    FROM dbo.SalesApprovalRequest
    WHERE ApprovalRequestId = @ApprovalRequestId;
END;
