/*
    AHS1368 September 2026 claim reconciliation
    Server:   MUMANMOAPP01\SQLEXPRESS
    Database: AnmolDristi

    REVIEW COPY. @CommitChanges stays 0.
    Do not set it to 1 until the result sets from a commit-0 run are accepted
    and execution is explicitly approved again.

    SSMS:
      1. Connect to MUMANMOAPP01\SQLEXPRESS.
      2. Select database AnmolDristi. The script also checks @@SERVERNAME and DB_NAME().
      3. Leave @CommitChanges = 0 and execute the whole script once.
      4. Review every result set. PostTransactionState must still show 1173 Pending
         and 1169 Returned, because the preview rolls back.
      5. Only after that review, and a separate execution approval, set
         @CommitChanges = 1 and execute once.

    Scope is requests 1169 and 1173 only.
    1169 is the canonical September claim. 1173 is closed as Rejected.
    The script does not call usp_ReturnClaimToEmployee. That procedure would
    require a synthetic Pending state and would overwrite approver row 2309.
    It calls only dbo.usp_RecalculateApprovalRequest, after checking that the
    live definition has no BEGIN TRAN and no COMMIT.

    This script does not insert approval history and does not move history
    rows 4560, 4563, 4565, or 4569.

    COMMIT SWITCH
      0 = preview. Updates run inside a transaction and are rolled back.
      1 = commit. Set this only after the preview result sets are accepted.
*/
DECLARE @CommitChanges bit = 0;

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 5000;
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

DECLARE @Failure TABLE
(
    Item NVARCHAR(40) NULL,
    Reason NVARCHAR(300) NOT NULL
);

DECLARE @BeforeHeader TABLE
(
    ApprovalRequestId INT NOT NULL PRIMARY KEY,
    RequesterLoginId NVARCHAR(100) NOT NULL,
    YearNo INT NOT NULL,
    MonthNo INT NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    CurrentApproverLoginId NVARCHAR(100) NULL,
    CurrentApproverLevel INT NULL,
    TotalKM DECIMAL(18,2) NULL,
    TotalClaimAmount DECIMAL(18,2) NULL,
    TotalDAAmount DECIMAL(18,2) NULL,
    TotalHotelAmount DECIMAL(18,2) NULL,
    TotalTravelAmount DECIMAL(18,2) NULL,
    TotalExpenseAmount DECIMAL(18,2) NULL,
    TotalStationaryAmount DECIMAL(18,2) NULL,
    ApprovedKM DECIMAL(18,2) NULL,
    ApprovedClaimAmount DECIMAL(18,2) NULL,
    ApprovedDAAmount DECIMAL(18,2) NULL,
    ApprovedHotelAmount DECIMAL(18,2) NULL,
    ApprovedTravelAmount DECIMAL(18,2) NULL,
    FinalApprovedAmount DECIMAL(18,2) NULL,
    ModifiedBy NVARCHAR(100) NULL
);

DECLARE @BeforeApprover TABLE
(
    ApprovalRequestApproverId INT NOT NULL PRIMARY KEY,
    ApprovalRequestId INT NOT NULL,
    ApproverLoginId NVARCHAR(100) NULL,
    ApproverRole NVARCHAR(100) NULL,
    ApproverLevel INT NULL,
    Status NVARCHAR(50) NOT NULL,
    ActionBy NVARCHAR(100) NULL,
    ActionDate DATETIME2 NULL,
    Remarks NVARCHAR(1000) NULL
);

DECLARE @BeforeDetail TABLE
(
    ApprovalRequestDetailId INT NOT NULL PRIMARY KEY,
    ApprovalRequestId INT NOT NULL,
    ActivityId INT NOT NULL,
    DetailVersion INT NOT NULL,
    PreviousDetailId INT NULL,
    DetailStatus NVARCHAR(50) NULL,
    DetailApprovedBy NVARCHAR(100) NULL,
    DetailApprovedLevel INT NULL,
    DetailApprovedOn DATETIME2 NULL,
    Snapshot_TotalDistanceKM DECIMAL(18,2) NULL,
    Snapshot_CalculatedClaimAmount DECIMAL(18,2) NULL,
    Snapshot_DAAmount DECIMAL(18,2) NULL,
    Snapshot_HotelAmount DECIMAL(18,2) NULL,
    Snapshot_TravelAmount DECIMAL(18,2) NULL,
    Snapshot_StationaryAmount DECIMAL(18,2) NULL,
    Snapshot_ExpenseTotal DECIMAL(18,2) NULL
);

DECLARE @BeforeHistory TABLE
(
    HistoryId INT NOT NULL PRIMARY KEY,
    ApprovalRequestId INT NOT NULL,
    OldStatus NVARCHAR(50) NULL,
    NewStatus NVARCHAR(50) NOT NULL,
    ChangedBy NVARCHAR(100) NULL,
    ChangedOn DATETIME2 NOT NULL,
    Remarks NVARCHAR(1000) NULL,
    ApprovalRequestDetailId INT NULL,
    ActivityId INT NULL
);

DECLARE @BeforeActivity TABLE
(
    ActivityId INT NOT NULL PRIMARY KEY,
    Login_Id NVARCHAR(100) NULL,
    ActivityDate DATE NULL,
    ApprovalRequestId INT NULL,
    ClaimStatus NVARCHAR(50) NULL,
    TotalDistanceKM DECIMAL(18,2) NULL,
    CalculatedClaimAmount DECIMAL(18,2) NULL,
    ClaimedDA DECIMAL(18,2) NULL,
    ApprovedDA DECIMAL(18,2) NULL,
    ApprovedDistanceKM DECIMAL(18,2) NULL,
    ApprovedCalculatedClaimAmount DECIMAL(18,2) NULL
);

