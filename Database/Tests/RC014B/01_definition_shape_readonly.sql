/*
    RC-014B definition shape check. Read-only.
    Authoring status: NOT EXECUTED. This file is not UAT evidence.
    G4-G7 are not authorized.

    Target, when a human later reviews and runs it:
      server  MUMANMOAPP01\SQLEXPRESS
      database AnmolDristiDev
      object   dbo.usp_ProcessApprovalAction

    Does not call the procedure. Does not BEGIN, COMMIT, or ROLLBACK a transaction.
    Does not write PASS for items this script cannot see.

    Accepted for this validation-only pilot, and checked so a later edit is visible:
      - non-Auditor final branch may write Completed (procedure lines 468-498)
      - stale header banner that still says the Auditor terminal branch writes Completed
        (procedure lines 24-29) stays in the definition

    UNVERIFIED here:
      - no C# override
      - Employee cannot skip L1
      - universal L1 cannot skip Auditor
*/
SET NOCOUNT ON;

IF (@@OPTIONS & 2) = 2
BEGIN
    PRINT N'RC014B definition | RESULT | NOT RUN | IMPLICIT_TRANSACTIONS is ON.';
    PRINT N'UNVERIFIED | no C# override | not observable from sys.sql_modules';
    PRINT N'UNVERIFIED | Employee cannot skip L1 | this procedure does not submit a claim';
    PRINT N'UNVERIFIED | universal L1 cannot skip Auditor | true only while a higher Pending required approver exists';
    RETURN;
END;

USE [AnmolDristiDev];

DECLARE @Server nvarchar(256) = CONVERT(nvarchar(256), SERVERPROPERTY(N'ServerName'));
DECLARE @Fail int = 0;
DECLARE @Def nvarchar(max);
DECLARE @Norm nvarchar(max);
DECLARE @Upper nvarchar(max);
DECLARE @ExpectedHash varchar(64) = 'F6471A308779E26D74A2B84B00A6DAF71DAA84FDFD5A7C71189F0DA5B61E13EE';
DECLARE @RawHash varchar(64);
DECLARE @CreateAt int;
DECLARE @Scan int;
DECLARE @Gap int;
DECLARE @MaxAt int;
DECLARE @CloseAt int;
DECLARE @Sig nvarchar(max);
DECLARE @AtCount int;
DECLARE @Aud int;
DECLARE @AudSet int;
DECLARE @Partial int;
DECLARE @NonAud int;
DECLARE @CompletedSet int;
DECLARE @BeginCount int;
DECLARE @CommitCount int;
DECLARE @RollbackCount int;

PRINT N'===== RC-014B definition shape (read-only, not a certified result) =====';
PRINT N'Server=' + ISNULL(@Server, N'<null>') + N' Database=' + DB_NAME();

IF UPPER(ISNULL(@Server, N'')) <> N'MUMANMOAPP01\SQLEXPRESS'
   OR DB_NAME() <> N'AnmolDristiDev'
BEGIN
    PRINT N'RC014B definition | RESULT | NOT RUN | Refusing a server or database other than MUMANMOAPP01\SQLEXPRESS / AnmolDristiDev.';
    PRINT N'UNVERIFIED | no C# override | not observable from sys.sql_modules';
    PRINT N'UNVERIFIED | Employee cannot skip L1 | this procedure does not submit a claim';
    PRINT N'UNVERIFIED | universal L1 cannot skip Auditor | true only while a higher Pending required approver exists';
    RETURN;
END;

SET @Def = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_ProcessApprovalAction'));

IF @Def IS NULL
BEGIN
    PRINT N'RC014B definition | RESULT | NOT RUN | dbo.usp_ProcessApprovalAction is not visible.';
    PRINT N'UNVERIFIED | no C# override | not observable from sys.sql_modules';
    PRINT N'UNVERIFIED | Employee cannot skip L1 | this procedure does not submit a claim';
    PRINT N'UNVERIFIED | universal L1 cannot skip Auditor | true only while a higher Pending required approver exists';
    RETURN;
END;

SET @Norm = REPLACE(@Def, CHAR(13), N'');
SET @Upper = UPPER(@Norm);
SET @RawHash = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Def)), 2);

PRINT N'INFO | SHA2_256 OBJECT_DEFINITION=' + @RawHash;
PRINT N'INFO | human-supplied pre-change baseline hash=' + @ExpectedHash;
IF @RawHash = @ExpectedHash
    PRINT N'INFO | baseline hash comparison | MATCH';
ELSE
    PRINT N'INFO | baseline hash comparison | REVIEW | not a behavior failure in this script; the hash method was not re-derived here.';

