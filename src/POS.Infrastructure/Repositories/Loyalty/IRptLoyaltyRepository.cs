using POS.Common.Dtos.RptLoyalty;

namespace POS.Infrastructure.Repositories.Interfaces;

/// <summary>
/// Repository dành riêng cho báo cáo POS.Web trên Loyalty DB (LoggingLoyalty).
/// Tách khỏi ILoyaltyRepository (dùng cho ghi log tích/tiêu điểm realtime) để tránh coupling,
/// giống cách IRptCentralSaleRepository tách khỏi ICentralSaleRepository.
/// </summary>
public interface IRptLoyaltyRepository
{
    // ── Danh sách hóa đơn tích/tiêu điểm hội viên (CỬA HÀNG → Giao dịch → Hội viên) ──
    // storeNo/memberCardNo/orderNo/actionType/transactionType: null = bỏ qua điều kiện lọc.
    // pageNumber: 0-based (theo MudTable TableState.Page). Total trong mỗi row = tổng số dòng khớp filter.
    Task<List<InvoiceLoyaltyDto>> GetInvoiceLoyaltyListAsync(
        string? storeNo, DateTime fromDate, DateTime toDate,
        string? memberCardNo, string? orderNo, string? actionType, int? transactionType,
        int pageSize, int pageNumber,
        CancellationToken ct = default);
}
