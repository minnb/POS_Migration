/* ============================================================================
   Data migration 1 LẦN — đổi ngữ nghĩa cột dbo.CpnVchBOMHeader.IsCheckItem cho Voucher.

   Bối cảnh: trước 2026-07-06, Voucher dùng ngược nghĩa Coupon cho cùng cột IsCheckItem
   (kế thừa từ 2 module độc lập ở legacy VCM.BLUEPOS):
     - CŨ (Voucher): IsCheckItem = 1 → "tổng bill" (không có Lines); 0 → "theo sản phẩm" (có Lines).
     - MỚI (khớp Coupon): IsCheckItem = 1 → "theo sản phẩm" (có Lines); 0 → "tổng bill" (không Lines).

   Script này CHỈ đảo bit cho các dòng VOUCHER (ArticleType IN ('ZVCN','ZVCO')) trên bảng CHUNG
   CpnVchBOMHeader — TUYỆT ĐỐI KHÔNG đụng dòng Coupon (ArticleType IN ('ZCPN','ZCOU') và các
   ArticleType khác) vì Coupon giữ nguyên ngữ nghĩa cũ, không đổi.

   THỨ TỰ CHẠY (BẮT BUỘC — xem docs/ROLLOUT.md):
     1) Deploy code C# (POS.Web + POS.Application) đã đổi logic khớp nghĩa mới.
     2) Chạy lại docs/sql/SetupVoucher_Save.sql + docs/sql/SetupVoucher_SaveIssue.sql
        (DROP + CREATE lại 2 SP với logic @IsCheckItem=1 mới).
     3) Chạy script migration data này (đảo bit dữ liệu voucher đã tồn tại).
   Nếu chạy sai thứ tự (data migrate trước khi code+SP deploy) sẽ có khoảng hở: code cũ đọc/ghi
   theo nghĩa cũ trên dữ liệu đã bị đảo → sai ngược lại. CHẠY 1 LẦN trên RPOSMasterData UAT rồi
   Production, xác nhận qua SELECT COUNT trước/sau khi chạy UPDATE.
   ============================================================================ */
USE [RPOSMasterData];
GO

-- ── Bước 1: xác nhận phạm vi ảnh hưởng TRƯỚC khi UPDATE (chạy riêng, đối chiếu số dòng) ──
SELECT ArticleType, IsCheckItem, COUNT(*) AS SoDong
FROM   dbo.CpnVchBOMHeader
WHERE  ArticleType IN ('ZVCN', 'ZVCO')
GROUP BY ArticleType, IsCheckItem
ORDER BY ArticleType, IsCheckItem;
GO

-- ── Bước 2: đảo bit CHỈ cho dòng Voucher, giữ NULL nguyên trạng (không suy đoán) ──
BEGIN TRANSACTION;

UPDATE dbo.CpnVchBOMHeader
SET    IsCheckItem = CASE IsCheckItem
                          WHEN 1 THEN 0
                          WHEN 0 THEN 1
                          ELSE IsCheckItem   -- NULL giữ NULL
                      END
WHERE  ArticleType IN ('ZVCN', 'ZVCO');

COMMIT TRANSACTION;
GO

-- ── Bước 3: xác nhận lại SAU khi UPDATE — số dòng theo IsCheckItem phải đảo đúng so với Bước 1 ──
SELECT ArticleType, IsCheckItem, COUNT(*) AS SoDong
FROM   dbo.CpnVchBOMHeader
WHERE  ArticleType IN ('ZVCN', 'ZVCO')
GROUP BY ArticleType, IsCheckItem
ORDER BY ArticleType, IsCheckItem;
GO
