/*
    Package:     Database/Deployments/HRMS_PhaseB2_3_UAT_OrgRemediation
    Script:      000_CreateAuditTable.sql
    Phase:       B2.3 — one-time UAT historical org-column remediation
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN

    Creates dbo.AUDIT_UserMasterOrgRemediation_B23 if missing.
    Not an application runtime table. Do not create on AnmolDristi.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() = N'AnmolDristi'
BEGIN
    THROW 56201, N'ABORT: Live database AnmolDristi is forbidden for Phase B2.3.', 1;
END;

IF DB_NAME() <> N'AnmolDristiDev'
BEGIN
    DECLARE @dbMsg nvarchar(400) =
        N'ABORT: connected to ' + QUOTENAME(DB_NAME()) + N'. Expected [AnmolDristiDev].';
    THROW 56202, @dbMsg, 1;
END;

IF OBJECT_ID(N'dbo.AUDIT_UserMasterOrgRemediation_B23', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AUDIT_UserMasterOrgRemediation_B23
    (
        AuditId             int IDENTITY(1,1) NOT NULL,
        RemediationRunId    varchar(40)      NOT NULL,
        AppliedAt           datetime2(7)     NOT NULL,
        UserMasterId        int              NOT NULL,
        EmployeeCode        varchar(50)      NOT NULL,
        ColumnName          sysname          NOT NULL,
        OldValue            int              NOT NULL,
        NewValue            int              NOT NULL,
        MasterTable         sysname          NOT NULL,
        MasterInternalId    int              NOT NULL,
        MasterERPValue      int              NOT NULL,
        Classification      varchar(40)      NOT NULL,
        ApplyStatus         varchar(20)      NOT NULL,
        ValidationEvidence  nvarchar(400)    NULL,
        CONSTRAINT PK_AUDIT_UserMasterOrgRemediation_B23 PRIMARY KEY CLUSTERED (AuditId),
        CONSTRAINT CK_AUDIT_B23_ApplyStatus CHECK (ApplyStatus IN (N'PENDING', N'APPLIED', N'FAILED', N'ROLLED_BACK')),
        CONSTRAINT CK_AUDIT_B23_OldNewDiff CHECK (OldValue <> NewValue),
        CONSTRAINT CK_AUDIT_B23_NewNonZero CHECK (NewValue <> 0)
    );

    CREATE NONCLUSTERED INDEX IX_AUDIT_B23_Run
        ON dbo.AUDIT_UserMasterOrgRemediation_B23 (RemediationRunId, ApplyStatus);

    CREATE NONCLUSTERED INDEX IX_AUDIT_B23_UserCol
        ON dbo.AUDIT_UserMasterOrgRemediation_B23 (UserMasterId, ColumnName);

    PRINT N'Created dbo.AUDIT_UserMasterOrgRemediation_B23.';
END
ELSE
BEGIN
    PRINT N'dbo.AUDIT_UserMasterOrgRemediation_B23 already exists — no change.';
END;

SELECT
    DB_NAME() AS DbName,
    OBJECT_ID(N'dbo.AUDIT_UserMasterOrgRemediation_B23', N'U') AS AuditObjectId,
    (SELECT COUNT(*) FROM dbo.AUDIT_UserMasterOrgRemediation_B23) AS ExistingAuditRows;
