-- ========================================================================
-- When:    2026-08-25
-- What:    Register ITSM.Queue.View and ITSM.Approval.View permissions.
--          Assign ITSM.Queue.View to ITSM_Agent and ITSMAdmin.
--          Assign ITSM.Approval.View to ITSMAdmin only.
-- Why:     Phase 1B — TicketList tab visibility is now permission-gated.
--          My Queue and All Open require ITSM.Queue.View.
--          Pending Approvals requires ITSM.Approval.View.
--          My Tickets remains visible to all authenticated employees.
--          ITSM module navigation is always visible (code override).
-- Idempotent: Safe to execute repeatedly. Uses IF NOT EXISTS guards.
--             Resolves RoleId and PermissionId by name, never hard-codes.
-- ========================================================================
USE AnmolDristiDev;
GO

-- ──────────────────────────────────────────────────────────────────────
-- 1. Register new permissions
-- ──────────────────────────────────────────────────────────────────────

-- ITSM.Queue.View — gates My Queue and All Open tabs in TicketList
IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'ITSM.Queue.View')
BEGIN
    INSERT INTO dbo.Permission (PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete)
    VALUES (N'ITSM.Queue.View', N'ITSM Queue View', N'Business', N'~/ITSM/UI/TicketList.aspx', NULL, 0);
END
GO

-- ITSM.Approval.View — gates Pending Approvals tab in TicketList
IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'ITSM.Approval.View')
BEGIN
    INSERT INTO dbo.Permission (PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete)
    VALUES (N'ITSM.Approval.View', N'ITSM Approval View', N'Business', N'~/ITSM/UI/TicketList.aspx', NULL, 0);
END
GO

-- ──────────────────────────────────────────────────────────────────────
-- 2. Assign permissions to roles
-- ──────────────────────────────────────────────────────────────────────

-- ITSM_Agent gets ITSM.Queue.View (My Queue + All Open)
IF NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    JOIN dbo.Role r ON rp.RoleId = r.RoleId
    JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
    WHERE r.RoleKey = N'ITSM_Agent' AND p.PermissionKey = N'ITSM.Queue.View'
)
BEGIN
    INSERT INTO dbo.RolePermission (RoleId, PermissionId)
    SELECT r.RoleId, p.PermissionId
    FROM dbo.Role r, dbo.Permission p
    WHERE r.RoleKey = N'ITSM_Agent' AND p.PermissionKey = N'ITSM.Queue.View';
END
GO

-- ITSMAdmin gets ITSM.Queue.View (My Queue + All Open)
IF NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    JOIN dbo.Role r ON rp.RoleId = r.RoleId
    JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
    WHERE r.RoleKey = N'ITSMAdmin' AND p.PermissionKey = N'ITSM.Queue.View'
)
BEGIN
    INSERT INTO dbo.RolePermission (RoleId, PermissionId)
    SELECT r.RoleId, p.PermissionId
    FROM dbo.Role r, dbo.Permission p
    WHERE r.RoleKey = N'ITSMAdmin' AND p.PermissionKey = N'ITSM.Queue.View';
END
GO

-- ITSMAdmin gets ITSM.Approval.View (Pending Approvals)
IF NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    JOIN dbo.Role r ON rp.RoleId = r.RoleId
    JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
    WHERE r.RoleKey = N'ITSMAdmin' AND p.PermissionKey = N'ITSM.Approval.View'
)
BEGIN
    INSERT INTO dbo.RolePermission (RoleId, PermissionId)
    SELECT r.RoleId, p.PermissionId
    FROM dbo.Role r, dbo.Permission p
    WHERE r.RoleKey = N'ITSMAdmin' AND p.PermissionKey = N'ITSM.Approval.View';
END
GO

-- ITSM_Agent must NOT get ITSM.Approval.View — intentionally omitted.
