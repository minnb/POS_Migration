namespace POS.Web.Services.Pdf;

/// <summary>
/// Dữ liệu cho báo cáo pivot dạng ma trận: hàng = entity, cột = ngày, ô = (số lượng, số tiền).
/// Generic cho mọi báo cáo pivot-by-date — không gắn cứng "ngành hàng".
/// </summary>
public sealed class PivotReportData
{
    /// <summary>Caption cột mã/tên entity. VD: "Tên gian hàng".</summary>
    public string RowCodeHeader { get; init; } = "Tên gian hàng";

    /// <summary>Caption cột chỉ số tổng (2 dòng số lượng/số tiền). VD: "Số lượng/Số tiền".</summary>
    public string MetricHeader { get; init; } = "Số lượng/Số tiền";

    /// <summary>Danh sách ngày = các cột (đã sort tăng dần).</summary>
    public IReadOnlyList<DateTime> Dates { get; init; } = [];

    /// <summary>Các hàng dữ liệu.</summary>
    public IReadOnlyList<PivotReportRow> Rows { get; init; } = [];

    public long TotalQty { get; init; }
    public double TotalAmt { get; init; }
}

/// <summary>Một hàng trong báo cáo pivot. <paramref name="ByDate"/> key = ngày, value = (Qty, Amt).</summary>
public sealed record PivotReportRow(
    string Code,
    string Name,
    long TotalQty,
    double TotalAmt,
    IReadOnlyDictionary<DateTime, (long Qty, double Amt)> ByDate);
