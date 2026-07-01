using System.Globalization;
using Dapper;
using POS.Common.Dtos.Voucher;
using POS.Infrastructure.Database;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// 8.4 Tra cứu Voucher Phát hành — CentralSales (routed per-store qua StoreRoutedConnectionFactory,
/// mirror CentralSaleRepository). Port từ VCM.BLUEPOS VoucherData.GetVoucherPublishedList /
/// ExportExcelVoucherPublishedList. Tái dùng SP [dbo].[GetTransCpnVchIssueList].
/// </summary>
public sealed class VoucherPublishedRepository(
    StoreRoutedConnectionFactory connectionFactory) : IVoucherPublishedRepository
{
    private const int Timeout = 120;
    private const string Sql =
        "[dbo].[GetTransCpnVchIssueList] @FromDate,@ToDate,@StoreNo,@VoucherType,@TextSearch,@StatusVoucher,@SendSAP,@Export,@PageSize,@PageNumber";

    public async Task<(List<VoucherPublishedItemDto> Items, int Total)> GetListAsync(
        VoucherPublishedFilter filter, CancellationToken ct = default)
    {
        var rows = await QueryAsync(filter, export: 1,
            pageSize: Math.Max(1, filter.PageSize), pageNumber: Math.Max(0, filter.PageNumber), ct);
        var total = rows.Count > 0 ? rows[0].Total : 0;
        return (rows, total);
    }

    public async Task<List<VoucherPublishedItemDto>> ExportAsync(
        VoucherPublishedFilter filter, CancellationToken ct = default)
        => await QueryAsync(filter, export: 2, pageSize: 999999, pageNumber: 0, ct);

    private async Task<List<VoucherPublishedItemDto>> QueryAsync(
        VoucherPublishedFilter filter, int export, int pageSize, int pageNumber, CancellationToken ct)
    {
        // Store bắt buộc để route đúng server CentralSales.
        var storeNo = (filter.StoreNo ?? string.Empty).Trim();
        using var conn = await connectionFactory.CreateOpenConnectionAsync(storeNo, ct: ct);

        var items = await conn.QueryAsync<VoucherPublishedItemDto>(new CommandDefinition(Sql, new
        {
            FromDate      = ParseFrom(filter.FromDate),
            ToDate        = ParseTo(filter.ToDate),
            StoreNo       = storeNo,
            VoucherType   = (filter.VoucherType ?? string.Empty).Trim(),
            TextSearch    = (filter.TextSearch ?? string.Empty).Trim(),
            StatusVoucher = (filter.StatusVoucher ?? string.Empty).Trim(),
            SendSAP       = (filter.SendSAP ?? string.Empty).Trim(),
            Export        = export,
            PageSize      = pageSize,
            PageNumber    = pageNumber
        }, commandTimeout: Timeout, cancellationToken: ct));

        return items.ToList();
    }

    // From = đầu ngày; To = cuối ngày (23:59:59) để bao trọn giao dịch trong ngày chọn.
    private static DateTime ParseFrom(string? dmy)
        => TryParse(dmy, out var d) ? d.Date : DateTime.Today;

    private static DateTime ParseTo(string? dmy)
        => (TryParse(dmy, out var d) ? d.Date : DateTime.Today).AddDays(1).AddSeconds(-1);

    private static bool TryParse(string? dmy, out DateTime d)
        => DateTime.TryParseExact(dmy, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
}
