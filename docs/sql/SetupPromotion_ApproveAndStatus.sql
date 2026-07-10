/* ============================================================================
   11.1 Cài đặt CTKM — SP Duyệt + Đổi trạng thái — RPOSMasterData
   Duyệt: đánh dấu IsApprove=1 rồi publish draft → Offer* qua SP đã có
          [dbo].[Setup_Promotion_Insert] @BBY.
   CHẠY 1 LẦN trên RPOSMasterData.

   Cập nhật (POSLastCounter rollout, xem .claude/rules/masterdata-sync.md mục
   "Cập nhật POSLastCounter bất đồng bộ" — Pilot D): [dbo].[Setup_Promotion_Insert] là SP legacy
   production, KHÔNG được sửa, và không có OUTPUT Counter. Theo đúng mẫu Pilot C (SalesPrice/
   Setup_SalePrice_Get_ALL), usp_SetupPromotion_Approve đọc lại MAX(Counter) của 5 bảng
   OfferHeader/OfferBuy/OfferGet/OfferBenefits/OfferSite NGAY SAU khi EXEC Setup_Promotion_Insert
   chạy xong (vẫn trong transaction, trước COMMIT), gán vào 5 OUTPUT param mới — C#
   PromotionRepository.ApproveSetupAsync đọc lại rồi gọi ISyncTableTrackerService.Track() cho mỗi
   bảng. OfferPriority KHÔNG có OUTPUT ở đây — chưa tìm thấy write-path nào ghi bảng này trong
   toàn bộ ứng dụng (kể cả Setup_Promotion_Insert), nên không rollout Track() cho OfferPriority.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── Duyệt CTKM (publish) ───────────────────────────────────────────────── */
IF OBJECT_ID(N'dbo.usp_SetupPromotion_Approve', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupPromotion_Approve;
GO

CREATE PROCEDURE dbo.usp_SetupPromotion_Approve
(
    @BBYNR                     nvarchar(20),
    @OutOfferHeaderCounter     bigint = 0 OUTPUT,
    @OutOfferBuyCounter        bigint = 0 OUTPUT,
    @OutOfferGetCounter        bigint = 0 OUTPUT,
    @OutOfferBenefitsCounter   bigint = 0 OUTPUT,
    @OutOfferSiteCounter       bigint = 0 OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- error number 51002 (dải >=50000, không cần đăng ký sys.messages) — xem quy ước
    -- cùng dải 5100x trong SetupPromotion_Save.sql (51001), giúp C# phân biệt lỗi
    -- nghiệp vụ có chủ đích với lỗi kỹ thuật khác.
    IF NOT EXISTS (SELECT 1 FROM dbo.SetupPromotionHEADER WHERE BBYNR = @BBYNR)
    BEGIN
        DECLARE @notFoundMsg nvarchar(200) = FORMATMESSAGE(N'Không tìm thấy CTKM %s', @BBYNR);
        THROW 51002, @notFoundMsg, 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.SetupPromotionHEADER
        SET    IsApprove = 1
        WHERE  BBYNR = @BBYNR;

        -- Publish draft sang Offer* (SP nghiệp vụ đã có sẵn, legacy production — KHÔNG sửa)
        EXEC [dbo].[Setup_Promotion_Insert] @BBY = @BBYNR;

        -- Đọc lại Counter mới nhất SAU khi Setup_Promotion_Insert đã ghi xong — SP đó không có
        -- OUTPUT nên không lấy trực tiếp được (xem comment đầu file, Pilot D).
        SET @OutOfferHeaderCounter   = ISNULL((SELECT MAX(Counter) FROM dbo.OfferHeader), 0);
        SET @OutOfferBuyCounter      = ISNULL((SELECT MAX(Counter) FROM dbo.OfferBuy), 0);
        SET @OutOfferGetCounter      = ISNULL((SELECT MAX(Counter) FROM dbo.OfferGet), 0);
        SET @OutOfferBenefitsCounter = ISNULL((SELECT MAX(Counter) FROM dbo.OfferBenefits), 0);
        SET @OutOfferSiteCounter     = ISNULL((SELECT MAX(Counter) FROM dbo.OfferSite), 0);

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
