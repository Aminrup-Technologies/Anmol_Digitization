/*
    Expense 2646 rejection persistence check.
    Authoring status: NOT EXECUTED.

    Database/Tests/AuditorReturnToL1/02_readonly_verification.sql remains the
    parked Auditor -> L1 read for approval request 118. It does not resolve an
    expense id, and it does not decide whether a line was persisted as Rejected.
    This script is that missing read. It does not extend the parked routing check.

    ClaimsApproval_v2.aspx/UpdateIndividualExpense calls
    dbo.usp_UpdateSalesDailyExpenseStatus and returns success when that call
    does not throw. This script does not call the procedure. A success payload
    is not evidence that SalesDailyExpense.ClaimStatus is Rejected.

    This script does not insert, update, delete, or merge application data.
    This script does not execute a stored procedure.
    This script does not begin, commit, or roll back a transaction.

    Accidental execution stops in the gate below. A reviewer sets
    @RunReadOnlyChecks to 1 only while connected to AnmolDristiDev.

    Snapshot amounts are columns on dbo.SalesApprovalRequestDetail. There is
    no separate expense snapshot table in the repository definitions.
    dbo.usp_GetApprovalRequestDetails reads those stored columns. It does not
    recompute them. The formulas below are the ones
    dbo.usp_UpdateSalesDailyExpenseStatus writes:

      type amount = SUM(Amount) for that activity
                    where IsDeleted = 0
                      and ISNULL(ClaimStatus, '') <> 'Rejected'
                      and ExpenseType is DA, Hotel, Travel, or Stationary
      Snapshot_ExpenseTotal = DA + Hotel + Travel + Stationary
      TotalClaimAmount = SUM of latest-detail Snapshot_CalculatedClaimAmount
      TotalExpenseAmount = SUM of latest-detail Snapshot_ExpenseTotal

    Latest detail for the expense activity follows that procedure:
    highest ISNULL(DetailVersion, 1), then highest ApprovalRequestDetailId.
    The header sums then use that same version rule inside the resolved request.
    The procedure does not write ApprovalRequestStatusHistory, DetailStatus,
    DetailVersion, or the header component columns TotalDAAmount,
    TotalHotelAmount, TotalTravelAmount, and TotalStationaryAmount.

    ClaimProcessedDate is stored as IST (UTC plus 330 minutes).
    SalesDailyExpense.ModifiedOn and ApprovalRequestStatusHistory.ChangedOn
    are UTC. History at or after the expense update is compared to ModifiedOn.
*/
SET NOCOUNT ON;

IF (@@OPTIONS & 2) = 2
BEGIN
    PRINT N'NOT RUN | IMPLICIT_TRANSACTIONS is ON. This script does not open a transaction.';
    RETURN;
END;

IF @@TRANCOUNT > 0
BEGIN
    PRINT N'NOT RUN | @@TRANCOUNT is already greater than 0.';
    RETURN;
END;

IF DB_NAME() = N'AnmolDristi'
BEGIN
    PRINT N'NOT RUN | Refusing live database AnmolDristi.';
    RETURN;
END;

IF DB_NAME() <> N'AnmolDristiDev'
BEGIN
    PRINT N'NOT RUN | Connected database is ' + ISNULL(DB_NAME(), N'<null>') + N'. Intended database is AnmolDristiDev.';
    RETURN;
END;

DECLARE @RunReadOnlyChecks bit = 0;

IF @RunReadOnlyChecks <> 1
BEGIN
    PRINT N'NOT RUN | Database=' + ISNULL(DB_NAME(), N'<null>')
        + N' | Set @RunReadOnlyChecks = 1 only for an authorized read of expense 2646 on AnmolDristiDev.';
    RETURN;
END;

DECLARE @ExpenseId int = 2646;
DECLARE @ExpectedExpenseType nvarchar(50) = N'Stationary';
DECLARE @ExpectedAmount decimal(18,2) = 180.00;

DECLARE @ExpenseFound bit = 0;
DECLARE @ActivityId int;
DECLARE @ExpenseType nvarchar(50);
DECLARE @Amount decimal(18,2);
DECLARE @ClaimStatus nvarchar(50);
DECLARE @IsDeleted bit;
DECLARE @ModifiedOn datetime2(7);
DECLARE @ClaimProcessedBy nvarchar(200);
DECLARE @ClaimProcessedDate datetime;
DECLARE @ExpenseRemarks nvarchar(1000);

