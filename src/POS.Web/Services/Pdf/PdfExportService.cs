using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POS.Web.Services.Pdf;

/// <summary>
/// Cài đặt xuất PDF báo cáo bằng QuestPDF. Stateless → đăng ký singleton.
/// Khung header (<see cref="ComposeReportHeader"/>) dùng chung cho mọi loại báo cáo;
/// mỗi loại báo cáo có 1 method dựng body riêng (hiện có pivot).
/// </summary>
public sealed class PdfExportService : IPdfExportService
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    // Màu khớp app.css (.rpt-pivot-table) để PDF giống form trên màn hình
    private static readonly Color HeaderBg    = Color.FromHex("#4FC3F7");
    private static readonly Color HeaderBorder = Color.FromHex("#039BE5");
    private static readonly Color AmountColor  = Color.FromHex("#1976D2");
    private static readonly Color TotalBg       = Color.FromHex("#ECEFF1");
    private static readonly Color TotalBorder   = Color.FromHex("#B0BEC5");
    private static readonly Color RowBorder     = Color.FromHex("#EEEEEE");
    private static readonly Color MutedColor    = Color.FromHex("#555555");

    public byte[] BuildPivotReport(ReportHeaderModel header, PivotReportData data)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeReportHeader(c, header));
                page.Content().PaddingTop(6).Element(c => ComposePivotTable(c, data));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(8).FontColor(MutedColor));
                    t.Span("Trang ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }

    // ── Khung header dùng chung — khớp khối header trên màn hình ───────────
    private static void ComposeReportHeader(IContainer container, ReportHeaderModel h)
    {
        container.Column(col =>
        {
            col.Spacing(3);

            // Hàng 1: user info (trái) + timestamp (phải)
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => { t.DefaultTextStyle(s => s.FontSize(8)); t.Span("ID của người dùng: ").FontColor(MutedColor); t.Span(h.UserId).SemiBold().FontColor(MutedColor); });
                    c.Item().Text(t => { t.DefaultTextStyle(s => s.FontSize(8)); t.Span("Tên người dùng: ").FontColor(MutedColor); t.Span(h.UserFullName).SemiBold().FontColor(MutedColor); });
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(8));
                    t.Span("Ngày giờ: ").FontColor(MutedColor);
                    t.Span(h.PrintedAt.ToString("dd/MM/yyyy HH:mm:ss", Vi)).SemiBold().FontColor(MutedColor);
                });
            });

            // Hàng 2: tiêu đề in hoa, căn giữa
            col.Item().PaddingVertical(6).AlignCenter().Text(h.Title.ToUpper(Vi)).Bold().FontSize(13);

            // Hàng 3: cửa hàng + khoảng ngày
            col.Item().Text(t => { t.DefaultTextStyle(s => s.FontSize(9)); t.Span("Cửa hàng: "); t.Span(h.StoreLabel).SemiBold(); });
            col.Item().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(9));
                t.Span("Ngày giao dịch: ");
                t.Span($"{h.FromDate:dd/MM/yyyy} – {h.ToDate:dd/MM/yyyy}").SemiBold();
            });
        });
    }

    // ── Bảng pivot — khớp bảng .rpt-pivot-table trên màn hình ─────────────
    private static void ComposePivotTable(IContainer container, PivotReportData data)
    {
        // Font co theo số cột-ngày để bảng vừa khổ A4 landscape (page chặn > 31 cột)
        var bodyFont = data.Dates.Count switch
        {
            > 24 => 6.5f,
            > 16 => 7.5f,
            > 10 => 8f,
            _    => 9f
        };

        container.DefaultTextStyle(x => x.FontSize(bodyFont)).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28);   // STT
                columns.RelativeColumn(3);    // Tên gian hàng
                columns.ConstantColumn(74);   // Số lượng/Số tiền (tổng)
                foreach (var _ in data.Dates)
                    columns.RelativeColumn(1.4f);   // mỗi ngày — co giãn để vừa khổ
            });

            // Header row — lặp lại trên mỗi trang
            table.Header(h =>
            {
                h.Cell().Element(HeaderCell).AlignCenter().Text("STT");
                h.Cell().Element(HeaderCell).AlignLeft().Text(data.RowCodeHeader);
                h.Cell().Element(HeaderCell).AlignRight().Text(data.MetricHeader);
                foreach (var d in data.Dates)
                {
                    h.Cell().Element(HeaderCell).Column(c =>
                    {
                        c.Item().AlignRight().Text(d.ToString("dd/MM", Vi));
                        c.Item().AlignRight().Text($"({GetDow(d)})").FontSize(bodyFont - 2).Light();
                    });
                }

                static IContainer HeaderCell(IContainer c) =>
                    c.Background(HeaderBg).BorderBottom(1).BorderColor(HeaderBorder).PaddingVertical(4).PaddingHorizontal(4);
            });

            // Body rows
            var stt = 1;
            foreach (var row in data.Rows)
            {
                table.Cell().Element(BodyCell).AlignCenter().Text((stt++).ToString(Vi));
                table.Cell().Element(BodyCell).Column(c =>
                {
                    c.Item().Text(row.Code).SemiBold();
                    c.Item().Text(row.Name).FontColor(AmountColor).FontSize(bodyFont - 1);
                });
                table.Cell().Element(BodyCell).AlignRight().Column(c =>
                {
                    c.Item().Text(N0(row.TotalQty));
                    c.Item().Text(N0(row.TotalAmt)).FontColor(AmountColor).SemiBold();
                });
                foreach (var d in data.Dates)
                {
                    var has = row.ByDate.TryGetValue(d, out var v);
                    var qty = has ? v.Qty : 0L;
                    var amt = has ? v.Amt : 0d;
                    table.Cell().Element(BodyCell).AlignRight().Column(c =>
                    {
                        c.Item().Text(qty > 0 ? N0(qty) : string.Empty);
                        c.Item().Text(amt > 0 ? N0(amt) : string.Empty).FontColor(AmountColor);
                    });
                }
            }

            // Footer Total
            table.Cell().ColumnSpan(2).Element(TotalCell).AlignCenter().Text("Total").Bold();
            table.Cell().Element(TotalCell).AlignRight().Column(c =>
            {
                c.Item().Text(N0(data.TotalQty)).Bold();
                c.Item().Text(N0(data.TotalAmt)).Bold();
            });
            foreach (var d in data.Dates)
            {
                var qty = data.Rows.Sum(r => r.ByDate.TryGetValue(d, out var v) ? v.Qty : 0L);
                var amt = data.Rows.Sum(r => r.ByDate.TryGetValue(d, out var v) ? v.Amt : 0d);
                table.Cell().Element(TotalCell).AlignRight().Column(c =>
                {
                    c.Item().Text(qty > 0 ? N0(qty) : string.Empty).Bold();
                    c.Item().Text(amt > 0 ? N0(amt) : string.Empty).Bold();
                });
            }

            static IContainer BodyCell(IContainer c) =>
                c.BorderBottom(1).BorderColor(RowBorder).PaddingVertical(3).PaddingHorizontal(4);

            static IContainer TotalCell(IContainer c) =>
                c.Background(TotalBg).BorderTop(1).BorderColor(TotalBorder).PaddingVertical(4).PaddingHorizontal(4);
        });
    }

    private static string N0(long v) => v.ToString("N0", Vi);
    private static string N0(double v) => v.ToString("N0", Vi);

    private static string GetDow(DateTime d) => d.DayOfWeek switch
    {
        DayOfWeek.Monday    => "Mon",
        DayOfWeek.Tuesday   => "Tue",
        DayOfWeek.Wednesday => "Wed",
        DayOfWeek.Thursday  => "Thu",
        DayOfWeek.Friday    => "Fri",
        DayOfWeek.Saturday  => "Sat",
        DayOfWeek.Sunday    => "Sun",
        _ => ""
    };
}
