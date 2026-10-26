/*
    AHS1180 and M2659 September 2026 competing-request reconciliation
    Server:   MUMANMOAPP01\SQLEXPRESS
    Database: AnmolDristi
    Build observed at preparation: SQL Server 2019 Express 15.0.2000.5

    PREPARED SCRIPT. @CommitChanges stays 0.
    Do not set it to 1 until a commit-0 preview has been reviewed and a
    separate execution approval is given. This file does not itself prove
    that a commit ran.

    SSMS:
      1. Connect to MUMANMOAPP01\SQLEXPRESS.
      2. Select database AnmolDristi. The script also checks @@SERVERNAME,
         DB_NAME(), product version, and edition.
      3. Leave @CommitChanges = 0 and execute the whole script once.
      4. Review every result set. InsideTransaction is the proposed state.
         PostTransactionState must still show 1495 and 1490 Pending, because
         the preview rolls back.
      5. Only after that review, and a separate execution approval, set
         @CommitChanges = 1 and execute once.

    Scope is requests 1424, 1495, 1483, and 1490 only.
    1424 remains the canonical September claim for AHS1180.
    1483 remains the canonical September claim for M2659.
    1495 and 1490 are closed as Rejected. Their approver pointers, amounts,
    detail rows, and history are not rewritten.

    The script does not call dbo.usp_ReturnClaimToEmployee. That procedure
    requires a Pending header and an approver role of exactly L1. These
    canonical headers are Returned, and their level-1 role is
    Reporting_Manager1. The script also does not call
    dbo.usp_SubmitSalesDailyActivities. That procedure has its own
    transaction and is the later employee resubmission path, not this repair.

    It calls only dbo.usp_RecalculateApprovalRequest, and only for 1424 and
    1483, after the status changes. The live definition inspected on
    2026-10-14 has no BEGIN TRAN and no COMMIT, does not assign TotalKM or
    TotalClaimAmount, and writes Approved* plus FinalApprovedAmount from
    latest details whose status is NULL or otherwise not Rejected or
    Returned. It also refreshes expense snapshots and ModifiedOn on every
    latest detail of the request it is given. The preflight below refuses
    to start if those snapshots already differ from the qualifying
    expenses, so the refresh does not move amounts.

    The stored approved headers already exclude latest details 46299 and
    46301. On each canonical request the other 29 latest rows have NULL
    detail status, so the procedure already includes them. Returning 46299
    and 46301 therefore leaves the approved headers at their current stored
    values. Claimed totals are not rewritten.
      1424: ApprovedKM 1441.31, ApprovedClaimAmount 5404.92,
            ApprovedDAAmount 5475.00, ApprovedHotelAmount 0.00,
            ApprovedTravelAmount 5395.00, FinalApprovedAmount 16274.92.
      1483: ApprovedKM 2842.52, ApprovedClaimAmount 10659.46,
            ApprovedDAAmount 5300.00, ApprovedHotelAmount 600.00,
            ApprovedTravelAmount 350.00, FinalApprovedAmount 16909.46.

    Active expense predicate, taken from the live return procedure and
    matched by the live recalculation procedure:
      IsDeleted = 0 AND ClaimStatus is not Rejected.
    Expense 41185 is soft-deleted and Draft. It is not updated.
    Expenses 39392, 39395, and 41487 are active and become Returned.
    Amounts are not assigned.

    This script does not insert approval history and does not change
    approver roles or approver rows. L1 rows 2819 and 2937 stay Returned.
    The employee corrects and resubmits later through the application.

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
    TotalKM DECIMAL(12,2) NULL,
    TotalClaimAmount DECIMAL(18,2) NULL,
    TotalDAAmount DECIMAL(18,2) NULL,
    TotalHotelAmount DECIMAL(18,2) NULL,
    TotalTravelAmount DECIMAL(18,2) NULL,
    TotalExpenseAmount DECIMAL(18,2) NULL,
    TotalStationaryAmount DECIMAL(18,2) NULL,
    ApprovedKM DECIMAL(12,2) NULL,
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
    Snapshot_ExpenseTotal DECIMAL(18,2) NULL,
    ModifiedOn DATETIME2 NULL
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
    ActivityId INT NULL,
    ApproverLevel INT NULL,
    ActionType NVARCHAR(30) NULL
);

DECLARE @BeforeActivity TABLE
(
    ActivityId INT NOT NULL PRIMARY KEY,
    Login_Id NVARCHAR(100) NULL,
    ActivityDate DATE NULL,
    ApprovalRequestId INT NULL,
    ClaimStatus NVARCHAR(50) NULL,
    TotalDistanceKM DECIMAL(10,2) NULL,
    CalculatedClaimAmount DECIMAL(18,2) NULL,
    IsDeleted BIT NOT NULL
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

/* Guard: session identity. */
IF DB_NAME() <> N'AnmolDristi'
   OR @@SERVERNAME <> N'MUMANMOAPP01\SQLEXPRESS'
   OR CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')) <> N'15.0.2000.5'
   OR CONVERT(nvarchar(128), SERVERPROPERTY('Edition')) NOT LIKE N'%Express%'
    INSERT INTO @Failure (Item, Reason)
    VALUES (NULL, N'Connected session is not MUMANMOAPP01\SQLEXPRESS / AnmolDristi / SQL Server 2019 Express 15.0.2000.5.');

/* Guard: recalculation is safe to call inside this transaction and does not write claimed totals. */
DECLARE @RecalcDefinition NVARCHAR(MAX) =
    OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest'));
DECLARE @SubmitDefinition NVARCHAR(MAX) =
    OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_SubmitSalesDailyActivities'));
DECLARE @ReturnDefinition NVARCHAR(MAX) =
    OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_ReturnClaimToEmployee'));

IF @RecalcDefinition IS NULL
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_RecalculateApprovalRequest', N'Procedure definition was not found.');
IF @RecalcDefinition IS NOT NULL AND CHARINDEX(N'BEGIN TRAN', @RecalcDefinition) > 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_RecalculateApprovalRequest', N'Procedure contains BEGIN TRAN and can defeat the outer rollback.');
IF @RecalcDefinition IS NOT NULL AND CHARINDEX(N'COMMIT', @RecalcDefinition) > 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_RecalculateApprovalRequest', N'Procedure contains COMMIT and can defeat the outer rollback.');
IF @RecalcDefinition IS NOT NULL AND CHARINDEX(N'TotalKM', @RecalcDefinition) > 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_RecalculateApprovalRequest', N'Procedure now references claimed TotalKM.');
IF @RecalcDefinition IS NOT NULL AND CHARINDEX(N'TotalClaimAmount', @RecalcDefinition) > 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_RecalculateApprovalRequest', N'Procedure now references claimed TotalClaimAmount.');
IF @RecalcDefinition IS NOT NULL
   AND CHARINDEX(N'NOT IN (''Rejected'', ''Returned'')', @RecalcDefinition) = 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_RecalculateApprovalRequest', N'Returned-row exclusion was not found in the live definition.');
IF @RecalcDefinition IS NOT NULL
   AND CHARINDEX(N'IsDeleted = 0', @RecalcDefinition) = 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_RecalculateApprovalRequest', N'Active-expense predicate was not found in the live definition.');

/* Guard: submission procedure still keeps a Returned request and is not invoked here. */
IF @SubmitDefinition IS NULL
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_SubmitSalesDailyActivities', N'Procedure definition was not found.');
IF @SubmitDefinition IS NOT NULL
   AND CHARINDEX(N'Returned and Pending keep this request.', @SubmitDefinition) = 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_SubmitSalesDailyActivities', N'Live definition no longer keeps a Returned request for resubmission.');
IF @SubmitDefinition IS NOT NULL
   AND CHARINDEX(N'Resubmission can contain only Returned activities.', @SubmitDefinition) = 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_SubmitSalesDailyActivities', N'Live definition no longer limits resubmission to Returned activities.');
IF @SubmitDefinition IS NOT NULL
   AND CHARINDEX(N'Multiple active approval requests exist for this employee and month.', @SubmitDefinition) = 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_SubmitSalesDailyActivities', N'Live definition no longer rejects a second active header.');
IF @SubmitDefinition IS NOT NULL AND CHARINDEX(N'BEGIN TRAN', @SubmitDefinition) = 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_SubmitSalesDailyActivities', N'Live definition no longer owns a transaction; do not guess a new call pattern.');

/* Guard: return procedure still does not match these headers, so it stays uncalled. */
IF @ReturnDefinition IS NULL
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_ReturnClaimToEmployee', N'Procedure definition was not found.');
IF @ReturnDefinition IS NOT NULL
   AND CHARINDEX(N'UPPER(LTRIM(RTRIM(a.ApproverRole))) = N''L1''', @ReturnDefinition) = 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_ReturnClaimToEmployee', N'Live definition no longer requires role L1. Stop and review before any return call.');
IF @ReturnDefinition IS NOT NULL
   AND CHARINDEX(N'ISNULL(@OldRequestStatus, N'''') <> N''Pending''', @ReturnDefinition) = 0
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'usp_ReturnClaimToEmployee', N'Live definition no longer requires a Pending header. Stop and review before any return call.');

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
WHERE r.ApprovalRequestId IN (1424, 1495, 1483, 1490);

