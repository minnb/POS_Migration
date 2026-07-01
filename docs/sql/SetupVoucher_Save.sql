/* ============================================================================
   8.3 Danh mục Voucher — SP lưu (upsert header + replace lines) — RPOSMasterData
   Port từ VCM.BLUEPOS VoucherData.CreateVoucherCoupon / UpdateVoucherHeader (EF).

   Khác biệt QUAN TRỌNG so với Coupon (8.1/8.2):
     - ItemNo voucher là SỐ THUẦN, seed '70000001' (KHÔNG prefix 'C'). Sinh ItemNo mới =
       MAX(ItemNo thuần số)+1 — BỎ QUA mã coupon 'C...' để không lỗi CAST (legacy int.Parse bị lỗi này).
     - IsCheckItem NGƯỢC nghĩa với coupon:
         IsCheckItem = 1 → "Áp dụng tổng bill"  → KHÔNG có dòng sản phẩm.
         IsCheckItem = 0 → "Áp dụng theo sản phẩm" → có dòng sản phẩm (replace).
     - Serial (CouponCode) BẮT BUỘC duy nhất trên CpnVchBOMHeader.
   Trả 1 row (Ok bit, Message, ItemNo). Lỗi nghiệp vụ (serial trùng) → Ok=0, KHÔNG throw.
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

IF TYPE_ID(N'dbo.VoucherLineTVP') IS NULL
CREATE TYPE dbo.VoucherLineTVP AS TABLE
(
    LineItemNo     nvarchar(20)   NULL,
    [Description]   nvarchar(100)  NULL,
    UnitOfMeasure  nvarchar(10)   NULL,
    Barcode        varchar(50)    NULL
);
GO

IF OBJECT_ID(N'dbo.usp_SetupVoucher_Save', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupVoucher_Save;
GO

CREATE PROCEDURE dbo.usp_SetupVoucher_Save
(
    @ItemNo          nvarchar(20),          -- rỗng = tạo mới (tự sinh số thuần)
    @Serial          nvarchar(50),          -- CouponCode — BẮT BUỘC, duy nhất
    @ItemName        nvarchar(300),
    @ArticleType     nvarchar(10),
    @UnitOfMeasure   nvarchar(10),
    @DiscountType    int,
    @DiscountValue   decimal(38,20),
    @ValueOfVoucher  decimal(38,20),
    @MaxAmount       decimal(38,20),
    @LimitQty        int,
    @IsCheckItem     bit,                    -- 1 = tổng bill (no lines); 0 = theo sản phẩm
    @Blocked         bit,
    @StartingDate    datetime,
    @EndingDate      datetime,
    @Lines           dbo.VoucherLineTVP READONLY,
    @Actor           nvarchar(100)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now datetime = GETDATE();
    DECLARE @isNew bit = CASE WHEN @ItemNo IS NULL OR LTRIM(RTRIM(@ItemNo)) = '' THEN 1 ELSE 0 END;
    DECLARE @blk tinyint = CASE WHEN @Blocked = 1 THEN 1 ELSE 0 END;

    -- ── Validate serial (CouponCode) trùng ──
    IF EXISTS (SELECT 1 FROM dbo.CpnVchBOMHeader
               WHERE CouponCode = @Serial AND (@isNew = 1 OR ItemNo <> @ItemNo))
    BEGIN
        SELECT CAST(0 AS bit) AS Ok,
               N'Số serial ' + @Serial + N' đã tồn tại. Vui lòng kiểm tra lại' AS [Message],
               CAST('' AS nvarchar(20)) AS ItemNo;
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @isNew = 1
        BEGIN
            -- Chỉ lấy MAX trên ItemNo thuần số (bỏ 'C...' của coupon) — seed 70000001.
            DECLARE @maxNum bigint =
                (SELECT MAX(CONVERT(bigint, ItemNo))
                 FROM dbo.CpnVchBOMHeader (NOLOCK)
                 WHERE ItemNo NOT LIKE '%[^0-9]%' AND LEN(ItemNo) BETWEEN 1 AND 18);
            SET @ItemNo = CONVERT(varchar(20), CASE WHEN @maxNum IS NULL OR @maxNum < 70000000
                                                    THEN 70000001 ELSE @maxNum + 1 END);
        END

        DECLARE @cntHeader bigint = (SELECT ISNULL(MAX(Counter), 0) FROM dbo.CpnVchBOMHeader);

        IF EXISTS (SELECT 1 FROM dbo.CpnVchBOMHeader WHERE ItemNo = @ItemNo)
        BEGIN
            UPDATE dbo.CpnVchBOMHeader
            SET    ItemName = @ItemName, UnitOfMeasure = @UnitOfMeasure, DiscountType = @DiscountType,
                   DiscountValue = @DiscountValue, ValueOfVoucher = @ValueOfVoucher, MaxAmount = @MaxAmount,
                   ArticleType = @ArticleType, CouponCode = @Serial, LimitQty = @LimitQty,
                   IsCheckItem = @IsCheckItem, Blocked = @blk, StartingDate = @StartingDate,
                   EndingDate = @EndingDate, LastDateModified = @now, Counter = @cntHeader + 1
            WHERE  ItemNo = @ItemNo;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.CpnVchBOMHeader
                (ItemNo, ItemName, UnitOfMeasure, DiscountType, DiscountValue, ValueOfVoucher,
                 MaxAmount, ArticleType, StartingDate, EndingDate, Blocked, CouponCode, LimitQty,
                 LastDateModified, IsCheckItem, Counter, Pkey, CpnVchType, IsCheckAPI, IsMultiUse, LimitQtyUsed)
            VALUES
                (@ItemNo, @ItemName, @UnitOfMeasure, @DiscountType, @DiscountValue, @ValueOfVoucher,
                 @MaxAmount, @ArticleType, @StartingDate, @EndingDate, @blk, @Serial, @LimitQty,
                 @now, @IsCheckItem, @cntHeader + 1, @ItemNo, '', 0, 0, 0);
        END

        -- Lines: replace. Chỉ giữ khi áp dụng theo sản phẩm (IsCheckItem = 0).
        DELETE FROM dbo.CpnVchBOMLine WHERE ItemNo = @ItemNo;
        IF @IsCheckItem = 0 AND EXISTS (SELECT 1 FROM @Lines)
        BEGIN
            DECLARE @cntLine bigint = (SELECT ISNULL(MAX(Counter), 0) FROM dbo.CpnVchBOMLine);
            INSERT INTO dbo.CpnVchBOMLine
                (ItemNo, LineNo, LineItemNo, [Description], UnitOfMeasure, Counter, Barcode, Pkey)
            SELECT @ItemNo,
                   ROW_NUMBER() OVER (ORDER BY (SELECT 1)),
                   ISNULL(l.LineItemNo, ''), ISNULL(l.[Description], ''), ISNULL(l.UnitOfMeasure, ''),
                   @cntLine + 1, ISNULL(l.Barcode, ''),
                   @ItemNo + '-' + ISNULL(l.LineItemNo, '') + '-' + ISNULL(l.Barcode, '')
            FROM (SELECT DISTINCT LineItemNo, [Description], UnitOfMeasure, Barcode FROM @Lines) l;
        END

        COMMIT TRANSACTION;

        SELECT CAST(1 AS bit) AS Ok,
               N'Lưu voucher ' + @ItemNo + N' thành công' AS [Message],
               @ItemNo AS ItemNo;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SELECT CAST(0 AS bit) AS Ok, ERROR_MESSAGE() AS [Message], CAST('' AS nvarchar(20)) AS ItemNo;
    END CATCH
END
GO
