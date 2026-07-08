-- Migration 004: thêm cột PinHash vào DashboardUsers — PIN gate cho SQL Console
-- Database  : RPOSMasterData (ConnectionStrings:CentralMD)
-- Mục đích  : mỗi tài khoản SystemAdmin có 1 PIN riêng (BCrypt hash) để mở khoá SqlConsolePage
-- Idempotent : có thể chạy lại nhiều lần, không lỗi

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'DashboardUsers' AND COLUMN_NAME = 'PinHash'
)
BEGIN
    ALTER TABLE DashboardUsers ADD PinHash NVARCHAR(200) NULL;
    -- NULL = chưa thiết lập PIN → SqlConsolePage từ chối truy cập (fail closed), xem ROLLOUT.md
    PRINT 'DashboardUsers.PinHash: cột đã thêm thành công.';
END
ELSE
BEGIN
    PRINT 'DashboardUsers.PinHash: đã tồn tại, bỏ qua.';
END
