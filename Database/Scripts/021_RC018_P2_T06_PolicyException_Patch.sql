/* ============================================================================
   NAME:        RC-018 Phase 2 / Table 6 — PolicyException schema + MERGE
   WHEN:        2026-09-25
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
                Database/Scripts/009_PolicyException_IntegrityHardening.sql
   TARGET:      LIVE dbo.PolicyException (AnmolDristi)
   AUTHORITY:   UAT AnmolDristiDev configuration rows (ExceptionId 1, 2, 4, 90002)
   WHAT:        1) ADD CK_PolicyException_Priority and CK_PolicyException_EffectiveDates
                2) ADD IX_PolicyException_Employee_Active_Dates
                3) Backup then MERGE eligible UAT rows (no DELETE)
   GATE:        @EnableFinancialOverrides = 0 skips ExceptionId 1, 2, and 90002.
                ExceptionId 4 always remains eligible.
   PRESERVE:    ExceptionId via IDENTITY_INSERT. Never delete LIVE rows.
   NOTE:        DO NOT EXECUTE until Production Gate. Default @DryRun = 1.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @DryRun BIT = 1;
DECLARE @EnableFinancialOverrides BIT = 0;

DECLARE @RunTag      NVARCHAR(32) = CONVERT(CHAR(8), GETDATE(), 112)
                                 + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_PolicyException_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @Inserted    INT = 0;
DECLARE @Updated     INT = 0;

PRINT N'RC-018 P2-T06 PolicyException | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1))
    + N' | EnableFinancialOverrides=' + CAST(@EnableFinancialOverrides AS NVARCHAR(1));

IF OBJECT_ID(N'dbo.PolicyException', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.PolicyException not found. Abort.', 16, 1);
    RETURN;
END;

DECLARE @LiveCnt INT = (SELECT COUNT(*) FROM dbo.PolicyException);
PRINT N'Preflight LIVE row count=' + CAST(@LiveCnt AS NVARCHAR(20)) + N' (Phase 0 expected 0)';

/* =========================================================================
   1) SCHEMA — checks and index, only if absent
   ========================================================================= */
PRINT N'--- SCHEMA ---';

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PolicyException_Priority'
      AND parent_object_id = OBJECT_ID(N'dbo.PolicyException')
)
BEGIN
    PRINT N'CK_PolicyException_Priority missing — will ADD.';
    IF @DryRun = 0
    BEGIN
        ALTER TABLE dbo.PolicyException WITH CHECK
            ADD CONSTRAINT CK_PolicyException_Priority CHECK (Priority > 0);
        PRINT N'ADD CHECK CK_PolicyException_Priority completed.';
    END
END
ELSE
    PRINT N'CK_PolicyException_Priority already present.';

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PolicyException_EffectiveDates'
      AND parent_object_id = OBJECT_ID(N'dbo.PolicyException')
)
BEGIN
    PRINT N'CK_PolicyException_EffectiveDates missing — will ADD.';
    IF @DryRun = 0
    BEGIN
        ALTER TABLE dbo.PolicyException WITH CHECK
            ADD CONSTRAINT CK_PolicyException_EffectiveDates
            CHECK (EffectiveTo IS NULL OR EffectiveFrom IS NULL OR EffectiveTo >= EffectiveFrom);
        PRINT N'ADD CHECK CK_PolicyException_EffectiveDates completed.';
    END
END
ELSE
    PRINT N'CK_PolicyException_EffectiveDates already present.';

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PolicyException_Employee_Active_Dates'
      AND object_id = OBJECT_ID(N'dbo.PolicyException')
)
BEGIN
    PRINT N'IX_PolicyException_Employee_Active_Dates missing — will CREATE.';
    IF @DryRun = 0
    BEGIN
        CREATE NONCLUSTERED INDEX IX_PolicyException_Employee_Active_Dates
            ON dbo.PolicyException (EmployeeCode ASC, IsActive ASC, EffectiveFrom ASC, EffectiveTo ASC)
            INCLUDE (Priority, ExceptionId);
        PRINT N'CREATE INDEX completed.';
    END
END
ELSE
    PRINT N'IX_PolicyException_Employee_Active_Dates already present.';

/* =========================================================================
   2) UAT CONFIGURATION SOURCE (exported 2026-09-25)
   ========================================================================= */
IF OBJECT_ID(N'tempdb..#UatPolicy') IS NOT NULL DROP TABLE #UatPolicy;

CREATE TABLE #UatPolicy
(
    ExceptionId    INT            NOT NULL PRIMARY KEY,
    EmployeeCode   NVARCHAR(100)  NOT NULL,
    RegionId       INT            NULL,
    BranchId       INT            NULL,
    GradeId        INT            NULL,
    DesignationId  INT            NULL,
    PolicyJson     NVARCHAR(MAX)  NULL,
    Priority       INT            NOT NULL,
    EffectiveFrom  DATE           NULL,
    EffectiveTo    DATE           NULL,
    IsActive       BIT            NOT NULL,
    CreatedBy      NVARCHAR(200)  NULL,
    CreatedOn      DATETIME2(7)   NOT NULL,
    ModifiedBy     NVARCHAR(200)  NULL,
    ModifiedOn     DATETIME2(7)   NULL
);

