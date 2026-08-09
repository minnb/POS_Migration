/* ============================================================================
   11.1 Cài đặt CTKM — thêm cột SetupPromotionHEADER.MaxQuantity (giới hạn số
   lượng KM tối đa được hưởng — "Limit by customer" bổ trợ).
   ----------------------------------------------------------------------------
   BẮT BUỘC chạy TRƯỚC docs/sql/SetupPromotion_Save.sql (order 100) — proc đó
   ghi cột MaxQuantity do script này thêm. Xem manifest.json order 95 + ROLLOUT §D1.
   ----------------------------------------------------------------------------
   Idempotent (Track A, runOnce:false): chỉ ALTER khi cột chưa tồn tại.
   Cột này lưu tạm ở nhóm SetupPromotion* (nháp, Dashboard). Việc publish giá trị
   sang bảng OfferMaxQuantity là bước RIÊNG, có cổng xác nhận DBA/engine — xem
   docs/sql/SetupPromotion_Insert_AddMaxQty.sql (KHÔNG tự động apply).
   ============================================================================ */
USE [RPOSMasterData];
GO

IF COL_LENGTH(N'dbo.SetupPromotionHEADER', N'MaxQuantity') IS NULL
BEGIN
    ALTER TABLE dbo.SetupPromotionHEADER
        ADD MaxQuantity int NULL CONSTRAINT DF_SetupPromotionHEADER_MaxQuantity DEFAULT 0;
END
GO
