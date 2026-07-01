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
       IsActive=1, LastTimeUpdate=GETDATE(), defaults: CurrencyCode='VND', PriceIncludesVAT=1,
       AllowInvoiceDisc=1, AllowLineDisc=1, MinimumQuantity=1, VariantCode=''.
     - Pkey ĐÃ tồn tại   → gọi SP có sẵn [dbo].[Setup_SalePrice_Get_ALL] @Json (đã proven trên
       production) với JSON [{Pkey, FromDate 'yyyy-MM-dd', ToDate, UnitPrice}] — GIỮ NGUYÊN logic update legacy.

   Trả 1 row (Ok bit, Message). Lỗi → Ok=0 + ERROR_MESSAGE(), KHÔNG throw ra ngoài.

   ⚠️ PHỤ THUỘC: SP [dbo].[Setup_SalePrice_Get_ALL] phải tồn tại sẵn trên RPOSMasterData
      (đang dùng bởi hệ thống legacy). CHẠY 1 LẦN trên RPOSMasterData.
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
    @Lines dbo.SetupSalePriceLineTVP READONLY,
    @Actor nvarchar(200) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        /* Counter mới = MAX(Counter còn hiệu lực) + 1 (rỗng → 1) — giống legacy maxCounter+1 */
        DECLARE @maxCounter bigint =
            ISNULL((SELECT MAX(Counter) FROM dbo.SalesPrice WITH (UPDLOCK, HOLDLOCK)
                    WHERE YEAR(EndingDate) <> 7777), 0) + 1;

        /* 3a) INSERT các Pkey CHƯA tồn tại */
        INSERT dbo.SalesPrice
            (ItemNo, SalesCode, StartingDate, CurrencyCode, UnitOfMeasureCode, UnitPrice,
             PriceIncludesVAT, AllowInvoiceDisc, SalesType, MinimumQuantity, EndingDate,
             VariantCode, AllowLineDisc, IsActive, LastTimeUpdate, Counter, Pkey)
        SELECT
            L.ItemNo, L.SalesCode, L.StartingDate, N'VND', L.UnitOfMeasureCode, L.UnitPrice,
            1, 1, L.SalesType, 1, L.EndingDate,
            N'', 1, 1, GETDATE(), @maxCounter, L.Pkey
        FROM @Lines L
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.SalesPrice SP
            WHERE SP.Pkey = L.Pkey AND YEAR(SP.EndingDate) <> 7777);

        /* 3b) Pkey ĐÃ tồn tại → ủy quyền SP update legacy qua JSON */
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

        IF @Json IS NOT NULL AND LEN(@Json) > 0
            EXEC dbo.Setup_SalePrice_Get_ALL @Json = @Json;

        COMMIT;
        SELECT CAST(1 AS bit) AS Ok, N'Cập nhật thành công bảng giá' AS Message;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SELECT CAST(0 AS bit) AS Ok, ERROR_MESSAGE() AS Message;
    END CATCH
END
GO
