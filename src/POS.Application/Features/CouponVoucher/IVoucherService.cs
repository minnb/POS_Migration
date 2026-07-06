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

    /// <summary>Phát hành voucher: validate + sinh/validate mã (Auto/Import) + check trùng DB + lưu.</summary>
    Task<VoucherSaveResult> SaveIssueAsync(VoucherIssueSaveRequest request, string actor, CancellationToken ct = default);

    /// <summary>Danh sách mã voucher đã phát hành (tab "Mã đã phát hành").</summary>
    Task<(List<VoucherCodeDto> Items, int Total)> GetCodesAsync(VoucherCodeFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Phát hành THÊM một lô mã Auto mới cho voucher đã tồn tại (không tạo header, không import).
    /// Reuse CouponVoucherCodeGenerator.GenerateAutoCodes + CheckCodesExistAsync giống SaveIssueAsync.
    /// </summary>
    Task<VoucherSaveResult> IssueMoreAsync(VoucherIssueMoreRequest request, string actor, CancellationToken ct = default);

    Task<(bool Ok, string Message)> DeleteAsync(string itemNo, CancellationToken ct = default);

    /// <summary>Cập nhật RIÊNG trạng thái khóa (Blocked) — dùng ở trang Xem voucher sau khi đã phát hành.</summary>
    Task<VoucherSaveResult> UpdateBlockedAsync(string itemNo, bool blocked, CancellationToken ct = default);
}
