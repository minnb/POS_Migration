using POS.Common.Dtos.Voucher;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Application.Features.CouponVoucher;

/// <summary>8.4 — Thin wrapper: delegate xuống IVoucherPublishedRepository (SP CentralSales per-store).</summary>
public sealed class VoucherPublishedService(IVoucherPublishedRepository repository) : IVoucherPublishedService
{
    public Task<(List<VoucherPublishedItemDto> Items, int Total)> GetListAsync(
        VoucherPublishedFilter filter, CancellationToken ct = default)
        => repository.GetListAsync(filter, ct);

    public Task<List<VoucherPublishedItemDto>> ExportAsync(
        VoucherPublishedFilter filter, CancellationToken ct = default)
        => repository.ExportAsync(filter, ct);
}
