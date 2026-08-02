/*
    Object:      [dbo].[usp_InsertTravel_WithLock]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_InsertTravel_WithLock]
  @ActivityId int,
  @ExpenseDate date,
  @Amount decimal(18,2),
  @TravelType nvarchar(50) = NULL,
  @FromPlace nvarchar(255) = NULL,
  @ToPlace nvarchar(255) = NULL,
  @BillNo nvarchar(100) = NULL,
  @AttachmentFileName nvarchar(255) = NULL,
  @AttachmentPath nvarchar(500) = NULL,
  @Purpose nvarchar(250) = NULL,
  @PolicyMode nvarchar(50) = NULL,
  @PolicySnapshot nvarchar(max) = NULL,
  @CreatedBy nvarchar(100) = NULL,
  @CreatedByIP nvarchar(50) = NULL,
  @Source nvarchar(50) = 'UI',
  -- outputs
  @IsFirst bit OUTPUT,           -- 1 if there were no Travel rows before insert
  @InsertedExpenseId int OUTPUT, -- the new Expense PK (if you have one)
  @ErrCode int OUTPUT,
  @ErrMessage nvarchar(400) OUTPUT
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;
  BEGIN TRY
    DECLARE @resource nvarchar(200) = CONCAT('SalesDailyExpense_', @ActivityId, '_', FORMAT(@ExpenseDate,'yyyyMMdd'));
    DECLARE @lockRc int;

    -- Acquire application lock (10s timeout here; tune as needed)
    EXEC @lockRc = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 10000;

    IF @lockRc < 0
    BEGIN
      SET @ErrCode = -1;
      SET @ErrMessage = 'Could not acquire applock';
      RETURN;
    END

    BEGIN TRAN;

    -- 1) check existing travel count (before insert)
    DECLARE @existingTravelCount int;
    SELECT @existingTravelCount =
      COUNT(1)
    FROM dbo.SalesDailyExpense WITH (UPDLOCK, HOLDLOCK)
    WHERE ActivityId = @ActivityId
      AND LOWER(ExpenseType) = 'travel'
      AND CAST(ExpenseDate AS date) = @ExpenseDate
      AND IsDeleted = 0;  -- <--- YOU MUST ADD THIS LINE!

    SET @IsFirst = CASE WHEN @existingTravelCount = 0 THEN 1 ELSE 0 END;

    -- 2) compute next ExpenseSeq
    DECLARE @nextSeq int;
    SELECT @nextSeq = ISNULL(MAX(ExpenseSeq), 0) + 1
    FROM dbo.SalesDailyExpense
    WHERE ActivityId = @ActivityId
      AND CAST(ExpenseDate AS date) = @ExpenseDate
      AND LOWER(ExpenseType) = 'travel';

    -- 3) insert travel row (adjust columns to match your table exactly)
    INSERT INTO dbo.SalesDailyExpense
      (ActivityId, ExpenseType, ExpenseDate, BillNo, Amount, Currency, Purpose,
       AttachmentFileName, AttachmentContent, CreatedBy, CreatedDateTime,
       TravelType, FromPlace, ToPlace, TravelDate,
       PolicyMode, PolicySnapshot, IsAutoDA, CreatedByIP, Source, AttachmentPath, ExpenseSeq)
    VALUES
      (@ActivityId, N'Travel', @ExpenseDate, @BillNo, @Amount, NULL, @Purpose,
       @AttachmentFileName, 0x, @CreatedBy, SYSUTCDATETIME(),
       @TravelType, @FromPlace, @ToPlace, @ExpenseDate,
       @PolicyMode, @PolicySnapshot, 0, @CreatedByIP, @Source, @AttachmentPath, @nextSeq);

    -- capture inserted PK (assumes identity column named ExpenseId or SalesDailyExpense has identity PK)
    SET @InsertedExpenseId = SCOPE_IDENTITY();

    COMMIT TRAN;

    -- release lock (optional; session-level lock will be released when connection closes)
    EXEC sp_releaseapplock @Resource = @resource, @LockOwner = 'Session';

    SET @ErrCode = 0;
    SET @ErrMessage = 'OK';
  END TRY
  BEGIN CATCH
    IF XACT_STATE() <> 0
      ROLLBACK TRAN;

    -- try release applock best-effort
    BEGIN TRY
      EXEC sp_releaseapplock @Resource = @resource, @LockOwner = 'Session';
    END TRY
    BEGIN CATCH
    END CATCH

    SET @ErrCode = ERROR_NUMBER();
    SET @ErrMessage = ERROR_MESSAGE();
    RETURN;
  END CATCH
END