DECLARE @BeforeExpense TABLE
(
    ExpenseId INT NOT NULL PRIMARY KEY,
    ActivityId INT NOT NULL,
    ExpenseType NVARCHAR(50) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    ClaimStatus NVARCHAR(50) NULL,
    IsDeleted BIT NOT NULL
);

IF DB_NAME() <> N'AnmolDristi'
   OR @@SERVERNAME <> N'MUMANMOAPP01\SQLEXPRESS'
    INSERT INTO @Failure (Item, Reason)
    VALUES (NULL, N'Connected session is not MUMANMOAPP01\SQLEXPRESS / AnmolDristi.');

DECLARE @RecalcDefinition NVARCHAR(MAX) =
    OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest'));

IF @RecalcDefinition IS NULL
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_RecalculateApprovalRequest', N'Procedure definition was not found.');
IF @RecalcDefinition IS NOT NULL AND CHARINDEX(N'BEGIN TRAN', @RecalcDefinition) > 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_RecalculateApprovalRequest', N'Procedure contains BEGIN TRAN and can defeat the outer rollback.');
IF @RecalcDefinition IS NOT NULL AND CHARINDEX(N'COMMIT', @RecalcDefinition) > 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_RecalculateApprovalRequest', N'Procedure contains COMMIT and can defeat the outer rollback.');
IF @RecalcDefinition IS NOT NULL AND CHARINDEX(N'TotalClaimAmount', @RecalcDefinition) > 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_RecalculateApprovalRequest', N'Procedure now references claimed totals.');
IF @RecalcDefinition IS NOT NULL
   AND CHARINDEX(N'NOT IN (''Rejected'', ''Returned'')', @RecalcDefinition) = 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_RecalculateApprovalRequest', N'Returned-row exclusion was not found in the live definition.');

IF EXISTS (
    SELECT 1
    FROM sys.triggers
    WHERE parent_id IN (
        OBJECT_ID(N'dbo.SalesApprovalRequest'),
        OBJECT_ID(N'dbo.SalesApprovalRequestDetail'),
        OBJECT_ID(N'dbo.SalesApprovalRequestApprover'),
        OBJECT_ID(N'dbo.ApprovalRequestStatusHistory'),
        OBJECT_ID(N'dbo.SalesDailyActivity'),
        OBJECT_ID(N'dbo.SalesDailyExpense')
    )
)
    INSERT INTO @Failure (Item, Reason)
    VALUES (NULL, N'A trigger exists on a table this script changes.');

INSERT INTO @BeforeHeader
(
    ApprovalRequestId, RequesterLoginId, YearNo, MonthNo, Status,
    CurrentApproverLoginId, CurrentApproverLevel,
    TotalKM, TotalClaimAmount, TotalDAAmount, TotalHotelAmount, TotalTravelAmount,
    TotalExpenseAmount, TotalStationaryAmount,
    ApprovedKM, ApprovedClaimAmount, ApprovedDAAmount, ApprovedHotelAmount, ApprovedTravelAmount,
    FinalApprovedAmount, ModifiedBy
)
SELECT
    r.ApprovalRequestId, r.RequesterLoginId, r.YearNo, r.MonthNo, r.Status,
    r.CurrentApproverLoginId, r.CurrentApproverLevel,
    r.TotalKM, r.TotalClaimAmount, r.TotalDAAmount, r.TotalHotelAmount, r.TotalTravelAmount,
    r.TotalExpenseAmount, r.TotalStationaryAmount,
    r.ApprovedKM, r.ApprovedClaimAmount, r.ApprovedDAAmount, r.ApprovedHotelAmount, r.ApprovedTravelAmount,
    r.FinalApprovedAmount, r.ModifiedBy
FROM dbo.SalesApprovalRequest r
WHERE r.ApprovalRequestId IN (1169, 1173);

INSERT INTO @BeforeApprover
(
    ApprovalRequestApproverId, ApprovalRequestId, ApproverLoginId, ApproverRole,
    ApproverLevel, Status, ActionBy, ActionDate, Remarks
)
SELECT
    a.ApprovalRequestApproverId, a.ApprovalRequestId, a.ApproverLoginId, a.ApproverRole,
    a.ApproverLevel, a.Status, a.ActionBy, a.ActionDate, a.Remarks
FROM dbo.SalesApprovalRequestApprover a
WHERE a.ApprovalRequestId IN (1169, 1173);

INSERT INTO @BeforeDetail
(
    ApprovalRequestDetailId, ApprovalRequestId, ActivityId, DetailVersion, PreviousDetailId,
    DetailStatus, DetailApprovedBy, DetailApprovedLevel, DetailApprovedOn,
    Snapshot_TotalDistanceKM, Snapshot_CalculatedClaimAmount, Snapshot_DAAmount,
    Snapshot_HotelAmount, Snapshot_TravelAmount, Snapshot_StationaryAmount, Snapshot_ExpenseTotal
)
SELECT
    d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId, d.DetailVersion, d.PreviousDetailId,
    d.DetailStatus, d.DetailApprovedBy, d.DetailApprovedLevel, d.DetailApprovedOn,
    d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount, d.Snapshot_DAAmount,
    d.Snapshot_HotelAmount, d.Snapshot_TravelAmount, d.Snapshot_StationaryAmount, d.Snapshot_ExpenseTotal
