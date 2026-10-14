/*
    RC-014B catalog check for the reduced whole-claim return.
    Authoring status: NOT EXECUTED. This file is not catalog evidence
    until a human runs it on the authorized test database and reviews
    the output.

    Target, when a human later reviews and runs it:
      server   MUMANMOAPP01\SQLEXPRESS
      database AnmolDristiDev

    The server and database gate only refuses a different instance.
    Passing that gate does not mean the current catalog still matches
    the 2 August 2026 AnmolDristiDev extraction.

    Historical reference, not an expected result:
      Database/Triggers/README.md and Database/Inventory.md section 8
      record no triggers on the sixteen Monthly Sales Claims tables.
      Database/ExtractionReport.md risk R-05 says that snapshot can drift.
      Database/Tables/SalesApprovalRequestApprover.sql records a primary
      key on ApprovalRequestApproverId and filtered unique index
      UX_SAR_Auditor_Once where ApproverRole = 'Auditor'.
      That extracted index does not cover pending L1 rows.

    Scope:
      Triggers on the six tables written by dbo.usp_ReturnClaimToEmployee,
      and unique indexes and key constraints on dbo.SalesApprovalRequestApprover.
      This script does not call that procedure and does not read its text.

    Does not BEGIN, COMMIT, or ROLLBACK a transaction.
    Does not INSERT, UPDATE, or DELETE application data.
    Does not CREATE, ALTER, or DROP a permanent object.
    The table variable below is session memory for verdict text only.

    How to read a future run:
      NOT RUN
        The catalog was not checked.
      CATALOG VISIBILITY LIMITATION
        The login could not see the table, or could not see enough
        metadata. No-trigger and no-unique-index are not established.
      NO TRIGGER ROWS FOUND
        sys.triggers returned no row for that dbo parent. This is the
        source-review assumption for that table. It is still limited to
        metadata this login can see.
      TRIGGER FOUND
        A trigger exists. Read trigger_definition before any execution
        of the return procedure. A trigger can change request status,
        activity ownership, detail status, approver state, expense
        state, or history, and can make @@ROWCOUNT and SCOPE_IDENTITY()
        unreliable.
      TRIGGER DEFINITION UNAVAILABLE | permissions
        A trigger row is visible and its module text is null. The
        trigger check did not pass.
      TRIGGER DEFINITION UNAVAILABLE | encrypted
        The trigger body cannot be read. The trigger check did not pass.
      UNIQUE FINDING
        Read filter_definition and the key columns. An included column
        is not part of the unique key. A disabled unique index does not
        enforce uniqueness. Pending-L1 protection is established only
        when an enabled unique key contains ApprovalRequestId and
        ApproverLoginId and its predicate, if any, actually excludes a
        second pending L1 row. This script does not parse filter text
        into that conclusion.
      CATALOG ONLY
        A clean trigger and index reading still does not prove return
        or resubmission behavior and does not authorize execution of
        dbo.usp_ReturnClaimToEmployee.
*/
SET NOCOUNT ON;

IF (@@OPTIONS & 2) = 2
BEGIN
    PRINT N'NOT RUN | IMPLICIT_TRANSACTIONS is ON. This script does not open a transaction.';
    RETURN;
END;

IF @@TRANCOUNT > 0
BEGIN
    PRINT N'NOT RUN | @@TRANCOUNT is already greater than 0. This script does not run inside an open transaction.';
    RETURN;
END;

DECLARE @Server nvarchar(256) = CONVERT(nvarchar(256), SERVERPROPERTY(N'ServerName'));

IF UPPER(ISNULL(@Server, N'')) <> N'MUMANMOAPP01\SQLEXPRESS'
BEGIN
    PRINT N'NOT RUN | Refusing server ' + ISNULL(@Server, N'<null>') + N'. The authorized test instance is MUMANMOAPP01\SQLEXPRESS.';
    RETURN;
END;

IF DB_ID(N'AnmolDristiDev') IS NULL
BEGIN
    PRINT N'NOT RUN | Database AnmolDristiDev is not visible on this server.';
    RETURN;
END;

USE [AnmolDristiDev];

