using POS.Common.Dtos.SetupCoupon;

namespace POS.Infrastructure.Repositories.Interfaces;

/// <summary>
/// 8.1/8.2 Setup Coupon — RPOSMasterData (CentralMD). Đọc/ghi qua SP usp_SetupCoupon_*.
/// </summary>
public interface ICouponRepository
{
    /// <summary>SP usp_SetupCoupon_GetList — list + filter + paging (Total/row). Màn "Phát hành Coupon" (join IssueRule).</summary>
    Task<(List<CouponListItemDto> Items, int Total)> GetListAsync(
        CouponListFilter filter, CancellationToken ct = default);

    /// <summary>SP usp_CpnVchBOMHeader_GetList — danh sách master Coupon/Voucher (list thẳng Header, mọi ArticleType).</summary>
    Task<(List<CouponHeaderListItemDto> Items, int Total)> GetHeaderListAsync(
        CouponHeaderListFilter filter, CancellationToken ct = default);

    /// <summary>SP usp_SetupCoupon_GetCodes — mã coupon theo ItemNo (paging).</summary>
    Task<(List<CouponCodeDto> Items, int Total)> GetCodesAsync(
        CouponCodeFilter filter, CancellationToken ct = default);

    /// <summary>SP usp_SetupCoupon_GetDetail — header + rule + sản phẩm. Null nếu không tồn tại.</summary>
    Task<CouponDetailDto?> GetDetailAsync(string itemNo, CancellationToken ct = default);

    /// <summary>Dropdown form: SalesType (IsActive), StoreGroup (Status), UnitOfMeasure. Inline SQL.</summary>
    Task<CouponFormLookupDto> GetFormLookupAsync(CancellationToken ct = default);

    /// <summary>SP usp_SetupCoupon_CheckCodesExist — trả các mã đã tồn tại trong DB.</summary>
    Task<List<string>> CheckCodesExistAsync(IEnumerable<string> codes, CancellationToken ct = default);

    /// <summary>SP usp_SetupCoupon_SaveIssue — upsert rule+header, insert codes, replace lines/stores. Trả ItemNo.</summary>
    Task<string> SaveIssueAsync(CouponIssueSaveRequest request, IReadOnlyList<string> codes, CancellationToken ct = default);

    /// <summary>SP usp_SetupCoupon_SaveAdvanced — upsert field nâng cao. Trả ItemNo.</summary>
    Task<string> SaveAdvancedAsync(CouponAdvancedSaveRequest request, CancellationToken ct = default);

    /// <summary>SP usp_SetupCoupon_IssueMore — thêm 1 lô mã Auto mới cho coupon đã tồn tại (không đổi header/lines). Trả số mã đã thêm.</summary>
    Task<int> IssueMoreAsync(string itemNo, IReadOnlyList<string> codes, CancellationToken ct = default);

    /// <summary>SP usp_SetupCoupon_Delete — xóa (guard QtyCoupon==0). Trả (Deleted, Message).</summary>
    Task<(bool Deleted, string Message)> DeleteAsync(string itemNo, CancellationToken ct = default);

    /// <summary>
    /// SP usp_SetupCoupon_UpdateBlocked — cập nhật RIÊNG field Blocked trên CpnVchBOMHeader.
    /// Dùng cho nút Xóa (soft-block) ở danh sách Coupon và checkbox Khóa ở trang Xem coupon.
    /// </summary>
    Task<CouponSaveResult> UpdateBlockedAsync(string itemNo, bool blocked, CancellationToken ct = default);
}
