namespace POS.Web.Services.Pdf;

/// <summary>
/// Thông tin header dùng chung cho mọi báo cáo xuất PDF — khớp với khối header
/// hiển thị trên màn hình (xem <c>.claude/skills/web/reports.md</c> §Report Page Layout).
/// </summary>
public sealed record ReportHeaderModel
{
    /// <summary>Tiêu đề báo cáo (in hoa, căn giữa). VD: "TỔNG HỢP DOANH THU THEO NGÀNH HÀNG".</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>ID người dùng (username) — góc trái.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>Họ tên người dùng — góc trái.</summary>
    public string UserFullName { get; init; } = string.Empty;

    /// <summary>Thời điểm in — góc phải.</summary>
    public DateTime PrintedAt { get; init; } = DateTime.Now;

    /// <summary>Nhãn cửa hàng. VD: "2018 – Cửa hàng demo" hoặc "Tất cả".</summary>
    public string StoreLabel { get; init; } = "Tất cả";

    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
}
