/*
    Mark Ready Auditor-row package. Read-only.
    Authoring status: NOT EXECUTED as this file.

    Target, when a reviewer runs it:
      server   MUMANMOAPP01\SQLEXPRESS
      database AnmolDristiDev

    Refuses AnmolDristi. Does not call dbo.usp_MarkRequestReadyForPayment.
    Does not begin, commit, or roll back a transaction.
    Does not insert, update, or delete.

    Prints which module fingerprint is present:
      pre-image  97ACDA7C1DDD052C8AB53451316B1F5B11977C4EE6FB2BE73106578B6347A901
      reviewed   8B2ECBCE8AC258D67A2C3CFCC6C45CC385F01354B8F5D8B5594409889685E158
    Then returns the current residue for requests 121 and 100.
    A row in those results is an observation, not a pass.
*/
SET NOCOUNT ON;

IF (@@OPTIONS & 2) = 2
BEGIN
    PRINT N'NOT RUN | IMPLICIT_TRANSACTIONS is ON.';
    RETURN;
END;

IF @@TRANCOUNT > 0
BEGIN
    PRINT N'NOT RUN | @@TRANCOUNT is already greater than 0.';
    RETURN;
END;

IF DB_NAME() = N'AnmolDristi'
BEGIN
    PRINT N'NOT RUN | Refusing live database AnmolDristi.';
    RETURN;
END;

IF UPPER(CONVERT(nvarchar(256), SERVERPROPERTY(N'ServerName'))) <> N'MUMANMOAPP01\SQLEXPRESS'
   OR DB_NAME() <> N'AnmolDristiDev'
BEGIN
    PRINT N'NOT RUN | Refusing a server or database other than MUMANMOAPP01\SQLEXPRESS / AnmolDristiDev.';
    RETURN;
END;

DECLARE @ObjectId int = OBJECT_ID(N'dbo.usp_MarkRequestReadyForPayment', N'P');
DECLARE @Definition nvarchar(max) = OBJECT_DEFINITION(@ObjectId);
DECLARE @Hash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), @Definition)), 2);

PRINT N'Server=' + ISNULL(CONVERT(nvarchar(256), SERVERPROPERTY(N'ServerName')), N'<null>')
    + N' Database=' + ISNULL(DB_NAME(), N'<null>');
PRINT N'ObjectId=' + ISNULL(CONVERT(nvarchar(20), @ObjectId), N'<null>')
    + N' LEN=' + ISNULL(CONVERT(nvarchar(20), LEN(@Definition)), N'<null>')
    + N' SHA2_256=' + ISNULL(@Hash, N'<null>');

IF @Hash = '97ACDA7C1DDD052C8AB53451316B1F5B11977C4EE6FB2BE73106578B6347A901'
    PRINT N'FINGERPRINT | pre-image | Auditor-row update absent';
ELSE IF @Hash = '8B2ECBCE8AC258D67A2C3CFCC6C45CC385F01354B8F5D8B5594409889685E158'
    PRINT N'FINGERPRINT | reviewed apply candidate | Auditor-row update present';
ELSE
    PRINT N'FINGERPRINT | neither reviewed text';

SELECT
    DB_NAME() AS database_name,
    @ObjectId AS object_id,
    LEN(@Definition) AS definition_length,
    @Hash AS definition_sha256,
    CASE WHEN CHARINDEX(N'@Remarks', @Definition) > 0 THEN 1 ELSE 0 END AS has_remarks_parameter,
    CASE WHEN CHARINDEX(N'Consume the Auditor assignment', @Definition) > 0 THEN 1 ELSE 0 END AS has_auditor_row_update,
    CASE WHEN CHARINDEX(N'Only Auditor can mark Ready for Payment.', @Definition) > 0 THEN 1 ELSE 0 END AS has_auditor_guard,
    CASE WHEN CHARINDEX(N'Request not completed.', @Definition) > 0 THEN 1 ELSE 0 END AS has_completed_guard,
    CASE WHEN CHARINDEX(N'BEGIN TRAN', @Definition) > 0 THEN 1 ELSE 0 END AS has_explicit_transaction,
    CASE
        WHEN CHARINDEX(N'SalesDailyExpense', @Definition) > 0
          OR CHARINDEX(N'SalesApprovalRequestDetail', @Definition) > 0
        THEN 1 ELSE 0
    END AS touches_detail_or_expense;

SELECT
    p.parameter_id,
    p.name,
    TYPE_NAME(p.user_type_id) AS type_name,
    p.max_length,
    p.has_default_value
FROM sys.parameters AS p
WHERE p.object_id = @ObjectId
ORDER BY p.parameter_id;

SELECT
    r.ApprovalRequestId,
    r.Status,
    r.ModifiedBy,
    CONVERT(varchar(27), r.ModifiedOn, 121) AS modified_on,
    r.TotalKM,
    r.TotalClaimAmount,
    r.ApprovedKM,
    r.ApprovedClaimAmount,
    r.FinalApprovedAmount,
    r.TotalStationaryAmount,
    r.TotalDeductionAmount
FROM dbo.SalesApprovalRequest AS r
WHERE r.ApprovalRequestId IN (100, 121)
ORDER BY r.ApprovalRequestId;

SELECT
    a.ApprovalRequestApproverId,
    a.ApprovalRequestId,
    a.ApproverLoginId,
    a.ApproverRole,
    a.ApproverLevel,
    a.Status,
    a.ActionBy,
    CONVERT(varchar(27), a.ActionDate, 121) AS action_date,
    a.Remarks
FROM dbo.SalesApprovalRequestApprover AS a
WHERE a.ApprovalRequestId IN (100, 121)
ORDER BY a.ApprovalRequestId, a.ApproverLevel;

SELECT
    h.HistoryId,
    h.ApprovalRequestId,
    h.OldStatus,
    h.NewStatus,
    h.ChangedBy,
    CONVERT(varchar(27), h.ChangedOn, 121) AS changed_on,
    h.Remarks
FROM dbo.ApprovalRequestStatusHistory AS h
WHERE h.HistoryId IN (701, 702, 797, 798)
ORDER BY h.HistoryId;

SELECT
    r.ApprovalRequestId,
    a.ApprovalRequestApproverId
FROM dbo.SalesApprovalRequest AS r
INNER JOIN dbo.SalesApprovalRequestApprover AS a
    ON a.ApprovalRequestId = r.ApprovalRequestId
WHERE r.Status = N'ReadyForPayment'
  AND a.ApproverRole = N'Auditor'
  AND a.Status = N'Pending'
ORDER BY r.ApprovalRequestId;
