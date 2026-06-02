using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Abstraction cho 2 SAP SOAP services:
/// SI_VoucherSerialNo_OUT (voucher mới ZVCN/ZCPN) và VoucherSerialNoService (return voucher BNMH/BNTT).
/// Implementation gọi WCF client được sinh từ WSDL (pending dotnet-svcutil regen).
/// ProcessingType: "A"=create, "C"=check, "U"=update.
/// </summary>
public interface ISapVoucherClient
{
    /// <summary>Kiểm tra/tạo/cập nhật voucher ZVCN/ZCPN — gọi SI_VoucherSerialNo_OUT.</summary>
    Task<List<SapVoucherOutput>> ProcessVouchersAsync(List<SapVoucherInput> inputs);

    /// <summary>Lấy danh sách e-voucher theo CSN — gọi SI_VoucherCoupon_Request.</summary>
    Task<List<SapEVoucherOutput>> GetEVouchersAsync(List<SapEVoucherInput> inputs);

    /// <summary>Kiểm tra/tạo/cập nhật voucher trả hàng (BNMH/BNTT, VoucherType=R) — gọi VoucherSerialNoService.</summary>
    Task<List<SapVoucherOutput>> ProcessReturnVouchersAsync(List<SapVoucherInput> inputs);
}
