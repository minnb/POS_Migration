using POS.Common.Dtos.Price;

namespace POS.Infrastructure.Repositories.Interfaces;

/// <summary>
/// 9.1 Danh mục Bảng giá + 9.3 Setup Giá — DB RPOSMasterData (CentralMD).
/// Port từ VCM.BLUEPOS PriceData/SetupPriceData. Đọc qua SP có sẵn (GetSalesPriceList*),
/// validate import + lưu qua SP mới usp_SetupSalePrice_* (docs/sql/SetupSalePrice_Save.sql).
/// </summary>
public interface IPriceRepository
{
    /// <summary>SP [dbo].[GetSalesPriceList] — list + filter + server-side paging (Total/row).</summary>
    Task<(List<PriceListItemDto> Items, int Total)> GetListAsync(
        PriceListFilter filter, CancellationToken ct = default);

    /// <summary>SP [dbo].[GetSalesPriceList_Export] — toàn bộ dòng theo filter (không paging) để xuất Excel.</summary>
    Task<List<PriceListItemDto>> GetExportListAsync(PriceListFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Validate danh sách dòng import qua TVP dbo.SetupSalePriceImportTVP — join Item/ItemUnitOfMeasure/Barcodes.
    /// Trả từng dòng kèm ErrorMessage (rỗng = hợp lệ). Port nguyên query legacy SetupPriceData.ValidateImport.
    /// </summary>
    Task<List<PriceImportResultRow>> ValidateImportAsync(
        IReadOnlyList<PriceImportRow> rows, CancellationToken ct = default);

    /// <summary>
    /// SP usp_SetupSalePrice_Save — INSERT Pkey mới + cập nhật Pkey đã tồn tại (ủy quyền Setup_SalePrice_Get_ALL).
    /// Trả (Ok, Message).
    /// </summary>
    Task<PriceSaveResult> SaveAsync(IReadOnlyList<PriceSaveLine> lines, string actor, CancellationToken ct = default);

    /// <summary>SP usp_SalesPrice_UpdatePrice — sửa UnitPrice in-place theo composite PK + bump Counter. Trả (Ok, Message).</summary>
    Task<PriceSaveResult> UpdatePriceAsync(PriceRowKey key, double unitPrice, string actor, CancellationToken ct = default);

    /// <summary>SP usp_SalesPrice_SoftDelete — soft-delete (EndingDate năm 7777) theo composite PK + bump Counter. Trả (Ok, Message).</summary>
    Task<PriceSaveResult> SoftDeletePriceAsync(PriceRowKey key, string actor, CancellationToken ct = default);

    /// <summary>Dropdown Nhóm giá — DISTINCT dbo.StorePriceGroup (PriceGroupCode/PriceGroupName), cache Redis 12h.</summary>
    Task<List<PriceOptionDto>> GetPriceGroupOptionsAsync(CancellationToken ct = default);

    /// <summary>Danh sách ĐVT của 1 item — dbo.ItemUnitOfMeasure.Code (cho dropdown ĐVT inline).</summary>
    Task<List<string>> GetItemUomsAsync(string itemNo, CancellationToken ct = default);
}
