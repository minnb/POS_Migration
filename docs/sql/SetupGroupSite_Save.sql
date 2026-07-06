/* ============================================================================
   11.1 Cài đặt CTKM — SP tạo/sửa 1 nhóm cửa hàng (dbo.SetupGroupSite) — RPOSMasterData
   Dùng bởi modal "CÀI ĐẶT NHÓM CỬA HÀNG" (sub-tab "Cài đặt nhóm CH/ST") trong
   trang Cài đặt CTKM (PromotionSetupPage). Upsert theo GroupCode; sinh ID thủ
   công qua UPDLOCK/HOLDLOCK vì bảng SetupGroupSite không có PK tự nhiên.
   ListStore lưu dạng JSON array string (vd ["1535","1561"]) hoặc literal "ALL"
   — Repository (C#) đã parse chuỗi nhập tay (";"/","/khoảng trắng) sang JSON
   trước khi gọi SP này, SP chỉ upsert thuần.
   ----------------------------------------------------------------------------
   CHẠY 1 LẦN trên RPOSMasterData. Script tự DROP PROCEDURE cũ nếu đã tồn tại.
   ============================================================================ */
USE [RPOSMasterData];
GO

IF OBJECT_ID(N'dbo.usp_SetupGroupSite_Save', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupGroupSite_Save;
GO

CREATE PROCEDURE dbo.usp_SetupGroupSite_Save
(
    @GroupCode nvarchar(50),
    @GroupName nvarchar(250),
    @ListStore nvarchar(max),   -- JSON array string hoặc "ALL"
    @UserName  nvarchar(50)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM dbo.SetupGroupSite WHERE GroupCode = @GroupCode)
        BEGIN
            UPDATE dbo.SetupGroupSite
            SET    GroupName      = @GroupName,
                   ListStore      = @ListStore,
                   LastUpdateUser = @UserName,
                   LastUpdateDate = GETDATE()
            WHERE  GroupCode = @GroupCode;
        END
        ELSE
        BEGIN
            DECLARE @newId int =
                (SELECT ISNULL(MAX(ID), 0) + 1 FROM dbo.SetupGroupSite WITH (UPDLOCK, HOLDLOCK));

            INSERT INTO dbo.SetupGroupSite
                (ID, GroupCode, GroupName, ListStore, CreatedDate, CreatedUser, Status)
            VALUES
                (@newId, @GroupCode, @GroupName, @ListStore, GETDATE(), @UserName, 1);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
