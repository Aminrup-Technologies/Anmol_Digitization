/*
    A_Apply_VMS_AfterHoursApprovalAudit.sql

    Purpose : Create the missing audit table dbo.VMS_AfterHoursApprovalAudit.
    Target  : database AnmolDristi on server MUMANMOAPP01\SQLEXPRESS ONLY.
              The owner has not authorized that target. These guards are unchanged
              and are not permission to execute.
    Source  : AnmolDristi/Database/VMS_AttendanceHistory_AfterHoursAudit_Migration.sql
              introduced by commit c13b908. Migration-file blob
              fb712020dd5bdeb20c7226d9ab67a66bc2861df1 is identical at c13b908,
              at 38fd117, and at current HEAD 8cba843067a2b9bce254d5b41ba5d4f745e4044c.
              Commit 38fd117 is not an ancestor of current HEAD.

    Deployment prerequisite, unresolved: this repository does not define
    VMS_VisitLog.HostEmployeeCode. HostEmpCode stays varchar(50). The application
    insert does not truncate the host code it copies from VMS_VisitLog. Confirm
    that stored host codes fit varchar(50) before execution. This script does not
    widen the column and does not change application code.

    Scope limits enforced below:
      - returns before any catalog read while @Execute is not 1;
      - returns without COMMIT or ROLLBACK when a caller transaction is open;
      - aborts unless the connected database is exactly AnmolDristi;
      - aborts unless the connected server is exactly MUMANMOAPP01\SQLEXPRESS;
      - if the table already exists, compares it with the approved shape and
        aborts without ALTER, DROP, or repair when any part differs;
      - a matching existing table is left unchanged;
      - creates ONLY this table, its clustered PK, its nonclustered index and its
        default constraint;
      - inserts no rows and touches no other table, view, procedure, function,
        trigger, user, role or permission;
      - DDL runs in one transaction and is rolled back if the post-check fails.

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
    PRINT N'NOT RUN | @Execute is 0. dbo.VMS_AfterHoursApprovalAudit was not created or altered.';
    RETURN;
END;

/*
    THROW under XACT_ABORT ON would roll back a caller-owned transaction.
    Return instead. This script commits or rolls back only a transaction it begins.
*/
IF @@TRANCOUNT <> 0
BEGIN
    PRINT N'ABORT 59500: @@TRANCOUNT is not 0. This script must start with no open transaction. No object was created or altered, and the caller transaction was not committed or rolled back.';
    RETURN;
END;

DECLARE @Db     sysname       = DB_NAME();
DECLARE @Server nvarchar(128) = CONVERT(nvarchar(128), SERVERPROPERTY('ServerName'));

/* ---- Precondition 1: the connected database must be the authorized target ---- */
IF @Db <> N'AnmolDristi'
    THROW 59501, N'ABORT: connected database is not AnmolDristi. No object created.', 1;

/* ---- Precondition 2: the connected server must be the authorized instance ---- */
IF @Server <> N'MUMANMOAPP01\SQLEXPRESS'
    THROW 59502, N'ABORT: server is not MUMANMOAPP01\SQLEXPRESS. No object created.', 1;

