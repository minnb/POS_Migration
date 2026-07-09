using POS.Common.Dtos.SetupCoupon;

namespace POS.Application.Features.CouponVoucher;

/// <summary>
/// 8.1/8.2 Setup Coupon — business (sinh mã Auto, validate ngày/độ dài, validate mã Import,
/// tra sản phẩm) + delegate xuống ICouponRepository. POS.Api tái dùng được.
/// </summary>
public interface ICouponService
{
    Task<(List<CouponListItemDto> Items, int Total)> GetListAsync(
        CouponListFilter filter, CancellationToken ct = default);

    /// <summary>Danh sách master Coupon/Voucher (list thẳng CpnVchBOMHeader, mọi ArticleType) — trang /promotion/coupons.</summary>
    Task<(List<CouponHeaderListItemDto> Items, int Total)> GetHeaderListAsync(
        CouponHeaderListFilter filter, CancellationToken ct = default);

    Task<(List<CouponCodeDto> Items, int Total)> GetCodesAsync(
        CouponCodeFilter filter, CancellationToken ct = default);

    Task<CouponDetailDto?> GetDetailAsync(string itemNo, CancellationToken ct = default);

    Task<CouponFormLookupDto> GetFormLookupAsync(CancellationToken ct = default);

    /// <summary>Tra sản phẩm cho picker (reuse GetProductList). Trả (Items, Total) server-side paging.</summary>
    Task<(List<CouponItemLineDto> Items, int Total)> SearchItemsAsync(
        string? itemNo, string? itemName, int pageSize, int pageNumber, CancellationToken ct = default);

    /// <summary>Phát hành coupon: sinh/validate mã (Auto/Import) + check trùng DB + lưu.</summary>
    Task<CouponSaveResult> SaveIssueAsync(CouponIssueSaveRequest request, CancellationToken ct = default);

    /// <summary>Cài đặt nâng cao (discount/limit/blocked...).</summary>
    Task<CouponSaveResult> SaveAdvancedAsync(CouponAdvancedSaveRequest request, CancellationToken ct = default);

    /// <summary>Xóa coupon (guard QtyCoupon==0).</summary>
    Task<(bool Ok, string Message)> DeleteAsync(string itemNo, CancellationToken ct = default);

    /// <summary>Phát hành THÊM một lô mã Auto mới cho coupon đã tồn tại (không tạo header, không import).</summary>
    Task<CouponSaveResult> IssueMoreAsync(CouponIssueMoreRequest request, CancellationToken ct = default);

    /// <summary>Cập nhật RIÊNG trạng thái khóa (Blocked) — dùng cho nút Xóa ở danh sách và checkbox Khóa ở trang Xem coupon.</summary>
    Task<CouponSaveResult> UpdateBlockedAsync(string itemNo, bool blocked, CancellationToken ct = default);
}
