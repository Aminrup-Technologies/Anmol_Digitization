-- ========================================================================
-- When:    2026-08-25
-- What:    Register all ITSM permission keys in dbo.Permission.
--          UI permissions control sidebar/navigation visibility.
--          Business permissions control page-level authorization.
-- Why:     Centralizes ITSM permission definitions for the RBAC model.
--          Idempotent: safe to execute multiple times.
-- ========================================================================

SET NOCOUNT ON;
GO

-- ============================================================================
-- UI/Navigation Permissions (sidebar visibility via PermissionHelper)
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_Module.View')
BEGIN
    INSERT INTO dbo.Permission
    (PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete)
    VALUES
    (N'UI.ITSM_Module.View', N'ITSM sidebar module', N'UI', N'ITSM/UI/TicketList.aspx', N'ITSM_Module', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_TicketList.View')
BEGIN
    INSERT INTO dbo.Permission
    (PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete)
    VALUES
    (N'UI.ITSM_TicketList.View', N'ITSM ticket queue', N'UI', N'ITSM/UI/TicketList.aspx', N'ITSM_TicketList', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_CreateTicket.View')
BEGIN
    INSERT INTO dbo.Permission
    (PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete)
    VALUES
    (N'UI.ITSM_CreateTicket.View', N'ITSM create ticket', N'UI', N'ITSM/UI/CreateTicket.aspx', N'ITSM_CreateTicket', 0);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_ApprovalConfig.View')
BEGIN
    INSERT INTO dbo.Permission
    (PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete)
    VALUES
    (N'UI.ITSM_ApprovalConfig.View', N'ITSM approval config navigation', N'UI', N'ITSM/UI/ApprovalConfig.aspx', N'ITSM_ApprovalConfig', 0);
END
GO

-- ============================================================================
-- Business/Page Permissions (enforced via BasePage.HasPermission/RequirePermission)
-- ============================================================================

-- NOTE: ITSM.Ticket.View is registered by ITSM_TicketView_Permission.sql
-- (owned by PR #172). Do NOT duplicate it here.

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'ITSM.ApprovalConfig.Manage')
BEGIN
    INSERT INTO dbo.Permission
    (PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete)
    VALUES
    (N'ITSM.ApprovalConfig.Manage', N'ITSM approval configuration management', N'ITSM', N'ITSM/UI/ApprovalConfig.aspx', NULL, 0);
END
GO