IF DB_NAME() <> N'AnmolDristiDev'
BEGIN
    PRINT N'NOT RUN | Refusing database ' + ISNULL(DB_NAME(), N'<null>') + N'. The authorized test database is AnmolDristiDev.';
    RETURN;
END;

PRINT N'CONTEXT | Server=' + ISNULL(CONVERT(nvarchar(256), SERVERPROPERTY(N'ServerName')), N'<null>');
PRINT N'CONTEXT | Database=' + ISNULL(DB_NAME(), N'<null>');
PRINT N'CONTEXT | ProductVersion=' + ISNULL(CONVERT(nvarchar(128), SERVERPROPERTY(N'ProductVersion')), N'<null>');
PRINT N'CONTEXT | ProductLevel=' + ISNULL(CONVERT(nvarchar(128), SERVERPROPERTY(N'ProductLevel')), N'<null>');
PRINT N'CONTEXT | Edition=' + ISNULL(CONVERT(nvarchar(128), SERVERPROPERTY(N'Edition')), N'<null>');
PRINT N'CONTEXT | The 2 August 2026 extraction is historical reference only. Compare the result sets to it; do not treat a mismatch as already interpreted.';

DECLARE @TriggerVerdict TABLE
(
      sort_order int NOT NULL PRIMARY KEY
    , table_name sysname NOT NULL
    , verdict nvarchar(2000) NOT NULL
);

INSERT INTO @TriggerVerdict
(
      sort_order
    , table_name
    , verdict
)
SELECT
      expected.sort_order
    , expected.table_name
    , CASE
          WHEN OBJECT_ID(N'dbo.' + expected.table_name, N'U') IS NULL
              THEN N'CATALOG VISIBILITY LIMITATION | dbo.' + expected.table_name
                 + N' | table is not visible. No-trigger is not established.'
          WHEN ISNULL(HAS_PERMS_BY_NAME(N'dbo.' + expected.table_name, N'OBJECT', N'VIEW DEFINITION'), 0) <> 1
               AND NOT EXISTS
               (
                   SELECT 1
                   FROM sys.triggers AS tr
                   INNER JOIN sys.tables AS parent_table
                       ON tr.parent_id = parent_table.object_id
                   INNER JOIN sys.schemas AS parent_schema
                       ON parent_schema.schema_id = parent_table.schema_id
                   WHERE parent_schema.name = N'dbo'
                     AND parent_table.name = expected.table_name
                     AND tr.parent_class = 1
               )
              THEN N'CATALOG VISIBILITY LIMITATION | dbo.' + expected.table_name
                 + N' | VIEW DEFINITION is not granted and sys.triggers returned no row. A hidden trigger is not ruled out.'
          WHEN EXISTS
               (
                   SELECT 1
                   FROM sys.triggers AS tr
                   INNER JOIN sys.tables AS parent_table
                       ON tr.parent_id = parent_table.object_id
                   INNER JOIN sys.schemas AS parent_schema
                       ON parent_schema.schema_id = parent_table.schema_id
                   WHERE parent_schema.name = N'dbo'
                     AND parent_table.name = expected.table_name
                     AND tr.parent_class = 1
                     AND tr.is_encrypted = 0
                     AND OBJECT_DEFINITION(tr.object_id) IS NULL
               )
              THEN N'TRIGGER DEFINITION UNAVAILABLE | permissions | dbo.' + expected.table_name
                 + N' | at least one trigger row has no module text and is not encrypted.'
          WHEN EXISTS
               (
                   SELECT 1
                   FROM sys.triggers AS tr
                   INNER JOIN sys.tables AS parent_table
                       ON tr.parent_id = parent_table.object_id
                   INNER JOIN sys.schemas AS parent_schema
                       ON parent_schema.schema_id = parent_table.schema_id
                   WHERE parent_schema.name = N'dbo'
                     AND parent_table.name = expected.table_name
                     AND tr.parent_class = 1
                     AND tr.is_encrypted = 1
               )
              THEN N'TRIGGER DEFINITION UNAVAILABLE | encrypted | dbo.' + expected.table_name
                 + N' | at least one trigger is encrypted. Its definition is not available.'
          WHEN NOT EXISTS
               (
                   SELECT 1
                   FROM sys.triggers AS tr
                   INNER JOIN sys.tables AS parent_table
                       ON tr.parent_id = parent_table.object_id
                   INNER JOIN sys.schemas AS parent_schema
                       ON parent_schema.schema_id = parent_table.schema_id
                   WHERE parent_schema.name = N'dbo'
                     AND parent_table.name = expected.table_name
                     AND tr.parent_class = 1
               )
              THEN N'NO TRIGGER ROWS FOUND | dbo.' + expected.table_name
                 + N' | sys.triggers returned no row for this parent. The caller has VIEW DEFINITION on the table. Metadata visibility is still limited to what this login can see.'
          ELSE N'TRIGGER FOUND | dbo.' + expected.table_name
             + N' | at least one trigger row was returned and module text was returned where the trigger is not encrypted. Read trigger_definition before any execution of usp_ReturnClaimToEmployee.'
      END
