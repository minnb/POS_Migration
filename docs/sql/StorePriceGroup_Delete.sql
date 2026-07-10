/* ============================================================================
   Danh mục nhóm giá — SP xóa 1 nhóm giá (header + chi tiết store) — RPOSMasterData
   Dùng bởi trang "Danh mục nhóm giá" (PriceGroupsPage) trong menu Giá bán.

   NGHIỆP VỤ:
     - CHẶN xóa nếu PriceGroupCode đang được dùng làm SalesCode trong bảng giá dbo.SalesPrice
       (dòng còn hiệu lực: IsActive=1 AND YEAR(EndingDate)<>7777) → @Ok=0 + message, KHÔNG xóa.
     - Ngược lại: hard-delete dbo.StorePriceGroup + dbo.StorePriceGroupHeader theo PriceGroupCode.

   Kết quả trả qua OUTPUT param @Ok bit + @Message nvarchar(400). Lỗi → @Ok=0 + ERROR_MESSAGE().
   ----------------------------------------------------------------------------
   CHẠY 1 LẦN trên RPOSMasterData. Script tự DROP PROCEDURE cũ nếu đã tồn tại.
   ============================================================================ */
USE [RPOSMasterData];
GO

IF OBJECT_ID(N'dbo.usp_StorePriceGroup_Delete', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_StorePriceGroup_Delete;
GO

CREATE PROCEDURE dbo.usp_StorePriceGroup_Delete
(
    @PriceGroupCode varchar(20),
    @Ok             bit           OUTPUT,
    @Message        nvarchar(400) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        /* Chặn nếu nhóm giá đang được dùng trong bảng giá còn hiệu lực */
        IF EXISTS (SELECT 1 FROM dbo.SalesPrice
                   WHERE SalesCode = @PriceGroupCode
                     AND ISNULL(IsActive, 1) = 1
                     AND YEAR(EndingDate) <> 7777)
        BEGIN
            SET @Ok = 0;
            SET @Message = N'Nhóm giá đang được dùng trong bảng giá, không thể xóa';
            RETURN;
        END

        BEGIN TRANSACTION;

        DELETE FROM dbo.StorePriceGroup       WHERE PriceGroupCode = @PriceGroupCode;
        DELETE FROM dbo.StorePriceGroupHeader WHERE PriceGroupCode = @PriceGroupCode;

        COMMIT TRANSACTION;
        SET @Ok = 1;
        SET @Message = N'Xóa nhóm giá thành công';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @Ok = 0;
        SET @Message = ERROR_MESSAGE();
    END CATCH
END
GO
