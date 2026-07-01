using POS.Common.Dtos.Voucher;

namespace POS.Infrastructure.Repositories.Interfaces;

/// <summary>
/// 8.4 Tra cứu Voucher Phát hành — DB CentralSales (routed per-store).
/// Tái dùng SP có sẵn [dbo].[GetTransCpnVchIssueList] (@Export=1 paging, =2 export).
/// </summary>
public interface IVoucherPublishedRepository
{
    /// <summary>Tra cứu phân trang (@Export=1). StoreNo bắt buộc (routing).</summary>
    Task<(List<VoucherPublishedItemDto> Items, int Total)> GetListAsync(
        VoucherPublishedFilter filter, CancellationToken ct = default);

    /// <summary>Toàn bộ theo filter (@Export=2) cho xuất Excel.</summary>
    Task<List<VoucherPublishedItemDto>> ExportAsync(
        VoucherPublishedFilter filter, CancellationToken ct = default);
}
