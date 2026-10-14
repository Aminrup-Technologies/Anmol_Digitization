/*
    Sales Monthly Review — auditor reassignment
    Server:   MUMANMOAPP01\SQLEXPRESS
    Database: AnmolDristi

    Preview by default. This script rolls the transaction back unless
    @CommitChanges is set to 1. Do not change that value on the first run.

    SSMS:
      1. Connect to MUMANMOAPP01\SQLEXPRESS.
      2. Select database AnmolDristi.
      3. Leave @CommitChanges = 0 and execute the whole script (F5).
      4. Review every result set. The last database read must still show Audit.
      5. Only after that review, set @CommitChanges = 1 and execute once.
      6. Confirm TransactionOutcome = COMMITTED and the post-transaction
         result set shows AuditOne on all 15 Auditor rows.

    Scope is the 15 listed IDs only. Request 1240 is not included.
    The script updates existing Auditor rows. It does not insert approver
    rows, approval history, or claim details.

    COMMIT SWITCH
      0 = preview. Updates run inside a transaction and are rolled back.
      1 = commit. Set this only after the preview result sets are accepted.
*/
DECLARE @CommitChanges bit = 0;

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

DECLARE @TargetIds TABLE (ApprovalRequestId INT NOT NULL PRIMARY KEY);
INSERT INTO @TargetIds (ApprovalRequestId) VALUES
    (1247), (1276), (1293), (1324), (1536),
    (1562), (1648), (1667), (1675), (1676),
    (1470), (1517), (1558), (1563), (1616);

DECLARE @QueueOwnerIds TABLE (ApprovalRequestId INT NOT NULL PRIMARY KEY);
INSERT INTO @QueueOwnerIds (ApprovalRequestId) VALUES
    (1470), (1517), (1558), (1563), (1616);

DECLARE @LevelOneIds TABLE (ApprovalRequestId INT NOT NULL PRIMARY KEY);
INSERT INTO @LevelOneIds (ApprovalRequestId) VALUES
    (1247), (1276), (1293), (1324), (1536),
    (1562), (1648), (1667), (1675), (1676);

DECLARE @Failure TABLE
(
    ApprovalRequestId INT NULL,
    Reason NVARCHAR(200) NOT NULL
);

DECLARE @Before TABLE
(
    ApprovalRequestId INT NOT NULL PRIMARY KEY,
    RequesterLoginId NVARCHAR(100) NOT NULL,
    YearNo INT NOT NULL,
    MonthNo INT NOT NULL,
    RequestDate DATETIME2 NOT NULL,
    RequestStatus NVARCHAR(50) NOT NULL,
    CurrentApproverLoginId NVARCHAR(100) NULL,
    CurrentApproverLevel INT NULL,
    RegionId INT NULL,
    RegionMatchCount INT NOT NULL,
    RegionDescription VARCHAR(50) NULL,
    AuditorRowId INT NULL,
    AuditorLogin NVARCHAR(100) NULL,
    AuditorRole NVARCHAR(100) NULL,
    AuditorLevel INT NULL,
    AuditorStatus NVARCHAR(50) NULL,
    AuditorActionBy NVARCHAR(100) NULL,
    AuditorActionDate DATETIME2 NULL,
    ApproverRowCount INT NOT NULL,
    QueueGroup NVARCHAR(20) NOT NULL
);

DECLARE @BeforeOther TABLE
(
    ApprovalRequestApproverId INT NOT NULL PRIMARY KEY,
    ApprovalRequestId INT NOT NULL,
    ApproverLoginId NVARCHAR(100) NULL,
    ApproverRole NVARCHAR(100) NULL,
    ApproverLevel INT NULL,
    Status NVARCHAR(50) NOT NULL,
    ActionBy NVARCHAR(100) NULL,
    ActionDate DATETIME2 NULL
);

IF DB_NAME() <> N'AnmolDristi'
   OR @@SERVERNAME <> N'MUMANMOAPP01\SQLEXPRESS'
    INSERT INTO @Failure (ApprovalRequestId, Reason)
    VALUES (NULL, N'Connected session is not MUMANMOAPP01\SQLEXPRESS / AnmolDristi.');

