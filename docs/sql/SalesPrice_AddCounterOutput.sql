/* ============================================================================
   9.1/9.3 Bảng giá — bổ sung Counter đầu ra cho 3 SP ghi dbo.SalesPrice, phục vụ
   POS.Infrastructure.Sync.ISyncTableTrackerService.Track("SalesPrice", counter)
   (PriceRepository gọi ngay sau khi SP chạy thành công — xem
   .claude/rules/masterdata-sync.md mục "Cập nhật POSLastCounter bất đồng bộ").

   CHẠY THỦ CÔNG 1 LẦN trên RPOSMasterData, SAU KHI đã áp dụng:
     - docs/sql/SetupSalePrice_Save.sql
     - docs/sql/SalesPrice_EditDelete.sql
     - docs/sql/SalesPrice_EditDelete_AddSalesType.sql
   (ALTER PROCEDURE dưới đây thay thế toàn bộ thân 3 SP đã tồn tại — không đổi hành vi nghiệp vụ,
   chỉ thêm output Counter.)

   1. usp_SetupSalePrice_Save — thêm @OutCounter bigint OUTPUT (mặc định 0). Nhánh insert (Pkey
      chưa tồn tại) dùng @maxCounter đã tính sẵn; nhánh update (Pkey đã tồn tại) ủy quyền SP legacy
      [Setup_SalePrice_Get_ALL] (production, KHÔNG sửa) tự bump Counter nội bộ — SP đó không có
      OUTPUT nên KHÔNG lấy trực tiếp được. Giải pháp: đọc lại MAX(Counter) SAU khi cả 2 nhánh chạy
      xong — 1 câu SELECT thêm, chấp nhận được vì đây là thao tác lưu bulk (không phải hot path tần
      suất cao) và không có cách nào khác lấy giá trị đúng mà không sửa SP legacy production.
   2. usp_SalesPrice_UpdatePrice / usp_SalesPrice_SoftDelete — @newCounter đã tính sẵn trong SP,
      chỉ cần thêm vào result set (SELECT ... , @newCounter AS Counter) — KHÔNG cần OUTPUT param
      vì repository đã đọc kết quả qua QueryFirstOrDefaultAsync<PriceSaveResult> (Dapper tự map
      cột "Counter" mới vào property PriceSaveResult.Counter, additive — không phá field cũ).
   ============================================================================ */
USE [RPOSMasterData];
GO

/* 1) usp_SetupSalePrice_Save ------------------------------------------------ */
CREATE OR ALTER PROCEDURE dbo.usp_SetupSalePrice_Save
(
    @Lines      dbo.SetupSalePriceLineTVP READONLY,
    @Actor      nvarchar(200)  = NULL,
    @Ok         bit            = 0   OUTPUT,
    @Message    nvarchar(4000) = N'' OUTPUT,
    @OutCounter bigint         = 0   OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        DECLARE @Json nvarchar(max) =
        (
            SELECT
                L.Pkey                                        AS Pkey,
                CONVERT(varchar(10), L.StartingDate, 120)     AS FromDate,
                CONVERT(varchar(10), L.EndingDate,   120)     AS ToDate,
                CONVERT(real, L.UnitPrice)                    AS UnitPrice
            FROM @Lines L
            WHERE EXISTS (
                SELECT 1 FROM dbo.SalesPrice SP
                WHERE SP.Pkey = L.Pkey AND YEAR(SP.EndingDate) <> 7777)
            FOR JSON PATH
        );

        BEGIN TRAN;

            DECLARE @maxCounter bigint =
                ISNULL((SELECT MAX(Counter) FROM dbo.SalesPrice WITH (UPDLOCK, HOLDLOCK)
                        WHERE YEAR(EndingDate) <> 7777), 0) + 1;

            INSERT dbo.SalesPrice
                (ItemNo, SalesCode, StartingDate, CurrencyCode, UnitOfMeasureCode, UnitPrice,
                 PriceIncludesVAT, AllowInvoiceDisc, SalesType, MinimumQuantity, EndingDate,
                 VariantCode, AllowLineDisc, Counter, Pkey,IsActive)
            SELECT
                L.ItemNo, L.SalesCode, L.StartingDate, N'VND', L.UnitOfMeasureCode, L.UnitPrice,
                1, 1, L.SalesType, 1,
                CASE WHEN YEAR(L.EndingDate) = 9999 THEN CONVERT(datetime, '9999-01-01') ELSE L.EndingDate END,
                N'', 1, @maxCounter, L.Pkey,1
            FROM @Lines L
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.SalesPrice SP
                WHERE SP.Pkey = L.Pkey AND YEAR(SP.EndingDate) <> 7777);

        COMMIT;   -- đóng transaction của mình TRƯỚC khi gọi SP legacy

        IF @Json IS NOT NULL AND LEN(@Json) > 0
            EXEC dbo.Setup_SalePrice_Get_ALL @Json = @Json;

        -- Đọc lại Counter mới nhất SAU khi cả 2 nhánh (insert + update-qua-legacy) đã chạy xong —
        -- xem giải thích ở đầu file vì sao không lấy trực tiếp từ nhánh update được.
        SET @OutCounter = ISNULL((SELECT MAX(Counter) FROM dbo.SalesPrice), 0);

        SET @Ok = 1;
        SET @Message = N'Cập nhật thành công bảng giá';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SET @Ok = 0;
        SET @Message = ERROR_MESSAGE();
    END CATCH
END
GO

/* 2) usp_SalesPrice_UpdatePrice ---------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_SalesPrice_UpdatePrice
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
            SELECT CAST(0 AS bit) AS Ok, N'Giá bán phải lớn hơn 0' AS Message, CAST(0 AS bigint) AS Counter;
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
            SELECT CAST(0 AS bit) AS Ok, N'Không tìm thấy dữ liệu cần cập nhật' AS Message, CAST(0 AS bigint) AS Counter;
            RETURN;
        END

        IF @oldPrice = @UnitPrice
        BEGIN
            SELECT CAST(0 AS bit) AS Ok, N'Không có dữ liệu thay đổi' AS Message, CAST(0 AS bigint) AS Counter;
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

        SELECT CAST(1 AS bit) AS Ok, N'Success' AS Message, @newCounter AS Counter;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SELECT CAST(0 AS bit) AS Ok, ERROR_MESSAGE() AS Message, CAST(0 AS bigint) AS Counter;
    END CATCH
END
GO

/* 3) usp_SalesPrice_SoftDelete ------------------------------------------------ */
CREATE OR ALTER PROCEDURE [dbo].[usp_SalesPrice_SoftDelete]
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
            SELECT CAST(0 AS bit) AS Ok, N'Không tìm thấy dữ liệu cần xóa' AS Message, CAST(0 AS bigint) AS Counter;
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

        SELECT CAST(1 AS bit) AS Ok, N'Success' AS Message, @newCounter AS Counter;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SELECT CAST(0 AS bit) AS Ok, ERROR_MESSAGE() AS Message, CAST(0 AS bigint) AS Counter;
    END CATCH
END
GO
