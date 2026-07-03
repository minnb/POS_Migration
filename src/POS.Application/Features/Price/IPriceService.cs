using POS.Common.Dtos.Price;
using POS.Common.Dtos.Promotion;

namespace POS.Application.Features.Price;

/// <summary>
/// 9.1 Danh mục Bảng giá + 9.3 Setup Giá — port từ VCM.BLUEPOS PriceController/SetupPriceController.
/// Validate nghiệp vụ ở tầng này; persistence qua IPriceRepository (SP).
/// </summary>
public interface IPriceService
{
    /// <summary>9.1 — list + filter + server-side paging.</summary>
    Task<(List<PriceListItemDto> Items, int Total)> GetPriceListAsync(
        PriceListFilter filter, CancellationToken ct = default);

    /// <summary>9.1 — toàn bộ dòng theo filter để xuất Excel.</summary>
    Task<List<PriceListItemDto>> ExportPriceListAsync(PriceListFilter filter, CancellationToken ct = default);

    /// <summary>9.3 — lookup form (Hình thức bán hàng + Nhóm giá + Loại khai báo).</summary>
    Task<PriceSetupLookupDto> GetSetupLookupAsync(CancellationToken ct = default);

    /// <summary>9.3 — tìm sản phẩm cho item picker (theo mã/tên).</summary>
    Task<List<ItemOptionDto>> SearchItemsAsync(string keyword, CancellationToken ct = default);

    /// <summary>9.3 — danh sách ĐVT của 1 item (cho dropdown ĐVT inline trên lưới khai báo tay).</summary>
    Task<List<string>> GetItemUomsAsync(string itemNo, CancellationToken ct = default);

    /// <summary>9.3 — validate danh sách dòng import Excel (trả kèm ErrorMessage/row).</summary>
    Task<List<PriceImportResultRow>> ValidateImportAsync(
        IReadOnlyList<PriceImportRow> rows, CancellationToken ct = default);

    /// <summary>9.3 — validate toàn bộ + build Pkey + lưu. Trả (Ok, Message).</summary>
    Task<PriceSaveResult> SaveAsync(
        PriceSetupContext context, IReadOnlyList<PriceSaveRow> rows, string actor, CancellationToken ct = default);
}
