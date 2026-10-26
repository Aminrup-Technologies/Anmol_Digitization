/*
    M2562 request 1493 recovery wrapper.
    Server:   MUMANMOAPP01\SQLEXPRESS
    Database: AnmolDristi

    This file is the reviewed ReadyForPayment -> Returned transaction.
    It was not previously stored under sql/changes. The transaction body
    is unchanged from that review.

    @CommitChanges stays 0. A preview run rolls back.
    Do not set @CommitChanges to 1 in this file. A commit requires a
    separate execution after the preview result sets have been accepted.

    @AuthorizedOperator and @Reason must be supplied for a preview that
    reaches the data statements. Blank values stop at error 54103 and
    write nothing. The values below are the preview record only.
*/
DECLARE @CommitChanges bit = 0;
DECLARE @ApprovalRequestId int = 1493;
DECLARE @ExpectedStatus nvarchar(50) = N'ReadyForPayment';
DECLARE @AuthorizedOperator nvarchar(100) = N'PREVIEW';
DECLARE @Reason nvarchar(1000) = N'Preview only. Return request 1493 for M2562, September 2026, from ReadyForPayment to Returned so the employee can correct and resubmit. This execution rolls back.';

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 5000;

IF DB_NAME() <> N'AnmolDristi' OR @@SERVERNAME <> N'MUMANMOAPP01\SQLEXPRESS'
    THROW 54101, 'Session is not MUMANMOAPP01\SQLEXPRESS / AnmolDristi.', 1;
IF @ApprovalRequestId <> 1493 OR @ExpectedStatus <> N'ReadyForPayment'
    THROW 54102, 'Request id or expected status does not match the reviewed recovery.', 1;
IF NULLIF(LTRIM(RTRIM(@AuthorizedOperator)), N'') IS NULL OR NULLIF(LTRIM(RTRIM(@Reason)), N'') IS NULL
    THROW 54103, 'Authorized operator and documented reason are required.', 1;

BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.SalesApprovalRequest WITH (UPDLOCK, HOLDLOCK)
    WHERE ApprovalRequestId = 1493
      AND RequesterLoginId = N'M2562'
      AND YearNo = 2026 AND MonthNo = 9
      AND Status = N'ReadyForPayment'
      AND CurrentApproverLoginId = N'Audit'
      AND CurrentApproverLevel = 2
      AND FinalApprovedAmount = 2648.35
      AND ApprovedClaimAmount = 2648.35
      AND TotalClaimAmount = 2648.35
)
    THROW 54104, 'Header 1493 is no longer the reviewed ReadyForPayment row.', 1;

IF EXISTS (
    SELECT 1 FROM dbo.SalesApprovalRequest
    WHERE RequesterLoginId = N'M2562' AND YearNo = 2026 AND MonthNo = 9
      AND ApprovalRequestId <> 1493
      AND Status IN (N'Returned', N'Pending', N'Completed', N'ReadyForPayment', N'Processed')
)
    THROW 54105, 'Another active September 2026 request exists for M2562.', 1;

IF NOT EXISTS (
    SELECT 1 FROM dbo.SalesApprovalRequestApprover
    WHERE ApprovalRequestApproverId = 2957 AND ApprovalRequestId = 1493
      AND ApproverLoginId = N'M2641' AND ApproverRole = N'Reporting_Manager1'
      AND ApproverLevel = 1 AND Status = N'Approved' AND ActionBy = N'M2641'
) OR NOT EXISTS (
    SELECT 1 FROM dbo.SalesApprovalRequestApprover
    WHERE ApprovalRequestApproverId = 2958 AND ApprovalRequestId = 1493
      AND ApproverLoginId = N'Audit' AND ApproverRole = N'Auditor'
      AND ApproverLevel = 2 AND Status = N'Approved' AND ActionBy = N'Audit'
)
    THROW 54106, 'Approver rows 2957 and 2958 are no longer the reviewed approvals.', 1;

IF (SELECT COUNT(*) FROM dbo.SalesApprovalRequestDetail WHERE ApprovalRequestId = 1493) <> 30
   OR EXISTS (
        SELECT 1 FROM dbo.SalesApprovalRequestDetail
        WHERE ApprovalRequestId = 1493
          AND (ApprovalRequestDetailId < 45123 OR ApprovalRequestDetailId > 45152
               OR ISNULL(DetailVersion, 1) <> 1 OR PreviousDetailId IS NOT NULL
               OR DetailStatus <> N'Approved' OR DetailApprovedBy <> N'Audit'
               OR ISNULL(DetailApprovedLevel, 0) <> 2)
   )
    THROW 54107, 'Latest details are no longer version 1, ids 45123-45152, approved by Audit at level 2.', 1;

IF (SELECT COUNT(*) FROM dbo.SalesDailyActivity a
    WHERE a.ActivityId IN (SELECT ActivityId FROM dbo.SalesApprovalRequestDetail WHERE ApprovalRequestId = 1493)
) <> 30
   OR EXISTS (
        SELECT 1 FROM dbo.SalesDailyActivity a
        WHERE a.ActivityId IN (SELECT ActivityId FROM dbo.SalesApprovalRequestDetail WHERE ApprovalRequestId = 1493)
          AND (a.Login_Id <> N'M2562' OR a.ApprovalRequestId <> 1493 OR a.ClaimStatus <> N'Approved'
               OR YEAR(a.ActivityDate) <> 2026 OR MONTH(a.ActivityDate) <> 9)
   )
    THROW 54108, 'Activities are no longer the 30 approved September rows owned by request 1493.', 1;