FROM
(
    VALUES
        (1, N'SalesApprovalRequestDetail'),
        (2, N'SalesDailyActivity'),
        (3, N'SalesDailyExpense'),
        (4, N'SalesApprovalRequestApprover'),
        (5, N'SalesApprovalRequest'),
        (6, N'ApprovalRequestStatusHistory')
) AS expected (sort_order, table_name);

SELECT
      parent_schema.name AS parent_schema
    , parent_table.name AS parent_table
    , tr.name AS trigger_name
    , tr.is_disabled
    , CASE
          WHEN tr.is_disabled = 1 THEN N'Disabled'
          ELSE N'Enabled'
      END AS enabled_state
    , tr.is_instead_of_trigger
    , tr.is_encrypted
    , events.event_list
    , CASE
          WHEN tr.is_encrypted = 1 THEN N'TRIGGER DEFINITION UNAVAILABLE | encrypted'
          WHEN OBJECT_DEFINITION(tr.object_id) IS NULL THEN N'TRIGGER DEFINITION UNAVAILABLE | permissions'
          ELSE N'DEFINITION RETURNED'
      END AS definition_status
    , OBJECT_DEFINITION(tr.object_id) AS trigger_definition
FROM sys.triggers AS tr
INNER JOIN sys.tables AS parent_table
    ON tr.parent_id = parent_table.object_id
INNER JOIN sys.schemas AS parent_schema
    ON parent_schema.schema_id = parent_table.schema_id
CROSS APPLY
(
    SELECT STUFF(
    (
        SELECT N',' + te.type_desc
        FROM sys.trigger_events AS te
        WHERE te.object_id = tr.object_id
        ORDER BY te.type_desc
        FOR XML PATH(''), TYPE
    ).value(N'.', N'nvarchar(200)'), 1, 1, N'')
) AS events (event_list)
WHERE parent_schema.name = N'dbo'
  AND parent_table.name IN
  (
        N'SalesApprovalRequestDetail'
      , N'SalesDailyActivity'
      , N'SalesDailyExpense'
      , N'SalesApprovalRequestApprover'
      , N'SalesApprovalRequest'
      , N'ApprovalRequestStatusHistory'
  )
  AND tr.parent_class = 1
ORDER BY parent_table.name, tr.name;

SELECT
      sort_order
    , table_name
    , verdict
FROM @TriggerVerdict
ORDER BY sort_order;

DECLARE @VerdictLine nvarchar(2000);

SELECT @VerdictLine = verdict FROM @TriggerVerdict WHERE sort_order = 1;
PRINT @VerdictLine;
SELECT @VerdictLine = verdict FROM @TriggerVerdict WHERE sort_order = 2;
PRINT @VerdictLine;
SELECT @VerdictLine = verdict FROM @TriggerVerdict WHERE sort_order = 3;
PRINT @VerdictLine;
SELECT @VerdictLine = verdict FROM @TriggerVerdict WHERE sort_order = 4;
PRINT @VerdictLine;
SELECT @VerdictLine = verdict FROM @TriggerVerdict WHERE sort_order = 5;
PRINT @VerdictLine;
SELECT @VerdictLine = verdict FROM @TriggerVerdict WHERE sort_order = 6;
PRINT @VerdictLine;