INSERT INTO @BeforeApprover
(
    ApprovalRequestApproverId, ApprovalRequestId, ApproverLoginId, ApproverRole,
    ApproverLevel, Status, ActionBy, ActionDate, Remarks
)
SELECT
    a.ApprovalRequestApproverId, a.ApprovalRequestId, a.ApproverLoginId, a.ApproverRole,
    a.ApproverLevel, a.Status, a.ActionBy, a.ActionDate, a.Remarks
FROM dbo.SalesApprovalRequestApprover a
WHERE a.ApprovalRequestId IN (1424, 1495, 1483, 1490);

INSERT INTO @BeforeDetail
(
    ApprovalRequestDetailId, ApprovalRequestId, ActivityId, DetailVersion, PreviousDetailId,
    DetailStatus, DetailApprovedBy, DetailApprovedLevel, DetailApprovedOn,
    Snapshot_TotalDistanceKM, Snapshot_CalculatedClaimAmount, Snapshot_DAAmount,
    Snapshot_HotelAmount, Snapshot_TravelAmount, Snapshot_StationaryAmount, Snapshot_ExpenseTotal,
    ModifiedOn
)
SELECT
    d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId, d.DetailVersion, d.PreviousDetailId,
    d.DetailStatus, d.DetailApprovedBy, d.DetailApprovedLevel, d.DetailApprovedOn,
    d.Snapshot_TotalDistanceKM, d.Snapshot_CalculatedClaimAmount, d.Snapshot_DAAmount,
    d.Snapshot_HotelAmount, d.Snapshot_TravelAmount, d.Snapshot_StationaryAmount, d.Snapshot_ExpenseTotal,
    d.ModifiedOn
FROM dbo.SalesApprovalRequestDetail d
WHERE d.ApprovalRequestId IN (1424, 1495, 1483, 1490);

INSERT INTO @BeforeHistory
(
    HistoryId, ApprovalRequestId, OldStatus, NewStatus, ChangedBy, ChangedOn, Remarks,
    ApprovalRequestDetailId, ActivityId, ApproverLevel, ActionType
)
SELECT
    h.HistoryId, h.ApprovalRequestId, h.OldStatus, h.NewStatus, h.ChangedBy, h.ChangedOn, h.Remarks,
    h.ApprovalRequestDetailId, h.ActivityId, h.ApproverLevel, h.ActionType
FROM dbo.ApprovalRequestStatusHistory h
WHERE h.ApprovalRequestId IN (1424, 1495, 1483, 1490)
   OR h.ApprovalRequestDetailId IN (43086, 46299, 45183, 44852, 46301, 45062)
   OR h.ActivityId IN (91457, 104263)
   OR h.Remarks LIKE N'%Detail 43086%'
   OR h.Remarks LIKE N'%Detail 46299%'
   OR h.Remarks LIKE N'%Detail 45183%'
   OR h.Remarks LIKE N'%Detail 44852%'
   OR h.Remarks LIKE N'%Detail 46301%'
   OR h.Remarks LIKE N'%Detail 45062%';

INSERT INTO @BeforeActivity
(
    ActivityId, Login_Id, ActivityDate, ApprovalRequestId, ClaimStatus,
    TotalDistanceKM, CalculatedClaimAmount, IsDeleted
)
SELECT
    a.ActivityId, a.Login_Id, a.ActivityDate, a.ApprovalRequestId, a.ClaimStatus,
    a.TotalDistanceKM, a.CalculatedClaimAmount, a.IsDeleted
FROM dbo.SalesDailyActivity a
WHERE a.Login_Id IN (N'AHS1180', N'M2659')
  AND a.ActivityDate >= CONVERT(date, '20260901', 112)
  AND a.ActivityDate < CONVERT(date, '20261001', 112);

INSERT INTO @BeforeExpense (ExpenseId, ActivityId, ExpenseType, Amount, ClaimStatus, IsDeleted)
SELECT e.ExpenseId, e.ActivityId, e.ExpenseType, e.Amount, e.ClaimStatus, e.IsDeleted
FROM dbo.SalesDailyExpense e
WHERE e.ActivityId IN (91457, 104263);

/* Guard: approved header evidence. */
INSERT INTO @Failure (Item, Reason)
SELECT v.Item, v.Reason
FROM (VALUES
    (N'1424', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHeader
        WHERE ApprovalRequestId = 1424 AND RequesterLoginId = N'AHS1180' AND YearNo = 2026 AND MonthNo = 9
          AND Status = N'Returned' AND CurrentApproverLoginId = N'AHS1794' AND CurrentApproverLevel = 1
          AND TotalKM = 1514.64 AND TotalClaimAmount = 5679.91 AND TotalDAAmount = 5925.00
          AND ISNULL(TotalHotelAmount, 0) = 0 AND TotalTravelAmount = 6617.00
          AND TotalExpenseAmount = 12542.00 AND TotalStationaryAmount = 0
          AND ApprovedKM = 1441.31 AND ApprovedClaimAmount = 5404.92 AND ApprovedDAAmount = 5475.00
          AND ISNULL(ApprovedHotelAmount, 0) = 0 AND ApprovedTravelAmount = 5395.00
          AND FinalApprovedAmount = 16274.92 AND ModifiedBy = N'AHS1794'
     ) THEN N'Request 1424 is not the expected Returned canonical header.' END),
    (N'1495', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHeader
        WHERE ApprovalRequestId = 1495 AND RequesterLoginId = N'AHS1180' AND YearNo = 2026 AND MonthNo = 9
          AND Status = N'Pending' AND CurrentApproverLoginId = N'Audit' AND CurrentApproverLevel = 2
          AND TotalKM = 73.33 AND TotalClaimAmount = 274.99 AND TotalDAAmount = 225.00
          AND ISNULL(TotalHotelAmount, 0) = 0 AND TotalTravelAmount = 547.00
          AND TotalExpenseAmount = 772.00 AND TotalStationaryAmount = 0
          AND ApprovedKM = 73.33 AND ApprovedClaimAmount = 274.99 AND ApprovedDAAmount = 225.00
          AND ISNULL(ApprovedHotelAmount, 0) = 0 AND ApprovedTravelAmount = 547.00
          AND FinalApprovedAmount = 1046.99 AND ModifiedBy = N'AHS1794'
     ) THEN N'Request 1495 is not the expected Pending duplicate header.' END),
    (N'1483', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHeader
        WHERE ApprovalRequestId = 1483 AND RequesterLoginId = N'M2659' AND YearNo = 2026 AND MonthNo = 9
          AND Status = N'Returned' AND CurrentApproverLoginId = N'M2277' AND CurrentApproverLevel = 1
          AND TotalKM = 4292.52 AND TotalClaimAmount = 16096.96 AND TotalDAAmount = 5525.00
          AND TotalHotelAmount = 600.00 AND TotalTravelAmount = 350.00
          AND TotalExpenseAmount = 6250.00 AND TotalStationaryAmount = 0
          AND ApprovedKM = 2842.52 AND ApprovedClaimAmount = 10659.46 AND ApprovedDAAmount = 5300.00
          AND ApprovedHotelAmount = 600.00 AND ApprovedTravelAmount = 350.00
          AND FinalApprovedAmount = 16909.46 AND ModifiedBy = N'M2277'
     ) THEN N'Request 1483 is not the expected Returned canonical header.' END),
    (N'1490', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHeader
        WHERE ApprovalRequestId = 1490 AND RequesterLoginId = N'M2659' AND YearNo = 2026 AND MonthNo = 9
          AND Status = N'Pending' AND CurrentApproverLoginId = N'Audit' AND CurrentApproverLevel = 2
          AND TotalKM = 1450.00 AND TotalClaimAmount = 5437.50 AND TotalDAAmount = 450.00
          AND ISNULL(TotalHotelAmount, 0) = 0 AND ISNULL(TotalTravelAmount, 0) = 0
          AND TotalExpenseAmount = 450.00 AND TotalStationaryAmount = 0
          AND ApprovedKM = 140.00 AND ApprovedClaimAmount = 525.00 AND ApprovedDAAmount = 225.00
          AND ISNULL(ApprovedHotelAmount, 0) = 0 AND ISNULL(ApprovedTravelAmount, 0) = 0
          AND FinalApprovedAmount = 750.00 AND ModifiedBy = N'M2277'
     ) THEN N'Request 1490 is not the expected Pending duplicate header.' END)
) v (Item, Reason)
WHERE v.Reason IS NOT NULL;

