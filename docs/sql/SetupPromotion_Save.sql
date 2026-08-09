/* ============================================================================
   11.1 Cài đặt CTKM — SP lưu draft (header + Buy/Get/Site) — RPOSMasterData
   Chiến lược: REPLACE-ON-SAVE (xóa hết Buy/Get/Site theo BBYNR rồi chèn lại
   từ TVP) → 1 SP duy nhất cho toàn bộ thao tác Lưu, transaction an toàn.
   Cột bám đúng entity legacy SetupPromotionHEADER/BUY/GET/SITE.
   ----------------------------------------------------------------------------
   CHẠY 1 LẦN trên RPOSMasterData. Nếu TYPE đã tồn tại, drop SP trước rồi TYPE.
   ----------------------------------------------------------------------------
   BẢN SỬA LẦN 2 (2026-07-05) — thêm tham số/cột trước đây hard-code rỗng hoặc
   bỏ qua hoàn toàn, để khớp 100% field legacy SetupMain: khung giờ áp dụng
   (TIMEFROM/TIMETO) + ngày trong tuần (MON..SUN), giá trị tổng tiền tối thiểu
   (MINVALUE), giảm giá tổng bill (TOTALDISCOUNT/TOTALDISCOUNTTYPE/
   TOTALDISCOUNTVALUE), voucher delay (ZVCDAY_AFTER/ZVCTIME_AFTER). Toàn bộ cột
   đã tồn tại sẵn trong SetupPromotionHEADER — không ALTER TABLE, chỉ sửa SP.
   ----------------------------------------------------------------------------
   BẢN SỬA LẦN 3 (2026-07-10) — đổi lỗi "đã DUYỆT, không được lưu tạm" từ
   RAISERROR (number mặc định 50000 cho mọi ad-hoc message) sang THROW 51001
   (dải error number >=50000, không cần đăng ký sys.messages). Mục đích: tầng
   C# (PromotionRepository) phân biệt được lỗi NGHIỆP VỤ có chủ đích (an toàn
   hiện nguyên message cho end-user) với lỗi KỸ THUẬT khác (mất kết nối, SP
   chưa deploy, timeout...) — trước đây cả 2 loại đều hiện y nguyên ex.Message
   thô ra Snackbar. Quy ước dải lỗi CTKM: 51001 = đã duyệt không được lưu tạm
   (file này), 51002 = không tìm thấy CTKM khi Duyệt (SetupPromotion_
   ApproveAndStatus.sql).
   ----------------------------------------------------------------------------
   BẢN SỬA LẦN 4 (2026-07-10) — thêm tham số @NumOfDaysList (optional, default
   '' để backward-compat) ghi vào cột MỚI NUMOFDAYSLIST (JSON array nhiều ngày
   trong tháng — xem docs/sql/SetupPromotion_AddNumOfDaysList.sql, BẮT BUỘC chạy
   TRƯỚC script này). @NumOfDays/NUMOFDAYS cũ giữ NGUYÊN 100% — KHÔNG đổi gì —
   vì SP legacy Setup_Promotion_Insert (đang chạy production, không thuộc repo
   này) Convert(int, NUMOFDAYS) thẳng khi Duyệt/publish, đổi kiểu sẽ crash.
   ----------------------------------------------------------------------------
   BẢN SỬA LẦN 5 (2026-07-15) — khớp cơ chế publish Setup_Promotion_Insert
   (docs/web/offers/Setup_Promotion_Insert.sql). 3 thay đổi:
   (a) TVP Buy thêm DiscountType/DiscountValue → ghi SetupPromotionBUY.DiscountType/
       DiscountValue (ZB02 combo: giá bán; ZB07: ngưỡng bill). Publish đọc 2 cột này.
       → drop+recreate TYPE Buy (không còn IF TYPE_ID IS NULL) vì phải thêm cột.
   (b) @TotalMinValue (0/1) ghi cột TOTALMINVALUE — C# gán = IsTotalBill của
       OfferType đang chọn. Publish rẽ nhánh Get→OfferBenefits khi TOTALMINVALUE=1
       (StepAmount = MINVALUE). Trước đây KHÔNG hề set cột này → OfferBenefits
       KHÔNG BAO GIỜ sinh cho ZB06/ZB12/ZB13/ZB14/ZB15 (lỗi gốc).
   (c) @MaxQuantity ghi cột MaxQuantity (script SetupPromotion_AddMaxQuantity.sql,
       order 95, BẮT BUỘC chạy TRƯỚC). Fix TOTALDISCOUNTTYPE: lưu ký hiệu '%'/'R'/'P'
       (thay vì int '0'/'1'/'2') để publish IIF('%'→0,'P'→2,else 1) trả đúng.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── Save proc — drop TRƯỚC để có thể drop+recreate TVP Buy (thêm cột) ─────── */