IF EXISTS (SELECT 1 FROM @TargetIds WHERE ApprovalRequestId = 1240)
   OR (SELECT COUNT(*) FROM @TargetIds) <> 15
   OR (SELECT COUNT(*) FROM @QueueOwnerIds) <> 5
   OR (SELECT COUNT(*) FROM @LevelOneIds) <> 10
   OR EXISTS (
        SELECT 1
        FROM @QueueOwnerIds q
        INNER JOIN @LevelOneIds l ON l.ApprovalRequestId = q.ApprovalRequestId
      )
    INSERT INTO @Failure (ApprovalRequestId, Reason)
    VALUES (NULL, N'Approved ID lists are not the disjoint sets of 15, 10, and 5.');

IF NOT EXISTS (
    SELECT 1 FROM dbo.MST_UserMaster
    WHERE EmployeeCode = N'Audit' AND EmployeeStatus = N'Active'
)
    INSERT INTO @Failure (ApprovalRequestId, Reason)
    VALUES (NULL, N'Audit is missing or not Active.');

IF NOT EXISTS (
    SELECT 1 FROM dbo.MST_UserMaster
    WHERE EmployeeCode = N'AuditOne' AND EmployeeStatus = N'Active'
)
    INSERT INTO @Failure (ApprovalRequestId, Reason)
    VALUES (NULL, N'AuditOne is missing or not Active.');

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SalesApprovalRequestApprover')
      AND name = N'UX_SAR_Auditor_Once'
      AND is_unique = 1
      AND has_filter = 1
      AND filter_definition LIKE N'%ApproverRole%'
      AND filter_definition LIKE N'%Auditor%'
)
    INSERT INTO @Failure (ApprovalRequestId, Reason)
    VALUES (NULL, N'Unique filtered Auditor index UX_SAR_Auditor_Once was not found.');

INSERT INTO @Before
(
    ApprovalRequestId, RequesterLoginId, YearNo, MonthNo, RequestDate,
    RequestStatus, CurrentApproverLoginId, CurrentApproverLevel,
    RegionId, RegionMatchCount, RegionDescription,
    AuditorRowId, AuditorLogin, AuditorRole, AuditorLevel, AuditorStatus,
    AuditorActionBy, AuditorActionDate, ApproverRowCount, QueueGroup
)
SELECT
    r.ApprovalRequestId,
    r.RequesterLoginId,
    r.YearNo,
    r.MonthNo,
    r.RequestDate,
    r.Status,
    r.CurrentApproverLoginId,
    r.CurrentApproverLevel,
    u.RegionId,
    ISNULL(region.MatchCount, 0),
    region.RegionDescription,
    auditor.ApprovalRequestApproverId,
    auditor.ApproverLoginId,
    auditor.ApproverRole,
    auditor.ApproverLevel,
    auditor.Status,
    auditor.ActionBy,
    auditor.ActionDate,
    ISNULL(rows.ApproverRowCount, 0),
    CASE
        WHEN q.ApprovalRequestId IS NOT NULL THEN N'WithAuditor'
        ELSE N'LevelOne'
    END
FROM @TargetIds t
INNER JOIN dbo.SalesApprovalRequest r
    ON r.ApprovalRequestId = t.ApprovalRequestId
LEFT JOIN dbo.MST_UserMaster u
    ON u.EmployeeCode = r.RequesterLoginId
LEFT JOIN @QueueOwnerIds q
    ON q.ApprovalRequestId = t.ApprovalRequestId
OUTER APPLY (
    SELECT
        COUNT(*) AS MatchCount,
        MAX(reg.Region_Description) AS RegionDescription
    FROM dbo.MST_Region reg
    WHERE u.EmployeeCode IS NOT NULL
      AND reg.Region_ID = CONVERT(varchar(50), u.RegionId)
) region
OUTER APPLY (
    SELECT TOP (1)
        a.ApprovalRequestApproverId,
        a.ApproverLoginId,
        a.ApproverRole,
        a.ApproverLevel,
        a.Status,
        a.ActionBy,
        a.ActionDate
    FROM dbo.SalesApprovalRequestApprover a
    WHERE a.ApprovalRequestId = r.ApprovalRequestId
      AND a.ApproverRole = N'Auditor'
    ORDER BY a.ApprovalRequestApproverId
) auditor
OUTER APPLY (
    SELECT COUNT(*) AS ApproverRowCount
    FROM dbo.SalesApprovalRequestApprover a
    WHERE a.ApprovalRequestId = r.ApprovalRequestId
) rows;

