/*
    Package:     Database/Deployments/MarkReadyAuditorRow
    Script:      R121_Recover_Approver238.sql
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN (AnmolDristi)
    Mode:        PREPARED — do not execute. Data correction is not authorized.

    Corrects only SalesApprovalRequestApprover row 238.
    Does not call dbo.usp_MarkRequestReadyForPayment.
    Does not change the header, history, details, expenses, totals,
    remarks, or any other approver row.

    Observed on AnmolDristiDev at 2026-10-12, and re-checked inside the
    transaction if this script is later authorized:

      Header 121                         ReadyForPayment
      ModifiedBy                         Audit
      ModifiedOn                         2026-10-12 07:07:16.4216939
      History 797                        Pending -> Completed
                                         remark Auditor finalized all rows before Ready for Payment.
                                         ChangedOn 2026-10-12 07:07:16.1926027
      History 798                        Completed -> ReadyForPayment
                                         ChangedBy Audit
                                         remark Auditor marked Ready for Payment
                                         ChangedOn 2026-10-12 07:07:16.2551270
      Approver 238                       request 121, Audit, Auditor, level 2, Pending
                                         ActionBy null, ActionDate null, Remarks null
      Approver 237                       left unchanged

    History 797 is an additional guard. The correction itself reads
    ActionDate from history 798.

    @Execute defaults to 0: no read, no lock, no write.
    @Commit defaults to 0: a later rehearsal that passes every check
    still rolls back.
    A second run, after a committed correction, finds the post-state and
    writes nothing.

    Both gates stay 0 until a separate DBA authorization.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Execute bit = 0;
DECLARE @Commit bit = 0;

IF @Execute <> 1
BEGIN
    PRINT N'NOT RUN | @Execute is 0. Approver 238 was not read or updated.';
    RETURN;
END;

BEGIN TRY

    IF @@TRANCOUNT <> 0
        THROW 58341, N'ABORT: this script must start with no open transaction. No row updated.', 1;

    IF DB_NAME() = N'AnmolDristi'
        THROW 58301, N'ABORT: Live database AnmolDristi is forbidden. No row updated.', 1;

    IF UPPER(CONVERT(nvarchar(128), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
        THROW 58304, N'ABORT: server is not MUMANMOAPP01\SQLEXPRESS. No row updated.', 1;

    IF DB_NAME() <> N'AnmolDristiDev'
        THROW 58302, N'ABORT: database is not AnmolDristiDev. No row updated.', 1;

    BEGIN TRAN;

    DECLARE @Status nvarchar(50);
    DECLARE @ModifiedBy nvarchar(100);
    DECLARE @ModifiedOn datetime2(7);
    DECLARE @TotalKM decimal(12, 2);
    DECLARE @TotalClaimAmount decimal(18, 2);
    DECLARE @TotalDAAmount decimal(18, 2);
    DECLARE @TotalHotelAmount decimal(18, 2);
    DECLARE @TotalTravelAmount decimal(18, 2);
    DECLARE @TotalExpenseAmount decimal(18, 2);
    DECLARE @ApprovedDAAmount decimal(18, 2);
    DECLARE @ApprovedHotelAmount decimal(18, 2);
    DECLARE @ApprovedTravelAmount decimal(18, 2);
    DECLARE @ApprovedKM decimal(12, 2);
    DECLARE @ApprovedClaimAmount decimal(18, 2);
    DECLARE @TotalStationaryAmount decimal(18, 2);
    DECLARE @FinalApprovedAmount decimal(18, 2);
    DECLARE @TotalDeductionAmount decimal(18, 2);

    SELECT
        @Status = Status,
        @ModifiedBy = ModifiedBy,
        @ModifiedOn = ModifiedOn,
        @TotalKM = TotalKM,
        @TotalClaimAmount = TotalClaimAmount,
        @TotalDAAmount = TotalDAAmount,
        @TotalHotelAmount = TotalHotelAmount,
        @TotalTravelAmount = TotalTravelAmount,
        @TotalExpenseAmount = TotalExpenseAmount,
        @ApprovedDAAmount = ApprovedDAAmount,
        @ApprovedHotelAmount = ApprovedHotelAmount,
        @ApprovedTravelAmount = ApprovedTravelAmount,
        @ApprovedKM = ApprovedKM,
        @ApprovedClaimAmount = ApprovedClaimAmount,
        @TotalStationaryAmount = TotalStationaryAmount,
        @FinalApprovedAmount = FinalApprovedAmount,
        @TotalDeductionAmount = TotalDeductionAmount
    FROM dbo.SalesApprovalRequest WITH (UPDLOCK, HOLDLOCK)
    WHERE ApprovalRequestId = 121;

    IF @@ROWCOUNT <> 1
        THROW 58342, N'ABORT: request 121 was not found. No row updated.', 1;

    IF @Status <> N'ReadyForPayment'
        OR @ModifiedBy <> N'Audit'
        OR @ModifiedOn <> '2026-10-12 07:07:16.4216939'
        OR @TotalKM <> 2275.15
        OR @TotalClaimAmount <> 8531.84
        OR @TotalDAAmount <> 350.00
        OR @TotalHotelAmount <> 100.00
        OR @TotalTravelAmount <> 51.00
        OR @TotalExpenseAmount <> 501.00
        OR @ApprovedDAAmount <> 350.00
        OR @ApprovedHotelAmount <> 100.00
        OR @ApprovedTravelAmount <> 51.00
        OR @ApprovedKM <> 2275.15
        OR @ApprovedClaimAmount <> 8531.84
        OR @TotalStationaryAmount <> 0.00
        OR @FinalApprovedAmount <> 9032.84
        OR @TotalDeductionAmount IS NOT NULL
        THROW 58343, N'ABORT: request 121 header pin does not match. No row updated.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.ApprovalRequestStatusHistory WITH (UPDLOCK, HOLDLOCK)
        WHERE HistoryId = 797
          AND ApprovalRequestId = 121
          AND OldStatus = N'Pending'
          AND NewStatus = N'Completed'
          AND ChangedBy = N'Audit'
          AND ChangedOn = '2026-10-12 07:07:16.1926027'
          AND Remarks = N'Auditor finalized all rows before Ready for Payment.'
    )
        THROW 58344, N'ABORT: history 797 does not match the Completed transition. No row updated.', 1;

    DECLARE @ActionDate datetime2(7);

    SELECT @ActionDate = ChangedOn
    FROM dbo.ApprovalRequestStatusHistory WITH (UPDLOCK, HOLDLOCK)
    WHERE HistoryId = 798
      AND ApprovalRequestId = 121
      AND OldStatus = N'Completed'
      AND NewStatus = N'ReadyForPayment'
      AND ChangedBy = N'Audit'
      AND ChangedOn = '2026-10-12 07:07:16.2551270'
      AND Remarks = N'Auditor marked Ready for Payment';

    IF @@ROWCOUNT <> 1 OR @ActionDate IS NULL
        THROW 58345, N'ABORT: history 798 does not match the ReadyForPayment transition. No row updated.', 1;

    DECLARE @HistoryRows int;
    DECLARE @MaxHistoryId int;

    SELECT
        @HistoryRows = COUNT(*),
        @MaxHistoryId = MAX(HistoryId)
    FROM dbo.ApprovalRequestStatusHistory WITH (UPDLOCK, HOLDLOCK)
    WHERE ApprovalRequestId = 121;

    IF @HistoryRows <> 8 OR @MaxHistoryId <> 798
        THROW 58346, N'ABORT: request 121 history set is not 8 rows ending at 798. No row updated.', 1;

    DECLARE @ApproverRows int;

    SELECT @ApproverRows = COUNT(*)
    FROM dbo.SalesApprovalRequestApprover WITH (UPDLOCK, HOLDLOCK)
    WHERE ApprovalRequestId = 121;

    IF @ApproverRows <> 2
        THROW 58347, N'ABORT: request 121 does not have exactly two approver rows. No row updated.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestApprover WITH (UPDLOCK, HOLDLOCK)
        WHERE ApprovalRequestApproverId = 237
          AND ApprovalRequestId = 121
          AND ApproverLoginId = N'AHS1806'
          AND ApproverRole = N'L1'
          AND ApproverLevel = 1
          AND Status = N'Approved'
          AND ActionBy = N'AHS1806'
          AND ActionDate = '2026-10-12 06:51:12.1600000'
          AND Remarks = N'Approved via Unified Day Modal - Added by Bidya Sankar Acharya (AHS1806)'
    )
        THROW 58348, N'ABORT: approver 237 does not match the pinned L1 row. No row updated.', 1;

    DECLARE @RowStatus nvarchar(50);
    DECLARE @RowActionBy nvarchar(100);
    DECLARE @RowActionDate datetime2(7);
    DECLARE @RowRemarks nvarchar(1000);
    DECLARE @RowLogin nvarchar(100);
    DECLARE @RowRole nvarchar(100);
    DECLARE @RowLevel int;
    DECLARE @RowRequest int;

    SELECT
        @RowRequest = ApprovalRequestId,
        @RowLogin = ApproverLoginId,
        @RowRole = ApproverRole,
        @RowLevel = ApproverLevel,
        @RowStatus = Status,
        @RowActionBy = ActionBy,
        @RowActionDate = ActionDate,
        @RowRemarks = Remarks
    FROM dbo.SalesApprovalRequestApprover WITH (UPDLOCK, HOLDLOCK)
    WHERE ApprovalRequestApproverId = 238;

    IF @@ROWCOUNT <> 1
        THROW 58349, N'ABORT: approver 238 was not found. No row updated.', 1;

    IF @RowRequest = 121
       AND @RowLogin = N'Audit'
       AND @RowRole = N'Auditor'
       AND @RowLevel = 2
       AND @RowStatus = N'Approved'
       AND @RowActionBy = N'Audit'
       AND @RowActionDate = @ActionDate
       AND @RowRemarks IS NULL
    BEGIN
        ROLLBACK TRAN;
        PRINT N'ALREADY IN POST-STATE | approver 238 is Approved with ActionDate from history 798. No row updated.';
        RETURN;
    END;

    IF @RowRequest <> 121
       OR @RowLogin <> N'Audit'
       OR @RowRole <> N'Auditor'
       OR @RowLevel <> 2
       OR @RowStatus <> N'Pending'
       OR @RowActionBy IS NOT NULL
       OR @RowActionDate IS NOT NULL
       OR @RowRemarks IS NOT NULL
        THROW 58350, N'ABORT: approver 238 is not the pinned Pending Auditor row. No row updated.', 1;

    UPDATE dbo.SalesApprovalRequestApprover
    SET
        Status = N'Approved',
        ActionBy = N'Audit',
        ActionDate = @ActionDate
    WHERE ApprovalRequestApproverId = 238
      AND ApprovalRequestId = 121
      AND ApproverLoginId = N'Audit'
      AND ApproverRole = N'Auditor'
      AND ApproverLevel = 2
      AND Status = N'Pending'
      AND ActionBy IS NULL
      AND ActionDate IS NULL
      AND Remarks IS NULL;

    IF @@ROWCOUNT <> 1
        THROW 58351, N'ABORT: approver 238 update count was not 1. Rolled back.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = 121
          AND Status = N'ReadyForPayment'
          AND ModifiedBy = N'Audit'
          AND ModifiedOn = '2026-10-12 07:07:16.4216939'
          AND TotalKM = 2275.15
          AND TotalClaimAmount = 8531.84
          AND ApprovedClaimAmount = 8531.84
          AND ApprovedKM = 2275.15
          AND FinalApprovedAmount = 9032.84
          AND TotalDeductionAmount IS NULL
    )
        THROW 58352, N'ABORT: request 121 header changed. Rolled back.', 1;

    IF (
        SELECT COUNT(*)
        FROM dbo.ApprovalRequestStatusHistory
        WHERE ApprovalRequestId = 121
    ) <> 8
        OR (
            SELECT MAX(HistoryId)
            FROM dbo.ApprovalRequestStatusHistory
            WHERE ApprovalRequestId = 121
        ) <> 798
        THROW 58353, N'ABORT: request 121 history changed. Rolled back.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestApprover
        WHERE ApprovalRequestApproverId = 237
          AND Status = N'Approved'
          AND ActionBy = N'AHS1806'
          AND ActionDate = '2026-10-12 06:51:12.1600000'
    )
        THROW 58354, N'ABORT: approver 237 changed. Rolled back.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestApprover
        WHERE ApprovalRequestApproverId = 238
          AND ApprovalRequestId = 121
          AND ApproverLoginId = N'Audit'
          AND ApproverRole = N'Auditor'
          AND ApproverLevel = 2
          AND Status = N'Approved'
          AND ActionBy = N'Audit'
          AND ActionDate = @ActionDate
          AND Remarks IS NULL
    )
        THROW 58355, N'ABORT: approver 238 post-state does not match. Rolled back.', 1;

    IF @Commit <> 1
    BEGIN
        ROLLBACK TRAN;
        PRINT N'ROLLED BACK | approver 238 matched the post-state inside the transaction. @Commit is 0, so the correction was not kept.';
        RETURN;
    END;

    COMMIT TRAN;
    PRINT N'COMMITTED | approver 238 set to Approved. ActionBy Audit. ActionDate taken from history 798.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;
    THROW;
END CATCH;