INSERT INTO #UatPolicy
(
    ExceptionId, EmployeeCode, RegionId, BranchId, GradeId, DesignationId,
    PolicyJson, Priority, EffectiveFrom, EffectiveTo, IsActive,
    CreatedBy, CreatedOn, ModifiedBy, ModifiedOn
)
VALUES
 (1, N'AHS1562', 1096, 1965, 1102, 65,
  N'{"MaxKM":2250.0,"RatePerKm":5.75,"DA":250.0,"HotelCap":1000.0,"OutstationDA":450.0,"OwnArrangementDA":800.0,"Remarks":"Policy Exception"}',
  1, '2026-04-01', '2027-04-30', 1,
  N'AHO445', '2026-07-31T17:18:17.0855190', N'INTEG_TEST', '2026-08-05T00:50:00.1250531'),
 (2, N'AHS628', 1096, 1965, 1102, 65,
  N'{"MaxKM":2500.0,"RatePerKm":5.00,"DA":230.0,"HotelCap":1000.0,"OutstationDA":500.0,"OwnArrangementDA":1000.0,"Remarks":"Exception Policy"}',
  1, '2026-04-01', '2027-03-31', 1,
  N'AHO539', '2026-08-04T15:55:06.2455593', N'INTEG_TEST', '2026-08-05T00:51:17.2518536'),
 (4, N'AHS1562', 1096, 1965, 1102, 65,
  N'{"DA":99,"Remarks":"temp overlap"}',
  1, '2000-01-01', '2000-01-02', 0,
  N'INTEG_TEST', '2026-08-05T00:49:59.0933793', N'INTEG_TEST', '2026-08-05T00:49:59.6141977'),
 (90002, N'AHS1530', 1096, 1965, 1102, 65,
  N'{"MaxKM":3.0,"RatePerKm":6.95,"DA":600.0,"HotelCap":2000.0,"Remarks":"Exception"}',
  1, '2026-08-01', '2026-12-31', 1,
  N'AHO539', '2026-09-22T11:18:10.5385275', N'AHO539', '2026-09-22T11:27:13.2817440');

PRINT N'UAT source loaded: 4 rows (1 active AHS1562, 1 active AHS628, 1 inactive AHS1562, 1 active AHS1530).';
PRINT N'ExceptionId 4 remains eligible regardless of @EnableFinancialOverrides.';

IF @EnableFinancialOverrides = 0
BEGIN
    PRINT N'SKIP ExceptionId 1 (AHS1562): financial override held. Set @EnableFinancialOverrides = 1 after owner approval.';
    PRINT N'SKIP ExceptionId 2 (AHS628): financial override held. Set @EnableFinancialOverrides = 1 after owner approval.';
    PRINT N'SKIP ExceptionId 90002 (AHS1530): financial override held. Set @EnableFinancialOverrides = 1 after explicit KM cap confirmation (3 km).';
    DELETE FROM #UatPolicy WHERE ExceptionId IN (1, 2, 90002);
END
ELSE
    PRINT N'Financial overrides enabled. ExceptionIds 1, 2, and 90002 are eligible for MERGE.';

PRINT N'--- PREVIEW: would INSERT ---';
SELECT u.ExceptionId, u.EmployeeCode, u.IsActive, u.EffectiveFrom, u.EffectiveTo
FROM #UatPolicy u
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PolicyException l WHERE l.ExceptionId = u.ExceptionId
);

PRINT N'--- PREVIEW: would UPDATE ---';
SELECT l.ExceptionId, l.EmployeeCode AS LIVE_Employee, u.EmployeeCode AS UAT_Employee
FROM dbo.PolicyException l
INNER JOIN #UatPolicy u ON u.ExceptionId = l.ExceptionId
WHERE ISNULL(l.EmployeeCode, N'') <> ISNULL(u.EmployeeCode, N'')
   OR ISNULL(l.RegionId, -1) <> ISNULL(u.RegionId, -1)
   OR ISNULL(l.BranchId, -1) <> ISNULL(u.BranchId, -1)
   OR ISNULL(l.GradeId, -1) <> ISNULL(u.GradeId, -1)
   OR ISNULL(l.DesignationId, -1) <> ISNULL(u.DesignationId, -1)
   OR ISNULL(l.PolicyJson, N'') <> ISNULL(u.PolicyJson, N'')
   OR ISNULL(l.Priority, -1) <> ISNULL(u.Priority, -1)
   OR ISNULL(l.EffectiveFrom, '19000101') <> ISNULL(u.EffectiveFrom, '19000101')
   OR ISNULL(l.EffectiveTo, '19000101') <> ISNULL(u.EffectiveTo, '19000101')
   OR ISNULL(l.IsActive, 0) <> ISNULL(u.IsActive, 0);

