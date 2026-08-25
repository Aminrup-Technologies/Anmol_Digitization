-- ========================================================================
-- When:    2026-08-25
-- What:    Assign ITSM permissions to ITSM_Agent and ITSMAdmin roles.
--          Uses PermissionKey and RoleKey lookups (no hard-coded IDs).
-- Why:     Establishes the RBAC boundary for ITSM navigation and
--          authorization. Idempotent: safe to execute multiple times.
-- ========================================================================

SET NOCOUNT ON;
GO

-- ============================================================================
-- ITSM_Agent Role (RoleId = 19)
-- Permissions: UI navigation + ticket detail view
-- Must NOT receive: ApprovalConfig view/manage
-- ============================================================================

DECLARE @AgentRoleId INT;
SELECT @AgentRoleId = RoleId FROM dbo.Role WHERE RoleKey = N'ITSM_Agent';

IF @AgentRoleId IS NOT NULL
BEGIN
    -- UI.ITSM_Module.View
    IF NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        INNER JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
        WHERE rp.RoleId = @AgentRoleId AND p.PermissionKey = N'UI.ITSM_Module.View'
    )
    BEGIN
        INSERT INTO dbo.RolePermission (RoleId, PermissionId)
        SELECT @AgentRoleId, PermissionId FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_Module.View';
    END

    -- UI.ITSM_TicketList.View
    IF NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        INNER JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
        WHERE rp.RoleId = @AgentRoleId AND p.PermissionKey = N'UI.ITSM_TicketList.View'
    )
    BEGIN
        INSERT INTO dbo.RolePermission (RoleId, PermissionId)
        SELECT @AgentRoleId, PermissionId FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_TicketList.View';
    END

    -- UI.ITSM_CreateTicket.View
    IF NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        INNER JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
        WHERE rp.RoleId = @AgentRoleId AND p.PermissionKey = N'UI.ITSM_CreateTicket.View'
    )
    BEGIN
        INSERT INTO dbo.RolePermission (RoleId, PermissionId)
        SELECT @AgentRoleId, PermissionId FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_CreateTicket.View';
    END

    -- ITSM.Ticket.View
    IF NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        INNER JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
        WHERE rp.RoleId = @AgentRoleId AND p.PermissionKey = N'ITSM.Ticket.View'
    )
    BEGIN
        INSERT INTO dbo.RolePermission (RoleId, PermissionId)
        SELECT @AgentRoleId, PermissionId FROM dbo.Permission WHERE PermissionKey = N'ITSM.Ticket.View';
    END

    -- NOTE: ITSM_Agent must NOT receive:
    --   UI.ITSM_ApprovalConfig.View
    --   ITSM.ApprovalConfig.Manage
END
GO

-- ============================================================================
-- ITSMAdmin Role (RoleId = 18)
-- Permissions: all 6 ITSM permissions
-- ============================================================================

DECLARE @AdminRoleId INT;
SELECT @AdminRoleId = RoleId FROM dbo.Role WHERE RoleKey = N'ITSMAdmin';

IF @AdminRoleId IS NOT NULL
BEGIN
    -- UI.ITSM_Module.View
    IF NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        INNER JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
        WHERE rp.RoleId = @AdminRoleId AND p.PermissionKey = N'UI.ITSM_Module.View'
    )
    BEGIN
        INSERT INTO dbo.RolePermission (RoleId, PermissionId)
        SELECT @AdminRoleId, PermissionId FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_Module.View';
    END

    -- UI.ITSM_TicketList.View
    IF NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        INNER JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
        WHERE rp.RoleId = @AdminRoleId AND p.PermissionKey = N'UI.ITSM_TicketList.View'
    )
    BEGIN
        INSERT INTO dbo.RolePermission (RoleId, PermissionId)
        SELECT @AdminRoleId, PermissionId FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_TicketList.View';
    END

    -- UI.ITSM_CreateTicket.View
    IF NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        INNER JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
        WHERE rp.RoleId = @AdminRoleId AND p.PermissionKey = N'UI.ITSM_CreateTicket.View'
    )
    BEGIN
        INSERT INTO dbo.RolePermission (RoleId, PermissionId)
        SELECT @AdminRoleId, PermissionId FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_CreateTicket.View';
    END

    -- UI.ITSM_ApprovalConfig.View
    IF NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        INNER JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
        WHERE rp.RoleId = @AdminRoleId AND p.PermissionKey = N'UI.ITSM_ApprovalConfig.View'
    )
    BEGIN
        INSERT INTO dbo.RolePermission (RoleId, PermissionId)
        SELECT @AdminRoleId, PermissionId FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_ApprovalConfig.View';
    END

    -- ITSM.Ticket.View
    IF NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        INNER JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
        WHERE rp.RoleId = @AdminRoleId AND p.PermissionKey = N'ITSM.Ticket.View'
    )
    BEGIN
        INSERT INTO dbo.RolePermission (RoleId, PermissionId)
        SELECT @AdminRoleId, PermissionId FROM dbo.Permission WHERE PermissionKey = N'ITSM.Ticket.View';
    END

    -- ITSM.ApprovalConfig.Manage
    IF NOT EXISTS (
        SELECT 1 FROM dbo.RolePermission rp
        INNER JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
        WHERE rp.RoleId = @AdminRoleId AND p.PermissionKey = N'ITSM.ApprovalConfig.Manage'
    )
    BEGIN
        INSERT INTO dbo.RolePermission (RoleId, PermissionId)
        SELECT @AdminRoleId, PermissionId FROM dbo.Permission WHERE PermissionKey = N'ITSM.ApprovalConfig.Manage';
    END
END
GO
