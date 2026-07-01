using POS.Common.Dtos.Voucher;

namespace POS.Application.Features.CouponVoucher;

/// <summary>8.4 Tra cứu Voucher Phát hành — thin wrapper xuống IVoucherPublishedRepository (CentralSales).</summary>
public interface IVoucherPublishedService
{
    Task<(List<VoucherPublishedItemDto> Items, int Total)> GetListAsync(
        VoucherPublishedFilter filter, CancellationToken ct = default);

    Task<List<VoucherPublishedItemDto>> ExportAsync(
        VoucherPublishedFilter filter, CancellationToken ct = default);
}
