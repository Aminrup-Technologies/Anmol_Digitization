/*
    UAT only. Adds four activity counters to dbo.usp_RecalculateApprovalRequest.
    Database: AnmolDristiDev only.
    Server:   MUMANMOAPP01\SQLEXPRESS only.
    Live catalog AnmolDristi is forbidden.
    This script alters that one procedure. It does not alter
    dbo.usp_RecalculateSalesClaim, tables, claims, or any other module.
    Database/Deployments/L2R19_UAT is not run by this script.

    @Execute must stay 0 in the repository copy.
    @Execute = 0 reads the catalog and returns before any change.
    @Execute = 1 applies the candidate only after the live module matches
    the embedded preimage exactly.

    The embedded preimage is the repository procedure at HEAD
    98947ebcebdd8e5db8da14029ae4ecb94a8445a3, which is also the
    @RecalcCatalog body in
    Database/Deployments/L2R19_UAT/B_Apply_L2R19_Finalization_UAT.sql.
    Its catalog SHA-256 was calculated in the repository. It was not read
    from UAT. The 2026-10-13 rollback preimage hash
    D14FEF7DFF5F8C4E008B2485D445B897CA4B76FBB60D6E5CEF68745894BE5A9D
    is a different module. This script aborts when the live module is that
    body or any other unexpected definition.

    Catalog comparison uses HASHBYTES('SHA2_256', CONVERT(varbinary(max), module))
    over UTF-16LE with no byte-order mark. This procedure is stored as
    CREATE PROCEDURE with one space. Leading comments, CRLF line endings,
    and that single space stay significant.

    Read-only preimage query, for an operator, before setting @Execute = 1:

    SELECT
          LEN(m.definition) AS module_len
        , DATALENGTH(m.definition) AS module_bytes
        , CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), m.definition)), 2) AS sha256_utf16le
    FROM sys.sql_modules AS m
    WHERE m.object_id = OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest', N'P');

    Post-deployment query:

    SELECT
          LEN(m.definition) AS module_len
        , DATALENGTH(m.definition) AS module_bytes
        , CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), m.definition)), 2) AS sha256_utf16le
    FROM sys.sql_modules AS m
    WHERE m.object_id = OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest', N'P');
    -- Expected sha256_utf16le: 6FF6F1F59420DE3684ADC54EB3CBDBB14C46A97B128AC27D0C9F88C1B4CBE094
    -- Expected LEN: 8745
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Execute bit = 0;
DECLARE @ExpectedPrevHash varchar(64) = '3ECC03CBD6C1AFF40CF7F6B7ECF5E637399A7D07B4A6AA4AB1FB219231F7900F';
DECLARE @ExpectedNextHash varchar(64) = '6FF6F1F59420DE3684ADC54EB3CBDBB14C46A97B128AC27D0C9F88C1B4CBE094';

IF @@TRANCOUNT <> 0
    THROW 59401, N'ABORT: this script must start with no open transaction.', 1;
IF CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName')) <> N'MUMANMOAPP01\SQLEXPRESS'
    THROW 59402, N'ABORT: server must be MUMANMOAPP01\SQLEXPRESS.', 1;
IF DB_NAME() = N'AnmolDristi'
    THROW 59403, N'ABORT: Live catalog AnmolDristi is forbidden.', 1;
IF DB_NAME() <> N'AnmolDristiDev'
    THROW 59404, N'ABORT: catalog must be AnmolDristiDev.', 1;

DECLARE @Prev nvarchar(max) = N'/* ============================================================================
   NAME:        fsot_request_canonical_totals
   WHEN:        2026-09-15
   WHY:         Request approved totals were recomputed in C# from multiple formulas
   WHAT:        usp_RecalculateApprovalRequest refreshes latest day snapshots and writes header Approved* + FinalApprovedAmount
   ============================================================================ */

