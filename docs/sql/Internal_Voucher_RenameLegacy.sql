/* ============================================================================
   ⚠️ ĐIỂM KHÔNG THỂ QUAY LẠI DỄ DÀNG — chỉ chạy sau khi:
     1. Đã deploy code mới (SAPService dùng IVoucherCodeRepository/CpnVchBOMCodeIssue).
     2. Đã chạy CpnVchBOMCodeIssue_MigrateFromInternalVoucher.sql ĐỦ 2 LẦN (trước và sau deploy).
     3. Đã smoke test luồng SAP (CreateNewVoucher/CheckVoucher/redeemCpnVch) OK trên Production.
     4. Đã theo dõi ổn định (log lỗi) một khoảng thời gian sau go-live.

   Sau khi chạy: nếu cần rollback code về bản cũ, code cũ SẼ LỖI CỨNG (bảng Internal_Voucher
   không còn tồn tại dưới tên đó) thay vì âm thầm ghi nhầm bảng — lựa chọn CHỦ ĐÍCH (fail loud
   thay vì silent data loss). Từ đây rollback phải forward-fix, không revert code đơn thuần.

   Internal_Voucher_Legacy giữ lại làm backup tạm — lên lịch DROP sau 2-4 tuần ổn định
   (docs/ROLLOUT.md §D6, docs/CHANGELOG.md — chưa chốt ngày cụ thể).
   ============================================================================ */
USE [RPOSMasterData];
GO

EXEC sp_rename 'dbo.Internal_Voucher', 'Internal_Voucher_Legacy';
GO
