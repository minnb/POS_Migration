using System.Data;
using System.Globalization;
using Dapper;
using POS.Common.Dtos.SetupCoupon;
using POS.Infrastructure.Database;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// 8.1/8.2 Setup Coupon — DB RPOSMasterData (CentralMD). Port từ VCM.BLUEPOS SetupCouponData (EF).
/// Đọc/ghi qua SP usp_SetupCoupon_* (script docs/sql/SetupCoupon_*.sql). Sinh mã Auto ở tầng Application.
/// </summary>
public sealed class CouponRepository(CentralMDConnectionFactory connectionFactory)
    : BaseRepository(connectionFactory), ICouponRepository
{
    public async Task<(List<CouponListItemDto> Items, int Total)> GetListAsync(
        CouponListFilter filter, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var items = (await conn.QueryAsync<CouponListItemDto>(new CommandDefinition(
            "dbo.usp_SetupCoupon_GetList",
            new
            {
                IssueType   = (filter.IssueType ?? string.Empty).Trim(),
                ItemNo      = (filter.ItemNo ?? string.Empty).Trim(),
                Description = (filter.Description ?? string.Empty).Trim(),
                Status      = string.IsNullOrWhiteSpace(filter.Status) ? "-1" : filter.Status.Trim(),
                PageSize    = Math.Max(1, filter.PageSize),
                PageNumber  = Math.Max(0, filter.PageNumber)
            },
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct))).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<(List<CouponHeaderListItemDto> Items, int Total)> GetHeaderListAsync(
        CouponHeaderListFilter filter, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var items = (await conn.QueryAsync<CouponHeaderListItemDto>(new CommandDefinition(
            "dbo.usp_CpnVchBOMHeader_GetList",
            new
            {
                KeyWord    = (filter.KeyWord ?? string.Empty).Trim(),
                Type       = (filter.ArticleType ?? string.Empty).Trim(),
                Status     = string.IsNullOrWhiteSpace(filter.Status) ? "-1" : filter.Status.Trim(),
                PageSize   = Math.Max(1, filter.PageSize),
                PageNumber = Math.Max(0, filter.PageNumber)
            },
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct))).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<(List<CouponCodeDto> Items, int Total)> GetCodesAsync(
        CouponCodeFilter filter, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var items = (await conn.QueryAsync<CouponCodeDto>(new CommandDefinition(
            "dbo.usp_SetupCoupon_GetCodes",
            new
            {
                ItemNo     = (filter.ItemNo ?? string.Empty).Trim(),
                PageSize   = Math.Max(1, filter.PageSize),
                PageNumber = Math.Max(0, filter.PageNumber)
            },
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct))).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<CouponDetailDto?> GetDetailAsync(string itemNo, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(
            "dbo.usp_SetupCoupon_GetDetail", new { ItemNo = itemNo },
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));

        var header = await multi.ReadFirstOrDefaultAsync<CouponDetailDto>();
        if (header == null) return null;

        header.Items = (await multi.ReadAsync<CouponItemLineDto>()).ToList();
        return header;
    }

    public async Task<CouponFormLookupDto> GetFormLookupAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(@"
            SELECT Code AS Value, ISNULL(Description, Code) AS Text
            FROM   dbo.SalesOrderType (NOLOCK)
            WHERE  ISNULL(IsActive, 0) = 1
            ORDER BY Description;

            SELECT DISTINCT GroupCode
            FROM   dbo.StoreGroup (NOLOCK)
            WHERE  ISNULL([Status], 0) = 1 AND ISNULL(GroupCode, '') <> ''
            ORDER BY GroupCode;

            SELECT Code AS Value, ISNULL(Description, Code) AS Text
            FROM   dbo.UnitOfMeasure (NOLOCK)
            ORDER BY Code;",
            commandTimeout: 60, cancellationToken: ct));

        var salesTypes  = (await multi.ReadAsync<CouponOptionDto>()).ToList();
        var storeGroups = (await multi.ReadAsync<string>()).ToList();
        var uoms        = (await multi.ReadAsync<CouponOptionDto>()).ToList();

        return new CouponFormLookupDto { SalesTypes = salesTypes, StoreGroups = storeGroups, Uoms = uoms };
    }

    public async Task<List<string>> CheckCodesExistAsync(IEnumerable<string> codes, CancellationToken ct = default)
    {
        var table = BuildCodeTable(codes);
        if (table.Rows.Count == 0) return [];

        var p = new DynamicParameters();
        p.Add("@Codes", table.AsTableValuedParameter("dbo.CouponCodeTVP"));

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var exist = await conn.QueryAsync<string>(new CommandDefinition(
            "dbo.usp_SetupCoupon_CheckCodesExist", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));
        return exist.ToList();
    }

    public async Task<string> SaveIssueAsync(
        CouponIssueSaveRequest request, IReadOnlyList<string> codes, CancellationToken ct = default)
    {
        var start = ParseDmy(request.StartingDateStr);
        var end = ParseDmy(request.EndingDateStr);

        var p = new DynamicParameters();
        p.Add("@ItemNo", (request.ItemNo ?? string.Empty).Trim());
        p.Add("@Description", request.Description ?? string.Empty);
        p.Add("@ArticleType", string.IsNullOrWhiteSpace(request.ArticleType) ? "ZCPN" : request.ArticleType);
        p.Add("@SaleType", request.SaleType ?? string.Empty);
        p.Add("@StoreGroupCode", request.StoreGroupCode ?? string.Empty);
        p.Add("@StartingDate", start);
        p.Add("@EndingDate", end);
        p.Add("@Prefix", request.Prefix ?? string.Empty);
        p.Add("@LenCode", request.LenCode);
        p.Add("@IssueType", request.IssueType ?? string.Empty);
        p.Add("@CharOfNumber", request.CharOfNumber);
        p.Add("@CharPosition", request.CharPosition);
        p.Add("@IsCheckItem", request.IsCheckItem);
        p.Add("@Codes", BuildCodeTable(codes).AsTableValuedParameter("dbo.CouponCodeTVP"));
        p.Add("@Lines", BuildLineTable(request.Items).AsTableValuedParameter("dbo.CouponLineTVP"));
        p.Add("@OutItemNo", dbType: DbType.String, direction: ParameterDirection.Output, size: 20);

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("dbo.usp_SetupCoupon_SaveIssue", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 300, cancellationToken: ct));
        return p.Get<string>("@OutItemNo") ?? string.Empty;
    }

    public async Task<string> SaveAdvancedAsync(CouponAdvancedSaveRequest request, CancellationToken ct = default)
    {
        var start = ParseDmy(request.StartingDateStr);
        var end = ParseDmy(request.EndingDateStr);

        var p = new DynamicParameters();
        p.Add("@ItemNo", (request.ItemNo ?? string.Empty).Trim());
        p.Add("@Description", request.Description ?? string.Empty);
        p.Add("@ArticleType", string.IsNullOrWhiteSpace(request.ArticleType) ? "ZCPN" : request.ArticleType);
        p.Add("@UOM", request.UOM ?? string.Empty);
        p.Add("@IssueType", request.IssueType ?? string.Empty);
        p.Add("@CpnVchType", request.CpnVchType ?? string.Empty);
        p.Add("@DiscountType", request.DiscountType);
        p.Add("@DiscountValue", (decimal)request.DiscountValue);
        p.Add("@MaxAmount", (decimal)request.MaxValue);
        p.Add("@StartingDate", start);
        p.Add("@EndingDate", end);
        p.Add("@Blocked", request.Blocked);
        p.Add("@IsMultiUse", request.IsMultiUsed);
        p.Add("@IsCheckAPI", request.IsCheckAPI);
        p.Add("@LimitQty", request.LimitQty);
        p.Add("@LimitQtyUsed", request.LimitQtyUsed);
        p.Add("@SaleType", request.SaleType ?? string.Empty);
        p.Add("@StoreGroupCode", request.StoreGroupCode ?? string.Empty);
        p.Add("@OutItemNo", dbType: DbType.String, direction: ParameterDirection.Output, size: 20);

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("dbo.usp_SetupCoupon_SaveAdvanced", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));
        return p.Get<string>("@OutItemNo") ?? string.Empty;
    }

    public async Task<(bool Deleted, string Message)> DeleteAsync(string itemNo, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<DeleteRow>(new CommandDefinition(
            "dbo.usp_SetupCoupon_Delete", new { ItemNo = itemNo },
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));
        return row is null ? (false, "Không xóa được coupon") : (row.Deleted, row.Message ?? string.Empty);
    }

    public async Task<int> IssueMoreAsync(string itemNo, IReadOnlyList<string> codes, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@ItemNo", (itemNo ?? string.Empty).Trim());
        p.Add("@Codes", BuildCodeTable(codes).AsTableValuedParameter("dbo.CouponCodeTVP"));
        p.Add("@OutQuantityAdded", dbType: DbType.Int32, direction: ParameterDirection.Output);

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("dbo.usp_SetupCoupon_IssueMore", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 300, cancellationToken: ct));
        return p.Get<int>("@OutQuantityAdded");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private static DataTable BuildCodeTable(IEnumerable<string> codes)
    {
        var t = new DataTable();
        t.Columns.Add("Code", typeof(string));
        foreach (var c in codes)
            if (!string.IsNullOrWhiteSpace(c))
                t.Rows.Add(c.Trim());
        return t;
    }

    private static DataTable BuildLineTable(IEnumerable<CouponItemLineDto> rows)
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
