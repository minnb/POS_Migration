using System.Net;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Tập hợp toàn bộ use case của SAPController (voucher + coupon SAP/ROP/PLG/Capillary).
/// Controller chỉ delegate sang đây — không chứa business logic.
/// </summary>
public interface ISAPService
{
    /// <summary>GET api/sap/CheckVoucherBySerials — kiểm tra serial voucher (ưu tiên ROP, fallback SAP SOAP).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> CheckVoucherBySerialsAsync(
        string voucherNumber, string posTerminal, bool isLog = true);

    /// <summary>GET api/sap/CheckVoucher — kiểm tra voucher/coupon (WinX → Capillary → PLG → Olala → ROP/SAP).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> CheckVoucherAsync(
        string voucherNumber, string siteNo, string posTerminal,
        string companyCode, string isEmployee, string partner,
        string isCardVoucher, int quantity, bool isVoucher,
        string phoneNumber, bool isLog = true);

    /// <summary>POST api/sap/CreateNewVoucher — tạo mới voucher (ROP hoặc SAP SOAP).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> CreateNewVoucherAsync(
        List<CreateVoucherModel> model);

    /// <summary>POST api/sap/UpdateVoucher — cập nhật trạng thái voucher (ROP hoặc SAP SOAP).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> UpdateVoucherAsync(
        List<VoucherUpdateRequest> model);

    /// <summary>POST api/sap/winlife/redeemCpnVch — sử dụng voucher/coupon (ROP + Capillary + PLG + SAP SOAP).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> RedeemCpnVchAsync(VoucherUpdateModel model);

    /// <summary>GET api/sap/CheckReturnVoucher — kiểm tra BNMH (ROP hoặc SAP SOAP return).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> CheckReturnVoucherAsync(
        string voucherNumber, string siteNo, string posTerminal, bool isLog = true);

    /// <summary>POST api/sap/UpdateReturnVoucher — cập nhật trạng thái BNMH (ROP hoặc SAP SOAP return).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> UpdateReturnVoucherAsync(
        List<VoucherUpdateRequest> model);

    /// <summary>POST api/sap/CreateReturnVoucher — tạo mới BNTT (ROP hoặc SAP SOAP return).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> CreateReturnVoucherAsync(
        List<CreateVoucherModel> model);

    /// <summary>GET api/sap/EVoucher — lấy danh sách e-voucher theo CSN (SAP SOAP).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> EVoucherAsync(
        string csn, string storeNo, string posTerminal, bool isLog = true);

    /// <summary>GET api/sap/SendCodeCpnVch — cấp mã voucher/coupon cho đơn hàng.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> SendCodeCpnVchAsync(
        string storeNo, string posID, string bussinessDate,
        string orderNo, string itemNo, string phoneNumber = "");

    /// <summary>GET api/sap/retry — retry UpdateRedeemCpnVchCodeSends.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> RetryAsync();
}
