/* ============================================================================
   Deactive 1 offer LIVE (dbo.OfferHeader) — RPOSMasterData
   Set Status=2 (Ngưng áp dụng) + Counter=MAX(Counter)+1 (bắt buộc để trigger
   delta-sync xuống ~5.000 máy POS — xem docs/architecture/database-schema.md
   mục OfferHeader + cơ chế LastCounter theo store).
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

IF OBJECT_ID(N'dbo.usp_OfferHeader_Deactivate', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_OfferHeader_Deactivate;
GO

CREATE PROCEDURE dbo.usp_OfferHeader_Deactivate
(
    @OfferNo  nvarchar(20)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.OfferHeader WHERE [No] = @OfferNo)
    BEGIN
        RAISERROR (N'Không tìm thấy Offer %s', 16, 1, @OfferNo);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @NewCounter bigint;
        SELECT  @NewCounter = ISNULL(MAX([Counter]), 0) + 1
        FROM    dbo.OfferHeader WITH (UPDLOCK, HOLDLOCK);

        UPDATE dbo.OfferHeader
        SET    [Status] = 2, [Counter] = @NewCounter
        WHERE  [No] = @OfferNo;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