/* Guard: level-1 rows stay recognizable, and every Auditor row is still unactioned. */
INSERT INTO @Failure (Item, Reason)
SELECT v.Item, v.Reason
FROM (VALUES
    (N'2819', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeApprover
        WHERE ApprovalRequestApproverId = 2819 AND ApprovalRequestId = 1424
          AND ApproverLoginId = N'AHS1794' AND ApproverRole = N'Reporting_Manager1' AND ApproverLevel = 1
          AND Status = N'Returned' AND ActionBy = N'AHS1794'
     ) THEN N'Approver 2819 is not the Returned level-1 row on 1424.' END),
    (N'2820', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeApprover
        WHERE ApprovalRequestApproverId = 2820 AND ApprovalRequestId = 1424
          AND ApproverLoginId = N'Audit' AND ApproverRole = N'Auditor' AND ApproverLevel = 2
          AND Status = N'Pending' AND ActionBy IS NULL AND ActionDate IS NULL
          AND NULLIF(LTRIM(RTRIM(Remarks)), N'') IS NULL
     ) THEN N'Approver 2820 is not an unactioned Auditor row.' END),
    (N'2961', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeApprover
        WHERE ApprovalRequestApproverId = 2961 AND ApprovalRequestId = 1495
          AND ApproverLoginId = N'AHS1794' AND ApproverRole = N'Reporting_Manager1' AND ApproverLevel = 1
          AND Status = N'Approved' AND ActionBy = N'AHS1794'
     ) THEN N'Approver 2961 is not the level-1 approval on 1495.' END),
    (N'2962', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeApprover
        WHERE ApprovalRequestApproverId = 2962 AND ApprovalRequestId = 1495
          AND ApproverLoginId = N'Audit' AND ApproverRole = N'Auditor' AND ApproverLevel = 2
          AND Status = N'Pending' AND ActionBy IS NULL AND ActionDate IS NULL
          AND NULLIF(LTRIM(RTRIM(Remarks)), N'') IS NULL
     ) THEN N'Approver 2962 is not an unactioned Auditor row.' END),
    (N'2937', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeApprover
        WHERE ApprovalRequestApproverId = 2937 AND ApprovalRequestId = 1483
          AND ApproverLoginId = N'M2277' AND ApproverRole = N'Reporting_Manager1' AND ApproverLevel = 1
          AND Status = N'Returned' AND ActionBy = N'M2277'
     ) THEN N'Approver 2937 is not the Returned level-1 row on 1483.' END),
    (N'2938', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeApprover
        WHERE ApprovalRequestApproverId = 2938 AND ApprovalRequestId = 1483
          AND ApproverLoginId = N'Audit' AND ApproverRole = N'Auditor' AND ApproverLevel = 2
          AND Status = N'Pending' AND ActionBy IS NULL AND ActionDate IS NULL
          AND NULLIF(LTRIM(RTRIM(Remarks)), N'') IS NULL
     ) THEN N'Approver 2938 is not an unactioned Auditor row.' END),
    (N'2951', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeApprover
        WHERE ApprovalRequestApproverId = 2951 AND ApprovalRequestId = 1490
          AND ApproverLoginId = N'M2277' AND ApproverRole = N'Reporting_Manager1' AND ApproverLevel = 1
          AND Status = N'Approved' AND ActionBy = N'M2277'
     ) THEN N'Approver 2951 is not the level-1 approval on 1490.' END),
    (N'2952', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeApprover
        WHERE ApprovalRequestApproverId = 2952 AND ApprovalRequestId = 1490
          AND ApproverLoginId = N'Audit' AND ApproverRole = N'Auditor' AND ApproverLevel = 2
          AND Status = N'Pending' AND ActionBy IS NULL AND ActionDate IS NULL
          AND NULLIF(LTRIM(RTRIM(Remarks)), N'') IS NULL
     ) THEN N'Approver 2952 is not an unactioned Auditor row.' END)
) v (Item, Reason)
WHERE v.Reason IS NOT NULL;

IF EXISTS (
    SELECT 1
    FROM @BeforeApprover a
    WHERE (
            UPPER(LTRIM(RTRIM(ISNULL(a.ApproverRole, N'')))) = N'AUDITOR'
         OR UPPER(LTRIM(RTRIM(ISNULL(a.ApproverLoginId, N'')))) IN (N'AUDIT', N'AUDITONE')
          )
      AND (
            a.Status <> N'Pending'
         OR a.ActionBy IS NOT NULL
         OR a.ActionDate IS NOT NULL
         OR NULLIF(LTRIM(RTRIM(a.Remarks)), N'') IS NOT NULL
          )
)
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'Auditor', N'An Auditor approver row has an action. Leave both requests unchanged.');

/* Guard: preserved and current detail lineage. */
INSERT INTO @Failure (Item, Reason)
SELECT v.Item, v.Reason
FROM (VALUES
    (N'43086', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeDetail
        WHERE ApprovalRequestDetailId = 43086 AND ApprovalRequestId = 1424 AND ActivityId = 91457
          AND DetailVersion = 1 AND PreviousDetailId IS NULL AND DetailStatus = N'Returned'
          AND DetailApprovedBy = N'AHS1794' AND DetailApprovedLevel = 1
          AND Snapshot_TotalDistanceKM = 73.33 AND Snapshot_CalculatedClaimAmount = 274.99
          AND Snapshot_DAAmount = 225.00 AND ISNULL(Snapshot_HotelAmount, 0) = 0
          AND Snapshot_TravelAmount = 547.00 AND ISNULL(Snapshot_StationaryAmount, 0) = 0
          AND Snapshot_ExpenseTotal = 772.00
     ) THEN N'Detail 43086 is not the preserved version 1 Returned row on 1424.' END),
    (N'46299', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeDetail
        WHERE ApprovalRequestDetailId = 46299 AND ApprovalRequestId = 1424 AND ActivityId = 91457
          AND DetailVersion = 2 AND PreviousDetailId = 43086 AND DetailStatus = N'Approved'
          AND DetailApprovedBy = N'AHS1794' AND DetailApprovedLevel = 1
          AND Snapshot_TotalDistanceKM = 73.33 AND Snapshot_CalculatedClaimAmount = 274.99
          AND Snapshot_DAAmount = 225.00 AND ISNULL(Snapshot_HotelAmount, 0) = 0
          AND Snapshot_TravelAmount = 547.00 AND ISNULL(Snapshot_StationaryAmount, 0) = 0
          AND Snapshot_ExpenseTotal = 772.00
     ) THEN N'Detail 46299 is not the latest Approved row for activity 91457.' END),
    (N'45183', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeDetail
        WHERE ApprovalRequestDetailId = 45183 AND ApprovalRequestId = 1495 AND ActivityId = 91457
          AND DetailVersion = 1 AND PreviousDetailId IS NULL AND DetailStatus = N'Approved'
          AND DetailApprovedBy = N'AHS1794' AND DetailApprovedLevel = 1
          AND Snapshot_TotalDistanceKM = 73.33 AND Snapshot_CalculatedClaimAmount = 274.99
          AND Snapshot_DAAmount = 225.00 AND Snapshot_TravelAmount = 547.00
          AND Snapshot_ExpenseTotal = 772.00
     ) THEN N'Detail 45183 is not the independent version 1 row on 1495.' END),
    (N'44852', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeDetail
        WHERE ApprovalRequestDetailId = 44852 AND ApprovalRequestId = 1483 AND ActivityId = 104263
          AND DetailVersion = 1 AND PreviousDetailId IS NULL AND DetailStatus = N'Returned'
          AND DetailApprovedBy = N'M2277' AND DetailApprovedLevel = 1
          AND Snapshot_TotalDistanceKM = 140.00 AND Snapshot_CalculatedClaimAmount = 525.00
          AND ISNULL(Snapshot_DAAmount, 0) = 0 AND ISNULL(Snapshot_HotelAmount, 0) = 0
          AND ISNULL(Snapshot_TravelAmount, 0) = 0 AND ISNULL(Snapshot_ExpenseTotal, 0) = 0
     ) THEN N'Detail 44852 is not the preserved version 1 Returned row on 1483.' END),
    (N'46301', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeDetail
        WHERE ApprovalRequestDetailId = 46301 AND ApprovalRequestId = 1483 AND ActivityId = 104263
          AND DetailVersion = 2 AND PreviousDetailId = 44852 AND DetailStatus = N'Approved'
          AND DetailApprovedBy = N'M2277' AND DetailApprovedLevel = 1
          AND Snapshot_TotalDistanceKM = 140.00 AND Snapshot_CalculatedClaimAmount = 525.00
          AND Snapshot_DAAmount = 225.00 AND ISNULL(Snapshot_HotelAmount, 0) = 0
          AND ISNULL(Snapshot_TravelAmount, 0) = 0 AND ISNULL(Snapshot_StationaryAmount, 0) = 0
          AND Snapshot_ExpenseTotal = 225.00
     ) THEN N'Detail 46301 is not the latest Approved row for activity 104263.' END),
    (N'45062', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeDetail
        WHERE ApprovalRequestDetailId = 45062 AND ApprovalRequestId = 1490 AND ActivityId = 104263
          AND DetailVersion = 1 AND PreviousDetailId IS NULL AND DetailStatus = N'Approved'
          AND DetailApprovedBy = N'M2277' AND DetailApprovedLevel = 1
          AND Snapshot_TotalDistanceKM = 140.00 AND Snapshot_CalculatedClaimAmount = 525.00
          AND Snapshot_DAAmount = 225.00 AND Snapshot_ExpenseTotal = 225.00
     ) THEN N'Detail 45062 is not the independent version 1 row on 1490.' END)
) v (Item, Reason)
WHERE v.Reason IS NOT NULL;

IF EXISTS (
    SELECT 1
    FROM @BeforeDetail d
    WHERE ISNULL(d.DetailApprovedLevel, 0) >= 2
       OR UPPER(LTRIM(RTRIM(ISNULL(d.DetailApprovedBy, N'')))) IN (N'AUDIT', N'AUDITONE')
)
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'Auditor', N'A detail was approved at level 2 or by Audit. Leave both requests unchanged.');