SELECT
      @ExpenseFound = 1
    , @ActivityId = e.ActivityId
    , @ExpenseType = e.ExpenseType
    , @Amount = e.Amount
    , @ClaimStatus = e.ClaimStatus
    , @IsDeleted = e.IsDeleted
    , @ModifiedOn = e.ModifiedOn
    , @ClaimProcessedBy = e.ClaimProcessedBy
    , @ClaimProcessedDate = e.ClaimProcessedDate
    , @ExpenseRemarks = e.Remarks
FROM dbo.SalesDailyExpense e
WHERE e.ExpenseId = @ExpenseId;

DECLARE @OwningDetailId int;
DECLARE @ApprovalRequestId int;
DECLARE @DetailVersion int;
DECLARE @DetailStatus nvarchar(50);

SELECT TOP (1)
      @OwningDetailId = d.ApprovalRequestDetailId
    , @ApprovalRequestId = d.ApprovalRequestId
    , @DetailVersion = d.DetailVersion
    , @DetailStatus = d.DetailStatus
FROM dbo.SalesApprovalRequestDetail d
INNER JOIN (
    SELECT
          ActivityId
        , MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
    FROM dbo.SalesApprovalRequestDetail
    WHERE ActivityId = @ActivityId
    GROUP BY ActivityId
) lv
    ON lv.ActivityId = d.ActivityId
   AND ISNULL(d.DetailVersion, 1) = lv.LatestVersion
WHERE d.ActivityId = @ActivityId
ORDER BY d.ApprovalRequestDetailId DESC;

DECLARE @RequestStatus nvarchar(50);
DECLARE @CurrentApproverLoginId nvarchar(100);
DECLARE @CurrentApproverLevel int;
DECLARE @StoredTotalClaim decimal(18,2);
DECLARE @StoredTotalExpense decimal(18,2);

SELECT
      @RequestStatus = r.Status
    , @CurrentApproverLoginId = r.CurrentApproverLoginId
    , @CurrentApproverLevel = r.CurrentApproverLevel
    , @StoredTotalClaim = r.TotalClaimAmount
    , @StoredTotalExpense = r.TotalExpenseAmount
FROM dbo.SalesApprovalRequest r
WHERE r.ApprovalRequestId = @ApprovalRequestId;

DECLARE @CurrentLevelApproverCount int = 0;
DECLARE @CurrentApproverRole nvarchar(100);
DECLARE @CurrentApproverRowLogin nvarchar(100);
DECLARE @CurrentApproverRowStatus nvarchar(50);

SELECT
      @CurrentLevelApproverCount = COUNT(*)
    , @CurrentApproverRole = CASE WHEN COUNT(*) = 1 THEN MAX(a.ApproverRole) END
    , @CurrentApproverRowLogin = CASE WHEN COUNT(*) = 1 THEN MAX(a.ApproverLoginId) END
    , @CurrentApproverRowStatus = CASE WHEN COUNT(*) = 1 THEN MAX(a.Status) END
FROM dbo.SalesApprovalRequestApprover a
WHERE a.ApprovalRequestId = @ApprovalRequestId
  AND a.ApproverLevel = @CurrentApproverLevel;

DECLARE @StoredDA decimal(18,2);
DECLARE @StoredHotel decimal(18,2);
DECLARE @StoredTravel decimal(18,2);
DECLARE @StoredStationary decimal(18,2);
DECLARE @StoredExpenseTotal decimal(18,2);

SELECT
      @StoredDA = d.Snapshot_DAAmount
    , @StoredHotel = d.Snapshot_HotelAmount
    , @StoredTravel = d.Snapshot_TravelAmount
    , @StoredStationary = d.Snapshot_StationaryAmount
    , @StoredExpenseTotal = d.Snapshot_ExpenseTotal
FROM dbo.SalesApprovalRequestDetail d
WHERE d.ApprovalRequestDetailId = @OwningDetailId;

DECLARE @CalcDA decimal(18,2) = 0;
DECLARE @CalcHotel decimal(18,2) = 0;
DECLARE @CalcTravel decimal(18,2) = 0;
DECLARE @CalcStationary decimal(18,2) = 0;
DECLARE @CalcExpenseTotal decimal(18,2) = 0;

