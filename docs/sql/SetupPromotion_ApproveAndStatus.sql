/* ============================================================================
   11.1 Cài đặt CTKM — SP Duyệt + Đổi trạng thái — RPOSMasterData
   Duyệt: đánh dấu IsApprove=1 rồi publish draft → Offer* qua SP đã có
          [dbo].[Setup_Promotion_Insert] @BBY.
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── Duyệt CTKM (publish) ───────────────────────────────────────────────── */
IF OBJECT_ID(N'dbo.usp_SetupPromotion_Approve', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupPromotion_Approve;
GO

CREATE PROCEDURE dbo.usp_SetupPromotion_Approve
(
    @BBYNR  nvarchar(20)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.SetupPromotionHEADER WHERE BBYNR = @BBYNR)
    BEGIN
        RAISERROR (N'Không tìm thấy CTKM %s', 16, 1, @BBYNR);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.SetupPromotionHEADER
        SET    IsApprove = 1
        WHERE  BBYNR = @BBYNR;

        -- Publish draft sang Offer* (SP nghiệp vụ đã có sẵn)
        EXEC [dbo].[Setup_Promotion_Insert] @BBY = @BBYNR;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ── Đổi trạng thái CTKM (Activated/Planned/Deactivated) ─────────────────── */
IF OBJECT_ID(N'dbo.usp_SetupPromotion_UpdateStatus', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupPromotion_UpdateStatus;
GO

CREATE PROCEDURE dbo.usp_SetupPromotion_UpdateStatus
(
    @BBYNR   nvarchar(20),
    @Status  nvarchar(10)
)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.SetupPromotionHEADER
    SET    STATUS = @Status
    WHERE  BBYNR = @BBYNR;
END
GO