INSERT INTO @BeforeOther
(
    ApprovalRequestApproverId, ApprovalRequestId, ApproverLoginId,
    ApproverRole, ApproverLevel, Status, ActionBy, ActionDate
)
SELECT
    a.ApprovalRequestApproverId,
    a.ApprovalRequestId,
    a.ApproverLoginId,
    a.ApproverRole,
    a.ApproverLevel,
    a.Status,
    a.ActionBy,
    a.ActionDate
FROM dbo.SalesApprovalRequestApprover a
INNER JOIN @TargetIds t
    ON t.ApprovalRequestId = a.ApprovalRequestId
WHERE a.ApproverRole <> N'Auditor';

INSERT INTO @Failure (ApprovalRequestId, Reason)
SELECT t.ApprovalRequestId, v.Reason
FROM @TargetIds t
LEFT JOIN @Before b
    ON b.ApprovalRequestId = t.ApprovalRequestId
LEFT JOIN @QueueOwnerIds q
    ON q.ApprovalRequestId = t.ApprovalRequestId
LEFT JOIN @LevelOneIds l
    ON l.ApprovalRequestId = t.ApprovalRequestId
OUTER APPLY (
    SELECT COUNT(*) AS AuditorRows
    FROM dbo.SalesApprovalRequestApprover a
    WHERE a.ApprovalRequestId = t.ApprovalRequestId
      AND a.ApproverRole = N'Auditor'
) auditorCount
CROSS APPLY (VALUES
    (CASE WHEN b.ApprovalRequestId IS NULL THEN N'Header not found.' END),
    (CASE WHEN b.ApprovalRequestId IS NOT NULL AND b.RequestStatus <> N'Pending' THEN N'Status is not Pending.' END),
    (CASE WHEN b.ApprovalRequestId IS NOT NULL AND (b.YearNo <> 2026 OR b.MonthNo <> 9) THEN N'Claim period is not September 2026.' END),
    (CASE WHEN b.ApprovalRequestId IS NOT NULL AND (b.RequestDate < CONVERT(datetime2, '20261001', 112) OR b.RequestDate >= CONVERT(datetime2, '20261101', 112)) THEN N'RequestDate is outside October 2026.' END),
    (CASE WHEN b.ApprovalRequestId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.MST_UserMaster u WHERE u.EmployeeCode = b.RequesterLoginId) THEN N'Requester is missing from MST_UserMaster.' END),
    (CASE WHEN b.ApprovalRequestId IS NOT NULL AND b.RegionMatchCount <> 1 THEN N'RegionId does not match exactly one MST_Region.Region_ID.' END),
    (CASE WHEN b.ApprovalRequestId IS NOT NULL AND b.RegionMatchCount = 1 AND ISNULL(LTRIM(RTRIM(b.RegionDescription)), N'') <> N'South' THEN N'Resolved region is not South.' END),
    (CASE WHEN ISNULL(auditorCount.AuditorRows, 0) <> 1 THEN N'Auditor row count is not exactly one.' END),
    (CASE WHEN b.ApprovalRequestId IS NOT NULL AND (b.AuditorRowId IS NULL OR ISNULL(b.AuditorLogin, N'') <> N'Audit' OR ISNULL(b.AuditorStatus, N'') <> N'Pending' OR b.AuditorActionBy IS NOT NULL OR b.AuditorActionDate IS NOT NULL) THEN N'Auditor row is not Pending Audit with null action fields.' END),
    (CASE WHEN q.ApprovalRequestId IS NOT NULL AND NOT (b.CurrentApproverLevel = 2 AND b.CurrentApproverLoginId = N'Audit') THEN N'Queue-owner request is not at level 2 with Audit.' END),
    (CASE WHEN l.ApprovalRequestId IS NOT NULL AND NOT (b.CurrentApproverLevel = 1 AND NULLIF(LTRIM(RTRIM(b.CurrentApproverLoginId)), N'') IS NOT NULL AND b.CurrentApproverLoginId <> N'Audit' AND b.CurrentApproverLoginId <> N'AuditOne') THEN N'Level-1 request is not awaiting its current approver at level 1.' END)
) v (Reason)
WHERE v.Reason IS NOT NULL;

