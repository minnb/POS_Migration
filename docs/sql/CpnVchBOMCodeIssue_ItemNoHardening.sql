/* ============================================================================
   CpnVchBOMCodeIssue — mở rộng ItemNo + thêm index — RPOSMasterData

   Hệ quả trực tiếp của việc gán ItemNo=ActicleNo cho voucher SAP (usp_Voucher_Create) và đồng
   bộ 2 chiều Coupon↔SAP Voucher (usp_SetupCoupon_SaveIssue/SaveAdvanced, usp_Voucher_GetByCode/
   Redeem — xem Voucher_Save.sql, Voucher_Read.sql, SetupCoupon_Save.sql cùng đợt):

   1. ItemNo varchar(20) quá hẹp so với ActicleNo varchar(50) — SAP gửi Article_No dài hơn 20
      ký tự sẽ làm INSERT lỗi cứng "String or binary data would be truncated".
   2. Chưa có index nào trên ItemNo — các UPDATE đồng bộ mới (chạy MỌI lần Lưu coupon, không chỉ
      lần đầu) sẽ quét toàn bảng nếu không có index, tốn kém khi bảng phình to theo thời gian.

   KHÔNG sửa lại CpnVchBOMCodeIssue_ExtendSchema.sql (đã chạy trước đó) — theo nguyên tắc không
   sửa migration đã apply, luôn nối tiếp migration mới. Cả 2 thao tác dưới đây đều NHẸ (mở rộng
   varchar + tạo index không unique) — không cần rebuild bảng, không cần cửa sổ bảo trì dài.

   CHẠY 1 LẦN trên RPOSMasterData, TRƯỚC SetupCoupon_Save.sql / Voucher_Read.sql / Voucher_Save.sql
   (bản đã sửa cùng đợt).
   ============================================================================ */
USE [RPOSMasterData];
GO

ALTER TABLE dbo.CpnVchBOMCodeIssue ALTER COLUMN ItemNo varchar(50) NULL;
GO

CREATE NONCLUSTERED INDEX IX_CpnVchBOMCodeIssue_ItemNo
    ON dbo.CpnVchBOMCodeIssue (ItemNo)
    WHERE ItemNo IS NOT NULL;
GO
