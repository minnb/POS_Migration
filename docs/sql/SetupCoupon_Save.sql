/* ============================================================================
   8.2 Setup Coupon — SP lưu (phát hành + nâng cao) + check trùng mã — RPOSMasterData
   Port từ VCM.BLUEPOS SetupCouponData.SaveCoupon / SaveAdvancedCoupon (EF, transaction).
   - usp_SetupCoupon_SaveIssue    : upsert IssueRule + Header, insert Codes (1 lần),
                                     replace Lines, replace Stores. Tự sinh ItemNo "C7...".
   - usp_SetupCoupon_SaveAdvanced : upsert IssueRule + Header (field nâng cao).
   - usp_SetupCoupon_CheckCodesExist : trả các mã đã tồn tại trong DB.
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── TVP ────────────────────────────────────────────────────────────────── */
IF TYPE_ID(N'dbo.CouponCodeTVP') IS NULL
CREATE TYPE dbo.CouponCodeTVP AS TABLE
(
    Code  nvarchar(50)  NULL
);
GO

IF TYPE_ID(N'dbo.CouponLineTVP') IS NULL
CREATE TYPE dbo.CouponLineTVP AS TABLE
(
    LineItemNo     nvarchar(20)   NULL,
    [Description]   nvarchar(100)  NULL,
    UnitOfMeasure  nvarchar(10)   NULL,
    Barcode        varchar(50)    NULL
);
GO

