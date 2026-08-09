/* ============================================================================
   11.1 Cài đặt CTKM — PUBLISH giới hạn số lượng KM (MaxQuantity) sang OfferMaxQuantity
   ============================================================================
   ⚠️⚠️  SCRIPT CÓ CỔNG XÁC NHẬN — KHÔNG tự động apply, KHÔNG đăng ký manifest.json.
   ----------------------------------------------------------------------------
   LÝ DO GATE (đọc kỹ trước khi chạy):
   1. Bằng chứng (Grep toàn bộ docs/web/offers/offer_procedure.sql — 30 calc-proc):
      HIỆN KHÔNG calc-proc nào đọc bảng dbo.OfferMaxQuantity (0 tham chiếu). Cơ chế
      chặn số lượng KM đang hoạt động là: OfferBenefits/OfferGet.Quantity × OfferHeader.LimitQty
      (xem BLUEPOS_PRO_Cal_ZB006_ByOfferNo) và loại-trừ item qua bảng dbo.OfferRetrict.
      ⇒ Dữ liệu ghi vào OfferMaxQuantity sẽ CHƯA có tác dụng runtime cho tới khi ENGINE POS
      được cập nhật đọc bảng này (nằm ngoài repo POS.Web).
   2. Do đó BẮT BUỘC DBA/chủ engine POS xác nhận:
      (a) Đúng bảng target là OfferMaxQuantity (không phải OfferRetrict)?
      (b) Calc-proc POS sẽ được sửa để đọc OfferMaxQuantity?
      Chỉ khi có "CÓ" cho cả (a) và (b) mới chạy script này.
   ----------------------------------------------------------------------------
   THIẾT KẾ AN TOÀN (khác kế hoạch "ALTER thẳng Setup_Promotion_Insert"):
   Thay vì viết đè SP production 277 dòng (rủi ro cao, khó review, khó rollback),
   script này tạo 1 SP COMPANION riêng, độc lập, reversible:
       dbo.usp_SetupPromotion_PublishMaxQuantity @BBYNR
   SP này DELETE + INSERT dbo.OfferMaxQuantity từ MaxQuantity (nhập ở Dashboard) và
   danh sách SP hưởng KM đã publish (OfferBenefits/OfferGet) × cửa hàng (OfferSite).
   ----------------------------------------------------------------------------
   TÍCH HỢP (chọn 1, sau khi đã xác nhận gate):
   • Cách A (khuyến nghị): thêm 1 dòng vào cuối Setup_Promotion_Insert, TRƯỚC COMMIT:
         EXEC dbo.usp_SetupPromotion_PublishMaxQuantity @BBY;
   • Cách B: gọi ngay sau khi Duyệt (usp_SetupPromotion_Approve) trong cùng flow.
   Rollback: DROP PROCEDURE dbo.usp_SetupPromotion_PublishMaxQuantity + gỡ dòng EXEC.
   ============================================================================ */
USE [RPOSMasterData];
GO

IF OBJECT_ID(N'dbo.usp_SetupPromotion_PublishMaxQuantity', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupPromotion_PublishMaxQuantity;
GO

CREATE PROCEDURE dbo.usp_SetupPromotion_PublishMaxQuantity
(
    @BBYNR nvarchar(20)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Giá trị giới hạn nhập ở Dashboard (SetupPromotion_AddMaxQuantity.sql thêm cột này).
    DECLARE @MaxQty float =
        (SELECT TRY_CONVERT(float, MaxQuantity) FROM dbo.SetupPromotionHEADER WHERE BBYNR = @BBYNR);

    -- Replace-on-publish: luôn xóa trước để không tồn dữ liệu cũ khi user hạ MaxQuantity về 0.
    DELETE FROM dbo.OfferMaxQuantity WHERE OfferNo = @BBYNR;

    -- MaxQuantity <= 0 → không giới hạn riêng, chỉ xóa (đã làm ở trên) rồi thoát.
    IF (ISNULL(@MaxQty, 0) <= 0) RETURN;

    DECLARE @baseId int = (SELECT ISNULL(MAX(ID), 0) FROM dbo.OfferMaxQuantity WITH (UPDLOCK, HOLDLOCK));

    -- SP hưởng KM đã publish = OfferBenefits (loại tổng bill) UNION OfferGet (loại theo dòng),
    -- nhân với cửa hàng áp dụng (OfferSite). MaxQuantity giống nhau cho mọi (store, item).
    ;WITH BenefitItems AS
    (
        SELECT DISTINCT [No] AS ItemNo, [UnitOfMeasure] AS UOM
        FROM dbo.OfferBenefits WHERE OfferNo = @BBYNR AND ISNULL([No], '') <> ''
        UNION
        SELECT DISTINCT [No] AS ItemNo, [UnitOfMeasure] AS UOM
        FROM dbo.OfferGet WHERE OfferNo = @BBYNR AND ISNULL([No], '') <> ''
    ),
    Rows AS
    (
        SELECT s.StoreNo, b.ItemNo, b.UOM,
               ROW_NUMBER() OVER (ORDER BY s.StoreNo, b.ItemNo) AS rn
        FROM dbo.OfferSite s
        CROSS JOIN BenefitItems b
        WHERE s.OfferNo = @BBYNR
    )
    INSERT INTO dbo.OfferMaxQuantity
        (ID, OfferNo, StoreNo, ItemNo, UOM, MaxQuantity, Status, CreatedDate, IsDeleted)
    SELECT @baseId + rn, @BBYNR, StoreNo, ItemNo, UOM, @MaxQty, 1, GETDATE(), 0
    FROM Rows;
END
GO