IF CHARINDEX(N'Auditor terminal path leaves the header Pending', @Norm) > 0
    PRINT N'CHECK hf231 Pending banner present | PASS';
ELSE
BEGIN
    PRINT N'CHECK hf231 Pending banner present | FAIL';
    SET @Fail += 1;
END;

-- OBJECT_DEFINITION may keep more than one whitespace character between CREATE and PROCEDURE.
SET @CreateAt = 0;
SET @Scan = 1;
WHILE @Scan > 0
BEGIN
    SET @Scan = CHARINDEX(N'CREATE', @Norm, @Scan);
    IF @Scan = 0
        BREAK;

    SET @Gap = @Scan + LEN(N'CREATE');
    IF @Gap <= LEN(@Norm)
       AND CHARINDEX(SUBSTRING(@Norm, @Gap, 1), N' ' + NCHAR(9) + NCHAR(10) + NCHAR(11) + NCHAR(12)) > 0
       AND (
              @Scan = 1
           OR SUBSTRING(@Norm, @Scan - 1, 1) NOT LIKE N'[0-9A-Za-z_@#]'
          )
    BEGIN
        WHILE @Gap <= LEN(@Norm)
          AND CHARINDEX(SUBSTRING(@Norm, @Gap, 1), N' ' + NCHAR(9) + NCHAR(10) + NCHAR(11) + NCHAR(12)) > 0
            SET @Gap += 1;

        IF SUBSTRING(@Norm, @Gap, LEN(N'PROCEDURE')) = N'PROCEDURE'
           AND (
                  @Gap + LEN(N'PROCEDURE') > LEN(@Norm)
               OR SUBSTRING(@Norm, @Gap + LEN(N'PROCEDURE'), 1) NOT LIKE N'[0-9A-Za-z_@#]'
              )
        BEGIN
            SET @CreateAt = @Scan;
            BREAK;
        END;
    END;

    SET @Scan += LEN(N'CREATE');
END;

IF CHARINDEX(N'usp_ProcessApprovalAction Auditor terminal branch writes Completed and one history row', @Norm) > 0
   AND @CreateAt > 0
   AND CHARINDEX(N'usp_ProcessApprovalAction Auditor terminal branch writes Completed and one history row', @Norm)
       < @CreateAt
    PRINT N'CHECK stale banner retained (accepted comment, not runtime proof) | PASS';
ELSE
BEGIN
    PRINT N'CHECK stale banner retained (accepted comment, not runtime proof) | FAIL';
    SET @Fail += 1;
END;

SET @NonAud = CHARINDEX(N'Non-auditor final level with all days resolved -> Completed.', @Norm);
SET @CompletedSet = CHARINDEX(N'SET @NewRequestStatus = ''Completed'';', @Norm);
SET @Aud = CHARINDEX(N'Auditor pass complete: keep the request Pending and still', @Norm);
SET @AudSet = CASE WHEN @Aud > 0 THEN CHARINDEX(N'SET @NewRequestStatus = ''Pending'';', @Norm, @Aud) ELSE 0 END;
SET @Partial = CHARINDEX(N'Partial approval: keep request Pending while days remain open.', @Norm);

IF @NonAud > 0 AND @CompletedSet > @NonAud AND @Aud > @CompletedSet
    PRINT N'CHECK non-Auditor Completed branch still present (accepted 39a12b0 baseline) | PASS';
ELSE
BEGIN
    PRINT N'CHECK non-Auditor Completed branch still present (accepted 39a12b0 baseline) | FAIL';
    SET @Fail += 1;
END;

IF @Aud > 0 AND @AudSet > @Aud AND @Partial > @AudSet
    PRINT N'CHECK Auditor terminal branch sets Pending and does not fall through to partial handling | PASS';
ELSE
BEGIN
    PRINT N'CHECK Auditor terminal branch sets Pending and does not fall through to partial handling | FAIL';
    SET @Fail += 1;
END;

IF CHARINDEX(N'IN (''Completed'', ''ReadyForPayment'')', @Norm) > 0
    PRINT N'CHECK payment lock still names ReadyForPayment | PASS';
ELSE
BEGIN
    PRINT N'CHECK payment lock still names ReadyForPayment | FAIL';
    SET @Fail += 1;
END;

IF CHARINDEX(N'= ''READYFORPAYMENT''', @Upper) > 0
   OR CHARINDEX(N'= N''READYFORPAYMENT''', @Upper) > 0
BEGIN
    PRINT N'CHECK definition does not assign ReadyForPayment | FAIL';
    SET @Fail += 1;
END
ELSE
    PRINT N'CHECK definition does not assign ReadyForPayment | PASS';