;WITH Latest AS
(
    SELECT d.ApprovalRequestDetailId, d.ApprovalRequestId, d.ActivityId
    FROM @BeforeDetail d
    INNER JOIN (
        SELECT d2.ApprovalRequestId, d2.ActivityId, MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
        FROM @BeforeDetail d2
        INNER JOIN (
            SELECT ApprovalRequestId, ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
            FROM @BeforeDetail
            GROUP BY ApprovalRequestId, ActivityId
        ) lv
            ON lv.ApprovalRequestId = d2.ApprovalRequestId
           AND lv.ActivityId = d2.ActivityId
           AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
        GROUP BY d2.ApprovalRequestId, d2.ActivityId
    ) x ON x.LatestDetailId = d.ApprovalRequestDetailId
)
INSERT INTO @Failure (Item, Reason)
SELECT N'Latest', v.Reason
FROM (VALUES
    (CASE WHEN EXISTS (SELECT 1 FROM Latest WHERE ApprovalRequestId = 1424 AND ActivityId = 91457 AND ApprovalRequestDetailId <> 46299)
          THEN N'Latest detail for activity 91457 on 1424 is not 46299.' END),
    (CASE WHEN EXISTS (SELECT 1 FROM Latest WHERE ApprovalRequestId = 1483 AND ActivityId = 104263 AND ApprovalRequestDetailId <> 46301)
          THEN N'Latest detail for activity 104263 on 1483 is not 46301.' END),
    (CASE WHEN EXISTS (SELECT 1 FROM Latest WHERE ApprovalRequestDetailId IN (43086, 44852))
          THEN N'A preserved predecessor is being treated as the latest detail.' END),
    (CASE WHEN (SELECT COUNT(*) FROM Latest WHERE ApprovalRequestId = 1495 AND ApprovalRequestDetailId = 45183) <> 1
          THEN N'Detail 45183 is not the only latest row on 1495.' END),
    (CASE WHEN (SELECT COUNT(*) FROM Latest WHERE ApprovalRequestId = 1490 AND ApprovalRequestDetailId = 45062) <> 1
          THEN N'Detail 45062 is not the only latest row on 1490.' END)
) v (Reason)
WHERE v.Reason IS NOT NULL;

/* Guard: correlated history contains no Auditor decision. */
IF (SELECT COUNT(*) FROM @BeforeHistory) <> 8
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'History', N'Correlated history is not the expected eight rows. Evidence is ambiguous.');

INSERT INTO @Failure (Item, Reason)
SELECT v.Item, v.Reason
FROM (VALUES
    (N'5030', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHistory
        WHERE HistoryId = 5030 AND ApprovalRequestId = 1424 AND ChangedBy = N'AHS1180'
          AND OldStatus IS NULL AND NewStatus = N'Pending' AND Remarks = N'Created via bulk submit.'
          AND ApprovalRequestDetailId IS NULL AND ActivityId IS NULL
          AND ApproverLevel IS NULL AND ActionType IS NULL
     ) THEN N'History 5030 is not the employee submission on 1424.' END),
    (N'5132', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHistory
        WHERE HistoryId = 5132 AND ApprovalRequestId = 1424 AND ChangedBy = N'AHS1794'
          AND OldStatus = N'Pending' AND NewStatus = N'Returned'
          AND Remarks = N'Detail 43086 RETURN: Returned via Unified Day Modal - Added by Subham Kumar Panda (AHS1794)'
     ) THEN N'History 5132 is not the level-1 return of detail 43086.' END),
    (N'5259', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHistory
        WHERE HistoryId = 5259 AND ApprovalRequestId = 1424 AND ChangedBy = N'AHS1180'
          AND OldStatus IS NULL AND NewStatus = N'Pending' AND Remarks = N'Created via bulk submit.'
     ) THEN N'History 5259 is not the employee event stored on 1424 for the second header.' END),
    (N'5283', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHistory
        WHERE HistoryId = 5283 AND ApprovalRequestId = 1424 AND ChangedBy = N'AHS1794'
          AND OldStatus = N'Pending' AND NewStatus = N'Pending'
          AND Remarks = N'Level 1 completed. Forwarded to Auditor (Audit).'
     ) THEN N'History 5283 is not the level-1 handoff on 1424.' END),
    (N'5220', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHistory
        WHERE HistoryId = 5220 AND ApprovalRequestId = 1483 AND ChangedBy = N'M2659'
          AND OldStatus IS NULL AND NewStatus = N'Pending' AND Remarks = N'Created via bulk submit.'
     ) THEN N'History 5220 is not the employee submission on 1483.' END),
    (N'5239', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHistory
        WHERE HistoryId = 5239 AND ApprovalRequestId = 1483 AND ChangedBy = N'M2277'
          AND OldStatus = N'Pending' AND NewStatus = N'Returned'
          AND Remarks = N'Detail 44852 RETURN: Returned via Unified Day Modal - Added by Dhiraj Kumar Tripathi (M2277)'
     ) THEN N'History 5239 is not the level-1 return of detail 44852.' END),
    (N'5245', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHistory
        WHERE HistoryId = 5245 AND ApprovalRequestId = 1483 AND ChangedBy = N'M2659'
          AND OldStatus IS NULL AND NewStatus = N'Pending' AND Remarks = N'Created via bulk submit.'
     ) THEN N'History 5245 is not the employee event stored on 1483 for the second header.' END),
    (N'5248', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeHistory
        WHERE HistoryId = 5248 AND ApprovalRequestId = 1483 AND ChangedBy = N'M2277'
          AND OldStatus = N'Pending' AND NewStatus = N'Pending'
          AND Remarks = N'Level 1 completed. Forwarded to Auditor (Audit).'
     ) THEN N'History 5248 is not the level-1 handoff on 1483.' END)
) v (Item, Reason)
WHERE v.Reason IS NOT NULL;

IF EXISTS (
    SELECT 1
    FROM @BeforeHistory h
    WHERE UPPER(LTRIM(RTRIM(ISNULL(h.ChangedBy, N'')))) IN (N'AUDIT', N'AUDITONE')
       OR ISNULL(h.ApproverLevel, 0) >= 2
       OR h.Remarks LIKE N'%Auditor finalized%'
       OR h.Remarks LIKE N'%Ready for Payment%'
       OR h.Remarks LIKE N'%Rejected via%'
)
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'Auditor', N'Correlated history contains an Auditor decision. Leave both requests unchanged.');

/* Guard: activity ownership. */
IF (SELECT COUNT(*) FROM @BeforeActivity WHERE Login_Id = N'AHS1180') <> 30
   OR EXISTS (SELECT 1 FROM @BeforeActivity WHERE Login_Id = N'AHS1180' AND ApprovalRequestId <> 1424)
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'AHS1180', N'September activities are not 30 rows owned by request 1424.');
IF (SELECT COUNT(*) FROM @BeforeActivity WHERE Login_Id = N'M2659') <> 30
   OR EXISTS (SELECT 1 FROM @BeforeActivity WHERE Login_Id = N'M2659' AND ApprovalRequestId <> 1483)
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'M2659', N'September activities are not 30 rows owned by request 1483.');
IF (SELECT COUNT(*) FROM @BeforeActivity WHERE Login_Id = N'AHS1180' AND ClaimStatus = N'Submitted') <> 29
   OR (SELECT COUNT(*) FROM @BeforeActivity WHERE ActivityId = 91457 AND ClaimStatus = N'Approved' AND ApprovalRequestId = 1424 AND IsDeleted = 0 AND TotalDistanceKM = 73.33 AND CalculatedClaimAmount = 274.99 AND ActivityDate = CONVERT(date, '20260905', 112)) <> 1
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'91457', N'AHS1180 activities are not 29 Submitted plus active activity 91457 Approved.');
IF (SELECT COUNT(*) FROM @BeforeActivity WHERE Login_Id = N'M2659' AND ClaimStatus = N'Submitted') <> 29
   OR (SELECT COUNT(*) FROM @BeforeActivity WHERE ActivityId = 104263 AND ClaimStatus = N'Approved' AND ApprovalRequestId = 1483 AND IsDeleted = 0 AND TotalDistanceKM = 140.00 AND CalculatedClaimAmount = 5437.50 AND ActivityDate = CONVERT(date, '20260901', 112)) <> 1
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'104263', N'M2659 activities are not 29 Submitted plus active activity 104263 Approved.');
IF EXISTS (SELECT 1 FROM dbo.SalesDailyActivity WHERE ApprovalRequestId IN (1495, 1490))
    INSERT INTO @Failure (Item, Reason)
    VALUES (NULL, N'A duplicate request owns an activity.');

/* Guard: the four known expenses and the active-expense predicate. */
IF (SELECT COUNT(*) FROM @BeforeExpense) <> 4
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'Expense', N'Activities 91457 and 104263 do not have exactly four expense rows.');