SELECT
      other_schema.name AS parent_schema
    , other_table.name AS parent_table
    , N'CATALOG VISIBILITY LIMITATION | this table is not dbo. usp_ReturnClaimToEmployee writes dbo. Triggers on this table are outside the six-table check.' AS note
FROM sys.tables AS other_table
INNER JOIN sys.schemas AS other_schema
    ON other_schema.schema_id = other_table.schema_id
WHERE other_schema.name <> N'dbo'
  AND other_table.name IN
  (
        N'SalesApprovalRequestDetail'
      , N'SalesDailyActivity'
      , N'SalesDailyExpense'
      , N'SalesApprovalRequestApprover'
      , N'SalesApprovalRequest'
      , N'ApprovalRequestStatusHistory'
  )
ORDER BY other_schema.name, other_table.name;

IF OBJECT_ID(N'dbo.SalesApprovalRequestApprover', N'U') IS NULL
BEGIN
    PRINT N'CATALOG VISIBILITY LIMITATION | dbo.SalesApprovalRequestApprover | table is not visible. Unique indexes and key constraints are not established.';
    PRINT N'CATALOG ONLY | triggers and approver uniqueness were read only as far as this login can see. This script does not prove return or resubmission behavior and does not authorize execution of usp_ReturnClaimToEmployee.';
    RETURN;
END;

DECLARE @CanViewApproverDefinition bit =
    CASE
        WHEN HAS_PERMS_BY_NAME(N'dbo.SalesApprovalRequestApprover', N'OBJECT', N'VIEW DEFINITION') = 1 THEN 1
        ELSE 0
    END;

IF @CanViewApproverDefinition <> 1
    PRINT N'CATALOG VISIBILITY LIMITATION | dbo.SalesApprovalRequestApprover | VIEW DEFINITION is not granted. A null filter_definition is not proof that an index is unfiltered, and unique-index absence is not fully established.';

DECLARE
      @ApproverIndexCount int
    , @ApproverUniqueIndexCount int
    , @ApproverKeyConstraintCount int;

SELECT @ApproverIndexCount = COUNT(*)
FROM sys.indexes AS i
INNER JOIN sys.tables AS t
    ON t.object_id = i.object_id
INNER JOIN sys.schemas AS s
    ON s.schema_id = t.schema_id
WHERE s.name = N'dbo'
  AND t.name = N'SalesApprovalRequestApprover';

PRINT N'INFO | dbo.SalesApprovalRequestApprover index count='
    + CONVERT(nvarchar(10), @ApproverIndexCount);

SELECT @ApproverUniqueIndexCount = COUNT(*)
FROM sys.indexes AS i
INNER JOIN sys.tables AS t
    ON t.object_id = i.object_id
INNER JOIN sys.schemas AS s
    ON s.schema_id = t.schema_id
WHERE s.name = N'dbo'
  AND t.name = N'SalesApprovalRequestApprover'
  AND i.is_unique = 1;

PRINT N'INFO | dbo.SalesApprovalRequestApprover unique index count='
    + CONVERT(nvarchar(10), @ApproverUniqueIndexCount);

SELECT @ApproverKeyConstraintCount = COUNT(*)
FROM sys.key_constraints AS kc
INNER JOIN sys.tables AS t
    ON t.object_id = kc.parent_object_id
INNER JOIN sys.schemas AS s
    ON s.schema_id = t.schema_id
WHERE s.name = N'dbo'
  AND t.name = N'SalesApprovalRequestApprover';

PRINT N'INFO | dbo.SalesApprovalRequestApprover key constraint count='
    + CONVERT(nvarchar(10), @ApproverKeyConstraintCount);

SELECT
      s.name AS table_schema
    , t.name AS table_name
    , i.name AS index_name
    , i.index_id
    , i.type_desc
    , i.is_unique
    , i.is_primary_key
    , i.is_unique_constraint
    , i.is_disabled
    , i.filter_definition
    , key_columns.column_list AS key_column_list
    , included_columns.column_list AS included_column_list
