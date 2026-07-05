/* ============================================================================
   9.3 Setup Giá (Bulk Import) — TVP + SP lưu bảng giá — RPOSMasterData (CentralMD)
   Port từ VCM.BLUEPOS SetupPriceData.SaveSalesPrice / ValidateImport (EF + Setup_SalePrice_Get_ALL).

   Gồm:
     1. dbo.SetupSalePriceImportTVP  — TVP dùng cho validate import (repository chạy query inline).
     2. dbo.SetupSalePriceLineTVP    — TVP dùng cho lưu (usp_SetupSalePrice_Save).
     3. dbo.usp_SetupSalePrice_Save  — INSERT Pkey mới + ủy quyền cập nhật Pkey đã tồn tại.

   HÀNH VI (giữ nguyên legacy):
     - Chỉ xét các Pkey có YEAR(EndingDate) <> 7777 (bỏ qua bản ghi đánh dấu xóa).
     - Pkey CHƯA tồn tại  → INSERT mới, Counter = MAX(Counter còn hiệu lực)+1 (rỗng → 1),
       defaults: CurrencyCode='VND', PriceIncludesVAT=1, AllowInvoiceDisc=1, AllowLineDisc=1,
       MinimumQuantity=1, VariantCode=''.
     - Pkey ĐÃ tồn tại   → gọi SP có sẵn [dbo].[Setup_SalePrice_Get_ALL] @Json (đã proven trên
       production) với JSON [{Pkey, FromDate 'yyyy-MM-dd', ToDate, UnitPrice}] — GIỮ NGUYÊN logic update legacy.

   Trả kết quả qua OUTPUT param @Ok bit + @Message nvarchar(4000) (KHÔNG dùng result set — vì nhánh
   update gọi [Setup_SalePrice_Get_ALL] có SELECT Interface_Errors + ROLLBACK bên trong, không thể
   hứng/nuốt result set bằng INSERT...EXEC). Lỗi → @Ok=0 + ERROR_MESSAGE(), KHÔNG throw ra ngoài.

   ⚠️ SCHEMA (đối chiếu DDL hiện hành dbo.SalesPrice): bảng CHỈ có 15 cột — KHÔNG có
      IsActive / LastTimeUpdate / Id (khác EF model legacy .NET 4.6). INSERT vì vậy chỉ ghi
      15 cột thực có; "đánh dấu xóa" dựa hoàn toàn vào EndingDate (YEAR = 7777) + Counter,
      KHÔNG dùng IsActive. SalesType là cột [int] → giá trị SalesType (chuỗi mã) được SQL
      Server convert ngầm khi INSERT; đảm bảo mã hình thức bán hàng là số hợp lệ.

   ⚠️ PHỤ THUỘC: SP [dbo].[Setup_SalePrice_Get_ALL] phải tồn tại sẵn trên RPOSMasterData
      (đang dùng bởi hệ thống legacy) và cũng phải khớp schema 15 cột nói trên. CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* 1) TVP validate import ---------------------------------------------------- */
IF TYPE_ID(N'dbo.SetupSalePriceImportTVP') IS NULL
CREATE TYPE dbo.SetupSalePriceImportTVP AS TABLE
(
    ItemNo        nvarchar(50)  NULL,
    Uom           nvarchar(20)  NULL,
    Barcode       nvarchar(50)  NULL,
    UnitPrice     varchar(20)   NULL,
    StartingDate  varchar(20)   NULL,
    EndingDate    varchar(20)   NULL
);
GO

/* 2) TVP lưu bảng giá ------------------------------------------------------- */
IF TYPE_ID(N'dbo.SetupSalePriceLineTVP') IS NULL
CREATE TYPE dbo.SetupSalePriceLineTVP AS TABLE
(
    Pkey               nvarchar(200)  NOT NULL,
    ItemNo             nvarchar(50)   NOT NULL,
    SalesCode          nvarchar(50)   NOT NULL,
    SalesType          nvarchar(50)   NOT NULL,
    UnitOfMeasureCode  nvarchar(20)   NOT NULL,
    UnitPrice          float          NOT NULL,
    StartingDate       datetime       NOT NULL,
    EndingDate         datetime       NOT NULL
);
GO

