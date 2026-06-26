namespace POS.Common.Dtos.RptCentralSale;

// Map các result set của SP [dbo].[Rpt_ReportSaleByPayment].
// Số tiền đã Net (trừ hàng trả) ngay trong SP. Amount là FLOAT (DB) → double.

// RESULT SET 1 — KPI tổng hợp (1 dòng).
public sealed class PaymentKpiDto
{
    public double TotalPaymentAmount  { get; set; }
    public int    TotalOrders         { get; set; }
    public int    TotalPaymentMethods { get; set; }
    public double CashAmount          { get; set; }
    public double CashPct             { get; set; }
    public double NonCashAmount       { get; set; }
    public double NonCashPct          { get; set; }
}

// RESULT SET 2 — mỗi hình thức thanh toán 1 dòng (Pareto bar list + bảng chi tiết).
public sealed class PaymentByMethodDto
{
    public string PaymentCode   { get; set; } = string.Empty;
    public string PaymentName   { get; set; } = string.Empty;
    public double PaymentAmount { get; set; }
    public int    OrderCount    { get; set; }
    public double AvgPerOrder   { get; set; }
    public double SharePct      { get; set; }
    public double CumulativePct { get; set; }   // Pareto tích lũy
    public int    Rank          { get; set; }
}

// RESULT SET 3 — mỗi ngày × HTTT 1 dòng (line chart xu hướng).
public sealed class PaymentTrendDto
{
    public DateTime TxDate        { get; set; }
    public string   DateLabel     { get; set; } = string.Empty;
    public string   PaymentCode   { get; set; } = string.Empty;
    public string   PaymentName   { get; set; } = string.Empty;
    public double   PaymentAmount { get; set; }
    public int      OrderCount    { get; set; }
}