SELECT
    N'Session' AS ResultSetName,
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    SYSDATETIMEOFFSET() AS SessionTime,
    @CommitChanges AS CommitChanges;

SELECT
    N'Accounts' AS ResultSetName,
    EmployeeCode,
    Id,
    EmployeeName,
    EmployeeStatus
FROM dbo.MST_UserMaster
WHERE EmployeeCode IN (N'Audit', N'AuditOne')
ORDER BY EmployeeCode;

SELECT
    N'AuditorConstraint' AS ResultSetName,
    i.name AS IndexName,
    i.is_unique,
    i.has_filter,
    i.filter_definition
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'dbo.SalesApprovalRequestApprover')
  AND i.name = N'UX_SAR_Auditor_Once';

SELECT
    N'Baseline' AS ResultSetName,
    b.ApprovalRequestId,
    b.RequesterLoginId,
    b.YearNo,
    b.MonthNo,
    b.RequestDate,
    b.RequestStatus,
    b.CurrentApproverLoginId,
    b.CurrentApproverLevel,
    b.RegionId,
    b.RegionDescription,
    b.AuditorRowId,
    b.AuditorLogin,
    b.AuditorRole,
    b.AuditorLevel,
    b.AuditorStatus,
    b.AuditorActionBy,
    b.AuditorActionDate,
    b.QueueGroup
FROM @Before b
ORDER BY b.ApprovalRequestId;

SELECT
    N'BaselineOtherApprovers' AS ResultSetName,
    o.ApprovalRequestId,
    o.ApprovalRequestApproverId,
    o.ApproverLoginId,
    o.ApproverRole,
    o.ApproverLevel,
    o.Status,
    o.ActionBy,
    o.ActionDate
FROM @BeforeOther o
ORDER BY o.ApprovalRequestId, o.ApproverLevel, o.ApprovalRequestApproverId;

SELECT
    N'ProposedChanges' AS ResultSetName,
    b.ApprovalRequestId,
    b.AuditorRowId,
    b.AuditorLogin AS AuditorLoginBefore,
    N'AuditOne' AS AuditorLoginAfter,
    b.CurrentApproverLoginId AS CurrentApproverBefore,
    CASE
        WHEN b.QueueGroup = N'WithAuditor' THEN N'AuditOne'
        ELSE b.CurrentApproverLoginId
    END AS CurrentApproverAfter,
    b.CurrentApproverLevel AS LevelBeforeAndAfter
FROM @Before b
ORDER BY b.ApprovalRequestId;

SELECT
    N'EligibilityFailures' AS ResultSetName,
    f.ApprovalRequestId,
    f.Reason
FROM @Failure f
ORDER BY f.ApprovalRequestId, f.Reason;

