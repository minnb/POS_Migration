using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Repository cho các thao tác DB của ValidateController:
/// kiểm tra hóa đơn, tạo yêu cầu xuất HĐGTGT, truy vấn HVDN.
/// </summary>
public interface IValidateRepository
{
    /// <summary>Truy vấn dữ liệu giao dịch từ DB cửa hàng để kiểm tra xuất HĐGTGT.</summary>
    Task<(ValidateTransactionDto? Header, List<ValidateTransactionLine> Lines,
          CashOrderSentToEInvoice? CashSent, InvoiceCreatedItem? ExistingTemp)>
        GetTransactionDataAsync(string connStr, string orderNo);

    /// <summary>Ghi danh sách hóa đơn vào EInvoice.dbo.InvoiceCreated_Temp.</summary>
    Task<(bool Success, string Message, string? Technical)> InsertInvoiceCreatedAsync(
        List<InvoiceCreatedItem> items, string connStr);

    /// <summary>Lấy thông tin hội viên doanh nghiệp (HVDN) từ bảng MemberBusiness.</summary>
    Task<MemberBusinessDbDto?> GetMemberBusinessAsync(string memberCard, string connStr);

    /// <summary>Lấy danh sách hạn mức mặt hàng HVDN từ MemberItemDiscount + MemberRemnItem.</summary>
    Task<List<MemberBusinessItemDb>> GetMemberBusinessItemsAsync(string memberCard, string month, string connStr);
}
