/*
    Object:      [dbo].[usp_GetSalesDailyActivity_Month]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_GetSalesDailyActivity_Month]
    @LoginId NVARCHAR(100),
    @Year INT,
    @Month INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartDate DATE = DATEFROMPARTS(@Year, @Month, 1);
    DECLARE @EndDate DATE = DATEADD(DAY, -1, DATEADD(MONTH, 1, @StartDate));

    ;WITH DA_ByActivity AS
    (
        SELECT a.ActivityId, a.Login_Id AS LoginId, 
               CAST(SUM(ISNULL(e.Amount, 0)) AS DECIMAL(18,2)) AS DAAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS HotelAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS TravelAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS StationaryAmount
        FROM dbo.SalesDailyExpense e
        INNER JOIN dbo.SalesDailyActivity a ON e.ActivityId = a.ActivityId AND a.Login_Id = @LoginId AND a.ActivityDate BETWEEN @StartDate AND @EndDate
        WHERE e.ExpenseType = 'DA' AND e.ActivityId IS NOT NULL AND ISNULL(e.IsDeleted,0) = 0
          AND YEAR(ISNULL(e.ExpenseDate, e.FromDate)) = @Year AND MONTH(ISNULL(e.ExpenseDate, e.FromDate)) = @Month
        GROUP BY a.ActivityId, a.Login_Id
    ),
    DA_ByDate AS
    (
        SELECT a.ActivityId, a.Login_Id AS LoginId, 
               CAST(SUM(ISNULL(e.Amount, 0)) AS DECIMAL(18,2)) AS DAAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS HotelAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS TravelAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS StationaryAmount
        FROM dbo.SalesDailyExpense e
        INNER JOIN dbo.SalesDailyActivity a ON CAST(ISNULL(e.ExpenseDate, e.FromDate) AS DATE) = CAST(a.ActivityDate AS DATE) AND a.Login_Id = @LoginId
        WHERE e.ExpenseType = 'DA' AND (e.ActivityId IS NULL OR e.ActivityId = 0) AND ISNULL(e.IsDeleted,0) = 0
          AND YEAR(ISNULL(e.ExpenseDate, e.FromDate)) = @Year AND MONTH(ISNULL(e.ExpenseDate, e.FromDate)) = @Month
        GROUP BY a.ActivityId, a.Login_Id
    ),
    Hotel_ByActivity AS
    (
        SELECT a.ActivityId, a.Login_Id AS LoginId, 
               CAST(0.00 AS DECIMAL(18,2)) AS DAAmount, 
               CAST(SUM(ISNULL(e.Amount, 0)) AS DECIMAL(18,2)) AS HotelAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS TravelAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS StationaryAmount
        FROM dbo.SalesDailyExpense e
        INNER JOIN dbo.SalesDailyActivity a ON e.ActivityId = a.ActivityId AND a.Login_Id = @LoginId AND a.ActivityDate BETWEEN @StartDate AND @EndDate
        WHERE e.ExpenseType = 'Hotel' AND e.ActivityId IS NOT NULL AND e.ToDate IS NOT NULL AND ISNULL(e.IsDeleted,0) = 0
          AND YEAR(e.ToDate) = @Year AND MONTH(e.ToDate) = @Month
        GROUP BY a.ActivityId, a.Login_Id
    ),
    Hotel_ByToDate AS
    (
        SELECT a.ActivityId, a.Login_Id AS LoginId, 
               CAST(0.00 AS DECIMAL(18,2)) AS DAAmount, 
               CAST(SUM(ISNULL(e.Amount, 0)) AS DECIMAL(18,2)) AS HotelAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS TravelAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS StationaryAmount
        FROM dbo.SalesDailyExpense e
        INNER JOIN dbo.SalesDailyActivity a ON CAST(e.ToDate AS DATE) = CAST(a.ActivityDate AS DATE) AND a.Login_Id = @LoginId
        WHERE e.ExpenseType = 'Hotel' AND (e.ActivityId IS NULL OR e.ActivityId = 0) AND e.ToDate IS NOT NULL AND ISNULL(e.IsDeleted,0) = 0
          AND YEAR(e.ToDate) = @Year AND MONTH(e.ToDate) = @Month
        GROUP BY a.ActivityId, a.Login_Id
    ),
    Travel_ByActivity AS
    (
        SELECT a.ActivityId, a.Login_Id AS LoginId, 
               CAST(0.00 AS DECIMAL(18,2)) AS DAAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS HotelAmount, 
               CAST(SUM(ISNULL(e.Amount, 0)) AS DECIMAL(18,2)) AS TravelAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS StationaryAmount
        FROM dbo.SalesDailyExpense e
        INNER JOIN dbo.SalesDailyActivity a ON e.ActivityId = a.ActivityId AND a.Login_Id = @LoginId AND a.ActivityDate BETWEEN @StartDate AND @EndDate
        WHERE e.ExpenseType = 'Travel' AND e.ActivityId IS NOT NULL AND ISNULL(e.IsDeleted,0) = 0
          AND YEAR(ISNULL(e.TravelDate, e.ExpenseDate)) = @Year AND MONTH(ISNULL(e.TravelDate, e.ExpenseDate)) = @Month
        GROUP BY a.ActivityId, a.Login_Id
    ),
    Travel_ByDate AS
    (
        SELECT a.ActivityId, a.Login_Id AS LoginId, 
               CAST(0.00 AS DECIMAL(18,2)) AS DAAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS HotelAmount, 
               CAST(SUM(ISNULL(e.Amount, 0)) AS DECIMAL(18,2)) AS TravelAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS StationaryAmount
        FROM dbo.SalesDailyExpense e
        INNER JOIN dbo.SalesDailyActivity a ON CAST(ISNULL(e.TravelDate, e.ExpenseDate) AS DATE) = CAST(a.ActivityDate AS DATE) AND a.Login_Id = @LoginId
        WHERE e.ExpenseType = 'Travel' AND (e.ActivityId IS NULL OR e.ActivityId = 0) AND ISNULL(e.IsDeleted,0) = 0
          AND YEAR(ISNULL(e.TravelDate, e.ExpenseDate)) = @Year AND MONTH(ISNULL(e.TravelDate, e.ExpenseDate)) = @Month
        GROUP BY a.ActivityId, a.Login_Id
    ),
    /* NEW CTEs for Stationary */
    Stationary_ByActivity AS
    (
        SELECT a.ActivityId, a.Login_Id AS LoginId, 
               CAST(0.00 AS DECIMAL(18,2)) AS DAAmount, CAST(0.00 AS DECIMAL(18,2)) AS HotelAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS TravelAmount, 
               CAST(SUM(ISNULL(e.Amount, 0)) AS DECIMAL(18,2)) AS StationaryAmount
        FROM dbo.SalesDailyExpense e
        INNER JOIN dbo.SalesDailyActivity a ON e.ActivityId = a.ActivityId AND a.Login_Id = @LoginId AND a.ActivityDate BETWEEN @StartDate AND @EndDate
        WHERE e.ExpenseType = 'Stationary' AND e.ActivityId IS NOT NULL AND ISNULL(e.IsDeleted,0) = 0
          AND YEAR(ISNULL(e.ExpenseDate, e.FromDate)) = @Year AND MONTH(ISNULL(e.ExpenseDate, e.FromDate)) = @Month
        GROUP BY a.ActivityId, a.Login_Id
    ),
    Stationary_ByDate AS
    (
        SELECT a.ActivityId, a.Login_Id AS LoginId, 
               CAST(0.00 AS DECIMAL(18,2)) AS DAAmount, CAST(0.00 AS DECIMAL(18,2)) AS HotelAmount, 
               CAST(0.00 AS DECIMAL(18,2)) AS TravelAmount, 
               CAST(SUM(ISNULL(e.Amount, 0)) AS DECIMAL(18,2)) AS StationaryAmount
        FROM dbo.SalesDailyExpense e
        INNER JOIN dbo.SalesDailyActivity a ON CAST(ISNULL(e.ExpenseDate, e.FromDate) AS DATE) = CAST(a.ActivityDate AS DATE) AND a.Login_Id = @LoginId
        WHERE e.ExpenseType = 'Stationary' AND (e.ActivityId IS NULL OR e.ActivityId = 0) AND ISNULL(e.IsDeleted,0) = 0
          AND YEAR(ISNULL(e.ExpenseDate, e.FromDate)) = @Year AND MONTH(ISNULL(e.ExpenseDate, e.FromDate)) = @Month
        GROUP BY a.ActivityId, a.Login_Id
    ),
    /* --- */

    ExpenseUnion AS
    (
        SELECT ActivityId, LoginId, DAAmount, HotelAmount, TravelAmount, StationaryAmount FROM DA_ByActivity
        UNION ALL SELECT ActivityId, LoginId, DAAmount, HotelAmount, TravelAmount, StationaryAmount FROM DA_ByDate
        UNION ALL SELECT ActivityId, LoginId, DAAmount, HotelAmount, TravelAmount, StationaryAmount FROM Hotel_ByActivity
        UNION ALL SELECT ActivityId, LoginId, DAAmount, HotelAmount, TravelAmount, StationaryAmount FROM Hotel_ByToDate
        UNION ALL SELECT ActivityId, LoginId, DAAmount, HotelAmount, TravelAmount, StationaryAmount FROM Travel_ByActivity
        UNION ALL SELECT ActivityId, LoginId, DAAmount, HotelAmount, TravelAmount, StationaryAmount FROM Travel_ByDate
        UNION ALL SELECT ActivityId, LoginId, DAAmount, HotelAmount, TravelAmount, StationaryAmount FROM Stationary_ByActivity
        UNION ALL SELECT ActivityId, LoginId, DAAmount, HotelAmount, TravelAmount, StationaryAmount FROM Stationary_ByDate
    ),

    ExpenseAgg AS
    (
        SELECT
            ActivityId,
            LoginId,
            CAST(SUM(ISNULL(DAAmount,0)) AS DECIMAL(18,2))      AS DAAmount,
            CAST(SUM(ISNULL(HotelAmount,0)) AS DECIMAL(18,2))    AS HotelAmount,
            CAST(SUM(ISNULL(TravelAmount,0)) AS DECIMAL(18,2))   AS TravelAmount,
            CAST(SUM(ISNULL(StationaryAmount,0)) AS DECIMAL(18,2)) AS StationaryAmount,
            CAST(SUM(ISNULL(DAAmount,0) + ISNULL(HotelAmount,0) + ISNULL(TravelAmount,0) + ISNULL(StationaryAmount,0)) AS DECIMAL(18,2)) AS ExpenseTotal
        FROM ExpenseUnion
        GROUP BY ActivityId, LoginId
    )

    --SELECT s.*, t.MinAllowedKM, t.MaxAllowedKM,
    --       ISNULL(ea.DAAmount, 0.00)     AS DAAmount,
    --       ISNULL(ea.HotelAmount, 0.00)  AS HotelAmount,
    --       ISNULL(ea.TravelAmount, 0.00) AS TravelAmount,
    --       ISNULL(ea.StationaryAmount, 0.00) AS StationaryAmount,
    --       ISNULL(ea.ExpenseTotal, 0.00) AS ExpenseTotal
    --FROM dbo.SalesDailyActivity s
    --LEFT JOIN dbo.MST_EmployeeTolerance t ON t.EmployeeCode = s.Login_Id AND (t.EffectiveFrom IS NULL OR t.EffectiveFrom <= @EndDate) AND (t.EffectiveTo IS NULL OR t.EffectiveTo >= @StartDate)
    --LEFT JOIN ExpenseAgg ea ON ea.ActivityId = s.ActivityId AND ea.LoginId = s.Login_Id
    --WHERE s.Login_Id = @LoginId AND s.ActivityDate BETWEEN @StartDate AND @EndDate
    --ORDER BY s.ActivityDate;

	SELECT s.*, t.MinAllowedKM, t.MaxAllowedKM,
           ISNULL(ea.DAAmount, 0.00)     AS DAAmount,
           ISNULL(ea.HotelAmount, 0.00)  AS HotelAmount,
           ISNULL(ea.TravelAmount, 0.00) AS TravelAmount,
           ISNULL(ea.StationaryAmount, 0.00) AS StationaryAmount,
           ISNULL(ea.ExpenseTotal, 0.00) AS ExpenseTotal,
           
           -- ⬅️ NEW: Grab the most recent Manager Remark for this Activity!
           ISNULL(SARD.DetailRemarks, '') AS DetailRemarks 

    FROM dbo.SalesDailyActivity s
    
    LEFT JOIN dbo.MST_EmployeeTolerance t 
        ON t.EmployeeCode = s.Login_Id 
       AND (t.EffectiveFrom IS NULL OR t.EffectiveFrom <= @EndDate) 
       AND (t.EffectiveTo IS NULL OR t.EffectiveTo >= @StartDate)
       
    LEFT JOIN ExpenseAgg ea 
        ON ea.ActivityId = s.ActivityId 
       AND ea.LoginId = s.Login_Id
       
    -- ⬅️ NEW: Safely join the Detail table to pull the remark
    OUTER APPLY (
        SELECT TOP 1 DetailRemarks 
        FROM dbo.SalesApprovalRequestDetail 
        WHERE ActivityId = s.ActivityId 
        ORDER BY ApprovalRequestDetailId DESC
    ) SARD

    WHERE s.Login_Id = @LoginId 
      AND s.ActivityDate BETWEEN @StartDate AND @EndDate
    ORDER BY s.ActivityDate;
END
