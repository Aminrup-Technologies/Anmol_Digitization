/*
    ITSM sidebar visibility keys for Dristi.Master.

    PermissionHelper.ApplyUiPermissions maps each runat=server control ID to
    UI.{ControlId}.View. If the key is absent from dbo.Permission, the control
    keeps its markup default (visible="false"). If the key exists but the user
    is not granted it, the item stays hidden.

    Control IDs:
      ITSM_Module        -> UI.ITSM_Module.View
      ITSM_TicketList    -> UI.ITSM_TicketList.View
      ITSM_CreateTicket  -> UI.ITSM_CreateTicket.View

    Grants SuperAdmin (RoleId = 1) only. Other roles must be assigned through
    RolePermissionManager. Does not grant every authenticated user.

    Does not backfill Ticket.CompanyId and does not change ticket authorization.
*/

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_Module.View')
BEGIN
    INSERT INTO dbo.Permission
    (
          PermissionKey
        , PermissionName
        , PermissionType
        , PagePath
        , ControlId
        , IsObsolete
        , CreatedBy
        , CreatedAtUTC
    )
    VALUES
    (
          N'UI.ITSM_Module.View'
        , N'ITSM sidebar module'
        , N'UI'
        , N'ITSM/UI/TicketList.aspx'
        , N'ITSM_Module'
        , 0
        , N'SYSTEM'
        , SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_TicketList.View')
BEGIN
    INSERT INTO dbo.Permission
    (
          PermissionKey
        , PermissionName
        , PermissionType
        , PagePath
        , ControlId
        , IsObsolete
        , CreatedBy
        , CreatedAtUTC
    )
    VALUES
    (
          N'UI.ITSM_TicketList.View'
        , N'ITSM ticket queue'
        , N'UI'
        , N'ITSM/UI/TicketList.aspx'
        , N'ITSM_TicketList'
        , 0
        , N'SYSTEM'
        , SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'UI.ITSM_CreateTicket.View')
BEGIN
    INSERT INTO dbo.Permission
    (
          PermissionKey
        , PermissionName
        , PermissionType
        , PagePath
        , ControlId
        , IsObsolete
        , CreatedBy
        , CreatedAtUTC
    )
    VALUES
    (
          N'UI.ITSM_CreateTicket.View'
        , N'ITSM create ticket'
        , N'UI'
        , N'ITSM/UI/CreateTicket.aspx'
        , N'ITSM_CreateTicket'
        , 0
        , N'SYSTEM'
        , SYSUTCDATETIME()
    );
END
GO

INSERT INTO dbo.RolePermission (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM dbo.Role r
CROSS JOIN dbo.Permission p
WHERE r.RoleId = 1 -- SuperAdmin only
  AND p.PermissionKey IN (
        N'UI.ITSM_Module.View',
        N'UI.ITSM_TicketList.View',
        N'UI.ITSM_CreateTicket.View')
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.RolePermission rp
      WHERE rp.RoleId = r.RoleId
        AND rp.PermissionId = p.PermissionId
  );
GO
