/*
    Auditor -> L1 routing checks.
    Authoring status: NOT EXECUTED.

    This script does not deploy dbo.usp_ReturnApprovalRequestToL1.
    This script does not call that procedure.
    This script does not insert, update, or delete application data.
    This script does not begin, commit, or roll back a transaction.

    Accidental execution stops in the gate below. A reviewer sets
    @RunReadOnlyChecks to 1 only after a separate authorization to read
    the fixture, and only while connected to the authorized non-live
    database. Request 118 is the default fixture id. Change
    @ApprovalRequestId only when the authorized fixture is a different id.

    Compare each labeled result set with the saved baseline. This script
    does not decide pass or fail by itself.
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

DECLARE @RunReadOnlyChecks bit = 0;

IF @RunReadOnlyChecks <> 1
BEGIN
    PRINT N'NOT RUN | Set @RunReadOnlyChecks = 1 only for an authorized read of the fixture.';
    RETURN;
END;

DECLARE @ApprovalRequestId int = 118;
DECLARE @ReturnedActivityDate date = '2026-09-01';
DECLARE @HistoryPrefix nvarchar(100) = N'Auditor returned to L1 for correction: ';

PRINT N'CONTEXT | Server=' + ISNULL(CONVERT(nvarchar(256), SERVERPROPERTY(N'ServerName')), N'<null>');
PRINT N'CONTEXT | Database=' + ISNULL(DB_NAME(), N'<null>');
PRINT N'CONTEXT | ApprovalRequestId=' + CONVERT(nvarchar(12), @ApprovalRequestId);
PRINT N'CONTEXT | This script has not called the routing procedure.';

/*
    Definition shape. Before deployment this result is PROCEDURE NOT IN CATALOG.

    @ReturnToL1Definition is the catalog text once, including any pre-CREATE
    banner. Token checks read @ReturnToL1Scan only. That copy masks block
    comments and line comments with one space so comment text cannot match,
    and so a comment cannot glue or hide an executable EXEC / EXECUTE.
    A banner-before-CREATE check must keep using @ReturnToL1Definition.
*/
DECLARE @ReturnToL1ObjectId int = OBJECT_ID(N'dbo.usp_ReturnApprovalRequestToL1', N'P');
DECLARE @ReturnToL1Definition nvarchar(max) = OBJECT_DEFINITION(@ReturnToL1ObjectId);
DECLARE @ReturnToL1Scan nvarchar(max) = N'';
DECLARE @ScanIndex int;
DECLARE @ScanLength int;
DECLARE @SegmentStart int;
DECLARE @ScanChar nchar(1);
DECLARE @ScanNext nchar(1);
DECLARE @ScanState tinyint;
DECLARE @BlockDepth int;

