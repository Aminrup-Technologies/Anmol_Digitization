/*
    KM Modification Enhancement
    Adds configurable per-activity modification count to employee tolerance.

    MinAllowedKM and MaxAllowedKM column names are unchanged.
    Application code interprets them as reduction/increase tolerance values.
*/

IF COL_LENGTH('dbo.MST_EmployeeTolerance', 'MaxKmModificationCount') IS NULL
BEGIN
    ALTER TABLE dbo.MST_EmployeeTolerance
    ADD MaxKmModificationCount INT NOT NULL
        CONSTRAINT DF_MST_EmployeeTolerance_MaxKmModificationCount DEFAULT (1);
END
GO
