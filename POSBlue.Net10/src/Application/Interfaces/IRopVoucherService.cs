using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Abstraction cho ROP (Retail Operation Platform) REST API — port từ ROPVoucherService cũ.
/// Sử dụng Basic Auth (Base64 UserName:Password từ SysWebApi AppCode="ROP").
/// Tất cả routes lấy từ SysWebApiRoute của config ROP.
/// </summary>
public interface IRopVoucherService
{
    /// <summary>Kiểm tra serial voucher đơn lẻ (dùng cho CheckVoucherBySerials — route CheckVoucherBySerials).</summary>
    Task<(bool Success, string Message, VoucherStatusResponse? Data)> CheckVoucherBySerialAsync(
        string voucherNumber, string siteNo, string posTerminal);

    /// <summary>Kiểm tra voucher/coupon (dùng cho CheckVoucher — route CheckVouchers).</summary>
    Task<(bool Success, string Message, VoucherStatusResponse? Data)> CheckVoucherAsync(
        string voucherNumber, string siteNo, string posTerminal);

    /// <summary>Tạo mới voucher/coupon trên ROP — route InsertVoucher.</summary>
    Task<(bool Success, string Message, List<VoucherStatusResponse>? Data)> CreateVoucherAsync(
        List<CreateVoucherModel> model, string voucherType, int statusId);

    /// <summary>Cập nhật trạng thái voucher — route UpdateVoucherStatus.</summary>
    Task<(bool Success, string Message, List<VoucherStatusResponse>? Data)> UpdateVoucherStatusAsync(
        VoucherUpdateModel model);

    /// <summary>Retry cập nhật trạng thái từ cache (Redis hash) — dùng cho retry flow trong RedeemCpnVch.</summary>
    Task<List<VoucherStatusResponse>?> RetryUpdateVoucherStatusAsync(string orderNo);

    /// <summary>Gọi retry UpdateRedeemCpnVchCodeSends — dùng cho GET api/sap/retry.</summary>
    Task UpdateRedeemCpnVchCodeSendsAsync();

    /// <summary>Trả true nếu voucher thuộc ROP (prefix khớp), false nếu SAP.</summary>
    bool IsRopVoucher(string voucherNumber, string ropPrefix);
}
