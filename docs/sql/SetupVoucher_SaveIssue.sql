/* ============================================================================
   8.3 Phát hành Voucher (nâng cấp) — SP phát hành mã thật vào CpnVchBOMCodeIssue.
   RPOSMasterData (CentralMD). Song song luồng Coupon (SetupCoupon_Save.sql) nhưng:
     - ItemNo voucher = SỐ THUẦN (seed 70000001) — KHÔNG prefix 'C'.
     - KHÔNG ghi CpnVchBOMIssueRule → voucher vẫn nằm trong danh sách Voucher
       (usp_SetupVoucher_GetList lọc NOT EXISTS CpnVchBOMIssueRule).
     - Mã ghi vào CpnVchBOMCodeIssue với Source='VOUCHER' (nguồn thứ 3 cạnh 'COUPON'/'SAP').
     - IsCheckItem voucher khớp coupon (đổi 2026-07-06, trước đó NGƯỢC coupon):
       1 = theo sản phẩm (có lines); 0 = tổng bill (no lines).
   Điền đủ field dùng chung để POS.Api check/redeem (usp_Voucher_GetByCode/Redeem).
   CHẠY 1 LẦN trên RPOSMasterData (kèm chạy lại SetupVoucher_Read.sql cho GetDetail mới).
   Đổi ngữ nghĩa IsCheckItem 2026-07-06 → CHẠY LẠI script này (DROP+CREATE) trên RPOSMasterData
   UAT/PROD SAU KHI deploy code, kèm chạy docs/sql/Voucher_FlipIsCheckItem_Migration.sql.
   // TODO: confirm quy tắc phát hành Voucher với DBA.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* dbo.CouponCodeTVP + dbo.VoucherLineTVP đã tạo ở SetupCoupon_Save.sql / SetupVoucher_Save.sql. */
IF TYPE_ID(N'dbo.CouponCodeTVP') IS NULL
CREATE TYPE dbo.CouponCodeTVP AS TABLE
(
    Code  nvarchar(50)  NULL
);
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

