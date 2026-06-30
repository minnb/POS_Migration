using System.Data;
using System.Globalization;
using Dapper;
using Newtonsoft.Json;
using POS.Common.Dtos.Promotion;
using POS.Infrastructure.Database;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// Danh mục khuyến mãi (Offer Header) — DB RPOSMasterData (CentralMD).
/// SP server-side paging trả Total trên mỗi row (giống pattern CentralMDRepository.GetEmployeeListAsync).
/// </summary>
public sealed class PromotionRepository(
    CentralMDConnectionFactory connectionFactory,
    IRedisService redis)
    : BaseRepository(connectionFactory), IPromotionRepository
{
    private const string KeyOfferTypeOptions      = "MD:OfferTypeOptions";
    private const string KeySalesOrderTypeOptions = "MD:SalesOrderTypeOptions";
    private const string KeySiteGroupOptions      = "MD:SiteGroupOptions";
    private const string KeyMemberCodeOptions     = "MD:MemberCodeOptions";

    // PageSize lớn để lấy toàn bộ theo filter khi export (SP vẫn OFFSET/FETCH, constant memory phía SQL).
    private const int ExportPageSize = 100_000;

    public async Task<(List<OfferHeaderListItemDto> Items, int Total)> GetOfferHeaderListAsync(
        OfferListFilter filter, CancellationToken ct = default)
    {
        var items = (await QueryAsync<OfferHeaderListItemDto>(Sql, BuildParams(filter,
            Math.Max(0, filter.PageNumber), Math.Max(1, filter.PageSize)),
            commandTimeout: 120, ct: ct)).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<List<OfferHeaderListItemDto>> ExportOfferHeaderListAsync(
        OfferListFilter filter, CancellationToken ct = default)
        => (await QueryAsync<OfferHeaderListItemDto>(Sql, BuildParams(filter, 0, ExportPageSize),
            commandTimeout: 120, ct: ct)).ToList();

    public async Task<List<OptionItemDto>> GetOfferTypeOptionsAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<OptionItemDto>>(KeyOfferTypeOptions);
        if (cached?.Count > 0) return cached;

        const string sql = @"SELECT [OfferType] AS Value, [OfferName] AS Text
                             FROM   dbo.OfferType (NOLOCK)
                             WHERE  ISNULL([Enabled], 0) = 1
                             ORDER  BY [OfferType]";
        var data = (await QueryAsync<OptionItemDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyOfferTypeOptions, data, ttlSeconds: 43200);
        return data;
    }

    public async Task<List<OptionItemDto>> GetSalesOrderTypeOptionsAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<OptionItemDto>>(KeySalesOrderTypeOptions);
        if (cached?.Count > 0) return cached;

        const string sql = @"SELECT [Code] AS Value, [Description] AS Text
                             FROM   dbo.SalesOrderType (NOLOCK)
                             WHERE  ISNULL([IsActive], 0) = 1
                             ORDER  BY [Code]";
        var data = (await QueryAsync<OptionItemDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeySalesOrderTypeOptions, data, ttlSeconds: 43200);
        return data;
    }

    // ── Cài đặt CTKM (11.1) ──────────────────────────────────────────────────

    public async Task<(List<PromotionSetupListItemDto> Items, int Total)> GetSetupListAsync(
        PromotionSetupListFilter filter, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT  BBYNR              AS No,
                    ISNULL(BBYTEXT,'') AS Description,
                    ISNULL(BBYTYPE,'') AS OfferType,
                    ISNULL(SalesType,'') AS SalesType,
                    ISNULL(STATUS,'')  AS Status,
                    ISNULL(VALIDFROM,'') AS ValidFrom,
                    ISNULL(VALIDTO,'') AS ValidTo,
                    ISNULL(IsApprove,0) AS IsApprove,
                    COUNT(*) OVER()    AS Total
            FROM    dbo.SetupPromotionHEADER (NOLOCK)
            WHERE   (@OfferNo = '' OR BBYNR = @OfferNo)
              AND   (@OfferName = '' OR BBYTEXT LIKE '%' + @OfferName + '%')
              AND   (@ApproveStatus = ''
                     OR ISNULL(IsApprove,0) = CASE WHEN @ApproveStatus = '1' THEN 1 ELSE 0 END)
            ORDER BY BBYNR DESC
            OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await QueryAsync<PromotionSetupListItemDto>(sql, new
        {
            OfferNo       = (filter.OfferNo ?? string.Empty).Trim(),
            OfferName     = (filter.OfferName ?? string.Empty).Trim(),
            ApproveStatus = (filter.ApproveStatus ?? string.Empty).Trim(),
            PageSize      = Math.Max(1, filter.PageSize),
            PageNumber    = Math.Max(0, filter.PageNumber)
        }, commandTimeout: 120, ct: ct)).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<PromotionSetupDetailDto?> GetSetupDetailAsync(string bbynr, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT  BBYNR AS No, ISNULL(BBYTEXT,'') AS Description, ISNULL(SalesType,'') AS SalesType,
                    ISNULL(BBYTYPE,'') AS OfferType, ISNULL(STATUS,'1') AS Status,
                    ISNULL(VALIDFROM,'') AS StartingDate, ISNULL(VALIDTO,'') AS EndingDate,
                    ISNULL(IsVoucher,0) AS IsVoucher, ISNULL(IsApprove,0) AS IsApprove,
                    CASE WHEN BUYLINKCAT='O' THEN 'OR' ELSE 'AND' END AS ConditionBuy,
                    CASE WHEN GETLINKCAT='O' THEN 'OR' ELSE 'AND' END AS ConditionGet,
                    ISNULL(TRY_CONVERT(decimal(18,3), LIMIT),0) AS LimitQty,
                    CAST(CASE WHEN VINID='X' THEN 1 ELSE 0 END AS bit) AS MemberOnly,
                    ISNULL(MemberCode,'') AS MemberCode,
                    ISNULL(TRY_CONVERT(int, ZPRIOR),1) AS PriorityBBY,
                    ISNULL(TRY_CONVERT(int, NUMOFDAYS),0) AS NumOfDays,
                    ISNULL(ZVCDATE_ST,'') AS VoucherFromDate,
                    ISNULL(ZVCDATE_EN,'') AS VoucherToDate,
                    ISNULL(TRY_CONVERT(int, ZVCDATE_VA),0) AS VoucherValidDay,
                    ISNULL(TRY_CONVERT(int, LIMITNR),0) AS VoucherLimitNumber
            FROM    dbo.SetupPromotionHEADER (NOLOCK) WHERE BBYNR = @bbynr;

            SELECT  CASE WHEN BUYTYPE='MGP' THEN 1 ELSE 0 END AS LineType,
                    ISNULL(MAT_NR,'') AS No, ISNULL(MATGROUP,'') AS GroupCode, '' AS Description,
                    ISNULL(MEINH,'') AS UnitOfMeasure,
                    ISNULL(TRY_CONVERT(decimal(18,3), MAT_QUAN),0) AS Quantity,
                    ISNULL(ScaleType,'C') AS ScaleType
            FROM    dbo.SetupPromotionBUY (NOLOCK) WHERE BBYNR = @bbynr ORDER BY ID;

            SELECT  CASE WHEN GETTYPE='MGP' THEN 1 ELSE 0 END AS LineType,
                    ISNULL(MATERIALCODE,'') AS No, ISNULL(MATGROUP,'') AS GroupCode, '' AS Description,
                    ISNULL(MEINH,'') AS UnitOfMeasure,
                    ISNULL(TRY_CONVERT(decimal(18,3), QTY),0) AS Quantity,
                    ISNULL(SCALETYPE,'C') AS ScaleType,
                    CASE DISTYPE WHEN '%' THEN 0 WHEN 'R' THEN 1 WHEN 'P' THEN 2 ELSE 0 END AS DiscountType,
                    ISNULL(TRY_CONVERT(decimal(18,3), CASE WHEN DISTYPE='%' THEN BBYPER ELSE BBYVAL END),0) AS DiscountValue
            FROM    dbo.SetupPromotionGET (NOLOCK) WHERE BBYNR = @bbynr ORDER BY ID;

            SELECT  DISTINCT s.SITEGROUPCODE AS SiteGroupCode, ISNULL(g.GroupName,'') AS GroupName
            FROM    dbo.SetupPromotionSITE (NOLOCK) s
            LEFT JOIN dbo.SetupGroupSites (NOLOCK) g ON g.GroupCode = s.SITEGROUPCODE
            WHERE   s.BBYNR = @bbynr;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var multi = await conn.QueryMultipleAsync(
            new CommandDefinition(sql, new { bbynr }, commandTimeout: 120, cancellationToken: ct));

        var header = await multi.ReadFirstOrDefaultAsync<PromotionSetupHeaderDto>();
        if (header == null) return null;

        var buys = (await multi.ReadAsync<OfferBuyLineDto>()).ToList();
        var gets = (await multi.ReadAsync<OfferGetLineDto>()).ToList();
        var sites = (await multi.ReadAsync<OfferSiteLineDto>()).ToList();

        // VALIDFROM/VALIDTO lưu yyyyMMdd → đổi sang dd/MM/yyyy cho form
        header.StartingDate = YmdToDisplay(header.StartingDate);
        header.EndingDate = YmdToDisplay(header.EndingDate);
        // Voucher date: sentinel 19000101 / length≠8 → rỗng
        header.VoucherFromDate = YmdToVoucherDisplay(header.VoucherFromDate);
        header.VoucherToDate = YmdToVoucherDisplay(header.VoucherToDate);

        return new PromotionSetupDetailDto { Header = header, BuyRows = buys, GetRows = gets, SiteRows = sites };
    }

    public async Task<(bool Ok, string Message, string BBYNR)> SaveSetupAsync(
        PromotionSetupSaveRequest request, CancellationToken ct = default)
    {
        var h = request.Header;

        // ── Validate ──
        if (string.IsNullOrWhiteSpace(h.Description))
            return (false, "Vui lòng nhập tên chương trình khuyến mãi", string.Empty);
        if (string.IsNullOrWhiteSpace(h.SalesType))
            return (false, "Vui lòng chọn hình thức bán hàng", string.Empty);
        if (string.IsNullOrWhiteSpace(h.OfferType) && string.IsNullOrWhiteSpace(h.No))
            return (false, "Vui lòng chọn loại CTKM", string.Empty);
        if (!new[] { "0", "1", "2" }.Contains(h.Status))
            return (false, "Trạng thái không hợp lệ", string.Empty);
        if (!DateTime.TryParseExact(h.StartingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
            return (false, "Ngày bắt đầu không đúng định dạng (dd/MM/yyyy)", string.Empty);
        if (!DateTime.TryParseExact(h.EndingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
            return (false, "Ngày kết thúc không đúng định dạng (dd/MM/yyyy)", string.Empty);
        if (endDate < startDate)
            return (false, "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu", string.Empty);

        // ── Expand nhóm cửa hàng → (SiteGroupCode, SiteCode) ──
        var siteTable = await BuildSiteTableAsync(request.SiteGroupCodes, ct);

        var p = new DynamicParameters();
        p.Add("@BBYNR", string.IsNullOrWhiteSpace(h.No) ? string.Empty : h.No.Trim(),
              DbType.String, ParameterDirection.InputOutput, 20);
        p.Add("@SalesType", h.SalesType);
        p.Add("@Description", h.Description);
        p.Add("@OfferType", h.OfferType ?? string.Empty);
        p.Add("@Status", h.Status);
        p.Add("@ValidFrom", startDate.ToString("yyyyMMdd"));
        p.Add("@ValidTo", endDate.ToString("yyyyMMdd"));
        p.Add("@IsVoucher", h.IsVoucher);
        p.Add("@BuyLinkCat", h.ConditionBuy == "OR" ? "O" : "A");
        p.Add("@GetLinkCat", h.ConditionGet == "OR" ? "O" : "A");
        // ── Advanced (Phase 2) ──
        p.Add("@LimitQty", h.LimitQty.ToString(CultureInfo.InvariantCulture));
        p.Add("@MemberOnly", h.MemberOnly);
        p.Add("@MemberCode", h.MemberCode ?? string.Empty);
        p.Add("@Priority", (h.PriorityBBY <= 0 ? 1 : h.PriorityBBY).ToString(CultureInfo.InvariantCulture));
        p.Add("@NumOfDays", h.NumOfDays.ToString(CultureInfo.InvariantCulture));
        p.Add("@VoucherFrom", DmyToYmd(h.VoucherFromDate));
        p.Add("@VoucherTo", DmyToYmd(h.VoucherToDate));
        p.Add("@VoucherValidDay", h.VoucherValidDay.ToString(CultureInfo.InvariantCulture));
        p.Add("@VoucherLimitNumber", h.VoucherLimitNumber.ToString(CultureInfo.InvariantCulture));
        p.Add("@Buy", BuildBuyTable(request.BuyRows).AsTableValuedParameter("dbo.SetupPromotionBuyTVP"));
        p.Add("@Get", BuildGetTable(request.GetRows).AsTableValuedParameter("dbo.SetupPromotionGetTVP"));
        p.Add("@Site", siteTable.AsTableValuedParameter("dbo.SetupPromotionSiteTVP"));

        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SaveSetupCTKMAll", p,
                commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));

            var savedNo = p.Get<string>("@BBYNR");
            return (true, $"Lưu CTKM {savedNo} thành công", savedNo);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, string.Empty);
        }
    }

    public async Task<(bool Ok, string Message)> ApproveSetupAsync(string bbynr, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SetupPromotion_Approve", new { BBYNR = bbynr },
                commandType: CommandType.StoredProcedure, commandTimeout: 300, cancellationToken: ct));
            return (true, $"Duyệt CTKM {bbynr} thành công");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<bool> UpdateSetupStatusAsync(string bbynr, string status, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            var rows = await conn.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SetupPromotion_UpdateStatus", new { BBYNR = bbynr, Status = status },
                commandType: CommandType.StoredProcedure, commandTimeout: 60, cancellationToken: ct));
            return rows > 0;
        }
        catch { return false; }
    }

    public async Task<List<ItemOptionDto>> SearchItemsAsync(string keyword, CancellationToken ct = default)
    {
        const string sql = @"SELECT TOP 50 [No] AS No, ISNULL([Description],'') AS Description,
                                    ISNULL([BaseUnitOfMeasure],'') AS Uom
                             FROM   dbo.Item (NOLOCK)
                             WHERE  (@kw = '' OR [No] LIKE '%' + @kw + '%' OR [Description] LIKE '%' + @kw + '%')
                             ORDER  BY [No]";
        return (await QueryAsync<ItemOptionDto>(sql, new { kw = (keyword ?? string.Empty).Trim() }, ct: ct)).ToList();
    }

    public async Task<List<OptionItemDto>> GetMemberCodeOptionsAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<OptionItemDto>>(KeyMemberCodeOptions);
        if (cached?.Count > 0) return cached;

        const string sql = @"SELECT [Code] AS Value, ISNULL([Description],[Code]) AS Text
                             FROM   dbo.OptionData (NOLOCK)
                             WHERE  ISNULL([Status], 0) = 1 AND [Caption] = 'MEMBERCODETYPE'
                             ORDER  BY [Order]";
        var data = (await QueryAsync<OptionItemDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyMemberCodeOptions, data, ttlSeconds: 43200);
        return data;
    }

    public async Task<List<OfferSiteLineDto>> GetSiteGroupOptionsAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<OfferSiteLineDto>>(KeySiteGroupOptions);
        if (cached?.Count > 0) return cached;

        const string sql = @"SELECT [GroupCode] AS SiteGroupCode, ISNULL([GroupName],'') AS GroupName
                             FROM   dbo.SetupGroupSites (NOLOCK)
                             ORDER  BY [GroupCode]";
        var data = (await QueryAsync<OfferSiteLineDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeySiteGroupOptions, data, ttlSeconds: 43200);
        return data;
    }

    // ── Helpers (TVP builders + site expansion) ──────────────────────────────

    private async Task<DataTable> BuildSiteTableAsync(List<string> groupCodes, CancellationToken ct)
    {
        var table = new DataTable();
        table.Columns.Add("SiteGroupCode", typeof(string));
        table.Columns.Add("SiteCode", typeof(string));

        var codes = groupCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        if (codes.Count == 0) return table;

        const string sql = @"SELECT [GroupCode] AS GroupCode, ISNULL([ListStore],'') AS ListStore
                             FROM   dbo.SetupGroupSites (NOLOCK)
                             WHERE  [GroupCode] IN @codes";
        var groups = (await QueryAsync<SiteGroupRow>(sql, new { codes }, ct: ct)).ToList();

        foreach (var g in groups)
        {
            if (string.Equals(g.ListStore, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                table.Rows.Add(g.GroupCode, "ALL");
                continue;
            }
            List<string>? stores = null;
            try { stores = JsonConvert.DeserializeObject<List<string>>(g.ListStore); } catch { /* ignore bad json */ }
            if (stores == null || stores.Count == 0)
            {
                table.Rows.Add(g.GroupCode, "ALL");
                continue;
            }
            foreach (var s in stores.Where(s => !string.IsNullOrWhiteSpace(s)))
                table.Rows.Add(g.GroupCode, s);
        }
        return table;
    }

    private static DataTable BuildBuyTable(List<OfferBuyLineDto> rows)
    {
        var t = new DataTable();
        t.Columns.Add("LineType", typeof(int));
        t.Columns.Add("ItemNo", typeof(string));
        t.Columns.Add("GroupCode", typeof(string));
        t.Columns.Add("Quantity", typeof(string));
        t.Columns.Add("Uom", typeof(string));
        t.Columns.Add("ScaleType", typeof(string));
        foreach (var r in rows)
            t.Rows.Add(r.LineType, r.No ?? "", r.GroupCode ?? "",
                       r.Quantity.ToString(CultureInfo.InvariantCulture), r.UnitOfMeasure ?? "", r.ScaleType ?? "C");
        return t;
    }

    private static DataTable BuildGetTable(List<OfferGetLineDto> rows)
    {
        var t = new DataTable();
        t.Columns.Add("LineType", typeof(int));
        t.Columns.Add("ItemNo", typeof(string));
        t.Columns.Add("GroupCode", typeof(string));
        t.Columns.Add("Quantity", typeof(string));
        t.Columns.Add("Uom", typeof(string));
        t.Columns.Add("ScaleType", typeof(string));
        t.Columns.Add("DiscountType", typeof(int));
        t.Columns.Add("DiscountValue", typeof(string));
        foreach (var r in rows)
            t.Rows.Add(r.LineType, r.No ?? "", r.GroupCode ?? "",
                       r.Quantity.ToString(CultureInfo.InvariantCulture), r.UnitOfMeasure ?? "", r.ScaleType ?? "C",
                       r.DiscountType, r.DiscountValue.ToString(CultureInfo.InvariantCulture));
        return t;
    }

    private static string YmdToDisplay(string ymd)
        => DateTime.TryParseExact(ymd, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.ToString("dd/MM/yyyy") : string.Empty;

    // Voucher date: bỏ sentinel 19000101 / giá trị "0" / length≠8 → rỗng
    private static string YmdToVoucherDisplay(string ymd)
        => (string.IsNullOrEmpty(ymd) || ymd == "0" || ymd.Length != 8 || ymd == "19000101")
            ? string.Empty : YmdToDisplay(ymd);

    private static string DmyToYmd(string dmy)
        => DateTime.TryParseExact(dmy, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.ToString("yyyyMMdd") : string.Empty;

    private sealed class SiteGroupRow
    {
        public string GroupCode { get; set; } = string.Empty;
        public string ListStore { get; set; } = string.Empty;
    }

    // SP [dbo].[GetPromotionOfferHeaderList] (bản cập nhật) chỉ còn 9 tham số —
    // đã bỏ @StyleProfile và @SalesType so với legacy.
    private const string Sql =
        "[dbo].[GetPromotionOfferHeaderList] @No,@Description,@Status,@OfferType,@ItemNo,@StoreNo,@Exp,@PageSize,@PageNumber";

    private static object BuildParams(OfferListFilter f, int pageNumber, int pageSize) => new
    {
        No          = (f.TextSearch ?? string.Empty).Trim(),
        Description = (f.PromotionName ?? string.Empty).Trim(),
        Status      = string.IsNullOrWhiteSpace(f.Status) ? "-1" : f.Status.Trim(),
        OfferType   = (f.OfferType ?? string.Empty).Trim(),
        ItemNo      = (f.ItemNo ?? string.Empty).Trim(),
        StoreNo     = string.Empty,         // không lọc theo store (parity legacy)
        Exp         = 0,
        PageSize    = pageSize,
        PageNumber  = pageNumber
    };
}
