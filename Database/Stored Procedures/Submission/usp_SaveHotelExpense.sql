/*
    Object:      [dbo].[usp_SaveHotelExpense]
    Type:        STORED PROCEDURE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE PROCEDURE [dbo].[usp_SaveHotelExpense]
    @ActivityId INT,
    @FromDate DATE,
    @ToDate DATE,
    @BillNo NVARCHAR(200),
    @Amount DECIMAL(18,2),
    @Purpose NVARCHAR(1000),
    @AttachmentFileName NVARCHAR(500) = NULL,
    @AttachmentContent VARBINARY(MAX) = NULL,
    @AttachmentPath NVARCHAR(500) = NULL,
    @CreatedBy NVARCHAR(100) = NULL,

    -- newly added optional numeric param
    @ResolvedHotelCap DECIMAL(18,2) = NULL,

    -- optional policy/meta
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
        IF @ToDate < @FromDate
        BEGIN
            SET @ErrCode = 201;
            SET @ErrMessage = N'ToDate cannot be before FromDate';
            RETURN;
        END

        DECLARE @mode NVARCHAR(50) = LOWER(ISNULL(@PolicyMode, ''));

        IF (@mode = 'ownarrangement')
        BEGIN
            SET @ErrCode = 202;
            SET @ErrMessage = N'Policy mode "Own Arrangement" disallows Hotel claims.';
            RETURN;
        END

        IF (@ResolvedHotelCap IS NOT NULL AND @ResolvedHotelCap > 0)
        BEGIN
            DECLARE @days INT = DATEDIFF(day, @FromDate, @ToDate) + 1;
            IF @days < 1 SET @days = 1;
            DECLARE @allowedTotal DECIMAL(18,2) = @ResolvedHotelCap * CAST(@days AS DECIMAL(18,2));
            IF (@Amount > @allowedTotal)
            BEGIN
                SET @ErrCode = 203;
                SET @ErrMessage = N'Hotel amount exceeds policy cap. Allowed ' + CONVERT(NVARCHAR(30), @allowedTotal) + N' for ' + CONVERT(NVARCHAR(10), @days) + N' day(s).';
                RETURN;
            END
        END

        -- =================================================================
        -- NEW LOGIC: Calculate the next ExpenseSeq to prevent Unique Key errors
        -- =================================================================
        DECLARE @NextSeq INT;
        SELECT @NextSeq = ISNULL(MAX(ExpenseSeq), 0) + 1 
        FROM dbo.SalesDailyExpense WITH (UPDLOCK) 
        WHERE ActivityId = @ActivityId AND ExpenseType = 'Hotel';
        -- =================================================================

        INSERT INTO dbo.SalesDailyExpense
        (ActivityId, ExpenseType, ExpenseDate, FromDate, ToDate, BillNo, Amount, Purpose, AttachmentFileName, AttachmentContent, AttachmentPath, CreatedBy, CreatedDateTime,
         PolicyMode, PolicySnapshot, IsAutoDA, CreatedByIP, Source, ExpenseSeq) -- <-- Added ExpenseSeq here
        VALUES(@ActivityId, 'Hotel', @FromDate, @FromDate, @ToDate, @BillNo, @Amount, @Purpose, @AttachmentFileName, @AttachmentContent, @AttachmentPath, @CreatedBy, 
               GETUTCDATE(), @PolicyMode, @PolicySnapshot, @IsAutoDA, @CreatedByIP, @Source, @NextSeq); -- <-- Added @NextSeq here

        SET @ErrCode = 0;
        SET @ErrMessage = N'OK';
    END TRY
    BEGIN CATCH
        SET @ErrCode = ERROR_NUMBER();
        SET @ErrMessage = ERROR_MESSAGE();
        RETURN;
    END CATCH
END