IF EXISTS (SELECT 1 FROM @Failure)
    THROW 52010, 'Eligibility failed. No transaction was started. See EligibilityFailures.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @LockedHeader TABLE
    (
        ApprovalRequestId INT NOT NULL PRIMARY KEY,
        RequesterLoginId NVARCHAR(100) NOT NULL,
        YearNo INT NOT NULL,
        MonthNo INT NOT NULL,
        RequestDate DATETIME2 NOT NULL,
        RequestStatus NVARCHAR(50) NOT NULL,
        CurrentApproverLoginId NVARCHAR(100) NULL,
        CurrentApproverLevel INT NULL
    );

    INSERT INTO @LockedHeader
    SELECT
        r.ApprovalRequestId,
        r.RequesterLoginId,
        r.YearNo,
        r.MonthNo,
        r.RequestDate,
        r.Status,
        r.CurrentApproverLoginId,
        r.CurrentApproverLevel
    FROM dbo.SalesApprovalRequest r WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN @TargetIds t
        ON t.ApprovalRequestId = r.ApprovalRequestId;

    DECLARE @LockedApprover TABLE
    (
        ApprovalRequestApproverId INT NOT NULL PRIMARY KEY,
        ApprovalRequestId INT NOT NULL,
        ApproverLoginId NVARCHAR(100) NULL,
        ApproverRole NVARCHAR(100) NULL,
        ApproverLevel INT NULL,
        Status NVARCHAR(50) NOT NULL,
        ActionBy NVARCHAR(100) NULL,
        ActionDate DATETIME2 NULL
    );

    INSERT INTO @LockedApprover
    SELECT
        a.ApprovalRequestApproverId,
        a.ApprovalRequestId,
        a.ApproverLoginId,
        a.ApproverRole,
        a.ApproverLevel,
        a.Status,
        a.ActionBy,
        a.ActionDate
    FROM dbo.SalesApprovalRequestApprover a WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN @TargetIds t
        ON t.ApprovalRequestId = a.ApprovalRequestId;

    IF (SELECT COUNT(*) FROM @LockedHeader) <> 15
       OR (SELECT COUNT(*) FROM @Before) <> 15
       OR EXISTS (
            SELECT b.ApprovalRequestId, b.RequesterLoginId, b.YearNo, b.MonthNo, b.RequestDate, b.RequestStatus, b.CurrentApproverLoginId, b.CurrentApproverLevel
            FROM @Before b
            EXCEPT
            SELECT h.ApprovalRequestId, h.RequesterLoginId, h.YearNo, h.MonthNo, h.RequestDate, h.RequestStatus, h.CurrentApproverLoginId, h.CurrentApproverLevel
            FROM @LockedHeader h
          )
       OR EXISTS (
            SELECT b.AuditorRowId, b.ApprovalRequestId, b.AuditorLogin, b.AuditorRole, b.AuditorLevel, b.AuditorStatus, b.AuditorActionBy, b.AuditorActionDate
            FROM @Before b
            EXCEPT
            SELECT a.ApprovalRequestApproverId, a.ApprovalRequestId, a.ApproverLoginId, a.ApproverRole, a.ApproverLevel, a.Status, a.ActionBy, a.ActionDate
            FROM @LockedApprover a
            WHERE a.ApproverRole = N'Auditor'
          )
       OR EXISTS (
            SELECT ApprovalRequestApproverId, ApprovalRequestId, ApproverLoginId, ApproverRole, ApproverLevel, Status, ActionBy, ActionDate
            FROM @BeforeOther
            EXCEPT
            SELECT ApprovalRequestApproverId, ApprovalRequestId, ApproverLoginId, ApproverRole, ApproverLevel, Status, ActionBy, ActionDate
            FROM @LockedApprover
            WHERE ApproverRole <> N'Auditor'
          )
       OR EXISTS (
            SELECT ApprovalRequestApproverId, ApprovalRequestId, ApproverLoginId, ApproverRole, ApproverLevel, Status, ActionBy, ActionDate
            FROM @LockedApprover
            WHERE ApproverRole <> N'Auditor'
            EXCEPT
            SELECT ApprovalRequestApproverId, ApprovalRequestId, ApproverLoginId, ApproverRole, ApproverLevel, Status, ActionBy, ActionDate
            FROM @BeforeOther
          )
        THROW 52011, 'Locked rows no longer match the baseline. No update was applied.', 1;

    DECLARE @AuditorUpdates INT;

    UPDATE a
       SET a.ApproverLoginId = N'AuditOne'
    FROM dbo.SalesApprovalRequestApprover a
    INNER JOIN @Before b
        ON b.AuditorRowId = a.ApprovalRequestApproverId
       AND b.ApprovalRequestId = a.ApprovalRequestId
    WHERE a.ApproverRole = N'Auditor'
      AND a.ApproverLoginId = N'Audit'
      AND a.Status = N'Pending'
      AND a.ActionBy IS NULL
      AND a.ActionDate IS NULL
      AND a.ApprovalRequestId IN (SELECT ApprovalRequestId FROM @TargetIds);

    SET @AuditorUpdates = @@ROWCOUNT;

    DECLARE @QueueUpdates INT;

    UPDATE r
       SET r.CurrentApproverLoginId = N'AuditOne'
    FROM dbo.SalesApprovalRequest r
    INNER JOIN @QueueOwnerIds q
        ON q.ApprovalRequestId = r.ApprovalRequestId
    INNER JOIN @Before b
        ON b.ApprovalRequestId = r.ApprovalRequestId
       AND b.QueueGroup = N'WithAuditor'
    WHERE r.Status = N'Pending'
      AND r.CurrentApproverLoginId = N'Audit'
      AND r.CurrentApproverLevel = 2
      AND r.ApprovalRequestId NOT IN (SELECT ApprovalRequestId FROM @LevelOneIds);

    SET @QueueUpdates = @@ROWCOUNT;

    IF @AuditorUpdates <> 15 OR @QueueUpdates <> 5
    BEGIN
        SELECT
            N'UpdateCounts' AS ResultSetName,
            @AuditorUpdates AS AuditorLoginUpdates,
            @QueueUpdates AS QueueOwnerUpdates,
            15 AS ExpectedAuditorLoginUpdates,
            5 AS ExpectedQueueOwnerUpdates;
        THROW 52012, 'Update counts were not 15 Auditor rows and 5 queue-owner headers.', 1;
    END

    IF EXISTS (
        SELECT h.ApprovalRequestId
        FROM @LockedHeader h
        INNER JOIN @LevelOneIds l
            ON l.ApprovalRequestId = h.ApprovalRequestId
        INNER JOIN dbo.SalesApprovalRequest r
            ON r.ApprovalRequestId = h.ApprovalRequestId
        WHERE ISNULL(r.CurrentApproverLoginId, N'') <> ISNULL(h.CurrentApproverLoginId, N'')
           OR ISNULL(r.CurrentApproverLevel, -1) <> ISNULL(h.CurrentApproverLevel, -1)
           OR r.Status <> h.RequestStatus
           OR r.YearNo <> h.YearNo
           OR r.MonthNo <> h.MonthNo
           OR r.RequestDate <> h.RequestDate
           OR r.RequesterLoginId <> h.RequesterLoginId
    )
        THROW 52013, 'A protected level-1 header field changed.', 1;

    IF EXISTS (
        SELECT q.ApprovalRequestId
        FROM @QueueOwnerIds q
        INNER JOIN dbo.SalesApprovalRequest r
            ON r.ApprovalRequestId = q.ApprovalRequestId
        INNER JOIN @LockedHeader h
            ON h.ApprovalRequestId = q.ApprovalRequestId
        WHERE r.CurrentApproverLoginId <> N'AuditOne'
           OR r.CurrentApproverLevel <> h.CurrentApproverLevel
           OR r.CurrentApproverLevel <> 2
           OR r.Status <> h.RequestStatus
           OR r.YearNo <> h.YearNo
           OR r.MonthNo <> h.MonthNo
           OR r.RequestDate <> h.RequestDate
    )
        THROW 52014, 'A queue-owner header did not keep level 2 and Pending status.', 1;

    IF EXISTS (
        SELECT a.ApprovalRequestId
        FROM dbo.SalesApprovalRequestApprover a
        INNER JOIN @Before b
            ON b.AuditorRowId = a.ApprovalRequestApproverId
        WHERE a.ApproverLoginId <> N'AuditOne'
           OR a.ApproverRole <> b.AuditorRole
           OR ISNULL(a.ApproverLevel, -1) <> ISNULL(b.AuditorLevel, -1)
           OR a.Status <> b.AuditorStatus
           OR a.ActionBy IS NOT NULL
           OR a.ActionDate IS NOT NULL
    )
        THROW 52015, 'An Auditor row changed a field other than ApproverLoginId.', 1;

    IF EXISTS (
        SELECT b.ApprovalRequestApproverId
        FROM @BeforeOther b
        INNER JOIN dbo.SalesApprovalRequestApprover a
            ON a.ApprovalRequestApproverId = b.ApprovalRequestApproverId
        WHERE ISNULL(a.ApproverLoginId, N'') <> ISNULL(b.ApproverLoginId, N'')
           OR ISNULL(a.ApproverRole, N'') <> ISNULL(b.ApproverRole, N'')
           OR ISNULL(a.ApproverLevel, -1) <> ISNULL(b.ApproverLevel, -1)
           OR a.Status <> b.Status
           OR ISNULL(a.ActionBy, N'') <> ISNULL(b.ActionBy, N'')
           OR ISNULL(a.ActionDate, CONVERT(datetime2, '19000101', 112)) <> ISNULL(b.ActionDate, CONVERT(datetime2, '19000101', 112))
    )
        THROW 52016, 'A non-auditor approver row changed.', 1;

    IF EXISTS (
        SELECT r.ApprovalRequestId
        FROM dbo.SalesApprovalRequest r
        INNER JOIN @TargetIds t
            ON t.ApprovalRequestId = r.ApprovalRequestId
        INNER JOIN @Before b
            ON b.ApprovalRequestId = r.ApprovalRequestId
        CROSS APPLY (
            SELECT COUNT(*) AS ApproverRowCount
            FROM dbo.SalesApprovalRequestApprover a
            WHERE a.ApprovalRequestId = r.ApprovalRequestId
        ) rows
        CROSS APPLY (
            SELECT COUNT(*) AS AuditorRows
            FROM dbo.SalesApprovalRequestApprover a
            WHERE a.ApprovalRequestId = r.ApprovalRequestId
              AND a.ApproverRole = N'Auditor'
        ) auditorRows
        WHERE rows.ApproverRowCount <> b.ApproverRowCount
           OR auditorRows.AuditorRows <> 1
    )
        THROW 52017, 'Approver-row counts changed.', 1;

    SELECT
        N'UpdateCounts' AS ResultSetName,
        @AuditorUpdates AS AuditorLoginUpdates,
        @QueueUpdates AS QueueOwnerUpdates,
        15 AS ExpectedAuditorLoginUpdates,
        5 AS ExpectedQueueOwnerUpdates;

    SELECT
        N'AfterSnapshotInsideTransaction' AS ResultSetName,
        r.ApprovalRequestId,
        r.RequesterLoginId,
        r.YearNo,
        r.MonthNo,
        r.RequestDate,
        r.Status AS RequestStatus,
        r.CurrentApproverLoginId,
        r.CurrentApproverLevel,
        a.ApprovalRequestApproverId AS AuditorRowId,
        a.ApproverLoginId AS AuditorLogin,
        a.ApproverRole AS AuditorRole,
        a.ApproverLevel AS AuditorLevel,
        a.Status AS AuditorStatus,
        a.ActionBy AS AuditorActionBy,
        a.ActionDate AS AuditorActionDate
    FROM dbo.SalesApprovalRequest r
    INNER JOIN dbo.SalesApprovalRequestApprover a
        ON a.ApprovalRequestId = r.ApprovalRequestId
       AND a.ApproverRole = N'Auditor'
    WHERE r.ApprovalRequestId IN (SELECT ApprovalRequestId FROM @TargetIds)
    ORDER BY r.ApprovalRequestId;

    SELECT
        N'ValidationOutcome' AS ResultSetName,
        @AuditorUpdates AS AuditorLoginUpdates,
        @QueueUpdates AS QueueOwnerUpdates,
        CASE WHEN @AuditorUpdates = 15 AND @QueueUpdates = 5 THEN 1 ELSE 0 END AS CountsMatch,
        CASE WHEN @CommitChanges = 1 THEN N'Assertions passed. Commit requested.' ELSE N'Assertions passed. Preview will roll back.' END AS ValidationMessage;

    IF @CommitChanges = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'COMMITTED. 15 Auditor logins and 5 queue-owner logins were saved.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'ROLLED BACK (preview). Set @CommitChanges = 1 only after reviewing these result sets.';
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
    r.Status AS RequestStatus,
    r.CurrentApproverLoginId,
    r.CurrentApproverLevel,
    a.ApprovalRequestApproverId AS AuditorRowId,
    a.ApproverLoginId AS AuditorLogin,
    a.Status AS AuditorStatus,
    a.ActionBy AS AuditorActionBy,
    a.ActionDate AS AuditorActionDate
FROM dbo.SalesApprovalRequest r
INNER JOIN dbo.SalesApprovalRequestApprover a
    ON a.ApprovalRequestId = r.ApprovalRequestId
   AND a.ApproverRole = N'Auditor'
WHERE r.ApprovalRequestId IN (SELECT ApprovalRequestId FROM @TargetIds)
ORDER BY r.ApprovalRequestId;