/* 3) SP lưu ----------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.usp_SetupSalePrice_Save', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupSalePrice_Save;
GO

CREATE PROCEDURE dbo.usp_SetupSalePrice_Save
(
    @Lines   dbo.SetupSalePriceLineTVP READONLY,
    @Actor   nvarchar(200)  = NULL,
    @Ok      bit            = 0   OUTPUT,
    @Message nvarchar(4000) = N'' OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        /* Chuẩn bị JSON cho nhánh update legacy TRƯỚC (đọc, không ghi) */
        DECLARE @Json nvarchar(max) =
        (
            SELECT
                L.Pkey                                        AS Pkey,
                CONVERT(varchar(10), L.StartingDate, 120)     AS FromDate,  -- yyyy-MM-dd
                CONVERT(varchar(10), L.EndingDate,   120)     AS ToDate,
                CONVERT(real, L.UnitPrice)                    AS UnitPrice
            FROM @Lines L
            WHERE EXISTS (
                SELECT 1 FROM dbo.SalesPrice SP
                WHERE SP.Pkey = L.Pkey AND YEAR(SP.EndingDate) <> 7777)
            FOR JSON PATH
        );

        /* 3a) INSERT các Pkey CHƯA tồn tại — transaction RIÊNG, COMMIT ngay.
               KHÔNG bao lời gọi SP legacy trong transaction này (SP legacy tự quản
               transaction của nó — nếu lồng nhau, ROLLBACK/COMMIT của nó làm lệch
               @@TRANCOUNT và gây lỗi 266 "mismatching BEGIN and COMMIT"). */
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
                -- Chuẩn hóa sentinel "vô thời hạn" về 9999-01-01 CHO KHỚP nhánh update legacy
                -- (Setup_SalePrice_Get_ALL map 9999-12-31 → 9999-01-01). Nếu để 9999-12-31, lần
                -- cập nhật sau sẽ tạo khoảng "đuôi" thừa [ToDate+1 → 9999-12-31] do 9999-12-31 > 9999-01-01.
                CASE WHEN YEAR(L.EndingDate) = 9999 THEN CONVERT(datetime, '9999-01-01') ELSE L.EndingDate END,
                N'', 1, @maxCounter, L.Pkey,1
            FROM @Lines L
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.SalesPrice SP
                WHERE SP.Pkey = L.Pkey AND YEAR(SP.EndingDate) <> 7777);

        COMMIT;   -- đóng transaction của mình TRƯỚC khi gọi SP legacy

        /* 3b) Pkey ĐÃ tồn tại → ủy quyền SP update legacy (chạy NGOÀI transaction —
               SP legacy tự BEGIN/COMMIT/ROLLBACK, không còn nesting với ta).

               ⚠️ [Setup_SalePrice_Get_ALL] khi @IsInsert=1 TRẢ VỀ 1 result set
               (SELECT * FROM Interface_Errors). KHÔNG hứng bằng INSERT...EXEC được vì SP legacy
               có ROLLBACK bên trong → SQL báo "Cannot use the ROLLBACK statement within an
               INSERT-EXEC statement." Do đó KẾT QUẢ trả qua OUTPUT param @Ok/@Message thay vì
               result set: repository đọc output param SAU khi ExecuteNonQuery đã nuốt hết mọi
               result set thừa của SP legacy → không còn đọc nhầm → hết lỗi "Pkey đã tồn tại thất bại". */
        IF @Json IS NOT NULL AND LEN(@Json) > 0
            EXEC dbo.Setup_SalePrice_Get_ALL @Json = @Json;

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
