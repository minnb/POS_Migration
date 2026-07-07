using POS.Common.Dtos.Promotion;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Application.Features.Promotion;

/// <summary>Thin wrapper — delegate xuống IPromotionRepository (không business logic).</summary>
public sealed class PromotionService(IPromotionRepository repository) : IPromotionService
{
    public Task<(List<OfferHeaderListItemDto> Items, int Total)> GetOfferHeaderListAsync(
        OfferListFilter filter, CancellationToken ct = default)
        => repository.GetOfferHeaderListAsync(filter, ct);

    public Task<List<OfferHeaderListItemDto>> ExportOfferHeaderListAsync(
        OfferListFilter filter, CancellationToken ct = default)
        => repository.ExportOfferHeaderListAsync(filter, ct);

    public Task<List<OfferTypeOptionDto>> GetOfferTypeOptionsAsync(CancellationToken ct = default)
        => repository.GetOfferTypeOptionsAsync(ct);

    public Task<List<OptionItemDto>> GetSalesOrderTypeOptionsAsync(CancellationToken ct = default)
        => repository.GetSalesOrderTypeOptionsAsync(ct);

    // ── Cài đặt CTKM (11.1) ──────────────────────────────────────────────────
    public Task<(List<PromotionSetupListItemDto> Items, int Total)> GetSetupListAsync(
        PromotionSetupListFilter filter, CancellationToken ct = default)
        => repository.GetSetupListAsync(filter, ct);

    public Task<PromotionSetupDetailDto?> GetSetupDetailAsync(string bbynr, CancellationToken ct = default)
        => repository.GetSetupDetailAsync(bbynr, ct);

    public Task<(bool Ok, string Message, string BBYNR)> SaveSetupAsync(
        PromotionSetupSaveRequest request, CancellationToken ct = default)
        => repository.SaveSetupAsync(request, ct);

    public Task<(bool Ok, string Message)> ApproveSetupAsync(string bbynr, CancellationToken ct = default)
        => repository.ApproveSetupAsync(bbynr, ct);

    public Task<bool> UpdateSetupStatusAsync(string bbynr, string status, CancellationToken ct = default)
        => repository.UpdateSetupStatusAsync(bbynr, status, ct);

    public Task<List<ItemOptionDto>> SearchItemsAsync(string keyword, CancellationToken ct = default)
        => repository.SearchItemsAsync(keyword, ct);

    public Task<List<OfferSiteLineDto>> GetSiteGroupOptionsAsync(CancellationToken ct = default)
        => repository.GetSiteGroupOptionsAsync(ct);

    public Task<List<OptionItemDto>> GetMemberCodeOptionsAsync(CancellationToken ct = default)
        => repository.GetMemberCodeOptionsAsync(ct);

    public Task<(bool Ok, string Message)> SaveSiteGroupAsync(SiteGroupSaveRequest request, string actor, CancellationToken ct = default)
        => repository.SaveSiteGroupAsync(request, actor, ct);

    public Task<(List<SiteGroupListItemDto> Items, int Total)> GetSiteGroupListAsync(
        string groupCode, string groupName, int pageNumber, int pageSize, CancellationToken ct = default)
        => repository.GetSiteGroupListAsync(groupCode, groupName, pageNumber, pageSize, ct);

    public Task<List<SiteGroupStoreItemDto>> GetSiteGroupStoresAsync(
        string groupCode, string storeNo, string storeName, CancellationToken ct = default)
        => repository.GetSiteGroupStoresAsync(groupCode, storeNo, storeName, ct);

    public Task<(bool Ok, string Message)> SaveItemGroupAsync(ItemGroupSaveRequest request, string actor, CancellationToken ct = default)
        => repository.SaveItemGroupAsync(request, actor, ct);

    public Task<(List<ItemGroupListItemDto> Items, int Total)> GetItemGroupListAsync(
        string groupCode, string groupName, int pageNumber, int pageSize, CancellationToken ct = default)
        => repository.GetItemGroupListAsync(groupCode, groupName, pageNumber, pageSize, ct);

    public Task<List<ItemGroupItemDto>> GetItemGroupItemsAsync(
        string groupCode, string itemNo, string itemName, CancellationToken ct = default)
        => repository.GetItemGroupItemsAsync(groupCode, itemNo, itemName, ct);

    // ── Modal "Xem chi tiết" 1 offer đã publish ───────────────────────────────
    public Task<OfferHeaderDetailDto?> GetOfferHeaderDetailAsync(string offerNo, CancellationToken ct = default)
        => repository.GetOfferHeaderDetailAsync(offerNo, ct);

    public Task<(List<OfferBuyDetailLineDto> Items, int Total)> GetOfferBuyDetailAsync(
        string offerNo, string? search, int pageNumber, int pageSize, CancellationToken ct = default)
        => repository.GetOfferBuyDetailAsync(offerNo, search, pageNumber, pageSize, ct);

    public Task<(List<OfferBenefitLineDto> Items, int Total)> GetOfferBenefitDetailAsync(
        string offerNo, int pageNumber, int pageSize, CancellationToken ct = default)
        => repository.GetOfferBenefitDetailAsync(offerNo, pageNumber, pageSize, ct);

    public Task<(List<OfferGetDetailLineDto> Items, int Total)> GetOfferGetDetailAsync(
        string offerNo, string? search, int pageNumber, int pageSize, CancellationToken ct = default)
        => repository.GetOfferGetDetailAsync(offerNo, search, pageNumber, pageSize, ct);

    // Ported from src/legacy/VCM.BLUEPOS/Views/Promotion/PromotionList.cshtml:1723-1738 (mapping tên kênh, JS hardcode)
    public async Task<(List<OfferSiteLineDetailDto> Items, int Total)> GetOfferSiteDetailAsync(
        string offerNo, string? search, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await repository.GetOfferSiteDetailAsync(offerNo, search, pageNumber, pageSize, ct);
        foreach (var item in items)
            item.StyleProfileName = MapStyleProfileName(item.StyleProfile);
        return (items, total);
    }

    public Task<(List<OfferPriorityLineDto> Items, int Total)> GetOfferPriorityDetailAsync(
        string offerType, int pageNumber, int pageSize, CancellationToken ct = default)
        => repository.GetOfferPriorityDetailAsync(offerType, pageNumber, pageSize, ct);

    public Task<(bool Ok, string Message)> DeactivateOfferAsync(string offerNo, CancellationToken ct = default)
        => repository.DeactivateOfferAsync(offerNo, ct);

    private static string MapStyleProfileName(string styleProfile) => styleProfile switch
    {
        "VM" => "WinMart",
        "VMP" => "WinMart+",
        "FS" => "FlagShip",
        "KS" => "Kiosk",
        _ => styleProfile
    };
}
