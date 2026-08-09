/* ============================================================================
   CHẨN ĐOÁN — Vì sao Duyệt/EXEC Setup_Promotion_Insert '6000000047' KHÔNG ra dữ liệu Offer*
   ----------------------------------------------------------------------------
   ⚠️  READ-ONLY (chỉ SELECT). KHÔNG đăng ký manifest. Chạy tay trên RPOSMasterData.

   Sự thật đã xác nhận khi ĐỌC Setup_Promotion_Insert.sql:
   - SP có BEGIN CATCH chỉ ROLLBACK TRANSACTION, KHÔNG THROW, KHÔNG ghi Interface_Errors
     (dòng 265-270) → mọi lỗi runtime bị NUỐT im lặng, cả transaction rollback → 0 dòng ở
     TẤT CẢ Offer*, SP trả về bình thường (không exception). `SELECT * FROM Interface_Errors`
     cuối SP (dòng 272-274) LUÔN rỗng vì SP không bao giờ ghi vào bảng này → "không ra kết quả gì"
     là bình thường, KHÔNG có nghĩa chạy OK.
   - INSERT OfferHeader (dòng 60-88) KHÔNG phụ thuộc site (SELECT FROM SetupPromotionHEADER
     WHERE BBYNR=@BBY). Vậy nếu OfferHeader cũng rỗng → chắc chắn có LỖI runtime bị nuốt (không
     phải chỉ "thiếu site"). => cần tìm statement CONVERT/INSERT nào ném lỗi cho record này.

   Mục đích script: (A) đếm số dòng thực tế; (B) probe TRY_CONVERT mọi cột header rủi ro convert;
   (C) dump Get/Buy/Site để soi DISVAL/BBYVAL/QTY/DISTYPE (nhánh total-bill ZB06 đẩy Get→OfferBenefits).
   ============================================================================ */
USE [RPOSMasterData];
GO
DECLARE @BBY varchar(20) = '6000000047';   -- đổi nếu cần record ZB06 khác

/* ── (A) Đếm dòng: draft vs Offer* ─────────────────────────────────────────── */
SELECT 'draft.HEADER' t, COUNT(*) n FROM dbo.SetupPromotionHEADER WHERE BBYNR=@BBY
UNION ALL SELECT 'draft.BUY',  COUNT(*) FROM dbo.SetupPromotionBUY  WHERE BBYNR=@BBY
UNION ALL SELECT 'draft.GET',  COUNT(*) FROM dbo.SetupPromotionGET  WHERE BBYNR=@BBY
UNION ALL SELECT 'draft.SITE', COUNT(*) FROM dbo.SetupPromotionSITE WHERE BBYNR=@BBY
UNION ALL SELECT 'Offer.Header',   COUNT(*) FROM dbo.OfferHeader   WHERE [No]=@BBY
UNION ALL SELECT 'Offer.Buy',      COUNT(*) FROM dbo.OfferBuy      WHERE OfferNo=@BBY
UNION ALL SELECT 'Offer.Get',      COUNT(*) FROM dbo.OfferGet      WHERE OfferNo=@BBY
UNION ALL SELECT 'Offer.Benefits', COUNT(*) FROM dbo.OfferBenefits WHERE OfferNo=@BBY
UNION ALL SELECT 'Offer.Site',     COUNT(*) FROM dbo.OfferSite     WHERE OfferNo=@BBY;

/* ── (B) Probe convert HEADER — cột nào *_ok = NULL trong khi giá trị nguồn KHÔNG rỗng
       => chính là convert ném lỗi trong SP (SP dùng CONVERT/CAST cứng, không TRY_) ── */
SELECT
    BBYNR,
    [STATUS],            TRY_CONVERT(int,   [STATUS])                                         AS status_ok,
    NUMOFDAYS,           CASE WHEN ISNULL(NUMOFDAYS,'')<>'' THEN TRY_CONVERT(int, NUMOFDAYS) END AS numofdays_ok,
    NUMOFDAYSLIST,
    MINVALUE,            TRY_CONVERT(float, ISNULL(MINVALUE,'0'))                              AS minvalue_ok,
    TOTALMINVALUE,
    TOTALDISCOUNTVALUE,  TRY_CONVERT(float, ISNULL(TOTALDISCOUNTVALUE,'0'))                    AS totaldiscval_ok,
    ZPRIOR,              TRY_CONVERT(int, IIF(ISNULL(ZPRIOR,'')='','1',ZPRIOR))                AS zprior_ok,
    LIMITNR,             TRY_CONVERT(int, IIF(ISNULL(LIMITNR,'')='','0',LIMITNR))              AS limitnr_ok,
    ZVCDATE_VA,          TRY_CONVERT(int, LTRIM(ISNULL(ZVCDATE_VA,'0')))                       AS zvcdateva_ok,
    [LIMIT],             TRY_CONVERT(int, IIF([LIMIT]='' OR [LIMIT] IS NULL, 9999, [LIMIT]))   AS limitqty_ok
FROM dbo.SetupPromotionHEADER WHERE BBYNR=@BBY;

/* ── (C) Dump Get/Buy/Site — soi DISVAL/BBYVAL/BBYPER/QTY/DISTYPE cho nhánh OfferBenefits
       (total-bill ZB06). Cột [Value]/[StepAmount] của OfferBenefits là float — nếu DISVAL
       (=BBYVAL khi DISTYPE<>'%') hoặc QTY rỗng/không-số => convert float ném lỗi, nuốt im lặng. ── */
SELECT * FROM dbo.SetupPromotionGET  WHERE BBYNR=@BBY;
SELECT * FROM dbo.SetupPromotionBUY  WHERE BBYNR=@BBY;
SELECT * FROM dbo.SetupPromotionSITE WHERE BBYNR=@BBY;

/* ── (D) Kiểu cột đích nghi ngờ (OfferBenefits.[Value]/[StepAmount]/[Quantity]) ── */
SELECT c.name AS column_name, t.name AS type_name, c.is_nullable
FROM sys.columns c JOIN sys.types t ON c.user_type_id=t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.OfferBenefits')
ORDER BY c.column_id;
GO
