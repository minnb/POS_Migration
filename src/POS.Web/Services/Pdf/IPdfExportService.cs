namespace POS.Web.Services.Pdf;

/// <summary>
/// Helper xuất báo cáo dashboard ra PDF (dùng QuestPDF). Khung header + cơ chế tạo file
/// dùng chung cho mọi page báo cáo trong POS.Web; mỗi loại báo cáo có 1 method dựng body riêng.
/// </summary>
public interface IPdfExportService
{
    /// <summary>
    /// Dựng PDF cho báo cáo pivot (ma trận entity × ngày). Dữ liệu truyền vào là dữ liệu
    /// đã hiển thị trên màn hình → PDF khớp 100% với form người dùng đang xem.
    /// </summary>
    byte[] BuildPivotReport(ReportHeaderModel header, PivotReportData data);
}