FROM dbo.SalesApprovalRequestDetail d
WHERE d.ApprovalRequestId IN (1169, 1173);

INSERT INTO @BeforeHistory
(
    HistoryId, ApprovalRequestId, OldStatus, NewStatus, ChangedBy, ChangedOn, Remarks,
    ApprovalRequestDetailId, ActivityId
)
SELECT
    h.HistoryId, h.ApprovalRequestId, h.OldStatus, h.NewStatus, h.ChangedBy, h.ChangedOn, h.Remarks,
    h.ApprovalRequestDetailId, h.ActivityId
FROM dbo.ApprovalRequestStatusHistory h
WHERE h.ApprovalRequestId IN (1169, 1173)
   OR h.HistoryId IN (4560, 4563, 4565, 4569);

INSERT INTO @BeforeActivity
(
    ActivityId, Login_Id, ActivityDate, ApprovalRequestId, ClaimStatus,
    TotalDistanceKM, CalculatedClaimAmount, ClaimedDA, ApprovedDA,
    ApprovedDistanceKM, ApprovedCalculatedClaimAmount
)
SELECT
    a.ActivityId, a.Login_Id, a.ActivityDate, a.ApprovalRequestId, a.ClaimStatus,
    a.TotalDistanceKM, a.CalculatedClaimAmount, a.ClaimedDA, a.ApprovedDA,
    a.ApprovedDistanceKM, a.ApprovedCalculatedClaimAmount
FROM dbo.SalesDailyActivity a
WHERE a.Login_Id = N'AHS1368'
  AND a.ActivityDate >= CONVERT(date, '20260901', 112)
  AND a.ActivityDate < CONVERT(date, '20261001', 112);

INSERT INTO @BeforeExpense (ExpenseId, ActivityId, ExpenseType, Amount, ClaimStatus, IsDeleted)
SELECT e.ExpenseId, e.ActivityId, e.ExpenseType, e.Amount, e.ClaimStatus, e.IsDeleted
FROM dbo.SalesDailyExpense e
WHERE e.ActivityId IN (SELECT b.ActivityId FROM @BeforeActivity b);

INSERT INTO @Failure (Item, Reason)
SELECT v.Item, v.Reason
FROM (VALUES
    (CAST(1169 AS NVARCHAR(40)), CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHeader
        WHERE ApprovalRequestId = 1169 AND RequesterLoginId = N'AHS1368' AND YearNo = 2026 AND MonthNo = 9
          AND Status = N'Returned' AND CurrentApproverLoginId = N'AHS1181' AND CurrentApproverLevel = 1
          AND TotalKM = 3191.41 AND TotalClaimAmount = 11967.80 AND TotalDAAmount = 6525.00
          AND ISNULL(TotalHotelAmount, 0) = 0 AND ISNULL(TotalTravelAmount, 0) = 0
          AND TotalExpenseAmount = 6525.00 AND TotalStationaryAmount = 0
          AND ApprovedKM = 2074.41 AND ApprovedClaimAmount = 7779.05 AND ApprovedDAAmount = 6075.00
          AND ISNULL(ApprovedHotelAmount, 0) = 0 AND ISNULL(ApprovedTravelAmount, 0) = 0
          AND FinalApprovedAmount = 13854.05 AND ModifiedBy = N'AHS1181'
     ) THEN N'Request 1169 is not the expected Returned canonical header.' END),
    (N'1173', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHeader
        WHERE ApprovalRequestId = 1173 AND RequesterLoginId = N'AHS1368' AND YearNo = 2026 AND MonthNo = 9
          AND Status = N'Pending' AND CurrentApproverLoginId = N'Audit' AND CurrentApproverLevel = 2
          AND TotalKM = 1117.00 AND TotalClaimAmount = 4188.75 AND TotalDAAmount = 675.00
          AND ApprovedKM = 1117.00 AND ApprovedClaimAmount = 4188.75 AND ApprovedDAAmount = 225.00
          AND FinalApprovedAmount = 4413.75 AND ModifiedBy = N'AHS1181'
     ) THEN N'Request 1173 is not the expected Pending duplicate header.' END),
    (N'2309', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeApprover
        WHERE ApprovalRequestApproverId = 2309 AND ApprovalRequestId = 1169
          AND ApproverLoginId = N'AHS1181' AND ApproverRole = N'Reporting_Manager1' AND ApproverLevel = 1
          AND Status = N'Returned' AND ActionBy = N'AHS1181'
          AND Remarks = N'Incorrect - Added by Ranabir Ghosh (AHS1181)'
     ) THEN N'Approver 2309 is not the preserved Returned Reporting_Manager1 row.' END),
    (N'2310', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeApprover
        WHERE ApprovalRequestApproverId = 2310 AND ApprovalRequestId = 1169
          AND ApproverLoginId = N'Audit' AND ApproverRole = N'Auditor' AND ApproverLevel = 2
          AND Status = N'Pending' AND ActionBy IS NULL AND ActionDate IS NULL
     ) THEN N'Approver 2310 is not the unactioned Auditor row on 1169.' END),
    (N'2317', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeApprover
        WHERE ApprovalRequestApproverId = 2317 AND ApprovalRequestId = 1173
          AND ApproverLoginId = N'AHS1181' AND ApproverRole = N'Reporting_Manager1' AND ApproverLevel = 1
          AND Status = N'Approved' AND ActionBy = N'AHS1181'
          AND Remarks = N'Approved via Unified Day Modal - Added by Ranabir Ghosh (AHS1181)'
     ) THEN N'Approver 2317 is not the preserved L1 approval on 1173.' END),
    (N'2318', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeApprover
        WHERE ApprovalRequestApproverId = 2318 AND ApprovalRequestId = 1173
          AND ApproverLoginId = N'Audit' AND ApproverRole = N'Auditor' AND ApproverLevel = 2
          AND Status = N'Pending' AND ActionBy IS NULL AND ActionDate IS NULL
     ) THEN N'Approver 2318 is not the unactioned Auditor row on 1173.' END),
    (N'35468', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeDetail
        WHERE ApprovalRequestDetailId = 35468 AND ApprovalRequestId = 1169 AND ActivityId = 96111
          AND DetailVersion = 1 AND PreviousDetailId IS NULL AND DetailStatus = N'Returned'
     ) THEN N'Detail 35468 lineage is not version 1 Returned with no predecessor.' END),
    (N'46300', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeDetail
        WHERE ApprovalRequestDetailId = 46300 AND ApprovalRequestId = 1169 AND ActivityId = 96111
          AND DetailVersion = 2 AND PreviousDetailId = 35468 AND DetailStatus = N'Approved'
          AND DetailApprovedBy = N'AHS1181' AND DetailApprovedLevel = 1
     ) THEN N'Detail 46300 is not version 2 Approved with predecessor 35468.' END),
    (N'35580', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeDetail
        WHERE ApprovalRequestDetailId = 35580 AND ApprovalRequestId = 1173 AND ActivityId = 96111
          AND DetailVersion = 1 AND PreviousDetailId IS NULL AND DetailStatus = N'Approved'
     ) THEN N'Detail 35580 is not an independent version 1 row on 1173.' END)
) v (Item, Reason)
WHERE v.Reason IS NOT NULL;

