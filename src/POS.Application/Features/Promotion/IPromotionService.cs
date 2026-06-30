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

    Task<List<OptionItemDto>> GetOfferTypeOptionsAsync(CancellationToken ct = default);

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
}