/* ============================================================================
   NAME:        l2r19_recalculate_participates_in_caller_transaction
   WHEN:        2026-10-13
   WHY:         RAISERROR plus RETURN does not stop a caller transaction
   WHAT:        The two guard failures THROW. ReadyForPayment returns without rewriting totals. The snapshot and Approved* formula is unchanged.
   ============================================================================ */

/*
    Object:      [dbo].[usp_RecalculateApprovalRequest]
    Type:        STORED PROCEDURE
    Purpose:     FSOT request-level roll-up from latest SalesApprovalRequestDetail snapshots
*/

CREATE PROCEDURE dbo.usp_RecalculateApprovalRequest
(
    @ApprovalRequestId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @ApprovalRequestId IS NULL
        THROW 59121, ''ApprovalRequestId cannot be NULL.'', 1;

    DECLARE @HeaderStatus NVARCHAR(50);

    SELECT @HeaderStatus = Status
    FROM dbo.SalesApprovalRequest
    WHERE ApprovalRequestId = @ApprovalRequestId;

    IF @HeaderStatus IS NULL
        THROW 59122, ''Approval Request not found.'', 1;

    /* Payment lock. Mark Ready calls this procedure before it writes ReadyForPayment. */
    IF @HeaderStatus = N''ReadyForPayment''
        RETURN;

    /* Latest detail per activity: MAX(DetailVersion) then MAX(ApprovalRequestDetailId) — RC-012 */
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
    UPDATE d
    SET
        Snapshot_DAAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = ''DA''
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''
        ), 0),
        Snapshot_HotelAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = ''Hotel''
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''
        ), 0),
        Snapshot_TravelAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = ''Travel''
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''
        ), 0),
        Snapshot_StationaryAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = ''Stationary''
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''
        ), 0),
        ModifiedOn = SYSUTCDATETIME()
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN Latest l
        ON d.ApprovalRequestDetailId = l.LatestDetailId
    WHERE d.ApprovalRequestId = @ApprovalRequestId;

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
    UPDATE d
    SET Snapshot_ExpenseTotal =
          ISNULL(d.Snapshot_DAAmount, 0)
        + ISNULL(d.Snapshot_HotelAmount, 0)
        + ISNULL(d.Snapshot_TravelAmount, 0)
        + ISNULL(d.Snapshot_StationaryAmount, 0)
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN Latest l
        ON d.ApprovalRequestDetailId = l.LatestDetailId
    WHERE d.ApprovalRequestId = @ApprovalRequestId;

    DECLARE
          @ApprovedKM              DECIMAL(18,2) = 0
        , @ApprovedClaimAmount     DECIMAL(18,2) = 0
        , @ApprovedDAAmount        DECIMAL(18,2) = 0
        , @ApprovedHotelAmount     DECIMAL(18,2) = 0
        , @ApprovedTravelAmount    DECIMAL(18,2) = 0
        , @FinalApprovedAmount     DECIMAL(18,2) = 0;

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
          @ApprovedKM           = ISNULL(SUM(d.Snapshot_TotalDistanceKM), 0)
        , @ApprovedClaimAmount  = ISNULL(SUM(d.Snapshot_CalculatedClaimAmount), 0)
        , @ApprovedDAAmount     = ISNULL(SUM(d.Snapshot_DAAmount), 0)
        , @ApprovedHotelAmount  = ISNULL(SUM(d.Snapshot_HotelAmount), 0)
        , @ApprovedTravelAmount = ISNULL(SUM(d.Snapshot_TravelAmount), 0)
        , @FinalApprovedAmount  = ISNULL(SUM(
              ISNULL(d.Snapshot_CalculatedClaimAmount, 0)
            + ISNULL(d.Snapshot_ExpenseTotal, 0)
          ), 0)
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN Latest l
        ON d.ApprovalRequestDetailId = l.LatestDetailId
    WHERE d.ApprovalRequestId = @ApprovalRequestId
      AND ISNULL(d.DetailStatus, '''') NOT IN (''Rejected'', ''Returned'');

    /* ApprovedExpenseAmount / TotalApprovedAmount are computed from components (no Stationary column yet).
       FinalApprovedAmount is the FSOT Net Payable (Claim + four-part ExpenseTotal). */
    UPDATE dbo.SalesApprovalRequest
    SET
          ApprovedKM           = @ApprovedKM
        , ApprovedClaimAmount  = @ApprovedClaimAmount
        , ApprovedDAAmount     = @ApprovedDAAmount
        , ApprovedHotelAmount  = @ApprovedHotelAmount
        , ApprovedTravelAmount = @ApprovedTravelAmount
        , FinalApprovedAmount  = @FinalApprovedAmount
        , ModifiedOn           = SYSUTCDATETIME()
    WHERE ApprovalRequestId = @ApprovalRequestId;
END
';
DECLARE @Next nvarchar(max) = N'/* ============================================================================
   NAME:        fsot_request_canonical_totals
   WHEN:        2026-09-15
   WHY:         Request approved totals were recomputed in C# from multiple formulas
   WHAT:        usp_RecalculateApprovalRequest refreshes latest day snapshots and writes header Approved* + FinalApprovedAmount
   ============================================================================ */

/* ============================================================================
   NAME:        l2r19_recalculate_participates_in_caller_transaction
   WHEN:        2026-10-13
   WHY:         RAISERROR plus RETURN does not stop a caller transaction
   WHAT:        The two guard failures THROW. ReadyForPayment returns without rewriting totals. The snapshot and Approved* formula is unchanged.
   ============================================================================ */

/*
    Object:      [dbo].[usp_RecalculateApprovalRequest]
    Type:        STORED PROCEDURE
    Purpose:     FSOT request-level roll-up from latest SalesApprovalRequestDetail snapshots
*/

CREATE PROCEDURE dbo.usp_RecalculateApprovalRequest
(
    @ApprovalRequestId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @ApprovalRequestId IS NULL
        THROW 59121, ''ApprovalRequestId cannot be NULL.'', 1;

    DECLARE @HeaderStatus NVARCHAR(50);

    SELECT @HeaderStatus = Status
    FROM dbo.SalesApprovalRequest
    WHERE ApprovalRequestId = @ApprovalRequestId;

    IF @HeaderStatus IS NULL
        THROW 59122, ''Approval Request not found.'', 1;

    /* Payment lock. Mark Ready calls this procedure before it writes ReadyForPayment. */
    IF @HeaderStatus = N''ReadyForPayment''
        RETURN;

    /* Latest detail per activity: MAX(DetailVersion) then MAX(ApprovalRequestDetailId) — RC-012 */
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
    UPDATE d
    SET
        Snapshot_DAAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = ''DA''
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''
        ), 0),
        Snapshot_HotelAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = ''Hotel''
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''
        ), 0),
        Snapshot_TravelAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = ''Travel''
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''
        ), 0),
        Snapshot_StationaryAmount = ISNULL((
            SELECT SUM(e.Amount)
            FROM dbo.SalesDailyExpense e
            WHERE e.ActivityId = d.ActivityId
              AND e.ExpenseType = ''Stationary''
              AND e.IsDeleted = 0
              AND ISNULL(e.ClaimStatus, '''') <> ''Rejected''
        ), 0),
        ModifiedOn = SYSUTCDATETIME()
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN Latest l
        ON d.ApprovalRequestDetailId = l.LatestDetailId
    WHERE d.ApprovalRequestId = @ApprovalRequestId;

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
    UPDATE d
    SET Snapshot_ExpenseTotal =
          ISNULL(d.Snapshot_DAAmount, 0)
        + ISNULL(d.Snapshot_HotelAmount, 0)
        + ISNULL(d.Snapshot_TravelAmount, 0)
        + ISNULL(d.Snapshot_StationaryAmount, 0)
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN Latest l
        ON d.ApprovalRequestDetailId = l.LatestDetailId
    WHERE d.ApprovalRequestId = @ApprovalRequestId;

    DECLARE
          @ApprovedKM              DECIMAL(18,2) = 0
        , @ApprovedClaimAmount     DECIMAL(18,2) = 0
        , @ApprovedDAAmount        DECIMAL(18,2) = 0
        , @ApprovedHotelAmount     DECIMAL(18,2) = 0
        , @ApprovedTravelAmount    DECIMAL(18,2) = 0
        , @FinalApprovedAmount     DECIMAL(18,2) = 0
        , @TotalActivities         INT = 0
        , @PendingActivities       INT = 0
        , @ApprovedActivities      INT = 0
        , @RejectedActivities      INT = 0;

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
          @ApprovedKM           = ISNULL(SUM(d.Snapshot_TotalDistanceKM), 0)
        , @ApprovedClaimAmount  = ISNULL(SUM(d.Snapshot_CalculatedClaimAmount), 0)
        , @ApprovedDAAmount     = ISNULL(SUM(d.Snapshot_DAAmount), 0)
        , @ApprovedHotelAmount  = ISNULL(SUM(d.Snapshot_HotelAmount), 0)
        , @ApprovedTravelAmount = ISNULL(SUM(d.Snapshot_TravelAmount), 0)
        , @FinalApprovedAmount  = ISNULL(SUM(
              ISNULL(d.Snapshot_CalculatedClaimAmount, 0)
            + ISNULL(d.Snapshot_ExpenseTotal, 0)
          ), 0)
    FROM dbo.SalesApprovalRequestDetail d
    INNER JOIN Latest l
        ON d.ApprovalRequestDetailId = l.LatestDetailId
    WHERE d.ApprovalRequestId = @ApprovalRequestId
      AND ISNULL(d.DetailStatus, '''') NOT IN (''Rejected'', ''Returned'');

    ;WITH LatestDetail AS
    (
        SELECT
              d.DetailStatus
            , ROW_NUMBER() OVER (
                  PARTITION BY d.ActivityId
                  ORDER BY ISNULL(d.DetailVersion, 1) DESC, d.ApprovalRequestDetailId DESC
              ) AS RN
        FROM dbo.SalesApprovalRequestDetail d
        WHERE d.ApprovalRequestId = @ApprovalRequestId
    )
    SELECT
          @TotalActivities      = COUNT(*)
        , @PendingActivities    = ISNULL(SUM(CASE WHEN DetailStatus = ''Pending''  THEN 1 ELSE 0 END), 0)
        , @ApprovedActivities   = ISNULL(SUM(CASE WHEN DetailStatus = ''Approved'' THEN 1 ELSE 0 END), 0)
        , @RejectedActivities   = ISNULL(SUM(CASE WHEN DetailStatus = ''Rejected'' THEN 1 ELSE 0 END), 0)
    FROM LatestDetail
    WHERE RN = 1;

    /* ApprovedExpenseAmount / TotalApprovedAmount are computed from components (no Stationary column yet).
       FinalApprovedAmount is the FSOT Net Payable (Claim + four-part ExpenseTotal). */
    UPDATE dbo.SalesApprovalRequest
    SET
          ApprovedKM           = @ApprovedKM
        , ApprovedClaimAmount  = @ApprovedClaimAmount
        , ApprovedDAAmount     = @ApprovedDAAmount
        , ApprovedHotelAmount  = @ApprovedHotelAmount
        , ApprovedTravelAmount = @ApprovedTravelAmount
        , FinalApprovedAmount  = @FinalApprovedAmount
        , TotalActivities      = @TotalActivities
        , PendingActivities    = @PendingActivities
        , ApprovedActivities   = @ApprovedActivities
        , RejectedActivities   = @RejectedActivities
        , ModifiedOn           = SYSUTCDATETIME()
    WHERE ApprovalRequestId = @ApprovalRequestId;
END
';

IF LEN(@Prev) <> 7557 OR DATALENGTH(@Prev) <> 15114
    OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Prev)), 2) <> @ExpectedPrevHash
    THROW 59405, N'ABORT: embedded preimage is not the reviewed repository fingerprint. Nothing was changed.', 1;
IF LEN(@Next) <> 8745 OR DATALENGTH(@Next) <> 17490
    OR CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Next)), 2) <> @ExpectedNextHash
    THROW 59406, N'ABORT: embedded candidate is not the reviewed fingerprint. Nothing was changed.', 1;
IF (LEN(@Prev) - LEN(REPLACE(@Prev, N'CREATE PROCEDURE', N''))) / LEN(N'CREATE PROCEDURE') <> 1
    OR (LEN(@Next) - LEN(REPLACE(@Next, N'CREATE PROCEDURE', N''))) / LEN(N'CREATE PROCEDURE') <> 1
    OR CHARINDEX(N'CREATE OR ALTER PROCEDURE', @Prev) > 0
    OR CHARINDEX(N'CREATE OR ALTER PROCEDURE', @Next) > 0
    OR CHARINDEX(N'CREATE   PROCEDURE', @Prev) > 0
    OR CHARINDEX(N'CREATE   PROCEDURE', @Next) > 0
    THROW 59407, N'ABORT: embedded text does not contain exactly one CREATE PROCEDURE token. Nothing was changed.', 1;
IF CHARINDEX(N'@TotalActivities', @Prev) > 0
    OR CHARINDEX(N'@PendingActivities', @Prev) > 0
    OR CHARINDEX(N'@ApprovedActivities', @Prev) > 0
    OR CHARINDEX(N'@RejectedActivities', @Prev) > 0
    OR CHARINDEX(N'IF @HeaderStatus = N''ReadyForPayment''', @Prev) = 0
    OR CHARINDEX(N'IF @HeaderStatus = N''ReadyForPayment''', @Next) = 0
    OR CHARINDEX(N'ORDER BY ISNULL(d.DetailVersion, 1) DESC, d.ApprovalRequestDetailId DESC', @Next) = 0
    OR (LEN(@Next) - LEN(REPLACE(@Next, N'TotalActivities      = @TotalActivities', N''))) / LEN(N'TotalActivities      = @TotalActivities') <> 1
    OR (LEN(@Next) - LEN(REPLACE(@Next, N'PendingActivities    = @PendingActivities', N''))) / LEN(N'PendingActivities    = @PendingActivities') <> 1
    OR (LEN(@Next) - LEN(REPLACE(@Next, N'ApprovedActivities   = @ApprovedActivities', N''))) / LEN(N'ApprovedActivities   = @ApprovedActivities') <> 1
    OR (LEN(@Next) - LEN(REPLACE(@Next, N'RejectedActivities   = @RejectedActivities', N''))) / LEN(N'RejectedActivities   = @RejectedActivities') <> 1
    THROW 59408, N'ABORT: embedded texts do not match the approved counter correction. Nothing was changed.', 1;

