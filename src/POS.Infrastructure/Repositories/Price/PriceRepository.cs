using System.Data;
using Dapper;
using POS.Common.Dtos.Price;
using POS.Infrastructure.Database;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// 9.1 Danh mục Bảng giá + 9.3 Setup Giá — DB RPOSMasterData (CentralMD).
/// Port từ VCM.BLUEPOS PriceData/SetupPriceData. Read qua SP có sẵn; validate/save qua TVP + SP mới.
/// </summary>
public sealed class PriceRepository(CentralMDConnectionFactory connectionFactory)
    : BaseRepository(connectionFactory), IPriceRepository
{
    // PageSize lớn để lấy toàn bộ theo filter khi export (SP legacy không paging cho export).
    public async Task<(List<PriceListItemDto> Items, int Total)> GetListAsync(
        PriceListFilter filter, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        // Gọi SP bằng positional EXEC (tham số truyền theo ĐÚNG THỨ TỰ) — khớp legacy PriceData.GetPriceList.
        // KHÔNG dùng CommandType.StoredProcedure (bind theo TÊN): [GetSalesPriceList] là SP có sẵn (legacy),
        // tên tham số nội bộ KHÔNG đảm bảo trùng anon-object → bind theo tên sẽ ném lỗi "expects parameter…".
        var items = (await conn.QueryAsync<PriceListItemDto>(new CommandDefinition(
            "EXEC [dbo].[GetSalesPriceList] @ItemCode, @ItemName, @BarCode, @SalesCode, @isCheck, @PageSize, @PageNumber",
            new
            {
                ItemCode   = (filter.ItemNo ?? string.Empty).Trim(),
                ItemName   = (filter.ItemName ?? string.Empty).Trim(),
                BarCode    = (filter.Barcode ?? string.Empty).Trim(),
                SalesCode  = (filter.SalesCode ?? string.Empty).Trim(),
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
            "EXEC [dbo].[GetSalesPriceList_Export] @ItemCode, @ItemName, @BarCode, @SalesCode, @isCheck",
            new
            {
                ItemCode  = (filter.ItemNo ?? string.Empty).Trim(),
                ItemName  = (filter.ItemName ?? string.Empty).Trim(),
                BarCode   = (filter.Barcode ?? string.Empty).Trim(),
                SalesCode = (filter.SalesCode ?? string.Empty).Trim(),
                isCheck   = filter.IsCheck
            },
            commandTimeout: 120, cancellationToken: ct))).ToList();
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
    ISNULL(U.Code, '')                                          AS Uom,
    ''                                                          AS Barcode,
    ISNULL(IT.No, '') + '-' + ISNULL(IT.Description, '')       AS [Text],
    ISNULL(IT.No, '')                                          AS Id,
    CONCAT(
        CASE WHEN IT.No IS NULL THEN I.ItemNo + N' Item không tồn tại; ' ELSE '' END,
        CASE WHEN U.Code IS NULL THEN I.ItemNo + N'- UOM: ' + I.Uom + N' không hợp lệ; ' ELSE '' END
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

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<PriceSaveResult>(new CommandDefinition(
            "dbo.usp_SetupSalePrice_Save", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 300, cancellationToken: ct));
        return row ?? new PriceSaveResult { Ok = false, Message = "Lưu bảng giá thất bại" };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
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
