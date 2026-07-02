using POS.Common.Dtos.Vouchers;

namespace POS.Infrastructure.Repositories.Interfaces;

/// <summary>
/// SAP Internal Voucher (real-time, POS.Api) — RPOSMasterData (CentralMD).
/// Dùng chung bảng CpnVchBOMCodeIssue với Setup Coupon (phân biệt bằng cột Source='SAP').
/// Đọc/ghi qua SP usp_Voucher_*. Thay ISAPVoucherRepository cũ (bảng Internal_Voucher, đã legacy).
/// </summary>
public interface IVoucherCodeRepository
{
    /// <summary>SP usp_Voucher_GetByCode — voucher SAP theo Code (Source='SAP'). Null nếu không tồn tại.</summary>
    Task<VoucherStatusResponse?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>SP usp_Voucher_Create — tạo mới nếu chưa tồn tại (idempotent, atomic UPDLOCK). Luôn trả bản ghi hiện tại đọc lại từ DB.</summary>
    Task<VoucherStatusResponse> CreateOrGetAsync(VoucherStatusResponse data, CancellationToken ct = default);

    /// <summary>SP usp_Voucher_Redeem — transaction UPDLOCK: check đủ số lượng + Status=SOLD + VoucherType (nếu có) + amount hợp lệ, rồi chuyển RDM.</summary>
    Task<(bool Success, string Message, List<VoucherStatusResponse> Results)> RedeemAsync(
        List<(string VoucherNumber, double AmountRedeem)> serials,
        string orderNo,
        string? requiredVoucherType = null,
        CancellationToken ct = default);
}