DECLARE @Live nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest', N'P'));
IF @Live IS NULL
    THROW 59409, N'ABORT: dbo.usp_RecalculateApprovalRequest is missing.', 1;
DECLARE @RecalcClaim nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RecalculateSalesClaim', N'P'));
IF @RecalcClaim IS NULL
    THROW 59410, N'ABORT: dbo.usp_RecalculateSalesClaim is missing. It was not modified.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns AS c INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SalesApprovalRequest')
      AND c.name = N'TotalActivities' AND ty.name = N'int' AND c.is_nullable = 0
) OR NOT EXISTS (
    SELECT 1 FROM sys.columns AS c INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SalesApprovalRequest')
      AND c.name = N'PendingActivities' AND ty.name = N'int' AND c.is_nullable = 0
) OR NOT EXISTS (
    SELECT 1 FROM sys.columns AS c INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SalesApprovalRequest')
      AND c.name = N'ApprovedActivities' AND ty.name = N'int' AND c.is_nullable = 0
) OR NOT EXISTS (
    SELECT 1 FROM sys.columns AS c INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.SalesApprovalRequest')
      AND c.name = N'RejectedActivities' AND ty.name = N'int' AND c.is_nullable = 0
)
    THROW 59413, N'ABORT: a header activity counter is not int NOT NULL. Nothing was changed.', 1;

