using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Infrastructure.Gateways.SAP;

/// <summary>
/// Stub cho SAP SOAP services:
/// - SI_VoucherSerialNo_OUTService (voucher ZVCN/ZCPN, Processing_Type A/C/U)
/// - VoucherSerialNoService (return voucher BNMH/BNTT, Voucher_Type R)
/// - SI_VoucherCoupon_Request (EVoucher by CSN)
///
/// TODO: Regenerate WCF client bằng dotnet-svcutil từ WSDL của SAP SOAP endpoint,
/// sau đó inject _siVoucherOut và _returnVoucher thực vào đây thay cho các stub.
/// </summary>
public sealed class SapVoucherSoapClient : ISapVoucherClient
{
    public Task<List<SapVoucherOutput>> ProcessVouchersAsync(List<SapVoucherInput> inputs)
    {
        // Trả lỗi rõ ràng thay vì trả danh sách rỗng — POS sẽ thấy thông báo cụ thể.
        return Task.FromResult(inputs.Select(x => new SapVoucherOutput
        {
            Return = "E_NOTIMPL",
            Status = "SAP SOAP chưa được cấu hình, vui lòng liên hệ IT",
        }).ToList());
    }

    public Task<List<SapEVoucherOutput>> GetEVouchersAsync(List<SapEVoucherInput> inputs)
    {
        return Task.FromResult(new List<SapEVoucherOutput>());
    }

    public Task<List<SapVoucherOutput>> ProcessReturnVouchersAsync(List<SapVoucherInput> inputs)
    {
        return Task.FromResult(inputs.Select(x => new SapVoucherOutput
        {
            Return = "E_NOTIMPL",
            Status = "SAP SOAP chưa được cấu hình, vui lòng liên hệ IT",
        }).ToList());
    }
}