IF (SELECT COUNT(*) FROM @BeforeHeader) <> 2
    INSERT INTO @Failure (Item, Reason) VALUES (NULL, N'Header before-image is not exactly two requests.');
IF (SELECT COUNT(*) FROM @BeforeApprover) <> 4
    INSERT INTO @Failure (Item, Reason) VALUES (NULL, N'Approver before-image is not exactly four rows.');
IF (SELECT COUNT(*) FROM @BeforeDetail WHERE ApprovalRequestId = 1169) <> 31
    INSERT INTO @Failure (Item, Reason) VALUES (N'1169', N'Detail before-image for 1169 is not 31 rows.');
IF (SELECT COUNT(*) FROM @BeforeDetail WHERE ApprovalRequestId = 1173) <> 1
    INSERT INTO @Failure (Item, Reason) VALUES (N'1173', N'Detail before-image for 1173 is not the single row 35580.');
IF (SELECT COUNT(*) FROM @BeforeActivity) <> 30
    OR EXISTS (SELECT 1 FROM @BeforeActivity WHERE ApprovalRequestId <> 1169 OR Login_Id <> N'AHS1368')
    INSERT INTO @Failure (Item, Reason) VALUES (NULL, N'September activity ownership is not 30 rows on request 1169.');
IF (SELECT COUNT(*) FROM @BeforeActivity WHERE ClaimStatus = N'Submitted') <> 29
    OR (SELECT COUNT(*) FROM @BeforeActivity WHERE ClaimStatus = N'Approved' AND ActivityId = 96111) <> 1
    INSERT INTO @Failure (Item, Reason) VALUES (NULL, N'Activity statuses are not 29 Submitted plus activity 96111 Approved.');
IF (SELECT COUNT(*) FROM @BeforeHistory) <> 4
    OR EXISTS (SELECT 1 FROM @BeforeHistory WHERE ApprovalRequestId <> 1169)
    OR EXISTS (SELECT 1 FROM @BeforeHistory WHERE HistoryId NOT IN (4560, 4563, 4565, 4569))
    INSERT INTO @Failure (Item, Reason) VALUES (NULL, N'History before-image is not the four rows on request 1169.');

IF EXISTS (
    SELECT 1
    FROM @BeforeActivity a
    WHERE NOT EXISTS (
        SELECT 1 FROM @BeforeDetail d
        WHERE d.ApprovalRequestId = 1169 AND d.ActivityId = a.ActivityId
    )
)
    INSERT INTO @Failure (Item, Reason) VALUES (NULL, N'A September activity has no detail on request 1169.');

