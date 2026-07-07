/* ============================================================================
   Ảnh sản phẩm — RPOSMasterData
   Bảng mới dbo.ProductImage (ItemNo, Uom, ImageBase64) — lưu 1 ảnh đại diện/sản
   phẩm (Uom = BaseUnitOfMeasure tại thời điểm tạo), mã hóa base64 (nvarchar(max),
   theo đúng convention đã dùng ở dbo.TenderTypeImage.Image — không dùng varbinary).
   - usp_ProductImage_Save : UPSERT theo khóa ghép (ItemNo, Uom).
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── Bảng ───────────────────────────────────────────────────────────────── */
IF OBJECT_ID(N'dbo.ProductImage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductImage
    (
        ItemNo      nvarchar(20)  NOT NULL,
        Uom         nvarchar(10)  NOT NULL,
        ImageBase64 nvarchar(max) NOT NULL,
        CONSTRAINT PK_ProductImage PRIMARY KEY (ItemNo, Uom)
    );
END
GO

/* ── Upsert ảnh sản phẩm ──────────────────────────────────────────────────── */
IF OBJECT_ID(N'dbo.usp_ProductImage_Save', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_ProductImage_Save;
GO

CREATE PROCEDURE dbo.usp_ProductImage_Save
(
    @ItemNo      nvarchar(20),
    @Uom         nvarchar(10),
    @ImageBase64 nvarchar(max)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM dbo.ProductImage (NOLOCK) WHERE ItemNo = @ItemNo AND Uom = @Uom)
        BEGIN
            UPDATE dbo.ProductImage
               SET ImageBase64 = @ImageBase64
             WHERE ItemNo = @ItemNo AND Uom = @Uom;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.ProductImage (ItemNo, Uom, ImageBase64)
            VALUES (@ItemNo, @Uom, @ImageBase64);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
