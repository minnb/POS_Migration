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
   chạy xong (SP legacy tự quản transaction, KHÔNG bọc BEGIN TRAN ngoài — xem body SP), gán vào
   5 OUTPUT param mới — C#
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

    -- ⚠️  KHÔNG bọc BEGIN TRANSACTION quanh EXEC Setup_Promotion_Insert.
    -- [dbo].[Setup_Promotion_Insert] là SP legacy production TỰ QUẢN transaction và có nhánh
    -- ROLLBACK nội bộ khi gặp lỗi nghiệp vụ (vd ZB06/tổng-bill). Nếu bọc BEGIN TRANSACTION bên
    -- ngoài, ROLLBACK nội bộ của SP legacy làm trancount tụt 1→0 → SQL Server ném Error 266
    -- ("Transaction count after EXECUTE...") tại chính EXEC, NUỐT MẤT lỗi nghiệp vụ gốc (THROW/
    -- RAISERROR bên trong SP legacy) → C# không phân biệt được, hiện "Lỗi hệ thống".
    -- Đây đúng anti-pattern .claude/rules/database-standards.md cấm ("gọi SP legacy có ROLLBACK").
    -- Để SP legacy tự quản transaction → lỗi gốc nổi lên nguyên vẹn cho C# xử lý.
    -- SET XACT_ABORT ON (ở trên) vẫn đảm bảo abort sạch khi có lỗi.
    BEGIN TRY
        -- Publish draft sang Offer* TRƯỚC (SP nghiệp vụ đã có sẵn, legacy production — KHÔNG sửa).
        EXEC [dbo].[Setup_Promotion_Insert] @BBY = @BBYNR;

        -- ⚠️ Setup_Promotion_Insert NUỐT lỗi: BEGIN CATCH của nó chỉ ROLLBACK, KHÔNG THROW, KHÔNG
        -- ghi Interface_Errors → khi 1 CONVERT/INSERT lỗi (vd LIMIT='5.000'→int), cả transaction
        -- nội bộ rollback, SP trả về "thành công" nhưng 0 dòng sang Offer*. Verify tường minh:
        -- OfferHeader KHÔNG phụ thuộc site (luôn có ≥1 dòng nếu publish OK) → 0 dòng = publish hỏng.
        IF NOT EXISTS (SELECT 1 FROM dbo.OfferHeader WHERE [No] = @BBYNR)
        BEGIN
            DECLARE @pubFailMsg nvarchar(300) = FORMATMESSAGE(
                N'Duyệt CTKM %s thất bại: publish sang Offer* không tạo được dòng nào (Setup_Promotion_Insert đã rollback nội bộ — kiểm tra dữ liệu draft, vd cột LIMIT).',
                @BBYNR);
            THROW 51003, @pubFailMsg, 1;
        END

        -- Chỉ đánh dấu ĐÃ DUYỆT khi publish thành công — tránh IsApprove=1 mà chưa publish gì.
        UPDATE dbo.SetupPromotionHEADER
        SET    IsApprove = 1
        WHERE  BBYNR = @BBYNR;

        -- Đọc lại Counter mới nhất SAU khi Setup_Promotion_Insert đã ghi xong — SP đó không có
        -- OUTPUT nên không lấy trực tiếp được (xem comment đầu file, Pilot D).
        SET @OutOfferHeaderCounter   = ISNULL((SELECT MAX(Counter) FROM dbo.OfferHeader), 0);
        SET @OutOfferBuyCounter      = ISNULL((SELECT MAX(Counter) FROM dbo.OfferBuy), 0);
        SET @OutOfferGetCounter      = ISNULL((SELECT MAX(Counter) FROM dbo.OfferGet), 0);
        SET @OutOfferBenefitsCounter = ISNULL((SELECT MAX(Counter) FROM dbo.OfferBenefits), 0);
        SET @OutOfferSiteCounter     = ISNULL((SELECT MAX(Counter) FROM dbo.OfferSite), 0);
    END TRY
    BEGIN CATCH
        -- Không có transaction do SP này mở (SP legacy tự quản) → không ROLLBACK ở đây, chỉ
        -- ném lại lỗi gốc (không còn bị 266 che) để C# phân loại (business vs kỹ thuật).
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
