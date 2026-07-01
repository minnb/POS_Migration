using System.Data;
using System.Globalization;
using Dapper;
using POS.Common.Dtos.Voucher;
using POS.Infrastructure.Database;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// 8.3 Danh mục Voucher — DB RPOSMasterData (CentralMD). Port từ VCM.BLUEPOS VoucherData.
/// Đọc/ghi qua SP usp_SetupVoucher_* (docs/sql/SetupVoucher_*.sql).
/// </summary>
public sealed class VoucherRepository(CentralMDConnectionFactory connectionFactory)
    : BaseRepository(connectionFactory), IVoucherRepository
{
    public async Task<(List<VoucherListItemDto> Items, int Total)> GetListAsync(
        VoucherListFilter filter, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var items = (await conn.QueryAsync<VoucherListItemDto>(new CommandDefinition(
            "dbo.usp_SetupVoucher_GetList",
            new
            {
                ItemNo      = (filter.ItemNo ?? string.Empty).Trim(),
                ItemName    = (filter.ItemName ?? string.Empty).Trim(),
                Serial      = (filter.Serial ?? string.Empty).Trim(),
                ArticleType = (filter.ArticleType ?? string.Empty).Trim(),
                Status      = string.IsNullOrWhiteSpace(filter.Status) ? "-1" : filter.Status.Trim(),
                PageSize    = Math.Max(1, filter.PageSize),
                PageNumber  = Math.Max(0, filter.PageNumber)
            },
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct))).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<VoucherDetailDto?> GetDetailAsync(string itemNo, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(
            "dbo.usp_SetupVoucher_GetDetail", new { ItemNo = itemNo },
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));

        var header = await multi.ReadFirstOrDefaultAsync<VoucherDetailDto>();
        if (header == null) return null;

        header.Items = (await multi.ReadAsync<VoucherLineDto>()).ToList();
        return header;
    }

    public async Task<VoucherSaveResult> SaveAsync(
        VoucherSaveRequest request, string actor, CancellationToken ct = default)
    {
        var start = ParseDmy(request.StartingDateStr);
        var end = ParseDmy(request.EndingDateStr);

        var p = new DynamicParameters();
        p.Add("@ItemNo", (request.ItemNo ?? string.Empty).Trim());
        p.Add("@Serial", (request.Serial ?? string.Empty).Trim());
        p.Add("@ItemName", request.ItemName ?? string.Empty);
        p.Add("@ArticleType", request.ArticleType ?? string.Empty);
        p.Add("@UnitOfMeasure", request.UnitOfMeasure ?? string.Empty);
        p.Add("@DiscountType", request.DiscountType);
        p.Add("@DiscountValue", request.DiscountValue);
        p.Add("@ValueOfVoucher", request.ValueOfVoucher);
        p.Add("@MaxAmount", request.MaxAmount);
        p.Add("@LimitQty", request.LimitQty);
        p.Add("@IsCheckItem", request.IsCheckItem);
        p.Add("@Blocked", request.Blocked);
        p.Add("@StartingDate", start);
        p.Add("@EndingDate", end);
        p.Add("@Lines", BuildLineTable(request.Items).AsTableValuedParameter("dbo.VoucherLineTVP"));
        p.Add("@Actor", actor ?? string.Empty);

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<VoucherSaveResult>(new CommandDefinition(
            "dbo.usp_SetupVoucher_Save", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));
        return row ?? new VoucherSaveResult { Ok = false, Message = "Lưu voucher thất bại" };
    }

    public async Task<(bool Deleted, string Message)> DeleteAsync(string itemNo, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<DeleteRow>(new CommandDefinition(
            "dbo.usp_SetupVoucher_Delete", new { ItemNo = itemNo },
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));
        return row is null ? (false, "Không xóa được voucher") : (row.Deleted, row.Message ?? string.Empty);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private static DataTable BuildLineTable(IEnumerable<VoucherLineDto> rows)
    {
        var t = new DataTable();
        t.Columns.Add("LineItemNo", typeof(string));
        t.Columns.Add("Description", typeof(string));
        t.Columns.Add("UnitOfMeasure", typeof(string));
        t.Columns.Add("Barcode", typeof(string));
        foreach (var r in rows.Where(x => !string.IsNullOrWhiteSpace(x.ItemNo)))
            t.Rows.Add(r.ItemNo, r.ItemName ?? "", r.UOM ?? "", r.Barcode ?? "");
        return t;
    }

    private static DateTime ParseDmy(string? dmy)
        => DateTime.TryParseExact(dmy, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d : new DateTime(1900, 1, 1);

    private sealed class DeleteRow
    {
        public bool Deleted { get; set; }
        public string? Message { get; set; }
    }
}
