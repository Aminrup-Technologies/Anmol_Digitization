/*
    D_Rollback_VMS_AfterHoursApprovalAudit.sql

    Purpose : Undo A_Apply_VMS_AfterHoursApprovalAudit.sql ONLY.
    Target  : database AnmolDristi on server MUMANMOAPP01\SQLEXPRESS ONLY.
              The owner has not authorized that target. These guards are unchanged
              and are not permission to execute.

    DROP TABLE dbo.VMS_AfterHoursApprovalAudit removes that table and every index,
    constraint, and trigger that belongs to it. This script does not delete audit
    rows. The emptiness check and the DROP share one script-owned transaction.
    An exclusive table lock (TABLOCKX, HOLDLOCK) is taken before the row count
    and is held until that transaction commits or rolls back, so an insert cannot
    land between the check and the DROP. A non-empty table aborts. The transaction
    rolls back and the rows stay.

    Deployment prerequisite, unresolved: VMS_VisitLog.HostEmployeeCode width is
    not defined in this repository. HostEmpCode remains varchar(50). Confirm
    stored host codes fit varchar(50) before any execution of the apply script.

    Execution: DISABLED. @Execute defaults to 0 and returns before any
    catalog read or change.

    Status: PREPARED - not executed by the authoring session.
*/

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Execute bit = 0;

IF @Execute <> 1
BEGIN
    PRINT N'NOT RUN | @Execute is 0. dbo.VMS_AfterHoursApprovalAudit was not dropped.';
    RETURN;
END;

/*
    THROW under XACT_ABORT ON would roll back a caller-owned transaction.
    Return instead. This script commits or rolls back only a transaction it begins.
*/
IF @@TRANCOUNT <> 0
BEGIN
    PRINT N'ABORT 59510: @@TRANCOUNT is not 0. This script must start with no open transaction. Nothing was dropped, and the caller transaction was not committed or rolled back.';
    RETURN;
END;

DECLARE @Db     sysname       = DB_NAME();
DECLARE @Server nvarchar(128) = CONVERT(nvarchar(128), SERVERPROPERTY('ServerName'));

IF @Db <> N'AnmolDristi'
    THROW 59511, N'ABORT: connected database is not AnmolDristi. Nothing dropped.', 1;

IF @Server <> N'MUMANMOAPP01\SQLEXPRESS'
    THROW 59512, N'ABORT: server is not MUMANMOAPP01\SQLEXPRESS. Nothing dropped.', 1;

BEGIN TRY
    DECLARE @AuditRows int;

    BEGIN TRAN;

    IF OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit', N'U') IS NULL
    BEGIN
        ROLLBACK TRAN;
        PRINT N'SKIPPED | dbo.VMS_AfterHoursApprovalAudit does not exist. Nothing to roll back.';
        RETURN;
    END

    /* Held until COMMIT or ROLLBACK. Covers the count and the DROP. */
    SELECT @AuditRows = COUNT(*)
    FROM dbo.VMS_AfterHoursApprovalAudit WITH (TABLOCKX, HOLDLOCK);

    IF @AuditRows <> 0
    BEGIN
        ROLLBACK TRAN;
        THROW 59513, N'ABORT: dbo.VMS_AfterHoursApprovalAudit contains audit rows. Owner decision required before dropping. Transaction rolled back; no rows were deleted.', 1;
    END

    /* Removes the table and every index, constraint, and trigger that belongs to it. */
    DROP TABLE dbo.VMS_AfterHoursApprovalAudit;

    IF OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit', N'U') IS NOT NULL
    BEGIN
        ROLLBACK TRAN;
        THROW 59514, N'ABORT: table still present after DROP. Transaction rolled back.', 1;
    END

    COMMIT TRAN;
    PRINT N'COMMITTED | dbo.VMS_AfterHoursApprovalAudit dropped from ' + @Db
        + N'. The drop removed that table and every index, constraint, and trigger belonging to it. The table was empty; no audit rows were deleted. No other object was changed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;
    THROW;
END CATCH
