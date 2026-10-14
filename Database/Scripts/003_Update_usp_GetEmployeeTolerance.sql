/*
    Optional follow-up for dbo.usp_GetEmployeeTolerance.
    Apply only after 001_Add_MaxKmModificationCount.sql.

    Ensure the SELECT returns MaxKmModificationCount.
*/

/*
ALTER PROCEDURE dbo.usp_GetEmployeeTolerance
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ToleranceId,
        EmployeeCode,
        MinAllowedKM,
        MaxAllowedKM,
        ISNULL(MaxKmModificationCount, 2) AS MaxKmModificationCount,
        EffectiveFrom,
        EffectiveTo,
        CreatedBy,
        CreatedDateTime,
        ModifiedBy,
        ModifiedDateTime,
        IsActive
    FROM dbo.MST_EmployeeTolerance WITH (NOLOCK)
    WHERE IsActive = 1
    ORDER BY EmployeeCode, EffectiveFrom DESC;
END
*/