FROM sys.indexes AS i
INNER JOIN sys.tables AS t
    ON t.object_id = i.object_id
INNER JOIN sys.schemas AS s
    ON s.schema_id = t.schema_id
CROSS APPLY
(
    SELECT STUFF(
    (
        SELECT N', ' + c.name + CASE WHEN ic.is_descending_key = 1 THEN N' DESC' ELSE N'' END
        FROM sys.index_columns AS ic
        INNER JOIN sys.columns AS c
            ON c.object_id = ic.object_id
           AND c.column_id = ic.column_id
        WHERE ic.object_id = i.object_id
          AND ic.index_id = i.index_id
          AND ic.is_included_column = 0
        ORDER BY ic.key_ordinal, c.column_id
        FOR XML PATH(''), TYPE
    ).value(N'.', N'nvarchar(max)'), 1, 2, N'')
) AS key_columns (column_list)
CROSS APPLY
(
    SELECT STUFF(
    (
        SELECT N', ' + c.name
        FROM sys.index_columns AS ic
        INNER JOIN sys.columns AS c
            ON c.object_id = ic.object_id
           AND c.column_id = ic.column_id
        WHERE ic.object_id = i.object_id
          AND ic.index_id = i.index_id
          AND ic.is_included_column = 1
        ORDER BY c.column_id
        FOR XML PATH(''), TYPE
    ).value(N'.', N'nvarchar(max)'), 1, 2, N'')
) AS included_columns (column_list)
WHERE s.name = N'dbo'
  AND t.name = N'SalesApprovalRequestApprover'
  AND i.is_unique = 1
ORDER BY i.index_id;

SELECT
      i.name AS index_name
    , i.index_id
    , ic.key_ordinal
    , ic.is_included_column
    , ic.is_descending_key
    , c.name AS column_name
    , c.column_id
FROM sys.indexes AS i
INNER JOIN sys.tables AS t
    ON t.object_id = i.object_id
INNER JOIN sys.schemas AS s
    ON s.schema_id = t.schema_id
INNER JOIN sys.index_columns AS ic
    ON ic.object_id = i.object_id
   AND ic.index_id = i.index_id
INNER JOIN sys.columns AS c
    ON c.object_id = ic.object_id
   AND c.column_id = ic.column_id
WHERE s.name = N'dbo'
  AND t.name = N'SalesApprovalRequestApprover'
  AND i.is_unique = 1
ORDER BY i.index_id, ic.is_included_column, ic.key_ordinal, c.column_id;

SELECT
      kc.name AS constraint_name
    , kc.type AS constraint_type
    , kc.type_desc
    , kc.is_system_named
    , kc.unique_index_id
    , i.name AS index_name
    , i.is_unique
    , i.is_disabled
    , i.filter_definition
FROM sys.key_constraints AS kc
INNER JOIN sys.tables AS t
    ON t.object_id = kc.parent_object_id
INNER JOIN sys.schemas AS s
    ON s.schema_id = t.schema_id
LEFT JOIN sys.indexes AS i
    ON i.object_id = kc.parent_object_id
   AND i.index_id = kc.unique_index_id
WHERE s.name = N'dbo'
  AND t.name = N'SalesApprovalRequestApprover'
ORDER BY kc.type_desc, kc.name;

