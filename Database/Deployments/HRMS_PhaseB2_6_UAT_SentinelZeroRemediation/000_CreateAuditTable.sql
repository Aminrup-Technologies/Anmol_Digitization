/*
    Package:     Database/Deployments/HRMS_PhaseB2_6_UAT_SentinelZeroRemediation
    Script:      000_CreateAuditTable.sql
    Phase:       B2.6 — UAT sentinel-zero historical data-contract remediation
    Database:    AnmolDristiDev ONLY
    Live:        FORBIDDEN

    Creates dbo.AUDIT_UserMasterOrgSentinelZero_B26 if missing.
    Do NOT reuse dbo.AUDIT_UserMasterOrgRemediation_B23.
    Not an application runtime table. Do not create on AnmolDristi.

    NewValue = 0 is required in this phase (unlike B2.3).
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() = N'AnmolDristi'
    THROW 56601, N'ABORT: Live database AnmolDristi is forbidden for Phase B2.6.', 1;

IF DB_NAME() <> N'AnmolDristiDev'
BEGIN
    DECLARE @dbMsg nvarchar(400) =
        N'ABORT: connected to ' + QUOTENAME(DB_NAME()) + N'. Expected [AnmolDristiDev].';
    THROW 56602, @dbMsg, 1;
END;

IF OBJECT_ID(N'dbo.AUDIT_UserMasterOrgRemediation_B23', N'U') IS NOT NULL
    AND OBJECT_ID(N'dbo.AUDIT_UserMasterOrgSentinelZero_B26', N'U') IS NULL
    PRINT N'B2.3 audit object present and will not be reused.';

IF OBJECT_ID(N'dbo.AUDIT_UserMasterOrgSentinelZero_B26', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AUDIT_UserMasterOrgSentinelZero_B26
    (
        AuditId                  int IDENTITY(1,1) NOT NULL,
        RemediationRunId         varchar(40)      NOT NULL,
        AppliedAt                datetime2(7)     NOT NULL,
        UserMasterId             int              NOT NULL,
        EmployeeCode             varchar(50)      NOT NULL,
        ColumnName               sysname          NOT NULL,
        OldValue                 int              NOT NULL,
        NewValue                 int              NOT NULL,
        TargetMasterTable        sysname          NOT NULL,
        TargetMasterInternalId   int              NOT NULL,
        TargetERPValue           int              NOT NULL,
        Classification           varchar(40)      NOT NULL,
        ApplyStatus              varchar(20)      NOT NULL,
        ValidationEvidence       nvarchar(400)    NULL,
        CONSTRAINT PK_AUDIT_UserMasterOrgSentinelZero_B26 PRIMARY KEY CLUSTERED (AuditId),
        CONSTRAINT CK_AUDIT_B26_ApplyStatus CHECK (ApplyStatus IN (N'PENDING', N'APPLIED', N'FAILED', N'ROLLED_BACK')),
        CONSTRAINT CK_AUDIT_B26_OldNewDiff CHECK (OldValue <> NewValue),
        CONSTRAINT CK_AUDIT_B26_NewIsZero CHECK (NewValue = 0),
        CONSTRAINT CK_AUDIT_B26_TargetErpZero CHECK (TargetERPValue = 0)
    );

    CREATE NONCLUSTERED INDEX IX_AUDIT_B26_Run
        ON dbo.AUDIT_UserMasterOrgSentinelZero_B26 (RemediationRunId, ApplyStatus);

    CREATE NONCLUSTERED INDEX IX_AUDIT_B26_UserCol
        ON dbo.AUDIT_UserMasterOrgSentinelZero_B26 (UserMasterId, ColumnName);

    PRINT N'Created dbo.AUDIT_UserMasterOrgSentinelZero_B26.';
END
ELSE
BEGIN
    PRINT N'dbo.AUDIT_UserMasterOrgSentinelZero_B26 already exists — no change.';
END;

SELECT
    DB_NAME() AS DbName,
    OBJECT_ID(N'dbo.AUDIT_UserMasterOrgSentinelZero_B26', N'U') AS B26AuditObjectId,
    OBJECT_ID(N'dbo.AUDIT_UserMasterOrgRemediation_B23', N'U') AS B23AuditObjectId,
    (SELECT COUNT(*) FROM dbo.AUDIT_UserMasterOrgSentinelZero_B26) AS ExistingB26AuditRows;
