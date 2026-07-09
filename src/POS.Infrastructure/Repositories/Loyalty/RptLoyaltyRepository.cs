using POS.Common.Dtos.RptLoyalty;
using POS.Infrastructure.Database;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// Báo cáo POS.Web — truy vấn trực tiếp Loyalty DB (bảng LoggingLoyalty) qua LoyaltyConnectionFactory.
/// LƯU Ý: cột [StoreNo] chưa tồn tại trên LoggingLoyalty tại thời điểm viết code này (xem
/// docs/architecture/loyalty-schema.md, mục PENDING) — sẽ được bổ sung thủ công. Query giả định
/// cột này đã có; nếu chưa apply script, mọi lời gọi có storeNo != null sẽ ném lỗi SQL
/// "Invalid column name 'StoreNo'".
/// </summary>
public sealed class RptLoyaltyRepository(LoyaltyConnectionFactory connectionFactory)
    : BaseRepository(connectionFactory), IRptLoyaltyRepository
{
    private const int Timeout = 60;

    public async Task<List<InvoiceLoyaltyDto>> GetInvoiceLoyaltyListAsync(
        string? storeNo, DateTime fromDate, DateTime toDate,
        string? memberCardNo, string? orderNo, string? actionType, int? transactionType,
        int pageSize, int pageNumber,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT OrderNo, TransactionType, OrigOrderNo, MemberCardNo, StoreNo, ActionType,
                   CASE
                       WHEN ActionType IN ('EARN','REVERSE_REDEEM','REVERSE_EARN','TOPUP') THEN ABS(LoyaltyPoints)
                       WHEN ActionType = 'REDEEM' THEN -ABS(LoyaltyPoints)
                       ELSE LoyaltyPoints
                   END AS LoyaltyPoints,
                   CrtDate,
                   COUNT(*) OVER() AS Total
            FROM [dbo].[LoggingLoyalty] (NOLOCK)
            WHERE CrtDate >= @FromDate AND CrtDate < @ToDateExclusive
              AND (@StoreNo IS NULL OR StoreNo = @StoreNo)
              AND (@MemberCardNo IS NULL OR MemberCardNo LIKE '%' + @MemberCardNo + '%')
              AND (@OrderNo IS NULL OR OrderNo LIKE '%' + @OrderNo + '%')
              AND (@ActionType IS NULL OR ActionType = @ActionType)
              AND (@TransactionType IS NULL OR TransactionType = @TransactionType)
            ORDER BY CrtDate DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var finalPageSize = Math.Max(1, pageSize);
        var offset = Math.Max(0, pageNumber) * finalPageSize;

        var result = await QueryAsync<InvoiceLoyaltyDto>(sql, new
        {
            FromDate = fromDate.Date,
            ToDateExclusive = toDate.Date.AddDays(1),
            StoreNo = string.IsNullOrWhiteSpace(storeNo) ? null : storeNo.Trim(),
            MemberCardNo = string.IsNullOrWhiteSpace(memberCardNo) ? null : memberCardNo.Trim(),
            OrderNo = string.IsNullOrWhiteSpace(orderNo) ? null : orderNo.Trim(),
            ActionType = string.IsNullOrWhiteSpace(actionType) ? null : actionType.Trim(),
            TransactionType = transactionType,
            Offset = offset,
            PageSize = finalPageSize
        }, commandTimeout: Timeout, ct: ct);

        return result.ToList();
    }
}
