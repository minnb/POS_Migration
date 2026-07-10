/* ============================================================================
   11.1 Cài đặt CTKM — Thêm cột dbo.SetupPromotionHEADER.NUMOFDAYSLIST (RPOSMasterData)
   Cột MỚI, HOÀN TOÀN TÁCH BIỆT với NUMOFDAYS cũ — lưu nhiều "ngày áp dụng CTKM
   trong tháng" dạng JSON array (vd "[1,15,20]"), phục vụ multi-select ở Dashboard
   (PromotionSetupPage, tab "Cài đặt nâng cao").

   LÝ DO KHÔNG tái dùng/đổi kiểu cột NUMOFDAYS cũ: SP legacy ĐANG CHẠY PRODUCTION
   `Setup_Promotion_Insert` (publish draft → OfferHeader cho POS đọc, KHÔNG thuộc
   repo này, KHÔNG được sửa) có dòng cứng:
       iif(H.NUMOFDAYS <> '', Convert(int, H.NUMOFDAYS), 0) [NumOfDays]
   Nếu NUMOFDAYS chứa JSON array thay vì 1 số nguyên đơn, Convert(int,...) sẽ NÉM
   LỖI ngay khi Duyệt CTKM (crash toàn bộ luồng publish). Do đó NUMOFDAYS giữ
   NGUYÊN 100% hành vi cũ; NUMOFDAYSLIST chỉ Dashboard đọc/ghi, KHÔNG publish
   sang OfferHeader (ngoài phạm vi task này — enforcement nhiều-ngày-trong-tháng
   ở phía POS/CentralSale, nếu cần, là 1 quyết định/khối lượng công việc riêng).

   Idempotent — chạy lại nhiều lần an toàn (chỉ ALTER nếu cột chưa có).
   CHẠY 1 LẦN trên RPOSMasterData, TRƯỚC khi chạy lại `docs/sql/SetupPromotion_Save.sql`
   (bản đã cập nhật thêm tham số @NumOfDaysList).
   ============================================================================ */
USE [RPOSMasterData];
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SetupPromotionHEADER') AND name = N'NUMOFDAYSLIST'
)
BEGIN
    ALTER TABLE dbo.SetupPromotionHEADER ADD [NUMOFDAYSLIST] nvarchar(200) NULL;
END
GO
