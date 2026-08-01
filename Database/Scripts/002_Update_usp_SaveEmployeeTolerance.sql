/*
    Optional follow-up for dbo.usp_SaveEmployeeTolerance.
    Apply only after 001_Add_MaxKmModificationCount.sql.

    Extend the existing procedure to accept @MaxKmModificationCount.
    Adjust the body to persist the new column; keep all existing behavior.
*/

/*
ALTER PROCEDURE dbo.usp_SaveEmployeeTolerance
    @ToleranceId INT,
    @EmployeeCode VARCHAR(50),
    @MinAllowedKM DECIMAL(18, 2) = NULL,
    @MaxAllowedKM DECIMAL(18, 2) = NULL,
    @EffectiveFrom DATE = NULL,
    @EffectiveTo DATE = NULL,
    @ModifiedBy VARCHAR(100),
    @MaxKmModificationCount INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF ISNULL(@MaxKmModificationCount, 0) < 1
        SET @MaxKmModificationCount = 1;

    -- existing insert/update logic ...
    -- include MaxKmModificationCount in INSERT/UPDATE column list
END
*/
