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

    /// <summary>SP usp_SetupVoucher_Delete — xóa header + lines. Trả (Deleted, Message).</summary>
    Task<(bool Deleted, string Message)> DeleteAsync(string itemNo, CancellationToken ct = default);
}