IF @ActivityId IS NOT NULL
BEGIN
    SELECT
          @CalcDA = ISNULL((
                SELECT SUM(e.Amount)
                FROM dbo.SalesDailyExpense e
                WHERE e.ActivityId = @ActivityId
                  AND e.ExpenseType = 'DA'
                  AND e.IsDeleted = 0
                  AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
            ), 0)
        , @CalcHotel = ISNULL((
                SELECT SUM(e.Amount)
                FROM dbo.SalesDailyExpense e
                WHERE e.ActivityId = @ActivityId
                  AND e.ExpenseType = 'Hotel'
                  AND e.IsDeleted = 0
                  AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
            ), 0)
        , @CalcTravel = ISNULL((
                SELECT SUM(e.Amount)
                FROM dbo.SalesDailyExpense e
                WHERE e.ActivityId = @ActivityId
                  AND e.ExpenseType = 'Travel'
                  AND e.IsDeleted = 0
                  AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
            ), 0)
        , @CalcStationary = ISNULL((
                SELECT SUM(e.Amount)
                FROM dbo.SalesDailyExpense e
                WHERE e.ActivityId = @ActivityId
                  AND e.ExpenseType = 'Stationary'
                  AND e.IsDeleted = 0
                  AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
            ), 0);
END;

SET @CalcExpenseTotal = @CalcDA + @CalcHotel + @CalcTravel + @CalcStationary;

DECLARE @CalcHeaderClaim decimal(18,2);
DECLARE @CalcHeaderExpense decimal(18,2);

SELECT
      @CalcHeaderClaim = SUM(ISNULL(d.Snapshot_CalculatedClaimAmount, 0))
    , @CalcHeaderExpense = SUM(ISNULL(d.Snapshot_ExpenseTotal, 0))
FROM dbo.SalesApprovalRequestDetail d
INNER JOIN (
    SELECT
          d2.ActivityId
        , MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
    FROM dbo.SalesApprovalRequestDetail d2
    INNER JOIN (
        SELECT
              ActivityId
            , MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
        FROM dbo.SalesApprovalRequestDetail
        WHERE ApprovalRequestId = @ApprovalRequestId
        GROUP BY ActivityId
    ) lv
        ON lv.ActivityId = d2.ActivityId
       AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
    WHERE d2.ApprovalRequestId = @ApprovalRequestId
    GROUP BY d2.ActivityId
) latest
    ON d.ApprovalRequestDetailId = latest.LatestDetailId
WHERE d.ApprovalRequestId = @ApprovalRequestId;

DECLARE @OwningSnapshotMatches bit = 0;

IF @OwningDetailId IS NOT NULL
   AND @StoredDA = @CalcDA
   AND @StoredHotel = @CalcHotel
   AND @StoredTravel = @CalcTravel
   AND @StoredStationary = @CalcStationary
   AND @StoredExpenseTotal = @CalcExpenseTotal
   AND @StoredExpenseTotal = ISNULL(@StoredDA, 0) + ISNULL(@StoredHotel, 0) + ISNULL(@StoredTravel, 0) + ISNULL(@StoredStationary, 0)
BEGIN
    SET @OwningSnapshotMatches = 1;
END;

DECLARE @HeaderTotalsMatch bit = 0;

IF @ApprovalRequestId IS NOT NULL
   AND @StoredTotalClaim = @CalcHeaderClaim
   AND @StoredTotalExpense = @CalcHeaderExpense
BEGIN
    SET @HeaderTotalsMatch = 1;
END;

DECLARE @LineIsExpectedStationary bit = 0;
DECLARE @Stationary180Excluded bit = 0;

IF @ExpenseFound = 1
   AND @ExpenseType = @ExpectedExpenseType
   AND @Amount = @ExpectedAmount
   AND @IsDeleted = 0
BEGIN
    SET @LineIsExpectedStationary = 1;
END;

IF @LineIsExpectedStationary = 1
   AND @ClaimStatus = N'Rejected'
   AND @OwningSnapshotMatches = 1
   AND @HeaderTotalsMatch = 1
BEGIN
    SET @Stationary180Excluded = 1;
END;

DECLARE @AuditorAssignmentHolds bit = 0;

IF @RequestStatus = N'Pending'
   AND @CurrentLevelApproverCount = 1
   AND @CurrentApproverRole = N'Auditor'
   AND @CurrentApproverRowStatus = N'Pending'
   AND UPPER(LTRIM(RTRIM(@CurrentApproverRowLogin))) = UPPER(LTRIM(RTRIM(@CurrentApproverLoginId)))
BEGIN
    SET @AuditorAssignmentHolds = 1;
END;

DECLARE @PostUpdateHistoryCount int = 0;
DECLARE @ReturnToL1HistoryCount int = 0;
DECLARE @TerminalHistoryCount int = 0;

