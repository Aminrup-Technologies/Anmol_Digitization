/*
    Object:      [dbo].[usp_GetSalesDailyActivityKMHistory]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE dbo.usp_GetSalesDailyActivityKMHistory
    @ActivityId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        h.HistoryId,
        h.ActivityId,
        h.OldKM,
        h.NewKM,
        h.ModifiedBy,
        h.ModifiedDateTime,
        h.Reason,

        -- Aggregated FromPlace
        TravelFromPlace =
            STUFF((
                SELECT DISTINCT ', ' + e.FromPlace
                FROM dbo.SalesDailyExpense e
                WHERE e.ActivityId = h.ActivityId
                  AND e.IsDeleted = 0
                  AND e.FromPlace IS NOT NULL
                  AND LTRIM(RTRIM(e.FromPlace)) <> ''
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'), 1, 2, ''),

        -- Aggregated ToPlace
        TravelToPlace =
            STUFF((
                SELECT DISTINCT ', ' + e.ToPlace
                FROM dbo.SalesDailyExpense e
                WHERE e.ActivityId = h.ActivityId
                  AND e.IsDeleted = 0
                  AND e.ToPlace IS NOT NULL
                  AND LTRIM(RTRIM(e.ToPlace)) <> ''
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'), 1, 2, '')

    FROM dbo.SalesDailyActivityKMHistory h
    WHERE h.ActivityId = @ActivityId
    ORDER BY h.ModifiedDateTime DESC;
END
