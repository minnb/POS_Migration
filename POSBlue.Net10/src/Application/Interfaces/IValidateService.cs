using System.Net;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Tập hợp toàn bộ use case của ValidateController.
/// </summary>
public interface IValidateService
{
    /// <summary>POST api/v2/telegram/send — gửi cảnh báo qua Telegram/SMS queue.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> TelegramSendAsync(MessageTelegramRequest request);

    /// <summary>GET api/v2/validate/tax-code — tra cứu MST qua PARTNER WebApi.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> ValidateTaxCodeAsync(string taxCode);

    /// <summary>GET api/v2/validate/transaction — kiểm tra phiếu tính tiền trước khi xuất HĐGTGT.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> ValidateTransactionAsync(string orderNo);

    /// <summary>POST api/v2/invoice/create — tạo yêu cầu xuất HĐGTGT.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> InvoiceCreateAsync(InvoiceCreatedRequest request);

    /// <summary>POST api/v2/validate/member-business — kiểm tra hội viên doanh nghiệp (HVDN) khi thanh toán.</summary>
    Task<(HttpStatusCode Status, string Message, object? Data)> ValidateMemberBusinessAsync(ValidateMemberBusinessRequest request);
}