IF @ReturnToL1Definition IS NOT NULL
BEGIN
    SET @ScanIndex = 1;
    SET @ScanLength = DATALENGTH(@ReturnToL1Definition) / 2;
    SET @SegmentStart = 1;
    SET @ScanState = 0;
    SET @BlockDepth = 0;

    WHILE @ScanIndex <= @ScanLength
    BEGIN
        SET @ScanChar = SUBSTRING(@ReturnToL1Definition, @ScanIndex, 1);
        SET @ScanNext = CASE
                            WHEN @ScanIndex < @ScanLength
                                THEN SUBSTRING(@ReturnToL1Definition, @ScanIndex + 1, 1)
                            ELSE N''
                        END;

        /* 0 = code, 3 = string, 4 = bracket identifier, 5 = quoted identifier. */
        IF @ScanState = 0
        BEGIN
            IF @ScanChar = N'-' AND @ScanNext = N'-'
            BEGIN
                IF @ScanIndex > @SegmentStart
                    SET @ReturnToL1Scan += SUBSTRING(@ReturnToL1Definition, @SegmentStart, @ScanIndex - @SegmentStart);

                SET @ReturnToL1Scan += N' ';
                SET @ScanState = 1;
                SET @ScanIndex += 2;
                SET @SegmentStart = @ScanIndex;
                CONTINUE;
            END;

            IF @ScanChar = N'/' AND @ScanNext = N'*'
            BEGIN
                IF @ScanIndex > @SegmentStart
                    SET @ReturnToL1Scan += SUBSTRING(@ReturnToL1Definition, @SegmentStart, @ScanIndex - @SegmentStart);

                SET @ReturnToL1Scan += N' ';
                SET @ScanState = 2;
                SET @BlockDepth = 1;
                SET @ScanIndex += 2;
                SET @SegmentStart = @ScanIndex;
                CONTINUE;
            END;

            IF @ScanChar = N''''
            BEGIN
                SET @ScanState = 3;
                SET @ScanIndex += 1;
                CONTINUE;
            END;

            IF @ScanChar = N'['
            BEGIN
                SET @ScanState = 4;
                SET @ScanIndex += 1;
                CONTINUE;
            END;

            IF @ScanChar = N'"'
            BEGIN
                SET @ScanState = 5;
                SET @ScanIndex += 1;
                CONTINUE;
            END;

            SET @ScanIndex += 1;
            CONTINUE;
        END;

        /* 1 = line comment. The newline stays in the scan copy. */
        IF @ScanState = 1
        BEGIN
            IF @ScanChar = NCHAR(10) OR @ScanChar = NCHAR(13)
            BEGIN
                SET @ScanState = 0;
                SET @SegmentStart = @ScanIndex;
            END;

            SET @ScanIndex += 1;
            CONTINUE;
        END;

        /* 2 = block comment. SQL Server nests these. */
        IF @ScanState = 2
        BEGIN
            IF @ScanChar = N'/' AND @ScanNext = N'*'
            BEGIN
                SET @BlockDepth += 1;
                SET @ScanIndex += 2;
                CONTINUE;
            END;

            IF @ScanChar = N'*' AND @ScanNext = N'/'
            BEGIN
                SET @BlockDepth -= 1;
                SET @ScanIndex += 2;

                IF @BlockDepth = 0
                BEGIN
                    SET @ScanState = 0;
                    SET @SegmentStart = @ScanIndex;
                END;

                CONTINUE;
            END;

            SET @ScanIndex += 1;
            CONTINUE;
        END;

        IF @ScanState = 3
        BEGIN
            IF @ScanChar = N''''
            BEGIN
                IF @ScanNext = N''''
                BEGIN
                    SET @ScanIndex += 2;
                    CONTINUE;
                END;

                SET @ScanState = 0;
            END;

            SET @ScanIndex += 1;
            CONTINUE;
        END;

        IF @ScanState = 4
        BEGIN
            IF @ScanChar = N']'
            BEGIN
                IF @ScanNext = N']'
                BEGIN
                    SET @ScanIndex += 2;
                    CONTINUE;
                END;

                SET @ScanState = 0;
            END;

            SET @ScanIndex += 1;
            CONTINUE;
        END;

        IF @ScanState = 5
        BEGIN
            IF @ScanChar = N'"'
            BEGIN
                IF @ScanNext = N'"'
                BEGIN
                    SET @ScanIndex += 2;
                    CONTINUE;
                END;

                SET @ScanState = 0;
            END;

            SET @ScanIndex += 1;
            CONTINUE;
        END;
    END;

    IF @ScanState IN (0, 3, 4, 5) AND @SegmentStart <= @ScanLength
        SET @ReturnToL1Scan += SUBSTRING(@ReturnToL1Definition, @SegmentStart, @ScanLength - @SegmentStart + 1);
END;

SELECT
      CASE
          WHEN @ReturnToL1ObjectId IS NULL
              THEN N'PROCEDURE NOT IN CATALOG'
          WHEN @ReturnToL1Definition IS NULL
              THEN N'DEFINITION UNAVAILABLE'
          WHEN @ReturnToL1Scan LIKE N'%EXEC %'
            OR @ReturnToL1Scan LIKE N'%EXECUTE %'
              THEN N'FAIL | procedure calls another module'
          WHEN @ReturnToL1Scan LIKE N'%SalesApprovalRequestDetail%'
            OR @ReturnToL1Scan LIKE N'%SalesDailyActivity%'
            OR @ReturnToL1Scan LIKE N'%SalesDailyExpense%'
              THEN N'FAIL | procedure references detail, activity, or expense storage'
          WHEN @ReturnToL1Scan LIKE N'%Reopened%'
              THEN N'FAIL | non-canonical status token'
          ELSE N'DEFINITION SHAPE PASSED'
      END AS DefinitionVerdict;

/* Case 1 and the financial checksum. */
SELECT
      r.ApprovalRequestId
    , r.Status
    , r.CurrentApproverLoginId
    , r.CurrentApproverLevel
    , r.TotalKM
    , r.TotalClaimAmount
    , r.TotalDAAmount
    , r.TotalHotelAmount
    , r.TotalTravelAmount
    , r.TotalExpenseAmount
    , r.TotalStationaryAmount
    , r.ApprovedKM
    , r.ApprovedClaimAmount
    , r.ApprovedDAAmount
    , r.ApprovedHotelAmount
    , r.ApprovedTravelAmount
    , r.FinalApprovedAmount
    , r.TotalDeductionAmount
    , r.OriginalClaimAmount
    , r.TotalActivities
    , r.TotalExpenseEntries
    , r.TotalAttachmentCount
    , r.ApprovedActivities
    , r.RejectedActivities
    , r.PendingActivities
    , r.ReturnedActivities
    , r.BulkEligibleActivities
    , r.ManualReviewActivities
    , r.ModifiedBy
    , r.ModifiedOn
FROM dbo.SalesApprovalRequest r
WHERE r.ApprovalRequestId = @ApprovalRequestId;

SELECT
      a.ApprovalRequestApproverId
    , a.ApproverLoginId
    , a.ApproverRole
    , a.ApproverLevel
    , a.Status
    , a.ActionBy
    , a.ActionDate
    , a.Remarks
    , a.ModifiedBy
    , a.ModifiedOn
FROM dbo.SalesApprovalRequestApprover a
WHERE a.ApprovalRequestId = @ApprovalRequestId
ORDER BY a.ApproverLevel, a.ApprovalRequestApproverId;

SELECT
      MAX(h.HistoryId) AS MaxHistoryId
    , COUNT(*) AS HistoryRows
    , SUM(CASE
              WHEN h.OldStatus = N'Pending'
               AND h.NewStatus = N'Pending'
               AND h.Remarks LIKE @HistoryPrefix + N'%'
                  THEN 1 ELSE 0
          END) AS AuditorToL1HistoryRows
FROM dbo.ApprovalRequestStatusHistory h
WHERE h.ApprovalRequestId = @ApprovalRequestId;

SELECT
      h.HistoryId
    , h.OldStatus
    , h.NewStatus
    , h.ChangedBy
    , h.ChangedOn
    , h.Remarks
FROM dbo.ApprovalRequestStatusHistory h
WHERE h.ApprovalRequestId = @ApprovalRequestId
  AND h.Remarks LIKE @HistoryPrefix + N'%'
ORDER BY h.HistoryId;

/* Detail, activity, and expense checksum used by cases 1, 2, 3, and 4. */
SELECT
      d.ApprovalRequestDetailId
    , d.ActivityId
    , d.DetailVersion
    , d.PreviousDetailId
    , d.DetailStatus
    , d.Snapshot_TotalDistanceKM
    , d.Snapshot_CalculatedClaimAmount
    , d.Snapshot_DAAmount
    , d.Snapshot_HotelAmount
    , d.Snapshot_TravelAmount
    , d.Snapshot_StationaryAmount
    , d.Snapshot_ExpenseTotal
FROM dbo.SalesApprovalRequestDetail d
WHERE d.ApprovalRequestId = @ApprovalRequestId
ORDER BY d.ActivityId, d.DetailVersion, d.ApprovalRequestDetailId;

SELECT
      a.ActivityId
    , a.ActivityDate
    , a.ApprovalRequestId
    , a.ClaimStatus
    , a.TotalDistanceKM
    , a.CalculatedClaimAmount
FROM dbo.SalesDailyActivity a
WHERE a.ApprovalRequestId = @ApprovalRequestId
ORDER BY a.ActivityDate, a.ActivityId;

SELECT
      e.ExpenseId
    , e.ActivityId
    , a.ActivityDate
    , e.ExpenseType
    , e.Amount
    , e.ClaimStatus
    , e.IsDeleted
    , e.AttachmentFileName
    , e.AttachmentPath
    , DATALENGTH(e.AttachmentContent) AS AttachmentBytes
    , e.ModifiedBy
    , e.ModifiedOn
FROM dbo.SalesDailyExpense e
INNER JOIN dbo.SalesDailyActivity a
    ON a.ActivityId = e.ActivityId
WHERE a.ApprovalRequestId = @ApprovalRequestId
ORDER BY a.ActivityDate, e.ExpenseId;

/* Case 5: 01-Sep against every other day on the request. */
SELECT
      a.ActivityDate
    , a.ActivityId
    , a.ClaimStatus AS ActivityClaimStatus
    , latest.DetailStatus
    , latest.DetailVersion
    , latest.ApprovalRequestDetailId
    , SUM(CASE WHEN e.IsDeleted = 0 AND ISNULL(e.ClaimStatus, N'') = N'Rejected' THEN 1 ELSE 0 END) AS RejectedExpenses
    , SUM(CASE WHEN e.IsDeleted = 0 AND ISNULL(e.ClaimStatus, N'') = N'Returned' THEN 1 ELSE 0 END) AS ReturnedExpenses
    , SUM(CASE WHEN e.IsDeleted = 0 AND ISNULL(e.ClaimStatus, N'') NOT IN (N'Rejected', N'Returned') THEN 1 ELSE 0 END) AS OtherActiveExpenses
FROM dbo.SalesDailyActivity a
OUTER APPLY
(
    SELECT TOP (1)
          d.ApprovalRequestDetailId
        , d.DetailVersion
        , d.DetailStatus
    FROM dbo.SalesApprovalRequestDetail d
    WHERE d.ApprovalRequestId = a.ApprovalRequestId
      AND d.ActivityId = a.ActivityId
    ORDER BY ISNULL(d.DetailVersion, 1) DESC, d.ApprovalRequestDetailId DESC
) latest
LEFT JOIN dbo.SalesDailyExpense e
    ON e.ActivityId = a.ActivityId
WHERE a.ApprovalRequestId = @ApprovalRequestId
GROUP BY
      a.ActivityDate
    , a.ActivityId
    , a.ClaimStatus
    , latest.DetailStatus
    , latest.DetailVersion
    , latest.ApprovalRequestDetailId
ORDER BY a.ActivityDate, a.ActivityId;

/* Case 6: request id reuse and the next detail version for 01-Sep. */
SELECT
      a.ActivityId
    , a.ActivityDate
    , a.ApprovalRequestId
    , a.ClaimStatus
    , d.ApprovalRequestDetailId
    , d.DetailVersion
    , d.PreviousDetailId
    , d.DetailStatus
    , d.Snapshot_StationaryAmount
    , d.Snapshot_ExpenseTotal
FROM dbo.SalesDailyActivity a
INNER JOIN dbo.SalesApprovalRequestDetail d
    ON d.ActivityId = a.ActivityId
   AND d.ApprovalRequestId = a.ApprovalRequestId
WHERE a.ApprovalRequestId = @ApprovalRequestId
  AND a.ActivityDate = @ReturnedActivityDate
ORDER BY d.DetailVersion, d.ApprovalRequestDetailId;

SELECT
      e.ExpenseId
    , e.ExpenseType
    , e.Amount
    , e.ClaimStatus
    , e.IsDeleted
    , e.AttachmentFileName
    , e.AttachmentPath
    , DATALENGTH(e.AttachmentContent) AS AttachmentBytes
FROM dbo.SalesDailyExpense e
INNER JOIN dbo.SalesDailyActivity a
    ON a.ActivityId = e.ActivityId
WHERE a.ApprovalRequestId = @ApprovalRequestId
  AND a.ActivityDate = @ReturnedActivityDate
  AND e.ExpenseType = N'Stationary'
ORDER BY e.ExpenseId;

/*
    Case 7 is a source comparison, not a new rule.
    Submission aggregation includes every active expense.
    The expense-status snapshot excludes Rejected expenses.
    Both statements are existing behavior and are not executed here.
*/
SELECT
      e.ActivityId
    , e.ExpenseId
    , e.ExpenseType
    , e.Amount
    , e.ClaimStatus
    , CASE
          WHEN e.IsDeleted = 0 THEN N'Included by current submission aggregate'
          ELSE N'Excluded because the expense is soft-deleted'
      END AS SubmissionAggregateTreatment
    , CASE
          WHEN e.IsDeleted = 0 AND ISNULL(e.ClaimStatus, N'') <> N'Rejected'
              THEN N'Included by current expense-status snapshot'
          WHEN e.IsDeleted = 0 AND ISNULL(e.ClaimStatus, N'') = N'Rejected'
              THEN N'Excluded by expense-status snapshot; still included by submission aggregate'
          ELSE N'Excluded because the expense is soft-deleted'
      END AS ExistingDifference
FROM dbo.SalesDailyExpense e
INNER JOIN dbo.SalesDailyActivity a
    ON a.ActivityId = e.ActivityId
WHERE a.ApprovalRequestId = @ApprovalRequestId
ORDER BY a.ActivityDate, e.ExpenseId;
