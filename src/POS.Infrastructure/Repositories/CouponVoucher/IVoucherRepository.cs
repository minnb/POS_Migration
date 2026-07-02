using POS.Common.Dtos.Voucher;

namespace POS.Infrastructure.Repositories.Interfaces;

/// <summary>
/// 8.3 Danh mục Voucher — RPOSMasterData (CentralMD). Đọc/ghi qua SP usp_SetupVoucher_*.
/// Dùng chung bảng CpnVchBOMHeader/Line với Coupon; phân tách bằng NOT EXISTS CpnVchBOMIssueRule.
/// </summary>
public interface IVoucherRepository
{
    /// <summary>SP usp_SetupVoucher_GetList — list + filter + paging (Total/row).</summary>
    Task<(List<VoucherListItemDto> Items, int Total)> GetListAsync(
        VoucherListFilter filter, CancellationToken ct = default);

    /// <summary>SP usp_SetupVoucher_GetDetail — header + sản phẩm. Null nếu không tồn tại.</summary>
    Task<VoucherDetailDto?> GetDetailAsync(string itemNo, CancellationToken ct = default);

    /// <summary>SP usp_SetupVoucher_Save — upsert header + replace lines. Trả (Ok, Message, ItemNo).</summary>
    Task<VoucherSaveResult> SaveAsync(VoucherSaveRequest request, string actor, CancellationToken ct = default);

    /// <summary>
    /// SP usp_SetupVoucher_SaveIssue — phát hành voucher: upsert header + insert mã (Source='VOUCHER')
    /// vào CpnVchBOMCodeIssue + replace lines. Trả ItemNo (output). Lỗi nghiệp vụ (serial trùng) → THROW.
    /// </summary>
    Task<string> SaveIssueAsync(VoucherIssueSaveRequest request, IReadOnlyList<string> codes, CancellationToken ct = default);

    /// <summary>SP usp_SetupVoucher_GetCodes — danh sách mã đã phát hành (Source='VOUCHER'), phân trang.</summary>
    Task<(List<VoucherCodeDto> Items, int Total)> GetCodesAsync(VoucherCodeFilter filter, CancellationToken ct = default);

    /// <summary>SP usp_SetupVoucher_CheckCodesExist — trả các mã đã tồn tại trong DB (toàn bảng).</summary>
    Task<List<string>> CheckCodesExistAsync(IEnumerable<string> codes, CancellationToken ct = default);

    /// <summary>SP usp_SetupVoucher_Delete — xóa header + lines. Trả (Deleted, Message).</summary>
    Task<(bool Deleted, string Message)> DeleteAsync(string itemNo, CancellationToken ct = default);

    /// <summary>
    /// SP usp_SetupVoucher_UpdateBlocked — cập nhật RIÊNG field Blocked trên CpnVchBOMHeader.
    /// Dùng cho trang Xem voucher (sau phát hành chỉ còn cho phép khóa/mở khóa).
    /// </summary>
    Task<VoucherSaveResult> UpdateBlockedAsync(string itemNo, bool blocked, CancellationToken ct = default);
}
