using System.Net;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Tập hợp toàn bộ use case của LoyaltyController (cả Capillary và VINID).
/// Controller chỉ gọi interface này — không biết đến Infrastructure.
/// </summary>
public interface ILoyaltyService
{
    // ── Capillary Loyalty (api/v2/loyalty/*) ──────────────────────────────────

    /// <summary>GET api/v2/loyalty/retry — retry các giao dịch lỗi pending</summary>
    Task LoyaltyRetryAsync();

    /// <summary>GET api/v2/loyalty/customer/get — lấy thông tin hội viên (Capillary phone hoặc VINID card)</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> GetCustomerDetailAsync(
        string numberCard, string posId, string storeNo,
        string clubCode = "", bool isMobile = false, bool isLog = true);

    /// <summary>POST api/v2/loyalty/customer — đăng ký hội viên mới qua CX OTP</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> CustomerRegistrationAsync(
        CustomerRegisterRequest request, string channel);

    /// <summary>POST api/v2/loyalty/customer/update — cập nhật thông tin hội viên</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> CustomerUpdateAsync(
        CustomerUpdateRequest request, string channel);

    /// <summary>POST api/v2/loyalty/transaction/add — tích/tiêu điểm Capillary hoặc VINID</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> AddTransactionAsync(VinIdSalesRequest model);

    /// <summary>POST api/v2/loyalty/transaction/refund — trả hàng Capillary hoặc VINID</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> RefundTransactionAsync(VinIdRefundRequest model);

    /// <summary>POST api/v2/loyalty/other-status — cập nhật trạng thái khác của hội viên</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> OtherStatusUpdateAsync(OtherStatusUpdateRequest request);

    // ── VINID Loyalty (api/vinid/*) ───────────────────────────────────────────

    /// <summary>GET api/vinid/GetInfoMember — lấy thông tin hội viên VINID/Capillary (route cũ)</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> VinIdGetInfoMemberAsync(
        string numberCard, string posId, string storeNo,
        string clubCode = "", bool isLog = true);

    /// <summary>GET api/vinid/Sales — tích/tiêu điểm VINID hoặc CX (route cũ GET)</summary>
    (HttpStatusCode Status, string Message, object? Data) VinIdSales(
        string qrCode, string cardNumber, string merchantId, string terminalId,
        string invoiceNo, int spendPoints, decimal billAmount,
        string orderNo, bool isOffline, decimal orderAmount,
        string virtualCard = "");

    /// <summary>POST api/vinid/SalesV2 — tích/tiêu điểm VINID/CX v2 (request body)</summary>
    (HttpStatusCode Status, string Message, object? Data) VinIdSalesV2(VinIdSalesRequest model);

    /// <summary>GET api/vinid/Refund — trả hàng VINID/CX (route cũ GET)</summary>
    (HttpStatusCode Status, string Message, object? Data) VinIdRefund(
        string qrCode, string cardNumber, string merchantId, string terminalId,
        string invoiceNo, string orderNo, string origOrderNo,
        int spendPoints, decimal refundAmount, bool isOffline,
        decimal orderAmount, bool isScanAndGo = false);

    /// <summary>POST api/vinid/RefundV2 — trả hàng VINID/Capillary v2 (request body)</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> VinIdRefundV2Async(VinIdRefundRequest model);

    /// <summary>POST api/vinid/InitTransaction — khởi tạo thanh toán ví VINPAY</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> VinIdInitTransactionAsync(PosTransactionRequest request);

    /// <summary>GET api/vinid/GetTransaction — truy vấn trạng thái giao dịch VINPAY</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> VinIdGetTransactionAsync(string transactionId);

    /// <summary>PUT api/vinid/CancelTransaction — hủy giao dịch VINPAY</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> VinIdCancelTransactionAsync(PosCancelTransactionRequest request);

    /// <summary>GET api/vinid/ScanAndGo — lấy thông tin đơn hàng ScanAndGo</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> VinIdScanAndGoAsync(string barcode);

    /// <summary>PUT api/vinid/ScanAndGo/UpdateStatusOrder — cập nhật trạng thái đơn hàng ScanAndGo</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> VinIdUpdateStatusOrderAsync(ScanAndGoStatusOrderRequest request);

    /// <summary>POST api/vinid/ExtraSales — bán hàng extra (loyalty bổ sung)</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> VinIdExtraSalesAsync(ExtraSalesPosRequest request);

    /// <summary>POST api/vinid/ExtraRefund — trả hàng extra</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> VinIdExtraRefundAsync(ExtraRefundPosRequest request);

    /// <summary>POST api/vinid/ScanAndGo/SnGRefund — trả hàng ScanAndGo</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> VinIdSnGRefundAsync(ScanAndGoRefundRequest request);

    /// <summary>POST api/vinid/ScanAndGo/SnGTopup — nạp tiền/điểm ScanAndGo</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> VinIdSnGTopupAsync(ScanAndGoTopupRequest request);

    /// <summary>GET api/vinid/TransactionEnquiry — truy vấn lịch sử giao dịch SOAP</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> VinIdTransactionEnquiryAsync(
        string memberCard, string pageNo, string totalRecordPerPage,
        string startDate, string endDate);
}