INSERT INTO @Failure (Item, Reason)
SELECT v.Item, v.Reason
FROM (VALUES
    (N'39392', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeExpense
        WHERE ExpenseId = 39392 AND ActivityId = 91457 AND ExpenseType = N'DA'
          AND Amount = 225.00 AND ClaimStatus = N'Approved' AND IsDeleted = 0
     ) THEN N'Expense 39392 is not active Approved DA 225.' END),
    (N'39395', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeExpense
        WHERE ExpenseId = 39395 AND ActivityId = 91457 AND ExpenseType = N'Travel'
          AND Amount = 547.00 AND ClaimStatus = N'Approved' AND IsDeleted = 0
     ) THEN N'Expense 39395 is not active Approved Travel 547.' END),
    (N'41185', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeExpense
        WHERE ExpenseId = 41185 AND ActivityId = 104263 AND ExpenseType = N'DA'
          AND Amount = 225.00 AND ClaimStatus = N'Draft' AND IsDeleted = 1
     ) THEN N'Expense 41185 is not the soft-deleted Draft DA row.' END),
    (N'41487', CASE WHEN NOT EXISTS (
        SELECT 1 FROM @BeforeExpense
        WHERE ExpenseId = 41487 AND ActivityId = 104263 AND ExpenseType = N'DA'
          AND Amount = 225.00 AND ClaimStatus = N'Approved' AND IsDeleted = 0
     ) THEN N'Expense 41487 is not active Approved DA 225.' END)
) v (Item, Reason)
WHERE v.Reason IS NOT NULL;

IF (SELECT COUNT(*) FROM @BeforeExpense WHERE IsDeleted = 0 AND ISNULL(ClaimStatus, N'') <> N'Rejected') <> 3
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'Expense', N'Active expense predicate does not select exactly expenses 39392, 39395, and 41487.');

/*
    Guard: recalculation would otherwise rewrite latest expense snapshots.
    Abort unless every latest snapshot on 1424 and 1483 already equals the
    qualifying expense sums. Qualifying means IsDeleted = 0 and ClaimStatus
    is not Rejected, including Returned and Draft.
*/
;WITH CanonicalLatest AS
(
    SELECT d.ApprovalRequestDetailId, d.ActivityId,
           d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount, d.Snapshot_StationaryAmount
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN (
        SELECT d2.ActivityId, MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
        FROM dbo.SalesApprovalRequestDetail d2
        INNER JOIN (
            SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
            FROM dbo.SalesApprovalRequestDetail
            WHERE ApprovalRequestId IN (1424, 1483)
            GROUP BY ActivityId
        ) lv
            ON lv.ActivityId = d2.ActivityId
           AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
        WHERE d2.ApprovalRequestId IN (1424, 1483)
        GROUP BY d2.ActivityId
    ) x ON x.LatestDetailId = d.ApprovalRequestDetailId
    WHERE d.ApprovalRequestId IN (1424, 1483)
),
QualifyingExpense AS
(
    SELECT e.ActivityId,
           SUM(CASE WHEN e.ExpenseType = N'DA' THEN e.Amount ELSE 0 END) AS DAAmount,
           SUM(CASE WHEN e.ExpenseType = N'Hotel' THEN e.Amount ELSE 0 END) AS HotelAmount,
           SUM(CASE WHEN e.ExpenseType = N'Travel' THEN e.Amount ELSE 0 END) AS TravelAmount,
           SUM(CASE WHEN e.ExpenseType = N'Stationary' THEN e.Amount ELSE 0 END) AS StationaryAmount
    FROM dbo.SalesDailyExpense e
    WHERE e.ActivityId IN (SELECT c.ActivityId FROM CanonicalLatest c)
      AND e.IsDeleted = 0
      AND ISNULL(e.ClaimStatus, N'') <> N'Rejected'
    GROUP BY e.ActivityId
)
INSERT INTO @Failure (Item, Reason)
SELECT CONVERT(nvarchar(40), c.ActivityId), N'Latest expense snapshot differs from qualifying expenses, so recalculation would change an amount.'
FROM CanonicalLatest c
LEFT JOIN QualifyingExpense q ON q.ActivityId = c.ActivityId
WHERE ISNULL(c.Snapshot_DAAmount, 0) <> ISNULL(q.DAAmount, 0)
   OR ISNULL(c.Snapshot_HotelAmount, 0) <> ISNULL(q.HotelAmount, 0)
   OR ISNULL(c.Snapshot_TravelAmount, 0) <> ISNULL(q.TravelAmount, 0)
   OR ISNULL(c.Snapshot_StationaryAmount, 0) <> ISNULL(q.StationaryAmount, 0);

IF (SELECT COUNT(*) FROM @BeforeHeader) <> 4
    INSERT INTO @Failure (Item, Reason) VALUES (NULL, N'Header before-image is not exactly four requests.');
IF (SELECT COUNT(*) FROM @BeforeApprover) <> 8
    INSERT INTO @Failure (Item, Reason) VALUES (NULL, N'Approver before-image is not exactly eight rows.');
IF (SELECT COUNT(*) FROM @BeforeDetail WHERE ApprovalRequestId = 1424) <> 31
    INSERT INTO @Failure (Item, Reason) VALUES (N'1424', N'Detail before-image for 1424 is not 31 rows.');
IF (SELECT COUNT(*) FROM @BeforeDetail WHERE ApprovalRequestId = 1483) <> 31
    INSERT INTO @Failure (Item, Reason) VALUES (N'1483', N'Detail before-image for 1483 is not 31 rows.');
IF (SELECT COUNT(*) FROM @BeforeDetail WHERE ApprovalRequestId = 1495) <> 1
    INSERT INTO @Failure (Item, Reason) VALUES (N'1495', N'Detail before-image for 1495 is not the single row 45183.');
IF (SELECT COUNT(*) FROM @BeforeDetail WHERE ApprovalRequestId = 1490) <> 1
    INSERT INTO @Failure (Item, Reason) VALUES (N'1490', N'Detail before-image for 1490 is not the single row 45062.');
IF EXISTS (
    SELECT 1
    FROM @BeforeDetail d
    WHERE d.PreviousDetailId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM @BeforeDetail p
          WHERE p.ApprovalRequestDetailId = d.PreviousDetailId
            AND p.ActivityId = d.ActivityId
            AND p.ApprovalRequestId = d.ApprovalRequestId
      )
)
    INSERT INTO @Failure (Item, Reason)
    VALUES (N'Lineage', N'A predecessor is missing, crosses activities, or crosses requests.');

SELECT
    N'Session' AS ResultSetName,
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')) AS ProductVersion,
    CONVERT(nvarchar(128), SERVERPROPERTY('Edition')) AS Edition,
    SYSDATETIMEOFFSET() AS SessionTime,
    @CommitChanges AS CommitChanges;

SELECT N'BeforeHeader' AS ResultSetName, * FROM @BeforeHeader ORDER BY ApprovalRequestId;
SELECT N'BeforeApprover' AS ResultSetName, * FROM @BeforeApprover ORDER BY ApprovalRequestApproverId;
SELECT N'BeforeDetail' AS ResultSetName, * FROM @BeforeDetail ORDER BY ApprovalRequestId, ActivityId, DetailVersion, ApprovalRequestDetailId;
SELECT N'BeforeHistory' AS ResultSetName, * FROM @BeforeHistory ORDER BY HistoryId;
SELECT N'BeforeActivity' AS ResultSetName, * FROM @BeforeActivity ORDER BY Login_Id, ActivityDate, ActivityId;
SELECT N'BeforeExpense' AS ResultSetName, * FROM @BeforeExpense ORDER BY ActivityId, ExpenseId;

SELECT
    N'EligibilityFailures' AS ResultSetName,
    f.Item,
    f.Reason
FROM @Failure f
ORDER BY f.Item, f.Reason;

