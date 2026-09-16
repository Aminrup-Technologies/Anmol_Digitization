/*==============================================================================
  NAME:        rc017_sp_rollback_production
  WHEN:        2026-09-16
  WHY:         CAB rollback — restore Production stored procedures to pre-deploy
               definitions captured in RC017_Production_Schema_20260916.sql
  WHAT:        Six TA/DA v1.1 procedure bodies (verbatim from Production schema
               baseline). Absent Production objects use DROP IF EXISTS only.
  SOURCE:      docs/releases/TA_DA_GoLive_2026/RC017_Production_Schema_20260916.sql
               (SSMS Generate Scripts, Script Date: 16-Sep-26 4:19:54 PM)
  DATABASE:    AnmolDristi (Production)
  NOTE:        Do not rewrite. Execute in SSMS against Production only for
               CAB-authorized rollback after a failed Phase 4 SQL deployment.
==============================================================================*/
USE [AnmolDristi]
GO

PRINT 'RC-017 Rollback Started'
GO

/*------------------------------------------------------------------------------
  1. dbo.usp_ProcessApprovalAction
------------------------------------------------------------------------------*/
/* Production baseline: procedure ABSENT in RC017_Production_Schema_20260916.sql.
   Rollback action after a failed deploy of a NEW object: DROP IF EXISTS. */
