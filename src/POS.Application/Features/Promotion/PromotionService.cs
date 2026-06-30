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

    public Task<List<OptionItemDto>> GetOfferTypeOptionsAsync(CancellationToken ct = default)
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
}