/* ---- Existing table: validate the approved shape. Never alter or recreate it. ---- */
IF OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit', N'U') IS NOT NULL
BEGIN
    DECLARE @ColumnMatch int;

    SELECT @ColumnMatch = COUNT(*)
    FROM sys.columns AS c
    INNER JOIN sys.types AS ty
        ON ty.user_type_id = c.user_type_id
       AND ty.is_user_defined = 0
    WHERE c.object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')
      AND c.is_computed = 0
      AND (
            (c.column_id = 1  AND c.name = N'AuditID'        AND ty.name = N'int'      AND c.max_length = 4   AND c.is_nullable = 0 AND c.is_identity = 1)
         OR (c.column_id = 2  AND c.name = N'LogID'          AND ty.name = N'int'      AND c.max_length = 4   AND c.is_nullable = 0 AND c.is_identity = 0)
         OR (c.column_id = 3  AND c.name = N'DayLogID'       AND ty.name = N'int'      AND c.max_length = 4   AND c.is_nullable = 1 AND c.is_identity = 0)
         OR (c.column_id = 4  AND c.name = N'BranchID'       AND ty.name = N'int'      AND c.max_length = 4   AND c.is_nullable = 0 AND c.is_identity = 0)
         OR (c.column_id = 5  AND c.name = N'HostEmpCode'    AND ty.name = N'varchar'  AND c.max_length = 50  AND c.is_nullable = 1 AND c.is_identity = 0)
         OR (c.column_id = 6  AND c.name = N'ScheduledDate'  AND ty.name = N'date'     AND c.max_length = 3   AND c.is_nullable = 0 AND c.is_identity = 0)
         OR (c.column_id = 7  AND c.name = N'ScheduledFrom'  AND ty.name = N'datetime' AND c.max_length = 8   AND c.is_nullable = 0 AND c.is_identity = 0)
         OR (c.column_id = 8  AND c.name = N'ScheduledTo'    AND ty.name = N'datetime' AND c.max_length = 8   AND c.is_nullable = 0 AND c.is_identity = 0)
         OR (c.column_id = 9  AND c.name = N'PlantOpenTime'  AND ty.name = N'time'     AND c.max_length = 5   AND c.scale = 7 AND c.is_nullable = 1 AND c.is_identity = 0)
         OR (c.column_id = 10 AND c.name = N'PlantCloseTime' AND ty.name = N'time'     AND c.max_length = 5   AND c.scale = 7 AND c.is_nullable = 1 AND c.is_identity = 0)
         OR (c.column_id = 11 AND c.name = N'ApprovedOn'     AND ty.name = N'datetime' AND c.max_length = 8   AND c.is_nullable = 0 AND c.is_identity = 0)
         OR (c.column_id = 12 AND c.name = N'ApprovedBy'     AND ty.name = N'varchar'  AND c.max_length = 50  AND c.is_nullable = 1 AND c.is_identity = 0)
         OR (c.column_id = 13 AND c.name = N'Reason'         AND ty.name = N'varchar'  AND c.max_length = 500 AND c.is_nullable = 1 AND c.is_identity = 0)
      );

    IF (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')) <> 13
       OR @ColumnMatch <> 13
        THROW 59504, N'ABORT: dbo.VMS_AfterHoursApprovalAudit exists with a different column list, order, type, length, nullability, or identity flag. Existing table was not altered.', 1;

    IF (SELECT COUNT(*) FROM sys.identity_columns WHERE object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')) <> 1
       OR NOT EXISTS (
            SELECT 1
            FROM sys.identity_columns
            WHERE object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')
              AND name = N'AuditID'
              AND seed_value = 1
              AND increment_value = 1
          )
        THROW 59504, N'ABORT: dbo.VMS_AfterHoursApprovalAudit identity is not AuditID IDENTITY(1,1). Existing table was not altered.', 1;

    IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes AS i
            WHERE i.object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')
              AND i.name = N'PK_VMS_AfterHoursApprovalAudit'
              AND i.type_desc = N'CLUSTERED'
              AND i.is_primary_key = 1
              AND i.is_unique = 1
          )
       OR (
            SELECT COUNT(*)
            FROM sys.index_columns AS ic
            INNER JOIN sys.indexes AS i
                ON i.object_id = ic.object_id
               AND i.index_id = ic.index_id
            WHERE i.object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')
              AND i.name = N'PK_VMS_AfterHoursApprovalAudit'
              AND ic.is_included_column = 0
              AND ic.is_descending_key = 0
              AND ic.key_ordinal = 1
              AND COL_NAME(ic.object_id, ic.column_id) = N'AuditID'
          ) <> 1
       OR (
            SELECT COUNT(*)
            FROM sys.index_columns AS ic
            INNER JOIN sys.indexes AS i
                ON i.object_id = ic.object_id
               AND i.index_id = ic.index_id
            WHERE i.object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')
              AND i.name = N'PK_VMS_AfterHoursApprovalAudit'
          ) <> 1
        THROW 59504, N'ABORT: dbo.VMS_AfterHoursApprovalAudit clustered primary key is not PK_VMS_AfterHoursApprovalAudit on AuditID ASC. Existing table was not altered.', 1;

    IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes AS i
            WHERE i.object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')
              AND i.name = N'IX_VMS_AfterHoursApprovalAudit_LogID'
              AND i.type_desc = N'NONCLUSTERED'
              AND i.is_unique = 0
              AND i.is_primary_key = 0
              AND i.is_unique_constraint = 0
              AND i.has_filter = 0
          )
       OR (
            SELECT COUNT(*)
            FROM sys.index_columns AS ic
            INNER JOIN sys.indexes AS i
                ON i.object_id = ic.object_id
               AND i.index_id = ic.index_id
            WHERE i.object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')
              AND i.name = N'IX_VMS_AfterHoursApprovalAudit_LogID'
              AND ic.is_included_column = 0
              AND ic.is_descending_key = 0
              AND (
                    (ic.key_ordinal = 1 AND COL_NAME(ic.object_id, ic.column_id) = N'LogID')
                 OR (ic.key_ordinal = 2 AND COL_NAME(ic.object_id, ic.column_id) = N'ApprovedOn')
              )
          ) <> 2
       OR (
            SELECT COUNT(*)
            FROM sys.index_columns AS ic
            INNER JOIN sys.indexes AS i
                ON i.object_id = ic.object_id
               AND i.index_id = ic.index_id
            WHERE i.object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')
              AND i.name = N'IX_VMS_AfterHoursApprovalAudit_LogID'
          ) <> 2
        THROW 59504, N'ABORT: dbo.VMS_AfterHoursApprovalAudit index IX_VMS_AfterHoursApprovalAudit_LogID is not a non-unique nonclustered key on (LogID ASC, ApprovedOn ASC). Existing table was not altered.', 1;

    IF (SELECT COUNT(*) FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')) <> 1
       OR NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints AS d
            INNER JOIN sys.columns AS c
                ON c.object_id = d.parent_object_id
               AND c.column_id = d.parent_column_id
            WHERE d.parent_object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')
              AND d.name = N'DF_VMS_AfterHoursApprovalAudit_ApprovedOn'
              AND c.name = N'ApprovedOn'
              AND d.definition = N'(getdate())'
          )
        THROW 59504, N'ABORT: dbo.VMS_AfterHoursApprovalAudit default is not DF_VMS_AfterHoursApprovalAudit_ApprovedOn on ApprovedOn with definition (getdate()). Existing table was not altered.', 1;

    PRINT N'SKIPPED | dbo.VMS_AfterHoursApprovalAudit already exists in '
        + @Db + N' and matches the approved shape. Nothing was created, dropped or modified.';
    RETURN;
