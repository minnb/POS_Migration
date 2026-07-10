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

    /// <summary>9.1 — sửa giá 1 dòng (in-place UnitPrice). UnitPrice phải &gt; 0. Trả (Ok, Message).</summary>
    Task<PriceSaveResult> UpdatePriceAsync(
        PriceRowKey key, double unitPrice, string actor, CancellationToken ct = default);

    /// <summary>9.1 — xóa mềm 1 dòng giá (sentinel năm 7777). Trả (Ok, Message).</summary>
    Task<PriceSaveResult> DeletePriceAsync(PriceRowKey key, string actor, CancellationToken ct = default);

    // ── Danh mục nhóm giá (StorePriceGroupHeader + StorePriceGroup) ──────────────
    /// <summary>Danh sách nhóm giá + filter + server-side paging.</summary>
    Task<(List<PriceGroupListItemDto> Items, int Total)> GetPriceGroupListAsync(
        PriceGroupListFilter filter, CancellationToken ct = default);

    /// <summary>Danh sách cửa hàng đã gán vào 1 nhóm giá.</summary>
    Task<List<PriceGroupStoreItemDto>> GetPriceGroupStoresAsync(string priceGroupCode, CancellationToken ct = default);

    /// <summary>Lưu (thêm/sửa) nhóm giá + danh sách store (add-only). Validate mã/tên/Priority/≥1 store. Trả (Ok, Message).</summary>
    Task<PriceSaveResult> SavePriceGroupAsync(PriceGroupSaveRequest request, string actor, CancellationToken ct = default);

    /// <summary>Xóa nhóm giá (chặn nếu đang dùng trong bảng giá). Trả (Ok, Message).</summary>
    Task<PriceSaveResult> DeletePriceGroupAsync(string priceGroupCode, string actor, CancellationToken ct = default);
}
