/*
    Object:      [dbo].[usp_ResolveRegion]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_ResolveRegion]
    @InputValue VARCHAR(200),
    @ResolvedId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @ResolvedId = NULL;

    IF ISNULL(@InputValue,'') = '' RETURN;

    -- 1) numeric -> try PK
    IF TRY_CONVERT(INT, @InputValue) IS NOT NULL
    BEGIN
        DECLARE @tmp INT = TRY_CONVERT(INT, @InputValue);
        IF EXISTS (SELECT 1 FROM dbo.MST_Region WHERE Id = @tmp)
        BEGIN
            SET @ResolvedId = @tmp;
            RETURN;
        END
    END

    -- 2) try Region_ID (custom id)
    IF EXISTS (SELECT 1 FROM dbo.MST_Region WHERE Region_ID = @InputValue)
    BEGIN
        SELECT TOP (1) @ResolvedId = Id FROM dbo.MST_Region WHERE Region_ID = @InputValue ORDER BY Id;
        RETURN;
    END

    -- 3) try human description fallback
    IF EXISTS (SELECT 1 FROM dbo.MST_Region WHERE Region_Description = @InputValue)
    BEGIN
        SELECT TOP (1) @ResolvedId = Id FROM dbo.MST_Region WHERE Region_Description = @InputValue ORDER BY Id;
        RETURN;
    END
END
