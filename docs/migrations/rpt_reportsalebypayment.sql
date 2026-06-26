-- ============================================================================
-- Báo cáo "Doanh thu theo Hình thức thanh toán" (POS.Web → /store/payment-breakdown)
-- DB: CentralSales
-- Nguồn: TransPaymentEntry ⋈ TransHeader ON OrderNo (bảng giao dịch realtime)
--   - Dimension: th.OrderDate (lọc ngày), th.StoreNo (lọc store) — cấp đơn, canonical
--   - Net: trừ hàng trả qua cờ th.SalesIsReturn (amounts lưu dương, không lưu âm)
--   - Payment: pe.TenderType→PaymentCode, pe.TenderTypeName→PaymentName, pe.AmountTendered
-- Trả 3 result set: RS1 KPI · RS2 theo HTTT (Pareto) · RS3 xu hướng theo ngày × HTTT
--
-- ⚠️ CASH detection: literal 'CASH' ở RS1 (Cash/Non-cash). Nếu DB dùng mã tiền mặt
--    khác (vd '01'), sửa literal này + map màu BarColor trong page tương ứng.
-- ============================================================================
-- ── Index hỗ trợ: lọc TransHeader theo OrderDate (range scan thay vì full scan) ──
-- TransHeader filter chính theo OrderDate; JOIN sang TransPaymentEntry đã được PK
-- (OrderNo, LineNo) cover. INCLUDE StoreNo + SalesIsReturn để query không cần key lookup.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_TransHeader_OrderDate'
      AND object_id = OBJECT_ID('dbo.TransHeader'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_TransHeader_OrderDate
        ON dbo.TransHeader (OrderDate)
        INCLUDE (StoreNo, SalesIsReturn);
END
GO

CREATE OR ALTER PROCEDURE [dbo].[Rpt_ReportSaleByPayment]
    @FromDate DATETIME      = NULL,
    @ToDate   DATETIME      = NULL,
    @StoreNo  NVARCHAR(10)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromDate IS NULL SET @FromDate = CAST(GETDATE() AS DATE);
    IF @ToDate   IS NULL SET @ToDate   = CAST(GETDATE() AS DATE);
    SET @ToDate = DATEADD(SECOND, 86399, CAST(CAST(@ToDate AS DATE) AS DATETIME));

    -- ── Base: payment lines + header (date/store/return flag). Net hóa trả hàng. ──
    SELECT
        pe.OrderNo,
        th.StoreNo,
        CAST(th.OrderDate AS DATE)                 AS TxDate,
        pe.TenderType                              AS PaymentCode,
        ISNULL(pe.TenderTypeName, pe.TenderType)   AS PaymentName,
        CASE WHEN th.SalesIsReturn = 0
             THEN  ISNULL(pe.AmountTendered, 0)
             ELSE -ISNULL(pe.AmountTendered, 0)
        END                                        AS NetAmount
    INTO #pb
    FROM dbo.TransPaymentEntry pe WITH (NOLOCK)
    INNER JOIN dbo.TransHeader th WITH (NOLOCK) ON th.OrderNo = pe.OrderNo
    WHERE th.OrderDate BETWEEN @FromDate AND @ToDate
      AND (@StoreNo IS NULL OR th.StoreNo = @StoreNo)
    -- Mệnh đề "(@StoreNo IS NULL OR ...)" là kiểu catch-all → SQL dùng 1 plan chung
    -- cho cả NULL (tất cả store) lẫn store cụ thể, thường ra full-scan TransHeader.
    -- RECOMPILE: biên dịch plan riêng theo từng giá trị tham số mỗi lần chạy →
    -- range scan theo OrderDate khi NULL, seek theo store khi có store cụ thể.
    OPTION (RECOMPILE);

    DECLARE @TotalPayment FLOAT = (SELECT ISNULL(SUM(NetAmount), 0) FROM #pb);
    DECLARE @TotalOrders  INT   = (SELECT COUNT(DISTINCT OrderNo)   FROM #pb);

    -- ══════════════════════════════════════════════════════════════════════════
    -- RESULT SET 1: KPI CARDS
    -- ══════════════════════════════════════════════════════════════════════════
    SELECT
        @TotalPayment                                                              AS TotalPaymentAmount,
        @TotalOrders                                                               AS TotalOrders,
        (SELECT COUNT(DISTINCT PaymentCode) FROM #pb)                              AS TotalPaymentMethods,
        SUM(CASE WHEN PaymentCode = 'CASH' THEN NetAmount ELSE 0 END)              AS CashAmount,
        CASE WHEN @TotalPayment <> 0
             THEN CAST(SUM(CASE WHEN PaymentCode = 'CASH' THEN NetAmount ELSE 0 END)
                       * 100.0 / @TotalPayment AS DECIMAL(7,2))
             ELSE 0 END                                                            AS CashPct,
        @TotalPayment - SUM(CASE WHEN PaymentCode = 'CASH' THEN NetAmount ELSE 0 END) AS NonCashAmount,
        CASE WHEN @TotalPayment <> 0
             THEN CAST((@TotalPayment - SUM(CASE WHEN PaymentCode = 'CASH' THEN NetAmount ELSE 0 END))
                       * 100.0 / @TotalPayment AS DECIMAL(7,2))
             ELSE 0 END                                                            AS NonCashPct
    FROM #pb;

    -- ══════════════════════════════════════════════════════════════════════════
    -- RESULT SET 2: THEO TỪNG HÌNH THỨC THANH TOÁN (Pareto bar list + bảng)
    -- ══════════════════════════════════════════════════════════════════════════
    SELECT
        PaymentCode,
        MAX(PaymentName)                                                           AS PaymentName,
        SUM(NetAmount)                                                             AS PaymentAmount,
        COUNT(DISTINCT OrderNo)                                                    AS OrderCount,
        CASE WHEN COUNT(DISTINCT OrderNo) > 0
             THEN SUM(NetAmount) / COUNT(DISTINCT OrderNo) ELSE 0 END              AS AvgPerOrder,
        CASE WHEN @TotalPayment <> 0
             THEN CAST(SUM(NetAmount) * 100.0 / @TotalPayment AS DECIMAL(7,2))
             ELSE 0 END                                                            AS SharePct,
        CAST(SUM(SUM(NetAmount)) OVER (ORDER BY SUM(NetAmount) DESC
                ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
             * 100.0 / NULLIF(@TotalPayment, 0) AS DECIMAL(7,2))                  AS CumulativePct,
        RANK() OVER (ORDER BY SUM(NetAmount) DESC)                                 AS [Rank]
    FROM #pb
    GROUP BY PaymentCode
    ORDER BY PaymentAmount DESC;

    -- ══════════════════════════════════════════════════════════════════════════
    -- RESULT SET 3: XU HƯỚNG THEO NGÀY × HTTT (Line chart)
    -- ══════════════════════════════════════════════════════════════════════════
    SELECT
        TxDate,
        CONVERT(NVARCHAR(10), TxDate, 103)                                         AS DateLabel,
        PaymentCode,
        MAX(PaymentName)                                                           AS PaymentName,
        SUM(NetAmount)                                                             AS PaymentAmount,
        COUNT(DISTINCT OrderNo)                                                    AS OrderCount
    FROM #pb
    GROUP BY TxDate, PaymentCode
    ORDER BY TxDate, PaymentCode;

    DROP TABLE #pb;
END;
GO
