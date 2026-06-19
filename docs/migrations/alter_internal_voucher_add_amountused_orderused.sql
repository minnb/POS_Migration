-- Migration: Thêm cột AmountUsed và OrderUsed vào bảng Internal_Voucher
-- Mục đích:
--   AmountUsed: lưu amount thực tế đã redeem từ voucher (có thể < mệnh giá)
--   OrderUsed:  lưu mã đơn hàng POS đã sử dụng voucher này (truy vết giao dịch)
-- Zero-downtime: cả hai cột đều NULLABLE → không ảnh hưởng records cũ

ALTER TABLE [dbo].[Internal_Voucher]
ADD [AmountUsed] DECIMAL(18,2) NULL,
    [OrderUsed]  NVARCHAR(50)  NULL;
GO
