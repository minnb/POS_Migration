using POS.Common.Dtos.Promotion;

namespace POS.Application.Features.Promotion;

/// <summary>
/// Danh mục khuyến mãi (Offer Header) — Application layer, dùng chung cho POS.Web và POS.Api.
/// </summary>
public interface IPromotionService
{
    Task<(List<OfferHeaderListItemDto> Items, int Total)> GetOfferHeaderListAsync(
        OfferListFilter filter, CancellationToken ct = default);

    Task<List<OfferHeaderListItemDto>> ExportOfferHeaderListAsync(
        OfferListFilter filter, CancellationToken ct = default);

    Task<List<OfferTypeOptionDto>> GetOfferTypeOptionsAsync(CancellationToken ct = default);

    Task<List<OptionItemDto>> GetSalesOrderTypeOptionsAsync(CancellationToken ct = default);

    // ── Cài đặt CTKM (11.1) ──────────────────────────────────────────────────
    Task<(List<PromotionSetupListItemDto> Items, int Total)> GetSetupListAsync(
        PromotionSetupListFilter filter, CancellationToken ct = default);

    Task<PromotionSetupDetailDto?> GetSetupDetailAsync(string bbynr, CancellationToken ct = default);

    Task<(bool Ok, string Message, string BBYNR)> SaveSetupAsync(
        PromotionSetupSaveRequest request, CancellationToken ct = default);

    Task<(bool Ok, string Message)> ApproveSetupAsync(string bbynr, CancellationToken ct = default);

    Task<bool> UpdateSetupStatusAsync(string bbynr, string status, CancellationToken ct = default);

    Task<List<ItemOptionDto>> SearchItemsAsync(string keyword, CancellationToken ct = default);

    Task<List<OfferSiteLineDto>> GetSiteGroupOptionsAsync(CancellationToken ct = default);

    Task<List<OptionItemDto>> GetMemberCodeOptionsAsync(CancellationToken ct = default);

    Task<(bool Ok, string Message)> SaveSiteGroupAsync(SiteGroupSaveRequest request, string actor, CancellationToken ct = default);

    Task<(List<SiteGroupListItemDto> Items, int Total)> GetSiteGroupListAsync(
        string groupCode, string groupName, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<List<SiteGroupStoreItemDto>> GetSiteGroupStoresAsync(
        string groupCode, string storeNo, string storeName, CancellationToken ct = default);

    Task<(bool Ok, string Message)> SaveItemGroupAsync(ItemGroupSaveRequest request, string actor, CancellationToken ct = default);

    Task<(List<ItemGroupListItemDto> Items, int Total)> GetItemGroupListAsync(
        string groupCode, string groupName, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<List<ItemGroupItemDto>> GetItemGroupItemsAsync(
        string groupCode, string itemNo, string itemName, CancellationToken ct = default);

    // ── Modal "Xem chi tiết" 1 offer đã publish ───────────────────────────────
    Task<OfferHeaderDetailDto?> GetOfferHeaderDetailAsync(string offerNo, CancellationToken ct = default);

    Task<(List<OfferBuyDetailLineDto> Items, int Total)> GetOfferBuyDetailAsync(
        string offerNo, string? search, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<(List<OfferBenefitLineDto> Items, int Total)> GetOfferBenefitDetailAsync(
        string offerNo, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<(List<OfferGetDetailLineDto> Items, int Total)> GetOfferGetDetailAsync(
        string offerNo, string? search, int pageNumber, int pageSize, CancellationToken ct = default);

    /// <summary>Danh sách cửa hàng áp dụng — StyleProfileName được map ở đây (display logic, không phải SQL).</summary>
    Task<(List<OfferSiteLineDetailDto> Items, int Total)> GetOfferSiteDetailAsync(
        string offerNo, string? search, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<(List<OfferPriorityLineDto> Items, int Total)> GetOfferPriorityDetailAsync(
        string offerType, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<(bool Ok, string Message)> DeactivateOfferAsync(string offerNo, CancellationToken ct = default);
}