PRINT N'--- PREVIEW: LIVE-only rows (RETAINED; no DELETE) ---';
SELECT l.ExceptionId, l.EmployeeCode
FROM dbo.PolicyException l
WHERE NOT EXISTS (SELECT 1 FROM #UatPolicy u WHERE u.ExceptionId = l.ExceptionId);

IF @DryRun = 1
BEGIN
    PRINT N'DryRun=1 — schema apply, backup, and MERGE skipped.';
    RETURN;
END;

/* =========================================================================
   3) BACKUP then MERGE
   ========================================================================= */
IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
    RETURN;
END;

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.PolicyException WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup created: ' + @BackupTable;

BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID(N'tempdb..#ActionLog') IS NOT NULL DROP TABLE #ActionLog;
    CREATE TABLE #ActionLog
    (
        MergeAction NVARCHAR(10) NOT NULL,
        ExceptionId INT          NOT NULL
    );

    SET IDENTITY_INSERT dbo.PolicyException ON;

    MERGE dbo.PolicyException AS tgt
    USING #UatPolicy AS src
        ON tgt.ExceptionId = src.ExceptionId
    WHEN MATCHED AND (
            ISNULL(tgt.EmployeeCode, N'') <> ISNULL(src.EmployeeCode, N'')
         OR ISNULL(tgt.RegionId, -1) <> ISNULL(src.RegionId, -1)
         OR ISNULL(tgt.BranchId, -1) <> ISNULL(src.BranchId, -1)
         OR ISNULL(tgt.GradeId, -1) <> ISNULL(src.GradeId, -1)
         OR ISNULL(tgt.DesignationId, -1) <> ISNULL(src.DesignationId, -1)
         OR ISNULL(tgt.PolicyJson, N'') <> ISNULL(src.PolicyJson, N'')
         OR ISNULL(tgt.Priority, -1) <> ISNULL(src.Priority, -1)
         OR ISNULL(tgt.EffectiveFrom, '19000101') <> ISNULL(src.EffectiveFrom, '19000101')
         OR ISNULL(tgt.EffectiveTo, '19000101') <> ISNULL(src.EffectiveTo, '19000101')
         OR ISNULL(tgt.IsActive, 0) <> ISNULL(src.IsActive, 0)
         OR ISNULL(tgt.CreatedBy, N'') <> ISNULL(src.CreatedBy, N'')
         OR ISNULL(tgt.ModifiedBy, N'') <> ISNULL(src.ModifiedBy, N'')
        )
        THEN UPDATE SET
              tgt.EmployeeCode  = src.EmployeeCode
            , tgt.RegionId      = src.RegionId
            , tgt.BranchId      = src.BranchId
            , tgt.GradeId       = src.GradeId
            , tgt.DesignationId = src.DesignationId
            , tgt.PolicyJson    = src.PolicyJson
            , tgt.Priority      = src.Priority
            , tgt.EffectiveFrom = src.EffectiveFrom
            , tgt.EffectiveTo   = src.EffectiveTo
            , tgt.IsActive      = src.IsActive
            , tgt.CreatedBy     = src.CreatedBy
            , tgt.CreatedOn     = src.CreatedOn
            , tgt.ModifiedBy    = src.ModifiedBy
            , tgt.ModifiedOn    = src.ModifiedOn
    WHEN NOT MATCHED BY TARGET
        THEN INSERT
        (
            ExceptionId, EmployeeCode, RegionId, BranchId, GradeId, DesignationId,
            PolicyJson, Priority, EffectiveFrom, EffectiveTo, IsActive,
            CreatedBy, CreatedOn, ModifiedBy, ModifiedOn
        )
        VALUES
        (
            src.ExceptionId, src.EmployeeCode, src.RegionId, src.BranchId, src.GradeId, src.DesignationId,
            src.PolicyJson, src.Priority, src.EffectiveFrom, src.EffectiveTo, src.IsActive,
            src.CreatedBy, src.CreatedOn, src.ModifiedBy, src.ModifiedOn
        )
    OUTPUT $action, inserted.ExceptionId INTO #ActionLog (MergeAction, ExceptionId);

    SET IDENTITY_INSERT dbo.PolicyException OFF;

    SELECT @Inserted = COUNT(*) FROM #ActionLog WHERE MergeAction = N'INSERT';
    SELECT @Updated  = COUNT(*) FROM #ActionLog WHERE MergeAction = N'UPDATE';

    COMMIT TRAN;

    PRINT N'MERGE committed. Inserted=' + CAST(@Inserted AS NVARCHAR(10))
        + N' Updated=' + CAST(@Updated AS NVARCHAR(10));
    PRINT N'Backup for rollback: ' + @BackupTable;
    PRINT N'Next: 021b_RC018_P2_T06_PolicyException_Validate.sql';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    BEGIN TRY SET IDENTITY_INSERT dbo.PolicyException OFF; END TRY BEGIN CATCH END CATCH;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P2-T06 FAILED: %s', 16, 1, @Err);
END CATCH;
GO