IF EXISTS (SELECT 1 FROM @Failure)
    THROW 53210, 'Eligibility failed. No transaction was started. See EligibilityFailures.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @LockedHeaders int;
    DECLARE @LockedDetails int;
    DECLARE @LockedActivities int;
    DECLARE @LockedApprovers int;
    DECLARE @LockedExpenses int;
    DECLARE @LockedHistory int;
    DECLARE @HeaderUpdates int;
    DECLARE @DetailUpdates int;
    DECLARE @ActivityUpdates int;
    DECLARE @ExpenseUpdates int;
    DECLARE @SnapshotDrift int;

    SELECT @LockedHeaders = COUNT(*)
    FROM dbo.SalesApprovalRequest WITH (UPDLOCK, HOLDLOCK)
    WHERE ApprovalRequestId IN (1424, 1495, 1483, 1490);

    SELECT @LockedDetails = COUNT(*)
    FROM dbo.SalesApprovalRequestDetail WITH (UPDLOCK, HOLDLOCK)
    WHERE ApprovalRequestId IN (1424, 1495, 1483, 1490);

    SELECT @LockedActivities = COUNT(*)
    FROM dbo.SalesDailyActivity WITH (UPDLOCK, HOLDLOCK)
    WHERE ActivityId IN (SELECT b.ActivityId FROM @BeforeActivity b);

    SELECT @LockedApprovers = COUNT(*)
    FROM dbo.SalesApprovalRequestApprover WITH (UPDLOCK, HOLDLOCK)
    WHERE ApprovalRequestId IN (1424, 1495, 1483, 1490);

    SELECT @LockedExpenses = COUNT(*)
    FROM dbo.SalesDailyExpense WITH (UPDLOCK, HOLDLOCK)
    WHERE ActivityId IN (SELECT b.ActivityId FROM @BeforeActivity b);

    SELECT @LockedHistory = COUNT(*)
    FROM dbo.ApprovalRequestStatusHistory WITH (UPDLOCK, HOLDLOCK)
    WHERE HistoryId IN (SELECT b.HistoryId FROM @BeforeHistory b);

    IF @LockedHeaders <> 4 OR @LockedDetails <> 64 OR @LockedActivities <> 60
       OR @LockedApprovers <> 8 OR @LockedExpenses < 4 OR @LockedHistory <> 8
        THROW 53211, 'Locked row counts drifted from the before-image.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest r
        INNER JOIN @BeforeHeader b ON b.ApprovalRequestId = r.ApprovalRequestId
        WHERE r.Status <> b.Status
           OR ISNULL(r.CurrentApproverLoginId, N'') <> ISNULL(b.CurrentApproverLoginId, N'')
           OR ISNULL(r.CurrentApproverLevel, -1) <> ISNULL(b.CurrentApproverLevel, -1)
           OR ISNULL(r.TotalKM, -1) <> ISNULL(b.TotalKM, -1)
           OR ISNULL(r.TotalClaimAmount, -1) <> ISNULL(b.TotalClaimAmount, -1)
           OR ISNULL(r.FinalApprovedAmount, -1) <> ISNULL(b.FinalApprovedAmount, -1)
           OR ISNULL(r.ModifiedBy, N'') <> ISNULL(b.ModifiedBy, N'')
    )
        THROW 53212, 'A locked header no longer matches the before-image.', 1;

    ;WITH CanonicalLatest AS
    (
        SELECT d.ApprovalRequestDetailId, d.ActivityId,
               d.Snapshot_DAAmount, d.Snapshot_HotelAmount, d.Snapshot_TravelAmount, d.Snapshot_StationaryAmount
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN (
            SELECT d2.ActivityId, MAX(d2.ApprovalRequestDetailId) AS LatestDetailId
            FROM dbo.SalesApprovalRequestDetail d2
            INNER JOIN (
                SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
                FROM dbo.SalesApprovalRequestDetail
                WHERE ApprovalRequestId IN (1424, 1483)
                GROUP BY ActivityId
            ) lv
                ON lv.ActivityId = d2.ActivityId
               AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
            WHERE d2.ApprovalRequestId IN (1424, 1483)
            GROUP BY d2.ActivityId
        ) x ON x.LatestDetailId = d.ApprovalRequestDetailId
        WHERE d.ApprovalRequestId IN (1424, 1483)
    ),
    QualifyingExpense AS
    (
        SELECT e.ActivityId,
               SUM(CASE WHEN e.ExpenseType = N'DA' THEN e.Amount ELSE 0 END) AS DAAmount,
               SUM(CASE WHEN e.ExpenseType = N'Hotel' THEN e.Amount ELSE 0 END) AS HotelAmount,
               SUM(CASE WHEN e.ExpenseType = N'Travel' THEN e.Amount ELSE 0 END) AS TravelAmount,
               SUM(CASE WHEN e.ExpenseType = N'Stationary' THEN e.Amount ELSE 0 END) AS StationaryAmount
        FROM dbo.SalesDailyExpense e
        WHERE e.ActivityId IN (SELECT c.ActivityId FROM CanonicalLatest c)
          AND e.IsDeleted = 0
          AND ISNULL(e.ClaimStatus, N'') <> N'Rejected'
        GROUP BY e.ActivityId
    )
    SELECT @SnapshotDrift = COUNT(*)
    FROM CanonicalLatest c
    LEFT JOIN QualifyingExpense q ON q.ActivityId = c.ActivityId
    WHERE ISNULL(c.Snapshot_DAAmount, 0) <> ISNULL(q.DAAmount, 0)
       OR ISNULL(c.Snapshot_HotelAmount, 0) <> ISNULL(q.HotelAmount, 0)
       OR ISNULL(c.Snapshot_TravelAmount, 0) <> ISNULL(q.TravelAmount, 0)
       OR ISNULL(c.Snapshot_StationaryAmount, 0) <> ISNULL(q.StationaryAmount, 0);

    IF @SnapshotDrift > 0
        THROW 53214, 'A locked latest expense snapshot no longer matches qualifying expenses.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestApprover a
        WHERE a.ApprovalRequestId IN (1424, 1495, 1483, 1490)
          AND (
                UPPER(LTRIM(RTRIM(ISNULL(a.ApproverRole, N'')))) = N'AUDITOR'
             OR UPPER(LTRIM(RTRIM(ISNULL(a.ApproverLoginId, N'')))) IN (N'AUDIT', N'AUDITONE')
              )
          AND (
                a.Status <> N'Pending'
             OR a.ActionBy IS NOT NULL
             OR a.ActionDate IS NOT NULL
             OR NULLIF(LTRIM(RTRIM(a.Remarks)), N'') IS NOT NULL
              )
    )
        THROW 53213, 'An Auditor action appeared after the before-image.', 1;

    UPDATE dbo.SalesApprovalRequest
    SET Status = N'Rejected',
        ModifiedBy = N'Reconciliation',
        ModifiedOn = SYSUTCDATETIME()
    WHERE ApprovalRequestId = 1495
      AND Status = N'Pending'
      AND CurrentApproverLoginId = N'Audit'
      AND CurrentApproverLevel = 2
      AND RequesterLoginId = N'AHS1180'
      AND YearNo = 2026
      AND MonthNo = 9;

    SET @HeaderUpdates = @@ROWCOUNT;

    UPDATE dbo.SalesApprovalRequest
    SET Status = N'Rejected',
        ModifiedBy = N'Reconciliation',
        ModifiedOn = SYSUTCDATETIME()
    WHERE ApprovalRequestId = 1490
      AND Status = N'Pending'
      AND CurrentApproverLoginId = N'Audit'
      AND CurrentApproverLevel = 2
      AND RequesterLoginId = N'M2659'
      AND YearNo = 2026
      AND MonthNo = 9;

    SET @HeaderUpdates = @HeaderUpdates + @@ROWCOUNT;
    IF @HeaderUpdates <> 2
        THROW 53220, 'The two duplicate headers were not both the expected Pending rows.', 1;

    UPDATE d
    SET d.DetailStatus = N'Returned'
    FROM dbo.SalesApprovalRequestDetail d
    WHERE d.ApprovalRequestDetailId = 46299
      AND d.ApprovalRequestId = 1424
      AND d.ActivityId = 91457
      AND d.DetailVersion = 2
      AND d.PreviousDetailId = 43086
      AND d.DetailStatus = N'Approved'
      AND d.DetailApprovedBy = N'AHS1794'
      AND d.DetailApprovedLevel = 1;

    SET @DetailUpdates = @@ROWCOUNT;

    UPDATE d
    SET d.DetailStatus = N'Returned'
    FROM dbo.SalesApprovalRequestDetail d
    WHERE d.ApprovalRequestDetailId = 46301
      AND d.ApprovalRequestId = 1483
      AND d.ActivityId = 104263
      AND d.DetailVersion = 2
      AND d.PreviousDetailId = 44852
      AND d.DetailStatus = N'Approved'
      AND d.DetailApprovedBy = N'M2277'
      AND d.DetailApprovedLevel = 1;

    SET @DetailUpdates = @DetailUpdates + @@ROWCOUNT;
    IF @DetailUpdates <> 2
        THROW 53221, 'Latest details 46299 and 46301 were not both the expected Approved rows.', 1;

    UPDATE a
    SET a.ClaimStatus = N'Returned',
        a.ModifiedByUserId = N'Reconciliation',
        a.ModifiedDateTime = DATEADD(MINUTE, 330, SYSUTCDATETIME())
    FROM dbo.SalesDailyActivity a
    WHERE a.ActivityId = 91457
      AND a.ApprovalRequestId = 1424
      AND a.Login_Id = N'AHS1180'
      AND a.ClaimStatus = N'Approved'
      AND a.IsDeleted = 0;

    SET @ActivityUpdates = @@ROWCOUNT;

    UPDATE a
    SET a.ClaimStatus = N'Returned',
        a.ModifiedByUserId = N'Reconciliation',
        a.ModifiedDateTime = DATEADD(MINUTE, 330, SYSUTCDATETIME())
    FROM dbo.SalesDailyActivity a
    WHERE a.ActivityId = 104263
      AND a.ApprovalRequestId = 1483
      AND a.Login_Id = N'M2659'
      AND a.ClaimStatus = N'Approved'
      AND a.IsDeleted = 0;

    SET @ActivityUpdates = @ActivityUpdates + @@ROWCOUNT;
    IF @ActivityUpdates <> 2
        THROW 53222, 'Activities 91457 and 104263 were not both active Approved rows on the canonical requests.', 1;

    UPDATE e
    SET e.ClaimStatus = N'Returned',
        e.ClaimProcessedBy = N'Reconciliation',
        e.ClaimProcessedDate = DATEADD(MINUTE, 330, SYSUTCDATETIME()),
        e.ModifiedBy = N'Reconciliation',
        e.ModifiedOn = SYSUTCDATETIME()
    FROM dbo.SalesDailyExpense e
    INNER JOIN @BeforeExpense b ON b.ExpenseId = e.ExpenseId
    WHERE b.IsDeleted = 0
      AND e.IsDeleted = 0
      AND ISNULL(e.ClaimStatus, N'') <> N'Rejected'
      AND e.ActivityId IN (91457, 104263);

    SET @ExpenseUpdates = @@ROWCOUNT;
    IF @ExpenseUpdates <> 3
        THROW 53223, 'Active expense return count was not exactly 39392, 39395, and 41487.', 1;

    IF @@TRANCOUNT <> 1
        THROW 53224, 'Transaction count changed before recalculation.', 1;

    EXEC dbo.usp_RecalculateApprovalRequest @ApprovalRequestId = 1424;
    EXEC dbo.usp_RecalculateApprovalRequest @ApprovalRequestId = 1483;

    IF @@TRANCOUNT <> 1 OR XACT_STATE() <> 1
        THROW 53225, 'usp_RecalculateApprovalRequest changed the outer transaction.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = 1495
          AND (
                Status <> N'Rejected'
             OR CurrentApproverLoginId <> N'Audit'
             OR CurrentApproverLevel <> 2
             OR ModifiedBy <> N'Reconciliation'
             OR TotalKM <> 73.33
             OR TotalClaimAmount <> 274.99
             OR TotalDAAmount <> 225.00
             OR TotalTravelAmount <> 547.00
             OR TotalExpenseAmount <> 772.00
             OR ApprovedKM <> 73.33
             OR ApprovedClaimAmount <> 274.99
             OR ApprovedDAAmount <> 225.00
             OR ApprovedTravelAmount <> 547.00
             OR FinalApprovedAmount <> 1046.99
          )
    )
        THROW 53230, 'Request 1495 postcondition failed.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = 1490
          AND (
                Status <> N'Rejected'
             OR CurrentApproverLoginId <> N'Audit'
             OR CurrentApproverLevel <> 2
             OR ModifiedBy <> N'Reconciliation'
             OR TotalKM <> 1450.00
             OR TotalClaimAmount <> 5437.50
             OR TotalDAAmount <> 450.00
             OR TotalExpenseAmount <> 450.00
             OR ApprovedKM <> 140.00
             OR ApprovedClaimAmount <> 525.00
             OR ApprovedDAAmount <> 225.00
             OR FinalApprovedAmount <> 750.00
          )
    )
        THROW 53231, 'Request 1490 postcondition failed.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = 1424
          AND (
                Status <> N'Returned'
             OR CurrentApproverLoginId <> N'AHS1794'
             OR CurrentApproverLevel <> 1
             OR ModifiedBy <> N'AHS1794'
             OR TotalKM <> 1514.64
             OR TotalClaimAmount <> 5679.91
             OR TotalDAAmount <> 5925.00
             OR ISNULL(TotalHotelAmount, 0) <> 0
             OR TotalTravelAmount <> 6617.00
             OR TotalExpenseAmount <> 12542.00
             OR TotalStationaryAmount <> 0
             OR ApprovedKM <> 1441.31
             OR ApprovedClaimAmount <> 5404.92
             OR ApprovedDAAmount <> 5475.00
             OR ISNULL(ApprovedHotelAmount, 0) <> 0
             OR ApprovedTravelAmount <> 5395.00
             OR FinalApprovedAmount <> 16274.92
          )
    )
        THROW 53232, 'Request 1424 header postcondition failed.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = 1483
          AND (
                Status <> N'Returned'
             OR CurrentApproverLoginId <> N'M2277'
             OR CurrentApproverLevel <> 1
             OR ModifiedBy <> N'M2277'
             OR TotalKM <> 4292.52
             OR TotalClaimAmount <> 16096.96
             OR TotalDAAmount <> 5525.00
             OR TotalHotelAmount <> 600.00
             OR TotalTravelAmount <> 350.00
             OR TotalExpenseAmount <> 6250.00
             OR TotalStationaryAmount <> 0
             OR ApprovedKM <> 2842.52
             OR ApprovedClaimAmount <> 10659.46
             OR ApprovedDAAmount <> 5300.00
             OR ApprovedHotelAmount <> 600.00
             OR ApprovedTravelAmount <> 350.00
             OR FinalApprovedAmount <> 16909.46
          )
    )
        THROW 53233, 'Request 1483 header postcondition failed.', 1;

    IF (
        SELECT COUNT(*)
        FROM dbo.SalesApprovalRequest
        WHERE RequesterLoginId = N'AHS1180'
          AND YearNo = 2026 AND MonthNo = 9
          AND UPPER(LTRIM(RTRIM(Status))) IN (N'RETURNED', N'PENDING', N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED')
    ) <> 1
    OR NOT EXISTS (
        SELECT 1 FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = 1424 AND Status = N'Returned'
    )
        THROW 53234, 'AHS1180 does not have exactly returned request 1424 active.', 1;

    IF (
        SELECT COUNT(*)
        FROM dbo.SalesApprovalRequest
        WHERE RequesterLoginId = N'M2659'
          AND YearNo = 2026 AND MonthNo = 9
          AND UPPER(LTRIM(RTRIM(Status))) IN (N'RETURNED', N'PENDING', N'COMPLETED', N'READYFORPAYMENT', N'PROCESSED')
    ) <> 1
    OR NOT EXISTS (
        SELECT 1 FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = 1483 AND Status = N'Returned'
    )
        THROW 53235, 'M2659 does not have exactly returned request 1483 active.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN @BeforeDetail b ON b.ApprovalRequestDetailId = d.ApprovalRequestDetailId
        WHERE d.ApprovalRequestDetailId IN (43086, 45183, 44852, 45062)
          AND (
                d.ApprovalRequestId <> b.ApprovalRequestId
             OR d.ActivityId <> b.ActivityId
             OR d.DetailVersion <> b.DetailVersion
             OR ISNULL(d.PreviousDetailId, -1) <> ISNULL(b.PreviousDetailId, -1)
             OR ISNULL(d.DetailStatus, N'') <> ISNULL(b.DetailStatus, N'')
             OR ISNULL(d.DetailApprovedBy, N'') <> ISNULL(b.DetailApprovedBy, N'')
             OR ISNULL(d.DetailApprovedLevel, -1) <> ISNULL(b.DetailApprovedLevel, -1)
             OR ISNULL(d.DetailApprovedOn, CONVERT(datetime2, '19000101', 112)) <> ISNULL(b.DetailApprovedOn, CONVERT(datetime2, '19000101', 112))
             OR ISNULL(d.Snapshot_TotalDistanceKM, -1) <> ISNULL(b.Snapshot_TotalDistanceKM, -1)
             OR ISNULL(d.Snapshot_CalculatedClaimAmount, -1) <> ISNULL(b.Snapshot_CalculatedClaimAmount, -1)
             OR ISNULL(d.Snapshot_DAAmount, -1) <> ISNULL(b.Snapshot_DAAmount, -1)
             OR ISNULL(d.Snapshot_HotelAmount, -1) <> ISNULL(b.Snapshot_HotelAmount, -1)
             OR ISNULL(d.Snapshot_TravelAmount, -1) <> ISNULL(b.Snapshot_TravelAmount, -1)
             OR ISNULL(d.Snapshot_StationaryAmount, -1) <> ISNULL(b.Snapshot_StationaryAmount, -1)
             OR ISNULL(d.Snapshot_ExpenseTotal, -1) <> ISNULL(b.Snapshot_ExpenseTotal, -1)
             OR ISNULL(d.ModifiedOn, CONVERT(datetime2, '19000101', 112)) <> ISNULL(b.ModifiedOn, CONVERT(datetime2, '19000101', 112))
          )
    )
        THROW 53236, 'A preserved detail row changed.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestDetail d
        WHERE d.ApprovalRequestDetailId = 46299
          AND (
                d.ApprovalRequestId <> 1424
             OR d.ActivityId <> 91457
             OR d.DetailVersion <> 2
             OR d.PreviousDetailId <> 43086
             OR ISNULL(d.DetailStatus, N'') <> N'Returned'
             OR ISNULL(d.DetailApprovedBy, N'') <> N'AHS1794'
             OR ISNULL(d.DetailApprovedLevel, -1) <> 1
             OR ISNULL(d.Snapshot_TotalDistanceKM, -1) <> 73.33
             OR ISNULL(d.Snapshot_CalculatedClaimAmount, -1) <> 274.99
             OR ISNULL(d.Snapshot_DAAmount, -1) <> 225.00
             OR ISNULL(d.Snapshot_HotelAmount, -1) <> 0
             OR ISNULL(d.Snapshot_TravelAmount, -1) <> 547.00
             OR ISNULL(d.Snapshot_StationaryAmount, -1) <> 0
             OR ISNULL(d.Snapshot_ExpenseTotal, -1) <> 772.00
          )
    )
    OR EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestDetail d
        WHERE d.ApprovalRequestDetailId = 46301
          AND (
                d.ApprovalRequestId <> 1483
             OR d.ActivityId <> 104263
             OR d.DetailVersion <> 2
             OR d.PreviousDetailId <> 44852
             OR ISNULL(d.DetailStatus, N'') <> N'Returned'
             OR ISNULL(d.DetailApprovedBy, N'') <> N'M2277'
             OR ISNULL(d.DetailApprovedLevel, -1) <> 1
             OR ISNULL(d.Snapshot_TotalDistanceKM, -1) <> 140.00
             OR ISNULL(d.Snapshot_CalculatedClaimAmount, -1) <> 525.00
             OR ISNULL(d.Snapshot_DAAmount, -1) <> 225.00
             OR ISNULL(d.Snapshot_HotelAmount, -1) <> 0
             OR ISNULL(d.Snapshot_TravelAmount, -1) <> 0
             OR ISNULL(d.Snapshot_StationaryAmount, -1) <> 0
             OR ISNULL(d.Snapshot_ExpenseTotal, -1) <> 225.00
          )
    )
        THROW 53237, 'A returned latest detail lost its version, predecessor, or level-1 metadata.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestDetail d
        INNER JOIN @BeforeDetail b ON b.ApprovalRequestDetailId = d.ApprovalRequestDetailId
        WHERE d.ApprovalRequestDetailId NOT IN (46299, 46301, 43086, 45183, 44852, 45062)
          AND (
                d.ApprovalRequestId <> b.ApprovalRequestId
             OR d.ActivityId <> b.ActivityId
             OR d.DetailVersion <> b.DetailVersion
             OR ISNULL(d.PreviousDetailId, -1) <> ISNULL(b.PreviousDetailId, -1)
             OR ISNULL(d.DetailStatus, N'') <> ISNULL(b.DetailStatus, N'')
             OR ISNULL(d.DetailApprovedBy, N'') <> ISNULL(b.DetailApprovedBy, N'')
             OR ISNULL(d.DetailApprovedLevel, -1) <> ISNULL(b.DetailApprovedLevel, -1)
             OR ISNULL(d.Snapshot_TotalDistanceKM, -1) <> ISNULL(b.Snapshot_TotalDistanceKM, -1)
             OR ISNULL(d.Snapshot_CalculatedClaimAmount, -1) <> ISNULL(b.Snapshot_CalculatedClaimAmount, -1)
          )
    )
        THROW 53238, 'An unrelated detail changed status, ownership, version, or claim snapshot.', 1;

    IF (SELECT COUNT(*) FROM dbo.SalesApprovalRequestDetail WHERE ApprovalRequestId IN (1424, 1495, 1483, 1490)) <> 64
        THROW 53239, 'Detail row count changed.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesDailyActivity a
        INNER JOIN @BeforeActivity b ON b.ActivityId = a.ActivityId
        WHERE a.ActivityId NOT IN (91457, 104263)
          AND (
                ISNULL(a.ApprovalRequestId, -1) <> ISNULL(b.ApprovalRequestId, -1)
             OR ISNULL(a.ClaimStatus, N'') <> ISNULL(b.ClaimStatus, N'')
             OR ISNULL(a.TotalDistanceKM, -1) <> ISNULL(b.TotalDistanceKM, -1)
             OR ISNULL(a.CalculatedClaimAmount, -1) <> ISNULL(b.CalculatedClaimAmount, -1)
             OR a.IsDeleted <> b.IsDeleted
          )
    )
        THROW 53240, 'An unaffected September activity changed.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesDailyActivity a
        WHERE a.ActivityId IN (91457, 104263)
          AND (
                a.ClaimStatus <> N'Returned'
             OR a.IsDeleted <> 0
             OR (a.ActivityId = 91457 AND (a.ApprovalRequestId <> 1424 OR a.TotalDistanceKM <> 73.33 OR a.CalculatedClaimAmount <> 274.99))
             OR (a.ActivityId = 104263 AND (a.ApprovalRequestId <> 1483 OR a.TotalDistanceKM <> 140.00 OR a.CalculatedClaimAmount <> 5437.50))
          )
    )
        THROW 53241, 'A returned activity lost its request pointer or stored distance.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesDailyExpense e
        INNER JOIN @BeforeExpense b ON b.ExpenseId = e.ExpenseId
        WHERE e.Amount <> b.Amount
           OR e.IsDeleted <> b.IsDeleted
           OR e.ExpenseType <> b.ExpenseType
           OR e.ActivityId <> b.ActivityId
           OR (b.IsDeleted = 1 AND ISNULL(e.ClaimStatus, N'') <> ISNULL(b.ClaimStatus, N''))
           OR (b.IsDeleted = 0 AND ISNULL(e.ClaimStatus, N'') <> N'Returned')
    )
        THROW 53242, 'An expense amount, deletion flag, or status transition is wrong.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestApprover a
        INNER JOIN @BeforeApprover b ON b.ApprovalRequestApproverId = a.ApprovalRequestApproverId
        WHERE a.ApprovalRequestId <> b.ApprovalRequestId
           OR ISNULL(a.ApproverLoginId, N'') <> ISNULL(b.ApproverLoginId, N'')
           OR ISNULL(a.ApproverRole, N'') <> ISNULL(b.ApproverRole, N'')
           OR ISNULL(a.ApproverLevel, -1) <> ISNULL(b.ApproverLevel, -1)
           OR a.Status <> b.Status
           OR ISNULL(a.ActionBy, N'') <> ISNULL(b.ActionBy, N'')
           OR ISNULL(a.ActionDate, CONVERT(datetime2, '19000101', 112)) <> ISNULL(b.ActionDate, CONVERT(datetime2, '19000101', 112))
           OR ISNULL(a.Remarks, N'') <> ISNULL(b.Remarks, N'')
    )
        THROW 53243, 'An approver row changed.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.ApprovalRequestStatusHistory h
        FULL OUTER JOIN @BeforeHistory b ON b.HistoryId = h.HistoryId
        WHERE (h.ApprovalRequestId IN (1424, 1495, 1483, 1490) OR b.HistoryId IS NOT NULL)
          AND (
                h.HistoryId IS NULL
             OR b.HistoryId IS NULL
             OR h.ApprovalRequestId <> b.ApprovalRequestId
             OR ISNULL(h.OldStatus, N'') <> ISNULL(b.OldStatus, N'')
             OR h.NewStatus <> b.NewStatus
             OR ISNULL(h.ChangedBy, N'') <> ISNULL(b.ChangedBy, N'')
             OR h.ChangedOn <> b.ChangedOn
             OR ISNULL(h.Remarks, N'') <> ISNULL(b.Remarks, N'')
             OR ISNULL(h.ApprovalRequestDetailId, -1) <> ISNULL(b.ApprovalRequestDetailId, -1)
             OR ISNULL(h.ActivityId, -1) <> ISNULL(b.ActivityId, -1)
             OR ISNULL(h.ApproverLevel, -1) <> ISNULL(b.ApproverLevel, -1)
             OR ISNULL(h.ActionType, N'') <> ISNULL(b.ActionType, N'')
          )
    )
        THROW 53244, 'Approval history changed.', 1;

    SELECT
        N'InsideTransaction' AS ResultSetName,
        r.ApprovalRequestId,
        r.Status,
        r.CurrentApproverLoginId,
        r.CurrentApproverLevel,
        r.TotalKM,
        r.TotalClaimAmount,
        r.ApprovedKM,
        r.ApprovedClaimAmount,
        r.ApprovedDAAmount,
        r.ApprovedHotelAmount,
        r.ApprovedTravelAmount,
        r.FinalApprovedAmount,
        r.ModifiedBy
    FROM dbo.SalesApprovalRequest r
    WHERE r.ApprovalRequestId IN (1424, 1495, 1483, 1490)
    ORDER BY r.ApprovalRequestId;

    SELECT
        N'InsideTransactionDetail' AS ResultSetName,
        d.ApprovalRequestDetailId,
        d.ApprovalRequestId,
        d.ActivityId,
        d.DetailVersion,
        d.PreviousDetailId,
        d.DetailStatus,
        d.DetailApprovedBy,
        d.DetailApprovedLevel
    FROM dbo.SalesApprovalRequestDetail d
    WHERE d.ApprovalRequestDetailId IN (43086, 46299, 45183, 44852, 46301, 45062)
    ORDER BY d.ApprovalRequestDetailId;

    SELECT
        N'InsideTransactionActivity' AS ResultSetName,
        a.ActivityId,
        a.ApprovalRequestId,
        a.ClaimStatus,
        a.TotalDistanceKM,
        a.CalculatedClaimAmount
    FROM dbo.SalesDailyActivity a
    WHERE a.ActivityId IN (91457, 104263)
    ORDER BY a.ActivityId;

    SELECT
        N'InsideTransactionExpense' AS ResultSetName,
        e.ExpenseId,
        e.ActivityId,
        e.ExpenseType,
        e.Amount,
        e.ClaimStatus,
        e.IsDeleted
    FROM dbo.SalesDailyExpense e
    WHERE e.ExpenseId IN (39392, 39395, 41185, 41487)
    ORDER BY e.ExpenseId;

    IF @CommitChanges = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'COMMITTED. Requests 1495 and 1490 are Rejected. Latest details 46299 and 46301, and activities 91457 and 104263, are Returned.';
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
    r.ApprovedKM,
    r.ApprovedClaimAmount,
    r.ApprovedDAAmount,
    r.FinalApprovedAmount,
    r.ModifiedBy
FROM dbo.SalesApprovalRequest r
WHERE r.ApprovalRequestId IN (1424, 1495, 1483, 1490)
ORDER BY r.ApprovalRequestId;
