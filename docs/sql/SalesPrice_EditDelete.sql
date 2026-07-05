/* ============================================================================
   9.1 Danh mục Bảng giá — Sửa giá + Xóa giá (soft-delete) — RPOSMasterData (CentralMD)
   Port từ VCM.BLUEPOS SetupPriceData.UpdatePrice / DeletePrice (EF .NET 4.6).

   ⚠️ KHÁC schema legacy: bảng dbo.SalesPrice hiện KHÔNG có cột Id / IsActive / LastTimeUpdate
      (PK composite = ItemNo, SalesCode, StartingDate, UnitOfMeasureCode — xem
      docs/architecture/database-schema.md §SalesPrice). Do đó:
        • Định vị dòng bằng composite PK (khớp CONVERT(date, StartingDate) — chịu được cột datetime
          có phần giờ; giá setup luôn 00:00:00).
        • "Xóa" = SOFT-DELETE bằng sentinel EndingDate năm 7777 (đồng bộ điều kiện
          YEAR(EndingDate) <> 7777 của usp_SetupSalePrice_Save) — KHÔNG dùng IsActive.
        • Mọi thay đổi bump Counter = MAX(Counter)+1 để 5.000 POS re-sync (giữ hành vi legacy:
          cập nhật Counter cho toàn bộ dòng cùng Pkey).

   Trả 1 row (Ok bit, Message) — khớp DTO PriceSaveResult. Lỗi → Ok=0 + ERROR_MESSAGE(), không throw.
   CHẠY THỦ CÔNG 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* 1) Sửa giá (in-place UnitPrice) ------------------------------------------- */
IF OBJECT_ID(N'dbo.usp_SalesPrice_UpdatePrice', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SalesPrice_UpdatePrice;
GO
CREATE PROCEDURE dbo.usp_SalesPrice_UpdatePrice
(
    @ItemNo             nvarchar(20),
    @SalesCode          nvarchar(20),
    @StartingDate       date,
    @UnitOfMeasureCode  nvarchar(10),
    @UnitPrice          float,
    @Actor              nvarchar(200) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        IF @UnitPrice <= 0
        BEGIN
            SELECT CAST(0 AS bit) AS Ok, N'Giá bán phải lớn hơn 0' AS Message;
            RETURN;
        END

        DECLARE @Pkey varchar(50), @oldPrice float;
        SELECT TOP 1 @Pkey = Pkey, @oldPrice = UnitPrice
        FROM dbo.SalesPrice WITH (UPDLOCK, HOLDLOCK)
        WHERE ItemNo = @ItemNo
          AND SalesCode = @SalesCode
          AND UnitOfMeasureCode = @UnitOfMeasureCode
          AND CONVERT(date, StartingDate) = @StartingDate;

        IF @Pkey IS NULL
        BEGIN
            SELECT CAST(0 AS bit) AS Ok, N'Không tìm thấy dữ liệu cần cập nhật' AS Message;
            RETURN;
        END

        IF @oldPrice = @UnitPrice
        BEGIN
            SELECT CAST(0 AS bit) AS Ok, N'Không có dữ liệu thay đổi' AS Message;
            RETURN;
        END

        DECLARE @newCounter bigint = ISNULL((SELECT MAX(Counter) FROM dbo.SalesPrice), 0) + 1;

        BEGIN TRAN;
            UPDATE dbo.SalesPrice
               SET UnitPrice = @UnitPrice
            WHERE ItemNo = @ItemNo
              AND SalesCode = @SalesCode
              AND UnitOfMeasureCode = @UnitOfMeasureCode
              AND CONVERT(date, StartingDate) = @StartingDate;

            /* Bump Counter toàn bộ dòng cùng Pkey (gồm cả dòng vừa sửa) để POS re-pull */
            UPDATE dbo.SalesPrice SET Counter = @newCounter WHERE Pkey = @Pkey;
        COMMIT;

        SELECT CAST(1 AS bit) AS Ok, N'Success' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SELECT CAST(0 AS bit) AS Ok, ERROR_MESSAGE() AS Message;
    END CATCH
END
GO

/* 2) Xóa giá (soft-delete: EndingDate năm 7777) ----------------------------- */
IF OBJECT_ID(N'dbo.usp_SalesPrice_SoftDelete', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SalesPrice_SoftDelete;
GO
CREATE PROCEDURE dbo.usp_SalesPrice_SoftDelete
(
    @ItemNo             nvarchar(20),
    @SalesCode          nvarchar(20),
    @StartingDate       date,
    @UnitOfMeasureCode  nvarchar(10),
    @Actor              nvarchar(200) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        DECLARE @Pkey varchar(50);
        SELECT TOP 1 @Pkey = Pkey
        FROM dbo.SalesPrice WITH (UPDLOCK, HOLDLOCK)
        WHERE ItemNo = @ItemNo
          AND SalesCode = @SalesCode
          AND UnitOfMeasureCode = @UnitOfMeasureCode
          AND CONVERT(date, StartingDate) = @StartingDate;

        IF @Pkey IS NULL
        BEGIN
            SELECT CAST(0 AS bit) AS Ok, N'Không tìm thấy dữ liệu cần xóa' AS Message;
            RETURN;
        END

        DECLARE @newCounter bigint = ISNULL((SELECT MAX(Counter) FROM dbo.SalesPrice), 0) + 1;

        BEGIN TRAN;
            /* Soft-delete dòng đích: sentinel năm 7777 + bump Counter */
            UPDATE dbo.SalesPrice
               SET EndingDate = '7777-07-07', Counter = @newCounter
            WHERE ItemNo = @ItemNo
              AND SalesCode = @SalesCode
              AND UnitOfMeasureCode = @UnitOfMeasureCode
              AND CONVERT(date, StartingDate) = @StartingDate;

            /* Bump Counter các dòng còn lại cùng Pkey (giữ hành vi legacy DeletePrice) */
            UPDATE dbo.SalesPrice SET Counter = @newCounter
            WHERE Pkey = @Pkey
              AND NOT (ItemNo = @ItemNo
                       AND SalesCode = @SalesCode
                       AND UnitOfMeasureCode = @UnitOfMeasureCode
                       AND CONVERT(date, StartingDate) = @StartingDate);
        COMMIT;

        SELECT CAST(1 AS bit) AS Ok, N'Success' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SELECT CAST(0 AS bit) AS Ok, ERROR_MESSAGE() AS Message;
    END CATCH
END
GO
