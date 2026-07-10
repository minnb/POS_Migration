using System.Data;
using Dapper;
using POS.Common.Dtos.Price;
using POS.Infrastructure.Database;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;
using POS.Infrastructure.Sync;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// 9.1 Danh mục Bảng giá + 9.3 Setup Giá — DB RPOSMasterData (CentralMD).
/// Port từ VCM.BLUEPOS PriceData/SetupPriceData. Read qua SP có sẵn; validate/save qua TVP + SP mới.
/// </summary>
public sealed class PriceRepository(
    CentralMDConnectionFactory connectionFactory,
    IRedisService redis,
    ISyncTableTrackerService syncTracker)
    : BaseRepository(connectionFactory), IPriceRepository
{
    private const string KeyPriceGroupOptions = "MD:PriceGroupOptions";

    // PageSize lớn để lấy toàn bộ theo filter khi export (SP legacy không paging cho export).
    public async Task<(List<PriceListItemDto> Items, int Total)> GetListAsync(
        PriceListFilter filter, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        // Gọi SP bằng positional EXEC (tham số truyền theo ĐÚNG THỨ TỰ) — khớp legacy PriceData.GetPriceList.
        // KHÔNG dùng CommandType.StoredProcedure (bind theo TÊN): [GetSalesPriceList] là SP có sẵn (legacy),
        // tên tham số nội bộ KHÔNG đảm bảo trùng anon-object → bind theo tên sẽ ném lỗi "expects parameter…".
        // 2026-07: SP đổi @BarCode → @SaleType (Hình thức bán hàng), @SalesCode → @SalesGroup (mã nhóm giá).
        var items = (await conn.QueryAsync<PriceListItemDto>(new CommandDefinition(
            "EXEC [dbo].[GetSalesPriceList] @ItemCode, @ItemName, @SaleType, @SalesGroup, @isCheck, @PageSize, @PageNumber",
            new
            {
                ItemCode   = (filter.ItemNo ?? string.Empty).Trim(),
                ItemName   = (filter.ItemName ?? string.Empty).Trim(),
                SaleType   = (filter.SaleType ?? string.Empty).Trim(),
                SalesGroup = NormalizeSalesGroup(filter.SalesGroup),
                isCheck    = filter.IsCheck,
                PageSize   = Math.Max(1, filter.PageSize),
                PageNumber = Math.Max(0, filter.PageNumber)
            },
            commandTimeout: 120, cancellationToken: ct))).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<List<PriceListItemDto>> GetExportListAsync(
        PriceListFilter filter, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        // Positional EXEC — khớp legacy PriceData.ExportPriceList (xem ghi chú ở GetListAsync).
        return (await conn.QueryAsync<PriceListItemDto>(new CommandDefinition(
            "EXEC [dbo].[GetSalesPriceList_Export] @ItemCode, @ItemName, @SaleType, @SalesGroup, @isCheck",
            new
            {
                ItemCode   = (filter.ItemNo ?? string.Empty).Trim(),
                ItemName   = (filter.ItemName ?? string.Empty).Trim(),
                SaleType   = (filter.SaleType ?? string.Empty).Trim(),
                SalesGroup = NormalizeSalesGroup(filter.SalesGroup),
                isCheck    = filter.IsCheck
            },
            commandTimeout: 120, cancellationToken: ct))).ToList();
    }

    // "ALL" là sentinel UI (mục "Tất cả" trong combobox Nhóm giá) — SP hiểu "tất cả" là chuỗi rỗng.
    private static string NormalizeSalesGroup(string? salesGroup)
    {
        var trimmed = (salesGroup ?? string.Empty).Trim();
        return trimmed.Equals("ALL", StringComparison.OrdinalIgnoreCase) ? string.Empty : trimmed;
    }

    public async Task<List<PriceImportResultRow>> ValidateImportAsync(
        IReadOnlyList<PriceImportRow> rows, CancellationToken ct = default)
    {
        // Port nguyên query legacy SetupPriceData.ValidateImport, đổi #temp → TVP dbo.SetupSalePriceImportTVP.
        // Nhánh 1: dòng có ItemNo (Barcode rỗng) — join Item + ItemUnitOfMeasure.
        // Nhánh 2: dòng có Barcode — join Barcodes + Item.
        const string sql = @"
SELECT
    ISNULL(IT.No, '')                                            AS ItemNo,
    ISNULL(U.Code, SalesUnitOfMeasure)                           AS Uom,
    ''                                                          AS Barcode,
    ISNULL(IT.No, '') + '-' + ISNULL(IT.Description, '')       AS [Text],
    ISNULL(IT.No, '')                                          AS Id,
    CONCAT(
        CASE WHEN IT.No IS NULL THEN I.ItemNo + N' Item không tồn tại; ' ELSE '' END,
        CASE WHEN ISNULL(U.Code, ISNULL(IT.SalesUnitOfMeasure,'')) =''  THEN I.ItemNo + N'- UOM: ' + I.Uom + N' không hợp lệ; ' ELSE '' END
    )                                                          AS ErrorMessage,
    I.UnitPrice, I.StartingDate, I.EndingDate
FROM @Import I
LEFT JOIN dbo.Item IT ON IT.No = I.ItemNo
LEFT JOIN dbo.ItemUnitOfMeasure U ON U.ItemNo = I.ItemNo AND U.Code = I.Uom
WHERE ISNULL(I.Barcode, '') = ''

UNION

SELECT
    ISNULL(B.ItemNo, '')                                        AS ItemNo,
    ISNULL(B.UnitOfMeasureCode, '')                             AS Uom,
    I.Barcode                                                  AS Barcode,
    ISNULL(IT.No, '') + '-' + ISNULL(IT.Description, '')       AS [Text],
    ISNULL(IT.No, '')                                          AS Id,
    CASE WHEN B.BarcodeNo IS NULL THEN I.Barcode + N' Barcode không tồn tại; ' ELSE '' END AS ErrorMessage,
    I.UnitPrice, I.StartingDate, I.EndingDate
FROM @Import I
LEFT JOIN dbo.Barcodes B ON B.BarcodeNo = I.Barcode
LEFT JOIN dbo.Item IT ON IT.No = B.ItemNo
WHERE ISNULL(I.Barcode, '') <> ''";

        var p = new DynamicParameters();
        p.Add("@Import", BuildImportTable(rows).AsTableValuedParameter("dbo.SetupSalePriceImportTVP"));

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        return (await conn.QueryAsync<PriceImportResultRow>(new CommandDefinition(
            sql, p, commandTimeout: 120, cancellationToken: ct))).ToList();
    }

    public async Task<PriceSaveResult> SaveAsync(
        IReadOnlyList<PriceSaveLine> lines, string actor, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@Lines", BuildLineTable(lines).AsTableValuedParameter("dbo.SetupSalePriceLineTVP"));
        p.Add("@Actor", actor ?? string.Empty);
        // Kết quả qua OUTPUT param — KHÔNG dùng result set. Nhánh update của SP gọi
        // Setup_SalePrice_Get_ALL (có SELECT Interface_Errors + ROLLBACK bên trong) nên result set
        // của nó sẽ lẫn vào; ExecuteAsync (ExecuteNonQuery) nuốt hết result set rồi output param mới
        // được gán → đọc @Ok/@Message an toàn (trước đây QueryFirstOrDefault đọc nhầm set rỗng → báo lỗi giả).
        p.Add("@Ok", dbType: DbType.Boolean, direction: ParameterDirection.Output);
        p.Add("@Message", dbType: DbType.String, size: 4000, direction: ParameterDirection.Output);
        p.Add("@OutCounter", dbType: DbType.Int64, direction: ParameterDirection.Output);

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "dbo.usp_SetupSalePrice_Save", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 300, cancellationToken: ct));

        var ok = p.Get<bool?>("@Ok") ?? false;
        var message = p.Get<string?>("@Message");

        if (ok) syncTracker.Track("SalesPrice", p.Get<long>("@OutCounter"));

        return new PriceSaveResult
        {
            Ok = ok,
            Message = string.IsNullOrWhiteSpace(message)
                ? (ok ? "Cập nhật thành công bảng giá" : "Lưu bảng giá thất bại")
                : message!
        };
    }

    public async Task<PriceSaveResult> UpdatePriceAsync(
        PriceRowKey key, double unitPrice, string actor, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@ItemNo", key.ItemNo);
        p.Add("@SalesCode", key.SalesCode);
        p.Add("@StartingDate", key.StartingDate.Date, DbType.Date);
        p.Add("@UnitOfMeasureCode", key.UnitOfMeasureCode);
        p.Add("@SalesType", key.SalesType ?? string.Empty);
        p.Add("@UnitPrice", unitPrice);
        p.Add("@Actor", actor ?? string.Empty);

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<PriceSaveResult>(new CommandDefinition(
            "dbo.usp_SalesPrice_UpdatePrice", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));

        if (row is { Ok: true }) syncTracker.Track("SalesPrice", row.Counter);

        return row ?? new PriceSaveResult { Ok = false, Message = "Cập nhật giá thất bại" };
    }

    public async Task<PriceSaveResult> SoftDeletePriceAsync(
        PriceRowKey key, string actor, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@ItemNo", key.ItemNo);
        p.Add("@SalesCode", key.SalesCode);
        p.Add("@StartingDate", key.StartingDate.Date, DbType.Date);
        p.Add("@UnitOfMeasureCode", key.UnitOfMeasureCode);
        p.Add("@SalesType", key.SalesType ?? string.Empty);
        p.Add("@Actor", actor ?? string.Empty);

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<PriceSaveResult>(new CommandDefinition(
            "dbo.usp_SalesPrice_SoftDelete", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));

        if (row is { Ok: true }) syncTracker.Track("SalesPrice", row.Counter);

        return row ?? new PriceSaveResult { Ok = false, Message = "Xóa giá thất bại" };
    }

    public async Task<List<PriceOptionDto>> GetPriceGroupOptionsAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<PriceOptionDto>>(KeyPriceGroupOptions);
        if (cached?.Count > 0) return cached;

        // Text hiển thị = "PriceGroupCode - PriceGroupName" (nếu thiếu tên → chỉ Code).
        const string sql = @"SELECT DISTINCT [PriceGroupCode] AS Value,
                                    CASE WHEN ISNULL([PriceGroupName], '') = '' THEN [PriceGroupCode]
                                         ELSE [PriceGroupCode] + N' - ' + [PriceGroupName] END AS Text
                             FROM   dbo.StorePriceGroup (NOLOCK)
                             WHERE  ISNULL([PriceGroupCode], '') <> ''
                             ORDER  BY Text";
        var data = (await QueryAsync<PriceOptionDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyPriceGroupOptions, data, ttlSeconds: 43200);
        return data;
    }

    public async Task<List<string>> GetItemUomsAsync(string itemNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(itemNo)) return [];

        const string sql = @"SELECT [Code]
                             FROM   dbo.ItemUnitOfMeasure (NOLOCK)
                             WHERE  [ItemNo] = @itemNo
                             ORDER  BY [Code]";
        return (await QueryAsync<string>(sql, new { itemNo = itemNo.Trim() }, ct: ct)).ToList();
    }

    // ── Danh mục nhóm giá (StorePriceGroupHeader + StorePriceGroup) ──────────────
    public async Task<(List<PriceGroupListItemDto> Items, int Total)> GetPriceGroupListAsync(
        PriceGroupListFilter filter, CancellationToken ct = default)
    {
        // StoreCount/Priority lấy từ bảng chi tiết StorePriceGroup (Priority cấp nhóm → TOP 1 là đủ).
        const string sql = @"
SELECT COUNT(*) OVER()                                              AS Total,
       h.ID,
       h.PriceGroupCode,
       h.PriceGroupName,
       ISNULL((SELECT TOP 1 sg.[Priority] FROM dbo.StorePriceGroup sg (NOLOCK)
               WHERE sg.PriceGroupCode = h.PriceGroupCode), 0)      AS [Priority],
       (SELECT COUNT(*) FROM dbo.StorePriceGroup sg (NOLOCK)
               WHERE sg.PriceGroupCode = h.PriceGroupCode)          AS StoreCount,
       CONVERT(varchar(10), h.CreatedDate, 103)                     AS CreatedDateStr
FROM   dbo.StorePriceGroupHeader h (NOLOCK)
WHERE  (@Code = '' OR h.PriceGroupCode LIKE '%' + @Code + '%')
  AND  (@Name = '' OR h.PriceGroupName LIKE '%' + @Name + '%')
ORDER  BY h.PriceGroupCode
OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = (await QueryAsync<PriceGroupListItemDto>(sql, new
        {
            Code = (filter.PriceGroupCode ?? string.Empty).Trim(),
            Name = (filter.PriceGroupName ?? string.Empty).Trim(),
            PageSize = Math.Max(1, filter.PageSize),
            PageNumber = Math.Max(0, filter.PageNumber)
        }, ct: ct)).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<List<PriceGroupStoreItemDto>> GetPriceGroupStoresAsync(
        string priceGroupCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(priceGroupCode)) return [];

        const string sql = @"SELECT sg.Store,
                                    s.Name        AS StoreName,
                                    sg.[Priority] AS [Priority]
                             FROM   dbo.StorePriceGroup sg (NOLOCK)
                             LEFT   JOIN dbo.Store s (NOLOCK) ON s.No = sg.Store
                             WHERE  sg.PriceGroupCode = @code
                             ORDER  BY sg.Store";
        return (await QueryAsync<PriceGroupStoreItemDto>(sql, new { code = priceGroupCode.Trim() }, ct: ct)).ToList();
    }

    public async Task<PriceSaveResult> SavePriceGroupAsync(
        PriceGroupSaveRequest request, string actor, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@PriceGroupCode", (request.PriceGroupCode ?? string.Empty).Trim());
        p.Add("@PriceGroupName", (request.PriceGroupName ?? string.Empty).Trim());
        p.Add("@Priority", request.Priority);
        p.Add("@Stores", BuildStoreTable(request.Stores).AsTableValuedParameter("dbo.StorePriceGroupStoreTVP"));
        p.Add("@UserName", actor ?? string.Empty);
        p.Add("@Ok", dbType: DbType.Boolean, direction: ParameterDirection.Output);
        p.Add("@Message", dbType: DbType.String, size: 400, direction: ParameterDirection.Output);

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "dbo.usp_StorePriceGroup_Save", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));

        var ok = p.Get<bool?>("@Ok") ?? false;
        var message = p.Get<string?>("@Message");
        if (ok) redis.Delete(KeyPriceGroupOptions);   // dropdown "Nhóm giá" cập nhật lại
        return new PriceSaveResult
        {
            Ok = ok,
            Message = string.IsNullOrWhiteSpace(message)
                ? (ok ? "Lưu nhóm giá thành công" : "Lưu nhóm giá thất bại")
                : message!
        };
    }

    public async Task<PriceSaveResult> DeletePriceGroupAsync(
        string priceGroupCode, string actor, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@PriceGroupCode", (priceGroupCode ?? string.Empty).Trim());
        p.Add("@Ok", dbType: DbType.Boolean, direction: ParameterDirection.Output);
        p.Add("@Message", dbType: DbType.String, size: 400, direction: ParameterDirection.Output);

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "dbo.usp_StorePriceGroup_Delete", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));

        var ok = p.Get<bool?>("@Ok") ?? false;
        var message = p.Get<string?>("@Message");
        if (ok) redis.Delete(KeyPriceGroupOptions);
        return new PriceSaveResult
        {
            Ok = ok,
            Message = string.IsNullOrWhiteSpace(message)
                ? (ok ? "Xóa nhóm giá thành công" : "Xóa nhóm giá thất bại")
                : message!
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static DataTable BuildStoreTable(IEnumerable<string>? stores)
    {
        var t = new DataTable();
        t.Columns.Add("Store", typeof(string));
        foreach (var s in stores ?? [])
        {
            var code = (s ?? string.Empty).Trim();
            if (code.Length > 0) t.Rows.Add(code);
        }
        return t;
    }

    private static DataTable BuildImportTable(IEnumerable<PriceImportRow> rows)
    {
        var t = new DataTable();
        t.Columns.Add("ItemNo", typeof(string));
        t.Columns.Add("Uom", typeof(string));
        t.Columns.Add("Barcode", typeof(string));
        t.Columns.Add("UnitPrice", typeof(string));
        t.Columns.Add("StartingDate", typeof(string));
        t.Columns.Add("EndingDate", typeof(string));
        foreach (var r in rows)
            t.Rows.Add(
                r.ItemNo ?? string.Empty, r.Uom ?? string.Empty, r.Barcode ?? string.Empty,
                r.UnitPrice ?? string.Empty, r.StartingDate ?? string.Empty, r.EndingDate ?? string.Empty);
        return t;
    }

    private static DataTable BuildLineTable(IEnumerable<PriceSaveLine> lines)
    {
        var t = new DataTable();
        t.Columns.Add("Pkey", typeof(string));
        t.Columns.Add("ItemNo", typeof(string));
        t.Columns.Add("SalesCode", typeof(string));
        t.Columns.Add("SalesType", typeof(string));
        t.Columns.Add("UnitOfMeasureCode", typeof(string));
        t.Columns.Add("UnitPrice", typeof(double));
        t.Columns.Add("StartingDate", typeof(DateTime));
        t.Columns.Add("EndingDate", typeof(DateTime));
        foreach (var l in lines)
            t.Rows.Add(l.Pkey, l.ItemNo, l.SalesCode, l.SalesType, l.UOM, l.UnitPrice, l.StartDate, l.EndDate);
        return t;
    }
}
