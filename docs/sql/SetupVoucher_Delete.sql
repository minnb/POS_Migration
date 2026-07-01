/* ============================================================================
   8.3 Danh mục Voucher — SP xóa — RPOSMasterData
   Port từ VCM.BLUEPOS VoucherData.DeleteVoucherHeaderList (EF: xóa header).
   Dọn kèm CpnVchBOMLine để không mồ côi. Trả (Deleted bit, Message).
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

IF OBJECT_ID(N'dbo.usp_SetupVoucher_Delete', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupVoucher_Delete;
GO

CREATE PROCEDURE dbo.usp_SetupVoucher_Delete
(
    @ItemNo  nvarchar(20)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.CpnVchBOMHeader WHERE ItemNo = @ItemNo)
    BEGIN
        SELECT CAST(0 AS bit) AS Deleted, N'Không tìm thấy dữ liệu' AS [Message];
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;
        DELETE FROM dbo.CpnVchBOMLine   WHERE ItemNo = @ItemNo;
        DELETE FROM dbo.CpnVchBOMHeader WHERE ItemNo = @ItemNo;
        COMMIT TRANSACTION;
        SELECT CAST(1 AS bit) AS Deleted, N'Xóa thành công' AS [Message];
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SELECT CAST(0 AS bit) AS Deleted, ERROR_MESSAGE() AS [Message];
    END CATCH
END
GO