END

BEGIN TRY
    BEGIN TRAN;

    CREATE TABLE dbo.VMS_AfterHoursApprovalAudit (
        AuditID         int IDENTITY(1,1) NOT NULL,
        LogID           int               NOT NULL,
        DayLogID        int               NULL,
        BranchID        int               NOT NULL,
        HostEmpCode     varchar(50)       NULL,
        ScheduledDate   date              NOT NULL,
        ScheduledFrom   datetime          NOT NULL,
        ScheduledTo     datetime          NOT NULL,
        PlantOpenTime   time(7)           NULL,
        PlantCloseTime  time(7)           NULL,
        ApprovedOn      datetime          NOT NULL
                        CONSTRAINT DF_VMS_AfterHoursApprovalAudit_ApprovedOn DEFAULT (GETDATE()),
        ApprovedBy      varchar(50)       NULL,
        Reason          varchar(500)      NULL,
        CONSTRAINT PK_VMS_AfterHoursApprovalAudit PRIMARY KEY CLUSTERED (AuditID ASC)
    );

    CREATE INDEX IX_VMS_AfterHoursApprovalAudit_LogID
        ON dbo.VMS_AfterHoursApprovalAudit (LogID, ApprovedOn);

    /* ---- Post-check: confirm the created shape before committing ---- */
    IF NOT
    (
        (SELECT COUNT(*) FROM sys.columns
          WHERE object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')) = 13
        AND EXISTS (SELECT 1 FROM sys.indexes
                     WHERE object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')
                       AND name = N'PK_VMS_AfterHoursApprovalAudit'
                       AND type_desc = N'CLUSTERED' AND is_primary_key = 1)
        AND EXISTS (SELECT 1 FROM sys.indexes
                     WHERE object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')
                       AND name = N'IX_VMS_AfterHoursApprovalAudit_LogID'
                       AND type_desc = N'NONCLUSTERED')
        AND EXISTS (SELECT 1 FROM sys.identity_columns
                     WHERE object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')
                       AND name = N'AuditID'
                       AND seed_value = 1 AND increment_value = 1)
        AND EXISTS (SELECT 1 FROM sys.default_constraints
                     WHERE name = N'DF_VMS_AfterHoursApprovalAudit_ApprovedOn'
                       AND parent_object_id = OBJECT_ID(N'dbo.VMS_AfterHoursApprovalAudit')
                       AND definition = N'(getdate())')
    )
    BEGIN
        ROLLBACK TRAN;
        THROW 59503, N'ABORT: created table does not match the approved shape. Transaction rolled back; no object remains.', 1;
    END

    COMMIT TRAN;
    PRINT N'COMMITTED | dbo.VMS_AfterHoursApprovalAudit created in ' + @Db
        + N' (13 columns, clustered PK on AuditID, index IX_VMS_AfterHoursApprovalAudit_LogID on (LogID, ApprovedOn), default DF_VMS_AfterHoursApprovalAudit_ApprovedOn = getdate()). No rows inserted.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;
    THROW;
END CATCH