DECLARE @Module nvarchar(max) = REPLACE(@Live, N'ALTER PROCEDURE', N'CREATE PROCEDURE');
DECLARE @ModuleHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Module)), 2);
DECLARE @LiveHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Live)), 2);
PRINT N'CAPTURE | usp_RecalculateApprovalRequest LEN='
    + CONVERT(nvarchar(20), LEN(@Live))
    + N' SHA2_256='
    + @LiveHash;
PRINT N'CAPTURE | usp_RecalculateSalesClaim LEN='
    + CONVERT(nvarchar(20), LEN(@RecalcClaim))
    + N' SHA2_256='
    + CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @RecalcClaim)), 2)
    + N' | definition was not changed.';

IF @Module = @Next AND @ModuleHash = @ExpectedNextHash AND LEN(@Module) = LEN(@Next)
BEGIN
    PRINT N'ALREADY APPLIED | Activity-counter candidate is already stored. No change was made.';
    RETURN;
END;
IF @Module <> @Prev OR @ModuleHash <> @ExpectedPrevHash OR LEN(@Module) <> LEN(@Prev)
BEGIN
    DECLARE @Abort nvarchar(2048) =
        N'ABORT: stored procedure is not the repository HEAD preimage. Nothing was changed. Live SHA2_256='
        + CONVERT(nvarchar(64), @LiveHash)
        + N' LEN='
        + CONVERT(nvarchar(20), LEN(@Live));
    THROW 59411, @Abort, 1;