/* ── Check trùng mã trong DB (toàn bảng, Code unique toàn cục) ──────────────── */
IF OBJECT_ID(N'dbo.usp_SetupVoucher_CheckCodesExist', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupVoucher_CheckCodesExist;
GO

CREATE PROCEDURE dbo.usp_SetupVoucher_CheckCodesExist
(
    @Codes  dbo.CouponCodeTVP READONLY
)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT c.Code
    FROM   dbo.CpnVchBOMCodeIssue c (NOLOCK)
    JOIN   @Codes t ON t.Code = c.Code;
END
GO

/* ── Danh sách mã đã phát hành theo ItemNo (Source='VOUCHER'), phân trang ───── */
IF OBJECT_ID(N'dbo.usp_SetupVoucher_GetCodes', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupVoucher_GetCodes;
GO

CREATE PROCEDURE dbo.usp_SetupVoucher_GetCodes
(
    @ItemNo      nvarchar(20),
    @PageSize    int,
    @PageNumber  int                          -- 0-based
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  c.ItemNo,
            c.Code,
            CAST(ISNULL(c.Enabled, 0) AS bit) AS Enable,
            ISNULL(c.Status, '') AS Status,
            c.AmountUsed,
            c.OrderUsed,
            COUNT(*) OVER() AS Total
    FROM    dbo.CpnVchBOMCodeIssue c (NOLOCK)
    WHERE   c.ItemNo = @ItemNo AND c.Source = 'VOUCHER'
    ORDER BY c.Code
    OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

/* ── Phát hành voucher (issue) ─────────────────────────────────────────────── */
IF OBJECT_ID(N'dbo.usp_SetupVoucher_SaveIssue', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupVoucher_SaveIssue;
GO

CREATE PROCEDURE dbo.usp_SetupVoucher_SaveIssue
(
    @ItemNo          nvarchar(20),          -- rỗng = tạo mới (tự sinh số thuần)
    @Serial          nvarchar(50),          -- CouponCode (tùy chọn; check trùng nếu <> '')
    @ItemName        nvarchar(300),
    @ArticleType     nvarchar(10),
    @UnitOfMeasure   nvarchar(10),
    @DiscountType    int,
    @DiscountValue   decimal(38,20),
    @ValueOfVoucher  decimal(38,20),
    @MaxAmount       decimal(38,20),
    @LimitQty        int,
    @IsCheckItem     bit,                    -- 1 = theo sản phẩm; 0 = tổng bill (no lines)
    @Blocked         bit,
    @StartingDate    datetime,
    @EndingDate      datetime,
    @Codes           dbo.CouponCodeTVP  READONLY,
    @Lines           dbo.VoucherLineTVP READONLY,
    @OutItemNo       nvarchar(20) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now datetime = GETDATE();
    DECLARE @isNew bit = CASE WHEN @ItemNo IS NULL OR LTRIM(RTRIM(@ItemNo)) = '' THEN 1 ELSE 0 END;
    DECLARE @blk tinyint = CASE WHEN @Blocked = 1 THEN 1 ELSE 0 END;

    -- Serial (CouponCode) trùng → lỗi nghiệp vụ rõ ràng (chỉ check khi có nhập serial).
    IF @Serial IS NOT NULL AND LTRIM(RTRIM(@Serial)) <> ''
       AND EXISTS (SELECT 1 FROM dbo.CpnVchBOMHeader
                   WHERE CouponCode = @Serial AND (@isNew = 1 OR ItemNo <> @ItemNo))
    BEGIN
        ;THROW 50001, N'Số serial đã tồn tại. Vui lòng kiểm tra lại', 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @isNew = 1
        BEGIN
            -- MAX trên ItemNo thuần số (bỏ 'C...' của coupon) — seed 70000001.
            DECLARE @maxNum bigint =
                (SELECT MAX(CONVERT(bigint, ItemNo))
                 FROM dbo.CpnVchBOMHeader (NOLOCK)
                 WHERE ItemNo NOT LIKE '%[^0-9]%' AND LEN(ItemNo) BETWEEN 1 AND 18);
            SET @ItemNo = CONVERT(varchar(20), CASE WHEN @maxNum IS NULL OR @maxNum < 70000000
                                                    THEN 70000001 ELSE @maxNum + 1 END);
        END

        SET @OutItemNo = @ItemNo;

        DECLARE @cntHeader bigint = (SELECT ISNULL(MAX(Counter), 0) FROM dbo.CpnVchBOMHeader);

        -- 1) Header: upsert (KHÔNG ghi CpnVchBOMIssueRule → giữ voucher tách khỏi coupon).
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

        -- 2) Codes: chỉ insert khi CHƯA có mã Source='VOUCHER' cho ItemNo (parity coupon).
        --    "AND Source='VOUCHER'" bắt buộc: tránh dòng SAP/Coupon trùng ItemNo làm IF bị "lừa".
        --    Điền đủ field dùng chung với voucher SAP để POS.Api check/redeem
        --    (Status='SOLD', Voucher_Currency='VND', CompanyCode='WCM', Value/VoucherType từ form).
        IF NOT EXISTS (SELECT 1 FROM dbo.CpnVchBOMCodeIssue WHERE ItemNo = @ItemNo AND Source = 'VOUCHER')
        BEGIN
            DECLARE @cntCode bigint = (SELECT ISNULL(MAX(Counter), 0) FROM dbo.CpnVchBOMCodeIssue);
            INSERT INTO dbo.CpnVchBOMCodeIssue
                (ItemNo, Code, Enabled, CreatedDate, Counter, Pkey, Source,
                 ActicleNo, ActicleType, Validity_From_Date, Expiry_Date, Voucher_Currency,
                 CompanyCode, Status, [Return], Value, VoucherType)
            SELECT @ItemNo, t.Code, 1, @now, @cntCode + 1, @ItemNo, 'VOUCHER',
                   @ItemNo, @ArticleType, CONVERT(date, @StartingDate), CONVERT(date, @EndingDate),
                   'VND', 'WCM', 'SOLD', 0, CAST(@DiscountValue AS decimal(18,2)), @ArticleType
            FROM   @Codes t
            WHERE  ISNULL(t.Code, '') <> '';
        END

        -- 2b) Đồng bộ field "chụp nhanh" theo Header mới nhất cho các mã ĐÃ tồn tại (mọi lần gọi).
        --     TUYỆT ĐỐI KHÔNG set Status/[Return]/Enabled (tránh reset mã đã REDEEM qua POS.Api).
        UPDATE dbo.CpnVchBOMCodeIssue
        SET    ActicleNo          = @ItemNo,
               ActicleType        = @ArticleType,
               Validity_From_Date = CONVERT(date, @StartingDate),
               Expiry_Date        = CONVERT(date, @EndingDate),
               Voucher_Currency   = 'VND',
               CompanyCode        = 'WCM',
               Value              = CAST(@DiscountValue AS decimal(18,2)),
               VoucherType        = @ArticleType
        WHERE  ItemNo = @ItemNo AND Source = 'VOUCHER';

        -- 3) Lines: replace. Chỉ giữ khi áp dụng theo sản phẩm (IsCheckItem = 1).
        DELETE FROM dbo.CpnVchBOMLine WHERE ItemNo = @ItemNo;
        IF @IsCheckItem = 1 AND EXISTS (SELECT 1 FROM @Lines)
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
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
