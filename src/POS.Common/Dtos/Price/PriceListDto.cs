using Newtonsoft.Json;

namespace POS.Common.Dtos.Price;

/// <summary>
/// 9.1 Danh mục Bảng giá — 1 dòng kết quả từ SP [dbo].[GetSalesPriceList] / [GetSalesPriceList_Export].
/// Tên property khớp cột SP trả về (Dapper map theo tên, case-insensitive). Port từ VCM.BLUEPOS PriceListResponseModel.
/// </summary>
public sealed class PriceListItemDto
{
    /// <summary>Tổng số bản ghi (SP trả cùng mỗi row để server-side paging).</summary>
    public int Total { get; set; }

    public int ID { get; set; }
    public string? BarcodeNo { get; set; }
    public string? SalesCode { get; set; }
    public string? SiteNo { get; set; }
    public string? ItemNo { get; set; }
    public string? ItemNo_PLG { get; set; }
    public string? ItemName { get; set; }
    public string? UnitOfMeasureCode { get; set; }
    public string? UnitPrice { get; set; }
    public string? StartingDateStr { get; set; }
    public string? EndingDateStr { get; set; }
    public string? EndingYearStr { get; set; }
}

/// <summary>
/// Bộ lọc danh mục bảng giá. IsCheck ("Còn hiệu lực"): 1 = chỉ giá còn hiệu lực, 0 = tất cả.
/// </summary>
public sealed class PriceListFilter
{
    public string? ItemNo { get; set; }
    public string? ItemName { get; set; }
    public string? Barcode { get; set; }
    public string? SalesCode { get; set; }
    public bool OnlyActive { get; set; }
    public int PageSize { get; set; } = 20;
    public int PageNumber { get; set; }

    [JsonIgnore]
    public int IsCheck => OnlyActive ? 1 : 0;
}
