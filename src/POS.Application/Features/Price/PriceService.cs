using System.Globalization;
using POS.Common.Dtos.Price;
using POS.Common.Dtos.Promotion;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Application.Features.Price;

/// <summary>
/// 9.1 Danh mục Bảng giá + 9.3 Setup Giá — port từ VCM.BLUEPOS PriceController/SetupPriceController.
/// Bảo toàn 100% điều kiện validate của SetupPriceController.SaveItemPrice.
/// Pkey = {SalesType}-{ItemNo}-{UOM}-{SalesCode} (giữ nguyên legacy).
/// </summary>
public sealed class PriceService(
    IPriceRepository repository,
    IPromotionRepository promotionRepository) : IPriceService
{
    public Task<(List<PriceListItemDto> Items, int Total)> GetPriceListAsync(
        PriceListFilter filter, CancellationToken ct = default)
        => repository.GetListAsync(filter, ct);

    public Task<List<PriceListItemDto>> ExportPriceListAsync(PriceListFilter filter, CancellationToken ct = default)
        => repository.GetExportListAsync(filter, ct);

    public async Task<PriceSetupLookupDto> GetSetupLookupAsync(CancellationToken ct = default)
    {
        var salesTypes = await promotionRepository.GetSalesOrderTypeOptionsAsync(ct);
        var priceGroups = await repository.GetPriceGroupOptionsAsync(ct);
        return new PriceSetupLookupDto
        {
            SalesTypes = salesTypes
                .Where(s => !string.IsNullOrWhiteSpace(s.Value))
                .Select(s => new PriceOptionDto { Value = s.Value, Text = string.IsNullOrWhiteSpace(s.Text) ? s.Value : s.Text })
                .ToList(),
            PriceGroups = priceGroups,
            DeclarationTypes =
            [
                new PriceOptionDto { Value = "ITEM_UOM", Text = "Theo ITEM + ĐVT" },
                new PriceOptionDto { Value = "BARCODE",  Text = "Theo barcode" }
            ]
        };
    }

    public Task<List<ItemOptionDto>> SearchItemsAsync(string keyword, CancellationToken ct = default)
        => promotionRepository.SearchItemsAsync(keyword ?? string.Empty, ct);

    public Task<List<string>> GetItemUomsAsync(string itemNo, CancellationToken ct = default)
        => repository.GetItemUomsAsync(itemNo ?? string.Empty, ct);

    public Task<List<PriceImportResultRow>> ValidateImportAsync(
        IReadOnlyList<PriceImportRow> rows, CancellationToken ct = default)
        => repository.ValidateImportAsync(rows, ct);

    public async Task<PriceSaveResult> SaveAsync(
        PriceSetupContext context, IReadOnlyList<PriceSaveRow> rows, string actor, CancellationToken ct = default)
    {
        if (rows.Count == 0)
            return Fail("Chưa có dòng bảng giá nào để lưu");
        if (string.IsNullOrWhiteSpace(context.SalesType))
            return Fail("Vui lòng chọn Hình thức bán hàng");

        var salesCode = string.IsNullOrWhiteSpace(context.SalesCode) ? "ALL" : context.SalesCode.Trim();

        // ── Validate nghiệp vụ (port SetupPriceController.SaveItemPrice — GIỮ NGUYÊN 100% điều kiện) ──
        var noItem = rows.Where(r => string.IsNullOrWhiteSpace(r.ItemNo)).ToList();
        if (noItem.Count > 0)
            return Fail($"Có {noItem.Count} dòng chưa chọn sản phẩm");

        var noUom = rows.Where(r => string.IsNullOrWhiteSpace(r.UOM)).ToList();
        if (noUom.Count > 0)
            return Fail($"Danh sách sản phẩm ({Join(noUom.Select(a => a.ItemNo))}) chưa chọn Đơn vị tính");

        // Parse ngày dd/MM/yyyy — gộp check định dạng TỪ NGÀY / ĐẾN NGÀY (length + tháng hợp lệ).
        var parsed = new List<(PriceSaveRow Row, DateTime Start, DateTime End, double Price)>();
        var badStart = new List<string?>();
        var badEnd = new List<string?>();
        foreach (var r in rows)
        {
            var okStart = TryParseDmy(r.StartDate, out var start);
            var okEnd = TryParseDmy(r.EndDate, out var end);
            if (!okStart) badStart.Add(r.ItemNo);
            if (!okEnd) badEnd.Add(r.ItemNo);
            parsed.Add((r, start, end, ParsePrice(r.UnitPrice)));
        }
        if (badStart.Count > 0)
            return Fail($"Danh sách sản phẩm ({Join(badStart)}) định dạng TỪ NGÀY không đúng");
        if (badEnd.Count > 0)
            return Fail($"Danh sách sản phẩm ({Join(badEnd)}) định dạng ĐẾN NGÀY không đúng");

        var dup = rows.GroupBy(x => new { ItemNo = x.ItemNo?.Trim(), UOM = x.UOM?.Trim() })
                      .Where(g => g.Count() > 1)
                      .Select(g => g.Key.ItemNo)
                      .ToList();
        if (dup.Count > 0)
            return Fail($"Danh sách sản phẩm ({Join(dup)}) bị trùng khi cài đặt");

        var badPrice = parsed.Where(x => x.Price <= 0).Select(x => x.Row.ItemNo).ToList();
        if (badPrice.Count > 0)
            return Fail($"Danh sách sản phẩm ({Join(badPrice)}) chưa cài đặt giá");

        var badRange = parsed.Where(x => x.Start.Date > x.End.Date).Select(x => x.Row.ItemNo).ToList();
        if (badRange.Count > 0)
            return Fail($"Danh sách sản phẩm ({Join(badRange)}) cài đặt TỪ NGÀY KHÔNG NHỎ HƠN ĐẾN NGÀY");

        var today = DateTime.Now.Date;
        var startPast = parsed.Where(x => x.Start.Date < today).Select(x => x.Row.ItemNo).ToList();
        if (startPast.Count > 0)
            return Fail($"Danh sách sản phẩm ({Join(startPast)}) cài đặt TỪ NGÀY không được nhỏ hơn ngày hiện tại");

        var endPast = parsed.Where(x => x.End.Date < today).Select(x => x.Row.ItemNo).ToList();
        if (endPast.Count > 0)
            return Fail($"Danh sách sản phẩm ({Join(endPast)}) cài đặt ĐẾN NGÀY không được nhỏ hơn ngày hiện tại");

        // ── Build lines (Pkey = {SalesType}-{ItemNo}-{UOM}-{SalesCode}) ──
        var lines = parsed.Select(x => new PriceSaveLine
        {
            ItemNo = x.Row.ItemNo!.Trim(),
            UOM = x.Row.UOM!.Trim(),
            SalesType = context.SalesType.Trim(),
            SalesCode = salesCode,
            UnitPrice = x.Price,
            StartDate = x.Start,
            EndDate = x.End,
            Pkey = $"{context.SalesType.Trim()}-{x.Row.ItemNo!.Trim()}-{x.Row.UOM!.Trim()}-{salesCode}"
        }).ToList();

        return await repository.SaveAsync(lines, actor, ct);
    }

    // 9.1 — Sửa giá (port SetupPriceController.UpdatePrice: chặn UnitPrice <= 0 trước khi gọi DB).
    public Task<PriceSaveResult> UpdatePriceAsync(
        PriceRowKey key, double unitPrice, string actor, CancellationToken ct = default)
        => unitPrice <= 0
            ? Task.FromResult(Fail("Giá bán phải lớn hơn 0"))
            : repository.UpdatePriceAsync(key, unitPrice, actor, ct);

    // 9.1 — Xóa mềm (port SetupPriceController.DeletePrice: không validate, ủy quyền DB).
    public Task<PriceSaveResult> DeletePriceAsync(PriceRowKey key, string actor, CancellationToken ct = default)
        => repository.SoftDeletePriceAsync(key, actor, ct);

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static PriceSaveResult Fail(string message) => new() { Ok = false, Message = message };

    private static string Join(IEnumerable<string?> items)
        => string.Join(",", items.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static bool TryParseDmy(string? dmy, out DateTime value)
        => DateTime.TryParseExact(dmy, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);

    private static double ParsePrice(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0;
        var cleaned = raw.Replace(",", string.Empty).Replace(" ", string.Empty).Trim();
        return double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;
    }
}