/* ── Check trùng mã trong DB ─────────────────────────────────────────────── */
IF OBJECT_ID(N'dbo.usp_SetupCoupon_CheckCodesExist', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupCoupon_CheckCodesExist;
GO

CREATE PROCEDURE dbo.usp_SetupCoupon_CheckCodesExist
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

/* ── Phát hành coupon (issue) ────────────────────────────────────────────── */
IF OBJECT_ID(N'dbo.usp_SetupCoupon_SaveIssue', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupCoupon_SaveIssue;
GO

CREATE PROCEDURE dbo.usp_SetupCoupon_SaveIssue
(
    @ItemNo          nvarchar(20),         -- rỗng = tạo mới (tự sinh "C7...")
    @Description     nvarchar(300),
    @ArticleType     nvarchar(10),
    @SaleType        varchar(50),
    @StoreGroupCode  varchar(50),
    @StartingDate    datetime,
    @EndingDate      datetime,
    @Prefix          nvarchar(50),
    @LenCode         int,
    @IssueType       varchar(20),
    @CharOfNumber    int,
    @CharPosition    int,
    @IsCheckItem     bit,
    @Codes           dbo.CouponCodeTVP  READONLY,
    @Lines           dbo.CouponLineTVP  READONLY,
    @OutItemNo       nvarchar(20) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now datetime = GETDATE();

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @isNew bit = CASE WHEN @ItemNo IS NULL OR LTRIM(RTRIM(@ItemNo)) = '' THEN 1 ELSE 0 END;

        IF @isNew = 1
        BEGIN
            DECLARE @maxNum bigint =
                (SELECT MAX(TRY_CONVERT(bigint, SUBSTRING(ItemNo, 2, LEN(ItemNo))))
                 FROM dbo.CpnVchBOMHeader (NOLOCK) WHERE LEFT(ItemNo, 1) = 'C');
            SET @ItemNo = 'C' + CONVERT(varchar(20), CASE WHEN @maxNum IS NULL THEN 70000001 ELSE @maxNum + 1 END);
        END

        SET @OutItemNo = @ItemNo;

        DECLARE @cntRule   bigint = (SELECT ISNULL(MAX(Counter), 0) FROM dbo.CpnVchBOMIssueRule);
        DECLARE @cntHeader bigint = (SELECT ISNULL(MAX(Counter), 0) FROM dbo.CpnVchBOMHeader);

        -- 1) IssueRule: upsert
        IF EXISTS (SELECT 1 FROM dbo.CpnVchBOMIssueRule WHERE ItemNo = @ItemNo)
        BEGIN
            UPDATE dbo.CpnVchBOMIssueRule
            SET    Prefix = @Prefix, LenCode = @LenCode, IssueType = @IssueType,
                   CharOfNumber = @CharOfNumber, CharPosition = @CharPosition,
                   Counter = @cntRule + 1, CreatedDate = @now
            WHERE  ItemNo = @ItemNo;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.CpnVchBOMIssueRule
                (ItemNo, Prefix, LenCode, IssueType, CharOfNumber, CharPosition, Counter, CreatedDate, Pkey)
            VALUES
                (@ItemNo, @Prefix, @LenCode, @IssueType, @CharOfNumber, @CharPosition, @cntRule + 1, @now, @ItemNo);
        END

        -- 2) Header: upsert
        IF EXISTS (SELECT 1 FROM dbo.CpnVchBOMHeader WHERE ItemNo = @ItemNo)
        BEGIN
            UPDATE dbo.CpnVchBOMHeader
            SET    ItemName = @Description, StartingDate = @StartingDate, EndingDate = @EndingDate,
                   LastDateModified = @now, SaleType = @SaleType, StoreGroupCode = @StoreGroupCode,
                   ArticleType = @ArticleType, IssueType = @IssueType, IsCheckItem = @IsCheckItem,
                   Counter = @cntHeader + 1
            WHERE  ItemNo = @ItemNo;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.CpnVchBOMHeader
                (ItemNo, ItemName, UnitOfMeasure, DiscountType, DiscountValue, ValueOfVoucher,
                 StartingDate, EndingDate, MaxAmount, LastDateModified, Blocked, CouponCode,
                 LimitQty, IsCheckAPI, IsMultiUse, LimitQtyUsed, SaleType, StoreGroupCode, Pkey,
                 ArticleType, CpnVchType, IssueType, IsCheckItem, Counter)
            VALUES
                (@ItemNo, @Description, '', 0, 0, 0,
                 @StartingDate, @EndingDate, 0, @now, 0, '',
                 0, 1, 1, 0, @SaleType, @StoreGroupCode, @ItemNo,
                 @ArticleType, '', @IssueType, 0, @cntHeader + 1);
        END

        -- 3) Codes: chỉ insert khi chưa có mã nào cho ItemNo (parity legacy checkIssueCode==0)
        IF NOT EXISTS (SELECT 1 FROM dbo.CpnVchBOMCodeIssue WHERE ItemNo = @ItemNo)
        BEGIN
            DECLARE @cntCode bigint = (SELECT ISNULL(MAX(Counter), 0) FROM dbo.CpnVchBOMCodeIssue);
            INSERT INTO dbo.CpnVchBOMCodeIssue (ItemNo, Code, Enabled, CreatedDate, Counter, Pkey)
            SELECT @ItemNo, t.Code, 1, @now, @cntCode + 1, @ItemNo
            FROM   @Codes t
            WHERE  ISNULL(t.Code, '') <> '';
        END

        -- 4) Lines: replace
        DELETE FROM dbo.CpnVchBOMLine WHERE ItemNo = @ItemNo;
        IF EXISTS (SELECT 1 FROM @Lines)
        BEGIN
            DECLARE @cntLine bigint = (SELECT ISNULL(MAX(Counter), 0) FROM dbo.CpnVchBOMLine);
            INSERT INTO dbo.CpnVchBOMLine
                (ItemNo, LineNo, LineItemNo, [Description], UnitOfMeasure, Counter, Barcode, Pkey)
            SELECT @ItemNo,
                   ROW_NUMBER() OVER (ORDER BY (SELECT 1)),
                   ISNULL(l.LineItemNo, ''), ISNULL(l.[Description], ''), ISNULL(l.UnitOfMeasure, ''),
                   @cntLine + 1, ISNULL(l.Barcode, ''),
                   @ItemNo + '-' + ISNULL(l.LineItemNo, '') + '-' + ISNULL(l.Barcode, '')
            FROM   @Lines l;
        END

        -- 5) Stores: replace (ALL → 1 dòng; ngược lại bung theo StoreGroup)
        DELETE FROM dbo.CpnVchBOMStore WHERE ItemNo = @ItemNo;
        DECLARE @cntStore bigint = (SELECT ISNULL(MAX(Counter), 0) FROM dbo.CpnVchBOMStore);
        IF @StoreGroupCode = 'ALL'
        BEGIN
            INSERT INTO dbo.CpnVchBOMStore (ItemNo, StoreNo, Enabled, Counter, CreatedDate, Pkey)
            VALUES (@ItemNo, 'ALL', 1, @cntStore + 1, @now, @ItemNo);
        END
        ELSE
        BEGIN
            INSERT INTO dbo.CpnVchBOMStore (ItemNo, StoreNo, Enabled, Counter, CreatedDate, Pkey)
            SELECT @ItemNo, sg.StoreNo, 1, @cntStore + 1, @now, @ItemNo
            FROM   dbo.StoreGroup sg (NOLOCK)
            WHERE  sg.GroupCode = @StoreGroupCode;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ── Cài đặt nâng cao (advanced) ─────────────────────────────────────────── */