IF OBJECT_ID(N'[dbo].[usp_ProcessApprovalAction]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_ProcessApprovalAction];
GO
PRINT 'RC-017 Rollback: usp_ProcessApprovalAction — ABSENT on baseline; DROP IF EXISTS applied'
GO

/*------------------------------------------------------------------------------
  2. dbo.usp_MarkRequestReadyForPayment
------------------------------------------------------------------------------*/
IF OBJECT_ID(N'[dbo].[usp_MarkRequestReadyForPayment]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_MarkRequestReadyForPayment];
GO

/****** Object:  StoredProcedure [dbo].[usp_MarkRequestReadyForPayment]    Script Date: 16-Sep-26 4:19:54 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[usp_MarkRequestReadyForPayment]
(
    @ApprovalRequestId INT,
    @ActionBy NVARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;

    /* 1️⃣ Auditor validation */
    IF NOT EXISTS (
        SELECT 1
        FROM SalesApprovalRequestApprover
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND ApproverLoginId = @ActionBy
          AND ApproverRole = 'Auditor'
    )
    BEGIN
        RAISERROR('Only Auditor can mark Ready for Payment.',16,1);
        RETURN;
    END

    /* 2️⃣ Must be Completed */
    IF NOT EXISTS (
        SELECT 1
        FROM SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND Status = 'Completed'
    )
    BEGIN
        RAISERROR('Request not completed.',16,1);
        RETURN;
    END

    /* 3️⃣ Update header */
    UPDATE SalesApprovalRequest
    SET
        Status = 'ReadyForPayment',
        ModifiedBy = @ActionBy,
        ModifiedOn = SYSUTCDATETIME()
    WHERE ApprovalRequestId = @ApprovalRequestId;

    INSERT INTO ApprovalRequestStatusHistory
    VALUES
    (
        @ApprovalRequestId,'Completed','ReadyForPayment',
        @ActionBy,SYSUTCDATETIME(),
        'Auditor marked Ready for Payment'
    );
END
GO
PRINT 'RC-017 Rollback: usp_MarkRequestReadyForPayment — restored from Production baseline'
GO

/*------------------------------------------------------------------------------
  3. dbo.usp_RecalculateApprovalRequest
------------------------------------------------------------------------------*/
/* Production baseline: procedure ABSENT in RC017_Production_Schema_20260916.sql.
   Rollback action after a failed deploy of a NEW object: DROP IF EXISTS. */
IF OBJECT_ID(N'[dbo].[usp_RecalculateApprovalRequest]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_RecalculateApprovalRequest];
GO
PRINT 'RC-017 Rollback: usp_RecalculateApprovalRequest — ABSENT on baseline; DROP IF EXISTS applied'
GO

/*------------------------------------------------------------------------------
  4. dbo.usp_GetApprovalRequest
------------------------------------------------------------------------------*/
/* Production baseline: procedure ABSENT in RC017_Production_Schema_20260916.sql.
   Rollback action after a failed deploy of a NEW object: DROP IF EXISTS. */
IF OBJECT_ID(N'[dbo].[usp_GetApprovalRequest]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_GetApprovalRequest];
GO
PRINT 'RC-017 Rollback: usp_GetApprovalRequest — ABSENT on baseline; DROP IF EXISTS applied'
GO

/*------------------------------------------------------------------------------
  5. dbo.usp_UpdateSalesDailyExpenseStatus
------------------------------------------------------------------------------*/
IF OBJECT_ID(N'[dbo].[usp_UpdateSalesDailyExpenseStatus]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_UpdateSalesDailyExpenseStatus];
GO

/****** Object:  StoredProcedure [dbo].[usp_UpdateSalesDailyExpenseStatus]    Script Date: 16-Sep-26 4:19:54 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateSalesDailyExpenseStatus]
(
    @ExpenseId INT,
    @NewStatus NVARCHAR(50), -- 'Approved' or 'Rejected'
    @ActionBy  NVARCHAR(100),
    @Remarks   NVARCHAR(MAX) = NULL -- ⬅️ NEW: Added the Remarks parameter
)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1️⃣ Safety Check: Prevent update if deleted
    IF EXISTS (SELECT 1 FROM dbo.SalesDailyExpense WHERE ExpenseId = @ExpenseId AND IsDeleted = 1)
    BEGIN
        RAISERROR('Cannot update a deleted expense.', 16, 1);
        RETURN;
    END

    -- 2️⃣ Update the Expense Status AND Remarks
    UPDATE dbo.SalesDailyExpense
    SET 
        ClaimStatus = @NewStatus,
        Remarks = @Remarks, -- ⬅️ NEW: Save the rejection reason here!
        ClaimProcessedBy = @ActionBy,
        ClaimProcessedDate = DATEADD(MINUTE, 330, SYSUTCDATETIME()), 
        ModifiedBy = @ActionBy,
        ModifiedOn = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;

    -- 3️⃣ IDENTIFY PARENT RECORDS
    DECLARE @ActivityId INT, @ApprovalRequestDetailId INT, @ApprovalRequestId INT;

    SELECT @ActivityId = ActivityId FROM dbo.SalesDailyExpense WHERE ExpenseId = @ExpenseId;

    SELECT @ApprovalRequestDetailId = ApprovalRequestDetailId, @ApprovalRequestId = ApprovalRequestId
    FROM dbo.SalesApprovalRequestDetail
    WHERE ActivityId = @ActivityId;

    -- 4️⃣ CHAIN REACTION: RECALCULATE AMOUNTS (FIXED: Respects CK_TA_Calculated constraint)
    IF @ApprovalRequestDetailId IS NOT NULL
    BEGIN
        -- A. Recalculate the specific expense buckets for this Day/Row
        -- (Only summing expenses that are NOT rejected and NOT deleted)
        UPDATE dbo.SalesApprovalRequestDetail
        SET 
            Snapshot_DAAmount = ISNULL((SELECT SUM(Amount) FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND ExpenseType = 'DA' AND IsDeleted = 0 AND ISNULL(ClaimStatus,'') <> 'Rejected'), 0),
            Snapshot_HotelAmount = ISNULL((SELECT SUM(Amount) FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND ExpenseType = 'Hotel' AND IsDeleted = 0 AND ISNULL(ClaimStatus,'') <> 'Rejected'), 0),
            Snapshot_TravelAmount = ISNULL((SELECT SUM(Amount) FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND ExpenseType = 'Travel' AND IsDeleted = 0 AND ISNULL(ClaimStatus,'') <> 'Rejected'), 0),
            ModifiedOn = SYSUTCDATETIME()
        WHERE ApprovalRequestDetailId = @ApprovalRequestDetailId;

        -- B. Update the Total Expense Snapshot (DO NOT TOUCH Snapshot_CalculatedClaimAmount)
        UPDATE dbo.SalesApprovalRequestDetail
        SET Snapshot_ExpenseTotal = ISNULL(Snapshot_DAAmount, 0) + ISNULL(Snapshot_HotelAmount, 0) + ISNULL(Snapshot_TravelAmount, 0)
        WHERE ApprovalRequestDetailId = @ApprovalRequestDetailId;

        -- C. Recalculate the HEADER (Face Values)
        -- Sum up the updated snapshots from all rows
        DECLARE @HeaderTotalClaim DECIMAL(18,2);
        DECLARE @HeaderTotalExpense DECIMAL(18,2);

        SELECT 
            @HeaderTotalClaim = SUM(ISNULL(Snapshot_CalculatedClaimAmount, 0)),
            @HeaderTotalExpense = SUM(ISNULL(Snapshot_ExpenseTotal, 0))
        FROM dbo.SalesApprovalRequestDetail
        WHERE ApprovalRequestId = @ApprovalRequestId;

        UPDATE dbo.SalesApprovalRequest
        SET 
            TotalClaimAmount = @HeaderTotalClaim, -- This updates the main "Grand Total"
            TotalExpenseAmount = @HeaderTotalExpense,
            ModifiedOn = SYSUTCDATETIME()
        WHERE ApprovalRequestId = @ApprovalRequestId;
    END
END
GO
PRINT 'RC-017 Rollback: usp_UpdateSalesDailyExpenseStatus — restored from Production baseline'
GO

/*------------------------------------------------------------------------------
  6. dbo.usp_Export_Auditor_Final_Excel
------------------------------------------------------------------------------*/
IF OBJECT_ID(N'[dbo].[usp_Export_Auditor_Final_Excel]', N'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_Export_Auditor_Final_Excel];
GO

/****** Object:  StoredProcedure [dbo].[usp_Export_Auditor_Final_Excel]    Script Date: 16-Sep-26 4:19:54 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_Export_Auditor_Final_Excel]
(
    @ApprovalRequestId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SAR.ApprovalRequestId                 AS [Claim No],
        SAR.RequesterLoginId                  AS [Employee Code],
        UM.EmployeeName                       AS [Employee Name],
        SDA.ActivityDate                      AS [Date],
        SDA.Day                               AS [Day],
        
        ISNULL((SELECT TOP 1 FromPlace FROM dbo.SalesDailyExpense WHERE ActivityId = SARD.ActivityId AND IsDeleted = 0 AND FromPlace IS NOT NULL ORDER BY ExpenseId ASC), '-') AS [From Place],
        ISNULL((SELECT TOP 1 ToPlace FROM dbo.SalesDailyExpense WHERE ActivityId = SARD.ActivityId AND IsDeleted = 0 AND ToPlace IS NOT NULL ORDER BY ExpenseId DESC), '-') AS [To Place],

        ISNULL(SARD.Snapshot_TotalDistanceKM, 0) AS [KM Claimed],
        ISNULL(SARD.Snapshot_CalculatedClaimAmount, 0) AS [TA Bill],

        ISNULL(SARD.Snapshot_DAAmount, 0)     AS [DA],
        ISNULL(SARD.Snapshot_HotelAmount, 0)  AS [Hotel],
        ISNULL(SARD.Snapshot_TravelAmount, 0) AS [Travel / OPA],
        
        -- NEW: Explicitly select the Stationary amount
        ISNULL(SARD.Snapshot_StationaryAmount, 0) AS [Stationary],

        -- UPDATE: Include Stationary in the sum
        (ISNULL(SARD.Snapshot_DAAmount, 0) + ISNULL(SARD.Snapshot_HotelAmount, 0) + ISNULL(SARD.Snapshot_TravelAmount, 0) + ISNULL(SARD.Snapshot_StationaryAmount, 0)) AS [Total Expenses (DA+Hotel+Trv+Stat)],

        (ISNULL(SARD.Snapshot_ExpenseTotal, 0) + ISNULL(SARD.Snapshot_CalculatedClaimAmount, 0)) AS [Final Approved Amount]

    FROM SalesApprovalRequest SAR
    INNER JOIN SalesApprovalRequestDetail SARD ON SAR.ApprovalRequestId = SARD.ApprovalRequestId
    INNER JOIN SalesDailyActivity SDA ON SDA.ActivityId = SARD.ActivityId
    INNER JOIN MST_UserMaster UM ON UM.EmployeeCode = SAR.RequesterLoginId
    WHERE SAR.ApprovalRequestId = @ApprovalRequestId
      AND SAR.Status IN ('Completed', 'ReadyForPayment', 'Processed')
      AND SARD.DetailStatus = 'Approved' 
    ORDER BY SDA.ActivityDate ASC;
END
GO
PRINT 'RC-017 Rollback: usp_Export_Auditor_Final_Excel — restored from Production baseline'
GO

PRINT 'RC-017 Rollback Completed'
GO
