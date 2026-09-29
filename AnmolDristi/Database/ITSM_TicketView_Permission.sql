-- ========================================================================
-- When:    2026-08-24
-- What:    Register ITSM.Ticket.View permission in dbo.Permission.
-- Why:     Enables RBAC-gated ticket detail access for authorized ITSM
--          users. The permission is tenant-scoped: a user with this
--          permission can view tickets belonging to their authorized
--          company, not arbitrary tickets.
-- Run:     Execute against AnmolDristiDev after deploying the BLL changes.
-- ========================================================================

-- Idempotent insert: only adds the permission if it does not already exist.
IF NOT EXISTS (
    SELECT 1 FROM dbo.Permission WHERE PermissionKey = 'ITSM.Ticket.View'
)
BEGIN
    INSERT INTO dbo.Permission (PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete)
    VALUES ('ITSM.Ticket.View', 'ITSM Ticket View', 'ITSM', '~/ITSM/UI/TicketDetail.aspx', NULL, 0);
END
GO

-- ========================================================================
-- Role assignment guidance:
--
-- The permission must be assigned to the appropriate role(s) via
-- dbo.RolePermission. Do NOT hard-code role IDs.
--
-- Typical assignment:
--   ITSM Agent role      → receives ITSM.Ticket.View
--   ITSM Manager role    → receives ITSM.Ticket.View
--   General Employee role → does NOT receive ITSM.Ticket.View
--
-- Use the existing PermissionService.AssignPermissionToRole() or
-- the RolePermission admin UI to assign after deploying.
-- ========================================================================
