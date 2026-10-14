/*
    Package:     Database/Deployments/MarkReadyAuditorRow
    Script:      R100_Recover_Approver196.sql
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN (AnmolDristi)
    Mode:        PREPARED — do not execute. Data correction is not authorized.

    Separate from request 121. This script names approver 196 and history
    702 only. It does not call dbo.usp_MarkRequestReadyForPayment and it
    does not read or update request 121.

    Read on AnmolDristiDev at 2026-10-12:

      Header 100                         ReadyForPayment, ModifiedBy Audit
      ModifiedOn                         2026-09-17 13:20:44.4299643
      History 701                        Pending -> Completed at 2026-09-17 13:20:43.8825423
                                         remark Auditor finalized all rows before Ready for Payment.
      History 702                        Completed -> ReadyForPayment at 2026-09-17 13:20:44.2112321
                                         ChangedBy Audit
                                         remark Auditor marked Ready for Payment
      Approver 196                       request 100, Audit, Auditor, level 2, Pending
                                         ActionBy null, ActionDate null, Remarks null
      Approver 195                       L1 MHS242, Approved; left unchanged

    Those two history remarks are the same sentences written by
    ClaimsApproval_v2.EnsureHeaderCompletedForPayment and by the procedure's
    history INSERT. The residue is the same shape as request 121.

    The cause is not the same event. History 702 is 2026-09-17.
    Repository commit e1ddf06, which added the Auditor-row update, is
    2026-09-28. Baseline 3779d185 on 2026-09-16 has the named history
    INSERT and does not update the approver row. The module deployed on
    2026-10-12, modified 2026-10-08, still has no approver update, but that
    modify_date is later than history 702, so this read does not prove the
    17 September module text. This script is eligible only as alignment to
    the current repository contract, under request 100's own pins.

    @Execute defaults to 0. @Commit defaults to 0.
    Both stay 0 until a separate DBA authorization.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Execute bit = 0;
DECLARE @Commit bit = 0;

IF @Execute <> 1
BEGIN
    PRINT N'NOT RUN | @Execute is 0. Approver 196 was not read or updated.';
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
    WHERE ApprovalRequestId = 100;

    IF @@ROWCOUNT <> 1
        THROW 58362, N'ABORT: request 100 was not found. No row updated.', 1;

    IF @Status <> N'ReadyForPayment'
        OR @ModifiedBy <> N'Audit'
        OR @ModifiedOn <> '2026-09-17 13:20:44.4299643'
        OR @TotalKM <> 1861.95
        OR @TotalClaimAmount <> 6959.15
        OR @TotalDAAmount <> 1775.00
        OR @TotalHotelAmount <> 450.00
        OR @TotalTravelAmount <> 1350.00
        OR @TotalExpenseAmount <> 3535.00
        OR @ApprovedDAAmount <> 1550.00
        OR @ApprovedHotelAmount <> 0.00
        OR @ApprovedTravelAmount <> 600.00
        OR @ApprovedKM <> 1662.44
        OR @ApprovedClaimAmount <> 6234.15
        OR @TotalStationaryAmount <> 1510.00
        OR @FinalApprovedAmount <> 9294.15
        OR @TotalDeductionAmount IS NOT NULL
        THROW 58363, N'ABORT: request 100 header pin does not match. No row updated.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.ApprovalRequestStatusHistory WITH (UPDLOCK, HOLDLOCK)
        WHERE HistoryId = 701
          AND ApprovalRequestId = 100
          AND OldStatus = N'Pending'
          AND NewStatus = N'Completed'
          AND ChangedBy = N'Audit'
          AND ChangedOn = '2026-09-17 13:20:43.8825423'
          AND Remarks = N'Auditor finalized all rows before Ready for Payment.'
    )
        THROW 58364, N'ABORT: history 701 does not match the Completed transition. No row updated.', 1;

    DECLARE @ActionDate datetime2(7);

    SELECT @ActionDate = ChangedOn
    FROM dbo.ApprovalRequestStatusHistory WITH (UPDLOCK, HOLDLOCK)
    WHERE HistoryId = 702
      AND ApprovalRequestId = 100
      AND OldStatus = N'Completed'
      AND NewStatus = N'ReadyForPayment'
      AND ChangedBy = N'Audit'
      AND ChangedOn = '2026-09-17 13:20:44.2112321'
      AND Remarks = N'Auditor marked Ready for Payment';

    IF @@ROWCOUNT <> 1 OR @ActionDate IS NULL
        THROW 58365, N'ABORT: history 702 does not match the ReadyForPayment transition. No row updated.', 1;

    DECLARE @HistoryRows int;
    DECLARE @MaxHistoryId int;

    SELECT
        @HistoryRows = COUNT(*),
        @MaxHistoryId = MAX(HistoryId)
    FROM dbo.ApprovalRequestStatusHistory WITH (UPDLOCK, HOLDLOCK)
    WHERE ApprovalRequestId = 100;

    IF @HistoryRows <> 6 OR @MaxHistoryId <> 702
        THROW 58366, N'ABORT: request 100 history set is not 6 rows ending at 702. No row updated.', 1;

    DECLARE @ApproverRows int;

    SELECT @ApproverRows = COUNT(*)
    FROM dbo.SalesApprovalRequestApprover WITH (UPDLOCK, HOLDLOCK)
    WHERE ApprovalRequestId = 100;

    IF @ApproverRows <> 2
        THROW 58367, N'ABORT: request 100 does not have exactly two approver rows. No row updated.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestApprover WITH (UPDLOCK, HOLDLOCK)
        WHERE ApprovalRequestApproverId = 195
          AND ApprovalRequestId = 100
          AND ApproverLoginId = N'MHS242'
          AND ApproverRole = N'L1'
          AND ApproverLevel = 1
          AND Status = N'Approved'
          AND ActionBy = N'MHS242'
          AND ActionDate = '2026-09-16 13:09:59.3300000'
          AND Remarks IS NULL
    )
        THROW 58368, N'ABORT: approver 195 does not match the pinned L1 row. No row updated.', 1;

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
    WHERE ApprovalRequestApproverId = 196;

    IF @@ROWCOUNT <> 1
        THROW 58369, N'ABORT: approver 196 was not found. No row updated.', 1;

    IF @RowRequest = 100
       AND @RowLogin = N'Audit'
       AND @RowRole = N'Auditor'
       AND @RowLevel = 2
       AND @RowStatus = N'Approved'
       AND @RowActionBy = N'Audit'
       AND @RowActionDate = @ActionDate
       AND @RowRemarks IS NULL
    BEGIN
        ROLLBACK TRAN;
        PRINT N'ALREADY IN POST-STATE | approver 196 is Approved with ActionDate from history 702. No row updated.';
        RETURN;
    END;

    IF @RowRequest <> 100
       OR @RowLogin <> N'Audit'
       OR @RowRole <> N'Auditor'
       OR @RowLevel <> 2
       OR @RowStatus <> N'Pending'
       OR @RowActionBy IS NOT NULL
       OR @RowActionDate IS NOT NULL
       OR @RowRemarks IS NOT NULL
        THROW 58370, N'ABORT: approver 196 is not the pinned Pending Auditor row. No row updated.', 1;

    UPDATE dbo.SalesApprovalRequestApprover
    SET
        Status = N'Approved',
        ActionBy = N'Audit',
        ActionDate = @ActionDate
    WHERE ApprovalRequestApproverId = 196
      AND ApprovalRequestId = 100
      AND ApproverLoginId = N'Audit'
      AND ApproverRole = N'Auditor'
      AND ApproverLevel = 2
      AND Status = N'Pending'
      AND ActionBy IS NULL
      AND ActionDate IS NULL
      AND Remarks IS NULL;

    IF @@ROWCOUNT <> 1
        THROW 58371, N'ABORT: approver 196 update count was not 1. Rolled back.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequest
        WHERE ApprovalRequestId = 100
          AND Status = N'ReadyForPayment'
          AND ModifiedBy = N'Audit'
          AND ModifiedOn = '2026-09-17 13:20:44.4299643'
          AND TotalKM = 1861.95
          AND ApprovedKM = 1662.44
          AND ApprovedClaimAmount = 6234.15
          AND FinalApprovedAmount = 9294.15
          AND TotalStationaryAmount = 1510.00
          AND TotalDeductionAmount IS NULL
    )
        THROW 58372, N'ABORT: request 100 header changed. Rolled back.', 1;

    IF (
        SELECT COUNT(*)
        FROM dbo.ApprovalRequestStatusHistory
        WHERE ApprovalRequestId = 100
    ) <> 6
        OR (
            SELECT MAX(HistoryId)
            FROM dbo.ApprovalRequestStatusHistory
            WHERE ApprovalRequestId = 100
        ) <> 702
        THROW 58373, N'ABORT: request 100 history changed. Rolled back.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestApprover
        WHERE ApprovalRequestApproverId = 195
          AND Status = N'Approved'
          AND ActionBy = N'MHS242'
          AND ActionDate = '2026-09-16 13:09:59.3300000'
    )
        THROW 58374, N'ABORT: approver 195 changed. Rolled back.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SalesApprovalRequestApprover
        WHERE ApprovalRequestApproverId = 196
          AND ApprovalRequestId = 100
          AND ApproverLoginId = N'Audit'
          AND ApproverRole = N'Auditor'
          AND ApproverLevel = 2
          AND Status = N'Approved'
          AND ActionBy = N'Audit'
          AND ActionDate = @ActionDate
          AND Remarks IS NULL
    )
        THROW 58375, N'ABORT: approver 196 post-state does not match. Rolled back.', 1;

    IF @Commit <> 1
    BEGIN
        ROLLBACK TRAN;
        PRINT N'ROLLED BACK | approver 196 matched the post-state inside the transaction. @Commit is 0, so the correction was not kept.';
        RETURN;
    END;

    COMMIT TRAN;
    PRINT N'COMMITTED | approver 196 set to Approved. ActionBy Audit. ActionDate taken from history 702.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;
    THROW;
END CATCH;