SELECT @PostUpdateHistoryCount = COUNT(*)
FROM dbo.ApprovalRequestStatusHistory h
WHERE h.ApprovalRequestId = @ApprovalRequestId
  AND @ModifiedOn IS NOT NULL
  AND h.ChangedOn >= @ModifiedOn;

SELECT
      @ReturnToL1HistoryCount = SUM(CASE
            WHEN h.Remarks LIKE N'Auditor returned to L1 for correction:%' THEN 1
            ELSE 0
        END)
    , @TerminalHistoryCount = SUM(CASE
            WHEN h.NewStatus IN (N'Rejected', N'Completed', N'ReadyForPayment') THEN 1
            ELSE 0
        END)
FROM dbo.ApprovalRequestStatusHistory h
WHERE h.ApprovalRequestId = @ApprovalRequestId;

SET @ReturnToL1HistoryCount = ISNULL(@ReturnToL1HistoryCount, 0);
SET @TerminalHistoryCount = ISNULL(@TerminalHistoryCount, 0);

DECLARE @OtherActivityDriftCount int = 0;

SELECT @OtherActivityDriftCount = COUNT(*)
FROM (
    SELECT d.ApprovalRequestDetailId
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN (
        SELECT
              d2.ActivityId
            , MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
        FROM dbo.SalesApprovalRequestDetail d2
        INNER JOIN (
            SELECT
                  ActivityId
                , MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
            FROM dbo.SalesApprovalRequestDetail
            WHERE ApprovalRequestId = @ApprovalRequestId
            GROUP BY ActivityId
        ) lv
            ON lv.ActivityId = d2.ActivityId
           AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
        WHERE d2.ApprovalRequestId = @ApprovalRequestId
        GROUP BY d2.ActivityId
    ) latest
        ON d.ApprovalRequestDetailId = latest.LatestDetailId
    CROSS APPLY (
        SELECT
              ISNULL((
                    SELECT SUM(e.Amount)
                    FROM dbo.SalesDailyExpense e
                    WHERE e.ActivityId = d.ActivityId
                      AND e.ExpenseType = 'DA'
                      AND e.IsDeleted = 0
                      AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
                ), 0) AS DAAmount
            , ISNULL((
                    SELECT SUM(e.Amount)
                    FROM dbo.SalesDailyExpense e
                    WHERE e.ActivityId = d.ActivityId
                      AND e.ExpenseType = 'Hotel'
                      AND e.IsDeleted = 0
                      AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
                ), 0) AS HotelAmount
            , ISNULL((
                    SELECT SUM(e.Amount)
                    FROM dbo.SalesDailyExpense e
                    WHERE e.ActivityId = d.ActivityId
                      AND e.ExpenseType = 'Travel'
                      AND e.IsDeleted = 0
                      AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
                ), 0) AS TravelAmount
            , ISNULL((
                    SELECT SUM(e.Amount)
                    FROM dbo.SalesDailyExpense e
                    WHERE e.ActivityId = d.ActivityId
                      AND e.ExpenseType = 'Stationary'
                      AND e.IsDeleted = 0
                      AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
                ), 0) AS StationaryAmount
    ) calc
    WHERE d.ApprovalRequestId = @ApprovalRequestId
      AND (@OwningDetailId IS NULL OR d.ApprovalRequestDetailId <> @OwningDetailId)
      AND CASE
              WHEN d.Snapshot_DAAmount = calc.DAAmount
               AND d.Snapshot_HotelAmount = calc.HotelAmount
               AND d.Snapshot_TravelAmount = calc.TravelAmount
               AND d.Snapshot_StationaryAmount = calc.StationaryAmount
               AND d.Snapshot_ExpenseTotal = (calc.DAAmount + calc.HotelAmount + calc.TravelAmount + calc.StationaryAmount)
                  THEN 0
              ELSE 1
          END = 1
) drifted;

DECLARE @ConsistencyGaps nvarchar(2000) = N'';

IF @ExpenseFound = 0
    SET @ConsistencyGaps = N'Expense 2646 was not found.';
ELSE IF ISNULL(@ClaimStatus, N'') <> N'Rejected'
    SET @ConsistencyGaps = N'ClaimStatus is not Rejected. Do not retry the rejection.';
