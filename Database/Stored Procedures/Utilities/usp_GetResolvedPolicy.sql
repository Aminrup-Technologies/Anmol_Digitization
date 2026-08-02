/*
    Object:      [dbo].[usp_GetResolvedPolicy]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_GetResolvedPolicy]
    @EmployeeCode       VARCHAR(50),
    @RegionIdParam      VARCHAR(200) = NULL,  -- may be PK (Id) or custom code (Region_ID)
    @BranchIdParam      VARCHAR(200) = NULL,  -- may be PK (Id) or custom code (Branch_ID)
    @GradeIdParam       VARCHAR(200) = NULL,  -- may be PK (Id) or custom code (Grade_ID)
    @DesignationIdParam VARCHAR(200) = NULL   -- may be PK (Id) or custom code (Designation_ID)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RegionRaw VARCHAR(200), @BranchRaw VARCHAR(200), @GradeRaw VARCHAR(200), @DesignationRaw VARCHAR(200);
    DECLARE @RegionInput VARCHAR(200), @BranchInput VARCHAR(200), @GradeInput VARCHAR(200), @DesignationInput VARCHAR(200);

    DECLARE @RegionId INT = NULL, @BranchId INT = NULL, @GradeId INT = NULL, @DesignationId INT = NULL;

    -- read user-stored values only if corresponding param is NULL/empty
    SELECT
        @RegionRaw = CASE WHEN ISNULL(@RegionIdParam,'') = '' THEN TRY_CONVERT(VARCHAR(200), RegionId) ELSE NULL END,
        @BranchRaw = CASE WHEN ISNULL(@BranchIdParam,'') = '' THEN TRY_CONVERT(VARCHAR(200), BranchId) ELSE NULL END,
        @GradeRaw = CASE WHEN ISNULL(@GradeIdParam,'') = '' THEN TRY_CONVERT(VARCHAR(200), GradeId) ELSE NULL END,
        @DesignationRaw = CASE WHEN ISNULL(@DesignationIdParam,'') = '' THEN TRY_CONVERT(VARCHAR(200), CategoryId) ELSE NULL END
    FROM dbo.MST_UserMaster
    WHERE EmployeeCode = @EmployeeCode;

    SET @RegionInput      = COALESCE(NULLIF(@RegionIdParam,''), @RegionRaw);
    SET @BranchInput      = COALESCE(NULLIF(@BranchIdParam,''), @BranchRaw);
    SET @GradeInput       = COALESCE(NULLIF(@GradeIdParam,''), @GradeRaw);
    SET @DesignationInput = COALESCE(NULLIF(@DesignationIdParam,''), @DesignationRaw);

    -- Resolve via per-table resolver procs
    IF ISNULL(@RegionInput,'') <> ''
    BEGIN
        EXEC dbo.usp_ResolveRegion @InputValue = @RegionInput, @ResolvedId = @RegionId OUTPUT;
    END

    IF ISNULL(@BranchInput,'') <> ''
    BEGIN
        EXEC dbo.usp_ResolveBranch @InputValue = @BranchInput, @ResolvedId = @BranchId OUTPUT;
    END

    IF ISNULL(@GradeInput,'') <> ''
    BEGIN
        EXEC dbo.usp_ResolveGrade @InputValue = @GradeInput, @ResolvedId = @GradeId OUTPUT;
    END

    IF ISNULL(@DesignationInput,'') <> ''
    BEGIN
        EXEC dbo.usp_ResolveDesignation @InputValue = @DesignationInput, @ResolvedId = @DesignationId OUTPUT;
    END

    -- Optional debug: uncomment during testing
    -- PRINT FORMATMESSAGE('Resolved -> Region:%d Branch:%d Grade:%d Designation:%d', @RegionId, @BranchId, @GradeId, @DesignationId);

    -- Return best-matching policy
    SELECT TOP (1) pm.*
    FROM dbo.PolicyMatrix pm
    WHERE pm.IsActive = 1
      AND (pm.EffectiveFrom IS NULL OR pm.EffectiveFrom <= CONVERT(date, GETDATE()))
      AND (pm.EffectiveTo   IS NULL OR pm.EffectiveTo   >= CONVERT(date, GETDATE()))
      AND (@RegionId IS NULL       OR pm.RegionId = @RegionId)
      AND (@BranchId IS NULL       OR pm.BranchId = @BranchId)
      AND (@GradeId IS NULL        OR pm.GradeId = @GradeId)
      AND (@DesignationId IS NULL  OR pm.DesignationId = @DesignationId)
    ORDER BY pm.Priority DESC, pm.EffectiveFrom DESC;
END