END;

IF @Execute <> 1
BEGIN
    PRINT N'PREFLIGHT | Live module matches the repository HEAD preimage. @Execute = 0. No database change was made.';
    RETURN;
END;

BEGIN TRAN;

DECLARE @Alter nvarchar(max) = REPLACE(@Next, N'CREATE PROCEDURE', N'ALTER PROCEDURE');
EXEC sys.sp_executesql @Alter;

DECLARE @Stored nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RecalculateApprovalRequest', N'P'));
DECLARE @StoredModule nvarchar(max) = REPLACE(@Stored, N'ALTER PROCEDURE', N'CREATE PROCEDURE');
DECLARE @StoredHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @StoredModule)), 2);
IF @StoredModule IS NULL
    OR @StoredModule <> @Next
    OR @StoredHash <> @ExpectedNextHash
    OR LEN(@StoredModule) <> 8745
    OR (LEN(@StoredModule) - LEN(REPLACE(@StoredModule, N'CREATE PROCEDURE', N''))) / LEN(N'CREATE PROCEDURE') <> 1
BEGIN
    ROLLBACK TRAN;
    THROW 59414, N'ABORT: stored procedure did not match the candidate. Transaction rolled back.', 1;
END;

DECLARE @RecalcClaimAfter nvarchar(max) = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_RecalculateSalesClaim', N'P'));
IF @RecalcClaimAfter IS NULL OR @RecalcClaimAfter <> @RecalcClaim
BEGIN
    ROLLBACK TRAN;
    THROW 59415, N'ABORT: dbo.usp_RecalculateSalesClaim changed. Transaction rolled back.', 1;
END;

COMMIT TRAN;
PRINT N'COMMITTED | usp_RecalculateApprovalRequest LEN='
    + CONVERT(nvarchar(20), LEN(@StoredModule))
    + N' SHA2_256='
    + @StoredHash
    + N'. usp_RecalculateSalesClaim was not modified. SHA2_256='
    + CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @RecalcClaimAfter)), 2);