ELSE
BEGIN
    IF @OwningDetailId IS NULL
        SET @ConsistencyGaps += N' No latest approval detail was resolved for the activity.';
    IF @AuditorAssignmentHolds <> 1
        SET @ConsistencyGaps += N' Request is not Pending under one Pending Auditor row whose login matches the header.';
    IF @OwningSnapshotMatches <> 1
        SET @ConsistencyGaps += N' Owning detail snapshots do not match the expense-status formula.';
    IF @HeaderTotalsMatch <> 1
        SET @ConsistencyGaps += N' Header TotalClaimAmount or TotalExpenseAmount does not match the latest-detail snapshot sum.';
    IF @LineIsExpectedStationary <> 1
        SET @ConsistencyGaps += N' The line is not an active Stationary amount of 180.00.';
    ELSE IF @Stationary180Excluded <> 1
        SET @ConsistencyGaps += N' Stationary 180.00 was not confirmed as excluded from the snapshots and header expense total.';
    IF @ModifiedOn IS NULL
        SET @ConsistencyGaps += N' ModifiedOn is null, so post-update history cannot be bounded.';
    IF @PostUpdateHistoryCount > 0
        SET @ConsistencyGaps += N' Approval history exists at or after the expense ModifiedOn. The expense procedure does not write history.';
    IF @ReturnToL1HistoryCount > 0
        SET @ConsistencyGaps += N' Auditor-to-L1 history is present. That routing feature is parked.';
    IF @TerminalHistoryCount > 0
        SET @ConsistencyGaps += N' History contains Rejected, Completed, or ReadyForPayment.';
    IF @OtherActivityDriftCount > 0
        SET @ConsistencyGaps += N' Other latest details differ from the live expense formula. This procedure refreshes only the expense activity.';
END;

SELECT
      N'DiagnosticResult' AS ResultSet
    , DB_NAME() AS CurrentDatabase
    , CONVERT(nvarchar(256), SERVERPROPERTY(N'ServerName')) AS ServerName
    , @ExpenseId AS ExpenseId
    , CASE WHEN @ExpenseFound = 1 AND @ClaimStatus = N'Rejected' THEN 1 ELSE 0 END AS ExpenseStatusIsRejected
    , CASE WHEN @ExpenseFound = 1 THEN @ClaimStatus ELSE NULL END AS ClaimStatus
    , @RequestStatus AS RequestStatus
    , @CurrentApproverLoginId AS CurrentApproverLoginId
    , @CurrentApproverLevel AS CurrentApproverLevel
    , @CurrentApproverRole AS CurrentApproverRole
    , @OwningDetailId AS OwningApprovalRequestDetailId
    , @DetailVersion AS OwningDetailVersion
    , @DetailStatus AS OwningDetailStatus
    , @OwningSnapshotMatches AS OwningSnapshotMatches
    , @HeaderTotalsMatch AS HeaderTotalsMatch
    , @Stationary180Excluded AS Stationary180Excluded
    , @PostUpdateHistoryCount AS PostUpdateHistoryCount
    , CASE
          WHEN @ExpenseFound = 0
              THEN N'NOT Rejected | EXPENSE NOT FOUND | persistence is not confirmed'
          WHEN ISNULL(@ClaimStatus, N'') = N'Approved'
              THEN N'NOT Rejected | STILL APPROVED | HTTP success is not database persistence | do not retry the rejection'
          WHEN ISNULL(@ClaimStatus, N'') <> N'Rejected'
              THEN N'NOT Rejected | current ClaimStatus is ' + ISNULL(@ClaimStatus, N'<null>')
                   + N' | HTTP success is not database persistence | do not retry the rejection'
          WHEN @ConsistencyGaps = N''
              THEN N'Rejected | PERSISTED | request remains Pending under the current Auditor | owning snapshots and header totals match | Stationary 180.00 is excluded | no post-update, return-to-L1, or terminal history'
          ELSE N'Rejected | PERSISTED WITH A CONSISTENCY GAP | do not retry the rejection | ' + @ConsistencyGaps
      END AS DiagnosticResult
    , NULLIF(@ConsistencyGaps, N'') AS ConsistencyGaps
    , N'{"d":{"success":true}} means ExecuteNonQuery did not throw. This result is the persistence read.' AS HttpSuccessMeaning;

