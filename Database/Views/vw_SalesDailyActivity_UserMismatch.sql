/*
    Object:      [dbo].[vw_SalesDailyActivity_UserMismatch]
    Type:        VIEW
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE   VIEW [dbo].[vw_SalesDailyActivity_UserMismatch] 
AS
SELECT 
    SDA.ActivityId,
    SDA.ActivityDate,
    SDA.Login_Id,
    
    -- Employee Name Check
    SDA.User_Name AS [AsIs_UserName], 
    UM.EmployeeName AS [ToBe_UserName],
    
    -- Level 1 Manager Check
    SDA.Reporting_Manager1 AS [AsIs_Reporting_Manager1], 
    UM.ReportingManager AS [ToBe_Reporting_Manager1],
    SDA.Reporting_Manager1_Code AS [AsIs_Reporting_Manager1_Code],
    UM.ReportingManagerCode AS [ToBe_Reporting_Manager1_Code],

    -- Level 2 Manager Check
    SDA.Reporting_Manager2 AS [AsIs_Reporting_Manager2],
    UM.ReportingManagersManagerName AS [ToBe_Reporting_Manager2],
    SDA.Reporting_Manager2_Code AS [AsIs_Reporting_Manager2_Code],
    UM.ReportingManagersManagerCode AS [ToBe_Reporting_Manager2_Code]

FROM [dbo].[SalesDailyActivity] SDA
INNER JOIN [dbo].[MST_UserMaster] UM 
    ON SDA.Login_Id = UM.EmployeeCode
WHERE 
    (SDA.User_Name <> UM.EmployeeName OR SDA.User_Name IS NULL) OR
    (SDA.Reporting_Manager1 <> UM.ReportingManager OR SDA.Reporting_Manager1 IS NULL) OR
    (SDA.Reporting_Manager1_Code <> UM.ReportingManagerCode OR SDA.Reporting_Manager1_Code IS NULL) OR
    (SDA.Reporting_Manager2 <> UM.ReportingManagersManagerName OR SDA.Reporting_Manager2 IS NULL) OR
    (SDA.Reporting_Manager2_Code <> UM.ReportingManagersManagerCode OR SDA.Reporting_Manager2_Code IS NULL);