SELECT
      i.name AS index_name
    , i.is_disabled
    , i.is_primary_key
    , i.is_unique_constraint
    , i.filter_definition
    , CASE
          WHEN @CanViewApproverDefinition <> 1
              THEN N'CATALOG VISIBILITY LIMITATION | filter_definition is not reliable without VIEW DEFINITION. Pending-L1 uniqueness is not established.'
          WHEN i.is_disabled = 1
              THEN N'UNIQUE FINDING | disabled unique index does not enforce uniqueness.'
          WHEN EXISTS
               (
                   SELECT 1
                   FROM sys.index_columns AS ic
                   INNER JOIN sys.columns AS c
                       ON c.object_id = ic.object_id
                      AND c.column_id = ic.column_id
                   WHERE ic.object_id = i.object_id
                     AND ic.index_id = i.index_id
                     AND ic.is_included_column = 0
                     AND c.name = N'ApprovalRequestId'
               )
           AND EXISTS
               (
                   SELECT 1
                   FROM sys.index_columns AS ic
                   INNER JOIN sys.columns AS c
                       ON c.object_id = ic.object_id
                      AND c.column_id = ic.column_id
                   WHERE ic.object_id = i.object_id
                     AND ic.index_id = i.index_id
                     AND ic.is_included_column = 0
                     AND c.name = N'ApproverLoginId'
               )
           AND i.filter_definition IS NULL
              THEN N'UNIQUE FINDING | unfiltered unique key includes ApprovalRequestId and ApproverLoginId. The same request and login cannot both exist.'
          WHEN EXISTS
               (
                   SELECT 1
                   FROM sys.index_columns AS ic
                   INNER JOIN sys.columns AS c
                       ON c.object_id = ic.object_id
                      AND c.column_id = ic.column_id
                   WHERE ic.object_id = i.object_id
                     AND ic.index_id = i.index_id
                     AND ic.is_included_column = 0
                     AND c.name = N'ApprovalRequestId'
               )
           AND EXISTS
               (
                   SELECT 1
                   FROM sys.index_columns AS ic
                   INNER JOIN sys.columns AS c
                       ON c.object_id = ic.object_id
                      AND c.column_id = ic.column_id
                   WHERE ic.object_id = i.object_id
                     AND ic.index_id = i.index_id
                     AND ic.is_included_column = 0
                     AND c.name = N'ApproverLoginId'
               )
           AND i.filter_definition IS NOT NULL
              THEN N'UNIQUE FINDING | filtered unique key includes ApprovalRequestId and ApproverLoginId. Read filter_definition before treating it as pending-L1 protection.'
          ELSE N'UNIQUE FINDING | does not establish pending-L1 uniqueness for the same request and login.'
      END AS pending_l1_reading
FROM sys.indexes AS i
INNER JOIN sys.tables AS t
    ON t.object_id = i.object_id
INNER JOIN sys.schemas AS s
    ON s.schema_id = t.schema_id
WHERE s.name = N'dbo'
  AND t.name = N'SalesApprovalRequestApprover'
  AND i.is_unique = 1
ORDER BY i.index_id;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS i
    INNER JOIN sys.tables AS t
        ON t.object_id = i.object_id
    INNER JOIN sys.schemas AS s
        ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'SalesApprovalRequestApprover'
      AND i.is_unique = 1
)
BEGIN
    IF @CanViewApproverDefinition = 1
        PRINT N'UNIQUE FINDING | no unique index rows returned for dbo.SalesApprovalRequestApprover.';
    ELSE
        PRINT N'CATALOG VISIBILITY LIMITATION | dbo.SalesApprovalRequestApprover | no unique index rows were returned, and VIEW DEFINITION is not granted. Absence is not established.';
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.key_constraints AS kc
    INNER JOIN sys.tables AS t
        ON t.object_id = kc.parent_object_id
    INNER JOIN sys.schemas AS s
        ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'SalesApprovalRequestApprover'
)
BEGIN
    IF @CanViewApproverDefinition = 1
        PRINT N'UNIQUE FINDING | no key constraint rows returned for dbo.SalesApprovalRequestApprover.';
    ELSE
        PRINT N'CATALOG VISIBILITY LIMITATION | dbo.SalesApprovalRequestApprover | no key constraint rows were returned, and VIEW DEFINITION is not granted. Absence is not established.';
END;

