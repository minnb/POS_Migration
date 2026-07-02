/* ============================================================================
   8.3 Danh mục Voucher — SP cập nhật RIÊNG field Blocked — RPOSMasterData
   Dùng cho trang Xem voucher (/promotion/vouchers/issue?id=...): sau khi phát hành,
   voucher chỉ còn cho phép khóa/mở khóa (Blocked) — mọi field khác không được sửa lại.
   Trả (Ok bit, Message). CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

IF OBJECT_ID(N'dbo.usp_SetupVoucher_UpdateBlocked', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupVoucher_UpdateBlocked;
GO

CREATE PROCEDURE dbo.usp_SetupVoucher_UpdateBlocked
(
    @ItemNo   nvarchar(20),
    @Blocked  bit
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.CpnVchBOMHeader WHERE ItemNo = @ItemNo)
    BEGIN
        SELECT CAST(0 AS bit) AS Ok, N'Không tìm thấy voucher' AS [Message];
        RETURN;
    END

    UPDATE dbo.CpnVchBOMHeader
    SET    Blocked          = CASE WHEN @Blocked = 1 THEN 1 ELSE 0 END,
           LastDateModified = GETDATE()
    WHERE  ItemNo = @ItemNo;

    SELECT CAST(1 AS bit) AS Ok,
           N'Cập nhật trạng thái khóa voucher thành công' AS [Message];
END
GO
