using System.Data;
using System.Globalization;
using Dapper;
using POS.Common.Dtos.Promotion;
using POS.Infrastructure.Database;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// Special Combo (11.2) — DB RPOSMasterData (CentralMD). Reads qua SP; write replace-on-save (SP + TVP, transaction).
/// </summary>
public sealed class SpecialComboRepository(CentralMDConnectionFactory connectionFactory)
    : BaseRepository(connectionFactory), ISpecialComboRepository
{
    public async Task<(List<SpecialComboListItemDto> Items, int Total)> GetListAsync(
        SpecialComboListFilter filter, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var items = (await conn.QueryAsync<SpecialComboListItemDto>(new CommandDefinition(
            "dbo.usp_SpecialCombo_GetList",
            new
            {
                StoreNo    = (filter.StoreNo ?? string.Empty).Trim(),
                SalesType  = (filter.SalesType ?? string.Empty).Trim(),
                MemberType = (filter.MemberType ?? string.Empty).Trim(),
                Status     = string.IsNullOrWhiteSpace(filter.Status) ? "-1" : filter.Status.Trim(),
                TextSearch = (filter.TextSearch ?? string.Empty).Trim(),
                PageSize   = Math.Max(1, filter.PageSize),
                PageNumber = Math.Max(0, filter.PageNumber)
            },
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct))).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<SpecialComboDetailDto?> GetDetailAsync(string code, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(
            "dbo.usp_SpecialCombo_GetDetail", new { Code = code },
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));

        var hr = await multi.ReadFirstOrDefaultAsync<HeaderRow>();
        if (hr == null) return null;

        var lines = (await multi.ReadAsync<SpecialComboLineDto>()).ToList();
        var stores = (await multi.ReadAsync<SpecialComboStoreDto>()).ToList();

        return new SpecialComboDetailDto
        {
            Header = new SpecialComboHeaderDto
            {
                Code = hr.Code,
                SalesType = hr.SalesType,
                Name = hr.Name,
                ComboQuantity = hr.ComboQuantity,
                Amount = hr.Amount,
                IsMember = hr.IsMember,
                MemberCode = hr.MemberCode,
                FromDate = hr.FromDate?.ToString("dd/MM/yyyy") ?? string.Empty,
                ToDate = hr.ToDate?.ToString("dd/MM/yyyy") ?? string.Empty,
                IsEnable = hr.IsEnable
            },
            Lines = lines,
            Stores = stores
        };
    }

    public async Task<(bool Ok, string Message, string Code)> SaveAsync(
        SpecialComboSaveRequest request, string actor, CancellationToken ct = default)
    {
        var h = request.Header;

        // ── Validate ──
        if (string.IsNullOrWhiteSpace(h.Name))
            return (false, "Vui lòng nhập tên combo", string.Empty);
        if (request.Lines.Count == 0)
            return (false, "Combo phải có ít nhất 1 sản phẩm", string.Empty);
        if (request.Lines.Count(l => l.PriceMode == 2) > 1)
            return (false, "Mỗi combo chỉ được tối đa 1 sản phẩm có giá bán động", string.Empty);
        DateTime? fromDate = ParseDmy(h.FromDate);
        DateTime? toDate = ParseDmy(h.ToDate);
        if (fromDate == null || toDate == null)
            return (false, "Ngày bắt đầu / kết thúc không đúng định dạng (dd/MM/yyyy)", string.Empty);
        if (toDate < fromDate)
            return (false, "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu", string.Empty);

        // Auto-gen Code khi tạo mới: S{yyyyMMddHHmmss}
        var code = string.IsNullOrWhiteSpace(h.Code) ? $"S{DateTime.Now:yyyyMMddHHmmss}" : h.Code.Trim();

        var lineTable = BuildLineTable(request.Lines);
        var storeTable = BuildStoreTable(request.Stores);

        var p = new DynamicParameters();
        p.Add("@Code", code);
        p.Add("@SalesType", h.SalesType ?? string.Empty);
        p.Add("@Name", h.Name.Trim());
        p.Add("@ComboQuantity", h.ComboQuantity);
        p.Add("@Amount", h.Amount);
        p.Add("@IsMember", h.IsMember);
        p.Add("@MemberCode", h.MemberCode ?? string.Empty);
        p.Add("@FromDate", fromDate);
        p.Add("@ToDate", toDate);
        p.Add("@IsEnable", h.IsEnable);
        p.Add("@Actor", actor ?? string.Empty);
        p.Add("@Lines", lineTable.AsTableValuedParameter("dbo.SpecialComboLineTVP"));
        p.Add("@Stores", storeTable.AsTableValuedParameter("dbo.SpecialComboStoreTVP"));

        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition("dbo.usp_SpecialCombo_Save", p,
                commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));
            return (true, $"Lưu combo {code} thành công", code);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, string.Empty);
        }
    }

    public async Task<bool> UpdateStatusAsync(string code, bool isEnable, string actor, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            var rows = await conn.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SpecialCombo_UpdateStatus", new { Code = code, IsEnable = isEnable, Actor = actor ?? string.Empty },
                commandType: CommandType.StoredProcedure, commandTimeout: 60, cancellationToken: ct));
            return rows > 0;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteAsync(string code, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SpecialCombo_Delete", new { Code = code },
                commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));
            return true;
        }
        catch { return false; }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static DataTable BuildLineTable(List<SpecialComboLineDto> rows)
    {
        var t = new DataTable();
        t.Columns.Add("GroupCode", typeof(string));
        t.Columns.Add("GroupName", typeof(string));
        t.Columns.Add("ItemNo", typeof(string));
        t.Columns.Add("ItemName", typeof(string));
        t.Columns.Add("UOM", typeof(string));
        t.Columns.Add("MinimumQuantity", typeof(decimal));
        t.Columns.Add("MaximumQuantity", typeof(decimal));
        t.Columns.Add("Order", typeof(int));
        t.Columns.Add("PriceMode", typeof(int));
        t.Columns.Add("IsRequired", typeof(bool));
        t.Columns.Add("IsSendSAP", typeof(bool));
        foreach (var r in rows)
            t.Rows.Add(r.GroupCode ?? "", r.GroupName ?? "", r.ItemNo ?? "", r.ItemName ?? "", r.UOM ?? "",
                       r.MinimumQuantity, r.MaximumQuantity, r.Order, r.PriceMode, r.IsRequired, r.IsSendSAP);
        return t;
    }

    private static DataTable BuildStoreTable(List<SpecialComboStoreDto> rows)
    {
        var t = new DataTable();
        t.Columns.Add("StoreNo", typeof(string));
        t.Columns.Add("IsEnable", typeof(bool));
        foreach (var r in rows.Where(s => !string.IsNullOrWhiteSpace(s.StoreNo)))
            t.Rows.Add(r.StoreNo, r.IsEnable);
        return t;
    }

    private static DateTime? ParseDmy(string dmy)
        => DateTime.TryParseExact(dmy, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d : null;

    private sealed class HeaderRow
    {
        public string Code { get; set; } = string.Empty;
        public string SalesType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal ComboQuantity { get; set; }
        public decimal Amount { get; set; }
        public bool IsMember { get; set; }
        public string MemberCode { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool IsEnable { get; set; }
    }
}