;WITH Latest AS (
    SELECT d.ApprovalRequestDetailId, d.ActivityId, d.DetailStatus
    FROM @BeforeDetail d
    INNER JOIN (
        SELECT d2.ActivityId, MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
        FROM @BeforeDetail d2
        INNER JOIN (
            SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
            FROM @BeforeDetail
            WHERE ApprovalRequestId = 1169
            GROUP BY ActivityId
        ) lv ON lv.ActivityId = d2.ActivityId AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
        WHERE d2.ApprovalRequestId = 1169
        GROUP BY d2.ActivityId
    ) x ON x.LatestDetailId = d.ApprovalRequestDetailId
    WHERE d.ApprovalRequestId = 1169
)
INSERT INTO @Failure (Item, Reason)
SELECT N'Latest', v.Reason
FROM (VALUES
    (CASE WHEN (SELECT COUNT(*) FROM Latest) <> 30 THEN N'Latest-detail count on 1169 is not 30.' END),
    (CASE WHEN EXISTS (SELECT 1 FROM Latest WHERE ISNULL(DetailStatus, N'') = N'Rejected') THEN N'A latest detail is Rejected.' END),
    (CASE WHEN EXISTS (SELECT 1 FROM Latest WHERE ActivityId = 96111 AND ApprovalRequestDetailId <> 46300) THEN N'Latest detail for activity 96111 is not 46300.' END),
    (CASE WHEN EXISTS (SELECT 1 FROM Latest WHERE ApprovalRequestDetailId = 35468) THEN N'Historical detail 35468 is being treated as latest.' END)
) v (Reason)
WHERE v.Reason IS NOT NULL;

SELECT
    N'Session' AS ResultSetName,
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    SYSDATETIMEOFFSET() AS SessionTime,
    @CommitChanges AS CommitChanges;

SELECT N'BeforeHeader' AS ResultSetName, * FROM @BeforeHeader ORDER BY ApprovalRequestId;
SELECT N'BeforeApprover' AS ResultSetName, * FROM @BeforeApprover ORDER BY ApprovalRequestApproverId;
SELECT N'BeforeDetail' AS ResultSetName, * FROM @BeforeDetail ORDER BY ApprovalRequestId, ActivityId, DetailVersion, ApprovalRequestDetailId;
SELECT N'BeforeHistory' AS ResultSetName, * FROM @BeforeHistory ORDER BY HistoryId;
SELECT N'BeforeActivity' AS ResultSetName, * FROM @BeforeActivity ORDER BY ActivityDate, ActivityId;
SELECT N'BeforeExpense' AS ResultSetName, * FROM @BeforeExpense ORDER BY ActivityId, ExpenseId;

SELECT
    N'EligibilityFailures' AS ResultSetName,
    f.Item,
    f.Reason
FROM @Failure f
ORDER BY f.Item, f.Reason;