IF EXISTS (
    SELECT 1 FROM dbo.SalesDailyExpense e
    WHERE e.ActivityId IN (SELECT ActivityId FROM dbo.SalesApprovalRequestDetail WHERE ApprovalRequestId = 1493)
)
    THROW 54109, 'Expense rows appeared after the review.', 1;

UPDATE dbo.SalesApprovalRequest
SET Status = N'Returned',
    CurrentApproverLoginId = N'M2641',
    CurrentApproverLevel = 1,
    ModifiedBy = @AuthorizedOperator,
    ModifiedOn = SYSUTCDATETIME()
WHERE ApprovalRequestId = 1493
  AND Status = N'ReadyForPayment'
  AND CurrentApproverLoginId = N'Audit'
  AND CurrentApproverLevel = 2
  AND FinalApprovedAmount = 2648.35;
IF @@ROWCOUNT <> 1 THROW 54110, 'Header transition did not match the expected ReadyForPayment row.', 1;

UPDATE d
SET d.DetailStatus = N'Returned'
FROM dbo.SalesApprovalRequestDetail d
WHERE d.ApprovalRequestId = 1493
  AND d.ApprovalRequestDetailId BETWEEN 45123 AND 45152
  AND d.DetailVersion = 1 AND d.PreviousDetailId IS NULL
  AND d.DetailStatus = N'Approved' AND d.DetailApprovedBy = N'Audit' AND d.DetailApprovedLevel = 2;
IF @@ROWCOUNT <> 30 THROW 54111, 'Latest detail return count was not 30.', 1;

UPDATE a
SET a.ClaimStatus = N'Returned',
    a.ModifiedByUserId = @AuthorizedOperator,
    a.ModifiedDateTime = DATEADD(MINUTE, 330, SYSUTCDATETIME())
FROM dbo.SalesDailyActivity a
WHERE a.ApprovalRequestId = 1493 AND a.Login_Id = N'M2562' AND a.ClaimStatus = N'Approved'
  AND YEAR(a.ActivityDate) = 2026 AND MONTH(a.ActivityDate) = 9
  AND a.ActivityId IN (
        SELECT ActivityId FROM dbo.SalesApprovalRequestDetail
        WHERE ApprovalRequestId = 1493 AND ApprovalRequestDetailId BETWEEN 45123 AND 45152
  );
IF @@ROWCOUNT <> 30 THROW 54112, 'Activity return count was not 30.', 1;

INSERT INTO dbo.ApprovalRequestStatusHistory
    (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, Remarks)
VALUES
    (1493, N'ReadyForPayment', N'Returned', @AuthorizedOperator, @Reason);

IF NOT EXISTS (
    SELECT 1 FROM dbo.SalesApprovalRequest
    WHERE ApprovalRequestId = 1493 AND Status = N'Returned'
      AND CurrentApproverLoginId = N'M2641' AND CurrentApproverLevel = 1
      AND FinalApprovedAmount = 2648.35 AND ApprovedClaimAmount = 2648.35
      AND TotalClaimAmount = 2648.35
) THROW 54113, 'Returned header, L1 pointer, or stored amount failed validation.', 1;

IF EXISTS (
    SELECT 1 FROM dbo.SalesApprovalRequestApprover
    WHERE ApprovalRequestId = 1493 AND Status <> N'Approved'
) THROW 54114, 'An original approver row was changed.', 1;

IF EXISTS (
    SELECT 1 FROM dbo.SalesApprovalRequestDetail
    WHERE ApprovalRequestId = 1493
      AND (DetailStatus <> N'Returned' OR DetailApprovedBy <> N'Audit'
           OR DetailApprovedLevel <> 2 OR ISNULL(DetailVersion, 1) <> 1)
) THROW 54115, 'Detail status, approver, level, or version failed validation.', 1;

IF (SELECT COUNT(*) FROM dbo.SalesDailyActivity
    WHERE ApprovalRequestId = 1493 AND ClaimStatus = N'Returned' AND Login_Id = N'M2562') <> 30
    THROW 54116, 'Activity returned state failed validation.', 1;

IF (SELECT COUNT(*) FROM dbo.ApprovalRequestStatusHistory
    WHERE HistoryId IN (5254, 5388, 6230, 6231) AND ApprovalRequestId = 1493) <> 4
    THROW 54117, 'An existing approval-history row is missing.', 1;

IF EXISTS (
    SELECT 1
    FROM dbo.SalesApprovalRequest sar
    INNER JOIN dbo.SalesApprovalRequestApprover saa
        ON saa.ApprovalRequestId = sar.ApprovalRequestId
       AND saa.Status = N'Pending'
       AND saa.ApproverLoginId = sar.CurrentApproverLoginId
    WHERE sar.ApprovalRequestId = 1493 AND sar.Status = N'Pending'
) THROW 54118, 'Returned claim matches the L1 or Auditor queue predicate.', 1;

SELECT r.ApprovalRequestId, r.Status, r.CurrentApproverLoginId, r.CurrentApproverLevel,
       r.FinalApprovedAmount, r.ModifiedBy
FROM dbo.SalesApprovalRequest r
WHERE r.ApprovalRequestId = 1493;

IF @CommitChanges = 1 COMMIT TRANSACTION;
ELSE ROLLBACK TRANSACTION;

SELECT DB_NAME() AS DatabaseName,
       @@SERVERNAME AS ServerName,
       @@TRANCOUNT AS OpenTransactions,
       r.Status,
       r.CurrentApproverLoginId,
       r.CurrentApproverLevel,
       r.FinalApprovedAmount,
       r.ModifiedBy
FROM dbo.SalesApprovalRequest r
WHERE r.ApprovalRequestId = 1493;
