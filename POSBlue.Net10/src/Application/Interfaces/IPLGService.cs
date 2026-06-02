using System.Net;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Use cases cho PLGController (api/plg/*) — partner voucher (UrBox, GiftBox, GotIt, Giftee, Odoo SAP, CX).
/// </summary>
public interface IPLGService
{
    /// <summary>POST api/plg/GetInfoCard — kiểm tra voucher card (PLG/UrBox/GiftBox/GotIt...).</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GetInfoCardAsync(PlCheckVoucherRequest model);

    /// <summary>PUT api/plg/SaleVoucher — bán voucher card.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> SaleVoucherAsync(SaleVoucherRequest model);

    /// <summary>POST api/plg/RedeemVoucher — sử dụng voucher.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> RedeemVoucherAsync(RedeemVoucherRequest model);

    /// <summary>POST api/plg/Check_VC_Odoo — kiểm tra voucher trên Odoo SAP.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> CheckVcOdooAsync(PlCheckVoucherRequest model);

    /// <summary>POST api/plg/OdooSAP_Update_Voucher — cập nhật trạng thái voucher trên Odoo SAP.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> OdooSapUpdateVoucherAsync(OdooUpdateVoucherRequest model);

    /// <summary>POST api/plg/UrBoxCheck — kiểm tra voucher UrBox.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> UrBoxCheckAsync(PlCheckVoucherRequest model);

    /// <summary>POST api/plg/UrBox_Payment — thanh toán voucher UrBox.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> UrBoxPaymentAsync(RedeemVoucherRequest model);

    /// <summary>POST api/plg/GiftBox_mutilVoucherStatus — kiểm tra nhiều voucher GiftBox.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GiftBoxMultiVoucherStatusAsync(PlCheckVoucherRequest model);

    /// <summary>POST api/plg/GiftBox_voucherStatus — kiểm tra 1 voucher GiftBox.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GiftBoxVoucherStatusAsync(GiftBoxVoucherStatusRequest model);

    /// <summary>POST api/plg/GiftBox_voucherUsed — sử dụng voucher GiftBox.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GiftBoxVoucherUsedAsync(RedeemVoucherRequest model);

    /// <summary>POST api/plg/Giftee_statusTickets — kiểm tra vé Giftee.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GifteeStatusTicketsAsync(GifteeStatusRequest model);

    /// <summary>POST api/plg/GotItCheck — kiểm tra voucher GotIt.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GotItCheckAsync(GotItCheckPosRequest model);

    /// <summary>POST api/plg/GotIt_MultilCheckVoucher — kiểm tra nhiều voucher GotIt.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GotItMultiCheckAsync(PlCheckVoucherRequest model);

    /// <summary>POST api/plg/GotItUsed — sử dụng voucher GotIt.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GotItUsedAsync(RedeemVoucherRequest model);

    /// <summary>POST api/plg/Sale — tích/tiêu điểm PLG qua CX.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> SaleAsync(PlgSaleCxRequest model);

    /// <summary>GET api/plg/GetInfoMember — lấy thông tin thành viên PLG qua CX.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GetInfoMemberAsync(
        string numberCard, string storeNo, string posID);
}