IF @CanViewApproverDefinition = 1
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.indexes AS i
       INNER JOIN sys.tables AS t
           ON t.object_id = i.object_id
       INNER JOIN sys.schemas AS s
           ON s.schema_id = t.schema_id
       WHERE s.name = N'dbo'
         AND t.name = N'SalesApprovalRequestApprover'
         AND i.is_unique = 1
         AND i.is_disabled = 0
         AND EXISTS
         (
             SELECT 1
             FROM sys.index_columns AS ic
             INNER JOIN sys.columns AS c
                 ON c.object_id = ic.object_id
                AND c.column_id = ic.column_id
             WHERE ic.object_id = i.object_id
               AND ic.index_id = i.index_id
               AND ic.is_included_column = 0
               AND c.name = N'ApprovalRequestId'
         )
         AND EXISTS
         (
             SELECT 1
             FROM sys.index_columns AS ic
             INNER JOIN sys.columns AS c
                 ON c.object_id = ic.object_id
                AND c.column_id = ic.column_id
             WHERE ic.object_id = i.object_id
               AND ic.index_id = i.index_id
               AND ic.is_included_column = 0
               AND c.name = N'ApproverLoginId'
         )
   )
    PRINT N'UNIQUE FINDING | no enabled unique index has both ApprovalRequestId and ApproverLoginId as key columns. Duplicate pending L1 rows are not prevented by a key of that shape. Read every filter_definition before relying on this line.';

IF @CanViewApproverDefinition = 1
   AND EXISTS
   (
       SELECT 1
       FROM sys.indexes AS i
       INNER JOIN sys.tables AS t
           ON t.object_id = i.object_id
       INNER JOIN sys.schemas AS s
           ON s.schema_id = t.schema_id
       WHERE s.name = N'dbo'
         AND t.name = N'SalesApprovalRequestApprover'
         AND i.is_unique = 1
         AND i.is_disabled = 0
         AND i.filter_definition IS NULL
         AND EXISTS
         (
             SELECT 1
             FROM sys.index_columns AS ic
             INNER JOIN sys.columns AS c
                 ON c.object_id = ic.object_id
                AND c.column_id = ic.column_id
             WHERE ic.object_id = i.object_id
               AND ic.index_id = i.index_id
               AND ic.is_included_column = 0
               AND c.name = N'ApprovalRequestId'
         )
         AND EXISTS
         (
             SELECT 1
             FROM sys.index_columns AS ic
             INNER JOIN sys.columns AS c
                 ON c.object_id = ic.object_id
                AND c.column_id = ic.column_id
             WHERE ic.object_id = i.object_id
               AND ic.index_id = i.index_id
               AND ic.is_included_column = 0
               AND c.name = N'ApproverLoginId'
         )
   )
    PRINT N'UNIQUE FINDING | an enabled unfiltered unique index includes ApprovalRequestId and ApproverLoginId. The same request and login cannot both exist. This is stricter than the 2 August 2026 extraction.';

IF @CanViewApproverDefinition = 1
   AND EXISTS
   (
       SELECT 1
       FROM sys.indexes AS i
       INNER JOIN sys.tables AS t
           ON t.object_id = i.object_id
       INNER JOIN sys.schemas AS s
           ON s.schema_id = t.schema_id
       WHERE s.name = N'dbo'
         AND t.name = N'SalesApprovalRequestApprover'
         AND i.is_unique = 1
         AND i.is_disabled = 0
         AND i.filter_definition IS NOT NULL
         AND EXISTS
         (
             SELECT 1
             FROM sys.index_columns AS ic
             INNER JOIN sys.columns AS c
                 ON c.object_id = ic.object_id
                AND c.column_id = ic.column_id
             WHERE ic.object_id = i.object_id
               AND ic.index_id = i.index_id
               AND ic.is_included_column = 0
               AND c.name = N'ApprovalRequestId'
         )
         AND EXISTS
         (
             SELECT 1
             FROM sys.index_columns AS ic
             INNER JOIN sys.columns AS c
                 ON c.object_id = ic.object_id
                AND c.column_id = ic.column_id
             WHERE ic.object_id = i.object_id
               AND ic.index_id = i.index_id
               AND ic.is_included_column = 0
               AND c.name = N'ApproverLoginId'
         )
   )
    PRINT N'UNIQUE FINDING | an enabled filtered unique index includes ApprovalRequestId and ApproverLoginId. Pending-L1 protection depends on filter_definition and is not established by this script.';

PRINT N'CATALOG ONLY | triggers and approver uniqueness were read from the catalog. This script does not prove return or resubmission behavior and does not authorize execution of usp_ReturnClaimToEmployee.';