IF OBJECT_ID(N'dbo.usp_SaveSetupCTKMAll', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SaveSetupCTKMAll;
GO

/* ── Table-Valued Types ─────────────────────────────────────────────────── */
-- Buy TVP: drop+recreate vì (b5) thêm DiscountType/DiscountValue — TYPE không ALTER
-- được, phải drop; an toàn vì chỉ usp_SaveSetupCTKMAll (đã drop ở trên) tham chiếu.
IF TYPE_ID(N'dbo.SetupPromotionBuyTVP') IS NOT NULL
    DROP TYPE dbo.SetupPromotionBuyTVP;
GO
CREATE TYPE dbo.SetupPromotionBuyTVP AS TABLE
(
    LineType      int            NULL,   -- 0 = Item, 1 = Group
    ItemNo        nvarchar(50)   NULL,
    GroupCode     nvarchar(50)   NULL,
    Quantity      nvarchar(50)   NULL,
    Uom           nvarchar(50)   NULL,
    ScaleType     nvarchar(10)   NULL,
    DiscountType  int            NULL,   -- 0 = %, 1 = R (amount), 2 = P (price) — ZB02 combo=2
    DiscountValue nvarchar(50)   NULL    -- ZB02 = giá combo; ZB07 = ngưỡng bill
);
GO

IF TYPE_ID(N'dbo.SetupPromotionGetTVP') IS NULL
CREATE TYPE dbo.SetupPromotionGetTVP AS TABLE
(
    LineType      int            NULL,
    ItemNo        nvarchar(50)   NULL,
    GroupCode     nvarchar(50)   NULL,
    Quantity      nvarchar(50)   NULL,
    Uom           nvarchar(50)   NULL,
    ScaleType     nvarchar(10)   NULL,
    DiscountType  int            NULL,   -- 0 = %, 1 = R (amount), 2 = P (price)
    DiscountValue nvarchar(50)   NULL
);
GO

IF TYPE_ID(N'dbo.SetupPromotionSiteTVP') IS NULL
CREATE TYPE dbo.SetupPromotionSiteTVP AS TABLE
(
    SiteGroupCode nvarchar(50)  NULL,
    SiteCode      nvarchar(50)  NULL
);
GO

CREATE PROCEDURE dbo.usp_SaveSetupCTKMAll
(
    @BBYNR        nvarchar(20)  OUTPUT,   -- rỗng = tạo mới (auto-gen); có = update
    @SalesType    nvarchar(50),
    @Description  nvarchar(250),
    @OfferType    nvarchar(50),
    @Status       nvarchar(10),
    @ValidFrom    nvarchar(8),            -- yyyyMMdd
    @ValidTo      nvarchar(8),            -- yyyyMMdd
    @IsVoucher    bit,
    @BuyLinkCat   nvarchar(1)   = 'A',    -- 'A' = AND, 'O' = OR
    @GetLinkCat   nvarchar(1)   = 'A',
    -- ── Advanced (Phase 2) — đều có default để an toàn backward-compat ──
    @LimitQty           nvarchar(50) = '0',
    @MemberOnly         bit          = 0,
    @MemberCode         nvarchar(50) = '',
    @Priority           nvarchar(10) = '1',
    @NumOfDays          nvarchar(10) = '0',
    @NumOfDaysList      nvarchar(200) = '',  -- JSON array nhiều ngày trong tháng (cột mới NUMOFDAYSLIST) — TÁCH BIỆT @NumOfDays, không publish sang OfferHeader
    @VoucherFrom        nvarchar(8)  = '',   -- yyyyMMdd ('' = không có voucher date → dùng ngày CTKM)
    @VoucherTo          nvarchar(8)  = '',
    @VoucherValidDay    nvarchar(10) = '0',
    @VoucherLimitNumber nvarchar(10) = '0',
    @AllowUseAfterDay   nvarchar(10) = '0',   -- ZVCDAY_AFTER — dùng sau N ngày kể từ lúc ra bill
    @AllowUseAfterTime  nvarchar(10) = '',    -- ZVCTIME_AFTER
    -- ── Lịch áp dụng theo giờ/ngày trong tuần ──
    @FromTime nvarchar(10) = '', @ToTime nvarchar(10) = '',
    @Mon nvarchar(5) = '', @Tue nvarchar(5) = '', @Wed nvarchar(5) = '', @Thu nvarchar(5) = '',
    @Fri nvarchar(5) = '', @Sat nvarchar(5) = '', @Sun nvarchar(5) = '',
    -- ── Ngưỡng tổng bill (MINVALUE) + cờ tổng bill (TOTALMINVALUE) + giới hạn SL KM ──
    @MinValue nvarchar(50) = '0',
    @TotalMinValue int = 0,   -- (b5) 0/1 — =1 khi OfferType.IsTotalBill → publish rẽ Get→OfferBenefits
    @MaxQuantity   int = 0,   -- (b5) giới hạn SL KM tối đa (SetupPromotionHEADER.MaxQuantity)
    -- ── Giảm giá tổng bill (whole-bill, cơ chế riêng ZB21/ZB09) ──
    @CheckTotalDiscount nvarchar(5) = '', @TotalDiscountType nvarchar(10) = '0',
    @TotalDiscountValue nvarchar(50) = '0',
    @Buy          dbo.SetupPromotionBuyTVP  READONLY,
    @Get          dbo.SetupPromotionGetTVP  READONLY,
    @Site         dbo.SetupPromotionSiteTVP READONLY
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now datetime = GETDATE();
    -- ZVCDATE_ST/EN dùng kép: có voucher date → ghi đè, không thì = ngày hiệu lực CTKM
    DECLARE @VcSt nvarchar(8) = CASE WHEN LTRIM(RTRIM(@VoucherFrom)) <> '' THEN @VoucherFrom ELSE @ValidFrom END;
    DECLARE @VcEn nvarchar(8) = CASE WHEN LTRIM(RTRIM(@VoucherTo))   <> '' THEN @VoucherTo   ELSE @ValidTo   END;
    DECLARE @Vinid nvarchar(10) = CASE WHEN @MemberOnly = 1 THEN 'X' ELSE '' END;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1) Auto-gen BBYNR khi tạo mới (bắt đầu 6000000001), chống trùng bằng lock UPDLOCK
        IF (@BBYNR IS NULL OR LTRIM(RTRIM(@BBYNR)) = '')
        BEGIN
            DECLARE @maxNo bigint =
                (SELECT MAX(TRY_CONVERT(bigint, BBYNR))
                 FROM dbo.SetupPromotionHEADER WITH (UPDLOCK, HOLDLOCK));
            SET @BBYNR = CONVERT(nvarchar(20), ISNULL(@maxNo, 6000000000) + 1);
        END

        -- 2) Không cho sửa CTKM đã DUYỆT
        -- Dùng THROW với error number cố định 51001 (dải >=50000, KHÔNG cần đăng ký
        -- sys.messages) thay vì RAISERROR mặc định (number=50000 cho mọi ad-hoc message)
        -- — để tầng C# (PromotionRepository) phân biệt được lỗi NGHIỆP VỤ có chủ đích
        -- (an toàn hiện y nguyên message) với lỗi KỸ THUẬT khác (kết nối/timeout...).
        IF EXISTS (SELECT 1 FROM dbo.SetupPromotionHEADER
                   WHERE BBYNR = @BBYNR AND IsApprove = 1)
        BEGIN
            DECLARE @alreadyApprovedMsg nvarchar(200) =
                FORMATMESSAGE(N'CTKM %s đã được DUYỆT, không được phép lưu tạm', @BBYNR);
            ROLLBACK TRANSACTION;
            THROW 51001, @alreadyApprovedMsg, 1;
        END

        -- 3) Upsert HEADER
        IF EXISTS (SELECT 1 FROM dbo.SetupPromotionHEADER WHERE BBYNR = @BBYNR)
        BEGIN
            UPDATE dbo.SetupPromotionHEADER
            SET    SalesType     = @SalesType,
                   BBYTEXT       = @Description,
                   BBYTYPE       = @OfferType,
                   STATUS        = @Status,
                   VALIDFROM     = @ValidFrom,
                   VALIDTO       = @ValidTo,
                   PROMOTIONTEXT = @Description,
                   IsVoucher     = @IsVoucher,
                   BUYLINKCAT    = @BuyLinkCat,
                   GETLINKCAT    = @GetLinkCat,
                   -- Advanced
                   VINID         = @Vinid,
                   LIMIT         = @LimitQty,
                   MemberCode    = @MemberCode,
                   ZPRIOR        = @Priority,
                   NUMOFDAYS     = @NumOfDays,
                   NUMOFDAYSLIST = @NumOfDaysList,
                   ZVCDATE_ST    = @VcSt,
                   ZVCDATE_EN    = @VcEn,
                   ZVCDATE_VA    = @VoucherValidDay,
                   LIMITNR       = @VoucherLimitNumber,
                   ZVCDAY_AFTER  = @AllowUseAfterDay,
                   ZVCTIME_AFTER = @AllowUseAfterTime,
                   TIMEFROM      = @FromTime,
                   TIMETO        = @ToTime,
                   MON           = @Mon,
                   TUE           = @Tue,
                   WED           = @Wed,
                   THUR          = @Thu,
                   FRI           = @Fri,
                   SAT           = @Sat,
                   SUN           = @Sun,
                   MINVALUE      = @MinValue,
                   TOTALMINVALUE = CASE WHEN @TotalMinValue = 1 THEN '1' ELSE '0' END,
                   MaxQuantity   = @MaxQuantity,
                   TOTALDISCOUNT      = @CheckTotalDiscount,
                   -- Lưu ký hiệu '%'/'R'/'P' (KHÔNG lưu int) — publish IIF('%'→0,'P'→2,else 1)
                   TOTALDISCOUNTTYPE  = CASE @TotalDiscountType WHEN '0' THEN '%' WHEN '2' THEN 'P' ELSE 'R' END,
                   TOTALDISCOUNTVALUE = @TotalDiscountValue
            WHERE  BBYNR = @BBYNR;
        END
        ELSE
        BEGIN
            -- Cột ID trên bảng legacy có thể là IDENTITY hoặc không tùy môi trường (DEV/UAT/PROD) —
            -- kiểm tra động qua sys.columns thay vì giả định cứng, tránh lỗi "Cannot insert NULL into ID".
            DECLARE @idIsIdentity bit = ISNULL(
                (SELECT is_identity FROM sys.columns
                 WHERE object_id = OBJECT_ID(N'dbo.SetupPromotionHEADER') AND name = N'ID'), 0);

            IF (@idIsIdentity = 1)
            BEGIN
                INSERT INTO dbo.SetupPromotionHEADER
                    (BBYNR, SalesType, BBYTEXT, BBYTYPE, STATUS, VALIDFROM, VALIDTO,
                     ZVCDATE_ST, ZVCDATE_EN, TIMEFROM, TIMETO, MON, TUE, WED, THUR, FRI, SAT, SUN,
                     PROMOTION, PROMOTIONTEXT, IsVoucher, IsApprove, BUYLINKCAT, GETLINKCAT, CREATEDDATE,
                     VINID, LIMIT, MemberCode, ZPRIOR, NUMOFDAYS, NUMOFDAYSLIST, ZVCDATE_VA, LIMITNR,
                     ZVCDAY_AFTER, ZVCTIME_AFTER, MINVALUE, TOTALMINVALUE, MaxQuantity, TOTALDISCOUNT, TOTALDISCOUNTTYPE, TOTALDISCOUNTVALUE)
                VALUES
                    (@BBYNR, @SalesType, @Description, @OfferType, @Status, @ValidFrom, @ValidTo,
                     @VcSt, @VcEn, @FromTime, @ToTime, @Mon, @Tue, @Wed, @Thu, @Fri, @Sat, @Sun,
                     @BBYNR, @Description, @IsVoucher, 0, @BuyLinkCat, @GetLinkCat, @now,
                     @Vinid, @LimitQty, @MemberCode, @Priority, @NumOfDays, @NumOfDaysList, @VoucherValidDay, @VoucherLimitNumber,
                     @AllowUseAfterDay, @AllowUseAfterTime, @MinValue,
                     CASE WHEN @TotalMinValue = 1 THEN '1' ELSE '0' END, @MaxQuantity, @CheckTotalDiscount,
                     CASE @TotalDiscountType WHEN '0' THEN '%' WHEN '2' THEN 'P' ELSE 'R' END, @TotalDiscountValue);
            END
            ELSE
            BEGIN
                DECLARE @newId int =
                    (SELECT ISNULL(MAX(ID), 0) + 1 FROM dbo.SetupPromotionHEADER WITH (UPDLOCK, HOLDLOCK));

                INSERT INTO dbo.SetupPromotionHEADER
                    (ID, BBYNR, SalesType, BBYTEXT, BBYTYPE, STATUS, VALIDFROM, VALIDTO,
                     ZVCDATE_ST, ZVCDATE_EN, TIMEFROM, TIMETO, MON, TUE, WED, THUR, FRI, SAT, SUN,
                     PROMOTION, PROMOTIONTEXT, IsVoucher, IsApprove, BUYLINKCAT, GETLINKCAT, CREATEDDATE,
                     VINID, LIMIT, MemberCode, ZPRIOR, NUMOFDAYS, NUMOFDAYSLIST, ZVCDATE_VA, LIMITNR,
                     ZVCDAY_AFTER, ZVCTIME_AFTER, MINVALUE, TOTALMINVALUE, MaxQuantity, TOTALDISCOUNT, TOTALDISCOUNTTYPE, TOTALDISCOUNTVALUE)
                VALUES
                    (@newId, @BBYNR, @SalesType, @Description, @OfferType, @Status, @ValidFrom, @ValidTo,
                     @VcSt, @VcEn, @FromTime, @ToTime, @Mon, @Tue, @Wed, @Thu, @Fri, @Sat, @Sun,
                     @BBYNR, @Description, @IsVoucher, 0, @BuyLinkCat, @GetLinkCat, @now,
                     @Vinid, @LimitQty, @MemberCode, @Priority, @NumOfDays, @NumOfDaysList, @VoucherValidDay, @VoucherLimitNumber,
                     @AllowUseAfterDay, @AllowUseAfterTime, @MinValue,
                     CASE WHEN @TotalMinValue = 1 THEN '1' ELSE '0' END, @MaxQuantity, @CheckTotalDiscount,
                     CASE @TotalDiscountType WHEN '0' THEN '%' WHEN '2' THEN 'P' ELSE 'R' END, @TotalDiscountValue);
            END
        END

        -- 4) BUY: replace (b5: thêm DiscountType/DiscountValue — publish đọc 2 cột này)
        DELETE FROM dbo.SetupPromotionBUY WHERE BBYNR = @BBYNR;
        INSERT INTO dbo.SetupPromotionBUY
            (BBYNR, CREATEDDATE, LINEINDICATOR, BUYTYPE, MAT_NR, MATGROUP, MAT_QUAN, MEINH, ScaleType,
             DiscountType, DiscountValue)
        SELECT @BBYNR, @now, '2',
               CASE WHEN b.LineType = 1 THEN 'MGP' ELSE 'MAT' END,
               CASE WHEN b.LineType = 1 THEN '' ELSE ISNULL(b.ItemNo, '') END,
               CASE WHEN b.LineType = 1 THEN ISNULL(b.GroupCode, '') ELSE '' END,
               ISNULL(b.Quantity, ''), ISNULL(b.Uom, ''), ISNULL(b.ScaleType, 'C'),
               ISNULL(b.DiscountType, 0), ISNULL(b.DiscountValue, '0')
        FROM @Buy b;

        -- 5) GET: replace (tính DISTYPE/BBYVAL/BBYPER từ DiscountType/Value)
        DELETE FROM dbo.SetupPromotionGET WHERE BBYNR = @BBYNR;
        INSERT INTO dbo.SetupPromotionGET
            (BBYNR, CREATEDDATE, LINEINDICATOR, GETTYPE, MATERIALCODE, MATGROUP, QTY, MEINH,
             DISTYPE, BBYVAL, BBYPER, SCALETYPE)
        SELECT @BBYNR, @now, '3',
               CASE WHEN g.LineType = 1 THEN 'MGP' ELSE 'MAT' END,
               CASE WHEN g.LineType = 1 THEN '' ELSE ISNULL(g.ItemNo, '') END,
               CASE WHEN g.LineType = 1 THEN ISNULL(g.GroupCode, '') ELSE '' END,
               ISNULL(g.Quantity, ''), ISNULL(g.Uom, ''),
               CASE g.DiscountType WHEN 0 THEN '%' WHEN 1 THEN 'R' ELSE 'P' END,
               CASE WHEN g.DiscountType = 0 THEN '0' ELSE ISNULL(g.DiscountValue, '0') END,
               CASE WHEN g.DiscountType = 0 THEN ISNULL(g.DiscountValue, '0') ELSE '0' END,
               ISNULL(g.ScaleType, 'C')
        FROM @Get g;

        -- 6) SITE: replace (repository đã expand group → các SiteCode)
        DELETE FROM dbo.SetupPromotionSITE WHERE BBYNR = @BBYNR;
        INSERT INTO dbo.SetupPromotionSITE (BBYNR, SITEGROUPCODE, SITECODE, CREATEDDATE)
        SELECT @BBYNR, s.SiteGroupCode, s.SiteCode, @now
        FROM @Site s;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