SELECT
      N'Context' AS ResultSet
    , DB_NAME() AS CurrentDatabase
    , CONVERT(nvarchar(256), SERVERPROPERTY(N'ServerName')) AS ServerName
    , CASE
          WHEN DB_NAME() = N'AnmolDristiDev' THEN N'INTENDED DATABASE'
          ELSE N'NOT THE INTENDED DATABASE'
      END AS DatabaseCheck
    , @ExpenseId AS ExpenseId
    , N'Stationary' AS ExpectedExpenseType
    , @ExpectedAmount AS ExpectedAmount
    , N'This script has not called usp_UpdateSalesDailyExpenseStatus.' AS ProcedureCallStatus;

SELECT
      N'Expense' AS ResultSet
    , e.ExpenseId
    , e.ActivityId
    , e.ExpenseType
    , e.Amount
    , e.ClaimStatus
    , e.Remarks AS RejectionRemarks
    , e.ClaimProcessedBy
    , e.ClaimProcessedDate
    , e.ModifiedBy
    , e.ModifiedOn
    , e.IsDeleted
    , e.DeletedBy
    , e.DeletedOn
    , e.DeleteReason
    , e.ApprovalStatus
    , e.ApprovalRemarks
    , e.ApprovedBy
    , e.ApprovedOn
    , e.Purpose
    , e.BillNo
    , e.ExpenseDate
FROM dbo.SalesDailyExpense e
WHERE e.ExpenseId = @ExpenseId;

SELECT
      N'DetailVersions' AS ResultSet
    , d.ApprovalRequestDetailId
    , d.ApprovalRequestId
    , d.ActivityId
    , d.DetailVersion
    , d.PreviousDetailId
    , d.DetailStatus
    , d.DetailRemarks
    , d.DetailApprovedBy
    , d.DetailApprovedOn
    , d.DetailApprovedLevel
    , d.ModifiedBy
    , d.ModifiedOn
    , d.Snapshot_DAAmount
    , d.Snapshot_HotelAmount
    , d.Snapshot_TravelAmount
    , d.Snapshot_StationaryAmount
    , d.Snapshot_ExpenseTotal
    , d.Snapshot_CalculatedClaimAmount
    , CASE
          WHEN d.ApprovalRequestDetailId = @OwningDetailId THEN 1
          ELSE 0
      END AS IsOwningLatestDetail
FROM dbo.SalesApprovalRequestDetail d
WHERE d.ActivityId = @ActivityId
ORDER BY ISNULL(d.DetailVersion, 1), d.ApprovalRequestDetailId;

SELECT
      N'RequestHeader' AS ResultSet
    , r.ApprovalRequestId
    , r.Status
    , r.CurrentApproverLoginId
    , r.CurrentApproverLevel
    , r.RequesterLoginId
    , r.YearNo
    , r.MonthNo
    , r.TotalClaimAmount
    , r.TotalExpenseAmount
    , r.TotalDAAmount
    , r.TotalHotelAmount
    , r.TotalTravelAmount
    , r.TotalStationaryAmount
    , r.ApprovedClaimAmount
    , r.ApprovedDAAmount
    , r.ApprovedHotelAmount
    , r.ApprovedTravelAmount
    , r.ApprovedExpenseAmount
    , r.FinalApprovedAmount
    , r.TotalDeductionAmount
    , r.OriginalClaimAmount
    , r.ModifiedBy
    , r.ModifiedOn
    , N'usp_UpdateSalesDailyExpenseStatus assigns TotalClaimAmount, TotalExpenseAmount, and ModifiedOn only.' AS HeaderColumnsWrittenByExpenseStatus
FROM dbo.SalesApprovalRequest r
WHERE r.ApprovalRequestId = @ApprovalRequestId;

SELECT
      N'Approvers' AS ResultSet
    , a.ApprovalRequestApproverId
    , a.ApproverLoginId
    , a.ApproverRole
    , a.ApproverLevel
    , a.Status
    , a.ActionBy
    , a.ActionDate
    , a.Remarks
    , a.ModifiedBy
    , a.ModifiedOn
    , CASE
          WHEN a.ApproverLevel = @CurrentApproverLevel THEN 1
          ELSE 0
      END AS IsCurrentLevel
FROM dbo.SalesApprovalRequestApprover a
WHERE a.ApprovalRequestId = @ApprovalRequestId
ORDER BY a.ApproverLevel, a.ApprovalRequestApproverId;