IF OBJECT_ID(N'dbo.usp_SetupCoupon_SaveAdvanced', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupCoupon_SaveAdvanced;
GO

CREATE PROCEDURE dbo.usp_SetupCoupon_SaveAdvanced
(
    @ItemNo          nvarchar(20),         -- rỗng = tạo mới
    @Description     nvarchar(300),
    @ArticleType     nvarchar(10),
    @UOM             nvarchar(10),
    @IssueType       varchar(20),
    @CpnVchType      varchar(10),
    @DiscountType    int,
    @DiscountValue   decimal(38,20),
    @MaxAmount       decimal(38,20),
    @StartingDate    datetime,
    @EndingDate      datetime,
    @Blocked         bit,
    @IsMultiUse      bit,
    @IsCheckAPI      bit,
    @LimitQty        int,
    @LimitQtyUsed    int,
    @SaleType        varchar(50),
    @StoreGroupCode  varchar(50),
    @OutItemNo       nvarchar(20) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now datetime = GETDATE();
    DECLARE @blk tinyint = CASE WHEN @Blocked = 1 THEN 1 ELSE 0 END;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @isNew bit = CASE WHEN @ItemNo IS NULL OR LTRIM(RTRIM(@ItemNo)) = '' THEN 1 ELSE 0 END;

        IF @isNew = 1
        BEGIN
            DECLARE @maxNum bigint =
                (SELECT MAX(TRY_CONVERT(bigint, SUBSTRING(ItemNo, 2, LEN(ItemNo))))
                 FROM dbo.CpnVchBOMHeader (NOLOCK) WHERE LEFT(ItemNo, 1) = 'C');
            SET @ItemNo = 'C' + CONVERT(varchar(20), CASE WHEN @maxNum IS NULL THEN 70000001 ELSE @maxNum + 1 END);
        END

        SET @OutItemNo = @ItemNo;

        DECLARE @cntHeader bigint = (SELECT ISNULL(MAX(Counter), 0) FROM dbo.CpnVchBOMHeader);

        -- IssueRule: đảm bảo tồn tại (advanced có thể chạy trước issue)
        IF EXISTS (SELECT 1 FROM dbo.CpnVchBOMIssueRule WHERE ItemNo = @ItemNo)
            UPDATE dbo.CpnVchBOMIssueRule SET IssueType = @IssueType, CreatedDate = @now WHERE ItemNo = @ItemNo;
        ELSE
            INSERT INTO dbo.CpnVchBOMIssueRule (ItemNo, IssueType, Counter, CreatedDate, Pkey)
            VALUES (@ItemNo, @IssueType, 0, @now, @ItemNo);

        -- Header: upsert field nâng cao
        IF EXISTS (SELECT 1 FROM dbo.CpnVchBOMHeader WHERE ItemNo = @ItemNo)
        BEGIN
            UPDATE dbo.CpnVchBOMHeader
            SET    ItemName = @Description, UnitOfMeasure = @UOM, DiscountType = @DiscountType,
                   DiscountValue = @DiscountValue, ValueOfVoucher = @DiscountValue, MaxAmount = @MaxAmount,
                   StartingDate = @StartingDate, EndingDate = @EndingDate, LastDateModified = @now,
                   Blocked = @blk, LimitQty = @LimitQty, IsCheckAPI = @IsCheckAPI, IsMultiUse = @IsMultiUse,
                   LimitQtyUsed = @LimitQtyUsed, SaleType = @SaleType, StoreGroupCode = @StoreGroupCode,
                   ArticleType = @ArticleType, CpnVchType = @CpnVchType, Counter = @cntHeader + 1
            WHERE  ItemNo = @ItemNo;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.CpnVchBOMHeader
                (ItemNo, ItemName, UnitOfMeasure, DiscountType, DiscountValue, ValueOfVoucher,
                 StartingDate, EndingDate, MaxAmount, LastDateModified, Blocked, CouponCode,
                 LimitQty, IsCheckAPI, IsMultiUse, LimitQtyUsed, SaleType, StoreGroupCode, Pkey,
                 ArticleType, CpnVchType, IssueType, IsCheckItem, Counter)
            VALUES
                (@ItemNo, @Description, @UOM, @DiscountType, @DiscountValue, @DiscountValue,
                 @StartingDate, @EndingDate, @MaxAmount, @now, @blk, '',
                 @LimitQty, @IsCheckAPI, @IsMultiUse, @LimitQtyUsed, @SaleType, @StoreGroupCode, @ItemNo,
                 @ArticleType, @CpnVchType, @IssueType, 0, @cntHeader + 1);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