IF EXISTS (SELECT 1 FROM @Failure)
    THROW 53110, 'Eligibility failed. No transaction was started. See EligibilityFailures.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @LockedHeaders INT;
    DECLARE @LockedActivities INT;
    DECLARE @LockedDetails INT;
    DECLARE @HeaderUpdates INT;
    DECLARE @DetailUpdates INT;
    DECLARE @ActivityUpdates INT;
    DECLARE @ExpenseUpdates INT;
    DECLARE @ExpectedExpenseUpdates INT;

    SELECT @LockedHeaders = COUNT(*)
    FROM dbo.SalesApprovalRequest WITH (UPDLOCK, HOLDLOCK)
    WHERE RequesterLoginId = N'AHS1368'
      AND YearNo = 2026
      AND MonthNo = 9;

    SELECT @LockedDetails = COUNT(*)
    FROM dbo.SalesApprovalRequestDetail WITH (UPDLOCK, HOLDLOCK)
    WHERE ApprovalRequestId IN (1169, 1173);

    SELECT @LockedActivities = COUNT(*)
    FROM dbo.SalesDailyActivity WITH (UPDLOCK, HOLDLOCK)
    WHERE Login_Id = N'AHS1368'
      AND ActivityDate >= CONVERT(date, '20260901', 112)
      AND ActivityDate < CONVERT(date, '20261001', 112);

    SELECT COUNT(*)
    FROM dbo.SalesApprovalRequestApprover WITH (UPDLOCK, HOLDLOCK)
    WHERE ApprovalRequestId IN (1169, 1173);

    SELECT COUNT(*)
    FROM dbo.SalesDailyExpense WITH (UPDLOCK, HOLDLOCK)
    WHERE ActivityId IN (SELECT b.ActivityId FROM @BeforeActivity b);

    IF @LockedHeaders <> 2 OR @LockedDetails <> 32 OR @LockedActivities <> 30
        THROW 53111, 'Locked row counts drifted from the before-image.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest r
        INNER JOIN @BeforeHeader b ON b.ApprovalRequestId = r.ApprovalRequestId
        WHERE r.Status <> b.Status
           OR ISNULL(r.CurrentApproverLoginId, N'') <> ISNULL(b.CurrentApproverLoginId, N'')
           OR ISNULL(r.CurrentApproverLevel, -1) <> ISNULL(b.CurrentApproverLevel, -1)
           OR ISNULL(r.TotalKM, -1) <> ISNULL(b.TotalKM, -1)
           OR ISNULL(r.TotalClaimAmount, -1) <> ISNULL(b.TotalClaimAmount, -1)
           OR ISNULL(r.TotalDAAmount, -1) <> ISNULL(b.TotalDAAmount, -1)
           OR ISNULL(r.FinalApprovedAmount, -1) <> ISNULL(b.FinalApprovedAmount, -1)
    )
        THROW 53112, 'A locked header no longer matches the before-image.', 1;

    UPDATE dbo.SalesApprovalRequest
    SET Status = N'Rejected',
        ModifiedBy = N'Reconciliation',
        ModifiedOn = SYSUTCDATETIME()
    WHERE ApprovalRequestId = 1173
      AND Status = N'Pending'
      AND CurrentApproverLoginId = N'Audit'
      AND CurrentApproverLevel = 2
      AND RequesterLoginId = N'AHS1368'
      AND YearNo = 2026
      AND MonthNo = 9;

    SET @HeaderUpdates = @@ROWCOUNT;
    IF @HeaderUpdates <> 1
        THROW 53120, 'Request 1173 was not the expected Pending header.', 1;

    UPDATE d
    SET d.DetailStatus = N'Returned'
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN (
        SELECT MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
        FROM dbo.SalesApprovalRequestDetail d2
        INNER JOIN (
            SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
            FROM dbo.SalesApprovalRequestDetail
            WHERE ApprovalRequestId = 1169
            GROUP BY ActivityId
        ) lv
            ON lv.ActivityId = d2.ActivityId
           AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
        WHERE d2.ApprovalRequestId = 1169
        GROUP BY d2.ActivityId
    ) latest
        ON latest.LatestDetailId = d.ApprovalRequestDetailId
    WHERE d.ApprovalRequestId = 1169
      AND ISNULL(d.DetailStatus, N'') <> N'Rejected';

    SET @DetailUpdates = @@ROWCOUNT;
    IF @DetailUpdates <> 30
        THROW 53121, 'Latest-detail return count was not 30.', 1;

    UPDATE a
    SET a.ClaimStatus = N'Returned',
        a.ModifiedByUserId = N'Reconciliation',
        a.ModifiedDateTime = DATEADD(MINUTE, 330, SYSUTCDATETIME())
    FROM dbo.SalesDailyActivity a
    INNER JOIN @BeforeActivity b
        ON b.ActivityId = a.ActivityId
    WHERE a.ApprovalRequestId = 1169
      AND a.Login_Id = N'AHS1368'
      AND ISNULL(a.ClaimStatus, N'') <> N'Rejected';

    SET @ActivityUpdates = @@ROWCOUNT;
    IF @ActivityUpdates <> 30
        THROW 53122, 'Activity return count was not 30.', 1;

    SELECT @ExpectedExpenseUpdates = COUNT(*)
    FROM @BeforeExpense
    WHERE IsDeleted = 0
      AND ISNULL(ClaimStatus, N'') <> N'Rejected';

    UPDATE e
    SET e.ClaimStatus = N'Returned',
        e.ClaimProcessedBy = N'Reconciliation',
        e.ClaimProcessedDate = DATEADD(MINUTE, 330, SYSUTCDATETIME()),
        e.ModifiedBy = N'Reconciliation',
        e.ModifiedOn = SYSUTCDATETIME()
    FROM dbo.SalesDailyExpense e
    INNER JOIN @BeforeExpense b
        ON b.ExpenseId = e.ExpenseId
    WHERE b.IsDeleted = 0
      AND e.IsDeleted = 0
      AND ISNULL(e.ClaimStatus, N'') <> N'Rejected';

    SET @ExpenseUpdates = @@ROWCOUNT;
    IF @ExpenseUpdates <> @ExpectedExpenseUpdates
        THROW 53123, 'Active expense return count did not match the before-image predicate.', 1;

    IF @@TRANCOUNT <> 1
        THROW 53124, 'Transaction count changed before recalculation.', 1;

    EXEC dbo.usp_RecalculateApprovalRequest @ApprovalRequestId = 1169;

    IF @@TRANCOUNT <> 1 OR XACT_STATE() <> 1
        THROW 53125, 'usp_RecalculateApprovalRequest changed the outer transaction.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = 1173
          AND (
                Status <> N'Rejected'
             OR CurrentApproverLoginId <> N'Audit'
             OR CurrentApproverLevel <> 2
             OR ModifiedBy <> N'Reconciliation'
             OR TotalKM <> 1117.00
             OR TotalClaimAmount <> 4188.75
             OR TotalDAAmount <> 675.00
             OR ApprovedKM <> 1117.00
             OR ApprovedClaimAmount <> 4188.75
             OR ApprovedDAAmount <> 225.00
             OR FinalApprovedAmount <> 4413.75
          )
    )
        THROW 53130, 'Request 1173 postcondition failed.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = 1169
          AND (
                Status <> N'Returned'
             OR CurrentApproverLoginId <> N'AHS1181'
             OR CurrentApproverLevel <> 1
             OR ModifiedBy <> N'AHS1181'
             OR TotalKM <> 3191.41
             OR TotalClaimAmount <> 11967.80
             OR TotalDAAmount <> 6525.00
             OR ISNULL(TotalHotelAmount, 0) <> 0
             OR ISNULL(TotalTravelAmount, 0) <> 0
             OR TotalExpenseAmount <> 6525.00
             OR TotalStationaryAmount <> 0
             OR ISNULL(ApprovedKM, 0) <> 0
             OR ISNULL(ApprovedClaimAmount, 0) <> 0
             OR ISNULL(ApprovedDAAmount, 0) <> 0
             OR ISNULL(ApprovedHotelAmount, 0) <> 0
             OR ISNULL(ApprovedTravelAmount, 0) <> 0
             OR ISNULL(FinalApprovedAmount, 0) <> 0
             OR ISNULL(TotalApprovedAmount, 0) <> 0
             OR ISNULL(ApprovedExpenseAmount, 0) <> 0
          )
    )
        THROW 53131, 'Request 1169 header or approved-total postcondition failed.', 1;

    IF (
        SELECT COUNT(*)
        FROM dbo.SalesApprovalRequest
        WHERE RequesterLoginId = N'AHS1368'
          AND YearNo = 2026
          AND MonthNo = 9
          AND UPPER(LTRIM(RTRIM(Status))) IN (N'RETURNED', N'PENDING', N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED')
    ) <> 1
    OR NOT EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = 1169
          AND Status = N'Returned'
    )
        THROW 53132, 'Active September request count is not exactly request 1169.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest sar
        INNER JOIN dbo.SalesApprovalRequestApprover saa
            ON saa.ApprovalRequestId = sar.ApprovalRequestId
           AND saa.Status = N'Pending'
           AND saa.ApproverLoginId = sar.CurrentApproverLoginId
        WHERE sar.ApprovalRequestId IN (1169, 1173)
          AND sar.Status = N'Pending'
    )
        THROW 53133, 'A request still matches the pending approval queue predicate.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId IN (1169, 1173)
          AND Status IN (N'Pending', N'Completed')
    )
        THROW 53134, 'A request still has a status that payment finalization accepts.', 1;

    IF (
        SELECT COUNT(*)
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN (
            SELECT MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
            FROM dbo.SalesApprovalRequestDetail d2
            INNER JOIN (
                SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
                FROM dbo.SalesApprovalRequestDetail
                WHERE ApprovalRequestId = 1169
                GROUP BY ActivityId
            ) lv
                ON lv.ActivityId = d2.ActivityId
               AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
            WHERE d2.ApprovalRequestId = 1169
            GROUP BY d2.ActivityId
        ) x ON x.LatestDetailId = d.ApprovalRequestDetailId
        WHERE d.ApprovalRequestId = 1169
          AND ISNULL(d.DetailStatus, N'') = N'Returned'
          AND (
                d.ActivityId <> 96111
             OR (
                    d.ApprovalRequestDetailId = 46300
                AND d.DetailVersion = 2
                AND d.PreviousDetailId = 35468
                )
          )
    ) <> 30
        THROW 53135, 'Latest details on 1169 are not all Returned with lineage 35468 to 46300.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN @BeforeDetail b ON b.ApprovalRequestDetailId = d.ApprovalRequestDetailId
        WHERE d.ApprovalRequestDetailId = 35468
          AND (
                d.ApprovalRequestId <> b.ApprovalRequestId
             OR d.ActivityId <> b.ActivityId
             OR d.DetailVersion <> b.DetailVersion
             OR ISNULL(d.PreviousDetailId, -1) <> ISNULL(b.PreviousDetailId, -1)
             OR ISNULL(d.DetailStatus, N'') <> ISNULL(b.DetailStatus, N'')
             OR ISNULL(d.Snapshot_DAAmount, -1) <> ISNULL(b.Snapshot_DAAmount, -1)
             OR ISNULL(d.Snapshot_CalculatedClaimAmount, -1) <> ISNULL(b.Snapshot_CalculatedClaimAmount, -1)
             OR ISNULL(d.Snapshot_TotalDistanceKM, -1) <> ISNULL(b.Snapshot_TotalDistanceKM, -1)
          )
    )
        THROW 53136, 'Historical detail 35468 changed.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN @BeforeDetail b ON b.ApprovalRequestDetailId = d.ApprovalRequestDetailId
        WHERE d.ApprovalRequestDetailId = 46300
          AND (
                d.ApprovalRequestId <> 1169
             OR d.ActivityId <> 96111
             OR d.DetailVersion <> 2
             OR d.PreviousDetailId <> 35468
             OR ISNULL(d.DetailStatus, N'') <> N'Returned'
             OR ISNULL(d.DetailApprovedBy, N'') <> ISNULL(b.DetailApprovedBy, N'')
             OR ISNULL(d.DetailApprovedLevel, -1) <> ISNULL(b.DetailApprovedLevel, -1)
             OR ISNULL(d.DetailApprovedOn, CONVERT(datetime2, '19000101', 112)) <> ISNULL(b.DetailApprovedOn, CONVERT(datetime2, '19000101', 112))
          )
    )
        THROW 53137, 'Detail 46300 lineage or approval metadata changed beyond the Returned status.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN @BeforeDetail b ON b.ApprovalRequestDetailId = d.ApprovalRequestDetailId
        WHERE d.ApprovalRequestDetailId = 35580
          AND (
                d.ApprovalRequestId <> b.ApprovalRequestId
             OR d.ActivityId <> b.ActivityId
             OR d.DetailVersion <> b.DetailVersion
             OR ISNULL(d.PreviousDetailId, -1) <> ISNULL(b.PreviousDetailId, -1)
             OR ISNULL(d.DetailStatus, N'') <> ISNULL(b.DetailStatus, N'')
             OR ISNULL(d.DetailApprovedBy, N'') <> ISNULL(b.DetailApprovedBy, N'')
             OR ISNULL(d.Snapshot_DAAmount, -1) <> ISNULL(b.Snapshot_DAAmount, -1)
             OR ISNULL(d.Snapshot_CalculatedClaimAmount, -1) <> ISNULL(b.Snapshot_CalculatedClaimAmount, -1)
          )
    )
        THROW 53138, 'Detail 35580 did not stay independent on request 1173.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestApprover a
        INNER JOIN @BeforeApprover b ON b.ApprovalRequestApproverId = a.ApprovalRequestApproverId
        WHERE ISNULL(a.ApproverLoginId, N'') <> ISNULL(b.ApproverLoginId, N'')
           OR ISNULL(a.ApproverRole, N'') <> ISNULL(b.ApproverRole, N'')
           OR ISNULL(a.ApproverLevel, -1) <> ISNULL(b.ApproverLevel, -1)
           OR a.Status <> b.Status
           OR ISNULL(a.ActionBy, N'') <> ISNULL(b.ActionBy, N'')
           OR ISNULL(a.ActionDate, CONVERT(datetime2, '19000101', 112)) <> ISNULL(b.ActionDate, CONVERT(datetime2, '19000101', 112))
           OR ISNULL(a.Remarks, N'') <> ISNULL(b.Remarks, N'')
    )
        THROW 53139, 'An approver row changed.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesDailyActivity a
        INNER JOIN @BeforeActivity b ON b.ActivityId = a.ActivityId
        WHERE a.ApprovalRequestId <> 1169
           OR a.Login_Id <> N'AHS1368'
           OR ISNULL(a.ClaimStatus, N'') <> N'Returned'
           OR ISNULL(a.TotalDistanceKM, -1) <> ISNULL(b.TotalDistanceKM, -1)
           OR ISNULL(a.CalculatedClaimAmount, -1) <> ISNULL(b.CalculatedClaimAmount, -1)
           OR ISNULL(a.ClaimedDA, -1) <> ISNULL(b.ClaimedDA, -1)
           OR ISNULL(a.ApprovedDA, -1) <> ISNULL(b.ApprovedDA, -1)
    )
        THROW 53141, 'An activity owner or claimed amount changed.', 1;

    IF (
        SELECT COUNT(*)
        FROM dbo.SalesDailyExpense e
        WHERE e.ActivityId IN (SELECT b.ActivityId FROM @BeforeActivity b)
    ) <> (SELECT COUNT(*) FROM @BeforeExpense)
        THROW 53144, 'Expense row count for the September activities changed.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesDailyExpense e
        INNER JOIN @BeforeExpense b ON b.ExpenseId = e.ExpenseId
        WHERE e.Amount <> b.Amount
           OR e.IsDeleted <> b.IsDeleted
           OR e.ActivityId <> b.ActivityId
           OR e.ExpenseType <> b.ExpenseType
           OR (
                (b.IsDeleted = 1 OR ISNULL(b.ClaimStatus, N'') = N'Rejected')
                AND ISNULL(e.ClaimStatus, N'') <> ISNULL(b.ClaimStatus, N'')
              )
           OR (
                b.IsDeleted = 0
                AND ISNULL(b.ClaimStatus, N'') <> N'Rejected'
                AND ISNULL(e.ClaimStatus, N'') <> N'Returned'
              )
    )
        THROW 53142, 'An expense amount, deletion flag, or status transition is wrong.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.ApprovalRequestStatusHistory h
        FULL OUTER JOIN @BeforeHistory b ON b.HistoryId = h.HistoryId
        WHERE (h.ApprovalRequestId IN (1169, 1173) OR h.HistoryId IN (4560, 4563, 4565, 4569) OR b.HistoryId IS NOT NULL)
          AND (
                h.HistoryId IS NULL
             OR b.HistoryId IS NULL
             OR h.ApprovalRequestId <> b.ApprovalRequestId
             OR ISNULL(h.OldStatus, N'') <> ISNULL(b.OldStatus, N'')
             OR h.NewStatus <> b.NewStatus
             OR ISNULL(h.ChangedBy, N'') <> ISNULL(b.ChangedBy, N'')
             OR h.ChangedOn <> b.ChangedOn
             OR ISNULL(h.Remarks, N'') <> ISNULL(b.Remarks, N'')
          )
    )
        THROW 53143, 'Approval history changed.', 1;

    SELECT
        N'InsideTransaction' AS ResultSetName,
        r.ApprovalRequestId,
        r.Status,
        r.CurrentApproverLoginId,
        r.CurrentApproverLevel,
        r.TotalKM,
        r.TotalClaimAmount,
        r.TotalDAAmount,
        r.ApprovedKM,
        r.ApprovedClaimAmount,
        r.ApprovedDAAmount,
        r.FinalApprovedAmount,
        r.TotalApprovedAmount,
        r.ModifiedBy
    FROM dbo.SalesApprovalRequest r
    WHERE r.ApprovalRequestId IN (1169, 1173)
    ORDER BY r.ApprovalRequestId;

    IF @CommitChanges = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'COMMITTED. Request 1173 is Rejected and request 1169 latest lines are Returned.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'ROLLED BACK (preview). Set @CommitChanges = 1 only after reviewing these result sets and receiving execution approval.';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    N'TransactionOutcome' AS ResultSetName,
    CASE WHEN @CommitChanges = 1 THEN N'COMMITTED' ELSE N'ROLLED BACK (preview)' END AS TransactionOutcome,
    @CommitChanges AS CommitChanges,
    @@TRANCOUNT AS OpenTransactions;

SELECT
    N'PostTransactionState' AS ResultSetName,
    r.ApprovalRequestId,
    r.Status,
    r.CurrentApproverLoginId,
    r.CurrentApproverLevel,
    r.TotalKM,
    r.TotalClaimAmount,
    r.TotalDAAmount,
    r.ApprovedKM,
    r.ApprovedClaimAmount,
    r.ApprovedDAAmount,
    r.FinalApprovedAmount,
    r.ModifiedBy
FROM dbo.SalesApprovalRequest r
WHERE r.ApprovalRequestId IN (1169, 1173)
ORDER BY r.ApprovalRequestId;