SELECT
      N'ActivityExpenses' AS ResultSet
    , e.ExpenseId
    , e.ActivityId
    , e.ExpenseType
    , e.Amount
    , e.ClaimStatus
    , e.IsDeleted
    , e.Remarks
    , e.ClaimProcessedBy
    , e.ClaimProcessedDate
    , CASE
          WHEN e.IsDeleted = 1 THEN N'Excluded because IsDeleted = 1'
          WHEN ISNULL(e.ClaimStatus, N'') = N'Rejected' THEN N'Excluded because ClaimStatus is Rejected'
          WHEN e.ExpenseType = N'DA' THEN N'Included in Snapshot_DAAmount'
          WHEN e.ExpenseType = N'Hotel' THEN N'Included in Snapshot_HotelAmount'
          WHEN e.ExpenseType = N'Travel' THEN N'Included in Snapshot_TravelAmount'
          WHEN e.ExpenseType = N'Stationary' THEN N'Included in Snapshot_StationaryAmount'
          ELSE N'Not part of the four-part expense snapshot'
      END AS ExpenseStatusSnapshotTreatment
    , CASE WHEN e.ExpenseId = @ExpenseId THEN 1 ELSE 0 END AS IsTargetExpense
FROM dbo.SalesDailyExpense e
WHERE e.ActivityId = @ActivityId
ORDER BY e.ExpenseId;

SELECT
      N'SnapshotReconciliation' AS ResultSet
    , @ActivityId AS ActivityId
    , @OwningDetailId AS OwningApprovalRequestDetailId
    , @DetailVersion AS DetailVersion
    , @DetailStatus AS DetailStatus
    , @StoredDA AS StoredDAAmount
    , @CalcDA AS FormulaDAAmount
    , @StoredHotel AS StoredHotelAmount
    , @CalcHotel AS FormulaHotelAmount
    , @StoredTravel AS StoredTravelAmount
    , @CalcTravel AS FormulaTravelAmount
    , @StoredStationary AS StoredStationaryAmount
    , @CalcStationary AS FormulaStationaryAmount
    , @StoredExpenseTotal AS StoredExpenseTotal
    , @CalcExpenseTotal AS FormulaExpenseTotal
    , ISNULL(@StoredDA, 0) + ISNULL(@StoredHotel, 0) + ISNULL(@StoredTravel, 0) + ISNULL(@StoredStationary, 0) AS StoredFourPartSum
    , @OwningSnapshotMatches AS OwningSnapshotMatches
    , @ExpenseType AS Expense2646Type
    , @Amount AS Expense2646Amount
    , @ClaimStatus AS Expense2646ClaimStatus
    , @IsDeleted AS Expense2646IsDeleted
    , @LineIsExpectedStationary AS LineIsActiveStationary180
    , @Stationary180Excluded AS Stationary180Excluded
    , CASE
          WHEN @ExpenseFound = 0 THEN N'EXPENSE NOT FOUND'
          WHEN @LineIsExpectedStationary <> 1 THEN N'LINE IS NOT AN ACTIVE STATIONARY AMOUNT OF 180.00'
          WHEN @ClaimStatus <> N'Rejected' THEN N'NOT EXCLUDED | ClaimStatus is not Rejected'
          WHEN @Stationary180Excluded = 1 THEN N'EXCLUDED | stored stationary, expense total, and header expense total match the formula that omits Rejected rows'
          ELSE N'REJECTED STATIONARY 180.00 IS PRESENT BUT THE STORED SNAPSHOT OR HEADER TOTAL DOES NOT MATCH THE EXCLUDING FORMULA'
      END AS Stationary180ExclusionResult;

SELECT
      N'HeaderReconciliation' AS ResultSet
    , @ApprovalRequestId AS ApprovalRequestId
    , @StoredTotalClaim AS StoredTotalClaimAmount
    , @CalcHeaderClaim AS LatestDetailClaimSum
    , @StoredTotalExpense AS StoredTotalExpenseAmount
    , @CalcHeaderExpense AS LatestDetailExpenseSum
    , @HeaderTotalsMatch AS HeaderTotalsMatch
    , @OtherActivityDriftCount AS OtherActivitySnapshotDriftCount;

