/* ============================================================================
   11.1 Cài đặt CTKM — SP tạo/sửa 1 nhóm sản phẩm (dbo.SetupGroupItem) — RPOSMasterData
   Dùng bởi modal "CÀI ĐẶT NHÓM SẢN PHẨM" trong trang Cài đặt CTKM (PromotionSetupPage)
   khi dòng Buy/Get chọn Loại = "Nhóm SP" (LineType=1). Bảng SetupGroupItem đã tồn tại
   sẵn trong DB (script gốc `src/legacy/Database/CentralMD.sql`) nhưng chưa có SP nào —
   legacy dùng EF LINQ trực tiếp. SP này port lại theo đúng khuôn usp_SetupGroupSite_Save.

   Hạn chế CHỦ Ý giữ nguyên theo legacy (SetupGroupBuyItem, SetupPromotionData.cs):
   - ListItemNo chỉ lưu JSON List<string> ItemNo — KHÔNG lưu UOM.
   - Khi GroupCode đã tồn tại, chỉ UPDATE GroupName — KHÔNG ghi đè lại ListItemNo.
   ----------------------------------------------------------------------------
   CHẠY 1 LẦN trên RPOSMasterData. Script tự DROP PROCEDURE cũ nếu đã tồn tại.
   ============================================================================ */
USE [RPOSMasterData];
GO

IF OBJECT_ID(N'dbo.usp_SetupGroupItem_Save', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupGroupItem_Save;
GO

CREATE PROCEDURE dbo.usp_SetupGroupItem_Save
(
    @GroupCode  nvarchar(50),
    @GroupName  nvarchar(250),
    @ListItemNo nvarchar(max),   -- JSON array string List<string> ItemNo — Repository đã validate + build
    @UserName   nvarchar(50)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM dbo.SetupGroupItem WHERE GroupCode = @GroupCode)
        BEGIN
            -- Khớp hạn chế legacy (SetupGroupBuyItem): nhóm đã tồn tại chỉ sửa được Tên,
            -- KHÔNG ghi đè lại ListItemNo.
            UPDATE dbo.SetupGroupItem
            SET    GroupName      = @GroupName,
                   LastUpdateUser = @UserName,
                   LastUpdateDate = GETDATE()
            WHERE  GroupCode = @GroupCode;
        END
        ELSE
        BEGIN
            DECLARE @newId int =
                (SELECT ISNULL(MAX(ID), 0) + 1 FROM dbo.SetupGroupItem WITH (UPDLOCK, HOLDLOCK));

            INSERT INTO dbo.SetupGroupItem
                (ID, GroupCode, GroupName, ListItemNo, CreatedDate, CreatedUser, Status)
            VALUES
                (@newId, @GroupCode, @GroupName, @ListItemNo, GETDATE(), @UserName, 1);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
