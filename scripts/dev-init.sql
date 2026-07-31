-- Minimal schema for local Anmol Dristi development (login + home flow)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'AnmolDristiDev')
BEGIN
    CREATE DATABASE AnmolDristiDev;
END
GO

USE AnmolDristiDev;
GO

IF OBJECT_ID(N'dbo.MST_UserMaster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MST_UserMaster (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeCode NVARCHAR(50) NOT NULL,
        EmployeeName NVARCHAR(200) NOT NULL,
        BranchId INT NULL,
        PlantId INT NULL,
        EmployeeStatus NVARCHAR(20) NOT NULL DEFAULT 'Active',
        CompanyId INT NULL,
        DepartmentId INT NULL,
        GradeId INT NULL,
        SubDepartmentId INT NULL,
        DOJ DATE NULL,
        CategoryId INT NULL,
        Email NVARCHAR(200) NULL,
        Mobile NVARCHAR(20) NULL,
        Gender NVARCHAR(10) NULL,
        RegionId INT NULL,
        DivisionId INT NULL,
        CreatedDate DATETIME NULL DEFAULT GETDATE(),
        CreatedBy NVARCHAR(50) NULL,
        CreationMode NVARCHAR(50) NULL,
        Password NVARCHAR(100) NOT NULL,
        LastLoginDate DATETIME NULL,
        LastLogoutDate DATETIME NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.MST_UserMaster WHERE EmployeeCode = N'DEV001')
BEGIN
    INSERT INTO dbo.MST_UserMaster (
        EmployeeCode, EmployeeName, BranchId, PlantId, EmployeeStatus,
        CompanyId, DepartmentId, GradeId, SubDepartmentId, DOJ,
        CategoryId, Email, Mobile, Gender, RegionId, DivisionId,
        CreatedBy, CreationMode, Password
    ) VALUES (
        N'DEV001', N'Dev User', 1, 1, N'Active',
        1, 1, 1, 1, '2024-01-01',
        1, N'dev@example.com', N'9999999999', N'M', 1, 1,
        N'system', N'dev', N'devpass'
    );
END
GO