SELECT
      N'ActivitySnapshotDrift' AS ResultSet
    , d.ApprovalRequestDetailId
    , d.ActivityId
    , d.DetailVersion
    , d.Snapshot_DAAmount
    , calc.DAAmount AS FormulaDAAmount
    , d.Snapshot_HotelAmount
    , calc.HotelAmount AS FormulaHotelAmount
    , d.Snapshot_TravelAmount
    , calc.TravelAmount AS FormulaTravelAmount
    , d.Snapshot_StationaryAmount
    , calc.StationaryAmount AS FormulaStationaryAmount
    , d.Snapshot_ExpenseTotal
    , calc.DAAmount + calc.HotelAmount + calc.TravelAmount + calc.StationaryAmount AS FormulaExpenseTotal
    , CASE
          WHEN d.ApprovalRequestDetailId = @OwningDetailId THEN 1
          ELSE 0
      END AS IsOwningLatestDetail
    , CASE
          WHEN d.Snapshot_DAAmount = calc.DAAmount
           AND d.Snapshot_HotelAmount = calc.HotelAmount
           AND d.Snapshot_TravelAmount = calc.TravelAmount
           AND d.Snapshot_StationaryAmount = calc.StationaryAmount
           AND d.Snapshot_ExpenseTotal = (calc.DAAmount + calc.HotelAmount + calc.TravelAmount + calc.StationaryAmount)
              THEN 0
          ELSE 1
      END AS DiffersFromLiveFormula
FROM dbo.SalesApprovalRequestDetail d
INNER JOIN (
    SELECT
          d2.ActivityId
        , MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
    FROM dbo.SalesApprovalRequestDetail d2
    INNER JOIN (
        SELECT
              ActivityId
            , MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
        FROM dbo.SalesApprovalRequestDetail
        WHERE ApprovalRequestId = @ApprovalRequestId
        GROUP BY ActivityId
    ) lv
        ON lv.ActivityId = d2.ActivityId
       AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
    WHERE d2.ApprovalRequestId = @ApprovalRequestId
    GROUP BY d2.ActivityId
) latest
    ON d.ApprovalRequestDetailId = latest.LatestDetailId
CROSS APPLY (
    SELECT
          ISNULL((
                SELECT SUM(e.Amount)
                FROM dbo.SalesDailyExpense e
                WHERE e.ActivityId = d.ActivityId
                  AND e.ExpenseType = 'DA'
                  AND e.IsDeleted = 0
                  AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
            ), 0) AS DAAmount
        , ISNULL((
                SELECT SUM(e.Amount)
                FROM dbo.SalesDailyExpense e
                WHERE e.ActivityId = d.ActivityId
                  AND e.ExpenseType = 'Hotel'
                  AND e.IsDeleted = 0
                  AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
            ), 0) AS HotelAmount
        , ISNULL((
                SELECT SUM(e.Amount)
                FROM dbo.SalesDailyExpense e
                WHERE e.ActivityId = d.ActivityId
                  AND e.ExpenseType = 'Travel'
                  AND e.IsDeleted = 0
                  AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
            ), 0) AS TravelAmount
        , ISNULL((
                SELECT SUM(e.Amount)
                FROM dbo.SalesDailyExpense e
                WHERE e.ActivityId = d.ActivityId
                  AND e.ExpenseType = 'Stationary'
                  AND e.IsDeleted = 0
                  AND ISNULL(e.ClaimStatus, '') <> 'Rejected'
            ), 0) AS StationaryAmount
) calc
WHERE d.ApprovalRequestId = @ApprovalRequestId
ORDER BY d.ActivityId, d.ApprovalRequestDetailId;

SELECT
      N'ApprovalHistory' AS ResultSet
    , h.HistoryId
    , h.ApprovalRequestId
    , h.OldStatus
    , h.NewStatus
    , h.ChangedBy
    , h.ChangedOn
    , h.Remarks
    , h.ApprovalRequestDetailId
    , h.ActivityId
    , h.ApproverLevel
    , h.ActionType
    , h.DetailVersion
    , CASE
          WHEN @ModifiedOn IS NOT NULL AND h.ChangedOn >= @ModifiedOn
              THEN N'AT OR AFTER EXPENSE MODIFIEDON | expense status does not write history'
          WHEN h.Remarks LIKE N'Auditor returned to L1 for correction:%'
              THEN N'AUDITOR TO L1 ROUTING'
          WHEN h.NewStatus IN (N'Rejected', N'Completed', N'ReadyForPayment')
              THEN N'TERMINAL OR REQUEST REJECTION'
          WHEN h.Remarks LIKE N'%Forwarded to%'
              THEN N'EARLIER APPROVAL FORWARD'
          WHEN h.Remarks LIKE N'%Awaiting payment%'
              THEN N'PAYMENT TRANSITION'
          ELSE N'EARLIER WORKFLOW'
      END AS HistoryClassification
FROM dbo.ApprovalRequestStatusHistory h
WHERE h.ApprovalRequestId = @ApprovalRequestId
ORDER BY h.HistoryId;
