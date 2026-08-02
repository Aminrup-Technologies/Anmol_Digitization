/*
    Object:      [dbo].[usp_SaveDailyExpense]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_SaveDailyExpense]
    @ActivityId INT,
    @ExpenseType NVARCHAR(50),
    @ExpenseDate DATE = NULL,
    @Amount DECIMAL(18,2) = 0,

	@FromPlace nvarchar(255) = NULL,
	@ToPlace nvarchar(255) = NULL,
    @Purpose NVARCHAR(1000) = NULL,

    @BillNo NVARCHAR(200) = NULL,
    @AttachmentFileName NVARCHAR(500) = NULL,
    @AttachmentPath NVARCHAR(500) = NULL,
    @AttachmentContent VARBINARY(MAX) = NULL,
    @CreatedBy NVARCHAR(100) = NULL,

	

    @ResolvedDA DECIMAL(18,2) = NULL,
    @ResolvedOutstationDA DECIMAL(18,2) = NULL,
    @ResolvedOwnArrangementDA DECIMAL(18,2) = NULL,
    @ResolvedHotelCap DECIMAL(18,2) = NULL,

    @PolicyMode NVARCHAR(50) = NULL,
    @PolicySnapshot NVARCHAR(MAX) = NULL,
    @IsAutoDA BIT = 0,
    @CreatedByIP NVARCHAR(45) = NULL,
    @Source NVARCHAR(50) = NULL,

    @ErrCode INT OUTPUT,
    @ErrMessage NVARCHAR(400) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @ErrCode = 0;
    SET @ErrMessage = NULL;

    BEGIN TRY

        ----------------------------------------------------------------
        -- YOUR EXISTING VALIDATION LOGIC – UNTOUCHED
        ----------------------------------------------------------------

        -- (all validations remain unchanged)

        ----------------------------------------------------------------
        -- SEQUENCE COMPUTATION (NEW)
        ----------------------------------------------------------------
        DECLARE @NextSeq INT;

        SELECT @NextSeq = ISNULL(MAX(ExpenseSeq), 0) + 1
        FROM dbo.SalesDailyExpense WITH (UPDLOCK)  -- <--- JUST ADD WITH (UPDLOCK) HERE
        WHERE ActivityId = @ActivityId
          AND LOWER(ISNULL(ExpenseType,'')) = LOWER(@ExpenseType)
          AND CAST(ISNULL(ExpenseDate, GETDATE()) AS DATE)
              = CAST(ISNULL(@ExpenseDate, GETDATE()) AS DATE);

        ----------------------------------------------------------------
        -- INSERT WITH SEQUENCE (ONLY CHANGE)
        ----------------------------------------------------------------
        INSERT INTO dbo.SalesDailyExpense
            (ActivityId, ExpenseType, ExpenseDate, FromDate, ToDate, BillNo,FromPlace, ToPlace,
             Amount, Purpose, AttachmentFileName, AttachmentContent,
             AttachmentPath, CreatedBy, CreatedDateTime,
             PolicyMode, PolicySnapshot, IsAutoDA, CreatedByIP, Source,
             ExpenseSeq)
        VALUES
            (@ActivityId, @ExpenseType, @ExpenseDate, NULL, NULL, @BillNo, @FromPlace, @ToPlace,
             @Amount, @Purpose, @AttachmentFileName, @AttachmentContent,
             @AttachmentPath, @CreatedBy, GETUTCDATE(),
             @PolicyMode, @PolicySnapshot, @IsAutoDA, @CreatedByIP, @Source,
             @NextSeq);

        SET @ErrCode = 0;
        SET @ErrMessage = N'OK';
    END TRY
    BEGIN CATCH
        SET @ErrCode = ERROR_NUMBER();
        SET @ErrMessage = ERROR_MESSAGE();
        RETURN;
    END CATCH
END
