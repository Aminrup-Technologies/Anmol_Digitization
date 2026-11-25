-- ========================================================================
-- When:    2026-11-25
-- What:    Register ITSM category-administration permissions and grant
--          them to ITSMAdmin only.
-- Why:     Phase 1B R1. Sidebar control ITSM_CategoryConfig maps to
--          UI.ITSM_CategoryConfig.View. CategoryConfig.aspx requires
--          ITSM.CategoryConfig.Manage on load and on every save.
--          ITSM_Agent is intentionally not granted either key.
-- Idempotent: Safe to execute repeatedly. Resolves role and permission
--             by key, never by a hard-coded id.
-- This script is not executed by the application build.
-- ========================================================================

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_CategoryConfig.View')
BEGIN
    INSERT INTO dbo.Permission
    (PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete)
    VALUES
    (N'UI.ITSM_CategoryConfig.View', N'ITSM category configuration navigation', N'UI', N'ITSM/UI/CategoryConfig.aspx', N'ITSM_CategoryConfig', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'ITSM.CategoryConfig.Manage')
BEGIN
    INSERT INTO dbo.Permission
    (PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete)
    VALUES
    (N'ITSM.CategoryConfig.Manage', N'ITSM category configuration management', N'ITSM', N'ITSM/UI/CategoryConfig.aspx', NULL, 0);
END
GO

DECLARE @AdminRoleId INT;
SELECT @AdminRoleId = RoleId FROM dbo.Role WHERE RoleKey = N'ITSMAdmin';

IF @AdminRoleId IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        INNER JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
        WHERE rp.RoleId = @AdminRoleId AND p.PermissionKey = N'UI.ITSM_CategoryConfig.View'
    )
    BEGIN
        INSERT INTO dbo.RolePermission (RoleId, PermissionId)
        SELECT @AdminRoleId, PermissionId FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_CategoryConfig.View';
    END

    IF NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        INNER JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
        WHERE rp.RoleId = @AdminRoleId AND p.PermissionKey = N'ITSM.CategoryConfig.Manage'
    )
    BEGIN
        INSERT INTO dbo.RolePermission (RoleId, PermissionId)
        SELECT @AdminRoleId, PermissionId FROM dbo.Permission WHERE PermissionKey = N'ITSM.CategoryConfig.Manage';
    END
END
GO
