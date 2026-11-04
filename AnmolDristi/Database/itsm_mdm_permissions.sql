-- ========================================================================
-- When:    2026-08-25
-- What:    Register UI.ITSM_MasterData.View and ITSM.MasterData.Manage
--          permissions. Assign both to ITSMAdmin and SuperAdmin roles.
-- Why:     ITSM Phase 1B — Master Data Management navigation and page
--          authorization foundation. UI.ITSM_MasterData.View controls
--          sidebar navigation visibility. ITSM.MasterData.Manage gates
--          access to MasterData.aspx and future MDM CRUD pages.
-- Idempotent: Safe to execute repeatedly. Uses IF NOT EXISTS guards.
--             Resolves RoleId and PermissionId by name/key, never
--             hard-codes identity values.
-- ========================================================================
USE AnmolDristiDev;
GO

-- ──────────────────────────────────────────────────────────────────────
-- 1. Register permissions
-- ──────────────────────────────────────────────────────────────────────

-- UI.ITSM_MasterData.View — sidebar navigation visibility
IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_MasterData.View')
BEGIN
    INSERT INTO dbo.Permission (PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete)
    VALUES (N'UI.ITSM_MasterData.View', N'ITSM Master Data navigation', N'UI', N'~/ITSM/UI/MasterData.aspx', N'ITSM_MasterData', 0);
END
GO

-- ITSM.MasterData.Manage — page-level MDM authorization
IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'ITSM.MasterData.Manage')
BEGIN
    INSERT INTO dbo.Permission (PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete)
    VALUES (N'ITSM.MasterData.Manage', N'ITSM Master Data Management', N'Business', N'~/ITSM/UI/MasterData.aspx', NULL, 0);
END
GO

-- ──────────────────────────────────────────────────────────────────────
-- 2. Assign ITSMAdmin role
-- ──────────────────────────────────────────────────────────────────────

-- ITSMAdmin gets UI.ITSM_MasterData.View
IF NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    JOIN dbo.Role r ON rp.RoleId = r.RoleId
    JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
    WHERE r.RoleKey = N'ITSMAdmin' AND p.PermissionKey = N'UI.ITSM_MasterData.View'
)
BEGIN
    INSERT INTO dbo.RolePermission (RoleId, PermissionId)
    SELECT r.RoleId, p.PermissionId
    FROM dbo.Role r, dbo.Permission p
    WHERE r.RoleKey = N'ITSMAdmin' AND p.PermissionKey = N'UI.ITSM_MasterData.View';
END
GO

-- ITSMAdmin gets ITSM.MasterData.Manage
IF NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    JOIN dbo.Role r ON rp.RoleId = r.RoleId
    JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
    WHERE r.RoleKey = N'ITSMAdmin' AND p.PermissionKey = N'ITSM.MasterData.Manage'
)
BEGIN
    INSERT INTO dbo.RolePermission (RoleId, PermissionId)
    SELECT r.RoleId, p.PermissionId
    FROM dbo.Role r, dbo.Permission p
    WHERE r.RoleKey = N'ITSMAdmin' AND p.PermissionKey = N'ITSM.MasterData.Manage';
END
GO

-- ──────────────────────────────────────────────────────────────────────
-- 3. Assign SuperAdmin role (RoleId = 1)
-- ──────────────────────────────────────────────────────────────────────

-- SuperAdmin gets UI.ITSM_MasterData.View
IF NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
    WHERE rp.RoleId = 1 AND p.PermissionKey = N'UI.ITSM_MasterData.View'
)
BEGIN
    INSERT INTO dbo.RolePermission (RoleId, PermissionId)
    SELECT 1, p.PermissionId
    FROM dbo.Permission p
    WHERE p.PermissionKey = N'UI.ITSM_MasterData.View';
END
GO

-- SuperAdmin gets ITSM.MasterData.Manage
IF NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
    WHERE rp.RoleId = 1 AND p.PermissionKey = N'ITSM.MasterData.Manage'
)
BEGIN
    INSERT INTO dbo.RolePermission (RoleId, PermissionId)
    SELECT 1, p.PermissionId
    FROM dbo.Permission p
    WHERE p.PermissionKey = N'ITSM.MasterData.Manage';
END
GO

-- ITSM_Agent must NOT receive MDM permissions — intentionally omitted.
