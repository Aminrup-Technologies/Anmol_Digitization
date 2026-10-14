/*
    RC-014B read-only fixture discovery.
    Authoring status: NOT EXECUTED. This file is not UAT evidence
    and it does not approve a fixture.

    Target, when a human later runs it:
      server   MUMANMOAPP01\SQLEXPRESS
      database AnmolDristiDev

    Purpose:
      List existing approval requests so the database owner can choose
      a disposable employee, month, and ApprovalRequestId, and so a
      later recovery design has a before-image of the rows this UAT
      can change.

    This script does not establish owner designation, recovery
    approval, or write authorization.
    Every request it returns is a candidate for owner review only.
    Status = Pending is not a disposable designation.
    No claim is labeled disposable or safe to modify.

    Optional narrowing, edited by the owner before a run. NULL leaves
    that predicate open. All four NULL values list every request.
      @ApprovalRequestId
      @RequesterLoginId
      @YearNo
      @MonthNo

    Read-only. SELECT only.
    Does not EXEC a procedure.
    Does not use dynamic SQL.
    Does not INSERT, UPDATE, DELETE, or MERGE.
    Does not CREATE, ALTER, or DROP.
    Does not BEGIN, COMMIT, or ROLLBACK a transaction.

    Column names are taken from the checked-in table definitions:
      Database/Tables/SalesApprovalRequest.sql
      Database/Tables/SalesApprovalRequestApprover.sql
      Database/Tables/SalesApprovalRequestDetail.sql
      Database/Tables/SalesDailyActivity.sql
      Database/Tables/SalesDailyExpense.sql
      Database/Tables/ApprovalRequestStatusHistory.sql
      Database/Tables/SalesDailyActivityKMHistory.sql
    If one of those columns is absent on this database, the script
    prints the gap and returns. It does not substitute another column.

    Latest detail ownership, matching dbo.usp_RecalculateApprovalRequest:
      MAX(ISNULL(DetailVersion, 1)) for the activity inside the request,
      then MAX(ApprovalRequestDetailId) within that version.
    The six header amount columns are the stored values. The Sql*
    columns are a read of that same latest-detail sum, excluding
    current details whose DetailStatus is Rejected or Returned.
    They are a comparison baseline, not a write.

    Expense counts are separate and may overlap:
      ActiveExpenseCount   IsDeleted = 0
      DeletedExpenseCount  IsDeleted = 1
      RejectedExpenseCount ClaimStatus = Rejected
    Expense identity is ExpenseId.
    AttachmentContent, attachment names, passwords, employee names,
    remark text, and route text are not selected.
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
    PRINT N'NOT RUN | Refusing server ' + ISNULL(@Server, N'<null>') + N'. The authorized discovery instance is MUMANMOAPP01\SQLEXPRESS.';
    RETURN;
END;

IF DB_ID(N'AnmolDristiDev') IS NULL
BEGIN
    PRINT N'NOT RUN | Database AnmolDristiDev is not visible on this server.';
    RETURN;
END;

USE [AnmolDristiDev];

IF DB_NAME() <> N'AnmolDristiDev'
BEGIN
    PRINT N'NOT RUN | Refusing database ' + ISNULL(DB_NAME(), N'<null>') + N'. The authorized discovery database is AnmolDristiDev.';
    RETURN;
END;

PRINT N'CONTEXT | Server=' + ISNULL(CONVERT(nvarchar(256), SERVERPROPERTY(N'ServerName')), N'<null>');
PRINT N'CONTEXT | Database=' + ISNULL(DB_NAME(), N'<null>');
PRINT N'CONTEXT | ProductVersion=' + ISNULL(CONVERT(nvarchar(128), SERVERPROPERTY(N'ProductVersion')), N'<null>');
PRINT N'CONTEXT | Read-only discovery. Rows are candidates for owner review only.';
PRINT N'CONTEXT | This script does not establish owner designation, recovery approval, or write authorization.';

DECLARE @ApprovalRequestId int = NULL;
DECLARE @RequesterLoginId nvarchar(100) = NULL;
DECLARE @YearNo int = NULL;
DECLARE @MonthNo int = NULL;

IF EXISTS (
        SELECT 1
        WHERE COL_LENGTH(N'dbo.SalesApprovalRequest', N'ApprovalRequestId') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'RequesterLoginId') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'YearNo') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'MonthNo') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'Status') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'CurrentApproverLoginId') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'CurrentApproverLevel') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'ApprovedKM') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'ApprovedClaimAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'ApprovedDAAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'ApprovedHotelAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'ApprovedTravelAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequest', N'FinalApprovedAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApprovalRequestApproverId') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApprovalRequestId') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApproverLoginId') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApproverRole') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApproverLevel') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'Status') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'ApprovalRequestDetailId') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'ApprovalRequestId') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'ActivityId') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'DetailVersion') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'PreviousDetailId') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'DetailStatus') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_TotalDistanceKM') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_CalculatedClaimAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_DAAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_HotelAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_TravelAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_StationaryAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_ExpenseTotal') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivity', N'ActivityId') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivity', N'Login_Id') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivity', N'ActivityDate') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivity', N'ClaimStatus') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivity', N'TotalDistanceKM') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivity', N'CalculatedClaimAmount') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivity', N'ApprovalRequestId') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivity', N'IsDeleted') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyExpense', N'ExpenseId') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyExpense', N'ActivityId') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyExpense', N'ExpenseType') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyExpense', N'ExpenseDate') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyExpense', N'Amount') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyExpense', N'ClaimStatus') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyExpense', N'IsDeleted') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'HistoryId') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApprovalRequestId') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'OldStatus') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'NewStatus') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ChangedBy') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ChangedOn') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApprovalRequestDetailId') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ActivityId') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApproverLevel') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ActionType') IS NULL
           OR COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'DetailVersion') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'HistoryId') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ActivityId') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'OldKM') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'NewKM') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ModifiedBy') IS NULL
           OR COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ModifiedDateTime') IS NULL
    )
BEGIN
    PRINT N'NOT RUN | A required column from the checked-in table definition is missing. No replacement column is assumed.';
    SELECT
          v.TableName
        , v.ColumnName
    FROM (
            SELECT N'dbo.SalesApprovalRequest' AS TableName, N'ApprovalRequestId' AS ColumnName, COL_LENGTH(N'dbo.SalesApprovalRequest', N'ApprovalRequestId') AS ColumnLength
            UNION ALL SELECT N'dbo.SalesApprovalRequest', N'RequesterLoginId', COL_LENGTH(N'dbo.SalesApprovalRequest', N'RequesterLoginId')
            UNION ALL SELECT N'dbo.SalesApprovalRequest', N'YearNo', COL_LENGTH(N'dbo.SalesApprovalRequest', N'YearNo')
            UNION ALL SELECT N'dbo.SalesApprovalRequest', N'MonthNo', COL_LENGTH(N'dbo.SalesApprovalRequest', N'MonthNo')
            UNION ALL SELECT N'dbo.SalesApprovalRequest', N'Status', COL_LENGTH(N'dbo.SalesApprovalRequest', N'Status')
            UNION ALL SELECT N'dbo.SalesApprovalRequest', N'CurrentApproverLoginId', COL_LENGTH(N'dbo.SalesApprovalRequest', N'CurrentApproverLoginId')
            UNION ALL SELECT N'dbo.SalesApprovalRequest', N'CurrentApproverLevel', COL_LENGTH(N'dbo.SalesApprovalRequest', N'CurrentApproverLevel')
            UNION ALL SELECT N'dbo.SalesApprovalRequest', N'ApprovedKM', COL_LENGTH(N'dbo.SalesApprovalRequest', N'ApprovedKM')
            UNION ALL SELECT N'dbo.SalesApprovalRequest', N'ApprovedClaimAmount', COL_LENGTH(N'dbo.SalesApprovalRequest', N'ApprovedClaimAmount')
            UNION ALL SELECT N'dbo.SalesApprovalRequest', N'ApprovedDAAmount', COL_LENGTH(N'dbo.SalesApprovalRequest', N'ApprovedDAAmount')
            UNION ALL SELECT N'dbo.SalesApprovalRequest', N'ApprovedHotelAmount', COL_LENGTH(N'dbo.SalesApprovalRequest', N'ApprovedHotelAmount')
            UNION ALL SELECT N'dbo.SalesApprovalRequest', N'ApprovedTravelAmount', COL_LENGTH(N'dbo.SalesApprovalRequest', N'ApprovedTravelAmount')
            UNION ALL SELECT N'dbo.SalesApprovalRequest', N'FinalApprovedAmount', COL_LENGTH(N'dbo.SalesApprovalRequest', N'FinalApprovedAmount')
            UNION ALL SELECT N'dbo.SalesApprovalRequestApprover', N'ApprovalRequestApproverId', COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApprovalRequestApproverId')
            UNION ALL SELECT N'dbo.SalesApprovalRequestApprover', N'ApprovalRequestId', COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApprovalRequestId')
            UNION ALL SELECT N'dbo.SalesApprovalRequestApprover', N'ApproverLoginId', COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApproverLoginId')
            UNION ALL SELECT N'dbo.SalesApprovalRequestApprover', N'ApproverRole', COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApproverRole')
            UNION ALL SELECT N'dbo.SalesApprovalRequestApprover', N'ApproverLevel', COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'ApproverLevel')
            UNION ALL SELECT N'dbo.SalesApprovalRequestApprover', N'Status', COL_LENGTH(N'dbo.SalesApprovalRequestApprover', N'Status')
            UNION ALL SELECT N'dbo.SalesApprovalRequestDetail', N'ApprovalRequestDetailId', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'ApprovalRequestDetailId')
            UNION ALL SELECT N'dbo.SalesApprovalRequestDetail', N'ApprovalRequestId', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'ApprovalRequestId')
            UNION ALL SELECT N'dbo.SalesApprovalRequestDetail', N'ActivityId', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'ActivityId')
            UNION ALL SELECT N'dbo.SalesApprovalRequestDetail', N'DetailVersion', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'DetailVersion')
            UNION ALL SELECT N'dbo.SalesApprovalRequestDetail', N'PreviousDetailId', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'PreviousDetailId')
            UNION ALL SELECT N'dbo.SalesApprovalRequestDetail', N'DetailStatus', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'DetailStatus')
            UNION ALL SELECT N'dbo.SalesApprovalRequestDetail', N'Snapshot_TotalDistanceKM', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_TotalDistanceKM')
            UNION ALL SELECT N'dbo.SalesApprovalRequestDetail', N'Snapshot_CalculatedClaimAmount', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_CalculatedClaimAmount')
            UNION ALL SELECT N'dbo.SalesApprovalRequestDetail', N'Snapshot_DAAmount', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_DAAmount')
            UNION ALL SELECT N'dbo.SalesApprovalRequestDetail', N'Snapshot_HotelAmount', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_HotelAmount')
            UNION ALL SELECT N'dbo.SalesApprovalRequestDetail', N'Snapshot_TravelAmount', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_TravelAmount')
            UNION ALL SELECT N'dbo.SalesApprovalRequestDetail', N'Snapshot_StationaryAmount', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_StationaryAmount')
            UNION ALL SELECT N'dbo.SalesApprovalRequestDetail', N'Snapshot_ExpenseTotal', COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_ExpenseTotal')
            UNION ALL SELECT N'dbo.SalesDailyActivity', N'ActivityId', COL_LENGTH(N'dbo.SalesDailyActivity', N'ActivityId')
            UNION ALL SELECT N'dbo.SalesDailyActivity', N'Login_Id', COL_LENGTH(N'dbo.SalesDailyActivity', N'Login_Id')
            UNION ALL SELECT N'dbo.SalesDailyActivity', N'ActivityDate', COL_LENGTH(N'dbo.SalesDailyActivity', N'ActivityDate')
            UNION ALL SELECT N'dbo.SalesDailyActivity', N'ClaimStatus', COL_LENGTH(N'dbo.SalesDailyActivity', N'ClaimStatus')
            UNION ALL SELECT N'dbo.SalesDailyActivity', N'TotalDistanceKM', COL_LENGTH(N'dbo.SalesDailyActivity', N'TotalDistanceKM')
            UNION ALL SELECT N'dbo.SalesDailyActivity', N'CalculatedClaimAmount', COL_LENGTH(N'dbo.SalesDailyActivity', N'CalculatedClaimAmount')
            UNION ALL SELECT N'dbo.SalesDailyActivity', N'ApprovalRequestId', COL_LENGTH(N'dbo.SalesDailyActivity', N'ApprovalRequestId')
            UNION ALL SELECT N'dbo.SalesDailyActivity', N'IsDeleted', COL_LENGTH(N'dbo.SalesDailyActivity', N'IsDeleted')
            UNION ALL SELECT N'dbo.SalesDailyExpense', N'ExpenseId', COL_LENGTH(N'dbo.SalesDailyExpense', N'ExpenseId')
            UNION ALL SELECT N'dbo.SalesDailyExpense', N'ActivityId', COL_LENGTH(N'dbo.SalesDailyExpense', N'ActivityId')
            UNION ALL SELECT N'dbo.SalesDailyExpense', N'ExpenseType', COL_LENGTH(N'dbo.SalesDailyExpense', N'ExpenseType')
            UNION ALL SELECT N'dbo.SalesDailyExpense', N'ExpenseDate', COL_LENGTH(N'dbo.SalesDailyExpense', N'ExpenseDate')
            UNION ALL SELECT N'dbo.SalesDailyExpense', N'Amount', COL_LENGTH(N'dbo.SalesDailyExpense', N'Amount')
            UNION ALL SELECT N'dbo.SalesDailyExpense', N'ClaimStatus', COL_LENGTH(N'dbo.SalesDailyExpense', N'ClaimStatus')
            UNION ALL SELECT N'dbo.SalesDailyExpense', N'IsDeleted', COL_LENGTH(N'dbo.SalesDailyExpense', N'IsDeleted')
            UNION ALL SELECT N'dbo.ApprovalRequestStatusHistory', N'HistoryId', COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'HistoryId')
            UNION ALL SELECT N'dbo.ApprovalRequestStatusHistory', N'ApprovalRequestId', COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApprovalRequestId')
            UNION ALL SELECT N'dbo.ApprovalRequestStatusHistory', N'OldStatus', COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'OldStatus')
            UNION ALL SELECT N'dbo.ApprovalRequestStatusHistory', N'NewStatus', COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'NewStatus')
            UNION ALL SELECT N'dbo.ApprovalRequestStatusHistory', N'ChangedBy', COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ChangedBy')
            UNION ALL SELECT N'dbo.ApprovalRequestStatusHistory', N'ChangedOn', COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ChangedOn')
            UNION ALL SELECT N'dbo.ApprovalRequestStatusHistory', N'ApprovalRequestDetailId', COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApprovalRequestDetailId')
            UNION ALL SELECT N'dbo.ApprovalRequestStatusHistory', N'ActivityId', COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ActivityId')
            UNION ALL SELECT N'dbo.ApprovalRequestStatusHistory', N'ApproverLevel', COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApproverLevel')
            UNION ALL SELECT N'dbo.ApprovalRequestStatusHistory', N'ActionType', COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ActionType')
            UNION ALL SELECT N'dbo.ApprovalRequestStatusHistory', N'DetailVersion', COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'DetailVersion')
            UNION ALL SELECT N'dbo.SalesDailyActivityKMHistory', N'HistoryId', COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'HistoryId')
            UNION ALL SELECT N'dbo.SalesDailyActivityKMHistory', N'ActivityId', COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ActivityId')
            UNION ALL SELECT N'dbo.SalesDailyActivityKMHistory', N'OldKM', COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'OldKM')
            UNION ALL SELECT N'dbo.SalesDailyActivityKMHistory', N'NewKM', COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'NewKM')
            UNION ALL SELECT N'dbo.SalesDailyActivityKMHistory', N'ModifiedBy', COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ModifiedBy')
            UNION ALL SELECT N'dbo.SalesDailyActivityKMHistory', N'ModifiedDateTime', COL_LENGTH(N'dbo.SalesDailyActivityKMHistory', N'ModifiedDateTime')
        ) v
    WHERE v.ColumnLength IS NULL;
    RETURN;
END;

PRINT N'RESULT | CandidateRequest | CANDIDATE FOR OWNER REVIEW ONLY | Pending is not a disposable designation';

SELECT
      N'CANDIDATE FOR OWNER REVIEW ONLY' AS ReviewLabel
    , r.ApprovalRequestId
    , r.RequesterLoginId
    , r.YearNo
    , r.MonthNo
    , r.Status
    , r.CurrentApproverLoginId
    , r.CurrentApproverLevel
    , r.ApprovedKM
    , r.ApprovedClaimAmount
    , r.ApprovedDAAmount
    , r.ApprovedHotelAmount
    , r.ApprovedTravelAmount
    , r.FinalApprovedAmount
    , snap.SqlApprovedKM
    , snap.SqlApprovedClaimAmount
    , snap.SqlApprovedDAAmount
    , snap.SqlApprovedHotelAmount
    , snap.SqlApprovedTravelAmount
    , snap.SqlFinalApprovedAmount
    , ISNULL(cur.CurrentDetailCount, 0) AS CurrentDetailCount
    , ISNULL(l1.L1ApproverRowCount, 0) AS L1ApproverRowCount
    , ISNULL(l1.PendingL1RoleRowCount, 0) AS PendingL1RoleRowCount
    , ISNULL(ex.ActiveExpenseCount, 0) AS ActiveExpenseCount
    , ISNULL(ex.DeletedExpenseCount, 0) AS DeletedExpenseCount
    , ISNULL(ex.RejectedExpenseCount, 0) AS RejectedExpenseCount
FROM dbo.SalesApprovalRequest r
OUTER APPLY (
    SELECT COUNT(*) AS CurrentDetailCount
    FROM (
        SELECT d2.ActivityId, MAX(d2.ApprovalRequestDetailId) AS CurrentDetailId
        FROM dbo.SalesApprovalRequestDetail d2
        INNER JOIN (
            SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
            FROM dbo.SalesApprovalRequestDetail
            WHERE ApprovalRequestId = r.ApprovalRequestId
            GROUP BY ActivityId
        ) lv
            ON lv.ActivityId = d2.ActivityId
           AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
        WHERE d2.ApprovalRequestId = r.ApprovalRequestId
        GROUP BY d2.ActivityId
    ) currentDetail
) cur
OUTER APPLY (
    SELECT
          ISNULL(SUM(CONVERT(decimal(18, 2), d.Snapshot_TotalDistanceKM)), 0) AS SqlApprovedKM
        , ISNULL(SUM(d.Snapshot_CalculatedClaimAmount), 0) AS SqlApprovedClaimAmount
        , ISNULL(SUM(d.Snapshot_DAAmount), 0) AS SqlApprovedDAAmount
        , ISNULL(SUM(d.Snapshot_HotelAmount), 0) AS SqlApprovedHotelAmount
        , ISNULL(SUM(d.Snapshot_TravelAmount), 0) AS SqlApprovedTravelAmount
        , ISNULL(SUM(ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)), 0) AS SqlFinalApprovedAmount
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN (
        SELECT d2.ActivityId, MAX(d2.ApprovalRequestDetailId) AS CurrentDetailId
        FROM dbo.SalesApprovalRequestDetail d2
        INNER JOIN (
            SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
            FROM dbo.SalesApprovalRequestDetail
            WHERE ApprovalRequestId = r.ApprovalRequestId
            GROUP BY ActivityId
        ) lv
            ON lv.ActivityId = d2.ActivityId
           AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
        WHERE d2.ApprovalRequestId = r.ApprovalRequestId
        GROUP BY d2.ActivityId
    ) currentDetail
        ON currentDetail.CurrentDetailId = d.ApprovalRequestDetailId
    WHERE d.ApprovalRequestId = r.ApprovalRequestId
      AND ISNULL(d.DetailStatus, N'') NOT IN (N'Rejected', N'Returned')
) snap
OUTER APPLY (
    SELECT
          COUNT(*) AS L1ApproverRowCount
        , ISNULL(SUM(CASE
                WHEN a.Status = N'Pending'
                 AND UPPER(LTRIM(RTRIM(a.ApproverRole))) = N'L1'
                THEN 1 ELSE 0
            END), 0) AS PendingL1RoleRowCount
    FROM dbo.SalesApprovalRequestApprover a
    WHERE a.ApprovalRequestId = r.ApprovalRequestId
      AND a.ApproverLevel = 1
) l1
OUTER APPLY (
    SELECT
          ISNULL(SUM(CASE WHEN e.IsDeleted = 0 THEN 1 ELSE 0 END), 0) AS ActiveExpenseCount
        , ISNULL(SUM(CASE WHEN e.IsDeleted = 1 THEN 1 ELSE 0 END), 0) AS DeletedExpenseCount
        , ISNULL(SUM(CASE WHEN ISNULL(e.ClaimStatus, N'') = N'Rejected' THEN 1 ELSE 0 END), 0) AS RejectedExpenseCount
    FROM dbo.SalesDailyExpense e
    WHERE e.ActivityId IN (
            SELECT d.ActivityId
            FROM dbo.SalesApprovalRequestDetail d
            WHERE d.ApprovalRequestId = r.ApprovalRequestId
            UNION
            SELECT act.ActivityId
            FROM dbo.SalesDailyActivity act
            WHERE act.ApprovalRequestId = r.ApprovalRequestId
        )
) ex
WHERE (@ApprovalRequestId IS NULL OR r.ApprovalRequestId = @ApprovalRequestId)
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
  AND (@YearNo IS NULL OR r.YearNo = @YearNo)
  AND (@MonthNo IS NULL OR r.MonthNo = @MonthNo)
ORDER BY r.ApprovalRequestId;

PRINT N'RESULT | L1Approver | level 1 rows only | CANDIDATE FOR OWNER REVIEW ONLY';

SELECT
      N'CANDIDATE FOR OWNER REVIEW ONLY' AS ReviewLabel
    , a.ApprovalRequestApproverId
    , a.ApprovalRequestId
    , r.RequesterLoginId
    , r.YearNo
    , r.MonthNo
    , a.ApproverLoginId
    , a.ApproverRole
    , a.ApproverLevel
    , a.Status
FROM dbo.SalesApprovalRequestApprover a
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = a.ApprovalRequestId
WHERE a.ApproverLevel = 1
  AND (@ApprovalRequestId IS NULL OR r.ApprovalRequestId = @ApprovalRequestId)
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
  AND (@YearNo IS NULL OR r.YearNo = @YearNo)
  AND (@MonthNo IS NULL OR r.MonthNo = @MonthNo)
ORDER BY a.ApprovalRequestId, a.ApprovalRequestApproverId;

PRINT N'RESULT | DetailVersion | IsCurrentDetail uses MAX(ISNULL(DetailVersion, 1)) then MAX(ApprovalRequestDetailId)';

SELECT
      N'CANDIDATE FOR OWNER REVIEW ONLY' AS ReviewLabel
    , d.ApprovalRequestId
    , d.ApprovalRequestDetailId
    , d.ActivityId
    , d.DetailVersion
    , d.PreviousDetailId
    , d.DetailStatus
    , CASE WHEN d.ApprovalRequestDetailId = currentDetail.CurrentDetailId THEN 1 ELSE 0 END AS IsCurrentDetail
    , d.Snapshot_TotalDistanceKM
    , d.Snapshot_CalculatedClaimAmount
    , d.Snapshot_DAAmount
    , d.Snapshot_HotelAmount
    , d.Snapshot_TravelAmount
    , d.Snapshot_StationaryAmount
    , d.Snapshot_ExpenseTotal
FROM dbo.SalesApprovalRequestDetail d
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = d.ApprovalRequestId
INNER JOIN (
    SELECT d2.ApprovalRequestId, d2.ActivityId, MAX(d2.ApprovalRequestDetailId) AS CurrentDetailId
    FROM dbo.SalesApprovalRequestDetail d2
    INNER JOIN (
        SELECT ApprovalRequestId, ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
        FROM dbo.SalesApprovalRequestDetail
        GROUP BY ApprovalRequestId, ActivityId
    ) lv
        ON lv.ApprovalRequestId = d2.ApprovalRequestId
       AND lv.ActivityId = d2.ActivityId
       AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
    GROUP BY d2.ApprovalRequestId, d2.ActivityId
) currentDetail
    ON currentDetail.ApprovalRequestId = d.ApprovalRequestId
   AND currentDetail.ActivityId = d.ActivityId
WHERE (@ApprovalRequestId IS NULL OR r.ApprovalRequestId = @ApprovalRequestId)
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
  AND (@YearNo IS NULL OR r.YearNo = @YearNo)
  AND (@MonthNo IS NULL OR r.MonthNo = @MonthNo)
ORDER BY d.ApprovalRequestId, d.ActivityId, d.DetailVersion, d.ApprovalRequestDetailId;

PRINT N'RESULT | CurrentStatusSummary | current detail status and activity status';

SELECT
      N'CANDIDATE FOR OWNER REVIEW ONLY' AS ReviewLabel
    , d.ApprovalRequestId
    , ISNULL(d.DetailStatus, N'') AS DetailStatus
    , ISNULL(a.ClaimStatus, N'') AS ActivityClaimStatus
    , COUNT(*) AS CurrentRowCount
FROM dbo.SalesApprovalRequestDetail d
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = d.ApprovalRequestId
INNER JOIN (
    SELECT d2.ApprovalRequestId, d2.ActivityId, MAX(d2.ApprovalRequestDetailId) AS CurrentDetailId
    FROM dbo.SalesApprovalRequestDetail d2
    INNER JOIN (
        SELECT ApprovalRequestId, ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
        FROM dbo.SalesApprovalRequestDetail
        GROUP BY ApprovalRequestId, ActivityId
    ) lv
        ON lv.ApprovalRequestId = d2.ApprovalRequestId
       AND lv.ActivityId = d2.ActivityId
       AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
    GROUP BY d2.ApprovalRequestId, d2.ActivityId
) currentDetail
    ON currentDetail.CurrentDetailId = d.ApprovalRequestDetailId
LEFT JOIN dbo.SalesDailyActivity a
    ON a.ActivityId = d.ActivityId
WHERE (@ApprovalRequestId IS NULL OR r.ApprovalRequestId = @ApprovalRequestId)
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
  AND (@YearNo IS NULL OR r.YearNo = @YearNo)
  AND (@MonthNo IS NULL OR r.MonthNo = @MonthNo)
GROUP BY d.ApprovalRequestId, ISNULL(d.DetailStatus, N''), ISNULL(a.ClaimStatus, N'')
ORDER BY d.ApprovalRequestId, DetailStatus, ActivityClaimStatus;

PRINT N'RESULT | Activity | detail-linked and header-linked activities';

SELECT
      N'CANDIDATE FOR OWNER REVIEW ONLY' AS ReviewLabel
    , scopeRow.ApprovalRequestId
    , scopeRow.ActivityLink
    , a.ActivityId
    , a.Login_Id
    , a.ActivityDate
    , a.ClaimStatus
    , a.ApprovalRequestId AS ActivityApprovalRequestId
    , a.IsDeleted
    , a.TotalDistanceKM
    , a.CalculatedClaimAmount
    , (
        SELECT COUNT(DISTINCT otherDetail.ApprovalRequestId)
        FROM dbo.SalesApprovalRequestDetail otherDetail
        WHERE otherDetail.ActivityId = a.ActivityId
          AND otherDetail.ApprovalRequestId <> scopeRow.ApprovalRequestId
      ) AS OtherRequestCount
FROM (
        SELECT d.ApprovalRequestId, d.ActivityId, N'Detail' AS ActivityLink
        FROM dbo.SalesApprovalRequestDetail d
        UNION
        SELECT act.ApprovalRequestId, act.ActivityId, N'HeaderOnly' AS ActivityLink
        FROM dbo.SalesDailyActivity act
        WHERE act.ApprovalRequestId IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.SalesApprovalRequestDetail d
                WHERE d.ApprovalRequestId = act.ApprovalRequestId
                  AND d.ActivityId = act.ActivityId
            )
    ) scopeRow
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = scopeRow.ApprovalRequestId
INNER JOIN dbo.SalesDailyActivity a
    ON a.ActivityId = scopeRow.ActivityId
WHERE (@ApprovalRequestId IS NULL OR r.ApprovalRequestId = @ApprovalRequestId)
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
  AND (@YearNo IS NULL OR r.YearNo = @YearNo)
  AND (@MonthNo IS NULL OR r.MonthNo = @MonthNo)
ORDER BY scopeRow.ApprovalRequestId, a.ActivityId, scopeRow.ActivityLink;

PRINT N'RESULT | Expense | ExpenseId is the primary key | active, deleted, and rejected counts are in CandidateRequest';

SELECT
      N'CANDIDATE FOR OWNER REVIEW ONLY' AS ReviewLabel
    , scopeRow.ApprovalRequestId
    , e.ExpenseId
    , e.ActivityId
    , e.ExpenseType
    , e.ExpenseDate
    , e.Amount
    , e.ClaimStatus
    , e.IsDeleted
FROM (
        SELECT DISTINCT d.ApprovalRequestId, d.ActivityId
        FROM dbo.SalesApprovalRequestDetail d
        UNION
        SELECT act.ApprovalRequestId, act.ActivityId
        FROM dbo.SalesDailyActivity act
        WHERE act.ApprovalRequestId IS NOT NULL
    ) scopeRow
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = scopeRow.ApprovalRequestId
INNER JOIN dbo.SalesDailyExpense e
    ON e.ActivityId = scopeRow.ActivityId
WHERE (@ApprovalRequestId IS NULL OR r.ApprovalRequestId = @ApprovalRequestId)
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
  AND (@YearNo IS NULL OR r.YearNo = @YearNo)
  AND (@MonthNo IS NULL OR r.MonthNo = @MonthNo)
ORDER BY scopeRow.ApprovalRequestId, e.ActivityId, e.ExpenseId;

PRINT N'RESULT | StatusHistory | remark text is omitted | recovery baseline only';

SELECT
      N'CANDIDATE FOR OWNER REVIEW ONLY' AS ReviewLabel
    , h.HistoryId
    , h.ApprovalRequestId
    , h.OldStatus
    , h.NewStatus
    , h.ChangedBy
    , h.ChangedOn
    , h.ApprovalRequestDetailId
    , h.ActivityId
    , h.ApproverLevel
    , h.ActionType
    , h.DetailVersion
FROM dbo.ApprovalRequestStatusHistory h
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = h.ApprovalRequestId
WHERE (@ApprovalRequestId IS NULL OR r.ApprovalRequestId = @ApprovalRequestId)
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
  AND (@YearNo IS NULL OR r.YearNo = @YearNo)
  AND (@MonthNo IS NULL OR r.MonthNo = @MonthNo)
ORDER BY h.ApprovalRequestId, h.HistoryId;

PRINT N'RESULT | KMHistory | route text and reason text are omitted | recovery baseline only';

SELECT
      N'CANDIDATE FOR OWNER REVIEW ONLY' AS ReviewLabel
    , scopeRow.ApprovalRequestId
    , k.HistoryId
    , k.ActivityId
    , k.OldKM
    , k.NewKM
    , k.ModifiedBy
    , k.ModifiedDateTime
FROM (
        SELECT DISTINCT d.ApprovalRequestId, d.ActivityId
        FROM dbo.SalesApprovalRequestDetail d
        UNION
        SELECT act.ApprovalRequestId, act.ActivityId
        FROM dbo.SalesDailyActivity act
        WHERE act.ApprovalRequestId IS NOT NULL
    ) scopeRow
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = scopeRow.ApprovalRequestId
INNER JOIN dbo.SalesDailyActivityKMHistory k
    ON k.ActivityId = scopeRow.ActivityId
WHERE (@ApprovalRequestId IS NULL OR r.ApprovalRequestId = @ApprovalRequestId)
  AND (@RequesterLoginId IS NULL OR r.RequesterLoginId = @RequesterLoginId)
  AND (@YearNo IS NULL OR r.YearNo = @YearNo)
  AND (@MonthNo IS NULL OR r.MonthNo = @MonthNo)
ORDER BY scopeRow.ApprovalRequestId, k.ActivityId, k.HistoryId;

PRINT N'CONTEXT | Discovery complete. No request was designated disposable.';
PRINT N'CONTEXT | This script does not establish owner designation, recovery approval, or write authorization.';
