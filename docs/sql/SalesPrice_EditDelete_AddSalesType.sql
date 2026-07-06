/* ============================================================================
   9.1 Danh mục Bảng giá — Sửa giá + Xóa giá: bổ sung @SalesType để định vị đúng dòng
   RPOSMasterData (CentralMD). CHẠY THỦ CÔNG 1 LẦN (nối tiếp docs/sql/SalesPrice_EditDelete.sql
   đã áp dụng trước đó).

   Lý do: 1 item/uom/nhóm giá (SalesCode)/ngày hiệu lực (StartingDate) có thể có NHIỀU dòng khác
   nhau theo SalesType (hình thức bán hàng) — composite PK (ItemNo, SalesCode, StartingDate,
   UnitOfMeasureCode) KHÔNG đủ để định vị 1 dòng duy nhất trong trường hợp này. usp_SalesPrice_
   UpdatePrice / usp_SalesPrice_SoftDelete trước đó thiếu SalesType trong WHERE → có thể sửa/xóa
   nhầm dòng khi tồn tại nhiều SalesType trùng key còn lại. Bổ sung tham số @SalesType (rỗng =
   không lọc, giữ tương thích ngược) vào cả 2 proc.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* 1) Sửa giá (in-place UnitPrice) ------------------------------------------- */
ALTER PROCEDURE dbo.usp_SalesPrice_UpdatePrice
(
    @ItemNo             nvarchar(20),
    @SalesCode          nvarchar(20),
    @StartingDate       date,
    @UnitOfMeasureCode  nvarchar(10),
    @UnitPrice          float,
    @Actor              nvarchar(200) = NULL,
    @SalesType          nvarchar(50)  = ''
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
          AND CONVERT(date, StartingDate) = @StartingDate
          AND (@SalesType = '' OR SalesType = @SalesType);

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
               SET UnitPrice = @UnitPrice,
			   LastTimeUpdate = getdate()
            WHERE ItemNo = @ItemNo
              AND SalesCode = @SalesCode
              AND UnitOfMeasureCode = @UnitOfMeasureCode
              AND CONVERT(date, StartingDate) = @StartingDate
              AND (@SalesType = '' OR SalesType = @SalesType);

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

USE [RPOSMasterData]
GO
/****** Object:  StoredProcedure [dbo].[usp_SalesPrice_SoftDelete]    Script Date: 05/07/2026 10:15:27 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* 2) Xóa giá (soft-delete: EndingDate năm 7777) ----------------------------- */
ALTER PROCEDURE [dbo].[usp_SalesPrice_SoftDelete]
(
    @ItemNo             nvarchar(20),
    @SalesCode          nvarchar(20),
    @StartingDate       date,
    @UnitOfMeasureCode  nvarchar(10),
    @Actor              nvarchar(200) = NULL,
    @SalesType          nvarchar(50)  = ''
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
          AND CONVERT(date, StartingDate) = @StartingDate
          AND (@SalesType = '' OR SalesType = @SalesType);

        IF @Pkey IS NULL
        BEGIN
            SELECT CAST(0 AS bit) AS Ok, N'Không tìm thấy dữ liệu cần xóa' AS Message;
            RETURN;
        END

        DECLARE @newCounter bigint = ISNULL((SELECT MAX(Counter) FROM dbo.SalesPrice), 0) + 1;

        BEGIN TRAN;
            /* Soft-delete dòng đích: sentinel năm 7777 + bump Counter */
            UPDATE dbo.SalesPrice
               SET EndingDate = '7777-07-07', Counter = @newCounter,
			   LastTimeUpdate = getdate(),
			   IsActive = 0
            WHERE ItemNo = @ItemNo
              AND SalesCode = @SalesCode
              AND UnitOfMeasureCode = @UnitOfMeasureCode
              AND CONVERT(date, StartingDate) = @StartingDate
              AND (@SalesType = '' OR SalesType = @SalesType);

            /* Bump Counter các dòng còn lại cùng Pkey (giữ hành vi legacy DeletePrice) */
            UPDATE dbo.SalesPrice SET Counter = @newCounter
            WHERE Pkey = @Pkey
              AND NOT (ItemNo = @ItemNo
                       AND SalesCode = @SalesCode
                       AND UnitOfMeasureCode = @UnitOfMeasureCode
                       AND CONVERT(date, StartingDate) = @StartingDate
                       AND (@SalesType = '' OR SalesType = @SalesType));
        COMMIT;

        SELECT CAST(1 AS bit) AS Ok, N'Success' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SELECT CAST(0 AS bit) AS Ok, ERROR_MESSAGE() AS Message;
    END CATCH
END

