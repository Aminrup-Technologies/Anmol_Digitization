/*
    Policy Exception History – audit/compliance enhancements for PR #68.

    - Enrich history list projection for export/timeline/compare
    - Seed UI.PolicyException.History.View permission
    - Grant to SuperAdmin (RoleId=1) and ClaimsAdmin (RoleId=8) when present

    Deploy after 007_PolicyExceptionHistory.sql.
    Does not alter PolicyException table or policy resolution.
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.usp_GetPolicyExceptionHistory', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetPolicyExceptionHistory;
GO

CREATE PROCEDURE dbo.usp_GetPolicyExceptionHistory
(
    @ExceptionId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
          H.HistoryId
        , H.ExceptionId
        , H.VersionNo
        , H.ActionType
        , H.PolicyJson
        , H.Priority
        , H.EffectiveFrom
        , H.EffectiveTo
        , H.IsActive
        , H.ChangedBy
        , H.ChangedOn
        , H.ChangeReason
    FROM dbo.PolicyExceptionHistory H
    WHERE H.ExceptionId = @ExceptionId
    ORDER BY H.VersionNo DESC;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE PermissionKey = N'UI.PolicyException.History.View')
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
          N'UI.PolicyException.History.View'
        , N'View Policy Exception History'
        , N'Feature'
        , N'ManagePolicyException.aspx'
        , N'pnlHistory'
        , 0
        , N'SYSTEM'
        , SYSUTCDATETIME()
    );
END
GO

DECLARE @PermissionId INT =
(
    SELECT PermissionId
    FROM dbo.Permission
    WHERE PermissionKey = N'UI.PolicyException.History.View'
);

IF @PermissionId IS NOT NULL
BEGIN
    INSERT INTO dbo.RolePermission (RoleId, PermissionId)
    SELECT r.RoleId, @PermissionId
    FROM dbo.Role r
    WHERE r.RoleId IN (1, 8) -- SuperAdmin, ClaimsAdmin
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.RolePermission rp
          WHERE rp.RoleId = r.RoleId
            AND rp.PermissionId = @PermissionId
      );
END
GO