IF CHARINDEX(N'MAX(ISNULL(DetailVersion, 1))', @Norm) > 0
   AND CHARINDEX(N'MAX(d2.ApprovalRequestDetailId)', @Norm) > 0
   AND CHARINDEX(N'ORDER BY d2.ApprovalRequestDetailId DESC', @Norm) > 0
    PRINT N'CHECK DetailVersion then MAX(ApprovalRequestDetailId) markers present | PASS';
ELSE
BEGIN
    PRINT N'CHECK DetailVersion then MAX(ApprovalRequestDetailId) markers present | FAIL';
    SET @Fail += 1;
END;

SET @MaxAt = CASE WHEN @CreateAt > 0 THEN CHARINDEX(N'NVARCHAR(MAX)', @Norm, @CreateAt) ELSE 0 END;
SET @CloseAt = CASE WHEN @MaxAt > 0 THEN CHARINDEX(N')', @Norm, @MaxAt) ELSE 0 END;

IF @CreateAt > 0 AND @CloseAt > @CreateAt
BEGIN
    SET @Sig = SUBSTRING(@Norm, @CreateAt, @CloseAt - @CreateAt + 1);
    SET @AtCount = LEN(@Sig) - LEN(REPLACE(@Sig, N'@', N''));
END
ELSE
BEGIN
    SET @Sig = N'';
    SET @AtCount = -1;
END;

IF @AtCount = 4
   AND CHARINDEX(N'@ApprovalRequestDetailId', @Sig) > 0
   AND (
          CHARINDEX(N'@Action ', @Sig) > 0
       OR CHARINDEX(N'@Action' + NCHAR(9), @Sig) > 0
       OR CHARINDEX(N'@Action' + NCHAR(10), @Sig) > 0
       OR CHARINDEX(N'@Action,', @Sig) > 0
       OR CHARINDEX(N'@Action)', @Sig) > 0
      )
   AND CHARINDEX(N'@ActionBy', @Sig) > 0
   AND CHARINDEX(N'@Remarks', @Sig) > 0
   AND CHARINDEX(N'NVARCHAR(20)', @Sig) > 0
   AND CHARINDEX(N'NVARCHAR(100)', @Sig) > 0
   AND CHARINDEX(N'NVARCHAR(MAX)', @Sig) > 0
    PRINT N'CHECK parameter signature still four parameters | PASS';
ELSE
BEGIN
    PRINT N'CHECK parameter signature still four parameters | FAIL | at-count=' + CONVERT(nvarchar(12), @AtCount);
    SET @Fail += 1;
END;

SET @BeginCount = (LEN(@Norm) - LEN(REPLACE(@Norm, N'BEGIN TRANSACTION', N''))) / LEN(N'BEGIN TRANSACTION');
SET @CommitCount = (LEN(@Norm) - LEN(REPLACE(@Norm, N'COMMIT TRANSACTION', N''))) / LEN(N'COMMIT TRANSACTION');
SET @RollbackCount = (LEN(@Norm) - LEN(REPLACE(@Norm, N'ROLLBACK TRANSACTION', N''))) / LEN(N'ROLLBACK TRANSACTION');

PRINT N'INFO | BEGIN TRANSACTION count=' + CONVERT(nvarchar(12), @BeginCount)
    + N' COMMIT TRANSACTION count=' + CONVERT(nvarchar(12), @CommitCount)
    + N' ROLLBACK TRANSACTION count=' + CONVERT(nvarchar(12), @RollbackCount);

IF @BeginCount = 1 AND @CommitCount = 1 AND @RollbackCount = 1
   AND CHARINDEX(N'SET XACT_ABORT ON', @Norm) > 0
    PRINT N'CHECK single unnamed BEGIN, COMMIT, and ROLLBACK plus SET XACT_ABORT ON | PASS';
ELSE
BEGIN
    PRINT N'CHECK single unnamed BEGIN, COMMIT, and ROLLBACK plus SET XACT_ABORT ON | FAIL';
    SET @Fail += 1;
END;

PRINT N'UNVERIFIED | no C# override | not observable from sys.sql_modules';
PRINT N'UNVERIFIED | Employee cannot skip L1 | this procedure does not submit a claim';
PRINT N'UNVERIFIED | universal L1 cannot skip Auditor | true only while a higher Pending required approver exists';
PRINT N'G4-G7 | NOT AUTHORIZED';
PRINT N'RC014B definition | RESULT | ' + CASE WHEN @Fail = 0 THEN N'NO FAIL ROWS' ELSE N'FAIL' END
    + N' | fails=' + CONVERT(nvarchar(12), @Fail);
