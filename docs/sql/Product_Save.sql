/* ============================================================================
   6.2 Tạo sản phẩm mới — RPOSMasterData
   Chuyển từ script INSERT inline (Dapper, CentralMDRepository.CreateProductAsync)
   sang stored procedure để dễ kiểm soát/gỡ lỗi. Giữ nguyên toàn bộ logic nghiệp vụ
   (sinh ItemNo tự động, giá trị mặc định insert dbo.Item/dbo.Barcodes) — chỉ sửa
   tên bảng sai dbo.Barcode → dbo.Barcodes (bug gốc gây lỗi "Invalid object name").
   - usp_Product_Save : sinh ItemNo tự động (Max+1, bắt đầu 1000000001), insert
                        dbo.Item + insert dbo.Barcodes (nhiều dòng qua TVP).
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── TVP ────────────────────────────────────────────────────────────────── */
IF TYPE_ID(N'dbo.ProductBarcodeTVP') IS NULL
CREATE TYPE dbo.ProductBarcodeTVP AS TABLE
(
    BarcodeNo         nvarchar(50)  NULL,
    UnitOfMeasureCode nvarchar(10)  NULL
);
GO

/* ── Tạo sản phẩm mới ─────────────────────────────────────────────────────── */
IF OBJECT_ID(N'dbo.usp_Product_Save', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_Product_Save;
GO

CREATE PROCEDURE dbo.usp_Product_Save
(
    @ItemName           nvarchar(50),
    @ItemNameFull       nvarchar(100),
    @BaseUnitOfMeasure  nvarchar(10),
    @SalesUnitOfMeasure nvarchar(10),
    @ItemFamilyCode     nvarchar(20),
    @TaxGroupCode       nvarchar(10),
    @Blocked            tinyint,
    @BlockedVINID       tinyint,
    @Barcodes           dbo.ProductBarcodeTVP READONLY,
    @OutItemNo          nvarchar(20) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now datetime = GETDATE();

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1) Auto-generate ItemNo (Max+1, start 1000000001)
        DECLARE @itemNo nvarchar(20) =
            (SELECT CAST(ISNULL(MAX(CAST(No AS BIGINT)), 1000000000) + 1 AS NVARCHAR(50))
             FROM dbo.Item (NOLOCK));
        IF @itemNo IS NULL SET @itemNo = '1000000001';

        -- 2) Max Counter cho Item
        DECLARE @itemCounter bigint = (SELECT ISNULL(MAX(Counter), 0) + 1 FROM dbo.Item (NOLOCK));

        -- 3) INSERT Item
        INSERT INTO dbo.Item
        (No, No2, Description, SearchDescription, LongDescription,
         BaseUnitOfMeasure, SalesUnitOfMeasure, TaxGroupCode,
         ItemFamilyCode, Blocked, BlockedVINID,
         PriceIncludesVAT, StatisticsGroup, CommissionGroup,
         UnitPrice, CostingMethod, UnitCost, StandardCost,
         VendorNo, VendorItemNo, MaximumInventory, ReorderQuantity,
         GrossWeight, NetWeight, UnitsPerParcel, UnitVolume,
         PreventNegativeInventory, MinimumOrderQuantity, MaximumOrderQuantity,
         SafetyStockQuantity, OrderMultiple,
         ManufacturerCode, ItemCategoryCode, ProductGroupCode,
         ServiceItemGroup, ItemTrackingCode, ProductionBOMNo,
         CommonItemNo, DivisionCode, KeyinginPrice, ZeroPriceValid,
         LastDateModified, Counter, Pkey)
        VALUES
        (@itemNo, '', @ItemName, @ItemName, @ItemNameFull,
         @BaseUnitOfMeasure, @SalesUnitOfMeasure, @TaxGroupCode,
         @ItemFamilyCode, @Blocked, @BlockedVINID,
         1, 0, 0,
         0, 0, 0, 0,
         '', '', 0, 0,
         0, 0, 0, 0,
         0, 0, 0,
         0, 0,
         '', '', '',
         '', '', '',
         '', '', 0, 0,
         @now, @itemCounter, @itemNo);

        -- 4) Max Counter cho Barcodes (SỬA: bảng thật là dbo.Barcodes, không phải dbo.Barcode)
        DECLARE @baseBarcodeCounter bigint = (SELECT ISNULL(MAX(Counter), 0) FROM dbo.Barcodes (NOLOCK));

        -- 5) INSERT Barcodes (SỬA: dbo.Barcodes) — Counter tăng dần theo thứ tự trong TVP
        INSERT INTO dbo.Barcodes
        (BarcodeNo, ItemNo, UnitOfMeasureCode, ShowForItem, Description,
         Blocked, LastDateModified, VariantCode, DiscountPercent, Counter, Pkey)
        SELECT b.BarcodeNo, @itemNo, b.UnitOfMeasureCode, 0, '',
               1, @now, '', 0, @baseBarcodeCounter + ROW_NUMBER() OVER (ORDER BY (SELECT 1)), b.BarcodeNo
        FROM   @Barcodes b;

        SET @OutItemNo = @itemNo;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
