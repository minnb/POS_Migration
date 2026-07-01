using POS.Common.Dtos.Voucher;

namespace POS.Application.Features.CouponVoucher;

/// <summary>
/// 8.3 Danh mục Voucher — business (validate serial/ngày/items) + delegate ICouponVoucher repo.
/// Item picker tái dùng ICentralMDRepository.GetProductListAsync. POS.Api tái dùng được.
/// </summary>
public interface IVoucherService
{
    Task<(List<VoucherListItemDto> Items, int Total)> GetListAsync(
        VoucherListFilter filter, CancellationToken ct = default);

    Task<VoucherDetailDto?> GetDetailAsync(string itemNo, CancellationToken ct = default);

    Task<VoucherFormLookupDto> GetFormLookupAsync(CancellationToken ct = default);

    Task<(List<VoucherLineDto> Items, int Total)> SearchItemsAsync(
        string? itemNo, string? itemName, int pageSize, int pageNumber, CancellationToken ct = default);

    Task<VoucherSaveResult> SaveAsync(VoucherSaveRequest request, string actor, CancellationToken ct = default);

    Task<(bool Ok, string Message)> DeleteAsync(string itemNo, CancellationToken ct = default);
}
